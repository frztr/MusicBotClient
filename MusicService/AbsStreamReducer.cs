using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBotClient.MusicService
{
    public abstract class AbsStreamReducer
    {
        //public bool IsRunning { get; protected set; } = true;

        protected const int ARRAY_SIZE = 3840;

        protected const int BYTES_IN_SECOND = 192000;

        public abstract Task Execute(Stream stream);

        public abstract Task Destroy();

        public OnEndHandler OnEnd { get; set; }

        public delegate void OnEndHandler(AbsStreamReducer abs);
    }
}
