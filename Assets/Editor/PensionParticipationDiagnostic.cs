using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PN-1's other half (2026-09-23, §596): THE PARTICIPATION RESPONSE, EXPLAINED PER COUNTRY AND ASSERTED. On a fresh world, for each of the six:
    /// (1) the response is ZERO at the seed year's age - the tables are the seed's structure; (2) the statute's own path after the seed - the
    /// no-policy run's move - year by year: the age, the ages crossed, the step (the sourced rate before the lower age × the hazard, named with its
    /// source), and the structural rate with the response against the pyramid's alone, the pyramid held at the seed so the response is read alone;
    /// (3) the dial: an age set by a bill two years above and two below the seed's, the structural rate's move and the labour force's in percent,
    /// signed (a raised age adds participation, a lowered one removes it); (4) THE PENSION LINE'S SAVING UNCHANGED: the line's driver
    /// (SpendingDrivers.StatutoryPensionAge) is the headcount at or above the age in force, bit for bit, with the response in place;
    /// (5) the one country the source measured at a statutory age - France - against Rabaté &amp; Rochut's +20.9 pp. Exit 1 on any failed assertion.
    /// </summary>
    public static class PensionParticipationDiagnostic
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            void Assert(bool ok, string what) { sb.Append(ok ? "    PASS  " : "    FAIL  ").Append(what).Append('\n'); if (!ok) { failures++; } }
            string F(string format, params object[] args) => string.Format(Inv, format, args);

            sb.Append("=== PensionParticipationDiagnostic (§596): the bands a pension age crosses, per country ===\n");
            sb.Append("    source: Atav, Jongen & Rabate (2021), IZA DP 14150, Table B.1 - employment effect = the rate just before the age x the hazard at it\n\n");
            World world = WorldFactory.CreateDefault();
            foreach (Country c in world.Countries)
            {
                if (!PensionAgeStatute.Has(c.Id) || c.Cohorts == null || ParticipationRateTable.For(c.Id) == null) { sb.Append(F("    {0}: no statute, pyramid or table - no response\n", c.Id)); continue; }
                float[] rates = ParticipationRateTable.For(c.Id);
                float seedAge = PensionParticipationResponse.ReferenceAge(c.Id);
                float hazard = PensionParticipationResponse.Hazard(c.Id);
                string hazardSource = PensionParticipationResponse.HazardIsCountrys(c.Id)
                    ? (c.Id == CountryId.France ? "Rabate & Rochut 2019, the country's own" : "Geyer & Welteke 2019 (an ERA), the country's own")
                    : "the table's median of eight increases (no study of this country in it)";
                int keptYear = c.CalendarYear;
                float keptOverride = c.PensionAgeOverride;
                float pyramidSeed = ParticipationRateTable.StructuralRate(c.Id, c.Cohorts.Counts);
                sb.Append(F("  {0}: the age at the seed {1}; hazard {2:0.###} ({3})\n", c.Id, PensionAgeStatute.Format(seedAge), hazard, hazardSource));

                // (1) zero at the seed
                c.CalendarYear = PensionAgeStatute.SeedYear; c.PensionAgeOverride = -1f;
                float atSeed = ParticipationRateTable.StructuralRate(c);
                Assert(atSeed == pyramidSeed, F("{0}: the response is zero at the seed year ({1:0.0000} == the pyramid's {2:0.0000})", c.Id, atSeed, pyramidSeed));

                // (2) the statute's own path after the seed, the pyramid held
                float lastAge = seedAge;
                for (int year = PensionAgeStatute.SeedYear + 1; year <= 2040; year++)
                {
                    c.CalendarYear = year;
                    float age = PensionAgeStatute.AgeInForce(c, year);
                    if (Mathf.Abs(age - lastAge) < 1e-4f) { continue; }
                    lastAge = age;
                    float with = ParticipationRateTable.StructuralRate(c);
                    float lower = Mathf.Min(seedAge, age);
                    float step = PensionParticipationResponse.RateAtAge(rates, lower - 1f) * hazard;
                    sb.Append(F("      {0}: the statute steps to {1} - the ages {2} to {1} crossed, step {3:0.0} pp ({4:0.0} % before {2} x {5:0.###}); structural {6:0.000} % against the pyramid's {7:0.000} ({8:+0.000;-0.000} pts)\n",
                        year, PensionAgeStatute.Format(age), PensionAgeStatute.Format(lower), 100f * step, 100f * PensionParticipationResponse.RateAtAge(rates, lower - 1f), hazard, with, pyramidSeed, with - pyramidSeed));
                    Assert(age > seedAge ? with > pyramidSeed : with < pyramidSeed, F("{0} {1}: the statute's step moves the structural rate the age's way", c.Id, year));
                }
                if (Mathf.Abs(lastAge - seedAge) < 1e-4f) { sb.Append("      the statute holds the seed's age to 2040 - the no-policy run does not move here\n"); }

                // (3) the dial, two years each way, at the seed year
                c.CalendarYear = PensionAgeStatute.SeedYear;
                foreach (float delta in new[] { 2f, -2f })
                {
                    c.PensionAgeOverride = Mathf.Clamp(seedAge + delta, BudgetBill.PensionAgeMin, BudgetBill.PensionAgeMax);
                    float with = ParticipationRateTable.StructuralRate(c);
                    float lf = with / pyramidSeed - 1f;
                    sb.Append(F("      the dial at {0}: structural {1:0.000} % ({2:+0.000;-0.000} pts), the labour force {3:+0.00;-0.00} %\n", PensionAgeStatute.Format(c.PensionAgeOverride), with, with - pyramidSeed, 100f * lf));
                    Assert(delta > 0f ? with > pyramidSeed : with < pyramidSeed, F("{0}: an age set {1:+0;-0} years moves participation the same way", c.Id, delta));

                    // (4) the line's saving unchanged: the pension line's driver with the response on is its driver with the response suspended, bit for bit
                    float driverOn = SpendingDrivers.Level(SpendingDriver.StatutoryPensionAge, c);
                    float driverOff;
                    try { PensionParticipationResponse.ProbeSuspended = true; driverOff = SpendingDrivers.Level(SpendingDriver.StatutoryPensionAge, c); }
                    finally { PensionParticipationResponse.ProbeSuspended = false; }
                    Assert(driverOn == driverOff, F("{0}: the pension line's driver at {1} is the same with the response on and suspended ({2:0.000000} == {3:0.000000}) - the saving unchanged", c.Id, PensionAgeStatute.Format(c.PensionAgeOverride), driverOn, driverOff));
                }

                // (6) the level shift (the review's D1): the state's rate carries the response the day it moves - with the reversion held at zero, the rate moves by
                // exactly the response's points and the country records them as applied, so the FT-8 split sees the same step in the rate as in the anchor
                c.PensionAgeOverride = Mathf.Clamp(seedAge + 2f, BudgetBill.PensionAgeMin, BudgetBill.PensionAgeMax);
                float keptRate = c.State.LaborForceParticipationRate, keptApplied = c.PensionParticipationApplied;
                float points = ParticipationRateTable.PensionResponsePoints(c);
                float before = c.State.LaborForceParticipationRate;
                PoliSim.Simulation.MacroSystem.ApplyLaborForceParticipationRate(c, 0f);
                float moved = c.State.LaborForceParticipationRate - before;
                Assert(Mathf.Abs(moved - (points - keptApplied)) < 1e-4f && c.PensionParticipationApplied == points,
                    F("{0}: a raised age shifts the state's rate by its response the same day ({1:+0.0000;-0.0000} pts moved, {2:+0.0000;-0.0000} owed) and records it", c.Id, moved, points - keptApplied));
                c.State.LaborForceParticipationRate = keptRate; c.PensionParticipationApplied = keptApplied; c.PensionAgeOverride = -1f;

                // (7) continuity (the review's D2): the dial swept a month at a time over its track - no month may move the structural rate by more than 0.1 pts
                // (the band-edge jump the review found was some 0.5 pts in France for one month on the dial)
                float worst = 0f, worstAt = 0f, last = float.NaN;
                for (int m = 0; m <= 120; m++)
                {
                    c.PensionAgeOverride = BudgetBill.PensionAgeMin + m / 12f;
                    float s = ParticipationRateTable.StructuralRate(c);
                    if (!float.IsNaN(last) && Mathf.Abs(s - last) > worst) { worst = Mathf.Abs(s - last); worstAt = c.PensionAgeOverride; }
                    last = s;
                }
                c.PensionAgeOverride = keptOverride;
                sb.Append(F("      the dial swept 60 to 70 by months: the largest one-month move {0:0.0000} pts, at {1}\n", worst, PensionAgeStatute.Format(worstAt)));
                Assert(worst < 0.1f, F("{0}: no month on the dial moves participation by 0.1 pts or more ({1:0.0000})", c.Id, worst));
                c.CalendarYear = keptYear; c.PensionAgeOverride = keptOverride;
                sb.Append('\n');
            }

            // (5) France against its own study: Rabate & Rochut 2019, the NRA 60 -> 61, +20.9 pp at 45 % x 0.50
            float[] fr = ParticipationRateTable.For(CountryId.France);
            float frStep = 100f * PensionParticipationResponse.RateAtAge(fr, PensionParticipationResponse.ReferenceAge(CountryId.France) - 1f) * PensionParticipationResponse.Hazard(CountryId.France);
            sb.Append(F("  France's step {0:0.0} pp against Rabate & Rochut's measured +20.9 pp (45 % x 0.50 in Table B.1)\n", frStep));
            Assert(Mathf.Abs(frStep - 20.9f) < 5f, "France: the model's step is within 5 pp of the study measured in France");

            sb.Append(failures == 0 ? "\n=== PensionParticipationDiagnostic: ALL ASSERTIONS PASS ===\n" : F("\n=== PensionParticipationDiagnostic: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); } else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }
    }
}
