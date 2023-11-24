using System.Text;

namespace TcpChatServer;

public static class Package11207Helper
{
    public const int MaxSizeOfContent = 244;
    public const int MaxPacketSize = 256;
    public const int MaxFreeBytes = MaxPacketSize - MaxSizeOfContent;
    
    
    public const int Command = 8;
    public const int Fullness = 9;
    public const int Query = 10;
    
    
    
    public static readonly byte[] BasePackage = 
    {
        0x02, 0x02, 0x02,
        0x31, 0x31, 0x32,
        0x30, 0x37
    };

    public static readonly byte LastByte = 0x03;



    public static byte[] GetContent(byte[] buffer, int contentLength) =>
        buffer.Skip(MaxFreeBytes - 1).Take(contentLength - MaxFreeBytes).ToArray();

    public static bool IsQueryValid(byte[] buffer, int packageLength) =>
        HasStart(buffer) && HasEnd(buffer[packageLength - 1]) 
                         && IsCorrectProtocol(buffer)
                         && IsFullOrPartial(buffer[Fullness]) 
                         && HasQueryType(buffer[Query]);

    public static bool HasStart(byte[] buffer) => 
        buffer[..3].SequenceEqual(BasePackage[..3]);
    
    public static bool HasEnd(byte lastByte) => 
        lastByte.Equals(LastByte);
    
    public static bool IsCorrectProtocol(byte[] buffer) => 
        buffer[3..8].SequenceEqual(BasePackage[3..8]);
    public static bool IsFullOrPartial(byte fullness) => 
        fullness is (byte)FullnessPackage.Partial 
            or (byte)FullnessPackage.Full;
    public static bool IsFull(byte fullness) =>
        fullness == (byte)FullnessPackage.Full;

    public static bool IsPartial(byte fullness) =>
        fullness == (byte)FullnessPackage.Partial;
    
    public static bool HasQueryType(byte queryType) =>
        queryType is (byte)QueryType.Request 
            or (byte)QueryType.Response;
    public static bool IsRequest(byte queryType) =>
        queryType is (byte)QueryType.Request;
    public static bool IsResponse(byte queryType) =>
        queryType is (byte)QueryType.Response;
    
    public static bool IsHello(byte command, byte fullness, byte queryType) =>
        command == (byte)SupportCommand.Hello
        && fullness == (byte)FullnessPackage.Full
        && queryType == (byte)QueryType.Request;
    public static bool IsSay(byte command) => 
        command == (byte)SupportCommand.Say;
    public static bool IsBye(byte command) => 
        command == (byte)SupportCommand.Bye;

    public static byte[] CreatePackage(byte[] content, SupportCommand supportCommand,
        FullnessPackage fullnessPackage,
        QueryType queryType) => 
        new PackageBuilder(content.Length)
            .WithCommand(supportCommand)
            .WithFullness(fullnessPackage)
            .WithQueryType(queryType)
            .WithContent(content)
            .Build();

    public static List<byte[]> GetPackagesByMessage(byte[] resultContent, SupportCommand supportCommand, QueryType queryType)
    {
        
        var packages = new List<byte[]>();

        if (resultContent.Length > MaxSizeOfContent)
        {
            var chunks = resultContent
                .Chunk(MaxSizeOfContent).ToList();

            var chuncksCount = chunks.Count;

            for (var i = 0; i < chuncksCount; i++)
            {
                var builder = new PackageBuilder(chunks[i].Length)
                    .WithCommand(supportCommand)
                    .WithQueryType(queryType);

                builder = builder.WithFullness(i == chuncksCount - 1
                    ? FullnessPackage.Full
                    : FullnessPackage.Partial);

                packages.Add(builder
                    .WithContent(chunks[i])
                    .Build());
            }
        }
        else
        {
            packages.Add(new PackageBuilder(resultContent.Length)
                .WithCommand(supportCommand)
                .WithFullness(FullnessPackage.Full)
                .WithQueryType(queryType)
                .WithContent(resultContent)
                .Build());
        }
        return packages;
    }
    
}