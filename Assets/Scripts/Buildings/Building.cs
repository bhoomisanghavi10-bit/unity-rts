using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Buildings
{
    // Base for anything placeable that isn't a unit. Maintains a registry so
    // systems like Gatherer can find the nearest drop-off without a scene scan.
    public class Building : MonoBehaviour
    {
        public static readonly List<Building> All = new List<Building>();

        private void OnEnable()
        {
            All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }
    }
}
