/*
*
* This very simple installer is just here to make it easy to swap SBMI files by changing the build Configuration in VS
* It updates the Space Engineers Mods directory
*
* Levitator
*
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace Installer
{
	class Program
	{
		public  const string InstallFlag = "INSTALLOK";
		public const string ModFolder = "%appdata%\\SpaceEngineers\\Mods\\";

		static int Main(string[] args)
		{
			try
			{
				string SolutionDir = args[0];
				string Configuration = args[1];

				Directory.SetCurrentDirectory(SolutionDir);
				List<ModInfo> infos = EnumMods(Configuration);

				if (File.Exists(InstallFlag))
				{
					DoInstall(infos);
					Console.WriteLine("Done.");
				}
				else
				{
					Console.WriteLine("\n\n***************************************************************************************************************************");
					Console.WriteLine("Solution build is intended solely for error-checking purposes and to perform an install as a 'local mod' prior to deployment.");
					Console.WriteLine("The game does not use the binaries. It compiles from source.");
					Console.WriteLine("To enable local mod installation, create an empty file named '" + Path.Combine(SolutionDir, InstallFlag) + "'");
					Console.WriteLine("Then run the build again. The target directories in your Mods subfolder listed below will be OVERWRITTEN if they exist: \n");
					foreach (var info in EnumMods(Configuration)) { Console.WriteLine("\t" + info.ModSubFolder); }

					Console.WriteLine("\nThe destination folder will be suffixed by the name of the build configuration if you use other than 'Release'");
					Console.WriteLine("******************************************************************************************************************************\n\n");
					return 0;
				}
			}
			catch (Exception x)
			{
				Console.WriteLine("Something bad happened installing local mods: " + x);
				return -1;
			}
			return 0;
		}

		struct ModInfo {
			public string Name;
			public string ModSubFolder;
			public string SBMIPath;
		}

		private static List<ModInfo> EnumMods(string configuration)
		{
			var infos = new List<ModInfo>();
			var paths = Directory.GetFiles( Path.Combine("SBMI", configuration) );
			string ModSuffix;

			if (configuration == "Release") ModSuffix = "";
			else ModSuffix = configuration;

			foreach (var path in paths)
			{
				var name = Path.GetFileName(path);
                infos.Add( new ModInfo() { Name = name, ModSubFolder = name + ModSuffix, SBMIPath = path } );
			}
			return infos;
		}

		private static void DoInstall(List<ModInfo> infos) {
			string ModFolderExp = Environment.ExpandEnvironmentVariables(ModFolder);
			string ModPath;
			foreach (var info in infos)
			{
				ModPath = Path.Combine(ModFolderExp, info.ModSubFolder);
				Console.WriteLine("Installing mod: '" + info.Name + "' -> '" + ModPath + "'");
				if (Directory.Exists(ModPath)) Directory.Delete(ModPath, true);
				RecursiveCopy(info.Name, ModPath);
				File.Copy(info.SBMIPath, Path.Combine(ModPath, "modinfo.sbmi")); //Metadata that tells SE what Workshop item# to publish to
				File.Copy("LICENSE.txt", Path.Combine(ModPath, "LICENSE.txt"));
			}
		}

		private static void RecursiveCopy(string source, string dest)
		{
			Directory.CreateDirectory(dest);
			foreach (var file in Directory.GetFiles(source))
			{
				File.Copy(file, Path.Combine(dest,  Path.GetFileName(file)));
			}

			foreach (var dir in Directory.GetDirectories(source))
			{
				RecursiveCopy(dir, Path.Combine(dest, Path.GetFileName(dir)));
			}
		}
	}
}
