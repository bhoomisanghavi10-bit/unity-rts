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
        [SerializeField] private Vector3 townCenterPosition = new Vector3(0f, 1f, -8f);
        [SerializeField] private int startingWorkerCount = 4;
        [SerializeField] private float workerSpacing = 2f;
        [SerializeField] private float decisionInterval = 2f;
        [SerializeField] private float attackCheckInterval = 45f;
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

        // Local to this controller, not a mutation on ResourceNode itself -
        // keeps the "who's gathering what" bookkeeping contained to one file.
        private readonly HashSet<ResourceNode> _claimedNodes = new HashSet<ResourceNode>();

        private float _decisionTimer;
        private float _attackTimer;
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
        private float _scoutTimer;
        private float _scoutElapsed;
        private bool _hasScoutedPlayer;
        private Unit _scoutUnit;

        private void Start()
        {
            ApplyDifficulty();

            // Item 44: the selected map picks the AI's starting position;
            // RiverValley's matches this field's own default exactly.
            townCenterPosition = MapRegistry.Current.EnemyTownCenter;

            GameObject townCenterGo = TownCenterFactory.Place(townCenterPosition, FactionId.Enemy);
            townCenterGo.TryGetComponent(out _townCenter);

            for (int i = 0; i < startingWorkerCount; i++)
            {
                float x = i * workerSpacing - (startingWorkerCount - 1) * workerSpacing * 0.5f;
                Vector3 spawnPos = townCenterPosition + new Vector3(x, 0f, -3f);
                WorkerFactory.Spawn(spawnPos, FactionId.Enemy);
            }
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
            }

            _attackTimer += Time.deltaTime;
            if (_attackTimer >= attackCheckInterval)
            {
                _attackTimer = 0f;
                TryAttack();
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

        private bool IsNearAnyPlayerTarget(Vector3 fromPosition)
        {
            foreach (Unit unit in Unit.All)
            {
                if (unit.TryGetComponent(out FactionMember factionMember)
                    && factionMember.Faction == FactionId.Player
                    && Vector3.Distance(fromPosition, unit.transform.position) <= scoutDiscoveryRadius)
                {
                    return true;
                }
            }

            foreach (Building building in Building.All)
            {
                if (building.TryGetComponent(out FactionMember factionMember)
                    && factionMember.Faction == FactionId.Player
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

        private ResourceNode FindUnclaimedNode(Vector3 fromPosition, ResourceNode[] allNodes)
        {
            ResourceNode nearest = null;
            float bestDistance = float.MaxValue;

            foreach (ResourceNode node in allNodes)
            {
                if (_claimedNodes.Contains(node))
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
            if (_townCenter == null || _townCenter.IsAgingUp || !AgeProgress.HasNextAge(FactionId.Enemy))
            {
                return;
            }

            AgeProfile nextProfile = AgeProfile.For(AgeProgress.NextAge(FactionId.Enemy));
            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Enemy);
            if (stockpile.GetTotal(ResourceType.Wood) < nextProfile.WoodCost * ageUpResourceBuffer
                || stockpile.GetTotal(ResourceType.Stone) < nextProfile.StoneCost * ageUpResourceBuffer)
            {
                return;
            }

            _townCenter.RequestAgeUp();
        }

        private void TryBuildBarracks()
        {
            // Symmetric with the Player's own gate in BuildingPlacer -
            // the AI can't build a Barracks before Classical Age either.
            if (_barracks != null || AgeProgress.CurrentAge(FactionId.Enemy) == AgeId.Ancient)
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

            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Enemy);
            float multiplier = CivilizationProfile.For(CivilizationRegistry.For(FactionId.Enemy)).BuildCostMultiplier;
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

            GameObject go = BarracksFactory.Place(point, FactionId.Enemy, barracksBuildTime);
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
        // Soldiers, an Archer, a Cavalry, a Soldier, then a Siege,
        // repeating - so the enemy's attack squads benefit from
        // CombatBonus's full counter triangle (Infantry > Archer >
        // Cavalry > Infantry) instead of fielding just one class. Siege
        // is deliberately the rarest slot (1 in 6) - its 3x Building bonus
        // is wasted outside a siege against Walls/Towers/other buildings,
        // and it's slow and unremarkable against units in the meantime.
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
                case 5:
                    _barracks.RequestTrainSiege();
                    break;
                default:
                    _barracks.RequestTrain();
                    break;
            }

            _trainRotation = (_trainRotation + 1) % 6;
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

            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Enemy);

            if (!_barracks.IsResearchingAttack
                && UpgradeProgress.HasNextAttackTier(FactionId.Enemy)
                && stockpile.GetTotal(ResourceType.Gold) > 150f)
            {
                _barracks.RequestResearchAttack();
                return;
            }

            if (!_barracks.IsResearchingArmor
                && UpgradeProgress.HasNextArmorTier(FactionId.Enemy)
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
                && UpgradeProgress.HasNextClassAttackTier(FactionId.Enemy, UnitClass.Cavalry)
                && stockpile.GetTotal(ResourceType.Gold) > 150f)
            {
                _barracks.RequestResearchClassAttack(UnitClass.Cavalry);
                return;
            }

            if (!_barracks.IsResearchingClassArmor
                && UpgradeProgress.HasNextClassArmorTier(FactionId.Enemy, UnitClass.Archer)
                && stockpile.GetTotal(ResourceType.Gold) > 150f)
            {
                _barracks.RequestResearchClassArmor(UnitClass.Archer);
            }
        }

        private void TryBuildFarm()
        {
            if (_farm != null)
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Enemy);
            float multiplier = CivilizationProfile.For(CivilizationRegistry.For(FactionId.Enemy)).BuildCostMultiplier;
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

            GameObject go = FarmFactory.Place(point, FactionId.Enemy, farmBuildTime);
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

            if (Population.Cap(FactionId.Enemy) - Population.Current(FactionId.Enemy) > populationBuffer)
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Enemy);
            float multiplier = CivilizationProfile.For(CivilizationRegistry.For(FactionId.Enemy)).BuildCostMultiplier;
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

            GameObject go = HouseFactory.Place(point, FactionId.Enemy, houseBuildTime);
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

        // Player units take priority (soldiers wade through defenders
        // rather than beelining past them for a building), buildings only
        // considered when no player unit is nearer - both are Attackable
        // now that buildings carry it too, so the attack squad can raze a
        // Town Center/Barracks/Farm/House same as it can kill a unit.
        private static Attackable FindNearestPlayerTarget(Vector3 fromPosition)
        {
            Attackable nearest = null;
            float bestDistance = float.MaxValue;

            foreach (Unit unit in Unit.All)
            {
                if (!unit.TryGetComponent(out FactionMember factionMember)
                    || factionMember.Faction != FactionId.Player)
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
                    || factionMember.Faction != FactionId.Player)
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

        private static bool IsMine(Unit unit)
        {
            return unit.TryGetComponent(out FactionMember factionMember)
                && factionMember.Faction == FactionId.Enemy;
        }

        // For EconomyFirst's TryBuildBarracks gate - counts Gatherers
        // specifically (Workers), not every Enemy unit, so an already-
        // trained Soldier/Archer/Cavalry/Siege doesn't count toward the
        // "real worker base" threshold the build order is actually about.
        private static int CountMyWorkers()
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
