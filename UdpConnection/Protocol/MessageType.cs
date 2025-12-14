namespace UdpConnection.Protocol;

/// <summary>
/// メッセージ種別（1バイト）
/// </summary>
public enum MessageType : byte
{
    Unknown = 0x00,
    SampleUp = 0x01,
    SampleDown = 0x02,
}
