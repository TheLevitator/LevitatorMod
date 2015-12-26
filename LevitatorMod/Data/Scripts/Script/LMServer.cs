/*
* Levitator Mod
* Server Component Class
*
* We log some notifications and register server-side modules
* which contain the server-side datagram handlers
*
* Reuse is free as long as you attribute the author.
*
* V1.0
*
*/

using Sandbox.ModAPI;
using Levitator.SE.Modding;
using Levitator.SE.Network;
using Scripts.Modding.Modules.CommonServer;
using Levitator.SE.LevitatorMod.Modules;

namespace Levitator.SE.LevitatorMod
{
	class LMServer : ServerComponent
	{
		public LMServer(LevitatorMod mod) : base(mod)
		{
			Mod.Log.Log("Service starting", false);
			Mod.Log.Log("MyId: " + MyAPIGateway.Multiplayer.MyId + " ServerID: " + MyAPIGateway.Multiplayer.ServerId, false);

			Init();
			RegisterModule(new CommonServer(this));
			RegisterModule(new NHBCServer(this));
		}

		public override void Dispose()
		{
			base.Dispose();
			Mod.Log.Log("Service halted", true);
		}

		protected override void OnConnectionCompleted(Connection conn)
		{
			base.OnConnectionCompleted(conn);
			Mod.Log.Log("Connection established: " + conn.Destination.GetPlayer().DisplayName, false);
		}
	}
}
