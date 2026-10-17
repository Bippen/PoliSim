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
    /// SP-1 (§622): THE START POINTS, ASSERTED. Every country offers at least one; exactly one is playable today and it is the ruled start
    /// of §618 (its polling day the country's latest election of record, its opening `WorldClock.StartDate`, its line the selector's);
    /// every locked card carries a reason; Poland and France offer two contests, the presidential one locked; the cards are in date order;
    /// a date the record does not hold is the year alone on the card with ROUND DATES NOT IN THE RECORD beneath it, never typed; no register number on any card (18b).
    /// PS-6 US-33: the USA's presidential start, and no other card, offers the player's own nominee at the head of the DEM or REP ticket, each with its range.
    /// </summary>
    public static class StartPointsDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== StartPointsDiagnostic (SP-1, §622): the start points as data ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
                {
                    IReadOnlyList<StartPoints.StartPoint> points = StartPoints.For(id);
                    int playable = 0;
                    foreach (StartPoints.StartPoint p in points) { if (p.Playable) { playable++; } }
                    Check(points.Count >= 1 && playable == 1, F("{0}: {1} start point(s), {2} playable", id, points.Count, playable));
                    Check(StartPoints.TryPlayable(id, out StartPoints.StartPoint start) && start.PollingDay == WorldClock.LatestElectionDay(id) && start.Opens == WorldClock.StartDate(id) && start.Line == WorldClock.StartLine(id),
                        F("{0}: the playable start is the ruled one - {1}, polling day {2:yyyy-MM-dd}, opens {3:yyyy-MM-dd}: {4}", id, start.Kind, start.PollingDay, start.Opens, start.Line));
                    for (int i = 0; i < points.Count; i++)
                    {
                        StartPoints.StartPoint p = points[i];
                        Check(!string.IsNullOrEmpty(p.Line) && !string.IsNullOrEmpty(p.Basis) && !string.IsNullOrEmpty(p.Kind), F("{0} #{1} {2}: a line, a basis and a kind", id, i, p.Kind));
                        Check(p.Playable || p.Line.StartsWith("LOCKED", StringComparison.Ordinal), F("{0} #{1} {2}: a locked card says LOCKED and why - {3}", id, i, p.Kind, p.Line));
                        Check(p.PollingDay != DateTime.MinValue || (p.DateNote == StartPoints.RoundDatesNote && p.Year > 0 && StartPoints.DateLine(p) == p.Year.ToString(CultureInfo.InvariantCulture)), F("{0} #{1} {2}: the date is the record's, or the year alone with ROUND DATES NOT IN THE RECORD as the card's second line ({3})", id, i, p.Kind, StartPoints.DateLine(p)));
                        Check(!HasRegister(p.Line) && !HasRegister(p.DateNote), F("{0} #{1} {2}: no register number on the card (18b) - line \"{3}\", note \"{4}\"", id, i, p.Kind, p.Line, p.DateNote));
                        Check(p.Playable == (StartPoints.ModeLine(p) != null) && p.Playable == (StartPoints.Reason(p) == null), F("{0} #{1} {2}: a playable card has a mode line ({3}), a locked one a reason ({4})", id, i, p.Kind, StartPoints.ModeLine(p), StartPoints.Reason(p)));
                        if (i > 0) { Check(StartPoints.DateLine(points[i - 1]) != StartPoints.DateLine(p) || points[i - 1].Kind != p.Kind, F("{0}: #{1} and #{2} are distinct", id, i - 1, i)); }
                    }
                    Check((id == CountryId.Poland || id == CountryId.France) == (points.Count == 2), F("{0}: two contests offered only where a president is popularly elected beside the chamber (the USA's House is a later option, the spec's §1.1)", id));
                }
                Check(StartPoints.For(CountryId.Poland)[1].Kind.StartsWith("PRESIDENTIAL", StringComparison.Ordinal) && StartPoints.For(CountryId.Poland)[1].PollingDay == new DateTime(2025, 5, 18), "Poland's presidential card: 18 May 2025 (the record's first round), after the Sejm's");
                Check(StartPoints.For(CountryId.France)[0].Kind.StartsWith("PRESIDENTIAL", StringComparison.Ordinal) && StartPoints.For(CountryId.France)[0].DateNote == null && StartPoints.For(CountryId.France)[0].PollingDay == new DateTime(2022, 4, 10) && !StartPoints.For(CountryId.France)[0].Playable, "France's presidential card: first round 10 April 2022 [CC-195] (E-49 closed, §629), locked, before the legislative");
                Check(StartPoints.For(CountryId.Poland)[1].Line == "LOCKED · THE PRESIDENTIAL START IS NOT YET BUILT" && StartPoints.For(CountryId.France)[0].Line == StartPoints.For(CountryId.Poland)[1].Line, "the two locked presidential cards carry the one reason in the player's words (18b)");
                StartPoints.TryPlayable(CountryId.France, out StartPoints.StartPoint fr);
                Check(StartPoints.DateLine(fr) == "7 JUL 2024" && StartPoints.ModeLine(fr) == "WHAT-IF · YOUR PARTY GOVERNS · OPENS 18 JUL 2024", "France's playable card: stamp 7 JUL 2024 · WHAT-IF · YOUR PARTY GOVERNS · OPENS 18 JUL 2024");
                StartPoints.TryPlayable(CountryId.USA, out StartPoints.StartPoint us);
                // PS-6 US-8 (in place of US-1's pin): the game holds the USA's presidential election on its polling day, so the card names the run-up to it
                Check(StartPoints.DateLine(us) == "5 NOV 2024" && StartPoints.ModeLine(us) == "RUN-UP · OPENS 12 MAR 2024" && StartPoints.Name(us) == "Presidential election"
                      && WorldClock.TryNextPollingDay(CountryId.USA, us.Opens, out DateTime usHeld) && usHeld == us.PollingDay,
                    F("the USA's card (PS-6 US-8): stamp {0} · {1} · {2} - the run-up to the polling day the game holds", StartPoints.DateLine(us), StartPoints.ModeLine(us), StartPoints.Name(us)));
                StartPoints.TryPlayable(CountryId.Italy, out StartPoints.StartPoint it);
                Check(StartPoints.DateLine(it) == "25 SEP 2022" && StartPoints.ModeLine(it) == "NO ELECTION YET · OPENS 21 JUL 2022" && !StartPoints.ModeLine(it).Contains("SNAP") && StartPoints.Name(it) == "General election (snap)",
                    F("Italy's card (PS-7-interim): stamp 25 SEP 2022 · NO ELECTION YET · OPENS 21 JUL 2022 · General election (snap) - the record's stamp and name kept, no snap election promised that the game does not hold ({0} · {1} · {2})",
                        StartPoints.DateLine(it), StartPoints.ModeLine(it), StartPoints.Name(it)));
                StartPoints.TryPlayable(CountryId.Sweden, out StartPoints.StartPoint se);
                Check(StartPoints.DateLine(se) == "13 SEP 2026" && StartPoints.ModeLine(se) == "RUN-UP · OPENS 18 JAN 2026" && StartPoints.Name(se) == "Riksdag election", "Sweden's card: stamp 13 SEP 2026 · RUN-UP · OPENS 18 JAN 2026 · Riksdag election");
                // PS-6 US-33 (SP-6a; R-US4 (a), RULED - F8: the player's own nominee at the head of a major ticket, inside PS-6): the USA's presidential start, and no
                // other card, lets the player stand a nominee of their own - the two tickets the Global Party Survey 2019 places, each with its range (the first to the
                // third quartile of its experts' placements), the party's own point inside it; the locked presidential cards offer none (SP-6b's third-party run is no start's)
                var offering = new List<string>();
                foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId)))
                {
                    foreach (StartPoints.StartPoint p in StartPoints.For(id)) { if (StartPoints.OffersOwnNominee(p)) { offering.Add(id + " " + p.Kind); } }
                }
                var ranges = new List<string>();
                bool inside = true;
                foreach (string ticket in OwnNominees.Tickets(CountryId.USA))
                {
                    if (!OwnNominees.TryRangeOf(CountryId.USA, ticket, out OwnNominees.Range r)) { inside = false; continue; }
                    foreach (PoliticalParty party in PartySystems.RealRoster(CountryId.USA)) { if (party.Abbrev == ticket) { inside &= r.Holds(party.LrEcon, party.Galtan); } }
                    ranges.Add(F("{0} economic {1:0.00}-{2:0.00} GAL-TAN {3:0.00}-{4:0.00}", ticket, r.EconLo, r.EconHi, r.SocLo, r.SocHi));
                }
                Check(offering.Count == 1 && offering[0] == "USA PRESIDENTIAL ELECTION" && StartPoints.OffersOwnNominee(us) && string.Join(",", OwnNominees.Tickets(CountryId.USA)) == "REP,DEM" && inside
                      && !StartPoints.OffersOwnNominee(StartPoints.For(CountryId.Poland)[1]) && !StartPoints.OffersOwnNominee(StartPoints.For(CountryId.France)[0]),
                    F("US-33: the USA's start alone offers the player's own nominee ({0}) - the tickets {1}, each party's own point inside its range; Poland's and France's locked presidential cards offer none",
                        string.Join("; ", offering), string.Join("; ", ranges)));
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            if (failures > 0) { Debug.LogError($"START POINTS: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        /// <summary>18b: a register id (E-49, S7, S8, R-EL10 …) is not a word the player reads on a card.</summary>
        private static bool HasRegister(string s) => s != null && System.Text.RegularExpressions.Regex.IsMatch(s, "(^|[^A-Z])[A-Z]{1,2}-?[0-9]{1,3}([^0-9]|$)");
    }
}
