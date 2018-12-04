using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace PodCastBot.Services
{
    public class Settings
    {//cfg = builder.Build();

        public IConfigurationRoot cfg { get; set; }

        public Settings() => cfg = new ConfigurationBuilder().AddJsonFile("cfg.json").Build();
    }
}
