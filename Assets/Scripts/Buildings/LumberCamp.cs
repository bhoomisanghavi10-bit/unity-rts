namespace KingdomsOfBharat.Buildings
{
    // Marker building: a resource-specific drop-off for Wood (AoE-style
    // Lumber Camp). See Gatherer.AcceptsDropOff - a worker carrying Wood
    // routes to the nearest LumberCamp or TownCenter, whichever is closer,
    // instead of always defaulting to the TownCenter. No behavior of its
    // own beyond existing as a Building with a ConstructionSite; mirrors
    // House/Farm's lazy Site resolution for the same same-frame-creation
    // reason documented on Barracks.
    public class LumberCamp : Building
    {
        private ConstructionSite _site;
        private bool _siteResolved;

        private ConstructionSite Site
        {
            get
            {
                if (!_siteResolved)
                {
                    TryGetComponent(out _site);
                    _siteResolved = true;
                }

                return _site;
            }
        }

        public bool IsComplete => Site == null || Site.IsComplete;
    }
}
