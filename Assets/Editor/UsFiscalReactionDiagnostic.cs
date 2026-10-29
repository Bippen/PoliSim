using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Data.Generated;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// K274 (Elias, 2026-10-29; answering K269 (c)): THE AI USA'S PRIMARY-BALANCE REACTION - R-US21 (v) re-scoped: the AI USA's budget responds to debt
    /// through an empirical (Bohn-type) reaction on top of the debt limit, without a default cycle. It fails while any part of that statement is false:
    /// <list type="bullet">
    /// <item>(a) THE COEFFICIENT IS THE SOURCE'S: the stored tables (`ElectionsData/usa/raw/k274_fiscal_reaction/bohn_SUS98_TAB.pdf`) carry the digest the
    /// folder's SHA256SUMS.txt lists; their first page, LZW-decoded here, is Table I, and the coefficient on the debt in column (1), 1916-95, is
    /// `AiFinanceMinistry.UsDebtReactionCoefficient` to the printed decimal.</item>
    /// <item>(b) THE AMOUNT IS THE SLOPE TIMES THE RATIO'S CHANGE: on an AI-governed USA, a planted rise of the ratio since the last boundary is answered by
    /// cuts (no restore, no rate cut) worth the slope times the rise, per cent of GDP; a planted fall by restores (no cut, no rate rise) worth the same; a
    /// ratio that held, or an empty memory (the AI's first turn in office), by nothing; never on the pension line (review 1's M2, `Reviews/2026-10-29_k274_prereg2.md`
    /// (ii): 2 U.S.C. 641(g)).</item>
    /// <item>(c) ONLY THE AI USA'S BOOK: with the USA the player, six turns, the ministry never observed it (its memory empty, so neither the boundary nor the
    /// government's bill wrote the reaction) and the USA's rates stand where the player left them.</item>
    /// <item>(e) THE BILL PATH (review 1's M1): the player's USA under an AI head (a party seated out of the White House) - the government's budget bill, tabled
    /// between boundaries (`SimulationManager.TableGovernmentBudget`), answers the boundary year the memory closed last: with the ratio unchanged since the last
    /// boundary and a rise planted between the last two boundaries, the bill tightens by the slope times the rise while the boundary's reading of the same
    /// memory asks nothing; with a fall planted, it loosens by the same; with the year before empty (the AI's first year), it moves nothing.</item>
    /// <item>(d) NO DEFAULT IN A CENTURY: the all-AI world of `Tools/headless_hold/frf_play.sh` (no player, every country `PolicyDecision.None`, a turn a year,
    /// Sweden's start) at seeds 777 and 424242 - the USA enters no debt crisis in 100 years and the debt limit was raised by act (its mechanics stand).
    /// Printed beside it, unjudged: the debt ratio every ten years and the prereg's (B) reading, year 30 against CBO's figure for the matched calendar year
    /// (`Reviews/2026-10-29_k274_prereg.md`; the level crosses a perimeter - the model's gross federal stock against CBO's debt held by the public), and beside
    /// that the same year against CBO's figure for the year before (review 1's M3: the task's words name 2055, the registered calendar rule reads 2056).</item>
    /// </list>
    /// Run: <c>-executeMethod PoliSim.EditorTools.UsFiscalReactionDiagnostic.Run</c>; a passing run prints <c>US FISCAL REACTION: PASS</c>.
    /// </summary>
    public static class UsFiscalReactionDiagnostic
    {
        private const string Folder = "ElectionsData/usa/raw/k274_fiscal_reaction/";
        private const string Tables = "bohn_SUS98_TAB.pdf";
        /// <summary>The century's length and seeds - frf_play's measurement in the prereg.</summary>
        private const int Years = 100;
        private static readonly int[] Seeds = { 777, 424242 };
        /// <summary>The prereg's (B) horizon, years.</summary>
        private const int HorizonYears = 30;
        /// <summary>(c)'s turns - the AI finance ministry diagnostic's own (7).</summary>
        private const int PlayerTurns = 6;
        /// <summary>(e): the manager's pending budget slot, as `UsBudgetProcedureDiagnostic` stages it.</summary>
        private static readonly FieldInfo SlotField = typeof(SimulationManager).GetField("_pendingBudgetBillByCountry", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static string F(string format, params object[] args) => string.Format(Inv, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsFiscalReactionDiagnostic (K274): the AI USA's primary-balance reaction on top of the debt limit ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; Debug.LogError("US FISCAL REACTION: FAIL - " + what); } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            DateTime epochWas = SimulationManager.EpochDate;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // (a) the coefficient is the source's
                string[] sums = File.ReadAllText(PathOf(Folder + "SHA256SUMS.txt"), Encoding.ASCII).Replace("\r\n", "\n").Split('\n');
                byte[] pdf = File.ReadAllBytes(PathOf(Folder + Tables));
                string digest = Sha256(pdf);
                Check(sums.Contains(digest + " *" + Tables), F("(a) {0} {1}: listed in the folder's SHA256SUMS.txt", Tables, digest));
                string read = TableOneColumnOneDebt(pdf, out string why);
                bool parsed = float.TryParse(read, NumberStyles.Float, Inv, out float sourced);
                Check(parsed && sourced == AiFinanceMinistry.UsDebtReactionCoefficient,
                    F("(a) Bohn (1998) Table I, column (1), 1916-95, the coefficient on d_t reads {0} on the stored page{1}; the ministry's coefficient is {2}",
                        read ?? "nothing", why != null ? " (" + why + ")" : "", AiFinanceMinistry.UsDebtReactionCoefficient.ToString("0.000", Inv)));

                // (b) the amount, on an AI-governed USA (a Sweden-start world, six turns so the memory is the game's own)
                SimulationRandom.Seed(777);
                using (SimulationManager.EpochScope())
                {
                    PoliSim.Elections.WorldClock.ApplyStart(CountryId.Sweden);
                    World world = WorldFactory.CreateDefault();
                    var go = new GameObject("K274.amount"); hosts.Add(go);
                    SimulationManager sim = go.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    Advance(sim, world, PlayerTurns);
                    Country usa = world.GetCountry(CountryId.USA);
                    Check(usa.DebtRatioLastReport > 0f, F("(b) the AI ministry observed the AI-governed USA (its last ratio {0:F2} %)", usa.DebtRatioLastReport));
                    float lastWas = usa.DebtRatioLastReport, debt = usa.State.DebtToGdpRatio, ngdp = usa.State.NominalGdp;
                    const float Planted = 2f;   // points of GDP the ratio is planted to have moved since the last boundary
                    float want = AiFinanceMinistry.UsDebtReactionCoefficient * Planted;
                    try
                    {
                        usa.DebtRatioLastReport = debt - Planted;
                        PolicyDecision roseDecision = Boundary(usa);
                        float rose = Worth(usa, roseDecision.SpendingLineChanges, roseDecision.TaxRateOverrides, out int roseCuts, out int roseRestores, out int roseRaises, out int roseLowers);
                        bool rosePension = roseDecision.SpendingLineChanges.ContainsKey(SpendingCategory.SocialSecurity);
                        Check(roseCuts > 0 && roseRestores == 0 && roseLowers == 0 && !rosePension && Mathf.Abs(-rose / ngdp * 100f - want) <= 0.002f * want,
                            F("(b) the ratio {0} points higher than at the last boundary: {1:F4} % of GDP tightened ({2} cuts, {3} rate rises, {4} restores, {5} rate cuts; the pension line {6}) against the slope's {7:F4}", Planted, -rose / ngdp * 100f, roseCuts, roseRaises, roseRestores, roseLowers, rosePension ? "CUT" : "untouched", want));
                        usa.DebtRatioLastReport = debt + Planted;
                        PolicyDecision fellDecision = Boundary(usa);
                        float fell = Worth(usa, fellDecision.SpendingLineChanges, fellDecision.TaxRateOverrides, out int fellCuts, out int fellRestores, out int fellRaises, out int fellLowers);
                        bool fellPension = fellDecision.SpendingLineChanges.ContainsKey(SpendingCategory.SocialSecurity);
                        Check(fellRestores + fellLowers > 0 && fellCuts == 0 && fellRaises == 0 && !fellPension && Mathf.Abs(fell / ngdp * 100f - want) <= 0.002f * want,
                            F("(b) the ratio {0} points lower: {1:F4} % of GDP loosened ({2} restores, {3} rate cuts, {4} cuts, {5} rate rises; the pension line {6}) against the slope's {7:F4}", Planted, fell / ngdp * 100f, fellRestores, fellLowers, fellCuts, fellRaises, fellPension ? "RESTORED" : "untouched", want));
                        usa.DebtRatioLastReport = debt;
                        Moved(usa, out int heldCuts, out int heldRestores, out int heldRaises, out int heldLowers);
                        usa.DebtRatioLastReport = 0f;
                        Moved(usa, out int emptyCuts, out int emptyRestores, out int emptyRaises, out int emptyLowers);
                        Check(heldCuts + heldRestores + heldRaises + heldLowers == 0 && emptyCuts + emptyRestores + emptyRaises + emptyLowers == 0,
                            F("(b) a ratio that held writes nothing ({0} writes), nor an empty memory - the AI's first turn in office ({1} writes)", heldCuts + heldRestores + heldRaises + heldLowers, emptyCuts + emptyRestores + emptyRaises + emptyLowers));
                    }
                    finally { usa.DebtRatioLastReport = lastWas; }

                    // (e) the bill path (review 1's M1): the player's USA under an AI head, its government's bill tabled between boundaries
                    string outOfOffice = usa.Government != null && usa.Government.PmParty == "DEM" ? "REP" : "DEM";   // the US key not holding the White House this year (the check below says so)
                    sim.PlayerCountryId = CountryId.USA;
                    usa.PlayerPartyAbbrev = outOfOffice;
                    usa.PlayerHoldsPresidency = false;
                    Check(!sim.PlayerGoverns(usa), F("(e) the USA the player's, {0} seated out of the White House: an AI head governs it (the player governs: {1})", outOfOffice, sim.PlayerGoverns(usa)));
                    var slot = SlotField != null ? (Dictionary<CountryId, BudgetBill>)SlotField.GetValue(sim) : null;
                    Check(slot != null, "(e) the manager's pending budget slot resolved (the tablings' staging)");
                    float beforeWas = usa.DebtRatioReportBefore;
                    try
                    {
                        if (slot != null)
                        {
                            usa.DebtRatioLastReport = debt;   // nothing since the last boundary: the boundary's reading asks nothing
                            usa.DebtRatioReportBefore = debt - Planted;
                            int boundaryWrites = Writes(Boundary(usa));
                            BudgetBill rose = Tabled(sim, slot, usa);
                            float roseWorth = rose != null ? Worth(usa, rose.SpendingPercentChanges, rose.TaxLines, out int bc, out int brs, out int br, out int bl) : 0f;
                            int roseCuts = 0, roseLoosens = 0;
                            if (rose != null) { roseCuts = Count(rose.SpendingPercentChanges, true); roseLoosens = Count(rose.SpendingPercentChanges, false); }
                            Check(rose != null && rose.GovernmentBill && boundaryWrites == 0 && roseCuts > 0 && roseLoosens == 0 && !rose.SpendingPercentChanges.ContainsKey(SpendingCategory.SocialSecurity)
                                  && Mathf.Abs(-roseWorth / ngdp * 100f - want) <= 0.002f * want,
                                F("(e) the ratio {0} points higher at the last boundary than at the one before, unchanged since: the government's bill tightens {1:F4} % of GDP ({2} cuts, {3} restores) against the slope's {4:F4}; the boundary's reading of the same memory writes {5}",
                                    Planted, -roseWorth / ngdp * 100f, roseCuts, roseLoosens, want, boundaryWrites));
                            usa.DebtRatioReportBefore = debt + Planted;
                            BudgetBill fell = Tabled(sim, slot, usa);
                            float fellWorth = fell != null ? Worth(usa, fell.SpendingPercentChanges, fell.TaxLines, out int fc, out int frs, out int fr, out int fl) : 0f;
                            int fellCuts = fell != null ? Count(fell.SpendingPercentChanges, true) : 0;
                            Check(fell != null && fellCuts == 0 && Mathf.Abs(fellWorth / ngdp * 100f - want) <= 0.002f * want,
                                F("(e) the ratio {0} points lower at the last boundary than at the one before: the bill loosens {1:F4} % of GDP ({2} cuts) against the slope's {3:F4}", Planted, fellWorth / ngdp * 100f, fellCuts, want));
                            usa.DebtRatioReportBefore = 0f;
                            BudgetBill first = Tabled(sim, slot, usa);
                            Check(first != null && first.SpendingPercentChanges.Count == 0 && first.TaxLines.Count == 0,
                                F("(e) the year before empty (the AI's first year in office): the bill moves {0} line(s) and {1} rate(s)", first?.SpendingPercentChanges.Count ?? -1, first?.TaxLines.Count ?? -1));
                        }
                    }
                    finally { usa.DebtRatioLastReport = lastWas; usa.DebtRatioReportBefore = beforeWas; slot?.Remove(CountryId.USA); }
                }

                // (c) only the AI USA's book: the USA the player (no party seated - the instrument's hand, as the ministry diagnostic's (7) reads Sweden)
                SimulationRandom.Seed(777);
                using (SimulationManager.EpochScope())
                {
                    PoliSim.Elections.WorldClock.ApplyStart(CountryId.Sweden);
                    World world = WorldFactory.CreateDefault();
                    var go = new GameObject("K274.player"); hosts.Add(go);
                    SimulationManager sim = go.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    sim.PlayerCountryId = CountryId.USA;
                    Country usa = world.GetCountry(CountryId.USA);
                    var rates = new Dictionary<TaxType, float>(); foreach (TaxLine t in usa.TaxLines) { rates[t.Type] = t.Rate; }
                    Advance(sim, world, PlayerTurns);
                    int moved = 0; foreach (TaxLine t in usa.TaxLines) { if (!t.IsPerTonne && Math.Abs(t.Rate - rates[t.Type]) > 1e-6f) { moved++; } }
                    Check(sim.PlayerGoverns(usa) && usa.DebtRatioLastReport == 0f && moved == 0,
                        F("(c) the USA the player for {0} turns (the player governs: {1}): the ministry never observed it (its last ratio {2:F2}) and {3} of its rates moved - the player's book is the player's",
                            PlayerTurns, sim.PlayerGoverns(usa), usa.DebtRatioLastReport, moved));
                }

                // (d) no default in a century, the all-AI world at both seeds
                foreach (int seed in Seeds)
                {
                    SimulationRandom.Seed(seed);
                    using (SimulationManager.EpochScope())
                    {
                        PoliSim.Elections.WorldClock.ApplyStart(CountryId.Sweden);
                        int openingYear = SimulationManager.EpochDate.Year;
                        World world = WorldFactory.CreateDefault();
                        var go = new GameObject("K274.century." + seed); hosts.Add(go);
                        SimulationManager sim = go.AddComponent<SimulationManager>();
                        sim.SetWorld(world);
                        Country usa = world.GetCountry(CountryId.USA);
                        float opening = usa.State.DebtToGdpRatio;
                        var path = new List<string>();
                        int firstDefault = 0; float atHorizon = float.NaN, peak = opening;
                        var decisions = new Dictionary<CountryId, PolicyDecision>();
                        foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                        for (int year = 1; year <= Years; year++)
                        {
                            for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                            sim.AdvanceTurn(decisions);
                            float ratio = usa.State.DebtToGdpRatio;
                            peak = Mathf.Max(peak, ratio);
                            if (firstDefault == 0 && usa.Crisis != null && usa.Crisis.Crises > 0) { firstDefault = year; }
                            if (year == HorizonYears) { atHorizon = ratio; }
                            if (year % 10 == 0) { path.Add(F("y{0} {1:F1}", year, ratio)); }
                        }
                        int crises = usa.Crisis != null ? usa.Crisis.Crises : 0;
                        Check(crises == 0 && usa.DebtLimitRaises > 0,
                            F("(d) seed {0}, the all-AI world for {1} years: the USA entered {2} debt crisis(es){3}; the debt limit was raised by act {4} time(s) - its mechanics stand",
                                seed, Years, crises, firstDefault > 0 ? F(", the first in year {0}", firstDefault) : "", usa.DebtLimitRaises));
                        int cboYear = openingYear + HorizonYears;   // DebtPathHoldDiagnostic's declared calendar rule: the record after y years is the opening year plus y
                        int cboIndex = cboYear - DebtPathAnchors.CboFirstYear;
                        string cbo = cboIndex >= 0 && cboIndex < DebtPathAnchors.CboDebtHeldByPublic.Length
                            ? F("CBO's {0} figure {1:F3} % (held by the public; the prereg's band +/-25 points) - {2}", cboYear, DebtPathAnchors.CboDebtHeldByPublic[cboIndex],
                                Mathf.Abs(atHorizon - DebtPathAnchors.CboDebtHeldByPublic[cboIndex]) <= 25f ? "inside" : "OUTSIDE (the prereg's (B) misses)")
                            : F("no CBO figure for {0}", cboYear);
                        // review 1's M3: the task's words name CBO's 2055 figure, one year before the registered calendar rule's - printed beside it, never judged
                        if (cboIndex - 1 >= 0 && cboIndex - 1 < DebtPathAnchors.CboDebtHeldByPublic.Length)
                        {
                            float yearBefore = DebtPathAnchors.CboDebtHeldByPublic[cboIndex - 1];
                            cbo += F("; against CBO's {0} figure {1:F3} % (the task's wording, unjudged) {2:+0.00;-0.00} points, {3}", cboYear - 1, yearBefore, atHorizon - yearBefore,
                                Mathf.Abs(atHorizon - yearBefore) <= 25f ? "inside the same band" : "outside the same band");
                        }
                        sb.Append(F("    read      seed {0}: the USA's debt ratio opens at {1:F2} %, {2}; the peak {3:F1} %; year {4} reads {5:F2} % against {6}\n",
                            seed, opening, string.Join(", ", path), peak, HorizonYears, atHorizon, cbo));
                    }
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw - ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n').Append(e.StackTrace).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                SimulationRandom.RestoreState(seedWas, drawsWas);
            }
            Check(SimulationManager.EpochDate == epochWas, F("the epoch is put back ({0:yyyy-MM-dd})", SimulationManager.EpochDate));
            sb.Append(F("    read      the diagnostic ran {0:F1} s\n", watch.Elapsed.TotalSeconds));
            Debug.Log(sb.ToString());
            Debug.Log(failures == 0
                ? "US FISCAL REACTION: PASS - Bohn's coefficient read off the stored page; the reaction the slope times the ratio's change, both directions, nothing on a held ratio or an empty memory, never the pension line; the player's USA never touched; under an AI head its government's bill answers the last closed boundary year; no US default in a century of the all-AI world at either seed, the debt limit raised by act."
                : F("US FISCAL REACTION: FAIL - {0} check(s) failed.", failures));
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static void Advance(SimulationManager sim, World world, int turns)
        {
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
            for (int year = 1; year <= turns; year++)
            {
                for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);
            }
        }

        /// <summary>What the ministry writes for the USA now, nominal (positive = loosened, negative = tightened): the lines' changes on their amounts plus each
        /// rate's move on its base, with the counts of each kind. Pure: a fresh decision, nothing applied.</summary>
        private static float Moved(Country usa, out int cuts, out int restores, out int raises, out int lowers)
        {
            PolicyDecision decision = Boundary(usa);
            return Worth(usa, decision.SpendingLineChanges, decision.TaxRateOverrides, out cuts, out restores, out raises, out lowers);
        }

        /// <summary>The boundary's decision for the USA now - a fresh decision, nothing applied; the US reaction reads the ratio and its memory, not the closed balance.</summary>
        private static PolicyDecision Boundary(Country usa)
        {
            var decision = new PolicyDecision();
            AiFinanceMinistry.Apply(usa, 0f, decision);
            return decision;
        }

        /// <summary>A set of line changes and rates on the USA, nominal (positive = loosened, negative = tightened), with the counts of each kind.</summary>
        private static float Worth(Country usa, IDictionary<SpendingCategory, float> lines, IDictionary<TaxType, float> rates, out int cuts, out int restores, out int raises, out int lowers)
        {
            float sum = 0f; cuts = 0; restores = 0; raises = 0; lowers = 0;
            foreach (KeyValuePair<SpendingCategory, float> kv in lines)
            {
                if (kv.Value < 0f) { cuts++; } else if (kv.Value > 0f) { restores++; }
                foreach (SpendingLine line in usa.SpendingLines) { if (line.Category == kv.Key) { sum += kv.Value / 100f * line.Amount; } }
            }
            foreach (TaxLine t in usa.TaxLines)
            {
                if (!rates.TryGetValue(t.Type, out float to)) { continue; }
                if (to > t.Rate) { raises++; } else if (to < t.Rate) { lowers++; }
                sum -= TaxBases.Base(usa, t.Type) * (to - t.Rate) / 100f;
            }
            return sum;
        }

        private static int Writes(PolicyDecision decision) => decision.SpendingLineChanges.Count + decision.TaxRateOverrides.Count;

        /// <summary>The lines a set cuts (<paramref name="cuts"/>) or gives back.</summary>
        private static int Count(IDictionary<SpendingCategory, float> lines, bool cuts) { int n = 0; foreach (float v in lines.Values) { if (cuts ? v < 0f : v > 0f) { n++; } } return n; }

        /// <summary>(e): the government's bill on the USA as `TableGovernmentBudget` lays it, the slot emptied first (a pending bill blocks a tabling) and after.</summary>
        private static BudgetBill Tabled(SimulationManager sim, Dictionary<CountryId, BudgetBill> slot, Country usa)
        {
            slot.Remove(CountryId.USA);
            sim.TableGovernmentBudget(usa);
            BudgetBill bill = sim.GetPendingBudgetBill(CountryId.USA);
            slot.Remove(CountryId.USA);
            return bill;
        }

        /// <summary>Table I's column (1) coefficient on d_t, as the stored page prints it: the page's first stream LZW-decoded (PDF 1.1, EarlyChange 1), its text
        /// shown by Tj and TJ in order, split on white space; after "TABLE" "I", the first "1916-95" opens column (1), whose numbers run Constant, GVAR, YVAR,
        /// d_t - the fourth is the coefficient. Null, with the reason, where the page does not read so.</summary>
        private static string TableOneColumnOneDebt(byte[] pdf, out string why)
        {
            why = null;
            string raw = Latin1(pdf);   // one char per byte
            Match stream = Regex.Match(raw, "stream\r?\n");
            int end = stream.Success ? raw.IndexOf("endstream", stream.Index + stream.Length, StringComparison.Ordinal) : -1;
            if (end < 0) { why = "no stream on the page"; return null; }
            byte[] coded = new byte[end - (stream.Index + stream.Length)];
            Array.Copy(pdf, stream.Index + stream.Length, coded, 0, coded.Length);
            string content = Latin1(Lzw(coded));
            var text = new StringBuilder();
            foreach (Match m in Regex.Matches(content, @"\[((?:[^\]\\]|\\.)*)\]\s*TJ|\(((?:[^)\\]|\\.)*)\)\s*Tj|T\*|Td|TD|ET"))
            {
                if (m.Groups[1].Success)
                {
                    foreach (Match piece in Regex.Matches(m.Groups[1].Value, @"\(((?:[^)\\]|\\.)*)\)|(-?\d+\.?\d*)"))
                    {
                        if (piece.Groups[1].Success) { text.Append(piece.Groups[1].Value); }
                        else if (float.Parse(piece.Groups[2].Value, Inv) < -200f) { text.Append(' '); }
                    }
                }
                else if (m.Groups[2].Success) { text.Append(m.Groups[2].Value); }
                else { text.Append(' '); }
            }
            string[] tokens = Regex.Replace(text.ToString(), @"\\([()\\])", "$1").Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int table = -1;
            for (int i = 0; i + 1 < tokens.Length; i++) { if (tokens[i] == "TABLE" && tokens[i + 1] == "I") { table = i; break; } }
            if (table < 0) { why = "no \"TABLE I\" on the page"; return null; }
            int sample = Array.IndexOf(tokens, "1916-95", table);
            if (sample < 0) { why = "no 1916-95 column under Table I"; return null; }
            int seen = 0;
            for (int i = sample + 1; i < tokens.Length; i++)
            {
                if (!float.TryParse(tokens[i], NumberStyles.Float, Inv, out _)) { why = "a non-number inside column (1): " + tokens[i]; return null; }
                if (++seen == 4) { return tokens[i]; }
            }
            why = "column (1) ends early";
            return null;
        }

        /// <summary>PDF LZWDecode with EarlyChange 1: codes of 9 to 12 bits, 256 clears the table, 257 ends the data.</summary>
        private static byte[] Lzw(byte[] input)
        {
            var output = new List<byte>();
            var table = new List<byte[]>();
            void Reset() { table.Clear(); for (int i = 0; i < 256; i++) { table.Add(new[] { (byte)i }); } table.Add(null); table.Add(null); }
            Reset();
            int width = 9; long bitPos = 0, totalBits = (long)input.Length * 8;
            byte[] previous = null;
            while (bitPos + width <= totalBits)
            {
                int code = 0;
                for (int b = 0; b < width; b++, bitPos++) { code = (code << 1) | ((input[bitPos >> 3] >> (7 - (int)(bitPos & 7))) & 1); }
                if (code == 256) { Reset(); width = 9; previous = null; continue; }
                if (code == 257) { break; }
                byte[] entry;
                if (code < table.Count && table[code] != null) { entry = table[code]; }
                else if (previous != null && code == table.Count) { entry = previous.Concat(new[] { previous[0] }).ToArray(); }
                else { break; }
                output.AddRange(entry);
                if (previous != null) { table.Add(previous.Concat(new[] { entry[0] }).ToArray()); }
                previous = entry;
                int next = table.Count + 1;
                width = next >= 2048 ? 12 : next >= 1024 ? 11 : next >= 512 ? 10 : 9;
            }
            return output.ToArray();
        }

        /// <summary>Bytes as characters one for one (Latin-1), so a string index is a byte offset.</summary>
        private static string Latin1(byte[] bytes)
        {
            var chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++) { chars[i] = (char)bytes[i]; }
            return new string(chars);
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant(); }
        }

        private static string PathOf(string relative) => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), relative);
    }
}
