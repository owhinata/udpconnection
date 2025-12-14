using UdpConnection.Serialization;

namespace UdpConnection.Messages;

/// <summary>
/// ネゴシエーション応答メッセージ (Controller → Peer)
///
///  0                   1                   2                   3
///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |          SessionId (16bit)    |           PeerId (16bit)      |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
///
/// Total: 32 bits = 4 bytes
/// </summary>
public class NegotiationResponseMessage : IMessage
{
    /// <summary>
    /// ペイロードサイズ（バイト）
    /// </summary>
    public const int PayloadSizeConst = 4;

    /// <inheritdoc />
    public int PayloadSize => PayloadSizeConst;

    /// <summary>
    /// セッションID（Controller側管理ID）
    /// </summary>
    public ushort SessionId { get; set; }

    /// <summary>
    /// Peer ID（受信したPeer IDをエコーバック）
    /// </summary>
    public ushort PeerId { get; set; }

    public void WriteTo(BitWriter writer)
    {
        writer.WriteUInt16(SessionId);
        writer.WriteUInt16(PeerId);
    }

    public static NegotiationResponseMessage ReadFrom(BitReader reader)
    {
        var message = new NegotiationResponseMessage
        {
            SessionId = reader.ReadUInt16(),
            PeerId = reader.ReadUInt16()
        };
        return message;
    }

    public static NegotiationResponseMessage ReadFrom(byte[] buffer)
    {
        var reader = new BitReader(buffer);
        return ReadFrom(reader);
    }

    public string ToLogString()
    {
        return $"NegotiationResponse (SessionId=0x{SessionId:X4}, PeerId=0x{PeerId:X4})";
    }
}
