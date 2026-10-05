using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-4 (`COMPLETED.md` §787): THE STATE-SWING PROOF - asks R-US14, how a state's vote follows the national vote. Every jurisdiction and
    /// Maine's and Nebraska's districts are derived from the previous presidential election at the TRUE national shares of record (the
    /// catalog's own sums, <see cref="UsPresidentialReturns"/>), by the three methods R-US14 names, and the electors counted on the catalog by
    /// the statute's allocator (<see cref="ElectoralCollege.FromCatalog"/>'s shape, the predicted winners in place of the record's):
    /// <list type="bullet">
    /// <item>(a) §689's normalised uniform swing - <see cref="RegionalVoteModel.RegionalSharesByUniformSwing"/>, unchanged: re-solved until the
    /// states give the national shares back;</item>
    /// <item>(b) proportional swing - each party's share scaled by its national ratio, each state renormalised, not re-solved;</item>
    /// <item>(c) plain additive swing - Poland's okręg form (<see cref="PolishSejmAllocation"/>): each party's national change added, floored at
    /// zero, neither renormalised nor re-solved.</item>
    /// </list>
    /// Nothing is fitted. DECLARED: the parties are the two nominees and all others pooled per state, each a party with one swing; a state weighs
    /// what it cast in the previous election (no vote of the predicted election enters but its national shares); Maine's and Nebraska's districts
    /// swing with their state, in two-party terms (the district rows carry the two nominees only), a district's previous row standing in for the
    /// next election's district of the same number whatever its lines (between 2020 and 2024 both states' districts changed make-up - Maine's
    /// workbooks by town, Nebraska's books by county); faithless electors and Maine's ranked-choice count are not modelled. The record is the
    /// electors as appointed (§786: each slate's, its votes for other persons counted to it).
    ///
    /// <para>Pinned in the cheap bar and the documents bar, not live: what it measures is the US model card's marked block
    /// (`docs/reference/US_ELECTIONS.md`), written by <see cref="WriteReadings"/>; a card that no longer says what this measures fails here, and the
    /// pins hold the figures R-US14 is ruled on. The recommendation follows the plan's rule over <see cref="RuledPairs"/>: the method calling the most
    /// states right, (a) on a tie; a tie that leaves (a) out is not the rule's to settle and fails here, to be asked.</para>
    /// </summary>
    public static class UsStateSwingCheck
    {
        private static readonly string[] Methods = { "(a) normalised uniform", "(b) proportional", "(c) plain additive" };

        /// <summary>The pairs of elections R-US14's rule counts states right on.</summary>
        private static readonly (int Prev, int Next)[] RuledPairs = { (2016, 2020), (2020, 2024) };

        /// <summary>The figures R-US14 is ruled on, pinned: per pair of elections (by its first year) and method, the Republican nominee's electors,
        /// the states and the districts called wrong.</summary>
        private static readonly (int Prev, int Method, int ElectorsR, int StatesWrong, int DistrictsWrong)[] Pins =
        {
            (2012, 0, 235, 5, 1), (2012, 1, 235, 5, 1), (2012, 2, 235, 5, 1),
            (2016, 0, 230, 3, 0), (2016, 1, 231, 3, 1), (2016, 2, 230, 3, 0),
            (2020, 0, 312, 0, 0), (2020, 1, 312, 0, 0), (2020, 2, 312, 0, 0),
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsStateSwingCheck (PS-6 US-4, §787): the states from the national vote, three ways, on the elections of record ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string expected = CardBlock(card, digest);
                string onDisk = CardBlockOf(File.ReadAllText(CardPath(), Encoding.UTF8));
                Check(onDisk == expected, onDisk == null ? F("the model card ({0}) carries no readings block", CardRelative)
                    : onDisk == expected ? F("the model card's readings ({0}) say what this measures - {1} lines", CardRelative, card.Count)
                    : F("the model card's readings ({0}) are STALE - regenerate: -executeMethod PoliSim.EditorTools.UsStateSwingCheck.WriteReadings", CardRelative));
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n'); }

            sb.Append(failures == 0 ? "=== UsStateSwingCheck: the reading holds ===" : F("=== UsStateSwingCheck: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Writes the card's readings block from this reading - the stamp and its END marker are placed in the card once, by hand; this fills
        /// what lies between. Refuses while any check of the reading fails, the pins among them: the card is never written over a reading this check
        /// no longer holds.</summary>
        public static void WriteReadings()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsStateSwingCheck.WriteReadings (§787): the US model card's readings ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string path = CardPath();
                string text = File.ReadAllText(path, Encoding.UTF8);
                string old = CardBlockOf(text);
                if (failures > 0) { Check(false, "a check above failed - the card is not written; fix what failed (re-pin only where a pin moved), then write"); }
                else if (old == null) { Check(false, F("the model card ({0}) carries no readings block to write into", CardRelative)); }
                else
                {
                    string written = text.Replace("\r\n", "\n").Replace(old, CardBlock(card, digest));
                    if (text.Contains("\r\n")) { written = written.Replace("\n", "\r\n"); }
                    File.WriteAllText(path, written, new UTF8Encoding(false));
                    Check(true, F("the model card's readings written ({0}) - {1} lines", CardRelative, card.Count));
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n'); }

            sb.Append(failures == 0 ? "=== WriteReadings: written ===" : F("=== WriteReadings: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>The reading <see cref="Run"/> checks and <see cref="WriteReadings"/> writes: printed to <paramref name="sb"/>, every check through
        /// <paramref name="Check"/>, the card's lines added to <paramref name="card"/>. Returns the digest the block is stamped with - the catalog's
        /// own source digests, so a regenerated catalog makes the card stale.</summary>
        private static string Measure(StringBuilder sb, Action<bool, string> Check, List<string> card)
        {
            List<int> years = UsPresidentialReturns.Years.Select(y => y.Year).OrderBy(y => y).ToList();
            Check(years.Count >= 2, F("the catalog holds {0} elections - at least two to swing from one to the next", years.Count));
            string ruledLabel = string.Join(" and ", RuledPairs.Select(x => F("{0} → {1}", x.Prev, x.Next)));
            var rightOnRuled = new int[Methods.Length];
            var ruledSeen = new HashSet<(int, int)>();
            var measured = new List<(int Prev, int Next, int Method, int ElectorsR, int StatesWrong, int DistrictsWrong)>();
            card.Add("| from → to | method | electors R – D | record (as appointed) | states called wrong | districts called wrong | national R − D the states imply (pp) | mean absolute margin miss (pp) |");
            card.Add("|---|---|---:|---:|---|---|---:|---:|");
            var apart = new List<string>();
            var perState = new List<string>();
            for (int i = 1; i < years.Count; i++)
            {
                int prev = years[i - 1], next = years[i];
                var p = UsPresidentialReturns.States.Where(s => s.Year == prev).ToArray();
                var n = UsPresidentialReturns.States.Where(s => s.Year == next).ToArray();
                Check(p.Length > 0 && p.Select(s => s.State).SequenceEqual(n.Select(s => s.State)),
                    F("{0} → {1}: the same {2} jurisdictions in the same order", prev, next, p.Length));
                double[] natP = National(p), natN = National(n);
                var regions = p.Select(s => new RegionalVoteModel.RegionInput(s.State, s.VotesTotal, null)).ToArray();
                double weight = p.Sum(s => (double)s.VotesTotal);
                double[][] prior = p.Select(Shares).ToArray();
                double[][] uniform = RegionalVoteModel.RegionalSharesByUniformSwing(natN, regions, prior, out double residual);
                Check(residual < 1e-9, F("{0} → {1}: (a) closes on the national shares (worst residual {2:E1})", prev, next, residual));
                // the record's split as appointed: each nominee's electoral votes plus those the nominee's own electors cast for other persons (§786)
                var record = UsPresidentialReturns.Years.First(y => y.Year == next);
                int recR = record.CastR + record.OthersRSlate, recD = record.CastD + record.OthersDSlate;
                sb.Append(F("\n  {0} → {1}: the national shares D {2:0.00} R {3:0.00} others {4:0.00} → D {5:0.00} R {6:0.00} others {7:0.00} (%), R − D {8:+0.00;-0.00} pp; the record as appointed {9} - {10} (R - D)\n",
                    prev, next, 100 * natP[0], 100 * natP[1], 100 * natP[2], 100 * natN[0], 100 * natN[1], 100 * natN[2], 100 * (natN[1] - natN[0]), recR, recD));
                var predictedBy = new double[Methods.Length][][];
                var misses = new double[Methods.Length][];
                var wrongBy = new HashSet<string>[Methods.Length];
                var districtMisses = new Dictionary<string, double[]>();
                var districtWrongBy = new HashSet<string>[Methods.Length];
                var callsBy = new Dictionary<string, int>[Methods.Length];
                for (int m = 0; m < Methods.Length; m++)
                {
                    predictedBy[m] = new double[p.Length][];
                    misses[m] = new double[p.Length];
                    wrongBy[m] = new HashSet<string>();
                    districtWrongBy[m] = new HashSet<string>();
                    callsBy[m] = new Dictionary<string, int>();
                    var college = new List<ElectoralCollege.Jurisdiction>();
                    double absMiss = 0, implied = 0;
                    for (int r = 0; r < p.Length; r++)
                    {
                        double[] predicted = m == 0 ? uniform[r] : m == 1 ? Proportional(prior[r], natP, natN) : Additive(prior[r], natP, natN);
                        predictedBy[m][r] = predicted;
                        double[] actual = Shares(n[r]);
                        int win = predicted[1] > predicted[0] ? ElectoralCollege.Republican : ElectoralCollege.Democrat;
                        int actualWin = actual[1] > actual[0] ? ElectoralCollege.Republican : ElectoralCollege.Democrat;
                        callsBy[m][p[r].State] = win;
                        if (win != actualWin) { wrongBy[m].Add(p[r].State); }
                        misses[m][r] = 100.0 * ((predicted[1] - predicted[0]) - (actual[1] - actual[0]));
                        absMiss += Math.Abs(misses[m][r]);
                        implied += p[r].VotesTotal * (predicted[1] - predicted[0]);
                        var dPrev = UsPresidentialReturns.Districts.Where(d => d.Year == prev && d.State == p[r].State).OrderBy(d => d.District).ToArray();
                        var dNext = UsPresidentialReturns.Districts.Where(d => d.Year == next && d.State == p[r].State).OrderBy(d => d.District).ToArray();
                        if (dNext.Length == 0) { college.Add(new ElectoralCollege.Jurisdiction(p[r].State, n[r].Electors, win)); continue; }
                        if (m == 0) { Check(dPrev.Length == dNext.Length, F("{0} → {1} {2}: {3} districts, then {4}", prev, next, p[r].State, dPrev.Length, dNext.Length)); }
                        // DECLARED: a district swings with its state, in two-party terms - (a) and (c) add the state's change in its Democratic
                        // two-party share, (b) scales each nominee's share by the state's own ratio; its previous row stands in for the next
                        // election's district of the same number, whatever its lines
                        double statePrior = prior[r][0] / (prior[r][0] + prior[r][1]), stateNext = predicted[0] / (predicted[0] + predicted[1]);
                        var winners = new int[dNext.Length];
                        for (int k = 0; k < dNext.Length; k++)
                        {
                            double d2 = (double)dPrev[k].VotesD / (dPrev[k].VotesD + dPrev[k].VotesR);
                            double d2Next;
                            if (m == 1)
                            {
                                double a = d2 * predicted[0] / prior[r][0], b = (1.0 - d2) * predicted[1] / prior[r][1];
                                d2Next = a / (a + b);
                            }
                            else { d2Next = d2 + (stateNext - statePrior); }
                            winners[k] = d2Next > 0.5 ? ElectoralCollege.Democrat : ElectoralCollege.Republican;
                            double actual2 = (double)dNext[k].VotesD / (dNext[k].VotesD + dNext[k].VotesR);
                            int actualDistrictWin = actual2 > 0.5 ? ElectoralCollege.Democrat : ElectoralCollege.Republican;
                            string label = F("{0}-{1}", p[r].State, dNext[k].District);
                            callsBy[m][label] = winners[k];
                            if (winners[k] != actualDistrictWin) { districtWrongBy[m].Add(label); }
                            if (!districtMisses.TryGetValue(label, out double[] dm)) { dm = new double[Methods.Length + 1]; dm[0] = 100.0 * (1.0 - 2.0 * actual2); districtMisses[label] = dm; }
                            dm[m + 1] = 100.0 * ((1.0 - 2.0 * d2Next) - (1.0 - 2.0 * actual2));   // the two-party margin, R - D
                        }
                        college.Add(new ElectoralCollege.Jurisdiction(p[r].State, n[r].Electors, win, n[r].Electors - dNext.Length, winners));
                    }
                    int[] ev = ElectoralCollege.Allocate(college.ToArray(), 2);
                    int evR = ev[ElectoralCollege.Republican], evD = ev[ElectoralCollege.Democrat];
                    double mean = absMiss / p.Length;
                    implied = 100.0 * implied / weight;
                    string wrongStates = wrongBy[m].Count == 0 ? "none" : string.Join(", ", p.Select(s => s.State).Where(wrongBy[m].Contains));
                    string wrongDistricts = districtWrongBy[m].Count == 0 ? "none" : string.Join(", ", districtWrongBy[m].OrderBy(x => x, StringComparer.Ordinal));
                    sb.Append(F("    {0,-24} electors R {1} - D {2}{3}; states called wrong {4} ({5}); districts {6}; the states imply R − D {7:+0.00;-0.00} pp; mean |margin miss| {8:0.00} pp\n",
                        Methods[m], evR, evD, evR == recR && evD == recD ? " = the record" : "", wrongBy[m].Count, wrongStates, wrongDistricts, implied, mean));
                    card.Add(F("| {0} → {1} | {2} | {3} – {4} | {5} – {6} | {7} | {8} | {9:+0.00;-0.00} (true {10:+0.00;-0.00}) | {11:0.00} |", prev, next, Methods[m], evR, evD, recR, recD,
                        wrongBy[m].Count == 0 ? "none" : F("{0}: {1}", wrongBy[m].Count, wrongStates), wrongDistricts, implied, 100 * (natN[1] - natN[0]), mean));
                    measured.Add((prev, next, m, evR, wrongBy[m].Count, districtWrongBy[m].Count));
                    if (RuledPairs.Contains((prev, next))) { rightOnRuled[m] += p.Length - wrongBy[m].Count; ruledSeen.Add((prev, next)); }
                }

                // where (a) and (c) part: (a) is (c) renormalised and re-solved, and a renormalisation never changes which nominee leads - so they
                // differ only where a floor binds; the line says how far, and whether any call moves
                double widest = 0;
                for (int r = 0; r < p.Length; r++) { widest = Math.Max(widest, Math.Abs(misses[0][r] - misses[2][r])); }
                int callsApart = callsBy[0].Count(kv => callsBy[2][kv.Key] != kv.Value);
                string floored(int m) { var s = p.Where((x, r) => predictedBy[m][r].Any(v => v == 0.0)).Select(x => x.State).ToList(); return s.Count == 0 ? "none" : string.Join(", ", s); }
                apart.Add(F("| {0} → {1} | {2:0.00} | {3} | {4} | {5} |", prev, next, widest, callsApart, floored(0), floored(2)));

                // every state's miss, the margin (R - D) in points of the state's vote; ✗ where the method calls the winner wrong
                perState.Add("");
                perState.Add(F("**{0} → {1}** - the record's margin and each method's miss (R − D, pp; ✗ the winner called wrong):", prev, next));
                perState.Add("");
                perState.Add("| jurisdiction | record | (a) | (b) | (c) |");
                perState.Add("|---|---:|---:|---:|---:|");
                for (int r = 0; r < p.Length; r++)
                {
                    double[] actual = Shares(n[r]);
                    perState.Add(F("| {0} | {1:+0.00;-0.00} | {2} | {3} | {4} |", p[r].State, 100.0 * (actual[1] - actual[0]),
                        Miss(misses[0][r], wrongBy[0].Contains(p[r].State)), Miss(misses[1][r], wrongBy[1].Contains(p[r].State)), Miss(misses[2][r], wrongBy[2].Contains(p[r].State))));
                }
                foreach (KeyValuePair<string, double[]> d in districtMisses.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    perState.Add(F("| {0} (two-party) | {1:+0.00;-0.00} | {2} | {3} | {4} |", d.Key, d.Value[0],
                        Miss(d.Value[1], districtWrongBy[0].Contains(d.Key)), Miss(d.Value[2], districtWrongBy[1].Contains(d.Key)), Miss(d.Value[3], districtWrongBy[2].Contains(d.Key))));
                }
            }

            // the pins, each by name: the figures R-US14 is ruled on
            foreach (var pin in Pins)
            {
                var got = measured.Where(x => x.Prev == pin.Prev && x.Method == pin.Method).ToArray();
                Check(got.Length == 1 && got[0].ElectorsR == pin.ElectorsR && got[0].StatesWrong == pin.StatesWrong && got[0].DistrictsWrong == pin.DistrictsWrong,
                    got.Length == 0 ? F("{0} → ? {1}: pinned R {2}, states wrong {3}, districts wrong {4} - NOT MEASURED", pin.Prev, Methods[pin.Method], pin.ElectorsR, pin.StatesWrong, pin.DistrictsWrong)
                    : F("{0} → {1} {2}: electors R {3}, states wrong {4}, districts wrong {5} (pinned {6}, {7}, {8})", pin.Prev, got[0].Next, Methods[pin.Method],
                        got[0].ElectorsR, got[0].StatesWrong, got[0].DistrictsWrong, pin.ElectorsR, pin.StatesWrong, pin.DistrictsWrong));
            }
            foreach (var x in measured.Where(x => !Pins.Any(q => q.Prev == x.Prev && q.Method == x.Method)))
            {
                Check(false, F("{0} → {1} {2}: measured, not pinned - pin it before writing", x.Prev, x.Next, Methods[x.Method]));
            }
            Check(RuledPairs.All(x => ruledSeen.Contains((x.Prev, x.Next))), F("the ruled pairs ({0}) are in the catalog", ruledLabel));

            // R-US14's rule: the most states called right on the ruled pairs; (a) on a tie; a tie (a) is not in is not the rule's to settle
            int top = rightOnRuled.Max();
            List<int> leaders = Enumerable.Range(0, Methods.Length).Where(m => rightOnRuled[m] == top).ToList();
            string counts = string.Join("; ", Enumerable.Range(0, Methods.Length).Select(m => F("{0} {1}", Methods[m], rightOnRuled[m])));
            string verdict = leaders.Count == 1 ? F("**The plan's rule recommends {0}**.", Methods[leaders[0]])
                : leaders.Contains(0) ? F("**The plan's rule recommends {0}** - a tie, which the rule gives to (a).", Methods[0])
                : F("**The plan's rule does not decide**: {0} tie above (a), a case R-US14 does not cover - to be asked.", string.Join(" and ", leaders.Select(m => Methods[m])));
            Check(leaders.Count == 1 || leaders.Contains(0), F("R-US14's rule decides: {0}", verdict.Replace("**", string.Empty)));
            sb.Append(F("\n  R-US14: states called right on {0} together - {1}. {2}\n", ruledLabel, counts, verdict.Replace("**", string.Empty)));
            card.Add("");
            card.Add(F("States called right on {0} together: {1}. {2}", ruledLabel, counts, verdict));
            card.Add("");
            card.Add("Where (a) and (c) part - the largest difference between their margins in any jurisdiction, the calls (states and districts) they make differently, and the jurisdictions where each floors a party at zero:");
            card.Add("");
            card.Add("| from → to | largest (a) − (c) margin difference (pp) | calls apart | (a) floors | (c) floors |");
            card.Add("|---|---:|---:|---|---|");
            card.AddRange(apart);
            card.AddRange(perState);
            return Digest(UsPresidentialReturns.YearSourceDigest + UsPresidentialReturns.StateSourceDigest + UsPresidentialReturns.DistrictSourceDigest);
        }

        /// <summary>A jurisdiction's shares: the Democratic nominee, the Republican, all others together.</summary>
        private static double[] Shares((int Year, string State, int Electors, long VotesD, long VotesR, long VotesOther, long VotesTotal, int CastD, int CastR, int CastOther, string CastOtherTo) s)
            => new[] { (double)s.VotesD / s.VotesTotal, (double)s.VotesR / s.VotesTotal, (double)s.VotesOther / s.VotesTotal };

        /// <summary>The national shares: the jurisdictions' votes summed.</summary>
        private static double[] National((int Year, string State, int Electors, long VotesD, long VotesR, long VotesOther, long VotesTotal, int CastD, int CastR, int CastOther, string CastOtherTo)[] rows)
        {
            double d = 0, r = 0, o = 0, t = 0;
            foreach (var s in rows) { d += s.VotesD; r += s.VotesR; o += s.VotesOther; t += s.VotesTotal; }
            return new[] { d / t, r / t, o / t };
        }

        /// <summary>(b): each share scaled by its party's national ratio, the state renormalised - not re-solved, so the states need not give the
        /// national shares back (proportional swing lacks §689's property; <see cref="RegionalVoteModel"/>'s summary).</summary>
        private static double[] Proportional(double[] prior, double[] natP, double[] natN)
        {
            var s = new double[prior.Length];
            double sum = 0;
            for (int i = 0; i < s.Length; i++) { s[i] = prior[i] * natN[i] / natP[i]; sum += s[i]; }
            for (int i = 0; i < s.Length; i++) { s[i] /= sum; }
            return s;
        }

        /// <summary>(c): each party's national change added, floored at zero, not renormalised - Poland's okręg form. Where the floor binds, the
        /// state's shares sum above one and the states above the national shares.</summary>
        private static double[] Additive(double[] prior, double[] natP, double[] natN)
        {
            var s = new double[prior.Length];
            for (int i = 0; i < s.Length; i++) { s[i] = Math.Max(0.0, prior[i] + natN[i] - natP[i]); }
            return s;
        }

        private static string Miss(double miss, bool wrong) => F("{0:+0.00;-0.00}{1}", miss, wrong ? " ✗" : "");

        // ---- the US model card's readings (docs/reference/US_ELECTIONS.md) - the claim convention's marked block, written by WriteReadings ----

        private const string CardRelative = "docs/reference/US_ELECTIONS.md";
        private const string CardStamp = "<!-- GENERATED by PoliSim.EditorTools.UsStateSwingCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: ";
        private const string CardEnd = "<!-- END GENERATED -->";

        private static string CardPath() => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), CardRelative);

        private static string Digest(string text)
        {
            using (var sha = SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLowerInvariant(); }
        }

        /// <summary>The block as written: the stamp with the digest, the lines, the END marker - LF throughout.</summary>
        private static string CardBlock(List<string> lines, string digest)
        {
            var block = new StringBuilder(CardStamp).Append(digest).Append(" -->\n");
            foreach (string line in lines) { block.Append(line).Append('\n'); }
            return block.Append(CardEnd).ToString();
        }

        /// <summary>The card's block as it stands, line endings as LF - null where the card has no stamp and END marker.</summary>
        private static string CardBlockOf(string card)
        {
            string text = card.Replace("\r\n", "\n");
            int start = text.IndexOf(CardStamp, StringComparison.Ordinal);
            int end = start < 0 ? -1 : text.IndexOf(CardEnd, start, StringComparison.Ordinal);
            return end < 0 ? null : text.Substring(start, end + CardEnd.Length - start);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
