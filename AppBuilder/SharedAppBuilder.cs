using CoreMusicBot.AudioVoiceChannel;
using CoreMusicBot.MusicService;
using CoreMusicBot.VideoService;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MusicBotClient.AudioVoiceChannel;
using MusicBotClient.CoordinationService;
using MusicBotClient.IOService;
using MusicBotClient.LogService;
using MusicBotClient.MemeService;
using MusicBotClient.MusicService;
using MusicBotLibrary.LogService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreMusicBot.AppBuilder
{
    internal class SharedAppBuilder : IAppBuilder
    {
        public void InitApp(IServiceCollection collection)
        {
            Console.WriteLine("Enter ShardId");
            var shardId = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Enter TotalShardCount");
            var totalShards = Convert.ToInt32(Console.ReadLine());

            var discordclient = new DiscordShardedClient(new int[] { shardId },
                new DiscordSocketConfig()
                {
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
                    TotalShards = totalShards
                });
            ApplicationContext.ServiceProvider = collection
                .AddSingleton(discordclient)
                // .AddSingleton<ILogService>(new LogService("MusicBotClient"))
                .AddSingleton<ILogService>(new LogServiceClient())
                .AddSingleton<IShardedMusicService, ShardedMusicService>()
                .AddSingleton<AbsAudioChannelFactory, SharedAudioVoiceChannelFactory>()
                .AddSingleton<IIOService, IOService>()
                .AddSingleton<IMemeService, MemeService>()
                .AddSingleton<IVideoService, YoutubeVideoService>()
                .AddSingleton<AbsVideoInfoFactory,VideoInfoFactory>()
                .AddSingleton<CommandService>()
                .BuildServiceProvider();

            var client = ApplicationContext.ServiceProvider.GetService<DiscordShardedClient>();
            client.MessageReceived += Client_MessageReceived;
            ApplicationContext.ServiceProvider.GetService<CommandService>().AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: ApplicationContext.ServiceProvider).GetAwaiter().GetResult();

            async static Task Client_MessageReceived(SocketMessage message)
            {
                var client = ApplicationContext.ServiceProvider.GetService<DiscordShardedClient>();
                var commandService = ApplicationContext.ServiceProvider.GetService<CommandService>();
                int argpos = 0;
                if (message is SocketUserMessage message1)
                {
                    if (!(message1.HasCharPrefix('!', ref argpos) ||
                    message1.HasMentionPrefix(client.CurrentUser, ref argpos)) ||
                    message1.Author.IsBot)
                        return;
                    var context = new ShardedCommandContext(client, message1);
                    await commandService.ExecuteAsync(context: context, argPos: argpos, services: ApplicationContext.ServiceProvider);
                }
                else
                {
                    ApplicationContext.ServiceProvider.GetService<ILogService>().Log(LogCategories.LOG_DATA, "Discord", "Not a socket user message");
                }
            }
        }
    }
}
