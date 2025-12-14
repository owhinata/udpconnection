using UdpConnection.Serialization;
using UdpConnection.Tests.TestRunner;

namespace UdpConnection.Tests;

public class SerializationTests
{
    #region BitWriter Tests

    /// <summary>
    /// テスト名: BitWriter_WriteBool_True
    /// 目的: BitWriterで真偽値trueを1bitとして書き込めることを確認する
    /// 手順: WriteBool(true)を呼び出し、結果のバイト配列を確認する
    /// 期待値: 最上位ビットが1のバイト(0x80)が出力される
    /// </summary>
    [Test]
    public void BitWriter_WriteBool_True()
    {
        var writer = new BitWriter();
        writer.WriteBool(true);
        var result = writer.ToArray();

        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(0x80, result[0]);
    }

    /// <summary>
    /// テスト名: BitWriter_WriteBool_False
    /// 目的: BitWriterで真偽値falseを1bitとして書き込めることを確認する
    /// 手順: WriteBool(false)を呼び出し、結果のバイト配列を確認する
    /// 期待値: 最上位ビットが0のバイト(0x00)が出力される
    /// </summary>
    [Test]
    public void BitWriter_WriteBool_False()
    {
        var writer = new BitWriter();
        writer.WriteBool(false);
        var result = writer.ToArray();

        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(0x00, result[0]);
    }

    /// <summary>
    /// テスト名: BitWriter_WriteBits_3Bit
    /// 目的: BitWriterで3ビット値を書き込めることを確認する
    /// 手順: WriteBits(0b101, 3)を呼び出し、結果を確認する
    /// 期待値: 上位3ビットが101のバイト(0xA0)が出力される
    /// </summary>
    [Test]
    public void BitWriter_WriteBits_3Bit()
    {
        var writer = new BitWriter();
        writer.WriteBits(0b101, 3);
        var result = writer.ToArray();

        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(0xA0, result[0]); // 101_00000 = 0xA0
    }

    /// <summary>
    /// テスト名: BitWriter_WriteByte
    /// 目的: BitWriterで8ビット値を書き込めることを確認する
    /// 手順: WriteByte(0xAB)を呼び出し、結果を確認する
    /// 期待値: 0xABがそのまま出力される
    /// </summary>
    [Test]
    public void BitWriter_WriteByte()
    {
        var writer = new BitWriter();
        writer.WriteByte(0xAB);
        var result = writer.ToArray();

        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(0xAB, result[0]);
    }

    /// <summary>
    /// テスト名: BitWriter_WriteUInt16_BigEndian
    /// 目的: BitWriterで16ビット値をビッグエンディアンで書き込めることを確認する
    /// 手順: WriteUInt16(0x1234)を呼び出し、結果を確認する
    /// 期待値: [0x12, 0x34]がビッグエンディアン順で出力される
    /// </summary>
    [Test]
    public void BitWriter_WriteUInt16_BigEndian()
    {
        var writer = new BitWriter();
        writer.WriteUInt16(0x1234);
        var result = writer.ToArray();

        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(0x12, result[0]); // 上位バイト
        Assert.AreEqual(0x34, result[1]); // 下位バイト
    }

    /// <summary>
    /// テスト名: BitWriter_WriteInt32_BigEndian
    /// 目的: BitWriterで32ビット値をビッグエンディアンで書き込めることを確認する
    /// 手順: WriteInt32(0x12345678)を呼び出し、結果を確認する
    /// 期待値: [0x12, 0x34, 0x56, 0x78]がビッグエンディアン順で出力される
    /// </summary>
    [Test]
    public void BitWriter_WriteInt32_BigEndian()
    {
        var writer = new BitWriter();
        writer.WriteInt32(0x12345678);
        var result = writer.ToArray();

        Assert.AreEqual(4, result.Length);
        Assert.AreEqual(0x12, result[0]);
        Assert.AreEqual(0x34, result[1]);
        Assert.AreEqual(0x56, result[2]);
        Assert.AreEqual(0x78, result[3]);
    }

    /// <summary>
    /// テスト名: BitWriter_MultipleFields
    /// 目的: BitWriterで複数フィールドを連続書き込みできることを確認する
    /// 手順: 3bit + 1bit + 8bit + 4bit = 16bit を書き込み、結果を確認する
    /// 期待値: ビットが正しく連結された2バイトが出力される
    /// </summary>
    [Test]
    public void BitWriter_MultipleFields()
    {
        var writer = new BitWriter();
        writer.WriteBits(0b101, 3);  // 101
        writer.WriteBool(true);       // 1
        writer.WriteBits(0xFF, 8);    // 11111111
        writer.WriteBits(0x0, 4);     // 0000

        var result = writer.ToArray();

        // 101_1_1111_1111_0000 = 0xBF 0xF0
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(0xBF, result[0]);
        Assert.AreEqual(0xF0, result[1]);
    }

