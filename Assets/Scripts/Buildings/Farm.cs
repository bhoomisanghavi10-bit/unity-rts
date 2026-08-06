using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Buildings
{
    // Passively generates Food for whichever faction owns it, but only
    // while at least one worker is staffed here (FarmWorker.StaffAt) - the
    // same "needs an active worker" shape as ConstructionSite/Builder.
    // Deliberately slower than gathering directly from a resource node
    // (farmland/fruit bush/hunted carcass) - the tradeoff is that a Farm
    // can be built anywhere, anytime, rather than depending on the map's
    // fixed resource scatter.
    //
    // ConstructionSite/FactionMember resolved lazily, same reasoning as
    // Barracks: a spawner adds these one AddComponent call at a time, and
    // an external caller (e.g. a future AI economy) could query this
    // Farm within the same frame it was created, before Awake()/Start()
    // for this component has necessarily run.
    public class Farm : Building
    {
        [SerializeField] private float foodPerSecondPerWorker = 0.6f;

        private ConstructionSite _site;
        private bool _siteResolved;
        private FactionMember _factionMember;
        private int _activeWorkers;

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

        public void BeginWorking()
        {
            _activeWorkers++;
        }

        public void StopWorking()
        {
            _activeWorkers = Mathf.Max(0, _activeWorkers - 1);
        }

        private void Update()
        {
            if (!IsComplete || _activeWorkers <= 0)
            {
                return;
            }

            float amount = foodPerSecondPerWorker * _activeWorkers * Time.deltaTime;
            ResourceStockpile.For(Faction).Add(ResourceType.Food, amount);
        }
    }
}
