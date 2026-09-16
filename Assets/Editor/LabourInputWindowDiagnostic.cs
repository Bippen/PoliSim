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
    /// PN-3 (2026-09-16, DS-3c §474): **THE PREMISE OF POTENTIAL'S 20–64 WINDOW, MEASURED BEFORE ANYTHING IS RE-FORMED.** Potential's labour input
    /// (and the wage bill's base, which reads the same three factors) is the 20–64 cohort × the state's participation rate × (1 − U). The state's
    /// participation rate is a 15-AND-OVER rate by construction: `ParticipationRateTable.StructuralRate` is Σ(band × sourced rate) over the 15+ population,
    /// and `MacroSystem.ApplyLaborForceParticipationRate` reverts the state's rate toward it. So the input multiplies a count on one base by a rate on
    /// another, and an ageing pyramid moves both the same way - the 20–64 window shrinks AND the 15+ rate falls as the old grow - two ageing effects on one
    /// input. The labour force the pyramid implies at the sourced rates by age, Σ(band × rate), is the 15+ population × the structural rate exactly, and
    /// scaled by the levers' deviation it is the 15+ population × the state's rate.
    ///
    /// <para>This run PRINTS, per country, at the seed and after 10 and 25 years of the model's own ageing (a manager advanced, no player): the 20–64
    /// cohort, the 15+ population, the state's rate, the structural rate, Σ(band × rate), the unemployment rate; and the three labour inputs as ratios to
    /// their seeds - the WINDOW form (20–64 × p × (1 − U)), the BAND form (Σ(band × rate) × (1 − U), the sourced rates alone) and the 15+ form (15+ × p × (1 − U)).
    /// It ASSERTS the identities: Σ(band × rate) = 15+ × structural ÷ 100 (the table's own definition), the 15+ form = the band form × (p ÷ structural), and -
    /// since the re-form (§522) - potential's labour input (`PotentialOutput.LabourInput`) IS the 15+ form, at the seed and after ageing. The gap between the
    /// window form and the band form after 25 years was the premise (measured 2026-09-16 before the re-form: −2.2 to −11.4 %) and is printed still, as the
    /// deviation retired. Section 4 measures the same seam in the wage bill's base (`TaxBases`, WageBill), which still reads the window - PN-3b's premise.</para>
    /// </summary>
    public static class LabourInputWindowDiagnostic
    {
        private const int Years = 25;
        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        private static float BandWeighted(Country c)
        {
            float[] rates = ParticipationRateTable.For(c.Id);
            if (rates == null || c.Cohorts?.Counts == null) { return float.NaN; }
            float sum = 0f;
            for (int k = 3; k < PopulationCohorts.CohortCount && k < c.Cohorts.Counts.Length; k++) { sum += c.Cohorts.Counts[k] * rates[k]; }
            return sum;
        }

        private struct Reading { public float Win, Band, Plus, Pop2064, Pop15, P, Structural, U, Older65Share, Input; }

        /// <summary>§522's guard: potential's labour input is the 15+ form - the labour force the pyramid implies at the state's rate, employed - and not the window.
        /// The input is read into the Reading at the same moment as its factors (the first run compared a year-10 reading with the input at year 25).</summary>
        private static void AssertInputIsPlus(Country c, Reading r, string when, ref bool ok)
        {
            if (Math.Abs(r.Input - r.Plus) > 1e-4f * Math.Max(1f, r.Plus))
            {
                ok = false;
                Debug.LogError(F("LABOUR WINDOW: {0} at {1}: potential's labour input {2:N4} m is not the 15+ population at the state's rate, employed ({3:N4} m; the window would read {4:N4} m) - the input is not the pyramid's labour force.", c.Id, when, r.Input, r.Plus, r.Win));
            }
        }

        private static Reading Read(Country c)
        {
            var r = new Reading();
            r.Pop2064 = SpendingDrivers.Level(SpendingDriver.WorkingAge20To64, c);
            r.Pop15 = c.Cohorts != null ? c.Cohorts.InAgeRange(15, 999) : c.State.Population;
            r.P = Mathf.Clamp(c.State.LaborForceParticipationRate, 0f, 100f);
            r.Structural = c.Cohorts != null ? ParticipationRateTable.StructuralRate(c.Id, c.Cohorts.Counts) : float.NaN;
            r.U = Mathf.Clamp(100f - c.State.Unemployment, 0f, 100f) / 100f;
            float band = BandWeighted(c);
            r.Win = r.Pop2064 * r.P / 100f * r.U;
            r.Band = band * r.U;
            r.Plus = r.Pop15 * r.P / 100f * r.U;
            r.Input = PotentialOutput.LabourInput(c);
            // the 65+ bands' share of the band-weighted labour force - the workers the window never counted
            float[] rates = ParticipationRateTable.For(c.Id); float older = 0f;
            if (rates != null && c.Cohorts?.Counts != null) { for (int k = 13; k < PopulationCohorts.CohortCount; k++) { older += c.Cohorts.Counts[k] * rates[k]; } }
            r.Older65Share = band > 0f ? older / band : 0f;
            return r;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            sb.Append("=== LABOUR INPUT WINDOW (PN-3): the 20–64 window against the labour force the pyramid implies - MEASURED, identities asserted, nothing re-formed ===\n");
            bool ok = true;
            var go = new GameObject("LabourInputWindowDiagnostic");
            try
            {
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                var seed = new Dictionary<CountryId, Reading>();
                foreach (Country c in world.Countries) { seed[c.Id] = Read(c); }

                sb.Append("\n    1. THE SEED - the count the window reads, the population the rate is defined on, the two rates, and the 65+ share of the labour force the sourced rates imply\n");
                foreach (Country c in world.Countries)
                {
                    Reading r = seed[c.Id];
                    sb.Append(F("    {0,-8} 20–64 {1:N3} m · 15+ {2:N3} m · state p {3:F3} % · structural {4:F3} % · Σ(band × rate) {5:N3} m · U {6:F2} % · window input {7:N3} m · band input {8:N3} m · 15+ input {9:N3} m · the 65+ bands {10:P1} of the labour force\n",
                        c.Id, r.Pop2064, r.Pop15, r.P, r.Structural, r.Band / r.U, c.State.Unemployment, r.Win, r.Band, r.Plus, r.Older65Share));
                    float bandLf = r.Band / r.U, plusLf = r.Pop15 * r.Structural / 100f;
                    if (Math.Abs(bandLf - plusLf) > 1e-3f * Math.Max(1f, bandLf)) { ok = false; Debug.LogError(F("LABOUR WINDOW: {0}'s Σ(band × rate) {1:N4} m is not 15+ × structural {2:N4} m - the table's own definition does not hold.", c.Id, bandLf, plusLf)); }
                    AssertInputIsPlus(c, r, "the seed", ref ok);
                }

                var at = new Dictionary<int, Dictionary<CountryId, Reading>>();
                for (int year = 1; year <= Years; year++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    if (year == 10 || year == Years)
                    {
                        var now = new Dictionary<CountryId, Reading>();
                        foreach (Country c in world.Countries) { now[c.Id] = Read(c); }
                        at[year] = now;
                    }
                }
                foreach (int year in new[] { 10, Years })
                {
                    sb.Append(F("\n    {0}. YEAR {1} - the three inputs as ratios to their seeds; the window's gap against the band form is the premise\n", year == 10 ? 2 : 3, year));
                    foreach (Country c in world.Countries)
                    {
                        Reading r = at[year][c.Id], s = seed[c.Id];
                        float win = r.Win / s.Win, band = r.Band / s.Band, plus = r.Plus / s.Plus;
                        float plusFromBand = r.Band * (r.P / r.Structural) / s.Plus;   // the 15+ form IS the band form scaled by the levers' deviation
                        sb.Append(F("    {0,-8} 20–64 x{1:F4} · 15+ x{2:F4} · state p {3:F2} % (structural {4:F2}) · window x{5:F4} · band x{6:F4} · 15+ x{7:F4} · the window against the band {8:+0.00;-0.00} % · the 65+ bands {9:P1}\n",
                            c.Id, r.Pop2064 / s.Pop2064, r.Pop15 / s.Pop15, r.P, r.Structural, win, band, plus, 100f * (win / band - 1f), r.Older65Share));
                        if (Math.Abs(plus - plusFromBand) > 1e-3f) { ok = false; Debug.LogError(F("LABOUR WINDOW: {0} year {1}: the 15+ form x{2:F5} is not the band form scaled by p ÷ structural x{3:F5}.", c.Id, year, plus, plusFromBand)); }
                        AssertInputIsPlus(c, r, "year " + year.ToString(CultureInfo.InvariantCulture), ref ok);
                    }
                }
                sb.Append("\n    THE PREMISE (measured 2026-09-16, before the re-form): where the window's ratio sat below the band form's by more than the levers' deviation explains, the window was counting the pyramid's ageing twice - once in the count it took on a 20–64 base and once in the rate it took on a 15+ base. Since §522 potential's labour input is the 15+ form (asserted above); the window is printed as the deviation retired.\n");

                // 4. PN-3b's premise, measured here and not re-formed: the wage bill's base reads the same window
                sb.Append(F("\n    4. THE WAGE BILL'S BASE (TaxBases, WageBill) at year {0} - the same three factors × the real wage: its labour part against potential's input and against the window\n", Years));
                foreach (Country c in world.Countries)
                {
                    Reading r = at[Years][c.Id], s = seed[c.Id];
                    float wageBillLabour = TaxBases.Level(TaxBaseDriver.WageBill, c) / Mathf.Max(1e-6f, c.State.RealWageIndex / 100f);
                    float wageBillSeed = s.Win;   // the base's labour part at the seed is the window's seed (the real wage index is 100 at the seed)
                    float baseRatio = wageBillLabour / wageBillSeed;
                    sb.Append(F("    {0,-8} the base's labour part x{1:F4} · the window x{2:F4} · potential's input x{3:F4} · the base against potential's input {4:+0.00;-0.00} %\n",
                        c.Id, baseRatio, r.Win / s.Win, r.Plus / s.Plus, 100f * (baseRatio / (r.Plus / s.Plus) - 1f)));
                }
                sb.Append("    The wage bill still reads the window - its own family (PN-3b), opened with these figures; nothing here re-forms it.\n");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            Debug.Log(sb.ToString());
            Debug.Log(ok ? "=== LabourInputWindowDiagnostic: ALL ASSERTIONS PASS ===" : "=== LabourInputWindowDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
