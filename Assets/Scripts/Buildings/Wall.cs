namespace KingdomsOfBharat.Buildings
{
    // Pure static fortification: no training/production of its own. High
    // HP/armor and a NavMeshObstacle (added by WallFactory) that blocks its
    // full footprint edge-to-edge, no passable margin - the one exception
    // to BuildingFootprint's square-tile/margin system every other building
    // now goes through (see BuildingFootprint.cs).
    public class Wall : Building
    {
    }
}
