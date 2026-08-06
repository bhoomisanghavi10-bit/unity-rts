using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Buildings
{
    // Identifies this building as a valid drop-off point for gathered
    // resources, and trains Worker units - the Player's via the train
    // hotkey or BuildMenu's button, the AI's via AiController, both
    // funneling through RequestTrain(). Mirrors Barracks' training shape
    // exactly (lazy FactionMember resolution for the same same-frame-
    // creation reason documented there), plus a Population.HasRoom() check
    // Barracks now shares too - AoE-style, training blocks at the
    // population cap until a House is built.
    public class TownCenter : Building
    {
        [SerializeField] private KeyCode trainKey = KeyCode.G;
        [SerializeField] private float workerFoodCost = 50f;
        [SerializeField] private float trainTime = 6f;
        [SerializeField] private Vector3 rallyOffset = new Vector3(-3f, 0f, 3f);

        private FactionMember _factionMember;
        private float _remaining = -1f;

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
            if (IsTraining || !Population.HasRoom(Faction))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Food) < workerFoodCost)
            {
                return;
            }

            stockpile.Add(ResourceType.Food, -workerFoodCost);
            _remaining = trainTime * CivilizationProfile.For(CivilizationRegistry.For(Faction)).TrainTimeMultiplier;
        }

        private void TickTraining()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                WorkerFactory.Spawn(transform.position + rallyOffset, Faction);
                _remaining = -1f;
            }
        }
    }
}
