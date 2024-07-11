using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBotClient.CoordinationService
{
    interface ICoordinationService
    {
        Task SendAsync(string message);
        Task MessageReceived(string message);
    }
}
