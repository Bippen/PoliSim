using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-5, PART FOUR (§736): THE PRESIDENT'S VETO AND THE SEJM'S OVERRIDE, PROVEN ON THE LAW - LIVE SINCE §761. The rule as the sources write it
    /// (`ElectionsData/poland/veto.md`): the President signs a statute within 21 days or sends it back with reasons (Konstytucja Art. 122 ust. 2 and 5);
    /// the Sejm overrides by re-passing it with <b>3/5 of the votes, at least half the statutory number of deputies (230 of 460) present</b>, and the
    /// President then signs within 7 days (ust. 5; Regulamin Sejmu Art. 64 ust. 5); <b>a veto the Sejm does not override closes the procedure</b> -
    /// the statute is dead (Regulamin Art. 64 ust. 6); and <b>the budget act and the act on a provisional budget cannot be vetoed</b> - Art. 122 ust. 5
    /// does not apply to them, the President signs them within 7 days (Art. 224 ust. 1).
    ///
    /// <para>Checked: (a) the constants against the held texts, phrase by phrase - so a rule here cannot drift from its source; (b) the override's count,
    /// its edges planted - 276 of 460 overrides and 275 does not; 229 present cannot override even unanimous, 230 present can with 138; (c) an abstention
    /// read as a vote cast, in the base (ruled by B2, the Sejm's own practice in (f)) on a case where the two readings differ;
    /// (d) on the Sejm of record (2023, the PKW's seats) with the president of record on the day - Duda, then Nawrocki from 2025-08-06: the governing
    /// coalition's 248 seats cannot override alone (276 needed with every deputy voting), nor with Konfederacja's 18; PiS's 194 can; (e) the budget act
    /// exempt.</para>
    ///
    /// <para><b>B1 and B2 (Elias's rulings, 2026-10-02).</b> B1: <i>"The President vetoes an ordinary statute when a majority of his backing party's
    /// deputies voted against it. Two kinds of act can't be vetoed: the budget act (Constitution art. 224 ...) and constitutional amendments (art. 235(7)).
    /// Budget-related acts are ordinary statutes and can be vetoed."</i> - widened by F3 below (<see cref="AtRisk"/>), backtested in (g) on the 10th Sejm's record
    /// (`ElectionsData/poland/veto_record.csv`, `third_readings_term10.csv`, from the Sejm API's per-MP votes, clubs summed). B2: <i>"3/5 of the deputies
    /// voting, abstentions included in the base ... Required = ceil(0.6 × (yes + no + abstain))"</i> - <see cref="Required"/>, checked in (f) against
    /// every override vote of the term. **LIVE since §761**: the helpers below read the runtime class (`PresidentialVeto`) the Sejm's statutes pass
    /// through, so the backtest and the constants test the rule the game runs; (h) drives the gate itself on planted divisions (vetoed, overridden, an
    /// amendment), and (i) drives one statute through the game's own bill path - introduced, resolved, its effect withheld where the veto stands. Then
    /// (j) (§768, ruling D4) a player's budget moving a rate and a spending line, stepped through the game's own path: the budget act adopted, the rate
    /// in its own tax act, at risk where PiS does not vote for it and vetoed by the draw planted to veto (F3), the old rate standing while the spending moves, and a
    /// budget that changes no rate one division; (k) (§773, ruling E2) a pension-age step in its own pension act, at risk and vetoed so, while the budget
    /// act carries the spending alone - with (k2) a bill changing every statute part, (k3) a benefit-level step in its own act, at risk and vetoed so,
    /// (k4) a fund created by an act uncontested by construction (no
    /// side recorded) and (k4b) a standing fund's rules moved, its raw weights said as the Fund tab says them, through the same path; (l) the Finance
    /// partner's boundary rates put to the Sejm as a tax act, on a PLANTED Written, its rates alone (a held fund draws no fund act), and a rates-only
    /// step spent though nothing lands; (m) the turn boundary itself, on a planted head and partner, the partner's tax act standing - and (m2) falling
    /// on a re-planted Sejm, so the boundary's order is guarded on both branches.</para>
    ///
    /// <para><b>F3 (Elias's ruling, widening B1):</b> <i>"A statute is at risk when the backing party did not vote for it: a majority of its voting members
    /// voted no or abstained. ... A seeded draw then decides, at each president's own rate refitted on the widened base: Duda's and Nawrocki's from the
    /// record, and the pooled rate for a president with no record. The slip shows the veto risk as a percentage."</i> (g) now proves the widened rule on
    /// the record - every veto at risk, B1's own misses at risk - and the runtime table the game draws at (`Generated.PolishVetoRates`) recomputed from
    /// the same CSVs and generated from the files on disk; (h) proves the risk, the draw (keyed, reproducible, its frequency the rate) and the gate on
    /// both of the draw's branches, PLANTED (`PresidentialVeto.PlantDraw`) so each case is decided by name, never by the bar's seed - all but the last,
    /// which drives the gate UNPLANTED on titles the bar's own seed keys to each side of the risk, so its verdicts hold on any seed; (i) to (l) step
    /// their statutes through the game's own paths with the draw planted to veto, and (i) once more planted to sign; (m) plants the draw to sign, its case
    /// the boundary's order, not the draw.</para>
    /// </summary>
    public static class PresidentialVetoDiagnostic
    {
        /// <summary>The rule as the sources write it (veto.md §1-3): the clock, the override's fraction and quorum, the budget's exemption.</summary>
        private const int StatutoryDeputies = 460;          // Art. 96 ust. 1
        private const int SignDays = 21;                    // Art. 122 ust. 2
        private const int SignAfterOverrideDays = 7;        // Art. 122 ust. 5
        private const int OverrideNumerator = 3, OverrideDenominator = 5;   // Art. 122 ust. 5; Regulamin Art. 64 ust. 5
        private const int BudgetSignDays = 7;               // Art. 224 ust. 1

        /// <summary>The quorum: at least half the statutory number present (Art. 122 ust. 5).</summary>
        private static int Quorum => (StatutoryDeputies + 1) / 2;

        /// <summary>B2 (Elias's ruling, 2026-10-02): the votes an override needs - ceil(3/5 of the deputies voting, abstentions in the base), the Sejm's
        /// own practice in all seven override votes of the 10th term (`veto_record.md` §4). Integer arithmetic.</summary>
        private static int Required(int voting) => PresidentialVeto.OverrideRequired(voting, 0, 0);   // §761: the live rule's

        /// <summary>Whether the Sejm's re-pass overrides: the quorum present, and FOR at least <see cref="Required"/> of those voting - an abstention a
        /// vote cast (B2, ruled).</summary>
        private static bool Overrides(int forVotes, int against, int abstaining, int present)
        {
            return forVotes + against + abstaining > 0 && PresidentialVeto.Overrides(forVotes, against, abstaining, present);   // §761: the live rule's
        }

        /// <summary>F3 (Elias's ruling, widening B1): a statute is AT RISK when the President's backing party did not vote for it - a majority of its VOTING
        /// members voted no or abstained, the absent outside the count; never an act exempt (the budget act, Art. 224; a constitutional amendment,
        /// Art. 235 ust. 7).</summary>
        private static bool AtRisk(bool exempt, int backingYes, int backingNo, int backingAbstain) =>
            PresidentialVeto.AtRisk(exempt ? PresidentialVeto.Act.BudgetAct : PresidentialVeto.Act.OrdinaryStatute, backingYes, backingNo, backingAbstain);   // the live rule's

        /// <summary>A CSV with a header row, each row by column name.</summary>
        private static List<Dictionary<string, string>> ReadCsv(string path)
        {
            var rows = new List<Dictionary<string, string>>();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            string[] head = SplitCsv(lines[0]);
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) { continue; }
                string[] cells = SplitCsv(lines[i]);
                var row = new Dictionary<string, string>();
                for (int c = 0; c < head.Length; c++) { row[head[c]] = c < cells.Length ? cells[c] : string.Empty; }
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>Splits one CSV row; a field may be quoted, with a comma inside.</summary>
        private static string[] SplitCsv(string line)
        {
            var fields = new List<string>();
            var cur = new StringBuilder();
            bool quoted = false;
            foreach (char c in line)
            {
                if (c == '"') { quoted = !quoted; continue; }
                if (c == ',' && !quoted) { fields.Add(cur.ToString()); cur.Clear(); continue; }
                cur.Append(c);
            }
            fields.Add(cur.ToString());
            return fields.ToArray();
        }

        /// <summary>The same count on the other reading - the 3/5 of FOR and AGAINST only - for the one planted case that tells them apart.</summary>
        private static bool OverridesIgnoringAbstentions(int forVotes, int against, int present) =>
            present >= Quorum && forVotes + against > 0 && forVotes * OverrideDenominator >= (forVotes + against) * OverrideNumerator;

        /// <summary>A statute's kind as the veto reads it: the budget act (or a provisional budget) is not vetoable (Art. 224 ust. 1).</summary>
        private static bool MayVeto(bool budgetAct) => !budgetAct;

        /// <summary>SOURCED: the 10th-term Sejm as the PKW's notice announced it (Dz.U. 2023 poz. 2234; returns_2023.md) - the parties' seats.</summary>
        private static readonly Dictionary<string, int> Sejm2023 = new Dictionary<string, int> { { "PiS", 194 }, { "KO", 157 }, { "TD", 65 }, { "NL", 26 }, { "Konf", 18 } };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PresidentialVetoDiagnostic (PS-5 part four, §736): the veto and the Sejm's 3/5 override, on the law ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                // (a) the constants against the held texts
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "poland"));
                string veto = File.ReadAllText(Path.Combine(root, "veto.md"), Encoding.UTF8);
                string returns = File.ReadAllText(Path.Combine(root, "presidential_returns.md"), Encoding.UTF8);
                Check(returns.Contains("podpisuje ustawę w ciągu 21 dni") && SignDays == 21, "Art. 122 ust. 2 - signed within 21 days");
                Check(returns.Contains("większością 3/5 głosów w obecności co najmniej połowy ustawowej liczby posłów") && OverrideNumerator == 3 && OverrideDenominator == 5,
                    "Art. 122 ust. 5 - the override by 3/5 of the votes, half the statutory number present");
                Check(returns.Contains("w ciągu 7 dni podpisuje ustawę") && SignAfterOverrideDays == 7, "Art. 122 ust. 5 - signed within 7 days of the override");
                Check(returns.Contains("Sejm składa się z 460 posłów") && StatutoryDeputies == 460 && Quorum == 230, "Art. 96 ust. 1 - 460 deputies; the quorum 230");
                Check(veto.Contains("Do ustawy budżetowej i ustawy o prowizorium budżetowym nie stosuje się przepisu art. 122 ust. 5") && BudgetSignDays == 7 && !MayVeto(budgetAct: true),
                    "Art. 224 ust. 1 - the budget act and a provisional budget are not vetoable; signed within 7 days");
                Check(veto.Contains("postępowanie ustawodawcze ulega zamknięciu"), "Regulamin Sejmu Art. 64 ust. 6 - a veto not overridden closes the procedure");

                // (b) the count, its edges planted
                Check(Overrides(276, 184, 0, 460) && !Overrides(275, 185, 0, 460), "all 460 voting: 276 FOR overrides, 275 does not (276 × 5 = 1 380 ≥ 460 × 3; 275 × 5 = 1 375 <)");
                Check(!Overrides(229, 0, 0, 229), "229 present: below the quorum - not even a unanimous re-pass overrides");
                Check(Overrides(138, 92, 0, 230) && !Overrides(137, 93, 0, 230), "230 present: 138 FOR overrides (3/5 of 230), 137 does not");

                // (c) the abstention's reading, on the case that tells the two readings apart
                bool strict = Overrides(180, 100, 30, 310), lenient = OverridesIgnoringAbstentions(180, 100, 310);
                Check(!strict && lenient, F("180 FOR, 100 AGAINST, 30 abstaining: B2's reading (abstentions in the base) says no (180 of 310 voting, {0:0.0} %), the other would say yes ({1:0.0} % of FOR and AGAINST) - ruled, and the record's practice (f)",
                    100.0 * 180 / 310, 100.0 * 180 / 280));

                // (d) the Sejm of record, with the president of record on the day
                Check(Sejm2023.Values.Sum() == StatutoryDeputies, F("the 2023 Sejm's seats sum to {0}", Sejm2023.Values.Sum()));
                int coalition = Sejm2023["KO"] + Sejm2023["TD"] + Sejm2023["NL"];
                int others = StatutoryDeputies - coalition;
                Check(!Overrides(coalition, others, 0, StatutoryDeputies), F("KO + TD + NL ({0}) against the rest ({1}): the veto stands - 276 needed with every deputy voting", coalition, others));
                Check(!Overrides(coalition + Sejm2023["Konf"], Sejm2023["PiS"], 0, StatutoryDeputies), F("with Konfederacja ({0}) against PiS: still short", coalition + Sejm2023["Konf"]));
                Check(Overrides(coalition + Sejm2023["PiS"], Sejm2023["Konf"], 0, StatutoryDeputies), F("with PiS ({0}): overridden", coalition + Sejm2023["PiS"]));
                bool duda = PresidencyOfRecord.TryAt(CountryId.Poland, new DateTime(2023, 12, 13), out PresidencyOfRecord.President p2023) && p2023.Name == "Andrzej Duda";
                bool nawrocki = PresidencyOfRecord.TryAt(CountryId.Poland, new DateTime(2026, 1, 1), out PresidencyOfRecord.President p2026) && p2026.Name == "Karol Nawrocki";
                Check(duda && nawrocki, "the president of record holds the veto on the day - Duda on 2023-12-13 (the Tusk cabinet's day), Nawrocki on 2026-01-01");

                // (e) the budget
                Check(!MayVeto(budgetAct: true) && MayVeto(budgetAct: false), "a budget act cannot be vetoed; an ordinary statute can");

                // (f) B2 (Elias's ruling, 2026-10-02): "3/5 of the deputies voting, abstentions included in the base ... Required = ceil(0.6 × (yes + no +
                // abstain))" - every override vote of the 10th Sejm, its required figure as the Sejm's own record states it (`veto_record.csv`)
                List<Dictionary<string, string>> vetoes = ReadCsv(Path.Combine(root, "veto_record.csv"));
                int overrideVotes = 0, overrideMatches = 0;
                foreach (Dictionary<string, string> v in vetoes)
                {
                    if (string.IsNullOrEmpty(v["override_vote_no"])) { continue; }
                    overrideVotes++;
                    int yes = int.Parse(v["override_yes"], CultureInfo.InvariantCulture), no = int.Parse(v["override_no"], CultureInfo.InvariantCulture), abst = int.Parse(v["override_abstain"], CultureInfo.InvariantCulture);
                    int stated = int.Parse(v["override_required_stated"], CultureInfo.InvariantCulture);
                    bool match = Required(yes + no + abst) == stated;
                    if (match) { overrideMatches++; }
                    sb.Append(F("    override  {0}/{1} (on the veto of {2}): {3} / {4} / {5} - required {6}, the record states {7}{8}; {9}\n", v["override_sitting"], v["override_vote_no"], v["veto_date"],
                        yes, no, abst, Required(yes + no + abst), stated, match ? string.Empty : " - MISMATCH", Overrides(yes, no, abst, yes + no + abst) ? "OVERRIDDEN" : "the veto stands"));
                }
                Check(overrideVotes == 7 && overrideMatches == 7, F("B2: the Sejm's own required figure equals ceil(3/5 of yes + no + abstain) in {0} of {1} override votes - abstentions in the base, the record's practice", overrideMatches, overrideVotes));
                Check(Required(243 + 192) == 261 && Required(246 + 192) == 263 && Required(232 + 199) == 259, "B2's three quoted votes: 243+192 -> 261, 246+192 -> 263, 232+199 -> 259");

                // (g) F3 (Elias's ruling, widening B1): a statute is at risk when the backing party did not vote for it - a majority of its voting members
                // voted no or abstained - and a seeded draw decides at each president's rate on that base; the budget act (Art. 224) and a constitutional
                // amendment (Art. 235 ust. 7) are never at risk; a budget-related act is an ordinary statute. On the 10th Sejm's record: every veto falls on a
                // statute at risk, B1's own misses (§775) among them; and the runtime table the game draws at (Generated.PolishVetoRates, written by
                // Tools/veto_b1_backtest.pl) is these CSVs, recomputed here president by president, generated from the files on disk.
                Check(veto.Contains("Prezydent Rzeczypospolitej podpisuje ustawę w ciągu 21 dni od dnia przedstawienia i zarządza jej ogłoszenie") && !AtRisk(exempt: true, 0, 100, 100),
                    "Art. 235 ust. 7 - a constitutional amendment is signed within 21 days, never returned (and the budget act, Art. 224, above)");
                int vetoesAtRisk = 0, b1Misses = 0, b1MissesAtRisk = 0;
                var outside = new List<string>();
                foreach (Dictionary<string, string> v in vetoes)
                {
                    int pisYes = int.Parse(v["pis_yes"], CultureInfo.InvariantCulture), pisNo = int.Parse(v["pis_no"], CultureInfo.InvariantCulture);
                    int pisAbstain = int.Parse(v["pis_abstain"], CultureInfo.InvariantCulture), pisMembers = int.Parse(v["pis_members"], CultureInfo.InvariantCulture);
                    bool risk = AtRisk(exempt: false, pisYes, pisNo, pisAbstain);
                    if (risk) { vetoesAtRisk++; }
                    else
                    {
                        outside.Add(F("{0} {1}: {2} - PiS {3} for, {4} against, {5} abstaining, {6} absent of {7}", v["president"], v["veto_date"], v["act_title"], pisYes, pisNo, pisAbstain, v["pis_absent"], pisMembers));
                    }
                    if (pisNo * 2 <= pisMembers) { b1Misses++; if (risk) { b1MissesAtRisk++; } }   // B1 as first ruled (§757): more than half the club's MEMBERS voting NO
                }
                foreach (string m in outside) { sb.Append("    outside   ").Append(m).Append('\n'); }
                Check(vetoes.Count > 0 && vetoesAtRisk == vetoes.Count, F("F3: every veto on the record falls on a statute at risk - {0} of {1}", vetoesAtRisk, vetoes.Count));
                Check(b1Misses > 0 && b1MissesAtRisk == b1Misses,
                    F("F3: every veto B1 as first ruled missed is at risk - {0} of {1} (the bloc abstentions, the split club, the club absent)", b1MissesAtRisk, b1Misses));
                // the widened base, by the president on the day of the decision, against the runtime table
                var atRiskOf = new Dictionary<string, int>();
                var vetoedOf = new Dictionary<string, int>();
                foreach (Dictionary<string, string> r in ReadCsv(Path.Combine(root, "third_readings_term10.csv")))
                {
                    if (!string.IsNullOrEmpty(r["veto_exempt"])) { continue; }
                    string outcome = r["outcome"].StartsWith("vetoed", StringComparison.Ordinal) ? "vetoed" : r["outcome"].StartsWith("signed", StringComparison.Ordinal) ? "signed"
                        : r["outcome"].StartsWith("referred to Tribunal", StringComparison.Ordinal) ? "Tribunal" : null;
                    if (outcome == null) { continue; }   // undecided: at the Senate, at the President, Senate amendments pending
                    DateTime on = DateTime.ParseExact(r["outcome_date"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    if (!PresidencyOfRecord.TryAt(CountryId.Poland, on, out PresidencyOfRecord.President who)) { continue; }
                    if (!AtRisk(exempt: false, int.Parse(r["pis_yes"], CultureInfo.InvariantCulture), int.Parse(r["pis_no"], CultureInfo.InvariantCulture), int.Parse(r["pis_abstain"], CultureInfo.InvariantCulture))) { continue; }
                    atRiskOf[who.Name] = (atRiskOf.TryGetValue(who.Name, out int a) ? a : 0) + 1;
                    if (outcome == "vetoed") { vetoedOf[who.Name] = (vetoedOf.TryGetValue(who.Name, out int b) ? b : 0) + 1; }
                }
                int tableMatches = 0, sumRisk = 0, sumVetoed = 0;
                foreach ((string name, int risked, int vetoedN) in PoliSim.Elections.Generated.PolishVetoRates.OfRecord)
                {
                    int csvRisk = atRiskOf.TryGetValue(name, out int a) ? a : 0, csvVetoed = vetoedOf.TryGetValue(name, out int b) ? b : 0;
                    double rate = PresidentialVeto.RateOf(name, out string basis);
                    sb.Append(F("    rate      {0}: the record's {1} at risk, {2} vetoed - the game draws at {3:0.0} % ({4}); the table holds {5} and {6}\n", name, csvRisk, csvVetoed, rate * 100.0, basis, risked, vetoedN));
                    if (risked > 0 && csvRisk == risked && csvVetoed == vetoedN && Math.Abs(rate - (double)vetoedN / risked) < 1e-12) { tableMatches++; }
                    sumRisk += risked;
                    sumVetoed += vetoedN;
                }
                int tableRows = PoliSim.Elections.Generated.PolishVetoRates.OfRecord.Length;
                Check(tableRows > 0 && tableMatches == tableRows && atRiskOf.Count == tableRows,
                    F("F3: the runtime table (Generated.PolishVetoRates) is the record's widened base, president by president - {0} of {1} rows match the CSVs, the record names {2} president(s)", tableMatches, tableRows, atRiskOf.Count));
                string readingsDigest = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(Path.Combine(root, "third_readings_term10.csv")));
                string vetoesDigest = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(Path.Combine(root, "veto_record.csv")));
                Check(string.Equals(readingsDigest, PoliSim.Elections.Generated.PolishVetoRates.SourceDigest, StringComparison.OrdinalIgnoreCase)
                      && string.Equals(vetoesDigest, PoliSim.Elections.Generated.PolishVetoRates.VetoRecordDigest, StringComparison.OrdinalIgnoreCase),
                    "F3: the runtime table was generated from the CSVs on disk - both digests match (a changed CSV is re-run through Tools/veto_b1_backtest.pl, its diff read first)");
                double pooled = PresidentialVeto.RateOf("Rafał Trzaskowski", out string pooledBasis);
                Check(sumRisk > 0 && Math.Abs(pooled - (double)sumVetoed / sumRisk) < 1e-12 && pooledBasis.StartsWith("the pooled rate", StringComparison.Ordinal),
                    F("F3: a president with no record of their own draws at the pooled rate - {0:0.0} % ({1})", pooled * 100.0, pooledBasis));

                // (h) §761 - THE RULE LIVE: the runtime class's constants are the texts'; Decide on the 2023 Sejm's seats; the gate itself on a planted chamber
                Check(PresidentialVeto.StatutoryDeputies == StatutoryDeputies && PresidentialVeto.Quorum == Quorum && PresidentialVeto.OverrideNumerator == OverrideNumerator
                      && PresidentialVeto.OverrideDenominator == OverrideDenominator && !PresidentialVeto.MayVeto(PresidentialVeto.Act.BudgetAct) && !PresidentialVeto.MayVeto(PresidentialVeto.Act.ConstitutionalAmendment),
                    "§761: the live class's constants are the texts' (460, the quorum 230, 3/5), the budget act and an amendment never vetoed");
                var against = new List<DivisionSide> { Side("PiS", 194, -1), Side("KO", 157, 1), Side("TD", 65, 1), Side("NL", 26, 1), Side("Konf", 18, -1) };
                double nawrockiRate = PresidentialVeto.RateOf("Karol Nawrocki", out string nawrockiBasis), dudaRate = PresidentialVeto.RateOf("Andrzej Duda", out _);
                DateTime day2026 = new DateTime(2026, 1, 1);
                // the projection taken with the draw PLANTED to veto (the review's finding): a draw inside Decide would veto here on every run, and fail
                PresidentialVeto.Outcome o;
                using (PresidentialVeto.PlantDraw(0.0)) { o = PresidentialVeto.Decide(CountryId.Poland, null, day2026, PresidentialVeto.Act.OrdinaryStatute, against); }
                Check(o != null && o.President == "Karol Nawrocki" && o.BackingParty == "PiS" && o.AtRisk && o.Risk == nawrockiRate && o.RiskBasis == nawrockiBasis
                      && !o.OverrideCarries && o.Yes == 248 && o.Required == 276 && !o.Vetoed && o.Stands,
                    F("F3: PiS against on 2026-01-01 - at risk with {0}, at the president's own rate ({1:0.0} %, {2}); {3} for, {4} needed - an override would fail; nothing decided before the gate draws (taken with the draw planted to veto)",
                        o?.President, nawrockiRate * 100.0, nawrockiBasis, o?.Yes, o?.Required));
                bool Drawn(double u, out PresidentialVeto.Outcome drawn)
                {
                    drawn = PresidentialVeto.Decide(CountryId.Poland, null, day2026, PresidentialVeto.Act.OrdinaryStatute, against);
                    drawn.DrawOn(u);
                    return drawn.Vetoed;
                }
                bool vetoedAtZero = Drawn(0.0, out PresidentialVeto.Outcome atZero), vetoedJustBelow = Drawn(nawrockiRate - 1e-9, out _), vetoedAtRate = Drawn(nawrockiRate, out _), vetoedAtOne = Drawn(1.0, out _);
                Check(vetoedAtZero && !atZero.Overridden && !atZero.Stands && vetoedJustBelow && !vetoedAtRate && !vetoedAtOne,
                    "F3: the gate's draw decides - a draw below the risk vetoes (here the veto stands), a draw at or above it signs");
                var abstaining = new List<DivisionSide> { Side("PiS", 194, 0), Side("KO", 157, 1), Side("TD", 65, 1), Side("NL", 26, 1), Side("Konf", 18, -1) };
                var supporting = new List<DivisionSide> { Side("PiS", 194, 1), Side("KO", 157, 1), Side("TD", 65, 1), Side("NL", 26, 1), Side("Konf", 18, -1) };
                PresidentialVeto.Outcome abstained = PresidentialVeto.Decide(CountryId.Poland, null, day2026, PresidentialVeto.Act.OrdinaryStatute, abstaining);
                PresidentialVeto.Outcome supported = PresidentialVeto.Decide(CountryId.Poland, null, day2026, PresidentialVeto.Act.OrdinaryStatute, supporting);
                PresidentialVeto.Outcome budget = PresidentialVeto.Decide(CountryId.Poland, null, day2026, PresidentialVeto.Act.BudgetAct, against);
                PresidentialVeto.Outcome dudaOutcome = PresidentialVeto.Decide(CountryId.Poland, null, new DateTime(2024, 3, 1), PresidentialVeto.Act.OrdinaryStatute, against);
                Check(abstained != null && abstained.AtRisk && abstained.Risk == nawrockiRate && supported != null && !supported.AtRisk && supported.Risk == 0.0
                      && budget != null && !budget.AtRisk && budget.Risk == 0.0 && dudaOutcome != null && dudaOutcome.President == "Andrzej Duda" && dudaOutcome.AtRisk && dudaOutcome.Risk == dudaRate
                      && PresidentialVeto.Decide(CountryId.Germany, null, day2026, PresidentialVeto.Act.OrdinaryStatute, against) == null,
                    F("F3: PiS abstaining - at risk (B1 missed the record's bloc abstentions); PiS for - no risk; the budget act - no risk; Duda on 2024-03-01 - at risk at Duda's own rate ({0:0.0} %); Germany - no veto the game runs", dudaRate * 100.0));
                // AtRisk's edges (the review's finding): a tie among the club's voting members is no majority not voting for it; one more against, or one more
                // abstaining, is; a club with no member voting - no seat - puts nothing at risk (the reading stated on AtRisk)
                var tie = new List<DivisionSide> { Side("PiS", 10, 1), Side("PiS", 10, -1), Side("KO", 157, 1) };
                var noClub = new List<DivisionSide> { Side("KO", 157, 1), Side("TD", 65, -1) };
                PresidentialVeto.Outcome tied = PresidentialVeto.Decide(CountryId.Poland, null, day2026, PresidentialVeto.Act.OrdinaryStatute, tie);
                PresidentialVeto.Outcome clubless = PresidentialVeto.Decide(CountryId.Poland, null, day2026, PresidentialVeto.Act.OrdinaryStatute, noClub);
                Check(tied != null && !tied.AtRisk && tied.ClubVoting == 20 && clubless != null && !clubless.AtRisk && clubless.ClubVoting == 0 && clubless.Risk == 0.0
                      && PresidentialVeto.AtRisk(PresidentialVeto.Act.OrdinaryStatute, 10, 11, 0) && PresidentialVeto.AtRisk(PresidentialVeto.Act.OrdinaryStatute, 10, 0, 11)
                      && !PresidentialVeto.AtRisk(PresidentialVeto.Act.OrdinaryStatute, 10, 5, 5) && !PresidentialVeto.AtRisk(PresidentialVeto.Act.OrdinaryStatute, 0, 0, 0),
                    "F3's edges: a tie among the club's voting members - no risk; one more against, or one more abstaining - at risk; a club with no member voting (no seat) - no risk");
                // the seeded draw: reproducible, keyed on the master seed and the act, and its frequency the rate
                double d1 = PresidentialVeto.Draw(777, CountryId.Poland, day2026, "Enact: a planted statute"), d1Again = PresidentialVeto.Draw(777, CountryId.Poland, day2026, "Enact: a planted statute");
                double dSeed = PresidentialVeto.Draw(778, CountryId.Poland, day2026, "Enact: a planted statute"), dTitle = PresidentialVeto.Draw(777, CountryId.Poland, day2026, "Enact: another planted statute");
                double dDay = PresidentialVeto.Draw(777, CountryId.Poland, day2026.AddDays(1), "Enact: a planted statute");
                Check(d1 == d1Again && d1 >= 0.0 && d1 < 1.0 && d1 != dSeed && d1 != dTitle && d1 != dDay,
                    F("F3: the draw is the key's - the same seed, act and day draw the same ({0:0.0000}); another seed, title or day draws apart", d1));
                const int keyedDraws = 20000;
                int below = 0;
                for (int i = 0; i < keyedDraws; i++) { if (PresidentialVeto.Draw(777, CountryId.Poland, day2026.AddDays(i % 365), "Enact: statute " + i.ToString(CultureInfo.InvariantCulture)) < nawrockiRate) { below++; } }
                double frequency = (double)below / keyedDraws;
                Check(Math.Abs(frequency - nawrockiRate) < 0.01, F("F3: the draw's frequency is the rate - {0} of {1} keyed draws fall below {2:0.00} % ({3:0.00} %)", below, keyedDraws, nawrockiRate * 100.0, frequency * 100.0));
                double inside;
                using (PresidentialVeto.PlantDraw(0.25)) { inside = PresidentialVeto.Draw(777, CountryId.Poland, day2026, "Enact: a planted statute"); }
                Check(inside == 0.25 && PresidentialVeto.Draw(777, CountryId.Poland, day2026, "Enact: a planted statute") == d1, "F3: a planted draw holds inside its scope and the keyed draw returns after it");
                var go = new GameObject("PresidentialVetoDiagnostic gate");
                try
                {
                    using (PoliSim.Simulation.SimulationManager.EpochScope())
                    {
                        WorldClock.ApplyStart(CountryId.Poland);
                        World world = WorldFactory.CreateDefault();
                        var sim = go.AddComponent<PoliSim.Simulation.SimulationManager>();
                        sim.SetWorld(world);
                        Country pl = world.GetCountry(CountryId.Poland);
                        System.Reflection.MethodInfo gate = typeof(PoliSim.Simulation.SimulationManager).GetMethod("PresidentialVetoGate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        // the gate is handed THIS vote's division (RecordDivision's return), never the log's last entry
                        bool Gate(List<DivisionSide> sides, PresidentialVeto.Act act, out DivisionRecord passage, out DivisionRecord vote, string title = "Enact: a planted statute")
                        {
                            passage = pl.Divisions.Append(title, sim.CurrentDate, 0.5f, true, 1f, 0, new List<DivisionSide>(sides));
                            int before = pl.Divisions.Entries.Count;
                            bool stood = (bool)gate.Invoke(sim, new object[] { pl, passage, true, act });
                            vote = pl.Divisions.Entries.Count > before ? pl.Divisions.Entries[pl.Divisions.Entries.Count - 1] : null;
                            return stood;
                        }
                        // F3: every gate case below but the last is decided by name - the draw PLANTED to veto (0) or to sign (1), never the bar's own seed; the
                        // last drives the gate UNPLANTED, on titles the bar's own seed keys to each side of the risk, so its verdicts hold on any seed
                        bool stands; DivisionRecord passage1, vetoed;
                        using (PresidentialVeto.PlantDraw(0.0)) { stands = Gate(against, PresidentialVeto.Act.OrdinaryStatute, out passage1, out vetoed); }
                        Check(!stands && passage1.Motion && vetoed != null && !vetoed.Passed && !vetoed.Motion && vetoed.Required == 276 && vetoed.Contest == null
                              && vetoed.Title.StartsWith("Vetoed by the President", StringComparison.Ordinal) && vetoed.Title.Contains("the veto stands, 248 for, 276 needed")
                              && vetoed.Sides.Exists(s => s.Abbrev == "PiS" && s.Side < 0 && s.Reason.Contains("voted against the statute") && s.ReasonShort.EndsWith("against the override", StringComparison.Ordinal)),
                            F("§761, F3: the gate on Poland's start ({0}), the draw planted to veto - the statute does not stand; the passage keeps no ceremony; the vote on the veto, its line 276 and no budget contest: \"{1}\"",
                                sim.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), vetoed?.Title));
                        bool signedStands; DivisionRecord passageSigned, noneSigned;
                        using (PresidentialVeto.PlantDraw(1.0)) { signedStands = Gate(against, PresidentialVeto.Act.OrdinaryStatute, out passageSigned, out noneSigned); }
                        Check(signedStands && noneSigned == null && !passageSigned.Motion, "F3: the same statute at risk, the draw planted to sign - it stands, nothing recorded beside its passage");
                        bool stands2; DivisionRecord passage2, vote2;
                        using (PresidentialVeto.PlantDraw(0.0)) { stands2 = Gate(abstaining, PresidentialVeto.Act.OrdinaryStatute, out passage2, out vote2); }
                        Check(!stands2 && passage2.Motion && vote2 != null && !vote2.Passed
                              && vote2.Sides.Exists(s => s.Abbrev == "PiS" && s.Side == 0 && s.Reason.Contains("abstained on the statute") && s.ReasonShort.EndsWith("abstains", StringComparison.Ordinal)),
                            F("F3: PiS abstaining - at risk; the draw planted to veto, the veto stands and the backing party abstains on the override as on the statute: \"{0}\"", vote2?.Title));
                        bool stands5; DivisionRecord passage5, none5;
                        using (PresidentialVeto.PlantDraw(0.0)) { stands5 = Gate(supporting, PresidentialVeto.Act.OrdinaryStatute, out passage5, out none5); }
                        Check(stands5 && none5 == null && !passage5.Motion, "F3: PiS for - not at risk; even a draw planted to veto is never taken, the statute stands");
                        // the review's untested path: an override that carries - PiS 150 against, 310 for of 460 voting, 276 needed
                        var overridable = new List<DivisionSide> { Side("PiS", 150, -1), Side("KO", 200, 1), Side("TD", 80, 1), Side("NL", 30, 1) };
                        bool stands3; DivisionRecord passage3, overridden;
                        using (PresidentialVeto.PlantDraw(0.0)) { stands3 = Gate(overridable, PresidentialVeto.Act.OrdinaryStatute, out passage3, out overridden); }
                        Check(stands3 && passage3.Motion && overridden != null && overridden.Passed && overridden.Required == 276 && overridden.Title.Contains("overridden, 310 for, 276 needed"),
                            F("§761: an override that carries - the statute stands on the vote on the veto, its ceremony that vote's: \"{0}\"", overridden?.Title));
                        // the review's defect 2: a constitutional amendment never comes back, whoever opposes it (Art. 235 ust. 7)
                        bool stands4; DivisionRecord passage4, none4;
                        using (PresidentialVeto.PlantDraw(0.0)) { stands4 = Gate(against, PresidentialVeto.Act.ConstitutionalAmendment, out passage4, out none4); }
                        Check(stands4 && none4 == null && !passage4.Motion && LawCatalog.GetById("constitutional_debt_brake_act")?.ConstitutionalAmendment == true,
                            "§761: a constitutional amendment PiS opposes stands unvetoed, nothing recorded, a draw planted to veto never taken; the debt brake is the catalog's amendment");
                        // the uncontested passage - no sides, as the fund act's: no club voting, so never at risk, even against a draw planted to veto
                        bool standsEmpty; DivisionRecord passageEmpty, noneEmpty;
                        using (PresidentialVeto.PlantDraw(0.0)) { standsEmpty = Gate(new List<DivisionSide>(), PresidentialVeto.Act.OrdinaryStatute, out passageEmpty, out noneEmpty); }
                        Check(standsEmpty && noneEmpty == null && !passageEmpty.Motion, "F3: an uncontested passage (no sides) - no club voting, never at risk; a draw planted to veto never taken");
                        // the review's finding: the gate's own draw, UNPLANTED, on whatever seed the bar runs. The clock is stepped one day off the start, and two
                        // titles are found: one keyed below the risk on the gate's day and at or above it on the start's, one the other way round. The first is
                        // vetoed and the second signed - a key that dropped the title could not split them, and one keyed on the start's day (the turn's
                        // opening) would invert both - and no random stream moves (the draw is a hash of its key, never a stream's)
                        DateTime startDay = sim.CurrentDate;
                        sim.AdvanceDay();
                        PresidentialVeto.Outcome expected = PresidentialVeto.Decide(CountryId.Poland, pl.PresidentialElections, sim.CurrentDate, PresidentialVeto.Act.OrdinaryStatute, against);
                        string vetoTitle = null, signTitle = null;
                        for (int k = 0; k < 400 && expected != null && (vetoTitle == null || signTitle == null); k++)
                        {
                            string t = "Enact: an unplanted statute " + k.ToString(CultureInfo.InvariantCulture);
                            bool belowToday = PresidentialVeto.Draw(SimulationRandom.MasterSeed, CountryId.Poland, sim.CurrentDate, t) < expected.Risk;
                            bool belowAtStart = PresidentialVeto.Draw(SimulationRandom.MasterSeed, CountryId.Poland, startDay, t) < expected.Risk;
                            if (belowToday && !belowAtStart) { vetoTitle ??= t; } else if (!belowToday && belowAtStart) { signTitle ??= t; }
                        }
                        bool found = expected != null && expected.AtRisk && vetoTitle != null && signTitle != null;
                        Check(found, F("F3: on {0} the bar's seed keys one title below the risk ({1:0.000}) and one at or above it, each on the other side on the start's day {2}: \"{3}\", \"{4}\"",
                            sim.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), expected?.Risk, startDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), vetoTitle, signTitle));
                        if (found)
                        {
                            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
                            bool vetoStood = Gate(against, PresidentialVeto.Act.OrdinaryStatute, out DivisionRecord passageV, out DivisionRecord voteV, vetoTitle);
                            bool signStood = Gate(against, PresidentialVeto.Act.OrdinaryStatute, out DivisionRecord passageS, out DivisionRecord voteS, signTitle);
                            Dictionary<SimulationRandom.Stream, int> drawsNow = SimulationRandom.CaptureDrawCounts();
                            bool streamsStill = drawsWas.Count == drawsNow.Count && drawsWas.All(kv => drawsNow.TryGetValue(kv.Key, out int n) && n == kv.Value);
                            Check(!vetoStood && passageV.Motion && voteV != null && voteV.Title.StartsWith("Vetoed by the President", StringComparison.Ordinal)
                                  && signStood && !passageS.Motion && voteS == null && streamsStill,
                                F("F3: the gate UNPLANTED - \"{0}\" (keyed below the risk) vetoed, \"{1}\" (at or above it) signed, on one day; no random stream moved", vetoTitle, signTitle));
                        }
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }

                // (i) §761 - THE GAME'S OWN BILL PATH: the default epoch (the 10th Sejm, Tusk's government, Nawrocki president), KO the player's party; a law
                // the chamber passes, at risk of a veto no override would carry, is introduced and stepped to its resolution - its effect withheld, its passage a
                // motion, the vote on the veto recorded
                var host = new GameObject("PresidentialVetoDiagnostic bill path");
                try
                {
                    using (PoliSim.Simulation.SimulationManager.EpochScope())
                    {
                        World world = WorldFactory.CreateDefault();
                        var sim = host.AddComponent<PoliSim.Simulation.SimulationManager>();
                        sim.SetWorld(world);
                        sim.PlayerCountryId = CountryId.Poland;
                        Country pl = world.GetCountry(CountryId.Poland);
                        pl.PlayerPartyAbbrev = "KO";
                        LawDefinition chosen = null;
                        int offered = 0, passing = 0, pisAgainst = 0, pisAgainstPassing = 0, pisFor = 0, pisUndecided = 0, pisAgainstTdAbstains = 0, atRiskPassing = 0, overrideWouldCarry = 0;
                        double expectedVetoes = 0.0;
                        foreach (LawDefinition law in LawCatalog.All)
                        {
                            if (law.ConstitutionalAmendment || pl.EnactedLaws.Exists(e => e.LawId == law.Id) || !LawCatalog.IsWithinCompetence(world, pl, law)) { continue; }
                            offered++;
                            BillConcern concern = PoliSim.Simulation.ParliamentSystem.GetLawBillConcern(pl, new LawBill { LawId = law.Id });
                            bool passes = PoliSim.Simulation.ParliamentSystem.WouldBillPass(pl, concern);
                            var sides = new List<DivisionSide>();
                            foreach (PoliSim.Simulation.PartyStance s in PoliSim.Simulation.StanceModel.Stances(pl, concern)) { sides.Add(new DivisionSide { Abbrev = s.Party.Abbrev, Seats = s.Seats, Side = s.Side }); }
                            int pis = sides.Find(d => d.Abbrev == "PiS")?.Side ?? 0, td = sides.Find(d => d.Abbrev == "TD")?.Side ?? 0;
                            if (passes) { passing++; }
                            if (pis < 0) { pisAgainst++; if (passes) { pisAgainstPassing++; } if (td == 0) { pisAgainstTdAbstains++; } } else if (pis > 0) { pisFor++; } else { pisUndecided++; }
                            // §766: the veto's encounters on the record's chamber - each statute the Sejm passes, put to the President of the day
                            PresidentialVeto.Outcome met = passes ? PresidentialVeto.Decide(CountryId.Poland, pl.PresidentialElections, sim.CurrentDate, PresidentialVeto.Act.OrdinaryStatute, sides) : null;
                            if (met != null && met.AtRisk) { atRiskPassing++; expectedVetoes += met.Risk; if (met.OverrideCarries) { overrideWouldCarry++; } }
                        }
                        sb.Append(F("    info      §761/§766, F3: the catalog on {0}, the record's chamber: {1} laws offered, {2} the Sejm passes; PiS against {3} ({4} of them passing; TD abstaining on {7} of them), for {5}, undecided {6}; {8} the Sejm passes are at risk of the veto - an override would carry on {9}; at the president's rate the draw vetoes {10:0.0} of them on average\n",
                            sim.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), offered, passing, pisAgainst, pisAgainstPassing, pisFor, pisUndecided, pisAgainstTdAbstains,
                            atRiskPassing, overrideWouldCarry, expectedVetoes));
                        // the path is proved on a PLANTED chamber where KO alone carries a bill (the record's chamber is measured above)
                        foreach (KeyValuePair<string, int> kv in new Dictionary<string, int> { { "PiS", 194 }, { "KO", 262 }, { "TD", 0 }, { "NL", 0 }, { "Konf", 4 } }) { pl.ParliamentSeats[kv.Key] = kv.Value; }
                        foreach (LawDefinition law in LawCatalog.All)
                        {
                            if (law.ConstitutionalAmendment || pl.EnactedLaws.Exists(e => e.LawId == law.Id) || !LawCatalog.IsWithinCompetence(world, pl, law)) { continue; }
                            BillConcern concern = PoliSim.Simulation.ParliamentSystem.GetLawBillConcern(pl, new LawBill { LawId = law.Id });
                            if (!PoliSim.Simulation.ParliamentSystem.WouldBillPass(pl, concern)) { continue; }
                            var sides = new List<DivisionSide>();
                            foreach (PoliSim.Simulation.PartyStance s in PoliSim.Simulation.StanceModel.Stances(pl, concern)) { sides.Add(new DivisionSide { Abbrev = s.Party.Abbrev, Seats = s.Seats, Side = s.Side }); }
                            PresidentialVeto.Outcome projected = PresidentialVeto.Decide(CountryId.Poland, pl.PresidentialElections, sim.CurrentDate, PresidentialVeto.Act.OrdinaryStatute, sides);
                            if (projected != null && projected.AtRisk && !projected.OverrideCarries) { chosen = law; break; }
                        }
                        Check(chosen != null, F("§761: on {0}, on the planted chamber (KO 262, PiS 194, Konf 4), the catalog holds a law the Sejm passes, at risk of a veto no override would carry - {1}", sim.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), chosen?.Id ?? "NONE"));
                        if (chosen != null)
                        {
                            bool introduced = sim.IntroduceLawBill(CountryId.Poland, new LawBill { LawId = chosen.Id });
                            int before = pl.Divisions.Entries.Count;
                            using (PresidentialVeto.PlantDraw(0.0)) { for (int day = 0; day < 400 && sim.GetPendingLawBill(CountryId.Poland, chosen.Id) != null; day++) { sim.AdvanceLawBillsDay(CountryId.Poland); } }
                            bool enacted = pl.EnactedLaws.Exists(e => e.LawId == chosen.Id);
                            List<DivisionRecord> added = pl.Divisions.Entries.GetRange(before, pl.Divisions.Entries.Count - before);
                            Check(introduced && !enacted && added.Count == 2 && added[0].Motion && added[0].Passed && !added[1].Passed && added[1].Required > 0 && added[1].Title.StartsWith("Vetoed by the President (Karol Nawrocki)", StringComparison.Ordinal),
                                F("§761, F3: \"{0}\" introduced ({1}), resolved through the game's own bill path, the draw planted to veto - enacted {2}; recorded: {3}", chosen.Name, introduced, enacted,
                                    string.Join(" | ", added.Select(d => (d.Motion ? "[motion] " : "") + d.Title))));
                            // F3: the same statute again, the draw planted to sign - the President's answer is the draw's, so it is enacted on its one passage
                            bool introducedAgain = sim.IntroduceLawBill(CountryId.Poland, new LawBill { LawId = chosen.Id });
                            int beforeAgain = pl.Divisions.Entries.Count;
                            using (PresidentialVeto.PlantDraw(1.0)) { for (int day = 0; day < 400 && sim.GetPendingLawBill(CountryId.Poland, chosen.Id) != null; day++) { sim.AdvanceLawBillsDay(CountryId.Poland); } }
                            bool enactedAgain = pl.EnactedLaws.Exists(e => e.LawId == chosen.Id);
                            List<DivisionRecord> addedAgain = pl.Divisions.Entries.GetRange(beforeAgain, pl.Divisions.Entries.Count - beforeAgain);
                            Check(introducedAgain && enactedAgain && addedAgain.Count == 1 && addedAgain[0].Passed && !addedAgain[0].Motion,
                                F("F3: \"{0}\" again, the draw planted to sign - enacted {1} on its one passage; recorded: {2}", chosen.Name, enactedAgain,
                                    string.Join(" | ", addedAgain.Select(d => (d.Motion ? "[motion] " : "") + d.Title))));
                        }
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(host); }

                // (j) §768 (Elias's ruling D4): POLAND'S BUDGET IS TWO ACTS - a player's budget moving one rate and one spending line, on the planted chamber
                // (KO 262, PiS 194, Konf 4): the budget act adopted and applied; the rate in its own tax act; where PiS does not vote for it the act is at risk
                // and the draw, planted to veto (F3), vetoes it; the veto stands, and the old rate stands while the spending moves. A rate move the planted Sejm
                // passes, at risk of a veto no override would carry, is searched, as (i) does.
                var budgetHost = new GameObject("PresidentialVetoDiagnostic budget");
                try
                {
                    using (PoliSim.Simulation.SimulationManager.EpochScope())
                    {
                        World world = WorldFactory.CreateDefault();
                        var sim = budgetHost.AddComponent<PoliSim.Simulation.SimulationManager>();
                        sim.SetWorld(world);
                        sim.PlayerCountryId = CountryId.Poland;
                        Country pl = world.GetCountry(CountryId.Poland);
                        pl.PlayerPartyAbbrev = "KO";
                        foreach (KeyValuePair<string, int> kv in new Dictionary<string, int> { { "PiS", 194 }, { "KO", 262 }, { "TD", 0 }, { "NL", 0 }, { "Konf", 4 } }) { pl.ParliamentSeats[kv.Key] = kv.Value; }
                        TaxLine moved = null; float newRate = 0f;
                        foreach (TaxLine line in pl.TaxLines)
                        {
                            if (!line.IsImplemented || moved != null) { continue; }
                            foreach (float step in new[] { -2f, 2f })
                            {
                                var probe = new BudgetBill(); probe.TaxLines[line.Type] = line.Rate + step;
                                BillConcern concern = PoliSim.Simulation.ParliamentSystem.GetBudgetBillConcern(pl, probe.TaxActPart());
                                if (!PoliSim.Simulation.ParliamentSystem.WouldBillPass(pl, concern)) { continue; }
                                var sides = new List<DivisionSide>();
                                foreach (PoliSim.Simulation.PartyStance s in PoliSim.Simulation.StanceModel.Stances(pl, concern)) { sides.Add(new DivisionSide { Abbrev = s.Party.Abbrev, Seats = s.Seats, Side = s.Side }); }
                                PresidentialVeto.Outcome projected = PresidentialVeto.Decide(CountryId.Poland, pl.PresidentialElections, sim.CurrentDate, PresidentialVeto.Act.OrdinaryStatute, sides);
                                if (projected != null && projected.AtRisk && !projected.OverrideCarries) { moved = line; newRate = line.Rate + step; break; }
                            }
                        }
                        Check(moved != null, F("§768: on the planted chamber a rate move the Sejm passes, at risk of a veto no override would carry - {0}", moved != null ? moved.Type + " " + moved.Rate.ToString("0.##", CultureInfo.InvariantCulture) + " -> " + newRate.ToString("0.##", CultureInfo.InvariantCulture) : "NONE"));
                        if (moved != null)
                        {
                            SpendingLine spend = pl.SpendingLines.First(l => l.Amount > 0f);
                            float rateBefore = moved.Rate, spendBefore = spend.Amount;
                            var bill = new BudgetBill();
                            bill.TaxLines[moved.Type] = newRate;
                            bill.SpendingPercentChanges[spend.Category] = 5f;
                            bool introduced = sim.IntroduceBudgetBill(CountryId.Poland, bill);
                            int before = pl.Divisions.Entries.Count;
                            using (PresidentialVeto.PlantDraw(0.0)) { for (int day = 0; day < 400 && sim.GetPendingBudgetBill(CountryId.Poland) != null; day++) { sim.AdvanceBudgetBillDay(CountryId.Poland); } }
                            List<DivisionRecord> added = pl.Divisions.Entries.GetRange(before, pl.Divisions.Entries.Count - before);
                            bool shape = added.Count == 3 && added[0].Title == "Annual budget bill" && added[0].Passed && !added[0].Motion
                                && added[1].Title.StartsWith("Tax act: ", StringComparison.Ordinal) && added[1].Passed && added[1].Motion
                                && added[2].Title.StartsWith("Vetoed by the President (Karol Nawrocki): Tax act: ", StringComparison.Ordinal) && !added[2].Passed && added[2].Required > 0;
                            Check(introduced && shape && Math.Abs(moved.Rate - rateBefore) < 1e-6f && spend.Amount > spendBefore,
                                F("§768: the budget through the game's own path - the budget act adopted, the spending moved ({0:0.###} -> {1:0.###}); the rate in its own tax act, vetoed, the old rate standing ({2:0.##}); recorded: {3}",
                                    spendBefore, spend.Amount, moved.Rate, string.Join(" | ", added.Select(d => (d.Motion ? "[motion] " : "") + d.Title))));
                            // a budget that changes no rate is one act: nothing beside it
                            var plain = new BudgetBill(); plain.SpendingPercentChanges[spend.Category] = 5f;
                            bool introducedPlain = sim.IntroduceBudgetBill(CountryId.Poland, plain);
                            int beforePlain = pl.Divisions.Entries.Count;
                            for (int day = 0; day < 400 && sim.GetPendingBudgetBill(CountryId.Poland) != null; day++) { sim.AdvanceBudgetBillDay(CountryId.Poland); }
                            Check(introducedPlain && pl.Divisions.Entries.Count == beforePlain + 1 && pl.Divisions.Entries[beforePlain].Title == "Annual budget bill",
                                "§768: a budget that changes no rate is one act - its division alone, no tax act beside it");
                        }

                        // (k) §773 (Elias's ruling E2): "the pension age ... travels in its own act and can be vetoed, like tax rates. General rule for Poland: only
                        // spending stays in the budget act." A pension-age step the planted Sejm passes, at risk of a veto no override would carry, is searched; a budget carrying it and a
                        // spending line is stepped through the game's own path: the budget act carries the spending alone, the age its own pension act, vetoed.
                        float ageInForce = PensionAgeStatute.AgeInForce(pl, pl.CalendarYear);
                        float newAge = -1f;
                        foreach (float step in new[] { 2f, 1f, -1f, -2f })
                        {
                            float age = Mathf.Clamp(ageInForce + step, BudgetBill.PensionAgeMin, BudgetBill.PensionAgeMax);
                            var probe = new BudgetBill { PensionAgeSet = true, PensionAge = age };
                            if (!probe.ChangesPensionAge(pl)) { continue; }
                            BillConcern concern = PoliSim.Simulation.ParliamentSystem.GetBudgetBillConcern(pl, probe.PartOf(BudgetBill.StatutePart.PensionAge));
                            if (!PoliSim.Simulation.ParliamentSystem.WouldBillPass(pl, concern)) { continue; }
                            var sides = new List<DivisionSide>();
                            foreach (PoliSim.Simulation.PartyStance s in PoliSim.Simulation.StanceModel.Stances(pl, concern)) { sides.Add(new DivisionSide { Abbrev = s.Party.Abbrev, Seats = s.Seats, Side = s.Side }); }
                            PresidentialVeto.Outcome projected = PresidentialVeto.Decide(CountryId.Poland, pl.PresidentialElections, sim.CurrentDate, PresidentialVeto.Act.OrdinaryStatute, sides);
                            if (projected != null && projected.AtRisk && !projected.OverrideCarries) { newAge = age; break; }
                        }
                        Check(newAge >= 0f, F("§773: on the planted chamber a pension-age step the Sejm passes, at risk of a veto no override would carry - {0} -> {1}", PensionAgeStatute.Format(ageInForce), newAge >= 0f ? PensionAgeStatute.Format(newAge) : "none found"));
                        if (newAge >= 0f)
                        {
                            SpendingLine spend = pl.SpendingLines.First(l => l.Amount > 0f);
                            float spendBefore = spend.Amount, overrideBefore = pl.PensionAgeOverride;
                            var bill = new BudgetBill { PensionAgeSet = true, PensionAge = newAge };
                            bill.SpendingPercentChanges[spend.Category] = 5f;
                            BudgetBill budgetAct = PoliSim.Simulation.ParliamentSystem.BudgetActOf(pl, bill);
                            Check(!budgetAct.PensionAgeSet && budgetAct.TaxLines.Count == 0 && budgetAct.WelfarePrograms.Count == 0 && !budgetAct.ChangesFund(pl) && budgetAct.SpendingPercentChanges.Count == 1,
                                "§773: the budget act carries the spending alone - no rate, no pension age, no benefit level, the fund as it stands");
                            bool introduced = sim.IntroduceBudgetBill(CountryId.Poland, bill);
                            int before = pl.Divisions.Entries.Count;
                            using (PresidentialVeto.PlantDraw(0.0)) { for (int day = 0; day < 400 && sim.GetPendingBudgetBill(CountryId.Poland) != null; day++) { sim.AdvanceBudgetBillDay(CountryId.Poland); } }
                            List<DivisionRecord> added = pl.Divisions.Entries.GetRange(before, pl.Divisions.Entries.Count - before);
                            bool shape = added.Count == 3 && added[0].Title == "Annual budget bill" && added[0].Passed
                                && added[1].Title.StartsWith("Pension act: the pension age ", StringComparison.Ordinal) && added[1].Passed && added[1].Motion
                                && added[2].Title.StartsWith("Vetoed by the President (Karol Nawrocki): Pension act: ", StringComparison.Ordinal) && !added[2].Passed && added[2].Required > 0;
                            Check(introduced && shape && pl.PensionAgeOverride == overrideBefore && spend.Amount > spendBefore && PoliSim.Simulation.SimulationManager.IsStatuteActTitle(added[1].Title),
                                F("§773: the pension age in its own act, vetoed - the age stays ({0}), the spending moves ({1:0.###} -> {2:0.###}); recorded: {3}",
                                    PensionAgeStatute.Format(PensionAgeStatute.AgeInForce(pl, pl.CalendarYear)), spendBefore, spend.Amount, string.Join(" | ", added.Select(d => (d.Motion ? "[motion] " : "") + d.Title))));
                        }

                        // (l) §773 (E2): "A rate moves only through the tax act (Sejm vote, then the veto), whoever proposes it." The Finance partner's boundary
                        // rates go to the Sejm as a tax act its party tables - run through the game's own method. PLANTED (the second review's findings 3 and 6):
                        // the Written is hand-built - (j)'s searched move, tabled by the head's own party. No partner writes either (Tighten only raises the
                        // household rates; Holder() never names the head's party). The vote reads the move's direction (the concern is signed by it), so the
                        // move is (j)'s: this chamber passes it and the President's veto of it stands; no partner's rise was tried on this chamber. The holder
                        // rides only as TabledBy, which the vote never reads. What PartnerTaxAct does once its act falls is the same whatever the direction,
                        // the tax or the holder, so the fall's handling proved here is the partner's; (m) and (m2) carry a partner-shaped move through the
                        // boundary itself (the third review's findings 3 and 7).
                        if (moved != null)
                        {
                            System.Reflection.MethodInfo partnerAct = typeof(PoliSim.Simulation.SimulationManager).GetMethod("PartnerTaxAct", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            var decision = PolicyDecision.None();
                            decision.TaxRateOverrides[moved.Type] = newRate;
                            var written = new PoliSim.Simulation.FinancePartner.Written { Holder = "KO", StancePoints = -0.25f, RatePoints = -0.10f, RateAmount = 1f };
                            written.Taxes.Add(moved.Type);
                            written.Lines.Add(pl.SpendingLines.First(l => l.Amount > 0f).Category);
                            // §773's review (finding 4): a Poland holding a fund - the partner's act votes its rates alone, never a fund act it did not ask
                            SovereignWealthFund fundBefore = pl.SovereignWealthFund;
                            pl.SovereignWealthFund = new SovereignWealthFund();
                            int beforePartner = pl.Divisions.Entries.Count;
                            using (PresidentialVeto.PlantDraw(0.0)) { partnerAct?.Invoke(sim, new object[] { pl, decision, written, sim.CurrentDate }); }
                            List<DivisionRecord> partnerAdded = pl.Divisions.Entries.GetRange(beforePartner, pl.Divisions.Entries.Count - beforePartner);
                            bool fundKept = pl.SovereignWealthFund != null;
                            pl.SovereignWealthFund = fundBefore;
                            Check(partnerAct != null && partnerAdded.Count == 2 && partnerAdded[0].Title.StartsWith("Tax act: ", StringComparison.Ordinal)
                                  && partnerAdded[1].Title.StartsWith("Vetoed by the President (Karol Nawrocki): Tax act: ", StringComparison.Ordinal)
                                  && partnerAdded.All(d => !d.Title.Contains("Fund act: ")) && fundKept
                                  && !decision.TaxRateOverrides.ContainsKey(moved.Type) && written.Taxes.Count == 0 && Math.Abs(written.StancePoints - (-0.15f)) < 1e-6f && written.RatePoints == 0f
                                  && written.Moves.Count > 0 && written.Moves[written.Moves.Count - 1].Contains("the lines' part stands: -0.15 pp of GDP"),
                                F("§773: the partner's boundary rates as a tax act - voted, vetoed; the rate leaves the turn's decision and the step counts the lines' part alone ({0:0.00} pp); a held fund draws no fund act (PLANTED: (j)'s searched move, a holder no partner could be); recorded: {1}",
                                    written.StancePoints, string.Join(" | ", partnerAdded.Select(d => (d.Motion ? "[motion] " : "") + d.Title))));

                            // the rates alone (the review's finding 3): a step with no lines whose tax act falls - nothing of it lands, nothing is counted, and the
                            // year's step is spent all the same
                            var decisionR = PolicyDecision.None();
                            decisionR.TaxRateOverrides[moved.Type] = newRate;
                            var writtenR = new PoliSim.Simulation.FinancePartner.Written { Holder = "KO", StancePoints = -0.25f, RatePoints = -0.25f, RateAmount = 1f };
                            writtenR.Taxes.Add(moved.Type);
                            string holderBefore = pl.FinanceStanceHolder;
                            float appliedBefore = pl.FinanceStanceApplied;
                            DateTime steppedBefore = pl.FinancePartnerSteppedOn, boundary = sim.CurrentDate.AddDays(1);
                            using (PresidentialVeto.PlantDraw(0.0)) { partnerAct?.Invoke(sim, new object[] { pl, decisionR, writtenR, boundary }); }
                            PoliSim.Simulation.FinancePartner.Record(pl, writtenR, boundary);
                            Check(!writtenR.Any && !decisionR.TaxRateOverrides.ContainsKey(moved.Type) && pl.FinancePartnerSteppedOn == boundary
                                  && pl.FinanceStanceHolder == holderBefore && pl.FinanceStanceApplied == appliedBefore && writtenR.Moves[writtenR.Moves.Count - 1].Contains("nothing of the step lands"),
                                "§773: a rates-only step whose tax act falls - nothing lands, nothing is counted, the year's step spent all the same (PLANTED: (j)'s searched move, a holder no partner could be)");
                            pl.FinancePartnerSteppedOn = steppedBefore;
                        }

                        // (k2) §773's review (finding 8): one bill changing every statute part - its budget act changes none of them and keeps the spending
                        {
                            TaxLine anyRate = pl.TaxLines.First(t => t.IsImplemented);
                            WelfareProgram anyProgram = pl.WelfarePrograms.FirstOrDefault(w => w.IsImplemented);
                            SpendingLine anyLine = pl.SpendingLines.First(l => l.Amount > 0f);
                            var four = new BudgetBill { PensionAgeSet = true, PensionAge = Mathf.Clamp(ageInForce + 1f, BudgetBill.PensionAgeMin, BudgetBill.PensionAgeMax), SwfShouldExist = pl.SovereignWealthFund == null };
                            four.TaxLines[anyRate.Type] = anyRate.Rate + 1f;
                            if (anyProgram != null) { four.WelfarePrograms[anyProgram.Type] = Mathf.Clamp(anyProgram.GenerosityLevel + (anyProgram.GenerosityLevel < 95f ? 5f : -5f), 0f, 100f); }
                            four.SpendingPercentChanges[anyLine.Category] = 5f;
                            BudgetBill fourAct = PoliSim.Simulation.ParliamentSystem.BudgetActOf(pl, four);
                            bool allChanged = BudgetBill.StatuteParts.All(p => four.Changes(p, pl));
                            bool noneKept = BudgetBill.StatuteParts.All(p => !fourAct.Changes(p, pl));
                            bool spendingKept = fourAct.SpendingPercentChanges.Count == 1 && fourAct.SpendingPercentChanges.TryGetValue(anyLine.Category, out float kept) && kept == 5f;
                            Check(anyProgram != null && allChanged && noneKept && spendingKept,
                                F("§773: a bill changing every statute part ({0}) - its budget act changes none of them and keeps the spending", string.Join(", ", BudgetBill.StatuteParts)));
                        }

                        // (k3) a benefits act through the game's own path: a level step the planted Sejm passes, at risk of a veto no override would carry, is searched, as (k) searches the
                        // age; the act is at risk and the draw, planted to veto (F3), vetoes it; the level stays and the spending moves. The second review's finding 4: the search's success, the act's
                        // divisions (the budget act, the benefits act, the veto) and the veto standing are asserted, so a benefits act that could no longer be
                        // vetoed fails here.
                        {
                            WelfareProgram program = pl.WelfarePrograms.FirstOrDefault(w => w.IsImplemented);
                            float levelBefore = program?.GenerosityLevel ?? 0f, levelAsked = -1f;
                            if (program != null)
                            {
                                foreach (float step in new[] { -10f, 10f, -5f, 5f })
                                {
                                    float asked = Mathf.Clamp(levelBefore + step, 0f, 100f);
                                    var probe = new BudgetBill();
                                    probe.WelfarePrograms[program.Type] = asked;
                                    if (!probe.ChangesBenefits(pl)) { continue; }
                                    BillConcern concern = PoliSim.Simulation.ParliamentSystem.GetBudgetBillConcern(pl, probe.PartOf(BudgetBill.StatutePart.Benefits));
                                    if (!PoliSim.Simulation.ParliamentSystem.WouldBillPass(pl, concern)) { continue; }
                                    var sides = new List<DivisionSide>();
                                    foreach (PoliSim.Simulation.PartyStance s in PoliSim.Simulation.StanceModel.Stances(pl, concern)) { sides.Add(new DivisionSide { Abbrev = s.Party.Abbrev, Seats = s.Seats, Side = s.Side }); }
                                    PresidentialVeto.Outcome projected = PresidentialVeto.Decide(CountryId.Poland, pl.PresidentialElections, sim.CurrentDate, PresidentialVeto.Act.OrdinaryStatute, sides);
                                    if (projected != null && projected.AtRisk && !projected.OverrideCarries) { levelAsked = asked; break; }
                                }
                            }
                            Check(levelAsked >= 0f, F("§773: on the planted chamber a benefit-level step the Sejm passes, at risk of a veto no override would carry - {0} {1:0.#} -> {2}",
                                program != null ? program.Type.ToString() : "no implemented program", levelBefore, levelAsked >= 0f ? levelAsked.ToString("0.#", CultureInfo.InvariantCulture) : "none found"));
                            if (levelAsked >= 0f)
                            {
                                SpendingLine spend = pl.SpendingLines.First(l => l.Amount > 0f);
                                float spendBefore = spend.Amount;
                                var bill = new BudgetBill();
                                bill.WelfarePrograms[program.Type] = levelAsked;
                                bill.SpendingPercentChanges[spend.Category] = 5f;
                                bool introduced = sim.IntroduceBudgetBill(CountryId.Poland, bill);
                                int before = pl.Divisions.Entries.Count;
                                using (PresidentialVeto.PlantDraw(0.0)) { for (int day = 0; day < 400 && sim.GetPendingBudgetBill(CountryId.Poland) != null; day++) { sim.AdvanceBudgetBillDay(CountryId.Poland); } }
                                List<DivisionRecord> added = pl.Divisions.Entries.GetRange(before, pl.Divisions.Entries.Count - before);
                                bool shape = added.Count == 3 && added[0].Title == "Annual budget bill" && added[0].Passed
                                    && added[1].Title.StartsWith("Benefits act: ", StringComparison.Ordinal) && added[1].Title.Contains(" to " + levelAsked.ToString("0.#", CultureInfo.InvariantCulture)) && added[1].Passed && added[1].Motion
                                    && added[2].Title.StartsWith("Vetoed by the President (Karol Nawrocki): Benefits act: ", StringComparison.Ordinal) && !added[2].Passed && added[2].Required > 0;
                                Check(introduced && shape && PoliSim.Simulation.SimulationManager.IsStatuteActTitle(added[1].Title) && Math.Abs(program.GenerosityLevel - levelBefore) < 1e-4f && spend.Amount > spendBefore,
                                    F("§773: the benefit level in its own act, vetoed - the level stays ({0:0.#}), the spending moves ({1:0.###} -> {2:0.###}); recorded: {3}",
                                        program.GenerosityLevel, spendBefore, spend.Amount, string.Join(" | ", added.Select(d => (d.Motion ? "[motion] " : "") + d.Title))));
                            }
                        }

                        // (k4) a fund act - created through the game's own path: uncontested by construction, so it passes and is signed. No side is recorded
                        // (the second review's finding 7: asserted, so the declared premise trips here the day the fund's terms get a concern).
                        {
                            SovereignWealthFund fundBefore = pl.SovereignWealthFund;
                            pl.SovereignWealthFund = null;
                            var bill = new BudgetBill { SwfShouldExist = true, SwfContributionRatePercent = 1f, SwfDomesticAllocationPercent = 50f, SwfEquitiesWeight = 40f, SwfBondsWeight = 30f, SwfInfrastructureWeight = 15f, SwfRealEstateWeight = 15f };
                            bill.SpendingPercentChanges[pl.SpendingLines.First(l => l.Amount > 0f).Category] = 5f;
                            bool introduced = sim.IntroduceBudgetBill(CountryId.Poland, bill);
                            int before = pl.Divisions.Entries.Count;
                            using (PresidentialVeto.PlantDraw(0.0)) { for (int day = 0; day < 400 && sim.GetPendingBudgetBill(CountryId.Poland) != null; day++) { sim.AdvanceBudgetBillDay(CountryId.Poland); } }
                            List<DivisionRecord> added = pl.Divisions.Entries.GetRange(before, pl.Divisions.Entries.Count - before);
                            DivisionRecord fundAct = added.FirstOrDefault(d => d.Title.StartsWith("Fund act: ", StringComparison.Ordinal));
                            bool created = pl.SovereignWealthFund != null;
                            pl.SovereignWealthFund = fundBefore;
                            Check(introduced && fundAct != null && fundAct.Passed && fundAct.Sides.Count == 0 && fundAct.Title == "Fund act: the sovereign wealth fund created" && PoliSim.Simulation.SimulationManager.IsStatuteActTitle(fundAct.Title)
                                  && !added.Any(d => d.Title.StartsWith("Vetoed by the President", StringComparison.Ordinal)) && created,
                                F("§773: a fund act through the game's own path - uncontested (no side recorded), passed and signed, the fund created; recorded: {0}", string.Join(" | ", added.Select(d => (d.Motion ? "[motion] " : "") + d.Title))));
                        }

                        // (k4b) the second and third reviews (findings 7 and 8; 1 and 5): a standing fund's rules moved - a PLANTED fund, every figure set here
                        // and moved inside its clamp by construction: the contribution and the domestic allocation each moved, equities up and bonds down by the
                        // same step, so the infrastructure and real-estate shares do not move and are not named. The act names each rule it moves - the weights
                        // raw, as the Fund tab says them, and the shares of the fund they come to beside them. The expected title is built here from the planted
                        // figures, never transcribed.
                        CultureInfo invariant = CultureInfo.InvariantCulture;
                        string RunFund(SovereignWealthFund planted, BudgetBill bill, out bool passedUncontested, out bool landed)
                        {
                            SovereignWealthFund fundBefore = pl.SovereignWealthFund;
                            pl.SovereignWealthFund = planted;
                            bill.SpendingPercentChanges[pl.SpendingLines.First(l => l.Amount > 0f).Category] = 5f;
                            bool introduced = sim.IntroduceBudgetBill(CountryId.Poland, bill);
                            int before = pl.Divisions.Entries.Count;
                            using (PresidentialVeto.PlantDraw(0.0)) { for (int day = 0; day < 400 && sim.GetPendingBudgetBill(CountryId.Poland) != null; day++) { sim.AdvanceBudgetBillDay(CountryId.Poland); } }
                            List<DivisionRecord> added = pl.Divisions.Entries.GetRange(before, pl.Divisions.Entries.Count - before);
                            DivisionRecord fundAct = added.FirstOrDefault(d => d.Title.StartsWith("Fund act: ", StringComparison.Ordinal));
                            SovereignWealthFund now = pl.SovereignWealthFund;
                            landed = now != null && Math.Abs(now.ContributionRatePercent - bill.SwfContributionRatePercent) < 1e-4f && Math.Abs(now.DomesticAllocationPercent - bill.SwfDomesticAllocationPercent) < 1e-4f
                                && Math.Abs(now.EquitiesWeight - bill.SwfEquitiesWeight) < 1e-4f && Math.Abs(now.BondsWeight - bill.SwfBondsWeight) < 1e-4f
                                && Math.Abs(now.InfrastructureWeight - bill.SwfInfrastructureWeight) < 1e-4f && Math.Abs(now.RealEstateWeight - bill.SwfRealEstateWeight) < 1e-4f;
                            pl.SovereignWealthFund = fundBefore;
                            passedUncontested = introduced && fundAct != null && fundAct.Passed && fundAct.Sides.Count == 0 && !added.Any(d => d.Title.StartsWith("Vetoed by the President", StringComparison.Ordinal));
                            return fundAct?.Title;
                        }
                        {
                            var planted = new SovereignWealthFund { ContributionRatePercent = 1f, DomesticAllocationPercent = 50f, EquitiesWeight = 40f, BondsWeight = 30f, InfrastructureWeight = 15f, RealEstateWeight = 15f };
                            var bill = new BudgetBill
                            {
                                SwfShouldExist = true, SwfContributionRatePercent = planted.ContributionRatePercent + 1f, SwfDomesticAllocationPercent = planted.DomesticAllocationPercent + 10f,
                                SwfEquitiesWeight = planted.EquitiesWeight + 10f, SwfBondsWeight = planted.BondsWeight - 10f, SwfInfrastructureWeight = planted.InfrastructureWeight, SwfRealEstateWeight = planted.RealEstateWeight,
                            };
                            var mix = new SovereignWealthFund { EquitiesWeight = bill.SwfEquitiesWeight, BondsWeight = bill.SwfBondsWeight, InfrastructureWeight = bill.SwfInfrastructureWeight, RealEstateWeight = bill.SwfRealEstateWeight };
                            string ShareWords(string words, SovereignWealthAssetClass assetClass) =>
                                words + " " + (planted.GetNormalizedWeight(assetClass) * 100f).ToString("0.#", invariant) + " % to " + (mix.GetNormalizedWeight(assetClass) * 100f).ToString("0.#", invariant) + " %";
                            string expected = "Fund act: the sovereign wealth fund's rules - its contribution " + planted.ContributionRatePercent.ToString("0.0#", invariant) + " % to " + bill.SwfContributionRatePercent.ToString("0.0#", invariant) + " % of GDP a year"
                                + "; its domestic allocation " + planted.DomesticAllocationPercent.ToString("0.0#", invariant) + " % to " + bill.SwfDomesticAllocationPercent.ToString("0.0#", invariant) + " % of the fund"
                                + "; its equities weight " + planted.EquitiesWeight.ToString("0.#", invariant) + " to " + bill.SwfEquitiesWeight.ToString("0.#", invariant)
                                + "; its bonds weight " + planted.BondsWeight.ToString("0.#", invariant) + " to " + bill.SwfBondsWeight.ToString("0.#", invariant)
                                + "; its shares of the fund: " + ShareWords("equities", SovereignWealthAssetClass.Equities) + ", " + ShareWords("bonds", SovereignWealthAssetClass.Bonds);
                            string title = RunFund(planted, bill, out bool uncontested, out bool landed);
                            Check(uncontested && landed && title == expected,
                                F("§773: a standing fund's rules moved through the game's own path - each rule it moves named, the weights raw and the moved shares beside them, the unmoved shares not named; uncontested, passed, the rules landed; titled: {0}", title ?? "no fund act"));
                        }

                        // (k4c) the third review's finding 1: the skip pinned by its purpose - a PLANTED mix whose weights do not sum to a hundred, one weight
                        // moved by one point: a share that moves by less than the tenth it is printed to is not named, so no share reads "X % to X %"
                        {
                            var planted = new SovereignWealthFund { EquitiesWeight = 40f, BondsWeight = 30f, InfrastructureWeight = 21f, RealEstateWeight = 7f };
                            var bill = new BudgetBill
                            {
                                SwfShouldExist = true, SwfContributionRatePercent = planted.ContributionRatePercent, SwfDomesticAllocationPercent = planted.DomesticAllocationPercent,
                                SwfEquitiesWeight = planted.EquitiesWeight + 1f, SwfBondsWeight = planted.BondsWeight, SwfInfrastructureWeight = planted.InfrastructureWeight, SwfRealEstateWeight = planted.RealEstateWeight,
                            };
                            // the step's words before the run - the act's apply moves the planted fund itself, as it moves the country's
                            string stepWords = "its equities weight " + planted.EquitiesWeight.ToString("0.#", invariant) + " to " + bill.SwfEquitiesWeight.ToString("0.#", invariant);
                            string title = RunFund(planted, bill, out bool uncontested, out bool landed);
                            bool noEqualPair = title != null && !System.Text.RegularExpressions.Regex.IsMatch(title, @"(\d+(?:\.\d)?) % to \1 %");
                            bool named = title != null && title.Contains(stepWords) && title.Contains("its shares of the fund: equities ") && !title.Contains("real estate");
                            Check(uncontested && landed && noEqualPair && named,
                                F("§773: a weight step whose real-estate share moves less than a tenth - that share not named, no share reading \"X % to X %\" (uncontested and passed {1}, landed {2}, no equal pair {3}, named {4}); titled: {0}",
                                    title ?? "no fund act", uncontested, landed, noEqualPair, named));
                        }
                        Check(BudgetBill.StatuteParts.All(p => PoliSim.Simulation.SimulationManager.IsStatuteActTitle(PoliSim.Simulation.SimulationManager.ActWords(p) + ": anything"))
                              && !PoliSim.Simulation.SimulationManager.IsStatuteActTitle("Annual budget bill"),
                            "§773: the night's scan reads every act's title as a statute act's, and the budget act's as none");
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(budgetHost); }

                // (m) §773's review (finding 9): THE BOUNDARY ITSELF. A PLANTED Poland - the government of record on an epoch in 2024, its head re-planted to
                // NL and Finance to KO, which sits to NL's right and so asks a tightening; every spending line pinned, so the step falls wholly to the household
                // rates. One turn's boundary must put those rates to the Sejm as a tax act, and the turn must apply them only where it stood. (m2) the second
                // review (findings 3 and 5): the same boundary where the act FALLS - the Sejm re-planted to Konfederacja's seats alone (PLANTED; seats change
                // only at an election, and none falls in the epoch's first year), so the chamber rejects the rise: the rates stay, nothing of the step is
                // counted, the year's step is spent all the same, and the fall's cost is on the approval ledger. Together they guard the boundary's order -
                // Record above PartnerTaxAct would count a fallen step whole; PartnerTaxAct below the turn's apply would land fallen rates.
                foreach (bool fall in new[] { false, true })
                {
                    var boundaryHost = new GameObject(fall ? "PresidentialVetoDiagnostic boundary (fall)" : "PresidentialVetoDiagnostic boundary");
                    try
                    {
                        using (PoliSim.Simulation.SimulationManager.EpochScope())
                        {
                            var epoch = new DateTime(2024, 1, 15);
                            PoliSim.Simulation.SimulationManager.SetEpoch(epoch);
                            World world = WorldFactory.CreateDefault();
                            var sim = boundaryHost.AddComponent<PoliSim.Simulation.SimulationManager>();
                            sim.SetWorld(world);
                            sim.PlayerCountryId = CountryId.Poland;
                            Country pl = world.GetCountry(CountryId.Poland);
                            GovernmentRecord g = pl.Government;
                            g.PmParty = "NL";
                            foreach (KeyValuePair<string, List<CabinetPortfolio>> held in g.Portfolios) { held.Value?.Remove(CabinetPortfolio.FinanceTreasury); }
                            if (!g.Portfolios.TryGetValue("KO", out List<CabinetPortfolio> koPosts) || koPosts == null) { g.Portfolios["KO"] = koPosts = new List<CabinetPortfolio>(); }
                            koPosts.Add(CabinetPortfolio.FinanceTreasury);
                            pl.PlayerPartyAbbrev = "NL";
                            foreach (SpendingLine line in pl.SpendingLines) { line.Pinned = true; }
                            if (fall) { foreach (string party in new List<string>(pl.ParliamentSeats.Keys)) { pl.ParliamentSeats[party] = 0; } pl.ParliamentSeats["Konf"] = StatutoryDeputies; }
                            pl.FinancePartnerSteppedOn = DateTime.MinValue; pl.FinanceStanceHolder = null; pl.FinanceStanceApplied = 0f;
                            string holder = PoliSim.Simulation.FinancePartner.Holder(pl);
                            float incomeBefore = pl.TaxLines.First(t => t.Type == TaxType.IncomeTax).Rate, vatBefore = pl.TaxLines.First(t => t.Type == TaxType.VAT).Rate;
                            var decisions = new Dictionary<CountryId, PolicyDecision>();
                            foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                            int before = pl.Divisions.Entries.Count;
                            bool turned = false;
                            // F3: the draw PLANTED to sign - the case is the boundary's order (the act standing or falling in the Sejm), not the President's draw
                            using (PresidentialVeto.PlantDraw(1.0))
                            {
                                for (int day = 0; day < PoliSim.Simulation.SimulationManager.DaysPerTurn + 1 && !turned; day++) { if (sim.AdvanceDay()) { sim.AdvanceTurn(decisions); turned = true; } }
                            }
                            List<DivisionRecord> added = pl.Divisions.Entries.GetRange(before, pl.Divisions.Entries.Count - before);
                            DivisionRecord taxAct = added.FirstOrDefault(d => d.Title.StartsWith("Tax act: ", StringComparison.Ordinal));
                            bool vetoStood = added.Any(d => d.Title.StartsWith("Vetoed by the President", StringComparison.Ordinal) && d.Title.Contains("Tax act: ") && !d.Passed);
                            bool stood = taxAct != null && taxAct.Passed && !vetoStood;
                            float incomeAfter = pl.TaxLines.First(t => t.Type == TaxType.IncomeTax).Rate, vatAfter = pl.TaxLines.First(t => t.Type == TaxType.VAT).Rate;
                            DateTime boundary = epoch.AddDays(PoliSim.Simulation.SimulationManager.DaysPerTurn);
                            bool ratesRight = stood ? incomeAfter > incomeBefore : Math.Abs(incomeAfter - incomeBefore) < 1e-6f && Math.Abs(vatAfter - vatBefore) < 1e-6f;
                            bool countRight = pl.FinancePartnerSteppedOn == boundary && (stood ? pl.FinanceStanceHolder == holder && pl.FinanceStanceApplied < 0f : pl.FinanceStanceApplied == 0f);
                            string failedLabel = PoliSim.Simulation.SimulationManager.ActWords(BudgetBill.StatutePart.Rates) + " failed";
                            bool ledgerRight = stood || (pl.ApprovalLedgerLastPeriod != null && pl.ApprovalLedgerLastPeriod.Events.Any(e => e.Label == failedLabel));
                            Check(turned && holder == "KO" && taxAct != null && stood == !fall && ratesRight && countRight && ledgerRight && decisions[CountryId.Poland].TaxRateOverrides.Count == 0,
                                F("§773: the boundary itself (PLANTED: NL heads, Finance planted on KO - the partner read: {10}; every line pinned{0}) - the partner's tightening {1}; income tax {2:0.##} -> {3:0.##}, VAT {4:0.##} -> {5:0.##}; the stance counted {6:+0.00;-0.00} pp; the step spent {7:yyyy-MM-dd}{8}; recorded: {9}",
                                    fall ? "; the Sejm re-planted to Konfederacja's seats alone" : string.Empty,
                                    taxAct == null ? "was put to no vote - no tax act recorded" : stood ? "became a tax act that stood" : "became a tax act that fell",
                                    incomeBefore, incomeAfter, vatBefore, vatAfter, pl.FinanceStanceApplied, pl.FinancePartnerSteppedOn,
                                    !fall ? string.Empty : taxAct == null ? "; nothing on the approval ledger" : ledgerRight ? "; the fall on the approval ledger" : "; the fall NOT on the approval ledger",
                                    string.Join(" | ", added.Where(d => d.Title.Contains("Tax act")).Select(d => (d.Motion ? "[motion] " : "") + d.Title)), holder ?? "none"));
                        }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(boundaryHost); }
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n'); }

            sb.Append(failures == 0 ? "=== PresidentialVetoDiagnostic: the veto and its override, on the law ===" : F("=== PresidentialVetoDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static DivisionSide Side(string abbrev, int seats, int side) => new DivisionSide { Abbrev = abbrev, ShortName = abbrev, Seats = seats, Side = side, Alignment = side };

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
