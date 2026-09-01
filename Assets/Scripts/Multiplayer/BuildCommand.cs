using System;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Multiplayer
{
    // AoE-Parity Phase 5 gap-close: a building-placement order, wired the
    // same way TrainCommand is - BuildingPlacer enqueues this instead of
    // deducting resources / calling XFactory.Place directly at click time.
    // Carries the actual placement as a plain delegate (same reasoning as
    // TrainCommand: there's no shared "placeable" interface across
    // Barracks/Farm/House/etc., and BuildingPlacer's own per-kind cost/place
    // logic already lives as one big switch, not worth forcing into a typed
    // target just for this).
    public class BuildCommand : Command
    {
        private readonly UnityEngine.Object _source;
        private readonly Action _executeBuild;

        public BuildCommand(FactionId faction, UnityEngine.Object source, Action executeBuild) : base(faction)
        {
            _source = source;
            _executeBuild = executeBuild;
        }

        public override void Execute()
        {
            // BuildingPlacer is a persistent scene object and won't
            // normally go stale, but this matches the same guard every
            // other Command carries (MoveCommand/TrainCommand/AttackCommand).
            if (_source == null)
            {
                return;
            }

            _executeBuild();
        }
    }
}
