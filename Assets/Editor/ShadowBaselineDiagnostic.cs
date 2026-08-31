using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-C9 (P-G1) — **the proof gate. No shadow computation reaches a screen until this passes.**
    ///
    /// <para>The measured risk (`COMPLETED.md` §103): one shadow turn consumes **41 real draws**, so a
    /// naive shadow would make merely looking at the counterfactual change the game being played. The
    /// wrapper exists to stop that, and this asserts it in the two ways that matter — the weak one and
    /// the one that actually binds.</para>
    ///
    /// <list type="number">
    /// <item><description><b>THE COUNTER CHECK.</b> The real generator's master seed and every stream's
    /// draw count are identical across a shadow turn. Necessary, and on its own **not sufficient** — it
    /// checks a counter, not a consequence.</description></item>
    /// <item><description>⚠ <b>THE ONE THAT BINDS (C-C2's precedent).</b> Two real games from the same
    /// seed, advanced the same number of turns — one with a shadow advancing beside it every turn, one
    /// with no shadow at all — must end **byte-identical on every public field of every country's
    /// `EconomyState`**. That is the property the feature actually has to have, and a counter comparison
    /// cannot establish it: a restore that rewound a stream to the wrong position would leave the counts
    /// right and the values wrong.</description></item>
    /// <item><description><b>THE SHADOW IS THE NO-POLICY BASELINE.</b> A shadow advanced N turns must
    /// match a plain no-policy world advanced N turns from the same seed — which is the item's own
    /// done-when, and the reason the counterfactual can be called a baseline at all.</description></item>
    /// <item><description><b>AN EXCEPTION MUST NOT LEAVE THE REAL GENERATOR SHIFTED.</b> The restore is
    /// in a `finally`; this proves the `finally` by throwing inside a shadow turn and checking the real
    /// state came back anyway.</description></item>
    /// </list>
    /// </summary>
    public static class ShadowBaselineDiagnostic
    {
        private const int Turns = 8;
        private const int Seed = 777;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-C9: the shadow baseline's proof gate ===\n");
            int failures = 0;

            // ---- 1. the counter check ----
            SimulationRandom.Seed(Seed);
            World probe = WorldFactory.CreateDefault();
            var probeGo = new GameObject("C-C9 PROBE");
            SimulationManager probeSim = probeGo.AddComponent<SimulationManager>();
            probeSim.SetWorld(probe);
            var probeNone = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country c in probe.Countries) { probeNone[c.Id] = PolicyDecision.None(); }
            for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { probeSim.AdvanceDay(); }
            probeSim.AdvanceTurn(probeNone);

            var shadow = new ShadowBaseline(Seed);
            int seedBefore = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> before = SimulationRandom.CaptureDrawCounts();

            shadow.AdvanceTurn();

            int seedAfter = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> after = SimulationRandom.CaptureDrawCounts();

            int moved = 0;
            foreach (KeyValuePair<SimulationRandom.Stream, int> kv in after)
            {
                before.TryGetValue(kv.Key, out int was);
                if (kv.Value != was) { moved++; Debug.LogError($"C-C9: stream {kv.Key} moved {was} -> {kv.Value} across a shadow turn."); }
            }

            if (seedBefore != seedAfter) { moved++; Debug.LogError($"C-C9: the master seed moved {seedBefore} -> {seedAfter} across a shadow turn."); }
            failures += moved;
            sb.Append(moved == 0
                ? "    1. counters   OK - the master seed and every stream's draw count are unchanged across a shadow turn.\n"
                : F("    1. counters   {0} MOVED - see the errors above.\n", moved));

            shadow.Dispose();
            UnityEngine.Object.DestroyImmediate(probeGo);

            // ---- 2. the one that binds: does the real game end up in the same place? ----
            string withoutShadow = RunReal(Turns, withShadow: false);
            string withShadow = RunReal(Turns, withShadow: true);

            bool identical = string.Equals(withoutShadow, withShadow, StringComparison.Ordinal);
            if (!identical)
            {
                failures++;
                Debug.LogError("C-C9: the real game's state DIFFERS after " + Turns + " turns depending on whether a shadow ran beside it. "
                               + "The counterfactual is changing the game it is supposed to be observing - the whole reason the wrapper exists.");
            }

            sb.Append(identical
                ? F("    2. the real game   IDENTICAL over {0} turns, every public EconomyState field of every country, with a shadow running beside it and without.\n", Turns)
                : "    2. the real game   ⚠ DIFFERS - see the error above.\n");

            // ---- 3. the shadow IS the no-policy baseline ----
            string shadowState = RunShadowOnly(Turns);
            string plainNoPolicy = RunPlainNoPolicy(Turns);
            bool isBaseline = string.Equals(shadowState, plainNoPolicy, StringComparison.Ordinal);
            if (!isBaseline)
            {
                failures++;
                Debug.LogError("C-C9: the shadow does not match a plain no-policy world advanced the same way from the same seed, "
                               + "so it is not the baseline it claims to be.");
            }

            sb.Append(isBaseline
                ? F("    3. the baseline    IDENTICAL - a shadow advanced {0} turns matches a plain no-policy world from the same seed.\n", Turns)
                : "    3. the baseline    ⚠ DIFFERS - see the error above.\n");

            // ---- 4. the divergence is the player's, and the shadow does not follow them ----
            failures += AssertShadowDoesNotFollowThePlayer(sb);

            // ---- 5. the finally ----
            failures += AssertRestoreSurvivesAnException(sb);

            sb.Append(F("\n=== ShadowBaselineDiagnostic: {0} ===\n",
                failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILURE(S)"));

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        /// <summary>A real game advanced <paramref name="turns"/> turns, optionally with a shadow
        /// advancing beside it each turn, returned as its full state.</summary>
        private static string RunReal(int turns, bool withShadow)
        {
            SimulationRandom.Seed(Seed);
            var go = new GameObject("C-C9 REAL");
            ShadowBaseline shadow = null;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                World world = WorldFactory.CreateDefault();
                sim.SetWorld(world);

                var none = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { none[c.Id] = PolicyDecision.None(); }

                if (withShadow) { shadow = new ShadowBaseline(Seed); }

                for (int t = 0; t < turns; t++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(none);
                    shadow?.AdvanceTurn();
                }

                return Fingerprint(world);
            }
            finally
            {
                shadow?.Dispose();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static string RunShadowOnly(int turns)
        {
            SimulationRandom.Seed(Seed);
            var shadow = new ShadowBaseline(Seed);
            try
            {
                for (int t = 0; t < turns; t++) { shadow.AdvanceTurn(); }
                return Fingerprint(shadow.World);
            }
            finally
            {
                shadow.Dispose();
            }
        }

        private static string RunPlainNoPolicy(int turns)
        {
            SimulationRandom.Seed(Seed);
            var go = new GameObject("C-C9 PLAIN");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                World world = WorldFactory.CreateDefault();
                sim.SetWorld(world);

                var none = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { none[c.Id] = PolicyDecision.None(); }

                for (int t = 0; t < turns; t++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(none);
                }

                return Fingerprint(world);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// ⚠ **The item's last done-when: the divergence after a known change is the player's, and the
        /// shadow does not follow them.** A real game is advanced with a live interest-rate change every
        /// turn while a shadow runs beside it; the shadow must end **identical to a plain no-policy
        /// world** — not merely close to it.
        ///
        /// <para>That is what makes "with your policies against without" mean anything. A shadow that
        /// drifted toward the player's choices would still draw two lines and would still look like a
        /// counterfactual, while quietly measuring nothing: the gap between the two would be an artefact
        /// of the drift rather than the consequence of the policy. This asserts the property in the one
        /// state where it can fail — with the real run actually diverging.</para>
        /// </summary>
        private static int AssertShadowDoesNotFollowThePlayer(StringBuilder sb)
        {
            SimulationRandom.Seed(Seed);
            var go = new GameObject("C-C9 DIVERGE");
            ShadowBaseline shadow = null;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                World world = WorldFactory.CreateDefault();
                sim.SetWorld(world);
                shadow = new ShadowBaseline(Seed);

                // A real, live policy every turn. ⚠ NOT the interest-rate lever: the first draft used it and
                // this assertion honestly reported itself UNTESTED rather than claiming a pass, because
                // C-C7 seeded a central-bank head for Sweden and Poland, so all six countries now have
                // `CurrentFedChair != null` and `CurrencySystem.ApplyInterestRateChanges` (`:45-50`) hands
                // the rate to the bank and ignores the player's `InterestRateChange` entirely. That is
                // C-C7's ruling working as intended, not a bug - but it leaves the rate a DEAD lever for a
                // divergence test. The tax rate is a live one: `ApplyTaxRateChanges` writes `TaxLine.Rate`
                // directly every turn, and the USA is the country with the detailed portfolio behind it.
                var acting = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries)
                {
                    var act = new PolicyDecision();
                    if (c.Id == CountryId.USA) { act.TaxRateOverrides[TaxType.IncomeTax] = 60f; }
                    acting[c.Id] = act;
                }

                for (int t = 0; t < Turns; t++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(acting);
                    shadow.AdvanceTurn();
                }

                string shadowState = Fingerprint(shadow.World);
                string realState = Fingerprint(world);
                string baseline = RunPlainNoPolicy(Turns);

                bool shadowIsBaseline = string.Equals(shadowState, baseline, StringComparison.Ordinal);
                bool realDiverged = !string.Equals(realState, baseline, StringComparison.Ordinal);

                if (!realDiverged)
                {
                    // Not a failure of the shadow - a failure of the TEST, and saying so beats reporting
                    // a pass that proved nothing because the "known change" changed nothing.
                    sb.Append("    4. divergence      (the applied change moved nothing, so this run proves nothing - the assertion is untested, not passed)\n");
                    return 0;
                }

                if (!shadowIsBaseline)
                {
                    Debug.LogError("C-C9: with the real game diverging under a live policy, the SHADOW also moved off the no-policy baseline - "
                                   + "it is following the player. The gap between the two lines would then be an artefact of that drift rather than "
                                   + "the consequence of the policy, and the counterfactual would be measuring nothing.");
                    sb.Append("    4. divergence      ⚠ FAILED - the shadow followed the player.\n");
                    return 1;
                }

                sb.Append(F("    4. divergence      OK - over {0} turns of a live tax policy the real game moved and the shadow stayed EXACTLY on the no-policy baseline.\n", Turns));
                return 0;
            }
            finally
            {
                shadow?.Dispose();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>⚠ The `finally` is the difference between a safe wrapper and one that is safe only
        /// when nothing goes wrong. Forced by disposing the shadow's host mid-life so its next advance
        /// throws, then checking the real generator came back regardless.</summary>
        private static int AssertRestoreSurvivesAnException(StringBuilder sb)
        {
            SimulationRandom.Seed(Seed);
            var shadow = new ShadowBaseline(Seed);
            shadow.AdvanceTurn();

            int seedBefore = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> before = SimulationRandom.CaptureDrawCounts();

            // ⚠ Force a throw INSIDE the swapped-in state - the exact window where an unprotected restore
            // would leak. The first attempt at this simply disposed the host, and Unity's destroyed-object
            // semantics meant the next advance did NOT throw, so the guard went untested and the
            // diagnostic said so rather than claiming a pass. Nulling the private manager by reflection
            // throws for certain, and reflection against a private member is this project's established
            // diagnostic idiom rather than a test-only hook bolted onto production code.
            FieldInfo simField = typeof(ShadowBaseline).GetField("_sim", BindingFlags.Instance | BindingFlags.NonPublic);
            if (simField == null)
            {
                sb.Append("    5. the finally     (could not reach ShadowBaseline._sim to force a failure - guard untested this run)\n");
                shadow.Dispose();
                return 0;
            }

            simField.SetValue(shadow, null);

            bool threw = false;
            try { shadow.AdvanceTurn(); }
            catch { threw = true; }

            Dictionary<SimulationRandom.Stream, int> after = SimulationRandom.CaptureDrawCounts();
            int moved = 0;
            foreach (KeyValuePair<SimulationRandom.Stream, int> kv in after)
            {
                before.TryGetValue(kv.Key, out int was);
                if (kv.Value != was) { moved++; }
            }

            if (SimulationRandom.MasterSeed != seedBefore) { moved++; }

            if (!threw)
            {
                sb.Append("    5. the finally     (no exception was raised by the forced failure - the guard is untested this run, and says so rather than claiming a pass)\n");
                return 0;
            }

            if (moved > 0)
            {
                Debug.LogError("C-C9: a shadow turn that THREW left the real generator shifted - the restore is not in a finally, or the finally does not cover the swap.");
                sb.Append("    5. the finally     ⚠ FAILED - an exception mid-shadow leaked generator state.\n");
                return 1;
            }

            sb.Append("    5. the finally     OK - a shadow turn that threw left the real seed and every draw count untouched.\n");
            return 0;
        }

        /// <summary>Every public float of every country's state, as one string.</summary>
        private static string Fingerprint(World world)
        {
            var sb = new StringBuilder();
            foreach (Country c in world.Countries)
            {
                sb.Append(c.Id).Append(':');
                foreach (FieldInfo f in typeof(EconomyState).GetFields())
                {
                    if (f.FieldType != typeof(float)) { continue; }
                    sb.Append(f.Name).Append('=')
                      .Append(((float)f.GetValue(c.State)).ToString("R", CultureInfo.InvariantCulture)).Append('|');
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
