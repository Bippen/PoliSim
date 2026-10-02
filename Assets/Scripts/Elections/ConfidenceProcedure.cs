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
            /// <summary>§698: a CONSTRUCTIVE vote (Art. 67 GG) - the successor's party (the mover) and the government it would lead; whether its partners accept it decides, since §754, which government the elected successor forms.</summary>
            public bool Constructive;
            public List<string> SuccessorCabinet = new List<string>();
            public List<string> SuccessorSupport = new List<string>();
            /// <summary>§698: the drafted parties that refuse the successor - its answer says no, or a sitting partner stays where it has posts; they are not bound
            /// to elect it, and a refusing SUPPORTER only withholds its place, where a refusing CABINET partner means the drafted government does not hold
            /// (<see cref="PartnersAccept"/>) - since §754 that changes the government the elected successor forms, not his election.</summary>
            public List<string> Refusers = new List<string>();
            public bool PartnersAccept = true;
            public string Refusal;
            /// <summary>§754 (ruling A4): ELECTED by a majority of the members - the vote is on the person; whether the drafted government holds
            /// (<see cref="PartnersAccept"/>) decides which government the elected successor forms (<see cref="Governing"/>), never the election.</summary>
            public bool Carried => For >= Needed;
            /// <summary>§754: the government the successor forms if elected - the drafted one where it holds, else his party and its parliamentary group
            /// alone (the rule Art. 63 applies to an elected candidate whose government does not hold); empty where not projected.</summary>
            public List<string> Governing = new List<string>();
            /// <summary>§754: the player's party sits in that government only as its parliamentary group's partner (a group governs as one, the premise).</summary>
            public bool SeatedByGroup;
            public List<DivisionSide> Sides = new List<DivisionSide>();

            public string Title() => Constructive
                ? string.Format(CultureInfo.InvariantCulture, "Constructive vote of no confidence in the chancellor ({0}): {1}'s candidate {2}, {3} of {4} members for, {5} needed (Art. 67 GG)",
                    PmParty, Mover, Carried ? "elected" : "not elected", For, Members, Needed)
                : string.Format(CultureInfo.InvariantCulture, "Motion of no confidence in the prime minister ({0}): {1}, {2} of {3} members for it, {4} needed",
                    PmParty, Carried ? "carried" : "not carried", For, Members, Needed);
        }

        /// <summary>
        /// §698: the constructive vote's election of the successor - the government <paramref name="proposal"/> the mover would lead, with every party's answer
        /// (<paramref name="verdict"/>, the investiture under the country's positive rule). Its partners' answers decide whether that government HOLDS - a
        /// partner of the sitting government among them LEAVES it only for a better place (1982: the FDP left the chancellor's coalition and elected his
        /// successor), else it stays and refuses. §754 (Elias's ruling A4): the vote itself is on the PERSON, counted on <paramref name="chamber"/> (the
        /// round's chamber, its lines and compatibility) - elected by a majority of the members (Art. 67 (1)), whatever its partners answer; there is no
        /// separate vote against the incumbent.
        /// </summary>
        public static MotionVote ConstructiveVote(Country country, string mover, FormationProposal proposal, ProposalVerdict verdict,
            CoalitionFormation.Chamber chamber, IReadOnlyList<PoliticalParty> chamberParties)
        {
            GovernmentRecord government = country.Government;
            var vote = new MotionVote { Mover = mover, PmParty = government?.PmParty, Constructive = true };
            vote.SuccessorCabinet.AddRange(proposal.CabinetParties);
            vote.SuccessorSupport.AddRange(proposal.Supporters);
            // The review's defect 4: every drafted party by its own answer - a partner that refuses the successor is not bound to elect him (§754: it votes on the person like any party).
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
            // Whether the drafted government HOLDS: its CABINET together (a supporter is voluntary - a refusing one only withholds its place). §754: this decides the government the elected successor forms, never his election.
            bool cabinetHolds = true;
            foreach (string p in proposal.CabinetParties) { if (vote.Refusers.Contains(p)) { cabinetHolds = false; } }
            vote.PartnersAccept = verdict != null && verdict.Investiture != null && cabinetHolds;
            if (refusals.Count > 0 || !vote.PartnersAccept) { vote.Refusal = refusals.Count > 0 ? string.Join("; ", refusals) : verdict?.Reason ?? "no successor's investiture"; }
            // §754 (Elias's ruling A4, 2026-10-02): THE CONSTRUCTIVE VOTE IS A VOTE ON THE SUCCESSOR - "the Bundestag elects a new Chancellor by a
            // majority of its members. There is no separate vote against the incumbent." Each party votes on the person, as in Art. 63's ballots
            // (§713, the person ballot's tally): the mover's party for its candidate; a party bound by the successor's government (it accepted, its
            // cabinet holds, and it does not stay where it sits) for him - the coalition's word; a parliamentary group as one, the smaller member with
            // the larger; the sitting chancellor's party, and a sitting cabinet partner the successor does not seat better (it would lose its posts),
            // do not elect him; every other party votes sincerely - for the successor where it is NEARER him than the sitting chancellor he would
            // replace (the formation's compatibility) and does not refuse him, not for him where it is nearer the sitting chancellor, abstaining where
            // it refuses both. The sitting chancellor is no candidate and nothing is cast for or against him: a member who prefers him simply does not
            // elect the successor (the game's premise, stated: sincere votes; the ballot is secret, GO-BT § 4 Abs. 1, so party lines are a premise too).
            // Before §754 the other parties voted as the formation's investiture had it - on §713's (e5) chamber Art. 63 elected Merz with the SPD's
            // vote and an Art. 67 vote for the same Merz counted 208 (owed question B, §713).
            int Seats(string key) => country.ParliamentSeats != null && country.ParliamentSeats.TryGetValue(key ?? string.Empty, out int s) ? s : 0;
            string GroupPartner(string key)
            {
                foreach ((string a, string b) in ChamberRules.JointGroups(country.Id))
                {
                    string partner = a == key ? b : b == key ? a : null;
                    if (partner != null && Seats(partner) > 0 && Seats(key) > 0) { return partner; }
                }
                return null;
            }
            bool Smaller(string key) => GroupPartner(key) is string partner && (Seats(partner) > Seats(key) || (Seats(partner) == Seats(key) && string.CompareOrdinal(partner, key) < 0));
            int IndexOf(string key) { if (chamberParties != null) { for (int p = 0; p < chamberParties.Count; p++) { if (chamberParties[p].Abbrev == key) { return p; } } } return -1; }
            bool Refuses(int p, string candidate)
            {
                int ci = IndexOf(candidate);
                if (ci < 0) { return true; }
                int cp = GroupPartner(candidate) is string partnerOf ? IndexOf(partnerOf) : -1;
                foreach (RedLine line in chamber.Lines) { if (line.RefusesSupport(p, ci) || (cp >= 0 && line.RefusesSupport(p, cp))) { return true; } }
                return false;
            }
            string pm = government?.PmParty;
            var order = new List<PoliticalParty>();
            foreach (PoliticalParty party in PartySystems.For(country.Id)) { if (!Smaller(party.Abbrev)) { order.Add(party); } }
            foreach (PoliticalParty party in PartySystems.For(country.Id)) { if (Smaller(party.Abbrev)) { order.Add(party); } }
            var cast = new Dictionary<string, (int Side, string Reason)>();
            foreach (PoliticalParty party in order)
            {
                string key = party.Abbrev;
                if (Seats(key) <= 0) { continue; }
                bool draftedIn = proposal.CabinetParties.Contains(key) || proposal.Supporters.Contains(key);
                bool bound = draftedIn && key != mover && !vote.Refusers.Contains(key) && vote.PartnersAccept;
                bool sittingCabinet = government != null && government.Cabinet.Contains(key);
                int side; string reason;
                if (key == mover) { side = 1; reason = "moved it - its candidate"; }
                else if (Smaller(key) && GroupPartner(key) is string larger && cast.ContainsKey(larger)) { side = cast[larger].Side; reason = "votes with its parliamentary group, as one (" + larger + "'s side) - the game's premise: a group votes and governs as one"; }
                else if (key == pm) { side = -1; reason = "the sitting chancellor is its own - it does not elect his successor"; }
                else if (bound) { side = 1; reason = sittingCabinet ? "leaves the sitting government for the successor's - votes for its candidate (the coalition's word)" : "accepted the successor's government - votes for its candidate (the coalition's word)"; }
                // the review's defect 1: a sitting cabinet partner elects only where it is bound to a successor's government that HOLDS (above); drafted into one
                // that does not, the successor would govern without it (his party and its group alone) - it would vote out its own chancellor for no place
                else if (sittingCabinet) { side = -1; reason = !draftedIn || stays.Contains(key) ? "sits in the sitting cabinet and the successor offers it no better place - it does not elect him" : !vote.PartnersAccept ? "sits in the sitting cabinet, and the successor's drafted government does not hold - he would govern without it; it does not elect him" : "sits in the sitting cabinet and refuses the successor's offer - it does not elect him"; }
                else if (!party.HasPosition || IndexOf(key) < 0) { side = 0; reason = "holds no surveyed position to be near to - abstains (the game's premise)"; }
                else
                {
                    int p = IndexOf(key);
                    bool refusesSuccessor = Refuses(p, mover);
                    bool refusesChancellor = string.IsNullOrEmpty(pm) || Refuses(p, pm);
                    int mi = IndexOf(mover), ci = string.IsNullOrEmpty(pm) ? -1 : IndexOf(pm);
                    double toSuccessor = mi >= 0 ? chamber.Compatibility[p, mi] : double.NegativeInfinity;
                    double toChancellor = ci >= 0 ? chamber.Compatibility[p, ci] : double.NegativeInfinity;
                    if (refusesSuccessor && refusesChancellor) { side = 0; reason = "refuses both the successor and the sitting chancellor - abstains (the game's premise: sincere votes)"; }
                    else if (refusesSuccessor) { side = -1; reason = "refuses the successor - does not elect him (the game's premise: sincere votes)"; }
                    // the review's note: a successor whose party holds no surveyed position cannot be NEAR anyone (Art. 63's ballot excludes such a nominee from the
                    // sincere vote, §715) - it draws its own, bound and group votes only
                    else if (mi < 0 || !chamberParties[mi].HasPosition) { side = -1; reason = "the successor's party holds no surveyed position to be near to - does not elect him (the game's premise, as Art. 63's ballot)"; }
                    else if (refusesChancellor) { side = 1; reason = "refuses the sitting chancellor, not the successor - elects him (the game's premise: sincere votes)"; }
                    else if (toSuccessor > toChancellor) { side = 1; reason = "nearer the successor than the sitting chancellor - elects him (the game's premise: sincere votes)"; }
                    else { side = -1; reason = "nearer the sitting chancellor than his successor - does not elect him (the game's premise: sincere votes)"; }
                }
                cast[key] = (side, reason);
            }
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                if (!cast.TryGetValue(party.Abbrev, out (int Side, string Reason) c)) { continue; }
                int seats = Seats(party.Abbrev);
                vote.Members += seats;
                if (c.Side > 0) { vote.For += seats; } else if (c.Side < 0) { vote.Against += seats; } else { vote.Abstaining += seats; }
                vote.Sides.Add(new DivisionSide { Abbrev = party.Abbrev, ShortName = party.ShortName, Seats = seats, Side = c.Side, Alignment = c.Side, Reason = c.Reason });
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
