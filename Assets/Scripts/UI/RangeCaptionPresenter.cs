using System;
using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// P4-B2 (Playtest 4, 2026-09-04): THE PRESENTATION of a range caption. A dial's caption appears beneath its slider
    /// when the draft moves into a band, holds, and fades over a few seconds; moving again brings it back. Time-based
    /// alpha in IMGUI, deterministic under the film harness: the clock is a function the harness can replace
    /// (<see cref="ClockOverride"/>), so the three moments - on-drag, held, faded - are staged by setting the clock,
    /// never by waiting. Per dial the presenter remembers the band last shown and the moment it was touched; nothing
    /// here reads the simulation and nothing is persisted.
    /// </summary>
    public static class RangeCaptionPresenter
    {
        /// <summary>[AUTHORED-DRAFT] seconds the caption holds at full ink after the last move.</summary>
        public const float HoldSeconds = 1.5f;
        /// <summary>[AUTHORED-DRAFT] seconds the caption takes to fade after the hold.</summary>
        public const float FadeSeconds = 2.5f;

        /// <summary>The harness's clock: when set, every alpha reads this instead of <c>Time.unscaledTime</c>, so a staged moment films the same way every run.</summary>
        public static float? ClockOverride;

        private static float Now => ClockOverride ?? Time.unscaledTime;

        private readonly struct Touch
        {
            public readonly int Band;
            public readonly float At;
            public Touch(int band, float at) { Band = band; At = at; }
        }

        private static readonly Dictionary<string, Touch> Touches = new Dictionary<string, Touch>();

        /// <summary>
        /// Called by the dial row every Repaint with the draft's band. A NEW band (or a draft that differs from the
        /// standing value on a dial never touched) restarts the clock; the same band lets it run. Returns the alpha to
        /// draw the caption at - 1 while held, falling to 0 through the fade, 0 for a dial never touched.
        /// </summary>
        public static float Alpha(string dialKey, int band, bool draftDiffers)
        {
            float now = Now;
            if (Touches.TryGetValue(dialKey, out Touch touch))
            {
                if (touch.Band != band) { touch = new Touch(band, now); Touches[dialKey] = touch; }
            }
            else
            {
                if (!draftDiffers) { return 0f; }
                touch = new Touch(band, now);
                Touches[dialKey] = touch;
            }
            float elapsed = now - touch.At;
            if (elapsed <= HoldSeconds) { return 1f; }
            float u = Mathf.Clamp01((elapsed - HoldSeconds) / FadeSeconds);
            return (1f - u) * (1f - u);   // P6-4 (board 8d): ease-in, not linear - readable through most of the fade, gone quickly at the end
        }

        /// <summary>The harness's reset between films - every touch forgotten.</summary>
        public static void Reset() => Touches.Clear();
    }
}
