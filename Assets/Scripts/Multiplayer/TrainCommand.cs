using System;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Multiplayer
{
    // Item 51: a training-request order, wired the same way MoveCommand is -
    // BuildMenu enqueues this instead of calling Barracks/TownCenter.
    // RequestTrain*() directly. TownCenter and Barracks share no common
    // "trainable" interface (each exposes its own RequestTrain/
    // RequestTrainArcher/etc. with building-specific gating), so rather than
    // force one in just to give this command a typed target, it carries the
    // actual request as a plain delegate - same "code-authored delegate over
    // a generic data-driven type" convention ScenarioManager's
    // MissionObjective already established for this codebase.
    public class TrainCommand : Command
    {
        private readonly UnityEngine.Object _source;
        private readonly Action _requestTrain;

        public TrainCommand(FactionId faction, UnityEngine.Object source, Action requestTrain) : base(faction)
        {
            _source = source;
            _requestTrain = requestTrain;
        }

        public override void Execute()
        {
            // The building may have been destroyed in the delay window
            // between the click and this tick executing - same stale-
            // reference guard as MoveCommand.
            if (_source == null)
            {
                return;
            }

            _requestTrain();
        }
    }
}
