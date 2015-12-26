/*
* Levitator's Space Engineers Serialization Library
*
* C++-style traits system for the Serialization library which abstracts away
* differences between value types and object types for use in serializable Generics.
* It pretty much revolves around the desire to avoid boxing types to/from Object.
*
* Copyright Levitator 2015
*
* Reuse is free as long as you attribute the author.
*
* V1.0
*
*/

using System;

namespace Levitator.SE.Serialization
{	
	public interface SerializationTraits<T>
	{
		Func<T> GetParseFunction(ObjectParser parser);
		Action<T> GetSerializeFunction(ObjectSerializer serializer);
	}

	//Functor so that an object parse call is of the same form as for a value type
	public struct ParseFunctor<T>
	{
		private ObjectParser Parser;
		private Func<ObjectParser, T> Construct;

		public ParseFunctor(Func<ObjectParser, T> construct, ObjectParser parser)
		{
			Parser = parser;
			Construct = construct;
		}

		public T Parse() { return Parser.Parse<T>(Construct, true); }
	}

	public struct SerializableSerializationTraits<T> : SerializationTraits<T> where T : class, Serializable
	{
		private Func<ObjectParser, T> Construct;

		public SerializableSerializationTraits(Func<ObjectParser, T> construct){ Construct = construct; }
		public Func<T> GetParseFunction(ObjectParser parser){ return new ParseFunctor<T>(Construct, parser).Parse; }
		public Action<T> GetSerializeFunction(ObjectSerializer serializer){ return serializer.Write;  }
	}

	public struct StringSerializationTraits : SerializationTraits<string>
	{
		public Func<string> GetParseFunction(ObjectParser parser) { return parser.ParseField; }
		public Action<string> GetSerializeFunction(ObjectSerializer serializer) { return serializer.Write; }
	}

	public struct IntSerializationTraits : SerializationTraits<int>
	{
		public Func<int> GetParseFunction(ObjectParser parser) { return parser.ParseInt; }
		public Action<int> GetSerializeFunction(ObjectSerializer serializer) { return serializer.Write; }
	}

	public struct UIntSerializationTraits : SerializationTraits<uint>
	{
		public Func<uint> GetParseFunction(ObjectParser parser) { return parser.ParseUInt; }
		public Action<uint> GetSerializeFunction(ObjectSerializer serializer) { return serializer.Write; }
	}

	public struct LongSerializationTraits : SerializationTraits<long>
	{
		public Func<long> GetParseFunction(ObjectParser parser) { return parser.ParseLong; }
		public Action<long> GetSerializeFunction(ObjectSerializer serializer) { return serializer.Write; }
	}

	public struct ULongSerializationTraits : SerializationTraits<ulong>
	{
		public Func<ulong> GetParseFunction(ObjectParser parser) { return parser.ParseULong; }
		public Action<ulong> GetSerializeFunction(ObjectSerializer serializer) { return serializer.Write; }
	}

	public struct FloatSerializationTraits : SerializationTraits<float>
	{
		public Func<float> GetParseFunction(ObjectParser parser) { return parser.ParseFloat; }
		public Action<float> GetSerializeFunction(ObjectSerializer serializer) { return serializer.Write; }
	}

	public struct DoubleSerializationTraits : SerializationTraits<double>
	{
		public Func<double> GetParseFunction(ObjectParser parser) { return parser.ParseDouble; }
		public Action<double> GetSerializeFunction(ObjectSerializer serializer) { return serializer.Write; }
	}
}
