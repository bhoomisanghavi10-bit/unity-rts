using UnityEngine;
using UnityEngine.AI;
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
        internal enum BuildingKind { Barracks, Farm, House, Wall, Gate, Tower, Market, Dock, LumberCamp, MiningCamp, Mill, Durg, Karmashala, Monastery }

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
        // Wall system Session A: drag-placement chain tuning. Spacing
        // matches wallSize.x so segments sit flush edge-to-edge along the
        // drag line, same as the pre-existing manual one-at-a-time
        // edge-to-edge convention. The cap is a sane batch-size limit for
        // one drag, not a foundational design choice - tunable later.
        private const int MaxWallChainSegments = 30;

        // Ancient modular wall kit (2026-09-28): a genuine drag (2+
        // segments) always places Straight tiles - ComputeWallChain's math
        // assumes a uniform repeated tile, so a junction piece there
        // wouldn't line up. A plain click (the chain's own zero-drag
        // degenerate case, count==1) now auto-selects Straight/Corner/
        // EndPost/T/X from the click point's real neighbors instead of a
        // manual hotkey pick - see WallConnectivity.ClassifyPieceKind and
        // ConfirmWallChain/UpdateWallGhosts below (wall mechanics audit,
        // 2026-09-29 - replaces the removed Alpha1-5 selector, matching
        // AoE2's own always-automatic piece shape). Neighbor search radius
        // is a multiple of wallSize.x (not a fixed constant), so it stays
        // correct if that field is ever retuned in the Inspector.
        private const float WallNeighborDistanceMultiplier = 1.15f;

        // A junction piece is a taller, bulkier standalone structure than
        // one 2.4-wide straight tile - not independently balanced, just a
        // flat multiple of the per-segment Stone cost.
        private const float WallJunctionCostMultiplier = 3f;

        [Header("Gate")]
        [SerializeField] private KeyCode placeGateKey = KeyCode.K;
        // Costs scaled up alongside the 2026-09-28 Ancient modular kit's
        // real 3-tile-wide gate mesh (was tuned for the old 2.4-wide
        // placeholder box) - roughly proportional to the ~3x footprint.
        [SerializeField] private float gateStoneCost = 30f;
        [SerializeField] private float gateWoodCost = 15f;
        [SerializeField] private float gateBuildTime = 8f;
        [SerializeField] private Vector3 gateSize = new Vector3(7.2f, 6f, 2.6f);
        // Gate's own placement clearance (see IsClearForKind) - separate
        // from wallClearance since the widened gate needs a bigger radius
        // to register as clear of nearby buildings than a thin wall
        // segment does.
        [SerializeField] private float gateClearance = 4f;

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

        // Wave 2 item 7: the Durg building - AoE's Castle-equivalent, the
        // strongest defensive structure in the game (see DurgFactory.cs).
        // Gated to Durg age or later, same shape as BeginPlacementBarracks'
        // Classical-age gate below but one age later.
        [Header("Durg")]
        [SerializeField] private KeyCode placeDurgKey = KeyCode.D;
        [SerializeField] private float durgWoodCost = 200f;
        [SerializeField] private float durgStoneCost = 150f;
        [SerializeField] private float durgBuildTime = 25f;
        [SerializeField] private Vector3 durgSize = new Vector3(3.2f, 2.6f, 3.2f);

        // Wave 2 item 8: Karmashala, AoE's Blacksmith-equivalent - gated to
        // Classical Age, same as Barracks (CanPlaceKarmashala below). Wood
        // only, no Stone, matching AoE2's own real Blacksmith cost.
        [Header("Karmashala")]
        [SerializeField] private KeyCode placeKarmashalaKey = KeyCode.R;
        [SerializeField] private float karmashalaWoodCost = 150f;
        [SerializeField] private float karmashalaBuildTime = 10f;
        [SerializeField] private Vector3 karmashalaSize = new Vector3(2.4f, 1.8f, 2.4f);

        // Wave 4 item 27: Monastery - trains Vaidya/Purohita. Gated to Durg
        // Age, same as Durg itself - a mid/late-game unlock, not an early
        // building.
        [Header("Monastery")]
        [SerializeField] private KeyCode placeMonasteryKey = KeyCode.G;
        [SerializeField] private float monasteryWoodCost = 175f;
        [SerializeField] private float monasteryStoneCost = 100f;
        [SerializeField] private float monasteryBuildTime = 10f;
        [SerializeField] private Vector3 monasterySize = new Vector3(2.4f, 1.8f, 2.4f);

        // SelectionManager checks this so a click meant to place/cancel a
        // building doesn't also register as a select/move/gather command.
        public static bool IsPlacing { get; private set; }

        private UnityEngine.Camera _camera;
        private GameObject _ghost;
        private bool _placing;
        private BuildingKind _kind;

        // Wall system Session A: drag-placement state. _wallGhosts is a
        // pool reused across frames (grown as needed, extras deactivated
        // rather than destroyed) since a live drag recomputes the chain
        // every frame.
        private bool _wallDragActive;
        private Vector3 _wallDragAnchor;
        private readonly System.Collections.Generic.List<GameObject> _wallGhosts = new System.Collections.Generic.List<GameObject>();

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
            placeDurgKey = GameSettings.GetKey("PlaceDurg", placeDurgKey);
            placeKarmashalaKey = GameSettings.GetKey("PlaceKarmashala", placeKarmashalaKey);
            placeMonasteryKey = GameSettings.GetKey("PlaceMonastery", placeMonasteryKey);
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

        // Gated behind Durg Age - the structural unlock this building
        // provides (unique-unit training, strongest defense) shouldn't be
        // reachable earlier. Symmetric with AiController's own TryBuildDurg
        // gate.
        public void BeginPlacementDurg()
        {
            if (!_placing && CanPlaceDurg)
            {
                StartPlacing(BuildingKind.Durg);
            }
        }

        // For BuildMenu, to show/disable the Build Durg button.
        public static bool CanPlaceDurg => AgeProgress.CurrentAge(NetworkMatch.LocalFaction) >= AgeId.Durg;

        // Wave 2 item 8: gated behind Classical Age, same as Barracks -
        // user-confirmed decision (Karmashala is a foundational military
        // building, not a late-game unlock like Durg).
        public void BeginPlacementKarmashala()
        {
            if (!_placing && CanPlaceKarmashala)
            {
                StartPlacing(BuildingKind.Karmashala);
            }
        }

        // For BuildMenu, to show/disable the Build Karmashala button.
        public static bool CanPlaceKarmashala => AgeProgress.CurrentAge(NetworkMatch.LocalFaction) != AgeId.Ancient;

        // Wave 4 item 27: gated behind Durg Age - same gate as Durg itself
        // (CanPlaceDurg above), user-confirmed via AskUserQuestion (build a
        // new Monastery building for Vaidya/Purohita).
        public void BeginPlacementMonastery()
        {
            if (!_placing && CanPlaceMonastery)
            {
                StartPlacing(BuildingKind.Monastery);
            }
        }

        // For BuildMenu, to show/disable the Build Monastery button.
        public static bool CanPlaceMonastery => AgeProgress.CurrentAge(NetworkMatch.LocalFaction) >= AgeId.Durg;

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
                else if (Input.GetKeyDown(placeDurgKey))
                {
                    BeginPlacementDurg();
                }
                else if (Input.GetKeyDown(placeKarmashalaKey))
                {
                    BeginPlacementKarmashala();
                }
                else if (Input.GetKeyDown(placeMonasteryKey))
                {
                    BeginPlacementMonastery();
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

            if (_kind == BuildingKind.Wall)
            {
                UpdateWallDrag();
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
            // Wall uses its own ghost pool (_wallGhosts) instead of the
            // single shared _ghost every other kind uses - see
            // UpdateWallDrag.
            if (kind != BuildingKind.Wall)
            {
                _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _ghost.name = "PlacementGhost";
                _ghost.transform.localScale = CurrentSize();
                Destroy(_ghost.GetComponent<Collider>());

                var renderer = _ghost.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = GameplayMaterial.CreateTransparent(Color.white);
            }
        }

        private void CancelPlacing()
        {
            _placing = false;
            IsPlacing = false;
            Destroy(_ghost);
            _ghost = null;

            _wallDragActive = false;
            foreach (GameObject ghost in _wallGhosts)
            {
                Destroy(ghost);
            }
            _wallGhosts.Clear();
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
        // Building entry in UnitClass - see CsvToScriptableObject.cs's
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

            IssueBuildCommand(_kind, point, Quaternion.identity);
            CancelPlacing();
        }

        // Shared by TryConfirmPlacement (every non-Wall kind, one point per
        // placement session) and UpdateWallDrag's mouse-up handler (Wall,
        // one call per chain segment) - enqueues the local command and
        // sends the matching network message, unchanged from what
        // TryConfirmPlacement always did inline. Doesn't call
        // CancelPlacing() itself - callers decide when the placement
        // session actually ends (a chain confirms all its segments before
        // exiting placement mode once).
        private void IssueBuildCommand(BuildingKind kind, Vector3 point, Quaternion rotation, WallFactory.WallPieceKind pieceKind = WallFactory.WallPieceKind.Straight)
        {
            FactionId faction = NetworkMatch.LocalFaction;
            int tick = CommandBus.Enqueue(new BuildCommand(faction, this, () => ExecuteBuild(kind, point, rotation, pieceKind)));

            // Phase 5 LAN transport MVP: the remote peer needs this exact
            // order too, scheduled for the exact same tick - see
            // CommandSerializer.cs/LanTransport.cs. No-op in single-player
            // (NetworkMatch.IsActive stays false).
            if (NetworkMatch.IsActive)
            {
                NetworkMatch.SendCommand(CommandSerializer.ForBuild(tick, faction, ToNetBuildKind(kind), point, rotation.eulerAngles.y, (int)pieceKind));
            }
        }

        // Wall system Session A: mouse-down/drag/mouse-up chain placement.
        // Before the mouse is pressed, anchor == the live cursor point, so
        // ComputeWallChain trivially returns a single segment at the
        // cursor - identical to the old single-ghost preview, meaning a
        // plain click-without-drag is completely unaffected by this path.
        // Piece shape (Straight/Corner/EndPost/T/X) is now always derived
        // from real neighbors - see ClassifyChainSegment/
        // WallConnectivity.ClassifyPieceKind - instead of a manual
        // Alpha1-5 hotkey pick (removed, wall mechanics audit 2026-09-29:
        // AoE2 never lets the player choose a wall tile's shape).
        private void UpdateWallDrag()
        {
            bool hasGround = TryGetGroundPoint(out Vector3 current);

            if (!_wallDragActive && Input.GetMouseButtonDown(0) && hasGround)
            {
                _wallDragAnchor = current;
                _wallDragActive = true;
            }

            if (!hasGround)
            {
                return;
            }

            Vector3 anchor = _wallDragActive ? _wallDragAnchor : current;
            var chain = ComputeWallChain(anchor, current, wallSize.x, MaxWallChainSegments);
            UpdateWallGhosts(chain);

            if (_wallDragActive && Input.GetMouseButtonUp(0))
            {
                ConfirmWallChain(chain);
                CancelPlacing();
            }
        }

        // Pure/testable - see WallChainPlacementTests.cs. FromToRotation
        // (not LookRotation) because the Wall model's long axis is local
        // +X (its Size.x, 2.4, is the largest horizontal dimension), not
        // the +Z axis LookRotation aligns.
        internal static System.Collections.Generic.List<(Vector3 position, Quaternion rotation)> ComputeWallChain(
            Vector3 anchor, Vector3 current, float segmentSpacing, int maxSegments)
        {
            var result = new System.Collections.Generic.List<(Vector3, Quaternion)>();

            Vector3 flatDelta = new Vector3(current.x - anchor.x, 0f, current.z - anchor.z);
            float length = flatDelta.magnitude;
            int count = Mathf.Clamp(Mathf.RoundToInt(length / segmentSpacing) + 1, 1, maxSegments);

            Vector3 dir = length > 0.001f ? flatDelta.normalized : Vector3.right;
            Quaternion rotation = count > 1 ? Quaternion.FromToRotation(Vector3.right, dir) : Quaternion.identity;

            for (int i = 0; i < count; i++)
            {
                Vector3 position = anchor + dir * (segmentSpacing * i);
                result.Add((position, rotation));
            }

            return result;
        }

        // Wall mechanics audit (2026-09-29): the real neighbor set for
        // segment `index` in an in-progress chain - every OTHER segment in
        // this same chain (not yet real Buildings at preview/classify
        // time) plus every already-placed Wall/Gate, both within
        // wallSize.x * WallNeighborDistanceMultiplier of this segment's
        // own position. Shared by UpdateWallGhosts (preview) and
        // ConfirmWallChain (actual placement) so what the player sees is
        // exactly what gets built.
        private WallFactory.WallPieceKind ClassifyChainSegment(
            int index, System.Collections.Generic.List<(Vector3 position, Quaternion rotation)> chain)
        {
            Vector3 position = chain[index].position;
            float neighborDistance = wallSize.x * WallNeighborDistanceMultiplier;
            var neighbors = new System.Collections.Generic.List<Vector3>();

            for (int i = 0; i < chain.Count; i++)
            {
                if (i == index)
                {
                    continue;
                }
                if (FlatDistance(position, chain[i].position) <= neighborDistance)
                {
                    neighbors.Add(chain[i].position);
                }
            }

            foreach (Building building in Building.All)
            {
                if (!(building is Wall || building is Gate))
                {
                    continue;
                }
                if (FlatDistance(position, building.transform.position) <= neighborDistance)
                {
                    neighbors.Add(building.transform.position);
                }
            }

            return WallConnectivity.ClassifyPieceKind(position, neighbors);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            return Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
        }

        private void UpdateWallGhosts(System.Collections.Generic.List<(Vector3 position, Quaternion rotation)> chain)
        {
            while (_wallGhosts.Count < chain.Count)
            {
                GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ghost.name = "WallPlacementGhost";
                Destroy(ghost.GetComponent<Collider>());
                ghost.GetComponent<MeshRenderer>().sharedMaterial = GameplayMaterial.CreateTransparent(Color.white);
                _wallGhosts.Add(ghost);
            }

            for (int i = 0; i < _wallGhosts.Count; i++)
            {
                bool active = i < chain.Count;
                _wallGhosts[i].SetActive(active);
                if (!active)
                {
                    continue;
                }

                (Vector3 position, Quaternion rotation) = chain[i];
                if (!TryGetGroundHeightAt(position, out Vector3 grounded))
                {
                    grounded = position;
                }

                // Each ghost previews its own real, neighbor-derived
                // piece shape - see ClassifyChainSegment.
                WallFactory.WallPieceKind previewKind = ClassifyChainSegment(i, chain);
                Vector3 ghostSize = WallFactory.PieceSize(previewKind);

                GameObject ghost = _wallGhosts[i];
                ghost.transform.SetPositionAndRotation(grounded + Vector3.up * (ghostSize.y * 0.5f), rotation);
                ghost.transform.localScale = ghostSize;

                // Cumulative affordability preview: the tail of a long
                // drag reddens once the running cost would exceed the
                // live stockpile, mirroring AoE's own "greys out what you
                // can't afford" chain-drag cue. The authoritative check
                // still happens per-segment at confirm/execute time
                // (IsClearForKind/CanAfford), so this is preview-only and
                // can't itself let an unaffordable segment through.
                bool clear = IsClearForKind(BuildingKind.Wall, grounded);
                bool affordableSoFar = chain.Count == 1
                    ? CanAfford(BuildingKind.Wall, previewKind)
                    : CanAffordWallCount(i + 1);
                ghost.GetComponent<MeshRenderer>().sharedMaterial.color = clear && affordableSoFar
                    ? new Color(0.3f, 1f, 0.3f, 0.5f)
                    : new Color(1f, 0.3f, 0.3f, 0.5f);
            }
        }

        // How many of the first N wall segments in a chain the current
        // live stockpile could cover, given Wall's own per-segment Stone
        // cost/multipliers - reuses the exact same cost formula CanAfford
        // already computes for a single Wall, just checked against N times
        // the cost instead of asserting a boolean once.
        private bool CanAffordWallCount(int segmentIndex)
        {
            ResourceStockpile stockpile = ResourceStockpile.For(NetworkMatch.LocalFaction);
            float multiplier = CivilizationProfile.For(CivilizationRegistry.For(NetworkMatch.LocalFaction)).BuildCostMultiplier
                * (EconomyTechProgress.HasResearched(NetworkMatch.LocalFaction, EconomyTech.TradeDiscounts) ? EconomyTechDefinition.For(EconomyTech.TradeDiscounts).Bonus : 1f);
            float perSegmentCost = wallStoneCost * multiplier * StoneMultiplierFor(BuildingKind.Wall);
            return stockpile.GetTotal(ResourceType.Stone) >= perSegmentCost * segmentIndex;
        }

        // Issues one BuildCommand per chain segment, skipping (not
        // enqueuing) any segment that fails clearance/affordability at
        // confirm time - matches "build as many as currently valid" rather
        // than an all-or-nothing chain. Each segment's own deferred
        // ExecuteBuild call re-validates again anyway (see ExecuteBuild's
        // own comment), so this is a courtesy pre-check, not the only
        // guard.
        private void ConfirmWallChain(System.Collections.Generic.List<(Vector3 position, Quaternion rotation)> chain)
        {
            for (int i = 0; i < chain.Count; i++)
            {
                (Vector3 position, Quaternion rotation) = chain[i];
                if (!TryGetGroundHeightAt(position, out Vector3 grounded))
                {
                    continue;
                }

                // Piece shape is derived from real neighbors, exactly
                // matching whatever UpdateWallGhosts already previewed for
                // this same segment - see ClassifyChainSegment.
                WallFactory.WallPieceKind pieceKind = ClassifyChainSegment(i, chain);

                if (!IsClearForKind(BuildingKind.Wall, grounded) || !CanAfford(BuildingKind.Wall, pieceKind))
                {
                    continue;
                }

                IssueBuildCommand(BuildingKind.Wall, grounded, rotation, pieceKind);
            }
        }

        // The receiving peer's counterpart to TryConfirmPlacement's local
        // enqueue above - CommandSerializer.ToCommand resolves a received
        // NetBuildCommand into a BuildCommand wrapping a call to this
        // (internal rather than the private ExecuteBuild it forwards to,
        // exactly as much visibility as the network layer needs and no
        // more).
        internal void ExecuteBuildFromNetwork(NetBuildKind netKind, Vector3 point, float rotationY, int wallPieceKind = 0)
        {
            ExecuteBuild(ToBuildingKind(netKind), point, Quaternion.Euler(0f, rotationY, 0f), (WallFactory.WallPieceKind)wallPieceKind);
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
                BuildingKind.Durg => NetBuildKind.Durg,
                BuildingKind.Karmashala => NetBuildKind.Karmashala,
                BuildingKind.Monastery => NetBuildKind.Monastery,
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
                NetBuildKind.Durg => BuildingKind.Durg,
                NetBuildKind.Karmashala => BuildingKind.Karmashala,
                NetBuildKind.Monastery => BuildingKind.Monastery,
                _ => BuildingKind.Farm,
            };
        }

        // Re-checks IsClearForKind/CanAfford again before spending anything -
        // real game state (stockpile, other buildings) may have changed in
        // the delay window between the click and this executing, the same
        // reason Barracks.RequestTrain re-validates instead of trusting
        // TrainCommand's enqueue-time state. Silently no-ops if either check
        // now fails, matching RequestTrain's own convention.
        private void ExecuteBuild(BuildingKind kind, Vector3 point, Quaternion rotation, WallFactory.WallPieceKind pieceKind = WallFactory.WallPieceKind.Straight)
        {
            if (!IsClearForKind(kind, point) || !CanAfford(kind, pieceKind))
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

            // AoE-style "footprint placed over a tree permanently removes
            // it" rule - IsClearForKind already let this through (trees
            // don't block placement, only gold/stone/farm-type resources
            // do), so this is the moment the footprint is actually
            // committed and any tree under it is felled for good.
            BuildingFootprint.ClearTreesInFootprint(point, ResourceCheckFootprint(kind));

            GameObject spawned = null;
            switch (kind)
            {
                case BuildingKind.Barracks:
                {
                    float wood = barracksWoodCost * multiplier;
                    float stone = barracksStoneCost * multiplier;
                    stockpile.Add(ResourceType.Wood, -wood);
                    stockpile.Add(ResourceType.Stone, -stone);
                    spawned = BarracksFactory.Place(point, NetworkMatch.LocalFaction, barracksBuildTime);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    RecordCost(spawned, ResourceType.Stone, stone);
                    break;
                }
                case BuildingKind.Farm:
                {
                    float wood = farmWoodCost * multiplier;
                    stockpile.Add(ResourceType.Wood, -wood);
                    spawned = FarmFactory.Place(point, NetworkMatch.LocalFaction, farmBuildTime);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    break;
                }
                case BuildingKind.House:
                {
                    float wood = houseWoodCost * multiplier * WoodMultiplierFor(kind);
                    stockpile.Add(ResourceType.Wood, -wood);
                    spawned = HouseFactory.Place(point, NetworkMatch.LocalFaction, houseBuildTime);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    break;
                }
                case BuildingKind.Wall:
                {
                    // Ancient modular wall kit (2026-09-28): a junction
                    // piece (Corner/EndPost/T/X) is a taller, sturdier
                    // structure than one straight tile - costs more Stone
                    // proportionally, same multiplier this whole switch
                    // already uses for every other bonus/discount stack.
                    float pieceCostMultiplier = pieceKind == WallFactory.WallPieceKind.Straight ? 1f : WallJunctionCostMultiplier;
                    float stone = wallStoneCost * multiplier * StoneMultiplierFor(kind) * pieceCostMultiplier;
                    stockpile.Add(ResourceType.Stone, -stone);
                    spawned = WallFactory.Place(point, NetworkMatch.LocalFaction, wallBuildTime, rotation, pieceKind);
                    RecordCost(spawned, ResourceType.Stone, stone);
                    Vector3 wallPieceSize = WallFactory.PieceSize(pieceKind);
                    SealDiagonalGaps(spawned, new Vector2(wallPieceSize.x, wallPieceSize.z));
                    break;
                }
                case BuildingKind.Gate:
                {
                    // Wall mechanics audit: absorb any same-faction Wall
                    // segment(s) this Gate's own footprint now overlaps -
                    // see AbsorbWalls/FindAbsorbableWalls. Must happen
                    // before Place() spawns the Gate (which shares this
                    // exact spot), and after the re-validated
                    // IsClearForKind check above already let it through
                    // (that check excludes same-faction Walls specifically
                    // so they don't block a Gate that's about to absorb
                    // them - see IsClearForGate).
                    AbsorbWalls(FindAbsorbableWalls(point, gateSize, NetworkMatch.LocalFaction), NetworkMatch.LocalFaction);

                    float stone = gateStoneCost * multiplier * StoneMultiplierFor(kind);
                    float wood = gateWoodCost * multiplier;
                    stockpile.Add(ResourceType.Stone, -stone);
                    stockpile.Add(ResourceType.Wood, -wood);
                    spawned = GateFactory.Place(point, NetworkMatch.LocalFaction, gateBuildTime);
                    RecordCost(spawned, ResourceType.Stone, stone);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    SealDiagonalGaps(spawned, new Vector2(gateSize.x, gateSize.z));
                    break;
                }
                case BuildingKind.Tower:
                {
                    float wood = towerWoodCost * multiplier;
                    float stone = towerStoneCost * multiplier * StoneMultiplierFor(kind);
                    stockpile.Add(ResourceType.Wood, -wood);
                    stockpile.Add(ResourceType.Stone, -stone);
                    spawned = TowerFactory.Place(point, NetworkMatch.LocalFaction, towerBuildTime);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    RecordCost(spawned, ResourceType.Stone, stone);
                    break;
                }
                case BuildingKind.Market:
                {
                    float wood = marketWoodCost * multiplier;
                    float gold = marketGoldCost * multiplier;
                    stockpile.Add(ResourceType.Wood, -wood);
                    stockpile.Add(ResourceType.Gold, -gold);
                    spawned = MarketFactory.Place(point, NetworkMatch.LocalFaction, marketBuildTime);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    RecordCost(spawned, ResourceType.Gold, gold);
                    break;
                }
                case BuildingKind.Dock:
                {
                    float wood = dockWoodCost * multiplier;
                    float stone = dockStoneCost * multiplier;
                    stockpile.Add(ResourceType.Wood, -wood);
                    stockpile.Add(ResourceType.Stone, -stone);
                    spawned = DockFactory.Place(point, NetworkMatch.LocalFaction, dockBuildTime);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    RecordCost(spawned, ResourceType.Stone, stone);
                    break;
                }
                case BuildingKind.LumberCamp:
                {
                    float wood = lumberCampWoodCost * multiplier;
                    stockpile.Add(ResourceType.Wood, -wood);
                    spawned = LumberCampFactory.Place(point, NetworkMatch.LocalFaction, lumberCampBuildTime);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    break;
                }
                case BuildingKind.MiningCamp:
                {
                    float wood = miningCampWoodCost * multiplier;
                    stockpile.Add(ResourceType.Wood, -wood);
                    spawned = MiningCampFactory.Place(point, NetworkMatch.LocalFaction, miningCampBuildTime);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    break;
                }
                case BuildingKind.Mill:
                {
                    float wood = millWoodCost * multiplier;
                    stockpile.Add(ResourceType.Wood, -wood);
                    spawned = MillFactory.Place(point, NetworkMatch.LocalFaction, millBuildTime);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    break;
                }
                case BuildingKind.Durg:
                {
                    float wood = durgWoodCost * multiplier;
                    float stone = durgStoneCost * multiplier;
                    stockpile.Add(ResourceType.Wood, -wood);
                    stockpile.Add(ResourceType.Stone, -stone);
                    spawned = DurgFactory.Place(point, NetworkMatch.LocalFaction, durgBuildTime);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    RecordCost(spawned, ResourceType.Stone, stone);
                    break;
                }
                case BuildingKind.Karmashala:
                {
                    float wood = karmashalaWoodCost * multiplier;
                    stockpile.Add(ResourceType.Wood, -wood);
                    spawned = KarmashalaFactory.Place(point, NetworkMatch.LocalFaction, karmashalaBuildTime);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    break;
                }
                case BuildingKind.Monastery:
                {
                    float wood = monasteryWoodCost * multiplier;
                    float stone = monasteryStoneCost * multiplier;
                    stockpile.Add(ResourceType.Wood, -wood);
                    stockpile.Add(ResourceType.Stone, -stone);
                    spawned = MonasteryFactory.Place(point, NetworkMatch.LocalFaction, monasteryBuildTime);
                    RecordCost(spawned, ResourceType.Wood, wood);
                    RecordCost(spawned, ResourceType.Stone, stone);
                    break;
                }
            }
        }

        // Attaches (or reuses) a BuildingCost on the just-spawned
        // foundation and records one resource line of what was actually
        // deducted above - see BuildingCost.cs/ConstructionSite.CancelAndRefund
        // for why this is tracked (a canceled foundation refunds a
        // fraction of the real spend, not a hardcoded guess).
        private static void RecordCost(GameObject building, ResourceType type, float amount)
        {
            if (building == null || amount <= 0f)
            {
                return;
            }

            if (!building.TryGetComponent(out BuildingCost cost))
            {
                cost = building.AddComponent<BuildingCost>();
            }

            cost.Record(type, amount);
        }

        private bool CanAfford(BuildingKind kind, WallFactory.WallPieceKind pieceKind = WallFactory.WallPieceKind.Straight)
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
                {
                    float pieceCostMultiplier = pieceKind == WallFactory.WallPieceKind.Straight ? 1f : WallJunctionCostMultiplier;
                    return stockpile.GetTotal(ResourceType.Stone) >= wallStoneCost * multiplier * StoneMultiplierFor(kind) * pieceCostMultiplier;
                }
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
                case BuildingKind.Durg:
                    return stockpile.GetTotal(ResourceType.Wood) >= durgWoodCost * multiplier
                        && stockpile.GetTotal(ResourceType.Stone) >= durgStoneCost * multiplier;
                case BuildingKind.Karmashala:
                    return stockpile.GetTotal(ResourceType.Wood) >= karmashalaWoodCost * multiplier;
                case BuildingKind.Monastery:
                    return stockpile.GetTotal(ResourceType.Wood) >= monasteryWoodCost * multiplier
                        && stockpile.GetTotal(ResourceType.Stone) >= monasteryStoneCost * multiplier;
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
                case BuildingKind.Durg: return durgSize;
                case BuildingKind.Karmashala: return karmashalaSize;
                case BuildingKind.Monastery: return monasterySize;
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
                case BuildingKind.Durg: return BuildingFootprint.Square(BuildingFootprint.DurgTiles);
                case BuildingKind.Karmashala: return BuildingFootprint.Square(BuildingFootprint.KarmashalaTiles);
                case BuildingKind.Monastery: return BuildingFootprint.Square(BuildingFootprint.MonasteryTiles);
                case BuildingKind.Dock: return new Vector2(dockSize.x, dockSize.z);
                case BuildingKind.LumberCamp:
                case BuildingKind.MiningCamp:
                case BuildingKind.Mill:
                    return BuildingFootprint.Square(BuildingFootprint.DropOffTiles);
                default: return BuildingFootprint.Square(BuildingFootprint.FarmTiles);
            }
        }

        // Same idea as CurrentFootprint above, but for the stationary-
        // resource overlap check specifically: Wall/Gate don't have a
        // BuildingFootprint.Square entry (CurrentFootprint's own switch
        // never reaches them, since IsClearForKind branches around it for
        // those two), so this uses their real wallSize/gateSize (X,Z)
        // rectangle instead - a resource pile should block them exactly
        // where their real footprint would sit, not the smaller
        // wallClearance circle used for building-to-building spacing.
        private Vector2 ResourceCheckFootprint(BuildingKind kind)
        {
            return kind switch
            {
                BuildingKind.Wall => new Vector2(wallSize.x, wallSize.z),
                BuildingKind.Gate => new Vector2(gateSize.x, gateSize.z),
                _ => CurrentFootprint(kind),
            };
        }

        // Wall mechanics audit (2026-09-29): same clearance-circle shape as
        // BarracksFactory.IsClear, but a same-faction Wall doesn't count as
        // blocking - it's absorbable (see AbsorbWalls). An enemy-owned
        // Wall (or any non-Wall building, any faction) still blocks
        // normally, so this can't be used to grief/clear an opponent's
        // fortifications by dropping a Gate on them.
        internal static bool IsClearForGate(Vector3 point, float clearance, FactionId faction)
        {
            foreach (Building building in Building.All)
            {
                if (building is Wall && building.TryGetComponent(out FactionMember member) && member.Faction == faction)
                {
                    continue;
                }

                if (Vector3.Distance(building.transform.position, point) < clearance)
                {
                    return false;
                }
            }

            return true;
        }

        // Every same-faction Wall whose real footprint (BuildingFootprintTag,
        // set by WallFactory via BuildingFootprint.Attach) genuinely
        // overlaps the Gate's own footprint at `point` - the set this
        // Gate placement is about to absorb. Rotation-agnostic AABB
        // overlap, same simplification BuildingFootprint.IsClear already
        // uses everywhere in this project.
        internal static System.Collections.Generic.List<Wall> FindAbsorbableWalls(Vector3 point, Vector3 gateSize, FactionId faction)
        {
            var result = new System.Collections.Generic.List<Wall>();
            float halfX = gateSize.x * 0.5f;
            float halfZ = gateSize.z * 0.5f;

            foreach (Building building in Building.All)
            {
                if (!(building is Wall wall) || !wall.TryGetComponent(out FactionMember member) || member.Faction != faction)
                {
                    continue;
                }

                Vector2 wallFootprint = wall.TryGetComponent(out BuildingFootprintTag tag)
                    ? tag.Size
                    : new Vector2(2.4f, 2.4f);
                float halfWidthSum = halfX + wallFootprint.x * 0.5f;
                float halfDepthSum = halfZ + wallFootprint.y * 0.5f;
                Vector3 wallPosition = wall.transform.position;

                if (Mathf.Abs(point.x - wallPosition.x) < halfWidthSum && Mathf.Abs(point.z - wallPosition.z) < halfDepthSum)
                {
                    result.Add(wall);
                }
            }

            return result;
        }

        // Deletes each given Wall and refunds its recorded BuildingCost in
        // full - a replace, not a cancel, so (unlike
        // ConstructionSite.CancelAndRefund) this applies the same
        // regardless of the wall's completion/damage state, matching
        // AoE2's own "deleting the targeted 1x1 wall entities and
        // refunding... them" rule for a Gate placed over an existing wall
        // line.
        internal static void AbsorbWalls(System.Collections.Generic.List<Wall> walls, FactionId faction)
        {
            if (walls.Count == 0)
            {
                return;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(faction);
            foreach (Wall wall in walls)
            {
                if (wall.TryGetComponent(out BuildingCost cost))
                {
                    cost.Refund(stockpile, 1f);
                }

                if (Application.isPlaying)
                {
                    Destroy(wall.gameObject);
                }
                else
                {
                    DestroyImmediate(wall.gameObject);
                }
            }
        }

        // Wall mechanics audit (2026-09-29): scans every other live
        // Wall/Gate for a diagonal-corner gap against the just-spawned
        // piece and plugs each one found - see WallCornerSeal/
        // WallCornerSealLink. Faction-agnostic (a pure geometry/pathing
        // rule, not an ownership one, same convention BarracksFactory.
        // IsClear already uses for Wall/Gate clearance).
        private const float WallCornerSealMaxGap = 1.5f;

        private void SealDiagonalGaps(GameObject spawned, Vector2 footprint)
        {
            if (spawned == null || !spawned.TryGetComponent(out Building newBuilding))
            {
                return;
            }

            Vector3 newCenter = spawned.transform.position;

            foreach (Building other in Building.All)
            {
                if (other == newBuilding || !(other is Wall || other is Gate))
                {
                    continue;
                }

                Vector2 otherFootprint = other.TryGetComponent(out BuildingFootprintTag tag)
                    ? tag.Size
                    : new Vector2(2.4f, 2.4f);

                if (!WallCornerSeal.TryComputeSeal(newCenter, footprint, other.transform.position, otherFootprint, WallCornerSealMaxGap, out Vector3 sealCenter, out Vector2 sealSize))
                {
                    continue;
                }

                GameObject seal = new GameObject("WallCornerSeal");
                seal.transform.SetParent(spawned.transform, worldPositionStays: true);
                seal.transform.position = sealCenter;

                var obstacle = seal.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = new Vector3(sealSize.x, 10f, sealSize.y);
                obstacle.carving = true;

                seal.AddComponent<WallCornerSealLink>().Configure(newBuilding, other);
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
            bool clear;
            if (kind == BuildingKind.Gate)
            {
                // Wall mechanics audit: a same-faction Wall under/near the
                // Gate doesn't block placement - it gets absorbed instead
                // (see AbsorbWalls, called from ExecuteBuild). An
                // enemy-faction Wall still blocks normally, same as any
                // other building.
                clear = IsClearForGate(point, gateClearance, NetworkMatch.LocalFaction);
            }
            else if (kind == BuildingKind.Wall)
            {
                clear = BarracksFactory.IsClear(point, wallClearance);
            }
            else
            {
                clear = BuildingFootprint.IsClear(point, CurrentFootprint(kind));
            }

            // AoE-style stationary-resource rule: blocked by a gold/stone/
            // farm-type resource under the footprint, but NOT by a tree
            // (Wood) - see BuildingFootprint.OverlapsBlockingResource/
            // ClearTreesInFootprint. Wall/Gate use their own smaller
            // rectangular footprint here (not the wallClearance circle
            // above, which exists only so chain segments can sit
            // edge-to-edge against each other) since a resource pile is a
            // real physical obstruction regardless of that clearance
            // exception.
            if (clear && BuildingFootprint.OverlapsBlockingResource(point, ResourceCheckFootprint(kind)))
            {
                clear = false;
            }

            if (kind != BuildingKind.Dock)
            {
                // The water bed has a collider now (it used to be a hole),
                // so a click on water hits ground - keep land buildings out.
                return clear && !WaterProximity.IsInsideWater(point);
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

        // Wall system Session A: TryGetGroundPoint above raycasts from the
        // camera through the mouse, which only makes sense for the
        // cursor's own point. A wall chain's other segments are at fixed
        // XZ positions computed from the drag line, so their ground height
        // needs a straight-down raycast from above that XZ column instead.
        private bool TryGetGroundHeightAt(Vector3 xzPoint, out Vector3 point)
        {
            Vector3 origin = new Vector3(xzPoint.x, 500f, xzPoint.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1000f))
            {
                point = hit.point;
                return true;
            }

            point = Vector3.zero;
            return false;
        }

    }
}
