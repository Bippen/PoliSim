using System;
using System.Diagnostics;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Asks whether this work exists anywhere but this disk.
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14): the commits on the current branch that its configured
    /// upstream does not have, plus whether an upstream is configured at all. Nothing about their content,
    /// nothing about other branches, and nothing about whether the remote is reachable right now — the
    /// count comes from local refs, so it is honest offline and says so.</para>
    ///
    /// ⚠ **WHY IT EXISTS: 188 commits sat with no off-machine copy and nothing noticed.** Every other
    /// check in this project asks whether an ASSET is correct; not one asked whether the WORK still
    /// exists if the disk does not. `git status` showed `## main...origin/main [gone]`, which reads as
    /// *the remote is gone* — it actually meant the local tracking ref had never been created, while
    /// `origin` was reachable the whole time and a plain `git push` was a clean fast-forward. **A missing
    /// tracking ref and a missing remote are indistinguishable from inside the repository**, and only one
    /// of them is survivable.
    ///
    /// <para>This is rule 14 pointed at the repo instead of at a sprite: the enumeration gap was not
    /// inside any check, it was the population none of them was over. A backup is a check whose scope is
    /// the work itself.</para>
    /// </summary>
    public static class UpstreamCheck
    {
        /// <summary>Warn above this many unpushed commits. Low on purpose — the failure mode is silent accumulation, and by the time it is obvious it is already the whole day's work.</summary>
        private const int WarnAheadOf = 10;

        public static void Run()
        {
            if (!TryGit("rev-parse --abbrev-ref --symbolic-full-name @{u}", out string upstream))
            {
                Debug.LogError("UPSTREAM: no upstream configured for the current branch. This work exists " +
                               "on ONE DISK. `git push -u origin <branch>` — 188 commits once sat like this.");
                CheckExit.Finish(1);
                return;
            }

            if (!TryGit("rev-list --count @{u}..HEAD", out string aheadRaw) ||
                !int.TryParse(aheadRaw, out int ahead))
            {
                Debug.LogError("UPSTREAM: could not count commits ahead of " + upstream +
                               ". VERIFIED NOTHING — this is not evidence that the work is pushed.");
                CheckExit.Finish(1);
                return;
            }

            // The reachability half. `rev-parse @{u}` resolving proves the tracking ref EXISTS locally;
            // it does not prove the remote still has it. That distinction is exactly what "[gone]" hid.
            bool upstreamResolves = TryGit("rev-parse --verify --quiet @{u}", out string upstreamSha)
                                    && !string.IsNullOrEmpty(upstreamSha);

            Debug.Log($"UPSTREAM: tracking {upstream}, {ahead} commit(s) ahead, " +
                      $"tracking ref {(upstreamResolves ? "resolves" : "DOES NOT RESOLVE")}.");

            if (!upstreamResolves)
            {
                Debug.LogError($"UPSTREAM: {upstream} does not resolve locally. Run `git fetch` — a missing " +
                               "tracking ref reads as a missing remote and is not one.");
                CheckExit.Finish(1);
                return;
            }

            if (ahead > WarnAheadOf)
            {
                Debug.LogError($"UPSTREAM: {ahead} commits ahead of {upstream}, above the {WarnAheadOf} " +
                               "threshold. That work exists on one disk.");
                CheckExit.Finish(1);
                return;
            }

            CheckExit.Finish(0);
        }

        private static bool TryGit(string arguments, out string output)
        {
            output = null;
            try
            {
                var info = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = System.IO.Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using (Process process = Process.Start(info))
                {
                    if (process == null)
                    {
                        return false;
                    }

                    output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(10000);
                    return process.ExitCode == 0;
                }
            }
            catch (Exception)
            {
                // git absent, or not a repository. Reported by the caller as "verified nothing" rather
                // than passing quietly - a check that cannot run is not a check that passed.
                return false;
            }
        }
    }
}
