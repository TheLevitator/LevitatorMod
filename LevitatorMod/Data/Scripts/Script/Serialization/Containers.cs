/*
* Levitator's Space Engineers Serialization Library
* Container Classes
* 
* Stuff to facilitate the non-XML serialization of containers
*
* V1.00
*
*/

using Levitator.SE.Utility;
using System;
using System.Collections.Generic;

namespace Levitator.SE.Serialization
{
	//An object that has a name for retrieval from a dictionary
	public abstract class KeyedSerializable : Serializable
	{
		public abstract string GetKey();
		public void Serialize(ObjectSerializer ser) { }
	}

	public abstract class PersistentSequence<T> : Serializable
	{
		public void Deserialize(ObjectParser parser)
		{
			Func<T> parseF = GetTTraits().GetParseFunction(parser);
			while (parser.HaveData())
			{
				Add(parseF());
			}
		}

		public void Serialize(ObjectSerializer ser)
		{
			//foreach (var t in GetContainer()) { GetTTraits().GetSerializeFunction(ser)(t); }
			Util.ForEach(GetContainer(), GetTTraits().GetSerializeFunction(ser));
		}

		protected abstract IEnumerable<T> GetContainer();
		protected abstract SerializationTraits<T> GetTTraits();
		public abstract void Add(T item);
	}

	public abstract class PersistentStringSequence : PersistentSequence<string>
	{
		protected override SerializationTraits<string> GetTTraits() { return new StringSerializationTraits(); }
	}

	public class PersistentStringList : PersistentStringSequence
	{
		public readonly List<string> Container;
		public PersistentStringList() { Container = new List<string>(); }
		public override void Add(string item) { Container.Add(item); }
		protected override IEnumerable<string> GetContainer() { return Container; }
	}
}
