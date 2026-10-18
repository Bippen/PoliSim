using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-6 US-22b (Elias's ruling I13 (2), 2026-10-18, `docs/RULINGS_2026-10-18.md`): "(P) stands. Add R-US17's draw (H1) for the withholder's own senators on
    /// each resolution's cloture vote during a lapse, fitted on the record's lapse-period cloture votes, labelled FITTED n=...". THE WITHHOLDER'S DRAW, the
    /// arithmetic alone - R-US17's form (`UsVeto`): one recorded observation (d, m) is drawn by a keyed index from a list, and its crossing share is carried
    /// onto the bloc's seats, rounded half up and capped at a strict minority (`UsVeto.Crossers`). The draw is made in the Senate only, on a continuing
    /// resolution's cloture vote while funding is lapsed, for the party withholding it (REP or DEM), whose crossers move from against to for. Where it is
    /// read is `Congress.Divide` (the concern's `BillConcern.DrawsWithholders`, set by `SimulationManager.ResolveContinuingResolution` during a lapse).
    ///
    /// <para><b>The list is the record's</b> (<see cref="Record"/>): the Senate's own roll calls (LIS XML, `ElectionsData/usa/raw/lapse/senate/`) on the
    /// cloture motions to proceed to H.R. 5371 - the continuing resolution of record - from 1 October 2025, the lapse's first day, to its last cloture vote
    /// before P.L. 119-37; d is the Democratic caucus's yeas (the Democrats and the independents who caucus with them, R-US10 (a)'s caucus, as the game seats
    /// them) and m its yeas and nays. READINGS, DECLARED: the 2025 lapse alone - the lapse of record F8 names - not the lapses of 2013, January 2018 and
    /// 2018-2019; the cloture motions TO PROCEED, one per attempt, so the cloture votes on the substitute and on the bill that followed the successful
    /// attempt (the same senators, the agreement already made) are not counted again; the Democrats' own alternative resolutions are not the resolution
    /// withheld and are not read. The list fitted on the Democrats of record is drawn for whichever of the two parties withholds. `UsBudgetProcedureDiagnostic`
    /// (j) recomputes the list from the saved roll calls and fails on any difference.</para>
    ///
    /// <para>No SimulationRandom stream is created, drawn or moved: the draw is keyed on the master seed, the day, the resolution's title and the party
    /// (<see cref="Key"/>), so a reload and a fork that vote it on the same day draw alike. Its own counter (<see cref="DrawCalls"/>) and plant
    /// (<see cref="PlantIndex"/>): `UsVeto`'s counter still proves that R-US17's veto-path draw refuses every budget vote.</para>
    /// </summary>
    public static class UsLapseDraw
    {
        /// <summary>One recorded cloture vote: its identity (S | Congress | the vote's date | the Senate's roll-call number), the withholding caucus's yeas
        /// (Defectors) and its yeas and nays (Voting).</summary>
        public readonly struct Vote
        {
            public readonly string Identity;
            public readonly int Defectors, Voting;
            public Vote(string identity, int defectors, int voting) { Identity = identity; Defectors = defectors; Voting = voting; }
        }

        /// <summary>The record's lapse-period cloture votes, in date order - the drawn list. [FITTED] on the Senate's roll calls named in the summary.</summary>
        public static readonly Vote[] Record =
        {
            new Vote("S|119|2025-10-01|537", 3, 47),
            new Vote("S|119|2025-10-03|543", 3, 46),
            new Vote("S|119|2025-10-06|545", 3, 44),
            new Vote("S|119|2025-10-08|551", 3, 47),
            new Vote("S|119|2025-10-09|558", 3, 47),
            new Vote("S|119|2025-10-14|571", 2, 45),
            new Vote("S|119|2025-10-15|572", 3, 46),
            new Vote("S|119|2025-10-16|573", 3, 46),
            new Vote("S|119|2025-10-20|576", 2, 43),
            new Vote("S|119|2025-10-22|581", 3, 47),
            new Vote("S|119|2025-10-28|590", 3, 47),
            new Vote("S|119|2025-11-04|603", 3, 46),
            new Vote("S|119|2025-11-09|610", 8, 47),
        };

        /// <summary>The list as `UsVeto` reads a cell's: (d, m) in <see cref="Record"/>'s order.</summary>
        public static readonly IReadOnlyList<(int Defectors, int Voting)> List = Build();

        private static IReadOnlyList<(int Defectors, int Voting)> Build()
        {
            var list = new (int Defectors, int Voting)[Record.Length];
            for (int i = 0; i < list.Length; i++) { list[i] = (Record[i].Defectors, Record[i].Voting); }
            return Array.AsReadOnly(list);
        }

        /// <summary>
        /// THE RECORD'S SPACING (the review's M1, DECLARED): the list is fitted PER VOTE, so the game votes a lapse's resolutions as often as the record did -
        /// the days from its first vote to its last over the intervals between them, rounded to whole days (DERIVED from <see cref="Record"/>'s dated
        /// identities). A continuing resolution tabled while funding is lapsed takes this course instead of a bill's (`SimulationManager.TableContinuingResolution`).
        /// </summary>
        public static int RecordSpacingDays => (int)Math.Round((DayOf(Record[Record.Length - 1]) - DayOf(Record[0])).TotalDays / (Record.Length - 1), MidpointRounding.AwayFromZero);

        /// <summary>A recorded vote's date, read from its identity (S | Congress | yyyy-MM-dd | number).</summary>
        public static DateTime DayOf(Vote vote) => DateTime.ParseExact(vote.Identity.Split('|')[2], "yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>The label the ruling asks for - FITTED with n, the record's votes the list holds.</summary>
        public static string FittedLabel => "FITTED n=" + Record.Length.ToString(CultureInfo.InvariantCulture);

        /// <summary>The pooled crossing share over the list - every vote's yeas over every vote's yeas and nays - for the words and the diagnostic.</summary>
        public static (int Defectors, int Voting) Pooled()
        {
            int d = 0, m = 0;
            foreach (Vote v in Record) { d += v.Defectors; m += v.Voting; }
            return (d, m);
        }

        /// <summary>The Wilson score interval of <paramref name="d"/> of <paramref name="m"/> at <paramref name="z"/> (1.96: 95 %) - printed beside the label. It
        /// treats each senator's vote as its own trial; the votes are the same senators again and again, so it is narrower than the record's own spread
        /// (the per-vote shares), which the diagnostic prints beside it.</summary>
        public static (double Low, double High) Wilson(int d, int m, double z = 1.96)
        {
            if (m <= 0) { return (0d, 1d); }
            double p = (double)d / m, z2 = z * z, denominator = 1d + z2 / m;
            double centre = (p + z2 / (2d * m)) / denominator;
            double half = z * Math.Sqrt(p * (1d - p) / m + z2 / (4d * m * m)) / denominator;
            return (Math.Max(0d, centre - half), Math.Min(1d, centre + half));
        }

        /// <summary>THE KEY: masterSeed (invariant) | USA | the vote's day | the resolution's title | Senate | the party | withholders - `UsVeto.Key`'s form with
        /// its own tail, so it never coincides with a veto-path key.</summary>
        public static string Key(int masterSeed, DateTime day, string title, string party)
            => masterSeed.ToString(CultureInfo.InvariantCulture) + "|USA|" + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "|" + (title ?? string.Empty)
               + "|Senate|" + party + "|withholders";

        /// <summary>
        /// THE DRAW: K for the withholding party's <paramref name="seats"/> senators on one resolution's cloture vote - the keyed unit over <see cref="Key"/>, the
        /// index into <see cref="List"/> (`UsVeto.Index`), the crossers (`UsVeto.Crossers`). Refused (K = 0, nothing counted) for a party other than REP or DEM
        /// and for a party with no seat. Inside <see cref="PlantIndex"/> the index is the planted one.
        /// </summary>
        public static int Draw(int masterSeed, DateTime day, string title, string party, int seats, out int index)
        {
            index = -1;
            if (seats <= 0 || (party != "REP" && party != "DEM") || List.Count == 0) { return 0; }
            _drawCalls++;
            index = _planted.HasValue ? Math.Min(List.Count - 1, Math.Max(0, _planted.Value)) : UsVeto.Index(UsVeto.Unit(Key(masterSeed, day, title, party)), List.Count);
            return UsVeto.Crossers(seats, List[index].Defectors, List[index].Voting);
        }

        /// <summary>The draws made since the counter was last reset.</summary>
        public static long DrawCalls => _drawCalls;
        public static void ResetDrawCalls() { _drawCalls = 0; }
        [ThreadStatic] private static long _drawCalls;
        [ThreadStatic] private static int? _planted;

        /// <summary>For the checks: every draw inside the returned scope takes <paramref name="index"/> (clamped to the list) instead of the keyed one; disposing
        /// the scope restores what was planted before.</summary>
        public static IDisposable PlantIndex(int index)
        {
            var restore = new PlantedScope(_planted);
            _planted = index;
            return restore;
        }

        private sealed class PlantedScope : IDisposable
        {
            private readonly int? _before;
            public PlantedScope(int? before) { _before = before; }
            public void Dispose() { _planted = _before; }
        }
    }
}
