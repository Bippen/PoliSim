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
            /// US-8b (R-US25 as ruled): an election counted in play keys its noise off this stream's offset through
            /// <see cref="ElectionNoiseOn"/> - common to every world of one game - never off the counted stream.
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
            /// ⚠ CORRECTED (GA-1-3, 2026-10-14): the LIVE campaign draws from it - `CampaignRun` holds the
            /// object it is handed, as `CampaignAiHarness` does - which is why a caller that runs another
            /// game on the generator hands it back as objects, not counts (`LiveState`). This said that
            /// nothing in the live game drew from it (R-N2), true when it was written.
            /// </summary>
            CampaignAi = 8,

            /// <summary>
            /// A debate's exchange draws (elections spec §15: "Random Event" among the performance
            /// terms), added 2026-08-29 at W-B7. APPENDED after CampaignAi, never inserted; the
            /// trajectory suite is re-proven byte-identical at the boundary that adds it. Its own
            /// stream so a debate replays under a seed without re-running the AI's choices or the
            /// economy. ⚠ CORRECTED (GA-1-3): the live campaign holds and draws it, as `CampaignAi` above.
            /// </summary>
            Debate = 9,

            /// <summary>
            /// A scandal's lifecycle draws (elections spec §17: whether the evidence surfaces, and
            /// how strong the party estimates it to be — §36), added 2026-08-29 at W-B8. APPENDED
            /// after Debate, never inserted; the trajectory suite is re-proven byte-identical at the
            /// boundary that adds it. ⚠ CORRECTED (GA-1-3): the live campaign holds and draws it, as `CampaignAi` above.
            /// </summary>
            Scandal = 10,

            /// <summary>
            /// The order the constituencies DECLARE on election night (elections spec §30,
            /// W-E6), added 2026-08-30. APPENDED after Scandal, never inserted, so every
            /// existing stream's seed offset is untouched and no baseline moves; the trajectory
            /// suite is re-proven byte-identical at the boundary that adds it (the ElectionNoise
            /// and CampaignAi precedents).
            ///
            /// Its OWN stream because the night is a separate act from the count: an election
            /// already counted must be re-playable as a night, in a different declaration order,
            /// without moving a single vote. Borrowing ElectionNoise would make the arrival order
            /// and the result share one draw sequence, so changing how the night is staged would
            /// silently change who won — which is exactly the coupling this class exists to refuse.
            ///
            /// ⚠ NOTHING IN THE LIVE GAME DRAWS FROM IT (R-N2: the election system is unwired).
            /// </summary>
            ElectionNight = 11,

            /// <summary>
            /// CL-1 (2026-09-12): the player's PRE-campaign polls (`PreCampaignRun`) - its own stream so the run-up cannot
            /// move a single draw of the campaign's three, which is what lets the staging at the campaign's first day be
            /// asserted identical to the harness's (`CampaignClockHarness` 7a). Appended, never inserted, by this enum's
            /// own rule; rewound and re-stepped on load like the three (`SimulationManager.RestoreCampaign`).
            /// </summary>
            PreCampaign = 12
        }

        private static int? _masterSeed;

        private static readonly Dictionary<Stream, CountingRandom> Streams = new Dictionary<Stream, CountingRandom>();

        /// <summary>Makes every stream reproducible. Called once at the start of a validation run, before any turn is simulated - see SimulationTestRunner's `-seed=` argument. Clears existing streams, so a consumer that looks its stream up each time reads the new sequence; one that HOLDS a stream object across a reseed (the campaign's run, the run-up's poll) keeps the old one (GA-1-3's review: the films reseed mid-run).</summary>
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

        /// <summary>
        /// GA-1-3 (2026-10-14): the generator as it LIVES - its master seed and the stream objects themselves, not their counts. What a caller that runs
        /// another game on the generator takes and puts back (`ShadowBaseline`): a holder of a live stream - the campaign's run, the run-up's poll -
        /// draws on the object it was handed, and <see cref="RestoreState"/> builds new objects at the same counts, which such a holder never sees. The
        /// table's counts then froze while the campaign drew on, a save recorded the frozen figures (the false CAMPAIGN REPLAY alarm on the next load),
        /// and a campaign begun later in the session started from them, drawing numbers the last one had drawn.
        /// </summary>
        public sealed class LiveState
        {
            internal int MasterSeed;
            internal Dictionary<Stream, CountingRandom> Streams;
            internal LiveState() { }   // GA-1-3's review: only this assembly builds one, so Reinstate never meets a state with no streams
        }

        /// <summary>GA-1-3: the generator's live state - the master seed (taken as <see cref="MasterSeed"/> takes it) and the very stream objects.</summary>
        public static LiveState CaptureLive() => new LiveState { MasterSeed = MasterSeed, Streams = new Dictionary<Stream, CountingRandom>(Streams) };

        /// <summary>GA-1-3: puts back exactly what <see cref="CaptureLive"/> took - the same seed, the same objects at the positions they hold; a stream
        /// created since and absent from it is dropped, as <see cref="RestoreState"/> leaves an uncounted stream uncreated.</summary>
        public static void Reinstate(LiveState live)
        {
            _masterSeed = live.MasterSeed;
            Streams.Clear();
            foreach (KeyValuePair<Stream, CountingRandom> pair in live.Streams) { Streams[pair.Key] = pair.Value; }
        }

        /// <summary>
        /// US-8b (R-US25, RULED - Elias's ruling I9, 2026-10-18: "(a), with every fork drawing the live game's election noise (common random numbers), so a fork
        /// elects a different president only because of policy"): AN ELECTION'S NOISE, COMMON TO EVERY WORLD OF ONE GAME. A fresh generator keyed by the master
        /// seed, the <see cref="Stream.ElectionNoise"/> stream's offset, the country and the polling day - never a position on a counted stream. The game holds
        /// several worlds at once (the played one, the shadow's, the impact ledger's forks - each on its own copy of the streams, `ShadowBaseline`), and a
        /// counted stream's position is a world's own history: a fork that held one election more, or drew once more for any reason, would read the next
        /// election's noise from another place. Keyed by the day, the live game and every fork of it - which carry the live game's master seed (a fork is the
        /// live game's save restored; the seeded shadow is built on <see cref="MasterSeed"/>) - draw the same numbers for the same election whatever else each
        /// drew before it, so two worlds that differ only in policy differ in their count only through policy.
        /// <para>Not counted and not saved: nothing it returns enters the table, so it moves no stream, no save carries it and no load replays it (the
        /// numbers are rebuilt from the key). It reads <see cref="MasterSeed"/>, which seeds an unseeded game from the clock as <see cref="For"/> does.</para>
        /// </summary>
        public static System.Random ElectionNoiseOn(PoliSim.Data.CountryId country, System.DateTime pollingDay)
        {
            unchecked
            {
                ulong key = Mix((ulong)(uint)MasterSeed ^ ((ulong)(uint)((int)Stream.ElectionNoise * 7919) << 32));
                key = Mix(key ^ (ulong)(uint)(int)country);
                key = Mix(key ^ (ulong)pollingDay.Date.Ticks);
                return new System.Random((int)(key ^ (key >> 32)));
            }
        }

        /// <summary>
        /// AF-7 (q10): A CENTRAL BANK'S SHORTLIST AT A TERM'S END, COMMON TO EVERY WORLD OF ONE GAME - <see cref="ElectionNoiseOn"/>'s key on the
        /// <see cref="Stream.FederalReserve"/> stream's offset, the bank and the day its term ends: an AI appointer draws the same names in the played world,
        /// the shadow and every fork, and no counted stream moves (the player's own shortlist still draws the counted stream, as it always has).
        /// </summary>
        public static System.Random CentralBankShortlistOn(int bank, System.DateTime termEnds)
        {
            unchecked
            {
                ulong key = Mix((ulong)(uint)MasterSeed ^ ((ulong)(uint)((int)Stream.FederalReserve * 7919) << 32));
                key = Mix(key ^ (ulong)(uint)bank);
                key = Mix(key ^ (ulong)termEnds.Date.Ticks);
                return new System.Random((int)(key ^ (key >> 32)));
            }
        }

        /// <summary>SplitMix64's finaliser - a fixed bijection on 64 bits, so every part of the key moves every bit of the seed.</summary>
        private static ulong Mix(ulong z)
        {
            unchecked
            {
                z += 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
