/*
* Levitator Mod No Hale Bop Cult Module
* Server Side Module
* 
* Module which forces players to spawn only at their home Medical Bay
* Because this is a game about space travel, not a UFO suicide cult
*
* V1.03
*
*/

using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRageMath;
using VRage;
using Sandbox.ModAPI.Interfaces;
using Levitator.SE.Modding;
using Levitator.SE.Utility;
using Levitator.SE.Serialization;
using Levitator.SE.Network;
using Scripts.Modding.Modules.CommonClient;
using Sandbox.Common.ObjectBuilders;

namespace Levitator.SE.LevitatorMod.Modules
{
	public class NHBCServer : ModModule
	{
		//Hard config
		public const string Name = "NHBC";
		public const string HomeDataPath = "HomeData";			//File name to store users' home data
		public const float HomeRange = 2;                       //Max distance in m to a medical room at which user can set home
		public const int GraceLimit = 5;						//Maximum number of times Herr Noob may spawn freely before NHBC rules come into effect

		//Error messages
		public static readonly string RangeError = "You must be within " + HomeRange + "m of an accessible Medical Room";
		public static readonly string CharacterError = "You must be on foot to set your spawn";

		ConfigFile<Util.SerializableDictionaryWrapper<ulong, PlayerData>> HomeDataFile;
		HomeDictionary HomeData;
		Singleton<TopLevelEntityTracker<IMyCharacter>>.Ref CharacterTracker;
		EntityTrackerSink<IMyCharacter> Characters;
			
		List<IMyCharacter> RespawnQueue = new List<IMyCharacter>((int)MyAPIGateway.Players.Count * 2);
		List<IMyCharacter> NewRespawnQueue = new List<IMyCharacter>((int)MyAPIGateway.Players.Count * 2);
		readonly List<IMyPlayer> DeadList = new List<IMyPlayer>((int)MyAPIGateway.Players.Count * 2);
		readonly HashSet<IMyCharacter> Carcasses = new HashSet<IMyCharacter>();
		
		public static BoundingSphereD GetPlayerHomeBounds(IMyPlayer player){
			return new BoundingSphereD(player.GetPosition(), HomeRange);
		}

		//A consistent way of checking whether a spawn is valid, which we can use to select a spawn on the client,
		//validate it on the server, and (then validate again at the moment of respawn where bounds are null and ignored).
		public static bool IsHomeValid(IMyPlayer player, BoundingSphereD? bb, IMyMedicalRoom room) {			
			return null != player.Controller && null != player.Controller.ControlledEntity &&  null != room && (!bb.HasValue || bb.Value.Contains(room.GetPosition()) != ContainmentType.Disjoint) 
				&& room.HasPlayerAccess(player.PlayerID) && room.IsWorking;
		}

		public NHBCServer(ModComponent mc) : base(mc)
		{			
			Log.Log("NHBC Module starting...", false);

			CharacterTracker = Singleton.Get<TopLevelEntityTracker<IMyCharacter>>(TopLevelEntityTracker<IMyCharacter>.New);
            Characters = new EntityTrackerSink<IMyCharacter>(CharacterTracker.Instance, OnCharacterAdded, OnCharacterRemoved);
			
			mc.RegisterForUpdates(this);
        }

		public static NHBCServer New(ModComponent component) { return new NHBCServer(component); }

		public override void Dispose()
		{
			Log.Log("NHBC Module exiting...");	
			Util.DisposeIfSet(ref Characters);
			Util.DisposeIfSet(ref CharacterTracker);			
			if (null != HomeData)
			{
				HomeData.Clear();
				HomeData = null;
			}
			base.Dispose();
		}

		public override CommandRegistry GetCommands()
		{
			return new CommandRegistry()
			{
				{SetHomeMessage.Id, HandleHomeCommand},			
			};
		}

		public override List<string> GetClientDependencies()
		{
			return new List<string>()
			{
				NHBCClient.Name
			};
		}

		//Dead characters being removed have no controlling player
		//private void OnCharacterRemoved(IMyCharacter obj){}

