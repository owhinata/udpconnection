using System.Net;

namespace UdpConnection;

/// <summary>
/// Peer情報（Controller側で管理）
/// </summary>
public class PeerInfo
{
    /// <summary>
    /// Peer ID
    /// </summary>
    public ushort PeerId { get; }

    /// <summary>
    /// リモートエンドポイント
    /// </summary>
    public IPEndPoint RemoteEndPoint { get; private set; }

    /// <summary>
    /// 最後のネゴシエーション受信時刻
    /// </summary>
    public DateTime LastNegotiationTime { get; internal set; }

    public PeerInfo(ushort peerId, IPEndPoint remoteEndPoint)
    {
        PeerId = peerId;
        RemoteEndPoint = remoteEndPoint;
        LastNegotiationTime = DateTime.UtcNow;
    }

    /// <summary>
    /// リモートエンドポイントを更新する（アドレス変更対応）
    /// </summary>
    internal void UpdateRemoteEndPoint(IPEndPoint remoteEndPoint)
    {
        RemoteEndPoint = remoteEndPoint;
    }
}
