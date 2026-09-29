using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;

namespace PoliSim.UI
{
    /// <summary>One slip (19b): its head - the glyph's or the anchor's own word, so every slip is its own legend entry - and its lines. A marked term
    /// is written <c>[[TERM]]</c> in a line; resting on it opens the term's slip, level 2.</summary>
    public sealed class SlipContent
    {
        public readonly string Head;
        public readonly List<string> Lines = new List<string>();
        public SlipContent(string head) { Head = head; }
        public SlipContent Add(string line) { Lines.Add(line); return this; }

        private static readonly Regex Marked = new Regex(@"\[\[(.+?)\]\]", RegexOptions.CultureInvariant);

        /// <summary>The marked terms in a line, in order.</summary>
        public static IEnumerable<string> TermsIn(string line) { foreach (Match m in Marked.Matches(line)) { yield return m.Groups[1].Value; } }

        /// <summary>A line as it prints: the marks removed, the term's words kept.</summary>
        public static string Plain(string line) => line.Replace("[[", string.Empty).Replace("]]", string.Empty);
    }

    /// <summary>
    /// §666 (UI v3.3 §1 and §4.1, boards 19b and 20a): **PEOPLE'S SLIPS, BUILT FROM THE MODEL AND NOTHING ELSE.** Every anchor the cohort block keeps at
    /// rest - each band, each head, each figure, each voter group - has its level-1 slip here; every term a slip marks has its level-2 slip here. The
    /// page draws from this book and `PeopleSlipReachabilityCheck` reads the same book, so the check cannot pass on a slip the page does not open.
    /// Every word that left the page at rest (20a's table E, measured as the dry film's removed draws) is on a slip within two levels.
    /// </summary>
    public static class PeopleSlips
    {
        public sealed class Book
        {
            public readonly Dictionary<string, SlipContent> Anchors = new Dictionary<string, SlipContent>();
            public readonly Dictionary<string, SlipContent> Terms = new Dictionary<string, SlipContent>();
        }

        public static string Millions(float millions) => millions >= 1f
            ? millions.ToString("0.00", CultureInfo.InvariantCulture) + "M"
            : (millions * 1000f).ToString("0", CultureInfo.InvariantCulture) + "k";

        /// <summary>The turnout the band's first eligible age falls in, or NaN.</summary>
        public static double BandTurnout(CohortVoterGroups.Group[] groups, int eligibleFrom)
        {
            foreach (CohortVoterGroups.Group g in groups) { if (eligibleFrom >= g.FromAge && eligibleFrom <= g.ToAge) { return g.TurnoutBase; } }
            return double.NaN;
        }

        private static string P1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

