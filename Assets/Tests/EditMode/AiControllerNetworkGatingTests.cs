using System;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.AI;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer;

namespace KingdomsOfBharat.Tests
{
    // Item 6 (Scenario Editor, heavy path session 5). A real, pre-existing
    // bug found live while scoping multiplayer scenario play: AiController
    // had zero reference to NetworkMatch anywhere, so in a real 2-human LAN
    // match the Enemy faction's AiController kept running its full AI logic
    // at the same time the joining human's own commands targeted that same
    // faction. Fixed with an early guard at the top of Start().
    //
    // Only the guard itself is covered here, not "normal Start() behavior
    // is unchanged when NetworkMatch is inactive" - that path spawns a real
    // TownCenter via TownCenterFactory.Place, which calls
    // SelectionIndicator.Configure() and NREs outside Play mode (the exact,
    // already-documented EditMode-only limitation EntitySpawnerTests.cs's
    // own class comment discloses for the same reason). That path is
    // covered by live UnityMCP Play-mode verification instead (see
    // docs/SESSION_LOG.md), not duplicated here.
    //
    // NetworkMatch.IsActive can only legitimately become true via
    // NetworkMatch.Begin, which needs a real LanTransport - reuses the same
    // real two-socket loopback technique LanTransportTests.cs already
    // establishes, rather than inventing a lighter-weight fake.
    public class AiControllerNetworkGatingTests
    {
        private LanTransport _host;
        private LanTransport _client;
        private GameObject _spawned;

        [TearDown]
        public void TearDown()
        {
            NetworkMatch.End();
            _host?.Close();
            _client?.Close();
            _host = null;
            _client = null;

            if (_spawned != null)
            {
                UnityEngine.Object.DestroyImmediate(_spawned);
                _spawned = null;
            }
        }

        private static int NextTestPort()
        {
            return LanTransport.DefaultPort + 3000 + (Environment.TickCount % 1000);
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
        public void Start_WhenNetworkMatchActiveAndFactionIsEnemy_DisablesItselfWithoutSpawning()
        {
            int port = NextTestPort();
            _host = LanTransport.StartHost(port);
            _client = LanTransport.StartJoin("127.0.0.1", port);
            Assert.IsTrue(WaitUntil(() => _host.IsConnected && _client.IsConnected), "Precondition: both peers must connect.");

            NetworkMatch.Begin(FactionId.Player, isHost: true, seed: 1, _host);

            _spawned = new GameObject("Enemy AiController Under Test");
            AiController controller = _spawned.AddComponent<AiController>();

            MethodInfo startMethod = typeof(AiController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.DoesNotThrow(() => startMethod.Invoke(controller, null),
                "The guard must return before any spawn logic runs - if this throws, the guard isn't short-circuiting early enough.");

            Assert.IsFalse(controller.enabled,
                "AiController for FactionId.Enemy (the default) must disable itself while NetworkMatch.IsActive, since that faction is a real human in a network match, not an AI opponent.");
        }

        [Test]
        public void NetworkMatch_InactiveByDefault_ConfirmsTheGuardIsOffOutsideNetworkMatches()
        {
            // No NetworkMatch.Begin call in this test - confirms the guard's
            // own precondition (IsActive stays false for every local/
            // offline match) holds without any of this session's own setup,
            // matching the invariant every other Phase 5 rewiring already
            // relies on.
            Assert.IsFalse(NetworkMatch.IsActive);
        }
    }
}
