﻿/*
* Levitator's Space Engineers Utility Library
*
* Random stuff that could be useful in lots of places.
* Copyright Levitator 2015
*
* Reuse is free as long as you attribute the author.
*
* V1.03
*
*/

using System.Xml.Serialization;
using Sandbox.ModAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using VRage.ModAPI;
using VRageMath;
using Levitator.SE.Serialization;

namespace Levitator.SE.Utility
{
	public static class Util
	{	
		public static string WrapText(string text, int width)
		{
			var sb = new StringBuilder(text.Length * 2);
			int notANewLine = 0;
			char c;

			for (int i = 0; i < text.Length; ++i)
			{
				c = text[i];
				if (c != '\n') ++notANewLine;
				if (notANewLine > width)
				{
					notANewLine = 0;
					sb.Append('\n');
				}
				sb.Append(c);
			}

			return sb.ToString();
		}

		public static void Swap<T>(ref T a, ref T b)
		{
			T c = a;
			a = b;
			b = c;
		}

		//A foreach method that operates on functors
		//because the SpaceEngineers sandbox does (did?) not support foreach	
		public static void ForEach<T>(IEnumerable<T> collection, Action<T> f)
		{
			using (IEnumerator<T> x = collection.GetEnumerator())
			{
				while (x.MoveNext())
				{
					f(x.Current);
				}
			}				
		}

		//Call a predicate with successive elements of collection and stop if it returns false
		//Returns true if the end of the collection is passed or false otherwise
		public static bool DoWhile<T>(IEnumerable<T> collection, Func<T, bool> f)
		{
			using (IEnumerator<T> x = collection.GetEnumerator() ) {
				while (x.MoveNext()) {
					if (!f(x.Current))
						return false;
				}
				return true;
			}
		}


		public static void Disposer<T>(T t) where T : IDisposable{ t.Dispose(); }

		public static void DisposeIfSet<T>(ref T obj) where T : class, IDisposable
		{
			if (null != obj)
			{
				obj.Dispose();
				obj = null;
			}
		}

		public static void CopyToArray<T>(IEnumerable<T> collection, T[] array, int arrayIndex = 0)
		{			
			int i = arrayIndex;
			//foreach (var t in collection) { array[i++] = t; }
			Util.ForEach(collection, t => array[i++] = t);
		}

		//KeyValuePair is not serializable, but this is
		[Serializable]
		public class SerializableKeyValuePair<K, T>
		{
			public K Key;
			public T Value;

			public SerializableKeyValuePair(){}

			public SerializableKeyValuePair(K k, T v)
			{
				Key = k;
				Value = v;
			}
		}

		//In-place XML serialization of a Dictionary<> without intermediate conversion to a List<>
		[Serializable]
		public class SerializableDictionaryWrapper<K, T> : ICollection<SerializableKeyValuePair<K, T>>
		{
			private Dictionary<K, T> Dictionary;

			public SerializableDictionaryWrapper(Dictionary<K, T> dictionary)
			{
				Dictionary = dictionary;
			}

			public SerializableDictionaryWrapper()
			{
				Dictionary = new Dictionary<K, T>();
			}

			public Dictionary<K, T> GetDictionary()
			{
				return Dictionary;
			}

			public class Enumerator : IEnumerator<SerializableKeyValuePair<K, T>>
			{
				private IEnumerator<KeyValuePair<K, T>> DictEnumerator;

				public Enumerator(Dictionary<K, T> dictionary)
				{
					DictEnumerator = dictionary.GetEnumerator();
				}

				object IEnumerator.Current
				{
					get
					{
						return (this as IEnumerator<SerializableKeyValuePair<K, T>>).Current;
					}
				}

				SerializableKeyValuePair<K, T> IEnumerator<SerializableKeyValuePair<K, T>>.Current
				{
					get
					{
						return new SerializableKeyValuePair<K, T>(DictEnumerator.Current.Key, DictEnumerator.Current.Value);
					}
				}

				void IDisposable.Dispose()
				{
					DictEnumerator.Dispose();
				}

				bool IEnumerator.MoveNext()
				{
					return DictEnumerator.MoveNext();
				}

				void IEnumerator.Reset()
				{
					DictEnumerator.Reset();
				}
			}

			public void Add(SerializableKeyValuePair<K,T> skvp){ Dictionary.Add(skvp.Key, skvp.Value); }

			public void Add(object obj){ Add(obj as SerializableKeyValuePair<K, T>); }

			int ICollection<SerializableKeyValuePair<K, T>>.Count
			{
				get
				{
					return Dictionary.Count;
				}
			}

			bool ICollection<SerializableKeyValuePair<K, T>>.IsReadOnly
			{
				get
				{
					return false;
				}
			}

			void ICollection<SerializableKeyValuePair<K, T>>.Add(SerializableKeyValuePair<K, T> item)
			{
				Dictionary.Add(item.Key, item.Value);
			}

			void ICollection<SerializableKeyValuePair<K, T>>.Clear()
			{
				Dictionary.Clear();
			}

