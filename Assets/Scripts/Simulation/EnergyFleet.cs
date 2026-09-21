using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Data.Generated;

namespace PoliSim.Simulation
{
    /// <summary>
    /// P6-F2's first decision (2026-09-18, §539; re-founded 2026-09-21, §544): BUILD AND RETIRE - capacity by technology as a thing the player changes,
    /// with a lead time and a connection queue. An order is MW of one technology placed in a turn: it joins the country's queue and CONNECTS after the
    /// technology's lead time (EIA, Annual Energy Outlook 2023, "Cost and Performance Characteristics of New Generating Technologies", Table 1) or, for a
    /// retirement, LEAVES after a year's notice - counted in TURNS, the boundaries the order waits through, because the game's year boundary is 365 days
    /// and drifts across the calendar's leap years (an order's printed year is the year it serves; its landing is the turn).
    ///
    /// <para><b>The fleet is the COUNTRY's, not the process's</b> (§544's review, its first finding). A country's fleet is the 2023 record plus ITS OWN landed
    /// orders, read off <see cref="Country.FleetOrders"/> every time - there is no table. §539 kept one static table keyed by the country's id, and the game
    /// holds several worlds at once: the shadow baseline's countries (no orders) rebuilt the player's row to zero after every turn, every ledger fork reset
    /// it, and a load restored it in one place only. The market's readers are keyed by id and run deep, so the country reaches them through a SCOPE:
    /// <see cref="For"/> names the country whose orders an id-keyed read sees until it is disposed, and every entry point that holds a Country opens one
    /// (<c>EnergyMarket.Clear</c>, <c>EnergyLedger.Compute</c>, the ministry, the page). ⚠ An id-keyed read outside any scope sees the RECORD - which is
    /// right for the seed calibrations and the catalogue's checks, and is the reason a reader that holds a Country should use <see cref="CapacityMw(Country,int)"/>.</para>
    ///
    /// <para><b>What this cut does not do, stated on the row.</b> It PRICES NOTHING: IRENA's Renewable Power Generation Costs in 2024 is the CAPEX source and it
    /// is BILLED, so no money moves on an order and the cost row reads BILLED. It orders only a technology the record's fleet holds and runs: never hydro (the
    /// water, not the turbine, is the limit), never "other" (no lead-time row, no single technology), never a fossil label whose plants the record's fuel
    /// grouping does not hold (France's and Italy's coal sits in Eurostat's multi-fuel groups - 19 and 36 MW under "coal" against 0.5 and 2.1 GW of output -
    /// so a step on that label would scale a fleet that is not there), never nuclear where the law has closed it (Germany).</para>
    /// </summary>
    public static class EnergyFleet
    {
        /// <summary>One order in the connection queue: a technology (an index of <c>EnergyLayerData.Labels</c>), MW (positive builds, negative retires), when it was placed and when it connects or leaves.</summary>
        [Serializable]
        public sealed class Order
        {
            public int Technology;
            public double Mw;
            /// <summary>The year placed and the year it serves from - what the page prints.</summary>
            public int OrderedYear, OnlineYear;
            /// <summary>§544: the turn placed and the boundary it lands at (the turn's number AFTER that boundary) - what <see cref="Advance"/> reads. Zero on an order from before the turn clock (a §539 save), which lands by its year.</summary>
            public int OrderedTurn, OnlineTurn;
            public bool Landed;
            /// <summary>P6-F2d (§544): the sentence that explains an order an AI ministry placed - the statute, the year it was read at, the gap. Null on the player's own orders. Additive save field.</summary>
            public string Reason;

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

        /// <summary>CONVENTION - the labels by their index in <c>EnergyLayerData.Labels</c> (coal, gas, nuclear, hydro, wind, solar, other) that the ordering rules name.</summary>
        private const int CoalLabel = 0, GasLabel = 1, NuclearLabel = 2, HydroLabel = 3, OtherLabel = 6;

        /// <summary>True for a technology an order may name - see <see cref="CannotOrderWhy"/> for each refusal.</summary>
        public static bool CanOrder(CountryId id, int technology) => CannotOrderWhy(id, technology) == null;

