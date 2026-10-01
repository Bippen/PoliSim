using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-5, PART FOUR (§736): THE PRESIDENT'S VETO AND THE SEJM'S OVERRIDE, PROVEN ON THE LAW BEFORE IT GOES LIVE. The rule as the sources write it
    /// (`ElectionsData/poland/veto.md`): the President signs a statute within 21 days or sends it back with reasons (Konstytucja Art. 122 ust. 2 and 5);
    /// the Sejm overrides by re-passing it with <b>3/5 of the votes, at least half the statutory number of deputies (230 of 460) present</b>, and the
    /// President then signs within 7 days (ust. 5; Regulamin Sejmu Art. 64 ust. 5); <b>a veto the Sejm does not override closes the procedure</b> -
    /// the statute is dead (Regulamin Art. 64 ust. 6); and <b>the budget act and the act on a provisional budget cannot be vetoed</b> - Art. 122 ust. 5
    /// does not apply to them, the President signs them within 7 days (Art. 224 ust. 1).
    ///
    /// <para>Checked: (a) the constants against the held texts, phrase by phrase - so a rule here cannot drift from its source; (b) the override's count,
    /// its edges planted - 276 of 460 overrides and 275 does not; 229 present cannot override even unanimous, 230 present can with 138; (c) the strict
    /// reading of an abstention (a vote cast, [PROVISIONAL] - the denominator is not defined in the sources) on a case where the two readings differ;
    /// (d) on the Sejm of record (2023, the PKW's seats) with the president of record on the day - Duda, then Nawrocki from 2025-08-06: the governing
    /// coalition's 248 seats cannot override alone (276 needed with every deputy voting), nor with Konfederacja's 18; PiS's 194 can; (e) the budget act
    /// exempt.</para>
    ///
    /// <para><b>Not live, and why.</b> Who vetoes what is the game's rule, not the constitution's - DECLARED and owed to Elias (the proposal: the President
    /// vetoes an ordinary statute the party that backed him votes against); the record of the 10th Sejm's vetoes, for its backtest, is not fetched.
    /// Wiring it moves Poland's bills, so it lands as its own sentinel family, with the PiS start and the districts' live count.</para>
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

        /// <summary>Whether the Sejm's re-pass overrides: the quorum present, and FOR at least 3/5 of the votes cast - an abstention a vote cast, the strict
        /// reading [PROVISIONAL]. Integer arithmetic: FOR × 5 ≥ cast × 3.</summary>
        private static bool Overrides(int forVotes, int against, int abstaining, int present)
        {
            if (present < Quorum) { return false; }
            int cast = forVotes + against + abstaining;
            return cast > 0 && forVotes * OverrideDenominator >= cast * OverrideNumerator;
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
                Check(!strict && lenient, F("180 FOR, 100 AGAINST, 30 abstaining: the strict reading [PROVISIONAL] says no (180 of 310 cast, {0:0.0} %), the other yes ({1:0.0} % of FOR and AGAINST) - the case the doctrine's source must settle",
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
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n'); }

            sb.Append(failures == 0 ? "=== PresidentialVetoDiagnostic: the veto and its override, on the law ===" : F("=== PresidentialVetoDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
