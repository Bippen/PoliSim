using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The coherence audit, sweep (a) — **a comment that names a piece of code must name one that
    /// exists.** `PhantomGuardCheck` widened past guard names to any claim about a named thing.
    ///
    /// <para><b>Why widening was the right move.</b> The guard-name check found a real phantom on its
    /// first run (`SaveLoadDiagnostic`, a type that does not exist). But guards are a small share of what
    /// this codebase's comments assert: they cite formulas, fields, call sites and methods constantly,
    /// and **every one of those citations is a claim that can go stale in exactly the same way** — a
    /// renamed method, a deleted field, a moved call site. The comment still reads as evidence.</para>
    ///
    /// <para><b>THE ENUMERATION, and why it is BACKTICKS.</b> Every `.cs` file under `Assets/`; inside
    /// comment text only; every reference written as <c>`Type.Member`</c> — **backticked, which this
    /// codebase uses consistently for code references and for nothing else.** The type must exist and it
    /// must really have that member.</para>
    ///
    /// <para>⚠ <b>Backticks rather than a bare dotted name is the whole reason this is usable.</b> A bare
    /// dotted-name regex matches prose, file paths, version numbers and sentence ends, and a check with
    /// that much noise is a check somebody turns off in a week. The backtick convention is already
    /// enforced on the OTHER side by `MetaTextCheck`, which now bans backticks in player-facing strings —
    /// so the two guards agree that a backtick means "this is code".</para>
    ///
    /// <para>⚠ <b>What it deliberately does not do.</b> It does not check that the cited member does what
    /// the sentence says — no regex can. It checks the one machine-checkable half: **the thing named is
    /// there.** Past-tense sentences ("…until C-C14 deleted that field") are recorded as HISTORY and not
    /// failed, because this project keeps its history on purpose.</para>
    /// </summary>
    public static class CommentClaimCheck
    {
        /// <summary>A backticked `Type.Member` or `Type.Member.Deeper`. Anchored on a capitalised type
        /// name, because a lowercase leading segment is a variable or a path, not a type.</summary>
        private static readonly Regex Claim = new Regex(@"`([A-Z][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)`");

        private static readonly string[] PastTenseMarkers =
        {
            "deleted", "removed", "retired", "used to", "no longer", "did not exist", "does not exist",
            "was renamed", "replaced by", "superseded", "gone", "until c-", "before c-",
            // ⚠ "rejected" earns its place: `PoliSimWidgets.StandingDraftPair` is cited by a comment that
            // says the widget WAS REJECTED (COMPLETED.md §29) while keeping the one idea taken from it.
            // That is history being kept on purpose, which is the thing this check must not punish.
            "rejected", "was not built", "never built",

            // ⚠ ADDED 2026-09-01, found by PreWiringPremiseCheck borrowing this list. The list had
            // "replaced by" and `ELECTIONS_GAP_TABLE.md` says "53 real parties REPLACED THEM" - the same
            // history in the active voice, missed by one preposition. **A named-set rule fails at the
            // edges of its own list**, which is stated on both consumers; this is one such edge, found by
            // a second consumer reading the same lines for a different reason.
            // R-T3 - every consumer of this list: CommentClaimCheck (its own claims) and
            // PreWiringPremiseCheck (C-0.2's premises). Both benefit; neither is re-stated.
            "replaced them", "replaced with", "no longer exists", "since deleted", "discharged",
        };

        /// <summary>⚠ Types whose members this check cannot see and must not fail on, each with its
        /// reason. Enumerated so the blind spots are visible rather than implicit.</summary>
        private static readonly Dictionary<string, string> Unverifiable = new Dictionary<string, string>
        {
            { "CLAUDE", "a document, not a type - `CLAUDE.md` is cited constantly and correctly" },
            { "COMPLETED", "a document" },
            { "Assets", "a path" },
            { "Scripts", "a path" },
            { "Editor", "a path" },
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            // ⚠ ALL types with a simple name, not the last one seen. The first run reported
            // `Country.State` MISSING because a different assembly also defines a `Country`, and a
            // dictionary keyed on the simple name kept whichever loaded last. A guard that fails on the
            // wrong homonym is a guard nobody keeps.
            var types = new Dictionary<string, List<Type>>(StringComparer.Ordinal);
            var typeNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] found;
                try { found = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { found = e.Types; }

                foreach (Type type in found)
                {
                    if (type?.Name == null) { continue; }
                    typeNames.Add(type.Name);
                    if (!types.TryGetValue(type.Name, out List<Type> bucket)) { bucket = new List<Type>(); types[type.Name] = bucket; }
                    bucket.Add(type);
                }
            }

            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
            string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);

            int claims = 0, resolved = 0, history = 0, unknownType = 0;
            var missingMember = new List<string>();
            var unverifiable = new List<string>();

            foreach (string file in files)
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    int comment = lines[i].IndexOf("//", StringComparison.Ordinal);
                    if (comment < 0) { continue; }

                    string text = lines[i].Substring(comment);
                    foreach (Match match in Claim.Matches(text))
                    {
                        string typeName = match.Groups[1].Value;
                        string member = match.Groups[2].Value;
                        claims++;

                        if (Unverifiable.ContainsKey(typeName)) { unverifiable.Add($"{typeName}.{member}"); continue; }

                        // ⚠ A FILE, NOT A MEMBER. `EconomyState.cs` is a filename and this codebase cites
                        // files in backticks too. Excluded by extension rather than guessed at.
                        if (IsFileName(member)) { unverifiable.Add($"{typeName}.{member}"); continue; }

                        // ⚠ A NAMESPACE-QUALIFIED TYPE, NOT A MEMBER. `System.Random` names a type inside a
                        // namespace; reading `Random` as a member of a type called `System` produced 12 of
                        // the first run's 49 false positives.
                        if (typeNames.Contains(member)) { resolved++; continue; }

                        if (!types.TryGetValue(typeName, out List<Type> candidates)) { unknownType++; continue; }

                        bool found = false;
                        foreach (Type candidate in candidates) { found |= HasMember(candidate, member); }
                        if (found) { resolved++; continue; }
                        // ⚠ A DOC COMMENT IS A PARAGRAPH, NOT A LINE. The first pass looked for a past-tense
                        // marker on the citing line alone and failed `PoliSimWidgets.StandingDraftPair`,
                        // whose sentence says the widget "was rejected" two lines further down. A guard
                        // that cannot read a sentence to its end reports history as a defect.
                        if (LooksHistorical(Context(lines, i))) { history++; continue; }

                        missingMember.Add($"{Relative(file, root)}:{i + 1}  `{typeName}.{member}`");
                    }
                }
            }

            var sb = new StringBuilder();
            sb.Append("=== The coherence audit (a): comment-to-code claims ===\n");
            sb.Append(F("    THE ENUMERATION: {0} .cs files under Assets/, comment text only, every BACKTICKED `Type.Member`\n", files.Length));
            sb.Append(F("    reference - {0} claim(s) found.\n", claims));
            sb.Append(F("    {0} resolved · {1} history (a sentence marking the name as past) · {2} on a type this check cannot\n", resolved, history, unknownType));
            sb.Append(F("    see (an external or generic type) · {0} unverifiable by name (documents and paths) · {1} MISSING.\n",
                unverifiable.Count, missingMember.Count));

            foreach (string line in missingMember)
            {
                Debug.LogError($"CLAIM: {line} - a comment cites this member and the type does not have it. A comment naming a "
                               + "member is a claim about the code; either correct the name, or say plainly that it was removed.");
                sb.Append("    ⚠ MISSING ").Append(line).Append('\n');
            }

            if (missingMember.Count == 0)
            {
                sb.Append("    CLEAN - every backticked member reference a comment makes resolves.\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
                return;
            }

            Debug.LogError(sb.ToString());
            CheckExit.Finish(1);
        }

        /// <summary>Members of the type OR of any type it nests, since this codebase cites nested types'
        /// members through their outer name.</summary>
        private static bool HasMember(Type type, string member)
        {
            const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                                     | BindingFlags.Static | BindingFlags.FlattenHierarchy;

            if (type.GetMember(member, All).Length > 0) { return true; }
            if (type.GetNestedType(member, All) != null) { return true; }

            foreach (Type nested in type.GetNestedTypes(All))
            {
                if (nested.GetMember(member, All).Length > 0) { return true; }
            }

            return false;
        }

        /// <summary>The citing line plus a few either side - enough to see a sentence that runs across the
        /// line break, which in this codebase's comment style is most of them.</summary>
        private static string Context(string[] lines, int index)
        {
            var sb = new StringBuilder();
            for (int i = Mathf.Max(0, index - 3); i < Mathf.Min(lines.Length, index + 4); i++)
            {
                sb.Append(lines[i]).Append(' ');
            }

            return sb.ToString();
        }

        private static bool IsFileName(string member)
        {
            foreach (string ext in new[] { "cs", "md", "png", "json", "csv", "svc", "xlsx", "html", "txt", "asset", "meta" })
            {
                if (string.Equals(member, ext, StringComparison.OrdinalIgnoreCase)) { return true; }
            }

            return false;
        }

        /// <summary>Whether a line RECORDS that something was once so, rather than asserting it now.
        /// ⚠ **Exposed 2026-09-01 so <see cref="PreWiringPremiseCheck"/> can BORROW this judgement instead
        /// of restating it.** The same rule written twice is two things to keep true, and a second copy
        /// would drift the first time somebody added a marker to one list and not the other — which is the
        /// defect this repo has catalogued more often than any other. **One list, one test, two callers.**</summary>
        public static bool ReadsAsHistory(string text) => LooksHistorical(text);

        private static bool LooksHistorical(string text)
        {
            foreach (string marker in PastTenseMarkers)
            {
                if (text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0) { return true; }
            }

            return false;
        }

        private static string Relative(string path, string root) =>
            path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? path.Substring(root.Length + 1) : path;

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
