using BestBotOfSOGU;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;
using User = BestBotOfSOGU.User;

namespace NavalBattle
{
    class NavalBattle : IGame
    {
        static public TelegramBotClient bot;
        static public Dictionary<User, UserSession> Users = [];
        static public Dictionary<int, Room> LockedRooms = [];
        static public Dictionary<int, Room> OpenRooms = [];

        public event Action<User> EndGame;

        public GameInfo info = new("Морской бой", "Морской бой", Games.NavalBattle);
        public GameInfo Info => info;

        public static async Task<Message> SendHomeMessage(UserSession CurentUserSession, string text)
        {
            var markup = new InlineKeyboardButton[][]
                    {
                        [InlineKeyboardButton.WithCallbackData("Создать игру", "Создать игру"),
                        InlineKeyboardButton.WithCallbackData("Найти игру", "Найти игру")],
                    };
            return await bot.SendMessage(CurentUserSession.user.Id, text, replyMarkup: markup);
        }

        public async Task Start(User user)
        {
            Users[user] = new(user);
            Users[user].LastMessageId = (await SendHomeMessage(Users[user], "давай играть в морской бой")).Id;
            Users[user].State = UserState.UserStateInMainMenu;
        }

        public async Task Update(User user, Update update)
        {
            var curentUserSession = Users[user];
            switch (curentUserSession.State)
            {
                case UserState.UserStateDefoult:
                    break;
                case UserState.UserStateInMainMenu:
                    if (update.CallbackQuery?.Data is not null)
                    {
                        switch (update.CallbackQuery.Data)
                        {
                            case "Создать игру":
                                var mess = await bot.EditMessageText(curentUserSession.user.Id, (int)curentUserSession.LastMessageId, "Придумайте пароль", replyMarkup: InlineKeyboardButton.WithCallbackData("Назад", "Назад"));
                                var room = new Room(curentUserSession);
                                curentUserSession.State = UserState.UserStateCreatingRoom;
                                curentUserSession.LastMessageId = mess.Id;
                                curentUserSession.CurentRoom = room;
                                break;
                            case "Найти игру":
                                mess = await bot.EditMessageText(curentUserSession.user.Id, (int)curentUserSession.LastMessageId, "Введите номер комнаты", replyMarkup: InlineKeyboardButton.WithCallbackData("Назад", "Назад"));
                                curentUserSession.State = UserState.UserStateSelectRoomToConnect;
                                curentUserSession.LastMessageId = mess.Id;
                                break;
                            default: break;
                        }
                    }
                    break;
                case UserState.UserStateCreatingRoom:
                    if (update.Message?.Text is not null)
                    {
                        curentUserSession.CurentRoom.passvord = update.Message.Text;
                        await bot.DeleteMessages(curentUserSession.user.Id, [(int)curentUserSession.LastMessageId, update.Message.Id]);
                        curentUserSession.State = UserState.UserStateInRoom;
                        LockedRooms[curentUserSession.CurentRoom.Id] = curentUserSession.CurentRoom;
                        await curentUserSession.CurentRoom.SendUpdateRoomMessage();
                    }
                    break;
                case UserState.UserStateInRoom:
                    if (update.CallbackQuery is not null) 
                    {
                        await curentUserSession.CurentRoom.RoomOnUpdate(curentUserSession, update);
                    }
                    if (update.Message is not null) 
                    {
                        await curentUserSession.CurentRoom.RoomOnMessage(curentUserSession, update.Message);
                    }
                    break;
                case UserState.UserStateSelectRoomToConnect:
                    if (update.Message?.Text is not null) 
                    {
                        if (int.TryParse(update.Message.Text, out var roomId) && LockedRooms.ContainsKey(roomId))
                        {
                            await bot.DeleteMessages(curentUserSession.user.Id, [update.Message.Id]);
                            var mess = await bot.EditMessageText(curentUserSession.user.Id, (int)curentUserSession.LastMessageId, "Введите пароль", replyMarkup: InlineKeyboardButton.WithCallbackData("Назад", "Назад"));
                            curentUserSession.LastMessageId = mess.Id;
                            curentUserSession.CurentRoom = LockedRooms[roomId];
                            curentUserSession.State = UserState.UserStateConnectingToRoom;
                        }
                        else
                        {
                            await bot.SendMessage(curentUserSession.user.Id, "Такой комноты нет");
                            await bot.DeleteMessages(curentUserSession.user.Id, [(int)curentUserSession.LastMessageId, update.Message.Id]);
                            var mess = await SendHomeMessage(curentUserSession, "давай играть в морской бой");
                            curentUserSession.State = UserState.UserStateInMainMenu;
                            curentUserSession.LastMessageId = mess.Id;
                            curentUserSession.CurentRoom = null;
                        }
                    }
                    break;
                case UserState.UserStateConnectingToRoom:
                    if (update.Message?.Text is not null)
                    {
                        if (curentUserSession.CurentRoom.ConnectWithPassword(curentUserSession, update.Message.Text))
                        {
                            await bot.DeleteMessages(curentUserSession.user.Id, [(int)curentUserSession.LastMessageId, update.Message.Id]);
                            curentUserSession.State = UserState.UserStateInRoom;
                            LockedRooms.Remove(curentUserSession.CurentRoom.Id);
                            await curentUserSession.CurentRoom.SendUpdateRoomMessage();
                        }
                        else
                        {
                            await bot.SendMessage(curentUserSession.user.Id, "Это не правельный пароль");
                            await bot.DeleteMessages(curentUserSession.user.Id, [(int)curentUserSession.LastMessageId, update.Message.Id]);
                            var mess = await SendHomeMessage(curentUserSession, "давай играть в морской бой");
                            curentUserSession.State = UserState.UserStateInMainMenu;
                            curentUserSession.LastMessageId = mess.Id;
                            curentUserSession.CurentRoom = null;
                        }
                    }
                    break;
                default:
                    break;
            }
            if (update.CallbackQuery?.Data is not null && update.CallbackQuery?.Data == "Назад")
            {
                await bot.DeleteMessages(curentUserSession.user.Id, [(int)curentUserSession.LastMessageId]);
                var mess = await SendHomeMessage(curentUserSession, "давай играть в морской бой");
                curentUserSession.State = UserState.UserStateInMainMenu;
                curentUserSession.LastMessageId = mess.Id;
                curentUserSession.CurentRoom = null;
            }
        }

