using System.Net;
using UdpConnection.Messages;
using UdpConnection.Tests.TestRunner;

namespace UdpConnection.Tests;

public class ConnectionTests
{
    private static int _portCounter = 15000;

    private static (IPEndPoint local, IPEndPoint remote) GetTestEndpoints()
    {
        var port1 = Interlocked.Increment(ref _portCounter);
        var port2 = Interlocked.Increment(ref _portCounter);
        return (
            new IPEndPoint(IPAddress.Loopback, port1),
            new IPEndPoint(IPAddress.Loopback, port2)
        );
    }

    #region Start/Stop Tests

    /// <summary>
    /// テスト名: Peer_Start_Success
    /// 目的: UdpConnectionPeerが正常に開始できることを確認する
    /// 手順: Start()を呼び出す
    /// 期待値: trueが返される
    /// </summary>
    [Test]
    public void Peer_Start_Success()
    {
        var (local, remote) = GetTestEndpoints();
        using var peer = new UdpConnectionPeer();

        var result = peer.Start(new UdpConnectionOptions(local, remote));

        Assert.IsTrue(result);
    }

    /// <summary>
    /// テスト名: Peer_Start_Twice_ReturnsFalse
    /// 目的: 二重Startがfalseを返すことを確認する
    /// 手順: Start()を2回呼び出す
    /// 期待値: 1回目はtrue、2回目はfalse
    /// </summary>
    [Test]
    public void Peer_Start_Twice_ReturnsFalse()
    {
        var (local, remote) = GetTestEndpoints();
        using var peer = new UdpConnectionPeer();

        var result1 = peer.Start(new UdpConnectionOptions(local, remote));
        var result2 = peer.Start(new UdpConnectionOptions(local, remote));

        Assert.IsTrue(result1);
        Assert.IsFalse(result2);
    }

    /// <summary>
    /// テスト名: Peer_Stop_Success
    /// 目的: UdpConnectionPeerが正常に停止できることを確認する
    /// 手順: Start()後にStop()を呼び出す
    /// 期待値: trueが返される
    /// </summary>
    [Test]
    public void Peer_Stop_Success()
    {
        var (local, remote) = GetTestEndpoints();
        using var peer = new UdpConnectionPeer();

        peer.Start(new UdpConnectionOptions(local, remote));
        var result = peer.Stop();

        Assert.IsTrue(result);
    }

    /// <summary>
    /// テスト名: Peer_Stop_WithoutStart_ReturnsFalse
    /// 目的: 開始前のStopがfalseを返すことを確認する
    /// 手順: Start()なしでStop()を呼び出す
    /// 期待値: falseが返される
    /// </summary>
    [Test]
    public void Peer_Stop_WithoutStart_ReturnsFalse()
    {
        using var peer = new UdpConnectionPeer();

        var result = peer.Stop();

        Assert.IsFalse(result);
    }

    /// <summary>
    /// テスト名: Peer_Stop_Twice_ReturnsFalse
    /// 目的: 二重Stopがfalseを返すことを確認する
    /// 手順: Stop()を2回呼び出す
    /// 期待値: 1回目はtrue、2回目はfalse
    /// </summary>
    [Test]
    public void Peer_Stop_Twice_ReturnsFalse()
    {
        var (local, remote) = GetTestEndpoints();
        using var peer = new UdpConnectionPeer();

        peer.Start(new UdpConnectionOptions(local, remote));
        var result1 = peer.Stop();
        var result2 = peer.Stop();

        Assert.IsTrue(result1);
        Assert.IsFalse(result2);
    }

    /// <summary>
    /// テスト名: Peer_Restart_Success
    /// 目的: Stop後に再Startできることを確認する
    /// 手順: Start() → Stop() → Start()
    /// 期待値: 全てtrueが返される
    /// </summary>
    [Test]
    public void Peer_Restart_Success()
    {
        var (local, remote) = GetTestEndpoints();
        using var peer = new UdpConnectionPeer();

        var result1 = peer.Start(new UdpConnectionOptions(local, remote));
        var result2 = peer.Stop();
        var result3 = peer.Start(new UdpConnectionOptions(local, remote));

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
        Assert.IsTrue(result3);

        peer.Stop();
    }

