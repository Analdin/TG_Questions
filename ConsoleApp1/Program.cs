using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static ConsoleApp1.Program;

namespace ConsoleApp1
{
    internal class Program
    {
        private static readonly string BotToken = "6457237388:AAF9o4OV-KksF8rd7xBaBrAJz8d6EE6ED8c";

        static void Main(string[] args)
        {
            var bot = new Bot(BotToken);
            var botClient = new TelegramBotClient(BotToken);

            Console.WriteLine("Бот запущен. Нажмите Enter, чтобы завершить...");
            Console.ReadLine();
        }
        private static async Task BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var bot = new Bot(BotToken); // Создаем экземпляр бота
            await bot.HandleUpdateAsync(messageEventArgs.Message); // Обрабатываем полученное сообщение
        }

        public class User
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public class Question
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public List<string> Options { get; set; }
            public int CorrectOptionIndex { get; set; }
        }

        public class UserAnswer
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public int QuestionId { get; set; }
            public int SelectedOptionIndex { get; set; }
            public bool IsCorrect { get; internal set; }
            // Другие поля, например, результат (правильно/неправильно)
        }

        public class Bot
        {
            private readonly ITelegramBotClient _botClient;

            public Bot(string apiKey)
            {
                _botClient = new TelegramBotClient(apiKey);
            }

            public async Task HandleUpdateAsync(Update update)
            {
                if (update.Type == UpdateType.Message)
                {
                    var message = update.Message;

                    if (message.Text == "/start")
                    {
                        await _botClient.SendTextMessageAsync(message.Chat.Id, "Введите логин:");

                        // Добавьте обработчик для следующего сообщения, ожидая логин
                        _botClient.OnMessage += async (sender, args) =>
                        {
                            var login = args.Message.Text;

                            // Проверяем наличие пользователя с таким логином в базе данных
                            using (var context = new ExamContext())
                            {
                                var user = await context.Users.FirstOrDefaultAsync(u => u.Username == login);
                                if (user == null)
                                {
                                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Пользователь не найден.");
                                    return;
                                }

                                await _botClient.SendTextMessageAsync(message.Chat.Id, "Введите пароль:");

                                // Добавляем обработчик для следующего сообщения, ожидая пароль
                                _botClient.OnMessage += async (sender2, args2) =>
                                {
                                    var password = args2.Message.Text;

                                    // Проверяем правильность пароля
                                    if (user.Password == password)
                                    {
                                        await _botClient.SendTextMessageAsync(message.Chat.Id, "Доступ предоставлен.");
                                        // Теперь пользователь имеет доступ к остальным командам
                                    }
                                    else
                                    {
                                        await _botClient.SendTextMessageAsync(message.Chat.Id, "Неверный пароль.");
                                    }

                                    // Удаляем обработчик после завершения
                                    _botClient.OnMessage -= sender2;
                                };
                            }

                            // Удаляем обработчик после завершения
                            _botClient.OnMessage -= sender;
                        };
                    }
                    else if (message.Text == "/questions")
                    {
                        // Получение списка вопросов из базы данных
                        using (var context = new ExamContext())
                        {
                            var questions = context.Questions.ToList();

                            foreach (var question in questions)
                            {
                                var options = string.Join("\n", question.Options);
                                var questionText = $"{question.Text}\n{options}";

                                await _botClient.SendTextMessageAsync(message.Chat.Id, questionText);
                            }
                        }
                    }
                    else if (message.Text.StartsWith("/answer"))
                    {
                        var answerParts = message.Text.Split(' '); // Разделяем команду и ответ
                        if (answerParts.Length != 3)
                        {
                            await _botClient.SendTextMessageAsync(message.Chat.Id, "Некорректный формат команды.");
                            return;
                        }

                        if (!int.TryParse(answerParts[1], out var questionId) || !int.TryParse(answerParts[2], out var selectedOptionIndex))
                        {
                            await _botClient.SendTextMessageAsync(message.Chat.Id, "Некорректные параметры.");
                            return;
                        }

                        using (var context = new ExamContext())
                        {
                            var question = await context.Questions.FirstOrDefault(q => q.id == questionId);
                            if (question == null)
                            {
                                await _botClient.SendTextMessageAsync(message.Chat.Id, "Вопрос не найден.");
                                return;
                            }

                            if (selectedOptionIndex < 0 || selectedOptionIndex >= question.Options.Count)
                            {
                                await _botClient.SendTextMessageAsync(message.Chat.Id, "Некорректный индекс ответа.");
                                return;
                            }

                            // Проверяем правильность ответа
                            var isCorrect = question.CorrectOptionIndex == selectedOptionIndex;

                            // Сохраняем результат ответа в базе данных
                            var userId = GetUserFromDatabase(message.From.Username); // Здесь получите ID пользователя из базы данных
                            var userAnswer = new UserAnswer
                            {
                                UserId = userId,
                                QuestionId = questionId,
                                SelectedOptionIndex = selectedOptionIndex,
                                IsCorrect = isCorrect
                            };
                            context.UserAnswers.Add(userAnswer);
                            await context.SaveChangesAsync();

                            var resultMessage = isCorrect ? "Правильно!" : "Неправильно.";
                            await _botClient.SendTextMessageAsync(message.Chat.Id, resultMessage);
                        }
                    }
                    else
                    {
                        var errorMessage = "Неизвестная команда. Доступные команды:\n" +
                                           "/start - Начать работу с ботом\n" +
                                           "/questions - Получить список вопросов\n" +
                                           "/answer [id] [вариант] - Ответить на вопрос";

                        await _botClient.SendTextMessageAsync(message.Chat.Id, errorMessage);
                    }
                }
            }

            private int GetUserFromDatabase(string username)
            {
                throw new NotImplementedException();
            }
        }

    }

    internal class ExamContext : IDisposable
    {
        public string Questions { get; internal set; }
        public string UserAnswers { get; internal set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
