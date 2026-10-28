using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// GATE-18 piece two (`POLISIM_FEATURE_LIST.md` row 5; Elias's ruling q18 of 2026-10-18 with K-1): <b>THE EVE-FITTED FACTORS OF THE CHAMBER ELECTIONS OF
    /// RECORD</b> - "an election of record inside a start's window is reproduced by an idle game: eve-fitted factors at the unit the count uses (E1's form)".
    /// <list type="bullet">
    /// <item><description><b>E1's form, at the count's unit.</b> E1 (§772) multiplies a party's poll by a candidate factor fitted once so a world that follows
    /// history reproduces the first round of record. Here the unit is the list the chamber count reads - the party key the count is handed - and the factor
    /// multiplies the share handed to the count (`ParliamentaryCount`) on the polling day of that election of record and on no other day; the product is
    /// renormalised over the lists, so a factor is defined against the shares it meets. A list with no factor in the table carries one (E1's future factor).</description></item>
    /// <item><description><b>[FITTED], once per election, on its own count</b>: n is one - each election's factors are fitted on that election alone, on the
    /// idle game's count of its polling day (the world built as a new game builds it at the start, the player's party doing nothing, the campaign of its eve
    /// counted at the noise at zero), against the record's shares over the roster's own total (the scale the count's shares are on, as the out-of-sample file
    /// of K-1 part 0 reads its shares over the parties the model carries). `Gate18Diagnostic` refits them every run and prints the refit as FIT lines, which `Tools/chamber_factor_patch.pl` patches
    /// into the table below from the log - never retyped.</description></item>
    /// <item><description><b>Unfitted is inert.</b> A table entry holding NaN has had no fit patched in; an election with any unfitted entry carries no factor
    /// at all in play (its count is the model's own, as before), and the gate fails on it by name.</description></item>
    /// <item><description><b>No leak into the out-of-sample figure.</b> Nothing before the count reads this table - the campaign, its AI, the tactical layer and
    /// the poll never see it - so the idle game's shares handed to the count are the model's own prediction, and the same count of those shares at factor one
    /// is the out-of-sample figure the gate prints beside the fit.</description></item>
    /// </list>
    /// </summary>
    public static class RecordFactors
    {
        /// <summary>The chamber elections of record the table carries: those inside a start's window whose country the game counts (`ParliamentaryCount.CountsIn`).</summary>
        public static readonly ElectionVintage[] Elections = { ElectionVintage.Sweden2026, ElectionVintage.Germany2025, ElectionVintage.Poland2023, ElectionVintage.Italy2022 };   // PS-7 (K205): Italy's Camera of 25 Sep 2022, inside its start's window

        /// <summary>
        /// [FITTED] n = 1 per election (that election's own count), on the idle game's count of the polling day of record (the day `WorldClock.Chambers`
        /// dates; the shares handed to it are its eve's campaign), the noise at zero - the record's share over the roster's total divided by the share the
        /// idle game handed the count, refit to the count's fixed point. Patched in from `Gate18Diagnostic`'s FIT lines by `Tools/chamber_factor_patch.pl`;
        /// NaN where no fit has been patched in. Keyed by the election and the party key, in the roster's order.
        /// </summary>
        private static readonly (ElectionVintage Election, string Party, double Factor)[] Fitted =
        {
            (ElectionVintage.Sweden2026, "S", 0.99933090719699225),
            (ElectionVintage.Sweden2026, "SD", 0.85595592772742224),
            (ElectionVintage.Sweden2026, "M", 0.97022736669177057),
            (ElectionVintage.Sweden2026, "V", 1.3250626916285613),
            (ElectionVintage.Sweden2026, "C", 1.2061067444425941),
            (ElectionVintage.Sweden2026, "KD", 0.904466571629495),
            (ElectionVintage.Sweden2026, "MP", 1.1186258162371281),
            (ElectionVintage.Sweden2026, "L", 1.066402011391163),
            (ElectionVintage.Germany2025, "CDU", 1.4561571854783546),
            (ElectionVintage.Germany2025, "AfD", 2.2700389247046266),
            (ElectionVintage.Germany2025, "SPD", 0.7113541479026001),
            (ElectionVintage.Germany2025, "Grune", 0.90652324813575014),
            (ElectionVintage.Germany2025, "Linke", 1.4990825190758004),
            (ElectionVintage.Germany2025, "CSU", 1.0451677792228755),
            (ElectionVintage.Germany2025, "SSW", 1.7499142558899421),
            (ElectionVintage.Germany2025, "BSW", 0.3923045940200347),
            (ElectionVintage.Germany2025, "FDP", 0.40024743159011833),
            (ElectionVintage.Poland2023, "PiS", 1.0063974085158323),
            (ElectionVintage.Poland2023, "KO", 1.1982770732334098),
            (ElectionVintage.Poland2023, "TD", 0.84787763083056267),
            (ElectionVintage.Poland2023, "NL", 0.74335973796708),
            (ElectionVintage.Poland2023, "Konf", 1.0436385770366714),
            (ElectionVintage.Poland2023, "SLD", 1),
            (ElectionVintage.Poland2023, "PSL", 1),
            (ElectionVintage.Poland2023, "MN", 0.71411636220926122),
            // PS-7 (K205): Italy's Camera of 25 Sep 2022 - fitted from Gate18Diagnostic's FIT lines (patched by Tools/chamber_factor_patch.pl from the Italy run's log)
            (ElectionVintage.Italy2022, "FdI", 2.7556079508528666),
            (ElectionVintage.Italy2022, "PD", 1.3099090205481287),
            (ElectionVintage.Italy2022, "Lega", 0.734806712710914),
            (ElectionVintage.Italy2022, "M5S", 0.61198708056273732),
            (ElectionVintage.Italy2022, "FI", 0.79366154800378963),
            (ElectionVintage.Italy2022, "AzIV", 0.47460415849208903),
            (ElectionVintage.Italy2022, "AVS", 1.2354423240932009),
            (ElectionVintage.Italy2022, "NM", 0.82962342623323415),
            (ElectionVintage.Italy2022, "SVP", 1.2019524834966961),
            (ElectionVintage.Italy2022, "PlusE", 1.3082743177439526),
            (ElectionVintage.Italy2022, "IC", 1),
            (ElectionVintage.Italy2022, "ScN", 1),
            (ElectionVintage.Italy2022, "MAIE", 1),
            (ElectionVintage.Italy2022, "UV", 1),
            (ElectionVintage.Italy2022, "LeU", 1),
            (ElectionVintage.Italy2022, "CP", 1),
            (ElectionVintage.Italy2022, "NcI", 1),
            (ElectionVintage.Italy2022, "Misto", 1),
        };

        /// <summary>The country an election of the table belongs to; throws for one the table does not carry.</summary>
        public static CountryId CountryOf(ElectionVintage election)
        {
            switch (election)
            {
                case ElectionVintage.Sweden2026: return CountryId.Sweden;
                case ElectionVintage.Germany2025: return CountryId.Germany;
                case ElectionVintage.Poland2023: return CountryId.Poland;
                case ElectionVintage.Italy2022: return CountryId.Italy;   // PS-7 (K205)
                default: throw new ArgumentException($"the record's factors carry no election {election}");
            }
        }

        /// <summary>The polling day of an election of the table, as `WorldClock.Chambers` dates it.</summary>
        public static DateTime PollingDayOf(ElectionVintage election) => WorldClock.ElectionDayOf(CountryOf(election), election);

        /// <summary>The roster's share of the valid vote at <paramref name="election"/>, as its own returns give it (`PartySystems.TryHistory` at that election, DERIVED):
        /// the base the statutory threshold reads the counted shares on that day - the shares are fractions of the roster's vote, the lists the roster does not
        /// carry the rest of the valid vote - so a fit to the record's shares over the roster's total moves no list across a line the record put it on the
        /// other side of. One where the returns are not on disk.</summary>
        public static double RosterShareOfVote(ElectionVintage election)
        {
            if (!PartySystems.TryHistory(CountryOf(election), out double[] latest, out double[] _, election) || latest == null) { return 1.0; }
            double sum = 0.0;
            foreach (double s in latest) { sum += s; }
            return sum > 0.0 && sum <= 100.0 ? sum / 100.0 : 1.0;
        }

        /// <summary>GATE-18 q3: each counted valkrets's (<paramref name="regionNames"/>, matched by name) roster share of its valid vote at
        /// <paramref name="election"/>, by that election's own returns per valkrets (the lists the catalog carries over its valid vote, DERIVED) - the base
        /// Sweden's 12 % line reads the counted valkretsar on that day, as `RosterShareOfVote` is the 4 % line's. Null for any other election, or where a
        /// name is not in the returns.</summary>
        public static double[] RosterShareByRegion(ElectionVintage election, IReadOnlyList<string> regionNames)
        {
            if (regionNames == null) { return null; }
            string[] names;
            long[][] votes;
            long[] valid;
            if (election == ElectionVintage.Sweden2026) { names = Generated.SwedishValkretsReturns2026.Names; votes = Generated.SwedishValkretsReturns2026.Votes; valid = Generated.SwedishValkretsReturns2026.Valid; }
            else if (election == ElectionVintage.Sweden2022) { names = Generated.SwedishValkretsReturns2022.Names; votes = Generated.SwedishValkretsReturns2022.Votes; valid = Generated.SwedishValkretsReturns2022.Valid; }
            else { return null; }
            var share = new double[regionNames.Count];
            for (int r = 0; r < share.Length; r++)
            {
                int at = Array.IndexOf(names, regionNames[r]);
                if (at < 0 || valid[at] <= 0) { return null; }
                long roster = 0;
                foreach (long v in votes[at]) { roster += v; }
                share[r] = (double)roster / valid[at];
            }
            return share;
        }

        /// <summary>The stored factors of <paramref name="election"/> by party key, NaN where none is patched in.</summary>
        public static Dictionary<string, double> Stored(ElectionVintage election)
        {
            var stored = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var f in Fitted) { if (f.Election == election) { stored[f.Party] = f.Factor; } }
            return stored;
        }

        /// <summary>Whether every entry of <paramref name="election"/> holds a fit; <paramref name="unfitted"/> names the party keys that do not.</summary>
        public static bool IsFitted(ElectionVintage election, out List<string> unfitted)
        {
            unfitted = new List<string>();
            foreach (var f in Fitted) { if (f.Election == election && (double.IsNaN(f.Factor) || double.IsInfinity(f.Factor) || f.Factor < 0.0)) { unfitted.Add(f.Party); } }
            return unfitted.Count == 0;
        }

        /// <summary>The factors <paramref name="country"/>'s count of <paramref name="pollingDay"/> carries: true where that day is an election of record of
        /// the table and every entry of it is fitted; false (nothing to carry) on every other day, and while any entry is unfitted.</summary>
        public static bool TryFor(CountryId country, DateTime pollingDay, out ElectionVintage election, out Dictionary<string, double> factors)
        {
            election = ElectionVintage.Seated;
            factors = null;
            foreach (ElectionVintage e in Elections)
            {
                if (CountryOf(e) != country || PollingDayOf(e) != pollingDay.Date) { continue; }
                if (!IsFitted(e, out _)) { return false; }
                election = e;
                factors = Stored(e);
                return true;
            }
            return false;
        }

        /// <summary>The base each list's factor multiplies at <paramref name="election"/>: the share the game handed, or - for a list handed nothing - its share of
        /// record over the roster's total (RULED K109, 2026-10-24: E1's share-of-record base, as Poland's minor presidential candidates carry), so a list the
        /// model leaves at zero is still fitted to its record. The shares unchanged where the record's are not on disk.</summary>
        public static Dictionary<string, double> Based(ElectionVintage election, IReadOnlyDictionary<string, double> shares)
        {
            Dictionary<string, double> based = Copy(shares);
            if (!PartySystems.TryHistory(CountryOf(election), out double[] record, out double[] _, election) || record == null) { return based; }
            PoliticalParty[] roster = PartySystems.RealRoster(CountryOf(election));
            double total = 0.0;
            foreach (double s in record) { total += s; }
            if (!(total > 0.0)) { return based; }
            for (int i = 0; i < roster.Length && i < record.Length; i++)
            {
                string k = roster[i].Abbrev;
                if (based.TryGetValue(k, out double handed) && !(handed > 0.0) && record[i] > 0.0) { based[k] = record[i] / total; }
            }
            return based;
        }

        /// <summary>The shares the count is handed, each list's times its factor (one where it has none), renormalised over the lists; the shares unchanged
        /// where their product sums to nothing.</summary>
        public static Dictionary<string, double> Apply(IReadOnlyDictionary<string, double> shares, IReadOnlyDictionary<string, double> factors)
        {
            var product = new Dictionary<string, double>(StringComparer.Ordinal);
            double sum = 0.0;
            foreach (KeyValuePair<string, double> kv in shares)
            {
                double f = factors != null && factors.TryGetValue(kv.Key, out double stored) ? stored : 1.0;
                product[kv.Key] = kv.Value * f;
                sum += kv.Value * f;
            }
            if (!(sum > 0.0)) { return Copy(shares); }
            var applied = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, double> kv in product) { applied[kv.Key] = kv.Value / sum; }
            return applied;
        }

        private static Dictionary<string, double> Copy(IReadOnlyDictionary<string, double> shares)
        {
            var copy = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, double> kv in shares) { copy[kv.Key] = kv.Value; }
            return copy;
        }

        /// <summary>The log's line: each list's factor and its share handed to the count before and after it.</summary>
        public static string Line(ElectionVintage election, IReadOnlyDictionary<string, double> before, IReadOnlyDictionary<string, double> after, IReadOnlyDictionary<string, double> factors)
        {
            var sb = new StringBuilder();
            sb.Append("the eve-fitted factors of ").Append(election).Append(" (GATE-18, E1's form at the count's unit): ");
            bool first = true;
            foreach (KeyValuePair<string, double> kv in before)
            {
                double f = factors != null && factors.TryGetValue(kv.Key, out double stored) ? stored : 1.0;
                double a = after != null && after.TryGetValue(kv.Key, out double x) ? x : double.NaN;
                sb.Append(first ? string.Empty : ", ").Append(kv.Key).Append(' ')
                  .Append(f.ToString("0.0000", CultureInfo.InvariantCulture)).Append(" (")
                  .Append((100.0 * kv.Value).ToString("0.00", CultureInfo.InvariantCulture)).Append(" to ")
                  .Append((100.0 * a).ToString("0.00", CultureInfo.InvariantCulture)).Append(" %)");
                first = false;
            }
            return sb.ToString();
        }
    }
}
