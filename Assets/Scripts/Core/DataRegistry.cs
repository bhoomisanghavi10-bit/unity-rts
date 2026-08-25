using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Runtime access point for the CSV-generated ScriptableObject assets
    // under Assets/Resources/Data/Generated/ (CivilizationDefinition/
    // UnitDefinition/TechNode/CounterMatrix - see Assets/Editor/
    // CsvToScriptableObject.cs). Resources.LoadAll once per domain reload,
    // cached by id thereafter - callers should go through this rather than
    // calling Resources.Load directly, so there's one place to change
    // loading strategy later (e.g. Addressables) without touching every
    // system that reads civ/unit/tech data.
    public static class DataRegistry
    {
        private static Dictionary<string, CivilizationDefinition> _civilizations;
        private static Dictionary<string, UnitDefinition> _units;
        private static Dictionary<string, TechNode> _techs;
        private static Dictionary<string, AgeProfileDefinition> _ages;
        private static CounterMatrix _counterMatrix;

        public static CivilizationDefinition GetCivilization(string civId)
        {
            EnsureLoaded();
            return _civilizations.TryGetValue(civId, out CivilizationDefinition civ) ? civ : null;
        }

        public static UnitDefinition GetUnit(string unitId)
        {
            EnsureLoaded();
            return _units.TryGetValue(unitId, out UnitDefinition unit) ? unit : null;
        }

        public static TechNode GetTech(string techId)
        {
            EnsureLoaded();
            return _techs.TryGetValue(techId, out TechNode tech) ? tech : null;
        }

        public static AgeProfileDefinition GetAgeProfile(string ageId)
        {
            EnsureLoaded();
            return _ages.TryGetValue(ageId, out AgeProfileDefinition age) ? age : null;
        }

        public static CounterMatrix CounterMatrix
        {
            get
            {
                EnsureLoaded();
                return _counterMatrix;
            }
        }

        public static IReadOnlyCollection<CivilizationDefinition> AllCivilizations
        {
            get
            {
                EnsureLoaded();
                return _civilizations.Values;
            }
        }

        // Editor/tests may need a clean re-read after CSV regeneration
        // within the same domain (no reload in between).
        public static void ClearCache()
        {
            _civilizations = null;
            _units = null;
            _techs = null;
            _counterMatrix = null;
        }

        private static void EnsureLoaded()
        {
            if (_civilizations != null)
            {
                return;
            }

            _civilizations = new Dictionary<string, CivilizationDefinition>();
            foreach (CivilizationDefinition civ in Resources.LoadAll<CivilizationDefinition>("Data/Generated/Civilizations"))
            {
                _civilizations[civ.civId] = civ;
            }

            _units = new Dictionary<string, UnitDefinition>();
            foreach (UnitDefinition unit in Resources.LoadAll<UnitDefinition>("Data/Generated/Units"))
            {
                _units[unit.unitId] = unit;
            }

            _techs = new Dictionary<string, TechNode>();
            foreach (TechNode tech in Resources.LoadAll<TechNode>("Data/Generated/Techs"))
            {
                _techs[tech.techId] = tech;
            }

            _ages = new Dictionary<string, AgeProfileDefinition>();
            foreach (AgeProfileDefinition age in Resources.LoadAll<AgeProfileDefinition>("Data/Generated/Ages"))
            {
                _ages[age.ageId] = age;
            }

            CounterMatrix[] matrices = Resources.LoadAll<CounterMatrix>("Data/Generated");
            _counterMatrix = matrices.Length > 0 ? matrices[0] : null;
        }
    }
}
