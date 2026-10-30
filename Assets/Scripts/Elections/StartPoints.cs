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
        /// <summary>18b's one reason, its words changed at §770 and again at SP-8: France's presidential START - the player as a candidate - stays locked because
        /// the French vote has no model (`docs/specs/FRANCE_PRESIDENTIAL_START_PLAN.md` §2: no fitted French electorate, so no first round the game could hold);
        /// the rule, the record and its night are built. Poland's is built since SP-7b (`PresidentialStart`).</summary>
        public const string PresidentialStartReason = "THE FRENCH VOTE IS NOT MODELLED YET";
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
                    // §720 (PS-5 with S7, part one): the presidential start reads its record through the two-round rule - the first vote's day from the
                    // PKW's returns, the verdict, the run-off and the term counted, not typed; §770: the game holds its own presidential election from a Sejm start.
                    // SP-7b (Elias's ruling I14): PLAYABLE - the player a candidate of record (ask 1 (a)), the world opened on the Marshal's order (ask 4 (4b), a
                    // stated deviation for presidential starts only - `PresidentialStart.PolandOpens`), PL-C: the campaign staged on it since (ask 3 (3a) stood until PL-C landed)
                    TwoRoundElection.RoundOfRecord(id, 2025, 1, out DateTime firstVote2025);
                    list.Add(new StartPoint(id, "PRESIDENTIAL ELECTION", firstVote2025, null, PresidentialStart.PolandOpens, true,
                        CandidateLine(PresidentialStart.PolandOpens, firstVote2025),
                        TwoRoundElection.RecordBrief(id, 2025) + " (the PKW's notices, poland/presidential_returns.md; the oaths, poland/records_by_date.md); opens on the Marshal's order, Dz.U. 2025 poz. 48 (poland/presidential_calendar_2025.md) - ruled I14 (4b), a deviation from the standard run-up for presidential starts only"));
                    break;
                case CountryId.Italy:
                    list.Add(Ruled(id, "GENERAL ELECTION (SNAP)", "DPR 97/2022 [DPR-97]; opens on the dissolution of 2022-07-21 [DPR-96] (§618)"));
                    break;
                case CountryId.USA:
                    list.Add(Ruled(id, "PRESIDENTIAL ELECTION", "NARA's Electoral College calendar [EC-DATES]; the run-up by the standard window (§618)"));
                    break;
                case CountryId.France:
                    // SP-8 (K174): the card's day is the record's (the Conseil's declaration, as generated) and it would open on the Conseil's list of candidates, made
                    // public 7 Mar 2022 [CC-187] - K174's "about 7 Mar 2022", sourced; LOCKED while the French vote has no model (the plan's §2), the record read through Art. 6-7
                    list.Add(new StartPoint(id, "PRESIDENTIAL ELECTION", Generated.FrenchPresidentialReturns.FirstRound, null, Generated.FrenchPresidentialReturns.CandidatesListed, false,
                        LockedPrefix + PresidentialStartReason,
                        TwoRoundElection.RecordBrief(id, 2022) + " (the Conseil constitutionnel's declarations [CC-195] [CC-197]; france/raw/presidential2022/); would open on the list of candidates [CC-187]; locked - no French vote model (SP-8's stop)",
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

        /// <summary>SP-7b: a candidate's start in the selector's words - its opening on the Marshal's order (I14 (4b), the stated deviation), that the party campaigns
        /// (PL-C; until it landed none was staged, I14 (3a)), and the first vote.</summary>
        private static string CandidateLine(DateTime opens, DateTime firstVote) =>
            "OPENS " + Stamp(opens) + " · THE MARSHAL'S ORDER · YOUR PARTY CAMPAIGNS · THE FIRST VOTE " + Stamp(firstVote);

        private static DateTime Day(StartPoint p) => p.PollingDay != DateTime.MinValue ? p.PollingDay : new DateTime(Math.Max(1, p.Year), 1, 1);

        /// <summary>PS-6 US-33 (SP-6a; R-US4 (a), RULED - F8 §781: the player's own nominee at the head of a major ticket, inside PS-6): whether a start lets the player
        /// stand a nominee of their own - a playable presidential start in a country where one is offered (`OwnNominees.Offered`: the USA's, 5 Nov 2024); the tickets
        /// are `OwnNominees.Tickets`. The third-party run (SP-6b) is no start's.</summary>
        public static bool OffersOwnNominee(StartPoint p) => p.Playable && p.Presidential && OwnNominees.Offered(p.Country);

        /// <summary>The country's DEFAULT start point - the first playable in date order, the one `WorldClock.StartDate` opens and the sheet pre-selects (SP-7b:
        /// Poland offers two playable starts, its Sejm start first; DECLARED, the plan's §4.2 class E).</summary>
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

        /// <summary>18a: the playable card's mode and opening in caption mono, in the words this method returns (`StartPointsDiagnostic` pins them): the run-up, a snap
        /// start's election, France's what-if (`WorldClock.GoverningModeOnly`), or, where `WorldClock.NoElectionYet` holds (Italy's snap start, PS-7-interim - read
        /// before the snap test; the USA's start until PS-6 US-8 built its count, a run-up since), that no election is held yet - never a run-up or a snap election the game does not hold. The card keeps the
        /// record's stamp and name (the start point's identity, SP-1). Null for a locked card (its stamp reads LOCKED there).</summary>
        public static string ModeLine(StartPoint p)
        {
            if (!p.Playable) { return null; }
            string opens = "OPENS " + Stamp(p.Opens);
            if (PresidentialStart.IsCandidateStart(p)) { return "THE MARSHAL'S ORDER · RUN-UP · " + opens; }   // SP-7b (I14 (4b)): the deviation's day, said on the card; PL-C: the run-up and the campaign are staged (the start takes the Polish campaign up, the PL-C row)
            if (WorldClock.GoverningModeOnly(p.Country)) { return "WHAT-IF · YOUR PARTY GOVERNS · " + opens; }   // PS-3d (§631, ruled): the card states the what-if plainly
            if (WorldClock.NoElectionYet(p.Country)) { return "NO ELECTION YET · " + opens; }   // PS-6 US-1 (its review), PS-7-interim: the card the folder opens onto promises no run-up and no snap election
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
