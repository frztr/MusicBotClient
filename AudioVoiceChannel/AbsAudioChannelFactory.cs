using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBotClient.AudioVoiceChannel
{
    abstract class AbsAudioChannelFactory
    {
        public abstract IAudioVoiceChannel CreateChannel(ulong id,IAudioChannel channel);
    }
}
