/*
* Levitator's Space Engineers Modding Library
* Entity Tracker Classes
*
* This is a highly generic way of reliably obtaining one event for every instance of
* a given entity upon addition and removal, whether the entity is top-level or a block.
* Haven't yet experimented with floating objects or voxel entities.
*
* TODO: This has seen massive refactoring and needs testing
*
* Reuse is free as long as you attribute the author.
*
* V1.03
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
	static class Events
	{
		public static readonly EventProxy<IMyEntity> TopLevelEntityAdded =
			new EventProxy<IMyEntity>((Action<IMyEntity> handler) => MyAPIGateway.Entities.OnEntityAdd += handler, handler => MyAPIGateway.Entities.OnEntityAdd -= handler);

		public static readonly EventProxy<IMyEntity> TopLevelEntityRemoved =
			new EventProxy<IMyEntity>(handler => MyAPIGateway.Entities.OnEntityRemove += handler, handler => MyAPIGateway.Entities.OnEntityRemove -= handler);

		public static EventProxy<IMySlimBlock> MakeGridBlockAdded(IMyCubeGrid grid)
		{
			return new EventProxy<IMySlimBlock>(handler => grid.OnBlockAdded += handler, handler => grid.OnBlockAdded -= handler);
		}

		public static EventProxy<IMySlimBlock> MakeGridBlockRemoved(IMyCubeGrid grid)
		{
			return new EventProxy<IMySlimBlock>(handler => grid.OnBlockRemoved += handler, handler => grid.OnBlockRemoved -= handler);
		}

		/*
		public static EventProxy<IMySlimBlock> MakeGridBlockRemoved(IMyCubeGrid grid)
		{
			return new EventProxy<IMySlimBlock>(handler => grid.OnBlockRemoved += handler, handler => grid.OnBlockRemoved -= handler);
		}
		*/
	}

	//A hook an event for a specified base class and forward it only if it matches the subclass
	//A non-singular base class. Derived class attaches an event source to HandleEvent
	public abstract class EntityBroadcaster<T, B> where T : class
		where B : class, IMyEntity
	{
		public Event<T> Event = new Event<T>();

		public T Match(B b) { return b as T; }
		public T Match(IMySlimBlock b) { return b.FatBlock as T; }

		public void HandleEvent(B ent) { Util.ForwardIfNotNull(Match(ent), Event.Invoke); }
		public void HandleEvent(IMySlimBlock block) { Util.ForwardIfNotNull(Match(block), Event.Invoke); }
	}

	public class TopLevelEntityAddedBroadcaster<T> : EntityBroadcaster<T, IMyEntity>, ISingleton where T : class
	{		
		private TopLevelEntityAddedBroadcaster() {
			Singleton<TopLevelEntityAddedBroadcaster<T>>.Set(this);
			Events.TopLevelEntityAdded.Add(HandleEvent);			
        }

		public static TopLevelEntityAddedBroadcaster<T> New() { return new TopLevelEntityAddedBroadcaster<T>(); }

		public void SingletonDispose(){ Events.TopLevelEntityAdded.Remove(HandleEvent);	}
	}

	public class TopLevelEntityRemovedBroadcaster<T> : EntityBroadcaster<T, IMyEntity>, ISingleton where T : class
	{
		private TopLevelEntityRemovedBroadcaster()
		{
			Singleton<TopLevelEntityRemovedBroadcaster<T>>.Set(this);
			Events.TopLevelEntityRemoved.Add(HandleEvent);
		}
		public static TopLevelEntityRemovedBroadcaster<T> New() { return new TopLevelEntityRemovedBroadcaster<T>(); }
		public void SingletonDispose(){ Events.TopLevelEntityRemoved.Remove(HandleEvent); }
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
		protected abstract void GetExisting(); //To populate Entities with the set of extant objects through AddEntity
		public abstract IEvent<T> GetAddEvent();
		public abstract IEvent<T> GetRemoveEvent();

		protected virtual void AddEntity(T entity){ Entities.Add(entity); }
		protected virtual void RemoveEntity(T entity) { Entities.Remove(entity); }

		public IEnumerator<T> GetEnumerator() { return Entities.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return (this as IEnumerable<T>).GetEnumerator(); }
	}

	//Track all entities added or removed directly to MyAPIGateway.Entities	
	public class TopLevelEntityTracker<T> : EntityTracker<T>, ISingleton where T : class
	{
		private Singleton<TopLevelEntityAddedBroadcaster<T>>.Ref AddBroadcaster = Singleton.Get(TopLevelEntityAddedBroadcaster<T>.New);
		private Singleton<TopLevelEntityRemovedBroadcaster<T>>.Ref RemoveBroadcaster = Singleton.Get(TopLevelEntityRemovedBroadcaster<T>.New);

		private TopLevelEntityTracker() { Singleton<TopLevelEntityTracker<T>>.Set(this); }
		public static TopLevelEntityTracker<T> New() { return new TopLevelEntityTracker<T>(); }
		public void SingletonDispose()
		{
			AddBroadcaster.Dispose();
			RemoveBroadcaster.Dispose();			
			base.Dispose();
		}

		public override IEvent<T> GetAddEvent() { return AddBroadcaster.Instance.Event; }
		public override IEvent<T> GetRemoveEvent() { return RemoveBroadcaster.Instance.Event; }
		protected override void GetExisting() { MyAPIGateway.Entities.GetEntities(null, AddEntityPredicate); }

		private bool AddEntityPredicate(IMyEntity entity)
		{
			Util.ForwardIfNotNull(AddBroadcaster.Instance.Match(entity), AddEntity);			
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

	public abstract class GlobalBlockBroadcasterBase<T> : EntityBroadcaster<T, IMyEntity>, IDisposable where T : class
	{
		protected EntityTrackerSink<IMyCubeGrid> Grids;
		private Singleton<TopLevelEntityTracker<IMyCubeGrid>>.Ref Tracker = Singleton.Get<TopLevelEntityTracker<IMyCubeGrid>>(TopLevelEntityTracker<IMyCubeGrid>.New);
				
		protected GlobalBlockBroadcasterBase(){	 Grids = new EntityTrackerSink<IMyCubeGrid>(Tracker.Instance, AttachGrid, DetachGrid);}
		public virtual void Dispose(){
			Util.ForEach(Tracker.Instance, DetachGrid);
			Tracker.Dispose();
		}

		protected abstract IEvent<IMySlimBlock> GetSourceEvent(IMyCubeGrid grid);
		private void AttachGrid(IMyCubeGrid grid) { GetSourceEvent(grid).Add(HandleEvent); }
		private void DetachGrid(IMyCubeGrid grid) { GetSourceEvent(grid).Remove(HandleEvent); }
	}

	//Listen to the entire session for cube blocks. This is better than GameLogicComponent if you don't want to waste cycles on needless update calls
	public class GlobalBlockAddedBroadcaster<T> : GlobalBlockBroadcasterBase<T>, ISingleton where T : class
	{		
		private GlobalBlockAddedBroadcaster(){ Singleton<GlobalBlockAddedBroadcaster<T>>.Set(this);}
		public static GlobalBlockAddedBroadcaster<T> New() { return new GlobalBlockAddedBroadcaster<T>(); }
		public void SingletonDispose(){ base.Dispose(); }

		protected override IEvent<IMySlimBlock> GetSourceEvent(IMyCubeGrid grid){ return  Events.MakeGridBlockAdded(grid); }
	}

	public class GlobalBlockRemovedBroadcaster<T> : GlobalBlockBroadcasterBase<T>, ISingleton where T : class
	{
		private GlobalBlockRemovedBroadcaster() { Singleton<GlobalBlockRemovedBroadcaster<T>>.Set(this); }
		public static GlobalBlockRemovedBroadcaster<T> New() { return new GlobalBlockRemovedBroadcaster<T>(); }

		public void SingletonDispose(){ base.Dispose();	}

		protected override IEvent<IMySlimBlock> GetSourceEvent(IMyCubeGrid grid) { return Events.MakeGridBlockRemoved(grid); }
	}

	public class GlobalBlockTracker<T> : EntityTracker<T>, ISingleton where T : class
	{
		private Singleton<GlobalBlockAddedBroadcaster<T>>.Ref AddedBroadcaster = Singleton.Get(GlobalBlockAddedBroadcaster<T>.New);
		private Singleton<GlobalBlockRemovedBroadcaster<T>>.Ref RemovedBroadcaster = Singleton.Get(GlobalBlockRemovedBroadcaster<T>.New);
		private Singleton<TopLevelEntityTracker<IMyCubeGrid>>.Ref AllGrids = Singleton.Get(TopLevelEntityTracker<IMyCubeGrid>.New);

		public GlobalBlockTracker() { Singleton<GlobalBlockTracker<T>>.Set(this); }
		public static GlobalBlockTracker<T> New() { return new GlobalBlockTracker<T>(); }
		public void SingletonDispose() {			
			AllGrids.Dispose();
			RemovedBroadcaster.Dispose();
			AddedBroadcaster.Dispose();
			base.Dispose();			
		}

		public override IEvent<T> GetAddEvent() { return AddedBroadcaster.Instance.Event; }
		public override IEvent<T> GetRemoveEvent() { return RemovedBroadcaster.Instance.Event; }
		
		protected override void GetExisting()
		{
			//foreach (var grid in Singleton.Get<TopLevelEntityTracker<IMyCubeGrid>>(TopLevelEntityTracker<IMyCubeGrid>.Constructor)){ grid.GetBlocks(null, BlockPredicate);}
			Util.ForEach(AllGrids.Instance, grid => grid.GetBlocks(null, BlockPredicate));
		}

		private bool BlockPredicate(IMySlimBlock block)
		{
			Util.ForwardIfNotNull(AddedBroadcaster.Instance.Match(block), AddEntity);
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