		//It's a hard life...
		private void HandleHomeless(IMyPlayer player, IMyCharacter character, string detail)
		{
			MissionScreenCommand.Show(Component.Mod.ServerComponent.Endpoint[player], "This is not a UFO suicide cult", detail + "\n" +
				"You may travel to a friendly, active medical room and select it as your home by typing '/home' in chat.", "Objective: ",
				"find a Medical Room and type '/home'");

			//This used to work, but it started messing up the external camera and setting its position permanently outside the world
			//To the void with you
			/*			
			MoveEntityCommand.MoveEntity(Component.Mod.ServerComponent, character as IMyEntity,
				new MyPositionAndOrientation(new Vector3(float.MaxValue / 4, float.MaxValue / 4, float.MaxValue / 4),
				Matrix.Identity.Forward, Matrix.Identity.Up).GetMatrix());
			*/
			
			character.Kill();
		}

		//...except for when we are nice
		private void HandleGrace(IMyPlayer player, PlayerData data)
		{
			if (null == player) Log.Log("PLAYER NULL");
			if (null == data) Log.Log("DATA NULL");
			if (null == Component) Log.Log("COMPONENT NULL");
			if (null == Component.Mod) Log.Log("MOD NULL");
			if (null == Component.Mod.ServerComponent) Log.Log("SERVER NULL");
			if (null == Component.Mod.ServerComponent.Endpoint) Log.Log("ENDPOINT NULL");

			MissionScreenCommand.Show(Component.Mod.ServerComponent.Endpoint[player], "This is not a UFO suicide cult",
					"From now on, whenever you spawn on foot you will always start at your home Medical Room unless you choose a spawn ship instead. " +
					"This is to prevent people from hitchhiking across the galaxy by suiciding. You must travel to a Medical Room and type '/home' in chat to set your home spawn.\n\n" +
					"Your remaining grace period in spawns is: " + (GraceLimit - data.Graces).ToString(), "Objective: ", "Go type '/home' near a medical room!");			
			++data.Graces;
        }

		private void CheckForRespawn(IMyCharacter character)
		{
			var charent = character as IMyEntity;
			var player = MyAPIGateway.Players.GetPlayerControllingEntity(charent);
			IMyMedicalRoom room = null;

			if (null != player && DeadList.Contains(player))
			{
				DeadList.Remove(player);				
				var pd = HomeData[player];

				if (null != pd.MedicalRoom.Ref)
				{
					if (!IsHomeValid(player, null, pd.MedicalRoom.Ref as IMyMedicalRoom))											
					{
						HandleHomeless(player, character,
							"Your home spawn is currently unavailble because it is offline. You must respawn with a spawn ship until it becomes available again or... \n\n ");
						return;
					}
					//Fall through
				}
				else
				{
					if (pd.Graces >= GraceLimit)					
						HandleHomeless(player, character, "You do not have a home spawn set. You must use spawn ships until you have a home spawn active.");					
					else
						HandleGrace(player, pd);
					return;
				}

				IMyEntity phys = ModUtil.FindPhysics(room);
				Vector3 linearV;
				Vector3 angularV;

				if (null != phys) {
					linearV = phys.Physics.LinearVelocity;
					angularV = phys.Physics.AngularVelocity;
				}
				else linearV = angularV = new Vector3(0, 0, 0);

				room = pd.MedicalRoom.Ref as IMyMedicalRoom;
				(character as IMyControllableEntity).SwitchDamping(); //Switch damping off in case the player is spawning on a moving ship?
				MoveEntityWithVelocitiesCommand.MoveEntity(Component.Mod.ServerComponent, charent, 
					new MyPositionAndOrientation(pd.Position, pd.Forward, pd.Up).GetMatrix() * room.WorldMatrix, linearV, angularV);

				//charent.SetWorldMatrix(new MyPositionAndOrientation(pd.Position, pd.Forward, pd.Up).GetMatrix() * room.WorldMatrix );
				//charent.SetPosition( (new MyPositionAndOrientation(pd.Position, pd.Forward, pd.Up).GetMatrix() * room.WorldMatrix).Translation );	
				//MoveEntityRelativeCommand.MoveEntityRelative(Component.Mod.ServerComponent, charent, room,
				//	new MyPositionAndOrientation(pd.Position, pd.Forward, pd.Up).GetMatrix());
					
				NotificationCommand.Notice(Component.Mod.ServerComponent.Endpoint[player], "Spawned at your home medbay");
				pd.Graces = GraceLimit;	//Choose wisely
			}
			else
				if(!charent.Closed) NewRespawnQueue.Add(character);
        }

