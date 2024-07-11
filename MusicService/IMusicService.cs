using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicBotClient.AudioVoiceChannel;

namespace MusicBotClient.MusicService
{
    interface IMusicService
    {
        Task<string> Join(ulong voicechannel);

        Task Play(ulong voicechannel,string path);

        Task Stop(ulong voicechannel);

        Task<string> Leave(ulong voicechannel);

        Task<List<IAudioVoiceChannel>> getAudioVoiceChannels();
    }
}