        /// <summary>Why a technology cannot be ordered, for the row - null where it can. Read on the RECORD's fleet, so the answer never moves with the queue.</summary>
        public static string CannotOrderWhy(CountryId id, int technology)
        {
            if (technology < 0 || technology >= LeadTimeYears.Length) { return "NOT A TECHNOLOGY OF THE LAYER"; }
            if (technology == HydroLabel) { return "THE WATER, NOT THE TURBINE, IS THE LIMIT"; }
            if (technology == OtherLabel || LeadTimeYears[technology] <= 0) { return "NO SINGLE TECHNOLOGY, NO LEAD-TIME ROW"; }
            if (!EnergyLayer.Has(id)) { return "THE LAYER DOES NOT COVER THIS COUNTRY"; }
            if (EnergyLayer.RecordCapacityMw(id, technology) <= 0.0) { return "THE SEED FLEET RUNS NONE · NO PROFILE TO RUN AT"; }
            if (technology == NuclearLabel && EnergyMarket.NuclearClosedByLaw(id)) { return "CLOSED BY LAW · A BUILT MW WOULD NOT RUN"; }
            if ((technology == CoalLabel || technology == GasLabel) && !EnergyMarket.ClearsFossilFleet(id)) { return "THE ZONES CLEAR WITHOUT A FOSSIL FLEET · NOTHING WOULD MOVE"; }
            if (technology == CoalLabel || technology == GasLabel)
            {
                using (RecordOnly())
                {
                    // the record's fuel grouping must HOLD the plants the dispatch runs: where the category's capacity is made up from its 2023 output instead
                    // (EnergyMarket.DependableSource), the label's MW are a fragment and a step on them would scale the fleet by tens
                    if (EnergyMarket.DependableSource(id, technology == CoalLabel ? EnergyMarket.Coal : EnergyMarket.Gas) != "record") { return "THE RECORD'S FUEL GROUPS DO NOT HOLD THESE PLANTS"; }
                }
            }
            return null;
        }

        /// <summary>Why THIS order cannot be placed - a technology the queue refuses (<see cref="CannotOrderWhy"/>), or a build that would take the connection queue past
        /// what the country's operator publishes (<see cref="EnergyConnectionQueue.RefusalFor"/>, P6-F2e); null where no RULE refuses it - <see cref="Place"/> still places nothing for a
        /// zero order or a retirement with nothing left to retire, which are not refusals and carry no sentence.</summary>
        public static string CannotPlaceWhy(Country country, int technology, double mw)
        {
            if (country == null) { return "NO COUNTRY"; }
            return CannotOrderWhy(country.Id, technology) ?? EnergyConnectionQueue.RefusalFor(country, technology, mw);
        }

        /// <summary>The order step for a country, MW: one per cent of the seed fleet, rounded to 100 MW, never under 100.</summary>
        public static double StepMw(CountryId id)
        {
            double total = 0;
            for (int l = 0; l < EnergyLayerData.Labels.Length; l++) { total += EnergyLayer.RecordCapacityMw(id, l); }
            return Math.Max(100.0, Math.Round(total * 0.01 / 100.0) * 100.0);
        }

        /// <summary>
        /// Places an order in <paramref name="turn"/> (the turn being played; <paramref name="year"/> is what the page prints): <paramref name="mw"/> positive
        /// builds, negative retires - capped at what the fleet will have once the queue's retirements land, so the fleet is never ordered below zero.
        /// Returns the order, or null where nothing could be placed.
        /// </summary>
        public static Order Place(Country country, int technology, double mw, int year, int turn)
        {
            if (country == null || !CanOrder(country.Id, technology) || Math.Abs(mw) < 1e-9) { return null; }
            if (mw > 0 && EnergyConnectionQueue.RefusalFor(country, technology, mw) != null) { return null; }   // P6-F2e (§551): a build past the queue's published capacity is refused whole - CannotPlaceWhy has the sentence
            if (mw < 0)
            {
                double have = CapacityMw(country, technology) + QueuedMw(country, technology, retirementsOnly: true);
                if (have <= 0) { return null; }
                if (have + mw < 0) { mw = -have; }
            }
            int wait = mw > 0 ? LeadTimeYears[technology] : RetirementNoticeYears;
            var order = new Order
            {
                Technology = technology, Mw = mw, OrderedYear = year, OnlineYear = year + wait,
                OrderedTurn = turn, OnlineTurn = turn + wait, Landed = false,
            };
            (country.FleetOrders ?? (country.FleetOrders = new List<Order>())).Add(order);
            return order;
        }

