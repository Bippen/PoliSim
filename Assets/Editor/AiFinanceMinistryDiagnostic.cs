using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The AI finance ministry (2026-09-07, §388). No player, seed 777, six turns, then the ministry's decision read for every country from its last report as
    /// the next turn would read it: (1) an EU country owing nothing writes nothing; (2) one owing an adjustment writes cuts (every percentage negative, inside
    /// the ranges) and, where the ranges bind, a rise in the income-tax and VAT rates by one number of points; (3) one at or below 60 % in surplus writes
    /// RESTORES sized to the surplus (the Fiscal Compact's release), one at or below 60 % beyond the 1 % deficit limit writes cuts back to it; (4) the United
    /// States writes nothing while its debt ratio has not risen two years running above its seed and, when it has, cuts only its discretionary lines and no
    /// tax; (5) both directions on a clone of France - a 1 % surplus at 50 % debt restores 1 % of GDP, balance writes nothing, a 2 % deficit at 50 % closes
    /// 1 % of GDP, a 4 % deficit takes the 0.5 % EDP adjustment; (6) THE DECISION OBJECTS THE CALLER HANDED IN CARRY NOTHING AFTER SIX TURNS - the ministry
    /// withdraws what it wrote, or a reused object re-applies one year's cuts forever (the defect the first three builds carried); (7) with Sweden the player,
    /// Sweden's rates are untouched after six turns and the ministry never observed it. The century is the trajectory family's (§388).
    /// </summary>
    public static class AiFinanceMinistryDiagnostic
    {
        private const int Turns = 6;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var report = new List<string>();
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("FINMIN");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                foreach (Country c in world.Countries) { if (c.DebtRatioSeed <= 0f) { Debug.LogError($"FINMIN: {c.Id} carries no seed debt ratio."); ok = false; } }
                for (int year = 1; year <= Turns; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                }
                // (6) the caller's objects are untouched after the turn
                foreach (var kv in decisions) { if (kv.Value.SpendingLineChanges.Count > 0 || kv.Value.TaxRateOverrides.Count > 0) { Debug.LogError($"FINMIN: the decision object handed in for {kv.Key} carries {kv.Value.SpendingLineChanges.Count} line changes and {kv.Value.TaxRateOverrides.Count} rate overrides after the turn - the ministry did not withdraw what it wrote."); ok = false; } }
                bool sawCuts = false;
                foreach (Country c in world.Countries)
                {
                    FiscalTurnReport last = sim.GetLastFiscalReport(c.Id);
                    if (last == null) { Debug.LogError($"FINMIN: {c.Id} has no fiscal report after {Turns} turns."); ok = false; continue; }
                    float balance = last.BudgetBalance / c.State.NominalGdp * 100f;
                    float debt = c.State.DebtToGdpRatio;
                    PolicyDecision d = AiFinanceMinistry.Decide(c, last);
                    int cuts = 0, restores = 0, raises = 0; float worst = 0f;
                    foreach (var kv in d.SpendingLineChanges) { if (kv.Value < 0f) { cuts++; worst = Mathf.Min(worst, kv.Value); } else if (kv.Value > 0f) { restores++; worst = Mathf.Max(worst, kv.Value); } }
                    foreach (var kv in d.TaxRateOverrides) { raises++; if (kv.Key != TaxType.IncomeTax && kv.Key != TaxType.VAT) { Debug.LogError($"FINMIN: {c.Id} touched {kv.Key} - only the income tax and VAT are the ministry's."); ok = false; } }
                    if (cuts > 0 && restores > 0) { Debug.LogError($"FINMIN: {c.Id} both cut and restored in one decision."); ok = false; }
                    if (AiFinanceMinistry.IsEuMember(c.Id))
                    {
                        float fall = c.DebtRatioLastReport > 0f ? c.DebtRatioLastReport - debt : 0f;
                        float required = AiFinanceMinistry.EuRequiredAdjustmentPercentOfGdp(balance, debt, fall);
                        if (required == 0f && (cuts > 0 || restores > 0 || raises > 0)) { Debug.LogError($"FINMIN: {c.Id} owes nothing (debt {debt:F1}, balance {balance:F2}, fall {fall:F2}) and the ministry wrote {cuts} cuts, {restores} restores, {raises} raises."); ok = false; }
                        if (required > 0f && cuts == 0) { Debug.LogError($"FINMIN: {c.Id} owes {required:F2} % of GDP (debt {debt:F1}, balance {balance:F2}) and the ministry wrote no cut."); ok = false; }
                        if (required < 0f && (restores == 0 || cuts > 0 || raises > 0)) { Debug.LogError($"FINMIN: {c.Id} is owed a release of {-required:F2} % of GDP (debt {debt:F1}, balance {balance:F2}) and the ministry wrote {restores} restores, {cuts} cuts, {raises} raises."); ok = false; }
                        if (required > 0f && cuts > 0) { sawCuts = true; }
                        report.Add($"{c.Id}: debt {debt:F1} % (fall {fall:F2}), balance {balance:F2} % → owes {required:F2} % of GDP, {cuts} cut(s), {restores} restore(s) (largest {worst:F2} %), {raises} rate rise(s)");
                    }
                    else
                    {
                        bool triggered = AiFinanceMinistry.UsTriggered(c, debt);
                        if (!triggered && (cuts > 0 || restores > 0 || raises > 0)) { Debug.LogError($"FINMIN: the United States wrote {cuts} cuts, {restores} restores, {raises} raises without its trigger (debt {debt:F1}, last {c.DebtRatioLastReport:F1}, before {c.DebtRatioReportBefore:F1}, seed {c.DebtRatioSeed:F1})."); ok = false; }
                        if (triggered && cuts == 0) { Debug.LogError("FINMIN: the United States is triggered and wrote no cut."); ok = false; }
                        if (raises > 0 || restores > 0) { Debug.LogError("FINMIN: the United States wrote a tax rise or a restore - its statutes are spending-side and one-sided."); ok = false; }
                        foreach (var kv in d.SpendingLineChanges) { foreach (SpendingLine line in c.SpendingLines) { if (line.Category == kv.Key && line.IsMandatory) { Debug.LogError($"FINMIN: the United States sequestered a mandatory line ({kv.Key}) - exempt under 2 U.S.C. § 902."); ok = false; } } }
                        report.Add($"USA: debt {debt:F1} % (seed {c.DebtRatioSeed:F1}, last {c.DebtRatioLastReport:F1}, before {c.DebtRatioReportBefore:F1}), balance {balance:F2} % → {(triggered ? "trigger" : "no trigger")}, {cuts} discretionary cut(s), worst {worst:F2} %");
                    }
                }
                if (!sawCuts) { Debug.LogError("FINMIN: no EU country owing an adjustment wrote a cut - the corrective direction was not exercised."); ok = false; }
                // (5) both directions on France
                Country france = world.GetCountry(CountryId.France);
                FiscalTurnReport franceLast = sim.GetLastFiscalReport(CountryId.France);
                float savedDebt = france.State.GovernmentDebt; float savedBalance = franceLast.BudgetBalance; float savedLast = france.DebtRatioLastReport;
                france.DebtRatioLastReport = 0f;
                france.State.GovernmentDebt = 0.5f * france.State.NominalGdp;
                float ngdp = france.State.NominalGdp;
                franceLast.BudgetBalance = 0.01f * ngdp;
                float given = Sum(france, AiFinanceMinistry.Decide(france, franceLast), out int givenNeg, out int givenPos);
                if (givenPos == 0 || givenNeg > 0 || Mathf.Abs(given / ngdp * 100f - 1f) > 0.05f) { Debug.LogError($"FINMIN: France at 50 % debt and a 1 % surplus did not release the surplus ({given / ngdp * 100f:F3} % of GDP, {givenNeg} cuts)."); ok = false; }
                franceLast.BudgetBalance = 0f;
                PolicyDecision atBalance = AiFinanceMinistry.Decide(france, franceLast);
                if (atBalance.SpendingLineChanges.Count > 0 || atBalance.TaxRateOverrides.Count > 0) { Debug.LogError("FINMIN: France at 50 % debt and balance received a decision - the rule does not rest."); ok = false; }
                franceLast.BudgetBalance = -0.02f * ngdp;
                float closed = -Sum(france, AiFinanceMinistry.Decide(france, franceLast), out int closedNeg, out int closedPos);
                if (closedNeg == 0 || closedPos > 0 || Mathf.Abs(closed / ngdp * 100f - 1f) > 0.05f) { Debug.LogError($"FINMIN: France at 50 % debt and a 2 % deficit did not close the deficit back to the 1 % limit ({closed / ngdp * 100f:F3} % of GDP, {closedPos} restores)."); ok = false; }
                franceLast.BudgetBalance = -0.04f * ngdp;
                PolicyDecision edp = AiFinanceMinistry.Decide(france, franceLast);
                float taken = -Sum(france, edp, out int edpNeg, out int edpPos);
                if (edpNeg == 0 || (taken / ngdp * 100f < AiFinanceMinistry.EdpAdjustmentPercentOfGdp - 0.02f && edp.TaxRateOverrides.Count == 0)) { Debug.LogError($"FINMIN: France at a 4 % deficit and 50 % debt did not take the EDP adjustment ({taken / ngdp * 100f:F3} % of GDP in cuts, {edp.TaxRateOverrides.Count} rate rises)."); ok = false; }
                france.State.GovernmentDebt = savedDebt; franceLast.BudgetBalance = savedBalance; france.DebtRatioLastReport = savedLast;
                report.Add($"France at 50 % debt: a 1 % surplus → {givenPos} restores worth {given / ngdp * 100f:F3} % of GDP; balance → nothing; a 2 % deficit → {closedNeg} cuts worth {closed / ngdp * 100f:F3} %; a 4 % deficit → {edpNeg} cuts worth {taken / ngdp * 100f:F3} % and {edp.TaxRateOverrides.Count} rate rise(s)");
            }
            finally { Object.DestroyImmediate(go); }
            // (7) the player is never read
            SimulationRandom.Seed(777);
            World world2 = WorldFactory.CreateDefault();
            var go2 = new GameObject("FINMIN-PLAYER");
            try
            {
                SimulationManager sim2 = go2.AddComponent<SimulationManager>();
                sim2.SetWorld(world2);
                sim2.PlayerCountryId = CountryId.Sweden;
                Country sweden = world2.GetCountry(CountryId.Sweden);
                var seedRates = new Dictionary<TaxType, float>(); foreach (TaxLine t in sweden.TaxLines) { seedRates[t.Type] = t.Rate; }
                var decisions2 = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world2.Countries) { decisions2[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= Turns; year++) { for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim2.AdvanceDay(); } sim2.AdvanceTurn(decisions2); }
                foreach (TaxLine t in sweden.TaxLines)
                {
                    // EN-4e (§471): the carbon line moves by STATUTE between decisions - Sweden's is indexed by the year's price ratio - so the ministry's
                    // hands-off is read on the REAL rate for that line (the seed within the four-decimal rounding's drift), the nominal figure for every other
                    if (t.IsPerTonne) { float real = CarbonRateStatute.RealRate(t.Rate, sweden.State.PriceLevel); if (Mathf.Abs(real - seedRates[t.Type]) > seedRates[t.Type] * 5e-4f * Turns) { Debug.LogError($"FINMIN: the player's {t.Type} real rate moved ({seedRates[t.Type]} → {real}) beyond the statute's own indexation - the ministry read the player."); ok = false; } continue; }
                    if (Mathf.Abs(t.Rate - seedRates[t.Type]) > 1e-6f) { Debug.LogError($"FINMIN: the player's {t.Type} rate moved ({seedRates[t.Type]} → {t.Rate}) - the ministry read the player."); ok = false; }
                }
                if (sweden.DebtRatioLastReport != 0f) { Debug.LogError("FINMIN: the ministry observed the player's debt ratio."); ok = false; }
            }
            finally { Object.DestroyImmediate(go2); }
            Debug.Log($"FINMIN: after {Turns} turns - {string.Join("; ", report)}.");
            Debug.Log(ok ? "FINMIN: PASS - nothing owed nothing written, an adjustment owed cuts then rates, a surplus below 60 % given back and a deficit beyond 1 % closed, the United States on its trigger and its discretionary lines only, both directions, the caller's objects withdrawn, the player never read." : "FINMIN: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>The nominal sum of a decision's line changes on a country (positive = restored, negative = cut), with the counts of each sign.</summary>
        private static float Sum(Country c, PolicyDecision d, out int negatives, out int positives)
        {
            float sum = 0f; negatives = 0; positives = 0;
            foreach (var kv in d.SpendingLineChanges)
            {
                if (kv.Value < 0f) { negatives++; } else if (kv.Value > 0f) { positives++; }
                foreach (SpendingLine line in c.SpendingLines) { if (line.Category == kv.Key) { sum += kv.Value / 100f * line.Amount; } }
            }
            return sum;
        }
    }
}
