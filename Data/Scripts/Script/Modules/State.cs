/*
* Levitator Mod No Hale Bop Cult Module
* State Classes
* 
* Stuff we persist to/from disk in order to remember how to properly torment people
* for their morbid tendencies
*
* We use XML serialization because it's simpler and we do not have network bandwidth limitations
*
* V1.00
*
*/

using Levitator.SE.Utility;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace Levitator.SE.LevitatorMod.Modules
{

	[System.Serializable]
	public class PlayerData
	{
		public int Graces;
		public EntityRef MedicalRoom;
		public Vector3 Position;
		public Vector3 Forward;
		public Vector3 Up;

		public PlayerData() { }

		public void SetHome(MatrixD relpos, IMyMedicalRoom home)
		{
			MedicalRoom.Ref = home;
			Position = relpos.Translation;
			Forward = relpos.Forward;
			Up = relpos.Up;
			MedicalRoom.Ref = home;
		}
	}

	class HomeDictionary : Dictionary<ulong, PlayerData>
	{
		public HomeDictionary() { }
		public HomeDictionary(Dictionary<ulong, PlayerData> dict) : base(dict) { }

		public PlayerData this[IMyPlayer player]
		{
			get
			{
				PlayerData data = null;
				TryGetValue(player.SteamUserId, out data);
				if (null == data)
				{
					data = new PlayerData();
					this[player] = data;
				}
				return data;
			}
			set { this[player.SteamUserId] = value; }
		}
	}
}
