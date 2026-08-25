using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Phase 6 architecture migration, step 1: reads the 4 CSVs under
// Assets/Design/Data/ (civ_bonus_template, tech_tree_template,
// unit_roster_template, counter_matrix_template) and generates the
// corresponding ScriptableObject .asset files under
// Assets/Scripts/Data/Generated/, using the schema already scaffolded in
// Assets/Scripts/Data/Scripts/ (TechNode/UnitDefinition/
// CivilizationDefinition/CounterMatrix - user's own aef6d82 commit).
//
// This is data authoring only - it does not touch any live gameplay code
// (Barracks/factories/CombatBonus/etc. still read from the existing
// hardcoded C# systems). Wiring the game to actually READ from these
// generated assets is a separate, later migration step, deliberately not
// done here given how much surface area that touches.
//
// Re-running this is safe/idempotent: existing assets at a given path are
// updated in place (by re-populating their fields) rather than
// duplicated, so CSV edits can be iterated on without orphaning old
// assets or losing any Inspector-only fields this script doesn't set
// (icon/prefab/emblem references, if hand-assigned later).
public static class CsvToScriptableObject
{
    private const string CivBonusCsvPath = "Assets/Design/Data/civ_bonus_template.csv";
    private const string TechTreeCsvPath = "Assets/Design/Data/tech_tree_template.csv";
    private const string UnitRosterCsvPath = "Assets/Design/Data/unit_roster_template.csv";
    private const string CounterMatrixCsvPath = "Assets/Design/Data/counter_matrix_template.csv";
    // Phase 2 addition - no counterpart in the original Phase 1 CSV set,
    // see AgeProfileDefinition.cs.
    private const string AgeProfileCsvPath = "Assets/Design/Data/age_profile_template.csv";

    // Under Assets/Resources/ (not Assets/Scripts/Data/) so Phase 2's
    // runtime code can load these via Resources.Load/LoadAll - a build
    // can't AssetDatabase.LoadAssetAtPath outside the Editor, and these
    // assets aren't scene-referenced anywhere to pull them in otherwise.
    private const string RootFolder = "Assets/Resources/Data/Generated";
    private const string TechFolder = RootFolder + "/Techs";
    private const string UnitFolder = RootFolder + "/Units";
    private const string CivFolder = RootFolder + "/Civilizations";
    private const string AgeFolder = RootFolder + "/Ages";

    // Unique techs (civ_bonus_template's UniqueTech column) don't carry
    // structured cost/time data the way tech_tree_template's rows do -
    // it's a single free-text cell like "Arthashastra Statecraft (all
    // Age-up research times -25%)". Matches this session's own earlier
    // UniqueTechDefinition convention (150 Gold, ~30s) for the 3 civs that
    // already had one, applied uniformly to the 2 new civs' unique techs
    // too since neither the research pack nor the peer's design specified
    // a different cost for them.
    private const int UniqueTechGoldCost = 150;
    private const int UniqueTechResearchTime = 30;

    [MenuItem("BharatRTS/Generate Data Assets From CSV")]
    public static void GenerateAll()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(TechFolder);
        EnsureFolder(UnitFolder);
        EnsureFolder(CivFolder);
        EnsureFolder(AgeFolder);

