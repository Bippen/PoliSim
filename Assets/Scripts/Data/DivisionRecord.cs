using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// One resolved division: what Parliament decided, when, and how close it was.
    ///
    /// **This exists because the UI had nothing to draw.** The v2.0 Parliament screen prints a
    /// DIVISION RECORDS panel - number, title, date, alignment, verdict - and until 2026-08-10 not one
    /// of those five fields had anything behind it. <see cref="PoliSim.Simulation.ParliamentSystem"/>
    /// resolved every bill and then discarded the result: the eight Apply*BillResult methods mutate
    /// state and return, and no history was kept anywhere in the simulation or the UI.
    ///
    /// <para><b>Alignment is the seat-weighted lean, NOT a headcount.</b> There is no seats-based
    /// majority threshold anywhere in this model and no per-member vote - see
    /// <c>ParliamentSystem.GetSeatWeightedAlignment</c>'s own doc comment. The sign says which way the
    /// division went and the magnitude says how comfortably. A record printing "186 - 164" would be
    /// inventing a quantity that does not exist, which the Parliament card already shipped once.</para>
    /// </summary>
    [Serializable]
    public class DivisionRecord
    {
        /// <summary>Monotonic per country, and deliberately NOT the index into <see cref="DivisionLog.Entries"/> - numbering keeps counting as old entries are evicted, so "No. 214" stays the same division forever rather than renumbering itself when the buffer rolls.</summary>
        public int Number;

        public string Title;

        public DateTime Date;

        /// <summary>Captured at the vote, from the same <c>GetSeatWeightedAlignment</c> the live estimate visualises - so the record and the pre-vote projection can never disagree about what happened.</summary>
        public float Alignment;

        public bool Passed;
        /// <summary>P2-4.3 (2026-09-02): the bill's direction and axis at the division, and every party's side with its
        /// seats - what the ceremony's per-seat vote map draws. Empty on divisions recorded before this pass.</summary>
        public float Direction;
        /// <summary>PS-3i (§636): a MOTION (no confidence), not a bill - no signing ceremony, never the standing budget act. §761: also a statute's passage
        /// the President returned - its ceremony is the veto's own division, which follows it. PS-6 US-18: and a first chamber's passage the second chamber
        /// then voted - its ceremony is the second's, which carries it (<see cref="OtherChamber"/>).</summary>
        public bool Motion;
        /// <summary>§761: a QUALIFIED vote's line - the votes the motion needed, its base those voting (Poland's override of the President's veto:
        /// ceil(3/5 of those voting, abstentions in the base), Art. 122 ust. 5). 0 for an ordinary division, where the count decides. PS-6 US-18: on the
        /// Senate's division under Rule XXII (<see cref="Cloture"/>), the cloture line, its base the senators sworn (<see cref="Sworn"/>). PS-6 US-21: on a US chamber's vote on the president's veto (never at cloture), two thirds of those voting, its base every member of the division (<see cref="Sworn"/>).</summary>
        public int Required;
        public int Axis;
        public List<DivisionSide> Sides = new List<DivisionSide>();
        /// <summary>P2-4.3: the estimated impact of the turn's decision this division belonged to (the preview's arrows,
        /// P2-2.1), attached by the controller when the division is queued for its ceremony; empty when no preview was
        /// held for that turn.</summary>
        public List<DivisionEffect> Effects = new List<DivisionEffect>();
        /// <summary>PS-3f (§633, ruled): a BUDGET division is a contest of two proposals - each named with its votes, the abstentions, and the adopted one stamped; null for an ordinary for-or-against division.</summary>
        public DivisionContest Contest;

        // PS-6 US-18: A BILL THROUGH TWO CHAMBERS (the `Congress` class) - each chamber's division its own record. Every field below is additive, and its
        // default is exactly what a division recorded before US-18 was: held in the first (only) chamber, its bill begun there, no reconciliation, no cloture,
        // no second chamber - so a save of format 40 carries them unchanged and takes no step (US-28's precedent, §848).

        /// <summary>US-18: the chamber this division was held in - 0 the first chamber (the only one in every country with one; the USA's House), 1 the second
        /// (the Senate) - `Congress.House`, `Congress.Senate`.</summary>
        public int Chamber;
        /// <summary>US-18: THE ORIGIN - the chamber the bill began in, which votes first (and, at US-21, votes first on an override: the R-US17 block's "the
        /// ORIGIN chamber votes first"). Every bill begins in the House until US-19 builds who introduces where.</summary>
        public int Origin;
        /// <summary>US-18: the bill rode reconciliation (2 U.S.C. 641(e)) - the Senate's division a majority, no cloture; a BUDGET VOTE in the R-US17 block's
        /// terms, which draws no crossers (US-21).</summary>
        public bool Reconciliation;
        /// <summary>US-18: the Senate's division under Rule XXII - <see cref="Required"/> is its cloture line, three fifths of <see cref="Sworn"/>, rounded up;
        /// never a vote on a veto (whose line <see cref="Required"/> also carries where this is false).</summary>
        public bool Cloture;
        /// <summary>US-18: the senators duly chosen and sworn on the day - the cloture line's base; 0 on a first chamber's division. PS-6 US-21: on a chamber's vote on the president's veto, that vote's base - every member of the division.</summary>
        public int Sworn;
        /// <summary>US-18: where the Senate was equally divided on the majority path, the Vice President's party (Art. I §3 cl. 4) - null otherwise.</summary>
        public string VicePresidentParty;
        /// <summary>US-18: his vote there - +1 FOR, -1 AGAINST, 0 none cast (his party undecided, so the tie failed).</summary>
        public int VicePresidentVote;
        /// <summary>US-18: on the second chamber's division, the first chamber's on the same bill (marked a motion: this one carries the bill's ceremony, and the
        /// plate shows both) - PS-6 US-21: and on the second chamber's vote on a president's veto, the first chamber's vote; null on every other division. Saved by reference (the save's PreserveReferencesHandling), the log entry it was itself.</summary>
        public DivisionRecord OtherChamber;
    }

    /// <summary>PS-3f (§633): the two proposals of a budget division and how the chamber decided between them (Riksdagsordningen 11 kap. 10 § - the main proposal against the counter-proposal).</summary>
    public class DivisionContest
    {
        public string ProposalFor;
        public string ProposalAgainst;
        public int VotesFor;
        public int VotesAgainst;
        public int Abstentions;
        /// <summary>True where the counter-proposal (the alternative) was adopted.</summary>
        public bool AlternativeAdopted;
        /// <summary>The party whose alternative broke with the government it supports (a support party voting its own frames) - null where none did.</summary>
        public string BreakBy;
    }

    /// <summary>
    /// A country's bounded history of resolved divisions.
    ///
    /// **WRITE-ONLY BY CONSTRUCTION, and this is the load-bearing property.** Nothing in
    /// <c>Assets/Scripts/Simulation/</c> may read <see cref="Entries"/> back. The moment a system reads
    /// it, the log stops being a record of the simulation and becomes an input to it - a hidden
    /// feedback path where past votes silently steer future ones, invisible in every trajectory and
    /// impossible to reason about from the code that appears to compute the outcome. It is written where the
    /// chamber votes - <c>ParliamentSystem.RecordDivision</c> for a bill, <c>SimulationManager.ResolveGovernmentBudget</c> for the
    /// frame decision (a contest of two budgets, recorded passed either way: the chamber adopted a budget, the title says whose),
    /// and the motions', ballots' and the veto's own records beside it - and read by the UI, and by nothing else.
    ///
    /// <para><b>Kept again since Elias's ruling F5</b> ("the check reads every bill lost that day"). From P2-5.2 until F5,
    /// <c>CabinetSystem.UnderPressure</c> read the newest entry back, and since §773 a Polish budget records several acts on
    /// one day, so an act passed later could stand between a fall and the reader. The check now reads the country's own
    /// record of the day's loss (<c>Country.BillLostOn</c>, written by <c>ParliamentSystem.RecordBillLost</c>), and nothing in
    /// the simulation reads this log to decide anything - the lines that index the entry just appended only annotate it.</para>
    ///
    /// <para>Bounded on the same principle as <see cref="StatHistory"/>: a plain list with the oldest
    /// entry evicted past <see cref="MaxEntries"/>, rather than a true circular buffer. At this size a
    /// head index would buy nothing and would cost the natural oldest-to-newest ordering the UI
    /// wants.</para>
    /// </summary>
    [Serializable]
    public class DivisionLog
    {
        /// <summary>Small on purpose. The screen shows the most recent handful; this is "recent votes", not an archive, and an unbounded log on a 100+ turn game is a leak nobody would notice.</summary>
        public const int MaxEntries = 24;

        public readonly List<DivisionRecord> Entries = new List<DivisionRecord>();

        // ⚠ SAVE/LOAD (item 8, hazard 4): the numbering counter must survive a round trip - omitted,
        // numbering restarts at 1 while evicted entries keep their real numbers, and the signing
        // ceremony's high-water trigger (which compares against DivisionRecord.Number precisely
        // because the log evicts) misfires. [JsonProperty] because Json.NET skips private fields.
        [Newtonsoft.Json.JsonProperty] private int _lastNumber;

        /// <summary>Appends one division and evicts the oldest past <see cref="MaxEntries"/>. The caller supplies an alignment already captured at the vote rather than a bill, so this class never needs to know what a bill is.</summary>
        /// <summary>P2-4.3: the division with its direction, axis and every party's side recorded, for the ceremony's map.
        /// §761: returns the record it wrote - the writer's own vote, handed forward (the President's veto reads THIS division's sides); the log is still never read back.</summary>
        public DivisionRecord Append(string title, DateTime date, float alignment, bool passed, float direction, int axis, List<DivisionSide> sides)
        {
            Append(title, date, alignment, passed);
            DivisionRecord record = Entries[Entries.Count - 1];
            record.Direction = direction;
            record.Axis = axis;
            if (sides != null) { record.Sides.AddRange(sides); }
            return record;
        }

        public void Append(string title, DateTime date, float alignment, bool passed)
        {
            _lastNumber++;
            Entries.Add(new DivisionRecord
            {
                Number = _lastNumber,
                Title = title,
                Date = date,
                Alignment = alignment,
                Passed = passed
            });

            if (Entries.Count > MaxEntries)
            {
                Entries.RemoveAt(0);
            }
        }
    }

    /// <summary>P2-4.3: one party's side in a division - its seats and whether it stood FOR (+1), AGAINST (-1) or UNDECIDED (0).</summary>
    [Serializable]
    public class DivisionSide
    {
        public string Abbrev;
        /// <summary>§575: what the player READS - the election authority's own abbreviation (PoliticalParty.ShortName). The side is built from the
        /// party, never persisted, so a null here is only an older in-memory record and the drawer falls back to <see cref="Abbrev"/>.</summary>
        public string ShortName;
        public int Seats;
        public int Side;
        /// <summary>P3-A3 (2026-09-03): the stance model's alignment (−1 … +1) and the reasons that made it, as the vote was; null on a record that predates the model.</summary>
        public float Alignment;
        public string Reason;
        /// <summary>The same reasons in the plate's short form (`StanceModel.ReasonShort`).</summary>
        public string ReasonShort;
        /// <summary>PS-6 US-21 (R-US17's rule (c), the draw of defectors on the USA's veto path): this side is a party's DEFECTORS - the members the gate drew
        /// crossing to the other side of their party's stance, entered into a chamber's division only where they changed its verdict (`Congress.Divide`), beside
        /// the same party's side holding the rest of its seats. False on every other side and on every side saved before US-21: additive, no format step.</summary>
        public bool Defectors;
    }

    /// <summary>P2-4.3: one arrow of the estimate that accompanied a division to its ceremony.</summary>
    [Serializable]
    public class DivisionEffect
    {
        public string Name;
        public float Value;
        public bool HigherIsBetter;
        public string Figure;
        /// <summary>§725 (V35 rule 5): the outcome has no consensus direction - drawn in the neutral ink, <see cref="HigherIsBetter"/> unread. False on every effect saved before it.</summary>
        public bool Neutral;
    }
}
