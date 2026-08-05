using UnityEngine;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Buildings
{
    // Trains a Soldier unit. No build queue/UI yet (milestone 7) - every
    // completed, idle Barracks trains one Soldier per press of the train
    // key, if Food allows.
    public class Barracks : Building
    {
        [SerializeField] private KeyCode trainKey = KeyCode.T;
        [SerializeField] private float soldierFoodCost = 50f;
        [SerializeField] private float trainTime = 5f;
        [SerializeField] private Vector3 rallyOffset = new Vector3(3f, 0f, 3f);

        private ConstructionSite _site;
        private float _remaining = -1f;

        private bool IsComplete => _site == null || _site.IsComplete;
        private bool IsTraining => _remaining >= 0f;

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

            if (IsComplete && Input.GetKeyDown(trainKey))
            {
                TryStartTraining();
            }
        }

        private void TryStartTraining()
        {
            if (ResourceStockpile.Instance.GetTotal(ResourceType.Food) < soldierFoodCost)
            {
                return;
            }

            ResourceStockpile.Instance.Add(ResourceType.Food, -soldierFoodCost);
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
