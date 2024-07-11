using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBotClient.AudioVoiceChannel
{
    class StandartAudioChannelFactory : AbsAudioChannelFactory
    {
        public override IAudioVoiceChannel CreateChannel(ulong id, IAudioChannel channel)
        {
            return new AudioVoiceChannel(id,channel);
        }
    }
}
