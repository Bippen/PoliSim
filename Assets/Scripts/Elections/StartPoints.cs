using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// SP-1 (2026-09-25, §622; `docs/specs/START_POINTS_AND_PARTY_CREATION_SPEC.md` §1): THE START POINTS AS DATA. A country offers one start
    /// point per important, popularly decided national election, the latest of each kind; where a country elects both a president and a
    /// chamber (Poland, France, the USA) the player chooses which contest to fight. Elections decided by assemblies (Germany's and Italy's
    /// presidents) are not start points. A start point is a country, an election and the date its run-up began (`WorldClock.StartDate` for
    /// the ruled starts of §618); a card is PLAYABLE or LOCKED with its reason in one line. Every date is the record's (`records_by_date.md`);
    /// a date the record does not hold is billed, never typed from the spec.
    /// </summary>
    public static class StartPoints
    {
        public readonly struct StartPoint
        {
            public readonly CountryId Country;
            /// <summary>The election's kind, as the card says it: RIKSDAG ELECTION, PRESIDENTIAL ELECTION …</summary>
            public readonly string Kind;
            /// <summary>Polling day (the first round for a two-round election), or MinValue where the record does not hold it (then <see cref="DateNote"/> says so).</summary>
            public readonly DateTime PollingDay;
            public readonly string DateNote;
            /// <summary>The day the world opens on for this start - the run-up's first day, the snap trigger, or the governing-mode opening.</summary>
            public readonly DateTime Opens;
            public readonly bool Playable;
            /// <summary>The one-line reason a locked card carries; for a playable card, what its start is (the selector's own line).</summary>
            public readonly string Line;
            public readonly string Basis;

            public StartPoint(CountryId country, string kind, DateTime pollingDay, string dateNote, DateTime opens, bool playable, string line, string basis)
            {
                Country = country; Kind = kind; PollingDay = pollingDay; DateNote = dateNote; Opens = opens; Playable = playable; Line = line; Basis = basis;
            }
        }

        private static DateTime D(int y, int m, int d) => new DateTime(y, m, d);

        /// <summary>The country's start points in date order (a billed date sorts by its year).</summary>
        public static IReadOnlyList<StartPoint> For(CountryId id)
        {
            var list = new List<StartPoint>();
            switch (id)
            {
                case CountryId.Sweden:
                    list.Add(Ruled(id, "RIKSDAG ELECTION", "Valmyndigheten's result fixed 2026-09-19 (sweden/2026/returns_2026.md); the run-up by the calendar (§619)"));
                    break;
                case CountryId.Germany:
                    list.Add(Ruled(id, "BUNDESTAG ELECTION (SNAP)", "BGBl. 2024 I Nr. 435; opens on the chancellor's request of 2024-11-06 [BREG-ST24] (§618)"));
                    break;
                case CountryId.Poland:
                    list.Add(Ruled(id, "SEJM ELECTION", "the PKW's notice, Dz.U. 2023 poz. 2234 (poland/returns_2023.md); the run-up by the standard window (§618)"));
                    list.Add(new StartPoint(id, "PRESIDENTIAL ELECTION", D(2025, 5, 18), null, DateTime.MinValue, false,
                        "LOCKED · THE TWO-ROUND PRESIDENTIAL MODEL IS NOT BUILT (S7) · FIRST ROUND 18 MAY 2025, RUN-OFF 1 JUNE 2025",
                        "poland/records_by_date.md (the run-off of 2025-06-01, the first round 2025-05-18; the inauguration 2025-08-06)"));
                    break;
                case CountryId.Italy:
                    list.Add(Ruled(id, "GENERAL ELECTION (SNAP)", "DPR 97/2022 [DPR-97]; opens on the dissolution of 2022-07-21 [DPR-96] (§618)"));
                    break;
                case CountryId.USA:
                    list.Add(Ruled(id, "PRESIDENTIAL ELECTION", "NARA's Electoral College calendar [EC-DATES]; the run-up by the standard window (§618)"));
                    break;
                case CountryId.France:
                    list.Add(new StartPoint(id, "PRESIDENTIAL ELECTION", DateTime.MinValue, "2022 · THE ROUNDS' DATES ARE BILLED (E-49)", DateTime.MinValue, false,
                        "LOCKED · THE TWO-ROUND PRESIDENTIAL MODEL IS NOT BUILT (S8)",
                        "france/records_by_date.md holds the Élysée's appointment of 2022-05-16 [EL-B16], not the rounds - E-49"));
                    list.Add(Ruled(id, "LEGISLATIVE ELECTION (SNAP)", "the Élysée's address of 2024-06-09 [EL-DIS]; governing mode opens at the XVIIe's first sitting 2024-07-18 [AN-S18] (ruled, §618) - the 577-constituency system is not modelled (R-EL10)"));
                    break;
            }
            list.Sort((a, b) => Year(a).CompareTo(Year(b)) != 0 ? Year(a).CompareTo(Year(b)) : Day(a).CompareTo(Day(b)));
            return list;
        }

        /// <summary>The ruled start of §618 as a start point: the latest election of the country's kind, opening on `WorldClock.StartDate`, playable (France in governing mode).</summary>
        private static StartPoint Ruled(CountryId id, string kind, string basis) =>
            new StartPoint(id, kind, WorldClock.LatestElectionDay(id), null, WorldClock.StartDate(id), true, WorldClock.StartLine(id), basis);

        private static int Year(StartPoint p) => p.PollingDay != DateTime.MinValue ? p.PollingDay.Year : ParseYear(p.DateNote);
        private static DateTime Day(StartPoint p) => p.PollingDay != DateTime.MinValue ? p.PollingDay : new DateTime(Year(p), 1, 1);
        private static int ParseYear(string note)
        {
            if (note == null) { return 0; }
            foreach (string token in note.Split(' ')) { if (token.Length == 4 && int.TryParse(token, out int y)) { return y; } }
            return 0;
        }

        /// <summary>The playable start point of a country - the one the game opens on today (one per country until the presidential models land).</summary>
        public static bool TryPlayable(CountryId id, out StartPoint start)
        {
            foreach (StartPoint p in For(id)) { if (p.Playable) { start = p; return true; } }
            start = default;
            return false;
        }

        /// <summary>The card's date line: the polling day, or the note where the record does not hold it.</summary>
        public static string DateLine(StartPoint p) =>
            p.PollingDay != DateTime.MinValue ? p.PollingDay.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant() : (p.DateNote ?? "DATE BILLED");
    }
}
