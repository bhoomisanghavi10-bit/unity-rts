using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Match
{
    public enum MatchOutcome
    {
        Ongoing,
        Victory,
        Defeat,
        // Item 2 (Victory Conditions): only reachable via the Time Limit
        // path (ResolveTimeLimitOutcome) - elimination and scripted
        // missions (ScenarioManager.EvaluateOutcome) never produce this.
        Draw,
    }

    // AoE-style conquest victory: a faction is eliminated once it has zero
    // units AND zero buildings left. Polls on a short interval (not every
    // frame - elimination is a rare, coarse-grained event) rather than
    // reacting to individual death events, since Unit/Building already
    // maintain live registries this can just recount cheaply. Freezes the
    // match (Time.timeScale = 0) the instant an outcome is reached; a
    // GameOverScreen (or anything else) just reads Outcome to react.
    public class MatchManager : MonoBehaviour
    {
        [SerializeField] private float checkInterval = 1f;
        [SerializeField] private float graceAfterMatchStart = 2f;

        public static MatchOutcome Outcome { get; private set; } = MatchOutcome.Ongoing;

        private float _timer;
        private float _matchStartedAt = -1f;

        private void OnEnable()
        {
            Outcome = MatchOutcome.Ongoing;
        }

        private void Update()
        {
            if (Outcome != MatchOutcome.Ongoing || !CivilizationSetup.HasMatchStarted)
            {
                return;
            }

            if (_matchStartedAt < 0f)
            {
                _matchStartedAt = Time.unscaledTime;
            }

            if (Time.unscaledTime - _matchStartedAt < graceAfterMatchStart)
            {
                return;
            }

            _timer += Time.unscaledDeltaTime;
            if (_timer < checkInterval)
            {
                return;
            }

            _timer = 0f;
            Evaluate();
        }

        // Item 48: Victory now requires every faction hostile to Player to
        // be eliminated, not just the single Enemy - an allied faction
        // surviving doesn't block Victory (you didn't need to beat your
        // own ally), and Enemy2 staying alive does (same as Enemy always
        // did). Defeat is unchanged - it was never about faction count.
        private static readonly FactionId[] AllFactions = { FactionId.Player, FactionId.Enemy, FactionId.Enemy2 };

        // Item 50: a scripted mission's win/loss condition takes over
        // entirely when one is active - a mission like "survive 3 minutes"
        // or "destroy the enemy Barracks" shouldn't also trigger Victory
        // just because the AI happened to lose every unit some other way.
        // A plain skirmish never sets ActiveScenario, so this branch never
        // fires and the elimination/time-limit logic below runs exactly as
        // before this item's Time Limit addition (a 0 GameSettings.
        // TimeLimitMinutes - the default - makes IsTimeLimitReached always
        // false, so EvaluateSkirmishOutcome behaves identically to the old
        // inline elimination-only logic for anyone who never touches the
        // new setting).
        private void Evaluate()
        {
            if (ScenarioManager.ActiveScenario != null)
            {
                MatchOutcome scenarioOutcome = ScenarioManager.EvaluateOutcome();
                if (scenarioOutcome != MatchOutcome.Ongoing)
                {
                    Declare(scenarioOutcome);
                }
                return;
            }

            MatchOutcome outcome = EvaluateSkirmishOutcome(IsTimeLimitReached());
            if (outcome != MatchOutcome.Ongoing)
            {
                Declare(outcome);
            }
        }

        private bool IsTimeLimitReached()
        {
            int limitMinutes = GameSettings.TimeLimitMinutes;
            return limitMinutes > 0 && (Time.unscaledTime - _matchStartedAt) >= limitMinutes * 60f;
        }

        // Item 2 (Victory Conditions): extracted from Evaluate() as the
        // testable seam (same convention as ConstructionSite.internal Tick/
        // CommandBus.internal EnqueueAt) - EditMode tests spawn real Unit/
        // Building/FactionMember objects and call this directly, no
        // Update()/timer/Time.unscaledTime mocking needed. Elimination
        // (Defeat/Victory) always takes priority over the time limit - a
        // decisive event beats an approximate timer even if both would fire
        // the same tick.
        internal static MatchOutcome EvaluateSkirmishOutcome(bool timeLimitReached)
        {
            // Wave 4 item 28: Regicide, opt-in via GameSettings.
            // RegicideEnabled (default false - this whole block is skipped
            // for any match that never touches the setting). Checked before
            // the elimination logic below since Regicide should end the
            // match on hero death even while the losing faction's army is
            // still standing - that's the entire point of the mode. A
            // faction that never trained a hero (HeroProgress.
            // HasTrainedHero false) can't spuriously trigger either branch.
            if (GameSettings.RegicideEnabled)
            {
                if (HeroProgress.HasTrainedHero(FactionId.Player) && !HeroProgress.IsAlive(FactionId.Player))
                {
                    return MatchOutcome.Defeat;
                }

                bool anyHostileHeroTrained = false;
                bool allHostileHeroesDead = true;
                foreach (FactionId hostileFaction in AllFactions)
                {
                    if (hostileFaction == FactionId.Player || DiplomacyRegistry.AreAllied(FactionId.Player, hostileFaction))
                    {
                        continue;
                    }

                    if (!HeroProgress.HasTrainedHero(hostileFaction))
                    {
                        continue;
                    }

                    anyHostileHeroTrained = true;
                    if (HeroProgress.IsAlive(hostileFaction))
                    {
                        allHostileHeroesDead = false;
                        break;
                    }
                }

                if (anyHostileHeroTrained && allHostileHeroesDead)
                {
                    return MatchOutcome.Victory;
                }
            }

            if (!FactionHasForces(FactionId.Player))
            {
                return MatchOutcome.Defeat;
            }

            bool allHostilesEliminated = true;
            foreach (FactionId faction in AllFactions)
            {
                if (faction == FactionId.Player || DiplomacyRegistry.AreAllied(FactionId.Player, faction))
                {
                    continue;
                }

                if (FactionHasForces(faction))
                {
                    allHostilesEliminated = false;
                    break;
                }
            }

            if (allHostilesEliminated)
            {
                return MatchOutcome.Victory;
            }

            return timeLimitReached ? ResolveTimeLimitOutcome() : MatchOutcome.Ongoing;
        }

        // "Declare the winner by total remaining population, or draw if
        // tied" per docs/PARTIAL_ELEMENTS_FIX_PLAN.md item 2 - deliberately
        // simple since no Score system exists yet (that's a separate,
        // deferred victory condition per the same plan doc). Ally-aware
        // grouping mirrors EvaluateSkirmishOutcome's own hostile-faction
        // loop above, so a Player ally's population counts toward the
        // Player's side rather than against it.
        internal static MatchOutcome ResolveTimeLimitOutcome()
        {
            int playerSide = 0;
            int hostileSide = 0;

            foreach (FactionId faction in AllFactions)
            {
                int population = Population.Current(faction);
                if (faction == FactionId.Player || DiplomacyRegistry.AreAllied(FactionId.Player, faction))
                {
                    playerSide += population;
                }
                else
                {
                    hostileSide += population;
                }
            }

            if (playerSide > hostileSide)
            {
                return MatchOutcome.Victory;
            }

            return playerSide < hostileSide ? MatchOutcome.Defeat : MatchOutcome.Draw;
        }

        // TargetDummy never counts here: it's tagged Enemy faction as a
        // standing combat test target (see TargetDummySpawner), but it's
        // neither a Unit nor a Building, so it never appears in either
        // registry below - its presence/absence can't affect whether the
        // Enemy reads as eliminated.
        private static bool FactionHasForces(FactionId faction)
        {
            foreach (Unit unit in Unit.All)
            {
                if (unit.TryGetComponent(out FactionMember member) && member.Faction == faction)
                {
                    return true;
                }
            }

            foreach (Building building in Building.All)
            {
                if (building.TryGetComponent(out FactionMember member) && member.Faction == faction)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Declare(MatchOutcome outcome)
        {
            Outcome = outcome;
            Time.timeScale = 0f;
        }

        // Wave 6 item 39 (Cheat codes): the one public entry point a cheat
        // console needs - everything else in this file only ever reaches
        // Declare() through Evaluate()'s own elimination/time-limit/
        // scenario logic. Reuses Declare() rather than duplicating its
        // Time.timeScale-freeze side effect.
        public static void ForceOutcome(MatchOutcome outcome)
        {
            Declare(outcome);
        }
    }
}