    /// <summary>
    /// テスト名: Controller_Start_Success
    /// 目的: UdpConnectionControllerが正常に開始できることを確認する
    /// 手順: Start()を呼び出す
    /// 期待値: trueが返される
    /// </summary>
    [Test]
    public void Controller_Start_Success()
    {
        var (local, remote) = GetTestEndpoints();
        using var controller = new UdpConnectionController();

        var result = controller.Start(new UdpConnectionOptions(local, remote));

        Assert.IsTrue(result);
    }

    #endregion

    #region SendMessage Tests

    /// <summary>
    /// テスト名: Peer_SendMessage_BeforeStart_ReturnsFalse
    /// 目的: 開始前の送信がfalseを返すことを確認する
    /// 手順: Start()なしでSendSampleUpMessage()を呼び出す
    /// 期待値: falseが返される
    /// </summary>
    [Test]
    public void Peer_SendMessage_BeforeStart_ReturnsFalse()
    {
        using var peer = new UdpConnectionPeer();

        var result = peer.SendSampleUpMessage(new SampleUpMessage());

        Assert.IsFalse(result);
    }

    /// <summary>
    /// テスト名: Peer_SendMessage_AfterStart_ReturnsTrue
    /// 目的: 開始後の送信がtrueを返すことを確認する
    /// 手順: Start()後にSendSampleUpMessage()を呼び出す
    /// 期待値: trueが返される
    /// </summary>
    [Test]
    public void Peer_SendMessage_AfterStart_ReturnsTrue()
    {
        var (local, remote) = GetTestEndpoints();
        using var peer = new UdpConnectionPeer();

        peer.Start(new UdpConnectionOptions(local, remote));
        var result = peer.SendSampleUpMessage(new SampleUpMessage());

        Assert.IsTrue(result);
    }

    /// <summary>
    /// テスト名: Peer_SendMessage_AfterStop_ReturnsFalse
    /// 目的: 停止後の送信がfalseを返すことを確認する
    /// 手順: Start() → Stop() → SendSampleUpMessage()
    /// 期待値: falseが返される
    /// </summary>
    [Test]
    public void Peer_SendMessage_AfterStop_ReturnsFalse()
    {
        var (local, remote) = GetTestEndpoints();
        using var peer = new UdpConnectionPeer();

        peer.Start(new UdpConnectionOptions(local, remote));
        peer.Stop();
        var result = peer.SendSampleUpMessage(new SampleUpMessage());

        Assert.IsFalse(result);
    }

    /// <summary>
    /// テスト名: Controller_SendMessage_AfterStart_ReturnsTrue
    /// 目的: Controller側で開始後の送信がtrueを返すことを確認する
    /// 手順: Start()後にSendSampleDownMessage()を呼び出す
    /// 期待値: trueが返される
    /// </summary>
    [Test]
    public void Controller_SendMessage_AfterStart_ReturnsTrue()
    {
        var (local, remote) = GetTestEndpoints();
        using var controller = new UdpConnectionController();

        controller.Start(new UdpConnectionOptions(local, remote));
        var result = controller.SendSampleDownMessage(new SampleDownMessage());

        Assert.IsTrue(result);
    }

    #endregion

    #region Communication Tests

    /// <summary>
    /// テスト名: Peer_To_Controller_Communication
    /// 目的: PeerからControllerへのメッセージ送受信が正常に動作することを確認する
    /// 手順: Peer→Controllerへメッセージを送信し、Controllerで受信を確認する
    /// 期待値: 送信したメッセージがControllerで受信される
    /// </summary>
    [Test]
    public void Peer_To_Controller_Communication()
    {
        var (peerLocal, controllerLocal) = GetTestEndpoints();

        using var peer = new UdpConnectionPeer();
        using var controller = new UdpConnectionController();

        SampleUpMessage? receivedMessage = null;
        var receivedEvent = new ManualResetEventSlim(false);

        controller.SampleUpReceived += (sender, msg) =>
        {
            receivedMessage = msg;
            receivedEvent.Set();
        };

        // Controller: peerLocalから受信、controllerLocalでリッスン
        controller.Start(new UdpConnectionOptions(controllerLocal, peerLocal));
        // Peer: peerLocalでリッスン、controllerLocalへ送信
        peer.Start(new UdpConnectionOptions(peerLocal, controllerLocal));

        var sentMessage = new SampleUpMessage
        {
            Command = CommandType.Update,
            SignedValue = -100,
            Sequence = 0x5678,
            Position = 456.789
        };

        peer.SendSampleUpMessage(sentMessage);

        // 受信を待機（最大1秒）
        var received = receivedEvent.Wait(TimeSpan.FromSeconds(1));

        Assert.IsTrue(received, "Message was not received within timeout");
        Assert.IsNotNull(receivedMessage);
        Assert.AreEqual(CommandType.Update, receivedMessage!.Command);
        Assert.AreEqual(-100, receivedMessage.SignedValue);
        Assert.AreEqual((ushort)0x5678, receivedMessage.Sequence);
        Assert.AreEqualWithTolerance(456.789, receivedMessage.Position, 0.0001);
    }

