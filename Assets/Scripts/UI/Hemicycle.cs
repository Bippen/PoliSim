using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §567 (2026-09-22, the sitting pass's Track 4, D8) — **THE CHAMBER'S GEOMETRY, STATED ONCE.**
    ///
    /// <para><b>The finding</b> (Design's whole-game reading, drift row D8): the game drew a hemicycle in three
    /// places and in three ways - Parliament's inked chamber (<see cref="HemicycleRenderer"/>), a bill card's
    /// FOR / UNDECIDED / AGAINST dots (<see cref="SeatMapRenderer"/>), and the signing document's painted vote
    /// (<c>CanvasPaint.SeatMap</c>) - *"one painter at two sizes"* being the answer. The three did not differ in
    /// ARITHMETIC: all three seat a chamber on rings of a half-disc, apportion the seats to the rings in
    /// proportion to each ring's radius, and give the outermost the rounding remainder. What differed was that
    /// each had written that down for itself, twice with its own copy of the same five constants.</para>
    ///
    /// <para><b>What lives here</b> is the arithmetic and the constants: how many rings a chamber needs at a
    /// given size (<see cref="Rings"/>), how many seats each ring takes (<see cref="Apportion"/>), and where each
    /// seat sits on the arc (<see cref="Lay"/>). What does NOT live here is paint: one caller draws IMGUI
    /// textures, one writes pixels into a <see cref="Texture2D"/>, and one cuts gaps at party and bloc
    /// boundaries before it draws (board 10d). A painter that tried to own all three targets would be a
    /// fourth way of doing it, which is the defect rather than the fix.</para>
    ///
    /// <para><b>Two sizes, and they are the two the boards name.</b> A chamber with a FIXED ring count is board
    /// 10d's - nine rings, the Parliament page and the desk's gallery; a chamber that GROWS its rings until the
    /// arc seats every mandate at the pitch is the card's, which has to fit a preview column. Both go through
    /// the same apportionment and the same lay.</para>
    /// </summary>
    public static class Hemicycle
    {
        /// <summary>The fewest rings a grown chamber uses, and the most - a card's map at 3, a 600-seat chamber in a small cell at 24.</summary>
        public const int MinRows = 3;

        /// <summary>See <see cref="MinRows"/>.</summary>
        public const int MaxRows = 24;

        /// <summary>A dot's diameter as a fraction of the ring gap: the dot is smaller than its ring's spacing, so rings read as rings.</summary>
        public const float DotFill = 0.62f;

        /// <summary>A seat's pitch along its ring, in dot diameters - a dot and a third, so neighbours touch at no width.</summary>
        public const float DotPitch = 1.35f;

        /// <summary>The innermost ring's radius as a fraction of the outermost's: the well the chamber curves around.</summary>
        public const float InnerRadiusFraction = 0.38f;

        /// <summary>
        /// The fewest rings between <see cref="MinRows"/> and <see cref="MaxRows"/> whose arcs together seat
        /// <paramref name="total"/> mandates at the pitch - the grown chamber's ring count. Also reports the ring
        /// <paramref name="gap"/> and the <paramref name="dot"/> diameter it settled on, because both are derived
        /// from the count and a caller that recomputed them could disagree with this one.
        /// </summary>
        public static int Rings(int total, float inner, float outer, out float gap, out float dot)
        {
            int rows = MinRows;
            while (true)
            {
                gap = (outer - inner) / (rows - 1);
                dot = gap * DotFill;
                int capacity = 0;
                for (int r = 0; r < rows; r++) { capacity += Mathf.FloorToInt(Mathf.PI * (inner + r * gap) / (dot * DotPitch)); }
                if (capacity >= total || rows >= MaxRows) { break; }
                rows++;
            }

            return rows;
        }

        /// <summary>
        /// Seats per ring, in proportion to each ring's radius, **the outermost taking the rounding remainder**.
        /// ⚠ Not a fixed pitch: board 10d's nine rings from r 110 to r 220 hold 345 seats at a literal 13.3 pitch
        /// and Sweden returns 349, so a fixed step would drop four mandates or need a tenth ring the board does
        /// not draw. The remainder rides the outermost ring, which has the most room for it.
        /// </summary>
        public static int[] Apportion(int total, int rings, float inner, float gap)
        {
            var perRow = new int[rings];
            if (rings <= 0) { return perRow; }

            float radiusSum = 0f;
            for (int r = 0; r < rings; r++) { radiusSum += inner + r * gap; }
            if (radiusSum <= 0f) { return perRow; }

            int assigned = 0;
            for (int r = 0; r < rings; r++)
            {
                perRow[r] = Mathf.RoundToInt(total * ((inner + r * gap) / radiusSum));
                assigned += perRow[r];
            }

            perRow[rings - 1] += total - assigned;
            if (perRow[rings - 1] < 0) { perRow[rings - 1] = 0; }
            return perRow;
        }

        /// <summary>One seat's place on the arc: which ring it is on, and where its centre falls.</summary>
        public readonly struct Seat
        {
            public readonly int Ring;
            public readonly Vector2 Centre;

            public Seat(int ring, Vector2 centre) { Ring = ring; Centre = centre; }
        }

        /// <summary>
        /// Every seat's place, ring by ring from the inside out and left to right along each ring - the plain lay,
        /// before any caller cuts gaps into it. <paramref name="baseline"/> is the half-disc's centre on its
        /// flat edge; y grows DOWNWARD, as it does in IMGUI (a painter writing pixels bottom-up flips it itself).
        /// </summary>
        public static void Lay(Vector2 baseline, int total, int[] perRow, float inner, float gap, List<Seat> into)
        {
            into.Clear();
            int laid = 0;
            for (int r = 0; r < perRow.Length && laid < total; r++)
            {
                int rowSeats = Mathf.Min(perRow[r], total - laid);
                float radius = inner + r * gap;
                for (int i = 0; i < rowSeats; i++)
                {
                    float angle = rowSeats == 1 ? 90f : 180f - (180f / (rowSeats - 1)) * i;
                    float rad = angle * Mathf.Deg2Rad;
                    into.Add(new Seat(r, baseline + new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad)) * radius));
                    laid++;
                }
            }
        }
    }
}
