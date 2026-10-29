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
    /// while the deficit exceeds 3 %. Below the 60 % reference the Fiscal Compact (TSCG Art. 3, in the same Fact Sheet) is the rule: the structural deficit no
    /// worse than 1 % of GDP where the debt is below 60 % (0.5 % otherwise). K251 (2026-10-29): that limit is the rule's RESTING POINT below 60 % - a surplus or a
    /// smaller deficit is released down to it, a larger one closed back to it - and above 60 % a ratio falling faster than the safeguard's pace releases the
    /// excess, no further than the 0.5 % limit; a release lowers the household rates the rule raised before it gives lines back, and the rates rise only while
    /// the Pact requires consolidation (the debt above 60 % or the deficit beyond 3 %). The safeguards are read in Regulation (EU) 2024/1263 Arts. 7-8 and the EDP
    /// benchmark in Regulation (EU) 2024/1264, both fetched byte-exact (`ElectionsData/rules/raw/k251_eu_rule/`); the TSCG's own text is BILLED there.</para>
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
        /// <summary>SOURCED (DECLARED, not fetched) - TSCG Art. 3(1)(b) and (d), as the EP Fact Sheet reads it: the lower limit on the structural deficit where the debt
        /// is below 60 % of GDP, % of GDP. K251 (2026-10-29): it is the resting point at or below 60 % - a smaller deficit or a surplus is released down to it, a
        /// larger deficit closed back to it. The Treaty's own text was not reached (the Publications Office Cellar has no CELEX for it, the Council's PDF answers
        /// 403 to a script): BILLED, `ElectionsData/rules/raw/k251_eu_rule/README.md`.</summary>
        public const float EuLowDebtDeficitLimitPercent = 1.0f;
        /// <summary>SOURCED (DECLARED, not fetched) - TSCG Art. 3(1)(b): the lower limit on the structural deficit otherwise (the debt above 60 % of GDP), % of GDP. K251:
        /// above 60 % a ratio falling faster than the safeguard's pace releases its excess no further than this. BILLED as the limit above. DECLARED (review 2's
        /// L-N3): this stop is STRICTER than the Regulation alone, whose deficit resilience safeguard (2024/1263 Art. 8) is met at a structural deficit of 1.5 %;
        /// the rule takes the TSCG's 0.5 % because 2024/1263 recital (20) carries "the substance of Title III (Fiscal Compact) of the TSCG" into Union law.
        /// Whether Title III binds Sweden and Poland (non-euro parties, bound by declaration) is to be read when the Treaty's text is fetched.</summary>
        public const float EuHighDebtDeficitLimitPercent = 0.5f;
        /// <summary>[AUTHORED-DRAFT] (K251, 2026-10-29, review 2's M-N1) - the share of the gap to the Fiscal Compact's limit the rule moves in a year where it
        /// steers TO a limit (the release above 60 %, the resting point below it): a half-life of one year, DECLARED. Moving the whole gap in a year (the first
        /// build) overshot - a release shows in the next balance with a gain above one (the net-expenditure cap takes the restored lines back on top of what is
        /// owed), so the balance swung into the 3 % breach from either side. The EDP benchmark and the safeguards are paces of their own and are not scaled.</summary>
        public const float EuGapShareClosedPerYear = 0.5f;
        /// <summary>[AUTHORED-DRAFT], CALIBRATED - chosen by measurement, not read off a source (K251, review 2's M-N1; K271; review 3's M2): the most a release gives
        /// back in a year, % of GDP. It is set equal to the debt safeguard's pace above 90 % (<see cref="DebtSafeguardHighPointsPerYear"/>) - one point a year
        /// - because that was the figure measured to work, not because the Regulation sets a release pace (it sets none). Measured out of the engine
        /// (`Tools/headless_hold/frf_play.sh`, the all-AI world, a century, two seeds): with no cap the balance swung into the 3 % breach; at half a point
        /// a consolidation's overshoot came back so slowly that Germany ran to a net-creditor trough of some -25 %; at one point all five settle above
        /// zero but for Germany's trough of -2.67 % on one seed (424242), with the release left out of the net-expenditure excess (K271).</summary>
        public const float EuReleaseCapPercentOfGdp = DebtSafeguardHighPointsPerYear;
        /// <summary>CONVENTION - a rate rise below this many points is not written (noise).</summary>
        private const float MinRatePoints = 0.01f;

        /// <summary>What the ministry wrote into a decision this turn, so the caller can withdraw it after the turn.</summary>
        public sealed class Written
        {
            public readonly List<SpendingCategory> Lines = new List<SpendingCategory>();
            public readonly List<TaxType> Taxes = new List<TaxType>();
            /// <summary>K271: what the release restored on the lines, as a share of the lines it read - the caller keeps it (`Country.EuReleasedShareOfLines`) so next
            /// year's net-expenditure excess leaves it out.</summary>
            public float ReleasedShareOfLines;
            public bool Any => Lines.Count > 0 || Taxes.Count > 0;
        }

        /// <summary>TFEU Art. 126 binds every Member State; of the six, only the United States is outside the Union.</summary>
        public static bool IsEuMember(CountryId id) => id != CountryId.USA;

        /// <summary>K251 (2026-10-29, review 2's L-N1): the debt-ratio memory CLEARED at a boundary the player governs, so the AI taking office reads no fall on
        /// its first turn (the memory empty reads as none: the safeguard owes its whole pace) instead of a player spell's whole change as one year's - which the
        /// release above 60 % would have given away. The seed ratio stays.</summary>
        public static void Forget(Country country)
        {
            country.DebtRatioReportBefore = 0f;
            country.DebtRatioLastReport = 0f;
            country.EuReleasedShareOfLines = 0f;   // K271: nor a release of its own to leave out
        }

        /// <summary>The debt-ratio memory: shift last year's ratio into the year before and record today's. Called once per turn for an AI country, AFTER Decide
        /// (the EU rule reads the year's fall from it; for the USA it is kept, and read only by the measurement R-US21 was asked on).</summary>
        public static void Observe(Country country)
        {
            if (country.DebtRatioSeed <= 0f) { country.DebtRatioSeed = country.State.DebtToGdpRatio; }   // a save from before the ministry: its seed is the first ratio it sees
            country.DebtRatioReportBefore = country.DebtRatioLastReport;
            country.DebtRatioLastReport = country.State.DebtToGdpRatio;
        }

        /// <summary>The adjustment of the balance the EU rule asks this year, % of GDP (positive = tighten, negative = release): the EDP benchmark while the deficit
        /// exceeds 3 %; above 60 %, the debt safeguard's pace less the ratio's fall over the last year (Regulation 2024/1263 Art. 7), or the resilience adjustment
        /// while the deficit exceeds its margin (Art. 8), whichever is larger; at or below 60 %, the Fiscal Compact's limit as the resting point - the balance
        /// moved to a 1 % deficit from either side.
        /// <para>K251, review 2's M-N1: a release moves <see cref="EuGapShareClosedPerYear"/> of its gap a year, no more than <see cref="EuReleaseCapPercentOfGdp"/>;
        /// a deficit beyond the 1 % limit below 60 % is still closed back in one year, by lines alone.</para>
        /// <para>K251 (2026-10-29, the review's H1): THE RULE HAS A STOP. Above 60 % a ratio already falling faster than the safeguard's pace, with the deficit inside
        /// its margin, releases the excess of its fall over the pace - the cuts the rule made are not permanent - no further than the 0.5 % limit above 60 %; below
        /// 60 % a surplus or a smaller deficit is released down to the 1 % limit, no longer to a balanced budget. Before, the rule gave nothing back above 60 % and
        /// released only to balance below it, so its fixed point was zero debt (the record's K251 section). The safeguard is read on the headline balance (§390).</para></summary>
        public static float EuRequiredAdjustmentPercentOfGdp(float balancePercent, float debtPercent, float fallOverLastYear)
        {
            if (balancePercent < -EuDeficitReferencePercent) { return EdpAdjustmentPercentOfGdp; }
            float gap = -EuLowDebtDeficitLimitPercent - balancePercent;   // positive: a deficit beyond the limit; negative: room above it
            if (debtPercent <= EuDebtReferencePercent) { return gap >= 0f ? gap : -Mathf.Min(-gap * EuGapShareClosedPerYear, EuReleaseCapPercentOfGdp); }   // a deficit beyond the limit closed back to it (lines only, no rate - below); a release damped and capped (K251, review 2's M-N1)
            float pace = debtPercent > EuHighDebtPercent ? DebtSafeguardHighPointsPerYear : DebtSafeguardMidPointsPerYear;
            float safeguardShortfall = Mathf.Clamp(pace - fallOverLastYear, 0f, pace);
            float resilience = balancePercent < -ResilienceMarginPercent ? ResilienceAdjustmentPercentOfGdp : 0f;
            float owed = Mathf.Max(safeguardShortfall, resilience);
            if (owed > 0f) { return owed; }
            float room = balancePercent + EuHighDebtDeficitLimitPercent;   // how far the balance stands above the 0.5 % limit
            return room > 0f ? -Mathf.Min(Mathf.Min(fallOverLastYear - pace, room) * EuGapShareClosedPerYear, EuReleaseCapPercentOfGdp) : 0f;
        }

        /// <summary>Last year's growth of the lines above the net-expenditure benchmark (real potential growth plus the zone's target inflation), as a share of the lines; zero where the lines grew within it or have no last year.</summary>
        public static float EuExpenditureExcess(Country country)
        {
            float target = country.CurrencyZone != null ? country.CurrencyZone.InflationTarget : TaylorRule.DefaultInflationTarget;
            float benchmark = (1f + country.PotentialGrowthRate / 100f) * (1f + target / 100f) - 1f;
            float now = 0f, lastYear = 0f;
            foreach (SpendingLine line in country.SpendingLines) { if (line.LastYearAmount > 0f) { now += line.Amount; lastYear += line.LastYearAmount; } }
            if (lastYear <= 0f) { return 0f; }
            return Mathf.Max(0f, now / lastYear - 1f - benchmark - country.EuReleasedShareOfLines);   // K271: last year's release is the rule's own, not overspending
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
        public static Written Apply(Country country, FiscalTurnReport lastReport, PolicyDecision decision) =>
            lastReport == null ? new Written() : Apply(country, lastReport.FlowBalance, decision);

        /// <summary>K251 (2026-10-29, review 2's M-N1): the rule on a closed year's flow balance, nominal. THE BOUNDARY HANDS IN THE YEAR IT IS CLOSING
        /// (`SimulationManager`'s fiscal period, its accrued balance, before the period's report is written), not the stored report: at the boundary the
        /// stored report is still the year BEFORE, which does not yet carry what the rule wrote a year ago - a release read against it was released twice,
        /// and the rule swung into the 3 % breach it exists to prevent (review 2 measured it). The preview, between boundaries, reads the stored report,
        /// which is then the closed year.</summary>
        public static Written Apply(Country country, float closedFlowBalance, PolicyDecision decision)
        {
            var written = new Written();
            if (country.SpendingLines == null || country.SpendingLines.Count == 0) { return written; }
            float nominalGdp = country.State.NominalGdp;
            if (nominalGdp <= 0f) { return written; }
            float balancePercent = closedFlowBalance / nominalGdp * 100f;   // negative = deficit, nominal over nominal (P5-B6); AF-17 (M2): the flow, net of the year's one-offs - a rule judges the standing position
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
                // K251 (the review's H2): the release lowers the household rates the rule raised - back toward their seeds - before it gives spending back
                float left = LowerHouseholdRates(country, -adjustmentPercentOfGdp / 100f * nominalGdp, decision, written);
                RestoreLinesUniformly(country, left, linesNow, decision, written);
                return;
            }
            // CONSOLIDATION is the Pact's - the debt above 60 % or the deficit beyond 3 %; below 60 % the Fiscal Compact's close-back to its limit cuts lines only (K251)
            bool consolidating = debtPercent > EuDebtReferencePercent || balancePercent < -EuDeficitReferencePercent;
            // the net-expenditure cap: last year's excess growth of the lines over the benchmark is taken back while something is owed above 60 %
            float excess = consolidating ? EuExpenditureExcess(country) : 0f;
            float toTake = excess * linesNow + adjustmentPercentOfGdp / 100f * nominalGdp;
            if (toTake <= 0f) { return; }
            float shortfall = CutLinesUniformly(country, toTake, linesNow, includeMandatory: true, decision, written);
            if (shortfall > 0f && consolidating) { RaiseHouseholdRates(country, shortfall, decision, written); }   // K251 (H2): the rates rise only while the rule requires consolidation
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
                written.ReleasedShareOfLines += line.Amount * applied / 100f / scopeTotal;   // K271
            }
        }

        /// <summary>K251 (2026-10-29, the review's H2): THE RELEASE'S FIRST STEP - the income tax and VAT lowered by the same number of points, each no further than its
        /// seed (`TaxLine.RateSeed`), the release side of <see cref="RaiseHouseholdRates"/>. A rate above its seed in the AI's book is read as the rule's raise:
        /// nothing records who raised it, so a Finance partner's or a former government's raise is given back the same way (DECLARED). Returns the nominal amount
        /// the seeds leave to be released on the lines.</summary>
        private static float LowerHouseholdRates(Country country, float toGive, PolicyDecision decision, Written written)
        {
            float baseTotal = 0f;
            foreach (TaxLine line in country.TaxLines) { if (RaisedHouseholdLine(line) && !decision.TaxRateOverrides.ContainsKey(line.Type)) { baseTotal += TaxBases.Base(country, line.Type); } }
            if (baseTotal <= 0f || toGive <= 0f) { return toGive; }
            float points = toGive / baseTotal * 100f;
            if (points < MinRatePoints) { return toGive; }
            float given = 0f;
            foreach (TaxLine line in country.TaxLines)
            {
                if (!RaisedHouseholdLine(line) || decision.TaxRateOverrides.ContainsKey(line.Type)) { continue; }
                float lowered = Mathf.Min(points, line.Rate - line.RateSeed);
                decision.TaxRateOverrides[line.Type] = Mathf.Max(line.MinRate, line.Rate - lowered);
                written.Taxes.Add(line.Type);
                given += TaxBases.Base(country, line.Type) * lowered / 100f;
            }
            return toGive - given > 1e-4f * toGive ? toGive - given : 0f;   // CONVENTION - a float remainder of the rates' own arithmetic is not a release left for the lines
        }

        /// <summary>An implemented income tax or VAT standing above its seed by at least the noise threshold - a rate the release may lower.</summary>
        private static bool RaisedHouseholdLine(TaxLine line) =>
            line.IsImplemented && (line.Type == TaxType.IncomeTax || line.Type == TaxType.VAT) && line.RateSeed > 0f && line.Rate - line.RateSeed >= MinRatePoints;

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
