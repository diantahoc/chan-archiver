ChanArchiver is an archive for 4chan. 

By default, ChanArchiver start the http server (listen on port `8787`). You can modify it's behaviour with the following command line switches:

* `--thread:a:133` : Archive thread `133` from the /a/ board. Additional threads can be added via the web user interface. Cannot be used in conjunction with `--board`.
* `--noserver` : To prevent ChanArchiver starting the HTTP server. Useful if you don't want to serve page right now, or another instance of ChanArchiver is running as server.
* `--board:r` : Archive the /r/ board. Overrides the board for the `--thread` switch, so cannot be used with `--thread`
* `--thumbonly` : Only save thumbnails.
* `--verbose` : Output logs to the terminal console, instead of only logging them to the web ui. Plus perform additional loggings.
* `--port:123`: Change the http server port to 123.
* `--savedir <dir>` : Change the save directory.

ChanArchiver can be integrated with 4chan pages by adding an `[Archive]` button next to the op post, as shown in the picture below:

![preview](https://cdn.mediacru.sh/2kyg9wLrrWOv.png "Archive button in the OP post")

User script: https://raw.github.com/diantahoc/chan-archiver/master/chan_archiver.user.js

ChanArchiver must be running in order to work properly. Modify the script to match your `ipadress:port` settings.

ChanArchiver use the following libraries:
	
* `AniWrap` - .NET Wrapper for the popular anime website. https://github.com/diantahoc/AniWrap
* `Webserver` - C# embeddable HTTP server. http://webserver.codeplex.com/ 
* `SmartThreadPool` - Smart thread pool. http://www.codeproject.com/Articles/7933/Smart-Thread-Pool
* `HtmlAgilityPack` - An agile HTML parser. It's used by `AniWrap`, but it is included in ChanArchiver source. http://htmlagilitypack.codeplex.com/

ChanArchiver is legally licensed under the GPLv2. Contributing is simple as suggesting features, or reporting issues, or forking and sending pull requests.
