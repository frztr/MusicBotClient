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
using Discord.Commands;
using System.Reflection;
using CoreMusicBot.AppBuilder;
using CoreMusicBot;
using static CoreMusicBot.AppBuilder.DividedAppBuilder;
using static CoreMusicBot.AppBuilder.SharedAppBuilder;
namespace MusicBotClient
{
    class Program
    {
        static IAppBuilder builder = new SharedAppBuilder();
        static void Main(string[] args) => RunAsync().GetAwaiter().GetResult();

        private static async Task RunAsync()
        {
            //ApplCntxt.ServiceProvider = new ServiceCollection().BuildServiceProvider();
            builder.InitApp(new ServiceCollection());
            
            var provider = ApplicationContext.ServiceProvider;
            var client = provider.GetService<DiscordShardedClient>();
            
            client.Log += Client_Log;
            client.ShardDisconnected += Client_ShardDisconnected;

            await client.LoginAsync(TokenType.Bot, "ODEyMzE3NjA0MjY1MDY2NTE3.Grbw9X.-AVeOgLvzZZ4MXUAfz20wHtU72su1Z7V9g8l1Y");
            await client.StartAsync();
            provider.GetService<IMemeService>();
            Console.Read();
        }

        

        private async static Task Client_ShardDisconnected(Exception arg1, DiscordSocketClient arg2)
        {
            var provider = ApplicationContext.ServiceProvider;
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
                ApplicationContext.ServiceProvider.GetService<ILogService>().Log(LogCategories.LOG_DATA, "Discord", arg.Message);
            }
            else
            {
                ApplicationContext.ServiceProvider.GetService<ILogService>().Log(LogCategories.LOG_ERR, "Discord", exception: arg.Exception);
                //if (arg.Exception.GetType() == typeof(WebSocketException))
                //{
                //    var socketclient = provider.GetService<DiscordSocketClient>();
                //    //FIXING
                //    provider.GetService<ILogService>().Log(LogCategories.LOG_DATA, "Discord", "Starting new client connection...");
                //    await socketclient.StartAsync();
                //}
            }
        }
    }
}
