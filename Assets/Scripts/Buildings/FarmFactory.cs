using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Farm foundation (Farm + ConstructionSite + FactionMember).
    // Mirrors BarracksFactory's shape - used by BuildingPlacer for the
    // Player's mouse-driven placement.
    public static class FarmFactory
    {
        private static readonly Vector3 Size = new Vector3(2f, 0.6f, 2f);

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            CivilizationProfile profile = CivilizationProfile.For(CivilizationRegistry.For(faction));

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = faction == FactionId.Player ? "Farm" : "EnemyFarm";
            go.transform.position = point + Vector3.up * (Size.y * 0.5f);
            go.transform.localScale = Size;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(FindShader())
            {
                color = profile.PrimaryColor,
            };

            go.AddComponent<Farm>();
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

        private static Shader FindShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }
    }
}
