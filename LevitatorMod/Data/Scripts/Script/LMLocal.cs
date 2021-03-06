﻿/*
* Levitator Mod Local Input Component
*  
* "Simplicity is the ultimate sophistication" - Leonardo da Vinci
*
* Copyright 2015 Levitator
* Reuse is free if you attribute the author
*
* V1.00
*
*/

using Levitator.SE.LevitatorMod.Modules;
using Levitator.SE.Modding.Modules.CommonLocal;
using Levitator.SE.Modding;


namespace Levitator.SE.LevitatorMod
{
	class LMLocal : LocalInputComponent
	{
		public LMLocal(LevitatorMod mod) : base(mod) {	RegisterModule(NHBCLocal.Name, NHBCLocal.New);	}
	}
}
