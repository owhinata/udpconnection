using UdpConnection.Protocol;
using UdpConnection.Serialization;
using UdpConnection.Tests.TestRunner;

namespace UdpConnection.Tests;

public class ProtocolTests
{
    #region MessageHeader Tests

    /// <summary>
    /// テスト名: MessageHeader_Roundtrip_SampleUp
    /// 目的: MessageHeaderのSampleUp型を書き込み・読み込みできることを確認する
    /// 手順: SampleUp型、ペイロード長8のヘッダーを書き込み、読み込む
    /// 期待値: 同じType、PayloadLengthが復元される
    /// </summary>
    [Test]
    public void MessageHeader_Roundtrip_SampleUp()
    {
        var original = new MessageHeader(MessageType.SampleUp, 8);

        var writer = new BitWriter();
        original.WriteTo(writer);
        var data = writer.ToArray();

        var result = MessageHeader.ReadFrom(data);

        Assert.AreEqual(MessageType.SampleUp, result.Type);
        Assert.AreEqual((ushort)8, result.PayloadLength);
    }

    /// <summary>
    /// テスト名: MessageHeader_Roundtrip_SampleDown
    /// 目的: MessageHeaderのSampleDown型を書き込み・読み込みできることを確認する
    /// 手順: SampleDown型、ペイロード長8のヘッダーを書き込み、読み込む
    /// 期待値: 同じType、PayloadLengthが復元される
    /// </summary>
    [Test]
    public void MessageHeader_Roundtrip_SampleDown()
    {
        var original = new MessageHeader(MessageType.SampleDown, 8);

        var writer = new BitWriter();
        original.WriteTo(writer);
        var data = writer.ToArray();

        var result = MessageHeader.ReadFrom(data);

        Assert.AreEqual(MessageType.SampleDown, result.Type);
        Assert.AreEqual((ushort)8, result.PayloadLength);
    }

    /// <summary>
    /// テスト名: MessageHeader_Size
    /// 目的: MessageHeaderのサイズが4バイトであることを確認する
    /// 手順: ヘッダーを書き込み、バイト配列の長さを確認する
    /// 期待値: 4バイト
    /// </summary>
    [Test]
    public void MessageHeader_Size()
    {
        var header = new MessageHeader(MessageType.SampleUp, 100);

        var writer = new BitWriter();
        header.WriteTo(writer);
        var data = writer.ToArray();

        Assert.AreEqual(ProtocolConstants.HeaderSize, data.Length);
        Assert.AreEqual(4, data.Length);
    }

    /// <summary>
    /// テスト名: MessageHeader_PayloadLength_BigEndian
    /// 目的: PayloadLengthがビッグエンディアンで格納されることを確認する
    /// 手順: ペイロード長0x1234のヘッダーを書き込み、バイト配列を確認する
    /// 期待値: [2]が0x12、[3]が0x34
    /// </summary>
    [Test]
    public void MessageHeader_PayloadLength_BigEndian()
    {
        var header = new MessageHeader(MessageType.SampleUp, 0x1234);

        var writer = new BitWriter();
        header.WriteTo(writer);
        var data = writer.ToArray();

        // ヘッダー構造: [Type(1)] [Reserved(1)] [PayloadLength(2)]
        Assert.AreEqual(0x12, data[2]); // 上位バイト
        Assert.AreEqual(0x34, data[3]); // 下位バイト
    }

    /// <summary>
    /// テスト名: MessageHeader_Unknown
    /// 目的: Unknown型のMessageHeaderを処理できることを確認する
    /// 手順: Unknown型のヘッダーを書き込み、読み込む
    /// 期待値: MessageType.Unknownが復元される
    /// </summary>
    [Test]
    public void MessageHeader_Unknown()
    {
        var original = new MessageHeader(MessageType.Unknown, 0);

        var writer = new BitWriter();
        original.WriteTo(writer);
        var data = writer.ToArray();

        var result = MessageHeader.ReadFrom(data);

        Assert.AreEqual(MessageType.Unknown, result.Type);
        Assert.AreEqual((ushort)0, result.PayloadLength);
    }

    #endregion
}
