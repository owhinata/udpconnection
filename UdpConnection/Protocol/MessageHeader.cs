using UdpConnection.Serialization;

namespace UdpConnection.Protocol;

/// <summary>
/// メッセージヘッダー（4バイト固定）
///
///  0                   1                   2                   3
///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |  MessageType  |   Reserved    |         PayloadLength         |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// </summary>
public readonly struct MessageHeader
{
    public MessageType Type { get; }
    public ushort PayloadLength { get; }

    public MessageHeader(MessageType type, ushort payloadLength)
    {
        Type = type;
        PayloadLength = payloadLength;
    }

    public void WriteTo(BitWriter writer)
    {
        writer.WriteByte((byte)Type);
        writer.WriteByte(0); // Reserved
        writer.WriteUInt16(PayloadLength);
    }

    public static MessageHeader ReadFrom(BitReader reader)
    {
        var type = (MessageType)reader.ReadByte();
        _ = reader.ReadByte(); // Reserved
        var payloadLength = reader.ReadUInt16();

        return new MessageHeader(type, payloadLength);
    }

    public static MessageHeader ReadFrom(byte[] buffer)
    {
        var reader = new BitReader(buffer);
        return ReadFrom(reader);
    }
}