			bool ICollection<SerializableKeyValuePair<K, T>>.Contains(SerializableKeyValuePair<K, T> item)
			{
				return Dictionary.ContainsKey(item.Key); //Compare the value too?
			}

			void ICollection<SerializableKeyValuePair<K, T>>.CopyTo(SerializableKeyValuePair<K, T>[] array, int arrayIndex)
			{
				CopyToArray(this, array, arrayIndex);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return (this as IEnumerable<SerializableKeyValuePair<K, T>>).GetEnumerator();
			}

			IEnumerator<SerializableKeyValuePair<K, T>> IEnumerable<SerializableKeyValuePair<K, T>>.GetEnumerator()
			{
				return new Enumerator(Dictionary);
			}

			bool ICollection<SerializableKeyValuePair<K, T>>.Remove(SerializableKeyValuePair<K, T> item)
			{
				return Dictionary.Remove(item.Key);
			}
		}		
				
		public static void ForwardIfNotNull<T>(T x, Action<T> a) where T : class { if(null != x) a(x); }

		//Check whether x is at or within dt distance of t inclusive. dt must be positive
		public static bool IsWithin(float x, float t, float dt)
		{
			return x >= t - dt && x <= t + dt;
		}

		//Check whether p is inside a cube extending dist in each direction
		public static bool IsInCube(VRageMath.Vector3 p, VRageMath.Vector3 cube, float dist)
		{
			return IsWithin(p.X, cube.X, dist) && IsWithin(p.Y, cube.Y, dist) && IsWithin(p.Z, cube.Z, dist);
		}

		public static IMyCharacter PlayerCharacter(IMyPlayer player)
		{
			if (null != player.Controller) return player.Controller.ControlledEntity as IMyCharacter;
			else return null;
        }

		//x' = result * origin'
		public static MatrixD RelativePosition(MatrixD x, MatrixD origin)
		{
			return x * Matrix.Invert(origin);			
        }

		//Not sure how efficient this is, but it's pretty lame that our .NET version does not have it
		public static T First<T>(this HashSet<T> set)
		{
			var items = set.GetEnumerator();			
			if (!items.MoveNext()) throw new Exception("No First() in empty set");
			var first = items.Current;
			items.Dispose();
			return first;
		}
	}

	//Not whitelisted, so we define our own.
	public class NotImplementedException : Exception
	{
		public NotImplementedException() : base("Method not implemented") { }
		public NotImplementedException(string message) : base(message) { }
	}

	//Simple way of representing substrings without copying them
	public struct SubString : IEnumerable<char>
	{
		public readonly string String;
		public readonly int Start, End;

		class Enumerator : IEnumerator<char>
		{
			int Position;
			SubString SubString;

			public Enumerator(SubString substring)
			{
				SubString = substring;
				Position = substring.Start - 1;
			}

			object IEnumerator.Current
			{
				get
				{
					return (this as IEnumerator<char>).Current;
				}
			}

			char IEnumerator<char>.Current
			{
				get
				{
					if (Position >= SubString.End) throw new Exception("Out of bounds");
					return SubString.String[Position];
				}
			}

			void IDisposable.Dispose() { }

			bool IEnumerator.MoveNext() { return ++Position < SubString.End; }

			void IEnumerator.Reset() { Position = SubString.Start - 1; }
		}

		private void CheckBounds(int pos)
		{
			if (pos < 0 || pos >= String.Length) throw new ArgumentException("Out of bounds");
		}

		public SubString(string str, int start, int end)
		{
			String = str;
			Start = start;
			End = end;
			CheckBounds(start);
			CheckBounds(end);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (this as IEnumerable<char>).GetEnumerator();
		}

		IEnumerator<char> IEnumerable<char>.GetEnumerator()
		{
			return new Enumerator(this);
		}
	}

	//Like an enumerator, but you can actually test it to see whether it's past the end
	public class StringPos
	{
		public StringPos(StringPos old)
		{
			String = old.String;
			Index = old.Index;
		}

		public StringPos(string s, int p = 0) { String = s; Index = p; }
		public bool PastEnd { get { return Index >= String.Length; } }
		public char Current { get { return String[Index]; } }
		public void MoveNext() { ++Index; }
		public string String;
		public int Index;

		//public static implicit operator StringPos(string s) { return new StringPos(s, 0); }
	}

	//To avoid confusion with things that can be Dispose()ed any time
	//These things are disposed by Singleton<>
	public interface ISingleton
	{
		void SingletonDispose();
	}

	//This is kind of horrible but it's because we don't have WeakReference
	//Singleton rules. 
	//1) Call Set() on construction
	//2) Store a Ref at the point of use and Dispose it when done	
	public static class Singleton<T> where T : class, ISingleton
	{
		private static SingletonState State = new SingletonState();
		
		public static void Set(T me) {
			if (null != State.Instance) throw new Exception("Duplicate singleton");
			State.Instance = me;
		}
				
		public static void Dispose()
		{
			if (null != State.Instance)
			{
				State.Instance.SingletonDispose();
				State.UseCount = 0;
				State = new SingletonState();
			}
		}
		
		private class SingletonState
		{
			public T Instance;
			public int UseCount = 0;
		}

		public class Ref : IDisposable {
			private SingletonState State;

