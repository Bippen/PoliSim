using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// D-ST (boards 23a-23c, `COMPLETED.md` §701; UI v3.3 §1: *a removed word that no slip can reach fails the bar*): **EVERY WORD THE STATISTICS
    /// TAB TOOK OFF THE PAGE IS ON A SLIP WITHIN TWO LEVELS OR ON THE † DENSE LINE** (23c's reachability). The removed words are MEASURED, not listed:
    /// `docs/generated/STATS_AT_REST_REMOVED.tsv` is the dry films' own difference over the tab's six frames (`Tools/text_baseline.pl removed`, before
    /// D-ST against after). The reachable text is the page's own book (`StatsSlips.Build`, both absence states of the pass-through) and both tabs'
    /// dense lines (`StatsSlips.DenseLines`) on Sweden's seed world.
    /// <para>A draw is split into segments (at · — : ; and full stops; figures read as #; a leading figure is the figure's, its words are its unit's).
    /// A segment that is a figure alone - a reading, a money amount, a reprinted Δ in its unit - is data: the page still prints the reading. Three
    /// classes leave BY DESIGN'S FINDING, each logged with its board number: the relative-per-cent Δ and its definition (23a ⑩: WRONG - the Δ now
    /// prints in the reading's own unit); a count that was wrong (23b ⑧: EIGHT over nine rows); words that explained the page rather than a reading
    /// (23b ③ colour-only legend, ⑤ an empty pin, ⑥ the old pager's words, ⑦ the year, ⑫ WARM OR COOL). Everything else must be reachable.</para>
    /// </summary>
    public static class StatsSlipReachabilityCheck
    {
        private const string RemovedList = "docs/generated/STATS_AT_REST_REMOVED.tsv";
        private static readonly Regex Figure = new Regex(@"\d+(?:[.,]\d+)?", RegexOptions.CultureInvariant);
        private static readonly Regex Split = new Regex(@" · | - |: |; |\. |, ", RegexOptions.CultureInvariant);   // commas too: a slip breaks a long sentence into its clauses, one a line
        private static readonly Regex DataOnly = new Regex(@"^[#\s\-+kMBT%.,()·/]*$", RegexOptions.CultureInvariant);
        private static readonly Regex Money = new Regex(@"^[+\-]?US\$#[kMBT]?$", RegexOptions.CultureInvariant);
        private static readonly Regex Share = new Regex(@"^[+\-]?#% GDP$", RegexOptions.CultureInvariant);
        private static readonly Regex Delta = new Regex(@"^Δ [+\-]?(US\$)?#[kMBT]?%?( PP)?$", RegexOptions.CultureInvariant);
        private static readonly Regex LeadingFigure = new Regex(@"^[+\-]?(US\$)?#[kMBT]?%? ", RegexOptions.CultureInvariant);

        /// <summary>The segments that leave by Design's finding, each with its board number.</summary>
        private static readonly (string Segment, string Why)[] LeaveByFinding =
        {
            ("% OF THE FIRST", "23a ⑩: the Δ's old definition - WRONG, a relative change of a rate; the Δ prints in the reading's own unit"),
            ("MONEY FROM A ZERO BASE", "23a ⑩: the same definition's second clause"),
            ("EIGHT HEADLINE READINGS", "23b ⑧: the count was wrong - EIGHT over nine rows; the head reads READINGS"),
            ("THE SAME EIGHT EACH SIDE", "23b ⑧: the same wrong count"),
            ("GREEN HELPED AND RED HURT", "23b ③: a colour-only legend (§5) - the dots take GOOD and BAD, and the slip says so"),
            ("NO COUNTRY PINNED", "23b ⑤: a pin is a chip's state; with nothing pinned nothing draws"),
            ("‹ PREV", "23b ⑥: three controls for one choice - one line of the partners' codes, a click the step"),
            ("NEXT ›", "23b ⑥: the same"),
            ("YEAR #", "23b ⑦: the year is the rail calendar's"),
            ("NOTHING ON THIS PAGE READS WARM OR COOL", "23b ⑫: explained the page, not the absence - cut"),
            ("AND NO SCORE IS DRAWN IN ITS PLACE", "23b ⑫: the same sentence"),
            ("APPROVAL", "renamed ON THE PAGE, not removed: the pair's row reads APPROVAL RATING, the card's own name (one name per reading)"),
            ("DEU", "R-SP5 B (ruled): a map's chips carry the two-letter code - DEU reads DE on the chip, the name leading its slip"),
        };

        private static string N(string s)
        {
            string t = s.ToUpperInvariant().Replace('–', '-').Replace('—', '-').Replace('−', '-');
            t = Figure.Replace(t, "#");
            return Regex.Replace(t, @"\s+", " ").Trim();
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== StatsSlipReachabilityCheck (D-ST): every word the Statistics tab took off the page, on a slip within two levels or the dense line ===\n");
            int failures = 0;
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string listPath = Path.Combine(root, RemovedList);
            if (!File.Exists(listPath)) { Debug.LogError($"  {RemovedList} is missing - VERIFIED NOTHING"); CheckExit.Finish(1); return; }

            using IDisposable epoch = SimulationManager.EpochScope();
            WorldClock.ApplyStart(CountryId.Sweden);
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            try
            {
                Country sweden = world.GetCountry(CountryId.Sweden);
                Country partner = world.GetCountry(CountryId.USA);
                var reach = new StringBuilder();
                int anchors = 0, terms = 0;
                foreach (float? passThrough in new float?[] { null, 0f })
                {
                    PeopleSlips.Book book = StatsSlips.Build(sweden, world, partner, 0, 0, passThrough);
                    var level2 = new HashSet<string>();
                    void Take(SlipContent c) { reach.Append(N(SlipContent.Plain(c.Head))).Append('\n'); foreach (string l in c.Lines) { reach.Append(N(SlipContent.Plain(l))).Append('\n'); } }
                    foreach (SlipContent c in book.Anchors.Values)
                    {
                        anchors++;
                        Take(c);
                        foreach (string l in c.Lines) { foreach (string term in SlipContent.TermsIn(l)) { level2.Add(term); } }
                    }
                    foreach (string term in level2)
                    {
                        terms++;
                        if (book.Terms.TryGetValue(term, out SlipContent t)) { Take(t); }
                        else { failures++; sb.Append($"    FAIL      a level-1 slip marks '{term}', which has no level-2 slip\n"); }
                    }
                }
                foreach (bool domestic in new[] { true, false })
                {
                    foreach ((string head, string line) in StatsSlips.DenseLines(sweden, domestic, 0, 0)) { reach.Append(N(head)).Append(" · ").Append(N(line)).Append('\n'); }
                }
                string reachable = reach.ToString();

                int draws = 0, segments = 0, data = 0, leaves = 0, missing = 0;
                bool header = false;
                foreach (string raw in File.ReadAllLines(listPath))
                {
                    if (raw.StartsWith("#", StringComparison.Ordinal)) { continue; }
                    if (!header) { header = true; continue; }
                    if (raw.Length == 0) { continue; }
                    draws++;
                    string whole = N(raw);
                    if (Delta.IsMatch(whole)) { data++; continue; }   // a Δ, reprinted on its chart's head in its reading's own unit (23a ⑩)
                    foreach (string part in Split.Split(whole))
                    {
                        string seg = part.Trim().TrimEnd('.', ',');
                        if (seg.Length == 0) { continue; }
                        if (DataOnly.IsMatch(seg) || Money.IsMatch(seg) || Share.IsMatch(seg) || Delta.IsMatch(seg)) { data++; continue; }
                        string words = LeadingFigure.Replace(seg, string.Empty);   // a leading figure is data; its words are its unit's
                        segments++;
                        string why = null;
                        foreach ((string s, string w) in LeaveByFinding) { if (seg == s || words == s) { why = w; break; } }
                        if (why != null) { leaves++; sb.Append($"    leaves    '{seg}' - {why}\n"); continue; }
                        if (reachable.IndexOf(words, StringComparison.Ordinal) < 0) { missing++; failures++; sb.Append($"    FAIL      '{seg}' (from '{raw}') is on no slip within two levels and on no dense line\n"); }
                    }
                }
                if (draws == 0) { Debug.LogError($"  {RemovedList} lists no draws - VERIFIED NOTHING"); CheckExit.Finish(1); return; }
                sb.Append($"    {(missing == 0 ? "ok        " : "FAIL      ")}{draws} removed draws: {segments} word segments, {segments - leaves - missing} on a slip within two levels or the dense line, {leaves} leaving by Design's finding, {missing} unreachable; {data} figure-only segments (the page still prints the reading)\n");
                sb.Append($"    read      the book: {anchors} anchor slips over the two absence states, {terms} level-2 terms marked\n");
            }
            finally { EnergyMarket.ResetTurnState(); }

            if (failures > 0) { Debug.LogError($"STATS SLIPS: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
