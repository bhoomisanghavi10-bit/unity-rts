using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Buildings
{
    // Creates a House foundation (House + ConstructionSite + FactionMember).
    // Mirrors BarracksFactory/FarmFactory's shape - used by BuildingPlacer
    // for the Player's mouse-driven placement, and AiController for the
    // AI's own population growth.
    public static class HouseFactory
    {
        private static readonly Vector3 Size = new Vector3(2f, 1.6f, 2f);

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationProfile profile = CivilizationProfile.For(CivilizationRegistry.For(faction));

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = faction == FactionId.Player ? "House" : "EnemyHouse";
            go.transform.position = point + Vector3.up * (Size.y * 0.5f);
            go.transform.localScale = Size;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GameplayMaterial.CreateOpaque(profile.PrimaryColor);

            go.AddComponent<House>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<FactionMember>().Configure(faction);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(6f);
            }

            return go;
        }
    }
}