			public Ref(Func<T> constructor){
				State = Singleton<T>.State;
				++State.UseCount;
				if (null == State.Instance) State.Instance = constructor();
			}

			public void Dispose() {
				--State.UseCount;
				if (State.UseCount <= 0) Singleton<T>.Dispose();
            }
			
			public T Instance { get { return State.Instance; } }
		}
	}

	public static class Singleton
	{				
		public static Singleton<T>.Ref Get<T>(Func<T> constructor) where T : class, ISingleton { return new Singleton<T>.Ref(constructor); }
	}

	[System.Serializable]
	public struct EntityRef:Serializable
	{
		long Id;
		private IMyEntity mEntity;

		[XmlIgnore]
		public IMyEntity Ref
		{
			get
			{
				if (null == mEntity)
				{
					if (Id != 0)					
						MyAPIGateway.Entities.TryGetEntityById(Id, out mEntity);
					if(null != mEntity) NullIfClosed();
					//if (null == mEntity) Id = 0;		//Sometimes an entity which cannot be found will turn up later, when replicated
				}
				else NullIfClosed();
				return mEntity;
			}

			set {
				mEntity = value;
				if (null == mEntity)
					Id = 0;
				else 
					if(mEntity.Closed) Null();
					else Id = mEntity.EntityId;
			}
		}

		private void NullIfClosed() { if (mEntity.Closed) Null(); }

		public void Null() {
			mEntity = null;
			Id = 0;
		}

		public long EntityId
		{
			get{ return Id; }
			set {
				Id = value;
				mEntity = null;	//Lazy lookup upon retrieval
			}
		}

		public EntityRef(IMyEntity obj)
		{
			Id = 0;
			mEntity = null;
			Ref = obj;
		}

		public EntityRef(long id)
		{
			Id = id;
			mEntity = null;
		}

		public EntityRef(ObjectParser parser)
		{
			Id = parser.ParseLong();
			mEntity = null;
		}

		public static EntityRef New(ObjectParser parser) { return new EntityRef(parser); }
		public void Serialize(ObjectSerializer ser){ ser.Write(Id); }		
	}

	//Just a reference wrapped in a struct so that we can use null as a dictionary key
	public struct Reference<T>
	{
		public T Value;
		public Reference(T value) { Value = value; }		
	}

	public static class Reference
	{
		public static Reference<T> Create<T>(T value) { return new Reference<T>(value); }
	}

	//Attempt to abstract away the difficulty in referencing events as first-class objects
	public interface IEvent<T>
	{
		void Add(Action<T> handler);
		void Remove(Action<T> handler);
	}

	//Reimplementation of a C#-style multicast event because the SE script environment will throw exceptions
	//for System.Threading.Interlocked in the add() handlers of native events 12/13/2015
	public class Event<T> : IEvent<T>
	{
		public List<Action<T>> Queue = new List<Action<T>>();

		public void Add(Action<T> action) { Queue.Add(action); }
		public void Remove(Action<T> action) { Queue.Remove(action); }

		public static Event<T> operator +(Event<T> evt, Action<T> action)
		{
			evt.Add(action);
			return evt;
		}

		public static Event<T> operator -(Event<T> evt, Action<T> action)
		{
			evt.Remove(action);
			return evt;
		}

		public void Invoke(T param)
		{
			//foreach (var action in Queue) { action(param); }
			Util.ForEach(Queue, action => action(param));
		}
	}

	public struct EventProxy<T> : IEvent<T>
	{
		Action<Action<T>> AddMethod, RemoveMethod;

		public EventProxy(Action<Action<T>> addMethod, Action<Action<T>> removeMethod)
		{
			AddMethod = addMethod;
			RemoveMethod = removeMethod;
		}

		public void Add(Action<T> handler) { AddMethod(handler); }
		public void Remove(Action<T> handler) { RemoveMethod(handler); }
	}

	//Maybe someday WeakReference will get whitelisted
	/*
	public class WeakReference<T> : WeakReference
	{
		public WeakReference() : base(null) { }
		public WeakReference(T v) : base(v){}

		public new T Target
		{
			get { return (T)base.Target; }
			set { base.Target = value; }
		}
	}

	//A weak-referenecd event handler which forwards an event so long as its target exists
	//and detaches itself otherwise
	public struct WeakEventHandler<T>
	{
		IEvent<T> Event;
		private WeakReference<Action<T>> Target;
		

		public WeakEventHandler(IEvent<T> evt, Action<T> action)
		{
			Event = evt;
			Target = new WeakReference<Action<T>>(action);
			Event.Add(action);
		}

		private void Handler(T v)
		{
			var action = Target.Target;
			if (null != action)
				action(v);
			else
				Event.Remove(Handler);
		}
	}

	public static class WeakEventHandler
	{
		//public static WeakEventHandler<T> Create<T>(Action<T> target, Action<Action<T>> detach) { return new WeakEventHandler<T>(target, detach); }
		public static WeakEventHandler<T> Create<T>(IEvent<T> evt, Action<T> handler){ return new WeakEventHandler<T>(evt, handler); }
	}
	*/
}
