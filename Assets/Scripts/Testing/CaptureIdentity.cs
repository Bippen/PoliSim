using System.Collections.Generic;
using UnityEngine;


namespace PoliSim.Testing
{
    /// <summary>
    /// S-20 — **the capture-identity token: a written frame must prove it shows the screen it claims.**
    ///
    /// <para><b>Why this exists.</b> C-D5 found that every `-shotelectionnight` film ever taken had
    /// photographed the DESK under board 1h's name — W-E6's own films included. The board is a
    /// `ScreenSpaceOverlay` Canvas and `GameController.OnGUI` draws after overlay canvases, so the desk
    /// painted straight over it. Through all of it: **8 captured, 0 failed, 0 text overflows, 0
    /// containment escapes, exit 0.** ⚠ <b>The whole film bar checks containment and text fitting WITHIN
    /// WHATEVER WAS DRAWN; nothing checked that the thing under test was the thing on screen.</b> A
    /// screen that silently stopped rendering would have filmed clean forever.</para>
    ///
    /// <para><b>How it works, and why it is a token rather than a state flag.</b> While armed, whichever
    /// surface is actually presenting the frame paints a small solid marker in the top-left corner in a
    /// colour unique to it. Overlay canvases draw BEFORE IMGUI, so if the desk paints over a board the
    /// desk's marker is the one in the written PNG — which is exactly the defect, made visible in the
    /// pixels rather than inferred from a flag. The driver then reads that pixel out of the texture it
    /// just wrote and compares it with the surface the capture CLAIMS.</para>
    ///
    /// <para>⚠ <b>A state assertion would not have caught this.</b> `CanvasTextGuard` already asserts the
    /// live Canvas's own text and was green throughout: the board existed, its text was correct, and it
    /// was invisible. The only evidence that binds is the frame that was written.</para>
    ///
    /// <para>⚠ <b>DECLARED ARTEFACT:</b> while armed, films carry a 4×4 px block in the extreme top-left
    /// corner. It is outside `ScreenEdgeCheck`'s margin line, it is armed only by the capture harness, and
    /// it never appears in play. That is the price of the assertion and it is recorded rather than
    /// hidden.</para>
    /// </summary>
    public static class CaptureIdentity
    {
        /// <summary>Armed by the capture harness for the whole film run. Off in play — no marker is ever
        /// drawn in a game a person is playing.</summary>
        public static bool Armed;

        /// <summary>The surface the NEXT capture claims to show. Set by the driver before each shot.</summary>
        public static string Expected;

        public const int MarkerSize = 4;

        /// <summary>
        /// ⚠ **A small palette of EXTREME colours, not a hash.** A hashed colour can land anywhere,
        /// including on a shade the screen itself uses, and comparing it out of a PNG then needs a
        /// tolerance wide enough to be meaningless. These eight are maximally separated, so a ±40
        /// per-channel tolerance still cannot confuse two of them - and a name with no slot FAILS rather
        /// than falling back to a colour that means something else.
        /// </summary>
        private static readonly Dictionary<string, Color32> Palette = new Dictionary<string, Color32>
        {
            { "imgui", new Color32(255, 0, 0, 255) },
            { "selector", new Color32(0, 255, 0, 255) },
            { "signing", new Color32(0, 0, 255, 255) },
            { "electionnight", new Color32(255, 0, 255, 255) },
            { "campaign", new Color32(0, 255, 255, 255) },
            { "results", new Color32(255, 255, 0, 255) },
        };

        public static bool TryColorFor(string surface, out Color32 color) =>
            Palette.TryGetValue(surface ?? string.Empty, out color);

        /// <summary>Every surface name the palette knows — printed by the driver so the enumeration is
        /// visible rather than implied (the enumeration rule).</summary>
        public static IEnumerable<string> Surfaces => Palette.Keys;

        /// <summary>
        /// IMGUI's own marker. ⚠ Called from `GameController.OnGUI` **only when IMGUI is actually
        /// presenting** — never during a Canvas takeover, because the marker's whole meaning is "this is
        /// the surface the player is looking at", and a marker drawn by a suppressed surface would say
        /// the opposite of the truth.
        /// </summary>
        public static void DrawMarker(string surface)
        {
            if (!Armed || Event.current == null || Event.current.type != EventType.Repaint) { return; }
            if (!TryColorFor(surface, out Color32 color)) { return; }

            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(0f, 0f, MarkerSize, MarkerSize), Texture2D.whiteTexture);
            GUI.color = previous;
        }


        /// <summary>
        /// ⚠ **WHICH CANVAS BOARD OWNS THE SCREEN — set by the board itself when it builds.**
        ///
        /// <para>The token is stamped by IMGUI even for Canvas boards, and that is the fourth design this
        /// trap went through. Drawing it from the Canvas side did not work, twice over: a UI `Image` with
        /// a null sprite renders **nothing**, and anchoring the marker inside a board's own root made its
        /// placement depend on that board's layout and entrance animation. **A marker whose placement
        /// depends on the thing it is auditing is not an audit.** IMGUI draws last and unconditionally, so
        /// it is the one place a token is certain to reach the frame.</para>
        ///
        /// <para>⚠ <b>This does not weaken the trap.</b> `GameController` stamps this name ONLY on the
        /// branch where a Canvas board genuinely owns the screen. The defect C-D5 found — a board built by
        /// the harness with no takeover, painted over by the desk — takes the other branch and stamps
        /// `imgui`, which is exactly the mismatch that must fail.</para>
        /// </summary>
        public static string CanvasSurface;

        /// <summary>The token IMGUI should stamp this frame: the Canvas board's name while one owns the
        /// screen, otherwise IMGUI's own.</summary>
        public static string SurfaceForTakeover() => string.IsNullOrEmpty(CanvasSurface) ? "imgui" : CanvasSurface;
    }
}
