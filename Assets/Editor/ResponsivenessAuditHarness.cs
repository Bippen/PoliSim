using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-C11 (P-G3) — **the responsiveness audit. It measures and it proposes; it changes nothing.**
    ///
    /// <para>Elias's pre-ruling is the whole shape of this harness: <b>no constant moves. Where the
    /// literature disagrees, report the range. Where the model sits outside every sourced estimate, say so
    /// plainly — that is the finding.</b> Nothing here is fitted, nothing is tuned, and the harness has no
    /// code path that writes to a constant.</para>
    ///
    /// <para><b>The experiment.</b> From the Sweden baseline at seed 777, each major fiscal dial is
    /// stepped by a small and a large amount **once, as a permanent level shift**, and the trajectory
    /// compared with an otherwise identical no-policy run. One turn is one year (`DaysPerTurn` = 365), so the horizons are
    /// literally years — <b>L, L+1 and L+4, where L is the LANDING YEAR</b>: a decision handed to
    /// `AdvanceTurn` reaches the state the turn after, so a fixed "year 1" reads the model before the
    /// lever has moved, which is what the harness's own first run reported.</para>
    ///
    /// <para><b>The multiplier, defined so it can be argued with.</b> For every dial,
    /// <c>multiplier(t) = ΔGDP(t) / impulse</c>, where the impulse is the landing-year change in the
    /// budget balance caused by the dial, sign-corrected so that a positive multiplier always means
    /// "output moved the way a stimulus would move it". ⚠ For a TAX dial the impulse is the revenue change with spending
    /// untouched, so a tax RISE is a negative impulse; for a SPENDING dial it is the spending change with
    /// rates untouched. This is the standard "output response per unit of fiscal impulse" and is directly
    /// comparable with the literature quoted beside the table — it is not a bespoke index.</para>
    ///
    /// <para>⚠ <b>THREE QUANTITIES, ONE ENFORCED (D-9 route (b) → D-13, 2026-09-01).</b> The impulse above
    /// is <c>−Δ(budget balance)</c>: the spending change **net of the revenue that spending itself
    /// raised** (2.267 against 2.695 on the +2 % dial). **Ramey's 0.6–1.0 is not a band on that.** Read at
    /// the paper rather than recalled, it is stated over *"multipliers on general government PURCHASES"*
    /// and over a **cumulative** quantity — *"the present discounted value of the output response over
    /// time divided by the present discounted value of the government spending response over time"* —
    /// while <c>ΔGDP(h)/ΔG(impact)</c> is the one Ramey attributes to Blanchard and Perotti and says
    /// *"were not true dynamic multipliers"*. The second table therefore prints both of Ramey's
    /// quantities beside the enforced one:</para>
    ///
    /// <para><b>enforced (balance) 0.603 / 0.850 / 0.966 · quasi 0.507 / 0.715 / 0.807 ·
    /// CUMULATIVE 0.507 / 0.607 / 0.702.</b> ⚠ On the comparable column the model is **below the band at
    /// impact and inside it from L+1**, and that is true today with nothing pending.</para>
    ///
    /// <para>The constraint stays enforced on the basis it was pre-committed on: **swapping a denominator
    /// because a gate rejected a change is moving the bar to pass.** The other two are printed, never
    /// enforced, and the rule question is Elias's — register D-13.</para>
    ///
    /// <para>⚠ <b>AND THE SAME DEFECT ON THE TAX SIDE (R-D8, 2026-09-01).</b> Romer &amp; Romer normalise on
    /// an *"exogenous tax increase of 1 percent of GDP"* — the **statutory** change — which the quoted
    /// sentence in <see cref="Literature"/> has said all along; no column had ever been matched to it. On
    /// the statutory basis the model reads **0.335 / 0.471 / 0.525** against 0.485 / 0.682 / 0.760
    /// enforced. **The tax channel therefore undershoots the −2 to −3 band by a factor of four to six,
    /// not the three this record has been carrying.** Reported, not enforced, like the others.</para>
    ///
    /// <para>⚠ <b>The sourced values in <see cref="Literature"/> carry their citation and their vintage,
    /// and where a figure could not be read out of the source document it is marked as such rather than
    /// quoted.</b> The standing rule is that no figure is invented; a range nobody can check is an
    /// invented figure with a footnote.</para>
    /// </summary>
    public static class ResponsivenessAuditHarness
    {
        private const int Seed = 777;
        private const int Years = 6;
        private static readonly CountryId Subject = CountryId.Sweden;

        private enum Kind { Tax, Spending }

        private struct Dial
        {
            public string Name;
            public Kind Kind;
            public TaxType Tax;
            public float Step;
        }

        /// <summary>
        /// The sourced comparison values. **Every line names its study, its journal or institution, and
        /// its year**, and every one was checked against the source at C-C11 rather than recalled.
        /// </summary>
        private static readonly string[] Literature =
        {
            "SPENDING MULTIPLIER - Ramey, Journal of Economic Perspectives 33(2), Spring 2019, pp. 89-114:",
            "    the bulk of estimates for average spending multipliers lie in a narrow range of 0.6 to 1.0.",
            "    ⚠ READ AT THE PAPER 2026-09-01, not recalled, and it narrows what the band covers. Verbatim:",
            "    'For multipliers on general government PURCHASES ... The bulk of the estimates across the",
            "    leading methods of estimation and samples lie in a surprisingly narrow range of 0.6 to 1.'",
            "    And the quantity, verbatim: 'the present discounted value of the output response over time",
            "    divided by the present discounted value of the government spending response over time to",
            "    the shock', with 'a zero discount rate' giving 'nearly identical multipliers'. Ramey names",
            "    dGDP(h)/dG(impact) as Blanchard-Perotti's and says those 'were not true dynamic multipliers'.",
            "    ⚠ SO THE BAND IS A BAND ON THE CUMULATIVE COLUMN, not on the enforced one - see D-13.",
            "TAX - Romer & Romer, American Economic Review 100(3), June 2010, pp. 763-801:",
            "    an exogenous tax increase of 1% of GDP lowers real GDP by roughly 2 to 3 percent,",
            "    i.e. a tax multiplier of about -2 to -3 (their headline finding; 'much larger' than",
            "    estimates from broader tax measures).",
            "    ⚠ R-D8 (2026-09-01): the denominator was in this quoted sentence all along - 'an EXOGENOUS",
            "    tax increase of 1% of GDP' - and no column had ever been matched to it. On the statutory",
            "    basis the model reads 0.335 / 0.471 / 0.525, so the tax channel undershoots by a factor of",
            "    FOUR TO SIX rather than the three the record has been carrying.",
            "CRISIS-PERIOD SPENDING - Blanchard & Leigh, AER P&P 103(3), May 2013 / IMF WP 13/1:",
            "    multipliers were SUBSTANTIALLY HIGHER than the ~0.5 forecasters had assumed. ⚠ The often-",
            "    quoted 0.9-1.7 range could NOT be read out of the source document from here, so it is NOT",
            "    quoted as a number - only the direction the paper actually establishes.",
            "OKUN COEFFICIENT - Ball, Leigh & Loungani, IMF Working Paper 13/10, 2013, 'Okun's Law: Fit",
            "    at 50?': the estimated coefficients on the output gap vary across countries, with most",
            "    spread between -0.23 and -0.54; for the United States the 2009-2011 gap ratio was -0.41.",
            "⚠ NOT QUOTED: IMF TNM/14/04 (Batini, Eyraud, Forni & Weber, October 2014) is the standard",
            "    reference for country-specific multiplier bucketing, and Riksbank WP 365 (2019) carries a",
            "    Swedish estimate. Neither document could be read from here, so NO number from either is",
            "    used as an anchor. Both are named so the next session can fetch them rather than re-derive."
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var dials = new List<Dial>
            {
                new Dial { Name = "Income tax  +1pt", Kind = Kind.Tax, Tax = TaxType.IncomeTax, Step = 1f },
                new Dial { Name = "Income tax  +5pt", Kind = Kind.Tax, Tax = TaxType.IncomeTax, Step = 5f },
                new Dial { Name = "Income tax  -5pt", Kind = Kind.Tax, Tax = TaxType.IncomeTax, Step = -5f },
                new Dial { Name = "VAT         +1pt", Kind = Kind.Tax, Tax = TaxType.VAT, Step = 1f },
                new Dial { Name = "VAT         +5pt", Kind = Kind.Tax, Tax = TaxType.VAT, Step = 5f },
                new Dial { Name = "Corporate   +5pt", Kind = Kind.Tax, Tax = TaxType.CorporateTax, Step = 5f },
                new Dial { Name = "Spending    +2%", Kind = Kind.Spending, Step = 2f },
                new Dial { Name = "Spending   +10%", Kind = Kind.Spending, Step = 10f },
                new Dial { Name = "Spending   -10%", Kind = Kind.Spending, Step = -10f }
            };

            // R-D8 (2026-09-01): the subject's SEEDED rates and per-type base shares, read once off a
            // throwaway world so the statutory tax change below is derived from the model's own seed
            // rather than from a number written here. ⚠ Also PROVES the no-clamp assumption the
            // statutory figure rests on: Build() writes Max(0, rate + step), so if any seeded rate plus
            // its step went negative the applied change would not equal the step and the column would be
            // quietly wrong.
            var seededRate = new Dictionary<TaxType, float>();
            var baseShare = new Dictionary<TaxType, float>();
            {
                var probeGo = new GameObject("C-C11 SEED PROBE");
                try
                {
                    World probeWorld = WorldFactory.CreateDefault();
                    foreach (TaxLine line in probeWorld.GetCountry(Subject).TaxLines)
                    {
                        if (!line.IsImplemented) { continue; }
                        seededRate[line.Type] = line.Rate;
                        baseShare[line.Type] = line.BaseShareOfGdp;
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(probeGo);
                }
            }

            foreach (Dial d in dials)
            {
                if (d.Kind != Kind.Tax) { continue; }
                if (!seededRate.TryGetValue(d.Tax, out float seeded))
                {
                    Debug.LogError($"C-C11 (R-D8): tax dial '{d.Name}' names {d.Tax}, which the subject does not implement, "
                                   + "so no statutory tax change can be formed for it.");
                    continue;
                }

                if (seeded + d.Step < 0f)
                {
                    Debug.LogError($"C-C11 (R-D8): tax dial '{d.Name}' would clamp at zero ({seeded} + {d.Step}), so the "
                                   + "APPLIED rate change is not the step and the statutory column would misstate it.");
                }
            }

            float[] baseGdp = new float[Years + 1];
            float[] baseBudget = new float[Years + 1];
            float[] baseUnemployment = new float[Years + 1];
            float[] baseInflation = new float[Years + 1];
            float[] basePurchases = new float[Years + 1];
            RunCase(null, baseGdp, baseBudget, baseUnemployment, baseInflation, basePurchases);


            var sb = new StringBuilder();
            sb.Append("=== C-C11 (P-G3): the responsiveness audit - MEASURED, PROPOSED, NOTHING APPLIED ===\n");
            sb.Append(F("    Sweden, seed {0}, one turn = one year, each dial SET ONCE as a permanent level shift.\n", Seed));
            sb.Append(F("    Baseline GDP at year 1 / 2 / 6: {0:F1} / {1:F1} / {2:F1}\n\n", baseGdp[1], baseGdp[2], baseGdp[Years]));

            sb.Append("    dial                  impulse(L)      dGDP L     dGDP L+1     dGDP L+4  |   mult L  mult L+1  mult L+4  | dUnemp L+4 dInfl L+4 | Okun L+4\n");
            sb.Append("    ------------------------------------------------------------------------------------------------------------------------------\n");

            int deadDials = 0;
            var ramey = new StringBuilder();
            int rameyRows = 0;
            var statutoryTable = new StringBuilder();
            int statutoryRows = 0;
            foreach (Dial dial in dials)
            {
                float[] gdp = new float[Years + 1];
                float[] budget = new float[Years + 1];
                float[] unemployment = new float[Years + 1];
                float[] inflation = new float[Years + 1];
                float[] purchases = new float[Years + 1];
                RunCase(dial, gdp, budget, unemployment, inflation, purchases);

                // ⚠ THE LANDING YEAR, not year 1. The first run of this harness reported an impulse of
                // 0.00 for every dial and no multiplier at all - because a decision handed to
                // `AdvanceTurn` lands in the state the turn AFTER, so year 1 is always identical to the
                // baseline. Reading the impulse at a fixed year 1 measured the model before the lever
                // moved. The impulse is therefore taken at the first year the budget balance actually
                // responds, and the horizons run from there: impact, +1 and +4.
                int landing = 0;
                for (int y = 1; y <= Years; y++)
                {
                    if (Mathf.Abs(budget[y] - baseBudget[y]) > 1e-3f) { landing = y; break; }
                }

                if (landing == 0) { landing = 1; }

                float impulse = -(budget[landing] - baseBudget[landing]);

                int h2 = Mathf.Min(Years, landing + 1);
                int h5 = Mathf.Min(Years, landing + 4);
                float d1 = gdp[landing] - baseGdp[landing];
                float d2 = gdp[h2] - baseGdp[h2];
                float d5 = gdp[h5] - baseGdp[h5];

                bool impulseReal = Mathf.Abs(impulse) > 1e-3f;
                string m1 = impulseReal ? F("{0,8:F3}", d1 / impulse) : "       -";
                string m2 = impulseReal ? F("{0,8:F3}", d2 / impulse) : "       -";
                string m5 = impulseReal ? F("{0,8:F3}", d5 / impulse) : "       -";

                if (!impulseReal)
                {
                    deadDials++;
                    Debug.LogError($"C-C11: dial '{dial.Name}' moved the budget balance by nothing in year 1, so no multiplier can be "
                                   + "formed for it. Either the dial is inert or the experiment failed to pull it - and a table with an "
                                   + "unpulled lever in it would be measuring the wrong model.");
                }

                // D-9 route (b), 2026-09-01. The impulse above is -Δ(budget balance), which is the
                // spending change NET of every endogenous revenue response the spending change itself
                // caused. Ramey reviews ΔY/ΔG - output per unit of government PURCHASES - so the two
                // denominators are not the same quantity, and the gap is not small: measured here at
                // 2.267 against 2.695 on the +2% dial, because the extra output raises revenue. The
                // second basis is REPORTED, never enforced: the constraint stays on the basis it was
                // pre-committed on, and swapping a denominator because a gate rejected a change would be
                // moving the bar to pass. It is printed so the difference cannot go stale in a side
                // diagnostic nobody runs - the fifth coherence sweep's own lesson.
                if (dial.Kind == Kind.Spending)
                {
                    float purchaseImpulse = purchases[landing] - basePurchases[landing];
                    if (Mathf.Abs(purchaseImpulse) > 1e-3f)
                    {
                        rameyRows++;

                        // The CUMULATIVE (present-value) multiplier, which is the quantity Ramey's band
                        // summarises: the discounted sum of the output response divided by the discounted
                        // sum of the spending response, both taken from the landing year. Undiscounted,
                        // on Ramey's own note that "different interest rates used for this present
                        // discounted value - including the use of a zero discount rate - give nearly
                        // identical multipliers".
                        float cum1 = Cumulative(gdp, baseGdp, purchases, basePurchases, landing, landing);
                        float cum2 = Cumulative(gdp, baseGdp, purchases, basePurchases, landing, h2);
                        float cum5 = Cumulative(gdp, baseGdp, purchases, basePurchases, landing, h5);

                        ramey.Append(F("    {0,-18} {1,10:F3} {2,10:F3} | {3,7:F3} {4,7:F3} {5,7:F3} | {6,7:F3} {7,7:F3} {8,7:F3}\n",
                            dial.Name, impulse, purchaseImpulse,
                            d1 / purchaseImpulse, d2 / purchaseImpulse, d5 / purchaseImpulse,
                            cum1, cum2, cum5));
                    }
                    else
                    {
                        Debug.LogError($"C-C11: spending dial '{dial.Name}' moved discretionary spending by nothing at the "
                                       + "landing year, so no purchases-basis multiplier can be formed and that row would "
                                       + "have verified nothing.");
                    }
                }

                // R-D8 (2026-09-01), the same test D-13 applied to the spending side, applied to the tax
                // side. Romer & Romer's -2 to -3 is the output response to an EXOGENOUS tax increase of
                // 1% of GDP; the enforced impulse is the REALISED change in the budget balance, which
                // nets off the revenue the output change itself produced. The statutory change is the
                // mechanical one on unchanged output: baseGDP(L) x Δrate x BaseShareOfGdp, signed the way
                // this harness signs everything - a tax RISE is a negative impulse.
                if (dial.Kind == Kind.Tax && baseShare.TryGetValue(dial.Tax, out float share))
                {
                    float statutory = -(baseGdp[landing] * (dial.Step / 100f) * share);
                    if (Mathf.Abs(statutory) > 1e-3f)
                    {
                        statutoryRows++;
                        statutoryTable.Append(F("    {0,-18} {1,10:F3} {2,10:F3} | {3,7:F3} {4,7:F3} {5,7:F3}\n",
                            dial.Name, impulse, statutory,
                            d1 / statutory, d2 / statutory, d5 / statutory));
                    }
                }

                // The implied OKUN coefficient: the unemployment move per one percent of output, at the
                // longest horizon - directly comparable with Ball, Leigh & Loungani's country estimates
                // quoted below, and computed here rather than by hand off the table.
                float gdpPercent = baseGdp[h5] > 0f ? (d5 / baseGdp[h5]) * 100f : 0f;
                string okun = Mathf.Abs(gdpPercent) > 1e-4f
                    ? F("{0,8:F3}", (unemployment[h5] - baseUnemployment[h5]) / gdpPercent)
                    : "       -";

                sb.Append(F("    {0,-18} {1,12:F2} {2,12:F2} {3,12:F2} {4,12:F2}  | {5} {6} {7}  | {8,10:F3} {9,9:F3} | {10}\n",
                    dial.Name, impulse, d1, d2, d5, m1, m2, m5,
                    unemployment[h5] - baseUnemployment[h5], inflation[h5] - baseInflation[h5], okun));
            }

            sb.Append("\n    THE SPENDING DIALS ON RAMEY'S OWN QUANTITIES (D-9 route (b) / D-13; REPORTED, NOT ENFORCED)\n");
            sb.Append("    ------------------------------------------------------------------------------------------\n");
            sb.Append("    dial               impulse(bal) impulse(G) |  QUASI L   L+1     L+4 |   CUMULATIVE L   L+1     L+4\n");
            sb.Append(ramey);
            sb.Append(F("    Spending dials expressed on all three bases: {0} of {1}.\n", rameyRows, 3));
            sb.Append("    QUASI      = dGDP(h) / dG(landing). Ramey names this Blanchard-Perotti's quantity and says the\n");
            sb.Append("                 quantities they calculated 'were not true dynamic multipliers'.\n");
            sb.Append("    CUMULATIVE = sum dGDP / sum dG from the landing year, undiscounted. THIS is what Ramey's band\n");
            sb.Append("                 summarises: 'the present discounted value of the output response over time divided\n");
            sb.Append("                 by the present discounted value of the government spending response over time'.\n");
            sb.Append("    ⚠ THE FINDING (D-13, opened 2026-09-01 and sourced at the paper itself): the enforced column is\n");
            sb.Append("    NONE of these three. It divides by the change in the ACTUAL budget balance, which is the spending\n");
            sb.Append("    change NET of the revenue that spending raised - 2.267 against 2.695 on the +2% dial. Ramey's\n");
            sb.Append("    0.6-1.0 is stated over 'multipliers on general government PURCHASES', so the band and the column\n");
            sb.Append("    it is quoted beside are not the same quantity. The comparable column is CUMULATIVE.\n");
            sb.Append("    NOTHING IS ENFORCED ON THE NEW COLUMNS. Swapping a denominator because a gate rejected a change\n");
            sb.Append("    would be moving the bar to pass; measuring it and printing it beside the one in force is evidence.\n");
            sb.Append("    WHICH BASIS THE CONSTRAINT IS ENFORCED ON IS A CHANGE TO THE RULE, AND THE RULE IS ELIAS'S.\n");

            sb.Append("\n    THE TAX DIALS ON ROMER & ROMER'S DENOMINATOR (R-D8; REPORTED, NOT ENFORCED)\n");
            sb.Append("    --------------------------------------------------------------------------\n");
            sb.Append("    dial               impulse(bal) statutory |  mult L   L+1     L+4\n");
            sb.Append(statutoryTable);
            sb.Append(F("    Tax dials expressed on both bases: {0} of {1}.\n", statutoryRows, 6));
            sb.Append("    statutory = baseGDP(L) x d(rate) x BaseShareOfGdp - the mechanical revenue change on unchanged\n");
            sb.Append("                output, which is the EXOGENOUS change Romer & Romer normalise on. The enforced\n");
            sb.Append("                impulse is the REALISED balance change, net of the revenue the output move produced.\n");
            sb.Append("    Point ratios, not cumulative: Romer & Romer report the output LEVEL response to a permanent\n");
            sb.Append("    change, which is what a point ratio at a horizon is - the cumulative form is the spending side's.\n");
            sb.Append("    NOTHING IS ENFORCED ON THIS COLUMN EITHER. It exists because D-13 found the spending band and\n");
            sb.Append("    the spending column were different quantities, and the tax side had never been checked.\n");

            sb.Append("\n    THE SOURCED COMPARISON\n    ----------------------\n");
            foreach (string line in Literature) { sb.Append("    ").Append(line).Append('\n'); }

            sb.Append("\n    ⚠ NO CONSTANT WAS MOVED BY THIS HARNESS, AND IT HAS NO CODE PATH THAT COULD MOVE ONE.\n");
            sb.Append("    The recommendation list lives in COMPLETED.md, each line strikeable, and waits on Elias.\n");

            if (deadDials > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>One run of <see cref="Years"/> years with a single dial held, or the untouched
        /// baseline when <paramref name="dial"/> is null. ⚠ Tax targets are read off the country's own
        /// seeded rate and stepped, so no rate here is an authored number.</summary>
        private static void RunCase(Dial? dial, float[] gdp, float[] budget, float[] unemployment, float[] inflation, float[] purchases)
        {
            SimulationRandom.Seed(Seed);
            var go = new GameObject("C-C11 CASE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                World world = WorldFactory.CreateDefault();
                sim.SetWorld(world);

                Country subject = world.GetCountry(Subject);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                if (dial.HasValue) { decisions[Subject] = Build(subject, dial.Value); }

                // ⚠ THE DIAL IS SET ONCE, NOT HELD EVERY YEAR, and the difference is the whole validity of
                // the table. A tax override is an ABSOLUTE target: setting it once writes `TaxLine.Rate`
                // and it persists. A spending change is a PERCENTAGE of the line's own current amount:
                // re-sending it every year raises the level by that percentage AGAIN each year, so the
                // impulse grows exponentially while the harness divides by the landing year's. Held that
                // way the first run of this table showed spending multipliers climbing 0.603 -> 1.5 -> 5.3,
                // which would have been the harness's compounding read as the model's dynamics. Set once,
                // both kinds of dial are the same experiment: one permanent level shift.
                var once = new Dictionary<CountryId, PolicyDecision>(decisions);
                var nothing = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { nothing[c.Id] = PolicyDecision.None(); }

                for (int year = 1; year <= Years; year++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(year == 1 ? once : nothing);

                    gdp[year] = subject.State.GDP;
                    budget[year] = subject.State.Budget;
                    unemployment[year] = subject.State.Unemployment;
                    inflation[year] = subject.State.Inflation;

                    // G as the national accounts identity spends it: the DISCRETIONARY lines' sum, which
                    // is what SpendingLine's own doc names as G for a country with a detailed portfolio.
                    float discretionary = 0f;
                    foreach (SpendingLine line in subject.SpendingLines)
                    {
                        if (!line.IsMandatory) { discretionary += line.Amount; }
                    }

                    purchases[year] = discretionary;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static PolicyDecision Build(Country subject, Dial dial)
        {
            var decision = new PolicyDecision();

            if (dial.Kind == Kind.Tax)
            {
                foreach (TaxLine line in subject.TaxLines)
                {
                    if (line.Type == dial.Tax && line.IsImplemented)
                    {
                        decision.TaxRateOverrides[dial.Tax] = Mathf.Max(0f, line.Rate + dial.Step);
                    }
                }

                return decision;
            }

            // Every DISCRETIONARY line moved by the same percentage - a portfolio-wide fiscal impulse
            // rather than one category's, so the measured multiplier is the government's, not Defense's.
            foreach (SpendingLine line in subject.SpendingLines)
            {
                if (!line.IsMandatory) { decision.SpendingLineChanges[line.Category] = dial.Step; }
            }

            return decision;
        }

        /// <summary>The cumulative (present-value, zero-discount) multiplier between the landing year and
        /// <paramref name="horizon"/> inclusive: the summed output response over the summed spending
        /// response. Returns 0 when the summed spending response is nothing, which the caller has already
        /// excluded by testing the landing-year impulse.</summary>
        private static float Cumulative(float[] gdp, float[] baseGdp, float[] purchases, float[] basePurchases, int landing, int horizon)
        {
            float outputSum = 0f;
            float spendingSum = 0f;
            for (int y = landing; y <= horizon; y++)
            {
                outputSum += gdp[y] - baseGdp[y];
                spendingSum += purchases[y] - basePurchases[y];
            }

            return Mathf.Abs(spendingSum) > 1e-6f ? outputSum / spendingSum : 0f;
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
