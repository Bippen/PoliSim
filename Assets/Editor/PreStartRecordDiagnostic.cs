using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §709 (Elias's ruling of 2026-10-01, item 0): **THE PRE-START RECORD, MEASURED.** "First, measure the pre-start record. Report what the
    /// perceived-economy reading holds at each country's start date, and whether it draws on any history from before the start. If the
    /// incumbent's term before the start is missing, seed it from sourced macro data for the period since the previous election ... Report every
    /// backtest before and after, Germany 2025 and Sweden first. Tune nothing."
    /// (a) The generated table (<see cref="PreStartRecord"/>) against its CSV, every figure, and the CSV's digest - asserted.
    /// (b) Every country's start: the window the seed covers (<see cref="PublicationSystem.PreStartWindowOpens"/>), nothing seeded on or after the
    /// start, every seeded figure the table's - asserted; and the reading at the start and at the campaign's opening (where the record is judged),
    /// BEFORE (the two series emptied, as every start had them) and AFTER - printed. At the opening the publication calendar is stepped day by day
    /// over the seed's state (the game's own prints carry the seed economy's values, not a played one's - stated where they are the latest).
    /// (c) Germany 2025 and Sweden 2026: the vote model's prediction with the government's record applied as judged at the opening, before and
    /// after, beside the plain prediction the backtests print - printed, nothing tuned.
    /// </summary>
    public static class PreStartRecordDiagnostic
    {
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        private struct Measured
        {
            public DateTime Opening;
            public PerceivedPerformance.Reading AtStart, AtOpening;
            public string LatestAtStart, LatestAtOpening;
            public Dictionary<string, double> Shift;
            public int SeededUnemployment, SeededInflation;
            public string FirstLast;
            public bool NothingOnOrAfterStart, ValuesTheTable;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PreStartRecordDiagnostic (§709): what the perceived economy reads at a start, before and after the pre-start record ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                // (a) the table against its source, every figure
                string root = Path.GetDirectoryName(Application.dataPath);
                string csvPath = Path.Combine(root, "ElectionsData/macro/prestart_record.csv");
                byte[] bytes = File.ReadAllBytes(csvPath);
                string digest;
                using (var sha = System.Security.Cryptography.SHA256.Create()) { digest = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant(); }
                int rows = 0, wrong = 0, tableRows = 0;
                string firstWrong = null;
                foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId))) { tableRows += PreStartRecord.For(id).Length; }
                string[] lines = File.ReadAllLines(csvPath);
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] c = lines[i].Split(',');
                    if (c.Length != 4) { continue; }
                    rows++;
                    var id = (CountryId)Enum.Parse(typeof(CountryId), c[0]);
                    int year = int.Parse(c[1].Substring(0, 4), CultureInfo.InvariantCulture), month = int.Parse(c[1].Substring(5, 2), CultureInfo.InvariantCulture);
                    float u = c[2].Length == 0 ? float.NaN : float.Parse(c[2], CultureInfo.InvariantCulture);
                    float p = c[3].Length == 0 ? float.NaN : float.Parse(c[3], CultureInfo.InvariantCulture);
                    bool found = false;
                    foreach ((int y, int m, float tu, float tp) in PreStartRecord.For(id))
                    {
                        if (y != year || m != month) { continue; }
                        found = true;
                        bool same = (float.IsNaN(u) ? float.IsNaN(tu) : tu == u) && (float.IsNaN(p) ? float.IsNaN(tp) : tp == p);
                        if (!same) { wrong++; firstWrong = firstWrong ?? lines[i]; }
                    }
                    if (!found) { wrong++; firstWrong = firstWrong ?? lines[i] + " (not in the table)"; }
                }
                Check(digest == PreStartRecord.SourceDigest && wrong == 0 && rows == tableRows && rows > 0,
                    F("(a) the generated table is its CSV: {0} rows, {1} wrong{2}; the digest {3}", rows, wrong, firstWrong == null ? string.Empty : " (first: " + firstWrong + ")",
                        digest == PreStartRecord.SourceDigest ? "matches" : "DIFFERS - regenerate with Tools/prestart_record_prep.pl"));

                // (b) every start, before and after
                var order = new[] { CountryId.Germany, CountryId.Sweden, CountryId.USA, CountryId.Italy, CountryId.Poland, CountryId.France };
                var shiftAfter = new Dictionary<CountryId, Dictionary<string, double>>();
                var shiftBefore = new Dictionary<CountryId, Dictionary<string, double>>();
                foreach (CountryId id in order)
                {
                    using (SimulationManager.EpochScope())
                    {
                        WorldClock.ApplyStart(id);
                        DateTime start = SimulationManager.EpochDate;
                        DateTime opens = PublicationSystem.PreStartWindowOpens(id, start);
                        Measured before = Measure(id, start, seeded: false);
                        Measured after = Measure(id, start, seeded: true);
                        shiftBefore[id] = before.Shift; shiftAfter[id] = after.Shift;
                        Check(after.NothingOnOrAfterStart && after.ValuesTheTable && (opens == DateTime.MinValue || after.SeededUnemployment > 0 || start < opens.AddDays(40)),
                            F("(b) {0}: start {1:yyyy-MM-dd}, the term opened {2} - {3} unemployment and {4} inflation month(s) seeded{5}; none published on or after the start, every figure the table's",
                                id, start, opens == DateTime.MinValue ? "(no election of record before it)" : opens.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                after.SeededUnemployment, after.SeededInflation, after.FirstLast));
                        sb.Append(F("    read      {0} AT ITS START {1:yyyy-MM-dd}: BEFORE {2} - AFTER {3}\n", id, start, Describe(before.AtStart, before.LatestAtStart), Describe(after.AtStart, after.LatestAtStart)));
                        if (after.Opening != DateTime.MinValue)
                        {
                            sb.Append(F("    read      {0} AT ITS CAMPAIGN'S OPENING {1:yyyy-MM-dd} (where the record is judged): BEFORE {2} -> {3} - AFTER {4} -> {5}\n", id, after.Opening,
                                Describe(before.AtOpening, before.LatestAtOpening), Shifts(before.Shift), Describe(after.AtOpening, after.LatestAtOpening), Shifts(after.Shift)));
                        }
                        else { sb.Append(F("    read      {0}: no election on the calendar from this start - its record is never judged\n", id)); }
                    }
                }

                // (c) the predictions with the record, Germany 2025 and Sweden 2026
                Predict(sb, CountryId.Germany, ElectionVintage.Germany2025, "2025", shiftBefore[CountryId.Germany], shiftAfter[CountryId.Germany]);
                Predict(sb, CountryId.Sweden, ElectionVintage.Sweden2026, "2026", shiftBefore[CountryId.Sweden], shiftAfter[CountryId.Sweden]);
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"PRE-START RECORD: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>One start, measured: the reading at the start and at the campaign's opening, the publication calendar stepped over the seed's state.</summary>
        private static Measured Measure(CountryId id, DateTime start, bool seeded)
        {
            var m = new Measured { Opening = DateTime.MinValue, NothingOnOrAfterStart = true, ValuesTheTable = true, FirstLast = string.Empty };
            World world = WorldFactory.CreateDefault();
            Country country = world.GetCountry(id);
            PublicationSystem.SeedInheritedHistory(country);
            PublishedSeries une = country.Published.GetOrCreate(PublishedStat.Unemployment), inf = country.Published.GetOrCreate(PublishedStat.Inflation);
            if (!seeded) { une.Entries.Clear(); inf.Entries.Clear(); }   // BEFORE: as every start had them - the GDP quarter alone
            m.SeededUnemployment = une.Entries.Count; m.SeededInflation = inf.Entries.Count;
            var table = PreStartRecord.For(id);
            foreach ((PublishedSeries series, bool isU) in new[] { (une, true), (inf, false) })
            {
                foreach (PublishedEntry e in series.Entries)
                {
                    if (e.PublicationDate >= start) { m.NothingOnOrAfterStart = false; }
                    bool match = false;
                    foreach ((int y, int mo, float u, float p) in table) { if (y == e.ReferencePeriodStart.Year && mo == e.ReferencePeriodStart.Month) { match = (isU ? u : p) == e.Value; } }
                    if (!match) { m.ValuesTheTable = false; }
                }
            }
            if (une.Entries.Count > 0)
            {
                m.FirstLast = F(" ({0:yyyy-MM} .. {1:yyyy-MM} / {2})", une.Entries[0].ReferencePeriodStart, une.Entries[une.Entries.Count - 1].ReferencePeriodStart,
                    inf.Entries.Count > 0 ? inf.Entries[inf.Entries.Count - 1].ReferencePeriodStart.ToString("yyyy-MM", CultureInfo.InvariantCulture) : "none");
            }
            m.AtStart = PerceivedPerformance.Perceived(country, null);
            m.LatestAtStart = Latest(country, start);
            m.Shift = new Dictionary<string, double>();
            if (WorldClock.TryNextPollingDay(id, start, out DateTime poll))
            {
                CampaignCalendar calendar = CampaignCalendar.FromWorldStart(poll, start);
                m.Opening = calendar.CampaignStart;
                for (DateTime d = start; d < m.Opening; d = d.AddDays(1)) { PublicationSystem.PublishDueFigures(country, d); }
                m.AtOpening = PerceivedPerformance.Perceived(country, null);
                m.LatestAtOpening = Latest(country, start);
                m.Shift = EconomicVote.RecordShiftOf(country, m.AtOpening.Index);
            }
            return m;
        }

        private static string Latest(Country country, DateTime start)
        {
            string One(PublishedStat stat, string name)
            {
                PublishedEntry e = country.Published.Latest(stat);
                if (e == null) { return name + " none"; }
                return F("{0} {1:0.0} % for {2:yyyy-MM} ({3})", name, e.Value, e.ReferencePeriodStart, e.PublicationDate < start ? "the pre-start record" : "the game's own print, the seed economy's");
            }
            return One(PublishedStat.Unemployment, "unemployment") + "; " + One(PublishedStat.Inflation, "inflation");
        }

        private static string Describe(PerceivedPerformance.Reading r, string latest) =>
            r.ComponentsUsed == 0 ? "index 50.0 (nothing published - no component, the neutral by default)" : F("index {0:0.0} from {1} component(s): {2}", r.Index, r.ComponentsUsed, latest);

        private static string Shifts(Dictionary<string, double> shift)
        {
            if (shift == null || shift.Count == 0) { return "no record shift"; }
            var parts = new List<string>();
            foreach (KeyValuePair<string, double> kv in shift) { parts.Add(F("{0} {1:+0.00;-0.00;0.00} pp", kv.Key, kv.Value * 100.0)); }
            return string.Join(", ", parts);
        }

        /// <summary>The model's prediction of an election from its start, plain and with the record as judged before and after, against the count.</summary>
        private static void Predict(StringBuilder sb, CountryId id, ElectionVintage counted, string year, Dictionary<string, double> before, Dictionary<string, double> after)
        {
            using (SimulationManager.EpochScope())
            {
                WorldClock.ApplyStart(id);
                if (!PartySystems.TryHistory(id, out double[] real, out double[] _, counted)) { sb.Append(F("    read      {0} {1}: no count on disk to compare with\n", id, year)); return; }
                IReadOnlyList<PoliticalParty> roster = PartySystems.For(id);
                string Line(string label, IReadOnlyDictionary<string, double> shift)
                {
                    if (!NationalElection.TryPredictShares(id, out Dictionary<string, double> shares, shift)) { return label + " no prediction"; }
                    double dev = 0.0; int n = 0;
                    var cells = new StringBuilder();
                    for (int k = 0; k < roster.Count && k < real.Length; k++)
                    {
                        if (!roster[k].HasPosition || !shares.TryGetValue(roster[k].Abbrev, out double p)) { continue; }
                        dev += Math.Abs(p * 100.0 - real[k]); n++;
                        cells.Append(F(" {0} {1:F2}", roster[k].Abbrev, p * 100.0));
                    }
                    return F("{0}: mean absolute deviation {1:F2} pp over {2} -{3}", label, n > 0 ? dev / n : double.NaN, n, cells);
                }
                sb.Append(F("    read      {0} {1} from its start, against the count: {2}\n", id, year, Line("the plain prediction (every backtest's)", null)));
                sb.Append(F("    read      {0} {1}, the record as judged BEFORE: {2}\n", id, year, Line("with the record", before)));
                sb.Append(F("    read      {0} {1}, the record as judged AFTER: {2}\n", id, year, Line("with the record", after)));
            }
        }
    }
}
