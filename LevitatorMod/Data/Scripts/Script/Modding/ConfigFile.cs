/*
* Levitator's Modding Library
* ConfigFile class
* 
* This is how we load XML-serializable state files. Also, we remember whether we read them successfully so that we do not overwrite 
* damaged files which the admin may be attempting to adjust
*
* Reuse is free as long as you attribute the author.
*
* V1.03
*
*/

using Sandbox.ModAPI;
using System;
using System.IO;


namespace Levitator.SE.Modding
{
	public class ConfigFile<T>
	{

		public T Data;
		public bool Valid { get { return mValid; } }
		public string QualifiedName { get { return mQualifiedName; } }

		private bool mValid = false;
		private string mQualifiedName;
		private Type MyType;

		public ConfigFile(string name, Type type)
		{
			mQualifiedName = ModBase.QualifyFilename(name);
			MyType = type;

			if (MyAPIGateway.Utilities.FileExistsInLocalStorage(mQualifiedName, type))
			{
				TextReader tr = MyAPIGateway.Utilities.ReadFileInLocalStorage(mQualifiedName, type);

				try
				{
					Data = MyAPIGateway.Utilities.SerializeFromXML<T>(tr.ReadToEnd());
				}
				finally
				{
					tr.Dispose();
				}
			}
			mValid = true;			
		}

		public void Save()
		{
			if (mValid)
			{								
				string newData = MyAPIGateway.Utilities.SerializeToXML(Data);

				//Shame there are no file rename or copy functions
				if (MyAPIGateway.Utilities.FileExistsInLocalStorage(mQualifiedName, MyType))
					MyAPIGateway.Utilities.DeleteFileInLocalStorage(mQualifiedName, MyType);
				TextWriter tw = MyAPIGateway.Utilities.WriteFileInLocalStorage(mQualifiedName, MyType);

				try
				{
					tw.Write(newData);
				}
				finally
				{
					tw.Dispose();
				}
			}
		}
	}	
}
