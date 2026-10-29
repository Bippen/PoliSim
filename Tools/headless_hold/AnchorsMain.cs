// K276 (2026-10-29): Tools/headless_hold/anchors.sh's entry - LongRunAnchorsDiagnostic's no-policy band by name, then ParticipationCohortDiagnostic.Run,
// out of the engine, their log lines to the console (rewritten by anchors.sh). The engine's logger is switched off (its handler is an engine call).
using System.Reflection;
using System.Text;

public static class HeadlessAnchors
{
    public static void Main()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        UnityEngine.Debug.unityLogger.logEnabled = false;
        System.Type t = typeof(PoliSim.EditorTools.LongRunAnchorsDiagnostic);
        const BindingFlags S = BindingFlags.NonPublic | BindingFlags.Static;
        t.GetField("_sb", S).SetValue(null, new StringBuilder("LONG-RUN ANCHORS (headless): the no-policy band only\n"));
        t.GetField("_failures", S).SetValue(null, 0);
        t.GetField("_registered", S).SetValue(null, 0);
        t.GetMethod("NoPolicyBand", S).Invoke(null, null);
        System.Console.WriteLine(t.GetField("_sb", S).GetValue(null));
        System.Console.WriteLine("LONG-RUN ANCHORS (headless): " + t.GetField("_failures", S).GetValue(null) + " FAIL, " + t.GetField("_registered", S).GetValue(null) + " REGISTERED MISS");
        PoliSim.EditorTools.ParticipationCohortDiagnostic.Run();
    }
}
