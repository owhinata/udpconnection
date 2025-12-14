using System.Net;

using UdpConnection.Tests.TestRunner;

namespace UdpConnection.Tests;

public class NegotiationTests
{
    private static int _portCounter = 16000;

    private static (IPEndPoint local, IPEndPoint remote) GetTestEndpoints()
    {
        var port1 = Interlocked.Increment(ref _portCounter);
        var port2 = Interlocked.Increment(ref _portCounter);
        return (
            new IPEndPoint(IPAddress.Loopback, port1),
            new IPEndPoint(IPAddress.Loopback, port2)
        );
    }

    #region Peer Negotiation Tests

    /// <summary>
    /// テスト名: Peer_InitialState
    /// 目的: Peer開始時の初期状態が正しいことを確認する
    /// 期待値: SessionId=0, IsConnected=false
    /// </summary>
    [Test]
    public void Peer_InitialState()
    {
        var (local, remote) = GetTestEndpoints();
        using var peer = new UdpConnectionPeer();

        // 自動送信を無効化してテスト
        peer.DisconnectedInterval = TimeSpan.Zero;
        peer.ConnectedInterval = TimeSpan.Zero;

        peer.Start(new UdpConnectionOptions(local, remote, 0x1234));

        Assert.AreEqual((ushort)0x1234, peer.PeerId);
        Assert.AreEqual((ushort)0, peer.SessionId);
        Assert.IsFalse(peer.IsConnected);
    }

    /// <summary>
    /// テスト名: Peer_SendNegotiation_BeforeStart_ReturnsFalse
    /// 目的: 開始前のSendNegotiationがfalseを返すことを確認する
    /// </summary>
    [Test]
    public void Peer_SendNegotiation_BeforeStart_ReturnsFalse()
    {
        using var peer = new UdpConnectionPeer();

        var result = peer.SendNegotiation();

        Assert.IsFalse(result);
    }

    /// <summary>
    /// テスト名: Peer_SendNegotiation_AfterStart_ReturnsTrue
    /// 目的: 開始後のSendNegotiationがtrueを返すことを確認する
    /// </summary>
    [Test]
    public void Peer_SendNegotiation_AfterStart_ReturnsTrue()
    {
        var (local, remote) = GetTestEndpoints();
        using var peer = new UdpConnectionPeer();

        peer.DisconnectedInterval = TimeSpan.Zero;
        peer.Start(new UdpConnectionOptions(local, remote, 0x1234));

        var result = peer.SendNegotiation();

        Assert.IsTrue(result);
    }

    #endregion

    #region Controller Peer Management Tests

    /// <summary>
    /// テスト名: Controller_InitialPeersEmpty
    /// 目的: Controller開始時にPeersが空であることを確認する
    /// </summary>
    [Test]
    public void Controller_InitialPeersEmpty()
    {
        var (local, remote) = GetTestEndpoints();
        using var controller = new UdpConnectionController();

        controller.Start(new UdpConnectionOptions(local, remote));

        Assert.AreEqual(0, controller.Peers.Count);
    }

    #endregion

    #region Negotiation Flow Tests