        public static Book Build(PopulationCohorts cohorts, CohortVoterGroups.Group[] groups, int votingAge, bool turnoutSourced)
        {
            var book = new Book();
            float total = cohorts.Total;
            int peak = 0;
            for (int i = 1; i < PopulationCohorts.CohortCount; i++) { if (cohorts.Counts[i] > cohorts.Counts[peak]) { peak = i; } }
            int votingBandFrom = votingAge - votingAge % 5;
            double eligible = CohortVoterGroups.EligiblePopulation(cohorts, votingAge);
            float working = total > 0f ? cohorts.InAgeRange(15, 64) / total * 100f : 0f;

            // Level 2: the terms.
            book.Terms["WORKING AGE"] = new SlipContent("WORKING AGE · 15–64")
                .Add("15 — WORKING AGE BEGINS · 65 — OLD AGE · THE DASHED LINES BOUND IT")
                .Add("BOTH ARE THE MODEL'S · THE DEPENDENCY RATIOS DIVIDE BY IT")
                .Add(P1(working) + "% OF ALL");
            var key = new SlipContent("KEY · HONESTY, AS THE COALITION PAGE PRINTS IT")
                .Add("DERIVED — THE MODEL'S OWN ARITHMETIC OVER THE COHORTS")
                .Add("DECLARED — AUTHORED AND SAID SO")
                .Add("SOURCED — A PUBLISHED SERIES, WITH ITS YEAR")
                .Add("MEASURED — READ OFF THE RUNNING MODEL")
                .Add("◇ DATED — A SERIES OLDER THAN THE GAME");
            book.Terms["DERIVED"] = key;
            book.Terms["SOURCED"] = key;
            book.Terms["DATED"] = key;

            // Level 1: the population's anchors - the head, the foot's total and every band.
            var population = new SlipContent("POPULATION BY BAND · [[DERIVED]]")
                .Add("COHORT SUBSTRATE · 21 FIVE-YEAR BANDS · THE OPEN BAND LAST")
                .Add("TOTAL " + Millions(total) + " · THE BANDS SUM TO IT EXACTLY")
                .Add("[[WORKING AGE]] BETWEEN THE DASHED LINES");
            book.Anchors["head:population"] = population;
            book.Anchors["foot:total"] = population;
            for (int i = 0; i < PopulationCohorts.CohortCount; i++)
            {
                int from = i * PopulationCohorts.CohortWidth;
                int to = i == PopulationCohorts.OpenBandIndex ? 999 : from + PopulationCohorts.CohortWidth - 1;
                var band = new SlipContent(PopulationCohorts.Label(i) + (i == peak ? " · PEAK BAND" : string.Empty))
                    .Add(PopulationCohorts.Label(i) + " · " + Millions(cohorts.Counts[i]) + " · " + (total > 0f ? P1(cohorts.Counts[i] / total * 100f) : "0.0") + "% OF ALL");
                if (to < votingAge) { band.Add("UNDER THE VOTING AGE - NO TURNOUT"); }
                else if (!turnoutSourced) { band.Add("TURNOUT · NO SOURCE"); }
                else
                {
                    double t = BandTurnout(groups, from < votingAge ? votingAge : from);
                    if (!double.IsNaN(t)) { band.Add("TURNOUT " + t.ToString("0", CultureInfo.InvariantCulture) + "% ◇ [[DATED]] 2014"); }
                }
                if (from >= 15 && from < 65) { band.Add("[[WORKING AGE]] · " + working.ToString("0", CultureInfo.InvariantCulture) + "% OF ALL"); }
                if (i == peak) { band.Add("THE LARGEST OF 21 BANDS"); }
                book.Anchors["band:" + i.ToString(CultureInfo.InvariantCulture)] = band;
            }

            // The turnout lane's head (◇).
            book.Anchors["head:turnout"] = turnoutSourced
                ? new SlipContent("TURNOUT BY AGE · [[DATED]]")
                    .Add("[[SOURCED]] · SCB 2014 · A PUBLISHED SERIES")
                    .Add("DRAWN ONLY WHERE A BAND IS ELIGIBLE; DASHED WHERE NOT")
                    .Add($"VOTING AGE {votingAge} — INSIDE THE {votingBandFrom}–{votingBandFrom + 4} BAND, SPLIT PRO RATA")
                : new SlipContent("TURNOUT BY AGE · NO SOURCE")
                    .Add("NO SOURCE FOR THIS COUNTRY - THE LANE IS DASHED ON EVERY BAND RATHER THAN DRAWN FROM A GUESS");

            // Dependency: the head and the four figures.
            book.Anchors["head:dependency"] = new SlipContent("DEPENDENCY · AS THE SUBSTRATE DERIVES IT · [[DERIVED]]")
                .Add("OLD-AGE · TOTAL · 0–19 · 65+ - EACH OVER THE [[WORKING AGE]]");
            book.Anchors["fig:oldage"] = new SlipContent("OLD-AGE · " + P1(cohorts.OldAgeDependencyRatio)).Add("OLD-AGE · 65+ ⁄ 15–64 × 100").Add("OVER THE [[WORKING AGE]] · [[DERIVED]]");
            book.Anchors["fig:total"] = new SlipContent("TOTAL · " + P1(cohorts.TotalDependencyRatio)).Add("TOTAL · (0–14 + 65+) ⁄ 15–64 × 100").Add("OVER THE [[WORKING AGE]] · [[DERIVED]]");
            book.Anchors["fig:school"] = new SlipContent("0–19 · " + P1(cohorts.SchoolAgeShare) + "%").Add("SCHOOL-AGE SHARE (0–19)").Add("OF ALL · [[DERIVED]]");
            book.Anchors["fig:elderly"] = new SlipContent("65+ · " + P1(cohorts.ElderlyShare) + "%").Add("ELDERLY SHARE (65+)").Add("OF ALL · [[DERIVED]]");

            // The electorate: the head, the four figures and every voter group.
            book.Anchors["head:electorate"] = new SlipContent("THE ELECTORATE · [[DERIVED]]")
                .Add($"VOTER GROUPS · POPULATION SHARE OF THE ELIGIBLE · {groups.Length} GROUPS, ONE INK WITH HAIRLINE BREAKS");
            book.Anchors["fig:eligible"] = new SlipContent("ELIGIBLE · " + Millions((float)eligible)).Add($"THE POPULATION AT OR OVER THE VOTING AGE, {votingAge} · [[DERIVED]]");
            book.Anchors["fig:ofall"] = new SlipContent("OF ALL · " + (total > 0f ? P1(eligible / total * 100.0) : "—") + "%").Add("ELIGIBLE, AS A SHARE OF THE POPULATION");
            book.Anchors["fig:votingage"] = new SlipContent("VOTING AGE · " + votingAge.ToString(CultureInfo.InvariantCulture)).Add("VOTING AGE · [[SOURCED]] · CONSTITUTION");
            if (turnoutSourced)
            {
                double votes = 0.0;
                foreach (CohortVoterGroups.Group g in groups) { votes += g.PopulationShare * eligible * g.TurnoutBase / 100.0; }
                book.Anchors["fig:votes"] = new SlipContent("VOTES · ≈ " + Millions((float)votes))
                    .Add("WHAT VOTES · ELIGIBLE × TURNOUT BY BAND")
                    .Add("IF EACH BAND VOTED AT ITS 2014 RATE — A DERIVATION FROM TWO [[SOURCED]] SERIES, NOT A FORECAST");
            }
            for (int i = 0; i < groups.Length; i++)
            {
                var group = new SlipContent(groups[i].Name).Add(groups[i].Name + " " + (groups[i].PopulationShare * 100.0).ToString("0", CultureInfo.InvariantCulture) + " · % OF THE ELIGIBLE");
                if (turnoutSourced && !double.IsNaN(groups[i].TurnoutBase)) { group.Add("TURNOUT " + groups[i].TurnoutBase.ToString("0", CultureInfo.InvariantCulture) + "% ◇ [[DATED]] 2014"); }
                book.Anchors["group:" + i.ToString(CultureInfo.InvariantCulture)] = group;
            }
            return book;
        }
    }
}
