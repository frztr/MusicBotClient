using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreMusicBot.VideoService
{
    public abstract class AbsVideoInfo
    {
        public string VideoUrl { get; set; }

        public string Title { get; set; }

        public string LiveBroadcast { get; set; }

        public TimeSpan Duration { get; set; }
    }
}
