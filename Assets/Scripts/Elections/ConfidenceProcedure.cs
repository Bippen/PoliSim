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
        public enum Rules { Unsourced, Riksdag }

        public static Rules RulesOf(CountryId id) => id == CountryId.Sweden ? Rules.Riksdag : Rules.Unsourced;

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
            public bool Carried => For >= Needed;
            public List<DivisionSide> Sides = new List<DivisionSide>();

            public string Title() => string.Format(CultureInfo.InvariantCulture, "Motion of no confidence in the prime minister ({0}): {1}, {2} of {3} members for it, {4} needed",
                PmParty, Carried ? "carried" : "not carried", For, Members, Needed);
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
