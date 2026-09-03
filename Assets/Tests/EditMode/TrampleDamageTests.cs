using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Wave 0 item 4 (docs/IMPLEMENTATION_ROADMAP.md): wires the previously
    // declared-but-unused DamageType.Trample value onto a real damage type
    // and a real mechanic. Two things to prove:
    //  (1) Attackable.TakeDamage resolves a Trample hit against meleeArmor,
    //      not pierceArmor - it merged into what used to be a 2-value
    //      Combat.DamageType enum (Melee/Pierce only), so a naive change
    //      could easily leave Trample silently falling through to the
    //      wrong armor stat.
    //  (2) A Trample-tagged attacker with a splash radius (the mechanic
    //      MauryaWarElephantFactory/VijayanagaraWarElephantFactory now use)
    //      actually damages a nearby hostile, same as Siege's own splash
    //      (SiegeSplashTests) - reusing that mechanism, not a second one.
    // Drives MeleeAttacker directly via its internal Tick(deltaTime), same
    // convention as SiegeSplashTests/BuildingAttackerTests - Update()
    // itself depends on Time.deltaTime, which EditMode tests don't
    // naturally advance. Doesn't drive MauryaWarElephantFactory/
    // VijayanagaraWarElephantFactory.Spawn directly - those depend on
    // Resources-loaded prefabs/DataRegistry the way other combat-unit
    // factories do, which this project's own history documents as
    // EditMode-hostile (see docs/SESSION_LOG.md); the actual factory
    // wiring is live-verified via UnityMCP in Play mode instead (see
    // docs/SESSION_LOG.md's matching entry).
    public class TrampleDamageTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private static readonly Regex SetDestinationErrorPattern = new Regex("SetDestination.*NavMesh");
        private static readonly Regex VfxDestroyErrorPattern = new Regex("Destroy may not be called from edit mode");

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                {
                    continue;
                }
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

        private Attackable CreateTarget(float meleeArmor, float pierceArmor)
        {
            GameObject go = CreateGameObject("Target");
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(1000f);
            attackable.ConfigureArmor(meleeArmor, pierceArmor);
            attackable.ConfigureClass(UnitClass.Infantry);
            return attackable;
        }

        private static void ExpectVfxDestroyError()
        {
            LogAssert.Expect(LogType.Error, VfxDestroyErrorPattern);
        }

        [Test]
        public void TakeDamage_Trample_ResolvesAgainstMeleeArmor_NotPierceArmor()
        {
            Attackable meleeArmored = CreateTarget(meleeArmor: 100f, pierceArmor: 0f);
            Attackable pierceArmored = CreateTarget(meleeArmor: 0f, pierceArmor: 100f);

            ExpectVfxDestroyError();
            meleeArmored.TakeDamage(10f, DamageType.Trample);
            ExpectVfxDestroyError();
            pierceArmored.TakeDamage(10f, DamageType.Trample);

            Assert.AreEqual(999f, meleeArmored.Health, 0.001f,
                "100 meleeArmor should blunt a Trample hit down to the 1-damage floor - Trample reads meleeArmor.");
            Assert.AreEqual(990f, pierceArmored.Health, 0.001f,
                "100 pierceArmor should NOT block a Trample hit at all - Trample must not read pierceArmor.");
        }

        private GameObject CreateTramplingAttacker(FactionId faction, float splashRadius, float splashMultiplier)
        {
            GameObject go = CreateGameObject("WarElephant");
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<Attackable>().ConfigureClass(UnitClass.Siege);
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(10f);
            attacker.SetRange(3f);
            attacker.SetUnitClass(UnitClass.Siege);
            // Same wiring MauryaWarElephantFactory/VijayanagaraWarElephantFactory
            // now apply.
            attacker.SetDamageType(DamageType.Trample);
            attacker.SetSplashRadius(splashRadius, splashMultiplier);
            return go;
        }

        private Attackable CreateHostileUnit(FactionId faction, Vector3 position)
        {
            GameObject go = CreateGameObject("HostileUnit");
            go.transform.position = position;
            go.AddComponent<FactionMember>().Configure(faction);
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

        [Test]
        public void Tick_TramplingAttacker_DamagesPrimaryAndNearbyHostile_MissesFarHostile()
        {
            GameObject attackerGo = CreateTramplingAttacker(FactionId.Player, splashRadius: 1.4f, splashMultiplier: 0.4f);
            var attacker = attackerGo.GetComponent<MeleeAttacker>();
            Attackable primaryTarget = CreateHostileUnit(FactionId.Enemy, Vector3.zero);
            Attackable nearby = CreateHostileUnit(FactionId.Enemy, new Vector3(1f, 0f, 0f));
            Attackable far = CreateHostileUnit(FactionId.Enemy, new Vector3(5f, 0f, 0f));

            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            attacker.AttackMove(primaryTarget);
            ExpectVfxDestroyError();
            ExpectVfxDestroyError();
            attacker.Tick(1f);

            Assert.AreEqual(990f, primaryTarget.Health, 0.001f, "Primary target takes the full 10 base damage.");
            Assert.AreEqual(996f, nearby.Health, 0.001f,
                "A hostile within the trample radius should take reduced (0.4x) secondary damage: 10 * 0.4 = 4.");
            Assert.AreEqual(1000f, far.Health, "A hostile outside the trample radius should be untouched.");
        }
    }
}
