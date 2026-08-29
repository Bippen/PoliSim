using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-E2 / SPEC §25 (swing regions) with §24's regional frame and §36's gate — the model half
    /// of the campaign map. PURE FUNCTIONS AND DATA, WIRED TO NOTHING (R-N2).
    ///
    /// **A region's reading is a <see cref="Poll"/> of that region, or nothing.** §36 says swing
    /// detail is hidden until the player invests in polling; the gate is therefore not a blur or
    /// a rounding but ABSENCE — an unbought region carries no shares, no leader, no index, and the
    /// map draws it as unknown. A bought region carries the polled shares with the ± its sample
    /// size earned, and everything else on the reading is DERIVED from those two things:
    /// - the leader and the runner-up, by polled share;
    /// - the gap between them, in percentage points, and whether that gap is inside its own
    ///   sampling error (`TooCloseToCall` — the honest reading of a 40.5 / 39.8 on a small sample);
    /// - §25's swing index: `100 × max(0, 1 − gap / FullScaleGapPp)` — 100 at a dead heat, 0 at
    ///   a lead of <see cref="FullScaleGapPp"/> or more. **[AUTHORED-DRAFT]** `FullScaleGapPp = 20`,
    ///   the lead at which a region stops being worth contesting; strikeable.
    ///
    /// The map never sees a true regional vector: the harness stages real per-valkrets returns as
    /// the truth and polls them with `PollingSystem.Conduct`, exactly as Campaign HQ's national
    /// race is polled. The player's own party is only a highlight; the index is about the race.
    /// </summary>
    public static class SwingRegions
    {
        public const double FullScaleGapPp = 20.0;

        /// <summary>§25's index from a lead in percentage points: 100 at a tie, 0 at FullScaleGapPp or more.</summary>
        public static double Index(double gapPp)
        {
            if (gapPp < 0) { gapPp = -gapPp; }
            double index = 100.0 * (1.0 - gapPp / FullScaleGapPp);
            return index < 0 ? 0 : (index > 100 ? 100 : index);
        }

        /// <summary>A region's reading from its poll: leader, runner-up, gap, whether the gap is inside its own error, the index.</summary>
        public static MapRegionReading FromPoll(string name, double weight, Poll poll)
        {
            int leader = -1, runnerUp = -1;
            for (int p = 0; p < poll.PartyCount; p++)
            {
                if (leader < 0 || poll.Share(p) > poll.Share(leader)) { runnerUp = leader; leader = p; }
                else if (runnerUp < 0 || poll.Share(p) > poll.Share(runnerUp)) { runnerUp = p; }
            }

            double gapPp = runnerUp >= 0 ? 100.0 * (poll.Share(leader) - poll.Share(runnerUp)) : 100.0;
            // The gap's own uncertainty: the two shares' sampling errors, combined.
            double gapErrorPp = runnerUp >= 0
                ? Math.Sqrt(poll.MarginOfErrorPp(leader) * poll.MarginOfErrorPp(leader) + poll.MarginOfErrorPp(runnerUp) * poll.MarginOfErrorPp(runnerUp))
                : 0.0;
            return new MapRegionReading(name, weight, poll, leader, runnerUp, gapPp, gapErrorPp, Index(gapPp));
        }

        public static MapRegionReading Unknown(string name, double weight) => new MapRegionReading(name, weight);
    }

    /// <summary>What the map knows about one valkrets: a poll of it, or nothing (§36).</summary>
    public readonly struct MapRegionReading
    {
        public readonly string Name;
        /// <summary>The region's share of the national electorate — a public fact, drawn whether or not the region is polled.</summary>
        public readonly double Weight;
        public readonly bool Measured;
        public readonly Poll Poll;
        public readonly int Leader;
        public readonly int RunnerUp;
        public readonly double GapPp;
        public readonly double GapErrorPp;
        public readonly double SwingIndex;

        public MapRegionReading(string name, double weight, Poll poll, int leader, int runnerUp, double gapPp, double gapErrorPp, double swingIndex)
        {
            Name = name; Weight = weight; Measured = true; Poll = poll; Leader = leader; RunnerUp = runnerUp;
            GapPp = gapPp; GapErrorPp = gapErrorPp; SwingIndex = swingIndex;
        }

        public MapRegionReading(string name, double weight)
        {
            Name = name; Weight = weight; Measured = false; Poll = default; Leader = -1; RunnerUp = -1;
            GapPp = double.NaN; GapErrorPp = double.NaN; SwingIndex = double.NaN;
        }

        /// <summary>The lead is inside its own sampling error — the reading cannot say who leads.</summary>
        public bool TooCloseToCall => Measured && GapPp <= GapErrorPp;
    }

    /// <summary>One tile of the cartogram: where a valkrets sits on the grid and how it is captioned. Layout only — [AUTHORED-DRAFT], no geography claimed.</summary>
    public readonly struct MapTile
    {
        public readonly string Name;
        public readonly string Caption;
        public readonly int Column;
        public readonly int Row;

        public MapTile(string name, string caption, int column, int row)
        {
            Name = name; Caption = caption; Column = column; Row = row;
        }
    }

    /// <summary>
    /// W-E2 — everything the campaign map draws. PURE DATA (R-N2); contains a
    /// <see cref="CampaignSnapshot"/> so the masthead, the money and the days are the same values
    /// the other campaign screens show.
    /// </summary>
    public readonly struct CampaignMapSnapshot
    {
        public readonly CampaignSnapshot Campaign;
        public readonly MapRegionReading[] Regions;
        public readonly MapTile[] Layout;
        public readonly string[] PartyNames;
        public readonly int PlayerPartyIndex;
        /// <summary>What was bought: "" when nothing, else the offer's name; and the per-region sample it gave.</summary>
        public readonly string PollingBought;
        public readonly int SamplePerRegion;
        public readonly DateTime FieldDate;
        /// <summary>The two offers that would sharpen this sheet, named with their prices (W-E4's ladder), so the gate points at its key.</summary>
        public readonly string OfferLine;

        public CampaignMapSnapshot(CampaignSnapshot campaign, MapRegionReading[] regions, MapTile[] layout, string[] partyNames,
            int playerPartyIndex, string pollingBought, int samplePerRegion, DateTime fieldDate, string offerLine)
        {
            Campaign = campaign; Regions = regions; Layout = layout; PartyNames = partyNames; PlayerPartyIndex = playerPartyIndex;
            PollingBought = pollingBought; SamplePerRegion = samplePerRegion; FieldDate = fieldDate; OfferLine = offerLine;
        }

        public int MeasuredCount
        {
            get { int n = 0; foreach (MapRegionReading r in Regions) { if (r.Measured) { n++; } } return n; }
        }

        /// <summary>Measured regions by swing index, highest first — the ledger's order.</summary>
        public List<int> BySwing()
        {
            MapRegionReading[] regions = Regions;   // a local: a lambda inside a struct cannot capture `this`
            var order = new List<int>();
            for (int i = 0; i < regions.Length; i++) { if (regions[i].Measured) { order.Add(i); } }
            order.Sort((a, b) => regions[b].SwingIndex.CompareTo(regions[a].SwingIndex));
            return order;
        }
    }

    /// <summary>
    /// [AUTHORED-DRAFT] the cartogram: Sweden's 29 valkretsar on a 5 × 10 grid, north at the top,
    /// laid by hand to read as the country's spine (Norrbotten alone at the top, the two Stockholm
    /// valkretsar and Gotland to the east, Göteborg and the Västra Götaland four to the west, the
    /// Skåne four and Malmö at the foot). Positions are a reading aid, not geography, and no
    /// border is drawn. Names spelled as Valmyndigheten spells them (the 2018 file's keys).
    /// </summary>
    public static class SwedenCartogram
    {
        public static MapTile[] Layout() => new[]
        {
            new MapTile("Norrbottens län", "NORRBOTTEN", 3, 0),
            new MapTile("Västerbottens län", "VÄSTERBOTTEN", 3, 1),
            new MapTile("Jämtlands län", "JÄMTLAND", 2, 2),
            new MapTile("Västernorrlands län", "VÄSTERNORRLAND", 3, 2),
            new MapTile("Dalarnas län", "DALARNA", 1, 3),
            new MapTile("Gävleborgs län", "GÄVLEBORG", 2, 3),
            new MapTile("Värmlands län", "VÄRMLAND", 0, 4),
            new MapTile("Örebro län", "ÖREBRO", 1, 4),
            new MapTile("Västmanlands län", "VÄSTMANLAND", 2, 4),
            new MapTile("Uppsala län", "UPPSALA", 3, 4),
            new MapTile("Västra Götalands läns norra", "V. GÖTALAND N", 0, 5),
            new MapTile("Södermanlands län", "SÖDERMANLAND", 2, 5),
            new MapTile("Stockholms län", "STOCKHOLMS LÄN", 3, 5),
            new MapTile("Stockholms kommun", "STOCKHOLM", 4, 5),
            new MapTile("Göteborgs kommun", "GÖTEBORG", 0, 6),
            new MapTile("Västra Götalands läns västra", "V. GÖTALAND V", 1, 6),
            new MapTile("Västra Götalands läns östra", "V. GÖTALAND Ö", 2, 6),
            new MapTile("Östergötlands län", "ÖSTERGÖTLAND", 3, 6),
            new MapTile("Gotlands län", "GOTLAND", 4, 6),
            new MapTile("Hallands län", "HALLAND", 0, 7),
            new MapTile("Västra Götalands läns södra", "V. GÖTALAND S", 1, 7),
            new MapTile("Jönköpings län", "JÖNKÖPING", 2, 7),
            new MapTile("Kalmar län", "KALMAR", 3, 7),
            new MapTile("Skåne läns västra", "SKÅNE V", 0, 8),
            new MapTile("Kronobergs län", "KRONOBERG", 1, 8),
            new MapTile("Blekinge län", "BLEKINGE", 2, 8),
            new MapTile("Malmö kommun", "MALMÖ", 0, 9),
            new MapTile("Skåne läns södra", "SKÅNE S", 1, 9),
            new MapTile("Skåne läns norra och östra", "SKÅNE N & Ö", 2, 9),
        };

        public const int Columns = 5;
        public const int Rows = 10;
    }
}
