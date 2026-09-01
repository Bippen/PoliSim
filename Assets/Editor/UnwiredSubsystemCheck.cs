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
    /// The coherence audit's fifth sweep — **a subsystem the game does not call.**
    ///
    /// <para><b>Why it exists, and it is not a hypothetical.</b> `DeadStateCheck` scans PRIVATE
    /// declarations, so it cannot see a public one. On 2026-09-01, answering C-N1, `TacticalVoting` turned
    /// out to be **built, harness-proven, and called from nowhere in the game**: its public entry points
    /// appear in one file outside their own — `TacticalVotingHarness` — and `ElectionDay` never mentions a
    /// poll. A whole modelled behaviour, with a passing harness, wired to nothing, and **the coherence
    /// audit was green throughout.**</para>
    ///
    /// <para><b>THE ENUMERATION.</b> Every `.cs` file under <c>Assets/Scripts</c> that declares at least
    /// one `public static` method. A FILE is <b>UNWIRED</b> when **not one** of its public static methods
    /// is named anywhere outside <c>Assets/Editor</c> and <c>Assets/Scripts/Testing</c> — that is, when
    /// only harnesses, diagnostics and capture drivers reach any part of it.</para>
    ///
    /// <para>⚠ <b>JUDGED AT THE FILE, NOT THE METHOD, and that correction is what made it usable.</b> Cut
    /// per method, the first run reported **58** findings and most were **public helpers inside wired
    /// subsystems** — `SeatAllocation.DHondtDivisor` is exposed so a harness can test the divisor directly
    /// while the game calls the outer allocator. That is a legitimate, common pattern, not a defect, and a
    /// check reporting it is one somebody turns off in a week.</para>
    ///
    /// <para>⚠ <b>UNWIRED IS NOT DEAD, and the distinction is the whole point.</b> A dead method has no
    /// callers and should be deleted. An unwired subsystem has a harness proving it works and a game that
    /// never asks — usually a *plan that stalled*. This project builds that way deliberately (R-N2 built
    /// the entire elections model before wiring any of it), so each finding is a **GAP with a question
    /// attached**: is it waiting for its item, or did its item land without it?</para>
    ///
    /// <para>⚠ <b>What it cannot see, stated rather than discovered later.</b> Reflection, Unity
    /// serialization, `UnityEvent` wiring in a scene asset, and any call built from a string. Names inside
    /// string literals ARE counted, precisely so a reflected call still registers.</para>
    ///
    /// <para>⚠ <b>A ratchet, not a verdict</b> — the shape sweeps (b) and (d) use. What fails is GROWTH
    /// above a recorded ceiling; the ceiling may be lowered and never raised.</para>
    /// </summary>
    public static class UnwiredSubsystemCheck
    {
        /// <summary>⚠ The ceiling, measured when this check was built on 2026-09-01. Lower it when a
        /// subsystem is wired or deleted; never raise it.</summary>
        private const int UnwiredCeiling = 7;

        /// <summary>
        /// ⚠ **THE SECOND CEILING — this check's OWN blind spot, measured 2026-09-01.**
        ///
        /// <para>The scan below only ever considers a file that declares a `public static` method. A
        /// subsystem built out of instance types declares none, so it could never be reported however
        /// unreachable it was. **Two real ones were sitting in that hole**: `ElectoralCollege` (the US
        /// elector allocation, implemented from the statutes) and `RegionalVoteModel` (the per-Land vote,
        /// built because a national model over-predicted the CSU by 7.4 pp). Both headers say *"PURE
        /// FUNCTIONS, WIRED TO NOTHING (R-N2)"* — ⚠ **and R-N2 was RETIRED at W-G1**, so the licence that
        /// authorised building ahead of wiring is gone and the backlog it left was never re-homed.</para>
        ///
        /// <para>Lower it when a subsystem is wired or deleted; never raise it.</para>
        /// </summary>
        private const int UnreachableCeiling = 5;

        /// <summary>A `public static` method declaration. Instance methods are excluded: they need an
        /// object, and tracing who constructs it is beyond what a name scan can honestly claim.</summary>
        private static readonly Regex PublicStatic = new Regex(
            @"^\s*public\s+static\s+(?:readonly\s+)?[A-Za-z_][A-Za-z0-9_<>,\.\[\]\?]*\s+([A-Z][A-Za-z0-9_]*)\s*\(");

        /// <summary>A public TYPE declaration — class, struct, interface or enum, with any modifiers
        /// between `public` and the keyword. ⚠ This is the axis the entry-point scan cannot see: a type
        /// nothing outside its own file ever NAMES can be neither constructed, inherited from nor called,
        /// and it does not need a single static method to be unreachable.</summary>
        private static readonly Regex PublicType = new Regex(
            @"^\s*public\s+(?:(?:sealed|abstract|static|partial|readonly|unsafe)\s+)*(?:class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)");

        /// <summary>⚠ Names too common to trace by occurrence. Each would produce noise rather than a
        /// finding, and a check with that much noise is one somebody turns off.</summary>
        private static readonly HashSet<string> TooCommon = new HashSet<string>(StringComparer.Ordinal)
        {
            "ToString", "Equals", "GetHashCode", "Clone", "Create", "Get", "Set", "Run", "Apply",
            "From", "For", "Of", "Parse", "TryParse", "Format", "Draw", "Build", "Load", "Save",
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string scripts = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts");
            string editor = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Editor");
            if (!Directory.Exists(scripts))
            {
                Debug.LogError("UNWIRED: no Assets/Scripts directory - reporting nothing rather than reporting clean.");
                CheckExit.Finish(1);
                return;
            }

            var contents = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories)) { contents[file] = File.ReadAllText(file); }
            if (Directory.Exists(editor))
            {
                foreach (string file in Directory.GetFiles(editor, "*.cs", SearchOption.AllDirectories)) { contents[file] = File.ReadAllText(file); }
            }

            var gameCalls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var declaringFiles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            int declarations = 0, skipped = 0;
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, string> file in contents)
            {
                if (!file.Key.StartsWith(scripts, StringComparison.OrdinalIgnoreCase)) { continue; }
                if (IsHarness(file.Key, scripts)) { continue; }

                string[] lines = file.Value.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    Match match = PublicStatic.Match(lines[i]);
                    if (!match.Success) { continue; }

                    string name = match.Groups[1].Value;
                    declarations++;
                    if (TooCommon.Contains(name)) { skipped++; continue; }
                    seen.Add(name);

                    if (!declaringFiles.TryGetValue(file.Key, out List<string> names))
                    {
                        names = new List<string>();
                        declaringFiles[file.Key] = names;
                    }

                    names.Add(name);

                    foreach (KeyValuePair<string, string> other in contents)
                    {
                        if (string.Equals(other.Key, file.Key, StringComparison.OrdinalIgnoreCase)) { continue; }
                        if (IsHarness(other.Key, scripts)) { continue; }
                        if (CountWord(other.Value, name) == 0) { continue; }

                        gameCalls.Add(file.Key);
                        break;
                    }
                }
            }

            var unwired = new List<string>();
            foreach (KeyValuePair<string, List<string>> file in declaringFiles)
            {
                if (gameCalls.Contains(file.Key)) { continue; }

                // ⚠ TWO CLASSES, and the column separates them because they are not the same finding.
                // A file whose TYPE NAME is never mentioned in game code is unwired entire - nothing
                // reaches any part of it (TacticalVoting, Rosatellum). A file whose type IS mentioned but
                // whose public statics are not called is a wired data type with an UNCALLED ENTRY POINT
                // (CampaignRun holds the campaign's types and nothing invokes Simulate). Reporting them
                // identically would have hidden the difference behind one number.
                string typeName = Path.GetFileNameWithoutExtension(file.Key);
                int typeMentions = 0;
                foreach (KeyValuePair<string, string> other in contents)
                {
                    if (string.Equals(other.Key, file.Key, StringComparison.OrdinalIgnoreCase)) { continue; }
                    if (IsHarness(other.Key, scripts)) { continue; }
                    if (CountWord(other.Value, typeName) > 0) { typeMentions++; }
                }

                unwired.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0,-30} {1} entry point(s) uncalled; type named in {2} game file(s) - {3}",
                    Path.GetFileName(file.Key), file.Value.Count, typeMentions,
                    typeMentions == 0 ? "UNWIRED ENTIRE" : "wired type, uncalled entry point"));
            }

            unwired.Sort(StringComparer.Ordinal);

            // ⚠ THE SECOND PASS — the class the first one cannot see. A file is UNREACHABLE when NOT ONE
            // of the public types it declares is named anywhere else in game code. That needs no static
            // method, so it catches the instance-built subsystem the entry-point scan is blind to.
            //
            // ⚠ JUDGED AT THE FILE, on the fifth sweep's own hard-won lesson. Cut per TYPE this reports
            // 36, and most are not findings - they are companion types consumed only by the file that
            // declares them (`Rosatellum.ListEntry`, `RegionalVoteModel.RegionInput`, `CampaignRun.Setup`).
            // A check reporting 36 of those is one somebody turns off in a week. If ANY type in the file
            // is named outside it, something reaches the file and the file is not the finding.
            var declaredTypes = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            int typeDeclarations = 0;
            foreach (KeyValuePair<string, string> file in contents)
            {
                if (!file.Key.StartsWith(scripts, StringComparison.OrdinalIgnoreCase)) { continue; }
                if (IsHarness(file.Key, scripts)) { continue; }

                foreach (string line in file.Value.Split('\n'))
                {
                    Match m = PublicType.Match(line);
                    if (!m.Success) { continue; }

                    typeDeclarations++;
                    if (!declaredTypes.TryGetValue(file.Key, out List<string> names))
                    {
                        names = new List<string>();
                        declaredTypes[file.Key] = names;
                    }

                    if (!names.Contains(m.Groups[1].Value)) { names.Add(m.Groups[1].Value); }
                }
            }

            var unreachable = new List<string>();
            foreach (KeyValuePair<string, List<string>> file in declaredTypes)
            {
                bool reached = false;
                foreach (string name in file.Value)
                {
                    foreach (KeyValuePair<string, string> other in contents)
                    {
                        if (string.Equals(other.Key, file.Key, StringComparison.OrdinalIgnoreCase)) { continue; }
                        if (IsHarness(other.Key, scripts)) { continue; }
                        if (CountWord(other.Value, name) == 0) { continue; }

                        reached = true;
                        break;
                    }

                    if (reached) { break; }
                }

                if (!reached)
                {
                    unreachable.Add(string.Format(CultureInfo.InvariantCulture,
                        "{0,-30} declares {1} public type(s), NONE named anywhere else in game code",
                        Path.GetFileName(file.Key), file.Value.Count));
                }
            }

            unreachable.Sort(StringComparer.Ordinal);

            var sb = new StringBuilder();
            sb.Append("=== The coherence audit (e): subsystems the game does not call ===\n");
            sb.Append(F("    THE ENUMERATION: every .cs file under Assets/Scripts declaring a `public static` method -\n"));
            sb.Append(F("    {0} declaration(s) across {1} file(s), {2} distinct name(s), {3} skipped as too common to trace by\n",
                declarations, declaringFiles.Count, seen.Count, skipped));
            sb.Append("    occurrence. A FILE is UNWIRED when NOT ONE of its public entry points is named outside\n");
            sb.Append("    Assets/Editor and Assets/Scripts/Testing.\n\n");
            sb.Append(F("    {0} of {1} file(s) UNWIRED (ceiling {2}).\n", unwired.Count, declaringFiles.Count, UnwiredCeiling));
            foreach (string line in unwired) { sb.Append("    GAP  ").Append(line).Append('\n'); }

            sb.Append("\n    THE SECOND CLASS: UNREACHABLE FILES (added 2026-09-01, this check's own blind spot)\n");
            sb.Append("    ---------------------------------------------------------------------------------\n");
            sb.Append(F("    THE ENUMERATION: {0} public type declaration(s) across {1} game file(s). A FILE is UNREACHABLE\n",
                typeDeclarations, declaredTypes.Count));
            sb.Append("    when NOT ONE of the public types it declares is named anywhere else in game code - so nothing\n");
            sb.Append("    can construct it, inherit from it or call it, WITH OR WITHOUT a public static method.\n");
            sb.Append(F("    {0} of {1} file(s) UNREACHABLE (ceiling {2}).\n", unreachable.Count, declaredTypes.Count, UnreachableCeiling));
            foreach (string line in unreachable) { sb.Append("    GAP  ").Append(line).Append('\n'); }
            sb.Append("    ⚠ Judged at the FILE, not the type: cut per TYPE this reports 36 and most are companion types\n");
            sb.Append("    used only inside their own file. The fifth sweep learned that at 58 findings; this is the same\n");
            sb.Append("    lesson applied before the mistake rather than after.\n");

            sb.Append("\n    ⚠ UNWIRED IS NOT DEAD. A dead method has no callers and should be deleted; an unwired subsystem\n");
            sb.Append("    has a harness proving it works and a game that never asks - usually a plan that stalled. R-N2 built\n");
            sb.Append("    the whole elections model this way on purpose, so each GAP is a QUESTION: is it waiting for its\n");
            sb.Append("    item, or did its item land without it? ⚠ Reflection and scene-asset wiring are invisible here.\n");

            if (unreachable.Count > UnreachableCeiling)
            {
                Debug.LogError($"UNREACHABLE: {unreachable.Count} file(s) whose public types are named nowhere else in game "
                               + $"code, above the recorded ceiling of {UnreachableCeiling}. ⚠ This is the class the ENTRY-POINT "
                               + "scan above cannot see - it only ever looks at files declaring a `public static` method, so an "
                               + "instance-built subsystem was invisible to it however unreachable it was. Wire it, delete it, "
                               + "or record why it waits - and LOWER the ceiling, never raise it.");
                sb.Append("    ⚠ ABOVE THE UNREACHABLE CEILING - see the error above.\n");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            if (unwired.Count > UnwiredCeiling)
            {
                Debug.LogError($"UNWIRED: {unwired.Count} subsystem(s) whose public entry points only harnesses call, above the "
                               + $"recorded ceiling of {UnwiredCeiling}. ⚠ This is the class `DeadStateCheck` cannot see - it "
                               + "scans PRIVATE declarations, and `TacticalVoting` hid behind that for as long as it existed. "
                               + "Wire it, delete it, or record why it waits - and LOWER the ceiling, never raise it.");
                sb.Append("    ⚠ ABOVE THE CEILING - see the error above.\n");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            sb.Append(unwired.Count == 0
                ? "    CLEAN - every subsystem the game declares, the game also calls.\n"
                : "    At or under the ceiling: the backlog is reported and may only shrink.\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>A harness, diagnostic or capture driver rather than a gameplay path.
        /// `Assets/Scripts/Testing` counts: the capture driver lives there and is not the game either.</summary>
        private static bool IsHarness(string path, string scriptsRoot)
        {
            if (!path.StartsWith(scriptsRoot, StringComparison.OrdinalIgnoreCase)) { return true; }
            return path.IndexOf(Path.DirectorySeparatorChar + "Testing" + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int CountWord(string text, string word)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(word, index, StringComparison.Ordinal)) >= 0)
            {
                bool leftOk = index == 0 || !IsWordChar(text[index - 1]);
                int after = index + word.Length;
                bool rightOk = after >= text.Length || !IsWordChar(text[after]);
                if (leftOk && rightOk) { count++; }
                index = after;
            }

            return count;
        }

        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
