using System.Net;
using System.Text.Json;

using UdpConnection.Logging;
using UdpConnection.Messages;

namespace UdpConnection.SimpleCUI;

public enum AppMode
{
    Peer,
    Controller
}

public class AppOptions
{
    public AppMode Mode { get; set; }
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}

public class App
{
    private readonly AppOptions _options;
    private readonly ILogger _logger;
    private UdpConnectionBase? _connection;
    private IPEndPoint? _localEndPoint;
    private IPEndPoint? _remoteEndPoint;
    private ushort _peerId;
    private bool _running = true;

    public App(AppOptions options)
    {
        _options = options;
        _logger = new ConsoleLogger(options.LogLevel);
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

        _connection?.Dispose();
        Console.WriteLine("Shutting down...");
    }

    private void PrintHeader()
    {
        var mode = _options.Mode == AppMode.Peer ? "Peer" : "Controller";
        Console.WriteLine($"Starting {mode} mode...");
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
        if (_connection != null)
        {
            Console.WriteLine("Connection already exists. Stop first.");
            return;
        }

        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (_options.Mode == AppMode.Peer)
        {
            // Peer: start <local> <remote> <peerId>
            if (parts.Length != 3)
            {
                Console.WriteLine("Usage: start <local> <remote> <peerId>");
                Console.WriteLine("  Format: [ip]:port (e.g., :5000 or 127.0.0.1:5000)");
                Console.WriteLine("  PeerId: 0-65535 (hex: 0x1234)");
                return;
            }

            if (!TryParsePeerId(parts[2], out var peerId))
            {
                Console.WriteLine($"Invalid peerId: {parts[2]}");
                return;
            }
            _peerId = peerId;

            if (!TryParseEndPoint(parts[0], true, out var localEp))
            {
                Console.WriteLine($"Invalid local endpoint: {parts[0]}");
                return;
            }

            if (!TryParseEndPoint(parts[1], false, out var remoteEp))
            {
                Console.WriteLine($"Invalid remote endpoint: {parts[1]}");
                return;
            }

            var options = new UdpConnectionOptions(localEp!, remoteEp!, _peerId);
            var peer = new UdpConnectionPeer(_logger);
            peer.SampleDownReceived += OnSampleDownReceived;
            peer.NegotiationStateChanged += OnNegotiationStateChanged;
            _connection = peer;

            if (_connection.Start(options))
            {
                _localEndPoint = localEp;
                _remoteEndPoint = remoteEp;
                Console.WriteLine($"Connection started (local={localEp}, remote={remoteEp}, peerId=0x{_peerId:X4})");
            }
            else
            {
                Console.WriteLine("Failed to start connection");
                _connection = null;
            }
        }
        else
        {
            // Controller: start <local>
            if (parts.Length != 1)
            {
                Console.WriteLine("Usage: start <local>");
                Console.WriteLine("  Format: [ip]:port (e.g., :5000 or 127.0.0.1:5000)");
                return;
            }

            if (!TryParseEndPoint(parts[0], true, out var localEp))
            {
                Console.WriteLine($"Invalid local endpoint: {parts[0]}");
                return;
            }

            var options = new UdpConnectionOptions(localEp!);
            var controller = new UdpConnectionController(_logger);
            controller.SampleUpReceived += OnSampleUpReceived;
            controller.PeerStateChanged += OnPeerStateChanged;
            _connection = controller;

            if (_connection.Start(options))
            {
                _localEndPoint = localEp;
                _remoteEndPoint = null;
                Console.WriteLine($"Connection started (local={localEp})");
            }
            else
            {
                Console.WriteLine("Failed to start connection");
                _connection = null;
            }
        }
    }

    private static bool TryParsePeerId(string s, out ushort peerId)
    {
        peerId = 0;

        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return ushort.TryParse(s.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out peerId);
        }

