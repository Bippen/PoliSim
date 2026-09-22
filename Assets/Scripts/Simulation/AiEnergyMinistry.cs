using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Data.Generated;

namespace PoliSim.Simulation
{
    /// <summary>
    /// THE AI ENERGY MINISTRY (P6-F2d, 2026-09-21, COMPLETED.md §544) - the spec-let's S11, stage 7: *"the energy ministry on the AI finance ministry's
    /// pattern ... a mandate with weights, a budget, decisions explained through the attribution idiom; minister attributes inherited. Its failure modes
    /// measured, not authored."* A country the player does not govern answers ITS OWN STATUTE with the player's own lever - an order in the connection
    /// queue (<see cref="EnergyFleet.Place"/>), nothing the player cannot do, nothing that reads the player's country - and every order carries the
    /// sentence that explains it (<see cref="EnergyFleet.Order.Reason"/>): the statute, the year it is read at, the gap, the MW.
    ///
    /// <para><b>The mandates are the countries' own law, fetched on the day and not recalled</b> (2026-09-21; the record has the quotations):
    /// Germany - EEG 2023 § 4 (onshore wind 115 GW and solar 215 GW in 2030, 157 and 309 in 2035, 160 and 400 in 2040), WindSeeG § 1(2) (offshore at least
    /// 30 GW by 2030, 40 by 2035, 70 by 2045) and KVBG § 4 (coal's net rated capacity 30 GW in 2022, 17 GW on 1 April 2030, none after 31 December 2038, in
    /// equal annual steps); Italy - the PNIEC as sent to Brussels on 1 July 2024 (MASE's release: 131 GW of renewables in 2030, of which solar 79.2 and wind
    /// 28.1); France - Code de l'énergie art. L100-4 4° (renewables at least 40 % of electricity production in 2030); Sweden - the riksdag's goal as changed
    /// by bet. 2022/23:FiU21 (*100 procent fossilfri elproduktion år 2040*). Poland's KPEiK was reached through secondary reports only, so its mandate is
    /// BILLED and its ministry orders nothing until the plan's own text is in hand; the United States has no federal statutory target in force, and its
    /// ministry orders nothing because there is nothing to answer - the finance ministry's own precedent (*"the US rule has no release side: the statutes
    /// have none"*).</para>
    ///
    /// <para><b>The weights, and what they cannot yet be.</b> A mandate weighs three things (the trilemma every one of these statutes names): DECARBONISATION
    /// - the statute's path is followed in full (weight 1); SECURITY OF SUPPLY - a veto, not a weight: a retirement that would leave the peak block's residual
    /// demand above the dependable fleet is DEFERRED and says so; AFFORDABILITY - BILLED at zero, because nothing in the queue is priced until IRENA's costs
    /// land (§539), and a ministry cannot weigh a cost it cannot read. The budget S11 names is the same absence: an order costs nothing, so no budget binds.
    /// No Energy portfolio exists in the cabinet, so no minister's attributes are inherited yet - owed, with the portfolio.</para>
    ///
    /// <para>⚠ <b>HELD (<see cref="Live"/> = false), and why.</b> A ministry that orders moves every AI state's fleet, so its first live turn is a BASELINE
    /// family on the same fields FT-10's pending fix moves (§541 - proposed, not applied, Elias's to rule); and S11 asks that its failure modes be MEASURED
    /// before they are met. So the rule is built, the mandates sourced, the turn's call site wired behind the flag, and `EnergyMinistryDiagnostic` runs the
    /// century with the ministries deciding and reports what they do and where they fail. Flipping the flag is its own family with its own dump.</para>
    /// </summary>
    public static class AiEnergyMinistry
    {
        /// <summary>⚠ The hold: false until the ministry's family is dumped and ruled (the class note). The diagnostic and the page's mandate row do not read this; the turn's call site does.</summary>
        public static readonly bool Live = false;

        public enum MandateForm { CapacityPath, RenewableShare, FossilFree, Billed, None }

