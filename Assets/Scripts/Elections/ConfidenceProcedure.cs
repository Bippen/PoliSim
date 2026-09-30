using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-3i (2026-09-25, §636; the political-system spec's §5.4): CONFIDENCE AND COLLAPSE AS DATA - each country's rules sourced from its constitution
    /// and dated. Sweden's, from Regeringsformen, quoted in `ElectionsData/sweden/confidence_rules.md`: a motion of no confidence is taken up when at
    /// least a tenth of the members move it and carries when MORE THAN HALF OF THE MEMBERS vote for it - 35 and 175 of 349 [RF-R:13:4] [RD-MF-2]; none
    /// against a caretaker, none between an election and the new Riksdag's first sitting [RF-R:13:4]; a carried motion discharges the prime minister
    /// unless the government orders an extra election within a week [RF-R:6:7], the prime minister's discharge discharges every minister [RF-R:6:9],
    /// and the discharged government serves on as a caretaker until the next takes office [RF-R:6:11]; the Speaker proposes a prime minister, who is
    /// approved unless more than half of the members vote against [RF-R:6:4]; four rejected proposals order an extra election within three months unless
    /// an ordinary election is due by then [RF-R:6:5]; the government orders no extra election within three months of a new Riksdag's first sitting,
    /// nor as a caretaker [RF-R:3:11]. The 2021 sequence is the precedent (confidence_rules.md): declared 21 June 2021, 181 of 349 [RD-VOT-0621]; the
    /// prime minister asked to be discharged on 28 June rather than dissolve; re-proposed and approved on 7 July, 173 against, below 175 [RD-VOT-0707].
    /// Every other country's rules are data when its stage lands; until then no motion is taken up there, and the reason says so.
    /// </summary>
    public static class ConfidenceProcedure
    {
        public enum Rules { Unsourced, Riksdag, Bundestag }

        /// <summary>
        /// §698 (PS-4): <b>the Bundestag's rule is the CONSTRUCTIVE vote</b> - Art. 67 GG, quoted from gesetze-im-internet.de in
        /// `ElectionsData/germany/records_by_date.md` §4 [GG-67]: *"Der Bundestag kann dem Bundeskanzler das Mißtrauen nur dadurch aussprechen, daß er mit
        /// der Mehrheit seiner Mitglieder einen Nachfolger wählt und den Bundespräsidenten ersucht, den Bundeskanzler zu entlassen. Der Bundespräsident muß
        /// dem Ersuchen entsprechen und den Gewählten ernennen."* No motion without a successor; it carries only by electing that successor with a majority
        /// of the members, and the successor is appointed at once - no week, no discharge, no caretaker, no round. (2): *"Zwischen dem Antrage und der Wahl
        /// müssen achtundvierzig Stunden liegen"* - a PREMISE here, stated: the game takes the election on the motion's day, the two days not waited (nothing
        /// the model reads moves a party's vote inside them). The Bundestag's rule on who may move it (its standing orders) is not on disk: any party may.
        /// </summary>
        public static Rules RulesOf(CountryId id) => id == CountryId.Sweden ? Rules.Riksdag : id == CountryId.Germany ? Rules.Bundestag : Rules.Unsourced;

        /// <summary>[RF-R:6:7]: the government may order an extra election within a week of the declaration, and then no discharge follows (how "a week" counts is on no page - seven days, the premise).</summary>
        public const int ExtraElectionWindowDays = 7;
        /// <summary>[RF-R:6:5] [RF-R:3:11]: an extra election is held within three months of its decision.</summary>
        public const int ExtraElectionMonths = 3;

        /// <summary>One motion's vote: every seated party's side with its reason, the yes-votes against the majority of the members.</summary>
        public sealed class MotionVote
        {
            public string Mover;
            public string PmParty;
            public int Members;
            public int Needed;
            public int For;
            public int Against;
            public int Abstaining;
            /// <summary>§698: a CONSTRUCTIVE vote (Art. 67 GG) - the successor's party (the mover) and the government it would lead, whose partners must accept it.</summary>
            public bool Constructive;
            public List<string> SuccessorCabinet = new List<string>();
            public List<string> SuccessorSupport = new List<string>();
            /// <summary>§698: the drafted parties that refuse the successor - its answer says no, or a sitting partner stays where it has posts; they do not
            /// vote for it, and a refusing SUPPORTER only withholds its votes, where a refusing CABINET partner sinks the successor (<see cref="PartnersAccept"/>).</summary>
            public List<string> Refusers = new List<string>();
            public bool PartnersAccept = true;
            public string Refusal;
            public bool Carried => For >= Needed && PartnersAccept;
            public List<DivisionSide> Sides = new List<DivisionSide>();

            public string Title() => Constructive
                ? string.Format(CultureInfo.InvariantCulture, "Constructive vote of no confidence in the chancellor ({0}): {1}'s candidate {2}, {3} of {4} members for, {5} needed (Art. 67 GG)",
                    PmParty, Mover, Carried ? "elected" : "not elected", For, Members, Needed)
                : string.Format(CultureInfo.InvariantCulture, "Motion of no confidence in the prime minister ({0}): {1}, {2} of {3} members for it, {4} needed",
                    PmParty, Carried ? "carried" : "not carried", For, Members, Needed);
        }

        /// <summary>
        /// §698: the constructive vote's election of the successor - the government <paramref name="proposal"/> the mover would lead, with every party's answer
        /// (<paramref name="verdict"/>, the investiture under the country's positive rule). The chancellor's own party votes against, whatever its lines; the
        /// proposal's cabinet and supporters elect the successor if its partners accept - a partner of the sitting government among them LEAVES it to do so,
        /// the one constructive vote the Bundestag has carried (1982: the FDP left the chancellor's coalition and elected his successor); the sitting
        /// government's other parties vote against; every other party votes as the investiture has it. Elected by a majority of the members (Art. 67 (1)).
        /// </summary>
        public static MotionVote ConstructiveVote(Country country, string mover, FormationProposal proposal, ProposalVerdict verdict)
        {
            GovernmentRecord government = country.Government;
            var vote = new MotionVote { Mover = mover, PmParty = government?.PmParty, Constructive = true };
            vote.SuccessorCabinet.AddRange(proposal.CabinetParties);
            vote.SuccessorSupport.AddRange(proposal.Supporters);
            // The review's defect 4: every drafted party by its own answer - a partner that refuses the successor does not vote for it.
            var accepts = new Dictionary<string, bool>();
            if (verdict != null) { foreach (PartyAnswer answer in verdict.Answers) { accepts[answer.Party] = answer.Accepts; } }
            // The review's defect 3: a partner of the sitting CABINET drafted into the successor weighs the posts it already holds - it leaves only for a
            // better place (the formation's payoff; a supporter's place buys no posts, so it is worth nothing), else it stays and refuses the successor.
            var stays = new List<string>();
            if (government != null)
            {
                var drafted = new List<string>(proposal.CabinetParties);
                drafted.AddRange(proposal.Supporters);
                foreach (string p in drafted)
                {
                    if (p == mover || p == government.PmParty || !government.Cabinet.Contains(p)) { continue; }
                    double here = GovernmentFormation.PayoffIn(country, government.Cabinet, p);
                    double there = GovernmentFormation.PayoffIn(country, proposal.CabinetParties, p);
                    if (there <= here + CoalitionFormation.DefectionMargin) { stays.Add(p); }
                }
            }
            var refusals = new List<string>();
            foreach (string p in proposal.CabinetParties) { if (p != mover && (stays.Contains(p) || (accepts.TryGetValue(p, out bool ok) && !ok))) { vote.Refusers.Add(p); } }
            foreach (string p in proposal.Supporters) { if (stays.Contains(p) || (accepts.TryGetValue(p, out bool ok) && !ok)) { vote.Refusers.Add(p); } }
            if (verdict != null) { foreach (PartyAnswer answer in verdict.Answers) { if (!answer.Accepts) { refusals.Add(answer.Party + " " + answer.Reason); } } }
            foreach (string p in stays) { refusals.Add(p + " stays in the government it sits in - the successor offers it no better place"); }
            // The successor's CABINET must hold together; a supporter is voluntary - a refusing supporter only withholds its votes (the count decides).
            bool cabinetHolds = true;
            foreach (string p in proposal.CabinetParties) { if (vote.Refusers.Contains(p)) { cabinetHolds = false; } }
            vote.PartnersAccept = verdict != null && verdict.Investiture != null && cabinetHolds;
            if (refusals.Count > 0 || !vote.PartnersAccept) { vote.Refusal = refusals.Count > 0 ? string.Join("; ", refusals) : verdict?.Reason ?? "no successor's investiture"; }
            IReadOnlyList<PoliticalParty> ordered = verdict?.Parties;
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                int seats = country.ParliamentSeats != null && country.ParliamentSeats.TryGetValue(party.Abbrev, out int held) ? held : 0;
                if (seats <= 0) { continue; }
                vote.Members += seats;
                int side; string reason;
                bool sitting = government != null && (government.Cabinet.Contains(party.Abbrev) || government.Support.Contains(party.Abbrev));
                bool draftedIn = proposal.CabinetParties.Contains(party.Abbrev) || proposal.Supporters.Contains(party.Abbrev);
                bool refuses = draftedIn && party.Abbrev != mover && vote.Refusers.Contains(party.Abbrev);
                if (government != null && party.Abbrev == government.PmParty) { side = -1; reason = "the chancellor's own party"; }
                else if (stays.Contains(party.Abbrev)) { side = -1; reason = "stays in the government it sits in - defends its chancellor"; }
                else if (draftedIn && !refuses && proposal.CabinetParties.Contains(party.Abbrev)) { side = 1; reason = party.Abbrev == mover ? "moved it - its candidate" : sitting ? "leaves the sitting government for the successor's cabinet" : "in the successor's cabinet"; }
                else if (draftedIn && !refuses) { side = 1; reason = sitting ? "leaves the sitting government to carry the successor's" : "carries the successor's government from outside"; }
                else if (sitting) { side = -1; reason = "the sitting government's - defends its chancellor"; }
                else if (refuses) { side = 0; reason = "refuses the successor's offer - abstains"; }   // the investiture would count it in the cabinet it declined
                else
                {
                    int at = -1;
                    if (ordered != null) { for (int p = 0; p < ordered.Count; p++) { if (ordered[p].Abbrev == party.Abbrev) { at = p; } } }
                    CoalitionFormation.InvestitureSide s = at >= 0 && verdict.Investiture != null ? verdict.Investiture.Sides[at] : CoalitionFormation.InvestitureSide.Abstains;
                    side = s == CoalitionFormation.InvestitureSide.Against ? -1 : s == CoalitionFormation.InvestitureSide.Abstains ? 0 : 1;
                    reason = at >= 0 && verdict.Investiture != null ? verdict.Investiture.Reasons[at] : "abstains";
                }
                if (side > 0) { vote.For += seats; } else if (side < 0) { vote.Against += seats; } else { vote.Abstaining += seats; }
                vote.Sides.Add(new DivisionSide { Abbrev = party.Abbrev, ShortName = party.ShortName, Seats = seats, Side = side, Alignment = side, Reason = reason });
            }
            vote.Needed = vote.Members / 2 + 1;
            return vote;
        }

        /// <summary>
        /// The motion's vote on the sitting government. The mover votes for it; the cabinet and its support vote against; a party red-lined from the
        /// cabinet by its own declarations votes for it (the investiture's opposition rule - a party that would vote a cabinet down votes no confidence
        /// in it); every other party abstains. Carried by more than half of all the members (13 kap. 4 §) - abstentions count against, as absent votes do.
        /// <paramref name="asOf"/> (§644): the declarations standing on a date - for measuring only (PS-3i-2c); the game passes none.
        /// </summary>
        public static MotionVote Vote(Country country, string mover, DateTime? asOf = null)
        {
            GovernmentRecord government = country.Government;
            var vote = new MotionVote { Mover = mover, PmParty = government?.PmParty };
            HashSet<string> redLined = government != null ? GovernmentFormation.RedLinedFrom(country, government.Cabinet, asOf) : new HashSet<string>();
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                int seats = country.ParliamentSeats != null && country.ParliamentSeats.TryGetValue(party.Abbrev, out int held) ? held : 0;
                if (seats <= 0) { continue; }
                vote.Members += seats;
                int side; string reason;
                if (party.Abbrev == mover) { side = 1; reason = "moved the motion"; }
                else if (government != null && government.Cabinet.Contains(party.Abbrev)) { side = -1; reason = "the government's own party"; }
                else if (government != null && government.Support.Contains(party.Abbrev)) { side = -1; reason = "carries the government from outside"; }
                else if (redLined.Contains(party.Abbrev)) { side = 1; reason = "a red line against a cabinet party"; }
                else { side = 0; reason = "abstains - no red line against the cabinet"; }
                if (side > 0) { vote.For += seats; } else if (side < 0) { vote.Against += seats; } else { vote.Abstaining += seats; }
                vote.Sides.Add(new DivisionSide { Abbrev = party.Abbrev, ShortName = party.ShortName, Seats = seats, Side = side, Alignment = side, Reason = reason });
            }
            vote.Needed = vote.Members / 2 + 1;
            return vote;
        }

        /// <summary>[RF-R:13:4]: at least a tenth of the members move a motion - the mover's own seats here (co-signers are not modelled, stated; 2021's was moved by 36 members of one party [RD-TRB-1]).</summary>
        public static bool CanBeTakenUp(Country country, string mover, out int moverSeats, out int tenth)
        {
            int members = 0;
            foreach (KeyValuePair<string, int> kv in country.ParliamentSeats) { members += kv.Value; }
            tenth = (members + 9) / 10;
            moverSeats = country.ParliamentSeats.TryGetValue(mover ?? string.Empty, out int held) ? held : 0;
            return moverSeats >= tenth;
        }

        /// <summary>The extra election's polling day: the Sunday on or before three months after the decision - the constitution gives the limit, the Sunday is the premise (Swedish elections are held on Sundays).</summary>
        public static DateTime ExtraElectionDay(DateTime decidedOn)
        {
            DateTime limit = decidedOn.Date.AddMonths(ExtraElectionMonths);
            return limit.AddDays(-(int)limit.DayOfWeek);
        }
    }
}
