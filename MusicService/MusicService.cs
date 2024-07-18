using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Audio;
using System.Diagnostics;
using MusicBotLibrary.LogService;
using System.IO;
using MusicBotClient.IOService;
using Newtonsoft.Json.Linq;
using MusicBotClient.AudioVoiceChannel;
using Discord.Net;
using Websocket.Client.Exceptions;
using System.Threading.Channels;
using Newtonsoft.Json;
using MusicBotClient.CoordinationService;
using CoreMusicBot;

namespace MusicBotClient.MusicService
{
    class MusicService : IMusicService
    {
        DiscordShardedClient client;
        AbsAudioChannelFactory audioChannelFactory;
        ILogService logService;
        IIOService IOService;
        //ICoordinationService coordinationService;
        const string module = "MusicService";

        public MusicService()
        {
            IServiceProvider provider = ApplicationContext.ServiceProvider;
            client = provider.GetService<DiscordShardedClient>();
            audioChannelFactory = provider.GetService<AbsAudioChannelFactory>();
            logService = provider.GetService<ILogService>();
            IOService = provider.GetService<IIOService>();
            //coordinationService = provider.GetService<ICoordinationService>();
        }

        List<IAudioVoiceChannel> channels = new List<IAudioVoiceChannel>();

        public async Task<string> Join(ulong voicechannel)
        {
            logService.Log(LogCategories.LOG_DATA, module, $"Try join {voicechannel}");
            if (channels.FirstOrDefault(x => x.getId() == voicechannel) != null) return "already_connected";
            var channel = (client.GetChannel(voicechannel) as IAudioChannel);
            logService.Log(LogCategories.LOG_DATA, module, $"Client send channel info: {channel.Name}");
            //var audioclient = await channel.ConnectAsync();
            //logService.Log(LogCategories.LOG_DATA, module, $"Channel ${channel.Name} {audioclient.ConnectionState}.");
            //if (audioclient.ConnectionState != ConnectionState.Connected) return "error_connection";
            IAudioVoiceChannel vchannel = audioChannelFactory.CreateChannel(voicechannel, channel);
            channels.Add(vchannel);
            return "connected";
        }

        public async Task<string> Leave(ulong voicechannel)
        {
            var vChannel = channels.FirstOrDefault(x => x.getId() == voicechannel);
            if (vChannel == null) return "channel_not_been_connected";
            var channel = (client.GetChannel(voicechannel) as IAudioChannel);
            var audioclient = vChannel.getAudioClient();
            await channel.DisconnectAsync();
            if (audioclient.ConnectionState != ConnectionState.Disconnected) return "error";
            channels.Remove(vChannel);
            return "disconnected";
        }

        //Change Join method to return not a string status => return voicechannel;
        public async Task Play(ulong voicechannel, string path)
        {
            logService.Log(LogCategories.LOG_DATA, module, "COMMAND PLAY.");
            var vchannel = channels.FirstOrDefault(x => x.getId() == voicechannel);
            if (vchannel == null)
            {
                if (await Join(voicechannel) == "connected")
                {
                    vchannel = channels.FirstOrDefault(x => x.getId() == voicechannel);
                }
            }
            vchannel.addStreamReducer(new MusicStreamReducer(path));
        }

        public async Task Stop(ulong voicechannel)
        {
            var vchannel = channels.FirstOrDefault(x => x.getId() == voicechannel);
            if (vchannel != null)
            {
                vchannel.Stop();
            }
        }

        public async Task<List<IAudioVoiceChannel>> getAudioVoiceChannels()
        {
            return channels;
        }
    }
}
