﻿using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using static FlyleafLib.Plugins.YoutubeDLJson;

namespace FlyleafLib.Plugins
{
    public class YoutubeDLJson : Format
    {
        public string   id              { get; set; }
        public string   title           { get; set; }
        public string   description     { get; set; }
        public string   upload_date     { get; set; }
        public string   uploader        { get; set; }
        public string   uploader_id     { get; set; }
        public string   uploader_url    { get; set; }
        public string   channel_id      { get; set; }
        public string   channel_url     { get; set; }
        public long     duration        { get; set; }
        public long     view_count      { get; set; }
        public double   average_rating  { get; set; }
        public int      age_limit       { get; set; }
        public string   webpage_url     { get; set; }
        
        public bool     playable_in_embed { get; set; }
        public bool     is_live         { get; set; }
        public bool     was_live        { get; set; }
        public string   live_status     { get; set; }
        
        // Playlist
        public string   _type           { get; set; }
        public string   playlist_count  { get; set; }
        public string   playlist_index  { get; set; }
        public string   playlist        { get; set; }
        public string   playlist_title  { get; set; }
        


        public Dictionary<string, List<SubtitlesFormat>>
                        automatic_captions { get; set; }
        public List<string> 
                        categories      { get; set; }
        public List<Format> 
                        formats         { get; set; }
        public List<Thumbnail> 
                        thumbnails      { get; set; }

        public long     like_count      { get; set; }
        public long     dislike_count   { get; set; }
        public string   channel         { get; set; }
        public string   availability    { get; set; }
        public string   webpage_url_basename 
                                        { get; set; }
        public string   extractor       { get; set; }
        public string   extractor_key   { get; set; }
        public string   thumbnail       { get; set; }
        public string   display_id      { get; set; }
        public string   fulltitle       { get; set; }
        public int      epoch           { get; set; }

        public class DownloaderOptions
        {
            public int http_chunk_size { get; set; }
        }

        public class HttpHeaders
        {
            [JsonProperty("User-Agent")]
            public string UserAgent     { get; set; }

            [JsonProperty("Accept-Charset")]
            public string AcceptCharset { get; set; }
            public string Accept        { get; set; }

            [JsonProperty("Accept-Encoding")]
            public string AcceptEncoding{ get; set; }

            [JsonProperty("Accept-Language")]
            public string AcceptLanguage{ get; set; }
        }

        public class Thumbnail
        {
            public string url           { get; set; }
            public int    preference    { get; set; }
            public string id            { get; set; }
            public double height        { get; set; }
            public double width         { get; set; }
            public string resolution    { get; set; }
        }

        public class SubtitlesFormat
        {
            public string ext   { get; set; }
            public string url   { get; set; }
            public string name  { get; set; }
        }
    }

    public class Format
    {
        public double   asr         { get; set; }
        public long     filesize    { get; set; }
        public string   format_id   { get; set; }   
        public string   format_note { get; set; }
        public int      quality     { get; set; }
        public double   tbr         { get; set; }
        public string   url         { get; set; }
        public string   manifest_url{ get; set; }
        public string   language    { get; set; }
        public int      language_preference 
                                    { get; set; }
        public string   ext         { get; set; }
        public string   vcodec      { get; set; }
        public string   acodec      { get; set; }
        public double   abr         { get; set; }
        public DownloaderOptions 
                        downloader_options { get; set; }
        public string   container   { get; set; }
        public string   protocol    { get; set; }
        public string   audio_ext   { get; set; }
        public string   video_ext   { get; set; }
        public string   format      { get; set; }
        public Dictionary<string, string> 
                        http_headers{ get; set; }
        public double   fps         { get; set; }
        public double   height      { get; set; }
        public double   width       { get; set; }
        public double   vbr         { get; set; }
    }
}