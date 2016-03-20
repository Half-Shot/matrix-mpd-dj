from subprocess import call, check_output
import shlex
import socket
#TODO: Implement the rest

def onoff(val):
    if val == True:
        return "on"
    else:
        return "off"

class MPCClient:
    def __init__(self,hostname="localhost",port="6600"):
        self.host = hostname
        self.port = port

    def __runcmd(self,cmd,output=False,args=[]):
        c = ["mpc","-h",self.host,"-p",self.port,cmd]
        if(output):
            return check_output(c+args).decode()
        else:
            return call(c+args)

    def __runtcp(self,cmd,output=False,args=[]):
        BUFFER = 512
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        try:
            s.connect((self.host,int(self.port)))
        except:
            raise Exception("Couldn't connect to MPD")

        data = s.recv(BUFFER).decode()
        if not data.startswith("OK MPD"):
            s.close()
            raise Exception("Couldn't connect to MPD")
        msg = cmd+" "+" ".join(args)+"\n"
        s.send(msg.encode())
        data = s.recv(BUFFER)
        print(data.decode())
        s.close()
        return True

    def set_crossfade(self,seconds):
        return self.__runcmd("crossfade",False,[str(seconds)])

    def get_crossfade(self):
        cfade = self.__runcmd("crossfade",True)
        if(cfade):
            cfade = int(cfade[11:])
        return cfade

    def current(self):
        return self.__runcmd("current",True)

    def crop(self):
        return self.__runcmd("crop")

    def clear(self):
        return self.__runcmd("clear")

    def pause(self):
        return self.__runcmd("pause")

    def delete(self,songid=0):
        return self.__runcmd("del",False,[songid])

    def idle(self,filter=None):
        if filter != None:
            return self.__runcmd("idle",True,[filter])
        else:
            return self.__runcmd("idle",True)

    def play(self,songid=None):
        if(songid != None):
            return self.__runcmd("play",False,[songid])
        else:
            return self.__runcmd("play",False)

    def listall(self,playlist=None):
        if playlist != None:
            return self.__runcmd("listall",True,[playlist])
        else:
            return self.__runcmd("listall",True)

    def load(self,playlist):
        return self.__runcmd("load",False,[playlist])

    def ls(self,directory=None):
        if directory != None:
            return self.__runcmd("ls",True,[directory])
        else:
            return self.__runcmd("ls",True)

    def lsplaylists(self,directory=None):
        return self.__runcmd("lsplaylists",True)

    def move(self,frm,to):
        return self.__runcmd("move",False,[frm,to])

    def next(self):
        return self.__runcmd("next")

    def playlist(self,playlist=None):
        if playlist != None:
            return self.__runcmd("playlist",True,[playlist])
        else:
            return self.__runcmd("playlist",True)

    def prev(self):
        return self.__runcmd("prev")

    def random(self,on=None):
        if on != None:
            return self.__runcmd("random",False,[onoff(om)])
        else:
            return self.__runcmd("random",False)

    def repeat(self,on=None):
        if on != None:
            return self.__runcmd("repeat",False,[onoff(om)])
        else:
            return self.__runcmd("repeat",False)

    def single(self,on=None):
        if on != None:
            return self.__runcmd("single",False,[onoff(om)])
        else:
            return self.__runcmd("single",False)

    def consume(self,on=None):
        if on != None:
            return self.__runcmd("consume",False,[onoff(om)])
        else:
            return self.__runcmd("consume",False)

    def rm(self,playlist):
        return self.__runcmd("rm",False,[playlist])

    def save(self,playlist):
        return self.__runcmd("save",False,[playlist])

    def shuffle(self):
        return self.__runcmd("shuffle")

    def stats(self):
        return self.__runcmd("stats",True)

    def stop(self):
        return self.__runcmd("stop")

    def toggle(self):
        return self.__runcmd("toggle")

    def add(self,file):
        file = '"'+file+'"'
        return self.__runtcp("add",False,[file])

    def insert(self,file):
        file = shlex.quote(file)
        return self.__runtcp("insert",False,[file])

    def update(self,wait=False,path=None):
        path = shlex.quote(path)
        args = []

        if wait:
            args.append("--wait")

        if path != None:
            args.append(path)

        return self.__runcmd("update",False,args)

    def version(self):
        return self.__runcmd("version")
