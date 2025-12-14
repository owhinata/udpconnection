namespace UdpConnection;

/// <summary>
/// ネゴシエーション状態
/// </summary>
public enum NegotiationState
{
    /// <summary>
    /// 接続成功（SessionId取得）
    /// </summary>
    Connected,

    /// <summary>
    /// 応答タイムアウト（2回目、3回目）
    /// </summary>
    Timeout,

    /// <summary>
    /// 切断（3回連続ミス）
    /// </summary>
    Disconnected
}

/// <summary>
/// ネゴシエーション状態変更イベント引数
/// </summary>
public class NegotiationStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// 状態
    /// </summary>
    public NegotiationState State { get; }

    /// <summary>
    /// Peer ID
    /// </summary>
    public ushort PeerId { get; }

    /// <summary>
    /// セッションID（Disconnected時は直前の値）
    /// </summary>
    public ushort SessionId { get; }

    /// <summary>
    /// ミスカウント（Timeout時: 2 or 3）
    /// </summary>
    public int MissCount { get; }

    public NegotiationStateChangedEventArgs(NegotiationState state, ushort peerId, ushort sessionId, int missCount = 0)
    {
        State = state;
        PeerId = peerId;
        SessionId = sessionId;
        MissCount = missCount;
    }
}
