using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PoliSim.Testing;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// K321 (b)-(f), Code's recommendation on Elias's K320 (`docs/RULINGS_2026-10-23.md`): **THE FILM'S DECLUTTER JUDGE READS THE TABLES AS THE PERL
    /// TOOL DOES, AND IT HAS TEETH.** The film judges its own label table with `DeclutterRules`; the baseline and the reports come from
    /// `Tools/declutter_scan.pl`. Two implementations of one set of rules are held equal here by the fixture both must reproduce
    /// (`Tools/declutter_fixture/`; the perl half is `perl Tools/declutter_scan.pl selftest`), and over any set of real tables by
    /// `perl Tools/declutter_scan.pl parity`.
    /// <para>Asserted: (1) the rules file reads clean - every exemption a scope and a reason, every cap set; (2) the fixture's keys, read as a
    /// flagged table and as a legacy one, are exactly the hand-written expected keys; (3) the judge's teeth - the fixture against its planted
    /// baseline gives exactly the planted NEW and the planted STALE and fails - the planted row of another film mode on the same frame never
    /// stale, the planted Canvas row stale only when the table is a real film's; against a baseline of its own keys it passes with nothing new
    /// or stale; with no baseline, and with a baseline that holds nothing of its country, it fails without a verdict; (4) the generated baseline,
    /// once it exists, reads as one (its header says it is generated, every row a country, a width, a mode, a source, a capture, a kind). Until
    /// it is generated the check says so and passes: the films fail on it instead.</para>
    /// </summary>
    public static class DeclutterJudgeCheck
    {
        private const string Fixture = "Tools/declutter_fixture";

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== DeclutterJudgeCheck (K321): the film's declutter judge against the fixture the perl tool reproduces ===\n");
            int failures = 0;
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            void Fail(string line) { failures++; sb.Append("    FAIL      ").Append(line).Append('\n'); }
            void Ok(string line) { sb.Append("    ok        ").Append(line).Append('\n'); }

            // (1) the rules file
            var errors = new List<string>();
            DeclutterRules.RuleSet rules = DeclutterRules.Load(root, errors);
            foreach (string e in errors) { Fail(e); }
            if (errors.Count > 0) { Debug.LogError($"DECLUTTER JUDGE CHECK: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Ok($"(1) {DeclutterRules.RulesPath}: {rules.Exempt.Count} exemption(s) ({rules.Exempt.Count(e => e.All)} of every cap, {rules.Exempt.Count(e => !e.All)} of the at-rest caps only), every cap set");

            string table = Path.Combine(root, Fixture, "fixture_sweden_1280_labels.tsv");
            foreach (string need in new[] { table, Path.Combine(root, Fixture, "expected_keys.tsv"), Path.Combine(root, Fixture, "expected_keys_legacy.tsv"), Path.Combine(root, Fixture, "expected_judge.tsv"), Path.Combine(root, Fixture, "fixture_baseline.tsv") })
            {
                if (!File.Exists(need)) { Debug.LogError($"  {need} is missing - VERIFIED NOTHING"); CheckExit.Finish(1); return; }
            }

            // (2) the fixture's keys, flagged and legacy
            List<DeclutterRules.Row> rows = DeclutterRules.ReadTable(table);
            foreach ((bool legacy, string expected) in new[] { (false, "expected_keys.tsv"), (true, "expected_keys_legacy.tsv") })
            {
                List<string> got = DeclutterRules.KeyLines("sweden", 1280, DeclutterRules.Read(rows, rules, legacy));
                List<string> want = Lines(Path.Combine(root, Fixture, expected));
                int diff = Compare(want, got, legacy ? "legacy key" : "key", sb);
                if (diff > 0 || want.Count == 0) { failures += Math.Max(diff, 1); sb.Append($"    FAIL      (2) {expected}: {diff} difference(s) over {want.Count} expected key(s)\n"); }
                else { Ok($"(2) the fixture read {(legacy ? "as a legacy table" : "as a flagged table")}: its {got.Count} key(s) are {expected}'s, every kind among them"); }
            }

            // (3) the judge's teeth
            DeclutterRules.Verdict planted = DeclutterRules.Judge(root, table, null, "Sweden", real: false, DeclutterRules.SweepMode, Path.Combine(root, Fixture, "fixture_baseline.tsv"));
            var judged = planted.New.Select(b => $"DECLUTTER NEW {b.Capture} {b.Kind}").Concat(planted.Stale.Select(b => $"DECLUTTER STALE {b.Capture} {b.Kind}")).ToList();
            judged.Add("exit " + planted.Code.ToString(CultureInfo.InvariantCulture));
            int judgeDiff = Compare(Lines(Path.Combine(root, Fixture, "expected_judge.tsv")), judged, "judge", sb);
            if (judgeDiff > 0 || planted.Fatal != null) { failures += Math.Max(judgeDiff, 1); sb.Append($"    FAIL      (3) the planted baseline: {judgeDiff} difference(s) from expected_judge.tsv{(planted.Fatal != null ? "; no verdict - " + planted.Fatal : string.Empty)}\n"); }
            else { Ok($"(3) the planted baseline: exactly the planted NEW ({planted.New[0].Kind} '{planted.New[0].Text}') and STALE ({planted.Stale[0].Kind} '{planted.Stale[0].Key}'), and the film fails"); }

            DeclutterRules.Verdict asReal = DeclutterRules.Judge(root, table, null, "sweden", real: true, DeclutterRules.SweepMode, Path.Combine(root, Fixture, "fixture_baseline.tsv"));
            if (asReal.Stale.Count == planted.Stale.Count + 1 && asReal.Stale.Exists(b => b.Source == "canvas")) { Ok("(3) read as a real film's table, the planted Canvas row goes stale too: a dry film never judges what it cannot see"); }
            else { Fail($"(3) read as a real film's table the planted Canvas row must go stale too - {asReal.Stale.Count} stale against {planted.Stale.Count} dry"); }

            string scratch = Path.Combine(Path.GetTempPath(), "declutter_judge_check_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                DeclutterRules.Reading reading = DeclutterRules.Read(rows, rules, legacy: false);
                string own = Path.Combine(scratch, "own.tsv");
                File.WriteAllText(own, BaselineOf("sweden", 1280, reading), new UTF8Encoding(false));
                DeclutterRules.Verdict clean = DeclutterRules.Judge(root, table, null, "sweden", real: false, DeclutterRules.SweepMode, own);
                if (clean.Code == 0 && clean.New.Count == 0 && clean.Stale.Count == 0 && clean.Fatal == null) { Ok($"(3) against a baseline of its own {reading.AtRest.Count + reading.Slips.Count} key(s): nothing new, nothing stale, the film passes"); }
                else { Fail($"(3) against a baseline of its own keys the fixture must pass - {clean.New.Count} new, {clean.Stale.Count} stale, {clean.Fatal ?? "judged"}"); }

                DeclutterRules.Verdict none = DeclutterRules.Judge(root, table, null, "sweden", real: false, DeclutterRules.SweepMode, Path.Combine(scratch, "absent.tsv"));
                if (none.Code == 1 && none.Fatal != null && none.Fatal.Contains("no baseline")) { Ok("(3) with no baseline: no verdict, and the film fails telling the integrator to generate it"); }
                else { Fail($"(3) with no baseline the film must fail without a verdict - code {none.Code}, {none.Fatal ?? "it judged"}"); }

                DeclutterRules.Verdict elsewhere = DeclutterRules.Judge(root, table, null, "germany", real: false, DeclutterRules.SweepMode, own);
                if (elsewhere.Code == 1 && elsewhere.Fatal != null && elsewhere.Fatal.Contains("holds nothing")) { Ok("(3) a country the baseline holds nothing of: no verdict, and the film fails"); }
                else { Fail($"(3) a country the baseline holds nothing of must fail without a verdict - code {elsewhere.Code}, {elsewhere.Fatal ?? "it judged"}"); }
            }
            finally
            {
                try { Directory.Delete(scratch, true); } catch (IOException) { }
            }

            // (4) the generated baseline
            string baseline = Path.Combine(root, DeclutterRules.BaselinePath);
            if (!File.Exists(baseline))
            {
                sb.Append($"    pending   (4) {DeclutterRules.BaselinePath} is not generated yet - every film and dry film fails on it until the integrator runs {DeclutterRules.GenerateCommand}\n");
            }
            else
            {
                var baselineErrors = new List<string>();
                List<DeclutterRules.BaselineRow> held = DeclutterRules.ReadBaseline(baseline, baselineErrors);
                foreach (string e in baselineErrors.Take(10)) { Fail("(4) " + e); }
                if (baselineErrors.Count == 0)
                {
                    var scopes = held.Select(b => b.Country + " " + b.Width.ToString(CultureInfo.InvariantCulture) + " " + b.Mode + " " + b.Source).Distinct().OrderBy(s => s, StringComparer.Ordinal);
                    Ok($"(4) {DeclutterRules.BaselinePath}: {held.Count(b => b.Kind != DeclutterRules.Taken)} grandfathered breach(es) over {held.Count(b => b.Kind == DeclutterRules.Taken)} capture(s) taken; scopes {string.Join(", ", scopes)}");
                }
            }

            if (failures > 0) { Debug.LogError($"DECLUTTER JUDGE CHECK: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.Append("DECLUTTER JUDGE CHECK: PASS\n").ToString());
            CheckExit.Finish(0);
        }

        /// <summary>A file's lines, comments and blanks dropped, a carriage return trimmed.</summary>
        private static List<string> Lines(string path) =>
            File.ReadAllLines(path, Encoding.UTF8).Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal)).ToList();

        /// <summary>The lines one side holds and the other does not, printed; their count.</summary>
        private static int Compare(List<string> want, List<string> got, string what, StringBuilder sb)
        {
            var w = new HashSet<string>(want, StringComparer.Ordinal);
            var g = new HashSet<string>(got, StringComparer.Ordinal);
            int n = 0;
            foreach (string s in want) { if (!g.Contains(s)) { n++; sb.Append($"    MISSING   {what}: {s}\n"); } }
            foreach (string s in got) { if (!w.Contains(s)) { n++; sb.Append($"    EXTRA     {what}: {s}\n"); } }
            return n;
        }

        /// <summary>A baseline in the generated form holding exactly <paramref name="reading"/>'s keys and captures - the clean case's.</summary>
        private static string BaselineOf(string country, int width, DeclutterRules.Reading reading)
        {
            var sb = new StringBuilder("# GENERATED by Tools/declutter_scan.pl baseline - the form of one, written by DeclutterJudgeCheck for its clean case\n");
            sb.Append(DeclutterRules.BaselineHeader).Append('\n');
            string scope = country + "\t" + width.ToString(CultureInfo.InvariantCulture) + "\t" + DeclutterRules.SweepMode;
            foreach (string capture in reading.Captures) { sb.Append($"{scope}\timgui\t{capture}\t{DeclutterRules.Taken}\t-\n"); }
            foreach (DeclutterRules.Breach b in reading.All) { sb.Append($"{scope}\t{b.KeyLine}\n"); }
            return sb.ToString();
        }
    }
}