        public async Task Break(User user)
        {
            if (Users.ContainsKey(user))
            {
                var curentUserSession = Users[user];
                if (curentUserSession.CurentRoom is not null)
                {
                    if (curentUserSession.State == UserState.UserStateInRoom && curentUserSession.CurentRoom.SecondUser is not null)
                    {
                        await bot.SendMessage(curentUserSession.CurentRoom.SecondUser.user.Id, "Противник покинул игру");
                        var mess = await SendHomeMessage(curentUserSession, "давай играть в морской бой");
                        curentUserSession.State = UserState.UserStateInMainMenu;
                        curentUserSession.LastMessageId = mess.Id;
                        curentUserSession.CurentRoom = null;
                    }
                }
                Users.Remove(user);
            }
        }

        public void Init(TelegramBotClient bot)
        {
            NavalBattle.bot = bot;
        }
    }
    public class UserSession(User user)
    {
        public User user = user;
        public UserState State = UserState.UserStateDefoult;
        public int? LastMessageId;
        public Room? CurentRoom;
        public void ToDefoultStait()
        {
            State = UserState.UserStateDefoult;
        }
    }

    public enum UserState 
    {
        UserStateDefoult,
        UserStateInMainMenu,
        UserStateCreatingRoom,
        UserStateInRoom,
        UserStateSelectRoomToConnect,
        UserStateConnectingToRoom
    }

