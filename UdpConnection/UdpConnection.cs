using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using UdpConnection.Messages;
using UdpConnection.Protocol;
using UdpConnection.Serialization;

namespace UdpConnection;

/// <summary>
/// UDPでデータの送受信を行うクラス
/// </summary>
public class UdpConnection : IDisposable
{
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoop;
    private Task? _sendLoop;
    private Channel<byte[]>? _sendChannel;
    private IPEndPoint? _remoteEndPoint;
    private bool _disposed;
    private readonly object _lock = new();

    /// <summary>
    /// SampleDownメッセージ受信時に発火するイベント
    /// </summary>
    public event EventHandler<SampleDownMessage>? SampleDownReceived;

    /// <summary>
    /// 送受信を開始する
    /// </summary>
    /// <param name="options">接続オプション</param>
    /// <returns>成功した場合はtrue、既に開始済みまたは失敗した場合はfalse</returns>
    public bool Start(UdpConnectionOptions options)
    {
        return StartInternalAsync(options, CancellationToken.None)
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
        return StopInternalAsync(CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// SampleUpメッセージを送信する
    /// </summary>
    /// <param name="message">送信するメッセージ</param>
    public void SendSampleUp(SampleUpMessage message)
    {
        SendSampleUpAsync(message, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    private Task<bool> StartInternalAsync(UdpConnectionOptions options, CancellationToken ct)
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

    private async Task<bool> StopInternalAsync(CancellationToken ct)
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
            catch (OperationCanceledException)
            {
                // 正常なキャンセル
            }
        }

        if (sendLoop != null)
        {
            try
            {
                await sendLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 正常なキャンセル
            }
        }

        udpClient?.Dispose();
        cts?.Dispose();

        return true;
    }

    private async Task SendSampleUpAsync(SampleUpMessage message, CancellationToken ct)
    {
        var channel = _sendChannel;
        if (channel == null)
        {
            throw new InvalidOperationException("Connection is not started");
        }

        var packet = SerializeMessage(MessageType.SampleUp, message);

        using var linkedCts = _cts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct)
            : CancellationTokenSource.CreateLinkedTokenSource(ct);

        await channel.Writer.WriteAsync(packet, linkedCts.Token).ConfigureAwait(false);
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

    private void ProcessReceivedData(byte[] data)
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
            case MessageType.SampleDown:
                var sampleDownMessage = SampleDownMessage.ReadFrom(payloadReader);
                SampleDownReceived?.Invoke(this, sampleDownMessage);
                break;

            default:
                // 未知のメッセージタイプは無視
                break;
        }
    }

    private static byte[] SerializeMessage(MessageType type, SampleUpMessage message)
    {
        var writer = new BitWriter();

        // ヘッダー
        var header = new MessageHeader(type, SampleUpMessage.PayloadSize);
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
