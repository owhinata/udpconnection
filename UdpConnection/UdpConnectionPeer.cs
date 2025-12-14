using UdpConnection.Logging;
using UdpConnection.Messages;
using UdpConnection.Protocol;
using UdpConnection.Serialization;

namespace UdpConnection;

/// <summary>
/// Peer側のUDP接続クラス
/// SampleUpメッセージを送信し、SampleDownメッセージを受信する
/// </summary>
public class UdpConnectionPeer : UdpConnectionBase
{
    public UdpConnectionPeer(ILogger? logger = null)
        : base(logger)
    {
    }

    /// <summary>
    /// SampleDownメッセージ受信時に発火するイベント
    /// </summary>
    public event EventHandler<SampleDownMessage>? SampleDownReceived;

    /// <summary>
    /// SampleUpメッセージを送信する
    /// </summary>
    /// <param name="message">送信するメッセージ</param>
    /// <returns>送信キューへの追加に成功した場合はtrue</returns>
    public bool SendSampleUpMessage(SampleUpMessage message)
    {
        return SendMessage(MessageType.SampleUp, message);
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
            case MessageType.SampleDown:
                var sampleDownMessage = SampleDownMessage.ReadFrom(payloadReader);
                LogReceive(header.Type, sampleDownMessage, data);
                SampleDownReceived?.Invoke(this, sampleDownMessage);
                break;

            default:
                // 未知のメッセージタイプは無視
                break;
        }
    }
}
