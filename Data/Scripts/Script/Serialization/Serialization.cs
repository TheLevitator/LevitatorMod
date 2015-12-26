/*
* Levitator's Space Engineers Serialization Library
*
* Basic lightweight object serialization without reflection to a simpler
* format than XML. This was originally targeted at Programmable Blocks 
* where XML serialization is not available, but since I have it, might
* as well put it to use.
*
* Copyright Levitator 2015
*
* Reuse is free as long as you attribute the author.
*
* V1.0
*
*/

using Levitator.SE.Utility;
using System;
using System.Text;
using VRage;
using VRageMath;

namespace Levitator.SE.Serialization
{
	public class ParseException : System.Exception
	{
		public ParseException(string message) : base(message) { }
	}

	public struct ObjectParser
	{

		public StringPos Pos;
		private bool Present, Null;
		private StringBuilder sb;

		public ObjectParser(ObjectParser parser, bool require = true)
		{
			Pos = parser.Pos;
			Present = false;
			Null = false;
			sb = parser.sb;
			OpenObject(require);
		}

		public ObjectParser(string data, bool require = true)
		{
			Pos = new StringPos(data, 0);
			Present = false;
			Null = false;
			sb = new StringBuilder();
			OpenObject(require);
		}

		public ObjectParser(StringPos position, bool require = true)
		{
			Pos = position;
			Present = false;
			Null = false;
			sb = new StringBuilder();
			OpenObject(require);
		}

		public bool IsPresent() { return Present; }
		public bool IsNull() { return Null; }

		private void OpenObject(bool require)
		{
			char c;
			if (EatWS())
			{
				c = Pos.Current;

				if (c == 'N')
				{
					Present = true;
					Null = true;
					ParseNull();
					return;
				}
				else if (c == '{')
				{
					DemandNext("Data ended while parsing object");
					Present = true;
					Null = false;
					return;
				}
				else if (Pos.Current != '}') throw new ParseException("Expected object data");
			}

			if (require) throw new ParseException("Object missing");
		}

		public void CloseObject()
		{
			if (!EatWS()) throw new ParseException("Data ended seeking close brace");
			if (Pos.Current != '}') throw new ParseException("Unexpected character seeking close bracket: '" + Pos.Current + "'");
			Pos.MoveNext();
			Present = false;
			Null = false;
		}

		private void DemandNext(string msg)
		{
			Pos.MoveNext();
			if (Pos.PastEnd) throw new ParseException(msg);
		}

		//Do we have an additional datum in the current object?
		//Good for variable-length arrays and collections
		public bool HaveData()
		{
			char c;
			if (!EatWS()) return false;
			c = Pos.Current;
			return c != '}';
		}

		public string ParseField()
		{
			bool quoted;
			char c;

			if (!EatWS()) throw new ParseException("Reached end of data expecting field");
			quoted = Pos.Current == '"';
			if (quoted)
				DemandNext("Data ended while parsing string");

			sb.Clear();

			while (!Pos.PastEnd)
			{
				c = Pos.Current;
				if (!quoted)
				{
					if (IsWS(c))
					{
						Pos.MoveNext();
						break;
					}
					else if (c == '{' || c == '}') return sb.ToString();
				}

				if (c == '\\')
				{
					DemandNext("Data ended while parsing escape in field");
					c = Pos.Current;
					if (c == 'r') c = '\r';
					else if (c == 'n') c = '\n';
				}
				else if (c == '"')
				{
					if (!quoted)
						throw new ParseException("Unmatched quote in field");
					else
					{
						Pos.MoveNext();
						break;
					}
				}
				sb.Append(c);

				if (quoted)
					DemandNext("Data ended while parsing string");
				else
					Pos.MoveNext();
			}
			return sb.ToString();
		}

		public int ParseInt() { return int.Parse(ParseField()); }
		public uint ParseUInt() { return uint.Parse(ParseField()); }
		public long ParseLong() { return long.Parse(ParseField()); }
		public ulong ParseULong() { return ulong.Parse(ParseField()); }
		public float ParseFloat() { return float.Parse(ParseField()); }
		public double ParseDouble() { return double.Parse(ParseField()); }

		//We're only interested in a few whitespace characters
		static public bool IsWS(char c)
		{
			return c == ' ' || c == '\t' || c == '\r' || c == '\n';
		}

		public bool EatWS()
		{
			while (!Pos.PastEnd)
			{
				if (!IsWS(Pos.Current))
					return true;
				Pos.MoveNext();
			}

			return false;
		}

		//Just make sure the null is terminated in a valid way
		void ParseNull()
		{
			Pos.MoveNext();
			char c = Pos.Current;
			if (!(IsWS(c) || c == '{' || c == '}' || Pos.PastEnd)) throw new ParseException("Unexpected character after null: " + c);
		}

		public T Parse<T>(Func<ObjectParser, T> construct, bool require = true)
		{
			ObjectParser subparser = new ObjectParser(this, require);
			if (subparser.IsPresent())
			{
				if (subparser.IsNull())
					return default(T);

				T result = construct(subparser);
				subparser.CloseObject();
				return result;
			}
			return default(T);
		}
	}

	public struct ObjectSerializer
	{
		private StringBuilder output;
		private int depth;

