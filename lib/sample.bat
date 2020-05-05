naucon.exe -d wasapiloopback -r 44100 -b 16 -c 2 | lame.exe -r -b 128k -s 44100 --bitwidth 16 - - > sample.mp3
