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
using System.Configuration;


namespace MusicBotClient
{
    class Program
    {
        static IAppBuilder builder;

        static AutoResetEvent autoEvent = new AutoResetEvent(false);
        static void Main(string[] args)
        {
            int? shardId = null;
            int? total = null;
            if (args.Length > 0)
            {
                if (args[0] != "" && args[1] != "")
                {
                    try
                    {
                        shardId = int.Parse(args[0]);
                        total = int.Parse(args[1]);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Exception:{ex.Message}");
                        Console.WriteLine("Wrong params format");
                    }
                }
            }

            RunAsync(shardId, total).GetAwaiter().GetResult();
        }

        private static async Task RunAsync(int? ShardId = null, int? TotalShardCount = null)
        {
            //ApplCntxt.ServiceProvider = new ServiceCollection().BuildServiceProvider();

            builder = new SharedAppBuilder(ShardId, TotalShardCount);
            builder.InitApp(new ServiceCollection());

            var provider = ApplicationContext.ServiceProvider;
            var client = provider.GetService<DiscordShardedClient>();

            client.Log += Client_Log;
            client.ShardDisconnected += Client_ShardDisconnected;

            

            // Console.WriteLine($"Bot_token:{ConfigurationManager.AppSettings["Bot_Token"]}");
            await client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["Bot_Token"]);
            await client.StartAsync();
            provider.GetService<IMemeService>();

            autoEvent.WaitOne();
            // Thread.Sleep(0);
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
