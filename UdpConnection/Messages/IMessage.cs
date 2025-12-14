using UdpConnection.Serialization;

namespace UdpConnection.Messages;

/// <summary>
/// メッセージの基底インターフェース
/// </summary>
public interface IMessage
{
    /// <summary>
    /// ペイロードサイズ（バイト）
    /// </summary>
    int PayloadSize { get; }

    /// <summary>
    /// メッセージをBitWriterに書き込む
    /// </summary>
    void WriteTo(BitWriter writer);

    /// <summary>
    /// ログ出力用の文字列表現を取得する
    /// </summary>
    string ToLogString();
}