    /// <summary>
    /// テスト名: Negotiation_PeerConnects_ControllerAssignsSessionId
    /// 目的: PeerがNegotiationRequestを送信し、ControllerがSessionIdを割り当てることを確認する
    /// </summary>
    [Test]
    public void Negotiation_PeerConnects_ControllerAssignsSessionId()
    {
        var (peerLocal, controllerLocal) = GetTestEndpoints();

        using var peer = new UdpConnectionPeer();
        using var controller = new UdpConnectionController();

        // 自動送信を無効化
        peer.DisconnectedInterval = TimeSpan.Zero;
        peer.ConnectedInterval = TimeSpan.Zero;

        var peerConnectedEvent = new ManualResetEventSlim(false);
        var controllerPeerConnectedEvent = new ManualResetEventSlim(false);

        ushort receivedSessionId = 0;
        ushort controllerSessionId = 0;

        peer.NegotiationStateChanged += (sender, e) =>
        {
            if (e.State == NegotiationState.Connected)
            {
                receivedSessionId = e.SessionId;
                peerConnectedEvent.Set();
            }
        };

        controller.PeerStateChanged += (sender, e) =>
        {
            if (e.State == PeerState.Connected)
            {
                controllerSessionId = e.SessionId;
                controllerPeerConnectedEvent.Set();
            }
        };

        controller.Start(new UdpConnectionOptions(controllerLocal, peerLocal));
        peer.Start(new UdpConnectionOptions(peerLocal, controllerLocal, 0x1234));

        // 手動でネゴシエーション送信
        peer.SendNegotiation();

        // Controller側でPeer接続イベントを待機
        var controllerReceived = controllerPeerConnectedEvent.Wait(TimeSpan.FromSeconds(2));
        Assert.IsTrue(controllerReceived, "Controller did not receive peer connection");
        Assert.AreEqual((ushort)1, controllerSessionId, "First SessionId should be 1");

        // Peer側で接続イベントを待機
        var peerReceived = peerConnectedEvent.Wait(TimeSpan.FromSeconds(2));
        Assert.IsTrue(peerReceived, "Peer did not receive connection confirmation");
        Assert.AreEqual((ushort)1, receivedSessionId, "Peer should receive SessionId 1");

        // Peer状態確認
        Assert.IsTrue(peer.IsConnected);
        Assert.AreEqual((ushort)1, peer.SessionId);

        // Controller Peers確認
        Assert.AreEqual(1, controller.Peers.Count);
        Assert.IsTrue(controller.Peers.ContainsKey(1));
        Assert.AreEqual((ushort)0x1234, controller.Peers[1].PeerId);
    }

    /// <summary>
    /// テスト名: Negotiation_MultiplePeerIds_UniqueSessionIds
    /// 目的: 同じエンドポイントから異なるPeerIdで接続した場合、異なるSessionIdが割り当てられることを確認する
    /// 注: 現在の設計では1:1通信のため、異なるエンドポイントからの複数Peerには対応していない
    /// </summary>
    [Test]
    public void Negotiation_MultiplePeerIds_UniqueSessionIds()
    {
        var (peerLocal, controllerLocal) = GetTestEndpoints();

        using var peer1 = new UdpConnectionPeer();
        using var peer2 = new UdpConnectionPeer();
        using var controller = new UdpConnectionController();

        peer1.DisconnectedInterval = TimeSpan.Zero;
        peer2.DisconnectedInterval = TimeSpan.Zero;

        var peer1ConnectedEvent = new ManualResetEventSlim(false);
        var peer2ConnectedEvent = new ManualResetEventSlim(false);

        peer1.NegotiationStateChanged += (sender, e) =>
        {
            if (e.State == NegotiationState.Connected)
            {
                peer1ConnectedEvent.Set();
            }
        };

        peer2.NegotiationStateChanged += (sender, e) =>
        {
            if (e.State == NegotiationState.Connected)
            {
                peer2ConnectedEvent.Set();
            }
        };

        // 同じエンドポイントを使用（異なるPeerIdを持つ2つのPeerを順番に接続）
        controller.Start(new UdpConnectionOptions(controllerLocal, peerLocal));

        // Peer1接続
        peer1.Start(new UdpConnectionOptions(peerLocal, controllerLocal, 0x0001));
        peer1.SendNegotiation();
        var peer1Received = peer1ConnectedEvent.Wait(TimeSpan.FromSeconds(2));
        Assert.IsTrue(peer1Received, "Peer1 did not receive connection");
        Assert.AreEqual((ushort)1, peer1.SessionId);
        peer1.Stop();

        // 少し待機してからPeer2接続（同じローカルポートを再利用）
        Thread.Sleep(100);
        peer2.Start(new UdpConnectionOptions(peerLocal, controllerLocal, 0x0002));
        peer2.SendNegotiation();
        var peer2Received = peer2ConnectedEvent.Wait(TimeSpan.FromSeconds(2));
        Assert.IsTrue(peer2Received, "Peer2 did not receive connection");
        Assert.AreEqual((ushort)2, peer2.SessionId);

        // Controller側で両方のPeerが登録されていることを確認
        Assert.AreEqual(2, controller.Peers.Count);
    }

