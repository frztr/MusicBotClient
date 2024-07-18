using Microsoft.Extensions.DependencyInjection;
using MusicBotClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreMusicBot.VideoService
{
    public class YoutubeVideoService : IVideoService
    {
        HttpClient httpClient = new HttpClient();
        public string Token = "AIzaSyC9OIDTfEtg8bB-ygWdgnLEc15T8wq0us8";

        public async Task<List<AbsVideoInfo>> getVideoInfo(string videoId)
        {
            List<AbsVideoInfo> videoInfo = new List<AbsVideoInfo>();
            if (videoId.Contains("list="))
            {
                string playlistId = Regex.Match(videoId, @"list=[a-zA-Z0-9_\-]+").Value.Split('=').Last();
                string actualvideoId = Regex.Match(videoId, @"v=[a-zA-Z0-9_\-]+").Value.Split('=').Last();
                videoInfo.AddRange(await GetPlalistInfoFromYoutube(playlistId, actualvideoId));
            }
            else
            {
                videoInfo.Add(await GetVideoInfoFromYoutube(Regex.Match(videoId, @"v=[a-zA-Z0-9_\-]+").Value.Split('=').Last()));
            }
            return videoInfo;
        }

        private async Task<AbsVideoInfo> GetVideoInfoFromYoutube(string videoId)
        {
            var response = await GetResponseAsync($"https://www.googleapis.com/youtube/v3/videos?key={Token}&part=snippet,contentDetails&id={videoId}");
            return DynamicToVideoInfo(response.items[0]);
        }
        private async Task<List<AbsVideoInfo>> GetPlalistInfoFromYoutube(string playlistId, string videoId = null)
        {
            List<AbsVideoInfo> infos = new List<AbsVideoInfo>();
            var response = await GetResponseAsync($"https://www.googleapis.com/youtube/v3/playlistItems?key={Token}&maxResults=50&part=snippet,contentDetails&playlistId={playlistId}");
            if (response.error == null)
            {
                List<string> ids = new List<string>();
                foreach (var item in response.items)
                {
                    ids.Add(item.contentDetails.videoId.ToString());
                }
                var response2 = await GetResponseAsync($"https://www.googleapis.com/youtube/v3/videos?key={Token}&maxResults=50&part=snippet,contentDetails&id={String.Join(",", ids.ToArray())}");
                
                foreach (var item in response2.items)
                {
                    infos.Add(DynamicToVideoInfo(item));
                }
            }
            else
            {
                if (videoId != null)
                {
                    var startvideo = await GetVideoInfoFromYoutube(videoId);
                    infos.Add(startvideo);
                }
            }
            return infos;
        }

        private async Task<dynamic> GetResponseAsync(string url)
        {
            try
            {
                var answer = await httpClient.GetStringAsync(url);
                dynamic response = JsonConvert.DeserializeObject<dynamic>(answer);
                return response;
            }
            catch (HttpRequestException ex)
            {
                dynamic response = new { error = true };
                return response;
            }
        }

        private TimeSpan DurationStringToTimeSpan(string stringDuration)
        {
            int days = int.Parse(("0" + Regex.Match(stringDuration, @"\d+D").Value).Replace("D", ""));
            int hours = int.Parse(("0" + Regex.Match(stringDuration, @"\d+H").Value).Replace("H", ""));
            int minutes = int.Parse(("0" + Regex.Match(stringDuration, @"\d+M").Value).Replace("M", ""));
            int seconds = int.Parse(("0" + Regex.Match(stringDuration, @"\d+S").Value).Replace("S", ""));
            return new TimeSpan(days, hours, minutes, seconds);
        }

        private AbsVideoInfo DynamicToVideoInfo(dynamic input_info)
        {
            AbsVideoInfo info = ApplicationContext.ServiceProvider.GetService<AbsVideoInfoFactory>().CreateVideoInfo();
            info.VideoUrl = $"https://youtube.com/watch?v={input_info.id.ToString()}";
            info.Title = input_info.snippet.title.ToString();
            info.LiveBroadcast = input_info.snippet.liveBroadcastContent.ToString();
            if (info.LiveBroadcast == "none")
            {
                info.Duration = DurationStringToTimeSpan(input_info.contentDetails.duration.ToString());
            }
            return info;
        }
    }
}
