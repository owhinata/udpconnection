namespace UdpConnection.Protocol;

/// <summary>
/// メッセージ種別（1バイト）
/// </summary>
public enum MessageType : byte
{
    Unknown = 0x00,
    NegotiationRequest = 0x01,
    NegotiationResponse = 0x02,
    SampleUp = 0x03,
    SampleDown = 0x04,
}
