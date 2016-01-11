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
		public const string Version = "1.0.5";		
		public override ushort MessageId{ get { return 0xBEEF; } }
		private static ModLog mLog = new ModLog("log.txt", typeof(LevitatorMod), Name);

		private ProjectorCheatFix ProjectorCheatFix;
		
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
			ProjectorCheatFix = new ProjectorCheatFix(this);
			return true;
		}

		protected override ServerComponent CreateServerComponent() { return new LMServer(this); }
		protected override ClientComponent CreateClientComponent() { return new LMClient(this); }
		protected override ModComponent CreateLocalInputComponent() { return new LMLocal(this); }

		protected override void Shutdown()
		{
			Log.Log("Shutting down", false);
			ProjectorCheatFix.Dispose();
		}

		protected override void Update() { }
		protected override void LoadDataDefinitely() { }
		protected override void SaveDataDefinitely() { }
	}
}
