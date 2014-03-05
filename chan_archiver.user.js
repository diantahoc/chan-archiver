// ==UserScript==
// @name ChanArchiver 4chan integration
// @namespace ChanArchiver
// @include *://boards.4chan.org/*
// @version 0.1
// @grant none
// @run-at document-end
// ==/UserScript==

function a()
{
var op_posts = document.getElementsByClassName("op");

for ( i = 0; i < op_posts.length; i++ )
{
	var postInfo = op_posts[i].getElementsByClassName("postInfo")[0];
	var postNum = postInfo.getElementsByClassName("postNum")[0];
	var link = postNum.children[0];
	var h = link.href;
	var button = document.createElement("a");
	button.textContent = "[Archive]";
	button.href = "http://127.0.0.1:8787/add/thread/?urlorformat=" + escape(h);
	postInfo.appendChild(button);
}
}

a();