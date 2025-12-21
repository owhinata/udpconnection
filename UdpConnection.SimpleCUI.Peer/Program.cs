using UdpConnection.Logging;
using UdpConnection.SimpleCUI.Peer;

var logLevel = args.Contains("--debug") ? LogLevel.Debug : LogLevel.Information;

if (args.Contains("-h") || args.Contains("--help"))
{
    PrintUsage();
    return 0;
}

var app = new PeerApp(logLevel);
app.Run();
return 0;

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: UdpConnection.SimpleCUI.Peer [options]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options:");
    Console.Error.WriteLine("  --debug     Enable debug logging");
    Console.Error.WriteLine("  -h, --help  Show this help");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  start <local> <remote> <peerId> - Start connection");
    Console.Error.WriteLine("    Example: start :5000 :5001 0x1234");
    Console.Error.WriteLine("  stop     - Stop connection");
    Console.Error.WriteLine("  send 1   - Send NegotiationRequest");
    Console.Error.WriteLine("  send 3 <json> - Send SampleUp message");
    Console.Error.WriteLine("    Example: send 3 {\"Command\":1,\"SignedValue\":-100,\"Sequence\":1,\"Position\":123.456}");
    Console.Error.WriteLine("  status   - Show connection status");
    Console.Error.WriteLine("  help     - Show available commands");
    Console.Error.WriteLine("  quit     - Exit application");
}
