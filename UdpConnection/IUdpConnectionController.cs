using System.Net;

using UdpConnection.Messages;

namespace UdpConnection;

/// <summary>
/// Controller側UDP接続のインターフェース（プロトコル層のみ）
/// Peer管理はアプリケーション層で実装する
/// </summary>
public interface IUdpConnectionController : IUdpConnection
{
    /// <summary>
    /// SampleDownメッセージを指定エンドポイントへ送信する
    /// </summary>
    /// <param name="message">送信するメッセージ</param>
    /// <param name="destination">送信先エンドポイント</param>
    /// <returns>送信キューへの追加に成功した場合はtrue</returns>
    bool SendSampleDownMessage(SampleDownMessage message, IPEndPoint destination);

    /// <summary>
    /// NegotiationResponseを手動送信する
    /// </summary>
    /// <param name="response">送信するメッセージ</param>
    /// <param name="destination">送信先エンドポイント</param>
    /// <returns>送信キューへの追加に成功した場合はtrue</returns>
    bool SendNegotiationResponse(NegotiationResponseMessage response, IPEndPoint destination);

    /// <summary>
    /// NegotiationRequest受信イベント
    /// アプリ層でSessionIdを決定し、ResponseSessionIdに設定する
    /// </summary>
    event EventHandler<NegotiationRequestReceivedEventArgs>? NegotiationRequestReceived;

    /// <summary>
    /// SampleUpメッセージ受信イベント
    /// </summary>
    event EventHandler<SampleUpReceivedEventArgs>? SampleUpReceived;
}
