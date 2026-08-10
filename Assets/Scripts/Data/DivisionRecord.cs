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

        private int _lastNumber;

        /// <summary>Appends one division and evicts the oldest past <see cref="MaxEntries"/>. The caller supplies an alignment already captured at the vote rather than a bill, so this class never needs to know what a bill is.</summary>
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
}
