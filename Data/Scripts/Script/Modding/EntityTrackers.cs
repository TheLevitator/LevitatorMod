/*
* Levitator's Space Engineers Modding Library
* Entity Tracker Classes
*
* This is a highly generic way of reliably obtaining one event for every instance of
* a given entity upon addition and removal, whether the entity is top-level or a block.
* Haven't yet experimented with floating objects or voxel entities.
*
* TODO: I can't remember why I commented out some Singleton<> references.
* Some of these should probably be enforced as Singletons to catch any cleanup problems
*
* Reuse is free as long as you attribute the author.
*
* V1.0
*
*/

using Levitator.SE.Utility;
using Sandbox.ModAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using VRage.ModAPI;

namespace Levitator.SE.Modding
{
	//Hook an event for a specified base class and forward it only if it matches the subclass
	//A non-singular base class
	public class EntityBroadcaster<T, B> where T : class
		where B : class, IMyEntity
	{
		public Event<T> Event = new Event<T>();

		public T Match(B b) { return b as T; }
		public T Match(IMySlimBlock b) { return b.FatBlock as T; }

		public void HandleEvent(B ent) { Util.ForwardIfNotNull(Match(ent), Event.Invoke); }
		public void HandleEvent(IMySlimBlock block) { Util.ForwardIfNotNull(Match(block), Event.Invoke); }
	}

	public static class TopLevelEntityAddedBroadcaster<T> where T : class
	{
		static public readonly EntityBroadcaster<T, IMyEntity> Broadcaster = new EntityBroadcaster<T, IMyEntity>();
		static TopLevelEntityAddedBroadcaster() { MyAPIGateway.Entities.OnEntityAdd += Broadcaster.HandleEvent; }
	}

	public static class TopLevelEntityRemovedBroadcaster<T> where T : class
	{
		static public readonly EntityBroadcaster<T, IMyEntity> Broadcaster = new EntityBroadcaster<T, IMyEntity>();
		static TopLevelEntityRemovedBroadcaster() { MyAPIGateway.Entities.OnEntityAdd += Broadcaster.HandleEvent; }
	}
	
	public abstract class EntityTracker<T> : IDisposable, IEnumerable<T> where T : class
	{
		private readonly HashSet<T> Entities = new HashSet<T>();

		protected EntityTracker()
		{			
			GetAddEvent().Add(AddEntity);
			GetRemoveEvent().Add(RemoveEntity);
			GetExisting();
		}

		public virtual void Dispose()
		{
			GetAddEvent().Remove(AddEntity);
			GetRemoveEvent().Remove(RemoveEntity);
		}

		public int Count { get { return Entities.Count; } }
		protected abstract void GetExisting();
		public abstract Event<T> GetAddEvent();
		public abstract Event<T> GetRemoveEvent();

		protected virtual void AddEntity(T entity){ Entities.Add(entity); }
		protected virtual void RemoveEntity(T entity) { Entities.Remove(entity); }

		public IEnumerator<T> GetEnumerator() { return Entities.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return (this as IEnumerable<T>).GetEnumerator(); }
	}

	//Track all entities added or removed directly to MyAPIGateway.Entities
	//Would be static, but static cannot inherit
	public class TopLevelEntityTracker<T> : EntityTracker<T> where T : class
	{
		static Singleton<TopLevelEntityTracker<T>> Singleton;

		private TopLevelEntityTracker() { Singleton = new Singleton<TopLevelEntityTracker<T>>(this); }
		public static TopLevelEntityTracker<T> Constructor() { return new TopLevelEntityTracker<T>(); }

		public override Event<T> GetAddEvent() { return TopLevelEntityAddedBroadcaster<T>.Broadcaster.Event; }
		public override Event<T> GetRemoveEvent() { return TopLevelEntityRemovedBroadcaster<T>.Broadcaster.Event; }
		protected override void GetExisting() { MyAPIGateway.Entities.GetEntities(null, AddEntityPredicate); }

		private bool AddEntityPredicate(IMyEntity entity)
		{
			Util.ForwardIfNotNull(TopLevelEntityAddedBroadcaster<T>.Broadcaster.Match(entity), AddEntity);			
			return false;
		}
	}

	//Receive reliable notifications
	//public class EntityTrackerSink<T, TrackerT> : IDisposable where T : class, IMyEntity where TrackerT : EntityTracker<T>
	public class EntityTrackerSink<T> : IDisposable where T : class
	{
		private Action<T> AddAction = null;
		private Action<T> RemoveAction = null;
		public readonly EntityTracker<T> Tracker = null;
	
