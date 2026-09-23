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
    /// FT-15 (ruled 2026-09-23, §586): the rating review's SETTLED growth, measured both ways on one no-policy century. The settled path read growth
    /// off the NOMINAL GDP closings until §586 and reads the recorded REAL closing now; this reads both off the same closings at every year's
    /// boundary, runs `CreditRatingSystem.EvaluateFrom` on each, and counts the letters and outlooks the two readings disagree on. It is the
    /// evidence that the new path runs (a real closing exists and is read) and what the ruling changes; the no-policy dump cannot show it, because
    /// the rating's risk premium reads the debt burden, not the letter.
    /// </summary>
    public static class SettledGrowthProbe
    {
        /// <summary>CONVENTION: a century, as the trajectory family reads.</summary>
        private const int Turns = 100;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("=== FT-15: the settled review's growth, real (now) against nominal (before), one no-policy century, seed 777 ===\n");
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("SETTLED-GROWTH");
            int reads = 0, realMissing = 0, letterDiffers = 0, outlookDiffers = 0, inflationFlattered = 0;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                for (int turn = 1; turn <= Turns; turn++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    foreach (Country c in world.Countries)
                    {
                        CreditRatingSystem.GetSettledPosition(c, sim.CurrentDate, out float debt, out float? deficit, out float? realGrowth);
                        System.DateTime settled = ReleaseCalendar.GetCurrentPeriodStart(ClosingStat.Gdp, sim.CurrentDate.AddDays(-1));
                        float? nowN = c.Published.ClosingValue(ClosingStat.Gdp, settled);
                        float? priorN = c.Published.ClosingValue(ClosingStat.Gdp, settled.AddYears(-1));
                        if (!nowN.HasValue || !priorN.HasValue || priorN.Value <= 0f) { continue; }
                        float nominalGrowth = (nowN.Value - priorN.Value) / priorN.Value * 100f;
                        reads++;
                        if (!realGrowth.HasValue) { realMissing++; continue; }
                        CreditRatingSystem.Assessment now = CreditRatingSystem.EvaluateFrom(debt, c.RiskPremiumSensitivity, deficit, realGrowth);
                        CreditRatingSystem.Assessment before = CreditRatingSystem.EvaluateFrom(debt, c.RiskPremiumSensitivity, deficit, nominalGrowth);
                        if (now.Rating != before.Rating) { letterDiffers++; }
                        if (now.Outlook != before.Outlook) { outlookDiffers++; }
                        if ((int)before.Rating < (int)now.Rating || (before.Outlook == RatingOutlook.Positive && now.Outlook != RatingOutlook.Positive)) { inflationFlattered++; }
                        if (turn == 1 || turn == 25 || turn == 50 || turn == 100)
                        {
                            sb.Append(string.Format(inv, "    t{0,-3} {1,-8} growth real {2,6:F2} % / nominal {3,6:F2} %   rating {4} / {5}   outlook {6} / {7}\n",
                                turn, c.Id, realGrowth.Value, nominalGrowth, now.Rating, before.Rating, now.Outlook, before.Outlook));
                        }
                    }
                }
            }
            finally { Object.DestroyImmediate(go); }

            sb.Append(string.Format(inv, "\n    THE ENUMERATION: {0} settled reading(s) over {1} turns and {2} countries; the real closing missing on {3}.\n", reads, Turns, world.Countries.Count, realMissing));
            sb.Append(string.Format(inv, "    The two readings disagree on the LETTER {0} time(s) and on the OUTLOOK {1} time(s); the nominal reading scored the better of the two {2} time(s) - inflation improving a rating, which is what §586 removes.\n", letterDiffers, outlookDiffers, inflationFlattered));
            if (reads == 0 || realMissing == reads)
            {
                Debug.LogError(sb + "SETTLEDGROWTH: no settled real reading was taken - the probe verified NOTHING.");
                CheckExit.Finish(1);
                return;
            }
            Debug.Log(sb + "SETTLEDGROWTH: done.");
            CheckExit.Finish(0);
        }
    }
}
