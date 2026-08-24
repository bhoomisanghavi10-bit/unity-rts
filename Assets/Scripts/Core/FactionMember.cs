using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Item 48: Enemy2 is a second, independent AI faction - added
    // alongside Player/Enemy rather than replacing the pair with a generic
    // N-ary scheme, so every existing FactionId.Player/.Enemy comparison
    // across the codebase keeps meaning exactly what it always meant. A
    // match that never spawns a second AiController never touches Enemy2
    // at all.
    public enum FactionId
    {
        Player,
        Enemy,
        Enemy2,
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
