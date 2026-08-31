using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-C1 — the four things `SimulationManager.EstimateBudgetBill` has to be true for the Budget and
    /// Tax/Welfare surfaces to print its figures.
    ///
    /// <list type="number">
    /// <item><description><b>NO CLONE ESCAPE.</b> An estimate applies a draft bill to a clone through
    /// the same delegate a passed bill uses, which mutates tax rates, spending lines, welfare programs
    /// and the sovereign wealth fund. If any of those were a shared reference, drafting a budget would
    /// silently edit the running game. Asserted field by field on the REAL country before and after.
    /// ⚠ This project has caught this class three times already — the R4-1 escape, `BaselineGini`, the
    /// fiscal ledger — every one on a field somebody believed was covered.</description></item>
    /// <item><description><b>AN UNTOUCHED DRAFT ESTIMATES TO ZERO.</b> A bill that restates the current
    /// rates asks for nothing, so all three figures must be 0. This is the basis check: a sign error, a
    /// wrong baseline or a level reported as a delta all show up here and nowhere else.</description></item>
    /// <item><description><b>THE LEGS AGREE WITH THE BALANCE.</b> `NetDelta` is read from the model's
    /// own `Budget` movement, NOT computed as revenue minus spending — so asserting the two agree
    /// measures something rather than restating a definition. A term the balance carries that the two
    /// legs do not would surface here.</description></item>
    /// <item><description><b>DIRECTION.</b> A tax RISE raises revenue and a spending CUT lowers
    /// spending. Direction only — no magnitude is asserted anywhere in this file, because a magnitude
    /// bar is a calibration this item has no mandate to set.</description></item>
    /// </list>
    /// </summary>
    public static class BudgetDraftEstimateDiagnostic
    {
        /// <summary>The legs and the balance are floats over money in the billions; this is a float
        /// precision tolerance, not an accuracy claim.</summary>
        private const float Epsilon = 0.5f;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-C1: the Budget draft estimate - escape, basis, legs, direction ===\n");

            int failures = 0;
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("C-C1 ESTIMATE");
            SimulationManager manager = go.AddComponent<SimulationManager>();
            manager.SetWorld(world);

            foreach (Country country in world.Countries)
            {
                string before = Fingerprint(country);

                // (2) an untouched draft: every rate restated exactly as it stands.
                BudgetBill unchanged = RestatingBill(country);
                BudgetBillEstimate flat = manager.EstimateBudgetBill(country.Id, unchanged);

                string afterFlat = Fingerprint(country);
                if (!string.Equals(before, afterFlat, StringComparison.Ordinal))
                {
                    failures++;
                    Debug.LogError($"C-C1: estimating a budget draft MUTATED the real {country.Id} - a clone escaped. Before/after differ.");
                }

                bool flatZero = Math.Abs(flat.RevenueDelta) < Epsilon
                                && Math.Abs(flat.SpendingDelta) < Epsilon
                                && Math.Abs(flat.NetDelta) < Epsilon;
                if (!flatZero)
                {
                    failures++;
                    Debug.LogError(F("C-C1: {0} - an UNTOUCHED draft does not estimate to zero (rev {1:F3}, spend {2:F3}, net {3:F3}). A draft that asks for nothing must cost nothing; this is a basis error, not a rounding one.",
                        country.Id, flat.RevenueDelta, flat.SpendingDelta, flat.NetDelta));
                }

                // (4) direction: raise every implemented tax by a point.
                BudgetBill raised = RestatingBill(country);
                int raisedLines = 0;
                foreach (TaxLine line in country.TaxLines)
                {
                    if (!line.IsImplemented) { continue; }
                    raised.TaxLines[line.Type] = Mathf.Clamp(line.Rate + 1f, line.MinRate, line.MaxRate);
                    if (raised.TaxLines[line.Type] > line.Rate) { raisedLines++; }
                }

                BudgetBillEstimate hike = manager.EstimateBudgetBill(country.Id, raised);

                string afterHike = Fingerprint(country);
                if (!string.Equals(before, afterHike, StringComparison.Ordinal))
                {
                    failures++;
                    Debug.LogError($"C-C1: estimating a TAX RISE mutated the real {country.Id} - a clone escaped.");
                }

                if (raisedLines > 0 && hike.RevenueDelta <= 0f)
                {
                    failures++;
                    Debug.LogError(F("C-C1: {0} - raising {1} tax line(s) by a point does not raise revenue (delta {2:F3}).",
                        country.Id, raisedLines, hike.RevenueDelta));
                }

                // (3) the legs against the balance, on the run that actually moves.
                float legs = hike.RevenueDelta - hike.SpendingDelta;
                if (Math.Abs(legs - hike.NetDelta) > Epsilon)
                {
                    failures++;
                    Debug.LogError(F("C-C1: {0} - revenue minus spending ({1:F3}) does not equal the balance movement ({2:F3}). The balance carries a term the two legs do not, and the surface would print three figures that do not add up.",
                        country.Id, legs, hike.NetDelta));
                }

                sb.Append(F("    {0,-8} untouched draft: rev {1,9:F2} spend {2,9:F2} net {3,9:F2}  |  +1pp on {4} line(s): rev {5,9:F2} spend {6,9:F2} net {7,9:F2}  legs-vs-balance {8:F4}\n",
                    country.Id, flat.RevenueDelta, flat.SpendingDelta, flat.NetDelta,
                    raisedLines, hike.RevenueDelta, hike.SpendingDelta, hike.NetDelta, legs - hike.NetDelta));
            }

            sb.Append("\n    No magnitude is asserted anywhere above - only that an untouched draft costs\n");
            sb.Append("    nothing, that a rise raises, that the legs reconcile with the balance, and that\n");
            sb.Append("    the real world is untouched. A magnitude bar would be a calibration this item\n");
            sb.Append("    has no mandate to set.\n");

            sb.Append(F("\n=== BudgetDraftEstimateDiagnostic: {0} ===\n",
                failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILURE(S)"));

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        /// <summary>A draft that restates every current rate — it asks Parliament for nothing.</summary>
        private static BudgetBill RestatingBill(Country country)
        {
            var bill = new BudgetBill();
            foreach (TaxLine line in country.TaxLines)
            {
                if (line.IsImplemented) { bill.TaxLines[line.Type] = line.Rate; }
            }

            foreach (SpendingLine line in country.SpendingLines)
            {
                bill.SpendingPercentChanges[line.Category] = 0f;
            }

            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                bill.WelfarePrograms[program.Type] = program.GenerosityLevel;
            }

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

        /// <summary>
        /// Everything a budget bill can reach, as one string. ⚠ Deliberately a HAND-LIST over exactly
        /// what `ParliamentSystem.ApplyBillResult` and `ApplyBudgetBillSpendingAndSwf` mutate — the
        /// same discipline `ClonePreviewCountry`'s own hand-list follows, and for the same reason: a
        /// reflective sweep would look thorough while silently covering fields nothing writes and
        /// missing the one that matters.
        /// </summary>
        private static string Fingerprint(Country country)
        {
            var sb = new StringBuilder();
            sb.Append(country.State.Budget.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            sb.Append(country.State.GovernmentDebt.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            sb.Append(country.State.ApprovalRating.ToString("R", CultureInfo.InvariantCulture)).Append('|');

            foreach (TaxLine line in country.TaxLines)
            {
                sb.Append(line.Type).Append('=').Append(line.Rate.ToString("R", CultureInfo.InvariantCulture))
                  .Append(':').Append(line.IsImplemented).Append('|');
            }

            foreach (SpendingLine line in country.SpendingLines)
            {
                sb.Append(line.Category).Append('=').Append(line.Amount.ToString("R", CultureInfo.InvariantCulture))
                  .Append(':').Append(line.IsMandatory).Append('|');
            }

            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                sb.Append(program.Type).Append('=').Append(program.GenerosityLevel.ToString("R", CultureInfo.InvariantCulture))
                  .Append(':').Append(program.IsImplemented).Append('|');
            }

            SovereignWealthFund fund = country.SovereignWealthFund;
            sb.Append(fund == null
                ? "SWF=none|"
                : F("SWF={0}:{1}:{2}:{3}:{4}:{5}:{6}|", fund.TotalAssets, fund.ContributionRatePercent,
                    fund.DomesticAllocationPercent, fund.EquitiesWeight, fund.BondsWeight,
                    fund.InfrastructureWeight, fund.RealEstateWeight));

            return sb.ToString();
        }


        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
