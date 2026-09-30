using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The hang watchdog's other half (ruled 2026-09-30, round 4 follow-up 3; `COMPLETED.md` §702): **AT EXIT, WHAT IS STILL ALIVE.** Four batch runs
    /// have hung after printing their result - each in teardown, each on *"CodeReloadManager destroyed"* (`Tools/unity_hangs.tsv`) - and no code of ours
    /// runs there. So the state is logged on the way out, where our code still runs: when the Editor begins to quit (`EditorApplication.quitting`,
    /// raised on the path of every one of the 32 `EditorApplication.Exit` sites; `AppDomain.ProcessExit` was tried and is not reached - managed code is
    /// gone by then).
    /// <para>Logged: the process's OS threads by NAME from the Windows snapshot (Mono's `Process.Threads` reads none inside the Editor, and Mono exposes no
    /// list of managed `Thread` objects - managed threads run on these), and the five busiest; and what THIS project started that could outlive a run - threads, timers, file watchers, child processes -
    /// counted by a scan of `Assets/`' own source at exit, so the line is measured, not asserted (at §702: none; four synchronous `Process.Start`
    /// calls, each disposed). The children Unity itself keeps (the import worker, the shader compiler, the package server) are listed by the
    /// PowerShell side when the watchdog ends a process (`Tools/unity_end_own.ps1`).</para>
    /// </summary>
    [InitializeOnLoad]
    public static class ExitDiagnostics
    {
        static ExitDiagnostics()
        {
            EditorApplication.quitting += () => Log("quitting");
        }

        /// <summary>One EXITDIAG block: the moment, the threads, and what the project's own source starts.</summary>
        public static void Log(string when)
        {
            try
            {
                var sb = new StringBuilder();
                int pid = Process.GetCurrentProcess().Id;
                // Mono's Process.Threads reads nothing inside the Editor (the planted run printed 0) - the Windows thread snapshot is read instead,
                // with each thread's NAME (GetThreadDescription): Unity names its threads, and a name is what points at a hang's cause.
                List<(int Id, string Name, double Cpu)> threads = NativeThreads.Of(pid);
                sb.Append($"EXITDIAG: {when} - pid {pid}, {threads.Count} OS thread(s) alive (Mono exposes no managed Thread list; managed threads run on these)\n");
                var byName = new SortedDictionary<string, int>(StringComparer.Ordinal);
                foreach (var t in threads)
                {
                    // a numbered pool ("Background Job.Worker 7") is one name with a count
                    string k = string.IsNullOrEmpty(t.Name) ? "(unnamed)" : System.Text.RegularExpressions.Regex.Replace(t.Name, @"\s*\d+$", " #");
                    byName[k] = byName.TryGetValue(k, out int n) ? n + 1 : 1;
                }
                sb.Append("EXITDIAG:   by name - ").Append(string.Join(", ", byName.Select(kv => kv.Value > 1 ? kv.Key + " x" + kv.Value : kv.Key))).Append('\n');
                foreach ((int id, string name, double cpu) in threads.OrderByDescending(t => t.Cpu).Take(5))
                {
                    sb.Append($"EXITDIAG:   busiest - thread {id} '{(string.IsNullOrEmpty(name) ? "(unnamed)" : name)}': {cpu.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} s CPU\n");
                }
                sb.Append("EXITDIAG:   the project's own source starts - ").Append(ProjectStarts()).Append('\n');
                Debug.Log(sb.ToString());
            }
            catch (Exception e)
            {
                Debug.Log("EXITDIAG: " + when + " - the state could not be read: " + e.Message);
            }
        }

        /// <summary>The watchdog's planted proof: a batch method that prints a result line and returns without exiting - under -batchmode with no -quit the
        /// Editor stays alive, the shape of every recorded hang. Run it through Tools/unity_run.ps1: the watchdog must end it 90 s after this line and keep
        /// the code 0 it prints.</summary>
        public static void PlantedHang()
        {
            Log("the planted hang, before its result line");
            Debug.Log("CHECKS: the watchdog's planted hang - it printed this result and will not exit - exiting 0.");
        }

        /// <summary>The process's threads from the Windows ToolHelp snapshot: id, name (GetThreadDescription, Windows 10 1607+) and CPU seconds.</summary>
        private static class NativeThreads
        {
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            private struct ThreadEntry32
            {
                public uint dwSize, cntUsage, th32ThreadID, th32OwnerProcessID;
                public int tpBasePri, tpDeltaPri;
                public uint dwFlags;
            }

            [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint pid);
            [System.Runtime.InteropServices.DllImport("kernel32.dll")] private static extern bool Thread32First(IntPtr snapshot, ref ThreadEntry32 entry);
            [System.Runtime.InteropServices.DllImport("kernel32.dll")] private static extern bool Thread32Next(IntPtr snapshot, ref ThreadEntry32 entry);
            [System.Runtime.InteropServices.DllImport("kernel32.dll")] private static extern IntPtr OpenThread(uint access, bool inherit, uint tid);
            [System.Runtime.InteropServices.DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
            [System.Runtime.InteropServices.DllImport("kernel32.dll")] private static extern int GetThreadDescription(IntPtr thread, out IntPtr description);
            [System.Runtime.InteropServices.DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr memory);
            [System.Runtime.InteropServices.DllImport("kernel32.dll")] private static extern bool GetThreadTimes(IntPtr thread, out long creation, out long exit, out long kernel, out long user);

            public static List<(int Id, string Name, double Cpu)> Of(int pid)
            {
                var list = new List<(int, string, double)>();
                IntPtr snap = CreateToolhelp32Snapshot(0x4, 0);   // TH32CS_SNAPTHREAD: every thread on the machine, filtered to ours below
                if (snap == IntPtr.Zero || snap == new IntPtr(-1)) { return list; }
                try
                {
                    var e = new ThreadEntry32 { dwSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(ThreadEntry32)) };
                    for (bool ok = Thread32First(snap, ref e); ok; ok = Thread32Next(snap, ref e))
                    {
                        if (e.th32OwnerProcessID != (uint)pid) { continue; }
                        string name = null;
                        double cpu = 0.0;
                        IntPtr h = OpenThread(0x0800, false, e.th32ThreadID);   // THREAD_QUERY_LIMITED_INFORMATION
                        if (h != IntPtr.Zero)
                        {
                            try
                            {
                                if (GetThreadDescription(h, out IntPtr d) >= 0 && d != IntPtr.Zero) { name = System.Runtime.InteropServices.Marshal.PtrToStringUni(d); LocalFree(d); }
                                if (GetThreadTimes(h, out long _, out long _, out long k, out long u)) { cpu = (k + u) / 1e7; }
                            }
                            catch (EntryPointNotFoundException) { }
                            finally { CloseHandle(h); }
                        }
                        list.Add(((int)e.th32ThreadID, name, cpu));
                    }
                }
                finally { CloseHandle(snap); }
                return list;
            }
        }

        /// <summary>Counted in Assets/'s own .cs files: what could outlive a run if it were started and not stopped.</summary>
        private static string ProjectStarts()
        {
            string[] kinds = { "new Thread(", "new System.Threading.Thread(", "Task.Run(", "ThreadPool.", "new Timer(", "System.Threading.Timer", "FileSystemWatcher", "Process.Start(" };
            var counts = new int[kinds.Length];
            string self = Path.GetFileName(typeof(ExitDiagnostics).Name) + ".cs";
            foreach (string file in Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file) == self) { continue; }
                string text = SourceText.ReadWithoutComments(file);   // a start named in a comment is history, not a start (CommentImmunityCheck's enrolment)
                for (int i = 0; i < kinds.Length; i++)
                {
                    for (int at = text.IndexOf(kinds[i], StringComparison.Ordinal); at >= 0; at = text.IndexOf(kinds[i], at + 1, StringComparison.Ordinal)) { counts[i]++; }
                }
            }
            int threads = counts[0] + counts[1] + counts[2] + counts[3];
            int timers = counts[4] + counts[5];
            return $"{threads} thread(s) or pool task(s), {timers} timer(s), {counts[6]} file watcher(s), {counts[7]} child process start(s) (each a synchronous, disposed call when this was written)";
        }
    }
}
