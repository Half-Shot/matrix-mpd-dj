using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Xml;
using Newtonsoft.Json.Linq;
namespace MpdDj
{
	public class Downloaders
	{
		const string YT_BIN_WRAP = "/usr/bin/youtube-dl";
		const string FFMPEG_BIN_WRAP = "/usr/bin/ffmpeg";
		//const string YT_FFMPEG = " -i \"{0}.{2}\" -vn -y -c:a libvorbis -b:a 192k \"{1}.ogg\"";
		const string YT_YOUTUBEDL = " -x --audio-format \"vorbis\" -f best -o '{0}.%(ext)s' {1}";
		static readonly Regex YoutubeRegex = new Regex("youtu(?:\\.be|be\\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)",RegexOptions.Compiled);
		static readonly Regex YoutubePLRegex = new Regex ("^.*(youtu.be\\/|list=)([^#\\&\\?]*).*", RegexOptions.Compiled);
		static readonly Regex SoundcloudRegex = new Regex ("^https?:\\/\\/(soundcloud.com|snd.sc)\\/(.*)$", RegexOptions.Compiled);


		public static string GetSongNameByLyric(string lyric){
			const string URL = "http://api.chartlyrics.com/apiv1.asmx/SearchLyricText?lyricText={0}";
			string finalUrl = string.Format(URL,Uri.EscapeUriString(lyric));
			string result = "";
			using (System.Net.WebClient client = new System.Net.WebClient ()) {
				result = client.DownloadString(finalUrl);
			}
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(result);
			XmlElement firstSong = (XmlElement)doc.ChildNodes[1].FirstChild;
			if((firstSong).GetAttribute("xsi:nil") != ""){
				return null;
			}
			else
			{
				string artist = firstSong.GetElementsByTagName("Artist")[0].InnerText;
				string song = firstSong.GetElementsByTagName("Song")[0].InnerText;
				return string.Format("{0} {1}",artist,song);
			}

			
		}

		public static void GenericDownload(string url,string filename){
			string[] allowedMimetypes = Configuration.Config ["file"] ["mimetypes"].Split (' ');
			using (HttpClient client = new HttpClient ()) {
				Task<HttpResponseMessage> msg = client.SendAsync (new HttpRequestMessage (HttpMethod.Head, url));
				msg.Wait ();
				IEnumerable<String> types;
				IEnumerable<String> lengths;

				if (msg.Result.StatusCode != System.Net.HttpStatusCode.OK) {
					throw new Exception ("Server gave a " + msg.Result.StatusCode);
				}

				if (msg.Result.Content.Headers.TryGetValues ("Content-Type", out types)) {
					if (!types.Any (x => allowedMimetypes.Contains (x))) {
						throw new Exception ("Filetype not supported");
					}
					if (msg.Result.Content.Headers.TryGetValues ("Content-Length", out lengths)) {
						float length = int.Parse (lengths.First()) / (float)Math.Pow(1024,2);
						float maxlength = float.Parse (Configuration.Config ["file"] ["size_limit"]);
						if (length > maxlength) {
							throw new Exception ("File is over " + maxlength + "MBs in size");
						}
					} else {
						throw new Exception ("Cannot gauge content size from headers. Bailing");
					}
				}
				else
				{
					throw new Exception("Server does not state a Content-Type. Bailing.");
				}
			}


			using (System.Net.WebClient client = new System.Net.WebClient ()) {
				try {
					string fname = System.IO.Path.Combine(Configuration.Config ["mpc"] ["music_dir"],filename);
					System.IO.FileInfo finfo = new System.IO.FileInfo(fname);
					if(!client.DownloadFileTaskAsync (url,finfo.FullName).Wait(TimeSpan.FromSeconds(15))){
						throw new Exception("File took too long to download");
					}
				} catch (Exception e) {
					Console.WriteLine ("Issue downloading file", e);
					throw new Exception ("Couldn't download file");
				}
			}
		}

