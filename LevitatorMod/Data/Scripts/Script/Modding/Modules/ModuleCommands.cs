/*
* Levitator Mod Module Commands
*  
* Datagram definitions for loading and unloading of Levitator Mod modules
*
* Copyright 2015 Levitator
* Reuse is free if you attribute the author
*
*/

using Levitator.SE.Serialization;
using Levitator.SE.Network;
using Scripts.Modding.Modules.CommonClient;
using Sandbox.ModAPI;

namespace Levitator.SE.Modding.Modules
{
	public abstract class ModuleCommand : Command
	{
		public string ModuleName;

		public ModuleCommand(string name) { ModuleName = name; }
		public ModuleCommand(ObjectParser parser) { ModuleName = parser.ParseField(); }

		public override void Serialize(ObjectSerializer ser)
		{
			base.Serialize(ser);
			ser.Write(ModuleName);
		}
	}

	public class LoadModuleCommand : ModuleCommand
	{
		public const string ClassId = "insmod";
		public override string GetId(){ return ClassId;	}

		public LoadModuleCommand(ObjectParser parser):base(parser) { }
		public LoadModuleCommand(string name) : base(name) { }

		public static void HandleOnClient(ModModule module, Connection conn, ObjectParser parser)
		{			
			if (conn != module.ServerConnection)
			{
				module.Log.Log("Spurious module load from: " + ModLog.DestinationString(conn.Destination), true);
				return;
			}

			//Server knows best
			var msg = new LoadModuleCommand(parser);
			var newmod = module.Component.LoadModule(msg.ModuleName);
			newmod.LoadData();
		}

		public static void HandleOnServer(ModModule module, Connection conn, ObjectParser parser)
		{
			ModModule newmod;
			if (!conn.Destination.IsAdmin)
			{
				NotificationCommand.Notice(conn, "You are not a server admin");
				module.Log.Log("Unauthorized module load request from player: " + ModLog.DestinationString(conn.Destination));
				return;
			}
			else
			{				
				var msg = new LoadModuleCommand(parser);
				module.Log.Log(string.Format("Module load '{0}' by {1}", msg.ModuleName, ModLog.DestinationString(conn.Destination)));

				try
				{
					newmod = module.Component.LoadModule(msg.ModuleName);
				}
				catch (ModuleException x)
				{
					module.Log.Log("Module load failed: " + x.Message);
					NotificationCommand.Notice(conn, "Failed: " + x.Message);
					return;
				}
				newmod.LoadData();	
				NotificationCommand.Notice(conn, "Loaded.");
			}
		}
	}
	
	//We do not save module state on unload. If you want to save your module state, save the world first, then unload.
	public class UnloadModuleCommand : ModuleCommand
	{
		public const string ClassId = "rmmod";
		public override string GetId() { return ClassId; }

		public UnloadModuleCommand(ObjectParser parser) : base(parser) { }
		public UnloadModuleCommand(string name) : base(name) { }

		public static void HandleOnClient(ModModule module, Connection conn, ObjectParser parser)
		{
			if (conn != module.ServerConnection)
			{
				module.Log.Log("Spurious module unload from: " + ModLog.DestinationString(conn.Destination), true);
				return;
			}

			//Server knows best
			var msg = new UnloadModuleCommand(parser);
			module.Component.UnloadModule(msg.ModuleName);
		}

		public static void HandleOnServer(ModModule module, Connection conn, ObjectParser parser)
		{
			if (!conn.Destination.IsAdmin)
			{
				NotificationCommand.Notice(conn, "You are not a server admin");
				module.Log.Log("Unauthorized module unload request from player: " + ModLog.DestinationString(conn.Destination));
				return;
			}
			else
			{
				var msg = new UnloadModuleCommand(parser);
				if (msg.ModuleName == CommonServer.CommonServer.Name)
				{
					//This keeps us from thinking about dependencies for the time being
					//You would be locked out of the mod until restart if you unloaded the bootstrap anyway
					NotificationCommand.Notice(conn, "You must remove " + CommonServer.CommonServer.Name + " from the mod list and restart the server to unload it");
					return;
				}

				try
				{
					module.Log.Log(string.Format("Module unload '{0}' by {1}", msg.ModuleName, ModLog.DestinationString(conn.Destination)));					
					module.Component.UnloadModule(msg.ModuleName);
				}
				catch (ModuleException x)
				{
					module.Log.Log("Module unload failed: " + x.Message);
					NotificationCommand.Notice(conn, "Failed: " + x.Message);					
					return;
				}				
				NotificationCommand.Notice(conn, "Unloaded.");				
			}
		}
	}
}
