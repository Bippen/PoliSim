using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// **P6-F2e (ruled by Elias 2026-09-21): THE CONNECTION QUEUE'S CAPACITY** - *"sourced per country (ENTSO-E's connection figures or the TSO's own published queue), billed
    /// where no source publishes one, never authored; a queue that refuses an order past its cap and says why."* §544 measured the need: the queue took an order of any size, and
    /// Germany's held ministry opened with a single year's order nearly the size of the country's whole wind fleet.
    ///
    /// <para><b>The rule.</b> A country's connection queue cannot hold more megawatts of BUILD orders - placed and not yet landed - than its grid operator's OWN PUBLISHED
    /// CONNECTION QUEUE holds, line by line as the operator prints it: the player's orders stand for the projects in that queue, and an operator that publishes what it has in
    /// process has published what its grid is set to connect. An order that would take a line past its figure is REFUSED WHOLE, with a sentence; an order that lands frees its
    /// megawatts; a retirement is not a connection and neither takes room nor gives it. An order already standing when a figure is lowered is not evicted - a capacity refuses
    /// what is asked of it, it does not take back what it took.</para>
    ///
    /// <para><b>What was fetched, 2026-09-21, and what each publisher means by its queue</b> (the definitions differ and are printed, never harmonised):
    /// ENTSO-E publishes NO connection figure - its Power Statistics are installed stock and ERAA 2025 / TYNDP 2024 are TSO-submitted trajectories (three independent readers, the
    /// Transparency Platform's 14.1.A unreadable without a token). So every figure here is an operator's own queue, or a national laboratory's compilation of them:
    /// SWEDEN - Svenska kraftnät, *Ansökt effekt per anslutningstyp*, per 1 September 2026: applications to the transmission grid until capacity is reserved; offshore wind is
    /// handled in a separate process and is NOT in it. FRANCE - RTE, Enedis, Agence ORE and the SER, *Panorama de l'électricité renouvelable au 31 décembre 2025*: projects with an
    /// accepted queue-entry or technical-and-financial offer, or RETAINED IN A TENDER (RTE), or a complete request (Enedis, the local operators, EDF SEI) - the glossary's own three
    /// limbs; all grids; renewables only. ⚠ Its offshore HEADLINE (9 278 MW) adds 5 850 MW of
    /// tenders still to come, which its own glossary does not call queued: the line here takes the chapter's 3 398 MW of projects in development (the F2e review's finding). ITALY - Terna, the Econnextion dashboard's dataset at 31 August 2026: every request to the national transmission grid whatever its status,
    /// renewables and storage only. POLAND - PSE, *Wykaz Zagregowanych Informacji o Przyłączeniach* (transmission at 31 July 2026, distribution above 1 kV at 30 June 2026),
    /// Tabela 3 and Tabela 4: connection conditions issued plus agreements in force - ⚠ Tabela 4's conditions row counts offshore wind's PRELIMINARY conditions, by PSE's own
    /// footnote, some 6,8 GW of the wind line: the publisher's row as printed, disclosed on the page, and Elias's to strike; the register's MIX column (hybrid sites, 71,4 GW of injection) names no single technology and
    /// stands in no line here, so the PV and wind lines understate what Poland's operators have in process. THE USA - there is no national operator; Lawrence Berkeley National Laboratory's *Queued Up:
    /// 2026 Edition* compiles queue data from seven ISO/RTOs and fifty balancing areas (some 98 % of installed capacity; forty-seven of the fifty had active requests) at the end of 2025. GERMANY - BILLED: no German
    /// operator or regulator publishes a national queue for generation; the four TSOs' one joint figure (717 applications, about 270 GW, end of the third quarter of 2025) is 211 GW
    /// of batteries and an unsplit remainder of consumers and generators on the transmission grid alone, and the Bundesnetzagentur's permitted-and-not-commissioned wind is a
    /// permit pipeline, not a grid queue. Where a publisher's queue has no line for a technology (France's and Italy's gas and nuclear: their queues are the renewables'), that
    /// technology's capacity is BILLED too, and an order of any size stands - which the page says.</para>
    ///
    /// <para><b>The several-worlds class (§544).</b> The figures are a catalog; what stands in a queue is read off the COUNTRY's own orders. Nothing here is keyed by a country's
    /// id and written at run time.</para>
    /// </summary>
    public static class EnergyConnectionQueue
    {
        /// <summary>One line of a publisher's queue, mapped onto the layer's technologies.</summary>
        public sealed class Line
        {
            /// <summary>The line in the page's words.</summary>
            public string Name;
            /// <summary>The layer's labels that stand in it (EnergyLayerData.Labels: coal 0, gas 1, nuclear 2, wind 4, solar 5).</summary>
            public int[] Technologies;
            /// <summary>SOURCED - the publisher's figure, MW; where it is a sum of the publisher's cells, <see cref="Made"/> shows the addends.</summary>
            public double CapMw;
            /// <summary>The publisher's own cells the figure is made of, for the record and the diagnostic.</summary>
            public string Made;
        }

        /// <summary>What one country's operator publishes - or why nothing is.</summary>
        public sealed class Published
        {
            public string Publisher;
            /// <summary>Whose queue the figures are, in the page's words - an operator's own, or a compilation of operators'.</summary>
            public string Whose;
            public string AsOf;
            /// <summary>What the publisher's queue holds, in its own definition, for the page's second line.</summary>
            public string Holds;
            public Line[] Lines = new Line[0];
            /// <summary>Non-null where no source publishes a queue at all: no capacity applies, and the page says why.</summary>
            public string BilledWhy;
        }

        private const int Coal = 0, Gas = 1, Nuclear = 2, Wind = 4, Solar = 5;   // CONVENTION - the layer's labels by index

        private static readonly Dictionary<CountryId, Published> Catalog = new Dictionary<CountryId, Published>
        {
            { CountryId.Sweden, new Published {
                Publisher = "SVENSKA KRAFTNÄT", Whose = "SVENSKA KRAFTNÄT'S OWN QUEUE", AsOf = "1 SEPT 2026", Holds = "APPLICATIONS TO THE TRANSMISSION GRID UNTIL CAPACITY IS RESERVED · OFFSHORE WIND IS OUTSIDE IT",
                Lines = new[] {
                    new Line { Name = "WIND", Technologies = new[] { Wind }, CapMw = 9004.0, Made = "Landbaserad vindkraft 9 004 MW" },
                    new Line { Name = "SOLAR", Technologies = new[] { Solar }, CapMw = 4844.0, Made = "Solkraft 4 844 MW" },
                    new Line { Name = "OTHER PRODUCTION", Technologies = new[] { Coal, Gas, Nuclear }, CapMw = 4348.0, Made = "Övrig elproduktion 4 348 MW" } } } },
            { CountryId.France, new Published {
                Publisher = "RTE · ENEDIS · AGENCE ORE · SER", Whose = "THE GRID OPERATORS' QUEUE IN THE PANORAMA", AsOf = "31 DEC 2025", Holds = "OFFER ACCEPTED, TENDER WON OR REQUEST COMPLETE, ALL GRIDS · RENEWABLES ONLY",
                Lines = new[] {
                    new Line { Name = "WIND", Technologies = new[] { Wind }, CapMw = 16934.0, Made = "éolien terrestre 13 536 MW + éolien en mer 3 398 MW of projets en développement - NOT the headline's 9 278 MW, which adds 5 850 MW of appels d'offres à venir that the Panorama's own glossary does not count as queued (and the chapter's two cells sum 30 MW short of its headline, unexplained)" },
                    new Line { Name = "SOLAR", Technologies = new[] { Solar }, CapMw = 35078.0, Made = "solaire 35 078 MW" } } } },
            { CountryId.Italy, new Published {
                Publisher = "TERNA · ECONNEXTION", Whose = "TERNA'S OWN QUEUE (ECONNEXTION)", AsOf = "31 AUG 2026", Holds = "EVERY REQUEST TO THE TRANSMISSION GRID, WHATEVER ITS STATUS · RENEWABLES AND STORAGE ONLY",
                Lines = new[] {
                    new Line { Name = "WIND", Technologies = new[] { Wind }, CapMw = 168872.0, Made = "eolico on-shore 104 657 MW (five statuses) + eolico off-shore 64 215 MW (four: none stands at STMD/Contratti), each summed over the dataset's statuses" },
                    new Line { Name = "SOLAR", Technologies = new[] { Solar }, CapMw = 140312.0, Made = "solare 140 312 MW, the sum of the dataset's five statuses" } } } },
            { CountryId.Poland, new Published {
                Publisher = "PSE", Whose = "PSE'S OWN REGISTER", AsOf = "31 JULY 2026", Holds = "CONDITIONS ISSUED (OFFSHORE: PRELIMINARY TOO) PLUS AGREEMENTS IN FORCE · TRANSMISSION; DISTRIBUTION >1 kV AT 30 JUNE",
                Lines = new[] {
                    new Line { Name = "WIND", Technologies = new[] { Wind }, CapMw = 24600.0, Made = "FW 7,5 GW (Tabela 3) + morskie farmy wiatrowe 17,1 GW (Tabela 4: 8,7 GW at the conditions stage, which PSE's footnote 11 says INCLUDES PRELIMINARY conditions - its project register of the same date lists 1 875 MW with conditions issued - plus 8,4 GW of agreements in force)" },
                    new Line { Name = "SOLAR", Technologies = new[] { Solar }, CapMw = 34000.0, Made = "PV 34 GW (Tabela 3)" },
                    new Line { Name = "OTHER GENERATION", Technologies = new[] { Coal, Gas, Nuclear }, CapMw = 27100.0, Made = "inne MWE 27,1 GW (Tabela 3)" } } } },
            { CountryId.USA, new Published {
                Publisher = "LAWRENCE BERKELEY NATIONAL LABORATORY · QUEUED UP 2026", Whose = "BERKELEY LAB'S COMPILATION OF THE OPERATORS' QUEUES", AsOf = "END OF 2025", Holds = "ACTIVE PROJECTS AT 7 ISO/RTOs AND 47 OF 50 BALANCING AREAS · NO NATIONAL OPERATOR EXISTS",
                Lines = new[] {
                    new Line { Name = "WIND", Technologies = new[] { Wind }, CapMw = 219750.0, Made = "Wind 196.32 GW + Offshore Wind 23.43 GW" },
                    new Line { Name = "SOLAR", Technologies = new[] { Solar }, CapMw = 772570.0, Made = "Solar 772.57 GW" },
                    new Line { Name = "GAS", Technologies = new[] { Gas }, CapMw = 252830.0, Made = "Gas 252.83 GW" },
                    new Line { Name = "NUCLEAR", Technologies = new[] { Nuclear }, CapMw = 10390.0, Made = "Nuclear 10.39 GW" },
                    new Line { Name = "COAL", Technologies = new[] { Coal }, CapMw = 3700.0, Made = "Coal 3.70 GW" } } } },
            { CountryId.Germany, new Published {
                Publisher = "-", Whose = "-", AsOf = "-",
                Holds = "THE FOUR TSOs' ONE JOINT FIGURE IS MOSTLY BATTERIES AND IS NOT SPLIT · THE PERMIT PIPELINE IS NOT A GRID QUEUE · SOURCED, OR NOT DRAWN",
                BilledWhy = "NO GERMAN OPERATOR OR REGULATOR PUBLISHES A NATIONAL QUEUE FOR GENERATION" } },
        };

        /// <summary>What the country's operator publishes; null where the layer does not cover the country.</summary>
        public static Published Of(CountryId id) => Catalog.TryGetValue(id, out Published p) ? p : null;

        /// <summary>The publisher's line a technology stands in; null where the capacity is billed for it (no publisher, or a queue with no such line).</summary>
        public static Line LineOf(CountryId id, int technology)
        {
            Published p = Of(id);
            if (p == null || p.BilledWhy != null) { return null; }
            foreach (Line line in p.Lines) { if (Array.IndexOf(line.Technologies, technology) >= 0) { return line; } }
            return null;
        }

        /// <summary>The megawatts of BUILD orders standing in a line - placed, not landed - read off the country's own queue.</summary>
        public static double StandingMw(Country country, Line line)
        {
            if (country?.FleetOrders == null || line == null) { return 0.0; }
            double mw = 0;
            foreach (EnergyFleet.Order o in country.FleetOrders) { if (o != null && !o.Landed && o.Mw > 0 && Array.IndexOf(line.Technologies, o.Technology) >= 0) { mw += o.Mw; } }
            return mw;
        }

        /// <summary>The room a technology's line still has, MW - infinite where its capacity is billed.</summary>
        public static double RoomMw(Country country, int technology)
        {
            Line line = country != null ? LineOf(country.Id, technology) : null;
            return line == null ? double.PositiveInfinity : Math.Max(0.0, line.CapMw - StandingMw(country, line));
        }

        /// <summary>CONVENTION - the queue's grain is the whole megawatt: a line with less than one left is full (<see cref="FullText"/>), and the page's last step is the room rounded down.</summary>
        public const double GrainMw = 1.0;

        /// <summary>Why a build of <paramref name="mw"/> cannot stand in the queue, in the queue's own words (the ministry's deferral and the diagnostic read it; the page's line uses <see cref="FullText"/>) - null where it can (or where the capacity is billed). A retirement is never refused here.</summary>
        public static string RefusalFor(Country country, int technology, double mw)
        {
            if (country == null || mw <= 0) { return null; }
            Line line = LineOf(country.Id, technology);
            if (line == null) { return null; }
            double standing = StandingMw(country, line);
            if (standing + mw <= line.CapMw + 1e-6) { return null; }
            Published p = Of(country.Id);
            double room = Math.Max(0.0, line.CapMw - standing);
            // FULL is said only of a full queue: an order larger than the room left is past the ROOM, and the sentence says how much is left (the review: a first step into an empty
            // line read "THE QUEUE IS FULL · 0 MW STAND")
            return (room < GrainMw ? "THE QUEUE IS FULL · " + line.Name + " " + Mw(standing) + " MW STAND OF " + Mw(line.CapMw)
                               : "THE ORDER IS PAST THE QUEUE'S ROOM · " + line.Name + " " + Mw(room) + " MW LEFT OF " + Mw(line.CapMw)) + " · " + p.Whose + ", " + p.AsOf;
        }

        /// <summary>The technology line's own short sentence where the line has no room left at all - the publisher and the date stand on the queue's second line (<see cref="SourceText"/>),
        /// and a line of the plate holds some ninety characters at 1280. Null where a megawatt still fits, or where the capacity is billed.</summary>
        public static string FullText(Country country, int technology)
        {
            Line line = country != null ? LineOf(country.Id, technology) : null;
            if (line == null || RoomMw(country, technology) >= GrainMw) { return null; }
            return "THE QUEUE IS FULL · " + line.Name + " " + Mw(StandingMw(country, line)) + " OF " + Mw(line.CapMw) + " MW";
        }

        /// <summary>The step up the page offers: the country's step, or the line's remaining room where that is less - a step larger than the room IS the room, so a line that holds less
        /// than one step (the USA's coal and nuclear: a step is a per cent of the fleet) can still be filled, and the last step of any line lands exactly on its figure.</summary>
        public static double StepUpMw(Country country, int technology, double step)
        {
            double room = RoomMw(country, technology);
            return room < step ? Math.Floor(room) : step;
        }

        /// <summary>The page's line under the queue's caption: what the queue holds, line by line - or that its capacity is billed, and why.</summary>
        public static string CapacityText(Country country)
        {
            Published p = country != null ? Of(country.Id) : null;
            if (p == null) { return "THE LAYER DOES NOT COVER THIS COUNTRY"; }
            if (p.BilledWhy != null) { return "CAPACITY BILLED · " + p.BilledWhy + " · AN ORDER OF ANY SIZE STANDS"; }
            var parts = new List<string>();
            foreach (Line line in p.Lines) { parts.Add(line.Name + " " + Mw(StandingMw(country, line)) + " OF " + Mw(line.CapMw) + " MW"); }
            return "HOLDS · " + string.Join(" · ", parts);
        }

        /// <summary>The page's second line: whose queue the figures are, what it holds in the publisher's own definition, and what it has no line for.</summary>
        public static string SourceText(Country country)
        {
            Published p = country != null ? Of(country.Id) : null;
            if (p == null) { return "-"; }
            if (p.BilledWhy != null) { return p.Holds; }   // a billed country's own sentence: what exists instead, and why it is not a queue
            return p.Whose + ", " + p.AsOf + " · " + p.Holds;
        }

        /// <summary>For a technology's own line on the page: what stands where the published queue has NO line for it - null where it has one, or where no capacity is published at all.</summary>
        public static string NoLineText(CountryId id, int technology)
        {
            Published p = Of(id);
            return p != null && p.BilledWhy == null && LineOf(id, technology) == null ? "NO LINE IN THE PUBLISHED QUEUE · CAPACITY BILLED, ANY SIZE STANDS" : null;
        }

        /// <summary>Whole megawatts with a space for the thousands - the plate's one way of printing a megawatt figure (the queue's figures run to six digits).</summary>
        public static string Mw(double mw) => Math.Round(mw).ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");
    }
}
