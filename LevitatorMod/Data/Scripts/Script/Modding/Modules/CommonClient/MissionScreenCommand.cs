/*
* Levitator Mod Mission Screen Command Message
*  
* Datagram definition to display the mission screen which is basically a large message box
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
using Sandbox.ModAPI;

namespace Scripts.Modding.Modules.CommonClient
{
	public class MissionScreenCommand : Command
	{
		public const string ClassId = "MissionScreen";
		public override string GetId() { return ClassId; }

		public string Title;
		public string Message;
		public string Prefix;
		public string Objective;


		public MissionScreenCommand(string title, string message, string prefix = "", string objective = "")
		{
			Title = title;
			Message = message;
			Prefix = prefix;
			Objective = objective;
		}

		public MissionScreenCommand(ObjectParser parser)
		{
			Title = parser.ParseField();
			Message = parser.ParseField();
			Prefix = parser.ParseField();
			Objective = parser.ParseField();
		}

		public override void Serialize(ObjectSerializer ser)
		{
			base.Serialize(ser);
			ser.Write(Title);
			ser.Write(Message);
			ser.Write(Prefix);
			ser.Write(Objective);
		}

		public static void ClientProcess(ModModule module, Connection conn, ObjectParser parser)
		{
			var msg = new MissionScreenCommand(parser);
			if (null == MyAPIGateway.Utilities) module.Log.Log("Failed to display mission screen [" + msg.Title + "]: " + msg.Message);
			else MyAPIGateway.Utilities.ShowMissionScreen(msg.Title, msg.Prefix, msg.Objective, msg.Message, null, null);
		}

		public static void Show(Connection conn, string title, string message, string prefix = "", string objective = "")
		{
			var msg = new MissionScreenCommand(title, message, prefix, objective);
			conn.Send(msg);
		}
	}
}
