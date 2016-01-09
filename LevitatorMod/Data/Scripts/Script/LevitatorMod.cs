/*
* Levitator Mod Entry Points
* 
* Levitator mod will be a framework for managing and customizing
* Space Engineers.
*
* Entry points, initialization, shutdown, state loading/saving go here
*
* Reuse is free as long as you attribute the author.
*
*
*/
using Sandbox.Common;
using Levitator.SE.Modding;
using Levitator.SE.Utility;
using System;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;

namespace Levitator.SE.LevitatorMod
{
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	public class LevitatorMod : ModBase, ISingleton {
		
		//Config
		public const string Name = "LevitatorMod";
		public const string Version = "1.0.4";
		public override ushort MessageId{ get { return 0xBEEF; } }
		private static ModLog mLog = new ModLog("log.txt", typeof(LevitatorMod), Name);

		Singleton<GlobalBlockAppendedBroadcaster<IMyInventoryOwner>>.Ref TerminalBlockAddedBroadcaster;

		public override ModLog Log
		{
			get { return mLog; }
			protected set { mLog = value; }
		}

		public override bool IsRequiredByGame { get { return true; } }
		
		public LevitatorMod()
		{
			Log.Log(Name + " created", false);

			try
			{
				Singleton<LevitatorMod>.Set(this);
			}
			catch (Exception x)
			{
				Log.Log("Duplicate mod instance!", x);
				Fatal = true;				
			}
		}

		public void SingletonDispose(){ base.Dispose(); }

		protected override bool Initialize()
		{
			Log.Log("v" + Version + " Intializing", false);

			TerminalBlockAddedBroadcaster = Singleton.Get(GlobalBlockAppendedBroadcaster<IMyInventoryOwner>.New);
			TerminalBlockAddedBroadcaster.Instance.Event.Add(OnTerminalBlockAdded);

			return true;
		}

		protected override ServerComponent CreateServerComponent() { return new LMServer(this); }
		protected override ClientComponent CreateClientComponent() { return new LMClient(this); }
		protected override ModComponent CreateLocalInputComponent() { return new LMLocal(this); }

		protected override void Shutdown()
		{
			Log.Log("Shutting down", false);
			if (null != TerminalBlockAddedBroadcaster)
			{
				TerminalBlockAddedBroadcaster.Instance.Event.Remove(OnTerminalBlockAdded);
				TerminalBlockAddedBroadcaster.Dispose();
				TerminalBlockAddedBroadcaster = null;
			}			
		}

		protected override void Update() { }
		protected override void LoadDataDefinitely() { }
		protected override void SaveDataDefinitely() { }

		//Here, we delete all of the inventory from new blocks because the game is including the blueprinted inventory of projected blocks when the block is spawned
		//If we ever wind up in a situation where spawning an individual block with inventory is valid in Survival, this will break that, but for now, it fixes a bug.
		//Note that IMyInventoryOwner is marked as deprecated, but is used by the game internally anyway, and if you do not check for it, you will get a cast exception
		private void OnTerminalBlockAdded(IMyInventoryOwner owner)
		{
			try
			{
				var block = owner as Sandbox.ModAPI.IMyTerminalBlock;
				if (null == block || !block.HasInventory()) return;

				var count = block.GetInventoryCount();
				Sandbox.ModAPI.IMyInventory inv;
				for (int i = count - 1; i >= 0; --i)
				{
					inv = block.GetInventory(i) as Sandbox.ModAPI.IMyInventory;
					if (null != inv) inv.Clear();
				}
			}
			catch (Exception x)
			{
				Log.Log("TerminalBlockAdded()", x);
			}
		}
	}
}
