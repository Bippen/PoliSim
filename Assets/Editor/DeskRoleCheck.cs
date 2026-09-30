using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §694 (ruled 2026-09-30): **THE DESK READS THE PLAYER'S ROLE, AND THE RAIL LIGHTS ONE CELL.** Two legs, one check.
    /// (a) The effects note: every role the game seats (governing, junior partner, support, opposition) against every rate lever a country
    /// gives (the dial, the eurozone's push, none where a chair sets the rate), every tabling right and every state of the chamber - the
    /// subject is the player's draft only while the player governs, else what is before the chamber (the alternative tabled, the government's
    /// budget), else the alternative drafted; and no note
    /// names a lever the role does not hold. The pre-§694 note (the rate dial named to everyone) must be caught for an opposition player.
    /// (b) The rail: in every state - the Desk, each document, the campaign's page open over each, its cell present or not - exactly ONE
    /// cell is lit; the per-cell rule the rail had before (the campaign page left the document beneath it lit) must be caught lighting two,
    /// by name. (c) The wiring: the Desk and the rail read these rules, and every document tab maps to a rail caption.
    /// </summary>
    public static class DeskRoleCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== DeskRoleCheck (§694): the effects note names only a held lever; the rail lights exactly one cell ===\n");
            int failures = 0, cases = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                // (a) THE EFFECTS NOTE, over every role and state
                string[] rateLevers = { DeskEffectsNote.RateDial, DeskEffectsNote.RatePush, null };
                bool[] tf = { false, true };
                int wrongSubject = 0, unheldNamed = 0;
                string firstWrong = null, firstUnheld = null;
                foreach (bool governs in tf)
                foreach (bool mayTable in tf)
                foreach (string rate in rateLevers)
                foreach (bool draftMoves in tf)
                foreach (bool rateMoved in tf)
                foreach (bool tabled in tf)
                foreach (bool pending in tf)
                {
                    if (!governs && rateMoved) { continue; }          // a role that does not govern holds no rate lever to move
                    if (governs && rate == null && rateMoved) { continue; }   // nor a governing one where a chair sets the rate
                    if (governs && (tabled || pending)) { continue; }  // the player's own government tables no alternative against itself
                    if (tabled && (!mayTable || !pending)) { continue; }   // an alternative is tabled only against a pending budget, by a role that may
                    cases++;
                    DeskEffectsSubject s = DeskEffectsNote.SubjectOf(governs, draftMoves, rateMoved, tabled, mayTable, pending);
                    bool ownDraft = s == DeskEffectsSubject.YourDraft || s == DeskEffectsSubject.NothingDrafted;
                    bool subjectRight = governs ? ownDraft
                        : tabled ? s == DeskEffectsSubject.YourAlternativeTabled
                        : pending ? s == DeskEffectsSubject.GovernmentDraft   // what is before the chamber first (the first film's finding)
                        : mayTable && draftMoves ? s == DeskEffectsSubject.YourAlternativeDrafted
                        : s == DeskEffectsSubject.StandingBook;
                    string state = $"governs {governs}, may table {mayTable}, rate lever {rate ?? "none"}, draft moves {draftMoves}, rate moved {rateMoved}, tabled {tabled}, government's pending {pending}";
                    if (!subjectRight) { wrongSubject++; firstWrong = firstWrong ?? state + " -> " + s; }
                    string note = DeskEffectsNote.Line(s, governs ? rate : null, mayTable, 12) + " " + DeskEffectsNote.Method(s, 121);
                    List<string> unheld = DeskEffectsNote.UnheldLeversNamed(note, governs, governs ? rate : null, mayTable);
                    if (unheld.Count > 0) { unheldNamed++; firstUnheld = firstUnheld ?? state + " names " + string.Join(", ", unheld); }
                }
                Check(wrongSubject == 0, $"the subject is the player's draft only while the player governs, else the alternative tabled, else the government's budget, else the alternative drafted, else the book as it stands - {cases} states{(firstWrong != null ? "; FIRST WRONG: " + firstWrong : string.Empty)}");
                Check(unheldNamed == 0, $"no note names a lever the role does not hold - {cases} states{(firstUnheld != null ? "; FIRST: " + firstUnheld : string.Empty)}");
                string rateNote = DeskEffectsNote.Line(DeskEffectsSubject.NothingDrafted, DeskEffectsNote.RateDial, false, 0);
                string chairNote = DeskEffectsNote.Line(DeskEffectsSubject.NothingDrafted, null, false, 0);
                Check(rateNote.Contains(DeskEffectsNote.RateDial) && !chairNote.Contains("RATE"), "a governing player's empty note names the rate dial where the player holds it, and no rate lever where a chair sets the rate: " + chairNote);
                const string Pre694 = "NO DRAFT PENDING — ESTIMATES FOLLOW THE RATE DIAL AS DRAFTED AND EVERY BILL AS IT PASSES · NO MARGIN: THE PROJECTION IS DETERMINISTIC · SCALED DISPLAY ESTIMATE, NOT A SIMULATED SUB-YEAR VALUE";
                List<string> planted = DeskEffectsNote.UnheldLeversNamed(Pre694, governs: false, rateLever: null, mayTable: true);
                Check(planted.Count == 1 && planted[0] == DeskEffectsNote.RateDial, "THE PLANTED ERROR is caught: the pre-§694 note read to an opposition player names " + (planted.Count > 0 ? string.Join(", ", planted) : "NOTHING"));

                // (b) THE RAIL, over every state
                int railStates = 0, notOne = 0;
                string firstNotOne = null;
                var documents = RailLit.Documents;
                foreach (bool onDesk in tf)
                foreach (bool campaignOpen in tf)
                foreach (bool present in tf)
                foreach (string document in documents)
                {
                    if (campaignOpen && onDesk) { continue; }   // opening the campaign's page leaves the Desk (OpenLiveCampaign)
                    railStates++;
                    string key = RailLit.Of(onDesk, campaignOpen, present, document);
                    List<string> lit = RailLit.Lit(RailLit.Drawn(present), cell => cell == key);
                    if (lit.Count != 1) { notOne++; firstNotOne = firstNotOne ?? $"on the Desk {onDesk}, campaign open {campaignOpen}, its cell {present}, document {document}: {lit.Count} lit ({string.Join(", ", lit)})"; }
                }
                Check(notOne == 0, $"exactly one cell is lit in every state - {railStates} states{(firstNotOne != null ? "; FIRST: " + firstNotOne : string.Empty)}");
                // THE PLANTED ERROR: the per-cell rule before §694 - HOME lit on the Desk, a document lit when it is the tab and the Desk is not up, CAMPAIGN lit while its page is open
                bool oldOnDesk = false, oldCampaign = true; string oldTab = "BUDGET";
                List<string> two = RailLit.Lit(RailLit.Drawn(true), cell => cell == RailLit.Desk ? oldOnDesk : cell == RailLit.Campaign ? oldCampaign : !oldOnDesk && cell == oldTab);
                Check(two.Count == 2 && two.Contains("BUDGET") && two.Contains(RailLit.Campaign), "THE PLANTED ERROR is caught: the pre-§694 rule, the campaign's page open over the Budget, lights " + two.Count + " (" + string.Join(", ", two) + ")");

                // (c) THE WIRING - the rules above are the ones drawn
                Type gc = typeof(GameController);
                MethodInfo caption = gc.GetMethod("RailDocumentCaption", BindingFlags.NonPublic | BindingFlags.Static);
                Type tabEnum = gc.GetNestedType("ConsolidatedTab", BindingFlags.NonPublic);
                var unmapped = new List<string>();
                if (caption != null && tabEnum != null)
                {
                    foreach (object tab in Enum.GetValues(tabEnum))
                    {
                        string c = caption.Invoke(null, new[] { tab }) as string;
                        if (c == null || Array.IndexOf(documents, c) < 0) { unmapped.Add(tab + " -> " + (c ?? "null")); }
                    }
                }
                Check(caption != null && tabEnum != null && unmapped.Count == 0, "every document tab maps to one of the rail's seven captions" + (unmapped.Count > 0 ? ": UNMAPPED " + string.Join(", ", unmapped) : caption == null ? ": RailDocumentCaption NOT FOUND" : string.Empty));
                string root = Path.GetDirectoryName(Application.dataPath);
                string rail = File.ReadAllText(Path.Combine(root, "Assets/Scripts/UI/GameController.cs")) + File.ReadAllText(Path.Combine(root, "Assets/Scripts/UI/GameController.Campaign.cs"));
                int reads = CountOf(rail, "_railLitCell == ");
                int sets = CountOf(rail, "_railLitCell = RailLitKey();");
                Check(reads == 3 && sets == 1 && !rail.Contains("bool selected = !_onDesk && _consolidatedTab == tab") && !rail.Contains("bool selected = _liveCampaignOpen;") && !rail.Contains("bool active = _onDesk;"),
                    $"the rail's three cell painters (home, document, campaign) read the one rule - set {sets} time(s) per rail from RailLitKey(), read {reads} times, the per-cell rules gone");
                string desk = File.ReadAllText(Path.Combine(root, "Assets/Scripts/UI/GameController.Desk.cs"));
                Check(desk.Contains("DeskEffectsNote.SubjectOf(") && desk.Contains("DeskEffectsNote.Line(") && !desk.Contains("ESTIMATES FOLLOW THE RATE DIAL AS DRAFTED"),
                    "the Desk's effects card reads its subject and its note from DeskEffectsNote; the fixed note naming the rate dial to every role is gone");
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append((e.InnerException ?? e).Message).Append('\n'); }
            sb.Append(failures == 0 ? $"    CLEAN - {cases} note states and the rail's states each read true to the role\n" : $"    {failures} failure(s)\n");
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static int CountOf(string text, string needle)
        {
            int n = 0;
            for (int i = text.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = text.IndexOf(needle, i + needle.Length, StringComparison.Ordinal)) { n++; }
            return n;
        }
    }
}
