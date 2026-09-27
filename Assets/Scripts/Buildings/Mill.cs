using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Buildings
{
    // Marker building: a resource-specific drop-off for Food (AoE-style
    // Mill). See Gatherer.AcceptsDropOff. Mirrors LumberCamp/MiningCamp's
    // shape exactly, plus a Farm auto-reseed toggle (AoE reference: "Farms
    // can be automatically reseeded if they are queued in the Mill (or
    // equivalent)") - see MillAutoReseedRegistry's own header comment for
    // why this is a per-faction toggle rather than a literal per-Mill
    // queue.
    public class Mill : Building
    {
        private ConstructionSite _site;
        private bool _siteResolved;
        private FactionMember _factionMember;

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

        private FactionId Faction
        {
            get
            {
                if (_factionMember == null)
                {
                    _factionMember = GetComponent<FactionMember>();
                }
                return _factionMember.Faction;
            }
        }

        public bool IsComplete => Site == null || Site.IsComplete;

        public bool AutoReseedEnabled => MillAutoReseedRegistry.IsEnabled(Faction);

        public void RequestToggleAutoReseed()
        {
            if (!IsComplete)
            {
                return;
            }

            MillAutoReseedRegistry.Toggle(Faction);
        }
    }
}
