using System.Net;

namespace UdpConnection;

/// <summary>
/// Peer状態（Controller側）
/// </summary>
public enum PeerState
{
    /// <summary>
    /// 新規Peer接続
    /// </summary>
    Connected,

    /// <summary>
    /// Peerタイムアウト削除
    /// </summary>
    Disconnected
}

/// <summary>
/// Peer状態変更イベント引数
/// </summary>
public class PeerStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// 状態
    /// </summary>
    public PeerState State { get; }

    /// <summary>
    /// Peer ID
    /// </summary>
    public ushort PeerId { get; }

    /// <summary>
    /// セッションID（Controller側管理ID）
    /// </summary>
    public ushort SessionId { get; }

    /// <summary>
    /// リモートエンドポイント
    /// </summary>
    public IPEndPoint RemoteEndPoint { get; }

    public PeerStateChangedEventArgs(PeerState state, ushort peerId, ushort sessionId, IPEndPoint remoteEndPoint)
    {
        State = state;
        PeerId = peerId;
        SessionId = sessionId;
        RemoteEndPoint = remoteEndPoint;
    }
}
