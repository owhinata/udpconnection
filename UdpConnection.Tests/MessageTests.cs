using UdpConnection.Messages;
using UdpConnection.Serialization;
using UdpConnection.Tests.TestRunner;

namespace UdpConnection.Tests;

public class MessageTests
{
    #region SampleUpMessage Tests

    /// <summary>
    /// テスト名: SampleUpMessage_Roundtrip_AllFields
    /// 目的: SampleUpMessageの全フィールドが正しくシリアライズ・デシリアライズされることを確認する
    /// 手順: 全フィールドに値を設定し、書き込み→読み込みを行う
    /// 期待値: 全フィールドが元の値と一致する
    /// </summary>
    [Test]
    public void SampleUpMessage_Roundtrip_AllFields()
    {
        var original = new SampleUpMessage
        {
            Command = CommandType.Start,
            SignedValue = -128,
            Sequence = 0x1234,
            Position = 123.456
        };

        var writer = new BitWriter();
        original.WriteTo(writer);
        var data = writer.ToArray();

        var result = SampleUpMessage.ReadFrom(data);

        Assert.AreEqual(CommandType.Start, result.Command);
        Assert.AreEqual(-128, result.SignedValue);
        Assert.AreEqual((ushort)0x1234, result.Sequence);
        Assert.AreEqualWithTolerance(123.456, result.Position, 0.0001);
    }

    /// <summary>
    /// テスト名: SampleUpMessage_PayloadSize
    /// 目的: SampleUpMessageのペイロードサイズが8バイトであることを確認する
    /// 手順: メッセージを書き込み、バイト配列の長さを確認する
    /// 期待値: 8バイト
    /// </summary>
    [Test]
    public void SampleUpMessage_PayloadSize()
    {
        var message = new SampleUpMessage();
        Assert.AreEqual(8, message.PayloadSize);
        Assert.AreEqual(8, SampleUpMessage.PayloadSizeConst);

        var writer = new BitWriter();
        message.WriteTo(writer);
        var data = writer.ToArray();

        Assert.AreEqual(8, data.Length);
    }

    /// <summary>
    /// テスト名: SampleUpMessage_Command_AllValues
    /// 目的: CommandType(3bit)の全値が正しく処理されることを確認する
    /// 手順: 各CommandType値でラウンドトリップを行う
    /// 期待値: 全ての値が正しく復元される
    /// </summary>
    [Test]
    public void SampleUpMessage_Command_AllValues()
    {
        foreach (CommandType cmd in Enum.GetValues(typeof(CommandType)))
        {
            var original = new SampleUpMessage { Command = cmd };

            var writer = new BitWriter();
            original.WriteTo(writer);
            var data = writer.ToArray();

            var result = SampleUpMessage.ReadFrom(data);

            Assert.AreEqual(cmd, result.Command, $"Failed for {cmd}");
        }
    }

    /// <summary>
    /// テスト名: SampleUpMessage_SignedValue_Positive
    /// 目的: 正の符号付き値(9bit)が正しく処理されることを確認する
    /// 手順: 正の値(255)でラウンドトリップを行う
    /// 期待値: 255が復元される
    /// </summary>
    [Test]
    public void SampleUpMessage_SignedValue_Positive()
    {
        var original = new SampleUpMessage { SignedValue = 255 };

        var writer = new BitWriter();
        original.WriteTo(writer);
        var data = writer.ToArray();

        var result = SampleUpMessage.ReadFrom(data);

        Assert.AreEqual(255, result.SignedValue);
    }

    /// <summary>
    /// テスト名: SampleUpMessage_SignedValue_Negative
    /// 目的: 負の符号付き値(9bit)が正しく処理されることを確認する
    /// 手順: 負の値(-255)でラウンドトリップを行う
    /// 期待値: -255が復元される
    /// </summary>
    [Test]
    public void SampleUpMessage_SignedValue_Negative()
    {
        var original = new SampleUpMessage { SignedValue = -255 };

        var writer = new BitWriter();
        original.WriteTo(writer);
        var data = writer.ToArray();

        var result = SampleUpMessage.ReadFrom(data);

        Assert.AreEqual(-255, result.SignedValue);
    }

