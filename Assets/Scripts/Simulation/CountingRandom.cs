namespace PoliSim.Simulation
{
    /// <summary>
    /// Master Sequence item 8 (save/load), Gap 1: a `System.Random` that records how many draws it has
    /// taken, so a stream's position can be restored on load.
    ///
    /// **Why this exists.** `System.Random` exposes no way to read or restore its internal position.
    /// Saving only the master seed and re-seeding on load therefore rewinds every stream to turn zero: a
    /// game saved at turn 50 and reloaded would REPLAY the same event draws, Fed-chair candidates and
    /// cabinet decisions the player already saw, in the same order. The save would look valid and the
    /// simulation would still look deterministic - it would just be running the wrong part of the
    /// sequence. That is a correctness failure, not a save-scum exploit, and easy to mistake for one.
    ///
    /// **Elias's ruling (A1, 2026-08-01): the counting shim**, over replacing `System.Random` with a
    /// serialisable PRNG. Reversible beats permanent under uncertainty; the xorshift option stays open
    /// once real load times are known. Crucially it **preserves every recorded baseline** - the number
    /// sequences are unchanged, because this subclasses `System.Random` rather than replacing it.
    ///
    /// **It derives from `System.Random` deliberately.** Every call site is typed `System.Random` and
    /// calls `.Next(...)` / `.NextDouble()` on it, so subclassing means not one call site changes and not
    /// one draw sequence moves. A wrapper class would have required touching all of them.
    ///
    /// **The fast-forward's correctness rests on one property**, verified empirically rather than assumed
    /// (see `SaveLoadDiagnostic`): in this runtime's `System.Random`, every public draw method consumes
    /// exactly ONE internal sample. That makes replaying N calls of any single method equivalent to
    /// replaying the original mix of N calls. `NextBytes` is the documented exception - it consumes one
    /// per byte - and it is not used anywhere in this project; the override below throws rather than
    /// silently miscounting if that ever changes.
    /// </summary>
    public sealed class CountingRandom : System.Random
    {
        /// <summary>How many draws this stream has taken. This is the value that gets saved.</summary>
        public int DrawCount { get; private set; }

        public CountingRandom(int seed) : base(seed) { }

        public override int Next()
        {
            DrawCount++;
            return base.Next();
        }

        public override int Next(int maxValue)
        {
            DrawCount++;
            return base.Next(maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            DrawCount++;
            return base.Next(minValue, maxValue);
        }

        public override double NextDouble()
        {
            DrawCount++;
            return base.NextDouble();
        }

        /// <summary>
        /// Deliberately unsupported. `NextBytes` consumes one internal sample PER BYTE, so counting it as
        /// a single draw would make the fast-forward land in the wrong place - a save that looks valid and
        /// replays the wrong sequence, which is the exact failure this class exists to prevent. Nothing in
        /// this project calls it; if something starts to, this throws immediately instead of corrupting
        /// restores silently. Fix by counting `buffer.Length` draws here and re-verifying the equivalence
        /// property in `SaveLoadDiagnostic`.
        /// </summary>
        public override void NextBytes(byte[] buffer)
        {
            throw new System.NotSupportedException(
                "CountingRandom.NextBytes is unsupported: it consumes one internal sample per byte, which " +
                "would break the save/load fast-forward. See the class comment before enabling it.");
        }

        /// <summary>
        /// Advances this stream by <paramref name="draws"/> without using the results, putting it back
        /// where it was when the game was saved. O(draws) by design - the accepted cost of the counting
        /// shim over a serialisable PRNG.
        /// </summary>
        public void FastForward(int draws)
        {
            for (int i = 0; i < draws; i++)
            {
                base.NextDouble();
            }

            DrawCount = draws;
        }
    }
}