        /// <summary>
        /// On load: drops every order on a label the queue refuses TODAY, landed or not, and says how many. §539's build refused only an empty record, so a save from
        /// it may carry a step on France's or Italy's coal - 1 300 MW on a record of 36 - which the fossil floor's scale (§544) would turn into a fleet thirty-seven
        /// times the size. An order the queue would not take cannot stand in it.
        /// </summary>
        public static int Sanitise(Country country)
        {
            if (country?.FleetOrders == null) { return 0; }
            return country.FleetOrders.RemoveAll(o => o == null || !CanOrder(country.Id, o.Technology));
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
        /// The boundary's step for one country: every order whose turn has come lands. <paramref name="turnAfterBoundary"/> is the number the turn will carry
        /// once this boundary has passed (the manager's counter plus one), so an order placed in turn t with a wait of w lands at the w-th boundary after it;
        /// an order from before the turn clock lands by <paramref name="year"/>. Called at the TOP of the boundary, before anything clears (§544: the water
        /// value and the power CO₂ read a clearing before the ledger does, and both read last year's fleet while it sat beside the ledger).
        /// </summary>
        public static int Advance(Country country, int turnAfterBoundary, int year)
        {
            int landed = 0;
            if (country?.FleetOrders == null) { return 0; }
            foreach (Order o in country.FleetOrders)
            {
                if (o.Landed) { continue; }
                bool due = o.OnlineTurn > 0 || o.OrderedTurn > 0 ? o.OnlineTurn <= turnAfterBoundary : o.OnlineYear <= year;
                if (due) { o.Landed = true; landed++; }
            }
            return landed;
        }

        // ---- the fleet a reader sees ---------------------------------------------------------------------------------------------

        /// <summary>The landed orders' MW for a technology - the country's own, read off its queue.</summary>
        public static double LandedMw(Country country, int label)
        {
            double sum = 0;
            if (country?.FleetOrders == null) { return 0.0; }
            foreach (Order o in country.FleetOrders) { if (o.Landed && o.Technology == label) { sum += o.Mw; } }
            return sum;
        }

        /// <summary>The country's fleet for a technology, MW: the record plus its own landed orders. What a reader that HOLDS the country asks.</summary>
        public static double CapacityMw(Country country, int label) => country == null ? 0.0 : Math.Max(0.0, EnergyLayer.RecordCapacityMw(country.Id, label) + LandedMw(country, label));

        private static readonly List<Country> Ambient = new List<Country>();
        private static int _recordDepth;

        /// <summary>
        /// A scope naming the country whose landed orders the market's ID-KEYED readers see (<see cref="DeltaMw"/>, through <c>EnergyLayer.CapacityMw</c> and
        /// <see cref="Scale"/>). Nestable - the innermost scope for an id wins, so Sweden's clearing can clear Germany's inside it; dispose ends it.
        /// </summary>
        public static IDisposable For(Country country) { Ambient.Add(country); return new FleetScope(); }

        private sealed class FleetScope : IDisposable
        {
            private bool _done;
            public void Dispose() { if (!_done) { _done = true; if (Ambient.Count > 0) { Ambient.RemoveAt(Ambient.Count - 1); } } }
        }

        /// <summary>
        /// A scope in which every reader sees the RECORD's fleet whatever country is named: the market's SEED calibrations (the adders, the seed spread, the
        /// seed water value, the seed clearing) are facts about 2023, cached for the process, and may be asked for the first time from inside a country's
        /// clearing. Nestable; dispose ends it.
        /// </summary>
        public static IDisposable RecordOnly() { _recordDepth++; return new RecordScope(); }

        private sealed class RecordScope : IDisposable
        {
            private bool _done;
            public void Dispose() { if (!_done) { _done = true; _recordDepth--; } }
        }

        /// <summary>The landed orders' MW an id-keyed reader sees: the innermost scoped country with this id, or none - the record.</summary>
        public static double DeltaMw(CountryId id, int label)
        {
            if (_recordDepth > 0) { return 0.0; }
            for (int i = Ambient.Count - 1; i >= 0; i--)
            {
                Country c = Ambient[i];
                if (c != null && c.Id == id) { return LandedMw(c, label); }
            }
            return 0.0;
        }

        /// <summary>The fleet's scale for a technology as an id-keyed reader sees it: (record + landed) over the record - exactly one with nothing landed or no scope, one where the record is empty, never below zero.</summary>
        public static double Scale(CountryId id, int label)
        {
            double record = EnergyLayer.RecordCapacityMw(id, label);
            return record > 0 ? Math.Max(0.0, (record + DeltaMw(id, label)) / record) : 1.0;
        }
    }
}
