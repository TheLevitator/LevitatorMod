/*
* Levitator Mod No Hale Bop Cult
* Client Side Module
* 
* This is just in charge of loading the LocalInput module for the home-setting command
*
*/

using Levitator.SE.Modding;
namespace Levitator.SE.LevitatorMod.Modules
{
	class NHBCClient : ModModule
	{
		public const string Name = "NHBC";
		public NHBCClient(ClientComponent client) : base(client) { client.Mod.LocalInputComponent.LoadModule(NHBCLocal.Name); }
		public static NHBCClient New(ModComponent component) { return new NHBCClient((ClientComponent)component); }

		public override void Dispose()
		{
			Component.Mod.LocalInputComponent.UnloadModule(NHBCLocal.Name);
			base.Dispose();
		}
	}
}
