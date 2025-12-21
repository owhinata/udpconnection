using System.Net;

using UdpConnection.Logging;
using UdpConnection.Messages;
using UdpConnection.Protocol;
using UdpConnection.Serialization;

namespace UdpConnection;

/// <summary>
/// Controller側のUDP接続クラス（プロトコル層のみ）
/// SampleDownメッセージを送信し、SampleUpメッセージを受信する
/// Peer管理はアプリケーション層で実装する
/// </summary>
public class UdpConnectionController : UdpConnectionBase, IUdpConnectionController
{
    public UdpConnectionController(ILogger? logger = null)
        : base(logger)
    {
    }

    /// <summary>
    /// NegotiationRequest受信イベント
    /// アプリ層でSessionIdを決定し、ResponseSessionIdに設定する
    /// </summary>
    public event EventHandler<NegotiationRequestReceivedEventArgs>? NegotiationRequestReceived;

    /// <summary>
    /// SampleUpメッセージ受信イベント
    /// </summary>
    public event EventHandler<SampleUpReceivedEventArgs>? SampleUpReceived;

    /// <summary>
    /// SampleDownメッセージを指定エンドポイントへ送信する
    /// </summary>
    /// <param name="message">送信するメッセージ</param>
    /// <param name="destination">送信先エンドポイント</param>
    /// <returns>送信キューへの追加に成功した場合はtrue</returns>
    public bool SendSampleDownMessage(SampleDownMessage message, IPEndPoint destination)
    {
        return SendMessageTo(MessageType.SampleDown, message, destination);
    }

    /// <summary>
    /// NegotiationResponseを手動送信する
    /// </summary>
    /// <param name="response">送信するメッセージ</param>
    /// <param name="destination">送信先エンドポイント</param>
    /// <returns>送信キューへの追加に成功した場合はtrue</returns>
    public bool SendNegotiationResponse(NegotiationResponseMessage response, IPEndPoint destination)
    {
        return SendMessageTo(MessageType.NegotiationResponse, response, destination);
    }

    protected override void ProcessReceivedData(byte[] data, IPEndPoint remoteEndPoint)
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
            case MessageType.NegotiationRequest:
                var negotiationRequest = NegotiationRequestMessage.ReadFrom(payloadReader);
                LogReceive(header.Type, negotiationRequest, data);
                ProcessNegotiationRequest(negotiationRequest, remoteEndPoint);
                break;

            case MessageType.SampleUp:
                var sampleUpMessage = SampleUpMessage.ReadFrom(payloadReader);
                LogReceive(header.Type, sampleUpMessage, data);
                SampleUpReceived?.Invoke(this, new SampleUpReceivedEventArgs(sampleUpMessage, remoteEndPoint));
                break;

            default:
                // 未知のメッセージタイプは無視
                break;
        }
    }

    private void ProcessNegotiationRequest(NegotiationRequestMessage request, IPEndPoint remoteEndPoint)
    {
        // イベント引数を作成し、アプリ層にSessionId決定を委譲
        var args = new NegotiationRequestReceivedEventArgs(request, remoteEndPoint);
        NegotiationRequestReceived?.Invoke(this, args);

        // 応答送信（アプリ層がSendResponse=falseにした場合はスキップ）
        if (args.SendResponse)
        {
            var response = new NegotiationResponseMessage
            {
                SessionId = args.ResponseSessionId,
                PeerId = request.PeerId
            };
            SendMessageTo(MessageType.NegotiationResponse, response, remoteEndPoint);
        }
    }
}
