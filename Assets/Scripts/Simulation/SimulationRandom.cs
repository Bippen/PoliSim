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
            PublicationRevision = 6,

            /// <summary>
            /// Election-day noise (elections spec §27: `Final Vote = Expected Vote + Election
            /// Noise`), added 2026-08-29. APPENDED, never inserted - this enum's own rule above -
            /// so every existing stream's seed offset is untouched and no baseline moves; the
            /// trajectory suite is re-proven byte-identical at the boundary that adds it.
            ///
            /// Its own stream because election noise must not perturb any simulation consumer's
            /// draws, and so one election can be re-run reproducibly without re-running the
            /// economy - the isolation argument this class was built on.
            ///
            /// ⚠ NOTHING IN THE LIVE GAME DRAWS FROM IT (R-N2: the election system is unwired).
            /// The name exists so that whatever eventually wires it cannot be tempted to borrow
            /// another consumer's stream.
            /// </summary>
            ElectionNoise = 7,

            /// <summary>
            /// The campaign AI's choice randomness (elections spec §32/§33: the softmax over
            /// scored actions, the chaotic personality's "inconsistent strategy"), added
            /// 2026-08-29 at W-C1. APPENDED after ElectionNoise, never inserted, so every
            /// existing stream's seed offset is untouched; the trajectory suite is re-proven
            /// byte-identical at the boundary that adds it (the ElectionNoise precedent).
            ///
            /// Its own stream so that an AI's decisions can be replayed under a seed without
            /// re-running the economy, and so that adding an AI draw can never shift an event,
            /// a cabinet decision or a poll's sampling.
            ///
            /// ⚠ NOTHING IN THE LIVE GAME DRAWS FROM IT (R-N2). `CampaignAi` takes a
            /// `System.Random` from its caller; today the only caller is `CampaignAiHarness`.
            /// </summary>
            CampaignAi = 8,

            /// <summary>
            /// A debate's exchange draws (elections spec §15: "Random Event" among the performance
            /// terms), added 2026-08-29 at W-B7. APPENDED after CampaignAi, never inserted; the
            /// trajectory suite is re-proven byte-identical at the boundary that adds it. Its own
            /// stream so a debate replays under a seed without re-running the AI's choices or the
            /// economy. ⚠ NOTHING IN THE LIVE GAME DRAWS FROM IT (R-N2).
            /// </summary>
            Debate = 9,

            /// <summary>
            /// A scandal's lifecycle draws (elections spec §17: whether the evidence surfaces, and
            /// how strong the party estimates it to be — §36), added 2026-08-29 at W-B8. APPENDED
            /// after Debate, never inserted; the trajectory suite is re-proven byte-identical at the
            /// boundary that adds it. ⚠ NOTHING IN THE LIVE GAME DRAWS FROM IT (R-N2).
            /// </summary>
            Scandal = 10
        }

        private static int? _masterSeed;

        private static readonly Dictionary<Stream, CountingRandom> Streams = new Dictionary<Stream, CountingRandom>();

        /// <summary>Makes every stream reproducible. Called once at the start of a validation run, before any turn is simulated - see SimulationTestRunner's `-seed=` argument. Clears existing streams so a mid-run reseed cannot leave some consumers on stale sequences.</summary>
        public static void Seed(int seed)
        {
            _masterSeed = seed;
            Streams.Clear();
        }

        /// <summary>
        /// This consumer's own sequence. Created on first use, seeded from the master seed plus the
        /// stream's own offset.
        ///
        /// **Changed for save/load (2026-08-02): there is now ALWAYS a master seed.** Previously an
        /// unseeded run used `new System.Random()` per stream, whose position could never be reconstructed
        /// - so an unseeded game, which is every real playthrough, would have been unsaveable. On first use
        /// without an explicit seed one is drawn from the clock and recorded, which keeps real play varying
        /// between playthroughs (the original intent) while making every game restorable.
        ///
        /// Returns `System.Random` rather than `CountingRandom` so no call site changes and none can
        /// accidentally depend on the counting.
        /// </summary>
        public static System.Random For(Stream stream)
        {
            if (Streams.TryGetValue(stream, out CountingRandom existing))
            {
                return existing;
            }

            if (!_masterSeed.HasValue)
            {
                _masterSeed = System.Environment.TickCount;
            }

            var created = new CountingRandom(_masterSeed.Value + (int)stream * 7919);
            Streams[stream] = created;
            return created;
        }

        /// <summary>
        /// The master seed this game is running on. Always has a value once any stream has been used - see
        /// <see cref="For"/>. Saved so a reload can rebuild every stream from the same root.
        /// </summary>
        public static int MasterSeed
        {
            get
            {
                if (!_masterSeed.HasValue)
                {
                    _masterSeed = System.Environment.TickCount;
                }

                return _masterSeed.Value;
            }
        }

        /// <summary>
        /// Every stream's draw count, for saving. **Only streams that have actually been used appear** -
        /// an absent stream has taken zero draws, and <see cref="RestoreState"/> recreates it on demand at
        /// position zero, which is the same thing.
        /// </summary>
        public static Dictionary<Stream, int> CaptureDrawCounts()
        {
            var counts = new Dictionary<Stream, int>();
            foreach (KeyValuePair<Stream, CountingRandom> pair in Streams)
            {
                counts[pair.Key] = pair.Value.DrawCount;
            }

            return counts;
        }

        /// <summary>
        /// Rebuilds every stream at its saved position: re-seed from the master seed, then fast-forward by
        /// the recorded draw count. This is the counting shim's whole purpose - without the fast-forward,
        /// a load silently rewinds every stream to turn zero and the game replays events the player has
        /// already seen.
        ///
        /// **Streams absent from <paramref name="drawCounts"/> are left uncreated rather than created at
        /// zero.** Identical behaviour - `For` builds them at position zero on demand - but it also means a
        /// save written before a new `Stream` value existed still loads correctly, with the new stream
        /// starting fresh. That matters because the `Stream` enum is append-only by rule.
        /// </summary>
        public static void RestoreState(int masterSeed, Dictionary<Stream, int> drawCounts)
        {
            _masterSeed = masterSeed;
            Streams.Clear();

            if (drawCounts == null)
            {
                return;
            }

            foreach (KeyValuePair<Stream, int> pair in drawCounts)
            {
                var stream = new CountingRandom(masterSeed + (int)pair.Key * 7919);
                stream.FastForward(pair.Value);
                Streams[pair.Key] = stream;
            }
        }
    }
}
