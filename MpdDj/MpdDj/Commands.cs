using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using MatrixSDK.Client;
namespace MpdDj
{
	[AttributeUsage(AttributeTargets.Method)]
	public class BotCmd : Attribute{
		public readonly string CMD;
		public readonly string[] BeginsWith;
		public BotCmd(string cmd,params string[] beginswith){
			CMD = cmd;
			BeginsWith = beginswith;
		}
	}

	public class Commands
	{
		[BotCmd("shuffle")]
		public static void Shuffle(string cmd,string sender, MatrixRoom room){
			Console.WriteLine("Shuffle");
		}

		[BotCmd("ping")]
		public static void Ping(string cmd, string sender, MatrixRoom room){
			room.SendMessage ("pong!");
		}

		[BotCmd("current")]
		public static void GetSongName(string cmd,string sender,MatrixRoom room){
			MPCCurrentSong song = Program.MPCClient.CurrentSong ();
			if (song.file != null) {
				FileInfo file = new FileInfo (song.file);
				string name = file.Name.Replace (file.Extension, "");
				name = new string(System.Text.Encoding.UTF8.GetChars (Convert.FromBase64String (name)));
				room.SendMessage (name);
			} else {
				room.SendMessage ("Nothing is currently playing");
			}
		}
			

		[BotCmd("next")]
		public static void NextTrack(string cmd, string sender, MatrixRoom room){
			Program.MPCClient.Next ();
		}

		[BotCmd("help")]
		public static void Help(string cmd, string sender, MatrixRoom room){

		}
			
		[BotCmd("","http://","https://","youtube.com","youtu.be","soundcloud.com")]
		public static void DownloadTrack(string cmd, string sender, MatrixRoom room)
		{
			try
			{
				List<string[]> videos = new List<string[]>();
				if (Downloaders.YoutubeGetIDFromURL (cmd) != "") {
					videos = DownloadYoutube (cmd, sender, room);
				}
				else if(Uri.IsWellFormedUriString (cmd, UriKind.Absolute)){
					videos = new List<string[]>();
					videos.Add(DownloadGeneric (cmd, sender, room));
				}
				else
				{
					room.SendMessage ("Sorry, that type of URL isn't supported right now :/");
					return;
				}

				Program.MPCClient.RequestLibraryUpdate();
				//Program.MPCClient.Idle("update");//Wait for it to start
				Program.MPCClient.Idle("update");//Wait for it to finish

				foreach(string[] res in videos){
					Program.MPCClient.AddFile(res[0]);
				}
				string[] playlist = Program.MPCClient.Playlist();
				//Console.WriteLine(string.Join("\n",playlist));
				int position = playlist.Length-(videos.Count-1);
				//ffConsole.WriteLine(position);
				if(position == 1){
					Program.MPCClient.Play();
					room.SendMessage("Started playing " + videos[0][1] + " | " + Configuration.Config["mpc"]["streamurl"]);
				}
				else
				{
					room.SendMessage(videos[0][1] + " has been queued at position "+position+".");
				}

			}
			catch(Exception e){
				room.SendMessage ("There was an issue with that request, "+sender+": " + e.Message);
				Console.Error.WriteLine (e);
			}
		}

		public static string[] DownloadGeneric(string cmd, string sender, MatrixRoom room){
			Uri uri;
			if (!Uri.TryCreate (cmd, UriKind.Absolute,out uri)) {
				throw new Exception ("Not a url :(");
			}
			FileInfo info = new FileInfo (uri.Segments.Last ());
			string filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(info.Name))+info.Extension;
			Downloaders.GenericDownload (cmd, filename);
			return new string[2] {filename, uri.Segments.Last ()};
		}

		public static List<string[]> DownloadYoutube(string cmd, string sender, MatrixRoom room){
			JObject[] videos = Downloaders.YoutubeGetData (cmd);
			List<string[]> output = new List<string[]>(videos.Length);
			List<Task> tasks = new List<Task> ();
			foreach(JObject data in videos){
				//Check Length
				int seconds = data["duration"].ToObject<int>();
				int max = int.Parse(Configuration.Config["youtube"]["maxlength"]);
				if(seconds > max){
					throw new Exception("Video exceeds duration limit of " + Math.Round(max / 60f,1) + " minutes");
				}
				string filename = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data["fulltitle"].ToObject<string>()));
				Task t = new Task( () => {Downloaders.YoutubeDownload(data["webpage_url"].ToObject<string>(),filename);});
				t.Start ();
				tasks.Add(t);
				output.Add(new string[2]{filename + ".ogg",data["title"].ToObject<string>()});
			}
			System.Threading.Tasks.Task.WaitAll (tasks.ToArray(),TimeSpan.FromSeconds(20*videos.Length));
			return output;
		}

		[BotCmd("","stream")]
		public static void StreamUrl(string cmd, string sender, MatrixRoom room){
			room.SendMessage(Configuration.Config["mpc"]["streamurl"]);
		}
		[BotCmd("playlist")]
		public static void PlaylistDisplay(string cmd, string sender, MatrixRoom room){
			string[] files = Program.MPCClient.Playlist ();
			string output = "▶ ";
			if (files.Length > 0) {
				for (int i = 0; i < files.Length; i++) {
					if (i > 4)
						break;
					string file = files [i].Substring (0, files [i].Length - 4) + '\n';//Remove the extension
					file = new string(System.Text.Encoding.UTF8.GetChars (Convert.FromBase64String (file))); 
					output += file + "\n";
				}
				room.SendMessage (output);
			} else {
				room.SendMessage ("The playlist is empty");
			}

		}

	}
}

