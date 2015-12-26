/*
* Levitator's Space Engineers Modding Library
* ModComponent Class
*
* This is basically a container in which to register and dispatch handlers for commands.
* This class is subclassed three different ways; to support input from the local user,
* handle datagrams from the server, and handle datagrams from the client
*
* Copyright 2015 Levitator
* Reuse is free if you attribute the author
*
* V1.00
*
*/

using Levitator.SE.Network;
using Levitator.SE.Serialization;
using Levitator.SE.Utility;
using System;
using System.Collections.Generic;

namespace Levitator.SE.Modding
{
	//Stuff common to server and client
	public class ModComponent : IDisposable
	{
		private List<ModModule> Modules = new List<ModModule>();
		private CommandRegistry Commands = new CommandRegistry();
		private HashSet<ModModule> ToUpdate = new HashSet<ModModule>();
		public readonly ModBase Mod;

		protected ModComponent(ModBase mod) { Mod = mod; }

		public virtual void Dispose() { while (Modules.Count > 0) { Modules[0].Dispose(); } }
		//public virtual void LoadData() { foreach (var m in Modules) { m.LoadData(); } }
		public virtual void LoadData() { Util.ForEach(Modules, m => m.LoadData()); }
		//public virtual void SaveData() { foreach (var m in Modules) { m.SaveData(); } }
		public virtual void SaveData() { Util.ForEach(Modules, m => m.SaveData() ); }
		//public virtual void Update() { foreach (var m in Modules) { m.Update(); } }
		public virtual void Update() { Util.ForEach(Modules, m => m.Update() ); }

		public void RegisterModule(ModModule module)
		{
			Modules.Add(module);
			//foreach (var command in module.GetCommands()) { Commands.Add(command); }
			Util.ForEach(module.GetCommands(), Commands.Add);
		}
		
		public void UnregisterModule(ModModule module)
		{
			//foreach (var key in module.GetCommands().Keys) { Commands.Remove(key); }
			Util.ForEach(module.GetCommands().Keys, key => Commands.Remove(key));
			Modules.Remove(module);
			ToUpdate.Remove(module);
		}

		public void RegisterForUpdates(ModModule module) { ToUpdate.Add(module); }
		public void UnregisterForUpdates(ModModule module) { ToUpdate.Remove(module); }

		public bool DispatchCommand(Connection connection, StringPos data, out string name)
		{
			var parser = new ObjectParser(data);
			name = parser.ParseField();
			var action = Commands[name];

			if (null != action)
			{
				action(connection, parser);
				return true;
			}
			else
				return false;
		}
	}

	//Stubs for the client instance
	public class NullModComponent : ModComponent
	{
		public NullModComponent(ModBase mod) : base(mod) { }
		public override void Dispose() { }
		public override void LoadData() { }
		public override void SaveData() { }
		public override void Update() { }
	}

	public class NullClientComponent : ClientComponent
	{
		public NullClientComponent(ModBase mod) : base(mod) { }
		public override void Dispose() { }
		public override void LoadData() { }
		public override void SaveData() { }
		public override void Update() { }
	}
}
