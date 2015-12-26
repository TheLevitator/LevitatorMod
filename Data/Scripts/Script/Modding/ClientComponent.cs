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

namespace Levitator.SE.Modding
{
	public class ClientComponent : ModComponent
	{		
		NetworkEndpoint EndPoint;
		public Connection ServerConnection;

		public ClientComponent(ModBase sc):base(sc){}

		//This is broken out so that the null component can decline to initialize
		protected void Init()
		{			
			EndPoint = new NetworkEndpoint(Mod.MessageId, Mod.Log);
			ServerConnection = EndPoint.Open(Destination.Server());
			ServerConnection.OnDataArrival = OnDataArrival;
		}

		public override void Dispose(){
			base.Dispose();
			Utility.Util.DisposeIfSet(ref EndPoint);
		}

		private void OnDataArrival(Connection conn, StringPos pos)
		{
			string name;					
			if (!DispatchCommand(null, pos, out name))
				Mod.Log.Log("Unrecognized server message: " + name);
		}
	}	
}
