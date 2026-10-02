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
    /// Budget-related acts are ordinary statutes and can be vetoed."</i> - <see cref="Vetoes"/>, backtested in (g) on the 10th Sejm's record
    /// (`ElectionsData/poland/veto_record.csv`, `third_readings_term10.csv`, from the Sejm API's per-MP votes, clubs summed). B2: <i>"3/5 of the deputies
    /// voting, abstentions included in the base ... Required = ceil(0.6 × (yes + no + abstain))"</i> - <see cref="Required"/>, checked in (f) against
    /// every override vote of the term. **LIVE since §761**: the helpers below read the runtime class (`PresidentialVeto`) the Sejm's statutes pass
    /// through, so the backtest and the constants test the rule the game runs; (h) drives the gate itself on planted divisions (vetoed, overridden, an
    /// amendment), and (i) drives one statute through the game's own bill path - introduced, resolved, its effect withheld where the veto stands.</para>
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

        /// <summary>B1 (Elias's ruling, 2026-10-02): the President vetoes an ordinary statute when a majority of his backing party's deputies - its
        /// MEMBERS at the vote, absent ones included - voted against it; never an act exempt (the budget act, Art. 224; a constitutional amendment,
        /// Art. 235 ust. 7).</summary>
        private static bool Vetoes(bool exempt, int backingNo, int backingMembers) =>
            PresidentialVeto.Vetoes(exempt ? PresidentialVeto.Act.BudgetAct : PresidentialVeto.Act.OrdinaryStatute, backingMembers, backingNo);   // §761: the live rule's

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

                // (g) B1 (Elias's ruling, 2026-10-02): the President vetoes an ordinary statute when a majority of his backing party's deputies voted
                // against it; the budget act (Art. 224) and a constitutional amendment (Art. 235 ust. 7) cannot be vetoed; a budget-related act is an
                // ordinary statute. Backtested on the 10th Sejm's record: every veto, and every vetoable act the President decided on.
                Check(veto.Contains("Prezydent Rzeczypospolitej podpisuje ustawę w ciągu 21 dni od dnia przedstawienia i zarządza jej ogłoszenie") && !Vetoes(exempt: true, 100, 100),
                    "Art. 235 ust. 7 - a constitutional amendment is signed within 21 days, never returned (and the budget act, Art. 224, above)");
                int hits = 0, missesDuda = 0, missesNawrocki = 0, hitsDuda = 0, hitsNawrocki = 0;
                var misses = new List<string>();
                foreach (Dictionary<string, string> v in vetoes)
                {
                    int pisNo = int.Parse(v["pis_no"], CultureInfo.InvariantCulture), pisMembers = int.Parse(v["pis_members"], CultureInfo.InvariantCulture);
                    bool byDuda = v["president"] == "Andrzej Duda";
                    if (Vetoes(exempt: false, pisNo, pisMembers)) { hits++; if (byDuda) { hitsDuda++; } else { hitsNawrocki++; } continue; }
                    if (byDuda) { missesDuda++; } else { missesNawrocki++; }
                    misses.Add(F("{0} {1}: {2} - PiS {3} for, {4} against, {5} abstaining, {6} absent of {7}{8}", v["president"], v["veto_date"], v["act_title"],
                        v["pis_yes"], pisNo, v["pis_abstain"], v["pis_absent"], pisMembers,
                        string.IsNullOrEmpty(v["rozwojplus_no"]) ? string.Empty : F(" (RozwojPlus {0} / {1} / {2} / {3} absent)", v["rozwojplus_yes"], v["rozwojplus_no"], v["rozwojplus_abstain"], v["rozwojplus_absent"])));
                }
                foreach (string m in misses) { sb.Append("    miss      ").Append(m).Append('\n'); }
                Check(vetoes.Count == 53 && hitsDuda == 7 && missesDuda == 1 && hitsNawrocki == 38 && missesNawrocki == 7,
                    F("B1's rule against the record's {0} vetoes: {1} caught - Duda {2} of {3}, Nawrocki {4} of {5}; the {6} missed listed above", vetoes.Count, hits,
                        hitsDuda, hitsDuda + missesDuda, hitsNawrocki, hitsNawrocki + missesNawrocki, misses.Count));
                // the acts the rule would veto that the President signed or referred to the Tribunal - by the president on the day of the decision
                var table = new Dictionary<string, int>();
                foreach (Dictionary<string, string> r in ReadCsv(Path.Combine(root, "third_readings_term10.csv")))
                {
                    if (!string.IsNullOrEmpty(r["veto_exempt"])) { continue; }
                    string outcome = r["outcome"].StartsWith("vetoed", StringComparison.Ordinal) ? "vetoed" : r["outcome"].StartsWith("signed", StringComparison.Ordinal) ? "signed"
                        : r["outcome"].StartsWith("referred to Tribunal", StringComparison.Ordinal) ? "Tribunal" : null;
                    if (outcome == null) { continue; }   // undecided: at the Senate, at the President, Senate amendments pending
                    DateTime on = DateTime.ParseExact(r["outcome_date"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    if (!PresidencyOfRecord.TryAt(CountryId.Poland, on, out PresidencyOfRecord.President who)) { continue; }
                    bool rule = Vetoes(exempt: false, int.Parse(r["pis_no"], CultureInfo.InvariantCulture), int.Parse(r["pis_members"], CultureInfo.InvariantCulture));
                    string key = who.Name + (rule ? " yes " : " no ") + outcome;
                    table[key] = table.TryGetValue(key, out int n) ? n + 1 : 1;
                }
                int T(string k) => table.TryGetValue(k, out int n) ? n : 0;
                foreach (string who in new[] { "Andrzej Duda", "Karol Nawrocki" })
                {
                    sb.Append(F("    table     {0}: the rule says veto - {1} vetoed, {2} signed, {3} to the Tribunal; says sign - {4} vetoed, {5} signed, {6} to the Tribunal\n", who,
                        T(who + " yes vetoed"), T(who + " yes signed"), T(who + " yes Tribunal"), T(who + " no vetoed"), T(who + " no signed"), T(who + " no Tribunal")));
                }
                Check(T("Andrzej Duda yes vetoed") == 7 && T("Andrzej Duda yes signed") == 58 && T("Andrzej Duda yes Tribunal") == 5 && T("Andrzej Duda no vetoed") == 1 && T("Andrzej Duda no signed") == 149 && T("Andrzej Duda no Tribunal") == 3
                      && T("Karol Nawrocki yes vetoed") == 38 && T("Karol Nawrocki yes signed") == 57 && T("Karol Nawrocki yes Tribunal") == 1 && T("Karol Nawrocki no vetoed") == 7 && T("Karol Nawrocki no signed") == 220 && T("Karol Nawrocki no Tribunal") == 2,
                    F("B1's precision on every vetoable act decided: of the acts the rule would veto, Duda vetoed 7, signed 58 and sent 5 to the Tribunal; Nawrocki vetoed 38, signed 57 and sent 1 - {0} of {1} the rule names were vetoed",
                        T("Andrzej Duda yes vetoed") + T("Karol Nawrocki yes vetoed"), T("Andrzej Duda yes vetoed") + T("Andrzej Duda yes signed") + T("Andrzej Duda yes Tribunal") + T("Karol Nawrocki yes vetoed") + T("Karol Nawrocki yes signed") + T("Karol Nawrocki yes Tribunal")));

                // (h) §761 - THE RULE LIVE: the runtime class's constants are the texts'; Decide on the 2023 Sejm's seats; the gate itself on a planted chamber
                Check(PresidentialVeto.StatutoryDeputies == StatutoryDeputies && PresidentialVeto.Quorum == Quorum && PresidentialVeto.OverrideNumerator == OverrideNumerator
                      && PresidentialVeto.OverrideDenominator == OverrideDenominator && !PresidentialVeto.MayVeto(PresidentialVeto.Act.BudgetAct) && !PresidentialVeto.MayVeto(PresidentialVeto.Act.ConstitutionalAmendment),
                    "§761: the live class's constants are the texts' (460, the quorum 230, 3/5), the budget act and an amendment never vetoed");
                var against = new List<DivisionSide> { Side("PiS", 194, -1), Side("KO", 157, 1), Side("TD", 65, 1), Side("NL", 26, 1), Side("Konf", 18, -1) };
                PresidentialVeto.Outcome o = PresidentialVeto.Decide(CountryId.Poland, new DateTime(2026, 1, 1), PresidentialVeto.Act.OrdinaryStatute, against);
                Check(o != null && o.President == "Karol Nawrocki" && o.BackingParty == "PiS" && o.Vetoed && !o.Overridden && o.Yes == 248 && o.Required == 276 && !o.Stands,
                    F("§761: PiS against on 2026-01-01 - {0} vetoes; {1} for, {2} needed - the veto stands", o?.President, o?.Yes, o?.Required));
                var abstaining = new List<DivisionSide> { Side("PiS", 194, 0), Side("KO", 157, 1), Side("TD", 65, 1), Side("NL", 26, 1), Side("Konf", 18, -1) };
                PresidentialVeto.Outcome signed = PresidentialVeto.Decide(CountryId.Poland, new DateTime(2026, 1, 1), PresidentialVeto.Act.OrdinaryStatute, abstaining);
                PresidentialVeto.Outcome budget = PresidentialVeto.Decide(CountryId.Poland, new DateTime(2026, 1, 1), PresidentialVeto.Act.BudgetAct, against);
                PresidentialVeto.Outcome dudaOutcome = PresidentialVeto.Decide(CountryId.Poland, new DateTime(2024, 3, 1), PresidentialVeto.Act.OrdinaryStatute, against);
                Check(signed != null && !signed.Vetoed && signed.Stands && budget != null && !budget.Vetoed && dudaOutcome != null && dudaOutcome.President == "Andrzej Duda" && dudaOutcome.Vetoed
                      && PresidentialVeto.Decide(CountryId.Germany, new DateTime(2026, 1, 1), PresidentialVeto.Act.OrdinaryStatute, against) == null,
                    "§761: PiS abstaining - signed (the record's bloc abstentions); the budget act - signed; Duda on 2024-03-01 - vetoes; Germany - no veto the game runs");
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
                        bool Gate(List<DivisionSide> sides, PresidentialVeto.Act act, out DivisionRecord passage, out DivisionRecord vote)
                        {
                            passage = pl.Divisions.Append("Enact: a planted statute", sim.CurrentDate, 0.5f, true, 1f, 0, new List<DivisionSide>(sides));
                            int before = pl.Divisions.Entries.Count;
                            bool stood = (bool)gate.Invoke(sim, new object[] { pl, passage, true, act });
                            vote = pl.Divisions.Entries.Count > before ? pl.Divisions.Entries[pl.Divisions.Entries.Count - 1] : null;
                            return stood;
                        }
                        bool stands = Gate(against, PresidentialVeto.Act.OrdinaryStatute, out DivisionRecord passage1, out DivisionRecord vetoed);
                        Check(!stands && passage1.Motion && vetoed != null && !vetoed.Passed && !vetoed.Motion && vetoed.Required == 276 && vetoed.Contest == null
                              && vetoed.Title.StartsWith("Vetoed by the President", StringComparison.Ordinal) && vetoed.Title.Contains("the veto stands, 248 for, 276 needed"),
                            F("§761: the gate on Poland's start ({0}) - the statute does not stand; the passage keeps no ceremony; the vote on the veto, its line 276 and no budget contest: \"{1}\"",
                                sim.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), vetoed?.Title));
                        bool stands2 = Gate(abstaining, PresidentialVeto.Act.OrdinaryStatute, out DivisionRecord passage2, out DivisionRecord none2);
                        Check(stands2 && none2 == null && !passage2.Motion, "§761: PiS abstaining - the statute stands, nothing recorded beside its passage");
                        // the review's untested path: an override that carries - PiS 150 against, 310 for of 460 voting, 276 needed
                        var overridable = new List<DivisionSide> { Side("PiS", 150, -1), Side("KO", 200, 1), Side("TD", 80, 1), Side("NL", 30, 1) };
                        bool stands3 = Gate(overridable, PresidentialVeto.Act.OrdinaryStatute, out DivisionRecord passage3, out DivisionRecord overridden);
                        Check(stands3 && passage3.Motion && overridden != null && overridden.Passed && overridden.Required == 276 && overridden.Title.Contains("overridden, 310 for, 276 needed"),
                            F("§761: an override that carries - the statute stands on the vote on the veto, its ceremony that vote's: \"{0}\"", overridden?.Title));
                        // the review's defect 2: a constitutional amendment never comes back, whoever opposes it (Art. 235 ust. 7)
                        bool stands4 = Gate(against, PresidentialVeto.Act.ConstitutionalAmendment, out DivisionRecord passage4, out DivisionRecord none4);
                        Check(stands4 && none4 == null && !passage4.Motion && LawCatalog.GetById("constitutional_debt_brake_act")?.ConstitutionalAmendment == true,
                            "§761: a constitutional amendment PiS opposes stands unvetoed, nothing recorded; the debt brake is the catalog's amendment");
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }

                // (i) §761 - THE GAME'S OWN BILL PATH: the default epoch (the 10th Sejm, Tusk's government, Nawrocki president), KO the player's party; a law
                // the chamber passes and PiS opposes is introduced and stepped to its resolution - its effect withheld, its passage a motion, the vote on the veto recorded
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
                        int offered = 0, passing = 0, pisAgainst = 0, pisAgainstPassing = 0, pisFor = 0, pisUndecided = 0, pisAgainstTdAbstains = 0;
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
                        }
                        sb.Append(F("    info      §761 the catalog on {0}, the record's chamber: {1} laws offered, {2} the Sejm passes; PiS against {3} ({4} of them passing; TD abstaining on {7} of them), for {5}, undecided {6}\n",
                            sim.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), offered, passing, pisAgainst, pisAgainstPassing, pisFor, pisUndecided, pisAgainstTdAbstains));
                        // the record's chamber passes nothing PiS opposes (above) - the path is proved on a PLANTED chamber where KO alone carries a bill
                        foreach (KeyValuePair<string, int> kv in new Dictionary<string, int> { { "PiS", 194 }, { "KO", 262 }, { "TD", 0 }, { "NL", 0 }, { "Konf", 4 } }) { pl.ParliamentSeats[kv.Key] = kv.Value; }
                        foreach (LawDefinition law in LawCatalog.All)
                        {
                            if (law.ConstitutionalAmendment || pl.EnactedLaws.Exists(e => e.LawId == law.Id) || !LawCatalog.IsWithinCompetence(world, pl, law)) { continue; }
                            BillConcern concern = PoliSim.Simulation.ParliamentSystem.GetLawBillConcern(pl, new LawBill { LawId = law.Id });
                            if (!PoliSim.Simulation.ParliamentSystem.WouldBillPass(pl, concern)) { continue; }
                            var sides = new List<DivisionSide>();
                            foreach (PoliSim.Simulation.PartyStance s in PoliSim.Simulation.StanceModel.Stances(pl, concern)) { sides.Add(new DivisionSide { Abbrev = s.Party.Abbrev, Seats = s.Seats, Side = s.Side }); }
                            PresidentialVeto.Outcome projected = PresidentialVeto.Decide(CountryId.Poland, sim.CurrentDate, PresidentialVeto.Act.OrdinaryStatute, sides);
                            if (projected != null && projected.Vetoed && !projected.Overridden) { chosen = law; break; }
                        }
                        Check(chosen != null, F("§761: on {0}, on the planted chamber (KO 262, PiS 194, Konf 4), the catalog holds a law the Sejm passes and PiS opposes - {1}", sim.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), chosen?.Id ?? "NONE"));
                        if (chosen != null)
                        {
                            bool introduced = sim.IntroduceLawBill(CountryId.Poland, new LawBill { LawId = chosen.Id });
                            int before = pl.Divisions.Entries.Count;
                            for (int day = 0; day < 400 && sim.GetPendingLawBill(CountryId.Poland, chosen.Id) != null; day++) { sim.AdvanceLawBillsDay(CountryId.Poland); }
                            bool enacted = pl.EnactedLaws.Exists(e => e.LawId == chosen.Id);
                            List<DivisionRecord> added = pl.Divisions.Entries.GetRange(before, pl.Divisions.Entries.Count - before);
                            Check(introduced && !enacted && added.Count == 2 && added[0].Motion && added[0].Passed && !added[1].Passed && added[1].Required > 0 && added[1].Title.StartsWith("Vetoed by the President (Karol Nawrocki)", StringComparison.Ordinal),
                                F("§761: \"{0}\" introduced ({1}), resolved through the game's own bill path - enacted {2}; recorded: {3}", chosen.Name, introduced, enacted,
                                    string.Join(" | ", added.Select(d => (d.Motion ? "[motion] " : "") + d.Title))));
                        }
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(host); }
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
