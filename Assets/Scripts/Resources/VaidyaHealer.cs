using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.ResourceGathering
{
    // Wave 4 item 27: Vaidya's heal-target ability. Calls Attackable.Heal
    // directly (already exists, used today only by Repairable) rather than
    // needing a target-side component - so nothing has to be added to any
    // of the existing unit factories. Same "chase while out of range, act
    // while in range" shape as MeleeAttacker.Tick, just healing instead of
    // damaging.
    [RequireComponent(typeof(UnitMover))]
    public class VaidyaHealer : MonoBehaviour
    {
        [SerializeField] private float healRange = 3f;
        [SerializeField] private float healPerSecond = 12f;

        private UnitMover _mover;
        private Attackable _target;

        public bool IsHealing => _target != null;

        private UnitMover Mover => _mover != null ? _mover : (_mover = GetComponent<UnitMover>());

        public void HealAt(Attackable target)
        {
            _target = target;
            Mover.MoveTo(target.transform.position);
        }

        public void CancelHeal()
        {
            _target = null;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // Internal (not private) so EditMode tests can drive this directly
        // with an explicit deltaTime - same convention as
        // MeleeAttacker.Tick/ConstructionSite.EnsureInitialized (see
        // AssemblyInfo.cs's InternalsVisibleTo grant).
        internal void Tick(float deltaTime)
        {
            if (_target == null || _target.IsDead || _target.Health >= _target.MaxHealth)
            {
                _target = null;
                return;
            }

            float distance = Vector3.Distance(transform.position, _target.transform.position);
            if (distance > healRange)
            {
                Mover.MoveTo(_target.transform.position);
                return;
            }

            _target.Heal(healPerSecond * deltaTime);
        }
    }
}
