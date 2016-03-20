#from  mutagen.oggvorbis import OggVorbis
import youtube_dl
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
        path = ydl.prepare_filename(data)
        ydl.download([url])
        #TODO: Solve commenting
        #meta = OggVorbis(path)
        #meta.tags["TITLE"] = data["title"];
        #meta.save()
        return path.replace(".tmp",".ogg").split('/')[-1:][0]
