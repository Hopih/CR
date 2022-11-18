// See https://aka.ms/new-console-template for more information

Console.WriteLine("Hello, World!");

var currentDirectory = Directory.GetCurrentDirectory();

await using var fileStream = new FileStream($@"{currentDirectory}\text.txt", FileMode.Open);
using var binaryStream = new BinaryReader(fileStream);
while (binaryStream.BaseStream.CanRead)
{
    var inputChar = binaryStream.ReadChar();
    Console.Write(inputChar);
    switch (inputChar)
    {
        case ',':
            Thread.Sleep(1500);
            break;
        case '.':
            Thread.Sleep(2000);
            Console.Write("\n\r");
            break;
        default:
            Thread.Sleep(new Random().Next(70, 200));
            break;
    }
}
