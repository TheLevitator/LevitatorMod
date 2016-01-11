/*
*
* This code is borrowed directly from midspace's Admin Helper mod:
* https://github.com/midspace/Space-Engineers-Admin-script-mod/tree/master/midspace%20admin%20helper
*
* Many thanks.
*
*/

using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System.Linq;

namespace Levitator.SE.Modding
{
	public static class Midspace
	{
		public static bool IsAdmin(IMyPlayer player)
		{
			// Offline mode. You are the only player.
			if (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE)
			{
				return true;
			}

			// Hosted game, and the player is hosting the server.
			if (player.IsHost())
			{
				return true;
			}

			// determine if client is admin of Dedicated server.
			var clients = MyAPIGateway.Session.GetCheckpoint("null").Clients;
			if (clients != null)
			{
				var client = clients.FirstOrDefault(c => c.SteamId == player.SteamUserId && c.IsAdmin);
				return client != null;
				// If user is not in the list, automatically assume they are not an Admin.
			}

			// clients is null when it's not a dedicated server.
			// Otherwise Treat everyone as Normal Player.

			return false;
		}

		public static bool IsHost(this IMyPlayer player)
		{
			return MyAPIGateway.Multiplayer.IsServerPlayer(player.Client);
		}
	}
}


