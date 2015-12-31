/*
* Levitator Mod No Hale Bop Cult Module
* Server Side Module
* 
* Module which forces players to spawn only at their home Medical Bay
* Because this is a game about space travel, not a UFO suicide cult
*
* V1.03
*
*/

using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using System.IO;
using VRage.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRageMath;
using VRage;
using Sandbox.ModAPI.Interfaces;
using Levitator.SE.Modding;
using Levitator.SE.Utility;
using Levitator.SE.Serialization;
using Levitator.SE.Network;
using Scripts.Modding.Modules.CommonClient;

namespace Levitator.SE.LevitatorMod.Modules
{
	public class SetHomeMessage : Command
	{
		public const string Id = "home";
		public override string GetId() { return Id; }
		public EntityRef Home;

		public SetHomeMessage(IMyMedicalRoom home) { Home.Ref = home; }
		public SetHomeMessage(ObjectParser parser) { Home = parser.Parse(EntityRef.New); }

		public override void Serialize(ObjectSerializer ser)
		{
			base.Serialize(ser);
			ser.Write(Home);
		}
	}
	
	public class NHBCServer : ModModule
	{
		//Hard config
		public const string HomeDataPath = "HomeData";			//File name to store users' home data
		public const float HomeRange = 2;                       //Max distance in m to a medical room at which user can set home
		public const int GraceLimit = 5;						//Maximum number of times Herr Noob may spawn freely before NHBC rules come into effect

		//Error messages
		public static readonly string RangeError = "You must be within " + HomeRange + "m of an accessible Medical Room";
		public static readonly string CharacterError = "You must be on foot to set your spawn";
		
		bool DataFileValid;
		HomeDictionary HomeData;
		Singleton<TopLevelEntityTracker<IMyCharacter>>.Ref CharacterTracker;	
		EntityTrackerSink<IMyCharacter> Characters;
		List<IMyCharacter> RespawnQueue = new List<IMyCharacter>((int)MyAPIGateway.Players.Count * 2);
		List<IMyCharacter> NewRespawnQueue = new List<IMyCharacter>((int)MyAPIGateway.Players.Count * 2);
		readonly List<IMyPlayer> DeadList = new List<IMyPlayer>((int)MyAPIGateway.Players.Count * 2);

		public static BoundingSphereD GetPlayerHomeBounds(IMyPlayer player){
			return new BoundingSphereD(player.GetPosition(), HomeRange);
		}

		//A consistent way of checking whether a spawn is valid, which we can use to select a spawn on the client,
		//validate it on the server, and (then validate again at the moment of respawn where bounds are null and ignored).
		public static bool IsHomeValid(IMyPlayer player, BoundingSphereD? bb, IMyMedicalRoom room) {
			return null != room && (!bb.HasValue || bb.Value.Contains(room.GetPosition()) != ContainmentType.Disjoint) 
				&& room.HasPlayerAccess(player.PlayerID) && room.IsWorking;
		}

		public NHBCServer(ModComponent mc) : base(mc)
		{			
			Log.Log("NHBC Module Created", false);
			CharacterTracker = Singleton.Get<TopLevelEntityTracker<IMyCharacter>>(TopLevelEntityTracker<IMyCharacter>.New);
            Characters = new EntityTrackerSink<IMyCharacter>(CharacterTracker.Instance, OnCharacterAdded, null);
			mc.RegisterForUpdates(this);
        }

		public override void Dispose()
		{
			Util.DisposeIfSet(ref Characters);
			Util.DisposeIfSet(ref CharacterTracker);	
			if (null != HomeData)
			{
				HomeData.Clear();
				HomeData = null;
			}
			base.Dispose();
		}		

		//Dead characters being removed have no controlling player
		//private void OnCharacterRemoved(IMyCharacter obj){}
	