    #endregion

    #region BitReader Tests

    /// <summary>
    /// テスト名: BitReader_ReadBool_True
    /// 目的: BitReaderで1ビットをboolとして読み込めることを確認する
    /// 手順: 0x80(最上位ビット=1)からReadBool()を呼び出す
    /// 期待値: trueが返される
    /// </summary>
    [Test]
    public void BitReader_ReadBool_True()
    {
        var reader = new BitReader(new byte[] { 0x80 });
        var result = reader.ReadBool();

        Assert.IsTrue(result);
    }

    /// <summary>
    /// テスト名: BitReader_ReadBool_False
    /// 目的: BitReaderで1ビットをboolとして読み込めることを確認する
    /// 手順: 0x00(最上位ビット=0)からReadBool()を呼び出す
    /// 期待値: falseが返される
    /// </summary>
    [Test]
    public void BitReader_ReadBool_False()
    {
        var reader = new BitReader(new byte[] { 0x00 });
        var result = reader.ReadBool();

        Assert.IsFalse(result);
    }

    /// <summary>
    /// テスト名: BitReader_ReadBits_3Bit
    /// 目的: BitReaderで3ビット値を読み込めることを確認する
    /// 手順: 0xA0(上位3ビット=101)からReadBits(3)を呼び出す
    /// 期待値: 5(0b101)が返される
    /// </summary>
    [Test]
    public void BitReader_ReadBits_3Bit()
    {
        var reader = new BitReader(new byte[] { 0xA0 });
        var result = reader.ReadBits(3);

        Assert.AreEqual(5u, result); // 0b101 = 5
    }

    /// <summary>
    /// テスト名: BitReader_ReadByte
    /// 目的: BitReaderで8ビット値を読み込めることを確認する
    /// 手順: 0xABからReadByte()を呼び出す
    /// 期待値: 0xABが返される
    /// </summary>
    [Test]
    public void BitReader_ReadByte()
    {
        var reader = new BitReader(new byte[] { 0xAB });
        var result = reader.ReadByte();

        Assert.AreEqual(0xAB, result);
    }

    /// <summary>
    /// テスト名: BitReader_ReadUInt16_BigEndian
    /// 目的: BitReaderで16ビット値をビッグエンディアンで読み込めることを確認する
    /// 手順: [0x12, 0x34]からReadUInt16()を呼び出す
    /// 期待値: 0x1234が返される
    /// </summary>
    [Test]
    public void BitReader_ReadUInt16_BigEndian()
    {
        var reader = new BitReader(new byte[] { 0x12, 0x34 });
        var result = reader.ReadUInt16();

        Assert.AreEqual((ushort)0x1234, result);
    }

    /// <summary>
    /// テスト名: BitReader_ReadInt32_BigEndian
    /// 目的: BitReaderで32ビット値をビッグエンディアンで読み込めることを確認する
    /// 手順: [0x12, 0x34, 0x56, 0x78]からReadInt32()を呼び出す
    /// 期待値: 0x12345678が返される
    /// </summary>
    [Test]
    public void BitReader_ReadInt32_BigEndian()
    {
        var reader = new BitReader(new byte[] { 0x12, 0x34, 0x56, 0x78 });
        var result = reader.ReadInt32();

        Assert.AreEqual(0x12345678, result);
    }

    /// <summary>
    /// テスト名: BitReader_Skip
    /// 目的: BitReaderでビットをスキップできることを確認する
    /// 手順: 4ビットスキップ後に4ビット読み込む
    /// 期待値: 下位4ビットの値が返される
    /// </summary>
    [Test]
    public void BitReader_Skip()
    {
        var reader = new BitReader(new byte[] { 0xAB }); // 1010_1011
        reader.Skip(4);
        var result = reader.ReadBits(4);

        Assert.AreEqual(0xBu, result); // 1011 = 0xB
    }

    /// <summary>
    /// テスト名: BitReader_MultipleFields
    /// 目的: BitReaderで複数フィールドを連続読み込みできることを確認する
    /// 手順: 3bit + 1bit + 8bit + 4bit を順に読み込む
    /// 期待値: 各フィールドが正しく読み込まれる
    /// </summary>
    [Test]
    public void BitReader_MultipleFields()
    {
        // 101_1_1111_1111_0000 = 0xBF 0xF0
        var reader = new BitReader(new byte[] { 0xBF, 0xF0 });

        Assert.AreEqual(5u, reader.ReadBits(3));    // 101
        Assert.IsTrue(reader.ReadBool());            // 1
        Assert.AreEqual(0xFFu, reader.ReadBits(8)); // 11111111
        Assert.AreEqual(0x0u, reader.ReadBits(4));  // 0000
    }

    #endregion

