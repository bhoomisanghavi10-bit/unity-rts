using System;
using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Match
{
    // Item 6 (Scenario Editor, docs/PARTIAL_ELEMENTS_FIX_PLAN.md), light path:
    // lets a mission be authored as data (3 CSVs) instead of hand-written C#
    // like every mission in ScenarioRegistry.cs today. Unlike this project's
    // other CSV-driven data (TechNode/UnitDefinition/CivilizationDefinition,
    // baked at Editor time into ScriptableObject assets by
    // CsvToScriptableObject.cs), ScenarioDefinition.BuildObjectives/
    // BuildTriggers are System.Func<> delegates - Unity can't serialize a
    // delegate into an asset, so there's no Editor-time bake step possible
    // here. These 3 CSVs are read and turned into real MissionObjective/
    // MissionTrigger closures at RUNTIME instead, via Resources.Load<TextAsset>
    // (not File.ReadAllLines against an Assets/Design/Data path the way
    // CsvToScriptableObject.ReadCsv does - that only works in the Editor, not
    // a built player).
    //
    // A CSV mission is restricted to the fixed vocabulary below (ObjectiveKind/
    // TriggerKind) - a disclosed limitation, not literally "any mission logic
    // a non-programmer can invent." The vocabulary was sized directly off the
    // 3 real hand-coded missions in ScenarioRegistry.cs (every objective/
    // trigger they use reduces to one of these shapes) plus 2 extra shapes
    // (ResourceThreshold/PopulationThreshold) the Roadmap's own already-scoped
    // Tutorial item explicitly names as objectives it will want.
    public enum ObjectiveKind
    {
        SurviveSeconds,
        BuildingCountThreshold,
        ResourceThreshold,
        PopulationThreshold,
        DestroyScriptedTarget,
    }

    public enum TriggerKind
    {
        GrantResourceAtTime,
        RepeatingGrantResource,
    }

    public static class MissionCsvLoader
    {
        private const string DefinitionsPath = "Data/Missions/mission_definitions";
        private const string ObjectivesPath = "Data/Missions/mission_objectives";
        private const string TriggersPath = "Data/Missions/mission_triggers";

        public static List<ScenarioDefinition> LoadAll()
        {
            return BuildFromCsv(
                ReadResourceText(DefinitionsPath),
                ReadResourceText(ObjectivesPath),
                ReadResourceText(TriggersPath));
        }

        // Internal (not private) so EditMode tests can drive this directly
        // with small in-memory CSV strings instead of depending on the real
        // Resources-loaded files - same "separate the testable logic from
        // its I/O wrapper" convention this project's internal Tick methods
        // already use elsewhere (see AssemblyInfo.cs's InternalsVisibleTo
        // grant).
        internal static List<ScenarioDefinition> BuildFromCsv(string definitionsCsv, string objectivesCsv, string triggersCsv)
        {
            var result = new List<ScenarioDefinition>();

            List<Dictionary<string, string>> definitionRows = ReadCsv(definitionsCsv);
            List<Dictionary<string, string>> objectiveRows = ReadCsv(objectivesCsv);
            List<Dictionary<string, string>> triggerRows = ReadCsv(triggersCsv);

            foreach (Dictionary<string, string> defRow in definitionRows)
            {
                string missionId = defRow["Id"];
                List<Dictionary<string, string>> myObjectives = FilterByMissionId(objectiveRows, missionId);
                List<Dictionary<string, string>> myTriggers = FilterByMissionId(triggerRows, missionId);

                result.Add(new ScenarioDefinition
                {
                    Id = missionId,
                    Title = defRow["Title"],
                    FlavorText = defRow["FlavorText"],
                    PlayerCivilization = ParseEnum(defRow["PlayerCivilization"], CivilizationId.Chola),
                    AiCivilization = ParseEnum(defRow["AiCivilization"], CivilizationId.Vijayanagara),
                    Map = ParseEnum(defRow["Map"], MapId.RiverValley),
                    VictoryText = defRow.TryGetValue("VictoryText", out string vt) ? vt : null,
                    DefeatText = defRow.TryGetValue("DefeatText", out string dt) ? dt : null,
                    BuildObjectives = () => BuildObjectives(myObjectives),
                    BuildTriggers = () => BuildTriggers(myTriggers),
                });
            }

            return result;
        }

        // --- Objectives ---

        private static List<MissionObjective> BuildObjectives(List<Dictionary<string, string>> rows)
        {
            float missionStart = Time.time;
            var objectives = new List<MissionObjective>();

            foreach (Dictionary<string, string> row in rows)
            {
                ObjectiveKind kind = ParseEnum(row["Kind"], ObjectiveKind.SurviveSeconds);
                string description = row["Description"];
                string completeText = row.TryGetValue("CompleteText", out string ct) && ct.Length > 0 ? ct : null;

                Func<bool> isComplete;
                switch (kind)
                {
                    case ObjectiveKind.SurviveSeconds:
                    {
                        float seconds = ParseFloat(row["Param1"]);
                        isComplete = () => Time.time - missionStart >= seconds;
                        break;
                    }
                    case ObjectiveKind.BuildingCountThreshold:
                    {
                        string buildingKind = row["Param1"];
                        int count = ParseInt(row["Param2"]);
                        FactionId faction = ParseEnum(row.GetValueOrDefault("Param3", "Player"), FactionId.Player);
                        isComplete = () => CountCompleteBuildingsOfKind(buildingKind, faction) >= count;
                        break;
                    }
                    case ObjectiveKind.ResourceThreshold:
                    {
                        ResourceType resourceType = ParseEnum(row["Param1"], ResourceType.Food);
                        float amount = ParseFloat(row["Param2"]);
                        FactionId faction = ParseEnum(row.GetValueOrDefault("Param3", "Player"), FactionId.Player);
                        isComplete = () => ResourceStockpile.For(faction) != null
                            && ResourceStockpile.For(faction).GetTotal(resourceType) >= amount;
                        break;
                    }
                    case ObjectiveKind.PopulationThreshold:
                    {
                        int amount = ParseInt(row["Param1"]);
                        FactionId faction = ParseEnum(row.GetValueOrDefault("Param2", "Player"), FactionId.Player);
                        isComplete = () => Population.Current(faction) >= amount;
                        break;
                    }
                    case ObjectiveKind.DestroyScriptedTarget:
                    {
                        // Spawned here (BuildObjectives itself), not inside
                        // the closure - same reasoning ScenarioRegistry's own
                        // Chola Expansion documents: the closure needs to
                        // capture a specific Attackable reference, not
                        // re-derive "does this faction have zero of this
                        // building" (trivially true before the target even
                        // exists).
                        string buildingKind = row["Param1"];
                        FactionId targetFaction = ParseEnum(row["Param2"], FactionId.Enemy);
                        Vector3 position = ParseVector3(row["Param3"]);
                        GameObject targetGo = SpawnScriptedTarget(buildingKind, targetFaction, position);
                        Attackable targetAttackable = targetGo != null ? targetGo.GetComponent<Attackable>() : null;
                        isComplete = () => targetAttackable == null || targetAttackable.IsDead;
                        break;
                    }
                    default:
                        isComplete = () => true;
                        break;
                }

                objectives.Add(new MissionObjective(description, isComplete, completeText: completeText));
            }

            return objectives;
        }

        // Generic across every building type (Barracks/Farm/House/.../
        // TownCenter) via the shared "Site == null || Site.IsComplete"
        // ConstructionSite pattern every factory already follows, rather
        // than a hand-written switch checking each concrete type's own
        // IsComplete property.
        private static int CountCompleteBuildingsOfKind(string buildingKindName, FactionId faction)
        {
            int count = 0;
            foreach (Building building in Building.All)
            {
                if (building.GetType().Name != buildingKindName)
                {
                    continue;
                }

                if (!building.TryGetComponent(out FactionMember factionMember) || factionMember.Faction != faction)
                {
                    continue;
                }

                if (building.TryGetComponent(out ConstructionSite site) && !site.IsComplete)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        // Deliberately small - only the building kinds a scripted "destroy
        // this" target realistically needs. Extend as future CSV missions
        // need more.
        private static GameObject SpawnScriptedTarget(string buildingKindName, FactionId faction, Vector3 position)
        {
            switch (buildingKindName)
            {
                case "Barracks":
                {
                    GameObject go = BarracksFactory.Place(position, faction, 0.01f);
                    if (go.TryGetComponent(out ConstructionSite site))
                    {
                        site.CompleteImmediately();
                    }
                    return go;
                }
                case "TownCenter":
                    return TownCenterFactory.Place(position, faction);
                default:
                    Debug.LogWarning($"MissionCsvLoader: unsupported DestroyScriptedTarget building kind '{buildingKindName}'.");
                    return null;
            }
        }

        // --- Triggers ---

        private static List<MissionTrigger> BuildTriggers(List<Dictionary<string, string>> rows)
        {
            float missionStart = Time.time;
            var triggers = new List<MissionTrigger>();

            foreach (Dictionary<string, string> row in rows)
            {
                TriggerKind kind = ParseEnum(row["Kind"], TriggerKind.GrantResourceAtTime);
                string triggerId = row["TriggerId"];

                switch (kind)
                {
                    case TriggerKind.GrantResourceAtTime:
                    {
                        ResourceType resourceType = ParseEnum(row["Param1"], ResourceType.Gold);
                        float amount = ParseFloat(row["Param2"]);
                        float atSeconds = ParseFloat(row["Param3"]);
                        FactionId faction = ParseEnum(row.GetValueOrDefault("Param4", "Player"), FactionId.Player);
                        triggers.Add(new MissionTrigger(
                            triggerId,
                            () => Time.time - missionStart >= atSeconds,
                            () => ResourceStockpile.For(faction).Add(resourceType, amount)));
                        break;
                    }
                    case TriggerKind.RepeatingGrantResource:
                    {
                        ResourceType resourceType = ParseEnum(row["Param1"], ResourceType.Gold);
                        float amount = ParseFloat(row["Param2"]);
                        float intervalSeconds = ParseFloat(row["Param3"]);
                        int repeatCount = ParseInt(row["Param4"]);
                        FactionId faction = ParseEnum(row.GetValueOrDefault("Param5", "Player"), FactionId.Player);
                        // Expands to `repeatCount` discrete one-shot triggers,
                        // same convention Defend Hampi's own hand-written
                        // for-loop already establishes (MissionTrigger only
                        // ever fires once per Id, by design).
                        for (int i = 1; i <= repeatCount; i++)
                        {
                            float fireAt = i * intervalSeconds;
                            triggers.Add(new MissionTrigger(
                                triggerId + "_" + i,
                                () => Time.time - missionStart >= fireAt,
                                () => ResourceStockpile.For(faction).Add(resourceType, amount)));
                        }
                        break;
                    }
                }
            }

            return triggers;
        }

        // --- CSV grouping/parsing ---

        private static List<Dictionary<string, string>> FilterByMissionId(List<Dictionary<string, string>> rows, string missionId)
        {
            var result = new List<Dictionary<string, string>>();
            foreach (Dictionary<string, string> row in rows)
            {
                if (row["MissionId"] == missionId)
                {
                    result.Add(row);
                }
            }
            return result;
        }

        private static string ReadResourceText(string resourcePath)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            return asset != null ? asset.text : string.Empty;
        }

        // Small self-contained quoted-field-aware CSV parser (mirrors
        // CsvToScriptableObject.ParseCsvLine's logic) - can't reuse that
        // method directly, it lives in the Editor-only assembly
        // (Assets/Editor/KingdomsOfBharat.Editor.asmdef), unreachable from
        // this Runtime code.
        internal static List<Dictionary<string, string>> ReadCsv(string text)
        {
            var rows = new List<Dictionary<string, string>>();
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            if (lines.Length == 0)
            {
                return rows;
            }

            string[] headers = ParseCsvLine(lines[0]);
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                string[] fields = ParseCsvLine(lines[i]);
                var row = new Dictionary<string, string>();
                for (int c = 0; c < headers.Length; c++)
                {
                    row[headers[c]] = c < fields.Length ? fields[c] : string.Empty;
                }
                rows.Add(row);
            }
            return rows;
        }

        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            fields.Add(current.ToString());
            return fields.ToArray();
        }

        private static Vector3 ParseVector3(string s)
        {
            string[] parts = s.Split(',');
            return new Vector3(
                parts.Length > 0 ? ParseFloat(parts[0]) : 0f,
                parts.Length > 1 ? ParseFloat(parts[1]) : 0f,
                parts.Length > 2 ? ParseFloat(parts[2]) : 0f);
        }

        private static int ParseInt(string s) => int.TryParse(s, out int v) ? v : 0;
        private static float ParseFloat(string s) => float.TryParse(s, out float v) ? v : 0f;

        private static T ParseEnum<T>(string s, T fallback) where T : struct =>
            Enum.TryParse(s.Trim(), true, out T v) ? v : fallback;
    }
}
