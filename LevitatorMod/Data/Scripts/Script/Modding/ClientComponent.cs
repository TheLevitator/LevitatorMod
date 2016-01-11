/*
* Levitator Mod Client Component
* 
* Consume network messages from the server and dispatch them to the appropriate handler registerd by a ModModule
*
* Reuse is free as long as you attribute the author.
*
* V1.00
*
*/
using Levitator.SE.Network;
using Levitator.SE.Utility;
using Scripts.Modding.Modules.CommonClient;

namespace Levitator.SE.Modding
{
	public class ClientComponent : ModComponent
	{		
		NetworkEndpoint EndPoint;
		public new Connection ServerConnection;

		public ClientComponent(ModBase sc):base(sc){}

		//This is broken out so that the null component can decline to initialize
		protected void Init()
		{			
			EndPoint = new NetworkEndpoint(Mod.MessageId, Mod.Log);
			ServerConnection = EndPoint.Open(Destination.Server());
			ServerConnection.OnDataArrival = OnDataArrival;

			RegisterModule(CommonClient.Name, CommonClient.New);
			LoadModule(CommonClient.Name);  //Always load CommonClient because it is our bootstrap		
		}

		public override void Dispose(){
			base.Dispose();
			Util.DisposeIfSet(ref EndPoint);
		}

		public override ModModule LoadModule(string name)
		{
			Mod.Log.Log(string.Format("Loading client module '{0}'", name));
			return base.LoadModule(name);
		}

		public override void UnloadModule(string name)
		{
			Mod.Log.Log(string.Format("Unloading client module '{0}'", name));
			base.UnloadModule(name);
		}

		private void OnDataArrival(Connection conn, StringPos pos)
		{
			string name;					
			if (!DispatchCommand(conn, pos, out name))
				Mod.Log.Log("Unrecognized server message: " + name);
		}
	}	
}
