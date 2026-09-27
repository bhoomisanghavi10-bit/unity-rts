using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Logical definition identity, independent of the object name, model or combat class.
    [DisallowMultipleComponent]
    public sealed class DefinitionId : MonoBehaviour
    {
        [SerializeField] private string value;
        public string Value => value;
        internal void Initialize(string id) => value = id;
    }
}
