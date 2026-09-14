using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Core
{
    // Wave 6 item 39 (Cheat codes): pure string -> command parsing, kept
    // completely separate from CheatCodes.cs's actual side-effecting
    // execution so every parse case is EditMode-testable with zero Unity
    // scene dependency, the same "pure/testable seam" convention as
    // MatchManager.EvaluateSkirmishOutcome or CommandBus.EnqueueAt.
    public enum CheatCommandKind
    {
        Unknown,
        Help,
        Resources,
        Age,
        Reveal,
        Spawn,
        Win,
        Lose,
    }

    public readonly struct CheatCommand
    {
        public readonly CheatCommandKind Kind;
        public readonly ResourceType Resource;
        public readonly bool GrantAllResources;
        public readonly float Amount;
        public readonly AgeId Age;
        public readonly string UnitType;
        public readonly int Count;
        public readonly string Error;

        public bool IsValid => Error == null;

        private CheatCommand(CheatCommandKind kind, ResourceType resource, bool grantAllResources, float amount, AgeId age, string unitType, int count, string error)
        {
            Kind = kind;
            Resource = resource;
            GrantAllResources = grantAllResources;
            Amount = amount;
            Age = age;
            UnitType = unitType;
            Count = count;
            Error = error;
        }

        public static CheatCommand Ok(CheatCommandKind kind, ResourceType resource = ResourceType.Wood, bool grantAllResources = false, float amount = 0f, AgeId age = AgeId.Ancient, string unitType = null, int count = 0)
        {
            return new CheatCommand(kind, resource, grantAllResources, amount, age, unitType, count, null);
        }

        public static CheatCommand Fail(string error)
        {
            return new CheatCommand(CheatCommandKind.Unknown, ResourceType.Wood, false, 0f, AgeId.Ancient, null, 0, error);
        }
    }

    public static class CheatCommandParser
    {
        private const float DefaultResourceAmount = 1000f;
        private const int DefaultSpawnCount = 1;

        // Never throws - every malformed input resolves to a CheatCommand
        // with a non-null Error instead, so the console UI (or a test) can
        // just check IsValid rather than wrapping every call in try/catch.
        public static CheatCommand Parse(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                return CheatCommand.Fail("Empty command.");
            }

            string[] tokens = rawInput.Trim().Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
            string verb = tokens[0].ToLowerInvariant();

            switch (verb)
            {
                case "help":
                    return CheatCommand.Ok(CheatCommandKind.Help);

                case "win":
                    return CheatCommand.Ok(CheatCommandKind.Win);

                case "lose":
                    return CheatCommand.Ok(CheatCommandKind.Lose);

                case "reveal":
                    return CheatCommand.Ok(CheatCommandKind.Reveal);

                case "resources":
                    return ParseAmount(tokens, ResourceType.Wood, allResources: true);

                case "wood":
                    return ParseAmount(tokens, ResourceType.Wood, allResources: false);

                case "food":
                    return ParseAmount(tokens, ResourceType.Food, allResources: false);

                case "gold":
                    return ParseAmount(tokens, ResourceType.Gold, allResources: false);

                case "stone":
                    return ParseAmount(tokens, ResourceType.Stone, allResources: false);

                case "age":
                    return ParseAge(tokens);

                case "spawn":
                    return ParseSpawn(tokens);

                default:
                    return CheatCommand.Fail($"Unknown command: {verb}");
            }
        }

        private static CheatCommand ParseAmount(string[] tokens, ResourceType resource, bool allResources)
        {
            float amount = DefaultResourceAmount;
            if (tokens.Length >= 2 && !float.TryParse(tokens[1], out amount))
            {
                return CheatCommand.Fail($"Expected a number, got '{tokens[1]}'.");
            }

            return CheatCommand.Ok(CheatCommandKind.Resources, resource, grantAllResources: allResources, amount: amount);
        }

        private static CheatCommand ParseAge(string[] tokens)
        {
            if (tokens.Length < 2)
            {
                return CheatCommand.Fail("Usage: age <ancient|classical|durg|imperial>");
            }

            string ageToken = tokens[1];
            foreach (AgeId candidate in new[] { AgeId.Ancient, AgeId.Classical, AgeId.Durg, AgeId.Imperial })
            {
                if (string.Equals(candidate.ToString(), ageToken, System.StringComparison.OrdinalIgnoreCase))
                {
                    return CheatCommand.Ok(CheatCommandKind.Age, age: candidate);
                }
            }

            return CheatCommand.Fail($"Unknown age: {ageToken}");
        }

        private static CheatCommand ParseSpawn(string[] tokens)
        {
            if (tokens.Length < 2)
            {
                return CheatCommand.Fail("Usage: spawn <unitType> [count]");
            }

            string unitType = null;
            foreach (string candidate in EntitySpawner.UnitTypes)
            {
                if (string.Equals(candidate, tokens[1], System.StringComparison.OrdinalIgnoreCase))
                {
                    unitType = candidate;
                    break;
                }
            }

            if (unitType == null)
            {
                return CheatCommand.Fail($"Unknown unit type: {tokens[1]} (try: {string.Join(", ", EntitySpawner.UnitTypes)})");
            }

            int count = DefaultSpawnCount;
            if (tokens.Length >= 3 && !int.TryParse(tokens[2], out count))
            {
                return CheatCommand.Fail($"Expected a whole number, got '{tokens[2]}'.");
            }

            count = UnityEngine.Mathf.Clamp(count, 1, 20);
            return CheatCommand.Ok(CheatCommandKind.Spawn, unitType: unitType, count: count);
        }
    }
}
