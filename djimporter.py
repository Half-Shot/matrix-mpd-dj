#from  mutagen.oggvorbis import OggVorbis
import youtube_dl
from os.path import getctime, basename,exists
from glob import iglob
from threading import Lock
from time import sleep

yt_mutex = Lock()
__yt_callback = None

def yt_hook(status):
    global __yt_callback
    if status['status'] == "finished":
        fname = status['filename'].replace(".tmp",".ogg")
        attempts = 0
        while not exists(fname) and attempts < 10:
            attempts += 1
            sleep(0.5)

        if exists(fname):
            __yt_callback(basename(fname))

def download_youtube(url,outputdir,callback):
    global __yt_callback
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
            path = False
            if 'entries' not in data.keys():
                path = basename(ydl.prepare_filename(data).replace(".tmp",".ogg"))

            status = (ydl.download([url]) == 0)
    finally:
        yt_mutex.release()
    return status, path
