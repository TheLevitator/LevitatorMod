using Levitator.SE.Modding;
using Levitator.SE.Utility;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;

namespace Levitator.SE.LevitatorMod
{
	public class ProjectorCheatFix : IDisposable
	{
		ModBase Mod;
		Singleton<GlobalBlockAppendedBroadcaster<IMyInventoryOwner>>.Ref TerminalBlockAppendedBroadcaster;

		public ProjectorCheatFix(ModBase mod)
		{
			Mod = mod;
			TerminalBlockAppendedBroadcaster = Singleton.Get(GlobalBlockAppendedBroadcaster<IMyInventoryOwner>.New);
			TerminalBlockAppendedBroadcaster.Instance.Event.Add(OnTerminalBlockAppended);
		}

		public void Dispose()
		{			
			TerminalBlockAppendedBroadcaster.Instance.Event.Remove(OnTerminalBlockAppended);
			TerminalBlockAppendedBroadcaster.Dispose();			
		}

		//Here, we delete all of the inventory from new blocks because the game is including the blueprinted inventory of projected blocks when the block is spawned
		//If we ever wind up in a situation where spawning an individual block with inventory is valid in Survival, this will break that, but for now, it fixes a bug.
		//Note that IMyInventoryOwner is marked as deprecated, but is used by the game internally anyway, and if you do not check for it, you will get a cast exception
		private void OnTerminalBlockAppended(IMyInventoryOwner owner)
		{
			if (!MyAPIGateway.Session.SurvivalMode) return; //Cheat all you want in Creative Mode. That's the point.
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
				Mod.Log.Log("TerminalBlockAdded()", x);
			}
		}
	}
}
