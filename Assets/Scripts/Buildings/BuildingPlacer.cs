using UnityEngine;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.Multiplayer.Wire;

namespace KingdomsOfBharat.Buildings
{
    // Placement flow for the Player's constructible buildings (Barracks,
    // Farm, House): triggered by the B/F/H hotkeys or BuildMenu's buttons,
    // both funnel through BeginPlacementBarracks()/BeginPlacementFarm()/
    // BeginPlacementHouse(). Move the mouse to preview it (green if
    // affordable and clear, red otherwise), left-click to confirm,
    // right-click/Escape to cancel. Always places for NetworkMatch.LocalFaction -
    // it's an inherently player-driven tool, not a spawned/faction-tagged
    // entity itself, so it reads "who am I" from there rather than trying
    // to derive it (defaults to FactionId.Player, matching every
    // single-player/AI-opponent match - see NetworkMatch.cs).
    // Actual GameObject creation is BarracksFactory's/FarmFactory's/
    // HouseFactory's job (shared with AiController's programmatic
    // placement, for Barracks and House).
    public class BuildingPlacer : MonoBehaviour
    {
        // Internal (not private) so EditMode tests can exercise
        // WoodMultiplierFor/StoneMultiplierFor directly - see
        // Assets/Scripts/AssemblyInfo.cs for the InternalsVisibleTo grant.
        internal enum BuildingKind { Barracks, Farm, House, Wall, Gate, Tower, Market, Dock, LumberCamp, MiningCamp, Mill }

        [Header("Barracks")]
        [SerializeField] private KeyCode placeBarracksKey = KeyCode.B;
        [SerializeField] private float barracksWoodCost = 100f;
        [SerializeField] private float barracksStoneCost = 50f;
        [SerializeField] private float barracksBuildTime = 8f;
        // X/Z match BarracksFactory's real BuildingFootprint tile size -
        // Y is the ghost cube's visual height only, unrelated to footprint.
        [SerializeField] private Vector3 barracksSize = new Vector3(4f, 2f, 4f);

        [Header("Farm")]
        [SerializeField] private KeyCode placeFarmKey = KeyCode.F;
        [SerializeField] private float farmWoodCost = 60f;
        [SerializeField] private float farmBuildTime = 5f;
        [SerializeField] private Vector3 farmSize = new Vector3(2f, 0.6f, 2f);

        [Header("House")]
        [SerializeField] private KeyCode placeHouseKey = KeyCode.H;
        [SerializeField] private float houseWoodCost = 30f;
        [SerializeField] private float houseBuildTime = 4f;
        [SerializeField] private Vector3 houseSize = new Vector3(2f, 1.6f, 2f);

        [Header("Wall")]
        [SerializeField] private KeyCode placeWallKey = KeyCode.L;
        [SerializeField] private float wallStoneCost = 5f;
        [SerializeField] private float wallBuildTime = 3f;
        [SerializeField] private Vector3 wallSize = new Vector3(2.4f, 1.8f, 0.4f);
        // Small enough that segments can sit edge-to-edge in a line, same
        // as AoE wall chains - BuildingFootprint's square-tile clearance
        // (used by every other kind below) would otherwise leave gaps
        // between wall segments wide enough to walk through.
        [SerializeField] private float wallClearance = 1.5f;

        [Header("Gate")]
        [SerializeField] private KeyCode placeGateKey = KeyCode.K;
        [SerializeField] private float gateStoneCost = 10f;
        [SerializeField] private float gateWoodCost = 5f;
        [SerializeField] private float gateBuildTime = 4f;
        [SerializeField] private Vector3 gateSize = new Vector3(2.4f, 1.8f, 0.4f);

        [Header("Tower")]
        [SerializeField] private KeyCode placeTowerKey = KeyCode.O;
        [SerializeField] private float towerWoodCost = 25f;
        [SerializeField] private float towerStoneCost = 50f;
        [SerializeField] private float towerBuildTime = 10f;
        [SerializeField] private Vector3 towerSize = new Vector3(2f, 4.4f, 2f);

