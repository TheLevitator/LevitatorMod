
/*
* Levitator's Space Engineers Network Library
*
* A simple authenticating protocol that prevents
* endpoints from spoofing their identity
*
* Copyright Levitator 2015
*
* Reuse is free as long as you attribute the author.
*
* V1.0
*
*/

using Levitator.SE.Modding;
using Levitator.SE.Serialization;
using Levitator.SE.Utility;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;

namespace Levitator.SE.Network
{
    public enum Opcode
    {
        SYN,
        ACK,        
        Payload
    }

    public class SYN : Serializable
    {        
        public ulong CookieOffered;

		public SYN(ulong cookie)
        {            
            CookieOffered = cookie;
        }

        public SYN(ObjectParser parser)
        {            
            CookieOffered = ulong.Parse(parser.ParseField());
        }

        public void Serialize(ObjectSerializer ser)
        {            
            ser.Write(CookieOffered);
        }
    }

    public struct PacketHeader : Serializable
    {
        //Header
        public Opcode MessageType;
		public ulong Sender;
        public ulong Cookie;
        
        //Body -- one or the other
        private StringPos RawData;
		private Serializable Data;

        public bool Reliable;
        
        public PacketHeader(Opcode type, ulong cookie, Serializable data, bool reliable = true)
        {
            MessageType = type;
			Sender = Destination.Local();
			Cookie = cookie;
			RawData = null;
            Data = data;
            Reliable = reliable;			            
        }

		public PacketHeader(Opcode type, ulong cookie, StringPos rawData, bool reliable = true)
		{
			MessageType = type;
			Sender = Destination.Local(); ;
			Cookie = cookie;
			RawData = rawData;
			Data = null;
			Reliable = reliable;				
		}

		public PacketHeader(ObjectParser parser)
        {
            MessageType = (Opcode)int.Parse( parser.ParseField() );
			Sender = ulong.Parse(parser.ParseField());
			Cookie = ulong.Parse( parser.ParseField() );
            RawData = parser.Pos;
			Data = null;
            Reliable = true;    //It was reliable enough since we obviously received it			
        }

        public void Serialize(ObjectSerializer ser)
        {
            ser.Write((int)MessageType);
			ser.Write(MyAPIGateway.Multiplayer.MyId);
            ser.Write(Cookie);
			if (null != Data)
				ser.Write(Data);
			else
				if(null != RawData)
					ser.Write(RawData);
        }

        public StringPos GetData(){ return new StringPos(RawData); }
		public ConnectionKey GetConnectionKey() { return new ConnectionKey() { Remote = Sender, InCookie = Cookie }; }
    }

    //Cached dictionary of players by SteamId.
    //We only repopulate it on a cache miss.
    public static class PlayerCache
    {
        static Dictionary<ulong, IMyPlayer> Players = new Dictionary<ulong, IMyPlayer>();
		
        static public IMyPlayer GetPlayer(ulong steamId)
        {
            IMyPlayer result;

            if (Players.TryGetValue(steamId, out result))
                return result;
            else
            {
                Players.Clear();
                MyAPIGateway.Players.GetPlayers(null, AddPlayer);
                Players.TryGetValue(steamId, out result);
                return result;
            }
        }
        
        static private bool AddPlayer(IMyPlayer player)
        {
            Players.Add(player.SteamUserId, player);
            return false;
        }
    }

    public struct Destination
    {
        private IMyPlayer Player;
        private ulong RemoteId;        
               
        public  Destination(ulong id)
        {
            RemoteId = id;
            Player = PlayerCache.GetPlayer(RemoteId);
        }

        public Destination(IMyPlayer player)
        {
            RemoteId = player.SteamUserId;
            Player = player;
        }

		public IMyPlayer GetPlayer()
		{
			if (null != Player) return Player;
			return Player = PlayerCache.GetPlayer(RemoteId);			
		}

		//This would have been extremely obscure to track down without midspace's code for reference
		public bool IsAdmin
		{
			get{ return Midspace.IsAdmin(GetPlayer()); }
		}		

		public static implicit operator ulong (Destination dest){ return dest.RemoteId; }
        public static implicit operator Destination(ulong id){ return new Destination(id); }

