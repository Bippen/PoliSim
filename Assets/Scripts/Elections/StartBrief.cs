using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// SP-2 (2026-09-25, §623; the start-points spec §1.3): THE BRIEF, DERIVED - generated from the sourced records of the start's date, never
    /// written by hand, so it cannot go stale or invent. Its shape: <i>[Country], [start date]. [Government of record] has governed since [date],
    /// with [seats] of [chamber size] seats. Polling day is [date]. [n] parties sit in the [chamber].</i> Every clause carries the record it
    /// traces to (`WorldClock`'s chambers and governments of record, the seat tables, the calendar); a clause the record cannot fill says so
    /// in the sentence rather than filling itself. The optional tagline is a labelled slot, empty until Elias reviews one. One exception, stated: where
    /// `WorldClock.NoElectionYet` holds (the USA's start, PS-6 US-1) - not every start whose election the game does not hold, which is
    /// `WorldClock.TryNextPollingDay`'s answer (Italy's snap start is PS-7's) - the election's slot reports the game's own calendar, not the record's; its
    /// basis is that ruling, not a record.
    /// </summary>
    public static class StartBrief
    {
        public readonly struct Clause
        {
            public readonly string Text;
            public readonly string Basis;
            public Clause(string text, string basis) { Text = text; Basis = basis; }
        }

        private static readonly Dictionary<CountryId, string> ChamberName = new Dictionary<CountryId, string>
        {
            { CountryId.Sweden, "Riksdag" }, { CountryId.Germany, "Bundestag" }, { CountryId.Poland, "Sejm" },
            { CountryId.Italy, "Camera" }, { CountryId.USA, "House" }, { CountryId.France, "Assemblée" },
        };

        private static readonly Dictionary<CountryId, string> CountryName = new Dictionary<CountryId, string>
        {
            { CountryId.Sweden, "Sweden" }, { CountryId.Germany, "Germany" }, { CountryId.Poland, "Poland" },
            { CountryId.Italy, "Italy" }, { CountryId.USA, "United States" }, { CountryId.France, "France" },
        };

        private static string Long(DateTime d) => d.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

        private static string Stamp(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

        /// <summary>The chamber's name as the cards and the ledger say it: Riksdag, Bundestag, Sejm, Camera, House, Assemblée.</summary>
        public static string ChamberOf(CountryId id) => ChamberName.TryGetValue(id, out string name) ? name : "chamber";

        private static readonly Dictionary<CountryId, string> ChamberFullName = new Dictionary<CountryId, string>
        {
            { CountryId.Sweden, "Riksdag" }, { CountryId.Germany, "Bundestag" }, { CountryId.Poland, "Sejm" },
            { CountryId.Italy, "Camera dei deputati" }, { CountryId.USA, "House of Representatives" }, { CountryId.France, "Assemblée nationale" },
        };

        /// <summary>§718 (Elias's ruling of 2026-10-01, item 9: the chamber names per country): the chamber's full name, as a page's caption names it -
        /// the Riksdag, the Bundestag, the Sejm, the Camera dei deputati, the House of Representatives, the Assemblée nationale (the Politics screen's
        /// caption said THE NATIONAL ASSEMBLY for all six).</summary>
        public static string ChamberFullOf(CountryId id) => ChamberFullName.TryGetValue(id, out string name) ? name : ChamberOf(id);

        /// <summary>The chamber's largest party and its seats - a presidential brief's second half (PS-3b, §629).</summary>
        private static (string party, int seats) Majority(Dictionary<string, int> seats)
        {
            string best = "-"; int bestSeats = -1;
            foreach (KeyValuePair<string, int> kv in seats) { if (kv.Value > bestSeats) { best = kv.Key; bestSeats = kv.Value; } }
            return (best, Math.Max(bestSeats, 0));
        }

        /// <summary>The country as the ledger heads it (BRIEF · SWEDEN, 18 JAN 2026).</summary>
        public static string CountryOf(CountryId id) => CountryName.TryGetValue(id, out string name) ? name : id.ToString();

        /// <summary>The brief's clauses for a start point, in order, each with its basis.</summary>
        public static List<Clause> Clauses(StartPoints.StartPoint start)
        {
            var clauses = new List<Clause>();
            CountryId id = start.Country;
            string country = CountryName.TryGetValue(id, out string named) ? named : id.ToString();
            DateTime opens = start.Opens;
            clauses.Add(new Clause(country + ", " + Long(opens) + ".", "WorldClock.StartDate (" + start.Basis + ")"));

            WorldClock.ChamberOfRecord chamber = WorldClock.ChamberAt(id, opens);
            ElectionVintage seated = WorldClock.SeatedVintage(id, opens);
            Dictionary<string, int> seats = PartySystems.InitialSeats(id, seated);
            int size = 0, partiesSeated = 0;
            foreach (KeyValuePair<string, int> kv in seats) { size += kv.Value; if (kv.Value > 0) { partiesSeated++; } }

            if (WorldClock.TryGovernmentAt(id, opens, out WorldClock.GovernmentOfRecord government))
            {
                if (government.Kind == WorldClock.ExecutiveKind.Presidency)
                {
                    // PS-3b (§629): a presidential system's government of record is the president and their party; the chamber's majority is the sentence's other half (divided government, §6).
                    (string majorityParty, int majoritySeats) = Majority(seats);
                    clauses.Add(new Clause("The president is " + government.President + ", in office since " + Long(government.From) + "; the " + ChamberOf(id) + " majority is " + majorityParty
                        + ", with " + majoritySeats.ToString(CultureInfo.InvariantCulture) + " of " + size.ToString(CultureInfo.InvariantCulture) + " seats.",
                        "WorldClock.Governments (" + government.Basis + "); the seats " + chamber.Basis));
                }
                else if (government.CabinetSourced && government.Cabinet != null)
                {
                    int cabinetSeats = 0;
                    foreach (string abbrev in government.Cabinet) { if (seats.TryGetValue(abbrev, out int n)) { cabinetSeats += n; } }
                    clauses.Add(new Clause(government.Head + "'s government (" + string.Join("+", government.Cabinet) + ") has governed since " + Long(government.From)
                        + ", with " + cabinetSeats.ToString(CultureInfo.InvariantCulture) + " of " + size.ToString(CultureInfo.InvariantCulture) + " seats"
                        + (government.Support != null && government.Support.Length > 0 ? " and the support of " + string.Join("+", government.Support) : string.Empty)
                        + (government.President != null ? ", under the president " + government.President : string.Empty) + ".",
                        "WorldClock.Governments (" + government.Basis + "); the seats " + chamber.Basis));
                }
                else
                {
                    clauses.Add(new Clause(government.Head + " has governed since " + Long(government.From) + (government.President != null ? ", under the president " + government.President : string.Empty)
                        + "; the record does not name the cabinet's parties, so its seats are not counted here.",
                        "WorldClock.Governments (" + government.Basis + ") - cabinet not sourced"));
                }
            }
            else
            {
                clauses.Add(new Clause("No government of record holds on this date.", "WorldClock.Governments - a gap"));
            }

            if (WorldClock.NoElectionYet(id))
            {
                // PS-6, US-1: the game holds no election here yet - the clause says what holds instead, never a polling day it does not hold
                // PS-6 US-2 (R-US1 (a)): the record's next House and president, named and dated from the record's own rows - never the start's, already named above
                string house = WorldClock.RecordSeatsChamber(id) && WorldClock.TryNextChamberOfRecord(id, opens, out WorldClock.ChamberOfRecord nextHouse)
                    ? (nextHouse.ElectionDay == DateTime.MinValue ? "the next House of record" : "the House elected on " + Long(nextHouse.ElectionDay)) + " is seated on " + Long(nextHouse.Convened) : null;
                string head = WorldClock.RecordSeatsExecutive(id) && WorldClock.TryNextGovernmentOfRecord(id, opens, out WorldClock.GovernmentOfRecord nextHead)
                    ? (nextHead.President ?? nextHead.Head) + " takes office on " + Long(nextHead.From) : null;
                string dated = house != null && head != null ? house + " and " + head : house ?? head;
                clauses.Add(new Clause("No election is held in this game yet - " + (dated != null ? dated + ", as the record dates them" : "the record's House and president hold")
                    + "; no Senate is modelled.", "WorldClock.NoElectionYet, RecordSeatsChamber, RecordSeatsExecutive (PS-6, US-1 and US-2; R-US1 (a))"));
            }
            else if (start.PollingDay != DateTime.MinValue)
            {
                clauses.Add(new Clause("Polling day is " + Long(start.PollingDay) + ".", start.Playable ? "StartPoints (" + start.Basis + ")" : "StartPoints - the record's date; the contest is locked"));
            }
            else
            {
                clauses.Add(new Clause("Polling day is not on record (" + (start.DateNote ?? "billed") + ").", "StartPoints - billed"));
            }

            string chamberName = ChamberName.TryGetValue(id, out string name) ? name : "chamber";
            string deviation = WorldClock.SeatingDeviation(id, opens);
            clauses.Add(new Clause(partiesSeated.ToString(CultureInfo.InvariantCulture) + " parties sit in the " + chamberName + " (" + size.ToString(CultureInfo.InvariantCulture) + " seats, the election of "
                + Long(chamber.ElectionDay == DateTime.MinValue ? chamber.Convened : chamber.ElectionDay) + ")" + (deviation != null ? " - " + deviation : string.Empty) + ".",
                "PartySystems.InitialSeats(" + seated + "); " + chamber.Basis));
            return clauses;
        }

        /// <summary>One row of the brief as a ledger (board 18a): the slot's name, its figure in the stamp register, and the record it traces to.</summary>
        public readonly struct Row
        {
            public readonly string Name;
            public readonly string Figure;
            public readonly string Basis;
            public Row(string name, string figure, string basis) { Name = name; Figure = figure; Basis = basis; }
        }

        /// <summary>The ledger's head: BRIEF · SWEDEN, 18 JAN 2026.</summary>
        public static string Head(StartPoints.StartPoint start) => "BRIEF · " + CountryOf(start.Country).ToUpperInvariant() + ", " + Stamp(start.Opens);

        /// <summary>
        /// Board 18a (Design, 2026-09-24): THE BRIEF AS A LEDGER OF ITS SLOTS, the same records <see cref="Clauses"/> reads, one row per slot:
        /// Government → M + KD + L · Ulf Kristersson (M); Since → 18 OCT 2022; Seats → 103 OF 349; Support → SD; Polling day → 13 SEP 2026;
        /// In the Riksdag → 8 PARTIES · 349 SEATS · ELECTED 11 SEP 2022. Where the record holds no government the row reads "— NONE OF RECORD"
        /// and the rows that hang off a government (Since, Seats, Support) are not emitted; a polling day already past when the start opens
        /// (France) is "Last polling day"; a day the record does not hold reads its note.
        /// </summary>
        public static List<Row> Rows(StartPoints.StartPoint start)
        {
            var rows = new List<Row>();
            CountryId id = start.Country;
            DateTime opens = start.Opens;

            WorldClock.ChamberOfRecord chamber = WorldClock.ChamberAt(id, opens);
            ElectionVintage seated = WorldClock.SeatedVintage(id, opens);
            Dictionary<string, int> seats = PartySystems.InitialSeats(id, seated);
            int size = 0, partiesSeated = 0;
            foreach (KeyValuePair<string, int> kv in seats) { size += kv.Value; if (kv.Value > 0) { partiesSeated++; } }

            if (WorldClock.TryGovernmentAt(id, opens, out WorldClock.GovernmentOfRecord government))
            {
                string basis = "WorldClock.Governments (" + government.Basis + ")";
                if (government.Kind == WorldClock.ExecutiveKind.Presidency)
                {
                    // PS-3b (§629): the president and their party, then the chamber's majority - the two halves of a presidential government.
                    (string majorityParty, int majoritySeats) = Majority(seats);
                    rows.Add(new Row("President", government.President, basis));
                    rows.Add(new Row("Since", Stamp(government.From), basis));
                    rows.Add(new Row(ChamberOf(id), majorityParty + " · " + majoritySeats.ToString(CultureInfo.InvariantCulture) + " OF " + size.ToString(CultureInfo.InvariantCulture), basis + "; the seats " + chamber.Basis));
                }
                else if (government.CabinetSourced && government.Cabinet != null)
                {
                    int cabinetSeats = 0;
                    foreach (string abbrev in government.Cabinet) { if (seats.TryGetValue(abbrev, out int n)) { cabinetSeats += n; } }
                    rows.Add(new Row("Government", string.Join(" + ", System.Array.ConvertAll(government.Cabinet, a => PartySystems.ShortName(id, a))) + " · " + government.Head, basis));   // §707: the party's name, not the data key ("Grune")
                    rows.Add(new Row("Since", Stamp(government.From), basis));
                    rows.Add(new Row("Seats", cabinetSeats.ToString(CultureInfo.InvariantCulture) + " OF " + size.ToString(CultureInfo.InvariantCulture), basis + "; the seats " + chamber.Basis));
                    if (government.Support != null && government.Support.Length > 0)
                    {
                        rows.Add(new Row("Support", string.Join(" + ", System.Array.ConvertAll(government.Support, a => PartySystems.ShortName(id, a))), basis));
                    }
                    if (government.President != null) { rows.Add(new Row("President", government.President, basis)); }   // PS-3b (§629): France's cabinet sits under its president
                }
                else
                {
                    rows.Add(new Row("Government", government.Head + " · CABINET NOT IN THE RECORD", basis + " - cabinet not sourced"));
                    rows.Add(new Row("Since", Stamp(government.From), basis));
                    if (government.President != null) { rows.Add(new Row("President", government.President, basis)); }   // PS-3b (§629)
                }
            }
            else
            {
                rows.Add(new Row("Government", "— NONE OF RECORD", "WorldClock.Governments - a gap"));
            }

            if (WorldClock.NoElectionYet(id))
            {
                rows.Add(new Row("Election", "NONE IN THIS GAME YET · THE RECORD SEATED ON ITS DATES", "WorldClock.NoElectionYet (PS-6, US-1 and US-2; R-US1 (a))"));   // US-2: what holds, not a polling day
            }
            else if (start.PollingDay != DateTime.MinValue)
            {
                bool past = start.PollingDay < opens;
                rows.Add(new Row(past ? "Last polling day" : "Polling day", Stamp(start.PollingDay),
                    start.Playable ? "StartPoints (" + start.Basis + ")" : "StartPoints - the record's date; the contest is locked"));
            }
            else
            {
                rows.Add(new Row("Polling day", start.DateNote ?? "NOT IN THE RECORD", "StartPoints - not in the record"));
            }

            DateTime elected = chamber.ElectionDay == DateTime.MinValue ? chamber.Convened : chamber.ElectionDay;
            rows.Add(new Row("In the " + ChamberOf(id),
                partiesSeated.ToString(CultureInfo.InvariantCulture) + " PARTIES · " + size.ToString(CultureInfo.InvariantCulture) + " SEATS · "
                + (chamber.ElectionDay == DateTime.MinValue ? "CONVENED " : "ELECTED ") + Stamp(elected),
                "PartySystems.InitialSeats(" + seated + "); " + chamber.Basis));
            return rows;
        }

        /// <summary>The brief as one paragraph.</summary>
        public static string Text(StartPoints.StartPoint start)
        {
            var parts = new List<string>();
            foreach (Clause c in Clauses(start)) { parts.Add(c.Text); }
            return string.Join(" ", parts);
        }

        /// <summary>The optional tagline - labelled game fiction, reviewed by Elias, never a fact the record does not hold. Empty until one is reviewed.</summary>
        public static string Tagline(StartPoints.StartPoint start) => null;
    }
}
