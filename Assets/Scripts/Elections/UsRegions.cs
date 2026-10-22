using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-6 US-30 (`docs/specs/USA_STAGE_PLAN.md`, US-30): **THE 51 JURISDICTIONS AS A US CAMPAIGN'S REGIONS** - the runtime reader of US-3's catalog
    /// (<see cref="UsPresidentialReturns.States"/>), the way <see cref="GermanRegions"/> reads the Länder. A state (and the District of Columbia) is where a
    /// presidential vote is counted and its electors appointed, so it is the unit a US campaign's regions are made of.
    ///
    /// <para><b>The previous election's returns are the regions' weights</b>, as the seated election's are for Sweden and Germany: each jurisdiction's
    /// audience - who a local act can reach - is its valid presidential votes at the presidential election before the campaign's polling day (the catalog's
    /// total of votes cast; 2020's for the 2024 campaign, 2024's for a later one). The game's own count carries shares, not votes, so a later campaign
    /// reads the record's last returns (DECLARED).</para>
    ///
    /// <para><b>The eligible electorate</b> - who a ground game can mobilise, F3's distinction - is the Census Bureau's citizen voting-age population,
    /// the 2020-2024 ACS special tabulation (`ElectionsData/usa/raw/cvap/State.csv`, its "Total" line per state), the newest on disk for every campaign
    /// (DECLARED: the five-year window ending in the 2024 election's year). <c>UsCampaignDiagnostic</c> re-reads the file against its digest and holds
    /// every figure below to it, mapping the Bureau's state names to the catalog's postal codes through the University of Florida's file already on disk.</para>
    ///
    /// <para><b>Where each candidate is on the ballot</b> (§699, as Germany's candidacies are read): the two major parties' nominees everywhere; a column of
    /// the field of record that a campaigning party backs and that is no nominee stands only where <see cref="PresidencyOfRecord.StandingOfRecord"/> lists
    /// it (US-6c, J3). The roster's parties back the two nominees alone, so the staging's every party stands in every jurisdiction; the minor candidates
    /// campaign nowhere (no roster party is theirs) and stand in the count where on the ballot of record, which US-6c builds.</para>
    /// </summary>
    public static class UsRegions
    {
        // [SOURCED] the Census Bureau's citizen voting-age population, 2020-2024 ACS 5-year special tabulation - `ElectionsData/usa/raw/cvap/State.csv`,
        // each state's "Total" line, column cvap_est; keyed by the catalog's postal code. Held figure by figure by UsCampaignDiagnostic.
        private static readonly (string State, long Cvap)[] Cvap2020To2024 =
        {
            ("AL", 3845696L), ("AK", 538813L), ("AZ", 5324115L), ("AR", 2245940L), ("CA", 26132799L), ("CO", 4355173L), ("CT", 2663362L), ("DE", 764113L),
            ("DC", 506632L), ("FL", 16182912L), ("GA", 7834503L), ("HI", 1054556L), ("ID", 1411149L), ("IL", 9094987L), ("IN", 5030938L), ("IA", 2381746L),
            ("KS", 2134491L), ("KY", 3401787L), ("LA", 3413948L), ("ME", 1114201L), ("MD", 4418305L), ("MA", 5155255L), ("MI", 7653711L), ("MN", 4247053L),
            ("MS", 2221761L), ("MO", 4685988L), ("MT", 871202L), ("NE", 1413882L), ("NV", 2223535L), ("NH", 1109601L), ("NJ", 6433712L), ("NM", 1553239L),
            ("NY", 14104696L), ("NC", 7898161L), ("ND", 581737L), ("OH", 8956209L), ("OK", 2923605L), ("OR", 3211655L), ("PA", 9961815L), ("RI", 826623L),
            ("SC", 4009585L), ("SD", 669733L), ("TN", 5275348L), ("TX", 19865984L), ("UT", 2291434L), ("VT", 521797L), ("VA", 6378483L), ("WA", 5583139L),
            ("WV", 1409278L), ("WI", 4505014L), ("WY", 440292L),
        };

        /// <summary>The electorate table as the diagnostic holds it: each postal code and its citizen voting-age population.</summary>
        public static IReadOnlyList<(string State, long Cvap)> CitizenVotingAgePopulation => Cvap2020To2024;

        /// <summary>The presidential election whose returns weigh a campaign for <paramref name="pollingDay"/>: the latest the catalog holds before the
        /// polling day's year; 0 where it holds none.</summary>
        public static int PriorYear(DateTime pollingDay)
        {
            int best = 0;
            foreach (var y in UsPresidentialReturns.Years) { if (y.Year < pollingDay.Year && y.Year > best) { best = y.Year; } }
            return best;
        }

        /// <summary>The citizen voting-age population of <paramref name="state"/> (a postal code); -1 where the table has none.</summary>
        public static long EligibleOf(string state)
        {
            foreach ((string s, long cvap) in Cvap2020To2024) { if (s == state) { return cvap; } }
            return -1L;
        }

        /// <summary>The jurisdictions as a campaign's regions for <paramref name="pollingDay"/>, in the catalog's order: each one's name (its postal code),
        /// audience (its valid presidential votes at <paramref name="priorYear"/>) and eligible electorate (its citizen voting-age population). Empty, with
        /// <paramref name="why"/>, where the catalog holds no earlier election or the electorate table misses a jurisdiction.</summary>
        public static RegionAudience[] Regions(DateTime pollingDay, out double national, out int priorYear, out string why)
        {
            national = 0.0;
            why = null;
            priorYear = PriorYear(pollingDay);
            var regions = new List<RegionAudience>();
            if (priorYear == 0) { why = "the returns catalog holds no presidential election before " + pollingDay.Year; return regions.ToArray(); }
            foreach (var s in UsPresidentialReturns.States)
            {
                if (s.Year != priorYear) { continue; }
                long eligible = EligibleOf(s.State);
                if (eligible <= 0L) { why = "the electorate table holds no citizen voting-age population for " + s.State; return new RegionAudience[0]; }
                regions.Add(new RegionAudience(s.State, s.VotesTotal, eligible: eligible));
                national += s.VotesTotal;
            }
            return regions.ToArray();
        }

        /// <summary>Where each of <paramref name="keys"/> stands, one flag per region of <paramref name="regions"/> (§699): a party backing a nominee of
        /// <paramref name="pollingDay"/>'s field of record, or backing no column of it, everywhere; a party backing a column that is no nominee, only where
        /// <see cref="PresidencyOfRecord.StandingOfRecord"/> lists that column.</summary>
        public static bool[][] Stands(IReadOnlyList<string> keys, RegionAudience[] regions, DateTime pollingDay)
        {
            IReadOnlyList<PresidencyOfRecord.CandidateOfRecord> field = PresidencyOfRecord.CandidatesOf(CountryId.USA, pollingDay.Year + 1);
            var nominees = new HashSet<string>();
            foreach (var c in UsPresidentialReturns.Candidates) { if (c.Year == pollingDay.Year && (c.Ticket == "D" || c.Ticket == "R")) { nominees.Add(c.Candidate); } }
            var stands = new bool[keys.Count][];
            for (int p = 0; p < keys.Count; p++)
            {
                stands[p] = new bool[regions.Length];
                Dictionary<string, double> listed = null;
                foreach (PresidencyOfRecord.CandidateOfRecord c in field)
                {
                    if (c.BackingParty == keys[p] && !nominees.Contains(c.Surname)) { listed = PresidencyOfRecord.StandingOfRecord(CountryId.USA, pollingDay.Year, c.Surname); }
                }
                for (int r = 0; r < regions.Length; r++) { stands[p][r] = listed == null || listed.ContainsKey(regions[r].Name); }
            }
            return stands;
        }
    }
}
