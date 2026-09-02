using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Wildlife;
using KingdomsOfBharat.Camera;
using KingdomsOfBharat.Audio;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.Multiplayer.Wire;

namespace KingdomsOfBharat.Selection
{
    // Owns all selection/command input: left-click selects a single unit,
    // left-click-drag box-selects multiple, right-click issues a move order
    // (or a gather/build/attack-move order, depending what the click landed
    // on) to whatever is currently selected. Disarmed entirely while
    // BuildingPlacer is mid-placement so a single click doesn't double up as
    // both a placement action and a unit command.
    public class SelectionManager : MonoBehaviour
    {
        [SerializeField] private float dragThreshold = 6f;
        [SerializeField] private KeyCode cycleStanceKey = KeyCode.V;
        // Not F - PlaceFarm already owns that key (see SettingsMenu's
        // rebindable-action list).
        [SerializeField] private KeyCode cycleFormationKey = KeyCode.R;
        [SerializeField] private float formationSpacing = 1.5f;
        // C for "Composed formation" - not F/T/B/H/L/K/O/M/N/G/V/R, all
        // already taken (see SettingsMenu's rebindable-action list).
        [SerializeField] private KeyCode toggleComposedFormationKey = KeyCode.C;
        // Loaded by path rather than a direct Inspector reference, so this
        // works the same way in any scene without a manual per-scene
        // assignment - matches DataRegistry's own Resources.Load
        // convention for CSV-generated assets, even though this one isn't
        // CSV-generated.
        private const string DefaultComposedFormationPath = "Formations/MeleeFrontRangedBack";

        // Optional: when assigned, a plain-ground move order is composed
        // via FormationController (front/back rows by UnitCategory) using
        // THIS asset's own FormationType/spacing/unitsPerRow instead of the
        // hotkey-cycled _currentFormation/formationSpacing above - a
        // FormationDefinition represents a specific composed formation
        // (e.g. "melee front, ranged back"), not just a shape, so it owns
        // its own shape choice rather than deferring to the ambient one.
        // Left null by default - existing move-order behavior (GroupFormation.
        // GetOffset with the cycled formation/spacing) is completely
        // unchanged unless a designer/mission explicitly assigns one, or
        // the player toggles it on via toggleComposedFormationKey below.
        [SerializeField] private FormationDefinition composedFormation;

        // Phase 6 gap-close: which formation a plain-ground move order
        // spreads the current selection into - a per-player mode (like
        // stance's per-unit cycling below, but this lives on the
        // selection/command layer instead of each Unit) rather than
        // per-unit persistent state, since it's about how *this* move
        // order lays units out, not a standing behavior each unit
        // remembers independently.
        // Fully-qualified, not just `using KingdomsOfBharat.Units;` - the
        // user's own Assets/Scripts/Data/Scripts/FormationDefinition.cs
        // declares an unrelated, unnamespaced global `FormationType` too;
        // C# resolves enclosing-namespace/global names before `using`
        // imports, so an unqualified `FormationType` here would silently
        // bind to the wrong one.
        private Units.FormationType _currentFormation = Units.FormationType.Grid;

        // Self-added in Awake, same "eager is safe here" reasoning as
        // Barracks' own RallyPoint - this component has no dependency on
        // any sibling that might not exist yet.
        private FormationController _formationController;

        private readonly List<Unit> _selected = new List<Unit>();
        private Building _selectedBuilding;
        private UnityEngine.Camera _camera;
        private Vector2 _dragStart;
        private bool _dragging;

        // Control groups (Ctrl+1-9 to assign the current selection, 1-9 to
        // reselect it) - 9 fixed slots rather than a Dictionary, since AoE's
        // own convention is exactly the digit row, no more.
        private readonly List<Unit>[] _controlGroups = new List<Unit>[9];

        // For SelectedUnitPanel / BuildMenu (UI) to read current selection.
        public IReadOnlyList<Unit> Selected => _selected;

        // For a HUD indicator to show which formation is currently active.
        public Units.FormationType CurrentFormation => _currentFormation;

        // For FormationIndicator to show whether a composed formation
        // (front/back rows by UnitCategory) is currently active on top of
        // the plain Grid/Line/Box spread above, and which one.
        public bool IsComposedFormationActive => composedFormation != null;
        public string ComposedFormationName => composedFormation != null ? composedFormation.displayName : null;

