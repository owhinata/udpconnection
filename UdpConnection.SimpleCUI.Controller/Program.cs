using UdpConnection.Logging;
using UdpConnection.SimpleCUI.Controller;

var logLevel = args.Contains("--debug") ? LogLevel.Debug : LogLevel.Information;

if (args.Contains("-h") || args.Contains("--help"))
{
    PrintUsage();
    return 0;
}

var app = new ControllerApp(logLevel);
app.Run();
return 0;

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: UdpConnection.SimpleCUI.Controller [options]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options:");
    Console.Error.WriteLine("  --debug     Enable debug logging");
    Console.Error.WriteLine("  -h, --help  Show this help");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  start <local> - Start listening (e.g., start :5001)");
    Console.Error.WriteLine("  stop     - Stop listening");
    Console.Error.WriteLine("  send 4 <sessionId> <json> - Send SampleDown message");
    Console.Error.WriteLine("    Example: send 4 1 {\"Status\":2,\"SignedValue\":50,\"Timestamp\":1234,\"Velocity\":99.99}");
    Console.Error.WriteLine("  status   - Show connection status and connected peers");
    Console.Error.WriteLine("  help     - Show available commands");
    Console.Error.WriteLine("  quit     - Exit application");
}
