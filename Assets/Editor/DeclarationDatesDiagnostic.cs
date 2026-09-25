using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §621 (ruled 2026-09-25): THE DECLARATIONS' TIMELINE, ASSERTED. The timeline read on the 2022 election's day has the shape of the pinned
    /// `Sweden2022` set and on 13 September 2026 the shape of the `Sweden2026` set (pairs, strengths, one-way flags, candidacies, in-or-against
    /// rules - the bases may differ in wording); and at every change point of the run-up the ruled fact appears or lifts on its ruled day:
    /// C→V from 30 January, L's line lifted 13 March, M's 1 April with its 2026 candidacy, V's rule 18 April, S's 2026 candidacy 1 May, KD's line
    /// lifted 8 September; K-1g (§639): SD's refusal of the support role 10 October 2025, MP's rule 4 April 2026 (a press report's date), KD → S 2 September. Every fact's dates are ordered and no two facts of one party and kind overlap.
    /// </summary>
    public static class DeclarationDatesDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== DeclarationDatesDiagnostic (§621): Sweden's declarations by date ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                IReadOnlyList<PoliticalParty> parties = PartySystems.For(CountryId.Sweden);
                string Shape(List<RedLine> lines)
                {
                    var keys = new List<string>();
                    foreach (RedLine l in lines)
                    {
                        if (l.Kind != RedLineKind.Declared) { continue; }
                        keys.Add(parties[l.A].Abbrev + (l.OneWay ? ">" : "-") + parties[l.B].Abbrev + (l.BlocksSupport ? " S" : " C") + (DeclaredRedLines.IsCandidacy(l) ? " cand" : string.Empty));
                    }
                    keys.Sort(StringComparer.Ordinal);
                    return string.Join(" | ", keys);
                }
                string Rules(List<InOrAgainst> rules) { var k = new List<string>(); foreach (InOrAgainst r in rules) { k.Add(parties[r.Party].Abbrev + (r.VotesAgainst ? string.Empty : "~")); } k.Sort(StringComparer.Ordinal); return string.Join(",", k); }   // "~": K-1g's no-support-role form
                string Cand(DateTime day, string abbrev) { foreach ((string a, string _, string basis) in DeclaredRedLines.CandidaciesAt(CountryId.Sweden, day)) { if (a == abbrev) { return basis; } } return string.Empty; }   // by abbreviation, never by array order (the review)
                bool Has(List<RedLine> lines, string a, string b, bool oneWay, bool blocksSupport)
                {
                    foreach (RedLine l in lines) { if (l.Kind == RedLineKind.Declared && parties[l.A].Abbrev == a && parties[l.B].Abbrev == b && l.OneWay == oneWay && l.BlocksSupport == blocksSupport) { return true; } }
                    return false;
                }

                // 1. The two vintages are points on the timeline.
                DateTime day2022 = WorldClock.ElectionDayOf(CountryId.Sweden, ElectionVintage.Sweden2022), day2026 = WorldClock.ElectionDayOf(CountryId.Sweden, ElectionVintage.Sweden2026);
                string v22 = Shape(DeclaredRedLines.For(CountryId.Sweden, parties, ElectionVintage.Sweden2022)), t22 = Shape(DeclaredRedLines.ForDate(CountryId.Sweden, parties, day2022));
                string v26 = Shape(DeclaredRedLines.For(CountryId.Sweden, parties, ElectionVintage.Sweden2026)), t26 = Shape(DeclaredRedLines.ForDate(CountryId.Sweden, parties, day2026));
                Check(v22 == t22, F("{0:yyyy-MM-dd}: the timeline reads the Sweden2022 set - [{1}] vs [{2}]", day2022, t22, v22));
                Check(v26 == t26, F("{0:yyyy-MM-dd}: the timeline reads the Sweden2026 set - [{1}] vs [{2}]", day2026, t26, v26));
                Check(Rules(DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, parties, day2022)) == Rules(DeclaredRedLines.InOrAgainstFor(CountryId.Sweden, parties, ElectionVintage.Sweden2022)), "2022: the in-or-against rules agree (none)");
                Check(Rules(DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, parties, day2026)) == Rules(DeclaredRedLines.InOrAgainstFor(CountryId.Sweden, parties, ElectionVintage.Sweden2026)) && Rules(DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, parties, day2026)) == "MP,SD~,V",
                    "2026: the in-or-against rules agree (V and MP vote against, SD refuses the support role - K-1g) - [" + Rules(DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, parties, day2026)) + "]");

                // 2. The change points of the run-up, each on its ruled day and not the day before.
                void Point(DateTime day, string what, Func<List<RedLine>, bool> holds)
                {
                    bool before = holds(DeclaredRedLines.ForDate(CountryId.Sweden, parties, day.AddDays(-1)));
                    bool on = holds(DeclaredRedLines.ForDate(CountryId.Sweden, parties, day));
                    Check(!before && on, F("{0:yyyy-MM-dd}: {1} - the eve {2}, the day {3}", day, what, before ? "already" : "not yet", on ? "yes" : "NO"));
                }
                List<RedLine> start = DeclaredRedLines.ForDate(CountryId.Sweden, parties, WorldClock.StartDate(CountryId.Sweden));
                Check(Has(start, "C", "SD", false, true) && Has(start, "M", "SD", false, false) && Has(start, "KD", "SD", false, false) && Has(start, "L", "SD", false, false)
                    && Has(start, "S", "M", true, true) && Has(start, "M", "S", true, true) && !Has(start, "C", "V", true, true)
                    && !Has(start, "KD", "S", true, true)
                    && Rules(DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, parties, WorldClock.StartDate(CountryId.Sweden))) == "SD~",
                    F("{0:yyyy-MM-dd} (the start): the 2022 lines and the carried candidacy pair, no C>V, no KD>S, no V or MP rule, SD's no-support rule (2025-10-10) - [{1}]", WorldClock.StartDate(CountryId.Sweden), Shape(start)));
                Point(new DateTime(2026, 1, 30), "C>V one way appears (C's own publication)", l => Has(l, "C", "V", true, true));
                Point(new DateTime(2026, 3, 13), "L's no-SD-ministers line lifts", l => !Has(l, "L", "SD", false, false));
                Point(new DateTime(2026, 4, 1), "M's no-SD-ministers line lifts", l => !Has(l, "M", "SD", false, false));
                Check(Cand(new DateTime(2026, 3, 31), "M").Contains("2022-03-26") && Cand(new DateTime(2026, 4, 1), "M").Contains("[MSD-P1]"), "2026-04-01: M's candidacy is 2026's from that day, 2022's the day before");
                Check(Rules(DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, parties, new DateTime(2026, 4, 17))) == "MP,SD~" && Rules(DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, parties, new DateTime(2026, 4, 18))) == "MP,SD~,V", "2026-04-18: V's rule takes effect (the decision's day, not the PDF's)");
                Check(Rules(DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, parties, new DateTime(2025, 10, 9))) == string.Empty && Rules(DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, parties, new DateTime(2025, 10, 10))) == "SD~", "2025-10-10: SD's no-support rule takes effect (Åkesson's own post - K-1g)");
                Check(Rules(DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, parties, new DateTime(2026, 4, 3))) == "SD~" && Rules(DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, parties, new DateTime(2026, 4, 4))) == "MP,SD~", "2026-04-04: MP's rule takes effect (TT's report - a press report's date, stated, K-1g)");
                Point(new DateTime(2026, 9, 2), "KD>S one way appears (Busch's own words - K-1g)", l => Has(l, "KD", "S", true, true));
                Check(Cand(new DateTime(2026, 4, 30), "S").Contains("2022-08-04") && Cand(new DateTime(2026, 5, 1), "S").Contains("[S-P2]"), "2026-05-01: S's candidacy is 2026's from that day, 2022's the day before");
                Point(new DateTime(2026, 9, 8), "KD's no-SD-ministers line lifts (KD's own words)", l => !Has(l, "KD", "SD", false, false));
                foreach (DateTime day in new[] { new DateTime(2026, 1, 18), new DateTime(2026, 6, 1), new DateTime(2026, 9, 13) })
                {
                    Check(DeclaredRedLines.CandidaciesAt(CountryId.Sweden, day).Count == 2, F("{0:yyyy-MM-dd}: the candidacy pair stands (S and M)", day));
                }

                // 3. The timeline's own order.
                var seen = new Dictionary<string, List<(DateTime, DateTime)>>();
                foreach (DeclaredRedLines.DatedFact f in DeclaredRedLines.SwedenTimeline)
                {
                    Check(f.From < f.Until, F("{0} {1} {2}: from {3:yyyy-MM-dd} before until {4}", f.Kind, f.Party, f.Other ?? "-", f.From, f.Until == DateTime.MaxValue ? "open" : f.Until.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    string key = f.Kind + " " + f.Party + " " + (f.Other ?? "-");
                    if (!seen.TryGetValue(key, out List<(DateTime, DateTime)> spans)) { spans = new List<(DateTime, DateTime)>(); seen[key] = spans; }
                    foreach ((DateTime a, DateTime b) in spans) { Check(f.From >= b || f.Until <= a, F("{0}: no overlap between [{1:yyyy-MM-dd}, {2}) and an earlier span", key, f.From, f.Until == DateTime.MaxValue ? "open" : f.Until.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))); }
                    spans.Add((f.From, f.Until));
                }
                sb.Append(F("    {0} dated fact(s) on Sweden's timeline.\n", DeclaredRedLines.SwedenTimeline.Count));
            }
            catch (Exception e)
            {
                failures++;
                sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n");
            }
            if (failures > 0) { Debug.LogError($"DECLARATION DATES: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