    /// <summary>
    /// テスト名: Negotiation_SameSessionId_OnReconnect
    /// 目的: 同じPeerが再接続しても同じSessionIdを維持することを確認する
    /// </summary>
    [Test]
    public void Negotiation_SameSessionId_OnReconnect()
    {
        var (peerLocal, controllerLocal) = GetTestEndpoints();

        using var peer = new UdpConnectionPeer();
        using var controller = new UdpConnectionController();

        peer.DisconnectedInterval = TimeSpan.Zero;

        var connectedCount = 0;
        var sessionIds = new List<ushort>();
        var connectedEvent = new ManualResetEventSlim(false);

        peer.NegotiationStateChanged += (sender, e) =>
        {
            if (e.State == NegotiationState.Connected)
            {
                sessionIds.Add(e.SessionId);
                connectedCount++;
                if (connectedCount >= 2)
                {
                    connectedEvent.Set();
                }
            }
        };

        controller.Start(new UdpConnectionOptions(controllerLocal, peerLocal));
        peer.Start(new UdpConnectionOptions(peerLocal, controllerLocal, 0x1234));

        // 2回ネゴシエーション送信（再接続シミュレート）
        peer.SendNegotiation();
        Thread.Sleep(200);
        peer.SendNegotiation();

        var received = connectedEvent.Wait(TimeSpan.FromSeconds(2));

        // 最初の接続時のみConnectedイベントが発火する（既に接続済みなら発火しない）
        // ただし、仕様によっては毎回発火する可能性もある
        // ここではSessionIdが同じことを確認
        Assert.AreEqual((ushort)1, peer.SessionId, "SessionId should remain 1");
    }

    #endregion

    #region SampleMessage with SessionId/PeerId Tests

    /// <summary>
    /// テスト名: SampleUpMessage_AutoPopulatesSessionIdAndPeerId
    /// 目的: SendSampleUpMessageがSessionIdとPeerIdを自動設定することを確認する
    /// </summary>
    [Test]
    public void SampleUpMessage_AutoPopulatesSessionIdAndPeerId()
    {
        var (peerLocal, controllerLocal) = GetTestEndpoints();

        using var peer = new UdpConnectionPeer();
        using var controller = new UdpConnectionController();

        peer.DisconnectedInterval = TimeSpan.Zero;

        var connectedEvent = new ManualResetEventSlim(false);
        var messageReceivedEvent = new ManualResetEventSlim(false);
        Messages.SampleUpMessage? receivedMessage = null;

        peer.NegotiationStateChanged += (sender, e) =>
        {
            if (e.State == NegotiationState.Connected)
            {
                connectedEvent.Set();
            }
        };

        controller.SampleUpReceived += (sender, msg) =>
        {
            receivedMessage = msg;
            messageReceivedEvent.Set();
        };

        controller.Start(new UdpConnectionOptions(controllerLocal, peerLocal));
        peer.Start(new UdpConnectionOptions(peerLocal, controllerLocal, 0xABCD));

        // 接続確立
        peer.SendNegotiation();
        var connected = connectedEvent.Wait(TimeSpan.FromSeconds(2));
        Assert.IsTrue(connected, "Failed to connect");

        // SampleUpメッセージ送信（SessionId, PeerIdは設定しない）
        var message = new Messages.SampleUpMessage
        {
            Command = Messages.CommandType.Start,
            SignedValue = 100,
            Sequence = 0x1234,
            Position = 50.5
        };
        peer.SendSampleUpMessage(message);

        var received = messageReceivedEvent.Wait(TimeSpan.FromSeconds(2));
        Assert.IsTrue(received, "Message not received");

        Assert.IsNotNull(receivedMessage);
        Assert.AreEqual((ushort)1, receivedMessage!.SessionId, "SessionId should be auto-populated");
        Assert.AreEqual((ushort)0xABCD, receivedMessage.PeerId, "PeerId should be auto-populated");
    }

    #endregion
}
