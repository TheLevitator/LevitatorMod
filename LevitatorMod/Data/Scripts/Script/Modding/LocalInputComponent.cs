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
using Levitator.SE.Serialization;
using Levitator.SE.Modding.Modules.CommonLocal;

namespace Levitator.SE.Modding
{
	public class LocalInputComponent : ModComponent
	{
		//Leading character we look for to decide if something might be a command
		public const char Prefix = '/';

		public LocalInputComponent(ModBase mod) : base(mod) {
			RegisterModule(CommonLocal.Name, CommonLocal.New);
			LoadModule(CommonLocal.Name); //Always needed to bootstrap
			MyAPIGateway.Utilities.MessageEntered += HandleChatInput;
		}
		public override void Dispose() { MyAPIGateway.Utilities.MessageEntered -= HandleChatInput; }

		public override ModModule LoadModule(string name)
		{
			Mod.Log.Log(string.Format("Loading local input module '{0}'", name));
			return base.LoadModule(name);
		}

		public override void UnloadModule(string name)
		{
			Mod.Log.Log(string.Format("Unloading local input module '{0}'", name));
			base.UnloadModule(name);
		}

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

				try
				{
					if (DispatchCommand(null, new StringPos(parsable), out name))
						send = false;
				}
				catch (ParseException pe) {
					ModBase.ShowNotification("Syntax error: " + pe.Message);										
				}
			}
			catch (Exception x)
			{
				Mod.Log.Log("Unexpected exception in HandleChatInput()", x);
			}
		}
	}
}
