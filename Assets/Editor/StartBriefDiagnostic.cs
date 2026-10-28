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
    /// tagline slot is empty until one is reviewed. PS-6 US-33: the player's own nominee, registered for the USA, adds a last clause and a last ledger row naming it
    /// and its ticket - and moves no other brief.
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
                        // PS-6 US-15: and a fifth, the second chamber's, where the record seats one (the USA's Senate)
                        int expected = WorldClock.RecordSeatsSecondChamber(id) ? 5 : 4;
                        bool candidate = PresidentialStart.IsCandidateStart(start);   // SP-7b: and two more, the contest's and the start's own terms, on a candidate's start (Poland's presidential)
                        if (candidate) { expected += 2; }
                        Check(clauses.Count == expected, F("{0} {1}: {2} clauses ({3}){4}", id, start.Kind, expected, clauses.Count, expected == 5 ? " - the fifth the second chamber's" : candidate ? " - the last two the contest's and the start's (the Marshal's order, no campaign)" : string.Empty));
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
                // SP-7b (I14): Poland's presidential start's brief - the 10th Sejm and Tusk's government on 15 Jan 2025, the president of record and his term's end, the
                // run-off of record's day, the field, and the start's own terms said plainly (the Marshal's order, a deviation for presidential starts; no campaign)
                StartPoints.StartPoint polandPresidential = StartPoints.For(CountryId.Poland)[1];
                string plText = StartBrief.Text(polandPresidential);
                var plRows = new List<string>();
                foreach (StartBrief.Row r in StartBrief.Rows(polandPresidential)) { plRows.Add(r.Name + " = " + r.Figure); }
                Check(plText.StartsWith("Poland, 15 January 2025.", StringComparison.Ordinal) && plText.Contains("(KO+TD+NL) has governed since 13 December 2023")
                      && plText.Contains("The president is Andrzej Duda, whose term ends on 6 August 2025; the first vote is on 18 May 2025, and the run-off, if no one has more than half, on 1 June 2025.")
                      && plText.Contains("13 candidates stand of record, 5 of them backed by a party in the Sejm") && plText.Contains("the Marshal of the Sejm ordered the election") && plText.Contains("No campaign is staged")
                      && plRows.Contains("Term ends = 6 AUG 2025 · ANDRZEJ DUDA") && plRows.Contains("Run-off = 1 JUN 2025 · IF NO ONE HAS MORE THAN HALF") && plRows.Contains("Candidates = 13 OF RECORD · 5 BACKED BY A PARTY IN THE SEJM")
                      && plRows.Contains("Opens on = THE MARSHAL'S ORDER · NO CAMPAIGN IS STAGED") && StartBrief.Head(polandPresidential) == "BRIEF · POLAND, 15 JAN 2025",
                    F("SP-7b: Poland's presidential brief reads the record on 15 Jan 2025 and says the start's terms - {0} | {1}", plText, string.Join("; ", plRows)));
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
                // PS-6 US-8 (in place of US-1's pin): the US start names its polling day, which the game now holds - 5 November 2024, 3 U.S.C. §21(1)'s day,
                // the calendar's own answer (WorldClock.TryNextPollingDay), held by name. The folder card's line and the start card's mode line name the run-up;
                // the brief's polling-day clause says what the day elects - the president, through the Electoral College, and (PS-6 US-16) the House and the Senate
                // seats up, the new Congress seated on 3 January - and that the Senate of record changes on its own dates until then; the ledger's Polling day
                // row; the not-held reason says the chamber is elected on the day loop beside the president, not through the chamber's path. The record's next president is named on none of them
                // (the game's count decides who takes office). Each pinned whole.
                bool electionRow = false, pollingRow = false;
                foreach (StartBrief.Row row in StartBrief.Rows(usa))
                {
                    if (row.Name == "Election") { electionRow = true; }
                    if (row.Name == "Polling day" && row.Figure == "5 NOV 2024") { pollingRow = true; }
                }
                string usaLine = WorldClock.StartLine(CountryId.USA), usaReason = NationalElection.NotHeldReason(CountryId.USA);
                string usaMode = StartPoints.ModeLine(usa);
                bool usaHeld = WorldClock.TryNextPollingDay(CountryId.USA, usa.Opens, out DateTime usaPolling);
                bool namesRecordsNext = usaText.Contains("Donald J. Trump") || usaLine.Contains("TRUMP") || usaMode.Contains("TRUMP");
                Check(usaHeld && usaPolling == new DateTime(2024, 11, 5) && usa.PollingDay == usaPolling
                      && usaText.Contains("Polling day is 5 November 2024. The president is elected through the Electoral College; the House and the Senate seats up are elected that day, and the new Congress is seated on 3 January 2025; until then the Senate of record changes on its own dates.")
                      && usaLine == "OPENS 12 MAR 2024 · THE RUN-UP TO 5 NOV 2024" && usaMode == "RUN-UP · OPENS 12 MAR 2024"
                      && usaReason == "The president is elected through the Electoral College, and the House and the Senate seats up are elected on the same day, state by state; the new Congress is seated on the third of January, and until then the House and Senate in office sit."
                      && !namesRecordsNext,
                    F("US-8, US-15: the USA's start names the polling day the game holds - the calendar's {0:yyyy-MM-dd}; the folder card \"{1}\"; the start card \"{2}\"; the not-held reason \"{3}\"; the record's next president named on none ({4})",
                        usaPolling, usaLine, usaMode, usaReason, !namesRecordsNext));
                Check(pollingRow && !electionRow, "US-8: the USA's ledger carries its Polling day row, 5 NOV 2024, and no Election row (US-1's NONE IN THIS GAME YET retired)");
                // PS-6 US-15 (R-US10 (a): "including N independents"; R-US1 (a): the Senate of record by date): the USA's brief carries a Senate clause of its own after
                // the government's - the Senate of record on the start's day, its majority, the independents counted with their conference - and its ledger a Senate
                // row right after the House's; both read from the record (UsSenate.On over US-12's table), pinned whole. No other start carries either: the record seats
                // no other second chamber (Italy's Senato is the game's own count's, PS-7 - none sits at the start).
                List<StartBrief.Clause> usaClauses = StartBrief.Clauses(usa);
                List<StartBrief.Row> usaRows = StartBrief.Rows(usa);
                int houseRow = usaRows.FindIndex(r => r.Name == StartBrief.ChamberOf(CountryId.USA)), senateRow = usaRows.FindIndex(r => r.Name == "Senate");
                const string SenateClause = "The Senate of record is seated by its own dates; its majority is DEM, with 51 of 100 seats, including 3 independents counted with DEM.";
                Check(usaClauses.Count == 5 && usaClauses[2].Text == SenateClause && usaClauses[2].Basis.StartsWith("WorldClock.SecondChamberOfRecord", StringComparison.Ordinal)
                      && senateRow == houseRow + 1 && houseRow >= 0 && usaRows[senateRow].Figure == "DEM · 51 OF 100 · INCLUDING 3 INDEPENDENTS",
                    F("US-15: the USA's brief carries its Senate clause, the third of five - \"{0}\" <- {1}; its ledger's Senate row after the House's - \"{2}\"",
                        usaClauses.Count > 2 ? usaClauses[2].Text : "none", usaClauses.Count > 2 ? usaClauses[2].Basis : "none", senateRow >= 0 ? usaRows[senateRow].Figure : "none"));
                var upperElsewhere = new List<string>();
                foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId)))
                {
                    if (id == CountryId.USA || !StartPoints.TryPlayable(id, out StartPoints.StartPoint sp)) { continue; }
                    if (StartBrief.Text(sp).Contains("Senate of record") || StartBrief.Rows(sp).Exists(r => r.Name == "Senate") || StartBrief.SecondChamberOf(id) != null) { upperElsewhere.Add(id.ToString()); }
                }
                Check(upperElsewhere.Count == 0, F("US-15: no other playable start carries a second chamber's clause, row or name{0}", upperElsewhere.Count > 0 ? "; NOT: " + string.Join(", ", upperElsewhere) : string.Empty));
                // PS-7 (in place of PS-7-interim's pins): Italy's start holds the snap election it is cut on - the folder card and the start card name it, the
                // brief and its ledger carry its polling day (the loop below holds every start's brief), and none of the interim's words stay.
                StartPoints.TryPlayable(CountryId.Italy, out StartPoints.StartPoint italy);
                string itText = StartBrief.Text(italy);
                string itLine = WorldClock.StartLine(CountryId.Italy), itMode = StartPoints.ModeLine(italy);
                bool itPollingRow = StartBrief.Rows(italy).Exists(r => r.Name == "Polling day" && r.Figure == "25 SEP 2022"), itElectionRow = StartBrief.Rows(italy).Exists(r => r.Name == "Election");
                Check(itLine == "OPENS 21 JUL 2022 · THE SNAP ELECTION OF 25 SEP 2022" && itMode == "SNAP ELECTION · OPENS 21 JUL 2022" && itText.Contains("Polling day is 25 September 2022.")
                      && !itText.Contains("in this game yet") && !itText.Contains("no Senato is modelled") && itPollingRow && !itElectionRow,
                    F("PS-7: Italy's start names the snap election the game holds - the folder card \"{0}\"; the start card \"{1}\"; the ledger's Polling day row {2}, no Election row {3} - {4}", itLine, itMode, itPollingRow, !itElectionRow, itText));
                // the predicate holds for no start since PS-7 retired Italy (US-8 the USA), and every playable start with a polling day keeps it,
                // in the brief and the ledger - the USA's among them now
                var heldElsewhere = new List<string>();
                bool namedOnly = true;
                foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId)))
                {
                    namedOnly &= !WorldClock.NoElectionYet(id);   // PS-7 retired Italy: no start holds the predicate
                    if (WorldClock.NoElectionYet(id) || !StartPoints.TryPlayable(id, out StartPoints.StartPoint sp) || sp.PollingDay == DateTime.MinValue) { continue; }
                    string brief = StartBrief.Text(sp);
                    bool row = false;
                    foreach (StartBrief.Row r in StartBrief.Rows(sp)) { if (r.Name == "Polling day" || r.Name == "Last polling day") { row = true; } }
                    if (!brief.Contains("Polling day is " + sp.PollingDay.ToString("d MMMM yyyy", CultureInfo.InvariantCulture) + ".") || !row) { heldElsewhere.Add(id.ToString()); }
                }
                Check(namedOnly && heldElsewhere.Count == 0, F("US-8, PS-7: NoElectionYet holds for no start (PS-7 retired Italy's), and every playable start with a polling day - the USA's among them - keeps it in its brief and its ledger{0}",
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
                    // PS-6 US-8: and the day a start's card names is the day its calendar holds - the USA's 5 November 2024 by name above
                    else if (held && heldOn != sp.PollingDay) { mismatched.Add(F("{0}'s calendar holds {1:yyyy-MM-dd}, but its card names {2:yyyy-MM-dd}", id, heldOn, sp.PollingDay)); }
                }
                Check(mismatched.Count == 0, F("PS-7-interim, US-8: every playable start says no election is held exactly where the game's calendar holds none from its opening, and names the day it holds where it holds one{0}",
                    mismatched.Count > 0 ? "; NOT: " + string.Join("; ", mismatched) : string.Empty));
                StartPoints.TryPlayable(CountryId.France, out StartPoints.StartPoint france);
                string frText = StartBrief.Text(france);
                Check(frText.Contains("under the president Emmanuel Macron"), "France's brief names its cabinet under its president - " + frText);
                // PS-6 US-33 (SP-6a; the own nominee in place of R-US2 (a)'s nominee of record for the player's party - the plan's "a leader at the head of the DEM or REP ticket"): with no own nominee registered every brief is as pinned
                // above; with one registered for the USA its brief gains a sixth clause and its ledger a last row, naming the nominee and the ticket and no nominee of
                // record (the naming before the count is US-6's, asked with R-US23), each tracing to its record; no other start's brief moves; the clause the flow's
                // review reads from a draft is the registered one's, and none is made for an unnamed draft or another country's start
                using (OwnNominees.Scope())
                {
                    OwnNominees.Clear();
                    var before = new Dictionary<CountryId, (string Text, int Rows)>();
                    foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId)))
                    {
                        if (StartPoints.TryPlayable(id, out StartPoints.StartPoint sp)) { before[id] = (StartBrief.Text(sp), StartBrief.Rows(sp).Count); }
                    }
                    var hale = new OwnNominee { Country = CountryId.USA, Party = "REP", Name = "Jordan Hale", LrEcon = 8.0f, Galtan = 8.0f };
                    bool registered = OwnNominees.TryRegister(hale, out string refused);
                    List<StartBrief.Clause> withClauses = StartBrief.Clauses(usa);
                    List<StartBrief.Row> withRows = StartBrief.Rows(usa);
                    string withText = StartBrief.Text(usa);
                    const string NomineeText = "Your own nominee, Jordan Hale, heads the Republican Party's ticket in place of its nominee of record; the party's poll reads the nominee's position.";
                    StartBrief.Row last = withRows.Count > 0 ? withRows[withRows.Count - 1] : default;
                    Check(registered && withClauses.Count == 6 && withClauses[5].Text == NomineeText && withClauses[5].Basis.Contains("StartPoints") && withClauses[5].Basis.Contains("OwnNominees")
                          && withText == before[CountryId.USA].Text + " " + NomineeText && withRows.Count == before[CountryId.USA].Rows + 1 && last.Name == "Your nominee" && last.Figure == "Jordan Hale (REP)"
                          && !withText.Contains("Donald J. Trump") && !withText.Contains("Kamala D. Harris"),
                        F("US-33: the USA's brief with its own nominee registered - the sixth clause \"{0}\" <- {1}; the ledger's last row {2} · {3}; no nominee of record named{4}",
                            withClauses.Count > 5 ? withClauses[5].Text : "none", withClauses.Count > 5 ? withClauses[5].Basis : "none", last.Name, last.Figure, registered ? string.Empty : " - refused: " + refused));
                    var moved = new List<string>();
                    foreach (KeyValuePair<CountryId, (string Text, int Rows)> kv in before)
                    {
                        if (kv.Key == CountryId.USA) { continue; }
                        StartPoints.TryPlayable(kv.Key, out StartPoints.StartPoint sp);
                        if (StartBrief.Text(sp) != kv.Value.Text || StartBrief.Rows(sp).Count != kv.Value.Rows) { moved.Add(kv.Key.ToString()); }
                    }
                    var draft = new OwnNominee { Country = CountryId.USA, Party = "REP", Name = "Jordan Hale", LrEcon = 8.5f, Galtan = 7.5f };
                    var unnamed = new OwnNominee { Country = CountryId.USA, Party = "REP", Name = " ", LrEcon = 8.5f, Galtan = 7.5f };
                    StartBrief.Clause? fromDraft = StartBrief.NomineeClause(usa, draft);
                    Check(moved.Count == 0 && fromDraft.HasValue && fromDraft.Value.Text == NomineeText && !StartBrief.NomineeClause(usa, unnamed).HasValue
                          && !StartBrief.NomineeClause(sweden, hale).HasValue && !StartBrief.NomineeRow(sweden, hale).HasValue,
                        F("US-33: no other start's brief moves{0}; the flow's review reads the same clause from its draft; none for an unnamed draft or for Sweden's start", moved.Count > 0 ? " - NOT: " + string.Join(", ", moved) : string.Empty));
                    OwnNominees.Clear();
                    Check(StartBrief.Text(usa) == before[CountryId.USA].Text && StartBrief.Clauses(usa).Count == 5, "US-33: the nominee cleared, the USA's brief is the five clauses pinned above again");
                }
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            if (failures > 0) { Debug.LogError($"START BRIEF: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
