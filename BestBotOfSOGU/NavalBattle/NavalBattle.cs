using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Image = System.Drawing.Image;

namespace NavalBattle
{
    static class NavalBattle : IGame
    {
        static public TelegramBotClient bot;
        static public Dictionary<User, UserSession> Users = [];
        static public Dictionary<int, Room> LockedRooms = [];
        static public Dictionary<int, Room> OpenRooms = [];
        private static async Task Bot_OnUpdate(Telegram.Bot.Types.Update update)
        {
            var CurentUserId = update.CallbackQuery.Message.Chat.Id;
            var CurentUser = Users[CurentUserId];
            if (CurentUser.State is UserStateInMainMenu stateInMainMenu)
            {
                switch (update.CallbackQuery.Data)
                {
                    case "Создать игру":
                        var mess = await bot.EditMessageText(CurentUser.Id, stateInMainMenu.LastMessageId, "Придумайте пароль", replyMarkup: InlineKeyboardButton.WithCallbackData("Назад", "Назад"));
                        var room = new Room(CurentUser);
                        CurentUser.State = new UserStateCreatingRoom(mess.Id, room);
                        break;
                    case "Найти игру":
                        mess = await bot.EditMessageText(CurentUser.Id, stateInMainMenu.LastMessageId, "Введите номер комнаты", replyMarkup: InlineKeyboardButton.WithCallbackData("Назад", "Назад"));
                        CurentUser.State = new UserStateSelectRoomToConnect(mess.Id);
                        break;
                    default: break;
                }
            }
            else if (CurentUser.State is UserStateCreatingRoom stateCreatingRoom)
            {
                await bot.DeleteMessages(CurentUser.Id, [stateCreatingRoom.LastMessageId]);
                var mess = await SendHomeMessage(CurentUser, "давай играть в морской бой");
                CurentUser.State = new UserStateInMainMenu(mess.Id);
            }
            else if (CurentUser.State is UserStateSelectRoomToConnect stateSelectRoomToConnect)
            {
                await bot.DeleteMessages(CurentUser.Id, [stateSelectRoomToConnect.LastMessageId]);
                var mess = await SendHomeMessage(CurentUser, "давай играть в морской бой");
                CurentUser.State = new UserStateInMainMenu(mess.Id);
            }
            else if (CurentUser.State is UserStateConnectingToRoom stateConnectingToRoom)
            {
                await bot.DeleteMessages(CurentUser.Id, [stateConnectingToRoom.LastMessageId]);
                var mess = await SendHomeMessage(CurentUser, "давай играть в морской бой");
                CurentUser.State = new UserStateInMainMenu(mess.Id);
            }
            else if (CurentUser.State is UserStateInRoom stateInRoom) 
            {
                stateInRoom.CurentRoom.RoomOnUpdate(CurentUser, update);
            }
        }
        private static async Task Bot_OnMessage(Telegram.Bot.Types.Message message, Telegram.Bot.Types.Enums.UpdateType type)
        {
            var CurentUserId = message.Chat.Id;

            if (!Users.ContainsKey(CurentUserId)) Users[CurentUserId] = new UserSession(message.Chat);

            var CurentUser = Users[CurentUserId];

            if (message.Text == "/start")
            {
                Console.WriteLine(message.Chat.Username);
                await bot.DeleteMessages(CurentUser.Id, [message.Id]);
                var mess = await SendHomeMessage(CurentUser, "давай играть в морской бой");
                CurentUser.State = new UserStateInMainMenu(mess.Id);
            }
            else if (CurentUser.State is UserStateCreatingRoom stateCreatingRoom)
            {
                stateCreatingRoom.CreateRoom.passvord = message.Text;
                await bot.DeleteMessages(CurentUser.Id, [stateCreatingRoom.LastMessageId, message.Id]);
                CurentUser.State = new UserStateInRoom(stateCreatingRoom.CreateRoom);
                LockedRooms[stateCreatingRoom.CreateRoom.Id] = stateCreatingRoom.CreateRoom;
                await stateCreatingRoom.CreateRoom.SendUpdateRoomMessage();

            }
            else if (CurentUser.State is UserStateSelectRoomToConnect stateSelectRoomToConnect)
            {
                if (int.TryParse(message.Text, out var roomId) && LockedRooms.ContainsKey(roomId))
                {
                    await bot.DeleteMessages(CurentUser.Id, [message.Id]);
                    var mess = await bot.EditMessageText(CurentUser.Id, stateSelectRoomToConnect.LastMessageId, "Введите пароль", replyMarkup: InlineKeyboardButton.WithCallbackData("Назад", "Назад"));
                    CurentUser.State = new UserStateConnectingToRoom(mess.Id, LockedRooms[roomId]);
                }
                else 
                {
                    await bot.SendMessage(CurentUser.Id, "Такой комноты нет");
                    await bot.DeleteMessages(CurentUser.Id, [stateSelectRoomToConnect.LastMessageId, message.Id]);
                    var mess = await SendHomeMessage(CurentUser, "давай играть в морской бой");
                    CurentUser.State = new UserStateInMainMenu(mess.Id);
                }
            }
            else if (CurentUser.State is UserStateConnectingToRoom stateConnectingToRoom)
            {
                if (stateConnectingToRoom.CurentRoom.ConnectWithPassword(CurentUser, message.Text))
                {
                    await bot.DeleteMessages(CurentUser.Id, [stateConnectingToRoom.LastMessageId, message.Id]);
                    CurentUser.State = new UserStateInRoom(stateConnectingToRoom.CurentRoom);
                    LockedRooms.Remove(stateConnectingToRoom.CurentRoom.Id);
                    await stateConnectingToRoom.CurentRoom.SendUpdateRoomMessage();
                }
                else
                {
                    await bot.SendMessage(CurentUser.Id, "Это не правельный пароль");
                    await bot.DeleteMessages(CurentUser.Id, [stateConnectingToRoom.LastMessageId, message.Id]);
                    var mess = await SendHomeMessage(CurentUser, "давай играть в морской бой");
                    CurentUser.State = new UserStateInMainMenu(mess.Id);
                }
            }
            else if (CurentUser.State is UserStateInRoom stateInRoom)
            {
                stateInRoom.CurentRoom.RoomOnMessage(CurentUser, message);
            }
        }
        public static async Task<Message> SendHomeMessage(UserSession CurentUser, string text)
        {
            var markup = new InlineKeyboardButton[][]
                    {
                        [InlineKeyboardButton.WithCallbackData("Создать игру", "Создать игру"),
                        InlineKeyboardButton.WithCallbackData("Найти игру", "Найти игру")],
                    };
            return await bot.SendMessage(CurentUser.Id, text, replyMarkup: markup);
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

        public string Name { get { return $"Комнота {Owner.UserName} № {Id}"; } }
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
                    OwnerRoomMessageId = (await Program.bot.SendMessage(Owner.Id, text)).Id;
                else await Program.bot.EditMessageText(Owner.Id, (int)OwnerRoomMessageId, text);
            }

