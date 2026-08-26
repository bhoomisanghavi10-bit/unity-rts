using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer;

namespace KingdomsOfBharat.Combat
{
    // Phase 6 gap-close: Rajput's "defeated Cavalry has a 25% chance to
    // leave behind a weakened Infantry survivor instead of dying outright"
    // bonus. Not representable as a passiveBonuses StatModifier (a
    // probabilistic structural mechanic, not a stat multiplier), so a
    // hand-picked civ check - kept in its own file rather than inline in
    // Attackable.TakeDamage so Attackable itself doesn't need a Core/
    // Multiplayer dependency baked into its damage-resolution method for
    // one civ's mechanic.
    public static class RajputDefianceHook
    {
        private const float SurvivalChance = 0.25f;
        private const float SurvivorHealthFraction = 0.5f;

        // Pure gating checks, split out from TrySpawnSurvivor so they're
        // testable without driving a full SoldierFactory.Spawn (which
        // pulls in model/animation setup a lightweight EditMode test
        // shouldn't need to exercise just to verify the civ/class/roll
        // gate).
        public static bool IsEligible(FactionId faction, UnitClass unitClass)
        {
            return unitClass == UnitClass.Cavalry && CivilizationRegistry.For(faction) == CivilizationId.Rajput;
        }

        public static bool RollSucceeds(float roll)
        {
            return roll < SurvivalChance;
        }

        // Called from Attackable.TakeDamage right before the dying
        // GameObject is destroyed. Uses DeterministicRandom (not
        // UnityEngine.Random) so the roll replays identically under the
        // lockstep CommandBus, same convention as every other gameplay-
        // relevant random roll in this project. Only draws from it for
        // eligible (Rajput Cavalry) deaths, so every other death doesn't
        // silently consume a Match roll some other system might depend on.
        public static void TrySpawnSurvivor(Attackable dying)
        {
            if (!dying.TryGetComponent(out FactionMember factionMember)
                || !IsEligible(factionMember.Faction, dying.Class)
                || !RollSucceeds(DeterministicRandom.Match.NextFloat01()))
            {
                return;
            }

            GameObject survivor = SoldierFactory.Spawn(dying.transform.position, factionMember.Faction);
            if (survivor.TryGetComponent(out Attackable survivorAttackable))
            {
                survivorAttackable.Configure(survivorAttackable.MaxHealth * SurvivorHealthFraction);
            }
        }
    }
}
