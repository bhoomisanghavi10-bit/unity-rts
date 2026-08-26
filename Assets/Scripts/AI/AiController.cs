using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.AI
{
    // Single scripted, timer-driven AI opponent - not adaptive/learning, a
    // deliberate first-pass simplification. Economy/build decisions
    // (AssignIdleWorkers, TryBuildBarracks, ...) still act on full internal
    // knowledge of the map/player - that's normal for a scripted RTS AI and
    // not what "scouting" is about here. What's gated on scouting instead
    // is combat: the AI won't send its attack squad after the player until
    // one of its own units has actually gotten close enough to a Player
    // unit/building to "find" it (see CheckForScoutDiscovery/TryAttack),
    // giving the player a genuine early grace period instead of getting
    // attacked exactly at attackCheckInterval regardless of anything having
    // happened yet. This is intentionally separate from the player-facing
    // fog-of-war grid (FogOfWarManager), which this script never touches -
    // "discovered" here always means "known to the AI," never "revealed to
    // the player" (the reverse is already true: WorkerFactory deliberately
    // never gives Enemy units a VisionSource, so the AI's own base doesn't
    // leak into the player's fog).
    public class AiController : MonoBehaviour
    {
        // Item 48: which faction this instance plays as - a scene can now
        // hold two AiControllers (Enemy and Enemy2) instead of exactly
        // one. Every FactionId.Enemy self-reference in this file was
        // replaced with this field; every "the AI's target" reference
        // (FactionId.Player) was replaced with HostileTargetFaction()
        // below, which still resolves to Player by default (no
        // DiplomacyRegistry relations set = today's exact behavior).
        [SerializeField] private FactionId myFaction = FactionId.Enemy;
        [SerializeField] private Vector3 townCenterPosition = new Vector3(0f, 1f, -8f);
        [SerializeField] private int startingWorkerCount = 4;
        [SerializeField] private float workerSpacing = 2f;
        [SerializeField] private float decisionInterval = 2f;
        [SerializeField] private float attackCheckInterval = 45f;
        [SerializeField] private float diplomacyEvalInterval = 20f;
        [SerializeField] private int attackSquadSize = 3;
        [SerializeField] private float barracksWoodCost = 100f;
        [SerializeField] private float barracksStoneCost = 50f;
        [SerializeField] private float barracksBuildTime = 8f;
        [SerializeField] private float barracksClearance = 3f;
        [SerializeField] private Vector3 barracksOffset = new Vector3(6f, 0f, 0f);
        [SerializeField] private float farmWoodCost = 60f;
        [SerializeField] private float farmBuildTime = 5f;
        [SerializeField] private float farmClearance = 3f;
        [SerializeField] private Vector3 farmOffset = new Vector3(-6f, 0f, 0f);
        [SerializeField] private float houseWoodCost = 30f;
        [SerializeField] private float houseBuildTime = 4f;
        [SerializeField] private float houseClearance = 3f;
        [SerializeField] private Vector3 houseOffset = new Vector3(0f, 0f, -6f);
        [SerializeField] private int populationBuffer = 2;
        [SerializeField] private float ageUpResourceBuffer = 1.5f;
        [SerializeField] private float scoutInterval = 15f;
        [SerializeField] private float scoutDiscoveryRadius = 12f;
        [SerializeField] private float scoutTimeout = 20f;
        [SerializeField] private AiDifficulty difficulty = AiDifficulty.Normal;
        [SerializeField] private BuildOrderStyle buildOrder = BuildOrderStyle.Balanced;
        [SerializeField] private int economyFirstWorkerThreshold = 6;

        // Item 49 gap-closing: AI naval behavior. Dock siting has no fixed
        // offset like Barracks/Farm/House (water isn't always in the same
        // place relative to the town center) - see TryFindDockSpot.
        [SerializeField] private float dockWoodCost = 80f;
        [SerializeField] private float dockStoneCost = 20f;
        [SerializeField] private float dockBuildTime = 6f;
        [SerializeField] private float dockClearance = 3f;
        [SerializeField] private float dockMaxWaterDistance = 4f;
        [SerializeField] private int navalAttackSquadSize = 2;

        // Local to this controller, not a mutation on ResourceNode itself -
        // keeps the "who's gathering what" bookkeeping contained to one file.
        private readonly HashSet<ResourceNode> _claimedNodes = new HashSet<ResourceNode>();

        private float _decisionTimer;
        private float _attackTimer;
        private float _diplomacyTimer;
        private TownCenter _townCenter;
        private Barracks _barracks;
        private ConstructionSite _barracksSite;
        private bool _barracksBuilderAssigned;
        private Farm _farm;
        private ConstructionSite _farmSite;
        private bool _farmBuilderAssigned;
        private bool _farmWorkerAssigned;
        private House _house;
        private ConstructionSite _houseSite;
        private bool _houseBuilderAssigned;
        private int _houseCount;
        private Dock _dock;
        private ConstructionSite _dockSite;
        private bool _dockBuilderAssigned;
        private int _dockTrainRotation;
        private float _scoutTimer;
        private float _scoutElapsed;
        private bool _hasScoutedPlayer;
        private Unit _scoutUnit;

        // Guards against a leftover test/scaffolding AiController (found
        // active in Main.unity as "TestAi_MultiFront", claiming the same
        // myFaction/position as the real Enemy AiController) silently
        // running as a second, competing brain for the same faction -
        // spawning its own TownCenter+workers on top of the real one's.
        // Deliberately checked here in script rather than by removing the
        // object from the scene asset directly, so this holds regardless
        // of what test/debug objects end up saved into the scene.
        //
        // Done in Awake() rather than Start(), and keyed off the
        // GameObject's own name rather than "whoever claims the faction
        // first" - Unity guarantees every already-active object's Awake()
        // runs before any of their Start()s, but does NOT guarantee Awake
        // order between objects, so a first-claim-wins check in Start()
        // can (and did, confirmed live) disable the real AiController
        // instead of the test one if the test object simply happened to
        // sit earlier in the Hierarchy. Naming is the one signal that
        // reliably identifies which instance is the scaffolding rather
        // than depending on execution order.
        private void Awake()
        {
            if (!name.ToLowerInvariant().Contains("test"))
            {
                return;
            }

            // Include inactive: the real AiController for a faction is
            // gated (inactive until CivilizationSetup.BeginMatchCore
            // activates it), but a leftover test object like this one can
            // sit active in the scene from the start - excluding inactive
            // objects here meant this check ran before the real sibling
            // it needed to compare against even existed yet.
            foreach (AiController other in FindObjectsByType<AiController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other != this && other.myFaction == myFaction && !other.name.ToLowerInvariant().Contains("test"))
                {
                    Debug.LogWarning($"AiController on '{name}' disabled: faction {myFaction} is already " +
                        $"controlled by '{other.name}' (this object's name marks it as a test/scaffolding duplicate).");
                    enabled = false;
                    return;
                }
            }
        }

        private void Start()
        {
            ApplyDifficulty();

            // Item 44: the selected map picks the AI's starting position;
            // RiverValley's matches this field's own default exactly.
            // Item 48: Enemy2 reads the map's 3rd spawn slot instead.
            townCenterPosition = myFaction == FactionId.Enemy2
                ? MapRegistry.Current.Enemy2TownCenter
                : MapRegistry.Current.EnemyTownCenter;

            GameObject townCenterGo = TownCenterFactory.Place(townCenterPosition, myFaction);
            townCenterGo.TryGetComponent(out _townCenter);

            for (int i = 0; i < startingWorkerCount; i++)
            {
                float x = i * workerSpacing - (startingWorkerCount - 1) * workerSpacing * 0.5f;
                Vector3 spawnPos = townCenterPosition + new Vector3(x, 0f, -3f);
                WorkerFactory.Spawn(spawnPos, myFaction);
            }
        }

        // Item 48/49-gap-closing: which faction this AI currently treats as
        // its combat focus - resolved fresh (not cached) so a mid-match
        // alliance change, or a closer threat showing up, picks a new
        // target instead of continuing to march toward a stale one.
        // Originally a fixed Player/Enemy/Enemy2 priority order; now
        // genuinely reactive - whichever hostile faction has the nearest
        // unit/building to the AI's own base wins, so a 3-faction match
        // has real multi-front behavior (an AI under attack from Enemy2
        // will defend against Enemy2 instead of blindly continuing to
        // march on a distant Player). Falls back to Player when nothing
        // hostile has any presence at all (e.g. a 2-faction match before
        // Enemy2 exists, or every other faction allied) - matches this
        // AI's original pre-diplomacy behavior exactly in the common case.
        private static readonly FactionId[] CandidateFactions = { FactionId.Player, FactionId.Enemy, FactionId.Enemy2 };

        private FactionId HostileTargetFaction()
        {
            FactionId? nearest = null;
            float bestDistance = float.MaxValue;

            foreach (FactionId candidate in CandidateFactions)
            {
                if (candidate == myFaction || !DiplomacyRegistry.IsHostile(myFaction, candidate))
                {
                    continue;
                }

                float distance = NearestPresenceDistance(candidate, townCenterPosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = candidate;
                }
            }

            return nearest ?? FactionId.Player;
        }

        // float.MaxValue means "this faction has no units/buildings at
        // all right now" - lets HostileTargetFaction() naturally skip a
        // faction with no actual presence (e.g. Enemy2 in a 2-faction
        // match) without a separate existence check.
        private static float NearestPresenceDistance(FactionId faction, Vector3 fromPosition)
        {
            float best = float.MaxValue;

            foreach (Unit unit in Unit.All)
            {
                if (unit.TryGetComponent(out FactionMember factionMember) && factionMember.Faction == faction)
                {
                    best = Mathf.Min(best, Vector3.Distance(fromPosition, unit.transform.position));
                }
            }

            foreach (Building building in Building.All)
            {
                if (building.TryGetComponent(out FactionMember factionMember) && factionMember.Faction == faction)
                {
                    best = Mathf.Min(best, Vector3.Distance(fromPosition, building.transform.position));
                }
            }

            return best;
        }

        // Item 49-gap-closing: "the enemy of my enemy is my friend" - a
        // simple, well-established AI diplomacy heuristic rather than a
        // full negotiation system. Checked on a slow timer (not every
        // decision tick) to avoid flip-flopping, and an alliance once
        // formed is never broken by this AI on its own (no "backstab"
        // behavior) - sticky alliances are far easier to reason about for
        // a player than an ally that might turn at any moment. Naturally
        // a no-op in a 2-faction match: with only one other faction to
        // possibly ally with, there's no third faction left to need
        // rescuing from, so biggestThreat stays null and nothing happens -
        // this AI's default behavior is completely unchanged unless a 3rd
        // faction is actually in play.
        private void TryEvaluateDiplomacy()
        {
            foreach (FactionId other in CandidateFactions)
            {
                if (other == myFaction || DiplomacyRegistry.AreAllied(myFaction, other) || !FactionHasPresence(other))
                {
                    continue;
                }

                FactionId? biggestThreat = null;
                float biggestThreatStrength = 0f;

                foreach (FactionId threat in CandidateFactions)
                {
                    if (threat == myFaction || threat == other || !DiplomacyRegistry.IsHostile(myFaction, threat))
                    {
                        continue;
                    }

                    float strength = FactionStrength(threat);
                    if (strength > biggestThreatStrength)
                    {
                        biggestThreatStrength = strength;
                        biggestThreat = threat;
                    }
                }

                // Only seek help when losing badly (below half the
                // threat's strength) - a fair fight is still this AI's
                // own fight to handle.
                if (biggestThreat != null && FactionStrength(myFaction) < biggestThreatStrength * 0.5f)
                {
                    DiplomacyRegistry.SetAllied(myFaction, other, true);
                    return;
                }
            }
        }

        private static bool FactionHasPresence(FactionId faction)
        {
            return FactionStrength(faction) > 0f;
        }

        // Simple unit+building count - deliberately not damage/tier-
        // weighted, matching this AI's other "coarse, not omniscient-
        // precise" heuristics (see TryAttack's squad-size threshold).
        private static float FactionStrength(FactionId faction)
        {
            float score = 0f;

            foreach (Unit unit in Unit.All)
            {
                if (unit.TryGetComponent(out FactionMember factionMember) && factionMember.Faction == faction)
                {
                    score += 1f;
                }
            }

            foreach (Building building in Building.All)
            {
                if (building.TryGetComponent(out FactionMember factionMember) && factionMember.Faction == faction)
                {
                    score += 1f;
                }
            }

            return score;
        }

        // Scales existing timer/threshold fields rather than changing any
        // decision logic - Hard reacts faster and commits to fights with
        // smaller squads and less margin; Easy is the reverse. Called once
        // before anything else in Start() so every field it touches is
        // already at its scaled value by the time the rest of Start() (and
        // every subsequent Update tick) reads it.
        private void ApplyDifficulty()
        {
            // Item 46: once the player has actually opened Settings and
            // chosen a difficulty, that choice wins over this Inspector
            // default - HasDifficultyOverride distinguishes "never touched
            // Settings" from "explicitly chose Normal", so an untouched
            // Settings menu never silently overwrites a designer's choice.
            if (GameSettings.HasDifficultyOverride)
            {
                difficulty = GameSettings.Difficulty;
            }

            switch (difficulty)
            {
                case AiDifficulty.Easy:
                    decisionInterval *= 1.5f;
                    attackCheckInterval *= 1.5f;
                    attackSquadSize += 2;
                    ageUpResourceBuffer += 0.5f;
                    populationBuffer -= 1;
                    break;
                case AiDifficulty.Hard:
                    decisionInterval *= 0.75f;
                    attackCheckInterval *= 0.7f;
                    attackSquadSize = Mathf.Max(1, attackSquadSize - 1);
                    ageUpResourceBuffer -= 0.3f;
                    populationBuffer += 1;
                    break;
            }
        }

        private void Update()
        {
            _decisionTimer += Time.deltaTime;
            if (_decisionTimer >= decisionInterval)
            {
                _decisionTimer = 0f;
                AssignIdleWorkers();
                TryAgeUp();
                TryResearchEconomyTechs();
                TryBuildBarracks();
                AssignBuilderIfNeeded();
                TryTrainSoldiers();
                TryBuildFarm();
                AssignFarmBuilderIfNeeded();
                AssignFarmWorkerIfNeeded();
                TryBuildHouse();
                AssignHouseBuilderIfNeeded();
                TryTrainWorkers();
                TryResearchUpgrades();
                UpdateScouting();
                TryBuildDock();
                AssignDockBuilderIfNeeded();
                TryTrainNavalUnits();
                AssignIdleFishingBoats();
            }

            _attackTimer += Time.deltaTime;
            if (_attackTimer >= attackCheckInterval)
            {
                _attackTimer = 0f;
                TryAttack();
                TryNavalAttack();
            }

            _diplomacyTimer += Time.deltaTime;
            if (_diplomacyTimer >= diplomacyEvalInterval)
            {
                _diplomacyTimer = 0f;
                TryEvaluateDiplomacy();
            }
        }

        // Discovery is checked every decision tick regardless of dispatch
        // state (any unit wandering close enough counts, not just a
        // dedicated scout), but a new scout is only sent out - and only one
        // at a time - once every scoutInterval while still undiscovered.
        private void UpdateScouting()
        {
            if (_hasScoutedPlayer)
            {
                return;
            }

            CheckForScoutDiscovery();
            if (_hasScoutedPlayer)
            {
                _scoutUnit = null;
                return;
            }

            if (_scoutUnit != null)
            {
                _scoutElapsed += decisionInterval;
                if (_scoutElapsed >= scoutTimeout)
                {
                    // Gave up without finding anything - releases the unit
                    // back to AssignIdleWorkers next tick; scoutTimer below
                    // will send another scout out once scoutInterval passes.
                    _scoutUnit = null;
                }
                return;
            }

            _scoutTimer += decisionInterval;
            if (_scoutTimer < scoutInterval)
            {
                return;
            }
            _scoutTimer = 0f;

            DispatchScout();
        }

        // A unit close enough to any Player unit/building counts as having
        // found it - not just a unit explicitly sent out to look, since a
        // worker gathering near the map's edge could plausibly stumble onto
        // the player's base too.
        private void CheckForScoutDiscovery()
        {
            foreach (Unit unit in Unit.All)
            {
                if (IsMine(unit) && IsNearAnyPlayerTarget(unit.transform.position))
                {
                    _hasScoutedPlayer = true;
                    return;
                }
            }
        }

        // Item 48: triggers scout-discovery on any hostile faction found
        // nearby, not just Player specifically - a 3-faction match should
        // still gate combat on genuine contact, regardless of which
        // faction that contact is with.
        private bool IsNearAnyPlayerTarget(Vector3 fromPosition)
        {
            foreach (Unit unit in Unit.All)
            {
                if (unit.TryGetComponent(out FactionMember factionMember)
                    && DiplomacyRegistry.IsHostile(myFaction, factionMember.Faction)
                    && Vector3.Distance(fromPosition, unit.transform.position) <= scoutDiscoveryRadius)
                {
                    return true;
                }
            }

            foreach (Building building in Building.All)
            {
                if (building.TryGetComponent(out FactionMember factionMember)
                    && DiplomacyRegistry.IsHostile(myFaction, factionMember.Faction)
                    && Vector3.Distance(fromPosition, building.transform.position) <= scoutDiscoveryRadius)
                {
                    return true;
                }
            }

            return false;
        }

        // Pulls one worker off gathering (skipping anything mid-build, same
        // courtesy AssignBuilderIfNeeded's siblings extend to an
        // in-progress foundation) and sends it toward ScoutTarget.
        // AssignIdleWorkers explicitly skips _scoutUnit so it doesn't get
        // immediately reassigned back to a resource node next tick.
        private void DispatchScout()
        {
            foreach (Unit unit in Unit.All)
            {
                if (!IsMine(unit) || !unit.TryGetComponent(out Gatherer gatherer) || !unit.TryGetComponent(out UnitMover mover))
                {
                    continue;
                }

                if (unit.TryGetComponent(out Builder builder) && builder.IsBuilding)
                {
                    continue;
                }

                gatherer.CancelGather();
                mover.MoveTo(ScoutTarget());
                _scoutUnit = unit;
                _scoutElapsed = 0f;
                return;
            }
        }

        // A guess, not omniscience: the map's starting positions are
        // roughly mirrored across the center, so this is where a rival
        // civilization would plausibly have settled. The scout still has
        // to physically travel there and get within scoutDiscoveryRadius
        // before TryAttack can act on anything - this only decides where
        // to send it looking, not what it finds.
        private Vector3 ScoutTarget()
        {
            return new Vector3(-townCenterPosition.x, townCenterPosition.y, -townCenterPosition.z);
        }

        private void AssignIdleWorkers()
        {
            List<Unit> idleWorkers = new List<Unit>();
            foreach (Unit unit in Unit.All)
            {
                if (unit == _scoutUnit)
                {
                    continue;
                }

                if (IsMine(unit) && unit.TryGetComponent(out Gatherer gatherer) && !gatherer.IsWorking)
                {
                    idleWorkers.Add(unit);
                }
            }

            if (idleWorkers.Count == 0)
            {
                return;
            }

            ResourceNode[] allNodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);

            foreach (Unit unit in idleWorkers)
            {
                ResourceNode nearest = FindUnclaimedNode(unit.transform.position, allNodes);
                if (nearest == null)
                {
                    continue;
                }

                _claimedNodes.Add(nearest);
                unit.TryGetComponent(out Gatherer gatherer);
                gatherer.GatherFrom(nearest);
            }
        }

        // Item 49 gap-closing: excludes Fish nodes (see AssignIdleFishingBoats
        // for their own claim scan) - without this check, a land Worker
        // could be sent to "gather" a Fish node sitting in the water, walk
        // to the shore, and get stuck there forever (UnitMover has no path
        // across water - see WaterMover's own doc comment on why boats
        // need a separate mover in the first place).
        private ResourceNode FindUnclaimedNode(Vector3 fromPosition, ResourceNode[] allNodes)
        {
            ResourceNode nearest = null;
            float bestDistance = float.MaxValue;

            foreach (ResourceNode node in allNodes)
            {
                if (_claimedNodes.Contains(node) || WaterProximity.IsInsideWater(node.transform.position))
                {
                    continue;
                }

                float distance = Vector3.Distance(fromPosition, node.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = node;
                }
            }

            return nearest;
        }

        // Item 49 gap-closing: the naval equivalent of AssignIdleWorkers -
        // same shape, checks BoatGatherer instead of Gatherer.
        private void AssignIdleFishingBoats()
        {
            List<Unit> idleBoats = new List<Unit>();
            foreach (Unit unit in Unit.All)
            {
                if (IsMine(unit) && unit.TryGetComponent(out BoatGatherer boatGatherer) && !boatGatherer.IsWorking)
                {
                    idleBoats.Add(unit);
                }
            }

            if (idleBoats.Count == 0)
            {
                return;
            }

            ResourceNode[] allNodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);

            foreach (Unit unit in idleBoats)
            {
                ResourceNode nearest = FindUnclaimedFishNode(unit.transform.position, allNodes);
                if (nearest == null)
                {
                    continue;
                }

                _claimedNodes.Add(nearest);
                unit.TryGetComponent(out BoatGatherer boatGatherer);
                boatGatherer.GatherFrom(nearest);
            }
        }

        // Mirrors FindUnclaimedNode, restricted to nodes actually inside
        // the water rectangle - the only ones a boat can reach.
        private ResourceNode FindUnclaimedFishNode(Vector3 fromPosition, ResourceNode[] allNodes)
        {
            ResourceNode nearest = null;
            float bestDistance = float.MaxValue;

            foreach (ResourceNode node in allNodes)
            {
                if (_claimedNodes.Contains(node) || !WaterProximity.IsInsideWater(node.transform.position))
                {
                    continue;
                }

                float distance = Vector3.Distance(fromPosition, node.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = node;
                }
            }

            return nearest;
        }

        // Ages up once it's holding a comfortable buffer past the next
        // Age's cost - mirrors the "builds Houses proactively before
        // hitting the cap" convention already used elsewhere in this
        // controller: act early on a margin rather than reactively at the
        // exact threshold. RequestAgeUp() itself is the source of truth
        // for cost/eligibility/in-progress checks; this is just the
        // decision of *when* to call it.
        private void TryAgeUp()
        {
            if (_townCenter == null || _townCenter.IsAgingUp || !AgeProgress.HasNextAge(myFaction))
            {
                return;
            }

            AgeProfile nextProfile = AgeProfile.For(AgeProgress.NextAge(myFaction));
            ResourceStockpile stockpile = ResourceStockpile.For(myFaction);
            if (stockpile.GetTotal(ResourceType.Wood) < nextProfile.WoodCost * ageUpResourceBuffer
                || stockpile.GetTotal(ResourceType.Stone) < nextProfile.StoneCost * ageUpResourceBuffer)
            {
                return;
            }

            _townCenter.RequestAgeUp();
        }

        // Phase 6 gap-close (deeper tech tree): fixed priority order
        // (ImprovedTools -> PackMules -> TradeDiscounts) rather than
        // cycled/random, same reasoning as the Cavalry/Archer fixed
        // pairing in TryResearchUpgrades - keeps the AI's choice legible.
        // Improved gathering compounds the earliest (every worker benefits
        // for the rest of the match), so it goes first; TradeDiscounts
        // only pays off once the AI is actually spending on buildings
        // regularly, so it's last. Shares TownCenter's one economy-tech
        // slot with itself here (RequestResearchEconomyTech no-ops if
        // already researching), same "one at a time, real prioritization"
        // design the slot itself was built around.
        private void TryResearchEconomyTechs()
        {
            if (_townCenter == null || _townCenter.IsResearchingEconomyTech)
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(myFaction);

            if (!_townCenter.HasResearchedEconomyTech(EconomyTech.ImprovedTools))
            {
                TryResearchEconomyTech(EconomyTech.ImprovedTools, stockpile);
                return;
            }

            if (!_townCenter.HasResearchedEconomyTech(EconomyTech.PackMules))
            {
                TryResearchEconomyTech(EconomyTech.PackMules, stockpile);
                return;
            }

            if (!_townCenter.HasResearchedEconomyTech(EconomyTech.TradeDiscounts))
            {
                TryResearchEconomyTech(EconomyTech.TradeDiscounts, stockpile);
            }
        }

        private void TryResearchEconomyTech(EconomyTech tech, ResourceStockpile stockpile)
        {
            EconomyTechDefinition definition = EconomyTechDefinition.For(tech);
            if (stockpile.GetTotal(ResourceType.Wood) < definition.WoodCost
                || stockpile.GetTotal(ResourceType.Gold) < definition.GoldCost)
            {
                return;
            }

            _townCenter.RequestResearchEconomyTech(tech);
        }

        private void TryBuildBarracks()
        {
            // Symmetric with the Player's own gate in BuildingPlacer -
            // the AI can't build a Barracks before Classical Age either.
            if (_barracks != null || AgeProgress.CurrentAge(myFaction) == AgeId.Ancient)
            {
                return;
            }

            // EconomyFirst's actual teeth: every other style builds the
            // instant resources allow (below), this one waits for a real
            // worker base first - the whole point of choosing it over
            // Balanced/RushMilitary.
            if (buildOrder == BuildOrderStyle.EconomyFirst && CountMyWorkers() < economyFirstWorkerThreshold)
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(myFaction);
            float multiplier = CivilizationProfile.For(CivilizationRegistry.For(myFaction)).BuildCostMultiplier;
            if (stockpile.GetTotal(ResourceType.Wood) < barracksWoodCost * multiplier
                || stockpile.GetTotal(ResourceType.Stone) < barracksStoneCost * multiplier)
            {
                return;
            }

            Vector3 candidateXz = townCenterPosition + barracksOffset;
            if (!TryResolveGroundHeight(candidateXz, out Vector3 point))
            {
                return;
            }

            if (!BarracksFactory.IsClear(point, barracksClearance))
            {
                return;
            }

            stockpile.Add(ResourceType.Wood, -barracksWoodCost * multiplier);
            stockpile.Add(ResourceType.Stone, -barracksStoneCost * multiplier);

            GameObject go = BarracksFactory.Place(point, myFaction, barracksBuildTime);
            go.TryGetComponent(out _barracks);
            go.TryGetComponent(out _barracksSite);
        }

        // The AI must actively send a worker to build, same as the Player -
        // a placed foundation doesn't build itself (milestone 5's rule).
        // Pulls a worker off gathering duty if nothing's idle, rather than
        // waiting for one to naturally become free (which may never happen,
        // since AssignIdleWorkers immediately re-assigns anything idle).
        private void AssignBuilderIfNeeded()
        {
            if (_barracksSite == null || _barracksBuilderAssigned || _barracksSite.IsComplete)
            {
                return;
            }

            foreach (Unit unit in Unit.All)
            {
                if (!IsMine(unit) || !unit.TryGetComponent(out Builder builder))
                {
                    continue;
                }

                if (unit.TryGetComponent(out Gatherer gatherer))
                {
                    gatherer.CancelGather();
                }

                builder.BuildAt(_barracksSite);
                _barracksBuilderAssigned = true;
                return;
            }
        }

        private int _trainRotation;

        // AoE-style mixed composition rather than an all-melee army: two
        // Soldiers, an Archer, a Cavalry, a Spearman, a Siege, then the
        // AI's own civ unique unit, repeating - so the enemy's attack
        // squads benefit from CombatBonus's full counter web
        // (Infantry > Archer > Cavalry > Infantry, plus Spearman's 2x
        // hard counter vs Cavalry) instead of fielding just one class.
        // Spearman took over what used to be a third plain-Soldier slot
        // (case 4) - added here because without it, the AI never trained
        // one at all despite it being a real, live-verified unit (see
        // playtest_log.csv's Spearman-vs-Cavalry entry) - the AI opponent
        // structurally couldn't field the one unit that hard-counters its
        // own Cavalry slot. (Human players now train it via BuildMenu's
        // Spearman button; before that button existed, this rotation was
        // the AI's only access to it too.) Siege is deliberately
        // rare (1 in 7) - its 3x Building bonus is wasted outside a siege
        // against Walls/Towers/other buildings, and it's slow and
        // unremarkable against units in the meantime. The unique unit slot
        // (Phase 6) is equally rare and last in the cycle - it's the AI's
        // civ-identity piece, worth fielding regularly but not so often it
        // crowds out the counter web the other 6 slots are built around.
        private void TryTrainSoldiers()
        {
            if (_barracks == null || !_barracks.IsComplete || _barracks.IsTraining)
            {
                return;
            }

            switch (_trainRotation)
            {
                case 2:
                    _barracks.RequestTrainArcher();
                    break;
                case 3:
                    _barracks.RequestTrainCavalry();
                    break;
                case 4:
                    _barracks.RequestTrainSpearman();
                    break;
                case 5:
                    _barracks.RequestTrainSiege();
                    break;
                case 6:
                    _barracks.RequestTrainUniqueUnit();
                    break;
                default:
                    _barracks.RequestTrain();
                    break;
            }

            _trainRotation = (_trainRotation + 1) % 7;
        }

        // Same early-margin-not-exact-threshold shape as TryAgeUp: research
        // once holding a comfortable buffer above the tier's Gold cost,
        // alternating tracks so both keep advancing over a long game.
        private void TryResearchUpgrades()
        {
            if (_barracks == null || !_barracks.IsComplete)
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(myFaction);

            if (!_barracks.IsResearchingAttack
                && UpgradeProgress.HasNextAttackTier(myFaction)
                && stockpile.GetTotal(ResourceType.Gold) > 150f)
            {
                _barracks.RequestResearchAttack();
                return;
            }

            if (!_barracks.IsResearchingArmor
                && UpgradeProgress.HasNextArmorTier(myFaction)
                && stockpile.GetTotal(ResourceType.Gold) > 150f)
            {
                _barracks.RequestResearchArmor();
                return;
            }

            // Item 40's per-class layer, on top of the flat tracks above -
            // Cavalry gets the attack line (it's the aggressive strike
            // unit, most rewarded by raw damage), Archer gets the armor
            // line (the roster's most fragile unit, most rewarded by
            // extra survivability). A fixed pairing, not cycled - keeps
            // the AI's choice legible/predictable rather than spreading
            // Gold thin across all four classes.
            if (!_barracks.IsResearchingClassAttack
                && UpgradeProgress.HasNextClassAttackTier(myFaction, UnitClass.Cavalry)
                && stockpile.GetTotal(ResourceType.Gold) > 150f)
            {
                _barracks.RequestResearchClassAttack(UnitClass.Cavalry);
                return;
            }

            if (!_barracks.IsResearchingClassArmor
                && UpgradeProgress.HasNextClassArmorTier(myFaction, UnitClass.Archer)
                && stockpile.GetTotal(ResourceType.Gold) > 150f)
            {
                _barracks.RequestResearchClassArmor(UnitClass.Archer);
                return;
            }

            // Phase 6: the AI's own civ unique tech - last in the priority
            // order (after the flat/per-class tracks above, which help
            // every fight immediately) since a unique tech is a one-time,
            // civ-specific payoff rather than a repeatable stat track: an
            // AI that never gets around to it just plays without its own
            // civ's extra identity, not meaningfully weaker overall.
            if (!_barracks.IsResearchingUniqueTech
                && !_barracks.HasResearchedUniqueTech
                && stockpile.GetTotal(ResourceType.Gold) > 150f)
            {
                _barracks.RequestResearchUniqueTech();
            }
        }

        private void TryBuildFarm()
        {
            if (_farm != null)
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(myFaction);
            float multiplier = CivilizationProfile.For(CivilizationRegistry.For(myFaction)).BuildCostMultiplier;
            if (stockpile.GetTotal(ResourceType.Wood) < farmWoodCost * multiplier)
            {
                return;
            }

            Vector3 candidateXz = townCenterPosition + farmOffset;
            if (!TryResolveGroundHeight(candidateXz, out Vector3 point))
            {
                return;
            }

            if (!BarracksFactory.IsClear(point, farmClearance))
            {
                return;
            }

            stockpile.Add(ResourceType.Wood, -farmWoodCost * multiplier);

            GameObject go = FarmFactory.Place(point, myFaction, farmBuildTime);
            go.TryGetComponent(out _farm);
            go.TryGetComponent(out _farmSite);
        }

        // Same shape as AssignBuilderIfNeeded, for the Farm instead of the
        // Barracks. A worker pulled here for one job can't simultaneously
        // be pulled for the other in the same tick since both explicitly
        // cancel Gatherer first - but nothing here stops both this and
        // AssignBuilderIfNeeded from picking the *same* worker on the same
        // tick if both a Barracks and a Farm need building at once; the
        // second call just re-targets that worker, and the loser retries
        // next tick. A known, accepted inefficiency at this scale.
        private void AssignFarmBuilderIfNeeded()
        {
            if (_farmSite == null || _farmBuilderAssigned || _farmSite.IsComplete)
            {
                return;
            }

            foreach (Unit unit in Unit.All)
            {
                if (!IsMine(unit) || !unit.TryGetComponent(out Builder builder))
                {
                    continue;
                }

                if (unit.TryGetComponent(out Gatherer gatherer))
                {
                    gatherer.CancelGather();
                }

                builder.BuildAt(_farmSite);
                _farmBuilderAssigned = true;
                return;
            }
        }

        private void AssignFarmWorkerIfNeeded()
        {
            if (_farm == null || !_farm.IsComplete || _farmWorkerAssigned)
            {
                return;
            }

            foreach (Unit unit in Unit.All)
            {
                if (!IsMine(unit) || !unit.TryGetComponent(out FarmWorker farmWorker))
                {
                    continue;
                }

                if (unit.TryGetComponent(out Gatherer gatherer))
                {
                    gatherer.CancelGather();
                }

                farmWorker.StaffAt(_farm);
                _farmWorkerAssigned = true;
                return;
            }
        }

        // Item 49 gap-closing: builds one Dock, same single-instance shape
        // TryBuildBarracks/TryBuildFarm/TryBuildHouse already use, gated
        // on the map actually having water (WaterProximity.HasWater) - a
        // no-op on RiverValley/Highlands.
        private void TryBuildDock()
        {
            if (_dock != null || !WaterProximity.HasWater)
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(myFaction);
            float multiplier = CivilizationProfile.For(CivilizationRegistry.For(myFaction)).BuildCostMultiplier;
            if (stockpile.GetTotal(ResourceType.Wood) < dockWoodCost * multiplier
                || stockpile.GetTotal(ResourceType.Stone) < dockStoneCost * multiplier)
            {
                return;
            }

            if (!TryFindDockSpot(out Vector3 point))
            {
                return;
            }

            stockpile.Add(ResourceType.Wood, -dockWoodCost * multiplier);
            stockpile.Add(ResourceType.Stone, -dockStoneCost * multiplier);

            GameObject go = DockFactory.Place(point, myFaction, dockBuildTime);
            go.TryGetComponent(out _dock);
            go.TryGetComponent(out _dockSite);
        }

        // Unlike Barracks/Farm/House (a fixed offset from townCenterPosition -
        // water isn't always in the same place relative to the town center),
        // this walks from the nearest point on the water rectangle's edge
        // back toward the town center in small steps, looking for the
        // first spot that's clear, on dry ground, and within
        // dockMaxWaterDistance - the same constraint BuildingPlacer.
        // IsClearForKind enforces for the Player's own Dock placement.
        private bool TryFindDockSpot(out Vector3 point)
        {
            point = Vector3.zero;
            if (!WaterProximity.HasWater)
            {
                return false;
            }

            MapDefinitionData map = MapRegistry.Current;
            float clampedX = Mathf.Clamp(townCenterPosition.x, map.WaterCenter.x - map.WaterHalfExtents.x, map.WaterCenter.x + map.WaterHalfExtents.x);
            float clampedZ = Mathf.Clamp(townCenterPosition.z, map.WaterCenter.z - map.WaterHalfExtents.z, map.WaterCenter.z + map.WaterHalfExtents.z);
            Vector3 nearestOnWater = new Vector3(clampedX, 0f, clampedZ);

            Vector3 towardLand = townCenterPosition - nearestOnWater;
            towardLand.y = 0f;
            if (towardLand.sqrMagnitude < 0.01f)
            {
                towardLand = Vector3.back;
            }
            towardLand = towardLand.normalized;

            for (float distance = 1f; distance <= dockMaxWaterDistance; distance += 1f)
            {
                Vector3 candidateXz = nearestOnWater + towardLand * distance;
                if (!TryResolveGroundHeight(candidateXz, out Vector3 candidate))
                {
                    continue;
                }

                if (DockFactory.IsClear(candidate, dockClearance) && !WaterProximity.IsInsideWater(candidate))
                {
                    point = candidate;
                    return true;
                }
            }

            return false;
        }

        private void AssignDockBuilderIfNeeded()
        {
            if (_dockSite == null || _dockBuilderAssigned || _dockSite.IsComplete)
            {
                return;
            }

            foreach (Unit unit in Unit.All)
            {
                if (!IsMine(unit) || !unit.TryGetComponent(out Builder builder))
                {
                    continue;
                }

                if (unit.TryGetComponent(out Gatherer gatherer))
                {
                    gatherer.CancelGather();
                }

                builder.BuildAt(_dockSite);
                _dockBuilderAssigned = true;
                return;
            }
        }

        // Mostly Fishing Boats (economy), one War Galley every 4th train -
        // same "mostly economy, some military" ratio TryTrainSoldiers
        // already uses for its own rotation.
        private void TryTrainNavalUnits()
        {
            if (_dock == null || !_dock.IsComplete || _dock.IsTraining)
            {
                return;
            }

            if (_dockTrainRotation == 3)
            {
                _dock.RequestTrainWarGalley();
            }
            else
            {
                _dock.RequestTrainFishingBoat();
            }

            _dockTrainRotation = (_dockTrainRotation + 1) % 4;
        }

        // Population.Cap/Current recompute fresh from Building.All/Unit.All
        // each call (see Population), so no persistent house-count state
        // is needed for the cap math itself - only for tracking the
        // currently-in-progress house until it's complete, at which point
        // tracking resets so a later call (population grown again) can
        // start another one. AoE-style: builds proactively once room gets
        // low, not only once actually full.
        private void TryBuildHouse()
        {
            if (_houseSite != null && _houseSite.IsComplete)
            {
                _house = null;
                _houseSite = null;
                _houseBuilderAssigned = false;
            }

            if (_house != null)
            {
                return;
            }

            if (Population.Cap(myFaction) - Population.Current(myFaction) > populationBuffer)
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(myFaction);
            float multiplier = CivilizationProfile.For(CivilizationRegistry.For(myFaction)).BuildCostMultiplier;
            if (stockpile.GetTotal(ResourceType.Wood) < houseWoodCost * multiplier)
            {
                return;
            }

            Vector3 candidateXz = townCenterPosition + houseOffset + new Vector3(_houseCount * 2.5f, 0f, 0f);
            if (!TryResolveGroundHeight(candidateXz, out Vector3 point))
            {
                return;
            }

            if (!BarracksFactory.IsClear(point, houseClearance))
            {
                return;
            }

            stockpile.Add(ResourceType.Wood, -houseWoodCost * multiplier);

            GameObject go = HouseFactory.Place(point, myFaction, houseBuildTime);
            go.TryGetComponent(out _house);
            go.TryGetComponent(out _houseSite);
            _houseCount++;
        }

        // Same shape as AssignBuilderIfNeeded/AssignFarmBuilderIfNeeded.
        private void AssignHouseBuilderIfNeeded()
        {
            if (_houseSite == null || _houseBuilderAssigned || _houseSite.IsComplete)
            {
                return;
            }

            foreach (Unit unit in Unit.All)
            {
                if (!IsMine(unit) || !unit.TryGetComponent(out Builder builder))
                {
                    continue;
                }

                if (unit.TryGetComponent(out Gatherer gatherer))
                {
                    gatherer.CancelGather();
                }

                builder.BuildAt(_houseSite);
                _houseBuilderAssigned = true;
                return;
            }
        }

        private void TryTrainWorkers()
        {
            if (_townCenter == null)
            {
                return;
            }

            _townCenter.RequestTrain();
        }

        private void TryAttack()
        {
            if (!_hasScoutedPlayer)
            {
                return;
            }

            // Workers now carry a weak MeleeAttacker too (can fight back/
            // hunt boars), so "has MeleeAttacker" alone no longer means
            // "is a Soldier" - excluding anything with a Gatherer keeps
            // the AI's attack squad built from actual Soldiers only,
            // instead of pulling its whole economy into an attack-move.
            List<Unit> soldiers = new List<Unit>();
            foreach (Unit unit in Unit.All)
            {
                if (IsMine(unit) && unit.TryGetComponent(out MeleeAttacker _) && !unit.TryGetComponent(out Gatherer _))
                {
                    soldiers.Add(unit);
                }
            }

            if (soldiers.Count < attackSquadSize)
            {
                return;
            }

            Attackable target = FindNearestPlayerTarget(townCenterPosition);
            if (target == null)
            {
                return;
            }

            foreach (Unit soldier in soldiers)
            {
                soldier.TryGetComponent(out MeleeAttacker attacker);
                attacker.AttackMove(target);
            }
        }

        // Item 49 gap-closing: the naval equivalent of TryAttack, kept
        // fully separate rather than merged into it - a naval squad has
        // its own (smaller) size threshold, and WaterMover has no
        // pathfinding across land, so it needs its own target search
        // restricted to things actually reachable from water (see
        // FindNearestNavalTarget) rather than reusing FindNearestPlayerTarget,
        // which would happily send a War Galley sailing at a landlocked
        // Town Center it can never reach.
        private void TryNavalAttack()
        {
            if (!_hasScoutedPlayer)
            {
                return;
            }

            List<Unit> galleys = new List<Unit>();
            foreach (Unit unit in Unit.All)
            {
                if (IsMine(unit) && unit.TryGetComponent(out BoatAttacker _))
                {
                    galleys.Add(unit);
                }
            }

            if (galleys.Count < navalAttackSquadSize)
            {
                return;
            }

            Attackable target = FindNearestNavalTarget();
            if (target == null)
            {
                return;
            }

            foreach (Unit galley in galleys)
            {
                galley.TryGetComponent(out BoatAttacker attacker);
                attacker.AttackMove(target);
            }
        }

        // Restricted to other naval units (BoatAttacker/BoatGatherer) and
        // Dock buildings specifically - the only things actually within a
        // ship's reach given WaterMover's straight-line, water-only
        // movement (see WaterMover's own doc comment). A land Soldier or
        // inland Town Center is structurally unreachable, so it's never a
        // candidate here even if it's the AI's designated
        // HostileTargetFaction().
        private Attackable FindNearestNavalTarget()
        {
            FactionId targetFaction = HostileTargetFaction();
            Attackable nearest = null;
            float bestDistance = float.MaxValue;

            foreach (Unit unit in Unit.All)
            {
                if (!unit.TryGetComponent(out FactionMember factionMember) || factionMember.Faction != targetFaction)
                {
                    continue;
                }

                if (!unit.TryGetComponent(out BoatAttacker _) && !unit.TryGetComponent(out BoatGatherer _))
                {
                    continue;
                }

                if (!unit.TryGetComponent(out Attackable attackable) || attackable.IsDead)
                {
                    continue;
                }

                float distance = Vector3.Distance(townCenterPosition, unit.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = attackable;
                }
            }

            if (nearest != null)
            {
                return nearest;
            }

            foreach (Building building in Building.All)
            {
                if (!(building is Dock)
                    || !building.TryGetComponent(out FactionMember factionMember)
                    || factionMember.Faction != targetFaction)
                {
                    continue;
                }

                if (!building.TryGetComponent(out Attackable attackable) || attackable.IsDead)
                {
                    continue;
                }

                float distance = Vector3.Distance(townCenterPosition, building.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = attackable;
                }
            }

            return nearest;
        }

        // Target-faction units take priority (soldiers wade through
        // defenders rather than beelining past them for a building),
        // buildings only considered when no unit is nearer - both are
        // Attackable now that buildings carry it too, so the attack squad
        // can raze a Town Center/Barracks/Farm/House same as it can kill a
        // unit. Item 48: targets whichever faction HostileTargetFaction()
        // currently resolves to, not always Player.
        private Attackable FindNearestPlayerTarget(Vector3 fromPosition)
        {
            FactionId targetFaction = HostileTargetFaction();
            Attackable nearest = null;
            float bestDistance = float.MaxValue;

            foreach (Unit unit in Unit.All)
            {
                if (!unit.TryGetComponent(out FactionMember factionMember)
                    || factionMember.Faction != targetFaction)
                {
                    continue;
                }

                if (!unit.TryGetComponent(out Attackable attackable) || attackable.IsDead)
                {
                    continue;
                }

                float distance = Vector3.Distance(fromPosition, unit.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = attackable;
                }
            }

            if (nearest != null)
            {
                return nearest;
            }

            foreach (Building building in Building.All)
            {
                if (!building.TryGetComponent(out FactionMember factionMember)
                    || factionMember.Faction != targetFaction)
                {
                    continue;
                }

                if (!building.TryGetComponent(out Attackable attackable) || attackable.IsDead)
                {
                    continue;
                }

                float distance = Vector3.Distance(fromPosition, building.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = attackable;
                }
            }

            return nearest;
        }

        private bool IsMine(Unit unit)
        {
            return unit.TryGetComponent(out FactionMember factionMember)
                && factionMember.Faction == myFaction;
        }

        // For EconomyFirst's TryBuildBarracks gate - counts Gatherers
        // specifically (Workers), not every Enemy unit, so an already-
        // trained Soldier/Archer/Cavalry/Siege doesn't count toward the
        // "real worker base" threshold the build order is actually about.
        private int CountMyWorkers()
        {
            int count = 0;
            foreach (Unit unit in Unit.All)
            {
                if (IsMine(unit) && unit.TryGetComponent(out Gatherer _))
                {
                    count++;
                }
            }

            return count;
        }

        // No mouse input available for the AI - fires its own downward ray
        // onto the Ground's existing MeshCollider instead of relying on
        // BuildingPlacer's ScreenPointToRay-based TryGetGroundPoint. See
        // GroundReference - filters to the actual Ground GameObject by
        // name rather than a Physics Layer, so this can't hit a nearby
        // unit or building's collider first and place a foundation at
        // that wrong height instead of the actual terrain.
        private static bool TryResolveGroundHeight(Vector3 xzPoint, out Vector3 point)
        {
            if (GroundReference.TryGetHeight(xzPoint, out float height))
            {
                point = new Vector3(xzPoint.x, height, xzPoint.z);
                return true;
            }

            point = Vector3.zero;
            return false;
        }
    }
}
