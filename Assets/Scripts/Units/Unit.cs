using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace KingdomsOfBharat.Units
{
    // Identity marker for anything the player can select and command.
    // Maintains a registry of all live units so systems like box-select
    // don't need per-frame scene scans.
    [RequireComponent(typeof(NavMeshAgent))]
    public class Unit : MonoBehaviour
    {
        public static readonly List<Unit> All = new List<Unit>();

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
