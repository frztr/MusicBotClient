using Microsoft.Extensions.DependencyInjection;
using MusicBotLibrary.LogService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace MusicBotClient.MemeService
{
    internal class MemeService : IMemeService
    {
        private Dictionary<string, byte[]> Sounds;

        const string module = "MemeService";
        private ILogService logService;
        public MemeService()
        {
            Sounds = new Dictionary<string, byte[]>();
            logService = Program.getProvider().GetService<ILogService>();
            Task.Run(() => Loading());
        }

        private async Task Loading()
        {
            Sounds.Add("fart", await Load("./sounds/fart.webm"));
            Sounds.Add("ban", await Load("./sounds/ban.m4a"));
            Sounds.Add("discord_leave", await Load("./sounds/discord-leave.mp3"));
            Sounds.Add("discord_join", await Load("./sounds/discord-sounds.mp3"));
            Sounds.Add("fart2", await Load("./sounds/fart-with-reverb.mp3"));
            Sounds.Add("fart3", await Load("./sounds/fart_fat.mp3"));
            Sounds.Add("gachi", await Load("./sounds/gachi.mp3"));
            Sounds.Add("modem", await Load("./sounds/modem.mp3"));
            Sounds.Add("gachi_sorry", await Load("./sounds/oh-shit-iam-sorry.mp3"));
            Sounds.Add("gachi_shout", await Load("./sounds/rip-ears.mp3"));
            Sounds.Add("clown", await Load("./sounds/spasibo-kloun.mp3"));
            Sounds.Add("gachi_finger", await Load("./sounds/stick-your-finger-in-my-ass.mp3"));
        }

        private async Task<byte[]> Load(string path)
        {
            try
            {
                Process p;
                ProcessStartInfo ffmpeg = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = $" -hide_banner -loglevel error -i \"{path}\" -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                p = Process.Start(ffmpeg);

                MemoryStream ms = new MemoryStream();
                var output = p.StandardOutput.BaseStream;
                await output.CopyToAsync(ms);
                logService.Log(LogCategories.LOG_DATA, module, $"Loaded Sound: {path}");
                byte[] ret = ms.ToArray();
                ms.SetLength(0);
                ms.Close();
                return ret;
            }
            catch (Exception ex)
            {
                logService.Log(LogCategories.LOG_ERR, module, exception: ex);
                return null;
            }
        }

        public byte[] getSound(string name)
        {
            return Sounds[name];
        }
    }
}
