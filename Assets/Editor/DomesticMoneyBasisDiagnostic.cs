using System;
using System.Collections;
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
    /// P2-0.1 (Playtest 2, 2026-09-02) — **every money figure on Statistics › Domestic traced to its
    /// basis, and the one seam where a figure authored on one country's scale reached another's.**
    ///
    /// <para><b>The finding as Elias sent it:</b> a deficit figure on the Domestic sheet looked as if a
    /// national-unit (SEK) line had been mixed into a USD total, the likeliest seam being the budget
    /// decomposition's sourced lines against the USD seed. <b>Measure first.</b> This diagnostic is the
    /// measurement; it ran red before the fix and the numbers it printed are the record.</para>
    ///
    /// <para><b>What it enumerates.</b> For each of the six countries: (1) the seed — GDP, the debt stock
    /// and the ratio identity, GDP per capita's identity, and every seeded spending line as a share of
    /// GDP (a SEK figure applied to a $-basis economy would be a multiple of it, not a share of it);
    /// (2) after two closed years — the fiscal report's revenue, spending and balance as shares of GDP,
    /// with the sign convention the sheet prints, and the cumulative accumulator beside the year's
    /// balance (P2-0.4's fact, printed here and asserted there); (3) <b>the one-time settlements</b> —
    /// every option in the cabinet decision pool and the foreign-policy meeting pool, its authored
    /// figure, and what it lands as on each country's economy. ⚠ <b>This is where the basis broke:</b>
    /// the pools are authored in billions on the USA seed's scale and were applied unscaled, so the same
    /// option was a fraction of a percent of the USA's GDP and a third of Sweden's (the exact shares are
    /// printed by every run, before and after the seam). The fix is at the seam
    /// (<see cref="AuthoredImpactScale"/>), never at the display, and this diagnostic asserts the applied
    /// figure against a ceiling on every country.</para>
    ///
    /// <para><b>Directions (S-37).</b> Every bound below is a ceiling on a share of GDP; growth above it
    /// fails. None is a target.</para>
    /// </summary>
    public static class DomesticMoneyBasisDiagnostic
    {
        /// <summary>No single seeded spending line above this share of GDP. A ceiling, not a target: the
        /// largest real line (Sweden's pension top-up plus UO11, or the USA's Social Security) sits well
        /// under it, and a national-unit figure on a $-basis economy would be several multiples of it.</summary>
        private const float MaxSeededLineShareOfGdp = 30f;
        /// <summary>The seeded portfolio's sum as a share of GDP, a ceiling.</summary>
        private const float MaxSeededPortfolioShareOfGdp = 70f;
        /// <summary>The closed year's revenue and spending as shares of GDP, floors and ceilings. Wide on
        /// purpose - these catch a basis error (a figure off by the SEK/USD ratio), not a calibration.</summary>
        private const float MinFlowShareOfGdp = 10f;
        private const float MaxFlowShareOfGdp = 70f;
        /// <summary>The closed year's deficit or surplus as a share of GDP, a ceiling on the magnitude.</summary>
        private const float MaxBalanceShareOfGdp = 15f;
        /// <summary>An authored one-time settlement, as applied, above this share of the deciding country's
        /// GDP is a basis defect. The ceiling sits above the largest authored option's share of the USA seed's
        /// GDP (printed by the run) and gives nothing beyond that headroom.</summary>
        private const float MaxOneTimeImpactShareOfGdp = 2f;
        private const int TurnsToClose = 2;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== P2-0.1: every Domestic money figure to its basis ===\n");

            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("DomesticMoneyBasis");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);

                sb.Append("\n--- 1. The seed: the headline figures and every spending line as a share of GDP ---\n");
                foreach (Country country in world.Countries)
                {
                    EconomyState s = country.State;
                    float ratio = s.GovernmentDebt / s.GDP * 100f;
                    float perCapita = s.Population > 0f ? s.GDP / s.Population : float.NaN;
                    sb.Append(F("  {0,-8} GDP {1,9:0.0}  debt {2,9:0.0}  debt/GDP {3,6:0.0}% (identity {4,6:0.0}%)  per capita {5,6:0.0}k  population {6,6:0.0}M\n",
                        country.Id, s.GDP, s.GovernmentDebt, s.DebtToGdpRatio, ratio, perCapita, s.Population));
                    failures += Assert(sb, $"{country.Id}: Debt-to-GDP is the identity debt/GDP",
                        Mathf.Abs(s.DebtToGdpRatio - ratio) < 1e-3f, F("{0} vs {1}", s.DebtToGdpRatio, ratio));

                    float sum = 0f;
                    float largest = 0f;
                    SpendingCategory largestCategory = default;
                    foreach (SpendingLine line in country.SpendingLines)
                    {
                        sum += line.Amount;
                        if (line.Amount > largest) { largest = line.Amount; largestCategory = line.Category; }
                    }

                    float sumShare = sum / s.GDP * 100f;
                    float largestShare = largest / s.GDP * 100f;
                    sb.Append(F("           {0} spending lines sum {1,8:0.0} = {2,5:0.0}% of GDP; largest {3} {4,7:0.0} = {5,5:0.0}% of GDP\n",
                        country.SpendingLines.Count, sum, sumShare, largestCategory, largest, largestShare));
                    failures += Assert(sb, $"{country.Id}: no seeded line above {MaxSeededLineShareOfGdp}% of GDP",
                        largestShare <= MaxSeededLineShareOfGdp, F("{0} at {1:0.0}%", largestCategory, largestShare));
                    failures += Assert(sb, $"{country.Id}: the seeded portfolio sums to at most {MaxSeededPortfolioShareOfGdp}% of GDP",
                        sumShare <= MaxSeededPortfolioShareOfGdp, F("{0:0.0}%", sumShare));
                }

                sb.Append(F("\n--- 2. After {0} closed years: the fiscal report's shares, as the sheet prints them ---\n", TurnsToClose));
                var noDecisions = new Dictionary<CountryId, PolicyDecision>();
                int closed = 0;
                int guard = 0;
                while (closed < TurnsToClose && guard++ < 3000)
                {
                    if (sim.AdvanceDay())
                    {
                        sim.AdvanceTurn(noDecisions);
                        closed++;
                    }
                }

                foreach (Country country in world.Countries)
                {
                    FiscalTurnReport report = sim.GetLastFiscalReport(country.Id);
                    failures += Assert(sb, $"{country.Id}: a fiscal report exists after {TurnsToClose} closed years", report != null, "null");
                    if (report == null) { continue; }

                    float? tax = DerivedStats.TaxBurdenPercentOfGdp(country, report);
                    float? spending = DerivedStats.SpendingPercentOfGdp(country, report);
                    float? deficit = DerivedStats.DeficitPercentOfGdp(country, report);
                    float? primary = DerivedStats.PrimaryDeficitPercentOfGdp(country, report);
                    sb.Append(F("  {0,-8} revenue {1,7:0.0} ({2,5:0.0}% of GDP)  spending {3,7:0.0} ({4,5:0.0}%)  balance {5,7:+0.0;-0.0} -> the sheet says {6} {7:0.0}%  primary {8:0.0}%  cumulative accumulator {9,8:+0.0;-0.0}\n",
                        country.Id, report.Revenue, tax ?? float.NaN, report.TotalSpending, spending ?? float.NaN, report.BudgetBalance,
                        deficit.HasValue && deficit.Value < 0f ? "SURPLUS" : "DEFICIT", Mathf.Abs(deficit ?? float.NaN), primary ?? float.NaN, country.State.Budget));
                    failures += Assert(sb, $"{country.Id}: revenue share of GDP within [{MinFlowShareOfGdp}, {MaxFlowShareOfGdp}]",
                        tax.HasValue && tax.Value >= MinFlowShareOfGdp && tax.Value <= MaxFlowShareOfGdp, F("{0:0.0}%", tax ?? float.NaN));
                    failures += Assert(sb, $"{country.Id}: spending share of GDP within [{MinFlowShareOfGdp}, {MaxFlowShareOfGdp}]",
                        spending.HasValue && spending.Value >= MinFlowShareOfGdp && spending.Value <= MaxFlowShareOfGdp, F("{0:0.0}%", spending ?? float.NaN));
                    failures += Assert(sb, $"{country.Id}: the year's balance within {MaxBalanceShareOfGdp}% of GDP either way",
                        deficit.HasValue && Mathf.Abs(deficit.Value) <= MaxBalanceShareOfGdp, F("{0:0.0}%", deficit ?? float.NaN));
                    failures += Assert(sb, $"{country.Id}: the sheet's deficit is the report's balance with the sign flipped, over the same GDP",
                        deficit.HasValue && Mathf.Abs(deficit.Value - (-report.BudgetBalance / country.State.GDP * 100f)) < 1e-3f,
                        F("{0} vs {1}", deficit ?? float.NaN, -report.BudgetBalance / country.State.GDP * 100f));
                }

                sb.Append("\n--- 3. The one-time settlements: every pooled option, authored and as applied, per country ---\n");
                sb.Append(F("    (authored on the USA seed's scale, GDP {0:0}; applied as the same share of the deciding country's GDP)\n", AuthoredImpactScale.AuthoredScaleGdp));
                List<(string Source, string Option, float Authored)> options = EnumeratePooledOptions(sb, ref failures);
                foreach (Country country in world.Countries)
                {
                    float worstShare = 0f;
                    string worst = "-";
                    float worstApplied = 0f;
                    float worstUnscaled = 0f;
                    foreach ((string source, string option, float authored) in options)
                    {
                        float applied = AuthoredImpactScale.ToCountryBillions(authored, country);
                        float share = Mathf.Abs(applied) / country.State.GDP * 100f;
                        if (share > worstShare)
                        {
                            worstShare = share;
                            worst = source + " / " + option;
                            worstApplied = applied;
                            worstUnscaled = authored;
                        }
                    }

                    sb.Append(F("  {0,-8} largest as applied {1,7:+0.0;-0.0} = {2,5:0.00}% of GDP (unscaled it was {3,7:+0.0;-0.0} = {4,6:0.00}% of GDP)  <- {5}\n",
                        country.Id, worstApplied, worstShare, worstUnscaled, Mathf.Abs(worstUnscaled) / country.State.GDP * 100f, worst));
                    failures += Assert(sb, $"{country.Id}: no pooled settlement lands above {MaxOneTimeImpactShareOfGdp}% of its GDP",
                        worstShare <= MaxOneTimeImpactShareOfGdp, F("{0:0.00}% ({1})", worstShare, worst));
                }

                // The identity claim is about the SEED (the scale is the USA seed's GDP), so it is read off a
                // fresh world rather than the one that has closed two years above - the first run of this
                // check asked it of a USA that had grown for two years and reported the growth as a defect.
                Country usa = WorldFactory.CreateDefault().GetCountry(CountryId.USA);
                bool usaIdentical = true;
                foreach ((string _, string _, float authored) in options)
                {
                    if (AuthoredImpactScale.ToCountryBillions(authored, usa) != authored) { usaIdentical = false; break; }
                }

                failures += Assert(sb, "USA at seed: every option applies exactly as authored (the scale is the USA seed, byte-identical)",
                    usaIdentical, F("USA GDP now {0}", usa.State.GDP));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            sb.Append(F("\n=== P2-0.1: {0} failure(s) ===\n", failures));
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Both pools, read by reflection - a harness reaching private state is the project's
        /// idiom, and a public accessor for a diagnostic would be a door nothing in play uses.</summary>
        private static List<(string Source, string Option, float Authored)> EnumeratePooledOptions(StringBuilder sb, ref int failures)
        {
            var options = new List<(string, string, float)>();
            FieldInfo cabinetField = typeof(CabinetSystem).GetField("DecisionPool", BindingFlags.NonPublic | BindingFlags.Static);
            if (cabinetField?.GetValue(null) is IDictionary cabinetPool)
            {
                foreach (DictionaryEntry entry in cabinetPool)
                {
                    if (!(entry.Value is List<CabinetDecision> decisions)) { continue; }
                    foreach (CabinetDecision decision in decisions)
                    {
                        foreach (CabinetDecisionOption option in decision.Options)
                        {
                            options.Add(("cabinet: " + decision.Name, option.Label, option.BudgetImpact));
                        }
                    }
                }
            }

            FieldInfo meetingField = typeof(ForeignPolicySystem).GetField("MeetingPool", BindingFlags.NonPublic | BindingFlags.Static);
            if (meetingField?.GetValue(null) is List<ForeignPolicyMeeting> meetings)
            {
                foreach (ForeignPolicyMeeting meeting in meetings)
                {
                    foreach (ForeignPolicyMeetingOption option in meeting.Options)
                    {
                        options.Add(("foreign policy: " + meeting.Name, option.Label, option.BudgetImpact));
                    }
                }
            }

            int withMoney = 0;
            float largest = 0f;
            foreach ((string _, string _, float authored) in options)
            {
                if (authored != 0f) { withMoney++; }
                largest = Mathf.Max(largest, Mathf.Abs(authored));
            }

            sb.Append(F("    {0} pooled options, {1} with a settlement, largest authored magnitude {2:0}\n", options.Count, withMoney, largest));
            failures += Assert(sb, "both pools were read (a diagnostic that enumerates nothing verified nothing)", withMoney > 0, "no options with a settlement found by reflection");
            return options;
        }

        private static int Assert(StringBuilder sb, string claim, bool holds, string detail)
        {
            sb.Append(holds ? "    ok    " : "    FAIL  ").Append(claim).Append("  (").Append(detail).Append(")\n");
            return holds ? 0 : 1;
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