        /// <summary>One country's mandate: its form, the statute it is read from, and the figures in the statute's own units.</summary>
        public sealed class Mandate
        {
            public CountryId Country;
            public MandateForm Form;
            /// <summary>The citation, as the page prints it.</summary>
            public string Statute;
            /// <summary>The mandate in one line, as the page prints it.</summary>
            public string Headline;
            /// <summary>Capacity paths, GW by year, in the statute's own points; between two points the path is a straight line, before the first it runs from the record's fleet in <see cref="EnergyLayer.Year"/>, after the last it holds.</summary>
            public (int Year, double Gw)[] WindGw, SolarGw;
            /// <summary>Coal's path as a fraction of the record's coal fleet, by year - the statute's own ratio of its target levels.</summary>
            public (int Year, double Fraction)[] CoalFraction;
            /// <summary>A generation-share target: renewables (hydro, wind, solar) as a per cent of electricity production, and its year.</summary>
            public double RenewableSharePercent; public int ShareYear;
            /// <summary>The year by which the fleet's fossil categories the queue can reach (coal, gas) stand at zero.</summary>
            public int FossilFreeYear;
        }

        /// <summary>What a year's decision did: the orders placed and the steps deferred, each with its sentence.</summary>
        public sealed class Decision
        {
            public readonly List<EnergyFleet.Order> Placed = new List<EnergyFleet.Order>();
            public readonly List<string> Deferred = new List<string>();
        }

        // SOURCED - every figure below is its statute's, fetched 2026-09-21 (the class note names each; COMPLETED.md §544 quotes them). DERIVED where stated:
        // Germany's wind path is § 4 EEG's onshore figure plus § 1(2) WindSeeG's offshore figure at each year both name (2030: 115 + 30; 2035: 157 + 40), with the
        // offshore figure between 2035 and 2045 on the statute's own straight line (2040: 160 + 55) and onshore held at its last point after 2040 (2045: 160 + 70);
        // coal's fractions are KVBG § 4's target levels over its own 2022 level (17 ⁄ 30 in 2030, none after 2038).
        private static readonly Dictionary<CountryId, Mandate> Mandates = new Dictionary<CountryId, Mandate>
        {
            { CountryId.Germany, new Mandate { Country = CountryId.Germany, Form = MandateForm.CapacityPath,
                Statute = "EEG 2023 § 4 · WINDSEEG § 1 · KVBG § 4", Headline = "WIND 145 GW · SOLAR 215 GW IN 2030 · COAL OUT BY 2038",
                WindGw = new[] { (2030, 145.0), (2035, 197.0), (2040, 215.0), (2045, 230.0) },
                SolarGw = new[] { (2030, 215.0), (2035, 309.0), (2040, 400.0) },
                CoalFraction = new[] { (2022, 1.0), (2030, 17.0 / 30.0), (2038, 0.0) } } },
            { CountryId.Italy, new Mandate { Country = CountryId.Italy, Form = MandateForm.CapacityPath,
                Statute = "PNIEC · SENT 1 JULY 2024 · MASE", Headline = "WIND 28.1 GW · SOLAR 79.2 GW IN 2030",
                WindGw = new[] { (2030, 28.1) }, SolarGw = new[] { (2030, 79.2) } } },
            { CountryId.France, new Mandate { Country = CountryId.France, Form = MandateForm.RenewableShare,
                Statute = "CODE DE L'ÉNERGIE ART. L100-4 4°", Headline = "RENEWABLES 40 % OF PRODUCTION IN 2030",
                RenewableSharePercent = 40.0, ShareYear = 2030 } },
            { CountryId.Sweden, new Mandate { Country = CountryId.Sweden, Form = MandateForm.FossilFree,
                Statute = "BET. 2022/23:FIU21 · RIKSDAGEN", Headline = "FOSSIL-FREE ELECTRICITY BY 2040",
                FossilFreeYear = 2040 } },
            { CountryId.Poland, new Mandate { Country = CountryId.Poland, Form = MandateForm.Billed,
                Statute = "KPEiK · THE PLAN'S OWN TEXT IS OWED", Headline = "BILLED · REACHED ONLY THROUGH SECONDARY REPORTS" } },
            { CountryId.USA, new Mandate { Country = CountryId.USA, Form = MandateForm.None,
                Statute = "NO FEDERAL STATUTORY TARGET IN FORCE", Headline = "NONE · NOTHING TO ANSWER" } },
        };

        /// <summary>CONVENTION - the four labels the ministry may order, by their index in EnergyLayerData.Labels (coal, gas, nuclear, hydro, wind, solar, other).</summary>
        private const int Coal = 0, Gas = 1, Wind = 4, Solar = 5;
        private static readonly int[] RenewableLabels = { 3, 4, 5 };   // CONVENTION - hydro, wind, solar: the labels a generation-share mandate counts ("other" mixes biomass with oil and is counted on neither side)

