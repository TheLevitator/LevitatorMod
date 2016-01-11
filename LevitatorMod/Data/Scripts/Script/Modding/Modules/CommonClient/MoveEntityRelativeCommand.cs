/*
* Levitator Mod Move Entity Relative Command
*  
* Datagram definition to move an entity into place in relation to another entity.
* The player is placed relative to anoter object's position, so the network time delta
* will not skew the position and cause false collisions
*
* Copyright 2015 Levitator
* Reuse is free if you attribute the author
*
* V1.00
*
*/

using System;
using Levitator.SE.Modding;
using Levitator.SE.Network;
using Levitator.SE.Serialization;
using Levitator.SE.Utility;
using VRage.ModAPI;
using VRageMath;
using VRage.Components;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;

namespace Scripts.Modding.Modules.CommonClient
{
	public class FinishMoveEntityRelative : DeferredTask
	{
		private MoveEntityRelativeCommand Command;
		private RigidBodyFlag? oldFlags;
		public FinishMoveEntityRelative(MoveEntityRelativeCommand command, RigidBodyFlag? flags) : base(5000) { Command = command; }
				

		public bool DeferredRun()
		{
			if (null != Command.Target.Ref)
			{
				//MyObjectBuilder_EntityBase blah;				
				return true;
			}
			else return false;
		}

		public string OnAbandon(){ return "";  }
	}

	public class MoveEntityRelativeCommand : MoveEntityCommand
	{
		public new const string ClassId = "MERC";
		public override string GetId() { return ClassId; }

		public EntityRef Target;
		public SPAO WorldDest;
		 
		public MoveEntityRelativeCommand(IMyEntity ent, IMyEntity target, MatrixD pos) : base(ent, pos) {
			Target.Ref = target;
			WorldDest = new SPAO(Target.Ref.WorldMatrix);
		}
		public MoveEntityRelativeCommand(ObjectParser parser) : base(parser) {
			Target = parser.Parse(EntityRef.New);
			WorldDest = parser.Parse(SPAO.New);
		}
		public override void Serialize(ObjectSerializer ser)
		{
			base.Serialize(ser);
			ser.Write(Target);
			ser.Write(WorldDest);
		}

		public override bool DeferredRun()
		{			
			if (null != Entity.Ref && null != Target.Ref)
			{
				if (null != Entity.Ref.Physics)
				{
					//If the target is a block, it may not have physics. Traverse the hierarchy to find an entity that does and copy those velocities
					IMyEntity tmp = ModUtil.FindPhysics(Target.Ref);
					if (null != tmp)
					{
						Entity.Ref.Physics.AngularVelocity = tmp.Physics.AngularVelocity;
						Entity.Ref.Physics.LinearVelocity = tmp.Physics.LinearVelocity;
					}
					else
					{
						//No physics? Not moving.
						Entity.Ref.Physics.AngularVelocity = new Vector3(0, 0, 0);
						Entity.Ref.Physics.LinearVelocity = new Vector3(0, 0, 0);
					}
				}
				Entity.Ref.SetWorldMatrix(PAO.PAO.GetMatrix() * Target.Ref.WorldMatrix);
				return true;
			}
			else return false;
		}

		//Source on server
		static public void MoveEntityRelative(ServerComponent server, IMyEntity ent, IMyEntity target, MatrixD pos)
		{			
			var msg = new MoveEntityRelativeCommand(ent, target, pos);
			msg.DeferredRun();
			server.Endpoint.Broadcast(msg, true);
		}

		//Consume on client
		public static new void ClientProcess(ModModule module, Connection conn, ObjectParser parser)
		{
			module.Component.Mod.DeferredTasks.Add(new MoveEntityRelativeCommand(parser));
		}
	}
}
