using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §572 (2026-09-22, the efficiency review's loop change 2) — **SEVERAL METHODS, ONE LAUNCH.**
    ///
    /// <para><b>The measurement this answers.</b> Over the three passes of 2026-09-21/22, Unity was launched 156 times.
    /// Every launch pays the same fixed toll before it does any work: **~6.4 s of domain reload (1.2 + 5.2, read out of
    /// Unity's own Domain Reload Profiling), ~2.8 s of compilation bookkeeping even when no script changed, and enough
    /// package/asset-database start-up to put a do-nothing launch at 23 s wall.** A pass that lands one BASELINE family
    /// spent 44 of those launches on SINGLE diagnostics - the sentinel, a dump, a probe, a mutation, the same again after
    /// one edit - 42.4 minutes of wall for work that is mostly seconds each.</para>
    ///
    /// <para><b>What this does.</b> <c>-executeMethod PoliSim.EditorTools.MultiRun.Run -methods=A.B,C.D</c> invokes each
    /// fully-qualified static method IN ORDER inside ONE Editor, collecting each one's exit code through
    /// <see cref="CheckExit.Collect"/> (so a method that would have exited the Editor merely reports), timing each, and
    /// exiting once with the worst code any of them wanted. Six diagnostics that cost 6 × 25 s of toll now cost one.</para>
    ///
    /// <para>⚠ <b>What it does NOT do, and why.</b> It does not keep an Editor warm ACROSS calls: static state that
    /// survives between runs is exactly where this project found three defects in a month (the fleet table, the market's
    /// turn state, a leaked turn value), and a warm Editor has to be proved against a cold one before anything relies on
    /// it. This is the safe half of that saving - the toll is paid once per BATCH rather than once per method - and it
    /// carries the same risk profile as the cheap bar, which has run 45 checks in one Editor since it was written.</para>
    ///
    /// <para>⚠ It also refuses an empty list rather than exiting 0: a runner that ran nothing and reported success is the
    /// failure mode this project has caught twice elsewhere (a silent subset, a partial sweep).</para>
    /// </summary>
    public static class MultiRun
    {
        public static void Run()
        {
            string arg = string.Empty;
            foreach (string a in Environment.GetCommandLineArgs())
            {
                if (a.StartsWith("-methods=", StringComparison.Ordinal)) { arg = a.Substring(9); }
            }

            string[] wanted = arg.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (wanted.Length == 0)
            {
                Debug.LogError("MULTIRUN: needs -methods=Namespace.Type.Method,... - it ran nothing, which is not a pass.");
                EditorApplication.Exit(1);
                return;
            }

            var resolved = new List<(string Name, MethodInfo Method)>();
            var unknown = new List<string>();
            foreach (string raw in wanted)
            {
                string name = raw.Trim();
                int lastDot = name.LastIndexOf('.');
                MethodInfo found = null;
                if (lastDot > 0)
                {
                    string typeName = name.Substring(0, lastDot), methodName = name.Substring(lastDot + 1);
                    foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        Type t = asm.GetType(typeName, false);
                        if (t == null) { continue; }

                        found = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (found != null) { break; }
                    }
                }

                if (found == null) { unknown.Add(name); } else { resolved.Add((name, found)); }
            }

            if (unknown.Count > 0)
            {
                Debug.LogError($"MULTIRUN: cannot resolve {string.Join(", ", unknown)} - a method named and not found is a typo, not a pass.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"MULTIRUN: {resolved.Count} method(s) in ONE launch - {string.Join(", ", resolved.ConvertAll(r => r.Name))}.");
            var sb = new StringBuilder();
            int worst = 0;
            var total = Stopwatch.StartNew();
            foreach ((string name, MethodInfo method) in resolved)
            {
                var watch = Stopwatch.StartNew();
                int code;
                try
                {
                    MethodInfo m = method;
                    code = CheckExit.Collect(() => m.Invoke(null, null));
                }
                catch (Exception e)
                {
                    Debug.LogError($"MULTIRUN: {name} threw - {e.GetBaseException().Message}");
                    code = 1;
                }

                watch.Stop();
                worst = Math.Max(worst, code);
                sb.Append($"  {name,-58} {watch.Elapsed.TotalSeconds,7:F1} s  {(code == 0 ? "ok" : "FAILED " + code.ToString(CultureInfo.InvariantCulture))}\n");
            }

            total.Stop();
            Debug.Log($"MULTIRUN: {resolved.Count} method(s), {total.Elapsed.TotalSeconds:F1} s of work in one launch:\n{sb}MULTIRUN: exiting {worst}.");
            EditorApplication.Exit(worst);
        }
    }
}
