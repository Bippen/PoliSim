using System;
using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>One rate a budget moves, placed on the `redistribution` scale (<see cref="SpatialBudgetVote.ScaleAt"/>): where the standing law has it, and
    /// where the bill would put it.</summary>
    public readonly struct SpatialRate
    {
        public readonly TaxType Type;
        /// <summary>The standing rate on the scale (0 = the dial's ceiling, the most redistribution; 10 = its floor).</summary>
        public readonly float StandingAt;
        /// <summary>The bill's rate on the same scale.</summary>
        public readonly float BillAt;
        /// <summary>The rate's move in the political scale the concern builder adds it in (`TaxLine.PointsOf`) - its size weighs the spatial axis (the review's L3).</summary>
        public readonly float Points;

        public SpatialRate(TaxType type, float standingAt, float billAt, float points) { Type = type; StandingAt = standingAt; BillAt = billAt; Points = points; }
    }

    /// <summary>
    /// q16 (Elias's ruling of 2026-10-18, `docs/RULINGS_2026-10-18.md`: "a bloc votes for a budget when it moves policy toward the party's position against the
    /// standing law (spatial)"; the audit's GA-2-6, the US House's budget vote) - THE SPATIAL BUDGET VOTE, built with PS-6 US-23 for the US Congress's budget votes.
    /// <list type="bullet">
    /// <item><b>The place of a rate</b> ([AUTHORED-DRAFT] reading, DECLARED): a tax line's rate is placed on the `redistribution` scale its moves already load
    /// (`ParliamentSystem.GetBudgetBillConcern`) linearly across the line's own dial - its ceiling at 0 ("strongly favors redistribution"), its floor at 10 - so
    /// a party's position on that axis (`StanceModel.Position`, `lrecon` where `redistribution` is unpublished) is the rate it would set, and the standing law
    /// and the bill are points on the same scale. No figure is added: the dial's range is the model's own (`TaxTypeRateRanges`).</item>
    /// <item><b>The vote</b>: the party's term-1 alignment on that axis is how much nearer the bill brings the rates to the party's position than the standing
    /// law holds them, per unit of the bill's move - (d(standing) - d(bill)) / |move|, the distances Euclidean over the rates the bill moves - so a move
    /// straight toward the party's position reads +1, straight away -1, and a move past it reads less the further it overshoots (negative past twice the
    /// distance). It replaces the axis midpoint the term read before (the party's side of 5), which voted a party for any move toward its end however far.
    /// <b>Its weight</b> among the bill's axes (the review's L3): the rates' own sizes, sum over k of |points_k| (`SpatialRate.Points`), in place of the axis's net
    /// move - so rates moved in opposite directions, whose net move is near nothing, still weigh what they move (`BillConcern.Loaded`); for rates moved one
    /// way the two are the same.</item>
    /// <item><b>Its reach</b> (DECLARED, asked): the US Congress's budget votes (<see cref="Applies"/>: a Senate seated - the player's USA), read from the ruling's
    /// "bloc" as R-US17's parties-as-blocs and the audit question it answers; on the RATES only - a spending line is an amount with no place on a dial,
    /// and the benefit levels and the fund keep the axis-midpoint term; and only where the bill moves no tax sub-row (the sub-rows load the same axis with
    /// no place of their own). Every other country's budget vote is unchanged.</item>
    /// </list>
    /// Pure, deterministic: no stream is drawn.
    /// </summary>
    public static class SpatialBudgetVote
    {
        /// <summary>Whether the spatial vote reads <paramref name="country"/>'s budget votes: the US Congress (a Senate seated).</summary>
        public static bool Applies(Country country) => Congress.Applies(country);

        /// <summary>A rate's place on the `redistribution` scale across the line's dial: the ceiling at 0, the floor at 10; NaN for a line with no range.</summary>
        public static float ScaleAt(TaxLine line, float rate)
        {
            float span = line.MaxRate - line.MinRate;
            return span > 0f ? 10f * (line.MaxRate - rate) / span : float.NaN;
        }

        /// <summary>The rate a position on the `redistribution` scale stands for on the line's dial (<see cref="ScaleAt"/>'s inverse), within the dial.</summary>
        public static float RateAt(TaxLine line, float position)
            => Mathf.Clamp(line.MaxRate - Mathf.Clamp(position, 0f, 10f) / 10f * (line.MaxRate - line.MinRate), line.MinRate, line.MaxRate);

        /// <summary>The rates <paramref name="bill"/> moves, placed on the scale - null where the spatial vote does not read the bill (another country, or a bill moving no
        /// rate); a bill moving a sub-row is the caller's to exclude. Each named implemented line whose rate differs from the standing one, as the concern builder reads a move.</summary>
        public static List<SpatialRate> RatesOf(Country country, BudgetBill bill)
        {
            if (bill == null || !Applies(country)) { return null; }
            List<SpatialRate> rates = null;
            foreach (KeyValuePair<TaxType, float> kv in bill.TaxLines)
            {
                TaxLine line = null;
                foreach (TaxLine l in country.TaxLines) { if (l.Type == kv.Key) { line = l; break; } }
                if (line == null || !line.IsImplemented || Mathf.Approximately(line.PointsOf(kv.Value - line.Rate), 0f)) { continue; }
                float standingAt = ScaleAt(line, line.Rate), billAt = ScaleAt(line, Mathf.Clamp(kv.Value, line.MinRate, line.MaxRate));
                if (float.IsNaN(standingAt) || float.IsNaN(billAt)) { continue; }
                (rates ??= new List<SpatialRate>()).Add(new SpatialRate(line.Type, standingAt, billAt, line.PointsOf(kv.Value - line.Rate)));
            }
            return rates;
        }

        /// <summary>The party at <paramref name="position"/> on the scale: (d(standing) - d(bill)) / |move| over <paramref name="rates"/>, in [-1, 1] by the triangle
        /// inequality; 0 where the bill moves nothing.</summary>
        public static float Alignment(float position, IReadOnlyList<SpatialRate> rates)
        {
            double standing = 0.0, bill = 0.0, move = 0.0;
            foreach (SpatialRate r in rates)
            {
                standing += (r.StandingAt - position) * (double)(r.StandingAt - position);
                bill += (r.BillAt - position) * (double)(r.BillAt - position);
                move += (r.BillAt - r.StandingAt) * (double)(r.BillAt - r.StandingAt);
            }
            if (move <= 0.0) { return 0f; }
            return Mathf.Clamp((float)((Math.Sqrt(standing) - Math.Sqrt(bill)) / Math.Sqrt(move)), -1f, 1f);
        }

        /// <summary>The spatial axis's size among the bill's axes: sum over the rates of |points| (the review's L3); 0 for none.</summary>
        public static float Size(IReadOnlyList<SpatialRate> rates)
        {
            float size = 0f;
            if (rates != null) { foreach (SpatialRate r in rates) { size += Mathf.Abs(r.Points); } }
            return size;
        }

        /// <summary>The standing law's and the bill's places on the scale as a reason prints them - the mean over the rates moved.</summary>
        public static (float Standing, float Bill) MeanPlaces(IReadOnlyList<SpatialRate> rates)
        {
            float s = 0f, b = 0f;
            foreach (SpatialRate r in rates) { s += r.StandingAt; b += r.BillAt; }
            return rates.Count > 0 ? (s / rates.Count, b / rates.Count) : (float.NaN, float.NaN);
        }
    }
}
