using System;
using System.IO;
using IniParser;
using IniParser.Model;
namespace MpdDj
{
	public class Configuration
	{
		public static IniData Config { get; private set; }
		public static IniData DefaultConfiguration(){
			IniData defaultData = new IniData ();
			SectionData MPC = new SectionData ("mpc");
			SectionData Matrix = new SectionData ("matrix");

			defaultData.Sections.Add (MPC); 
			defaultData.Sections.Add (Matrix); 

			MPC.Keys.AddKey("host","localhost");
			MPC.Keys.AddKey("port","6600");
			MPC.Keys.AddKey("streamurl","http://localhost:8000");
			MPC.Keys.AddKey("music_dir","/var/lib/mpd/music");

			Matrix.Keys.AddKey("host","https://localhost:8448");
			Matrix.Keys.AddKey("user","username");
			Matrix.Keys.AddKey("pass","password");
			Matrix.Keys.AddKey("rooms","#RoomA,#RoomB:localhost,#RoomC");
			return defaultData;
		}

		public static void ReadConfig(string cfgpath){
			if (File.Exists (cfgpath)) {
				FileIniDataParser parser = new FileIniDataParser ();
				Config = parser.ReadFile (cfgpath);
			} else {
				Console.WriteLine ("[Warn] The config file could not be found. Using defaults");
				Config = DefaultConfiguration ();
			}
		}
	}
}

