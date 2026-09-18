using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Data.Generated;

namespace PoliSim.Simulation
{
    /// <summary>
    /// P6-F2's first decision (2026-09-18, §539): BUILD AND RETIRE - capacity by technology as a thing the player changes, with a lead
    /// time and a connection queue. An order is MW of one technology placed in a year: it joins the queue and CONNECTS in its online year
    /// (the year placed plus the technology's lead time - EIA, Annual Energy Outlook 2023, "Cost and Performance Characteristics of New
    /// Generating Technologies", Table 1) or, for a retirement, LEAVES after a year's notice. On landing, the order moves the country's
    /// fleet DELTA for that technology, and every reader of the fleet sees the record plus the delta through <see cref="EnergyLayer.CapacityMw"/>:
    /// the fossil categories' dependable capacity, the page's capacity bar and the utilisation - and the must-run levels of nuclear, wind and
    /// solar scale with it in <c>EnergyMarket.MustRun</c>, so new capacity runs as the seed fleet ran (its own block profile) and retired
    /// capacity takes its output with it.
    ///
    /// <para><b>What this cut does not do, stated on the row.</b> It PRICES NOTHING: IRENA's Renewable Power Generation Costs in 2024 is the
    /// CAPEX source and it is BILLED (the feature list's F2 row, `ERRANDS.md`), so no money moves on an order and the cost row reads BILLED -
    /// the decision stands as MW, a year and a queue until the costs land. It orders only a technology the seed fleet already runs (the record
    /// holds its block profile; a technology the fleet lacks has no sourced profile to run at), never hydro (the water, not the turbine, is the
    /// limit) and never "other" (no lead-time row, no single technology). AI states never order: their fleets are the seed's, and the no-policy
    /// century is unchanged.</para>
    ///
    /// <para><b>State and the derived table.</b> The queue is state on the Country (<see cref="Country.FleetOrders"/>: saved, cloned for the
    /// preview). The delta is DERIVED from the landed orders and rebuilt from state - at every year boundary for the country that advanced, on
    /// load for every country, and cleared on a new world - so it never carries across worlds and the market and the page read one figure.</para>
    /// </summary>
    public static class EnergyFleet
    {
        /// <summary>One order in the connection queue: a technology (an index of <c>EnergyLayerData.Labels</c>), MW (positive builds, negative retires), the year placed and the year it connects or leaves.</summary>
        [Serializable]
        public sealed class Order
        {
            public int Technology;
            public double Mw;
            public int OrderedYear;
            public int OnlineYear;
            public bool Landed;

            public Order Clone() => (Order)MemberwiseClone();
        }

        /// <summary>
        /// Lead time by technology, years, in the labels' order (coal, gas, nuclear, hydro, wind, solar, other): EIA AEO2023 Table 1 - ultra-supercritical
        /// coal 4, combined cycle 3, nuclear (light water reactor) 6, conventional hydropower 4, onshore wind 3, solar PV with tracking 2. Hydro carries its
        /// row but is not orderable here (<see cref="CanOrder"/>); "other" has no row.
        /// </summary>
        public static readonly int[] LeadTimeYears = { 4, 3, 6, 4, 3, 2, 0 };
        public const string LeadTimeSource = "EIA AEO2023 · COST AND PERFORMANCE CHARACTERISTICS OF NEW GENERATING TECHNOLOGIES · TABLE 1";
        /// <summary>The same citation at the width a row's source line has at 1280 (the full one is in the plate's foot).</summary>
        public const string LeadTimeSourceShort = "EIA AEO2023 · NEW GENERATING TECHNOLOGIES · TABLE 1";

        /// <summary>A retirement's notice, years - CONVENTION, stated on the row: no statute names one, the market's own step is the year, and a
        /// retirement placed in a year leaves at the next boundary. Not sourced; the row says so.</summary>
        public const int RetirementNoticeYears = 1;

        /// <summary>The cost row's word, verbatim on the page: the first cut prices nothing and says so.</summary>
        public const string CapexBill = "IRENA'S RENEWABLE POWER GENERATION COSTS IN 2024 IS THE CAPEX SOURCE AND IT IS BILLED · NO MONEY MOVES ON AN ORDER · THE DECISION STANDS AS MW, A YEAR AND A QUEUE UNTIL THE COSTS LAND";

        /// <summary>CONVENTION - the two labels an order may not name, by their index in <c>EnergyLayerData.Labels</c> (coal, gas, nuclear, hydro, wind, solar, other).</summary>
        private const int Hydro = 3, Other = 6;

        /// <summary>True for a technology an order may name: one with a lead-time row, not hydro, not "other", and one the seed fleet runs.</summary>
        public static bool CanOrder(CountryId id, int technology)
        {
            if (technology < 0 || technology >= LeadTimeYears.Length || technology == Hydro || technology == Other || LeadTimeYears[technology] <= 0) { return false; }
            return EnergyLayer.RecordCapacityMw(id, technology) > 0.0;
        }

        /// <summary>Why a technology cannot be ordered, for the row - null where it can.</summary>
        public static string CannotOrderWhy(CountryId id, int technology)
        {
            if (technology == Hydro) { return "THE WATER, NOT THE TURBINE, IS THE LIMIT"; }
            if (technology == Other) { return "NO SINGLE TECHNOLOGY, NO LEAD-TIME ROW"; }
            if (EnergyLayer.RecordCapacityMw(id, technology) <= 0.0) { return "THE SEED FLEET RUNS NONE · NO PROFILE TO RUN AT"; }
            return null;
        }

