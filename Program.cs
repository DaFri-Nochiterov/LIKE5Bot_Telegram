using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Telesharp;
using Telesharp.Common.BotTypes;
using Telesharp.Common.Types;
using File = System.IO.File;

namespace LIKE5BOT
{
    // (i) Information:
    //      Please, don't edit this sources and don't use for your works!!!
    //      It's better to create your own.

    //                                                 by DaFri-Nochiterov
    internal class Program
    {
        public static Bot Bot;

        private static string _cacheDirectory;

        private static readonly Dictionary<int, DateTime> ClearedDatas = new Dictionary<int, DateTime>();

        private static Dictionary<string, int[,]> _indexes;

        #region Prog

        private static void Main()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Bot = new Bot(INSERT_YOUR_TOKEN_HERE)
            {
                Settings =
                {
                    InfoToConsole = true,
                    ExceptionsToConsole = true,
                    RequestsToConsole = true,
                    ResponsesToConsole = true,
                    Name = "LIKE5BOT",
                    GetProfile = true
                }
            };
            _cacheDirectory = Environment.CurrentDirectory + "\\WallCache\\";
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
            Run();
            Bot.WaitToDie();
        }

        #endregion

        private static void Run()
        {
            // Fix commands:
            //var helpCommand = new HelpCommand();
            var buttonsCommand = new WallpaperButtonsCommand();
            Bot.OnParseMessage += (sender, eventArgs) =>
            {
                // Checking for message text is null
                if (eventArgs.Message.Text == null) return;

                // Checking for texts
                switch (eventArgs.Message.Text.ToLower())
                {
                    case "✌️ ok, it's all":
                        Bot.Methods.SendMessage(eventArgs.Message.Chat, "😉", new ReplyKeyboardHide());
                        return;
                    case "🔠 menu":
                        buttonsCommand.Executed(eventArgs.Message);
                        return;
                }
                // Prepare message:
                var messageText = eventArgs.Message.Text;
                var indexOfName = messageText.IndexOf('@' + Bot.Me.UserName, StringComparison.OrdinalIgnoreCase);
                messageText = messageText.ToLower().Remove(0, 1);
                if (indexOfName != -1)
                {
                    messageText = messageText.Remove(indexOfName - 1, Bot.Me.UserName.Length + 1);
                }

                eventArgs.Message.Text = messageText;

                // Find command
                switch (eventArgs.Message.Text)
                {
                    case "wallpaper_auto":
                        SendWall(eventArgs.Message.Chat, "Auto");
                        break;
                    case "wallpaper_beauty":
                        SendWall(eventArgs.Message.Chat, "Beauty");
                        break;
                    case "wallpaper_cartoon":
                        SendWall(eventArgs.Message.Chat, "Cartoon");
                        break;
                    case "wallpaper_classic":
                        SendWall(eventArgs.Message.Chat, "Classic");
                        break;
                    case "wallpaper_game":
                    case "wallpaper_games":
                        SendWall(eventArgs.Message.Chat, "Game");
                        break;
                    case "wallpaper_holiday":
                        SendWall(eventArgs.Message.Chat, "Holiday");
                        break;
                    case "wallpaper_movies":
                    case "wallpaper_films":
                        SendWall(eventArgs.Message.Chat, "Movies");
                        break;
                    case "wallpaper_scenery":
                    case "wallpaper_nature":
                    case "wallpaper_landscape":
                        SendWall(eventArgs.Message.Chat, "Scenery");
                        break;
                    case "wallpaper_sports":
                    case "wallpaper_sport":
                        SendWall(eventArgs.Message.Chat, "Sports");
                        break;
                    case "help":
                    case "start":
                        // Help command
                        buttonsCommand.Executed(eventArgs.Message);
                        //helpCommand.Executed(eventArgs.Message);
                        break;
                    case "wallpaper":
                        buttonsCommand.Executed(eventArgs.Message);
                        break;
                    default:
                        // Trying to search other commands
                        CheckCommand(eventArgs.Message);
                        break;
                }
            };
            Bot.Run();
        }

