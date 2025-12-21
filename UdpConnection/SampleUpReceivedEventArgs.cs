using System.Net;

using UdpConnection.Messages;

namespace UdpConnection;

/// <summary>
/// SampleUp受信時のイベント引数（送信元情報付き）
/// </summary>
public class SampleUpReceivedEventArgs : EventArgs
{
    /// <summary>
    /// 受信したメッセージ
    /// </summary>
    public SampleUpMessage Message { get; }

    /// <summary>
    /// 送信元エンドポイント
    /// </summary>
    public IPEndPoint RemoteEndPoint { get; }

    public SampleUpReceivedEventArgs(SampleUpMessage message, IPEndPoint remoteEndPoint)
    {
        Message = message;
        RemoteEndPoint = remoteEndPoint;
    }
}
