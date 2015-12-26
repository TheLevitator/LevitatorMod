/*
* Levitator's Space Engineers Serialization Library
* Polymorphism System
* 
* This is so that we can unserialize derived classes
* It's not used because we use command dictionaries instead.
* If we have to do a lookup for the command handler anyway,
* no point in doing another lookup to construct the message too.
* Might come in handy later, though.
*
* V1.00
*
*/

using System;
using System.Collections.Generic;
using Levitator.SE.Serialization;

namespace Scripts.Serialization
{
	//This is a bit of a pain without reflection available
	public abstract class Polymorphic<T> : Serializable
	{
		public static readonly ClassRegistry<Polymorphic<T>> Subclasses = new ClassRegistry<Polymorphic<T>>();

		private string ClassId;

		public static Polymorphic<T> PolyConstructor(ObjectParser parser)
		{
			string id = parser.ParseField();
			Polymorphic<T> result = Subclasses.GetConstructor(id)(parser);
			result.ClassId = id;
			return result;
		}

		//Call this first when subclassing
		public virtual void Serialize(ObjectSerializer serializer){ serializer.Write(ClassId); }

		static public void Register(string id, Func<ObjectParser, Polymorphic<T>> constructor) { Subclasses.Register(id, constructor); }
	}

	public class ClassRegistry<T>
	{
		private Dictionary<string, Func<ObjectParser, T>> Constructors = new Dictionary<string, Func<ObjectParser, T>>();
		virtual public void Register(string id, Func<ObjectParser, T> constructor) { Constructors.Add(id, constructor); }
		virtual public void Unregister(string id) { Constructors.Remove(id); }
		virtual public Func<ObjectParser, T> GetConstructor(string classId)
		{
			Func<ObjectParser, T> result = null;
			Constructors.TryGetValue(classId, out result);
			return result;
		}
	}

	//This will probably never work, as System.Type is supposed to be sandboxed out
	/*
	public class PolymorphicAttribute
	{
		public PolymorphicAttribute(Func<ObjectParser, Serializable> constructor, Action<string, Func<ObjectParser, Serializable>> registrator)
		{
		}
	}

	public class RegisterAttribute : Attribute
	{
		public readonly string ClassId;
		public RegisterAttribute(string id, Type poly)
		{

			Type tFunc = typeof(Func<,>);
			Type tConstruct = tFunc.MakeGenericType(new Type[] { typeof(ObjectParser), poly } );			
			MethodInfo miHandler = poly.GetMethod("Constructor", new Type[] { typeof(ObjectParser) } );
			Delegate d = Delegate.CreateDelegate(tConstruct, miHandler);
			poly.InvokeMember("Register", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, null, null, new object[] { id, d });
			
			MethodInfo miRegister = poly.GetMethod("Register",
				BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
				null,
				CallingConventions.Any,
				new Type[] { typeof(string), tConstruct },
				null);
			if (null != miRegister) throw new Exception("MANY SUCCESSS!");


			ClassId = id;
			throw new Exception("!!!????");			
		}
	}
	*/
}
