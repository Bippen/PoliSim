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
    /// C-N4 — **the tax transmission gap: WHERE the impulse is lost, measured before anything is
    /// proposed and long before anything is changed.**
    ///
    /// <para><b>The ruling this runs under</b> (Elias, 2026-08-31): a FIX TRACK, not a calibration, and
    /// **ruling-first — measure where the impulse is lost before changing anything; propose with the
    /// sourced basis attached; apply nothing.** ⚠ **Nothing in this file writes a constant.**</para>
    ///
    /// <para><b>What is already measured</b> (`COMPLETED.md` §107, C-C11): three tax types, three step
    /// sizes, both directions, three horizons — every implied output multiplier is <b>exactly 0.000</b>
    /// while each produces a real revenue impulse. This harness answers the next question: the money
    /// leaves households and arrives in the budget, so **at which term does it stop?**</para>
    ///
    /// <para><b>The method: follow the impulse field by field.</b> One tax rise on one country, and every
    /// public float of `EconomyState` sorted into <b>MOVED</b> and <b>UNMOVED</b>. A transmission gap is
    /// not an opinion about a formula; it is a list of the things that did not move.</para>
    /// </summary>
    public static class TaxTransmissionDiagnostic
    {
        private const int Seed = 777;
        private const int Turns = 6;
        private static readonly CountryId Subject = CountryId.Sweden;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-N4: where does a tax rise stop? MEASURED, PROPOSED, NOTHING APPLIED ===\n");

            Dictionary<string, float> baseline = RunCase(false);
            Dictionary<string, float> taxed = RunCase(true);

            var moved = new List<string>();
            var unmoved = new List<string>();
            foreach (KeyValuePair<string, float> field in baseline)
            {
                float after = taxed[field.Key];
                if (Mathf.Approximately(after, field.Value)) { unmoved.Add(field.Key); }
                else { moved.Add(F("{0} {1:F4} -> {2:F4}", field.Key, field.Value, after)); }
            }

            sb.Append(F("\n    Sweden, seed {0}, {1} turns, income tax +10 points held from turn 1.\n\n", Seed, Turns));
            sb.Append("    MOVED\n");
            foreach (string line in moved) { sb.Append("        ").Append(line).Append('\n'); }

            sb.Append("\n    UNMOVED (every one of these is a term the impulse never reached)\n        ");
            sb.Append(string.Join(", ", unmoved)).Append('\n');

            // ⚠ THE ASSERTION THAT BINDS THE FINDING TO ITS CALL SITE (C-C2's precedent). The claim is not
            // "the multiplier is zero" - C-C11 measured that. The claim is that CONSUMPTION is where it
            // stops, and the way to bind it is to require Consumption to be in the UNMOVED list. If a
            // later change gives tax a consumption channel, THIS fails, and the finding is retired by the
            // guard rather than by someone remembering to.
            // ⚠ THE ASSERTION FLIPPED WHEN THE CHANNEL WAS BUILT, WHICH IS THE DESIGN WORKING.
            // Before C-N4's build this required Consumption to be UNMOVED - the finding it was reporting.
            // The build made it move, the assertion fired, and it was rewritten to guard the channel from
            // the other side: Consumption AND GDP must BOTH move, so a regression that severs the
            // transmission fails here instead of quietly restoring a zero tax multiplier.
            int failures = 0;
            bool consumptionMoved = !unmoved.Contains("Consumption");
            bool gdpMoved = !unmoved.Contains("GDP");

            if (!consumptionMoved)
            {
                failures++;
                Debug.LogError("C-N4: Consumption did NOT move under a tax rise. The disposable-income term is severed - "
                               + "the money leaves households and the C term does not learn of it, which is the exact defect "
                               + "this item was opened to fix.");
            }

            if (!gdpMoved)
            {
                failures++;
                Debug.LogError("C-N4: Consumption moved and GDP did NOT. ⚠ That is the SECOND loss point this item found: "
                               + "the daily path builds GDP from an analytic fixed point where C enters as a share COEFFICIENT "
                               + "and only G, NX and potential enter as LEVELS, so a delta written into state.Consumption alone "
                               + "is cosmetic. The delta must sit in `attractorTerm` beside G and NX.");
            }


            // ⚠ "EXPLAINED PER COUNTRY" - and it can be arithmetic rather than six more simulation runs,
            // because the term is a closed form: -MPC x (household burden share change) x GDP. Six rows of
            // what a +10-point income-tax rise does to each country's C term, from that country's OWN
            // seeded portfolio, so the differences are the seeds' and not the formula's.
            sb.Append("\n    THE TERM, PER COUNTRY - a +10-point income-tax rise, from each country's own seeded portfolio\n");
            sb.Append("    country   income tax   household burden   d burden      GDP        dC        dC as % of GDP\n");
            sb.Append("    ------------------------------------------------------------------------------------------\n");

            SimulationRandom.Seed(Seed);
            World probe = WorldFactory.CreateDefault();
            var responsesOfTheSourced = new List<string>();   // D-16 step 4: dC as a share of GDP, per sourced country, to three decimals
            foreach (Country c in probe.Countries)
            {
                float gdp = c.State.GDP;
                float burden = 0f, incomeRate = 0f, incomeShare = 0f;
                foreach (TaxLine line in c.TaxLines)
                {
                    if (!line.IsImplemented) { continue; }
                    if (line.Type == TaxType.CorporateTax || line.Type == TaxType.Tariffs) { continue; }
                    burden += line.Rate / 100f * TaxBaseTable.BaseShareOfGdp(c.Id, line.Type);
                    if (line.Type == TaxType.IncomeTax) { incomeRate = line.Rate; incomeShare = TaxBaseTable.BaseShareOfGdp(c.Id, line.Type); }
                }

                float dBurden = 0.10f * incomeShare;
                float dC = -0.67f * dBurden * gdp;
                sb.Append(F("    {0,-9} {1,10:F1} {2,17:P2} {3,10:P2} {4,9:F0} {5,9:F2} {6,15:P2}{7}\n",
                    c.Id, incomeRate, burden, dBurden, gdp, dC, gdp > 0 ? dC / gdp : 0,
                    TaxBaseTable.IsSourced(c.Id) ? "" : "   (the uniform stand-in - F-B)"));
                if (TaxBaseTable.IsSourced(c.Id)) { responsesOfTheSourced.Add(F("{0:P3}", gdp > 0 ? dC / gdp : 0)); }
            }

            // D-16 (a), 2026-09-04 (its step 4): the response MUST differ per country now that the base is the
            // country's own. Before the table this column read -2.68 % for all six, which was the finding; a run
            // where the five sourced countries still answer with one figure means the table is not being read.
            var distinctResponses = new HashSet<string>(responsesOfTheSourced);
            if (responsesOfTheSourced.Count >= 2 && distinctResponses.Count < 2)
            {
                failures++;
                Debug.LogError("D-16: the five sourced countries answer a +10-point income-tax rise with ONE figure (dC as a share "
                               + "of GDP) - the per-country base is not reaching the burden term, and the point of the item is lost.");
            }
            sb.Append(F("    The last column is the item's point (D-16 (a), COMPLETED.md §282): {0} distinct response(s) among the {1}\n", distinctResponses.Count, responsesOfTheSourced.Count));
            sb.Append("    sourced countries. Before the table it read one figure for all six - `TaxLine.BaseShareOfGdp` was a\n");
            sb.Append("    per-TAX-TYPE constant, so an income-tax POINT moved every country's consumption by the same share of\n");
            sb.Append("    its own GDP. Now each of the five pays its own base's worth (TaxBaseTable, OECD/Eurostat 2022 over the\n");
            sb.Append("    seeded rate); the USA stays on the uniform stand-in for F-B's perimeter reason and still reads -2.68 %.\n");
            sb.Append(F("\n    THE CHANNEL: {0}\n", consumptionMoved && gdpMoved
                ? "LIVE. The tax rise reaches Consumption AND GDP. Built at C-N4; this harness now guards it."
                : "⚠ SEVERED - see the errors above."));
            sb.Append("    ⚠ TWO loss points were found, and the second only because the first fix was measured rather than\n");
            sb.Append("    trusted:\n");
            sb.Append("      1. `ApplyNationalAccounts` had NO DISPOSABLE-INCOME TERM - consumption was a fixed share of prior\n");
            sb.Append("         GDP times interest and confidence, so a tax rise took money from households and the C term\n");
            sb.Append("         never learned of it, while G entered the identity directly.\n");
            sb.Append("      2. ⚠ The DAILY path does not build GDP from `state.Consumption` at all. It solves an analytic\n");
            sb.Append("         fixed point where C and I enter as SHARE COEFFICIENTS and only G, NX and potential enter as\n");
            sb.Append("         LEVELS. Writing the delta into `state.Consumption` alone moved the reported stat and left GDP\n");
            sb.Append("         untouched - a cosmetic fix. The delta belongs in `attractorTerm`, beside G and NX.\n");
            sb.Append("    That second point is also the reason the SPENDING multiplier always worked: G was already a level\n");
            sb.Append("    in that line, and nothing households did ever reached it.\n");

            sb.Append("\n    THE MAGNITUDE - SOURCED, with its stretch stated\n");
            sb.Append("    Johnson, Parker & Souleles, American Economic Review 96(5), December 2006, pp. 1589-1610:\n");
            sb.Append("    households spent 20-40% of the 2001 rebates on nondurables in the quarter of arrival and ROUGHLY\n");
            sb.Append("    TWO-THIRDS cumulatively over that quarter and the next. A turn here is a YEAR, so the cumulative\n");
            sb.Append("    figure is the one that matches the period; 0.67 is the model's MPC.\n");
            sb.Append("    ⚠ Three limits, on the record rather than buried: it is a US estimate and NO Swedish or euro-area\n");
            sb.Append("    anchor was readable (that one is BILLED); it measures a TRANSITORY rebate, where a permanent rate\n");
            sb.Append("    change plausibly has a HIGHER propensity, so this is if anything conservative; and the source gives\n");
            sb.Append("    a RANGE, which is recorded so a later pass can argue with the choice rather than rediscover it.\n");
            sb.Append("    ⚠ Romer & Romer's -2 to -3 is NOT a target and is NOT transplanted. It is a US narrative-shock\n");
            sb.Append("    estimate of an OUTCOME; this item sourced an INPUT and reports the outcome that follows.\n");

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private static Dictionary<string, float> RunCase(bool raiseTax)
        {
            SimulationRandom.Seed(Seed);
            var go = new GameObject("C-N4 CASE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                World world = WorldFactory.CreateDefault();
                sim.SetWorld(world);

                Country subject = world.GetCountry(Subject);
                var none = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { none[c.Id] = PolicyDecision.None(); }

                var acting = new Dictionary<CountryId, PolicyDecision>(none);
                if (raiseTax)
                {
                    var decision = new PolicyDecision();
                    foreach (TaxLine line in subject.TaxLines)
                    {
                        if (line.Type == TaxType.IncomeTax && line.IsImplemented)
                        {
                            decision.TaxRateOverrides[TaxType.IncomeTax] = line.Rate + 10f;
                        }
                    }

                    acting[Subject] = decision;
                }

                for (int t = 0; t < Turns; t++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(t == 0 ? acting : none);
                }

                var state = new Dictionary<string, float>();
                foreach (FieldInfo f in typeof(EconomyState).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (f.FieldType == typeof(float)) { state[f.Name] = (float)f.GetValue(subject.State); }
                }

                return state;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
