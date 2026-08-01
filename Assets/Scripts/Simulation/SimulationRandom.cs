using System.Collections.Generic;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Master Sequence step 9, Step A0: deterministic randomness for validation runs, with each consumer
    /// on its OWN independent stream.
    ///
    /// Two requirements that pull against each other, both of which have to hold:
    ///
    /// **Reproducibility.** Six systems originally held their own `new System.Random()`, clock-seeded, so
    /// no two runs of identical code ever matched - measured, not assumed: consecutive 100-turn baselines
    /// reported 96 and 97 anomalies. That made Step A's bar ("changes ZERO simulation numbers, proven by
    /// identical trajectories") unfalsifiable, since the comparison would be swamped by noise.
    ///
    /// **Isolation.** A0's first attempt fixed reproducibility by putting all six on ONE shared stream.
    /// That was a mistake, caught by Elias before it did damage. A single stream couples every consumer:
    /// adding one new draw anywhere - say noise for a preliminary published figure - shifts every
    /// subsequent draw for events, SWF returns, Fed chair candidates, cabinet decisions and parliament
    /// jitter. Outcomes would change with no bug anywhere in the new code, and the identical-trajectory
    /// proof would fail for a reason that looks exactly like the leak it is meant to detect.
    ///
    /// Per-stream seeding satisfies both. Each consumer gets its own `System.Random`, seeded from the
    /// master seed plus a fixed per-stream offset. Same master seed reproduces every stream exactly;
    /// adding, removing or reordering draws in one stream cannot perturb another. This is the property
    /// the original per-system instances had, now made reproducible instead of clock-dependent.
    ///
    /// Unseeded remains the default, so real play still varies between playthroughs.
    /// </summary>
    public static class SimulationRandom
    {
        /// <summary>
        /// One independent sequence per consumer. **Append new values only - never reorder or insert.**
        /// The enum's integer value is baked into that stream's seed, so renumbering silently changes
        /// every seeded run's results and invalidates any baseline captured before the change.
        /// </summary>
        public enum Stream
        {
            Cabinet = 0,
            Event = 1,
            FederalReserve = 2,
            ForeignPolicy = 3,
            Parliament = 4,
            SovereignWealth = 5,

            /// <summary>Noise on preliminary published figures (Step A's revision mechanic). Its own stream specifically so publishing cannot perturb any simulation consumer's draws - the whole point of this class.</summary>
            PublicationRevision = 6
        }

        private static int? _masterSeed;

        private static readonly Dictionary<Stream, System.Random> Streams = new Dictionary<Stream, System.Random>();

        /// <summary>Makes every stream reproducible. Called once at the start of a validation run, before any turn is simulated - see SimulationTestRunner's `-seed=` argument. Clears existing streams so a mid-run reseed cannot leave some consumers on stale sequences.</summary>
        public static void Seed(int seed)
        {
            _masterSeed = seed;
            Streams.Clear();
        }

        /// <summary>This consumer's own sequence. Created on first use; seeded from the master seed plus the stream's own offset when a seed is set, clock-seeded otherwise.</summary>
        public static System.Random For(Stream stream)
        {
            if (Streams.TryGetValue(stream, out System.Random existing))
            {
                return existing;
            }

            System.Random created = _masterSeed.HasValue
                ? new System.Random(_masterSeed.Value + (int)stream * 7919)
                : new System.Random();

            Streams[stream] = created;
            return created;
        }
    }
}
