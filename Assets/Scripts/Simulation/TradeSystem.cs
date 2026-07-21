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
    /// </summary>
    public static class TradeSystem
    {
        /// <summary>Fraction of a currency-strength deviation from neutral that passes through to export competitiveness.</summary>
        private const float CurrencyStrengthExportSensitivity = 0.5f;

        private const float MinExportCurrencyFactor = 0.5f;
        private const float MaxExportCurrencyFactor = 1.5f;

        /// <summary>
        /// The tariff rate (as a percentage) the importer applies to goods coming from the exporter:
        /// zero-ish if both share a trade bloc, the bloc's common external rate if only the importer
        /// is a member, or the importer's own base tariff rate if it belongs to no bloc.
        /// </summary>
        public static float GetTariffRate(Country importer, Country exporter, List<TradeBloc> tradeBlocs)
        {
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
        /// Applies every trade-partner link for one country this turn: tariffs on our exports
        /// (charged by the partner) and our own currency strength both dampen or boost how much of
        /// that export demand actually lands; tariffs on our imports (charged by us) generate
        /// government revenue. Sets TradeBalance (the NX term MacroSystem's national accounts
        /// identity reads as this turn's net exports) and adjusts the budget with tariff revenue.
        /// Does not touch GDP directly - MacroSystem.ApplyNationalAccounts owns that. Returns the
        /// tariff revenue collected so callers (e.g. SimulationManager's fiscal report) can show it
        /// without re-deriving it.
        /// </summary>
        public static float ApplyTradeEffects(Country country, World world)
        {
            float netTradeBalance = 0f;
            float tariffRevenue = 0f;
            float exportCurrencyFactor = GetExportCurrencyFactor(country, world);

            foreach (var link in country.TradePartners)
            {
                Country partner = world.GetCountry(link.PartnerId);
                if (partner == null)
                {
                    continue;
                }

                float tariffOnOurExports = GetTariffRate(partner, country, world.TradeBlocs);
                float tariffOnOurImports = GetTariffRate(country, partner, world.TradeBlocs);

                float effectiveExports = link.ExportVolume * (1f - tariffOnOurExports / 100f) * exportCurrencyFactor;
                float effectiveImports = link.ImportVolume;

                netTradeBalance += effectiveExports - effectiveImports;
                tariffRevenue += link.ImportVolume * (tariffOnOurImports / 100f);
            }

            country.State.TradeBalance = netTradeBalance;
            country.State.Budget += tariffRevenue;
            return tariffRevenue;
        }
    }
}
