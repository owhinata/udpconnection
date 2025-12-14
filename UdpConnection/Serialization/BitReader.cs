namespace UdpConnection.Serialization;

/// <summary>
/// ビットレベルでデータを読み取るためのリーダー。
/// ビッグエンディアンで読み取りを行う。
/// </summary>
public class BitReader
{
    private readonly byte[] _buffer;
    private int _bytePosition;
    private int _bitPosition; // 現在のバイト内でのビット位置 (0-7, MSBから)

    public BitReader(byte[] buffer)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    public BitReader(byte[] buffer, int offset, int count)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        _buffer = new byte[count];
        Array.Copy(buffer, offset, _buffer, 0, count);
    }

    public int RemainingBits => (_buffer.Length - _bytePosition) * 8 - _bitPosition;

    public uint ReadBits(int bitCount)
    {
        if (bitCount < 1 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount), "bitCount must be between 1 and 32");

        if (RemainingBits < bitCount)
            throw new InvalidOperationException("Not enough bits remaining in buffer");

        uint result = 0;

        // MSBから順に読み取る
        for (int i = 0; i < bitCount; i++)
        {
            int bit = (_buffer[_bytePosition] >> (7 - _bitPosition)) & 1;
            result = (result << 1) | (uint)bit;
            _bitPosition++;

            if (_bitPosition == 8)
            {
                _bytePosition++;
                _bitPosition = 0;
            }
        }

        return result;
    }

    public bool ReadBool()
    {
        return ReadBits(1) == 1;
    }

    public byte ReadByte()
    {
        return (byte)ReadBits(8);
    }

    public ushort ReadUInt16()
    {
        return (ushort)ReadBits(16);
    }

    public int ReadInt32()
    {
        return (int)ReadBits(32);
    }

    public uint ReadUInt32()
    {
        return ReadBits(32);
    }

    public double ReadFixed16_16()
    {
        int fixedValue = ReadInt32();
        return Fixed16_16.ToDouble(fixedValue);
    }

    public void Skip(int bitCount)
    {
        if (RemainingBits < bitCount)
            throw new InvalidOperationException("Not enough bits remaining in buffer");

        int totalBits = _bitPosition + bitCount;
        _bytePosition += totalBits / 8;
        _bitPosition = totalBits % 8;
    }

    public void Reset()
    {
        _bytePosition = 0;
        _bitPosition = 0;
    }
}
