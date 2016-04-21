using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Linq;
using System.Threading;
namespace MpdDj
{	
	public struct MPCCurrentSong
	{
		public string file;
		public DateTime date;
		public string Artist;
		public string Title;
		public string Album;
		public string Track;
		public string Date;
		public string Genre;
		public string Disc;
		public int Time;
		public int Pos;
		public int Id;
	}

	public struct MPCStatus
	{
		public int volume;
		public bool repeat;
		public bool random;
		public bool single;
		public bool consume;
		public int playlist;
		public int playlistlength;
		public float mixrampdb;
		public string state;
		public int song;
		public int songid;
		public string time;
		public float elapsed;
		public int bitrate;
		public string audio;
		public int nextsong;
		public int nextsongid;
	}

	public class MPC
	{
		TcpClient client;
		NetworkStream stream;
		public MPCStatus lastStatus { get; private set; }
		int port;
		string host;
		Mutex client_mutex;
		public MPC (string host, string port)
		{
			this.host = host;
			this.port = int.Parse (port);
			client_mutex = new Mutex ();
			OpenConnection ();
		}

		private T FillStruct<T>(string data){
			string[] lines = data.Split ('\n');
			object o = Activator.CreateInstance<T> ();
			foreach (string line in lines) {
				string[] keyval = line.Split (new string[1]{": "}, 2,StringSplitOptions.RemoveEmptyEntries);
				if (keyval.Length == 2) {
					System.Reflection.FieldInfo f = typeof(T).GetField (keyval [0]);
					if (f != null) {
						object value;
						if (f.FieldType  == typeof(float)) {
							value = float.Parse (keyval [1]);
						} else if (f.FieldType == typeof(int)) {
							value = int.Parse (keyval [1]);
						} else if (f.FieldType == typeof(bool)) {
							value = int.Parse (keyval [1]) == 1;
						} else if (f.FieldType == typeof(DateTime)) {
							value = DateTime.Parse (keyval [1]);
						} else {
							value = keyval [1];
						}
						f.SetValue(o,value);
					}
				}
			}
			return (T)o;
		}

		private void OpenConnection(){
			client = new TcpClient (host,port);
			stream = client.GetStream ();
			byte[] buff = new byte[6];
			stream.Read (buff, 0, buff.Length);
			if (Encoding.UTF8.GetString (buff) != "OK MPD") {
				throw new Exception ("Connection is not a MPD stream");
			}
			//Eat the rest
			while (stream.DataAvailable) {
				stream.ReadByte ();
			}
		}

		private string Send(string data, bool wait = false){
			if (!client_mutex.WaitOne (30000)) {
				throw new Exception ("Timed out waiting for the mutex to become avaliable for the mpc client.");
			}
			OpenConnection ();

			byte[] bdata = Encoding.UTF8.GetBytes (data+"\n");
			stream.Write (bdata,0,bdata.Length);
			List<byte> buffer = new List<byte>();
			while (!stream.DataAvailable && wait) {
				System.Threading.Thread.Sleep (100);
			}

			while (stream.DataAvailable) {
				buffer.Add ((byte)stream.ReadByte ());
			}
			string sbuffer = Encoding.UTF8.GetString (buffer.ToArray());

			client.Close ();
			stream.Dispose ();
			client_mutex.ReleaseMutex ();
			return sbuffer;
		}

		public void Play(){
			Send ("play");
		}

		public void Next(){
			Send ("next");
		}

		public void Previous(){
			Send ("prev");
		}

		public void AddFile(string file){
			Send ("add " + file);
		}

		public void Idle(string subsystem){
			Send ("idle " + subsystem,true);
		}

		public string[] Playlist(){
			string playlist = Send ("playlist");
			List<string> newsongs = new List<string> ();
			foreach (string song in playlist.Split ('\n')) {
				int indexof = song.IndexOf (' ');
				if (indexof != -1) {
					newsongs.Add(song.Substring (indexof + 1));
				}
			}
			return newsongs.ToArray();
		}

		public void Status(){
			string sstatus = Send ("status");
			MPCStatus status = FillStruct<MPCStatus> (sstatus);
			lastStatus = status;
		}

		public MPCCurrentSong CurrentSong(){
			string result = Send("currentsong");
			MPCCurrentSong song = FillStruct<MPCCurrentSong>(result);
			return song;
		}

		public void RequestLibraryUpdate(){
			Send ("update");
		}
	}

	public static class TcpExtensions{
		public static TcpState GetState(this TcpClient tcpClient)
		{
			var foo = IPGlobalProperties.GetIPGlobalProperties()
				.GetActiveTcpConnections()
				.SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));
			return foo != null ? foo.State : TcpState.Unknown;
		}
	}


}


