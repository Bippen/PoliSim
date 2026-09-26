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
        /// <summary>The declarations the round reads - the election's that seated the chamber (§607, §636).</summary>
        public ElectionVintage Vintage;
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

        public bool Open => Stage != RoundStage.Concluded;
    }
}
