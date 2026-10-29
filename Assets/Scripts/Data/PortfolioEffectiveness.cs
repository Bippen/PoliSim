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
        public float Efficiency = 1f; // the minister's efficiency on AF-30's scale (CabinetSystem.EfficiencyScale): 1 the pool's average minister, a vacant chair below the floor's
        /// <summary>Ruled 2026-09-06: THE LEVEL beside the flow - the portfolio's lines against their own seed anchors (SpendingLine.SeedAmount, the seed carried
        /// forward by prices, drivers and the AI's real growth), i.e. spending per indexed unit against its seed. The FLOW (Ratio) says "met this year's
        /// ask"; the LEVEL says "the ask has shrunk" - a cut that stands is met from its second year while its level stays low.</summary>
        public float SeedAnchor;
        public float LevelRatio => SeedAnchor > 0f ? Allocated / SeedAnchor : 1f;
        public bool Recorded;

        public float AllocationRatio => Requested > 0f ? Allocated / Requested : 1f;
        public float Ratio => AllocationRatio * Efficiency;
    }

    public static class Effectiveness
    {
        /// <summary>Which ministry reads a spending line - the table the mechanic runs on. [AUTHORED-DRAFT]: the portfolios are P2-5's six and AF-6's economy portfolio, and
        /// the lines are the book's; the assignment is by the line's subject, stated here in one place. A line no ministry reads (pensions, debt
        /// interest, municipal grants, housing, the rest) returns null and its row prints no ratio (9d).</summary>
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
                // AF-6 (q6): the departments none of the six fits - Agriculture, Transportation, Energy (`Departments`) - and the economy ministry that holds
                // energy where the survey rates no energy post of its own (the US Department of Commerce; the five's business and industry line).
                case SpendingCategory.Agriculture:
                case SpendingCategory.Transportation:
                case SpendingCategory.Energy:
                case SpendingCategory.Commerce:
                case SpendingCategory.BusinessAndIndustry:
                    return CabinetPortfolio.Economy;
                default:
                    return null;
            }
        }

        /// <summary>The portfolio's display name - R4-4's one scoped lookup: never DisplayName.Of/Spaced, which is how "Education" avoids
        /// reference-class-trap instance #4 against SpendingCategory.Education/PolicyNodeId.Education. The simulation's player-visible texts (a
        /// minister alert, a ledger line) read it here, as the UI does, so no raw identifier ("InteriorJustice") reaches the player.</summary>
        public static string DisplayName(CabinetPortfolio portfolio)
        {
            switch (portfolio)
            {
                case CabinetPortfolio.FinanceTreasury: return "Finance & Treasury";
                case CabinetPortfolio.InteriorJustice: return "Interior & Justice";
                case CabinetPortfolio.HealthSocialAffairs: return "Health & Social Affairs";
                case CabinetPortfolio.Defense: return "Defense";
                case CabinetPortfolio.ForeignAffairs: return "Foreign Affairs";
                case CabinetPortfolio.Education: return "Education";
                case CabinetPortfolio.Economy: return "Economy";
                default: return portfolio.ToString();
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
                case CabinetPortfolio.Economy: return "ECONOMY";
                default: return portfolio.ToString().ToUpperInvariant();
            }
        }

        /// <summary>The minister's efficiency as the ratio's factor - AF-30's one scale (<see cref="PoliSim.Simulation.CabinetSystem.EfficiencyScale"/>): the pool's
        /// average minister 1 (neutral), a vacant chair the player's to fill below the pool's floor, an unmodelled cabinet 1.</summary>
        public static float EfficiencyOf(Country country, CabinetPortfolio portfolio) => PoliSim.Simulation.CabinetSystem.EfficiencyScale(country, portfolio);

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
                e.SeedAnchor += line.SeedAmount;   // the level's denominator (ruled 2026-09-06)
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

        /// <summary>AF-30 (the review's M2): the portfolio's allocation ratio alone - allocated / requested, 1 before the first record - so a readout says UNDERFUNDED only
        /// where the money fell short, and names a below-average minister or a vacant chair as what they are.</summary>
        public static float AllocationRatioOf(Country country, CabinetPortfolio portfolio) =>
            country.Effectiveness != null && country.Effectiveness.TryGetValue(portfolio, out PortfolioEffectiveness e) && e.Recorded ? e.AllocationRatio : 1f;

        /// <summary>Ruled 2026-09-06: the portfolio's LEVEL - its lines' amounts against their seed anchors (spending per indexed unit against its own seed); 1 before
        /// the first record. Read beside the flow ratio: the flow says whether this year's ask was met, the level whether the ask itself has shrunk or grown.</summary>
        public static float LevelOf(Country country, CabinetPortfolio portfolio)
        {
            if (country.Effectiveness != null && country.Effectiveness.TryGetValue(portfolio, out PortfolioEffectiveness e) && e.Recorded) { return e.LevelRatio; }
            float amount = 0f, anchor = 0f;
            foreach (SpendingLine line in country.SpendingLines) { if (PortfolioOf(line.Category) == portfolio) { amount += line.Amount; anchor += line.SeedAmount; } }
            return anchor > 0f ? amount / anchor : 1f;
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
