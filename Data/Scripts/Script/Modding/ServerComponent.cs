/*
* Levitator Mod Server Component
* 
* Server-side logic goes here.
*
* We spawn a NetworkEndpoint and we register to listen for client connections
* We accept client commands and dispatch them to handlers
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
	public class ServerComponent : ModComponent
	{					
		public NetworkEndpoint EndPoint { get; private set; }

		public ServerComponent(ModBase sc):base(sc){}

		//So that the null version needn't initialize
		protected void Init()
		{
			EndPoint = new NetworkEndpoint(Mod.MessageId, Mod.Log);
			EndPoint.OnConnectionCompleted = OnConnectionCompleted;
		}

		public override void Dispose()
		{
			if (null != EndPoint) EndPoint.Dispose();
			base.Dispose();			
		}
	
		protected virtual void OnConnectionCompleted(Connection conn){ conn.OnDataArrival = OnDataArrival; }		

		private void OnDataArrival(Connection conn, StringPos data)
		{
			string name;	
			DispatchCommand(conn, data, out name);
		}		
	}

	public class NullServerComponent : ServerComponent
	{
		public NullServerComponent(ModBase mod) : base(mod) { }
		public override void Dispose() { }
		public override void LoadData() { }
		public override void SaveData() { }
		public override void Update() { }
	}
}
