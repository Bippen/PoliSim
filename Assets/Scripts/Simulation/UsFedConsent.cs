using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-6 US-29 (`docs/specs/USA_STAGE_PLAN.md`; Elias's ruling F8 on R-US12 (a): "the four-year chair term (12 U.S.C. 242, read), the president nominating
    /// and the Senate confirming by majority (G9's precedents read)"), with Elias's rulings K15-K17 of 2026-10-23 (`docs/RULINGS_2026-10-23.md`):
    /// <b>THE SENATE'S CONSENT TO THE FED CHAIR, THE VICE CHAIR ACTING, AND THE NOMINATION BEFORE THE SENATE.</b> The term is AF-7's (`CentralBankTerms.Fed`,
    /// the statute's 4 years); this is the other half of the statute's words - the Chairman "designated by the President, by and with the advice and consent of
    /// the Senate" (12 U.S.C. 242, on disk under `ElectionsData/rules/raw/central_banks/`).
    ///
    /// <para><b>By majority</b> (`ElectionsData/usa/raw/fed/`, its fetch log): the Senate's precedent of 21 Nov 2013 - "the threshold for cloture on
    /// nominations, not including those to the Supreme Court of the United States, is now a majority" (Congressional Record, S8418) - so no cloture line stands
    /// before the Fed chair's confirmation, and the nominee is confirmed where more senators vote for than against, the Vice President deciding an equal
    /// division (`UsSenate.Carries`).</para>
    ///
    /// <para><b>The count: a crossing rule (K17), [AUTHORED-DRAFT] with the four roll calls of the majority-cloture era as its reading.</b> Every Fed-chair
    /// confirmation the Senate held by roll call was fetched (the fetch log names them: the Senate's own XML from 1996, Voteview's record before it); the
    /// rule is read on the ones held under the precedent the count already rests on (21 Nov 2013 onward - Yellen 2014, Powell 2018 and 2022, Warsh 2026),
    /// the votes as its reading because four are too few to call it a fit. The president's caucus votes for its nominee at the four votes' pooled share
    /// (<c>InPartyYeas</c> of <c>InPartyVoting</c>); the other caucus crosses at a share that falls as the president's majority widens - the least-squares
    /// line through the four votes' crossing shares against the president's caucus margin (its seats less the other caucus's), clamped to 0..1
    /// (<c>CrossingAtEvenSenate</c>, <c>CrossingPerSeatOfMargin</c>). Each senator is counted as seated, no one absent (DECLARED). `FedChairConsentDiagnostic`
    /// re-derives every constant from the saved roll calls, holds that the rule reproduces Yellen 2014's other party (against) and Powell 2018's (for), reads
    /// the six earlier roll calls out of sample, and says what the rule does to a hostile Senate (measured, never assumed). Nothing about the nominee enters
    /// the count (the lean included): no source on disk ties a senator's vote to it.</para>
    ///
    /// <para><b>Who nominates.</b> The president of the day (`Congress.PresidentOn`). A player who is the president (or acts as one) chooses from the
    /// candidates, and the choice is LODGED before the Senate (K16): the nomination is pending - the nominee, the nominating party, the day and the vote day,
    /// the record's lag after it (<c>NominationToVoteDays</c>: the median of the days between the Senate receiving each Fed-chair nomination of the
    /// 1996-2026 roll calls and its vote, from congress.gov's records and the Senate's XML, the diagnostic re-deriving it) - and the Senate votes on that day
    /// by this count (`Resolve`); a refusal is recorded (`Country.FedSenateVotes`) and plays as an event on the Docket and the bank page, and the
    /// player may nominate again. A player-president may nominate under any Senate (K16), hostile or not. An AI president names the AI's pick
    /// (`CentralBankTerms.AiNominee`), counted on the term's day and on every day after it until the Senate confirms (as built: the AI is read as having
    /// nominated ahead of the term's end, so its vote falls on the term's day - DECLARED). A counterfactual
    /// waits for the player's pick as the played world does (the K15-K17 review's H-1, `SimulationManager.FedAwaitsPlayersPick`); a nomination the player
    /// lodged LAPSES when the player leaves the presidency, the incoming AI president's pick seated at the oath, as nominations pending at a change of
    /// administration are returned (the delta review's H-A, the integrator's reading). K106 (Elias, 2026-10-24): the
    /// refusal path is KEPT AS BUILT - the record holds no refusal and the rule reaches none, so it never fires in play; the diagnostic and the film plant it.
    /// A null count is a defect, never a refusal (the review's M-2), and its retry is bounded (the delta review's L-A).
    /// DECLARED readings (the second delta review): a Fed term that ends on a turn's last day closes that turn with the Vice Chair acting in every world that waits for the player's pick (a world with no player, or where the player does
    /// not govern, seats the AI's pick on the term day itself), because the played world crosses the boundary before its hold rises (`GameController.Update`), and a counterfactual takes the AI's pick the next morning,
    /// dated the term day (H-B, which reverted a pre-close answer that made follower worlds part from the played one). The lapse at the role gate's closing
    /// needs the AI's confirmation, so a defect or a planted refusal on that day leaves the departed nomination to its vote day or the L-A release (I-A). The
    /// verb lodges before a term has ended only on a harness's path (the screens offer candidates at the term's end alone); a nomination lodged so and still
    /// pending when the gate closes is voted on its day, unless the term ends before that day, when it lapses to the AI's pick (I-C).</para>
    ///
    /// <para><b>The Vice Chair acts (K15) - the statute read, and where the record differs.</b> 12 U.S.C. 242 designates a Vice Chairman "who shall serve in
    /// the absence of the Chairman, as provided in section 244"; 12 U.S.C. 244 (saved byte-exact) says "At meetings of the Board the chairman shall preside,
    /// and, in his absence, the vice chairman shall preside. In the absence of the chairman and the vice chairman, the Board shall elect a member to act as
    /// chairman pro tempore." The game reads a chair's ended designation with no successor confirmed as the Chairman's absence, so the Vice Chair acts as
    /// chair (`Country.FedViceChair`, `Country.FedViceChairActing`) and the rate rule reads the Vice Chair's lean (`FederalReserveSystem.PolicyHead`) - the
    /// ruling's words. <b>The 2022 record is the statute's other case, not this one:</b> the Board named the outgoing chair Chair Pro Tempore from
    /// 5 Feb 2022 "pending Senate confirmation" (its press release of 4 Feb 2022), when the Vice Chair's seat was empty - the Vice Chair had resigned on
    /// 14 Jan 2022 (its release of 10 Jan 2022). That is the Board electing a member in the absence of both, and the member it elected was the outgoing chair.
    /// The game's Vice Chair is always seated, so the Board's election never arises (DECLARED); the outgoing chair keeps no office in the game. A refused AI
    /// nominee likewise leaves the Vice Chair acting.</para>
    ///
    /// <para><b>Which Senate.</b> The chamber the world seats (`Country.SecondChamberSeats` - the record's by date, and from the game's first class the game's,
    /// US-15 and US-16). An AI USA - the USA in every other country's start, and every world with no player - seats none (US-15's premise), so its consent is
    /// counted on the Senate of record by date (`UsCongressElection.SenateOn`) - R-US1 (a)'s "whatever the game does not elect is seated on its record's
    /// dates" (DECLARED).</para>
    /// </summary>
    public static class UsFedConsent
    {
        /// <summary>[AUTHORED-DRAFT] K17: the president's caucus's yeas and its senators voting, pooled over the four roll calls of the majority-cloture era
        /// (`FedChairConsentDiagnostic` re-derives both from the saved XML).</summary>
        public const int InPartyYeas = 186, InPartyVoting = 196;

        /// <summary>[AUTHORED-DRAFT] K17: the other caucus's crossing share at an even Senate, and its change per seat of the president's caucus margin - the
        /// least-squares line through the four votes (`FedChairConsentDiagnostic` re-derives both).</summary>
        public const double CrossingAtEvenSenate = 0.748131, CrossingPerSeatOfMargin = -0.062359;

        /// <summary>[FITTED] K16: the days from a nomination to the Senate's vote - the median over the Fed-chair nominations whose roll calls are saved with
        /// a congress.gov record (n = 7, `FedChairConsentDiagnostic` re-derives it).</summary>
        public const int NominationToVoteDays = 89;

        /// <summary>One day's count on the president's nominee.</summary>
        public sealed class Count
        {
            /// <summary>The president who nominates, as the game shows the office-holder, and the nominating party's key.</summary>
            public string President, PresidentsParty;
            public int For, Against;
            /// <summary>The seats of the nominating party's caucus and of the rest, as counted.</summary>
            public int OwnSeats, OtherSeats;
            /// <summary>The other caucus's crossing share the rule read for this Senate.</summary>
            public double Crossing;
            /// <summary>The Vice President's party where that vote decided an equal division, else null.</summary>
            public string VicePresidentParty;
            /// <summary>The Senate counted is the record's by date because the world seats none (an AI USA).</summary>
            public bool SenateOfRecord;
            public bool Confirms;
        }

        /// <summary>Whether the head of this country's bank needs the Senate's consent: the USA's (12 U.S.C. 242).</summary>
        public static bool Applies(Country country) => country != null && country.Id == CountryId.USA;

        /// <summary>The Senate that consents on <paramref name="on"/>: the world's seated chamber, or - where it seats none - the record's by date.</summary>
        public static IReadOnlyDictionary<string, int> SenateFor(Country country, DateTime on, out bool ofRecord)
        {
            ofRecord = Congress.Sworn(country.SecondChamberSeats) <= 0;
            return ofRecord ? UsCongressElection.SenateOn(country, on) : country.SecondChamberSeats;
        }

        /// <summary>K17: the president's caucus's share for its nominee.</summary>
        public static double InPartyShare => (double)InPartyYeas / InPartyVoting;

        /// <summary>K17: the other caucus's crossing share where the president's caucus holds <paramref name="margin"/> seats more than the rest (negative
        /// where it holds fewer), clamped to 0..1.</summary>
        public static double CrossingShare(int margin) => Math.Max(0.0, Math.Min(1.0, CrossingAtEvenSenate + CrossingPerSeatOfMargin * margin));

        /// <summary>K17: the rule's yeas on a Senate of <paramref name="own"/> seats of the nominating caucus and <paramref name="other"/> of the rest.</summary>
        public static int Yeas(int own, int other)
        {
            double yeas = InPartyShare * own + CrossingShare(own - other) * other;
            return Math.Max(0, Math.Min(own + other, (int)Math.Round(yeas, MidpointRounding.AwayFromZero)));
        }

        /// <summary>The count on the president of the day's nominee on <paramref name="on"/>; null where it cannot be held.</summary>
        public static Count Vote(Country country, DateTime on) => Vote(country, on, null);

        /// <summary>The count on <paramref name="on"/> on a nominee of <paramref name="nominatingParty"/> (null: the president of the day's party); null where
        /// it cannot be held - not the USA, no president found, or no Senate seated or on record that day (a caller treats null as no consent, and says so).</summary>
        public static Count Vote(Country country, DateTime on, string nominatingParty)
        {
            if (!Applies(country) || !Congress.PresidentOn(country, on, out string shownAs, out string party, out _) || string.IsNullOrEmpty(party)) { return null; }
            string side = string.IsNullOrEmpty(nominatingParty) ? party : nominatingParty;
            IReadOnlyDictionary<string, int> seats = SenateFor(country, on, out bool ofRecord);
            int sworn = Congress.Sworn(seats);
            if (sworn <= 0) { return null; }
            int own = seats.TryGetValue(side, out int n) ? n : 0;
            int yeas = Yeas(own, sworn - own);
            var count = new Count
            {
                President = shownAs, PresidentsParty = side, For = yeas, Against = sworn - yeas, OwnSeats = own, OtherSeats = sworn - own,
                Crossing = CrossingShare(own - (sworn - own)), SenateOfRecord = ofRecord,
            };
            bool tie = count.For == count.Against;
            string vice = tie ? UsSenate.VicePresidentParty(country, on) : null;
            count.VicePresidentParty = vice;
            count.Confirms = UsSenate.Carries(count.For, count.Against, tie && vice == side);
            return count;
        }

        /// <summary>K15: whether the Vice Chair acts as chair on <paramref name="on"/> - the Fed's chair's term ended with no successor confirmed, a Vice Chair seated.</summary>
        public static bool ViceChairActs(Country country, DateTime on) =>
            Applies(country) && country.FedViceChair != null && CentralBankTerms.Ended(country.CurrentFedChair, on);

        /// <summary>K16: lodges a nominee before the Senate on <paramref name="on"/>, the vote the record's lag later; returns the nomination.</summary>
        public static FedNomination Lodge(Country country, FedChair nominee, string nominatingParty, DateTime on)
        {
            var nomination = new FedNomination { Nominee = nominee, NominatingParty = nominatingParty, NominatedOn = on.Date, VoteOn = on.Date.AddDays(NominationToVoteDays) };
            country.FedNominationPending = nomination;
            return nomination;
        }

        /// <summary>
        /// K16: the Senate's vote on a pending nomination, by <paramref name="count"/> (the day's, `SimulationManager` holds it). Confirmed: the nominee is seated
        /// for the statute's full term from <paramref name="on"/> and the Vice Chair stops acting. Refused: the seat stays empty and the Vice Chair acts on. Either
        /// way the vote is recorded on the country and the nomination is cleared. A null count (no president or no Senate found) is a DEFECT, never a refusal (the
        /// K15-K17 review's M-2): nothing is recorded and null is returned; the manager logs it, holds the nomination one more day and then releases it (the delta review's L-A, the second delta review's L-F).
        /// </summary>
        public static FedSenateVote Resolve(Country country, FedNomination nomination, Count count, DateTime on)
        {
            if (count == null || nomination == null) { return null; }
            bool confirmed = count.Confirms;
            var vote = new FedSenateVote
            {
                Nominee = nomination.Nominee?.Name, NominatedOn = nomination.NominatedOn, VotedOn = on.Date,
                For = count.For, Against = count.Against, Confirmed = confirmed,
            };
            if (country.FedSenateVotes == null) { country.FedSenateVotes = new List<FedSenateVote>(); }
            country.FedSenateVotes.Add(vote);
            if (ReferenceEquals(country.FedNominationPending, nomination)) { country.FedNominationPending = null; }
            if (confirmed && nomination.Nominee != null)
            {
                country.CurrentFedChair = CentralBankTerms.Seat(nomination.Nominee, CentralBankTerms.Fed, on);
            }
            country.FedViceChairActing = ViceChairActs(country, on);
            return vote;
        }

        /// <summary>K16: the Senate's last vote on a player-president's nominee where it refused and nothing has been lodged or confirmed since, else null - the
        /// event the Docket and the bank page show. The review's L-1: only while the Vice Chair acts on <paramref name="today"/> - a chair seated since (the AI's,
        /// which records no vote) ends it.</summary>
        public static FedSenateVote StandingRefusal(Country country, DateTime today)
        {
            if (!ViceChairActs(country, today) || country.FedNominationPending != null || country.FedSenateVotes == null || country.FedSenateVotes.Count == 0) { return null; }
            FedSenateVote last = country.FedSenateVotes[country.FedSenateVotes.Count - 1];
            return last.Confirmed ? null : last;
        }

        private static string Day(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

        private static string Tally(int yeas, int nays) => yeas.ToString(CultureInfo.InvariantCulture) + "-" + nays.ToString(CultureInfo.InvariantCulture);

        /// <summary>The count in a page's capitals - "THE SENATE CONFIRMS, 68-32" or "THE SENATE REFUSES, 48-52", the Vice President named where that vote decides.</summary>
        public static string CountWords(Count count)
        {
            if (count == null) { return "NO SENATE COUNT CAN BE HELD"; }
            return (count.Confirms ? "THE SENATE CONFIRMS, " : "THE SENATE REFUSES, ") + Tally(count.For, count.Against)
                   + (count.VicePresidentParty != null ? " - THE VICE PRESIDENT DECIDES THE EQUAL DIVISION" : string.Empty)
                   + (count.SenateOfRecord ? " (THE SENATE OF RECORD)" : string.Empty);
        }

        /// <summary>The count as a sentence ends - "the Senate would confirm the nominee 68-32" - for the choice's own paragraph.</summary>
        public static string CountSentence(Count count)
        {
            if (count == null) { return "no Senate count can be held today"; }
            return "the Senate would " + (count.Confirms ? "confirm" : "refuse") + " the nominee " + Tally(count.For, count.Against)
                   + (count.VicePresidentParty != null ? ", the Vice President deciding the equal division" : string.Empty);
        }

        /// <summary>K15: who acts while the chair's term has ended - "Acting: the Vice Chair, Rowan Achterberg"; null where no one acts.</summary>
        public static string ActingWords(Country country, DateTime today) =>
            ViceChairActs(country, today) ? "Acting: the Vice Chair, " + country.FedViceChair.Name : null;

        /// <summary>K16: the pending nomination in a sentence - "Before the Senate: Ada Lindqvist, nominated 1 Feb 2030; the vote on 1 May 2030"; null where none.</summary>
        public static string PendingWords(Country country)
        {
            FedNomination n = Applies(country) ? country.FedNominationPending : null;
            return n == null ? null : "Before the Senate: " + (n.Nominee?.Name ?? "a nominee") + ", nominated " + Day(n.NominatedOn) + "; the vote on " + Day(n.VoteOn);
        }

        /// <summary>K16: a vote in a sentence - "The Senate refused Ada Lindqvist, 48-52, on 1 May 2030".</summary>
        public static string VoteWords(FedSenateVote vote) =>
            vote == null ? null : "The Senate " + (vote.Confirmed ? "confirmed " : "refused ") + (vote.Nominee ?? "the nominee") + ", " + Tally(vote.For, vote.Against) + ", on " + Day(vote.VotedOn);

        /// <summary>The Fed chair's term past its end with no successor confirmed, in a page's capitals - the Vice Chair acting (K15) and the nomination's state
        /// (K16); null where the term has not ended.</summary>
        public static string HoldoverWords(Country country, FedChair chair, DateTime today)
        {
            if (!Applies(country) || chair == null || !CentralBankTerms.Ended(chair, today)) { return null; }
            string acting = ActingWords(country, today);
            string pending = PendingWords(country);
            FedSenateVote refused = StandingRefusal(country, today);
            return "THE TERM ENDED " + Day(chair.TermEnds).ToUpperInvariant()
                   + (acting != null ? " · " + acting.ToUpperInvariant() : string.Empty)
                   + " · " + (pending != null ? pending.ToUpperInvariant() : refused != null ? VoteWords(refused).ToUpperInvariant() : "NO SUCCESSOR IS CONFIRMED");
        }
    }
}
