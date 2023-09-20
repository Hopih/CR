using System.Net;
using System.Text;
using TcpServerExample;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.UTF8;
using var server = new TcpServer(new IPEndPoint(IPAddress.Any, 5000));

server.Start();

await server.ProcessListen();

await server.Stop();