using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace MpdDj
{
	public class Downloaders
	{
		const string YT_BIN_WRAP = "/usr/bin/youtube-dl";
		const string FFMPEG_BIN_WRAP = "/usr/bin/ffmpeg";
		const string YT_FFMPEG = " -i \"{0}.{2}\" -vn -y -c:a libvorbis -b:a 192k \"{1}.ogg\"";
		const string YT_YOUTUBEDL = " -f best -o '{0}.%(ext)s' {1}";
		static readonly Regex YoutubeRegex = new Regex("youtu(?:\\.be|be\\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)",RegexOptions.Compiled);
		static readonly Regex YoutubePLRegex = new Regex ("^.*(youtu.be\\/|list=)([^#\\&\\?]*).*", RegexOptions.Compiled);
		static readonly Regex SoundcloudRegex = new Regex ("^https?:\\/\\/(soundcloud.com|snd.sc)\\/(.*)$", RegexOptions.Compiled);

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

		public static string YoutubeGetIDFromURL(string url)
		{
			GroupCollection regg = YoutubeRegex.Match (url).Groups;
			GroupCollection reggpl = YoutubePLRegex.Match (url).Groups;

			if (regg.Count > 1 && regg[1].Value != "playlist") {
				return regg [1].Value;
			} else if (reggpl.Count > 2) {
				return reggpl [2].Value;
			} else {
				return "";
			}
		}

		private static void YoutubeConvert(string filename){
			string[] extensions = new string[2]{"mp4","webm"};
			//It doesn't tell us :/
			string extension = null;
			foreach (string ext in extensions) {
				if (System.IO.File.Exists ("/tmp/" + filename + "." + ext)) {
					extension = ext;
				}
			}

			if(extension == null){
				throw new Exception ("Couldn't find video file");
			}


			Process proc = new Process ();
			proc.StartInfo = new ProcessStartInfo () {
				FileName = FFMPEG_BIN_WRAP,
				WorkingDirectory = "/tmp",
				Arguments = String.Format(YT_FFMPEG,filename,Configuration.Config["mpc"]["music_dir"] + "/" + filename,extension),
				UseShellExecute = false,
				LoadUserProfile = true,
			};
			proc.Start ();
			proc.WaitForExit ();
			if(proc.ExitCode != 0){
				throw new Exception("There was an error transcoding the video");
			}
			System.IO.File.Delete("/tmp/"+filename+"."+extension);
		}

		public static void YoutubeDownload(string url, string filename){
			Process proc = new Process ();
			proc.StartInfo = new ProcessStartInfo () {
				FileName = YT_BIN_WRAP,
				WorkingDirectory = "/tmp",
				Arguments = String.Format(YT_YOUTUBEDL,filename,url),
				UseShellExecute = false,
				LoadUserProfile = true,
				RedirectStandardOutput = true,
			};
			proc.Start ();
			proc.WaitForExit ();
			if(proc.ExitCode != 0){
				throw new Exception("There was an error downloading the video");
			}
			YoutubeConvert (filename);
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

