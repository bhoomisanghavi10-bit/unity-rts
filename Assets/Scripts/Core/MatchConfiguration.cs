using System;
using System.Collections.Generic;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Core
{
    public enum SlotType
    {
        Closed,
        Human,
        AI,
    }

    // One player slot. Faction is the fixed slot identity (Player/Enemy/
    // Enemy2 - this codebase's slots); Team groups slots that start allied.
    [Serializable]
    public sealed class MatchSlot
    {
        public FactionId Faction;
        public SlotType Type;
        public CivilizationId Civilization;
        public int Team;
    }

    // "Override = false" keeps whatever the scene's ResourceStockpile
    // objects were authored with (the pre-configuration behaviour).
    [Serializable]
    public struct StartingResourceRule
    {
        public bool Override;
        public float Food;
        public float Wood;
        public float Gold;
        public float Stone;
    }

    // The single authoritative description of one match, created once at
    // match start and consumed by CivilizationSetup and the systems it
    // starts. Owns: map, ONE seed (with named deterministic substreams),
    // slots (type/civ/team), local player, starting-resource and
    // population rules, victory settings, and content/schema versions.
    // Anything that used to invent its own seed or infer these from scene
    // fields, PlayerPrefs or FactionId defaults reads this instead.
    public sealed class MatchConfiguration
    {
        public const int CurrentSchemaVersion = 1;
        public const string CurrentContentVersion = "kob-2026.09";
        public const int DefaultPopulationBase = 10;
        public const int DefaultPopulationPerHouse = 5;

        public int SchemaVersion = CurrentSchemaVersion;
        public string ContentVersion = CurrentContentVersion;
        public MapId Map;
        public int Seed;
        public FactionId LocalFaction = FactionId.Player;
        public List<MatchSlot> Slots = new List<MatchSlot>();
        public StartingResourceRule StartingResources;
        public int PopulationBase = DefaultPopulationBase;
        public int PopulationPerHouse = DefaultPopulationPerHouse;
        public int TimeLimitMinutes;
        public bool RegicideEnabled;

        // The configuration of the match currently running (null between
        // matches and in any scene that never started one - callers then
        // fall back to their legacy behaviour). Replaced wholesale by the
        // next Begin, cleared by End.
        public static MatchConfiguration Current { get; private set; }

        public static void Begin(MatchConfiguration configuration)
        {
            Current = configuration;
        }

        public static void End()
        {
            Current = null;
        }

        public MatchSlot SlotFor(FactionId faction)
        {
            foreach (MatchSlot slot in Slots)
            {
                if (slot.Faction == faction)
                {
                    return slot;
                }
            }

            return null;
        }

        // True for a Human or AI slot (a Closed or absent slot spawns and
        // simulates nothing).
        public bool IsPlayable(FactionId faction)
        {
            MatchSlot slot = SlotFor(faction);
            return slot != null && slot.Type != SlotType.Closed;
        }

        public bool IsAi(FactionId faction)
        {
            MatchSlot slot = SlotFor(faction);
            return slot != null && slot.Type == SlotType.AI;
        }

        // Independent, reproducible substream per named system: mixing the
        // match seed with the stream name (FNV-1a) means adding or reordering
        // draws in one system can never shift another's sequence, and no
        // system shares a global random state.
        public int SeedFor(string stream)
        {
            unchecked
            {
                uint hash = 2166136261u;
                uint s = (uint)Seed;
                for (int i = 0; i < 4; i++)
                {
                    hash = (hash ^ ((s >> (i * 8)) & 0xFF)) * 16777619u;
                }

                foreach (char c in stream)
                {
                    hash = (hash ^ c) * 16777619u;
                }

                return (int)(hash & 0x7FFFFFFF);
            }
        }

        public DeterministicRandom Stream(string stream)
        {
            return new DeterministicRandom(SeedFor(stream));
        }

        // Order-stable hash of everything that defines the match, for
        // comparing two peers/runs (also usable in logs). LocalFaction is
        // deliberately excluded: it is each peer's own perspective, and the
        // two peers' hashes must be equal.
        public int ComputeHash()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + SchemaVersion;
                h = h * 31 + ContentVersion.GetHashCode();
                h = h * 31 + (int)Map;
                h = h * 31 + Seed;
                foreach (MatchSlot slot in Slots)
                {
                    h = h * 31 + (int)slot.Faction;
                    h = h * 31 + (int)slot.Type;
                    h = h * 31 + (int)slot.Civilization;
                    h = h * 31 + slot.Team;
                }
                h = h * 31 + (StartingResources.Override ? 1 : 0);
                h = h * 31 + StartingResources.Food.GetHashCode();
                h = h * 31 + StartingResources.Wood.GetHashCode();
                h = h * 31 + StartingResources.Gold.GetHashCode();
                h = h * 31 + StartingResources.Stone.GetHashCode();
                h = h * 31 + PopulationBase;
                h = h * 31 + PopulationPerHouse;
                h = h * 31 + TimeLimitMinutes;
                h = h * 31 + (RegicideEnabled ? 1 : 0);
                return h;
            }
        }

        // A fresh, non-negative seed that is never the legacy "-1 = random"
        // marker.
        public static int NewRandomSeed()
        {
            return Guid.NewGuid().GetHashCode() & 0x7FFFFFFF;
        }

        // Adapter for every existing entry point (BeginMatch,
        // BeginScenarioMatch, BeginNetworkMatch, custom scenarios): builds
        // the configuration those callers used to imply piecemeal.
        //  - seed: explicit seed if >= 0; else a LAN-agreed seed
        //    (NetworkMatch.PendingSeed); else the map's fixed ResourceSeed
        //    if it has one; else a fresh random one, chosen ONCE here.
        //  - civs: player's pick is authoritative, the other slots resolve
        //    around it exactly as CivilizationSetup always did.
        public static MatchConfiguration Create(
            MapId map,
            CivilizationId playerCivilization,
            CivilizationId aiCivilization,
            bool thirdFaction = false,
            CivilizationId enemy2Civilization = CivilizationId.Rajput,
            int seed = -1,
            bool secondSlotIsHuman = false)
        {
            MapDefinitionData mapData = MapRegistry.Get(map);

            var taken = new HashSet<CivilizationId> { playerCivilization };
            CivilizationId enemyCiv = CivilizationSetup.ResolveDistinctCivilization(aiCivilization, taken);
            taken.Add(enemyCiv);

            var config = new MatchConfiguration
            {
                Map = map,
                Seed = seed >= 0 ? seed
                    : NetworkMatch.PendingSeed.HasValue ? NetworkMatch.PendingSeed.Value
                    : mapData.ResourceSeed != -1 ? mapData.ResourceSeed
                    : NewRandomSeed(),
                LocalFaction = NetworkMatch.IsActive ? NetworkMatch.LocalFaction : FactionId.Player,
                TimeLimitMinutes = GameSettings.TimeLimitMinutes,
                RegicideEnabled = GameSettings.RegicideEnabled,
            };

            config.Slots.Add(new MatchSlot { Faction = FactionId.Player, Type = SlotType.Human, Civilization = playerCivilization, Team = 0 });
            config.Slots.Add(new MatchSlot { Faction = FactionId.Enemy, Type = secondSlotIsHuman ? SlotType.Human : SlotType.AI, Civilization = enemyCiv, Team = 1 });
            if (thirdFaction)
            {
                CivilizationId enemy2Civ = CivilizationSetup.ResolveDistinctCivilization(enemy2Civilization, taken);
                config.Slots.Add(new MatchSlot { Faction = FactionId.Enemy2, Type = SlotType.AI, Civilization = enemy2Civ, Team = 2 });
            }
            else
            {
                config.Slots.Add(new MatchSlot { Faction = FactionId.Enemy2, Type = SlotType.Closed, Civilization = enemyCiv, Team = 2 });
            }

            return config;
        }
    }
}
