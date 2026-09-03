using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Wave 2 item 8: AoE's Blacksmith-equivalent - researches the flat
    // Attack/Armor tiers (Progression/UpgradeProgress.cs) that used to live
    // on Barracks (RequestResearchAttack/RequestResearchArmor and their
    // supporting state, moved here verbatim this session - see
    // docs/IMPLEMENTATION_ROADMAP.md Wave 2 item 8). Barracks' per-class
    // Attack/Armor tracks (item 40) and civ UniqueTech stay on Barracks/Durg
    // untouched - those are a separate mechanic earmarked for future
    // dedicated per-unit-class buildings, not this one.
    //
    // Same lazy-resolved Site/FactionMember convention as Barracks/Durg - see
    // those classes' own comments for why (AddComponent fires Awake()
    // immediately, before a factory has necessarily added every sibling
    // component yet). No RallyPoint: Karmashala never spawns or trains
    // anything, it only researches.
    public class Karmashala : Building
    {
        [SerializeField] private float upgradeGoldCostPerTier = 80f;
        [SerializeField] private float upgradeResearchTimePerTier = 15f;

        private ConstructionSite _site;
        private bool _siteResolved;
        private FactionMember _factionMember;
        private float _attackResearchRemaining = -1f;
        private float _armorResearchRemaining = -1f;

        private ConstructionSite Site
        {
            get
            {
                if (!_siteResolved)
                {
                    TryGetComponent(out _site);
                    _siteResolved = true;
                }
                return _site;
            }
        }

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

        public bool IsComplete => Site == null || Site.IsComplete;
        public bool IsResearchingAttack => _attackResearchRemaining >= 0f;
        public bool IsResearchingArmor => _armorResearchRemaining >= 0f;
        public float AttackResearchProgress => IsResearchingAttack
            ? 1f - (_attackResearchRemaining / (upgradeResearchTimePerTier * (UpgradeProgress.AttackTier(Faction) + 1)))
            : 0f;
        public float ArmorResearchProgress => IsResearchingArmor
            ? 1f - (_armorResearchRemaining / (upgradeResearchTimePerTier * (UpgradeProgress.ArmorTier(Faction) + 1)))
            : 0f;

        // Exposed so BuildMenu's cost label reads the same number
        // RequestResearchAttack/Armor actually charge, instead of
        // duplicating the *(tier+1) formula and risking the two drifting
        // apart if upgradeGoldCostPerTier is ever tuned in the Inspector.
        public float NextAttackUpgradeCost => upgradeGoldCostPerTier * (UpgradeProgress.AttackTier(Faction) + 1);
        public float NextArmorUpgradeCost => upgradeGoldCostPerTier * (UpgradeProgress.ArmorTier(Faction) + 1);

        private void Update()
        {
            if (IsResearchingAttack)
            {
                TickAttackResearch();
            }

            if (IsResearchingArmor)
            {
                TickArmorResearch();
            }
        }

        public void RequestResearchAttack()
        {
            if (!IsComplete || IsResearchingAttack || !UpgradeProgress.HasNextAttackTier(Faction))
            {
                return;
            }

            int tier = UpgradeProgress.AttackTier(Faction);
            float cost = upgradeGoldCostPerTier * (tier + 1);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < cost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -cost);
            _attackResearchRemaining = upgradeResearchTimePerTier * (tier + 1);
        }

        public void RequestResearchArmor()
        {
            if (!IsComplete || IsResearchingArmor || !UpgradeProgress.HasNextArmorTier(Faction))
            {
                return;
            }

            int tier = UpgradeProgress.ArmorTier(Faction);
            float cost = upgradeGoldCostPerTier * (tier + 1);
            ResourceStockpile stockpile = ResourceStockpile.For(Faction);
            if (stockpile.GetTotal(ResourceType.Gold) < cost)
            {
                return;
            }

            stockpile.Add(ResourceType.Gold, -cost);
            _armorResearchRemaining = upgradeResearchTimePerTier * (tier + 1);
        }

        private void TickAttackResearch()
        {
            _attackResearchRemaining -= Time.deltaTime;
            if (_attackResearchRemaining <= 0f)
            {
                UpgradeProgress.AdvanceAttack(Faction);
                _attackResearchRemaining = -1f;
            }
        }

        private void TickArmorResearch()
        {
            _armorResearchRemaining -= Time.deltaTime;
            if (_armorResearchRemaining <= 0f)
            {
                UpgradeProgress.AdvanceArmor(Faction);
                _armorResearchRemaining = -1f;
            }
        }
    }
}
