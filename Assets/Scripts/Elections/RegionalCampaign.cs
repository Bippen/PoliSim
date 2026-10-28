using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// Regional 5a, ruling K146 (Elias, 2026-10-27, `docs/RULINGS_2026-10-23.md`): <b>WHAT A REGION IS, PER COUNTRY.</b> "A region is Sweden's
    /// valkrets, Germany's Land, a US state and Poland's okreg (each okreg's electorate fetched from the PKW); Italy and France national until PS-7 /
    /// SP-8." Each region carries its name, its audience (the valid votes of the election the country's chamber is seated on - who a campaign can
    /// reach there) and its electorate (who can be mobilised), all SOURCED and already on disk except Poland's electorates, which this item fetched
    /// (`PolishOkregElectorates2023`, the PKW's own per-district files). The three staged campaigns read their regions from their own
    /// stagings (`LiveCampaignSetup`); this layer is the one place the four countries' units are named side by side, and where Italy's and France's
    /// absence is stated rather than implied.
    /// </summary>
    public static class RegionLayer
    {
        /// <summary>K146: whether <paramref name="country"/> is campaigned by region - Sweden, Germany, the USA and Poland; Italy and France stay national.</summary>
        public static bool IsRegional(CountryId country) =>
            country == CountryId.Sweden || country == CountryId.Germany || country == CountryId.USA || country == CountryId.Poland;

        /// <summary>K146: the unit a region is in <paramref name="country"/>, in the player's words; null for a national country.</summary>
        public static string UnitOf(CountryId country) =>
            country == CountryId.Sweden ? "valkrets" : country == CountryId.Germany ? "Land" : country == CountryId.USA ? "state" : country == CountryId.Poland ? "okreg" : null;

        /// <summary>Why <paramref name="country"/> has no regions (K146's words), or null for a regional country.</summary>
        public static string NationalBecause(CountryId country) =>
            IsRegional(country) ? null
            : country == CountryId.Italy ? "Italy is campaigned nationally until PS-7 (K146)"
            : country == CountryId.France ? "France is campaigned nationally until SP-8 (K146)"
            : country + " has no campaign regions (K146 names Sweden, Germany, the USA and Poland)";

        /// <summary>
        /// K146: <paramref name="country"/>'s regions on <paramref name="day"/> - Sweden's valkretsar (the seated election's valid votes and roll,
        /// `LiveCampaignSetup.SwedenRegions`), Germany's Länder (the seated election's valid Zweitstimmen and Wahlberechtigte), the US jurisdictions
        /// (the previous presidential election's votes and the citizen voting-age population, `UsRegions.Regions`) and Poland's okregi (the 2023
        /// Sejm's valid list votes and electorates, the PKW's). Empty for a national country, with <paramref name="why"/>.
        /// </summary>
        public static RegionAudience[] Of(CountryId country, DateTime day, out string why)
        {
            why = NationalBecause(country);
            if (why != null) { return new RegionAudience[0]; }
            switch (country)
            {
                case CountryId.Sweden:
                    return LiveCampaignSetup.SwedenRegions(out double _);
                case CountryId.Germany:
                {
                    var lands = new RegionAudience[GermanRegions.Count];
                    for (int r = 0; r < lands.Length; r++) { lands[r] = new RegionAudience(GermanRegions.NameAt(r), GermanRegions.ValidAt(r, day), eligible: GermanRegions.EligibleAt(r, day)); }
                    return lands;
                }
                case CountryId.USA:
                    return UsRegions.Regions(day, out double _, out int _, out why);
                default:
                {
                    var okregi = new RegionAudience[Generated.PolishOkregElectorates2023.Names.Length];
                    for (int r = 0; r < okregi.Length; r++)
                    {
                        okregi[r] = new RegionAudience(Generated.PolishOkregElectorates2023.Names[r], Generated.PolishOkregElectorates2023.Valid[r], eligible: Generated.PolishOkregElectorates2023.Eligible[r]);
                    }
                    return okregi;
                }
            }
        }
    }

    /// <summary>
    /// Regional 5a, rulings K148 and K149 (Elias, 2026-10-27) with K83: <b>THE BOUGHT REGIONAL POLL, THE SWING INDEX AS POLLED, AND INTELLIGENCE.</b>
    /// <list type="bullet">
    /// <item><description><b>A regional poll</b> is priced information (K2's precedent, Q7's POLL row): the campaign poll verb queued with a region buys a
    /// poll of that region alone - its sample <see cref="Sample"/> and price <see cref="Cost"/> [AUTHORED-DRAFT], on the calibration list (K149) - fielded
    /// by the same multinomial draw as every poll (`PollingSystem.Conduct`) on the region's truth (<see cref="TruthIn"/>), its margin of error the binomial
    /// formula at its own sample (`PollingSystem.MarginOfErrorPp`: 1.96 × √(s(1 − s)/n)). It moves no vote.</description></item>
    /// <item><description><b>The breakdown is by region only</b> (K148): groups wait for a vote model with groups, so a poll carries the region's shares
    /// and nothing beneath them.</description></item>
    /// <item><description><b>The swing index per region</b> is §25's (`SwingRegions.FromPoll`) on the newest regional poll the party bought there: a region
    /// not polled is UNKNOWN (§36: absence, never a guess), a polled one carries its index, and one whose lead is inside its own combined error is TOO
    /// CLOSE TO CALL (<see cref="StateOf"/>).</description></item>
    /// <item><description><b>Intelligence</b> is information from the party's own books (<see cref="Intelligence"/>): a field report per office - its
    /// volunteers, the doors it has opened and the calls completed in its region - and every regional poll's reading. Focus groups are not built: a
    /// focus group reads a group, and K148 defers groups to a vote model with groups.</description></item>
    /// </list>
    /// </summary>
    public static class RegionalPolling
    {
        /// <summary>[AUTHORED-DRAFT] (K149, on the calibration list): a regional poll's sample - the public tracker's 600, the staging's own [AUTHORED-DRAFT]
        /// national size, so a region polled is read as closely as the country is by the tracker.</summary>
        public const int Sample = 600;

        /// <summary>[AUTHORED-DRAFT] (K149, on the calibration list): a regional poll's price in the staging's purse units - half the standard commission's
        /// 120 000 for half its 1 200 interviews (the commission's price per interview, itself [AUTHORED-DRAFT]); a US purse is re-denominated as every act's is (K110).</summary>
        public const double Cost = 60_000.0;

        /// <summary>The house a regional poll of <paramref name="regionName"/> is fielded by: <see cref="Sample"/> interviews at <see cref="Cost"/>, no house lean (the party's own commission).</summary>
        public static PollingHouse House(string regionName) => new PollingHouse("Regional poll · " + regionName, Sample, Cost, null, isInternal: true);

        /// <summary>The margin of error a poll of <paramref name="sample"/> interviews carries at <paramref name="share"/>, in points - the binomial formula at 95 % (`PollingSystem.MarginOfErrorPp`).</summary>
        public static double MarginOfErrorPp(double share, int sample) => PollingSystem.MarginOfErrorPp(share, sample);

        /// <summary>DERIVED, for the HQ's offer: the interviews a national poll of <paramref name="nationalSample"/> holds in a region of <paramref name="weight"/> of the
        /// national audience (at least one) - what the region would be read on without a poll of its own.</summary>
        public static int SubSample(int nationalSample, double weight) => Math.Max(1, (int)Math.Round(nationalSample * Math.Max(0.0, weight)));

        /// <summary>
        /// The truth a regional poll of region <paramref name="r"/> is fielded on: the national <paramref name="national"/> (the run's true preference, the
        /// momentum on it as on every poll) plus the region's offset at the staging's prior (<see cref="CampaignRun.Setup.RegionalOffset"/>, the count's own
        /// derivation) plus the campaign's movement there less its national movement (<paramref name="regionShift"/> and <paramref name="nationalShift"/>,
        /// `CampaignPressure.RegionalShifts`; null for none), each share held at zero and the vector renormalised. DECLARED: the offset is held at the
        /// prior's, so the swing since the prior is read uniformly, as the count's swing reads it, without the count's re-solve. Null where the staging
        /// carries no offset for the region.
        /// </summary>
        public static double[] TruthIn(CampaignRun.Setup setup, double[] national, double[] regionShift, double[] nationalShift, int r)
        {
            if (setup.RegionalOffset == null || r < 0 || r >= setup.RegionalOffset.Length || setup.RegionalOffset[r] == null || national == null) { return null; }
            var truth = new double[national.Length];
            double total = 0.0;
            for (int p = 0; p < truth.Length; p++)
            {
                double v = national[p] + (p < setup.RegionalOffset[r].Length ? setup.RegionalOffset[r][p] : 0.0);
                if (regionShift != null && nationalShift != null && p < regionShift.Length && p < nationalShift.Length) { v += regionShift[p] - nationalShift[p]; }
                if (!setup.StandsIn(p, r) || v < 0.0) { v = 0.0; }
                truth[p] = v;
                total += v;
            }
            if (!(total > 0.0)) { return null; }
            for (int p = 0; p < truth.Length; p++) { truth[p] /= total; }
            return truth;
        }

        /// <summary>
        /// Each region's offset from <paramref name="prior"/> (in <paramref name="keys"/>' order) at the staging's prior, in <paramref name="regions"/>'
        /// order - the count's own derivation of the prior's regions less the prior: Sweden's valkretsar and Germany's Länder by the uniform swing the night
        /// and the count read (`NationalElection.RegionalOf`), the US states by the presidential count's own derivation (`UsPresidentialElection.Count`, its
        /// three columns - the Democratic nominee, the Republican, every other - each other party taking the others' column in proportion to its prior share
        /// among those standing in the state). Null, with <paramref name="why"/>, where the derivation cannot be read or its regions do not match by name.
        /// </summary>
        public static double[][] OffsetsFor(CountryId country, string[] keys, double[] prior, DateTime day, RegionAudience[] regions, bool[][] stands, Country played, out string why)
        {
            why = null;
            if (keys == null || prior == null || regions == null || keys.Length != prior.Length) { why = "no staging to derive regions on"; return null; }
            double[][] regional;
            string[] names;
            if (country == CountryId.USA)
            {
                regional = UsRegionalShares(keys, prior, day, regions, stands, played, out names, out why);
            }
            else
            {
                regional = NationalElection.RegionalOf(country, keys, prior, day, out names, out double[] _);
                if (regional == null) { why = PoliSim.Wording.EnglishText.Possessive(country.ToString()) + " regions are not derived from a national vote"; }
            }
            if (regional == null) { return null; }
            var at = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < names.Length; i++) { at[names[i]] = i; }
            var offset = new double[regions.Length][];
            for (int r = 0; r < regions.Length; r++)
            {
                if (!at.TryGetValue(regions[r].Name, out int i)) { why = regions[r].Name + " is staged and has no derived region"; return null; }
                offset[r] = new double[keys.Length];
                for (int p = 0; p < keys.Length; p++) { offset[r][p] = regional[i][p] - prior[p]; }
            }
            return offset;
        }

        private static double[][] UsRegionalShares(string[] keys, double[] prior, DateTime day, RegionAudience[] regions, bool[][] stands, Country played, out string[] names, out string why)
        {
            names = null;
            UsPresidentialElection.Contest previous = UsPresidentialElection.PreviousCount(played, day.Date);
            ElectoralCollege.StateCall[] previousStates = previous != null ? UsPresidentialElection.StatesOf(played, previous) : null;
            IReadOnlyList<UsTermLimit.Nominee> nominees = played != null ? UsTermLimit.NomineesFor(played, day.Date) : null;
            var shares = new Dictionary<string, double>(StringComparer.Ordinal);
            for (int p = 0; p < keys.Length; p++) { shares[keys[p]] = prior[p]; }
            UsPresidentialElection.Contest read = UsPresidentialElection.Count(CountryId.USA, day.Date, shares, nominees, 0, previous, previousStates, out ElectoralCollege.StateCall[] states, out why);
            if (read == null || states == null) { why = why ?? "the presidential count derived no states"; return null; }
            int rep = Array.IndexOf(keys, "REP"), dem = Array.IndexOf(keys, "DEM");
            if (rep < 0 || dem < 0) { why = "the staging carries no REP and DEM"; return null; }
            names = new string[states.Length];
            var regional = new double[states.Length][];
            var regionAt = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int r = 0; r < regions.Length; r++) { regionAt[regions[r].Name] = r; }
            for (int i = 0; i < states.Length; i++)
            {
                names[i] = states[i].State;
                regional[i] = new double[keys.Length];
                regional[i][dem] = states[i].Shares[ElectoralCollege.ShareDemocrat];
                regional[i][rep] = states[i].Shares[ElectoralCollege.ShareRepublican];
                regionAt.TryGetValue(states[i].State, out int r);
                double others = 0.0;
                for (int p = 0; p < keys.Length; p++) { if (p != rep && p != dem && (stands == null || stands[p] == null || stands[p][r])) { others += prior[p]; } }
                for (int p = 0; p < keys.Length; p++)
                {
                    if (p == rep || p == dem || !(others > 0.0) || (stands != null && stands[p] != null && !stands[p][r])) { continue; }
                    regional[i][p] = states[i].Shares[ElectoralCollege.ShareOthers] * prior[p] / others;
                }
            }
            return regional;
        }

        /// <summary>A region's reading on the swing map: UNKNOWN where nothing was polled, else polled, or too close to call where the lead is inside its own error (§25, §36).</summary>
        public enum MapState
        {
            Unknown = 0,
            Polled = 1,
            TooCloseToCall = 2,
        }

        /// <summary>The map state of a reading (`SwingRegions.FromPoll` / `Unknown`).</summary>
        public static MapState StateOf(MapRegionReading reading) => !reading.Measured ? MapState.Unknown : reading.TooCloseToCall ? MapState.TooCloseToCall : MapState.Polled;

        /// <summary>The newest regional poll <paramref name="party"/> bought in each region of the run, or null per region where none was bought.</summary>
        public static Poll?[] NewestPerRegion(CampaignRun.State s, int party)
        {
            if (s == null || party < 0 || party >= s.PartyCount) { return new Poll?[0]; }
            var newest = new Poll?[s.Setup.Regions.Length];
            if (s.RegionalPolls == null || s.RegionalPolls[party] == null) { return newest; }
            foreach ((int Day, int Region, Poll Poll) bought in s.RegionalPolls[party])
            {
                if (bought.Region >= 0 && bought.Region < newest.Length) { newest[bought.Region] = bought.Poll; }   // in the order bought: the last is the newest
            }
            return newest;
        }

        /// <summary>
        /// The swing map's readings for <paramref name="party"/>: every region of the run UNKNOWN unless the party bought a poll of it, else §25's reading of
        /// its newest poll (the index, the leader and the runner-up, too close to call where the lead is inside its own combined error). Weights are each
        /// region's audience over the national.
        /// </summary>
        public static MapRegionReading[] Readings(CampaignRun.State s, int party)
        {
            if (s == null) { return new MapRegionReading[0]; }
            RegionAudience[] regions = s.Setup.Regions;
            double national = 0.0;
            foreach (RegionAudience r in regions) { national += r.Audience; }
            Poll?[] newest = NewestPerRegion(s, party);
            var readings = new MapRegionReading[regions.Length];
            for (int r = 0; r < regions.Length; r++)
            {
                double weight = national > 0.0 ? regions[r].Audience / national : 0.0;
                readings[r] = r < newest.Length && newest[r].HasValue ? SwingRegions.FromPoll(regions[r].Name, weight, newest[r].Value) : SwingRegions.Unknown(regions[r].Name, weight);
            }
            return readings;
        }

        /// <summary>
        /// Intelligence as information (K83, the D-WORKLIST ask's "field reports ... as information"): one line per office of <paramref name="party"/> - its
        /// region, its volunteers, the doors opened and the calls completed there by the party so far (its own books, `RegionalMobilization`) - and one per
        /// region the party has polled, its newest reading (leader, lead and its error, the index, the state). Nothing true is read but the party's own acts.
        /// </summary>
        public static List<string> Intelligence(CampaignRun.State s, int party)
        {
            var lines = new List<string>();
            if (s == null || party < 0 || party >= s.PartyCount) { return lines; }
            RegionAudience[] regions = s.Setup.Regions;
            foreach (CampaignOffice office in s.Offices[party].Offices)
            {
                if (office.Region < 0 || office.Region >= regions.Length) { continue; }
                lines.Add(string.Format(CultureInfo.InvariantCulture, "FIELD REPORT · {0} · {1:N0} VOLUNTEERS · {2:N0} DOORS OPENED · {3:N0} CALLS COMPLETED",
                    regions[office.Region].Name.ToUpperInvariant(), office.Volunteers, s.Gotv.DoorContacts(office.Region, party), s.Gotv.PhoneContacts(office.Region, party)));
            }
            MapRegionReading[] readings = Readings(s, party);
            for (int r = 0; r < readings.Length; r++)
            {
                MapRegionReading reading = readings[r];
                if (!reading.Measured || reading.Leader < 0) { continue; }
                lines.Add(string.Format(CultureInfo.InvariantCulture, "REGIONAL POLL · {0} · {1} LEADS BY {2:0.0} ± {3:0.0} PP · INDEX {4:0} · {5}",
                    reading.Name.ToUpperInvariant(), s.Setup.Parties[reading.Leader].Name, reading.GapPp, reading.GapErrorPp, reading.SwingIndex,
                    StateOf(reading) == MapState.TooCloseToCall ? "TOO CLOSE TO CALL" : "POLLED"));
            }
            return lines;
        }
    }
}