        [Header("Market")]
        [SerializeField] private KeyCode placeMarketKey = KeyCode.M;
        [SerializeField] private float marketWoodCost = 100f;
        [SerializeField] private float marketGoldCost = 50f;
        [SerializeField] private float marketBuildTime = 8f;
        [SerializeField] private Vector3 marketSize = new Vector3(3f, 1.6f, 3f);

        [Header("Dock")]
        [SerializeField] private KeyCode placeDockKey = KeyCode.N;
        [SerializeField] private float dockWoodCost = 80f;
        [SerializeField] private float dockStoneCost = 20f;
        [SerializeField] private float dockBuildTime = 6f;
        [SerializeField] private Vector3 dockSize = new Vector3(2.2f, 0.6f, 4f);
        // Item 49: how close to the water rectangle's edge a Dock must be
        // placed - a Dock has to actually reach the water to be useful
        // (boats spawn/dock there), but the foundation itself sits on
        // land (there's no ground collider inside the water hole to place
        // on - see ProceduralGround).
        [SerializeField] private float dockMaxWaterDistance = 4f;

        // Phase 3.1 (resource-specific drop-offs): Lumber Camp/Mining
        // Camp/Mill, added so Gatherer.FindNearestDropOff can route by
        // resource type instead of always defaulting to the TownCenter -
        // see Gatherer.AcceptsDropOff. Costs/build time picked from the
        // same range as House/Farm (100 Wood, no Stone, matching AoE's
        // own cheap-early-economy-building convention for these).
        [Header("Lumber Camp")]
        [SerializeField] private KeyCode placeLumberCampKey = KeyCode.J;
        [SerializeField] private float lumberCampWoodCost = 100f;
        [SerializeField] private float lumberCampBuildTime = 5f;
        [SerializeField] private Vector3 lumberCampSize = new Vector3(2f, 1.4f, 2f);

        [Header("Mining Camp")]
        [SerializeField] private KeyCode placeMiningCampKey = KeyCode.U;
        [SerializeField] private float miningCampWoodCost = 100f;
        [SerializeField] private float miningCampBuildTime = 5f;
        [SerializeField] private Vector3 miningCampSize = new Vector3(2f, 1.4f, 2f);

        [Header("Mill")]
        [SerializeField] private KeyCode placeMillKey = KeyCode.P;
        [SerializeField] private float millWoodCost = 100f;
        [SerializeField] private float millBuildTime = 5f;
        [SerializeField] private Vector3 millSize = new Vector3(2f, 1.4f, 2f);

        // SelectionManager checks this so a click meant to place/cancel a
        // building doesn't also register as a select/move/gather command.
        public static bool IsPlacing { get; private set; }

        private UnityEngine.Camera _camera;
        private GameObject _ghost;
        private bool _placing;
        private BuildingKind _kind;

        private void Awake()
        {
            _camera = UnityEngine.Camera.main;
            ApplyKeySettings();
        }

        // Item 46: same per-field GameSettings override pattern used
        // across SelectionManager/Barracks/TownCenter, just seven fields
        // at once instead of one.
        private void ApplyKeySettings()
        {
            placeBarracksKey = GameSettings.GetKey("PlaceBarracks", placeBarracksKey);
            placeFarmKey = GameSettings.GetKey("PlaceFarm", placeFarmKey);
            placeHouseKey = GameSettings.GetKey("PlaceHouse", placeHouseKey);
            placeWallKey = GameSettings.GetKey("PlaceWall", placeWallKey);
            placeGateKey = GameSettings.GetKey("PlaceGate", placeGateKey);
            placeTowerKey = GameSettings.GetKey("PlaceTower", placeTowerKey);
            placeMarketKey = GameSettings.GetKey("PlaceMarket", placeMarketKey);
            placeDockKey = GameSettings.GetKey("PlaceDock", placeDockKey);
            placeLumberCampKey = GameSettings.GetKey("PlaceLumberCamp", placeLumberCampKey);
            placeMiningCampKey = GameSettings.GetKey("PlaceMiningCamp", placeMiningCampKey);
            placeMillKey = GameSettings.GetKey("PlaceMill", placeMillKey);
        }

