using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// Records a rect that was drawn outside the rect meant to contain it, so a capture pass FAILS
    /// instead of producing a screenshot with something sitting on its neighbour.
    ///
    /// <para><b>Why this is a second guard and not a wider first one.</b>
    /// <see cref="UiOverflowGuard"/> asks *"does this text fit the rect it was handed?"* The defect
    /// that motivated this one - `StatTile`'s delta drawn below the tile's own bottom edge, onto the
    /// next row's keyline - answered that question **yes**. The label fitted its rect perfectly; the
    /// rect was in the wrong place. That is a relation between two RECTS with no text in it, so no
    /// amount of widening the text check reaches it. It was found by a person looking at a capture the
    /// overflow guard had just passed, which is the same way all eleven clipping instances were
    /// found.</para>
    ///
    /// <para><b>Scoped to composite widgets that lay out a stack inside a fixed rect</b> -
    /// <see cref="PoliSimWidgets.StatTile"/>, <see cref="LedgerRow"/>,
    /// <see cref="PolicyScreenStatsRenderer"/>. A general "every rect inside its parent" check would
    /// need a container stack this codebase does not have, and building one to satisfy a check is the
    /// wrong trade. These three are where the pattern lives: each is handed a rect, walks a cumulative
    /// offset through it, and has a SEPARATE height accessor its caller uses to reserve space.</para>
    ///
    /// ⚠ **THAT SEPARATION IS THE THING BEING GUARDED.** `StatTileHeight` walks the same named
    /// constants the drawing walks, which is correct today and is exactly the shape that drifts in
    /// silence: add an element to the stack, forget the accessor, and the tile overruns by precisely
    /// the element's height with nothing failing. This makes the agreement ENFORCED rather than
    /// remembered - the same reasoning as one accessor for arc and swatch, and
    /// <see cref="DisplayName"/> over a copied table.
    ///
    /// ⚠ EDITOR-ONLY BY CONSTRUCTION, like its sibling. The collection compiles out of player builds
    /// entirely; a guard that costs frame time in the build is one someone eventually turns off.
    /// </summary>
    public static class UiContainmentGuard
    {
        public struct Violation
        {
            public string Site;
            public string Edge;
            public float Overrun;
            public Rect Child;
            public Rect Container;
            public string Screen;

            public override string ToString()
            {
                return $"{Site} escapes its container {Edge} by {Overrun:F1} " +
                       $"(child {Child.x:F0},{Child.y:F0} {Child.width:F0}x{Child.height:F0} in " +
                       $"container {Container.x:F0},{Container.y:F0} {Container.width:F0}x{Container.height:F0}) " +
                       $"on {Screen}";
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// ⚠ DEDUPED AT RECORD TIME, NOT AT REPORT TIME. Built this way from the start rather than
        /// discovered: the overflow guard's first version appended one entry per label per FRAME, and a
        /// few hundred settle frames became an unbounded list that took the Editor down mid-pass. A
        /// guard that brings down the run it is guarding reads as flakiness, which is worse than no
        /// guard at all.
        /// </summary>
        private static readonly HashSet<string> Keys = new HashSet<string>();

        /// <summary>Hard stop, so a pathological screen cannot reproduce that crash by generating unbounded DISTINCT keys.</summary>
        private const int MaxRecorded = 200;

        private static readonly List<Violation> Recorded = new List<Violation>();

        /// <summary>
        /// Every distinct violation, INCLUDING those past <see cref="MaxRecorded"/>. The cap bounds
        /// MEMORY; it must not be allowed to understate a TOTAL, which is how "200 overflows" was read
        /// as a count when it was a ceiling and the truth was 608.
        /// </summary>
        public static int TotalViolations { get; private set; }

        public static IReadOnlyList<Violation> Violations => Recorded;

        public static void Reset()
        {
            Recorded.Clear();
            Keys.Clear();
            TotalViolations = 0;
        }
#endif

        /// <summary>
        /// Asserts <paramref name="child"/> lies within <paramref name="container"/>.
        /// </summary>
        /// <param name="site">Names the widget AND which piece of it, so a failure is actionable without a debugger.</param>
        public static void Check(string site, Rect child, Rect container)
        {
#if UNITY_EDITOR
            // ⚠ REPAINT ONLY, GATED FROM THE START. During the Layout pass GUILayoutUtility returns a
            // dummy rect - measured at width 1.0, and negative once a caller subtracts padding - so
            // every containment comparison against it is meaningless and every one of them "fails". The
            // overflow guard learned this the expensive way: 608 findings, 608 Layout-phase, 0 real,
            // with two of them written up as prime suspects. Nothing is drawn during Layout, so nothing
            // can escape anything during Layout.
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (container.width <= 0f || container.height <= 0f)
            {
                return;
            }

            Record(site, "left", container.xMin - child.xMin, child, container);
            Record(site, "right", child.xMax - container.xMax, child, container);
            Record(site, "top", container.yMin - child.yMin, child, container);
            Record(site, "bottom", child.yMax - container.yMax, child, container);
#endif
        }

        /// <summary>
        /// The vertical variant, for a widget that walks a cumulative offset and knows how far down it
        /// got but has no single rect to describe the result.
        /// </summary>
        public static void CheckStackBottom(string site, float contentBottom, Rect container)
        {
            Check(site, new Rect(container.x, container.y, container.width, contentBottom - container.y), container);
        }

#if UNITY_EDITOR
        private static void Record(string site, string edge, float overrun, Rect child, Rect container)
        {
            // A pixel of slack, matching the overflow guard: rounding between a layout computed in
            // floats and a rect snapped for drawing disagrees by sub-pixel amounts, and a guard that
            // cries wolf gets switched off, which costs more than it saves.
            if (overrun <= 1f)
            {
                return;
            }

            // Rounded into the key so sub-pixel jitter between frames cannot defeat the dedupe and
            // reintroduce the unbounded growth this exists to prevent.
            string key = $"{UiGuardContext.CurrentScreen}|{site}|{edge}|{Mathf.RoundToInt(overrun)}";
            if (!Keys.Add(key))
            {
                return;
            }

            // Counted BEFORE the cap, so a truncated list still reports an honest total.
            TotalViolations++;

            if (Recorded.Count >= MaxRecorded)
            {
                return;
            }

            Recorded.Add(new Violation
            {
                Site = site,
                Edge = edge,
                Overrun = overrun,
                Child = child,
                Container = container,
                Screen = UiGuardContext.CurrentScreen,
            });
        }
#endif
    }
}