        /// <summary>The order step for a country, MW: one per cent of the seed fleet, rounded to 100 MW, never under 100.</summary>
        public static double StepMw(CountryId id)
        {
            double total = 0;
            for (int l = 0; l < EnergyLayerData.Labels.Length; l++) { total += EnergyLayer.RecordCapacityMw(id, l); }
            return Math.Max(100.0, Math.Round(total * 0.01 / 100.0) * 100.0);
        }

        /// <summary>
        /// Places an order: <paramref name="mw"/> positive builds, negative retires (capped at what the fleet will have once the queue's
        /// retirements land, so the fleet is never ordered below zero). Returns the order, or null where nothing could be placed.
        /// </summary>
        public static Order Place(Country country, int technology, double mw, int year)
        {
            if (country == null || !CanOrder(country.Id, technology) || Math.Abs(mw) < 1e-9) { return null; }
            if (mw < 0)
            {
                double have = EnergyLayer.CapacityMw(country.Id, technology) + QueuedMw(country, technology, retirementsOnly: true);
                if (have <= 0) { return null; }
                if (have + mw < 0) { mw = -have; }
            }
            var order = new Order
            {
                Technology = technology, Mw = mw, OrderedYear = year,
                OnlineYear = year + (mw > 0 ? LeadTimeYears[technology] : RetirementNoticeYears), Landed = false,
            };
            (country.FleetOrders ?? (country.FleetOrders = new List<Order>())).Add(order);
            return order;
        }

        /// <summary>Withdraws an order that has not landed. False where it had.</summary>
        public static bool Withdraw(Country country, Order order)
        {
            if (country?.FleetOrders == null || order == null || order.Landed) { return false; }
            return country.FleetOrders.Remove(order);
        }

        /// <summary>The MW still in the queue for a technology (not landed): builds and retirements together, or the retirements alone.</summary>
        public static double QueuedMw(Country country, int technology, bool retirementsOnly = false)
        {
            double sum = 0;
            if (country?.FleetOrders == null) { return 0; }
            foreach (Order o in country.FleetOrders)
            {
                if (o.Landed || o.Technology != technology) { continue; }
                if (retirementsOnly && o.Mw > 0) { continue; }
                sum += o.Mw;
            }
            return sum;
        }

        /// <summary>The queue in the order placed - pending first, then landed - for the page.</summary>
        public static IEnumerable<Order> Queue(Country country)
        {
            if (country?.FleetOrders == null) { yield break; }
            foreach (Order o in country.FleetOrders) { if (!o.Landed) { yield return o; } }
            foreach (Order o in country.FleetOrders) { if (o.Landed) { yield return o; } }
        }

        /// <summary>How many orders have not landed.</summary>
        public static int PendingCount(Country country)
        {
            int n = 0;
            if (country?.FleetOrders != null) { foreach (Order o in country.FleetOrders) { if (!o.Landed) { n++; } } }
            return n;
        }

        /// <summary>
        /// The year boundary's step for one country: every order whose online year has come lands, and the country's delta row is rebuilt
        /// from its landed orders. Returns how many landed. Called before the year's dispatch (<c>EnergyLedger.AdvanceYear</c>), so the
        /// year clears on the fleet the orders made.
        /// </summary>
        public static int Advance(Country country, int year)
        {
            int landed = 0;
            if (country?.FleetOrders != null)
            {
                foreach (Order o in country.FleetOrders) { if (!o.Landed && o.OnlineYear <= year) { o.Landed = true; landed++; } }
            }
            RebuildCountry(country);
            return landed;
        }

        // ---- the derived table --------------------------------------------------------------------------------------------------

        private static double[][] _delta;

        /// <summary>The landed orders' MW for a technology, this world - zero where none landed or the country is not in the layer.</summary>
        public static double DeltaMw(CountryId id, int label)
        {
            if (_delta == null) { return 0.0; }
            int i = EnergyLayer.Index(id);
            return i < 0 || label < 0 || label >= _delta[i].Length ? 0.0 : _delta[i][label];
        }

        /// <summary>The fleet's scale for a resource-driven technology: (record + delta) over the record - one at the seed, one where the record is empty.</summary>
        public static double Scale(CountryId id, int label)
        {
            double record = EnergyLayer.RecordCapacityMw(id, label);
            return record > 0 ? Math.Max(0.0, (record + DeltaMw(id, label)) / record) : 1.0;
        }

        /// <summary>Rebuilds one country's delta row from its landed orders.</summary>
        public static void RebuildCountry(Country country)
        {
            if (country == null) { return; }
            int i = EnergyLayer.Index(country.Id);
            if (i < 0) { return; }
            EnsureTable();
            var row = new double[EnergyLayerData.Labels.Length];
            if (country.FleetOrders != null)
            {
                foreach (Order o in country.FleetOrders) { if (o.Landed && o.Technology >= 0 && o.Technology < row.Length) { row[o.Technology] += o.Mw; } }
            }
            _delta[i] = row;
        }

        /// <summary>Rebuilds every country's row - on load, so a loaded queue's landed orders are in the fleet before the first frame.</summary>
        public static void Rebuild(IEnumerable<Country> countries)
        {
            Reset();
            if (countries == null) { return; }
            foreach (Country c in countries) { RebuildCountry(c); }
        }

        /// <summary>Clears the table - a new world starts with the record's fleet, whatever the last world ordered.</summary>
        public static void Reset() { _delta = null; }

        private static void EnsureTable()
        {
            if (_delta != null) { return; }
            _delta = new double[EnergyLayerData.Countries.Length][];
            for (int i = 0; i < _delta.Length; i++) { _delta[i] = new double[EnergyLayerData.Labels.Length]; }
        }
    }
}
