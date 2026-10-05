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
    /// PS-5 item C4 (§770): THE GAME ELECTS POLAND'S PRESIDENT - checked on the game's own path. The days (the record's first vote, then the ruled
    /// practice's - E2); the first vote held by the day loop itself (`SimulationManager.AdvanceDay` into the record's day - the field, where each
    /// candidate stands, the gate, the count); the run-off held by the day loop on its day (B4's τ and the draft abstention); the president of record
    /// holding until the elected takes office, the game's president after; the veto reading the game's president, a planted winner unlike the record's
    /// telling the two apart; the contests round-tripping through the save's own serializer settings.
    /// <para>§772 (Elias's ruling E1): on <see cref="PresidentialReferenceWorld"/> - the fresh world taken straight to the run-off's day (how it is
    /// built and stepped, its own doc says) - the world seats the government of record and its poll carries that government's record; the stored
    /// candidate factors must be the fit (refit here every bar, the literal printed for a refit); the game's first round must reproduce the PKW's
    /// (in-sample); the acceptance: that world elects Nawrocki; and a later field held on it.</para>
    /// </summary>
    public static class PresidentialElectionLiveDiagnostic
    {
        /// <summary>E1: how close the game's first round on the reference world must come to the PKW's, per candidate, in points.</summary>
        private const double ReproductionTolerance = 0.005;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PresidentialElectionLiveDiagnostic (PS-5 item C4, §770; E1, §772): the game's own presidential election, on its own days ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var host = new GameObject("PresidentialElectionLiveDiagnostic");
            try
            {
                TwoRoundElection.Rule rule = TwoRoundElection.RuleOf(CountryId.Poland);

                // ---- the days, on a fresh world at the start (no contest held) ----
                using (Simulation.SimulationManager.EpochScope())
                {
                    WorldClock.ApplyStart(CountryId.Poland);
                    Country fresh = WorldFactory.CreateDefault().GetCountry(CountryId.Poland);
                    DateTime start = Simulation.SimulationManager.EpochDate;
                    bool next = PresidentialElection.TryNextFirstVote(fresh, start, out DateTime first, out DateTime termEnds, out string basis);
                    Check(next && first == new DateTime(2025, 5, 18) && termEnds == new DateTime(2025, 8, 6) && basis.StartsWith("the record's day", StringComparison.Ordinal),
                        F("the days: from the start ({0:yyyy-MM-dd}) the first vote falls on {1:yyyy-MM-dd}, Duda's term ending {2:yyyy-MM-dd} - {3}", start, first, termEnds, basis));
                }
                DateTime later = PresidentialElection.FirstVoteFor(rule, CountryId.Poland, new DateTime(2030, 8, 6), out string laterBasis);
                (DateTime _, DateTime opens2030, DateTime closes2030) = TwoRoundElection.TermOf(rule, new DateTime(2025, 8, 6));
                Check(opens2030 == new DateTime(2030, 4, 28) && closes2030 == new DateTime(2030, 5, 23) && later == new DateTime(2030, 5, 19) && later.DayOfWeek == DayOfWeek.Sunday
                      && closes2030.Subtract(later).TotalDays < 7 && laterBasis.StartsWith("RULED (E2)", StringComparison.Ordinal),
                    F("the days: the term ending 2030-08-06 - the window {0:yyyy-MM-dd} to {1:yyyy-MM-dd}, its last Sunday {2:yyyy-MM-dd} ({3})", opens2030, closes2030, later, laterBasis));

                // ---- the fresh world taken straight to the run-off's day (E1's reference): the first vote held by the day loop ----
                PresidentialReferenceWorld.Result world = PresidentialReferenceWorld.Hold(host);
                Country pl = world.Poland;
                PresidentialElection.Contest contest = world.Contest;
                Check(contest != null && contest.FirstVote == world.FirstVoteDay && pl.PresidentialElections.Count == 1,
                    F("the first vote held by AdvanceDay on {0:yyyy-MM-dd} - {1} contest(s)", world.FirstVoteDay, pl.PresidentialElections.Count));
                if (contest == null) { throw new InvalidOperationException("no contest was held"); }
                sb.Append("      the field: ").Append(string.Join("; ", contest.Field.Select(c => F("{0} ({1}) {2:0.00} %", c.Name, c.Party ?? "-", c.Share)))).Append('\n');
                foreach (string n in contest.NotStanding) { sb.Append("      not standing: ").Append(n).Append('\n'); }
                sb.Append("      ").Append(contest.Line).Append('\n');
                // §772's review: the reference world is the record's state on the eve, built as a new game builds one - the government of record that day
                var cabinet = world.GovernmentCabinet ?? new List<string>();
                Check(world.GovernmentPmParty == "KO" && cabinet.Count == 3 && cabinet.Contains("KO") && cabinet.Contains("TD") && cabinet.Contains("NL"),
                    F("the reference world seats the government of record on the eve - prime minister's party {0}, cabinet {1}", world.GovernmentPmParty ?? "none", string.Join("+", cabinet)));
                // §772's second review: and its poll carries that government's record - figures at both ends of the term, the term begun before the eve -
                // so the fit's poll is the one the ruling means, not a record-less one (an incomplete term reads neutral and shifts nothing)
                Check(world.Term.Unemployment.Complete && world.Term.Inflation.Complete && world.Term.TookOffice < world.FirstVoteDay.AddDays(-1),
                    "the reference world's poll carries the government's record - " + EconomicVote.Describe(world.Term));

                // ---- E1: the stored factors are the fit ----
                IReadOnlyList<PresidencyOfRecord.CandidateOfRecord> ofRecord = PresidencyOfRecord.CandidatesOf(CountryId.Poland, 2025);
                var drift = new List<string>();
                foreach (PresidencyOfRecord.CandidateOfRecord c in ofRecord.Where(c => c.BackingParty != null))
                {
                    double fit = PresidentialReferenceWorld.FittedFactor(world, c);
                    sb.Append(F("      FIT {0} {1} {2:R}  (poll {3:0.000000}, record {4:0.000000})\n", c.Surname, c.BackingParty, fit,
                        world.PollOnFirstVote.TryGetValue(c.BackingParty, out double poll) ? poll : double.NaN, PresidencyOfRecord.ShareOfRecord(CountryId.Poland, 2025, c.Surname)));
                    if (double.IsNaN(fit) || Math.Abs(c.Factor - fit) > 1e-6 * Math.Max(1.0, Math.Abs(fit))) { drift.Add(F("{0} stored {1} - the fit {2:0.0000000}", c.Surname, c.Factor, fit)); }
                }
                Check(drift.Count == 0, drift.Count == 0 ? "E1: the stored candidate factors are the fit (the PKW's first round over the game's poll on the reference world's first vote)"
                    : "E1: the stored candidate factors are NOT the fit - " + string.Join("; ", drift) + " (the FIT lines above are the refit)");

                // ---- E1: the whole field of record, its first round the PKW's ----
                string NameOf(PresidencyOfRecord.CandidateOfRecord c) => PresidencyOfRecord.NameOfRecord(CountryId.Poland, 2025, c.Surname);
                Check(contest.Field.Count == ofRecord.Count && ofRecord.All(c => contest.Field.Any(f => f.Name == NameOf(c) && f.Party == c.BackingParty)),
                    F("the field: every candidate of record ({0}), each standing for the roster party that backed it or for none", ofRecord.Count));
                double worst = 0.0;
                string worstName = null;
                foreach (PresidencyOfRecord.CandidateOfRecord c in ofRecord)
                {
                    PresidentialElection.Candidate f = contest.Field.FirstOrDefault(x => x.Name == NameOf(c));
                    double miss = f == null ? double.PositiveInfinity : Math.Abs(f.Share - 100.0 * PresidencyOfRecord.ShareOfRecord(CountryId.Poland, 2025, c.Surname));
                    if (miss > worst) { worst = miss; worstName = NameOf(c); }
                }
                Check(worst < ReproductionTolerance, F("E1: the reference world - the record's state on the eve, as a game started that day holds it - reproduces the PKW's first round (in-sample: the fit's own world) - the largest miss {0:0.0000} pp ({1})", worst, worstName ?? "none"));
                Check(Math.Abs(contest.Field.Sum(c => c.Share) - 100.0) < 1e-6 && contest.Field.All(c => c.Share > 0.0), F("the first round's shares sum to {0:0.000000} %", contest.Field.Sum(c => c.Share)));

                // ---- where each stands (E1: each at their own party's position) ----
                bool StandsAt(string name, string unit)
                {
                    PresidentialElection.Candidate f = contest.Field.FirstOrDefault(x => x.Name == name);
                    return f != null && f.Placed && f.PositionUnit == unit && PresidentialElection.TryPosition(CountryId.Poland, unit, out PresidentialElection.Point at)
                        && f.Galtan == at.Galtan && f.Nationalism == at.Nationalism && f.Eu == at.Eu;
                }
                bool partyOwn = ofRecord.Where(c => c.BackingParty != null && c.Surname != "HOŁOWNIA").All(c => StandsAt(NameOf(c), c.BackingParty));
                bool unplaced = ofRecord.Where(c => c.PositionUnit == null).All(c => contest.Field.Any(f => f.Name == NameOf(c) && !f.Placed));
                Check(StandsAt("Szymon Hołownia", "Polska 2050") && StandsAt("Adrian Zandberg", "Razem") && StandsAt("Grzegorz Braun", "Konf") && partyOwn && unplaced,
                    "where each stands (E1): Hołownia at Polska 2050's own row, Zandberg at Razem's, Braun at Konfederacja's; every party's candidate at its party's; the rest unplaced");

                // ---- the gate (B5): every roster party outside the field with a share is short of the signatures ----
                var shortOfGate = PartySystems.For(CountryId.Poland).Where(p => contest.Field.All(c => c.Party != p.Abbrev) && world.PollOnFirstVote.TryGetValue(p.Abbrev, out double s) && s > 0.0).ToList();
                Check(shortOfGate.Count > 0 && shortOfGate.All(p => contest.NotStanding.Any(n => n.StartsWith(PartySystems.ShortName(CountryId.Poland, p.Abbrev) + " - ", StringComparison.Ordinal) && n.Contains("signatures that nominate"))),
                    F("the gate (B5): {0} roster part(ies) outside the field of record with a share in the poll, each short of the {1:N0} signatures ({2}); the field of record is not put to it",
                        shortOfGate.Count, rule.NominationSignatures, string.Join(", ", shortOfGate.Select(p => p.Abbrev))));

                // ---- the count, the run-off and E1's acceptance ----
                PresidentialElection.Candidate top = contest.Field[0], second = contest.Field[1];
                Check(contest.RunOffA == top.Name && contest.RunOffB == second.Name && contest.RunOffOn == new DateTime(2025, 6, 1) && top.Share <= 50.0,
                    F("the count (Art. 127 ust. 4): no one past half - {0} and {1} meet on {2:yyyy-MM-dd}", contest.RunOffA, contest.RunOffB, contest.RunOffOn));
                IReadOnlyList<(string Candidate, long Votes)> runOffOfRecord = TwoRoundElection.RoundOfRecord(CountryId.Poland, 2025, 2, out _);
                double nawrockiOfRecord = 100.0 * runOffOfRecord.Where(v => v.Candidate.StartsWith("NAWROCKI", StringComparison.Ordinal)).Sum(v => v.Votes) / runOffOfRecord.Sum(v => v.Votes);
                double nawrockiGame = contest.RunOffA == "Karol Nawrocki" ? contest.RunOffShareA : 100.0 - contest.RunOffShareA;
                Check(world.RunOffStepped && contest.RunOffHeld && contest.Decided() && contest.Elected == "Karol Nawrocki" && contest.ElectedParty == "PiS",
                    F("E1's ACCEPTANCE: a fresh world taken straight to 1 June 2025 elects Nawrocki - {0:0.00} % of the two (the record {1:0.00}, a miss of {2:+0.00;-0.00}): {3}",
                        nawrockiGame, nawrockiOfRecord, nawrockiGame - nawrockiOfRecord, contest.Line));
                var firstRound = contest.Field.Select(c => (c.Name, c.Share, c.Placed ? new PresidentialElection.Point(c.Galtan, c.Nationalism, c.Eu) : (PresidentialElection.Point?)null)).ToList();
                PresidentialElection.Candidate fa = contest.Field.First(c => c.Name == contest.RunOffA), fb = contest.Field.First(c => c.Name == contest.RunOffB);
                double again = PresidentialElection.RunOffShare(firstRound, contest.RunOffA, contest.RunOffB, new PresidentialElection.Point(fa.Galtan, fa.Nationalism, fa.Eu), new PresidentialElection.Point(fb.Galtan, fb.Nationalism, fb.Eu),
                    PresidentialElection.TransferTau, PresidentialElection.AbstentionDraftMax, out double voting);
                Check(Math.Abs(again - contest.RunOffShareA) < 1e-9 && Math.Abs(voting - contest.RunOffVoting) < 1e-9 && voting < 100.0,
                    F("the run-off: {0} {1:0.00} % of the two, {2:0.00} % of the first round's voters voting (the draft abstention keeps the rest home) - τ {3}", contest.RunOffA, contest.RunOffShareA, contest.RunOffVoting, PresidentialElection.TransferTau));
                Check(contest.TakesOffice == new DateTime(2025, 8, 6), F("the elected ({0}, {1}) takes office {2:yyyy-MM-dd} - the day Duda's term ends", contest.Elected, contest.ElectedParty, contest.TakesOffice));

                // ---- who holds the office ----
                bool before = PresidentialElection.PresidentAt(CountryId.Poland, pl.PresidentialElections, new DateTime(2025, 8, 5), out PresidencyOfRecord.President stillDuda);
                bool after = PresidentialElection.PresidentAt(CountryId.Poland, pl.PresidentialElections, new DateTime(2025, 8, 6), out PresidencyOfRecord.President elected);
                Check(before && stillDuda.Name == "Andrzej Duda" && after && elected.Name == contest.Elected && elected.BackingParty == contest.ElectedParty && elected.Until == DateTime.MaxValue
                      && PresidentialElection.ElectedInGame(elected),
                    F("the office: {0} to 2025-08-05 (the record's), {1} ({2}) from 2025-08-06 (the game's)", stillDuda.Name, elected.Name, elected.BackingParty));

                // ---- the veto reads the game's president; a planted winner unlike the record's tells the two apart ----
                var sides = new List<DivisionSide>();
                foreach (KeyValuePair<string, int> kv in new Dictionary<string, int> { { "PiS", 194 }, { "KO", 157 }, { "TD", 65 }, { "NL", 26 }, { "Konf", 18 } })
                {
                    sides.Add(new DivisionSide { Abbrev = kv.Key, Seats = kv.Value, Side = kv.Key == contest.ElectedParty ? -1 : 1 });
                }
                PresidentialVeto.Outcome veto = PresidentialVeto.Decide(CountryId.Poland, pl.PresidentialElections, new DateTime(2025, 9, 1), PresidentialVeto.Act.OrdinaryStatute, sides);
                double electedRate = PresidentialVeto.RateOf(contest.Elected, out string electedBasis);
                Check(veto != null && veto.President == contest.Elected && veto.BackingParty == contest.ElectedParty && veto.AtRisk && veto.Risk == electedRate
                      && electedBasis.StartsWith("the president's own rate", StringComparison.Ordinal),
                    F("the veto on 2025-09-01 (F3): {0}'s backing party ({1}) against - at risk, at the record's own rate for a president the game re-elects, {2:0.0} % ({3})", veto?.President, veto?.BackingParty, electedRate * 100.0, electedBasis));
                var planted = new List<PresidentialElection.Contest>
                {
                    new PresidentialElection.Contest { FirstVote = new DateTime(2025, 5, 18), TakesOffice = new DateTime(2025, 8, 6), Elected = "Rafał Trzaskowski", ElectedParty = "KO", Line = "planted" },
                };
                var koAgainst = sides.Select(s => new DivisionSide { Abbrev = s.Abbrev, Seats = s.Seats, Side = s.Abbrev == "KO" ? -1 : 1 }).ToList();
                PresidentialVeto.Outcome plantedVeto = PresidentialVeto.Decide(CountryId.Poland, planted, new DateTime(2025, 9, 1), PresidentialVeto.Act.OrdinaryStatute, koAgainst);
                PresidentialVeto.Outcome recordOnly = PresidentialVeto.Decide(CountryId.Poland, null, new DateTime(2025, 9, 1), PresidentialVeto.Act.OrdinaryStatute, koAgainst);
                PresidentialElection.PresidentAt(CountryId.Poland, planted, new DateTime(2025, 8, 6), out PresidencyOfRecord.President plantedPresident);
                double pooledRate = PresidentialVeto.RateOf("Rafał Trzaskowski", out string pooledBasis);
                Check(plantedVeto != null && plantedVeto.President == "Rafał Trzaskowski" && plantedVeto.BackingParty == "KO" && plantedVeto.AtRisk && plantedVeto.Risk == pooledRate && pooledBasis.StartsWith("the pooled rate", StringComparison.Ordinal)
                      && PresidentialElection.ElectedInGame(plantedPresident) && recordOnly != null && recordOnly.President == "Karol Nawrocki" && !recordOnly.AtRisk && recordOnly.Risk == 0.0,
                    F("a planted game winner unlike the record's (F3): the office and the veto read {0} ({1}) - KO against, at risk at the pooled rate, {2:0.0} % ({3}); the record alone reads {4}, whose party votes for it - no risk",
                        plantedVeto?.President, plantedVeto?.BackingParty, pooledRate * 100.0, pooledBasis, recordOnly?.President));

                // ---- the next term's day, counted from the game's president ----
                bool nextTerm = PresidentialElection.TryNextFirstVote(pl, new DateTime(2025, 8, 7), out DateTime firstNext, out DateTime endsNext, out _);
                Check(nextTerm && firstNext == new DateTime(2030, 5, 19) && endsNext == new DateTime(2030, 8, 6), F("the next first vote after the oath: {0:yyyy-MM-dd} (the term ending {1:yyyy-MM-dd})", firstNext, endsNext));

                // ---- the save carries it ----
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(pl.PresidentialElections, PoliSim.Persistence.SaveGameService.BuildSettings());
                var back = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PresidentialElection.Contest>>(json, PoliSim.Persistence.SaveGameService.BuildSettings());
                Check(back != null && back.Count == 1 && back[0].Elected == contest.Elected && back[0].TakesOffice == contest.TakesOffice && back[0].Field.Count == contest.Field.Count && back[0].Line == contest.Line
                      && back[0].Field.Zip(contest.Field, (x, y) => x.Galtan == y.Galtan && x.Placed == y.Placed && x.PositionUnit == y.PositionUnit && x.Party == y.Party).All(same => same),
                    F("the contests round-trip through the save's own serializer settings (SaveGameService.BuildSettings, {0} characters), the positions with them", json.Length));

                // ---- §772's review (finding 7): a LATER field - the 2030 first vote held on the same world: one candidate per roster party clearing the
                // gate, the incumbent standing again within the term limit, the others unnamed, every one at FutureFactor (the party's poll, normalised)
                PresidentialElection.Contest laterField = PresidentialElection.HoldRounds(pl, firstNext, world.PollOnFirstVote);
                try
                {
                    long gateBase = PoliSim.Elections.Generated.PolishPresidentialReturns.Rounds.First(r => r.Year == 2025 && r.Round == 1).Valid;
                    var clears = PartySystems.For(CountryId.Poland).Where(p => world.PollOnFirstVote.TryGetValue(p.Abbrev, out double s) && s > 0.0 && s * gateBase >= rule.NominationSignatures).ToList();
                    double pollSum = clears.Sum(p => world.PollOnFirstVote[p.Abbrev]);
                    bool shape = laterField != null && laterField.FirstVote == firstNext && laterField.TakesOffice == endsNext && laterField.Field.Count == clears.Count
                        && laterField.Field.All(c => c.Party != null && c.PositionUnit == c.Party && (!PresidentialElection.TryPosition(CountryId.Poland, c.Party, out PresidentialElection.Point at) || (c.Placed && c.Galtan == at.Galtan)))
                        && laterField.Field.All(c => Math.Abs(c.Share - 100.0 * PresidentialElection.FutureFactor * world.PollOnFirstVote[c.Party] / (PresidentialElection.FutureFactor * pollSum)) < 1e-9);
                    bool names = laterField != null && laterField.Field.Any(c => c.Party == contest.ElectedParty && c.Name == contest.Elected)
                        && laterField.Field.Where(c => c.Party != contest.ElectedParty).All(c => c.Name == PartySystems.ShortName(CountryId.Poland, c.Party) + "'s candidate (" + firstNext.Year.ToString(CultureInfo.InvariantCulture) + ")");
                    var shortLater = PartySystems.For(CountryId.Poland).Where(p => !clears.Contains(p) && world.PollOnFirstVote.TryGetValue(p.Abbrev, out double s2) && s2 > 0.0).ToList();
                    bool gated = laterField != null && shortLater.Count > 0   // §772's second review: never a pass on an empty set
                        && shortLater.All(p => laterField.NotStanding.Any(n => n.StartsWith(PartySystems.ShortName(CountryId.Poland, p.Abbrev) + " - ", StringComparison.Ordinal) && n.Contains("signatures that nominate")));
                    Check(shape && names && gated, F("a later field (the {0:yyyy} first vote on the same world): {1} - the incumbent ({2}) again within the term limit, the rest unnamed, every share the party's poll at the future factor; not standing: {3}",
                        firstNext, laterField == null ? "none held" : string.Join("; ", laterField.Field.Select(c => F("{0} ({1}) {2:0.00} %", c.Name, c.Party, c.Share))), contest.Elected,
                        laterField == null ? "-" : string.Join("; ", laterField.NotStanding)));
                }
                finally { if (laterField != null) { pl.PresidentialElections.Remove(laterField); } }
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
