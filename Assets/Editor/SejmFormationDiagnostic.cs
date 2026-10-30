using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
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
    /// <para>K302 (Elias, 2026-10-29): ART. 155 AND THE SHORTENED TERM, and K301's filed finding. (f) The words: Art. 155 and Art. 98 ust. 5-6 in the Tribunal's
    /// text, the Kodeks wyborczy's day off work and forty-five days, the 2007 shortened term on record - each held to its SHA-256 - and the 2007 day
    /// reproduced. (g) A PLANTED 230/230 CHAMBER on the player's day path: no designee, no Sejm's election, the President's cabinet appointed and refused,
    /// the term shortened and the player's polling day on Art. 98 ust. 5's day (its fortieth to forty-fifth), the procedure riding a save at the President's
    /// step; the Sejm it elects first sitting on the fifteenth day, the calendar re-anchored on its term (ust. 6), and on the next ordinary election's own day
    /// the calendar answering that day. (h) A SIMPLE-MAJORITY CHAMBER (a cabinet carried by fewer than half the members, the rest abstaining): the President's
    /// cabinet takes office and holds. (i) The same two chambers in an AI Poland, stepped by the day loop: installed on the first sitting, or the President's
    /// refused cabinet a caretaker (Art. 162 ust. 2 pkt 1, ust. 3) and the extra election on Art. 98 ust. 5's day after the sitting, counted on its day and its
    /// Sejm formed on its fifteenth; and an older save's AI caretaker whose owed day has passed ordered again on the day found. (j) No Polish election of
    /// record moves: the 2023 count's sitting and steps as before, an ordinary count's calendar the chain's. (k) THE
    /// DECLARED SUPPORT LINES HONOURED: the lines' shapes and directions pinned as the record words them; the formation's government pinned on two planted
    /// chambers (one where Konfederacja's seats would carry PiS) and Art. 155's vote pinned (Konfederacja against a PiS cabinet, TD against a Konfederacja
    /// one); the "Konf led by Konf with PiS" arrangement main's dump installs is the declarations' own reading (no declared line refuses PiS's support of a
    /// Konfederacja cabinet). Its crossing test is CONSTRUCTIVE - it re-applies the predicate the formation draws supporters by, and says so.</para>
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
                        : after3.Caretaker && done3 != null && done3.Open && done3.Stage == SejmStage.PresidentAppoints,
                    F("(e) planted: {0} - {1} [{2}] on 2023-12-14", projectionCarries3 ? "the projection the Sejm elects takes office on the record's day" : "no projection carries - the caretaker serves while Art. 155 runs (K302)",
                        after3.PmParty, Sorted(after3.Cabinet)));
                if (done3 != null) { foreach (string line in done3.Log) { sb.Append("    log (e)   ").Append(line).Append('\n'); } }

                Article155(hosts, sb, Check, table23, poll23, poll27, pl1, f1, pl2);
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); } EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"SEJM FORMATION: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private const string KonstytucjaPage = "ElectionsData/poland/raw/records/trybunal_konstytucja.html";
        private static readonly string[] KonstytucjaWords =
        {
            "udziela jej wotum zaufania większością głosów w obecności co najmniej połowy ustawowej liczby posłów",   // Art. 155 ust. 1
            "Prezydent Rzeczypospolitej skraca kadencję Sejmu i zarządza wybory",                                    // Art. 155 ust. 2
            "nie później niż w ciągu 45 dni od dnia zarządzenia skrócenia kadencji Sejmu",                           // Art. 98 ust. 5
            "nie później niż na 15 dzień po dniu przeprowadzenia wyborów",                                           // Art. 98 ust. 5
            "W razie skrócenia kadencji Sejmu stosuje się odpowiednio przepis ust. 1",                               // Art. 98 ust. 6
        };
        private const string KodeksPage = "ElectionsData/poland/raw/records/eli_DU_2011_112_kodeks_wyborczy.html";
        private static readonly string[] KodeksWords =
        {
            "Wybory odbywają się w dniu wolnym od pracy",                                                            // art. 4 § 1
            "lub od dnia ogłoszenia postanowienia Prezydenta Rzeczypospolitej o skróceniu",                         // art. 195 § 1, the President's shortening's limb
            "w art. 202 § 3 - do 40 dnia przed dniem wyborów",                                                      // art. 195 § 2 pkt 1 lit. c, the floor (g) holds
        };
        private const string Order2007Page = "ElectionsData/poland/raw/shortened_term_2007/eli_search_DU_2007_wyborow_do_sejmu.json";
        private static readonly string[] Order2007Words =
        {
            "z dnia 7 września 2007 r. w sprawie zarządzenia wyborów do Sejmu",                                      // the President's order, Dz.U. 2007 nr 162 poz. 1145
            "\"art\":\"art. 98 ust. 5\"",                                                                            // its basis
            "przeprowadzonych w dniu 21 października 2007 r.",                                                       // the PKW's notice of the vote, poz. 1438
        };

        /// <summary>K302: clauses (f) to (k) - see the class.</summary>
        private static void Article155(List<GameObject> hosts, StringBuilder sb, Action<bool, string> Check, Dictionary<string, int> table23, DateTime poll23, DateTime poll27,
            Country pl1, SejmFormation f1, Country pl2)
        {
            // (f) the words, each page held to its SHA-256, and the one shortened term on record reproduced
            Check(PresidencyDiagnostic.Page(KonstytucjaPage, out string konstytucja) && Array.TrueForAll(KonstytucjaWords, konstytucja.Contains),
                F("(f) the Konstytucja (trybunal_konstytucja.html) held to its SHA-256, holding \"{0}\"", string.Join("\" · \"", KonstytucjaWords)));
            Check(PresidencyDiagnostic.Page(KodeksPage, out string kodeks) && Array.TrueForAll(KodeksWords, kodeks.Contains),
                F("(f) the Kodeks wyborczy [KW-2011, the 2011 original text] held to its SHA-256, holding \"{0}\" - its forty-five days run from the order's publication, on or after the order's day the model counts from, so every day the model gives is lawful under both texts", string.Join("\" · \"", KodeksWords)));
            Check(PresidencyDiagnostic.Page(Order2007Page, out string order2007) && Array.TrueForAll(Order2007Words, order2007.Contains),
                F("(f) the 2007 shortened term on record (ELI's listing, raw/shortened_term_2007) held to its SHA-256, holding \"{0}\"", string.Join("\" · \"", Order2007Words)));
            DateTime day2007 = SejmFormation.ShortenedTermElectionDay(new DateTime(2007, 9, 7));
            Check(day2007 == new DateTime(2007, 10, 21) && day2007.DayOfWeek == DayOfWeek.Sunday && SejmFormation.ShortenedTermElectionWithinDays == 45 && SejmFormation.ShortenedTermFirstSittingAfterDays == 15,
                F("(f) Art. 98 ust. 5's day reproduces 2007's: ordered 2007-09-07, elections on {0:yyyy-MM-dd} ({1}), the last Sunday in the forty-five days - the record's 21 Oct 2007; a Sejm so elected first sits on the fifteenth day", day2007, day2007.DayOfWeek));

            DeclarationReading reading27 = DeclarationReading.OfElection(CountryId.Poland, poll27);
            DateTime sitting27 = poll27.AddDays(30), sejmVote27 = sitting27.AddDays(SejmFormation.StepWindowDays);
            DateTime appointed27 = sejmVote27.AddDays(SejmFormation.PresidentAppointsAfterSejmVoteDays), confidence27 = appointed27.AddDays(SejmFormation.StepWindowDays);

            // (g) a planted 230/230 chamber on the player's day path
            Dictionary<string, int> hung = Zeroed(table23);
            hung["PiS"] = 230; hung["KO"] = 230;
            (SimulationManager s4, Country pl4) = Open(hosts, "PiS", hung, poll27);
            pl4.ElectionHistory.Add(Counted(poll27, hung));   // the count the night seated, as the game's own election adds it
            GovernmentFormation.View view4 = GovernmentFormation.ViewOf(pl4, reading27);
            SejmFormation.PresidentsCabinet named4 = SejmFormation.PresidentAppoints(pl4, reading27);
            GovernmentRecord outgoing4 = pl4.Government;
            SejmFormation f4 = s4.OpenSejmFormation(pl4, poll27);
            DateTime polling4 = SejmFormation.ShortenedTermElectionDay(confidence27);
            GovernmentRecord beforeAppointment4 = null, atAppointment4 = null, atConfidence4 = null;
            // The integrator's fix at §955 (the first Unity run): the records stay live while the loop steps on to the vote day, so each step's
            // state is read on its own day - read at the loop's end, the outgoing record's procedure has concluded and the President's cabinet is the caretaker.
            bool beforeOk4 = false, atAppointmentOk4 = false; string atAppointmentCabinet4 = null;
            string save4 = null, save4Vote = null;
            while (s4.CurrentDate < confidence27)
            {
                Days(s4, 1);
                if (s4.CurrentDate == appointed27.AddDays(-1))
                {
                    beforeAppointment4 = pl4.Government;
                    beforeOk4 = beforeAppointment4 != null && beforeAppointment4.Caretaker && beforeAppointment4.Sejm != null && beforeAppointment4.Sejm.Stage == SejmStage.PresidentAppoints && beforeAppointment4.Sejm.NextOn == appointed27;
                    save4 = Persistence.SaveGameService.Serialize(Persistence.SaveGameService.CreateSaveGame(s4, s4.World, CountryId.Poland, null));
                }
                if (s4.CurrentDate == appointed27)
                {
                    atAppointment4 = pl4.Government;
                    atAppointmentCabinet4 = atAppointment4 == null ? null : Sorted(atAppointment4.Cabinet);
                    atAppointmentOk4 = atAppointment4 != null && atAppointmentCabinet4 == Sorted(named4.Cabinet) && !atAppointment4.Caretaker && atAppointment4.Sejm != null && atAppointment4.Sejm.Stage == SejmStage.AwaitingPresidentsConfidence;
                }
                if (s4.CurrentDate == confidence27.AddDays(-1)) { save4Vote = Persistence.SaveGameService.Serialize(Persistence.SaveGameService.CreateSaveGame(s4, s4.World, CountryId.Poland, null)); }
                if (s4.CurrentDate == confidence27) { atConfidence4 = pl4.Government; }
            }
            Check(!view4.HasGovernment && named4.Cabinet.Count > 0 && !named4.Carried && named4.For == 230 && named4.Against == 230 && named4.Abstaining == 0 && named4.Carrying == 0,
                F("(g) planted PiS 230, KO 230 on {0:yyyy-MM-dd}: the formation forms none; no cabinet carries even a simple majority - the President's best, {1}", poll27, named4.Line()));
            Check(f4 != null && ReferenceEquals(beforeAppointment4, outgoing4) && beforeOk4,
                F("(g) {0:yyyy-MM-dd}: the outgoing cabinet resigned at the first sitting ({1:yyyy-MM-dd}) carries on - no designee, no Sejm's election on {2:yyyy-MM-dd}; the President's step due on {3:yyyy-MM-dd} (ust. 3's fourteen days, then his own)",
                    appointed27.AddDays(-1), sitting27, sejmVote27, appointed27));
            SejmFormation loaded4 = save4 != null ? Persistence.SaveGameService.Deserialize(save4).World.GetCountry(CountryId.Poland).Government.Sejm : null;
            Check(loaded4 != null && loaded4.Stage == SejmStage.PresidentAppoints && loaded4.NextOn == appointed27 && loaded4.Open,
                F("(g) a save cut on {0:yyyy-MM-dd} carries the procedure at the President's step: {1}, next on {2:yyyy-MM-dd}", appointed27.AddDays(-1), loaded4?.Stage.ToString() ?? "NONE", loaded4?.NextOn ?? DateTime.MinValue));
            // the art155 review's R2-L2: each save LOADED into a manager (`SaveGameService.RestoreInto`) and stepped one day - the procedure goes on from it
            (SimulationManager l4, Country lp4) = Loaded(hosts, save4);
            Days(l4, 1);
            GovernmentRecord loadedAppointed = lp4?.Government;
            Check(l4.CurrentDate == appointed27 && loadedAppointed != null && Sorted(loadedAppointed.Cabinet) == Sorted(named4.Cabinet) && !loadedAppointed.Caretaker && loadedAppointed.Sejm != null
                  && loadedAppointed.Sejm.Stage == SejmStage.AwaitingPresidentsConfidence && loadedAppointed.Sejm.NextOn == confidence27,
                F("(g) that save, loaded and stepped to {0:yyyy-MM-dd}: the President appoints {1}, its vote due {2:yyyy-MM-dd} - as the continuous run", l4.CurrentDate, loadedAppointed == null ? "NOTHING" : Sorted(loadedAppointed.Cabinet),
                    loadedAppointed?.Sejm?.NextOn ?? DateTime.MinValue));
            (SimulationManager l4v, Country lp4v) = Loaded(hosts, save4Vote);
            bool loadedAtVote = lp4v?.Government?.Sejm != null && lp4v.Government.Sejm.Stage == SejmStage.AwaitingPresidentsConfidence;
            Days(l4v, 1);
            Check(save4Vote != null && loadedAtVote && l4v.CurrentDate == confidence27 && lp4v.Government.Caretaker && lp4v.Government.CaretakerSince == confidence27 && lp4v.Government.Sejm != null && !lp4v.Government.Sejm.Open
                  && l4v.ExtraElectionDate == polling4,
                F("(g) a save cut on {0:yyyy-MM-dd} at the vote's step (AwaitingPresidentsConfidence), loaded and stepped to {1:yyyy-MM-dd}: refused, the term shortened, the elections on {2:yyyy-MM-dd} - as the continuous run",
                    confidence27.AddDays(-1), l4v.CurrentDate, l4v.ExtraElectionDate));
            Check(atAppointmentOk4,
                F("(g) {0:yyyy-MM-dd}: the President appoints {1} and it governs, the Sejm's confidence to follow (Art. 155 ust. 1)", appointed27, atAppointmentCabinet4 ?? "NOTHING"));
            SejmFormation done4 = atConfidence4?.Sejm;
            bool next4 = s4.TryPlayerPollingDay(out DateTime nextPolling4);
            Check(atConfidence4 != null && atConfidence4.Caretaker && atConfidence4.CaretakerSince == confidence27 && done4 != null && !done4.Open && s4.ExtraElectionDate == polling4 && next4 && nextPolling4 == polling4
                  && polling4.DayOfWeek == DayOfWeek.Sunday && polling4 <= confidence27.AddDays(45) && polling4 >= confidence27.AddDays(40) && done4.Log.Exists(l => l.Contains("Art. 155 ust. 2")),
                F("(g) THE SHORTENED TERM: {0:yyyy-MM-dd} the Sejm refuses its confidence, the President's cabinet resigns and carries on as caretaker; the President shortens the term - the player's next polling day {1:yyyy-MM-dd} ({2}, day {3}), the last Sunday within forty-five days (Art. 155 ust. 2, Art. 98 ust. 5; DECLARED, the 2007 practice) and no earlier than the fortieth (the Kodeks's shortened district notice, art. 195 § 2)",
                    confidence27, nextPolling4, nextPolling4.DayOfWeek, (nextPolling4 - confidence27).Days));
            if (done4 != null) { foreach (string line in done4.Log) { sb.Append("    log (g)   ").Append(line).Append('\n'); } }
            // the Sejm that election chooses: Art. 98 ust. 5's fifteenth day, and a term of its own (ust. 6)
            pl4.ElectionHistory.Add(Counted(polling4, hung));
            DateTime sitting4 = SejmFormation.FirstSittingAfter(pl4, polling4);
            bool ordinary4 = WorldCalendar.TryNextOrdinaryDay(pl4, polling4.AddDays(1), out DateTime reanchored4);
            WorldClock.TryNextPollingDay(CountryId.Poland, polling4.AddDays(1), out DateTime chain4);
            SejmFormation opened4 = SejmFormation.OpenAfter(pl4, polling4);
            Check(SejmFormation.ElectedAfterShortenedTerm(pl4, polling4) && sitting4 == polling4.AddDays(15) && WorldCalendar.FirstSittingAfter(pl4, polling4) == sitting4 && opened4.FirstSitting == sitting4
                  && ordinary4 && reanchored4 == WorldClock.FirstSundayOfTermWindow(sitting4) && reanchored4 != chain4,
                F("(g) the Sejm elected on {0:yyyy-MM-dd} first sits on {1:yyyy-MM-dd} (Art. 98 ust. 5's fifteenth day), and its term runs four years of its own (ust. 6): the next ordinary polling day {2:yyyy-MM-dd}, not the old term's chain {3:yyyy-MM-dd}",
                    polling4, sitting4, reanchored4, chain4));
            // the review's L1: on that ordinary election's own day, its count in, the calendar still answers the day - the old chain would answer its own next
            pl4.ElectionHistory.Add(Counted(reanchored4, hung));
            bool onTheDay4 = WorldCalendar.TryNextOrdinaryDay(pl4, reanchored4, out DateTime sameDay4);
            bool dayAfter4 = WorldCalendar.TryNextOrdinaryDay(pl4, reanchored4.AddDays(1), out DateTime following4);
            WorldClock.TryNextPollingDay(CountryId.Poland, reanchored4, out DateTime chainOnDay4);
            Check(onTheDay4 && sameDay4 == reanchored4 && dayAfter4 && following4 == WorldClock.FirstSundayOfTermWindow(reanchored4.AddDays(30)) && chainOnDay4 != reanchored4,
                F("(g) on that ordinary election's own day ({0:yyyy-MM-dd}), its count in, the calendar answers the day itself (where the old chain answers {1:yyyy-MM-dd}); the day after, the next term's {2:yyyy-MM-dd}",
                    sameDay4, chainOnDay4, following4));

            // (h) a simple-majority chamber on the player's day path
            Dictionary<string, int> simple = Zeroed(table23);
            simple["KO"] = 83; simple["Konf"] = 10; simple["NL"] = 35; simple["PiS"] = 220; simple["TD"] = 112;
            (SimulationManager s5, Country pl5) = Open(hosts, "Konf", simple, poll27);
            pl5.ElectionHistory.Add(Counted(poll27, simple));
            GovernmentFormation.View view5 = GovernmentFormation.ViewOf(pl5, reading27);
            SejmFormation.PresidentsCabinet named5 = SejmFormation.PresidentAppoints(pl5, reading27);
            SejmFormation f5 = s5.OpenSejmFormation(pl5, poll27);
            GovernmentRecord atAppointment5 = null;
            while (s5.CurrentDate < confidence27) { Days(s5, 1); if (s5.CurrentDate == appointed27) { atAppointment5 = pl5.Government; } }
            GovernmentRecord after5 = pl5.Government;
            Check(!view5.HasGovernment && named5.Carried && named5.For * 2 <= named5.Members && named5.Abstaining > 0 && named5.Carrying > 0,
                F("(h) planted {0} on {1:yyyy-MM-dd}: the formation forms none; the President's best of {2} cabinet(s) a simple majority carries - {3}", Seats(simple), poll27, named5.Carrying, named5.Line()));
            Check(f5 != null && atAppointment5 != null && ReferenceEquals(after5, atAppointment5) && Sorted(after5.Cabinet) == Sorted(named5.Cabinet) && !after5.Caretaker && after5.FormedOn <= appointed27
                  && after5.Sejm != null && !after5.Sejm.Open && s5.ExtraElectionDate == DateTime.MinValue && after5.Sejm.Log.Exists(l => l.Contains("carried, more for than against")),
                F("(h) {0:yyyy-MM-dd}: the President's {1} governs; {2:yyyy-MM-dd}: it holds the Sejm's confidence by a simple majority - the procedure concluded, no election ordered",
                    appointed27, after5 == null ? "NOTHING" : Sorted(after5.Cabinet), confidence27));
            if (after5?.Sejm != null) { foreach (string line in after5.Sejm.Log) { sb.Append("    log (h)   ").Append(line).Append('\n'); } }

            // (i) the same chambers in an AI Poland, stepped by the day loop (`AdvanceDay`, its `HoldWorldCalendar`): the formation run whole on the first sitting through
            // the calendar's own gate (`FormationDue` -> `FormAsAi`); the 230/230 chamber stepped on through its extra election's count and the new Sejm's first sitting
            foreach ((string name, Dictionary<string, int> seats, SejmFormation.PresidentsCabinet named) in new[] { ("230/230", hung, named4), ("simple-majority", simple, named5) })
            {
                (SimulationManager ai, Country poland) = OpenAbroad(hosts, seats, poll27, out ElectionRecord _);
                GovernmentRecord eve = poland.Government;
                typeof(SimulationManager).GetProperty("CurrentDate").SetValue(ai, sitting27.AddDays(-1));
                ai.AdvanceDay();
                GovernmentRecord after = poland.Government;
                FormationProjection projected = ai.LastCalendarFormation(CountryId.Poland);
                bool extra = WorldCalendar.TryExtraElection(poland, out DateTime extraDay, out DateTime orderedOn, out string why);
                if (named.Carried)
                {
                    Check(ai.CurrentDate == sitting27 && eve != null && !ReferenceEquals(after, eve) && Sorted(after.Cabinet) == Sorted(named.Cabinet) && after.FormedOn == sitting27 && !after.Caretaker && !extra
                          && projected != null && projected.Step.Contains("ART. 155 UST. 1"),
                        F("(i) an AI Poland, the {0} chamber, stepped to its first sitting {1:yyyy-MM-dd}: the steps install the President's {2} ({3}); no election ordered", name, sitting27, after == null ? "NOTHING" : Sorted(after.Cabinet), projected?.Step ?? "NO PROJECTION"));
                    continue;
                }
                // the review's L6: the caretaker is the President's refused cabinet (Art. 162 ust. 2 pkt 1, ust. 3), as in the player's (g) - installed and discharged on the sitting
                bool polls = WorldCalendar.IsChamberPollingDay(poland, extraDay, out bool asExtra);
                string governs = WorldCalendar.GovernsLine(poland, sitting27);
                Check(ai.CurrentDate == sitting27 && eve != null && !ReferenceEquals(after, eve) && Sorted(after.Cabinet) == Sorted(named.Cabinet) && after.FormedOn == sitting27 && after.Caretaker && after.CaretakerSince == sitting27
                      && extra && orderedOn == sitting27 && extraDay == SejmFormation.ShortenedTermElectionDay(sitting27) && polls && asExtra && why.Contains("Art. 155 ust. 2")
                      && projected != null && projected.Reason != null && projected.Reason.Contains("Art. 155 ust. 2") && governs.Contains("governs as caretaker; a new election on"),
                    F("(i) an AI Poland, the {0} chamber, stepped to its first sitting {1:yyyy-MM-dd}: no cabinet carries; the President's refused {2} carries on as caretaker, and the extra election on {3:yyyy-MM-dd} (the sitting + Art. 98 ust. 5) - \"{4}\"",
                        name, sitting27, after == null ? "NOTHING" : Sorted(after.Cabinet), extraDay, governs));
                // stepped on, day by day: the extra election counted on its day, the Sejm it elects formed on its fifteenth day by the calendar's own gate
                DateTime sitting2 = extraDay.AddDays(SejmFormation.ShortenedTermFirstSittingAfterDays);
                int countsBefore = poland.ElectionHistory.Count;
                while (ai.CurrentDate < sitting2) { ai.AdvanceDay(); }
                ElectionRecord extraCount = poland.ElectionHistory.Find(e => e.Date.Date == extraDay && NationalElection.CountsParliament(e.Method));
                GovernmentRecord formed2 = poland.Government;
                bool formedOnSitting2 = formed2 != null && formed2.FormedOn == sitting2 && !formed2.Caretaker;
                bool refusedAgain = formed2 != null && formed2.Caretaker && formed2.CaretakerSince == sitting2 && WorldCalendar.TryExtraElection(poland, out DateTime _, out DateTime again, out string _) && again == sitting2;
                Check(extraCount != null && poland.ElectionHistory.Count > countsBefore && SejmFormation.ElectedAfterShortenedTerm(poland, extraDay) && WorldCalendar.FirstSittingAfter(poland, extraDay) == sitting2
                      && (formedOnSitting2 || refusedAgain),
                    F("(i) stepped on: the extra election counted on {0:yyyy-MM-dd} ({1}); the Sejm it elects first sits on {2:yyyy-MM-dd}, and the steps run there - {3}",
                        extraDay, extraCount == null ? "NO COUNT" : Seats(extraCount.Seats), sitting2,
                        formedOnSitting2 ? Sorted(formed2.Cabinet) + " led by " + formed2.PmParty + " installed" : refusedAgain ? "refused again, the term shortened again (no limit, K302)" : "NOTHING RAN"));
            }

            // the review's L5 (and R2-L2: through the load itself): an AI Polish caretaker as a build before K302 left it (the outgoing cabinet discharged on the
            // sitting, no election held), saved after its owed day and LOADED (`SaveGameService.RestoreInto`) - ordered again on the load's day, by the same rule
            {
                (SimulationManager old, Country poland) = OpenAbroad(hosts, hung, poll27, out ElectionRecord _, seatPlayer: true);
                poland.Government.Discharge(sitting27);
                var loadedOn = new DateTime(2028, 3, 7);
                typeof(SimulationManager).GetProperty("CurrentDate").SetValue(old, loadedOn);
                bool owedBefore = WorldCalendar.TryExtraElection(poland, out DateTime pastDay, out DateTime _, out string _);
                string oldSave = Persistence.SaveGameService.Serialize(Persistence.SaveGameService.CreateSaveGame(old, old.World, CountryId.Sweden, null));
                (SimulationManager loadedOld, Country loadedPoland) = Loaded(hosts, oldSave);
                bool owedAfter = WorldCalendar.TryExtraElection(loadedPoland, out DateTime newDay, out DateTime newOrder, out string _);
                Check(owedBefore && pastDay < loadedOn && loadedOld.CurrentDate == loadedOn && owedAfter && newOrder == loadedOn && newDay == SejmFormation.ShortenedTermElectionDay(loadedOn) && newDay > loadedOn
                      && loadedPoland.Government.Caretaker && loadedPoland.Government.Breaks.Exists(l => l.Contains("were never held")),
                    F("(i) an older save's AI caretaker owing {0:yyyy-MM-dd}, loaded on {1:yyyy-MM-dd}: the load orders it again that day, the elections on {2:yyyy-MM-dd} (DECLARED, the same rule from the day found)", pastDay, loadedOn, newDay));
            }

            // (j) no Polish election of record moves: the 2023 count's first sitting and steps (b), an ordinary count's calendar the chain's
            Dictionary<string, int> ordinarySeats = new Dictionary<string, int>(pl2.ParliamentSeats);
            pl2.ElectionHistory.Add(Counted(poll27, ordinarySeats));
            bool chainNext = WorldClock.TryNextPollingDay(CountryId.Poland, poll27.AddDays(1), out DateTime chain27);
            bool calendarNext = WorldCalendar.TryNextOrdinaryDay(pl2, poll27.AddDays(1), out DateTime calendar27);
            Check(!SejmFormation.ElectedAfterShortenedTerm(pl1, poll23) && SejmFormation.FirstSittingAfter(pl1, poll23) == new DateTime(2023, 11, 13) && f1.FirstSitting == new DateTime(2023, 11, 13)
                  && !SejmFormation.ElectedAfterShortenedTerm(pl2, poll27) && SejmFormation.FirstSittingAfter(pl2, poll27) == poll27.AddDays(30) && chainNext && calendarNext && chain27 == calendar27,
                F("(j) the record's 2023 count first sits on 2023-11-13 and runs Art. 154 to Tusk's appointment ((b) above); an ordinary count ({0:yyyy-MM-dd}) first sits on the thirtieth day and its next polling day is the chain's {1:yyyy-MM-dd} (the calendar's {2:yyyy-MM-dd})",
                    poll27, chain27, calendar27));

            // (k) THE DECLARED SUPPORT LINES HONOURED (K301's filed finding)
            IReadOnlyList<PoliticalParty> parties = PartySystems.For(CountryId.Poland);
            int Ix(string key) { for (int p = 0; p < parties.Count; p++) { if (parties[p].Abbrev == key) { return p; } } return -1; }
            int pis = Ix("PiS"), konf = Ix("Konf"), td = Ix("TD");
            DeclarationReading reading43 = DeclarationReading.OfElection(CountryId.Poland, new DateTime(2043, 10, 25));
            List<RedLine> declared43 = reading43.Lines(CountryId.Poland, parties).FindAll(l => l.Kind == RedLineKind.Declared);
            bool pisCabinetOnly = declared43.Exists(l => l.Covers(pis, konf) && !l.BlocksSupport) && !declared43.Exists(l => l.RefusesSupport(pis, konf));
            bool konfOneWay = declared43.Exists(l => l.A == konf && l.B == pis && l.BlocksSupport && l.OneWay && l.RefusesSupport(konf, pis) && !l.RefusesSupport(pis, konf));   // the direction pinned
            bool tdRefusesKonf = declared43.Exists(l => l.RefusesSupport(td, konf));
            Check(pisCabinetOnly && konfOneWay && tdRefusesKonf,
                "(k) the declarations an in-run election reads (2023's, carried forward): PiS - Konf a cabinet line only (coalition_declarations_2023.md §1: \"A shared government is refused; support is not addressed\"); Konf -> PiS support-blocking ONE WAY (Konfederacja lends PiS's rule no hand; no source has it refuse PiS's support, §2); TD -> Konf support-blocking - so no declared line refuses PiS's support of a Konfederacja cabinet");
            var planted = new List<(string Name, DateTime Day, Dictionary<string, int> Seats, string Expected)>
            {
                ("main's 2043 chamber (seed 777)", new DateTime(2043, 10, 25), Plant(table23, ("KO", 87), ("Konf", 30), ("NL", 26), ("PiS", 229), ("TD", 88)), "Konf with PiS"),
                ("Konfederacja's seats would carry PiS", poll27, Plant(table23, ("PiS", 225), ("Konf", 10), ("KO", 100), ("TD", 90), ("NL", 35)), "Konf with PiS"),
            };
            foreach ((string name, DateTime day, Dictionary<string, int> seats, string expected) in planted)
            {
                var country = new Country { Id = CountryId.Poland, ParliamentSeats = seats };
                DeclarationReading reading = DeclarationReading.OfElection(CountryId.Poland, day);
                GovernmentFormation.View view = GovernmentFormation.ViewOf(country, reading);
                string formed = view.HasGovernment ? Sorted(view.Cabinet.ConvertAll(c => c.Abbrev)) + (view.Support.Count > 0 ? " with " + Sorted(view.Support.ConvertAll(c => c.Abbrev)) : string.Empty) : "none";
                string crossed = Crossed(parties, reading, view.Cabinet.ConvertAll(c => c.Abbrev), view.Support.ConvertAll(c => c.Abbrev));
                Check(view.HasGovernment && crossed == null && formed == expected,
                    F("(k) {0} ({1}): the formation installs {2}, pinned - and, CONSTRUCTIVE, no supporter a declared line refuses to the cabinet{3}", name, Seats(seats), formed, crossed != null ? "; CROSSED: " + crossed : string.Empty));
            }
            var konfCarriesPis = new Country { Id = CountryId.Poland, ParliamentSeats = planted[1].Seats };
            SejmFormation.PresidentsCabinet pisVote = SejmFormation.SimpleMajority(konfCarriesPis, new[] { "PiS" }, new string[0], reading27);
            var hung59 = new Country { Id = CountryId.Poland, ParliamentSeats = Plant(table23, ("KO", 80), ("Konf", 5), ("NL", 36), ("PiS", 225), ("TD", 114)) };
            DeclarationReading reading59 = DeclarationReading.OfElection(CountryId.Poland, new DateTime(2059, 11, 2));
            SejmFormation.PresidentsCabinet konfVote = SejmFormation.SimpleMajority(hung59, new[] { "Konf" }, new[] { "PiS" }, reading59);
            SejmFormation.PresidentsCabinet named59 = SejmFormation.PresidentAppoints(hung59, reading59);
            bool konfAgainstPis = pisVote.Sides.Exists(s => s.Abbrev == "Konf" && s.Side < 0), tdAgainstKonf = konfVote.Sides.Exists(s => s.Abbrev == "TD" && s.Side < 0);
            string crossed155 = Crossed(parties, reading27, named4.Cabinet, named4.Support) ?? Crossed(parties, reading27, named5.Cabinet, named5.Support) ?? Crossed(parties, reading59, named59.Cabinet, named59.Support);
            Check(konfAgainstPis && tdAgainstKonf && crossed155 == null,
                F("(k) Art. 155's vote keeps the lines: Konfederacja votes against a PiS cabinet ({0}); TD against a Konfederacja one ({1}); CONSTRUCTIVE (it re-applies `RedLine.RefusesSupport`, the predicate the formation draws its supporters by, so it holds only that the formation consults it - the direction is the pinned lines' above): the President's cabinets of (g), (h) and K300's 2059 chamber ({2}) carry no supporter a declared line refuses{3}",
                    pisVote.Line(), konfVote.Line(), named59.Line(), crossed155 != null ? "; CROSSED: " + crossed155 : string.Empty));
        }

        /// <summary>The first declared line a supporter crosses toward a cabinet member (`RedLine.RefusesSupport`, its own direction), as text; null where none.</summary>
        private static string Crossed(IReadOnlyList<PoliticalParty> parties, DeclarationReading reading, List<string> cabinet, List<string> support)
        {
            foreach (RedLine line in reading.Lines(CountryId.Poland, parties))
            {
                if (line.Kind != RedLineKind.Declared || !line.BlocksSupport) { continue; }
                foreach (string s in support)
                {
                    foreach (string c in cabinet)
                    {
                        int si = -1, ci = -1;
                        for (int p = 0; p < parties.Count; p++) { if (parties[p].Abbrev == s) { si = p; } if (parties[p].Abbrev == c) { ci = p; } }
                        if (si >= 0 && ci >= 0 && line.RefusesSupport(si, ci)) { return s + " carries " + c + " across " + parties[line.A].Abbrev + " -> " + parties[line.B].Abbrev; }
                    }
                }
            }
            return null;
        }

        private static Dictionary<string, int> Zeroed(Dictionary<string, int> like)
        {
            var seats = new Dictionary<string, int>(like);
            foreach (string k in new List<string>(seats.Keys)) { seats[k] = 0; }
            return seats;
        }

        private static Dictionary<string, int> Plant(Dictionary<string, int> like, params (string Key, int Seats)[] held)
        {
            Dictionary<string, int> seats = Zeroed(like);
            foreach ((string key, int n) in held) { seats[key] = n; }
            return seats;
        }

        private static string Seats(Dictionary<string, int> seats)
        {
            var parts = new List<string>();
            foreach (KeyValuePair<string, int> kv in seats) { if (kv.Value > 0) { parts.Add(kv.Key + " " + kv.Value.ToString(CultureInfo.InvariantCulture)); } }
            return string.Join(", ", parts);
        }

        /// <summary>The count a polling day seated, as the game's own election records it.</summary>
        private static ElectionRecord Counted(DateTime day, Dictionary<string, int> seats) =>
            new ElectionRecord { Date = day, CountryId = CountryId.Poland.ToString(), Method = ElectionMethod.PolandDistricts, Seats = new Dictionary<string, int>(seats) };

        /// <summary>A world at Sweden's start (the player's country Sweden), Poland an AI country with <paramref name="seats"/> seated by its count on <paramref name="pollingDay"/>.</summary>
        private static (SimulationManager, Country) OpenAbroad(List<GameObject> hosts, Dictionary<string, int> seats, DateTime pollingDay, out ElectionRecord counted, bool seatPlayer = false)
        {
            var go = new GameObject("SejmFormationDiagnostic.abroad." + hosts.Count.ToString(CultureInfo.InvariantCulture)); hosts.Add(go);
            WorldClock.ApplyStart(CountryId.Sweden);
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            sim.PlayerCountryId = CountryId.Sweden;
            if (seatPlayer)
            {
                // a world built as a new game builds one (WorldCalendarDiagnostic's OpenWorld): the start's prime minister's party, the record seated, the average cabinet
                Country sweden = world.GetCountry(CountryId.Sweden);
                sweden.PlayerPartyAbbrev = WorldClock.TryGovernmentAt(CountryId.Sweden, SimulationManager.EpochDate, out WorldClock.GovernmentOfRecord g)
                    ? SejmFormation.PartyOfHead(g.Head) ?? (g.Cabinet != null && g.Cabinet.Length > 0 ? g.Cabinet[0] : null) : null;
                typeof(SimulationManager).GetMethod("SeatTheRecordOnItsDate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(sim, null);
                CabinetSystem.SeatAverageCabinet(sweden);
            }
            Country poland = world.GetCountry(CountryId.Poland);
            poland.ParliamentSeats = new Dictionary<string, int>(seats);
            counted = Counted(pollingDay, seats);
            poland.ElectionHistory.Add(counted);
            if (poland.Government == null) { poland.Government = GovernmentRecord.OfNamedCabinet("KO", new[] { "KO", "TD", "NL" }, new string[0], pollingDay.AddYears(-4), "planted: the outgoing government"); }
            typeof(SimulationManager).GetProperty("CurrentDate").SetValue(sim, pollingDay);
            return (sim, poland);
        }

        /// <summary>The art155 review's R2-L2: a save LOADED as the game loads one - restored into a manager of its own (`SaveGameService.RestoreInto`, the player
        /// seated, the load's own steps run), the turn's market begun - and its Poland.</summary>
        private static (SimulationManager, Country) Loaded(List<GameObject> hosts, string save)
        {
            var go = new GameObject("SejmFormationDiagnostic.loaded." + hosts.Count.ToString(CultureInfo.InvariantCulture)); hosts.Add(go);
            SimulationManager sim = go.AddComponent<SimulationManager>();
            Persistence.SaveGameService.RestoreInto(sim, Persistence.SaveGameService.Deserialize(save));
            EnergyMarket.BeginTurn(sim.World);
            return (sim, sim.World.GetCountry(CountryId.Poland));
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
