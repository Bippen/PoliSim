using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PRE-AUTHORING MEASUREMENT for "Wage Boom Management" - the "Inherit the Fund" precedent
    /// applied again: measure what the mechanism actually does BEFORE writing a single objective
    /// number. Nothing here is scenario code; this file's only job is to report.
    ///
    /// Three questions, each with its own section below: (1) what seed delta produces a boom and
    /// how fast Okun's own reversion closes it unmanaged; (2) how the Q5 loop's measured ~0.03
    /// gain expresses over 100-200 turns on top of that; (3) which player levers actually bite -
    /// tested by running the SAME delta with a lever held active for the run and diffing against
    /// the unmanaged trajectory.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.WageBoomMeasurementDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class WageBoomMeasurementDiagnostic
    {
        [MenuItem("PoliSim/Run Wage Boom Measurement (pre-authoring)")]
        private static void RunFromMenu() => Run();

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1 (2026-08-25): this advances turns; a measurement taken while the model's self-audit fails is meaningless, so an ATTRIB during it exits nonzero even though this tool exits 0 by design otherwise.
            Debug.Log("WAGEBOOM: --- §1 unmanaged trajectory, several impulse sizes, Sweden ---");
            foreach (float impulse in new[] { 1.5f, 3f, 4.5f })
            {
                RunTrajectory(CountryId.Sweden, impulse, turns: 30, rateHikePp: 0f, label: $"impulse{impulse:F1}_noaction");
            }

            Debug.Log("WAGEBOOM: --- §2 the same impulse, longer horizon, both candidate countries ---");
            RunTrajectory(CountryId.Sweden, 3f, turns: 200, rateHikePp: 0f, label: "sweden_3pp_200t");
            RunTrajectory(CountryId.Poland, 3f, turns: 200, rateHikePp: 0f, label: "poland_3pp_200t");
            RunTrajectory(CountryId.USA, 3f, turns: 60, rateHikePp: 0f, label: "usa_3pp_60t_fedchair");

            Debug.Log("WAGEBOOM: --- §3 does a rate hike actually bite? Sweden, 3pp impulse, held for 10 turns ---");
            RunTrajectory(CountryId.Sweden, 3f, turns: 30, rateHikePp: 0f, label: "sweden_3pp_control");
            RunTrajectory(CountryId.Sweden, 3f, turns: 30, rateHikePp: 2f, label: "sweden_3pp_hike2pp_10t", hikeDurationTurns: 10);
            RunTrajectory(CountryId.Sweden, 3f, turns: 30, rateHikePp: 4f, label: "sweden_3pp_hike4pp_10t", hikeDurationTurns: 10);

            CheckExit.Finish(0);
        }

        private static void RunTrajectory(CountryId id, float impulsePp, int turns, float rateHikePp, string label, int hikeDurationTurns = 0)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"WAGEBOOM_{label}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(id);
                c.State.Unemployment = Mathf.Max(0f, c.NaturalUnemploymentRate - impulsePp);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                Debug.Log($"WAGEBOOM[{label}]: {id} start U={c.State.Unemployment:F2} (NAIRU {c.NaturalUnemploymentRate:F2}, " +
                          $"{impulsePp:F1}pp below) rate={c.CurrencyZone.InterestRate:F2}%");

                for (int turn = 1; turn <= turns; turn++)
                {
                    if (hikeDurationTurns > 0 && turn <= hikeDurationTurns && rateHikePp != 0f)
                    {
                        decisions[id] = new PolicyDecision { InterestRateChange = rateHikePp };
                    }
                    else
                    {
                        decisions[id] = PolicyDecision.None();
                    }

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);

                    if (turn <= 6 || turn % 5 == 0 || turn == turns)
                    {
                        EconomyState s = c.State;
                        float uGap = c.NaturalUnemploymentRate - s.Unemployment;
                        Debug.Log($"WAGEBOOM[{label}] t{turn}: U={s.Unemployment:F3} (gap {uGap:+0.000;-0.000}) " +
                                  $"π={s.Inflation:F3} πe={s.InflationExpectations:F3} rate={c.CurrencyZone.InterestRate:F3} " +
                                  $"RealWage={s.RealWageIndex:F2} Productivity={s.Productivity:F2} Approval={s.ApprovalRating:F2} " +
                                  $"GDP={s.GDP:F1} PotGDP={s.PotentialGDP:F1}");
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
