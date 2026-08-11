namespace PoliSim.UI
{
    /// <summary>
    /// What the UI guards need to know about the run they are guarding. One field today: which screen
    /// is being drawn.
    ///
    /// **Its own type because two guards need it.** <see cref="UiOverflowGuard"/> owned this while it
    /// was the only one; <see cref="UiContainmentGuard"/> needs the same label, and the alternative was
    /// either a second property the capture driver must remember to set in step, or one guard reaching
    /// into the other for state that belongs to neither. Two names for one fact is the failure this
    /// codebase has now written up three times - the copied name table, the duplicated tile height, the
    /// two agreeing-until-edited column formulas.
    ///
    /// ⚠ Deliberately NOT inside `#if UNITY_EDITOR`. The guards compile their collection out of player
    /// builds, but the capture driver lives in a runtime assembly and assigns this by name; guarding the
    /// property would push a `#if` into every caller to save one string field that is never written in a
    /// shipped game.
    /// </summary>
    public static class UiGuardContext
    {
        /// <summary>
        /// Labels the violations that follow, so a failure names the screen rather than only the string.
        ///
        /// Set BEFORE the frame is drawn, never after: IMGUI records during the OnGUI of the frame being
        /// captured, so naming the screen afterwards files its findings under the next one.
        /// </summary>
        public static string CurrentScreen { get; set; } = "(unlabelled)";
    }
}
