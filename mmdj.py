#!/usr/bin/env python3
""" Matrix MPD DJ """
import configparser
from os.path import expanduser,exists
from cmdlistener import CmdListener
from sys import exit
# Error Codes:
# 1 - Unknown problem has occured
# 2 - Could not find the server.
# 3 - Bad URL Format.
# 4 - Bad username/password.
# 11 - Wrong room format.
# 12 - Couldn't find room.

def default_config(path):
    config = configparser.ConfigParser()

    config["mpc"] = {}
    config["matrix"] = {}

    config["mpc"]["host"] = "localhost"
    config["mpc"]["port"] = "6600"
    config["mpc"]["streamurl"] = "http://localhost:8000"

    config["matrix"]["host"] = "https://localhost:8448"
    config["matrix"]["user"] = "username"
    config["matrix"]["pass"] = "password"
    config["matrix"]["rooms"] = "#RoomA,#RoomB:localhost,#RoomC"

    with open(path, 'w') as configfile:
        config.write(configfile)

def read_config(path):
    config = configparser.ConfigParser()
    config.read(path)
    if "mpc" not in config.keys():
        print("Error, missing mpc section")
        return False

    keys = ["host","port","streamurl"]
    for key in keys:
        if key not in config["mpc"].keys():
            print("Error, missing",key,"from mpc section")
            return False

    if "matrix" not in config.keys():
        print("Error, missing matrix section")
        return False

    keys = ["host","user","pass","rooms"]
    for key in keys:
        if key not in config["matrix"].keys():
            print("Error, missing",key,"from matrix section")
            return False

    return True

#Get config
cfgfile = expanduser("~/.config/mpddj.ini")
config = None
if not exists(cfgfile):
    print("Config file not found, writing a new one")
    print("Writing to",cfgfile)
    config = default_config(cfgfile)
else:
    print("Reading",cfgfile)
    if read_config(cfgfile):
        config = configparser.ConfigParser()
        config.read(cfgfile)
    else:
        print("Cannot start, you have errors in your config file")

try:
    cmd = CmdListener(config)
except Exception as e:
    print("Failed to connect to one or more services.")
    print("The message was:",e)
    exit(2)
cmd.run()


MPD_HOST = "localhost"
MPD_STREAMURL = "http://half-shot.uk:8000/mpd.ogg"
MTX_HOST = "https://souppenguin.com:8448"
MTX_USERNAME = "@mpddj:souppenguin.com"
MTX_ROOMS = ["#devtest:souppenguin.com","#offtopic:matrix.org"]
