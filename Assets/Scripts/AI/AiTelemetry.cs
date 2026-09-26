using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.AI
{
    // Observes an AI faction from the outside (world state only, never the
    // controller's own bookkeeping) so its behaviour can be measured the
    // same way before and after a change: time to first worker / military
    // unit / attack, where workers actually are, and how long production
    // sat blocked or idle. Cheap (1 Hz) and side-effect free.
    public class AiTelemetry : MonoBehaviour
    {
        public static readonly List<AiTelemetry> All = new List<AiTelemetry>();

        private const float SampleInterval = 1f;
        private const float WorkerFoodCost = 50f;
        private const float SoldierFoodCost = 50f;
        private const float SoldierGoldCost = 20f;

        public FactionId Faction;
        private float _startTime;
        private float _timer;
        private bool _initialised;
        private int _initialWorkers;

        // -1 = has not happened yet (seconds since AI start otherwise).
        public float FirstWorkerProduced = -1f;
        public float FirstMilitaryUnit = -1f;
        public float FirstAttackOrder = -1f;
        public float PopulationBlockedSeconds;
        public float IdleTownCenterSeconds;
        public float IdleBarracksSeconds;
        public float ObservedSeconds;
        public int PeakWorkers;
        public int PeakMilitary;

        // Worker-seconds by activity: Food, Wood, Gold, Stone, Building, Idle, Farming.
        public readonly float[] WorkerSeconds = new float[7];

        // Latest explanation per decision area ("why is the AI not doing X"),
        // kept as one line each - never logged per tick.
        private readonly Dictionary<string, string> _notes = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> Notes => _notes;

        public void Note(string key, string message)
        {
            _notes[key] = $"{Time.time - _startTime:0}s {message}";
        }

        // First time (seconds) each milestone was observed, e.g.
        // "Farm placed", "Farm complete", "Farm staffed", "Barracks complete".
        private readonly Dictionary<string, float> _milestones = new Dictionary<string, float>();

        private void Milestone(string name, float now)
        {
            if (!_milestones.ContainsKey(name))
            {
                _milestones[name] = now;
            }
        }

        public static AiTelemetry For(FactionId faction)
        {
            foreach (AiTelemetry t in All)
            {
                if (t != null && t.Faction == faction)
                {
                    return t;
                }
            }

            return null;
        }

        public static AiTelemetry Attach(GameObject host, FactionId faction)
        {
            AiTelemetry existing = host.GetComponent<AiTelemetry>();
            if (existing != null)
            {
                return existing;
            }

            AiTelemetry t = host.AddComponent<AiTelemetry>();
            t.Faction = faction;
            return t;
        }

        private void OnEnable()
        {
            All.Add(this);
            _startTime = Time.time;
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        public void NoteAttackOrder()
        {
            if (FirstAttackOrder < 0f)
            {
                FirstAttackOrder = Time.time - _startTime;
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < SampleInterval)
            {
                return;
            }

            float dt = _timer;
            _timer = 0f;
            Sample(dt);
        }

        private void Sample(float dt)
        {
            float now = Time.time - _startTime;
            ObservedSeconds += dt;

            int workers = 0, military = 0;
            foreach (Unit unit in Unit.All)
            {
                if (unit == null || !unit.TryGetComponent(out FactionMember m) || m.Faction != Faction)
                {
                    continue;
                }

                if (unit.TryGetComponent(out FarmWorker fw) && fw.Farm != null)
                {
                    Milestone("Farm staffed", now);
                }

                if (unit.TryGetComponent(out Gatherer gatherer))
                {
                    workers++;
                    int bucket = 5;
                    if (unit.TryGetComponent(out FarmWorker farmer) && farmer.Farm != null)
                    {
                        bucket = 6;
                    }
                    else if (unit.TryGetComponent(out Builder builder) && builder.IsBuilding)
                    {
                        bucket = 4;
                    }
                    else if (gatherer.IsWorking && gatherer.CurrentResourceType.HasValue)
                    {
                        bucket = (int)gatherer.CurrentResourceType.Value; // Food, Wood, Gold, Stone = 0..3
                    }

                    WorkerSeconds[bucket] += dt;
                }
                else if (unit.TryGetComponent(out MeleeAttacker _) || unit.TryGetComponent(out BoatAttacker _))
                {
                    military++;
                }
            }

            if (!_initialised)
            {
                _initialised = true;
                _initialWorkers = workers;
            }

            PeakWorkers = Mathf.Max(PeakWorkers, workers);
            PeakMilitary = Mathf.Max(PeakMilitary, military);
            if (FirstWorkerProduced < 0f && workers > _initialWorkers)
            {
                FirstWorkerProduced = now;
            }

            if (FirstMilitaryUnit < 0f && military > 0)
            {
                FirstMilitaryUnit = now;
            }

            ResourceStockpile stock = ResourceStockpile.For(Faction);
            if (stock == null)
            {
                return;
            }

            if (stock.GetTotal(ResourceType.Food) >= WorkerFoodCost)
            {
                Milestone("50 food held", now);
            }

            bool room = Population.HasRoom(Faction);
            bool canAffordWorker = stock.GetTotal(ResourceType.Food) >= WorkerFoodCost;
            if (canAffordWorker && !room)
            {
                PopulationBlockedSeconds += dt;
            }

            foreach (Building b in Building.All)
            {
                if (b == null || !b.TryGetComponent(out FactionMember bm) || bm.Faction != Faction)
                {
                    continue;
                }

                Milestone(b.GetType().Name + " placed", now);
                if (b.TryGetComponent(out ConstructionSite site) ? site.IsComplete : true)
                {
                    Milestone(b.GetType().Name + " complete", now);
                }

                if (b is TownCenter tc && !tc.IsTraining && canAffordWorker && room)
                {
                    IdleTownCenterSeconds += dt;
                }
                else if (b is Barracks bk && bk.IsComplete && !bk.IsTraining && room
                    && stock.GetTotal(ResourceType.Food) >= SoldierFoodCost && stock.GetTotal(ResourceType.Gold) >= SoldierGoldCost)
                {
                    IdleBarracksSeconds += dt;
                }
            }
        }

        public string Report()
        {
            var sb = new StringBuilder();
            float total = 0f;
            foreach (float s in WorkerSeconds) total += s;
            string share(int i) => total > 0f ? $"{100f * WorkerSeconds[i] / total:0}%" : "-";
            sb.Append($"firstWorker {Fmt(FirstWorkerProduced)}s, firstMilitary {Fmt(FirstMilitaryUnit)}s, firstAttack {Fmt(FirstAttackOrder)}s, ");
            sb.Append($"peakWorkers {PeakWorkers}, peakMilitary {PeakMilitary}, ");
            sb.Append($"popBlocked {PopulationBlockedSeconds:0}s, idleTC {IdleTownCenterSeconds:0}s, idleBarracks {IdleBarracksSeconds:0}s of {ObservedSeconds:0}s, ");
            sb.Append($"workers food {share(0)} wood {share(1)} gold {share(2)} stone {share(3)} building {share(4)} farming {share(6)} idle {share(5)}");
            foreach (var kv in _milestones) sb.Append($" [{kv.Key} {kv.Value:0}s]");
            foreach (var kv in _notes) sb.Append($" | {kv.Key}: {kv.Value}");
            return sb.ToString();
        }

        private static string Fmt(float v) => v < 0f ? "never" : v.ToString("0");
    }
}
