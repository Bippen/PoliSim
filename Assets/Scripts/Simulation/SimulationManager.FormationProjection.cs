using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.Simulation
{
    /// <summary>
    /// GATE-18 piece three, q20 (Elias's ruling of 2026-10-18: "one formation code. The night's line names a projection ('EXPECTED TO FORM'), and office
    /// changes when the procedure completes"; the audit's GA-3-5, where the night told the player "IN OFFICE" before the Speaker's round rejected four
    /// proposals): <b>THE NIGHT'S FORMATION IS THE PROCEDURE'S OWN, PROJECTED.</b> The night ran the bare formation on the new chamber
    /// (`GovernmentFormation.ViewOf`) while the round drafted, answered and voted by its own code - two computations, and once a player had broken a bloc
    /// they parted. Here the procedure's own steps are walked from the night with nothing installed and nothing stored, by the very functions the
    /// procedure runs:
    /// <list type="bullet">
    /// <item><description><b>The Riksdag</b> (RF 6 kap. 4-5 §§): the round as it opens the day after (this election's declarations, no standing refusal - §641),
    /// the Speaker's order (`SpeakerOrder`), each party asked drafting (`DraftProposal`), every invited party answering and the investiture voted
    /// (`Formateur.Answer`), a rejected proposal counted to the sourced limit, the next party asked.</description></item>
    /// <item><description><b>The Bundestag</b> (Art. 63 GG): the Bundespräsident's order, the first candidate's government drafted, the bound parties'
    /// commitments (`Commitments`), the person ballot's tally (`Tally`) against a majority of the members, the elected candidate's government
    /// (`ElectedGovernment`). It stops at the first ballot: the fourteen days' nominations and the ballot the most votes win are not projected, and the
    /// projection says so (`FormationProjection.Partial`).</description></item>
    /// <item><description><b>The Sejm</b> (Konstytucja Art. 162, 154): the President's designee at the first sitting (the record's where the record names one,
    /// else the formation's projection), the confidence vote (`SejmFormation.Carries`); refused, the Sejm's own election (the record's cabinet where it
    /// carries, `SejmFormation.TryRecordCabinet`, else the projection where it carries); K302: Art. 155 ust. 1, the President's cabinet carried by a simple
    /// majority (`SejmFormation.PresidentAppoints`), else the term shortened and its elections' day (ust. 2, Art. 98 ust. 5) - the order `SejmStep` runs them in.</description></item>
    /// </list>
    /// A wait on the player - asked, or offered a place - is answered by the AI's rule for the player's party (K49, `AnswerWaitsAsTheAi`: the draft tabled; an
    /// offer accepted where every invited party accepts, else declined and redrafted without it, `AnswerOfferNow`), and the projection says it read one. Every
    /// function it calls reads the chamber and the declarations and writes nothing outside the round it opens for itself, so a projection moves no state.
    /// </summary>
    public partial class SimulationManager
    {
        /// <summary>GATE-18 q20: the government <paramref name="country"/>'s procedure is expected to form after the election of <paramref name="pollingDay"/>,
        /// on the chamber it seated - the night's projection; null for a country whose government no procedure forms here (a presidency; no player's chamber).</summary>
        public FormationProjection ProjectFormation(Country country, DateTime pollingDay)
        {
            if (country == null) { return null; }
            if (RoundsApply(country.Id)) { return ProjectRound(country, pollingDay.Date); }
            if (SejmStepsApply(country.Id)) { return ProjectSejm(country, pollingDay.Date); }
            if (ItalianMandatesApply(country.Id)) { return ProjectMandates(country, pollingDay.Date); }   // PS-7 (K204): Italy's mandates from the first sitting (SimulationManager.Italy.cs)
            return null;
        }

        private FormationProjection ProjectRound(Country country, DateTime pollingDay)
        {
            bool bundestag = IsBundestag(country.Id);
            string player = country.PlayerPartyAbbrev;
            // the round as OpenRoundAfterElection opens it the next day: the election's own declarations (§653), no refusal standing (§641)
            var round = new SpeakerRound
            {
                OpenedOn = pollingDay.AddDays(1), Occasion = "after the election of " + pollingDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Vintage = WorldClock.VintageOfElection(country.Id, pollingDay), Stage = RoundStage.Consulting, ReadsOn = pollingDay,
            };
            if (bundestag) { round.Convenes = pollingDay.AddDays(SpeakerRound.BundestagConvenesWithinDays); round.Phase = 1; }
            DeclarationReading reading = RoundReading(country, round);
            List<RedLine> lines = RoundLines(country, round);
            round.Order = SpeakerOrder(country, reading, lines);
            var projection = new FormationProjection
            {
                Country = country.Id, PollingDay = pollingDay,
                Procedure = bundestag ? "the chancellor's election (Art. 63 GG)" : "the Speaker's round (RF 6 kap.)",
            };
            projection.Order.AddRange(round.Order);
            projection.FirstAsked = round.Order.Count > 0 ? round.Order[0] : null;
            projection.PlayerAskedFirst = projection.FirstAsked != null && projection.FirstAsked == player;
            if (round.Order.Count == 0) { projection.Reason = "no party is asked"; projection.View = NoneView(country, projection); return projection; }

            // each ask as the round makes it (AdvanceSpeakerRound, AskNext): an ask that tables nothing passes to the next party without a rejection; the
            // asks are bounded so a round in which no party can ever table ends the projection rather than the frame
            int asks = 0, maxAsks = (SpeakerRound.ProposalLimit + 1) * round.Order.Count;
            // D-NIGHT ("For Code" 2): the round's cells - one per proposal the Riksdag votes on, in the order it votes - and the last refused vote, which the
            // night draws where none is expected ("For Code" 1)
            var cells = new List<GovernmentFormation.RoundCell>();
            ProposalVerdict lastRefused = null;
            while (round.Rejections < SpeakerRound.ProposalLimit && asks < maxAsks)
            {
                string asked = round.Order[asks % round.Order.Count];
                asks++;
                FormationProposal proposal = DraftProposal(country, round, asked);
                if (asks == 1) { projection.FirstDraft.AddRange(proposal.CabinetParties); }
                if (asked == player)
                {
                    projection.ReadsPlayersAnswer = true;
                    projection.Steps.Add(Pf("{0} (the player's party) is asked and tables the formation's draft - {1}", asked, Describe(proposal)));
                }
                else if (Involves(proposal, player))
                {
                    projection.ReadsPlayersAnswer = true;
                    ProposalVerdict offer = Formateur.Answer(country, proposal, CurrentDate, _world, reading, lines, null);
                    if (offer != null && offer.AllAccept) { projection.Steps.Add(Pf("{0} offers {1} a place in {2}; it accepts (every invited party accepts)", asked, player, Describe(proposal))); }
                    else
                    {
                        round.Declines.Add(player + ">" + asked);
                        FormationProposal without = DraftProposal(country, round, asked);
                        ProposalVerdict alone = Involves(without, player) ? null : Formateur.Answer(country, without, CurrentDate, _world, reading, lines, player);
                        bool tables = alone != null && (bundestag ? alone.AllAccept && alone.Investiture != null && !alone.Investiture.SplitsJointGroup : alone.Passes);
                        if (!tables)
                        {
                            projection.Steps.Add(Pf("{0} offers {1} a place in {2}; it declines (an invited party refuses), and {0} cannot form without it - the next party is asked", asked, player, Describe(proposal)));
                            continue;
                        }
                        projection.Steps.Add(Pf("{0} offers {1} a place; it declines, and {0} tables {2} without it", asked, player, Describe(without)));
                        proposal = without;
                    }
                }

                if (bundestag) { return FirstBallot(country, round, proposal, projection); }

                ProposalVerdict verdict = Formateur.Answer(country, proposal, CurrentDate, _world, reading, lines, player);
                int number = round.Rejections + 1;
                if (verdict.Passes)
                {
                    projection.Steps.Add(Pf("proposal {0}: {1} - the Riksdag approves it ({2})", number, Describe(proposal), verdict.Reason));
                    Expect(country, projection, proposal, verdict, Pf("THE SPEAKER'S PROPOSAL {0} OF AT MOST {1} - {2} ASKED", number, SpeakerRound.ProposalLimit, PartySystems.ShortName(country.Id, asked).ToUpperInvariant()));
                    cells.Add(new GovernmentFormation.RoundCell(asked, GovernmentFormation.RoundCellState.Current));
                    Cells(projection.View, cells, SpeakerRound.ProposalLimit);
                    AddVote(projection.View, VoteOf(country, verdict, projection.View));
                    return projection;
                }
                round.Rejections++;
                cells.Add(new GovernmentFormation.RoundCell(asked, GovernmentFormation.RoundCellState.Refused));
                lastRefused = verdict;
                projection.Steps.Add(Pf("proposal {0}: {1} - rejected ({2}); {3} of {4} (RF 6 kap. 5 §)", number, Describe(proposal), verdict.Reason, round.Rejections, SpeakerRound.ProposalLimit));
            }
            projection.Reason = round.Rejections >= SpeakerRound.ProposalLimit
                ? Pf("the Riksdag rejects {0} proposals - the procedure breaks off (RF 6 kap. 5 §)", SpeakerRound.ProposalLimit)
                : "no party asked can table a proposal - the Speaker's round finds none";
            projection.View = NoneView(country, projection);
            if (round.Rejections >= SpeakerRound.ProposalLimit && cells.Count > 0) { cells[cells.Count - 1] = new GovernmentFormation.RoundCell(cells[cells.Count - 1].Asked, GovernmentFormation.RoundCellState.LastRefused); }
            Cells(projection.View, cells, SpeakerRound.ProposalLimit);
            AddVote(projection.View, VoteOf(country, lastRefused, projection.View));
            return projection;
        }

        /// <summary>The Bundestag's first ballot (Art. 63 Abs. 1-2 GG), as `PersonBallot` holds it in phase 1, on the proposal tabled.</summary>
        private FormationProjection FirstBallot(Country country, SpeakerRound round, FormationProposal proposal, FormationProjection projection)
        {
            DeclarationReading reading = RoundReading(country, round);
            List<RedLine> lines = RoundLines(country, round);
            CoalitionFormation.Chamber chamber = Formateur.ChamberOf(country, reading, lines, out IReadOnlyList<PoliticalParty> parties);
            Dictionary<string, string> committed = Commitments(country, round, proposal);
            var candidates = new List<string> { proposal.Formateur };
            Dictionary<string, int> votes = Tally(country, round, chamber, parties, candidates, out List<(string Party, string For, string Reason)> cast, committed);
            int majority = MajorityOf(country);
            int got = votes.TryGetValue(proposal.Formateur, out int v) ? v : 0;
            // D-NIGHT ("For Code" 8): Art. 63's three steps as the round's cells - the first ballot, the fourteen days, the last ballot; the projection reaches
            // the first alone
            var cells = new List<GovernmentFormation.RoundCell> { new GovernmentFormation.RoundCell(proposal.Formateur, got >= majority ? GovernmentFormation.RoundCellState.Current : GovernmentFormation.RoundCellState.Refused) };
            if (got >= majority)
            {
                FormationProposal government = ElectedGovernment(country, round, proposal.Formateur, proposal, out ProposalVerdict verdict, out bool _);
                if (government != null && verdict?.Investiture != null)
                {
                    projection.Steps.Add(Pf("the Bundespräsident proposes {0} candidate; the Bundestag elects at the first ballot, {1} of the {2} a majority of the members needs - {3}", PoliSim.Wording.EnglishText.Possessive(proposal.Formateur, false, true), got, majority, Describe(government)));
                    Expect(country, projection, government, verdict, Pf("THE BUNDESPRÄSIDENT'S CANDIDATE - ELECTED AT THE FIRST BALLOT, {0} VOTES, {1} NEEDED", got, majority));
                    Cells(projection.View, cells, BundestagBallots);
                    AddVote(projection.View, BallotOf(chamber, parties, cast, proposal.Formateur, projection.Cabinet, majority));
                    return projection;
                }
                projection.Reason = Pf("{0} candidate is elected at the first ballot, and no government can be drawn on its party", PoliSim.Wording.EnglishText.Possessive(Short(country, proposal.Formateur), false, true));
            }
            else
            {
                projection.Reason = Pf("{0} candidate, the Bundespräsident's, takes {1} of the {2} a majority of the members needs at the first ballot - the fourteen days follow (Art. 63 Abs. 3), and the projection stops there", PoliSim.Wording.EnglishText.Possessive(Short(country, proposal.Formateur), false, true), got, majority);
            }
            projection.Partial = true;
            projection.Steps.Add(projection.Reason);
            projection.View = NoneView(country, projection);
            Cells(projection.View, cells, BundestagBallots);
            GovernmentFormation.NightVote ballot = BallotOf(chamber, parties, cast, proposal.Formateur, proposal.CabinetParties, majority);
            ballot.ElectedWithoutGovernment = ballot.Carried;   // the review-2 N8: elected, and no government drawn on its party - marked, never inferred
            if (ballot.ElectedWithoutGovernment) { projection.View.NoneWords = "ELECTED - NO GOVERNMENT DRAWN ON ITS PARTY"; }   // N3: the board's words, not "the ballots go on"
            AddVote(projection.View, ballot);
            return projection;
        }

        /// <summary>SOURCED - Art. 63 GG, its three phases. D-NIGHT ("For Code" 8): the chancellor's election as the night's cells - Art. 63 Abs. 2 (the first ballot), Abs. 3 (the fourteen days), Abs. 4 (the last ballot).</summary>
        private const int BundestagBallots = 3;

        /// <summary>D-NIGHT: the round's cells onto the board's view, padded with the steps not reached.</summary>
        private static void Cells(GovernmentFormation.View view, List<GovernmentFormation.RoundCell> cells, int limit)
        {
            if (view == null) { return; }
            view.Round.AddRange(cells);
            while (view.Round.Count < limit) { view.Round.Add(new GovernmentFormation.RoundCell(null, GovernmentFormation.RoundCellState.NotReached)); }
            view.RoundLimit = limit;
        }

        private static void AddVote(GovernmentFormation.View view, GovernmentFormation.NightVote vote)
        {
            if (view != null && vote != null) { view.Votes.Add(vote); }
        }

        /// <summary>D-NIGHT ("For Code" 1): a Riksdag proposal's investiture as the night draws it - every seated party's side and reason as
        /// `CoalitionFormation.Evaluate` weighed them (the cabinet's and its accepted supporters' votes for it, the red-lined and hold-outs against, the rest
        /// abstaining), the refusal line at a majority of the members against it (RF 6 kap. 4 §). Null where the proposal faced no investiture.</summary>
        private static GovernmentFormation.NightVote VoteOf(Country country, ProposalVerdict verdict, GovernmentFormation.View view)
        {
            CoalitionFormation.CabinetEvaluation e = verdict?.Investiture;
            if (e == null || e.Sides == null) { return null; }
            IReadOnlyList<PoliticalParty> parties = verdict.Parties ?? PartySystems.For(country.Id);
            if (parties == null || parties.Count != e.Sides.Length) { return null; }
            var vote = new GovernmentFormation.NightVote
            {
                Members = view.TotalSeats, Needed = view.Majority, CountsAgainst = view.NegativeRule, Carried = verdict.Passes, Fell = verdict.Passes ? null : verdict.Reason,
            };
            for (int p = 0; p < parties.Count; p++)
            {
                int seats = SeatsOf(country, parties[p].Abbrev);
                if (seats <= 0) { continue; }
                vote.Sides.Add(new GovernmentFormation.VoteSide { Abbrev = parties[p].Abbrev, Seats = seats, Side = e.Sides[p], Reason = e.Reasons?[p] });
                switch (e.Sides[p])
                {
                    case CoalitionFormation.InvestitureSide.InCabinet:
                    case CoalitionFormation.InvestitureSide.Supports: vote.For += seats; break;
                    case CoalitionFormation.InvestitureSide.Against: vote.Against += seats; break;
                    default: vote.Abstain += seats; break;
                }
            }
            // the review's M2 (N1): a drafted partner that declines its seat leaves "for" - the proposal falls on its answer, not on a majority against; it is
            // drawn as it stands (abstaining: it neither sits in the cabinet nor votes against it - the round counts the refusal, RF 6 kap. 5 §), its own answer
            // the reason. Against is untouched, so it stays the investiture's OpposedSeats.
            // review 3's N9: a refusing supporter likewise - drawn as it stands, with its own reason
            var refusals = new List<string>();
            foreach (PartyAnswer answer in verdict.Answers)
            {
                if (answer.Accepts) { continue; }
                if (answer.InCabinet) { vote.Decline(answer.Party, answer.Reason); } else { vote.Withdraw(answer.Party, answer.Reason); }
                refusals.Add(PartySystems.ShortName(country.Id, answer.Party) + ": " + answer.Reason);
            }
            // review 3's N12: the fall in the model's own specific words - each refusing party's answer, where the verdict names only "an invited party refuses"
            if (!verdict.Passes && refusals.Count > 0) { vote.Fell = string.Join("; ", refusals); }
            return vote;
        }

        /// <summary>D-NIGHT ("For Code" 1): the Bundestag's first ballot as the night draws it - the ballot's own tally (`Tally`), each party for the
        /// candidate (in the government it forms, or from outside it) or not (the tally casts no "no": a party that refuses every candidate abstains, the
        /// game's premise), the line at a majority of the members for (Art. 63 Abs. 2).</summary>
        private static GovernmentFormation.NightVote BallotOf(CoalitionFormation.Chamber chamber, IReadOnlyList<PoliticalParty> parties, List<(string Party, string For, string Reason)> cast,
            string candidate, IReadOnlyList<string> cabinet, int majority)
        {
            var vote = new GovernmentFormation.NightVote { Needed = majority };
            foreach (int s in chamber.Seats) { vote.Members += s; }
            foreach ((string party, string votedFor, string reason) in cast)
            {
                int p = -1;
                for (int i = 0; i < parties.Count; i++) { if (parties[i].Abbrev == party) { p = i; break; } }
                if (p < 0 || chamber.Seats[p] <= 0) { continue; }
                int seats = chamber.Seats[p];
                bool votesFor = votedFor != null && votedFor == candidate;
                CoalitionFormation.InvestitureSide side = !votesFor ? CoalitionFormation.InvestitureSide.Abstains
                    : cabinet != null && Contains(cabinet, party) ? CoalitionFormation.InvestitureSide.InCabinet : CoalitionFormation.InvestitureSide.Supports;
                vote.Sides.Add(new GovernmentFormation.VoteSide { Abbrev = party, Seats = seats, Side = side, Reason = reason });
                if (votesFor) { vote.For += seats; } else { vote.Abstain += seats; }
            }
            vote.Carried = vote.For >= majority;
            return vote;
        }

        /// <summary>D-NIGHT ("For Code" 1, 8): one Italian chamber's confidence vote (art. 94) as the night draws it - the division's own sides
        /// (`ConfidenceProcedure.Article94`), its members and the votes it needs.</summary>
        private static GovernmentFormation.NightVote MotionOf(ConfidenceProcedure.MotionVote motion, IReadOnlyList<string> cabinet)
        {
            if (motion == null) { return null; }
            var vote = new GovernmentFormation.NightVote
            {
                Chamber = motion.ChamberName, Members = motion.Members, Needed = motion.Needed, For = motion.For, Against = motion.Against, Carried = motion.Carried,
            };
            vote.Abstain = Math.Max(0, motion.Members - motion.For - motion.Against);
            foreach (DivisionSide s in motion.Sides)
            {
                if (s == null || s.Seats <= 0) { continue; }
                CoalitionFormation.InvestitureSide side = s.Side < 0 ? CoalitionFormation.InvestitureSide.Against : s.Side == 0 ? CoalitionFormation.InvestitureSide.Abstains
                    : cabinet != null && Contains(cabinet, s.Abbrev) ? CoalitionFormation.InvestitureSide.InCabinet : CoalitionFormation.InvestitureSide.Supports;
                vote.Sides.Add(new GovernmentFormation.VoteSide { Abbrev = s.Abbrev, Seats = s.Seats, Side = side, Reason = s.Reason });
            }
            return vote;
        }

        private static bool Contains(IReadOnlyList<string> keys, string key)
        {
            foreach (string k in keys) { if (k == key) { return true; } }
            return false;
        }

        private FormationProjection ProjectSejm(Country country, DateTime pollingDay)
        {
            var projection = new FormationProjection { Country = country.Id, PollingDay = pollingDay, Procedure = "the Sejm's steps (Konstytucja Art. 162, 154)" };
            DateTime sitting = SejmFormation.FirstSittingAfter(country, pollingDay);   // K302: Art. 98 ust. 5's fifteenth day after a shortened term's election
            bool ofRecord = SejmFormation.TryOfRecord(country.Id, pollingDay, out SejmFormation.StepsOfRecord record);
            // Art. 154 ust. 1, at the first sitting (SejmStep, AwaitingSitting): the record's designee where the record names one, else the formation's projection
            List<string> cabinet = null, support = null;
            string pm = null, who = null, designeeHead = null;
            if (ofRecord && WorldClock.TryGovernmentAt(country.Id, record.Appointed, out WorldClock.GovernmentOfRecord designee) && designee.Cabinet != null)
            {
                cabinet = new List<string>(designee.Cabinet);
                support = new List<string>(designee.Support ?? new string[0]);
                pm = PartyOfHead(designee.Head) ?? designee.Cabinet[0];
                who = "the record's designee, " + designee.Head;
                designeeHead = designee.Head;
            }
            else
            {
                GovernmentFormation.View view = Projection(country, pollingDay);
                if (view != null && view.HasGovernment)
                {
                    cabinet = Keys(view.Cabinet);
                    support = Keys(view.Support);
                    pm = GovernmentRecord.FromView(country, view, sitting, world: _world).PmParty;
                    who = "the formation's projection on the new Sejm";
                }
            }
            if (cabinet != null)
            {
                // ust. 2 (AwaitingConfidence): carried, the designee's cabinet governs
                bool carried = SejmFormation.Carries(country, cabinet, support, out int yes, out int members);
                projection.Steps.Add(Pf("the President designates {0} - {1}; the Sejm's confidence: {2} of {3} members, {4} (Art. 154 ust. 1-2)", who, string.Join("+", cabinet), yes, members, carried ? "carried" : "refused"));
                if (carried) { Installed(country, pollingDay, ofRecord ? record.Appointed : sitting.AddDays(SejmFormation.StepWindowDays), true, ref pm, ref cabinet, ref support); return ExpectNamed(projection, pm, cabinet, support, "THE PRESIDENT'S DESIGNEE - THE SEJM'S CONFIDENCE (ART. 154 UST. 2)", designeeHead, yes, members); }
            }
            else { projection.Steps.Add("no cabinet the Sejm could carry is designated - Art. 154 ust. 1's fourteen days pass without an appointment"); }

            // ust. 3 (SejmElects): the record's cabinet where it carries the game's Sejm, else the formation's projection where it carries
            if (ofRecord && SejmFormation.TryRecordCabinet(country, pollingDay, out List<string> recordCabinet, out List<string> recordSupport, out string recordPm, out string recordHead,
                    out bool recordCarries, out int recordYes, out int recordMembers))
            {
                projection.Steps.Add(Pf("the Sejm votes on the record's cabinet {0} under {1}: {2} of {3} members (Art. 154 ust. 3)", string.Join("+", recordCabinet), recordHead, recordYes, recordMembers));
                if (recordCarries) { Installed(country, pollingDay, record.SejmAppointed, false, ref recordPm, ref recordCabinet, ref recordSupport); return ExpectNamed(projection, recordPm, recordCabinet, recordSupport, "THE SEJM ELECTS THE RECORD'S CABINET (ART. 154 UST. 3)", recordHead, recordYes, recordMembers); }
            }
            GovernmentFormation.View elects = Projection(country, pollingDay);
            if (elects != null && elects.HasGovernment && SejmFormation.Carries(country, Keys(elects.Cabinet), Keys(elects.Support), out int y3, out int m3))
            {
                projection.Steps.Add(Pf("the Sejm elects the formation's projection {0}: {1} of {2} members (Art. 154 ust. 3)", string.Join("+", Keys(elects.Cabinet)), y3, m3));
                // the review's L1: on the procedure's own days - the Sejm elects on the step after the designee's confidence was refused (two windows after the
                // sitting), or on the window after the sitting where none was designated, the record's days where the record dates them; the President appoints
                // after it, and the install's own rule seats it (`Installed`)
                DateTime electsOn = ofRecord ? record.SejmElected : sitting.AddDays((cabinet != null ? 2 : 1) * SejmFormation.StepWindowDays);
                DateTime appointedOn = ofRecord ? record.SejmAppointed : electsOn.AddDays(SejmFormation.AppointmentAfterSejmElectionDays);
                string electedPm = GovernmentRecord.FromView(country, elects, electsOn, world: _world).PmParty;
                List<string> electedCabinet = Keys(elects.Cabinet), electedSupport = Keys(elects.Support);
                Installed(country, pollingDay, appointedOn, false, ref electedPm, ref electedCabinet, ref electedSupport);
                return ExpectNamed(projection, electedPm, electedCabinet, electedSupport, "THE SEJM ELECTS ITS CABINET (ART. 154 UST. 3)", null, y3, m3);
            }
            projection.Steps.Add("the Sejm elects no prime minister (Art. 154 ust. 3)");

            // K302: Art. 155 ust. 1 (PresidentAppoints, AwaitingPresidentsConfidence) - the President appoints, and the Sejm's confidence by a simple majority; ust. 2 -
            // refused, the President shortens the Sejm's term and orders elections. Its days are the steps' own in the player's Poland (`SejmStep`: ust. 3's fourteen
            // days run out after its vote, then the President's fourteen, the vote on the appointment's fourteenth); an AI Sejm's steps run whole on its first
            // sitting, so its shortening is dated that day (`WorldCalendar.TryExtraElection` reads it)
            projection.Procedure = "the Sejm's steps (Konstytucja Art. 162, 154, 155)";
            bool daysOfSteps = SejmStepsApply(country.Id);
            DateTime sejmVoted = ofRecord ? record.SejmElected : sitting.AddDays((cabinet != null ? 2 : 1) * SejmFormation.StepWindowDays);
            DateTime appointedBy = sejmVoted.AddDays(SejmFormation.PresidentAppointsAfterSejmVoteDays), votedBy = appointedBy.AddDays(SejmFormation.StepWindowDays);
            SejmFormation.PresidentsCabinet named = SejmFormation.PresidentAppoints(country, DeclarationReading.OfElection(country.Id, pollingDay));
            if (named.Cabinet.Count > 0)
            {
                projection.Steps.Add(Pf("the President appoints {0} (Art. 155 ust. 1), {1}; the Sejm's confidence: {2}", string.Join("+", named.Cabinet), named.Choice(), named.Line()));
                string pm155 = named.PmParty;
                List<string> cabinet155 = new List<string>(named.Cabinet), support155 = new List<string>(named.Support);
                Installed(country, pollingDay, daysOfSteps ? appointedBy : sitting, true, ref pm155, ref cabinet155, ref support155);
                if (named.Carried) { return ExpectNamed(projection, pm155, cabinet155, support155, "THE PRESIDENT'S CABINET - THE SEJM'S CONFIDENCE BY A SIMPLE MAJORITY (ART. 155 UST. 1)", null, named.For, named.Members); }
                // refused: the President's cabinet resigns and carries on as caretaker (Art. 162 ust. 2 pkt 1, ust. 3) - the AI's install reads it (`FormAsAi`)
                projection.RefusedPmParty = pm155;
                projection.RefusedCabinet.AddRange(cabinet155);
                projection.RefusedSupport.AddRange(support155);
            }
            // the shortening's day as `SejmStep` takes it: the vote's day, or the President's own where he finds no admissible cabinet to appoint
            DateTime shortenedOn = daysOfSteps ? (named.Cabinet.Count > 0 ? votedBy : appointedBy) : sitting;
            projection.Reason = Pf("the Sejm refuses the President's cabinet{0} its confidence, {1} for and {2} against (Art. 155 ust. 1) - the President shortens the Sejm's term and orders elections for {3} (Art. 155 ust. 2, Art. 98 ust. 5)",
                named.Cabinet.Count > 0 ? " " + string.Join("+", named.Cabinet) : string.Empty, named.For, named.Against,
                SejmFormation.ShortenedTermElectionDay(shortenedOn).ToString("d MMMM yyyy", CultureInfo.InvariantCulture));
            projection.Steps.Add(projection.Reason);
            return projection;
        }

        /// <summary>
        /// PS-7 (K204, q20's one formation code): <b>Italy's mandates</b> (Costituzione art. 92, 94; K155) projected from the night - the very mandates
        /// `ItalianFormation.Run` tries on the chambers the count seated, as the day loop runs them on the first sitting (`AdvanceItalianMandates`); nothing installed,
        /// nothing stored. The board's view is the Camera's (its seats, the confidence vote's For and Against there), whichever arithmetic the granted mandate was
        /// formed on - the step line names it; where three mandates fail, the dissolution (art. 88, the semestre bianco of art. 88 c. 2 deferring it) and art. 61's
        /// polling day.
        /// </summary>
        private FormationProjection ProjectMandates(Country country, DateTime pollingDay)
        {
            var projection = new FormationProjection
            {
                Country = country.Id, PollingDay = pollingDay,
                Procedure = "the President's mandates, each government to the confidence of both chambers (Costituzione art. 92, 94)",
            };
            DateTime firstSitting = WorldClock.ItalyFirstSittingAfter(pollingDay);
            List<ItalianFormation.Mandate> mandates = ItalianFormation.Run(country, DeclarationReading.OfElection(country.Id, pollingDay), out ItalianFormation.Mandate granted);
            foreach (ItalianFormation.Mandate m in mandates) { projection.Steps.Add(ItalianFormation.Line(m)); }
            if (granted != null)
            {
                string step = Pf("MANDATE {0} OF AT MOST {1} · FORMED ON {2} · CONFIDENCE OF BOTH CHAMBERS ({3}-{4} CAMERA, {5}-{6} SENATO) · FROM THE FIRST SITTING, {7} · SEATS SHOWN ARE THE CAMERA'S",
                    granted.Number, ConfidenceProcedure.ItalianMandates, granted.Arithmetic.ToUpperInvariant(), granted.Camera.For, granted.Camera.Against, granted.Senato.For, granted.Senato.Against,
                    firstSitting.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant());
                ExpectNamed(projection, granted.Camera.PmParty, Keys(granted.View.Cabinet), Keys(granted.View.Support), step, null, granted.Camera.For, granted.Camera.Members);
                GovernmentFormation.View view = ProjectedView(country, projection, null);
                foreach ((string _, int seats) in view.Cabinet) { view.CabinetSeats += seats; }
                view.SupportedSeats = granted.Camera.For;
                view.OpposedSeats = granted.Camera.Against;
                view.Outcome = granted.View.Outcome;
                view.ProjectionVote = ItalianVote;
                projection.View = view;
                MandateCells(view, mandates);
                AddVote(view, MotionOf(granted.Camera, projection.Cabinet));   // art. 94: once per chamber
                AddVote(view, MotionOf(granted.Senato, projection.Cabinet));
                return projection;
            }
            bool barred = Dissolution(firstSitting, out DateTime dissolvedOn, out DateTime polling);
            projection.Reason = Pf("{0} mandates fail - the chambers are dissolved{1} (art. 88), and the new ones elected on {2} (art. 61)",
                ConfidenceProcedure.ItalianMandates, barred ? " on " + dissolvedOn.ToString("d MMMM yyyy", CultureInfo.InvariantCulture) + ", when the President's last six months end" : string.Empty,
                polling.ToString("d MMMM yyyy", CultureInfo.InvariantCulture));
            projection.Steps.Add(projection.Reason);
            GovernmentFormation.View none = NoneView(country, projection);
            none.ProjectionVote = ItalianVote;
            none.NoneWords = "THE MANDATES FAIL - DISSOLUTION";
            projection.View = none;
            MandateCells(none, mandates);
            ItalianFormation.Mandate voted = null;
            foreach (ItalianFormation.Mandate m in mandates) { if (m.Camera != null && m.Senato != null) { voted = m; } }   // the last the chambers voted on
            if (voted != null)
            {
                var cabinet = Keys(voted.View.Cabinet);
                AddVote(none, MotionOf(voted.Camera, cabinet));
                AddVote(none, MotionOf(voted.Senato, cabinet));
            }
            return projection;
        }

        /// <summary>D-NIGHT ("For Code" 2, 8): Italy's mandates as the round's cells - each tried mandate under its prime minister's party (none where it formed no
        /// government), the granted one current, three failed the last struck in the refusal's ink.</summary>
        private static void MandateCells(GovernmentFormation.View view, List<ItalianFormation.Mandate> mandates)
        {
            var cells = new List<GovernmentFormation.RoundCell>();
            foreach (ItalianFormation.Mandate m in mandates)
            {
                string asked = m.Camera?.PmParty ?? (m.View != null && m.View.Cabinet.Count > 0 ? m.View.Cabinet[0].Abbrev : null);
                cells.Add(new GovernmentFormation.RoundCell(asked, m.Granted ? GovernmentFormation.RoundCellState.Current : GovernmentFormation.RoundCellState.Refused));
            }
            if (cells.Count >= ConfidenceProcedure.ItalianMandates && cells[cells.Count - 1].State == GovernmentFormation.RoundCellState.Refused)
            {
                cells[cells.Count - 1] = new GovernmentFormation.RoundCell(cells[cells.Count - 1].Asked, GovernmentFormation.RoundCellState.LastRefused);
            }
            Cells(view, cells, ConfidenceProcedure.ItalianMandates);
        }

        /// <summary>PS-7: the vote office changes on in Italy, in the board's foot ("A PROJECTION - OFFICE CHANGES ONLY WHEN ...").</summary>
        private const string ItalianVote = "BOTH CHAMBERS VOTE CONFIDENCE";

        /// <summary>The expected government from the proposal the procedure carries, with its supporters as the install reads them (`Install`: the invited
        /// parties outside the cabinet that accept) and the board's view of it.</summary>
        private FormationProjection Expect(Country country, FormationProjection projection, FormationProposal proposal, ProposalVerdict verdict, string step)
        {
            var supporters = new List<string>();
            foreach (PartyAnswer answer in verdict.Answers) { if (!answer.InCabinet && answer.Accepts) { supporters.Add(answer.Party); } }
            projection.Expected = true;
            projection.PmParty = proposal.Formateur;
            projection.Cabinet.AddRange(proposal.CabinetParties);
            projection.Support.AddRange(supporters);
            projection.Step = step;
            projection.View = ProjectedView(country, projection, verdict.Investiture);
            return projection;
        }

        /// <summary>The government the Sejm's install seats for a cabinet the steps carry, by its own rule (`InstallSejmCabinet`; the appointment's
        /// `SameCabinet`): the outgoing record where the President's designee is the sitting cabinet (reappointed, Art. 154 ust. 1); the formation's record
        /// - its supporters, its prime minister's party - where the formation's projection names the same cabinet; else the cabinet as named.
        /// <paramref name="on"/>: the day it is installed, the record's where the record dates it.</summary>
        private void Installed(Country country, DateTime pollingDay, DateTime on, bool designated, ref string pm, ref List<string> cabinet, ref List<string> support)
        {
            GovernmentRecord g = country.Government;
            if (designated && g != null && g.PmParty == pm && SameKeys(g.Cabinet, cabinet))
            {
                pm = g.PmParty; cabinet = new List<string>(g.Cabinet); support = new List<string>(g.Support);
                return;
            }
            GovernmentFormation.View view = Projection(country, pollingDay);
            if (view != null && view.HasGovernment && SameKeys(Keys(view.Cabinet), cabinet))
            {
                GovernmentRecord formed = GovernmentRecord.FromView(country, view, on, world: _world);
                pm = formed.PmParty; cabinet = new List<string>(formed.Cabinet); support = new List<string>(formed.Support);
            }
        }

        private static FormationProjection ExpectNamed(FormationProjection projection, string pm, List<string> cabinet, List<string> support, string step, string head, int votesFor, int members)
        {
            projection.Expected = true;
            projection.PmParty = pm;
            projection.Cabinet.AddRange(cabinet);
            if (support != null) { projection.Support.AddRange(support); }
            projection.Step = step;
            projection.Head = head;
            projection.VotesFor = votesFor;
            projection.Members = members;
            return projection;
        }

        /// <summary>The board's view of the expected government - its cabinet and supporters with their seats, and the investiture the procedure votes.</summary>
        private static GovernmentFormation.View ProjectedView(Country country, FormationProjection projection, CoalitionFormation.CabinetEvaluation investiture)
        {
            GovernmentFormation.View view = BaseView(country, projection);
            view.HasGovernment = true;
            if (investiture != null)
            {
                view.Outcome = investiture.Kind;
                view.CabinetSeats = investiture.CabinetSeats;
                view.SupportedSeats = investiture.SupportedSeats;
                view.OpposedSeats = investiture.OpposedSeats;
            }
            foreach (string key in projection.Cabinet) { view.Cabinet.Add((key, SeatsOf(country, key))); }
            foreach (string key in projection.Support) { view.Support.Add((key, SeatsOf(country, key))); }
            view.Cabinet.Sort((a, b) => b.Seats.CompareTo(a.Seats));
            view.Support.Sort((a, b) => b.Seats.CompareTo(a.Seats));
            if (view.PlayerParty != null)
            {
                view.PlayerInCabinet = projection.Cabinet.Contains(view.PlayerParty);
                view.PlayerSupports = !view.PlayerInCabinet && projection.Support.Contains(view.PlayerParty);
            }
            view.ProjectionStep = projection.Step;
            return view;
        }

        private static GovernmentFormation.View NoneView(Country country, FormationProjection projection)
        {
            GovernmentFormation.View view = BaseView(country, projection);
            view.HasGovernment = false;
            view.Reason = projection.Reason;
            return view;
        }

        private static GovernmentFormation.View BaseView(Country country, FormationProjection projection)
        {
            var view = new GovernmentFormation.View
            {
                NegativeRule = ChamberRules.UsesNegativeParliamentarism(country.Id),
                DeclarationsSourced = DeclaredRedLines.IsSourced(country.Id),
                PlayerParty = string.IsNullOrEmpty(country.PlayerPartyAbbrev) ? null : country.PlayerPartyAbbrev,
                ReadsPlayersAnswer = projection.ReadsPlayersAnswer,
                Partial = projection.Partial,
            };
            if (country.ParliamentSeats != null) { foreach (int s in country.ParliamentSeats.Values) { view.TotalSeats += s; } }
            view.Majority = view.TotalSeats / 2 + 1;
            return view;
        }

        private static string Describe(FormationProposal proposal) =>
            proposal == null ? "no proposal" : string.Join("+", proposal.CabinetParties) + " led by " + proposal.Formateur + (proposal.Supporters.Count > 0 ? " with " + string.Join("+", proposal.Supporters) : string.Empty);

        private static string Pf(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
