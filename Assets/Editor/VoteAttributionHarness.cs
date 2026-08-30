using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-D4's harness — §31's post-election attribution, on the W-C1 staging's own campaign.
    ///
    /// **The done-when has two clauses and the first is an identity, not a tolerance:** the
    /// attribution lines must sum to the actual deviation from baseline. They do so EXACTLY — to
    /// the last bit the double can carry — because the decomposition is Shapley over the sources,
    /// and Shapley's efficiency axiom is that identity. The worklist allows "within a stated
    /// tolerance"; the stated tolerance here is 1e-12 of a share, which is floating-point noise
    /// rather than modelling slack, and the harness prints the largest residual it saw so the
    /// claim can be checked rather than believed.
    ///
    /// **The second clause: no line is authored prose.** Every label is a `VoteAttributionSource`
    /// enum name — a mechanism the model applies — and the harness asserts by reflection that the
    /// instrument carries no string field at all, so a line CANNOT be a sentence someone wrote.
    /// </summary>
    public static class VoteAttributionHarness
    {
        private const double Tolerance = 1e-12;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-D4: post-election attribution (section 31) - every line derived, the lines summing to the movement they explain ===\n");

            CampaignRun.Setup setup = CampaignAiHarness.BuildSetup(out _);
            CampaignRun.Result run = CampaignAiHarness.RunSeeded(setup, 777);

            failures += Structural(sb);
            failures += Identity(sb, setup, run);
            failures += Readable(sb, setup, run);

            sb.Append($"\nATTRIBUTION: {(failures == 0 ? "all assertions hold" : failures + " FAILED")}\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Build one party's ledger from what the run RECORDED, never from a recomputation.</summary>
        private static VoteAttribution.Ledger LedgerFor(CampaignRun.Setup setup, CampaignRun.Result run, int party)
        {
            var input = new VoteAttribution.Inputs
            {
                OwnPersuasionByAction = run.Parties[party].PersuasionByAction,
                AttacksReceived = run.Parties[party].PersuasionAgainstMe,
                TotalPersuasionPerParty = run.PersuasionPerParty,
                BaseCompatibility = setup.Compatibility,
                PriorShares = setup.PriorShares,
                LoyaltyPerParty = setup.LoyaltyPerParty,
            };

            return VoteAttribution.Explain(input, party);
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }

        /// <summary>No line can be authored prose - asserted structurally, not promised in a comment.</summary>
        private static int Structural(StringBuilder sb)
        {
            int failures = 0;

            var offenders = new StringBuilder();
            foreach (Type t in new[] { typeof(VoteAttribution), typeof(VoteAttribution.Ledger), typeof(VoteAttribution.Inputs) })
            {
                foreach (FieldInfo fi in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (fi.FieldType == typeof(string) || fi.FieldType == typeof(string[])) { offenders.Append(t.Name).Append('.').Append(fi.Name).Append(' '); }
                }

                foreach (PropertyInfo pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (pi.PropertyType == typeof(string) || pi.PropertyType == typeof(string[])) { offenders.Append(t.Name).Append('.').Append(pi.Name).Append(' '); }
                }
            }

            failures += Assert(sb, "0a. no line can be authored prose: the instrument carries no string field or property at all, so a label can only be a mechanism's enum name",
                offenders.Length == 0, offenders.Length == 0 ? "no string anywhere in the instrument" : $"offenders: {offenders}");

            // Every source the enum declares must be swept. A source declared and never attributed
            // would be a line that silently reads zero.
            bool allSwept = VoteAttribution.Sources.Length == Enum.GetValues(typeof(VoteAttributionSource)).Length;
            failures += Assert(sb, "0b. every source the enum declares is swept (no silently-zero line)",
                allSwept, $"{VoteAttribution.Sources.Length} sources swept of {Enum.GetValues(typeof(VoteAttributionSource)).Length} declared");

            return failures;
        }

        /// <summary>THE DONE-WHEN'S FIRST CLAUSE: the lines sum to the deviation from baseline.</summary>
        private static int Identity(StringBuilder sb, CampaignRun.Setup setup, CampaignRun.Result run)
        {
            int failures = 0;
            double worst = 0.0;
            int worstParty = -1;
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                VoteAttribution.Ledger ledger = LedgerFor(setup, run, p);
                if (Math.Abs(ledger.Residual) > Math.Abs(worst)) { worst = ledger.Residual; worstParty = p; }
            }

            failures += Assert(sb, "1a. THE CLAUSE: for every party the lines sum to the deviation from baseline, to within 1e-12 of a share",
                Math.Abs(worst) <= Tolerance,
                string.Format(CultureInfo.InvariantCulture, "largest residual {0:E3} (party {1}), tolerance {2:E0} - Shapley's efficiency axiom, so this is floating-point noise and not modelling slack",
                    worst, worstParty < 0 ? "none" : setup.Parties[worstParty].Name, Tolerance));

            // And the baseline the ledger opens at must be the campaign's own baseline, not a
            // number the instrument invented for itself.
            double worstBaseline = 0.0;
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                VoteAttribution.Ledger ledger = LedgerFor(setup, run, p);
                worstBaseline = Math.Max(worstBaseline, Math.Abs(ledger.ShareAtBaseline - run.BaselineShares[p]));
            }

            failures += Assert(sb, "1b. the ledger opens at the campaign's OWN baseline (an empty campaign's shares), not at a figure of its own",
                worstBaseline <= 1e-12, string.Format(CultureInfo.InvariantCulture, "largest difference from Result.BaselineShares {0:E3}", worstBaseline));

            double worstClose = 0.0;
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                VoteAttribution.Ledger ledger = LedgerFor(setup, run, p);
                worstClose = Math.Max(worstClose, Math.Abs(ledger.ShareAtClose - run.FinalShares[p]));
            }

            failures += Assert(sb, "1c. and it closes at the share the campaign actually produced",
                worstClose <= 1e-12, string.Format(CultureInfo.InvariantCulture, "largest difference from Result.FinalShares {0:E3}", worstClose));

            return failures;
        }

        /// <summary>The ledger printed as §31 asks for it - "why you won" and "why you lost" for the two ends of the result - and the properties a reading has to have to be worth showing.</summary>
        private static int Readable(StringBuilder sb, CampaignRun.Setup setup, CampaignRun.Result run)
        {
            int failures = 0;

            int best = 0, worst = 0;
            for (int p = 1; p < setup.Parties.Length; p++)
            {
                double gain = run.FinalShares[p] - run.BaselineShares[p];
                if (gain > run.FinalShares[best] - run.BaselineShares[best]) { best = p; }
                if (gain < run.FinalShares[worst] - run.BaselineShares[worst]) { worst = p; }
            }

            foreach (int p in new[] { best, worst })
            {
                VoteAttribution.Ledger ledger = LedgerFor(setup, run, p);
                sb.Append(string.Format(CultureInfo.InvariantCulture, "\n  {0} {1} - baseline {2:P2} -> {3:P2} ({4:+0.00;-0.00} pp):\n",
                    setup.Parties[p].Name, p == best ? "(the campaign's largest gain)" : "(the campaign's largest loss)",
                    ledger.ShareAtBaseline, ledger.ShareAtClose, ledger.Deviation * 100.0));

                var ordered = new List<KeyValuePair<VoteAttributionSource, double>>(ledger.Lines);
                ordered.Sort((a, b) => Math.Abs(b.Value).CompareTo(Math.Abs(a.Value)));
                foreach (KeyValuePair<VoteAttributionSource, double> line in ordered)
                {
                    if (Math.Abs(line.Value) < 5e-7) { continue; }   // below a hundredth of a pp - print nothing rather than a row of zeroes
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-20} {1,8:+0.000;-0.000} pp\n", line.Key, line.Value * 100.0));
                }

                sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-20} {1,8:+0.000;-0.000} pp   (residual {2:E2})\n",
                    "TOTAL", ledger.LineSum * 100.0, ledger.Residual));
            }

            // A reading nobody can act on is not an explanation. Two properties are asserted: the
            // party that gained most has a positive total, and a source the party never used
            // contributes exactly nothing - if an unused action showed a number, the instrument
            // would be attributing movement to something that never happened.
            VoteAttribution.Ledger bestLedger = LedgerFor(setup, run, best);
            failures += Assert(sb, "2a. the ledger's sign matches the result: the party that gained most has a positive total",
                bestLedger.LineSum > 0.0, string.Format(CultureInfo.InvariantCulture, "{0} total {1:+0.000;-0.000} pp", setup.Parties[best].Name, bestLedger.LineSum * 100.0));

            var unused = new List<string>();
            bool unusedAreZero = true;
            for (int i = 0; i < 8; i++)
            {
                if (run.Parties[best].PersuasionByAction[i] != 0.0) { continue; }
                var source = (VoteAttributionSource)i;
                unused.Add(source.ToString());
                if (Math.Abs(bestLedger.Lines[source]) > Tolerance) { unusedAreZero = false; }
            }

            failures += Assert(sb, "2b. an action the party never took contributes exactly nothing - the ledger cannot attribute movement to something that did not happen",
                unusedAreZero, unused.Count == 0 ? "the party used every action kind" : $"{string.Join(", ", unused.ToArray())} unused, all exactly 0");

            // And the instrument must be a reading of THIS run: re-run the same seed and the
            // ledger must come back identical, line for line.
            CampaignRun.Result again = CampaignAiHarness.RunSeeded(setup, 777);
            VoteAttribution.Ledger twice = LedgerFor(setup, again, best);
            bool identical = Math.Abs(twice.LineSum - bestLedger.LineSum) <= Tolerance;
            foreach (VoteAttributionSource s in VoteAttribution.Sources)
            {
                if (Math.Abs(twice.Lines[s] - bestLedger.Lines[s]) > Tolerance) { identical = false; }
            }

            failures += Assert(sb, "2c. the ledger is a reading of the run, not of the reader: the same seed gives the same lines",
                identical, "line for line under seed 777");

            return failures;
        }
    }
}
