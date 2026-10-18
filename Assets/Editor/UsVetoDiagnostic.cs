using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-21 (`docs/specs/USA_STAGE_PLAN.md`, "The US veto"; R-US17's rule (c), INSTALLED - the fenced block's HOW THE FIGURES REACH THE GAME and WHAT STAYS
    /// AS IT IS AND ITS PROOF): THE US VETO DIAGNOSTIC the done-when names - the veto in play held to the instrument that measured it (`UsVetoDrawCheck`) and to
    /// the block's own asserts, on worlds opened as a new game opens them:
    /// <list type="bullet">
    /// <item>(a) the move: `UsVeto.cs` and the catalog in Assets/Scripts/Elections at their pinned git blobs (index and working copy), nothing left at the old
    /// paths; the files the block keeps byte-identical at their blobs (Poland's veto, its rates and its document; US-10's tools and document);</item>
    /// <item>(b) the ruling record INSTALLED (with its E4 section) at the current block and catalog digests: the block's SHA-256 the catalog's RuleDigest, the
    /// mirror line, the catalog's outcome INSTALL and decisive digest, its source digest the record's bytes; the draw installed in play;</item>
    /// <item>(c) `UsVeto.Applies` the USA's alone over every country; `UsVeto.Unit` equal to `PresidentialVeto.Draw` on eight fixed inputs outside any plant;
    /// rule (a)'s identity under the cap; the override's two thirds never below cloture's three fifths of the same base;</item>
    /// <item>(d) US-10's RECORD CASES: for every measure of V (every counted veto among them), enumerating the RUNTIME's planted indices - `Congress.DrawCrossers`
    /// through `UsVeto.Draw` under `UsVeto.PlantIndex`, over the catalog's lists - reproduces the instrument's exact P(sent) and P(named) as the same rationals,
    /// both its in-memory figures and its document's DIAGNOSTIC section; omega, and the plan's sentence "no Congress of record overrides", printed again with the
    /// draw through the runtime;</item>
    /// <item>(e) the projection's draw counter at 0 (`Congress.ProjectVeto`, `ChamberVerdicts.UsVetoProjection`), and its exact chance equal to the gate's
    /// enumeration; the budget plant - every US budget vote through the gate under j = n - 1 identical to the unplanted run, the counter 0; the five other
    /// countries' gates drawing nothing, planted or not;</item>
    /// <item>(f) one play key per chamber per bill;</item>
    /// <item>(g) PLANTED BILLS through the game's own gate (`Congress.Divide`, `SimulationManager.UsVetoGate`): a REP bill under Biden of record sent on its
    /// drawn defectors and vetoed by rule (a), the House sustaining it and the Senate never voting; the same bill on the least recorded crossing stopped at
    /// cloture, never reaching him; a DEM bill under Trump of record sent and overridden in both chambers; a REP reconciliation reaching a president of the
    /// other party - Biden of record over the 119th, and a planted game-elected Harris - vetoed by rule (a) with nothing drawn (the review of US-19's M4); a
    /// player-president's answer, signing where rule (a) would veto and vetoing where he chooses.</item>
    /// </list>
    /// <see cref="Run2024"/> is the done-when's run from 12 Mar 2024 to 20 Jan 2025. The random streams are restored after, the epoch by its scope, the draw
    /// counter and every host, so no later check starts elsewhere.
    /// </summary>
    public static class UsVetoDiagnostic
    {
        // ------------------------------------------------------------------ the pins (git blob ids, `git hash-object`, taken at US-21's base 869ff5bb)
        /// <summary>The runtime and the catalog, moved byte for byte from Assets/Editor (their blobs there before the move).</summary>
        private static readonly (string Path, string Blob, string Was)[] Moved =
        {
            ("Assets/Scripts/Elections/UsVeto.cs", "88c23c912e8821c4a0f1dda9a7c6c02fbd2e6486", "Assets/Editor/UsVeto.cs"),
            ("Assets/Scripts/Elections/Generated/UsVetoDefections.cs", "816c24c42fb040e5a943ccabbb776816f4a3782a", "Assets/Editor/Generated/UsVetoDefections.cs"),
        };

        /// <summary>The files the block keeps byte-identical (WHAT STAYS AS IT IS): Poland's B1/F3 and US-10's record of §802.</summary>
        private static readonly (string Path, string Blob)[] Unchanged =
        {
            ("Assets/Scripts/Elections/PresidentialVeto.cs", "e63fa8423e86613c3c29934ff2a1774c0fdb0686"),
            ("Assets/Scripts/Elections/Generated/PolishVetoRates.cs", "5383688ba952a582f2a3f69eb6372f05e005aa46"),
            ("docs/generated/VETO_B1_BACKTEST.md", "a5eea1003e1f90cadcc264ea7985477bfe02e171"),
            ("Tools/us_veto_prep.pl", "358eba1c4a3f1dd0af7e65dc91feaae20ba77558"),
            ("Tools/us_veto_backtest.pl", "11225df4ecfe29388cfc0751dd7aab05e68ffee5"),
            ("docs/generated/US_VETO_BACKTEST.md", "b1b10e7d0282b95bd35a982feb31006619917cec"),
        };

        private const string PlanRelative = "docs/specs/USA_STAGE_PLAN.md";
        private const string DrawDocRelative = "docs/generated/US_VETO_DRAW.md";
        private const string CsvRelative = "ElectionsData/usa/us_veto_measures.csv";
        private const string BlockBegin = "<!-- R-US17 RULE BEGIN -->\n", BlockEnd = "<!-- R-US17 RULE END -->";
        private const string MirrorInstalled = "**Ruling record:** INSTALLED (§834; E4 §835): (c).";
        private const int Seed = 777;
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        // resolved by its full parameter list, so a change of its arity fails here by name rather than at Invoke (the review of 54f10b8d, H1)
        private static readonly MethodInfo Gate = typeof(SimulationManager).GetMethod("UsVetoGate", BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(Country), typeof(DivisionRecord), typeof(BillConcern), typeof(bool), typeof(bool) }, null);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsVetoDiagnostic (PS-6 US-21, R-US17's rule (c) INSTALLED): the US veto in play ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                Check(Gate != null, "SimulationManager.UsVetoGate resolved by its full parameter list (Country, DivisionRecord, BillConcern, bool passed, bool amendment) - every reflective call gives all five");
                // ================================================================ (a) the move, and what stays byte-identical
                foreach ((string path, string blob, string was) in Moved)
                {
                    string index = GitText("rev-parse :" + path), working = GitText("hash-object -- " + path);
                    Check(index == blob && working == blob && !File.Exists(Path.Combine(Root, was)) && !File.Exists(Path.Combine(Root, was + ".meta")),
                        F("(a) {0}: the index blob {1} and the working copy's {2} are the pinned {3} - moved byte for byte from {4}, nothing left there", path, Short(index), Short(working), Short(blob), was));
                }
                Check(!Directory.Exists(Path.Combine(Root, "Assets/Editor/Generated")) || Directory.GetFiles(Path.Combine(Root, "Assets/Editor/Generated"), "UsVetoDefections*").Length == 0,
                    "(a) no copy of the catalog left under Assets/Editor/Generated");
                foreach ((string path, string blob) in Unchanged)
                {
                    string index = GitText("rev-parse :" + path), working = GitText("hash-object -- " + path);
                    Check(index == blob && working == blob, F("(a) byte-identical: {0} at its blob {1} (index {2}, working copy {3})", path, Short(blob), Short(index), Short(working)));
                }

                // ================================================================ (b) the ruling record INSTALLED at the current digests
                string plan = File.ReadAllText(Path.Combine(Root, PlanRelative), new UTF8Encoding(false)).Replace("\r\n", "\n").TrimStart('﻿');
                int b0 = plan.IndexOf(BlockBegin, StringComparison.Ordinal), b1 = plan.IndexOf(BlockEnd, StringComparison.Ordinal);
                string block = b0 >= 0 && b1 > b0 ? plan.Substring(b0 + BlockBegin.Length, b1 - b0 - BlockBegin.Length) : null;
                string blockDigest = block == null ? "-" : Sha256(Encoding.UTF8.GetBytes(block));
                Check(blockDigest == UsVetoDefections.RuleDigest, F("(b) the R-US17 block's SHA-256 {0} is the catalog's RuleDigest {1} - the rule the catalog was generated under", Short(blockDigest), Short(UsVetoDefections.RuleDigest)));
                string mirror = MirrorLine(plan, b1);
                Check(mirror == MirrorInstalled, F("(b) the ruling record's mirror reads \"{0}\" - INSTALLED with its E4 section", mirror));
                string csvDigest = Sha256(File.ReadAllBytes(Path.Combine(Root, CsvRelative)));
                Check(UsVetoDefections.Outcome == "INSTALL" && UsVetoDefections.DecisiveDigest == "12da26b99c3506c38bbd17e5ecf5b0ab38f50e00a503faa53418bc3123842866"
                      && UsVetoDefections.SourceDigest == csvDigest && UsVetoDefections.SourcePin == csvDigest && UsVetoDefections.MinimumObservations == UsVeto.MinimumObservations && Congress.DrawInstalled,
                    F("(b) the catalog: outcome {0}, decisive digest {1} (the one §834 registered), source digest {2} - the record's bytes; the draw installed in play", UsVetoDefections.Outcome, Short(UsVetoDefections.DecisiveDigest), Short(csvDigest)));
                bool cellsFitted = new[] { UsVetoDefections.HP, UsVetoDefections.HO, UsVetoDefections.SP, UsVetoDefections.SO }.All(c => c.Fitted && c.List.Length >= UsVetoDefections.MinimumObservations)
                                   && Congress.CellOf(Congress.House, true).Count == UsVetoDefections.HP.List.Length && Congress.CellOf(Congress.Senate, false).Count == UsVetoDefections.SO.List.Length;
                Check(cellsFitted, F("(b) the four cells the runtime indexes, each fitted and read in the catalog's order: HP {0}, HO {1}, SP {2}, SO {3} observations",
                    UsVetoDefections.HP.List.Length, UsVetoDefections.HO.List.Length, UsVetoDefections.SP.List.Length, UsVetoDefections.SO.List.Length));

                // ================================================================ (c) scope, the unit, (a)'s identity, the override's base
                bool usaAlone = Enum.GetValues(typeof(CountryId)).Cast<CountryId>().All(id => UsVeto.Applies(id) == (id == CountryId.USA));
                Check(usaAlone, "(c) UsVeto.Applies is true for the USA alone, over every country (H1)");
                var unitInputs = new (int Seed, CountryId Country, DateTime Date, string Title)[]
                {
                    (777, CountryId.Poland, new DateTime(2024, 3, 12), "Enact: Cash Bail Reform Act"), (424242, CountryId.Poland, new DateTime(2025, 6, 1), "Tax act: VAT 23 % to 22 %"),
                    (1, CountryId.Poland, new DateTime(2023, 12, 13), "Labor Market bill"), (-5, CountryId.Sweden, new DateTime(2026, 9, 13), ""),
                    (int.MaxValue, CountryId.USA, new DateTime(2024, 11, 5), "Crime & Justice bill"), (20260913, CountryId.Germany, new DateTime(2025, 2, 23), "Ü ö ä - non-ASCII"),
                    (0, CountryId.Italy, new DateTime(2022, 7, 21), "Implement IncomeTax"), (31337, CountryId.France, new DateTime(2024, 7, 18), "Annual budget bill"),
                };
                bool unitSame = unitInputs.All(x => UsVeto.Unit(x.Seed.ToString(CultureInfo.InvariantCulture) + "|" + x.Country + "|" + x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "|" + x.Title)
                                                     == PresidentialVeto.Draw(x.Seed, x.Country, x.Date, x.Title));
                Check(unitSame, F("(c) UsVeto.Unit on Poland's key format equals PresidentialVeto.Draw on {0} fixed inputs, outside any plant (the block's SEEDS AND DETERMINISM)", unitInputs.Length));
                bool identity = true;
                foreach (int seats in new[] { 1, 2, 3, 47, 49, 51, 53, 199, 213, 215, 220, 222, 235 })
                {
                    foreach (UsVetoDefections.Cell cell in new[] { UsVetoDefections.HP, UsVetoDefections.SP })
                    {
                        foreach (UsVetoDefections.Entry e in cell.List) { identity &= UsVeto.NamedByA(seats, seats - UsVeto.Crossers(seats, e.Defectors, e.Voting)); }
                    }
                }
                Check(identity, "(c) THE VETO's identity: under the cap, rule (a) fires on every drawn K of the president's party's lists, at every seat count tried - the draw decides whether a bill REACHES the president, never whether (a) fires");
                bool base23 = Enumerable.Range(1, 200).All(w => (2 * w + 2) / 3 >= (3 * w + 4) / 5);
                Check(base23, "(c) with every member voting, the override's two thirds is never below cloture's three fifths of the same base (1 to 200 members)");

                // ================================================================ (d) US-10's record cases: the runtime's planted-index enumeration against the instrument
                List<UsVetoDrawCheck.VetoPathMeasure> vPath = UsVetoDrawCheck.VetoPathForPlay();
                Dictionary<string, (string Sent, string Named)> doc = DiagnosticSection(File.ReadAllText(Path.Combine(Root, DrawDocRelative), new UTF8Encoding(false)));
                int counted = 0, agree = 0, agreeAll = 0;
                var omegas = new List<string>();
                var mismatches = new List<string>();
                UsVeto.ResetDrawCalls();
                foreach (UsVetoDrawCheck.VetoPathMeasure m in vPath)
                {
                    Enumerated h = Enumerate(m, m.House, m.HousePpCell, m.HouseOpCell, Congress.House), s = Enumerate(m, m.Senate, m.SenatePpCell, m.SenateOpCell, Congress.Senate);
                    string sent = Rational(h.Pass * s.Pass, h.Total * s.Total), named = Rational(h.Named * s.Pass, h.Total * s.Total), omega = Rational(h.PassOverride * s.PassOverride, h.Total * s.Total);
                    bool inDoc = doc.TryGetValue(m.Id, out (string Sent, string Named) d);
                    bool same = sent == m.Sent && named == m.Named && inDoc && d.Sent == sent && d.Named == named;
                    // the rationals themselves are printed on no surface but US_VETO_DRAW.md's DIAGNOSTIC section (the block's THE PRINT): a difference names the
                    // measure and which of the three disagree, never a figure
                    if (same) { agreeAll++; } else { mismatches.Add(F("{0} ({1})", m.Id, !inDoc ? "absent from the document" : sent != m.Sent || named != m.Named ? "the runtime against the instrument" : "the document against both")); }
                    if (m.CountedVeto) { counted++; if (same) { agree++; } }
                    if (omega != "0/1") { omegas.Add(m.Id); }
                }
                Check(vPath.Count > 0 && counted > 0 && agree == counted && agreeAll == vPath.Count && doc.Count == vPath.Count,
                    F("(d) US-10's record cases: for all {0} counted vetoes in V ({1} measures of V in all), enumerating the runtime's planted indices (Congress.DrawCrossers through UsVeto.Draw under UsVeto.PlantIndex, the catalog's lists) reproduces the instrument's exact P(sent) and P(named) - its in-memory figures and the {2} lines of {3}'s DIAGNOSTIC section - as the same rationals; {4} measure(s) agree{5}",
                        counted, vPath.Count, doc.Count, DrawDocRelative, agreeAll, mismatches.Count == 0 ? string.Empty : " - differing: " + string.Join("; ", mismatches)));
                sb.Append(F("    printed   THE OVERRIDE through the runtime, on the passage's recorded sides (crossers counted only where they entered, no fresh draw): {0} - {1} (the figures: US_VETO_DRAW.md, section 6)\n",
                    omegas.Count == 0 ? "no measure of V can be both sent and overridden in both chambers, " + vPath.Count + " of " + vPath.Count + " measures read" : "a measure can be sent and overridden: " + string.Join(", ", omegas),
                    omegas.Count == 0 ? "under it no Congress of record overrides (the plan's sentence, measured again with the draw)" : "the plan's sentence does not hold with the draw"));
                Check(UsVeto.DrawCalls > 0, F("(d) the enumeration ran through UsVeto.Draw: {0} planted draws counted", UsVeto.DrawCalls));

                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        // ============================================================ the 118th of record under Biden: 12 Mar 2024, the US start
                        DateTime start = WorldClock.StartDate(CountryId.USA);
                        (SimulationManager sim, World world, Country usa) = UsCongressDiagnostic.OpenUsa(hosts, start, "REP");
                        Check(Congress.PresidentOn(usa, start, out string biden, out string pp118, out bool player118) && pp118 == "DEM" && !player118 && Congress.HoldsHouse(usa, "REP") && Congress.HoldsSenate(usa, "DEM", start),
                            F("(g) the US start {0}: the president of the day {1} ({2}), not the player's; REP holds the House, DEM the Senate", Day(start), biden, pp118));
                        BillConcern repCrime = Concern(usa, "REP", u => ParliamentSystem.GetCrimeJusticeBillConcern(u, UsCongressDiagnostic.CrimeBill(u, "REP")));

                        // (e) the projection never draws, and reads the gate's own chances
                        UsVeto.ResetDrawCalls();
                        Congress.Verdict blocs = Congress.Project(usa, repCrime, start, Congress.OriginOf(usa, repCrime, start));
                        Congress.VetoProjection projected = Congress.ProjectVeto(usa, repCrime, start, blocs);
                        Congress.VetoProjection viaPage = new PoliSim.UI.ChamberVerdicts().UsVetoProjection(usa, repCrime, start);
                        long projectionDraws = UsVeto.DrawCalls;
                        (long hp, long ht) = GateEnumeration(usa, blocs.House, "DEM", start);
                        (long sp, long st) = GateEnumeration(usa, blocs.Senate, "DEM", start);
                        Check(projectionDraws == 0 && projected != null && viaPage != null && projected.OnVetoPath && projected.NamedByA && !projected.PlayerPresident && !projected.BlocsSend
                              && projected.FirstPasses.Passing * ht == hp * projected.FirstPasses.Total && projected.SecondPasses.Passing * st == sp * projected.SecondPasses.Total
                              && viaPage.FirstPasses.ToString() == projected.FirstPasses.ToString() && viaPage.SecondPasses.ToString() == projected.SecondPasses.ToString(),
                            F("(e) the projection (Congress.ProjectVeto, and ChamberVerdicts.UsVetoProjection over the page's cache) never draws - the counter {0} - and its exact chance in each chamber equals the gate's own enumeration over the same {1} and {2} index pairs (the chances themselves are printed on no surface); on the veto path, rule (a) vetoing, the blocs sending nothing",
                                projectionDraws, ht, st));

                        // (g) a REP bill sent on its drawn defectors and vetoed: the House sustains, the Senate never votes
                        Gated vetoed = Through(sim, usa, "Crime & Justice bill", repCrime, start, (chamber, party, n) => int.MaxValue);
                        DivisionRecord senatePassage = vetoed.Written.FirstOrDefault(r => r.Chamber == Congress.Senate && r.Required != 0 && r.Cloture);
                        int demCrossers = senatePassage?.Sides.Where(x => x.Defectors && x.Abbrev == "DEM").Sum(x => x.Seats) ?? 0;
                        DivisionRecord sustained = vetoed.Written.LastOrDefault();
                        Check(vetoed.Sent && !vetoed.Law && vetoed.Written.Count == 3 && vetoed.Written[0].Chamber == Congress.House && vetoed.Written[0].Motion && vetoed.Written[1].Motion && senatePassage != null && senatePassage.Passed
                              && demCrossers > 0 && demCrossers == vetoed.Verdict.Senate.Crossing?.Kp && vetoed.Verdict.Senate.Crossing.Matters && !(vetoed.Verdict.House.Crossing?.Matters ?? true)
                              && sustained != null && sustained.Chamber == Congress.House && sustained.Required == (2 * sustained.Sides.Sum(x => x.Seats) + 2) / 3 && !sustained.Cloture && !sustained.Passed && sustained.OtherChamber == null
                              && sustained.Title.StartsWith("Vetoed by the President (" + biden + ")", StringComparison.Ordinal) && !Congress.GovernmentBears(usa, repCrime) && !vetoed.LostMarked && vetoed.Draws == 4,
                            F("(g) a REP crime-and-justice bill under {0}, the draw planted at the largest recorded crossing (j = n - 1): the House passes it on the blocs (its draw does not matter, so no crosser enters), the Senate invokes cloture on {1} DEM defectors entered as their own side; sent, vetoed by rule (a); the House sustains the veto - \"{2}\"; the Senate never votes; the House majority's bill is not the government's (Congress.GovernmentBears), so the sustained veto marks no day lost; {3} draws counted",
                                biden, demCrossers, sustained?.Title, vetoed.Draws));

                        // (g) WHO BEARS A VETOED BILL at a real site (US-19's rule, Congress.GovernmentBears): the REP House majority's bill, introduced through the game's
                        // own entry point and voted on its day, sent on its drawn defectors and vetoed - the government charged nothing for it, no day marked lost
                        usa.BillLostOn = DateTime.MinValue;
                        float approvalBefore = usa.State.ApprovalRating;
                        int numberBefore = usa.Divisions.Entries.Count > 0 ? usa.Divisions.Entries.Last().Number : 0;
                        bool introduced = sim.IntroduceCrimeJusticeBill(CountryId.USA, UsCongressDiagnostic.CrimeBill(usa, "REP"));
                        using (UsVeto.PlantIndex(int.MaxValue))
                        {
                            for (int d = 0; d < ParliamentSystem.BillDurationDays && sim.GetPendingCrimeJusticeBill(CountryId.USA) != null; d++) { sim.AdvanceCrimeJusticeBillDay(CountryId.USA); }
                        }
                        List<DivisionRecord> atSite = usa.Divisions.Entries.Where(r => r.Number > numberBefore).ToList();
                        Check(introduced && sim.GetPendingCrimeJusticeBill(CountryId.USA) == null && atSite.Count == 3 && atSite.Last().Required > 0 && !atSite.Last().Passed && usa.State.ApprovalRating == approvalBefore && usa.BillLostOn == DateTime.MinValue,
                            F("(g) at the game's own site (IntroduceCrimeJusticeBill, its day's resolution): the REP House majority's bill sent and vetoed costs the government nothing - approval {0} before and after - and marks no day lost (the site's result charges a failed bill only where the government bears it; the gate charges nothing itself)",
                                approvalBefore.ToString("0.###", CultureInfo.InvariantCulture)));

                        // (g) the president's own party's bill is never vetoed by rule (a): its author votes for it (the stance model's own term), so it is never on the veto path
                        BillConcern demOwn = Concern(usa, "DEM", u => ParliamentSystem.GetCrimeJusticeBillConcern(u, UsCongressDiagnostic.CrimeBill(u, "DEM")));
                        Congress.VetoProjection own = Congress.ProjectVeto(usa, demOwn, start, Congress.Project(usa, demOwn, start, Congress.OriginOf(usa, demOwn, start)));
                        Check(own != null && !own.NamedByA && !own.OnVetoPath && own.PresidentsParty == "DEM",
                            "(g) the president's own party's bill (a DEM bill under Biden): its author votes for it, so it is off the veto path and rule (a) never names it - an AI president vetoes none of his own party's bills in this build");

                        // (f) one play key per chamber per bill, on that bill
                        Check(OneKeyPerChamber(vetoed.Verdict, start, "Crime & Justice bill", out string keys), "(f) one play key per chamber per bill: " + keys);

                        // (g) the same bill on the least recorded crossing: stopped at cloture, never reaching the president
                        Gated stopped = Through(sim, usa, "Crime & Justice bill", repCrime, start, (chamber, party, n) => 0);
                        Check(!stopped.Sent && !stopped.Law && stopped.Written.Count == 2 && stopped.Written[1].Chamber == Congress.Senate && !stopped.Written[1].Passed && stopped.Written.All(r => !r.Sides.Any(x => x.Defectors))
                              && stopped.Written.All(r => r.Required == 0 || r.Cloture) && !stopped.LostMarked,
                            F("(g) the same bill on the least recorded crossing (j = 0): the Senate's cloture fails ({0} for, {1} needed), no crosser enters - the president never sees it, no vote on a veto is written, and the House majority's rejected bill marks no day lost",
                                stopped.Written.ElementAtOrDefault(1)?.Sides.Where(x => x.Side > 0).Sum(x => x.Seats), stopped.Written.ElementAtOrDefault(1)?.Required));

                        // (g) an amendment is neither drawn on nor presented (the review of 54f10b8d, M3): the REP House majority's Constitutional Debt Brake Act, the
                        // draw planted at the largest recorded crossing - nothing is drawn, so it dies at cloture as it did before US-21
                        LawDefinition brake = LawCatalog.GetById("constitutional_debt_brake_act");
                        BillConcern amendment = Concern(usa, "REP", u => ParliamentSystem.GetLawBillConcern(u, new LawBill { LawId = "constitutional_debt_brake_act" }));
                        Congress.Verdict amendmentBlocs = Congress.Project(usa, amendment, start, Congress.OriginOf(usa, amendment, start));
                        Congress.VetoProjection amendmentProjection = Congress.ProjectVeto(usa, amendment, start, amendmentBlocs);
                        bool demAgainst = amendmentBlocs.Senate.Stances.Any(x => x.Party.Abbrev == "DEM" && x.Measured && x.Side < 0);
                        Gated amended = Through(sim, usa, "Enact: " + (brake?.Name ?? "constitutional_debt_brake_act"), amendment, start, (chamber, party, n) => int.MaxValue);
                        Check(brake != null && brake.ConstitutionalAmendment && amendment.Unpresented && demAgainst && amended.Draws == 0 && !amended.Sent && amended.Written.Count == 2 && amended.Written[1].Chamber == Congress.Senate
                              && !amended.Written[1].Passed && amended.Written.All(r => !r.Sides.Any(x => x.Defectors)) && amended.Verdict.House.Crossing == null && amended.Verdict.Senate.Crossing == null
                              && amendmentProjection != null && amendmentProjection.Unpresented && !amendmentProjection.OnVetoPath && !amendmentProjection.NamedByA,
                            F("(g) an amendment is not drawn on: the REP House majority's {0}, the president's party against it, the draw planted at j = n - 1 - nothing drawn (the counter {1}), so it dies at cloture as before US-21 ({2} for, {3} needed); the projection names no veto and no draw",
                                brake?.Name, amended.Draws, amended.Written.ElementAtOrDefault(1)?.Sides.Where(x => x.Side > 0).Sum(x => x.Seats), amended.Written.ElementAtOrDefault(1)?.Required));

                        // (g) ... nor presented (the review of dc32c5ff, L-a): the same amendment SENT - the Senate's seats PLANTED for this case alone (REP 61, DEM 39), so the
                        // blocs carry cloture - and gated as the law site gates it (amendment: true, its `law.ConstitutionalAmendment`): it becomes law with no
                        // presentment and no vote on a veto; the same passage gated with amendment: false IS presented, and rule (a) vetoes it - the flag decides
                        Dictionary<string, int> senateOfRecord = usa.SecondChamberSeats;
                        var senate61 = new Dictionary<string, int>();
                        foreach (string key in senateOfRecord.Keys) { senate61[key] = 0; }
                        senate61["REP"] = 61;
                        senate61["DEM"] = 39;
                        Gated unpresented, presented;
                        try
                        {
                            usa.SecondChamberSeats = senate61;
                            unpresented = Through(sim, usa, "Enact: " + (brake?.Name ?? "constitutional_debt_brake_act"), amendment, start, (chamber, party, n) => int.MaxValue, amendment: true);
                            presented = Through(sim, usa, "Enact: " + (brake?.Name ?? "constitutional_debt_brake_act"), amendment, start, (chamber, party, n) => int.MaxValue, amendment: false);
                        }
                        finally { usa.SecondChamberSeats = senateOfRecord; }
                        bool NoVetoVote(Gated g) => !g.Written.Any(r => r.Required > 0 && !r.Cloture);
                        Check(unpresented.Sent && unpresented.Law && unpresented.Draws == 0 && unpresented.Written.Count == 2 && NoVetoVote(unpresented) && unpresented.Written.All(r => !r.Sides.Any(x => x.Defectors))
                              && presented.Sent && !presented.Law && presented.Draws == 0 && presented.Written.Count == 3 && !NoVetoVote(presented) && presented.Written.Last().Title.StartsWith("Vetoed by the President (" + biden + ")", StringComparison.Ordinal),
                            F("(g) an amendment is not presented: the {0} sent through both chambers on its blocs (the Senate planted REP 61 of 100, cloture at 60; nothing drawn, the counter {1}) and gated as the law site gates it becomes law with no presentment and no vote on a veto ({2} records); the same passage gated as an ordinary act is presented and vetoed by rule (a) - \"{3}\" - so the amendment flag is what decides it",
                                brake?.Name, unpresented.Draws + presented.Draws, unpresented.Written.Count, presented.Written.LastOrDefault()?.Title));

                        // (e) the budget plant: every US budget vote identical under j = n - 1, the counter 0 - a REP tax programme bill the president's party opposes among them
                        var budgets = new List<(string Title, BillConcern Concern)>
                        {
                            ("Annual budget bill (rates)", Concern(usa, "REP", u => ParliamentSystem.GetBudgetBillConcern(u, UsCongressDiagnostic.RatesBill(u, "REP")))),
                            ("Annual budget bill (spending)", Concern(usa, "REP", u => ParliamentSystem.GetBudgetBillConcern(u, UsCongressDiagnostic.SpendingBill(u, SpendingCategory.Transportation, -10f)))),
                            ("Remove tax (planted)", Concern(usa, "REP", u => ParliamentSystem.GetTaxProgramBillConcern(u, new TaxProgramBill { Type = u.TaxLines.First(t => t.IsImplemented).Type, IsAdd = false }))),
                            ("Welfare programme (planted)", Concern(usa, "DEM", u => ParliamentSystem.GetWelfareProgramBillConcern(u, new WelfareProgramBill { Type = u.WelfarePrograms[0].Type, IsAdd = !u.WelfarePrograms[0].IsImplemented }))),
                            ("Sovereign fund drawdown (planted)", Concern(usa, "REP", u => ParliamentSystem.GetSwfDrawdownBillConcern(u, new SwfDrawdownBill { WithdrawalPercentOfGdp = 1f }))),
                        };
                        bool allBudget = budgets.All(b => b.Concern.BudgetVote);
                        int ppAgainstBudgets = 0, identical = 0;
                        long budgetDraws = 0;
                        foreach ((string title, BillConcern concern) in budgets)
                        {
                            Congress.Verdict bv = Congress.Project(usa, concern, start, Congress.OriginOf(usa, concern, start));
                            if (bv.House.Stances.Any(x => x.Party.Abbrev == "DEM" && x.Measured && x.Side < 0)) { ppAgainstBudgets++; }
                            Gated plain = Through(sim, usa, title, concern, start, null, gate: false);
                            Gated planted = Through(sim, usa, title, concern, start, (chamber, party, n) => int.MaxValue, gate: false);
                            budgetDraws += plain.Draws + planted.Draws;
                            if (SameRecords(plain.Written, planted.Written) && plain.Sent == planted.Sent) { identical++; }
                        }
                        Check(allBudget && identical == budgets.Count && budgetDraws == 0 && ppAgainstBudgets > 0,
                            F("(e) the budget plant: {0} US budget votes (a rates budget, a spending budget, a tax programme, a welfare programme, the fund's drawdown - {1} of them opposed by the president's party) through the gate under j = n - 1 write records identical to the unplanted run's, {2} of {0}; UsVeto refuses each - the counter {3}",
                                budgets.Count, ppAgainstBudgets, identical, budgetDraws));

                        // (e) the five other countries draw nothing, planted or not
                        long otherDraws = 0;
                        var others = new List<string>();
                        foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland })
                        {
                            Country c = world.GetCountry(id);
                            string mine = c.PlayerPartyAbbrev;
                            if (string.IsNullOrEmpty(c.PlayerPartyAbbrev)) { c.PlayerPartyAbbrev = c.ParliamentSeats.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).First().Key; }
                            try
                            {
                                BillConcern crime = ParliamentSystem.GetCrimeJusticeBillConcern(c, UsCongressDiagnostic.CrimeBill(c, "x"));
                                long before = UsVeto.DrawCalls;
                                ParliamentSystem.Divide(c, "Crime & Justice bill (UsVetoDiagnostic, one chamber)", crime, start, out bool _);
                                using (UsVeto.PlantIndex(int.MaxValue)) { ParliamentSystem.Divide(c, "Crime & Justice bill (UsVetoDiagnostic, one chamber, planted)", crime, start, out bool _); }
                                otherDraws += UsVeto.DrawCalls - before;
                                long drewHere = UsVeto.DrawCalls - before;   // the counter is the proof; the projection is computed on a real verdict and answers nothing outside the USA
                                others.Add(F("{0} {1}", id, drewHere == 0 && Congress.ProjectVeto(c, crime, start, Congress.Project(c, crime, start)) == null && !Congress.IsBudgetVote(c, Congress.Kind.Budget) && !Congress.IsUnpresented(c, LawCatalog.GetById("constitutional_debt_brake_act")) ? "draws nothing" : "DREW"));
                            }
                            finally { c.PlayerPartyAbbrev = mine; }
                        }
                        Check(otherDraws == 0 && others.All(o => o.EndsWith("draws nothing", StringComparison.Ordinal)), F("(e) the five other countries: {0} - the counter {1}", string.Join(", ", others), otherDraws));

                        // ============================================================ the 119th of record under Trump: a DEM bill overridden in both chambers
                        (SimulationManager sim119, World _, Country usa119) = UsCongressDiagnostic.OpenUsa(hosts, UsCongressDiagnostic.Day119, "DEM");
                        Check(Congress.PresidentOn(usa119, UsCongressDiagnostic.Day119, out string trump, out string pp119, out bool p119) && pp119 == "REP" && !p119,
                            F("(g) {0}: the president of the day {1} ({2})", Day(UsCongressDiagnostic.Day119), trump, pp119));
                        BillConcern demCrime = Concern(usa119, "DEM", u => ParliamentSystem.GetCrimeJusticeBillConcern(u, UsCongressDiagnostic.CrimeBill(u, "DEM")));
                        Gated overridden = Through(sim119, usa119, "Crime & Justice bill", demCrime, UsCongressDiagnostic.Day119, (chamber, party, n) => party == "REP" ? int.MaxValue : 0);
                        DivisionRecord last = overridden.Written.LastOrDefault();
                        Check(overridden.Sent && overridden.Law && last != null && last.Passed && last.Required > 0 && !last.Cloture && last.OtherChamber != null && last.OtherChamber.Passed && last.OtherChamber.Chamber == Congress.House
                              && last.Chamber == Congress.Senate && overridden.Verdict.House.Crossing?.Matters == true && overridden.Verdict.Senate.Crossing?.Matters == true && !overridden.LostMarked
                              && last.Title.Contains("two thirds in each chamber, so it becomes law") && !Congress.GovernmentBears(usa119, demCrime),
                            F("(g) a DEM crime-and-justice bill under {0}, REP's draw planted at its largest recorded crossing: the crossers decide both chambers and enter both; sent, vetoed by rule (a), and overridden - the House first, then the Senate: \"{1}\"; it becomes law, and as the minority's bill the government bears no cost of it (Congress.GovernmentBears)", trump, last?.Title));

                        // ============================================================ a reconciliation bill reaching a president of the other party (US-19's review, M4)
                        DateTime lameDuck = new DateTime(2025, 1, 15);
                        (SimulationManager simLd, World _, Country usaLd) = UsCongressDiagnostic.OpenUsa(hosts, lameDuck, "REP");
                        BillConcern repRates = Concern(usaLd, "REP", u => ParliamentSystem.GetBudgetBillConcern(u, UsCongressDiagnostic.RatesBill(u, "REP")));
                        Gated lame = Through(simLd, usaLd, "Annual budget bill", repRates, lameDuck, (chamber, party, n) => int.MaxValue);
                        Check(Congress.HoldsHouse(usaLd, "REP") && Congress.HoldsSenate(usaLd, "REP", lameDuck) && repRates.Reconciliation && repRates.BudgetVote && lame.Sent && !lame.Law && lame.Draws == 0
                              && lame.Written.LastOrDefault() is DivisionRecord ldVeto && ldVeto.Required > 0 && !ldVeto.Passed && !lame.Written.Any(r => r.Sides.Any(x => x.Defectors))
                              && !Congress.GovernmentBears(usaLd, repRates) && !lame.LostMarked,
                            F("(g) a REP reconciliation (a rates-only budget) on {0}, the 119th seated and Biden of record in office: both chambers pass it by their majorities with nothing drawn (a budget vote; the plant ignored, the counter {1}), rule (a) vetoes it, and the House sustains the veto - \"{2}\"; the majority's bill, so no day is the government's loss",
                                Day(lameDuck), lame.Draws, lame.Written.LastOrDefault()?.Title));

                        // a game-elected Harris over the 119th (planted as the oath installs one), the player REP
                        var harris = new UsPresidentialElection.Contest { PollingDay = new DateTime(2024, 11, 5), TakesOffice = new DateTime(2025, 1, 20), Winner = "Kamala D. Harris", WinnerParty = "DEM", ElectorsAppointed = 538, Line = "planted (UsVetoDiagnostic)" };
                        harris.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Kamala D. Harris", Party = "DEM", Votes = 270 });
                        harris.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Donald J. Trump", Party = "REP", Votes = 268 });
                        usa119.UsPresidentialElections.Add(harris);
                        GovernmentRecord trumpOfRecord = usa119.Government;
                        usa119.Government = UsPresidentialElection.GovernmentOf(harris, harris.TakesOffice);   // her presidency, as the oath installs it - who bears a bill reads it
                        try
                        {
                            usa119.PlayerPartyAbbrev = "REP";
                            bool found = Congress.PresidentOn(usa119, UsCongressDiagnostic.Day119, out string gamePresident, out string gameParty, out bool gamePlayer);
                            BillConcern repTax = Concern(usa119, "REP", u => ParliamentSystem.GetTaxProgramBillConcern(u, new TaxProgramBill { Type = u.TaxLines.First(t => t.IsImplemented).Type, IsAdd = false }));
                            Gated reconciled = Through(sim119, usa119, "Remove tax (planted, M4)", repTax, UsCongressDiagnostic.Day119, null);
                            Check(found && gameParty == "DEM" && !gamePlayer && repTax.Reconciliation && reconciled.Sent && !reconciled.Law && reconciled.Draws == 0 && reconciled.Written.LastOrDefault()?.Required > 0
                                  && !Congress.GovernmentBears(usa119, repTax) && !reconciled.LostMarked,
                                F("(g) a planted game-elected president ({0}, {1}; her presidency installed as the oath installs it) over the 119th, the player REP: REP's tax programme bill rides reconciliation through both REP chambers - the case US-19's review named (M4) - and rule (a) vetoes it; the veto stands, and the majority's bill is no loss of the government's: \"{2}\"",
                                    gamePresident, gameParty, reconciled.Written.LastOrDefault()?.Title));

                            // the player-president: the planted winner is the player's (DEM). His answer, on a bill both chambers sent - the records written as the gate
                            // receives them (the House REP 220 for, DEM 215 against; the Senate by reconciliation, REP 53 for, DEM 47 against), so the stances cannot move
                            // them - rule (a) would veto it, and is not read for him
                            usa119.PlayerPartyAbbrev = "DEM";
                            bool playerFound = Congress.PresidentOn(usa119, UsCongressDiagnostic.Day119, out _, out _, out bool isPlayer);
                            BillConcern repRates119 = Concern(usa119, "REP", u => ParliamentSystem.GetBudgetBillConcern(u, UsCongressDiagnostic.RatesBill(u, "REP")));
                            BillConcern demRates119 = Concern(usa119, "DEM", u => ParliamentSystem.GetBudgetBillConcern(u, UsCongressDiagnostic.RatesBill(u, "DEM")));
                            DivisionRecord Sent(string title)
                            {
                                var house = new DivisionRecord { Title = title, Date = UsCongressDiagnostic.Day119, Passed = true, Chamber = Congress.House, Origin = Congress.House, Reconciliation = true, Motion = true };
                                house.Sides.Add(new DivisionSide { Abbrev = "REP", ShortName = "REP", Seats = 220, Side = 1 });
                                house.Sides.Add(new DivisionSide { Abbrev = "DEM", ShortName = "DEM", Seats = 215, Side = -1 });
                                var senate = new DivisionRecord { Title = title, Date = UsCongressDiagnostic.Day119, Passed = true, Chamber = Congress.Senate, Origin = Congress.House, Reconciliation = true, Sworn = 100, OtherChamber = house };
                                senate.Sides.Add(new DivisionSide { Abbrev = "REP", ShortName = "REP", Seats = 53, Side = 1 });
                                senate.Sides.Add(new DivisionSide { Abbrev = "DEM", ShortName = "DEM", Seats = 47, Side = -1 });
                                return senate;
                            }
                            (bool Law, bool Lost, DivisionRecord Last) Answer(BillConcern concern, Func<DivisionRecord, bool> answer)
                            {
                                usa119.BillLostOn = DateTime.MinValue;
                                Func<DivisionRecord, bool> was = sim119.PlayerPresidentVetoes;
                                sim119.PlayerPresidentVetoes = answer;
                                try
                                {
                                    bool law = (bool)Gate.Invoke(sim119, new object[] { usa119, Sent("Annual budget bill (planted, the player-president)"), concern, true, false });
                                    return (law, usa119.BillLostOn == UsCongressDiagnostic.Day119.Date, usa119.Divisions.Entries.LastOrDefault());
                                }
                                finally { sim119.PlayerPresidentVetoes = was; }
                            }
                            (bool Law, bool Lost, DivisionRecord Last) signs = Answer(repRates119, null), vetoes = Answer(repRates119, _ => true), ownVetoed = Answer(demRates119, _ => true);
                            Check(playerFound && isPlayer && UsVeto.NamedByA(215, 215) && signs.Law && !signs.Lost
                                  && !vetoes.Law && vetoes.Last != null && vetoes.Last.Title.Contains("(you, ") && !vetoes.Last.Passed && !vetoes.Lost && !Congress.GovernmentBears(usa119, repRates119)
                                  && !ownVetoed.Law && ownVetoed.Lost && Congress.GovernmentBears(usa119, demRates119),
                                F("(g) the player-president (the planted contest's winner, of the player's party; the bill's records as the gate receives them): with no answer he signs, rule (a) not read though it would veto (215 of his party's 215 House members against); answering veto, it is vetoed and the House sustains it (\"{0}\") - the REP majority's bill, so no day is lost; his veto of his own party's bill is the government's loss: the day marked lost (Congress.RecordLost by Congress.GovernmentBears)",
                                    vetoes.Last?.Title));

                            // an UNCONTESTED bill vetoed (the review of 54f10b8d, M1): a law whose concern moves nothing passes both chambers unasked; the player-president
                            // vetoes it, and the gate and the projection read the one override rule - every member of each chamber's base for it, carried in both
                            usa119.PlayerPartyAbbrev = "DEM";
                            BillConcern unasked = Concern(usa119, "DEM", u => ParliamentSystem.GetLawBillConcern(u, new LawBill { LawId = "debt_office_long_issuance_act" }));
                            Congress.Verdict unaskedBlocs = Congress.Project(usa119, unasked, UsCongressDiagnostic.Day119, Congress.OriginOf(usa119, unasked, UsCongressDiagnostic.Day119));
                            Congress.VetoProjection unaskedProjection = Congress.ProjectVeto(usa119, unasked, UsCongressDiagnostic.Day119, unaskedBlocs);
                            Gated unaskedVeto = Through(sim119, usa119, "Enact: Debt Office Long-Issuance Mandate", unasked, UsCongressDiagnostic.Day119, null, playerAnswer: _ => true);
                            DivisionRecord unaskedLast = unaskedVeto.Written.LastOrDefault(), unaskedFirst = unaskedLast?.OtherChamber;
                            int houseSeated = Congress.Sworn(usa119.ParliamentSeats), senateSworn = Congress.Sworn(usa119.SecondChamberSeats);
                            Check(unasked.IsEmpty && unaskedVeto.Draws == 0 && unaskedVeto.Sent && unaskedVeto.Law && unaskedLast != null && unaskedLast.Passed && unaskedLast.Required > 0 && unaskedFirst != null && unaskedFirst.Passed
                                  && unaskedFirst.Chamber == Congress.House && unaskedFirst.Sworn == houseSeated && unaskedLast.Chamber == Congress.Senate && unaskedLast.Sworn == senateSworn
                                  && unaskedProjection != null && unaskedProjection.OverrideCarries && unaskedProjection.First.Members == unaskedFirst.Sworn && unaskedProjection.First.Yea == unaskedFirst.Sworn
                                  && unaskedProjection.Second.Members == unaskedLast.Sworn && unaskedProjection.Second.Yea == unaskedLast.Sworn && !unaskedVeto.LostMarked,
                                F("(g) an uncontested bill (the Debt Office Long-Issuance Mandate, its concern moving nothing) passes both chambers unasked; vetoed by the player-president, it is overridden in both on the one rule the gate and the projection share - \"{0}\" - every member of the base for it: the House's {1} seated, the Senate's {2} sworn",
                                    unaskedLast?.Title, houseSeated, senateSworn));
                        }
                        finally { usa119.UsPresidentialElections.Remove(harris); usa119.Government = trumpOfRecord; usa119.PlayerPartyAbbrev = "DEM"; }
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw - ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n').Append(e.StackTrace).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                SimulationRandom.RestoreState(seedWas, drawsWas);
                UsVeto.ResetDrawCalls();
            }

            sb.Append(failures == 0 ? "=== UsVetoDiagnostic: the US veto in play holds to rule (c) as installed - the draw at the gate, rule (a), the player-president's answer, the override ===\n"
                                    : F("=== UsVetoDiagnostic: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        /// <summary>
        /// THE DONE-WHEN'S RUN: a US game from its start, 12 Mar 2024, to 20 Jan 2025, stepped day by day in the controller's order with no player decision, and
        /// BILLS PLANTED (DECLARED - the game tables no bill but the player's, so an idle run sends none to a president): on the 1st of each month REP, and on
        /// the 15th DEM, each tables a crime-and-justice bill and a labour bill moved toward its own end, and a tax programme bill (removing a levied tax for REP,
        /// levying an unlevied one for DEM - a budget vote, on which nothing is drawn), where the party may introduce that kind on the day (R-US7 (a)); each is
        /// voted through the game's own gate on its day (`Congress.Divide`, `SimulationManager.UsVetoGate`) and NOT applied - the run measures who sends and who
        /// vetoes, not what a bill does. It prints every planted bill, then the bills that reached the president and those vetoed, beside the record's count for
        /// the 118th (`UsVetoDrawCheck`'s reading of the extract) and the record's measures decided in the run's window - REPORTED, NOT ASSERTED.
        /// ⚠ STATED (the US-21 review's finding 8): the run drives the manager alone, with no controller, and only the controller holds an election on its polling
        /// day (`GameController.HoldElectionWithoutTheNight`, the film warm-up's) - so 5 Nov 2024 passes with no election held, a state play does not reach. Inside
        /// this window that moves no answer the run reports: the president of every planted day is Biden of record either way (the game's winner takes office on
        /// 20 Jan 2025, the window's end), and the 119th seated from 3 Jan 2025 is the record's by date (the manager's own seating of the record) - whatever a held election would have seated.
        /// </summary>
        public static void Run2024()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsVetoDiagnostic.Run2024 (PS-6 US-21): a US game from 12 Mar 2024 to 20 Jan 2025 with planted bills - who reached the president, and who was vetoed ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            DateTime start = WorldClock.StartDate(CountryId.USA), end = new DateTime(2025, 1, 20), new119 = new DateTime(2025, 1, 3);
            var rows = new List<(DateTime Day, string Author, string Kind, bool Sent, bool Vetoed, bool Law, string President, string Line)>();
            try
            {
                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        (SimulationManager sim, World world, Country usa) = UsCongressDiagnostic.OpenUsa(hosts, start, "DEM");
                        var none = new Dictionary<CountryId, PolicyDecision>();
                        foreach (Country c in world.Countries) { none[c.Id] = PolicyDecision.None(); }
                        int steps = 0, presidentsMissing = 0;
                        long drawsCounted = 0, drawsRecorded = 0;
                        UsVeto.ResetDrawCalls();
                        while (sim.CurrentDate < end && steps++ < 400)
                        {
                            DateTime day = sim.CurrentDate;
                            string author = day.Day == 1 ? "REP" : day.Day == 15 ? "DEM" : null;
                            if (author != null)
                            {
                                if (!Congress.PresidentOn(usa, day, out string president, out _, out _)) { presidentsMissing++; }
                                string mine = usa.PlayerPartyAbbrev;
                                usa.PlayerPartyAbbrev = author;
                                try
                                {
                                    foreach ((Congress.Kind kind, CabinetPortfolio? portfolio, string title, Func<Country, BillConcern> build) in Planted(usa, author))
                                    {
                                        if (!sim.PlayerMayIntroduce(CountryId.USA, kind, portfolio, out string refused)) { rows.Add((day, author, kind.ToString(), false, false, false, president, "not introduced - " + refused)); continue; }
                                        BillConcern concern = build(usa);
                                        if (concern == null || concern.IsEmpty) { rows.Add((day, author, kind.ToString(), false, false, false, president, "nothing to table")); continue; }
                                        long before = UsVeto.DrawCalls;
                                        Gated g = Through(sim, usa, title, concern, day, null);
                                        drawsCounted += UsVeto.DrawCalls - before;
                                        drawsRecorded += Drawn(g.Verdict);
                                        bool vetoed = g.Written.Any(r => r.Required > 0 && !r.Cloture);
                                        rows.Add((day, author, kind.ToString(), g.Sent, vetoed, g.Law, president, F("'{0}' - {1}{2}", title, Describe(g), vetoed ? " - \"" + g.Written.Last().Title + "\"" : string.Empty)));
                                    }
                                }
                                finally { usa.PlayerPartyAbbrev = mine; }
                            }
                            bool boundary = sim.AdvanceDay();
                            sim.AdvanceCountryDayTick(CountryId.USA);
                            if (boundary) { sim.AdvanceTurn(none); }
                        }
                        Check(sim.CurrentDate == end, F("the run stepped {0} days, {1} to {2}, in the controller's order (the day, the player's day tick, the boundary's turn)", steps - 1, Day(start), Day(sim.CurrentDate)));
                        Check(presidentsMissing == 0, F("a president of the day found on every planted day ({0} missing - a defect)", presidentsMissing));
                        Check(drawsCounted == drawsRecorded, F("every draw the gate counted is on a chamber's record of the bill: {0} counted, {1} recorded", drawsCounted, drawsRecorded));
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                }

                foreach (var r in rows) { sb.Append(F("    bill      {0} {1} {2}: {3}\n", Day(r.Day), r.Author, r.Kind, r.Line)); }
                sb.Append("    THE BILLS THAT REACHED THE PRESIDENT, AND THOSE VETOED (REPORTED, NOT ASSERTED; the bills planted, DECLARED):\n");
                foreach ((string label, Func<DateTime, bool> within) in new (string, Func<DateTime, bool>)[] { ("the 118th Congress (to 2025-01-02)", d => d < new119), ("the 119th Congress (2025-01-03 to 2025-01-19)", d => d >= new119) })
                {
                    var set = rows.Where(r => within(r.Day)).ToList();
                    sb.Append(F("    the game  {0}: {1} bills tabled, {2} introduced; {3} reached the president, {4} vetoed, {5} overridden, {6} signed\n", label, set.Count, set.Count(r => !r.Line.StartsWith("not introduced", StringComparison.Ordinal) && r.Line != "nothing to table"),
                        set.Count(r => r.Sent), set.Count(r => r.Vetoed), set.Count(r => r.Vetoed && r.Law), set.Count(r => r.Sent && !r.Vetoed)));
                }
                List<(int Congress, string Id, string DecisionDate, bool Vetoed)> record = UsVetoDrawCheck.RecordRows();
                bool InWindow((int Congress, string Id, string DecisionDate, bool Vetoed) m) =>
                    DateTime.TryParseExact(m.DecisionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d) && d >= start && d < end;
                var r118 = record.Where(m => m.Congress == 118).ToList();
                sb.Append(F("    record    the 118th Congress of record (the extract, every public law and every vetoed measure): {0} measures reached the president, {1} vetoed\n", r118.Count, r118.Count(m => m.Vetoed)));
                foreach (int congress in new[] { 118, 119 })
                {
                    var w = record.Where(m => m.Congress == congress && InWindow(m)).ToList();
                    sb.Append(F("    record    the {0}th of record decided in the run's window ({1} to {2}): {3} measures, {4} vetoed\n", congress, Day(start), Day(end.AddDays(-1)), w.Count, w.Count(m => m.Vetoed)));
                }
                Check(r118.Count > 0, "the record's 118th read from the extract the rule reads (UsVetoDrawCheck's reader)");
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw - ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n').Append(e.StackTrace).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                SimulationRandom.RestoreState(seedWas, drawsWas);
                UsVeto.ResetDrawCalls();
            }
            sb.Append(failures == 0 ? "=== UsVetoDiagnostic.Run2024: the run completed and is reported ===\n" : F("=== UsVetoDiagnostic.Run2024: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        // ------------------------------------------------------------------ the planted bills of the run
        private static IEnumerable<(Congress.Kind Kind, CabinetPortfolio? Portfolio, string Title, Func<Country, BillConcern> Build)> Planted(Country u, string author)
        {
            float toward = author == "REP" ? 1f : -1f;
            yield return (Congress.Kind.CrimeJustice, CabinetPortfolio.InteriorJustice, "Crime & Justice bill", c => ParliamentSystem.GetCrimeJusticeBillConcern(c, UsCongressDiagnostic.CrimeBill(c, author)));
            yield return (Congress.Kind.Labor, null, "Labor Market bill", c => ParliamentSystem.GetLaborBillConcern(c, new LaborPolicyBill
            {
                MinimumWage = c.MinimumWagePercentOfMedianBase - 10f * toward, PaidFamilyLeaveWeeks = c.PaidFamilyLeaveWeeksBase - 10f * toward, OvertimeRegulation = c.OvertimeRegulationBase,
                RetrainingProgram = c.RetrainingProgramBase, FamilyPolicy = c.FamilyPolicyBase, ImmigrationPolicy = c.ImmigrationPolicyBase,
            }));
            TaxLine tax = author == "REP" ? u.TaxLines.FirstOrDefault(t => t.IsImplemented) : u.TaxLines.FirstOrDefault(t => !t.IsImplemented);
            if (tax != null)
            {
                bool add = author != "REP";
                yield return (Congress.Kind.TaxProgram, CabinetPortfolio.FinanceTreasury, (add ? "Implement " : "Remove ") + tax.Type, c => ParliamentSystem.GetTaxProgramBillConcern(c, new TaxProgramBill { Type = tax.Type, IsAdd = add }));
            }
        }

        // ------------------------------------------------------------------ the gate, as play runs it
        private sealed class Gated
        {
            public bool Sent, Law, LostMarked;
            public long Draws;
            public Congress.Verdict Verdict;
            public List<DivisionRecord> Written = new List<DivisionRecord>();
        }

        /// <summary>A bill through the game's own gate on <paramref name="date"/>: `Congress.Divide` in the chamber its author introduces it in (as
        /// `ParliamentSystem.Divide` routes a bill through both chambers), then the US veto gate (`SimulationManager.UsVetoGate`, by reflection - it is the
        /// manager's own) where <paramref name="gate"/>; the draw planted by <paramref name="plant"/> where given; the player-president's answer
        /// <paramref name="playerAnswer"/>. Returns what it wrote, the draws it counted and whether the day was marked lost.</summary>
        private static Gated Through(SimulationManager sim, Country usa, string title, BillConcern concern, DateTime date, Func<UsVeto.Chamber, string, int, int> plant, bool gate = true, Func<DivisionRecord, bool> playerAnswer = null, bool amendment = false)
        {
            if (Gate == null) { throw new MissingMethodException("SimulationManager.UsVetoGate"); }
            var g = new Gated();
            int lastBefore = usa.Divisions.Entries.Count > 0 ? usa.Divisions.Entries.Last().Number : 0;
            usa.BillLostOn = DateTime.MinValue;
            long drawsBefore = UsVeto.DrawCalls;
            Func<DivisionRecord, bool> answerWas = sim.PlayerPresidentVetoes;
            sim.PlayerPresidentVetoes = playerAnswer;
            IDisposable planted = plant != null ? UsVeto.PlantIndex(plant) : null;
            try
            {
                DivisionRecord passage = Congress.Divide(usa, _ => title, concern, date, out bool sent, out _, out Congress.Verdict verdict, BillAxis.Fiscal, Congress.OriginOf(usa, concern, date));
                g.Sent = sent;
                g.Verdict = verdict;
                g.Law = gate ? (bool)Gate.Invoke(sim, new object[] { usa, passage, concern, sent, amendment }) : sent;   // every parameter given (MethodInfo.Invoke fills no optional one); the amendment flag as the law site passes it
            }
            finally { planted?.Dispose(); sim.PlayerPresidentVetoes = answerWas; }
            g.Draws = UsVeto.DrawCalls - drawsBefore;
            g.Written = usa.Divisions.Entries.Where(r => r.Number > lastBefore).ToList();
            g.LostMarked = usa.BillLostOn == date.Date;
            return g;
        }

        /// <summary>The concern a bill of <paramref name="author"/>'s carries - built with the author as the player's party (every player bill's author, §654),
        /// the player's party restored after.</summary>
        private static BillConcern Concern(Country usa, string author, Func<Country, BillConcern> build)
        {
            string mine = usa.PlayerPartyAbbrev;
            usa.PlayerPartyAbbrev = author;
            try { return build(usa); }
            finally { usa.PlayerPartyAbbrev = mine; }
        }

        /// <summary>The gate's own enumeration of a projected division's index pairs: `Congress.DrawCrossers` under every planted pair, the passing ones counted.</summary>
        private static (long Pass, long Total) GateEnumeration(Country usa, Congress.Division d, string pp, DateTime date)
        {
            string op = pp == "REP" ? "DEM" : "REP";
            PartyStance? p = null, o = null;
            foreach (PartyStance s in d.Stances) { if (s.Party.Abbrev == pp) { p = s; } else if (s.Party.Abbrev == op) { o = s; } }
            if (p == null || !p.Value.Measured || p.Value.Side >= 0) { return (d.Passed ? 1 : 0, 1); }
            bool opDraws = o != null && o.Value.Measured && o.Value.Side > 0 && o.Value.Seats > 0;
            var v = new UsVeto.ChamberVote
            {
                Procedure = d.Chamber == Congress.House ? UsVeto.Procedure.House : d.Cloture ? UsVeto.Procedure.SenateCloture : UsVeto.Procedure.SenateMajority,
                Yea0 = d.For, Nay0 = d.Against, Sworn = d.Sworn, PpSeats = p.Value.Seats, PpList = Congress.CellOf(d.Chamber, true), OpSeats = opDraws ? o.Value.Seats : 0, OpList = opDraws ? Congress.CellOf(d.Chamber, false) : null,
            };
            long pass = 0, total = 0;
            int np = v.PpList.Count, no = v.OpList?.Count ?? 1;
            for (int jp = 0; jp < np; jp++)
            {
                for (int jo = 0; jo < no; jo++)
                {
                    using (UsVeto.PlantIndex((chamber, party, n) => party == pp ? jp : jo))
                    {
                        Congress.Crossing c = Congress.DrawCrossers(v, CountryId.USA, false, Seed, date, "the projection's enumeration", d.Chamber, pp, op);
                        total++;
                        if (UsVeto.ChamberOutcome(v, c.Kp, c.Ko)) { pass++; }
                    }
                }
            }
            return (pass, total);
        }

        private sealed class Enumerated { public long Pass, Named, PassOverride, Total; }

        /// <summary>One chamber of a measure of V through the RUNTIME: the instrument's (B) chamber with the catalog's lists (by the cell it names), every index pair
        /// planted in turn and drawn through `Congress.DrawCrossers` (a RECORD BUDGET CLASS measure refused there, K = 0); the passes counted, the House's
        /// passes rule (a) names, and the passes the override carries on the passage's sides (the crossers counted only where they entered).</summary>
        private static Enumerated Enumerate(UsVetoDrawCheck.VetoPathMeasure m, UsVeto.ChamberVote bare, string ppCell, string opCell, int chamber)
        {
            var v = new UsVeto.ChamberVote { Procedure = bare.Procedure, Yea0 = bare.Yea0, Nay0 = bare.Nay0, Sworn = bare.Sworn, VicePresident = bare.VicePresident, PpSeats = bare.PpSeats, OpSeats = bare.OpSeats,
                PpList = ppCell == null ? null : Congress.ListOf(CellNamed(ppCell)), OpList = opCell == null ? null : Congress.ListOf(CellNamed(opCell)) };
            var r = new Enumerated();
            int np = v.PpList?.Count ?? 1, no = v.OpList?.Count ?? 1;
            bool pass0 = UsVeto.ChamberOutcome(v, 0, 0);
            int members = v.Yea0 + v.Nay0;
            for (int jp = 0; jp < np; jp++)
            {
                for (int jo = 0; jo < no; jo++)
                {
                    int kp, ko;
                    using (UsVeto.PlantIndex((ch, party, n) => party == m.PP ? jp : jo))
                    {
                        Congress.Crossing c = Congress.DrawCrossers(v, CountryId.USA, m.Budget, Seed, new DateTime(2020, 1, 1), m.Id, chamber, m.PP, m.OP);
                        kp = c.Kp; ko = c.Ko;
                    }
                    r.Total++;
                    bool pass = UsVeto.ChamberOutcome(v, kp, ko);
                    if (!pass) { continue; }
                    r.Pass++;
                    if (chamber == Congress.Senate || UsVeto.NamedByA(m.PpHouseSeats, m.PpHouseNay ? m.PpHouseSeats - kp : 0)) { r.Named++; }
                    int yea = v.Yea0 + (pass != pass0 ? kp - ko : 0);
                    if (UsVeto.OverrideCarries(yea, members - yea)) { r.PassOverride++; }
                }
            }
            return r;
        }

        private static UsVetoDefections.Cell CellNamed(string name) =>
            name == "HP" ? UsVetoDefections.HP : name == "HO" ? UsVetoDefections.HO : name == "SP" ? UsVetoDefections.SP : name == "SO" ? UsVetoDefections.SO : null;

        /// <summary>One key per party per chamber, every key of a chamber on the bill's day and title, the two chambers' keys apart by chamber alone.</summary>
        private static bool OneKeyPerChamber(Congress.Verdict v, DateTime day, string title, out string line)
        {
            var keys = new List<string>();
            bool ok = true;
            foreach (Congress.Division d in new[] { v.House, v.Senate })
            {
                Congress.Crossing c = d.Crossing;
                if (c == null) { continue; }
                string chamber = d.Chamber == Congress.Senate ? "Senate" : "House";
                foreach (string k in new[] { c.KeyPp, c.KeyOp })
                {
                    if (k == null) { continue; }
                    keys.Add(k);
                    ok &= k == UsVeto.Key(Seed, day, title, d.Chamber == Congress.Senate ? UsVeto.Chamber.Senate : UsVeto.Chamber.House, k == c.KeyPp ? c.PresidentsParty : c.OtherParty) && k.Contains("|" + chamber + "|");
                }
            }
            ok &= keys.Count == keys.Distinct(StringComparer.Ordinal).Count() && keys.Count >= 2;
            line = F("{0} keys on '{1}', each the chamber's own on {2}: {3}", keys.Count, title, Day(day), string.Join(" ; ", keys));
            return ok;
        }

        /// <summary>The draws a verdict records - one per party that drew in each chamber that voted.</summary>
        private static long Drawn(Congress.Verdict v)
        {
            long n = 0;
            foreach (Congress.Division d in new[] { v.House, v.Senate }) { if (d.Crossing != null) { n += (d.Crossing.KeyPp != null ? 1 : 0) + (d.Crossing.KeyOp != null ? 1 : 0); } }
            return n;
        }

        private static string Describe(Gated g)
        {
            string Chamber(Congress.Division d) => (d.Chamber == Congress.Senate ? "the Senate " : "the House ") + (d.Passed ? "passes" : "rejects") + F(" {0}-{1}", d.For, d.Against)
                + (d.Crossing == null ? string.Empty : F(" (drawn: {0} {1}{2}{3})", d.Crossing.PresidentsParty, d.Crossing.Kp, d.Crossing.KeyOp != null ? F(", {0} {1}", d.Crossing.OtherParty, d.Crossing.Ko) : string.Empty, d.Crossing.Matters ? ", entered" : ""));
            Congress.Division first = g.Verdict.First, second = g.Verdict.Second;
            return Chamber(first) + (first.Passed ? "; " + Chamber(second) : "; never reaches the other chamber") + (g.Sent ? (g.Written.Any(r => r.Required > 0 && !r.Cloture) ? (g.Law ? "; vetoed and overridden" : "; vetoed, the veto stands") : "; signed") : string.Empty);
        }

        private static bool SameRecords(List<DivisionRecord> a, List<DivisionRecord> b)
        {
            if (a.Count != b.Count) { return false; }
            for (int i = 0; i < a.Count; i++)
            {
                DivisionRecord x = a[i], y = b[i];
                if (x.Title != y.Title || x.Passed != y.Passed || x.Chamber != y.Chamber || x.Origin != y.Origin || x.Required != y.Required || x.Cloture != y.Cloture || x.Sworn != y.Sworn || x.Motion != y.Motion
                    || x.Reconciliation != y.Reconciliation || x.VicePresidentVote != y.VicePresidentVote || x.Alignment != y.Alignment || x.Sides.Count != y.Sides.Count) { return false; }
                for (int s = 0; s < x.Sides.Count; s++)
                {
                    if (x.Sides[s].Abbrev != y.Sides[s].Abbrev || x.Sides[s].Seats != y.Sides[s].Seats || x.Sides[s].Side != y.Sides[s].Side || x.Sides[s].Defectors != y.Sides[s].Defectors) { return false; }
                }
            }
            return true;
        }

        // ------------------------------------------------------------------ the instrument's document, the plan, git
        /// <summary>The DIAGNOSTIC section of US_VETO_DRAW.md: "- &lt;id&gt;: P(sent) = p/q; P(named) = p/q" per measure of V.</summary>
        private static Dictionary<string, (string Sent, string Named)> DiagnosticSection(string text)
        {
            var map = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
            text = text.Replace("\r\n", "\n");
            int at = text.IndexOf("### The DIAGNOSTIC", StringComparison.Ordinal);
            if (at < 0) { return map; }
            foreach (string line in text.Substring(at).Split('\n').Skip(1))
            {
                if (line.StartsWith("#", StringComparison.Ordinal)) { break; }
                System.Text.RegularExpressions.Match mt = System.Text.RegularExpressions.Regex.Match(line, @"^- (.+): P\(sent\) = (\S+); P\(named\) = (\S+)$");
                if (mt.Success) { map[mt.Groups[1].Value] = (mt.Groups[2].Value, mt.Groups[3].Value); }
            }
            return map;
        }

        private static string MirrorLine(string planLf, int blockEnd)
        {
            if (blockEnd < 0) { return "-"; }
            int nl = planLf.IndexOf('\n', blockEnd);
            foreach (string line in (nl < 0 ? string.Empty : planLf.Substring(nl + 1)).Split('\n')) { if (line.Trim().Length > 0) { return line; } }
            return "-";
        }

        private static string GitText(string arguments)
        {
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo("git", arguments) { WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(info))
                {
                    if (process == null) { return "git failed"; }
                    process.ErrorDataReceived += (s, e) => { };
                    process.BeginErrorReadLine();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(30000);
                    return process.ExitCode == 0 ? output.Trim() : "git failed";
                }
            }
            catch (Exception) { return "git failed"; }
        }

        private static string Rational(BigInteger n, BigInteger d)
        {
            if (d.IsZero) { return "UNDEFINED"; }
            BigInteger g = BigInteger.GreatestCommonDivisor(BigInteger.Abs(n), d);
            if (!g.IsZero && !g.IsOne) { n /= g; d /= g; }
            if (n.IsZero) { d = BigInteger.One; }
            return n.ToString(CultureInfo.InvariantCulture) + "/" + d.ToString(CultureInfo.InvariantCulture);
        }

        private static string Sha256(byte[] bytes) { using (var sha = SHA256.Create()) { return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2", CultureInfo.InvariantCulture))); } }
        private static string Short(string digest) => string.IsNullOrEmpty(digest) || digest.Length < 8 ? digest ?? "-" : digest.Substring(0, 8);
        private static string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
