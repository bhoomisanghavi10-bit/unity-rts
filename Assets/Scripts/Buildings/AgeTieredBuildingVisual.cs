using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Age-aware building visuals: marks a building whose model changes
    // with the owning faction's Age (TownCenter/Tower/Wall only - every
    // other building factory never calls Configure, so this component
    // simply never exists on them). User-confirmed (2026-09-04): Age-up
    // re-skins ARE retroactive - every standing marked building owned by
    // the faction that just aged up rebuilds its visual mesh in place the
    // instant AgeProgress.Advance fires, a deliberate exception to this
    // project's usual "baked in at spawn, not retroactive" convention
    // (unit tiers, upgrades, civ bonuses all stay non-retroactive).
    public class AgeTieredBuildingVisual : MonoBehaviour
    {
        private string _resourceName;
        private Vector3 _fallbackSize;

        public void Configure(string resourceName, Vector3 fallbackSize)
        {
            _resourceName = resourceName;
            _fallbackSize = fallbackSize;
        }

        // Called from TownCenter.TickAgeUp - the sole call site of
        // AgeProgress.Advance - right after a faction's Age actually
        // advances. Covers all 3 age-tiered building types for that
        // faction from one call: Tower/Wall never research Age-up
        // themselves, they just react to the faction-wide advance the
        // same way every standing building of that faction would.
        public static void RefreshAllForFaction(FactionId faction, AgeId newAge)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            foreach (AgeTieredBuildingVisual visual in Object.FindObjectsByType<AgeTieredBuildingVisual>(FindObjectsSortMode.None))
            {
                FactionMember member = visual.GetComponent<FactionMember>();
                if (member == null || member.Faction != faction)
                {
                    continue;
                }

                BuildingModelFactory.Refresh(visual.gameObject, visual._resourceName, civ, newAge, visual._fallbackSize, profile.PrimaryColor, faction: faction);
            }
        }
    }
}
