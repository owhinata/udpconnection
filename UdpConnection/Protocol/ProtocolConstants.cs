namespace UdpConnection.Protocol;

public static class ProtocolConstants
{
    /// <summary>
    /// ヘッダーサイズ（バイト）
    /// </summary>
    public const int HeaderSize = 4;

    /// <summary>
    /// 最大ペイロードサイズ（バイト）
    /// UDPの最大値: 65535 - 20(IP) - 8(UDP) = 65507
    /// 注: MTU(1500)を超えるとIPフラグメンテーションが発生する
    /// </summary>
    public const int MaxPayloadSize = 65507;

    /// <summary>
    /// 最大パケットサイズ（ヘッダー + ペイロード）
    /// </summary>
    public const int MaxPacketSize = HeaderSize + MaxPayloadSize;
}
