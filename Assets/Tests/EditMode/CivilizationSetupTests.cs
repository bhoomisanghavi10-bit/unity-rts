using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // Covers the AoE-parity Phase 1 duplicate-civilization guard:
    // CivilizationSetup.ResolveDistinctCivilization is the pure decision
    // logic behind ensuring Player/Enemy/Enemy2 never silently collide on
    // the same civilization (which today makes two factions render as the
    // same body tint, since civ identity is the only tint that exists) -
    // extracted specifically so it's testable without a full BeginMatch
    // scene/CivPicker flow, same pattern HoverCursorStateTests uses.
    public class CivilizationSetupTests
    {
        [Test]
        public void DesiredCivilization_NotTaken_ReturnsDesiredUnchanged()
        {
            var taken = new HashSet<CivilizationId> { CivilizationId.Chola };

            CivilizationId result = CivilizationSetup.ResolveDistinctCivilization(
                CivilizationId.Rajput, taken);

            Assert.AreEqual(CivilizationId.Rajput, result);
        }

        [Test]
        public void DesiredCivilization_Taken_FallsBackToFirstUntakenInEnumOrder()
        {
            // Enum order: Chola, Vijayanagara, Rajput, Maurya, Maratha.
            var taken = new HashSet<CivilizationId> { CivilizationId.Vijayanagara, CivilizationId.Chola };

            CivilizationId result = CivilizationSetup.ResolveDistinctCivilization(
                CivilizationId.Vijayanagara, taken);

            Assert.AreEqual(CivilizationId.Rajput, result);
        }

        [Test]
        public void AllOtherCivilizationsTaken_FallsBackToDesiredRatherThanThrowing()
        {
            var taken = new HashSet<CivilizationId>
            {
                CivilizationId.Chola, CivilizationId.Vijayanagara, CivilizationId.Rajput,
                CivilizationId.Maurya, CivilizationId.Maratha,
            };

            CivilizationId result = CivilizationSetup.ResolveDistinctCivilization(
                CivilizationId.Maratha, taken);

            Assert.AreEqual(CivilizationId.Maratha, result);
        }

        [Test]
        public void PlayerAndDefaultAiCivilizationCollision_ResolvesToDistinctCivs()
        {
            // Reproduces the real, live bug: player picks the civ that
            // happens to match CivilizationSetup's own aiCivilization
            // Inspector default (Vijayanagara) - the resolver must not
            // hand back the same civ for both.
            CivilizationId playerCiv = CivilizationId.Vijayanagara;
            var taken = new HashSet<CivilizationId> { playerCiv };

            CivilizationId resolvedAiCiv = CivilizationSetup.ResolveDistinctCivilization(
                CivilizationId.Vijayanagara, taken);

            Assert.AreNotEqual(playerCiv, resolvedAiCiv);
        }

        // Covers the match-start camera-focus fix's own local-faction
        // resolution: ResolveLocalPlayerStart is the pure decision behind
        // which map-defined town center CivilizationSetup.BeginMatchCore
        // focuses the camera on, extracted the same way
        // ResolveDistinctCivilization already is above - directly
        // testable without FindFirstObjectByType/a live scene, and the
        // one piece of the camera fix that was previously unverified: the
        // PlayMode smoke test only ever exercises the default (Player)
        // branch, since NetworkMatch.LocalFaction never becomes Enemy
        // outside a real 2-human LAN match.
        [Test]
        public void ResolveLocalPlayerStart_DefaultsToPlayerTownCenter()
        {
            var map = new MapDefinitionData
            {
                PlayerTownCenter = new Vector3(0f, 1f, 20f),
                EnemyTownCenter = new Vector3(0f, 1f, -20f),
            };

            Vector3 result = CivilizationSetup.ResolveLocalPlayerStart(FactionId.Player, map);

            Assert.AreEqual(map.PlayerTownCenter, result);
        }

        [Test]
        public void ResolveLocalPlayerStart_ForTheRemoteLanFaction_ReturnsEnemyTownCenter()
        {
            // The one branch a single-player skirmish never exercises:
            // NetworkMatch.LocalFaction == Enemy only for the human who
            // joined (not hosted) a real 2-human LAN match - that
            // process's own camera must focus its own faction's start,
            // not Player's.
            var map = new MapDefinitionData
            {
                PlayerTownCenter = new Vector3(0f, 1f, 20f),
                EnemyTownCenter = new Vector3(0f, 1f, -20f),
            };

            Vector3 result = CivilizationSetup.ResolveLocalPlayerStart(FactionId.Enemy, map);

            Assert.AreEqual(map.EnemyTownCenter, result);
        }
    }
}
