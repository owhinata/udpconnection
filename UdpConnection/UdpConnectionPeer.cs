using UdpConnection.Logging;
using UdpConnection.Messages;
using UdpConnection.Protocol;
using UdpConnection.Serialization;

namespace UdpConnection;

/// <summary>
/// Peer側のUDP接続クラス
/// SampleUpメッセージを送信し、SampleDownメッセージを受信する
/// </summary>
public class UdpConnectionPeer : UdpConnectionBase
{
    private readonly object _negotiationLock = new();
    private Timer? _negotiationTimer;
    private ushort _peerId;
    private ushort _sessionId;
    private int _missCount;
    private bool _waitingForResponse;

    /// <summary>
    /// Peer ID（Start時に指定）
    /// </summary>
    public ushort PeerId => _peerId;

    /// <summary>
    /// セッションID（Controller側管理ID、0=未接続）
    /// </summary>
    public ushort SessionId => _sessionId;

    /// <summary>
    /// 未接続時の送信間隔（デフォルト3秒、0=自動送信無効）
    /// </summary>
    public TimeSpan DisconnectedInterval { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// 接続時の送信間隔（デフォルト60秒、0=自動送信無効）
    /// </summary>
    public TimeSpan ConnectedInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 接続状態（SessionId != 0）
    /// </summary>
    public bool IsConnected => _sessionId != 0;

    public UdpConnectionPeer(ILogger? logger = null)
        : base(logger)
    {
    }

    /// <summary>
    /// SampleDownメッセージ受信時に発火するイベント
    /// </summary>
    public event EventHandler<SampleDownMessage>? SampleDownReceived;

    /// <summary>
    /// ネゴシエーション状態変更時に発火するイベント
    /// </summary>
    public event EventHandler<NegotiationStateChangedEventArgs>? NegotiationStateChanged;

    /// <summary>
    /// 送受信を開始する
    /// </summary>
    /// <param name="options">接続オプション</param>
    /// <returns>成功した場合はtrue</returns>
    public override bool Start(UdpConnectionOptions options)
    {
        _peerId = options.PeerId;
        _sessionId = 0;
        _missCount = 0;
        _waitingForResponse = false;

        var result = base.Start(options);
        if (result)
        {
            // タイマーで定期的にNegotiationRequest送信
            var timerStarted = StartNegotiationTimer();
            if (timerStarted)
            {
                // タイマー開始時は即座にネゴシエーション送信
                SendNegotiationImmediate();
            }
        }
        return result;
    }

    /// <summary>
    /// 送受信を停止する
    /// </summary>
    /// <returns>成功した場合はtrue</returns>
    public override bool Stop()
    {
        StopNegotiationTimer();
        return base.Stop();
    }

    /// <summary>
    /// 手動でNegotiationRequestを送信する
    /// </summary>
    /// <returns>送信キューへの追加に成功した場合はtrue</returns>
    public bool SendNegotiation()
    {
        return SendNegotiationInternal(isTimerTrigger: false);
    }

    /// <summary>
    /// SampleUpメッセージを送信する
    /// </summary>
    /// <param name="message">送信するメッセージ</param>
    /// <returns>送信キューへの追加に成功した場合はtrue</returns>
    public bool SendSampleUpMessage(SampleUpMessage message)
    {
        // SessionIdとPeerIdを自動設定
        message.SessionId = _sessionId;
        message.PeerId = _peerId;
        return SendMessage(MessageType.SampleUp, message);
    }

    protected override void ProcessReceivedData(byte[] data, System.Net.IPEndPoint remoteEndPoint)
    {
        if (data.Length < ProtocolConstants.HeaderSize)
        {
            return; // ヘッダーが不完全
        }

        var header = MessageHeader.ReadFrom(data);

        if (data.Length < ProtocolConstants.HeaderSize + header.PayloadLength)
        {
            return; // ペイロードが不完全
        }

        var payloadReader = new BitReader(data, ProtocolConstants.HeaderSize, header.PayloadLength);

        switch (header.Type)
        {
            case MessageType.NegotiationResponse:
                var negotiationResponse = NegotiationResponseMessage.ReadFrom(payloadReader);
                LogReceive(header.Type, negotiationResponse, data);
                ProcessNegotiationResponse(negotiationResponse);
                break;

            case MessageType.SampleDown:
                var sampleDownMessage = SampleDownMessage.ReadFrom(payloadReader);
                LogReceive(header.Type, sampleDownMessage, data);
                SampleDownReceived?.Invoke(this, sampleDownMessage);
                break;

            default:
                // 未知のメッセージタイプは無視
                break;
        }
    }

    private void ProcessNegotiationResponse(NegotiationResponseMessage response)
    {
        // PeerIdが一致しない場合は無視
        if (response.PeerId != _peerId)
        {
            return;
        }

        bool wasDisconnected;
        ushort newSessionId;

        lock (_negotiationLock)
        {
            wasDisconnected = _sessionId == 0;
            newSessionId = response.SessionId;
            _sessionId = newSessionId;
            _missCount = 0;
            _waitingForResponse = false;

            // タイマー間隔を更新
            UpdateTimerInterval();
        }

        // 接続成功イベント発火・ログ出力
        if (wasDisconnected && newSessionId != 0)
        {
            LogNegotiationState(NegotiationState.Connected, _peerId, newSessionId, 0);
            NegotiationStateChanged?.Invoke(this, new NegotiationStateChangedEventArgs(
                NegotiationState.Connected,
                _peerId,
                newSessionId));
        }
    }

    private void LogNegotiationState(NegotiationState state, ushort peerId, ushort sessionId, int missCount)
    {
        var stateStr = state switch
        {
            NegotiationState.Connected => "Connected",
            NegotiationState.Timeout => $"Timeout (miss={missCount})",
            NegotiationState.Disconnected => "Disconnected",
            _ => state.ToString()
        };
        LogInfo($"[Nego] State={stateStr}, PeerId=0x{peerId:X4}, SessionId=0x{sessionId:X4}");
    }

    private bool SendNegotiationInternal(bool isTimerTrigger)
    {
        int currentMissCount;
        ushort currentSessionId;
        bool shouldFireTimeout = false;
        bool shouldFireDisconnected = false;

        lock (_negotiationLock)
        {
            // Connected状態で前回の送信に対する応答がなかった場合のみmissCountをインクリメント
            // Disconnected状態ではmissCountは関係ない（既に切断済み）
            if (_waitingForResponse && _sessionId != 0)
            {
                _missCount++;

                if (_missCount >= 3)
                {
                    // 3回連続ミス → Disconnected
                    shouldFireDisconnected = true;
                    currentSessionId = _sessionId;
                    _sessionId = 0;
                    _missCount = 0;
                }
                else
                {
                    // 1回目、2回目のミス → Timeout
                    shouldFireTimeout = true;
                }
            }

            currentMissCount = _missCount;
            currentSessionId = _sessionId;
            _waitingForResponse = true;

            // タイマー間隔を更新（状態変化に対応）
            if (shouldFireDisconnected)
            {
                UpdateTimerInterval();
            }
        }

        // イベント発火・ログ出力（ロック外で）
        if (shouldFireDisconnected)
        {
            LogNegotiationState(NegotiationState.Disconnected, _peerId, currentSessionId, 3);
            NegotiationStateChanged?.Invoke(this, new NegotiationStateChangedEventArgs(
                NegotiationState.Disconnected,
                _peerId,
                currentSessionId,
                3));
        }
        else if (shouldFireTimeout)
        {
            LogNegotiationState(NegotiationState.Timeout, _peerId, currentSessionId, currentMissCount);
            NegotiationStateChanged?.Invoke(this, new NegotiationStateChangedEventArgs(
                NegotiationState.Timeout,
                _peerId,
                currentSessionId,
                currentMissCount));
        }

        // NegotiationRequest送信
        var message = new NegotiationRequestMessage
        {
            SessionId = _sessionId,
            PeerId = _peerId
        };

        return SendMessage(MessageType.NegotiationRequest, message);
    }

    /// <summary>
    /// 即座にネゴシエーションを送信する（missCount処理なし）
    /// </summary>
    private bool SendNegotiationImmediate()
    {
        lock (_negotiationLock)
        {
            _waitingForResponse = true;
        }

        var message = new NegotiationRequestMessage
        {
            SessionId = _sessionId,
            PeerId = _peerId
        };

        return SendMessage(MessageType.NegotiationRequest, message);
    }

    private void OnNegotiationTimerElapsed(object? state)
    {
        SendNegotiationInternal(isTimerTrigger: true);
    }

    private bool StartNegotiationTimer()
    {
        lock (_negotiationLock)
        {
            StopNegotiationTimerInternal();

            var interval = GetCurrentInterval();
            if (interval > TimeSpan.Zero)
            {
                _negotiationTimer = new Timer(
                    OnNegotiationTimerElapsed,
                    null,
                    interval,
                    interval);
                return true;
            }
            return false;
        }
    }

    private void UpdateTimerInterval()
    {
        // ロック内で呼ばれる想定
        StopNegotiationTimerInternal();

        var interval = GetCurrentInterval();
        if (interval > TimeSpan.Zero)
        {
            _negotiationTimer = new Timer(
                OnNegotiationTimerElapsed,
                null,
                interval,
                interval);
        }
    }

    private TimeSpan GetCurrentInterval()
    {
        return _sessionId == 0 ? DisconnectedInterval : ConnectedInterval;
    }

    private void StopNegotiationTimer()
    {
        lock (_negotiationLock)
        {
            StopNegotiationTimerInternal();
        }
    }

    private void StopNegotiationTimerInternal()
    {
        // ロック内で呼ばれる想定
        _negotiationTimer?.Dispose();
        _negotiationTimer = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopNegotiationTimer();
        }
        base.Dispose(disposing);
    }
}
