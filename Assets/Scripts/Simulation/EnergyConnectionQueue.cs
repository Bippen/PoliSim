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
    /// <para><b>THE THIRD KIND OF SOURCE (ruled by Elias 2026-09-21, built §575): A LEGISLATED LIMIT ON ANNUAL CONNECTIONS.</b> A figure here is one of three kinds, and the page
    /// names which (<see cref="SourceKind"/>): an OPERATOR QUEUE, a STATUTE, or BILLED. The two sourced kinds are counted differently because they are different things, and that
    /// difference is the whole of what the kind means. An operator's queue is a STOCK - the projects it has in process at a date - so what stands against it is every build order
    /// placed and not yet landed, and a landed order gives its megawatts back. A statute's tender volume is a FLOW - what may be awarded a connection in one calendar year - so
    /// what stands against it is every build order placed IN THAT YEAR, landed or not, because a volume is taken when it is awarded and not when the plant connects; and a year
    /// that ends gives nothing back, because the next year's volume is its own. ⚠ Neither kind is the better source and neither is converted into the other: a stock is not an
    /// annual rate and an annual rate is not a stock, and any arithmetic between them here would be authored.</para>
    ///
    /// <para><b>What was fetched, 2026-09-21, and what each publisher means by its queue</b> (the definitions differ and are printed, never harmonised):
    /// ENTSO-E publishes NO connection figure - its Power Statistics are installed stock and ERAA 2025 / TYNDP 2024 are TSO-submitted trajectories (three independent readers, the
    /// Transparency Platform's 14.1.A unreadable without a token). So every figure here is an operator's own queue, a national laboratory's compilation of them, or a statute:
    /// SWEDEN - Svenska kraftnät, *Ansökt effekt per anslutningstyp*, per 1 September 2026: applications to the transmission grid until capacity is reserved; offshore wind is
    /// handled in a separate process and is NOT in it. FRANCE - RTE, Enedis, Agence ORE and the SER, *Panorama de l'électricité renouvelable au 31 décembre 2025*: projects with an
    /// accepted queue-entry or technical-and-financial offer, or RETAINED IN A TENDER (RTE), or a complete request (Enedis, the local operators, EDF SEI) - the glossary's own three
    /// limbs; all grids; renewables only. ⚠ Its offshore HEADLINE (9 278 MW) adds 5 850 MW of
    /// tenders still to come, which its own glossary does not call queued: the line here takes the chapter's 3 398 MW of projects in development (the F2e review's finding). ITALY - Terna, the Econnextion dashboard's dataset at 31 August 2026: every request to the national transmission grid whatever its status,
    /// renewables and storage only. POLAND - PSE, *Wykaz Zagregowanych Informacji o Przyłączeniach* (transmission at 31 July 2026, distribution above 1 kV at 30 June 2026),
    /// Tabela 3 and Tabela 4: connection conditions issued plus agreements in force - ⚠ Tabela 4's conditions row counts offshore wind's PRELIMINARY conditions, by PSE's own
    /// footnote, some 6,8 GW of the wind line: the publisher's row as printed, disclosed on the page, and Elias's to strike; the register's MIX column (hybrid sites, 71,4 GW of injection) names no single technology and
    /// stands in no line here, so the PV and wind lines understate what Poland's operators have in process. THE USA - there is no national operator; Lawrence Berkeley National Laboratory's *Queued Up:
    /// 2026 Edition* compiles queue data from seven ISO/RTOs and fifty balancing areas (some 98 % of installed capacity; forty-seven of the fifty had active requests) at the end of 2025.</para>
    ///
    /// <para><b>GERMANY - THE STATUTE, not an operator</b> (§575, and the reason the third kind exists). No German operator or regulator publishes a national queue for
    /// generation: the four TSOs' one joint figure (717 applications, about 270 GW, end of the third quarter of 2025) is 211 GW of batteries and an unsplit remainder of consumers
    /// and generators on the transmission grid alone, and the Bundesnetzagentur's permitted-and-not-commissioned wind is a permit pipeline, not a grid queue. What Germany DOES
    /// publish is a law. The EEG 2023 names, per calendar year, the installed capacity that may be tendered - § 28 Abs. 2 for onshore wind, § 28a Abs. 2 for the first segment's
    /// ground-mounted solar, each spread equally over that section's own Gebotstermine (four a year and three a year, Abs. 1 of each). Those are the figures Germany carries, as a
    /// statute and said on the page to be one. ⚠ They bind onshore wind and ground-mounted solar ONLY: offshore wind is auctioned under the WindSeeG and the second segment's
    /// rooftop solar under § 28b, and neither stands in a line here. Where a source has no line for a technology (France's and Italy's gas and nuclear: their queues are the
    /// renewables'; Germany's coal and gas, which the EEG's tenders do not reach), that technology's capacity is BILLED too, and an order of any size stands - which the page says.</para>
    ///
    /// <para><b>The several-worlds class (§544).</b> The figures are a catalog; what stands against them is read off the COUNTRY's own orders. Nothing here is keyed by a country's
    /// id and written at run time.</para>
    ///
    /// <para><b>A STATUTE'S YEAR IS THE CALENDAR'S, AND THE GAME'S TURN IS 365 DAYS (§575, disclosed).</b> The turn's year drifts against the calendar by about a
    /// quarter of a day a turn (the boundary EnergyFleet.Advance counts is 365 days; the calendar's year is not), so across a century two turns can report the same calendar
    /// year and another can be skipped. A statute's volume is the CALENDAR year's, so two turns inside one calendar year SHARE that year's volume and a skipped year's volume
    /// is never drawn. That is the statute read as written rather than a per-turn allowance invented to be tidy, and it is what the page prints: the year it names is the year
    /// it counted.</para>
    ///
    /// <para><b>THE YEAR IS PASSED, NEVER INFERRED (§575).</b> Every read that can depend on a year takes it as an argument - the year the order being tested will CARRY, which is
    /// what <c>EnergyFleet.Place</c> writes into it. It is NOT read off <c>Country.CalendarYear</c> here, and the reason is measured: the AI ministry decides AT the boundary for
    /// the year about to be played (<c>SimulationManager</c> passes the turn after this one while the country still carries the year just finished), so a rule that inferred the
    /// year would have counted the ministry's orders against a year they were not placed in - and a per-year volume that never sees its own orders is no limit at all.</para>
    /// </summary>
    public static class EnergyConnectionQueue
    {
        /// <summary>What a country's figure IS, named on the page - and what a megawatt is counted against (the class note's third paragraph).</summary>
        public enum SourceKind
        {
            /// <summary>A grid operator's own published queue: a STOCK of projects in process. Counted: builds placed and not yet landed.</summary>
            OperatorQueue = 0,
            /// <summary>A statute's own annual tender volumes: a legislated limit on what may be connected in a calendar year, a FLOW. Counted: builds placed in that year, landed or not.</summary>
            Statute = 1,
            /// <summary>No source publishes one at all: no capacity applies and the page says why (<see cref="Published.BilledWhy"/>).</summary>
            Billed = 2,
        }

        /// <summary>One line of a publisher's queue - or of a statute's schedule - mapped onto the layer's technologies.</summary>
        public sealed class Line
        {
            /// <summary>One calendar year's figure in a STATUTE line's schedule (<see cref="Schedule"/>).</summary>
            public sealed class Volume
            {
                /// <summary>The first year the statute names this figure for.</summary>
                public int Year;
                /// <summary>The LAST year the statute names this figure for - what the law speaks to, as distinct from where the figure stops changing. Past the last <c>Through</c> of a line's schedule the volume is a CONVENTION, and the page discloses it in those years.</summary>
                public int Through;
                /// <summary>The installed capacity the statute names for that year, MW.</summary>
                public double Mw;
            }

            /// <summary>The line in the page's words.</summary>
            public string Name;
            /// <summary>The layer's labels that stand in it (EnergyLayerData.Labels: coal 0, gas 1, nuclear 2, wind 4, solar 5).</summary>
            public int[] Technologies;
            /// <summary>SOURCED - the publisher's figure, MW; where it is a sum of the publisher's cells, <see cref="Made"/> shows the addends. On a STATUTE line it is the figure of
            /// the LAST year the schedule names, which is also the one that stands on past it - every reader goes through <see cref="CapMwIn"/>, and the diagnostic asserts the two agree.</summary>
            public double CapMw;
            /// <summary>The publisher's own cells the figure is made of, for the record and the diagnostic.</summary>
            public string Made;
            /// <summary>A STATUTE line's figure by calendar year, ascending - null on an operator's line, which publishes one figure at one date.</summary>
            public Volume[] Schedule;
            /// <summary>The last year the statute names a volume for on THIS line - 0 on an operator's line. Past it the line runs on the convention
            /// (<see cref="CapMwIn"/>), and the page says so on the line itself (<see cref="CapacityText"/>).</summary>
            public int NamedThrough => Schedule == null || Schedule.Length == 0 ? 0 : Schedule[Schedule.Length - 1].Through;

            /// <summary>
            /// The line's figure in <paramref name="year"/>: the publisher's one figure where there is no schedule, else the statute's own volume for that year.
            /// <para>⚠ CONVENTION, and the page says it: a statute names volumes for the years it names, and this game runs a century. Inside the schedule the figure is SOURCED;
            /// past its last year THE LAST VOLUME STANDS ON, and before its first the first does. Neither is the statute speaking - a legislature that has not legislated a year
            /// cannot be quoted for it - and the alternative (no limit at all past 2028) would be authored too, and a looser claim.</para>
            /// </summary>
            public double CapMwIn(int year)
            {
                if (Schedule == null || Schedule.Length == 0) { return CapMw; }
                double mw = Schedule[0].Mw;
                foreach (Volume v in Schedule) { if (year >= v.Year) { mw = v.Mw; } }
                return mw;
            }
        }

        /// <summary>What one country publishes - an operator's queue, a statute's volumes, or why neither exists.</summary>
        public sealed class Published
        {
            /// <summary>Which of the three kinds this row is - printed on the page, and what <see cref="TakenMw"/> counts by.</summary>
            public SourceKind Kind;
            public string Publisher;
            /// <summary>Whose the figures are, in the page's words - an operator's own queue, a compilation of operators', or the statute's own volumes.</summary>
            public string Whose;
            public string AsOf;
            /// <summary>What the source holds, in its own definition, for the page's second line.</summary>
            public string Holds;
            public Line[] Lines = new Line[0];
            /// <summary>Non-null where no source publishes a figure at all: no capacity applies, and the page says why.</summary>
            public string BilledWhy;
        }

        private const int Coal = 0, Gas = 1, Nuclear = 2, Wind = 4, Solar = 5;   // CONVENTION - the layer's labels by index

        private static readonly Dictionary<CountryId, Published> Catalog = new Dictionary<CountryId, Published>
        {
            { CountryId.Sweden, new Published {
                Kind = SourceKind.OperatorQueue,
                Publisher = "SVENSKA KRAFTNÄT", Whose = "SVENSKA KRAFTNÄT'S OWN QUEUE", AsOf = "1 SEPT 2026", Holds = "APPLICATIONS TO THE TRANSMISSION GRID UNTIL CAPACITY IS RESERVED · OFFSHORE WIND IS OUTSIDE IT",
                Lines = new[] {
                    new Line { Name = "WIND", Technologies = new[] { Wind }, CapMw = 9004.0, Made = "Landbaserad vindkraft 9 004 MW" },
                    new Line { Name = "SOLAR", Technologies = new[] { Solar }, CapMw = 4844.0, Made = "Solkraft 4 844 MW" },
                    new Line { Name = "OTHER PRODUCTION", Technologies = new[] { Coal, Gas, Nuclear }, CapMw = 4348.0, Made = "Övrig elproduktion 4 348 MW" } } } },
            { CountryId.France, new Published {
                Kind = SourceKind.OperatorQueue,
                Publisher = "RTE · ENEDIS · AGENCE ORE · SER", Whose = "THE GRID OPERATORS' QUEUE IN THE PANORAMA", AsOf = "31 DEC 2025", Holds = "OFFER ACCEPTED, TENDER WON OR REQUEST COMPLETE, ALL GRIDS · RENEWABLES ONLY",
                Lines = new[] {
                    new Line { Name = "WIND", Technologies = new[] { Wind }, CapMw = 16934.0, Made = "éolien terrestre 13 536 MW + éolien en mer 3 398 MW of projets en développement - NOT the headline's 9 278 MW, which adds 5 850 MW of appels d'offres à venir that the Panorama's own glossary does not count as queued (and the chapter's two cells sum 30 MW short of its headline, unexplained)" },
                    new Line { Name = "SOLAR", Technologies = new[] { Solar }, CapMw = 35078.0, Made = "solaire 35 078 MW" } } } },
            { CountryId.Italy, new Published {
                Kind = SourceKind.OperatorQueue,
                Publisher = "TERNA · ECONNEXTION", Whose = "TERNA'S OWN QUEUE (ECONNEXTION)", AsOf = "31 AUG 2026", Holds = "EVERY REQUEST TO THE TRANSMISSION GRID, WHATEVER ITS STATUS · RENEWABLES AND STORAGE ONLY",
                Lines = new[] {
                    new Line { Name = "WIND", Technologies = new[] { Wind }, CapMw = 168872.0, Made = "eolico on-shore 104 657 MW (five statuses) + eolico off-shore 64 215 MW (four: none stands at STMD/Contratti), each summed over the dataset's statuses" },
                    new Line { Name = "SOLAR", Technologies = new[] { Solar }, CapMw = 140312.0, Made = "solare 140 312 MW, the sum of the dataset's five statuses" } } } },
            { CountryId.Poland, new Published {
                Kind = SourceKind.OperatorQueue,
                Publisher = "PSE", Whose = "PSE'S OWN REGISTER", AsOf = "31 JULY 2026", Holds = "CONDITIONS ISSUED (OFFSHORE: PRELIMINARY TOO) PLUS AGREEMENTS IN FORCE · TRANSMISSION; DISTRIBUTION >1 kV AT 30 JUNE",
                Lines = new[] {
                    new Line { Name = "WIND", Technologies = new[] { Wind }, CapMw = 24600.0, Made = "FW 7,5 GW (Tabela 3) + morskie farmy wiatrowe 17,1 GW (Tabela 4: 8,7 GW at the conditions stage, which PSE's footnote 11 says INCLUDES PRELIMINARY conditions - its project register of the same date lists 1 875 MW with conditions issued - plus 8,4 GW of agreements in force)" },
                    new Line { Name = "SOLAR", Technologies = new[] { Solar }, CapMw = 34000.0, Made = "PV 34 GW (Tabela 3)" },
                    new Line { Name = "OTHER GENERATION", Technologies = new[] { Coal, Gas, Nuclear }, CapMw = 27100.0, Made = "inne MWE 27,1 GW (Tabela 3)" } } } },
            { CountryId.USA, new Published {
                Kind = SourceKind.OperatorQueue,
                Publisher = "LAWRENCE BERKELEY NATIONAL LABORATORY · QUEUED UP 2026", Whose = "BERKELEY LAB'S COMPILATION OF THE OPERATORS' QUEUES", AsOf = "END OF 2025", Holds = "ACTIVE PROJECTS AT 7 ISO/RTOs AND 47 OF 50 BALANCING AREAS · NO NATIONAL OPERATOR EXISTS",
                Lines = new[] {
                    new Line { Name = "WIND", Technologies = new[] { Wind }, CapMw = 219750.0, Made = "Wind 196.32 GW + Offshore Wind 23.43 GW" },
                    new Line { Name = "SOLAR", Technologies = new[] { Solar }, CapMw = 772570.0, Made = "Solar 772.57 GW" },
                    new Line { Name = "GAS", Technologies = new[] { Gas }, CapMw = 252830.0, Made = "Gas 252.83 GW" },
                    new Line { Name = "NUCLEAR", Technologies = new[] { Nuclear }, CapMw = 10390.0, Made = "Nuclear 10.39 GW" },
                    new Line { Name = "COAL", Technologies = new[] { Coal }, CapMw = 3700.0, Made = "Coal 3.70 GW" } } } },
            // GERMANY - THE THIRD KIND (§575). Not an operator's queue, because no German operator publishes one: the EEG 2023's own annual tender volumes, a limit on connections
            // that a legislature wrote rather than a grid that measured itself. Counted per calendar year, and the page says which kind it is.
            { CountryId.Germany, new Published {
                Kind = SourceKind.Statute,
                Publisher = "EEG 2023 § 28, § 28a", Whose = "THE STATUTE'S OWN ANNUAL TENDER VOLUMES", AsOf = "VOLUMES NAMED TO 2028 (WIND) AND 2029 (SOLAR)",
                Holds = "A STATUTE, NOT AN OPERATOR'S QUEUE · WHAT MAY BE AWARDED A CONNECTION IN A CALENDAR YEAR · ONSHORE WIND AND GROUND-MOUNTED SOLAR ONLY",
                Lines = new[] {
                    new Line { Name = "WIND", Technologies = new[] { Wind }, CapMw = 10000.0,
                        Made = "EEG 2023 § 28 Abs. 2: 12 840 MW zu installierende Leistung in 2023, then 10 000 MW in each of 2024-2028, spread equally over Abs. 1's four Gebotstermine (1 Feb, 1 May, 1 Aug, 1 Nov) - ONSHORE ONLY: offshore wind is auctioned under the WindSeeG and stands in no line here",
                        Schedule = new[] {
                            new Line.Volume { Year = 2023, Through = 2023, Mw = 12840.0 },
                            new Line.Volume { Year = 2024, Through = 2028, Mw = 10000.0 } } },
                    new Line { Name = "SOLAR", Technologies = new[] { Solar }, CapMw = 9900.0,
                        Made = "EEG 2023 § 28a Abs. 2: 5 850 MW zu installierende Leistung in 2023, 8 100 MW in 2024, then 9 900 MW in each of 2025-2029, spread equally over Abs. 1's three Gebotstermine (1 Mar, 1 July, 1 Dec) - the FIRST SEGMENT (ground-mounted) only: the second segment's rooftop volumes are § 28b's and stand in no line here",
                        Schedule = new[] {
                            new Line.Volume { Year = 2023, Through = 2023, Mw = 5850.0 },
                            new Line.Volume { Year = 2024, Through = 2024, Mw = 8100.0 },
                            new Line.Volume { Year = 2025, Through = 2029, Mw = 9900.0 } } } } } },
        };

        /// <summary>What the country publishes; null where the layer does not cover the country.</summary>
        public static Published Of(CountryId id) => Catalog.TryGetValue(id, out Published p) ? p : null;

        /// <summary>The line a technology stands in; null where the capacity is billed for it (no source, or a source with no such line).</summary>
        public static Line LineOf(CountryId id, int technology)
        {
            Published p = Of(id);
            if (p == null || p.BilledWhy != null) { return null; }
            foreach (Line line in p.Lines) { if (Array.IndexOf(line.Technologies, technology) >= 0) { return line; } }
            return null;
        }

        /// <summary>
        /// The megawatts of BUILD orders standing against a line's figure for <paramref name="year"/> - ONE counting rule, and the KIND decides what it counts (the class note's
        /// third paragraph): an operator's queue holds the stock placed and not yet landed, a statute's volume the flow placed in that calendar year, landed or not. Read off the
        /// COUNTRY's own orders, never a table. A retirement is not a connection and is counted by neither.
        /// </summary>
        public static double TakenMw(Country country, Line line, int year)
        {
            if (country?.FleetOrders == null || line == null) { return 0.0; }
            bool perYear = (Of(country.Id)?.Kind ?? SourceKind.OperatorQueue) == SourceKind.Statute;
            double mw = 0;
            foreach (EnergyFleet.Order o in country.FleetOrders)
            {
                if (o == null || o.Mw <= 0 || Array.IndexOf(line.Technologies, o.Technology) < 0) { continue; }
                if (perYear ? o.OrderedYear == year : !o.Landed) { mw += o.Mw; }
            }
            return mw;
        }

        /// <summary>The room a technology's line still has in <paramref name="year"/>, MW - infinite where its capacity is billed.</summary>
        public static double RoomMw(Country country, int technology, int year)
        {
            Line line = country != null ? LineOf(country.Id, technology) : null;
            return line == null ? double.PositiveInfinity : Math.Max(0.0, line.CapMwIn(year) - TakenMw(country, line, year));
        }

        /// <summary>CONVENTION - the grain is the whole megawatt: a line with less than one left is full (<see cref="FullText"/>), and the page's last step is the room rounded down.</summary>
        public const double GrainMw = 1.0;

        /// <summary>Why a build of <paramref name="mw"/> placed in <paramref name="year"/> cannot stand, in the source's own terms (the ministry's deferral and the diagnostic read
        /// it; the page's line uses <see cref="FullText"/>) - null where it can, or where the capacity is billed. A retirement is never refused here. A queue is FULL or has ROOM;
        /// a statute's volume is TAKEN or has a remainder FOR THAT YEAR - a player told "the queue is full" of a limit that empties on 1 January would have been told the wrong thing.</summary>
        public static string RefusalFor(Country country, int technology, double mw, int year)
        {
            if (country == null || mw <= 0) { return null; }
            Line line = LineOf(country.Id, technology);
            if (line == null) { return null; }
            double cap = line.CapMwIn(year), taken = TakenMw(country, line, year);
            if (taken + mw <= cap + 1e-6) { return null; }
            Published p = Of(country.Id);
            bool statute = p.Kind == SourceKind.Statute;
            double room = Math.Max(0.0, cap - taken);
            // FULL is said only of a full line: an order larger than the room left is past the ROOM, and the sentence says how much is left (the F2e review's finding - a first
            // step into an empty line read "THE QUEUE IS FULL · 0 MW STAND")
            string said = room < GrainMw
                ? (statute ? "THE YEAR'S TENDER VOLUME IS TAKEN · " + line.Name + " " + Mw(taken) + " MW AWARDED OF " + Mw(cap) + " FOR " + year
                           : "THE QUEUE IS FULL · " + line.Name + " " + Mw(taken) + " MW STAND OF " + Mw(cap))
                : (statute ? "THE ORDER IS PAST THE YEAR'S TENDER VOLUME · " + line.Name + " " + Mw(room) + " MW LEFT OF " + Mw(cap) + " FOR " + year
                           : "THE ORDER IS PAST THE QUEUE'S ROOM · " + line.Name + " " + Mw(room) + " MW LEFT OF " + Mw(cap));
            return said + " · " + (statute ? p.Publisher : p.Whose + ", " + p.AsOf);
        }

        /// <summary>The technology line's own short sentence where the line has no room left at all in <paramref name="year"/> - the source and the date stand on the second line
        /// (<see cref="SourceText"/>), and a line of the plate holds some ninety characters at 1280. Null where a megawatt still fits, or where the capacity is billed.</summary>
        public static string FullText(Country country, int technology, int year)
        {
            Line line = country != null ? LineOf(country.Id, technology) : null;
            if (line == null || RoomMw(country, technology, year) >= GrainMw) { return null; }
            return (Of(country.Id).Kind == SourceKind.Statute
                       ? "THE YEAR'S VOLUME IS TAKEN · " + line.Name + " " + Mw(TakenMw(country, line, year)) + " OF " + Mw(line.CapMwIn(year)) + " MW FOR " + year
                       : "THE QUEUE IS FULL · " + line.Name + " " + Mw(TakenMw(country, line, year)) + " OF " + Mw(line.CapMwIn(year)) + " MW");
        }

        /// <summary>The step up the page offers: the country's step, or the line's remaining room where that is less - a step larger than the room IS the room, so a line that holds less
        /// than one step (the USA's coal and nuclear: a step is a per cent of the fleet) can still be filled, and the last step of any line lands exactly on its figure.</summary>
        public static double StepUpMw(Country country, int technology, double step, int year)
        {
            double room = RoomMw(country, technology, year);
            return room < step ? Math.Floor(room) : step;
        }

        /// <summary>The page's line under the caption: what the source allows and what stands against it, line by line - or that the capacity is billed, and why.</summary>
        public static string CapacityText(Country country, int year)
        {
            Published p = country != null ? Of(country.Id) : null;
            if (p == null) { return "THE LAYER DOES NOT COVER THIS COUNTRY"; }
            if (p.BilledWhy != null) { return "CAPACITY BILLED · " + p.BilledWhy + " · AN ORDER OF ANY SIZE STANDS"; }
            var parts = new List<string>();
            bool statute = p.Kind == SourceKind.Statute;
            foreach (Line line in p.Lines)
            {
                string part = line.Name + " " + Mw(TakenMw(country, line, year)) + " OF " + Mw(line.CapMwIn(year)) + " MW";
                // §575: the convention is disclosed ON THE LINE it applies to, in the years it applies to. One statute's lines stop at different years
                // (§ 28's wind at 2028, § 28a's solar at 2029): the first cut said it once for the row past the LAST of them, and stayed silent through
                // 2029 while wind already ran on the convention - the item's filmed width caught it on a 2029 frame, which the review had not.
                if (statute && line.NamedThrough > 0 && year > line.NamedThrough) { part += " (" + line.NamedThrough + "'S VOLUME, BY CONVENTION)"; }
                parts.Add(part);
            }
            return (statute ? "AWARDED IN " + year + " · " : "HOLDS · ") + string.Join(" · ", parts);
        }

        /// <summary>The page's second line: whose the figures are, what the source holds in its own definition, and - where it is a statute - the law and the years it names.</summary>
        public static string SourceText(Country country)
        {
            Published p = country != null ? Of(country.Id) : null;
            if (p == null) { return "-"; }
            if (p.BilledWhy != null) { return p.Holds; }   // a billed country's own sentence: what exists instead, and why it is not a source
            // §575: this line is DRAWN, and its budget is the longest one already on the page (the USA's 155 characters at 1280) - so the statute's line is the law and what it holds, and Whose and AsOf stay for the record, the diagnostic and the refusal. The kind is said in Holds.
            if (p.Kind == SourceKind.Statute) { return p.Publisher + " · " + p.Holds; }
            return p.Whose + ", " + p.AsOf + " · " + p.Holds;
        }

        /// <summary>For a technology's own line on the page: what stands where the source has NO line for it - null where it has one, or where no capacity is published at all.</summary>
        public static string NoLineText(CountryId id, int technology)
        {
            Published p = Of(id);
            if (p == null || p.BilledWhy != null || LineOf(id, technology) != null) { return null; }
            return (p.Kind == SourceKind.Statute ? "THE STATUTE NAMES NO VOLUME FOR IT" : "NO LINE IN THE PUBLISHED QUEUE") + " · CAPACITY BILLED, ANY SIZE STANDS";
        }

        /// <summary>Whole megawatts with a space for the thousands - the plate's one way of printing a megawatt figure (the queue's figures run to six digits).</summary>
        public static string Mw(double mw) => Math.Round(mw).ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");
    }
}