		public class SerializeException : Exception
		{
			public SerializeException(string desc) : base(desc) { }
		}

		public ObjectSerializer(ObjectSerializer? dts = null)
		{
			if (dts.HasValue)
			{
				output = dts.Value.output;
				depth = dts.Value.depth + 1;
			}
			else
			{
				output = new StringBuilder();
				depth = 0;
			}
			output.Append('\n');
			tab();
			output.Append('{');
			++depth;
		}

		public void CloseObject()
		{
			--depth;
			output.Append("} ");
		}

		struct FieldWriter
		{
			public StringBuilder Output;
			public bool Quoted;
			public FieldWriter(StringBuilder sb, bool quoted) {
				Output = sb;
				Quoted = quoted;
			}

			public void ProcessCharacter(char c)
			{
				if (!Quoted)
					if (c == ' ' || c == '{' || c == '}')
					{
						Output.Append('\\');
						Output.Append(c);
						return;
					}

				if (c == '"')
					Output.Append('\\');
				else if (c == '\n')
				{
					Output.Append('\\');
					c = 'n';
				}
				else if (c == '\r')
				{
					Output.Append('\\');
					c = 'r';
				}

				Output.Append(c);
			}
		}

		public void Write(int x) { Write(x.ToString(), false); }
		public void Write(uint x) { Write(x.ToString(), false); }
		public void Write(long x) { Write(x.ToString(), false); }
		public void Write(ulong x) { Write(x.ToString(), false); }
		public void Write(float x) { Write(x.ToString(), false); }
		public void Write(double x) { Write(x.ToString(), false); }
		public void Write(object obj, bool quoted = false) { Write(obj.ToString(), quoted); }
		public void Write(StringPos p) { output.Append(p.String, p.Index, p.String.Length - p.Index); }
		public void Write(string str) { Write(str, true); }

		public void Write(string str, bool quoted = true)
		{
			tab();

			if (str.Length == 0)
			{
				output.Append("\"\" ");  //Force quotes for zero-length fields so that they remain present despite being empty
				return;
			}

			if (quoted)
				output.Append('"');

			var writer = new FieldWriter(output, quoted);
			//foreach (char c in str) { writer.ProcessCharacter(c); }
			Util.ForEach(str, writer.ProcessCharacter);

			if (quoted)
				output.Append("\" ");
		}

		public void tab()
		{
			int count = depth;
			while (count-- > 0) { output.Append('\t'); }
		}

		public override string ToString() { return output.ToString(); }

		public void Write(Serializable obj)
		{
			if (null == obj)
			{
				output.Append("N ");
				return;
			}

			ObjectSerializer subser = new ObjectSerializer(this);
			obj.Serialize(subser);
			subser.CloseObject();
		}
	}

	/*
	public interface ISerializable
	{
		void Serialize(ObjectSerializer ser);
	}
	*/

	//public interface Serializable : ISerializable { }

	public interface Serializable 
	{
		void Serialize(ObjectSerializer ser);

		/*
		public static string ToStringImplementation(Serializable obj)
		{
			ObjectSerializer ser = new ObjectSerializer(null);
			obj.Serialize(ser);
			ser.CloseObject();
			return ser.ToString();
		}

		public override string ToString() { return ToStringImplementation(this); }
		*/
	}		

	public struct SV3D : Serializable
	{
		public Vector3D V;

		public SV3D(ObjectParser parser)
		{
			V = new Vector3D(parser.ParseDouble(), parser.ParseDouble(), parser.ParseDouble());
		}

		public SV3D(Vector3D v) { V = v; }
		public static SV3D New(ObjectParser parser) { return new SV3D(parser); }
		public void Serialize(ObjectSerializer ser)
		{
			ser.Write(V.X);
			ser.Write(V.Y);
			ser.Write(V.Z);
		}

		public static implicit operator Vector3D(SV3D v) { return v.V; }
	}

	public struct SV3 : Serializable
	{
		public Vector3 V;

		public SV3(ObjectParser parser)
		{
			V = new Vector3(parser.ParseFloat(), parser.ParseFloat(), parser.ParseFloat());
		}

		public SV3(Vector3 v) { V = v; }
		public static SV3 New(ObjectParser parser) { return new SV3(parser); }
		public void Serialize(ObjectSerializer ser)
		{
			ser.Write(V.X);
			ser.Write(V.Y);
			ser.Write(V.Z);
		}

		public static implicit operator Vector3(SV3 v) { return v.V; }
	}

	//Serializable position and orientation
	public struct SPAO : Serializable
	{
		public MyPositionAndOrientation PAO;

		public static SPAO New(ObjectParser parser) { return new SPAO(parser); }
		public SPAO(ObjectParser parser)
		{
			PAO = new MyPositionAndOrientation(parser.Parse(SV3D.New), parser.Parse(SV3.New), parser.Parse(SV3.New));
		}

		public SPAO(MatrixD matrix) { PAO = new MyPositionAndOrientation(matrix); }

		public void Serialize(ObjectSerializer ser)
		{
			ser.Write(new SV3D(PAO.Position));
			ser.Write(new SV3(PAO.Forward));
			ser.Write(new SV3(PAO.Up));
		}		
	}
}
