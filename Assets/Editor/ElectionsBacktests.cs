using System;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §681 (the vote model's entrant layer, "every elections backtest reproduces its current figures or better"): **THE ELECTIONS BACKTESTS,
    /// ONE LAUNCH** - every harness that scores the vote or the seat model against real returns, gathered so a change to the vote model is
    /// measured on all of them BEFORE and AFTER in the same form. <c>-executeMethod PoliSim.EditorTools.ElectionsBacktests.RunBatch</c>.
    /// ⚠ Deliberately NOT in `CheckSuite`: most of these print figures to read, not verdicts to gate a commit (`ReBacktest` and
    /// `VoteShareBacktest` have no failure path, which is right for a report and wrong for a registered check - `EvidenceDiscriminationCheck`
    /// says so the moment one is registered). This is not a tier's bar; it borrows the suite's runner only so each harness's exit is collected
    /// rather than ending the process.
    /// </summary>
    public static class ElectionsBacktests
    {
        internal static readonly (string Name, Action Run)[] Set = new (string Name, Action Run)[]
        {
            ("VoteShareBacktest", VoteShareBacktest.Run),
            ("ReBacktest", ReBacktest.Run),
            ("OutOfSample2026Diagnostic", OutOfSample2026Diagnostic.Run),
            ("SeatAllocationBacktest", SeatAllocationBacktest.Run),
            ("SeatConversionHarness", SeatConversionHarness.Run),
            ("ItalySurgeCeilingDiagnostic", ItalySurgeCeilingDiagnostic.Run),
            ("GateReRun", GateReRun.Run),
            ("LoyaltyHarness", LoyaltyHarness.Run),
            ("CompositionHarness", CompositionHarness.Run),
            ("ElectionNightHarness", ElectionNightHarness.Run),
            ("ChainHarness", ChainHarness.Run),
        };

        public static void RunBatch()
        {
            int worst = CheckSuite.RunTable(Set, "elections backtests", announceClean: true);
            Debug.Log($"ELECTIONS BACKTESTS: {Set.Length} run, exiting {worst} (OutOfSample2026Diagnostic refuses by design once its kept record exists - it runs ONCE).");
            EditorApplication.Exit(worst);
        }
    }
}
