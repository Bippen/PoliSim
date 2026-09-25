using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Simulation;
using PoliSim.UI;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **PF-1's assertion (ruled 2026-09-17, `COMPLETED.md` §525): the cached chamber answer is the uncached one.**
    ///
    /// <para><b>THE ENUMERATION.</b> On the default world, for Italy (a fourteen-party chamber, whose coalition search
    /// was one of the two that made a draw cost seconds) and Sweden (a small one): the first two tax lines' and the first
    /// welfare program's implement-or-remove verdicts and a budget-shaped concern with a cut, each asked of one
    /// <see cref="ChamberVerdicts"/> and of the model directly - verdict against verdict, and stances party by party
    /// through this check's OWN comparison (seats, side, measured, alignment to the bit, every reason), not the cache's.
    /// Asked six times with the state moved between: (1) fresh, every question a miss; (2) again, every question a hit,
    /// timed; (3) again with <see cref="ChamberVerdicts.VerifyHits"/> on, so the cache's own drift verification runs; (4) a
    /// DRAFT changed - the budget concern's magnitude moved, so its answers are asked again; (5) the CHAMBER changed -
    /// seats moved from the largest party to the smallest seated one; (6) the PLAYER'S PARTY changed. After (2) the
    /// hits must have risen with no new miss, after (4) a miss must have followed, after (5) and (6) the cache must have
    /// dropped its entries. The seats and the party are restored. Then (7), on a world of its own, WHO GOVERNS changes with nothing else moved (<see cref="GovernmentChanges"/>, §642).</para>
    ///
    /// <para>⚠ <b>France is not asked here, on cost</b> (ninety milliseconds a question, and this runs on every cheap
    /// bar); the code under test does not branch on the country. France is verified where it is drawn: every film and
    /// dry film turns <see cref="ChamberVerdicts.VerifyHits"/> on for each captured frame. The two mutations that proved
    /// this check fails - the seats, then the player's party, taken out of the cache's key - are §525's.</para>
    /// </summary>
    public static class ChamberVerdictCacheCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var failures = new List<string>();
            var sb = new StringBuilder("=== ChamberVerdictCacheCheck: the cached chamber answer against the model's own ===\n");
            bool verifyWas = ChamberVerdicts.VerifyHits;
            int driftsBefore = ChamberVerdicts.Drifts;
            World world = WorldFactory.CreateDefault();

            foreach (CountryId id in new[] { CountryId.Italy, CountryId.Sweden })
            {
                Country country = world.GetCountry(id);
                string partyWas = country.PlayerPartyAbbrev;
                var seatsWas = new Dictionary<string, int>(country.ParliamentSeats);
                if (string.IsNullOrEmpty(country.PlayerPartyAbbrev)) { country.PlayerPartyAbbrev = Largest(country); }

                var cache = new ChamberVerdicts();
                int compared = 0;

                void Compare(string step, string what, BillConcern concern)
                {
                    bool cachedVerdict = cache.WouldPass(country, concern);
                    IReadOnlyList<PartyStance> cachedStances = cache.Stances(country, concern);
                    bool modelVerdict = ParliamentSystem.WouldBillPass(country, concern);
                    List<PartyStance> modelStances = StanceModel.Stances(country, concern);
                    compared++;
                    if (cachedVerdict != modelVerdict)
                    {
                        failures.Add($"{id} {step} {what}: the cache says {(cachedVerdict ? "PASS" : "FAIL")}, the model {(modelVerdict ? "PASS" : "FAIL")}");
                    }

                    string difference = Differ(cachedStances, modelStances);
                    if (difference != null) { failures.Add($"{id} {step} {what}: {difference}"); }
                }

                List<(string What, BillConcern Concern)> Questions(float budgetMagnitude)
                {
                    var list = new List<(string, BillConcern)>();
                    foreach (TaxLine line in country.TaxLines.Take(2))
                    {
                        float d = ParliamentSystem.GetTaxProgramBillDirection(country, new TaxProgramBill { Type = line.Type, IsAdd = !line.IsImplemented });
                        list.Add(("tax " + line.Type, BillConcern.FromLegacy(d, BillAxis.Fiscal)));
                    }

                    foreach (WelfareProgram program in country.WelfarePrograms.Take(1))
                    {
                        float d = ParliamentSystem.GetWelfareProgramBillDirection(country, new WelfareProgramBill { Type = program.Type, IsAdd = !program.IsImplemented });
                        list.Add(("welfare " + program.Type, BillConcern.FromLegacy(d, BillAxis.Fiscal)));
                    }

                    var budget = new BillConcern { Direction = budgetMagnitude }.Add(StanceAxis.SpendVsTax, budgetMagnitude);
                    budget.Cuts.Add((country.SpendingLines[0].Category, null, 0.2f));
                    list.Add(("budget draft", budget));
                    return list;
                }

                ChamberVerdicts.VerifyHits = false;
                var clock = Stopwatch.StartNew();
                foreach ((string what, BillConcern concern) in Questions(3f)) { cache.WouldPass(country, concern); cache.Stances(country, concern); }
                double missMs = clock.Elapsed.TotalMilliseconds;
                foreach ((string what, BillConcern concern) in Questions(3f)) { Compare("fresh", what, concern); }
                int missesFresh = cache.Misses, hitsFresh = cache.Hits;

                clock.Restart();
                foreach ((string what, BillConcern concern) in Questions(3f)) { cache.WouldPass(country, concern); cache.Stances(country, concern); }
                double hitMs = clock.Elapsed.TotalMilliseconds;
                if (cache.Hits <= hitsFresh) { failures.Add($"{id}: asked again, the cache took no hits ({cache.Hits} after {hitsFresh}) - it is not caching"); }
                if (cache.Misses != missesFresh) { failures.Add($"{id}: asked again, the cache missed {cache.Misses - missesFresh} time(s) - an unchanged question went to the chamber again"); }

                ChamberVerdicts.VerifyHits = true;
                foreach ((string what, BillConcern concern) in Questions(3f)) { Compare("again, verified", what, concern); }
                ChamberVerdicts.VerifyHits = false;

                int missesBeforeDraft = cache.Misses;
                foreach ((string what, BillConcern concern) in Questions(-4f)) { Compare("draft moved", what, concern); }
                if (cache.Misses == missesBeforeDraft) { failures.Add($"{id}: the draft moved and nothing was asked again - the draft's concern is not in the key"); }

                string largest = Largest(country);
                string smallest = country.ParliamentSeats.Where(kv => kv.Value > 0 && kv.Key != largest).OrderBy(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).First().Key;
                int moved = Math.Max(1, country.ParliamentSeats[largest] / 3);
                country.ParliamentSeats[largest] -= moved;
                country.ParliamentSeats[smallest] += moved;
                int invalidationsBefore = cache.Invalidations;
                foreach ((string what, BillConcern concern) in Questions(3f)) { Compare("seats moved", what, concern); }
                if (cache.Invalidations == invalidationsBefore) { failures.Add($"{id}: {moved} seats moved from {largest} to {smallest} and the cache kept its entries"); }

                string other = country.ParliamentSeats.Where(kv => kv.Value > 0 && kv.Key != country.PlayerPartyAbbrev).OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).First().Key;
                string playerBefore = country.PlayerPartyAbbrev;
                country.PlayerPartyAbbrev = other;
                invalidationsBefore = cache.Invalidations;
                foreach ((string what, BillConcern concern) in Questions(3f)) { Compare("player's party moved", what, concern); }
                if (cache.Invalidations == invalidationsBefore) { failures.Add($"{id}: the player's party moved from {playerBefore} to {other} and the cache kept its entries"); }

                country.ParliamentSeats.Clear();
                foreach (KeyValuePair<string, int> kv in seatsWas) { country.ParliamentSeats[kv.Key] = kv.Value; }
                country.PlayerPartyAbbrev = partyWas;

                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,-7} {1,2} comparisons, {2} hits, {3} misses, {4} invalidations; four questions asked of the chamber {5:F1} ms, the same four from the cache {6:F2} ms\n",
                    id, compared, cache.Hits, cache.Misses, cache.Invalidations, missMs, hitMs));
            }

            GovernmentChanges(failures, sb);

            ChamberVerdicts.VerifyHits = verifyWas;
            int drifts = ChamberVerdicts.Drifts - driftsBefore;
            if (drifts > 0) { failures.Add($"{drifts} drift(s) the cache's own verification caught on a hit"); }

            if (failures.Count == 0)
            {
                sb.Append("    CLEAN - every cached verdict and every cached stance is the model's own, through a draft, a seat, a party and a government change.\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
                return;
            }

            foreach (string failure in failures.Take(20)) { sb.Append("    ⚠ ").Append(failure).Append('\n'); }
            sb.Append($"    {failures.Count} failure(s). ⚠ A cached chamber answer that differs from the model's is a screen that lies about a vote.\n");
            Debug.LogError(sb.ToString());
            CheckExit.Finish(1);
        }

        /// <summary>
        /// §642 (the ultrareview of PR #1): THE CACHE UNDER A CHANGE OF WHO GOVERNS - no seat and no party moved. Sweden at its start, on a live
        /// SimulationManager, through the player's own verbs; the premises (a government SD supports, KD a partner in it) are asserted, not assumed. One
        /// cache is asked every question before and after each change with its own verification off, so it cannot heal itself, and is held against the
        /// model on every verdict and on every party's side, alignment and reasons - the tuple the seat map draws. (A) The player SD WITHDRAWS: its pull
        /// on a government bill turns from the support's cohesion to the bloc's line, so its seats move on the map; the check counts the verdicts that
        /// change with it too, and follows the withdrawal's own consequence - SD moves no confidence, the week runs out and the Speaker's round replaces the
        /// government - until verdicts move. (B) On a world of its own, the player KD LEAVES the cabinet. The compass's cabinet
        /// (`GovernmentFormation.Cabinet`) is the votes' cabinet at every step. (C) The guard: no source writes a record's cabinet, support, prime
        /// minister or caretaker standing outside its own methods, which bump its version. Proved failing with the government taken out of the cache's
        /// key and the compass back on the seat formation (§642). The random streams are restored after, so no later check starts elsewhere.
        /// </summary>
        private static void GovernmentChanges(List<string> failures, StringBuilder sb)
        {
            using IDisposable epoch = SimulationManager.EpochScope();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            var line = new StringBuilder("    Sweden, who governs alone");
            var clock = Stopwatch.StartNew();
            try
            {
                OnSweden("ChamberVerdictCacheCheck.withdrawal", failures, (sim, sweden, world) => Withdrawal(sim, sweden, world, failures, line));
                OnSweden("ChamberVerdictCacheCheck.leave", failures, (sim, sweden, world) => Leaving(sim, sweden, world, failures, line));
                try { Guard(failures, line); }
                catch (Exception e) { failures.Add($"the who-governs guard threw - {e.GetType().Name}: {e.Message}"); }
            }
            finally { SimulationRandom.RestoreState(seedWas, drawsWas); }
            sb.Append(line).Append(string.Format(CultureInfo.InvariantCulture, "; this part {0:F1} s of the bar\n", clock.Elapsed.TotalSeconds));
        }

        private static void Withdrawal(SimulationManager sim, Country sweden, World world, List<string> failures, StringBuilder line)
        {
            PoliSim.Elections.GovernmentRecord start = sweden.Government;
            if (start == null || !start.Support.Contains("SD"))
            {
                failures.Add($"Sweden (who governs): the premise failed - the start's government is {Describe(start)}, not one SD supports");
                return;
            }
            List<(string Law, BillConcern Concern)> bills = GovernmentBills(world, sweden);
            sweden.PlayerPartyAbbrev = "SD";
            var cache = new ChamberVerdicts();
            ChamberVerdicts.VerifyHits = false;
            Answers atStart = Ask(cache, sweden, bills, "at the start", failures);

            // The concern's AUTHOR is in the key (§642, the second reading): the same moves as a member's bill - SD, outside the cabinet, is no
            // government here - where the model answers them apart, asked of the same warm cache after the government's.
            int authorPairs = 0;
            foreach ((string law, BillConcern government) in bills)
            {
                BillConcern member = new BillConcern { Direction = government.Direction, GovernmentAuthored = false };
                foreach (KeyValuePair<StanceAxis, float> move in government.Moves) { member.Add(move.Key, move.Value); }
                member.Cuts.AddRange(government.Cuts);
                if (Differ(StanceModel.Stances(sweden, member), StanceModel.Stances(sweden, government)) == null) { continue; }
                authorPairs++;
                bool cachedMember = cache.WouldPass(sweden, member);
                string difference = Differ(cache.Stances(sweden, member), StanceModel.Stances(sweden, member));
                if (cachedMember != ParliamentSystem.WouldBillPass(sweden, member) || difference != null) { failures.Add($"Sweden: {law} as a member's bill was answered from the government's entry - the concern's author is not in the key ({difference ?? "the verdict"})"); break; }
                if (authorPairs >= 3) { break; }
            }
            if (authorPairs == 0) { failures.Add("Sweden: no government bill's stances differ from the same member's bill - the author's place in the key proved nothing"); }
            Compass(sweden, "at the start", failures);

            // SD withdraws its support.
            int invalidations = cache.Invalidations, version = start.Version;
            if (!sim.WithdrawSupport(CountryId.Sweden, out string refused)) { failures.Add($"Sweden (who governs): SD could not withdraw its support ({refused})"); return; }
            if (start.Version == version) { failures.Add("Sweden: SD withdrew and the government's version did not move"); }
            Answers withdrawn = Ask(cache, sweden, bills, "SD withdrawn", failures);
            if (cache.Invalidations == invalidations) { failures.Add("Sweden: SD withdrew its support and the cache kept its entries - they were cached under the old coalition"); }
            int seatMapMoved = atStart.MovedFor(withdrawn, "SD");
            if (seatMapMoved == 0) { failures.Add($"Sweden: SD withdrew and its seats moved on the map of none of {bills.Count} questions - the withdrawal proved nothing"); }
            int verdictsOnWithdrawal = atStart.VerdictsMoved(withdrawn);
            Compass(sweden, "SD withdrawn", failures);

            // Its consequence: SD moves no confidence; the week runs out; the Speaker's round replaces the government.
            if (!sim.MoveNoConfidence(CountryId.Sweden, out refused, out PoliSim.Elections.ConfidenceProcedure.MotionVote vote) || vote == null || !vote.Carried)
            {
                failures.Add($"Sweden (who governs): SD's motion was not carried ({refused ?? (vote == null ? "no vote" : vote.For + " for")}) - the round cannot follow");
                return;
            }
            PoliSim.Elections.GovernmentRecord fallen = sweden.Government;
            invalidations = cache.Invalidations;
            for (int d = 0; d < PoliSim.Elections.ConfidenceProcedure.ExtraElectionWindowDays + 1 && ReferenceEquals(sweden.Government, fallen); d++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); }
            if (ReferenceEquals(sweden.Government, fallen)) { failures.Add($"Sweden (who governs): the week ran out and the round replaced nothing ({Describe(fallen)}, caretaker {fallen.Caretaker})"); return; }
            Answers afterRound = Ask(cache, sweden, bills, "after the Speaker's round", failures);
            if (cache.Invalidations == invalidations) { failures.Add("Sweden: the round replaced the government and the cache kept its entries"); }
            int verdictsMoved = withdrawn.VerdictsMoved(afterRound);
            if (verdictsMoved == 0) { failures.Add($"Sweden: the round formed {Describe(sweden.Government)} and no verdict of {bills.Count} moved - the round proved nothing"); }
            Compass(sweden, "after the Speaker's round", failures);
            line.Append(string.Format(CultureInfo.InvariantCulture,
                " ({0} government bills): SD withdrew - its seats moved on the map of {1}, {2} verdict(s) moved; SD's motion carried and the round formed {3} - {4} verdict(s) moved",
                bills.Count, seatMapMoved, verdictsOnWithdrawal, Describe(sweden.Government), verdictsMoved));
        }

        private static void Leaving(SimulationManager sim, Country sweden, World world, List<string> failures, StringBuilder line)
        {
            PoliSim.Elections.GovernmentRecord start = sweden.Government;
            if (start == null || !start.Cabinet.Contains("KD") || start.PmParty == "KD")
            {
                failures.Add($"Sweden (who governs): the premise failed - the start's government is {Describe(start)}, not one KD sits in as a partner");
                return;
            }
            List<(string Law, BillConcern Concern)> bills = GovernmentBills(world, sweden);
            sweden.PlayerPartyAbbrev = "KD";
            var cache = new ChamberVerdicts();
            ChamberVerdicts.VerifyHits = false;
            Answers before = Ask(cache, sweden, bills, "KD in the cabinet", failures);
            int invalidations = cache.Invalidations, version = start.Version;
            if (!sim.LeaveGovernment(CountryId.Sweden, out string refused)) { failures.Add($"Sweden (who governs): KD could not leave the government ({refused})"); return; }
            if (start.Version == version) { failures.Add("Sweden: KD left and the government's version did not move"); }
            Answers left = Ask(cache, sweden, bills, "KD out of the cabinet", failures);
            if (cache.Invalidations == invalidations) { failures.Add("Sweden: KD left the government and the cache kept its entries"); }
            int moved = before.MovedFor(left, "KD");
            if (moved == 0) { failures.Add("Sweden: KD left the government and its seats moved on no map - the leaving proved nothing"); }
            Compass(sweden, "KD out of the cabinet", failures);
            if (PoliSim.Elections.GovernmentFormation.Cabinet(sweden).Contains("KD")) { failures.Add("Sweden: KD left the government and the compass still seats it in the cabinet"); }
            line.Append(string.Format(CultureInfo.InvariantCulture, "; KD left - its seats moved on the map of {0}, the cabinet {1}", moved, string.Join("+", PoliSim.Elections.GovernmentFormation.Cabinet(sweden))));
        }

        /// <summary>A write to a record's cabinet, support, prime minister or caretaker standing - by its receiver. A formation's `view` lists are not a record.</summary>
        private static readonly Regex DirectWrite = new Regex(@"(\w+)\??\.(?:(?:Cabinet|Support)\.(?:Add|AddRange|Remove|RemoveAll|RemoveAt|Clear|Insert|Sort|Reverse)\(|(?:Cabinet|Support|PmParty|Caretaker|CaretakerSince)\s*=(?!=))", RegexOptions.Compiled);

        /// <summary>(C) Who governs changes in place only through `GovernmentRecord`'s own methods, which bump its version - a write anywhere else in the game's source passes under the cache.</summary>
        private static void Guard(List<string> failures, StringBuilder line)
        {
            int files = 0, views = 0;
            foreach (string path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                if (path.Replace('\\', '/').EndsWith("/Elections/GovernmentRecord.cs", StringComparison.Ordinal)) { continue; }
                files++;
                foreach (Match m in DirectWrite.Matches(SourceText.ReadWithoutComments(path)))
                {
                    if (m.Groups[1].Value == "view") { views++; continue; }
                    failures.Add($"{path.Replace('\\', '/')}: '{m.Value}' writes who governs in place outside GovernmentRecord's methods - the record's version does not move, and the chamber cache keeps the old government");
                }
            }
            if (files == 0) { failures.Add("the who-governs guard read no source - it verified NOTHING"); }
            line.Append(string.Format(CultureInfo.InvariantCulture, "; the guard read {0} source files, every write to who governs outside the record's methods a formation view's ({1})", files, views));
        }

        private static void OnSweden(string name, List<string> failures, Action<SimulationManager, Country, World> body)
        {
            var go = new UnityEngine.GameObject(name);
            try
            {
                (SimulationManager sim, Country sweden, World world) = OpenSweden(go);
                body(sim, sweden, world);
            }
            catch (Exception e) { failures.Add($"Sweden (who governs, {name}): threw - {e.GetType().Name}: {e.Message}"); }
            finally { UnityEngine.Object.DestroyImmediate(go); EnergyMarket.ResetTurnState(); }
        }

        private static (SimulationManager, Country, World) OpenSweden(UnityEngine.GameObject go)
        {
            PoliSim.Elections.WorldClock.ApplyStart(CountryId.Sweden);
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            sim.PlayerCountryId = CountryId.Sweden;
            return (sim, world.GetCountry(CountryId.Sweden), world);
        }

        /// <summary>The questions: every catalogue law as a government bill, every enacted law's repeal, and a government bill on each stance axis alone, each way.</summary>
        private static List<(string Law, BillConcern Concern)> GovernmentBills(World world, Country country)
        {
            var bills = new List<(string Law, BillConcern Concern)>();
            foreach (LawDefinition law in LawCatalog.All)
            {
                if (!LawCatalog.IsWithinCompetence(world, country, law) || country.EnactedLaws.Exists(e => e.LawId == law.Id)) { continue; }
                BillConcern concern = ParliamentSystem.GetLawBillConcern(country, new LawBill { LawId = law.Id, IsRepeal = false });
                if (concern == null || concern.IsEmpty) { continue; }
                concern.GovernmentAuthored = true;
                bills.Add((law.Id, concern));
            }
            foreach (EnactedLaw enacted in country.EnactedLaws)
            {
                BillConcern concern = ParliamentSystem.GetLawBillConcern(country, new LawBill { LawId = enacted.LawId, IsRepeal = true });
                if (concern == null || concern.IsEmpty) { continue; }
                concern.GovernmentAuthored = true;
                bills.Add(("repeal " + enacted.LawId, concern));
            }
            foreach (StanceAxis axis in (StanceAxis[])Enum.GetValues(typeof(StanceAxis)))
            {
                foreach (float toward in new[] { -5f, -3f, -1f, 1f, 3f, 5f })
                {
                    bills.Add((string.Format(CultureInfo.InvariantCulture, "{0} {1:+0;-0}", axis, toward), new BillConcern { Direction = toward, GovernmentAuthored = true }.Add(axis, toward)));
                }
            }
            return bills;
        }

        /// <summary>The cache's answers at one step - each held against the model's as it is taken.</summary>
        private sealed class Answers
        {
            public readonly Dictionary<string, bool> Verdicts = new Dictionary<string, bool>();
            public readonly Dictionary<string, List<PartyStance>> Stances = new Dictionary<string, List<PartyStance>>();

            public int VerdictsMoved(Answers later) => Verdicts.Count(kv => later.Verdicts.TryGetValue(kv.Key, out bool v) && v != kv.Value);

            /// <summary>The questions on which a party's seats sit differently on the map - its side or its alignment.</summary>
            public int MovedFor(Answers later, string abbrev)
            {
                int moved = 0;
                foreach (KeyValuePair<string, List<PartyStance>> kv in Stances)
                {
                    if (!later.Stances.TryGetValue(kv.Key, out List<PartyStance> after)) { continue; }
                    int ia = kv.Value.FindIndex(s => s.Party.Abbrev == abbrev), ib = after.FindIndex(s => s.Party.Abbrev == abbrev);
                    if (ia < 0 || ib < 0) { continue; }
                    if (kv.Value[ia].Side != after[ib].Side || kv.Value[ia].Alignment != after[ib].Alignment) { moved++; }
                }
                return moved;
            }
        }

        private static Answers Ask(ChamberVerdicts cache, Country country, List<(string Law, BillConcern Concern)> bills, string step, List<string> failures)
        {
            var answers = new Answers();
            int wrong = 0;
            foreach ((string law, BillConcern concern) in bills)
            {
                bool cached = cache.WouldPass(country, concern);
                var stances = new List<PartyStance>(cache.Stances(country, concern));
                bool model = ParliamentSystem.WouldBillPass(country, concern);
                string difference = Differ(stances, StanceModel.Stances(country, concern));
                if (cached != model && wrong++ < 3) { failures.Add($"Sweden, {step}: the cache says {law} {(cached ? "PASSES" : "FAILS")}, the model that it {(model ? "PASSES" : "FAILS")} - a verdict cached under the old government"); }
                if (difference != null && wrong++ < 3) { failures.Add($"Sweden, {step}, the seat map of {law}: {difference}"); }
                answers.Verdicts[law] = cached;
                answers.Stances[law] = stances;
            }
            return answers;
        }

        /// <summary>The compass's cabinet against the votes' - one answer to who governs.</summary>
        private static void Compass(Country country, string step, List<string> failures)
        {
            PoliSim.Elections.GovernmentFormation.TryGovernment(country, out IReadOnlyList<string> votes, out IReadOnlyList<string> _);
            IReadOnlyList<string> compass = PoliSim.Elections.GovernmentFormation.Cabinet(country);
            if (!votes.SequenceEqual(compass)) { failures.Add($"Sweden, {step}: the compass names the cabinet {string.Join("+", compass)}, the votes {string.Join("+", votes)} - two answers to who governs"); }
        }

        private static string Describe(PoliSim.Elections.GovernmentRecord g) =>
            g == null ? "none" : string.Join("+", g.Cabinet) + (g.Support.Count > 0 ? " with " + string.Join("+", g.Support) : string.Empty);

        /// <summary>This check's own comparison, independent of the cache's: null when the lists are the same answer party by party.</summary>
        private static string Differ(IReadOnlyList<PartyStance> cached, IReadOnlyList<PartyStance> model)
        {
            if (cached.Count != model.Count) { return $"{cached.Count} stances cached against the model's {model.Count}"; }
            for (int i = 0; i < cached.Count; i++)
            {
                PartyStance c = cached[i], m = model[i];
                if (c.Party.Abbrev != m.Party.Abbrev || c.Seats != m.Seats || c.Side != m.Side || c.Measured != m.Measured || c.Alignment != m.Alignment)
                {
                    return $"{c.Party.Abbrev} {c.Seats} seats, side {c.Side}, alignment {c.Alignment:R} cached against {m.Party.Abbrev} {m.Seats} seats, side {m.Side}, alignment {m.Alignment:R}";
                }

                if (!(c.Reasons ?? Array.Empty<string>()).SequenceEqual(m.Reasons ?? Array.Empty<string>()))
                {
                    return $"{c.Party.Abbrev}'s reasons differ from the model's";
                }
            }

            return null;
        }

        private static string Largest(Country country)
        {
            return country.ParliamentSeats.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).First().Key;
        }
    }
}
