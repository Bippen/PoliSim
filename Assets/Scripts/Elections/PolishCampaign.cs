using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>PL-C (K168): the contest a campaign is staged for - the chamber (every staging before PL-C, Poland's Sejm among them), a presidential first
    /// vote, or a presidential run-off (Poland's: the two finalists through the 14 days between the rounds).</summary>
    public enum CampaignContest
    {
        Chamber = 0,
        PresidentialFirstVote = 1,
        PresidentialRunOff = 2,
    }

    /// <summary>
    /// PL-C (Elias's rulings K167-K169 of 2026-10-27, `docs/RULINGS_2026-10-23.md`): <b>WHAT A POLISH CAMPAIGN IS STAGED ON</b> - which contest a polling day
    /// is, the regions each contest is campaigned on, and the Kodeks wyborczy's spending limits that cap the purse.
    /// <list type="bullet">
    /// <item><description><b>The contests</b> (K168): a day is a presidential run-off where the world holds a first vote whose run-off falls on it, a first
    /// vote where it is the next first vote not yet held (`PresidentialElection.TryNextFirstVote`, Art. 128's days), and the Sejm's otherwise.</description></item>
    /// <item><description><b>The regions</b> (K169): the Sejm's 41 okregi (`RegionLayer.Of`, the PKW's 2023 electorates and valid list votes); the
    /// presidency's 16 wojewodztwa (<see cref="Wojewodztwa"/>, the latest presidential first round of record before the vote - its valid votes the audience,
    /// its electorate who a ground game can reach). The voters abroad and on ships, who vote in a presidential election, are no region in either vintage: 2025's
    /// KBW files carry them as units of their own, and 2020's count them inside mazowieckie, so the generated table takes the KBW powiat file's two rows
    /// out of it (`PolishWojewodztwa.AbroadValid2020`, SOURCED) - the 16 regions hold the voters in Poland alone, and a national act reaches no voter abroad
    /// (DECLARED: no campaign act is modelled abroad; their votes are in the count's national shares, which are the vote model's, not the regions').</description></item>
    /// <item><description><b>The limits</b> (K167, `ElectionsData/poland/raw/spending_limits/`, the statute's words quoted in its fetch log): the Sejm - art.
    /// 199 § 1, L = w × k × m / 460, m the seats of the okregi where the committee registered lists; the President - art. 327 § 1, L = w × k. k is the
    /// amount per voter the Minister of Finance's regulation in force sets (art. 200 § 1, art. 327 § 3), w the register the PKW announced for the election
    /// (art. 199 § 2, art. 327 § 2). One limit covers a committee's whole election, both presidential rounds included.</description></item>
    /// </list>
    /// </summary>
    public static class PolishCampaign
    {
        /// <summary>SOURCED - Kodeks wyborczy art. 199 § 1 pkt 2 (`../electoral_code/eli_DU_2011_112_kodeks_wyborczy.html`): the formula's divisor, the
        /// Sejm's 460 seats.</summary>
        public const int SejmSeats = 460;

        /// <summary>SOURCED - the regulations raising art. 199 § 1 pkt 1's amount per voter for the Sejm (`ElectionsData/poland/raw/spending_limits/`):
        /// the day each entered into force (its ELI record's `entryIntoForce`), the amount in złoty, the act.</summary>
        private static readonly (DateTime InForce, double PerVoter, string Act)[] SejmRates =
        {
            (new DateTime(2023, 2, 2), 1.10, "Dz.U. 2023 poz. 132"),
            (new DateTime(2024, 5, 7), 1.17, "Dz.U. 2024 poz. 605"),
            (new DateTime(2025, 11, 15), 1.24, "Dz.U. 2025 poz. 1496"),
        };

        /// <summary>SOURCED - the regulations raising art. 327 § 1's amount per voter for the President (`ElectionsData/poland/raw/spending_limits/`), as above.</summary>
        private static readonly (DateTime InForce, double PerVoter, string Act)[] PresidentialRates =
        {
            (new DateTime(2024, 5, 3), 0.85, "Dz.U. 2024 poz. 590"),
            (new DateTime(2025, 11, 15), 0.90, "Dz.U. 2025 poz. 1501"),
        };

        /// <summary>SOURCED - the register the PKW announced for an election (art. 199 § 2, art. 327 § 2): the day of the notice, the voters on the register,
        /// the notice (`ElectionsData/poland/raw/spending_limits/eli_MP_*.pdf`).</summary>
        private static readonly (DateTime Announced, long Voters, string Notice)[] Registers =
        {
            (new DateTime(2023, 8, 14), 29_097_503L, "M.P. 2023 poz. 867 (the register at 30 Jun 2023)"),
            (new DateTime(2025, 1, 16), 28_945_200L, "M.P. 2025 poz. 66 (the register at 31 Dec 2024)"),
        };

        /// <summary>
        /// The contest <paramref name="electionDay"/> holds in <paramref name="played"/>'s world (none handed: a world that has held no presidential round):
        /// a run-off where a held first vote's run-off falls on it (<paramref name="runOff"/> that contest), a first vote where it is a held first vote's day
        /// (its campaign replayed) or the next one not yet held,
        /// the Sejm's otherwise.
        /// </summary>
        public static CampaignContest ContestOn(DateTime electionDay, Country played, out PresidentialElection.Contest runOff)
        {
            runOff = null;
            List<PresidentialElection.Contest> held = played?.Id == CountryId.Poland ? played.PresidentialElections : null;
            if (held != null)
            {
                foreach (PresidentialElection.Contest c in held) { if (c.RunOffA != null && c.RunOffOn.Date == electionDay.Date) { runOff = c; return CampaignContest.PresidentialRunOff; } }   // pending, or held today (a replay on the day)
                if (held.Exists(c => c.FirstVote.Date == electionDay.Date)) { return CampaignContest.PresidentialFirstVote; }   // a first vote held today: a replay of its campaign
            }
            if (PresidentialElection.TryNextFirstVote(CountryId.Poland, held, electionDay.Date, out DateTime first, out DateTime _, out string _) && first == electionDay.Date)
            {
                return CampaignContest.PresidentialFirstVote;
            }
            return CampaignContest.Chamber;
        }

        /// <summary>The register an election on <paramref name="electionDay"/> reads: the PKW's notice for it - the newest announced on or before the day.
        /// DECLARED: an election after the newest notice on disk reads that notice's register (the register of an election not yet ordered is not published).
        /// 0 before the first notice on disk.</summary>
        public static long RegisterFor(DateTime electionDay, out string basis)
        {
            basis = null;
            long voters = 0;
            foreach ((DateTime announced, long count, string notice) in Registers)
            {
                if (announced <= electionDay.Date) { voters = count; basis = notice; }
            }
            if (voters > 0 && electionDay.Date > Registers[Registers.Length - 1].Announced.AddYears(1)) { basis += ", the newest on disk standing in for this election's"; }
            return voters;
        }

        /// <summary>The amount per voter in force on <paramref name="opens"/> (the campaign's opening, DECLARED) from <paramref name="rates"/>; NaN before the
        /// first regulation on disk.</summary>
        private static double RateOn((DateTime InForce, double PerVoter, string Act)[] rates, DateTime opens, out string act)
        {
            act = null;
            double k = double.NaN;
            foreach ((DateTime inForce, double perVoter, string a) in rates) { if (inForce <= opens.Date) { k = perVoter; act = a; } }
            return k;
        }

        /// <summary>
        /// K167: a Sejm committee's limit (złoty) - art. 199 § 1, w × k × <paramref name="mandates"/> / 460, k the regulation in force on the campaign's opening
        /// <paramref name="opens"/>, w the register for <paramref name="electionDay"/>. NaN, with <paramref name="basis"/> saying why, where either is not on disk.
        /// </summary>
        public static double SejmLimit(DateTime opens, DateTime electionDay, int mandates, out string basis)
        {
            double k = RateOn(SejmRates, opens, out string act);
            long w = RegisterFor(electionDay, out string register);
            if (double.IsNaN(k) || w <= 0) { basis = "no regulation in force or no register on disk for this election"; return double.NaN; }
            basis = string.Format(CultureInfo.InvariantCulture, "Kodeks wyborczy art. 199 § 1: {0:N0} voters ({1}) × {2:0.00} zł ({3}) × {4} of 460 seats", w, register, k, act, mandates);
            return w * k * mandates / SejmSeats;
        }

        /// <summary>K167: a presidential candidate's committee's limit (złoty) - art. 327 § 1, w × k, read as <see cref="SejmLimit"/>.</summary>
        public static double PresidentialLimit(DateTime opens, DateTime electionDay, out string basis)
        {
            double k = RateOn(PresidentialRates, opens, out string act);
            long w = RegisterFor(electionDay, out string register);
            if (double.IsNaN(k) || w <= 0) { basis = "no regulation in force or no register on disk for this election"; return double.NaN; }
            basis = string.Format(CultureInfo.InvariantCulture, "Kodeks wyborczy art. 327 § 1: {0:N0} voters ({1}) × {2:0.00} zł ({3})", w, register, k, act);
            return w * k;
        }

        /// <summary>
        /// K169: the 16 wojewodztwa as the presidential campaign's regions for a first vote on <paramref name="firstVote"/> - the latest presidential first round
        /// of record held before it (2020's for 2025, 2025's after): each one's valid votes the audience, its electorate the eligible. <paramref name="national"/>
        /// the regions' audience summed; <paramref name="year"/> the round read.
        /// </summary>
        public static RegionAudience[] Wojewodztwa(DateTime firstVote, out double national, out int year)
        {
            bool latest = firstVote.Date > new DateTime(2025, 5, 18);   // SOURCED: the first round of 18 May 2025 (`PolishPresidentialReturns`)
            year = latest ? 2025 : 2020;
            long[] valid = latest ? PolishWojewodztwa.Valid2025 : PolishWojewodztwa.Valid2020;
            long[] eligible = latest ? PolishWojewodztwa.Eligible2025 : PolishWojewodztwa.Eligible2020;
            var regions = new RegionAudience[PolishWojewodztwa.Names.Length];
            national = 0.0;
            for (int r = 0; r < regions.Length; r++)
            {
                regions[r] = new RegionAudience(PolishWojewodztwa.Names[r], valid[r], eligible: eligible[r]);
                national += valid[r];
            }
            return regions;
        }

        /// <summary>
        /// K169: where each list stands among the okregi - DECLARED, the seated election's candidacies as the count reads them: a 2023 committee that stood in
        /// one okreg alone stands there (`PolishSejmAllocation.SoleOkreg`, the MN's okreg 21), a list with a share in the prior everywhere, a list with none
        /// (the 2019 lists the roster carries, SLD and PSL, on the 2023 chamber) nowhere.
        /// </summary>
        public static bool[][] SejmStands(string[] keys, double[] prior, int okregi)
        {
            var stands = new bool[keys.Length][];
            for (int p = 0; p < keys.Length; p++)
            {
                stands[p] = new bool[okregi];
                int sole = PolishSejmAllocation.SoleOkreg(keys[p]);
                for (int d = 0; d < okregi; d++) { stands[p][d] = sole >= 0 ? d == sole : prior[p] > 0.0; }
            }
            return stands;
        }

        /// <summary>K167: the seats of the okregi where a list stands (<paramref name="stands"/>, okreg 1 to 41) - art. 199 § 1 pkt 2's m.</summary>
        public static int MandatesWhere(bool[] stands)
        {
            int m = 0;
            for (int d = 0; d < stands.Length && d < PolishSejmDistricts2023.Magnitudes.Length; d++) { if (stands[d]) { m += PolishSejmDistricts2023.Magnitudes[d]; } }
            return m;
        }

        /// <summary>[SOURCED] the last day of Eurobarometer 105's fieldwork (12 March to 5 April 2026, `ElectionsData/salience/issue_salience.md`): a Polish campaign
        /// opening before it reads the wave all the same, and its note says it postdates the campaign (no earlier Polish wave is on disk in this form).</summary>
        private static readonly DateTime Eb105FieldworkEnds = new DateTime(2026, 4, 5);

        /// <summary>The salience a Polish campaign opening on <paramref name="opens"/> reads - the country's seed (`StanceModel`: EB105, Spring 2026), each
        /// measured slot rounded to the wave's whole per cents.</summary>
        public static double[] Salience(DateTime opens, out string label)
        {
            var salience = new double[IssueVector.IssueCount];
            var parts = new List<string>();
            for (int i = 0; i < salience.Length; i++) { salience[i] = double.NaN; }
            foreach (IssueId issue in Enum.GetValues(typeof(IssueId)))
            {
                float seed = PoliSim.Simulation.StanceModel.SalienceOf(CountryId.Poland, issue, out bool measured);
                if (!measured) { continue; }
                salience[(int)issue] = Math.Round(seed, 2);
                parts.Add(issue.ToString().ToLowerInvariant() + " " + salience[(int)issue].ToString("0.00", CultureInfo.InvariantCulture));
            }
            label = "EB105 PL (the seed): " + string.Join(", ", parts) + (opens.Date > Eb105FieldworkEnds ? string.Empty : " - fielded after this campaign opened, no earlier Polish wave on disk");
            return salience;
        }

        /// <summary>
        /// K147 for the Sejm: the campaign's movement in each okreg by list key - each key's staged party's row of <paramref name="regionalShift"/> in the staged
        /// okregi (`CampaignRun.Result.RegionalShift`, regions found by name), 41 values in okreg order - for the count to lay on the okregi
        /// (`PolishSejmAllocation.SwungVotes`); null where no Sejm campaign was staged on the okregi or nothing moved. <paramref name="moved"/>: the okregi with
        /// a non-zero movement.
        /// </summary>
        public static Dictionary<string, double[]> OkregShiftOf(CampaignRun.Setup? setup, double[][] regionalShift, IReadOnlyList<string> keys, out int moved)
        {
            moved = 0;
            if (regionalShift == null || !setup.HasValue || setup.Value.Contest != CampaignContest.Chamber || setup.Value.Regions == null || setup.Value.Parties == null) { return null; }
            CampaignRun.Setup s = setup.Value;
            string[] names = PolishOkregElectorates2023.Names;
            var at = new int[names.Length];
            for (int d = 0; d < names.Length; d++) { at[d] = -1; for (int r = 0; r < s.Regions.Length && r < regionalShift.Length; r++) { if (s.Regions[r].Name == names[d]) { at[d] = r; break; } } }
            var shift = new Dictionary<string, double[]>(StringComparer.Ordinal);
            var any = new bool[names.Length];
            foreach (string key in keys)
            {
                int p = -1;
                for (int i = 0; i < s.Parties.Length; i++) { if (s.Parties[i].Name == key) { p = i; break; } }
                var row = new double[names.Length];
                for (int d = 0; d < names.Length; d++)
                {
                    int r = at[d];
                    double v = p >= 0 && r >= 0 && regionalShift[r] != null && p < regionalShift[r].Length ? regionalShift[r][p] : 0.0;
                    row[d] = v;
                    if (v != 0.0) { any[d] = true; }
                }
                shift[key] = row;
            }
            foreach (bool a in any) { if (a) { moved++; } }
            return moved == 0 ? null : shift;
        }
    }
}
