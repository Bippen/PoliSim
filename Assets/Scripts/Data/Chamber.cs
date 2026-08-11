using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>How much of a chamber faces the voters at one election.</summary>
    public enum ChamberRenewal
    {
        /// <summary>Every seat, every time. Riksdag, Bundestag, Sejm, Camera, Assemblée nationale, US House.</summary>
        Whole,

        /// <summary>A third of the seats each cycle, on staggered classes. <b>US Senate</b> - the property that stops any one election from handing a president the whole chamber, and therefore one of the few structural brakes the American system has.</summary>
        StaggeredThirds,

        /// <summary>Half the seats each cycle. <b>French Sénat</b>, série by série, every three years.</summary>
        StaggeredHalves,

        /// <summary>Membership follows another body and changes when THAT body changes, with no election of its own. <b>German Bundesrat</b> - its composition moves with Land governments, so a state election a player never sees can flip a federal veto point.</summary>
        FollowsAnotherBody
    }

    /// <summary>
    /// One legislative chamber: its size, its term, how it is elected, and how much of it is renewed at
    /// a time.
    ///
    /// <para><b>Every field here was a hardcoded constant somewhere before.</b>
    /// <c>ParliamentConstants.TotalSeats</c> was 200 for all six countries - an arbitrary round number
    /// chosen for a clean hemicycle, per its own doc comment, and explicitly not modelled on any real
    /// chamber. The real sizes are 349, 400 + 200, 435 + 100, 460 + 100, 577 + 348 and 630, and the
    /// spread is not cosmetic: it sets how much a single seat is worth, which is what makes an Italian
    /// confidence vote feel different from a Swedish one.</para>
    /// </summary>
    public class Chamber
    {
        /// <summary>Official name in the country's own usage - "Riksdag", "House of Representatives", "Sejm". Displayed as-is; this game does not translate institutions into generic labels.</summary>
        public string Name;

        /// <summary>Total voting seats. Excludes Italy's senatori a vita, who are counted separately because they are appointed for life and never face an election.</summary>
        public int TotalSeats;

        /// <summary>
        /// Full term in days.
        ///
        /// <para>⚠ <b>Never express this by changing <c>ElectionSystem.ElectionCycle</c>.</b> That
        /// constant is a statement about how long a TURN is - <c>MacroSystem.YearsPerTurn</c> is derived
        /// from it as <c>4f / ElectionCycle</c> - and it only looks like a term length because a US
        /// presidential term happens to be four years. Modelling France's five-year term by touching it
        /// would silently rescale every macroeconomic rate in the game.</para>
        /// </summary>
        public int TermDays;

        /// <summary>How votes become seats. See <see cref="ElectoralFormula"/>; <c>IndirectlyElected</c> means this chamber has no election of its own.</summary>
        public ElectoralFormula Formula;

        /// <summary>The legal bar for winning any seats. <see cref="ThresholdRule.None"/> for single-member-district chambers, which have no list to bar anyone from.</summary>
        public ThresholdRule Threshold;

        /// <summary>How much of the chamber is renewed per election.</summary>
        public ChamberRenewal Renewal;

        /// <summary>
        /// Seats per class for a staggered chamber, in the order the classes face the voters. Null for
        /// <see cref="ChamberRenewal.Whole"/>.
        ///
        /// <para>The US Senate is {33, 33, 34} and the unevenness is real, not a rounding artefact - it
        /// is why "a third of the Senate" is never exactly a third and why some cycles are structurally
        /// worse for one party than others.</para>
        /// </summary>
        public int[] ClassSeats;

        /// <summary>Index into <see cref="ClassSeats"/> facing the voters at the next election. For the US Senate at <c>EpochDate</c> this is Class 2, which is up on 2026-11-03.</summary>
        public int NextClassUp;

        /// <summary>Single-member constituencies, where the system has them. 299 for Germany's first votes, 435 for the US House, 577 for France, 0 for a pure list system.</summary>
        public int Constituencies;

        /// <summary>National levelling seats that correct constituency-level disproportionality. 39 for Sweden - and they are why a single national Sainte-Laguë calculation reproduces the Riksdag exactly (measured 2026-08-11, 0 seats of error), while the same shortcut is off by 70 seats for Poland, which has none.</summary>
        public int LevellingSeats;

        /// <summary>Current seats per party id. The live composition, updated by elections and by <c>ParliamentSystem</c>'s between-election drift.</summary>
        public Dictionary<string, int> Seats = new Dictionary<string, int>();

        /// <summary>Sum of <see cref="Seats"/>. Should equal <see cref="TotalSeats"/> at all times; a divergence means an allocator dropped or invented a seat, and is worth failing on rather than tolerating.</summary>
        public int SeatsHeld
        {
            get
            {
                int total = 0;
                foreach (int seats in Seats.Values)
                {
                    total += seats;
                }

                return total;
            }
        }

        /// <summary>Seats needed for a bare majority. Integer division then +1, so 435 gives 218 and 100 gives 51 - both correct, and both wrong if computed as a rounded half.</summary>
        public int MajorityThreshold => TotalSeats / 2 + 1;
    }
}
