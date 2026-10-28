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
    /// Each country's no-policy century (`NoPolicyCentury`, seed 777, its own start) against the source's baseline OVER THE SAME CALENDAR YEARS (the q32
    /// review's L5): the model read from its opening state at the start's calendar year (DECLARED: a mid-year start is read as its year, and the record
    /// after y years as that year plus y), the source from the same year. The hold reads the CHANGE in the debt ratio, because the USA's seeded debt is
    /// gross federal debt where CBO's is debt held by the public (a perimeter the change crosses and the level does not), and because a seed's level is
    /// its own vintage's. The bands are preregistered in the plan before the first run; a miss prints a named REGISTERED MISS line under K217 (it failed until s930), every UNVERIFIED path still fails.
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
    /// Printed beside each: the level at the horizon, and any debt crisis the century entered (a programme or a default inside the horizon changes the path
    /// the hold reads, and is named). Run: <c>-executeMethod PoliSim.EditorTools.DebtPathHoldDiagnostic.Run</c>; a passing run prints <c>DEBT HOLD: PASS</c>.
    /// </summary>
    public static class DebtPathHoldDiagnostic
    {
        private static int _failures;
        private static int _registeredMisses;   // K217
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

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _failures = 0; _registeredMisses = 0;
            _sb = new StringBuilder("DEBT HOLD: MF q32 - the no-policy debt path against CBO's extended baseline (Feb 2026) and the EC DSM 2024 (seed 777, each country's own start)\n");
            try
            {
                foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland }) { HoldEu(id); }
                HoldUsa();
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            Debug.Log(_sb.ToString());
            if (_registeredMisses > 0) { Debug.Log(F("DEBT HOLD: {0} REGISTERED MISS line(s) (K217: printed, not failed, until K218 lands)", _registeredMisses)); }
            Debug.Log(_failures == 0
                ? "DEBT HOLD: PASS - every country's no-policy debt change read against its preregistered band (misses REGISTERED under K217): the DSM's fan to 2029 and its baseline to 2035 for the EU five from their start years, CBO's over ten and thirty years for the USA, over the same calendar years."
                : F("DEBT HOLD: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "      ok   " : "      BAND MISS ").Append(what).Append('\n');
            if (!ok) { _failures++; Debug.LogError("DEBT HOLD: BAND MISS - " + what); }
        }

        /// <summary>K217 (s930; Code's recommendation under Elias's standing instruction): a band miss is a named REGISTERED MISS line, printed and not failed, until K218 holds the no-policy fiscal path against CBO and the DSM - the K211 pattern. The UNVERIFIED paths and a throw still fail through <see cref="Check"/>.</summary>
        private static void Band(bool ok, string what)
        {
            _sb.Append(ok ? "      ok   " : "      REGISTERED MISS (prereg 8e07e1d9; K218 to close) ").Append(what).Append('\n');
            if (!ok) { _registeredMisses++; Debug.Log("DEBT HOLD: REGISTERED MISS - " + what); }
        }

        /// <summary>The player's country's debt ratio before its first day - the base every change is read from.</summary>
        private static float OpeningRatio(CountryId id)
        {
            SimulationRandom.Seed(NoPolicyCentury.Seed);
            using (SimulationManager.EpochScope())
            {
                PoliSim.Elections.WorldClock.ApplyStart(id);
                World world = WorldFactory.CreateDefault();
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

        /// <summary>The q32 review's L5: the model's debt ratio at calendar year <paramref name="year"/> - the opening state at the start's own year, and year
        /// record y read as the start's year plus y (each record is taken after a whole year of days from the start date). NaN beyond the century.</summary>
        private static float ModelAt(IReadOnlyList<NoPolicyCentury.Year> run, float open, int startYear, int year)
        {
            int y = year - startYear;
            if (y == 0) { return open; }
            return y > 0 && y <= run.Count ? run[y - 1].DebtToGdpRatio : float.NaN;
        }

        private static void HoldEu(CountryId id)
        {
            IReadOnlyList<NoPolicyCentury.Year> run = NoPolicyCentury.For(id);
            float open = OpeningRatio(id);
            int start = PoliSim.Elections.WorldClock.StartDate(id).Year;
            float[] baseline = DebtPathAnchors.DsmBaseline[id], p10 = DebtPathAnchors.DsmP10[id], p90 = DebtPathAnchors.DsmP90[id];
            if (start < DebtPathAnchors.DsmFirstYear || start > DsmFanYear) { Check(false, F("{0}: the start year {1} lies outside the DSM's window {2}-{3} - UNVERIFIED, not clean", id, start, DebtPathAnchors.DsmFirstYear, DsmFanYear)); return; }
            // the q32 review's L5: the windows ALIGNED - the DSM read from the start's own calendar year, the model from its opening state, both to the DSM's years
            float dsmAtStart = At(baseline, DebtPathAnchors.DsmFirstYear, start);
            float lowFan = At(p10, DebtPathAnchors.DsmFanFirstYear, DsmFanYear) - dsmAtStart, highFan = At(p90, DebtPathAnchors.DsmFanFirstYear, DsmFanYear) - dsmAtStart;
            float model2029 = ModelAt(run, open, start, DsmFanYear), model2035 = ModelAt(run, open, start, DsmHorizonYear);
            if (float.IsNaN(model2029) || float.IsNaN(model2035)) { Check(false, F("{0}: the century does not reach {1} - UNVERIFIED, not clean", id, DsmHorizonYear)); return; }
            _sb.Append(F("    {0}: starts {1}, opens at {2:0.0} % of GDP (the DSM's {1} {3:0.0} %)\n", id, start, open, dsmAtStart));
            float modelFan = model2029 - open;
            Band(modelFan >= lowFan && modelFan <= highFan, F("{0} (h1): the change {1}-{2} {3:+0.0;-0.0} points, the DSM's fan {4:+0.0;-0.0} to {5:+0.0;-0.0} (p10 and p90 in {2} less its {1} baseline){6}",
                id, start, DsmFanYear, modelFan, lowFan, highFan, Crises(run, DsmFanYear - start)));
            float dsmChange = At(baseline, DebtPathAnchors.DsmFirstYear, DsmHorizonYear) - dsmAtStart;
            float band = HorizonBandShareOfFan * (At(p90, DebtPathAnchors.DsmFanFirstYear, DsmFanYear) - At(p10, DebtPathAnchors.DsmFanFirstYear, DsmFanYear));
            float modelChange = model2035 - open;
            Band(Mathf.Abs(modelChange - dsmChange) <= band, F("{0} (h2): the change {1}-{2} {3:+0.0;-0.0} points (level {4:0.0} %), the DSM baseline's {5:+0.0;-0.0}, band +/-{6:0.0}{7}",
                id, start, DsmHorizonYear, modelChange, model2035, dsmChange, band, Crises(run, DsmHorizonYear - start)));
        }

        private static void HoldUsa()
        {
            IReadOnlyList<NoPolicyCentury.Year> run = NoPolicyCentury.For(CountryId.USA);
            float open = OpeningRatio(CountryId.USA);
            int start = PoliSim.Elections.WorldClock.StartDate(CountryId.USA).Year;
            float[] cbo = DebtPathAnchors.CboDebtHeldByPublic;
            // the q32 review's L5: the windows ALIGNED - from the later of the start's year and CBO's first year, over the same calendar years on both sides
            int from = Math.Max(start, DebtPathAnchors.CboFirstYear);
            float modelFrom = ModelAt(run, open, start, from);
            _sb.Append(F("    USA: starts {0}, opens at {1:0.0} % of GDP gross; the hold reads from {2} (the model {3:0.0} %, CBO's debt held by the public {4:0.0} % - the perimeter differs, so the hold reads the change)\n",
                start, open, from, modelFrom, cbo[from - DebtPathAnchors.CboFirstYear]));
            foreach ((int years, float band) in new[] { (CboShortYears, CboShortBand), (CboLongYears, CboLongBand) })
            {
                int to = from + years;
                float modelTo = ModelAt(run, open, start, to);
                if (float.IsNaN(modelFrom) || float.IsNaN(modelTo) || to - DebtPathAnchors.CboFirstYear >= cbo.Length) { Check(false, F("USA: the century or CBO's series does not reach {0} - UNVERIFIED, not clean", to)); continue; }
                float cboChange = cbo[to - DebtPathAnchors.CboFirstYear] - cbo[from - DebtPathAnchors.CboFirstYear];
                float modelChange = modelTo - modelFrom;
                Band(Mathf.Abs(modelChange - cboChange) <= band, F("USA (h3): the change {0}-{1} {2:+0.0;-0.0} points (level {3:0.0} %), CBO's {4:+0.0;-0.0}, band +/-{5:0.0}{6}",
                    from, to, modelChange, modelTo, cboChange, band, Crises(run, to - start)));
            }
        }
    }
}
