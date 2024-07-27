using CoreMusicBot;
using Discord;
using Discord.Audio;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MusicBotClient.CoordinationService;
using MusicBotClient.MusicService;
using MusicBotLibrary.LogService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MusicBotClient.AudioVoiceChannel
{
    class AudioVoiceChannel : IAudioVoiceChannel
    {
        IAudioChannel channel;
        IAudioClient client;
        Process currentProcess;
        ulong id;
        AudioOutStream outStream;
        Stream MixerStream;
        List<AbsStreamReducer> streamReducers = new List<AbsStreamReducer>();
        ILogService logService;
        ICoordinationService coordinationService;
        const string module = "AudioVoiceChannel";

        public AudioVoiceChannel(ulong id, IAudioChannel channel)
        {
            this.id = id;
            this.channel = channel;

            this.logService = ApplicationContext.ServiceProvider.GetService<ILogService>();
            this.coordinationService = ApplicationContext.ServiceProvider.GetService<ICoordinationService>();
            try
            {
                this.MixerStream = new MemoryStream();
                Run();
            }
            catch (Exception ex)
            {
                logService.Log(LogCategories.LOG_DATA, module, "AudioVoiceChannel()");
                logService.Log(LogCategories.LOG_ERR, module, exception: ex);
            }
        }

        private async Task Run()
        {
            this.client = await channel.ConnectAsync();
            this.outStream = this.client.CreatePCMStream(AudioApplication.Mixed);
            Task.Run(() => Loop());
        }

        bool _isplaying = false;
        bool IsPlaying
        {
            get
            {
                return _isplaying;
            }
            set
            {
                _isplaying = value;
                if (value == false)
                {
                    logService.Log(LogCategories.LOG_DATA, module, "Send_async. track_ended");
                    coordinationService.SendAsync(
                        JsonConvert.SerializeObject(new { operation = "clientEndedVideo", channelid = id }));
                }
            }
        }

        private async Task Loop()
        {
            logService.Log(LogCategories.LOG_DATA, module, $"Loop for audioclient {id} started");

            while (true)
            {
                if (streamReducers.Count > 0)
                {
                    while (streamReducers.Count > 0)
                    {
                        for (int i = 0; i < streamReducers.Count; i++)
                        {
                            var reducer = streamReducers[i];
                            try
                            {
                                if (reducer != null)
                                {
                                    await reducer.Execute(MixerStream);
                                }
                            }
                            catch (WebSocketClosedException ex)
                            {
                                logService.Log(LogCategories.LOG_ERR, module, exception: ex);
                                logService.Log(LogCategories.LOG_DATA, module, $"WEBSOCKET CLOSED REASON: ${ex.CloseCode} {ex.Reason}");
                                streamReducers.Remove(reducer);
                                //await reducer.Destroy();
                                i--;
                                logService.Log(LogCategories.LOG_DATA, module, $"reducers count:{streamReducers.Count}");
                            }
                            catch (ArgumentException ex)
                            {
                                logService.Log(LogCategories.LOG_ERR, module, exception: ex);
                                streamReducers.Remove(reducer);
                                //await reducer.Destroy();
                                i--;
                                logService.Log(LogCategories.LOG_DATA, module, $"reducers count:{streamReducers.Count}");
                            }
                            catch (Exception ex)
                            {
                                logService.Log(LogCategories.LOG_DATA, module, "Reducer exception");
                                logService.Log(LogCategories.LOG_ERR, module, exception: ex);
                                streamReducers.Remove(reducer);
                                //await reducer.Destroy();
                                i--;
                                logService.Log(LogCategories.LOG_DATA, module, $"reducers count:{streamReducers.Count}");
                            }
                        }
                        try
                        {
                            await MixerStream.CopyToAsync(outStream);
                        }
                        catch (TaskCanceledException ex)
                        {                      
                            logService.Log(LogCategories.LOG_DATA, module, "TaskCanceledException");
                            if (IsPlaying)
                            {
                                await Task.Delay(300);
                                await Run();
                            }
                            return;
                        }
                        catch (OperationCanceledException ex)
                        {
                            logService.Log(LogCategories.LOG_DATA, module, "OPERATIONCANCELEDEXCEPTION");
                            logService.Log(LogCategories.LOG_DATA, module, $"AudioClient connection: {client.ConnectionState}");
                            logService.Log(LogCategories.LOG_ERR, module, exception: ex);
                            logService.Log(LogCategories.LOG_DATA, module, $"reducers count:{streamReducers.Count}");
                            if (IsPlaying)
                            {
                                await Task.Delay(300);
                                await Run();
                            }
                            return;
                        }
                        catch (Exception ex)
                        {
                            logService.Log(LogCategories.LOG_DATA, module, "EXCEPTION");
                            logService.Log(LogCategories.LOG_ERR, module, exception: ex);
                            if (IsPlaying)
                            {
                                await Task.Delay(300);
                                await Run();
                            }
                            return;
                        }
                        MixerStream.SetLength(0);
                    }

                }
            }

        }

        public ulong getId()
        {
            return id;
        }

        public void setCurrentProcess(Process process)
        {
            currentProcess = process;
        }

        public void Stop()
        {
            logService.Log(LogCategories.LOG_DATA, module, $"Stopping audio in {id}");
            //IsPlaying = false;

            for (int i = 0; i < streamReducers.Count; i++)
            {
                var x = streamReducers[i];
                logService.Log(LogCategories.LOG_DATA, module, $"{x.ToString()}");
                if (x.GetType() == typeof(MusicStreamReducer))
                {
                    var reducer = (MusicStreamReducer)x;
                    //streamReducers.Remove(x);
                    reducer.Destroy();
                }
            }
            logService.Log(LogCategories.LOG_DATA, module, $"Reducers count {streamReducers.Count}");
            logService.Log(LogCategories.LOG_DATA, module, $"Audio Stopped in {id}");
        }

        public void addStreamReducer(AbsStreamReducer reducer)
        {
            if (reducer.GetType() == typeof(MusicStreamReducer))
            {
                IsPlaying = true;
            }
            reducer.OnEnd += reducerOnEnd;
            streamReducers.Add(reducer);
        }

        public void reducerOnEnd(AbsStreamReducer reducer)
        {
            streamReducers.Remove(reducer);
            logService.Log(LogCategories.LOG_DATA, module, $"Reducer on End. reducers count:{streamReducers.Count}");
            if (reducer.GetType() == typeof(MusicStreamReducer))
            {
                IsPlaying = false;
                logService.Log(LogCategories.LOG_DATA, module, $"Reducer on End. IsPlaying:{IsPlaying}");
            }

        }

        public IAudioClient getAudioClient()
        {
            return client;
        }
    }
}
//NOTIFY SERVER THAT MUSIC IS OVER

//if (streamReducers.FirstOrDefault(x => x.GetType() == typeof(MusicStreamReducer)) == null)
//{
//    logService.Log(LogCategories.LOG_DATA, module, "Send_async. track_ended");
//    coordinationService.SendAsync(JsonConvert.SerializeObject(new { operation = "clientEndedVideo", channelid = id }));
//}

//try
//{
//    Process p = Process.GetProcessById(((MusicStreamReducer)reducer).usedProcess.Id);
//}
//catch (ArgumentException ex)
//{
//    logService.Log(LogCategories.LOG_ERR, module, exception: ex);

//    //CONTINUE PLAYING IF IT STOPS

//    //if (((MusicStreamReducer)reducer).IsRunning)
//    //{
//    //    ((MusicStreamReducer)reducer).Continue();
//    //}

//    streamReducers.Remove(reducer);
//    await reducer.Destroy();
//    i--;
//    logService.Log(LogCategories.LOG_DATA, module, $"reducers count:{streamReducers.Count}");
//}