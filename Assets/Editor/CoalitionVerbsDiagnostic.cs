using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
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
    /// COAL-2 (RULINGS_2026-10-18 q1, q19; K136, K137; Design's §9 return of 2026-10-30): THE COALITION VERBS - each acted through the manager's own verb on a
    /// planted chamber and government (COAL-1's harness shape, `CoalitionExitDiagnostic`), the open motions settled by THE GAME'S OWN DAY TICK
    /// (`SimulationManager.AdvanceCountryDayTick`, the controller's call - the COAL-2 review's M2: a step reached by reflection passed with the day loop
    /// unplugged), the clock set as `SejmFormationDiagnostic` sets it; only where the tick's other steps would change the planted state (a round opened on a
    /// government formed before the planted count, a world with no player) is the coalition step reached alone, and the case says so.
    /// <para>(1) THE GATES by role, each refusal in the verb's own words: a junior partner threatens and nothing else; the head dismisses a partner, never
    /// itself, a party outside its cabinet or one member of a parliamentary group alone (§705, the CSU); a support party enforces; no one re-declares before a
    /// count; the opposition does none of the four.</para>
    /// <para>(2) A THREAT: made with a demand from the threatener's own list and a deadline of the declared choices; one at a time; MET when its demand is
    /// delivered (the agreement's own test) - the threatener stays; CARRIED on its deadline undelivered - the threatener leaves by the leaving's own path, the
    /// leaving on the record of verbs and the exit opening the confidence procedure (COAL-1's break line); WITHDRAWN by the verb; carried before its day where the
    /// threatener leaves; the strip's place 4 while it stands and place 3 the day it is carried out. The review's M1: a threat LAPSES once another record
    /// governs (the same parties re-formed), and on its old deadline the threatener stays.</para>
    /// <para>(3) A DISMISSAL: the partner struck, the posts re-apportioned, the dismissal on the record of verbs and the government's breaks, the confidence
    /// procedure opened (q21: a partner's exit), the strip's place 3; K59 on it: every chair was the head's before and after (`CabinetSystem.ChairIsThePlayers`),
    /// so no chair is seated and none changes its minister or its vacancy. K130's AI exit goes on the record of verbs too.</para>
    /// <para>(4) AN ENFORCEMENT: an owed item called due; MET when delivered; REFUSED on its deadline undelivered, the item BROKEN that day; the strip's place 4.
    /// M1: a demand LAPSES once another record governs, and the new government's item under the same key is never broken by it.</para>
    /// <para>(5) K137 as K330 narrows it, RE-DECLARING: after a planted count whose government is in office, each seated party but the player's declares FOR
    /// where it sits in the cabinet or its recorded support, AGAINST only where `GovernmentFormation.RedLinedFrom` holds it, and nothing otherwise; once per
    /// count; Sweden's bloc reads the re-declared side from its day and the side of record before it (`NationalElection.BlocOf`, the tactical layer's
    /// `SwedenBlocsOn` on a country); an S minority with no support recorded - V, MP and C re-declare nothing and keep their standing blocs, a seatless party
    /// nothing; the player's own verb once per count, FOR only from inside the government; Germany's sides recorded and no bloc read; a world with no player
    /// country re-declares nothing; the count's call hands the tactical layer the played world (`played: country`, read in the source).</para>
    /// <para>(6) The preview's clone carries the record of verbs as its own list of its own copies.</para>
    /// </summary>
    public static class CoalitionVerbsDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CoalitionVerbsDiagnostic (COAL-2; K136, K137): threat, dismissal, enforcement, re-declaration ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            SimulationRandom.LiveState live = SimulationRandom.CaptureLive();
            var hosts = new List<GameObject>();
            MethodInfo dayStep = typeof(SimulationManager).GetMethod("AdvanceCoalitionVerbsDay", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo clock = typeof(SimulationManager).GetProperty("CurrentDate");
            (SimulationManager, Country) Open(string name, CountryId id, DateTime date, string player)
            {
                var go = new GameObject(name); hosts.Add(go);
                SimulationManager.SetEpoch(date);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                if (player != null) { sim.PlayerCountryId = id; }
                Country c = world.GetCountry(id);
                c.PlayerPartyAbbrev = player;
                return (sim, c);
            }
            void Seat(Country c, Dictionary<string, int> seats)
            {
                foreach (string k in new List<string>(c.ParliamentSeats.Keys)) { c.ParliamentSeats[k] = 0; }
                foreach (KeyValuePair<string, int> kv in seats) { c.ParliamentSeats[kv.Key] = kv.Value; }
            }
            GovernmentRecord Govern(Country c, string pm, DateTime on, string[] cabinet, string[] support)
            {
                var g = new GovernmentRecord { PmParty = pm, Outcome = CoalitionOutcomeKind.MajorityCoalition.ToString(), FormedOn = on, Kind = c.Government != null ? c.Government.Kind : default, Basis = "planted (CoalitionVerbsDiagnostic)" };
                g.Cabinet.AddRange(cabinet);
                g.Support.AddRange(support);
                g.AllocatePortfolios(c);
                c.Government = g;
                return g;
            }
            void Step(SimulationManager sim, CountryId id) => sim.AdvanceCountryDayTick(id);   // the game's own day tick (M2) - it fails with the coalition step unplugged
            void CoalitionStepAlone(SimulationManager sim, CountryId id) => dayStep.Invoke(sim, new object[] { id });
            void SetDay(SimulationManager sim, DateTime d) => clock.SetValue(sim, d);
            void Deliver(Country c, string key, float target, DateTime on)
            {
                if (key.StartsWith("law:", StringComparison.Ordinal)) { c.EnactedLaws.Add(new EnactedLaw { LawId = key.Substring(4), EnactedOn = on }); }
                else if (key == "dial:" + AgreementDial.BorderEnforcement) { c.BorderEnforcementLevel = target; }
                else { c.PoliceFundingLevel = target; }
            }
            int Count(Country c, CoalitionVerb verb, string actor) { int n = 0; foreach (CoalitionMotion m in c.CoalitionMotions) { if (m.Verb == verb && (actor == null || m.Actor == actor)) { n++; } } return n; }
            string Last(List<string> breaks) => breaks.Count > 0 ? breaks[breaks.Count - 1] : "NONE";
            bool AnyBreak(GovernmentRecord g, string fragment) { foreach (string b in g.Breaks) { if (b.Contains(fragment)) { return true; } } return false; }
            // q21 (K131): the exit opened the procedure - a motion moved (the Riksdag's week), a successor installed (a constructive vote), or the break line that says why none was
            bool ProcedureOpened(Country c, GovernmentRecord g, string departed) => !ReferenceEquals(c.Government, g) || g.NoConfidenceOn != DateTime.MinValue || AnyBreak(g, "after " + departed + " left");
            var seats = new Dictionary<string, int> { { "S", 95 }, { "M", 74 }, { "SD", 71 }, { "KD", 27 }, { "V", 23 }, { "C", 22 }, { "MP", 21 }, { "L", 16 } };   // GA-4-1's chamber, 349
            DateTime clear = new DateTime(2027, 3, 1);
            DateTime formed = new DateTime(2026, 10, 20);
            if (dayStep == null || clock == null || clock.SetMethod == null)
            {
                Debug.LogError("COALITION VERBS: the day's step (AdvanceCoalitionVerbsDay) or the clock's setter is not reachable - VERIFIED NOTHING");
                CheckExit.Finish(1);
                return;
            }
            try
            {
                Gates();
                Threat();
                Dismissal();
                Enforcement();
                Redeclaring();
                Minority();
                Guard();
                AgainstStaysOut();
                NoBlocHead();
                PreviewClone();
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); } EnergyMarket.ResetTurnState(); SimulationRandom.Reinstate(live); }
            if (failures > 0) { Debug.LogError($"COALITION VERBS: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            sb.Append("PASS: the four verbs gated by role in their own words, settled by the game's day tick; a threat met, carried out (the leaving's act, the procedure opened), withdrawn, lapsed with its government; a dismissal opens the procedure and seats no chair; K130's exit recorded; an enforcement met, refused (the item broken), lapsed; K137 as K330 narrows it - FOR in or behind the cabinet, AGAINST only where red-lined, nothing otherwise (an S minority: V, MP, C keep their blocs), once, read as Sweden's blocs from their day, the count's call handing the played world; the player's own re-declaration; Germany recorded; no player, nothing; the clone's own copies\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);

            // (1) the gates
            void Gates()
            {
                (SimulationManager sim, Country se) = Open("CoalitionVerbsDiagnostic.gates", CountryId.Sweden, clear, "L");
                Seat(se, seats);
                GovernmentRecord g = Govern(se, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                g.FormAgreements(se, formed, sim.World);
                bool threaten = sim.CanThreaten(CountryId.Sweden, out string threatenWhy);
                bool dismiss = sim.CanDismiss(CountryId.Sweden, "KD", out string dismissWhy);
                bool enforce = sim.CanEnforce(CountryId.Sweden, out string enforceWhy);
                bool redeclare = sim.CanRedeclare(CountryId.Sweden, BlocSide.For, out string redeclareWhy);
                Check(threaten && !dismiss && dismissWhy == "ONLY THE HEAD OF GOVERNMENT'S PARTY DISMISSES A PARTNER" && !enforce && enforceWhy == "YOUR PARTY IS NOT A SUPPORT PARTY"
                      && !redeclare && redeclareWhy == "NO COUNT HELD YET - A PARTY RE-DECLARES AFTER ONE",
                    F("(1) L, a junior partner: may threaten ({0}); dismissal \"{1}\"; enforcement \"{2}\"; re-declaring \"{3}\"", threaten ? "yes" : "NO - " + threatenWhy, dismissWhy ?? "MAY", enforceWhy ?? "MAY", redeclareWhy ?? "MAY"));

                se.PlayerPartyAbbrev = "M";
                threaten = sim.CanThreaten(CountryId.Sweden, out threatenWhy);
                dismiss = sim.CanDismiss(CountryId.Sweden, "KD", out dismissWhy);
                bool self = sim.CanDismiss(CountryId.Sweden, "M", out string selfWhy);
                bool outside = sim.CanDismiss(CountryId.Sweden, "SD", out string outsideWhy);
                Check(!threaten && threatenWhy == "YOUR PARTY IS NOT A JUNIOR PARTNER" && dismiss && dismissWhy == null && !self && selfWhy == "M IS NOT A PARTNER IN YOUR CABINET" && !outside && outsideWhy == "SD IS NOT A PARTNER IN YOUR CABINET",
                    F("(1) M, the head: threat \"{0}\"; may dismiss KD ({1}); itself \"{2}\"; its supporter \"{3}\"", threatenWhy ?? "MAY", dismiss ? "yes" : "NO - " + dismissWhy, selfWhy ?? "MAY", outsideWhy ?? "MAY"));

                se.PlayerPartyAbbrev = "SD";
                enforce = sim.CanEnforce(CountryId.Sweden, out enforceWhy);
                threaten = sim.CanThreaten(CountryId.Sweden, out threatenWhy);
                Check(enforce && !threaten && threatenWhy == "YOUR PARTY IS NOT A JUNIOR PARTNER",
                    F("(1) SD, the supporter: may enforce ({0}; {1} item(s) owed); threat \"{2}\"", enforce ? "yes" : "NO - " + enforceWhy, CoalitionVerbs.OwedOf(g.AgreementOf("SD")).Count, threatenWhy ?? "MAY"));

                se.PlayerPartyAbbrev = "S";
                bool acted = sim.Threaten(CountryId.Sweden, "law:x", 30, out string actWhy);
                Check(!sim.CanThreaten(CountryId.Sweden, out _) && !sim.CanDismiss(CountryId.Sweden, "KD", out _) && !sim.CanEnforce(CountryId.Sweden, out _) && !acted
                      && actWhy == "YOUR PARTY IS NOT A JUNIOR PARTNER" && se.CoalitionMotions.Count == 0,
                    F("(1) S, the opposition: none of the verbs; the threat's verb refuses as its reader - \"{0}\"; nothing recorded", actWhy ?? "ACTED"));

                (SimulationManager deSim, Country de) = Open("CoalitionVerbsDiagnostic.csu", CountryId.Germany, new DateTime(2025, 9, 1), "CDU");
                Govern(de, "CDU", new DateTime(2025, 5, 6), new[] { "CDU", "CSU", "SPD" }, new string[0]);
                bool csu = deSim.CanDismiss(CountryId.Germany, "CSU", out string csuWhy);
                bool spd = deSim.CanDismiss(CountryId.Germany, "SPD", out string spdWhy);
                Check(!csu && csuWhy != null && csuWhy.Contains("ONE FRAKTION") && spd,
                    F("(1) §705: the CDU's chancellor does not dismiss the CSU alone - \"{0}\"; the SPD may be dismissed ({1})", csuWhy ?? "MAY", spd ? "yes" : "NO - " + spdWhy));
            }

            // (2) a threat: met, carried out, withdrawn, carried before its day
            void Threat()
            {
                (SimulationManager sim, Country se) = Open("CoalitionVerbsDiagnostic.threat", CountryId.Sweden, clear, "L");
                Seat(se, seats);
                GovernmentRecord g = Govern(se, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                List<AgreementItem> demands = sim.ThreatDemands(CountryId.Sweden);
                sb.Append(F("    measured  (2) L's demands on M's cabinet: {0}\n", string.Join(" · ", demands.ConvertAll(d => SupportAgreement.KeyOf(d)))));
                if (demands.Count < 2) { Check(false, F("(2) L holds {0} demand(s) the lead's line allows - two are needed to walk met and carried; VERIFIED NOTHING", demands.Count)); return; }
                AgreementItem first = demands[0], second = demands[1];
                bool refusedDays = !sim.Threaten(CountryId.Sweden, SupportAgreement.KeyOf(first), 45, out string daysWhy);
                bool made = sim.Threaten(CountryId.Sweden, SupportAgreement.KeyOf(first), 30, out string madeWhy);
                CoalitionMotion threat = CoalitionVerbs.OpenOf(se, CoalitionVerb.Threaten, "L");
                bool again = sim.CanThreaten(CountryId.Sweden, out string againWhy);
                var coming = new List<TopStrip.Chip>();
                TopStrip.CoalitionChips(se, sim.CurrentDate, null, coming);
                Check(refusedDays && made && threat != null && threat.Target == "M" && threat.Deadline == clear.AddDays(30) && Last(g.Breaks).Contains("threatened to leave") && !again && againWhy == "YOUR THREAT STANDS UNTIL ITS DAY"
                      && coming.Exists(chip => chip.Words == "L threatens · " + CoalitionVerbs.ShortDay(clear.AddDays(30))),
                    F("(2) L threatens M's cabinet: 45 days refused (\"{0}\"); '{1}' by {2:yyyy-MM-dd} ({3}); a second refused - \"{4}\"; the strip's place 4: {5}", daysWhy, first.Name, clear.AddDays(30), madeWhy ?? "made", againWhy ?? "MAY",
                        coming.Count > 0 ? coming[0].Words : "NONE"));
                Deliver(se, SupportAgreement.KeyOf(first), first.Target, sim.CurrentDate);
                SetDay(sim, clear.AddDays(5));
                Step(sim, CountryId.Sweden);
                Check(threat.State == MotionState.Met && g.Cabinet.Contains("L"), F("(2) delivered on day 5: the threat is {0}, L {1} ('{2}')", threat.State, g.Cabinet.Contains("L") ? "stays" : "LEFT", threat.Outcome));

                bool second_ = sim.Threaten(CountryId.Sweden, SupportAgreement.KeyOf(second), 30, out string secondWhy);
                CoalitionMotion carried = CoalitionVerbs.OpenOf(se, CoalitionVerb.Threaten, "L");
                SetDay(sim, clear.AddDays(34));
                Step(sim, CountryId.Sweden);
                bool stillOpen = carried != null && carried.IsOpen;
                SetDay(sim, clear.AddDays(35));
                Step(sim, CountryId.Sweden);
                var binds = new List<TopStrip.Chip>();
                TopStrip.CoalitionChips(se, sim.CurrentDate, binds, null);
                CoalitionMotion leaving = CoalitionVerbs.LatestOf(se, CoalitionVerb.Leave, "L");
                Check(second_ && stillOpen && carried.State == MotionState.Carried && !se.Government.Cabinet.Contains("L") && leaving != null && leaving.ByPlayer && leaving.On == clear.AddDays(35)
                      && AnyBreak(g, SimulationManager.LeftTheGovernmentLine(clear.AddDays(35), "L")) && ProcedureOpened(se, g, "L") && binds.Exists(chip => chip.Words == "L has left"),
                    F("(2) '{0}' undelivered: open the day before ({1}), carried out on its day ({2}) - L left M's cabinet, the leaving recorded ({3}), the procedure opened - \"{4}\"; the strip's place 3: {5}",
                        second.Name, stillOpen, carried?.State.ToString() ?? "NONE", leaving != null ? "yes" : "NO", Last(se.Government.Breaks), binds.Count > 0 ? binds[0].Words : "NONE"));
                if (!second_) { sb.Append("    note      the second threat was refused: " + secondWhy + "\n"); }

                (SimulationManager sim2, Country se2) = Open("CoalitionVerbsDiagnostic.withdraw", CountryId.Sweden, clear, "L");
                Seat(se2, seats);
                GovernmentRecord g2 = Govern(se2, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                sim2.Threaten(CountryId.Sweden, SupportAgreement.KeyOf(first), 60, out _);
                CoalitionMotion withdrawn = CoalitionVerbs.OpenOf(se2, CoalitionVerb.Threaten, "L");
                bool took = sim2.WithdrawThreat(CountryId.Sweden, out string tookWhy);
                bool twice = sim2.WithdrawThreat(CountryId.Sweden, out string twiceWhy);
                sim2.Threaten(CountryId.Sweden, SupportAgreement.KeyOf(first), 60, out _);
                CoalitionMotion early = CoalitionVerbs.OpenOf(se2, CoalitionVerb.Threaten, "L");
                bool left = sim2.LeaveGovernment(CountryId.Sweden, out _);
                Check(took && withdrawn != null && withdrawn.State == MotionState.Withdrawn && !twice && twiceWhy == "NO THREAT OF YOURS STANDS" && left && early != null && early.State == MotionState.Carried && !g2.Cabinet.Contains("L")
                      && Count(se2, CoalitionVerb.Leave, "L") == 1,
                    F("(2) withdrawn by the verb ({0}); a second withdrawal \"{1}\"; a threat standing when L leaves is carried out before its day ({2})", tookWhy ?? withdrawn?.State.ToString(), twiceWhy ?? "TAKEN", early?.State.ToString() ?? "NONE"));

                // M1: the same parties re-formed on another record (a count's round) - the threat made to the first lapses, and on its old deadline L stays
                (SimulationManager sim3, Country se3) = Open("CoalitionVerbsDiagnostic.lapse", CountryId.Sweden, clear, "L");
                Seat(se3, seats);
                Govern(se3, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                bool lapseMade = sim3.Threaten(CountryId.Sweden, SupportAgreement.KeyOf(first), 60, out string lapseWhy);
                CoalitionMotion lapse = CoalitionVerbs.OpenOf(se3, CoalitionVerb.Threaten, "L");
                string madeTo = lapse?.Government;
                GovernmentRecord reformed = Govern(se3, "M", clear.AddDays(10), new[] { "M", "KD", "L" }, new[] { "SD" });
                SetDay(sim3, clear.AddDays(11));
                Step(sim3, CountryId.Sweden);
                SetDay(sim3, clear.AddDays(60));
                Step(sim3, CountryId.Sweden);
                // the second review's N2: the key changes only when the government does - not when a count's procedure is written onto the record (Poland's polling
                // night), but at the resignation (a caretaker) and at an appointment of the same record again (Art. 154 ust. 1)
                var keyed = new GovernmentRecord { PmParty = "KO", FormedOn = new DateTime(2023, 12, 13), Basis = "planted (CoalitionVerbsDiagnostic)" };
                keyed.Cabinet.AddRange(new[] { "KO", "TD", "NL" });
                string k0 = CoalitionVerbs.GovernmentKey(keyed);
                keyed.Sejm = new SejmFormation { PollingDay = new DateTime(2027, 10, 17), Stage = SejmStage.AwaitingSitting };
                string kNight = CoalitionVerbs.GovernmentKey(keyed);
                keyed.Discharge(new DateTime(2027, 11, 12));
                string kResigned = CoalitionVerbs.GovernmentKey(keyed);
                keyed.Reappoint(new DateTime(2027, 11, 26));
                string kAgain = CoalitionVerbs.GovernmentKey(keyed);
                Check(kNight == k0 && kResigned != k0 && kAgain != k0 && kAgain != kResigned,
                    F("(2) N2: the key {0} holds on the polling night ({1}), changes at the resignation ({2}) and again at the appointment of the same record ({3})", k0, kNight, kResigned, kAgain));
                (SimulationManager sim6, Country se6) = Open("CoalitionVerbsDiagnostic.resign", CountryId.Sweden, clear, "L");
                Seat(se6, seats);
                GovernmentRecord g6 = Govern(se6, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                sim6.Threaten(CountryId.Sweden, SupportAgreement.KeyOf(first), 60, out _);
                CoalitionMotion resigned = CoalitionVerbs.OpenOf(se6, CoalitionVerb.Threaten, "L");
                g6.Discharge(clear.AddDays(3));
                SetDay(sim6, clear.AddDays(3));
                CoalitionStepAlone(sim6, CountryId.Sweden);   // alone: the confidence day would open the discharged government's round
                Check(resigned != null && resigned.State == MotionState.Withdrawn && resigned.Outcome != null && resigned.Outcome.Contains("caretaker") && g6.Cabinet.Contains("L"),
                    F("(2) N2: the cabinet resigns (a caretaker, the same record) - the threat {0}: '{1}'; L {2}", resigned?.State.ToString() ?? "NONE", resigned?.Outcome, g6.Cabinet.Contains("L") ? "stays" : "LEFT"));
                // N3 (c): the partner's own lapse - L struck from the same record (the key unchanged), so the threat lapses by its own line, not the key's
                (SimulationManager sim9, Country se9) = Open("CoalitionVerbsDiagnostic.partner", CountryId.Sweden, clear, "L");
                Seat(se9, seats);
                GovernmentRecord g9 = Govern(se9, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                sim9.Threaten(CountryId.Sweden, SupportAgreement.KeyOf(first), 60, out _);
                CoalitionMotion partner = CoalitionVerbs.OpenOf(se9, CoalitionVerb.Threaten, "L");
                string key9 = CoalitionVerbs.GovernmentKey(g9);
                g9.LeaveCabinet("L", se9);
                SetDay(sim9, clear.AddDays(2));
                Step(sim9, CountryId.Sweden);
                Check(partner != null && partner.State == MotionState.Withdrawn && partner.Outcome != null && partner.Outcome.Contains("no longer a partner") && key9 == CoalitionVerbs.GovernmentKey(g9),
                    F("(2) N3: L struck from M's cabinet (the key unchanged): the threat {0} - '{1}'", partner?.State.ToString() ?? "NONE", partner?.Outcome));
                Check(lapseMade && lapse != null && lapse.State == MotionState.Withdrawn && lapse.SettledOn == clear.AddDays(11) && madeTo != CoalitionVerbs.GovernmentKey(reformed)
                      && reformed.Cabinet.Contains("L") && ReferenceEquals(se3.Government, reformed) && Count(se3, CoalitionVerb.Leave, "L") == 0,
                    F("(2) M1: M+KD+L re-formed on a new record ({0} -> {1}): the threat {2} on {3:yyyy-MM-dd} ('{4}'), and on its old deadline L {5}", madeTo, CoalitionVerbs.GovernmentKey(reformed),
                        lapse?.State.ToString() ?? "NONE (" + lapseWhy + ")", lapse?.SettledOn ?? DateTime.MinValue, lapse?.Outcome, reformed.Cabinet.Contains("L") ? "stays" : "LEFT"));
            }

            // (3) a dismissal
            void Dismissal()
            {
                (SimulationManager sim, Country se) = Open("CoalitionVerbsDiagnostic.dismiss", CountryId.Sweden, clear, "M");
                Seat(se, seats);
                GovernmentRecord g = Govern(se, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                // K59 on a dismissal: one of M's chairs left vacant, so a seating where none is owed would show
                var lHeld = new List<CabinetPortfolio>(g.Portfolios.TryGetValue("L", out List<CabinetPortfolio> lp) ? lp : new List<CabinetPortfolio>());
                CabinetPortfolio? vacated = null;
                foreach (CabinetPortfolio p in g.Portfolios.TryGetValue("M", out List<CabinetPortfolio> mp) ? mp : new List<CabinetPortfolio>()) { vacated = p; break; }
                if (vacated.HasValue && se.CabinetMinisters != null) { se.CabinetMinisters.Remove(vacated.Value); }
                var ministers = se.CabinetMinisters != null ? new Dictionary<CabinetPortfolio, CabinetMinister>(se.CabinetMinisters) : new Dictionary<CabinetPortfolio, CabinetMinister>();
                int chairsBefore = CabinetSystem.PlayersChairs(se).Count;
                bool done = sim.Dismiss(CountryId.Sweden, "L", out string why);
                bool sameMinisters = se.CabinetMinisters != null && se.CabinetMinisters.Count == ministers.Count;
                foreach (KeyValuePair<CabinetPortfolio, CabinetMinister> kv in ministers) { sameMinisters &= se.CabinetMinisters.TryGetValue(kv.Key, out CabinetMinister now) && ReferenceEquals(now, kv.Value); }
                int allChairs = Enum.GetValues(typeof(CabinetPortfolio)).Length;
                Check(done && vacated.HasValue && lHeld.Count > 0 && chairsBefore == allChairs && CabinetSystem.PlayersChairs(se).Count == allChairs && sameMinisters && !se.CabinetMinisters.ContainsKey(vacated.Value),
                    F("(3) K59 on the dismissal: the head's chairs {0} of {1} before and {2} after - every one already his - so nothing is seated: {3} minister(s) unchanged, {4} still vacant; L's former posts ({5}) re-apportioned with their ministers",
                        chairsBefore, allChairs, CabinetSystem.PlayersChairs(se).Count, ministers.Count, vacated?.ToString() ?? "NONE", string.Join(", ", lHeld)));
                CoalitionMotion m = CoalitionVerbs.LatestOf(se, CoalitionVerb.Dismiss, "M");
                var binds = new List<TopStrip.Chip>();
                TopStrip.CoalitionChips(se, sim.CurrentDate, binds, null);
                Check(done && why == null && !g.Cabinet.Contains("L") && !g.Portfolios.ContainsKey("L") && m != null && m.Target == "L" && m.State == MotionState.Carried
                      && AnyBreak(g, CoalitionVerbs.DismissedLine(clear, "M", "L")) && ProcedureOpened(se, g, "L") && binds.Exists(chip => chip.Words == "L dismissed"),
                    F("(3) M dismisses L: {0} - M+KD with SD; the dismissal recorded ({1}); the procedure opened - \"{2}\"; the strip's place 3: {3}", why ?? "done", m != null ? m.Outcome : "NONE", Last(g.Breaks), binds.Count > 0 ? binds[0].Words : "NONE"));
                bool again = sim.Dismiss(CountryId.Sweden, "L", out string againWhy);
                Check(!again && againWhy == "L IS NOT A PARTNER IN YOUR CABINET", F("(3) a second dismissal of L refused - \"{0}\"", againWhy ?? "DONE"));

                // K130's AI exit on the record of verbs (the review's hole (h)): KD's planted projection outside beats inside by more than sigma_v at the budget vote
                (SimulationManager sim5, Country se5) = Open("CoalitionVerbsDiagnostic.k130", CountryId.Sweden, clear, "S");
                Seat(se5, seats);
                Govern(se5, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                double sigma = CoalitionExit.Threshold;
                using (CoalitionExit.PlantProjection(p => p == "KD" ? (0.05, 0.05 + 2.0 * sigma) : (0.10, 0.10)))
                {
                    sim5.CountBudgetVoteOnAgreements(se5);
                }
                CoalitionMotion k130 = CoalitionVerbs.LatestOf(se5, CoalitionVerb.Leave, "KD");
                Check(k130 != null && !k130.ByPlayer && k130.Target == "M" && k130.State == MotionState.Carried && !se5.Government.Cabinet.Contains("KD"),
                    F("(3) K130: KD leaves at the budget vote and the leaving is on the record of verbs ({0}, by the AI's rule: {1})", k130 != null ? "yes" : "NO", k130 != null ? (!k130.ByPlayer).ToString() : "-"));
            }

            // (4) an enforcement: met, refused
            void Enforcement()
            {
                (SimulationManager sim, Country se) = Open("CoalitionVerbsDiagnostic.enforce", CountryId.Sweden, clear, "SD");
                Seat(se, seats);
                GovernmentRecord g = Govern(se, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                g.FormAgreements(se, formed, sim.World);
                List<AgreementItem> owed = CoalitionVerbs.OwedOf(g.AgreementOf("SD"));
                sb.Append(F("    measured  (4) SD's agreement owes: {0}\n", string.Join(" · ", owed.ConvertAll(d => SupportAgreement.KeyOf(d)))));
                if (owed.Count < 2) { Check(false, F("(4) SD's agreement owes {0} item(s) - two are needed to walk met and refused; VERIFIED NOTHING", owed.Count)); return; }
                AgreementItem first = owed[0], second = owed[1];
                bool made = sim.Enforce(CountryId.Sweden, SupportAgreement.KeyOf(first), 30, out string madeWhy);
                CoalitionMotion m = CoalitionVerbs.OpenOf(se, CoalitionVerb.Enforce, "SD");
                var coming = new List<TopStrip.Chip>();
                TopStrip.CoalitionChips(se, sim.CurrentDate, null, coming);
                Deliver(se, SupportAgreement.KeyOf(first), first.Target, sim.CurrentDate);
                sim.TrackAgreements(se);
                SetDay(sim, clear.AddDays(3));
                Step(sim, CountryId.Sweden);
                Check(made && m != null && m.State == MotionState.Met && first.State == AgreementState.Delivered && coming.Exists(chip => chip.Words == "SD enforces · " + CoalitionVerbs.ShortDay(clear.AddDays(30))),
                    F("(4) SD calls '{0}' due ({1}); the strip's place 4: {2}; delivered and tracked: the demand is {3}", first.Name, madeWhy ?? "made", coming.Count > 0 ? coming[0].Words : "NONE", m?.State.ToString() ?? "NONE"));
                bool made2 = sim.Enforce(CountryId.Sweden, SupportAgreement.KeyOf(second), 30, out string made2Why);
                CoalitionMotion r = CoalitionVerbs.OpenOf(se, CoalitionVerb.Enforce, "SD");
                SetDay(sim, clear.AddDays(33));
                Step(sim, CountryId.Sweden);
                Check(made2 && r != null && r.State == MotionState.Refused && second.State == AgreementState.Broken && second.BrokenOn == clear.AddDays(33) && g.Support.Contains("SD") && AnyBreak(g, "refused SD's demand"),
                    F("(4) '{0}' undelivered on its day ({1}): the demand is {2}, the item {3} on {4:yyyy-MM-dd}; SD still supports (it may withdraw by its verb)", second.Name, made2Why ?? "made", r?.State.ToString() ?? "NONE", second.State, second.BrokenOn));

                // M1: the cabinet re-formed on another record with its own agreement - the demand made to the first lapses, and the new agreement's item under the
                // same key is never broken by it on the old deadline
                (SimulationManager sim4, Country se4) = Open("CoalitionVerbsDiagnostic.enforcelapse", CountryId.Sweden, clear, "SD");
                Seat(se4, seats);
                GovernmentRecord g4 = Govern(se4, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                g4.FormAgreements(se4, formed, sim4.World);
                List<AgreementItem> owed4 = CoalitionVerbs.OwedOf(g4.AgreementOf("SD"));
                string key4 = owed4.Count > 0 ? SupportAgreement.KeyOf(owed4[0]) : null;
                bool made4 = key4 != null && sim4.Enforce(CountryId.Sweden, key4, 30, out string _);
                CoalitionMotion d4 = CoalitionVerbs.OpenOf(se4, CoalitionVerb.Enforce, "SD");
                GovernmentRecord g4b = Govern(se4, "M", clear.AddDays(10), new[] { "M", "KD", "L" }, new[] { "SD" });
                g4b.FormAgreements(se4, clear.AddDays(10), sim4.World);
                SetDay(sim4, clear.AddDays(30));
                Step(sim4, CountryId.Sweden);
                AgreementItem newItem = CoalitionVerbs.ItemOf(g4b.AgreementOf("SD"), key4);
                // the second review's N3 (a): the new agreement must hold the same key, or the case proves nothing (the pre-M1 code lapsed on a missing item too)
                Check(made4 && d4 != null && d4.State == MotionState.Withdrawn && newItem != null && newItem.State == AgreementState.Owed && !AnyBreak(g4b, "refused SD's demand"),
                    F("(4) M1: the cabinet re-formed with a new agreement: the demand {0} ('{1}'); the new agreement's '{2}' {3} on the old deadline", d4?.State.ToString() ?? "NONE", d4?.Outcome, key4 ?? "NONE",
                        newItem != null ? newItem.State.ToString() : "NOT HELD - VERIFIED NOTHING"));

                // N3 (b), the review's L3: delivered where the tracker does not run (a declaration's week: the confidence day returns before tracking) - met on the
                // live read, the item itself still owed; read off the tracked state it would break on that day instead
                (SimulationManager sim7, Country se7) = Open("CoalitionVerbsDiagnostic.liveread", CountryId.Sweden, clear, "SD");
                Seat(se7, seats);
                GovernmentRecord g7 = Govern(se7, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                g7.FormAgreements(se7, formed, sim7.World);
                AgreementItem law7 = null;
                foreach (AgreementItem item in CoalitionVerbs.OwedOf(g7.AgreementOf("SD"))) { if (item.Kind == AgreementItemKind.Law) { law7 = item; break; } }
                bool made7 = law7 != null && sim7.Enforce(CountryId.Sweden, SupportAgreement.KeyOf(law7), 30, out string _);
                CoalitionMotion d7 = CoalitionVerbs.OpenOf(se7, CoalitionVerb.Enforce, "SD");
                SetDay(sim7, clear.AddDays(30));
                g7.NoConfidenceOn = clear.AddDays(30);
                if (law7 != null) { se7.EnactedLaws.Add(new EnactedLaw { LawId = law7.LawId, EnactedOn = clear.AddDays(30) }); }
                Step(sim7, CountryId.Sweden);
                Check(made7 && d7 != null && d7.State == MotionState.Met && law7.State == AgreementState.Owed,
                    F("(4) L3: '{0}' enacted on the deadline inside a declaration's week (no tracking): the demand {1}, the item {2} (the tracker's to move)", law7?.Name ?? "NO LAW ITEM - VERIFIED NOTHING", d7?.State.ToString() ?? "NONE", law7?.State.ToString() ?? "-"));

                // N3 (c): the supporter's own lapse - SD withdraws its support under an open demand (the same record, so the same key); on the old deadline nothing
                // breaks on a government it no longer supports
                (SimulationManager sim8, Country se8) = Open("CoalitionVerbsDiagnostic.withdrawn", CountryId.Sweden, clear, "SD");
                Seat(se8, seats);
                GovernmentRecord g8 = Govern(se8, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                g8.FormAgreements(se8, formed, sim8.World);
                List<AgreementItem> owed8 = CoalitionVerbs.OwedOf(g8.AgreementOf("SD"));
                bool made8 = owed8.Count > 0 && sim8.Enforce(CountryId.Sweden, SupportAgreement.KeyOf(owed8[0]), 30, out string _);
                CoalitionMotion d8 = CoalitionVerbs.OpenOf(se8, CoalitionVerb.Enforce, "SD");
                string key8 = CoalitionVerbs.GovernmentKey(g8);
                bool withdrew = sim8.WithdrawSupport(CountryId.Sweden, out string _);
                SetDay(sim8, clear.AddDays(1));
                Step(sim8, CountryId.Sweden);
                SetDay(sim8, clear.AddDays(30));
                Step(sim8, CountryId.Sweden);
                Check(made8 && withdrew && d8 != null && d8.State == MotionState.Withdrawn && d8.Outcome != null && d8.Outcome.Contains("no longer supports") && key8 == CoalitionVerbs.GovernmentKey(g8)
                      && owed8[0].State != AgreementState.Broken && !AnyBreak(g8, "refused SD's demand"),
                    F("(4) N3: SD withdraws under its demand (the key unchanged): '{0}'; on the old deadline the item {1}", d8?.Outcome, owed8.Count > 0 ? owed8[0].State.ToString() : "NONE"));
            }

            // (5) K137: the sides re-declared after a count
            void Redeclaring()
            {
                DateTime count = new DateTime(2030, 9, 8), installed = new DateTime(2030, 10, 4), day = new DateTime(2030, 10, 5);
                (SimulationManager sim, Country se) = Open("CoalitionVerbsDiagnostic.k137", CountryId.Sweden, day, "S");
                Seat(se, seats);
                GovernmentRecord g = Govern(se, "M", count.AddDays(-400), new[] { "M", "KD", "L", "C" }, new[] { "SD" });   // C in the Tidö side's cabinet, so its re-declared side departs from its side of record
                se.ElectionHistory.Add(new ElectionRecord { Date = count, CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
                CoalitionStepAlone(sim, CountryId.Sweden);   // alone: the day tick would open the count's round on a government formed before it, and discharge it
                bool beforeFormed = Count(se, CoalitionVerb.Redeclare, null) == 0;
                g.FormedOn = installed;
                int cRecord = NationalElection.BlocOf(CountryId.Sweden, "C", day);
                Step(sim, CountryId.Sweden);
                int made = Count(se, CoalitionVerb.Redeclare, null);
                Step(sim, CountryId.Sweden);
                int afterSecond = Count(se, CoalitionVerb.Redeclare, null);
                HashSet<string> declared = GovernmentFormation.RedLinedFrom(se, g.Cabinet, day);
                int expected = 0;
                foreach (KeyValuePair<string, int> kv in se.ParliamentSeats) { if (kv.Value > 0 && kv.Key != "S" && (g.Cabinet.Contains(kv.Key) || g.Support.Contains(kv.Key) || declared.Contains(kv.Key))) { expected++; } }
                var sides = new List<string>();
                bool shape = true;
                foreach (CoalitionMotion m in se.CoalitionMotions)
                {
                    if (m.Verb != CoalitionVerb.Redeclare) { continue; }
                    sides.Add(m.Actor + " " + m.Demand + " " + m.Bloc.ToString(CultureInfo.InvariantCulture));
                    bool inGov = g.Cabinet.Contains(m.Actor) || g.Support.Contains(m.Actor);
                    int standing = NationalElection.BlocOf(CountryId.Sweden, m.Actor, day);
                    shape &= !m.ByPlayer && m.Actor != "S" && m.Target == "M" && (inGov ? m.Demand == "FOR" && m.Bloc == 1 : declared.Contains(m.Actor) && m.Demand == "AGAINST" && m.Bloc == (standing == 1 ? -1 : standing));   // K331
                }
                sb.Append(F("    measured  (5) K137 (K330) after the count of {0:yyyy-MM-dd}: {1}; red-lined from M+KD+L+C: {2}\n", count, string.Join(", ", sides), string.Join("+", declared)));
                se.CalendarDay = day;
                int cNow = NationalElection.BlocOf(se, "C");
                se.CalendarDay = count;
                int cBefore = NationalElection.BlocOf(se, "C");
                se.CalendarDay = day;
                Check(beforeFormed && made == expected && made > 0 && afterSecond == made && shape && cRecord == 0 && cNow == 1 && cBefore == 0,
                    F("(5) nothing while the count's government is not formed ({0}); then {1} of the {2} owed (in or behind M+KD+L+C with SD FOR, the red-lined AGAINST, no other party, never the player's S), once per count ({3}); C's bloc 1 from its day, {4} on the count's day and of record {5}",
                        beforeFormed, made, expected, afterSecond, cBefore, cRecord));
                var keys = new[] { "C", "S" };
                int[] withCountry = NationalElection.SwedenBlocsOn(keys, day, se);
                int[] without = NationalElection.SwedenBlocsOn(keys, day);
                Check(withCountry[0] == 1 && without[0] == cRecord && withCountry[1] == without[1],
                    F("(5) the tactical layer's sides on a country read C's re-declared {0} (of record {1}); S not re-declared reads its record ({2})", withCountry[0], without[0], withCountry[1]));

                bool neither = sim.CanRedeclare(CountryId.Sweden, BlocSide.Neither, out string neitherWhy);
                bool did = sim.Redeclare(CountryId.Sweden, BlocSide.Neither, out string didWhy);
                int sNow = NationalElection.BlocOf(se, "S");
                bool twice = sim.CanRedeclare(CountryId.Sweden, BlocSide.For, out string twiceWhy);
                se.PlayerPartyAbbrev = "L";
                bool against = sim.CanRedeclare(CountryId.Sweden, BlocSide.Against, out string againstWhy);
                Check(neither && did && sNow == -1 && !twice && twiceWhy == "YOU RE-DECLARED AFTER THIS COUNT" && !against && againstWhy == "LEAVE THE GOVERNMENT FIRST",
                    F("(5) the player's S re-declares NEITHER ({0}) - its bloc {1}; again \"{2}\"; a partner against its own government \"{3}\"", didWhy ?? neitherWhy ?? "done", sNow, twiceWhy ?? "MAY", againstWhy ?? "MAY"));

                // M2: the count hands the tactical layer the played world (the review's hole (b)) - read where the night calls it
                string night = SourceText.ReadWithoutComments(Path.Combine("Assets", "Scripts", "Simulation", "SimulationManager.PlayerElection.cs"));
                Check(Regex.IsMatch(night, @"SharesOnPublishedPolls\([^;]*played: country\)"), "(5) the count's call hands SharesOnPublishedPolls the played world (played: country), so a re-declared side reaches the election");

                (SimulationManager deSim, Country de) = Open("CoalitionVerbsDiagnostic.k137de", CountryId.Germany, day, "AfD");
                GovernmentRecord dg = Govern(de, "CDU", installed, new[] { "CDU", "CSU", "SPD" }, new string[0]);
                de.ElectionHistory.Add(new ElectionRecord { Date = count, CountryId = CountryId.Germany.ToString(), Method = ElectionMethod.GermanyNationalProportional });
                Step(deSim, CountryId.Germany);
                bool deShape = Count(de, CoalitionVerb.Redeclare, null) > 0;
                foreach (CoalitionMotion m in de.CoalitionMotions) { if (m.Verb == CoalitionVerb.Redeclare) { deShape &= m.Bloc == -1 && m.Actor != "AfD"; } }
                de.CalendarDay = day;
                Check(deShape && NationalElection.BlocOf(de, "SPD") == -1,
                    F("(5) Germany: {0} sides recorded, none a bloc (the model reads blocs in Sweden only); the SPD's bloc {1}", Count(de, CoalitionVerb.Redeclare, null), NationalElection.BlocOf(de, "SPD")));

                (SimulationManager none, Country ai) = Open("CoalitionVerbsDiagnostic.noplayer", CountryId.Sweden, day, null);
                Govern(ai, "M", installed, new[] { "M", "KD", "L" }, new[] { "SD" });
                ai.ElectionHistory.Add(new ElectionRecord { Date = count, CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
                CoalitionStepAlone(none, CountryId.Sweden);   // alone: the day tick is the player country's, and here there is none
                Check(ai.CoalitionMotions.Count == 0, F("(5) a world with no player country: {0} motion(s) after the day's step", ai.CoalitionMotions.Count));
            }

            // (5) K330: an S minority with no support recorded - only the cabinet and the red-lined re-declare; V, MP and C keep their standing blocs
            void Minority()
            {
                DateTime count = new DateTime(2030, 9, 8), installed = new DateTime(2030, 10, 4), day = new DateTime(2030, 10, 5);
                (SimulationManager sim, Country se) = Open("CoalitionVerbsDiagnostic.k330", CountryId.Sweden, day, "M");   // the player M, so S, V, MP and C are the AI's
                Seat(se, seats);
                se.ParliamentSeats["KD"] = 0;   // N3 (d): a seatless party re-declares nothing, whatever its lines - KD's own line refuses S, so only the seat filter stops it
                GovernmentRecord g = Govern(se, "S", installed, new[] { "S" }, new string[0]);
                g.Outcome = CoalitionOutcomeKind.MinorityGovernment.ToString();
                se.ElectionHistory.Add(new ElectionRecord { Date = count, CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
                CoalitionStepAlone(sim, CountryId.Sweden);   // alone: the confidence day would weigh an AI motion against the planted minority first
                HashSet<string> declared = GovernmentFormation.RedLinedFrom(se, g.Cabinet, day);
                se.CalendarDay = day;
                CoalitionMotion s = CoalitionVerbs.LatestOf(se, CoalitionVerb.Redeclare, "S");
                bool keep = true;
                var kept = new List<string>();
                foreach (string p in new[] { "V", "MP", "C" })
                {
                    int standing = NationalElection.BlocOf(CountryId.Sweden, p, day);
                    keep &= CoalitionVerbs.LatestOf(se, CoalitionVerb.Redeclare, p) == null && NationalElection.BlocOf(se, p) == standing && !declared.Contains(p);
                    kept.Add(p + " " + NationalElection.BlocOf(se, p).ToString(CultureInfo.InvariantCulture) + " (of record " + standing.ToString(CultureInfo.InvariantCulture) + ")");
                }
                bool others = true;
                var sides = new List<string>();
                foreach (string p in new[] { "M", "KD", "SD", "L" })
                {
                    CoalitionMotion m = CoalitionVerbs.LatestOf(se, CoalitionVerb.Redeclare, p);
                    bool owed = p != "M" && p != "KD" && declared.Contains(p);
                    others &= owed ? m != null && m.Demand == "AGAINST" && m.Bloc == 1 : m == null;
                    sides.Add(p + " " + (m != null ? m.Demand + " " + m.Bloc.ToString(CultureInfo.InvariantCulture) : "nothing"));
                }
                sb.Append(F("    measured  (5) K330 after an S minority formed: S {0}; {1}; red-lined from S: {2}; kept: {3}\n", s != null ? s.Demand + " " + s.Bloc : "nothing", string.Join(", ", sides), string.Join("+", declared), string.Join(", ", kept)));
                Check(s != null && s.Demand == "FOR" && s.Bloc == 0 && keep && others && declared.Contains("KD"),
                    F("(5) K330: S alone, no support recorded - S FOR on its own bloc; V, MP and C re-declare nothing and keep their standing blocs ({0}); the player's M nothing, and KD - red-lined, seatless - nothing ({1})",
                        string.Join(", ", kept), string.Join(", ", sides)));
            }

            // (5) the second review's N1 (its GuardProbe): a count whose government draws no AI side - the player's L alone, nothing red-lined from it - then a
            // successor formed before the next count. The continuous manager writes nothing toward it, and neither does a fresh manager over the same world (a load,
            // an impact-ledger fork): the once-per-count guard is the country's, saved
            void Guard()
            {
                DateTime count = new DateTime(2030, 9, 8), installed = new DateTime(2030, 10, 4), day = new DateTime(2030, 10, 5), later = new DateTime(2031, 3, 2);
                (SimulationManager sim, Country se) = Open("CoalitionVerbsDiagnostic.guard", CountryId.Sweden, day, "L");
                Seat(se, seats);
                se.ElectionHistory.Add(new ElectionRecord { Date = count, CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
                GovernmentRecord g1 = Govern(se, "L", installed, new[] { "L" }, new string[0]);
                g1.Outcome = CoalitionOutcomeKind.MinorityGovernment.ToString();
                CoalitionStepAlone(sim, CountryId.Sweden);
                HashSet<string> redLined = GovernmentFormation.RedLinedFrom(se, g1.Cabinet, day);
                int first = Count(se, CoalitionVerb.Redeclare, null);
                bool guarded = se.RedeclaredAfterCount == count;
                GovernmentRecord g2 = Govern(se, "M", later, new[] { "M", "C", "KD" }, new string[0]);
                SetDay(sim, later);
                se.CalendarDay = later;
                CoalitionStepAlone(sim, CountryId.Sweden);
                int continuous = Count(se, CoalitionVerb.Redeclare, null);
                var go = new GameObject("CoalitionVerbsDiagnostic.guard.fresh"); hosts.Add(go);
                SimulationManager fresh = go.AddComponent<SimulationManager>();
                fresh.SetWorld(sim.World);
                fresh.PlayerCountryId = CountryId.Sweden;
                SetDay(fresh, later);
                se.CalendarDay = later;
                CoalitionStepAlone(fresh, CountryId.Sweden);
                int afterFresh = Count(se, CoalitionVerb.Redeclare, null);
                Check(redLined.Count == 0 && first == 0 && guarded && continuous == 0 && afterFresh == 0 && ReferenceEquals(se.Government, g2),
                    F("(5) N1: L alone after the count of {0:yyyy-MM-dd} (red-lined from it: [{1}]) writes {2} side(s), the count held on the country ({3}); M+C+KD formed {4:yyyy-MM-dd}: the continuous manager writes {5}, a fresh manager over the same world {6}",
                        count, string.Join("+", redLined), first, guarded, later, continuous, afterFresh));
            }

            // (5) K331: an AGAINST side never moves a party onto the other bloc. Under M+C+KD (no support recorded) SD is red-lined through the two-way C-SD
            // line: it leaves the head's bloc and stands in none, never S's; a red-lined party on the other bloc keeps it; FOR takes the head's bloc
            void AgainstStaysOut()
            {
                DateTime count = new DateTime(2030, 9, 8), installed = new DateTime(2030, 10, 4), day = new DateTime(2030, 10, 5);
                (SimulationManager sim, Country se) = Open("CoalitionVerbsDiagnostic.k331", CountryId.Sweden, day, "L");
                Seat(se, seats);
                GovernmentRecord g = Govern(se, "M", installed, new[] { "M", "C", "KD" }, new string[0]);
                g.Outcome = CoalitionOutcomeKind.MinorityGovernment.ToString();
                se.ElectionHistory.Add(new ElectionRecord { Date = count, CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
                CoalitionStepAlone(sim, CountryId.Sweden);   // alone: the confidence day would weigh an AI motion against the planted minority first
                se.CalendarDay = day;
                HashSet<string> declared = GovernmentFormation.RedLinedFrom(se, g.Cabinet, day);
                CoalitionMotion sd = CoalitionVerbs.LatestOf(se, CoalitionVerb.Redeclare, "SD");
                bool mapped = true;
                var sides = new List<string>();
                foreach (CoalitionMotion m in se.CoalitionMotions)
                {
                    if (m.Verb != CoalitionVerb.Redeclare) { continue; }
                    int standing = NationalElection.BlocOf(CountryId.Sweden, m.Actor, day);
                    mapped &= m.Demand == "FOR" ? m.Bloc == 1 : m.Bloc == (standing == 1 ? -1 : standing);
                    sides.Add(m.Actor + " " + m.Demand + " " + m.Bloc.ToString(CultureInfo.InvariantCulture) + " (of record " + standing.ToString(CultureInfo.InvariantCulture) + ")");
                }
                sb.Append(F("    measured  (5) K331 under M+C+KD: {0}; red-lined from it: {1}\n", string.Join(", ", sides), string.Join("+", declared)));
                Check(declared.Contains("SD") && sd != null && sd.Demand == "AGAINST" && sd.Bloc == -1 && NationalElection.BlocOf(se, "SD") == -1 && NationalElection.BlocOf(se, "C") == 1 && mapped,
                    F("(5) K331: SD red-lined from M+C+KD declares AGAINST and stands in no bloc ({0}), never S's; C FOR on the head's bloc ({1}); every side mapped as K331 rules ({2})",
                        sd != null ? sd.Bloc.ToString(CultureInfo.InvariantCulture) : "NOT RE-DECLARED", NationalElection.BlocOf(se, "C"), mapped));
            }

            // (5) K331's third rule (the delta check's D3): under a head standing in no bloc every side keeps the party's standing bloc. C stands in no bloc of
            // record from its speech of 2025-11-13 until 2026-06-09 (`NationalElection.SwedenSides`), so a C-led cabinet after a count planted inside that stretch
            // has no bloc to give: its partners declaring FOR keep theirs (L the Tidö side, MP S's), and so does every party declaring AGAINST
            void NoBlocHead()
            {
                DateTime count = new DateTime(2026, 2, 1), installed = new DateTime(2026, 2, 20), day = new DateTime(2026, 3, 1);
                (SimulationManager sim, Country se) = Open("CoalitionVerbsDiagnostic.nobloc", CountryId.Sweden, day, "S");
                Seat(se, seats);
                GovernmentRecord g = Govern(se, "C", installed, new[] { "C", "L", "MP" }, new string[0]);
                g.Outcome = CoalitionOutcomeKind.MinorityGovernment.ToString();
                se.ElectionHistory.Add(new ElectionRecord { Date = count, CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
                CoalitionStepAlone(sim, CountryId.Sweden);   // alone: the confidence day would weigh an AI motion against the planted minority first
                se.CalendarDay = day;
                int head = NationalElection.BlocOf(CountryId.Sweden, "C", day);
                bool kept = true;
                int forSides = 0;
                var sides = new List<string>();
                foreach (CoalitionMotion m in se.CoalitionMotions)
                {
                    if (m.Verb != CoalitionVerb.Redeclare) { continue; }
                    int standing = NationalElection.BlocOf(CountryId.Sweden, m.Actor, day);
                    kept &= m.Bloc == standing && NationalElection.BlocOf(se, m.Actor) == standing;
                    if (m.Demand == "FOR") { forSides++; }
                    sides.Add(m.Actor + " " + m.Demand + " " + m.Bloc.ToString(CultureInfo.InvariantCulture) + " (of record " + standing.ToString(CultureInfo.InvariantCulture) + ")");
                }
                CoalitionMotion l = CoalitionVerbs.LatestOf(se, CoalitionVerb.Redeclare, "L"), mp = CoalitionVerbs.LatestOf(se, CoalitionVerb.Redeclare, "MP");
                sb.Append(F("    measured  (5) K331 under a no-bloc head (C+L+MP, C at {0}): {1}\n", head, string.Join(", ", sides)));
                Check(head == -1 && forSides == 3 && kept && l != null && l.Bloc == 1 && mp != null && mp.Bloc == 0,
                    F("(5) K331: C, standing in no bloc ({0}), heads C+L+MP - every side keeps its standing bloc ({1}): L FOR at {2}, MP FOR at {3}",
                        head, kept, l != null ? l.Bloc.ToString(CultureInfo.InvariantCulture) : "NONE", mp != null ? mp.Bloc.ToString(CultureInfo.InvariantCulture) : "NONE"));
            }

            // (6) the preview's clone
            void PreviewClone()
            {
                (SimulationManager sim, Country se) = Open("CoalitionVerbsDiagnostic.clone", CountryId.Sweden, clear, "M");
                Seat(se, seats);
                Govern(se, "M", formed, new[] { "M", "KD", "L" }, new[] { "SD" });
                sim.Dismiss(CountryId.Sweden, "L", out _);
                MethodInfo cloneOf = typeof(SimulationManager).GetMethod("ClonePreviewCountry", BindingFlags.NonPublic | BindingFlags.Static);
                var clone = cloneOf?.Invoke(null, new object[] { se }) as Country;
                bool copies = clone != null && clone.CoalitionMotions != null && clone.CoalitionMotions.Count == se.CoalitionMotions.Count;
                for (int i = 0; copies && i < se.CoalitionMotions.Count; i++) { copies &= !ReferenceEquals(clone.CoalitionMotions[i], se.CoalitionMotions[i]) && clone.CoalitionMotions[i].Verb == se.CoalitionMotions[i].Verb && clone.CoalitionMotions[i].Actor == se.CoalitionMotions[i].Actor; }
                Check(copies && se.CoalitionMotions.Count > 0 && !ReferenceEquals(clone.CoalitionMotions, se.CoalitionMotions),
                    F("(6) the preview's clone carries {0} of {1} motion(s), in its own list of its own copies (the review's L5)", clone?.CoalitionMotions?.Count ?? -1, se.CoalitionMotions.Count));
            }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
