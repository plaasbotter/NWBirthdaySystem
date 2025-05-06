using NWBirthdaySystem.Library;
using NWBirthdaySystem.Models;
using NWBirthdaySystem.Utils;
using Serilog;
using System.Text;

namespace NWBirthdaySystem
{
    internal class Program
    {
        private static ILogger _logger;
        private static IDatabaseContext _databaseContext;
        private static HttpClient _httpClient;
        private static IMessageContext _messageContext;

        static Program()
        {
            Config.LoadConfig();
            _logger = Factory.GetLogger();
            _databaseContext = Factory.GetDatabaseContext(_logger, Config.GetConnectionString());
            _databaseContext.CheckAndCreateTables("Birthdays", new Birthday(), false);
            _databaseContext.CheckAndCreateTables("BirthdayEntries", new BirthdayEntry(), false);
            _httpClient = new HttpClient();
            _messageContext = Factory.GetMessageContext(_logger, _httpClient);
        }

        static void Main(string[] args)
        {
            Run();
        }


        private static void Run()
        {
            DateTime past = DateTime.Now;
            DateTime future = DateTime.MinValue;
            while (true)
            {
                past = DateTime.Now;
                if (past > future)
                {
                    SendOutBirthdayNotifications();
                    SendOutWeeklyBirthdays();
                    future = DateTime.Today.AddDays(1).AddHours(8);
                }
                CheckMessages();
                _logger.Information("[{0}] [{1}]ms [{2}]", "Program.Run", "Execution complete, Waiting", Config.configVaraibles.sleepTimer);
                Thread.Sleep(Config.configVaraibles.sleepTimer);
            }
        }

        private static void SendOutBirthdayNotifications()
        {
            List<Birthday> birthdays = _databaseContext.GetBirthdays(DateTime.Today);
            foreach(var birthday in birthdays)
            {
                _messageContext.SendMessage($"{birthday.Name}'s birthday is today", birthday.ChatId);
            }
        }

        private static void SendOutWeeklyBirthdays()
        {
            DateTime today = DateTime.Today;
            if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                DateTime date = today;
                Dictionary<string, List<BirthdayShort>> birthdayMap = new Dictionary<string, List<BirthdayShort>>();
                for (int i = 0; i < 7; i++)
                {
                    date = date.AddDays(1);
                    List<Birthday> birthdays = _databaseContext.GetBirthdays(date);
                    foreach (var birthday in birthdays)
                    {
                        BirthdayShort bshort = new BirthdayShort 
                        {
                            Name = birthday.Name,
                            Date = date,
                        };
                        if (!birthdayMap.ContainsKey(birthday.ChatId))
                        {
                            birthdayMap[birthday.ChatId] = new List<BirthdayShort>();
                        }
                        birthdayMap[birthday.ChatId].Add(bshort);
                    }

                }
                foreach (var kvp in birthdayMap)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Upcoming birthdays:");
                    foreach (var bshort in kvp.Value)
                    {
                        sb.AppendLine(bshort.Date.DayOfWeek.ToString() + ": " + bshort.Name);
                    }
                    sb.Append(today.ToShortDateString() + "-->" + date.ToShortDateString());
                    _messageContext.SendMessage(sb.ToString(), kvp.Key);
                }
            }
        }

        private static void CheckMessages()
        {
            TelegramMessage messages = (TelegramMessage)_messageContext.ReadMessages();
            int senderId = 0;
            long lastMessageId = _databaseContext.GetLastMessage();
            if (messages.result != null && messages.result.Length > 0)
            {
                foreach (Result message in messages.result)
                {
                    if (message.message.message_id > lastMessageId)
                    {
                        try
                        {
                            senderId = message.message.from.id;
                            BirthdayEntry tempBirthdayEntry = new BirthdayEntry
                            {
                                Id = message.message.message_id,
                                ChatId = message.message.from.id,
                                Message = message.message.text,
                            };
                            _databaseContext.UpsertMessage(tempBirthdayEntry);
                            var lines = message.message.text.Replace("\r\n","\n").Split("\n");
                            foreach (var line in lines)
                            {
                                try
                                {
                                    string[] splittedString = line.Split(':');
                                    short part_a = (short)(short.Parse(splittedString[0]) * 32);
                                    short part_b = short.Parse(splittedString[1]);
                                    if (part_a < 0 || part_a > 400)
                                    {
                                        throw new Exception("Could not parse part a of date");
                                    }
                                    if (part_b < 0 || part_b > 32)
                                    {
                                        throw new Exception("Could not parse part a of date");
                                    }
                                    Birthday tempBirthday = new Birthday
                                    {
                                        BirthDate = (short)(part_a + part_b),
                                        ChatId = message.message.from.id.ToString(),
                                        Name = splittedString[2]
                                    };
                                    bool hasSent = _databaseContext.UpsertBirthday(tempBirthday);
                                    if (hasSent == true)
                                    {
                                        _messageContext.SendMessage($"Inserted birthday of {tempBirthday.Name}", senderId.ToString());
                                    }
                                    else
                                    {
                                        _logger.Error("[{0}] [{1}]", "Could not insert person, likely duplicate", "Program.CheckMessages");
                                        _messageContext.SendMessage($"Could not insert person {tempBirthday.Name}, likely duplicate", senderId.ToString());
                                    }
                                }
                                catch (Exception err)
                                {
                                    _logger.Error("[{0}] [{1}]", err.Message, "Program.CheckMessages");
                                    _messageContext.SendMessage("Could not parse message", senderId.ToString());
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            _logger.Error("[{0}] [{1}]", err.Message, "Program.CheckMessages");
                        }
                    }
                }
            }
        }
    }
}