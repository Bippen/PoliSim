using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Data.Generated;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// MF q26 (Elias's ruling of 2026-10-18, `docs/RULINGS_2026-10-18.md`: <i>"the player's fleet orders meet the AI ministry's reliability check (the grid
    /// operator's statute where one exists, e.g. EnWG §13b). Unserved energy costs output at ACER's VoLL (CEPA 2018)"</i>; K145: <i>"the USA's value of lost load
    /// is LBNL's Interruption Cost Estimate (Sullivan 2015)"</i>) - the audit's GA-6-1: the player retired a whole fleet in a few weeks of presses, the blocks went
    /// unserved at the ceiling, and real GDP did not move.
    ///
    /// <para><b>THE CHECK.</b> A retirement the player orders (<see cref="SimulationManager.PlaceFleetOrder"/>) is refused, with its sentence, where it fails
    /// the AI ministry's own security-of-supply test (<see cref="AiEnergyMinistry.PeakBlockSpareMw"/> - the peak block's net residual against the flexible
    /// fossil fleet, the queue's retirements counted) or where, with the queue's retirements landed and its builds not credited (the ministry's own
    /// "a veto errs toward the lights"), the clearing would leave more energy unserved than it does without the order. The first is the ministry's closed form
    /// for a fossil label; the second is the same question asked of the clearing itself, so it reaches the labels the ministry never retires (nuclear, wind,
    /// solar) and Sweden's chain (MF q27, <see cref="EnergyMarket.SwedenSeedUnbalanceMw"/>). DECLARED: every block of the clearing, not the peak alone - a
    /// must-run technology's retirement can bind outside the peak. Germany's grid operator acts under EnWG § 13b (a closure of a plant the transmission
    /// operator designates system-relevant is barred - Abs. 2 Satz 2, Abs. 4, Abs. 5; `EnergyData/raw/reliability/`), so Germany's refusal cites it; for the
    /// other five no grid operator's statute has been fetched yet, and their refusal is the ministry's own test, said so on the sentence. The citation keeps the
    /// statute's own letters (EnWG § 13b) inside a sentence the Energy page sets in capitals by its style.
    ///
    /// <para><b>DECLARED, for Elias to strike (the q26 review's M3).</b> The clearing test goes further than the ministry's closed form: it reads every label,
    /// every block, at a tolerance of a micro-megawatt. Two consequences follow and are the reading's, not the ruling's words. (1) A retirement the player
    /// presses can never leave load unserved, so the ruling's second sentence - unserved energy costing output at the VoLL - is reached only by an order
    /// queued before this check, a planted fleet, or a future path that moves supply or demand without an order: GA-6-1 is closed by refusal, and the cost
    /// stands behind it. (2) Sweden's demand carries the exogenous exports and its hydro is fixed (MF q27 holds the seed's national balance as the
    /// reference), so every megawatt of Swedish nuclear, wind or solar retired below that balance is refused until builds have landed - where in the world
    /// the first megawatts retired would cut exports, which a later energy stage (imports and exports at neighbours' prices) is to answer.</para>
    ///
    /// <para><b>WHAT UNSERVED ENERGY COSTS.</b> The year's unserved energy (<see cref="EnergyMarket.UnservedGwh"/>) times the NON-HOUSEHOLD share of consumption
    /// times the output value of lost load (<see cref="ValueOfLostLoadData"/>: CEPA's non-domestic VoLL for the EU five, LBNL's medium and large C&amp;I cost per
    /// unserved kWh at 16 hours for the USA, each carried to 2023 - the energy layer's catalog year, the seed's prices) is the output lost - an overlay on real
    /// GDP for the year, the US lapse's form (<see cref="UsLapse.ApplyOutput"/>): applied each day after Okun has read the identity's growth, taken off before
    /// the next day's identity, lifted at the boundary's readings. DECLARED: (i) load is shed across the classes in proportion to consumption, and only the
    /// non-household share loses OUTPUT - the domestic VoLL (CEPA Table G.1, LBNL residential) values lost leisure, which is not GDP; it is carried in the data
    /// and reaches nothing yet; (ii) the loss is held at its 2023 real value (the VoLL does not grow with productivity); (iii) THREE UNITS: a HICP measures
    /// prices in the country's own currency, so CEPA's 2015 euros reach Sweden and Poland through it - to krona or zloty at the ECB's 2015 annual rate, carried
    /// by the national HICP, and back to the market's euro at the energy layer's own 2023 rate (`EnergyLayerData.NationalPerMarketCurrency`, ECB 2023); the euro
    /// three carry theirs directly; (iv) the overlay does not feed Okun's law or the identity - it is output lost while the lights are out, recovered the day
    /// they come back on; (v) for Elias to strike: the USA's output VoLL is LBNL's MEDIUM AND LARGE C&amp;I figure alone - small C&amp;I (258.0 dollars per
    /// unserved kWh at 16 hours in Table ES-1, against 12.7) is left out because the report does not give the two classes' shares of consumption, so the
    /// figure is conservative (K145 names the ICE without choosing a class).</para>
    /// </summary>
    public static class EnergyReliability
    {
        /// <remarks>CONVENTION - a refusal needs more than this many MW of added unserved load; below it two clearings are the same clearing.</remarks>
        public const double ToleranceMw = 1e-6;
        /// <remarks>CONVENTION - the labels by their index in EnergyLayerData.Labels (coal, gas, nuclear, hydro, wind, solar, other) the ministry's closed form reads.</remarks>
        private const int CoalLabel = 0, GasLabel = 1;
        /// <remarks>CONVENTION - GWh × currency per kWh is millions of the currency (10^6 kWh per GWh); over this, billions.</remarks>
        private const double MillionsPerBillion = 1000.0;

        /// <summary>The authority a refusal cites: the grid operator's statute where one is sourced, the ministry's own test otherwise.</summary>
        public static string StatuteOf(CountryId id) => id == CountryId.Germany
            ? "EnWG § 13b · THE TSO BARS CLOSING A SYSTEM-RELEVANT PLANT"
            : "THE ENERGY MINISTRY SECURITY-OF-SUPPLY TEST · NO GRID STATUTE SOURCED";

        // ---- the value of lost load ------------------------------------------------------------------------------------------------------------

        /// <summary>The output value of lost load, the market's currency per kWh at 2023 prices (the seed's): the generated row into the country's own currency at its
        /// year's ECB rate, carried by its own price index, and back to the market's currency at the energy layer's 2023 rate (THREE UNITS; both rates 1 for the
        /// euro three and the USA).</summary>
        public static double OutputVollPerKwh(CountryId id)
        {
            if (!ValueOfLostLoadData.Rows.TryGetValue(id, out var row)) { return 0.0; }
            string market = id == CountryId.USA ? "USD" : "EUR";   // the energy market clears in euro for the five and in dollars for the USA (EnergyLayerData.UsdPerMarketCurrency)
            if (row.Currency != market) { throw new InvalidOperationException("EnergyReliability: the value of lost load for " + id + " is in " + row.Currency + ", the market clears in " + market); }
            return row.OutputPerKwh * row.FxThen * row.Index2023 / row.IndexThen / EnergyLayerData.NationalPerMarketCurrency[EnergyLayer.Index(id)];
        }

        /// <summary>The non-household share of the country's 2023 consumption (EnergyLayerData.RetailConsumptionGwh) - the part of shed load that loses output.</summary>
        public static double NonHouseholdShare(CountryId id)
        {
            int ci = EnergyLayer.Index(id);
            if (ci < 0) { return 0.0; }
            double total = 0.0; for (int c = 0; c < EnergyLedger.ClassCount; c++) { total += EnergyLayerData.RetailConsumptionGwh[ci][c]; }
            return total > 0.0 ? EnergyLayerData.RetailConsumptionGwh[ci][EnergyLedger.NonHouseholds] / total : 0.0;
        }

        /// <summary>The output a clearing's unserved energy costs, billions of the book's dollars a year at the seed's prices: unserved GWh × the non-household
        /// share × the output VoLL, bridged to dollars at the energy layer's rate. Zero where nothing is unserved.</summary>
        public static double OutputLossBillions(CountryId id, EnergyMarket.Result r)
        {
            double gwh = EnergyMarket.UnservedGwh(r);
            if (!(gwh > 0.0) || !EnergyLayer.Has(id)) { return 0.0; }
            return gwh * NonHouseholdShare(id) * OutputVollPerKwh(id) / MillionsPerBillion * EnergyLayerData.UsdPerMarketCurrency[EnergyLayer.Index(id)];
        }

        /// <summary>The boundary's step (called from `EnergyLedger.AdvanceYear` on the year's own clearing): the year's output loss rate, held for the year.</summary>
        public static void SetYear(Country country, EnergyMarket.Result r)
        {
            float rate = (float)OutputLossBillions(country.Id, r);
            if (rate != country.UnservedOutputLossRate) { country.UnservedOutputLossRate = rate; }
        }

        // ---- the overlay on output (the US lapse's form) ------------------------------------------------------------------------------------------

        /// <summary>Yesterday's overlay off before the identity reads GDP - nothing where none is applied. Taken off BEFORE the lapse's (its overlay went on after).</summary>
        public static void RestoreOutput(Country country)
        {
            if (country.UnservedOutputWedge == 0f) { return; }
            country.State.GDP += country.UnservedOutputWedge;
            country.UnservedOutputWedge = 0f;
        }

        /// <summary>Today's overlay, after Okun has read the identity's growth (and after the lapse's): the year's loss rate off the level. Nothing where none.</summary>
        public static void ApplyOutput(Country country)
        {
            float loss = country.UnservedOutputLossRate;
            if (!(loss > 0f)) { return; }
            float before = country.State.GDP;
            country.State.GDP = Mathf.Max(MacroSystem.MinGdp, before - loss);
            country.UnservedOutputWedge = before - country.State.GDP;
        }

        /// <summary>The overlay lifted for a boundary reading that must see the identity's level; <see cref="LowerOverlay"/> puts it back. Nothing where none.</summary>
        public static void LiftOverlay(Country country)
        {
            if (country.UnservedOutputWedge == 0f) { return; }
            country.State.GDP += country.UnservedOutputWedge;
        }

        /// <summary>The overlay put back after <see cref="LiftOverlay"/>.</summary>
        public static void LowerOverlay(Country country)
        {
            if (country.UnservedOutputWedge == 0f) { return; }
            country.State.GDP -= country.UnservedOutputWedge;
        }

        // ---- the check -----------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Why a RETIREMENT of <paramref name="mw"/> (negative) of a technology fails the reliability check, or null where it passes (and for a build, a label the
        /// queue refuses, or nothing left to retire - those are the queue's answers, not this check's). The ministry's closed form first, for a fossil label;
        /// then the clearing with the queue's retirements landed, with and without the order.
        /// </summary>
        public static string RefusalFor(Country country, int technology, double mw)
        {
            if (country == null || !(mw < 0.0) || !EnergyLayer.Has(country.Id) || !EnergyFleet.CanOrder(country.Id, technology)) { return null; }
            double have = EnergyFleet.CapacityMw(country, technology) + EnergyFleet.QueuedMw(country, technology, retirementsOnly: true);
            double retire = Math.Min(-mw, Math.Max(0.0, have));   // EnergyFleet.Place's own cap: never below zero
            if (!(retire > 0.0)) { return null; }
            string name = EnergyLayerData.Labels[technology].ToUpperInvariant();
            if (technology == CoalLabel || technology == GasLabel)
            {
                double spare = AiEnergyMinistry.PeakBlockSpareMw(country, technology, out double residual, out double flexible);
                if (retire > spare + ToleranceMw)
                {
                    return F("{0}: {1} -{2:0} MW REFUSED - THE PEAK BLOCK CAN SPARE {3:0} MW, A NET RESIDUAL OF {4:0} MW AGAINST A FLEXIBLE FOSSIL FLEET OF {5:0} MW WITH THE QUEUE",
                        StatuteOf(country.Id), name, retire, Math.Max(0.0, spare), residual, flexible);
                }
            }
            double without = TrialUnservedMw(country, -1, 0.0), with = TrialUnservedMw(country, technology, -retire);
            if (with > without + ToleranceMw)
            {
                return F("{0}: {1} -{2:0} MW REFUSED - WITH THE QUEUE LANDED IT LEAVES {3:0} MW UNSERVED ACROSS THE BLOCKS, {4:0} MW WITHOUT IT", StatuteOf(country.Id), name, retire, with, without);
            }
            return null;
        }

        /// <summary>The MW a clearing leaves unserved, summed over every zone's blocks, on the country's fleet with every landed order and every queued RETIREMENT
        /// landed (queued builds not credited) plus, where <paramref name="technology"/> is a label, an order of <paramref name="mw"/> landed - at the country's
        /// price level and its ETS probe. The country's own queue is not touched: the trial fleet rides a stand-in through the fleet's scope.</summary>
        public static double TrialUnservedMw(Country country, int technology, double mw)
        {
            var orders = new List<EnergyFleet.Order>();
            if (country.FleetOrders != null)
            {
                foreach (EnergyFleet.Order o in country.FleetOrders)
                {
                    if (o == null || (!o.Landed && o.Mw > 0.0)) { continue; }
                    EnergyFleet.Order landed = o.Clone(); landed.Landed = true; orders.Add(landed);
                }
            }
            if (technology >= 0 && mw != 0.0) { orders.Add(new EnergyFleet.Order { Technology = technology, Mw = mw, Landed = true }); }
            var stand = new Country { Id = country.Id, FleetOrders = orders };
            EnergyMarket.Result r;
            using (EnergyFleet.For(stand)) { r = EnergyMarket.ClearAt(country.Id, Math.Max(0.0001f, country.State.PriceLevel), EnergyMarket.ProbeEtsRiseFor(country.Id)); }
            double unserved = 0.0;
            foreach (EnergyMarket.BlockResult[] zone in r.Zones) { foreach (EnergyMarket.BlockResult block in zone) { unserved += block.UnservedMw; } }
            return unserved;
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