        // Gated behind Classical Age - gives the Age system real teeth
        // (rather than pure stat bonuses) and mirrors the same gate
        // AiController's TryBuildBarracks() checks for the Enemy faction,
        // so the requirement is symmetric between Player and AI.
        public void BeginPlacementBarracks()
        {
            if (!_placing && AgeProgress.CurrentAge(NetworkMatch.LocalFaction) != AgeId.Ancient)
            {
                StartPlacing(BuildingKind.Barracks);
            }
        }

        // For BuildMenu, to show/disable the Build Barracks button.
        public static bool CanPlaceBarracks => AgeProgress.CurrentAge(NetworkMatch.LocalFaction) != AgeId.Ancient;

        public void BeginPlacementFarm()
        {
            if (!_placing)
            {
                StartPlacing(BuildingKind.Farm);
            }
        }

        public void BeginPlacementHouse()
        {
            if (!_placing)
            {
                StartPlacing(BuildingKind.House);
            }
        }

        public void BeginPlacementWall()
        {
            if (!_placing)
            {
                StartPlacing(BuildingKind.Wall);
            }
        }

        public void BeginPlacementGate()
        {
            if (!_placing)
            {
                StartPlacing(BuildingKind.Gate);
            }
        }

        public void BeginPlacementTower()
        {
            if (!_placing)
            {
                StartPlacing(BuildingKind.Tower);
            }
        }

        public void BeginPlacementMarket()
        {
            if (!_placing)
            {
                StartPlacing(BuildingKind.Market);
            }
        }

        // Gated on the current map actually having water - no point
        // offering Dock placement on RiverValley/Highlands.
        public void BeginPlacementDock()
        {
            if (!_placing && WaterProximity.HasWater)
            {
                StartPlacing(BuildingKind.Dock);
            }
        }

        // For BuildMenu, to show/disable the Build Dock button.
        public static bool CanPlaceDock => WaterProximity.HasWater;

        public void BeginPlacementLumberCamp()
        {
            if (!_placing)
            {
                StartPlacing(BuildingKind.LumberCamp);
            }
        }

        public void BeginPlacementMiningCamp()
        {
            if (!_placing)
            {
                StartPlacing(BuildingKind.MiningCamp);
            }
        }

        public void BeginPlacementMill()
        {
            if (!_placing)
            {
                StartPlacing(BuildingKind.Mill);
            }
        }

        private void Update()
        {
            if (!_placing)
            {
                if (Input.GetKeyDown(placeBarracksKey))
                {
                    BeginPlacementBarracks();
                }
                else if (Input.GetKeyDown(placeFarmKey))
                {
                    BeginPlacementFarm();
                }
                else if (Input.GetKeyDown(placeHouseKey))
                {
                    BeginPlacementHouse();
                }
                else if (Input.GetKeyDown(placeWallKey))
                {
                    BeginPlacementWall();
                }
                else if (Input.GetKeyDown(placeGateKey))
                {
                    BeginPlacementGate();
                }
                else if (Input.GetKeyDown(placeTowerKey))
                {
                    BeginPlacementTower();
                }
                else if (Input.GetKeyDown(placeMarketKey))
                {
                    BeginPlacementMarket();
                }
                else if (Input.GetKeyDown(placeDockKey))
                {
                    BeginPlacementDock();
                }
                else if (Input.GetKeyDown(placeLumberCampKey))
                {
                    BeginPlacementLumberCamp();
                }
                else if (Input.GetKeyDown(placeMiningCampKey))
                {
                    BeginPlacementMiningCamp();
                }
                else if (Input.GetKeyDown(placeMillKey))
                {
                    BeginPlacementMill();
                }
            }

            if (!_placing)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelPlacing();
                return;
            }

            if (KingdomsOfBharat.Camera.MinimapController.IsPointerOverMinimap)
            {
                return;
            }

            UpdateGhost();

            if (Input.GetMouseButtonDown(0))
            {
                TryConfirmPlacement();
            }
        }

        private void StartPlacing(BuildingKind kind)
        {
            _kind = kind;
            _placing = true;
            IsPlacing = true;
            _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ghost.name = "PlacementGhost";
            _ghost.transform.localScale = CurrentSize();
            Destroy(_ghost.GetComponent<Collider>());

            var renderer = _ghost.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GameplayMaterial.CreateTransparent(Color.white);
        }

