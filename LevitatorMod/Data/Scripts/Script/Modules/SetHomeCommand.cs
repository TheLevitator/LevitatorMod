using Levitator.SE.Modding;
using Levitator.SE.Serialization;
using Levitator.SE.Utility;
using Sandbox.ModAPI.Ingame;

namespace Levitator.SE.LevitatorMod.Modules
{
	public class SetHomeMessage : Command
	{
		public const string Id = "home";
		public override string GetId() { return Id; }
		public EntityRef Home;

		public SetHomeMessage(IMyMedicalRoom home) { Home.Ref = home; }
		public SetHomeMessage(ObjectParser parser) { Home = parser.Parse(EntityRef.New); }

		public override void Serialize(ObjectSerializer ser)
		{
			base.Serialize(ser);
			ser.Write(Home);
		}
	}
}

