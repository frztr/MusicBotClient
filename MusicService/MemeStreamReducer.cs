using Microsoft.Extensions.DependencyInjection;
using MusicBotLibrary.LogService;
using MusicBotClient.MemeService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MusicBotClient.MusicService
{
    public class MemeStreamReducer : AbsStreamReducer
    {
        private IMemeService memeService;
        private ILogService logService;
        private string module = "MemeStreamReducer";
        const int ARRAY_SIZE = 960;
        private Stream usedProcess;

        public MemeStreamReducer(Stream usedProcess)
        {
            logService = Program.getProvider().GetService<ILogService>();
            memeService = Program.getProvider().GetService<IMemeService>();
            logService.Log(LogCategories.LOG_DATA, module, $"Stream position: {usedProcess.Position}");
            usedProcess.Position = 0;
            this.usedProcess = usedProcess;
            logService.Log(LogCategories.LOG_DATA, module, "Initialize memestream");
        }

        public static async Task<MemeStreamReducer> CreateMemeStreamReducer(string name)
        {
            return await Task.Run(() =>
            {
                var memeService = Program.getProvider().GetService<IMemeService>();
                var usedProcess = new MemoryStream();
                byte[] buffer = memeService.getSound(name);
                usedProcess.Write(buffer, 0, buffer.Length);
                MemeStreamReducer reducer = new MemeStreamReducer(usedProcess);
                return reducer;
            });
        }

        public override async Task Execute(Stream stream)
        {
            await Task.Run(async () =>
            {
                byte[] basearray = new byte[ARRAY_SIZE];
                await stream.ReadAsync(basearray, 0, basearray.Length);
                byte[] array = new byte[ARRAY_SIZE];
                int size = await usedProcess.ReadAsync(array, 0, array.Length);
                if (usedProcess.Length != usedProcess.Position)
                {
                    for (int i = 0; i < basearray.Length; i = i+2)
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
                    stream.Position -= ARRAY_SIZE;
                }
                else
                {
                    usedProcess.Close();
                    OnEnd(this);
                    //IsRunning = false;
                }
            });
        }

        public override async Task Destroy()
        {
            this.usedProcess.Close();
        }
    }
}
