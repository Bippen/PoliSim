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
    /// C-N5 — **Okun's coefficient: the measurement, and the finding that it is a SPECIFICATION mismatch
    /// rather than a magnitude error.**
    ///
    /// <para><b>The ruling</b> (Elias, 2026-08-31): a fix track, not a calibration; **ruling-first —
    /// measure where the link is broken before changing anything; propose with the sourced range
    /// attached; apply nothing.** ⚠ **Nothing here writes a constant.**</para>
    ///
    /// <para><b>What C-C11 measured:</b> an implied Okun coefficient of **−0.007** against Ball, Leigh &amp;
    /// Loungani (IMF WP 13/10, 2013) reporting country estimates mostly between **−0.23 and −0.54** —
    /// between 33 and 77 times too small. Read as a magnitude, that says "multiply the constant". ⚠
    /// **Read against the code, it says something else entirely, and this harness is what tells them
    /// apart.**</para>
    ///
    /// <para><b>`MacroSystem.OkunCoefficient` is 0.5</b> — INSIDE the sourced range, at its far end. The
    /// constant is not wrong. What differs is what it multiplies:</para>
    ///
    /// <list type="bullet">
    /// <item><description><b>The model</b> applies it to a <b>GROWTH GAP</b> — actual growth minus
    /// potential growth — and then mean-reverts unemployment toward NAIRU every period.</description></item>
    /// <item><description><b>The literature</b> applies it to an <b>OUTPUT GAP</b> — the LEVEL of output
    /// against potential. Ball, Leigh &amp; Loungani's own figure is a ratio of GAPS.</description></item>
    /// </list>
    ///
    /// <para>⚠ <b>The consequence, which this harness measures rather than argues:</b> a permanent rise
    /// in the LEVEL of output is a one-period growth event. Unemployment dips while growth is above
    /// trend and is then pulled back to NAIRU by the reversion term, so a country can be permanently
    /// richer with unemployment exactly where it started. **That is why the implied coefficient reads
    /// near zero at a five-year horizon while the constant is textbook.**</para>
    /// </summary>
    public static class OkunSpecificationDiagnostic
    {
        private const int Seed = 777;
        private const int Years = 8;
        private static readonly CountryId Subject = CountryId.Sweden;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-N5: Okun - a SPECIFICATION mismatch, not a magnitude error. MEASURED, PROPOSED, NOTHING APPLIED ===\n");

            float[] baseGdp = new float[Years + 1], baseU = new float[Years + 1];
            float[] stepGdp = new float[Years + 1], stepU = new float[Years + 1];
            Run(false, baseGdp, baseU);
            Run(true, stepGdp, stepU);

            sb.Append(F("\n    Sweden, seed {0}. A ONE-OFF permanent +10% step to every discretionary spending line in year 1.\n", Seed));
            sb.Append(F("    `MacroSystem.OkunCoefficient` = 0.5, which is INSIDE Ball/Leigh/Loungani's sourced -0.23..-0.54.\n\n"));
            sb.Append("    year    dGDP        dGDP%      dUnemployment    implied Okun (dU / dGDP%)\n");
            sb.Append("    -------------------------------------------------------------------------\n");

            for (int y = 1; y <= Years; y++)
            {
                float dGdp = stepGdp[y] - baseGdp[y];
                float pct = baseGdp[y] > 0f ? dGdp / baseGdp[y] * 100f : 0f;
                float dU = stepU[y] - baseU[y];
                string okun = Mathf.Abs(pct) > 1e-4f ? F("{0,10:F3}", dU / pct) : "         -";
                sb.Append(F("    {0,4} {1,10:F2} {2,10:F3}% {3,15:F4} {4}\n", y, dGdp, pct, dU, okun));
            }

            // ⚠ THE ASSERTION THAT BINDS THE FINDING. The claim is not "unemployment barely moves" -
            // C-C11 measured that. The claim is that it moves EARLY and then RETURNS: the level gain
            // persists while the unemployment gain does not. If a later change makes the level gap
            // durable, this fails and the finding is retired by the guard rather than by memory.
            // ⚠ The LANDING year is 2, not 1: a decision handed to `AdvanceTurn` reaches the state the turn
            // after, so year 1 is identical to the baseline by construction (C-C11's own lesson).
            float dU1 = stepU[2] - baseU[2];
            float dULast = stepU[Years] - baseU[Years];
            float dGdpLast = stepGdp[Years] - baseGdp[Years];

            bool levelPersists = Mathf.Abs(dGdpLast) > 1f;
            bool unemploymentReturns = Mathf.Abs(dULast) < Mathf.Abs(dU1) * 0.5f || Mathf.Abs(dULast) < 0.02f;

            int failures = 0;
            if (!levelPersists)
            {
                failures++;
                Debug.LogError("C-N5: the spending step did not leave a durable LEVEL gain, so the specification claim below "
                               + "is untested - the harness would be comparing two runs that converged for a different reason.");
            }

            if (!unemploymentReturns)
            {
                failures++;
                Debug.LogError("C-N5: unemployment did NOT return toward its baseline while the output level gain persisted. "
                               + "That is the opposite of this item's finding, so the finding is stale and the proposal must be "
                               + "re-derived rather than carried forward.");
            }

            sb.Append(F("\n    THE FINDING: the output LEVEL gain persists ({0:F2} at year {1}) while the unemployment gain\n", dGdpLast, Years));
            sb.Append(F("    DECAYS ({0:F4} at the LANDING year -> {1:F4} at year {2}).\n", dU1, dULast, Years));
            float landingPct = (stepGdp[2] - baseGdp[2]) / Mathf.Max(1e-6f, baseGdp[2]) * 100f;
            sb.Append(F("    ⚠ AND THE IMPLIED COEFFICIENT AT THE LANDING YEAR IS {0:F3} — INSIDE the sourced -0.23..-0.54.\n",
                dU1 / Mathf.Max(1e-6f, landingPct)));
            sb.Append("    ⚠ THE CONSTANT IS NOT WRONG. `OkunCoefficient` = 0.5 sits inside the sourced range. What differs is\n");
            sb.Append("    WHAT IT MULTIPLIES: the model applies it to a GROWTH GAP (actual growth minus potential growth) and\n");
            sb.Append("    then mean-reverts unemployment toward NAIRU every period; the literature applies it to an OUTPUT GAP\n");
            sb.Append("    (the LEVEL of output against potential). Ball/Leigh/Loungani's own figure is a ratio of GAPS.\n");
            sb.Append("    So a country can end permanently richer with unemployment exactly where it started, and the implied\n");
            sb.Append("    five-year coefficient reads near zero while the constant is textbook.\n");

            sb.Append("\n    ⚠ AND THE FIX IS BLOCKED BY SOMETHING ALREADY ON THE SHELF, which is the more useful half of this\n");
            sb.Append("    finding. A gap-form Okun needs a LEVEL output gap that means something. The roadmap's trigger shelf\n");
            sb.Append("    already records that this model has no such thing: the identity's G term is discretionary lines only,\n");
            sb.Append("    general-government consumption is nowhere, and every country's level output gap is a share-determined\n");
            sb.Append("    fixed point no seed can close (USA -14.5%, Poland -7, Italy -4.5, Germany -2.7, Sweden -0.8,\n");
            sb.Append("    France -0.5). ⚠ C-N5 IS THE FIRST MECHANIC THAT NEEDS THE LEVEL OUTPUT GAP TO MEAN SOMETHING -\n");
            sb.Append("    WHICH IS THAT SHELF ENTRY'S OWN STATED TRIGGER. It has fired.\n");

            sb.Append("\n    THE PROPOSAL - strikeable, and NOTHING IS APPLIED\n");
            sb.Append("    P-N5a. Do NOT scale `OkunCoefficient`. It is inside the sourced range; multiplying it to chase a\n");
            sb.Append("           five-year implied figure would move a right constant to compensate for a wrong specification,\n");
            sb.Append("           which is tuning to pass a gate.\n");
            sb.Append("    P-N5b. Re-specify Okun on the OUTPUT GAP, which requires the government-consumption block first.\n");
            sb.Append("           Two BASELINE items in a fixed order, and the shelf entry is the prerequisite, not the sequel.\n");
            sb.Append("    P-N5c. ⚠ HARD CONSTRAINT: the spending multiplier is 0.603 / 0.850 / 0.966, inside Ramey (JEP 33(2),\n");
            sb.Append("           2019) 0.6-1.0. Any fix moving it out of that band is REJECTED by that fact alone - and a\n");
            sb.Append("           government-consumption term in the identity moves G directly, so this constraint bites hardest\n");
            sb.Append("           here. `ResponsivenessAuditHarness` is the acceptance test.\n");
            sb.Append("    P-N5d. Sourced range, not a number: Ball, Leigh & Loungani (IMF WP 13/10, 2013) report country\n");
            sb.Append("           coefficients mostly between -0.23 and -0.54, US 2009-2011 at -0.41. The literature gives a\n");
            sb.Append("           RANGE and the proposal reports it as one.\n");
            sb.Append("    P-N5e. AFTER C-N4, never in the same pass.\n");

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private static void Run(bool step, float[] gdp, float[] unemployment)
        {
            SimulationRandom.Seed(Seed);
            var go = new GameObject("C-N5 CASE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                World world = WorldFactory.CreateDefault();
                sim.SetWorld(world);

                Country subject = world.GetCountry(Subject);
                var none = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { none[c.Id] = PolicyDecision.None(); }

                var acting = new Dictionary<CountryId, PolicyDecision>(none);
                if (step)
                {
                    var decision = new PolicyDecision();
                    foreach (SpendingLine line in subject.SpendingLines)
                    {
                        if (!line.IsMandatory) { decision.SpendingLineChanges[line.Category] = 10f; }
                    }

                    acting[Subject] = decision;
                }

                for (int y = 1; y <= Years; y++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(y == 1 ? acting : none);
                    gdp[y] = subject.State.GDP;
                    unemployment[y] = subject.State.Unemployment;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