    enum RoomState
    {
        Batle,
        ArrangeShips,
        OutOfGame
    }

    public class Room(UserSession owner)
    {
        public static Random random = new();
        public int Id = random.Next(10000000);
        public UserSession Owner = owner;
        public UserSession? SecondUser;
        public string? passvord;
        int? OwnerRoomMessageId;
        int? SecondUserRoomMessageId;
        RoomState state = RoomState.OutOfGame;
        Field? fieldOwner;
        Field? fieldSecondUser;
        UserSession? Move;

        public string Name { get { return $"Комнота {Owner.user.UserName} № {Id}"; } }
        public bool Connect(UserSession user)
        {
            if (SecondUser is null)
            {
                SecondUser = user;
                return true;
            }
            return false;
        }
        public bool ConnectWithPassword(UserSession user, string? passvord)
        {
            if (passvord != this.passvord)
            {
                return false;
            }
            if (SecondUser is null)
            {
                SecondUser = user;
                return true;
            }
            return false;
        }
        public async Task SendUpdateRoomMessage()
        {
            var text = CriateRoomMessageText();
            if (SecondUser is null)
            {
                if (OwnerRoomMessageId is null)
                    OwnerRoomMessageId = (await NavalBattle.bot.SendMessage(Owner.user.Id, text)).Id;
                else await NavalBattle.bot.EditMessageText(Owner.user.Id, (int)OwnerRoomMessageId, text);
            }

            else
            {
                if (OwnerRoomMessageId is null)
                    OwnerRoomMessageId = (await NavalBattle.bot.SendMessage(Owner.user.Id, text)).Id;
                else await NavalBattle.bot.EditMessageText(Owner.user.Id, (int)OwnerRoomMessageId, text, replyMarkup: InlineKeyboardButton.WithCallbackData("Начать", "Начать"));

                if (SecondUserRoomMessageId is null)
                    SecondUserRoomMessageId = (await NavalBattle.bot.SendMessage(SecondUser.user.Id, text)).Id;
                else await NavalBattle.bot.EditMessageText(SecondUser.user.Id, (int)SecondUserRoomMessageId, text);
            }
        }
        public string CriateRoomMessageText()
        {
            string text = $"Название комнаты: {Name} \nИгроки: \n{Owner.user.UserName}\nvs\n{((SecondUser is not null) ? SecondUser.user.UserName : "Ожидание игрока")}";
            return text;
        }
        public async Task RoomOnUpdate(UserSession CurentUser, Telegram.Bot.Types.Update update)
        {
            if (state == RoomState.OutOfGame && CurentUser == Owner && update.CallbackQuery.Data == "Начать")
            {
                state = RoomState.ArrangeShips;
                await StartArrangeShips();
                await ArrangeShipsUpdateMessage();
            }
        }
        public async Task RoomOnMessage(UserSession CurentUser, Telegram.Bot.Types.Message message)
        {
            await Program.bot.DeleteMessages(CurentUser.user.Id, [message.Id]);
            if (state == RoomState.ArrangeShips)
            {
                var text = GenerateArrangeShipMessageText();
                var field = CurentUser == Owner ? fieldOwner : fieldSecondUser;
                var ms = message.Text?.Split().ToList();

                if (ms is not null && ms.Count == 4 && int.TryParse(ms[1], out int y) && int.TryParse(ms[2], out int type))
                {
                    string x = ms[0].ToUpper();
                    string side = ms[3].ToLower();
                    Console.WriteLine(x + " " + y + " " + type + " " + side);

                    if (!field.AddShip(x, y, type, side))
                    {
                        text += "\nвы где то ошиблись";
                    }
                }
                else
                {
                    text += "\nвы где то ошиблись";
                }
                if (field.Ready)
                {
                    text = "Расстановка кораблей завершина, ожидание соперника";
                }
                UpdateFieldMassage(NavalBattle.bot, CurentUser, field, (int)(CurentUser == Owner ? OwnerRoomMessageId : SecondUserRoomMessageId), text);
                if (fieldOwner.Ready && fieldSecondUser.Ready)
                {
                    state = RoomState.Batle;
                    await StartBattle();
                }
            }
            else if (state == RoomState.Batle)
            {
                if (CurentUser == Move)
                {
                    var field = CurentUser == Owner ? fieldSecondUser : fieldOwner;
                    var ms = message.Text?.Split().ToList();
                    if (int.TryParse(ms[1], out int y))
                    {
                        string x = ms[0].ToUpper();
                        if (field.Shoot(x, y, out bool hit))
                        {
                            await UpdateBattle();
                            if (!hit)
                            {
                                Move = (Move == Owner) ? SecondUser : Owner;
                            }
                            else 
                            {
                                if (field.AliveShips == 0) 
                                {
                                    await NavalBattle.bot.DeleteMessages(Owner.user.Id, [(int)OwnerRoomMessageId]);
                                    await NavalBattle.bot.DeleteMessages(SecondUser.user.Id, [(int)SecondUserRoomMessageId]);
                                    OwnerRoomMessageId = null;
                                    SecondUserRoomMessageId = null;
                                    SendUpdateRoomMessage();
                                }
                            }
                        }
                        else 
                        {
                            UpdateBattle("\nВы ошиблись");
                        }
                    }
                    else
                    {
                        UpdateBattle("\nВы ошиблись");
                    }
                }
            }
        }
        public async Task StartArrangeShips()
        {
            fieldOwner = new();
            fieldSecondUser = new();
        }
        public async Task ArrangeShipsUpdateMessage() 
        {
            var text = GenerateArrangeShipMessageText();

            await UpdateFieldMassage(NavalBattle.bot, Owner, fieldOwner, (int)OwnerRoomMessageId, text);

            await UpdateFieldMassage(NavalBattle.bot, SecondUser, fieldSecondUser, (int)SecondUserRoomMessageId, text);
        }
        public async Task UpdateFieldMassage(ITelegramBotClient botClient, UserSession user, Field field, int messageToUpdate, string text)
        {
            using (Image image = field.DrawField())
            using (MemoryStream stream = new MemoryStream())
            {
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;

                var inputFile = new InputFileStream(stream, "field.png");
                var inputMedia = new InputMediaPhoto(inputFile) { Caption = text };

                await botClient.EditMessageMedia(user.user.Id, messageToUpdate, inputMedia);
            }
        }
        public async Task Update2FieldMassage(ITelegramBotClient botClient, UserSession user, Field field1, Field field2, int messageToUpdate, string text)
        {
            using (Image image = Field.DrawTwoFields(field1, field2))
            using (MemoryStream stream = new MemoryStream())
            {
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;

                var inputFile = new InputFileStream(stream, "field.png");
                var inputMedia = new InputMediaPhoto(inputFile) { Caption = text };

                await botClient.EditMessageMedia(user.user.Id, messageToUpdate, inputMedia);
            }
        }
        public string GenerateArrangeShipMessageText() 
        {
            return "Чтобы добавить корабль необходимо:\nУказать букву\nуказать цифру\nуказать количество палуб\nуказать направление: w - вправо, h - вниз";
        }
        public async Task StartBattle() 
        {
            Move = random.Next(2) == 0 ? Owner : SecondUser;

            await UpdateBattle();
        }
        public async Task UpdateBattle(string text = "")
        {
            await Update2FieldMassage(NavalBattle.bot, Owner, fieldOwner, fieldSecondUser, (int)OwnerRoomMessageId, Owner == Move ? "Ваш ход, введите координаты" + text : "Ход противника");

            await Update2FieldMassage(NavalBattle.bot, SecondUser, fieldSecondUser, fieldOwner, (int)SecondUserRoomMessageId, SecondUser == Move ? "Ваш ход, введите координаты" + text : "Ход противника");
        }
    }
}
