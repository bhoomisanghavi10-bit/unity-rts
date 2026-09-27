using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.Buildings
{
    // Creates a Farm foundation (Farm + ConstructionSite + FactionMember).
    // Mirrors BarracksFactory's shape - used by BuildingPlacer for the
    // Player's mouse-driven placement.
    public static class FarmFactory
    {
        private static readonly Vector3 Size = new Vector3(2f, 0.6f, 2f);
        private const float MaxHealth = 150f;

        public static GameObject Place(Vector3 point, FactionId faction, float buildTime)
        {
            // Farm's visual is a flat, pre-rendered isometric sprite that
            // swaps live between construction/full/depleted states (see
            // FarmVisual) - structurally different from every other
            // building's static 3D mesh, so this deliberately bypasses
            // BuildingModelFactory.Spawn (which would resolve a model/
            // procedural-fallback shape and civ-tint it, neither of which
            // applies here) and builds its own minimal root/visual/collider
            // instead.
            GameObject go = new GameObject(faction == FactionId.Player ? "Farm" : "EnemyFarm");
            go.transform.position = point + Vector3.up * (Size.y * 0.5f);
            FarmVisual.Build(go.transform, BuildingFootprint.FarmTiles);

            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(BuildingFootprint.FarmTiles, 0.2f, BuildingFootprint.FarmTiles);
            collider.center = new Vector3(0f, -Size.y * 0.5f + 0.1f, 0f);

            BuildingFootprint.Attach(go, BuildingFootprint.Square(BuildingFootprint.FarmTiles), carveObstacle: true);

            go.AddComponent<Farm>();
            var site = go.AddComponent<ConstructionSite>();
            site.Configure(buildTime);
            go.AddComponent<SelectionIndicator>().Configure(1.3f, -Size.y * 0.5f);
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(MaxHealth);
            attackable.ConfigureArmor(meleeArmor: 0f, pierceArmor: 1f);
            attackable.ConfigureClass(UnitClass.Building);
            go.AddComponent<Repairable>();
            go.AddComponent<HealthBar>();
            go.AddComponent<FactionMember>().Configure(faction);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (VisionSource.IsTracked(faction))
            {
                go.AddComponent<VisionSource>().Configure(6f);
            }

            // A flat sprite decal has no real height to derive banner size/
            // position from the way BuildingModelFactory.AlignBaseToGround
            // does for a 3D mesh - use a nominal 1-unit height (comparable
            // to House/Farm's old built height) so the banner pole doesn't
            // read as buried in the ground.
            var bannerBounds = new Bounds(go.transform.position + Vector3.up * 0.5f,
                new Vector3(BuildingFootprint.FarmTiles, 1f, BuildingFootprint.FarmTiles));
            TeamColorAccent.AttachToBuilding(go.transform, bannerBounds, faction);

            return go;
        }

    }
}
