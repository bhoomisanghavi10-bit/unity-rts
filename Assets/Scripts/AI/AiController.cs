using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.AI
{
    // Single scripted, timer-driven AI opponent - not adaptive/learning, a
    // deliberate first-pass simplification. Has full internal knowledge of
    // the map/player (no scouting logic); this is intentionally separate
    // from the player-facing fog-of-war grid (FogOfWarManager), which this
    // script never touches - "visible" here always means "known to the AI,"
    // never "revealed to the player."
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

        private void Start()
        {
            GameObject townCenterGo = TownCenterFactory.Place(townCenterPosition, FactionId.Enemy);
            townCenterGo.TryGetComponent(out _townCenter);

            for (int i = 0; i < startingWorkerCount; i++)
            {
                float x = i * workerSpacing - (startingWorkerCount - 1) * workerSpacing * 0.5f;
                Vector3 spawnPos = townCenterPosition + new Vector3(x, 0f, -3f);
                WorkerFactory.Spawn(spawnPos, FactionId.Enemy);
            }
        }

        private void Update()
        {
            _decisionTimer += Time.deltaTime;
            if (_decisionTimer >= decisionInterval)
            {
                _decisionTimer = 0f;
                AssignIdleWorkers();
                TryBuildBarracks();
                AssignBuilderIfNeeded();
                TryTrainSoldiers();
                TryBuildFarm();
                AssignFarmBuilderIfNeeded();
                AssignFarmWorkerIfNeeded();
                TryBuildHouse();
                AssignHouseBuilderIfNeeded();
                TryTrainWorkers();
            }

            _attackTimer += Time.deltaTime;
            if (_attackTimer >= attackCheckInterval)
            {
                _attackTimer = 0f;
                TryAttack();
            }
        }

        private void AssignIdleWorkers()
        {
            List<Unit> idleWorkers = new List<Unit>();
            foreach (Unit unit in Unit.All)
            {
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

        private void TryBuildBarracks()
        {
            if (_barracks != null)
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

        private void TryTrainSoldiers()
        {
            if (_barracks == null || !_barracks.IsComplete)
            {
                return;
            }

            _barracks.RequestTrain();
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

            Attackable target = FindNearestPlayerUnit(townCenterPosition);
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

        private static Attackable FindNearestPlayerUnit(Vector3 fromPosition)
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

            return nearest;
        }

        private static bool IsMine(Unit unit)
        {
            return unit.TryGetComponent(out FactionMember factionMember)
                && factionMember.Faction == FactionId.Enemy;
        }

        // No mouse input available for the AI - fires its own downward ray
        // onto the Ground's existing MeshCollider instead of relying on
        // BuildingPlacer's ScreenPointToRay-based TryGetGroundPoint.
        // Restricted to the Ground layer (see ProceduralGround /
        // HumanModelFactory.GroundLayerMask) so this can't hit a nearby
        // unit or building's collider first and place a foundation at
        // that wrong height instead of the actual terrain.
        private static bool TryResolveGroundHeight(Vector3 xzPoint, out Vector3 point)
        {
            Vector3 origin = new Vector3(xzPoint.x, 50f, xzPoint.z);
            // Resolved per-call rather than cached in a static field:
            // LayerMask.GetMask can't run from a MonoBehaviour's static
            // field initializer (Unity throws - it runs before the engine
            // is ready for that call), only from Awake/Start/Update or
            // later. AiController is a MonoBehaviour, so this has to be a
            // plain method call instead - cheap enough to not bother
            // caching for how infrequently this runs (once per building
            // placement attempt, not per frame).
            int mask = LayerMask.GetMask("Ground");
            if (mask == 0)
            {
                mask = ~0;
            }

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f, mask))
            {
                point = hit.point;
                return true;
            }

            point = Vector3.zero;
            return false;
        }
    }
}