        // Buildings are single-select only and mutually exclusive with unit
        // selection (AoE-style) - selecting one clears the other. Null when
        // nothing/a unit is selected instead.
        public Building SelectedBuilding => _selectedBuilding;

        private void Awake()
        {
            cycleStanceKey = GameSettings.GetKey("CycleStance", cycleStanceKey);
            cycleFormationKey = GameSettings.GetKey("CycleFormation", cycleFormationKey);
            toggleComposedFormationKey = GameSettings.GetKey("ToggleComposedFormation", toggleComposedFormationKey);

            _camera = UnityEngine.Camera.main;
            _formationController = gameObject.AddComponent<FormationController>();

            for (int i = 0; i < _controlGroups.Length; i++)
            {
                _controlGroups[i] = new List<Unit>();
            }
        }

        private void Update()
        {
            // A selected unit can now die mid-selection (workers are
            // killable since milestone 11 - wild boars, enemy soldiers).
            // Prune before anything this frame reads _selected: HandleMoveInput
            // below, and SelectedUnitPanel/BuildMenu's OnGUI, which would
            // otherwise throw MissingReferenceException touching a
            // destroyed Unit every frame. The == null check here is
            // deliberate - it invokes UnityEngine.Object's overridden
            // equality, which is what actually detects "destroyed but not
            // yet real C# null" Unity objects; a plain reference/is-null
            // check would not catch this.
            _selected.RemoveAll(unit => unit == null);

            // Same destroyed-but-not-yet-null concern as above, now that
            // buildings are attackable and can die mid-selection too.
            if (!_selectedBuilding)
            {
                _selectedBuilding = null;
            }

            if (BuildingPlacer.IsPlacing || MinimapController.IsPointerOverMinimap)
            {
                return;
            }

            HandleSelectionInput();
            HandleMoveInput();
            HandleRallyInput();
            HandleStanceHotkey();
            HandleFormationHotkey();
            HandleComposedFormationHotkey();
            HandleControlGroupInput();
        }

        // Cycles the active formation (Grid -> Line -> Box -> Grid) for
        // future plain-ground move orders - doesn't touch units already
        // moving, only how the next order spreads them. No selection
        // requirement (unlike stance cycling) since this is a standing
        // mode for whatever gets selected next, not an action applied to
        // the current selection.
        private void HandleFormationHotkey()
        {
            if (!Input.GetKeyDown(cycleFormationKey))
            {
                return;
            }

            _currentFormation = (Units.FormationType)(((int)_currentFormation + 1) % 3);
            SfxPlayer.PlayMove();
        }

        // Simplest version of the "give the player an actual way to reach
        // composedFormation" gap - a single on/off toggle against the one
        // FormationDefinition asset that exists so far
        // (Formations/MeleeFrontRangedBack), not a picker across several.
        // Loaded via Resources rather than an Inspector reference so this
        // works in any scene without per-scene wiring, same reasoning as
        // DataRegistry's own CSV-generated-asset loading. No selection
        // requirement, same as HandleFormationHotkey above - this is a
        // standing mode for whatever gets selected next, not an action
        // applied to the current selection.
        private void HandleComposedFormationHotkey()
        {
            if (!Input.GetKeyDown(toggleComposedFormationKey))
            {
                return;
            }

            if (composedFormation != null)
            {
                composedFormation = null;
            }
            else
            {
                composedFormation = Resources.Load<FormationDefinition>(DefaultComposedFormationPath);
                if (composedFormation == null)
                {
                    Debug.LogWarning($"SelectionManager: no FormationDefinition found at Resources/{DefaultComposedFormationPath} - composed formation toggle has nothing to turn on.");
                    return;
                }
            }

            SfxPlayer.PlayMove();
        }

        // Ctrl+[1-9] assigns the current unit selection to that group,
        // replacing whatever was in it before; plain [1-9] reselects it.
        // Buildings never join a control group (AoE-style - groups are for
        // maneuvering an army, not a base), matching how SelectedBuilding
        // is already mutually exclusive with unit selection elsewhere here.
        private void HandleControlGroupInput()
        {
            bool assigning = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            for (int i = 0; i < _controlGroups.Length; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    continue;
                }

                if (assigning)
                {
                    AssignControlGroup(i);
                }
                else
                {
                    SelectControlGroup(i);
                }
            }
        }

