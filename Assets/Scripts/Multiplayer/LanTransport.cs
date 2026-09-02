using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using KingdomsOfBharat.Multiplayer.Wire;

namespace KingdomsOfBharat.Multiplayer
{
    // Phase 5 LAN transport MVP: a deliberately minimal 2-peer TCP
    // connection - no matchmaking, no NAT traversal, no reconnect. TCP
    // (not UDP) because lockstep needs every message delivered exactly
    // once, in order, with nothing silently dropped - a LAN's extra
    // latency over UDP is irrelevant against CommandBus.InputDelayTicks'
    // 200ms budget. JSON (not a binary format) because MatchSaveData
    // already round-trips through JsonUtility for the F5/F9 quicksave
    // feature (see SaveManager.cs) - reusing that convention needs no new
    // serialization library for a LAN-only MVP where message size/parse
    // speed isn't a real constraint.
    //
    // Framing: a 4-byte little-endian length prefix followed by that many
    // UTF8 JSON bytes - the simplest framing that survives TCP's "stream,
    // not messages" delivery model without a 3rd-party library.
    //
    // Threading: connecting and receiving both happen on background
    // threads (TcpListener.AcceptTcpClient/NetworkStream.Read both block),
    // pushing fully-parsed NetMessageEnvelopes into a thread-safe queue.
    // Every Unity API touch (CommandBus, SimClock, gameplay factories)
    // stays on the main thread - see NetworkDriver.cs, which drains
    // Incoming once per Update().
    public class LanTransport
    {
        public const int DefaultPort = 7788;

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private volatile bool _running;
        private readonly object _writeLock = new object();
        private readonly ConcurrentQueue<NetMessageEnvelope> _incoming = new ConcurrentQueue<NetMessageEnvelope>();

        public bool IsConnected { get; private set; }
        public string LastError { get; private set; }

        public static LanTransport StartHost(int port = DefaultPort)
        {
            var transport = new LanTransport { _running = true };
            transport._listener = new TcpListener(IPAddress.Any, port);
            transport._listener.Start();

            var acceptThread = new Thread(() =>
            {
                try
                {
                    TcpClient client = transport._listener.AcceptTcpClient();
                    transport.Attach(client);
                }
                catch (Exception e)
                {
                    if (transport._running)
                    {
                        transport.LastError = e.Message;
                    }
                }
            })
            { IsBackground = true };
            acceptThread.Start();

            return transport;
        }

        public static LanTransport StartJoin(string hostAddress, int port = DefaultPort)
        {
            var transport = new LanTransport { _running = true };

            var connectThread = new Thread(() =>
            {
                try
                {
                    var client = new TcpClient();
                    client.Connect(hostAddress, port);
                    transport.Attach(client);
                }
                catch (Exception e)
                {
                    transport.LastError = e.Message;
                }
            })
            { IsBackground = true };
            connectThread.Start();

            return transport;
        }

        private void Attach(TcpClient client)
        {
            _client = client;
            _client.NoDelay = true;
            _stream = client.GetStream();
            IsConnected = true;

            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();
        }

        private void ReceiveLoop()
        {
            try
            {
                while (_running)
                {
                    byte[] lengthBytes = ReadExact(4);
                    if (lengthBytes == null)
                    {
                        break;
                    }

                    int length = BitConverter.ToInt32(lengthBytes, 0);
                    byte[] payload = ReadExact(length);
                    if (payload == null)
                    {
                        break;
                    }

                    string json = Encoding.UTF8.GetString(payload);
                    NetMessageEnvelope envelope = JsonUtility.FromJson<NetMessageEnvelope>(json);
                    _incoming.Enqueue(envelope);
                }
            }
            catch (Exception e)
            {
                if (_running)
                {
                    LastError = e.Message;
                }
            }
            finally
            {
                IsConnected = false;
            }
        }

        private byte[] ReadExact(int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = _stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                {
                    return null;
                }

                offset += read;
            }

            return buffer;
        }

        public void Send(NetMessageEnvelope envelope)
        {
            if (!IsConnected)
            {
                return;
            }

            string json = JsonUtility.ToJson(envelope);
            byte[] payload = Encoding.UTF8.GetBytes(json);
            byte[] length = BitConverter.GetBytes(payload.Length);

            try
            {
                lock (_writeLock)
                {
                    _stream.Write(length, 0, length.Length);
                    _stream.Write(payload, 0, payload.Length);
                }
            }
            catch (Exception e)
            {
                LastError = e.Message;
                IsConnected = false;
            }
        }

        public void SendHeartbeat(int tick)
        {
            Send(new NetMessageEnvelope { kind = NetMessageKind.Heartbeat, tick = tick });
        }

        public void SendStateHash(int tick, uint hash)
        {
            Send(new NetMessageEnvelope { kind = NetMessageKind.StateHash, tick = tick, hash = hash });
        }

        public void SendResyncSnapshot(int tick, string snapshotJson)
        {
            Send(new NetMessageEnvelope { kind = NetMessageKind.ResyncSnapshot, tick = tick, snapshotJson = snapshotJson });
        }

        public bool TryDequeue(out NetMessageEnvelope envelope)
        {
            return _incoming.TryDequeue(out envelope);
        }

        // Local IPv4 address to show the host user for the joining player
        // to type in - best-effort (first non-loopback IPv4 found), not a
        // full NIC-selection UI.
        public static string LocalIPv4()
        {
            try
            {
                foreach (IPAddress address in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                    {
                        return address.ToString();
                    }
                }
            }
            catch (Exception)
            {
                // Best-effort only - LanMatchMenu falls back to "localhost".
            }

            return "127.0.0.1";
        }

        public void Close()
        {
            _running = false;
            IsConnected = false;
            try { _stream?.Close(); } catch (Exception) { }
            try { _client?.Close(); } catch (Exception) { }
            try { _listener?.Stop(); } catch (Exception) { }
        }
    }
}
