// §574 (2026-09-22) — THE DOCUMENT TIER'S BAR, OUT OF THE ENGINE.
//
// Measured: a document bar is 23 s of wall for ≈ 0 s of work, because Unity's start is the whole of it (§573). The eight
// checks it runs read files and print. This runs the same source files in a console process - about a second - and
// exits with the worst code any of them wanted, exactly as `CheckSuite.RunDocumentBatch` does.
//
// ⚠ IT RUNS FIVE OF THE TIER'S EIGHT, AND THE THREE IT LEAVES ARE NAMED. `D18InventoryCheck` reads the game's own party and
// area tables (PoliSim.Data, PoliSim.UI) and would drag the engine in behind them; `PlaySheetCheck` reflects over
// `CampaignPressure` for the same reason; `MojibakeCheck` reads every file's bytes and its verdict depends on the Editor's
// own encoding behaviour, which a console build does not reproduce - it reported a file set the Unity bar does not. Those
// three stay in the Editor and the document bar still runs them. **This runner is a LOOP instrument, not the bar**: it
// answers in about a second while a record is being written, and the bar is unchanged.
using System;
using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.IO;
using PoliSim.EditorTools;
using UnityEngine;

public static class Runner
{
    public static int Main(string[] args)
    {
        string root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        Application.dataPath = Path.Combine(root, "Assets");
        if (!Directory.Exists(Application.dataPath))
        {
            Console.Error.WriteLine($"TEXTCHECK: no Assets under '{root}' - give the project root as the first argument.");
            return 2;
        }

        Directory.SetCurrentDirectory(root);
        Debug.Logged += (condition, stack, type) => { if (type == LogType.Error || type == LogType.Exception) { CheckExit.CountError(); } };

        var table = new (string Name, Action Run)[]
        {
            ("UpstreamCheck", UpstreamCheck.Run),
            ("DocumentClaimCheck", DocumentClaimCheck.Run),
            ("PreWiringPremiseCheck", PreWiringPremiseCheck.Run),
            ("DesignNotificationCheck", DesignNotificationCheck.Run),
            ("ResidueCheck", ResidueCheck.Run),
        };

        var failed = new List<string>();
        int worst = 0;
        var all = Stopwatch.StartNew();
        foreach ((string name, Action run) in table)
        {
            var watch = Stopwatch.StartNew();
            int code;
            try { code = CheckExit.Collect(run); }
            catch (Exception e) { Console.WriteLine($"TEXTCHECK: {name} threw - {e.GetBaseException().Message}"); code = 1; }

            watch.Stop();
            if (code != 0) { failed.Add(name); }

            worst = Math.Max(worst, code);
            Console.WriteLine($"TEXTCHECK: {name,-26} {watch.Elapsed.TotalSeconds,6:F2} s  {(code == 0 ? "ok" : "FAILED " + code)}");
        }

        all.Stop();
        Console.WriteLine(failed.Count == 0
            ? $"CHECKS: {table.Length} of {table.Length} clean in {all.Elapsed.TotalSeconds:F2} s (out of the engine)."
            : $"CHECKS: {failed.Count} of {table.Length} FAILED - {string.Join(", ", failed)} ({all.Elapsed.TotalSeconds:F2} s).");
        return worst;
    }
}
