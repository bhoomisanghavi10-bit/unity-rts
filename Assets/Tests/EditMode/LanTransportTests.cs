using System;
using System.Diagnostics;
using NUnit.Framework;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.Multiplayer.Wire;

namespace KingdomsOfBharat.Tests
{
    // Phase 5 LAN transport MVP: the closest thing to "real cross-peer"
    // achievable inside a single EditMode run - two real LanTransport
    // instances, two real OS TCP sockets, talking over 127.0.0.1 in the
    // same process. Proves the actual wire mechanics (connect, length-
    // prefixed JSON framing, background receive thread -> thread-safe
    // queue) work end to end, which no pure in-memory test of
    // CommandSerializer/NetworkId alone can prove. Live cross-machine
    // verification (two separate real processes) is done manually via
    // UnityMCP as part of this session's own live verification pass - see
    // docs/SESSION_LOG.md.
    public class LanTransportTests
    {
        private LanTransport _host;
        private LanTransport _client;

        [TearDown]
        public void TearDown()
        {
            _host?.Close();
            _client?.Close();
            _host = null;
            _client = null;
        }

        // Ports are reused across test runs on the same machine - a fixed
        // port could collide with a leftover TIME_WAIT socket from a
        // previous run. Offset from LanTransport.DefaultPort so this test
        // suite never collides with a real Host/Join session either.
        private static int NextTestPort()
        {
            return LanTransport.DefaultPort + 1000 + (Environment.TickCount % 1000);
        }

        private static bool WaitUntil(Func<bool> condition, int timeoutMs = 3000)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                if (condition())
                {
                    return true;
                }
                System.Threading.Thread.Sleep(5);
            }
            return condition();
        }

        [Test]
        public void HostAndJoin_ConnectOverLoopback()
        {
            int port = NextTestPort();
            _host = LanTransport.StartHost(port);
            _client = LanTransport.StartJoin("127.0.0.1", port);

            bool connected = WaitUntil(() => _host.IsConnected && _client.IsConnected);

            Assert.IsTrue(connected, $"Host and client should both report connected within the timeout. host={_host.IsConnected} client={_client.IsConnected} hostError={_host.LastError} clientError={_client.LastError}");
        }

        [Test]
        public void Send_DeliversAnEnvelopeToTheOtherPeer_WithFieldsIntact()
        {
            int port = NextTestPort();
            _host = LanTransport.StartHost(port);
            _client = LanTransport.StartJoin("127.0.0.1", port);
            Assert.IsTrue(WaitUntil(() => _host.IsConnected && _client.IsConnected), "Precondition: both peers must connect.");

            _host.Send(new NetMessageEnvelope
            {
                kind = NetMessageKind.StateHash,
                tick = 778,
                hash = 1000u,
            });

            NetMessageEnvelope envelope = default;
            bool dequeued = WaitUntil(() => _client.TryDequeue(out envelope));

            Assert.IsTrue(dequeued, "The client should receive the host's message over the real TCP loopback connection.");
            Assert.AreEqual(NetMessageKind.StateHash, envelope.kind);
            Assert.AreEqual(778, envelope.tick);
            Assert.AreEqual(1000u, envelope.hash);
        }

        [Test]
        public void Send_IsBidirectional()
        {
            int port = NextTestPort();
            _host = LanTransport.StartHost(port);
            _client = LanTransport.StartJoin("127.0.0.1", port);
            Assert.IsTrue(WaitUntil(() => _host.IsConnected && _client.IsConnected), "Precondition: both peers must connect.");

            _client.Send(new NetMessageEnvelope { kind = NetMessageKind.Heartbeat, tick = 42 });

            NetMessageEnvelope envelope = default;
            bool dequeued = WaitUntil(() => _host.TryDequeue(out envelope));

            Assert.IsTrue(dequeued, "The host should receive a message the client sent - the connection is a real bidirectional TCP stream, not host-to-client only.");
            Assert.AreEqual(NetMessageKind.Heartbeat, envelope.kind);
            Assert.AreEqual(42, envelope.tick);
        }

        [Test]
        public void Close_DisconnectsBothPeers()
        {
            int port = NextTestPort();
            _host = LanTransport.StartHost(port);
            _client = LanTransport.StartJoin("127.0.0.1", port);
            Assert.IsTrue(WaitUntil(() => _host.IsConnected && _client.IsConnected), "Precondition: both peers must connect.");

            _host.Close();

            Assert.IsTrue(WaitUntil(() => !_client.IsConnected),
                "Closing the host's side of the socket should surface as the client's connection dropping too.");
        }
    }
}
