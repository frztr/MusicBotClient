using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreMusicBot.VideoService
{
    public interface IVideoService
    {
        Task<List<AbsVideoInfo>> getVideoInfo(string videoId);
    }
}
