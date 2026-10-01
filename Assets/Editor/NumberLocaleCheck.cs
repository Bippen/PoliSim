using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §568 (2026-09-22, drift row D1), §718 (Elias's ruling of 2026-10-01, item 9: <i>"one fixed English culture for all dates and numbers, with a
    /// check that fails on machine-locale formatting"</i>) — **the desk's one culture is installed, in one place, and nothing the game formats reads
    /// the machine's.**
    ///
    /// <para><b>What it decides.</b> (1) <see cref="UiCulture.Install"/> is CALLED from the game's own startup - exactly once in the runtime sources;
    /// the harness's call is allowed beside it. (2) No runtime source builds or reads another culture - <c>new CultureInfo(</c>,
    /// <c>GetCultureInfo(</c>, <c>CreateSpecificCulture(</c>, <c>InstalledUICulture</c>, <c>RegionInfo</c> - outside <see cref="UiCulture"/> and the
    /// film harness (whose <c>-shotlocale=</c> plays a foreign machine on purpose). (3) THE BATTERY: under each of four hostile machine cultures
    /// (sv-SE, de-DE, fr-FR, en-US), the install, then the formats the game's surfaces use - the desk calendar's month (<c>MMM</c>, upper-cased),
    /// the sheet's <c>MMMM yyyy</c> and its first weekday and weekday names, a long date, a fixed-point figure, a negative figure through an
    /// interpolation, a grouped integer through <c>string.Format</c> - each must read the one English culture's. (4) THE CHECK'S TEETH: the same
    /// battery WITHOUT the install, under sv-SE, must DIFFER - so the check is shown failing on machine-locale formatting, every run.</para>
    ///
    /// <para>⚠ <b>Why it is not a scan for bad call sites.</b> There were 195 numeric interpolations when §568 counted (141 with a format in the four
    /// runtime folders today), every one taking the thread's culture. The culture is a property of the SURFACE, set once for the thread that draws
    /// it; what this check guards is that the one statement is still there, still says what the ruling says, and that nothing builds a second.</para>
    /// </summary>
    public static class NumberLocaleCheck
    {
        private static readonly string[] Hostile = { "sv-SE", "de-DE", "fr-FR", "en-US" };
        private static readonly string[] Forbidden = { "new CultureInfo(", "new System.Globalization.CultureInfo(", "GetCultureInfo(", "CreateSpecificCulture(", "InstalledUICulture", "RegionInfo" };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var runtimeCallers = new List<string>();
            var harnessCallers = new List<string>();
            var otherCultures = new List<string>();
            foreach (string file in Directory.GetFiles(Path.Combine(projectRoot, "Assets/Scripts"), "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(projectRoot.Length + 1).Replace('\\', '/');
                if (rel.EndsWith("UiCulture.cs", StringComparison.Ordinal)) { continue; }
                bool harness = rel.StartsWith("Assets/Scripts/Testing/", StringComparison.Ordinal);

                // COMMENTS STRIPPED (the immunity census enrols this check): a commented-out call is history, not an install site.
                string code = SourceText.ReadWithoutComments(file);
                foreach (int at in Occurrences(code, "UiCulture.Install("))
                {
                    string where = rel + ":" + LineOf(code, at).ToString(CultureInfo.InvariantCulture);
                    if (harness) { harnessCallers.Add(where); } else { runtimeCallers.Add(where); }
                }
                if (harness) { continue; }
                foreach (string token in Forbidden)
                {
                    foreach (int at in Occurrences(code, token)) { otherCultures.Add($"{rel}:{LineOf(code, at).ToString(CultureInfo.InvariantCulture)} ({token})"); }
                }
            }

            var sb = new StringBuilder();
            sb.Append("=== NumberLocaleCheck (§568, §718): one fixed English culture for all dates and numbers ===\n");
            sb.Append($"  runtime install site(s): {(runtimeCallers.Count == 0 ? "NONE" : string.Join(", ", runtimeCallers))}\n");
            sb.Append($"  harness install site(s): {(harnessCallers.Count == 0 ? "none" : string.Join(", ", harnessCallers))}\n");
            sb.Append($"  the installed culture: {UiCulture.English.Name} names and calendar, the invariant number format\n");
            int failures = 0;
            if (runtimeCallers.Count != 1) { sb.Append($"  ⚠ FAULT  the runtime installs the culture in {runtimeCallers.Count} place(s); it is stated ONCE, at the game's start\n"); failures++; }
            if (otherCultures.Count > 0) { sb.Append($"  ⚠ FAULT  a runtime source builds or reads another culture: {string.Join("; ", otherCultures)}\n"); failures++; }

            CultureInfo savedCulture = Thread.CurrentThread.CurrentCulture, savedUi = Thread.CurrentThread.CurrentUICulture;
            CultureInfo savedDefault = CultureInfo.DefaultThreadCurrentCulture, savedDefaultUi = CultureInfo.DefaultThreadCurrentUICulture;
            try
            {
                string[] expected = Battery(UiCulture.English, install: false);   // the battery read straight off the English culture
                string[] labels = { "the desk calendar's month", "the sheet's month and year", "the first weekday", "Monday's short name", "a long date", "a fixed-point figure", "a negative figure (interpolated)", "a grouped integer (string.Format)" };
                string[] literal = { "JAN", "MARCH 2026", "Monday", "Mon", "1 October 2026", "1234.5", "-3.25", "1,234,567" };
                for (int i = 0; i < literal.Length; i++)
                {
                    if (expected[i] != literal[i]) { sb.Append($"  ⚠ FAULT  the English culture reads {labels[i]} as \"{expected[i]}\", not \"{literal[i]}\"\n"); failures++; }
                }
                foreach (string machine in Hostile)
                {
                    string[] got = Battery(new CultureInfo(machine), install: true);
                    var wrong = new List<string>();
                    for (int i = 0; i < expected.Length; i++) { if (got[i] != expected[i]) { wrong.Add($"{labels[i]} \"{got[i]}\" for \"{expected[i]}\""); } }
                    sb.Append(wrong.Count == 0 ? $"  {machine,-6} machine, installed: all {expected.Length} formats read English\n" : $"  ⚠ FAULT  {machine} machine, installed: {string.Join("; ", wrong)}\n");
                    if (wrong.Count > 0) { failures++; }
                }
                string[] raw = Battery(new CultureInfo("sv-SE"), install: false);
                int differing = 0;
                for (int i = 0; i < expected.Length; i++) { if (raw[i] != expected[i]) { differing++; } }
                sb.Append($"  the teeth: sv-SE with NO install reads {differing} of {expected.Length} differently (month \"{raw[0]}\", the sheet \"{raw[1]}\", the figure \"{raw[5]}\")\n");
                if (differing < 3) { sb.Append("  ⚠ FAULT  the battery cannot tell a machine-locale reading from the English one - the check has no teeth\n"); failures++; }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = savedCulture; Thread.CurrentThread.CurrentUICulture = savedUi;
                CultureInfo.DefaultThreadCurrentCulture = savedDefault; CultureInfo.DefaultThreadCurrentUICulture = savedDefaultUi;
            }

            sb.Append($"\n=== NumberLocaleCheck: {(failures == 0 ? "CLEAN" : failures + " fault(s)")} ===\n");
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>The formats the surfaces use, each as the game writes it (no culture named - the thread's), on a thread set to
        /// <paramref name="machine"/> and, where <paramref name="install"/>, after the game's own install.</summary>
        private static string[] Battery(CultureInfo machine, bool install)
        {
            Thread.CurrentThread.CurrentCulture = machine;
            Thread.CurrentThread.CurrentUICulture = machine;
            if (install) { UiCulture.Install(); }
            var jan = new DateTime(2026, 1, 11);
            var march = new DateTime(2026, 3, 1);
            DateTimeFormatInfo sheet = DateTimeFormatInfo.CurrentInfo;
            float negative = -3.25f;
            return new[]
            {
                jan.ToString("MMM").ToUpperInvariant(),
                march.ToString("MMMM yyyy", CultureInfo.CurrentCulture).ToUpper(CultureInfo.CurrentCulture),
                sheet.FirstDayOfWeek.ToString(),
                sheet.GetAbbreviatedDayName(DayOfWeek.Monday),
                new DateTime(2026, 10, 1).ToString("d MMMM yyyy"),
                1234.5f.ToString("F1"),
                $"{negative:F2}",
                string.Format("{0:N0}", 1234567),
            };
        }

        private static IEnumerable<int> Occurrences(string code, string token)
        {
            for (int at = code.IndexOf(token, StringComparison.Ordinal); at >= 0; at = code.IndexOf(token, at + 1, StringComparison.Ordinal)) { yield return at; }
        }

        private static int LineOf(string code, int at)
        {
            int line = 1;
            for (int i = 0; i < at; i++) { if (code[i] == '\n') { line++; } }
            return line;
        }
    }
}