        Dictionary<string, TechNode> techLookup = GenerateTechNodes();
        Dictionary<string, UnitDefinition> unitLookup = GenerateUnitDefinitions();
        GenerateCivilizations(techLookup, unitLookup);
        GenerateCounterMatrix();
        GenerateAgeProfiles();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("BharatRTS: data asset generation complete.");
    }

    // --- Tech tree ---

    private static Dictionary<string, TechNode> GenerateTechNodes()
    {
        var lookup = new Dictionary<string, TechNode>();
        List<Dictionary<string, string>> rows = ReadCsv(TechTreeCsvPath);

        // Two passes: create/update every node first (so prerequisite
        // references below can always resolve, regardless of CSV row
        // order), then wire prerequisites in a second pass.
        foreach (Dictionary<string, string> row in rows)
        {
            string techId = row["TechID"];
            TechNode node = LoadOrCreate<TechNode>(TechFolder, techId);
            node.techId = techId;
            node.displayName = row["DisplayName"];
            node.description = row["EffectDescription"];
            node.age = ParseInt(row["Age"], 1);
            node.cost = new ResourceCost
            {
                food = ParseInt(row["FoodCost"], 0),
                wood = ParseInt(row["WoodCost"], 0),
                gold = ParseInt(row["GoldCost"], 0),
                stone = 0,
            };
            node.researchTimeSeconds = ParseFloat(row["ResearchTime"], 0f);
            node.effects = BuildEffects(row["EffectType"], row["EffectMagnitude"], row["EffectDescription"]);
            EditorUtility.SetDirty(node);
            lookup[techId] = node;
        }

        foreach (Dictionary<string, string> row in rows)
        {
            TechNode node = lookup[row["TechID"]];
            node.prerequisites.Clear();
            foreach (string prereqId in SplitList(row["PrerequisiteTechIDs"]))
            {
                if (lookup.TryGetValue(prereqId, out TechNode prereq))
                {
                    node.prerequisites.Add(prereq);
                }
                else
                {
                    Debug.LogWarning($"BharatRTS CSV import: tech '{row["TechID"]}' references unknown prerequisite '{prereqId}'.");
                }
            }
            EditorUtility.SetDirty(node);
        }

        return lookup;
    }

    // --- Unit roster ---

    private static Dictionary<string, UnitDefinition> GenerateUnitDefinitions()
    {
        var lookup = new Dictionary<string, UnitDefinition>();
        foreach (Dictionary<string, string> row in ReadCsv(UnitRosterCsvPath))
        {
            string unitId = row["UnitID"];
            UnitDefinition unit = LoadOrCreate<UnitDefinition>(UnitFolder, unitId);
            unit.unitId = unitId;
            unit.displayName = row["DisplayName"];
            unit.category = ParseEnum(row["Category"], UnitCategory.Infantry);
            unit.ageRequirement = ParseInt(row["Age"], 1);
            unit.cost = new ResourceCost
            {
                food = ParseInt(row["FoodCost"], 0),
                wood = ParseInt(row["WoodCost"], 0),
                gold = ParseInt(row["GoldCost"], 0),
                stone = 0,
            };
            unit.trainTimeSeconds = ParseFloat(row["TrainTime"], 0f);
            unit.maxHP = ParseFloat(row["HP"], 0f);
            unit.attackDamage = ParseFloat(row["Attack"], 0f);
            unit.attackType = ParseEnum(row["AttackType"], DamageType.Melee);
            unit.meleeArmor = ParseFloat(row["MeleeArmor"], 0f);
            unit.pierceArmor = ParseFloat(row["PierceArmor"], 0f);
            unit.moveSpeed = ParseFloat(row["MoveSpeed"], 0f);
            unit.attackRange = ParseFloat(row["AttackRange"], 0f);
            unit.isUniqueUnit = ParseBool(row["IsUniqueToCiv"]);
            unit.civOwnerId = row["CivOwnerID"];
            unit.counterNotes = row["CounterNotes"];
            unit.historicalBasis = row["HistoricalBasis"];
            EditorUtility.SetDirty(unit);
            lookup[unitId] = unit;
        }
        return lookup;
    }

    // --- Civilizations ---

    // civ_bonus_template.csv packs 4 different row shapes under one
    // header, distinguished by which columns are populated rather than a
    // dedicated "row type" column: a plain BonusDescription row is a
    // passive bonus, a populated UniqueUnit cell references an already-
    // generated UnitDefinition by display name, a populated UniqueTech
    // cell describes a one-time tech to generate here, and a populated
    // TeamBonus cell is the civ's single team bonus. Multiple rows share
    // the same CivID, so this groups by CivID first rather than assuming
    // one row per civ.
    private static void GenerateCivilizations(Dictionary<string, TechNode> techLookup, Dictionary<string, UnitDefinition> unitLookup)
    {
        var rowsByCiv = new Dictionary<string, List<Dictionary<string, string>>>();
        foreach (Dictionary<string, string> row in ReadCsv(CivBonusCsvPath))
        {
            string civId = row["CivID"];
            if (!rowsByCiv.TryGetValue(civId, out List<Dictionary<string, string>> rows))
            {
                rows = new List<Dictionary<string, string>>();
                rowsByCiv[civId] = rows;
            }
            rows.Add(row);
        }

        foreach (KeyValuePair<string, List<Dictionary<string, string>>> entry in rowsByCiv)
        {
            string civId = entry.Key;
            CivilizationDefinition civ = LoadOrCreate<CivilizationDefinition>(CivFolder, civId);
            civ.civId = civId;
            civ.displayName = entry.Value[0]["CivName"];
            civ.passiveBonuses.Clear();
            civ.uniqueUnits.Clear();
            civ.uniqueTechs.Clear();
            // Unlike the 3 lists above, flavorText was never reset here -
            // every re-run of this idempotent generator kept re-appending
            // the same rows' text on top of what a PRIOR run had already
            // written, growing unboundedly. Found via a diff on this pass's
            // regeneration (chola.asset's flavorText had visibly duplicated
            // content) - fixed by resetting it the same way the lists above
            // already are.
            civ.flavorText = string.Empty;

            foreach (Dictionary<string, string> row in entry.Value)
            {
                if (!string.IsNullOrEmpty(row["UniqueUnit"]))
                {
                    UnitDefinition unit = FindUnitByDisplayNamePrefix(unitLookup, row["UniqueUnit"]);
                    if (unit != null)
                    {
                        civ.uniqueUnits.Add(unit);
                    }
                    else
                    {
                        Debug.LogWarning($"BharatRTS CSV import: civ '{civId}' unique unit cell '{row["UniqueUnit"]}' didn't match any generated UnitDefinition by name.");
                    }
                }
                else if (!string.IsNullOrEmpty(row["UniqueTech"]))
                {
                    TechNode tech = GenerateUniqueTech(civId, row["UniqueTech"]);
                    civ.uniqueTechs.Add(tech);
                    techLookup[tech.techId] = tech;
                }
                else if (!string.IsNullOrEmpty(row["TeamBonus"]))
                {
                    List<StatModifier> parsed = ParseNumericEffect(row["TeamBonus"], row["TeamBonus"]);
                    civ.teamBonus = parsed.Count > 0
                        ? parsed[0]
                        : new StatModifier { applyToAllCategories = true, stat = StatType.HP, operation = ModifierOp.Add, value = 0f };
                    civ.flavorText = string.IsNullOrEmpty(civ.flavorText)
                        ? $"Team bonus: {row["TeamBonus"]}"
                        : civ.flavorText + $"\nTeam bonus: {row["TeamBonus"]}";
                }
                else if (!string.IsNullOrEmpty(row["BonusDescription"]))
                {
                    civ.passiveBonuses.AddRange(BuildPassiveBonus(row));
                    civ.flavorText = string.IsNullOrEmpty(civ.flavorText)
                        ? row["BonusDescription"]
                        : civ.flavorText + "\n" + row["BonusDescription"];
                }
            }

            EditorUtility.SetDirty(civ);
        }
    }

    // civ_bonus_template's UniqueTech cell is one free-text sentence like
    // "Chola Trade Networks (existing, port as-is: Market spread 70/30 ->
    // 85/15)" - the display name is everything before the first "(",
    // trimmed; the rest is kept verbatim as the description rather than
    // attempting to parse it into a structured effect, since these
    // sentences don't follow the tech_tree_template's uniform EffectType/
    // EffectMagnitude columns the way shared techs do.
    private static TechNode GenerateUniqueTech(string civId, string cellText)
    {
        int parenIndex = cellText.IndexOf('(');
        string name = (parenIndex > 0 ? cellText.Substring(0, parenIndex) : cellText).Trim();
        string techId = civId + "_unique_tech";

        TechNode node = LoadOrCreate<TechNode>(TechFolder, techId);
        node.techId = techId;
        node.displayName = name;
        node.description = cellText;
        node.age = 2;
        node.cost = new ResourceCost { food = 0, wood = 0, gold = UniqueTechGoldCost, stone = 0 };
        node.researchTimeSeconds = UniqueTechResearchTime;
        EditorUtility.SetDirty(node);
        return node;
    }

    // civ_bonus_template's NumericEffect column carries a short summary
    // ("+15% gather rate", "+20% dmg / +15% HP") but sometimes lacks the
    // unit-type context BonusDescription's fuller sentence has (e.g.
    // NumericEffect "+15% move speed" vs. BonusDescription "Workers move
    // 15% faster (Mauryan road network)" - only the latter says "Workers"),
    // so both are consulted, but NOT symmetrically: only NumericEffect gets
    // split on "/" (its "/" is a genuine multi-effect separator, e.g. "+20%
    // dmg / +15% HP"), while BonusDescription's prose routinely contains
    // unrelated "/"s (e.g. "Villagers/Workers", "Fishing Boats/War
    // Galleys") that would silently fragment into bogus extra entries if
    // split the same way. BonusDescription is used whole, as read-only
    // context for unit-category detection and as a stat-keyword fallback
    // only when a given NumericEffect part carries no keyword of its own.
    private static List<StatModifier> BuildPassiveBonus(Dictionary<string, string> row)
    {
        return ParseNumericEffect(row["NumericEffect"], row["BonusDescription"]);
    }

    private static readonly Regex PercentPattern = new Regex(@"([+-]?\d+(?:\.\d+)?)\s*%", RegexOptions.IgnoreCase);
    private static readonly Regex FlatAddPattern = new Regex(@"([+-]?\d+(?:\.\d+)?)\s*flat", RegexOptions.IgnoreCase);

    // Not every civ_bonus_template row is representable as a StatModifier:
    // UnitCategory has no "Building" entry, so per-building costs (Houses/
    // Docks/fortifications/Market spread) and flat building stats (Tower
    // range) have no home here; probabilistic/structural mechanics
    // (dismount-survival chance, Age-skip, permanent scouted-position
    // memory) aren't a stat multiplier at all. Those are deliberately left
    // unrepresented (flavor text only) rather than forced into this
    // schema - same as this project's existing pattern of handling
    // one-off mechanics as bespoke code (UniqueTechDefinition) instead of
    // stretching a generic data schema to cover everything. A future pass
    // wiring these into live gameplay code will need hand-written hooks
    // for that remainder, the same way Rajput's dismount-survival and
    // Vijayanagara's fortification-HP bonus already are.
    private static List<StatModifier> ParseNumericEffect(string numericEffect, string descriptionContext)
    {
        List<StatModifier> results = new List<StatModifier>();
        if (string.IsNullOrEmpty(numericEffect))
        {
            return results;
        }

        string descLower = (descriptionContext ?? string.Empty).ToLowerInvariant();

        foreach (string part in numericEffect.Split('/'))
        {
            string partLower = part.ToLowerInvariant();

            Match percentMatch = PercentPattern.Match(part);
            Match flatMatch = FlatAddPattern.Match(part);

            float pct;
            ModifierOp op;
            bool haveNumber;
            if (percentMatch.Success)
            {
                pct = float.Parse(percentMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                op = ModifierOp.Multiply;
                haveNumber = true;
            }
            else if (flatMatch.Success)
            {
                pct = float.Parse(flatMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                op = ModifierOp.Add;
                haveNumber = true;
            }
            else
            {
                pct = 0f;
                op = ModifierOp.Add;
                haveNumber = false;
            }

            if (!haveNumber)
            {
                continue;
            }

            // Building-specific phrasing ("Wood cost on Houses", "Dock
            // cost", "Towers get +15% HP") has no representable target (no
            // Building entry in UnitCategory) for ANY stat, not just cost/
            // time - excluded before stat detection runs. Deliberately
            // checked against this split PART's own text only, not the
            // shared description: a row like "Docks cost 25% less Wood and
            // Fishing Boats/War Galleys train 15% faster" mentions "Docks"
            // for an earlier, unrelated part of the same sentence, and
            // that mention must not also suppress this part's own naval
            // train-speed effect.
            bool partMentionsBuilding = partLower.Contains("house") || partLower.Contains("dock")
                || partLower.Contains("market") || partLower.Contains("wall")
                || partLower.Contains("gate") || partLower.Contains("tower");
            if (partMentionsBuilding)
            {
                continue;
            }

            StatType? stat = DetectStat(partLower);
            if (stat == null)
            {
                // Fall back to the full description only when the part
                // itself carries no keyword - never lets a keyword that
                // belongs to a DIFFERENT part of the same multi-effect row
                // (e.g. "damage" bleeding into the "+15% HP" part) win.
                stat = DetectStat(descLower);
            }

            if (stat == null)
            {
                continue;
            }

            float value = op == ModifierOp.Multiply ? 1f + (pct / 100f) : pct;
            if (stat == StatType.TrainTime && op == ModifierOp.Multiply && (partLower.Contains("faster") || partLower.Contains("speed") || descLower.Contains("faster") || descLower.Contains("speed")) && !(partLower.Contains("time") || descLower.Contains("time")))
            {
                // "+15% ... train speed" / "train X% faster" - percent
                // describes how much faster, i.e. the inverse of time, so
                // the multiplier is 1 - pct/100 rather than 1 + pct/100.
                value = 1f - (pct / 100f);
            }

            string combined = partLower + " " + descLower;
            UnitCategory category = UnitCategory.Infantry;
            bool allCategories = true;
            if (combined.Contains("cavalry"))
            {
                category = UnitCategory.Cavalry;
                allCategories = false;
            }
            else if (combined.Contains("naval") || combined.Contains("ship") || combined.Contains("boat"))
            {
                category = UnitCategory.Naval;
                allCategories = false;
            }
            else if (combined.Contains("worker"))
            {
                category = UnitCategory.Support;
                allCategories = false;
            }
            else if (combined.Contains("archer"))
            {
                category = UnitCategory.Archer;
                allCategories = false;
            }

            results.Add(new StatModifier
            {
                applyToAllCategories = allCategories,
                targetCategory = category,
                stat = stat.Value,
                operation = op,
                value = value,
            });
        }

        return results;
    }

    // Ordered keyword match - order matters when a sentence mentions more
    // than one stat word (e.g. "...20% more damage, +15% max health..."
    // contains both "damage" and "health"); callers pass the narrowest
    // available text first (one split part) before falling back to the
    // full sentence, so a keyword belonging to a different part of the
    // same row doesn't win.
    private static StatType? DetectStat(string lowerText)
    {
        if (lowerText.Contains("gather"))
        {
            return StatType.ResourceRate;
        }
        if (lowerText.Contains("build cost") || lowerText.Contains("gold cost") || lowerText.Contains("wood cost") || lowerText.Contains("stone cost"))
        {
            return StatType.ResourceCost;
        }
        if (lowerText.Contains("train") && lowerText.Contains("time"))
        {
            return StatType.TrainTime;
        }
        if (lowerText.Contains("train") && (lowerText.Contains("faster") || lowerText.Contains("speed")))
        {
            return StatType.TrainTime;
        }
        if (lowerText.Contains("move") && (lowerText.Contains("faster") || lowerText.Contains("speed")))
        {
            return StatType.MoveSpeed;
        }
        if (lowerText.Contains("dmg") || lowerText.Contains("damage"))
        {
            return StatType.Attack;
        }
        if (lowerText.Contains("hp") || lowerText.Contains("health"))
        {
            return StatType.HP;
        }
        return null;
    }

    // civ_bonus_template references unique units by a descriptive phrase
    // ("Chola Naval Raider (Archer-class, 22HP/6dmg/range7...)"), not the
    // unit_roster_template's exact UnitID - matches by checking whether
    // the roster unit's displayName is a prefix of the cell text, which
    // holds for every row in this pass's actual CSV content.
    private static UnitDefinition FindUnitByDisplayNamePrefix(Dictionary<string, UnitDefinition> unitLookup, string cellText)
    {
        foreach (UnitDefinition unit in unitLookup.Values)
        {
            if (cellText.StartsWith(unit.displayName, StringComparison.OrdinalIgnoreCase))
            {
                return unit;
            }
        }
        return null;
    }

    // --- Counter matrix ---

    private static void GenerateCounterMatrix()
    {
        string path = RootFolder + "/CounterMatrix.asset";
        CounterMatrix matrix = AssetDatabase.LoadAssetAtPath<CounterMatrix>(path);
        if (matrix == null)
        {
            matrix = ScriptableObject.CreateInstance<CounterMatrix>();
            AssetDatabase.CreateAsset(matrix, path);
        }

        matrix.entries.Clear();
        foreach (Dictionary<string, string> row in ReadCsv(CounterMatrixCsvPath))
        {
            // ParseEnum silently falls back to Infantry on an unparseable
            // value - previously let a "Building" cell (before UnitCategory
            // had that entry) silently collide with a real Infantry row
            // instead of erroring, which went undetected through Phase 1's
            // count-only verification. Explicit warnings now, so a future
            // schema/CSV mismatch surfaces immediately instead of silently
            // mis-keying an entry.
            if (!Enum.TryParse(row["Attacker"].Replace("-", "").Trim(), true, out UnitCategory attacker))
            {
                Debug.LogWarning($"BharatRTS CSV import: counter_matrix_template.csv Attacker '{row["Attacker"]}' didn't parse as a UnitCategory - defaulting to Infantry.");
            }
            if (!Enum.TryParse(row["Defender"].Replace("-", "").Trim(), true, out UnitCategory defender))
            {
                Debug.LogWarning($"BharatRTS CSV import: counter_matrix_template.csv Defender '{row["Defender"]}' didn't parse as a UnitCategory - defaulting to Infantry.");
            }
            matrix.entries.Add(new CounterMatrix.CounterEntry
            {
                attacker = attacker,
                defender = defender,
                damageMultiplier = ParseFloat(row["DamageMultiplier"], 1f),
            });
        }
        matrix.BuildLookup();
        EditorUtility.SetDirty(matrix);
    }

    // --- Age profiles (Phase 2 addition - see AgeProfileDefinition.cs) ---

    private static void GenerateAgeProfiles()
    {
        foreach (Dictionary<string, string> row in ReadCsv(AgeProfileCsvPath))
        {
            string ageId = row["AgeID"];
            AgeProfileDefinition age = LoadOrCreate<AgeProfileDefinition>(AgeFolder, ageId);
            age.ageId = ageId;
            age.displayName = row["DisplayName"];
            age.woodCost = ParseFloat(row["WoodCost"], 0f);
            age.stoneCost = ParseFloat(row["StoneCost"], 0f);
            age.researchTime = ParseFloat(row["ResearchTime"], 0f);
            age.gatherRateMultiplier = ParseFloat(row["GatherRateMultiplier"], 1f);
            age.maxHealthMultiplier = ParseFloat(row["MaxHealthMultiplier"], 1f);
            age.trainTimeMultiplier = ParseFloat(row["TrainTimeMultiplier"], 1f);
            EditorUtility.SetDirty(age);
        }
    }

    // --- Effect parsing (tech_tree_template only - unique techs/passive
    // bonuses stay free-text, see the comments above each) ---

    private static List<StatModifier> BuildEffects(string effectTypeRaw, string magnitudeRaw, string description)
    {
        var effects = new List<StatModifier>();
        if (!TryParseStatType(effectTypeRaw, out StatType statType))
        {
            return effects;
        }

        float value = ParseFirstNumber(magnitudeRaw);
        bool isPercent = magnitudeRaw.Contains("%");
        bool hasCategory = TryFindCategoryMention(description, out UnitCategory category);

        effects.Add(new StatModifier
        {
            applyToAllCategories = !hasCategory,
            targetCategory = category,
            stat = statType,
            operation = isPercent ? ModifierOp.Multiply : ModifierOp.Add,
            // Percent magnitudes are stored as a multiplier (e.g. "+15%"
            // -> 1.15) so StatModifier.operation == Multiply reads
            // naturally at the consuming end; flat magnitudes are stored
            // as their literal add/subtract value.
            value = isPercent ? 1f + (value / 100f) : value,
        });
        return effects;
    }

    private static bool TryParseStatType(string raw, out StatType statType)
    {
        return Enum.TryParse(raw, ignoreCase: true, out statType);
    }

    private static bool TryFindCategoryMention(string description, out UnitCategory category)
    {
        foreach (UnitCategory candidate in Enum.GetValues(typeof(UnitCategory)))
        {
            if (description.IndexOf(candidate.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
            {
                category = candidate;
                return true;
            }
        }
        category = default;
        return false;
    }

    private static float ParseFirstNumber(string text)
    {
        Match match = Regex.Match(text, @"-?\d+(\.\d+)?");
        return match.Success ? float.Parse(match.Value) : 0f;
    }

    // --- Generic asset load-or-create ---

    private static T LoadOrCreate<T>(string folder, string id) where T : ScriptableObject
    {
        string path = $"{folder}/{id}.asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }
        AssetDatabase.CreateFolder(parent, leaf);
    }

    // --- CSV parsing (handles quoted fields with embedded commas, e.g.
    // "AoE2 Persians-style infra bonus, but structural not flat") ---

    private static List<Dictionary<string, string>> ReadCsv(string path)
    {
        var rows = new List<Dictionary<string, string>>();
        string[] lines = File.ReadAllLines(path);
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

    // --- Small parsing helpers ---

    private static int ParseInt(string s, int fallback) => int.TryParse(s, out int v) ? v : fallback;
    private static float ParseFloat(string s, float fallback) => float.TryParse(s, out float v) ? v : fallback;
    private static bool ParseBool(string s) => s.Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase);
    private static T ParseEnum<T>(string s, T fallback) where T : struct => Enum.TryParse(s.Replace("-", "").Trim(), true, out T v) ? v : fallback;

    private static IEnumerable<string> SplitList(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            yield break;
        }
        foreach (string part in s.Split(';', ','))
        {
            string trimmed = part.Trim();
            if (trimmed.Length > 0)
            {
                yield return trimmed;
            }
        }
    }
}
