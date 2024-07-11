using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBotClient.MemeService
{
    public interface IMemeService
    {
        byte[] getSound(string name);
    }
}
