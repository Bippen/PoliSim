using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-5 item C4 (§770): THE GAME ELECTS POLAND'S PRESIDENT - checked on the game's own path. The days (the record's first vote, then the
    /// premise's); the first vote held by the day loop itself (`SimulationManager.AdvanceDay` into the record's day - the field, where each candidate
    /// stands, the gate, the count); the run-off held by the day loop on its day (B4's τ and the draft abstention); the president of record holding
    /// until the elected takes office, the game's president after; the veto reading the game's president, a planted winner unlike the record's
    /// telling the two apart; the contests round-tripping through the save's own serializer settings. Each round's day is stepped from an epoch set
    /// on its eve, so one world day is simulated per round.
    /// </summary>
    public static class PresidentialElectionLiveDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PresidentialElectionLiveDiagnostic (PS-5 item C4, §770): the game's own presidential election, on its own days ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var host = new GameObject("PresidentialElectionLiveDiagnostic");
            try
            {
                using (Simulation.SimulationManager.EpochScope())
                {
                    WorldClock.ApplyStart(CountryId.Poland);
                    World world = WorldFactory.CreateDefault();
                    Country pl = world.GetCountry(CountryId.Poland);
                    TwoRoundElection.Rule rule = TwoRoundElection.RuleOf(CountryId.Poland);

                    // ---- the days ----
                    DateTime start = Simulation.SimulationManager.EpochDate;
                    bool next = PresidentialElection.TryNextFirstVote(pl, start, out DateTime first, out DateTime termEnds, out string basis);
                    Check(next && first == new DateTime(2025, 5, 18) && termEnds == new DateTime(2025, 8, 6) && basis.StartsWith("the record's day", StringComparison.Ordinal),
                        F("the days: from the start ({0:yyyy-MM-dd}) the first vote falls on {1:yyyy-MM-dd}, Duda's term ending {2:yyyy-MM-dd} - {3}", start, first, termEnds, basis));
                    DateTime later = PresidentialElection.FirstVoteFor(rule, CountryId.Poland, new DateTime(2030, 8, 6), out string laterBasis);
                    (DateTime _, DateTime opens2030, DateTime closes2030) = TwoRoundElection.TermOf(rule, new DateTime(2025, 8, 6));
                    Check(opens2030 == new DateTime(2030, 4, 28) && closes2030 == new DateTime(2030, 5, 23) && later == new DateTime(2030, 5, 19) && later.DayOfWeek == DayOfWeek.Sunday
                          && closes2030.Subtract(later).TotalDays < 7 && laterBasis.StartsWith("PREMISE, DECLARED", StringComparison.Ordinal),
                        F("the days: the term ending 2030-08-06 - the window {0:yyyy-MM-dd} to {1:yyyy-MM-dd}, its last Sunday {2:yyyy-MM-dd} ({3})", opens2030, closes2030, later, laterBasis));

                    // ---- the first vote, held by the day loop ----
                    Simulation.SimulationManager.SetEpoch(new DateTime(2025, 5, 17));
                    var sim = host.AddComponent<Simulation.SimulationManager>();
                    sim.SetWorld(world);
                    sim.PlayerCountryId = CountryId.Poland;
                    sim.AdvanceDay();
                    PresidentialElection.Contest contest = pl.PresidentialElections.FirstOrDefault();
                    Check(sim.CurrentDate == new DateTime(2025, 5, 18) && pl.PresidentialElections.Count == 1 && contest != null && contest.FirstVote == sim.CurrentDate,
                        F("the first vote held by AdvanceDay on {0:yyyy-MM-dd} - {1} contest(s)", sim.CurrentDate, pl.PresidentialElections.Count));
                    if (contest == null) { throw new InvalidOperationException("no contest was held"); }
                    sb.Append("      the field: ").Append(string.Join("; ", contest.Field.Select(c => F("{0} ({1}) {2:0.00} %", c.Name, c.Party, c.Share)))).Append('\n');
                    foreach (string n in contest.NotStanding) { sb.Append("      not standing: ").Append(n).Append('\n'); }
                    sb.Append("      ").Append(contest.Line).Append('\n');
                    var expected = new Dictionary<string, string> { { "PiS", "Karol Nawrocki" }, { "KO", "Rafał Trzaskowski" }, { "TD", "Szymon Hołownia" }, { "NL", "Magdalena Biejat" }, { "Konf", "Sławomir Mentzen" } };
                    Check(contest.Field.Count == expected.Count && contest.Field.All(c => expected.TryGetValue(c.Party, out string who) && who == c.Name),
                        "the field: the record's candidates of 2025, each standing for the roster party whose standing it inherits");
                    PresidentialElection.Candidate holownia = contest.Field.FirstOrDefault(c => c.Party == "TD");
                    bool p2050 = PresidentialElection.TryPosition(CountryId.Poland, "Polska 2050", out PresidentialElection.Point at2050);
                    Check(holownia != null && holownia.Placed && holownia.PositionUnit == "Polska 2050" && p2050 && holownia.Galtan == at2050.Galtan && holownia.Nationalism == at2050.Nationalism && holownia.Eu == at2050.Eu
                          && contest.Field.Where(c => c.Party != "TD").All(c => c.Placed && c.PositionUnit == c.Party),
                        F("where each stands (B4's placement): Hołownia at Polska 2050's own row ({0:0.00}/{1:0.00}/{2:0.00}), not TD's joint position; every other at its party's", holownia?.Galtan, holownia?.Nationalism, holownia?.Eu));
                    Check(Math.Abs(contest.Field.Sum(c => c.Share) - 100.0) < 1e-6 && contest.Field.All(c => c.Share > 0.0), F("the first round's shares sum to {0:0.000000} %", contest.Field.Sum(c => c.Share)));
                    Check(contest.NotStanding.Any(n => n.StartsWith("Grzegorz Braun", StringComparison.Ordinal)) && contest.NotStanding.Any(n => n.StartsWith("Adrian Zandberg", StringComparison.Ordinal)),
                        "not standing: Braun and Zandberg - no roster party backs either, no standing to inherit");
                    // the gate (B5): the same prediction the day loop read - every roster party with a share that fields no one is short of the signatures
                    NationalElection.TryPredictShares(CountryId.Poland, out Dictionary<string, double> predicted, EconomicVote.RecordOverTerm(pl, contest.FirstVote, out _), on: contest.FirstVote);
                    var shortOfGate = PartySystems.For(CountryId.Poland).Where(p => contest.Field.All(c => c.Party != p.Abbrev) && predicted.TryGetValue(p.Abbrev, out double s) && s > 0.0).ToList();
                    Check(shortOfGate.All(p => contest.NotStanding.Any(n => n.StartsWith(PartySystems.ShortName(CountryId.Poland, p.Abbrev) + " - ", StringComparison.Ordinal) && n.Contains("signatures that nominate")))
                          && contest.Field.All(c => predicted[c.Party] * PoliSim.Elections.Generated.PolishPresidentialReturns.Rounds.First(r => r.Year == 2025 && r.Round == 1).Valid >= rule.NominationSignatures),
                        F("the gate (B5): {0} roster part(ies) with a predicted share field no one, each short of the {1:N0} signatures ({2}); every candidate's party clears it",
                            shortOfGate.Count, rule.NominationSignatures, string.Join(", ", shortOfGate.Select(p => p.Abbrev))));
                    PresidentialElection.Candidate top = contest.Field[0], second = contest.Field[1];
                    bool outright = top.Share > 50.0;
                    Check(outright ? contest.Elected == top.Name : contest.RunOffA == top.Name && contest.RunOffB == second.Name && contest.RunOffOn == new DateTime(2025, 6, 1) && !contest.Decided(),
                        outright ? F("the count: {0} elected outright with {1:0.00} %", top.Name, top.Share) : F("the count (Art. 127 ust. 4): no one past half - {0} and {1} meet on {2:yyyy-MM-dd}", contest.RunOffA, contest.RunOffB, contest.RunOffOn));

                    // ---- the run-off, held by the day loop on its day ----
                    if (!outright)
                    {
                        Simulation.SimulationManager.SetEpoch(new DateTime(2025, 5, 31));
                        var sim2 = host.AddComponent<Simulation.SimulationManager>();
                        sim2.SetWorld(world);
                        sim2.PlayerCountryId = CountryId.Poland;
                        sim2.AdvanceDay();
                        Check(contest.RunOffHeld && contest.Decided() && sim2.CurrentDate == contest.RunOffOn && pl.PresidentialElections.Count == 1,
                            F("the run-off held by AdvanceDay on {0:yyyy-MM-dd}: {1}", sim2.CurrentDate, contest.Line));
                        // the arithmetic is B4's: recomputed here from the contest's own first round, at the runtime's τ and draft abstention
                        var firstRound = contest.Field.Select(c => (c.Name, c.Share, c.Placed ? new PresidentialElection.Point(c.Galtan, c.Nationalism, c.Eu) : (PresidentialElection.Point?)null)).ToList();
                        PresidentialElection.Candidate fa = contest.Field.First(c => c.Name == contest.RunOffA), fb = contest.Field.First(c => c.Name == contest.RunOffB);
                        double again = PresidentialElection.RunOffShare(firstRound, contest.RunOffA, contest.RunOffB, new PresidentialElection.Point(fa.Galtan, fa.Nationalism, fa.Eu), new PresidentialElection.Point(fb.Galtan, fb.Nationalism, fb.Eu),
                            PresidentialElection.TransferTau, PresidentialElection.AbstentionDraftMax, out double voting);
                        Check(Math.Abs(again - contest.RunOffShareA) < 1e-9 && Math.Abs(voting - contest.RunOffVoting) < 1e-9 && voting < 100.0,
                            F("the run-off: {0} {1:0.00} % of the two, {2:0.00} % of the first round's voters voting (the draft abstention keeps the rest home) - τ {3}", contest.RunOffA, contest.RunOffShareA, contest.RunOffVoting, PresidentialElection.TransferTau));
                    }
                    Check(contest.TakesOffice == new DateTime(2025, 8, 6) && contest.ElectedParty != null, F("the elected ({0}, {1}) takes office {2:yyyy-MM-dd} - the day Duda's term ends", contest.Elected, contest.ElectedParty, contest.TakesOffice));

                    // ---- who holds the office ----
                    bool before = PresidentialElection.PresidentAt(CountryId.Poland, pl.PresidentialElections, new DateTime(2025, 8, 5), out PresidencyOfRecord.President stillDuda);
                    bool after = PresidentialElection.PresidentAt(CountryId.Poland, pl.PresidentialElections, new DateTime(2025, 8, 6), out PresidencyOfRecord.President elected);
                    Check(before && stillDuda.Name == "Andrzej Duda" && after && elected.Name == contest.Elected && elected.BackingParty == contest.ElectedParty && elected.Until == DateTime.MaxValue,
                        F("the office: {0} to 2025-08-05 (the record's), {1} ({2}) from 2025-08-06 (the game's)", stillDuda.Name, elected.Name, elected.BackingParty));
                    PresidencyOfRecord.TryAt(CountryId.Poland, new DateTime(2025, 8, 6), out PresidencyOfRecord.President ofRecord);
                    Check(ofRecord.Name == "Karol Nawrocki", "the record still reads Nawrocki from 2025-08-06 - the game's election, not the record, decides the game's office");

                    // ---- the veto reads the game's president ----
                    var sides = new List<DivisionSide>();
                    foreach (KeyValuePair<string, int> kv in new Dictionary<string, int> { { "PiS", 194 }, { "KO", 157 }, { "TD", 65 }, { "NL", 26 }, { "Konf", 18 } })
                    {
                        sides.Add(new DivisionSide { Abbrev = kv.Key, Seats = kv.Value, Side = kv.Key == contest.ElectedParty ? -1 : 1 });
                    }
                    PresidentialVeto.Outcome veto = PresidentialVeto.Decide(CountryId.Poland, pl.PresidentialElections, new DateTime(2025, 9, 1), PresidentialVeto.Act.OrdinaryStatute, sides);
                    PresidentialVeto.Outcome recordOnly = PresidentialVeto.Decide(CountryId.Poland, null, new DateTime(2025, 9, 1), PresidentialVeto.Act.OrdinaryStatute, sides);
                    Check(veto != null && veto.President == contest.Elected && veto.BackingParty == contest.ElectedParty && veto.Vetoed && recordOnly != null && recordOnly.President == "Karol Nawrocki",
                        F("the veto on 2025-09-01: {0}'s backing party ({1}) against - vetoed by {2}; the record alone would read {3}", veto?.President, veto?.BackingParty, veto?.President, recordOnly?.President));

                    // a planted contest the game's winner differs from the record's in - the office and the veto must read the game's, not the record's
                    var planted = new List<PresidentialElection.Contest>
                    {
                        new PresidentialElection.Contest { FirstVote = new DateTime(2025, 5, 18), TakesOffice = new DateTime(2025, 8, 6), Elected = "Rafał Trzaskowski", ElectedParty = "KO", Line = "planted" },
                    };
                    var koAgainst = sides.Select(s => new DivisionSide { Abbrev = s.Abbrev, Seats = s.Seats, Side = s.Abbrev == "KO" ? -1 : 1 }).ToList();
                    PresidentialVeto.Outcome plantedVeto = PresidentialVeto.Decide(CountryId.Poland, planted, new DateTime(2025, 9, 1), PresidentialVeto.Act.OrdinaryStatute, koAgainst);
                    PresidentialElection.PresidentAt(CountryId.Poland, planted, new DateTime(2025, 8, 6), out PresidencyOfRecord.President plantedPresident);
                    Check(plantedVeto != null && plantedVeto.President == "Rafał Trzaskowski" && plantedVeto.BackingParty == "KO" && plantedVeto.Vetoed
                          && PresidentialElection.ElectedInGame(plantedPresident) && PresidentialElection.ElectedInGame(elected),
                        F("a planted game winner unlike the record's: the office and the veto read {0} ({1}), not the record's Nawrocki - KO against, vetoed", plantedVeto?.President, plantedVeto?.BackingParty));

                    // ---- the next term's day, counted from the game's president ----
                    bool nextTerm = PresidentialElection.TryNextFirstVote(pl, new DateTime(2025, 8, 7), out DateTime firstNext, out DateTime endsNext, out _);
                    Check(nextTerm && firstNext == new DateTime(2030, 5, 19) && endsNext == new DateTime(2030, 8, 6), F("the next first vote after the oath: {0:yyyy-MM-dd} (the term ending {1:yyyy-MM-dd})", firstNext, endsNext));

                    // ---- the save carries it ----
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(pl.PresidentialElections, PoliSim.Persistence.SaveGameService.BuildSettings());
                    var back = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PresidentialElection.Contest>>(json, PoliSim.Persistence.SaveGameService.BuildSettings());
                    Check(back != null && back.Count == 1 && back[0].Elected == contest.Elected && back[0].TakesOffice == contest.TakesOffice && back[0].Field.Count == contest.Field.Count && back[0].Line == contest.Line && back[0].Field.Zip(contest.Field, (x, y) => x.Galtan == y.Galtan && x.Placed == y.Placed && x.PositionUnit == y.PositionUnit).All(same => same),
                        F("the contests round-trip through the save's own serializer settings (SaveGameService.BuildSettings, {0} characters), the positions with them", json.Length));
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }
            finally { UnityEngine.Object.DestroyImmediate(host); }

            sb.Append(failures == 0 ? "=== PresidentialElectionLiveDiagnostic: the game elects its president ===" : F("=== PresidentialElectionLiveDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
