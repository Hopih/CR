var currentDirectory = Directory.GetCurrentDirectory();

await using var fileStream = new FileStream($@"{currentDirectory}\text.txt", FileMode.Open);
using var binaryStream = new BinaryReader(fileStream);
char lastChar = default;
while (binaryStream.BaseStream.CanRead)
{
    var inputChar = binaryStream.ReadChar();
    if (inputChar == ' ' && lastChar == '.')
    { 
        lastChar = inputChar;
        continue; 
    }
    Console.Write(inputChar);
    
    switch (inputChar)
    {
        case ',':
            Thread.Sleep(1500);
            break;
        case '.':
            Thread.Sleep(2000);
            Console.Write("\n");
            break;
        default:
            Thread.Sleep(new Random().Next(70, 200));
            break;
    }
    lastChar = inputChar;
}
