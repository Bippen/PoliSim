using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// **The screen size every IMGUI layout reads (2026-09-17).** In play it is <c>Screen.width</c> and
    /// <c>Screen.height</c> and nothing else - the override exists only in the Editor, where the dry film
    /// sets it, because a `-batchmode` Editor reports a 640x480 screen and every style in this UI derives its
    /// type size from the height (`GameController.RescaleStylesToScreen`). A dry film at 1280 sets the frame
    /// a 1280x720 film captures (1280x699, the Game View's toolbar taken off) and the layout then runs at
    /// exactly that size with no window.
    ///
    /// <para>⚠ <b>An int, as <c>Screen.width</c> is</b>, so every expression that read the screen keeps its
    /// type and its value in play: this is a seam, not a change to any layout.</para>
    /// </summary>
    public static class UiScreen
    {
#if UNITY_EDITOR
        /// <summary>The dry film's frame width; 0 = the real screen.</summary>
        public static int OverrideWidth;

        /// <summary>The dry film's frame height; 0 = the real screen.</summary>
        public static int OverrideHeight;

        public static int Width => OverrideWidth > 0 ? OverrideWidth : Screen.width;

        public static int Height => OverrideHeight > 0 ? OverrideHeight : Screen.height;
#else
        public static int Width => Screen.width;

        public static int Height => Screen.height;
#endif
    }
}