		public static Destination Server() { return new Destination(MyAPIGateway.Multiplayer.ServerId); }
		public static Destination Local() { return new Destination(MyAPIGateway.Multiplayer.MyId); }

		public override string ToString(){ return RemoteId.ToString(); }
	}

    public class Connection : IDisposable
    {        
        private NetworkEndpoint EndPoint;
        public readonly Destination Destination;
        public readonly ulong InCookie;
        public ulong OutCookie;
        Queue<PacketHeader> OutQueue = new Queue<PacketHeader>();
        public bool IsOpen = false;

        //public event Action<StringPos> OnDataArrival;
        public Action<Connection, StringPos> OnDataArrival;
             
        public Connection(NetworkEndpoint net, Destination dest, ulong inCookie, ulong outCookie)
        {
            EndPoint = net;
            Destination = dest;
            InCookie = inCookie;
            OutCookie = outCookie;

            PacketHeader SynHeader = new PacketHeader(Opcode.SYN, OutCookie, new SYN(InCookie), true);            
            EndPoint.SendRaw(SynHeader, dest);            
        }

		public void Dispose()
		{
			if (null != OutQueue) //Dispose() and Close() call each other, so don't recurse
			{
				OutQueue.Clear();
				OutQueue = null;
				EndPoint.Close(this);
				EndPoint = null;
				OnDataArrival = null;								
			}
		}

		public ConnectionKey GetKey() { return new ConnectionKey() { Remote = Destination, InCookie = InCookie  }; }

		/*
		//Sends raw data. It's not escaped or formatted. It's just inserted into the packet body as-is.
		public void Send(StringPos pos, bool reliable = true)
		{
			Send(new PacketHeader(Opcode.Payload, OutCookie, pos, reliable));
		}
		*/
		/*
		private StringPos TmpPos = new StringPos("", 0); //Avoid creating a new one all the time
		public void Send(string data, bool reliable = true)
		{
			TmpPos.String = data;			
			Send(new PacketHeader(Opcode.Payload, OutCookie, TmpPos, reliable));
		}
		*/

		public void Send(Serializable obj, bool reliable = true)
        {
            PacketHeader packet = new PacketHeader(Opcode.Payload, OutCookie, obj, reliable);
			Send(packet);            
        }

		private void Send(PacketHeader packet)
		{
			if (OutCookie == 0)
				OutQueue.Enqueue(packet);
			else
				EndPoint.SendRaw(packet, Destination);
		}

		public void Flush()
        {
            PacketHeader packet;
            if (OutCookie == 0) return;
            while (OutQueue.Count > 0)
            {
                packet = OutQueue.Dequeue();
                packet.Cookie = OutCookie;
                EndPoint.SendRaw(packet, Destination);
            }
        }

        public void RaiseOnDataArrival(StringPos pos) { if(null != OnDataArrival) OnDataArrival(this, pos); }		
	}

	class ConnectionDictionary
	{
		private Dictionary<ConnectionKey, Connection> Connections = new Dictionary<ConnectionKey, Connection>();
		private Dictionary<Reference<IMyPlayer>, HashSet<Connection>> ConnectionsByPlayer = new Dictionary<Reference<IMyPlayer>, HashSet<Connection>>();

		public ConnectionDictionary() { }
		public void Add(Connection conn, bool unique)
		{			
            var pc = this[conn.Destination.GetPlayer()];

			if (unique && pc.Count > 0)
				throw new Exception("Duplicate connection");

			pc.Add(conn);
			Connections.Add(conn.GetKey(), conn);
		}

		public void Remove(Connection conn)
		{
			Connections.Remove(conn.GetKey());
			this[conn.Destination.GetPlayer()].Remove(conn);			
		}

		public Connection this[ConnectionKey key]
		{
			get
			{
				Connection result = null;
				Connections.TryGetValue(key, out result);
				return result;
			}
		}

		public HashSet<Connection> this[IMyPlayer key]
		{
			get
			{
				HashSet<Connection> result;
				ConnectionsByPlayer.TryGetValue(Reference.Create(key), out result);
				if (null == result)
				{
					var connections = new HashSet<Connection>();
					ConnectionsByPlayer.Add(Reference.Create(key), connections);
					return connections;
				}
				else return result;
			}
		}

		public Dictionary<ConnectionKey, Connection>.ValueCollection Values { get { return Connections.Values; } }
	}


