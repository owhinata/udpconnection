using UdpConnection.Serialization;

namespace UdpConnection.Messages;

/// <summary>
/// 下りサンプルメッセージ（受信用）
///
///  0                   1                   2                   3
///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |Sts|S|   Value (8bit)  | Reserved|        Timestamp (16bit)    |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |                    Velocity (32bit固定小数点)                  |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
///
/// Total: 64 bits = 8 bytes
/// </summary>
public class SampleDownMessage : IMessage
{
    /// <summary>
    /// ペイロードサイズ（バイト）
    /// </summary>
    public const int PayloadSizeConst = 8;

    /// <inheritdoc />
    public int PayloadSize => PayloadSizeConst;

    /// <summary>
    /// ステータス種別（3bit）
    /// </summary>
    public StatusType Status { get; set; }

    /// <summary>
    /// 符号付き値（8bit unsigned + 1bit sign = 9bit）
    /// 範囲: -255 ～ +255
    /// </summary>
    public int SignedValue { get; set; }

    /// <summary>
    /// タイムスタンプ（16bit）
    /// </summary>
    public ushort Timestamp { get; set; }

    /// <summary>
    /// 速度（16.16固定小数点）
    /// </summary>
    public double Velocity { get; set; }

    public void WriteTo(BitWriter writer)
    {
        // Status: 3bit
        writer.WriteBits((uint)Status, 3);

        // Sign: 1bit (0=正, 1=負)
        bool isNegative = SignedValue < 0;
        writer.WriteBool(isNegative);

        // Value: 8bit (絶対値)
        int absValue = Math.Abs(SignedValue);
        if (absValue > 255) absValue = 255;
        writer.WriteBits((uint)absValue, 8);

        // Reserved: 4bit
        writer.WriteBits(0, 4);

        // Timestamp: 16bit
        writer.WriteUInt16(Timestamp);

        // Velocity: 32bit固定小数点
        writer.WriteFixed16_16(Velocity);
    }

    public static SampleDownMessage ReadFrom(BitReader reader)
    {
        var message = new SampleDownMessage();

        // Status: 3bit
        message.Status = (StatusType)reader.ReadBits(3);

        // Sign: 1bit
        bool isNegative = reader.ReadBool();

        // Value: 8bit
        int absValue = (int)reader.ReadBits(8);

        message.SignedValue = isNegative ? -absValue : absValue;

        // Reserved: 4bit
        reader.Skip(4);

        // Timestamp: 16bit
        message.Timestamp = reader.ReadUInt16();

        // Velocity: 32bit固定小数点
        message.Velocity = reader.ReadFixed16_16();

        return message;
    }

    public static SampleDownMessage ReadFrom(byte[] buffer)
    {
        var reader = new BitReader(buffer);
        return ReadFrom(reader);
    }

    public string ToLogString()
    {
        return $"SampleDown (Status={Status}, SignedValue={SignedValue}, Timestamp=0x{Timestamp:X4}, Velocity={Velocity:F3})";
    }
}