        /// <summary>CONVENTION - an order below this many MW is not placed (noise), as the finance ministry leaves a rate rise below its MinRatePoints unwritten.</summary>
        public const double MinOrderMw = 50.0;

        public static Mandate MandateOf(CountryId id) => Mandates.TryGetValue(id, out Mandate m) ? m : null;

        /// <summary>A path's value at a year: the statute's points on straight lines, from the record's fleet in the record's year before the first, held after the last.</summary>
        public static double PathAt((int Year, double Value)[] path, int year, double recordValue)
        {
            if (path == null || path.Length == 0) { return recordValue; }
            if (year <= EnergyLayer.Year) { return recordValue; }
            int prevYear = EnergyLayer.Year; double prev = recordValue;
            foreach ((int y, double v) in path)
            {
                if (year <= y) { return y == prevYear ? v : prev + (v - prev) * (year - prevYear) / (double)(y - prevYear); }
                prevYear = y; prev = v;
            }
            return prev;
        }

        /// <summary>The 2023 capacity factor of a label on the RECORD's fleet - what a built MW is taken to generate.</summary>
        private static double RecordUtilisation(CountryId id, int label)
        {
            double cap = EnergyLayer.RecordCapacityMw(id, label);
            return cap > 0 ? EnergyLayer.GenerationGwh(id, label) * 1000.0 / (cap * EnergyLayer.HoursPerYear) : 0.0;
        }

        /// <summary>What the fleet will be for a label once the queue has landed, MW.</summary>
        private static double FleetWithQueueMw(Country country, int label) => EnergyFleet.CapacityMw(country, label) + EnergyFleet.QueuedMw(country, label);   // the country's OWN fleet (§544)

        /// <summary>The renewable share of production the fleet-with-its-queue implies, per cent: the record's generation with wind and solar scaled by their fleets, over the record's total (the load is static, so what is built displaces, it does not add).</summary>
        public static double RenewableSharePercent(Country country)
        {
            double total = EnergyLayer.TotalGenerationGwh(country.Id), renewable = 0;
            if (total <= 0) { return 0; }
            foreach (int label in RenewableLabels)
            {
                double record = EnergyLayer.RecordCapacityMw(country.Id, label);
                double scale = record > 0 && (label == Wind || label == Solar) ? Math.Max(0.0, FleetWithQueueMw(country, label) / record) : 1.0;
                renewable += EnergyLayer.GenerationGwh(country.Id, label) * scale;
            }
            return 100.0 * renewable / total;
        }

        /// <summary>
        /// The year's decision for one country, in <paramref name="turn"/> (the turn being played; <paramref name="year"/> is the year the page prints): read the
        /// mandate at the year an order placed NOW would serve from, take the gap against the fleet with its queue, place it - or defer what the peak block could
        /// not bear, and say so. Pure in everything but the queue it writes; never reads another country.
        /// </summary>
        public static Decision Decide(Country country, int year, int turn)
        {
            var decision = new Decision();
            Mandate m = country != null ? MandateOf(country.Id) : null;
            if (m == null || !EnergyLayer.Has(country.Id) || m.Form == MandateForm.None || m.Form == MandateForm.Billed) { return decision; }

            if (m.Form == MandateForm.CapacityPath)
            {
                Build(country, year, turn, Wind, m.WindGw, m.Statute, decision);
                Build(country, year, turn, Solar, m.SolarGw, m.Statute, decision);
                if (m.CoalFraction != null) { Retire(country, year, turn, Coal, PathAt(m.CoalFraction, year + EnergyFleet.RetirementNoticeYears, 1.0), m.Statute, decision); }
            }
            else if (m.Form == MandateForm.RenewableShare)
            {
                int landsWind = year + EnergyFleet.LeadTimeYears[Wind];
                double recordShare = RecordRenewableSharePercent(country.Id);
                double target = PathAt(new[] { (m.ShareYear, m.RenewableSharePercent) }, landsWind, recordShare);
                double gap = target - RenewableSharePercent(country);
                if (gap > 0)
                {
                    // the load is static: a point of share is a per cent of the record's production, built at the fleet's own 2023 capacity factors, split as the fleet is split
                    double gwh = gap / 100.0 * EnergyLayer.TotalGenerationGwh(country.Id);
                    double wind = FleetWithQueueMw(country, Wind), solar = FleetWithQueueMw(country, Solar), both = wind + solar;
                    foreach (int label in new[] { Wind, Solar })
                    {
                        double part = both > 0 ? (label == Wind ? wind : solar) / both : 0.5, cf = RecordUtilisation(country.Id, label);
                        if (cf <= 0) { continue; }
                        double mw = gwh * part * 1000.0 / (cf * EnergyLayer.HoursPerYear);
                        Place(country, label, mw, year, turn, string.Format(CultureInfo.InvariantCulture, "{0}: {1:0.#} % OF PRODUCTION IN {2} AGAINST {3:0.#} % WITH THE QUEUE", m.Statute, target, landsWind, target - gap), decision);
                    }
                }
            }
            else if (m.Form == MandateForm.FossilFree)
            {
                foreach (int label in new[] { Coal, Gas })
                {
                    if (!EnergyFleet.CanOrder(country.Id, label)) { continue; }   // a label the queue refuses is one the goal cannot reach (Sweden: the zones clear without a fossil fleet) - nothing to place, nothing to defer each year
                    double fraction = PathAt(new[] { (m.FossilFreeYear, 0.0) }, year + EnergyFleet.RetirementNoticeYears, 1.0);
                    Retire(country, year, turn, label, fraction, m.Statute, decision);
                }
            }
            return decision;
        }

