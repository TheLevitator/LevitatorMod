/*
* Levitator Mod ModuleConfig
* 
* This is a serializable object to persist the list of modules which the admin has enabled
*
* Reuse is free as long as you attribute the author.
*
*/

using System;
using System.Collections.Generic;

namespace Levitator.SE.Modding
{
	[Serializable]
	public class ModuleConfig
	{		
		public List<string> Modules;		
	}
}
