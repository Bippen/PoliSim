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
    /// in the sentence rather than filling itself. The optional tagline is a labelled slot, empty until Elias reviews one. Where `WorldClock.NoElectionYet` held (Italy's snap start until PS-7, the USA's until PS-6 US-8) the election's
    /// slot reported what held instead; no start holds it now, and every start with a polling day carries it (`StartBriefDiagnostic`). PS-6 US-8: where the polling day
    /// elects no chamber (`WorldClock.RecordSeatsChamber` - the USA's House, by date until US-16), the polling-day clause says what it elects and what stays
    /// the record's, after the same "Polling day is ..." sentence every start's brief carries. PS-6 US-15: where the record seats a second chamber
    /// (`WorldClock.RecordSeatsSecondChamber` - the USA's Senate, by date until US-16), a clause of its own follows the government's - the Senate of record and
    /// its majority on the start's day, the independents counted with their conference (R-US10 (a)) - and the ledger a row of its own beside the House's.
    /// PS-6 US-33: where the player's own nominee is registered for the start's country (`OwnNominees`), a last clause and a last row name it and its ticket
    /// (<see cref="NomineeClause"/>, <see cref="NomineeRow"/>); with none registered every clause and row is as before.
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

        private static readonly Dictionary<CountryId, string> SecondChamberName = new Dictionary<CountryId, string> { { CountryId.USA, "Senate" } };

        /// <summary>PS-6 US-15: the second chamber's name, as the brief, the cards and the Parliament page say it - the Senate; null where no second chamber is
        /// seated (Italy's Senato joins at PS-7).</summary>
        public static string SecondChamberOf(CountryId id) => SecondChamberName.TryGetValue(id, out string name) ? name : null;

        /// <summary>PS-6 US-15 (R-US10 (a); Art. I §3 cl. 4): the second chamber of record's majority on a date - the party holding it, its seats, the
        /// independents counted with it, the members, the vacant seats, and whether it is the vice president of record's party's on an equal division (the
        /// vice president by PS-6 US-28's one derivation, <see cref="UsTermLimit.TryVicePresidentOfRecordOn"/>). False where the record seats no second chamber,
        /// or none on the date.</summary>
        public static bool TrySecondChamberMajority(CountryId id, DateTime on, out string party, out int seats, out int independents, out int members, out int vacant, out bool byVicePresident)
        {
            party = null; seats = independents = members = vacant = 0; byVicePresident = false;
            if (!WorldClock.RecordSeatsSecondChamber(id)) { return false; }
            UsSenate.Seating senate = UsSenate.On(on);
            UsTermLimit.TryVicePresidentOfRecordOn(id, on, out _, out string vicePresidentParty, out _);
            party = UsSenate.MajorityOf(senate.BySide, vicePresidentParty);
            seats = party != null && senate.BySide.TryGetValue(party, out int held) ? held : 0;
            independents = party != null && senate.IndependentsBySide.TryGetValue(party, out int counted) ? counted : 0;
            members = senate.Members;
            vacant = senate.Vacant;
            byVicePresident = UsSenate.EquallyDivided(senate.BySide);
            return party != null;
        }

        private static string Independents(int n) => n == 1 ? "1 independent" : n.ToString(CultureInfo.InvariantCulture) + " independents";

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
            clauses.Add(new Clause(country + ", " + Long(opens) + ".", (PresidentialStart.IsCandidateStart(start) ? "StartPoints - PresidentialStart.PolandOpens (" : "WorldClock.StartDate (") + start.Basis + ")"));   // SP-7b: a candidate's start opens on its own day

            WorldClock.ChamberOfRecord chamber = WorldClock.ChamberAt(id, opens);
            ElectionVintage seated = WorldClock.SeatedVintage(id, opens);
            Dictionary<string, int> seats = PartySystems.InitialSeats(id, seated);
            int size = 0, partiesSeated = 0, remainder = 0;
            foreach (KeyValuePair<string, int> kv in seats)
            {
                size += kv.Value;
                if (kv.Value <= 0) { continue; }
                if (PartySystems.IsGroupRemainder(id, kv.Key)) { remainder += kv.Value; } else { partiesSeated++; }   // SEAT-1 (L3): a group's remainder is no party
            }

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
                    clauses.Add(new Clause(PoliSim.Wording.EnglishText.Possessive(government.Head) + " government (" + string.Join("+", government.Cabinet) + ") has governed since " + Long(government.From)
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

            // PS-6 US-15 (R-US1 (a), R-US10 (a)): the second chamber of record on the start's day, "of record" as the House is - its majority, the independents
            // counted with the conference they caucus with, an equal division the vice president of record's party's (Art. I §3 cl. 4: the party, not a vote the
            // record shows), a vacant seat said
            if (TrySecondChamberMajority(id, opens, out string upperParty, out int upperSeats, out int upperIndependents, out int upperMembers, out int upperVacant, out bool upperByVice))
            {
                UsTermLimit.TryVicePresidentOfRecordOn(id, opens, out string vicePresident, out _, out _);
                clauses.Add(new Clause("The " + SecondChamberOf(id) + " of record is seated by its own dates; its majority is " + upperParty + ", with " + upperSeats.ToString(CultureInfo.InvariantCulture)
                    + " of " + upperMembers.ToString(CultureInfo.InvariantCulture) + " seats" + (upperIndependents > 0 ? ", including " + Independents(upperIndependents) + " counted with " + upperParty : string.Empty)
                    + (upperByVice ? ", an equal division held by the party of the vice president, " + vicePresident : string.Empty)
                    + (upperVacant > 0 ? "; " + upperVacant.ToString(CultureInfo.InvariantCulture) + (upperVacant == 1 ? " seat stands" : " seats stand") + " vacant" : string.Empty) + ".",
                    "WorldClock.SecondChamberOfRecord (UsSenate.On - US-12's table, senate_seats.csv, each independent with his conference by R-US10 (a)); UsTermLimit.TryVicePresidentOfRecordOn (Art. I §3 cl. 4)"));
            }

            if (start.PollingDay != DateTime.MinValue && WorldClock.RecordSeatsChamber(id))
            {
                // PS-6 US-8 (R-US1 (a)): the polling day elects the president through the Electoral College and no chamber - the House stays the record's, seated on
                // the record's day, named and dated from its own rows (US-2); the record's next president is not named: the game's count decides who takes office.
                // PS-6 US-15: the Senate of record has its own clause above, so this one no longer says that none is modelled
                // PS-6 US-16: the day elects the House and the Senate seats up too, seated on the 3rd of January after it - the game's own Congress, not the record's
                string house = UsCongressElection.ElectsIn(id)
                    ? "the House and the " + SecondChamberOf(id) + " seats up are elected that day, and the new Congress is seated on " + Long(UsCongressElection.ConvenesAfter(start.PollingDay))
                    : WorldClock.TryNextChamberOfRecord(id, opens, out WorldClock.ChamberOfRecord nextHouse)
                    ? (nextHouse.ElectionDay == start.PollingDay ? "the House of record elected that day" : nextHouse.ElectionDay == DateTime.MinValue ? "the next House of record" : "the House of record elected on " + Long(nextHouse.ElectionDay))
                      + " is seated on " + Long(nextHouse.Convened) + ", as the record dates it"
                    : "the House of record stands, by its dates";
                clauses.Add(new Clause("Polling day is " + Long(start.PollingDay) + ". The president is elected through the Electoral College; " + house
                    + (WorldClock.RecordSeatsSecondChamber(id) ? "; until then the " + SecondChamberOf(id) + " of record changes on its own dates." : "."),
                    "StartPoints (" + start.Basis + "); WorldClock.TryNextPollingDay (3 U.S.C. §21(1)); UsCongressElection (the House and the Senate class, seated on 3 January)"));
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
            string declared = WorldClock.SeatingDeclaration(id, opens);   // SEAT-1 (J2): Italy's XVIII Camera by its groups, DECLARED - said where it is seated
            string positions = declared != null ? PartySystems.PositionDeclaration(id) : null;   // SEAT-1 (L1): AVS's runtime point beside the fit's
            clauses.Add(new Clause(partiesSeated.ToString(CultureInfo.InvariantCulture) + " parties sit in the " + chamberName
                + (remainder > 0 ? ", with " + remainder.ToString(CultureInfo.InvariantCulture) + (remainder == 1 ? " member" : " members") + " of its mixed group in no component" : string.Empty)
                + " (" + size.ToString(CultureInfo.InvariantCulture) + " seats, the election of "
                + Long(chamber.ElectionDay == DateTime.MinValue ? chamber.Convened : chamber.ElectionDay) + ")" + (deviation != null ? " - " + deviation : string.Empty)
                + (declared != null ? "; " + declared : string.Empty) + (positions != null ? ". " + positions : string.Empty) + ".",
                "PartySystems.InitialSeats(" + seated + "); " + chamber.Basis));
            // PS-6 US-33 (SP-6a): the player's own nominee, where one is registered for the start's country - a clause of its own, last; none registered, none said
            if (OwnNominees.TryOf(id, out OwnNominee own) && NomineeClause(start, own) is Clause nominee) { clauses.Add(nominee); }
            // SP-7b (I14): a candidate's start says the contest - the president of record and his term's end, the run-off's day, the field - and that it opens on
            // the Marshal's order, a deviation for presidential starts alone, with no campaign staged; two clauses, last
            clauses.AddRange(CandidateClauses(start));
            return clauses;
        }

        /// <summary>SP-7b (I14, asks 1, 3 and 4): a candidate's start's two clauses - the contest (the president of record and his term's end, the run-off of
        /// record's day, the field and how many of it a Sejm party backs) and the start's own terms (the Marshal's order, a stated deviation; no campaign
        /// staged). Empty for any other start.</summary>
        public static List<Clause> CandidateClauses(StartPoints.StartPoint start)
        {
            var clauses = new List<Clause>();
            if (!PresidentialStart.IsCandidateStart(start)) { return clauses; }
            CountryId id = start.Country;
            PresidencyOfRecord.TryAt(id, start.Opens, out PresidencyOfRecord.President sitting);
            TwoRoundElection.RoundOfRecord(id, start.Year, 2, out DateTime runOff);
            List<PresidentialStart.Offer> offers = PresidentialStart.Offers(start);
            int pickable = offers.FindAll(o => o.Pickable).Count;
            clauses.Add(new Clause("The president is " + (sitting.Name ?? "not of record") + ", whose term ends on " + Long(sitting.Until) + "; the first vote is on " + Long(start.PollingDay)
                + ", and the run-off, if no one has more than half, on " + Long(runOff) + ". " + offers.Count.ToString(CultureInfo.InvariantCulture) + " candidates stand of record, "
                + pickable.ToString(CultureInfo.InvariantCulture) + " of them backed by a party in the " + ChamberOf(id) + " - you stand as one of the " + pickable.ToString(CultureInfo.InvariantCulture) + ", and your party is the one that backs you.",
                "StartPoints (" + start.Basis + "); PresidencyOfRecord (records_by_date.md §2d); TwoRoundElection.RoundOfRecord (the PKW's notices); PresidentialStart.Offers (PresidencyOfRecord.CandidatesOf, PartySystems.InitialSeats)"));
            clauses.Add(new Clause("The game opens on " + Long(start.Opens) + ", the day the Marshal of the Sejm ordered the election and its campaign opened - not the standard run-up, for presidential starts alone. No campaign is staged: your candidate's vote moves with your party's standing until the first vote.",
                "StartPoints, PresidentialStart.PolandOpens (Dz.U. 2025 poz. 48; Kodeks wyborczy art. 104) - ruled I14 (4b) and (3a); WorldClock.StartDate keeps the Sejm start's own rule"));
            return clauses;
        }

        /// <summary>SP-7b: a candidate's start's four ledger rows - the term's end, the run-off, the field, how the start opens. Empty for any other start.</summary>
        public static List<Row> CandidateRows(StartPoints.StartPoint start)
        {
            var rows = new List<Row>();
            if (!PresidentialStart.IsCandidateStart(start)) { return rows; }
            CountryId id = start.Country;
            PresidencyOfRecord.TryAt(id, start.Opens, out PresidencyOfRecord.President sitting);
            TwoRoundElection.RoundOfRecord(id, start.Year, 2, out DateTime runOff);
            List<PresidentialStart.Offer> offers = PresidentialStart.Offers(start);
            int pickable = offers.FindAll(o => o.Pickable).Count;
            rows.Add(new Row("Term ends", Stamp(sitting.Until) + " · " + (sitting.Name ?? "-").ToUpperInvariant(), "PresidencyOfRecord (records_by_date.md §2d)"));
            rows.Add(new Row("Run-off", Stamp(runOff) + " · IF NO ONE HAS MORE THAN HALF", "TwoRoundElection.RoundOfRecord (the PKW's notices; Art. 127 ust. 4)"));
            rows.Add(new Row("Candidates", offers.Count.ToString(CultureInfo.InvariantCulture) + " OF RECORD · " + pickable.ToString(CultureInfo.InvariantCulture) + " BACKED BY A PARTY IN THE " + ChamberOf(id).ToUpperInvariant(),
                "PresidentialStart.Offers (PresidencyOfRecord.CandidatesOf, PartySystems.InitialSeats)"));
            rows.Add(new Row("Opens on", "THE MARSHAL'S ORDER · NO CAMPAIGN IS STAGED", "StartPoints, PresidentialStart.PolandOpens (Dz.U. 2025 poz. 48) - ruled I14 (4b), (3a)"));
            return rows;
        }

        private static string PartyNameOf(CountryId id, string key)
        {
            foreach (PoliticalParty p in PartySystems.RealRoster(id)) { if (p.Abbrev == key) { return p.Name; } }
            return key;
        }

        /// <summary>
        /// PS-6 US-33 (SP-6a; the own nominee in place of R-US2 (a)'s nominee of record for the player's party - the plan's "a leader at the head of the DEM or REP ticket"): the brief's clause for the player's own nominee - who heads
        /// which ticket in place of its nominee of record, and that the party's poll reads the nominee's position - or null where none is given, it is unnamed, or
        /// the start offers none (<see cref="StartPoints.OffersOwnNominee"/>). It names no nominee of record (the naming before the count is US-6's, by date - asked
        /// of Elias with R-US23). Read from the nominee given, so the creation flow's review shows it before the nominee is registered; <see cref="Clauses"/> reads
        /// the registered one (`OwnNominees`).
        /// </summary>
        public static Clause? NomineeClause(StartPoints.StartPoint start, OwnNominee nominee)
        {
            if (nominee == null || nominee.Country != start.Country || !StartPoints.OffersOwnNominee(start) || string.IsNullOrWhiteSpace(nominee.Name)) { return null; }
            return new Clause("Your own nominee, " + nominee.Name.Trim() + ", heads the " + PoliSim.Wording.EnglishText.Possessive(PartyNameOf(start.Country, nominee.Party)) + " ticket in place of its nominee of record; the party's poll reads the nominee's position.",
                "OwnNominees (the player's own nominee, inside the party's range - the middle half of the Global Party Survey 2019's experts, UsGps2019); StartPoints.OffersOwnNominee (" + start.Basis + ")");
        }

        /// <summary>PS-6 US-33: the ledger's row for the player's own nominee - "Your nominee", the name and the ticket the president row's way ("Jordan Hale (REP)") -
        /// or null where <see cref="NomineeClause"/> is.</summary>
        public static Row? NomineeRow(StartPoints.StartPoint start, OwnNominee nominee)
        {
            if (!(NomineeClause(start, nominee) is Clause clause)) { return null; }
            return new Row("Your nominee", nominee.Name.Trim() + " (" + PartySystems.ShortName(start.Country, nominee.Party) + ")", clause.Basis);
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
            foreach (KeyValuePair<string, int> kv in seats) { size += kv.Value; if (kv.Value > 0 && !PartySystems.IsGroupRemainder(id, kv.Key)) { partiesSeated++; } }   // SEAT-1 (L3)

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

            // PS-6 US-15 (R-US10 (a): the chamber card says "including N independents"): the second chamber of record's majority, beside the House's
            if (TrySecondChamberMajority(id, opens, out string upperParty, out int upperSeats, out int upperIndependents, out int upperMembers, out int upperVacant, out bool upperByVice))
            {
                rows.Add(new Row(SecondChamberOf(id), PartySystems.ShortName(id, upperParty) + " · " + upperSeats.ToString(CultureInfo.InvariantCulture) + " OF " + upperMembers.ToString(CultureInfo.InvariantCulture)
                    + (upperIndependents > 0 ? " · INCLUDING " + Independents(upperIndependents).ToUpperInvariant() : string.Empty)
                    + (upperByVice ? " · AN EQUAL DIVISION · THE VICE PRESIDENT'S PARTY" : string.Empty)
                    + (upperVacant > 0 ? " · " + upperVacant.ToString(CultureInfo.InvariantCulture) + " VACANT" : string.Empty),
                    "WorldClock.SecondChamberOfRecord (UsSenate.On - US-12's table, senate_seats.csv; R-US10 (a)); UsTermLimit.TryVicePresidentOfRecordOn (Art. I §3 cl. 4)"));
            }

            if (start.PollingDay != DateTime.MinValue)
            {
                // PS-6 US-8: the USA's start has its polling day here since its count is built (the House stays the record's - the brief's clause says so)
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
            // PS-6 US-33 (SP-6a): the player's own nominee's row, last, where one is registered for the start's country
            if (OwnNominees.TryOf(id, out OwnNominee own) && NomineeRow(start, own) is Row nominee) { rows.Add(nominee); }
            rows.AddRange(CandidateRows(start));   // SP-7b: a candidate's start's rows, last
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
