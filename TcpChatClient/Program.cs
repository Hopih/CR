using System.Net;
using System.Text;

var client = new TcpChatClient.TcpClient();
var ipAddress = new IPAddress(Encoding.UTF8.GetBytes("127.0.0.1"));
await client.ConnectAsync(ipAddress,5065);
