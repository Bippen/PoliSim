using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Lightweight bilateral trade + tariff model. Trade volumes are static inputs (not a full
    /// market simulation) - each turn, tariffs and currency strength shrink or boost the effective
    /// value of exports/imports, producing this turn's trade balance (the NX term MacroSystem's
    /// national accounts identity reads for GDP) and tariff revenue.
    ///
    /// Tariff revenue (pass 5, 2026-08-26) is a RECURRING FLOW that reaches the books through the
    /// fiscal path: the figure this system computes at a boundary becomes the coming period's
    /// FiscalPeriod.PlannedTariffRevenue and is accrued day by day inside
    /// SimulationManager.ApplyRevenueAndSpending beside tax revenue, inside the fiscal-reaction
    /// multiplier - so the primary balance, the debt stock, the debt ledger and the Budget
    /// accumulator all read it through the one real path. Before pass 5 this system added the figure
    /// to EconomyState.Budget alone (the cumulative display accumulator), the two-books defect F1
    /// closed for interrupt impacts and CLAUDE.md "What remains dark" recorded for tariffs.
    /// </summary>
    public static class TradeSystem
    {
        /// <summary>
        /// THE WIRED-INERT CONTROL (the couplings precedent). False = the pre-pass-5 books: the
        /// figure is added to the Budget accumulator only and the fiscal path sees nothing - the
        /// plumbing is live at zero, and a no-policy dump must be byte-identical to pre_pass5. True =
        /// the routed books. Held false for the control dump, then flipped and removed in the shipping
        /// commit; the constant exists only so the control is the same code with one bit changed.
        /// </summary>
        public const bool RoutesToTheBooks = false;

        /// <summary>Fraction of a currency-strength deviation from neutral that passes through to export competitiveness.</summary>
        private const float CurrencyStrengthExportSensitivity = 0.5f;

        private const float MinExportCurrencyFactor = 0.5f;
        private const float MaxExportCurrencyFactor = 1.5f;

        /// <summary>
        /// The tariff rate (as a percentage) the importer applies to goods coming from the exporter,
        /// most-specific-wins: the importer's own player-set TradePartner.PlayerTariffOverride for
        /// this specific exporter, if one is set; otherwise zero-ish if both share a trade bloc, the
        /// bloc's common external rate if only the importer is a member, or the importer's own base
        /// tariff rate if it belongs to no bloc. The override lookup is keyed off the importer's OWN
        /// TradePartner link to the exporter (importer.TradePartners, found by CountryId - each side
        /// of a bilateral relationship has its own TradePartner instance, per
        /// WorldFactory.AddBilateralTrade), so it only ever reflects a rate the importer itself set on
        /// its own imports - never a rate the exporter might have set on ITS imports from the
        /// importer, which is an entirely separate TradePartner instance this lookup never touches.
        ///
        /// A consequence worth knowing (pass 5's derivation, recorded): an EU member's own
        /// BaseTariffRate is never reached - every partner it has is either a fellow member (the
        /// bloc's internal rate) or the USA (the bloc's external rate) - so the "General Base Tariff"
        /// dial is inert for the five EU countries; only per-partner overrides move their take.
        /// </summary>
        public static float GetTariffRate(Country importer, Country exporter, List<TradeBloc> tradeBlocs)
        {
            TradePartner link = importer.TradePartners.Find(partner => partner.PartnerId == exporter.Id);
            if (link != null && link.HasPlayerTariffOverride)
            {
                return link.PlayerTariffOverride;
            }

            foreach (var bloc in tradeBlocs)
            {
                if (bloc.IsMember(importer.Id) && bloc.IsMember(exporter.Id))
                {
                    return bloc.InternalTariffRate;
                }
            }

            foreach (var bloc in tradeBlocs)
            {
                if (bloc.IsMember(importer.Id))
                {
                    return bloc.ExternalTariffRate;
                }
            }

            return importer.BaseTariffRate;
        }

        /// <summary>
        /// Export competitiveness multiplier from currency strength: a country with an independent
        /// currency that is stronger than neutral sees its exports dampened (pricier abroad), and a
        /// weaker-than-neutral currency boosts them (cheaper abroad). Shared-currency countries
        /// (e.g. Eurozone members) have no individual currency to price exports off of, so they
        /// always get a neutral factor of 1.
        /// </summary>
        private static float GetExportCurrencyFactor(Country exporter, World world)
        {
            if (CurrencySystem.SharesCurrencyZoneWithOthers(exporter, world))
            {
                return 1f;
            }

            float deviation = (exporter.State.CurrencyStrength - CurrencySystem.NeutralCurrencyStrength) / 100f;
            float factor = 1f - deviation * CurrencyStrengthExportSensitivity;
            return Mathf.Clamp(factor, MinExportCurrencyFactor, MaxExportCurrencyFactor);
        }

        /// <summary>
        /// The tariff revenue one period of this country's imports yields at the rates in force:
        /// the sum over its links of ImportVolume x the rate it charges that partner. A PURE reading
        /// of state - the seed fiscal period is planned from it before any turn runs, and
        /// ApplyTradeEffects reports the same figure at a boundary, so the two can never disagree.
        /// Imports themselves are static: a tariff on them raises revenue without shrinking the flow
        /// (the model has no import elasticity, no price pass-through and no retaliation - a stated
        /// placeholder property, recorded with its consequences in CLAUDE.md "Pass 5 ships").
        /// </summary>
        public static float ComputeTariffRevenue(Country country, World world)
        {
            float tariffRevenue = 0f;
            foreach (var link in country.TradePartners)
            {
                Country partner = world.GetCountry(link.PartnerId);
                if (partner == null)
                {
                    continue;
                }

                tariffRevenue += link.ImportVolume * (GetTariffRate(country, partner, world.TradeBlocs) / 100f);
            }

            return tariffRevenue;
        }

        /// <summary>
        /// Applies every trade-partner link for one country this turn: tariffs on our exports
        /// (charged by the partner) and our own currency strength both dampen or boost how much of
        /// that export demand actually lands; tariffs on our imports (charged by us) generate
        /// government revenue. Sets TradeBalance (the NX term MacroSystem's national accounts
        /// identity reads as this turn's net exports). Does not touch GDP directly -
        /// MacroSystem.ApplyNationalAccounts owns that. Returns the period's tariff revenue
        /// (ComputeTariffRevenue) so SimulationManager can plan the coming period's flow from it and
        /// record it; under the pre-pass-5 books (RoutesToTheBooks false) it is instead added to the
        /// Budget accumulator here, the old two-books behaviour the control dump reproduces.
        /// </summary>
        public static float ApplyTradeEffects(Country country, World world)
        {
            float netTradeBalance = 0f;
            float exportCurrencyFactor = GetExportCurrencyFactor(country, world);

            foreach (var link in country.TradePartners)
            {
                Country partner = world.GetCountry(link.PartnerId);
                if (partner == null)
                {
                    continue;
                }

                float tariffOnOurExports = GetTariffRate(partner, country, world.TradeBlocs);

                float effectiveExports = link.ExportVolume * (1f - tariffOnOurExports / 100f) * exportCurrencyFactor;
                float effectiveImports = link.ImportVolume;

                netTradeBalance += effectiveExports - effectiveImports;
            }

            float tariffRevenue = ComputeTariffRevenue(country, world);
            country.State.TradeBalance = netTradeBalance;
            if (!RoutesToTheBooks)
            {
                country.State.Budget += tariffRevenue;
            }

            return tariffRevenue;
        }
    }
}
