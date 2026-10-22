using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// THE AI FINANCE MINISTRY (2026-09-07, COMPLETED.md §387 the page, §388 the build). A country the player does not govern answers its own closed balance
    /// and its debt ratio with the player's own levers - a percentage on each spending line (PolicyDecision.SpendingLineChanges, clamped by the model's own
    /// ranges) and a tax rate (TaxRateOverrides, within the line's range) - under a sourced rule - the Union's for the five EU Member States; for the United States
    /// the debt limit, which is not this class's (below). Nothing here can do what the player cannot; nothing here reads the player's country.
    ///
    /// <para><b>The five (TFEU Art. 126 binds every Member State):</b> the reference values of Protocol No 12 - a deficit of 3 % of GDP, a debt of 60 % - and the
    /// 2024 reform (Regulation (EU) 2024/1263, read in the European Parliament's Fact Sheet <i>The EU framework for fiscal policies</i>): net expenditure as the
    /// single operational indicator; the debt sustainability safeguard (debt falls by at least 1 point a year above 90 %, 0.5 between 60 and 90); the deficit
    /// resilience margin of 1.5 % of GDP reached by at least 0.4 points a year; and in the corrective arm a minimum annual structural adjustment of 0.5 % of GDP
    /// while the deficit exceeds 3 %. Below the 60 % reference the Fiscal Compact (TSCG Art. 3, in the same Fact Sheet) is the rule: a balanced budget, the
    /// structural deficit no worse than 1 % of GDP where the debt is below 60 % - a surplus is given back to the lines, a deficit beyond the limit is closed to it.</para>
    ///
    /// <para><b>The United States</b> (PS-6 US-24; R-US21 (v) as ruled, I5: "the dollar limit replaces the AI ministry's ratio-trend logic and its borrowed
    /// sequester for the USA"): the ministry writes nothing for the USA. Its rule is the federal debt limit (`UsDebtLimit`) - an AI-governed USA raises it when
    /// its extraordinary measures run out (R-US21 (vi)), and where a raise is not law the game's cut of spending to revenue is the day's flows'
    /// (`UsDebtLimit.CutToday`), not a decision's. Retired with it: the ratio-trend trigger (the ratio above its seed and rising two years running), the
    /// Fiscal Responsibility Act of 2023 § 101 cap-growth excess it carried, and the sequester's size borrowed from the EU benchmark (§387, §390) - the
    /// measurement R-US21 was asked on reconstructs the trigger from the memory below (`UsDebtLimitCheck`).</para>
    ///
    /// <para><b>Three corrections from the first measurements (§388):</b> the law asks that the debt ratio FALL at the safeguard's pace, so a state already
    /// falling that fast owes nothing more that year; a cut to a line is a level the indexation never gives back, so the golden rule's release is the other
    /// side; and the ministry writes into the decision its caller hands it, which a caller may hand it again next turn - so what it wrote is WITHDRAWN after
    /// the turn (Withdraw), or year 2's cuts would be re-applied for a century and every later restore skipped for a key already present, which is exactly
    /// what the trace of the first three builds showed. The net-expenditure cap reads LAST year's growth of the lines (a one-year control account).</para>
    /// </summary>
    public static class AiFinanceMinistry
    {
        /// <summary>SOURCED - Protocol No 12 TFEU: the deficit reference value, % of GDP (EP Fact Sheet, the EU framework for fiscal policies).</summary>
        public const float EuDeficitReferencePercent = 3f;
        /// <summary>SOURCED - Protocol No 12 TFEU: the debt reference value, % of GDP.</summary>
        public const float EuDebtReferencePercent = 60f;
        /// <summary>SOURCED - Regulation (EU) 2024/1263, the debt sustainability safeguard's upper band, % of GDP.</summary>
        public const float EuHighDebtPercent = 90f;
        /// <summary>SOURCED - Regulation (EU) 2024/1264 (the corrective arm): the minimum annual structural adjustment while the deficit exceeds 3 %, % of GDP.</summary>
        public const float EdpAdjustmentPercentOfGdp = 0.5f;
        /// <summary>SOURCED - the debt sustainability safeguard above 90 % of GDP: the ratio falls by at least one point a year (Regulation 2024/1263). Read here on the HEADLINE balance, ruled to stay (§390): the law reads the structural balance, and the deviation is recorded until T-3 gives the model an output-gap-adjusted balance.</summary>
        public const float DebtSafeguardHighPointsPerYear = 1.0f;
        /// <summary>SOURCED - the debt sustainability safeguard between 60 and 90 % of GDP: at least half a point a year (Regulation 2024/1263).</summary>
        public const float DebtSafeguardMidPointsPerYear = 0.5f;
        /// <summary>SOURCED - the deficit resilience margin, % of GDP (Regulation 2024/1263).</summary>
        public const float ResilienceMarginPercent = 1.5f;
        /// <summary>SOURCED - the minimum annual adjustment toward the resilience margin, points of GDP a year (Regulation 2024/1263; 0.25 with an extended period, not modelled). On the headline balance, as the safeguards above (§390).</summary>
        public const float ResilienceAdjustmentPercentOfGdp = 0.4f;
        /// <summary>SOURCED - TSCG Art. 3 (the Fiscal Compact, EP Fact Sheet): the balanced budget below 60 % of debt, % of GDP - the balance a surplus is released toward.</summary>
        public const float EuReleaseTargetBalancePercent = 0f;
        /// <summary>SOURCED - TSCG Art. 3: the lower limit on the structural deficit where the debt is below 60 % of GDP (1 %; 0.5 % otherwise), % of GDP - a deficit beyond it is closed back to it.</summary>
        public const float EuLowDebtDeficitLimitPercent = 1.0f;
        /// <summary>CONVENTION - a rate rise below this many points is not written (noise).</summary>
        private const float MinRatePoints = 0.01f;

        /// <summary>What the ministry wrote into a decision this turn, so the caller can withdraw it after the turn.</summary>
        public sealed class Written
        {
            public readonly List<SpendingCategory> Lines = new List<SpendingCategory>();
            public readonly List<TaxType> Taxes = new List<TaxType>();
            public bool Any => Lines.Count > 0 || Taxes.Count > 0;
        }

        /// <summary>TFEU Art. 126 binds every Member State; of the six, only the United States is outside the Union.</summary>
        public static bool IsEuMember(CountryId id) => id != CountryId.USA;

        /// <summary>The debt-ratio memory: shift last year's ratio into the year before and record today's. Called once per turn for an AI country, AFTER Decide
        /// (the EU rule reads the year's fall from it; for the USA it is kept, and read only by the measurement R-US21 was asked on).</summary>
        public static void Observe(Country country)
        {
            if (country.DebtRatioSeed <= 0f) { country.DebtRatioSeed = country.State.DebtToGdpRatio; }   // a save from before the ministry: its seed is the first ratio it sees
            country.DebtRatioReportBefore = country.DebtRatioLastReport;
            country.DebtRatioLastReport = country.State.DebtToGdpRatio;
        }

        /// <summary>The adjustment of the balance the EU rule asks this year, % of GDP (positive = tighten, negative = release): the EDP benchmark while the deficit
        /// exceeds 3 %; above 60 %, the debt safeguard's pace less the ratio's fall over the last year, or the resilience adjustment while the deficit exceeds its
        /// margin, whichever is larger; at or below 60 %, the Fiscal Compact - a surplus is released toward balance, a deficit beyond 1 % is closed back to 1 %; otherwise zero.</summary>
        public static float EuRequiredAdjustmentPercentOfGdp(float balancePercent, float debtPercent, float fallOverLastYear)
        {
            if (balancePercent < -EuDeficitReferencePercent) { return EdpAdjustmentPercentOfGdp; }
            if (debtPercent <= EuDebtReferencePercent)
            {
                if (balancePercent > EuReleaseTargetBalancePercent) { return -(balancePercent - EuReleaseTargetBalancePercent); }
                if (balancePercent < -EuLowDebtDeficitLimitPercent) { return -EuLowDebtDeficitLimitPercent - balancePercent; }
                return 0f;
            }
            float pace = debtPercent > EuHighDebtPercent ? DebtSafeguardHighPointsPerYear : DebtSafeguardMidPointsPerYear;
            float safeguardShortfall = Mathf.Clamp(pace - fallOverLastYear, 0f, pace);
            float resilience = balancePercent < -ResilienceMarginPercent ? ResilienceAdjustmentPercentOfGdp : 0f;
            return Mathf.Max(safeguardShortfall, resilience);
        }

        /// <summary>Last year's growth of the lines above the net-expenditure benchmark (real potential growth plus the zone's target inflation), as a share of the lines; zero where the lines grew within it or have no last year.</summary>
        public static float EuExpenditureExcess(Country country)
        {
            float target = country.CurrencyZone != null ? country.CurrencyZone.InflationTarget : TaylorRule.DefaultInflationTarget;
            float benchmark = (1f + country.PotentialGrowthRate / 100f) * (1f + target / 100f) - 1f;
            float now = 0f, lastYear = 0f;
            foreach (SpendingLine line in country.SpendingLines) { if (line.LastYearAmount > 0f) { now += line.Amount; lastYear += line.LastYearAmount; } }
            if (lastYear <= 0f) { return 0f; }
            return Mathf.Max(0f, now / lastYear - 1f - benchmark);
        }

        /// <summary>
        /// PS-3e (§632): THE MINISTRY'S RULE AS A BILL. What <see cref="Decide"/> wrote - the lines' percent changes and the two household rates - in the
        /// budget bill's own slots, so the chamber votes on it and <c>ParliamentSystem.ApplyBillResult</c> applies it the day it is adopted. The fund
        /// rides unchanged (the bill's SWF block always runs: a bill that says no fund would dissolve one), welfare and the pension age stay off the bill.
        /// </summary>
        public static BudgetBill AsBudgetBill(Country country, PolicyDecision decision)
        {
            var bill = new BudgetBill { GovernmentBill = true };
            foreach (KeyValuePair<SpendingCategory, float> kv in decision.SpendingLineChanges) { bill.SpendingPercentChanges[kv.Key] = kv.Value; }
            foreach (KeyValuePair<TaxType, float> kv in decision.TaxRateOverrides) { bill.TaxLines[kv.Key] = kv.Value; }
            SovereignWealthFund fund = country.SovereignWealthFund;
            bill.SwfShouldExist = fund != null;
            if (fund != null)
            {
                bill.SwfContributionRatePercent = fund.ContributionRatePercent; bill.SwfDomesticAllocationPercent = fund.DomesticAllocationPercent;
                bill.SwfEquitiesWeight = fund.EquitiesWeight; bill.SwfBondsWeight = fund.BondsWeight; bill.SwfInfrastructureWeight = fund.InfrastructureWeight; bill.SwfRealEstateWeight = fund.RealEstateWeight;
            }
            return bill;
        }

        /// <summary>A fresh decision carrying only what the ministry writes - pure: reads the country and last year's report, changes nothing.</summary>
        public static PolicyDecision Decide(Country country, FiscalTurnReport lastReport)
        {
            var decision = new PolicyDecision();
            Apply(country, lastReport, decision);
            return decision;
        }

        /// <summary>Writes the ministry's levers into <paramref name="decision"/> where it carries none of its own for that line or tax, and returns what it wrote so the
        /// caller can Withdraw it after the turn. Pure with respect to the country. The ratio's fall over the last year is read from the memory Observe keeps.</summary>
        public static Written Apply(Country country, FiscalTurnReport lastReport, PolicyDecision decision)
        {
            var written = new Written();
            if (lastReport == null || country.SpendingLines == null || country.SpendingLines.Count == 0) { return written; }
            float nominalGdp = country.State.NominalGdp;
            if (nominalGdp <= 0f) { return written; }
            float balancePercent = lastReport.FlowBalance / nominalGdp * 100f;   // negative = deficit, nominal over nominal (P5-B6); AF-17 (M2): the flow, net of the year's one-offs - a rule judges the standing position
            float debtPercent = country.State.DebtToGdpRatio;
            float fall = country.DebtRatioLastReport > 0f ? country.DebtRatioLastReport - debtPercent : 0f;
            if (IsEuMember(country.Id)) { ApplyEuRule(country, balancePercent, debtPercent, fall, nominalGdp, decision, written); }   // PS-6 US-24 (R-US21 (v)): the USA writes nothing - its rule is the debt limit (`UsDebtLimit`)
            return written;
        }

        /// <summary>Removes what Apply wrote, so a decision object a caller hands in again next turn carries nothing of this turn's ministry.</summary>
        public static void Withdraw(PolicyDecision decision, Written written)
        {
            if (decision == null || written == null) { return; }
            foreach (SpendingCategory c in written.Lines) { decision.SpendingLineChanges.Remove(c); }
            foreach (TaxType t in written.Taxes) { decision.TaxRateOverrides.Remove(t); }
        }

        private static void ApplyEuRule(Country country, float balancePercent, float debtPercent, float fall, float nominalGdp, PolicyDecision decision, Written written)
        {
            float adjustmentPercentOfGdp = EuRequiredAdjustmentPercentOfGdp(balancePercent, debtPercent, fall);
            if (adjustmentPercentOfGdp == 0f) { return; }   // compliant: nothing owed, nothing to give back
            float linesNow = 0f;
            foreach (SpendingLine line in country.SpendingLines) { linesNow += line.Amount; }
            if (adjustmentPercentOfGdp < 0f)
            {
                RestoreLinesUniformly(country, -adjustmentPercentOfGdp / 100f * nominalGdp, linesNow, decision, written);
                return;
            }
            // the net-expenditure cap: last year's excess growth of the lines over the benchmark is taken back while something is owed above 60 %
            float excess = debtPercent > EuDebtReferencePercent || balancePercent < -EuDeficitReferencePercent ? EuExpenditureExcess(country) : 0f;
            float toTake = excess * linesNow + adjustmentPercentOfGdp / 100f * nominalGdp;
            if (toTake <= 0f) { return; }
            float shortfall = CutLinesUniformly(country, toTake, linesNow, includeMandatory: true, decision, written);
            if (shortfall > 0f) { RaiseHouseholdRates(country, shortfall, decision, written); }
        }

        /// <summary>One uniform percentage across the lines in scope, clamped as the applier will clamp it; returns the nominal amount the clamps leave untaken.</summary>
        private static float CutLinesUniformly(Country country, float toTake, float scopeTotal, bool includeMandatory, PolicyDecision decision, Written written)
        {
            if (scopeTotal <= 0f) { return toTake; }
            float percent = toTake / scopeTotal * 100f;
            float taken = 0f;
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (!includeMandatory && line.IsMandatory) { continue; }
                if (line.Pinned || decision.SpendingLineChanges.ContainsKey(line.Category)) { continue; }
                float range = line.IsMandatory ? SimulationManager.MandatoryPercentChangeRangeForRules : SimulationManager.DiscretionaryPercentChangeRangeForRules;
                float applied = Mathf.Min(percent, range);
                if (applied <= 0f) { continue; }
                decision.SpendingLineChanges[line.Category] = -applied;
                written.Lines.Add(line.Category);
                taken += line.Amount * applied / 100f;
            }
            return Mathf.Max(0f, toTake - taken);
        }

        /// <summary>The release side: a surplus given back to every line as one uniform percentage, clamped as the applier will clamp it (the seed band underneath).</summary>
        private static void RestoreLinesUniformly(Country country, float toGive, float scopeTotal, PolicyDecision decision, Written written)
        {
            if (scopeTotal <= 0f || toGive <= 0f) { return; }
            float percent = toGive / scopeTotal * 100f;
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (line.Pinned || decision.SpendingLineChanges.ContainsKey(line.Category)) { continue; }
                float range = line.IsMandatory ? SimulationManager.MandatoryPercentChangeRangeForRules : SimulationManager.DiscretionaryPercentChangeRangeForRules;
                float applied = Mathf.Min(percent, range);
                if (applied <= 0f) { continue; }
                decision.SpendingLineChanges[line.Category] = applied;
                written.Lines.Add(line.Category);
            }
        }

        /// <summary>The remainder on the household rates - the income tax and VAT raised by the same number of points, which spreads it in proportion to their bases.</summary>
        private static void RaiseHouseholdRates(Country country, float shortfall, PolicyDecision decision, Written written)
        {
            float baseTotal = 0f;
            foreach (TaxLine line in country.TaxLines) { if (line.IsImplemented && (line.Type == TaxType.IncomeTax || line.Type == TaxType.VAT)) { baseTotal += TaxBases.Base(country, line.Type); } }
            if (baseTotal <= 0f) { return; }
            float points = shortfall / baseTotal * 100f;
            if (points < MinRatePoints) { return; }
            foreach (TaxLine line in country.TaxLines)
            {
                if (!line.IsImplemented || (line.Type != TaxType.IncomeTax && line.Type != TaxType.VAT) || decision.TaxRateOverrides.ContainsKey(line.Type)) { continue; }
                decision.TaxRateOverrides[line.Type] = Mathf.Min(line.MaxRate, line.Rate + points);
                written.Taxes.Add(line.Type);
            }
        }
    }
}
