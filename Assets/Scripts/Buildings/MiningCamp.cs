namespace KingdomsOfBharat.Buildings
{
    // Marker building: a resource-specific drop-off for Gold and Stone
    // (AoE-style Mining Camp covers both). See Gatherer.AcceptsDropOff.
    // Mirrors LumberCamp's shape exactly.
    public class MiningCamp : Building
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
