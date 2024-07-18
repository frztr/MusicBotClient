using CoreMusicBot.VideoService;
using MusicBotClient.MusicService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreMusicBot.MusicService
{
    interface IShardedMusicService
    {
        Task Join(ulong voicechannel);

        Task Play(ulong voicechannel);

        Task<Tuple<AbsVideoInfo, AbsVideoInfo>> Skip(ulong voicechannel);

        Task Stop(ulong voicechannel);

        Task Leave(ulong voicechannel);

        Task<Tuple<AbsVideoInfo, List<AbsVideoInfo>>> Queue(ulong voicechannel);

        void AddToQueue(ulong channelid, List<AbsVideoInfo> videoInfos);

        Task AddCustomReducer(ulong voicechannel, AbsStreamReducer reducer);

    }
}
