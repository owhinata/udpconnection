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
    /// Peer側では必須、Controller側では省略可能（null）
    /// </summary>
    public IPEndPoint? RemoteEndPoint { get; }

    /// <summary>
    /// Peer ID（Peer側で必須、Controller側では無視）
    /// </summary>
    public ushort PeerId { get; }

    /// <summary>
    /// 送信キューの容量
    /// </summary>
    public int SendQueueCapacity { get; init; } = 100;

    /// <summary>
    /// Controller用コンストラクタ（RemoteEndPoint不要）
    /// </summary>
    public UdpConnectionOptions(IPEndPoint localEndPoint)
    {
        LocalEndPoint = localEndPoint ?? throw new ArgumentNullException(nameof(localEndPoint));
        RemoteEndPoint = null;
        PeerId = 0;
    }

    /// <summary>
    /// Controller用コンストラクタ（RemoteEndPoint指定、後方互換用）
    /// </summary>
    public UdpConnectionOptions(IPEndPoint localEndPoint, IPEndPoint? remoteEndPoint)
    {
        LocalEndPoint = localEndPoint ?? throw new ArgumentNullException(nameof(localEndPoint));
        RemoteEndPoint = remoteEndPoint;
        PeerId = 0;
    }

    /// <summary>
    /// Peer用コンストラクタ（PeerId必須、RemoteEndPoint必須）
    /// </summary>
    public UdpConnectionOptions(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, ushort peerId)
    {
        LocalEndPoint = localEndPoint ?? throw new ArgumentNullException(nameof(localEndPoint));
        RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
        PeerId = peerId;
    }
}