        private void CancelPlacing()
        {
            _placing = false;
            IsPlacing = false;
            Destroy(_ghost);
        }

        private void UpdateGhost()
        {
            if (!TryGetGroundPoint(out Vector3 point))
            {
                return;
            }

            Vector3 size = CurrentSize();
            _ghost.transform.position = point + Vector3.up * (size.y * 0.5f);

            bool affordable = CanAfford(_kind);
            bool clear = IsClearForKind(_kind, point);
            var renderer = _ghost.GetComponent<MeshRenderer>();
            renderer.sharedMaterial.color = affordable && clear
                ? new Color(0.3f, 1f, 0.3f, 0.5f)
                : new Color(1f, 0.3f, 0.3f, 0.5f);
        }

        // Phase 6 gap-close: Maurya's "Houses cost no Wood" bonus. Not
        // representable as a passiveBonuses StatModifier at all (no
        // Building entry in UnitCategory - see CsvToScriptableObject.cs's
        // BuildPassiveBonus), so this is a hand-picked civ check, same
        // shape as UniqueTechDefinition's per-civ dictionary.
        internal static float WoodMultiplierFor(BuildingKind kind)
        {
            if (kind != BuildingKind.House)
            {
                return 1f;
            }

            if (CivilizationRegistry.For(NetworkMatch.LocalFaction) == CivilizationId.Maurya)
            {
                return 0f;
            }

            // AoE-parity Phase 3.2: Maurya's team bonus - allied factions'
            // Houses cost 25% less Wood (a diluted version of Maurya's own
            // free-Houses bonus above). Player-only, same scope as this
            // method already has - see TeamBonus.cs.
            return TeamBonus.HasAlly(NetworkMatch.LocalFaction, CivilizationId.Maurya)
                ? TeamBonus.MauryaHouseWoodMultiplier
                : 1f;
        }

        // Phase 6 gap-close: Vijayanagara's "Wall/Gate/Tower cost 20% less
        // Stone" bonus - same non-representable-in-passiveBonuses reasoning
        // as WoodMultiplierFor above.
        internal static float StoneMultiplierFor(BuildingKind kind)
        {
            bool isFortification = kind == BuildingKind.Wall || kind == BuildingKind.Gate || kind == BuildingKind.Tower;
            if (isFortification && CivilizationRegistry.For(NetworkMatch.LocalFaction) == CivilizationId.Vijayanagara)
            {
                return 0.8f;
            }
            return 1f;
        }

        // AoE-Parity Phase 5 gap-close: this used to deduct resources and
        // call XFactory.Place directly at click time - the one remaining
        // Player-input path that bypassed CommandBus (Move/Attack/Train
        // orders already went through it). Now only does the client-side
        // "is this obviously doomed" pre-check (so a hopeless click doesn't
        // enqueue a pointless command) and enqueues a BuildCommand; the real
        // spend + spawn happens InputDelayTicks later in ExecuteBuild below,
        // same deferred-execution shape as TrainCommand/Barracks.RequestTrain.
        private void TryConfirmPlacement()
        {
            if (!TryGetGroundPoint(out Vector3 point) || !IsClearForKind(_kind, point))
            {
                return;
            }

            if (!CanAfford(_kind))
            {
                return;
            }

            BuildingKind kind = _kind;
            FactionId faction = NetworkMatch.LocalFaction;
            int tick = CommandBus.Enqueue(new BuildCommand(faction, this, () => ExecuteBuild(kind, point)));

            // Phase 5 LAN transport MVP: the remote peer needs this exact
            // order too, scheduled for the exact same tick - see
            // CommandSerializer.cs/LanTransport.cs. No-op in single-player
            // (NetworkMatch.IsActive stays false).
            if (NetworkMatch.IsActive)
            {
                NetworkMatch.Transport.Send(CommandSerializer.ForBuild(tick, faction, ToNetBuildKind(kind), point));
            }

            CancelPlacing();
        }