	class ConnectionException : Exception
    {
        public ConnectionException(Connection conn, string message):base(message + ": SteamID: " + (ulong)conn.Destination){}
    }

	public struct ConnectionKey
	{
		public ulong Remote;
		public ulong InCookie;
	}

	class CookieGenerator
	{		
		private HashSet<ulong> Cookies = new HashSet<ulong>();		
		private int Seed = new Random().Next();

		//Not fancy. Probably sufficient.
		private void GetEntropy()
		{
			Seed +=  DateTime.Now.Millisecond  | DateTime.Now.Second << 10;			
			MyAPIGateway.Entities.GetEntities(null, EntropyPredicate);
		}

		//Jumble the RNG around in an unpredictable way
		private bool EntropyPredicate(IMyEntity ent)
		{
			var p = ent.GetPosition();
			Seed += (int)((p.X % 1000.0) * 1000);
			Seed += (int)((p.Y % 1000.0) * 1000);
			Seed += (int)((p.Z % 1000.0) * 1000);
			return false;
		}

		public ulong GetCookie()
		{
			ulong cookie;
			var Rnd = new Random(Seed);

			do
			{
				GetEntropy();
				cookie = (ulong)(ulong.MaxValue * Rnd.NextDouble()); //Correct (tm) way, per MSDN
				Seed += Rnd.Next(); //Avoid periodicity
			} while (cookie == 0 || Cookies.Contains(cookie) );

			return cookie;
		}

		public void Discard(ulong cookie) { Cookies.Remove(cookie);  }		
	}

    //A node
    public class NetworkEndpoint : IDisposable
    {
        readonly ushort MessageId;        
        private IModLog Log;
        private ConnectionDictionary Connections = new ConnectionDictionary();
		private ConnectionDictionary PendingConnections = new ConnectionDictionary();
        private DateTime LastClean = DateTime.Now;
		static readonly CookieGenerator CookieGenerator = new CookieGenerator();        

        public NetworkEndpoint(ushort messageId, IModLog log)
        {
            MessageId = messageId;            
            Log = log;
            MyAPIGateway.Multiplayer.RegisterMessageHandler(MessageId, MessageHandler);
			//Log.Log("NetworkEndpoint up");
        }

		public Connection this[IMyPlayer key]
		{
			get
			{
				var Set = Connections[key];
				return Set.Count > 0 ? Set.First() : null;
			}
		}
		
		public Connection Open(Destination dest)
        {
			//Currently only support one connection to a given destination because we need to be able to map Player -> Connection 1:1
			//Really can't think of why we would need more than one link from one node to another anyway			
			var conns = Connections[dest.GetPlayer()];
			if (conns.Count > 0) conns.First().Dispose();			

			var oldpend = PendingConnections[dest.GetPlayer()];
			Util.ForEach(oldpend, c => c.Dispose());

			var conn = new Connection(this, dest, CookieGenerator.GetCookie(), 0);
            PendingConnections.Add(conn, false);
            return conn;
        }

		public void Close(Connection conn)
		{
			CookieGenerator.Discard(conn.InCookie);
			Connections.Remove(conn);
			PendingConnections.Remove(conn);
			conn.Dispose();
		}

        public void SendRaw(PacketHeader packet, ulong dest)
        {
            ObjectSerializer ser = new ObjectSerializer(null);
            string outstring;
            packet.Serialize(ser);
            ser.CloseObject();

            outstring = ser.ToString();
            byte[] bytes = Encoding.Unicode.GetBytes(outstring);
            if (bytes.Length > 4096) throw new Exception("Message overflow"); //midspace limits their messages to 4k. Don't know if it's necessary.
            MyAPIGateway.Multiplayer.SendMessageTo(MessageId, bytes, dest, packet.Reliable);
        }

		public void Broadcast(Serializable message, bool reliable = true)
		{
			Util.ForEach(Connections.Values, conn => conn.Send(message, reliable));
		}