        /// <summary>The record's renewable share of production, per cent.</summary>
        public static double RecordRenewableSharePercent(CountryId id)
        {
            double total = EnergyLayer.TotalGenerationGwh(id), renewable = 0;
            foreach (int label in RenewableLabels) { renewable += EnergyLayer.GenerationGwh(id, label); }
            return total > 0 ? 100.0 * renewable / total : 0.0;
        }

        private static void Build(Country country, int year, int turn, int label, (int Year, double Gw)[] path, string statute, Decision decision)
        {
            if (path == null) { return; }
            int lands = year + EnergyFleet.LeadTimeYears[label];
            double target = PathAt(path, lands, EnergyLayer.RecordCapacityMw(country.Id, label) / 1000.0) * 1000.0;
            double have = FleetWithQueueMw(country, label);
            if (target - have < MinOrderMw) { return; }   // the fleet with its queue already stands at the path: a build never turns into a retirement
            Place(country, label, target - have, year, turn, string.Format(CultureInfo.InvariantCulture, "{0}: {1:0.#} GW IN {2} AGAINST {3:0.#} GW WITH THE QUEUE", statute, target / 1000.0, lands, have / 1000.0), decision);
        }

        private static void Retire(Country country, int year, int turn, int label, double fraction, string statute, Decision decision)
        {
            double target = Math.Max(0.0, fraction) * EnergyLayer.RecordCapacityMw(country.Id, label);
            double have = FleetWithQueueMw(country, label);
            double excess = have - target;
            if (excess < MinOrderMw && !(target <= 0.0 && excess > 1e-6)) { return; }   // below the noise floor nothing is placed - except a fleet's LAST megawatts, when the statute's figure is zero
            string name = EnergyLayerData.Labels[label].ToUpperInvariant();
            // SECURITY OF SUPPLY, the veto: what the peak block can spare of this label, the queue's retirements already counted
            double spare = PeakBlockSpareMw(country, label, out double residual, out double dependable);
            double retire = Math.Min(excess, spare);
            if (retire < excess)
            {
                decision.Deferred.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1}: {2} -{3:0} MW DEFERRED OF -{4:0} - THE PEAK BLOCK'S NET RESIDUAL {5:0} MW AGAINST A FLEXIBLE FOSSIL FLEET OF {6:0} MW WITH THE QUEUE", country.Id, year, name, excess - Math.Max(0.0, retire), excess, residual, dependable));
            }
            if (retire < MinOrderMw && !(target <= 0.0 && retire > 1e-6)) { return; }
            Place(country, label, -retire, year, turn, string.Format(CultureInfo.InvariantCulture, "{0}: {1} AT {2:0.#} GW IN {3} AGAINST {4:0.#} GW WITH THE QUEUE", statute, name, target / 1000.0, year + EnergyFleet.RetirementNoticeYears, have / 1000.0), decision);
        }

