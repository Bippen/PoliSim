using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-8 (`POLISIM_FEATURE_LIST.md` row PS-8; Elias's rulings K159-K163; §618's ruling 4: "every election on a country's calendar inside a run is simulated
    /// once that country's model exists"): <b>EVERY COUNTRY OTHER THAN THE PLAYER'S HOLDS ITS ELECTIONS ON ITS OWN CALENDAR, AS AN AI COUNTRY</b> - in every
    /// world the day loop steps (the played one, the shadow baseline's, the impact ledger's forks, the trajectory dump's, where no country is the player's).
    /// <list type="bullet">
    /// <item><description><b>The count</b> (K160): GATE-18's idle game - the player's own path with no campaign (`RunPlayerElectionNow`'s): the prediction on the
    /// polling day with the government's record over its term (§752), the record's eve-fitted factors on a polling day of record (`RecordFactors`), the count
    /// region by region with the declared noise keyed by the master seed, the country and the day (`ParliamentaryCount` - no counted stream is drawn), the
    /// allocation by the country's own law (`NationalElection.Run`), the new chamber seated, Italy's Senato with it, each party's capital carried. A pure function
    /// of the world's state: the day's readout is put back as found (`NationalElection.KeepRegionalReadout`), so the player's screens never read an AI count's
    /// regions.</description></item>
    /// <item><description><b>The formation</b>: on the new chamber's first sitting (`WorldCalendar.FirstSittingAfter`), the country's own procedure run whole by the
    /// code that projects the player's night (q20's one formation code): the Riksdag's Speaker's round, the Bundestag's first ballot (and past it Art. 63 Abs. 3-4 by the player's own round, H1), the Sejm's steps
    /// (Art. 154 and, K302, Art. 155), Italy's mandates - every party answering by its own rule (no player's party sits in an AI country) - and the government it
    /// forms INSTALLED (`GovernmentRecord.FromProjection`), unlike the player's night, which installs nothing. Where it forms none the outgoing government is
    /// discharged and governs on as caretaker, and the Riksdag's, the Sejm's and Italy's procedures order the extra election their statutes give
    /// (`WorldCalendar.TryExtraElection`, K161; the Sejm's shortened term, K302).</description></item>
    /// <item><description><b>The USA</b>: its record seated by date until the game elects (the House from the 3rd of January after a count, the president at the
    /// oath), and its presidential and midterm counts on 3 U.S.C. §21(1)'s and 2 U.S.C. §7's days by the player's own US model with no campaign (R-US5 (a): the
    /// count a pure function of the world). DECLARED: an AI USA seats no Senate (US-2's premise, kept - the Senate's seats change no AI economy, and the funding
    /// horizon an AI Senate would seed is the player's machinery); the Senate's choice of a Vice President reads none.</description></item>
    /// <item><description><b>Poland's president</b>: the two rounds on Art. 128's days (`PresidentialElection.HoldRounds`) on the prediction, as the player's
    /// Poland holds them.</description></item>
    /// <item><description><b>France</b> (K162): its chamber and government of record seated by date to the record's end (`WorldCalendar.FranceRecordEnds`), then
    /// held.</description></item>
    /// </list>
    /// Nothing here is saved beyond what the country already carries (its counts, its chambers, its government, its contests): the calendar's state is read
    /// off them (`WorldCalendar`). A day that throws logs an error naming the country and leaves that country as it stood - it never ends the day loop.
    /// </summary>
    public partial class SimulationManager
    {
        /// <summary>PS-8: each AI country's last chamber count as this world held it - the shares before the record's factors, what the count was handed, what it
        /// returned. Per world, never saved (a check's reading, as <see cref="LastPlayerCount"/> is).</summary>
        private readonly Dictionary<CountryId, ParliamentaryCount.Held> _calendarCounts = new Dictionary<CountryId, ParliamentaryCount.Held>();

        /// <summary>PS-8: each AI country's last formation as this world ran it - the procedure's projection, installed where it formed a government. Per world,
        /// never saved.</summary>
        private readonly Dictionary<CountryId, FormationProjection> _calendarFormations = new Dictionary<CountryId, FormationProjection>();

        /// <summary>PS-8: the AI country's last chamber count this world held, or null.</summary>
        public ParliamentaryCount.Held LastCalendarCount(CountryId id) => _calendarCounts.TryGetValue(id, out ParliamentaryCount.Held held) ? held : null;

        /// <summary>PS-8: the AI country's last formation this world ran, or null.</summary>
        public FormationProjection LastCalendarFormation(CountryId id) => _calendarFormations.TryGetValue(id, out FormationProjection p) ? p : null;

        /// <summary>PS-8: the day's elections abroad - every country but the player's, after the player's own record is seated and before the day's campaign.</summary>
        private void HoldWorldCalendar()
        {
            if (_world == null) { return; }
            foreach (Country country in _world.Countries)
            {
                if (PlayerCountryId.HasValue && PlayerCountryId.Value == country.Id) { continue; }
                try
                {
                    if (WorldCalendar.SeatedByRecordAlone(country.Id)) { SeatRecordAlone(country); continue; }
                    if (UsPresidentialElection.ElectsIn(country.Id)) { SeatUsRecordAsAi(country); HoldUsCountsAsAi(country); continue; }
                    if (TwoRoundElection.RuleOf(country.Id) != null) { HoldPresidentialRoundAsAi(country); }
                    // H1 (the s938 review): an AI Bundestag's chancellor election past its first ballot runs its days by the player's own round code - the fourteen
                    // days (Art. 63 Abs. 3) and the ballot the most votes win (Abs. 4), `AdvanceSpeakerRound`, every party answering by its own rule
                    if (country.Government?.Round != null && country.Government.Round.Open && country.Government.Round.Bundestag) { AdvanceSpeakerRound(country, country.Government); }
                    ReorderLapsedShortening(country);   // K302 (the review's L5): a shortened term's polling day already past is ordered again today
                    if (WorldCalendar.IsChamberPollingDay(country, CurrentDate, out bool extra)) { CountChamberAsAi(country, extra); }
                    if (WorldCalendar.FormationDue(country, CurrentDate, out ElectionRecord counted, out DateTime sitting)) { FormAsAi(country, counted, sitting); }
                }
                catch (Exception e)
                {
                    Debug.LogError($"CALENDAR: {country.Id} on {CurrentDate:yyyy-MM-dd} - the day's election abroad threw {e.GetType().Name}: {e.Message}; the country stands as it was\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// K302 (the review's L5): <b>A SHORTENED TERM WHOSE POLLING DAY HAS PASSED IS ORDERED AGAIN</b> - DECLARED, re-anchored on the day it is found by the same rule:
        /// the President shortens the term that day (the caretaker discharged anew, its `CaretakerSince` the day) and the elections fall on Art. 98 ust. 5's day
        /// from it (`WorldCalendar.TryExtraElection`). Reached only where a world holds an AI Polish caretaker whose owed day is behind its clock - a save cut by a
        /// build before K302, whose failed Sejm left the caretaker serving on with no election, or a day whose count predicted nothing. Never in a continuous run of this build: the extra count is held on its own day, after which nothing is owed. Run on every AI
        /// country's day before its count, and on a load (`SaveGameService.RestoreInto`, <see cref="ReorderLapsedShortenings"/>), so no screen reads a past day.
        /// </summary>
        private void ReorderLapsedShortening(Country country)
        {
            if (country?.Government == null || (PlayerCountryId.HasValue && PlayerCountryId.Value == country.Id)) { return; }
            if (ConfidenceProcedure.RulesOf(country.Id) != ConfidenceProcedure.Rules.Sejm) { return; }
            if (!WorldCalendar.TryExtraElection(country, out DateTime owed, out DateTime orderedOn, out string _) || owed >= CurrentDate.Date) { return; }
            country.Government.Discharge(CurrentDate);
            country.Government.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the shortened term's elections, owed on {owed:yyyy-MM-dd} from the order of {orderedOn:yyyy-MM-dd}, were never held - the term is shortened again today and the elections ordered for {SejmFormation.ShortenedTermElectionDay(CurrentDate):yyyy-MM-dd} (Art. 155 ust. 2, Art. 98 ust. 5; DECLARED, K302)");
            if (!IsCounterfactual) { Debug.Log($"CALENDAR: {country.Id} - the shortened term's polling day {owed:yyyy-MM-dd} had passed unheld; ordered again on {CurrentDate:yyyy-MM-dd}, the elections on {SejmFormation.ShortenedTermElectionDay(CurrentDate):yyyy-MM-dd}"); }
        }

        /// <summary>K302 (the review's L5): <see cref="ReorderLapsedShortening"/> for every AI country - the load's call, after the world and the clock are restored.</summary>
        internal void ReorderLapsedShortenings()
        {
            if (_world == null) { return; }
            foreach (Country country in _world.Countries) { ReorderLapsedShortening(country); }
        }

        /// <summary>PS-8 (K160): an AI country's chamber count on its polling day - the idle game's arithmetic (see the class), its chamber seated with it.</summary>
        private void CountChamberAsAi(Country country, bool extra)
        {
            CountryId id = country.Id;
            Action putBack = NationalElection.KeepRegionalReadout();
            try
            {
                if (!NationalElection.TryPredictShares(id, out Dictionary<string, double> shares, EconomicVote.RecordOverTerm(country, CurrentDate, out _), on: CurrentDate, played: country))
                {
                    // the measurement's (1): a polling day of record the vote model cannot predict (Sweden's 11 Sep 2022 from Italy's start) seats the record by date, as
                    // the row asks where a country's model does not exist - the election's own lists, its shares of record; the formation runs on its first sitting
                    ElectionVintage ofRecord = WorldClock.VintageOfElection(id, CurrentDate);
                    if (ofRecord != ElectionVintage.Seated && WorldClock.ElectionDayOf(id, ofRecord) == CurrentDate.Date)
                    {
                        ElectionRecord seated = WorldCalendar.RecordSeatedOn(id, CurrentTurn, CurrentDate, ofRecord);
                        country.ElectionHistory.Add(seated);
                        ParliamentSystem.SetSeatsFromElection(country, seated.Seats);
                        PartyCapital.CarryOver(country.PartyCapital, seated.Seats);
                        if (!IsCounterfactual) { Debug.LogWarning($"CALENDAR: {id} - no prediction could be made on its polling day {CurrentDate:yyyy-MM-dd}; the chamber of record ({ofRecord}) is seated by date in its place, its own lists, and its government is formed on its first sitting"); }
                        return;
                    }
                    country.ElectionHistory.Add(new ElectionRecord { Turn = CurrentTurn, Date = CurrentDate, CountryId = id.ToString(), Method = ElectionMethod.NotImplemented, NotHeldReason = NationalElection.NotHeldReason(id) });
                    if (!IsCounterfactual) { Debug.LogWarning($"CALENDAR: {id} - no prediction could be made on its polling day {CurrentDate:yyyy-MM-dd}; nothing is counted, and its chamber stays"); }
                    return;
                }
                Dictionary<string, double> beforeFactors = shares;
                if (RecordFactors.TryFor(id, CurrentDate, out ElectionVintage factoredElection, out Dictionary<string, double> factors))
                {
                    shares = RecordFactors.Apply(RecordFactors.Based(factoredElection, beforeFactors), factors);   // K109: a list handed nothing on its share of record
                }
                ParliamentaryCount.Result counted = ParliamentaryCount.Count(id, CurrentDate, shares, null, null, writeReadout: false);
                _calendarCounts[id] = new ParliamentaryCount.Held { Day = CurrentDate, BeforeFactors = beforeFactors, Handed = shares, Factors = factors, Election = factoredElection, Counted = counted };
                if (counted != null) { shares = counted.Shares; }
                ElectionRecord record = NationalElection.Run(id, CurrentTurn, shares, CurrentDate, factors != null ? RecordFactors.RosterShareOfVote(factoredElection) : 1.0,
                    counted, factors != null ? RecordFactors.RosterShareByRegion(factoredElection, counted?.RegionNames) : null, logFallback: !IsCounterfactual);
                country.ElectionHistory.Add(record);
                if (NationalElection.CountsParliament(record.Method))
                {
                    ParliamentSystem.SetSeatsFromElection(country, record.Seats);
                    if (record.Method == ElectionMethod.ItalyRosatellum)
                    {
                        // PS-7 (K153, K154), as the player's count: the Senato elected the same day on the same vote, the senators for life beside it
                        ParliamentSystem.SetSecondChamberFromElection(country, ItalianCount.SenatoSeated(record.Shares, WorldClock.ItalyFirstSittingAfter(CurrentDate), factors != null && factoredElection == ElectionVintage.Italy2022 ? ItalianCount.SenatoRosterShare2022 : 1.0));
                    }
                    PartyCapital.CarryOver(country.PartyCapital, record.Seats);
                }
                if (!IsCounterfactual)
                {
                    var seats = new List<string>();
                    foreach (KeyValuePair<string, int> kv in record.Seats) { if (kv.Value > 0) { seats.Add(kv.Key + " " + kv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)); } }
                    Debug.Log($"CALENDAR: {id} held {(extra ? "its extra election" : "its election")} on {CurrentDate:yyyy-MM-dd} as an AI country by {record.Method}{(factors != null ? " (the record's factors, " + factoredElection + ")" : string.Empty)} - {string.Join(", ", seats)}; the new chamber first sits on {WorldCalendar.FirstSittingAfter(country, CurrentDate):yyyy-MM-dd}");
                }
            }
            finally { putBack(); }
        }

        /// <summary>PS-8: an AI country's formation on its new chamber's first sitting - its procedure's projection, installed where it forms a government; else the
        /// outgoing government discharged, and the statute's extra election where the procedure orders one (K161).</summary>
        private void FormAsAi(Country country, ElectionRecord counted, DateTime sitting)
        {
            CountryId id = country.Id;
            if (country.Government?.Round != null && country.Government.Round.Open)
            {
                // an election held while a round is open ends it, as the player's round ends (OpenRoundAfterElection)
                country.Government.Round.Stage = RoundStage.Concluded;
                country.Government.Round.Log.Add($"{CurrentDate:yyyy-MM-dd}: the election of {counted.Date:yyyy-MM-dd} ends the round");
            }
            FormationProjection projection;
            switch (ConfidenceProcedure.RulesOf(id))
            {
                case ConfidenceProcedure.Rules.Riksdag:
                case ConfidenceProcedure.Rules.Bundestag: projection = ProjectRound(country, counted.Date.Date); break;
                case ConfidenceProcedure.Rules.Sejm: projection = ProjectSejm(country, counted.Date.Date); break;
                case ConfidenceProcedure.Rules.Parlamento: projection = ProjectMandates(country, counted.Date.Date); break;
                default: projection = null; break;
            }
            if (projection != null) { _calendarFormations[id] = projection; }
            if (projection != null && projection.Expected && projection.Cabinet.Count > 0)
            {
                string basis = $"{projection.Procedure} after the election of {counted.Date:yyyy-MM-dd}, run whole on the new chamber's first sitting as an AI country - {projection.Step}";
                HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);   // K59 - a no-op in an AI country (no player's chairs), kept so every install is bracketed
                country.Government = GovernmentRecord.FromProjection(country, projection, CurrentDate, basis, _world);
                CabinetSystem.SeatOnEntry(country, chairsBefore);
                if (!IsCounterfactual) { Debug.Log($"CALENDAR: {id} - installed {string.Join("+", country.Government.Cabinet)} led by {country.Government.PmParty}{(country.Government.Support.Count > 0 ? " with " + string.Join("+", country.Government.Support) : string.Empty)} on {CurrentDate:yyyy-MM-dd} by {projection.Procedure} ({projection.Step}); the Finance partner: {FinancePartner.Holder(country) ?? "none - Finance is held by the head's own party or group, so no partner's stance moves the book (FinancePartner.Holder)"}"); }
                return;
            }
            if (projection != null && projection.Partial && ConfidenceProcedure.RulesOf(id) == ConfidenceProcedure.Rules.Bundestag && country.Government != null)
            {
                ContinueChancellorElection(country, counted, projection);
                return;
            }
            GovernmentRecord outgoing = country.Government;
            string reason = projection?.Reason ?? "no procedure forms this country's government";
            if (outgoing != null && projection != null && projection.RefusedCabinet.Count > 0 && !(outgoing.PmParty == projection.RefusedPmParty && SameKeys(outgoing.Cabinet, projection.RefusedCabinet)))
            {
                // K302 (the review's L6): Art. 162 ust. 2 pkt 1 and ust. 3 - the cabinet the President appointed under Art. 155 ust. 1 and the Sejm refused resigns and
                // carries on until the new Sejm's cabinet, so it is the caretaker: installed today and discharged today, the steps run whole on the first sitting (the
                // player's steps install it on the President's day and discharge it on the vote's). Where the President re-appoints the outgoing cabinet itself, that
                // record carries on, as the player's `Reappoint` keeps it (the branch below)
                GovernmentRecord refused = GovernmentRecord.OfNamedCabinet(projection.RefusedPmParty, projection.RefusedCabinet, projection.RefusedSupport, CurrentDate,
                    $"appointed by the President after the election of {counted.Date:yyyy-MM-dd} and refused the Sejm's confidence (Konstytucja Art. 155 ust. 1) - it resigned and carries on (Art. 162 ust. 2 pkt 1, ust. 3)");
                refused.Outcome = (SejmFormation.Carries(country, projection.RefusedCabinet, null, out _, out _) ? CoalitionOutcomeKind.MajorityCoalition : CoalitionOutcomeKind.MinorityGovernment).ToString();
                refused.Kind = outgoing.Kind;
                refused.Executive = outgoing.Executive;
                refused.AllocatePortfolios(country);
                refused.FormAgreements(country, CurrentDate, _world);
                HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);   // K59 - a no-op in an AI country, kept so every install is bracketed
                country.Government = refused;
                CabinetSystem.SeatOnEntry(country, chairsBefore);
                refused.Discharge(CurrentDate);
                refused.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: {WorldCalendar.FailureMarker}{counted.Date:yyyy-MM-dd} - {reason}; the President's cabinet governs on as caretaker");
            }
            else if (outgoing != null)
            {
                outgoing.Discharge(CurrentDate);   // the record's version moves (ChamberVerdicts), CaretakerSince set - the procedure's end, read by WorldCalendar
                outgoing.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: {WorldCalendar.FailureMarker}{counted.Date:yyyy-MM-dd} - {reason}; the outgoing government governs on as caretaker");   // read back by WorldCalendar.FormationFailure (M1)
            }
            if (!IsCounterfactual)
            {
                Debug.Log(WorldCalendar.TryExtraElection(country, out DateTime extraDay, out DateTime _, out string why)
                    ? $"CALENDAR: {id} - no government formed on {CurrentDate:yyyy-MM-dd} ({reason}); {why}: the extra election on {extraDay:yyyy-MM-dd}"
                    : $"CALENDAR: {id} - no government formed on {CurrentDate:yyyy-MM-dd} ({reason}); the outgoing government governs on as caretaker");
            }
        }

        /// <summary>
        /// H1 (the s938 review): AN AI BUNDESTAG WHOSE FIRST BALLOT ELECTS NO CHANCELLOR GOES ON BY ART. 63 ABS. 3-4, through the player's own round - its first ballot taken by
        /// the player's own `PersonBallot` (the division recorded; not elected, phase 2 opens, the fourteen days from the ballot's day, `SpeakerRound.BundestagSecondPhaseDays`,
        /// the next nomination asked - or, elected after all, installed); then each day `AdvanceSpeakerRound` - a nomination standing, drafted, tabled and
        /// balloted (`PersonBallot`), and when the fourteen days pass, the ballot the most votes win (`PluralityBallot`), the Bundespräsident appointing (the
        /// premise §713 states) - installing through `Install`. The new Bundestag has convened (this is its first sitting), so the outgoing government's office
        /// ends today and it serves on until a chancellor is elected (Art. 69 Abs. 2-3 GG), as the round's own convening discharges the player's. DECLARED: the
        /// round runs from the first sitting, not the player's §714 formation time (the record's median) - the AI country's formation is run on its first sitting.
        /// </summary>
        private void ContinueChancellorElection(Country country, ElectionRecord counted, FormationProjection firstBallot)
        {
            GovernmentRecord g = country.Government;
            var round = new SpeakerRound
            {
                OpenedOn = counted.Date.Date.AddDays(1), Occasion = "after the election of " + counted.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                Vintage = WorldClock.VintageOfElection(country.Id, counted.Date), Stage = RoundStage.Consulting, ReadsOn = counted.Date.Date, Convenes = CurrentDate.Date, Phase = 1,
            };
            round.Order = SpeakerOrder(country, RoundReading(country, round), RoundLines(country, round));
            round.Turn = 0;
            round.Asked = firstBallot.FirstAsked;
            foreach (string step in firstBallot.Steps) { round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {step}"); }
            g.Discharge(CurrentDate);
            g.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the new Bundestag convenes - the government's office ends (Art. 69 Abs. 2 GG); it serves on until a chancellor is elected (Abs. 3)");
            g.Round = round;
            // L-N4 (the s938 review): the first ballot itself is the player's round's own (PersonBallot, phase 1) - recorded as a division, and, not elected, it opens
            // the fourteen days and asks the next nomination exactly as the player's round does; the projection only said the ballot would not carry
            FormationProposal proposal = DraftProposal(country, round, round.Asked);
            round.Proposal = proposal;
            round.Stage = RoundStage.VotePending;
            round.VoteOn = CurrentDate.Date;
            if (!IsCounterfactual) { Debug.Log($"CALENDAR: {country.Id} - the new Bundestag sits on {CurrentDate:yyyy-MM-dd} and its first ballot is projected not to carry ({firstBallot.Reason}); the chancellor's election runs by the round's own code - the first ballot, the fourteen days, then the ballot the most votes win (Art. 63 Abs. 2-4 GG)"); }
            PersonBallot(country, g, round);
        }

        /// <summary>PS-8: Poland's presidential rounds in an AI Poland - the player's own call (`HoldPresidentialRound`) on the prediction, no campaign.</summary>
        private void HoldPresidentialRoundAsAi(Country country)
        {
            if (!PresidentialElection.IsRoundDay(country, CurrentDate, out bool runOff)) { return; }
            Action putBack = NationalElection.KeepRegionalReadout();
            try
            {
                Dictionary<string, double> predicted = null;
                if (!runOff && !NationalElection.TryPredictShares(country.Id, out predicted, EconomicVote.RecordOverTerm(country, CurrentDate, out _), on: CurrentDate, played: country)) { predicted = null; }
                PresidentialElection.Contest held = PresidentialElection.HoldRounds(country, CurrentDate, predicted);
                if (held != null && !IsCounterfactual) { Debug.Log($"CALENDAR: {country.Id} - PRESIDENTIAL as an AI country: {held.Line}"); }
            }
            finally { putBack(); }
        }

        /// <summary>PS-8: an AI USA's counts on its polling days - the House's choice where an earlier count left no majority, a midterm's Congress, or the president
        /// with the House and the Senate class, each by the player's own US model with no campaign (the polling day's states with the declared keyed noise alone).</summary>
        private void HoldUsCountsAsAi(Country country)
        {
            Action putBack = NationalElection.KeepRegionalReadout();
            try
            {
                UsPresidentialElection.Contest chosen = UsPresidentialElection.HoldHouseChoice(country, CurrentDate, out string choice);
                if (chosen != null && !IsCounterfactual) { Debug.Log($"CALENDAR: {country.Id} - the House's choice as an AI country: {choice}"); }
                if (UsCongressElection.IsMidterm(CurrentDate)) { HoldUsMidtermCount(country, quiet: IsCounterfactual); return; }
                if (!UsPresidentialElection.IsPollingDay(country, CurrentDate)) { return; }
                if (!NationalElection.TryPredictShares(country.Id, out Dictionary<string, double> poll, EconomicVote.RecordOverTerm(country, CurrentDate, out _), on: CurrentDate, played: country))
                {
                    if (!IsCounterfactual) { Debug.LogWarning($"CALENDAR: {country.Id} - no prediction could be made on the polling day {CurrentDate:yyyy-MM-dd}; nothing is counted"); }
                    return;
                }
                UsGroundGame.Campaign states = ParliamentaryCount.SigmaPp > 0.0 ? UsGroundGame.NoCampaign(CurrentDate, out string _) : null;
                UsPresidentialElection.Contest held = UsPresidentialElection.Hold(country, CurrentDate, poll, states, out string why);
                if (held == null)
                {
                    if (!IsCounterfactual) { Debug.LogWarning($"CALENDAR: {country.Id} - no presidential count on {CurrentDate:yyyy-MM-dd}: {why}"); }
                    return;
                }
                UsCongressElection.Contest congress = UsCongressElection.Hold(country, CurrentDate, UsPresidentialElection.CountedPoll(country, CurrentDate, poll), held, out string congressWhy);
                if (!IsCounterfactual) { Debug.Log($"CALENDAR: {country.Id} - PRESIDENTIAL as an AI country: {held.Line}" + (congress != null ? "; CONGRESS: " + congress.Line : "; no Congress count: " + congressWhy)); }
            }
            finally { putBack(); }
        }

        /// <summary>PS-8: an AI USA's record seated by date until the game elects - the player's rule (<see cref="SeatTheRecordOnItsDate"/>) for the House and the
        /// executive, the government changed in place of the player's arrival budget and chairs (an AI country has neither).</summary>
        private void SeatUsRecordAsAi(Country country)
        {
            CountryId id = country.Id;
            if (UsCongressElection.TryInOffice(country, CurrentDate, out UsCongressElection.Contest congress))
            {
                Dictionary<string, int> won = congress.HouseSeats(id);
                if (!SameSeats(country.ParliamentSeats, won))
                {
                    ParliamentSystem.SetSeatsFromElection(country, won);
                    PartyCapital.CarryOver(country.PartyCapital, country.ParliamentSeats);
                    if (!IsCounterfactual) { Debug.Log($"CALENDAR: {id} - the House the game elected on {congress.PollingDay:yyyy-MM-dd} seated on {CurrentDate:yyyy-MM-dd}: REP {congress.HouseRep}, DEM {congress.HouseDem}"); }
                }
            }
            else if (WorldClock.RecordSeatsChamber(id))
            {
                ElectionVintage seated = WorldClock.SeatedVintage(id, CurrentDate);
                if (!SameSeats(country.ParliamentSeats, PartySystems.InitialSeats(id, seated)))
                {
                    ParliamentSystem.SeatChamberOfRecord(country, seated);
                    PartyCapital.CarryOver(country.PartyCapital, country.ParliamentSeats);
                    if (!IsCounterfactual) { Debug.Log($"CALENDAR: {id} - the chamber of record seated on {CurrentDate:yyyy-MM-dd}: {WorldClock.RecordStanding(id, CurrentDate)}"); }
                }
            }
            GovernmentRecord next = null;
            if (UsPresidentialElection.TryActingOn(country, CurrentDate, out UsPresidentialElection.Contest undecided))
            {
                if (!UsPresidentialElection.ActingGoverns(country.Government, undecided)) { next = UsPresidentialElection.ActingGovernmentOf(country, undecided, CurrentDate); }
            }
            else if (UsPresidentialElection.TryInOffice(country, CurrentDate, out UsPresidentialElection.Contest sworn))
            {
                if (!UsPresidentialElection.Governs(country.Government, sworn)) { next = UsPresidentialElection.GovernmentOf(sworn, CurrentDate); }
            }
            else if (WorldClock.RecordSeatsExecutive(country, CurrentDate) && WorldClock.TryGovernmentAt(id, CurrentDate, out WorldClock.GovernmentOfRecord g) && country.Government?.Executive != g.President)
            {
                next = GovernmentRecord.AtStart(country, CurrentDate, _world);
            }
            if (next == null) { return; }
            HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);   // K59 - a no-op in an AI country (no player's chairs), kept so every install is bracketed
            country.Government = next;
            CabinetSystem.SeatOnEntry(country, chairsBefore);
            if (!IsCounterfactual) { Debug.Log($"CALENDAR: {id} - {next.Executive} takes office on {CurrentDate:yyyy-MM-dd} as an AI country ({next.Outcome})"); }
        }

        /// <summary>PS-8 (K162): France's chamber and government of record by date, to the record's end and then held - the latest chamber of record convened by
        /// the day (the dissolved one stays in the seat table until its successor sits), and each government of record from its first day (its cabinet unsourced,
        /// so the formation's stand-in on the seated chamber, as `GovernmentRecord.AtStart` seats it).</summary>
        private void SeatRecordAlone(Country country)
        {
            if (CurrentDate.Date > WorldCalendar.FranceRecordEnds) { return; }
            CountryId id = country.Id;
            WorldClock.ChamberOfRecord? latest = null;
            foreach (WorldClock.ChamberOfRecord c in WorldClock.Chambers(id)) { if (c.Convened <= CurrentDate.Date && (latest == null || c.Convened > latest.Value.Convened)) { latest = c; } }
            if (latest.HasValue && !SameSeats(country.ParliamentSeats, PartySystems.InitialSeats(id, latest.Value.Vintage)))
            {
                ParliamentSystem.SeatChamberOfRecord(country, latest.Value.Vintage);
                PartyCapital.CarryOver(country.PartyCapital, country.ParliamentSeats);
                if (!IsCounterfactual) { Debug.Log($"CALENDAR: {id} - the chamber of record seated on {CurrentDate:yyyy-MM-dd}: {latest.Value.Vintage} (K162: seated by date to {WorldCalendar.FranceRecordEnds:yyyy-MM-dd}, then held)"); }
            }
            if (WorldClock.TryGovernmentAt(id, CurrentDate, out WorldClock.GovernmentOfRecord g) && g.From == CurrentDate.Date)
            {
                HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);   // K59 - a no-op in an AI country (no player's chairs), kept so every install is bracketed
                country.Government = GovernmentRecord.AtStart(country, CurrentDate, _world);
                CabinetSystem.SeatOnEntry(country, chairsBefore);
                if (!IsCounterfactual) { Debug.Log($"CALENDAR: {id} - the government of record from {CurrentDate:yyyy-MM-dd}: {g.Head} (K162)"); }
            }
        }
    }
}
