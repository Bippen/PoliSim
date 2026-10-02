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
    /// after, beside the plain prediction the backtests print - printed, nothing tuned; and since §752 (Elias's ruling A1) with the record judged
    /// OVER THE TERM for the count, as play now judges it.
    /// (d) §752: the term's reading at the opening and for the count, and the day the government took office - the two starts' asserted, and
    /// `EconomicVote.TookOffice` on planted governments, one per branch today's records can show, each failing without it: the walk over a
    /// partner's exit; the stop at the election that opened the term (a caretaker after it); the clamp to that election (an outgoing government of
    /// record, a president's lame weeks); a stand-in seated after it; the outcome gate; the head's party gate; a stand-in taking its record head's
    /// day (Poland's PiS, no walk); the midterm that opens no president's term - asserted. (The same-person test is masked on today's records: an
    /// election falls between every change of head - the fourth review's note.)
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
            // §752 (A1): the record over the term - at the campaign's opening and for the count, the government's took-office day
            public DateTime? TookOffice, CountDay;
            public PerceivedPerformance.TermReading TermAtOpening, TermAtCount;
            public Dictionary<string, double> TermShiftOpening, TermShiftCount;
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
                var measuredAfter = new Dictionary<CountryId, Measured>();
                foreach (CountryId id in order)
                {
                    using (SimulationManager.EpochScope())
                    {
                        WorldClock.ApplyStart(id);
                        DateTime start = SimulationManager.EpochDate;
                        DateTime opens = PublicationSystem.PreStartWindowOpens(id, start);
                        Measured before = Measure(id, start, seeded: false);
                        Measured after = Measure(id, start, seeded: true);
                        shiftBefore[id] = before.Shift; shiftAfter[id] = after.Shift; measuredAfter[id] = after;
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

                // (d) §752 (Elias's ruling A1): the record over the government's TERM - the day it took office (the record's head walked back),
                // the change from then to the opening and to the count; the two the record gives asserted
                foreach (CountryId id in order)
                {
                    Measured m = measuredAfter[id];
                    if (m.Opening == DateTime.MinValue) { continue; }
                    sb.Append(F("    read      {0} (A1) AT ITS CAMPAIGN'S OPENING {1:yyyy-MM-dd}: {2} -> {3}\n", id, m.Opening, EconomicVote.Describe(m.TermAtOpening), Shifts(m.TermShiftOpening)));
                    sb.Append(F("    read      {0} (A1) FOR THE COUNT {1:yyyy-MM-dd}: {2} -> {3}\n", id, m.CountDay, EconomicVote.Describe(m.TermAtCount), Shifts(m.TermShiftCount)));
                }
                Check(measuredAfter[CountryId.Germany].TookOffice == new DateTime(2021, 12, 8),
                    F("(d) Germany's government took office {0:yyyy-MM-dd} - Scholz's; the start sits in his first row, no walk (the planted minority below walks)", measuredAfter[CountryId.Germany].TookOffice));
                Check(measuredAfter[CountryId.Sweden].TookOffice == new DateTime(2022, 10, 18),
                    F("(d) Sweden's government took office {0:yyyy-MM-dd} - Kristersson's, 2022-10-18", measuredAfter[CountryId.Sweden].TookOffice));
                // (d) the walk-back itself, on planted governments (the first review's defect 3: no start reaches it), where it must stop - at the latest
                // election, by election day (the second review's defect 1) - and every branch proven by a plant that fails without it (its defect 2)
                DateTime? Took(CountryId id, DateTime formed, string pm, string outcome, bool provisional, WorldClock.ExecutiveKind kind = WorldClock.ExecutiveKind.Cabinet)
                {
                    using (SimulationManager.EpochScope())
                    {
                        WorldClock.ApplyStart(id);
                        Country c = WorldFactory.CreateDefault().GetCountry(id);
                        c.Government = new GovernmentRecord { FormedOn = formed, PmParty = pm, Outcome = outcome, Provisional = provisional, Kind = kind };
                        return EconomicVote.TookOffice(c);
                    }
                }
                DateTime? scholzMinority = Took(CountryId.Germany, new DateTime(2024, 11, 7), "SPD", "of record", false);
                Check(scholzMinority == new DateTime(2021, 12, 8), F("(d) planted: Scholz's minority of record from 2024-11-07 took office {0:yyyy-MM-dd} - walked back to 2021-12-08 over the FDP's exit (the walk)", scholzMinority));
                DateTime? caretaker = Took(CountryId.Sweden, new DateTime(2026, 9, 17), "M", "of record", false);
                Check(caretaker == new DateTime(2026, 9, 17), F("(d) planted: Kristersson's caretaker of record from 2026-09-17 took office {0:yyyy-MM-dd} - the 2026-09-13 election stops the walk short of 2022-10-18 (the of-record stop)", caretaker));
                DateTime? window = Took(CountryId.Germany, new DateTime(2025, 3, 25), "SPD", "of record", false);
                Check(window == new DateTime(2025, 2, 23), F("(d) planted: Scholz's minority of record in an epoch of 2025-03-25, after the election and before Merz, measured from {0:yyyy-MM-dd} - the 2025-02-23 election, where §709's seed opens (the clamp)", window));
                DateTime? standIn = Took(CountryId.Sweden, new DateTime(2026, 10, 1), "M", "the formation's", true);
                Check(standIn == new DateTime(2026, 10, 1), F("(d) planted: an M-led stand-in seated after the 2026-09-13 election on 2026-10-01 took office {0:yyyy-MM-dd} - its own, never the outgoing term (the stand-in's stop)", standIn));
                DateTime? formedInPlay = Took(CountryId.Germany, new DateTime(2025, 4, 30), "SPD", "formed", false);
                Check(formedInPlay == new DateTime(2025, 4, 30), F("(d) planted: an SPD government formed in play on 2025-04-30, the record's head's party, took office {0:yyyy-MM-dd} - its own formation day (the outcome gate)", formedInPlay));
                DateTime? biden = Took(CountryId.USA, new DateTime(2024, 3, 12), "DEM", "of record", false, WorldClock.ExecutiveKind.Presidency);
                Check(biden == new DateTime(2021, 1, 20), F("(d) planted: Biden's presidency of record at 2024-03-12 took office {0:yyyy-MM-dd} - the 2022 midterm opens no president's term (§709's window: the presidential election)", biden));
                DateTime? lameDuck = Took(CountryId.USA, new DateTime(2025, 1, 10), "DEM", "of record", false, WorldClock.ExecutiveKind.Presidency);
                Check(lameDuck == new DateTime(2024, 11, 5), F("(d) planted: Biden of record in an epoch of 2025-01-10, after his successor's election, measured from {0:yyyy-MM-dd} - the 2024-11-05 election, where §709's seed opens (the third review's lame weeks)", lameDuck));
                DateTime? polandStandIn = Took(CountryId.Poland, new DateTime(2023, 2, 19), "PiS", "the formation's", true);
                Check(polandStandIn == new DateTime(2019, 11, 15), F("(d) planted: Poland's PiS stand-in at its 2023-02-19 start took office {0:yyyy-MM-dd} - the record's head (Morawiecki) from 2019-11-15 (the stand-in branch of the gate)", polandStandIn));
                DateTime? otherParty = Took(CountryId.Germany, new DateTime(2024, 11, 7), "CDU", "of record", false);
                Check(otherParty == new DateTime(2024, 11, 7), F("(d) planted: a CDU government of record on 2024-11-07, where the record's head is the SPD's, took office {0:yyyy-MM-dd} - its own day, never Scholz's (the head's party gate)", otherParty));

                // (c) the predictions with the record, Germany 2025 and Sweden 2026 - and §752's: over the term, for the count (Germany's vote gap re-run)
                Predict(sb, CountryId.Germany, ElectionVintage.Germany2025, "2025", shiftBefore[CountryId.Germany], shiftAfter[CountryId.Germany], measuredAfter[CountryId.Germany].TermShiftCount);
                Predict(sb, CountryId.Sweden, ElectionVintage.Sweden2026, "2026", shiftBefore[CountryId.Sweden], shiftAfter[CountryId.Sweden], measuredAfter[CountryId.Sweden].TermShiftCount);
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
                // §752 (A1): the record over the term - judged at the opening, then for the count on the campaign's last day, as play judges it
                m.TookOffice = EconomicVote.TookOffice(country);
                m.TermShiftOpening = EconomicVote.RecordOverTerm(country, m.Opening, out m.TermAtOpening);
                m.CountDay = calendar.CampaignStart.AddDays(calendar.TotalCampaignDays - 1);
                for (DateTime d = m.Opening; d <= m.CountDay.Value; d = d.AddDays(1)) { PublicationSystem.PublishDueFigures(country, d); }
                m.TermShiftCount = EconomicVote.RecordOverTerm(country, m.CountDay.Value, out m.TermAtCount);
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
        private static void Predict(StringBuilder sb, CountryId id, ElectionVintage counted, string year, Dictionary<string, double> before, Dictionary<string, double> after, Dictionary<string, double> overTerm)
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
                sb.Append(F("    read      {0} {1}, the record OVER THE TERM for the count (§752, A1 - what play now judges): {2}\n", id, year, Line("with the record", overTerm)));
            }
        }
    }
}
