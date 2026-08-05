using UnityEngine;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Buildings
{
    // Trains a Soldier unit. Every completed, idle Barracks trains one
    // Soldier when RequestTrain() is called - either the train hotkey
    // (below) or BuildMenu's Train Soldier button (UI, milestone 7), both
    // funnel through the same entry point.
    public class Barracks : Building
    {
        [SerializeField] private KeyCode trainKey = KeyCode.T;
        [SerializeField] private float soldierFoodCost = 50f;
        [SerializeField] private float soldierGoldCost = 20f;
        [SerializeField] private float trainTime = 5f;
        [SerializeField] private Vector3 rallyOffset = new Vector3(3f, 0f, 3f);

        private ConstructionSite _site;
        private float _remaining = -1f;

        public bool IsComplete => _site == null || _site.IsComplete;
        public bool IsTraining => _remaining >= 0f;

        private void Awake()
        {
            TryGetComponent(out _site);
        }

        private void Update()
        {
            if (IsTraining)
            {
                TickTraining();
                return;
            }

            if (Input.GetKeyDown(trainKey))
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

            if (ResourceStockpile.Instance.GetTotal(ResourceType.Food) < soldierFoodCost
                || ResourceStockpile.Instance.GetTotal(ResourceType.Gold) < soldierGoldCost)
            {
                return;
            }

            ResourceStockpile.Instance.Add(ResourceType.Food, -soldierFoodCost);
            ResourceStockpile.Instance.Add(ResourceType.Gold, -soldierGoldCost);
            _remaining = trainTime;
        }

        private void TickTraining()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                SoldierFactory.Spawn(transform.position + rallyOffset);
                _remaining = -1f;
            }
        }
    }
}
