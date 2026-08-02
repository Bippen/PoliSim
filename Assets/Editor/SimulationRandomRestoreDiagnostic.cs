using System.Collections.Generic;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Proves the counting shim (Master Sequence item 8, Gap 1) actually restores stream position.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.SimulationRandomRestoreDiagnostic.Run -logFile &lt;path&gt;`
    ///
    /// **Three checks, and the second is the one that matters.** A save/load system that rewinds the RNG
    /// still looks deterministic and still produces a valid-looking file — the failure is invisible unless
    /// something explicitly compares the CONTINUATION after a restore against the continuation without
    /// one. That is check 2.
    ///
    /// No Play mode, no World, no scene: this is arithmetic over `SimulationRandom` only.
    /// </summary>
    public static class SimulationRandomRestoreDiagnostic
    {
        private const int Seed = 777;

        public static void Run()
        {
            int passed = 0, total = 0;

            // ---- SELF-TEST FIRST, per the standing rule from verification-integrity instance 10.
            // If a fresh seeded stream does not reproduce its own first values, every result below is void.
            SimulationRandom.Seed(Seed);
            double firstA = SimulationRandom.For(SimulationRandom.Stream.Event).NextDouble();
            SimulationRandom.Seed(Seed);
            double firstB = SimulationRandom.For(SimulationRandom.Stream.Event).NextDouble();
            Debug.Log($"SELFTEST reseed reproduces first draw: {firstA:R} == {firstB:R} -> {(firstA == firstB ? "OK" : "BROKEN - everything below is void")}");

            // ---- CHECK 1: the equivalence property the fast-forward depends on.
            // Claim: every public draw method consumes exactly one internal sample, so replaying N calls
            // of NextDouble() lands in the same place as the original MIXED sequence of N calls.
            total++;
            var mixed = new CountingRandom(12345);
            for (int i = 0; i < 50; i++)
            {
                if (i % 3 == 0) { mixed.Next(); }
                else if (i % 3 == 1) { mixed.Next(100); }
                else { mixed.NextDouble(); }
            }
            double afterMixed = mixed.NextDouble();

            var replayed = new CountingRandom(12345);
            replayed.FastForward(50);
            double afterReplay = replayed.NextDouble();

            bool check1 = afterMixed == afterReplay;
            if (check1) { passed++; }
            Debug.Log($"{(check1 ? "PASS" : "FAIL")} CHECK 1 mixed-call equivalence: 50 mixed draws then {afterMixed:R} " +
                $"vs FastForward(50) then {afterReplay:R}. This is the property the whole shim rests on.");

            // ---- CHECK 2: capture -> restore reproduces the CONTINUATION, not just the seed.
            // The failure this catches: re-seeding without fast-forwarding rewinds to turn zero, so a
            // reloaded game replays draws the player already saw. Deterministic, valid-looking, wrong.
            total++;
            SimulationRandom.Seed(Seed);
            var streams = new[] { SimulationRandom.Stream.Event, SimulationRandom.Stream.Cabinet, SimulationRandom.Stream.SovereignWealth };
            foreach (SimulationRandom.Stream s in streams)
            {
                System.Random r = SimulationRandom.For(s);
                int draws = 10 + (int)s * 7;              // deliberately uneven per stream
                for (int i = 0; i < draws; i++) { r.NextDouble(); }
            }

            int savedSeed = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> savedCounts = SimulationRandom.CaptureDrawCounts();

            // What the game WOULD have produced next, had it never been saved.
            var expected = new Dictionary<SimulationRandom.Stream, double>();
            foreach (SimulationRandom.Stream s in streams) { expected[s] = SimulationRandom.For(s).NextDouble(); }

            // Now restore from the capture and ask for the next value again.
            SimulationRandom.RestoreState(savedSeed, savedCounts);
            bool check2 = true;
            foreach (SimulationRandom.Stream s in streams)
            {
                double got = SimulationRandom.For(s).NextDouble();
                bool ok = got == expected[s];
                check2 &= ok;
                Debug.Log($"    {(ok ? "ok  " : "MISMATCH")} stream {s,-16} expected {expected[s]:R} got {got:R}");
            }
            if (check2) { passed++; }
            Debug.Log($"{(check2 ? "PASS" : "FAIL")} CHECK 2 restore reproduces the continuation across {streams.Length} streams at different positions.");

            // ---- CHECK 3: the naive approach must FAIL, or check 2 proves nothing.
            // If re-seeding alone happened to reproduce the continuation, check 2 would pass for a system
            // with no fast-forward at all - a check that cannot fail is not a check.
            total++;
            SimulationRandom.RestoreState(savedSeed, new Dictionary<SimulationRandom.Stream, int>());
            double naive = SimulationRandom.For(SimulationRandom.Stream.Event).NextDouble();
            bool check3 = naive != expected[SimulationRandom.Stream.Event];
            if (check3) { passed++; }
            Debug.Log($"{(check3 ? "PASS" : "FAIL")} CHECK 3 seed-only restore DOES diverge (got {naive:R}, " +
                $"continuation was {expected[SimulationRandom.Stream.Event]:R}) - confirming check 2 tests something real.");

            Debug.Log($"=== SimulationRandom restore: {passed} of {total} PASS ===");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }
    }
}
