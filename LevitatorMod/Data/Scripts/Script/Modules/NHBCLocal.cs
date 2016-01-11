using Levitator.SE.Modding;
using Levitator.SE.Network;
using Levitator.SE.Serialization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Levitator.SE.Utility;
using Scripts.Modding.Modules.CommonClient;

namespace Levitator.SE.LevitatorMod.Modules
{
	public class NHBCLocal : ModModule
	{
		public const string Name = "NHBC";
		public NHBCLocal(LocalInputComponent client) : base(client) { }
		public static NHBCLocal New(ModComponent component) { return new NHBCLocal((LocalInputComponent)component); }

		public override CommandRegistry GetCommands()
		{
			return new CommandRegistry() { { SetHomeMessage.Id, HandleHomeCommand } };
		}

		private void HandleHomeCommand(Connection none, ObjectParser command)
		{
			IMyPlayer player = MyAPIGateway.Session.Player;

			var ppos = player.GetPosition();
			var sphere = NHBCServer.GetPlayerHomeBounds(player);

			if (null == Util.PlayerCharacter(player))
			{
				NotificationCommand.Notice(NHBCServer.CharacterError);
				return;
			}

			//Do the MedicalRoom search client-side since the player is idle anyway and the server always has its hands full			
			//The two box search functions did not pick up blocks, but the sphere one does
			var near = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
			IMyMedicalRoom room;
			//foreach (IMyEntity ent in near)

			//This is awkward without foreach
			var notFound = Util.DoWhile(near, ent =>
			{
				if (null != (room = ent as IMyMedicalRoom) && NHBCServer.IsHomeValid(player, sphere, room))
				{
					ServerConnection.Send(new SetHomeMessage(room));
					return false;
				}
				else return true;
			});

			if (notFound) NotificationCommand.Notice(NHBCServer.RangeError);
		}
	}
}
