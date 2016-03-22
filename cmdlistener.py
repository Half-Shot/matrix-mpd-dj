#!/usr/bin/env python3

from djimporter import download_youtube
from matrix_client.client import MatrixClient
from mpc.mpc import MPCClient
from time import time, sleep
from queue import Queue
import traceback


class CmdListener:
    rooms = {}
    mpc = None
    client = None
    stream_url = ""
    cmd_queue = None
    music_dir = None
    def __init__(self,config):
        self.mpc = MPCClient(config["mpc"]["host"],config["mpc"]["port"])
        self.music_dir = config["mpc"]["music_dir"]
        self.cmd_queue = Queue()

        try:
            self.mpc.current()
        except:
            raise Exception("An error occured while connecting to the mpd server.")
            return

        try:
            self.client = MatrixClient(config["matrix"]["host"])
        except:
            raise Exception("An error occured while connecting to the matrix server!")
            return


        self.stream_url = config["mpc"]["streamurl"]
        try:
            self.client.login_with_password(config["matrix"]["user"],config["matrix"]["pass"])
        except MatrixRequestError as e:
            print(e)
            if e.code == 403:
                print("Bad username or password.")
                sys.exit(4)
            else:
                print("Check your sever details are correct.")
                sys.exit(3)

        MTX_ROOMS = config["matrix"]["rooms"].split(",")

        for sroom in MTX_ROOMS:
            room = self.client.join_room(sroom)
            room.add_listener(self.__on_cmd)
            self.rooms[room.room_id] = room

    def run(self):
        self.client.start_listener_thread()
        while True:
            event = self.cmd_queue.get()
            if event is None:
                continue;
            else:
                cmd = event['content']['body']
                body = cmd.lower()
                if body.startswith('mpddj:') or body.startswith('!mpddj'):
                    self.__parse_command(body[6:],event,cmd[6:])
                elif body.startswith('mpd dj:'):
                    self.__parse_command(body[7:],event,cmd[7:])

    def __on_cmd(self,event):
        if event['type'] == "m.room.message" and event['content']['msgtype'] == "m.text":
            if event['age'] < 5000:
                self.cmd_queue.put(event)

    def __newfile_play(self,fname,max_attempts=25):
        # Do update check
        attempts = 0
        gotfile = False
        while attempts < max_attempts and not gotfile:
            musiclist = self.mpc.listall()
            gotfile = fname in musiclist
            if not gotfile:
                sleep(0.5)
            attempts += 1
        if gotfile:
            self.mpc.add(fname)
            self.mpc.play()
        else:
            print("Couldn't find file :/")

    def __parse_command(self,cmd,event,cmd_regular):
        cmd = cmd.strip()
        parts = cmd.split(" ")
        room = self.rooms[event['room_id']];
        if parts[0] == "shuffle":
            self.mpc.shuffle()
        elif parts[0] == "prev":
            self.mpc.next()
        elif parts[0] == "play":
            self.mpc.play()
        elif parts[0] == "next":
            self.mpc.next()
        elif parts[0] == "playlist":
            plist = self.mpc.playlist().split("\n")[:-1][:3]
            if len(plist) > 0:
                plist[0] = "▶ " + plist[0]
                room.send_text("\n".join(plist).replace(".ogg",""))
            else:
                room.send_text("The playlist is empty")
        elif parts[0] == "current":
            fname = self.mpc.current()
            fname = fname.replace("_"," ").replace(".ogg","")
            room.send_text(fname)
        elif parts[0] == "update":
            self.mpc.update()
        elif parts[0] == "help":
            room.send_text("Commands are: play,prev,next,current,playlist,help,[youtube url],stream url")
        elif "youtube.com/" in parts[0]:
            try:
                url = cmd_regular.strip().split(" ")[0]
                status,fname = download_youtube(url,self.music_dir,self.__newfile_play)
            except Exception as e:
                print(e)
                print(traceback.format_exc())
                room.send_text("Couldn't download the file :(")
                return;
            self.mpc.update(True)


            if status:
                pos = len(self.mpc.playlist().split('\n'))-1
                if fname is not False:
                    fi = fname.replace(".ogg","")
                    if pos > 1:
                        room.send_text(fi + " has been queued. It currently at position "+str(pos))
                    else:
                        room.send_text(fi + " has begun playing")
                else:
                    if pos > 1:
                        room.send_text("Your playlist has been queued. It currently at position "+str(pos))
                    else:
                        room.send_text("Your playlist has begun playing")
                if self.mpc.current() == '':
                    sleep(0.5)# Allow it to breathe
                    self.mpc.play()
            else:
                room.send_text("I couldn't play the file. This is probably a bug and should be reported to Half-Shot.")

        elif "stream url" in cmd:
            room.send_text(self.stream_url)
