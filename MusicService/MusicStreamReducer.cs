using CoreMusicBot;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using MusicBotClient.IOService;
using MusicBotLibrary.LogService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBotClient.MusicService
{
    public class MusicStreamReducer : AbsStreamReducer
    {
        private ILogService logService;
        private string path;
        private string module = "MusicStreamReducer";
        public Process usedProcess { get; protected set; }
        private IIOService iOService;
        private ulong bytecount = 0;

        private static int global_id = 0;

        private int Id;

        private bool Live { get; set; }

        public MusicStreamReducer(string path, bool live = false)
        {
            logService = ApplicationContext.ServiceProvider.GetService<ILogService>();
            this.path = path;
            Id = global_id;
            global_id += 1;
            Live = live;
            Continue();
            this.iOService = ApplicationContext.ServiceProvider.GetService<IIOService>();
        }


        public void Continue()
        {
            this.usedProcess = CreateYoutubeStream(path);
            //IsRunning = true;
        }

        public override async Task Execute(Stream stream)
        {
            await Task.Run(async () =>
            {

                byte[] array = new byte[ARRAY_SIZE];
                int get = await usedProcess.StandardOutput.BaseStream.ReadAsync(array, 0, array.Length);

                byte[] basearray = new byte[ARRAY_SIZE];
                await stream.ReadAsync(basearray, 0, basearray.Length);

                for (int i = 0; i < basearray.Length; i = i + 2)
                {
                    byte[] bytes1 = { basearray[i], basearray[i + 1] };
                    byte[] bytes2 = { array[i], array[i + 1] };

                    int a = BitConverter.ToInt16(bytes1, 0);
                    int b = BitConverter.ToInt16(bytes2, 0);
                    int m = Convert.ToInt32((a + b));
                    if (m > short.MaxValue) m = short.MaxValue;
                    if (m < short.MinValue) m = short.MinValue;
                    short x = (short)m;
                    byte[] r = BitConverter.GetBytes(x);
                    basearray[i] = r[0];
                    basearray[i + 1] = r[1];
                }

                await stream.WriteAsync(basearray, 0, basearray.Length);
                // if (!Live)
                {
                    stream.Position -= ARRAY_SIZE;
                    bytecount += ARRAY_SIZE;
                }

                // Console.WriteLine($"Bytes in msr{Id}: {bytecount}");
            });
        }

        private Process CreateYoutubeStream(string url)
        {
            double ticks = (Convert.ToDouble(bytecount) / BYTES_IN_SECOND) * 10000000;
            DateTime pos = new DateTime(Convert.ToInt64(ticks));
            try
            {
                ProcessStartInfo ffmpeg = new ProcessStartInfo();
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    ffmpeg = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/K yt-dlp -q -f bestaudio/best \"{url}\" -o - | ffmpeg -ss {pos.ToString("HH:mm:ss")} -hide_banner -loglevel error -i pipe: -f s16le -ar 48000 pipe:1 && exit",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                }
                else
                {
                    logService.Log(LogCategories.LOG_DATA,"YTDLP",url);
                    ffmpeg = new ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = $"-c \"yt-dlp -x --socket-timeout 25 -R infinite -q -f bestaudio/best \"{url}\" -o - | ffmpeg -ss {pos.ToString("HH:mm:ss")} -hide_banner -loglevel error -i pipe: -f s16le -ar 48000 pipe:1 && exit\"",
                        // Arguments = $"-c \"yt-dlp --extractor-args \"youtube:formats=dashy\" -N 4  -q -f bestaudio/best \"{url.Replace("youtube.com","piped.video")}\" -o - | ffmpeg -ss {pos.ToString("HH:mm:ss")} -hide_banner -loglevel error -i pipe: -f s16le -ar 48000 pipe:1 && exit\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                }
                logService.Log(LogCategories.LOG_DATA, module, "Loading Track");
                Process p = Process.Start(ffmpeg);
                p.EnableRaisingEvents = true;
                p.ErrorDataReceived += P_ErrorDataReceived;
                p.Exited += P_Exited;
                p.BeginErrorReadLine();
                return p;
            }
            catch (Exception ex)
            {
                logService.Log(LogCategories.LOG_ERR, module, exception: ex);
                return null;
            }
        }

        private void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            logService.Log(LogCategories.LOG_DATA, module, $"Process Data: {e.Data}");

            if (e.Data != null)
            {
                if (e.Data.Contains("10054") ||
                    e.Data.Contains("403") ||
                    e.Data.Contains("Giving up after 10 retries") ||
                    e.Data.Contains("ERROR:"))
                {
                    logService.Log(LogCategories.LOG_DATA, module, $"Continue on bytes: {bytecount}");
                    Continue();
                }
            }
        }

        private void P_Exited(object sender, EventArgs e)
        {
            try
            {
                usedProcess.WaitForExit();
                logService.Log(LogCategories.LOG_DATA, module, $"Exited process. {usedProcess.ExitCode}");
                OnEnd(this);
            }
            catch (Exception ex)
            {
                logService.Log(LogCategories.LOG_ERR, module, exception: ex);
            }
            //IsRunning = false;
        }

        public override async Task Destroy()
        {
            try
            {
                var p = Process.GetProcessById(usedProcess.Id);
                await iOService.ProcessKill(usedProcess.Id);
                logService.Log(LogCategories.LOG_DATA, module, $"Destroyed process {usedProcess.Id}.");
            }
            catch (ArgumentException ex)
            {
                logService.Log(LogCategories.LOG_DATA, module, $"Already Destroyed.");
            }
            catch (Exception ex)
            {
                logService.Log(LogCategories.LOG_ERR, module, exception: ex);
            }
        }
    }
}
