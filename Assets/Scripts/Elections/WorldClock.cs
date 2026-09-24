using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-1 (2026-09-25, `docs/specs/POLITICAL_SYSTEM_SPEC.md` §3-§4, §9 stage 1; the rulings of `COMPLETED.md` §617/§618): THE WORLD CLOCK.
    /// One world on one calendar, whose start is set by the country the player chooses; on that date every country is seated in its
    /// chamber of record and governed by its government of record. Every date here is a fact read from a `records_by_date.md` under
    /// `ElectionsData/&lt;country&gt;/` (the source ids in square brackets are that file's register), never typed from memory.
    ///
    /// <para><b>The start rule (ruled).</b> A scheduled election starts at the standard run-up - the campaign calendar's pre-campaign
    /// start before the country's own polling day (`CampaignCalendar.DefaultPreCampaignWeeks` + `DefaultCampaignWeeks` before it). A snap
    /// election starts on <b>the first day a primary source records that an early election was set in motion</b>: Germany 2024-11-06
    /// (the chancellor's request and statement), Italy 2022-07-21 (the dissolution decree; the 14 July resignation was refused and 20 July
    /// rests on a secondary source). France's legislative start stays locked (R-EL10): governing mode opens 2024-07-18, the XVIIe's first
    /// sitting, with the government then in office.</para>
    ///
    /// <para><b>The chamber of record is seated AS ELECTED</b> - the election's lists, for every country; a mid-term club or group is a
    /// stated deviation in the record, never seated. A chamber whose per-list table is not yet sourced (Italy's 2018 Camera, France's
    /// 2022 Assembly by nuance - E-47) is reported as such: the country seats its latest sourced table and its view says so.</para>
    ///
    /// <para><b>Every election on a country's calendar inside a run is simulated once that country's model exists</b>; until then its chamber
    /// and head of state hold as of record, and its view says so. PS-2 / CL-4 (§619): Sweden's calendar is modelled - `TryNextPollingDay`, the statute's second Sunday of September every fourth year - and the game votes on it; the five others offer no polling day yet.</para>
    /// </summary>
    public static class WorldClock
    {
        /// <summary>One chamber of record: the election whose lists seat it, from the day it convened until the next convened.</summary>
        public readonly struct ChamberOfRecord
        {
            public readonly ElectionVintage Vintage;
            public readonly DateTime ElectionDay;
            public readonly DateTime Convened;
            /// <summary>Exclusive: the day the next chamber convened, or MaxValue while this one sits.</summary>
            public readonly DateTime Until;
            /// <summary>The source id(s) in the country's `records_by_date.md`.</summary>
            public readonly string Basis;

            public ChamberOfRecord(ElectionVintage vintage, DateTime electionDay, DateTime convened, DateTime until, string basis)
            {
                Vintage = vintage; ElectionDay = electionDay; Convened = convened; Until = until; Basis = basis;
            }

            public bool Holds(DateTime date) => date >= Convened && date < Until;
        }

        /// <summary>One government of record, by key, from the day it took office until it left; support is the parties carrying it from outside.</summary>
        public readonly struct GovernmentOfRecord
        {
            public readonly string Head;
            public readonly string[] Cabinet;
            public readonly string[] Support;
            public readonly DateTime From;
            public readonly DateTime Until;
            /// <summary>False where the record cannot name the cabinet's parties (a GAP): the formation's result stands in, marked provisional.</summary>
            public readonly bool CabinetSourced;
            /// <summary>True where the record names the cabinet's parties by a DERIVATION it states (the ministers' clubs, a motion's signatories) rather than a page that lists them; the standing says so.</summary>
            public readonly bool CabinetDerived;
            public readonly string Basis;

            public GovernmentOfRecord(string head, string[] cabinet, string[] support, DateTime from, DateTime until, bool cabinetSourced, string basis, bool cabinetDerived = false)
            {
                Head = head; Cabinet = cabinet; Support = support; From = from; Until = until; CabinetSourced = cabinetSourced; Basis = basis; CabinetDerived = cabinetDerived;
            }

            public bool Holds(DateTime date) => date >= From && date < Until;
        }

        private static readonly DateTime Open = DateTime.MaxValue;
        private static DateTime D(int y, int m, int d) => new DateTime(y, m, d);

        /// <summary>The polling day each country's start is cut on (the latest election of its kind, §4), or the snap trigger.</summary>
        public static DateTime LatestElectionDay(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden:  return D(2026, 9, 13);   // Valmyndigheten's result fixed 2026-09-19 (2026/returns_2026.md)
                case CountryId.Germany: return D(2025, 2, 23);   // BGBl. 2024 I Nr. 435 [germany records §0]
                case CountryId.Poland:  return D(2023, 10, 15);  // the PKW's Sejm notice, Dz.U. 2023 poz. 2234 (returns_2023.md; the Senate's is poz. 2235)
                case CountryId.Italy:   return D(2022, 9, 25);   // DPR 97/2022 [DPR-97]
                case CountryId.USA:     return D(2024, 11, 5);   // NARA's Electoral College calendar [EC-DATES]
                case CountryId.France:  return D(2024, 7, 7);    // the Élysée's address of 2024-06-09 names 30 June and 7 July [EL-DIS]
                default: return D(2026, 10, 1);
            }
        }

        /// <summary>True for a start that opens on a snap election's trigger day rather than at the standard run-up.</summary>
        public static bool IsSnapStart(CountryId id) => id == CountryId.Germany || id == CountryId.Italy;

        /// <summary>§8: France is selectable in governing mode only - its government of record, no election - until its two-round, 577-constituency system is modelled (R-EL10).</summary>
        public static bool GoverningModeOnly(CountryId id) => id == CountryId.France;

        /// <summary>The date the world opens on when this country is chosen, by the start rule above.</summary>
        public static DateTime StartDate(CountryId id)
        {
            switch (id)
            {
                case CountryId.Germany: return D(2024, 11, 6);   // ruled: the chancellor's request and statement of 6 Nov 2024 [BREG-ST24]; the FDP ministers were dismissed the next day [BP-ENT24]
                case CountryId.Italy:   return D(2022, 7, 21);   // ruled: DPR 96/2022, the dissolution, "Dato a Roma, addì 21 luglio 2022" [DPR-96]
                case CountryId.France:  return D(2024, 7, 18);   // ruled: governing mode opens at the XVIIe's first sitting [AN-S18]
                default:
                    return new CampaignCalendar(LatestElectionDay(id)).PreCampaignStart;   // the standard run-up before the country's own polling day
            }
        }

        /// <summary>What the start is, in the selector's words - one line, no number the record does not hold.</summary>
        public static string StartLine(CountryId id)
        {
            DateTime start = StartDate(id);
            string opens = start.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant();
            if (GoverningModeOnly(id)) { return $"OPENS {opens} · GOVERNING MODE · NO ELECTION"; }   // the card's one line at 1280: "MODELLED" wrapped and clipped
            string polling = LatestElectionDay(id).ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant();
            return IsSnapStart(id)
                ? $"OPENS {opens} · THE SNAP ELECTION OF {polling}"
                : $"OPENS {opens} · THE RUN-UP TO {polling}";
        }

        /// <summary>The chambers of record per country, oldest first, as the records date them.</summary>
        public static IReadOnlyList<ChamberOfRecord> Chambers(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden:
                    return new[]
                    {
                        new ChamberOfRecord(ElectionVintage.Sweden2018, D(2018, 9, 9), D(2018, 9, 24), D(2022, 9, 26), "[VAL-18] (election day); convened DERIVED as 2018-09-09 + 15 days by RF 3:10 - the record dates no 2018 upprop, and no start falls before 2022-09-26; until the 2022 upprop [RD-KAL22] [RD-PROT1]"),
                        new ChamberOfRecord(ElectionVintage.Sweden2022, D(2022, 9, 11), D(2022, 9, 26), D(2026, 9, 28), "returns_2022.md; election day [RD-N1017]; the upprop 2022-09-26 11.00 [RD-KAL22] [RD-PROT1]; the 2026 upprop, scheduled [RD-N24] [RD-KAL26]"),
                        new ChamberOfRecord(ElectionVintage.Sweden2026, D(2026, 9, 13), D(2026, 9, 28), Open, "2026/returns_2026.md; RF 3:10, the upprop 2026-09-28 [RD-N24] [RD-KAL26] (and 2026/government_2026.md's [RD-N19] [RD-N21])"),
                    };
                case CountryId.Germany:
                    return new[]
                    {
                        new ChamberOfRecord(ElectionVintage.Germany2021, D(2021, 9, 26), D(2021, 10, 26), D(2025, 3, 25), "germany/records_by_date.md §1.1 (736; 735 from 2024-03-01 [BT-WW24]); constituent sitting [BT-K20]; on a start inside its term the record's chamber is 735 with FDP 91 and a BSW Gruppe of ten - seated as elected by ruling"),
                        new ChamberOfRecord(ElectionVintage.Germany2025, D(2025, 2, 23), D(2025, 3, 25), Open, "returns_2025.md; election day [BWL-WT25]; the constituent sitting 2025-03-25 [BT-K21]"),
                    };
                case CountryId.Poland:
                    return new[]
                    {
                        new ChamberOfRecord(ElectionVintage.Poland2019, D(2019, 10, 13), D(2019, 11, 12), D(2023, 11, 13), "the PKW notice Dz.U. 2019 poz. 1955 [PKW-2019]; the Sejm API's term dates [API-TERM]"),
                        new ChamberOfRecord(ElectionVintage.Poland2023, D(2023, 10, 15), D(2023, 11, 13), Open, "returns_2023.md; the 10th term's first sitting 2023-11-13 [API-TERM] [API-V1]"),
                    };
                case CountryId.Italy:
                    return new[]
                    {
                        new ChamberOfRecord(ElectionVintage.Italy2018, D(2018, 3, 4), D(2018, 3, 23), D(2022, 10, 13), "[ST-18] [GV-LEG]; per-list seats NOT SOURCED (E-47; by group at formation they are, returns_2022.md's re-verification)"),
                        new ChamberOfRecord(ElectionVintage.Italy2022, D(2022, 9, 25), D(2022, 10, 13), Open, "returns_2022.md [ALLOCATION PROVISIONAL]; DPR 97/2022 [DPR-97]; the first sitting 2022-10-13 [CAM-S1]"),
                    };
                case CountryId.USA:
                    return new[]
                    {
                        new ChamberOfRecord(ElectionVintage.Usa2020, D(2020, 11, 3), D(2021, 1, 3), D(2023, 1, 3), "[HH-DIV] [HH-117] [SEN-DATES]"),
                        new ChamberOfRecord(ElectionVintage.Usa2022, D(2022, 11, 8), D(2023, 1, 3), D(2025, 1, 3), "[HH-DIV] [HH-118] [SEN-DATES]"),
                        new ChamberOfRecord(ElectionVintage.Usa2024, D(2024, 11, 5), D(2025, 1, 3), Open, "[HH-DIV] [CLK-R1] [SEN-DATES]"),
                    };
                case CountryId.France:
                    return new[]
                    {
                        // ⚠ The XVIe's polling days are on no fetched page (france records, G1) - the election day is left UNSET rather than typed; it ends with the
                        // dissolution of 2024-06-09 [EL-DIS], and from that evening to the XVIIe's first sitting there is NO chamber of record (the record's own row).
                        new ChamberOfRecord(ElectionVintage.France2022, DateTime.MinValue, D(2022, 6, 28), D(2024, 6, 9), "[AN-S28] [AN-CAL16]; polling days billed (G1); seats by nuance NOT SOURCED (E-47); dissolved 2024-06-09 [EL-DIS]"),
                        new ChamberOfRecord(ElectionVintage.France2024, D(2024, 7, 7), D(2024, 7, 18), Open, "returns_2024.md; [EL-DIS]; the first sitting 2024-07-18 [AN-S18] [AN-CAL17]"),
                    };
                default:
                    return Array.Empty<ChamberOfRecord>();
            }
        }

        /// <summary>The chamber of record on a date. Before the first recorded chamber convened, the first; the records begin before every start.</summary>
        public static ChamberOfRecord ChamberAt(CountryId id, DateTime date)
        {
            IReadOnlyList<ChamberOfRecord> chambers = Chambers(id);
            if (chambers.Count == 0) { throw new ArgumentException($"no chambers of record for {id}"); }
            foreach (ChamberOfRecord c in chambers) { if (c.Holds(date)) { return c; } }
            if (date < chambers[0].Convened) { return chambers[0]; }
            // A gap between chambers - France from the dissolution of 2024-06-09 to the XVIIe's first sitting - holds no chamber of record; a world cannot open there.
            throw new InvalidOperationException($"{id} has no chamber of record on {date:yyyy-MM-dd}: a dissolved chamber and no new one convened");
        }

        /// <summary>`Seated` resolved to the election whose lists seat the chamber at the world's epoch; any other vintage as given.</summary>
        public static ElectionVintage Resolve(CountryId id, ElectionVintage vintage) =>
            vintage == ElectionVintage.Seated ? SeatedVintage(id, Simulation.SimulationManager.EpochDate) : vintage;

        /// <summary>The vintage whose seat table the country seats on a date - the chamber of record's where that table is sourced, else the latest sourced one (the deviation is <see cref="SeatingDeviation"/>).</summary>
        public static ElectionVintage SeatedVintage(CountryId id, DateTime date)
        {
            ElectionVintage ofRecord = ChamberAt(id, date).Vintage;
            return PartySystems.SeatsSourced(ofRecord) ? ofRecord : LatestSourced(id);
        }

        /// <summary>Where the chamber of record's per-list table is not on disk, the sentence the view carries; null when the seated chamber is the chamber of record.</summary>
        public static string SeatingDeviation(CountryId id, DateTime date)
        {
            ChamberOfRecord c = ChamberAt(id, date);
            if (PartySystems.SeatsSourced(c.Vintage)) { return null; }
            string elected = c.ElectionDay == DateTime.MinValue ? "elected in June 2022 (its polling days billed, G1)" : $"elected {c.ElectionDay:yyyy-MM-dd}";
            return $"the chamber of record on {date:yyyy-MM-dd} is the one {elected}, whose seats per list are not yet sourced (E-47); the {LatestSourced(id)} table is seated in its place";
        }

        private static ElectionVintage LatestSourced(CountryId id)
        {
            IReadOnlyList<ChamberOfRecord> chambers = Chambers(id);
            for (int i = chambers.Count - 1; i >= 0; i--) { if (PartySystems.SeatsSourced(chambers[i].Vintage)) { return chambers[i].Vintage; } }
            throw new InvalidOperationException($"{id} has no sourced seat table");
        }

        /// <summary>The governments of record per country, oldest first. Keys are the roster's; a cabinet the record cannot name is unsourced (the formation stands in).</summary>
        public static IReadOnlyList<GovernmentOfRecord> Governments(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden:
                    return new[]
                    {
                        new GovernmentOfRecord("Magdalena Andersson (S)", new[] { "S" }, null, D(2021, 11, 30), D(2022, 10, 18), true, "single-party S [RD-TRB] [RG-SMA] [RD-N1017]; caretaker from the dismissal request 2022-09-15 [RD-PK14] [RD-N1017]"),
                        new GovernmentOfRecord("Ulf Kristersson (M)", new[] { "M", "KD", "L" }, new[] { "SD" }, D(2022, 10, 18), D(2026, 9, 17), true, "the vote 2022-10-17, 176-173 [RD-PROT9]; M+KD+L with SD as samarbetsparti by the Tidö agreement [TIDO] [RG-RF] [RG-BOK]; took office 2022-10-18 [RG-HALL] [RG-NYA]"),
                        new GovernmentOfRecord("Ulf Kristersson (M), caretaker", new[] { "M", "KD", "L" }, null, D(2026, 9, 17), Open, false, "dismissed 2026-09-17 at his own request; a caretaker until the new Riksdag chooses a prime minister ([RG-ART], [RD-N17b]; its ministers' parties [RD-AKT]; 2026/government_2026.md) - by K-1 part (4)'s ruling the day-one government from this date is the formation's, PROVISIONAL"),
                    };
                case CountryId.Germany:
                    return new[]
                    {
                        new GovernmentOfRecord("Olaf Scholz (SPD)", new[] { "SPD", "Grune", "FDP" }, null, D(2021, 12, 8), D(2024, 11, 7), true, "elected 2021-12-08, 395 of 707 [BT-KW21]; the cabinet's parties [BT-BR21]; SPD+Grüne+FDP until the FDP ministers' dismissal 2024-11-07 [BP-ENT24]"),
                        new GovernmentOfRecord("Olaf Scholz (SPD), minority", new[] { "SPD", "Grune" }, null, D(2024, 11, 7), D(2025, 5, 6), true, "a Minderheitsregierung of SPD+Grüne [BT-MR24]; from 2025-03-25 a caretaker under GG 69(3) [BP-GF25]"),
                        new GovernmentOfRecord("Friedrich Merz (CDU)", new[] { "CDU", "CSU", "SPD" }, null, D(2025, 5, 6), Open, true, "elected 2025-05-06 on the second ballot, 325 of 618 [BT-KW25]; the cabinet's parties [BT-BR25]"),
                    };
                case CountryId.Poland:
                    return new[]
                    {
                        new GovernmentOfRecord("Mateusz Morawiecki (PiS)", new[] { "PiS" }, null, D(2019, 11, 15), D(2023, 12, 13), false, "appointed 2019-11-15 [ELI-MP-2019]; the coalition partners inside the PiS committee's government are a GAP (poland records G6) - the cabinet is carried as PiS alone, unsourced beyond the head"),
                        new GovernmentOfRecord("Donald Tusk (KO)", new[] { "KO", "TD", "NL" }, null, D(2023, 12, 13), Open, true, "elected 2023-12-11, 248-201 [API-V1]; appointed 2023-12-13 [ELI-MP-2023]; the cabinet's parties DERIVED by the record from the ministers' clubs as of 2026-09-24 (KO, PSL-TD and Polska2050 - the TD committee, Lewica); the day-one list of 2023-12-13 is its GAP G8", cabinetDerived: true),
                    };
                case CountryId.Italy:
                    return new[]
                    {
                        new GovernmentOfRecord("Mario Draghi", null, null, D(2021, 2, 13), D(2022, 10, 22), false, "in office 2021-02-13 → 2022-10-22 [GV-DR]; caretaker from 2022-07-21 [CAM-729]; the cabinet's party list is a GAP (italy records G5) - the formation's result stands in, PROVISIONAL"),
                        new GovernmentOfRecord("Giorgia Meloni (FdI)", new[] { "FdI", "Lega", "FI", "NM" }, null, D(2022, 10, 22), Open, true, "in office from 2022-10-22 [GV-ME]; the Camera's confidence 2022-10-25, 235-154 [CAM-S4]; the cabinet's parties DERIVED by the record from the confidence motion's signatories (its GAP G5)", cabinetDerived: true),
                    };
                default:
                    return Array.Empty<GovernmentOfRecord>();   // the USA's executive and France's governments are in their records; neither runs the formation model (§6, §8)
            }
        }

        /// <summary>The government of record on a date, where the country has one; false for the two countries outside the formation model.</summary>
        public static bool TryGovernmentAt(CountryId id, DateTime date, out GovernmentOfRecord government)
        {
            foreach (GovernmentOfRecord g in Governments(id)) { if (g.Holds(date)) { government = g; return true; } }
            government = default;
            return false;
        }

        /// <summary>Sets the world's epoch to this country's start. Called before the world is created; the epoch never moves while a world lives.</summary>
        public static void ApplyStart(CountryId id) => Simulation.SimulationManager.SetEpoch(StartDate(id));

        /// <summary>The polling day of the election a vintage names, or MinValue where the record bills it (France's XVIe).</summary>
        public static DateTime ElectionDayOf(CountryId id, ElectionVintage vintage)
        {
            foreach (ChamberOfRecord c in Chambers(id)) { if (c.Vintage == vintage) { return c.ElectionDay; } }
            return DateTime.MinValue;
        }

        // -----------------------------------------------------------------------------------------------------------------------------
        // PS-2 / CL-4 (2026-09-25, §619): THE ELECTION CALENDAR INSIDE A RUN. A country whose model exists holds its elections on its own
        // real calendar (the spec's §3 and ruling 4 of §618); until then its chamber holds as of record and no polling day is offered.
        // Sweden's is the statute's: "Ordinarie val till riksdagen hålls vart fjärde år" (regeringsformen 3 kap. 3 §, lag 2010:1408
        // [RF-3-3]) on "den andra söndagen i september" (vallagen 2005:837 1 kap. 3 § [VL-1-3]) - the cycle anchored on the latest
        // election of record (13 September 2026), so 2026, 2030, 2034 … Nothing here holds an extra election (RF 3 kap. 11 §): a
        // dissolution is stage 3's (§5.4 of the spec). The record's own date is the day the six records were closed (§617).
        // -----------------------------------------------------------------------------------------------------------------------------

        /// <summary>The day the records of §617 were closed - what "as of the record" means on a screen.</summary>
        public static readonly DateTime RecordDate = D(2026, 9, 24);

        /// <summary>The statute a country's ordinary polling day follows, with its citations, or null where no calendar is modelled.</summary>
        public static string PollingDayBasis(CountryId id) =>
            id == CountryId.Sweden ? "regeringsformen 3 kap. 3 § - every fourth year [RF-3-3]; vallagen 1 kap. 3 § - the second Sunday of September [VL-1-3] (sweden/election_calendar.md)" : null;

        /// <summary>
        /// The country's next ordinary polling day on or after <paramref name="onOrAfter"/>, false where the country's election calendar
        /// is not modelled (the five others until their stages: their chambers hold as of record).
        /// </summary>
        public static bool TryNextPollingDay(CountryId id, DateTime onOrAfter, out DateTime pollingDay)
        {
            pollingDay = DateTime.MinValue;
            if (id != CountryId.Sweden) { return false; }
            DateTime anchor = LatestElectionDay(id);
            // The election years are the anchor's every fourth year, in both directions (a date before the anchor reads the same cycle).
            int year = anchor.Year;
            while (SecondSundayOfSeptember(year) < onOrAfter.Date) { year += 4; }
            while (year - 4 >= 1 && SecondSundayOfSeptember(year - 4) >= onOrAfter.Date) { year -= 4; }
            pollingDay = SecondSundayOfSeptember(year);
            return true;
        }

        /// <summary>The second Sunday of September in a year - vallagen 1 kap. 3 §.</summary>
        public static DateTime SecondSundayOfSeptember(int year)
        {
            var first = new DateTime(year, 9, 1);
            int toSunday = ((int)DayOfWeek.Sunday - (int)first.DayOfWeek + 7) % 7;
            return first.AddDays(toSunday + 7);
        }

        /// <summary>
        /// The vintage an election held on <paramref name="pollingDay"/> reads its declarations from: the election of record on that very day
        /// where one exists (a game's 13 September 2026 reads 2026's declarations, K-1f's dated facts), else the latest election of record
        /// before it - the latest dated declarations stand until newer ones are sourced.
        /// </summary>
        public static ElectionVintage VintageOfElection(CountryId id, DateTime pollingDay)
        {
            IReadOnlyList<ChamberOfRecord> chambers = Chambers(id);
            ElectionVintage latest = ElectionVintage.Seated;
            DateTime latestDay = DateTime.MinValue;
            foreach (ChamberOfRecord c in chambers)
            {
                if (c.ElectionDay == DateTime.MinValue) { continue; }
                if (c.ElectionDay == pollingDay.Date) { return c.Vintage; }
                if (c.ElectionDay < pollingDay.Date && c.ElectionDay > latestDay) { latest = c.Vintage; latestDay = c.ElectionDay; }
            }
            return latest;
        }

        /// <summary>
        /// §7 of the spec - HISTORY AS THE REFERENCE: what actually happened at the election the game just held, where the record holds it.
        /// The real result's seats per party (the sourced table of the election of record on that polling day) and the government the
        /// record shows after it. False where the polling day is not an election of record (a game's 2030) - then there is no history yet.
        /// </summary>
        public sealed class Reference
        {
            public ElectionVintage Vintage;
            public string Label;
            public Dictionary<string, int> Seats;
            public string GovernmentLine;
            public string Basis;
        }

        public static bool TryReference(CountryId id, DateTime pollingDay, out Reference reference)
        {
            reference = null;
            foreach (ChamberOfRecord c in Chambers(id))
            {
                if (c.ElectionDay != pollingDay.Date || !PartySystems.SeatsSourced(c.Vintage)) { continue; }
                var r = new Reference
                {
                    Vintage = c.Vintage,
                    Label = id.ToString().ToUpperInvariant() + " " + c.ElectionDay.Year.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", AS IT HAPPENED",
                    Seats = PartySystems.InitialSeats(id, c.Vintage),
                    Basis = c.Basis,
                };
                // The government the record shows after the election: the first government of record from a day after polling day, if any.
                GovernmentOfRecord? after = null;
                foreach (GovernmentOfRecord g in Governments(id)) { if (g.From > pollingDay.Date && (!after.HasValue || g.From < after.Value.From)) { after = g; } }
                if (!after.HasValue)
                {
                    r.GovernmentLine = "THE RECORD SHOWS NO CHANGE OF GOVERNMENT AFTER THIS ELECTION (AS OF " + RecordDate.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant() + ")";
                }
                else if (after.Value.CabinetSourced)
                {
                    r.GovernmentLine = after.Value.From.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant() + " · " + after.Value.Head.ToUpperInvariant()
                        + " · IN CABINET " + string.Join("+", after.Value.Cabinet) + (after.Value.Support != null && after.Value.Support.Length > 0 ? " · SUPPORT " + string.Join("+", after.Value.Support) : string.Empty);
                }
                else
                {
                    // The review (§619): the sentence is generic - a caretaker head means the chamber had not chosen by the record's date; any other unsourced
                    // cabinet (Poland's Morawiecki, Italy's Draghi) means the record names the head but not the cabinet's parties.
                    bool caretaker = after.Value.Head != null && after.Value.Head.IndexOf("caretaker", StringComparison.OrdinalIgnoreCase) >= 0;
                    r.GovernmentLine = after.Value.From.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant() + " · " + after.Value.Head.ToUpperInvariant()
                        + (caretaker
                            ? " · THE CHAMBER HAD NOT CHOSEN A HEAD OF GOVERNMENT BY THE RECORD'S DATE, " + RecordDate.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant()
                            : " · ITS CABINET NOT NAMED BY THE RECORD");
                }
                reference = r;
                return true;
            }
            return false;
        }

        /// <summary>
        /// THE PICKER'S VIEW OF A COUNTRY, EVALUATED AT ITS START (the review's D4): the selector's world is built on the default epoch, so the
        /// chamber and cabinet it could read there were not the ones the game opens on. This reads the start's own: the seated table, the
        /// government of record where its cabinet is sourced, else the formation on those seats with the vintage's declarations.
        /// </summary>
        public sealed class PickerView
        {
            public ElectionVintage Vintage;
            public Dictionary<string, int> Seats;
            public int TotalSeats;
            public List<string> Cabinet = new List<string>();
            public bool Provisional;
            public string Deviation;
        }

        public static PickerView PickerViewOf(CountryId id)
        {
            DateTime start = StartDate(id);
            var view = new PickerView { Vintage = SeatedVintage(id, start), Deviation = SeatingDeviation(id, start) };
            view.Seats = PartySystems.InitialSeats(id, view.Vintage);
            foreach (KeyValuePair<string, int> kv in view.Seats) { view.TotalSeats += kv.Value; }
            if (SeatedGovernment.TryAt(id, start, out SeatedGovernment.Record record))
            {
                view.Provisional = record.Standing == SeatedGovernment.Standing.Provisional;
                if (record.Standing == SeatedGovernment.Standing.Installed) { view.Cabinet.AddRange(record.Cabinet); return view; }
            }

            IReadOnlyList<PoliticalParty> parties = PartySystems.For(id);
            var abbrevs = new List<string>(); var held = new List<int>();
            foreach (PoliticalParty p in parties) { abbrevs.Add(p.Abbrev); held.Add(view.Seats.TryGetValue(p.Abbrev, out int n) ? n : 0); }
            GovernmentFormation.View formed = GovernmentFormation.ViewOf(id, abbrevs, held, null, view.Vintage);
            if (formed.HasGovernment) { foreach ((string abbrev, int _) in formed.Cabinet) { view.Cabinet.Add(abbrev); } }
            return view;
        }
    }
}
