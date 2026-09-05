using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.Data
{
    /// <summary>
    /// P5-C7 (2026-09-05, late) - DEPARTMENT EFFECTIVENESS, the mechanic, built on Design's board 9d (D15 item 4): per portfolio,
    /// effectiveness = allocated ÷ requested × the minister's efficiency, where the REQUEST is P5-B2's driver-indexed line (the amount
    /// every line carries after IndexSpendingLines and before the player's figure is applied) and the ALLOCATION is the player's figure
    /// (P5-B5) as the turn resolves it. Unity is the baseline: below it the ministry is underfunded, at or above it "met" - not a verdict.
    /// It prints as a ratio with two decimals, never a score. No money lives here (the two amounts are on the Budget row); the ratio and
    /// its two factors are what the card and the row read, and what the families read for that portfolio's outcomes (the health family
    /// reads HealthSocialAffairs for its waits and quality).
    /// </summary>
    public sealed class PortfolioEffectiveness
    {
        public float Requested;      // the indexed lines' sum this turn, nominal
        public float Allocated;      // the resolved lines' sum this turn, nominal
        public float Efficiency = 1f; // the minister's efficiency, 0..1 (CabinetMinister.Efficiency / 100)
        public bool Recorded;

        public float AllocationRatio => Requested > 0f ? Allocated / Requested : 1f;
        public float Ratio => AllocationRatio * Efficiency;
    }

    public static class Effectiveness
    {
        /// <summary>Which ministry reads a spending line - the table the mechanic runs on. [AUTHORED-DRAFT]: the portfolios are P2-5's six and
        /// the lines are the book's; the assignment is by the line's subject, stated here in one place. A line no ministry reads (pensions, debt
        /// interest, municipal grants, transport, energy, housing, the rest) returns null and its row prints no ratio (9d).</summary>
        public static CabinetPortfolio? PortfolioOf(SpendingCategory category)
        {
            switch (category)
            {
                case SpendingCategory.HealthcareAndSocialCare:
                case SpendingCategory.Medicare:
                case SpendingCategory.Medicaid:
                case SpendingCategory.HHSDiscretionary:
                case SpendingCategory.SicknessAndDisability:
                case SpendingCategory.FamilyAndChildren:
                case SpendingCategory.SocialPrograms:
                    return CabinetPortfolio.HealthSocialAffairs;
                case SpendingCategory.Defense:
                case SpendingCategory.VeteransAffairsDiscretionary:
                case SpendingCategory.VeteransBenefitsMandatory:
                    return CabinetPortfolio.Defense;
                case SpendingCategory.Education:
                case SpendingCategory.StudentAid:
                    return CabinetPortfolio.Education;
                case SpendingCategory.Justice:
                case SpendingCategory.HomelandSecurity:
                case SpendingCategory.Migration:
                case SpendingCategory.IntegrationAndEquality:
                    return CabinetPortfolio.InteriorJustice;
                case SpendingCategory.StateForeignAffairs:
                case SpendingCategory.InternationalAid:
                    return CabinetPortfolio.ForeignAffairs;
                case SpendingCategory.TreasuryOps:
                case SpendingCategory.FinancialAdministration:
                case SpendingCategory.TaxAdministration:
                case SpendingCategory.CentralGovernment:
                case SpendingCategory.Administration:
                    return CabinetPortfolio.FinanceTreasury;
                default:
                    return null;
            }
        }

        /// <summary>The short word the Budget row's caption prints for a portfolio (9d: PORTFOLIO · EFF ×r).</summary>
        public static string ShortName(CabinetPortfolio portfolio)
        {
            switch (portfolio)
            {
                case CabinetPortfolio.HealthSocialAffairs: return "HEALTH";
                case CabinetPortfolio.Defense: return "DEFENCE";
                case CabinetPortfolio.Education: return "EDUCATION";
                case CabinetPortfolio.InteriorJustice: return "INTERIOR";
                case CabinetPortfolio.ForeignAffairs: return "FOREIGN";
                case CabinetPortfolio.FinanceTreasury: return "TREASURY";
                default: return portfolio.ToString().ToUpperInvariant();
            }
        }

        /// <summary>The minister's efficiency as a 0..1 factor; 1 when the chair is empty (no ministry to multiply by - the ratio is the allocation alone).</summary>
        public static float EfficiencyOf(Country country, CabinetPortfolio portfolio)
        {
            if (country.CabinetMinisters != null && country.CabinetMinisters.TryGetValue(portfolio, out CabinetMinister m) && m != null)
            {
                return Mathf.Clamp(m.Efficiency, 1f, 100f) / 100f;
            }
            return 1f;
        }

        /// <summary>Records this turn's requests and allocations per portfolio. <paramref name="requestedByCategory"/> is every line's amount after
        /// the index and before the player's figure (SimulationManager.ResolveSpendingForTurn captures it); the allocation is the line's amount now.</summary>
        public static void Record(Country country, Dictionary<SpendingCategory, float> requestedByCategory)
        {
            // A FRESH dictionary of fresh records, assigned - never mutated in place: the preview clone carries a shallow copy of the dictionary, and
            // mutating the shared record objects from the preview's turn wrote the preview's (minister-less) ratio onto the real card (the first 9d film
            // read ×1.00 above a decomposition of × 0.70). Assigning a new dictionary on the clone leaves the real country's records untouched.
            var fresh = new Dictionary<CabinetPortfolio, PortfolioEffectiveness>();
            foreach (SpendingLine line in country.SpendingLines)
            {
                CabinetPortfolio? p = PortfolioOf(line.Category);
                if (p == null) { continue; }
                if (!fresh.TryGetValue(p.Value, out PortfolioEffectiveness e)) { e = new PortfolioEffectiveness(); fresh[p.Value] = e; }
                e.Requested += requestedByCategory != null && requestedByCategory.TryGetValue(line.Category, out float requested) ? requested : line.Amount;
                e.Allocated += line.Amount;
                e.Recorded = true;
            }
            foreach (KeyValuePair<CabinetPortfolio, PortfolioEffectiveness> kv in fresh) { kv.Value.Efficiency = EfficiencyOf(country, kv.Key); }
            country.Effectiveness = fresh;
        }

        /// <summary>The portfolio's effectiveness ratio, or 1 × efficiency before the first turn has recorded one (the seed: allocated = requested).</summary>
        public static float RatioOf(Country country, CabinetPortfolio portfolio)
        {
            if (country.Effectiveness != null && country.Effectiveness.TryGetValue(portfolio, out PortfolioEffectiveness e) && e.Recorded) { return e.Ratio; }
            return EfficiencyOf(country, portfolio);
        }

        /// <summary>The largest departure from unity across the cabinet - the arrow's scale on the card (9d: "against the largest departure in the Cabinet").</summary>
        public static float LargestDeparture(Country country)
        {
            float max = 0f;
            foreach (CabinetPortfolio p in System.Enum.GetValues(typeof(CabinetPortfolio))) { max = Mathf.Max(max, Mathf.Abs(RatioOf(country, p) - 1f)); }
            return max;
        }
    }
}