        private void AssignControlGroup(int index)
        {
            if (_selected.Count == 0)
            {
                return;
            }

            _controlGroups[index].Clear();
            _controlGroups[index].AddRange(_selected);
        }

        private void SelectControlGroup(int index)
        {
            // Same destroyed-but-not-yet-null pruning as _selected/
            // _selectedBuilding above - a grouped unit can die long after
            // the group was assigned, well before it's ever reselected.
            _controlGroups[index].RemoveAll(unit => unit == null);
            if (_controlGroups[index].Count == 0)
            {
                return;
            }

            ClearSelection();
            foreach (Unit unit in _controlGroups[index])
            {
                Select(unit);
            }
        }

        // Right-click while a production building (one with a RallyPoint -
        // see TownCenter/Barracks) is selected sets its rally point instead
        // of issuing a unit-move order; unit and building selection are
        // already mutually exclusive (see SelectBuilding), so this never
        // fires on the same click as HandleMoveInput. Mirrors
        // HandleMoveInput's own hit-priority shape (resource node -> hostile
        // target -> plain point) but without the friendly-build-assist/
        // staff-farm cases, which don't apply to a rally point.
        private void HandleRallyInput()
        {
            if (_selectedBuilding == null
                || !_selectedBuilding.TryGetComponent(out RallyPoint rally)
                || !Input.GetMouseButtonDown(1))
            {
                return;
            }

            // RaycastAll + skip the selected building's own collider(s),
            // not a plain Raycast - a naive single Raycast can hit the
            // selected building's own (often large) collider before
            // reaching the ground/target the player actually aimed at
            // (e.g. a tall TownCenter model), placing the rally flag on
            // the building's own surface instead. Every other hit type
            // below (node/attackable/ground) is unaffected - this only
            // skips hits that belong to _selectedBuilding itself.
            if (!TryRaycastSkipping(_selectedBuilding.gameObject, out RaycastHit hit))
            {
                return;
            }

            if (hit.collider.TryGetComponent(out ResourceNode node))
            {
                rally.SetGatherTarget(node);
                return;
            }

            if (hit.collider.TryGetComponent(out Attackable attackable)
                && !attackable.IsDead
                && !IsFriendlyToPlayer(attackable))
            {
                rally.SetAttackTarget(attackable);
                return;
            }

            rally.SetPoint(hit.point);
        }

        // Cycles stance (Aggressive -> Defensive -> StandGround) for every
        // currently selected unit that has a StanceController - Workers
        // don't get one (see StanceController's own comment), so this is a
        // silent no-op for an all-Worker selection.
        private void HandleStanceHotkey()
        {
            if (_selected.Count == 0 || !Input.GetKeyDown(cycleStanceKey))
            {
                return;
            }

            foreach (Unit unit in _selected)
            {
                if (unit.TryGetComponent(out StanceController stance))
                {
                    stance.CycleStance();
                }
            }
        }

