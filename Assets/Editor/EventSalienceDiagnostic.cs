using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// A-LOOP q5 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §6 (4)): <b>what an Editor harness can prove of events moving salience</b>, on the default
    /// epoch's world (seed 777). Outside (1d) the year's dice are off for its game days (`EventSystem.Enabled`, restored after), so only the events it
    /// lands move anything; the pool's events landed one by one never reach a boundary unrecorded.
    /// <list type="number">
    /// <item><b>Each event of the pool, through `EventSystem.ApplyEvent`</b>, on Sweden with nothing standing, against an INDEPENDENT oracle - the
    /// hand-list <see cref="ExpectedMovers"/> (the pool's adverse events of the generated classes, by name): a listed event moves exactly its issue
    /// by exactly its class's fitted jump (held under 1 − seed) and nothing else, with one `SALIENCE:` line; every other event moves nothing, and no
    /// event that neither costs output nor costs approval moves anything. The pool carries a crime and a war or security event, and every event of
    /// q5's tags says truly in its analogue whether it moves salience. (1d) Ten played years with the dice on: every listed event the game fires on
    /// any country but the USA logs its `SALIENCE:` line for that country (an issue already at the top of the scale excepted), and nothing else
    /// does. (1e) The USA moves no salience.</item>
    /// <item><b>The hold and the decay on the game's own days</b> (`SimulationManager.AdvanceDay`): each generated class's jump reads exactly the
    /// survey's jump through its hold, then shrinks by its daily persistence a day (to a float's step), stands at or above half its size on the last
    /// whole day of its fitted half-life after the hold and below half the day after; then the vector returns to the seed (the stored shift dropped,
    /// every reading the seed's to the bit).</item>
    /// <item><b>No salience leaves [0, 1]</b>: forty events of each generated class landed on every country, then ten years of decay.</item>
    /// <item><b>The readers</b>: the stance model's opinion cost of a social-security cut grows on every country an Economy event lands on (every
    /// party's alignment at or below the seed's, one at least strictly, the reason naming the raised salience); Sweden's campaign wave measures no
    /// generated class's issue and reads exactly its seed; Germany's campaign reads Economy and Immigration raised by exactly the shift, and an
    /// Economy shift changes a campaign OUTCOME - an Economy issue message's relevance and per-contact effect against the norm, and (4e) Germany's
    /// campaign played through with and without the shift ends on different shares - while the general message's relevance against the norm is
    /// printed (it cancels, `EventSalience`'s declared reading).</item>
    /// <item><b>The save, the record and the clone</b>: a mid-decay save loads the shift exactly and the two games stay equal thirty days on; a save of
    /// the format before is refused with the plain message; a campaign record's shift survives the JSON; the preview's clone carries its own copy.</item>
    /// </list>
    /// Prints `EVENT SALIENCE: PASS` and exits 0 when every check holds; each failure is an `EVENT SALIENCE:` error naming its check.
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.EventSalienceDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class EventSalienceDiagnostic
    {
        /// <summary>THE ORACLE, written out by hand (review 882's H-1): the pool's events that move salience while the fit generates the Economy and
        /// Migration classes - each costs output or approval and carries a tag of a generated class (War among the Economy tags) - with the issue each moves. The four booms with
        /// a small inflation shock (Consumer Confidence Surge, Tourism Boom, Stock Market Rally, Export Boom in a Key Partner) are not on it. A refit
        /// that changes which classes are generated fails (1a) until this list is re-read against the pool.</summary>
        private static readonly Dictionary<string, IssueId> ExpectedMovers = new Dictionary<string, IssueId>
        {
            { "Recession in a Trading Partner", IssueId.Economy }, { "Global Commodity Price Spike", IssueId.Economy }, { "Energy Price Shock", IssueId.Economy },
            { "Banking Sector Stress", IssueId.Economy }, { "Major Cyberattack on Financial Infrastructure", IssueId.Economy }, { "Regional Conflict Disrupts Trade Routes", IssueId.Economy },
            { "Sovereign Credit Rating Downgrade", IssueId.Economy }, { "Major Labor Strikes", IssueId.Economy }, { "Housing Market Correction", IssueId.Economy },
            { "Currency Under Attack", IssueId.Economy }, { "Sharp Rate Rise Abroad", IssueId.Economy }, { "Pension Fund Losses", IssueId.Economy },
            { "Global Recession", IssueId.Economy }, { "Sovereign Debt Crisis Contagion", IssueId.Economy }, { "Major Industrial Accident", IssueId.Economy },
            { "Infrastructure Collapse", IssueId.Economy }, { "Tech Sector Bust", IssueId.Economy }, { "Major Factory Closure", IssueId.Economy },
            { "Currency Appreciation Shock", IssueId.Economy }, { "Wage Round Overshoots", IssueId.Economy }, { "Trade Partner Turns Protectionist", IssueId.Economy },
            { "War on the Doorstep", IssueId.Economy },   // review 882b's M-C: tagged War, which moves the Economy slot
            { "Migration Surge", IssueId.Immigration },
        };

        private const int PlayedYears = 10;

        private static int _failures;
        private static StringBuilder _sb;
        private static readonly List<string> SalienceLines = new List<string>();

        public static void Run()
        {
            CheckExit.ArmLogFold();   // the worlds cross boundaries: an ATTRIB during the run fails it
            using IDisposable epoch = SimulationManager.EpochScope();
            SimulationManager.SetEpoch(SimulationManager.DefaultEpoch);   // the default epoch named, never the process's
            _failures = 0;
            SalienceLines.Clear();
            _sb = new StringBuilder("EVENT SALIENCE: A-LOOP q5 - events move salience (the default epoch, seed 777)\n");
            foreach (SalienceClass c in (SalienceClass[])Enum.GetValues(typeof(SalienceClass)))
            {
                _sb.Append(F("    class {0,-9} -> {1,-11} jump {2:0.0000}, half-life {3} days{4}\n", c, EventSalience.IssueOf(c), EventSalience.JumpOf(c),
                    EventSalience.JumpOf(c) > 0f ? EventSalience.HalfLifeDays(c).ToString("F1", CultureInfo.InvariantCulture) : "-", EventSalience.JumpOf(c) > 0f ? "" : " (inert: the fit could not resolve it)"));
            }
            bool eventsWere = EventSystem.Enabled;
            Application.logMessageReceived += Count;
            try
            {
                Pool();
                Played();
                Decay();
                Bounds();
                Readers();
                SaveRecordClone();
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            finally { Application.logMessageReceived -= Count; EventSystem.Enabled = eventsWere; }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "EVENT SALIENCE: PASS - each listed event moves its issue by its class's fitted jump and logs it (landed and played), the decay keeps the fitted half-life and returns to the seed, nothing leaves [0, 1], the stance model and the campaign read it, and the save, the record and the clone carry it."
                : F("EVENT SALIENCE: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        private static void Count(string message, string stack, LogType type) { if (message != null && message.StartsWith("SALIENCE:", StringComparison.Ordinal)) { SalienceLines.Add(message); } }

        // ============================================================ (1) the pool, through the game's own application, against the hand-list
        private static void Pool()
        {
            _sb.Append("    1. each event of the pool, through EventSystem.ApplyEvent (Sweden, nothing standing), against the hand-list\n");
            List<EconomicEvent> pool = PoolOf();
            bool crime = false, security = false;
            WithWorld((sim, world) =>
            {
                Country se = world.GetCountry(CountryId.Sweden);
                int moved = 0, wrong = 0, booms = 0, logsBefore = SalienceLines.Count;
                foreach (EconomicEvent e in pool)
                {
                    crime |= e.Tag == EventTag.Crime; security |= e.Tag == EventTag.Security || e.Tag == EventTag.War;   // the s882c review's L-4
                    se.SalienceShift = null;
                    EventSystem.ApplyEvent(se, e);
                    bool favourable = e.GdpShockPercent >= 0f && e.ApprovalEffect >= 0f;
                    if (favourable) { booms++; }
                    if (!ExpectedMovers.TryGetValue(e.Name, out IssueId issue))
                    {
                        if (se.SalienceShift != null) { wrong++; Check(false, F("(1a) {0} ({1}{2}) moved salience, and it is not on the hand-list", e.Name, e.Tag, favourable ? ", costing neither output nor approval" : "")); }
                        continue;
                    }
                    moved++;
                    TryClassOf(issue, out SalienceClass c);
                    float seed = StanceModel.SalienceOf(CountryId.Sweden, issue, out _);
                    float want = Mathf.Clamp(EventSalience.JumpOf(c), -seed, 1f - seed);
                    bool only = se.SalienceShift != null && want > 0f;
                    for (int i = 0; only && i < se.SalienceShift.Length; i++) { only &= i == (int)issue ? se.SalienceShift[i] == want : se.SalienceShift[i] == 0f; }
                    if (!only) { wrong++; Check(false, F("(1a) {0} ({1}): {2} should move by exactly {3:R} and nothing else; the shift reads [{4}]", e.Name, e.Tag, issue, want, se.SalienceShift == null ? "null" : string.Join(", ", se.SalienceShift))); }
                }
                Check(wrong == 0 && moved == ExpectedMovers.Count, F("(1a) {0} events: the {1} on the hand-list ({2} named) moved exactly their issue by their class's fitted jump, the rest - {3} costing neither output nor approval among them - moved nothing",
                    pool.Count, moved, ExpectedMovers.Count, booms));
                Check(SalienceLines.Count - logsBefore == moved, F("(1b) one SALIENCE: line per moving event ({0} lines, {1} moving events)", SalienceLines.Count - logsBefore, moved));
            });
            int untrue = 0;
            foreach (EconomicEvent e in pool)
            {
                if (e.Tag != EventTag.Crime && e.Tag != EventTag.Security && e.Tag != EventTag.Migration && e.Tag != EventTag.War
                    && (e.Analogue == null || e.Analogue.IndexOf("salience", StringComparison.OrdinalIgnoreCase) < 0)) { continue; }
                bool says = e.Analogue != null && e.Analogue.Contains("It moves no salience");
                if (says == ExpectedMovers.ContainsKey(e.Name)) { untrue++; Check(false, F("(1c) {0} ({1}): its analogue {2}, and the hand-list says it {3}", e.Name, e.Tag, says ? "says it moves no salience" : "does not say it moves no salience", says ? "moves" : "moves nothing")); }
            }
            Check(crime && security && untrue == 0, F("(1c) the pool carries a crime event ({0}) and a war or security event ({1}), and every event of q5's tags says truly whether it moves salience", crime, security));
        }

        // ============================================================ (1d) a played run: what the game fires, it logs
        private static void Played()
        {
            _sb.Append(F("    1d. {0} played years, the dice on: each listed event the game fires logs its SALIENCE: line, and nothing else does\n", PlayedYears));
            WithWorld((sim, world) =>
            {
                EventSystem.Enabled = true;
                var expected = new List<string>();
                int before = SalienceLines.Count, fired = 0, atTop = 0, usa = 0;
                var topBefore = new Dictionary<CountryId, bool[]>();
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int d = 0; d < PlayedYears * SimulationManager.DaysPerTurn; d++)
                {
                    if (!sim.AdvanceDay()) { continue; }
                    foreach (Country k in world.Countries)   // review 882b's L-E: an issue already at the scale's top moves nothing and logs nothing
                    {
                        var tops = new bool[IssueVector.IssueCount];
                        foreach (IssueId i in (IssueId[])Enum.GetValues(typeof(IssueId))) { tops[(int)i] = EventSalience.Of(k, i) >= 1f - 1e-6f; }
                        topBefore[k.Id] = tops;
                    }
                    sim.AdvanceTurn(decisions);
                    foreach (Country k in world.Countries)
                    {
                        EconomicEvent e = sim.GetLastEvent(k.Id);
                        if (e == null) { continue; }
                        fired++;
                        if (!ExpectedMovers.TryGetValue(e.Name, out IssueId issue)) { continue; }
                        if (!EventSalience.MovesSalience(k.Id)) { usa++; continue; }
                        if (topBefore[k.Id][(int)issue]) { atTop++; continue; }
                        expected.Add(F("SALIENCE: {0} - {1} (", k.Id, e.Name));
                    }
                }
                List<string> got = SalienceLines.Skip(before).ToList();
                var unmatched = new List<string>(got);
                int missing = 0;
                foreach (string want in expected)
                {
                    int at = unmatched.FindIndex(l => l.StartsWith(want, StringComparison.Ordinal));
                    if (at < 0) { missing++; } else { unmatched.RemoveAt(at); }
                }
                Check(fired > 0 && expected.Count > 0 && missing == 0 && unmatched.Count == 0,
                    F("(1d) {0} events fired on {1} countries; {2} on the hand-list, each with its SALIENCE: line ({3} missing); {4} lines no listed event accounts for{5}; {6} listed on the USA (no salience), {7} on an issue already at the top",
                        fired, world.Countries.Count, expected.Count, missing, unmatched.Count, unmatched.Count > 0 ? ": " + unmatched[0] : "", usa, atTop));
                Country us = world.GetCountry(CountryId.USA);
                if (us != null)
                {
                    us.SalienceShift = null;
                    foreach (string name in ExpectedMovers.Keys) { EventSalience.Land(us, PoolOf().First(p => p.Name == name)); }
                    Check(us.SalienceShift == null, "(1e) every listed event landed on the USA moves nothing (its seeds are Gallup's single-mention figures; no analogue fitted)");
                }
                else { Check(false, "(1e) the world carries no USA"); }
                EventSystem.Enabled = false;
            });
        }

        // ============================================================ (2) the decay on the game's days, and the return to the seed
        private static void Decay()
        {
            _sb.Append("    2. the decay on the game's own days, and the return to the seed\n");
            WithWorld((sim, world) =>
            {
                EventSystem.Enabled = false;
                Country se = world.GetCountry(CountryId.Sweden);
                var classes = new List<SalienceClass>();
                foreach (SalienceClass c in (SalienceClass[])Enum.GetValues(typeof(SalienceClass))) { if (EventSalience.JumpOf(c) > 0f) { classes.Add(c); EventSalience.Land(se, FirstMoving(c)); } }
                if (classes.Count == 0) { Check(false, "(2) no class is generated - nothing decays (the fit generated none)"); return; }
                var start = (float[])se.SalienceShift.Clone();
                var series = new Dictionary<SalienceClass, List<float>>();
                foreach (SalienceClass c in classes) { series[c] = new List<float> { start[(int)EventSalience.IssueOf(c)] }; }
                int longest = 0;
                foreach (SalienceClass c in classes) { longest = Math.Max(longest, EventSalience.HoldDaysOf(c) + (int)Math.Floor(EventSalience.HalfLifeDays(c)) + 1); }
                Days(sim, longest, () => { foreach (SalienceClass c in classes) { series[c].Add(se.SalienceShift == null ? 0f : se.SalienceShift[(int)EventSalience.IssueOf(c)]); } });
                foreach (SalienceClass c in classes)
                {
                    List<float> s = series[c];
                    float p = EventSalience.DailyPersistence(c);
                    int hold = EventSalience.HoldDaysOf(c);
                    int h = (int)Math.Floor(EventSalience.HalfLifeDays(c));
                    bool held = s[0] == EventSalience.JumpOf(c);
                    for (int d = 1; d <= hold; d++) { held &= s[d] == s[0]; }
                    Check(held && hold > 0, F("(2h) {0}: the reading holds exactly the survey's jump {1:R} through its hold of {2} days (day {2} holds {3:R})", c, EventSalience.JumpOf(c), hold, s[hold]));
                    Check(Math.Abs(s[hold + 1] - s[hold] * p) <= 1e-6f * Math.Abs(s[hold]), F("(2a) {0}: the first day after the hold keeps the daily persistence to a float's step (the in-place `*=` rounds once, n882: 1 ulp) {1:R} ({2:R} -> {3:R})", c, p, s[hold], s[hold + 1]));
                    Check(s[hold + h] >= s[0] / 2f && s[hold + h + 1] < s[0] / 2f, F("(2b) {0}: half the jump on the fitted half-life after the hold ({1:F1} days) - day {2} holds {3:F5}, day {4} {5:F5}, half of {6:F5}", c, EventSalience.HalfLifeDays(c), hold + h, s[hold + h], hold + h + 1, s[hold + h + 1], s[0]));
                }
                int days = 0, cap = 40 * longest;
                while (se.SalienceShift != null && days < cap) { EventSalience.AdvanceDay(se); days++; }
                bool atSeed = se.SalienceShift == null;
                foreach (IssueId issue in (IssueId[])Enum.GetValues(typeof(IssueId))) { atSeed &= EventSalience.Of(se, issue) == StanceModel.SalienceOf(CountryId.Sweden, issue, out _); }
                Check(atSeed, F("(2c) the vector returns to the seed: the stored shift dropped {0} days after the game days above (cap {1}), every reading the seed's to the bit", days, cap));
            });
        }

        // ============================================================ (3) the bounds
        private static void Bounds()
        {
            _sb.Append("    3. no salience leaves [0, 1]\n");
            WithWorld((sim, world) =>
            {
                int outside = 0, saturated = 0, landed = 0;
                foreach (Country k in world.Countries)
                {
                    foreach (SalienceClass c in (SalienceClass[])Enum.GetValues(typeof(SalienceClass)))
                    {
                        if (!(EventSalience.JumpOf(c) > 0f)) { continue; }
                        for (int i = 0; i < 40; i++) { EventSalience.Land(k, FirstMoving(c)); landed++; }
                        if (EventSalience.Of(k, EventSalience.IssueOf(c)) >= 1f - 1e-6f) { saturated++; }
                    }
                    for (int d = 0; d < 3650 && k.SalienceShift != null; d++)
                    {
                        foreach (IssueId issue in (IssueId[])Enum.GetValues(typeof(IssueId))) { float v = EventSalience.Of(k, issue); if (float.IsNaN(v) || v < 0f || v > 1f) { outside++; } }
                        EventSalience.AdvanceDay(k);
                    }
                }
                Check(outside == 0 && saturated > 0, F("(3) {0} events landed on {1} countries, then ten years of decay: {2} readings outside [0, 1]; {3} saturated issues read 1 (within a float's rounding)", landed, world.Countries.Count, outside, saturated));
            });
        }

        // ============================================================ (4) the readers
        private static void Readers()
        {
            _sb.Append("    4. the stance model and the campaign read it\n");
            if (!(EventSalience.JumpOf(SalienceClass.Economy) > 0f)) { Check(false, "(4) the Economy class is inert - the readers are not tested"); return; }
            WithWorld((sim, world) =>
            {
                BillConcern Cut() { var concern = new BillConcern { Direction = -1f }.Add(StanceAxis.SpendVsTax, 1f); concern.Cuts.Add((SpendingCategory.SocialSecurity, null, 0.5f)); return concern; }
                var lines = new List<string>();
                int tested = 0, wrongWay = 0;
                foreach (Country k in world.Countries)
                {
                    k.SalienceShift = null;
                    if (!EventSalience.MovesSalience(k.Id)) { EventSalience.Land(k, FirstMoving(SalienceClass.Economy)); lines.Add(k.Id + (k.SalienceShift == null ? " excluded (no shift)" : " MOVED")); if (k.SalienceShift != null) { wrongWay++; } continue; }
                    List<PartyStance> before = StanceModel.Stances(k, Cut());
                    EventSalience.Land(k, FirstMoving(SalienceClass.Economy));
                    List<PartyStance> after = StanceModel.Stances(k, Cut());
                    if (before.Count == 0) { lines.Add(k.Id + " no chamber"); continue; }
                    int lower = 0, higher = 0, named = 0;
                    for (int i = 0; i < Math.Min(before.Count, after.Count); i++)
                    {
                        if (!before[i].Measured || !after[i].Measured) { continue; }
                        if (after[i].Alignment < before[i].Alignment - 1e-6f) { lower++; }
                        if (after[i].Alignment > before[i].Alignment + 1e-6f) { higher++; }
                        foreach (string r in after[i].Reasons) { if (r.Contains("raised by recent events")) { named++; break; } }
                    }
                    tested++;
                    if (before.Count != after.Count || lower == 0 || higher > 0 || named == 0) { wrongWay++; }
                    lines.Add(F("{0} {1} lower, {2} higher, {3} named", k.Id, lower, higher, named));
                }
                Check(tested >= 1 && wrongWay == 0, F("(4a) a 50 % social-security cut, Economy raised by an Economy event, on {0} countries' chambers: {1}", tested, string.Join("; ", lines)));
            });
            WithWorld((sim, world) =>
            {
                Country se = world.GetCountry(CountryId.Sweden);
                se.SalienceShift = null;
                foreach (SalienceClass c in (SalienceClass[])Enum.GetValues(typeof(SalienceClass))) { if (EventSalience.JumpOf(c) > 0f) { EventSalience.Land(se, FirstMoving(c)); } }
                float[] shift = EventSalience.Snapshot(se);
                if (!Stage(CountryId.Sweden, out CampaignRun.Setup seSeed) || !Stage(CountryId.Sweden, out CampaignRun.Setup seMoved)) { Check(false, "(4b) Sweden's campaign could not be staged"); return; }
                EventSalience.ApplyToCampaign(shift, seMoved.TrueSalience);
                Check(Same(seSeed.TrueSalience, seMoved.TrueSalience), F("(4b) Sweden's campaign wave measures no generated class's issue (Economy, Immigration NaN): it reads its seed exactly - [{0}]", string.Join(", ", seMoved.TrueSalience.Select(v => v.ToString("0.00", CultureInfo.InvariantCulture)))));

                if (!Stage(CountryId.Germany, out CampaignRun.Setup deSeed) || !Stage(CountryId.Germany, out CampaignRun.Setup deMoved)) { Check(false, "(4c) Germany's campaign could not be staged"); return; }
                EventSalience.ApplyToCampaign(shift, deMoved.TrueSalience);
                int econ = (int)IssueId.Economy, imm = (int)IssueId.Immigration;
                bool exact = true;
                for (int i = 0; i < deSeed.TrueSalience.Length; i++)
                {
                    double want = double.IsNaN(deSeed.TrueSalience[i]) ? double.NaN : Math.Min(1.0, Math.Max(0.0, deSeed.TrueSalience[i] + shift[i]));
                    exact &= double.IsNaN(want) ? double.IsNaN(deMoved.TrueSalience[i]) : Math.Abs(deMoved.TrueSalience[i] - want) < 1e-12;
                }
                Check(exact && deMoved.TrueSalience[econ] > deSeed.TrueSalience[econ] && deMoved.TrueSalience[imm] > deSeed.TrueSalience[imm],
                    F("(4c) Germany's campaign reads its wave moved by exactly the shift: Economy {0:F4} -> {1:F4}, Immigration {2:F4} -> {3:F4}", deSeed.TrueSalience[econ], deMoved.TrueSalience[econ], deSeed.TrueSalience[imm], deMoved.TrueSalience[imm]));

                // an OUTCOME, on Germany's campaign with the Economy shift alone (so the raised issue is one): the electorate's top issue, the
                // parties' ground, and an Economy issue message's relevance against the norm; the general message's against the norm printed
                var econOnly = new float[shift.Length]; econOnly[econ] = shift[econ];
                Stage(CountryId.Germany, out CampaignRun.Setup deEcon);
                EventSalience.ApplyToCampaign(econOnly, deEcon.TrueSalience);
                MethodInfo top = typeof(CampaignRun).GetMethod("IsTopSalience", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo ground = typeof(CampaignRun).GetMethod("TopIssue", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo message = typeof(CampaignRun).GetMethod("TrueMessage", BindingFlags.NonPublic | BindingFlags.Static);
                if (top == null || ground == null || message == null) { Check(false, "(4d) CampaignRun's IsTopSalience, TopIssue or TrueMessage not found by reflection - the outcome is not tested"); return; }
                IssueId TopOf(CampaignRun.Setup s) { foreach (IssueId i in (IssueId[])Enum.GetValues(typeof(IssueId))) { if ((bool)top.Invoke(null, new object[] { s, i })) { return i; } } return IssueId.Economy; }
                double Relevance(CampaignRun.Setup s, int p, IssueId? issue) { var args = new object[] { s, p, issue, 0.0, 0.0 }; message.Invoke(null, args); return (double)args[3] * (double)args[4]; }
                int grounds = 0;
                for (int p = 0; p < deSeed.Parties.Length; p++) { if ((IssueId)ground.Invoke(null, new object[] { deSeed, p }) != (IssueId)ground.Invoke(null, new object[] { deEcon, p })) { grounds++; } }
                double normSeed = CampaignPersuasion.ElectorateMean(deSeed), normEcon = CampaignPersuasion.ElectorateMean(deEcon);
                double issueSeed = Relevance(deSeed, 0, IssueId.Economy) / normSeed, issueEcon = Relevance(deEcon, 0, IssueId.Economy) / normEcon;
                double generalSeed = Relevance(deSeed, 0, null) / normSeed, generalEcon = Relevance(deEcon, 0, null) / normEcon;
                IssueId topSeed = TopOf(deSeed), topEcon = TopOf(deEcon);
                // the per-contact effect in points (q24's own function) of party 0's Economy message, each contact class, before and after
                double cred = deSeed.Parties[0].Credibility;
                int up = 0, down = 0;
                var effects = new List<string>();
                foreach (ContactClass k in (ContactClass[])Enum.GetValues(typeof(ContactClass)))
                {
                    double a = CampaignPersuasion.PerContactPp(k, 1.0, Relevance(deSeed, 0, IssueId.Economy) * cred, normSeed);
                    double b = CampaignPersuasion.PerContactPp(k, 1.0, Relevance(deEcon, 0, IssueId.Economy) * cred, normEcon);
                    if (b > a + 1e-12) { up++; } else if (b < a - 1e-12) { down++; }
                    effects.Add(F("{0} {1:F4} -> {2:F4} pp", k, a, b));
                }
                Check(normSeed > 0 && issueEcon > issueSeed && up > 0 && down == 0,
                    F("(4d) an Economy shift of {0:F4} moves Germany's campaign: party 0's Economy message against the norm {1:F4} -> {2:F4}, its per-contact effect {3}; the electorate's top issue {4} -> {5}; {6} of {7} parties' ground changed; its GENERAL message against the norm {8:F4} -> {9:F4} (the common factor cancels - declared)",
                        econOnly[econ], issueSeed, issueEcon, string.Join(", ", effects), topSeed, topEcon, grounds, deSeed.Parties.Length, generalSeed, generalEcon));

                // (4e) review 882b's L-F: Germany's campaign PLAYED through, the same seed, at its seed and with the Economy shift
                double[] Play(CampaignRun.Setup s)
                {
                    SimulationRandom.Seed(777);
                    CampaignRun.State st = CampaignRun.Begin(s, SimulationRandom.For(SimulationRandom.Stream.CampaignAi), SimulationRandom.For(SimulationRandom.Stream.Debate), SimulationRandom.For(SimulationRandom.Stream.Scandal));
                    while (!st.Finished) { CampaignRun.StepDay(st); }
                    return CampaignRun.Finish(st).FinalShares;
                }
                Stage(CountryId.Germany, out CampaignRun.Setup playSeed);
                Stage(CountryId.Germany, out CampaignRun.Setup playEcon);
                EventSalience.ApplyToCampaign(econOnly, playEcon.TrueSalience);
                double[] a0 = Play(playSeed), a1 = Play(playEcon);
                double widest = 0.0; int widestAt = 0;
                for (int p = 0; p < a0.Length; p++) { if (Math.Abs(a1[p] - a0[p]) > widest) { widest = Math.Abs(a1[p] - a0[p]); widestAt = p; } }
                Check(widest > 1e-9, F("(4e) Germany's campaign played through on one seed ends on different shares with the Economy shift: the widest move {0:+0.0000;-0.0000} points ({1})",
                    100.0 * (a1[widestAt] - a0[widestAt]), playSeed.Parties[widestAt].Name));
            });
        }

        // ============================================================ (5) the save, the record and the clone
        private static void SaveRecordClone()
        {
            _sb.Append("    5. the save, the campaign record and the preview's clone\n");
            var hosts = new List<GameObject>();
            try
            {
                (SimulationManager sim, World world, GameObject go) = Open(); hosts.Add(go);
                EventSystem.Enabled = false;
                Country se = world.GetCountry(CountryId.Sweden);
                Days(sim, 10, null);
                foreach (SalienceClass c in (SalienceClass[])Enum.GetValues(typeof(SalienceClass))) { if (EventSalience.JumpOf(c) > 0f) { EventSalience.Land(se, FirstMoving(c)); } }
                Days(sim, 20, null);
                string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null));
                SaveGame loaded = SaveGameService.Deserialize(json);
                var goB = new GameObject("SALIENCE.loaded"); hosts.Add(goB);
                SimulationManager simB = goB.AddComponent<SimulationManager>();
                SaveGameService.RestoreInto(simB, loaded);
                Country back = simB.World.GetCountry(CountryId.Sweden);
                Check(Same(se.SalienceShift, back.SalienceShift) && se.SalienceShift != null && se.SalienceHoldDays != null && back.SalienceHoldDays != null && se.SalienceHoldDays.SequenceEqual(back.SalienceHoldDays),
                    F("(5a) loaded into a manager, the shift and its holds come back exactly ([{0}]; holds [{1}])", se.SalienceShift == null ? "null" : string.Join(", ", se.SalienceShift), se.SalienceHoldDays == null ? "null" : string.Join(", ", se.SalienceHoldDays)));
                Days(sim, 30, null);
                Days(simB, 30, null);
                Check(Same(se.SalienceShift, back.SalienceShift), "(5b) thirty days on, the continuous game and the loaded one hold the same shift");

                string older = json.Replace("\"SaveVersion\": " + SaveGameService.CurrentSaveVersion.ToString(CultureInfo.InvariantCulture), "\"SaveVersion\": " + (SaveGameService.CurrentSaveVersion - 1).ToString(CultureInfo.InvariantCulture));
                bool refused = false; string why = null;
                try { SaveGameService.Deserialize(older); } catch (SaveLoadException e) { refused = true; why = e.Message; }
                Check(older != json && refused && why.Contains("format version " + (SaveGameService.CurrentSaveVersion - 1).ToString(CultureInfo.InvariantCulture)),
                    F("(5c) a save of the format before this build's ({0}) is refused with the plain message (ruling A): \"{1}\"", SaveGameService.CurrentSaveVersion, why));

                var record = new PlayerCampaignRecord { ElectionDate = sim.CurrentDate, StartDate = sim.CurrentDate, SalienceShift = EventSalience.Snapshot(se) };
                var recordBack = JsonConvert.DeserializeObject<PlayerCampaignRecord>(JsonConvert.SerializeObject(record, SaveGameService.BuildSettings()), SaveGameService.BuildSettings());
                Check(Same(record.SalienceShift, recordBack.SalienceShift) && !ReferenceEquals(record.SalienceShift, se.SalienceShift), "(5d) a campaign record's shift is its own copy and survives the save's JSON");

                MethodInfo cloneOf = typeof(SimulationManager).GetMethod("ClonePreviewCountry", BindingFlags.NonPublic | BindingFlags.Static);
                var clone = cloneOf == null ? null : (Country)cloneOf.Invoke(null, new object[] { se });
                Check(clone != null && Same(se.SalienceShift, clone.SalienceShift) && !ReferenceEquals(se.SalienceShift, clone.SalienceShift)
                      && clone.SalienceHoldDays != null && clone.SalienceHoldDays.SequenceEqual(se.SalienceHoldDays) && !ReferenceEquals(se.SalienceHoldDays, clone.SalienceHoldDays), "(5e) the preview's clone carries the shift and its holds as its own copies");
            }
            finally { foreach (GameObject go in hosts) { Object.DestroyImmediate(go); } }
        }

        // ============================================================ helpers
        private static List<EconomicEvent> PoolOf()
        {
            FieldInfo poolField = typeof(EventSystem).GetField("EventPool", BindingFlags.Static | BindingFlags.NonPublic);   // the pool is private; EventPoolCheck reads it the same way
            return poolField?.GetValue(null) as List<EconomicEvent> ?? new List<EconomicEvent>();
        }

        /// <summary>The class whose issue is <paramref name="issue"/> (each issue is one class's).</summary>
        private static bool TryClassOf(IssueId issue, out SalienceClass c) => EventSalience.TryClassOfIssue(issue, out c);

        /// <summary>The pool's first event of a class on the hand-list - the event a test lands for it.</summary>
        private static EconomicEvent FirstMoving(SalienceClass c)
        {
            foreach (EconomicEvent e in PoolOf()) { if (ExpectedMovers.TryGetValue(e.Name, out IssueId issue) && TryClassOf(issue, out SalienceClass k) && k == c) { return e; } }
            throw new InvalidOperationException("the hand-list names no event of the class " + c);
        }

        private static bool Stage(CountryId country, out CampaignRun.Setup setup) => LiveCampaignSetup.TryFor(country, new (int, int, Scandal)[0], null, out setup, out _);

        private static (SimulationManager, World, GameObject) Open()
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("SALIENCE");
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            sim.PlayerCountryId = CountryId.Sweden;
            return (sim, world, go);
        }

        private static void WithWorld(Action<SimulationManager, World> body)
        {
            (SimulationManager sim, World world, GameObject go) = Open();
            try { body(sim, world); }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary><paramref name="n"/> days of the game, each boundary resolved with no policy; <paramref name="after"/> read after each day.</summary>
        private static void Days(SimulationManager sim, int n, Action after)
        {
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country k in sim.World.Countries) { decisions[k.Id] = PolicyDecision.None(); }
            for (int d = 0; d < n; d++)
            {
                if (sim.AdvanceDay()) { sim.AdvanceTurn(decisions); }
                after?.Invoke();
            }
        }

        private static bool Same(float[] a, float[] b)
        {
            if (a == null || b == null) { return a == b; }
            if (a.Length != b.Length) { return false; }
            for (int i = 0; i < a.Length; i++) { if (a[i] != b[i]) { return false; } }
            return true;
        }

        private static bool Same(double[] a, double[] b)
        {
            if (a == null || b == null) { return a == b; }
            if (a.Length != b.Length) { return false; }
            for (int i = 0; i < a.Length; i++) { if (!(a[i] == b[i] || (double.IsNaN(a[i]) && double.IsNaN(b[i])))) { return false; } }
            return true;
        }

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "      ok   " : "      FAIL ").Append(what).Append('\n');
            if (!ok) { _failures++; Debug.LogError("EVENT SALIENCE: FAIL - " + what); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
