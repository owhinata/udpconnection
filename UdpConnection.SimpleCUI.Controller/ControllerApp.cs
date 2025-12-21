using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

using UdpConnection.Logging;
using UdpConnection.Messages;

namespace UdpConnection.SimpleCUI.Controller;

/// <summary>
/// Peer情報（アプリ層で管理）
/// </summary>
public class PeerInfo
{
    public ushort PeerId { get; }
    public IPEndPoint RemoteEndPoint { get; private set; }
    public DateTime LastNegotiationTime { get; set; }

    public PeerInfo(ushort peerId, IPEndPoint remoteEndPoint)
    {
        PeerId = peerId;
        RemoteEndPoint = remoteEndPoint;
        LastNegotiationTime = DateTime.UtcNow;
    }

    public void UpdateRemoteEndPoint(IPEndPoint newEndPoint)
    {
        RemoteEndPoint = newEndPoint;
    }
}

public class ControllerApp
{
    private readonly ILogger _logger;
    private IUdpConnectionController? _controller;
    private IPEndPoint? _localEndPoint;
    private bool _running = true;

    // Peer管理（アプリ層）
    private readonly ConcurrentDictionary<ushort, PeerInfo> _peers = new();
    private readonly ConcurrentDictionary<ushort, ushort> _peerIdToSessionId = new();
    private readonly object _sessionIdLock = new();
    private ushort _nextSessionId = 1;
    private Timer? _cleanupTimer;
    private TimeSpan _peerTimeout = TimeSpan.FromSeconds(180);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ControllerApp(LogLevel logLevel = LogLevel.Information)
    {
        _logger = new ConsoleLogger(logLevel);
    }

    public void Run()
    {
        PrintHeader();

        while (_running)
        {
            Console.Write("> ");
            Console.Out.Flush();

            var line = Console.ReadLine();
            if (line == null)
            {
                break;
            }

            ProcessCommand(line.Trim());
        }

        StopCleanupTimer();
        _controller?.Dispose();
        Console.WriteLine("Shutting down...");
    }

    private void PrintHeader()
    {
        Console.WriteLine("UDP Connection Controller");
        Console.WriteLine("Type 'help' for available commands.");
    }

    private void ProcessCommand(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts[1] : "";

        switch (command)
        {
            case "start":
                CommandStart(args);
                break;
            case "stop":
                CommandStop();
                break;
            case "send":
                CommandSend(args);
                break;
            case "status":
                CommandStatus();
                break;
            case "help":
                CommandHelp();
                break;
            case "quit":
            case "exit":
                _running = false;
                break;
            default:
                Console.WriteLine($"Unknown command: {command}");
                break;
        }
    }

    private void CommandStart(string args)
    {
        if (_controller != null)
        {
            Console.WriteLine("Controller already running. Stop first.");
            return;
        }

        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 1)
        {
            Console.WriteLine("Usage: start <local>");
            Console.WriteLine("  Format: [ip]:port (e.g., :5001 or 127.0.0.1:5001)");
            return;
        }

        if (!TryParseEndPoint(parts[0], true, out var localEp))
        {
            Console.WriteLine($"Invalid local endpoint: {parts[0]}");
            return;
        }

        var options = new UdpConnectionOptions(localEp!);
        var controller = new UdpConnectionController(_logger);
        controller.NegotiationRequestReceived += OnNegotiationRequestReceived;
        controller.SampleUpReceived += OnSampleUpReceived;

