namespace PoliSim.UI
{
    /// <summary>
    /// UI v3.0's shell state (V3-R2, <c>POLISIM_UI_V3_DIRECTION.md</c>, 2026-08-28). OPEN is the v2
    /// frame exactly - the chrome column at <c>GameController.LeftColumnWidthFraction</c>, the tab
    /// tongues, the content column. FOLDED collapses the column and the tongues to one icon rail
    /// (<c>GameController.DrawFoldedRail</c>) so the content column becomes the stage. The flip is
    /// instant - this desk does not tween (the calendar's own ruling); the state persists per save as
    /// the player's per-screen override (<c>UiDraftState.ShellFoldOverrides</c>); each screen has a
    /// default (<c>GameController.DefaultShellFold</c>): the landing screen and the Budget ledger
    /// FOLDED, every other screen OPEN. Every screen must be legal in both states; only defaults are
    /// canonical on film (V3-R4).
    /// </summary>
    public enum ShellFoldState
    {
        Open = 0,
        Folded = 1
    }
}
