namespace KingdomsOfBharat.Buildings
{
    // Marker building: a resource-specific drop-off for Food (AoE-style
    // Mill). See Gatherer.AcceptsDropOff. Mirrors LumberCamp/MiningCamp's
    // shape exactly.
    public class Mill : Building
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
