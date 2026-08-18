using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Round 2 of the pre-authoring measurement, responding to what round 1 found: Okun's own
    /// 0.7/turn reversion closes a one-shot unemployment impulse within ~5 turns regardless of
    /// player action, but the 150-200 turn no-policy runs showed inflation drifting from ~2% to
    /// 3%+ even while unemployment sat back at NAIRU almost the whole time. Two questions: (1) is
    /// that drift real/independent of the impulse (test at impulse=0), and (2) does a REALISTIC
    /// single rate hike (not re-applied every turn, which pins the rate at its 15% ceiling within
    /// a handful of turns and was round 1's own measurement error) actually slow it.
    /// </summary>
    public static class WageBoomMeasurementDiagnostic2
    {
        [MenuItem("PoliSim/Run Wage Boom Measurement 2 (drift + single hike)")]
        private static void RunFromMenu() => Run();

        public static void Run()
        {
            Debug.Log("WAGEBOOM2: --- §4 is the drift present with ZERO impulse? Sweden/Poland, 150 turns, no policy ---");
            RunTrajectory(CountryId.Sweden, 0f, turns: 150, hikeAtT1: 0f, label: "sweden_0pp_150t_baseline");
            RunTrajectory(CountryId.Poland, 0f, turns: 150, hikeAtT1: 0f, label: "poland_0pp_150t_baseline");

            Debug.Log("WAGEBOOM2: --- §5 a REALISTIC single hike (once, at t1, then left alone) vs control, 150 turns ---");
            RunTrajectory(CountryId.Sweden, 3f, turns: 150, hikeAtT1: 0f, label: "sweden_3pp_150t_control");
            RunTrajectory(CountryId.Sweden, 3f, turns: 150, hikeAtT1: 1.5f, label: "sweden_3pp_150t_hike1.5once");
            RunTrajectory(CountryId.Sweden, 3f, turns: 150, hikeAtT1: 3f, label: "sweden_3pp_150t_hike3once");

            Debug.Log("WAGEBOOM2: --- §6 does the SAME single hike help the zero-impulse baseline drift too? ---");
            RunTrajectory(CountryId.Sweden, 0f, turns: 150, hikeAtT1: 1.5f, label: "sweden_0pp_150t_hike1.5once");

            CheckExit.Finish(0);
        }

        private static void RunTrajectory(CountryId id, float impulsePp, int turns, float hikeAtT1, string label)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"WAGEBOOM2_{label}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(id);
                if (impulsePp != 0f)
                {
                    c.State.Unemployment = Mathf.Max(0f, c.NaturalUnemploymentRate - impulsePp);
                }

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                Debug.Log($"WAGEBOOM2[{label}]: {id} start U={c.State.Unemployment:F2} (NAIRU {c.NaturalUnemploymentRate:F2}) rate={c.CurrencyZone.InterestRate:F2}%");

                for (int turn = 1; turn <= turns; turn++)
                {
                    decisions[id] = (turn == 1 && hikeAtT1 != 0f)
                        ? new PolicyDecision { InterestRateChange = hikeAtT1 }
                        : PolicyDecision.None();

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);

                    if (turn <= 3 || turn % 10 == 0 || turn == turns)
                    {
                        EconomyState s = c.State;
                        Debug.Log($"WAGEBOOM2[{label}] t{turn}: U={s.Unemployment:F3} gap={c.NaturalUnemploymentRate - s.Unemployment:+0.000;-0.000} " +
                                  $"π={s.Inflation:F3} πe={s.InflationExpectations:F3} rate={c.CurrencyZone.InterestRate:F3} " +
                                  $"RealWage={s.RealWageIndex:F1} Approval={s.ApprovalRating:F2}");
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
