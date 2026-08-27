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
    ///
    /// Tariffs COST something (pass 6, 2026-08-27 - the three forces in TradeCosts): the rate a
    /// partner charges on our exports now carries a retaliatory term mirroring the excess of our
    /// override on it over the rate we would otherwise charge (GetRetaliatoryTariffRate, read by
    /// GetTariffRate and so by both the export leg below and the partner's own take), and the change
    /// in the take a boundary reports passes through to prices for a year (SimulationManager's
    /// boundary re-plan). Imports stay static - the one placeholder property of the three pass 5
    /// named ("no import elasticity") that survives, with the import-side-symmetry ruling that keeps
    /// it so: C and I are gross of imports in the identity, so a tariff must never shrink
    /// effectiveImports (it would credit reduced leakage as GDP).
    /// </summary>
    public static class TradeSystem
    {
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
        ///
        /// Pass 6 (2026-08-27): this is the importer's OWN rate. The rate everything reads is
        /// GetTariffRate = own + GetRetaliatoryTariffRate, so a partner's resolved rate on our exports
        /// now responds to OUR override on it even though its own link is never written.
        /// </summary>
        public static float GetOwnTariffRate(Country importer, Country exporter, List<TradeBloc> tradeBlocs)
        {
            TradePartner link = importer.TradePartners.Find(partner => partner.PartnerId == exporter.Id);
            if (link != null && link.HasPlayerTariffOverride)
            {
                return link.PlayerTariffOverride;
            }

            return GetStandingTariffRate(importer, exporter, tradeBlocs);
        }

        /// <summary>
        /// The rate the importer would charge the exporter with overrides ignored - steps 2-4 of
        /// GetOwnTariffRate's precedence (shared-bloc internal, importer's bloc external, base rate).
        /// The ONE implementation of that precedence: GetOwnTariffRate delegates here, the
        /// retaliation term measures an override's excess against it, and the Trade bill's direction
        /// reads it with the bill's own base rate substituted (the overload below). No arithmetic in
        /// it, so the split cannot perturb a trajectory.
        /// </summary>
        public static float GetStandingTariffRate(Country importer, Country exporter, List<TradeBloc> tradeBlocs)
        {
            return GetStandingTariffRate(importer, exporter, tradeBlocs, importer.BaseTariffRate);
        }

        /// <summary>The standing precedence with <paramref name="baseTariffRate"/> in place of the importer's own base rate - what a Trade bill proposing that base rate would charge a partner with no override.</summary>
        public static float GetStandingTariffRate(Country importer, Country exporter, List<TradeBloc> tradeBlocs, float baseTariffRate)
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

            return baseTariffRate;
        }

        /// <summary>
        /// PASS 6, THE RETALIATION MIRROR (TradeCosts.RetaliationMirrorFraction): the extra rate the
        /// importer charges the exporter because the EXPORTER holds an override on the importer that
        /// exceeds the rate it would otherwise charge (its standing rate). Computed from the
        /// exporter's own link, never stored - so no AI country's TradePartner is ever written (the
        /// one-directional invariant TradePartner.PlayerTariffOverride states holds at the state
        /// level), the preview clone's links carry it for free, and it lifts the boundary after the
        /// override does. A first cut, recorded as such: instant and memoryless (the 21-day bill delay
        /// is the lag on the player's side; the model has no diplomatic memory to decay), cuts
        /// unanswered (the excess floors at 0 - no reciprocal-liberalization lever appears), and a
        /// base-rate hike unanswered (a country's base rate IS its standing rate, so there is no
        /// excess to mirror - a stated residual). Bound: own + excess &lt; 100 (own &lt;= 50, excess
        /// &lt;= 50 - 0), so effective exports can be driven to zero but never below; at the seeded
        /// rate structure the reachable maximum is 50 (Sweden's 50 on Germany: 0.1 + 49.9), and a
        /// player who first zeroes a non-bloc base rate can push a partner to 53. Cannot recurse:
        /// GetStandingTariffRate never reads an override.
        ///
        /// The inert branch comes first: with the fraction at 0 this returns the literal 0f before
        /// any lookup, so the wired-inert control is the old resolution to the bit.
        /// </summary>
        public static float GetRetaliatoryTariffRate(Country importer, Country exporter, List<TradeBloc> tradeBlocs)
        {
            if (TradeCosts.RetaliationMirrorFraction <= 0f)
            {
                return 0f;
            }

            TradePartner theirLink = exporter.TradePartners.Find(partner => partner.PartnerId == importer.Id);
            if (theirLink == null || !theirLink.HasPlayerTariffOverride)
            {
                return 0f;
            }

            float theirStandingRate = GetStandingTariffRate(exporter, importer, tradeBlocs);
            return TradeCosts.RetaliationMirrorFraction * Mathf.Max(0f, theirLink.PlayerTariffOverride - theirStandingRate);
        }

        /// <summary>
        /// The rate the importer actually charges the exporter: its own rate (GetOwnTariffRate) plus
        /// the retaliatory term the exporter's override on it draws (GetRetaliatoryTariffRate, 0
        /// unless such an override exists). Every reader - the export leg of ApplyTradeEffects, the
        /// take in ComputeTariffRevenue, the Trade tab's partner row, the pass-5 diagnostic - goes
        /// through this one door.
        /// </summary>
        public static float GetTariffRate(Country importer, Country exporter, List<TradeBloc> tradeBlocs)
        {
            return GetOwnTariffRate(importer, exporter, tradeBlocs) + GetRetaliatoryTariffRate(importer, exporter, tradeBlocs);
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
        /// (the model has no import elasticity - the one placeholder property that survives pass 6;
        /// the retaliatory term the rate carries makes a country that mirrors an override collect
        /// the mirrored take too, and pass 6's price pass-through reads the CHANGE in this figure
        /// between two boundaries, so a country that retaliates pays its own price - recorded in
        /// CLAUDE.md "Pass 6 ships").
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
        /// (charged by the partner - since pass 6 including whatever it mirrors of our override on
        /// it) and our own currency strength both dampen or boost how much of that export demand
        /// actually lands; tariffs on our imports (charged by us) generate government revenue. Sets
        /// TradeBalance (the NX term MacroSystem's national accounts identity reads as this turn's
        /// net exports). Does not touch GDP directly - MacroSystem.ApplyNationalAccounts owns that.
        /// Returns the period's tariff revenue (ComputeTariffRevenue) so SimulationManager can plan
        /// the coming period's flow from it; nothing is booked here - the Budget accumulator reads
        /// the take through the fiscal path's budgetBalance (the wired-inert control that held the
        /// old accumulator write, ad82104, was byte-identical to pre_pass5 6/6 before the switch was
        /// flipped and removed). Both quantities are planned from ONE rate state at ONE boundary, so
        /// an override's revenue and its retaliation always land in the same period.
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

            country.State.TradeBalance = netTradeBalance;
            return ComputeTariffRevenue(country, world);
        }
    }
}
