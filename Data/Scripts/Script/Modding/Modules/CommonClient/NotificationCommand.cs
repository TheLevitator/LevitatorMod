/*
* Levitator Notification Command
*  
* Datagram definition to superimpose notification text along the bottom of the client's screen
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

namespace Scripts.Modding.Modules.CommonClient
{
	public class NotificationCommand : Command
	{
		public const string ClassId = "Notice";
		public override string GetId() { return ClassId; }

		public string Message;

		public NotificationCommand(string message) { Message = message; }

		public NotificationCommand(ObjectParser parser) { Message = parser.ParseField(); }

		public override void Serialize(ObjectSerializer ser)
		{
			base.Serialize(ser);
			ser.Write(Message);
		}

		public static void ClientProcess(ModModule module, Connection conn, ObjectParser parser)
		{
			var notice = new NotificationCommand(parser);
			ModBase.ShowNotification(notice.Message);
		}

		//Server version
		public static void Notice(Connection conn, string msg) { conn.Send(new NotificationCommand(msg)); }

		//Local version
		public static void Notice(string msg) { ModBase.ShowNotification(msg); }
	}
}
