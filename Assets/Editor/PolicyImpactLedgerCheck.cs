using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-B5 (2026-09-05): every PolicyDecision field belongs to exactly one PolicyImpactLedger family. The ledger
    /// asserts this itself (PolicyImpactLedger.AssertPartitionCovers) - but only when a ledger is BUILT, which is at
    /// play time, so a field added to PolicyDecision without a family passed both bars and threw on the first frame
    /// of the next film (P5-B2 shipped SpendingNominalTargets and SpendingPinChanges that way; the B5 film found it).
    /// This runs the same assertion on the cheap bar, so the throw lands here.
    /// <para>K51 (Elias's ruling of 2026-10-23: "each kind of bill is its own ledger family"): the assertion also holds every bill kind to a family of its own,
    /// and this check reads the manager's source - every player's answer that lays a bill (its words name a bill: "a ... bill", or the alternative budget)
    /// must carry its kind (`bill: Congress.Kind.`), or the ledger reads it as an answer of no family and its effect lands on the interaction line again. The
    /// docket's answers sign or veto a bill the chamber already passed and lay none (DECLARED). A planted source proves the reader has teeth. The review's L5: a
    /// label chosen by a condition is read on every literal in it, and a label with no literal at all fails the check - so a future bill verb cannot escape it by
    /// how its label is written. The debt limit's raise, voted as a budget bill, stays an answer of no family (DECLARED, put to Elias beside K51).</para>
    /// </summary>
    public static class PolicyImpactLedgerCheck
    {
        private const string Simulation = "Assets/Scripts/Simulation";
        private static readonly Regex Literal = new Regex("\"([^\"]*)\"");

        public static void Run()
        {
            CheckExit.ArmLogFold();
            try
            {
                // Reached by reflection: the method stays private so UnwiredSubsystemCheck does not count a public entry point only a harness calls.
                MethodInfo assert = typeof(PolicyImpactLedger).GetMethod("AssertPartitionCovers", BindingFlags.NonPublic | BindingFlags.Static);
                if (assert == null) { throw new InvalidOperationException("PolicyImpactLedger.AssertPartitionCovers was not found - the ledger's completeness check moved or was renamed."); }
                try { assert.Invoke(null, null); }
                catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException inner) { throw inner; }

                // K51: the planted source first - a bill's answer without its kind is caught, with its kind passes, the docket's answer is no bill; the
                // review's L5: a label chosen by a condition is read on every one of its words, and a label the reader cannot read (no words in it) fails
                const string planted = "x = new PlayerAnswer(\"a planted bill\", id, sim => sim.Go(a, (b)));\n"
                                     + "y = new PlayerAnswer(\"a planted kind bill\", id, sim => true, answersAWait: false, bill: Congress.Kind.Law);\n"
                                     + "z = new PlayerAnswer(\"a docket bill signed\", id, sim => true);\n"
                                     + "w = new PlayerAnswer(add ? \"a planted levy\" : \"a planted repeal bill\", id, sim => true);\n"
                                     + "v = new PlayerAnswer(label, id, sim => true);\n";
                List<string> plantedMissing = BillAnswersWithoutKind(planted, out int plantedBills, out List<string> plantedUnread);
                if (plantedBills != 3 || plantedMissing.Count != 2 || plantedMissing[0] != "a planted bill" || plantedMissing[1] != "a planted levy / a planted repeal bill" || plantedUnread.Count != 1 || plantedUnread[0] != "label")
                {
                    throw new InvalidOperationException($"the bill answers' reader has no teeth - on the planted source it found {plantedBills} bill answer(s) (3), {plantedMissing.Count} without a kind (2: 'a planted bill', the conditional label) [{string.Join(", ", plantedMissing)}] and {plantedUnread.Count} label(s) it cannot read (1: label) [{string.Join(", ", plantedUnread)}]");
                }

                if (!Directory.Exists(Simulation)) { throw new InvalidOperationException($"the manager's folder {Simulation} is not there - VERIFIED NOTHING"); }
                var missing = new List<string>();
                var unread = new List<string>();
                int bills = 0;
                foreach (string path in Directory.GetFiles(Simulation, "SimulationManager*.cs", SearchOption.TopDirectoryOnly))
                {
                    List<string> without = BillAnswersWithoutKind(SourceText.ReadWithoutComments(path), out int found, out List<string> labels);
                    foreach (string what in without) { missing.Add(Path.GetFileName(path) + ": '" + what + "'"); }
                    foreach (string label in labels) { unread.Add(Path.GetFileName(path) + ": " + label); }
                    bills += found;
                }
                if (bills == 0) { throw new InvalidOperationException("no player's answer that lays a bill was found in the manager's source - the reader matched nothing, VERIFIED NOTHING"); }
                if (unread.Count > 0)
                {
                    throw new InvalidOperationException($"{unread.Count} player's answer(s) carry a label with no words this check can read, so whether each lays a bill is unknown - give it its words (a literal, or a condition between literals): {string.Join("; ", unread)}");
                }
                if (missing.Count > 0)
                {
                    throw new InvalidOperationException($"{missing.Count} of the {bills} player's answers that lay a bill carry no bill kind, so the impact ledger reads each as an answer of no family (K51): {string.Join("; ", missing)}");
                }

                Debug.Log($"=== PolicyImpactLedgerCheck: every PolicyDecision field is in exactly one ledger family, every player lever (AF-11) in one the table holds, every bill kind (K51) in one of its own, and all {bills} of the manager's bill answers carry their kind ===");
                CheckExit.Finish(0);
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError("LEDGER: " + ex.Message);
                Debug.Log("=== PolicyImpactLedgerCheck: FAILED ===");
                CheckExit.Finish(1);
            }
        }

        /// <summary>K51: every `new PlayerAnswer(` call in <paramref name="code"/> any of whose first argument's words names a bill (`NamesABill`) - the
        /// call read to its closing parenthesis, literals skipped, its first argument to the first comma outside brackets, every literal in it read (the review's
        /// L5: a label chosen by a condition) - and of those, the words of each that carries no `bill: Congress.Kind.`; <paramref name="unread"/>, the first
        /// arguments holding no literal at all, which the reader cannot judge.</summary>
        private static List<string> BillAnswersWithoutKind(string code, out int bills, out List<string> unread)
        {
            var missing = new List<string>();
            unread = new List<string>();
            bills = 0;
            const string Call = "new PlayerAnswer(";
            for (int at = code.IndexOf(Call, StringComparison.Ordinal); at >= 0; at = code.IndexOf(Call, at + Call.Length, StringComparison.Ordinal))
            {
                int open = at + Call.Length;
                string args = code.Substring(open, CallLength(code, open));
                string first = args.Substring(0, FirstArgumentLength(args));
                var words = new List<string>();
                foreach (Match m in Literal.Matches(first)) { words.Add(m.Groups[1].Value); }
                if (words.Count == 0) { unread.Add(first.Trim()); continue; }
                if (!words.Exists(NamesABill)) { continue; }
                bills++;
                if (args.IndexOf("bill: Congress.Kind.", StringComparison.Ordinal) < 0) { missing.Add(string.Join(" / ", words)); }
            }
            return missing;
        }

        /// <summary>K51: an answer's words that name a bill it lays - "a ... bill" or the alternative budget; the docket's ("a docket bill signed") lay none.</summary>
        private static bool NamesABill(string what) =>
            what == "an alternative budget" || (what.EndsWith(" bill", StringComparison.Ordinal) && !what.StartsWith("a docket bill", StringComparison.Ordinal));

        /// <summary>The review's L5: the length of a call's first argument - to the first comma outside brackets and literals.</summary>
        private static int FirstArgumentLength(string args)
        {
            int depth = 0;
            for (int i = 0; i < args.Length; i++)
            {
                char c = args[i];
                if (c == '"' || c == '\'')
                {
                    for (i++; i < args.Length && args[i] != c; i++) { if (args[i] == '\\') { i++; } }
                    continue;
                }
                if (c == '(' || c == '[' || c == '{') { depth++; }
                else if (c == ')' || c == ']' || c == '}') { depth--; }
                else if (c == ',' && depth == 0) { return i; }
            }
            return args.Length;
        }

        /// <summary>The length of a call's arguments from just after its opening parenthesis to its closing one, parentheses inside literals not counted.</summary>
        private static int CallLength(string code, int from)
        {
            int depth = 1;
            for (int i = from; i < code.Length; i++)
            {
                char c = code[i];
                if (c == '"' || c == '\'')
                {
                    for (i++; i < code.Length && code[i] != c; i++) { if (code[i] == '\\') { i++; } }
                    continue;
                }
                if (c == '(') { depth++; }
                else if (c == ')' && --depth == 0) { return i - from; }
            }
            return code.Length - from;
        }
    }
}