    /// <summary>
    /// テスト名: Controller_To_Peer_Communication
    /// 目的: ControllerからPeerへのメッセージ送受信が正常に動作することを確認する
    /// 手順: Controller→Peerへメッセージを送信し、Peerで受信を確認する
    /// 期待値: 送信したメッセージがPeerで受信される
    /// </summary>
    [Test]
    public void Controller_To_Peer_Communication()
    {
        var (peerLocal, controllerLocal) = GetTestEndpoints();

        using var peer = new UdpConnectionPeer();
        using var controller = new UdpConnectionController();

        SampleDownMessage? receivedMessage = null;
        var receivedEvent = new ManualResetEventSlim(false);

        peer.SampleDownReceived += (sender, msg) =>
        {
            receivedMessage = msg;
            receivedEvent.Set();
        };

        peer.Start(new UdpConnectionOptions(peerLocal, controllerLocal));
        controller.Start(new UdpConnectionOptions(controllerLocal, peerLocal));

        var sentMessage = new SampleDownMessage
        {
            Status = StatusType.Complete,
            SignedValue = 200,
            Timestamp = 0xDEAD,
            Velocity = -123.456
        };

        controller.SendSampleDownMessage(sentMessage);

        // 受信を待機（最大1秒）
        var received = receivedEvent.Wait(TimeSpan.FromSeconds(1));

        Assert.IsTrue(received, "Message was not received within timeout");
        Assert.IsNotNull(receivedMessage);
        Assert.AreEqual(StatusType.Complete, receivedMessage!.Status);
        Assert.AreEqual(200, receivedMessage.SignedValue);
        Assert.AreEqual((ushort)0xDEAD, receivedMessage.Timestamp);
        Assert.AreEqualWithTolerance(-123.456, receivedMessage.Velocity, 0.0001);
    }

    /// <summary>
    /// テスト名: MultipleMessages_OrderPreserved
    /// 目的: 複数メッセージが順序を保って送受信されることを確認する
    /// 手順: 3つのメッセージを連続送信し、受信順序を確認する
    /// 期待値: 送信順と同じ順序で受信される
    /// </summary>
    [Test]
    public void MultipleMessages_OrderPreserved()
    {
        var (peerLocal, controllerLocal) = GetTestEndpoints();

        using var peer = new UdpConnectionPeer();
        using var controller = new UdpConnectionController();

        var receivedSequences = new List<ushort>();
        var allReceivedEvent = new ManualResetEventSlim(false);

        controller.SampleUpReceived += (sender, msg) =>
        {
            lock (receivedSequences)
            {
                receivedSequences.Add(msg.Sequence);
                if (receivedSequences.Count >= 3)
                {
                    allReceivedEvent.Set();
                }
            }
        };

        controller.Start(new UdpConnectionOptions(controllerLocal, peerLocal));
        peer.Start(new UdpConnectionOptions(peerLocal, controllerLocal));

        // 3つのメッセージを順番に送信
        peer.SendSampleUpMessage(new SampleUpMessage { Sequence = 1 });
        peer.SendSampleUpMessage(new SampleUpMessage { Sequence = 2 });
        peer.SendSampleUpMessage(new SampleUpMessage { Sequence = 3 });

        // 受信を待機（最大2秒）
        var received = allReceivedEvent.Wait(TimeSpan.FromSeconds(2));

        Assert.IsTrue(received, "Not all messages were received within timeout");
        Assert.AreEqual(3, receivedSequences.Count);
        Assert.AreEqual((ushort)1, receivedSequences[0]);
        Assert.AreEqual((ushort)2, receivedSequences[1]);
        Assert.AreEqual((ushort)3, receivedSequences[2]);
    }

    #endregion
}
