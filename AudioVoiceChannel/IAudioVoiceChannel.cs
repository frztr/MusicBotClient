using Discord.Audio;
using MusicBotClient.MusicService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBotClient.AudioVoiceChannel
{
    interface IAudioVoiceChannel
    {
        ulong getId();

        IAudioClient getAudioClient();

        void Stop();
        void addStreamReducer(AbsStreamReducer reducer);
    }
}
