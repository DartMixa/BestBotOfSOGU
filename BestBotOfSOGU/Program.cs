using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

// В тексте комментариев много синтаксических ошибок)))
namespace BestBotOfSOGU
{
    //Основной класс
    public class Program
    {
        //Основной список юзеров, который передаётся в каждую игру
        static public Dictionary<long, User> Users = [];
        //Список всех игр в боте, чтобы добавить игру просто допишите её в список
        static public List<IGame> Games = [];
        //токен бота
        private readonly static string Token = "8225210157:AAE2UPEDXliKC3sOn1lr3laZA3WgzfT7E7o";
        // собственно сам бот он передаётся в каждую игру
        static public TelegramBotClient bot = new(Token);

        // функция запуска бота и инициализации всех игр
        public static void Main()
        {
            foreach (var game in Games)
            {
                // инициализация игры
                game.Init(Users, bot);
                game.EndGame += ToStartScrin;
            }
            bot.OnUpdate += Bot_OnUpdate;
            Console.ReadKey();
        }

        private static async void ToStartScrin(User user)
        {
            user.CurentGame = null;
            await SendStartMessage(user);
        }

        private static async Task Bot_OnUpdate(Update update)
        {
            // определение юзера, юзер хранится в CurentUser
            Chat? user = null;
            // находим chat из сообщения или колбека
            if (update.Message is not null) user = update.Message.Chat;
            if (update.CallbackQuery?.Message is not null) user = update.CallbackQuery.Message.Chat;
            //если update не является сообщением или колбеком завершаем метод
            if (user is null)
            {
                Console.WriteLine("Не удалось определить ChatId");
                throw new Exception();
            }
            //если такого юзера ещё нет в списке добавляем
            if (!Users.ContainsKey(user.Id)) Users[user.Id] = new(user);
            
            // берём юзера из списка
            var CurentUser = Users[user.Id];

            // проверка если пользователь написал старт
            if (update.Message is not null)
            {
                if (update.Message.Text == "/start")
                {
                    //завершение работы всех игр (даже если они не работают с этим юзером)
                    foreach (var game in Games)
                    {
                        game.Break(CurentUser);
                    }
                    // очистка текущей игры юзера
                    CurentUser.CurentGame = null;
                    await SendStartMessage(CurentUser);
                }
            }
            //проверка если юзер выбрал игру
            if (CurentUser.CurentGame is null)
            {
                if (update.CallbackQuery?.Message is not null)
                {
                    // находим нужную игру из списка игр
                    foreach (var game in Games)
                    {
                        if (game.Info.CallbackData == update.CallbackQuery.Data)
                        {
                            // делаем эту игру текущей
                            CurentUser.CurentGame = game.Info.gameType;
                            // запускаем игру для текущего юзера
                            game.Start(CurentUser);
                            break;
                        }
                    }
                }
            }
            else 
            {
                // если текущая игра у юзера есть то передаём ей информацию об update
                foreach (var game in Games)
                {
                    if (game.Info.gameType == CurentUser.CurentGame)
                    {
                        game.Update(CurentUser, update);
                        break;
                    }
                }
            }
        }

        // вывод стартового сообщения
        static private async Task<Message> SendStartMessage(User user)
        {
            string text = "Выбери игру";

            // перевод Game.Info в InlineKeyboardButton
            InlineKeyboardButton[][] markup = Games.Select(x => new InlineKeyboardButton[]{ InlineKeyboardButton.WithCallbackData(x.Info.gameName, x.Info.CallbackData)}).ToArray();

            return await bot.SendMessage(user.Id, text, replyMarkup: markup);
        }
    }
    // список игр который будет использоваться для определения текущей игры
    public enum Games
    {
        
    }
    // это класс User он используется для получения основной информации о юзере !!!ОН НЕ ХРАНИТ ИНФОРМАЦИЮ ИГР!!!
    public class User(long id, string? userName)
    {
        // Id юзера совподает с Id чата юзера
        public long Id = id;
        // юзернейм в тг
        public string? UserName = userName;
        // текущая игра если null то юзер находится на стартовом сообщении
        public Games? CurentGame;
        // удобный конструктор
        public User(Telegram.Bot.Types.Chat chat) : this(chat.Id, chat.Username) { }
        // переопределения сравнителя позволяет сравнивать юзеров по Id и сравнивать юзеров с числави
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
        // информация о игре содержит то что бот выводит на стартовом экране для выбора игры а также название игры из enum Games
        public GameInfo Info { get; }
        // эта функция вызывается из мейна при запуске бота и передаёт список игроков и бота
        void Init(Dictionary<long, User> users, TelegramBotClient bot) { }
        // запускается когда игрок user выберает эту игру
        void Start(User user) { }
        // запускается когда игрок принудитльно покидает эту игру, может быть вызван даже если игрок в эту игру в текущий момент не играет
        void Break(User user) { }
        // во время игры передаёт игре update игрока
        void Update(User user, Update update) { }
        // вызовите это событие при завершении игры, чтобы бот вернул игрока на главный экран
        event Action<User> EndGame;
    }
    // здесь информация о игре
    public class GameInfo(string name, string callbackData)
    {
        // это будет выведено на стартовый экран внутри инлайна для выбора игры
        public string gameName = name;
        // это колбек который будет привязан к инлайну
        public string CallbackData = callbackData;
        // это то что будет у игрока в CurentGame при выборе этой игры
        public Games gameType;
    }
}