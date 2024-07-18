using CoreMusicBot.VideoService;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MusicBotClient.AudioVoiceChannel;
using MusicBotClient.IOService;
using MusicBotClient.MusicService;
using MusicBotLibrary.LogService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CoreMusicBot.MusicService
{
    internal class ShardedMusicService : IShardedMusicService
    {
        DiscordShardedClient client;
        AbsAudioChannelFactory audioChannelFactory;
        ILogService logService;
        IIOService IOService;
        const string module = "ShardedMusicService";

        public ShardedMusicService()
        {
            client = ApplicationContext.ServiceProvider.GetService<DiscordShardedClient>();
            audioChannelFactory = ApplicationContext.ServiceProvider.GetService<AbsAudioChannelFactory>();
            logService = ApplicationContext.ServiceProvider.GetService<ILogService>();
            IOService = ApplicationContext.ServiceProvider.GetService<IIOService>();
        }

        List<ChannelInfo> channels = new List<ChannelInfo>();

        public async void AddToQueue(ulong channelid, List<AbsVideoInfo> videoInfos)
        {
            var channel = channels.FirstOrDefault(x => x.avChannel.getId() == channelid);
            if (channel == null)
            {
                await Join(channelid);
                channel = channels.FirstOrDefault(x => x.avChannel.getId() == channelid);
            }
            foreach (var video in videoInfos)
            {
                channel.Queue.Enqueue(video);
            }
            Play(channelid);
        }

        public async Task Join(ulong voicechannel)
        {
            if (channels.FirstOrDefault(x => x.avChannel.getId() == voicechannel) != null) return;
            var channel = (client.GetChannel(voicechannel) as IAudioChannel);
            IAudioVoiceChannel vchannel = audioChannelFactory.CreateChannel(voicechannel, channel);
            channels.Add(new ChannelInfo() { avChannel = vchannel });
        }

        public async Task Leave(ulong voicechannel)
        {
            var vChannel = channels.FirstOrDefault(x => x.avChannel.getId() == voicechannel);
            if (vChannel == null) return;
            await Stop(voicechannel);
            var aChannel = client.GetChannel(voicechannel) as IAudioChannel;
            await aChannel.DisconnectAsync();
            channels.Remove(vChannel);
        }

        public async Task<Tuple<AbsVideoInfo, List<AbsVideoInfo>>> Queue(ulong voicechannel)
        {
            var channel = channels.FirstOrDefault(x => x.avChannel.getId() == voicechannel);
            return new Tuple<AbsVideoInfo, List<AbsVideoInfo>>(channel.CurrentVideo, channel.Queue.ToList());
        }

        private async Task Abort(ChannelInfo channelInfo)
        {
            channelInfo.avChannel.Stop();
            channelInfo.CurrentVideo = null;
        }

        public async Task<Tuple<AbsVideoInfo, AbsVideoInfo>> Skip(ulong voicechannel)
        {
            var channel = channels.FirstOrDefault(x => x.avChannel.getId() == voicechannel);
            logService.Log(LogCategories.LOG_DATA, module, $"Skip operation. Voicechannel: on {channel.avChannel.getId()}");
            var skipping = channel.CurrentVideo;
            var next = channel.Queue.FirstOrDefault();
            logService.Log(LogCategories.LOG_DATA, module, $"Skip {skipping.VideoUrl}. Next {next}");
            Abort(channel);
            Play(voicechannel);
            return new Tuple<AbsVideoInfo, AbsVideoInfo>(skipping, next);
        }

        public async Task Stop(ulong voicechannel)
        {
            var channel = channels.FirstOrDefault(x => x.avChannel.getId() == voicechannel);
            Abort(channel);
            channel.Queue.Clear();
        }

        public async Task Play(ulong voicechannel)
        {
            var channel = channels.FirstOrDefault(x => x.avChannel.getId() == voicechannel);
            if (channel.CurrentVideo != null) return;
            var song = channel.Queue.Dequeue();
            if (song != null)
            {
                channel.CurrentVideo = song;
                channel.avChannel.addStreamReducer(new MusicStreamReducer(song.VideoUrl));
            }
        }

        public async Task AddCustomReducer(ulong voicechannel, AbsStreamReducer reducer)
        {
            var channel = channels.FirstOrDefault(x => x.avChannel.getId() == voicechannel);
            channel.avChannel.addStreamReducer(reducer);
        }

        class ChannelInfo
        {
            public IAudioVoiceChannel avChannel { get; set; }
            public AbsVideoInfo CurrentVideo { get; set; }
            public Queue<AbsVideoInfo> Queue { get; set; } = new Queue<AbsVideoInfo>();
        }
    }
}
