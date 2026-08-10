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
    /// ⚠ EDITOR-ONLY BY CONSTRUCTION. The collection is compiled out of player builds entirely, so a
    /// shipped game pays nothing - not a branch, not a list allocation. A guard that costs frame time
    /// in the build is a guard someone eventually turns off.
    /// </summary>
    public static class UiOverflowGuard
    {
        public struct Violation
        {
            public string Text;
            public float Needed;
            public float Available;
            public int FontSize;
            public string Screen;

            public override string ToString()
            {
                return $"\"{Text}\" needs {Needed:F1} in {Available:F1} at {FontSize}px " +
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

        /// <summary>Labels the violations that follow, so a failure names the screen rather than only the string.</summary>
        public static string CurrentScreen { get; set; } = "(unlabelled)";

        public static IReadOnlyList<Violation> Violations => Recorded;

        public static void Reset()
        {
            Recorded.Clear();
            Keys.Clear();
        }
#endif

        /// <summary>
        /// Called after shrink-to-fit has done all it can. <paramref name="needed"/> greater than
        /// <paramref name="available"/> at this point means the text is going to be cut.
        /// </summary>
        public static void Check(string text, float needed, float available, int fontSize)
        {
#if UNITY_EDITOR
            // A pixel of slack: CalcSize and the actual glyph run disagree by sub-pixel amounts on some
            // fonts, and a guard that cries wolf gets disabled, which costs more than it saves.
            if (needed <= available + 1f || available <= 0f)
            {
                return;
            }

            if (Recorded.Count >= MaxRecorded)
            {
                return;
            }

            // Rounded into the key so sub-pixel jitter between frames cannot defeat the dedupe and
            // reintroduce the unbounded growth this exists to prevent.
            string key = $"{CurrentScreen}|{text}|{Mathf.RoundToInt(available)}";
            if (!Keys.Add(key))
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
            });
#endif
        }
    }
}
