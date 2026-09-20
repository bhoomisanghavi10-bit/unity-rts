using UnityEngine;

namespace KingdomsOfBharat.ResourceGathering
{
    public enum ResourceType
    {
        Food,
        Wood,
        Gold,
        Stone,
    }

    // A harvestable world object (tree, farmland). Depletes as workers
    // harvest it and removes itself once empty.
    public class ResourceNode : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType = ResourceType.Wood;
        [SerializeField] private float amount = 200f;

        private float _initialAmount = -1f;

        // Raised after every harvest (before a depleted node destroys itself).
        // TerrainForest proxies use it to thin the rendered trees.
        public event System.Action<ResourceNode> Changed;

        public ResourceType ResourceType => resourceType;
        public bool IsDepleted => amount <= 0f;
        public float Amount => amount;
        public float RemainingFraction => _initialAmount > 0f
            ? Mathf.Clamp01(amount / _initialAmount)
            : (amount > 0f ? 1f : 0f);

        public void Configure(ResourceType type, float startingAmount)
        {
            resourceType = type;
            amount = startingAmount;
            _initialAmount = startingAmount;
        }

        public float Harvest(float requested)
        {
            float taken = Mathf.Min(requested, amount);
            amount -= taken;
            Changed?.Invoke(this);

            if (IsDepleted)
            {
                Destroy(gameObject);
            }

            return taken;
        }
    }
}
