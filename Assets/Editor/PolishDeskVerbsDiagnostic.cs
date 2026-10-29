using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// K197 (Code's recommendation under Elias's standing instruction of 2026-10-27; COAL-1's ask c): THE PLAYER'S POLISH DESK VERBS - leave the government
    /// and move an Art. 158 constructive vote - with their gates, their outcomes and what the Docket and the strip read of them. Each case plants the chamber
    /// and the government it needs on a Polish world (COAL-1's harness shape, `CoalitionExitDiagnostic`) and acts through the manager's own verbs.
    /// <para>(1) THE GATES, each refused with its reason where the verb does not apply, and the reader (`CanLeaveGovernment`, `CanMoveNoConfidence`) giving
    /// the verb's own words: the head of government and the opposition cannot leave; a partner and a supporter cannot move Art. 158 (leave or withdraw
    /// first); the head moves nothing against itself; a player-president's party is the AI's (SP-7b) for both.</para>
    /// <para>(2) ART. 158 AT THE LINE: PiS 231 against KO 229 elects PiS's candidate - 231 of the statutory 460 - and the successor's government is installed
    /// through the record's own method (`GovernmentRecord.FromProposal`, its basis the Sejm's choice); PiS 230 against KO 230 does not, the government
    /// stands and the failed vote is on the division log. The Docket's reader and the strip's place 4 read both: the strip on the motion's day only.</para>
    /// <para>(3) A PARTNER LEAVING CHANGES THE STANDING: NL leaves KO+TD+NL (240 of 460) and KO+TD govern on 230, a minority; no opposition candidate can reach
    /// 231 on the planted chamber, so COAL-1's procedure opens and moves nothing, and the record says so; the Docket reads the day and the standing; NL may
    /// now move Art. 158 and can no longer leave.</para>
    /// <para>(4) K270 (on the review of `wave/pldesk`): the player's constructive motion is NOT held by K196's quiet days, which rule an AI partner's exit - on a
    /// day inside 90 days of the 2023 Sejm election the motion is taken up and voted, as Germany's is.</para>
    /// <para>(5) The review's L1 - Germany's constructive row: a seatless opposition party (the FDP on a planted Bundestag) is refused by the reader in the verb's
    /// own words, so ELECT A SUCCESSOR draws disabled with that reason rather than offered and refused on the click.</para>
    /// </summary>
    public static class PolishDeskVerbsDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PolishDeskVerbsDiagnostic (K197; K195, K270): the player's Polish desk verbs - leave the government, move Art. 158 ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            SimulationRandom.LiveState live = SimulationRandom.CaptureLive();
            var hosts = new List<GameObject>();
            (SimulationManager, Country) Open(string name, DateTime date, string player)
            {
                var go = new GameObject(name); hosts.Add(go);
                SimulationManager.SetEpoch(date);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Poland;
                Country c = world.GetCountry(CountryId.Poland);
                c.PlayerPartyAbbrev = player;
                return (sim, c);
            }
            void Seat(Country c, Dictionary<string, int> seats)
            {
                foreach (string k in new List<string>(c.ParliamentSeats.Keys)) { c.ParliamentSeats[k] = 0; }
                foreach (KeyValuePair<string, int> kv in seats) { c.ParliamentSeats[kv.Key] = kv.Value; }
            }
            GovernmentRecord Govern(Country c, string pm, params string[] cabinet)
            {
                var g = new GovernmentRecord { PmParty = pm, Outcome = CoalitionOutcomeKind.MajorityCoalition.ToString(), FormedOn = new DateTime(2023, 12, 13), Kind = c.Government != null ? c.Government.Kind : default, Basis = "planted (PolishDeskVerbsDiagnostic)" };
                g.Cabinet.AddRange(cabinet);
                g.AllocatePortfolios(c);
                c.Government = g;
                return g;
            }
            DateTime clear = new DateTime(2024, 6, 1);   // more than 90 days from the 2023 Sejm election and from the 2025 presidential first vote
            var coalition = new Dictionary<string, int> { { "PiS", 200 }, { "KO", 165 }, { "TD", 65 }, { "NL", 10 }, { "Konf", 20 } };   // KO+TD+NL 240; KO+TD 230; PiS+Konf+NL 230
            try
            {
                Gates();
                AtTheLine();
                PartnerLeaves();
                NoQuietDays();
                GermanySeatless();
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); } EnergyMarket.ResetTurnState(); SimulationRandom.Reinstate(live); }
            if (failures > 0) { Debug.LogError($"POLISH DESK VERBS: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            sb.Append("PASS: the Polish desk verbs are gated by role and state with the verb's own reasons; Art. 158 carries at 231 of 460 and installs through the record, fails at 230 and is recorded; a partner's leaving re-reads the standing; no quiet days hold the player's motion (K270); Germany's seatless mover is refused by the reader\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);

            // (1) the gates
            void Gates()
            {
                (SimulationManager sim, Country pl) = Open("PolishDeskVerbsDiagnostic.gates", clear, "NL");
                var system = new List<string>(); foreach (PoliticalParty p in PartySystems.For(CountryId.Poland)) { system.Add(p.Abbrev); }
                bool listed = system.Contains("PiS") && system.Contains("KO") && system.Contains("TD") && system.Contains("NL") && system.Contains("Konf");
                Check(listed, F("(1) the Sejm seated on {0:yyyy-MM-dd} lists PiS, KO, TD, NL and Konf: {1}", clear, string.Join(", ", system)));
                if (!listed) { return; }
                Seat(pl, coalition);
                GovernmentRecord g = Govern(pl, "KO", "KO", "TD", "NL");
                bool mayLeave = sim.CanLeaveGovernment(CountryId.Poland, out string leaveWhy);
                bool mayMove = sim.CanMoveNoConfidence(CountryId.Poland, out string moveWhy);
                Check(mayLeave && leaveWhy == null && !mayMove && moveWhy == "LEAVE THE GOVERNMENT FIRST" && g.RoleOf("NL") == PlayerRole.JuniorPartner,
                    F("(1) NL, a partner of KO+TD+NL: may leave ({0}); Art. 158 refused - \"{1}\"", mayLeave ? "yes" : "NO - " + leaveWhy, moveWhy ?? "TAKEN UP"));
                bool movedAnyway = sim.MoveNoConfidence(CountryId.Poland, out string movedWhy, out _);
                Check(!movedAnyway && movedWhy == moveWhy, F("(1) the verb refuses as its reader says - \"{0}\"", movedWhy ?? "MOVED"));

                pl.PlayerPartyAbbrev = "KO";
                mayLeave = sim.CanLeaveGovernment(CountryId.Poland, out leaveWhy);
                mayMove = sim.CanMoveNoConfidence(CountryId.Poland, out moveWhy);
                bool leftAnyway = sim.LeaveGovernment(CountryId.Poland, out string leftWhy);
                Check(!mayLeave && leaveWhy == "YOUR PARTY IS NOT A JUNIOR PARTNER" && !leftAnyway && leftWhy == leaveWhy && !mayMove && moveWhy == "THE GOVERNMENT MOVES NO MOTION AGAINST ITSELF"
                      && g.Cabinet.Contains("KO"),
                    F("(1) KO, the head of government: leaving refused - \"{0}\" (the verb: \"{1}\"); Art. 158 refused - \"{2}\"", leaveWhy ?? "MAY", leftWhy ?? "LEFT", moveWhy ?? "TAKEN UP"));

                pl.PlayerPartyAbbrev = "PiS";
                mayLeave = sim.CanLeaveGovernment(CountryId.Poland, out leaveWhy);
                mayMove = sim.CanMoveNoConfidence(CountryId.Poland, out moveWhy);
                Check(!mayLeave && leaveWhy == "YOUR PARTY IS NOT A JUNIOR PARTNER" && mayMove && moveWhy == null,
                    F("(1) PiS, the opposition: leaving refused - \"{0}\"; Art. 158 may be moved ({1})", leaveWhy ?? "MAY", mayMove ? "yes" : "NO - " + moveWhy));

                pl.PlayerPartyAbbrev = "Konf";
                g.Support.Add("Konf");
                mayMove = sim.CanMoveNoConfidence(CountryId.Poland, out moveWhy);
                mayLeave = sim.CanLeaveGovernment(CountryId.Poland, out leaveWhy);
                Check(!mayMove && moveWhy == "WITHDRAW YOUR SUPPORT FIRST" && !mayLeave && leaveWhy == "YOUR PARTY IS NOT A JUNIOR PARTNER",
                    F("(1) Konf, a supporter: Art. 158 refused - \"{0}\"; leaving refused - \"{1}\"", moveWhy ?? "TAKEN UP", leaveWhy ?? "MAY"));
                g.Support.Remove("Konf");

                pl.PlayerPartyAbbrev = "NL";
                pl.PlayerHoldsPresidency = true;
                mayLeave = sim.CanLeaveGovernment(CountryId.Poland, out leaveWhy);
                pl.PlayerPartyAbbrev = "PiS";
                mayMove = sim.CanMoveNoConfidence(CountryId.Poland, out moveWhy);
                pl.PlayerHoldsPresidency = false;
                Check(!mayLeave && leaveWhy == SimulationManager.PresidentOfficeWords && !mayMove && moveWhy == SimulationManager.PresidentOfficeWords,
                    F("(1) SP-7b: a player-president's party is the AI's - leaving refused (\"{0}\"), Art. 158 refused (\"{1}\")", leaveWhy ?? "MAY", moveWhy ?? "TAKEN UP"));
            }

            // (2) Art. 158 at the line: 231 carries, 230 does not
            void AtTheLine()
            {
                (SimulationManager sim, Country pl) = Open("PolishDeskVerbsDiagnostic.231", clear, "PiS");
                Seat(pl, new Dictionary<string, int> { { "PiS", 231 }, { "KO", 229 } });
                GovernmentRecord g = Govern(pl, "KO", "KO");
                int version = g.Version;
                bool moved = sim.MoveNoConfidence(CountryId.Poland, out string refused, out ConfidenceProcedure.MotionVote vote);
                GovernmentRecord now = pl.Government;
                Check(moved && vote != null && vote.Carried && vote.For == 231 && vote.Needed == 231 && vote.Members == 460
                      && vote.Title() == "Constructive vote of no confidence in the Council of Ministers (KO): PiS's candidate elected, 231 of 460 deputies for, 231 needed (Art. 158 Konstytucja)",
                    F("(2) 231: PiS's motion - {0}", vote?.Title() ?? refused ?? "NOTHING"));
                Check(!ReferenceEquals(now, g) && now.PmParty == "PiS" && now.Cabinet.Contains("PiS") && !now.Cabinet.Contains("KO") && now.RoleOf("PiS") == PlayerRole.PrimeMinister
                      && now.Basis != null && now.Basis.Contains("the Sejm chose") && now.Basis.Contains("Art. 158") && g.Version == version,
                    F("(2) 231: the successor's government installed through the record's method - {0} led by {1}, basis \"{2}\"; the outgoing record untouched (version {3} -> {4})",
                        string.Join("+", now.Cabinet), now.PmParty, now.Basis, version, g.Version));
                DateTime today = sim.CurrentDate;
                List<DivisionRecord> read = PolishDeskRecord.RecentArt158(pl, today);
                DivisionRecord rec = read.Count > 0 ? read[0] : null;
                TopStrip.Chip chip = TopStrip.Art158Chip(pl, today);
                Check(rec != null && rec.Motion && rec.Passed && PolishDeskRecord.MoverOf(rec) == "PiS" && PolishDeskRecord.ForOf(rec) == 231 && PolishDeskRecord.MembersOf(rec) == 460
                      && PolishDeskRecord.Art158Words(pl, rec) == "Art. 158 · PiS's candidate elected"
                      && chip != null && chip.Words == "Art. 158 · PiS's candidate elected today" && chip.Face == TopStrip.Face.Amber && TopStrip.Art158Chip(pl, today.AddDays(1)) == null
                      && PolishDeskRecord.RecentArt158(pl, today.AddDays(WorldCalendar.ShownForDays)).Count == 0 && PolishDeskRecord.RecentArt158(pl, today.AddDays(WorldCalendar.ShownForDays - 1)).Count == 1,
                    F("(2) 231 on the Docket and the strip: \"{0}\"; place 4 \"{1}\" on the motion's day, none the day after; listed for {2} days",
                        rec != null ? PolishDeskRecord.Art158Words(pl, rec) : "NOT READ", chip?.Words ?? "NO CHIP", WorldCalendar.ShownForDays));
                bool again = sim.CanMoveNoConfidence(CountryId.Poland, out string againWhy);
                Check(!again && againWhy == "THE GOVERNMENT MOVES NO MOTION AGAINST ITSELF", F("(2) 231: governing now, the player moves nothing against itself - \"{0}\"", againWhy ?? "TAKEN UP"));

                (SimulationManager sim2, Country pl2) = Open("PolishDeskVerbsDiagnostic.230", clear, "PiS");
                Seat(pl2, new Dictionary<string, int> { { "PiS", 230 }, { "KO", 230 } });
                GovernmentRecord g2 = Govern(pl2, "KO", "KO");
                int motions = pl2.Divisions.Entries.Count;
                bool moved2 = sim2.MoveNoConfidence(CountryId.Poland, out string refused2, out ConfidenceProcedure.MotionVote vote2);
                DivisionRecord last = pl2.Divisions.Entries.Count > motions ? pl2.Divisions.Entries[pl2.Divisions.Entries.Count - 1] : null;
                TopStrip.Chip chip2 = TopStrip.Art158Chip(pl2, sim2.CurrentDate);
                Check(moved2 && vote2 != null && !vote2.Carried && vote2.For == 230 && vote2.Needed == 231 && ReferenceEquals(pl2.Government, g2) && g2.PmParty == "KO"
                      && last != null && last.Motion && !last.Passed && last.Title == vote2.Title()
                      && chip2 != null && chip2.Words == "Art. 158 · PiS's candidate not elected today",
                    F("(2) 230: PiS's motion fails and is recorded - \"{0}\"; KO governs on; place 4 \"{1}\"", last?.Title ?? refused2 ?? "NOTHING RECORDED", chip2?.Words ?? "NO CHIP"));
            }

            // (3) a partner leaving changes the government's standing
            void PartnerLeaves()
            {
                (SimulationManager sim, Country pl) = Open("PolishDeskVerbsDiagnostic.leave", clear, "NL");
                Seat(pl, coalition);
                GovernmentRecord g = Govern(pl, "KO", "KO", "TD", "NL");
                string before = g.Outcome;
                bool left = sim.LeaveGovernment(CountryId.Poland, out string refused);
                GovernmentRecord now = pl.Government;
                string standing = PolishDeskRecord.StandingWords(pl);
                Check(left && ReferenceEquals(now, g) && !g.Cabinet.Contains("NL") && g.RoleOf("NL") == PlayerRole.Opposition
                      && before == CoalitionOutcomeKind.MajorityCoalition.ToString() && g.Outcome == CoalitionOutcomeKind.MinorityGovernment.ToString()
                      && standing.EndsWith("govern on 230 of 460 seats, a minority", StringComparison.Ordinal),
                    F("(3) NL leaves KO+TD+NL: {0} -> {1}; \"{2}\"{3}", before, g.Outcome, standing, left ? string.Empty : " - REFUSED " + refused));
                bool procedure = g.Breaks.Exists(b => b.Contains("after NL left, no motion is moved"));
                Check(procedure && g.Breaks.Contains(SimulationManager.LeftTheGovernmentLine(sim.CurrentDate, "NL")),
                    F("(3) COAL-1's procedure opened and moved nothing - no candidate reaches 231 on 460: \"{0}\"", g.Breaks.Count > 0 ? g.Breaks[g.Breaks.Count - 1] : "NO LINE"));
                bool read = PolishDeskRecord.TryLeftOn(pl, sim.CurrentDate, out DateTime day);
                bool expired = PolishDeskRecord.TryLeftOn(pl, sim.CurrentDate.AddDays(WorldCalendar.ShownForDays), out _);
                Check(read && day == sim.CurrentDate.Date && !expired, F("(3) the Docket reads the day NL left ({0:yyyy-MM-dd}) for {1} days", day, WorldCalendar.ShownForDays));
                bool mayMove = sim.CanMoveNoConfidence(CountryId.Poland, out string moveWhy);
                bool mayLeave = sim.CanLeaveGovernment(CountryId.Poland, out string leaveWhy);
                Check(mayMove && !mayLeave && leaveWhy == "YOUR PARTY IS NOT A JUNIOR PARTNER",
                    F("(3) in opposition now: Art. 158 may be moved ({0}); leaving refused - \"{1}\"", mayMove ? "yes" : "NO - " + moveWhy, leaveWhy ?? "MAY"));
            }

            // (4) K270: no quiet days on the player's motion
            void NoQuietDays()
            {
                DateTime inside = new DateTime(2024, 1, 1);   // 78 days after the 2023 Sejm election (15 October 2023) - inside K130's 90 had they applied
                (SimulationManager sim, Country pl) = Open("PolishDeskVerbsDiagnostic.noquiet", inside, "PiS");
                Seat(pl, new Dictionary<string, int> { { "PiS", 230 }, { "KO", 230 } });
                GovernmentRecord g = Govern(pl, "KO", "KO");
                bool may = sim.CanMoveNoConfidence(CountryId.Poland, out string why);
                bool moved = sim.MoveNoConfidence(CountryId.Poland, out string refused, out ConfidenceProcedure.MotionVote vote);
                Check(may && why == null && moved && vote != null && !vote.Carried && ReferenceEquals(pl.Government, g),
                    F("(4) K270: on {0:yyyy-MM-dd}, 78 days after a polling day, PiS's motion is taken up and voted - {1}", inside, vote?.Title() ?? why ?? refused ?? "NOTHING"));
            }

            // (5) the review's L1: Germany's seatless mover, refused by the reader
            void GermanySeatless()
            {
                DateTime day = new DateTime(2025, 9, 1);
                var go = new GameObject("PolishDeskVerbsDiagnostic.de"); hosts.Add(go);
                SimulationManager.SetEpoch(day);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Germany;
                Country de = world.GetCountry(CountryId.Germany);
                de.PlayerPartyAbbrev = "FDP";
                Seat(de, new Dictionary<string, int> { { "CDU", 316 }, { "SPD", 314 } });
                var g = new GovernmentRecord { PmParty = "CDU", Outcome = CoalitionOutcomeKind.MajorityCoalition.ToString(), FormedOn = day.AddDays(-1), Kind = de.Government != null ? de.Government.Kind : default, Basis = "planted (PolishDeskVerbsDiagnostic)" };
                g.Cabinet.AddRange(new[] { "CDU", "SPD" });
                g.AllocatePortfolios(de);
                de.Government = g;
                bool may = sim.CanMoveNoConfidence(CountryId.Germany, out string why);
                bool moved = sim.MoveNoConfidence(CountryId.Germany, out string refused, out _);
                Check(!may && why == "YOUR PARTY HOLDS NO SEAT - A CONSTRUCTIVE VOTE IS MOVED BY MEMBERS" && !moved && refused == why,
                    F("(5) L1: Germany's seatless FDP - the reader refuses (\"{0}\") as the verb does (\"{1}\"), so the row draws its act disabled with that reason", why ?? "TAKEN UP", refused ?? "MOVED"));
            }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
