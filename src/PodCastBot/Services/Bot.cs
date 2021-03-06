﻿using System;
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

    class PodcastBot
    {
        public Settings cfg;

        //ILogger log;
        TelegramBotClient Bot;

        string StorePath = "podcasts.txt";
        List<string> Store;
        NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        HttpClient httpClient = new HttpClient();

        public PodcastBot(Settings c)
        {
            cfg = c;
            Bot = new TelegramBotClient(cfg.cfg["telegramKey"]);
            Store = System.IO.File.ReadAllLines(StorePath).ToList();
        }

        public void Main()
        {
            log.Error("nlog");
            //COMMANDS ON LINUX
            //for running in background: nohup dotnet run&
            //ps -e
            //killall dotnet

            //init logs

            //old log = ApplicationLogging.LoggerFactory.CreateLogger("my name");
            //ApplicationLogging.LoggerFactory.AddConsole();//then logs will be doubled, couse Nl.config have console type logs too :)
            log.Error("Yeap!)"); log.Error("Yeap!)");
            //end logs

            testToDo();
            //Bot.SetWebhookAsync("").Wait();
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
            catch (Exception e) { log.Warn(e, "не смогли узнать имя бота."); }
            Bot.StartReceiving();
            while (true) { Thread.Sleep(999999999); }//Console.ReadLine(); //instead this
                                                     //Bot.StopReceiving();
        }



        private void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            log.Warn(receiveErrorEventArgs.ApiRequestException.Message);
            Thread.Sleep(60 * 1000);
            //Debugger.Break();
        }

        private void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            //Console.WriteLine($"Received choosen inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        private async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
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

        string StemByRuEn(string text)
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
        List<Tuple<int, string>> GetRatingBySearchStr(string SearchStr, IEnumerable<string> list)
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

        List<Tuple<long, string>> MsgsHistory = new List<Tuple<long, string>>();
        private async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            log.Info(message.Chat.Id.ToString());
            if (message == null || message.Type != MessageType.TextMessage) return;

            //config
            ///сначала простые команды
            var mTextLower = message.Text.ToLower();
            if (mTextLower.StartsWith("/help") || mTextLower.StartsWith("/h"))
            {
                await Bot.SendTextMessageAsync(message.Chat.Id,
@"/search - searching
/add - add new podcast 
/all - shows all podcasts
/help - just such a command :)");
                return;
            }
            ///потом команды, нуждающиеся в продолжении
            if (mTextLower.StartsWith("/add")) // добавить подкаст просто в наш список
            {
                MsgsHistory.Add(Tuple.Create(message.Chat.Id, message.Text));
                await Bot.SendTextMessageAsync(message.Chat.Id,
                "Введите сылку на новый подкаст");
                return;
            }
            if (mTextLower.StartsWith("/s")) // добавить подкаст просто в наш список
            {
                MsgsHistory.Add(Tuple.Create(message.Chat.Id, message.Text));
                await Bot.SendTextMessageAsync(message.Chat.Id,
                @"Введите название языка или облость в которой хотите найти подкасты.
(Наиболее вероятные подкасты будут вверху списка)");
                return;
            }

            ///повторные сообщения- ответы после команды
            var msgForCmd = MsgsHistory.SingleOrDefault(_ => _.Item1 == message.Chat.Id);
            if (msgForCmd != null)
            {
                var x = MsgsHistory.Remove(msgForCmd);
                if (msgForCmd.Item2.StartsWith("/add"))
                {
                    var str = message.Text.Replace("/add", "").Replace(Environment.NewLine, "  ");

                    System.IO.File.AppendAllText(StorePath, str);
                    Store.Add(str);
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Спасибо за новый подкаст! Его увидят все мои 'подписчики'");
                    return;
                }
                //поиск по тегам/умныйПоиск в  message.Text
                if (msgForCmd.Item2.StartsWith("/s") || mTextLower.StartsWith("/search") || mTextLower.StartsWith("/find"))
                {
                    var SortedBySearch = GetRatingBySearchStr(mTextLower.Replace("/s", ""), Store)
                        .OrderBy(_ => _.Item1)
                        .Select(_ => _.Item2);


                    //выдача
                    if (SortedBySearch.Any())
                    {
                        var t = SortedBySearch.Aggregate((av, e) => av + Environment.NewLine + e);
                        await Bot.SendTextMessageAsync(message.Chat.Id, t, parseMode: ParseMode.Html);
                    }
                    else
                        await Bot.SendTextMessageAsync(message.Chat.Id, @"Поиск не дал результатов. 
Чтобы вывести все подкасты - напишите /all", parseMode: ParseMode.Html);

                    return;
                }

            }

            //in here i want to get mp3 or smth from link of podcast

            //выдача
            await Bot.SendTextMessageAsync(message.Chat.Id, Store.Aggregate((av, e) => av + Environment.NewLine + e), parseMode: ParseMode.Html
                /*replyMarkup: keyboard*/);


            return;
            #region может пригодится..
            //             if (message.Text.StartsWith("/inline")) // send inline keyboard
            //             {
            //                 await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            //                 var keyboard = new InlineKeyboardMarkup(new[]
            //                 {
            //                     new[] // first row
            //                     {
            //                         new InlineKeyboardButton("1.1"),
            //                         new InlineKeyboardButton("1.2"),
            //                     },
            //                     new[] // second row
            //                     {
            //                         new InlineKeyboardButton("2.1"),
            //                         new InlineKeyboardButton("2.2"),
            //                     }
            //                 });

            //                 await Task.Delay(500); // simulate longer running task

            //                 await Bot.SendTextMessageAsync(message.Chat.Id, "Choose",
            //                     replyMarkup: keyboard);
            //             }
            //             else if (message.Text.StartsWith("/keyboard")) // send custom keyboard
            //             {
            //                 var keyboard = new ReplyKeyboardMarkup(new[]
            //                 {
            //                     new [] // first row
            //                     {
            //                         new KeyboardButton("1.1"),
            //                         new KeyboardButton("1.2"),
            //                     },
            //                     new [] // last row
            //                     {
            //                         new KeyboardButton("2.1"),
            //                         new KeyboardButton("2.2"),
            //                     }
            //                 });

            //                 await Bot.SendTextMessageAsync(message.Chat.Id, "Choose",
            //                     replyMarkup: keyboard);
            //             }
            //             else if (message.Text.StartsWith("/photo")) // send a photo
            //             {
            //                 await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

            //                 const string file = @"<FilePath>";

            //                 var fileName = file.Split('\\').Last();

            //                 using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            //                 {
            //                     var fts = new FileToSend(fileName, fileStream);

            //                     await Bot.SendPhotoAsync(message.Chat.Id, fts, "Nice Picture");
            //                 }
            //             }
            //             else if (message.Text.StartsWith("/request")) // request location or contact
            //             {
            //                 var keyboard = new ReplyKeyboardMarkup(new[]
            //                 {
            //                     new KeyboardButton("Location")
            //                     {
            //                         RequestLocation = true
            //                     },
            //                     new KeyboardButton("Contact")
            //                     {
            //                         RequestContact = true
            //                     },
            //                 });

            //                 await Bot.SendTextMessageAsync(message.Chat.Id, "Who or Where are you?", replyMarkup: keyboard);
            //             }
            //             else
            //             {
            //                 var usage = @"Usage:
            // /inline   - send inline keyboard
            // /keyboard - send custom keyboard
            // /photo    - send a photo
            // /request  - request location or contact
            // ";

            //                 await Bot.SendTextMessageAsync(message.Chat.Id, usage,
            //                     replyMarkup: new ReplyKeyboardHide());
            //             }
            #endregion
        }

        private async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
                $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
        }

        void findAudios(object _site)
        {
            var site = (string)_site;
            if (site.Contains("("))
                site.Replace("(", "");
            if (site.Contains(")"))
                site = site.Replace(")", "");
            log.Trace(site);
            var doc = new HtmlDocument();
            try
            {
                var x = httpClient.GetAsync(site).Result;
                log.Trace(x.StatusCode.ToString());
                var htmlStream = x.Content.ReadAsStreamAsync().Result;
                doc.Load(htmlStream);
            }
            catch
            {
                log.Warn(" не смогли открыть сайт " + site);
                return;
            }

            var audios = doc.DocumentNode.Descendants("audio");
            if (doc.DocumentNode.InnerHtml.IndexOf("audio") > 0)
            {//l.Error("!!!!!!!!!!! audio exists on " + site);
            }
            else if (doc.DocumentNode.InnerHtml.IndexOf(".mp3") > 0)
            {
                log.Debug(".mp3 " + site);
                var gg = doc.DocumentNode.SelectNodes(".//a[@href.contains(.mp3)]");
                // смотрим на втором уровне сайта
                SendMe(site);
            }
            //l.Information(string.Join("\r\n", audios.ToList().Select(q => q.InnerHtml)));


            // if (audios.Count() > 0)
            // {
            //     audios.First().
            // }
        }

        void SendMe(string text)
        {
            Bot.SendTextMessageAsync(180504101, text);
        }

        IEnumerable<string> GetPodcasts()///get PodCasts ToDo: repository or easier
        {
            var str = httpClient.GetAsync("https://raw.githubusercontent.com/AveVlad/russia-it-podcast/master/README.md").Result;
            var sttr = ""; sttr = str.Content.ReadAsStringAsync().Result;

            var podcasts = sttr.Split("<br><hr><br>");
            return podcasts.Select(_ => _.Replace("--------------------------------------", "---")
            .Replace("\n\n", "\n"));
        }


        string GetUrlFromPodcast(string podcastText)//firstUrl from podcast text
        {
            var pT = podcastText;
            var firstUrl = podcastText.Substring(pT.IndexOf("[site](") + 7, pT.IndexOf(")", pT.IndexOf("[site](")) - pT.IndexOf("[site](") + 1 - 7);
            return firstUrl;
        }

        void testToDo()
        {

        }

    }
}
