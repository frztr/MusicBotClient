using Discord;
using Microsoft.Extensions.DependencyInjection;
using MusicBotClient.MemeService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicBotClient.AudioVoiceChannel;
using MusicBotClient.CoordinationService;
using MusicBotClient.MusicService;
using System.Threading;
using System.Net.WebSockets;
using System.Diagnostics;
using System.Security.Policy;
using MusicBotClient.IOService;
using Discord.WebSocket;
using MusicBotLibrary.LogService;
namespace MusicBotClient
{
    class Program
    {
        static IServiceProvider provider;
        static void Main(string[] args) => RunAsync().GetAwaiter().GetResult();

        private static async Task RunAsync()
        {
            var discordclient = new DiscordShardedClient(new DiscordSocketConfig() { GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent });
            provider = new ServiceCollection()
                .AddSingleton<ILogService>(new LogService("MusicBotClient"))
                .AddSingleton<ICoordinationService, MarfusiousCoodinationService>()
                .AddSingleton<IMusicService, MusicService.MusicService>()
                .AddSingleton<AbsAudioChannelFactory, StandartAudioChannelFactory>()
                .AddSingleton(discordclient)
                .AddSingleton<IIOService, IOService.IOService>()
                .AddSingleton<IMemeService, MemeService.MemeService>()
                .BuildServiceProvider();
            provider.GetService<ICoordinationService>();


            var client = provider.GetService<DiscordShardedClient>();

            client.Log += Client_Log;
            client.ShardDisconnected += Client_ShardDisconnected;
            //client.Disconnected += Client_Disconnected;

            await client.LoginAsync(TokenType.Bot, "ODEyMzE3NjA0MjY1MDY2NTE3.Grbw9X.-AVeOgLvzZZ4MXUAfz20wHtU72su1Z7V9g8l1Y");
            await client.StartAsync();
            provider.GetService<IMemeService>();
            Console.Read();
        }

        private async static Task Client_ShardDisconnected(Exception arg1, DiscordSocketClient arg2)
        {
            provider.GetService<ILogService>().Log(LogCategories.LOG_DATA, "DiscordShardedClient", "Client Disconnected");
            provider.GetService<ILogService>().Log(LogCategories.LOG_ERR, "DiscordShardedClient", exception: arg1);
        }

        //private static async Task Client_Disconnected(Exception arg)
        //{
        //    provider.GetService<ILogService>().Log(LogCategories.LOG_DATA, "DiscordSocketClient", "Client Disconnected");
        //    provider.GetService<ILogService>().Log(LogCategories.LOG_ERR, "DiscordSocketClient", exception: arg);
        //    //added for exception fix
        //    //if (arg.GetType() == typeof(WebSocketException)) 
        //    //{
        //    //    await provider.GetService<DiscordSocketClient>().StartAsync();
        //    //}
        //}

        private static async Task Client_Log(LogMessage arg)
        {
            if (arg.Exception == null)
            {
                provider.GetService<ILogService>().Log(LogCategories.LOG_DATA, "Discord", arg.Message);
            }
            else
            {
                provider.GetService<ILogService>().Log(LogCategories.LOG_ERR, "Discord", exception: arg.Exception);
                //if (arg.Exception.GetType() == typeof(WebSocketException))
                //{
                //    var socketclient = provider.GetService<DiscordSocketClient>();
                //    //FIXING
                //    provider.GetService<ILogService>().Log(LogCategories.LOG_DATA, "Discord", "Starting new client connection...");
                //    await socketclient.StartAsync();
                //}
            }
        }

        public static IServiceProvider getProvider()
        {
            return provider;
        }
    }
}