        if (controller.Start(options))
        {
            _controller = controller;
            _localEndPoint = localEp;
            StartCleanupTimer();
            Console.WriteLine($"Controller started (local={localEp})");
        }
        else
        {
            controller.Dispose();
            Console.WriteLine("Failed to start controller");
        }
    }

    private void CommandStop()
    {
        if (_controller == null)
        {
            Console.WriteLine("Controller not running");
            return;
        }

        StopCleanupTimer();

        if (_controller.Stop())
        {
            Console.WriteLine("Controller stopped");
        }

        _controller.Dispose();
        _controller = null;
        _localEndPoint = null;
        ClearPeers();
    }

    private void CommandSend(string args)
    {
        if (_controller == null)
        {
            Console.WriteLine("Controller not running. Start first.");
            return;
        }

        var parts = args.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            Console.WriteLine("Usage: send 4 <sessionId> <json>");
            PrintSendExample();
            return;
        }

        if (!int.TryParse(parts[0], out var typeNum) || typeNum != 4)
        {
            Console.WriteLine("Only message type 4 (SampleDown) is supported");
            PrintSendExample();
            return;
        }

        if (!TryParseSessionId(parts[1], out var sessionId))
        {
            Console.WriteLine($"Invalid sessionId: {parts[1]}");
            return;
        }

        var json = parts[2];

        try
        {
            var msg = JsonSerializer.Deserialize<SampleDownMessage>(json, JsonOptions);
            if (msg == null)
            {
                Console.WriteLine("Failed to parse JSON");
                return;
            }

            // SessionIdとPeerIdを設定
            msg.SessionId = sessionId;
            if (_peers.TryGetValue(sessionId, out var peerInfo))
            {
                msg.PeerId = peerInfo.PeerId;
                if (_controller.SendSampleDownMessage(msg, peerInfo.RemoteEndPoint))
                {
                    // 送信成功（ログはライブラリ内で出力）
                }
                else
                {
                    Console.WriteLine("Failed to send message");
                }
            }
            else
            {
                Console.WriteLine($"Unknown sessionId: 0x{sessionId:X4}");
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON parse error: {ex.Message}");
            PrintSendExample();
        }
    }

    private void PrintSendExample()
    {
        Console.WriteLine("Message type:");
        Console.WriteLine("  4: SampleDown - {\"Status\":2,\"SignedValue\":50,\"Timestamp\":1234,\"Velocity\":99.99}");
        Console.WriteLine("     Status: Unknown=0, Ready=1, Running=2, Paused=3, Error=4, Complete=5");
        Console.WriteLine("     (SessionId, PeerId are set from peer info)");
        Console.WriteLine();
        Console.WriteLine("Example: send 4 1 {\"Status\":2,\"SignedValue\":50,\"Timestamp\":1234,\"Velocity\":99.99}");
    }

    private void CommandStatus()
    {
        var state = _controller != null ? "Started" : "Stopped";
        Console.WriteLine($"Mode: Controller, State: {state}");

        if (_localEndPoint != null)
        {
            Console.WriteLine($"Local: {_localEndPoint}");
        }

        if (_controller != null)
        {
            Console.WriteLine($"Connected Peers: {_peers.Count}");
            foreach (var kvp in _peers)
            {
                Console.WriteLine($"  SessionId=0x{kvp.Key:X4}: PeerId=0x{kvp.Value.PeerId:X4}, Endpoint={kvp.Value.RemoteEndPoint}, LastNego={kvp.Value.LastNegotiationTime:HH:mm:ss}");
            }
        }
    }

    private void CommandHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  start <local> - Start listening (e.g., start :5001)");
        Console.WriteLine("  stop     - Stop listening");
        Console.WriteLine("  send 4 <sessionId> <json> - Send SampleDown message");
        Console.WriteLine("    Example: send 4 1 {\"Status\":2,\"SignedValue\":50,\"Timestamp\":1234,\"Velocity\":99.99}");
        Console.WriteLine("  status   - Show status and connected peers");
        Console.WriteLine("  help     - Show this help");
        Console.WriteLine("  quit     - Exit application");
    }

    private void OnNegotiationRequestReceived(object? sender, NegotiationRequestReceivedEventArgs e)
    {
        // アプリ層でSessionIdを決定
        ushort sessionId;
        bool isNewPeer = false;

        if (_peerIdToSessionId.TryGetValue(e.Request.PeerId, out sessionId))
        {
            // 既存Peer: タイムスタンプ更新、エンドポイント更新
            if (_peers.TryGetValue(sessionId, out var peerInfo))
            {
                peerInfo.LastNegotiationTime = DateTime.UtcNow;
                peerInfo.UpdateRemoteEndPoint(e.RemoteEndPoint);
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

            var newPeerInfo = new PeerInfo(e.Request.PeerId, e.RemoteEndPoint);
            _peers[sessionId] = newPeerInfo;
            _peerIdToSessionId[e.Request.PeerId] = sessionId;
            isNewPeer = true;
        }

        // イベント引数にSessionIdを設定
        e.ResponseSessionId = sessionId;

        // 新規Peer接続ログ
        if (isNewPeer)
        {
            _logger.LogI($"[Peer] State=Connected, PeerId=0x{e.Request.PeerId:X4}, SessionId=0x{sessionId:X4}, Endpoint={e.RemoteEndPoint}");
        }
    }

    private void OnSampleUpReceived(object? sender, SampleUpReceivedEventArgs e)
    {
        // ログ出力はライブラリ内で行われる
    }

    private void StartCleanupTimer()
    {
        StopCleanupTimer();
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
            if (now - kvp.Value.LastNegotiationTime > _peerTimeout)
            {
                expiredPeers.Add((kvp.Key, kvp.Value));
            }
        }

        foreach (var (sessionId, info) in expiredPeers)
        {
            if (_peers.TryRemove(sessionId, out _))
            {
                _peerIdToSessionId.TryRemove(info.PeerId, out _);
                _logger.LogI($"[Peer] State=Disconnected, PeerId=0x{info.PeerId:X4}, SessionId=0x{sessionId:X4}, Endpoint={info.RemoteEndPoint}");
            }
        }
    }

    private void ClearPeers()
    {
        _peers.Clear();
        _peerIdToSessionId.Clear();
        lock (_sessionIdLock)
        {
            _nextSessionId = 1;
        }
    }

    private static bool TryParseSessionId(string s, out ushort sessionId)
    {
        sessionId = 0;

        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return ushort.TryParse(s.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out sessionId);
        }

        return ushort.TryParse(s, out sessionId);
    }

    private static bool TryParseEndPoint(string s, bool isLocal, out IPEndPoint? endPoint)
    {
        endPoint = null;

        var colonIndex = s.LastIndexOf(':');
        if (colonIndex < 0)
        {
            return false;
        }

        var ipPart = s.Substring(0, colonIndex);
        var portPart = s.Substring(colonIndex + 1);

        if (!int.TryParse(portPart, out var port) || port < 0 || port > 65535)
        {
            return false;
        }

        IPAddress ip;
        if (string.IsNullOrEmpty(ipPart))
        {
            ip = isLocal ? IPAddress.Any : IPAddress.Loopback;
        }
        else
        {
            if (!IPAddress.TryParse(ipPart, out ip!))
            {
                return false;
            }
        }

        endPoint = new IPEndPoint(ip, port);
        return true;
    }
}
