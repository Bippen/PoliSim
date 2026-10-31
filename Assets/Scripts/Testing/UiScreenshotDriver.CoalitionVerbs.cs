using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;

namespace PoliSim.Testing
{
    public partial class UiScreenshotDriver
    {
        /// <summary>
        /// COAL-2 (K136: "done when threat, dismissal, enforcement and re-declaring are each played once in a filmed Swedish and a filmed German game"): THE
        /// COALITION VERBS FILMED, on Sweden's and Germany's main sweeps after every other frame, each verb in its states - offered, threatened, carried out,
        /// refused - on Politics › Cabinet ("The coalition"), the Docket and the strip. STAGED, said in the log: the chamber as seated (no seat is typed) and the
        /// government named through the record's own method (`GovernmentRecord.OfNamedCabinet`) - M+KD+L with SD's support and its agreement, CDU+CSU+SPD with
        /// the Grüne's; the player's party moved through the roles (a junior partner, the head, the supporter, the opposition after a planted count). Every verb
        /// is the manager's own; a deadline is reached by setting the clock and running the day's own step (`SimulationManager.AdvanceCoalitionVerbsDay`). The
        /// frames are expected - a refusal fails the film. The impact ledger does not watch the staged verbs (K197's frames' precedent), and the government, the
        /// seats, the division log, the record of verbs, the election history and its re-declaration guard, the enacted laws, the two dials, the ministers, the
        /// clock, the player's party and the presidency flag are put back after, so nothing staged reaches a later frame or a forked world.
        /// </summary>
        private IEnumerator CaptureCoalitionVerbs(GameController controller)
        {
            bool german = _countryId == CountryId.Germany;
            string[] stems =
            {
                "co01_cabinet_junior", "co02_cabinet_threat_compose", "co03_cabinet_threatened", "co04_docket_threatened", "co05_cabinet_threat_met",
                "co06_cabinet_threat_carried", "co07_docket_left", "co08_cabinet_lead", "co09_docket_dismissed", "co10_cabinet_support", "co11_cabinet_enforced",
                "co12_cabinet_enforce_refused", "co13_cabinet_redeclare", "co14_docket_sides",
            };
            Expect(stems);
            if (german) { Expect("co15_cabinet_junior_refused"); }
            const BindingFlags Own = BindingFlags.Instance | BindingFlags.NonPublic;
            var sim = controller.GetType().GetField("_simulationManager", Own)?.GetValue(controller) as SimulationManager;
            Country player = sim?.World?.GetCountry(_countryId);
            GovernmentRecord government0 = player?.Government;
            MethodInfo dayStep = typeof(SimulationManager).GetMethod("AdvanceCoalitionVerbsDay", Own);
            PropertyInfo clock = typeof(SimulationManager).GetProperty("CurrentDate");
            MethodInfo pin = controller.GetType().GetMethod("PinSlipOnAnchorForFilm", Own);
            FieldInfo pins = controller.GetType().GetField("_slipPins", Own);
            if (sim == null || government0 == null || player.ParliamentSeats == null || dayStep == null || clock?.SetMethod == null || pin == null || pins == null)
            {
                Debug.LogError("SHOT: COAL-2 - the manager, the government, its seats, the day's step, the clock's setter or the slip pin could not be reached; the co frames are MISSING, not clean.");
                _failed++;
                yield break;
            }
            string lead = german ? "CDU" : "M", junior = german ? "SPD" : "L", support = german ? "Grune" : "SD", opposition = german ? "Linke" : "S";
            string[] cabinet = german ? new[] { "CDU", "CSU", "SPD" } : new[] { "M", "KD", "L" };
            string own = player.PlayerPartyAbbrev;
            bool president0 = player.PlayerHoldsPresidency;
            Dictionary<string, int> seats0 = player.ParliamentSeats;
            var divisions0 = new List<DivisionRecord>(player.Divisions.Entries);
            var motions0 = new List<CoalitionMotion>(player.CoalitionMotions);
            var history0 = new List<ElectionRecord>(player.ElectionHistory);
            DateTime redeclared0 = player.RedeclaredAfterCount;   // the delta check's D1: the staged count's guard is put back with the rest
            var enacted0 = new List<EnactedLaw>(player.EnactedLaws);
            float border0 = player.BorderEnforcementLevel, police0 = player.PoliceFundingLevel;
            var ministers0 = player.CabinetMinisters != null ? new Dictionary<CabinetPortfolio, CabinetMinister>(player.CabinetMinisters) : null;
            PolicyImpactLedger ledger0 = sim.ImpactLedger;
            DateTime date0 = sim.CurrentDate;
            void Fail(string what) { Debug.LogError("SHOT: COAL-2 - " + what + "; the remaining co frames are MISSING, not clean."); _failed++; }
            void Step() => dayStep.Invoke(sim, new object[] { _countryId });
            void SetDay(DateTime d) => clock.SetValue(sim, d);
            void Deliver(string key, float target)
            {
                if (key.StartsWith("law:", StringComparison.Ordinal)) { player.EnactedLaws.Add(new EnactedLaw { LawId = key.Substring(4), EnactedOn = sim.CurrentDate }); }
                else if (key == "dial:" + AgreementDial.BorderEnforcement) { player.BorderEnforcementLevel = target; }
                else { player.PoliceFundingLevel = target; }
            }
            GovernmentRecord Fresh(string party, DateTime formedOn)
            {
                player.CoalitionMotions.Clear();
                player.CoalitionMotions.AddRange(motions0);
                GovernmentRecord g = GovernmentRecord.OfNamedCabinet(lead, cabinet, new[] { support }, formedOn, "staged (UiScreenshotDriver): the coalition verbs' frames (COAL-2)");
                g.Kind = government0.Kind;
                g.Executive = government0.Executive;
                g.Outcome = CoalitionOutcomeKind.MajorityCoalition.ToString();
                g.AllocatePortfolios(player);
                g.FormAgreements(player, formedOn, sim.World);
                player.Government = g;
                player.PlayerPartyAbbrev = party;
                SetPrivateField(controller, "_coalitionComposing", null);
                SetPrivateField(controller, "_coalitionPinnedPreview", null);
                return g;
            }
            IEnumerator Film(string stem, string tab, string pinned)
            {
                SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
                SetPrivateField(controller, "_onDesk", false);
                SetEnumField(controller, "_consolidatedTab", tab);
                if (tab == "Politics") { SetEnumField(controller, "_politicsCategory", "Cabinet"); }
                ResetScrolls(controller);
                yield return Settle();
                if (pinned != null)
                {
                    pin.Invoke(controller, new object[] { pinned, new Vector2(UiScreen.Width * 0.62f, UiScreen.Height * 0.40f) });
                    yield return Settle();
                    yield return Settle();
                }
                Claim("imgui");
                yield return Capture(stem);
                (pins.GetValue(controller) as IList)?.Clear();
            }
            try
            {
                foreach (string k in new List<string>(cabinet) { support, opposition }) { if (!player.ParliamentSeats.TryGetValue(k, out int n) || n <= 0) { Fail(k + " holds no seat in the chamber as seated"); yield break; } }
                player.PlayerHoldsPresidency = false;
                sim.ImpactLedger = null;
                DateTime today = date0;

                // ---- a threat: offered, composed, made, met, carried out ----
                GovernmentRecord g = Fresh(junior, today.AddDays(-1));
                bool mayThreaten = sim.CanThreaten(_countryId, out string threatWhy);
                bool mayLeave = sim.CanLeaveGovernment(_countryId, out string leaveWhy);
                Debug.Log($"SHOT: co01_cabinet_junior - STAGED: {string.Join("+", cabinet)} led by {lead} with {support}'s support on the chamber as seated, the player {junior}, on {today:yyyy-MM-dd}: THREATEN {(mayThreaten ? "offered" : "disabled - " + threatWhy)}, LEAVE {(mayLeave ? "offered" : "disabled - " + leaveWhy)}; LEAVE's chamber previewed (dashed) and its slip pinned");
                if (!mayThreaten || !mayLeave) { Fail("the junior partner's verbs are refused"); yield break; }
                SetPrivateField(controller, "_coalitionPinnedPreview", "leave:" + junior);
                yield return Film("co01_cabinet_junior", "Politics", "coal:verb:leave");
                SetPrivateField(controller, "_coalitionPinnedPreview", null);
                List<AgreementItem> demands = sim.ThreatDemands(_countryId);
                if (demands.Count < 2) { Fail($"{junior} holds {demands.Count} demand(s) the lead's line allows - two are needed"); yield break; }
                SetPrivateField(controller, "_coalitionComposing", CoalitionVerb.Threaten);
                Debug.Log($"SHOT: co02_cabinet_threat_compose - the threat's composer: the demand '{demands[0].Name}' (1 of {demands.Count}), the deadline 30 days");
                yield return Film("co02_cabinet_threat_compose", "Politics", "coal:compose:deadline");
                SetPrivateField(controller, "_coalitionComposing", null);
                if (!sim.Threaten(_countryId, SupportAgreement.KeyOf(demands[0]), 30, out string madeWhy)) { Fail("the threat was refused (" + madeWhy + ")"); yield break; }
                CoalitionMotion threat = CoalitionVerbs.OpenOf(player, CoalitionVerb.Threaten, junior);
                Debug.Log($"SHOT: co03_cabinet_threatened - {junior} threatens to leave unless '{threat.DemandName}' by {threat.Deadline:yyyy-MM-dd}; the strip's place 4 reads it");
                yield return Film("co03_cabinet_threatened", "Politics", "coal:play:" + junior);
                Debug.Log("SHOT: co04_docket_threatened - the threat on the Docket as Information, led by the threat glyph");
                yield return Film("co04_docket_threatened", "Decisions", null);
                Deliver(threat.Demand, threat.DialTarget);
                SetDay(today.AddDays(5));
                Step();
                Debug.Log($"SHOT: co05_cabinet_threat_met - '{threat.DemandName}' delivered: the threat {threat.State} ('{threat.Outcome}')");
                if (threat.State != MotionState.Met) { Fail("the delivered threat is " + threat.State); yield break; }
                yield return Film("co05_cabinet_threat_met", "Politics", null);
                List<AgreementItem> next = sim.ThreatDemands(_countryId);
                if (next.Count == 0 || !sim.Threaten(_countryId, SupportAgreement.KeyOf(next[0]), 30, out string secondWhy)) { Fail("the second threat was refused (" + (next.Count == 0 ? "no demand left" : "refused") + ")"); yield break; }
                CoalitionMotion carried = CoalitionVerbs.OpenOf(player, CoalitionVerb.Threaten, junior);
                SetDay(carried.Deadline);
                Step();
                Debug.Log($"SHOT: co06_cabinet_threat_carried - '{carried.DemandName}' undelivered on {carried.Deadline:yyyy-MM-dd}: the threat {carried.State} ('{carried.Outcome}'); {CoalitionVerbs.StandingLine(player)}; the strip's place 3: {StripWords(player, sim.CurrentDate, true)}");
                if (carried.State != MotionState.Carried) { Fail("the threat on its day is " + carried.State); yield break; }
                yield return Film("co06_cabinet_threat_carried", "Politics", null);
                Debug.Log("SHOT: co07_docket_left - the threat carried out and the leaving on the Docket, each led by its glyph");
                yield return Film("co07_docket_left", "Decisions", null);

                // ---- a dismissal: offered (and, in Germany, refused for the CSU alone), carried out ----
                SetDay(today);
                player.EnactedLaws.Clear(); player.EnactedLaws.AddRange(enacted0);
                player.BorderEnforcementLevel = border0; player.PoliceFundingLevel = police0;
                g = Fresh(lead, today.AddDays(-1));
                bool mayDismiss = sim.CanDismiss(_countryId, junior, out string dismissWhy);
                string groupWords = german ? (sim.CanDismiss(_countryId, "CSU", out string csuWhy) ? "the CSU OFFERED (expected refused)" : "the CSU refused - " + csuWhy) : "no parliamentary group";
                Debug.Log($"SHOT: co08_cabinet_lead - the player {lead}, the head: DISMISS {junior} {(mayDismiss ? "offered" : "disabled - " + dismissWhy)}; {groupWords}; {junior}'s dismissal previewed and its slip pinned");
                if (!mayDismiss) { Fail("the head may not dismiss " + junior); yield break; }
                SetPrivateField(controller, "_coalitionPinnedPreview", "dismiss:" + junior);
                yield return Film("co08_cabinet_lead", "Politics", "coal:verb:dismiss:" + junior);
                SetPrivateField(controller, "_coalitionPinnedPreview", null);
                if (!sim.Dismiss(_countryId, junior, out string dismissed)) { Fail("the dismissal was refused (" + dismissed + ")"); yield break; }
                Debug.Log($"SHOT: co09_docket_dismissed - {lead} dismissed {junior}: {CoalitionVerbs.StandingLine(player)}; the strip's place 3: {StripWords(player, sim.CurrentDate, true)}");
                yield return Film("co09_docket_dismissed", "Decisions", null);

                // ---- an enforcement: offered, made, refused ----
                g = Fresh(support, today.AddDays(-1));
                // §966: under a CDU formateur the model's own scoring leaves the Grüne no item (every law and dial they want, the CDU opposes - the
                // first film's "0 OWED"), so the German film STAGES the one item the enforcement needs, in the model's own form (a dial one step up),
                // and says so; the agreement goes back with the staged government
                SupportAgreement held = g.AgreementOf(support);
                if (german && held != null && CoalitionVerbs.OwedOf(held).Count == 0)
                {
                    float standing = SupportAgreement.DialValue(player, AgreementDial.PoliceFunding);
                    float target = standing + SupportAgreement.DialStep;
                    held.Items.Add(new AgreementItem
                    {
                        Kind = AgreementItemKind.Dial, Dial = AgreementDial.PoliceFunding, Target = target, StartValue = standing, State = AgreementState.Owed,
                        Name = SupportAgreement.DialName(AgreementDial.PoliceFunding) + " to " + target.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                        Basis = "STAGED (UiScreenshotDriver, COAL-2's film): the model's scoring gave this pairing no item",
                    });
                    Debug.Log($"SHOT: co10_cabinet_support - STAGED: the model's scoring gave {support} under {lead} no item, so the film plants one in the model's own form ({held.Items[held.Items.Count - 1].Name}).");
                }
                bool mayEnforce = sim.CanEnforce(_countryId, out string enforceWhy);
                List<AgreementItem> owed = CoalitionVerbs.OwedOf(g.AgreementOf(support));
                Debug.Log($"SHOT: co10_cabinet_support - the player {support}, the supporter: ENFORCE {(mayEnforce ? "offered" : "disabled - " + enforceWhy)}, WITHDRAW offered; its agreement {g.AgreementOf(support)?.Tally() ?? "NONE"}");
                if (!mayEnforce || owed.Count == 0) { Fail("the supporter may not enforce (" + (enforceWhy ?? "nothing owed") + ")"); yield break; }
                yield return Film("co10_cabinet_support", "Politics", "coal:verb:enforce");
                if (!sim.Enforce(_countryId, SupportAgreement.KeyOf(owed[0]), 30, out string enforcedWhy)) { Fail("the enforcement was refused (" + enforcedWhy + ")"); yield break; }
                CoalitionMotion demand = CoalitionVerbs.OpenOf(player, CoalitionVerb.Enforce, support);
                Debug.Log($"SHOT: co11_cabinet_enforced - {support} calls '{demand.DemandName}' due by {demand.Deadline:yyyy-MM-dd}; the strip's place 4: {StripWords(player, sim.CurrentDate, false)}");
                yield return Film("co11_cabinet_enforced", "Politics", "coal:play:" + support);
                SetDay(demand.Deadline);
                Step();
                Debug.Log($"SHOT: co12_cabinet_enforce_refused - '{demand.DemandName}' undelivered on its day: the demand {demand.State} ('{demand.Outcome}'), the item {owed[0].State}");
                if (demand.State != MotionState.Refused) { Fail("the enforcement on its day is " + demand.State); yield break; }
                yield return Film("co12_cabinet_enforce_refused", "Politics", "coal:play:" + support);

                // ---- re-declaring after a count (K137): the AI's sides, the player's own ----
                SetDay(today);
                DateTime count = today.AddDays(-30);
                player.ElectionHistory.Add(new ElectionRecord { Date = count, CountryId = _countryId.ToString(), Method = german ? ElectionMethod.GermanyNationalProportional : ElectionMethod.SwedenTwoTier });
                g = Fresh(opposition, today);
                Step();
                var sides = new List<string>();
                foreach (CoalitionMotion m in player.CoalitionMotions) { if (m.Verb == CoalitionVerb.Redeclare) { sides.Add(m.Actor + " " + m.Demand + (german ? string.Empty : " (bloc " + m.Bloc + ")")); } }
                bool mayRedeclare = sim.CanRedeclare(_countryId, BlocSide.Against, out string redeclareWhy);
                Debug.Log($"SHOT: co13_cabinet_redeclare - STAGED: a count planted on {count:yyyy-MM-dd} and its government installed today; K137's sides: {string.Join(", ", sides)}; the player {opposition}: RE-DECLARE {(mayRedeclare ? "offered" : "disabled - " + redeclareWhy)}, its composer open");
                if (sides.Count == 0 || !mayRedeclare) { Fail("no side was re-declared, or the player may not re-declare"); yield break; }
                SetPrivateField(controller, "_coalitionComposing", CoalitionVerb.Redeclare);
                yield return Film("co13_cabinet_redeclare", "Politics", null);
                SetPrivateField(controller, "_coalitionComposing", null);
                if (!sim.Redeclare(_countryId, BlocSide.Against, out string redeclared)) { Fail("the re-declaration was refused (" + redeclared + ")"); yield break; }
                Debug.Log($"SHOT: co14_docket_sides - the sides re-declared and the player's own (AGAINST) on the Docket; {opposition}'s bloc now {NationalElection.BlocOf(player, opposition)}");
                yield return Film("co14_docket_sides", "Decisions", null);

                // ---- Germany: a parliamentary group's member refused (§705) ----
                if (german)
                {
                    player.ElectionHistory.Clear(); player.ElectionHistory.AddRange(history0);
                    g = Fresh("CSU", today.AddDays(-1));
                    bool csuLeave = sim.CanLeaveGovernment(_countryId, out string csuLeaveWhy);
                    bool csuThreat = sim.CanThreaten(_countryId, out string csuThreatWhy);
                    Debug.Log($"SHOT: co15_cabinet_junior_refused - the player CSU: LEAVE {(csuLeave ? "OFFERED (expected refused)" : "disabled - " + csuLeaveWhy)}, THREATEN {(csuThreat ? "OFFERED (expected refused)" : "disabled - " + csuThreatWhy)}; LEAVE's refusal pinned");
                    if (csuLeave || csuThreat) { Fail("the CSU's verbs are offered"); yield break; }
                    yield return Film("co15_cabinet_junior_refused", "Politics", "coal:verb:leave");
                }
            }
            finally
            {
                player.Government = government0;
                player.ParliamentSeats = seats0;
                player.Divisions.Entries.Clear(); player.Divisions.Entries.AddRange(divisions0);
                player.CoalitionMotions.Clear(); player.CoalitionMotions.AddRange(motions0);
                player.ElectionHistory.Clear(); player.ElectionHistory.AddRange(history0);
                player.RedeclaredAfterCount = redeclared0;
                player.EnactedLaws.Clear(); player.EnactedLaws.AddRange(enacted0);
                player.BorderEnforcementLevel = border0; player.PoliceFundingLevel = police0;
                if (ministers0 != null) { player.CabinetMinisters = ministers0; }
                player.PlayerPartyAbbrev = own;
                player.PlayerHoldsPresidency = president0;
                sim.ImpactLedger = ledger0;
                clock.SetValue(sim, date0);
                SetPrivateField(controller, "_coalitionComposing", null);
                SetPrivateField(controller, "_coalitionPinnedPreview", null);
                (pins.GetValue(controller) as IList)?.Clear();
                if (IconLibrary.S9Fallbacks.Count > 0) { Debug.LogError("SHOT: COAL-2 - verb glyphs that did not load: " + string.Join(", ", IconLibrary.S9Fallbacks)); _failed++; }
                Debug.Log("SHOT: COAL-2 - the staged government, seats, divisions, record of verbs, election history, laws, dials, ministers, clock, party and presidency flag restored; the impact ledger watches the player's verbs again.");
            }
        }

        /// <summary>The strip's coalition chips' words on a day - place 3 (<paramref name="binds"/>) or place 4 - for the film's log.</summary>
        private static string StripWords(Country country, DateTime day, bool binds)
        {
            var three = new List<TopStrip.Chip>();
            var four = new List<TopStrip.Chip>();
            TopStrip.CoalitionChips(country, day, three, four);
            var words = new List<string>();
            foreach (TopStrip.Chip c in binds ? three : four) { words.Add(c.Words); }
            return words.Count > 0 ? string.Join(" | ", words) : "NONE";
        }
    }
}
