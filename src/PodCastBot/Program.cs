using System;
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

namespace PodCastBot
{
    class Program
    {
        static ILogger log;
        private static readonly TelegramBotClient Bot = new TelegramBotClient("267989730:AAH7VbASzQeOLWf8iLSdusooE00Pg_qlao4");

        static string StorePath = "podcasts.txt";
        static List<string> Store = System.IO.File.ReadAllLines(StorePath).ToList();

        public static class ApplicationLogging
        {
            public static ILoggerFactory LoggerFactory { get; } =
              new LoggerFactory();
            public static ILogger CreateLogger<T>() =>
              LoggerFactory.CreateLogger<T>();
        }

        static void Main(string[] args)
        {
            //COMMANDS ON LINUX
            //for running in background: nohup dotnet run&
            //ps -e
            //killall dotnet
            //init logs
            ApplicationLogging.LoggerFactory.AddNLog();
            ApplicationLogging.LoggerFactory.ConfigureNLog("NLog.config");
            log = ApplicationLogging.LoggerFactory.CreateLogger("my name");

            //ApplicationLogging.LoggerFactory.AddConsole();//then logs will be doubled, couse NLog.config have console type logs too :)
            log.LogCritical("Yeap!)"); log.LogError("Yeap!)");
            //end logs

            Bot.SetWebhookAsync("").Wait();
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;


            try
            {
                var me = Bot.GetMeAsync().Result;

                //Console.Title = me.Username;

            }
            catch (Exception e) { log.LogWarning(e, "не смогли узнать имя бота."); }
            Bot.StartReceiving();
            while (true) { Thread.Sleep(999999999); }//Console.ReadLine(); //instead this
            //Bot.StopReceiving();
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            log.LogWarning(receiveErrorEventArgs.ApiRequestException.Message);
            Thread.Sleep(5000);
            //Debugger.Break();
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            //Console.WriteLine($"Received choosen inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            InlineQueryResult[] results = {
                new InlineQueryResultLocation
                {
                    Id = "1",
                    Latitude = 40.7058316f, // displayed result
                    Longitude = -74.2581888f,
                    Title = "New York",
                    InputMessageContent = new InputLocationMessageContent // message if result is selected
                    {
                        Latitude = 40.7058316f,
                        Longitude = -74.2581888f,
                    }
                },

                new InlineQueryResultLocation
                {
                    Id = "2",
                    Longitude = 52.507629f, // displayed result
                    Latitude = 13.1449577f,
                    Title = "Berlin",
                    InputMessageContent = new InputLocationMessageContent // message if result is selected
                    {
                        Longitude = 52.507629f,
                        Latitude = 13.1449577f
                    }
                }
            };

            await Bot.AnswerInlineQueryAsync(inlineQueryEventArgs.InlineQuery.Id, results, isPersonal: true, cacheTime: 0);
        }

        static string StemByRuEn(string text)
        {
            var r = new RussianStemmer().Stem(text);
            var r2 = new EnglishStemmer().Stem(text);
            if (r.Length > r2.Length)//we chosee the text which have deleted more 
                return r2;
            return r;
        }

        /// <summary>
        /// Tuple: items rate, original list item
        /// </summary>
        /// <param name="SearchStr"></param>
        /// <param name="list"></param>
        /// <returns>Tuple: items rate, original list item</returns>
        static List<Tuple<int, string>> GetRatingBySearchStr(string SearchStr, IEnumerable<string> list)
        {
            var stemmedSearchTextTrimed = StemByRuEn(SearchStr).Trim();
            List<Tuple<int, string>> ratingBySearch = new List<Tuple<int, string>>();
            foreach (var podcastItem in list)
            {
                var countOfMachedWords = 0;
                foreach (var podcastWord in podcastItem
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(_ => !string.IsNullOrEmpty(_))
                    .Select(_ => _.Trim())
                    )
                {
                    countOfMachedWords += stemmedSearchTextTrimed
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Where(_ => podcastWord.Contains(_))
                        .Count();//hard Searching

                }
                if (countOfMachedWords > 0)
                    ratingBySearch.Add(Tuple.Create(countOfMachedWords, podcastItem));
            }
            return ratingBySearch;
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.TextMessage) return;

            //config
            if (message.Text.StartsWith("/add")) // добавить подкаст просто в наш список
            {
                var str = message.Text.Replace("/add", "").Replace(Environment.NewLine, "  ");

                System.IO.File.AppendAllText(StorePath, str);
                Store.Add(str);
                await Bot.SendTextMessageAsync(message.Chat.Id, "Спасибо за новый подкаст! Его увидят все мои 'подписчики'");
                return;
            }
            //поиск по тегам/умныйПоиск в  message.Text
            var mTextLower = message.Text.ToLower();
            if (mTextLower.StartsWith("/s") || mTextLower.StartsWith("/search") || mTextLower.StartsWith("/find"))
            {
                var SortedBySearch = GetRatingBySearchStr(mTextLower.Replace("/s",""), Store)
                    .OrderBy(_ => _.Item1)
                    .Select(_ => _.Item2);


                //выдача
                if (SortedBySearch.Any())
                {
                    var t = SortedBySearch.Aggregate((av, e) => av + e);
                    await Bot.SendTextMessageAsync(message.Chat.Id, t);
                }
                else
                    await Bot.SendTextMessageAsync(message.Chat.Id, @"Поиск не дал результатов. 
Чтобы вывести все подкасты - напишите /all");

                return;
            }


            //выдача
            await Bot.SendTextMessageAsync(message.Chat.Id, Store.Aggregate((av, e) => av + e)
                /*replyMarkup: keyboard*/);


            return;
            #region может приголится..
            if (message.Text.StartsWith("/inline")) // send inline keyboard
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        new InlineKeyboardButton("1.1"),
                        new InlineKeyboardButton("1.2"),
                    },
                    new[] // second row
                    {
                        new InlineKeyboardButton("2.1"),
                        new InlineKeyboardButton("2.2"),
                    }
                });

                await Task.Delay(500); // simulate longer running task

                await Bot.SendTextMessageAsync(message.Chat.Id, "Choose",
                    replyMarkup: keyboard);
            }
            else if (message.Text.StartsWith("/keyboard")) // send custom keyboard
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new [] // first row
                    {
                        new KeyboardButton("1.1"),
                        new KeyboardButton("1.2"),
                    },
                    new [] // last row
                    {
                        new KeyboardButton("2.1"),
                        new KeyboardButton("2.2"),
                    }
                });

                await Bot.SendTextMessageAsync(message.Chat.Id, "Choose",
                    replyMarkup: keyboard);
            }
            else if (message.Text.StartsWith("/photo")) // send a photo
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

                const string file = @"<FilePath>";

                var fileName = file.Split('\\').Last();

                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var fts = new FileToSend(fileName, fileStream);

                    await Bot.SendPhotoAsync(message.Chat.Id, fts, "Nice Picture");
                }
            }
            else if (message.Text.StartsWith("/request")) // request location or contact
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton("Location")
                    {
                        RequestLocation = true
                    },
                    new KeyboardButton("Contact")
                    {
                        RequestContact = true
                    },
                });

                await Bot.SendTextMessageAsync(message.Chat.Id, "Who or Where are you?", replyMarkup: keyboard);
            }
            else
            {
                var usage = @"Usage:
/inline   - send inline keyboard
/keyboard - send custom keyboard
/photo    - send a photo
/request  - request location or contact
";

                await Bot.SendTextMessageAsync(message.Chat.Id, usage,
                    replyMarkup: new ReplyKeyboardHide());
            }
            #endregion
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
                $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
        }
    }
}