		public EntityTrackerSink(EntityTracker<T> tracker, Action<T> onAdd = null, Action<T> onRemove = null)
		{
			Tracker = tracker;		
			if (null != onAdd)
			{				
				AddAction = onAdd;
				Tracker.GetAddEvent().Add(onAdd);
				//foreach (var ent in Tracker) { onAdd(ent); } //Catch up
				Util.ForEach(Tracker, onAdd);
			}

			if (null != onRemove)
			{
				RemoveAction = onRemove;
				Tracker.GetRemoveEvent().Add(onRemove);
			}
		}

		public void Dispose()
		{
			if (null != AddAction) Tracker.GetAddEvent().Remove(AddAction);
			if (null != RemoveAction) Tracker.GetRemoveEvent().Remove(AddAction);
		}
	}

	//Listen to the entire session for cube blocks. This is better than GameLogicComponent if you don't want to waste cycles on needless update calls
	public static class GlobalBlockAddedBroadcaster<T> where T : class
	{
		static public readonly EntityBroadcaster<T, IMyEntity> Broadcaster = new EntityBroadcaster<T, IMyEntity>();
		static private EntityTrackerSink<IMyCubeGrid> Grids = new EntityTrackerSink<IMyCubeGrid>(
			Singleton.Get<TopLevelEntityTracker<IMyCubeGrid>>(TopLevelEntityTracker<IMyCubeGrid>.Constructor), OnGridAdded, OnGridRemoved);
		static private void OnGridAdded(IMyCubeGrid grid) { grid.OnBlockAdded += Broadcaster.HandleEvent; }
		static private void OnGridRemoved(IMyCubeGrid grid) { grid.OnBlockAdded -= Broadcaster.HandleEvent; }		
	}

	public static class GlobalBlockRemovedBroadcaster<T> where T : class
	{
		static public readonly EntityBroadcaster<T, IMyEntity> Broadcaster = new EntityBroadcaster<T, IMyEntity>();
		static private EntityTrackerSink<IMyCubeGrid> Grids = new EntityTrackerSink<IMyCubeGrid>(
			Singleton.Get<TopLevelEntityTracker<IMyCubeGrid>>(TopLevelEntityTracker<IMyCubeGrid>.Constructor), OnGridAdded, OnGridRemoved);
		static private void OnGridAdded(IMyCubeGrid grid) { grid.OnBlockRemoved += Broadcaster.HandleEvent; }
		static private void OnGridRemoved(IMyCubeGrid grid) { grid.OnBlockRemoved -= Broadcaster.HandleEvent; }		
	}

	public class GlobalBlockTracker<T> : EntityTracker<T> where T : class
	{
		private Singleton<GlobalBlockTracker<T>> Instance;
		public static GlobalBlockTracker<T> Constructor() { return new GlobalBlockTracker<T>(); }
		public GlobalBlockTracker() { Instance = new Singleton<GlobalBlockTracker<T>>(this); }
		public override Event<T> GetAddEvent() { return GlobalBlockAddedBroadcaster<T>.Broadcaster.Event; }
		public override Event<T> GetRemoveEvent() { return GlobalBlockRemovedBroadcaster<T>.Broadcaster.Event; }

		protected override void GetExisting()
		{
			//foreach (var grid in Singleton.Get<TopLevelEntityTracker<IMyCubeGrid>>(TopLevelEntityTracker<IMyCubeGrid>.Constructor)){ grid.GetBlocks(null, BlockPredicate);}
			Util.ForEach(Singleton.Get(TopLevelEntityTracker<IMyCubeGrid>.Constructor), grid => grid.GetBlocks(null, BlockPredicate));
		}

		private bool BlockPredicate(IMySlimBlock block)
		{
			Util.ForwardIfNotNull(GlobalBlockAddedBroadcaster<T>.Broadcaster.Match(block), AddEntity);
			return false;
		}
	}

	//Obsolete
	/*
	public class GameLogicComponentBase : MyGameLogicComponent
	{		
		public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
		{
			return Entity.GetObjectBuilder(copy);
		}
	}

	//Track a class of entities so as to avoid scanning everything in the world
	public class EntityTrackerBaseGameLogic<D> : GameLogicComponentBase, IEnumerable<D> where D : EntityTrackerBaseGameLogic<D>
	{
		protected static HashSet<D> Entities = new HashSet<D>();

		public IEnumerator<D> GetEnumerator() { return Entities.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return Entities.GetEnumerator(); }
	}

	public class SceneEntityTrackerGameLogic<T> : EntityTrackerBaseGameLogic<SceneEntityTrackerGameLogic<T>> where T : IMyEntity
	{
		public override void OnAddedToScene()
		{
			Entities.Add(this);
			base.OnAddedToScene();
		}

		public override void OnRemovedFromScene()
		{
			Entities.Remove(this);
			base.OnRemovedFromScene();
		}
	}
	*/
}
