using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-8 (`POLISIM_FEATURE_LIST.md` row PS-8; `docs/specs/POLITICAL_SYSTEM_SPEC.md` §3; §618's ruling 4; Elias's rulings K159-K163): <b>EVERY COUNTRY'S
    /// CALENDAR</b> - the days an AI country votes on, read off its own law, and what its count and its formation are owed. Pure: every answer here is read
    /// off the country's own state (its held counts, its government) and the clock, so a world and a fork of it answer alike; the day loop that holds the
    /// elections is `SimulationManager.WorldCalendar.cs`.
    /// <list type="bullet">
    /// <item><description><b>The ordinary days</b> are <see cref="WorldClock.TryNextPollingDay"/>'s - Sweden's second Sunday of September every fourth year,
    /// Germany's first Sunday of Art. 39 GG's window (K159), Poland's Art. 98 ust. 2 window, Italy's last Sunday before the term's fifth anniversary (K156),
    /// re-anchored on the country's own last count (K206: a dissolution starts a new term), Poland's re-anchored the same way after a shortened term (K302,
    /// Art. 98 ust. 6: `SejmFormation.OrdinaryAfter`), the USA's even Novembers (its own counts,
    /// `UsPresidentialElection`, `UsCongressElection`) - and Poland's presidential first vote by `PresidentialElection.TryNextFirstVote`.</description></item>
    /// <item><description><b>The snap elections</b> (K161) are the confidence procedures' own, where the model runs them for an AI country: an AI country's
    /// government is formed by its procedure after each count (<see cref="FirstSittingAfter(Country, DateTime)"/>), and where the procedure fails the statute's own election
    /// follows - the Riksdag's round broken off (RF 6 kap. 5 §: four rejected proposals; and, as the player's round breaks off in `AskNext`, a round with no
    /// party to ask), the extra election within three months unless the ordinary falls in them; Italy's three failed mandates (art. 88, the semestre bianco of
    /// art. 88 c. 2, art. 61's polling day); the Sejm's procedure run to its end (K302: Art. 155 ust. 1's simple majority refused, the President shortens the
    /// term and orders elections, ust. 2, within forty-five days, Art. 98 ust. 5 - `SejmFormation.ShortenedTermElectionDay`). With no player's party in an AI
    /// country every party asked tables and is voted on, so an AI round ends only in an
    /// investiture won, four rejections, or no party to ask. DECLARED: no mid-term confidence procedure runs in an AI country (COAL-1's exits and motions are
    /// the player's country's, K130-K133), so an AI country holds no other snap election. The Bundestag's failed first ballot goes on by Art. 63 Abs. 3-4
    /// through the player's own round (`SimulationManager.ContinueChancellorElection`: the fourteen days, then the ballot the most votes win, the
    /// Bundespräsident appointing - §713's premise; a dissolution under Abs. 4 Satz 3 is not taken, so no German extra election). DECLARED, as K204's mandates
    /// are: an AI Sejm's steps, Art. 155's with them, run whole on its first sitting, so its shortened term is ordered that day.</description></item>
    /// <item><description><b>France</b> (K162): no election is held - its chamber and government of record are seated by date to the record's end,
    /// <see cref="FranceRecordEnds"/>, then held, until SP-8 models its two rounds.</description></item>
    /// </list>
    /// The procedure's state is the country's own, so nothing is added to the save: a count is due on a polling day no record of the country carries; the
    /// formation is due on and after the count's first sitting while the government in office was formed before that count and is no caretaker discharged on
    /// or after that sitting; an extra election is due where that discharge is the procedure's failure (Sweden's, Italy's and, since K302, Poland's). A Polish
    /// count's first sitting is read off the country's own counts too (<see cref="FirstSittingAfter(Country, DateTime)"/>: Art. 98 ust. 5's fifteenth day after
    /// a shortened term's election).
    /// </summary>
    public static class WorldCalendar
    {
        /// <summary>K162 (Elias, 2026-10-27): "France's record seated by date to its end (25 Sep 2026), then held, until SP-8".</summary>
        public static readonly DateTime FranceRecordEnds = new DateTime(2026, 9, 25);

        /// <summary>SOURCED - RF 3 kap. 10 § (`sweden/raw/records/riksdagen_sfs-1974-152_regeringsformen.html`): "Den nyvalda riksdagen samlas på den femtonde dagen
        /// efter valdagen" - the proviso that follows ("dock tidigast på den fjärde dagen efter det att valresultatet har kungjorts") is not modelled, DECLARED.</summary>
        public const int SwedenFirstSittingAfterDays = 15;

        /// <summary>Whether the game counts this country's chamber as an AI country - the four whose chamber count is built (`ParliamentaryCount.CountsIn`);
        /// the USA's Congress is counted by its own model, France holds none (K162).</summary>
        public static bool CountsChamber(CountryId id) => ParliamentaryCount.CountsIn(id) && !WorldClock.RecordSeatsChamber(id);

        /// <summary>K162: whether the country is seated by its record's dates alone - France, whose election is not modelled (R-EL10) - and through which day.</summary>
        public static bool SeatedByRecordAlone(CountryId id) => WorldClock.GoverningModeOnly(id);

        /// <summary>The day the chambers a polling day elects first sit, each by its statute - the record's own convening for an election of record (the
        /// chamber table, `WorldClock.Chambers`); else Sweden's fifteenth day (RF 3 kap. 10 §), Germany's thirtieth (Art. 39 Abs. 2 GG, its last day,
        /// <see cref="WorldClock.GermanyFirstSittingAfterDays"/>), Poland's thirtieth (Art. 109 ust. 2, E2), Italy's twentieth (art. 61,
        /// <see cref="WorldClock.ItalyFirstSittingAfter"/>). An AI country's government is formed on this day by its procedure, run whole that day (DECLARED, as
        /// K204's mandates are: the record's own intervals between the sitting and the appointment are not dated); the government in office governs until then.</summary>
        public static DateTime FirstSittingAfter(CountryId id, DateTime pollingDay)
        {
            foreach (WorldClock.ChamberOfRecord c in WorldClock.Chambers(id)) { if (c.ElectionDay == pollingDay.Date) { return c.Convened; } }
            switch (id)
            {
                case CountryId.Sweden: return pollingDay.Date.AddDays(SwedenFirstSittingAfterDays);
                case CountryId.Germany: return pollingDay.Date.AddDays(WorldClock.GermanyFirstSittingAfterDays);
                case CountryId.Poland: return pollingDay.Date.AddDays(WorldClock.PolandFirstSittingAfterDays);
                case CountryId.Italy: return WorldClock.ItalyFirstSittingAfter(pollingDay);
                default: return pollingDay.Date;
            }
        }

        /// <summary>K302: <see cref="FirstSittingAfter(CountryId, DateTime)"/> read off the country's own counts - a Polish Sejm a shortened term's election chooses first
        /// sits on Art. 98 ust. 5's fifteenth day (`SejmFormation.FirstSittingAfter(Country, DateTime)`); every other answer is the statute's by the country alone.</summary>
        public static DateTime FirstSittingAfter(Country country, DateTime pollingDay) =>
            country != null && country.Id == CountryId.Poland ? SejmFormation.FirstSittingAfter(country, pollingDay) : FirstSittingAfter(country?.Id ?? default, pollingDay);

        /// <summary>What a record carries where the vote model could not predict its polling day of record and the chamber of record was seated in its place.</summary>
        public const string RecordSeatedReason = "The vote model holds no prediction for this day: the chamber of record is seated, its election's own lists.";

        /// <summary>The measurement's (1): the chamber an election of record seats, as a record the calendar reads like a count - the country's own method, the
        /// record's seats (`PartySystems.InitialSeats`) and its shares over the roster's total (`PartySystems.TryHistory`), <see cref="RecordSeatedReason"/> on it.
        /// DECLARED: Italy's Senato is not seated from it (no AI Italy votes on a day of record: every start where Italy is not the player's opens after 25 Sep 2022).</summary>
        public static ElectionRecord RecordSeatedOn(CountryId id, int turn, DateTime day, ElectionVintage vintage)
        {
            var record = new ElectionRecord
            {
                Turn = turn, Date = day, CountryId = id.ToString(), NotHeldReason = RecordSeatedReason,
                Method = id == CountryId.Sweden ? ElectionMethod.SwedenTwoTier : id == CountryId.Germany ? ElectionMethod.GermanyNationalProportional
                    : id == CountryId.Poland ? ElectionMethod.PolandDistricts : id == CountryId.Italy ? ElectionMethod.ItalyRosatellum : ElectionMethod.NotImplemented,
            };
            foreach (KeyValuePair<string, int> kv in PartySystems.InitialSeats(id, vintage)) { record.Seats[kv.Key] = kv.Value; }
            if (PartySystems.TryHistory(id, out double[] shares, out double[] _, vintage) && shares != null)
            {
                PoliticalParty[] roster = PartySystems.RealRoster(id);
                double total = 0.0;
                foreach (double s in shares) { total += s; }
                for (int i = 0; i < roster.Length && i < shares.Length && total > 0.0; i++) { record.Shares[roster[i].Abbrev] = shares[i] / total; }
            }
            return record;
        }

        /// <summary>The country's latest chamber count the game held (a parliamentary method, `NationalElection.CountsParliament`), false before its first.</summary>
        public static bool TryLatestCount(Country country, out ElectionRecord latest)
        {
            latest = null;
            if (country?.ElectionHistory == null) { return false; }
            foreach (ElectionRecord e in country.ElectionHistory) { if (NationalElection.CountsParliament(e.Method) && (latest == null || e.Date > latest.Date)) { latest = e; } }
            return latest != null;
        }

        /// <summary>The country's next ordinary polling day on or after <paramref name="onOrAfter"/> - <see cref="WorldClock.TryNextPollingDay"/>, Italy's re-anchored on
        /// its own latest count (K206; `WorldClock.ItalyOrdinaryAfter`, as the player's Italy is), Poland's on its own latest count's term (K302, Art. 98 ust. 6:
        /// `SejmFormation.OrdinaryAfter`, the same chain day for day until a term is shortened - asked on the last count's own day too, which it answers with
        /// that day, as the chain does on an ordinary polling day: the review's L1, where the chain's next day showed on the evening of the first ordinary
        /// election after a shortened term).</summary>
        public static bool TryNextOrdinaryDay(Country country, DateTime onOrAfter, out DateTime day)
        {
            if (country == null || !WorldClock.TryNextPollingDay(country.Id, onOrAfter, out day)) { day = DateTime.MinValue; return false; }
            if (country.Id == CountryId.Italy && TryLatestCount(country, out ElectionRecord last)) { day = WorldClock.ItalyOrdinaryAfter(last.Date, onOrAfter); }
            if (country.Id == CountryId.Poland && TryLatestCount(country, out ElectionRecord lastPolish) && lastPolish.Date.Date <= onOrAfter.Date) { day = SejmFormation.OrdinaryAfter(country, lastPolish.Date, onOrAfter); }
            return true;
        }

        /// <summary>K302 (the review's L6): an AI Poland's caretaker after Art. 155 ust. 1's refusal - the President's cabinet, installed on the new Sejm's first sitting
        /// and discharged that day (Art. 162 ust. 2 pkt 1, ust. 3; `SimulationManager.FormAsAi`) - formed on or after the sitting, unlike every other caretaker the
        /// calendar reads (the government in office before the count, discharged on the sitting). The art155 review's R2-L1: and carrying the failure line
        /// `FormAsAi` writes on it the day it installs it (<see cref="FailureMarker"/>, dated its <see cref="GovernmentRecord.FormedOn"/>) - so a player's
        /// Art. 154 ust. 2 designee, refused and carrying on while ust. 3 runs, is never read as Art. 155's; a lapse ordered again (L5) writes another line and
        /// leaves this one standing.</summary>
        private static bool RefusedPresidentsCabinet(Country country, GovernmentRecord g, DateTime firstSitting)
        {
            if (ConfidenceProcedure.RulesOf(country.Id) != ConfidenceProcedure.Rules.Sejm || !g.Caretaker || g.FormedOn < firstSitting || g.CaretakerSince < firstSitting) { return false; }
            string head = g.FormedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ": " + FailureMarker;
            foreach (string line in g.Breaks) { if (line.StartsWith(head, StringComparison.Ordinal)) { return true; } }
            return false;
        }

        /// <summary>Whether the formation after the country's latest count is still owed on <paramref name="today"/>: on or after that count's first sitting, the
        /// government in office formed before the count, and no caretaker discharged on or after the sitting (the procedure's own end).</summary>
        public static bool FormationDue(Country country, DateTime today, out ElectionRecord counted, out DateTime firstSitting)
        {
            firstSitting = DateTime.MinValue;
            if (!TryLatestCount(country, out counted)) { return false; }
            firstSitting = FirstSittingAfter(country, counted.Date);
            GovernmentRecord g = country.Government;
            if (today.Date < firstSitting || g == null) { return false; }
            if (g.FormedOn > counted.Date) { return false; }
            return !(g.Caretaker && g.CaretakerSince >= firstSitting);
        }

        /// <summary>
        /// K161: the extra election the country's procedure owes, where its formation after the latest count failed - the government discharged on or after that
        /// count's first sitting. Sweden (RF 6 kap. 5 §): the Sunday on or before three months after the discharge (`ConfidenceProcedure.ExtraElectionDay`), unless
        /// the ordinary election falls within the three months and serves. Italy (art. 88, K155, K206): the chambers dissolved that day - or on the day the
        /// semestre bianco lifts (art. 88 c. 2) - and the new ones elected on art. 61's day (`ConfidenceProcedure.ItalianElectionDay`). Poland (K302, Art. 155
        /// ust. 2): the President shortens the Sejm's term that day and orders elections on Art. 98 ust. 5's last Sunday (`SejmFormation.ShortenedTermElectionDay`) -
        /// the steps ran whole on the first sitting, so the Sejm's failure there is Art. 155's, and its caretaker the President's refused cabinet
        /// (<see cref="RefusedPresidentsCabinet"/>). False for every other country and outcome; once the extra election is held, its count is the latest and nothing
        /// is owed.
        /// </summary>
        public static bool TryExtraElection(Country country, out DateTime day, out DateTime orderedOn, out string why)
        {
            day = orderedOn = DateTime.MinValue;
            why = null;
            if (country?.Government == null || !TryLatestCount(country, out ElectionRecord counted)) { return false; }
            GovernmentRecord g = country.Government;
            DateTime firstSitting = FirstSittingAfter(country, counted.Date);
            if (!g.Caretaker || g.CaretakerSince < firstSitting || (g.FormedOn > counted.Date && !RefusedPresidentsCabinet(country, g, firstSitting))) { return false; }
            orderedOn = g.CaretakerSince;
            switch (ConfidenceProcedure.RulesOf(country.Id))
            {
                case ConfidenceProcedure.Rules.Riksdag:
                    if (TryNextOrdinaryDay(country, orderedOn, out DateTime ordinary) && ordinary <= orderedOn.AddMonths(ConfidenceProcedure.ExtraElectionMonths)) { return false; }
                    day = ConfidenceProcedure.ExtraElectionDay(orderedOn);
                    // M1 (the s938 review): the round's own reason as the formation recorded it on the government - four rejections, or no party to ask
                    string failure = FormationFailure(g, orderedOn)?.Replace(" (RF 6 kap. 5 §)", string.Empty);   // L-N3: the citation once, at the end
                    why = (failure ?? "the Speaker's round broke off with no prime minister approved") + " - an extra election within three months (RF 6 kap. 5 §)";
                    return true;
                case ConfidenceProcedure.Rules.Parlamento:
                    DateTime termFirstSitting = WorldClock.ItalyFirstSittingAfter(counted.Date);
                    bool barred = WorldClock.ItalyDissolutionBarred(orderedOn, termFirstSitting, out DateTime opens);
                    DateTime dissolvedOn = barred ? opens : orderedOn;
                    day = ConfidenceProcedure.ItalianElectionDay(dissolvedOn);
                    why = ConfidenceProcedure.ItalianMandates.ToString(CultureInfo.InvariantCulture) + " mandates failed - the chambers dissolved"
                        + (barred ? " on " + dissolvedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ", when the President's last six months end (art. 88 c. 2)" : " (art. 88)")
                        + ", the new ones elected within seventy days (art. 61)";
                    return true;
                case ConfidenceProcedure.Rules.Sejm:
                    // K302: the Sejm's procedure has one failure, Art. 155 ust. 1's refusal, and one order after it (ust. 2)
                    day = SejmFormation.ShortenedTermElectionDay(orderedOn);
                    why = "the Sejm refused the President's cabinet its confidence (Art. 155 ust. 1) - the President shortened the Sejm's term and ordered elections within"
                        + " forty-five days (Art. 155 ust. 2, Art. 98 ust. 5)";
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>The words of the procedure's failure the formation wrote on the government's breaks on <paramref name="on"/> (`SimulationManager.FormAsAi`'s line,
        /// between its dash and its semicolon), null where none is there.</summary>
        public static string FormationFailure(GovernmentRecord g, DateTime on)
        {
            string head = on.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ": " + FailureMarker;
            for (int i = g.Breaks.Count - 1; i >= 0; i--)
            {
                string line = g.Breaks[i];
                if (!line.StartsWith(head, StringComparison.Ordinal)) { continue; }
                int dash = line.IndexOf(" - ", head.Length, StringComparison.Ordinal), semi = line.LastIndexOf("; ", StringComparison.Ordinal);
                return dash >= 0 && semi > dash ? line.Substring(dash + 3, semi - dash - 3) : null;
            }
            return null;
        }

        /// <summary>The opening words of the line the formation writes on the government it leaves in office when it forms none.</summary>
        public const string FailureMarker = "no government was formed after the election of ";

        /// <summary>The chamber election the country holds on <paramref name="date"/>: its ordinary day or its procedure's extra election, where no record of the
        /// country is dated that day; false otherwise (and for a country whose chamber the game does not count as an AI country's).</summary>
        public static bool IsChamberPollingDay(Country country, DateTime date, out bool extra)
        {
            extra = false;
            if (country == null || !CountsChamber(country.Id)) { return false; }
            foreach (ElectionRecord e in country.ElectionHistory) { if (e.Date.Date == date.Date) { return false; } }
            if (TryExtraElection(country, out DateTime extraDay, out DateTime _, out string _) && extraDay == date.Date) { extra = true; return true; }
            return TryNextOrdinaryDay(country, date, out DateTime ordinary) && ordinary == date.Date;
        }

        // ------------------------------------------------------------------------------------------------------------ what reaches the player (K163)

        /// <summary>CONVENTION: the days a foreign election stays on the player's Docket after it SETTLES - its polling day, or for the country's latest chamber
        /// count whose government its procedure forms (<see cref="Result.Formation"/>), the day that formation ended (<see cref="FormationEnded"/>). S6 (Design's
        /// return of 2026-10-30, the FORMED state): a formation still running keeps its count listed, so a formation that ends past the month is reached.</summary>
        public const int ShownForDays = 30;

        /// <summary>One list's line on a foreign result: its seats now and before, its share now and before (NaN where the earlier share is not on record).</summary>
        public sealed class Row
        {
            public string Key;
            public string Name;
            public int Seats, SeatsBefore;
            public double Share = double.NaN, ShareBefore = double.NaN;
        }

        /// <summary>K163: a foreign election as it reaches the player - what was elected, on which day, the lists by seat, and who governs now.</summary>
        public sealed class Result
        {
            public CountryId Country;
            public DateTime Day;
            /// <summary>The chamber or office elected, in the country's words ("the Riksdag", "the House and a Senate class", "the President").</summary>
            public string Elected;
            /// <summary>The verb the Docket's title puts before <see cref="Elected"/> - "elected", or for a first round with its run-off still to come "held the first round to elect" (L6).</summary>
            public string Verb = "elected";
            /// <summary>Whose seats the rows are, in the card's foot - the chamber's, or the Camera's alone where the Senato is elected beside it (L5).</summary>
            public string RowsAre = "the chamber's seats";
            public string Headline;
            public List<Row> Rows = new List<Row>();
            public int TotalSeats;
            public string Governs;
            /// <summary>S6: the chamber or office in one word, the Docket's caption (<see cref="ChamberWord"/>; "House"; "President").</summary>
            public string Chamber;
            /// <summary>S6 (Design's UPCOMING state): not yet held - <see cref="Day"/> is its polling day, ahead (<see cref="UpcomingAbroad"/>). A chamber's rows
            /// are the chamber sitting now, each list's seats in <see cref="Row.SeatsBefore"/>; nothing is counted.</summary>
            public bool Upcoming;
            /// <summary>S6: the chamber of record seated in place of a count (<see cref="RecordSeatedReason"/> on the record).</summary>
            public bool OfRecord;
            /// <summary>S6: a chamber count whose government the country's own procedure forms after it (the AI chambers, <see cref="CountsChamber"/>); false for
            /// the US House, which forms none, and for a president.</summary>
            public bool Formation;
            /// <summary>S6: the day the chamber elected first sits (<see cref="FirstSittingAfter(Country, DateTime)"/>; the US House's
            /// `UsCongressElection.Contest.Convenes`); MinValue for a president.</summary>
            public DateTime Seated = DateTime.MinValue;
            /// <summary>S6 (L5's headline, moved to the count's slip): the Senato elected beside the Camera's rows - its largest group's key and seats, and its
            /// total; null where no Senato is seated with the count.</summary>
            public string SenatoLargest;
            public int SenatoSeats, SenatoTotal;
            /// <summary>S6: a president's result at rest - who is elected and in which round, or the run-off's pair - the headline without its verb; null for a
            /// chamber.</summary>
            public string Line;
            /// <summary>A stable key for the Docket's open item.</summary>
            public string Key => Country.ToString() + ":" + Day.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ":" + Elected;
        }

        /// <summary>K163: every election a country other than the player's held in the <see cref="ShownForDays"/> to <paramref name="today"/>, newest first -
        /// its chamber counts (S6: the latest kept while its formation runs, and for the days after it ended, <see cref="Listed"/>), the USA's Congress and
        /// president, Poland's president.</summary>
        public static List<Result> RecentAbroad(World world, CountryId player, DateTime today)
        {
            var results = new List<Result>();
            if (world == null) { return results; }
            foreach (Country c in world.Countries)
            {
                if (c.Id == player) { continue; }
                foreach (ElectionRecord e in c.ElectionHistory)
                {
                    if (!NationalElection.CountsParliament(e.Method) || !Listed(c, e, today)) { continue; }
                    results.Add(ChamberResult(c, e, today));
                }
                if (c.UsCongressElections != null)
                {
                    foreach (UsCongressElection.Contest k in c.UsCongressElections) { if (Recent(k.PollingDay, today)) { results.Add(CongressResult(c, k)); } }
                }
                if (c.UsPresidentialElections != null)
                {
                    foreach (UsPresidentialElection.Contest p in c.UsPresidentialElections)
                    {
                        if (!Recent(p.PollingDay, today)) { continue; }
                        results.Add(new Result
                        {
                            Country = c.Id, Day = p.PollingDay.Date, Elected = "the President", Chamber = "President",
                            Headline = p.Decided() ? p.Winner + " (" + PartySystems.ShortName(c.Id, p.WinnerParty) + ") elected" + (p.Contingent ? " by the House" : string.Empty) : "No candidate has a majority of the electors",
                            Line = p.Decided() ? p.Winner + " (" + PartySystems.ShortName(c.Id, p.WinnerParty) + ")" + (p.Contingent ? " · by the House" : string.Empty) : "No elector majority",
                            Governs = GovernsLine(c, today),
                        });
                    }
                }
                if (c.PresidentialElections != null)
                {
                    foreach (PresidentialElection.Contest p in c.PresidentialElections)
                    {
                        if (!Recent(p.FirstVote, today) && !(p.RunOffHeld && Recent(p.RunOffOn, today))) { continue; }
                        results.Add(new Result
                        {
                            Country = c.Id, Day = p.RunOffHeld ? p.RunOffOn.Date : p.FirstVote.Date, Elected = "the President", Chamber = "President", Verb = p.RunOffPending() ? "held the first round to elect" : "elected",
                            Headline = p.Decided() ? p.Elected + " elected" + (p.RunOffHeld ? " in the run-off" : " in the first round")
                                : p.RunOffPending() ? p.RunOffA + " and " + p.RunOffB + " meet in the run-off on " + OnDay(p.RunOffOn) : "No one is elected",
                            Line = p.Decided() ? p.Elected + (p.RunOffHeld ? " · run-off" : " · first round")
                                : p.RunOffPending() ? p.RunOffA + " · " + p.RunOffB + " · run-off " + OnDay(p.RunOffOn) : "No one elected",
                            Governs = GovernsLine(c, today),
                        });
                    }
                }
            }
            results.Sort((a, b) => b.Day != a.Day ? b.Day.CompareTo(a.Day) : string.CompareOrdinal(a.Key, b.Key));
            return results;
        }

        private static bool Recent(DateTime day, DateTime today) => day != DateTime.MinValue && day.Date <= today.Date && (today.Date - day.Date).TotalDays <= ShownForDays;

        /// <summary>S6 (<see cref="ShownForDays"/>): whether a chamber count abroad is listed on <paramref name="today"/> - within the days after its polling day, or,
        /// for the country's latest count whose government its procedure forms, while that formation runs and for the days after it ended.</summary>
        private static bool Listed(Country country, ElectionRecord counted, DateTime today)
        {
            if (counted.Date.Date > today.Date) { return false; }
            if (Recent(counted.Date, today)) { return true; }
            if (!CountsChamber(country.Id) || !TryLatestCount(country, out ElectionRecord latest) || latest.Date != counted.Date) { return false; }
            DateTime? ended = FormationEnded(country, counted);
            return ended == null || (today.Date - ended.Value).TotalDays <= ShownForDays;
        }

        /// <summary>S6: the day the formation after <paramref name="counted"/> (the country's latest count) ended - the day the government its procedure formed took
        /// office, or the day the caretaker was discharged on or after the first sitting (the procedure's failure); null while it runs (before the first sitting,
        /// or the chancellor's election past its first ballot still open); the polling day where the country stores no government. Read off the country's
        /// own records, as <see cref="FormationDue"/> is.</summary>
        public static DateTime? FormationEnded(Country country, ElectionRecord counted)
        {
            GovernmentRecord g = country?.Government;
            if (g == null || counted == null) { return counted?.Date.Date; }
            if (g.Round != null && g.Round.Open && g.Round.Bundestag) { return null; }
            if (FormedAfter(country, counted)) { return g.FormedOn.Date; }
            DateTime sitting = FirstSittingAfter(country, counted.Date);
            if (g.Caretaker && g.CaretakerSince >= sitting) { return g.CaretakerSince.Date; }
            return null;
        }

        /// <summary>S6: the government in office is the one the country's procedure formed after <paramref name="counted"/> - formed after the count, and not the
        /// Sejm's refused cabinet of Art. 155 ust. 1 (K302: a caretaker though formed on the sitting, <see cref="RefusedPresidentsCabinet"/>).</summary>
        public static bool FormedAfter(Country country, ElectionRecord counted)
        {
            GovernmentRecord g = country?.Government;
            return g != null && counted != null && g.FormedOn > counted.Date && !RefusedPresidentsCabinet(country, g, FirstSittingAfter(country, counted.Date));
        }

        /// <summary>S6: whether any record of the country is dated <paramref name="day"/> - a count held that day, of whatever method.</summary>
        private static bool HeldOn(Country country, DateTime day)
        {
            foreach (ElectionRecord e in country.ElectionHistory) { if (e.Date.Date == day.Date) { return true; } }
            return false;
        }

        /// <summary>S6 (the polling days, DERIVED): the country's next chamber polling day on or after <paramref name="onOrAfter"/> that no record of it is dated -
        /// the extra election its procedure owes where it falls first (<see cref="TryExtraElection"/>, K161: <paramref name="extra"/>), else the ordinary day
        /// (<see cref="TryNextOrdinaryDay"/>), asked again from the day after where a count already holds the day. False for a country whose chamber the game
        /// does not count as an AI country's.</summary>
        public static bool TryNextChamberDay(Country country, DateTime onOrAfter, out DateTime day, out bool extra)
        {
            day = DateTime.MinValue;
            extra = false;
            if (country == null || !CountsChamber(country.Id)) { return false; }
            DateTime from = onOrAfter.Date, ordinary = DateTime.MinValue;
            bool found = false;
            for (int asked = 0; asked < 4 && !found; asked++)
            {
                if (!TryNextOrdinaryDay(country, from, out ordinary)) { break; }
                if (HeldOn(country, ordinary)) { from = ordinary.AddDays(1); continue; }
                found = true;
            }
            if (TryExtraElection(country, out DateTime owed, out DateTime _, out string _) && owed >= onOrAfter.Date && !HeldOn(country, owed) && (!found || owed < ordinary))
            {
                day = owed;
                extra = true;
                return true;
            }
            if (!found) { return false; }
            day = ordinary;
            return true;
        }

        /// <summary>S6 (Design's UPCOMING state): the day a foreign election is listed from - its campaign's start, the game's standard campaign before its polling
        /// day (`CampaignCalendar.CampaignStart`, the convention the player's own campaign opens on).</summary>
        public static DateTime UpcomingFrom(DateTime pollingDay) => new CampaignCalendar(pollingDay).CampaignStart;

        private static bool Upcoming(DateTime day, DateTime today) => day != DateTime.MinValue && day.Date >= today.Date && today.Date >= UpcomingFrom(day);

        /// <summary>S6 (Design's UPCOMING state): every election a country other than the player's holds from its campaign's start (<see cref="UpcomingFrom"/>) to
        /// its polling day, soonest first - its chamber (<see cref="TryNextChamberDay"/>), the US House and, in a presidential year, the president, Poland's
        /// president. Nothing is counted: a chamber's rows are the chamber sitting now. France holds none (K162).</summary>
        public static List<Result> UpcomingAbroad(World world, CountryId player, DateTime today)
        {
            var results = new List<Result>();
            if (world == null) { return results; }
            foreach (Country c in world.Countries)
            {
                if (c.Id == player || SeatedByRecordAlone(c.Id)) { continue; }
                if (TryNextChamberDay(c, today, out DateTime chamberDay, out bool _) && Upcoming(chamberDay, today))
                {
                    results.Add(SittingChamber(c, chamberDay, ChamberWords(c.Id), ChamberWord(c.Id), FirstSittingAfter(c, chamberDay), c.Id == CountryId.Italy ? "the Camera's seats" : "the chamber's seats", true, today));
                }
                if (UsCongressElection.ElectsIn(c.Id) && WorldClock.TryNextPollingDay(c.Id, today, out DateTime usDay) && Upcoming(usDay, today) && !HeldUs(c, usDay))
                {
                    bool midterm = UsCongressElection.IsMidterm(usDay);
                    if (!midterm) { results.Add(new Result { Country = c.Id, Day = usDay.Date, Elected = "the President", Chamber = "President", Upcoming = true, Headline = "Polling day " + OnDay(usDay), Governs = GovernsLine(c, today) }); }
                    results.Add(SittingChamber(c, usDay, midterm ? "the House, at the midterm" : "the House", "House", UsCongressElection.ConvenesAfter(usDay), "the House's seats", false, today));
                }
                if (PresidentialElection.TryNextFirstVote(c, today, out DateTime firstVote, out DateTime _, out string _) && Upcoming(firstVote, today))
                {
                    results.Add(new Result { Country = c.Id, Day = firstVote.Date, Elected = "the President", Chamber = "President", Upcoming = true, Headline = "Polling day " + OnDay(firstVote), Governs = GovernsLine(c, today) });
                }
            }
            results.Sort((a, b) => a.Day != b.Day ? a.Day.CompareTo(b.Day) : string.CompareOrdinal(a.Key, b.Key));
            return results;
        }

        private static bool HeldUs(Country country, DateTime day)
        {
            if (country.UsCongressElections != null) { foreach (UsCongressElection.Contest k in country.UsCongressElections) { if (k.PollingDay.Date == day.Date) { return true; } } }
            if (country.UsPresidentialElections != null) { foreach (UsPresidentialElection.Contest p in country.UsPresidentialElections) { if (p.PollingDay.Date == day.Date) { return true; } } }
            return false;
        }

        /// <summary>S6: an upcoming chamber election as it reaches the player - the chamber sitting now (its lists by seat, held in <see cref="Row.SeatsBefore"/>,
        /// nothing counted), the polling day, the day the chamber it elects first sits, and who governs meanwhile.</summary>
        private static Result SittingChamber(Country country, DateTime day, string elected, string chamber, DateTime seated, string rowsAre, bool formation, DateTime today)
        {
            var result = new Result
            {
                Country = country.Id, Day = day.Date, Elected = elected, Chamber = chamber, Upcoming = true, Formation = formation, Seated = seated, RowsAre = rowsAre,
                Headline = "Polling day " + OnDay(day), Governs = GovernsLine(country, today),
            };
            if (country.ParliamentSeats != null)
            {
                foreach (KeyValuePair<string, int> kv in country.ParliamentSeats)
                {
                    result.TotalSeats += kv.Value;
                    if (kv.Value > 0) { result.Rows.Add(new Row { Key = kv.Key, Name = PartySystems.ShortName(country.Id, kv.Key), SeatsBefore = kv.Value }); }
                }
            }
            result.Rows.Sort((a, b) => b.SeatsBefore != a.SeatsBefore ? b.SeatsBefore.CompareTo(a.SeatsBefore) : string.CompareOrdinal(a.Key, b.Key));
            return result;
        }

        private static string OnDay(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

        /// <summary>A chamber count abroad: each list's seats against the chamber that sat before it (the country's previous count, else the chamber of record on the
        /// eve) and its share against the previous count's, else the record's share over the roster's total.</summary>
        public static Result ChamberResult(Country country, ElectionRecord counted, DateTime today)
        {
            var result = new Result
            {
                Country = country.Id, Day = counted.Date.Date, Elected = ChamberWords(country.Id), Governs = GovernsLine(country, today),
                Chamber = ChamberWord(country.Id), Formation = CountsChamber(country.Id), OfRecord = counted.NotHeldReason == RecordSeatedReason, Seated = FirstSittingAfter(country, counted.Date),
            };
            ElectionRecord before = null;
            foreach (ElectionRecord e in country.ElectionHistory) { if (NationalElection.CountsParliament(e.Method) && e.Date < counted.Date && (before == null || e.Date > before.Date)) { before = e; } }
            Dictionary<string, int> seatsBefore = before != null ? before.Seats : PartySystems.InitialSeats(country.Id, WorldClock.SeatedVintage(country.Id, counted.Date.AddDays(-1)));
            Dictionary<string, double> sharesBefore = before?.Shares;
            if (sharesBefore == null && PartySystems.TryHistory(country.Id, out double[] record, out double[] _, WorldClock.SeatedVintage(country.Id, counted.Date.AddDays(-1))) && record != null)
            {
                PoliticalParty[] roster = PartySystems.RealRoster(country.Id);
                double total = 0.0;
                foreach (double s in record) { total += s; }
                if (total > 0.0)
                {
                    sharesBefore = new Dictionary<string, double>();
                    for (int i = 0; i < roster.Length && i < record.Length; i++) { sharesBefore[roster[i].Abbrev] = record[i] / total; }
                }
            }
            foreach (KeyValuePair<string, int> kv in counted.Seats)
            {
                result.TotalSeats += kv.Value;
                int was = seatsBefore != null && seatsBefore.TryGetValue(kv.Key, out int n) ? n : 0;
                if (kv.Value == 0 && was == 0) { continue; }
                result.Rows.Add(new Row
                {
                    Key = kv.Key, Name = PartySystems.ShortName(country.Id, kv.Key), Seats = kv.Value, SeatsBefore = was,
                    Share = counted.Shares.TryGetValue(kv.Key, out double s) ? s : double.NaN,
                    ShareBefore = sharesBefore != null && sharesBefore.TryGetValue(kv.Key, out double b) ? b : double.NaN,
                });
            }
            result.Rows.Sort((a, b) => b.Seats != a.Seats ? b.Seats.CompareTo(a.Seats) : string.CompareOrdinal(a.Key, b.Key));
            result.Headline = result.Rows.Count > 0 ? result.Rows[0].Name + " largest with " + result.Rows[0].Seats.ToString(CultureInfo.InvariantCulture) + " of " + result.TotalSeats.ToString(CultureInfo.InvariantCulture) + " seats" : "No seats allocated";
            if (counted.Method == ElectionMethod.ItalyRosatellum)
            {
                // L5 (the s938 review): the rows are the Camera's; the Senato elected the same day is named in the headline by its largest group, as seated
                result.RowsAre = "the Camera's seats";
                string senatoLargest = null; int senatoSeats = 0, senatoTotal = 0;
                if (country.SecondChamberSeats != null) { foreach (KeyValuePair<string, int> kv in country.SecondChamberSeats) { senatoTotal += kv.Value; if (kv.Value > senatoSeats || (kv.Value == senatoSeats && senatoLargest != null && string.CompareOrdinal(kv.Key, senatoLargest) < 0)) { senatoLargest = kv.Key; senatoSeats = kv.Value; } } }
                result.Headline = "Camera: " + result.Headline + (senatoLargest != null && senatoSeats > 0 ? "; Senato: " + PartySystems.ShortName(country.Id, senatoLargest) + " largest with " + senatoSeats.ToString(CultureInfo.InvariantCulture) + " of " + senatoTotal.ToString(CultureInfo.InvariantCulture) : string.Empty);
                if (senatoLargest != null && senatoSeats > 0) { result.SenatoLargest = senatoLargest; result.SenatoSeats = senatoSeats; result.SenatoTotal = senatoTotal; }
            }
            return result;
        }

        private static Result CongressResult(Country country, UsCongressElection.Contest contest)
        {
            var result = new Result
            {
                Country = country.Id, Day = contest.PollingDay.Date, Elected = UsCongressElection.IsMidterm(contest.PollingDay) ? "the House, at the midterm" : "the House", RowsAre = "the House's seats",   // L4 (the s938 review): an AI USA seats no Senate (K242), so the card names the House alone
                Governs = "The new Congress is seated on " + OnDay(contest.Convenes), Chamber = "House", Seated = contest.Convenes,
            };
            foreach (KeyValuePair<string, int> kv in contest.HouseSeats(country.Id))
            {
                result.TotalSeats += kv.Value;
                if (kv.Value > 0) { result.Rows.Add(new Row { Key = kv.Key, Name = PartySystems.ShortName(country.Id, kv.Key), Seats = kv.Value, SeatsBefore = country.ParliamentSeats != null && country.ParliamentSeats.TryGetValue(kv.Key, out int n) ? n : 0 }); }
            }
            result.Rows.Sort((a, b) => b.Seats != a.Seats ? b.Seats.CompareTo(a.Seats) : string.CompareOrdinal(a.Key, b.Key));
            result.Headline = "House: " + string.Join(", ", result.Rows.ConvertAll(r => r.Name + " " + r.Seats.ToString(CultureInfo.InvariantCulture))) + " - the Senate class counted, no Senate seated abroad";
            return result;
        }

        /// <summary>The chamber a country's count elects, in its own words.</summary>
        public static string ChamberWords(CountryId id) =>
            id == CountryId.Sweden ? "the Riksdag" : id == CountryId.Germany ? "the Bundestag" : id == CountryId.Poland ? "the Sejm" : id == CountryId.Italy ? "the Camera and the Senato" : "the chamber";

        /// <summary>S6: the chamber a country's count elects, in one word for a caption - Italy's two chambers by their joint name (Design's "Parlamento").</summary>
        public static string ChamberWord(CountryId id) =>
            id == CountryId.Sweden ? "Riksdag" : id == CountryId.Germany ? "Bundestag" : id == CountryId.Poland ? "Sejm" : id == CountryId.Italy ? "Parlamento" : "chamber";

        /// <summary>K163: who governs the country today, in a sentence - the government in office, or, while its formation is owed, the outgoing one and the day the
        /// new chamber first sits.</summary>
        public static string GovernsLine(Country country, DateTime today)
        {
            GovernmentRecord g = country?.Government;
            if (g == null) { return "No government is in office"; }
            if (g.Kind == WorldClock.ExecutiveKind.Presidency) { return (g.Executive ?? "The president") + " is President"; }
            string cabinet = g.Cabinet.Count > 0 ? FormationProjection.Join(country.Id, g.Cabinet) : "no cabinet";
            string led = g.PmParty != null ? " led by " + PartySystems.ShortName(country.Id, g.PmParty) : string.Empty;
            if (CountsChamber(country.Id) && TryLatestCount(country, out ElectionRecord counted)
                && (g.FormedOn <= counted.Date || RefusedPresidentsCabinet(country, g, FirstSittingAfter(country, counted.Date))))   // K302: the Sejm's refused cabinet, a caretaker too
            {
                DateTime sitting = FirstSittingAfter(country, counted.Date);
                if (today.Date < sitting) { return cabinet + led + " governs until the new chamber first sits on " + OnDay(sitting) + ", when the government is formed"; }
                if (TryExtraElection(country, out DateTime extra, out DateTime _, out string _)) { return cabinet + led + " governs as caretaker; a new election on " + OnDay(extra); }
                if (g.Round != null && g.Round.Open) { return cabinet + led + " governs as caretaker while the chancellor's election goes on (Art. 63 Abs. 3-4)"; }
                if (g.Caretaker) { return cabinet + led + " governs on as caretaker - no government was formed"; }
            }
            return cabinet + led + " governs, from " + OnDay(g.FormedOn);
        }
    }
}
