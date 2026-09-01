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
    /// The coherence audit's **SEVENTH sweep — a written claim about the code, checked against the code.**
    ///
    /// <para><b>The instance that opened it, and the correction that followed.</b> A shelf pass reported
    /// `ELECTIONS_PLAY_CALIBRATION.md`'s entry 5 — `CampaignCalendar.DefaultPreCampaignWeeks` — as naming
    /// a type that does not exist, and rewrote it. ⚠ **The entry was right and the pass was wrong**:
    /// `CampaignCalendar` is a real `public readonly struct` that happens to live in the FILE
    /// `CampaignClock.cs`. The instrument at fault matched TYPE names against FILE names, so a type
    /// sharing a file with another type read as absent.</para>
    ///
    /// <para>⚠ **This check caught that within the hour of being written**, which is a better argument for
    /// it than its own description: a pass hunting false claims in documents made one, and the guard built
    /// in the same session found it. The list's premise — *"each entry is a one-line change with a named
    /// owner in the code"* — is exactly why: **an owner that does not resolve makes an entry unusable at
    /// the moment somebody acts on it, and so does an owner "corrected" to the wrong thing.**</para>
    ///
    /// <para>⚠ <b>Neither `PhantomGuardCheck` nor `CommentClaimCheck` could have seen it: both scan CODE
    /// COMMENTS.</b> Nothing checked a markdown claim — and this project's documents make far more claims
    /// about the code than its comments do. This is `PhantomGuardCheck`'s sibling and the sixth sweep's
    /// cousin: **the documentation half of the project's dominant failure mode.**</para>
    ///
    /// <para><b>TWO CLAUSES, AND BOTH ARE NARROW ON PURPOSE.</b> A naive scan of every backticked
    /// `Type.Member` in the root documents yields ~580 candidates, most of them BCL and Unity names this
    /// repo has no standing to judge. The fifth sweep learned at 58 findings what a noisy check costs;
    /// these two clauses are cut where they produce findings instead:</para>
    /// <list type="number">
    /// <item><b>MEMBER GONE.</b> The type is declared **exactly once** under `Assets/`, and the member
    /// name does not appear in that declaring file. The document is describing a member that moved or
    /// went.</item>
    /// <item><b>WRONG OWNER.</b> The type is neither declared NOR USED anywhere under `Assets/`, and the
    /// member IS declared there exactly once, on some other type. ⚠ **The USE test is what the first run
    /// forced**: it reported `Mathf.Max`, `GUILayout.Height` and `Resources.Load` as wrong owners, because
    /// those Unity types' member names collide with ours. A name our own code uses everywhere is a name
    /// this repo has no standing to call wrong — and a use test says that without a hand-written
    /// exclusion list, which would have been an authored judgement about which names are foreign.</item>
    /// </list>
    ///
    /// <para>⚠ A type declared MORE than once is skipped and counted: the reference is ambiguous, and
    /// guessing which file was meant would be the check inventing the claim it is supposed to verify.</para>
    ///
    /// <para><b>Ratcheted on the measured backlog, failing only on growth</b> — the documents were not
    /// written under this rule, and a check that goes red on its first run is a check somebody turns off.</para>
    /// </summary>
    public static class DocumentClaimCheck
    {
        /// <summary>⚠ MEMBER-GONE, measured on the first run. Lower it as documents are corrected; never raise it.</summary>
        private const int MemberGoneCeiling = 2;

        /// <summary>⚠ WRONG-OWNER, measured on the first run. Lower it as documents are corrected; never raise it.</summary>
        private const int WrongOwnerCeiling = 0;

        /// <summary>A backticked `Type.Member` in prose. File names are excluded by extension below.</summary>
        private static readonly Regex Reference = new Regex(@"`([A-Z][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)`");

        /// <summary>A type declaration, with any modifiers between the accessibility and the keyword.</summary>
        private static readonly Regex TypeDeclaration = new Regex(
            @"^\s*(?:public|internal|private|protected)\s+(?:(?:sealed|abstract|static|partial|readonly|unsafe|new)\s+)*(?:class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Multiline);

        /// <summary>A member declaration — a field, constant, property or method, named after its type.</summary>
        private static readonly Regex MemberDeclaration = new Regex(
            @"^\s*(?:public|internal|private|protected)\s+(?:(?:static|const|readonly|virtual|override|abstract|sealed|async|extern|new|partial|unsafe)\s+)*[A-Za-z_][A-Za-z0-9_<>,\.\[\]\?]*\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:[;=({]|=>)",
            RegexOptions.Multiline);

        /// <summary>Any bare identifier in a source file — the USE index that keeps Unity and BCL type
        /// names out of the wrong-owner clause without a hand-written exclusion list.</summary>
        private static readonly Regex Identifier = new Regex(@"\b[A-Z][A-Za-z0-9_]*\b");

        /// <summary>⚠ The HISTORICAL records. They exist to say what WAS done, so naming a member that has
        /// since been deleted is them working correctly. This check binds on the LIVE documents.</summary>
        private static readonly HashSet<string> Historical = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "COMPLETED.md", "CLAUDE.md", "ELECTIONS_PROTOTYPE_LOG.md",
        };

        private static readonly HashSet<string> FileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cs", "md", "txt", "json", "csv", "png", "svg", "html", "ps1", "xml", "asset", "unity",
            "prefab", "yml", "yaml", "cfg", "slnx", "jpg", "jpeg", "pdf", "log", "meta", "zip", "ttf", "otf",
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string root = Directory.GetCurrentDirectory();
            string assets = Path.Combine(root, "Assets");
            if (!Directory.Exists(assets))
            {
                Debug.LogError("DOCCLAIM: no Assets directory, so no claim can be resolved and this run verified NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            // Index every type and every member declared under Assets, by name, to the files declaring
            // them. ⚠ The COUNT is what makes the two clauses safe: a name declared more than once is
            // ambiguous and is skipped rather than guessed.
            var typeFiles = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var memberFiles = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var fileText = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var usedNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (string path in Directory.GetFiles(assets, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(path);
                fileText[path] = text;
                foreach (Match m in TypeDeclaration.Matches(text)) { Add(typeFiles, m.Groups[1].Value, path); }
                foreach (Match m in MemberDeclaration.Matches(text)) { Add(memberFiles, m.Groups[1].Value, path); }
                foreach (Match m in Identifier.Matches(text)) { usedNames.Add(m.Value); }
            }

            var docs = new List<string>();
            foreach (string path in Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly))
            {
                // ⚠ THE HISTORICAL RECORDS ARE EXCLUDED, AND THIS IS A RULING RATHER THAN A CONVENIENCE.
                // `COMPLETED.md`, `CLAUDE.md` and `ELECTIONS_PROTOTYPE_LOG.md` exist to say what WAS done;
                // naming a member that has since been deleted is them working correctly, not failing.
                // The first run reported `GraphRenderer.DrawPublished` against three documents - and that
                // member was deliberately deleted at RIDE-1, with the deletion recorded in two of them.
                // A check that made history wrong for describing history would be turned off in a week.
                // **This check binds on the LIVE documents: the ones that tell you what to do now.**
                if (Historical.Contains(Path.GetFileName(path))) { continue; }
                docs.Add(path);
            }

            docs.Sort(StringComparer.Ordinal);

            var memberGone = new List<string>();
            var wrongOwner = new List<string>();
            int candidates = 0, ambiguous = 0, foreign = 0;

            foreach (string doc in docs)
            {
                string text = File.ReadAllText(doc);
                string name = Path.GetFileName(doc);
                var seen = new HashSet<string>(StringComparer.Ordinal);

                foreach (Match m in Reference.Matches(text))
                {
                    string type = m.Groups[1].Value;
                    string member = m.Groups[2].Value;
                    if (FileExtensions.Contains(member)) { continue; }
                    if (!seen.Add(type + "." + member)) { continue; }

                    candidates++;

                    if (typeFiles.TryGetValue(type, out List<string> declaring))
                    {
                        if (declaring.Count > 1) { ambiguous++; continue; }
                        if (!Regex.IsMatch(fileText[declaring[0]], @"\b" + Regex.Escape(member) + @"\b"))
                        {
                            memberGone.Add(F("{0}: `{1}.{2}` - the type is declared in {3} and the member is not in it",
                                name, type, member, Path.GetFileName(declaring[0])));
                        }

                        continue;
                    }

                    // The type is not DECLARED by us. ⚠ Before calling that a wrong owner, ask whether the
                    // name is one our code USES: `Mathf`, `GUILayout`, `Resources` are Unity types whose
                    // member names (`Max`, `Height`, `Load`) collide with ours, and the first run reported
                    // all three as wrong owners. A name our code uses everywhere is a name this repo has
                    // no standing to call wrong. ⚠ This is a USE test, not a hand-written exclusion list:
                    // a list would be an authored judgement about which names are foreign.
                    if (usedNames.Contains(type)) { foreign++; continue; }

                    // What is left IS decidable, and it is the bug this sweep was built for: the member
                    // is ours, declared exactly once, and the type the document names exists nowhere at
                    // all - not declared, not used.
                    if (memberFiles.TryGetValue(member, out List<string> owners) && owners.Count == 1)
                    {
                        wrongOwner.Add(F("{0}: `{1}.{2}` - no type `{1}` exists; `{2}` is declared in {3}",
                            name, type, member, Path.GetFileName(owners[0])));
                        continue;
                    }

                    foreign++;
                }
            }

            memberGone.Sort(StringComparer.Ordinal);
            wrongOwner.Sort(StringComparer.Ordinal);

            // The enumeration rule: a run that read no document, or resolved no candidate, has checked
            // nothing and would print exactly like a run in which every claim was true.
            if (docs.Count == 0 || candidates == 0)
            {
                Debug.LogError($"DOCCLAIM: {docs.Count} document(s) and {candidates} candidate reference(s) - this run "
                               + "verified NOTHING rather than finding nothing wrong.");
                CheckExit.Finish(1);
                return;
            }

            var sb = new StringBuilder();
            sb.Append("=== The coherence audit (g): a written claim about the code, checked against the code ===\n");
            sb.Append(F("    THE ENUMERATION: {0} root document(s); {1} distinct `Type.Member` reference(s) after file names\n"
                        + "    are excluded; {2} skipped as AMBIGUOUS (the type is declared more than once under Assets, and\n"
                        + "    guessing which file was meant would be the check inventing the claim it verifies); {3} skipped as\n"
                        + "    FOREIGN (neither the type nor the member is ours - a BCL or Unity name this repo cannot judge).\n",
                docs.Count, candidates, ambiguous, foreign));
            sb.Append(F("\n    MEMBER GONE: {0} (ceiling {1}) - the type is ours and the member is not in it.\n",
                memberGone.Count, MemberGoneCeiling));
            RatchetLedger.Report("DocumentClaimCheck.MEMBER_GONE", memberGone.Count, MemberGoneCeiling);
            foreach (string g in memberGone) { sb.Append("    GAP  ").Append(g).Append('\n'); }
            sb.Append(F("\n    WRONG OWNER: {0} (ceiling {1}) - no such type, and the member lives on another one.\n",
                wrongOwner.Count, WrongOwnerCeiling));
            foreach (string g in wrongOwner) { sb.Append("    GAP  ").Append(g).Append('\n'); }
            RatchetLedger.Report("DocumentClaimCheck.WRONG_OWNER", wrongOwner.Count, WrongOwnerCeiling);
            sb.Append("\n    ⚠ WHAT THIS CANNOT SEE: a PROSE claim about behaviour - which is the larger half of what a\n");
            sb.Append("    document asserts, and S-22's standing finding. It reads identifiers, not sentences.\n");

            int over = 0;
            if (memberGone.Count > MemberGoneCeiling)
            {
                over++;
                Debug.LogError($"DOCCLAIM: {memberGone.Count} document claim(s) name a member their own type does not have, "
                               + $"above the ceiling of {MemberGoneCeiling}. Correct the document or the code - and lower the "
                               + "ceiling, never raise it.");
            }

            if (wrongOwner.Count > WrongOwnerCeiling)
            {
                over++;
                Debug.LogError($"DOCCLAIM: {wrongOwner.Count} document claim(s) name a type that does not exist while the "
                               + $"member they name does, above the ceiling of {WrongOwnerCeiling}. That is entry 5's bug: an "
                               + "owner that does not exist makes the entry unusable at the moment somebody acts on it.");
            }

            if (over > 0)
            {
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static void Add(Dictionary<string, List<string>> index, string key, string path)
        {
            if (!index.TryGetValue(key, out List<string> list)) { list = new List<string>(); index[key] = list; }
            if (!list.Contains(path)) { list.Add(path); }
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
