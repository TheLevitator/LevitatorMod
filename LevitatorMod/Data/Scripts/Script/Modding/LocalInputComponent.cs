/*
* Levitator's Space Engineers Modding Library
* Local Input Component Class
* 
* Here we register with the game engine to listen for chat events
* If a chat message corresponds with a registered command name
* then we dispatch it to a handler
*
* Reuse is free as long as you attribute the author.
*
* V1.00
*
*/

using System;
using System.Text;
using Sandbox.ModAPI;
using Levitator.SE.Utility;

namespace Levitator.SE.Modding
{
	public class LocalInputComponent : ModComponent
	{
		//Leading character we look for to decide if something might be a command
		public const char Prefix = '/';

		public LocalInputComponent(ModBase mod) : base(mod) { MyAPIGateway.Utilities.MessageEntered += HandleChatInput; }
		public override void Dispose() { MyAPIGateway.Utilities.MessageEntered -= HandleChatInput; }

		private StringBuilder ChatSB = new StringBuilder(128);
		private void HandleChatInput(string msg, ref bool send)
		{
			try
			{
				if (msg[0] != Prefix) return;

				//Reuse the serialization parser to interpret the user's command string								
				ChatSB.Clear();
				ChatSB.Append('{');
				ChatSB.Append(msg, 1, msg.Length - 1);
				ChatSB.Append('}');

				string parsable = ChatSB.ToString();
				string name;
				if (DispatchCommand(null, new StringPos(parsable), out name))
					send = false;
			}
			catch (Exception x)
			{
				Mod.Log.Log("Unexpected exception in HandleChatInput()", x);
			}
		}
	}
}
