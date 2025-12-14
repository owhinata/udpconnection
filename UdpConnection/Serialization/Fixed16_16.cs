namespace UdpConnection.Serialization;

/// <summary>
/// 16.16固定小数点数のヘルパークラス。
/// 整数部16bit + 小数部16bit = 32bit
/// 範囲: -32768.0 ～ +32767.99998...
/// 精度: 約0.000015 (1/65536)
/// </summary>
public static class Fixed16_16
{
    private const double Scale = 65536.0; // 2^16

    public static int FromDouble(double value)
    {
        // オーバーフロー対策
        if (value >= 32768.0)
            return int.MaxValue;
        if (value < -32768.0)
            return int.MinValue;

        return (int)(value * Scale);
    }

    public static double ToDouble(int fixedValue)
    {
        return fixedValue / Scale;
    }
}