    /// <summary>
    /// テスト名: SampleUpMessage_SignedValue_Zero
    /// 目的: ゼロの符号付き値が正しく処理されることを確認する
    /// 手順: ゼロでラウンドトリップを行う
    /// 期待値: 0が復元される
    /// </summary>
    [Test]
    public void SampleUpMessage_SignedValue_Zero()
    {
        var original = new SampleUpMessage { SignedValue = 0 };

        var writer = new BitWriter();
        original.WriteTo(writer);
        var data = writer.ToArray();

        var result = SampleUpMessage.ReadFrom(data);

        Assert.AreEqual(0, result.SignedValue);
    }

    /// <summary>
    /// テスト名: SampleUpMessage_Sequence_BigEndian
    /// 目的: Sequence(16bit)がビッグエンディアンで格納されることを確認する
    /// 手順: 0xABCDを設定し、バイト配列を確認する
    /// 期待値: 上位バイトが先に格納される
    /// </summary>
    [Test]
    public void SampleUpMessage_Sequence_BigEndian()
    {
        var original = new SampleUpMessage { Sequence = 0xABCD };

        var writer = new BitWriter();
        original.WriteTo(writer);
        var data = writer.ToArray();

        // ビット配置: Command(3) + Sign(1) + Value(8) + Reserved(4) + Sequence(16) + Position(32)
        // Sequence開始位置: (3+1+8+4)/8 = 2バイト目から
        Assert.AreEqual(0xAB, data[2]); // 上位バイト
        Assert.AreEqual(0xCD, data[3]); // 下位バイト
    }

    /// <summary>
    /// テスト名: SampleUpMessage_Position_FixedPoint
    /// 目的: Position(固定小数点)が正しく処理されることを確認する
    /// 手順: 小数値でラウンドトリップを行う
    /// 期待値: 精度範囲内で復元される
    /// </summary>
    [Test]
    public void SampleUpMessage_Position_FixedPoint()
    {
        var original = new SampleUpMessage { Position = -999.123 };

        var writer = new BitWriter();
        original.WriteTo(writer);
        var data = writer.ToArray();

        var result = SampleUpMessage.ReadFrom(data);

        Assert.AreEqualWithTolerance(-999.123, result.Position, 0.0001);
    }

    /// <summary>
    /// テスト名: SampleUpMessage_ToLogString
    /// 目的: ToLogStringが正しくフォーマットされることを確認する
    /// 手順: フィールドを設定し、ToLogStringを呼び出す
    /// 期待値: 各フィールドが含まれる文字列が返される
    /// </summary>
    [Test]
    public void SampleUpMessage_ToLogString()
    {
        var message = new SampleUpMessage
        {
            Command = CommandType.Start,
            SignedValue = -100,
            Sequence = 0x1234,
            Position = 50.5
        };

        var logString = message.ToLogString();

        Assert.IsTrue(logString.Contains("Command=Start"), "Should contain Command");
        Assert.IsTrue(logString.Contains("SignedValue=-100"), "Should contain SignedValue");
        Assert.IsTrue(logString.Contains("Sequence=0x1234"), "Should contain Sequence");
        Assert.IsTrue(logString.Contains("Position="), "Should contain Position");
    }

    #endregion

    #region SampleDownMessage Tests

    /// <summary>
    /// テスト名: SampleDownMessage_Roundtrip_AllFields
    /// 目的: SampleDownMessageの全フィールドが正しくシリアライズ・デシリアライズされることを確認する
    /// 手順: 全フィールドに値を設定し、書き込み→読み込みを行う
    /// 期待値: 全フィールドが元の値と一致する
    /// </summary>
    [Test]
    public void SampleDownMessage_Roundtrip_AllFields()
    {
        var original = new SampleDownMessage
        {
            Status = StatusType.Running,
            SignedValue = 50,
            Timestamp = 0xABCD,
            Velocity = 99.99
        };

        var writer = new BitWriter();
        original.WriteTo(writer);
        var data = writer.ToArray();

        var result = SampleDownMessage.ReadFrom(data);

        Assert.AreEqual(StatusType.Running, result.Status);
        Assert.AreEqual(50, result.SignedValue);
        Assert.AreEqual((ushort)0xABCD, result.Timestamp);
        Assert.AreEqualWithTolerance(99.99, result.Velocity, 0.0001);
    }

    /// <summary>
    /// テスト名: SampleDownMessage_PayloadSize
    /// 目的: SampleDownMessageのペイロードサイズが8バイトであることを確認する
    /// 手順: メッセージを書き込み、バイト配列の長さを確認する
    /// 期待値: 8バイト
    /// </summary>
    [Test]
    public void SampleDownMessage_PayloadSize()
    {
        var message = new SampleDownMessage();
        Assert.AreEqual(8, message.PayloadSize);
        Assert.AreEqual(8, SampleDownMessage.PayloadSizeConst);

        var writer = new BitWriter();
        message.WriteTo(writer);
        var data = writer.ToArray();

        Assert.AreEqual(8, data.Length);
    }

