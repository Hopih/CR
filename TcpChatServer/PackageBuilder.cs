namespace TcpChatServer;

public class PackageBuilder
{
    private readonly byte[] _package;

    public const int MaxSizeOfContent = 245;

    public PackageBuilder(int sizeOfContent)
    {
        if (sizeOfContent > MaxSizeOfContent)
        {
            throw new ArgumentException(nameof(sizeOfContent));
        }

        _package = new byte[10 + sizeOfContent];
        CreateBasePackage();
    }

    void CreateBasePackage()
    {
        var basePackage = new byte[]
        {
            0x02, 0x02, 0x02,
            0x31, 0x31, 0x32,
            0x30, 0x37
        };

        Array.Copy(basePackage, _package, basePackage.Length);

        _package[^1] = 0x03;
    }

    public PackageBuilder WithCommand(SupportCommand command)
    {
        _package[8] = (byte)command;

        return this;
    }

    public PackageBuilder WithFullness(FullnessPackage fullness)
    {
        _package[9] = (byte)fullness;

        return this;
    }

    public PackageBuilder WithContent(byte[] content)
    {
        if (content.Length > _package.Length - 10)
        {
            throw new ArgumentException(nameof(content));
        }

        for (var i = 0; i < content.Length; i++)
        {
            _package[i + 10] = content[i];
        }

        return this;
    }

    public byte[] Build()
    {
        return _package;
    }
}

