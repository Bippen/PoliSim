using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-5 with S7, part one: A DIRECTLY ELECTED PRESIDENT OF RECORD, BY DATE - who holds the office the spec models (Poland's president, whose
    /// veto is live since §761 - `PresidentialVeto`), from the day the oath was taken (Art. 128 ust. 1: the term begins on taking office) to the next's, each with the
    /// election that put them there. Sourced in `ElectionsData/poland/records_by_date.md` §2d (the National Assembly's oath protocols) and
    /// `presidential_returns.md` (the PKW's notices). A Poland run from its start (2023-02-19) sits in Duda's second term and crosses the 2025
    /// election; the president of record holds until the game's own presidential election seats a successor (§770, `PresidentialElection.PresidentAt`).
    /// <para>PS-6 US-6: THE USA'S 2024 FIELD AND ITS NAMING. The USA's presidents of record, and their dates, are its governments of record
    /// (`WorldClock.Governments` - a presidency's government is the president and their party, §629), so they are not copied here and <see cref="Of"/>
    /// holds none for the USA. What is here is the 2024 election's field of record (<see cref="CandidatesOf"/>, R-US2 (a) and E1) and the nominees'
    /// naming by the nomination's date of record (<see cref="NomineeName"/>). The prediction reads them; the count is US-8's.</para>
    /// </summary>
    public static class PresidencyOfRecord
    {
        public readonly struct President
        {
            public readonly string Name;
            public readonly DateTime TookOffice;
            /// <summary>The day the successor took office; <see cref="DateTime.MaxValue"/> while in office on the record's last day.</summary>
            public readonly DateTime Until;
            public readonly DateTime FirstVote;
            public readonly DateTime RunOff;
            public readonly string Basis;
            /// <summary>§761 (PS-5, Elias's ruling B1): the party whose deputies' vote the veto reads - "his backing party". The committee is the voters'
            /// (Kodeks wyborczy art. 84 § 3: a party cannot nominate); the backing is the party that put him forward.</summary>
            public readonly string BackingParty;
            /// <summary>Where <see cref="BackingParty"/> comes from - its source, or the reading DECLARED where none is held.</summary>
            public readonly string BackingBasis;
            public President(string name, DateTime tookOffice, DateTime until, DateTime firstVote, DateTime runOff, string basis, string backingParty, string backingBasis)
            {
                Name = name; TookOffice = tookOffice; Until = until; FirstVote = firstVote; RunOff = runOff; Basis = basis; BackingParty = backingParty; BackingBasis = backingBasis;
            }
        }

        private static DateTime D(int y, int m, int d) => new DateTime(y, m, d);

        private static readonly President[] Poland =
        {
            new President("Andrzej Duda", D(2020, 8, 6), D(2025, 8, 6), D(2020, 6, 28), D(2020, 7, 12),
                "the run-off of 2020-07-12 (Dz.U. 2020 poz. 1238; the first vote Dz.U. 2020 poz. 1163); the oath before the National Assembly 2020-08-06 [ZN-2020] [ZN-2020-META]",
                "PiS", "DECLARED - the game's reading (B1's backtest reads PiS for both presidents, §757); his 2020 backing is not sourced (presidential_returns.md, NOT REACHED)"),
            new President("Karol Nawrocki", D(2025, 8, 6), DateTime.MaxValue, D(2025, 5, 18), D(2025, 6, 1),
                "the run-off of 2025-06-01 (Dz.U. 2025 poz. 714; the first vote Dz.U. 2025 poz. 652); the oath before the National Assembly 2025-08-06 [ZN-2025-CONV] [ZN-2025] [PKW-PREZ-2025]",
                "PiS", "[SECONDARY] the PiS site's #Nawrocki2025 page (presidential_returns.md: the party's decision to put him forward)"),
        };

        /// <summary>The country's presidents of record in order; empty where none is directly elected in the record the game holds - and for the USA,
        /// whose presidents of record are its governments of record (`WorldClock.Governments`), held there once (US-6, the plan's "no second copy").</summary>
        public static IReadOnlyList<President> Of(CountryId id) => id == CountryId.Poland ? Poland : Array.Empty<President>();

        /// <summary>§770 (PS-5 item C4): one candidate of a presidential election of record - the PKW's surname and the roster party whose standing
        /// the candidate inherits in the game's own election (null: none - an independent, a splinter), with the reading. B5: a party cannot
        /// nominate; the committee is the voters', the backing the party's. <see cref="PositionUnit"/> is where B4 placed the candidate in the sovereignty
        /// space - the CHES unit the transfer was fitted on (§764): a roster party's key, a joint list's member (Hołownia at Polska 2050's own row), or a
        /// unit the roster does not seat (Zandberg at Razem's, <see cref="PartySystems.PolandUnseatedUnits"/>) - the units
        /// <see cref="PresidentialElection.TryPosition"/> reads; null where no CHES unit places the candidate.
        /// `PresidentialVoteBacktest` holds its own field to these readings - the committees, and the positions of the candidates who stand.
        /// <para>§772 (Elias's ruling E1): the candidate's own pull. A candidate a roster party backs carries <see cref="Factor"/> - [FITTED] once, to the
        /// PKW's first round of 18 May 2025 against the game's poll that day on the reference world - the record's state on the eve, as a game started
        /// that day holds it (`PresidentialReferenceWorld`; `PresidentialElectionLiveDiagnostic` refits it on every bar and fails where a figure here is no
        /// longer the fit): first-round support = the party's poll × the factor. A candidate whose party has no roster row carries the record's own
        /// first-round share as its base (<see cref="Factor"/> NaN). Per-candidate data, not a free parameter. RULED (Elias's ruling F4): fitted on the
        /// world that follows history; a game played from Poland's 2023 start polls otherwise that day, so its first round moves with its play
        /// (<see cref="PresidentialElection"/>).</para></summary>
        public readonly struct CandidateOfRecord
        {
            public readonly string Surname, BackingParty, PositionUnit, Why;
            /// <summary>[FITTED] (E1) the candidate factor - support = the backing party's poll × this; NaN where no roster party backs the candidate.</summary>
            public readonly double Factor;
            public CandidateOfRecord(string surname, string backingParty, string positionUnit, double factor, string why)
            {
                Surname = surname; BackingParty = backingParty; PositionUnit = positionUnit; Factor = factor; Why = why;
            }
        }

        // §772 (E1): the whole field of record - every candidate the PKW's first round of 2025 returned. The factors are the fit's (see the struct).
        private static readonly CandidateOfRecord[] Poland2025 =
        {
            new CandidateOfRecord("NAWROCKI", "PiS", "PiS", 0.879942594, "the candidate PiS backed (a civic committee)"),
            new CandidateOfRecord("TRZASKOWSKI", "KO", "KO", 1.04365446, "KO's candidate"),
            new CandidateOfRecord("MENTZEN", "Konf", "Konf", 2.17814027, "Konfederacja's candidate"),
            new CandidateOfRecord("BRAUN", null, "Konf", double.NaN, "his party ran on Konfederacja's 2023 list and left it in 2025 - no roster row; at Konfederacja's, where B4 placed him"),
            new CandidateOfRecord("HOŁOWNIA", "TD", "Polska 2050", 0.243875544, "Trzecia Droga's candidate (Polska 2050's leader; PSL backed him)"),
            new CandidateOfRecord("ZANDBERG", null, "Razem", double.NaN, "Razem ran inside Lewica's 2023 committee and left its club in 2024 - no roster row; at Razem's own"),
            new CandidateOfRecord("BIEJAT", "NL", "NL", 0.470311426, "Lewica's candidate"),
            new CandidateOfRecord("STANOWSKI", null, null, double.NaN, "no roster party backed the candidate and no CHES unit places him - unplaced"),
            new CandidateOfRecord("SENYSZYN", null, null, double.NaN, "no roster party backed the candidate and no CHES unit places her - unplaced"),
            new CandidateOfRecord("JAKUBIAK", null, null, double.NaN, "no roster party backed the candidate and no CHES unit places him - unplaced"),
            new CandidateOfRecord("BARTOSZEWICZ", null, null, double.NaN, "no roster party backed the candidate and no CHES unit places him - unplaced"),
            new CandidateOfRecord("MACIAK", null, null, double.NaN, "no roster party backed the candidate and no CHES unit places him - unplaced"),
            new CandidateOfRecord("WOCH", null, null, double.NaN, "no roster party backed the candidate and no CHES unit places him - unplaced"),
        };

        /// <summary>
        /// PS-6 US-6 (E1 for the USA; R-US2 (a), RULED - Elias's ruling F8, §781: the November nominee of record, "carrying that candidate's fitted factor
        /// from the start"): [FITTED] the two nominees' factors - each nominee's share of the FEC's national total ÷ the party's poll on the day of the
        /// vote, on the USA's reference world (`PresidentialReferenceWorld`: the epoch set to the eve of 5 Nov 2024 before the world is created, one day
        /// stepped by the game's loop; RULED F4: the world that follows history). `UsPresidentialFactorCheck` refits them on every bar, fails where a figure
        /// here is no longer the fit, and prints the refit as FIT lines, which `Tools/us_factor_patch.pl` patches in from the log (never retyped).
        /// NaN where no fit has been patched in - the check fails on it by name. Keyed by NARA's ticket letter, as the catalog's rows are.
        /// </summary>
        private static readonly (string Ticket, string Party, double Factor)[] Usa2024Factors =
        {
            ("R", "REP", 1.0183908285859777),
            ("D", "DEM", 0.94561954699236783),
        };

        /// <summary>
        /// PS-6 US-6: the USA's 2024 field of record, whole - every column of the FEC's workbook as the generated catalog holds it
        /// (<see cref="Generated.UsPresidentialReturns.Candidates"/>), so nothing of it is typed. The two nominees NARA names (Trump, REP; Harris, DEM) stand
        /// for their parties, at their parties' positions, carrying <see cref="Usa2024Factors"/>; every other column - Stein, Kennedy, Oliver, the rest,
        /// and the two ballot lines that are not candidates (Nevada's None of These Candidates, the scattered write-ins) - carries its national share of
        /// record as its base (<see cref="CandidateOfRecord.Factor"/> NaN), so the field sums to the FEC's total. A DEPARTURE (US-6) from the plan's words,
        /// "the minor candidates standing only where the FEC shows them on the ballot", put to Elias: each stands NATIONALLY, at its national share - the
        /// prediction is national, so its shares are the same either way; where the FEC shows a candidate on no ballot (Kennedy, withdrawn, stood on
        /// some) is a state's matter (`Tools/us_ballot_standing.pl` prints where the workbook shows each with votes). PS-6 US-8's count reads the states
        /// with every minor candidate pooled per state (US-4's form, on which R-US14 was ruled), so a per-state table would move no elector - it stays
        /// US-6's ask.
        /// </summary>
        private static readonly CandidateOfRecord[] Usa2024 = Usa2024Field();

        private static CandidateOfRecord[] Usa2024Field()
        {
            var field = new List<CandidateOfRecord>();
            foreach (var c in Generated.UsPresidentialReturns.Candidates)
            {
                if (c.Year != 2024) { continue; }
                string party = null;
                double factor = double.NaN;
                foreach (var f in Usa2024Factors) { if (f.Ticket == c.Ticket) { party = f.Party; factor = f.Factor; } }
                field.Add(new CandidateOfRecord(c.Candidate, party, party, factor, party != null
                    ? "the nominee of record NARA names [" + c.Ticket + "], at the party's position"
                    : "no roster party backs the column - at its national share of record as base (the FEC's workbook), standing nationally (a departure from the plan, put to Elias - US-6)"));
            }
            return field.ToArray();
        }

        /// <summary>The candidates of the election of record held for the term ending in <paramref name="termEndYear"/> - §772: its whole field; empty
        /// where the record holds none (every election after 2025). PS-6 US-6: the USA's 2024 election, for the term ending on 20 January 2025
        /// (<see cref="Usa2024"/>); a later field starts at `PresidentialElection.FutureFactor` (E1).</summary>
        public static IReadOnlyList<CandidateOfRecord> CandidatesOf(CountryId id, int termEndYear) =>
            id == CountryId.Poland && termEndYear == 2025 ? Poland2025
            : id == CountryId.USA && termEndYear == 2025 ? Usa2024
            : (IReadOnlyList<CandidateOfRecord>)Array.Empty<CandidateOfRecord>();

        /// <summary>A candidate of record's name as the game prints it - the first given name and the surname, read from the PKW's form
        /// ("NAWROCKI Karol Tadeusz" - "Karol Nawrocki"); the surname alone where the returns do not hold the year's first round. PS-6 US-6: a US nominee
        /// by NARA's name for the year's ticket (the catalog's year row); any other US column by its own label.</summary>
        public static string NameOfRecord(CountryId id, int year, string surname)
        {
            string Title(string s) => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
            if (id == CountryId.Poland)
            {
                foreach (var v in Generated.PolishPresidentialReturns.Votes)
                {
                    if (v.Year != year || v.Round != 1) { continue; }
                    string[] parts = v.Candidate.Split(' ');
                    if (parts.Length >= 2 && parts[0] == surname) { return parts[1] + " " + Title(parts[0]); }
                }
            }
            if (id == CountryId.USA)
            {
                foreach (var c in Generated.UsPresidentialReturns.Candidates)
                {
                    if (c.Year != year || c.Candidate != surname || (c.Ticket != "R" && c.Ticket != "D")) { continue; }
                    foreach (var y in Generated.UsPresidentialReturns.Years) { if (y.Year == year) { return c.Ticket == "R" ? y.NomineeR : y.NomineeD; } }
                }
            }
            return Title(surname);
        }

        /// <summary>
        /// PS-6 US-6 (R-US2 (a), RULED - F8, §781: "Named on screen from the nomination's date of record, 'the Democratic/Republican nominee' before
        /// it"): the 2024 nominations by date. DEM: the DNC's release of 6 Aug 2024 - Harris and Walz certified as the party's nominees and the
        /// nominations accepted (`ElectionsData/usa/records_by_date.md` §2a, [DNC-NOMINEE]). REP: BILLED - gop.com refuses this machine (G1, G13) and
        /// NARA's certificates (the plan's stand-in) name the candidates only after the count, so no date is held and this table leaves the Republican
        /// nominee unnamed on every day (MaxValue), to a REP player as to anyone, while the Democratic nominee is named from 6 Aug 2024. PS-6 US-8: that
        /// is the naming before the count, though no screen reads this table yet; from the count on, the game's own count names each nominee by NARA's name for the ticket
        /// (<see cref="NameOfRecord"/>; the contest, the oath's government, the Parliament page's president row) - the asymmetry and that naming put to
        /// Elias as one question. The withdrawal of 21 July 2024 is not replayed (R-US2 (a)).
        /// </summary>
        private static readonly (string Party, string Ticket, string Word, DateTime NamedFrom)[] UsNominations2024 =
        {
            ("DEM", "D", "Democratic", D(2024, 8, 6)),
            ("REP", "R", "Republican", DateTime.MaxValue),
        };

        /// <summary>PS-6 US-6 (R-US2 (a)): how the 2024 field names a party's nominee on <paramref name="on"/> - the nominee of record by name from the
        /// nomination's date of record (<see cref="NameOfRecord"/>), "the Democratic nominee" / "the Republican nominee" before it or where the date is
        /// billed; null for a country or party with no nominee of record. US-8's screens read it; the factor check holds it to the dated pages.</summary>
        public static string NomineeName(CountryId id, string party, DateTime on)
        {
            if (id != CountryId.USA) { return null; }
            foreach (var n in UsNominations2024)
            {
                if (n.Party != party) { continue; }
                if (on.Date < n.NamedFrom) { return "the " + n.Word + " nominee"; }
                foreach (var c in Generated.UsPresidentialReturns.Candidates) { if (c.Year == 2024 && c.Ticket == n.Ticket) { return NameOfRecord(id, 2024, c.Candidate); } }
            }
            return null;
        }

        /// <summary>PS-6 US-6: the day the 2024 field names a party's nominee from - MaxValue where the date is billed (the Republican side); false for a
        /// country or party with no nominee of record.</summary>
        public static bool TryNamedFrom(CountryId id, string party, out DateTime namedFrom)
        {
            namedFrom = DateTime.MinValue;
            if (id != CountryId.USA) { return false; }
            foreach (var n in UsNominations2024) { if (n.Party == party) { namedFrom = n.NamedFrom; return true; } }
            return false;
        }

        /// <summary>§772 (E1): a candidate of record's share of the first round's valid votes (a fraction), read from the PKW's returns as generated -
        /// the base a candidate with no roster party carries; NaN where the returns do not hold the candidate. PS-6 US-6: for the USA, a column's
        /// national vote over the FEC's total - every column of the year together (the jurisdictions' total, held so by US-5's instrument).</summary>
        public static double ShareOfRecord(CountryId id, int year, string surname)
        {
            if (id == CountryId.USA)
            {
                long all = 0, votes = -1;
                foreach (var c in Generated.UsPresidentialReturns.Candidates)
                {
                    if (c.Year != year) { continue; }
                    all += c.Votes;
                    if (c.Candidate == surname) { votes = c.Votes; }
                }
                return votes >= 0 && all > 0 ? (double)votes / all : double.NaN;
            }
            if (id != CountryId.Poland) { return double.NaN; }
            long valid = 0;
            foreach (var r in Generated.PolishPresidentialReturns.Rounds) { if (r.Year == year && r.Round == 1) { valid = r.Valid; } }
            foreach (var v in Generated.PolishPresidentialReturns.Votes)
            {
                if (v.Year == year && v.Round == 1 && valid > 0 && v.Candidate.Split(' ')[0] == surname) { return (double)v.Votes / valid; }
            }
            return double.NaN;
        }

        /// <summary>The president of record on <paramref name="date"/>; false before the first the record holds, or where none is held.</summary>
        public static bool TryAt(CountryId id, DateTime date, out President president)
        {
            foreach (President p in Of(id)) { if (date >= p.TookOffice && date < p.Until) { president = p; return true; } }
            president = default;
            return false;
        }
    }
}
