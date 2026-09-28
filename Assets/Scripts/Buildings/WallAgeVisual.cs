using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Wall/Gate's own retroactive Age-up re-skin marker (2026-09-28) -
    // mirrors AgeTieredBuildingVisual's shape (same "every standing marked
    // building owned by the faction that just aged up rebuilds its visual
    // mesh in place" behavior, user-confirmed 2026-09-04) but calls into
    // FortificationVisual instead of BuildingModelFactory.Refresh directly,
    // since Wall/Gate now need to pick between a 3D mesh (Ancient/Imperial)
    // and a flat sprite billboard (Classical/Durg) depending on the new
    // age - TownCenter/Tower stay on the original AgeTieredBuildingVisual
    // unchanged, since they're mesh-only at every age.
    public class WallAgeVisual : MonoBehaviour
    {
        private bool _isGate;
        private WallFactory.WallPieceKind _pieceKind;
        private Vector3 _size;

        public void ConfigureWall(WallFactory.WallPieceKind pieceKind, Vector3 size)
        {
            _isGate = false;
            _pieceKind = pieceKind;
            _size = size;
        }

        public void ConfigureGate(Vector3 size)
        {
            _isGate = true;
            _size = size;
        }

        // Called from TownCenter.TickAgeUp, right alongside the existing
        // AgeTieredBuildingVisual.RefreshAllForFaction call - see that
        // method's own comment for why Age-up re-skins are a deliberate
        // exception to this project's usual non-retroactive convention.
        public static void RefreshAllForFaction(FactionId faction, AgeId newAge)
        {
            CivilizationId civ = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civ);

            foreach (WallAgeVisual visual in Object.FindObjectsByType<WallAgeVisual>(FindObjectsSortMode.None))
            {
                FactionMember member = visual.GetComponent<FactionMember>();
                if (member == null || member.Faction != faction)
                {
                    continue;
                }

                if (visual._isGate)
                {
                    GateFactory.RefreshVisual(visual.gameObject, newAge, civ, profile.PrimaryColor, faction, visual._size);
                }
                else
                {
                    WallFactory.RefreshVisual(visual.gameObject, visual._pieceKind, newAge, civ, profile.PrimaryColor, faction, visual._size);
                }
            }
        }
    }
}
