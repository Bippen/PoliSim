using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Data;
using PoliSim.Data.Generated;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P-I2 stage 1's guard — **the cohort substrate reconciles against its own publishers, and against
    /// the population the model already carried.**
    ///
    /// <para><b>THE ENUMERATION.</b> All six countries. For each: (1) the 21 bands must sum to the
    /// publisher's OWN total, transcribed separately in `PopulationPyramids.PublishedTotal` — Eurostat's
    /// <c>TOTAL</c> age class, the Census file's <c>AGE=999</c> row; (2) every band must be positive and
    /// finite; (3) the derived dependency ratio is REPORTED against the seeded
    /// `Country.BaselineDependencyRatio`; (4) the pyramid total is REPORTED against the seeded
    /// `EconomyState.Population`.</para>
    ///
    /// <para>⚠ <b>(1) is the only clause that FAILS, and it is the only one that can.</b> The sum is a
    /// transcription check with a real adversary: 126 band figures were folded from single years and
    /// typed into source, and the total they are checked against came from a different field of the
    /// dataset. A slip in any one band breaks it. **The guard was proven in both directions before it was
    /// committed** — a single band perturbed by 0.01 M made it fail, naming the country and the size of
    /// the gap.</para>
    ///
    /// <para>⚠ <b>(3) and (4) are FINDINGS, not failures, and the distinction is the whole design.</b>
    /// Spec-let §4.5 predicted the dependency ratio would become exactly computable and that each
    /// country's seeded value would then be either right or wrong. It is now computable and they
    /// disagree. **Failing on that would be failing on a measurement this stage exists to take** — and
    /// worse, it would push toward re-seeding the pyramids to match the old scalars, which is tuning a
    /// sourced figure to pass a gate. The disagreement is the input to the retirement stage, which is
    /// where a scalar is allowed to move and must be explained per country.</para>
    /// </summary>
    public static class CohortSubstrateDiagnostic
    {
        /// <summary>CONVENTION: the reconciliation tolerance, in millions. The bands are stored as float
        /// millions at six decimals, so 21 of them accumulate rounding well below this; 0.001 M is one
        /// thousand people against national totals of 10 to 340 million, which catches a typo in any
        /// digit that matters and does not chase float noise.</summary>
        private const float ReconcileToleranceMillions = 0.001f;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            World world = WorldFactory.CreateDefault();
            var sb = new StringBuilder();
            var failures = new List<string>();

            sb.Append("=== P-I2 stage 1: the cohort substrate ===\n");
            sb.Append("    THE ENUMERATION: all six countries; 21 five-year bands each (0-4 ... 95-99, 100+), millions of\n");
            sb.Append("    persons. Eurostat demo_pjan sex=T time=2024 (1 Jan 2024) for the EU five; US Census PEP vintage\n");
            sb.Append("    2024 nc-est2024-agesex-res.csv POPESTIMATE2024 SEX=0 (1 Jul 2024) for the USA.\n\n");
            sb.Append("    country     bands sum   published    gap  |  old-age dep   seeded  |  pyramid   seeded pop\n");
            sb.Append("    ---------------------------------------------------------------------------------------\n");

            foreach (Country country in world.Countries)
            {
                if (country.Cohorts == null)
                {
                    failures.Add($"{country.Name}: no pyramid seeded");
                    Debug.LogError($"COHORTS: {country.Name} has no age pyramid. Every country the world builds must carry one, "
                                   + "or the substrate is present for some countries and absent for others - which is worse than "
                                   + "absent for all, because the code above it cannot tell.");
                    continue;
                }

                // F2 step 4: the reconciliation is of the SEEDED table against its publisher - the live
                // pyramid on the country has been walked to the epoch (CohortDemographics.WalkToEpoch)
                // and is no longer the 2024 stock. The band checks below stay on the live pyramid.
                float sum = PopulationPyramids.For(country.Id).Total;
                float published = PopulationPyramids.PublishedTotal[country.Id];
                float gap = sum - published;

                for (int i = 0; i < PopulationCohorts.CohortCount; i++)
                {
                    float band = country.Cohorts.Counts[i];
                    if (band > 0f && !float.IsNaN(band) && !float.IsInfinity(band)) { continue; }
                    failures.Add($"{country.Name} band {PopulationCohorts.Label(i)}");
                    Debug.LogError($"COHORTS: {country.Name}'s band {PopulationCohorts.Label(i)} is {band}. Every band of a real "
                                   + "pyramid holds people; a zero or a NaN is a transcription failure, not a small country.");
                }

                if (Mathf.Abs(gap) > ReconcileToleranceMillions)
                {
                    failures.Add($"{country.Name} reconciliation");
                    Debug.LogError($"COHORTS: {country.Name}'s 21 bands sum to {sum:F6} M against the publisher's own total of "
                                   + $"{published:F6} M - a gap of {gap:F6} M. ⚠ The two numbers come from DIFFERENT fields of the "
                                   + "same source and reconciled to the person when they were fetched, so a gap now is a "
                                   + "transcription error in this repository, not a disagreement in the data.");
                }

                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,-10} {1,10:F4} {2,11:F4} {3,7:F4}  |  {4,8:F2} {5,7:F1}  |  {6,8:F3} {7,8:F1}\n",
                    country.Name, sum, published, gap,
                    country.Cohorts.OldAgeDependencyRatio, country.BaselineDependencyRatio,
                    sum, country.State.Population));
            }

            sb.Append("\n    ⚠ THE SPEC-LET WAS WRONG ABOUT THE DEFINITION, and this stage is what found out.\n");
            sb.Append("    §3 specified the replacement as '(0-14 + 65+) / 15-64, the standard definition'. The field the model\n");
            sb.Append("    actually seeds is the OLD-AGE ratio, 65+ / 15-64 - the column above, which lands within ~0.1 of the\n");
            sb.Append("    seed for Sweden, Germany and the USA. The TOTAL ratio reads 60.52, 57.14 and 55.14 for those same\n");
            sb.Append("    three: roughly DOUBLE. Building the derivation on the spec-let's own words would have doubled every\n");
            sb.Append("    country's dependency ratio SILENTLY - the exact class of quiet breakage §4's collision map exists\n");
            sb.Append("    to catch. The spec-let is corrected; the code is not written to match a wrong spec.\n");
            sb.Append("\n    ⚠ FINDINGS, not failures - and deliberately so.\n");
            sb.Append("    The dependency ratio is now EXACTLY computable (spec-let §4.5), so each country's seeded value is\n");
            sb.Append("    either right or wrong and this stage is what finds out. Same for Population against the pyramid's\n");
            sb.Append("    own sum. Failing here would push toward re-seeding a SOURCED pyramid to match an authored scalar,\n");
            sb.Append("    which is tuning a figure to pass a gate. Both disagreements are the retirement stage's input, and\n");
            sb.Append("    that stage is where a scalar may move - with its family explained per country.\n");
            sb.Append("    ⚠ NOTHING IN EconomyState DERIVES FROM THESE BANDS YET. The trajectory dump is run to PROVE the\n");
            sb.Append("    no-policy family did not move, rather than the reasoning being trusted.\n");

            IncomeSection(world, sb, failures);

            if (failures.Count == 0)
            {
                sb.Append("\n    CLEAN - all six pyramids reconcile against their own publishers.\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
                return;
            }

            sb.Append(string.Format(CultureInfo.InvariantCulture, "\n    ⚠ {0} FAILURE(S) - see the errors above.\n", failures.Count));
            Debug.LogError(sb.ToString());
            CheckExit.Finish(1);
        }

        // ---- F4-1 (2026-09-12): the income dimension ------------------------------------------------------------

        /// <summary>The share of the anchor band's published mean the cohort mixture must land within to read "within"; beyond it the line reads DIVERGENT - a FINDING, never a failure (the seeded pyramid's weights are demo_pjan's, the publisher's are EU-SILC's sample weights).</summary>
        private const double MixtureMeanTolerance = 0.05;

        /// <summary>
        /// **F4-1's section - the income dimension, read back and reconciled, and its cross-checks PRINTED and never
        /// fitted (DS-2 (c)).** For each country: the 21 cohorts with the source band each took, the median, the sigma,
        /// the mean recovered from the log-normal and the band's own Gini (2Φ(σ/√2) − 1); then (1) the cohort mixture's
        /// mean - the seeded pyramid's counts weighting each cohort's mean - against the anchor band's published mean
        /// (16+ for the five, 15+ for the USA): within `MixtureMeanTolerance` or DIVERGENT; (2) the mixture's Gini,
        /// integrated numerically from the mixture's own distribution, against `Country.BaselineGini` (Eurostat
        /// `ilc_di12` on equivalised disposable income - the same concept as `ilc_di03`'s for the five; the USA's is a
        /// PERSON concept against a household Gini, so its line reads DIVERGENT BY CONCEPT and is no gate); (3) the
        /// national decile cut-offs (`ilc_di01`) beside the mixture's own quantiles, the five; (4) Sweden's shape against
        /// SCB's class table (the sigma each SCB age band implies from its classes' mean over median - personal earned
        /// income in tkr, a different concept and currency: the SHAPE is compared, nothing else); (5) the USA's per-band
        /// log-normal Gini against the CPS's published per-band Gini.
        ///
        /// <para>⚠ <b>What FAILS and what is a FINDING.</b> A cohort from 15 up without a positive median and finite sigma,
        /// a country whose pyramid took no seeds, a country with no seeded pyramid to weight with - those fail: they
        /// are the catalog not being what it says. Every comparison against a publisher is a finding: DS-2b keeps
        /// `Gini` a calibrated gate with its writer unchanged, and a diagnostic that failed on a divergence it exists
        /// to measure would push toward fitting the shape to the gate - the thing (c) forbids.</para>
        /// </summary>
        private static void IncomeSection(World world, StringBuilder sb, List<string> failures)
        {
            sb.Append("\n=== F4-1: the income dimension - one log-normal per cohort, DERIVED from the publisher's mean and median ===\n");
            sb.Append("    THE ENUMERATION: all six countries, the 21 cohorts each; the five from Eurostat ilc_di03 2024 (income year 2023, EUR,\n");
            sb.Append("    EQUIVALISED net household income per person), the USA from CPS ASEC 2024 PINC-01 (income year 2023, USD, total money\n");
            sb.Append("    income per PERSON 15+ with income). Two concepts, never compared across; the shape (mean / median) is what is read.\n");
            sb.Append(string.Format(CultureInfo.InvariantCulture, "    catalog: {0} ({1}); income year {2}.\n",
                CohortIncomeSeeds.SourcePath, CohortIncomeSeeds.SourceDigest.Substring(0, 16), CohortIncomeSeeds.IncomeYear));

            Dictionary<string, Dictionary<string, double>> deciles = ReadDeciles(failures);
            Dictionary<string, (double Median, double Mean, double Gini)> cpsBands = ReadCpsBands(failures);
            Dictionary<string, (double Median, double MeanLower)> scbBands = ReadScbBands(failures);

            foreach (Country country in world.Countries)
            {
                PopulationCohorts live = country.Cohorts;
                PopulationCohorts seeded = PopulationPyramids.For(country.Id);
                if (live == null || seeded == null) { continue; }   // the stage-1 clause above already failed it
                if (!CohortIncomeSeeds.Median.ContainsKey(country.Id))
                {
                    failures.Add($"{country.Name}: no income seeds");
                    Debug.LogError($"INCOME: {country.Name} has no entry in the income catalog - six countries are seeded or the dimension is absent for some and present for others.");
                    continue;
                }
                if (string.IsNullOrEmpty(live.IncomeUnit) || live.IncomeMedian == null || live.IncomeMedian.Length != PopulationCohorts.CohortCount)
                {
                    failures.Add($"{country.Name}: the pyramid took no income seeds");
                    Debug.LogError($"INCOME: {country.Name}'s pyramid carries no income dimension although the catalog has one - WorldFactory did not apply it, or a clone dropped it.");
                    continue;
                }

                string[] bandOf = CohortIncomeSeeds.SourceBand[country.Id];
                sb.Append(string.Format(CultureInfo.InvariantCulture, "\n    {0} - {1}, {2}\n", country.Name, live.IncomeUnit, live.IncomeConcept));
                sb.Append("    cohort   source    median      sigma      mean    band Gini\n");
                double weight = 0, weightedMean = 0;
                var mus = new List<double>(); var sigmas = new List<double>(); var weights = new List<double>();
                for (int i = 0; i < PopulationCohorts.CohortCount; i++)
                {
                    bool below15 = i * PopulationCohorts.CohortWidth < 15;
                    float med = live.IncomeMedian[i], sig = live.IncomeSigma[i];
                    if (below15)
                    {
                        if (med != 0f || sig != 0f) { failures.Add($"{country.Name} {PopulationCohorts.Label(i)} carries income"); Debug.LogError($"INCOME: {country.Name}'s cohort {PopulationCohorts.Label(i)} carries an income dimension below 15."); }
                        sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-8} {1,-9} {2}\n", PopulationCohorts.Label(i), "-", "no income dimension below 15"));
                        continue;
                    }
                    if (!(med > 0f) || !(sig > 0f) || float.IsInfinity(sig))
                    {
                        failures.Add($"{country.Name} {PopulationCohorts.Label(i)} income");
                        Debug.LogError($"INCOME: {country.Name}'s cohort {PopulationCohorts.Label(i)} reads median {med} sigma {sig} - every cohort from 15 up carries a positive median and a finite sigma or the catalog is not what it says.");
                        continue;
                    }
                    double mean = live.IncomeMean(i);
                    double bandGini = 2.0 * NormalCdf(sig / Math.Sqrt(2.0)) - 1.0;
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-8} {1,-9} {2,9:N0} {3,9:F3} {4,9:N0} {5,10:F3}\n",
                        PopulationCohorts.Label(i), bandOf[i], med, sig, mean, bandGini));
                    double w = seeded.Counts[i];
                    weight += w; weightedMean += w * mean;
                    mus.Add(Math.Log(med)); sigmas.Add(sig); weights.Add(w);
                }
                if (weight <= 0 || mus.Count == 0) { continue; }

                // (1) the mixture's mean against the anchor band's published mean.
                double mixtureMean = weightedMean / weight;
                double anchorMean = CohortIncomeSeeds.AnchorMean[country.Id];
                double meanGap = mixtureMean / anchorMean - 1.0;
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    mixture mean {0:N0} against the published {1} mean {2:N0}: {3:+0.0%;-0.0%} - {4}\n",
                    mixtureMean, CohortIncomeSeeds.AnchorBand[country.Id], anchorMean, meanGap,
                    Math.Abs(meanGap) <= MixtureMeanTolerance ? "within " + (MixtureMeanTolerance * 100).ToString("F0", CultureInfo.InvariantCulture) + " % (the weights are the seeded pyramid's, the publisher's are EU-SILC's)" : "DIVERGENT (a finding: the seeded pyramid's weights against the survey's)"));

                // (2) the mixture's Gini, integrated, against the seeded Gini.
                double[] wn = weights.ToArray(); double wsum = 0; foreach (double w in wn) { wsum += w; } for (int k = 0; k < wn.Length; k++) { wn[k] /= wsum; }
                double mixtureGini = MixtureGini(mus.ToArray(), sigmas.ToArray(), wn, out double[] quantiles);
                bool personConcept = country.Id == CountryId.USA;
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    mixture Gini {0:F1} against the seeded BaselineGini {1:F1} ({2}): {3:+0.0;-0.0} points - {4}\n",
                    mixtureGini * 100.0, country.BaselineGini, personConcept ? "OECD IDD, equivalised household - a different concept from the CPS person figure" : "Eurostat ilc_di12, the same equivalised concept",
                    mixtureGini * 100.0 - country.BaselineGini,
                    personConcept ? "DIVERGENT BY CONCEPT (no gate: person income against a household Gini)" : Math.Abs(mixtureGini * 100.0 - country.BaselineGini) <= 3.0 ? "within 3 points" : "DIVERGENT (a finding - a log-normal per band carries no tail beyond its sigma, and within-band inequality is all the mixture can see)"));

                // (3) the national deciles beside the mixture's quantiles - the five; printed, never fitted.
                string geo = GeoOf(country.Id);
                if (deciles != null && deciles.TryGetValue(geo, out Dictionary<string, double> cuts))
                {
                    sb.Append("    deciles  published ilc_di01 D1..D9 | the mixture's own quantiles (printed beside, never fitted):\n");
                    var line1 = new StringBuilder("             "); var line2 = new StringBuilder("             ");
                    for (int d = 1; d <= 9; d++)
                    {
                        cuts.TryGetValue("D" + d, out double cut);
                        line1.Append(string.Format(CultureInfo.InvariantCulture, "{0,8:N0}", cut));
                        line2.Append(string.Format(CultureInfo.InvariantCulture, "{0,8:N0}", quantiles[d - 1]));
                    }
                    sb.Append(line1).Append('\n').Append(line2).Append('\n');
                }

                // (4) Sweden: the shape against SCB's class table, band by band.
                if (country.Id == CountryId.Sweden && scbBands != null && scbBands.Count > 0)
                {
                    sb.Append("    SCB HE0110 (sammanräknad förvärvsinkomst, tkr, persons 16+ - a PERSON earned-income concept): the sigma each\n");
                    sb.Append("    SCB band implies from its classes (mean over median; the open 1000+ class at its lower bound, so the mean is a floor)\n");
                    sb.Append("    beside the catalog's sigma for the cohort - the shape compared, the levels not:\n");
                    foreach (KeyValuePair<string, (double Median, double MeanLower)> b in scbBands)
                    {
                        int cohort = CohortOfScbBand(b.Key);
                        double scbSigma = b.Value.MeanLower > b.Value.Median ? Math.Sqrt(2.0 * Math.Log(b.Value.MeanLower / b.Value.Median)) : double.NaN;
                        sb.Append(string.Format(CultureInfo.InvariantCulture, "      SCB {0,-6} median {1,6:N0} tkr, mean>= {2,6:N0} -> sigma>= {3,5:F2}  |  catalog cohort {4,-6} ({5}) sigma {6,5:F2}\n",
                            b.Key, b.Value.Median, b.Value.MeanLower, scbSigma, PopulationCohorts.Label(cohort), bandOf[cohort], live.IncomeSigma[cohort]));
                    }
                }

                // (5) the USA: the per-band log-normal Gini against the CPS's published band Gini.
                if (country.Id == CountryId.USA && cpsBands != null && cpsBands.Count > 0)
                {
                    sb.Append("    CPS PINC-01 by band: the log-normal's Gini (from the band's mean over median) beside the published band Gini -\n");
                    sb.Append("    the tail a log-normal cannot carry is the gap:\n");
                    foreach (KeyValuePair<string, (double Median, double Mean, double Gini)> b in cpsBands)
                    {
                        double s = CohortIncomeCatalogGenerator.SigmaOf(b.Value.Mean, b.Value.Median);
                        double lnGini = double.IsNaN(s) ? double.NaN : 2.0 * NormalCdf(s / Math.Sqrt(2.0)) - 1.0;
                        sb.Append(string.Format(CultureInfo.InvariantCulture, "      {0,-8} median {1,7:N0} mean {2,7:N0} -> sigma {3,5:F2}, log-normal Gini {4:F3} | published {5:F3}\n",
                            b.Key, b.Value.Median, b.Value.Mean, s, lnGini, b.Value.Gini));
                    }
                }
            }

            sb.Append("\n    ⚠ FINDINGS, not failures, for every line against a publisher above - DS-2 (c): the deciles and the class tables are\n");
            sb.Append("    printed beside the shape and never fitted to; DS-2b: Gini stays a calibrated gate with its writer unchanged. What\n");
            sb.Append("    fails here is the catalog not being what it says. NOTHING IN EconomyState DERIVES FROM THE DIMENSION - the trajectory\n");
            sb.Append("    dump proves the no-policy family did not move (traj_f2i against traj_en4e).\n");
        }

        private static string GeoOf(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden: return "SE"; case CountryId.Germany: return "DE"; case CountryId.France: return "FR";
                case CountryId.Italy: return "IT"; case CountryId.Poland: return "PL"; default: return "US";
            }
        }

        /// <summary>The SCB band's cohort by its lower edge (16-19 → 15-19, 85+ → 85-89).</summary>
        private static int CohortOfScbBand(string band)
        {
            string lower = band.Split('-', '+')[0];
            int age = int.Parse(lower, CultureInfo.InvariantCulture);
            return Math.Min(PopulationCohorts.OpenBandIndex, age / PopulationCohorts.CohortWidth);
        }

        /// <summary>Abramowitz–Stegun 7.1.26 (|error| below 1.5e-7) - a diagnostic's Φ, not a library's.</summary>
        private static double NormalCdf(double z)
        {
            double t = 1.0 / (1.0 + 0.3275911 * Math.Abs(z) / Math.Sqrt(2.0));
            double x = Math.Abs(z) / Math.Sqrt(2.0);
            double erf = 1.0 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Math.Exp(-x * x);
            return 0.5 * (1.0 + (z < 0 ? -erf : erf));
        }

        /// <summary>
        /// The Gini of a weighted mixture of log-normals, integrated on a log-spaced grid: G = 1 − (1/m) ∫ (1 − F(x))² dx,
        /// with F the mixture's CDF and m its mean; the nine decile quantiles read off the same grid.
        /// </summary>
        private static double MixtureGini(double[] mu, double[] sigma, double[] w, out double[] quantiles)
        {
            double lo = double.MaxValue, hi = 0, mean = 0;
            for (int k = 0; k < mu.Length; k++) { lo = Math.Min(lo, Math.Exp(mu[k])); hi = Math.Max(hi, Math.Exp(mu[k])); mean += w[k] * Math.Exp(mu[k] + 0.5 * sigma[k] * sigma[k]); }
            double xLo = lo * 1e-3, xHi = hi * 1e3;
            const int n = 6000;
            double logStep = Math.Log(xHi / xLo) / n;
            double integral = 0, prevX = 0, prevS = 1.0;   // (1 - F) squared is 1 at x = 0
            quantiles = new double[9];
            int nextDecile = 1;
            for (int g = 0; g <= n; g++)
            {
                double x = xLo * Math.Exp(g * logStep);
                double f = 0;
                for (int k = 0; k < mu.Length; k++) { f += w[k] * NormalCdf((Math.Log(x) - mu[k]) / sigma[k]); }
                double s = (1.0 - f) * (1.0 - f);
                integral += 0.5 * (prevS + s) * (x - prevX);
                prevX = x; prevS = s;
                while (nextDecile <= 9 && f >= nextDecile / 10.0) { quantiles[nextDecile - 1] = x; nextDecile++; }
            }
            return 1.0 - integral / mean;
        }

        private static string DataPath(string relative) => Path.Combine(Directory.GetCurrentDirectory(), relative.Replace('/', Path.DirectorySeparatorChar));

        /// <summary>ilc_di01's cut-offs: geo → (quantile → EUR). Absent file: a failure - the cross-check is part of what S1 put on disk.</summary>
        private static Dictionary<string, Dictionary<string, double>> ReadDeciles(List<string> failures)
        {
            string path = DataPath("ElectionsData/income/income_deciles_2024.csv");
            if (!File.Exists(path)) { failures.Add("deciles file"); Debug.LogError("INCOME: ElectionsData/income/income_deciles_2024.csv is not on disk - the deciles cross-check cannot print."); return null; }
            var result = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] p = lines[i].Split(',');
                if (p.Length < 4) { continue; }
                if (!result.TryGetValue(p[0], out Dictionary<string, double> q)) { q = new Dictionary<string, double>(StringComparer.Ordinal); result[p[0]] = q; }
                if (double.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) { q[p[2]] = v; }
            }
            return result;
        }

        /// <summary>PINC-01's class table: band label → (median, mean, Gini), the published per-band figures - the last three columns.</summary>
        private static Dictionary<string, (double Median, double Mean, double Gini)> ReadCpsBands(List<string> failures)
        {
            string path = DataPath("ElectionsData/income/us_income_classes_by_age_2023.csv");
            if (!File.Exists(path)) { failures.Add("CPS class file"); Debug.LogError("INCOME: ElectionsData/income/us_income_classes_by_age_2023.csv is not on disk - the CPS cross-check cannot print."); return null; }
            var result = new Dictionary<string, (double, double, double)>(StringComparer.Ordinal);
            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                // the band label is quoted and may hold a comma: split after the closing quote
                string line = lines[i];
                int close = line.IndexOf('"', 1);
                if (line.Length == 0 || line[0] != '"' || close < 0) { continue; }
                string[] p = line.Substring(close + 2).Split(',');
                if (p.Length < 4) { continue; }
                string code = p[0];
                if (!code.StartsWith("Y", StringComparison.Ordinal) || code.Contains("GE15") || code == "Y15-64") { continue; }   // the five-year bands and 15-24, 75+; not the aggregates
                if (double.TryParse(p[p.Length - 3], NumberStyles.Float, CultureInfo.InvariantCulture, out double med)
                    && double.TryParse(p[p.Length - 2], NumberStyles.Float, CultureInfo.InvariantCulture, out double mean)
                    && double.TryParse(p[p.Length - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double gini))
                {
                    result[code] = (med, mean, gini);
                }
            }
            return result;
        }

        /// <summary>
        /// SCB's class table summed over the 29 valkretsar: age band → (the class-interpolated median, the mean with the
        /// open 1000+ class at its lower bound - a floor). Classes in tkr; the column set is the file's own header.
        /// </summary>
        private static Dictionary<string, (double Median, double MeanLower)> ReadScbBands(List<string> failures)
        {
            string path = DataPath("ElectionsData/sweden/valkrets_income_by_age_class_2024.csv");
            if (!File.Exists(path)) { failures.Add("SCB class file"); Debug.LogError("INCOME: ElectionsData/sweden/valkrets_income_by_age_class_2024.csv is not on disk - Sweden's cross-check cannot print."); return null; }
            string[] lines = File.ReadAllLines(path);
            string[] header = null;
            var counts = new Dictionary<string, double[]>(StringComparer.Ordinal);
            var order = new List<string>();
            var lower = new List<double>(); var upper = new List<double>();
            foreach (string line in lines)
            {
                if (line.StartsWith("#", StringComparison.Ordinal) || line.Length == 0) { continue; }
                string[] p = line.Split(',');
                if (header == null)
                {
                    header = p;
                    for (int c = 2; c < p.Length; c++)
                    {
                        if (!p[c].StartsWith("tkr_", StringComparison.Ordinal)) { break; }
                        string range = p[c].Substring(4);
                        if (range.EndsWith("+", StringComparison.Ordinal)) { double lo = double.Parse(range.TrimEnd('+'), CultureInfo.InvariantCulture); lower.Add(lo); upper.Add(lo); }
                        else { string[] r = range.Split('-'); lower.Add(double.Parse(r[0], CultureInfo.InvariantCulture)); upper.Add(r.Length > 1 ? double.Parse(r[1], CultureInfo.InvariantCulture) + 1 : lower[lower.Count - 1]); }
                    }
                    continue;
                }
                string band = p[1];
                if (!counts.TryGetValue(band, out double[] acc)) { acc = new double[lower.Count]; counts[band] = acc; order.Add(band); }
                for (int c = 0; c < lower.Count && c + 2 < p.Length; c++) { if (double.TryParse(p[c + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) { acc[c] += v; } }
            }
            var result = new Dictionary<string, (double, double)>(StringComparer.Ordinal);
            foreach (string band in order)
            {
                double[] acc = counts[band]; double total = 0; foreach (double v in acc) { total += v; }
                if (total <= 0) { continue; }
                double half = total / 2.0, cum = 0, median = 0;
                for (int c = 0; c < acc.Length; c++)
                {
                    if (cum + acc[c] >= half) { double frac = acc[c] > 0 ? (half - cum) / acc[c] : 0; median = lower[c] + frac * (upper[c] - lower[c]); break; }
                    cum += acc[c];
                }
                double meanLower = 0;
                for (int c = 0; c < acc.Length; c++) { meanLower += acc[c] * (upper[c] > lower[c] ? 0.5 * (lower[c] + upper[c]) : lower[c]); }
                meanLower /= total;
                result[band] = (median, meanLower);
            }
            return result;
        }
    }
}
