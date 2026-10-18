using System.Collections.Generic;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Simulation;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// AF-9 (Elias's ruling q9, 2026-10-18: "fund terms go to the vote; ... the budget gates move into the model"): the harnesses' two hands on a
    /// budget bill. The budget window is now the manager's refusal (`SimulationManager.IntroduceBudgetBill`), so a harness that introduces a budget off
    /// the calendar PLANTS the window first - the instrument's hand, as ConstructiveVoteDiagnostic's (e) plants it - and takes the plant back where the
    /// bill is refused for another reason, so the manager's state is what it was. And a bill's fund block is weighed by the chamber
    /// (`ParliamentSystem.FundTermsOf`) and applied whole: a bill built new names no fund, which on a country holding one reads, and applies, as a
    /// dissolution - so a harness bill meant to leave the fund alone restates it (<see cref="RestatingFund"/>), as the Budget page's unchanged draft does.
    /// </summary>
    internal static class BudgetBillHarness
    {
        private static readonly FieldInfo ProcessField = typeof(SimulationManager).GetField("_pendingBudgetProcessByCountry", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>The bill introduced through the manager's own verb with the window planted where it is not open; false, and the plant taken back,
        /// where the manager refuses it.</summary>
        internal static bool Introduce(SimulationManager sim, CountryId id, BudgetBill bill)
        {
            var open = ProcessField?.GetValue(sim) as HashSet<CountryId>;
            bool planted = open != null && open.Add(id);
            bool introduced = sim.IntroduceBudgetBill(id, bill);
            if (!introduced && planted) { open.Remove(id); }
            return introduced;
        }

        /// <summary>Whether the window is open, planting it where it is not; the caller takes it back with <see cref="Unplant"/>.</summary>
        internal static bool Plant(SimulationManager sim, CountryId id) => ProcessField?.GetValue(sim) is HashSet<CountryId> open && open.Add(id);

        /// <summary>Takes a planted window back.</summary>
        internal static void Unplant(SimulationManager sim, CountryId id) { if (ProcessField?.GetValue(sim) is HashSet<CountryId> open) { open.Remove(id); } }

        /// <summary><paramref name="bill"/> with its fund block set to the fund as it stands on <paramref name="country"/> - none where none stands - so the
        /// bill moves no fund term (the AI government's bill does the same, `AiFinanceMinistry.AsBudgetBill`). Returns the bill.</summary>
        internal static BudgetBill RestatingFund(Country country, BudgetBill bill)
        {
            SovereignWealthFund fund = country.SovereignWealthFund;
            bill.SwfShouldExist = fund != null;
            if (fund != null)
            {
                bill.SwfContributionRatePercent = fund.ContributionRatePercent;
                bill.SwfDomesticAllocationPercent = fund.DomesticAllocationPercent;
                bill.SwfEquitiesWeight = fund.EquitiesWeight;
                bill.SwfBondsWeight = fund.BondsWeight;
                bill.SwfInfrastructureWeight = fund.InfrastructureWeight;
                bill.SwfRealEstateWeight = fund.RealEstateWeight;
            }
            return bill;
        }
    }
}
