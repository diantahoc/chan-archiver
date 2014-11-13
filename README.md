ChanArchiver is a personal archive program for 4chan.

### Hidden features:

#### Wordfilter:

The word filter is used to remove certain words from archived posts. It's applied to the post comment, post subject and file names. They can be added or removed using the `Interactive Console`.

#### File browser:

ChanArchiver include a mobile optimized file browser. It can be accesed at `http://ip:port/filetree`.

The file browser basically list files thumbnails according to the selected file type.

#### FoolFuuka threads

ChanArchiver can load 404'd threads from a FoolFuuka-based archive. This is limited to boards that exist on 4chan, that means you cannot archive a thread from some /meta/ board. 

This is useful for downloading all the files inside some thread.

To add a thread, please use the `add-fuuka` interactive console command. The command syntax is: `add-fuuka HOST BOARD ID` where `HOST` is the board host (`archive.foolz.us` for example), `BOARD` is the board letter and `ID` is the thread id. 

The archive has to support FoolFuuka JSON API. An HTML parser might be added in the future, along with a web interface for this feature.

#### Command Line switches

NOTICE: The command line switches provide basic and legacy thread adding / board monitoring. The preffered way to perform these actions is to the the Web interface. 

By default, ChanArchiver start the http server (listen on port `8787`). You can modify it's behaviour with the following command line switches:

* `--thread:a:133` : Archive thread `133` from the /a/ board. Additional threads can be added via the web user interface. Cannot be used in conjunction with `--board`.
* `--noserver` : To prevent ChanArchiver starting the HTTP server. Useful if you don't want to serve page right now, or another instance of ChanArchiver is running as server.
* `--board:r` : Archive the /r/ board. Overrides the board for the `--thread` switch, so cannot be used with `--thread`
* `--thumbonly` : Only save thumbnails.
* `--verbose` : Output logs to the terminal console, instead of only logging them to the web ui. Plus perform additional loggings.
* `--port:123`: Change the http server port to 123.
* `--savedir <dir>` : Change the save directory.

#### Integration with 4chan

ChanArchiver can be integrated with 4chan pages by adding an `[Archive] [Archive TO]` buttons next to the op post, as shown in the picture below:

![preview](https://cdn.mediacru.sh/2kyg9wLrrWOv.png "Archive button in the OP post")

The `[Archive TO]` button stand for `Archive Thumbnail Only`.

User script: https://raw.github.com/diantahoc/chan-archiver/master/chan_archiver.user.js

ChanArchiver must be running in order have this script working properly. Modify the script to match your `ipadress:port` settings.

#### Credits and Legal

ChanArchiver use the following libraries:
	
* `AniWrap` - .NET Wrapper for the popular anime website. https://github.com/diantahoc/AniWrap
* `Webserver` - C# embeddable HTTP server. http://webserver.codeplex.com/ 
* `SmartThreadPool` - Smart thread pool. http://www.codeproject.com/Articles/7933/Smart-Thread-Pool
* `HtmlAgilityPack` - An agile HTML parser. It's used by `AniWrap`, but it is included in ChanArchiver source. http://htmlagilitypack.codeplex.com/

ChanArchiver is legally licensed under the GPLv2. Contributing is simple as suggesting features, or reporting issues, or forking and sending pull requests.