    #region BitWriter + BitReader Roundtrip Tests

    /// <summary>
    /// テスト名: BitWriter_BitReader_Roundtrip
    /// 目的: BitWriterで書き込んだデータをBitReaderで正しく読み込めることを確認する
    /// 手順: 複数フィールドを書き込み、同じ順序で読み込む
    /// 期待値: 書き込んだ値と読み込んだ値が一致する
    /// </summary>
    [Test]
    public void BitWriter_BitReader_Roundtrip()
    {
        var writer = new BitWriter();
        writer.WriteBits(7, 3);       // 3bit
        writer.WriteBool(true);        // 1bit
        writer.WriteByte(0xCD);        // 8bit
        writer.WriteUInt16(0x1234);    // 16bit
        writer.WriteInt32(unchecked((int)0xDEADBEEF)); // 32bit

        var data = writer.ToArray();
        var reader = new BitReader(data);

        Assert.AreEqual(7u, reader.ReadBits(3));
        Assert.IsTrue(reader.ReadBool());
        Assert.AreEqual(0xCD, reader.ReadByte());
        Assert.AreEqual((ushort)0x1234, reader.ReadUInt16());
        Assert.AreEqual(unchecked((int)0xDEADBEEF), reader.ReadInt32());
    }

    #endregion

    #region Fixed16_16 Tests

    /// <summary>
    /// テスト名: Fixed16_16_PositiveInteger
    /// 目的: 正の整数を固定小数点に変換できることを確認する
    /// 手順: 100.0をFromDoubleで変換し、ToDoubleで戻す
    /// 期待値: 100.0が返される
    /// </summary>
    [Test]
    public void Fixed16_16_PositiveInteger()
    {
        int fixedValue = Fixed16_16.FromDouble(100.0);
        double result = Fixed16_16.ToDouble(fixedValue);

        Assert.AreEqual(100.0, result);
    }

    /// <summary>
    /// テスト名: Fixed16_16_NegativeInteger
    /// 目的: 負の整数を固定小数点に変換できることを確認する
    /// 手順: -50.0をFromDoubleで変換し、ToDoubleで戻す
    /// 期待値: -50.0が返される
    /// </summary>
    [Test]
    public void Fixed16_16_NegativeInteger()
    {
        int fixedValue = Fixed16_16.FromDouble(-50.0);
        double result = Fixed16_16.ToDouble(fixedValue);

        Assert.AreEqual(-50.0, result);
    }

    /// <summary>
    /// テスト名: Fixed16_16_Fraction
    /// 目的: 小数を固定小数点に変換できることを確認する（精度許容）
    /// 手順: 123.456をFromDoubleで変換し、ToDoubleで戻す
    /// 期待値: 123.456に近い値が返される（誤差0.0001以内）
    /// </summary>
    [Test]
    public void Fixed16_16_Fraction()
    {
        double original = 123.456;
        int fixedValue = Fixed16_16.FromDouble(original);
        double result = Fixed16_16.ToDouble(fixedValue);

        // 16.16固定小数点の精度は約0.000015なので、0.0001以内であればOK
        Assert.AreEqualWithTolerance(original, result, 0.0001);
    }

    /// <summary>
    /// テスト名: Fixed16_16_Zero
    /// 目的: 0を固定小数点に変換できることを確認する
    /// 手順: 0.0をFromDoubleで変換し、ToDoubleで戻す
    /// 期待値: 0.0が返される
    /// </summary>
    [Test]
    public void Fixed16_16_Zero()
    {
        int fixedValue = Fixed16_16.FromDouble(0.0);
        double result = Fixed16_16.ToDouble(fixedValue);

        Assert.AreEqual(0.0, result);
    }

    /// <summary>
    /// テスト名: Fixed16_16_MaxValue
    /// 目的: 最大値付近を固定小数点に変換できることを確認する
    /// 手順: 32767.0をFromDoubleで変換し、ToDoubleで戻す
    /// 期待値: 32767.0が返される
    /// </summary>
    [Test]
    public void Fixed16_16_MaxValue()
    {
        int fixedValue = Fixed16_16.FromDouble(32767.0);
        double result = Fixed16_16.ToDouble(fixedValue);

        Assert.AreEqual(32767.0, result);
    }

    /// <summary>
    /// テスト名: Fixed16_16_MinValue
    /// 目的: 最小値付近を固定小数点に変換できることを確認する
    /// 手順: -32768.0をFromDoubleで変換し、ToDoubleで戻す
    /// 期待値: -32768.0が返される
    /// </summary>
    [Test]
    public void Fixed16_16_MinValue()
    {
        int fixedValue = Fixed16_16.FromDouble(-32768.0);
        double result = Fixed16_16.ToDouble(fixedValue);

        Assert.AreEqual(-32768.0, result);
    }

    #endregion
}
