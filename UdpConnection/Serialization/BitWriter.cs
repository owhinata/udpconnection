namespace UdpConnection.Serialization;

/// <summary>
/// ビットレベルでデータを書き込むためのライター。
/// ビッグエンディアンで書き込みを行う。
/// </summary>
public class BitWriter
{
    private readonly List<byte> _buffer = new();
    private byte _currentByte;
    private int _bitPosition; // 現在のバイト内でのビット位置 (0-7, MSBから)

    public int TotalBits => _buffer.Count * 8 + _bitPosition;

    public void WriteBits(uint value, int bitCount)
    {
        if (bitCount < 1 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount), "bitCount must be between 1 and 32");

        // MSBから順に書き込む
        for (int i = bitCount - 1; i >= 0; i--)
        {
            uint bit = (value >> i) & 1;
            _currentByte |= (byte)(bit << (7 - _bitPosition));
            _bitPosition++;

            if (_bitPosition == 8)
            {
                _buffer.Add(_currentByte);
                _currentByte = 0;
                _bitPosition = 0;
            }
        }
    }

    public void WriteBool(bool value)
    {
        WriteBits(value ? 1u : 0u, 1);
    }

    public void WriteByte(byte value)
    {
        WriteBits(value, 8);
    }

    public void WriteUInt16(ushort value)
    {
        WriteBits(value, 16);
    }

    public void WriteInt32(int value)
    {
        WriteBits((uint)value, 32);
    }

    public void WriteUInt32(uint value)
    {
        WriteBits(value, 32);
    }

    public void WriteFixed16_16(double value)
    {
        int fixedValue = Fixed16_16.FromDouble(value);
        WriteInt32(fixedValue);
    }

    public byte[] ToArray()
    {
        if (_bitPosition == 0)
        {
            return _buffer.ToArray();
        }

        // 残りのビットがある場合は最後のバイトを追加
        var result = new byte[_buffer.Count + 1];
        _buffer.CopyTo(result);
        result[^1] = _currentByte;
        return result;
    }

    public void Reset()
    {
        _buffer.Clear();
        _currentByte = 0;
        _bitPosition = 0;
    }
}
