using Discord.Commands;
using Discord;
using MusicBotClient.AudioVoiceChannel;
using MusicBotClient.CoordinationService;
using MusicBotClient.IOService;
using MusicBotClient.MemeService;
using MusicBotClient.MusicService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicBotLibrary.LogService;
using Microsoft.Extensions.DependencyInjection;
using Discord.WebSocket;
using System.Reflection;

namespace CoreMusicBot.AppBuilder
{
    internal class DividedAppBuilder : IAppBuilder
    {
        public void InitApp(IServiceCollection collection)
        {
            var discordclient = new DiscordShardedClient(
                new DiscordSocketConfig()
                {
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
                });
            ApplicationContext.ServiceProvider = collection                
                .AddSingleton<ILogService>(new LogService("MusicBotClient"))
                .AddSingleton<ICoordinationService, MarfusiousCoodinationService>()
                .AddSingleton<IMusicService, MusicBotClient.MusicService.MusicService>()
                .AddSingleton<AbsAudioChannelFactory, StandartAudioChannelFactory>()
                .AddSingleton<IIOService, IOService>()
                .AddSingleton<IMemeService, MemeService>()
                .AddSingleton(discordclient)
                .BuildServiceProvider();

            ApplCntxt.ServiceProvider = ApplicationContext.ServiceProvider;

            ApplicationContext.ServiceProvider.GetService<ICoordinationService>();
        }

        internal class ApplCntxt
        {
            private static IServiceProvider sp = null;
            public static IServiceProvider ServiceProvider
            {
                get
                {
                    return sp;
                }
                internal set
                {
                    if (sp == null)
                    {
                        sp = value;
                    }
                }
            }
        }
    }
}
