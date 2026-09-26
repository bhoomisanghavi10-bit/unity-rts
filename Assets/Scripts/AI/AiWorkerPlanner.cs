using UnityEngine;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.AI
{
    // Pure decision helpers for the AI economy (no scene access) so the
    // choices can be tested and never depend on scene-search order.
    public static class AiWorkerPlanner
    {
        // Workers wanted on each resource, as a share of the workforce.
        public static readonly float[] DesiredShare = { 0.40f, 0.30f, 0.15f, 0.15f }; // Food, Wood, Gold, Stone
        // A resource already this plentiful is not worth more workers.
        public static readonly float[] Saturation = { 400f, 400f, 300f, 300f };
        public const int MaxWorkersPerNode = 3;
        public const int WorkerTarget = 16;
        public const int FarmStaffTarget = 3;

        // current[t] = workers presently on resource t; stock[t] = stockpile.
        // Returns the resource with the biggest shortfall against its desired
        // share (ties go to the lower index, so the result is deterministic).
        // needs[t] = how much of resource t the AI's current goal is still
        // short of (0 when covered); each 30 short adds one worker's worth of
        // pull (capped), so what unlocks the next step gets gathered first.
        public static ResourceType ChooseResource(int[] current, float[] stock, int totalWorkers, float[] needs = null)
        {
            int best = 0;
            float bestScore = float.MinValue;
            for (int t = 0; t < 4; t++)
            {
                float share = stock[t] >= Saturation[t] ? 0f : DesiredShare[t];
                float pull = needs == null ? 0f : Mathf.Min(needs[t] / 30f, 4f);
                float score = share * (totalWorkers + 1) - current[t] + pull;
                if (score > bestScore + 0.0001f)
                {
                    bestScore = score;
                    best = t;
                }
            }

            return (ResourceType)best;
        }

        // Index of the candidate closest to the site (lowest index wins ties),
        // -1 if none: builders are chosen by distance, never "first found".
        public static int PickNearest(Vector3[] candidates, Vector3 site)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < candidates.Length; i++)
            {
                float d = (candidates[i] - site).sqrMagnitude;
                if (d < bestDistance - 0.0001f)
                {
                    bestDistance = d;
                    best = i;
                }
            }

            return best;
        }
    }
}
