using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-5 with S7, part one (Elias's ruling of 2026-10-01: <i>"Start PS-5, Poland, together with S7, the two-round presidential model, as already
    /// ruled"</i>; the political-system spec's stage 5, the start-points spec's S7): THE TWO-ROUND PRESIDENTIAL ELECTION - the rule as sourced data
    /// and the count, proven on the four rounds of record (`ElectionsData/poland/presidential_returns.md`, the PKW's notices). The vote that feeds it
    /// (the candidates' shares from the parties' and the run-off's transfers) is part two; the president's veto and its override part three.
    /// </summary>
    public static class TwoRoundElection
    {
        /// <summary>A country's two-round rule. Poland's is the Konstytucja's Art. 127-128 [TK-KONST]; France's lands with S8.</summary>
        public sealed class Rule
        {
            /// <summary>Art. 127 ust. 4: the run-off is held on this day after the first vote ("czternastego dnia po pierwszym głosowaniu").</summary>
            public int RunOffDay;
            /// <summary>Art. 127 ust. 5: a withdrawal, a loss of electoral rights or a death among the two postpones the run-off by this many days more.</summary>
            public int WithdrawalPostponementDays;
            /// <summary>Art. 127 ust. 2: the term, in years ("na pięcioletnią kadencję").</summary>
            public int TermYears;
            /// <summary>Art. 127 ust. 2: the terms one person may serve ("może być ponownie wybrany tylko raz").</summary>
            public int MaxTerms;
            /// <summary>Art. 128 ust. 2: the Marshal of the Sejm orders the election for a day no earlier than this many days before the term ends ...</summary>
            public int CalledEarliestDaysBefore;
            /// <summary>... and no later than this many.</summary>
            public int CalledLatestDaysBefore;
            /// <summary>The article read, for every line that cites the rule.</summary>
            public string Basis;

            // §765 (Elias's ruling B5, 2026-10-02): WHO MAY STAND - "only voters' committees nominate; parties can't ... The gate is the signatures,
            // not a party." The figures as Kodeks wyborczy's consolidated text states them (`ElectionsData/poland/presidential_nomination.md`); an
            // independent stands by the same gate as a party's candidate, so the game's field is whoever clears it.
            /// <summary>Kodeks wyborczy art. 84 § 3: whether a party may nominate - in Poland's presidential election it may not; committees "mogą być
            /// tworzone wyłącznie przez wyborców".</summary>
            public bool PartiesNominate;
            /// <summary>Art. 90 § 1: the citizens who form a voters' committee ("w liczbie co najmniej 15").</summary>
            public int CommitteeFounders;
            /// <summary>Art. 299 § 1: the signatures the committee gathers before it is registered with the PKW - counted toward the nomination's.</summary>
            public int RegistrationSignatures;
            /// <summary>Art. 299 § 4: the registration's last day, in days before the election.</summary>
            public int RegistrationLatestDayBefore;
            /// <summary>Art. 296 and Konstytucja Art. 127 ust. 3: the signatures that nominate ("co najmniej 100 000 obywateli").</summary>
            public int NominationSignatures;
            /// <summary>Art. 303 § 1: the nomination's last day, in days before the election (at 16:00 - the consolidated text; the act as first published read the 45th day).</summary>
            public int NominationLatestDayBefore;
            /// <summary>The articles read for the nomination gate.</summary>
            public string NominationBasis;
        }

        private static readonly Rule Poland = new Rule
        {
            RunOffDay = 14, WithdrawalPostponementDays = 14, TermYears = 5, MaxTerms = 2, CalledEarliestDaysBefore = 100, CalledLatestDaysBefore = 75,
            Basis = "Konstytucja Art. 127 ust. 2, 4-6; Art. 128 ust. 1-2 [TK-KONST]",
            PartiesNominate = false, CommitteeFounders = 15, RegistrationSignatures = 1000, RegistrationLatestDayBefore = 55, NominationSignatures = 100000, NominationLatestDayBefore = 44,
            NominationBasis = "Kodeks wyborczy art. 84 § 3, art. 90 § 1, art. 296-299, art. 303 § 1 (the consolidated text, Dz.U. 2026 poz. 1261); Konstytucja Art. 127 ust. 3",
        };

        /// <summary>The country's two-round rule; null where no presidency is elected in two rounds or its rule is not yet sourced (France until S8).</summary>
        public static Rule RuleOf(CountryId id) => id == CountryId.Poland ? Poland : null;

        /// <summary>What a first vote decides: a president elected outright, or the two who meet in the run-off and its day.</summary>
        public sealed class FirstVote
        {
            public string ElectedOutright;
            public string[] RunOff;
            public DateTime RunOffOn;
            public string Line;
        }

        /// <summary>
        /// Art. 127 ust. 4-5: the candidate with MORE THAN HALF of the valid votes is elected; otherwise the two with the most votes meet on the
        /// run-off day - and where one of them withdraws, loses the right or dies (<paramref name="outOfRunOff"/>), the next-placed takes the place
        /// and the run-off moves by the postponement. Valid votes are the candidates' sum (the PKW's "głosy ważne", which they sum to exactly).
        /// A tie at a place the rule must break is not resolved here - the statute's lot is not modelled - and is reported, never guessed.
        /// </summary>
        public static FirstVote CountFirstVote(Rule rule, DateTime held, IReadOnlyList<(string Candidate, long Votes)> votes, ICollection<string> outOfRunOff = null)
        {
            long valid = 0;
            foreach ((string _, long v) in votes) { valid += v; }
            var ranked = new List<(string Candidate, long Votes)>(votes);
            ranked.Sort((a, b) => b.Votes.CompareTo(a.Votes));
            var result = new FirstVote();
            if (ranked.Count > 0 && ranked[0].Votes * 2 > valid)
            {
                result.ElectedOutright = ranked[0].Candidate;
                result.Line = string.Format(CultureInfo.InvariantCulture, "{0} elected in the first vote with {1:N0} of {2:N0} valid votes - more than half ({3})", ranked[0].Candidate, ranked[0].Votes, valid, rule.Basis);
                return result;
            }
            var pair = new List<string>();
            int postponed = 0;
            foreach ((string candidate, long _) in ranked)
            {
                if (pair.Count == 2) { break; }
                if (outOfRunOff != null && outOfRunOff.Contains(candidate)) { postponed = rule.WithdrawalPostponementDays; continue; }
                pair.Add(candidate);
            }
            if (pair.Count == 2 && ranked.Count > 2)
            {
                long second = 0, third = 0; int seen = 0;
                foreach ((string candidate, long v) in ranked) { if (outOfRunOff != null && outOfRunOff.Contains(candidate)) { continue; } seen++; if (seen == 2) { second = v; } else if (seen == 3) { third = v; break; } }
                if (second == third) { result.Line = "a tie for the run-off's second place - the statute's resolution is not modelled; nothing is decided"; return result; }
            }
            result.RunOff = pair.ToArray();
            result.RunOffOn = held.AddDays(rule.RunOffDay + postponed);
            result.Line = pair.Count == 2
                ? string.Format(CultureInfo.InvariantCulture, "no candidate has more than half of {0:N0} valid votes - {1} and {2} meet in the run-off on {3:yyyy-MM-dd}{4} ({5})", valid, pair[0], pair[1], result.RunOffOn, postponed > 0 ? ", postponed " + postponed.ToString(CultureInfo.InvariantCulture) + " days by a withdrawal" : string.Empty, rule.Basis)
                : "fewer than two candidates for the run-off";
            return result;
        }

        /// <summary>Art. 127 ust. 6: the run-off's candidate with MORE votes is elected; null on a tie (the statute's resolution is not modelled).</summary>
        public static string CountRunOff(IReadOnlyList<(string Candidate, long Votes)> votes)
        {
            if (votes == null || votes.Count != 2 || votes[0].Votes == votes[1].Votes) { return null; }
            return votes[0].Votes > votes[1].Votes ? votes[0].Candidate : votes[1].Candidate;
        }

        /// <summary>Art. 128 ust. 1-2: a term begins on the day the president takes office and runs <see cref="Rule.TermYears"/>; the election falls in
        /// the window the Marshal orders it for - from <see cref="Rule.CalledEarliestDaysBefore"/> to <see cref="Rule.CalledLatestDaysBefore"/> days before the term ends.</summary>
        public static (DateTime TermEnds, DateTime WindowOpens, DateTime WindowCloses) TermOf(Rule rule, DateTime tookOffice)
        {
            DateTime ends = tookOffice.AddYears(rule.TermYears);
            return (ends, ends.AddDays(-rule.CalledEarliestDaysBefore), ends.AddDays(-rule.CalledLatestDaysBefore));
        }

        /// <summary>A round of record - the candidates' votes as the PKW returned them (`Generated.PolishPresidentialReturns`) and the day it was held;
        /// empty where the record holds no such round.</summary>
        public static IReadOnlyList<(string Candidate, long Votes)> RoundOfRecord(CountryId id, int year, int round, out DateTime held)
        {
            held = DateTime.MinValue;
            var votes = new List<(string Candidate, long Votes)>();
            if (id != CountryId.Poland) { return votes; }
            foreach (var r in Generated.PolishPresidentialReturns.Rounds) { if (r.Year == year && r.Round == round) { held = new DateTime(r.Year, r.Month, r.Day); } }
            foreach (var v in Generated.PolishPresidentialReturns.Votes) { if (v.Year == year && v.Round == round) { votes.Add((v.Candidate, v.Votes)); } }
            return votes;
        }

        /// <summary>
        /// §720: an election of record as the rule counts it, in one line - the first vote's verdict (an outright winner, or the two and the run-off's
        /// day), the run-off's winner, and the winner's term with the window its successor's election falls in. What the start card of a presidential
        /// election says of it, read through the rule rather than typed; null where the record or the rule is missing.
        /// </summary>
        public static string RecordBrief(CountryId id, int year)
        {
            Rule rule = RuleOf(id);
            IReadOnlyList<(string Candidate, long Votes)> first = RoundOfRecord(id, year, 1, out DateTime firstHeld);
            if (rule == null || first.Count == 0) { return null; }
            FirstVote counted = CountFirstVote(rule, firstHeld, first);
            string elected = counted.ElectedOutright ?? CountRunOff(RoundOfRecord(id, year, 2, out _));
            if (elected == null) { return counted.Line; }
            DateTime tookOffice = DateTime.MinValue;
            foreach (PresidencyOfRecord.President p in PresidencyOfRecord.Of(id)) { if (p.FirstVote == firstHeld) { tookOffice = p.TookOffice; } }
            if (tookOffice == DateTime.MinValue) { return counted.Line + "; " + elected + " elected"; }
            (DateTime ends, DateTime opens, DateTime closes) = TermOf(rule, tookOffice);
            return string.Format(CultureInfo.InvariantCulture, "{0}; {1} elected, in office {2:yyyy-MM-dd} to {3:yyyy-MM-dd}, the successor's election ordered for {4:yyyy-MM-dd} - {5:yyyy-MM-dd}",
                counted.Line, elected, tookOffice, ends, opens, closes);
        }
    }
}
