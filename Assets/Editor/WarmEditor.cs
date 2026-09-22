using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §574 (2026-09-22, the efficiency pass's item 2) — **ONE EDITOR, HELD WARM, TALKED TO THROUGH TWO FILES.**
    ///
    /// <para><b>The measurement.</b> A Unity launch costs 23 s before it does any work - 1.2 + 5.2 s of domain reload, 2.8 s of
    /// compilation bookkeeping with nothing changed, and 8-17 s more after a source edit (§573). Three passes paid that toll 156 times:
    /// **127 minutes of the 336 minutes Unity was running.** <see cref="MultiRun"/> pays it once per BATCH; this pays it once per
    /// SESSION.</para>
    ///
    /// <para><b>How it works, and why a file and not a port.</b> Started with
    /// <c>-executeMethod PoliSim.EditorTools.WarmEditor.Host</c>, this loops on the main thread through
    /// its own main-thread loop, watching <c>PoliSim-captures/bridge/cmd.txt</c>. A command is one line -
    /// <c>run &lt;Type.Method&gt;[,&lt;Type.Method&gt;…]</c>, <c>checks &lt;Name,Name&gt;</c>, or <c>quit</c> - and the host writes
    /// <c>done.txt</c> when it has finished, with each method's seconds and exit code. **No port, no socket, no server**: the only
    /// channel is two files in a directory outside the repository, which is the smallest thing that can carry a command and cannot be
    /// reached from anywhere else on the machine.</para>
    ///
    /// <para>⚠ <b>The static-state gate, which came first and stays first.</b> A warm Editor keeps static state alive between commands,
    /// and this project found three static-state defects in a month (the fleet table, the market's turn state, a leaked turn value).
    /// So: <see cref="MultiRun"/>'s own proof - the sentinel identical alone and batched - was run before batching was used, and the
    /// same proof is run WARM before anything relies on this: the sentinel's two digests and its six cells, cold against warm, on the
    /// record in §574. **A difference is a defect to find, never a thing to work around.**</para>
    ///
    /// <para>⚠ <b>What the host refuses.</b> It will not run while a film or a bar holds the project (Unity's own lock does that for
    /// it - a second Editor cannot open the project), and it exits on <c>quit</c> or after <see cref="IdleMinutes"/> with nothing to do,
    /// so a forgotten host cannot hold the project against the next cold run. It does not recompile: an edit to C# while it is warm is
    /// picked up by Unity's own asset refresh on the next command, which costs the compile but not the start - and the log says when
    /// that happened, because a command that silently ran the OLD code would be the worst failure this could have.</para>
    /// </summary>
    public static class WarmEditor
    {
        /// <summary>The bridge's directory, outside the repository beside the logs the runners already write.</summary>
        public static string BridgeDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "PoliSim-captures", "bridge"));

        /// <summary>A host with nothing to do for this long exits, so it cannot hold the project lock against a cold run.</summary>
        private const double IdleMinutes = 45.0;

        private static string _command = string.Empty;
        private static DateTime _lastWork;
        private static int _served;

        public static void Host()
        {
            Directory.CreateDirectory(BridgeDirectory);
            string cmd = Path.Combine(BridgeDirectory, "cmd.txt"), done = Path.Combine(BridgeDirectory, "done.txt");
            if (File.Exists(cmd)) { File.Delete(cmd); }
            if (File.Exists(done)) { File.Delete(done); }

            _lastWork = DateTime.UtcNow;
            _served = 0;
            Debug.Log($"WARM: host up - {BridgeDirectory}. Commands: 'run <Type.Method>[,…]', 'checks <Name>[,…]', 'quit'. Idle limit {IdleMinutes:F0} min.");
            File.WriteAllText(Path.Combine(BridgeDirectory, "ready.txt"), DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));

            // ⚠ THE PUMP IS THIS LOOP, not EditorApplication.update. The first cut registered the pump as an update callback and blocked
            // the main thread here waiting for it - and in batchmode a blocked main thread never ticks the editor loop, so the host sat
            // with a command file it would never read (measured on the first run: one wedged host, a locked project until its idle timer).
            // The work runs HERE, on the main thread, which is where every check has to run anyway.
            while (true)
            {
                Thread.Sleep(100);
                Pump();
                if (_command == "quit") { break; }
                if ((DateTime.UtcNow - _lastWork).TotalMinutes > IdleMinutes)
                {
                    Debug.Log($"WARM: idle past {IdleMinutes:F0} min after {_served} command(s) - exiting so the project is free.");
                    break;
                }
            }

            Debug.Log($"WARM: host down after {_served} command(s).");
            EditorApplication.Exit(0);
        }

        private static void Pump()
        {
            string cmd = Path.Combine(BridgeDirectory, "cmd.txt");
            if (!File.Exists(cmd)) { return; }

            string text;
            try { text = File.ReadAllText(cmd).Trim(); }
            catch (IOException) { return; }   // the writer still has it; next tick

            File.Delete(cmd);
            if (text.Length == 0) { return; }

            _lastWork = DateTime.UtcNow;
            _served++;
            if (text == "quit") { _command = "quit"; Write("done", 0, "quit"); return; }

            var report = new StringBuilder();
            int worst = 0;
            var total = Stopwatch.StartNew();
            try
            {
                if (text.StartsWith("checks ", StringComparison.Ordinal))
                {
                    foreach (string name in text.Substring(7).Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        worst = Math.Max(worst, RunOne(CheckSuite.Find(name.Trim()), name.Trim(), report));
                    }
                }
                else if (text.StartsWith("run ", StringComparison.Ordinal))
                {
                    foreach (string name in text.Substring(4).Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        worst = Math.Max(worst, RunOne(Resolve(name.Trim()), name.Trim(), report));
                    }
                }
                else if (text.StartsWith("bar ", StringComparison.Ordinal))
                {
                    // §575: a BAR, run so the host SURVIVES it - CheckSuite.RunTableNamed returns the worst code where the
                    // batch entry would have ended the Editor. This is the one command the loop actually needs from a warm host,
                    // and the host's first real use is what found that it was missing.
                    string which = text.Substring(4).Trim();
                    var barWatch = Stopwatch.StartNew();
                    worst = CheckSuite.RunTableNamed(which);
                    barWatch.Stop();
                    report.Append($"  bar {which,-54} {barWatch.Elapsed.TotalSeconds,7:F1} s  {(worst == 0 ? "ok" : "FAILED " + worst.ToString(CultureInfo.InvariantCulture))}\n");
                }
                else
                {
                    report.Append($"  unknown command '{text}'\n");
                    worst = 1;
                }
            }
            catch (Exception e)
            {
                report.Append($"  the host threw: {e.GetBaseException().Message}\n");
                worst = 1;
            }

            total.Stop();
            Debug.Log($"WARM: '{text}' -> {worst} in {total.Elapsed.TotalSeconds:F1} s\n{report}");
            Write(report.ToString(), worst, text);
        }

        private static Action Resolve(string name)
        {
            int dot = name.LastIndexOf('.');
            if (dot <= 0) { return null; }

            string typeName = name.Substring(0, dot), methodName = name.Substring(dot + 1);
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(typeName, false);
                MethodInfo m = t?.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (m != null) { return () => m.Invoke(null, null); }
            }

            return null;
        }

        private static int RunOne(Action work, string name, StringBuilder report)
        {
            if (work == null) { report.Append($"  {name,-58} NOT FOUND\n"); return 1; }

            var watch = Stopwatch.StartNew();
            int code;
            try { code = CheckExit.Collect(work); }
            catch (Exception e) { report.Append($"  {name,-58} THREW {e.GetBaseException().Message}\n"); return 1; }

            watch.Stop();
            report.Append($"  {name,-58} {watch.Elapsed.TotalSeconds,7:F1} s  {(code == 0 ? "ok" : "FAILED " + code.ToString(CultureInfo.InvariantCulture))}\n");
            return code;
        }

        private static void Write(string report, int code, string command)
        {
            File.WriteAllText(Path.Combine(BridgeDirectory, "done.txt"),
                $"command\t{command}\nexit\t{code}\nserved\t{_served}\n{report}");
        }
    }
}
