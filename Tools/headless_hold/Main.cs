// K218 (2026-10-28): Tools/headless_hold's entry - DebtPathHoldDiagnostic.Run out of the engine, its log lines to the console. The engine's logger is
// switched off (its handler is an engine call); the diagnostic's own lines are rewritten to Console.WriteLine by hold.sh.
public static class HeadlessHold
{
    public static void Main()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        UnityEngine.Debug.unityLogger.logEnabled = false;
        PoliSim.EditorTools.DebtPathHoldDiagnostic.Run();
    }
}
