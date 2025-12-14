namespace UdpConnection.Messages;

/// <summary>
/// ステータス種別（3bit enum: 0-7）
/// </summary>
public enum StatusType : byte
{
    Unknown = 0,
    Ready = 1,
    Running = 2,
    Paused = 3,
    Error = 4,
    Complete = 5,
    // 6, 7 reserved
}
