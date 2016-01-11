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
using Scripts.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Levitator.SE.Modding
{
	class ModuleException : Exception { public ModuleException(string message) : base(message) { } }
	class ModuleUnknown : ModuleException
	{
		public ModuleUnknown(string name) : base("'" + name + "' is not a valid module") {}
	}

	class ModuleDuplicate : ModuleException
	{
		public ModuleDuplicate(string name) : base("'" + name + "' is already loaded") {}
	}

	class ModuleNotLoaded : ModuleException
	{
		public ModuleNotLoaded(string name) : base("'" + name + "' is not loaded") {}
	}

	public class ModuleRegistry
	{
		public delegate ModModule ModuleConstructor(ModComponent component);
		private readonly Dictionary<string, ModModule> Loaded = new Dictionary<string, ModModule>();
		private readonly Dictionary<string, ModuleConstructor> Registered = new Dictionary<string, ModuleConstructor>();
		private ModComponent ModComponent;

		public ModuleRegistry(ModComponent component) { ModComponent = component; }

		public void Register(string name, ModuleConstructor construct){ Registered.Add(name, construct); }
		public ModModule Load(string name){
			ModModule existing;
			ModuleConstructor constructor;

			//Currently no duplicate modules
			Loaded.TryGetValue(name, out existing);
			if (null != existing) throw new ModuleDuplicate(name);

			Registered.TryGetValue(name, out constructor);
			if (null == constructor) throw new ModuleUnknown(name);
			else
			{
				var module = constructor(ModComponent);
				Loaded.Add(name, module);
				return module;
			}
		}

		//Be sure that the module is loaded before calling this
		public void Unload(string name) {
			ModModule module = Loaded[name];									
			Loaded.Remove(name);
			module.Dispose();
		}

		public ModModule Get(string name) {
			ModModule result;
			Loaded.TryGetValue(name, out result);
			return result;
		}

		public IEnumerable<ModModule> LoadedModules { get { return Loaded.Values; } }
		public IEnumerable<string> LoadedNames { get { return Loaded.Keys; } }
		public IEnumerable<string> RegisteredNames { get { return Registered.Keys; } }
	}

	//Stuff common to server and client
	public class ModComponent : IDisposable
	{
		protected readonly ModuleRegistry Modules;
		private readonly CommandRegistry Commands = new CommandRegistry();
		private readonly HashSet<ModModule> ToUpdate = new HashSet<ModModule>();
		public readonly ModBase Mod;

		protected ModComponent(ModBase mod) {
			Mod = mod;
			Modules = new ModuleRegistry(this);
		}

		public virtual void Dispose() {
			string name;
			while (null != (name = Modules.LoadedNames.FirstOrDefault()))
			{
				UnloadModule(name);
			}						
		}
		public virtual void LoadData() { Util.ForEach(Modules.LoadedModules, m => m.LoadData()); }
		public virtual void SaveData() { Util.ForEach(Modules.LoadedModules, m => m.SaveData()); }
		public virtual void Update() { Util.ForEach(Modules.LoadedModules, m => m.Update()); }

		public void RegisterModule(string name, ModuleRegistry.ModuleConstructor constructor){ Modules.Register(name, constructor);	}

		public virtual ModModule LoadModule(string name) {			
			var module = Modules.Load(name);			
			Util.ForEach(module.GetCommands(), Commands.Add);
			return module;				
		}

		public virtual void UnloadModule(string name) {			
			var module = Modules.Get(name);

			if (null == module) throw new ModuleNotLoaded(name);
			else
			{				
				Util.ForEach(module.GetCommands().Keys, key => Commands.Remove(key));
				Modules.Unload(name);				
			}
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

		//Convenience function
		public Connection ServerConnection { get { return Mod.ClientComponent.ServerConnection; } }
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
