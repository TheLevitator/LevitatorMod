/*
* Levitator Mod Common Local Module
*
* This is the client-side bootstrapping module that allows users to load other functionality from the client
*
* Copyright 2015 Levitator
* Reuse is free if you attribute the author
*/

using Levitator.SE.Network;
using Levitator.SE.Serialization;

namespace Levitator.SE.Modding.Modules.CommonLocal
{
	public class CommonLocal : ModModule
	{
		public const string Name = "LevitatorMod";
		public CommonLocal(ModComponent comp) : base(comp) { }
		public static CommonLocal New(ModComponent comp) { return new CommonLocal(comp); }

		public override CommandRegistry GetCommands()
		{
			return new CommandRegistry() {				
				{ LoadModuleCommand.ClassId, SendLoadModule },
				{ UnloadModuleCommand.ClassId, SendUnloadModule}
			};
		}

		private void SendLoadModule(Connection conn, ObjectParser parser)
		{			
			var msg = new LoadModuleCommand(parser);
			ServerConnection.Send(msg);
		}

		private void SendUnloadModule(Connection conn, ObjectParser parser)
		{
			var msg = new UnloadModuleCommand(parser);
			ServerConnection.Send(msg);
		}
	}
}