        // The receiving peer's counterpart to TryConfirmPlacement's local
        // enqueue above - CommandSerializer.ToCommand resolves a received
        // NetBuildCommand into a BuildCommand wrapping a call to this
        // (internal rather than the private ExecuteBuild it forwards to,
        // exactly as much visibility as the network layer needs and no
        // more).
        internal void ExecuteBuildFromNetwork(NetBuildKind netKind, Vector3 point)
        {
            ExecuteBuild(ToBuildingKind(netKind), point);
        }

        private static NetBuildKind ToNetBuildKind(BuildingKind kind)
        {
            return kind switch
            {
                BuildingKind.Barracks => NetBuildKind.Barracks,
                BuildingKind.Farm => NetBuildKind.Farm,
                BuildingKind.House => NetBuildKind.House,
                BuildingKind.Wall => NetBuildKind.Wall,
                BuildingKind.Gate => NetBuildKind.Gate,
                BuildingKind.Tower => NetBuildKind.Tower,
                BuildingKind.Market => NetBuildKind.Market,
                BuildingKind.Dock => NetBuildKind.Dock,
                BuildingKind.LumberCamp => NetBuildKind.LumberCamp,
                BuildingKind.MiningCamp => NetBuildKind.MiningCamp,
                BuildingKind.Mill => NetBuildKind.Mill,
                _ => NetBuildKind.Farm,
            };
        }

        private static BuildingKind ToBuildingKind(NetBuildKind kind)
        {
            return kind switch
            {
                NetBuildKind.Barracks => BuildingKind.Barracks,
                NetBuildKind.Farm => BuildingKind.Farm,
                NetBuildKind.House => BuildingKind.House,
                NetBuildKind.Wall => BuildingKind.Wall,
                NetBuildKind.Gate => BuildingKind.Gate,
                NetBuildKind.Tower => BuildingKind.Tower,
                NetBuildKind.Market => BuildingKind.Market,
                NetBuildKind.Dock => BuildingKind.Dock,
                NetBuildKind.LumberCamp => BuildingKind.LumberCamp,
                NetBuildKind.MiningCamp => BuildingKind.MiningCamp,
                NetBuildKind.Mill => BuildingKind.Mill,
                _ => BuildingKind.Farm,
            };
        }

