using System;
using System.Collections.Generic;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Buildings
{
    // One queued unit: what to spawn (Kind is the owning building's own
    // enum value, opaque to the queue), what it cost, and its countdown.
    public sealed class ProductionItem
    {
        public int Kind;
        public string Label;
        public ResourceType[] CostTypes;
        public float[] CostAmounts;
        public float Total;
        public float Remaining;

        public float Progress => Total <= 0f ? 1f : 1f - Remaining / Total;
    }

    // Shared production queue for a single building (Prompt 9). Pure C#, no
    // MonoBehaviour: the owning building calls TryEnqueue from its
    // RequestTrain*, Tick from Update, and supplies the spawn callback.
    // Rules:
    //  - prerequisites (capacity, population room counting already-queued
    //    items, affordability) are all checked BEFORE anything is spent;
    //  - cost is deducted exactly once, at enqueue;
    //  - only the head item counts down; order is strict FIFO;
    //  - Cancel refunds RefundFraction of that item's own recorded cost;
    //  - a finished head with no population room waits (Blocked) rather
    //    than exceeding the cap, and retries every tick;
    //  - Clear() drops everything with no refund (rematch/reset), while
    //    CancelAll() refunds (building destroyed).
    public sealed class ProductionQueue
    {
        public const int DefaultCapacity = 5;

        private readonly List<ProductionItem> _items = new List<ProductionItem>();

        public ProductionQueue(int capacity = DefaultCapacity, float refundFraction = 1f)
        {
            Capacity = capacity;
            RefundFraction = refundFraction;
        }

        public int Capacity { get; }
        public float RefundFraction { get; set; }
        public IReadOnlyList<ProductionItem> Items => _items;
        public int Count => _items.Count;
        public bool IsEmpty => _items.Count == 0;
        public bool IsFull => _items.Count >= Capacity;
        public ProductionItem Active => _items.Count > 0 ? _items[0] : null;
        public float ActiveProgress => Active != null ? Active.Progress : 0f;
        // Head has finished counting down but cannot spawn (population cap).
        public bool Blocked { get; private set; }
        // Human-readable reason for the most recent refused TryEnqueue.
        public string LastFailure { get; private set; }
        // Time.unscaledTime of that refusal, so the UI can show it briefly.
        public float LastFailureTime { get; private set; } = -100f;

        // One-line UI summary: active item + progress, waiting count,
        // population stall, and a recent refusal reason (shown ~3s).
        public string Describe()
        {
            string text = "";
            if (_items.Count > 0)
            {
                text = $"Training {Active.Label} {(int)(ActiveProgress * 100f)}%";
                if (_items.Count > 1)
                {
                    text += $" (+{_items.Count - 1} queued)";
                }

                if (Blocked)
                {
                    text += " - waiting: population cap";
                }
            }

            if (LastFailure != null && UnityEngine.Time.unscaledTime - LastFailureTime < 3f)
            {
                text += (text.Length > 0 ? " | " : "") + "Can't train: " + LastFailure;
            }

            return text;
        }

        public void Fail(string reason)
        {
            LastFailure = reason;
            LastFailureTime = UnityEngine.Time.unscaledTime;
        }

        public bool TryEnqueue(FactionId faction, int kind, string label, float time, params (ResourceType type, float amount)[] cost)
        {
            LastFailure = null;

            if (IsFull)
            {
                Fail("Queue is full");
                return false;
            }

            if (Population.Current(faction) + _items.Count >= Population.Cap(faction))
            {
                Fail("Population cap reached");
                return false;
            }

            ResourceStockpile stockpile = ResourceStockpile.For(faction);
            if (stockpile == null)
            {
                Fail("No stockpile");
                return false;
            }

            foreach ((ResourceType type, float amount) in cost)
            {
                if (stockpile.GetTotal(type) < amount)
                {
                    Fail($"Not enough {type}");
                    return false;
                }
            }

            var item = new ProductionItem
            {
                Kind = kind,
                Label = label,
                CostTypes = new ResourceType[cost.Length],
                CostAmounts = new float[cost.Length],
                Total = time,
                Remaining = time,
            };

            for (int i = 0; i < cost.Length; i++)
            {
                item.CostTypes[i] = cost[i].type;
                item.CostAmounts[i] = cost[i].amount;
                stockpile.Add(cost[i].type, -cost[i].amount);
            }

            _items.Add(item);
            return true;
        }

        // Advances the head item. spawn is invoked at most once per call,
        // only when the head has finished and the faction has room.
        public void Tick(FactionId faction, float deltaTime, Action<ProductionItem> spawn)
        {
            if (_items.Count == 0)
            {
                Blocked = false;
                return;
            }

            ProductionItem head = _items[0];
            if (head.Remaining > 0f)
            {
                head.Remaining -= deltaTime;
            }

            if (head.Remaining > 0f)
            {
                Blocked = false;
                return;
            }

            head.Remaining = 0f;
            if (!Population.HasRoom(faction))
            {
                Blocked = true;
                return;
            }

            Blocked = false;
            _items.RemoveAt(0);
            spawn(head);
        }

        // Cancels one item (0 = active). Returns false for a bad index.
        public bool Cancel(FactionId faction, int index)
        {
            if (index < 0 || index >= _items.Count)
            {
                return false;
            }

            Refund(faction, _items[index]);
            _items.RemoveAt(index);
            if (_items.Count == 0 || index == 0)
            {
                Blocked = false;
            }

            return true;
        }

        public bool CancelLast(FactionId faction)
        {
            return Cancel(faction, _items.Count - 1);
        }

        // Building destroyed: refund everything still queued.
        public void CancelAll(FactionId faction)
        {
            foreach (ProductionItem item in _items)
            {
                Refund(faction, item);
            }

            Clear();
        }

        // Reset without refund (rematch, or restoring a save over it).
        public void Clear()
        {
            _items.Clear();
            Blocked = false;
        }

        // Save/load: costs were already paid when the save was taken.
        public void RestoreItem(ProductionItem item)
        {
            _items.Add(item);
        }

        private void Refund(FactionId faction, ProductionItem item)
        {
            ResourceStockpile stockpile = ResourceStockpile.For(faction);
            if (stockpile == null)
            {
                return;
            }

            for (int i = 0; i < item.CostTypes.Length; i++)
            {
                stockpile.Add(item.CostTypes[i], item.CostAmounts[i] * RefundFraction);
            }
        }
    }
}
