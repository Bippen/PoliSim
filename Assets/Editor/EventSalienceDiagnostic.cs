using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
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
    /// hand-list `ExpectedMovers` (the pool's adverse events of the generated classes, by name, each with the severity K68 gives it, worked by hand
    /// from the pool's figures): a listed event moves exactly its issue by its severity times its class's fitted jump (held under 1 − seed) and
    /// nothing else, with one `SALIENCE:` line; every other event moves nothing, and no event that neither costs output nor costs approval moves
    /// anything. The pool carries a crime and a war or security event, and every event of q5's tags says truly in its analogue whether it moves
    /// salience. (1d) Ten played years with the dice on: every listed event the game fires on any country logs its `SALIENCE:` line for that country
    /// (an issue already at the top of the scale excepted), and nothing else does. (1e) K103: on the USA every listed event moves its issue by the
    /// same severity times jump, at scale one - the dropped conversion applied nowhere.</item>
    /// <item><b>The hold and the decay on the game's own days</b> (`SimulationManager.AdvanceDay`): each generated class's anchor event reads exactly
    /// the survey's jump through its hold, then shrinks by its daily persistence a day (to a float's step), stands at or above half its size on the
    /// last whole day of its fitted half-life after the hold and below half the day after; then the vector returns to the seed (the stored shift
    /// dropped, every reading the seed's to the bit).</item>
    /// <item><b>No salience leaves [0, 1]</b>: forty events of each generated class landed on every country, then ten years of decay.</item>
    /// <item><b>The readers</b>: the stance model's opinion cost of a social-security cut grows on every country an Economy event lands on (every
    /// party's alignment at or below the seed's, one at least strictly, the reason naming the raised salience); Sweden's and Germany's campaigns read
    /// their waves moved by exactly the shift where the wave measures the issue (Sweden's Climate by the Natural class), and an Economy shift changes
    /// a campaign OUTCOME - an Economy issue message's relevance and per-contact effect against the norm, and (4e) Germany's campaign played through
    /// with and without the shift ends on different shares - while the general message's relevance against the norm is printed (it cancels,
    /// `EventSalience`'s declared reading).</item>
    /// <item><b>The save, the record and the clone</b>: a mid-decay save loads the shift exactly and the two games stay equal thirty days on; a save of
    /// the format before is refused with the plain message; a campaign record's shift survives the JSON; the preview's clone carries its own copy.</item>
    /// <item><b>Elias's rulings of 2026-10-23</b>: (6a) K68, each class's anchors are the pool's own events of the class and land exactly the fitted
    /// jump, the 2022 shock ("War on the Doorstep") the Economy fit's figure and its hold; (6b) K68, a smaller event lands proportionally less;
    /// (6c) K69, the jump is read at the excess's peak - Health and Natural move, at their peak waves, and Economy and Migration, whose peak is the
    /// post-wave, keep the earlier fit's figures; (6d) K69, the USA's scale is the generated conversion, below one; (6e) K70, Crime and Security
    /// stay inert on every country; and of 2026-10-24: (2r) K94, a ramping class (Health, Natural) reads its first wave's level on day one - the
    /// check fails if the peak lands on day one - flat to the first wave, then a linear ramp reaching the peak on the hold's last day; (6f) K95, a
    /// minor event below what stands lands nothing and leaves the hold; (6g) K95, once what stands has decayed below its jump, it lands and restarts
    /// the hold.</item>
    /// </list>
    /// Prints `EVENT SALIENCE: PASS` and exits 0 when every check holds; each failure is an `EVENT SALIENCE:` error naming its check.
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.EventSalienceDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class EventSalienceDiagnostic
    {
        /// <summary>THE ORACLE, written out by hand (review 882's H-1): the pool's events that move salience while the fit generates the Economy,
        /// Health, Migration and Natural classes (K69) - each costs output or approval and carries a tag of a generated class (War among the Economy
        /// tags) - with the issue each moves and its K68 severity, worked by hand from the pool's own figures: the event's GDP fall over its class's
        /// anchor's (Economy "War on the Doorstep", Health "Public Health Emergency", Natural the smaller of "Severe Drought Hits Agricultural Output"
        /// and "Wildfires"), Migration's on the approval fall over "Migration Surge"'s, never above one. The four booms with a small inflation shock
        /// (Consumer Confidence Surge, Tourism Boom, Stock Market Rally, Export Boom in a Key Partner), the Bumper Harvest and the Medical Breakthrough
        /// are not on it. A refit that changes which classes are generated, or a pool figure that moves, fails (1a) until this list is re-read.</summary>
        private static readonly Dictionary<string, (IssueId Issue, float Severity)> ExpectedMovers = new Dictionary<string, (IssueId, float)>
        {
            { "Recession in a Trading Partner", (IssueId.Economy, 1f) }, { "Global Commodity Price Spike", (IssueId.Economy, 1f) }, { "Energy Price Shock", (IssueId.Economy, 1f) },
            { "Banking Sector Stress", (IssueId.Economy, 1f) }, { "Major Cyberattack on Financial Infrastructure", (IssueId.Economy, 1f) }, { "Regional Conflict Disrupts Trade Routes", (IssueId.Economy, 1f) },
            { "Sovereign Credit Rating Downgrade", (IssueId.Economy, 0.5f) }, { "Major Labor Strikes", (IssueId.Economy, 1f) }, { "Housing Market Correction", (IssueId.Economy, 1f) },
            { "Currency Under Attack", (IssueId.Economy, 1f) }, { "Sharp Rate Rise Abroad", (IssueId.Economy, 0.6f) }, { "Pension Fund Losses", (IssueId.Economy, 0.4f) },
            { "Global Recession", (IssueId.Economy, 1f) }, { "Sovereign Debt Crisis Contagion", (IssueId.Economy, 1f) }, { "Major Industrial Accident", (IssueId.Economy, 0.3f) },
            { "Infrastructure Collapse", (IssueId.Economy, 0.2f) }, { "Tech Sector Bust", (IssueId.Economy, 1f) }, { "Major Factory Closure", (IssueId.Economy, 0.4f) },
            { "Currency Appreciation Shock", (IssueId.Economy, 0.6f) }, { "Wage Round Overshoots", (IssueId.Economy, 0.2f) }, { "Trade Partner Turns Protectionist", (IssueId.Economy, 0.8f) },
            { "War on the Doorstep", (IssueId.Economy, 1f) },   // review 882b's M-C: tagged War, which moves the Economy slot; K68's Economy anchor
            { "Migration Surge", (IssueId.Immigration, 1f) },   // Migration's anchor; its severity is read on approval (its GDP shock is a rise)
            { "Public Health Emergency", (IssueId.Healthcare, 1f) }, { "Hospital System Crisis", (IssueId.Healthcare, 0.15f) },   // K69: Health generated at its peak
            { "Natural Disaster", (IssueId.Climate, 1f) }, { "Severe Drought Hits Agricultural Output", (IssueId.Climate, 1f) }, { "Wildfires", (IssueId.Climate, 1f) },   // K69: Natural generated at its peak
            { "Severe Flooding", (IssueId.Climate, 1f) }, { "Cold Snap Energy Squeeze", (IssueId.Climate, 0.8f) }, { "Forest Blight", (IssueId.Climate, 0.6f) },
        };

        /// <summary>A pin, not a claim about today (K68's "keep the fitted figure at the fit's own event" and K69's peak reading leave Economy and
        /// Migration where the post-wave reading put them): the two figures as §882 generated them, before K69 - a refit that moves either fails (6c).</summary>
        private const double Economy882Jump = 0.10259259259259264, Migration882Jump = 0.14642857142857149;
        private const int Economy882Hold = 128, Migration882Hold = 68;

        /// <summary>The tolerance a hand-worked severity is held to against the runtime's: the pool's float figures divided once (a float's step).</summary>
        private const float SeverityTolerance = 1e-6f;

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
            BelowLines.Clear();
            _sb = new StringBuilder("EVENT SALIENCE: A-LOOP q5 - events move salience (the default epoch, seed 777)\n");
            foreach (SalienceClass c in (SalienceClass[])Enum.GetValues(typeof(SalienceClass)))
            {
                _sb.Append(F("    class {0,-9} -> {1,-11} jump {2:0.0000}, held {3} days, half-life {4} days{5}\n", c, EventSalience.IssueOf(c), EventSalience.JumpOf(c), EventSalience.HoldDaysOf(c),
                    EventSalience.JumpOf(c) > 0f ? EventSalience.HalfLifeDays(c).ToString("F1", CultureInfo.InvariantCulture) : "-", EventSalience.JumpOf(c) > 0f ? "" : " (inert: the fit could not resolve it)"));
            }
            _sb.Append(F("    the USA's instrument scale {0:0.0000} (K103; the dropped conversion was Gallup {1:0.0000} over Eurobarometer {2:0.0000} mentions a respondent)\n", EventSalience.InstrumentScale(CountryId.USA), EventSalienceFit.GallupBase, EventSalienceFit.EurobarometerBase));
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
                Rulings();
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            finally { Application.logMessageReceived -= Count; EventSystem.Enabled = eventsWere; }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "EVENT SALIENCE: PASS - each listed event moves its issue by its severity times its class's fitted jump and logs it (landed and played, the USA at scale one, K103), the decay keeps the fitted half-life and returns to the seed, nothing leaves [0, 1], the stance model and the campaign read it, the save, the record and the clone carry it, and K68-K70 hold."
                : F("EVENT SALIENCE: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        private static readonly List<string> BelowLines = new List<string>();

        private static void Count(string message, string stack, LogType type)
        {
            if (message == null) { return; }
            if (message.StartsWith("SALIENCE:", StringComparison.Ordinal)) { SalienceLines.Add(message); }
            else if (message.StartsWith("SALIENCE-BELOW:", StringComparison.Ordinal)) { BelowLines.Add(message); }
        }

        /// <summary>The shift the hand-list says a listed event leaves on a country with nothing standing: its severity times its class's jump times the
        /// country's instrument scale, held under 1 − seed.</summary>
        private static float Want(CountryId country, IssueId issue, float severity)
        {
            TryClassOf(issue, out SalienceClass c);
            float seed = StanceModel.SalienceOf(country, issue, out _);
            float level = EventSalience.RampDaysOf(c) > 0 ? EventSalience.FirstLevelOf(c) : EventSalience.JumpOf(c);   // K94: a ramping class lands its first wave's level
            return Mathf.Clamp(severity * level, -seed, 1f - seed);   // K103: every country, the USA included, at scale one
        }

        /// <summary>What stands on each issue as K95 reads it: the shift, plus a running ramp's unfinished rest (its step times the ramp days left).</summary>
        private static float[] StandingOf(Country k)
        {
            var s = new float[IssueVector.IssueCount];
            for (int i = 0; k.SalienceShift != null && i < s.Length && i < k.SalienceShift.Length; i++)
            {
                s[i] = k.SalienceShift[i];
                if (s[i] != 0f && k.SalienceRampPerDay != null && i < k.SalienceRampPerDay.Length && k.SalienceRampPerDay[i] != 0f && k.SalienceHoldDays != null && i < k.SalienceHoldDays.Length
                    && EventSalience.TryClassOfIssue((IssueId)i, out SalienceClass c))
                {
                    s[i] += k.SalienceRampPerDay[i] * Math.Min(k.SalienceHoldDays[i], EventSalience.RampDaysOf(c));
                }
            }
            return s;
        }

        /// <summary>Whether a country's shift holds <paramref name="want"/> on <paramref name="issue"/> (within the severity's tolerance) and nothing elsewhere.</summary>
        private static bool Only(Country k, IssueId issue, float want)
        {
            bool only = k.SalienceShift != null && want > 0f;
            for (int i = 0; only && i < k.SalienceShift.Length; i++) { only &= i == (int)issue ? Math.Abs(k.SalienceShift[i] - want) <= SeverityTolerance : k.SalienceShift[i] == 0f; }
            return only;
        }

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
                    se.SalienceShift = null; se.SalienceHoldDays = null; se.SalienceRampPerDay = null;   // nothing standing: the shift, its holds and its ramp steps (n910b)
                    EventSystem.ApplyEvent(se, e);
                    bool favourable = e.GdpShockPercent >= 0f && e.ApprovalEffect >= 0f;
                    if (favourable) { booms++; }
                    if (!ExpectedMovers.TryGetValue(e.Name, out (IssueId Issue, float Severity) listed))
                    {
                        if (se.SalienceShift != null) { wrong++; Check(false, F("(1a) {0} ({1}{2}) moved salience, and it is not on the hand-list", e.Name, e.Tag, favourable ? ", costing neither output nor approval" : "")); }
                        continue;
                    }
                    moved++;
                    float want = Want(CountryId.Sweden, listed.Issue, listed.Severity);
                    if (!Only(se, listed.Issue, want)) { wrong++; Check(false, F("(1a) {0} ({1}): {2} should move by {3:R} (severity {4:R}) and nothing else; the shift reads [{5}]", e.Name, e.Tag, listed.Issue, want, listed.Severity, se.SalienceShift == null ? "null" : string.Join(", ", se.SalienceShift))); }
                }
                Check(wrong == 0 && moved == ExpectedMovers.Count, F("(1a) {0} events: the {1} on the hand-list ({2} named) moved exactly their issue by their severity times their class's fitted jump, the rest - {3} costing neither output nor approval among them - moved nothing",
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
                int before = SalienceLines.Count, belowBefore = BelowLines.Count, fired = 0, atTop = 0, usa = 0, below = 0;
                var topBefore = new Dictionary<CountryId, bool[]>();
                var standBefore = new Dictionary<CountryId, float[]>();
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
                        standBefore[k.Id] = StandingOf(k);   // K95: what stands as the boundary's events land (a running ramp's peak, review L-2)
                    }
                    sim.AdvanceTurn(decisions);
                    foreach (Country k in world.Countries)
                    {
                        EconomicEvent e = sim.GetLastEvent(k.Id);
                        if (e == null) { continue; }
                        fired++;
                        if (!ExpectedMovers.TryGetValue(e.Name, out (IssueId Issue, float Severity) listed)) { continue; }
                        if (EventSalience.JumpFor(e) * EventSalience.InstrumentScale(k.Id) < standBefore[k.Id][(int)listed.Issue]) { below++; continue; }   // K95: lands nothing, logs SALIENCE-BELOW:
                        if (topBefore[k.Id][(int)listed.Issue]) { atTop++; continue; }
                        if (k.Id == CountryId.USA) { usa++; }
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
                Check(fired > 0 && expected.Count > 0 && missing == 0 && unmatched.Count == 0 && BelowLines.Count - belowBefore == below,
                    F("(1d) {0} events fired on {1} countries; {2} on the hand-list, each with its SALIENCE: line ({3} missing); {4} lines no listed event accounts for{5}; {6} of them on the USA (K69), {7} on an issue already at the top; {8} below what stood (K95), {9} SALIENCE-BELOW: lines",
                        fired, world.Countries.Count, expected.Count, missing, unmatched.Count, unmatched.Count > 0 ? ": " + unmatched[0] : "", usa, atTop, below, BelowLines.Count - belowBefore));
                EventSystem.Enabled = false;
                Country us = world.GetCountry(CountryId.USA);
                if (us == null) { Check(false, "(1e) the world carries no USA"); return; }
                int wrong = 0, landed = 0;
                foreach (KeyValuePair<string, (IssueId Issue, float Severity)> kv in ExpectedMovers)
                {
                    us.SalienceShift = null; us.SalienceHoldDays = null; us.SalienceRampPerDay = null;
                    EventSalience.Land(us, PoolOf().First(p => p.Name == kv.Key));
                    landed++;
                    float want = Want(CountryId.USA, kv.Value.Issue, kv.Value.Severity);
                    if (!Only(us, kv.Value.Issue, want)) { wrong++; Check(false, F("(1e) {0} on the USA: {1} should move by {2:R} and nothing else; the shift reads [{3}]", kv.Key, kv.Value.Issue, want, us.SalienceShift == null ? "null" : string.Join(", ", us.SalienceShift))); }
                }
                us.SalienceShift = null; us.SalienceHoldDays = null; us.SalienceRampPerDay = null;
                Check(wrong == 0 && landed == ExpectedMovers.Count, F("(1e) K103: every listed event landed on the USA ({0}) moves its issue by its severity times its class's jump at scale one (the runtime's scale {1:0.0000}), and nothing else", landed, EventSalience.InstrumentScale(CountryId.USA)));
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
                foreach (SalienceClass c in (SalienceClass[])Enum.GetValues(typeof(SalienceClass))) { if (EventSalience.JumpOf(c) > 0f) { classes.Add(c); EventSalience.Land(se, Anchor(c)); } }
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
                    int ramp = EventSalience.RampDaysOf(c), flat = EventSalience.FirstHoldDaysOf(c);
                    float peak = EventSalience.JumpOf(c);
                    if (ramp == 0)
                    {
                        bool held = s[0] == peak && flat == hold;
                        for (int d = 1; d <= hold; d++) { held &= s[d] == s[0]; }
                        Check(held && hold > 0, F("(2h) {0}: the reading holds exactly the survey's jump {1:R} through its hold of {2} days (day {2} holds {3:R})", c, peak, hold, s[hold]));
                    }
                    else
                    {
                        // K94: the first wave's level on day one, flat to the first wave, a linear ramp to the peak on the hold's last day - never the peak on day one
                        float first = EventSalience.FirstLevelOf(c), step = (peak - first) / ramp;
                        bool flatOk = s[0] == first && first < peak && flat + ramp == hold;
                        for (int d = 1; d <= flat; d++) { flatOk &= s[d] == first; }
                        bool linear = true;
                        for (int d = flat + 1; d <= hold; d++) { linear &= Math.Abs(s[d] - (first + step * (d - flat))) <= 1e-5f && s[d] > s[d - 1]; }
                        Check(flatOk && linear && Math.Abs(s[hold] - peak) <= 1e-5f && s[0] < peak,
                            F("(2r) {0}: K94 - day one reads the first wave's {1:R}, not the peak's {2:R}; flat {3} days, then {4} days of a linear ramp ({5:R} a day) reaching {6:R} on day {7}", c, s[0], peak, flat, ramp, step, s[hold], hold));
                    }
                    Check(Math.Abs(s[hold + 1] - s[hold] * p) <= 1e-6f * Math.Abs(s[hold]), F("(2a) {0}: the first day after the hold keeps the daily persistence to a float's step (the in-place `*=` rounds once, n882: 1 ulp) {1:R} ({2:R} -> {3:R})", c, p, s[hold], s[hold + 1]));
                    Check(s[hold + h] >= s[hold] / 2f && s[hold + h + 1] < s[hold] / 2f, F("(2b) {0}: half the peak on the fitted half-life after the hold ({1:F1} days) - day {2} holds {3:F5}, day {4} {5:F5}, half of {6:F5}", c, EventSalience.HalfLifeDays(c), hold + h, s[hold + h], hold + h + 1, s[hold + h + 1], s[hold]));
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
                        for (int i = 0; i < 40; i++) { EventSalience.Land(k, Anchor(c)); landed++; }
                        if (EventSalience.Of(k, EventSalience.IssueOf(c)) >= 1f - 1e-6f) { saturated++; }
                    }
                    for (int d = 0; d < 3650 && k.SalienceShift != null; d++)
                    {
                        foreach (IssueId issue in (IssueId[])Enum.GetValues(typeof(IssueId))) { float v = EventSalience.Of(k, issue); if (float.IsNaN(v) || v < 0f || v > 1f) { outside++; } }
                        EventSalience.AdvanceDay(k);
                    }
                }
                Check(outside == 0, F("(3) {0} events landed on {1} countries, then ten years of decay: {2} readings outside [0, 1]; {3} saturated issues read 1 (review L-4: none can - K95 lands an event only on what stands below its own jump, so a stack stays under two jumps, and no seed plus two of its class's jumps reaches 1; the top clamp is unreachable on today's fit and seeds)", landed, world.Countries.Count, outside, saturated));
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
                    k.SalienceShift = null; k.SalienceHoldDays = null; k.SalienceRampPerDay = null;
                    List<PartyStance> before = StanceModel.Stances(k, Cut());
                    EventSalience.Land(k, Anchor(SalienceClass.Economy));
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
                Check(tested >= 1 && wrongWay == 0, F("(4a) a 50 % social-security cut, Economy raised by an Economy event, on {0} countries' chambers (the USA's at scale one, K103): {1}", tested, string.Join("; ", lines)));
            });
            WithWorld((sim, world) =>
            {
                Country se = world.GetCountry(CountryId.Sweden);
                se.SalienceShift = null; se.SalienceHoldDays = null; se.SalienceRampPerDay = null;
                foreach (SalienceClass c in (SalienceClass[])Enum.GetValues(typeof(SalienceClass))) { if (EventSalience.JumpOf(c) > 0f) { EventSalience.Land(se, Anchor(c)); } }
                float[] shift = EventSalience.Snapshot(se);
                if (!Stage(CountryId.Sweden, out CampaignRun.Setup seSeed) || !Stage(CountryId.Sweden, out CampaignRun.Setup seMoved)) { Check(false, "(4b) Sweden's campaign could not be staged"); return; }
                EventSalience.ApplyToCampaign(shift, seMoved.TrueSalience);
                int climate = (int)IssueId.Climate;
                Check(MovedExactly(seSeed.TrueSalience, seMoved.TrueSalience, shift) && seMoved.TrueSalience[climate] > seSeed.TrueSalience[climate],
                    F("(4b) Sweden's campaign reads its wave moved by exactly the shift where the wave measures the issue (NaN elsewhere): Climate {0:F4} -> {1:F4} by the Natural class (K69) - [{2}]",
                        seSeed.TrueSalience[climate], seMoved.TrueSalience[climate], string.Join(", ", seMoved.TrueSalience.Select(v => v.ToString("0.00", CultureInfo.InvariantCulture)))));

                if (!Stage(CountryId.Germany, out CampaignRun.Setup deSeed) || !Stage(CountryId.Germany, out CampaignRun.Setup deMoved)) { Check(false, "(4c) Germany's campaign could not be staged"); return; }
                EventSalience.ApplyToCampaign(shift, deMoved.TrueSalience);
                int econ = (int)IssueId.Economy, imm = (int)IssueId.Immigration;
                Check(MovedExactly(deSeed.TrueSalience, deMoved.TrueSalience, shift) && deMoved.TrueSalience[econ] > deSeed.TrueSalience[econ] && deMoved.TrueSalience[imm] > deSeed.TrueSalience[imm],
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

        /// <summary>Whether a campaign's moved wave is its seed wave plus the shift on every issue it measures, clamped to [0, 1], and NaN where the seed is.</summary>
        private static bool MovedExactly(double[] seed, double[] moved, float[] shift)
        {
            bool exact = seed.Length == moved.Length;
            for (int i = 0; exact && i < seed.Length; i++)
            {
                double want = double.IsNaN(seed[i]) ? double.NaN : Math.Min(1.0, Math.Max(0.0, seed[i] + (i < shift.Length ? shift[i] : 0f)));
                exact &= double.IsNaN(want) ? double.IsNaN(moved[i]) : Math.Abs(moved[i] - want) < 1e-12;
            }
            return exact;
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
                foreach (SalienceClass c in (SalienceClass[])Enum.GetValues(typeof(SalienceClass))) { if (EventSalience.JumpOf(c) > 0f) { EventSalience.Land(se, Anchor(c)); } }
                Days(sim, 160, null);   // review L-4: the cut inside both ramps (Health's flat phase is 145 days, Natural's 123), so the load resumes a ramp
                string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null));
                SaveGame loaded = SaveGameService.Deserialize(json);
                var goB = new GameObject("SALIENCE.loaded"); hosts.Add(goB);
                SimulationManager simB = goB.AddComponent<SimulationManager>();
                SaveGameService.RestoreInto(simB, loaded);
                Country back = simB.World.GetCountry(CountryId.Sweden);
                Check(Same(se.SalienceShift, back.SalienceShift) && se.SalienceShift != null && se.SalienceHoldDays != null && back.SalienceHoldDays != null && se.SalienceHoldDays.SequenceEqual(back.SalienceHoldDays)
                      && se.SalienceRampPerDay != null && Same(se.SalienceRampPerDay, back.SalienceRampPerDay),
                    F("(5a) loaded into a manager, the shift, its holds and its ramp steps (K94) come back exactly ([{0}]; holds [{1}]; ramp [{2}])", se.SalienceShift == null ? "null" : string.Join(", ", se.SalienceShift),
                        se.SalienceHoldDays == null ? "null" : string.Join(", ", se.SalienceHoldDays), se.SalienceRampPerDay == null ? "null" : string.Join(", ", se.SalienceRampPerDay)));
                int hc = (int)IssueId.Healthcare, cl = (int)IssueId.Climate;
                float hcAtCut = back.SalienceShift[hc], clAtCut = back.SalienceShift[cl];
                bool inRamp = back.SalienceHoldDays[hc] <= EventSalience.RampDaysOf(SalienceClass.Health) && back.SalienceHoldDays[cl] <= EventSalience.RampDaysOf(SalienceClass.Natural);
                Days(sim, 30, null);
                Days(simB, 30, null);
                Check(Same(se.SalienceShift, back.SalienceShift) && inRamp && back.SalienceShift[hc] > hcAtCut && back.SalienceShift[cl] > clAtCut,
                    F("(5b) the save cut inside both ramps (L-4): thirty days on, the continuous game and the loaded one hold the same shift, and the loaded ramps resumed - Healthcare {0:F4} -> {1:F4}, Climate {2:F4} -> {3:F4}",
                        hcAtCut, back.SalienceShift[hc], clAtCut, back.SalienceShift[cl]));

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
                      && clone.SalienceHoldDays != null && clone.SalienceHoldDays.SequenceEqual(se.SalienceHoldDays) && !ReferenceEquals(se.SalienceHoldDays, clone.SalienceHoldDays)
                      && Same(se.SalienceRampPerDay, clone.SalienceRampPerDay) && (se.SalienceRampPerDay == null || !ReferenceEquals(se.SalienceRampPerDay, clone.SalienceRampPerDay)), "(5e) the preview's clone carries the shift, its holds and its ramp steps as its own copies");
            }
            finally { foreach (GameObject go in hosts) { Object.DestroyImmediate(go); } }
        }

        // ============================================================ (6) Elias's rulings K68-K70 (2026-10-23)
        private static void Rulings()
        {
            _sb.Append("    6. K68-K70: severity, the peak, the USA, the inert classes\n");
            List<EconomicEvent> pool = PoolOf();
            WithWorld((sim, world) =>
            {
                Country se = world.GetCountry(CountryId.Sweden);

                // (6a) K68: each class's anchors are the pool's own events of its tag and land exactly the fitted jump; the 2022 shock the Economy fit's
                int anchorsWrong = 0;
                var anchorLines = new List<string>();
                foreach (SalienceClass c in (SalienceClass[])Enum.GetValues(typeof(SalienceClass)))
                {
                    foreach (string name in EventSalience.AnchorsOf(c))
                    {
                        EconomicEvent a = EventSystem.PoolEvent(name);
                        bool ok = a != null && pool.Contains(a) && EventSalience.TryClassOf(a.Tag, out SalienceClass ac) && ac == c && EventSalience.Moves(a) && EventSalience.SeverityOf(a) == 1f;
                        if (ok && EventSalience.JumpOf(c) > 0f)
                        {
                            se.SalienceShift = null; se.SalienceHoldDays = null; se.SalienceRampPerDay = null;
                            EventSalience.Land(se, a);
                            ok = Only(se, EventSalience.IssueOf(c), Want(CountryId.Sweden, EventSalience.IssueOf(c), 1f)) && se.SalienceHoldDays[(int)EventSalience.IssueOf(c)] == EventSalience.HoldDaysOf(c);
                        }
                        if (!ok) { anchorsWrong++; }
                        anchorLines.Add(F("{0} '{1}'{2}", c, name, ok ? "" : " WRONG"));
                    }
                }
                se.SalienceShift = null; se.SalienceHoldDays = null; se.SalienceRampPerDay = null;
                EventSalience.Land(se, EventSystem.PoolEvent("War on the Doorstep"));
                bool war = se.SalienceShift != null && se.SalienceShift[(int)IssueId.Economy] == (float)EventSalienceFit.EconomySurveyJump && se.SalienceHoldDays[(int)IssueId.Economy] == EventSalienceFit.EconomyHoldDays;
                Check(anchorsWrong == 0 && war, F("(6a) K68: every class's anchor is the pool's own event of the class at severity 1, landing exactly the class's jump and hold ({0}); the 2022 shock lands the Economy fit's {1:+0.0000} held {2} days",
                    string.Join(", ", anchorLines), EventSalienceFit.EconomySurveyJump, EventSalienceFit.EconomyHoldDays));

                // (6b) K68: proportionally less - every listed severity is the hand-list's, and a minor event lands strictly less than its class's jump
                int severityWrong = 0, smaller = 0, smallerWrong = 0;
                foreach (KeyValuePair<string, (IssueId Issue, float Severity)> kv in ExpectedMovers)
                {
                    EconomicEvent e = pool.FirstOrDefault(p => p.Name == kv.Key);
                    float s = e == null ? -1f : EventSalience.SeverityOf(e);
                    if (e == null || Math.Abs(s - kv.Value.Severity) > SeverityTolerance) { severityWrong++; Check(false, F("(6b) {0}: severity {1:R}, the hand-list's {2:R}", kv.Key, s, kv.Value.Severity)); continue; }
                    if (kv.Value.Severity < 1f)
                    {
                        smaller++;
                        TryClassOf(kv.Value.Issue, out SalienceClass c);
                        if (!(EventSalience.JumpFor(e) < EventSalience.JumpOf(c)) || !(EventSalience.JumpFor(e) > 0f)) { smallerWrong++; }
                    }
                }
                EconomicEvent minor = EventSystem.PoolEvent("Infrastructure Collapse");
                Check(severityWrong == 0 && smaller > 0 && smallerWrong == 0 && minor != null && minor.Band == EventBand.Minor,
                    F("(6b) K68: {0} listed severities are the hand-list's; {1} events smaller than their anchor land proportionally less, never zero - the MINOR 'Infrastructure Collapse' (GDP {2:0.0} %) lands {3:+0.0000} of the Economy jump {4:+0.0000}",
                        ExpectedMovers.Count, smaller, minor?.GdpShockPercent ?? float.NaN, minor == null ? float.NaN : EventSalience.JumpFor(minor), EventSalience.JumpOf(SalienceClass.Economy)));

                // (6c) K69: the jump at the excess's peak - Health and Natural move at their peak waves; Economy and Migration keep §882's figures
                bool health = EventSalienceFit.HealthGenerated && EventSalienceFit.HealthPeakStep > 0 && EventSalienceFit.HealthSurveyJump > EventSalienceFit.HealthFirstWaveJump
                              && EventSalience.JumpOf(SalienceClass.Health) == (float)EventSalienceFit.HealthSurveyJump && EventSalience.HoldDaysOf(SalienceClass.Health) > 0;
                bool natural = EventSalienceFit.NaturalGenerated && EventSalienceFit.NaturalPeakStep > 0 && EventSalienceFit.NaturalSurveyJump > EventSalienceFit.NaturalFirstWaveJump
                               && EventSalience.JumpOf(SalienceClass.Natural) == (float)EventSalienceFit.NaturalSurveyJump && EventSalience.HoldDaysOf(SalienceClass.Natural) > 0;
                bool kept = EventSalienceFit.EconomyPeakStep == 0 && EventSalienceFit.MigrationPeakStep == 0
                            && EventSalienceFit.EconomySurveyJump == Economy882Jump && EventSalienceFit.MigrationSurveyJump == Migration882Jump
                            && EventSalienceFit.EconomyHoldDays == Economy882Hold && EventSalienceFit.MigrationHoldDays == Migration882Hold;
                Check(health && natural && kept,
                    F("(6c) K69: Health moves at its peak (step {0}, {1:+0.0000} against the first wave's {2:+0.0000}, held {3} days, half-life {4:F1}); Natural at its peak (step {5}, {6:+0.0000} against {7:+0.0000}, held {8} days, half-life {9:F1}); Economy and Migration peak at the post-wave and keep §882's {10:+0.0000} / {11:+0.0000}",
                        EventSalienceFit.HealthPeakStep, EventSalienceFit.HealthSurveyJump, EventSalienceFit.HealthFirstWaveJump, EventSalience.HoldDaysOf(SalienceClass.Health), EventSalience.HalfLifeDays(SalienceClass.Health),
                        EventSalienceFit.NaturalPeakStep, EventSalienceFit.NaturalSurveyJump, EventSalienceFit.NaturalFirstWaveJump, EventSalience.HoldDaysOf(SalienceClass.Natural), EventSalience.HalfLifeDays(SalienceClass.Natural),
                        EventSalienceFit.EconomySurveyJump, EventSalienceFit.MigrationSurveyJump));

                // (6d) K103: every country's scale is one, the USA's included - the K69 conversion, which failed its K102 test, is applied nowhere
                float us = EventSalience.InstrumentScale(CountryId.USA);
                bool others = world.Countries.All(k => EventSalience.InstrumentScale(k.Id) == 1f);
                Check(us == 1f && others && !EventSalienceFit.K102Kept && (float)EventSalienceFit.UsConversion != 1f,
                    F("(6d) K103: the USA's instrument scale {0:0.0000}, every country's one ({1}); the dropped conversion {2:0.0000} (Gallup {3:0.0000} / Eurobarometer {4:0.0000}) stays generated, its K102 verdict FAILED ({5})",
                        us, others, EventSalienceFit.UsConversion, EventSalienceFit.GallupBase, EventSalienceFit.EurobarometerBase, EventSalienceFit.K102Kept ? "kept" : "failed"));

                // (6e) K70: Crime and Security stay inert - no jump, and every event of their tags moves nothing on any country
                int inertMoved = 0, inertEvents = 0;
                foreach (EconomicEvent e in pool)
                {
                    if (e.Tag != EventTag.Crime && e.Tag != EventTag.Security) { continue; }
                    inertEvents++;
                    foreach (Country k in world.Countries) { k.SalienceShift = null; k.SalienceHoldDays = null; k.SalienceRampPerDay = null; EventSalience.Land(k, e); if (k.SalienceShift != null) { inertMoved++; } }
                }
                Check(EventSalience.JumpOf(SalienceClass.Crime) == 0f && EventSalience.JumpOf(SalienceClass.Security) == 0f && !EventSalienceFit.CrimeGenerated && !EventSalienceFit.SecurityGenerated && inertEvents >= 2 && inertMoved == 0,
                    F("(6e) K70: Crime and Security are inert (jumps 0); their {0} pool events landed on {1} countries moved nothing ({2} moved)", inertEvents, world.Countries.Count, inertMoved));

                // (6f) K95, the branch below: the 2022 shock stands; the next day a MINOR Economy event (its jump a fifth of it) lands nothing and leaves the hold
                int econ = (int)IssueId.Economy;
                EconomicEvent big = EventSystem.PoolEvent("War on the Doorstep"), small = EventSystem.PoolEvent("Infrastructure Collapse");
                se.SalienceShift = null; se.SalienceHoldDays = null; se.SalienceRampPerDay = null;
                EventSalience.Land(se, big);
                EventSalience.AdvanceDay(se);
                float standing = se.SalienceShift[econ]; int holdLeft = se.SalienceHoldDays[econ];
                int belowLogs = BelowLines.Count;
                EventSalience.Land(se, small);
                Check(EventSalience.JumpFor(small) < standing && se.SalienceShift[econ] == standing && se.SalienceHoldDays[econ] == holdLeft && holdLeft == EventSalience.HoldDaysOf(SalienceClass.Economy) - 1 && BelowLines.Count == belowLogs + 1,
                    F("(6f) K95 below: a minor event's jump {0:0.0000} under the {1:0.0000} standing lands nothing - the shift stays {2:0.0000}, the hold stays {3} days (one SALIENCE-BELOW: line)", EventSalience.JumpFor(small), standing, se.SalienceShift[econ], se.SalienceHoldDays[econ]));

                // (6g) K95, the branch at or above: once the standing shift has decayed below the minor event's jump, the same event lands on it and restarts the hold
                int days = 0;
                while (se.SalienceShift != null && se.SalienceShift[econ] > EventSalience.JumpFor(small) && days < 5000) { EventSalience.AdvanceDay(se); days++; }
                float left = se.SalienceShift == null ? 0f : se.SalienceShift[econ];
                EventSalience.Land(se, small);
                float seedE = StanceModel.SalienceOf(CountryId.Sweden, IssueId.Economy, out _);
                Check(se.SalienceShift != null && left > 0f && Math.Abs(se.SalienceShift[econ] - Mathf.Clamp(left + EventSalience.JumpFor(small), -seedE, 1f - seedE)) <= SeverityTolerance && se.SalienceHoldDays[econ] == EventSalience.HoldDaysOf(SalienceClass.Economy),
                    F("(6g) K95 at or above: {0} days on, {1:0.0000} left under the minor event's jump {2:0.0000}; it lands on it ({3:0.0000}) and restarts the hold ({4} days)", days, left, EventSalience.JumpFor(small),
                        se.SalienceShift == null ? 0f : se.SalienceShift[econ], se.SalienceHoldDays == null ? 0 : se.SalienceHoldDays[econ]));
                se.SalienceShift = null; se.SalienceHoldDays = null; se.SalienceRampPerDay = null;

                // (6h) review L-2: during a ramp's FLAT phase what stands is the hold's peak, not the first wave's level - a "Natural Disaster" stands at
                // its first wave's level, ramping to its peak; the next day a "Cold Snap Energy Squeeze" (severity below one) whose jump clears that level
                // but not the peak lands nothing: the shift, the hold and the bigger event's ramp all stay
                int clim = (int)IssueId.Climate;
                EconomicEvent disaster = EventSystem.PoolEvent("Natural Disaster"), snap = EventSystem.PoolEvent("Cold Snap Energy Squeeze");
                EventSalience.Land(se, disaster);
                EventSalience.AdvanceDay(se);
                float level = se.SalienceShift[clim], rampStep = se.SalienceRampPerDay[clim]; int holdNow = se.SalienceHoldDays[clim];
                float peakStanding = level + rampStep * Math.Min(holdNow, EventSalience.RampDaysOf(SalienceClass.Natural));
                int belowBefore = BelowLines.Count;
                EventSalience.Land(se, snap);
                float snapJump = EventSalience.JumpFor(snap);
                Check(snapJump >= level && snapJump < peakStanding && se.SalienceShift[clim] == level && se.SalienceHoldDays[clim] == holdNow && se.SalienceRampPerDay[clim] == rampStep && BelowLines.Count == belowBefore + 1,
                    F("(6h) L-2: in the ramp's flat phase ({0:0.0000} standing, its peak {1:0.0000}), a smaller event's jump {2:0.0000} - above the level, below the peak - lands nothing: the shift {3:0.0000}, the hold {4} days and the ramp step {5:R} stay",
                        level, peakStanding, snapJump, se.SalienceShift[clim], se.SalienceHoldDays[clim], se.SalienceRampPerDay[clim]));
                se.SalienceShift = null; se.SalienceHoldDays = null; se.SalienceRampPerDay = null;
            });
        }

        // ============================================================ helpers
        private static List<EconomicEvent> PoolOf()
        {
            FieldInfo poolField = typeof(EventSystem).GetField("EventPool", BindingFlags.Static | BindingFlags.NonPublic);   // the pool is private; EventPoolCheck reads it the same way
            return poolField?.GetValue(null) as List<EconomicEvent> ?? new List<EconomicEvent>();
        }

        /// <summary>The class whose issue is <paramref name="issue"/> (each issue is one class's).</summary>
        private static bool TryClassOf(IssueId issue, out SalienceClass c) => EventSalience.TryClassOfIssue(issue, out c);

        /// <summary>The class's first anchor (K68: severity 1, the full fitted jump) - the event a test lands for it.</summary>
        private static EconomicEvent Anchor(SalienceClass c)
        {
            EconomicEvent a = EventSystem.PoolEvent(EventSalience.AnchorsOf(c)[0]);
            if (a == null) { throw new InvalidOperationException("the pool carries no anchor of the class " + c); }
            return a;
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

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "      ok   " : "      FAIL ").Append(what).Append('\n');
            if (!ok) { _failures++; Debug.LogError("EVENT SALIENCE: FAIL - " + what); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
