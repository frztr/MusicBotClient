using CoreMusicBot.MusicService;
using Discord;
using Discord.Audio;
using Discord.Net;
using Microsoft.Extensions.DependencyInjection;
using MusicBotClient;
using MusicBotClient.AudioVoiceChannel;
using MusicBotClient.CoordinationService;
using MusicBotClient.MusicService;
using MusicBotLibrary.LogService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreMusicBot.AudioVoiceChannel
{
    internal class SharedAudioVoiceChannel : IAudioVoiceChannel
    {
        IAudioChannel channel;
        IAudioClient client;
        Process currentProcess;
        ulong id;
        AudioOutStream outStream;
        Stream MixerStream;
        List<AbsStreamReducer> streamReducers = new List<AbsStreamReducer>();
        ILogService logService;
        IShardedMusicService musicService;
        const string module = "SharedAudioVoiceChannel";

        bool Broadcasting = false;

        public SharedAudioVoiceChannel(ulong id, IAudioChannel channel)
        {
            this.id = id;
            this.channel = channel;

            this.logService = ApplicationContext.ServiceProvider.GetService<ILogService>();
            this.musicService = ApplicationContext.ServiceProvider.GetService<IShardedMusicService>();
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
            // Task.Run(() => Loop());
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
                    musicService.PlayForce(id);
                }
            }
        }

        private async Task Loop()
        {
            logService.Log(LogCategories.LOG_DATA, module, $"Loop for audioclient {id} started");


            // while (true)
            // {
            // if (streamReducers.Count > 0)
            // {
            Broadcasting = true;
            while (streamReducers.Count > 0)
            {
                if (MixerStream == null && outStream == null)
                    continue;

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
            Broadcasting = false;
            // }
            // }

        }

        public ulong getId()
        {
            return id;
        }

        public void setCurrentProcess(Process process)
        {
            currentProcess = process;
        }

        public async void Stop()
        {
            logService.Log(LogCategories.LOG_DATA, module, $"Stopping audio in {id}");
            //Removed for dont duplicate sending to server.
            //IsPlaying = false;

            for (int i = 0; i < streamReducers.Count; i++)
            {
                var x = streamReducers[i];
                logService.Log(LogCategories.LOG_DATA, module, $"{x.ToString()}");
                if (x.GetType() == typeof(MusicStreamReducer))
                {
                    var reducer = (MusicStreamReducer)x;
                    //streamReducers.Remove(x);
                    await reducer.Destroy();
                }
            }

            logService.Log(LogCategories.LOG_DATA, module, $"Audio Stopped in {id}");
        }

        public async Task addStreamReducer(AbsStreamReducer reducer)
        {
            if (reducer.GetType() == typeof(MusicStreamReducer))
            {
                IsPlaying = true;
            }
            reducer.OnEnd += reducerOnEnd;
            streamReducers.Add(reducer);
            if (!Broadcasting)
            {
                Task.Run(() => Loop());
            }
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
