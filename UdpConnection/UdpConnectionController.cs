using System.Collections.Concurrent;
using System.Net;

using UdpConnection.Logging;
using UdpConnection.Messages;
using UdpConnection.Protocol;
using UdpConnection.Serialization;

namespace UdpConnection;

/// <summary>
/// Controller側のUDP接続クラス
/// SampleDownメッセージを送信し、SampleUpメッセージを受信する
/// </summary>
public class UdpConnectionController : UdpConnectionBase
{
    private readonly ConcurrentDictionary<ushort, PeerInfo> _peers = new();
    private readonly ConcurrentDictionary<ushort, ushort> _peerIdToSessionId = new();
    private readonly object _sessionIdLock = new();
    private ushort _nextSessionId = 1;
    private Timer? _cleanupTimer;

    /// <summary>
    /// Peerタイムアウト時間（デフォルト180秒）
    /// </summary>
    public TimeSpan PeerTimeout { get; set; } = TimeSpan.FromSeconds(180);

    /// <summary>
    /// 接続中のPeer一覧（SessionId → PeerInfo）
    /// </summary>
    public IReadOnlyDictionary<ushort, PeerInfo> Peers => _peers;

    public UdpConnectionController(ILogger? logger = null)
        : base(logger)
    {
    }

    /// <summary>
    /// SampleUpメッセージ受信時に発火するイベント
    /// </summary>
    public event EventHandler<SampleUpMessage>? SampleUpReceived;

    /// <summary>
    /// Peer状態変更時に発火するイベント
    /// </summary>
    public event EventHandler<PeerStateChangedEventArgs>? PeerStateChanged;

    /// <summary>
    /// 送受信を開始する
    /// </summary>
    /// <param name="options">接続オプション</param>
    /// <returns>成功した場合はtrue</returns>
    public override bool Start(UdpConnectionOptions options)
    {
        var result = base.Start(options);
        if (result)
        {
            StartCleanupTimer();
        }
        return result;
    }

    /// <summary>
    /// 送受信を停止する
    /// </summary>
    /// <returns>成功した場合はtrue</returns>
    public override bool Stop()
    {
        StopCleanupTimer();
        _peers.Clear();
        _peerIdToSessionId.Clear();
        lock (_sessionIdLock)
        {
            _nextSessionId = 1;
        }
        return base.Stop();
    }

    /// <summary>
    /// SampleDownメッセージを指定したSessionIdのPeerへ送信する
    /// </summary>
    /// <param name="message">送信するメッセージ（SessionIdで送信先を特定）</param>
    /// <returns>送信キューへの追加に成功した場合はtrue</returns>
    public bool SendSampleDownMessage(SampleDownMessage message)
    {
        // SessionIdからPeerInfoを取得して送信先を決定
        if (_peers.TryGetValue(message.SessionId, out var peerInfo))
        {
            return SendMessageTo(MessageType.SampleDown, message, peerInfo.RemoteEndPoint);
        }

        // SessionIdが不明な場合はデフォルトの送信先へ（後方互換）
        return SendMessage(MessageType.SampleDown, message);
    }

    protected override void ProcessReceivedData(byte[] data, IPEndPoint remoteEndPoint)
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
            case MessageType.NegotiationRequest:
                var negotiationRequest = NegotiationRequestMessage.ReadFrom(payloadReader);
                LogReceive(header.Type, negotiationRequest, data);
                ProcessNegotiationRequest(negotiationRequest, remoteEndPoint);
                break;

            case MessageType.SampleUp:
                var sampleUpMessage = SampleUpMessage.ReadFrom(payloadReader);
                LogReceive(header.Type, sampleUpMessage, data);
                SampleUpReceived?.Invoke(this, sampleUpMessage);
                break;

            default:
                // 未知のメッセージタイプは無視
                break;
        }
    }

    private void ProcessNegotiationRequest(NegotiationRequestMessage request, IPEndPoint remoteEndPoint)
    {
        ushort sessionId;
        bool isNewPeer = false;

        // 既存のPeerか確認
        if (_peerIdToSessionId.TryGetValue(request.PeerId, out sessionId))
        {
            // 既存Peer: タイムスタンプ更新、エンドポイント更新（アドレス変更対応）
            if (_peers.TryGetValue(sessionId, out var peerInfo))
            {
                peerInfo.LastNegotiationTime = DateTime.UtcNow;
                peerInfo.UpdateRemoteEndPoint(remoteEndPoint);
            }
        }
        else
        {
            // 新規Peer: SessionId生成
            lock (_sessionIdLock)
            {
                sessionId = _nextSessionId++;
                if (_nextSessionId == 0)
                {
                    _nextSessionId = 1; // 0はスキップ
                }
            }

            var newPeerInfo = new PeerInfo(request.PeerId, remoteEndPoint);
            _peers[sessionId] = newPeerInfo;
            _peerIdToSessionId[request.PeerId] = sessionId;
            isNewPeer = true;
        }

        // NegotiationResponse送信（受信元アドレスへ）
        var response = new NegotiationResponseMessage
        {
            SessionId = sessionId,
            PeerId = request.PeerId
        };
        SendMessageTo(MessageType.NegotiationResponse, response, remoteEndPoint);

        // 新規Peer接続イベント発火・ログ出力
        if (isNewPeer)
        {
            LogPeerState(PeerState.Connected, request.PeerId, sessionId, remoteEndPoint);
            PeerStateChanged?.Invoke(this, new PeerStateChangedEventArgs(
                PeerState.Connected,
                request.PeerId,
                sessionId,
                remoteEndPoint));
        }
    }

    private void StartCleanupTimer()
    {
        StopCleanupTimer();

        // 30秒ごとにタイムアウトチェック
        _cleanupTimer = new Timer(
            OnCleanupTimerElapsed,
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
    }

    private void StopCleanupTimer()
    {
        _cleanupTimer?.Dispose();
        _cleanupTimer = null;
    }

    private void OnCleanupTimerElapsed(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredPeers = new List<(ushort sessionId, PeerInfo info)>();

        foreach (var kvp in _peers)
        {
            if (now - kvp.Value.LastNegotiationTime > PeerTimeout)
            {
                expiredPeers.Add((kvp.Key, kvp.Value));
            }
        }

        foreach (var (sessionId, info) in expiredPeers)
        {
            if (_peers.TryRemove(sessionId, out _))
            {
                _peerIdToSessionId.TryRemove(info.PeerId, out _);

                // Peer切断イベント発火・ログ出力
                LogPeerState(PeerState.Disconnected, info.PeerId, sessionId, info.RemoteEndPoint);
                PeerStateChanged?.Invoke(this, new PeerStateChangedEventArgs(
                    PeerState.Disconnected,
                    info.PeerId,
                    sessionId,
                    info.RemoteEndPoint));
            }
        }
    }

    private void LogPeerState(PeerState state, ushort peerId, ushort sessionId, System.Net.IPEndPoint remoteEndPoint)
    {
        var stateStr = state switch
        {
            PeerState.Connected => "Connected",
            PeerState.Disconnected => "Disconnected",
            _ => state.ToString()
        };
        LogInfo($"[Peer] State={stateStr}, PeerId=0x{peerId:X4}, SessionId=0x{sessionId:X4}, Endpoint={remoteEndPoint}");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopCleanupTimer();
        }
        base.Dispose(disposing);
    }
}
