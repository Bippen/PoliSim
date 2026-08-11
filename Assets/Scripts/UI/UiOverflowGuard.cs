using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// Records text that did not fit the box it was drawn in, so a capture pass can FAIL instead of
    /// producing a screenshot nobody inspects closely enough.
    ///
    /// <para><b>Why this exists.</b> Clipping has now recurred eleven times in this project, and every
    /// single instance was found by a person looking at a screen - never by a check. Twelve screens
    /// were captured and approved by eye while overflows sat in them, which is the whole problem:
    /// eye-approval is exactly what has been missing these. A helper (<c>InnerWidth</c>) makes the
    /// arithmetic easier to get right when someone remembers to do it; this makes FORGETTING VISIBLE,
    /// which is the half that actually prevents instance #12.</para>
    ///
    /// <para><b>Where it hooks.</b> <see cref="PoliSimWidgets.MeasuredLabel"/> is the choke point for
    /// every shrink-to-fit label in the UI. It shrinks text toward an 8px floor; if the text still does
    /// not fit at the floor, no further resort exists and the label WILL clip. That is precisely the
    /// condition worth failing on, and it needs no per-call-site instrumentation.</para>
    ///
    /// <para><b>But the choke point alone sees only failures.</b> A ladder like
    /// <c>LedgerRow.DrawNameCell</c> reaches <c>MeasuredLabel</c> ONLY after its cheaper paths have
    /// already been rejected - so hooking the choke point instruments the labels that were hardest to
    /// fit and skips every label that fit on the first try. Those early paths are not safe by
    /// construction: each one tests a single axis and draws through a raw <c>GUI.Label</c>, leaving the
    /// other axis unexamined. They are checked individually for that reason.</para>
    ///
    /// ⚠ **NOT UNIVERSAL COVERAGE, and it must not be described as such.** ~178 raw label calls exist
    /// across the UI layer, most in GUILayout flow where the container sizes itself to the content. What
    /// this covers is FIXED-RECT drawing, which is where all eleven instances of the clipping class have
    /// actually landed. A clean run is evidence about that population, not about every label on screen.
    ///
    /// ⚠ EDITOR-ONLY BY CONSTRUCTION. The collection is compiled out of player builds entirely, so a
    /// shipped game pays nothing - not a branch, not a list allocation. A guard that costs frame time
    /// in the build is a guard someone eventually turns off.
    /// </summary>
    public static class UiOverflowGuard
    {
        public enum Axis
        {
            Horizontal,
            Vertical,
        }

        public struct Violation
        {
            public string Text;
            public float Needed;
            public float Available;
            public int FontSize;
            public string Screen;
            public Axis Direction;

            public override string ToString()
            {
                string axis = Direction == Axis.Horizontal ? "wide" : "tall";
                return $"\"{Text}\" needs {Needed:F1} {axis} in {Available:F1} at {FontSize}px " +
                       $"(over by {Needed - Available:F1}) on {Screen}";
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// ⚠ DEDUPED AT RECORD TIME, NOT AT REPORT TIME, and that is a correctness requirement rather
        /// than tidiness. OnGUI re-runs every label every frame, so a single offending string across a
        /// few hundred settle frames is a few hundred entries; the first version accumulated them all
        /// and crashed the Editor partway through the pass. A guard that brings down the run it is
        /// guarding is worse than no guard, because it looks like flakiness.
        /// </summary>
        private static readonly HashSet<string> Keys = new HashSet<string>();

        /// <summary>Hard stop, so a pathological screen cannot reproduce the crash by generating unbounded DISTINCT strings.</summary>
        private const int MaxRecorded = 200;

        private static readonly List<Violation> Recorded = new List<Violation>();

        /// <summary>
        /// Every distinct violation, INCLUDING the ones past <see cref="MaxRecorded"/>.
        ///
        /// ⚠ **The cap silently understated the count by two thirds.** The first pass reported "200
        /// overflows" and 200 was the cap, not the number - the truth was 608, and the difference is
        /// exactly the difference between "a couple of columns" and "systemic". A capped list is the
        /// right way to bound MEMORY and the wrong way to report a TOTAL, so the two are now separate.
        /// </summary>
        public static int TotalViolations { get; private set; }

        private static string CurrentScreen => UiGuardContext.CurrentScreen;

        public static IReadOnlyList<Violation> Violations => Recorded;

        public static void Reset()
        {
            Recorded.Clear();
            Keys.Clear();
            TotalViolations = 0;
        }
#endif

        /// <summary>
        /// Called once the drawing code has settled on a size. <paramref name="needed"/> greater than
        /// <paramref name="available"/> on either axis means the text is going to be cut.
        ///
        /// <para>Both axes, because they fail for different reasons and only one of them was ever
        /// checked. HORIZONTAL is the shrink-to-fit floor: text that still does not fit at 8px has no
        /// further resort. VERTICAL is the row-pitch class: a cell whose height was derived for one font
        /// size, holding type set at another, spills into the row below - which no width check can
        /// see.</para>
        /// </summary>
        public static void Check(string text, Vector2 needed, Vector2 available, int fontSize)
        {
#if UNITY_EDITOR
            // ⚠ REPAINT ONLY, AND THIS IS THE WHOLE CORRECTNESS OF THE GUARD. IMGUI runs the same OnGUI
            // body for Layout and for Repaint, and during Layout GUILayoutUtility hands back a DUMMY rect
            // - measured here as width 1.0, and negative once a caller subtracts padding from it. Every
            // width derived from that is meaningless, so every comparison against it "fails".
            //
            // The first version of this guard did not make the distinction and reported 608 violations,
            // of which 608 were Layout-phase and 0 were real - including the two figures that were
            // written up as prime suspects. Nothing is DRAWN during Layout, so nothing can CLIP during
            // Layout; the only pass whose rectangles both exist and reach the screen is this one.
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            Record(text, needed.x, available.x, fontSize, Axis.Horizontal);
            Record(text, needed.y, available.y, fontSize, Axis.Vertical);
#endif
        }

#if UNITY_EDITOR
        private static void Record(string text, float needed, float available, int fontSize, Axis direction)
        {
            // A pixel of slack: CalcSize and the actual glyph run disagree by sub-pixel amounts on some
            // fonts, and a guard that cries wolf gets disabled, which costs more than it saves.
            if (needed <= available + 1f || available <= 0f)
            {
                return;
            }

            // Rounded into the key so sub-pixel jitter between frames cannot defeat the dedupe and
            // reintroduce the unbounded growth this exists to prevent.
            string key = $"{CurrentScreen}|{text}|{Mathf.RoundToInt(available)}|{direction}";
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
                Text = text,
                Needed = needed,
                Available = available,
                FontSize = fontSize,
                Screen = CurrentScreen,
                Direction = direction,
            });
        }
#endif
    }
}
