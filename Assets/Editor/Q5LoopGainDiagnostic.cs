using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Q5's headline measurement: **the model's first closed feedback loop, measured rather than
    /// trusted.**
    ///
    /// <para>The circuit the Q5 build closes:
    /// <code>
    /// U gap ──h──▶ productivity cycle ──1:1──▶ wage growth ──▶ Q2 gap ──▶ Consumption
    ///    ▲                                                                    │
    ///    └────────── Okun ◀── GDP growth ◀── national-accounts identity ◀──────┘
    /// </code>
    /// The derivation predicted gain = 0.075 × h ≈ 0.03 at h = 0.4, stable by ~20× against Okun's
    /// own 0.7/turn reversion. **A derived gain is a hypothesis**; if the measurement disagrees
    /// materially, something else is in the path and THAT is the pass's finding.</para>
    ///
    /// <para><b>How the counterfactual is taken, without duplicating any arithmetic:</b> every
    /// step calls the SAME production function twice - once with the real cycle and once with
    /// `cyclePerTurnPercent = 0`. Passing zero is the honest open-loop form of the shipped code
    /// path, not a reimplementation of it, which is exactly why the Q5 build threads the cycle as
    /// a parameter instead of reading it inside each function.</para>
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.Q5LoopGainDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class Q5LoopGainDiagnostic
    {
        /// <summary>The tightness impulse, in points of unemployment BELOW NAIRU.</summary>
        private const float ImpulsePp = 1f;

        [MenuItem("PoliSim/Run Q5 Loop Gain Diagnostic")]
        private static void RunFromMenu()
        {
            int code = CheckExit.Collect(Run);
            Debug.Log(code == 0 ? "Q5LOOP: measured." : $"Q5LOOP: FAILED ({code}).");
        }

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1 (2026-08-25): this advances turns; a measurement taken while the model's self-audit fails is meaningless, so an ATTRIB during it exits nonzero even though this tool exits 0 by design otherwise.
            Debug.Log($"Q5LOOP: impulse = {ImpulsePp:F2} pp of tightness (U set that far BELOW NAIRU); " +
                      "every link measured through the production functions, open loop via cycle = 0.");

            foreach (CountryId id in new[] { CountryId.USA, CountryId.Sweden, CountryId.Poland })
            {
                MeasureLinks(id);
            }

            MeasureRealizedGain();
            CheckExit.Finish(0);
        }

        /// <summary>Link-by-link, one period, at a stated state - the erosion-standard decomposition
        /// applied to a loop instead of to a stock.</summary>
        private static void MeasureLinks(CountryId id)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            Country c = world.GetCountry(id);

            // The impulse: unemployment ImpulsePp below NAIRU, everything else at seed.
            c.State.Unemployment = c.NaturalUnemploymentRate - ImpulsePp;
            float uAtOpen = c.State.Unemployment;

            // Link 1: the hoarding term itself.
            float cycle = MacroSystem.ProductivityCycleGrowthPerTurnPercent(c, uAtOpen);

            // Link 2: wage growth, with and without the cycle (the open-loop counterfactual).
            float wageWith = MacroSystem.RealWageGrowthPerTurnPercent(c, cycle);
            float wageWithout = MacroSystem.RealWageGrowthPerTurnPercent(c, 0f);

            // Link 3: Q2's gap - the quantity the sentiment factor consumes.
            float gapWith = MacroSystem.RealWageGrowthGapPerTurnPercent(c, cycle);
            float gapWithout = MacroSystem.RealWageGrowthGapPerTurnPercent(c, 0f);

            // Link 4: effective consumer confidence, hence consumption's multiplier.
            float confWith = MacroSystem.EffectiveConsumerConfidence(c, gapWith);
            float confWithout = MacroSystem.EffectiveConsumerConfidence(c, gapWithout);
            float consumptionPercent = confWithout != 0f ? (confWith / confWithout - 1f) * 100f : 0f;

            Debug.Log($"Q5LOOP {id}: cycle {cycle:+0.000;-0.000} pp · wage {wageWithout:F4} -> {wageWith:F4} " +
                      $"({wageWith - wageWithout:+0.000;-0.000}) · Q2 gap {gapWithout:F4} -> {gapWith:F4} · " +
                      $"effective confidence {confWithout:F5} -> {confWith:F5} (consumption {consumptionPercent:+0.0000;-0.0000}%)");
        }

        /// <summary>
        /// The realized one-period response to a tightness impulse, on the SHIPPED path.
        ///
        /// <para><b>This number is only half a measurement, and the other half is the h = 0
        /// control build the bar already requires.</b> Run this diagnostic under both builds: the
        /// control (h = 0, open loop, no cycle anywhere) and the force build (h = 0.4). The
        /// DIFFERENCE in the reported response, divided by the impulse, is the loop gain -
        /// measured end to end through the real day loop, with Okun's own reversion present in
        /// both runs and therefore cancelling. That is why this reports a raw response rather
        /// than computing a gain it cannot see from inside one build.</para>
        /// </summary>
        private static void MeasureRealizedGain()
        {
            Debug.Log($"Q5LOOP: --- realized one-period response, shipped path, impulse {ImpulsePp:F2} pp ---");
            Debug.Log("Q5LOOP: run this under BOTH builds (h=0 control and h=0.4); the difference/impulse is the gain.");

            foreach (CountryId id in new[] { CountryId.USA, CountryId.Sweden, CountryId.Poland })
            {
                float responseAtZero = RunOnePeriodAndReadTightness(id, 0f);
                float responseAtImpulse = RunOnePeriodAndReadTightness(id, ImpulsePp);
                Debug.Log($"Q5LOOP {id}: end-of-period tightness — baseline (no impulse) {responseAtZero:F5} pp, " +
                          $"impulsed {responseAtImpulse:F5} pp, retained {responseAtImpulse - responseAtZero:F5} pp of the " +
                          $"{ImpulsePp:F2} pp shock.");
            }
        }

        /// <summary>Sets the impulse, advances exactly one period through the real day loop, and
        /// returns the tightness gap (NAIRU − U) the period ended at.</summary>
        private static float RunOnePeriodAndReadTightness(CountryId id, float impulsePp)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"Q5LOOP_{id}_{impulsePp}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(id);
                c.State.Unemployment = c.NaturalUnemploymentRate - impulsePp;

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);

                return c.NaturalUnemploymentRate - c.State.Unemployment;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
