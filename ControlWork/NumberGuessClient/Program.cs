using System.Net.Sockets;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("Угадай число от 1 до 100");
Console.WriteLine("Команды: start — новая игра, число — попытка, quit — выход.");

try
{
    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    await socket.ConnectAsync("127.0.0.1", 5050);
    var buffer = new byte[512];
    var pendingBytes = new List<byte>();

    while (true)
    {
        Console.Write("> ");
        var input = Console.ReadLine();
        if (input is null) break;
        input = input.Trim();
        if (input.Length == 0) continue;

        // Число превращаем в GUESS; остальные команды можно вводить как в протоколе.
        var command = int.TryParse(input, out _) ? $"GUESS {input}" : input;
        var commandBytes = Encoding.UTF8.GetBytes(command + "\n");
        if (commandBytes.Length - 1 > 1024)
        {
            Console.WriteLine("Команда слишком длинная: максимум 1024 байта UTF-8.");
            continue;
        }

        // Отправка может быть частичной, поэтому продолжаем с нужного смещения.
        var sent = 0;
        while (sent < commandBytes.Length)
        {
            var count = await socket.SendAsync(commandBytes.AsMemory(sent), SocketFlags.None);
            if (count == 0) throw new SocketException((int)SocketError.ConnectionReset);
            sent += count;
        }

        // Накапливаем байты до '\n', сохраняя остаток для следующего ответа.
        var lineEnd = pendingBytes.IndexOf((byte)'\n');
        while (lineEnd < 0)
        {
            if (pendingBytes.Count > 1024)
                throw new InvalidDataException("Слишком длинный ответ сервера.");
            var received = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None);
            if (received == 0) break;
            for (var i = 0; i < received; i++) pendingBytes.Add(buffer[i]);
            lineEnd = pendingBytes.IndexOf((byte)'\n');
        }

        if (lineEnd < 0)
        {
            Console.WriteLine("Сервер закрыл соединение.");
            break;
        }
        if (lineEnd > 1024) throw new InvalidDataException("Слишком длинный ответ сервера.");
        var response = Encoding.UTF8.GetString(pendingBytes.GetRange(0, lineEnd).ToArray()).TrimEnd('\r');

        switch (response)
        {
            case "STATUS NOT_STARTED":
                Console.WriteLine("Игра не начата");
                break;
            case "STATUS WON":
                Console.WriteLine("Последняя игра выйграна");
                break;
            case "ERROR INVALID_ARGUMENTS":
            {
                Console.WriteLine("Неправильные аргументы");
                break;
            }
            case string s when s.StartsWith("STATUS PLAYING "):
            {
                var range = s.Substring("STATUS PLAYING ".Length).Split('-');
                Console.WriteLine($"{range[0]}-{range[1]}");
                break;
            }

            case string s when s.StartsWith("WIN "):
            {
                var num = s.Substring("WIN ".Length).Trim();
                Console.WriteLine("Вы выйграли число попыток" + num);
                break;
            }
            case string s when s.StartsWith("RECORD"):
            {
                var num = s.Substring("RECORD ".Length).Trim();
                if (num == "NONE")
                {
                    Console.WriteLine("Еще никто не выйгрывал");
                    break;
                }
                Console.WriteLine("Лучший результат " + num);
                break;
            }
        }
        
        pendingBytes.RemoveRange(0, lineEnd + 1);

        Console.WriteLine(response switch
        {
            "OK" => "Число загадано. Введите попытку от 1 до 100.",
            "GREATER" => "Загаданное число больше.",
            "LESS" => "Загаданное число меньше.",
            "BYE" => "До встречи!",
            "ERROR NOT_STARTED" => "Сначала начните игру командой start.",
            "ERROR INVALID_NUMBER" => "Нужно целое число от 1 до 100.",
            "ERROR INVALID_ARGUMENTS" => "Неверные аргументы команды.",
            "ERROR UNKNOWN_COMMAND" => "Неизвестная команда. Используйте start, число или quit.",
            _ => $"Ответ сервера: {response}"
        });
        if (response == "BYE") break;
    }
}
catch (Exception ex) when (ex is SocketException or IOException)
{
    Console.Error.WriteLine($"Не удалось подключиться или соединение прервано: {ex.Message}");
    Console.Error.WriteLine("Убедитесь, что NumberGuessServer запущен.");
    Environment.ExitCode = 1;
}
