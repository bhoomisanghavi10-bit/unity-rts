using UnityEngine;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Buildings
{
    // Trains a Soldier unit for whichever faction owns this Barracks. Every
    // completed, idle Barracks trains one Soldier when RequestTrain() is
    // called - the Player's via the train hotkey (Player-owned Barracks
    // only) or BuildMenu's Train Soldier button, the AI's via AiController -
    // all funnel through the same entry point.
    //
    // Deliberately NOT [RequireComponent(typeof(FactionMember))]: that would
    // auto-add a default (Player) FactionMember the instant AddComponent
    // <Barracks>() runs, before a factory gets the chance to add its own
    // Configure()'d one - producing two FactionMember components on the same
    // GameObject and silently picking the wrong (default Player) one via
    // GetComponent.
    //
    // ConstructionSite/FactionMember are resolved lazily (on first actual
    // use), not in Awake()/Start(): a spawner adds Barracks and its siblings
    // one AddComponent call at a time within a single synchronous method,
    // and AddComponent fires Awake() immediately - so an eager Awake()-time
    // GetComponent for a sibling added moments later would find nothing.
    // Start() would fix that (guaranteed to run after every Awake()), but
    // AiController can call RequestTrain() on a Barracks it just created
    // within the very same Update() tick, before that Barracks' own Start()
    // has even run yet for the first time. Lazy resolution sidesteps both
    // problems: by the time anything actually reads these, every sibling
    // from that same synchronous spawn call already exists on the
    // GameObject, regardless of which lifecycle callback has or hasn't
    // fired.
    public class Barracks : Building
    {
        [SerializeField] private KeyCode trainKey = KeyCode.T;
        [SerializeField] private float soldierFoodCost = 50f;
        [SerializeField] private float soldierGoldCost = 20f;
        [SerializeField] private float trainTime = 5f;
        [SerializeField] private Vector3 rallyOffset = new Vector3(3f, 0f, 3f);

        private ConstructionSite _site;
        private bool _siteResolved;
        private FactionMember _factionMember;
        private float _remaining = -1f;

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
        public bool IsTraining => _remaining >= 0f;

        private void Update()
        {
            if (IsTraining)
            {
                TickTraining();
                return;
            }

            if (Faction == FactionId.Player && Input.GetKeyDown(trainKey))
            {
                RequestTrain();
            }
        }

        public void RequestTrain()
        {
            if (!IsComplete || IsTraining)
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < soldierFoodCost
                || stockpile.GetTotal(ResourceType.Gold) < soldierGoldCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -soldierFoodCost);
            stockpile.Add(ResourceType.Gold, -soldierGoldCost);
            _remaining = trainTime;
        }

        private void TickTraining()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                SoldierFactory.Spawn(transform.position + rallyOffset, Faction);
                _remaining = -1f;
            }
        }
    }
}
