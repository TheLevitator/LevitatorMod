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

using Levitator.SE.Modding;
using Levitator.SE.Network;
using Levitator.SE.Serialization;
using Levitator.SE.Utility;
using System;

namespace Scripts.Modding.Modules.CommonServer
{
	public class CommonServer : ModModule
	{
		public CommonServer(ServerComponent component) : base(component) { }

		public override CommandRegistry GetCommands()
		{
			return new CommandRegistry() { { UserCommand.ClassId, HandleUserCommand } };
		}

		private void HandleUserCommand(Connection conn, ObjectParser parser)
		{
			var cmd = new UserCommand(parser);
			string name;
			if (!Component.DispatchCommand(conn, new StringPos(cmd.Text), out name))
				Log.Log("", new Exception("Received unrecognized chat command: " + name));
		}
	}
}
