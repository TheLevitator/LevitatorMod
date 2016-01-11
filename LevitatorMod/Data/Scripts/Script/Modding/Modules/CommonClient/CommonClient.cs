/*
* Levitator's Space Engineers Modding Library
* Common Client Class
*
* Registers client-side event handlers for common general-purpose matters
*
* Reuse is free as long as you attribute the author.
*
* V1.0
*
*/

using Levitator.SE.Modding;
using Levitator.SE.Modding.Modules;

namespace Scripts.Modding.Modules.CommonClient
{	
	public class CommonClient : ModModule
	{
		public const string Name = "CommonClient";
		public CommonClient(ClientComponent component) : base(component) { }
		public static CommonClient New(ModComponent component) { return new CommonClient((ClientComponent)component); }

		public override CommandRegistry GetCommands()
		{
			return new CommandRegistry() {
				{ NotificationCommand.ClassId, new Stateless(this, NotificationCommand.ClientProcess).Handler },
				{ MissionScreenCommand.ClassId, new Stateless(this, MissionScreenCommand.ClientProcess).Handler },
				{ MoveEntityCommand.ClassId, new Stateless(this, MoveEntityCommand.ClientProcess).Handler },
				{ MoveEntityWithVelocitiesCommand.ClassId, new Stateless(this, MoveEntityWithVelocitiesCommand.ClientProcess).Handler },
				{ MoveEntityRelativeCommand.ClassId, new Stateless(this, MoveEntityRelativeCommand.ClientProcess).Handler },
                { LoadModuleCommand.ClassId, new Stateless(this, LoadModuleCommand.HandleOnClient).Handler },
                { UnloadModuleCommand.ClassId, new Stateless(this, UnloadModuleCommand.HandleOnClient).Handler }
			};
		}
	}
}