        return ushort.TryParse(s, out peerId);
    }

    private static bool TryParseEndPoint(string s, bool isLocal, out IPEndPoint? endPoint)
    {
        endPoint = null;

        // Format: [ip]:port or :port
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
            // IP省略時: local=ANY, remote=LOCALHOST
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

    private void CommandStop()
    {
        if (_connection == null)
        {
            Console.WriteLine("No active connection");
            return;
        }

        if (_connection.Stop())
        {
            Console.WriteLine("Connection stopped");
        }

        _connection.Dispose();
        _connection = null;
        _localEndPoint = null;
        _remoteEndPoint = null;
    }

    private void CommandSend(string args)
    {
        if (_connection == null)
        {
            Console.WriteLine("No active connection. Start first.");
            return;
        }

        var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        // Type 1 (NegotiationRequest) はJSONなしでOK
        if (parts.Length < 1)
        {
            Console.WriteLine("Usage: send <type> [json]");
            PrintSendExample();
            return;
        }

        if (!int.TryParse(parts[0], out var typeNum) || typeNum < 1 || typeNum > 4)
        {
            Console.WriteLine("Invalid message type. Use 1-4");
            PrintSendExample();
            return;
        }

        var json = parts.Length > 1 ? parts[1] : "";

        try
        {
            bool success = false;

            switch (typeNum)
            {
                case 1: // NegotiationRequest (Peer only)
                    if (_options.Mode != AppMode.Peer)
                    {
                        Console.WriteLine("NegotiationRequest can only be sent in Peer mode");
                        return;
                    }
                    success = ((UdpConnectionPeer)_connection).SendNegotiation();
                    break;

                case 2: // NegotiationResponse (Controller only, usually automatic)
                    Console.WriteLine("NegotiationResponse is sent automatically by Controller");
                    return;

                case 3: // SampleUp (Peer only)
                    if (_options.Mode != AppMode.Peer)
                    {
                        Console.WriteLine("SampleUp can only be sent in Peer mode");
                        return;
                    }
                    if (string.IsNullOrEmpty(json))
                    {
                        Console.WriteLine("SampleUp requires JSON payload");
                        PrintSendExample();
                        return;
                    }
                    var upMsg = JsonSerializer.Deserialize<SampleUpMessage>(json, JsonOptions);
                    if (upMsg == null)
                    {
                        Console.WriteLine("Failed to parse JSON");
                        return;
                    }
                    success = ((UdpConnectionPeer)_connection).SendSampleUpMessage(upMsg);
                    break;

                case 4: // SampleDown (Controller only)
                    if (_options.Mode != AppMode.Controller)
                    {
                        Console.WriteLine("SampleDown can only be sent in Controller mode");
                        return;
                    }
                    if (string.IsNullOrEmpty(json))
                    {
                        Console.WriteLine("SampleDown requires JSON payload");
                        PrintSendExample();
                        return;
                    }
                    var downMsg = JsonSerializer.Deserialize<SampleDownMessage>(json, JsonOptions);
                    if (downMsg == null)
                    {
                        Console.WriteLine("Failed to parse JSON");
                        return;
                    }
                    success = ((UdpConnectionController)_connection).SendSampleDownMessage(downMsg);
                    break;
            }

            if (!success)
            {
                Console.WriteLine("Failed to send message");
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
        Console.WriteLine("Message types:");
        Console.WriteLine("  1: NegotiationRequest (Peer, no JSON needed)");
        Console.WriteLine("  2: NegotiationResponse (Controller, automatic)");
        Console.WriteLine("  3: SampleUp   - {\"Command\":1,\"SignedValue\":-100,\"Sequence\":1,\"Position\":123.456}");
        Console.WriteLine("                  Command: None=0, Start=1, Stop=2, Reset=3, Query=4, Update=5");
        Console.WriteLine("                  (SessionId, PeerId are auto-populated)");
        Console.WriteLine("  4: SampleDown - {\"SessionId\":1,\"PeerId\":4660,\"Status\":2,\"SignedValue\":50,\"Timestamp\":1234,\"Velocity\":99.99}");
        Console.WriteLine("                  Status: Unknown=0, Ready=1, Running=2, Paused=3, Error=4, Complete=5");
    }

    private void CommandStatus()
    {
        var mode = _options.Mode == AppMode.Peer ? "Peer" : "Controller";
        var state = _connection != null ? "Started" : "Stopped";
        Console.WriteLine($"Mode: {mode}, State: {state}");

        if (_localEndPoint != null && _remoteEndPoint != null)
        {
            Console.WriteLine($"Local: {_localEndPoint}, Remote: {_remoteEndPoint}");
        }

        if (_connection != null)
        {
            if (_options.Mode == AppMode.Peer)
            {
                var peer = (UdpConnectionPeer)_connection;
                Console.WriteLine($"PeerId: 0x{peer.PeerId:X4}, SessionId: 0x{peer.SessionId:X4}, Connected: {peer.IsConnected}");
            }
            else
            {
                var controller = (UdpConnectionController)_connection;
                Console.WriteLine($"Connected Peers: {controller.Peers.Count}");
                foreach (var kvp in controller.Peers)
                {
                    Console.WriteLine($"  SessionId=0x{kvp.Key:X4}: PeerId=0x{kvp.Value.PeerId:X4}, LastNego={kvp.Value.LastNegotiationTime:HH:mm:ss}");
                }
            }
        }
    }

    private void CommandHelp()
    {
        Console.WriteLine("Available commands:");
        if (_options.Mode == AppMode.Peer)
        {
            Console.WriteLine("  start <local> <remote> <peerId> - Start connection (e.g., start :5000 :5001 0x1234)");
        }
        else
        {
            Console.WriteLine("  start <local> - Start listening (e.g., start :5001)");
        }
        Console.WriteLine("  stop     - Stop connection");
        Console.WriteLine("  send <type> [json] - Send message (type: 1=NegotiationRequest, 3=SampleUp, 4=SampleDown)");
        Console.WriteLine("  status   - Show connection status");
        Console.WriteLine("  help     - Show this help");
        Console.WriteLine("  quit     - Exit application");
    }

    private void OnSampleDownReceived(object? sender, SampleDownMessage msg)
    {
        // ログ出力はUdpConnectionBase内で行われる
    }

    private void OnSampleUpReceived(object? sender, SampleUpMessage msg)
    {
        // ログ出力はUdpConnectionBase内で行われる
    }

    private void OnNegotiationStateChanged(object? sender, NegotiationStateChangedEventArgs e)
    {
        // ログ出力はライブラリ内で行われる
    }

    private void OnPeerStateChanged(object? sender, PeerStateChangedEventArgs e)
    {
        // ログ出力はライブラリ内で行われる
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
