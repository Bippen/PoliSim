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
    /// in the sentence rather than filling itself. The optional tagline is a labelled slot, empty until Elias reviews one.
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
            { CountryId.Italy, "Camera" }, { CountryId.USA, "House" }, { CountryId.France, "Assemblée nationale" },
        };

        private static readonly Dictionary<CountryId, string> CountryName = new Dictionary<CountryId, string>
        {
            { CountryId.Sweden, "Sweden" }, { CountryId.Germany, "Germany" }, { CountryId.Poland, "Poland" },
            { CountryId.Italy, "Italy" }, { CountryId.USA, "United States" }, { CountryId.France, "France" },
        };

        private static string Long(DateTime d) => d.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

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
                if (government.CabinetSourced && government.Cabinet != null)
                {
                    int cabinetSeats = 0;
                    foreach (string abbrev in government.Cabinet) { if (seats.TryGetValue(abbrev, out int n)) { cabinetSeats += n; } }
                    clauses.Add(new Clause(government.Head + "'s government (" + string.Join("+", government.Cabinet) + ") has governed since " + Long(government.From)
                        + ", with " + cabinetSeats.ToString(CultureInfo.InvariantCulture) + " of " + size.ToString(CultureInfo.InvariantCulture) + " seats"
                        + (government.Support != null && government.Support.Length > 0 ? " and the support of " + string.Join("+", government.Support) : string.Empty) + ".",
                        "WorldClock.Governments (" + government.Basis + "); the seats " + chamber.Basis));
                }
                else
                {
                    clauses.Add(new Clause(government.Head + " has governed since " + Long(government.From) + "; the record does not name the cabinet's parties, so its seats are not counted here.",
                        "WorldClock.Governments (" + government.Basis + ") - cabinet not sourced"));
                }
            }
            else
            {
                clauses.Add(new Clause("No government of record holds on this date.", "WorldClock.Governments - a gap"));
            }

            if (start.PollingDay != DateTime.MinValue)
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
