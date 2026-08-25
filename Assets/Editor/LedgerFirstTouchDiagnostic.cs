using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// THE FIRST-TOUCH WINDOW CLASS, pinned at runtime (playtest finding 1, 2026-08-25): Sweden's
    /// approval ledger failed its audit at the FIRST boundary (2027-01-01) with explained exceeding
    /// observed by exactly +1.5000 - a recorded event whose effect the observed delta doesn't
    /// contain. The writer was the foreign-policy meeting's "Send substantial aid" (+1.5), resolved
    /// mid-first-year: ApprovalLedgerRecorder.RecordEvent LAZY-CREATED the accruing ledger AFTER
    /// the option's own write, so the period opened at the post-write approval - the event is in
    /// Events, its effect is outside the [open, close] window, and the audit is over-explained by
    /// exactly that delta.
    ///
    /// The DEBT twin closed this class BY CONSTRUCTION on 2026-08-18 ("every writer passes the
    /// stock AS IT WAS BEFORE ITS OWN WRITE" - DebtLedgerRecorder.EnsureAccruing's own doc
    /// comment); the approval recorder never got the same closure. This diagnostic reproduces the
    /// exact shape deterministically - an observed approval event as the ledger's first touch
    /// before any boundary, plus the debt sibling (a scenario-seed-shaped debt event before any
    /// day) - and asserts the window: audit identity holds AND the period opened at the PRE-write
    /// value. Pre-fix it fails with the playtest's exact +1.5000 gap; the fix mirrors the twin.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.LedgerFirstTouchDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class LedgerFirstTouchDiagnostic
    {
        private const float Tolerance = 0.001f;

        [MenuItem("PoliSim/Run Ledger First-Touch Diagnostic")]
        private static void RunFromMenu()
        {
            int code = CheckExit.Collect(Run);
            Debug.Log(code == 0 ? "FIRSTTOUCH: clean." : $"FIRSTTOUCH: FAILED ({code}).");
        }

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1: this advances turns; an ATTRIB during it now fails the run.
            SimulationRandom.Seed(4242);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("FIRSTTOUCH");
            int failures = 0;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country sweden = world.GetCountry(CountryId.Sweden);

                // --- Debt sibling, the scenario-seed shape: an observed debt event BEFORE any day
                // has run, so this RecordEvent is the debt ledger's first touch. The twin already
                // opens at the pre-write stock by construction; this pins that against regression.
                float debtBeforeSeed = sweden.State.GovernmentDebt;
                sweden.State.GovernmentDebt += 500f;
                DebtLedgerRecorder.RecordEvent(sweden, sim.CurrentDate, "Diagnostic: seed-shaped debt event",
                    debtBeforeSeed, sweden.State.GovernmentDebt);

                // --- 30 days into the first period: nothing has touched the approval ledger yet
                // (bills/meetings need player resolution, economic events fire only inside the
                // boundary), so the accruing approval ledger is still null - the playtest state.
                var decisions = BuildNoOpDecisions(world);
                RunDays(sim, world, decisions, CountryId.Sweden, 30);

                // --- The incident's exact site shape (ResolveForeignPolicyMeeting): write the
                // option's approval effect, then record the observed post-clamp delta. Pre-fix,
                // this RecordEvent lazy-creates the ledger AFTER the write.
                float approvalBeforeEvent = sweden.State.ApprovalRating;
                sweden.State.ApprovalRating = Mathf.Clamp(approvalBeforeEvent + 1.5f, 0f, 100f);
                ApprovalLedgerRecorder.RecordEvent(sweden, sim.CurrentDate,
                    "Foreign policy: Send substantial aid (diagnostic shape)",
                    sweden.State.ApprovalRating - approvalBeforeEvent);

                // --- Cross the first boundary: CloseAtBoundary runs the audit (ATTRIB on red,
                // folded into the exit by ruling 1) and promotes the period.
                RunDays(sim, world, decisions, CountryId.Sweden, 340);

                ApprovalAttribution approvalPeriod = sweden.ApprovalLedgerLastPeriod;
                if (approvalPeriod == null || !approvalPeriod.Closed)
                {
                    Debug.LogError("FIRSTTOUCH: no closed approval period after 370 days - the boundary never ran, VERIFIED NOTHING.");
                    CheckExit.Finish(2);
                    return;
                }

                // The window assertion the audit implies but states less directly: the period must
                // open at the PRE-event approval - an open that already contains the first event's
                // write is exactly the class this diagnostic exists to pin.
                if (Mathf.Abs(approvalPeriod.ApprovalAtPeriodOpen - approvalBeforeEvent) > Tolerance)
                {
                    Debug.LogError($"FIRSTTOUCH: approval period opened at {approvalPeriod.ApprovalAtPeriodOpen:F4}, " +
                                   $"but the pre-event approval was {approvalBeforeEvent:F4} - the first event's write is " +
                                   "OUTSIDE the observation window (the 2027-01-01 playtest shape).");
                    failures++;
                }

                float observed = approvalPeriod.ApprovalAtClose - approvalPeriod.ApprovalAtPeriodOpen;
                float explained = approvalPeriod.EventSum + approvalPeriod.TermSum + approvalPeriod.ClampLoss;
                if (Mathf.Abs(observed - explained) > Tolerance)
                {
                    Debug.LogError($"FIRSTTOUCH: audit identity broken - observed {observed:F4} vs explained {explained:F4} " +
                                   $"(gap {explained - observed:F4}; the playtest gap was +1.5000).");
                    failures++;
                }

                DebtAttribution debtPeriod = sweden.FiscalLedgerLastPeriod;
                if (debtPeriod == null)
                {
                    Debug.LogError("FIRSTTOUCH: no closed debt period - VERIFIED NOTHING on the sibling.");
                    failures++;
                }
                else if (Mathf.Abs(debtPeriod.DebtAtPeriodOpen - debtBeforeSeed) > 0.01f)
                {
                    Debug.LogError($"FIRSTTOUCH: debt period opened at {debtPeriod.DebtAtPeriodOpen:F2}, " +
                                   $"but the pre-seed stock was {debtBeforeSeed:F2} - the twin's by-construction " +
                                   "closure regressed.");
                    failures++;
                }

                // --- Informational (playtest finding 2's measured basis, asserts nothing): the
                // Derived row reads -report.BudgetBalance, which the formula makes NET of interest
                // (TotalSpending includes InterestOnDebt - SimulationManager.ApplyRevenueAndSpending).
                FiscalTurnReport report = sim.GetLastFiscalReport(CountryId.Sweden);
                if (report != null && sweden.State.GDP > 0f)
                {
                    float primary = report.BudgetBalance + report.InterestOnDebt;
                    Debug.Log($"FISCALREAD Sweden period: revenue {report.Revenue:F1}, totalSpending {report.TotalSpending:F1} " +
                              $"(interest {report.InterestOnDebt:F1}), budgetBalance {report.BudgetBalance:F1} " +
                              $"({report.BudgetBalance / sweden.State.GDP * 100f:F2}% of GDP), primary {primary:F1} " +
                              $"({primary / sweden.State.GDP * 100f:F2}% of GDP), debt open {debtPeriod?.DebtAtPeriodOpen:F1} " +
                              $"-> close {debtPeriod?.DebtAtClose:F1}.");
                }

                Debug.Log(failures == 0
                    ? "FIRSTTOUCH: PASS - both ledgers open their windows at the pre-write value on a first-touch event."
                    : $"FIRSTTOUCH: {failures} FAILURE(S).");
                CheckExit.Finish(failures == 0 ? 0 : 1);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>The controller's own day order, mirrored from SaveLoadRoundTripDiagnostic.RunDays.</summary>
        private static void RunDays(SimulationManager sim, World world, Dictionary<CountryId, PolicyDecision> decisions,
            CountryId player, int days)
        {
            for (int day = 0; day < days; day++)
            {
                bool boundary = sim.AdvanceDay();
                sim.AdvanceCountryDayTick(player);
                if (!boundary)
                {
                    continue;
                }

                sim.AdvanceTurn(decisions);
                foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in
                         new List<(CabinetPortfolio, CabinetDecision)>(sim.GetPendingCabinetDecisions(player)))
                {
                    sim.ResolveCabinetDecision(player, portfolio, decision, decision.Options[0]);
                }
            }
        }

        private static Dictionary<CountryId, PolicyDecision> BuildNoOpDecisions(World world)
        {
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country country in world.Countries)
            {
                decisions[country.Id] = PolicyDecision.None();
            }

            return decisions;
        }
    }
}