        private static void Place(Country country, int label, double mw, int year, int turn, string reason, Decision decision)
        {
            if (mw > 0 && mw < MinOrderMw) { return; }
            if (!EnergyFleet.CanOrder(country.Id, label))
            {
                // the queue refuses the label for the ministry as for the player - the mandate's step is recorded as deferred, with the queue's own reason
                decision.Deferred.Add(country.Id + " " + year + ": " + EnergyLayerData.Labels[label].ToUpperInvariant() + " DEFERRED - CANNOT BE ORDERED: " + EnergyFleet.CannotOrderWhy(country.Id, label));
                return;
            }
            if (mw > 0)
            {
                // P6-F2e (§551): the connection queue's capacity binds the ministry as it binds the player - it orders what the queue has room for and says what it could not
                double room = EnergyConnectionQueue.RoomMw(country, label, year);   // §575: the year the order will carry - the ministry decides AT the boundary for the year about to be played
                if (EnergyConnectionQueue.RefusalFor(country, label, mw, year) != null)   // the queue's ONE rule - the inequality Place itself tests, never a second copy of it
                {
                    double fits = room >= MinOrderMw ? room : 0.0;   // below the noise floor nothing is placed, and the whole step is what was deferred
                    decision.Deferred.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1}: {2} +{3:0} MW DEFERRED OF +{4:0} - THE CONNECTION QUEUE HAS ROOM FOR {5:0} MW: {6}", country.Id, year,
                        EnergyLayerData.Labels[label].ToUpperInvariant(), mw - fits, mw, room, EnergyConnectionQueue.RefusalFor(country, label, mw, year)));
                    mw = fits;
                    if (mw < MinOrderMw) { return; }
                }
            }
            EnergyFleet.Order order = EnergyFleet.Place(country, label, mw, year, turn);
            if (order == null) { return; }
            order.Reason = reason;
            decision.Placed.Add(order);
        }

        /// <summary>
        /// SECURITY OF SUPPLY, the veto's measure: how many MW of a fossil label the peak block can spare. Adequacy in the clearing is the block's residual against the
        /// FLEXIBLE fleet - each category's dependable capacity less its must-run floor, which is already inside the residual (the verification review: the first form
        /// compared the residual with a fleet that still held its floors, lenient by Germany's 11 GW of them). Retiring X MW of a label takes X·p of its dependable
        /// capacity and X·f of its floor - so the flexible fleet falls by X·(p − f) and the residual rises by X·f, and the f cancels: X ≤ (flexible − residual) ⁄ p.
        /// The queue's pending retirements of EITHER fossil label are taken off first, each at its own p. The residual is the NET one across the country's zones (the
        /// links' limits are not read) and the clearing's as it stands - builds still in the queue are not credited: a veto errs toward the lights.
        /// </summary>
        private static double PeakBlockSpareMw(Country country, int label, out double residual, out double flexible)
        {
            EnergyMarket.Result r = EnergyMarket.Clear(country);
            residual = 0;
            if (r.Zones != null) { foreach (EnergyMarket.BlockResult[] zone in r.Zones) { if (zone != null && zone.Length > 2 && zone[2] != null) { residual += zone[2].ResidualMw; } } }
            residual = Math.Max(0.0, residual);
            double perMw, pending = 0;
            using (EnergyFleet.For(country))
            {
                flexible = 0;
                for (int k = 0; k < EnergyMarket.FossilCount; k++) { flexible += Math.Max(0.0, EnergyMarket.DependableMw(country.Id, k) - EnergyMarket.MustRunFossilMw(country.Id, k)); }
                perMw = DependablePerMw(country, label);
                foreach (int fossil in new[] { Coal, Gas }) { pending += -EnergyFleet.QueuedMw(country, fossil, retirementsOnly: true) * DependablePerMw(country, fossil); }   // the queue's retirements are negative MW
            }
            flexible = Math.Max(0.0, flexible - pending);
            if (perMw <= 0.0) { return double.MaxValue; }   // the label holds no dependable capacity: nothing the peak block leans on leaves with it
            return Math.Max(0.0, (flexible - residual) / perMw);
        }

        /// <summary>A fossil label's dependable capacity per MW of its fleet - read inside the country's scope.</summary>
        private static double DependablePerMw(Country country, int label)
        {
            double fleet = EnergyFleet.CapacityMw(country, label);
            return fleet > 0 ? EnergyMarket.DependableMw(country.Id, label == Coal ? EnergyMarket.Coal : EnergyMarket.Gas) / fleet : 0.0;
        }
    }
}
