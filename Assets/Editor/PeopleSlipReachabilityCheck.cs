using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// §666 (UI v3.3 §1: *a removed word that no slip can reach fails the bar*): **EVERY WORD PEOPLE TOOK OFF THE PAGE AT REST IS ON A SLIP WITHIN
    /// TWO LEVELS.** The removed words are MEASURED, not listed: `docs/generated/PEOPLE_AT_REST_REMOVED.tsv` is the dry films' own difference for
    /// the cohort block's retrofit (§666, `Tools/text_baseline.pl removed`), and `docs/generated/PEOPLE_V35_AT_REST_REMOVED.tsv` the whole page's for
    /// UI v3.5 (§732, `removed-set`: what the People frames showed at rest before and none of them draws after). The slips are the page's own book
    /// (`PeopleSlips.BuildPage`) on Sweden's seed world: level 1 is every anchor's slip, level 2 the slip of every term a level-1 slip marks -
    /// nothing deeper counts.
    /// <para>Asserted: (a) every band's label and figure on its band's slip; (b) every voter group's name and share within the two levels (§732: the
    /// electorate's bar is gone, and the groups are the eligible's level 2) - the data rows of the removed lists, whose figures are the day's; (c) every
    /// other segment of every removed draw (split at · — : ; and full stops, figures read as #) in the reachable text. A segment naming a code
    /// identifier leaves the page by Design's table E (class (c)), logged; a segment that described a mark the v3.5 page no longer draws retires with
    /// it, by name in <see cref="RetiredWithTheirMark"/>, logged.</para>
    /// </summary>
    public static class PeopleSlipReachabilityCheck
    {
        private static readonly string[] RemovedLists = { "docs/generated/PEOPLE_AT_REST_REMOVED.tsv", "docs/generated/PEOPLE_V35_AT_REST_REMOVED.tsv" };
        private static readonly Regex Figure = new Regex(@"\d+(?:[.,]\d+)?", RegexOptions.CultureInvariant);
        private static readonly Regex Split = new Regex(@" · | - | — | – |: |; |\. ", RegexOptions.CultureInvariant);
        private static readonly Regex CodeName = new Regex(@"\b[A-Z][a-z]+[A-Z]\w*", RegexOptions.CultureInvariant);
        private static readonly Regex DataOnly = new Regex(@"^[#\s\-+kM%.,()]*$", RegexOptions.CultureInvariant);

        /// <summary>
        /// §732: segments of the §666 list that DESCRIBED A MARK the v3.5 page no longer draws - they retire with the mark, by name and with the reason,
        /// because a slip that still said them would describe a drawing that is not there. Nothing else may be listed here: a word that names a reading or
        /// a rule stays reachable.
        /// </summary>
        private static readonly Dictionary<string, string> RetiredWithTheirMark = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "DASHED WHERE NOT", "§732: the turnout column draws NO lane where a band cannot vote (the composition's) - the dashed empty lane is gone" },
            { "# GROUPS, ONE INK WITH HAIRLINE BREAKS", "§732: the electorate's one-ink bar is gone - the voter groups are the eligible's level 2" },
        };

        private static string N(string s)
        {
            string t = s.ToUpperInvariant().Replace('–', '-').Replace('—', '-');
            t = Figure.Replace(t, "#");
            return Regex.Replace(t, @"\s+", " ").Trim();
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PeopleSlipReachabilityCheck (§666, §732): every word People took off the page at rest, on a slip within two levels ===\n");
            int failures = 0;
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            foreach (string list in RemovedLists)
            {
                if (!File.Exists(Path.Combine(root, list))) { Debug.LogError($"  {list} is missing - VERIFIED NOTHING"); CheckExit.Finish(1); return; }
            }

            using IDisposable epoch = SimulationManager.EpochScope();
            WorldClock.ApplyStart(CountryId.Sweden);
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            Country sweden = world.GetCountry(CountryId.Sweden);
            try
            {
                PopulationCohorts cohorts = sweden?.Cohorts;
                if (cohorts == null) { Debug.LogError("  Sweden carries no cohort substrate - VERIFIED NOTHING"); CheckExit.Finish(1); return; }
                CohortVoterGroups.Group[] groups = CohortVoterGroups.For(sweden);
                PeopleSlips.Book book = PeopleSlips.BuildPage(sweden, world);

                // The two levels: every anchor's slip, and the slip of every term those mark.
                var reach = new StringBuilder();
                var level2 = new HashSet<string>();
                void Take(SlipContent c) { reach.Append(N(SlipContent.Plain(c.Head))).Append('\n'); foreach (string l in c.Lines) { reach.Append(N(SlipContent.Plain(l))).Append('\n'); } }
                foreach (SlipContent c in book.Anchors.Values)
                {
                    Take(c);
                    foreach (string term in SlipContent.TermsIn(c.Head)) { level2.Add(term); }
                    foreach (string l in c.Lines) { foreach (string term in SlipContent.TermsIn(l)) { level2.Add(term); } }
                }
                foreach (string term in level2)
                {
                    if (book.Terms.TryGetValue(term, out SlipContent t)) { Take(t); }
                    else { failures++; sb.Append($"    FAIL      a level-1 slip marks '{term}', which has no level-2 slip\n"); }
                }
                string reachable = reach.ToString();

                // (a) and (b): every band and every voter group, from the model.
                int bands = 0, groupRows = 0;
                for (int i = 0; i < PopulationCohorts.CohortCount; i++)
                {
                    bands++;
                    string text = book.Anchors.TryGetValue("band:" + i.ToString(CultureInfo.InvariantCulture), out SlipContent s) ? string.Join("\n", s.Lines) : string.Empty;
                    if (!text.Contains(PopulationCohorts.Label(i)) || !text.Contains(PeopleSlips.Millions(cohorts.Counts[i]))) { failures++; sb.Append($"    FAIL      band {PopulationCohorts.Label(i)}: its label or figure {PeopleSlips.Millions(cohorts.Counts[i])} is on no band slip\n"); }
                }
                for (int i = 0; i < groups.Length; i++)
                {
                    groupRows++;
                    string share = (groups[i].PopulationShare * 100.0).ToString("0", CultureInfo.InvariantCulture);
                    if (reachable.IndexOf(N(groups[i].Name + " " + share), StringComparison.Ordinal) < 0) { failures++; sb.Append($"    FAIL      voter group {groups[i].Name} {share}: on no slip within two levels\n"); }
                }
                sb.Append($"    ok        (a) {bands} bands, each one's label and figure on its own slip; (b) {groupRows} voter groups, each one's name and share within two levels\n");

                // (c): the measured removed draws, segment by segment.
                foreach (string list in RemovedLists)
                {
                    int draws = 0, segments = 0, data = 0, leaves = 0, retired = 0, missing = 0;
                    bool header = false;
                    foreach (string raw in File.ReadAllLines(Path.Combine(root, list)))
                    {
                        if (raw.StartsWith("#", StringComparison.Ordinal)) { continue; }
                        if (!header) { header = true; continue; }
                        if (raw.Length == 0) { continue; }
                        draws++;
                        foreach (string part in Split.Split(N(raw)))
                        {
                            // a draw that opens with its separator (" · DIABETES ") keeps none of it: the separator is not one of its words
                            string seg = part.Trim().TrimStart('·').Trim().TrimEnd('.', ',');
                            if (seg.Length == 0) { continue; }
                            if (DataOnly.IsMatch(seg)) { data++; continue; }
                            segments++;
                            if (CodeName.IsMatch(RawSegment(raw, seg))) { leaves++; sb.Append($"    leaves    '{seg}' - a code identifier; it leaves the page by Design's table E, class (c)\n"); continue; }
                            if (RetiredWithTheirMark.TryGetValue(seg, out string why)) { retired++; sb.Append($"    retired   '{seg}' - {why}\n"); continue; }
                            if (reachable.IndexOf(seg, StringComparison.Ordinal) < 0) { missing++; failures++; sb.Append($"    FAIL      '{seg}' (from '{raw}', {Path.GetFileName(list)}) is on no slip within two levels\n"); }
                        }
                    }
                    if (draws == 0) { Debug.LogError($"  {list} lists no draws - VERIFIED NOTHING"); CheckExit.Finish(1); return; }
                    sb.Append($"    {(missing == 0 ? "ok        " : "FAIL      ")}(c) {Path.GetFileName(list)}: {draws} removed draws, {segments} word segments, {segments - leaves - retired - missing} on a slip within two levels, {leaves} leaving by ruling, {retired} retired with their mark, {missing} unreachable; {data} figure-only segments\n");
                }
            }
            finally { EnergyMarket.ResetTurnState(); }

            if (failures > 0) { Debug.LogError($"PEOPLE SLIPS: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>The raw segment a normalised one came from - the code-identifier test reads the draw's own case.</summary>
        private static string RawSegment(string raw, string normalised)
        {
            foreach (string part in Split.Split(raw)) { if (N(part).Trim().TrimEnd('.', ',') == normalised) { return part; } }
            return string.Empty;
        }
    }
}