            else
            {
                if (OwnerRoomMessageId is null)
                    OwnerRoomMessageId = (await Program.bot.SendMessage(Owner.Id, text)).Id;
                else await Program.bot.EditMessageText(Owner.Id, (int)OwnerRoomMessageId, text, replyMarkup: InlineKeyboardButton.WithCallbackData("Начать", "Начать"));

                if (SecondUserRoomMessageId is null)
                    SecondUserRoomMessageId = (await Program.bot.SendMessage(SecondUser.Id, text)).Id;
                else await Program.bot.EditMessageText(SecondUser.Id, (int)SecondUserRoomMessageId, text);
            }
        }
        public string CriateRoomMessageText()
        {
            string text = $"Название комнаты: {Name} \nИгроки: \n{Owner.UserName}\nvs\n{((SecondUser is not null) ? SecondUser.UserName : "Ожидание игрока")}";
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
            await Program.bot.DeleteMessages(CurentUser.Id, [message.Id]);
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
                UpdateFieldMassage(Program.bot, CurentUser, field, (int)(CurentUser == Owner ? OwnerRoomMessageId : SecondUserRoomMessageId), text);
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
                                    await Program.bot.DeleteMessages(Owner.Id, [(int)OwnerRoomMessageId]);
                                    await Program.bot.DeleteMessages(SecondUser.Id, [(int)SecondUserRoomMessageId]);
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

            await UpdateFieldMassage(Program.bot, Owner, fieldOwner, (int)OwnerRoomMessageId, text);

            await UpdateFieldMassage(Program.bot, SecondUser, fieldSecondUser, (int)SecondUserRoomMessageId, text);
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

                await botClient.EditMessageMedia(user.Id, messageToUpdate, inputMedia);
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

                await botClient.EditMessageMedia(user.Id, messageToUpdate, inputMedia);
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
            await Update2FieldMassage(Program.bot, Owner, fieldOwner, fieldSecondUser, (int)OwnerRoomMessageId, Owner == Move ? "Ваш ход, введите координаты" + text : "Ход противника");

            await Update2FieldMassage(Program.bot, SecondUser, fieldSecondUser, fieldOwner, (int)SecondUserRoomMessageId, SecondUser == Move ? "Ваш ход, введите координаты" + text : "Ход противника");
        }
    }
}