    /// <summary>
    /// テスト名: SampleDownMessage_Status_AllValues
    /// 目的: StatusType(3bit)の全値が正しく処理されることを確認する
    /// 手順: 各StatusType値でラウンドトリップを行う
    /// 期待値: 全ての値が正しく復元される
    /// </summary>
    [Test]
    public void SampleDownMessage_Status_AllValues()
    {
        foreach (StatusType status in Enum.GetValues(typeof(StatusType)))
        {
            var original = new SampleDownMessage { Status = status };

            var writer = new BitWriter();
            original.WriteTo(writer);
            var data = writer.ToArray();

            var result = SampleDownMessage.ReadFrom(data);

            Assert.AreEqual(status, result.Status, $"Failed for {status}");
        }
    }

    /// <summary>
    /// テスト名: SampleDownMessage_SignedValue_Boundary
    /// 目的: 符号付き値の境界値が正しく処理されることを確認する
    /// 手順: -255, 0, 255でラウンドトリップを行う
    /// 期待値: 全ての値が正しく復元される
    /// </summary>
    [Test]
    public void SampleDownMessage_SignedValue_Boundary()
    {
        int[] testValues = { -255, 0, 255 };

        foreach (var value in testValues)
        {
            var original = new SampleDownMessage { SignedValue = value };

            var writer = new BitWriter();
            original.WriteTo(writer);
            var data = writer.ToArray();

            var result = SampleDownMessage.ReadFrom(data);

            Assert.AreEqual(value, result.SignedValue, $"Failed for {value}");
        }
    }

    /// <summary>
    /// テスト名: SampleDownMessage_Timestamp_BigEndian
    /// 目的: Timestamp(16bit)がビッグエンディアンで格納されることを確認する
    /// 手順: 0x1234を設定し、バイト配列を確認する
    /// 期待値: 上位バイトが先に格納される
    /// </summary>
    [Test]
    public void SampleDownMessage_Timestamp_BigEndian()
    {
        var original = new SampleDownMessage { Timestamp = 0x1234 };

        var writer = new BitWriter();
        original.WriteTo(writer);
        var data = writer.ToArray();

        // ビット配置: Status(3) + Sign(1) + Value(8) + Reserved(4) + Timestamp(16) + Velocity(32)
        Assert.AreEqual(0x12, data[2]); // 上位バイト
        Assert.AreEqual(0x34, data[3]); // 下位バイト
    }

    /// <summary>
    /// テスト名: SampleDownMessage_Velocity_FixedPoint
    /// 目的: Velocity(固定小数点)が正しく処理されることを確認する
    /// 手順: 小数値でラウンドトリップを行う
    /// 期待値: 精度範囲内で復元される
    /// </summary>
    [Test]
    public void SampleDownMessage_Velocity_FixedPoint()
    {
        var original = new SampleDownMessage { Velocity = 1234.5678 };

        var writer = new BitWriter();
        original.WriteTo(writer);
        var data = writer.ToArray();

        var result = SampleDownMessage.ReadFrom(data);

        Assert.AreEqualWithTolerance(1234.5678, result.Velocity, 0.0001);
    }

    /// <summary>
    /// テスト名: SampleDownMessage_ToLogString
    /// 目的: ToLogStringが正しくフォーマットされることを確認する
    /// 手順: フィールドを設定し、ToLogStringを呼び出す
    /// 期待値: 各フィールドが含まれる文字列が返される
    /// </summary>
    [Test]
    public void SampleDownMessage_ToLogString()
    {
        var message = new SampleDownMessage
        {
            Status = StatusType.Error,
            SignedValue = -50,
            Timestamp = 0xFFFF,
            Velocity = 0.0
        };

        var logString = message.ToLogString();

        Assert.IsTrue(logString.Contains("Status=Error"), "Should contain Status");
        Assert.IsTrue(logString.Contains("SignedValue=-50"), "Should contain SignedValue");
        Assert.IsTrue(logString.Contains("Timestamp=0xFFFF"), "Should contain Timestamp");
        Assert.IsTrue(logString.Contains("Velocity="), "Should contain Velocity");
    }

    #endregion
}
