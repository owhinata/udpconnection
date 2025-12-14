namespace UdpConnection.Messages;

/// <summary>
/// コマンド種別（3bit enum: 0-7）
/// </summary>
public enum CommandType : byte
{
    None = 0,
    Start = 1,
    Stop = 2,
    Reset = 3,
    Query = 4,
    Update = 5,
    // 6, 7 reserved
}
