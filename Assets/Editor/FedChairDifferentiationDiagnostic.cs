using System.Collections.Generic;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Pass 4 (the Taylor-path gap fix): the Fed chair DIFFERENTIATION measurement. Under the
    /// floored level-gap rule the three philosophies collapsed - the rule's suggestion read 0 for
    /// the USA, so the realized rate WAS the chair's bias (Hawkish ~1.25-1.5%, Moderate 0-0.25%,
    /// Dovish 0%). This harness forces each candidate chair in turn onto the no-policy trajectory
    /// (seed 777, the TrajectoryBaselineDump driving idiom: AdvanceDay x DaysPerTurn, then
    /// AdvanceTurn with all-None decisions) and reports, per chair, the realized USA rate, the
    /// rule's suggestion, the chair target, inflation and unemployment at fixed waypoints; then
    /// the two figures the pass is judged on:
    ///
    ///   DIFFERENTIATION - the spread between the most hawkish and most dovish chairs' realized
    ///   rates at t50 and t100 (collapsed = both sit at the floor or the spread is just the bias
    ///   difference clamped at 0), and the inflation difference the same pair produces by t100.
    ///
    ///   STABILITY - the crash-loop check. The documented undamped signature was a 5.05 -> 0.00
    ///   single-turn jump and a self-sustaining overshoot cycle (CLAUDE.md "Federal Reserve Rate
    ///   Damping"); RateAdjustmentSpeed 0.15 is the standing fix. A working gap term changes the
    ///   path the damping chases, so the maximum single-turn rate change and the count of
    ///   direction reversals over the run are reported for every chair.
    ///
    /// Reporting tool, not a gate: exits 0 on completion (the numbers are the finding). The log
    /// fold still turns an ATTRIB or any error raised during the run into a nonzero exit, and a
    /// failure to reach the candidate pool by reflection exits 1 - a measurement that silently
    /// covered the wrong chairs would be worse than none.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.FedChairDifferentiationDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class FedChairDifferentiationDiagnostic
    {
        [MenuItem("PoliSim/Run Fed Chair Differentiation (pass 4)")]
        private static void RunFromMenu() => Run();

        private const int Turns = 100;
        private const int Seed = 777;
        private static readonly int[] Waypoints = { 1, 2, 3, 5, 10, 20, 30, 50, 75, 100 };

        private sealed class ChairRun
        {
            public FedChair Chair;
            public float[] Rate = new float[Turns + 1];
            public float[] Suggested = new float[Turns + 1];
            public float[] Target = new float[Turns + 1];
            public float[] Inflation = new float[Turns + 1];
            public float[] Unemployment = new float[Turns + 1];
            public float MaxStep;
            public int MaxStepTurn;
            public int Reversals;
            public bool ChairHeld;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1 (2026-08-25): this advances turns; an ATTRIB during it is a failure even though the tool exits 0 by design otherwise.

            List<FedChair> chairs = ChairsToMeasure();
            if (chairs == null)
            {
                Debug.LogError("FEDCHAIR: could not reach FederalReserveSystem.CandidatePool by reflection - measuring nothing.");
                CheckExit.Finish(1);
                return;
            }

            Debug.Log($"FEDCHAIR: {chairs.Count} chairs (the seeded default + the candidate pool), seed {Seed}, {Turns} turns, no policy.");
            var runs = new List<ChairRun>(chairs.Count);
            foreach (FedChair chair in chairs)
            {
                runs.Add(RunOne(chair));
            }

            foreach (ChairRun run in runs)
            {
                Debug.Log($"FEDCHAIR[{run.Chair.Name} / {run.Chair.Philosophy} / bias {run.Chair.RateBias:+0.00;-0.00}] chair held all {Turns} turns: {(run.ChairHeld ? "yes" : "NO")}");
                foreach (int t in Waypoints)
                {
                    Debug.Log($"  t{t,-4} rate {run.Rate[t],6:F3}  suggested {run.Suggested[t],6:F3}  target {run.Target[t],6:F3}  pi {run.Inflation[t],5:F2}  U {run.Unemployment[t],5:F2}");
                }

                Debug.Log($"  stability: max single-turn rate change {run.MaxStep:F3} pp at t{run.MaxStepTurn}; direction reversals {run.Reversals} over {Turns} turns");
            }

            ChairRun hawk = runs[0], dove = runs[0];
            foreach (ChairRun run in runs)
            {
                if (run.Chair.RateBias > hawk.Chair.RateBias) { hawk = run; }
                if (run.Chair.RateBias < dove.Chair.RateBias) { dove = run; }
            }

            int atFloor50 = 0, atFloor100 = 0;
            float maxStepAll = 0f;
            int maxReversals = 0;
            foreach (ChairRun run in runs)
            {
                if (run.Rate[50] < 0.05f) { atFloor50++; }
                if (run.Rate[100] < 0.05f) { atFloor100++; }
                maxStepAll = Mathf.Max(maxStepAll, run.MaxStep);
                maxReversals = Mathf.Max(maxReversals, run.Reversals);
            }

            Debug.Log($"FEDCHAIR DIFFERENTIATION: most hawkish {hawk.Chair.Name} ({hawk.Chair.RateBias:+0.00}) vs most dovish {dove.Chair.Name} ({dove.Chair.RateBias:+0.00}): " +
                      $"rate spread t50 {hawk.Rate[50] - dove.Rate[50]:F3} pp, t100 {hawk.Rate[100] - dove.Rate[100]:F3} pp (bias difference {hawk.Chair.RateBias - dove.Chair.RateBias:F2}); " +
                      $"inflation dove-minus-hawk t100 {dove.Inflation[100] - hawk.Inflation[100]:+0.000;-0.000} pp; unemployment hawk-minus-dove t100 {hawk.Unemployment[100] - dove.Unemployment[100]:+0.000;-0.000} pp; " +
                      $"chairs at the floor: t50 {atFloor50}/{runs.Count}, t100 {atFloor100}/{runs.Count}.");
            Debug.Log($"FEDCHAIR STABILITY: max single-turn rate change over all chairs {maxStepAll:F3} pp; most reversals {maxReversals}. " +
                      "(The undamped crash-loop signature was a multi-point single-turn jump with reversals every turn.)");
            Debug.Log("FEDCHAIR: done.");
            CheckExit.Finish(0);
        }

        private static ChairRun RunOne(FedChair chair)
        {
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            Country usa = world.GetCountry(CountryId.USA);
            usa.CurrentFedChair = chair;

            var run = new ChairRun { Chair = chair };
            var go = new GameObject($"FEDCHAIR_{chair.Name}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country country in world.Countries)
                {
                    decisions[country.Id] = PolicyDecision.None();
                }

                float prevRate = usa.CurrencyZone.InterestRate;
                float prevDelta = 0f;
                for (int turn = 1; turn <= Turns; turn++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++)
                    {
                        sim.AdvanceDay();
                    }

                    sim.AdvanceTurn(decisions);

                    float rate = usa.CurrencyZone.InterestRate;
                    run.Rate[turn] = rate;
                    run.Suggested[turn] = TaylorRule.GetSuggestedInterestRate(usa);
                    run.Target[turn] = Mathf.Clamp(run.Suggested[turn] + chair.RateBias, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
                    run.Inflation[turn] = usa.State.Inflation;
                    run.Unemployment[turn] = usa.State.Unemployment;

                    float delta = rate - prevRate;
                    if (Mathf.Abs(delta) > run.MaxStep) { run.MaxStep = Mathf.Abs(delta); run.MaxStepTurn = turn; }
                    if (Mathf.Abs(delta) > 1e-4f && Mathf.Abs(prevDelta) > 1e-4f && Mathf.Sign(delta) != Mathf.Sign(prevDelta)) { run.Reversals++; }
                    if (Mathf.Abs(delta) > 1e-4f) { prevDelta = delta; }
                    prevRate = rate;
                }

                run.ChairHeld = ReferenceEquals(usa.CurrentFedChair, chair);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            return run;
        }

        /// <summary>The seeded default chair first (what a fresh game runs under), then every
        /// candidate in FederalReserveSystem's private pool - read by reflection so the pool stays
        /// the single source of chairs (the LaborLawCompositionDiagnostic idiom).</summary>
        private static List<FedChair> ChairsToMeasure()
        {
            FieldInfo poolField = typeof(FederalReserveSystem).GetField("CandidatePool", BindingFlags.NonPublic | BindingFlags.Static);
            if (poolField == null || !(poolField.GetValue(null) is List<FedChair> pool))
            {
                return null;
            }

            var chairs = new List<FedChair>();
            FedChair seeded = WorldFactory.CreateDefault().GetCountry(CountryId.USA).CurrentFedChair;
            if (seeded != null)
            {
                chairs.Add(seeded);
            }

            chairs.AddRange(pool);
            return chairs;
        }
    }
}
