#!/usr/bin/env python3

from djimporter import download_youtube
from matrix_client.client import MatrixClient
from mpc.mpc import MPCClient
from time import time, sleep

class CmdListener:
    rooms = {}
    mpc = None
    client = None
    stream_url = ""
    def __init__(self,config):
        self.mpc = MPCClient(config["mpc"]["host"],config["mpc"]["port"])

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
            sleep(0.1)
            pass

    def __on_cmd(self,event):
        if event['type'] == "m.room.message" and event['content']['msgtype'] == "m.text":
            if event['age'] < 300:
                body = event['content']['body'].lower()
                if body.startswith('mpddj:') or body.startswith('!mpddj'):
                    self.__parse_command(body[6:],event,event['content']['body'][6:])
                elif body.startswith('MPD DJ:'):
                    self.__parse_command(body[7:],event,event['content']['body'][7:])

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
        elif parts[0] == "current":
            fname = mpc.current()
            fname = fname.replace("_"," ").replace(".ogg","")
            room.send_text(fname)
        elif parts[0] == "update":
            self.mpc.update()
        elif parts[0] == "help":
            room.send_text("Commands are: play,prev,next,current,help,[youtube url],stream url")
        elif "youtube.com/" in parts[0]:
            try:
                url = cmd_regular.strip().split(" ")[0]
                f = download_youtube(url)
            except Exception as e:
                print(e)
                room.send_text("Couldn't download the file :(")
                return;
            sleep(3)
            self.mpc.add(f)
            pos = len(self.mpc.playlist().split('\n'))-1
            if pos > 1:
                room.send_text("Your request has been queued. It currently at position "+pos)
            else:
                room.send_text("Your request has begun playing")
            if self.mpc.current() == '':
                sleep(0.5)# Allow it to breathe
                self.mpc.play()
        elif "stream url" in cmd:
            room.send_text(self.stream_url)
