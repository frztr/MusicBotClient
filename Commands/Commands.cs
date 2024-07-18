using CoreMusicBot.MusicService;
using CoreMusicBot.VideoService;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MusicBotClient;
using MusicBotClient.AudioVoiceChannel;
using MusicBotClient.MusicService;
using MusicBotLibrary.LogService;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CoreMusicBot.Commands
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        ILogService logService;
        IShardedMusicService musicService;
        IVideoService videoService;
        DiscordShardedClient discord;
        const string module = "Commands";

        public Commands()
        {
            var provider = ApplicationContext.ServiceProvider;
            this.logService = provider.GetService<ILogService>();
            this.musicService = provider.GetService<IShardedMusicService>();
            this.videoService = provider.GetService<IVideoService>();
            this.discord = provider.GetService<DiscordShardedClient>();
        }

        [Command("join", RunMode = RunMode.Async)]
        public async Task Join()
        {
            var channelid = (Context.User as IVoiceState).VoiceChannel.Id;
            var message = Context.Message;
            musicService.Join(channelid);
            message.ReplyAsync($":wave: Подключен к каналу **{(discord.GetChannel(channelid) as IAudioChannel).Name}**.");
        }

        [Command("play", RunMode = RunMode.Async)]
        public async Task Play(string url) 
        {
            var message = Context.Message;
            if (CheckUserInChannel(Context))
            {
                List<AbsVideoInfo> videoInfos = await videoService.getVideoInfo(url);
                CommentInfo(videoInfos);
                var channelid = (Context.User as IVoiceState).VoiceChannel.Id;
                musicService.AddToQueue(channelid, videoInfos);
            }
            else 
            {
                Context.Message.ReplyAsync("Пользователь должен находиться в голосовом канале.");
            }
        }

        [Command("Leave", RunMode = RunMode.Async)]
        public async Task Leave() 
        {
            Context.Message.ReplyAsync(":wave: Покинул канал **Основной**.");
            var channelid = (Context.User as IVoiceState).VoiceChannel.Id;
            musicService.Leave(channelid);
        }

        [Command("Skip", RunMode = RunMode.Async)]
        public async Task Skip() 
        {
            var channelid = (Context.User as IVoiceState).VoiceChannel.Id;
            logService.Log(LogCategories.LOG_DATA, module, $"Skip music on {channelid}");
            Tuple<AbsVideoInfo,AbsVideoInfo> video = await musicService.Skip(channelid);

            var response = ":no_entry:  Нечего пропускать.";
            if (video.Item1 != null)
            {
                response = $":track_previous: Пропускаем видео: **\"{video.Item1.Title}\"**.";

                if (video.Item2 != null)
                {
                    response += $"\n:track_next: Следующее видео: **\"{video.Item2.Title}\"**. ";

                    if (video.Item2.Duration.TotalSeconds != 0)
                    {
                        response += $"Длительность **{video.Item2.Duration}**.";
                    }
                }
            }
            ReplyAsync(response);
        }

        [Command("Stop", RunMode = RunMode.Async)]
        public async Task Stop() 
        {
            ReplyAsync(":octagonal_sign: Воспроизведение остановлено. Очередь очищена.");
            var channelid = (Context.User as IVoiceState).VoiceChannel.Id;
            await musicService.Stop(channelid);
        }

        [Command("Queue", RunMode = RunMode.Async)]
        public async Task Queue() 
        {
            var channelid = (Context.User as IVoiceState).VoiceChannel.Id;
            Tuple<AbsVideoInfo, List<AbsVideoInfo>> res = await musicService.Queue(channelid);
            var currentvideo = res.Item1;
            string response = ":bucket: Сейчас очередь пуста.";
            if (currentvideo != null)
            {
                response = $":loud_sound: Сейчас играет: **\"{currentvideo.Title}\"**. ";
                if (currentvideo.Duration.TotalSeconds != 0)
                {
                    response += $"Длительность: **{currentvideo.Duration}**.\n";
                }
            }
            var queue = res.Item2;
            var infos = queue.Take(5).ToList();
            if (infos.Count > 0)
            {
                response += $":hourglass_flowing_sand: В очереди находятся: \n";
                for (int i = 0; i < infos.Count; i++)
                {

                    response += $"{i}. **\"{infos[i].Title}\"**. ";
                    if (infos[i].Duration.TotalSeconds != 0)
                    {
                        response += $"Длительность: **{infos[i].Duration}**.\n";
                    }
                }
                if (queue.Count > 5)
                {
                    response += $"...\nи ещё **{queue.Count - 5}** видео.";
                }
            }
            ReplyAsync(response);
        }

        private async Task CommentInfo(List<AbsVideoInfo> infos)
        {           
            string response = "";
            if (infos.Count > 1)
            {
                Context.Message.ReplyAsync($":ballot_box_with_check: Добавлено **\"{infos[0].Title}\"** и ещё **{infos.Count - 1}** видео в очередь общей длительностью **{TimeSpan.FromSeconds(infos.Sum(x => x.Duration.TotalSeconds))}**.");
            }
            else
            {
                if (infos[0] != null)
                {
                    if (infos[0].LiveBroadcast != "none")
                    {
                        response = $":tv: Трансляция **\"{infos[0].Title}\"** добавлена в очередь.";
                    }
                    else
                    {
                        response = $":musical_note: Добавлено видео **\"{infos[0].Title}\"** в очередь длительностью {infos[0].Duration}.";
                    }
                    Console.WriteLine(response);
                    Context.Message.ReplyAsync(response);
                }
            }
        }

        private bool CheckUserInChannel(SocketCommandContext context)
        {
            var user = context.User as IVoiceState;
            if (user == null)
                return false;
            var voicechannel = user.VoiceChannel;
            if (voicechannel == null)
                return false;
            else
                return true;

        }

        [Command("meme", RunMode = RunMode.Async)]
        public async Task Meme(string memeName)
        {
            var channelid = (Context.User as IVoiceState).VoiceChannel.Id;
            musicService.AddCustomReducer(channelid,await MemeStreamReducer.CreateMemeStreamReducer((string)memeName));
        }
    }
}
