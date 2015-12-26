/*
* Levitator's Space Engineers Mod-Creation Class
* 
* This mostly just serves to abstract away some initialization problems
* with ModAPIGateway, and it is the container for the LocalInputComponent,
* ServerComponent, and ClientComponent, each of which is concerned with
* command processing from a specific source
*
* Reuse is free as long as you attribute the author.
*
* V1.01
*
*/

using Levitator.SE.Utility;
using Sandbox.Common;
using Sandbox.ModAPI;
using System;
using VRage.ModAPI;

namespace Levitator.SE.Modding
{
	//The attribute is not inheritable, so add it to your subclass
	//[MySessionComponentDescriptor(MyUpdateOrder.X)]
	public abstract class ModBase : MySessionComponentBase
	{
		protected bool Initialized;
		protected bool Fatal;
		
		public DeferredTaskQueue DeferredTasks { get; protected set;}

		private ServerComponent mServerComponent;
		public ServerComponent ServerComponent
		{
			get { return mServerComponent; }
			protected set { mServerComponent = value; }
		}

		private ClientComponent mClientComponent;
		public ClientComponent ClientComponent
		{
			get { return mClientComponent; }
			protected set { mClientComponent = value; }
		}

		private ModComponent mLocalInputComponent;
		public ModComponent LocalInputComponent
		{
			get { return mLocalInputComponent; }
			protected set { mLocalInputComponent = value; }
		}

		public abstract ushort MessageId { get; }

		//Implement these to simplify initialization, shutdown, loading, and the priodic updates
		protected abstract bool Initialize();         //Return true for success, false to try again on next update		
		protected abstract void Shutdown();           //Called from UnloadData()  
		protected abstract void Update();             //Called for each update of whatever kind you specified in the attribute
		protected abstract void LoadDataDefinitely();
		protected abstract void SaveDataDefinitely(); //We get called to save data before we are initialized as well
		protected abstract ClientComponent CreateClientComponent();
		protected abstract ServerComponent CreateServerComponent();
		protected abstract ModComponent CreateLocalInputComponent();
		public abstract ModLog Log { get; protected set; }

		//Append a suffix to a filename to disambiguate it from other save instances
		//Otherwise, you would have nasty things like mod settings being applied retroactively to old savegames
		//I tried to improve upon this by including the SteamID, but a dedicated server's SteamID is variable between sessions!
		public static string QualifyFilename(string name)
		{
			string tmp = name + "." + MyAPIGateway.Session.Name;
			//Spaces cause problems for some reason. Replace them with underscores. Expand existing underscores to avoid name collisions.
            tmp = tmp.Replace("_", "__");
			tmp = tmp.Replace(' ', '_');
			return tmp;
		}

		//
		//Viscera
		//		
		private void BaseInitialize()
		{
			try
			{
				if (null == MyAPIGateway.Session || null == MyAPIGateway.Utilities) return;
				Log.Init();
				DeferredTasks = new DeferredTaskQueue(Log);
				if (!Initialize()) return;

				if (MyAPIGateway.Multiplayer.IsServer)
					ServerComponent = CreateServerComponent();
				else
					ServerComponent = new NullServerComponent(this);
				
				if (MyAPIGateway.Multiplayer.IsServer && null == MyAPIGateway.Session.Player)
				{
					ClientComponent = new NullClientComponent(this);
					LocalInputComponent = new NullModComponent(this);
				}
				else
				{
					ClientComponent = CreateClientComponent();
					LocalInputComponent = CreateLocalInputComponent();
				}

				Initialized = true;
				LoadData();				
			}
			catch (Exception x)
			{
				Fatal = true;
				Log.Log("ModBase.BaseInitialize(): Failed to start", x);
				BaseShutdown();
				return;
			}
		}

		private void BaseShutdown()
		{
			Shutdown();
			Util.DisposeIfSet(ref mLocalInputComponent);
			Util.DisposeIfSet(ref mClientComponent);
			Util.DisposeIfSet(ref mServerComponent);
			DeferredTasks = null;
			if (Log != null) Log.Dispose();
			Initialized = false;
			
		}

		public void DoPolling()
		{
			try
			{
				if (!Initialized && !Fatal)
					BaseInitialize();

				if (Initialized)
				{
					DeferredTasks.Poll();
					ServerComponent.Update();
					ClientComponent.Update();
					LocalInputComponent.Update();
					Update();
				}
			}
			catch (Exception x)
			{
				Log.Log("ModBase.DoPolling()", x);
			}
		}

		public override void LoadData()
		{
			try
			{
				if (Initialized)
				{
					LoadDataDefinitely();
					ServerComponent.LoadData();
					ClientComponent.LoadData();
					LocalInputComponent.LoadData();
				}
			}
			catch (Exception x)
			{
				Log.Log("ModBase.LoadData()", x);
			}
			base.LoadData();
		}

		public override void SaveData()
		{
			try
			{
				if (Initialized)
				{
					SaveDataDefinitely();
					ServerComponent.SaveData();
					ClientComponent.SaveData();
				}
			}
			catch (Exception x)
			{
				Log.Log("ModBase.SaveData()", x);
			}
			base.SaveData();
		}

		protected override void UnloadData()
		{
			try
			{
				BaseShutdown();
			}
			catch (Exception x)
			{
				Log.Log("ModBase.UnloadData()", x);
			}
			base.UnloadData();
		}

		public override void UpdateBeforeSimulation()
		{
			DoPolling();
			base.UpdateBeforeSimulation();
		}

		public override void Simulate()
		{
			DoPolling();
			base.Simulate();
		}

		public override void UpdateAfterSimulation()
		{
			DoPolling();
			base.UpdateAfterSimulation();
		}

		//Calling ShowNotifaction() at the wrong time can silently crash the game.
		//We hope that by waiting for session to be valid, we avoid that
		public static void ShowNotification(string msg, MyFontEnum font = MyFontEnum.White)
		{
			if (null != MyAPIGateway.Utilities && null != MyAPIGateway.Session && null != MyAPIGateway.Session.Player)				
					MyAPIGateway.Utilities.ShowNotification(msg, 10000, font);							
		}
	}

	//Utility functions for mod development
	static class ModUtil
	{
		public static IMyEntity FindPhysics(IMyEntity ent)
		{
			while (null != ent && null == ent.Physics) { ent = ent.Parent; }
			return ent;
		}
	}	
}
