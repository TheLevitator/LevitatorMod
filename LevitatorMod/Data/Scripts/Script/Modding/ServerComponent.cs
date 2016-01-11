/*
* Levitator Mod Server Component
* 
* Server-side logic goes here.
*
* We spawn a NetworkEndpoint and we register to listen for client connections
* We accept client commands and dispatch them to handlers
* 
* Reuse is free as long as you attribute the author.
*
* V1.00
*
*/

using Levitator.SE.Modding.Modules;
using Levitator.SE.Modding.Modules.CommonServer;
using Levitator.SE.Network;
using Levitator.SE.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Levitator.SE.Modding
{
	public class ServerComponent : ModComponent
	{		
		public NetworkEndpoint Endpoint { get; private set; }
		protected ConfigFile<ModuleConfig> ModuleConfigFile;

		public ServerComponent(ModBase sc):base(sc){}

		//So that the null version needn't initialize
		protected void Init()
		{
			Endpoint = new NetworkEndpoint(Mod.MessageId, Mod.Log);
			Endpoint.OnConnectionCompleted = OnConnectionCompleted;
			RegisterModule(CommonServer.Name, CommonServer.New);
		}

		public override void LoadData()
		{
			try
			{
				ModuleConfigFile = new ConfigFile<ModuleConfig>(Mod.ModuleConfigPath, GetType());
			}
			catch (Exception x)
			{
				Mod.Log.Log("Error loading module config", x);
			}

			if (!ModuleConfigFile.Valid)
				Mod.Log.Log(string.Format("{0} could not be loaded. It will not be saved.", ModuleConfigFile.QualifiedName), true);

			//Enable everything by default. It's awesome, or it wouldn't be in the mod.
			if (null == ModuleConfigFile.Data)
			{
				Mod.Log.Log("Default module config");
				ModuleConfigFile.Data = new ModuleConfig();
				ModuleConfigFile.Data.Modules = new List<string>(Modules.RegisteredNames);
			}

			var ModulesAddQuery = ModuleConfigFile.Data.Modules.Except(Modules.LoadedNames);
			var ModulesRemoveQuery = Modules.LoadedNames.Except(ModuleConfigFile.Data.Modules);

			Util.ForEach(ModulesAddQuery, name => LoadModule(name));
			Util.ForEach(ModulesRemoveQuery, name => UnloadModule(name));

			base.LoadData();
		}

		public override void SaveData()
		{
			ModuleConfigFile.Data.Modules.Clear();
			ModuleConfigFile.Data.Modules.AddRange(Modules.LoadedNames);
			ModuleConfigFile.Save();
			base.SaveData();
		}

		public override void Dispose()
		{
			if (null != Endpoint) Endpoint.Dispose();
			base.Dispose();			
		}
	
		protected virtual void OnConnectionCompleted(Connection conn){
			conn.OnDataArrival = OnDataArrival;
			Util.ForEach(Modules.LoadedModules,
				module => Util.ForEach(module.GetClientDependencies(), name => conn.Send(new LoadModuleCommand(name)))
			);				
		}

		public override ModModule LoadModule(string name)
		{
			Mod.Log.Log(string.Format("Loading server module '{0}'", name));
			var module = base.LoadModule(name);
			Util.ForEach(module.GetClientDependencies(), dep => Endpoint.Broadcast(new LoadModuleCommand(dep)));
			return module;
		}

		//TODO: We currently assume that client dependencies are never shared. We would need to reference count if they were.
		public override void UnloadModule(string name)
		{
			Mod.Log.Log(string.Format("Unloading server module '{0}'", name));
			var module = Modules.Get(name);
			if (null == module) throw new ModuleNotLoaded(name);
			
			Util.ForEach(module.GetClientDependencies(), dep => Endpoint.Broadcast(new UnloadModuleCommand(dep)));
			base.UnloadModule(name);			
		}

		private void OnDataArrival(Connection conn, StringPos data)
		{
			string name;	
			DispatchCommand(conn, data, out name);
		}		
	}

	public class NullServerComponent : ServerComponent
	{
		public NullServerComponent(ModBase mod) : base(mod) { }
		public override void Dispose() { }
		public override void LoadData() { }
		public override void SaveData() { }
		public override void Update() { }
	}
}
