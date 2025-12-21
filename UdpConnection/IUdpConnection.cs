namespace UdpConnection;

/// <summary>
/// UDP接続の共通インターフェース
/// </summary>
public interface IUdpConnection : IDisposable
{
    /// <summary>
    /// 送受信を開始する
    /// </summary>
    /// <param name="options">接続オプション</param>
    /// <returns>成功した場合はtrue、既に開始済みまたは失敗した場合はfalse</returns>
    bool Start(UdpConnectionOptions options);

    /// <summary>
    /// 送受信を停止する
    /// </summary>
    /// <returns>成功した場合はtrue、既に停止済みの場合はfalse</returns>
    bool Stop();
}
