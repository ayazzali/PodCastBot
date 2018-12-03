using System;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Logging;
using NLog.Web;
using NLog.Extensions.Logging;
using Iveonik.Stemmers;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using PodCastBot.Services;

namespace PodCastBot
{
    public static class ApplicationLogging// don't use - couse nlog 5 works
    {
        static ApplicationLogging()
        {
            ApplicationLogging.LoggerFactory.AddNLog();
            ApplicationLogging.LoggerFactory.ConfigureNLog("NLog.config");
            ApplicationLogging.LoggerFactory.AddConsole();
        }
        public static ILoggerFactory LoggerFactory { get; } =
          new LoggerFactory();
        public static ILogger CreateLogger<T>() =>
          LoggerFactory.CreateLogger<T>();
    }
    class Program0
    {
        static void Main()
        {
            Console.WriteLine("Initialization Main");

            var rootPath = System.IO.Path.GetPathRoot(AppContext.BaseDirectory);
            var logDir = System.IO.Path.Combine(rootPath, "logs", "PodCastBot");
            System.IO.Directory.CreateDirectory(logDir);
            var logPath = System.IO.Path.Combine(logDir, "app.log");
            //System.IO.File.
            var serv = new ServiceCollection()
                .AddLogging(loggingBuilder => loggingBuilder/*.AddFile(logPath, append: true)*/.AddConsole())
                .AddSingleton<Settings>()
                .AddSingleton<PodcastBot>()
                .BuildServiceProvider();

            //serv.GetService<ILoggerFactory>()
            //    .AddConsole();

            var InstanceOfBot = serv.GetService<PodcastBot>();
            InstanceOfBot.Main();
            Console.WriteLine("0");
            //while (true) { Thread.Sleep(999999999); }
        }
    }
}