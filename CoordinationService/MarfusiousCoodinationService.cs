using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Websocket.Client;
using Microsoft.Extensions.DependencyInjection;
using MusicBotLibrary.LogService;
using MusicBotClient.MusicService;
using CoreMusicBot;

namespace MusicBotClient.CoordinationService
{
    class MarfusiousCoodinationService : ICoordinationService
    {
        WebsocketClient client = new WebsocketClient(new Uri("ws://192.168.137.1:80"));
        ILogService logService;
        IMusicService musicService;
        const string module = "CoordinationService";

        public MarfusiousCoodinationService() 
        {
            logService = ApplicationContext.ServiceProvider.GetService<ILogService>();
            musicService = ApplicationContext.ServiceProvider.GetService<IMusicService>();
            client.ReconnectTimeout = null;
            client.MessageReceived.Subscribe(msg => MessageReceived(msg.Text));
            client.Start();
        }

        public async Task MessageReceived(string message)
        {
            logService.Log(LogCategories.LOG_DATA,module, message);
            dynamic json = JsonConvert.DeserializeObject<dynamic>(message);
            var voiceChannelId = Convert.ToUInt64(json.voicechannelid.ToString());
            switch (json.operation.ToString())
            {
                case "play":
                    {
                        var video = json.video.ToString();
                        SendAsync(JsonConvert.SerializeObject(new { operation = "clientPlayingVideo", channelid = voiceChannelId, video = video }));
                        musicService.Play(voiceChannelId, video);
                    }
                    break;
                case "join":
                    {
                        string response = await musicService.Join(voiceChannelId);
                        if (response == "connected")
                        {
                            if (json.messageid != null)
                            {
                                var messageid = Convert.ToUInt64(json.messageid);
                                SendAsync(JsonConvert.SerializeObject(new { operation = "clientJoinedChannel", channelid = voiceChannelId, messageid }));
                            }
                            else 
                            {
                                SendAsync(JsonConvert.SerializeObject(new { operation = "clientJoinedChannel", channelid = voiceChannelId }));
                            }
                        }
                    }
                    break;
                case "skip":
                    {
                        musicService.Stop(voiceChannelId);
                    }
                    break;
                case "stop":
                    {
                        musicService.Stop(voiceChannelId);
                    }
                    break;
                case "leave":
                    {
                        var messageid = Convert.ToUInt64(json.messageid);
                        string response = await musicService.Leave(voiceChannelId);
                        if (response == "disconnected")
                        {
                            SendAsync(JsonConvert.SerializeObject(new { operation = "clientLeavedChannel", channelid = voiceChannelId, messageid }));
                        }
                    }
                    break;
                case "meme":
                    var channels = await musicService.getAudioVoiceChannels();
                    var channel =  channels.FirstOrDefault(x=>x.getId() == voiceChannelId);
                    channel.addStreamReducer(await MemeStreamReducer.CreateMemeStreamReducer((string)json.memeName));
                    break;
            }
        }

        public async Task SendAsync(string message)
        {
            logService.Log(LogCategories.LOG_DATA, module, $"Message to server: {message}");
            client.Send(message);
        }
    }
}
