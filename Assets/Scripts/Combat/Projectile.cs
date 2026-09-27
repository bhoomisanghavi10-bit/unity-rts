using System;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Combat
{
    // A short-lived, code-built arrow that visually flies from a ranged
    // attacker's release point to wherever the target stood at release
    // time, arriving after `travelTime` seconds - see MeleeAttacker's
    // opt-in projectile mode (SetProjectile). Prefab-free/self-destructing,
    // same "everything built in code, nothing to clean up later"
    // convention as VfxFactory.SpawnBurst. No dedicated arrow/fletching
    // model is sourced yet (flagging directly, per this project's own
    // convention for real art gaps) - this is a plain procedural shaft,
    // the same "generic procedural shape until a real pack lands"
    // fallback already used for buildings with no sourced model.
    //
    // Deliberately decouples the visual release moment (when this is
    // spawned) from the actual gameplay hit: damage is applied via
    // onArrive, invoked only once the arrow's simulated flight actually
    // reaches its destination, not when it's fired. This is what lets
    // impact VFX/SFX (both already fired from inside Attackable.TakeDamage)
    // read as synchronized with the arrow's arrival instead of firing the
    // instant the archer's swing starts.
    public class Projectile : MonoBehaviour
    {
        private Vector3 _origin;
        private Vector3 _destination;
        private float _travelTime;
        private float _elapsed;
        private Action _onArrive;
        // Set the instant arrival resolves, so a stray extra Tick call
        // (e.g. Destroy's own one-frame lag in a real Update loop, or a
        // caller ticking this directly more than once) is a no-op instead
        // of re-invoking Destroy(gameObject) every time it's called - in
        // Play mode a destroyed GameObject simply stops receiving Update()
        // calls, so this never bites there, but nothing enforced that
        // invariant for a caller driving Tick() directly.
        private bool _resolved;

        // A projectile fired at point-blank range (travelTime near 0) still
        // needs at least one Tick to resolve - without a floor, dividing
        // elapsed/travelTime by a near-zero value could either finish
        // instantly (fine) or, if travelTime is exactly 0, divide by zero.
        // Floors to a small-but-nonzero value instead of exactly 0 so a
        // point-blank shot still visibly leaves the bow for one frame
        // rather than teleporting straight to the impact VFX.
        private const float MinTravelTime = 0.02f;

        public static Projectile Fire(Vector3 origin, Vector3 destination, float travelTime, Action onArrive)
        {
            var go = new GameObject("Arrow");
            go.transform.position = origin;

            Vector3 direction = destination - origin;
            go.transform.rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized)
                : Quaternion.identity;

            BuildVisual(go.transform);

            var projectile = go.AddComponent<Projectile>();
            projectile._origin = origin;
            projectile._destination = destination;
            projectile._travelTime = Mathf.Max(travelTime, MinTravelTime);
            projectile._onArrive = onArrive;
            return projectile;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // Internal so an EditMode test can drive arrival deterministically
        // with an explicit deltaTime, same convention as MeleeAttacker's
        // own internal Tick(deltaTime) - Update() itself depends on
        // Time.deltaTime, which EditMode tests don't naturally advance.
        // Advances by elapsed real seconds, not a fixed step count, so
        // travel time stays correct regardless of how many frames render
        // while it's in flight (satisfies the frame-rate-independence
        // requirement the same way every other per-frame system in this
        // project already does, e.g. MeleeAttacker's own cooldown).
        internal void Tick(float deltaTime)
        {
            if (_resolved)
            {
                return;
            }

            _elapsed += deltaTime;
            float progress = Mathf.Clamp01(_elapsed / _travelTime);
            transform.position = Vector3.Lerp(_origin, _destination, progress);

            if (progress < 1f)
            {
                return;
            }

            _resolved = true;
            Action callback = _onArrive;
            _onArrive = null;
            callback?.Invoke();
            Destroy(gameObject);
        }

        private static void BuildVisual(Transform parent)
        {
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            Destroy(shaft.GetComponent<Collider>());
            shaft.transform.SetParent(parent, false);
            // The default cylinder's long axis is local Y (height 2,
            // radius 0.5) - rotate it onto local Z so it points along the
            // parent's own forward (the flight direction, set in Fire),
            // then push it forward half its own length so the shaft's
            // nock end sits at the parent's origin and its tip leads.
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.transform.localScale = new Vector3(0.02f, 0.35f, 0.02f);
            shaft.transform.localPosition = Vector3.forward * 0.35f;

            Renderer renderer = shaft.GetComponent<Renderer>();
            renderer.sharedMaterial = GameplayMaterial.CreateOpaque(new Color(0.36f, 0.25f, 0.14f));
        }
    }
}
