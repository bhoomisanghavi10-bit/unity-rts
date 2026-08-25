using System;
using System.Collections.Generic;
using UnityEngine;

// AoE4-style explicit rock-paper-scissors table: one multiplier per
// (attacker category, defender category) pair. Keep this as a SINGLE
// asset for the whole game so balance changes happen in one place.
[CreateAssetMenu(fileName = "CounterMatrix", menuName = "BharatRTS/Counter Matrix")]
public class CounterMatrix : ScriptableObject
{
    [Serializable]
    public struct CounterEntry
    {
        public UnitCategory attacker;
        public UnitCategory defender;
        [Tooltip("1.0 = normal damage, >1.0 = attacker is strong vs defender, <1.0 = weak")]
        public float damageMultiplier;
    }

    public List<CounterEntry> entries = new List<CounterEntry>();

    private Dictionary<(UnitCategory, UnitCategory), float> _lookup;

    public float GetMultiplier(UnitCategory attacker, UnitCategory defender)
    {
        if (_lookup == null) BuildLookup();
        return _lookup.TryGetValue((attacker, defender), out var mult) ? mult : 1.0f;
    }

    public void BuildLookup()
    {
        _lookup = new Dictionary<(UnitCategory, UnitCategory), float>();
        foreach (var e in entries)
            _lookup[(e.attacker, e.defender)] = e.damageMultiplier;
    }

    private void OnEnable() => BuildLookup();
}
