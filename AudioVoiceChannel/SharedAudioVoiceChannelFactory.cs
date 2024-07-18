using Discord;
using MusicBotClient.AudioVoiceChannel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreMusicBot.AudioVoiceChannel
{
    internal class SharedAudioVoiceChannelFactory : AbsAudioChannelFactory
    {
        public override IAudioVoiceChannel CreateChannel(ulong id, IAudioChannel channel)
        {
            return new SharedAudioVoiceChannel(id,channel);
        }
    }
}