        public static void SendWall(Chat chat, string category)
        {
            // Getting ranges of wallpapers
            var randrange = GetRange(category, GetRandomBoolean());
            if (randrange.Length == 0)
            {
                Bot.Methods.SendMessage(chat, "⚠️ Sorry, unable to connect with LIKE5.");
                return;
            }
            // Sending preview:
            SendPreviewWall(chat, (new Random()).Next(randrange[0], randrange[1]), category);
        }

        public static void SendFullWall(Chat chat, int id, string category)
        {
            var uploadDone = false;
            Task.Factory.StartNew(() =>
            {
                while (!uploadDone)
                {
                    Bot.Methods.SendChatAction(chat, "upload_document");
                    Thread.Sleep(4999);
                }
            });
            Task.Factory.StartNew(delegate
            {
                var filename = _cacheDirectory + id + ".jpg";
                var readyId = '_' + category.ToLower() + '_' +
                              Path.GetFileName(filename).Replace(".jpg", "");
                if (!File.Exists(filename))
                {
                    var uri = $"http://hao.newtabplus.com/cloudWallpaper/{category}/{id}.jpg";
                    try
                    {
                        var wc = new WebClient();
                        wc.DownloadFile(uri, filename);
                    }
                    catch (WebException exc)
                    {
                        var errorResponse = exc.Response as HttpWebResponse;
                        if (errorResponse == null) return;
                        switch (errorResponse.StatusCode)
                        {
                            case HttpStatusCode.NotFound:
                                Bot.Methods.SendMessage(chat, "⚠️ This wallpaper not found.");
                                break;
                            default:
                                Bot.Methods.SendMessage(chat, "⚠️ Error, when download image. Try again in 5 seconds:\n"
                                                              + "/fullsize" + readyId);
                                break;
                        }
                        uploadDone = true;
                        return;
                    }
                }
                var fileInfo = new FileInfo(filename);
                if (IsFileLocked(fileInfo))
                {
                    Bot.Methods.SendMessage(chat, "Please wait...");
                    while (IsFileLocked(fileInfo)) { }
                }
                var markup = new ReplyKeyboardMarkup()
                {
                    Keyboard = new[]
                    {
                        new[] { $"/wallpaper_{category.ToLower()}" },
                        new[] { "🔠 Menu", "✌️ Ok, it's all" }
                    },
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };
                var message = Bot.Methods.SendDocumentFile(chat, filename, markup);
                uploadDone = true;
                if (message != null)
                {
                    Bot.Methods.SendMessage(chat, "💡 If you like this wallpapers visit LIKE5:\nhttp://www.like5.com 😉",
                        true);
                }
                else
                {
                    Bot.Methods.SendMessage(chat, "We are sorry, but something went wrong.");
                }
            });
        }

        public static void SendPreviewWall(Chat chat, int id, string category)
        {
            Bot.Methods.SendChatAction(chat, "upload_photo");
            Task.Factory.StartNew(delegate
            {
                var filename = Environment.CurrentDirectory + "\\WallCache\\s_" + id + ".jpg";
                var readyId = '_' + category.ToLower() + '_' +
                              Path.GetFileName(filename).Replace(".jpg", "").Replace("s_", "");
                if (!File.Exists(filename))
                {
                    var uri = $"http://hao.newtabplus.com/cloudWallpaper/{category}/s_{id}.jpg";
                    try
                    {
                        var wc = new WebClient();
                        wc.DownloadFile(uri, filename);
                    }
                    catch (WebException exc)
                    {
                        var errorResponse = exc.Response as HttpWebResponse;
                        if (errorResponse == null) return;
                        switch (errorResponse.StatusCode)
                        {
                            case HttpStatusCode.NotFound:
                                Bot.Methods.SendMessage(chat, "⚠️ This wallpaper not found.");
                                break;
                            default:
                                Bot.Methods.SendMessage(chat, "⚠️ Error, when get image. Try again in 5 seconds:\n"
                                                              + "/fullsize" + readyId);
                                break;
                        }
                        return;
                    }
                }
                var fileInfo = new FileInfo(filename);
                if (IsFileLocked(fileInfo))
                {
                    Bot.Methods.SendMessage(chat, "Please wait...");
                    while (IsFileLocked(fileInfo)) { }
                }
                // + "\n/wallpaper_" + category.ToLower() + " - next wallpaper from this category",
                var markup = new ReplyKeyboardMarkup()
                {
                    Keyboard = new[]
                    {
                        new[] {$"/wallpaper_{category.ToLower()}"},
                        new[] {$"/fullsize{readyId}"},
                        new[] { "🔠 Menu", "✌️ Ok, it's all" }
                    },
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };
                Bot.Methods.SendPhotoFile(chat, filename, markup, $"/fullsize{readyId}");
            });
        }

