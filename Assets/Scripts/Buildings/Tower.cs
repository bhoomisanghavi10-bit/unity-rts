namespace KingdomsOfBharat.Buildings
{
    // Marker only - ranged defense behavior lives in TowerAttacker (see
    // TowerFactory), kept as its own Combat-namespace component instead of
    // living here so Buildings doesn't need to depend on Combat for
    // something this self-contained.
    public class Tower : Building
    {
    }
}
