/*
* Levitator User Command
*  
* Datagram definition which just encapsulates the player's command text
* entered from chat. The prefix character is removed by the LocalInputComponent
* and the text has been placed inside curly braces to make it compatible with
* ObjectParser.
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

namespace Levitator.SE.Modding.Modules.CommonServer
{
	public class UserCommand : Command
	{
		public const string ClassId = "UserCommand";
		public override string GetId() { return ClassId; }

		public string Text;
		public UserCommand(string text) { Text = text; }
		public UserCommand(ObjectParser parser) { Text = parser.ParseField(); }

		public override void Serialize(ObjectSerializer ser)
		{
			base.Serialize(ser);
			ser.Write(Text);
		}

		public static void HandleUserCommand(ModModule module, Connection conn, ObjectParser parser)
		{
			var cmd = new UserCommand(parser);
			string name;
			if (!module.Component.DispatchCommand(conn, new StringPos(cmd.Text), out name))
				module.Log.Log("", new Exception("Received unrecognized chat command: " + name));
		}
	}
}
