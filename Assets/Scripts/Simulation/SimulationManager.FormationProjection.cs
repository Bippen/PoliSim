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
    /// carries, `SejmFormation.TryRecordCabinet`, else the projection where it carries) - the order `SejmStep` runs them in.</description></item>
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
                    return Expect(country, projection, proposal, verdict, Pf("THE SPEAKER'S PROPOSAL {0} OF AT MOST {1} - {2} ASKED", number, SpeakerRound.ProposalLimit, PartySystems.ShortName(country.Id, asked).ToUpperInvariant()));
                }
                round.Rejections++;
                projection.Steps.Add(Pf("proposal {0}: {1} - rejected ({2}); {3} of {4} (RF 6 kap. 5 §)", number, Describe(proposal), verdict.Reason, round.Rejections, SpeakerRound.ProposalLimit));
            }
            projection.Reason = round.Rejections >= SpeakerRound.ProposalLimit
                ? Pf("the Riksdag rejects {0} proposals - the procedure breaks off (RF 6 kap. 5 §)", SpeakerRound.ProposalLimit)
                : "no party asked can table a proposal - the Speaker's round finds none";
            projection.View = NoneView(country, projection);
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
            Dictionary<string, int> votes = Tally(country, round, chamber, parties, candidates, out List<(string Party, string For, string Reason)> _, committed);
            int majority = MajorityOf(country);
            int got = votes.TryGetValue(proposal.Formateur, out int v) ? v : 0;
            if (got >= majority)
            {
                FormationProposal government = ElectedGovernment(country, round, proposal.Formateur, proposal, out ProposalVerdict verdict, out bool _);
                if (government != null && verdict?.Investiture != null)
                {
                    projection.Steps.Add(Pf("the Bundespräsident proposes {0} candidate; the Bundestag elects at the first ballot, {1} of the {2} a majority of the members needs - {3}", PoliSim.Wording.EnglishText.Possessive(proposal.Formateur, false, true), got, majority, Describe(government)));
                    return Expect(country, projection, government, verdict, Pf("THE BUNDESPRÄSIDENT'S CANDIDATE - ELECTED AT THE FIRST BALLOT, {0} VOTES, {1} NEEDED", got, majority));
                }
                projection.Reason = Pf("{0} candidate is elected at the first ballot, and no government can be drawn on its party", PoliSim.Wording.EnglishText.Possessive(proposal.Formateur, false, true));
            }
            else
            {
                projection.Reason = Pf("{0} candidate, the Bundespräsident's, takes {1} of the {2} a majority of the members needs at the first ballot - the fourteen days follow (Art. 63 Abs. 3), and the projection stops there", PoliSim.Wording.EnglishText.Possessive(proposal.Formateur, false, true), got, majority);
            }
            projection.Partial = true;
            projection.Steps.Add(projection.Reason);
            projection.View = NoneView(country, projection);
            return projection;
        }

        private FormationProjection ProjectSejm(Country country, DateTime pollingDay)
        {
            var projection = new FormationProjection { Country = country.Id, PollingDay = pollingDay, Procedure = "the Sejm's steps (Konstytucja Art. 162, 154)" };
            DateTime sitting = SejmFormation.FirstSittingAfter(country.Id, pollingDay);
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
            projection.Reason = "the Sejm elects no prime minister (Art. 154 ust. 3) - the outgoing cabinet carries on (Art. 162 ust. 3)";
            projection.Steps.Add(projection.Reason);
            return projection;
        }

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
