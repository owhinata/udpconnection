using UdpConnection.Logging;
using UdpConnection.SimpleCUI;

var options = ParseArgs(args);
if (options == null)
{
    return 1;
}

var app = new App(options);
app.Run();
return 0;

static AppOptions? ParseArgs(string[] args)
{
    var options = new AppOptions();
    bool modeSet = false;

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-p":
                options.Mode = AppMode.Peer;
                modeSet = true;
                break;
            case "-c":
                options.Mode = AppMode.Controller;
                modeSet = true;
                break;
            case "--debug":
                options.LogLevel = LogLevel.Debug;
                break;
            case "--help":
            case "-h":
                PrintUsage();
                return null;
            default:
                PrintUsage($"Unknown option: {args[i]}");
                return null;
        }
    }

    if (!modeSet)
    {
        PrintUsage("Mode (-p or -c) is required");
        return null;
    }

    return options;
}

static void PrintUsage(string? error = null)
{
    if (error != null)
    {
        Console.Error.WriteLine($"Error: {error}");
        Console.Error.WriteLine();
    }

    Console.Error.WriteLine("Usage: UdpConnection.SimpleCUI <options>");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options:");
    Console.Error.WriteLine("  -p          Run as Peer (sends SampleUp, receives SampleDown)");
    Console.Error.WriteLine("  -c          Run as Controller (sends SampleDown, receives SampleUp)");
    Console.Error.WriteLine("  --debug     Enable debug logging");
    Console.Error.WriteLine("  -h, --help  Show this help");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Examples:");
    Console.Error.WriteLine("  UdpConnection.SimpleCUI -p");
    Console.Error.WriteLine("  UdpConnection.SimpleCUI -c --debug");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Peer mode: start <local> <remote> <peerId>");
    Console.Error.WriteLine("  start :5000 :5001 0x1234       # local=0.0.0.0:5000, remote=127.0.0.1:5001, peerId=0x1234");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Controller mode: start <local>");
    Console.Error.WriteLine("  start :5001                    # local=0.0.0.0:5001 (accepts connections from any peer)");
}
