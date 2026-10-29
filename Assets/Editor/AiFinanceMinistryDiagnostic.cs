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
    /// States writes nothing - PS-6 US-24 (R-US21 (v) as ruled): its rule is the debt limit (`UsDebtLimit`), the ratio-trend trigger and its borrowed
    /// sequester retired, so after six turns of a rising ratio it still writes no cut, no restore and no tax; (5) both directions on a clone of France - a 1 % surplus at 50 % debt restores 1 % of GDP, balance writes nothing, a 2 % deficit at 50 % closes
    /// 1 % of GDP, a 4 % deficit takes the 0.5 % EDP adjustment; (6) THE DECISION OBJECTS THE CALLER HANDED IN CARRY NOTHING AFTER SIX TURNS - the ministry
    /// withdraws what it wrote, or a reused object re-applies one year's cuts forever (the defect the first three builds carried); (7) with Sweden the player,
    /// Sweden's rates are untouched after six turns and the ministry never observed it. The century is the trajectory family's (§388).
    /// ⚠ K251 (2026-10-29, the rule given its stop): (3) and (5) now read the Fiscal Compact's 1 % deficit limit as the resting point below 60 % - a 1 % surplus
    /// releases 2 % of GDP, the raised income tax (planted two points above its seed) lowered first, a deficit at the limit writes nothing, a 2 % deficit closes
    /// back by cuts with no rate raised; (8) France at 80 %, its ratio two points lower than last year with balanced books, releases 0.5 % of GDP (the fall's
    /// excess over the safeguard's pace, stopped at the 0.5 % limit), and at a 0.2-point fall it cuts toward the pace.
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
                    float balance = last.FlowBalance / c.State.NominalGdp * 100f;   // AF-17 (M2): the figure the ministry reads
                    float debt = c.State.DebtToGdpRatio;
                    PolicyDecision d = AiFinanceMinistry.Decide(c, last);
                    int cuts = 0, restores = 0, raises = 0, lowers = 0; float worst = 0f;
                    foreach (var kv in d.SpendingLineChanges) { if (kv.Value < 0f) { cuts++; worst = Mathf.Min(worst, kv.Value); } else if (kv.Value > 0f) { restores++; worst = Mathf.Max(worst, kv.Value); } }
                    foreach (var kv in d.TaxRateOverrides)
                    {
                        if (kv.Key != TaxType.IncomeTax && kv.Key != TaxType.VAT) { Debug.LogError($"FINMIN: {c.Id} touched {kv.Key} - only the income tax and VAT are the ministry's."); ok = false; }
                        if (kv.Value < RateOf(c, kv.Key)) { lowers++; } else { raises++; }   // K251 (H2): the release lowers a raised rate, the consolidation raises one
                    }
                    if (cuts > 0 && restores > 0) { Debug.LogError($"FINMIN: {c.Id} both cut and restored in one decision."); ok = false; }
                    if (AiFinanceMinistry.IsEuMember(c.Id))
                    {
                        float fall = c.DebtRatioLastReport > 0f ? c.DebtRatioLastReport - debt : 0f;
                        float required = AiFinanceMinistry.EuRequiredAdjustmentPercentOfGdp(balance, debt, fall);
                        if (required == 0f && (cuts > 0 || restores > 0 || raises > 0 || lowers > 0)) { Debug.LogError($"FINMIN: {c.Id} owes nothing (debt {debt:F1}, balance {balance:F2}, fall {fall:F2}) and the ministry wrote {cuts} cuts, {restores} restores, {raises} raises, {lowers} rate cuts."); ok = false; }
                        if (required > 0f && (cuts == 0 || lowers > 0 || restores > 0)) { Debug.LogError($"FINMIN: {c.Id} owes {required:F2} % of GDP (debt {debt:F1}, balance {balance:F2}) and the ministry wrote {cuts} cuts, {restores} restores, {lowers} rate cuts."); ok = false; }
                        if (required > 0f && raises > 0 && debt <= AiFinanceMinistry.EuDebtReferencePercent && balance >= -AiFinanceMinistry.EuDeficitReferencePercent) { Debug.LogError($"FINMIN: {c.Id} raised {raises} rate(s) below 60 % with the deficit inside 3 % - the rates rise only while the Pact requires consolidation (K251)."); ok = false; }
                        if (required < 0f && ((restores == 0 && lowers == 0) || cuts > 0 || raises > 0)) { Debug.LogError($"FINMIN: {c.Id} is owed a release of {-required:F2} % of GDP (debt {debt:F1}, balance {balance:F2}) and the ministry wrote {restores} restores, {lowers} rate cuts, {cuts} cuts, {raises} raises."); ok = false; }
                        if (required > 0f && cuts > 0) { sawCuts = true; }
                        report.Add($"{c.Id}: debt {debt:F1} % (fall {fall:F2}), balance {balance:F2} % → owes {required:F2} % of GDP, {cuts} cut(s), {restores} restore(s) (largest {worst:F2} %), {raises} rate rise(s), {lowers} rate cut(s)");
                    }
                    else
                    {
                        // PS-6 US-24 (R-US21 (v)): the retired ratio-trend trigger, reconstructed from the memory, only to say whether it would have fired here
                        bool wouldHaveFired = c.DebtRatioSeed > 0f && debt > c.DebtRatioSeed && c.DebtRatioLastReport > 0f && debt > c.DebtRatioLastReport && c.DebtRatioReportBefore > 0f && c.DebtRatioLastReport > c.DebtRatioReportBefore;
                        if (cuts > 0 || restores > 0 || raises > 0) { Debug.LogError($"FINMIN: the United States wrote {cuts} cuts, {restores} restores, {raises} raises - since R-US21 (v) the ministry writes nothing for it (its rule is the debt limit)."); ok = false; }
                        report.Add($"USA: debt {debt:F1} % (seed {c.DebtRatioSeed:F1}, last {c.DebtRatioLastReport:F1}, before {c.DebtRatioReportBefore:F1}), balance {balance:F2} % → nothing written (R-US21 (v): the debt limit is its rule; the retired ratio-trend trigger {(wouldHaveFired ? "would have fired" : "would not have fired")})");
                    }
                }
                if (!sawCuts) { Debug.LogError("FINMIN: no EU country owing an adjustment wrote a cut - the corrective direction was not exercised."); ok = false; }
                // (5) both directions on France
                Country france = world.GetCountry(CountryId.France);
                FiscalTurnReport franceLast = sim.GetLastFiscalReport(CountryId.France);
                float savedDebt = france.State.GovernmentDebt; float savedBalance = franceLast.FlowBalance; float savedLast = france.DebtRatioLastReport;
                france.DebtRatioLastReport = 0f;
                france.State.GovernmentDebt = 0.5f * france.State.NominalGdp;
                float ngdp = france.State.NominalGdp;
                // K251 (the review's H1/H2): below 60 % the resting point is the Fiscal Compact's 1 % deficit limit, and a release lowers the rates the rule raised
                // (each to its seed at most) before it gives lines back - so the release is measured on lines AND rates together (Released)
                // K251 (the review's H1/H2, review 2's M-N1/L-N2): below 60 % the resting point is the Fiscal Compact's 1 % deficit limit, a release moves a share of
                // the gap a year capped at one point of GDP (EuReleaseCapPercentOfGdp), and it lowers the rates the rule raised - each TO ITS SEED - before any line is given back
                TaxLine franceIncome = null; foreach (TaxLine t in france.TaxLines) { if (t.Type == TaxType.IncomeTax && t.IsImplemented) { franceIncome = t; } }
                float savedIncomeRate = franceIncome != null ? franceIncome.Rate : 0f;
                float wantRelease = Mathf.Min(2f * AiFinanceMinistry.EuGapShareClosedPerYear, AiFinanceMinistry.EuReleaseCapPercentOfGdp);   // a 1 % surplus: the gap to the limit is 2 points
                franceLast.FlowBalance = 0.01f * ngdp;
                // (a) a raise larger than the release: the rate comes down, no line is restored
                if (franceIncome != null) { franceIncome.Rate = franceIncome.RateSeed + 2f; }
                PolicyDecision bigRaise = AiFinanceMinistry.Decide(france, franceLast);
                float bigGiven = Released(france, bigRaise, out int bigNeg, out int bigPos, out float bigRates);
                if (bigNeg > 0 || bigPos > 0 || Mathf.Abs(bigGiven / ngdp * 100f - wantRelease) > 0.02f) { Debug.LogError($"FINMIN: France at 50 % debt, a 1 % surplus and its income tax two points above its seed did not release {wantRelease:F2} % of GDP on the rates alone ({bigGiven / ngdp * 100f:F3} %, {bigPos} restores, {bigNeg} cuts)."); ok = false; }
                // (b) a raise smaller than the release: the rate reaches its seed, and only then are lines restored with the rest
                float smallPoints = 0.2f;
                if (franceIncome != null) { franceIncome.Rate = franceIncome.RateSeed + smallPoints; }
                PolicyDecision release = AiFinanceMinistry.Decide(france, franceLast);
                float given = Released(france, release, out int givenNeg, out int givenPos, out float rateCut);
                bool rateFirst = franceIncome == null || (release.TaxRateOverrides.TryGetValue(TaxType.IncomeTax, out float incomeAfter) && Mathf.Abs(incomeAfter - franceIncome.RateSeed) < 1e-4f);
                if (givenNeg > 0 || givenPos == 0 || !rateFirst || Mathf.Abs(given / ngdp * 100f - wantRelease) > 0.02f) { Debug.LogError($"FINMIN: France at 50 % debt, a 1 % surplus and its income tax {smallPoints} points above its seed did not take the rate to its seed before restoring lines with the rest of {wantRelease:F2} % of GDP ({given / ngdp * 100f:F3} %, {rateCut / ngdp * 100f:F3} on rates, {givenPos} restores, the rate at its seed: {rateFirst})."); ok = false; }
                if (franceIncome != null) { franceIncome.Rate = savedIncomeRate; }
                franceLast.FlowBalance = -AiFinanceMinistry.EuLowDebtDeficitLimitPercent / 100f * ngdp;
                PolicyDecision atLimit = AiFinanceMinistry.Decide(france, franceLast);
                if (atLimit.SpendingLineChanges.Count > 0 || atLimit.TaxRateOverrides.Count > 0) { Debug.LogError("FINMIN: France at 50 % debt and a deficit at the 1 % limit received a decision - the rule does not rest at its limit."); ok = false; }
                franceLast.FlowBalance = -0.02f * ngdp;
                PolicyDecision closeBack = AiFinanceMinistry.Decide(france, franceLast);
                float closed = -Sum(france, closeBack, out int closedNeg, out int closedPos);
                if (closedNeg == 0 || closedPos > 0 || Mathf.Abs(closed / ngdp * 100f - 1f) > 0.05f) { Debug.LogError($"FINMIN: France at 50 % debt and a 2 % deficit did not close the deficit back to the 1 % limit ({closed / ngdp * 100f:F3} % of GDP, {closedPos} restores)."); ok = false; }
                if (closeBack.TaxRateOverrides.Count > 0) { Debug.LogError($"FINMIN: France at 50 % debt and a 2 % deficit raised {closeBack.TaxRateOverrides.Count} rate(s) - below 60 % with the deficit inside 3 % the close-back cuts lines only (K251)."); ok = false; }
                franceLast.FlowBalance = -0.04f * ngdp;
                PolicyDecision edp = AiFinanceMinistry.Decide(france, franceLast);
                float taken = -Sum(france, edp, out int edpNeg, out int edpPos);
                if (edpNeg == 0 || (taken / ngdp * 100f < AiFinanceMinistry.EdpAdjustmentPercentOfGdp - 0.02f && edp.TaxRateOverrides.Count == 0)) { Debug.LogError($"FINMIN: France at a 4 % deficit and 50 % debt did not take the EDP adjustment ({taken / ngdp * 100f:F3} % of GDP in cuts, {edp.TaxRateOverrides.Count} rate rises)."); ok = false; }
                // (9) K251 (H2, review 2's L-N2): THE RATE GUARD EXERCISED - every line but one pinned, so a uniform cut runs into what it may take and leaves a
                // shortfall: below 60 % with the deficit inside 3 % (the close-back) no rate may rise; with the deficit beyond 3 % (consolidation) the rates take it
                var pinnedHere = new List<SpendingLine>();
                for (int i = 1; i < france.SpendingLines.Count; i++) { if (!france.SpendingLines[i].Pinned) { france.SpendingLines[i].Pinned = true; pinnedHere.Add(france.SpendingLines[i]); } }
                franceLast.FlowBalance = -0.029f * ngdp;
                PolicyDecision guarded = AiFinanceMinistry.Decide(france, franceLast);
                float guardedTaken = -Sum(france, guarded, out int guardedNeg, out int guardedPos);
                bool shortfallReached = guardedTaken < (2.9f - AiFinanceMinistry.EuLowDebtDeficitLimitPercent) / 100f * ngdp - 1e-3f;
                if (!shortfallReached) { Debug.LogError($"FINMIN: the guard's fixture did not leave a shortfall (cut {guardedTaken / ngdp * 100f:F3} % of GDP of the {2.9f - AiFinanceMinistry.EuLowDebtDeficitLimitPercent:F1} owed) - the rate guard was not exercised; pin more."); ok = false; }
                if (guarded.TaxRateOverrides.Count > 0) { Debug.LogError($"FINMIN: France at 50 % debt and a 2.9 % deficit, its cut short of what is owed, raised {guarded.TaxRateOverrides.Count} rate(s) - below 60 % inside 3 % the rates do not rise (K251)."); ok = false; }
                franceLast.FlowBalance = -0.04f * ngdp;
                PolicyDecision consolidation = AiFinanceMinistry.Decide(france, franceLast);
                if (consolidation.TaxRateOverrides.Count == 0) { Debug.LogError("FINMIN: France at 50 % debt and a 4 % deficit, its cut short of what is owed, raised no rate - the consolidation's rate step did not run."); ok = false; }
                foreach (SpendingLine line in pinnedHere) { line.Pinned = false; }
                // (8) K251 (H1, review 2's M-N1): above 60 % a ratio falling faster than the safeguard's pace releases a share of its excess, no further than the 0.5 %
                // limit and no more than the cap - France at 80 %, the ratio two points lower than last year, the books balanced: the pace 0.5, the excess 1.5, the
                // room to the limit 0.5, so the release is the share of 0.5 under the cap
                france.State.GovernmentDebt = 0.8f * ngdp;
                france.DebtRatioLastReport = france.State.DebtToGdpRatio + 2f;
                franceLast.FlowBalance = 0f;
                float wantFast = Mathf.Min(AiFinanceMinistry.EuHighDebtDeficitLimitPercent * AiFinanceMinistry.EuGapShareClosedPerYear, AiFinanceMinistry.EuReleaseCapPercentOfGdp);
                float fastFall = Released(france, AiFinanceMinistry.Decide(france, franceLast), out int fastNeg, out int fastPos, out float fastRates);
                if (fastNeg > 0 || Mathf.Abs(fastFall / ngdp * 100f - wantFast) > 0.02f) { Debug.LogError($"FINMIN: France at 80 % debt falling two points a year with balanced books did not release {wantFast:F2} % of GDP ({fastFall / ngdp * 100f:F3}, {fastNeg} cuts)."); ok = false; }
                france.DebtRatioLastReport = france.State.DebtToGdpRatio + 0.2f;
                PolicyDecision slowFall = AiFinanceMinistry.Decide(france, franceLast);
                if (Sum(france, slowFall, out int slowNeg, out int slowPos) >= 0f || slowPos > 0) { Debug.LogError("FINMIN: France at 80 % debt falling 0.2 points a year did not cut toward the safeguard's pace."); ok = false; }
                // (10) K251/K271 (review 3's M2): THE RELEASE CAP DISCRIMINATED - France at 50 % debt with a 3 % surplus: the gap to the 1 % limit is 4 points, half
                // of it is 2, so the cap decides - one point of GDP (half a point would release 0.5, no cap 2.0); the rates at their seeds, so it is all lines
                france.State.GovernmentDebt = 0.5f * ngdp;
                france.DebtRatioLastReport = 0f;
                franceLast.FlowBalance = 0.03f * ngdp;
                var cappedDecision = new PolicyDecision();
                AiFinanceMinistry.Written cappedWritten = AiFinanceMinistry.Apply(france, franceLast, cappedDecision);
                float capped = Released(france, cappedDecision, out int cappedNeg, out int cappedPos, out float cappedRates);
                // review 4's N5: the release the boundary records (`Country.EuReleasedShareOfLines` takes `Written.ReleasedShareOfLines`) is what was restored over the lines
                float linesTotal = 0f; foreach (SpendingLine line in france.SpendingLines) { linesTotal += line.Amount; }
                float restoredShare = linesTotal > 0f ? Sum(france, cappedDecision, out int _, out int _) / linesTotal : 0f;
                if (Mathf.Abs(cappedWritten.ReleasedShareOfLines - restoredShare) > 1e-5f) { Debug.LogError($"FINMIN: the release recorded for next year's excess ({cappedWritten.ReleasedShareOfLines:F6} of the lines) is not what was restored over them ({restoredShare:F6})."); ok = false; }
                if (cappedNeg > 0 || Mathf.Abs(cappedRates) > 1e-4f * ngdp || Mathf.Abs(capped / ngdp * 100f - 1.0f) > 0.02f) { Debug.LogError($"FINMIN: France at 50 % debt and a 3 % surplus released {capped / ngdp * 100f:F3} % of GDP - the release cap is one point a year ({cappedRates / ngdp * 100f:F3} on rates, {cappedNeg} cuts)."); ok = false; }
                // (11) K271 (review 3's M2): LAST YEAR'S RELEASE IS NOT OVERSPENDING - every line planted three points above the net-expenditure benchmark over last
                // year; with no release on record the excess is those three points, with a release of three points on record it is none
                float target = france.CurrencyZone != null ? france.CurrencyZone.InflationTarget : TaylorRule.DefaultInflationTarget;
                float benchmark = (1f + france.PotentialGrowthRate / 100f) * (1f + target / 100f) - 1f;
                var savedLastYear = new List<float>(); foreach (SpendingLine line in france.SpendingLines) { savedLastYear.Add(line.LastYearAmount); line.LastYearAmount = line.Amount / (1f + benchmark + 0.03f); }
                float savedReleased = france.EuReleasedShareOfLines;
                france.EuReleasedShareOfLines = 0f;
                float excessNone = AiFinanceMinistry.EuExpenditureExcess(france);
                france.EuReleasedShareOfLines = 0.03f;
                float excessReleased = AiFinanceMinistry.EuExpenditureExcess(france);
                france.EuReleasedShareOfLines = savedReleased;
                for (int i = 0; i < france.SpendingLines.Count; i++) { france.SpendingLines[i].LastYearAmount = savedLastYear[i]; }
                if (Mathf.Abs(excessNone - 0.03f) > 1e-3f || excessReleased > 1e-3f) { Debug.LogError($"FINMIN: the net-expenditure excess read {excessNone:F4} with no release on record and {excessReleased:F4} with a release of 0.03 - last year's release must be left out (K271)."); ok = false; }
                report.Add($"France at 50 % and a 3 % surplus → {capped / ngdp * 100f:F3} % released (the one-point cap); lines three points over the benchmark → an excess of {excessNone:F4} with no release on record, {excessReleased:F4} with 0.03");
                france.State.GovernmentDebt = savedDebt; franceLast.FlowBalance = savedBalance; france.DebtRatioLastReport = savedLast;
                report.Add($"France at 50 % debt: a 1 % surplus with its income tax two points above the seed → {bigGiven / ngdp * 100f:F3} % on the rates alone, {smallPoints} points above → {given / ngdp * 100f:F3} % of GDP released ({rateCut / ngdp * 100f:F3} on the rate, taken to its seed, then {givenPos} restores); every line but one pinned, a 2.9 % deficit → {guarded.TaxRateOverrides.Count} rate rise(s), a 4 % deficit → {consolidation.TaxRateOverrides.Count}; a deficit at the 1 % limit → nothing; a 2 % deficit → {closedNeg} cuts worth {closed / ngdp * 100f:F3} %, no rate; a 4 % deficit → {edpNeg} cuts worth {taken / ngdp * 100f:F3} % and {edp.TaxRateOverrides.Count} rate rise(s); at 80 % falling two points with balanced books → {fastFall / ngdp * 100f:F3} % released");
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
            Debug.Log(ok ? "FINMIN: PASS - nothing owed nothing written, an adjustment owed cuts then rates, below 60 % the 1 % limit the resting point from either side (the raised rates lowered first, no rate raised), above 60 % a fall faster than the pace released, the United States on its trigger and its discretionary lines only, both directions, the caller's objects withdrawn, the player never read." : "FINMIN: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>K251: a tax line's rate on the country (0 where it has none).</summary>
        private static float RateOf(Country c, TaxType type) { foreach (TaxLine t in c.TaxLines) { if (t.Type == type) { return t.Rate; } } return 0f; }

        /// <summary>K251: what a release gives back, nominal - the lines restored (<see cref="Sum"/>) plus each lowered rate's points on its base; the part on the rates in <paramref name="onRates"/>.</summary>
        private static float Released(Country c, PolicyDecision d, out int negatives, out int positives, out float onRates)
        {
            onRates = 0f;
            foreach (var kv in d.TaxRateOverrides) { float now = RateOf(c, kv.Key); if (kv.Value < now) { onRates += TaxBases.Base(c, kv.Key) * (now - kv.Value) / 100f; } }
            return Sum(c, d, out negatives, out positives) + onRates;
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
