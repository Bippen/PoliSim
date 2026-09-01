using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The coherence audit's **SIXTH sweep — evidence that would pass regardless.**
    ///
    /// <para><b>Why this class and not another.</b> It has five recorded instances and they are not a
    /// coincidence; they are this project's dominant failure mode:</para>
    /// <list type="number">
    /// <item>**C-C2's trajectory diff** — a diff that enumerated the no-policy trajectory only, and would
    /// have been byte-identical whether the change worked or not.</item>
    /// <item>**S-20's void films** — every `-shotelectionnight` film photographed the DESK, at *8
    /// captured, 0 failed, 0 overflows, exit 0*. The guards check what was DRAWN, never whether it was
    /// the thing under test.</item>
    /// <item>**Assertion 4 reporting itself untested** — a harness assertion nothing had ever made
    /// fire.</item>
    /// <item>**The Ramey basis** (D-13) — a published band quoted beside a column that is not the band's
    /// quantity. The comparison "passed" for as long as nobody read the paper.</item>
    /// <item>**The CRLF glance instruction** — the mirror image: an instruction that would have made a
    /// CORRECT readback read as a failed paste.</item>
    /// </list>
    ///
    /// <para>The common property is one sentence: ⚠ **the outcome of the test does not depend on the
    /// thing the test claims to be about.** Nothing decides that in general. What IS decidable, and what
    /// would have caught the sharpest of them, is far narrower and is what this check enforces:</para>
    ///
    /// <para><b>CLAUSE A — A REGISTERED CHECK MUST BE ABLE TO FAIL.</b> Every tool `CheckSuite` registers
    /// — the cheap suite and the simulation group — must contain a reachable-looking failure path: a
    /// `Debug.LogError`, or a `CheckExit.Finish` / `EditorApplication.Exit` with anything but a literal
    /// zero. A "check" whose only exit is `Finish(0)` reports clean **by construction**, and every run of
    /// the bar has been counting it.</para>
    ///
    /// <para>⚠ **Its first run found one**: `PublicationCadenceCheck`, one of the simulation checks,
    /// whose sole exit was `CheckExit.Finish(0)` — its own doc calls it a measurement. *"8 of 8 simulation
    /// checks clean"* had been counting a tool that could not say anything else. It was given the
    /// assertion its own documentation already named, rather than being quietly renamed.</para>
    ///
    /// <para><b>CLAUSE B — the census, reported and NOT enforced.</b> Every other tool under
    /// `Assets/Editor` with a `Run` is counted, and the ones with no failure path are named. Most are
    /// legitimate: **a measurement is not a test**, and a diagnostic whose job is to print a number should
    /// not invent a threshold to fail against. The census exists so the count is visible and so a tool
    /// that quietly moves from measuring to checking is noticed.</para>
    ///
    /// <para>⚠ <b>WHAT THIS CANNOT DO, stated here rather than discovered later.</b> It is a TEXT scan. It
    /// proves a failure path EXISTS in the file; it does not prove that path is reachable, that its
    /// condition can ever be true, or that it has ever fired. **The stronger form is a mutation probe** —
    /// break the subject, require the check to go red — which this project has done BY HAND for every
    /// guard it has armed (the throwaway probe file, deleted after). Automating that is the seventh sweep,
    /// and naming it here is the honest version of not having built it.</para>
    /// </summary>
    public static class EvidenceDiscriminationCheck
    {
        /// <summary>⚠ A registered check that cannot fail. **The ceiling is ZERO and is not a ratchet**:
        /// unlike a backlog of real findings, this one is always fixable in the file that has it — give
        /// the check an assertion, or take it out of the suite and call it the diagnostic it is.</summary>
        private const int RegisteredWithoutFailurePathCeiling = 0;

        /// <summary>
        /// A registration line in `CheckSuite` — a quoted name, then that type's `Run`.
        ///
        /// ⚠ **This comment used to carry a worked example with a made-up type name in it, and
        /// `PhantomGuardCheck` failed the suite on it within the hour** — *"a comment names this guard and
        /// no such type exists"*. That is the guard working exactly as designed, on the file whose whole
        /// subject is evidence that cannot tell truth from fiction. The example is described rather than
        /// spelled, and the incident is left here because it is a better argument for the guard than
        /// anything this file's own doc could say.
        /// </summary>
        private static readonly Regex Registration = new Regex(@"\(\s*""([A-Za-z0-9_]+)""\s*,\s*([A-Za-z0-9_]+)\.Run\s*\)");

        /// <summary>A failure path: a red log, or an exit with anything but a literal zero.</summary>
        private static readonly Regex FailurePath = new Regex(
            @"Debug\.LogError|CheckExit\.Finish\(\s*(?!0\s*\))|EditorApplication\.Exit\(\s*(?!0\s*\))");

        private static readonly Regex HasRun = new Regex(@"public\s+static\s+void\s+Run\s*\(");

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string editor = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Editor");
            string suitePath = Path.Combine(editor, "CheckSuite.cs");
            if (!File.Exists(suitePath))
            {
                Debug.LogError("EVIDENCE: CheckSuite.cs is not on disk, so the registered set cannot be read and this "
                               + "check would have verified NOTHING rather than found nothing.");
                CheckExit.Finish(1);
                return;
            }

            var registered = new List<string>();
            foreach (Match m in Registration.Matches(File.ReadAllText(suitePath)))
            {
                string type = m.Groups[2].Value;
                if (!registered.Contains(type)) { registered.Add(type); }
            }

            // The enumeration rule. A run that read zero registrations has checked nothing, and would
            // print exactly like a run where every registered check was sound.
            if (registered.Count == 0)
            {
                Debug.LogError("EVIDENCE: no registration lines were found in CheckSuite.cs. The suite is either empty or "
                               + "the registration shape changed - either way this run verified NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            var withoutFailure = new List<string>();
            var missingFile = new List<string>();
            foreach (string type in registered)
            {
                string path = Path.Combine(editor, type + ".cs");
                if (!File.Exists(path)) { missingFile.Add(type); continue; }
                if (!FailurePath.IsMatch(SourceText.WithoutComments(File.ReadAllText(path)))) { withoutFailure.Add(type); }
            }

            // CLAUSE B: the census over every editor tool with a Run, reported and not enforced.
            var census = new List<string>();
            int tools = 0;
            foreach (string path in Directory.GetFiles(editor, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(path);
                if (!HasRun.IsMatch(text)) { continue; }

                tools++;
                if (!FailurePath.IsMatch(SourceText.WithoutComments(text))) { census.Add(Path.GetFileNameWithoutExtension(path)); }
            }

            census.Sort(StringComparer.Ordinal);
            withoutFailure.Sort(StringComparer.Ordinal);

            var sb = new StringBuilder();
            sb.Append("=== The coherence audit (f): evidence that would pass regardless ===\n");
            sb.Append(F("    CLAUSE A - registered checks that must be able to fail: {0} registered in CheckSuite.\n", registered.Count));
            sb.Append(F("    {0} with NO failure path (ceiling {1}).\n", withoutFailure.Count, RegisteredWithoutFailurePathCeiling));
            foreach (string t in withoutFailure) { sb.Append("    ⚠ CANNOT FAIL  ").Append(t).Append('\n'); }
            if (missingFile.Count > 0)
            {
                sb.Append(F("    ⚠ {0} registered type(s) have no <Name>.cs beside CheckSuite and were not read: {1}\n",
                    missingFile.Count, string.Join(", ", missingFile.ToArray())));
            }

            sb.Append(F("\n    CLAUSE B - the census, REPORTED not enforced: {0} tool(s) under Assets/Editor declare a Run;\n", tools));
            sb.Append(F("    {0} of them contain no failure path at all.\n", census.Count));
            sb.Append("    ⚠ Most of those are legitimate: A MEASUREMENT IS NOT A TEST, and a diagnostic whose job is to\n");
            sb.Append("    print a number should not invent a threshold to fail against. The census is here so the count is\n");
            sb.Append("    visible and a tool that drifts from measuring to checking is noticed.\n");
            foreach (string t in census) { sb.Append("        ").Append(t).Append('\n'); }

            sb.Append("\n    ⚠ WHAT THIS CANNOT DO. It is a TEXT scan: it proves a failure path EXISTS, not that it is\n");
            sb.Append("    reachable, not that its condition can ever be true, and not that it has ever fired. The stronger\n");
            sb.Append("    form is a MUTATION PROBE - break the subject, require the check to go red - which this project has\n");
            sb.Append("    done BY HAND for every guard it has armed. Automating it is the seventh sweep, and saying so here\n");
            sb.Append("    is the honest version of not having built it.\n");

            // ⚠ A REGISTERED CHECK WHOSE FILE CANNOT BE FOUND IS A SILENT SKIP, AND IT IS THE SAME
            // TYPE-IS-ITS-FILE ASSUMPTION S-35 BURNED US ON (found by R-T3's retrofit, 2026-09-01). This
            // check maps a registered type name to `<Name>.cs`; a check declared in a differently-named
            // file was reported and then passed over, so clause A verified nothing about it while the
            // summary read clean. It fails now: an unreadable subject is not a passing subject.
            if (missingFile.Count > 0)
            {
                Debug.LogError("EVIDENCE: " + missingFile.Count + " registered check(s) have no <Name>.cs beside "
                               + "CheckSuite - " + string.Join(", ", missingFile.ToArray())
                               + ". Clause A verified NOTHING about them. ⚠ This check assumes a type lives in the file "
                               + "named after it, which is exactly the assumption that 'corrected' a correct document "
                               + "reference at S-35; the assumption stays, and the silent skip does not.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            RatchetLedger.Report("EvidenceDiscriminationCheck.NO_FAILURE_PATH", withoutFailure.Count, RegisteredWithoutFailurePathCeiling);
            if (withoutFailure.Count > RegisteredWithoutFailurePathCeiling)
            {
                Debug.LogError($"EVIDENCE: {withoutFailure.Count} REGISTERED check(s) contain no failure path - "
                               + string.Join(", ", withoutFailure.ToArray())
                               + ". A check whose only exit is Finish(0) reports clean BY CONSTRUCTION, and every run of the "
                               + "bar counts it as a pass. Give it the assertion its own doc names, or take it out of the "
                               + "suite and call it the diagnostic it is.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
