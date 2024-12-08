using CoreMusicBot;
using Microsoft.Extensions.DependencyInjection;
using MusicBotLibrary.LogService;

namespace MusicBotClient.LogService
{
    internal class LogServiceClient : ILogService
    {
        public LogServiceClient()
        {

        }

        public void Log(LogCategories logCategory, string module, string message = "", Exception exception = null)
        {
            if (logCategory == LogCategories.LOG_DATA)
            {
                Console.WriteLine($@"{DateTime.Now} [Data] [{module}] {message}");
            }
            else
            {
                Console.WriteLine($@"{DateTime.Now} [Error] [{module}] {exception.Message} {exception.GetType()} {exception.Source} {exception.StackTrace} {exception.TargetSite} {exception.HResult}");
            }
        }
    }
}