#from  mutagen.oggvorbis import OggVorbis
import youtube_dl
from os.path import getctime, basename
from glob import iglob
YT_OUTPUTDIR="/var/lib/mpd/music/"

def download_youtube(url):
    ydl_opts = {
    'format': 'bestaudio/best',
    'outtmpl': YT_OUTPUTDIR+'%(title)s.tmp',
    'add-metadata':True,
    'postprocessors': [{
            'key': 'FFmpegExtractAudio',
            'preferredcodec': 'vorbis',
            'preferredquality': '192',
        }]
    }
    path = None
    with youtube_dl.YoutubeDL(ydl_opts) as ydl:
        data = ydl.extract_info(url,False)
        paths = []
        if 'entries' in data.keys():
            path = basename(ydl.prepare_filename(data['entries'][0]).replace(".tmp",".ogg"))
            paths.append(path)
        else:
            paths.append(basename(ydl.prepare_filename(data).replace(".tmp",".ogg")))

        ydl.download([url])
        return paths
