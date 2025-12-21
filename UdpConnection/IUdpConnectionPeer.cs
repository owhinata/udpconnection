using UdpConnection.Messages;

namespace UdpConnection;

/// <summary>
/// Peer側UDP接続のインターフェース
/// </summary>
public interface IUdpConnectionPeer : IUdpConnection
{
    /// <summary>
    /// Peer ID（Start時に指定）
    /// </summary>
    ushort PeerId { get; }

    /// <summary>
    /// セッションID（Controller側管理ID、0=未接続）
    /// </summary>
    ushort SessionId { get; }

    /// <summary>
    /// 接続状態（SessionId != 0）
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 未接続時の送信間隔（デフォルト3秒、0=自動送信無効）
    /// </summary>
    TimeSpan DisconnectedInterval { get; set; }

    /// <summary>
    /// 接続時の送信間隔（デフォルト60秒、0=自動送信無効）
    /// </summary>
    TimeSpan ConnectedInterval { get; set; }

    /// <summary>
    /// 手動でNegotiationRequestを送信する
    /// </summary>
    /// <returns>送信キューへの追加に成功した場合はtrue</returns>
    bool SendNegotiation();

    /// <summary>
    /// SampleUpメッセージを送信する
    /// </summary>
    /// <param name="message">送信するメッセージ</param>
    /// <returns>送信キューへの追加に成功した場合はtrue</returns>
    bool SendSampleUpMessage(SampleUpMessage message);

    /// <summary>
    /// SampleDownメッセージ受信時に発火するイベント
    /// </summary>
    event EventHandler<SampleDownMessage>? SampleDownReceived;

    /// <summary>
    /// ネゴシエーション状態変更時に発火するイベント
    /// </summary>
    event EventHandler<NegotiationStateChangedEventArgs>? NegotiationStateChanged;
}