		//It's a hard life...
		private void HandleHomeless(IMyPlayer player, IMyCharacter character, string detail)
		{
			MissionScreenCommand.Show(Component.Mod.ServerComponent.EndPoint[player], "This is not a UFO suicide cult", detail + "\n" +
				"You may travel to a friendly, active medical room and select it as your home by typing '/home' in chat.", "Objective: ",
				"find a Medical Room and type '/home'");
			
			//To the void with you
			MoveEntityCommand.MoveEntity(Component.Mod.ServerComponent, character as IMyEntity,
				new MyPositionAndOrientation(new Vector3(float.MaxValue / 2, float.MaxValue / 2, float.MaxValue / 2),
				Matrix.Identity.Forward, Matrix.Identity.Up).GetMatrix());
			
			character.Kill();
		}

		//...except for when we are nice
		private void HandleGrace(IMyPlayer player, PlayerData data)
		{
			MissionScreenCommand.Show(Component.Mod.ServerComponent.EndPoint[player], "This is not a UFO suicide cult",
					"From now on, whenever you spawn on foot you will always start at your home Medical Room unless you choose a spawn ship instead. " +
					"This is to prevent people from hitchhiking across the galaxy by suiciding. You must travel to a Medical Room and type '/home' in chat to set your home spawn.\n\n" +
					"Your remaining grace period in spawns is: " + (GraceLimit - data.Graces).ToString(), "Objective: ", "Go type '/home' near a medical room!");			
			++data.Graces;
        }

		private void CheckForRespawn(IMyCharacter character)
		{
			var charent = character as IMyEntity;
			var player = MyAPIGateway.Players.GetPlayerControllingEntity(charent);
			IMyMedicalRoom room = null;

			if (null != player && DeadList.Contains(player))
			{
				DeadList.Remove(player);
				var pd = HomeData[player];

				if (null != pd.MedicalRoom.Ref)
				{
					if (!IsHomeValid(player, null, pd.MedicalRoom.Ref as IMyMedicalRoom))											
					{
						HandleHomeless(player, character,
							"Your home spawn is currently unavailble because it is offline. You must respawn with a spawn ship until it becomes available again or... \n\n ");
						return;
					}
					//Fall through
				}
				else
				{
					if (pd.Graces >= GraceLimit)					
						HandleHomeless(player, character, "You do not have a home spawn set. You must use spawn ships until you have a home spawn active.");					
					else
						HandleGrace(player, pd);
					return;
				}

				IMyEntity phys = ModUtil.FindPhysics(room);
				Vector3 linearV;
				Vector3 angularV;

				if (null != phys) {
					linearV = phys.Physics.LinearVelocity;
					angularV = phys.Physics.AngularVelocity;
				}
				else linearV = angularV = new Vector3(0, 0, 0);

				room = pd.MedicalRoom.Ref as IMyMedicalRoom;
				(character as IMyControllableEntity).SwitchDamping(); //Switch damping off in case the player is spawning on a moving ship?
				MoveEntityWithVelocitiesCommand.MoveEntity(Component.Mod.ServerComponent, charent, 
					new MyPositionAndOrientation(pd.Position, pd.Forward, pd.Up).GetMatrix() * room.WorldMatrix, linearV, angularV);

				//charent.SetWorldMatrix(new MyPositionAndOrientation(pd.Position, pd.Forward, pd.Up).GetMatrix() * room.WorldMatrix );
				//charent.SetPosition( (new MyPositionAndOrientation(pd.Position, pd.Forward, pd.Up).GetMatrix() * room.WorldMatrix).Translation );	
				//MoveEntityRelativeCommand.MoveEntityRelative(Component.Mod.ServerComponent, charent, room,
				//	new MyPositionAndOrientation(pd.Position, pd.Forward, pd.Up).GetMatrix());
					
				NotificationCommand.Notice(Component.Mod.ServerComponent.EndPoint[player], "Spawned at your home medbay");
				pd.Graces = GraceLimit;	//Choose wisely
			}
			else
				if(!charent.Closed) NewRespawnQueue.Add(character);
        }

		public override void Update()
		{
			if (RespawnQueue.Count == 0) return;
			NewRespawnQueue.Clear();
			Util.ForEach(RespawnQueue, CheckForRespawn);
			Util.Swap(ref RespawnQueue, ref NewRespawnQueue);
		}		

