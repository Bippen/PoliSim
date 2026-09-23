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
        if (args.Length > 0 && args[0] == "commentonly") { return CommentOnlyMode(args); }
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

    /// <summary>RL-1 (s588): `textcheck commentonly &lt;dir&gt;` - for every `&lt;name&gt;.before.txt` beside a `&lt;name&gt;.after.txt` in the directory, one line
    /// `VERDICT	&lt;name&gt;	ACCEPT` (comments and whitespace only) or `VERDICT	&lt;name&gt;	OWED	&lt;reason&gt;`, then `COMMENTONLY: N pair(s)`. The texts are read
    /// as STRICT UTF-8 with no BOM detection, so a byte-order mark stays a character and a text that is not valid UTF-8 is OWED (the RL-1 review's F2: a
    /// lossy decode turned two different invalid bytes into one replacement character, and Unity's compiler reads those bytes in the system code page).
    /// A CR before an LF is dropped on both sides (F3: the working tree is CRLF and the commits LF; the state's digest already ignores carriage returns),
    /// so a multi-line string's value is read the same from either. `ReviewLedgerCheck` writes the pairs, as the files' own bytes, and reads the lines.</summary>
    private static int CommentOnlyMode(string[] args)
    {
        if (args.Length < 2 || !Directory.Exists(args[1])) { Console.Error.WriteLine("COMMENTONLY: give the directory of pairs as the second argument."); return 2; }
        var utf8 = new System.Text.UTF8Encoding(false, true);
        int pairs = 0;
        string[] befores = Directory.GetFiles(args[1], "*.before.txt");
        Array.Sort(befores, StringComparer.Ordinal);
        foreach (string before in befores)
        {
            string name = Path.GetFileName(before).Substring(0, Path.GetFileName(before).Length - ".before.txt".Length);
            string after = Path.Combine(args[1], name + ".after.txt");
            if (!File.Exists(after)) { Console.WriteLine($"VERDICT\t{name}\tOWED\tno later text beside the earlier one"); continue; }
            string refusal;
            try { refusal = CommentOnly.Refusal(utf8.GetString(File.ReadAllBytes(before)).Replace("\r\n", "\n"), utf8.GetString(File.ReadAllBytes(after)).Replace("\r\n", "\n")); }
            catch (System.Text.DecoderFallbackException) { refusal = "a text is not valid UTF-8 - the compiler would read its bytes in another code page, so no verdict"; }
            catch (Exception e) { refusal = "the parser threw - " + e.GetBaseException().Message; }
            pairs++;
            Console.WriteLine(refusal == null ? $"VERDICT\t{name}\tACCEPT" : $"VERDICT\t{name}\tOWED\t{refusal.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", " ")}");
        }
        Console.WriteLine($"COMMENTONLY: {pairs} pair(s).");
        return 0;
    }
}
