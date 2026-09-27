using UnityEngine;
using KingdomsOfBharat.Vfx;
using KingdomsOfBharat.Audio;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Buildings
{
    // Drives a building's build-time progress: visually grows the building
    // from ground level up to full height, and flips IsComplete once done.
    // Progress only advances while at least one Builder is actively working
    // the site (AoE-style — a placed foundation sits idle until a worker is
    // sent to build it; multiple workers build proportionally faster). A
    // building with no ConstructionSite (e.g. the milestone-4 Town Center)
    // should be treated as always complete.
    //
    // The squash-to-grow animation scales the VISUAL child only (the sole
    // child BuildingModelFactory.Spawn always parents under root - the
    // imported model or procedural fallback), never this component's own
    // root transform. Buildings' NavMeshObstacle/footprint (see
    // BuildingFootprint) lives on the root, so keeping root's scale fixed
    // at (1,1,1) throughout construction is what lets a foundation's
    // footprint block movement from the instant it's placed, not only once
    // a Builder is assigned and the visual actually starts growing.
    public class ConstructionSite : MonoBehaviour
    {
        [SerializeField] private float buildTime = 8f;

        private Transform _visual;
        private bool _initialized;
        private float _progress; // 0..1
        private Vector3 _finalScale;
        private float _baseY;
        private int _activeBuilders;
        private float _vfxTimer;
        // Farm's visual (see FarmVisual) is a flat ground-decal quad, not a
        // 3D box - its own state-driven texture swap already communicates
        // "under construction" (a dedicated Construction sprite), so this
        // squash-from-ground-then-grow animation doesn't apply: every
        // vertex on that quad sits at local y=0, so scaling its Y does
        // nothing visually, but the accompanying position math below still
        // treats localScale.y as if it were a real height, sinking the
        // whole decal underground for the entire build duration. Detected
        // once here (not hardcoded to FarmVisual by name) so any future
        // flat-sprite visual can opt out the same way.
        private bool _skipVisualAnimation;

        public bool IsComplete { get; private set; }
        public float Progress => _progress;

        // AoE II's diminishing-returns multi-builder formula, confirmed
        // 2026-08-29: Tn = T1 / (1 + 0.6*min(n-1,1) + 0.3*max(n-2,0)), so
        // this returns the SPEED multiplier (1/Tn's ratio), not Tn itself.
        // 1 worker: 1x. 2: 1.6x (first extra worker adds 60%). 3: 1.9x
        // (second extra worker only adds 30%). Each worker beyond the 2nd
        // adds a flat +0.3 - deliberately, strongly diminishing, so a
        // rushed 4-worker Town Center is ~2.2x speed, not 4x. Replaces the
        // previous flat-linear (* activeBuilders) multiplier uniformly for
        // every building, no per-building exception - a real balance
        // change, not a bug fix (see playtest_log.csv).
        public static float SpeedMultiplier(int activeBuilders)
        {
            if (activeBuilders <= 0)
            {
                return 0f;
            }

            return 1f + 0.6f * Mathf.Min(activeBuilders - 1, 1) + 0.3f * Mathf.Max(activeBuilders - 2, 0);
        }

        public void Configure(float duration)
        {
            buildTime = duration;
        }

        public void BeginBuilding()
        {
            _activeBuilders++;
        }

        public void StopBuilding()
        {
            _activeBuilders = Mathf.Max(0, _activeBuilders - 1);
        }

        // Save/load only (also handy for tests) - snaps straight to the
        // finished visual/state instead of ticking Update() toward it, for
        // a building whose save data says it was already complete. No VFX/
        // SFX burst, since nothing was actually just built.
        public void CompleteImmediately()
        {
            EnsureInitialized();
            _progress = 1f;
            IsComplete = true;
            ApplyHeight(_finalScale.y);
        }

        // Save/load only: snaps directly to an arbitrary saved progress
        // fraction (unlike CompleteImmediately, which only ever snaps to
        // 1.0) - restores a building that was mid-construction when the
        // match was saved, without replaying Update() ticks to get there.
        // No active builder is implied by this alone - a restored
        // foundation sits exactly as idle as this project's existing
        // "placed but nobody's building it yet" state, matching how a
        // completed restore always did.
        internal void RestoreProgress(float progress)
        {
            EnsureInitialized();
            _progress = Mathf.Clamp01(progress);
            IsComplete = _progress >= 1f;
            ApplyHeight(Mathf.Lerp(0.01f, _finalScale.y, _progress));
        }

        // Lazily resolved instead of purely in Awake() - same "AddComponent
        // ordering hazard" this codebase already works around elsewhere
        // (see Barracks/Dock's lazy Site/FactionMember getters): a caller
        // can reach CompleteImmediately() (e.g. save/load restoring an
        // already-complete building) in the same synchronous sequence that
        // created this component, before Awake is guaranteed to have run.
        // Idempotent and does the one-time "squash to foundation height"
        // itself, so Awake() calling this after some other entry point
        // already ran it doesn't re-squash an already-completed building.
        // Internal (not private) so EditMode tests can call it directly
        // instead of depending on Unity's Editor Awake-scheduling timing,
        // same convention as BuildingPlacer.BuildingKind - see
        // Assets/Scripts/AssemblyInfo.cs for the InternalsVisibleTo grant.
        internal void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;

            _visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
            _skipVisualAnimation = _visual.GetComponent<FarmVisual>() != null;
            _finalScale = _visual.localScale;
            _baseY = _visual.position.y - _finalScale.y * 0.5f;
            ApplyHeight(0.01f);
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            EnsureInitialized();

            if (IsComplete || _activeBuilders <= 0)
            {
                return;
            }

            _progress += (Time.deltaTime / buildTime) * SpeedMultiplier(_activeBuilders);
            _progress = Mathf.Clamp01(_progress);
            ApplyHeight(Mathf.Lerp(0.01f, _finalScale.y, _progress));

            _vfxTimer += Time.deltaTime;
            if (_vfxTimer >= 0.5f)
            {
                _vfxTimer = 0f;
                VfxFactory.SpawnBurst(transform.position, new Color(0.6f, 0.55f, 0.5f), size: 0.15f, count: 5, speed: 1f, lifetime: 0.4f);
            }

            if (_progress >= 1f)
            {
                IsComplete = true;
                SfxPlayer.PlayBuildComplete(transform.position);
            }
        }

        // AoE-style cancel rule: as long as this foundation isn't complete,
        // the owner can cancel it for a refund of BuildingCost's recorded
        // spend, scaled by the fraction of health still remaining
        // (Attackable.Health/MaxHealth) - a pristine foundation refunds in
        // full, a damaged one only partially, matching "resources can be
        // fully refunded if the player cancels the building at any point
        // during its construction, as long as it hasn't taken damage... if
        // a building does take damage before it is complete and then is
        // canceled, only a portion of the resources are returned depending
        // on how much damage the building took." A foundation whose HP
        // already reached zero is destroyed through Attackable's own
        // TakeDamage path instead (a plain Destroy(gameObject), no refund
        // at all) - this method is simply unreachable for that case, since
        // the GameObject is already gone by the time anything could call it.
        public void CancelAndRefund()
        {
            if (IsComplete)
            {
                return;
            }

            float fraction = 1f;
            if (TryGetComponent(out Attackable attackable) && attackable.MaxHealth > 0f)
            {
                fraction = Mathf.Clamp01(attackable.Health / attackable.MaxHealth);
            }

            if (fraction > 0f && TryGetComponent(out BuildingCost cost) && TryGetComponent(out FactionMember member))
            {
                cost.Refund(ResourceStockpile.For(member.Faction), fraction);
            }

            // Same Application.isPlaying-gated Destroy/DestroyImmediate
            // split as ResourceNode.Harvest - Destroy(gameObject) alone is
            // a real Editor error outside Play mode (and, at runtime, is
            // deferred to end-of-frame rather than immediate).
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void ApplyHeight(float height)
        {
            if (_skipVisualAnimation)
            {
                return;
            }

            _visual.localScale = new Vector3(_finalScale.x, height, _finalScale.z);
            Vector3 position = _visual.position;
            position.y = _baseY + height * 0.5f;
            _visual.position = position;
        }
    }
}
