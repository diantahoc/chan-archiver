ChanARCHIVER is an archive for 4chan. It has 3 operation modes:

1 - Full board archival. Archive all threads, and monitor board for new threads to archive.

2 - Single thread archival. Archive only a single thread until it 404. Useful for image dumping/info thread.

3 - Server mode. ChanARCHIVER runs as an HTTP server, serving only the archived threads. No archiving is made in this mode.

By default, ChanARCHIVER start the http server (listen on port `8787`) and archive /g/. You can modify it's behaviour with the following command line switches:

* `--thread:a:133` : Archive thread `133` from the /a/ board. This will start the single thread archival mode. No other thread can be archived.
* `--server` : Run ChanARCHIVER in the server mode.
* `--noserver` : To prevent ChanARCHIVER starting the HTTP server. Useful if you don't want to serve page right now, or another instance of ChanARCHIVER is running as server.
* `--board:r` : Archive the /r/ board.
* `--thumbonly` : Only save thumbnails.
* `--wget` : Use wget as the download backend. In my testings, this has not worked. `wget` is only supported under a *nix os. Note: this feature is disabled for now.
* `--verbose` : Output logs to the terminal console, instead of only logging them to the web ui.

ChanARCHIVER use the following libraries:
	
* `AniWrap` - .NET Wrapper for the popular anime website. https://github.com/diantahoc/AniWrap
* `Webserver` - C# embeddable HTTP server. http://webserver.codeplex.com/ 
* `SmartThreadPool` - Smart thread pool. http://www.codeproject.com/Articles/7933/Smart-Thread-Pool
* `RSS.NET` - RSS library for .NET. http://rssdotnet.com/
* `HtmlAgilityPack` - An agile HTML parser. It's used by `AniWrap`, but it is included in ChanARCHIVER source. http://htmlagilitypack.codeplex.com/

ChanARCHIVER is legally licensed under the GPLv2. Contributing is simple as suggesting features, or reporting issues, or forking and sending pull requests.
