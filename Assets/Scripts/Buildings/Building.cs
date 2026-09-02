using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Multiplayer;

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
            // Phase 5 LAN transport MVP: see NetworkId.cs - piggybacks on
            // this same registration point rather than every factory.
            NetworkId.Assign(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }
    }
}
