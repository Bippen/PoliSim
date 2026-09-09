using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// D16 §5.2 and §8.6 (2026-09-09, §413): the waterfall's arithmetic, as a pure function, so the drawing and the acceptance bar are the
    /// same computation. The positive terms lay left to right at a FIXED scale in the rule's own order; the negative terms are a CUT taken
    /// out of the right end; the solid ink that remains is the total, and **the sum of the drawn positive widths minus the drawn cut width
    /// equals `total × scale` to the pixel** - which is the property §8.6 asks a test to assert.
    /// </summary>
    public static class RuleWaterfall
    {
        public readonly struct Segment
        {
            public readonly string Name;
            public readonly float Value, X, Width;
            /// <summary>A zero term is drawn as NOTHING and labelled struck through - never a sliver, never silently absent.</summary>
            public readonly bool Zero;
            public Segment(string name, float value, float x, float width, bool zero)
            {
                Name = name; Value = value; X = x; Width = width; Zero = zero;
            }
        }

        public readonly struct Geometry
        {
            public readonly List<Segment> Positives;
            public readonly List<Segment> Negatives;
            public readonly float PositiveSum, CutFrom, CutWidth, TotalX;
            public Geometry(List<Segment> positives, List<Segment> negatives, float positiveSum, float cutFrom, float cutWidth, float totalX)
            {
                Positives = positives; Negatives = negatives; PositiveSum = positiveSum; CutFrom = cutFrom; CutWidth = cutWidth; TotalX = totalX;
            }
        }

        /// <summary>Below this a term counts as zero - drawn as nothing, labelled struck through.</summary>
        public const float ZeroBand = 0.005f;

        /// <summary>The geometry of one year's rule, in pixels from the bar's own origin.</summary>
        public static Geometry Compute((string Name, float Value)[] terms, float total, float scale)
        {
            var positives = new List<Segment>();
            var negatives = new List<Segment>();
            float x = 0f;
            foreach ((string name, float value) in terms)
            {
                if (value > ZeroBand)
                {
                    float w = value * scale;
                    positives.Add(new Segment(name, value, x, w, false));
                    x += w;
                }
                else if (Mathf.Abs(value) <= ZeroBand)
                {
                    positives.Add(new Segment(name, 0f, x, 0f, true));
                }
            }
            float positiveSum = x;
            float totalX = total * scale;
            float cutFrom = Mathf.Min(totalX, positiveSum);
            float cutWidth = Mathf.Max(0f, positiveSum - cutFrom);
            float stacked = cutFrom;
            foreach ((string name, float value) in terms)
            {
                if (value < -ZeroBand)
                {
                    float w = Mathf.Min(-value * scale, positiveSum - stacked);
                    negatives.Add(new Segment(name, value, stacked, w, false));
                    stacked += w;
                }
            }
            return new Geometry(positives, negatives, positiveSum, cutFrom, cutWidth, totalX);
        }
    }
}
