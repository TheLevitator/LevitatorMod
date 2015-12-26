/*
* Levitator Mod Move Entity Command
*  
* Datagram definition to perform an entity movement on the server and broadcast it to all clients
*
* Copyright 2015 Levitator
* Reuse is free if you attribute the author
*
* V1.00
*
*/

using Levitator.SE.Modding;
using Levitator.SE.Network;
using Levitator.SE.Serialization;
using Levitator.SE.Utility;
using VRage;
using VRage.Components;
using VRage.ModAPI;
using VRageMath;

namespace Scripts.Modding.Modules.CommonClient
{
	public class MoveEntityCommand : DeferredTaskCommand
	{
		public const int DefaultTimeout = 5000; //ms
		public EntityRef Entity;
		public SPAO PAO;
		
		public const string ClassId = "MEC"; //Short name because we could conceivably need to exchange lots of these
		public override string GetId() { return ClassId; }

		public MoveEntityCommand(IMyEntity ent, MatrixD pos) : base(DefaultTimeout)
		{
			Entity = new EntityRef(ent);
			PAO = new SPAO(pos);	
		}

		public MoveEntityCommand(ObjectParser parser) : base(DefaultTimeout)
		{
			Entity = parser.Parse(EntityRef.New);
			PAO = parser.Parse(SPAO.New);
		}

		public override void Serialize(ObjectSerializer ser)
		{
			base.Serialize(ser);
			ser.Write(Entity);
			ser.Write(PAO);
		}

		public override bool DeferredRun()
		{
			if (null != Entity.Ref)
			{
				Entity.Ref.SetWorldMatrix(PAO.PAO.GetMatrix());
				return true;
			}
			else return false;
		}

		//Source on server
		public static void MoveEntity(ServerComponent server, IMyEntity ent, MatrixD pos)
		{
			var msg = new MoveEntityCommand(ent, pos);
			msg.DeferredRun(); //Not really deferred on the server, since it should always succeed
			server.EndPoint.Broadcast(msg, true);
		}

		//Consume on client
		public static void ClientProcess(ModModule module, Connection conn, ObjectParser parser)
		{
			module.Component.Mod.DeferredTasks.Add(new MoveEntityCommand(parser));
		}
	}
}
