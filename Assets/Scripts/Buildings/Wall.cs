namespace KingdomsOfBharat.Buildings
{
    // Pure static fortification: no training/production of its own. High
    // HP/armor and a NavMeshObstacle (added by WallFactory) that actually
    // blocks movement through it - unlike every other building today, which
    // has a Collider for selection/raycasting but no NavMesh presence
    // (NavMeshBaker bakes the ground only), so units currently walk straight
    // through a Barracks/TownCenter. Fortifications are the one place that
    // gap actually matters, hence the NavMeshObstacle addition living here
    // rather than being retrofitted onto every Building.
    public class Wall : Building
    {
    }
}
