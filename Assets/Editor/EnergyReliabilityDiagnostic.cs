using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Data.Generated;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// MF q26 + q27 (Elias's rulings q26 and q27 of 2026-10-18, K145 of 2026-10-27; the audit's GA-6-1 and GA-6-2): **THE PLAYER'S FLEET ORDERS MEET THE AI
    /// MINISTRY'S RELIABILITY CHECK, UNSERVED ENERGY COSTS OUTPUT AT THE VALUE OF LOST LOAD, AND SWEDEN'S GAP GOES UNSERVED AT THE CEILING.** It fails while any of
    /// these statements is false:
    /// <list type="bullet">
    /// <item><b>(a) the value of lost load</b> is the generated data's (Tools/voll_prep.pl from the stored CEPA, LBNL, Eurostat and BLS files): a row for each of the
    /// six, in the market's currency, carried to 2023 by its own index - the bridge recomputed here.</item>
    /// <item><b>(b) the player's order is refused where the AI's would be</b>: on Germany's fleet, a retirement of coal or gas past what the ministry's own
    /// veto lets the peak block spare (`AiEnergyMinistry.PeakBlockSpareMw`) is refused, its sentence citing EnWG § 13b, and half of it is not; through the
    /// manager's verb (`SimulationManager.PlaceFleetOrder`) the same order is refused with the same sentence and the queue does not move.</item>
    /// <item><b>(c) a label the ministry never retires</b>: France's whole nuclear fleet retired at once is refused by the clearing's test, which names the unserved MW.</item>
    /// <item><b>(d) a planted unserved block costs output at the VoLL, the arithmetic exact</b>: Germany's whole coal and gas fleets landed as retired, the
    /// year's step (`EnergyLedger.AdvanceYear`) sets the loss rate to the unserved GWh (each block's MW over its hours, recomputed here) × the non-household
    /// share × the output VoLL × the dollar bridge; the day's overlay takes exactly that rate off GDP and the next day's restore puts back exactly what it took;
    /// and through a manager's day, a world with the rate planted ends its first day lower than its untouched twin by exactly the rate.</item>
    /// <item><b>(e) Sweden's gap goes unserved at the ceiling</b> (GA-6-2's reproduction): on the record's fleet the chain's balance is the seed's to the bit and
    /// nothing is unserved; with Sweden's whole nuclear fleet retired, the fall below the seed's balance is unserved at SE4 in each block exactly, the zones
    /// priced with it clear at the ceiling, prices move against the control and the loss is positive; a single step of Swedish nuclear at the seed is refused;
    /// and the surplus half: one smallest step of wind, then a twentieth more, the balance above the seed's booked as curtailed (displaced hydro) to the excess, the
    /// balance net of it the seed's, and the zones priced as the control (the water value) wherever the block's hydro can back the excess down - the single step's
    /// in every block - the chain's end at the curtailment offer only where it cannot.</item>
    /// <item><b>(s) the seed is zero</b> for all six countries: nothing unserved, no loss rate after a year's step. And (d) shows the rate returning to 0 when the
    /// unserved load ends.</item>
    /// </list>
    /// Run: <c>-executeMethod PoliSim.EditorTools.EnergyReliabilityDiagnostic.Run</c>; a passing run prints <c>ENERGY RELIABILITY: PASS</c>.
    /// </summary>
    public static class EnergyReliabilityDiagnostic
    {
        private static int _failures;
        private static StringBuilder _sb;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static string F(string format, params object[] args) => string.Format(Inv, format, args);

        /// <remarks>CONVENTION - the labels by their index in EnergyLayerData.Labels (coal, gas, nuclear, hydro, wind, solar, other).</remarks>
        private const int Coal = 0, Gas = 1, Nuclear = 2, Wind = 4;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _failures = 0;
            _sb = new StringBuilder("ENERGY RELIABILITY: MF q26/q27 - the reliability check on the player's orders, unserved energy at the value of lost load, Sweden's gap at the ceiling (seed 777)\n");
            try
            {
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                EnergyMarket.ResetTurnState();
                Voll();
                RefusedWhereTheAiWouldBe();
                SeedIsZero();
                MustRunLabel();
                PlantedUnservedCostsOutput();
                ManagerDay();
                SwedenAtTheCeiling();
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            finally { EnergyMarket.ResetTurnState(); }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "ENERGY RELIABILITY: PASS - nothing is unserved at the seed in any of the six; the player's retirement is refused where the AI ministry's veto would refuse it (EnWG § 13b cited for Germany), the clearing's test refuses what the veto never reads, a planted unserved block costs output at the value of lost load (through the national currency) to the arithmetic and the cost ends when the load is served, and Sweden's gap goes unserved at the ceiling with its prices moving while its surplus is booked as displaced hydro at the water value."
                : F("ENERGY RELIABILITY: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "      ok   " : "      FAIL ").Append(what).Append('\n');
            if (!ok) { _failures++; Debug.LogError("ENERGY RELIABILITY: FAIL - " + what); }
        }

        private static Country Fresh(CountryId id) => WorldFactory.CreateDefault().GetCountry(id);

        /// <summary>An order landed at once - a planted fleet, outside the player's verb and outside the queue's rules (the harness's hand, not a press). A BUILD
        /// is written into the queue directly: `EnergyFleet.Place` refuses a build past the connection queue's published capacity and returns null, which planted
        /// nothing and let a case pass or fail on a fleet it never had (the third surplus case's 48.7 GW measured exactly the control's balance). A retirement goes
        /// through `Place`, whose cap keeps the fleet at or above zero. Every plant is asserted to have landed.</summary>
        private static void Land(Country c, int technology, double mw)
        {
            double before = EnergyFleet.CapacityMw(c, technology);
            if (mw > 0.0)
            {
                (c.FleetOrders ?? (c.FleetOrders = new List<EnergyFleet.Order>())).Add(new EnergyFleet.Order { Technology = technology, Mw = mw, OrderedYear = c.CalendarYear, OnlineYear = c.CalendarYear, Landed = true });
            }
            else
            {
                EnergyFleet.Order o = EnergyFleet.Place(c, technology, mw, c.CalendarYear, 0);
                if (o != null) { o.Landed = true; }
            }
            double after = EnergyFleet.CapacityMw(c, technology);
            Check(Near(after, Math.Max(0.0, before + mw), MwTolerance), F("the plant landed: {0} {1} {2:+0;-0} MW, {3:0} -> {4:0} MW", c.Id, EnergyLayerData.Labels[technology], mw, before, after));
        }

        // ============================================================ (a)
        private static void Voll()
        {
            _sb.Append("    a. the value of lost load (Tools/voll_prep.pl: CEPA 2018 Annex G for the five, LBNL-6941E Table ES-1 for the USA), carried to 2023, read from " + ValueOfLostLoadData.SourceFolder + "\n");
            foreach (CountryId id in new[] { CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.Sweden, CountryId.USA })
            {
                bool has = ValueOfLostLoadData.Rows.TryGetValue(id, out var row);
                Check(has, F("{0}: a generated row", id));
                if (!has) { continue; }
                double nat = EnergyLayerData.NationalPerMarketCurrency[EnergyLayer.Index(id)];
                double bridged = row.OutputPerKwh * row.FxThen * row.Index2023 / row.IndexThen / nat, read = EnergyReliability.OutputVollPerKwh(id);
                double usd = EnergyLayerData.UsdPerMarketCurrency[EnergyLayer.Index(id)];
                bool national = id == CountryId.Sweden || id == CountryId.Poland;
                // THREE UNITS: krona and zloty through the ECB 2015 rate and back at the layer's 2023 rate, which is the ECB's 2023 annual average to the catalog's rounding
                Check(national ? row.FxThen > 1.0 && Math.Abs(row.Fx2023 - nat) < 5e-5 && read < row.OutputPerKwh * row.Index2023 / row.IndexThen : row.FxThen == 1.0 && row.Fx2023 == 1.0 && nat == 1.0,
                    F("{0}: the currency route - {1} {2:0.####} per euro in {3}, {4:0.####} in 2023 (the layer holds {5:0.####})", id, national ? (id == CountryId.Sweden ? "SEK" : "PLN") : "no conversion,", row.FxThen, row.PriceYear, row.Fx2023, nat));
                Check(row.OutputPerKwh > 0 && row.IndexThen > 0 && row.Index2023 > row.IndexThen && bridged == read && row.Currency == (id == CountryId.USA ? "USD" : "EUR")
                      && (id == CountryId.USA ? row.PriceYear == 2013 && row.SectorsKept == 0 : row.PriceYear == 2015 && row.SectorsKept >= 12 && row.WeightGwh > 0),
                    F("{0}: output VoLL {1:0.0000} {2}/kWh ({3} prices), carried by {4} / {5} through the national currency = {6:0.0000} in 2023 prices = {7:0.0000} book USD/kWh; domestic {8:0.00} (carried, not in GDP); {9} sector(s) over {10:0} GWh",
                        id, row.OutputPerKwh, row.Currency, row.PriceYear, row.Index2023, row.IndexThen, read, read * usd, row.DomesticPerKwh, row.SectorsKept, row.WeightGwh));
            }
        }

        // ============================================================ (b)
        private static void RefusedWhereTheAiWouldBe()
        {
            _sb.Append("    b. Germany: a retirement past the AI ministry's spare is refused (EnWG § 13b), half of it is not; the manager's verb refuses the same order\n");
            Country de = Fresh(CountryId.Germany);
            int tested = 0;
            foreach (int label in new[] { Coal, Gas })
            {
                double have = EnergyFleet.CapacityMw(de, label) + EnergyFleet.QueuedMw(de, label, retirementsOnly: true);
                double spare = AiEnergyMinistry.PeakBlockSpareMw(de, label, out double residual, out double flexible);
                string name = EnergyLayerData.Labels[label];
                if (!(spare < have))
                {
                    _sb.Append(F("      ..   {0}: the peak block spares the whole fleet ({1:0} MW of {2:0}) - retired whole (landed) and the next label read\n", name, Math.Min(spare, 1e12), have));
                    Land(de, label, -have);
                    continue;
                }
                tested++;
                double past = Math.Min(have, spare + Math.Max(1.0, 0.01 * spare));
                string why = EnergyReliability.RefusalFor(de, label, -past);
                Check(why != null && why.StartsWith(EnergyReliability.StatuteOf(CountryId.Germany), StringComparison.Ordinal),
                    F("{0}: -{1:0} MW past the spare of {2:0} MW (net residual {3:0} against {4:0} flexible) is refused, citing EnWG § 13b: {5}", name, past, spare, residual, flexible, why ?? "NOT REFUSED"));
                if (spare >= 2.0)
                {
                    string half = EnergyReliability.RefusalFor(de, label, -0.5 * spare);
                    Check(half == null, F("{0}: -{1:0} MW, half the spare, passes ({2})", name, 0.5 * spare, half ?? "no refusal"));
                }

                var go = new GameObject("ENERGYRELIABILITY_VERB");
                try
                {
                    World world = WorldFactory.CreateDefault();
                    SimulationManager sim = go.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    Country w = world.GetCountry(CountryId.Germany);
                    if (label == Gas) { Land(w, Coal, -(EnergyFleet.CapacityMw(w, Coal))); }   // the same planted fleet the gas reading stood on
                    int before = w.FleetOrders?.Count ?? 0;
                    string expected = EnergyReliability.RefusalFor(w, label, -past);
                    bool placed = sim.PlaceFleetOrder(CountryId.Germany, label, -past, w.CalendarYear, out string refused);
                    Check(!placed && refused != null && refused == expected && (w.FleetOrders?.Count ?? 0) == before,
                        F("{0}: the manager's verb refuses the same order with the same sentence and the queue stands at {1} order(s)", name, before));
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
                break;   // one label read where the veto binds is the statement; the second would stand on a fleet the first already moved
            }
            Check(tested > 0, "the veto binds on Germany's fossil fleet somewhere - a label was read (otherwise nothing was compared)");
        }

        // ============================================================ (c)
        private static void MustRunLabel()
        {
            _sb.Append("    c. France: the whole nuclear fleet retired at once - the clearing's test, which the ministry's closed form never reads\n");
            Country fr = Fresh(CountryId.France);
            double have = EnergyFleet.CapacityMw(fr, Nuclear);
            string why = EnergyReliability.RefusalFor(fr, Nuclear, -have);
            double unserved = EnergyReliability.TrialUnservedMw(fr, Nuclear, -have);
            Check(why != null && why.Contains("UNSERVED") && unserved > 0.0, F("-{0:0} MW of nuclear: {1:0} MW unserved across the blocks, refused: {2}", have, unserved, why ?? "NOT REFUSED"));
            Check(EnergyReliability.TrialUnservedMw(fr, -1, 0.0) == 0.0, "on the record's fleet France leaves nothing unserved");
        }

        // ============================================================ (d)
        private static void PlantedUnservedCostsOutput()
        {
            _sb.Append("    d. Germany with its whole coal and gas fleets retired (planted): the year's loss rate is the unserved GWh x the non-household share x the VoLL x the bridge; the overlay exact\n");
            Country de = Fresh(CountryId.Germany);
            Land(de, Coal, -EnergyFleet.CapacityMw(de, Coal));
            Land(de, Gas, -EnergyFleet.CapacityMw(de, Gas));
            EnergyLedger.AdvanceYear(de);
            EnergyMarket.Result r = EnergyMarket.Clear(de);
            double gwh = 0.0;
            int zone = EnergyLayer.ZoneIndex(EnergyLayer.Code(CountryId.Germany));
            for (int b = 0; b < 3; b++) { gwh += r.Zones[0][b].UnservedMw * EnergyLayerData.DispatchHours[zone][b] / 1000.0; }
            int ci = EnergyLayer.Index(CountryId.Germany);
            double share = EnergyLayerData.RetailConsumptionGwh[ci][EnergyLedger.NonHouseholds] / (EnergyLayerData.RetailConsumptionGwh[ci][EnergyLedger.Households] + EnergyLayerData.RetailConsumptionGwh[ci][EnergyLedger.NonHouseholds]);
            var row = ValueOfLostLoadData.Rows[CountryId.Germany];
            double voll = row.OutputPerKwh * row.FxThen * row.Index2023 / row.IndexThen / EnergyLayerData.NationalPerMarketCurrency[ci];
            double expected = gwh * share * voll / 1000.0 * EnergyLayerData.UsdPerMarketCurrency[ci];
            float rate = de.UnservedOutputLossRate;
            Check(gwh > 0.0 && Math.Abs(EnergyMarket.UnservedGwh(r) - gwh) <= 1e-9 * gwh, F("unserved: {0:0.0} GWh a year (blocks {1:0} / {2:0} / {3:0} MW at {4:0} EUR/MWh in the peak)", gwh, r.Zones[0][0].UnservedMw, r.Zones[0][1].UnservedMw, r.Zones[0][2].UnservedMw, r.Zones[0][2].Price));
            Check(rate > 0f && rate == (float)expected, F("the loss rate {0:0.000} bn a year = {1:0.0} GWh x {2:0.0000} x {3:0.0000} EUR/kWh / 1000 x {4} = {5:0.000} ({6:0.00} % of GDP {7:0.0})",
                rate, gwh, share, voll, EnergyLayerData.UsdPerMarketCurrency[ci], expected, 100.0 * rate / Math.Max(1f, de.State.GDP), de.State.GDP));
            float g = de.State.GDP;
            EnergyReliability.ApplyOutput(de);
            float overlaid = de.State.GDP, wedge = de.UnservedOutputWedge;
            EnergyReliability.LiftOverlay(de); float lifted = de.State.GDP; EnergyReliability.LowerOverlay(de); float lowered = de.State.GDP;
            EnergyReliability.RestoreOutput(de);
            Check(overlaid == Mathf.Max(MacroSystem.MinGdp, g - rate) && wedge == g - overlaid && lifted == g && lowered == overlaid && de.State.GDP == g && de.UnservedOutputWedge == 0f,
                F("the overlay: {0:0.000} → {1:0.000} (wedge {2:0.000}); lifted {3:0.000}, lowered {4:0.000}; restored {5:0.000}", g, overlaid, wedge, lifted, lowered, de.State.GDP));
            de.FleetOrders.Clear();   // the planted retirements withdrawn: the unserved load ends
            EnergyLedger.AdvanceYear(de);
            Check(de.UnservedOutputLossRate == 0f, F("the unserved load ended, the next year's step sets the loss rate back to 0 (read {0})", de.UnservedOutputLossRate));
        }

        /// <summary>The q26 review's L1: on the record's fleet no country leaves load unserved and no year's step sets a loss - the default trajectory carries no overlay.</summary>
        private static void SeedIsZero()
        {
            _sb.Append("    s. the seed: all six countries on the record's fleet leave nothing unserved and set no loss\n");
            foreach (CountryId id in new[] { CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.Sweden, CountryId.USA })
            {
                Country c = Fresh(id);
                double gwh = EnergyMarket.UnservedGwh(EnergyMarket.Clear(c)), trial = EnergyReliability.TrialUnservedMw(c, -1, 0.0);
                EnergyLedger.AdvanceYear(c);
                Check(gwh == 0.0 && trial == 0.0 && c.UnservedOutputLossRate == 0f && c.UnservedOutputWedge == 0f,
                    F("{0}: {1} GWh unserved, {2} MW in the trial clearing, loss rate {3}", id, gwh, trial, c.UnservedOutputLossRate));
            }
        }

        private static void ManagerDay()
        {
            _sb.Append("    d'. a manager's day: the rate planted on Germany - its first day ends lower than its untouched twin's by exactly the rate\n");
            var gdp = new float[2]; var wedge = new float[2]; var plant = new float[2];
            for (int i = 0; i < 2; i++)
            {
                SimulationRandom.Seed(777);
                EnergyMarket.ResetTurnState();
                IDisposable epoch = SimulationManager.EpochScope();
                var go = new GameObject("ENERGYRELIABILITY_DAY");
                try
                {
                    PoliSim.Elections.WorldClock.ApplyStart(CountryId.Germany);
                    World world = WorldFactory.CreateDefault();
                    SimulationManager sim = go.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    Country de = world.GetCountry(CountryId.Germany);
                    plant[i] = i == 1 ? 0.01f * de.State.GDP : 0f;   // a loss of a per cent of GDP a year - the harness's plant, on the field the boundary writes
                    de.UnservedOutputLossRate = plant[i];
                    sim.AdvanceDay();
                    gdp[i] = de.State.GDP; wedge[i] = de.UnservedOutputWedge;
                }
                finally { UnityEngine.Object.DestroyImmediate(go); epoch.Dispose(); }
            }
            Check(wedge[0] == 0f && wedge[1] > 0f && gdp[1] == Mathf.Max(MacroSystem.MinGdp, gdp[0] - plant[1]) && wedge[1] == gdp[0] - gdp[1],
                F("twin {0:0.000}, planted {1:0.000}: lower by {2:0.000} (the rate {3:0.000}); the wedge {4:0.000}", gdp[0], gdp[1], gdp[0] - gdp[1], plant[1], wedge[1]));
        }

        /// <remarks>CONVENTION - the diagnostic's tolerances: a millionth of a MW per MW of the quantity's size (never under a millionth of a MW), and a billionth of a
        /// price per unit of its size - the bookings are differences of the balance with the seed's, exact but for the last bits of one subtraction.</remarks>
        private const double MwTolerance = 1e-6, PriceTolerance = 1e-9;
        private static bool Near(double a, double b, double tolerance) => Math.Abs(a - b) <= tolerance * Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));

        /// <summary>Sweden with <paramref name="windMw"/> of wind landed: the balance above the seed's booked as curtailed, exactly the excess; nothing unserved; the
        /// balance net of the booking is the seed's; every zone priced as the control (the water value) where the hydro of the zones priced with the chain's end can back
        /// the excess down, that group at the curtailment offer where it cannot; and the year's hydro energy lower than the control's by exactly what was backed down
        /// (the booked MW, capped at that hydro, over each block's hours - the q26 review 3's L2: a sign turned at the subtraction fails here).
        /// <paramref name="mustBeAbsorbed"/>: the hydro absorbs it in every block; <paramref name="mustSpill"/>: in some block it cannot (the floor branch is exercised).</summary>
        private static void Surplus(EnergyMarket.Result r0, double[] seed, double windMw, string what, bool mustBeAbsorbed, bool mustSpill)
        {
            Country built = Fresh(CountryId.Sweden);
            Land(built, Wind, windMw);   // landed at once (a plant)
            EnergyMarket.Result r2 = EnergyMarket.Clear(built);
            int n = r2.ZoneNames.Length;
            bool anySurplus = false, booked = true, priced = true, noneUnserved = true, absorbed = true;
            double backedGwh = 0.0;
            var line = new StringBuilder();
            for (int b = 0; b < 3; b++)
            {
                double above = Math.Max(0.0, r2.ChainUnbalanceMw[b] - seed[b]);
                int first = EnergyMarket.SwedenEndGroupStart(r2.Links, b);
                double hydro = 0.0; for (int z = first; z < n; z++) { hydro += EnergyLayerData.DispatchLevelMw[EnergyLayer.ZoneIndex(r2.ZoneNames[z])][b][4]; }   // CONVENTION - 4 is hydro's index in the dispatch categories (EnergyMarket.Hydro)
                bool binds = false; foreach (EnergyMarket.LinkResult l in r2.Links) { binds |= l.Binding[b]; }
                if (!Near(r2.ChainCurtailedMw[b], above, MwTolerance)) { booked = false; }
                if (above > 0.0) { anySurplus = true; }
                backedGwh += Math.Min(above, hydro) * EnergyLayerData.DispatchHours[EnergyLayer.ZoneIndex(r2.ZoneNames[n - 1])][b] / 1000.0;
                if (above > hydro)
                {
                    absorbed = false;
                    for (int z = first; z < n; z++) { if (r2.Zones[z][b].Price != EnergyMarket.CurtailmentOffer) { priced = false; } }
                }
                else if (!binds) { for (int z = 0; z < n; z++) { if (!Near(r2.Zones[z][b].Price, r0.Zones[z][b].Price, PriceTolerance)) { priced = false; } } }
                if (!binds)
                {
                    for (int z = 0; z < n; z++) { if (r2.Zones[z][b].UnservedMw != 0.0) { noneUnserved = false; } }
                    if (!Near(r2.ChainUnbalanceMw[b] - r2.ChainCurtailedMw[b], seed[b], MwTolerance)) { booked = false; }
                }
                line.Append(F(" block {0}: balance {1:0.0} MW against the seed's {2:0.0}, {3:0.0} curtailed (hydro {4:0} from {5}), {6} priced {7:0.00} against the control's {8:0.00}{9};",
                    b, r2.ChainUnbalanceMw[b], seed[b], r2.ChainCurtailedMw[b], hydro, r2.ZoneNames[first], r2.ZoneNames[n - 1], r2.Zones[n - 1][b].Price, r0.Zones[n - 1][b].Price, binds ? " (a link binds)" : ""));
            }
            bool mix = Near(r2.AnnualGwh[EnergyMarket.Hydro], r0.AnnualGwh[EnergyMarket.Hydro] - backedGwh, MwTolerance);
            Check(anySurplus && booked && priced && noneUnserved && mix && (!mustBeAbsorbed || absorbed) && (!mustSpill || !absorbed),
                F("+{0:0} MW, {1}:{2} nothing unserved; hydro {3:0.0} GWh a year against the control's {4:0.0} less {5:0.0} backed down{6}", windMw, what, line,
                    r2.AnnualGwh[EnergyMarket.Hydro], r0.AnnualGwh[EnergyMarket.Hydro], backedGwh, mustSpill ? " - the floor branch reached" : ""));
        }

        // ============================================================ (e)
        private static void SwedenAtTheCeiling()
        {
            _sb.Append("    e. Sweden (GA-6-2): the record's fleet balances at the seed's own balance; its nuclear retired, the fall goes unserved at SE4 at the ceiling\n");
            double[] seed = EnergyMarket.SwedenSeedUnbalanceMw();
            Country control = Fresh(CountryId.Sweden);
            EnergyMarket.Result r0 = EnergyMarket.Clear(control);
            double unserved0 = 0.0; foreach (EnergyMarket.BlockResult[] z in r0.Zones) { foreach (EnergyMarket.BlockResult x in z) { unserved0 += x.UnservedMw; } }
            Check(unserved0 == 0.0 && r0.ChainCurtailedMw[0] == 0.0 && r0.ChainCurtailedMw[1] == 0.0 && r0.ChainCurtailedMw[2] == 0.0 && r0.ChainUnbalanceMw[0] == seed[0] && r0.ChainUnbalanceMw[1] == seed[1] && r0.ChainUnbalanceMw[2] == seed[2],
                F("control: the balance {0:0.0} / {1:0.0} / {2:0.0} MW is the seed's to the bit, nothing unserved, nothing curtailed", r0.ChainUnbalanceMw[0], r0.ChainUnbalanceMw[1], r0.ChainUnbalanceMw[2]));

            Country se = Fresh(CountryId.Sweden);
            double nuclear = EnergyFleet.CapacityMw(se, Nuclear);
            Land(se, Nuclear, -nuclear);
            EnergyMarket.Result r1 = EnergyMarket.Clear(se);
            int n = r1.ZoneNames.Length;
            bool anyGap = false, exact = true, ceiling = true, moved = false;
            var line = new StringBuilder();
            for (int b = 0; b < 3; b++)
            {
                double gap = Math.Max(0.0, seed[b] - r1.ChainUnbalanceMw[b]);
                double atEnd = r1.Zones[n - 1][b].UnservedMw;
                bool northBinds = false; foreach (EnergyMarket.LinkResult l in r1.Links) { northBinds |= l.Binding[b] && l.FlowMw[b] < 0; }
                if (!northBinds && !Near(atEnd, gap, MwTolerance)) { exact = false; }
                if (gap > 0.0) { anyGap = true; if (r1.Zones[n - 1][b].Price != EnergyMarket.MaxClearingPrice) { ceiling = false; } }
                for (int z = 0; z < n; z++) { if (r1.Zones[z][b].Price != r0.Zones[z][b].Price) { moved = true; } }
                line.Append(F(" block {0}: balance {1:0.0} MW against the seed's {2:0.0}, {3:0.0} MW unserved at {4}, priced {5:0.0};", b, r1.ChainUnbalanceMw[b], seed[b], atEnd, r1.ZoneNames[n - 1], r1.Zones[n - 1][b].Price));
            }
            double loss = EnergyReliability.OutputLossBillions(CountryId.Sweden, r1);
            Check(anyGap && exact && ceiling && moved && loss > 0.0, F("-{0:0} MW of nuclear:{1} prices moved against the control; output lost {2:0.000} bn a year", nuclear, line, loss));

            // the surplus half (the q26 reviews' M1, N1, N3): a balance above the seed's is booked as curtailed (displaced hydro) and priced at the zones' own water value -
            // the curtailment offer only where it exceeds the block's hydro; first one smallest step of wind, then a twentieth more wind
            Surplus(r0, seed, EnergyFleet.StepMw(CountryId.Sweden), "one step of wind (N3)", true, false);
            Surplus(r0, seed, 0.05 * EnergyFleet.CapacityMw(Fresh(CountryId.Sweden), Wind), "a twentieth more wind", false, false);
            // the q26 review 3's L2: past the block's hydro, so the group reaches the curtailment offer. The links are widened by the probe's knob for this case alone, so
            // the whole chain is one group and the hydro bound is all four zones' - a binding cut would answer a different question (L1's), not this one
            double probeScale = EnergyMarket.ProbeLinkCapacityScale;
            EnergyMarket.ProbeLinkCapacityScale = 1000.0;
            try { Surplus(r0, seed, 3.0 * EnergyFleet.CapacityMw(Fresh(CountryId.Sweden), Wind), "three times the wind fleet more, the links widened", false, true); }
            finally { EnergyMarket.ProbeLinkCapacityScale = probeScale; }

            Country step = Fresh(CountryId.Sweden);
            double one = EnergyFleet.StepMw(CountryId.Sweden);
            string why = EnergyReliability.RefusalFor(step, Nuclear, -one);
            Check(why != null, F("a step of {0:0} MW of Swedish nuclear at the seed is refused (every MW below the seed's balance goes unserved): {1}", one, why ?? "NOT REFUSED"));
        }
    }
}
