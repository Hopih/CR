namespace TcpChatServer;

public class Package11207
{
    private byte[] _packageData;

    public Package11207(int sizeOfContent, byte command, byte fullness)
    {
        if (sizeOfContent > 246)
        {
            throw new ArgumentException(nameof(sizeOfContent));
        }

        if (!Enum.IsDefined(typeof(SupportCommand),command))
        {
            throw new ArgumentException(nameof(command));
        }

        if (!Enum.IsDefined(typeof(FullnessPackage), fullness))
        {
            throw new ArgumentException(nameof(fullness));
        }

        _packageData = new byte[10 + sizeOfContent];
        var basePackage = new byte[] { 0x02, 0x02, 0x02, 0x31, 0x31, 0x32, 0x30, 0x37, command, fullness};
        for (var i = 0; i < basePackage.Length; i++)
        {
            _packageData[i] = basePackage[i];
        }

        _packageData[^1] = 0x03;
    }

    public void WriteContent(byte[] content)
    {
        if (content.Length > _packageData.Length - 10)
        {
            throw new ArgumentException(nameof(content));
        }
        for (int i = 0; i < content.Length; i++)
        {
            _packageData[i+10] = content[i];
        }
    }

    public byte[] BuildContent()
    {
        return _packageData;
    }
}