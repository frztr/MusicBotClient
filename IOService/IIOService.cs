using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBotClient.IOService
{
    internal interface IIOService
    {
        Task ProcessKill(int id);
    }
}
