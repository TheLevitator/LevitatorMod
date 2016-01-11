/*
* Levitator Mod Move Entity With Velocities Command
*  
* Datagram definition to set an entity's position and velocity
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
using VRage.ModAPI;
using VRageMath;

namespace Scripts.Modding.Modules.CommonClient
{
	public class MoveEntityWithVelocitiesCommand : MoveEntityCommand
	{
		public new const string ClassId = "MEWV";
		public override string GetId() { return ClassId; }

		public SV3 LinearVelocity;
		public SV3 AngularVelocity;

		public MoveEntityWithVelocitiesCommand(IMyEntity ent, MatrixD pos, Vector3 linear, Vector3 angular) : base(ent, pos)
		{
			LinearVelocity.V = linear;
			AngularVelocity.V = angular;
		}

		public MoveEntityWithVelocitiesCommand(ObjectParser parser) : base(parser)
		{
			LinearVelocity = parser.Parse(SV3.New);
			AngularVelocity = parser.Parse(SV3.New);
		}

		public override void Serialize(ObjectSerializer ser)
		{
			base.Serialize(ser);
			ser.Write(LinearVelocity);
			ser.Write(AngularVelocity);
		}

		public override bool DeferredRun()
		{
			if (null != Entity.Ref)
			{
				Entity.Ref.SetWorldMatrix(PAO.PAO.GetMatrix());
				Entity.Ref.Physics.LinearVelocity = LinearVelocity.V;
				Entity.Ref.Physics.AngularVelocity = AngularVelocity.V;
				return true;
			}
			else return false;
		}

		//Source on server
		public static void MoveEntity(ServerComponent server, IMyEntity ent, MatrixD pos, Vector3 linear, Vector3 angular)
		{
			var msg = new MoveEntityWithVelocitiesCommand(ent, pos, linear, angular);
			msg.DeferredRun();
			server.Endpoint.Broadcast(msg, true);
		}

		//Consume on client
		public static new void ClientProcess(ModModule module, Connection conn, ObjectParser parser)
		{
			module.Component.Mod.DeferredTasks.Add(new MoveEntityWithVelocitiesCommand(parser));
		}
	}
}
