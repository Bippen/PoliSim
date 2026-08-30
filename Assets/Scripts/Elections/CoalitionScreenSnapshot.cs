using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-E8 — everything the coalition screen draws. PURE DATA (R-N2).
    ///
    /// **§36's line runs straight through this screen, and it is not the usual one.** A DECLARED
    /// red line is public — a party said it out loud, and the citation is on disk — so the screen
    /// may state it flatly. A DERIVED red line is this model's own reading of how far apart two
    /// parties stand, and stating it as a refusal would put a certainty in the player's hands that
    /// nobody in the world has uttered. So a derived line is shown as what it is: a DISTANCE, with
    /// the axis and the gap that produced it, and the reader can decide what to make of it.
    ///
    /// Everything here already exists in `CoalitionResult` — the ranked viable options, the
    /// arithmetic majorities a red line refused with each line's own basis string, the Banzhaf
    /// negotiating power, and the outcome. The screen computes nothing of its own, which is why it
    /// cannot disagree with the formation.
    /// </summary>
    public readonly struct CoalitionScreenSnapshot
    {
        public readonly string CountryName;
        public readonly string[] PartyNames;
        public readonly int[] Seats;
        public readonly int TotalSeats;
        public readonly int Majority;
        public readonly int PlayerPartyIndex;

        /// <summary>The formation, whole. The screen reads it and adds nothing.</summary>
        public readonly CoalitionResult Result;

        /// <summary>Every red line in play, so the screen can say which are DECLARED (public, quotable) and which are DERIVED (a distance, not a refusal anyone uttered).</summary>
        public readonly IReadOnlyList<RedLine> RedLines;

        public CoalitionScreenSnapshot(string countryName, string[] partyNames, int[] seats, int totalSeats,
            int majority, int playerPartyIndex, CoalitionResult result, IReadOnlyList<RedLine> redLines)
        {
            CountryName = countryName; PartyNames = partyNames; Seats = seats; TotalSeats = totalSeats;
            Majority = majority; PlayerPartyIndex = playerPartyIndex; Result = result;
            RedLines = redLines ?? new List<RedLine>();
        }

        /// <summary>The parties in a mask, as a joined label.</summary>
        public string Name(int mask)
        {
            var parts = new List<string>();
            for (int p = 0; p < PartyNames.Length; p++)
            {
                if ((mask & (1 << p)) != 0) { parts.Add(PartyNames[p]); }
            }

            return parts.Count == 0 ? "—" : string.Join("+", parts.ToArray());
        }

        /// <summary>Declared lines only - the ones a party said out loud and a citation records.</summary>
        public List<RedLine> Declared()
        {
            var declared = new List<RedLine>();
            foreach (RedLine line in RedLines) { if (line.Kind == RedLineKind.Declared) { declared.Add(line); } }
            return declared;
        }
    }
}
