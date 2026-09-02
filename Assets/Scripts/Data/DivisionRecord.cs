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
        public int Axis;
        public List<DivisionSide> Sides = new List<DivisionSide>();
        /// <summary>P2-4.3: the estimated impact of the turn's decision this division belonged to (the preview's arrows,
        /// P2-2.1), attached by the controller when the division is queued for its ceremony; empty when no preview was
        /// held for that turn.</summary>
        public List<DivisionEffect> Effects = new List<DivisionEffect>();
    }

    /// <summary>
    /// A country's bounded history of resolved divisions.
    ///
    /// **WRITE-ONLY BY CONSTRUCTION, and this is the load-bearing property.** Nothing in
    /// <c>Assets/Scripts/Simulation/</c> may read <see cref="Entries"/> back. The moment a system reads
    /// it, the log stops being a record of the simulation and becomes an input to it - a hidden
    /// feedback path where past votes silently steer future ones, invisible in every trajectory and
    /// impossible to reason about from the code that appears to compute the outcome. It is written by
    /// <c>ParliamentSystem.RecordDivision</c> and read by the UI, and by nothing else.
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
        /// <summary>P2-4.3: the division with its direction, axis and every party's side recorded, for the ceremony's map.</summary>
        public void Append(string title, DateTime date, float alignment, bool passed, float direction, int axis, List<DivisionSide> sides)
        {
            Append(title, date, alignment, passed);
            DivisionRecord record = Entries[Entries.Count - 1];
            record.Direction = direction;
            record.Axis = axis;
            if (sides != null) { record.Sides.AddRange(sides); }
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
        public int Seats;
        public int Side;
    }

    /// <summary>P2-4.3: one arrow of the estimate that accompanied a division to its ceremony.</summary>
    [Serializable]
    public class DivisionEffect
    {
        public string Name;
        public float Value;
        public bool HigherIsBetter;
        public string Figure;
    }
}
