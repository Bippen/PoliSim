using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// **Board 13b — the attribution ledger as a BRIDGE** (D19 item 2, 2026-09-10): start at the baseline share, step
    /// through every source in a FIXED order, and land on the close with no reconciling bar - which is only a drawing
    /// because <see cref="VoteAttribution.Ledger"/>'s eleven Shapley lines sum to the move exactly. PURE GEOMETRY; the
    /// painter and the page read it.
    ///
    /// <para><b>The board's rules, held here.</b> Order: the eight actions in the enum's order, then attacks received,
    /// opponent campaigns, earned coverage - never re-sorted by magnitude (<i>"largest first" is why the block read as a
    /// footnote</i>). Scale: 1 pp = 20 px, fixed across the campaign - a quiet night draws short, a loud one tall; the
    /// vertical range is the path's own excursion, never re-fitted to the box. A zero term is a step of no height with its
    /// abbreviation struck through - a blank slot IS the information. The close tick is heavier; the move prints once.</para>
    ///
    /// <para>⚠ The identity is asserted, not assumed: <see cref="Build"/> refuses a ledger whose lines do not land on its
    /// close within 1e-9 of a share, because a bridge that needs a reconciling step is the thing this instrument exists
    /// not to draw.</para>
    /// </summary>
    public static class AttributionBridge
    {
        /// <summary>The board's scale: one percentage point of share is twenty pixels at 1×.</summary>
        public const float PixelsPerPoint = 20f;

        /// <summary>The board's abbreviations, in <see cref="Order"/>.</summary>
        public static readonly string[] Abbreviations = { "R", "TH", "DTD", "TA", "DA", "SP", "I", "PA", "AR", "OC", "EC" };

        /// <summary>The fixed order - the enum's, which is the ask's.</summary>
        public static readonly VoteAttributionSource[] Order =
        {
            VoteAttributionSource.Rally, VoteAttributionSource.TownHall, VoteAttributionSource.DoorToDoor,
            VoteAttributionSource.TelevisionAd, VoteAttributionSource.DigitalAd, VoteAttributionSource.SocialPost,
            VoteAttributionSource.Interview, VoteAttributionSource.PolicyAnnouncement, VoteAttributionSource.AttacksReceived,
            VoteAttributionSource.OpponentCampaigns, VoteAttributionSource.EarnedCoverage,
        };

        /// <summary>Below this a line is a zero term: drawn as nothing, its abbreviation struck. A hundredth of a point.</summary>
        public const double ZeroPoints = 0.005;

        public readonly struct Step
        {
            public readonly VoteAttributionSource Source;
            public readonly string Abbreviation;
            /// <summary>The line's value in percentage points.</summary>
            public readonly double Points;
            /// <summary>The level before and after the step, in percentage points of share.</summary>
            public readonly double From, To;
            public readonly bool Zero;

            public Step(VoteAttributionSource source, string abbreviation, double points, double from, double to)
            {
                Source = source; Abbreviation = abbreviation; Points = points; From = from; To = to; Zero = Math.Abs(points) < ZeroPoints;
            }
        }

        public sealed class Geometry
        {
            public double BaselinePoints, ClosePoints, MovePoints;
            public readonly List<Step> Steps = new List<Step>();
            /// <summary>The path's own excursion - the lowest and highest level it visits, baseline and close included.</summary>
            public double LowPoints, HighPoints;
            /// <summary>The bridge's height at 1×: the excursion at <see cref="PixelsPerPoint"/>, never less than one point.</summary>
            public float HeightPixels => (float)Math.Max(1.0, HighPoints - LowPoints) * PixelsPerPoint;
        }

        /// <summary>The bridge for a ledger. Throws when the ledger's lines do not sum to its move - see the class doc.</summary>
        public static Geometry Build(VoteAttribution.Ledger ledger)
        {
            if (ledger == null) { throw new ArgumentNullException(nameof(ledger)); }
            var g = new Geometry
            {
                BaselinePoints = ledger.ShareAtBaseline * 100.0,
                ClosePoints = ledger.ShareAtClose * 100.0,
                MovePoints = ledger.Deviation * 100.0,
            };
            double level = g.BaselinePoints;
            g.LowPoints = g.HighPoints = level;
            for (int i = 0; i < Order.Length; i++)
            {
                ledger.Lines.TryGetValue(Order[i], out double share);
                double points = share * 100.0;
                double next = level + points;
                g.Steps.Add(new Step(Order[i], Abbreviations[i], points, level, next));
                level = next;
                if (level < g.LowPoints) { g.LowPoints = level; }
                if (level > g.HighPoints) { g.HighPoints = level; }
            }

            if (Math.Abs(level - g.ClosePoints) > 1e-9 * 100.0)
            {
                throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "the ledger's lines land on {0:F6} pp, not its close {1:F6} pp - a bridge cannot be drawn over a residual of {2:E2}",
                    level, g.ClosePoints, level - g.ClosePoints));
            }

            return g;
        }
    }
}
