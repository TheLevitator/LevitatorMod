/*
* Levitator Mod Client Component
*  
* Here we just print a couple of notifications, and more importantly, 
* load the CommonClient module
*
* Copyright 2015 Levitator
* Reuse is free if you attribute the author
*
* V1.00
*
*/

using Levitator.SE.LevitatorMod.Modules;
using Levitator.SE.Modding;
using Scripts.Modding.Modules.CommonClient;

namespace Levitator.SE.LevitatorMod
{
	class LMClient : ClientComponent
	{		
		public LMClient(LevitatorMod mod) : base(mod)
		{
			Mod.Log.Log("Client starting", false);
			Init();
			
			RegisterModule(NHBCClient.Name, NHBCClient.New);				
		}

		public override void Dispose()
		{
			base.Dispose();
			Mod.Log.Log("Client halted", false);
		}
	}
}