        public static void SendGoogleUrl(Message message, int id, string category)
        {
            var uri = $"http://hao.newtabplus.com/cloudWallpaper/{category}/s_{id}.jpg";
            Bot.Methods.SendMessage(message.From,
                "You can search this image in Google:\n" +
                $"https://images.google.com/searchbyimage?image_url={Uri.EscapeDataString(uri)}&image_content=&filename=&hl=en");
        }

        public static void CheckCommand(Message message)
        {
            var chat = message.Chat;
            var text = message.Text;
            try
            {
                if (text.IndexOf("fullsize_", StringComparison.Ordinal) == 0)
                {
                    text = text.Remove(0, 9);
                    var category = UppercaseFirst(text.Substring(0, text.IndexOf('_')));
                    text = text.Remove(0, text.IndexOf('_') + 1);
                    int id;
                    if (int.TryParse(text, out id))
                    {
                        SendFullWall(chat, id, category);
                    }
                }
                else if (text.IndexOf("preview_", StringComparison.Ordinal) == 0)
                {
                    text = text.Remove(0, 8);
                    var category = UppercaseFirst(text.Substring(0, text.IndexOf('_')));
                    text = text.Remove(0, text.IndexOf('_') + 1);
                    int id;
                    if (int.TryParse(text, out id))
                    {
                        SendPreviewWall(chat, id, category);
                    }
                }
                // else if (text.IndexOf("google_", StringComparison.Ordinal) == 0)
                // {
                //     text = text.Remove(0, 7);
                //     var category = UppercaseFirst(text.Substring(0, text.IndexOf('_')));
                //     text = text.Remove(0, text.IndexOf('_') + 1);
                //     int id;
                //     if (int.TryParse(text, out id))
                //     {
                //         SendGoogleUrl(message, id, category);
                //     }
                // }
            }
            catch
            {
                Bot.Methods.SendMessage(chat, "⚠️ Something wrong with your command.");
            }
        }

        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                stream?.Close();
            }

            //file is not locked
            return false;
        }

        private static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        private static Dictionary<string, int[,]> GetIndexes()
        {
            var categoriesIndexes = new Dictionary<string, int[,]>();
            var wc = new WebClient();
            string json;
            try
            {
                json = wc.DownloadString("http://hao.newtabplus.com/cloudWallpaper/index.json?t=0");
            }
            catch
            {
                return null;
            }
            Console.WriteLine(json);
            var array = JToken.Parse(json);

            foreach (var content in array.Children<JProperty>())
            {
                Console.WriteLine(content.Name + ": " + content.Value);
                // Create new massive of values for category:
                categoriesIndexes[content.Name.ToLower()] = new[,] {{0, 0}, {0, 0}};
                var strings = ((string) content.Value).Split(','); // Get indexes
                Array.Reverse(strings); // Reverse array
                var i = 0;
                foreach (var sStrings in strings.Select(s => s.Split('-')))
                {
                    Array.Reverse(sStrings); // Reverse this array
                    var i2 = 0;
                    foreach (var sString in sStrings)
                    {
                        int val;
                        if (!int.TryParse(sString, out val)) continue;
                        // Skip if can parse
                        categoriesIndexes[content.Name.ToLower()][i, i2] = val;
                        i2++;
                    }
                    i++;
                }
            }
            return categoriesIndexes;
        }

        public static int[] GetRange(string category, bool additionals)
        {
            if (_indexes == null)
            {
                var cacheIndexes = GetIndexes();
                if (cacheIndexes == null)
                {
                    return new int[0];
                }
                _indexes = cacheIndexes;
            }
            category = category.ToLower();
            if (!additionals) return new[] {_indexes[category][0, 0], _indexes[category][0, 1]};
            if (_indexes[category][1, 0] == 0 && _indexes[category][1, 1] == 0)
            {
                return new[] {_indexes[category][0, 0], _indexes[category][0, 1]};
            }
            return new[] {_indexes[category][1, 0], _indexes[category][1, 1]};
        }

        /// <summary>
        ///     Gets the random boolean.
        /// </summary>
        /// <returns></returns>
        public static bool GetRandomBoolean()
        {
            return (new Random(DateTime.Now.Millisecond)).Next(0, 2) == 0;
        }

        //private class HelpCommand : SimpleComparerCommand
        //{
        //    public HelpCommand()
        //    {
        //        CompareMode = Mode.BeginsWithTextFromOriginalMessage;
        //        HelpText = "Helpful command";
        //        Prototype = new Message(-1, "/help");
        //    }

        //    public override void Executed(Message message)
        //    {
        //        Bot.Methods.SendMessage(message.Chat, "Hello, i'm wallpaper bot!\n" +
        //                                              "I can send to you random wallpapers by catogories:\n" +
        //                                              "👻 /wallpaper_cartoon - favorite cartoons 😋\n" +
        //                                              "🎠 /wallpaper_classic\n" +
        //                                              "🗻 /wallpaper_scenery - Natural\n" +
        //                                              "📺 /wallpaper_movies\n" +
        //                                              "🎮 /wallpaper_games - Gamessssss*BOOM*👾\n" +
        //                                              "🏆 /wallpaper_sports\n" +
        //                                              "🎉 /wallpaper_holiday\n" +
        //                                              "👀 /wallpaper_beauty\n\n" +
        //                                              "If you want to get more wallpapers, visit:\n" +
        //                                              "🌎 http://www.like5.com/?tgbot");
        //    }
        //}

        private class WallpaperButtonsCommand : SimpleComparerCommand
        {
            public WallpaperButtonsCommand()
            {
                CompareMode = Mode.BeginsWithTextFromOriginalMessage;
                HelpText = "Display buttons with /wallpaper_* commands";
                Prototype = new Message(-1, "/wallpaper");
            }

            public override void Executed(Message message)
            {
                var markup = new ReplyKeyboardMarkup()
                {
                    Keyboard = new[]
                    {
                        new [] {"/wallpaper_cartoon", "/wallpaper_classic"},
                        new [] {"/wallpaper_scenery", "/wallpaper_movies"},
                        new [] {"/wallpaper_games", "/wallpaper_sports"},
                        new [] {"/wallpaper_holiday", "/wallpaper_beauty"}, 
                        new [] {"/help"}
                    },
                    ResizeKeyboard = true
                };
                Bot.Methods.SendMessage(message.Chat, "Hello, i'm wallpaper bot.\n" +
                                                      "I'll send randow wallpapers for these commands:\n" +
                                                      "👻 /wallpaper_cartoon - favorite cartoons 😋\n" +
                                                      "🎠 /wallpaper_classic\n" +
                                                      "🗻 /wallpaper_scenery - Natural\n" +
                                                      "📺 /wallpaper_movies\n" +
                                                      "🎮 /wallpaper_games - Gamessssss*BOOM*👾\n" +
                                                      "🏆 /wallpaper_sports\n" +
                                                      "🎉 /wallpaper_holiday\n" +
                                                      "👀 /wallpaper_beauty\n\n" +
                                                      "If you want to get list of wallpapers (no random), visit:\n" +
                                                      "🌎 http://www.like5.com/?tgbot", markup);
            }
        }
    }
}