# matrix-mpd-dj
A matrix bot for controlling a mpd music stream

Commands
--------
The current command selection is listed below:
```
  play - Play if the stream has stopped
  prev - Go to the previous track
  next - Go to the next track
  current - Current track name
  help - List avaliable commands
  [youtube url] - Give a youtube url to queue it
  stream url - What is the stream url?
  update - Refresh the library if the mpd fails to find a uploaded track.
```


Config
------
The configuration is stored in ~/.config/mpddj.ini

The default config is listed below:
```
  [mpc]
  host = localhost
  port = 6600
  streamurl = http://localhost:8000
  [matrix]
  host  = https://localhost:8448
  user  = username
  pass  = password
  rooms = #RoomA,#RoomB:localhost,#RoomC
```
