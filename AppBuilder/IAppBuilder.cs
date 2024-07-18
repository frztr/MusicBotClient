using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreMusicBot.AppBuilder
{
    internal interface IAppBuilder
    {
        void InitApp(IServiceCollection collection);
    }
}
