using System.Net;

namespace UdpConnection;

/// <summary>
/// UdpConnectionの設定オプション
/// </summary>
public class UdpConnectionOptions
{
    /// <summary>
    /// ローカルエンドポイント（バインド先）
    /// </summary>
    public IPEndPoint LocalEndPoint { get; }

    /// <summary>
    /// リモートエンドポイント（送信先）
    /// </summary>
    public IPEndPoint RemoteEndPoint { get; }

    /// <summary>
    /// 送信キューの容量
    /// </summary>
    public int SendQueueCapacity { get; init; } = 100;

    public UdpConnectionOptions(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
    {
        LocalEndPoint = localEndPoint ?? throw new ArgumentNullException(nameof(localEndPoint));
        RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
    }
}
