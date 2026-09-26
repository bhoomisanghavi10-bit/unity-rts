using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer.Wire;

namespace KingdomsOfBharat.Multiplayer
{
    // Phase 5 LAN transport MVP: the only place a received network message
    // touches Unity/gameplay APIs. LanTransport's receive thread only ever
    // pushes plain-data NetMessageEnvelopes into a thread-safe queue (see
    // LanTransport.cs's own threading note) - this drains that queue once
    // per Update(), on the main thread, same self-installing convention as
    // SimClock/SaveManager.
    public class NetworkDriver : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<NetworkDriver>() != null)
            {
                return;
            }

            GameObject go = new GameObject("NetworkDriver");
            go.AddComponent<NetworkDriver>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            if (!NetworkMatch.IsActive || NetworkMatch.Transport == null)
            {
                return;
            }

            while (NetworkMatch.Transport.TryDequeue(out NetMessageEnvelope envelope))
            {
                Dispatch(envelope);
            }
        }

        // Sender binding + duplicate rejection first, then typed target
        // resolution; every refusal is counted and reported (never silent).
        internal static void HandleCommand(NetMessageEnvelope envelope)
        {
            if (!NetworkMatch.AcceptRemoteCommand(envelope))
            {
                return;
            }

            Command command = CommandSerializer.ToCommand(envelope);
            if (command == null)
            {
                NetworkDiagnostics.Report(NetworkIssue.UnresolvedTarget,
                    $"{envelope.kind} tick {envelope.tick} seq {envelope.seq}: unit {envelope.unitNetId}/{envelope.attackerNetId}, target {envelope.targetNetId} (kind {envelope.targetKind}), building {envelope.sourceBuildingNetId}, resource {envelope.resourceNetId} did not resolve");
                return;
            }

            command.Sequence = envelope.seq;
            CommandBus.EnqueueAt(envelope.tick, command);
        }

        internal static void HandleConfigCheck(NetMessageEnvelope envelope)
        {
            MatchConfiguration mine = MatchConfiguration.Current;
            if (mine == null || mine.ComputeHash() != envelope.configHash)
            {
                NetworkDiagnostics.Report(NetworkIssue.ConfigMismatch, $"local {mine?.ComputeHash()} vs remote {envelope.configHash}");
                NetworkMatch.RaiseFault($"Match configuration mismatch (local {mine?.ComputeHash()}, remote {envelope.configHash}); the two games are not the same match.");
            }
        }

        private void Dispatch(NetMessageEnvelope envelope)
        {
            // Every message type advances the "remote is still with us, at
            // least up to this tick" watermark SimClock's lockstep gate
            // reads - see NetworkMatch.OnRemoteTickSeen/SimClock.cs.
            NetworkMatch.OnRemoteTickSeen(envelope.tick);

            switch (envelope.kind)
            {
                case NetMessageKind.Move:
                case NetMessageKind.Train:
                case NetMessageKind.Build:
                case NetMessageKind.Attack:
                case NetMessageKind.TradeRoute:
                case NetMessageKind.Heal:
                case NetMessageKind.Convert:
                case NetMessageKind.Gather:
                    HandleCommand(envelope);
                    break;

                case NetMessageKind.ConfigCheck:
                    HandleConfigCheck(envelope);
                    break;

                case NetMessageKind.StateHash:
                    NetworkDesyncMonitor.OnRemoteHashReceived(envelope.tick, envelope.hash);
                    break;

                case NetMessageKind.ResyncSnapshot:
                    MatchSaveData snapshot = JsonUtility.FromJson<MatchSaveData>(envelope.snapshotJson);
                    DesyncRecovery.Apply(snapshot);
                    Debug.Log($"[NetworkDriver] Applied authoritative resync snapshot for tick {envelope.tick}.");
                    break;

                case NetMessageKind.Heartbeat:
                    // Nothing beyond the watermark bump above - its whole
                    // purpose is proving "the remote peer is still sending
                    // for this tick," not carrying a payload.
                    break;

                case NetMessageKind.HostHello:
                case NetMessageKind.JoinHello:
                    // Handled synchronously by LanMatchMenu's own polling
                    // during the pre-match handshake, not here - by the
                    // time NetworkMatch.IsActive is true (this method's own
                    // gate), the handshake is already complete.
                    break;
            }
        }
    }
}
