/*
* Levitator's Space Engineers Modding Library
* Deferred Task Class
*
* We can't really predict when an entity will be replicated to a client
* So, we define the Deferred Task, which will retry an operation every update,
* for a certain period of time, until it succeeds or times out
*
* Reuse is free as long as you attribute the author.
*
* V1.0
*
*/

using Levitator.SE.Utility;
using System;
using System.Collections.Generic;

namespace Levitator.SE.Modding
{
	public interface IDeferredTask
	{
		DateTime GetExpiry();
		string OnAbandon(); //Do something upon giving up and return a comment about it for the log
		bool DeferredRun();
	}

	public abstract class DeferredTask
	{
		private DateTime mExpiry;
		public DeferredTask(int timeout_ms){ mExpiry = DateTime.Now + new TimeSpan(0, 0, 0, 0, timeout_ms); }
	}

	public abstract class DeferredTaskCommand : Command, IDeferredTask
	{
		private DateTime mExpiry;

		public virtual string OnAbandon() { return "Command: " + GetId(); }
		protected DeferredTaskCommand(int timeout_ms) { mExpiry = DateTime.Now + new TimeSpan(0, 0, 0, 0, timeout_ms); }
		public DateTime GetExpiry() { return mExpiry; }
		public abstract bool DeferredRun();
	}

	//Sometimes we need to do something as soon as possible, but which will fail an unpredictable number of times before it succeeds
	//Like if dependent entities have not been replicated yet
	//We give up on timeout or exception, so if you want to retry on exception, make sure to catch it
	public class DeferredTaskQueue
	{
		IModLog Log;
		private List<IDeferredTask> Tasks = new List<IDeferredTask>();
		private List<IDeferredTask> NewTasks = new List<IDeferredTask>();
		private DateTime Now;

		public DeferredTaskQueue(IModLog log) { Log = log; }

		public void Add(IDeferredTask task)
		{
			Now = DateTime.Now;
			TryTask(task, Tasks);
		}
		public void Remove(IDeferredTask task) { Tasks.Remove(task); }
		public void Poll()
		{
			Now = DateTime.Now;
			NewTasks.Clear();
			Util.ForEach(Tasks, t => TryTask(t, NewTasks));
			Util.Swap(ref Tasks, ref NewTasks);
		}

		private void TryTask(IDeferredTask task, List<IDeferredTask> list)
		{
			try
			{
				if (task.GetExpiry() > Now)
				{
					if (!task.DeferredRun())
						list.Add(task);
				}
				else
				{
					Log.Log("Expired: " + task.OnAbandon());
				}
			}
			catch (Exception x) { Log.Log("Exception: " + task.OnAbandon(), x); }
		}
	}	
}
