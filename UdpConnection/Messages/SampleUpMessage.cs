using UdpConnection.Serialization;

namespace UdpConnection.Messages;

/// <summary>
/// 上りサンプルメッセージ（送信用）
///
///  0                   1                   2                   3
///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |Cmd|S|   Value (8bit)  | Reserved|       Sequence (16bit)      |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |                    Position (32bit固定小数点)                  |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
///
/// Total: 64 bits = 8 bytes
/// </summary>
public class SampleUpMessage : IMessage
{
    /// <summary>
    /// ペイロードサイズ（バイト）
    /// </summary>
    public const int PayloadSizeConst = 8;

    /// <inheritdoc />
    public int PayloadSize => PayloadSizeConst;

    /// <summary>
    /// コマンド種別（3bit）
    /// </summary>
    public CommandType Command { get; set; }

    /// <summary>
    /// 符号付き値（8bit unsigned + 1bit sign = 9bit）
    /// 範囲: -255 ～ +255
    /// </summary>
    public int SignedValue { get; set; }

    /// <summary>
    /// シーケンス番号（16bit、エンディアン確認用）
    /// </summary>
    public ushort Sequence { get; set; }

    /// <summary>
    /// 位置（16.16固定小数点）
    /// </summary>
    public double Position { get; set; }

    public void WriteTo(BitWriter writer)
    {
        // Command: 3bit
        writer.WriteBits((uint)Command, 3);

        // Sign: 1bit (0=正, 1=負)
        bool isNegative = SignedValue < 0;
        writer.WriteBool(isNegative);

        // Value: 8bit (絶対値)
        int absValue = Math.Abs(SignedValue);
        if (absValue > 255) absValue = 255;
        writer.WriteBits((uint)absValue, 8);

        // Reserved: 4bit
        writer.WriteBits(0, 4);

        // Sequence: 16bit
        writer.WriteUInt16(Sequence);

        // Position: 32bit固定小数点
        writer.WriteFixed16_16(Position);
    }

    public static SampleUpMessage ReadFrom(BitReader reader)
    {
        var message = new SampleUpMessage();

        // Command: 3bit
        message.Command = (CommandType)reader.ReadBits(3);

        // Sign: 1bit
        bool isNegative = reader.ReadBool();

        // Value: 8bit
        int absValue = (int)reader.ReadBits(8);

        message.SignedValue = isNegative ? -absValue : absValue;

        // Reserved: 4bit
        reader.Skip(4);

        // Sequence: 16bit
        message.Sequence = reader.ReadUInt16();

        // Position: 32bit固定小数点
        message.Position = reader.ReadFixed16_16();

        return message;
    }

    public static SampleUpMessage ReadFrom(byte[] buffer)
    {
        var reader = new BitReader(buffer);
        return ReadFrom(reader);
    }

    public string ToLogString()
    {
        return $"SampleUp (Command={Command}, SignedValue={SignedValue}, Sequence=0x{Sequence:X4}, Position={Position:F3})";
    }
}
