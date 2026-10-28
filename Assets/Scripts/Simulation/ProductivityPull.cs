using System;
using PoliSim.Data;
using PoliSim.Data.Generated;

namespace PoliSim.Simulation
{
    /// <summary>
    /// K210 (docs/RULINGS_2026-10-23.md; q29's measurement, COMPLETED.md §929): <b>beyond the WEO's horizon the productivity trend is pulled toward the
    /// Ageing Report's (the EU five) and CBO's (the USA) paths.</b>
    ///
    /// <para><b>What it moves.</b> The ANCHOR of the productivity ledger - the term MacroSystem.ApplySectorGrowthEffect adds the infrastructure, sector
    /// and education adjustments to - and nothing else. Through the WEO's last year the anchor is the seed's sourced trend (Country.BasePotentialGrowthRate,
    /// the 2010-2025 mean WorldFactory names), as before. After it the anchor is the source's path plus the seed's distance from that path at the
    /// horizon, the distance closing at <see cref="PullRatePerYear"/>: an error correction whose target moves with the path. The adjustments still
    /// add on top under their own ceiling and the labour input still comes from the pyramid, so a player's spending, the attainment stock and the
    /// demography move growth around the pulled trend exactly as they moved it around the seed's.</para>
    ///
    /// <para><b>The path.</b> LongRunAnchors.ProductivityGrowth: potential GDP per WORKER (the Ageing Report 2024's table II.1.21; CBO's potential labor
    /// force productivity, LTBO March 2025) - the unit the model's labour input counts (PotentialOutput.LabourInput is persons employed), so
    /// potential growth reaches the source's where the model's employment growth equals the source's. The seed's trend is per HOUR (Eurostat
    /// RLPR_HW; the USA's BLS nonfarm business output per hour, a narrower perimeter than CBO's whole economy per member of the potential labour force);
    /// beyond 2031 the Ageing Report's per-hour and per-worker paths differ by at most three hundredths of a point a YEAR (its tables II.1.17 and
    /// II.1.21), and the seed's distance from the path decays at the pull rate, so the perimeters' difference fades with it - stated. The labour
    /// input's perimeter differs too (employment aged 15 and over at the actual unemployment rate, against the Ageing Report's employment 15-74
    /// and CBO's potential labour force), and K210's prereg reads that part directly. After the path's last year its last value holds.</para>
    ///
    /// <para><b>The year read.</b> The finalizer writes the trend at a turn's boundary and the coming turn's days compound it, so the trend is the
    /// path's at the coming span's midpoint - the mean of the boundary's fractional year and the next boundary's, the key a NoPolicyCentury record
    /// gives its span. The day is the BOUNDARY's, passed by the caller: the turn and the preview both pass SimulationManager's coming boundary date
    /// (the preview convention, EN-4e and §716), so a preview drawn on any day of a span shows the anchor the boundary it previews will apply
    /// (the review's M1). No state is added: the anchor is a function of the seed, the sourced path and the boundary's date, so a save, a preview
    /// clone and an impact fork read the same anchor for the same boundary.</para>
    /// </summary>
    public static class ProductivityPull
    {
        /// <summary>[AUTHORED-DRAFT] K210's pull rate: the share of the anchor's distance from the source's path closed a year beyond the WEO's horizon -
        /// DECLARED in K210's prereg (no source gives the model's rate of forgetting its seed's history; the Ageing Report's and CBO's own convergence
        /// is already inside the path it is pulled to). At one half a year the first decade beyond the horizon carries about a seventh of the
        /// horizon's distance on average and the second almost none.</summary>
        public const double PullRatePerYear = 0.5;

        /// <summary>The last year inside the WEO's horizon (LongRunAnchors.WeoLastYear); the pull acts after it.</summary>
        public static int HorizonYear => LongRunAnchors.WeoLastYear;

        /// <summary>True where the country carries a sourced productivity path (the EU five and the USA).</summary>
        public static bool Has(CountryId id) => LongRunAnchors.ProductivityGrowth.ContainsKey(id) && LongRunAnchors.PotentialGrowthFirstYear.ContainsKey(id);

        /// <summary>The fractional year the trend written at <paramref name="boundaryDay"/> is read at: the coming span's midpoint, keyed as
        /// NoPolicyCentury keys a span - the mean of the boundary's fractional year and the next boundary's, a turn of days on.</summary>
        public static double YearAhead(DateTime boundaryDay)
            => 0.5 * (MortalitySchedule.YearOf(boundaryDay) + MortalitySchedule.YearOf(boundaryDay.AddDays(SimulationManager.DaysPerTurn)));

        /// <summary>K210: the productivity ledger's anchor, per cent a year - the seed's trend through the WEO's horizon (the same float, so the play
        /// path's expression is untouched there), the pulled trend after it, for the boundary on <paramref name="boundaryDay"/>.</summary>
        public static float Anchor(Country country, DateTime boundaryDay)
        {
            if (!Has(country.Id)) { return country.BasePotentialGrowthRate; }
            double year = YearAhead(boundaryDay);
            if (year < HorizonYear + 1) { return country.BasePotentialGrowthRate; }
            return (float)AnchorAt(country.Id, country.BasePotentialGrowthRate, year);
        }

        /// <summary>The pulled anchor at a fractional <paramref name="year"/> for a seed trend: the path's value for the calendar year plus the seed's
        /// distance from the path at the horizon, times (1 - PullRatePerYear) per year since the horizon closed.</summary>
        public static double AnchorAt(CountryId id, double seedTrend, double year)
        {
            if (year < HorizonYear + 1) { return seedTrend; }
            double distance = seedTrend - PathAt(id, HorizonYear);
            return PathAt(id, (int)Math.Floor(year)) + distance * Math.Pow(1.0 - PullRatePerYear, year - (HorizonYear + 1));
        }

        /// <summary>The source's productivity growth for a calendar year, per cent; the path's first value before it opens, its last after it ends.</summary>
        public static double PathAt(CountryId id, int year)
        {
            float[] path = LongRunAnchors.ProductivityGrowth[id];
            int index = year - LongRunAnchors.PotentialGrowthFirstYear[id];
            return path[Math.Max(0, Math.Min(path.Length - 1, index))];
        }
    }
}
