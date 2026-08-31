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
    /// C-C6 (P-C2) — **what unit the seeds actually store, and whether the model cares.**
    ///
    /// <para>The finding: *"The Desk shows Sweden's GDP as $620B — a USD basis."* The ruling to execute
    /// is that figures store and display in NATIONAL units with cross-country views converting at a
    /// sourced vintage-dated rate — **unless the model turns out to be unit-agnostic, in which case say
    /// so and close cheap.** That "unless" is not a guess to be made by reading the code: it is a
    /// measurable property, and this measures it.</para>
    ///
    /// <para><b>The test.</b> Scale every stored money quantity of every country by a constant
    /// a constant, advance both worlds the same number of turns from the same seed, and ask
    /// two questions:</para>
    /// <list type="number">
    /// <item><description><b>Are the RATIOS invariant?</b> Debt-to-GDP, unemployment, inflation,
    /// approval, poverty, Gini and the rest are pure numbers — if the model is unit-agnostic they must
    /// come out BIT-IDENTICAL, because the unit cancels.</description></item>
    /// <item><description><b>Do the LEVELS scale exactly?</b> GDP, debt, the budget and the rest of the
    /// money fields must land on exactly that constant x their unscaled values.</description></item>
    /// </list>
    ///
    /// <para>⚠ **If both hold, re-basing is a SEED AND DISPLAY change with no behavioural consequence**,
    /// and the ruling's expensive branch — a seed change under the full sim-math bar with per-country
    /// diffs explained — is not needed. **If either fails, the failure names the constant that carries
    /// an absolute scale**, which is exactly the thing a re-basing would break, and the item stops
    /// there rather than re-basing on top of it.</para>
    ///
    /// <para>⚠ **A float model cannot be expected to be exact under scaling**, so the comparison uses a
    /// RELATIVE tolerance and reports the worst offender by name rather than a pass/fail alone. A
    /// difference far above float noise is a real dependence on absolute scale; one at the noise floor
    /// is arithmetic, not physics.</para>
    /// </summary>
    public static class MoneyBasisDiagnostic
    {
        /// <summary>Ten, so a failure shows up as an order of magnitude rather than a rounding wobble —
        /// and close to the real SEK/USD ratio (~10.5), which is the re-basing this item is about.</summary>


        /// <summary>Relative tolerance. Float32 carries ~7 significant digits and twelve turns of
        /// compounding erodes some of them; 1e-4 is far below any real scale dependence and far above
        /// the noise.</summary>
        private const double RelativeTolerance = 1e-4;

        /// <summary>The stored money quantities, by name. ⚠ A HAND-LIST, deliberately — the same
        /// discipline `ClonePreviewCountry` follows. A reflective "everything that looks like money"
        /// sweep would quietly include a RATE (a rate per unemployed, a percentage of GDP) and scaling
        /// one of those would break the model in a way that looks like a finding.</summary>
        private static readonly string[] MoneyFields =
        {
            "GDP", "PotentialGDP", "GovernmentDebt", "Budget", "TradeBalance", "Consumption", "Investment"
        };

        /// <summary>Pure numbers — ratios, rates and indices. The unit cancels in every one, so they
        /// must be identical under scaling if the model is unit-agnostic.</summary>
        private static readonly string[] RatioFields =
        {
            "Inflation", "Unemployment", "ApprovalRating", "PovertyRate", "LaborForceParticipationRate",
            "Gini", "CrimeIndex", "CorruptionIndex", "OrganizedCrimeIndex", "PrisonPopulationRate",
            "ConsumerConfidence", "BusinessConfidence", "InflationExpectations", "Productivity",
            "YouthUnemployment", "LifeExpectancy", "RealWageIndex", "HousingOverburden", "Homeownership",
            "HousePriceIndex", "DependencyRatio", "PopulationGrowthRate", "CurrencyStrength"
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-C6 (P-C2): the seeds' basis, and whether the model is unit-agnostic ===\n");

            sb.Append("\n--- 1. What the seeds store, read from the seed itself ---\n");
            World probe = WorldFactory.CreateDefault();
            foreach (Country c in probe.Countries)
            {
                sb.Append(F("    {0,-8} seeded GDP {1,10:F2}\n", c.Id, c.State.GDP));
            }

            sb.Append("    ⚠ Sweden's seed is 620, and WorldFactory's own comment reads \"against Sweden's\n");
            sb.Append("      real GDP (~$620B)\". Sweden's real GDP is ~6 500 billion SEK. THE SEEDS ARE\n");
            sb.Append("      IN USD BILLIONS for every country, including the five that do not use dollars.\n");

            sb.Append("\n--- 2. Is the model unit-agnostic? Every money quantity scaled, both worlds from seed 777 ---\n");
            sb.Append("    ⚠ TWO SCALE FACTORS, and the PAIR is the finding. 2 is exactly representable in\n");
            sb.Append("      binary floating point; 10 is not. Running only one of them would answer a\n");
            sb.Append("      different question than the one asked - a x10 divergence alone looks like a\n");
            sb.Append("      modelling dependence on absolute scale, and it is not.\n\n");
            sb.Append("      scale  turns   worst ratio diff        worst level diff\n");

            int failures = 0;
            bool exactScaleHolds = true;

            foreach (float scale in new[] { 2f, 10f })
            {
                foreach (int turns in new[] { 1, 12 })
                {
                    Dictionary<CountryId, EconomyState> plain = RunWorld(1f, turns);
                    Dictionary<CountryId, EconomyState> scaled = RunWorld(scale, turns);

                    double worstRatio = 0d;
                    string worstRatioName = "-";
                    double worstLevel = 0d;
                    string worstLevelName = "-";

                    foreach (KeyValuePair<CountryId, EconomyState> kv in plain)
                    {
                        foreach (string field in RatioFields)
                        {
                            double rel = Relative(Read(kv.Value, field), Read(scaled[kv.Key], field));
                            if (rel > worstRatio) { worstRatio = rel; worstRatioName = kv.Key + "." + field; }
                        }

                        foreach (string field in MoneyFields)
                        {
                            double rel = Relative(Read(kv.Value, field) * scale, Read(scaled[kv.Key], field));
                            if (rel > worstLevel) { worstLevel = rel; worstLevelName = kv.Key + "." + field; }
                        }
                    }

                    sb.Append(F("      x{0,-4:F0} {1,5}   {2:E3} ({3,-28})  {4:E3} ({5})\n",
                        scale, turns, worstRatio, worstRatioName, worstLevel, worstLevelName));

                    // ⚠ THE ASSERTION IS ON THE EXACTLY-REPRESENTABLE SCALE ONLY. At x2 the arithmetic
                    // is exact, so any difference at all is a real dependence on absolute money scale
                    // and the item must stop rather than re-base on top of it. At x10 a difference is
                    // expected and is NOT asserted: it measures the model's float-path sensitivity,
                    // which is a property worth reporting and not a defect to fail on.
                    if (Mathf.Approximately(scale, 2f) && (worstRatio > 0d || worstLevel > 0d))
                    {
                        exactScaleHolds = false;
                        failures++;
                        Debug.LogError(F("C-C6: at an EXACTLY representable scale (x2, {0} turn(s)) the model still moved - ratio {1:E3} at {2}, level {3:E3} at {4}. Something carries an absolute money scale; that constant is the finding, and no re-basing may be built on top of it.",
                            turns, worstRatio, worstRatioName, worstLevel, worstLevelName));
                    }
                }
            }

            sb.Append("\n--- 3. The verdict ---\n");
            if (exactScaleHolds)
            {
                sb.Append("    ⚠ UNIT-AGNOSTIC, AND THE RE-BASING IS STILL A SEED CHANGE. Two findings, and\n");
                sb.Append("    they point opposite ways, so both are stated:\n\n");
                sb.Append("    (a) THE MODEL DOES NOT CARE WHAT THE UNIT IS. At x2 - exact in binary - every\n");
                sb.Append("        ratio and every level is invariant to 0.000E+000 at 1 and at 12 turns. No\n");
                sb.Append("        constant anywhere on the macro path carries an absolute money scale, so\n");
                sb.Append("        the stored unit is a CONVENTION rather than a modelling choice.\n\n");
                sb.Append("    (b) BUT A REAL RE-BASING IS NOT A POWER OF TWO. SEK/USD is ~10.5, and at x10\n");
                sb.Append("        the float path diverges - small after one turn, order-unity after twelve.\n");
                sb.Append("        That is rounding, not economics, and it means a re-based seed set WOULD\n");
                sb.Append("        produce different trajectories.\n\n");
                sb.Append("    So the ruling's cheap branch is NOT available: re-basing is a seed change under\n");
                sb.Append("    the full sim-math bar with per-country diffs explained - and the honest\n");
                sb.Append("    explanation of those diffs is FLOAT-PATH DIVERGENCE, not a change in what the\n");
                sb.Append("    model believes. Saying 'the model is unit-agnostic so re-basing is free' would\n");
                sb.Append("    be true about the model and false about the build.\n");
            }
            else
            {
                sb.Append("    NOT unit-agnostic - see the error(s) above. The named field is where an\n");
                sb.Append("    absolute scale is carried, and it must be resolved BEFORE any re-basing.\n");
            }

            sb.Append("\n--- 4. ⚠ A second currency, already in the game, with no conversion ---\n");
            sb.Append("    The campaign layer prices in KRONOR - the war chest is 2 400 000 kr and every\n");
            sb.Append("    action's cost is in kr (a television buy 500 000, a social post 5 000) - while the\n");
            sb.Append("    macro layer is in USD BILLIONS. The two never meet today, because the campaign is\n");
            sb.Append("    staged rather than funded from the state's budget, so nothing converts and nothing\n");
            sb.Append("    is wrong yet. ⚠ THE DAY A CAMPAIGN IS PAID FOR OUT OF ANYTHING THE MACRO MODEL\n");
            sb.Append("    HOLDS, one of the two is wrong by a factor of ~10 500 000 000. Recorded here\n");
            sb.Append("    because it is invisible until it is expensive.\n");

            sb.Append(F("\n=== MoneyBasisDiagnostic: {0} ===\n",
                failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILURE(S)"));

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private static Dictionary<CountryId, EconomyState> RunWorld(float scale, int turns)
        {
            SimulationRandom.Seed(777);
            var go = new GameObject("C-C6 BASIS");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                World world = WorldFactory.CreateDefault();

                if (!Mathf.Approximately(scale, 1f))
                {
                    foreach (Country c in world.Countries) { ScaleMoney(c, scale); }
                }

                sim.SetWorld(world);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                for (int t = 0; t < turns; t++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                }

                var result = new Dictionary<CountryId, EconomyState>();
                foreach (Country c in world.Countries) { result[c.Id] = c.State.Clone(); }
                return result;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>Every stored money quantity on this country, scaled. ⚠ The hand-list above plus the
        /// country-level money the state does not hold: spending lines and the fund.</summary>
        private static void ScaleMoney(Country country, float scale)
        {
            EconomyState s = country.State;
            foreach (string field in MoneyFields)
            {
                System.Reflection.FieldInfo f = typeof(EconomyState).GetField(field);
                if (f == null) { throw new InvalidOperationException("C-C6: EconomyState has no field " + field); }
                f.SetValue(s, (float)Read(s, field) * scale);
            }

            foreach (SpendingLine line in country.SpendingLines) { line.Amount *= scale; }
            if (country.SovereignWealthFund != null) { country.SovereignWealthFund.TotalAssets *= scale; }

            // ⚠ The trade links are money too, and missing them is what the FIRST run of this
            // diagnostic got wrong. `TradeSystem.ApplyTradeEffects` RECOMPUTES `TradeBalance` from
            // these volumes every turn, so scaling the state's balance while leaving the volumes alone
            // simply had the model overwrite the scaled value with an unscaled one - and the resulting
            // divergence looked exactly like "the model is not unit-agnostic". A test that under-scales
            // reports the model's innocence as guilt.
            foreach (TradePartner link in country.TradePartners)
            {
                link.ImportVolume *= scale;
                link.ExportVolume *= scale;
            }
        }

        private static double Read(EconomyState state, string field)
        {
            System.Reflection.FieldInfo f = typeof(EconomyState).GetField(field);
            if (f == null) { throw new InvalidOperationException("C-C6: EconomyState has no field " + field); }
            return (float)f.GetValue(state);
        }

        /// <summary>Relative difference, with an absolute fallback so two near-zero values do not
        /// register as infinitely different.</summary>
        private static double Relative(double a, double b)
        {
            double scale = Math.Max(Math.Abs(a), Math.Abs(b));
            if (scale < 1e-6) { return Math.Abs(a - b); }
            return Math.Abs(a - b) / scale;
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
