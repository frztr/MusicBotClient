using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MusicBotLibrary.LogService;
using MusicBotClient.MusicService;

namespace MusicBotClient.CoordinationService
{
    class CoordinationService : ICoordinationService
    {
        string server_ip_port = "ws://26.238.79.37:80";
        ClientWebSocket socket = new ClientWebSocket();
        ILogService logService;
        const string module = "CoordinationService";

        public CoordinationService()
        {
            logService = Program.getProvider().GetService<ILogService>();
            RunAsync();
        }

        private async Task RunAsync()
        {
            await HandShake();
            Task.Run(async () => {
                while (socket.State == WebSocketState.Open)
                {
                    while (true)
                    {
                        await Listen();
                    }
                }
                logService.Log(LogCategories.LOG_DATA,module,"Socket close.");
            });
        }

        private async Task Listen()
        {
            byte[] buf = new byte[1056];
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
            if (result.MessageType != WebSocketMessageType.Close)
            {
                MessageReceived(Encoding.UTF8.GetString(buf, 0, result.Count));
            }
            else
            {
                await Close(result.CloseStatusDescription);
            }
        }

        private Task HandShake()
        {
            var result = socket.ConnectAsync(new Uri(server_ip_port), CancellationToken.None);
            logService.Log(LogCategories.LOG_DATA,module,"Client Connected to Coordination Server");
            return result;
        }

        public async Task SendAsync(string message)
        {
            logService.Log(LogCategories.LOG_DATA, module,$"Message to server: {message}");
            socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, false, CancellationToken.None);
        }

        private Task Close(string closeStatusDescription)
        {
            socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            logService.Log(LogCategories.LOG_DATA, module, $"{closeStatusDescription}");
            return Task.CompletedTask;
        }

        public async Task MessageReceived(string message)
        {
            logService.Log(LogCategories.LOG_DATA, module, $"message received: {message}");
            dynamic json = JsonConvert.DeserializeObject<dynamic>(message);
            var musicservice = Program.getProvider().GetService<IMusicService>();
            var voicechannelid = Convert.ToUInt64(json.voicechannelid.ToString());
            switch (json.operation.ToString())
            {
                case "play":
                    {
                        var video = json.video.ToString();
                        SendAsync(JsonConvert.SerializeObject(new { operation = "clientPlayingVideo", channelid = voicechannelid, video = video }));
                        string response = await musicservice.Play(voicechannelid, video);
                        logService.Log(LogCategories.LOG_DATA, module, $"response from play:{response}");
                        if (response == "track_ended")
                        {
                            logService.Log(LogCategories.LOG_DATA, module, "Send_async. track_ended");
                            SendAsync(JsonConvert.SerializeObject(new { operation = "clientEndedVideo", channelid = voicechannelid }));
                        }
                    }
                    break;
                case "join":
                    {
                        logService.Log(LogCategories.LOG_DATA, module, "Join()");
                        var messageid = Convert.ToUInt64(json.messageid);
                        string response = await musicservice.Join(voicechannelid);
                        logService.Log(LogCategories.LOG_DATA, module, $"{response}");
                        if (response == "connected")
                        {
                            SendAsync(JsonConvert.SerializeObject(new { operation = "clientJoinedChannel", channelid = voicechannelid, messageid }));
                        }
                    }
                    break;
                case "skip":
                    {
                        musicservice.Stop(voicechannelid);
                    }
                    break;
                case "stop":
                    {
                        musicservice.Stop(voicechannelid);
                    }
                    break;
                case "leave":
                    {
                        var messageid = Convert.ToUInt64(json.messageid);
                        string response = await musicservice.Leave(voicechannelid);
                        if (response == "disconnected")
                        {
                            SendAsync(JsonConvert.SerializeObject(new { operation = "clientJoinedChannel", channelid = voicechannelid, messageid }));
                        }
                    }
                    break;
            }
        }

        public Task Close()
        {
            socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            logService.Log(LogCategories.LOG_DATA, module, "Socket Close");
            return Task.CompletedTask;
        }
    }
}
