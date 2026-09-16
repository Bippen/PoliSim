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
    /// F4-2 (2026-09-13, the plan's S3a): the income tax as its statute's own shape over the cohort substrate's incomes, folded into the revenue
    /// engine as one ratio. It builds and advances worlds, so it belongs to the simulation group. The three requirements Elias set from the
    /// abandoned patch's findings are its three verdicts: (1) THE STATUTE'S OWN CURRENCY - every cohort income is converted before a threshold
    /// is compared, so the share of Swedish income above the skiktgräns and of Polish income above the first bracket is a real number, not
    /// nought (the same shares are printed UNCONVERTED to show the inert reading the patch had); (2) AN EXEMPT BAND STAYS EXEMPT - a +5 shift
    /// leaves the tax at an income inside the exempt band at zero for every shape, and Sweden's yield ratio at +5 is nowhere near the 4.28×
    /// the patch produced; (3) THE RATIO IS ONE AT THE SEED - the schedule's revenue is the anchored figure to the cent for six countries,
    /// Italy's (billed, flat) is one always. Then the readings: the average effective rate at the mean income (15b's figure), the drag a
    /// decade at no policy puts into the ratio per country (signed, the four indexed against Poland's nominal thresholds), and what +5 on
    /// the lever does to the effective rate.
    /// </summary>
    public static class TaxScheduleDiagnostic
    {
        private const int Years = 10;
        private const float ShiftPoints = 5f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var sb = new StringBuilder();
            sb.Append("=== TAX SCHEDULE (F4-2): the income tax as its statute's shape - the incomes in the statute's currency, the exempt bands untouched by the shift, the ratio one at the seed ===\n");

            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            EnergyMarket.BeginTurn(world);
            CountryId[] order = { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.USA };

            // (1) the statutes, and the incomes in their currency
            sb.Append("\n    1. THE STATUTES - the shape, the currency, the first taxed threshold, the 40-44 cohort's median income converted (requirement 1)\n");
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                TaxSchedule.Statute s = TaxSchedule.Of(id);
                double conv = TaxSchedule.StatutePerIncomeUnit(id);
                int cohort = 8;   // ages 40-44
                double medianSource = c.Cohorts != null && c.Cohorts.HasIncome(cohort) ? c.Cohorts.IncomeMedian[cohort] : 0;
                double first = TaxSchedule.FirstBracketThreshold(s);
                sb.Append(F("    {0,-8} {1,-16} {2} · first taxed threshold {3:N0} {2} · {4:F3} {2} per income unit ({5}) · 40-44 median {6:N0} {5} = {7:N0} {2} · {8}\n",
                    id, s.Kind, s.Currency, first, conv, c.Cohorts?.IncomeUnit ?? "?", medianSource, medianSource * conv, s.Source));
            }

            // (2) requirement 1: the share of income above the first taxed threshold, converted and - to show the inert reading - unconverted
            sb.Append("\n    2. REQUIREMENT 1 - the share of aggregate income above the first taxed threshold, in the statute's currency; beside it the share the UNCONVERTED incomes would give\n");
            foreach (CountryId id in order)
            {
                if (!TaxSchedule.Responds(id)) { sb.Append(F("    {0,-8} does not respond (flat) - no threshold to compare\n", id)); continue; }
                Country c = world.GetCountry(id);
                TaxSchedule.Statute s = TaxSchedule.Of(id);
                double first = TaxSchedule.FirstBracketThreshold(s);
                double conv = TaxSchedule.StatutePerIncomeUnit(id);
                double share = TaxSchedule.IncomeShareAbove(c, first, 1.0);
                double unconverted = TaxSchedule.IncomeShareAbove(c, first, 1.0 / Math.Max(1e-9, conv));   // the incomes read as if already in the statute's currency
                sb.Append(F("    {0,-8} above {1:N0} {2}: {3:P2} of income   (unconverted it would read {4:P3})\n", id, first, s.Currency, share, unconverted));
                if (id == CountryId.Sweden && (share < 0.005 || share > 0.5)) { ok = false; Debug.LogError($"TAX SCHEDULE: Sweden's income above the skiktgräns reads {share:P2} - the incomes are not in kronor, or the threshold is wrong."); }
                if (id == CountryId.Poland && share < 0.2) { ok = false; Debug.LogError($"TAX SCHEDULE: Poland's income above the first bracket reads {share:P2} - the incomes are not in złoty."); }
                if (id == CountryId.Sweden && unconverted > 0.001) { ok = false; Debug.LogError($"TAX SCHEDULE: Sweden's UNCONVERTED share above the skiktgräns reads {unconverted:P3} - the conversion is not what makes the difference."); }
            }

            // (3) requirement 3: the ratio is one at the seed, and the revenue is the anchored figure to the cent
            sb.Append("\n    3. REQUIREMENT 3 - the yield ratio at the seed, the schedule's revenue against the anchored figure (the seeded rate × the sourced base)\n");
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                TaxLine line = Find(c);
                if (line == null) { ok = false; Debug.LogError($"TAX SCHEDULE: {id} has no income tax line."); continue; }
                double ratio = TaxSchedule.YieldRatio(c, line, line.Rate);
                float anchored = TaxSchedule.RateSeedOf(line) / 100f * TaxBases.Base(c, TaxType.IncomeTax);
                float revenue = TaxBases.Revenue(c, line);
                sb.Append(F("    {0,-8} seed rate {1:F2} % · seed AER {2:F3} % · ratio {3:F9} · revenue {4:N4} bn against the anchored {5:N4} bn · effective {6:F2} %\n",
                    id, line.RateSeed, c.IncomeTaxSeedAer, ratio, revenue, anchored, TaxBases.EffectiveRate(c, line)));
                if (Math.Abs(ratio - 1.0) > 1e-9) { ok = false; Debug.LogError($"TAX SCHEDULE: {id}'s yield ratio at the seed is {ratio:F9}, not one."); }
                if (Math.Abs(revenue - anchored) > 1e-4f) { ok = false; Debug.LogError($"TAX SCHEDULE: {id}'s schedule revenue {revenue} is not the anchored {anchored} at the seed."); }
                if (TaxSchedule.Responds(id) && c.IncomeTaxSeedAer <= 0f) { ok = false; Debug.LogError($"TAX SCHEDULE: {id} responds but its seed AER was not captured."); }
                if (!TaxSchedule.Responds(id) && c.IncomeTaxSeedAer != 0f) { ok = false; Debug.LogError($"TAX SCHEDULE: {id} does not respond but carries a seed AER of {c.IncomeTaxSeedAer}."); }
            }

            // (4) requirement 2: a +5 shift leaves every exempt band exempt, and the ratio is bounded by the shift's own arithmetic
            sb.Append(F("\n    4. REQUIREMENT 2 - a +{0:F0} shift: the tax inside the exempt band, the yield ratio, its bound 1 + shift × layers ⁄ seed AER (Sweden's 4.28× was the patch's)\n", ShiftPoints));
            var exemptProbe = new Dictionary<CountryId, double> { { CountryId.Sweden, 500000 }, { CountryId.Germany, 10000 }, { CountryId.France, 10000 }, { CountryId.Poland, 20000 }, { CountryId.USA, 15000 } };
            foreach (CountryId id in order)
            {
                if (!TaxSchedule.Responds(id)) { continue; }
                Country c = world.GetCountry(id);
                TaxLine line = Find(c);
                TaxSchedule.Statute s = TaxSchedule.Of(id);
                double probe = exemptProbe[id];
                double before = TaxSchedule.Tax(s, probe, 0, 1.0), after = TaxSchedule.Tax(s, probe, ShiftPoints, 1.0);
                // Sweden's municipal layer taxes the probe (it is not exempt); the STATE band below the skiktgräns must not move: the change is the municipal shift alone
                double expectedChange = s.Kind == TaxScheduleKind.TwoLayer ? ShiftPoints / 100.0 * probe : 0;
                double ratio = TaxSchedule.YieldRatio(c, line, line.Rate + ShiftPoints);
                // the shift lands on every taxed band: one layer everywhere, both of Sweden's (the municipal rate on all income and the state rate above the skiktgräns) - the bound counts the layers
                double layers = s.Kind == TaxScheduleKind.TwoLayer ? 2.0 : 1.0;
                double bound = 1 + ShiftPoints * layers / Math.Max(1e-9, c.IncomeTaxSeedAer);
                sb.Append(F("    {0,-8} tax at {1:N0} {2}: {3:N2} → {4:N2} (the exempt band's change {5:N2}) · ratio at +{6:F0} {7:F4} · bound {8:F4}\n", id, probe, s.Currency, before, after, after - before - expectedChange, ShiftPoints, ratio, bound));
                if (Math.Abs(after - before - expectedChange) > 1e-6) { ok = false; Debug.LogError($"TAX SCHEDULE: {id}'s shift touched the exempt band at {probe} ({before} → {after})."); }
                if (ratio <= 1.0 || ratio > bound + 1e-6) { ok = false; Debug.LogError($"TAX SCHEDULE: {id}'s ratio at +{ShiftPoints} is {ratio:F4}, outside (1, {bound:F4}]."); }
                if (id == CountryId.Sweden && ratio > 1.5) { ok = false; Debug.LogError($"TAX SCHEDULE: Sweden's ratio at +{ShiftPoints} is {ratio:F4} - the state band's exemption is being shifted (the patch's 4.28×)."); }
            }

            // (5) the figure 15b's row prints, and the lever's reach
            sb.Append(F("\n    5. THE FIGURE - the average effective rate at the mean income (15b) and over the distribution; +{0:F0} on the lever moves the effective rate by\n", ShiftPoints));
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                TaxLine line = Find(c);
                double mean = TaxSchedule.AverageIncome(c, 1.0);
                double aerMean = TaxSchedule.AverageEffectiveRateAtMeanIncome(c, line, line.Rate);
                float effNow = TaxBases.EffectiveRate(c, line), effShift = TaxSchedule.EffectiveRate(c, line, line.Rate + ShiftPoints);
                sb.Append(F("    {0,-8} mean income {1:N0} {2} · AER at the mean {3:F2} % · AER over the distribution {4:F2} % · lever {5:F2} → {6:F2} % effective ({7:+0.00;-0.00} points)\n",
                    id, mean, TaxSchedule.Of(id).Currency, aerMean, c.IncomeTaxSeedAer, effNow, effShift, effShift - effNow));
            }

            // (6) the drag: a decade at no policy
            sb.Append(F("\n    6. THE DRAG - the yield ratio after {0} years at no policy: bracket creep, real where the statute indexes its thresholds (SE · DE · FR · US), nominal where it does not (PL); Italy one\n", Years));
            Dictionary<CountryId, (double Ratio, float Effective, double IncomeScale, double ThresholdScale, float Lever, float Seed)> drag = RunNoPolicy(Years);
            foreach (CountryId id in order)
            {
                (double ratio, float effective, double incomeScale, double thresholdScale, float lever, float seed) = drag[id];
                // the player's country (Sweden) is at no policy; an AI country's finance ministry may have moved its lever - the lever and its seed are printed so the ratio is read against them
                sb.Append(F("    {0,-8} incomes ×{1:F4} · thresholds ×{2:F4} · lever {3:F2} (seed {4:F2}) · ratio {5:F4} ({6}) · effective {7:F2} %\n", id, incomeScale, thresholdScale, lever, seed, ratio,
                    Math.Abs(lever - seed) > 1e-4f ? "THE AI MOVED THE LEVER" : ratio > 1.0005 ? "CREEP - the statute takes more of a grown income" : ratio < 0.9995 ? "RELIEF - the thresholds outran the incomes" : "FLAT", effective));
                if (id == CountryId.Italy && Math.Abs(ratio - 1.0) > 1e-9) { ok = false; Debug.LogError($"TAX SCHEDULE: Italy's ratio moved to {ratio} - a billed shape must stay one."); }
            }

            // (7) F4-4: Germany's ramps against § 32a's own arithmetic at every zone end, and one sub-row moved on its own dial
            sb.Append("\n    7. THE SUB-ROWS (F4-4) - Germany's ramps against the formula at the zone ends; Poland's upper band raised 32 → 37 on its own dial\n");
            {
                Country de = world.GetCountry(CountryId.Germany);
                TaxSchedule.Statute s = TaxSchedule.Of(CountryId.Germany);
                foreach (double x in new[] { s.FormulaZone2End, s.FormulaZone3End, s.FormulaZone4End, 400000.0 })
                {
                    double ramps = TaxSchedule.RampTax(s, x), formula = TaxSchedule.StatuteFormulaTax(s, x);   // the ramps explicitly: Tax() reads § 32a verbatim with no sub-row moved
                    sb.Append(F("    Germany  at {0:N0} EUR: the ramps {1:N2}, § 32a {2:N2} - {3:+0.00;-0.00} EUR\n", x, ramps, formula, ramps - formula));
                    if (Math.Abs(ramps - formula) > 1.0) { ok = false; Debug.LogError($"TAX SCHEDULE: Germany's ramps read {ramps:F2} at {x:N0} against the formula's {formula:F2} - more than a euro apart."); }
                }
                List<TaxSchedule.SubRow> rows = TaxSchedule.SubRows(s);
                sb.Append(F("    Germany  sub-rows: {0} - ramp ends DERIVED {1:F2} % and {2:F2} %, the entry rate {3:F0} %\n", rows.Count, TaxSchedule.DeRampEnd2, TaxSchedule.DeRampEnd3, TaxSchedule.DeEntryRate));

                // with no sub-row moved the revenue reads § 32a VERBATIM, to the bit - §490 had put the ramps on every path and moved the no-policy
                // trajectory with no dump; an unmoved line (null, or every entry −1) and a shifted lever both stay on the statute's arithmetic
                var unmoved = new float[rows.Count]; for (int i = 0; i < unmoved.Length; i++) { unmoved[i] = -1f; }
                int verbatim = 0, off = 0;
                foreach (double scale in new[] { 1.0, 1.137 })
                {
                    foreach (double shift in new[] { 0.0, 5.0 })
                    {
                        for (double income = 5000; income <= 450000; income += 3713.7)
                        {
                            double taxable = income - s.FormulaGrund * scale;
                            double expected = Math.Max(0, TaxSchedule.StatuteFormulaTax(s, Math.Floor(income / scale))) * scale + (taxable > 0 ? shift / 100.0 * taxable : 0);
                            foreach (float[] o in new[] { null, unmoved })
                            {
                                double got = TaxSchedule.Tax(s, income, shift, scale, o);
                                if (got == expected) { verbatim++; } else { off++; }
                            }
                        }
                    }
                }
                sb.Append(F("    Germany  with no sub-row moved: {0} of {1} incomes, shifts and scales read § 32a to the bit\n", verbatim, verbatim + off));
                if (off > 0) { ok = false; Debug.LogError($"TAX SCHEDULE: Germany's revenue left § 32a's own arithmetic on {off} unmoved cases - the ramps are on the statute's path."); }
                Country pl = world.GetCountry(CountryId.Poland);
                TaxLine line = Find(pl);
                List<TaxSchedule.SubRow> plRows = TaxSchedule.SubRows(TaxSchedule.Of(CountryId.Poland));
                int upper = -1; for (int i = 0; i < plRows.Count; i++) { if (plRows[i].Kind == TaxSubRowKind.Band && Math.Abs(plRows[i].StatuteRate - 32) < 1e-6) { upper = i; } }
                var overrides = new float[plRows.Count]; for (int i = 0; i < overrides.Length; i++) { overrides[i] = -1f; }
                overrides[upper] = 37f;
                TaxLine moved = line.Clone(); moved.BracketRates = overrides;
                double ratioMoved = TaxSchedule.YieldRatio(pl, moved, moved.Rate);
                double shareAbove = TaxSchedule.IncomeShareAbove(pl, 120000, 1.0);
                sb.Append(F("    Poland   the 32 % band at 37 %: ratio {0:F4} (the band's share of income {1:P2} × 5 points over a seed yield of {2:F2} % says {3:F4}); the lever untouched, the other band untouched\n", ratioMoved, shareAbove, pl.IncomeTaxSeedAer, 1 + 5 * shareAbove / pl.IncomeTaxSeedAer));
                if (ratioMoved <= 1.0 || Math.Abs(ratioMoved - (1 + 5 * shareAbove / pl.IncomeTaxSeedAer)) > 0.01) { ok = false; Debug.LogError($"TAX SCHEDULE: Poland's upper band at 37 reads a ratio of {ratioMoved:F4} - not the band's own share."); }
                double untouched = TaxSchedule.YieldRatio(pl, line, line.Rate);
                if (Math.Abs(untouched - 1.0) > 1e-9) { ok = false; Debug.LogError($"TAX SCHEDULE: the standing line moved after a clone's sub-row was set ({untouched})."); }
            }

            // (8) F4-3 (2026-09-15): THE LEVER'S POINT - what one point on the lever raises at the seed, against the base the AI finance ministry spends a
            // point by (AiFinanceMinistry.RaiseHouseholdRates: points = the shortfall over the income tax's and VAT's bases). The revenue engine anchors the
            // lever's seeded rate on the sourced base (rate × base, D-16) and moves it by the statute's yield ratio, so one point raises the anchored figure
            // over the statute's own yield: a seeded rate far from the statute's average effective rate prices its point wrong by that ratio.
            sb.Append("\n    8. THE LEVER'S POINT (F4-3) - one point on the lever at the seed, % of GDP, against the base a point is spent by; the seeded rate against the statute's yield\n");
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                TaxLine line = Find(c);
                float gdp = Mathf.Max(1f, c.State.NominalGdp);
                float perPoint = (TaxBases.RevenueAtRate(c, TaxType.IncomeTax, line.Rate + 1f) - TaxBases.RevenueAtRate(c, TaxType.IncomeTax, line.Rate)) / gdp * 100f;
                float baseShare = TaxBases.Base(c, TaxType.IncomeTax) / gdp;   // the base per point, as a share of GDP - one point of rate on it is this many per cent of GDP
                sb.Append(F("    {0,-8} seed rate {1:F2} % against the statute's yield {2:F2} % · a point raises {3:F4} % of GDP · the base a point is spent by {4:F4} % of GDP · raised over priced {5:F3}\n",
                    id, line.RateSeed, c.IncomeTaxSeedAer, perPoint, baseShare, baseShare > 0f ? perPoint / baseShare : 0f));
                // F4-5 (2026-09-16): what the schedule would have to be seeded at for that ratio to be one - its own average rate on the income it taxes,
                // with the taxed share it implies and the base that rate derives from the realised revenue this row is sourced on.
                if (TaxSchedule.Responds(id))
                {
                    double onTaxed = TaxSchedule.AverageRateOnTaxedIncome(c);
                    double taxedShare = onTaxed > 0 ? c.IncomeTaxSeedAer / onTaxed : 0.0;
                    double realised = line.RateSeed * baseShare;   // the revenue the row's base and seed stand on, % of GDP - the sourced figure by construction
                    sb.Append(F("    {0,-8} its own average rate on the income it taxes {1:F4} % (taxed share {2:F6}) · that seed derives the base {3:F6} from the realised {4:F6} % of GDP\n",
                        id, onTaxed, taxedShare, onTaxed > 0 ? realised / onTaxed : 0.0, realised));
                }
                TaxSchedule.Statute st = TaxSchedule.Of(id);
                if (TaxSchedule.Responds(id))
                {
                    // F4-3's three verdicts (§514), extended by F4-5 (2026-09-16) to EVERY schedule that responds:
                    // (1) the lever's level is the rate the statute names where it names one every unit of taxed income pays - Sweden's municipal layer -
                    //     and its own average rate on taxed income everywhere else. A blend, or a top rate, is a level nothing in the statute holds.
                    if (st.Kind == TaxScheduleKind.TwoLayer)
                    {
                        if (Math.Abs(line.RateSeed - (float)st.FlatLayerRate) > 1e-4f) { ok = false; Debug.LogError(F("TAX SCHEDULE: {0}'s two-layer line is seeded at {1:F2} %, not its municipal layer's {2:F2} % - the blended rate F4-3 retired is back.", id, line.RateSeed, st.FlatLayerRate)); }
                    }
                    else
                    {
                        double onTaxedSeed = TaxSchedule.AverageRateOnTaxedIncome(c);
                        if (Math.Abs(line.RateSeed - onTaxedSeed) > 0.01) { ok = false; Debug.LogError(F("TAX SCHEDULE: {0}'s line is seeded at {1:F4} % against the schedule's own average rate on the income it taxes, {2:F4} % - either the seeding step did not run or the statute moved under the table (F4-5).", id, line.RateSeed, onTaxedSeed)); }
                    }
                    // (2) the base is the country's own realised revenue over the rate the row is read against.
                    if (TaxBaseTable.IncomeTaxRevenuePctOfGdp.TryGetValue(id, out double sourcedPct))
                    {
                        double anchoredPct = line.RateSeed * TaxBaseTable.BaseShareOfGdp(id, TaxType.IncomeTax);
                        sb.Append(F("    {0,-8} the seeded rate × the sourced base {1:F4} % of GDP against the realised {2:F6} %\n", id, anchoredPct, sourcedPct));
                        if (Math.Abs(anchoredPct - sourcedPct) > 0.002) { ok = false; Debug.LogError(F("TAX SCHEDULE: {0}'s seeded rate × its base reads {1:F4} % of GDP against the realised {2:F6} % - the row was not re-derived for the rate it is read against.", id, anchoredPct, sourcedPct)); }
                    }
                    else { ok = false; Debug.LogError(F("TAX SCHEDULE: {0} responds to a statute but no realised revenue is on record for its income row - a base cannot be derived and nothing states what it stands on (F4-5).", id)); }
                    // (3) one point of the lever raises one point of the base the ministry spends it by.
                    if (baseShare <= 0f || Math.Abs(perPoint / baseShare - 1f) > 0.05f) { ok = false; Debug.LogError(F("TAX SCHEDULE: {0}'s lever point raises {1:F4} % of GDP against the {2:F4} % it is spent by - priced off a rate that is not the statute's yield.", id, perPoint, baseShare)); }
                }
            }

            Debug.Log(sb.ToString());
            Debug.Log(ok ? "=== TaxScheduleDiagnostic: ALL ASSERTIONS PASS ===" : "=== TaxScheduleDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>One world at no policy for a number of years; per country the yield ratio, the effective rate and the two scales at the end.</summary>
        private static Dictionary<CountryId, (double Ratio, float Effective, double IncomeScale, double ThresholdScale, float Lever, float Seed)> RunNoPolicy(int years)
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("TAXSCHEDULE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                }
                var result = new Dictionary<CountryId, (double, float, double, double, float, float)>();
                foreach (Country c in world.Countries)
                {
                    TaxLine line = Find(c);
                    result[c.Id] = (line != null ? TaxSchedule.YieldRatio(c, line, line.Rate) : 1.0, line != null ? TaxBases.EffectiveRate(c, line) : 0f, TaxSchedule.IncomeScale(c), TaxSchedule.ThresholdScale(c), line != null ? line.Rate : 0f, line != null ? line.RateSeed : 0f);
                }
                return result;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static TaxLine Find(Country c) { foreach (TaxLine l in c.TaxLines) { if (l.Type == TaxType.IncomeTax) { return l; } } return null; }
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
