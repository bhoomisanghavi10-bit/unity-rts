using UnityEngine;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Buildings
{
    // Worker-side half of construction: walk to an in-progress ConstructionSite
    // and count as an active builder for as long as it stays in range. A site
    // with zero active builders makes no progress (AoE-style: placing a
    // foundation doesn't build it, a worker actually has to be there).
    [RequireComponent(typeof(UnitMover))]
    public class Builder : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 2.5f;

        private UnitMover _mover;
        private ConstructionSite _site;
        private bool _building;
        private readonly StuckWatchdog _watchdog = new StuckWatchdog();
        private int _attempts;
        public WorkerFailure LastFailure { get; private set; }

        // For SelectedUnitPanel (UI) to show a status line. True only while
        // actually in range and contributing progress, not while walking over.
        public bool IsBuilding => _building;

        private UnitMover Mover => _mover != null ? _mover : (_mover = GetComponent<UnitMover>());

        public void BuildAt(ConstructionSite site)
        {
            CancelBuild();
            if (site == null)
            {
                return;
            }

            _site = site;
            _attempts = 0;
            LastFailure = WorkerFailure.None;
            MoveToSite();
        }

        // Walk to the nearest reachable point of the site's edge - a site's
        // centre can sit inside its own footprint/obstacle.
        private void MoveToSite()
        {
            _watchdog.Reset();
            Vector3 point = WorkerNav.ClosestPoint(_site, transform.position);
            if (Mover.TrySnap(point, 2.5f, out Vector3 snapped))
            {
                point = snapped;
            }

            Mover.MoveTo(point);
        }

        public void CancelBuild()
        {
            if (_building && _site != null)
            {
                _site.StopBuilding();
            }
            _building = false;
            _site = null;
        }

        private void Update()
        {
            if (_site == null)
            {
                // Site destroyed while we were counted as a builder: drop the
                // stale flag (the destroyed site can't be told to stop).
                _building = false;
                return;
            }

            if (_site.IsComplete)
            {
                CancelBuild();
                return;
            }

            Vector3 edge = WorkerNav.ClosestPoint(_site, transform.position);
            float distance = Vector3.Distance(transform.position, edge);
            bool inRange = distance <= interactionRange;

            if (inRange && !_building)
            {
                _site.BeginBuilding();
                _building = true;
            }
            else if (!inRange && _building)
            {
                _site.StopBuilding();
                _building = false;
            }

            if (!inRange && (Mover.IsPathInvalid || _watchdog.Tick(Time.deltaTime, distance)))
            {
                _attempts++;
                if (_attempts <= 1)
                {
                    MoveToSite();
                }
                else
                {
                    LastFailure = WorkerFailure.BuildSiteUnreachable;
                    WorkerDiagnostics.Report(WorkerFailure.BuildSiteUnreachable, name, $"cannot reach construction site {_site.name} at {_site.transform.position} from {transform.position} (edge distance {distance:0.0}, collider {_site.TryGetComponent(out Collider _)}, pathInvalid {Mover.IsPathInvalid})");
                    Mover.Stop();
                    CancelBuild();
                }
            }
        }

        private void OnDisable()
        {
            CancelBuild();
        }
    }
}
