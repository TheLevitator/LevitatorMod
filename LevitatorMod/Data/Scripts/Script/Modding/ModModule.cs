/*
* Levitator's Space Engineers Modding Library
* Mod Module Class
*
* The mod module is in charge of grouping together a set of related commands,
* registering and unregistering their event handlers as a group, 
* and maintaining their state if they are stateful.
* Derived classes implement event handlers for stateful commands.
*
* Reuse is free as long as you attribute the author.
*
* V1.0
*
*/

using System;
using System.Collections.Generic;
using Levitator.SE.Network;
using Levitator.SE.Serialization;
using Levitator.SE.Modding.Modules.CommonServer;

namespace Levitator.SE.Modding
{
	public abstract class ModModule : IDisposable
	{
		private static readonly CommandRegistry NullCommandList = new CommandRegistry();
		private static readonly List<string> NullStringList = new List<string>(0);
		public readonly ModComponent Component;
		protected ModModule(ModComponent comp) { Component = comp; }

		public virtual void LoadData() { }		
		public virtual void SaveData() { }
		public virtual CommandRegistry GetCommands() { return NullCommandList; }
		public virtual List<string> GetClientDependencies(){ return NullStringList; }
		public virtual void Update() { }
		public virtual void Dispose(){ SaveData(); }

		//
		//Convenience functions for use within module definitions
		//		
		public Connection ServerConnection { get { return Component.Mod.ClientComponent.ServerConnection; } }

		public ModLog Log { get { return Component.Mod.Log; } }

		public void ForwardToServer(Connection source, ObjectParser parser)
		{
			//This string position is a safe assumption only because it's how we construct them in the message input event handler
			Component.Mod.ClientComponent.ServerConnection.Send(new UserCommand(parser.Pos.String), true);
		}
	}

	public class CommandRegistry : Dictionary<string, Action<Connection, ObjectParser>>
	{
		public new Action<Connection, ObjectParser> this [string name]
		{
			get
			{
				Action<Connection, ObjectParser> action = null;
				TryGetValue(name, out action);
				return action;
			}
		}

		public void Add(KeyValuePair<string, Action<Connection, ObjectParser>> kvp) { Add(kvp.Key, kvp.Value); }
	}

	public abstract class Command : Serializable
	{
		public abstract string GetId();

		//Note that this is asymmetrical because we write the command Id here
		//But it is read outside the class on account of chicken/egg
		public virtual void Serialize(ObjectSerializer ser) { ser.Write(GetId()); }
	}


	//An adaptor functor for registering handlers declared inside message class definitions
	//When we have a stateful message, our handler is defined in the ModModule subclass and it is Action<Connection, ObjectParser>
	//When we have a stateless message our handler is defined in the message class and it is Action<ModModule, Connection, ObjectParser>
	//The distinction is so that we can define handlers within the ModModule or in the message class, depending on where they make more sense
	//It would be nice to move all handlers into the message definition, but then we would have to make private state variables in the
	//ModModule subclass public and downcast in handlers which require the derived class, and that is even worse
	public struct Stateless
	{
		private ModModule Module;
		private Action<ModModule, Connection, ObjectParser> Action;
		public Stateless(ModModule module, Action<ModModule, Connection, ObjectParser> handler)
		{
			Action = handler;
			Module = module;
		}
		public void Handler(Connection conn, ObjectParser parser) { Action(Module, conn, parser); }
	}
}
