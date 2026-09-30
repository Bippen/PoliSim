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
    /// §689 (ruled 2026-09-30): **A REGIONAL BREAKDOWN SUMS TO ITS NATIONAL FIGURE, FOR EVERY PARTY.** Asserted: for every party, the
    /// vote-weighted total of its regional shares equals its national share within rounding (<see cref="Tolerance"/>, a billionth of a share -
    /// under half a vote in the largest electorate the game carries). Every chamber the game derives regions for: Sweden's 29 valkretsar on the
    /// seated prior, Germany's sixteen Länder on the 2021 chamber (the snap start) and on the 2025 one. Every national result they are asked for:
    /// the model's own prediction, and seeded random ones - a Dirichlet draw for Sweden, the prediction moved at random for Germany (a uniform draw can ask the CSU for more than Bayern holds), and one where a large party collapses so the floor at zero bites. An impossible result (the CSU at twice Bayern's weight share) must be REPORTED - its residual handed back, never absorbed.
    /// The parties standing in a subset (the CSU, the SSW, the Grüne in 2021's Saarland) are named in the report, since they are the case the
    /// normalisation fixed.
    ///
    /// <para><b>Proved every run, not once:</b> the same assertion is run on a correct breakdown with one party's share moved a point from one
    /// region to another party (each region still summing to one), and must name that party - a check that cannot fail is not a check.</para>
    /// </summary>
    public static class RegionalSumCheck
    {
        /// <summary>Within rounding: a billionth of a share.</summary>
        private const double Tolerance = 1e-9;

        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        /// <summary>Every party whose regions miss its national share by more than the tolerance, with the miss.</summary>
        internal static List<string> Misses(double[] national, double[][] regional, double[] weights, IReadOnlyList<string> keys)
        {
            var misses = new List<string>();
            double total = 0.0;
            foreach (double w in weights) { total += w; }
            for (int p = 0; p < national.Length; p++)
            {
                double sum = 0.0;
                for (int r = 0; r < regional.Length; r++) { sum += regional[r][p] * weights[r]; }
                double miss = sum / total - national[p];
                if (Math.Abs(miss) > Tolerance) { misses.Add(F("{0} {1:+0.000000;-0.000000} pp", keys[p], miss * 100.0)); }
            }
            return misses;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== RegionalSumCheck (§689): every party's regions sum to its national share ===\n");
            int failures = 0, cases = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var random = new System.Random(689);
                void Case(string name, IReadOnlyList<string> keys, RegionalVoteModel.RegionInput[] regions, double[][] prior, double[] national)
                {
                    double[][] regional = RegionalVoteModel.RegionalSharesByUniformSwing(national, regions, prior, out double reported);
                    var weights = new double[regions.Length];
                    for (int r = 0; r < regions.Length; r++) { weights[r] = regions[r].ElectorateWeight; }
                    List<string> misses = Misses(national, regional, weights, keys);
                    var subset = new List<string>();
                    for (int p = 0; p < keys.Count; p++)
                    {
                        int standing = 0;
                        foreach (RegionalVoteModel.RegionInput x in regions) { if (x.PartyAvailable == null || x.PartyAvailable[p]) { standing++; } }
                        if (standing < regions.Length) { subset.Add(F("{0} in {1}", keys[p], standing)); }
                    }
                    cases++;
                    Check(misses.Count == 0, F("{0}: {1} parties over {2} regions, the swing's own residual {3:E1}{4}{5}", name, keys.Count, regions.Length, reported,
                        subset.Count > 0 ? " - standing in a subset: " + string.Join(", ", subset.ToArray()) : string.Empty,
                        misses.Count > 0 ? " - MISSED: " + string.Join("; ", misses.ToArray()) : string.Empty));
                }
                double[] Dirichlet(int n)
                {
                    var v = new double[n];
                    double s = 0.0;
                    for (int i = 0; i < n; i++) { v[i] = -Math.Log(1.0 - random.NextDouble()); s += v[i]; }
                    for (int i = 0; i < n; i++) { v[i] /= s; }
                    return v;
                }
                double[] Perturb(double[] national)
                {
                    // A result the chamber could produce: the prediction moved by up to five points a party, renormalised - a uniform draw can hand the
                    // CSU more than Bayern's whole weight share, which no breakdown of the Länder can reproduce (that case is asserted separately below).
                    var v = new double[national.Length];
                    double s = 0.0;
                    for (int i = 0; i < v.Length; i++) { v[i] = Math.Max(0.0, national[i] + (random.NextDouble() * 2.0 - 1.0) * 0.05 * Math.Min(1.0, national[i] / 0.1)); s += v[i]; }
                    for (int i = 0; i < v.Length; i++) { v[i] /= s; }
                    return v;
                }
                double[] Collapse(double[] national, int party)
                {
                    var v = (double[])national.Clone();
                    double moved = v[party] * 0.9;
                    v[party] -= moved;
                    for (int i = 0; i < v.Length; i++) { if (i != party) { v[i] += moved / (v.Length - 1); } }
                    return v;
                }

                // Sweden, on the seated prior, the model's own prediction first.
                Check(NationalElection.TryPredictShares(CountryId.Sweden, out Dictionary<string, double> se), "Sweden: the model predicts");
                var seKeys = new List<string>(NationalElection.LastRegionalKeys);
                double[] seNational = seKeys.ConvertAll(k => se[k]).ToArray();
                RegionalVoteModel.RegionInput[] seRegions = SwedishRegions.Regions(seKeys);
                double[][] sePrior = SwedishRegions.PriorShares(seKeys);
                Case("Sweden, the model's prediction", seKeys, seRegions, sePrior, seNational);
                for (int i = 0; i < 3; i++) { Case(F("Sweden, random {0}", i + 1), seKeys, seRegions, sePrior, Dirichlet(seKeys.Count)); }
                Case("Sweden, the largest party collapsing (the floor bites)", seKeys, seRegions, sePrior, Collapse(seNational, 0));

                // Germany, on the chamber seated at the snap start and on the 2025 chamber.
                foreach (DateTime on in new[] { new DateTime(2024, 11, 6), new DateTime(2025, 3, 25) })
                {
                    string when = on.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
                    Check(NationalElection.TryPredictShares(CountryId.Germany, out Dictionary<string, double> de, on: on), "Germany " + when + ": the model predicts");
                    var deKeys = new List<string>(NationalElection.LastRegionalKeys);
                    double[] deNational = deKeys.ConvertAll(k => de[k]).ToArray();
                    RegionalVoteModel.RegionInput[] deRegions = GermanRegions.Regions(deKeys, on);
                    double[][] dePrior = GermanRegions.PriorShares(deKeys, on);
                    Case("Germany " + when + ", the model's prediction", deKeys, deRegions, dePrior, deNational);
                    for (int i = 0; i < 3; i++) { Case(F("Germany {0}, the prediction moved at random {1}", when, i + 1), deKeys, deRegions, dePrior, Perturb(deNational)); }
                    int csu = deKeys.IndexOf("CSU");
                    if (csu >= 0) { Case("Germany " + when + ", the CSU collapsing (a one-Land party's floor)", deKeys, deRegions, dePrior, Collapse(deNational, csu)); }
                    if (csu >= 0)
                    {
                        // An impossible result is REPORTED, not absorbed: the CSU at twice Bayern's weight share cannot be rebuilt from Bayern alone.
                        double bayern = 0.0, all = 0.0;
                        foreach (RegionalVoteModel.RegionInput x in deRegions) { all += x.ElectorateWeight; if (x.PartyAvailable[csu]) { bayern += x.ElectorateWeight; } }
                        double[] impossible = (double[])deNational.Clone();
                        double want = Math.Min(0.9, 2.0 * bayern / all), take = want - impossible[csu];
                        impossible[csu] = want;
                        for (int i = 0; i < impossible.Length; i++) { if (i != csu) { impossible[i] = Math.Max(0.0, impossible[i] - take / (impossible.Length - 1)); } }
                        double sum = 0.0; foreach (double v in impossible) { sum += v; }
                        for (int i = 0; i < impossible.Length; i++) { impossible[i] /= sum; }
                        double[][] reg = RegionalVoteModel.RegionalSharesByUniformSwing(impossible, deRegions, dePrior, out double residual);
                        var w = new double[deRegions.Length];
                        for (int r = 0; r < deRegions.Length; r++) { w[r] = deRegions[r].ElectorateWeight; }
                        double rebuiltCsu = 0.0; for (int r = 0; r < reg.Length; r++) { rebuiltCsu += reg[r][csu] * w[r]; }
                        rebuiltCsu /= all;
                        Check(residual > 0.01 && residual + 1e-9 >= Math.Abs(rebuiltCsu - impossible[csu]),
                            F("Germany {0}: an impossible result is reported, not absorbed - the CSU at {1:F1} % nationally against Bayern's {2:F1} % of the weight rebuilds to {3:F1} %, the residual handed back {4:F2} pp", when, impossible[csu] * 100.0, bayern / all * 100.0, rebuiltCsu * 100.0, residual * 100.0));
                    }
                }

                // Every party the returns itemise, the SSW among them (the model's prediction carries no SSW - it has no survey position): 2025's own
                // national result over the itemised parties, broken down on the 2021 chamber's Länder (the snap start) - the SSW in Schleswig-Holstein alone.
                {
                    var keys = new List<string>(PoliSim.Elections.Generated.GermanLandReturns2025.Parties);
                    var national = new double[keys.Count];
                    double itemised = 0.0;
                    for (int p = 0; p < keys.Count; p++) { foreach (long[] row in PoliSim.Elections.Generated.GermanLandReturns2025.Votes) { national[p] += row[p]; } itemised += national[p]; }
                    for (int p = 0; p < keys.Count; p++) { national[p] /= itemised; }
                    DateTime snap = new DateTime(2024, 11, 6);
                    Case("Germany, 2025's itemised result on the 2021 Länder (every party, the SSW too)", keys, GermanRegions.Regions(keys, snap), GermanRegions.PriorShares(keys, snap), national);
                }

                // THE PLANTED ERROR: a correct breakdown with one point of one party moved to another in one region must be caught, by name.
                double[][] correct = RegionalVoteModel.RegionalSharesByUniformSwing(seNational, seRegions, sePrior, out _);
                var planted = new double[correct.Length][];
                for (int r = 0; r < correct.Length; r++) { planted[r] = (double[])correct[r].Clone(); }
                planted[0][0] -= 0.01;
                planted[0][1] += 0.01;
                var seWeights = new double[seRegions.Length];
                for (int r = 0; r < seRegions.Length; r++) { seWeights[r] = seRegions[r].ElectorateWeight; }
                List<string> caught = Misses(seNational, planted, seWeights, seKeys);
                Check(caught.Count == 2 && caught[0].StartsWith(seKeys[0] + " ", StringComparison.Ordinal) && caught[1].StartsWith(seKeys[1] + " ", StringComparison.Ordinal),
                    F("the planted error is caught: one point of {0} moved to {1} in {2} - the assertion names {3}", seKeys[0], seKeys[1], seRegions[0].Name,
                        caught.Count > 0 ? string.Join("; ", caught.ToArray()) : "NOTHING"));
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append(e.Message).Append('\n'); }
            sb.Append(failures == 0 ? F("    CLEAN - {0} breakdowns, every party within {1:E0} of its national share\n", cases, Tolerance) : F("    {0} failure(s)\n", failures));
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