        // Re-checks IsClearForKind/CanAfford again before spending anything -
        // real game state (stockpile, other buildings) may have changed in
        // the delay window between the click and this executing, the same
        // reason Barracks.RequestTrain re-validates instead of trusting
        // TrainCommand's enqueue-time state. Silently no-ops if either check
        // now fails, matching RequestTrain's own convention.
        private void ExecuteBuild(BuildingKind kind, Vector3 point)
        {
            if (!IsClearForKind(kind, point) || !CanAfford(kind))
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(NetworkMatch.LocalFaction);
            // Phase 6 gap-close: EconomyTechProgress's TradeDiscounts tech
            // stacks multiplicatively with the civ's own build-cost bonus
            // (e.g. Chola's existing -15%), same "multiply everything
            // relevant together" convention as every other layered bonus.
            float multiplier = CivilizationProfile.For(CivilizationRegistry.For(NetworkMatch.LocalFaction)).BuildCostMultiplier
                * (EconomyTechProgress.HasResearched(NetworkMatch.LocalFaction, EconomyTech.TradeDiscounts) ? EconomyTechDefinition.For(EconomyTech.TradeDiscounts).Bonus : 1f);

            switch (kind)
            {
                case BuildingKind.Barracks:
                    stockpile.Add(ResourceType.Wood, -barracksWoodCost * multiplier);
                    stockpile.Add(ResourceType.Stone, -barracksStoneCost * multiplier);
                    BarracksFactory.Place(point, NetworkMatch.LocalFaction, barracksBuildTime);
                    break;
                case BuildingKind.Farm:
                    stockpile.Add(ResourceType.Wood, -farmWoodCost * multiplier);
                    FarmFactory.Place(point, NetworkMatch.LocalFaction, farmBuildTime);
                    break;
                case BuildingKind.House:
                    stockpile.Add(ResourceType.Wood, -houseWoodCost * multiplier * WoodMultiplierFor(kind));
                    HouseFactory.Place(point, NetworkMatch.LocalFaction, houseBuildTime);
                    break;
                case BuildingKind.Wall:
                    stockpile.Add(ResourceType.Stone, -wallStoneCost * multiplier * StoneMultiplierFor(kind));
                    WallFactory.Place(point, NetworkMatch.LocalFaction, wallBuildTime);
                    break;
                case BuildingKind.Gate:
                    stockpile.Add(ResourceType.Stone, -gateStoneCost * multiplier * StoneMultiplierFor(kind));
                    stockpile.Add(ResourceType.Wood, -gateWoodCost * multiplier);
                    GateFactory.Place(point, NetworkMatch.LocalFaction, gateBuildTime);
                    break;
                case BuildingKind.Tower:
                    stockpile.Add(ResourceType.Wood, -towerWoodCost * multiplier);
                    stockpile.Add(ResourceType.Stone, -towerStoneCost * multiplier * StoneMultiplierFor(kind));
                    TowerFactory.Place(point, NetworkMatch.LocalFaction, towerBuildTime);
                    break;
                case BuildingKind.Market:
                    stockpile.Add(ResourceType.Wood, -marketWoodCost * multiplier);
                    stockpile.Add(ResourceType.Gold, -marketGoldCost * multiplier);
                    MarketFactory.Place(point, NetworkMatch.LocalFaction, marketBuildTime);
                    break;
                case BuildingKind.Dock:
                    stockpile.Add(ResourceType.Wood, -dockWoodCost * multiplier);
                    stockpile.Add(ResourceType.Stone, -dockStoneCost * multiplier);
                    DockFactory.Place(point, NetworkMatch.LocalFaction, dockBuildTime);
                    break;
                case BuildingKind.LumberCamp:
                    stockpile.Add(ResourceType.Wood, -lumberCampWoodCost * multiplier);
                    LumberCampFactory.Place(point, NetworkMatch.LocalFaction, lumberCampBuildTime);
                    break;
                case BuildingKind.MiningCamp:
                    stockpile.Add(ResourceType.Wood, -miningCampWoodCost * multiplier);
                    MiningCampFactory.Place(point, NetworkMatch.LocalFaction, miningCampBuildTime);
                    break;
                case BuildingKind.Mill:
                    stockpile.Add(ResourceType.Wood, -millWoodCost * multiplier);
                    MillFactory.Place(point, NetworkMatch.LocalFaction, millBuildTime);
                    break;
            }
        }

        private bool CanAfford(BuildingKind kind)
        {
            ResourceStockpile stockpile = ResourceStockpile.For(NetworkMatch.LocalFaction);
            // Phase 6 gap-close: EconomyTechProgress's TradeDiscounts tech
            // stacks multiplicatively with the civ's own build-cost bonus
            // (e.g. Chola's existing -15%), same "multiply everything
            // relevant together" convention as every other layered bonus.
            float multiplier = CivilizationProfile.For(CivilizationRegistry.For(NetworkMatch.LocalFaction)).BuildCostMultiplier
                * (EconomyTechProgress.HasResearched(NetworkMatch.LocalFaction, EconomyTech.TradeDiscounts) ? EconomyTechDefinition.For(EconomyTech.TradeDiscounts).Bonus : 1f);

            switch (kind)
            {
                case BuildingKind.Barracks:
                    return stockpile.GetTotal(ResourceType.Wood) >= barracksWoodCost * multiplier
                        && stockpile.GetTotal(ResourceType.Stone) >= barracksStoneCost * multiplier;
                case BuildingKind.House:
                    return stockpile.GetTotal(ResourceType.Wood) >= houseWoodCost * multiplier * WoodMultiplierFor(kind);
                case BuildingKind.Wall:
                    return stockpile.GetTotal(ResourceType.Stone) >= wallStoneCost * multiplier * StoneMultiplierFor(kind);
                case BuildingKind.Gate:
                    return stockpile.GetTotal(ResourceType.Stone) >= gateStoneCost * multiplier * StoneMultiplierFor(kind)
                        && stockpile.GetTotal(ResourceType.Wood) >= gateWoodCost * multiplier;
                case BuildingKind.Tower:
                    return stockpile.GetTotal(ResourceType.Wood) >= towerWoodCost * multiplier
                        && stockpile.GetTotal(ResourceType.Stone) >= towerStoneCost * multiplier * StoneMultiplierFor(kind);
                case BuildingKind.Market:
                    return stockpile.GetTotal(ResourceType.Wood) >= marketWoodCost * multiplier
                        && stockpile.GetTotal(ResourceType.Gold) >= marketGoldCost * multiplier;
                case BuildingKind.Dock:
                    return stockpile.GetTotal(ResourceType.Wood) >= dockWoodCost * multiplier
                        && stockpile.GetTotal(ResourceType.Stone) >= dockStoneCost * multiplier;
                case BuildingKind.LumberCamp:
                    return stockpile.GetTotal(ResourceType.Wood) >= lumberCampWoodCost * multiplier;
                case BuildingKind.MiningCamp:
                    return stockpile.GetTotal(ResourceType.Wood) >= miningCampWoodCost * multiplier;
                case BuildingKind.Mill:
                    return stockpile.GetTotal(ResourceType.Wood) >= millWoodCost * multiplier;
                default:
                    return stockpile.GetTotal(ResourceType.Wood) >= farmWoodCost * multiplier;
            }
        }

