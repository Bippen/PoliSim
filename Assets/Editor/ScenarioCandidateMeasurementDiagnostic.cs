using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// R-K2 (omnibus 2026-08-28): the two remaining scenarios of the ruled slate are MEASURED before
    /// they are authored - "Poland convergence is measured against UnemploymentReversionSpeed
    /// FIRST; if the measurement kills it, drop it with the measurement (the §§22/30 precedent) - do
    /// not tune the model to save a scenario."
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 6): two candidate premises, each run as a small set
    /// of policy LINES through the real SimulationManager for 30 turns (seed 777), the numbers the
    /// premise hinges on recorded at every boundary. It asserts nothing about winnability - the
    /// verdict on each premise is read off the printed streaks and margins in the report; a line
    /// that produces the same numbers as no-policy is the drop signal the precedent names.</para>
    ///
    /// <para><b>Poland convergence.</b> Objectives as drafted: sustained real-wage growth with
    /// inflation in band; fail: inflation above 6% three turns running. The premise's mechanism is
    /// tightness → wages → sentiment → consumption overheating itself. But real wages grow at
    /// PotentialGrowthRate (3.0 for Poland) on the no-policy path by construction (the 1:1 trend
    /// pass-through), so the only thing a player could ADD is the tightness term - 0.7 pp of wage
    /// growth per pp of unemployment below NAIRU - and UnemploymentReversionSpeed (0.7/turn) closes
    /// any gap by t2–t3. The measurement records, per line: the real-wage growth streaks above
    /// trend, the inflation band streaks, the fail streak, and the largest tightness reached.</para>
    ///
    /// <para><b>The Unequal Recovery.</b> Deltas: a structurally more unequal USA (BaselineGini and
    /// Gini +6, 39.5 → 45.5 - the 2019 OECD basis plus six, inside the model's [15, 65]) and a hostile
    /// hemicycle (seats 52/60/52/36 for Progressive/Conservative/Centrist/Nationalist - net fiscal
    /// alignment −0.08, so every EXPANSIONARY bill fails at the start). Objective: Gini back to the
    /// seed baseline (≤ 39.5) by t30 without approval ever below 30. The measurement records: Gini
    /// and approval per turn on a no-policy line and on an as-if-passed redistribution line (the
    /// harness path bypasses the vote - said in the log), and on every line the first turn at which
    /// an expansionary bill WOULD pass the drifting composition - the number the political dilemma
    /// turns on.</para>
    ///
    /// Read-only against the repo: builds worlds in memory, writes nothing.
    /// </summary>
    public static class ScenarioCandidateMeasurementDiagnostic
    {
        private const int Turns = 30;
        private const int Seed = 777;

        [MenuItem("PoliSim/Run Scenario Candidate Measurement (R-K2)")]
        private static void RunFromMenu()
        {
            int code = CheckExit.Collect(Run);
            Debug.Log(code == 0 ? "SCENMEASURE: clean." : $"SCENMEASURE: FAILED ({code}).");
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            Debug.Log("=== SCENMEASURE: Poland convergence - measured against UnemploymentReversionSpeed FIRST (R-K2) ===");
            MeasurePoland("no-policy", (turn, d) => { });
            MeasurePoland("stimulus: +20% discretionary at t1, policy rate -2 at t1", (turn, d) =>
            {
                if (turn != 1) return;
                d.SpendingLineChanges[SpendingCategory.InfrastructureAndDevelopment] = 20f;
                d.SpendingLineChanges[SpendingCategory.PublicServices] = 20f;
                d.SpendingLineChanges[SpendingCategory.Administration] = 20f;
                d.InterestRateChange = -2f;
            });
            MeasurePoland("overheat: +40% discretionary at t1 and t2, policy rate -3 each of t1..t3", (turn, d) =>
            {
                if (turn <= 2)
                {
                    d.SpendingLineChanges[SpendingCategory.InfrastructureAndDevelopment] = 40f;
                    d.SpendingLineChanges[SpendingCategory.PublicServices] = 40f;
                    d.SpendingLineChanges[SpendingCategory.Administration] = 40f;
                }
                if (turn <= 3) { d.InterestRateChange = -3f; }
            });
            MeasurePoland("restraint: -20% discretionary at t1, policy rate +2 at t1", (turn, d) =>
            {
                if (turn != 1) return;
                d.SpendingLineChanges[SpendingCategory.InfrastructureAndDevelopment] = -20f;
                d.SpendingLineChanges[SpendingCategory.PublicServices] = -20f;
                d.SpendingLineChanges[SpendingCategory.Administration] = -20f;
                d.InterestRateChange = 2f;
            });

            Debug.Log("=== SCENMEASURE: The Unequal Recovery (USA, Gini +6 structural, hostile hemicycle) ===");
            MeasureUnequal("no-policy", (turn, country, d) => { });
            MeasureUnequal("redistribution AS IF PASSED at t1: NIT + means-tested at 100, income tax +8 (the harness path bypasses the vote)", (turn, country, d) =>
            {
                if (turn != 1) return;
                foreach (WelfareProgram program in country.WelfarePrograms)
                {
                    if (program.Type == WelfareProgramType.NegativeIncomeTax || program.Type == WelfareProgramType.MeansTestedWelfare)
                    {
                        program.IsImplemented = true;
                    }
                }
                d.WelfareGenerosityOverrides[WelfareProgramType.NegativeIncomeTax] = 100f;
                d.WelfareGenerosityOverrides[WelfareProgramType.MeansTestedWelfare] = 100f;
                foreach (TaxLine line in country.TaxLines)
                {
                    if (line.Type == TaxType.IncomeTax) { d.TaxRateOverrides[TaxType.IncomeTax] = line.Rate + 8f; }
                }
            });
            MeasureUnequal("tax only AS IF PASSED at t1: income tax +8", (turn, country, d) =>
            {
                if (turn != 1) return;
                foreach (TaxLine line in country.TaxLines)
                {
                    if (line.Type == TaxType.IncomeTax) { d.TaxRateOverrides[TaxType.IncomeTax] = line.Rate + 8f; }
                }
            });

            CheckExit.Finish(0);
        }

        private static void MeasurePoland(string label, System.Action<int, PolicyDecision> decide)
        {
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("SCENMEASURE_PL");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(CountryId.Poland);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                float previousIndex = c.State.RealWageIndex;
                int streakAboveTrend = 0, bestAboveTrend = 0;      // real-wage growth >= trend + 0.5
                int streakGrowth2 = 0, bestGrowth2 = 0;            // >= 2.0 %/yr
                int streakBand = 0, bestBand = 0;                  // inflation in [1, 4]
                int streakFail = 0, bestFail = 0;                  // inflation > 6
                float maxTightness = 0f, maxInflation = float.MinValue, minApproval = float.MaxValue;
                int tightTurns = 0;                                // gap <= -1 pp
                var rows = new List<string>();

                for (int turn = 1; turn <= Turns; turn++)
                {
                    PolicyDecision d = PolicyDecision.None();
                    decide(turn, d);
                    decisions[CountryId.Poland] = d;
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);

                    EconomyState s = c.State;
                    float growth = previousIndex > 0f ? (s.RealWageIndex / previousIndex - 1f) * 100f : 0f;
                    previousIndex = s.RealWageIndex;
                    float gap = s.Unemployment - c.NaturalUnemploymentRate;

                    streakAboveTrend = growth >= c.PotentialGrowthRate + 0.5f ? streakAboveTrend + 1 : 0; bestAboveTrend = Mathf.Max(bestAboveTrend, streakAboveTrend);
                    streakGrowth2 = growth >= 2f ? streakGrowth2 + 1 : 0; bestGrowth2 = Mathf.Max(bestGrowth2, streakGrowth2);
                    streakBand = s.Inflation >= 1f && s.Inflation <= 4f ? streakBand + 1 : 0; bestBand = Mathf.Max(bestBand, streakBand);
                    streakFail = s.Inflation > 6f ? streakFail + 1 : 0; bestFail = Mathf.Max(bestFail, streakFail);
                    maxTightness = Mathf.Min(maxTightness, gap);
                    if (gap <= -1f) tightTurns++;
                    maxInflation = Mathf.Max(maxInflation, s.Inflation);
                    minApproval = Mathf.Min(minApproval, s.ApprovalRating);
                    if (turn <= 6 || turn % 5 == 0)
                    {
                        rows.Add($"t{turn}: wage growth {growth:+0.00;-0.00}% (trend {c.PotentialGrowthRate:F2}), inflation {s.Inflation:F2}, U gap {gap:+0.00;-0.00} pp, rate {c.CurrencyZone.InterestRate:F2}, approval {s.ApprovalRating:F1}");
                    }
                }

                Debug.Log($"SCENMEASURE[Poland | {label}]: max streak of wage growth >= trend+0.5: {bestAboveTrend} turn(s); >= 2%: {bestGrowth2}; " +
                          $"inflation in [1,4]: {bestBand}; inflation > 6 (the fail streak): {bestFail}; deepest tightness {maxTightness:+0.00;-0.00} pp over {tightTurns} turn(s) at <= -1; " +
                          $"max inflation {maxInflation:F2}; min approval {minApproval:F1}");
                foreach (string row in rows) { Debug.Log($"SCENMEASURE[Poland | {label}]   {row}"); }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void MeasureUnequal(string label, System.Action<int, Country, PolicyDecision> decide)
        {
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("SCENMEASURE_US");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(CountryId.USA);

                // The candidate deltas, exactly as the scenario would apply them.
                float seedBaselineGini = c.BaselineGini;
                c.BaselineGini += 6f;
                c.State.Gini = c.BaselineGini;
                // W-G1: the staged chamber is Sweden's real one, because the country under test IS
                // Sweden. The four archetype counts this replaces (P52/C60/Ce52/N36) summed to a
                // fictional 200-seat house; these sum to the Riksdag's 349.
                c.ParliamentSeats = PartySystems.InitialSeats(c.Id);
                Debug.Log($"SCENMEASURE[Unequal | {label}]: deltas applied - Gini {seedBaselineGini:F1} -> {c.State.Gini:F1} (baseline moved with it); " +
                          $"seats: Sweden 2022 (349), expansionary alignment {ParliamentSystem.GetSeatWeightedAlignment(c, 1f):+0.000;-0.000} " +
                          $"(passes: {ParliamentSystem.WouldBillPass(c, 1f)})");

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                float minApproval = float.MaxValue;
                int firstPassTurn = -1;
                var rows = new List<string>();
                for (int turn = 1; turn <= Turns; turn++)
                {
                    PolicyDecision d = PolicyDecision.None();
                    decide(turn, c, d);
                    decisions[CountryId.USA] = d;
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);

                    EconomyState s = c.State;
                    minApproval = Mathf.Min(minApproval, s.ApprovalRating);
                    float alignment = ParliamentSystem.GetSeatWeightedAlignment(c, 1f);
                    if (firstPassTurn < 0 && alignment > 0f) { firstPassTurn = turn; }
                    if (turn <= 6 || turn % 5 == 0)
                    {
                        var seatParts = new List<string>();
                        foreach (PoliticalParty party in PartySystems.For(c.Id))
                        {
                            if (c.ParliamentSeats.TryGetValue(party.Abbrev, out int held) && held > 0) { seatParts.Add($"{party.Abbrev}{held}"); }
                        }
                        rows.Add($"t{turn}: Gini {s.Gini:F2} (target <= {seedBaselineGini:F1}), approval {s.ApprovalRating:F1}, seats {string.Join("/", seatParts)}, expansionary alignment {alignment:+0.000;-0.000}, debt/GDP {s.DebtToGdpRatio:F1}");
                    }
                }

                Debug.Log($"SCENMEASURE[Unequal | {label}]: t{Turns} Gini {c.State.Gini:F2} vs target <= {seedBaselineGini:F1} (margin {seedBaselineGini - c.State.Gini:+0.00;-0.00}); " +
                          $"min approval {minApproval:F1} (floor 30); first turn an expansionary bill would pass: {(firstPassTurn < 0 ? "never" : "t" + firstPassTurn)}");
                foreach (string row in rows) { Debug.Log($"SCENMEASURE[Unequal | {label}]   {row}"); }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
