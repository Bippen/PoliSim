using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// The live American election cycle: who holds each chamber, when the next election is, and the
    /// result waiting to be acknowledged.
    ///
    /// <para><b>This is the piece that turns the USA slice from an engine into a system.</b> Everything
    /// under it - <see cref="UnitedStatesElections"/>, <see cref="NationalVoteModel"/>,
    /// <see cref="UnitedStatesSeed"/> - is pure and stateless by design so it can be tested outside
    /// Unity. Something has to hold the state those functions transform, advance the calendar, and
    /// decide when an election happens. That is this class, and it is deliberately the only stateful
    /// part.</para>
    ///
    /// <para><b>The pending-result slot follows the existing idiom rather than inventing a fourth one.</b>
    /// Fed Chair selection, cabinet decisions, foreign-policy meetings and the budget process all use the
    /// same shape: a single slot that, while occupied, blocks the day loop until the player resolves it.
    /// An election night is exactly that kind of event - the player must see it before time moves on -
    /// so it uses the same pattern, and <c>GameController</c>'s existing pause banner can name it with
    /// the same machinery that names the other four.</para>
    ///
    /// <para>⚠ <b>KNOWN DUPLICATE, recorded rather than left to look like drift.</b> The old
    /// <see cref="ElectionSystem"/> still exists and still decides game-over on
    /// <c>ApprovalRating &gt;= 35</c> every fourth turn. Nothing here retires it, because doing so means
    /// surgery in <c>GameController</c>'s game-over path that cannot be validated without a live Editor.
    /// Two systems currently answer "did the player lose an election", and only this one is real. They
    /// must be reconciled in the same pass that adds the results screen - see the roadmap's Open
    /// Question 2 on what losing should even mean once coalitions exist.</para>
    /// </summary>
    public class UnitedStatesElectionCycle
    {
        public Chamber House { get; private set; }
        public Chamber Senate { get; private set; }
        public List<PoliticalParty> Parties { get; private set; }
        public List<ElectorateCohort> Cohorts { get; private set; }

        /// <summary>The party holding the presidency. The player IS the president; this is their party, chosen at country selection once Open Question 1 is settled, and seeded to the 2024 winner until then.</summary>
        public string PresidentPartyId { get; private set; }

        /// <summary>When the current presidential term began - the clock <see cref="VoteModelInputs.YearsInOffice"/> reads, and therefore the cost-of-ruling term.</summary>
        public DateTime TermStartDate { get; private set; }

        public DateTime NextElectionDate { get; private set; }

        /// <summary>True when the government holds neither chamber outright. Recomputed after every election; the state that gives the existing bill-gating its teeth.</summary>
        public bool DividedGovernment { get; private set; }

        /// <summary>
        /// The result of the election just held, waiting to be shown and acknowledged. Non-null blocks
        /// the day loop, exactly as a pending cabinet decision does.
        /// </summary>
        public UnitedStatesElectionResult PendingResult { get; private set; }

        /// <summary>Every election held, newest last. The record a results screen and the history graphs read.</summary>
        public List<UnitedStatesElectionResult> History { get; } = new List<UnitedStatesElectionResult>();

        /// <summary>
        /// Builds the cycle at <c>EpochDate</c> from <see cref="UnitedStatesSeed"/> - the real 119th
        /// Congress, the real parties, the modelled cohorts.
        /// </summary>
        public static UnitedStatesElectionCycle CreateSeeded()
        {
            var cycle = new UnitedStatesElectionCycle
            {
                House = UnitedStatesSeed.BuildHouse(),
                Senate = UnitedStatesSeed.BuildSenate(),
                Parties = UnitedStatesSeed.BuildParties(),
                Cohorts = UnitedStatesSeed.BuildCohorts(),
                PresidentPartyId = UnitedStatesSeed.Republican,
                TermStartDate = new DateTime(2025, 1, 20),
                NextElectionDate = UnitedStatesSeed.MidtermElectionDate
            };

            cycle.RecomputeDividedGovernment();
            return cycle;
        }

        /// <summary>
        /// Election day is the Tuesday after the first Monday in November, in even-numbered years.
        ///
        /// <para>Computed rather than tabulated, because a table of dates is a cached value with an
        /// expiry and this rule has not changed since 1845. Note it is NOT simply "the first Tuesday":
        /// when 1 November falls on a Tuesday the election is on the 8th, and a naive first-Tuesday rule
        /// is wrong roughly one year in seven.</para>
        /// </summary>
        public static DateTime ElectionDayFor(int year)
        {
            var november = new DateTime(year, 11, 1);
            int daysToMonday = ((int)DayOfWeek.Monday - (int)november.DayOfWeek + 7) % 7;
            return november.AddDays(daysToMonday + 1);
        }

        /// <summary>The next even-numbered-year election day strictly after <paramref name="after"/>.</summary>
        public static DateTime NextElectionDayAfter(DateTime after)
        {
            int year = after.Year;
            if (year % 2 != 0)
            {
                year++;
            }

            DateTime candidate = ElectionDayFor(year);
            return candidate > after ? candidate : ElectionDayFor(year + 2);
        }

        /// <summary>
        /// Advances one simulated day. Returns true on the day an election is held, leaving the result in
        /// <see cref="PendingResult"/> for the caller to surface.
        ///
        /// <para>Nothing happens on any other day, and nothing happens at all while a previous result is
        /// still unacknowledged - the same "resolve before advancing" guarantee the other three
        /// interrupts give, which is what stops two elections stacking up unseen if the player leaves the
        /// clock running.</para>
        /// </summary>
        public bool AdvanceDay(DateTime date, VoteModelInputs inputs)
        {
            if (PendingResult != null || date < NextElectionDate)
            {
                return false;
            }

            inputs.IncumbentPartyId = PresidentPartyId;
            inputs.YearsInOffice = (date - TermStartDate).TotalDays / 365.25;

            UnitedStatesElectionResult result = UnitedStatesElections.RunElection(
                date.Year, House, Senate, Parties, Cohorts, inputs);

            // Apply the result to the live chambers. Assigned rather than merged: a chamber's composition
            // after an election IS the result, and merging would let a stale party linger at zero seats.
            House.Seats = new Dictionary<string, int>(result.HouseSeats);
            Senate.Seats = new Dictionary<string, int>(result.SenateSeats);

            if (!result.WasMidterm && result.PresidentPartyId != PresidentPartyId)
            {
                PresidentPartyId = result.PresidentPartyId;
                // Inauguration is 20 January following, and the term clock starts there rather than on
                // election night - eleven weeks that matter for the cost-of-ruling term.
                TermStartDate = new DateTime(date.Year + 1, 1, 20);
            }

            RecomputeDividedGovernment();

            NextElectionDate = NextElectionDayAfter(date);
            PendingResult = result;
            History.Add(result);
            return true;
        }

        /// <summary>Clears the pending result once the player has seen it, releasing the day loop.</summary>
        public void AcknowledgePendingResult()
        {
            PendingResult = null;
        }

        /// <summary>Days until the next election - what a campaign window counts down, and what the UI shows.</summary>
        public int DaysUntilElection(DateTime from)
        {
            return Math.Max(0, (int)(NextElectionDate - from).TotalDays);
        }

        private void RecomputeDividedGovernment()
        {
            int house = House.Seats.TryGetValue(PresidentPartyId, out int h) ? h : 0;
            int senate = Senate.Seats.TryGetValue(PresidentPartyId, out int s) ? s : 0;
            DividedGovernment = house < House.MajorityThreshold || senate < Senate.MajorityThreshold;
        }
    }
}
