using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Barracks foundation (Barracks + ConstructionSite +
    // FactionMember + VisionSource). Used by both BuildingPlacer
    // (mouse-driven, Player) and AiController (programmatic, Enemy) instead
    // of duplicating the component list in two places.
    public static class BarracksFactory
    {
        private static readonly Vector3 Size = new Vector3(3f, 2f, 3f);
        private static readonly Color PlayerColor = new Color(0.5f, 0.3f, 0.2f);
        private static readonly Color EnemyColor = new Color(0.35f, 0.15f, 0.35f);

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = faction == FactionId.Player ? "Barracks" : "EnemyBarracks";
            go.transform.position = point + Vector3.up * (Size.y * 0.5f);
            go.transform.localScale = Size;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(FindShader())
            {
                color = faction == FactionId.Player ? PlayerColor : EnemyColor,
            };

            go.AddComponent<Barracks>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<FactionMember>().Configure(faction);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(10f);
            }

            return go;
        }

        // Faction-agnostic on purpose: neither side should place a
        // foundation on top of any existing building, friend or foe.
        public static bool IsClear(Vector3 point, float clearance)
        {
            foreach (Building building in Building.All)
            {
                if (Vector3.Distance(building.transform.position, point) < clearance)
                {
                    return false;
                }
            }

            return true;
        }

        private static Shader FindShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }
    }
}
