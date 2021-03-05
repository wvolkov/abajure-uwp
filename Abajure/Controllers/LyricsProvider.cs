using Abajure.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace Abajure.Controllers
{
    static class LyricsProvider
    {
        private static string _lastErrorMessage;

        public static string LastErrorMessage
        {
            get
            {
                return _lastErrorMessage;
            }
        }

        public static async Task<LyricLineSet> GetLyricsAsync(string track, string artist, string album)
        {
            string lyricFileName = GenerateHash(track, artist, album);
            var lyrics = await LyricLineSet.Load(lyricFileName);
            if (lyrics != null)
                return lyrics;
            else
            {
                lyrics = await RequestLyricsAsync(track, artist, album);
                if (lyrics != null)
                    lyrics.Save(lyricFileName);
                return lyrics;
            }
        }

        public static async Task<LyricLineSet> RequestLyricsAsync(string track, string artist, string album)
        {
            HttpClient client = new HttpClient();
            var headers = client.DefaultRequestHeaders;
            SetMusixMatchDefaultHeaders(headers);
            Uri requestUri = CreateMusixMatchUriRequest(track, artist, album);

            try
            {
                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, requestUri);
                HttpResponseMessage resp = await client.SendRequestAsync(req);
                if (resp != null && resp.StatusCode == (HttpStatusCode)200)
                {
                    string resp_content = await resp.Content.ReadAsStringAsync();
                    JObject jsLyricsResult = JObject.Parse(resp_content);
                    JToken lyrics = null;
                    try
                    {
                        lyrics = jsLyricsResult["message"]["body"]["macro_calls"]["track.subtitles.get"]["message"]["body"]["subtitle_list"][0]["subtitle"]["subtitle_body"];
                        string lyrics_json = lyrics.Value<String>();
                        if(lyrics_json != null)
                        {
                            JArray parsed_lyrics = JArray.Parse(lyrics_json);
                            return new LyricLineSet(parsed_lyrics);
                        }
                    }
                    catch
                    {
                        try
                        {
                            lyrics = jsLyricsResult["message"]["body"]["macro_calls"]["track.lyrics.get"]["message"]["body"]["lyrics"]["lyrics_body"];
                            if (lyrics != null)
                                return new LyricLineSet(lyrics.Value<string>());
                        }
                        catch
                        {
                            lyrics = null;
                        }
                    }
                    return null; //TODO proper value
                }
                else
                {
                    _lastErrorMessage = resp.Content.ToString();
                    return null;
                }
            }
            catch (Exception ex) { _lastErrorMessage = ex.Message; }
            return null;


        }

        private static void SetMusixMatchDefaultHeaders(HttpRequestHeaderCollection headers)
        {
            headers.TryAppendWithoutValidation("Accept", "*/*");
            headers.TryAppendWithoutValidation("Accept-Encoding", "gzip, deflate, br");
            headers.TryAppendWithoutValidation("Connection", "keep-alive");
            headers.TryAppendWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Musixmatch/3.14.4564-master.20200505002 Chrome/78.0.3904.130 Electron/7.1.5 Safari/537.36");
        }

        private static Uri CreateMusixMatchUriRequest(string track, string artist, string album)
        {
            string
                trackParam = track.Replace(" ", "%20"),
                artistParam = artist.Replace(" ", "%20"),
                albumParam = album.Replace(" ", "%20"),
                query = "",
                url = "https://apic-desktop.musixmatch.com/ws/1.1/macro.subtitles.get";

            Dictionary<string, string> queryParams = new Dictionary<string, string>()
            {
                {"format", "json" },
                {"namespace", "lyrics_synched" },
                {"part", "lyrics_crowd%2Cuser%2Clyrics_verified_by" },
                {"q_album", albumParam },
                {"q_artist", artistParam },
                {"q_artists", artistParam },
                {"q_track", trackParam },
                {"tags", "nowplaying" },
                {"user_language", "en" },
                {"f_subtitle_length_max_deviation", "1" },
                {"subtitle_format", "mxm" },
                {"app_id", "web-desktop-app-v1.0" },
                {"usertoken", "210222ec1b35a92b7e1d31550927372d862aa1c1ed5aacf9315a24" }
            };
            foreach (var d in queryParams)
                query += $"&{d.Key}={d.Value}";

            return new Uri(url + "?" + query);

        }

        private static string GenerateHash(params string[] args)
        {
            string str = String.Join("", args);
            IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);
            HashAlgorithmProvider hashAlgorithm = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer hashBuffer = hashAlgorithm.HashData(buffer);

            return CryptographicBuffer.EncodeToHexString(hashBuffer);
        }
    }
}