        private void HandleSelectionInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _dragStart = Input.mousePosition;
                _dragging = true;
            }

            if (!Input.GetMouseButtonUp(0) || !_dragging)
            {
                return;
            }

            _dragging = false;
            Vector2 dragEnd = Input.mousePosition;

            if (Vector2.Distance(_dragStart, dragEnd) < dragThreshold)
            {
                SelectSingle(dragEnd);
            }
            else
            {
                SelectInBox(_dragStart, dragEnd);
            }
        }

        private void HandleMoveInput()
        {
            if (_selected.Count == 0 || !Input.GetMouseButtonDown(1))
            {
                return;
            }

            // Same RaycastAll + skip-self fix as HandleRallyInput - here
            // "self" is every currently selected unit's own collider, so a
            // click near/behind one of them can't resolve to hitting that
            // same unit instead of whatever's beyond it.
            if (!TryRaycastSkipping(_selected, out RaycastHit hit))
            {
                return;
            }

            // Once per click, not once per unit in the loop below - every
            // branch there (move/gather/build/attack/staff) is some kind
            // of "order issued" acknowledgment, same sound regardless of
            // which.
            SfxPlayer.PlayMove();

            bool hitNode = hit.collider.TryGetComponent(out ResourceNode node);
            // Faction-gated, not just "unfinished"/"finished": an enemy's
            // incomplete building shouldn't offer the "help build" action,
            // and an enemy's finished Farm shouldn't offer "staff it" -
            // both need to fall through to the attack branch below instead
            // (see hitAttackable), now that buildings carry Attackable.
            ConstructionSite site = null;
            bool hitSite = !hitNode
                && hit.collider.TryGetComponent(out site)
                && !site.IsComplete
                && IsFriendlyToPlayer(site);
            Farm farm = null;
            bool hitFarm = !hitNode && !hitSite
                && hit.collider.TryGetComponent(out farm)
                && farm.IsComplete
                && IsFriendlyToPlayer(farm);
            Livestock livestock = null;
            bool hitLivestock = !hitNode && !hitSite && !hitFarm
                && hit.collider.TryGetComponent(out livestock);
            // General garrisoning system (2026-09-01): right-clicking an
            // owned building with a GarrisonPoint (TownCenter/Tower, or
            // Wall for the Maratha Durg Garrison unique unit specifically)
            // with an eligible unit selected garrisons it - same
            // "friendly-only, falls through to attack otherwise" gating as
            // hitFarm/hitLivestock above.
            GarrisonPoint garrisonPoint = null;
            bool hitGarrison = !hitNode && !hitSite && !hitFarm && !hitLivestock
                && hit.collider.TryGetComponent(out garrisonPoint)
                && IsFriendlyToPlayer(garrisonPoint);
            // Repair system (worker mechanics audit, 2026-08-29): right-
            // clicking a friendly damaged building/ship/siege unit with a
            // worker selected repairs it - same "friendly-only, falls
            // through to attack otherwise" gating as hitFarm/hitLivestock/
            // hitGarrison above. IsRepairable already excludes full-health/
            // dead/still-under-construction targets.
            Repairable repairable = null;
            bool hitRepairable = !hitNode && !hitSite && !hitFarm && !hitLivestock && !hitGarrison
                && hit.collider.TryGetComponent(out repairable)
                && repairable.IsRepairable
                && IsFriendlyToPlayer(repairable);
            Attackable attackable = null;
            bool hitAttackable = !hitNode && !hitSite && !hitFarm && !hitLivestock && !hitGarrison && !hitRepairable
                && hit.collider.TryGetComponent(out attackable)
                && !attackable.IsDead;

            // Only the plain-move (else) branch below uses this - a group
            // ordered onto open ground spreads into the active formation
            // (see GroupFormation/_currentFormation) instead of every unit
            // pathing to the exact same point, but gather/build/attack/
            // staff targets are a single specific thing every selected
            // unit needs to reach, not open ground to spread across.
            int formationIndex = 0;

            // Centroid of the current selection, computed once - Line's
            // rank orientation is "which way is the group actually
            // traveling," not per-unit, so this has to be computed before
            // the loop rather than from each unit's own position.
            Vector3 selectionCentroid = Vector3.zero;
            foreach (Unit selectedUnit in _selected)
            {
                selectionCentroid += selectedUnit.transform.position;
            }
            selectionCentroid /= _selected.Count;
            Vector3 formationMoveDirection = hit.point - selectionCentroid;

            // Computed once for the whole selection, same reasoning as
            // selectionCentroid above - front/back-row composition needs
            // to see the whole group at once (who's melee vs ranged), not
            // one unit at a time. Null when no FormationDefinition is
            // assigned, so the loop below falls back to the original
            // per-index GroupFormation.GetOffset path untouched.
            Dictionary<GameObject, Vector3> composedOffsets = null;
            if (composedFormation != null)
            {
                _formationController.Formation = composedFormation;
                composedOffsets = _formationController.ComputeOffsets(_selected.ConvertAll(u => u.gameObject), formationMoveDirection);
            }

            foreach (Unit unit in _selected)
            {
                unit.TryGetComponent(out Gatherer gatherer);
                unit.TryGetComponent(out Builder builder);
                unit.TryGetComponent(out MeleeAttacker attacker);
                unit.TryGetComponent(out FarmWorker farmWorker);
                unit.TryGetComponent(out LivestockWorker livestockWorker);
                unit.TryGetComponent(out GarrisonSeeker garrisonSeeker);
                unit.TryGetComponent(out Repairer repairer);
                // Item 49: a boat has none of the land components above -
                // Gatherer/Builder/MeleeAttacker/FarmWorker/LivestockWorker
                // are all null for it - so it needs its own equivalents
                // wired into the same branches, or right-clicking with a
                // boat selected would silently do nothing at all.
                unit.TryGetComponent(out BoatGatherer boatGatherer);
                unit.TryGetComponent(out BoatAttacker boatAttacker);

                if (hitNode)
                {
                    builder?.CancelBuild();
                    attacker?.CancelAttack();
                    farmWorker?.CancelWork();
                    livestockWorker?.CancelWork();
                    repairer?.CancelRepair();
                    gatherer?.GatherFrom(node);
                    boatAttacker?.CancelAttack();
                    boatGatherer?.GatherFrom(node);
                }
                else if (hitSite && IsSameFaction(unit, site))
                {
                    gatherer?.CancelGather();
                    attacker?.CancelAttack();
                    farmWorker?.CancelWork();
                    livestockWorker?.CancelWork();
                    repairer?.CancelRepair();
                    builder?.BuildAt(site);
                }
                else if (hitFarm && farmWorker != null && IsSameFaction(unit, farm))
                {
                    gatherer?.CancelGather();
                    builder?.CancelBuild();
                    attacker?.CancelAttack();
                    livestockWorker?.CancelWork();
                    repairer?.CancelRepair();
                    farmWorker.StaffAt(farm);
                }
                else if (hitLivestock && livestockWorker != null)
                {
                    gatherer?.CancelGather();
                    builder?.CancelBuild();
                    attacker?.CancelAttack();
                    farmWorker?.CancelWork();
                    repairer?.CancelRepair();
                    livestockWorker.StaffAt(livestock);
                }
                else if (hitGarrison && garrisonSeeker != null && IsSameFaction(unit, garrisonPoint))
                {
                    gatherer?.CancelGather();
                    builder?.CancelBuild();
                    attacker?.CancelAttack();
                    farmWorker?.CancelWork();
                    livestockWorker?.CancelWork();
                    repairer?.CancelRepair();
                    garrisonSeeker.GarrisonAt(garrisonPoint);
                }
                else if (hitRepairable && repairer != null && IsSameFaction(unit, repairable))
                {
                    gatherer?.CancelGather();
                    builder?.CancelBuild();
                    attacker?.CancelAttack();
                    farmWorker?.CancelWork();
                    livestockWorker?.CancelWork();
                    repairer.RepairAt(repairable);
                }
                else if (hitAttackable && attacker != null && IsHostileTarget(unit, attackable))
                {
                    gatherer?.CancelGather();
                    builder?.CancelBuild();
                    farmWorker?.CancelWork();
                    livestockWorker?.CancelWork();
                    repairer?.CancelRepair();
                    FactionId attackFaction = unit.TryGetComponent(out FactionMember attackUnitFaction)
                        ? attackUnitFaction.Faction
                        : NetworkMatch.LocalFaction;
                    int attackTick = CommandBus.Enqueue(new AttackCommand(attackFaction, attacker, attackable, attacker.AttackMove));
                    SendNetworkCommand(CommandSerializer.ForAttack(attackTick, attackFaction, unit, attackable));
                }
                else if (hitAttackable && boatAttacker != null && IsHostileTarget(unit, attackable))
                {
                    boatGatherer?.CancelGather();
                    FactionId attackFaction = unit.TryGetComponent(out FactionMember attackUnitFaction)
                        ? attackUnitFaction.Faction
                        : NetworkMatch.LocalFaction;
                    int boatAttackTick = CommandBus.Enqueue(new AttackCommand(attackFaction, boatAttacker, attackable, boatAttacker.AttackMove));
                    SendNetworkCommand(CommandSerializer.ForAttack(boatAttackTick, attackFaction, unit, attackable));
                }
                else
                {
                    gatherer?.CancelGather();
                    builder?.CancelBuild();
                    attacker?.CancelAttack();
                    farmWorker?.CancelWork();
                    livestockWorker?.CancelWork();
                    repairer?.CancelRepair();
                    boatGatherer?.CancelGather();
                    boatAttacker?.CancelAttack();
                    if (unit.TryGetComponent(out UnitMover mover))
                    {
                        Vector3 offset = composedOffsets != null && composedOffsets.TryGetValue(unit.gameObject, out Vector3 composedOffset)
                            ? composedOffset
                            : GroupFormation.GetOffset(_currentFormation, formationIndex, _selected.Count, formationSpacing, formationMoveDirection);
                        FactionId faction = unit.TryGetComponent(out FactionMember unitFaction)
                            ? unitFaction.Faction
                            : NetworkMatch.LocalFaction;
                        Vector3 destination = hit.point + offset;
                        int moveTick = CommandBus.Enqueue(new MoveCommand(faction, mover, destination));
                        SendNetworkCommand(CommandSerializer.ForMove(moveTick, faction, unit, destination));
                        formationIndex++;
                    }
                    else if (unit.TryGetComponent(out WaterMover waterMover))
                    {
                        Vector3 offset = composedOffsets != null && composedOffsets.TryGetValue(unit.gameObject, out Vector3 composedOffset)
                            ? composedOffset
                            : GroupFormation.GetOffset(_currentFormation, formationIndex, _selected.Count, formationSpacing, formationMoveDirection);
                        waterMover.MoveTo(hit.point + offset);
                        formationIndex++;
                    }
                }
            }
        }

        // Shared by HandleRallyInput (selfObject = the selected building
        // itself) and HandleMoveInput (selfUnits = the current selection) -
        // RaycastAll rather than a single Raycast so a hit on the issuing
        // entity's own collider can be skipped in favor of whatever's
        // beyond it (ground, a resource node, an attack target), instead
        // of that nearer self-hit silently winning.
        private bool TryRaycastSkipping(GameObject selfObject, out RaycastHit hit)
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit candidate in hits)
            {
                if (candidate.collider.gameObject != selfObject)
                {
                    hit = candidate;
                    return true;
                }
            }

            hit = default;
            return false;
        }

        private bool TryRaycastSkipping(IReadOnlyList<Unit> selfUnits, out RaycastHit hit)
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit candidate in hits)
            {
                bool isSelf = false;
                for (int i = 0; i < selfUnits.Count; i++)
                {
                    if (selfUnits[i] != null && candidate.collider.gameObject == selfUnits[i].gameObject)
                    {
                        isSelf = true;
                        break;
                    }
                }

                if (!isSelf)
                {
                    hit = candidate;
                    return true;
                }
            }

            hit = default;
            return false;
        }

        private void SelectSingle(Vector2 screenPos)
        {
            ClearSelection();

            // RaycastAll rather than a single Raycast: a building's collider
            // (bigger, and often nearer the camera at typical RTS angles)
            // would otherwise eclipse a smaller unit collider standing right
            // next to it, making units near buildings unselectable. Units
            // take priority over whatever else the ray also passes through.
            Ray ray = _camera.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.TryGetComponent(out Unit unit) && IsPlayerControllable(unit))
                {
                    Select(unit);
                    SfxPlayer.PlaySelect();
                    return;
                }
            }

            // Buildings select second (any faction, not just Player's own -
            // AoE lets you click an enemy building to see its HP, just not
            // command it), so a unit standing on/near one is still picked
            // first by the loop above.
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.TryGetComponent(out Building building))
                {
                    SelectBuilding(building);
                    SfxPlayer.PlaySelect();
                    return;
                }
            }
        }

        private void SelectInBox(Vector2 start, Vector2 end)
        {
            ClearSelection();

            Rect box = Rect.MinMaxRect(
                Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y),
                Mathf.Max(start.x, end.x), Mathf.Max(start.y, end.y));

            foreach (Unit unit in Unit.All)
            {
                if (!IsPlayerControllable(unit))
                {
                    continue;
                }

                Vector3 screenPoint = _camera.WorldToScreenPoint(unit.transform.position);
                if (screenPoint.z > 0f && box.Contains(screenPoint))
                {
                    Select(unit);
                }
            }

            // Once for the whole box, not once per unit caught inside it -
            // Select() above is called in a loop, but the sound is one
            // "selection made" acknowledgment, not one per unit.
            if (_selected.Count > 0)
            {
                SfxPlayer.PlaySelect();
            }
        }

        // Phase 5 LAN transport MVP: the remote peer needs this exact
        // order too, scheduled for the same tick CommandBus.Enqueue already
        // computed - see CommandSerializer.cs/LanTransport.cs. No-op in
        // single-player (NetworkMatch.IsActive stays false).
        private static void SendNetworkCommand(NetMessageEnvelope envelope)
        {
            if (NetworkMatch.IsActive)
            {
                NetworkMatch.Transport.Send(envelope);
            }
        }

        // No FactionMember present is treated as "not player-controllable"
        // here (unlike the fail-open combat/build checks below) - every
        // spawner tags its units, so absence would mean something's wrong
        // rather than "neutral," and defaulting to selectable would let the
        // player command units nothing actually spawned as theirs.
        private static bool IsPlayerControllable(Unit unit)
        {
            return unit.TryGetComponent(out FactionMember factionMember)
                && factionMember.Faction == NetworkMatch.LocalFaction;
        }

        // Neutral (no FactionMember) targets/sites are always valid - see
        // TargetDummy, which is deliberately untagged-as-Player so it stays
        // attackable regardless of the attacker's own faction. Item 48:
        // an allied faction's units/buildings are never a valid attack
        // target either, same as your own.
        private static bool IsHostileTarget(Unit source, Attackable target)
        {
            if (!target.TryGetComponent(out FactionMember targetFaction))
            {
                return true;
            }

            if (!source.TryGetComponent(out FactionMember sourceFaction))
            {
                return true;
            }

            return DiplomacyRegistry.IsHostile(sourceFaction.Faction, targetFaction.Faction);
        }

        // No FactionMember present is treated as friendly here (matches
        // IsSameFaction/IsHostileTarget's fail-open convention) - only
        // used to decide whether a click should be a build-assist/staff
        // action (friendly) or an attack (hostile), and every selected
        // unit is always Player's own (see IsPlayerControllable). Item 48:
        // a faction allied with Player reads as friendly too, not just
        // Player's own units.
        private static bool IsFriendlyToPlayer(Component target)
        {
            return !target.TryGetComponent(out FactionMember targetFaction)
                || targetFaction.Faction == NetworkMatch.LocalFaction
                || DiplomacyRegistry.AreAllied(NetworkMatch.LocalFaction, targetFaction.Faction);
        }

        private static bool IsSameFaction(Unit source, Component target)
        {
            if (!source.TryGetComponent(out FactionMember sourceFaction))
            {
                return true;
            }

            if (!target.TryGetComponent(out FactionMember targetFaction))
            {
                return true;
            }

            return sourceFaction.Faction == targetFaction.Faction;
        }

        private void Select(Unit unit)
        {
            _selected.Add(unit);
            if (unit.TryGetComponent(out SelectionIndicator indicator))
            {
                indicator.SetSelected(true);
            }
        }

        // Single-select, mutually exclusive with unit selection - ClearSelection
        // (called by both SelectSingle and SelectInBox before reselecting)
        // already drops any previously selected building.
        private void SelectBuilding(Building building)
        {
            _selectedBuilding = building;
            if (building.TryGetComponent(out SelectionIndicator indicator))
            {
                indicator.SetSelected(true);
            }
        }

        private void ClearSelection()
        {
            foreach (Unit unit in _selected)
            {
                if (unit != null && unit.TryGetComponent(out SelectionIndicator indicator))
                {
                    indicator.SetSelected(false);
                }
            }
            _selected.Clear();

            if (_selectedBuilding != null && _selectedBuilding.TryGetComponent(out SelectionIndicator buildingIndicator))
            {
                buildingIndicator.SetSelected(false);
            }
            _selectedBuilding = null;
        }

        // Simple immediate-mode box-select overlay. Screen-space input is
        // bottom-left origin; OnGUI is top-left origin, hence the Y flip here
        // only (SelectInBox above deliberately uses raw screen coordinates to
        // match Camera.WorldToScreenPoint).
        private void OnGUI()
        {
            if (!_dragging)
            {
                return;
            }

            Vector2 start = _dragStart;
            Vector2 end = Input.mousePosition;
            start.y = Screen.height - start.y;
            end.y = Screen.height - end.y;

            Rect rect = Rect.MinMaxRect(
                Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y),
                Mathf.Max(start.x, end.x), Mathf.Max(start.y, end.y));

            Color previous = GUI.color;
            GUI.color = new Color(0.3f, 0.8f, 1f, 0.25f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