		public static string GetYoutubeURLFromSearch(string terms){
			const string URL_FORMAT = "https://www.googleapis.com/youtube/v3/search?part=snippet&q={0}&type=video&maxResults=1&key={1}&videoCategoryId=10";
			string url = string.Format(URL_FORMAT,Uri.EscapeUriString(terms),Configuration.Config["youtube"]["apikey"]);
			JObject obj;
			using (System.Net.WebClient client = new System.Net.WebClient ()) {
				try {
					string data = client.DownloadString(url);
					obj = JObject.Parse(data);
				} catch (Exception e) {
					Console.WriteLine ("Issue with YT API", e);
					throw new Exception ("Couldn't do search.");
				}
			}	
			JToken[] items = obj.GetValue("items").ToArray();
			if(items.Length == 0){
				return null;
			}
			else
			{
				return "https://youtube.com/watch?v=" + items[0].Value<JToken>("id").Value<string>("videoId");
			}
		}

		public static string YoutubeGetIDFromURL(string url)
		{
			GroupCollection regg = YoutubeRegex.Match (url).Groups;
			GroupCollection reggpl = YoutubePLRegex.Match (url).Groups;
			if (regg.Count > 1 && regg[1].Value != "playlist") {
				return regg [1].Value;
			} else if (reggpl.Count > 2) {
				return reggpl [2].Value;
			} else if (SoundcloudRegex.IsMatch (url)){
				return url;
			} else {
				return "";
			}
		}

//		private static void YoutubeConvert(string filename){
//			string[] extensions = new string[2]{"mp4","webm"};
//			//It doesn't tell us :/
//			string extension = null;
//			foreach (string ext in extensions) {
//				if (System.IO.File.Exists ("/tmp/" + filename + "." + ext)) {
//					extension = ext;
//				}
//			}
//
//			if(extension == null){
//				throw new Exception ("Couldn't find video file");
//			}
//
//
//			Process proc = new Process ();
//			proc.StartInfo = new ProcessStartInfo () {
//				FileName = FFMPEG_BIN_WRAP,
//				WorkingDirectory = "/tmp",
//				Arguments = String.Format(YT_FFMPEG,filename,Configuration.Config["mpc"]["music_dir"] + "/" + filename,extension),
//				UseShellExecute = false,
//				LoadUserProfile = true,
//			};
//			proc.Start ();
//			proc.WaitForExit ();
//			if(proc.ExitCode != 0){
//				throw new Exception("There was an error transcoding the video");
//			}
//			System.IO.File.Delete("/tmp/"+filename+"."+extension);
//		}

		public static void YoutubeDownload(string url, string filename){
			Process proc = new Process ();
			proc.StartInfo = new ProcessStartInfo () {
				FileName = YT_BIN_WRAP,
				WorkingDirectory = Configuration.Config["mpc"]["music_dir"] ,
				Arguments = String.Format(YT_YOUTUBEDL,filename,url),
				UseShellExecute = false,
				LoadUserProfile = true,
				RedirectStandardOutput = true,
			};
			proc.Start ();
			proc.WaitForExit ();
			if(proc.ExitCode != 0){
				throw new Exception("There was an error downloading/transcoding the video");
			}
			//YoutubeConvert (filename);
		}

		public static JObject[] YoutubeGetData(string url){
			string id = YoutubeGetIDFromURL (url);
			if (id == "") {
				throw new Exception ("Bad url.");
			}
			Process proc = new Process ();
			proc.StartInfo = new ProcessStartInfo () {
				FileName = YT_BIN_WRAP,
				Arguments = "-j " + id,
				UseShellExecute = false,
				LoadUserProfile = true,
				RedirectStandardOutput = true,

			};
			proc.Start ();
			string data = "";
			while (!proc.HasExited) {
					data += proc.StandardOutput.ReadToEnd ();
					System.Threading.Thread.Sleep (50);
			}
			proc.Dispose ();
			if (data == "") {
				throw new Exception ("Bad url.");
			}
			List<JObject> videos = new List<JObject> ();
			foreach(string line in data.Split('\n')){
				if(string.IsNullOrWhiteSpace(line))
					continue;
				videos.Add(JObject.Parse(line));
			}
			return videos.ToArray ();
		}
	}
}

