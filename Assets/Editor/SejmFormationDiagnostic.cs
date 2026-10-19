using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// AF-8 (Elias's ruling q8, 2026-10-18): POLAND'S GOVERNMENT CHANGES BY THE STATUTE'S STEPS, on the game's own day path (`AdvanceDay` and the player
    /// country's day tick, the night's call to `OpenSejmFormation` standing in for election night). (a) The dates: the 10th term's first sitting the
    /// record's, a later term's Art. 109 ust. 2's last day; the record's steps for 2023 and none for 2027. (b) THE DONE-WHEN: the 2023 election held on
    /// the record's count - the outgoing PiS cabinet governs to the first sitting, resigns and carries on there (Art. 162), is re-appointed on the record's
    /// day (Art. 154 ust. 1), loses the confidence vote on the game's own seats (ust. 2), and the cabinet the Sejm elects (ust. 3) takes office on the day
    /// the government of record's row gives Tusk's - not a day before. (c) The procedure rides the save mid-way. (d) A Sejm the game elects (2027): the
    /// formation's projection designated at the first sitting, appointed on Art. 154 ust. 1's last day, and confirmed by the Sejm's confidence.
    /// </summary>
    public static class SejmFormationDiagnostic
    {
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== SejmFormationDiagnostic (AF-8, ruling q8): Poland's government by Konstytucja Art. 162 and 154 ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            var hosts = new List<GameObject>();
            DateTime poll23 = WorldClock.LatestElectionDay(CountryId.Poland);
            try
            {
                // (a) the dates
                bool next = WorldClock.TryNextPollingDay(CountryId.Poland, poll23.AddDays(1), out DateTime poll27);
                bool rec23 = SejmFormation.TryOfRecord(CountryId.Poland, poll23, out SejmFormation.StepsOfRecord steps);
                bool tusk = WorldClock.TryGovernmentAt(CountryId.Poland, steps.SejmAppointed, out WorldClock.GovernmentOfRecord tuskRow);
                Check(SejmFormation.Applies(CountryId.Poland) && !SejmFormation.Applies(CountryId.Sweden) && !SejmFormation.Applies(CountryId.Germany),
                    "(a) the Sejm's steps are Poland's alone");
                Check(SejmFormation.FirstSittingAfter(CountryId.Poland, poll23) == new DateTime(2023, 11, 13),
                    F("(a) the 10th term first sits on the record's day: {0:yyyy-MM-dd} [API-TERM]", SejmFormation.FirstSittingAfter(CountryId.Poland, poll23)));
                Check(next && SejmFormation.FirstSittingAfter(CountryId.Poland, poll27) == poll27.AddDays(30),
                    F("(a) a term the game elects ({0:yyyy-MM-dd}) first sits on Art. 109 ust. 2's last day: {1:yyyy-MM-dd} (ruling E2)", poll27, SejmFormation.FirstSittingAfter(CountryId.Poland, poll27)));
                Check(rec23 && steps.Appointed == new DateTime(2023, 11, 27) && steps.ConfidenceVote == new DateTime(2023, 12, 11) && steps.SejmElected == new DateTime(2023, 12, 11)
                      && tusk && tuskRow.From == steps.SejmAppointed && steps.SejmAppointed == new DateTime(2023, 12, 13) && !SejmFormation.TryOfRecord(CountryId.Poland, poll27, out _),
                    F("(a) the record's steps for 2023: appointed {0:yyyy-MM-dd}, confidence {1:yyyy-MM-dd}, the Sejm's election {2:yyyy-MM-dd}, appointed {3:yyyy-MM-dd} = {4}'s row; none for 2027",
                        steps.Appointed, steps.ConfidenceVote, steps.SejmElected, steps.SejmAppointed, tusk ? tuskRow.Head : "NO ROW"));

                // (b) the 2023 election on the record's count, the game's own day path
                Dictionary<string, int> table23 = PartySystems.InitialSeats(CountryId.Poland, ElectionVintage.Poland2023);
                (SimulationManager s1, Country pl1) = Open(hosts, "Konf", table23, poll23);
                GovernmentRecord outgoing = pl1.Government;
                SejmFormation f1 = s1.OpenSejmFormation(pl1, poll23);
                Check(f1 != null && f1.OfRecord && f1.Stage == SejmStage.AwaitingSitting && f1.FirstSitting == new DateTime(2023, 11, 13) && ReferenceEquals(pl1.Government, outgoing) && outgoing.PmParty == "PiS",
                    F("(b) polling day {0:yyyy-MM-dd}: the procedure opens on the outgoing {1} cabinet, nothing installed - the first sitting {2:yyyy-MM-dd}", poll23, outgoing?.PmParty, f1?.FirstSitting ?? DateTime.MinValue));
                var days = new List<(DateTime Date, GovernmentRecord G, bool Caretaker, string Pm, string Cabinet)>();
                DateTime saveDay = new DateTime(2023, 11, 20);
                string save = null;   // serialised AT the cut: a SaveGame holds the live World by reference, so one serialised later reads the world as it stands then
                SejmFormation beforeSave = null;
                int logAtSave = -1;
                while (s1.CurrentDate < new DateTime(2023, 12, 15))
                {
                    Days(s1, 1);
                    GovernmentRecord g = pl1.Government;
                    days.Add((s1.CurrentDate, g, g.Caretaker, g.PmParty, Sorted(g.Cabinet)));
                    if (s1.CurrentDate == saveDay) { beforeSave = g.Sejm; logAtSave = g.Sejm?.Log.Count ?? -1; save = Persistence.SaveGameService.Serialize(Persistence.SaveGameService.CreateSaveGame(s1, s1.World, CountryId.Poland, null)); }
                }
                bool Span(DateTime from, DateTime to, Func<(DateTime Date, GovernmentRecord G, bool Caretaker, string Pm, string Cabinet), bool> holds)
                {
                    int n = 0;
                    foreach (var d in days) { if (d.Date >= from && d.Date <= to) { n++; if (!holds(d)) { sb.Append(F("    at        {0:yyyy-MM-dd}: {1} [{2}]{3}\n", d.Date, d.Pm, d.Cabinet, d.Caretaker ? " caretaker" : string.Empty)); return false; } } }
                    return n == (to - from).Days + 1;
                }
                Check(Span(poll23.AddDays(1), new DateTime(2023, 11, 12), d => ReferenceEquals(d.G, outgoing) && !d.Caretaker && d.Pm == "PiS"),
                    "(b) 16 Oct - 12 Nov 2023: the outgoing PiS cabinet governs, in office, the same record - the night installed nothing");
                Check(Span(new DateTime(2023, 11, 13), new DateTime(2023, 11, 26), d => d.Pm == "PiS" && d.Caretaker),
                    "(b) 13 - 26 Nov: resigned at the first sitting and carrying on (Art. 162 ust. 1, 3)");
                Check(Span(new DateTime(2023, 11, 27), new DateTime(2023, 12, 10), d => d.Pm == "PiS" && d.Cabinet == "PiS" && !d.Caretaker),
                    "(b) 27 Nov - 10 Dec: the record's designee re-appointed with his cabinet on the record's day (Art. 154 ust. 1, M.P. 2023 poz. 1287)");
                Check(Span(new DateTime(2023, 12, 11), new DateTime(2023, 12, 12), d => d.Pm == "PiS" && d.Caretaker),
                    "(b) 11 - 12 Dec: the confidence vote lost on the game's own seats, the cabinet resigned again and carrying on (Art. 154 ust. 2, Art. 162 ust. 2 pkt 1)");
                string tuskCabinet = Sorted(new List<string>(tuskRow.Cabinet ?? new string[0]));
                GovernmentRecord formed = pl1.Government;
                Check(Span(steps.SejmAppointed, new DateTime(2023, 12, 15), d => d.Pm == "KO" && d.Cabinet == tuskCabinet && !d.Caretaker && ReferenceEquals(d.G, formed))
                      && formed.FormedOn == steps.SejmAppointed && formed.Basis != null && formed.Basis.Contains("Art. 154 ust. 3") && formed.Sejm != null && !formed.Sejm.Open,
                    F("(b) THE DONE-WHEN: {0} led by {1} takes office on {2:yyyy-MM-dd}, the record's day for {3}'s cabinet [{4}] - elected by the Sejm (Art. 154 ust. 3); formed {5:yyyy-MM-dd}",
                        Sorted(formed.Cabinet), formed.PmParty, steps.SejmAppointed, tuskRow.Head, tuskCabinet, formed.FormedOn));
                if (formed.Sejm != null) { foreach (string line in formed.Sejm.Log) { sb.Append("    log       ").Append(line).Append('\n'); } }

                // (c) the procedure rides the save mid-way
                SejmFormation loaded = save != null ? Persistence.SaveGameService.Deserialize(save).World.GetCountry(CountryId.Poland).Government.Sejm : null;
                Check(beforeSave != null && loaded != null && loaded.Stage == SejmStage.Designated && loaded.NextOn == steps.Appointed && loaded.OfRecord && loaded.PollingDay == poll23
                      && loaded.FirstSitting == beforeSave.FirstSitting && Sorted(loaded.Cabinet) == "PiS" && loaded.PmParty == "PiS" && loaded.Log.Count == logAtSave,
                    F("(c) a save cut on {0:yyyy-MM-dd} carries the procedure: {1}, next on {2:yyyy-MM-dd}, {3} line(s) of its log", saveDay, loaded?.Stage.ToString() ?? "NONE", loaded?.NextOn ?? DateTime.MinValue, loaded?.Log.Count ?? 0));

                // (d) a Sejm the game elects: the projection designated, appointed on ust. 1's last day, confirmed
                (SimulationManager s2, Country pl2) = Open(hosts, "Konf", table23, poll27);
                GovernmentRecord before2 = pl2.Government;
                GovernmentFormation.View projection = GovernmentFormation.ViewOf(pl2, DeclarationReading.OfElection(CountryId.Poland, poll27));
                SejmFormation f2 = s2.OpenSejmFormation(pl2, poll27);
                DateTime sitting27 = poll27.AddDays(30), appointed27 = sitting27.AddDays(SejmFormation.StepWindowDays), confidence27 = appointed27.AddDays(SejmFormation.StepWindowDays);
                GovernmentRecord atSitting = null, beforeAppointment = null, atAppointment = null;
                while (s2.CurrentDate < confidence27.AddDays(1))
                {
                    Days(s2, 1);
                    if (s2.CurrentDate == sitting27) { atSitting = pl2.Government; }
                    if (s2.CurrentDate == appointed27.AddDays(-1)) { beforeAppointment = pl2.Government; }
                    if (s2.CurrentDate == appointed27) { atAppointment = pl2.Government; }
                }
                string projected = projection.HasGovernment ? Sorted(projection.Cabinet.ConvertAll(c => c.Abbrev)) : "none";
                Check(f2 != null && !f2.OfRecord && ReferenceEquals(atSitting, before2) && atSitting.Caretaker && ReferenceEquals(beforeAppointment, before2),
                    F("(d) {0:yyyy-MM-dd}: the first sitting - the outgoing {1} cabinet resigns and carries on until the appointment", sitting27, before2.PmParty));
                Check(projection.HasGovernment && atAppointment != null && !ReferenceEquals(atAppointment, before2) && Sorted(atAppointment.Cabinet) == projected && !atAppointment.Caretaker
                      && atAppointment.FormedOn == appointed27 && atAppointment.Basis.Contains("Art. 154 ust. 1") && ReferenceEquals(pl2.Government, atAppointment) && atAppointment.Sejm != null && !atAppointment.Sejm.Open,
                    F("(d) {0:yyyy-MM-dd}: the projection {1} appointed on Art. 154 ust. 1's last day; {2:yyyy-MM-dd}: the Sejm's confidence - the procedure {3}",
                        appointed27, projected, confidence27, atAppointment?.Sejm == null ? "NOT CARRIED OVER" : atAppointment.Sejm.Open ? "STILL OPEN" : "concluded"));
                if (atAppointment?.Sejm != null) { foreach (string line in atAppointment.Sejm.Log) { sb.Append("    log       ").Append(line).Append('\n'); } }
                // (e) the record's cabinet put to the game's own Sejm (the s873b review's L4): one rule, SejmFormation.TryRecordCabinet, read by the night's
                // EXPECTED TO FORM and by the install - on the record's count it carries; on a planted count where its parties are short of a majority it
                // does not, and the Sejm is put the formation's projection instead, with the log saying why
                bool recordOn23 = SejmFormation.TryRecordCabinet(pl1, poll23, out List<string> rc23, out _, out string rpm23, out _, out bool carries23, out int yes23, out int of23);
                Check(recordOn23 && carries23 && Sorted(rc23) == tuskCabinet && rpm23 == "KO",
                    F("(e) on the record's count the night and the install name the record's cabinet: {0} led by {1}, {2} of {3} seats", recordOn23 ? Sorted(rc23) : "NONE", rpm23, yes23, of23));
                var planted = new Dictionary<string, int>(table23);
                foreach (string k in new List<string>(planted.Keys)) { planted[k] = 0; }
                planted["PiS"] = 194; planted["KO"] = 120; planted["TD"] = 50; planted["NL"] = 30; planted["Konf"] = 66;
                (SimulationManager s3, Country pl3) = Open(hosts, "Konf", planted, poll23);
                GovernmentRecord outgoing3 = pl3.Government;
                bool record3 = SejmFormation.TryRecordCabinet(pl3, poll23, out List<string> rc3, out _, out _, out _, out bool carries3, out int yes3, out int of3);
                GovernmentFormation.View projection3 = GovernmentFormation.ViewOf(pl3, DeclarationReading.OfElection(CountryId.Poland, poll23));
                bool projectionCarries3 = projection3.HasGovernment && SejmFormation.Carries(pl3, projection3.Cabinet.ConvertAll(c => c.Abbrev), projection3.Support.ConvertAll(c => c.Abbrev), out _, out _);
                SejmFormation f3 = s3.OpenSejmFormation(pl3, poll23);
                while (s3.CurrentDate < new DateTime(2023, 12, 14)) { Days(s3, 1); }
                GovernmentRecord after3 = pl3.Government;
                SejmFormation done3 = after3.Sejm ?? f3;
                bool fellBack = done3 != null && done3.Log.Exists(l => l.Contains("short of a majority - the formation's projection is put instead"));
                Check(record3 && !carries3 && yes3 == 200 && fellBack,
                    F("(e) planted (KO+TD+NL {0} of {1}): the record's cabinet is short of a majority, and the Sejm is put the projection instead - logged {2}", yes3, of3, fellBack ? "so" : "NOT"));
                Check(projectionCarries3
                        ? !ReferenceEquals(after3, outgoing3) && Sorted(after3.Cabinet) == Sorted(projection3.Cabinet.ConvertAll(c => c.Abbrev)) && after3.FormedOn == new DateTime(2023, 12, 13) && after3.Basis.Contains("Art. 154 ust. 3")
                        : after3.Caretaker && after3.PmParty == "PiS" && done3 != null && !done3.Open,
                    F("(e) planted: {0} - {1} [{2}] on 2023-12-14", projectionCarries3 ? "the projection the Sejm elects takes office on the record's day" : "no projection carries, the caretaker serves on (Art. 155 not modelled)",
                        after3.PmParty, Sorted(after3.Cabinet)));
                if (done3 != null) { foreach (string line in done3.Log) { sb.Append("    log (e)   ").Append(line).Append('\n'); } }
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); } EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"SEJM FORMATION: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string Sorted(List<string> parties)
        {
            var copy = new List<string>(parties);
            copy.Sort(string.CompareOrdinal);
            return string.Join("+", copy);
        }

        /// <summary>A Polish game on its start with the given chamber seated and the clock set to the polling day - the count's seats, as the night seats them.</summary>
        private static (SimulationManager, Country) Open(List<GameObject> hosts, string player, Dictionary<string, int> seats, DateTime pollingDay)
        {
            var go = new GameObject("SejmFormationDiagnostic." + pollingDay.Year.ToString(CultureInfo.InvariantCulture)); hosts.Add(go);
            WorldClock.ApplyStart(CountryId.Poland);
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            sim.PlayerCountryId = CountryId.Poland;
            Country poland = world.GetCountry(CountryId.Poland);
            poland.PlayerPartyAbbrev = player;
            poland.ParliamentSeats = new Dictionary<string, int>(seats);
            typeof(SimulationManager).GetProperty("CurrentDate").SetValue(sim, pollingDay);
            return (sim, poland);
        }

        private static void Days(SimulationManager sim, int n) { for (int d = 0; d < n; d++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Poland); } }
    }
}
