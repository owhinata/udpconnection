using UdpConnection.Serialization;

namespace UdpConnection.Messages;

/// <summary>
/// ネゴシエーション要求メッセージ (Peer → Controller)
///
///  0                   1                   2                   3
///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |          SessionId (16bit)    |           PeerId (16bit)      |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
///
/// Total: 32 bits = 4 bytes
/// </summary>
public class NegotiationRequestMessage : IMessage
{
    /// <summary>
    /// ペイロードサイズ（バイト）
    /// </summary>
    public const int PayloadSizeConst = 4;

    /// <inheritdoc />
    public int PayloadSize => PayloadSizeConst;

    /// <summary>
    /// セッションID（Controller側管理ID、0=未接続）
    /// </summary>
    public ushort SessionId { get; set; }

    /// <summary>
    /// Peer ID
    /// </summary>
    public ushort PeerId { get; set; }

    public void WriteTo(BitWriter writer)
    {
        writer.WriteUInt16(SessionId);
        writer.WriteUInt16(PeerId);
    }

    public static NegotiationRequestMessage ReadFrom(BitReader reader)
    {
        var message = new NegotiationRequestMessage
        {
            SessionId = reader.ReadUInt16(),
            PeerId = reader.ReadUInt16()
        };
        return message;
    }

    public static NegotiationRequestMessage ReadFrom(byte[] buffer)
    {
        var reader = new BitReader(buffer);
        return ReadFrom(reader);
    }

    public string ToLogString()
    {
        return $"NegotiationRequest (SessionId=0x{SessionId:X4}, PeerId=0x{PeerId:X4})";
    }
}
