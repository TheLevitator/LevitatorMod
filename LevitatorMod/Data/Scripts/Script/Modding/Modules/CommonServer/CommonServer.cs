/*
* Levitator Mod Common Server Module
*
* This is the standard server module. All it does is listen for UserCommands,
* and extract the user's command text to then interpret that as a new command
*
* Copyright 2015 Levitator
* Reuse is free if you attribute the author
*
* V1.00
*
*/
namespace Levitator.SE.Modding.Modules.CommonServer
{
	public class CommonServer : ModModule
	{
		public const string Name = "LevitatorMod";
		public CommonServer(ServerComponent component) : base(component) { }
		public static CommonServer New(ModComponent component) { return new CommonServer((ServerComponent)component); }

		public override CommandRegistry GetCommands()
		{
			return new CommandRegistry() {
				{ UserCommand.ClassId, new Stateless(this, UserCommand.HandleUserCommand).Handler },
				{ LoadModuleCommand.ClassId, new Stateless(this, LoadModuleCommand.HandleOnServer).Handler },
				{ UnloadModuleCommand.ClassId, new Stateless(this, UnloadModuleCommand.HandleOnServer).Handler }
			};
		}		
	}
}