		private bool DeadPredicate(IMyPlayer player)
		{
			return null != player.Client && null != player.Controller && null == player.Controller.ControlledEntity;
		}

		//Live characters resuming a non-dedicated session have a controlling player
		//Live character exiting cockpits have a controlling player
		//Dead players respawning do not
		private void OnCharacterAdded(IMyCharacter character)
		{			
			if (null == MyAPIGateway.Players.GetPlayerControllingEntity(character as IMyEntity))
			{
				//We have to enumerate all the dead here so that when this soulless charcter receives its controller
				//we will then know if they were previously dead and now spawning, or merely climbing out of a cockpit
				DeadList.Clear();
				MyAPIGateway.Players.GetPlayers(DeadList, DeadPredicate);
                RespawnQueue.Add(character);				
			}
		}

		public override CommandRegistry GetCommands()
		{
			return new CommandRegistry()
			{
				{SetHomeMessage.Id, HandleHomeCommand}
			};
		}

		public override void LoadData()
		{
			Log.Log(HomeDataPath + " loading", false);
			HomeData = null;
			DataFileValid = true;
			if (MyAPIGateway.Utilities.FileExistsInLocalStorage(ModBase.QualifyFilename(HomeDataPath), GetType()))
			{
				try
				{
					LoadPlayerData();
				}
				catch (Exception x)
				{
					DataFileValid = false; //Remember not to save over a corrupt state file that the admin might want to repair
					HomeData = null;
					Log.Log(HomeDataPath + " could not be loaded. It will not be saved", x);
				}
			}
			else
			{
				Log.Log("No player home data found. Reset.", false);
			}

			if (null == HomeData)
				HomeData = new HomeDictionary();			
		}

		public override void SaveData()
		{
			if (null != HomeData && DataFileValid) //sometimes saves before loading?
				SavePlayerData();			
		}

		private void LoadPlayerData()
		{
			TextReader tr = MyAPIGateway.Utilities.ReadFileInLocalStorage(ModBase.QualifyFilename(HomeDataPath), GetType());

			try
			{
				HomeData = new HomeDictionary( MyAPIGateway.Utilities.SerializeFromXML<Util.SerializableDictionaryWrapper<ulong, PlayerData>>(tr.ReadToEnd()).GetDictionary() );
			}
			finally
			{
				tr.Dispose();
			}
		}

		private void SavePlayerData()
		{
			string file = ModBase.QualifyFilename(HomeDataPath);
			Log.Log(file + " saving.", false);
			string newData = MyAPIGateway.Utilities.SerializeToXML(new Util.SerializableDictionaryWrapper<ulong, PlayerData>(HomeData));

			//Shame there are no file rename or copy functions
			if (MyAPIGateway.Utilities.FileExistsInLocalStorage(file, GetType()))
				MyAPIGateway.Utilities.DeleteFileInLocalStorage(file, GetType());
			TextWriter tw = MyAPIGateway.Utilities.WriteFileInLocalStorage(file, GetType());

			try
			{
				tw.Write(newData);
			}
			finally
			{
				tw.Dispose();
			}
		}

		private void HandleHomeCommand(Connection conn, ObjectParser parser)
		{
			var player = conn.Destination.GetPlayer();
			var character = Util.PlayerCharacter(player);
			var msg = new SetHomeMessage(parser);
			var home = msg.Home.Ref as IMyMedicalRoom;
			
			if(null == Util.PlayerCharacter(player))
			{
				NotificationCommand.Notice(conn, CharacterError);
				return;
			}

			if(!IsHomeValid(player, GetPlayerHomeBounds(player), home))
			{
				NotificationCommand.Notice(conn, RangeError);
				return;
			}
									
			HomeData[player].SetHome(Util.RelativePosition((character as IMyEntity).WorldMatrix, home.WorldMatrix), home);
			NotificationCommand.Notice(conn, "Your home spawn has been set: " + home.DisplayNameText);
		}
	}
}
