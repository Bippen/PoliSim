namespace PoliSim.Simulation
{
    /// <summary>
    /// Master Sequence step 9, Step A0: the single random source every stochastic simulation system
    /// draws from, so a run can be made reproducible by seeding one place.
    ///
    /// Why this exists. Five systems (Cabinet, Event, FederalReserve, ForeignPolicy, Parliament) each
    /// held their own `new System.Random()`, which seeds from the clock - so no two runs of IDENTICAL
    /// code ever matched. That was measured, not assumed: two 100-turn baseline runs with no simulation
    /// changes between them reported 96 and 97 anomalies.
    ///
    /// That makes Step A's stated acceptance bar - "must change ZERO simulation numbers, proven by
    /// identical trajectories before and after" - impossible to evaluate, because the before/after
    /// difference would be swamped by run-to-run noise. And a published-value leak into Okun's Law, the
    /// Phillips Curve or the Fiscal Reaction Function is precisely the small, slow divergence that noise
    /// would hide. The proof was right; the infrastructure for it did not exist.
    ///
    /// Seeding changes WHICH random numbers appear, never how any equation works - no model behaviour is
    /// altered by this file. Unseeded remains the default, so real play is untouched and still varies
    /// between playthroughs; only a validation run that explicitly passes `-seed=N` becomes reproducible.
    ///
    /// Note this deliberately makes all five systems share ONE stream rather than keeping five
    /// independent ones. A single stream is seedable from a single call and cannot drift out of sync;
    /// the cost is that the draw order now interleaves across systems, which changes which numbers each
    /// system sees relative to the old code. That is why A0 is validated on its own, before Step A's real
    /// work, rather than bundled with it.
    /// </summary>
    public static class SimulationRandom
    {
        private static System.Random _shared = new System.Random();

        /// <summary>Makes every subsequent draw reproducible. Called once at the start of a validation run, before any turn is simulated - see SimulationTestRunner's `-seed=` argument.</summary>
        public static void Seed(int seed)
        {
            _shared = new System.Random(seed);
        }

        /// <summary>The shared source. A property rather than a field so re-seeding is picked up by callers that cached the reference at static-init time, which is every one of them.</summary>
        public static System.Random Shared => _shared;
    }
}
