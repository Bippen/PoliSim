using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// SP-1 (2026-09-25, §622; `docs/specs/START_POINTS_AND_PARTY_CREATION_SPEC.md` §1): THE START POINTS AS DATA. A country offers one start
    /// point per important, popularly decided national election, the latest of each kind; where a country elects both a president and a
    /// chamber (Poland, France, the USA) the player chooses which contest to fight. Elections decided by assemblies (Germany's and Italy's
    /// presidents) are not start points. A start point is a country, an election and the date its run-up began (`WorldClock.StartDate` for
    /// the ruled starts of §618); a card is PLAYABLE or LOCKED with its reason in one line. Every date is the record's (`records_by_date.md`);
    /// a date the record does not hold is not typed from the spec - the card carries the year the record holds and says the rest is not in it.
    ///
    /// <para>Board 18b (Design, 2026-09-24): NO REGISTER NUMBER on any card. `Line` and `DateNote` are the player's words; a register id
    /// (E-49, S7, S8) lives in <see cref="StartPoint.Basis"/> only.</para>
    /// </summary>
    public static class StartPoints
    {
        public readonly struct StartPoint
        {
            public readonly CountryId Country;
            /// <summary>The election's kind, as the card says it: RIKSDAG ELECTION, PRESIDENTIAL ELECTION …</summary>
            public readonly string Kind;
            /// <summary>Polling day (the first round for a two-round election), or MinValue where the record does not hold it (then <see cref="DateNote"/> says so and <see cref="Year"/> is what the record holds).</summary>
            public readonly DateTime PollingDay;
            /// <summary>The provenance line where the record does not hold the day (18b: ROUND DATES NOT IN THE RECORD); null where it does.</summary>
            public readonly string DateNote;
            /// <summary>The year the record holds for the election - the polling day's, or the year alone where the day is not in the record.</summary>
            public readonly int Year;
            /// <summary>The day the world opens on for this start - the run-up's first day, the snap trigger, or the governing-mode opening.</summary>
            public readonly DateTime Opens;
            public readonly bool Playable;
            /// <summary>The one-line reason a locked card carries (LOCKED · …); for a playable card, what its start is (the selector's own line).</summary>
            public readonly string Line;
            public readonly string Basis;

            public StartPoint(CountryId country, string kind, DateTime pollingDay, string dateNote, DateTime opens, bool playable, string line, string basis, int year = 0)
            {
                Country = country; Kind = kind; PollingDay = pollingDay; DateNote = dateNote; Opens = opens; Playable = playable; Line = line; Basis = basis;
                Year = pollingDay != DateTime.MinValue ? pollingDay.Year : year;
            }

            /// <summary>A presidency: the card draws no chamber bar (18b).</summary>
            public bool Presidential => Kind != null && Kind.StartsWith("PRESIDENTIAL", StringComparison.Ordinal);
        }

        private static DateTime D(int y, int m, int d) => new DateTime(y, m, d);

        /// <summary>18b: the one reason every locked card carries today, in the player's words.</summary>
        public const string LockedPrefix = "LOCKED · ";
        public const string TwoRoundReason = "THE TWO-ROUND SYSTEM IS NOT YET MODELLED";
        public const string RoundDatesNote = "ROUND DATES NOT IN THE RECORD";

        /// <summary>The country's start points in date order (a date the record does not hold sorts by its year).</summary>
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
                        LockedPrefix + TwoRoundReason,
                        "poland/records_by_date.md (the run-off of 2025-06-01, the first round 2025-05-18; the inauguration 2025-08-06); the two-round presidential model is not built (S7)"));
                    break;
                case CountryId.Italy:
                    list.Add(Ruled(id, "GENERAL ELECTION (SNAP)", "DPR 97/2022 [DPR-97]; opens on the dissolution of 2022-07-21 [DPR-96] (§618)"));
                    break;
                case CountryId.USA:
                    list.Add(Ruled(id, "PRESIDENTIAL ELECTION", "NARA's Electoral College calendar [EC-DATES]; the run-up by the standard window (§618)"));
                    break;
                case CountryId.France:
                    list.Add(new StartPoint(id, "PRESIDENTIAL ELECTION", DateTime.MinValue, RoundDatesNote, DateTime.MinValue, false,
                        LockedPrefix + TwoRoundReason,
                        "france/records_by_date.md holds the Élysée's appointment of 2022-05-16 [EL-B16], not the rounds - E-49; the two-round presidential model is not built (S8)",
                        year: 2022));
                    list.Add(Ruled(id, "LEGISLATIVE ELECTION (SNAP)", "the Élysée's address of 2024-06-09 [EL-DIS]; governing mode opens at the XVIIe's first sitting 2024-07-18 [AN-S18] (ruled, §618) - the 577-constituency system is not modelled (R-EL10)"));
                    break;
            }
            list.Sort((a, b) => a.Year.CompareTo(b.Year) != 0 ? a.Year.CompareTo(b.Year) : Day(a).CompareTo(Day(b)));
            return list;
        }

        /// <summary>The ruled start of §618 as a start point: the latest election of the country's kind, opening on `WorldClock.StartDate`, playable (France in governing mode).</summary>
        private static StartPoint Ruled(CountryId id, string kind, string basis) =>
            new StartPoint(id, kind, WorldClock.LatestElectionDay(id), null, WorldClock.StartDate(id), true, WorldClock.StartLine(id), basis);

        private static DateTime Day(StartPoint p) => p.PollingDay != DateTime.MinValue ? p.PollingDay : new DateTime(Math.Max(1, p.Year), 1, 1);

        /// <summary>The playable start point of a country - the one the game opens on today (one per country until the presidential models land).</summary>
        public static bool TryPlayable(CountryId id, out StartPoint start)
        {
            foreach (StartPoint p in For(id)) { if (p.Playable) { start = p; return true; } }
            start = default;
            return false;
        }

        private static string Stamp(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

        /// <summary>The card's date stamp: the polling day (13 SEP 2026), or the year alone where the record does not hold the day (France's presidential: 2022).</summary>
        public static string DateLine(StartPoint p) =>
            p.PollingDay != DateTime.MinValue ? Stamp(p.PollingDay) : p.Year.ToString(CultureInfo.InvariantCulture);

        /// <summary>18a: the playable card's mode and opening in caption mono - RUN-UP · OPENS 18 JAN 2026; GOVERNING · OPENS 18 JUL 2024; a snap start SNAP ELECTION · OPENS 6 NOV 2024. Null for a locked card (its stamp reads LOCKED there).</summary>
        public static string ModeLine(StartPoint p)
        {
            if (!p.Playable) { return null; }
            string opens = "OPENS " + Stamp(p.Opens);
            if (WorldClock.GoverningModeOnly(p.Country)) { return "GOVERNING · " + opens; }
            return (WorldClock.IsSnapStart(p.Country) ? "SNAP ELECTION · " : "RUN-UP · ") + opens;
        }

        /// <summary>18b: a locked card's reason as the one caption line, without the LOCKED prefix its <see cref="StartPoint.Line"/> carries; null for a playable card.</summary>
        public static string Reason(StartPoint p)
        {
            if (p.Playable || p.Line == null) { return null; }
            return p.Line.StartsWith(LockedPrefix, StringComparison.Ordinal) ? p.Line.Substring(LockedPrefix.Length) : p.Line;
        }

        /// <summary>The election's kind as the card's name - Riksdag election, Presidential election, Bundestag election (snap).</summary>
        public static string Name(StartPoint p)
        {
            if (string.IsNullOrEmpty(p.Kind)) { return string.Empty; }
            string lower = p.Kind.ToLowerInvariant();
            return char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }
    }
}
