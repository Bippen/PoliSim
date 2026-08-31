using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// C-C10 (P-G2) — **the impact ledger's proof gate.** The same discipline C-C9 established: an
    /// explanation layer that runs beside the game must be proved not to change the game, and proved to
    /// sum to the thing it claims to explain, before any of it reaches a screen.
    ///
    /// <list type="number">
    /// <item><description><b>THE PARTITION IS COMPLETE.</b> Every field of `PolicyDecision` belongs to
    /// exactly one family. A dial in no family is a dial the ledger would never attribute and nobody
    /// would notice.</description></item>
    /// <item><description>⚠ <b>THE ONE THAT BINDS.</b> A real game with the ledger's counterfactual
    /// worlds running beside it is **byte-identical** to the same game with none — every public
    /// `EconomyState` field of every country, over the whole run.</description></item>
    /// <item><description><b>THE IDENTITY.</b> For every stat, divergence == Σ contributions +
    /// interaction, exactly. With the interaction line present this holds by construction; asserting it
    /// is what stops a later change from quietly breaking the construction.</description></item>
    /// <item><description>⚠ <b>THE LAZY FORK IS EXACT, NOT AN APPROXIMATION.</b> A family's except-world
    /// forked at the turn the player first touches it must be byte-identical to one run from the seed
    /// with that family stripped the whole way. This is the claim the ledger's whole cost model rests
    /// on, so it is measured rather than argued.</description></item>
    /// <item><description><b>THE COST, MEASURED AND STATED.</b> Per real turn, with n families in
    /// play.</description></item>
    /// </list>
    /// </summary>
    public static class PolicyImpactLedgerDiagnostic
    {
        private const int Seed = 777;
        private const int Turns = 10;
        private const int TouchTaxesAtTurn = 4;
        private static readonly CountryId Player = CountryId.USA;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-C10 (P-G2): the impact ledger's proof gate ===\n");
            int failures = 0;

            // ---- 1. the partition ----
            try
            {
                var probeShadow = new ShadowBaseline(Seed);
                var probe = new PolicyImpactLedger(probeShadow);
                probeShadow.Dispose();
                probe.Dispose();
                sb.Append("    1. the partition   OK - every PolicyDecision field belongs to exactly one family.\n");
            }
            catch (Exception e)
            {
                failures++;
                Debug.LogError("C-C10: " + e.Message);
                sb.Append("    1. the partition   ⚠ INCOMPLETE - see the error above.\n");
            }

            // ---- 2. the one that binds ----
            string withoutLedger = RunReal(withLedger: false, out _, out _);
            string withLedger = RunReal(withLedger: true, out PolicyImpactLedger ledger, out Country realCountry);

            bool identical = string.Equals(withoutLedger, withLedger, StringComparison.Ordinal);
            if (!identical)
            {
                failures++;
                Debug.LogError("C-C10: the real game's state DIFFERS depending on whether the impact ledger ran beside it. "
                               + "An explanation layer that changes what it explains is worse than no explanation.");
            }

            sb.Append(identical
                ? F("    2. the real game   IDENTICAL over {0} turns with the ledger's worlds running beside it and without.\n", Turns)
                : "    2. the real game   ⚠ DIFFERS - see the error above.\n");

            // ---- 3. the identity ----
            string[] stats = { "GDP", "Inflation", "Unemployment", "ApprovalRating", "Budget", "GovernmentDebt" };
            float worstBreak = 0f;
            foreach (string stat in stats)
            {
                List<ImpactLine> lines = ledger.LinesFor(realCountry, stat, out float divergence);
                float sum = 0f;
                foreach (ImpactLine line in lines) { sum += line.Contribution; }
                worstBreak = Mathf.Max(worstBreak, Mathf.Abs(sum - divergence));
            }

            bool sums = worstBreak < 1e-3f;
            if (!sums)
            {
                failures++;
                Debug.LogError($"C-C10: the ledger's lines do not sum to the divergence - worst break {worstBreak:G6}. "
                               + "The interaction line exists precisely so this identity holds; if it does not, the lines are arithmetic fiction.");
            }

            sb.Append(sums
                ? F("    3. the identity    HOLDS - lines + interaction == divergence on all six graphed stats (worst break {0:G3}).\n", worstBreak)
                : "    3. the identity    ⚠ BROKEN - see the error above.\n");

            // ---- 4. the lazy fork is exact ----
            sb.Append(AssertLazyForkIsExact(ref failures));

            // ---- 5. the cost ----
            sb.Append(MeasureCost());

            ledger.Dispose();

            sb.Append(F("\n=== PolicyImpactLedgerDiagnostic: {0} ===\n",
                failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILURE(S)"));

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        /// <summary>A real game where the player leaves the dials alone for the first
        /// <see cref="TouchTaxesAtTurn"/> turns and then holds three families down for the rest.</summary>
        private static Dictionary<CountryId, PolicyDecision> DecisionsFor(World world, int turn)
        {
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

            if (turn < TouchTaxesAtTurn) { return decisions; }

            Country player = world.GetCountry(Player);
            var acting = new PolicyDecision();
            foreach (TaxLine line in player.TaxLines)
            {
                if (line.Type == TaxType.IncomeTax && line.IsImplemented) { acting.TaxRateOverrides[TaxType.IncomeTax] = line.Rate + 8f; }
            }

            acting.SpendingLineChanges[SpendingCategory.Defense] = 6f;
            acting.PoliceFundingOverride = Mathf.Min(100f, player.PoliceFundingLevel + 20f);
            decisions[Player] = acting;
            return decisions;
        }

        private static string RunReal(bool withLedger, out PolicyImpactLedger ledger, out Country realCountry)
        {
            SimulationRandom.Seed(Seed);
            var go = new GameObject("C-C10 REAL");
            SimulationManager sim = go.AddComponent<SimulationManager>();
            World world = WorldFactory.CreateDefault();
            sim.SetWorld(world);

            ShadowBaseline none = null;
            ledger = null;
            if (withLedger)
            {
                none = new ShadowBaseline(SimulationRandom.MasterSeed);
                ledger = new PolicyImpactLedger(none);
            }

            for (int t = 0; t < Turns; t++)
            {
                Dictionary<CountryId, PolicyDecision> decisions = DecisionsFor(world, t);

                // ⚠ BEFORE the real advance - a family first touched this turn must fork from the state
                // at the turn's START, which is what makes the lazy fork exact (assertion 4).
                ledger?.AdvanceTurn(sim, world, Player, decisions);

                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);
                none?.AdvanceTurn();
            }

            string fingerprint = Fingerprint(world);
            realCountry = world.GetCountry(Player);

            if (!withLedger) { UnityEngine.Object.DestroyImmediate(go); }

            return fingerprint;
        }

        /// <summary>⚠ The claim the cost model rests on. The ledger creates a family's except-world only
        /// when the player first touches that family, on the reasoning that before then it would have
        /// been identical to the real game. This runs the from-the-seed world the reasoning claims it
        /// equals, and compares.</summary>
        private static string AssertLazyForkIsExact(ref int failures)
        {
            // The from-the-seed world: every turn, the real decisions with Taxes stripped.
            SimulationRandom.Seed(Seed);
            var go = new GameObject("C-C10 FROM SEED");
            SimulationManager sim = go.AddComponent<SimulationManager>();
            World world = WorldFactory.CreateDefault();
            sim.SetWorld(world);

            for (int t = 0; t < Turns; t++)
            {
                Dictionary<CountryId, PolicyDecision> decisions = DecisionsFor(world, t);
                if (decisions.TryGetValue(Player, out PolicyDecision player)) { player.TaxRateOverrides.Clear(); }

                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);
            }

            string fromSeed = Fingerprint(world);
            UnityEngine.Object.DestroyImmediate(go);

            // The lazily forked world, taken out of a real run through the ledger's own path.
            SimulationRandom.Seed(Seed);
            var realGo = new GameObject("C-C10 LAZY");
            SimulationManager realSim = realGo.AddComponent<SimulationManager>();
            World realWorld = WorldFactory.CreateDefault();
            realSim.SetWorld(realWorld);

            var none = new ShadowBaseline(SimulationRandom.MasterSeed);
            var ledger = new PolicyImpactLedger(none);

            for (int t = 0; t < Turns; t++)
            {
                Dictionary<CountryId, PolicyDecision> decisions = DecisionsFor(realWorld, t);
                ledger.AdvanceTurn(realSim, realWorld, Player, decisions);
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { realSim.AdvanceDay(); }
                realSim.AdvanceTurn(decisions);
                none.AdvanceTurn();
            }

            FieldInfo exceptField = typeof(PolicyImpactLedger).GetField("_except", BindingFlags.Instance | BindingFlags.NonPublic);
            var except = (Dictionary<string, ShadowBaseline>)exceptField.GetValue(ledger);
            string lazy = except.TryGetValue("Taxes", out ShadowBaseline taxes) ? Fingerprint(taxes.World) : null;

            ledger.Dispose();
            none.Dispose();
            UnityEngine.Object.DestroyImmediate(realGo);

            if (lazy == null)
            {
                failures++;
                Debug.LogError("C-C10: the ledger never forked a Taxes world, so the lazy fork was not exercised at all.");
                return "    4. the lazy fork   ⚠ NOT EXERCISED - see the error above.\n";
            }

            bool exact = string.Equals(fromSeed, lazy, StringComparison.Ordinal);
            if (!exact)
            {
                failures++;
                Debug.LogError("C-C10: a family's except-world forked at first touch is NOT identical to one run from the seed with that "
                               + "family stripped throughout. The lazy fork is then an approximation, and every attribution built on it is "
                               + "off by an unknown amount.");
            }

            return exact
                ? F("    4. the lazy fork   EXACT - Taxes forked at turn {0} is byte-identical to the same world run from the seed.\n", TouchTaxesAtTurn)
                : "    4. the lazy fork   ⚠ NOT EXACT - see the error above.\n";
        }

        private static string MeasureCost()
        {
            SimulationRandom.Seed(Seed);
            var go = new GameObject("C-C10 COST");
            SimulationManager sim = go.AddComponent<SimulationManager>();
            World world = WorldFactory.CreateDefault();
            sim.SetWorld(world);

            var none = new ShadowBaseline(SimulationRandom.MasterSeed);
            var ledger = new PolicyImpactLedger(none);

            // Warm the families first, so the measured turns are steady-state rather than fork turns.
            for (int t = 0; t < TouchTaxesAtTurn + 2; t++)
            {
                Dictionary<CountryId, PolicyDecision> warm = DecisionsFor(world, t);
                ledger.AdvanceTurn(sim, world, Player, warm);
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(warm);
                none.AdvanceTurn();
            }

            var real = new Stopwatch();
            var explain = new Stopwatch();
            const int Measured = 5;
            for (int t = 0; t < Measured; t++)
            {
                Dictionary<CountryId, PolicyDecision> decisions = DecisionsFor(world, TouchTaxesAtTurn + 2 + t);

                explain.Start();
                ledger.AdvanceTurn(sim, world, Player, decisions);
                explain.Stop();

                real.Start();
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);
                real.Stop();

                explain.Start();
                none.AdvanceTurn();
                explain.Stop();
            }

            FieldInfo exceptField = typeof(PolicyImpactLedger).GetField("_except", BindingFlags.Instance | BindingFlags.NonPublic);
            int families = ((Dictionary<string, ShadowBaseline>)exceptField.GetValue(ledger)).Count;

            ledger.Dispose();
            none.Dispose();
            UnityEngine.Object.DestroyImmediate(go);

            double realMs = real.Elapsed.TotalMilliseconds / Measured;
            double explainMs = explain.Elapsed.TotalMilliseconds / Measured;

            return F("    5. the cost        real turn {0:F1} ms; the explanation layer {1:F1} ms on top ({2} except-world(s) + the no-policy shadow).\n",
                realMs, explainMs, families);
        }

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
