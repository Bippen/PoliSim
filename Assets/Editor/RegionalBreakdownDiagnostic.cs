using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **F1 steps 2–3, measured: the live election runs through the regional layer and produces a real
    /// per-constituency result.**
    ///
    /// <para>⚠ <b>This exists because the claim is easy to fake.</b> "Regional model wired" is satisfied by
    /// a call that returns the national percentages 29 times, and every check in the suite would stay
    /// green while election night declared Stockholm and Skåne identical. **So the measurement is not
    /// "did it run" but "does it VARY, and does it still add up".**</para>
    ///
    /// <para>It is a diagnostic and not a suite check, per R-N5: no defect has cost anything twice here.
    /// It is run when F1 is worked and its numbers are quoted in the record.</para>
    /// </summary>
    public static class RegionalBreakdownDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== F1: the live election through the regional layer ===\n");

            if (!NationalElection.TryPredictShares(CountryId.Sweden, out Dictionary<string, double> shares))
            {
                Debug.LogError("REGIONAL: Sweden's shares could not be predicted at all, so this run measured "
                               + "NOTHING rather than measuring a failure. " + NationalElection.NotHeldReason(CountryId.Sweden));
                CheckExit.Finish(1);
                return;
            }

            double[][] regional = NationalElection.LastRegionalShares;
            string[] names = NationalElection.LastRegionalNames;
            double[] weights = NationalElection.LastRegionalWeights;

            if (regional == null || names == null || weights == null)
            {
                Debug.LogError("REGIONAL: Sweden predicted national shares and produced NO regional breakdown. "
                               + "The layer is not on the live path.");
                CheckExit.Finish(1);
                return;
            }

            var keys = new List<string>(shares.Keys);
            sb.Append(F("    THE ENUMERATION: {0} valkrets, {1} part(ies) predicted, weights summing to {2:N0} valid votes.\n",
                names.Length, keys.Count, Sum(weights)));

            // ⚠ THE MEASUREMENT THAT MATTERS: does the breakdown VARY? A layer returning the national
            // number 29 times is arithmetic wearing a result's clothes, and it would pass every other test.
            double worstSpread = 0.0;
            string worstParty = "-";
            int worstLow = 0;
            int worstHigh = 0;

            for (int p = 0; p < keys.Count; p++)
            {
                double lo = double.MaxValue;
                double hi = double.MinValue;
                int loAt = 0;
                int hiAt = 0;

                for (int r = 0; r < regional.Length; r++)
                {
                    if (regional[r][p] < lo) { lo = regional[r][p]; loAt = r; }
                    if (regional[r][p] > hi) { hi = regional[r][p]; hiAt = r; }
                }

                if (hi - lo > worstSpread)
                {
                    worstSpread = hi - lo;
                    worstParty = keys[p];
                    worstLow = loAt;
                    worstHigh = hiAt;
                }

                sb.Append(F("      {0,-4} national {1,6:P2}   regions {2,6:P2} .. {3,6:P2}   spread {4,5:P2}   ({5} .. {6})\n",
                    keys[p], shares[keys[p]], lo, hi, hi - lo, names[loAt], names[hiAt]));
            }

            sb.Append(F("\n    WIDEST SPREAD: {0} varies {1:P2} across the country, {2} to {3}.\n",
                worstParty, worstSpread, names[worstLow], names[worstHigh]));
            sb.Append(F("    RECONCILIATION: the vote-weighted regional total sits {0:P4} from the national shares\n"
                        + "    it was derived from (the zero-floor is the only thing that can move it).\n",
                NationalElection.LastRegionalWorstAbsError));

            bool varies = worstSpread > 0.01;
            bool reconciles = NationalElection.LastRegionalWorstAbsError < 0.005;

            sb.Append(F("\n    VARIES: {0}   RECONCILES: {1}\n", varies ? "yes" : "NO", reconciles ? "yes" : "NO"));

            if (!varies)
            {
                Debug.LogError("REGIONAL: the breakdown does not VARY - the widest spread across 29 valkrets is "
                               + worstSpread.ToString("P2", CultureInfo.InvariantCulture) + ". ⚠ A regional layer "
                               + "returning the national number 29 times is arithmetic wearing a result's clothes, "
                               + "and election night drawn from it would declare every constituency identical. That "
                               + "is a screen lying while every check stays green.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            if (!reconciles)
            {
                Debug.LogError("REGIONAL: the regional total does NOT reproduce the national shares - worst "
                               + NationalElection.LastRegionalWorstAbsError.ToString("P4", CultureInfo.InvariantCulture)
                               + ". ⚠ Constituencies that do not add up to the headline are two different claims "
                               + "about one election.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static double Sum(double[] v)
        {
            double t = 0.0;
            foreach (double x in v) { t += x; }
            return t;
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
