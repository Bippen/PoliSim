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
    /// SP-2 (§623): THE BRIEF TRACES. For every start point of every country: four clauses, each with a non-empty basis naming its record;
    /// no clause carries a figure the record does not hold (a gap says so in the sentence); Sweden's brief reads the record's own figures
    /// (Kristersson's M+KD+L since 18 October 2022 with 103 of 349 seats and SD's support; polling day 13 September 2026; 8 parties); the
    /// tagline slot is empty until one is reviewed.
    /// </summary>
    public static class StartBriefDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== StartBriefDiagnostic (SP-2, §623): the derived brief ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
                {
                    foreach (StartPoints.StartPoint start in StartPoints.For(id))
                    {
                        if (!start.Playable) { continue; }   // a locked contest opens no world; its brief waits for its model
                        List<StartBrief.Clause> clauses = StartBrief.Clauses(start);
                        Check(clauses.Count == 4, F("{0} {1}: four clauses ({2})", id, start.Kind, clauses.Count));
                        foreach (StartBrief.Clause c in clauses)
                        {
                            Check(!string.IsNullOrEmpty(c.Text) && !string.IsNullOrEmpty(c.Basis) && c.Basis.Contains("WorldClock") | c.Basis.Contains("StartPoints") | c.Basis.Contains("PartySystems"),
                                F("{0}: \"{1}\" <- {2}", id, c.Text, c.Basis));
                        }
                        Check(StartBrief.Tagline(start) == null, F("{0}: the tagline slot is empty until one is reviewed", id));
                        // PS-3b (§629): no playable start prints a gap for its government - every one has an executive of record (the USA's president, France's cabinet under its president).
                        bool gap = StartBrief.Text(start).Contains("No government of record");
                        foreach (StartBrief.Row row in StartBrief.Rows(start)) { if (row.Figure.Contains("NONE OF RECORD")) { gap = true; } }
                        Check(!gap, F("{0} {1}: the brief names its government of record, never a gap", id, start.Kind));
                        sb.Append("    brief     ").Append(StartBrief.Text(start)).Append('\n');
                    }
                }
                StartPoints.TryPlayable(CountryId.Sweden, out StartPoints.StartPoint sweden);
                string text = StartBrief.Text(sweden);
                Check(text.StartsWith("Sweden, 18 January 2026.", StringComparison.Ordinal) && text.Contains("since 18 October 2022, with 103 of 349 seats and the support of SD.")
                    && text.Contains("Polling day is 13 September 2026.") && text.Contains("8 parties sit in the Riksdag (349 seats, the election of 11 September 2022)."),
                    "Sweden's brief reads the record: Kristersson's M+KD+L since 18 Oct 2022, 103 of 349, SD's support; polling day 13 Sep 2026; 8 parties");
                // PS-3b (§629): the USA's brief names the administration - the president and their party, then the House majority - instead of "NONE OF RECORD".
                StartPoints.TryPlayable(CountryId.USA, out StartPoints.StartPoint usa);
                string usaText = StartBrief.Text(usa);
                Check(usaText.Contains("The president is Joseph R. Biden Jr. (DEM), in office since 20 January 2021; the House majority is REP, with"),
                    "the USA's brief on 12 March 2024: Biden (DEM) since 20 Jan 2021, the House majority REP - " + usaText);
                bool presidentRow = false;
                foreach (StartBrief.Row row in StartBrief.Rows(usa)) { if (row.Name == "President" && row.Figure == "Joseph R. Biden Jr. (DEM)") { presidentRow = true; } }
                Check(presidentRow, "the USA's ledger carries a President row");
                // PS-6, US-1: the US start says what it holds - the game holds no US election yet, so no polling day is promised. The folder card's line
                // and the start card's mode line say only that no election is held; the brief, its Election row and the not-held reason say what holds -
                // since US-2 the record's House and president, seated on the record's dates (the brief reads those dates from the record), no Senate. Each
                // pinned whole.
                bool electionRow = false, pollingRow = false;
                foreach (StartBrief.Row row in StartBrief.Rows(usa))
                {
                    if (row.Name == "Election" && row.Figure == "NONE IN THIS GAME YET · THE RECORD SEATED ON ITS DATES") { electionRow = true; }
                    if (row.Name.EndsWith("olling day", StringComparison.Ordinal)) { pollingRow = true; }
                }
                string usaLine = WorldClock.StartLine(CountryId.USA), usaReason = NationalElection.NotHeldReason(CountryId.USA);
                string usaMode = StartPoints.ModeLine(usa);
                Check(!usaText.Contains("Polling day") && usaText.Contains("No election is held in this game yet - the House elected on 5 November 2024 is seated on 3 January 2025 and Donald J. Trump (REP) takes office on 20 January 2025, as the record dates them; no Senate is modelled.")
                      && usaLine == "OPENS 12 MAR 2024 · NO ELECTION IN THIS GAME YET" && usaMode == "NO ELECTION YET · OPENS 12 MAR 2024"
                      && usaReason == "No US election is held in this game yet: the House and the president are seated as the record seats them, on its dates, until the Electoral College count and the House races are built; no Senate is modelled.",
                    F("US-1: the USA's start says what it holds - no polling-day clause; the folder card \"{0}\"; the start card \"{1}\"; the not-held reason \"{2}\"", usaLine, usaMode, usaReason));
                Check(electionRow && !pollingRow, "US-1, US-2: the USA's ledger carries an Election row - none in this game yet, the record seated on its dates - and no polling-day row");
                // PS-7-interim (PS-7's "until then"): Italy's start says what it holds, in Italy's words - its calendar offers no polling day, so the snap election
                // the start is cut on is not held. The folder card's line and the start card's mode line say only that no election is held; the brief, its
                // Election row and the not-held reason say what holds - the Camera and the government seated at the start, the government a stand-in for the
                // caretaker's cabinet, no Senato. Each pinned whole, and the snap election's day named on none of them.
                StartPoints.TryPlayable(CountryId.Italy, out StartPoints.StartPoint italy);
                string itText = StartBrief.Text(italy);
                string itLine = WorldClock.StartLine(CountryId.Italy), itMode = StartPoints.ModeLine(italy), itReason = NationalElection.NotHeldReason(CountryId.Italy);
                DateTime snap = WorldClock.LatestElectionDay(CountryId.Italy);
                string snapLong = snap.ToString("d MMMM yyyy", CultureInfo.InvariantCulture), snapStamp = snap.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
                string snapIso = snap.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);   // the brief carries ISO dates too (SeatingDeviation's)
                bool itElectionRow = false, itPollingRow = false, itNamesSnap = false;
                foreach (StartBrief.Row row in StartBrief.Rows(italy))
                {
                    if (row.Name == "Election" && row.Figure == "NONE IN THIS GAME YET · THE START'S CAMERA AND GOVERNMENT HOLD") { itElectionRow = true; }
                    if (row.Name.EndsWith("olling day", StringComparison.Ordinal)) { itPollingRow = true; }
                    if (row.Figure.Contains(snapStamp) || row.Figure.Contains(snapIso)) { itNamesSnap = true; }
                }
                foreach (string said in new[] { itText, itLine, itMode, itReason }) { if (said.Contains(snapLong) || said.Contains(snapStamp) || said.Contains(snapIso)) { itNamesSnap = true; } }
                Check(!itText.Contains("Polling day") && itText.Contains("No election is held in this game yet - the Camera and the government seated at the start hold, the government a stand-in for Draghi's caretaker cabinet; no Senato is modelled.")
                      && itLine == "OPENS 21 JUL 2022 · NO ELECTION IN THIS GAME YET" && itMode == "NO ELECTION YET · OPENS 21 JUL 2022"
                      && itReason == "No Italian election is held in this game yet: the Camera and the government seated at the start hold. Italy is a mixed system - 147 uninominal colleges, 245 proportional seats, 8 overseas - that the game does not count yet, and the per-party seat totals on disk are unconfirmed; no Senato is modelled.",
                    F("PS-7-interim: Italy's start says what it holds - no polling-day clause; the folder card \"{0}\"; the start card \"{1}\"; the not-held reason \"{2}\" - {3}", itLine, itMode, itReason, itText));
                Check(itElectionRow && !itPollingRow, "PS-7-interim: Italy's ledger carries an Election row - none in this game yet, the start's Camera and government hold - and no polling-day row");
                Check(!itNamesSnap, F("PS-7-interim: the snap election's day ({0:yyyy-MM-dd}), which the game does not hold, is named on none of Italy's folder line, mode line, brief, ledger or not-held reason", snap));
                // The premise of Italy's words, pinned (the review): "the Camera and the government seated at the start hold" is true only while nothing seats
                // Italy's record by its dates and no motion of confidence is taken up there - the day either changes, the words must change with it.
                Check(!WorldClock.RecordSeatsChamber(CountryId.Italy) && !WorldClock.RecordSeatsExecutive(CountryId.Italy) && ConfidenceProcedure.RulesOf(CountryId.Italy) == ConfidenceProcedure.Rules.Unsourced,
                    F("PS-7-interim: the premise of Italy's words holds - the record seats neither Italy's chamber ({0}) nor its executive ({1}) by date, and its confidence rules are unsourced ({2})",
                        WorldClock.RecordSeatsChamber(CountryId.Italy), WorldClock.RecordSeatsExecutive(CountryId.Italy), ConfidenceProcedure.RulesOf(CountryId.Italy)));
                // the predicate is the USA's and Italy's alone, and every other playable start with a polling day keeps it, in the brief and the ledger
                var heldElsewhere = new List<string>();
                bool namedOnly = true;
                foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId)))
                {
                    namedOnly &= WorldClock.NoElectionYet(id) == (id == CountryId.USA || id == CountryId.Italy);
                    if (WorldClock.NoElectionYet(id) || !StartPoints.TryPlayable(id, out StartPoints.StartPoint sp) || sp.PollingDay == DateTime.MinValue) { continue; }
                    string brief = StartBrief.Text(sp);
                    bool row = false;
                    foreach (StartBrief.Row r in StartBrief.Rows(sp)) { if (r.Name == "Polling day" || r.Name == "Last polling day") { row = true; } }
                    if (!brief.Contains("Polling day is " + sp.PollingDay.ToString("d MMMM yyyy", CultureInfo.InvariantCulture) + ".") || !row) { heldElsewhere.Add(id.ToString()); }
                }
                Check(namedOnly && heldElsewhere.Count == 0, F("US-1, PS-7-interim: NoElectionYet is the USA's and Italy's alone, and every other playable start with a polling day keeps it in its brief and its ledger{0}",
                    heldElsewhere.Count > 0 ? "; NOT: " + string.Join(", ", heldElsewhere) : string.Empty));
                // PS-7-interim: the pin over every playable start, both ways - a start says no election is held (NoElectionYet) or governs as a what-if
                // (GoverningModeOnly) exactly where the game's calendar offers it no polling day from its opening (TryNextPollingDay). A start that promises a
                // polling day the game does not hold fails here by name, as Italy's would have; so does one that says none while the calendar holds one.
                var mismatched = new List<string>();
                foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId)))
                {
                    if (!StartPoints.TryPlayable(id, out StartPoints.StartPoint sp)) { continue; }
                    bool held = WorldClock.TryNextPollingDay(id, sp.Opens, out DateTime heldOn);
                    bool saysNone = WorldClock.NoElectionYet(id) || WorldClock.GoverningModeOnly(id);
                    if (held == saysNone) { mismatched.Add(held ? F("{0} says none is held, but its calendar holds {1:yyyy-MM-dd}", id, heldOn) : F("{0} promises a polling day its calendar does not hold", id)); }
                }
                Check(mismatched.Count == 0, F("PS-7-interim: every playable start says no election is held exactly where the game's calendar holds none from its opening{0}",
                    mismatched.Count > 0 ? "; NOT: " + string.Join("; ", mismatched) : string.Empty));
                StartPoints.TryPlayable(CountryId.France, out StartPoints.StartPoint france);
                string frText = StartBrief.Text(france);
                Check(frText.Contains("under the president Emmanuel Macron"), "France's brief names its cabinet under its president - " + frText);
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            if (failures > 0) { Debug.LogError($"START BRIEF: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
