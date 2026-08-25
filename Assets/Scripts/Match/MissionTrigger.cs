using System;

namespace KingdomsOfBharat.Match
{
    // Item 50: a one-shot scripted event - "when Condition first becomes
    // true, run Fire once." Same code-authored-delegate convention as
    // MissionObjective. Fires at most once per match (ScenarioManager
    // tracks which Ids have already fired) - a trigger describing "at
    // t=60s, grant a resource bonus" would otherwise re-fire every tick
    // once the condition stays true forever.
    public class MissionTrigger
    {
        public string Id;
        public Func<bool> Condition;
        public Action Fire;

        public MissionTrigger(string id, Func<bool> condition, Action fire)
        {
            Id = id;
            Condition = condition;
            Fire = fire;
        }
    }
}
