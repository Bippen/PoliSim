using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **Which BATCH audits a given ratchet — the enrolment's blind spot, closed (2026-09-01).**
    ///
    /// <para>⚠ <b>The finding this exists for.</b> `RatchetSlackCheck`'s enrolment asked one question of
    /// every check file declaring a ceiling: *does its source contain a `RatchetLedger.Report` call?* It
    /// answered "10 declare, 0 unreported" and printed its ledger table as the complete coverage — while
    /// **two of those files report into a ledger nobody reads.** `CohortAgingStepDiagnostic` and
    /// `PublicationCadenceCheck` are registered in `RunSimulationBatch`, which did not run the slack audit
    /// at all, so their ratchets were built into a per-process ledger and discarded at exit.</para>
    ///
    /// <para>⚠ <b>It is §161's own class, one level up.</b> §161 closed *"a ratchet that does not report"*
    /// and left open *"a ratchet that reports into a ledger nobody audits"* — and the enrolment's honest-
    /// looking `0 unreported` is what made the second invisible. **A source-text scan for a call proves the
    /// call was WRITTEN, never that it EXECUTED**, which is the weaker of the two claims and was being read
    /// as the stronger one.</para>
    ///
    /// <para><b>What this is.</b> The registration tables themselves — <see cref="CheckSuite.CheapGroup"/>
    /// and <see cref="CheckSuite.SimulationGroup"/> — rather than a scan of them or a hand-written list
    /// beside them. ⚠ **A hand table would be a second thing to keep true**, and a check registered in
    /// neither group would sit in it looking registered. Asking the arrays makes "registered in no group"
    /// a state that can be reported rather than one that hides.</para>
    ///
    /// <para><b>R-T3 — every consumer, enumerated.</b> This instrument now has exactly ONE:
    /// <see cref="RatchetSlackCheck"/>, which fails a declaring check that is in the running group and
    /// absent from the ledger, and names the deferred ones instead of omitting them.
    ///
    /// <para>⚠ **It had two until 2026-09-01**, when the audit era closed and the residue check was retired
    /// with the list it read. **R-T3 runs in this direction as well**: an enumeration is a claim about a
    /// SET, and a set that loses a member is as wrong as one that gains an unlisted one. The count in this
    /// sentence is part of the claim, which is why it is written as a number rather than left implied.</para></para>
    /// </summary>
    public static class RatchetResidency
    {
        /// <summary>Which registration table names a check. `None` is a hard fault: a ratchet armed for a
        /// group that never runs is armed for nobody.</summary>
        public enum Group { None, Cheap, Simulation }

        /// <summary>Which registration table is running, STATED by CheckSuite before it runs a group.
        /// ⚠ It is not inferred from the ledger: an empty group and a group whose checks all failed before
        /// reporting look identical from there, and guessing between them is how a blind audit reads clean.</summary>
        public static Group ActiveGroup = Group.None;

        public struct Entry
        {
            /// <summary>The check's file/type name, e.g. `CohortAgingStepDiagnostic`.</summary>
            public string Check;

            /// <summary>Which batch registers it.</summary>
            public Group Registers;

            /// <summary>Whether its source contains a `RatchetLedger.Report` call at all. ⚠ This is the
            /// WRITTEN claim; that the call ran is a separate question the ledger answers.</summary>
            public bool HasReportCall;

            /// <summary>The ledger names its `Report` calls pass, read from the string literals. Used only
            /// for a check whose group is not the running one — its ledger entry cannot exist to be read,
            /// so the M-R row is matched against the name the source will report under.</summary>
            public List<Reported> ReportsAs;
        }

        /// <summary>One `Report` call read out of a check's source: the ledger name it will use, and
        /// whether it passes `isFloor`. ⚠ The direction is carried because a FLOOR is not a backlog — S-37 —
        /// and a consumer that demands a backlog row for a floor would be demanding a row for a bound that
        /// is supposed to stay where it is.</summary>
        public struct Reported
        {
            public string Name;
            public bool IsFloor;
        }

        /// <summary>An `int` constant whose name ends in Ceiling or Ratchet — this repo's own naming for a
        /// backlog bound. ⚠ A NAMING convention doing structural work, and the enrolment's one soft spot:
        /// a bound called something else escapes. Stated where it bites rather than where it is convenient.</summary>
        private static readonly Regex DeclaresRatchet = new Regex(
            @"const\s+int\s+[A-Za-z0-9_]*(Ceiling|Ratchet)\b");

        /// <summary>The first argument of a `RatchetLedger.Report` call, when it is a plain literal.</summary>
        private static readonly Regex ReportName = new Regex(
            "RatchetLedger\\.Report\\(\\s*\"([^\"]+)\"([^)]*)\\)");

        /// <summary>Every check file under `Assets/Editor` that declares a ratchet constant, with the group
        /// that registers it. ⚠ Comments are stripped (R-T3's enrolment): a comment naming
        /// `RatchetLedger.Report` would otherwise make an unreporting check look enrolled — which is the
        /// defect §161 catalogued five times.</summary>
        public static List<Entry> Enumerate()
        {
            var result = new List<Entry>();

            string editorDir = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Editor");
            if (!Directory.Exists(editorDir)) { return result; }

            var cheap = new HashSet<string>(CheckSuite.CheapGroup);
            var simulation = new HashSet<string>(CheckSuite.SimulationGroup);

            foreach (string path in Directory.GetFiles(editorDir, "*.cs", SearchOption.TopDirectoryOnly))
            {
                string text = SourceText.WithoutComments(File.ReadAllText(path));
                if (!text.Contains("public static void Run")) { continue; }
                if (!DeclaresRatchet.IsMatch(text)) { continue; }

                string check = Path.GetFileNameWithoutExtension(path);

                var reportsAs = new List<Reported>();
                foreach (Match m in ReportName.Matches(text))
                {
                    reportsAs.Add(new Reported
                    {
                        Name = m.Groups[1].Value,
                        IsFloor = m.Groups[2].Value.Contains("true"),
                    });
                }

                result.Add(new Entry
                {
                    Check = check,
                    Registers = cheap.Contains(check) ? Group.Cheap
                              : simulation.Contains(check) ? Group.Simulation
                              : Group.None,
                    HasReportCall = text.Contains("RatchetLedger.Report"),
                    ReportsAs = reportsAs,
                });
            }

            result.Sort((a, b) => string.CompareOrdinal(a.Check, b.Check));
            return result;
        }
    }
}