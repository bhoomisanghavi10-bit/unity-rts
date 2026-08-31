using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // General garrisoning system (worker mechanics audit, 2026-08-29 /
    // Roadmap Section 1, closed 2026-09-01). BuildingAttacker generalizes
    // the old Tower-only TowerAttacker so TownCenter can share the same
    // scan-and-shoot logic and both can scale their shot count with
    // garrison occupancy (the AoE IV "murder holes" mechanic). Drives
    // BuildingAttacker directly via its internal Tick(deltaTime), same
    // convention as RepairableTests/ConstructionSiteTests - Update()
    // itself depends on Time.deltaTime, which EditMode tests don't
    // naturally advance.
    public class BuildingAttackerTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                {
                    continue;
                }
                // Unit.All registration/deregistration normally happens
                // via OnEnable/OnDisable, but Unity's Editor doesn't
                // guarantee those fire synchronously within a single test
                // method (same gotcha BuildingFootprintTests documents for
                // Building.All) - removed directly here instead.
                if (go.TryGetComponent(out Unit unit))
                {
                    Unit.All.Remove(unit);
                }
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private BuildingAttacker CreateAttacker(FactionId faction, GarrisonPoint garrisonPoint, int maxBonusShots)
        {
            GameObject go = CreateGameObject("Building");
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<Attackable>().ConfigureClass(UnitClass.Building);
            var attacker = go.AddComponent<BuildingAttacker>();
            attacker.ConfigureGarrisonBonus(garrisonPoint, maxBonusShots);
            return attacker;
        }

        private Attackable CreateHostile(FactionId faction, Vector3 position)
        {
            GameObject go = CreateGameObject("Hostile");
            go.transform.position = position;
            go.AddComponent<FactionMember>().Configure(faction);
            // Registered directly rather than relying on Unit.OnEnable -
            // see TearDown's comment for why.
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(1000f);
            attackable.ConfigureClass(UnitClass.Infantry);
            return attackable;
        }

        private GarrisonPoint CreateOccupiedGarrisonPoint(int occupantCount)
        {
            GameObject go = CreateGameObject("GarrisonHost");
            GarrisonPoint garrisonPoint = go.AddComponent<GarrisonPoint>();
            garrisonPoint.Configure(occupantCount, durgOnly: false);
            for (int i = 0; i < occupantCount; i++)
            {
                GameObject occupant = CreateGameObject($"Occupant{i}");
                occupant.AddComponent<GarrisonSeeker>();
                garrisonPoint.TryGarrison(occupant);
            }

            return garrisonPoint;
        }

        // Attackable.TakeDamage unconditionally spawns a VfxFactory particle
        // burst (stopAction: Destroy) - harmless at real runtime, but Unity's
        // Editor logs "Destroy may not be called from edit mode!" once that
        // particle system's internal cleanup fires outside Play mode, same
        // "expected, not a regression to chase" situation
        // BuildingModelFactoryTests documents for its own Editor-only
        // warning. Suppressed for the duration of the Tick call rather than
        // matched per-call, since exactly when the particle system's own
        // stop-action log fires isn't this test's concern.
        private void TickIgnoringVfxLogs(BuildingAttacker attacker, float deltaTime)
        {
            LogAssert.ignoreFailingMessages = true;
            try
            {
                attacker.Tick(deltaTime);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        [Test]
        public void Tick_WithNoGarrison_HitsExactlyOneTarget()
        {
            BuildingAttacker attacker = CreateAttacker(FactionId.Player, garrisonPoint: null, maxBonusShots: 3);
            Attackable target1 = CreateHostile(FactionId.Enemy, new Vector3(1f, 0f, 0f));
            Attackable target2 = CreateHostile(FactionId.Enemy, new Vector3(2f, 0f, 0f));

            TickIgnoringVfxLogs(attacker, 1f);

            int hit = (target1.Health < 1000f ? 1 : 0) + (target2.Health < 1000f ? 1 : 0);
            Assert.AreEqual(1, hit);
        }

        [Test]
        public void Tick_ShotCountScalesWithGarrisonOccupancy()
        {
            GarrisonPoint garrisonPoint = CreateOccupiedGarrisonPoint(2);
            BuildingAttacker attacker = CreateAttacker(FactionId.Player, garrisonPoint, maxBonusShots: 3);
            var targets = new List<Attackable>
            {
                CreateHostile(FactionId.Enemy, new Vector3(1f, 0f, 0f)),
                CreateHostile(FactionId.Enemy, new Vector3(2f, 0f, 0f)),
                CreateHostile(FactionId.Enemy, new Vector3(3f, 0f, 0f)),
            };

            TickIgnoringVfxLogs(attacker, 1f);

            int hitCount = targets.FindAll(t => t.Health < 1000f).Count;
            // 1 base shot + 2 garrisoned occupants = 3 simultaneous shots.
            Assert.AreEqual(3, hitCount);
        }

        [Test]
        public void Tick_ShotCountCapsAtMaxBonusShots()
        {
            GarrisonPoint garrisonPoint = CreateOccupiedGarrisonPoint(5);
            BuildingAttacker attacker = CreateAttacker(FactionId.Player, garrisonPoint, maxBonusShots: 2);
            var targets = new List<Attackable>();
            for (int i = 0; i < 5; i++)
            {
                targets.Add(CreateHostile(FactionId.Enemy, new Vector3(i + 1, 0f, 0f)));
            }

            TickIgnoringVfxLogs(attacker, 1f);

            int hitCount = targets.FindAll(t => t.Health < 1000f).Count;
            // 1 base shot + maxBonusShots(2) = 3, even though 5 are garrisoned.
            Assert.AreEqual(3, hitCount);
        }

        [Test]
        public void Tick_WithNoHostilesInRange_DoesNothingAndDoesNotConsumeCooldown()
        {
            BuildingAttacker attacker = CreateAttacker(FactionId.Player, garrisonPoint: null, maxBonusShots: 0);

            attacker.Tick(1f);

            Assert.IsFalse(attacker.IsAttacking);
        }
    }
}
