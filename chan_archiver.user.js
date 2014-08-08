// ==UserScript==
// @name ChanArchiver 4chan integration
// @namespace ChanArchiver
// @include *://boards.4chan.org/*
// @include *://boards.4chan.org/*/catalog
// @version 0.1
// @grant none
// ==/UserScript==
"use strict";
window.setTimeout(function () {
    //Change this setting only
    var host_name = "127.0.0.1:8787";

    var op_posts = document.getElementsByClassName("op");

    for (var i = 0; i < op_posts.length; i++) {
        var postInfo = op_posts[i].getElementsByClassName("postInfo")[0];
        var postNum = postInfo.getElementsByClassName("postNum")[0];
        var link = postNum.children[0];
        var h = link.href;
        var button = document.createElement("a");
        button.textContent = "[Archive]";
        button.href = "http://" + host_name + "/add/thread/?urlorformat=" + escape(h);
        postInfo.appendChild(button);


        var to_button = document.createElement("a");
        to_button.textContent = "[Archive TO]";
        to_button.href = "http://" + host_name + "/add/thread/?to=1&urlorformat=" + escape(h);
        postInfo.appendChild(to_button);
    }

    if (window.location.toString().contains("catalog")) {
        var catalog_items = document.getElementsByClassName("thread");
        for (i = 0; i < catalog_items.length; i++) {
            var h = catalog_items[i].children[0].href;
            var button = document.createElement("a");
            button.textContent = "[Archive]";
            button.href = "http://" + host_name + "/add/thread/?urlorformat=" + escape(h);
            catalog_items[i].appendChild(button);


            var to_button = document.createElement("a");
            to_button.textContent = "[Archive TO]";
            to_button.href = "http://" + host_name + "/add/thread/?to=1&urlorformat=" + escape(h);
            catalog_items[i].appendChild(to_button);
        }
    }
}, 1500);