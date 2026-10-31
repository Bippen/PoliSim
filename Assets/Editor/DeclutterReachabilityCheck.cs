using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using PoliSim.Testing;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// K321 (g), on K320 (5) - *decluttering moves words to the slip and never deletes content*: **EVERY WORD A SWEEP MOVES IS ON A SLIP WITHIN TWO
    /// LEVELS OR ON THE DENSE VIEW** - People's reachability check (`PeopleSlipReachabilityCheck`, §666), made general.
    ///
    /// <para><b>The moved words.</b> Each sweep item writes its own `docs/declutter/moved_&lt;item&gt;.tsv` (one file an item, so items built in
    /// parallel never write one file), columns `capture_stem` (the frame the words left), `text_before` (the draw as it read at rest),
    /// `now_in` (`slip:&lt;capture stem&gt;` - the slip book the page draws on that frame, two levels - or `dense:&lt;capture stem&gt;` - that frame's
    /// dense view) and `slip_text` (the words as the slip now holds them, where they were reworded; empty: the text before must be reachable).</para>
    ///
    /// <para><b>The same means as People's.</b> The reachable text is the slip book itself - every anchor's slip (level 1) and the slip of every
    /// term a level-1 slip marks (level 2), nothing deeper - normalised as People's check normalises it (upper case, figures '#'), and each word
    /// segment of the moved text (split at the middle dot, a spaced dash or slash, a colon, a semicolon, a full stop, a comma; a figure alone is
    /// data) must be in it. ⚠ <b>Where a book is built.</b> People's and Statistics' books are built from the model by static builders
    /// (`PeopleSlips.BuildPage`, `StatsSlips.Build` and its dense lines), so a moved row whose target is a People frame (04…) or a Statistics frame
    /// (02…) is read HERE, on Sweden's seed world, as their own checks read them. Every other page builds its book inside its draw (the Budget,
    /// Energy, Laws, Politics, Docket, Desk and masthead books are filled as the page lays itself out), so no edit-mode check can open it: those
    /// rows are judged by the film that takes their target, against the book that frame drew (`DryGuiPass.NoteSlipBook`, the film's
    /// `&lt;label&gt;_slipbooks.tsv`) and the dense frame's draws - `DeclutterRules.JudgeMoved`, which also fails a moved text still drawn at rest.
    /// This check names each such row and leaves it to the film; it never counts one as reached.</para>
    ///
    /// <para><b>Vacuous until the sweep moves something</b>, and it says so. Its teeth run every time: a planted row whose words are on People's
    /// book must be reached and a planted row on no slip must not; the film's judgement, driven with a planted book and frame, must find the
    /// reached row, the unreached row, the row still at rest and the row left to another film; a malformed row must be refused.</para>
    /// </summary>
    public static class DeclutterReachabilityCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== DeclutterReachabilityCheck (K321 (g)): every word a sweep moved, on a slip within two levels or the dense view ===\n");
            int failures = 0;
            void Fail(string line) { failures++; sb.Append("    FAIL      ").Append(line).Append('\n'); }
            void Ok(string line) { sb.Append("    ok        ").Append(line).Append('\n'); }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            var ruleErrors = new List<string>();
            DeclutterRules.RuleSet rules = DeclutterRules.Load(root, ruleErrors);
            foreach (string e in ruleErrors) { Fail(e); }

            var shape = new List<string>();
            List<DeclutterRules.MovedRow> moved = DeclutterRules.ReadMoved(root, shape, out int files);
            foreach (string e in shape) { Fail(e); }

            using IDisposable epoch = SimulationManager.EpochScope();
            WorldClock.ApplyStart(CountryId.Sweden);
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            try
            {
                Country sweden = world.GetCountry(CountryId.Sweden);
                Country partner = world.GetCountry(CountryId.USA);
                if (sweden == null) { Debug.LogError("  Sweden is not in the seed world - VERIFIED NOTHING"); CheckExit.Finish(1); return; }

                // the books built from the model, as their own checks build them
                string people = Reachable(PeopleSlips.BuildPage(sweden, world), Fail);
                var statsText = new StringBuilder();
                foreach (float? passThrough in new float?[] { null, 0f }) { statsText.Append(Reachable(StatsSlips.Build(sweden, world, partner, 0, 0, passThrough), Fail)); }
                foreach (bool domestic in new[] { true, false })
                {
                    foreach ((string head, string line) in StatsSlips.DenseLines(sweden, domestic, 0, 0)) { statsText.Append(DeclutterRules.Normalise(head)).Append(" · ").Append(DeclutterRules.Normalise(line)).Append('\n'); }
                }
                string stats = statsText.ToString();
                string ModelBook(string target) => target.StartsWith("04", StringComparison.Ordinal) ? people : target.StartsWith("02", StringComparison.Ordinal) ? stats : null;

                // the moved rows
                int reached = 0, unreached = 0, film = 0, exempt = 0, prose = 0;
                foreach (DeclutterRules.MovedRow m in moved)
                {
                    if (m.Where == DeclutterRules.ProseWhere) { prose++; continue; }   // K334: counted below, never judged, never reached
                    string where = $"{m.File} line {m.Line} ({m.CaptureStem} -> {m.NowIn})";
                    DeclutterRules.Exemption ex = rules.ExemptionOf(m.CaptureStem);
                    if (ex != null && ex.All) { exempt++; Fail($"{where}: '{m.CaptureStem}' is exempt from every cap by {ex.Glob} ({ex.Reason}) - nothing on it is decluttered, so nothing moves from it"); continue; }
                    string book = ModelBook(m.Target);
                    if (book == null)
                    {
                        film++;
                        sb.Append($"    film      {where}: '{m.Needed}' - the page builds its book as it draws; judged by the film that takes {m.Target}\n");
                        continue;
                    }
                    List<string> missing = DeclutterRules.Unreachable(m.Needed, book);
                    if (missing.Count == 0) { reached++; continue; }
                    unreached++;
                    Fail($"{where}: {string.Join(" | ", missing.Select(s => "'" + s + "'"))} on no slip within two levels{(m.Where == "dense" ? " and no line of the dense view" : string.Empty)} of {(m.Target.StartsWith("04", StringComparison.Ordinal) ? "People's book" : "Statistics' book")}");
                }
                if (files == 0) { sb.Append($"    vacuous   no moved file under {DeclutterRules.MovedFolder} - the sweep has moved nothing yet, so nothing is owed a slip; the check passes on its teeth alone\n"); }
                else { Ok($"{files} moved file(s), {moved.Count} row(s): {reached} reached on a book built from the model, {unreached} unreached, {film} left to the film that takes their target, {exempt} on an exempt frame, {prose} unjudged in prose"); }
                // K334: the prose rows, written before this reader landed - a ratchet that only falls as each file converts to slip:/dense: rows
                if (prose > DeclutterRules.ProseRowCeiling) { Fail($"K334: {prose} prose row(s) against the ceiling of {DeclutterRules.ProseRowCeiling} - a new moved file, or a row added to a prose one, is written as slip:/dense: rows, never prose"); }
                else { Ok($"K334: {prose} prose row(s) UNJUDGED (pre-K321, {DeclutterRules.ProseMarker}), within the ceiling of {DeclutterRules.ProseRowCeiling}{(prose < DeclutterRules.ProseRowCeiling ? " - LOWER THE CEILING to " + prose : string.Empty)}; each file converts when its K322 sweep touches its screens"); }

                // the teeth: People's book, by the same means
                string first = PeopleSlips.BuildPage(sweden, world).Anchors.Values.SelectMany(c => c.Lines).Select(SlipContent.Plain).FirstOrDefault(l => DeclutterRules.Words(l) >= 2);
                if (first == null) { Fail("teeth: People's book holds no line of two words - the planted reachable row has nothing to stand on"); }
                else
                {
                    bool reaches = DeclutterRules.Unreachable(first, people).Count == 0;
                    bool refuses = DeclutterRules.Unreachable("ZQX PLANTED WORDS ON NO SLIP", people).Count > 0;
                    if (reaches && refuses) { Ok($"teeth: a planted row of People's own words ('{first}') is reached; a planted row on no slip is not"); }
                    else { Fail($"teeth: People's book - the planted reachable row {(reaches ? "reached" : "NOT reached")}, the planted unreachable row {(refuses ? "refused" : "REACHED")}"); }
                }

                // the teeth: the film's judgement, on a planted book and frame
                var planted = new List<DeclutterRules.MovedRow>
                {
                    Planted("08_energy", "Prices at the wholesale market today", "slip:08_energy", "WHOLESALE PRICE · AT THE MARKET"),
                    Planted("08_energy", "A line nobody wrote anywhere", "slip:08_energy", "ZQX NOWHERE ON THE BOOK"),
                    Planted("08_energy", "The grid carries the target", "slip:08_energy", "GRID AND TARGET"),
                    Planted("04h_people_dense", "Band populations", "dense:04h_people_dense", "21 FIVE-YEAR BANDS"),
                    Planted("07a_politics_parliament", "Seats by list", "slip:07a_politics_parliament", ""),
                };
                var books = DeclutterRules.ReadSlipBooks(new[]
                {
                    "capture\tlevel\tkey\trole\ttext",
                    "08_energy\t1\ten:wholesale\thead\tWHOLESALE PRICE · 41.2 EUR/MWh",
                    "08_energy\t1\ten:wholesale\tline\tAT THE MARKET, BEFORE TAXES",
                    "08_energy\t2\tGRID\thead\tGRID AND TARGET",
                });
                var frame = new List<DeclutterRules.Row>
                {
                    PlantedRow("08_energy", "The grid carries the target", ""),
                    PlantedRow("08_energy", "Energy", ""),
                    PlantedRow("04h_people_dense", "21 FIVE-YEAR BANDS SUM TO THE POPULATION", "OUTSIDE"),
                };
                var verdict = new DeclutterRules.Verdict();
                DeclutterRules.JudgeMovedRows(planted, frame, books, verdict);
                if (verdict.MovedJudged == 4 && verdict.MovedUnreachable == 1 && verdict.MovedStillAtRest == 1 && verdict.MovedElsewhere == 1)
                {
                    Ok("teeth: the film's judgement on a planted book and frame - a reached row, an unreached one, one still at rest, a dense line scrolled out of the clip reached, one left to the film that takes its frame");
                }
                else
                {
                    Fail($"teeth: the film's judgement on the planted book and frame must judge 4, find 1 unreached, 1 still at rest and leave 1 - it judged {verdict.MovedJudged}, {verdict.MovedUnreachable} unreached, {verdict.MovedStillAtRest} still at rest, {verdict.MovedElsewhere} left");
                    foreach (string line in verdict.Failures) { sb.Append("              ").Append(line).Append('\n'); }
                }

                // the teeth: a malformed file is refused
                var refused = new List<string>();
                DeclutterRules.ParseMoved(new[] { DeclutterRules.MovedHeader, "05a_budget_tax\tthree columns only", "05a_budget_tax\tA text\tdrawer:05a\t" }, "planted.tsv", refused);
                if (refused.Count == 2) { Ok("teeth: a row short of a column and a row whose now_in is neither slip: nor dense: are refused"); }
                else { Fail($"teeth: the two malformed planted rows must both be refused - {refused.Count} refusal(s)"); }
            }
            finally { EnergyMarket.ResetTurnState(); }

            if (failures > 0) { Debug.LogError($"DECLUTTER REACH: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.Append("DECLUTTER REACH: PASS\n").ToString());
            CheckExit.Finish(0);
        }

        /// <summary>A book's reachable text, People's two levels: every anchor's slip, and the slip of every term a level-1 slip marks (a marked term
        /// with no slip fails, as in People's check). Normalised, a line a row.</summary>
        private static string Reachable(PeopleSlips.Book book, Action<string> fail)
        {
            var reach = new StringBuilder();
            var level2 = new HashSet<string>(StringComparer.Ordinal);
            void Take(SlipContent c)
            {
                reach.Append(DeclutterRules.Normalise(SlipContent.Plain(c.Head))).Append('\n');
                foreach (string l in c.Lines) { reach.Append(DeclutterRules.Normalise(SlipContent.Plain(l))).Append('\n'); }
            }
            foreach (SlipContent c in book.Anchors.Values)
            {
                Take(c);
                foreach (string term in SlipContent.TermsIn(c.Head)) { level2.Add(term); }
                foreach (string l in c.Lines) { foreach (string term in SlipContent.TermsIn(l)) { level2.Add(term); } }
            }
            foreach (string term in level2)
            {
                if (book.Terms.TryGetValue(term, out SlipContent t)) { Take(t); }
                else { fail($"a level-1 slip marks '{term}', which has no level-2 slip"); }
            }
            return reach.ToString();
        }

        private static DeclutterRules.MovedRow Planted(string stem, string before, string nowIn, string slip)
        {
            int colon = nowIn.IndexOf(':');
            return new DeclutterRules.MovedRow { File = "planted", Line = 0, CaptureStem = stem, TextBefore = before, NowIn = nowIn, SlipText = slip, Where = nowIn.Substring(0, colon), Target = nowIn.Substring(colon + 1) };
        }

        private static DeclutterRules.Row PlantedRow(string capture, string text, string flags) => new DeclutterRules.Row
        {
            Capture = capture, Width = 1280, Font = 14, Mode = "line", NeedsHeight = 14.1, Text = text, Plain = text,
            Flags = new HashSet<string>(flags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal),
        };
    }
}
