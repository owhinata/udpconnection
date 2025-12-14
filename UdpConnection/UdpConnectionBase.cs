using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

using UdpConnection.Logging;
using UdpConnection.Messages;
using UdpConnection.Protocol;
using UdpConnection.Serialization;

namespace UdpConnection;

/// <summary>
/// UDP接続の抽象基底クラス
/// </summary>
public abstract class UdpConnectionBase : IDisposable
{
    private readonly ILogger? _logger;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoop;
    private Task? _sendLoop;
    private Channel<byte[]>? _sendChannel;
    private IPEndPoint? _remoteEndPoint;
    private bool _disposed;
    private readonly object _lock = new();

    protected UdpConnectionBase(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 送受信を開始する
    /// </summary>
    /// <param name="options">接続オプション</param>
    /// <returns>成功した場合はtrue、既に開始済みまたは失敗した場合はfalse</returns>
    public bool Start(UdpConnectionOptions options)
    {
        return StartAsync(options, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 送受信を停止する
    /// </summary>
    /// <returns>成功した場合はtrue、既に停止済みの場合はfalse</returns>
    public bool Stop()
    {
        return StopAsync(CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 受信データを処理する（派生クラスで実装）
    /// </summary>
    protected abstract void ProcessReceivedData(byte[] data);

    /// <summary>
    /// メッセージを送信する
    /// </summary>
    protected bool SendMessage<T>(MessageType type, T message)
        where T : IMessage
    {
        return SendMessageAsync(type, message, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    private Task<bool> StartAsync(UdpConnectionOptions options, CancellationToken ct)
    {
        lock (_lock)
        {
            if (_udpClient != null)
            {
                return Task.FromResult(false); // 既に開始済み
            }

            try
            {
                _udpClient = new UdpClient(options.LocalEndPoint);
                _remoteEndPoint = options.RemoteEndPoint;

                _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                _sendChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(options.SendQueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false
                });

                _receiveLoop = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
                _sendLoop = Task.Run(() => SendLoopAsync(_cts.Token), _cts.Token);

                return Task.FromResult(true);
            }
            catch
            {
                _udpClient?.Dispose();
                _udpClient = null;
                _cts?.Dispose();
                _cts = null;
                _sendChannel = null;
                return Task.FromResult(false);
            }
        }
    }

    private async Task<bool> StopAsync(CancellationToken ct)
    {
        CancellationTokenSource? cts;
        Task? receiveLoop;
        Task? sendLoop;
        UdpClient? udpClient;
        Channel<byte[]>? sendChannel;

        lock (_lock)
        {
            if (_udpClient == null)
            {
                return false; // 既に停止済み
            }

            cts = _cts;
            receiveLoop = _receiveLoop;
            sendLoop = _sendLoop;
            udpClient = _udpClient;
            sendChannel = _sendChannel;

            _cts = null;
            _receiveLoop = null;
            _sendLoop = null;
            _udpClient = null;
            _sendChannel = null;
            _remoteEndPoint = null;
        }

        // キャンセルしてループを停止
        cts?.Cancel();

        // 送信チャネルを完了させる
        sendChannel?.Writer.TryComplete();

        // ループの終了を待機
        if (receiveLoop != null)
        {
            try
            {
                await receiveLoop.ConfigureAwait(false);
            }
            catch
            {
                // キャンセルやその他の例外を無視
            }
        }

        if (sendLoop != null)
        {
            try
            {
                await sendLoop.ConfigureAwait(false);
            }
            catch
            {
                // キャンセルやその他の例外を無視
            }
        }

        udpClient?.Dispose();
        cts?.Dispose();

        return true;
    }

    private async Task<bool> SendMessageAsync<T>(MessageType type, T message, CancellationToken ct)
        where T : IMessage
    {
        var channel = _sendChannel;
        var cts = _cts;
        if (channel == null)
        {
            return false;
        }

        var packet = SerializeMessage(type, message);

        try
        {
            using var linkedCts = cts != null
                ? CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct)
                : CancellationTokenSource.CreateLinkedTokenSource(ct);

            await channel.Writer.WriteAsync(packet, linkedCts.Token).ConfigureAwait(false);

            LogSend(type, message, packet);

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (ChannelClosedException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var udpClient = _udpClient;
                if (udpClient == null)
                {
                    break;
                }

                var result = await udpClient.ReceiveAsync(ct).ConfigureAwait(false);
                ProcessReceivedData(result.Buffer);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                // ソケットエラーは無視して継続
            }
        }
    }

    private async Task SendLoopAsync(CancellationToken ct)
    {
        var channel = _sendChannel;
        if (channel == null)
        {
            return;
        }

        try
        {
            await foreach (var packet in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                var udpClient = _udpClient;
                var remoteEndPoint = _remoteEndPoint;
                if (udpClient == null || remoteEndPoint == null)
                {
                    break;
                }

                try
                {
                    await udpClient.SendAsync(packet, packet.Length, remoteEndPoint).ConfigureAwait(false);
                }
                catch (SocketException)
                {
                    // ソケットエラーは無視して継続
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常なキャンセル
        }
        catch (ChannelClosedException)
        {
            // チャネルが閉じられた
        }
    }

    /// <summary>
    /// 受信ログを出力する（派生クラスから呼び出し）
    /// </summary>
    protected void LogReceive<T>(MessageType type, T message, byte[] packet)
        where T : IMessage
    {
        if (_logger == null)
        {
            return;
        }

        var msgLine = $"[Recv] {(int)type,2}: {message.ToLogString()}";
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogD($"{msgLine}\n{ToHexString(packet)}");
        }
        else
        {
            _logger.LogI(msgLine);
        }
    }

    private void LogSend<T>(MessageType type, T message, byte[] packet)
        where T : IMessage
    {
        if (_logger == null)
        {
            return;
        }

        var msgLine = $"[Send] {(int)type,2}: {message.ToLogString()}";
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogD($"{msgLine}\n{ToHexString(packet)}");
        }
        else
        {
            _logger.LogI(msgLine);
        }
    }

    private static string ToHexString(byte[] data)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < data.Length; i += 16)
        {
            if (i > 0)
            {
                sb.AppendLine();
            }
            sb.Append($"  {i:X4}:");
            for (int j = 0; j < 16 && i + j < data.Length; j++)
            {
                if (j == 8)
                {
                    sb.Append(' '); // middle separator
                }
                sb.Append($" {data[i + j]:X2}");
            }
        }
        return sb.ToString();
    }

    private static byte[] SerializeMessage<T>(MessageType type, T message)
        where T : IMessage
    {
        var writer = new BitWriter();

        // ヘッダー
        var header = new MessageHeader(type, (ushort)message.PayloadSize);
        header.WriteTo(writer);

        // ペイロード
        message.WriteTo(writer);

        return writer.ToArray();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Stop();
        }

        _disposed = true;
    }
}
