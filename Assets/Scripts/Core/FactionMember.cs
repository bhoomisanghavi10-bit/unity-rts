using UnityEngine;

namespace KingdomsOfBharat.Core
{
    public enum FactionId
    {
        Player,
        Enemy,
    }

    // Ownership tag attached by spawners to every unit/building. Absence of
    // this component (e.g. on the target dummy - see TargetDummySpawner for
    // the exception - or on truly neutral objects) means "always a valid
    // attack target, never selectable as a controllable unit."
    public class FactionMember : MonoBehaviour
    {
        [SerializeField] private FactionId faction = FactionId.Player;

        public FactionId Faction => faction;

        public void Configure(FactionId newFaction)
        {
            faction = newFaction;
        }
    }
}
