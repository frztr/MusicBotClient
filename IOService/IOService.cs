using Microsoft.Extensions.DependencyInjection;
using MusicBotLibrary.LogService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBotClient.IOService
{
    internal class IOService : IIOService
    {
        ILogService logService;
        const string module = "IOService";
        public IOService()
        {
            logService = Program.getProvider().GetService<ILogService>();
        }

        public Task ProcessKill(int id)
        {
            Kill(id);
            return Task.CompletedTask;
        }

        private void Kill(int processId)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    //ManagementObjectSearcher processSearcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + processId);
                    //ManagementObjectCollection processCollection = processSearcher.Get();
                    try
                    {
                        Process proc = Process.GetProcessById(processId);
                        if (!proc.HasExited)
                        {
                            logService.Log(LogCategories.LOG_DATA, module, $"Kill Process:{proc.ProcessName} ({proc.Id})");
                            proc.Kill(true);
                            proc.WaitForExit();
                            proc.Close();
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        logService.Log(LogCategories.LOG_ERR, module, exception: ex);
                    }

                    //if (processCollection != null)
                    //{
                    //    foreach (ManagementObject mo in processCollection)
                    //    {
                    //        ProcessKill(Convert.ToInt32(mo["ProcessID"]));
                    //    }
                    //}
                    break;

            }
        }
    }
}
