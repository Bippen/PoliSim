using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Data.Generated;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// MF q32 (ruled 2026-10-18 q32: "the no-policy debt path is held against CBO's long-term outlook and the EC Debt Sustainability Monitor"): THE HOLD.
    /// Each country's no-policy debt path (seed 777, its own start) against the source's baseline OVER THE SAME CALENDAR YEARS (the q32
    /// review's L5): the model read from its opening state at the start's calendar year (DECLARED: a mid-year start is read as its year, and the record
    /// after y years as that year plus y), the source from the same year. The hold reads the CHANGE in the debt ratio, because the USA's seeded debt is
    /// gross federal debt where CBO's is debt held by the public (a perimeter the change crosses and the level does not), and because a seed's level is
    /// its own vintage's. The bands are preregistered in the plan before the first run (8e07e1d9, kept by K218's own prereg).
    ///
    /// <para><b>K218 (2026-10-28): THE PATH IS THE SOURCES' PREMISE.</b> CBO projects current law and the DSM holds the structural primary balance
    /// unchanged with the cost of ageing on top; neither projects a government correcting its deficit. The shared no-policy century
    /// (<see cref="NoPolicyCentury.For"/>) is the player's own idle book - its lines hold the real provision the player set, and its revenue carries the fiscal
    /// reaction, which raises collection as the debt rises above the country's seeded comfort level - so the hold reads the NO-POLICY-CHANGE run instead
    /// (<see cref="NoPolicyCentury.Unchanged"/>: the AI's indexation, no ministry, no Finance partner, no fiscal reaction),
    /// and prints the shared century's change beside it for reference, unjudged.</para>
    ///
    /// <para><b>K252 (2026-10-28): THE STOCK OPENS AT WHAT IT PAID.</b> The stock's effective rate is seeded at the national accounts' implied rate
    /// (`DebtRateSeeds`, the interest paid over the average of the stocks at the ends of the year before and the year) rather than at the issuance rate, and converges on issuance over the stock's maturity;
    /// each country's opening rate is asserted against the seed, and the bands K252's prereg closed are asserted.</para>
    ///
    /// <para><b>The decomposition</b> (K218): for every window the change is split as the DSM splits it - minus the primary balance, the interest, the real
    /// growth effect and the inflation effect (each year's -b(t-1) x g / (1 + n) and -b(t-1) x (n - g) / (1 + n), g real and n nominal growth), and the
    /// remainder (the model's stock-flow: timing within the year, a fund's flows) - and printed against the DSM's own rows and CBO's (its primary balance and
    /// net interest; its growth effect from its GDP, nominal only, and its other means of financing by difference). Printed, never judged.</para>
    /// <list type="bullet">
    /// <item><b>(h1) the EU five, the DSM's fan</b>: the change from the start's year to 2029 inside the DSM's own stochastic band - its 10th and 90th
    /// percentiles in 2029 less its baseline at the start's year, against the model's change from its opening to 2029. SOURCED band where the start is 2024 (the
    /// fan's own base year); DECLARED for a later start, where the band conditioned on 2024 is wider than the fan from the start would be, and for an earlier
    /// one, where the years to 2024 are the DSM's outturns.</item>
    /// <item><b>(h2) the EU five, the DSM's horizon</b>: the change from the start's year to 2035 within half the 2029 fan's width of the DSM baseline's
    /// change. [AUTHORED-DRAFT] band: the fan's width carried to the horizon unwidened.</item>
    /// <item><b>(h3) the USA, CBO's extended baseline</b>: from the later of the start's year and 2026, the change over ten years within 10 points of
    /// CBO's over the same years, over thirty within 25 points. [AUTHORED-DRAFT] bands.</item>
    /// </list>
    /// A band K218 closes FAILS on a miss (K217 ended for it); a band in <see cref="Registered"/> prints a named REGISTERED MISS line with the term its
    /// decomposition names, and does not fail; every UNVERIFIED path fails. Printed beside each: the level at the horizon, and any debt crisis the run entered.
    /// Run: <c>-executeMethod PoliSim.EditorTools.DebtPathHoldDiagnostic.Run</c>; a passing run prints <c>DEBT HOLD: PASS</c>.
    /// </summary>
    public static class DebtPathHoldDiagnostic
    {
        private static int _failures;
        private static int _registeredMisses;
        private static StringBuilder _sb;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static string F(string format, params object[] args) => string.Format(Inv, format, args);

        /// <summary>The fan's horizon year (the DSM's stochastic projections run 2025-2029).</summary>
        private const int DsmFanYear = 2029;
        /// <summary>The DSM baseline's horizon year.</summary>
        private const int DsmHorizonYear = 2035;
        /// <summary>(h2)'s band: half the 2029 fan's width.</summary>
        private const float HorizonBandShareOfFan = 0.5f;
        /// <summary>(h3)'s two horizons and their bands, points of GDP.</summary>
        private const int CboShortYears = 10, CboLongYears = 30;
        private const float CboShortBand = 10f, CboLongBand = 25f;

        /// <summary>
        /// K218: THE BANDS K218 DOES NOT CLOSE (K252 closed France (h1) and Italy (h1), the stock's rate seeded at the national accounts' implied rate) - each prints a REGISTERED MISS line with its reason and does not fail (the K211 / K217 pattern), until the item
        /// its reason names lands. The reason names the decomposition's terms; the figures are the run's own, printed above the line. Keyed "Country (hN)" for
        /// the EU five and "USA (h3) N years" for CBO's two. A key that is not here is a band K218 closes or one that held already: a miss there FAILS.
        /// </summary>
        private static readonly Dictionary<string, string> Registered = new Dictionary<string, string>
        {
            { "Sweden (h2)", "the primary balance - the model's surplus widens where the DSM's stays near balance; interest, growth and inflation follow the DSM's (the caseload lines' cycle and the real wage against GDP: a calibration item)" },
            { "Germany (h2)", "the primary balance (the caseload lines' cycle after 2029), then the interest on the stock (K252's seed at the national accounts' rate, converging on the issuance rate) - a calibration item" },
            { "France (h2)", "the interest on the stock - K252 seeds it at the national accounts' rate, and over the stock's maturity it converges on the issuance rate and the convex spread at the higher debt, above the DSM's implied path - and the primary balance (the opening years' stagnation) - a calibration item" },
            { "Italy (h2)", "the interest on the stock (K252's seed, converging on the issuance rate and the convex spread at the higher debt) and the real growth effect - K276 put Italy's labour input on the Ageing Report's rates by age, but past 2028 the game carries the statute's 67 y 3 m where the Ageing Report keeps indexing it (K276 review 1, M1) - a programme follows" },
            { "Poland (h2)", "the primary balance - larger than the DSM's since K271: the no-policy book re-indexes its thresholds, and the bracket creep that offset the caseload lines' growth is gone (a calibration item); the DSM's stock-flow adjustments to 2026 are not modelled, and the inflation effect is smaller than the DSM's, so it adds to the miss" },
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _failures = 0; _registeredMisses = 0;
            _sb = new StringBuilder("DEBT HOLD: MF q32 / K218 - the no-policy-change debt path against CBO's extended baseline (Feb 2026) and the EC DSM 2024 (seed 777, each country's own start)\n");
            try
            {
                foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland }) { HoldEu(id); }
                HoldUsa();
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            Debug.Log(_sb.ToString());
            if (_registeredMisses > 0) { Debug.Log(F("DEBT HOLD: {0} REGISTERED MISS line(s) (K218: printed, not failed, each with the term its decomposition names)", _registeredMisses)); }
            Debug.Log(_failures == 0
                ? "DEBT HOLD: PASS - every country's no-policy-change debt change read against its preregistered band (the bands K218 does not close REGISTERED, the rest asserted): the DSM's fan to 2029 and its baseline to 2035 for the EU five from their start years, CBO's over ten and thirty years for the USA, over the same calendar years, each decomposed against the source's own."
                : F("DEBT HOLD: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "      ok   " : "      BAND MISS ").Append(what).Append('\n');
            if (!ok) { _failures++; Debug.LogError("DEBT HOLD: BAND MISS - " + what); }
        }

        /// <summary>K218: a band is asserted (<see cref="Check"/>) unless it is in <see cref="Registered"/>, where a miss prints a named REGISTERED MISS line with its
        /// reason and does not fail; a registered band that holds says so, so the registration can be lifted.</summary>
        private static void Band(string key, bool ok, string what)
        {
            if (!Registered.TryGetValue(key, out string reason)) { Check(ok, what); return; }
            if (ok) { _sb.Append("      ok   ").Append(what).Append(" - REGISTERED under K218 and holding: the registration can be lifted\n"); return; }
            _sb.Append("      REGISTERED MISS (K218) ").Append(what).Append(" - ").Append(reason).Append('\n');
            _registeredMisses++;
            Debug.Log("DEBT HOLD: REGISTERED MISS - " + what + " - " + reason);
        }

        /// <summary>The player's country's debt ratio before its first day - the base every change is read from; K252: and the effective rate its stock opens at.</summary>
        private static float OpeningRatio(CountryId id, out float effectiveRate)
        {
            SimulationRandom.Seed(NoPolicyCentury.Seed);
            using (SimulationManager.EpochScope())
            {
                PoliSim.Elections.WorldClock.ApplyStart(id);
                World world = WorldFactory.CreateDefault();
                effectiveRate = world.GetCountry(id).EffectiveDebtInterestRate;
                return world.GetCountry(id).State.DebtToGdpRatio;
            }
        }

        private static string Crises(IReadOnlyList<NoPolicyCentury.Year> run, int years)
        {
            for (int y = 1; y <= Math.Min(years, run.Count); y++)
            {
                if (run[y - 1].Crises > 0) { return F("; a debt crisis by year {0} ({1})", y, run[y - 1].CrisisPhase); }
            }
            return "; no debt crisis";
        }

        private static float At(float[] series, int firstYear, int year) => series[year - firstYear];

        /// <summary>K218: a term rounded to the printed tenth with the sign of zero dropped, so a vanishing remainder prints as +0.0.</summary>
        private static double R(double x) => Math.Round(x, 1) + 0.0;

        /// <summary>The q32 review's L5: the model's debt ratio at calendar year <paramref name="year"/> - the opening state at the start's own year, and year
        /// record y read as the start's year plus y (each record is taken after a whole year of days from the start date). NaN beyond the run.</summary>
        private static float ModelAt(IReadOnlyList<NoPolicyCentury.Year> run, float open, int startYear, int year)
        {
            int y = year - startYear;
            if (y == 0) { return open; }
            return y > 0 && y <= run.Count ? run[y - 1].DebtToGdpRatio : float.NaN;
        }

        /// <summary>K218: the model's change from calendar year <paramref name="from"/> to <paramref name="to"/>, decomposed - minus the primary balance, the
        /// interest, the real growth and inflation effects, the remainder - each summed over the records from+1 .. to.</summary>
        private static string ModelTerms(IReadOnlyList<NoPolicyCentury.Year> run, float open, int startYear, int from, int to)
        {
            double pb = 0, interest = 0, growth = 0, inflation = 0;
            float before = ModelAt(run, open, startYear, from);
            for (int year = from + 1; year <= to; year++)
            {
                NoPolicyCentury.Year r = run[year - startYear - 1];
                double n = r.NominalGrowth / 100.0, g = r.RealGrowth / 100.0;
                pb -= r.PrimaryBalancePercent; interest += r.InterestPercent;
                growth -= before * g / (1.0 + n); inflation -= before * (n - g) / (1.0 + n);
                before = r.DebtToGdpRatio;
            }
            double change = ModelAt(run, open, startYear, to) - ModelAt(run, open, startYear, from);
            return F("-pb {0:+0.0;-0.0} + interest {1:+0.0;-0.0} + real growth {2:+0.0;-0.0} + inflation {3:+0.0;-0.0} + the rest {4:+0.0;-0.0}", R(pb), R(interest), R(growth), R(inflation), R(change - pb - interest - growth - inflation));
        }

        /// <summary>K218: the DSM's own decomposition of its baseline's change over the same years, from its rows (element i is DsmFirstYear + i).</summary>
        private static string DsmTerms(CountryId id, int from, int to)
        {
            double pb = 0, interest = 0, growth = 0, inflation = 0, stockFlow = 0, ageing = 0;
            for (int year = from + 1; year <= to; year++)
            {
                pb -= At(DebtPathAnchors.DsmPrimaryBalance[id], DebtPathAnchors.DsmFirstYear, year);
                ageing += At(DebtPathAnchors.DsmCostOfAgeing[id], DebtPathAnchors.DsmFirstYear, year);
                interest += At(DebtPathAnchors.DsmInterest[id], DebtPathAnchors.DsmFirstYear, year);
                growth += At(DebtPathAnchors.DsmGrowthEffect[id], DebtPathAnchors.DsmFirstYear, year);
                inflation += At(DebtPathAnchors.DsmInflationEffect[id], DebtPathAnchors.DsmFirstYear, year);
                stockFlow += At(DebtPathAnchors.DsmStockFlow[id], DebtPathAnchors.DsmFirstYear, year);
            }
            return F("-pb {0:+0.0;-0.0} (of which the cost of ageing {5:+0.0;-0.0}) + interest {1:+0.0;-0.0} + real growth {2:+0.0;-0.0} + inflation {3:+0.0;-0.0} + stock-flow {4:+0.0;-0.0}", R(pb), R(interest), R(growth), R(inflation), R(stockFlow), R(ageing));
        }

        /// <summary>K218: CBO's own decomposition over the same fiscal years - minus its primary balance, its net interest, the growth effect read off its GDP
        /// (nominal; the table splits no real part) and its other means of financing by difference.</summary>
        private static string CboTerms(int from, int to)
        {
            int first = DebtPathAnchors.CboFirstYear;
            double pb = 0, interest = 0, growth = 0;
            float[] debt = DebtPathAnchors.CboDebtHeldByPublic, gdp = DebtPathAnchors.CboGdpBillions;
            for (int year = from + 1; year <= to; year++)
            {
                double n = gdp[year - first] / (double)gdp[year - 1 - first] - 1.0;
                pb -= DebtPathAnchors.CboPrimaryBalance[year - first]; interest += DebtPathAnchors.CboNetInterest[year - first];
                growth -= debt[year - 1 - first] * n / (1.0 + n);
            }
            double change = debt[to - first] - debt[from - first];
            return F("-pb {0:+0.0;-0.0} + net interest {1:+0.0;-0.0} + nominal growth {2:+0.0;-0.0} + other means of financing {3:+0.0;-0.0}", R(pb), R(interest), R(growth), R(change - pb - interest - growth));
        }

        /// <summary>K252: the stock opens at the national accounts' implied rate (`DebtRateSeeds`), not at the issuance rate ruling R4's sentinel gave it. It reads the world
        /// <see cref="OpeningRatio"/> builds (`WorldFactory.CreateDefault` after `ApplyStart`), not the run's own: <see cref="NoPolicyCentury.Unchanged"/> builds its world the same way, so a
        /// seed lost inside WorldFactory fails here by name, but a path that rebuilt or reset the rate inside the run would not (the K252 review's L2).</summary>
        private static void SeedHeld(CountryId id, float openRate)
        {
            float seed = DebtRateSeeds.ImpliedRatePercent[id];
            Check(openRate == seed, F("{0}: the stock's effective rate opens at {1:0.00} %, the national accounts' {2} implied rate {3:0.00} % (K252)", id, openRate, DebtRateSeeds.InterestYear, seed));
        }

        private static void HoldEu(CountryId id)
        {
            int start = PoliSim.Elections.WorldClock.StartDate(id).Year;
            float[] baseline = DebtPathAnchors.DsmBaseline[id], p10 = DebtPathAnchors.DsmP10[id], p90 = DebtPathAnchors.DsmP90[id];
            if (start < DebtPathAnchors.DsmFirstYear || start > DsmFanYear) { Check(false, F("{0}: the start year {1} lies outside the DSM's window {2}-{3} - UNVERIFIED, not clean", id, start, DebtPathAnchors.DsmFirstYear, DsmFanYear)); return; }
            IReadOnlyList<NoPolicyCentury.Year> run = NoPolicyCentury.Unchanged(id, DsmHorizonYear - start);
            IReadOnlyList<NoPolicyCentury.Year> idle = NoPolicyCentury.For(id);
            float open = OpeningRatio(id, out float openRate);
            // the q32 review's L5: the windows ALIGNED - the DSM read from the start's own calendar year, the model from its opening state, both to the DSM's years
            float dsmAtStart = At(baseline, DebtPathAnchors.DsmFirstYear, start);
            float lowFan = At(p10, DebtPathAnchors.DsmFanFirstYear, DsmFanYear) - dsmAtStart, highFan = At(p90, DebtPathAnchors.DsmFanFirstYear, DsmFanYear) - dsmAtStart;
            float model2029 = ModelAt(run, open, start, DsmFanYear), model2035 = ModelAt(run, open, start, DsmHorizonYear);
            if (float.IsNaN(model2029) || float.IsNaN(model2035)) { Check(false, F("{0}: the run does not reach {1} - UNVERIFIED, not clean", id, DsmHorizonYear)); return; }
            _sb.Append(F("    {0}: starts {1}, opens at {2:0.0} % of GDP (the DSM's {1} {3:0.0} %); the player's own idle book (the shared century, with the fiscal reaction) for reference, unjudged: {4:+0.0;-0.0} to {5}, {6:+0.0;-0.0} to {7}\n",
                id, start, open, dsmAtStart, ModelAt(idle, open, start, DsmFanYear) - open, DsmFanYear, ModelAt(idle, open, start, DsmHorizonYear) - open, DsmHorizonYear));
            SeedHeld(id, openRate);
            foreach (int to in new[] { DsmFanYear, DsmHorizonYear })
            {
                _sb.Append(F("        {0}-{1} the model: {2}\n", start, to, ModelTerms(run, open, start, start, to)));
                _sb.Append(F("        {0}-{1} the DSM:   {2}\n", start, to, DsmTerms(id, start, to)));
            }
            float modelFan = model2029 - open;
            Band(F("{0} (h1)", id), modelFan >= lowFan && modelFan <= highFan, F("{0} (h1): the change {1}-{2} {3:+0.0;-0.0} points, the DSM's fan {4:+0.0;-0.0} to {5:+0.0;-0.0} (p10 and p90 in {2} less its {1} baseline){6}",
                id, start, DsmFanYear, modelFan, lowFan, highFan, Crises(run, DsmFanYear - start)));
            float dsmChange = At(baseline, DebtPathAnchors.DsmFirstYear, DsmHorizonYear) - dsmAtStart;
            float band = HorizonBandShareOfFan * (At(p90, DebtPathAnchors.DsmFanFirstYear, DsmFanYear) - At(p10, DebtPathAnchors.DsmFanFirstYear, DsmFanYear));
            float modelChange = model2035 - open;
            Band(F("{0} (h2)", id), Mathf.Abs(modelChange - dsmChange) <= band, F("{0} (h2): the change {1}-{2} {3:+0.0;-0.0} points (level {4:0.0} %), the DSM baseline's {5:+0.0;-0.0}, band +/-{6:0.0}{7}",
                id, start, DsmHorizonYear, modelChange, model2035, dsmChange, band, Crises(run, DsmHorizonYear - start)));
        }

        private static void HoldUsa()
        {
            int start = PoliSim.Elections.WorldClock.StartDate(CountryId.USA).Year;
            float[] cbo = DebtPathAnchors.CboDebtHeldByPublic;
            // the q32 review's L5: the windows ALIGNED - from the later of the start's year and CBO's first year, over the same calendar years on both sides
            int from = Math.Max(start, DebtPathAnchors.CboFirstYear);
            IReadOnlyList<NoPolicyCentury.Year> run = NoPolicyCentury.Unchanged(CountryId.USA, from + CboLongYears - start);
            IReadOnlyList<NoPolicyCentury.Year> idle = NoPolicyCentury.For(CountryId.USA);
            float open = OpeningRatio(CountryId.USA, out float openRate);
            float modelFrom = ModelAt(run, open, start, from);
            _sb.Append(F("    USA: starts {0}, opens at {1:0.0} % of GDP gross; the hold reads from {2} (the model {3:0.0} %, CBO's debt held by the public {4:0.0} % - the perimeter differs, so the hold reads the change)\n",
                start, open, from, modelFrom, cbo[from - DebtPathAnchors.CboFirstYear]));
            SeedHeld(CountryId.USA, openRate);
            foreach ((int years, float band) in new[] { (CboShortYears, CboShortBand), (CboLongYears, CboLongBand) })
            {
                int to = from + years;
                float modelTo = ModelAt(run, open, start, to);
                if (float.IsNaN(modelFrom) || float.IsNaN(modelTo) || to - DebtPathAnchors.CboFirstYear >= cbo.Length) { Check(false, F("USA: the run or CBO's series does not reach {0} - UNVERIFIED, not clean", to)); continue; }
                float cboChange = cbo[to - DebtPathAnchors.CboFirstYear] - cbo[from - DebtPathAnchors.CboFirstYear];
                float modelChange = modelTo - modelFrom;
                _sb.Append(F("        {0}-{1} the model: {2}; the player's own idle book (the shared century, with the fiscal reaction) for reference, unjudged: {3:+0.0;-0.0}\n",
                    from, to, ModelTerms(run, open, start, from, to), ModelAt(idle, open, start, to) - ModelAt(idle, open, start, from)));
                _sb.Append(F("        {0}-{1} CBO:       {2}\n", from, to, CboTerms(from, to)));
                Band(F("USA (h3) {0} years", years), Mathf.Abs(modelChange - cboChange) <= band, F("USA (h3): the change {0}-{1} {2:+0.0;-0.0} points (level {3:0.0} %), CBO's {4:+0.0;-0.0}, band +/-{5:0.0}{6}",
                    from, to, modelChange, modelTo, cboChange, band, Crises(run, to - start)));
            }
        }
    }
}
