using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// FORK-2 (D-45's remainder; `COMPLETED.md` §871): THE PLAYER'S ELECTION OF THE CHAMBER, on the world's side - the count of the player's country's polling
    /// day and what the count seats, moved here from the controller's election call (`GameController.CheckElection`) so that every world that steps the
    /// player's day holds it: the played world through the controller, and the impact ledger's worlds and the no-policy world on their own polling day
    /// (`ShadowBaseline`), each counting its own electorate. What stays the controller's is what a person reads - the verdict's sentence, election night,
    /// the end of a run.
    /// </summary>
    public partial class SimulationManager
    {
        /// <summary>GATE-18 piece two: the player's last chamber count as this world held it - what it was handed before and after the record's factors, what
        /// it read and what it returned; null before this world has counted one. Per world, never saved (a check's reading, not the game's state).</summary>
        public ParliamentaryCount.Held LastPlayerCount { get; private set; }

        /// <summary>FORK-2: the player's country's election of the chamber held today - <see cref="RunPlayerElection"/>, then <see cref="SeatPlayerElection"/>
        /// - as a world with no controller holds it (a counterfactual's day, a check's). Nothing where today is not the player's polling day.</summary>
        public void HoldPlayerElection()
        {
            if (!PollingDayToday || !PlayerCountryId.HasValue) { return; }
            // FORK-2 (the review's M1): once a day - a world whose polling-day flag was restored with it (a fork made on the day) has its day's election in its record already
            List<ElectionRecord> held = _world?.GetCountry(PlayerCountryId.Value)?.ElectionHistory;
            if (held != null && held.Count > 0 && held[held.Count - 1].Date == CurrentDate) { return; }
            ElectionRecord record = RunPlayerElection();
            if (record != null) { SeatPlayerElection(record, out _, out _, out _); }
        }

        /// <summary>FORK-2 (the review's M1): <see cref="PollingDayToday"/> as the day stands - a world restored from a snapshot (a fork, a carried world) has no
        /// flag of its own until its next day, and a fork made on the polling day before the boundary crosses the boundary with no day run.</summary>
        public void RecomputePollingDayToday() => PollingDayToday = TryPlayerPollingDay(out System.DateTime pollingDay) && pollingDay == CurrentDate;

        /// <summary>
        /// FORK-2 (the review's H2; DECLARED, put to Elias): A COUNTERFACTUAL WORLD ANSWERS A WAIT ON THE PLAYER THAT NO REPLAYED ANSWER COVERS BY THE AI'S RULE
        /// FOR THE PLAYER'S PARTY, as it answers for an AI-run party - always in the no-policy world (handed no answer), and in an except-world once a replay
        /// found no match there (`ShadowBaseline`). Asked to form a government, the party consults its days and tables the government it would lead, as an asked
        /// party does (<see cref="DraftProposal"/>, the round's own consultation); offered a place, it answers as the formation answers for any party
        /// (`Formateur.Answer` with no player's party) - accepting where every invited party accepts (DECLARED, the fix review's L1: an AI-run party is never
        /// offered a place, so this is the nearest rule, not its own). A meeting is set aside, and so is a cabinet decision (the fix review's L4) - no AI-run
        /// country holds either: both roll for a player's portfolio alone. True where it answered. Never the played world's: a world with no player answers
        /// nothing here.
        /// </summary>
        public bool AnswerWaitsAsTheAi(CountryId countryId)
        {
            if (!IsCounterfactual || !PlayerCountryId.HasValue || PlayerCountryId.Value != countryId) { return false; }
            bool answered = SetAsideForeignPolicyMeeting(countryId);
            if (_pendingCabinetDecisionsByCountry.TryGetValue(countryId, out List<(CabinetPortfolio Portfolio, CabinetDecision Decision)> decisions) && decisions.Count > 0) { decisions.Clear(); answered = true; }
            Country country = _world?.GetCountry(countryId);
            if (AnswerDebtLimitAsTheAi(country)) { answered = true; }   // PS-6 K23 with K49: a binding debt limit with no raise before Congress - the AI's rule tables it
            SpeakerRound round = RoundOf(countryId);
            if (country == null || round == null) { return answered; }
            if (round.Stage == RoundStage.PlayerAsked)
            {
                if (CurrentDate < round.AskedOn.AddDays(SpeakerRound.ConsultationDays)) { return answered; }   // the consultation an asked party takes
                FormationProposal proposal = DraftProposal(country, round, round.Asked);
                round.Log.Add($"{CurrentDate:yyyy-MM-dd}: {round.Asked} proposes as the formation drafts it (no player answers in this world)");
                TableAndVote(country, round, proposal);
                return true;
            }
            if (round.Stage == RoundStage.OfferToPlayer && round.Proposal != null)
            {
                ProposalVerdict verdict = Formateur.Answer(country, round.Proposal, CurrentDate, _world, RoundReading(country, round), RoundLines(country, round), null);
                return AnswerOfferNow(countryId, verdict != null && verdict.AllAccept, out _) || answered;
            }
            return answered;
        }

        /// <summary>
        /// W-G1 (moved by FORK-2 from `GameController.RunNationalElection`, its words kept): hold the player country's election on its own electoral system, set
        /// the chamber from the result, and keep the record - which it returns (null where there is no player's country to hold it in). A country with no live
        /// path records the reason and leaves the chamber exactly as it was. FORK-2 (the review's L1): a counterfactual's count leaves the process's regional
        /// readout as it found it (`NationalElection.KeepRegionalReadout`) - the readout is the played world's.
        /// </summary>
        public ElectionRecord RunPlayerElection()
        {
            if (!IsCounterfactual) { return RunPlayerElectionNow(); }
            System.Action putBack = NationalElection.KeepRegionalReadout();
            try { return RunPlayerElectionNow(); }
            finally { putBack(); }
        }

        /// <summary>FORK-2: <see cref="RunPlayerElection"/>'s count.</summary>
        private ElectionRecord RunPlayerElectionNow()
        {
            if (!PlayerCountryId.HasValue) { return null; }
            CountryId id = PlayerCountryId.Value;
            Country country = _world?.GetCountry(id);
            if (country == null) { return null; }

            // PS-6 US-8 (R-US1 (a), RULED - F8), and PS-6 US-16: the USA's polling day elects the president AND the House and the Senate seats up on the day loop
            // (HoldUsPresidentialCount - UsPresidentialElection.Hold, then UsCongressElection.Hold), seated on 3 January - so THIS path, the parliamentary
            // chamber's, counts nothing: it records the day with no seats (the record's readers treat a NotImplemented record as no parliamentary election, which
            // keeps the formation's rounds and the reference row off the USA - DECLARED) and its reason says where the chamber was elected
            // PS-6 K74 (Elias's ruling of 2026-10-23: "a US election method is added to the chamber record"): where the day loop counted the House today the record
            // carries the method (UsCongress) and the House it elected, by party; no shares (the House vote is the contest's V); every parliamentary reader reads
            // NationalElection.CountsParliament, so no formation, round, night or reference row reads it. A day whose count failed keeps the NotImplemented record.
            if (WorldClock.RecordSeatsChamber(id))
            {
                string reason = NationalElection.NotHeldReasonOn(id, CurrentDate);   // PS-6 US-27: a midterm's own words
                UsCongressElection.Contest countedToday = country.UsCongressElections?.Find(c => c.PollingDay.Date == CurrentDate.Date);
                var notCounted = new ElectionRecord
                {
                    Turn = CurrentTurn,
                    Date = CurrentDate,
                    CountryId = id.ToString(),
                    Method = countedToday != null ? ElectionMethod.UsCongress : ElectionMethod.NotImplemented,
                    NotHeldReason = reason,
                };
                if (countedToday != null) { foreach (KeyValuePair<string, int> kv in countedToday.HouseSeats(id)) { notCounted.Seats[kv.Key] = kv.Value; } }
                country.ElectionHistory.Add(notCounted);
                if (!IsCounterfactual)
                {
                    Debug.Log(country.UsCongressElections != null && country.UsCongressElections.Exists(c => c.PollingDay.Date == CurrentDate.Date)   // PS-6 US-27: a count of TODAY, not any count held before
                        ? $"ELECTION: {id} on {CurrentDate:yyyy-MM-dd} - the House and the Senate seats up were elected on the day loop " + (UsCongressElection.IsMidterm(CurrentDate) ? "at the midterm, with no presidential count" : "beside the president")   // PS-6 US-27
                          + " (the CONGRESS line), seated on 3 January; this path records the day without seats"
                        : $"ELECTION: the chamber is not elected for {id} on {CurrentDate:yyyy-MM-dd} - {reason}");
                }
                return notCounted;
            }

            // C-R4b step 5 (D-21): when the player's campaign ran up to THIS boundary, election night
            // counts the campaign's own shares - the same preference model over the same compatibility,
            // prior and loyalty the prediction uses, moved by eight weeks of campaigning - and the
            // regional breakdown is derived from them the same way. No campaign (another country, or
            // no staging): the prediction, as before.
            Dictionary<string, double> shareByParty = null;
            CampaignRun.Result campaign = PlayerCampaignResult;
            if (campaign != null && CampaignRecord != null
                && CampaignRecord.ElectionDate == CurrentDate   // PS-2 (§619): the campaign that ran up to TODAY, the polling day
                && PlayerCampaign != null)
            {
                var keys = new string[PlayerCampaign.Setup.Parties.Length];
                for (int i = 0; i < keys.Length; i++) { keys[i] = PlayerCampaign.Setup.Parties[i].Name; }
                // D-10 (a): the tactical layer (§23) over the campaign's shares, believing the last
                // PUBLISHED tracker - the poll the electorate saw - about who clears the threshold.
                shareByParty = NationalElection.SharesFromCampaign(id, keys, campaign.FinalShares,
                    PlayerCampaign.PublicPoll, out TacticalResult tactical, on: CurrentDate);   // §707: the Länder derived on the polling day
                if (!IsCounterfactual)
                {
                    Debug.Log($"ELECTION: counted the campaign's shares for {id} - " + string.Join(" ", System.Array.ConvertAll(campaign.FinalShares, v => v.ToString("P1", System.Globalization.CultureInfo.InvariantCulture))));
                    if (tactical != null)
                    {
                        var flows = new System.Text.StringBuilder();
                        foreach (TacticalFlow f in tactical.Flows)
                        {
                            if (f.Share < 5e-5) { continue; }
                            flows.Append(keys[f.From]).Append(f.Rescue ? " lends " : " abandons to ").Append(keys[f.To]).Append(' ')
                                .Append((f.Share * 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append(" pp; ");
                        }
                        Debug.Log($"ELECTION: tactical layer (D-10 (a)) on the last tracker - " + (flows.Length > 0 ? flows.ToString() : "no flows") + " -> counted "
                            + string.Join(" ", System.Array.ConvertAll(tactical.Preference, v => v.ToString("P1", System.Globalization.CultureInfo.InvariantCulture))));
                    }
                }
            }
            // PS-3k (§638, ruled): with no campaign the prediction reads the government's record on polling day - over its term (§752, A1); a campaign's shares already carry it.
            if (shareByParty == null && !NationalElection.TryPredictShares(id, out shareByParty, EconomicVote.RecordOverTerm(country, CurrentDate, out _),
                    on: CurrentDate, played: country))   // PS-4 (§688): the day it is held, so Germany derives its Länder on the chamber then seated; PS-6 US-26: on this world's own country's history rolled forward - a fork reads the fork's own counts
            {
                var notHeld = new ElectionRecord
                {
                    Turn = CurrentTurn,
                    Date = CurrentDate,
                    CountryId = id.ToString(),
                    Method = ElectionMethod.NotImplemented,
                    NotHeldReason = NationalElection.NotHeldReason(id),
                };
                country.ElectionHistory.Add(notHeld);
                if (!IsCounterfactual) { Debug.Log($"ELECTION: not held for {id} at turn {CurrentTurn} - {NationalElection.NotHeldReason(id)}"); }
                return notHeld;
            }

            // GATE-18 q2 with A-LOOP q24 5b: the count in play - every region on its own turnout, the campaign's doors at the turnout CACE, the declared
            // regional noise keyed by the master seed, the country and the day (ParliamentaryCount); the chamber is allocated on the counted shares
            bool campaignRan = campaign != null && CampaignRecord != null && CampaignRecord.ElectionDate == CurrentDate && PlayerCampaign != null;
            // GATE-18 piece two (q18 with K-1): on the polling day of a chamber election of record, the shares handed to the count carry that election's
            // eve-fitted factors per list (RecordFactors, E1's form at the count's unit); on any other day, and while no fit is patched in, nothing
            Dictionary<string, double> beforeFactors = shareByParty;
            if (RecordFactors.TryFor(id, CurrentDate, out ElectionVintage factoredElection, out Dictionary<string, double> factors))
            {
                shareByParty = RecordFactors.Apply(RecordFactors.Based(factoredElection, beforeFactors), factors);   // K109: a list handed nothing on its share of record
                if (!IsCounterfactual) { Debug.Log($"FACTORS: {id} on {CurrentDate:yyyy-MM-dd} - " + RecordFactors.Line(factoredElection, beforeFactors, shareByParty, factors)); }
            }
            ParliamentaryCount.Result counted = ParliamentaryCount.Count(id, CurrentDate, shareByParty, campaignRan ? PlayerCampaign.Setup : (CampaignRun.Setup?)null, campaignRan ? campaign.Gotv : null);
            LastPlayerCount = new ParliamentaryCount.Held
            {
                Day = CurrentDate, BeforeFactors = beforeFactors, Handed = shareByParty, Factors = factors, Election = factoredElection,
                Setup = campaignRan ? PlayerCampaign.Setup : (CampaignRun.Setup?)null, Gotv = campaignRan ? campaign.Gotv : null, Counted = counted,
            };
            if (counted != null)
            {
                shareByParty = counted.Shares;
                if (!IsCounterfactual) { Debug.Log($"COUNT: {id} on {CurrentDate:yyyy-MM-dd} - " + counted.Line); }
            }

            // GATE-18 piece two: on the polling day of record the threshold reads the record's base - the shares are the roster's vote, the lists it does not carry the rest
            ElectionRecord record = NationalElection.Run(id, CurrentTurn, shareByParty, CurrentDate, factors != null ? RecordFactors.RosterShareOfVote(factoredElection) : 1.0);
            country.ElectionHistory.Add(record);
            if (!IsCounterfactual) { Debug.Log($"ELECTION: held for {id} on {CurrentDate:yyyy-MM-dd} (turn {CurrentTurn}) by {record.Method}" + (PlayerCampaignResult != null ? " (a campaign result exists)" : " (no campaign result)")); }
            if (NationalElection.CountsParliament(record.Method))   // PS-6 K74: the parliamentary methods
            {
                ParliamentSystem.SetSeatsFromElection(country, record.Seats);

                // C-D4 (§38): the result carries into each party's long-term capital. ⚠ Its rule holds no
                // invented constant - organisational strength moves by the ratio of the party's new seat
                // count to its own previous one, which is the election's own number. Since PS-3k (§638) the
                // electorate moves with the government's record (EconomicVote), so two elections in one game
                // can return different chambers and the ratio is live.
                PartyCapital.CarryOver(country.PartyCapital, record.Seats);
            }
            return record;
        }

        /// <summary>
        /// FORK-2 (moved from `GameController.ResolveElectionVerdict`, its rules kept): what the chamber just elected SEATS in the world - nothing for a presidency
        /// (PS-6 US-7: a presidency is not formed from the chamber) or where no election was held today; otherwise the motion's standing refusals end (§641), the
        /// Speaker's round opens tomorrow on its own (§646: nothing installed tonight), Poland's Sejm opens its statute's steps (AF-8), and anywhere else the
        /// formation's government is installed with its arrival budget (§628, §632). Returns false where nothing was held today; the reading, the formation and
        /// the Sejm's procedure go back to the controller, whose verdict reads them.
        /// </summary>
        public bool SeatPlayerElection(ElectionRecord latest, out DeclarationReading reading, out GovernmentFormation.Formed government, out SejmFormation sejm)
        {
            reading = default;
            government = default;
            sejm = null;
            if (!PlayerCountryId.HasValue || latest == null || latest.Date != CurrentDate || !NationalElection.CountsParliament(latest.Method)) { return false; }
            CountryId id = PlayerCountryId.Value;
            Country country = _world?.GetCountry(id);
            if (country == null) { return false; }
            if (ConfidenceProcedure.RulesOf(id) == ConfidenceProcedure.Rules.Presidential) { return true; }

            // PS-2 (§619): the formation reads the declarations of the election just held; PS-3i-2c (§653): any election reads those dated at its own polling day
            reading = DeclarationReading.OfElection(id, latest.Date);
            government = GovernmentFormation.Form(country, reading);
            // PS-3a (§628): the government the election formed is STORED - the player's role and whose levers move the book are read from it from now on.
            GovernmentFormation.View formedView = GovernmentFormation.ViewOf(country, reading);
            // §641 (the reader): a motion's standing refusals end at the election either way - a new record starts without them, and a record that stands drops them.
            country.Government?.StandingRefusals.Clear();
            // §646 (R2): where the Speaker's round runs, the night installs nothing - the round opens the day after on this election's declarations.
            if (RoundsApply(id)) { return true; }
            // AF-8 (q8): Poland's government changes by Art. 162 and 154's steps, not tonight
            if (SejmStepsApply(id)) { sejm = OpenSejmFormation(country, latest.Date); return true; }
            // No government formed: the previous record stands - the verdict's own words, "you stay in office until one can" - so the AI does not take a book nobody was given.
            if (formedView != null && formedView.HasGovernment)
            {
                HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);   // K59
                country.Government = GovernmentRecord.FromView(country, formedView, latest.Date, world: _world);
                CabinetSystem.SeatOnEntry(country, chairsBefore);   // K59: the night's government seats the chairs it hands the player
                ResetArrivalBudgetWindow(id);   // PS-3e (§632): a new government gets its arrival budget
            }
            return true;
        }
    }
}
