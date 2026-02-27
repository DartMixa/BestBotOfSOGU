using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BestBotOfSOGU
{
    public class Program
    {
        static public Dictionary<long, User> Users = [];
        static public List<IGame> Games = [];
        private readonly static string Token = "8225210157:AAE2UPEDXliKC3sOn1lr3laZA3WgzfT7E7o";
        static public TelegramBotClient bot = new(Token);

        public static void Main()
        {
            foreach (var game in Games)
            {
                game.Init(Users, bot);
            }
            bot.OnUpdate += Bot_OnUpdate;
            Console.ReadKey();
        }

        private static async Task Bot_OnUpdate(Update update)
        {
            Chat? user = null;
            if (update.Message is not null) user = update.Message.Chat;
            if (update.CallbackQuery?.Message is not null) user = update.CallbackQuery.Message.Chat;
            if (user is null)
            {
                Console.WriteLine("Не удалось определить ChatId");
                throw new Exception();
            }
            if (!Users.ContainsKey(user.Id)) Users[user.Id] = new(user);
            
            var CurentUser = Users[user.Id];

            if (update.Message is not null)
            {
                if (update.Message.Text == "/start")
                {
                    foreach (var game in Games)
                    {
                        game.Break(CurentUser);
                    }
                    CurentUser.CurentGame = null;
                    await SendStartMessage(CurentUser);
                }
            }
            if (CurentUser.CurentGame is null)
            {
                if (update.CallbackQuery?.Message is not null)
                {
                    switch (update.CallbackQuery.Data)
                    {
                        default: break;
                    }

                }
            }
            else 
            {
                switch (CurentUser.CurentGame)
                {
                    default: break;
                }
            }
        }
        static private async Task<Message> SendStartMessage(User user) 
        {
            string text = "Выбери игру";
            var markup = new InlineKeyboardButton[][]
                    {
                        [InlineKeyboardButton.WithCallbackData("Морской бой", "Морской бой")],
                        [InlineKeyboardButton.WithCallbackData("Кто хочет стать миллионером", "Кто хочет стать миллионером")],
                        [InlineKeyboardButton.WithCallbackData("Быки и коровы", "Быки и коровы")],
                        [InlineKeyboardButton.WithCallbackData("Крестики нолики", "Крестики нолики")],
                    };
            return await bot.SendMessage(user.Id, text, replyMarkup: markup);
        }
    }
    public enum Games
    {
        
    }
    public class User(long id, string? userName)
    {
        public long Id = id;
        public string? UserName = userName;
        public Games? CurentGame;
        public User(Telegram.Bot.Types.Chat chat) : this(chat.Id, chat.Username) { }
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            else if (obj is User user) return Id.Equals(user.Id);
            else if (obj is long id) return Id.Equals(id);
            else return false;
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
    public interface IGame 
    {
        void Init(Dictionary<long, User> users, TelegramBotClient bot) { }
        void Break(User user) { }
        void Update(User user, Update update) { }
        event Action EndGame;
    }
}