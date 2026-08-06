using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Town Center (TownCenter + FactionMember + VisionSource).
    // Used by both TownCenterSpawner (the Player's fixed starting base) and
    // AiController (the AI's own base).
    public static class TownCenterFactory
    {
        private static readonly Vector3 Size = new Vector3(3f, 2f, 3f);

        public static GameObject Place(Vector3 position, FactionId faction)
        {
            CivilizationProfile profile = CivilizationProfile.For(CivilizationRegistry.For(faction));

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = faction == FactionId.Player ? "TownCenter" : "EnemyTownCenter";
            go.transform.position = position;
            go.transform.localScale = Size;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GameplayMaterial.CreateOpaque(profile.PrimaryColor);

            go.AddComponent<TownCenter>();
            go.AddComponent<FactionMember>().Configure(faction);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(10f);
            }

            return go;
        }

    }
}
