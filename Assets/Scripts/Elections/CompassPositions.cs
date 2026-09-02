using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Elections
{
    /// <summary>
    /// P2-3.2 (Playtest 2, 2026-09-02) — **the compass's points, every one traceable.** The compass plots
    /// CHES 2024 <c>lrecon</c> (economic left–right, 0 left … 10 right) against <c>galtan</c> (0 liberal /
    /// GAL … 10 conservative / TAN), the two scales F5 wired into <see cref="PoliticalParty"/> with their
    /// endpoints read from the codebook. A party sits at its published pair; a country at the seat-weighted
    /// mean of its chamber over the parties that hold seats and publish both values; a cabinet at the
    /// seat-weighted mean of its member parties. Nothing here is authored: a party without a published
    /// pair is left out of every mean and drawn nowhere (absence, never a centred stand-in — §36), and the
    /// counts of seats each mean rests on are returned with it so a screen can say so.
    /// OfficeTestDiagnostic re-derives every point with its own loop and holds the two equal.
    /// </summary>
    public static class CompassPositions
    {
        public const float ScaleMin = 0f;
        public const float ScaleMax = 10f;

        /// <summary>A position on the two CHES scales and the seats it rests on (1 for a party's own point).</summary>
        public readonly struct Point
        {
            public readonly float LrEcon;
            public readonly float Galtan;
            public readonly int Seats;
            public Point(float lrEcon, float galtan, int seats) { LrEcon = lrEcon; Galtan = galtan; Seats = seats; }
        }

        /// <summary>True when the party publishes both scales - the only parties any point here is built from.</summary>
        public static bool HasPair(PoliticalParty party) => !float.IsNaN(party.LrEcon) && !float.IsNaN(party.Galtan);

        /// <summary>The party's own published pair; null where either scale is absent.</summary>
        public static Point? Party(PoliticalParty party) => HasPair(party) ? new Point(party.LrEcon, party.Galtan, 1) : (Point?)null;

        /// <summary>
        /// The chamber's seat-weighted mean over the seated parties that publish both scales; null when no
        /// such party holds a seat. <paramref name="seatsLeftOut"/> reports the seats that could not count.
        /// </summary>
        public static Point? ChamberMean(Country country, out int seatsLeftOut)
        {
            return WeightedMean(country, null, out seatsLeftOut);
        }

        /// <summary>
        /// The sitting cabinet's seat-weighted mean over its member parties that publish both scales; null
        /// when no government is formed for this chamber (<see cref="GovernmentFormation.Cabinet"/>) or none
        /// of its members publish a pair.
        /// </summary>
        public static Point? CabinetMean(Country country, out int seatsLeftOut)
        {
            IReadOnlyList<string> cabinet = GovernmentFormation.Cabinet(country);
            if (cabinet == null || cabinet.Count == 0) { seatsLeftOut = 0; return null; }
            return WeightedMean(country, new HashSet<string>(cabinet), out seatsLeftOut);
        }

        private static Point? WeightedMean(Country country, HashSet<string> onlyAbbrevs, out int seatsLeftOut)
        {
            seatsLeftOut = 0;
            float lrSum = 0f, galSum = 0f;
            int seatSum = 0;
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                if (onlyAbbrevs != null && !onlyAbbrevs.Contains(party.Abbrev)) { continue; }
                int seats = country.ParliamentSeats.TryGetValue(party.Abbrev, out int s) ? s : 0;
                if (seats <= 0) { continue; }
                if (!HasPair(party)) { seatsLeftOut += seats; continue; }
                lrSum += party.LrEcon * seats;
                galSum += party.Galtan * seats;
                seatSum += seats;
            }
            if (seatSum <= 0) { return null; }
            return new Point(lrSum / seatSum, galSum / seatSum, seatSum);
        }
    }
}
