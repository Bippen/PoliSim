using System;
using System.Reflection;
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
    /// </summary>
    public static class PolicyImpactLedgerCheck
    {
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
                Debug.Log("=== PolicyImpactLedgerCheck: every PolicyDecision field is in exactly one ledger family ===");
                CheckExit.Finish(0);
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError("LEDGER: " + ex.Message);
                Debug.Log("=== PolicyImpactLedgerCheck: FAILED ===");
                CheckExit.Finish(1);
            }
        }
    }
}
