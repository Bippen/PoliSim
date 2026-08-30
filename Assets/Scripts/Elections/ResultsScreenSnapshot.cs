using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-E7 — everything §30's results screen draws, plus W-D4's ledger for the "why". PURE DATA
    /// (R-N2).
    ///
    /// **Every figure here traces to the model or to a sourced file, and the ones that cannot are
    /// ABSENT rather than invented.** §30 asks for a demographic breakdown — young voters, older
    /// voters, urban, rural, income groups — and the electorate is ONE GROUP until W-F4
    /// (`ElectionDay.cs:28`, `CampaignRun.cs:91`, and five more sites say so). §0.4 forbids
    /// inventing demographics, so <see cref="DemographicsAvailable"/> is false and the screen draws
    /// that block as a stated absence. A results screen that split a single group into five
    /// plausible-looking rows would be the exact failure the rule exists to prevent.
    ///
    /// The comparison election is carried whole rather than as pre-computed deltas, so the screen
    /// can show the reader what it is comparing against instead of a bare arrow.
    /// </summary>
    public readonly struct ResultsScreenSnapshot
    {
        public readonly string CountryName;
        public readonly DateTime ElectionDate;
        public readonly string[] PartyNames;
        public readonly int PlayerPartyIndex;

        /// <summary>This election, as counted.</summary>
        public readonly long[] Votes;
        public readonly int[] Seats;
        public readonly long ValidVotes;
        public readonly double Turnout;
        public readonly long Eligible;
        public readonly int TotalSeats;

        /// <summary>The election being compared against - SOURCED, and named on the screen so a delta is never a bare arrow.</summary>
        public readonly string PreviousLabel;
        public readonly long[] PreviousVotes;
        public readonly int[] PreviousSeats;
        public readonly long PreviousValidVotes;

        /// <summary>§30's regional results: the per-constituency count this screen tabulates.</summary>
        public readonly string[] RegionNames;
        public readonly long[][] RegionVotes;
        public readonly long[] RegionValid;

        /// <summary>W-D4's ledger for the player's own party - the "why", every line a mechanism and the lines summing to the movement.</summary>
        public readonly VoteAttribution.Ledger Attribution;

        /// <summary>False until W-F4 gives the electorate more than one group. While false the screen draws §30's demographic block as ABSENT, never as five invented rows.</summary>
        public readonly bool DemographicsAvailable;

        public ResultsScreenSnapshot(string countryName, DateTime electionDate, string[] partyNames,
            int playerPartyIndex, long[] votes, int[] seats, long validVotes, double turnout, long eligible,
            int totalSeats, string previousLabel, long[] previousVotes, int[] previousSeats, long previousValidVotes,
            string[] regionNames, long[][] regionVotes, long[] regionValid,
            VoteAttribution.Ledger attribution, bool demographicsAvailable = false)
        {
            CountryName = countryName; ElectionDate = electionDate; PartyNames = partyNames;
            PlayerPartyIndex = playerPartyIndex; Votes = votes; Seats = seats; ValidVotes = validVotes;
            Turnout = turnout; Eligible = eligible; TotalSeats = totalSeats;
            PreviousLabel = previousLabel; PreviousVotes = previousVotes; PreviousSeats = previousSeats;
            PreviousValidVotes = previousValidVotes;
            RegionNames = regionNames; RegionVotes = regionVotes; RegionValid = regionValid;
            Attribution = attribution; DemographicsAvailable = demographicsAvailable;
        }

        public double Share(int party) => ValidVotes <= 0 ? 0.0 : (double)Votes[party] / ValidVotes;

        public double PreviousShare(int party) =>
            PreviousValidVotes <= 0 ? 0.0 : (double)PreviousVotes[party] / PreviousValidVotes;

        /// <summary>The party's movement in percentage points against the comparison election.</summary>
        public double SwingPp(int party) => (Share(party) - PreviousShare(party)) * 100.0;

        public int SeatChange(int party) => Seats[party] - PreviousSeats[party];

        /// <summary>The region's leader by counted votes, or -1 if it counted none.</summary>
        public int RegionLeader(int region)
        {
            int best = -1;
            for (int p = 0; p < PartyNames.Length; p++)
            {
                if (RegionVotes[region][p] <= 0) { continue; }
                if (best < 0 || RegionVotes[region][p] > RegionVotes[region][best]) { best = p; }
            }

            return best;
        }
    }
}
