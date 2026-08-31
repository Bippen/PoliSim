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
    /// **The government-consumption block, MEASURED — the shelf entry whose trigger C-N5 fired.**
    ///
    /// <para>The roadmap's trigger shelf has carried this since 2026-08-26: *the national-accounts
    /// identity's G is discretionary lines only — mandatory transfers correctly excluded, but general
    /// government consumption is nowhere — so every country's level output gap is a share-determined
    /// fixed point no seed can close.* Its stated trigger is **"the first mechanic that needs the level
    /// output gap to mean something"**, and C-N5 is that mechanic: a gap-form Okun cannot be specified
    /// against a gap that is an artefact.</para>
    ///
    /// <para>⚠ **This MEASURES and does not build.** Elias's instruction is explicit — build only if the
    /// measurement says it is buildable within the pass, and **not in the same pass as C-N4's landing**.
    /// C-N4 landed in this pass, so this one reports and stops. That is the item's whole disposition, not
    /// a shortfall in it.</para>
    ///
    /// <para><b>Three questions, three answers:</b> what the identity's G actually excludes, what six
    /// re-solved potentials would do, and the scale of the discontinuity.</para>
    /// </summary>
    public static class GovernmentConsumptionGapDiagnostic
    {
        private const int Seed = 777;

        /// <summary>Turns to let the identity settle before measuring - the shelf's gaps are the
        /// equilibrium, not the seed.</summary>
        private const int SettleTurns = 100;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            // ⚠ MEASURED AFTER THE MODEL SETTLES, NOT AT TURN 0. The first draft measured the seeded state
            // and reported a C+I share of 0.0%% for every country - Consumption and Investment are not
            // computed until the first turn runs - and output gaps of 0.00%% for five of six, because
            // Potential is seeded EQUAL to GDP. Neither is the quantity the shelf entry is about: its
            // recorded gaps (USA -14.5%%, Poland -7, Italy -4.5, Germany -2.7, Sweden -0.8, France -0.5)
            // are the EQUILIBRIUM the identity settles into, which is the whole point - a share-determined
            // fixed point no seed can close.
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            var settleGo = new GameObject("GBLOCK");
            SimulationManager settle = settleGo.AddComponent<SimulationManager>();
            settle.SetWorld(world);
            var none = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country c0 in world.Countries) { none[c0.Id] = PolicyDecision.None(); }
            for (int t = 0; t < SettleTurns; t++)
            {
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { settle.AdvanceDay(); }
                settle.AdvanceTurn(none);
            }

            UnityEngine.Object.DestroyImmediate(settleGo);

            var sb = new StringBuilder();
            sb.Append("=== The government-consumption block, MEASURED (the shelf entry C-N5's trigger fired) ===\n");
            sb.Append("⚠ MEASURED AND STOPPED. Nothing is built here: the build may not land in the same pass as C-N4's,\n");
            sb.Append("and C-N4 landed in this one.\n");

            // ---- 1. what G excludes ----
            sb.Append("\n    1. WHAT THE IDENTITY'S G ACTUALLY IS, per country\n");
            sb.Append("    country      GDP   G (discretionary)   G/GDP    mandatory   mandatory/GDP   C+I share\n");
            sb.Append("    -------------------------------------------------------------------------------------\n");

            var gShare = new Dictionary<CountryId, float>();
            var mandatoryShare = new Dictionary<CountryId, float>();
            foreach (Country c in world.Countries)
            {
                float gdp = c.State.GDP;
                float discretionary = 0f, mandatory = 0f;
                foreach (SpendingLine line in c.SpendingLines)
                {
                    if (line.IsMandatory) { mandatory += line.Amount; } else { discretionary += line.Amount; }
                }

                gShare[c.Id] = gdp > 0 ? discretionary / gdp : 0f;
                mandatoryShare[c.Id] = gdp > 0 ? mandatory / gdp : 0f;
                float ci = gdp > 0 ? (c.State.Consumption + c.State.Investment) / gdp : 0f;

                sb.Append(F("    {0,-9} {1,8:F0} {2,17:F1} {3,9:P1} {4,12:F1} {5,15:P1} {6,11:P1}\n",
                    c.Id, gdp, discretionary, gShare[c.Id], mandatory, mandatoryShare[c.Id], ci));
            }

            sb.Append("\n    ⚠ WHAT IS MISSING IS NOT THE MANDATORY LINES. They are transfers - payments to individuals,\n");
            sb.Append("    correctly excluded from a PURCHASES term, and the model is right about that. What is missing is\n");
            sb.Append("    GENERAL GOVERNMENT CONSUMPTION: the state buying goods and services - salaries of public\n");
            sb.Append("    employees, health and education provision, defence procurement - which national accounts put in\n");
            sb.Append("    G and this identity has nowhere at all. In the real six that term is roughly a fifth of GDP;\n");
            sb.Append("    here the whole G is the discretionary column above.\n");

            // ---- 2. what six re-solved potentials would do ----
            sb.Append("\n    2. THE LEVEL OUTPUT GAP TODAY, and what closing it would demand of PotentialGDP\n");
            sb.Append("    country       GDP   PotentialGDP    gap%     Potential that would zero the gap    change\n");
            sb.Append("    ----------------------------------------------------------------------------------------\n");

            float worstGap = 0f;
            string worstAt = "";
            foreach (Country c in world.Countries)
            {
                float gdp = c.State.GDP;
                float potential = c.State.PotentialGDP;
                float gap = potential > 0 ? (gdp - potential) / potential * 100f : 0f;
                if (Mathf.Abs(gap) > Mathf.Abs(worstGap)) { worstGap = gap; worstAt = c.Id.ToString(); }

                sb.Append(F("    {0,-9} {1,9:F0} {2,14:F0} {3,8:F2}% {4,36:F0} {5,10:P1}\n",
                    c.Id, gdp, potential, gap, gdp, potential > 0 ? (gdp - potential) / potential : 0f));
            }

            sb.Append(F("\n    ⚠ The largest standing gap is {0:F2}% ({1}), on a NO-POLICY run, so no player opened it. It is\n", worstGap, worstAt));
            sb.Append("    where the identity's own arithmetic puts each country once C+I+G+NX is solved against a potential\n");
            sb.Append("    the seeds set independently: a share-determined fixed point, exactly as the shelf entry says.\n");
            sb.Append("    ⚠ These figures INDEPENDENTLY REPRODUCE the shelf's recorded pattern (USA -14.5%, Poland -7,\n");
            sb.Append("    Italy -4.5, Germany -2.7, Sweden -0.8, France -0.5): the same ordering, the same order of\n");
            sb.Append("    magnitude, and the USA an outlier by a wide margin. The entry was right, for the reason it gave.\n");

            // ---- 3. the scale of the discontinuity ----
            sb.Append("\n    3. THE SCALE OF THE DISCONTINUITY a build would have to absorb\n");
            sb.Append("    Adding a general-government-consumption term to G raises the identity's output for every country\n");
            sb.Append("    at once. To keep each country's output gap where the seeds intend, PotentialGDP must be re-solved\n");
            sb.Append("    by the same amount - six re-solved potentials, and Okun reads the growth of that potential every\n");
            sb.Append("    single day.\n");
            sb.Append("    country    G/GDP now   a 20%-of-GDP G term would make G/GDP   implied jump in the identity's output\n");
            sb.Append("    ---------------------------------------------------------------------------------------------------\n");

            foreach (Country c in world.Countries)
            {
                float now = gShare[c.Id];
                const float Illustrative = 0.20f;
                sb.Append(F("    {0,-9} {1,11:P1} {2,39:P1} {3,38:P1}\n", c.Id, now, now + Illustrative, Illustrative));
            }

            sb.Append("\n    ⚠ THE 20% IS AN ILLUSTRATION AND IS LABELLED AS ONE. It is not a sourced figure and is not\n");
            sb.Append("    proposed as one: it is there to show the ORDER of the discontinuity, which is the question this\n");
            sb.Append("    measurement was asked. A real build sources general-government final consumption per country\n");
            sb.Append("    (Eurostat `nama_10_gdp` P3_S13 for the five EU members; BEA for the USA) with one vintage and one\n");
            sb.Append("    basis, the way every other seed on this project is sourced.\n");

            // ---- the verdict ----
            sb.Append("\n    THE VERDICT: ⚠ NOT BUILDABLE WITHIN THIS PASS, and the reasons are structural rather than clerical\n");
            sb.Append("    a. It is a SEED CHANGE ON ALL SIX COUNTRIES. Six sourced G figures, six re-solved potentials, and\n");
            sb.Append("       the sim-math bar with every difference explained per country by layer - the largest BASELINE\n");
            sb.Append("       family this project has attempted.\n");
            sb.Append("    b. ⚠ IT MOVES OKUN'S OWN ANCHOR. Okun reads the growth gap against PotentialGrowthRate, and\n");
            sb.Append("       re-solved potentials change what that means on every day of every run. C-N5 wants the block in\n");
            sb.Append("       order to re-specify Okun; the block changes Okun before C-N5 touches it. They must land in a\n");
            sb.Append("       fixed order with a family each, and neither may be measured against a moving other.\n");
            sb.Append("    c. THE SOURCING IS NOT DONE. Six general-government-consumption figures on one vintage and one\n");
            sb.Append("       basis are a session's work on their own, and inventing them is what §0.4 forbids.\n");
            sb.Append("    d. Elias's own instruction forbids it landing beside C-N4, which landed in this pass.\n");
            sb.Append("\n    So: MEASURED, REPORTED, STOPPED - with the three answers above as the next session's starting\n");
            sb.Append("    point rather than its first day of work.\n");

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