		public override void Update()
		{
			if (RespawnQueue.Count == 0) return;
			NewRespawnQueue.Clear();
			Util.ForEach(RespawnQueue, CheckForRespawn);
			Util.Swap(ref RespawnQueue, ref NewRespawnQueue);
		}		

		private bool DeadPredicate(IMyPlayer player)
		{
			return null != player.Client && null != player.Controller && null == player.Controller.ControlledEntity;
		}

		//This is how we detect when corpses are attempting to set home. DENIED.
		class CarcassHandler {
			private IMyCharacter Character;
			private HashSet<IMyCharacter> Carcasses;
			public CarcassHandler(IMyCharacter character, HashSet<IMyCharacter> carcasses)
			{
				Character = character;
				Carcasses = carcasses;
				Character.OnMovementStateChanged += Handler;
				(Character as IMyEntity).OnClose += OnClose;
			}

			private void OnClose(IMyEntity ent)
			{
				Carcasses.Remove(Character);
				Character.OnMovementStateChanged -= Handler;
				ent.OnClose -= OnClose;
			}

			private void Handler(MyCharacterMovementEnum oldstate, MyCharacterMovementEnum newstate)
			{
				if (newstate == MyCharacterMovementEnum.Died)
					Carcasses.Add(Character);
				else
				{
					if(oldstate == MyCharacterMovementEnum.Died)
						Carcasses.Remove(Character); //In case the game ever adds space zombies
				}
			}
		}

		//Live characters resuming a non-dedicated session have a controlling player
		//Live character exiting cockpits have a controlling player
		//Dead players respawning do not
		//Beware that dead characters whose player is still attached to the external camera still have a controlling player for a short time until they reach the spawn screen
		private void OnCharacterAdded(IMyCharacter character)
		{
			new CarcassHandler(character, Carcasses);	
			if (null == MyAPIGateway.Players.GetPlayerControllingEntity(character as IMyEntity))
			{
				//We have to enumerate all the dead here so that when this soulless charcter receives its controller
				//we will then know if they were previously dead and now spawning, or merely climbing out of a cockpit
				DeadList.Clear();
				MyAPIGateway.Players.GetPlayers(DeadList, DeadPredicate);
                RespawnQueue.Add(character);				
			}
		}

		private void OnCharacterRemoved(IMyCharacter character){ Carcasses.Remove(character); }

		public override void LoadData()
		{
			try
			{
				LoadPlayerData();
			}
			catch (Exception x)
			{
				Log.Log(string.Format("'{0}' could not be loaded. It will not be saved", HomeDataFile.QualifiedName), x);
			}

			if (null == HomeData)
			{
				HomeData = new HomeDictionary();
				Log.Log("No player home data found. Reset.");
			}					
		}

		public override void SaveData()
		{
			if (null != HomeData) //sometimes saves before loading?
				SavePlayerData();			
		}

		private void LoadPlayerData()
		{
			HomeData = null;
			Log.Log("Loading home data");
			HomeDataFile = new ConfigFile<Util.SerializableDictionaryWrapper<ulong, PlayerData>>(HomeDataPath, GetType());

			if(null != HomeDataFile.Data)
				HomeData = new HomeDictionary(HomeDataFile.Data.GetDictionary());			
		}

		private void SavePlayerData()
		{			
			Log.Log(HomeDataFile.QualifiedName + " saving.", false);
			HomeDataFile.Data = new Util.SerializableDictionaryWrapper<ulong, PlayerData>(HomeData);
			HomeDataFile.Save();		
		}

		private void HandleHomeCommand(Connection conn, ObjectParser parser)
		{
			var player = conn.Destination.GetPlayer();
			var character = Util.PlayerCharacter(player);
			var msg = new SetHomeMessage(parser);
			var home = msg.Home.Ref as IMyMedicalRoom;
					
			if(null == character)
			{
				NotificationCommand.Notice(conn, CharacterError);
				return;
			}

			//Wiseguys
			if (Carcasses.Contains(character))
			{
				NotificationCommand.Notice(conn, "No.");
				return;
			}

			if (!IsHomeValid(player, GetPlayerHomeBounds(player), home))
			{
				NotificationCommand.Notice(conn, RangeError);
				return;
			}
									
			HomeData[player].SetHome(Util.RelativePosition((character as IMyEntity).WorldMatrix, home.WorldMatrix), home);
			NotificationCommand.Notice(conn, "Your home spawn has been set: " + home.DisplayNameText);
		}
	}
}
