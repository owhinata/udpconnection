using System.Net;
using System.Text.Json;

using UdpConnection.Logging;
using UdpConnection.Messages;

namespace UdpConnection.SimpleCUI.Peer;

public class PeerApp
{
    private readonly ILogger _logger;
    private IUdpConnectionPeer? _peer;
    private IPEndPoint? _localEndPoint;
    private IPEndPoint? _remoteEndPoint;
    private bool _running = true;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PeerApp(LogLevel logLevel = LogLevel.Information)
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

        _peer?.Dispose();
        Console.WriteLine("Shutting down...");
    }

    private void PrintHeader()
    {
        Console.WriteLine("UDP Connection Peer");
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
        if (_peer != null)
        {
            Console.WriteLine("Connection already exists. Stop first.");
            return;
        }

        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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

        var options = new UdpConnectionOptions(localEp!, remoteEp!, peerId);
        var peer = new UdpConnectionPeer(_logger);
        peer.SampleDownReceived += OnSampleDownReceived;
        peer.NegotiationStateChanged += OnNegotiationStateChanged;

        if (peer.Start(options))
        {
            _peer = peer;
            _localEndPoint = localEp;
            _remoteEndPoint = remoteEp;
            Console.WriteLine($"Connection started (local={localEp}, remote={remoteEp}, peerId=0x{peerId:X4})");
        }
        else
        {
            peer.Dispose();
            Console.WriteLine("Failed to start connection");
        }
    }

    private void CommandStop()
    {
        if (_peer == null)
        {
            Console.WriteLine("No active connection");
            return;
        }

        if (_peer.Stop())
        {
            Console.WriteLine("Connection stopped");
        }

        _peer.Dispose();
        _peer = null;
        _localEndPoint = null;
        _remoteEndPoint = null;
    }

    private void CommandSend(string args)
    {
        if (_peer == null)
        {
            Console.WriteLine("No active connection. Start first.");
            return;
        }

        var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 1)
        {
            Console.WriteLine("Usage: send <type> [json]");
            PrintSendExample();
            return;
        }

        if (!int.TryParse(parts[0], out var typeNum))
        {
            Console.WriteLine("Invalid message type");
            PrintSendExample();
            return;
        }

        var json = parts.Length > 1 ? parts[1] : "";

        try
        {
            bool success = false;

            switch (typeNum)
            {
                case 1: // NegotiationRequest
                    success = _peer.SendNegotiation();
                    break;

                case 3: // SampleUp
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
                    success = _peer.SendSampleUpMessage(upMsg);
                    break;

                default:
                    Console.WriteLine("Invalid message type. Use 1 (NegotiationRequest) or 3 (SampleUp)");
                    PrintSendExample();
                    return;
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
        Console.WriteLine("  1: NegotiationRequest (no JSON needed)");
        Console.WriteLine("  3: SampleUp - {\"Command\":1,\"SignedValue\":-100,\"Sequence\":1,\"Position\":123.456}");
        Console.WriteLine("     Command: None=0, Start=1, Stop=2, Reset=3, Query=4, Update=5");
        Console.WriteLine("     (SessionId, PeerId are auto-populated)");
    }

    private void CommandStatus()
    {
        var state = _peer != null ? "Started" : "Stopped";
        Console.WriteLine($"Mode: Peer, State: {state}");

        if (_localEndPoint != null && _remoteEndPoint != null)
        {
            Console.WriteLine($"Local: {_localEndPoint}, Remote: {_remoteEndPoint}");
        }

        if (_peer != null)
        {
            Console.WriteLine($"PeerId: 0x{_peer.PeerId:X4}, SessionId: 0x{_peer.SessionId:X4}, Connected: {_peer.IsConnected}");
        }
    }

    private void CommandHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  start <local> <remote> <peerId> - Start connection");
        Console.WriteLine("    Example: start :5000 :5001 0x1234");
        Console.WriteLine("  stop     - Stop connection");
        Console.WriteLine("  send <type> [json] - Send message (type: 1=NegotiationRequest, 3=SampleUp)");
        Console.WriteLine("  status   - Show connection status");
        Console.WriteLine("  help     - Show this help");
        Console.WriteLine("  quit     - Exit application");
    }

    private void OnSampleDownReceived(object? sender, SampleDownMessage msg)
    {
        // ログ出力はライブラリ内で行われる
    }

    private void OnNegotiationStateChanged(object? sender, NegotiationStateChangedEventArgs e)
    {
        // ログ出力はライブラリ内で行われる
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
