using System.Net;

using UdpConnection.Messages;

namespace UdpConnection;

/// <summary>
/// NegotiationRequest受信時のイベント引数
/// </summary>
public class NegotiationRequestReceivedEventArgs : EventArgs
{
    /// <summary>
    /// リクエスト内容
    /// </summary>
    public NegotiationRequestMessage Request { get; }

    /// <summary>
    /// 送信元エンドポイント
    /// </summary>
    public IPEndPoint RemoteEndPoint { get; }

    /// <summary>
    /// 応答として返すSessionId（アプリ層で設定）
    /// </summary>
    public ushort ResponseSessionId { get; set; }

    /// <summary>
    /// 応答を送信するかどうか（デフォルトtrue）
    /// </summary>
    public bool SendResponse { get; set; } = true;

    public NegotiationRequestReceivedEventArgs(
        NegotiationRequestMessage request,
        IPEndPoint remoteEndPoint)
    {
        Request = request;
        RemoteEndPoint = remoteEndPoint;
        ResponseSessionId = 0;
    }
}
