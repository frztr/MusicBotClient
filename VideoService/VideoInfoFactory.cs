using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreMusicBot.VideoService
{
    public class VideoInfoFactory : AbsVideoInfoFactory
    {
        public override AbsVideoInfo CreateVideoInfo()
        {
            return new VideoInfo();
        }
    }
}
