namespace KingdomsOfBharat.Buildings
{
    // Marker building: each completed House raises its faction's
    // population cap (see Population.Cap) - AoE-style. No behavior of its
    // own beyond existing as a Building with a ConstructionSite; mirrors
    // Farm/Barracks' lazy Site resolution for the same same-frame-creation
    // reason documented on Barracks.
    public class House : Building
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
