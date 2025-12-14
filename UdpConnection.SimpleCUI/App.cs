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
        if (parts.Length != 2)
        {
            Console.WriteLine("Usage: start <local> <remote>");
            Console.WriteLine("  Format: [ip]:port (e.g., :5000 or 127.0.0.1:5000)");
            Console.WriteLine("  If IP omitted: local=ANY, remote=LOCALHOST");
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

        var options = new UdpConnectionOptions(localEp!, remoteEp!);

        if (_options.Mode == AppMode.Peer)
        {
            var peer = new UdpConnectionPeer(_logger);
            peer.SampleDownReceived += OnSampleDownReceived;
            _connection = peer;
        }
        else
        {
            var controller = new UdpConnectionController(_logger);
            controller.SampleUpReceived += OnSampleUpReceived;
            _connection = controller;
        }

        if (_connection.Start(options))
        {
            _localEndPoint = localEp;
            _remoteEndPoint = remoteEp;
            Console.WriteLine($"Connection started (local={localEp}, remote={remoteEp})");
        }
        else
        {
            Console.WriteLine("Failed to start connection");
            _connection = null;
        }
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
        if (parts.Length != 2)
        {
            Console.WriteLine("Usage: send <type> <json>");
            PrintSendExample();
            return;
        }

        if (!int.TryParse(parts[0], out var typeNum) || typeNum < 1 || typeNum > 2)
        {
            Console.WriteLine("Invalid message type. Use 1 (SampleUp) or 2 (SampleDown)");
            return;
        }

        var json = parts[1];

        try
        {
            bool success;
            if (typeNum == 1)
            {
                var message = JsonSerializer.Deserialize<SampleUpMessage>(json, JsonOptions);
                if (message == null)
                {
                    Console.WriteLine("Failed to parse JSON");
                    return;
                }
                success = ((UdpConnectionPeer)_connection).SendSampleUpMessage(message);
            }
            else
            {
                var message = JsonSerializer.Deserialize<SampleDownMessage>(json, JsonOptions);
                if (message == null)
                {
                    Console.WriteLine("Failed to parse JSON");
                    return;
                }
                success = ((UdpConnectionController)_connection).SendSampleDownMessage(message);
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
        catch (InvalidCastException)
        {
            Console.WriteLine($"Cannot send message type {typeNum} in current mode");
        }
    }

    private void PrintSendExample()
    {
        Console.WriteLine("Message types:");
        Console.WriteLine("  1: SampleUp   - {\"Command\":1,\"SignedValue\":-100,\"Sequence\":1,\"Position\":123.456}");
        Console.WriteLine("                  Command: None=0, Start=1, Stop=2, Reset=3, Query=4, Update=5");
        Console.WriteLine("  2: SampleDown - {\"Status\":2,\"SignedValue\":50,\"Timestamp\":1234,\"Velocity\":99.99}");
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
    }

    private void CommandHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  start <local> <remote> - Start connection (e.g., start :5000 :5001)");
        Console.WriteLine("  stop     - Stop connection");
        Console.WriteLine("  send <type> <json> - Send message (type: 1=SampleUp, 2=SampleDown)");
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
