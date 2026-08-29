using System;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PHASE 4 (2026-08-29): the vote-share backtest — the day's centerpiece and the harder
    /// rung above seats-from-votes. Sourced party positions (CHES 2024; GPS-2019 for the USA)
    /// and sourced issue salience (Eurobarometer 105 Spring 2026; Gallup July 2026) go through
    /// <see cref="VoteModel"/> with campaigns OFF; the predicted national vote shares are
    /// compared against the official returns already in `ElectionsData/`.
    ///
    /// ⚠ **DEVIATION IS THE EXPECTED RESULT AND IS THE POINT.** A four-parameter spatial model
    /// with no loyalty, no incumbency, no turnout structure and no regional detail cannot
    /// reproduce six real elections, and a version of it that did would be a fitted curve, not
    /// a model. Two runs are printed for every country: the PRIOR run (zero fitted parameters —
    /// a neutral electorate at the centre of both axes) and the CALIBRATED run (the one pass
    /// the kickoff allows: four numbers per country, every one printed). The gap between them
    /// is itself a reading — a country the prior already fits is one whose party system sits
    /// symmetrically around the electorate's centre.
    ///
    /// The layer each deviation implicates is named per country in the report, not guessed at
    /// here: this harness prints numbers, the report interprets them.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.VoteShareBacktest.Run -logFile &lt;path&gt;`. InvariantCulture on
    /// every printed number (B3). Deterministic: grid quadrature and a grid search, no RNG.
    /// </summary>
    public static class VoteShareBacktest
    {
        private readonly struct Case
        {
            public readonly string Country;
            public readonly VoteModel.PartyPoint[] Parties;
            public readonly double[] ActualPct;
            public readonly double WEcon;
            public readonly string SalienceNote;

            public Case(string country, VoteModel.PartyPoint[] parties, double[] actualPct, double wEcon, string salienceNote)
            {
                Country = country;
                Parties = parties;
                ActualPct = actualPct;
                WEcon = wEcon;
                SalienceNote = salienceNote;
            }
        }

        public static void Run()
        {
            Case[] cases = BuildCases();
            var summary = new StringBuilder();
            summary.Append("=== VoteShareBacktest summary (mean absolute deviation, percentage points) ===\n");

            foreach (Case c in cases)
            {
                // The actual shares are renormalised over the MODELLED parties only (the
                // sourced returns' remainder - minor lists, and any party CHES does not
                // cover - is excluded from both sides, stated per country in the report).
                double actualSum = 0.0;
                foreach (double a in c.ActualPct) { actualSum += a; }
                var actual = new double[c.ActualPct.Length];
                for (int i = 0; i < actual.Length; i++) { actual[i] = c.ActualPct[i] / actualSum; }

                var prior = new VoteModel.Electorate(5.0, 5.0, 2.0, 2.0);
                double[] priorPredicted = VoteModel.PredictShares(c.Parties, prior, c.WEcon);
                double priorMad = VoteModel.MeanAbsoluteDeviationPp(priorPredicted, actual);

                VoteModel.Electorate best = VoteModel.Calibrate(c.Parties, actual, c.WEcon, out double bestMad);
                double[] bestPredicted = VoteModel.PredictShares(c.Parties, best, c.WEcon);

                var sb = new StringBuilder();
                sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "VOTEBACKTEST: {0} - {1} modelled parties, wEcon={2:F2} ({3}); coverage {4:F1}% of the valid vote\n",
                    c.Country, c.Parties.Length, c.WEcon, c.SalienceNote, actualSum));
                sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "  prior      {0}   MAD {1:F2} pp\n", prior, priorMad));
                sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "  calibrated {0}   MAD {1:F2} pp\n", best, bestMad));
                sb.Append("  party        pos(econ,soc)     actual   prior   calib    dev(calib)\n");
                for (int i = 0; i < c.Parties.Length; i++)
                {
                    sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "  {0,-11} ({1,5:F2},{2,5:F2})   {3,6:F2}  {4,6:F2}  {5,6:F2}   {6,6:F2}\n",
                        c.Parties[i].Name, c.Parties[i].Econ, c.Parties[i].Soc,
                        100.0 * actual[i], 100.0 * priorPredicted[i], 100.0 * bestPredicted[i],
                        100.0 * (bestPredicted[i] - actual[i])));
                }

                Debug.Log(sb.ToString());
                summary.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "  {0,-8} prior {1,6:F2}   calibrated {2,6:F2}   [{3}]\n", c.Country, priorMad, bestMad, best));
            }

            Debug.Log(summary.ToString());
            Debug.Log("VOTEBACKTEST: no loyalty, incumbency, turnout structure, regional detail, tactical voting or party-specific constant exists in this model - see VoteModel's class doc for the complete list of what its silence does NOT claim.");
            CheckExit.Finish(0);
        }

        /// <summary>
        /// The sourced inputs, assembled. Positions: CHES 2024 lrecon/galtan
        /// (`ElectionsData/positions/party_positions.md`), GPS-2019 V4/V6 for the USA. Shares:
        /// the official returns in `ElectionsData/&lt;country&gt;/returns_*.md`. Salience weights:
        /// derived from `ElectionsData/salience/issue_salience.md` by the issue-to-axis mapping
        /// the day report logs as an [AUTHORED-DRAFT] call (economic issues -> lrecon;
        /// immigration/crime/democracy/environment/security -> galtan; everything else dropped;
        /// wEcon floored at 0.15 so no country degenerates to a single axis).
        /// </summary>
        private static Case[] BuildCases()
        {
            return new[]
            {
                new Case("SWEDEN", new[]
                {
                    new VoteModel.PartyPoint("S",  3.68, 4.74),
                    new VoteModel.PartyPoint("SD", 6.32, 9.00),
                    new VoteModel.PartyPoint("M",  7.89, 6.47),
                    new VoteModel.PartyPoint("V",  1.89, 2.42),
                    new VoteModel.PartyPoint("C",  7.84, 2.95),
                    new VoteModel.PartyPoint("KD", 7.26, 7.79),
                    new VoteModel.PartyPoint("MP", 3.16, 1.95),
                    new VoteModel.PartyPoint("L",  7.32, 4.47),
                }, new[] { 30.33, 20.54, 19.10, 6.75, 6.71, 5.34, 5.08, 4.61 }, 0.15,
                   "EB105: no economic issue in the Swedish top five - the floor binds"),

                new Case("GERMANY", new[]
                {
                    new VoteModel.PartyPoint("CDU",   6.58, 6.56),
                    new VoteModel.PartyPoint("AfD",   7.63, 9.39),
                    new VoteModel.PartyPoint("SPD",   3.47, 3.61),
                    new VoteModel.PartyPoint("Grune", 3.37, 1.61),
                    new VoteModel.PartyPoint("Linke", 1.37, 2.29),
                    new VoteModel.PartyPoint("CSU",   6.77, 7.54),
                    new VoteModel.PartyPoint("BSW",   2.78, 7.06),
                    new VoteModel.PartyPoint("FDP",   7.58, 3.22),
                }, new[] { 22.55, 20.80, 16.41, 11.61, 8.78, 5.97, 4.98, 4.33 }, 0.80,
                   "EB105: prices 36 + economy 20 vs immigration 14"),

                new Case("POLAND", new[]
                {
                    new VoteModel.PartyPoint("PiS",  2.52, 8.45),
                    new VoteModel.PartyPoint("KO",   6.17, 3.66),
                    new VoteModel.PartyPoint("TD",   5.25, 5.93),
                    new VoteModel.PartyPoint("NL",   2.32, 1.75),
                    new VoteModel.PartyPoint("Konf", 8.96, 8.41),
                }, new[] { 35.38, 30.70, 14.40, 8.61, 7.16 }, 0.54,
                   "EB105: prices 30 vs security 26; TD = mean of Polska2050 and PSL"),

                new Case("ITALY", new[]
                {
                    new VoteModel.PartyPoint("FdI",   6.40, 9.13),
                    new VoteModel.PartyPoint("PD",    2.93, 2.33),
                    new VoteModel.PartyPoint("M5S",   2.87, 3.27),
                    new VoteModel.PartyPoint("Lega",  6.80, 8.87),
                    new VoteModel.PartyPoint("FI",    7.40, 6.07),
                    new VoteModel.PartyPoint("AzIV",  5.21, 3.46),
                    new VoteModel.PartyPoint("AVS",   1.80, 1.70),
                }, new[] { 25.98, 19.04, 15.43, 8.79, 8.11, 7.78, 3.64 }, 0.79,
                   "EB105: prices 31 + economy 21 vs security 14; AVS = mean of SI and EV"),

                new Case("FRANCE", new[]
                {
                    new VoteModel.PartyPoint("RN+UXD", 6.00, 8.36),
                    new VoteModel.PartyPoint("NFP",    2.19, 2.08),
                    new VoteModel.PartyPoint("ENS",    6.18, 4.09),
                    new VoteModel.PartyPoint("LR",     7.82, 7.18),
                }, new[] { 33.22, 28.06, 20.04, 6.57 }, 0.83,
                   "EB105: prices 40 + economy 17 + debt 16 vs crime 15; NFP = mean of LFI, PS, EELV; UXD folded into RN"),

                new Case("USA", new[]
                {
                    new VoteModel.PartyPoint("Dem", 3.73, 2.41),
                    new VoteModel.PartyPoint("Rep", 8.23, 8.30),
                }, new[] { 48.32, 49.80 }, 0.65,
                   "Gallup Jul 2026: economy 11 + cost of living 11 vs immigration 12; GPS-2019 positions, pre-2020 vintage; TWO parties - a degenerate test, included for completeness"),
            };
        }
    }
}
