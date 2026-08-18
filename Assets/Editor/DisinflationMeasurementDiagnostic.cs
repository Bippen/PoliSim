using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PRE-AUTHORING MEASUREMENT for "The Disinflation" - the "Inherit the Fund"/Wage Boom
    /// precedent applied a third time: measure before writing an objective number, report the
    /// half-life FIRST (Wage Boom died on turn 2-3 self-correction), then whether the rate lever
    /// bites, then settle the Eurozone-membership ambiguity by measurement rather than by
    /// argument. Nothing here is scenario code.
    ///
    /// The Phillips curve's own shape (ApplyPhillipsCurveInflation: Inflation =
    /// InflationExpectations - slope*(U-NAIRU), and expectations adapt HALFWAY toward realized
    /// inflation each turn) means a start with U AT NAIRU and Inflation=Expectations elevated
    /// together is a FIXED POINT by construction - nothing pulls it down without unemployment
    /// moving into genuine SLACK (above NAIRU) first. That is the opposite failure risk from Wage
    /// Boom (not "self-corrects too fast" but "does not correct without deliberate policy") and
    /// is worth confirming directly before assuming it.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.DisinflationMeasurementDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class DisinflationMeasurementDiagnostic
    {
        [MenuItem("PoliSim/Run Disinflation Measurement (pre-authoring)")]
        private static void RunFromMenu() => Run();

        public static void Run()
        {
            Debug.Log("DISINFLATION: --- §1 the fixed-point hypothesis: elevated pi=pie, U=NAIRU, no policy ---");
            RunTrajectory(CountryId.Poland, startInflation: 10f, rateChangeAtT1: 0f, turns: 30, label: "poland_10pct_nopolicy");
            RunTrajectory(CountryId.Sweden, startInflation: 10f, rateChangeAtT1: 0f, turns: 30, label: "sweden_10pct_nopolicy");
            RunTrajectory(CountryId.Germany, startInflation: 10f, rateChangeAtT1: 0f, turns: 30, label: "germany_10pct_nopolicy_eurozone");

            Debug.Log("DISINFLATION: --- §2 does a ONE-TIME rate hike actually disinflate? Poland, magnitudes ---");
            RunTrajectory(CountryId.Poland, startInflation: 10f, rateChangeAtT1: 1.5f, turns: 30, label: "poland_10pct_hike1.5");
            RunTrajectory(CountryId.Poland, startInflation: 10f, rateChangeAtT1: 3f, turns: 30, label: "poland_10pct_hike3");
            RunTrajectory(CountryId.Poland, startInflation: 10f, rateChangeAtT1: 5f, turns: 30, label: "poland_10pct_hike5");

            Debug.Log("DISINFLATION: --- §3 same test, Sweden, for the country-choice comparison ---");
            RunTrajectory(CountryId.Sweden, startInflation: 10f, rateChangeAtT1: 1.5f, turns: 30, label: "sweden_10pct_hike1.5");
            RunTrajectory(CountryId.Sweden, startInflation: 10f, rateChangeAtT1: 3f, turns: 30, label: "sweden_10pct_hike3");

            Debug.Log("DISINFLATION: --- §4 the Eurozone question: does the auto-following blend disinflate WITHOUT the player? ---");
            RunTrajectory(CountryId.Germany, startInflation: 10f, rateChangeAtT1: 0f, turns: 30, label: "germany_10pct_nopush_control");
            RunTrajectory(CountryId.Germany, startInflation: 10f, rateChangeAtT1: 0.75f, turns: 30, label: "germany_10pct_maxpush0.75");
            RunTrajectory(CountryId.Italy, startInflation: 10f, rateChangeAtT1: 0f, turns: 30, label: "italy_10pct_nopush_control");

            CheckExit.Finish(0);
        }

        private static void RunTrajectory(CountryId id, float startInflation, float rateChangeAtT1, int turns, string label)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"DISINFLATION_{label}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(id);
                c.State.Inflation = startInflation;
                c.State.InflationExpectations = startInflation;

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                Debug.Log($"DISINFLATION[{label}]: {id} start pi={c.State.Inflation:F2} pie={c.State.InflationExpectations:F2} " +
                          $"U={c.State.Unemployment:F2} (NAIRU {c.NaturalUnemploymentRate:F2}) rate={c.CurrencyZone.InterestRate:F2}%");

                for (int turn = 1; turn <= turns; turn++)
                {
                    decisions[id] = (turn == 1 && rateChangeAtT1 != 0f)
                        ? new PolicyDecision { InterestRateChange = rateChangeAtT1 }
                        : PolicyDecision.None();

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);

                    if (turn <= 5 || turn % 5 == 0 || turn == turns)
                    {
                        EconomyState s = c.State;
                        Debug.Log($"DISINFLATION[{label}] t{turn}: pi={s.Inflation:F3} pie={s.InflationExpectations:F3} " +
                                  $"U={s.Unemployment:F3} gap={c.NaturalUnemploymentRate - s.Unemployment:+0.000;-0.000} " +
                                  $"rate={c.CurrencyZone.InterestRate:F3} Approval={s.ApprovalRating:F2} GDP={s.GDP:F1} PotGDP={s.PotentialGDP:F1}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
