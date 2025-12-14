using UdpConnection.Logging;
using UdpConnection.Messages;
using UdpConnection.Protocol;
using UdpConnection.Serialization;

namespace UdpConnection;

/// <summary>
/// Controller側のUDP接続クラス
/// SampleDownメッセージを送信し、SampleUpメッセージを受信する
/// </summary>
public class UdpConnectionController : UdpConnectionBase
{
    public UdpConnectionController(ILogger? logger = null)
        : base(logger)
    {
    }

    /// <summary>
    /// SampleUpメッセージ受信時に発火するイベント
    /// </summary>
    public event EventHandler<SampleUpMessage>? SampleUpReceived;

    /// <summary>
    /// SampleDownメッセージを送信する
    /// </summary>
    /// <param name="message">送信するメッセージ</param>
    /// <returns>送信キューへの追加に成功した場合はtrue</returns>
    public bool SendSampleDownMessage(SampleDownMessage message)
    {
        return SendMessage(MessageType.SampleDown, message);
    }

    protected override void ProcessReceivedData(byte[] data)
    {
        if (data.Length < ProtocolConstants.HeaderSize)
        {
            return; // ヘッダーが不完全
        }

        var header = MessageHeader.ReadFrom(data);

        if (data.Length < ProtocolConstants.HeaderSize + header.PayloadLength)
        {
            return; // ペイロードが不完全
        }

        var payloadReader = new BitReader(data, ProtocolConstants.HeaderSize, header.PayloadLength);

        switch (header.Type)
        {
            case MessageType.SampleUp:
                var sampleUpMessage = SampleUpMessage.ReadFrom(payloadReader);
                LogReceive(header.Type, sampleUpMessage, data);
                SampleUpReceived?.Invoke(this, sampleUpMessage);
                break;

            default:
                // 未知のメッセージタイプは無視
                break;
        }
    }
}
