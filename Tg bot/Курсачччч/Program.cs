using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json.Converters;
using System;
using System.Net;
using System.Runtime.CompilerServices;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;



namespace Курсач
{
    class Program
    {
        private static ITelegramBotClient botClient;
        private static YouTubeService youtubeService;
        private static YoutubeClient youtubeClient;

        private static string youtubeApiKey = "AIzaSyADrTme0513FUbqJ4ktaE_1GlyaD2m9Q1E";
        private static SearchResult lastSearchResult = null;
        private static ReplyKeyboardRemove keyboardRemove = new ReplyKeyboardRemove();
        private static Action<ITelegramBotClient, Message> handleMessage;
        static void Main(string[] args)
        {
            botClient = new TelegramBotClient("5682413535:AAH8rTupp-yyetDX5H8xChnuLd4jJozRq3Y");
            youtubeClient = new YoutubeClient();
            botClient.StartReceiving(Update, Error );
            youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = youtubeApiKey,
                ApplicationName = "FindAllKindMusicBot"
            });
            handleMessage = HandleCommand;
            Console.ReadLine();
            
        }

        #region message handlers
        static async void HandleCommand(ITelegramBotClient botClient, Message message)
        {
            if (message.Type == MessageType.Text)
            {
                if (message.Text.StartsWith("/start") || message.Text.StartsWith("/help"))
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Чтобы начать поиск, напишите /search и название песни");

               else if (message.Text.StartsWith("/search"))
                     AvailabilityCheck(botClient, message);
                else
               await botClient.SendTextMessageAsync(message.Chat.Id, "Введённой команды нет в списке команд данного бота");
            }
            else
                await botClient.SendTextMessageAsync(message.Chat.Id, "Данный бот принимает только текстовые сообщения");
        }

        static async void HandleSearch(ITelegramBotClient botClient,Message message)
        {
            string videoId = lastSearchResult.Id.VideoId;
            await botClient.SendTextMessageAsync(message.Chat.Id, "обработка запроса...", replyMarkup: new ReplyKeyboardRemove());

            if (message.Text.Contains("Ссылку на Ютуб"))
            {
                Console.WriteLine("url");
                SendURL(message, lastSearchResult, videoId);
                lastSearchResult = null;
                handleMessage = HandleCommand;               
            }

            else if (message.Text.Contains("Аудиофайл"))
            {
                Console.WriteLine("file");
                SendFile(message, lastSearchResult, videoId);
                lastSearchResult = null;
                handleMessage = HandleCommand;
            }
            else
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введён некорректный запрос");
        }
#endregion


        async static Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;
            handleMessage(botClient, message);
          
        }

        #region processing requests
        private static async Task AvailabilityCheck(ITelegramBotClient botClient, Message? message)
        {
            var query = message.Text.Replace("/search", "").Trim();

            var searchResults = await SearchYoutubeVideo(query);
            Console.WriteLine($"{query}");
            if (searchResults.Items.Any())
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Выберите, что бы вы хотели получить", replyMarkup: GetButtons());
                Console.WriteLine("Found");
                lastSearchResult = searchResults.Items.First();
                handleMessage = HandleSearch;

            }
            else
                await botClient.SendTextMessageAsync(message.Chat.Id, "По вашему запросу ничего не найдено.");

        }
        private static async Task<SearchListResponse> SearchYoutubeVideo(string query)
        {
            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = query;
            searchListRequest.MaxResults = 1;
            return await searchListRequest.ExecuteAsync(); ;
        }
#endregion



        #region bot response
        private static async Task SendFile(Message message, SearchResult lastSearchResult, string videoId)
        {
            var video = await youtubeClient.Videos.GetAsync(videoId);
            var streamInfoSet = await youtubeClient.Videos.Streams.GetManifestAsync(videoId);
            var audioStreamInfo = streamInfoSet.GetAudioStreams().GetWithHighestBitrate();

            using (var client = new WebClient())
            using (var stream = await client.OpenReadTaskAsync(audioStreamInfo.Url))
            {
                await botClient.SendAudioAsync(message.Chat.Id, InputFile.FromStream(stream), title: lastSearchResult.Snippet.Title);
            }
        }


        private static async Task SendURL(Message message, SearchResult lastSearchResult, string videoId)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
          {
            InlineKeyboardButton.WithUrl("Watch on YouTube", $"https://www.youtube.com/watch?v={videoId}")
          });
          await botClient.SendTextMessageAsync(message.Chat.Id, $"Title: {lastSearchResult.Snippet.Title}\n\nDescription: {lastSearchResult.Snippet.Description}", replyMarkup: inlineKeyboard);
           
        }



        private static IReplyMarkup GetButtons()
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
            {
                new KeyboardButton[] { "Ссылку на Ютуб", "Аудиофайл" },
            });
            return replyKeyboardMarkup;
        }
#endregion


        
        private static Task Error(ITelegramBotClient botClient, Exception exception, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}