        private void MessageHandler(byte[] message)
        {
            try {
                if(DateTime.Now - LastClean >= new TimeSpan(0, 10, 0))                
                    CleanConnections();
                

                string AsString = Encoding.Unicode.GetString(message);
				Log.Log("PACKET: " + AsString, false);
				ObjectParser parser = new ObjectParser(AsString, true);
                PacketHeader packet = new PacketHeader(parser);
				Connection conn;
							
				//Incoming connection request           
				if (packet.MessageType == Opcode.SYN && packet.Cookie == 0)
                {
                    ObjectParser synParser = new ObjectParser(packet.GetData(), true);
                    SYN syn = new SYN(synParser);

					//Ignore our own outgoing connection request
					if (packet.Sender == Destination.Local() && null != PendingConnections[new ConnectionKey() { Remote = packet.Sender, InCookie = syn.CookieOffered }])			
						return;

					Log.Log("PACKET (Connection request)", false);
					PendingConnections.Add(new Connection(this, packet.Sender, CookieGenerator.GetCookie(), syn.CookieOffered), false);
					return;
                }
                
                if (null == (conn = Connections[packet.GetConnectionKey()]) )
                {
					if (null != (conn = PendingConnections[packet.GetConnectionKey()]))
					{
						ProcessPendingConnection(conn, packet);
						return;
					}
					else
						return; //When there is a local player, we see our own packets					
				}

				//Log.Log("PACKET: " + AsString, false);

				switch (packet.MessageType)
                {
					case Opcode.Payload:
						conn.RaiseOnDataArrival(packet.GetData());
						break;

					case Opcode.SYN:
						Log.Log("", new ConnectionException(conn, "Duplicate SYN"));						
                        break;

                    case Opcode.ACK:					
						Log.Log("", new ConnectionException(conn, "Duplicate ACK"));						
                        break;
                                      
                    default:
                        Log.Log("Unknown packet type: " + packet.MessageType);
                        break;
                }
            }
            catch (Exception x)
            {
                Log.Log("Unexpected exception handling network message", x);
            }
        }

		public void ProcessPendingConnection(Connection conn, PacketHeader packet)
		{
			switch (packet.MessageType)
			{
				case Opcode.SYN:					
					ObjectParser synParser = new ObjectParser(packet.GetData(), true);
					SYN syn = new SYN(synParser);
					conn.OutCookie = syn.CookieOffered;					
					PacketHeader ack = new PacketHeader(Opcode.ACK, conn.OutCookie, null as Serializable, true);
					SendRaw(ack, conn.Destination);
					conn.Flush();
					DoCompletion(conn);
					break;

				case Opcode.ACK:        //This proves that our connection requestor is legitimate if they can return the cookie we provided them
										//Log.Log("ACK!", true);
					DoCompletion(conn);					
					break;

				case Opcode.Payload:    //Assuming this is UDP underneath, we could get a data packet prior the the ACK, which is still valid
					DoCompletion(conn);
					conn.RaiseOnDataArrival(packet.GetData());
					break;

				default:
					Log.Log("Unknown packet type on pending connection: " + packet.MessageType);
					break;
			}
		}

		public void DoCompletion(Connection conn)
		{
			PendingConnections.Remove(conn);
			Util.ForEach(PendingConnections[conn.Destination.GetPlayer()], c => c.Dispose() );

			conn.IsOpen = true;
			Connections.Add(conn, true);
			if(null != OnConnectionCompleted) OnConnectionCompleted(conn);
		}

        public static bool AreServer(){ return MyAPIGateway.Multiplayer.IsServer; }		

        public Action<Connection> OnConnectionCompleted;

		private List<Connection> CleanQueue = new List<Connection>();
		public void Dispose()
        {
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(MessageId, MessageHandler);
			OnConnectionCompleted = null;
			CleanQueue.Clear();
			CleanQueue.AddRange(Connections.Values);
			//foreach (var conn in CleanQueue) { conn.Dispose(); }
			Util.ForEach(CleanQueue, Util.Disposer);

			//Log.Log("NetworkEndpoint down");
		}
		       
        private void CleanConnections()
        {
			IMyPlayer player;
			CleanQueue.Clear();
			//foreach (var conn in Connections.Values){
			Util.ForEach(Connections.Values, conn =>
				{
					player = conn.Destination.GetPlayer();
					if (null != player && null == player.Client)
						CleanQueue.Add(conn);
				});
			//foreach (var conn in CleanQueue) { conn.Dispose(); }
			Util.ForEach(CleanQueue, Util.Disposer);
            LastClean = DateTime.Now;          
        }
    }    
}
