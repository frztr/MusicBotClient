using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreMusicBot
{
    internal class ApplicationContext
    {
        private static IServiceProvider sp = null;
        public static IServiceProvider ServiceProvider
        {
            get
            {
                return sp;
            }
            set 
            {
                if (sp == null) 
                {
                    sp = value;
                }
            }
        }
    }
}
