namespace KingdomsOfBharat.Core
{
    // Pure selection logic for biasing resource placement (Gold/Stone)
    // toward a map's feature mask (see SkirmishTerrainCarving's Compute*Mask
    // functions) - a "quarry in the mountains" feel on Mountain Pass, ore
    // on Crossroad Valleys' mesas - without a true (unbounded) rejection
    // sampling loop. ResourceNodeSpawner draws a handful of candidate points
    // in the usual ring and scores each against the mask; this just picks
    // the best-scoring one.
    public static class ResourceBias
    {
        // Index of the highest score, first occurrence wins on a tie. -1
        // for a null/empty array.
        public static int PickBestScoringCandidate(float[] scores)
        {
            if (scores == null || scores.Length == 0)
            {
                return -1;
            }

            int best = 0;
            for (int i = 1; i < scores.Length; i++)
            {
                if (scores[i] > scores[best])
                {
                    best = i;
                }
            }

            return best;
        }
    }
}
