using System;
using MatrixSDK.Client;
using MatrixSDK.Structures;
namespace MpdDj
{
	class MainClass
	{
		public static MatrixClient client;
		public static void Main (string[] args)
		{
			Console.WriteLine ("Reading INI File");
			string cfgpath;
			if (args.Length > 1) {
				cfgpath = System.IO.Path.GetFullPath (args [1]);
			} else {
				cfgpath = "~/.config/mpddj.ini";
			}
			Configuration.ReadConfig (cfgpath);

			Console.WriteLine ("Connecting to Matrix");
			client = new MatrixClient (Configuration.Config ["mpc"] ["host"]);
			Console.WriteLine("Connected. Logging in");
			client.LoginWithPassword (Configuration.Config ["mpc"] ["user"], Configuration.Config ["mpc"] ["pass"]);
			Console.WriteLine("Logged in OK");
			Console.WriteLine("Joining Rooms:");
			foreach (string roomid in Configuration.Config ["mpc"] ["rooms"].Split(',')) {
				MatrixRoom room = client.JoinRoom (roomid);
				room.OnMessage += Room_OnMessage;
				Console.WriteLine("\tJoined " + roomid);
			}

		}

		static void Room_OnMessage (MatrixRoom room, MatrixSDK.Structures.MatrixEvent evt)
		{
			if (evt.age > 3000) {
				return; // Too old
			}
			string msg = ((MatrixMRoomMessage)evt.content).body;

			if (msg.StartsWith ("!mpddj")) {
				msg = msg.Substring (7);
				Console.WriteLine ("Got message okay");
			}

		}
	}
}
