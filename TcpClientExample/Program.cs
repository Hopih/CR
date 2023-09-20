using System.Text;
using System.Text.Json;
using TcpServerExample;

namespace TcpClientExample;

public class Program
{
    static async Task Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.OutputEncoding = Encoding.UTF8;
        var tcpConnector = new TcpConnector();
        Console.WriteLine("Как вас зовут?");
        var autor = Console.ReadLine();
        const string host = "localhost";
        await tcpConnector.Connect(host, 5000);
        Console.WriteLine("Подключение успешно. Можете писать свои сообщения! Если хотите выйти напишите END");
        
        //В фоне ждем результатов
        var receiveTask = Task.Run(async ()=>
        {
            while (true)
            {
                try
                {
                    Console.WriteLine(await tcpConnector.GetResult());
                }
                catch
                {
                    break;
                }
            }
            
        });
        
        while(true)
        {
            var inputMessage = new Message
            {
                Autor = autor,
            };
            var message = Console.ReadLine() ?? string.Empty;
            if (message.Trim() == "END")
            {
                inputMessage.IsClosedMessage = true;
            }
            
            inputMessage.Data = message;
            var messageData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(inputMessage));
            var messageSize = BitConverter.GetBytes(messageData.Length);
            await tcpConnector.SendMessage(new ArraySegment<byte>(messageSize));
            await tcpConnector.SendMessage(new ArraySegment<byte>(messageData));

            if (inputMessage.IsClosedMessage)
            {
                Console.WriteLine("До свидания!");
                break;
            }
        }
        await tcpConnector.Disconnect();
        receiveTask.Dispose();
    }
}