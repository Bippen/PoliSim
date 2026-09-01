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
    /// The coherence audit's **NINTH sweep — a guard a COMMENT can switch off.**
    ///
    /// <para><b>The instance, and it was four instances.</b> A generated file's header comment named two
    /// subsystems while explaining why the file existed, and `UnwiredSubsystemCheck` stopped reporting
    /// both as unreachable. The sweep that followed found every name-scanning check read raw text:
    /// `PlayerReachabilityCheck` (⚠ a comment in `GameController` naming a takeover would have made it
    /// "reachable" — the very thing its own ratchet doc warns against), `EvidenceDiscriminationCheck`
    /// (⚠ **a commented-out `Debug.LogError` counted as a failure path**, defeating the sixth sweep with
    /// a comment) and `DocumentClaimCheck`.</para>
    ///
    /// <para><b>What this check is, and why it is different from the eight before it.</b> It is a
    /// **MUTATION PROBE, run every time** — the thing the sixth sweep billed and could not build.
    /// It does not read the repo and report a backlog. It hands <see cref="SourceText.WithoutComments"/>
    /// inputs whose right answer is known, and requires the answer. **The subject is the mechanism, not
    /// the codebase**, which is why it can be exhaustive where a scan can only be a ratchet.</para>
    ///
    /// <para>⚠ <b>It also names WHO must use it</b>, and that half is a census rather than a proof: the
    /// checks that count names in source are listed, and each is required to route through the shared
    /// stripper. A check added later that reads raw text is invisible here until somebody adds it to the
    /// list — **which is the same hole `RatchetSlackCheck` has, and it is named for the same reason.**</para>
    /// </summary>
    public static class CommentImmunityCheck
    {
        /// <summary>The checks that count a NAME in source text, and must therefore strip comments first.
        /// ⚠ `CommentClaimCheck` and `PhantomGuardCheck` are deliberately absent: their subject IS the
        /// comment, and stripping it would leave them reading nothing.</summary>
        private static readonly string[] MustStrip =
        {
            "UnwiredSubsystemCheck", "PlayerReachabilityCheck", "EvidenceDiscriminationCheck", "DocumentClaimCheck",
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var failures = new List<string>();
            var sb = new StringBuilder();
            sb.Append("=== The coherence audit (i): a guard a comment can switch off ===\n");

            // --- The mutation probe. Each case's right answer is known, and both directions are here:
            // a comment must NOT survive, and a string literal MUST.
            int cases = 0;
            Case(sb, failures, ref cases, "a line comment naming a type",
                "class A { }\n// ZzzSubject is mentioned here\n", "ZzzSubject", false);
            Case(sb, failures, ref cases, "a doc comment naming a type",
                "/// <summary>ZzzSubject explains itself</summary>\nclass A { }\n", "ZzzSubject", false);
            Case(sb, failures, ref cases, "a block comment naming a type",
                "class A { /* ZzzSubject */ }\n", "ZzzSubject", false);
            Case(sb, failures, ref cases, "a block comment spanning lines",
                "class A {\n/*\n ZzzSubject\n*/\n}\n", "ZzzSubject", false);
            Case(sb, failures, ref cases, "⚠ a STRING LITERAL naming a type (a reflected call is built from one)",
                "class A { string s = \"ZzzSubject\"; }\n", "ZzzSubject", true);
            Case(sb, failures, ref cases, "real code naming a type",
                "class A { ZzzSubject x; }\n", "ZzzSubject", true);
            Case(sb, failures, ref cases, "⚠ a URL inside a literal is not eaten by the // rule",
                "class A { string s = \"https://example.test/ZzzSubject\"; }\n", "ZzzSubject", true);
            Case(sb, failures, ref cases, "a commented-out failure path (the sixth sweep's own exposure)",
                "class A { void R() { /* Debug.LogError(\"x\"); */ } }\n", "Debug.LogError", false);

            sb.Append(F("\n    {0} mutation case(s), each with a known right answer, both directions covered.\n", cases));

            // The enumeration rule: a run with no cases proves nothing and would print like a clean one.
            if (cases == 0)
            {
                Debug.LogError("IMMUNITY: no mutation cases ran, so this check verified NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            // --- The census: who must route through the shared stripper.
            string editor = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Editor");
            sb.Append("\n    THE CENSUS - checks that count a name in source and must strip comments first:\n");
            foreach (string name in MustStrip)
            {
                string path = Path.Combine(editor, name + ".cs");
                if (!File.Exists(path))
                {
                    failures.Add(name + " is named here and has no file");
                    sb.Append("    ⚠ MISSING  ").Append(name).Append('\n');
                    continue;
                }

                bool routes = Regex.IsMatch(File.ReadAllText(path), @"SourceText\.WithoutComments|StripComments");
                if (!routes) { failures.Add(name + " does not route through the shared stripper"); }
                sb.Append(routes ? "    ok       " : "    ⚠ RAW     ").Append(name).Append('\n');
            }

            sb.Append("\n    ⚠ WHAT THIS CANNOT SEE: a check added later that reads raw text is invisible until somebody\n");
            sb.Append("    adds it to the list above. That is the same hole RatchetSlackCheck has, named for the same\n");
            sb.Append("    reason - a census is honest about its coverage or it is worse than none.\n");
            sb.Append("    ⚠ AND THE STRIPPER IS AN APPROXIMATION: a real line comment following a string literal on the\n");
            sb.Append("    same line survives. That residue can only cause the SAME class of miss, smaller, and closing it\n");
            sb.Append("    needs a C# lexer rather than a regex.\n");

            if (failures.Count > 0)
            {
                Debug.LogError("IMMUNITY: " + failures.Count + " failure(s) - " + string.Join(" | ", failures.ToArray())
                               + ". ⚠ A guard a COMMENT can switch off has stopped discriminating: a prose mention is not "
                               + "a reference, a commented-out assertion is not an assertion, and a name in a doc comment "
                               + "is not a route.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static void Case(StringBuilder sb, List<string> failures, ref int cases,
            string description, string source, string needle, bool shouldSurvive)
        {
            cases++;
            string stripped = SourceText.WithoutComments(source);
            bool survived = Regex.IsMatch(stripped, @"\b" + Regex.Escape(needle) + @"\b");

            if (survived == shouldSurvive)
            {
                sb.Append("    ok       ").Append(description).Append(shouldSurvive ? "  (kept)" : "  (stripped)").Append('\n');
                return;
            }

            failures.Add(description + (shouldSurvive ? " was stripped and must survive" : " survived and must be stripped"));
            sb.Append("    ⚠ WRONG  ").Append(description).Append('\n');
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
