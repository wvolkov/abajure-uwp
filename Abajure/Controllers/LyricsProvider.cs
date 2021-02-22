using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static async Task<object> GetLyricsAsync(string track, string artist, string album)
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
                    return jsLyricsResult;
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
    }
}
