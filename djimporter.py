#from  mutagen.oggvorbis import OggVorbis
import youtube_dl
from os.path import getctime, basename,exists
from glob import iglob
from threading import Lock
from time import sleep

yt_mutex = Lock()
__yt_callback = None
__yt_lastfile = ""
def yt_hook(status):
    global __yt_callback
    global __yt_lastfile
    if status['status'] == "finished":
        print("Finished downloading video")
        fname = status['filename'].replace(".tmp",".ogg")
        if exists(__yt_lastfile):
            __yt_callback(__yt_lastfile)
        __yt_lastfile = basename(fname)

def download_youtube(url,outputdir,callback):
    global __yt_callback
    global __yt_lastfile
    yt_mutex.acquire()
    path = False
    status = False
    __yt_callback = callback
    try:
        ydl_opts = {
        'format': 'bestaudio/best',
        'outtmpl': outputdir+'%(title)s.tmp',
        'add-metadata':True,
        'postprocessors': [{
                'key': 'FFmpegExtractAudio',
                'preferredcodec': 'vorbis',
                'preferredquality': '192',
            }],
        'progress_hooks':[yt_hook]
        }
        with youtube_dl.YoutubeDL(ydl_opts) as ydl:

            data = ydl.extract_info(url,False)
            if 'entries' not in data.keys():
                path = basename(ydl.prepare_filename(data).replace(".tmp",".ogg"))
            status = ydl.download([url])
            print(status)
            status = (status == 0)
            __yt_callback(__yt_lastfile)
    finally:
        yt_mutex.release()
    return status, path