        private Vector3 CurrentSize()
        {
            switch (_kind)
            {
                case BuildingKind.Barracks: return barracksSize;
                case BuildingKind.House: return houseSize;
                case BuildingKind.Wall: return wallSize;
                case BuildingKind.Gate: return gateSize;
                case BuildingKind.Tower: return towerSize;
                case BuildingKind.Market: return marketSize;
                case BuildingKind.Dock: return dockSize;
                case BuildingKind.LumberCamp: return lumberCampSize;
                case BuildingKind.MiningCamp: return miningCampSize;
                case BuildingKind.Mill: return millSize;
                default: return farmSize;
            }
        }

        // Wall/Gate keep their own pre-existing, smaller circle-distance
        // clearance check (wallClearance) so segments can still sit
        // edge-to-edge in a chain - unrelated to BuildingFootprint's
        // square-tile/margin system, which every other kind below uses
        // instead (see BuildingFootprint.cs).
        private Vector2 CurrentFootprint(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.Barracks: return BuildingFootprint.Square(BuildingFootprint.BarracksTiles);
                case BuildingKind.House: return BuildingFootprint.Square(BuildingFootprint.HouseTiles);
                case BuildingKind.Tower: return BuildingFootprint.Square(BuildingFootprint.TowerTiles);
                case BuildingKind.Market: return BuildingFootprint.Square(BuildingFootprint.MarketTiles);
                case BuildingKind.Dock: return new Vector2(dockSize.x, dockSize.z);
                case BuildingKind.LumberCamp:
                case BuildingKind.MiningCamp:
                case BuildingKind.Mill:
                    return BuildingFootprint.Square(BuildingFootprint.DropOffTiles);
                default: return BuildingFootprint.Square(BuildingFootprint.FarmTiles);
            }
        }

        // Item 49: Dock needs an extra gate beyond the generic "not on top
        // of another building" check every other kind uses - it has to
        // actually be near water to be useful, and can't be placed
        // directly inside the water rectangle itself (no ground collider
        // there to place a foundation on - see ProceduralGround's hole).
        //
        // AoE-Parity Phase 5 gap-close: takes kind explicitly rather than
        // reading the mutable _kind field - ExecuteBuild's deferred call
        // (up to InputDelayTicks after the click that captured this kind)
        // must re-check against the kind the command actually committed to,
        // not whatever _kind has since drifted to if the player started a
        // different placement in the meantime.
        private bool IsClearForKind(BuildingKind kind, Vector3 point)
        {
            bool clear = kind == BuildingKind.Wall || kind == BuildingKind.Gate
                ? BarracksFactory.IsClear(point, wallClearance)
                : BuildingFootprint.IsClear(point, CurrentFootprint(kind));

            if (kind != BuildingKind.Dock)
            {
                return clear;
            }

            return clear
                && !WaterProximity.IsInsideWater(point)
                && WaterProximity.DistanceToWater(point) <= dockMaxWaterDistance;
        }

        private bool TryGetGroundPoint(out Vector3 point)
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                point = hit.point;
                return true;
            }

            point = Vector3.zero;
            return false;
        }

    }
}
