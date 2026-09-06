using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Wave 5 item 29: a PLAYER-SLOT color, independent of whichever
    // civilization a faction plays - CivilizationProfile.PrimaryColor
    // stays the "what civ is this" identity, this is the separate "whose
    // is this" identity layered on top via TeamColorAccent. Deliberately
    // not derived from CivilizationProfile.Colors (a civ's own crest
    // color) - two Player-controlled units of different civs should read
    // as the same team, and two enemy factions playing the same civ
    // should not be indistinguishable.
    public static class TeamColor
    {
        public static Color For(FactionId faction)
        {
            switch (faction)
            {
                case FactionId.Player: return new Color(0.16f, 0.38f, 0.85f);
                case FactionId.Enemy: return new Color(0.82f, 0.16f, 0.16f);
                case FactionId.Enemy2: return new Color(0.18f, 0.72f, 0.30f);
                default: return Color.white;
            }
        }
    }
}
