#!/usr/bin/env python3
""" Matrix MPD DJ """
from matrix_client.client import MatrixClient
from mpc.mpc import MPCClient
from os.path import expanduser
from time import time, sleep
from djimporter import download_youtube

# Error Codes:
# 1 - Unknown problem has occured
# 2 - Could not find the server.
# 3 - Bad URL Format.
# 4 - Bad username/password.
# 11 - Wrong room format.
# 12 - Couldn't find room.

#TODO: Add a config file

def on_cmd(event):
    if event['type'] == "m.room.message" and event['content']['msgtype'] == "m.text":
        if event['age'] < 300:
            body = event['content']['body'].lower()
            if body.startswith('mpddj:') or body.startswith('!mpddj'):
                parse_command(body[6:],event,event['content']['body'][6:])
            elif body.startswith('MPD DJ:'):
                parse_command(body[7:],event,event['content']['body'][7:])

def parse_command(cmd,event,cmd_regular):
    cmd = cmd.strip()
    parts = cmd.split(" ")
    room = rooms[event['room_id']];
    if parts[0] == "shuffle":
        mpc.shuffle()
    elif parts[0] == "prev":
        mpc.next()
    elif parts[0] == "next":
        mpc.next()
    elif parts[0] == "current":
        fname = mpc.current()
        fname = fname.replace("_"," ").replace(".ogg","")
        room.send_text(fname)
    elif parts[0] == "update":
        mpc.update()
    elif "youtube.com/" in parts[0]:
        try:
            url = cmd_regular.strip().split(" ")[0]
            f = download_youtube(url)
        except Exception as e:
            print(e)
            room.send_text("Couldn't download the file :(")
            return;
        mpc.update(True)
        mpc.add(f)
        pos = len(mpc.playlist().split('\n'))-1
        room.send_text("Your request has been queued. It currently at position",pos)
        if client.current() == '':
            mpc.play()
    elif "stream url" in cmd:
        room.send_text(MPD_STREAMURL)

MPD_HOST = "localhost"

#f = open(expanduser("~/.config/mpd.pwd"))
#MPD_HOST = f.read().strip() + MPD_HOST
#f.close()

MPD_STREAMURL = "http://half-shot.uk:8000/mpd.ogg"
MTX_HOST = "https://souppenguin.com:8448"
MTX_USERNAME = "@mpddj:souppenguin.com"

f = open(expanduser("~/.config/mpddj.pwd"))
MTX_PASSWORD = f.read().strip()
f.close()

MTX_ROOMS = ["#devtest:souppenguin.com","#offtopic:matrix.org"]
rooms = {}

mpc = MPCClient(MPD_HOST)
client = MatrixClient(MTX_HOST)

try:
    client.login_with_password(MTX_USERNAME,MTX_PASSWORD)
except MatrixRequestError as e:
    print(e)
    if e.code == 403:
        print("Bad username or password.")
        sys.exit(4)
    else:
        print("Check your sever details are correct.")
        sys.exit(3)

for sroom in MTX_ROOMS:
    room = client.join_room(sroom)
    room.add_listener(on_cmd)
    rooms[room.room_id] = room

client.start_listener_thread()

while True:
    sleep(0.1)
    pass
