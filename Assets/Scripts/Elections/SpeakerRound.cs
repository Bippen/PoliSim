using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>§646: where a Speaker's round stands.</summary>
    public enum RoundStage
    {
        /// <summary>An AI party is asked; its proposal is tabled when the consultation ends.</summary>
        Consulting,
        /// <summary>The player's party is asked to form a government; the clock waits on the player.</summary>
        PlayerAsked,
        /// <summary>An AI party's proposal seats the player's party or asks its support: the offer waits on the player.</summary>
        OfferToPlayer,
        /// <summary>A proposal is tabled; the chamber votes on <see cref="SpeakerRound.VoteOn"/>.</summary>
        VotePending,
        Concluded,
    }

    /// <summary>
    /// §646 (the formateur, POLITICAL_SYSTEM_SPEC.md §5.3: premises 4-8 and the builder's R1-R8): THE SPEAKER'S ROUND, dated - who the Speaker asks,
    /// in the formation's order; the proposal on the table and the day the chamber votes on it; the proposals the chamber has rejected, to the sourced
    /// limit; the refusals that stand in it. It rides the outgoing government's record (saved, format 35), which serves as a caretaker throughout.
    /// </summary>
    public sealed class SpeakerRound
    {
        /// <summary>[AUTHORED-DRAFT] premise 7: the consultation before an AI party's proposal - the constitution sets no deadline for it.</summary>
        public const int ConsultationDays = 7;
        /// <summary>[RF-R:6:4] "Riksdagen ska inom fyra dagar ... pröva förslaget genom omröstning" - the vote on the last day allowed (R6).</summary>
        public const int VoteDays = 4;
        /// <summary>[RF-R:6:5] "Har riksdagen fyra gånger förkastat talmannens förslag, ska förfarandet avbrytas" - the proposals the chamber may reject.</summary>
        public const int ProposalLimit = 4;

        public DateTime OpenedOn;
        public string Occasion;
        /// <summary>The declarations the round reads - the election's that seated the chamber (§607, §636); a vintage-only reading where neither field below is set.</summary>
        public ElectionVintage Vintage;
        /// <summary>PS-3i-2c (§653): a round after an election reads everything dated at that election's polling day (this day); MinValue otherwise.</summary>
        public DateTime ReadsOn;
        /// <summary>PS-3i-2c (§653): a mid-term round (after a discharge) - the lines and candidacies standing on each day it reads, the platforms held to the sitting chamber's election.</summary>
        public bool MidTerm;
        /// <summary>Premise 6: the Speaker's order - the formation's prime-minister party, then the other parties with a declared candidate, largest first.</summary>
        public List<string> Order = new List<string>();
        public int Turn = -1;
        public string Asked;
        public DateTime AskedOn;
        public RoundStage Stage;
        /// <summary>The proposal on the table, or offered to the player.</summary>
        public FormationProposal Proposal;
        public DateTime VoteOn;
        public int Rejections;
        /// <summary>The refusals that stand in every proposal of the round and in the government it installs, as "PARTY>PM" (§641: until the next election).</summary>
        public List<string> Refusals = new List<string>();
        /// <summary>Premise 5: the player's declines of an offer, as "PLAYER>FORMATEUR" - they stand for the rest of this round only.</summary>
        public List<string> Declines = new List<string>();
        /// <summary>What happened, dated - the desk's record of the round.</summary>
        public List<string> Log = new List<string>();

        // -------- §705 (round 4 follow-up 4): THE BUNDESTAG'S CHANCELLOR ELECTION, Art. 63 GG - the same round, the Grundgesetz's phases --------

        /// <summary>[GG-39] Art. 39 Abs. 2: "Der Bundestag tritt spätestens am dreißigsten Tage nach der Wahl zusammen" - the latest day, the one the game
        /// takes (2025: polling day 23 February, the constituent sitting 25 March, exactly thirty days).</summary>
        public const int BundestagConvenesWithinDays = 30;
        /// <summary>[GG-63] Art. 63 Abs. 3: "Wird der Vorgeschlagene nicht gewählt, so kann der Bundestag binnen vierzehn Tagen nach dem Wahlgange mit
        /// mehr als der Hälfte seiner Mitglieder einen Bundeskanzler wählen."</summary>
        public const int BundestagSecondPhaseDays = 14;

        /// <summary>The Art. 63 phase: 0 or 1 - the Bundespräsident's candidate (Abs. 1-2); 2 - the fourteen days (Abs. 3); 3 - the ballot the most votes
        /// win (Abs. 4). Always 0 in the Riksdag's round.</summary>
        public int Phase;
        /// <summary>The day the new Bundestag convenes - the outgoing government's office ends with it (Art. 69 Abs. 2) and it serves on at the
        /// Bundespräsident's request (Abs. 3); no chancellor is elected before it. MinValue in the Riksdag's round.</summary>
        public DateTime Convenes;
        /// <summary>The last day of the fourteen after the first failed ballot (Art. 63 Abs. 3); MinValue before one fails.</summary>
        public DateTime SecondPhaseUntil;

        /// <summary>§705 (the review's second pass, defect 2): the player's party tabled a proposal in this round - only then does its candidate stand in
        /// the ballot the most votes win (Art. 63 Abs. 4); a party that passed or was never asked is not made a candidate by the game.</summary>
        public bool PlayerStood;

        /// <summary>§706 (the review's defect 1): the parties whose nomination has stood in the fourteen days (Art. 63 Abs. 3), or whose party
        /// passed there - each at most once, so a pass cannot re-ask the same party on the same day and hold the clock for good.</summary>
        public List<string> StoodInPhase2 = new List<string>();

        /// <summary>§714 (Elias's ruling of 2026-10-01, item 5): after an election, the day the formation's time on record runs out - the polling day plus
        /// <see cref="WorldClock.FormationDays"/>; no proposal comes to its vote before it, and the caretaker governs meanwhile. MinValue for a mid-term
        /// round, a country with no formation on record, and a round saved before §714 (it loads as it was).</summary>
        public DateTime FormationDue;

        /// <summary>§714 (the review's defect 1): the day a proposal tabled on <paramref name="today"/> comes to its vote - ONE rule, read by the tabling
        /// and by every line that tells the player the day: the Bundestag's not before it convenes nor before the formation's day (the same day once
        /// both have passed, "ohne Aussprache"); the Riksdag's on the fourth day after the Speaker submits it (RF 6 kap. 4 §), and not before the
        /// formation's day - the Speaker submits it when the formation's time has run.</summary>
        public DateTime VoteDayIfTabled(DateTime today)
        {
            DateTime vote = Bundestag ? (today > Convenes ? today : Convenes) : today.AddDays(VoteDays);
            return vote < FormationDue ? FormationDue : vote;
        }

        /// <summary>§705: the round is the Bundestag's chancellor election - it carries the day the new Bundestag convenes.</summary>
        public bool Bundestag => Convenes != DateTime.MinValue;

        public bool Open => Stage != RoundStage.Concluded;
    }
}
