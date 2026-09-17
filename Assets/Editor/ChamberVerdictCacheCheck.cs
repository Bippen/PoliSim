using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
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
    /// dropped its entries. The seats and the party are restored.</para>
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

            ChamberVerdicts.VerifyHits = verifyWas;
            int drifts = ChamberVerdicts.Drifts - driftsBefore;
            if (drifts > 0) { failures.Add($"{drifts} drift(s) the cache's own verification caught on a hit"); }

            if (failures.Count == 0)
            {
                sb.Append("    CLEAN - every cached verdict and every cached stance is the model's own, through a draft, a seat and a party change.\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
                return;
            }

            foreach (string failure in failures.Take(20)) { sb.Append("    ⚠ ").Append(failure).Append('\n'); }
            sb.Append($"    {failures.Count} failure(s). ⚠ A cached chamber answer that differs from the model's is a screen that lies about a vote.\n");
            Debug.LogError(sb.ToString());
            CheckExit.Finish(1);
        }

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
