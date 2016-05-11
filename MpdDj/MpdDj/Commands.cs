using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using MatrixSDK.Client;
using MatrixSDK.Structures;
using System.Reflection;
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

    [AttributeUsage(AttributeTargets.Method)]
    public class BotFallback : Attribute {
		
	}

    [AttributeUsage(AttributeTargets.Method)]
    public class BotHelp : Attribute {
        public readonly string HelpText;
        public BotHelp(string help){
            HelpText = help;
        }
    }

	public class Commands
	{
		//[BotCmd("shuffle")]
		//public static void Shuffle(string cmd,string sender, MatrixRoom room){
		//	Console.WriteLine("Shuffle");
		//}

		[BotCmd("ping")]
        [BotHelp("Ping the server and get the delay.")]
		public static void Ping(string cmd, string sender, MatrixRoom room){
			room.SendMessage ("Pong at" + DateTime.Now.ToLongTimeString());
		}

		[BotCmd("current")]
        [BotHelp("Get the current song title.")]
		public static void GetSongName(string cmd,string sender,MatrixRoom room){
			MPCCurrentSong song = Program.MPCClient.CurrentSong ();
			Program.MPCClient.Status();
			if (song.file != null) {
				FileInfo file = new FileInfo (song.file);
				string name = file.Name.Replace (file.Extension, "");
				name = new string(System.Text.Encoding.UTF8.GetChars (Convert.FromBase64String (name)));

				string[] time = Program.MPCClient.lastStatus.time.Split(':');
				int elapsed = int.Parse(time[0]);
				int total = int.Parse(time[1]);
				name += String.Format(" {0}:{1}/{2}:{3}", elapsed/60,elapsed%60,total/60,total%60);

				room.SendNotice (name);
			} else {
				room.SendNotice ("Nothing is currently playing");
			}
		}
			

		[BotCmd("next")]
        [BotHelp("Skip current song.")]
		public static void NextTrack(string cmd, string sender, MatrixRoom room){
			Program.MPCClient.Next ();
		}

		[BotCmd("help")]
        [BotHelp("This help text.")]
		public static void Help(string cmd, string sender, MatrixRoom room){
            string helptext = "";
            foreach(MethodInfo method in typeof(Commands).GetMethods(BindingFlags.Static|BindingFlags.Public)){
                BotCmd c = method.GetCustomAttribute<BotCmd> ();
                BotHelp h= method.GetCustomAttribute<BotHelp> ();
				
                if (c != null) {
                    helptext += String.Format("<p><strong>{0}</strong> {1}</p>",c.CMD, h != null ? System.Web.HttpUtility.HtmlEncode(h.HelpText) : "");
                }
            }
            MMessageCustomHTML htmlmsg = new MMessageCustomHTML();
            htmlmsg.body = helptext.Replace("<strong>","").Replace("</strong>","").Replace("<p>","").Replace("</p>","\n");
            htmlmsg.formatted_body = helptext;
            room.SendMessage(htmlmsg);
       }
		
		[BotCmd("search")]
		[BotFallback()]
        [BotHelp("Get the first youtube result by keywords.")]
		public static void SearchYTForTrack(string cmd, string sender, MatrixRoom room){
			string query = cmd.Replace("search ","");
			if(string.IsNullOrWhiteSpace(query)){
				return;
			}
			try
			{
				string url = Downloaders.GetYoutubeURLFromSearch(query);
				if(url != null){
					DownloadTrack(url,sender,room);
				}
				else
				{
					throw new Exception("No videos matching those terms were found");
				}
			}
			catch(Exception e){
				room.SendNotice ("There was an issue with that request, "+sender+": " + e.Message);
				Console.Error.WriteLine (e);
			}
			
		}

		[BotCmd("[url]","http://","https://","youtube.com","youtu.be","soundcloud.com")]
        [BotHelp("Type a youtube/soundcloud/file url in to add to the playlist.")]
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
					room.SendNotice ("Sorry, that type of URL isn't supported right now :/");
					return;
				}

				Program.MPCClient.RequestLibraryUpdate();
				//Program.MPCClient.Idle("update");//Wait for it to start
				Program.MPCClient.Idle("update");//Wait for it to finish

				foreach(string[] res in videos){
					Program.MPCClient.AddFile(res[0]);
				}

				Program.MPCClient.Status();

				#if DEBUG
				Console.WriteLine(JObject.FromObject(Program.MPCClient.lastStatus));
				#endif

				int position = Program.MPCClient.lastStatus.playlistlength;
				if(position == 1){
					Program.MPCClient.Play();
					room.SendNotice("Started playing " + videos[0][1] + " | " + Configuration.Config["mpc"]["streamurl"]);
				}
				else
				{
					room.SendNotice(videos[0][1] + " has been queued at position "+position+".");
				}

			}
			catch(Exception e){
				room.SendNotice ("There was an issue with that request, "+sender+": " + e.Message);
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

		[BotCmd("stream")]
        [BotHelp("Get the url of the stream.")]
		public static void StreamUrl(string cmd, string sender, MatrixRoom room){
			room.SendNotice(Configuration.Config["mpc"]["streamurl"]);
		}

		[BotCmd("lyrics")]
        [BotHelp("Search by lyric")]
		public static void LyricSearch(string cmd, string sender, MatrixRoom room){
			string suggestion = Downloaders.GetSongNameByLyric(cmd.Replace("lyrics ",""));
			if(suggestion == null){
				room.SendNotice("I couldn't find any songs with that lyric :(");
			}
			else
			{
				room.SendNotice(String.Format("Matched '{0}'. Checking Youtube for it",suggestion));
				SearchYTForTrack("search "+suggestion,sender,room);
			}
		}

		[BotCmd("lyric")]
        [BotHelp("You fear the letter 's'? Aww babe, I'll fix that for you <3")]
		public static void LyricSearchAlias(string cmd, string sender, MatrixRoom room){
			LyricSearch(cmd,sender,room);
		}

		[BotCmd("playlist")]
        [BotHelp("Display the the shortened playlist.")]
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
				room.SendNotice ("The playlist is empty");
			}
		}

		

	}
}

