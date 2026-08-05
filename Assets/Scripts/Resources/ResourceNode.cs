using UnityEngine;

namespace KingdomsOfBharat.ResourceGathering
{
    public enum ResourceType
    {
        Food,
        Wood,
    }

    // A harvestable world object (tree, farmland). Depletes as workers
    // harvest it and removes itself once empty.
    public class ResourceNode : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType = ResourceType.Wood;
        [SerializeField] private float amount = 200f;

        public ResourceType ResourceType => resourceType;
        public bool IsDepleted => amount <= 0f;

        public void Configure(ResourceType type, float startingAmount)
        {
            resourceType = type;
            amount = startingAmount;
        }

        public float Harvest(float requested)
        {
            float taken = Mathf.Min(requested, amount);
            amount -= taken;

            if (IsDepleted)
            {
                Destroy(gameObject);
            }

            return taken;
        }
    }
}
