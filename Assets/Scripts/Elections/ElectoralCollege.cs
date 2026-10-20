using System;
using System.Collections.Generic;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>
    /// R-EL8 (ruled by Elias, Day-1 2026-08-29): the United States' REAL presidential elector
    /// allocation — per-state winner-take-all PLUS Maine's and Nebraska's congressional-district
    /// method — implemented from the statutes, to the standard the other five countries' rules
    /// were held to. PURE FUNCTIONS. Two inputs: <see cref="FromCatalog"/> hands it any election
    /// the generated catalog holds (<see cref="UsPresidentialReturns.Years"/>; since PS-6 US-3,
    /// `COMPLETED.md` §786) - the backtests' and US-4's; and, since PS-6 US-8, <see cref="FromNationalShares"/>
    /// hands it a predicted election - the states derived from a national vote by R-US14 (a), which
    /// is how the game's own count reaches it (<see cref="UsPresidentialElection"/>, on the day loop's
    /// polling day). US-4's instrument (`UsStateSwingCheck`) reads its method (a) from the same
    /// derivation, so the game counts the rule the ruling was measured on.
    ///
    /// The rules, each with its citation (full text and URLs in
    /// `ElectionsData/usa/district_method_2024.md`):
    /// - **The federal frame.** Each state appoints electors "in such Manner as the Legislature
    ///   thereof may direct" (U.S. Const. art. II §1 cl. 2), a number equal to its Senators plus
    ///   Representatives; 538 total, 270 to elect (NARA).
    /// - **Winner-take-all** is a STATE CHOICE, not a federal rule — 48 states and DC direct it.
    ///   Modelled as: the statewide plurality winner takes every elector of that jurisdiction.
    /// - **Maine**, Me. Rev. Stat. tit. 21-A §802: "One presidential elector shall be chosen from
    ///   each congressional district and 2 at large." → 2 at-large by statewide plurality + 1 per
    ///   congressional district by that district's plurality (4 total).
    /// - **Nebraska**, Neb. Rev. Stat. §32-710 (structure: one per district plus two at large) and
    ///   §32-1038(1) (allocation: the highest statewide vote elects the two at-large electors, the
    ///   highest vote in a congressional district elects that district's elector) → 5 total.
    ///   ⚠ §32-714 is NOT this rule (it governs vacancies and faithless electors) — the correction
    ///   is recorded in the data file.
    ///
    /// What this deliberately does NOT model, so its silence claims nothing: faithless electors
    /// (Me. §805, Neb. §32-714 pledge provisions), the Twelfth-Amendment contingent election when
    /// no candidate reaches 270 (this file reports it - <see cref="Winner"/>; the game's count holds
    /// the House's choice, R-US3 (a), in <see cref="UsPresidentialElection"/>), NPVIC activation
    /// (Me. §723-A(7)), or any ranked-choice tabulation
    /// (Maine's presidential race is legally RCV-eligible but was decided in round one in 2024 —
    /// see the data file — and in 2020: `Tools/us_returns_prep.pl` requires every Maine leader it
    /// reads from a district workbook to hold more than half of that workbook's total ballots cast,
    /// the statute's outright majority on the reading that its blank ballots include those overvoted
    /// at the first rank; a model that assumed plurality forever would be wrong, and this one does
    /// not assume it, it simply takes the winner it is handed).
    /// </summary>
    public static class ElectoralCollege
    {
        /// <summary>Total electors and the majority, per NARA — asserted rather than assumed by the harness.</summary>
        public const int TotalElectors = 538;
        public const int MajorityToElect = 270;

        /// <summary>The candidate indices <see cref="FromCatalog"/> uses: the Republican nominee, the Democratic nominee.</summary>
        public const int Republican = 0;
        public const int Democrat = 1;

        /// <summary>
        /// One presidential election of record as this allocator's input, read from the generated catalog
        /// (<see cref="UsPresidentialReturns"/>, written by `Tools/us_returns_prep.pl` from the saved pages): every
        /// jurisdiction with its electors in force and its statewide winner; Maine's and Nebraska's districts with
        /// theirs, the rest of the state's electors at large. A winner is the larger of the two nominees - the tool
        /// refuses a catalog in which all other candidates together reach the larger nominee in any state or
        /// district, so that comparison is the plurality. Candidate indices <see cref="Republican"/> and
        /// <see cref="Democrat"/>. Throws for a year the catalog does not hold.
        /// </summary>
        public static Jurisdiction[] FromCatalog(int year)
        {
            var jurisdictions = new List<Jurisdiction>();
            foreach (var state in UsPresidentialReturns.States)
            {
                if (state.Year != year) { continue; }
                var districtWinners = new List<int>();
                foreach (var district in UsPresidentialReturns.Districts)
                {
                    if (district.Year == year && district.State == state.State)
                    {
                        districtWinners.Add(district.VotesR > district.VotesD ? Republican : Democrat);
                    }
                }

                int statewide = state.VotesR > state.VotesD ? Republican : Democrat;
                jurisdictions.Add(districtWinners.Count == 0
                    ? new Jurisdiction(state.State, state.Electors, statewide)
                    : new Jurisdiction(state.State, state.Electors, statewide, state.Electors - districtWinners.Count, districtWinners.ToArray()));
            }

            if (jurisdictions.Count == 0)
            {
                throw new ArgumentException($"the catalog holds no presidential election of {year}");
            }

            return jurisdictions.ToArray();
        }

        /// <summary>The order of a state's shares in <see cref="FromNationalShares"/> (and of the national shares it is handed): the Democratic nominee,
        /// the Republican nominee, every other candidate together - US-4's order, a party with one swing each.</summary>
        public const int ShareDemocrat = 0, ShareRepublican = 1, ShareOthers = 2;

        /// <summary>PS-6 US-8: one jurisdiction as a predicted election calls it - its electors in force, its shares as derived and as of the prior
        /// election, its statewide winner, and Maine's and Nebraska's districts with each one's derived two-party share and winner.</summary>
        public sealed class StateCall
        {
            public string State;
            public int Electors;
            /// <summary>The derived shares, in <see cref="ShareDemocrat"/>'s order, and the prior election's.</summary>
            public double[] Shares, PriorShares;
            /// <summary>The statewide winner, a candidate index (<see cref="Republican"/>, <see cref="Democrat"/>).</summary>
            public int Winner;
            /// <summary>Every other candidate together above both nominees - the electors still go to the larger nominee (the others are not one
            /// candidate; DECLARED), and the count says so by name.</summary>
            public bool OthersLead;
            /// <summary>Maine's and Nebraska's congressional districts, by number; empty for a winner-take-all jurisdiction.</summary>
            public int[] Districts = new int[0];
            /// <summary>Each district's derived Democratic share of the two nominees' vote, and its winner (a candidate index).</summary>
            public double[] DistrictDemocraticTwoParty = new double[0];
            public int[] DistrictWinners = new int[0];
        }

        /// <summary>
        /// PS-6 US-8 - R-US14 (a), RULED (Elias's ruling G2, `COMPLETED.md` §797): A PREDICTED ELECTION'S COLLEGE. Every jurisdiction derived from the
        /// previous presidential election of record (<paramref name="priorYear"/>) at <paramref name="national"/> - the Democratic nominee's, the
        /// Republican's and every other candidate's together, in <see cref="ShareDemocrat"/>'s order - by §689's normalised uniform swing
        /// (<see cref="RegionalVoteModel.RegionalSharesByUniformSwing"/>, re-solved until the states give the national shares back; the worst residual
        /// handed back), each jurisdiction weighing what it cast in the prior election; Maine's and Nebraska's districts swinging with their state in
        /// two-party terms - each district's prior Democratic two-party share moved by its state's, its prior row standing in for the district of the
        /// same number whatever its lines (US-4's DECLARED form, `UsStateSwingCheck`); the electors those in force in <paramref name="electorsYear"/>.
        /// This is US-4's method (a) as its instrument measured it, moved here so that the game's count and the instrument run one derivation: the
        /// instrument reads its (a) column from this. A winner is the larger of the two nominees (an exact tie goes to the Democratic nominee, as
        /// US-4's instrument read it - a recount and the statutes' lot are not modelled) - PS-6 US-8c REPLACES that reading: Elias's ruling J4 (2026-10-19,
        /// `COMPLETED.md` §870): "an exact state tie is broken by a seeded draw, DECLARED, never in one party's favour", and a district's exact tie with it
        /// (Code's reading of the ruling, reversible by Elias's word) - a fair draw (<see cref="TieBreak"/>) keyed on <paramref name="tieSeed"/>, the year and
        /// the place, so the same count on the same seed calls the same tie the same way, and a recount from a stored contest re-derives it. Throws where the
        /// catalog holds either year not, or where the two years' jurisdictions or districts differ.
        /// </summary>
        public static Jurisdiction[] FromNationalShares(int priorYear, int electorsYear, double[] national, out StateCall[] calls, out double worstResidual) =>
            FromNationalShares(priorYear, electorsYear, national, 0, out calls, out worstResidual);

        /// <summary>PS-6 US-8c: <see cref="FromNationalShares(int, int, double[], out StateCall[], out double)"/> with its exact ties drawn on <paramref name="tieSeed"/>
        /// (the overload without one draws on seed 0 - the instruments' and the planted counts'; the game's count hands it the world's master seed).</summary>
        public static Jurisdiction[] FromNationalShares(int priorYear, int electorsYear, double[] national, int tieSeed, out StateCall[] calls, out double worstResidual)
        {
            if (national == null || national.Length != 3) { throw new ArgumentException("three national shares: the Democratic nominee, the Republican, every other candidate"); }
            var prior = new List<(int Year, string State, int Electors, long VotesD, long VotesR, long VotesOther, long VotesTotal, int CastD, int CastR, int CastOther, string CastOtherTo)>();
            var inForce = new List<(int Year, string State, int Electors, long VotesD, long VotesR, long VotesOther, long VotesTotal, int CastD, int CastR, int CastOther, string CastOtherTo)>();
            foreach (var state in UsPresidentialReturns.States)
            {
                if (state.Year == priorYear) { prior.Add(state); }
                if (state.Year == electorsYear) { inForce.Add(state); }
            }
            if (prior.Count == 0) { throw new ArgumentException($"the catalog holds no presidential election of {priorYear} to swing from"); }
            if (inForce.Count != prior.Count) { throw new ArgumentException($"the catalog holds {inForce.Count} jurisdictions' electors for {electorsYear} against {prior.Count} in {priorYear}"); }

            var regions = new RegionalVoteModel.RegionInput[prior.Count];
            var priorShares = new double[prior.Count][];
            for (int r = 0; r < prior.Count; r++)
            {
                if (inForce[r].State != prior[r].State) { throw new ArgumentException($"{electorsYear}'s jurisdiction {inForce[r].State} stands where {priorYear}'s {prior[r].State} does"); }
                regions[r] = new RegionalVoteModel.RegionInput(prior[r].State, prior[r].VotesTotal, null);
                priorShares[r] = new[] { (double)prior[r].VotesD / prior[r].VotesTotal, (double)prior[r].VotesR / prior[r].VotesTotal, (double)prior[r].VotesOther / prior[r].VotesTotal };
            }
            double[][] derived = RegionalVoteModel.RegionalSharesByUniformSwing(national, regions, priorShares, out worstResidual);

            calls = new StateCall[prior.Count];
            var college = new Jurisdiction[prior.Count];
            for (int r = 0; r < prior.Count; r++)
            {
                double[] shares = derived[r];
                int win = shares[ShareRepublican] > shares[ShareDemocrat] ? Republican : shares[ShareDemocrat] > shares[ShareRepublican] ? Democrat : TieBreak(tieSeed, electorsYear, prior[r].State);   // US-8c (J4): an exact tie drawn
                var call = new StateCall
                {
                    State = prior[r].State, Electors = inForce[r].Electors, Shares = shares, PriorShares = priorShares[r], Winner = win,
                    OthersLead = shares[ShareOthers] > Math.Max(shares[ShareDemocrat], shares[ShareRepublican]),
                };
                calls[r] = call;
                var districts = new List<(int District, long VotesD, long VotesR)>();
                int districtsInForce = 0;
                foreach (var d in UsPresidentialReturns.Districts)
                {
                    if (d.State != prior[r].State) { continue; }
                    if (d.Year == priorYear) { districts.Add((d.District, d.VotesD, d.VotesR)); }
                    if (d.Year == electorsYear) { districtsInForce++; }
                }
                if (districts.Count == 0) { college[r] = new Jurisdiction(call.State, call.Electors, win); continue; }
                if (electorsYear != priorYear && districtsInForce > 0 && districtsInForce != districts.Count)
                {
                    throw new ArgumentException($"{call.State}: {districts.Count} districts in {priorYear}, {districtsInForce} in {electorsYear}");
                }
                districts.Sort((a, b) => a.District.CompareTo(b.District));
                // US-4's DECLARED form: the state's change in its Democratic two-party share added to each district's own
                double statePrior = priorShares[r][ShareDemocrat] / (priorShares[r][ShareDemocrat] + priorShares[r][ShareRepublican]);
                double stateNext = shares[ShareDemocrat] / (shares[ShareDemocrat] + shares[ShareRepublican]);
                call.Districts = new int[districts.Count];
                call.DistrictDemocraticTwoParty = new double[districts.Count];
                call.DistrictWinners = new int[districts.Count];
                for (int k = 0; k < districts.Count; k++)
                {
                    double d2 = (double)districts[k].VotesD / (districts[k].VotesD + districts[k].VotesR);
                    double d2Next = d2 + (stateNext - statePrior);
                    call.Districts[k] = districts[k].District;
                    call.DistrictDemocraticTwoParty[k] = d2Next;
                    call.DistrictWinners[k] = d2Next > 0.5 ? Democrat : d2Next < 0.5 ? Republican : TieBreak(tieSeed, electorsYear, call.State + "-" + call.Districts[k].ToString(System.Globalization.CultureInfo.InvariantCulture));   // US-8c (J4, Code's reading): a district's exact tie drawn too
                }
                college[r] = new Jurisdiction(call.State, call.Electors, win, call.Electors - districts.Count, call.DistrictWinners);
            }
            return college;
        }

        /// <summary>
        /// PS-6 US-8c - Elias's ruling J4 (2026-10-19, `COMPLETED.md` §870): AN EXACT TIE BROKEN BY A SEEDED DRAW, never in one party's favour - a fair coin
        /// (<see cref="Republican"/> or <see cref="Democrat"/>, each with probability one half over seeds) keyed on <paramref name="seed"/>, the election's
        /// <paramref name="year"/> and the <paramref name="place"/> (a state's postal code, or a district's "ME-2"): SplitMix64's finaliser over the three,
        /// the place's characters folded in one by one (the generator `SimulationRandom` keys its common draws by - CONVENTION, not a figure about the world).
        /// Pure: no stream is drawn, so no count of record moves; the same seed, year and place give the same call wherever it is asked (the count, a recount
        /// from a stored contest, a fork). DECLARED: a recount and the statutes' lot (each state's own tie rule) are not modelled - the draw stands in for both.
        /// </summary>
        public static int TieBreak(int seed, int year, string place)
        {
            unchecked
            {
                ulong z = Mix((ulong)(uint)seed ^ ((ulong)(uint)year << 32));
                foreach (char ch in place ?? string.Empty) { z = Mix(z ^ ch); }
                return (Mix(z) & 1UL) == 0UL ? Republican : Democrat;
            }
        }

        /// <summary>SplitMix64's finaliser (Steele, Lea and Flood 2014; the constants are the published ones, as in `SimulationRandom`) - CONVENTION.</summary>
        private static ulong Mix(ulong z)
        {
            unchecked
            {
                z += 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>PS-6 US-8: the catalog's latest presidential election before <paramref name="year"/> - the prior a predicted election of that year
        /// swings from (R-US14 (a)); false where the catalog holds none.</summary>
        public static bool TryPriorYear(int year, out int prior)
        {
            prior = 0;
            foreach (var y in UsPresidentialReturns.Years) { if (y.Year < year && y.Year > prior) { prior = y.Year; } }
            return prior > 0;
        }

        /// <summary>PS-6 US-8: the catalog's latest presidential election in or before <paramref name="year"/> - whose electors are in force for an
        /// election that year; past the catalog's last they are carried forward (R-US13 (a), RULED - F8: the 2020-census allocation until the 2030
        /// apportionment is sourced). False where the catalog holds none.</summary>
        public static bool TryElectorsYear(int year, out int inForce)
        {
            inForce = 0;
            foreach (var y in UsPresidentialReturns.Years) { if (y.Year <= year && y.Year > inForce) { inForce = y.Year; } }
            return inForce > 0;
        }

        /// <summary>
        /// One jurisdiction's rule and result. <paramref name="districtWinners"/> is null or empty
        /// for a winner-take-all jurisdiction; where present, each entry is the winning candidate
        /// index in one congressional district, and <paramref name="atLargeElectors"/> go to the
        /// statewide winner (Maine 2 + 2 districts; Nebraska 2 + 3 districts).
        /// </summary>
        public readonly struct Jurisdiction
        {
            public readonly string Name;
            public readonly int TotalEv;
            public readonly int StatewideWinner;
            public readonly int AtLargeElectors;
            public readonly int[] DistrictWinners;

            public Jurisdiction(string name, int totalEv, int statewideWinner)
            {
                Name = name;
                TotalEv = totalEv;
                StatewideWinner = statewideWinner;
                AtLargeElectors = totalEv;
                DistrictWinners = null;
            }

            public Jurisdiction(string name, int totalEv, int statewideWinner, int atLargeElectors, int[] districtWinners)
            {
                Name = name;
                TotalEv = totalEv;
                StatewideWinner = statewideWinner;
                AtLargeElectors = atLargeElectors;
                DistrictWinners = districtWinners;
            }

            public bool UsesDistrictMethod => DistrictWinners != null && DistrictWinners.Length > 0;
        }

        /// <summary>
        /// Electors won by each candidate in one jurisdiction, by its own rule. A district-method
        /// jurisdiction's at-large electors follow the statewide plurality and each district's
        /// elector follows that district's plurality; a winner-take-all jurisdiction gives every
        /// elector to the statewide plurality winner.
        /// </summary>
        public static int[] AllocateJurisdiction(Jurisdiction jurisdiction, int candidateCount)
        {
            var result = new int[candidateCount];
            if (!jurisdiction.UsesDistrictMethod)
            {
                result[jurisdiction.StatewideWinner] += jurisdiction.TotalEv;
                return result;
            }

            if (jurisdiction.AtLargeElectors + jurisdiction.DistrictWinners.Length != jurisdiction.TotalEv)
            {
                throw new ArgumentException(
                    $"{jurisdiction.Name}: {jurisdiction.AtLargeElectors} at-large + " +
                    $"{jurisdiction.DistrictWinners.Length} district electors != {jurisdiction.TotalEv} total");
            }

            result[jurisdiction.StatewideWinner] += jurisdiction.AtLargeElectors;
            foreach (int districtWinner in jurisdiction.DistrictWinners)
            {
                result[districtWinner]++;
            }

            return result;
        }

        /// <summary>The whole college: every jurisdiction by its own rule, summed. Throws if the electors do not total <see cref="TotalElectors"/> — a silent miscount is the one failure this must never produce.</summary>
        public static int[] Allocate(Jurisdiction[] jurisdictions, int candidateCount)
        {
            var total = new int[candidateCount];
            int evSum = 0;
            foreach (Jurisdiction jurisdiction in jurisdictions)
            {
                int[] allocated = AllocateJurisdiction(jurisdiction, candidateCount);
                for (int c = 0; c < candidateCount; c++)
                {
                    total[c] += allocated[c];
                }

                evSum += jurisdiction.TotalEv;
            }

            if (evSum != TotalElectors)
            {
                throw new ArgumentException($"jurisdictions total {evSum} electors, not {TotalElectors}");
            }

            return total;
        }

        /// <summary>
        /// The same college computed as if EVERY jurisdiction were winner-take-all — the
        /// counterfactual that measures what the district method is actually worth in a given
        /// cycle. (In 2024 it happens to produce the same national totals, because Maine's and
        /// Nebraska's district effects cancel; that coincidence is the reason the real rule had to
        /// be built rather than inferred from a matching total.)
        /// </summary>
        public static int[] AllocateAsIfWinnerTakeAll(Jurisdiction[] jurisdictions, int candidateCount)
        {
            var total = new int[candidateCount];
            foreach (Jurisdiction jurisdiction in jurisdictions)
            {
                total[jurisdiction.StatewideWinner] += jurisdiction.TotalEv;
            }

            return total;
        }

        /// <summary>The winning candidate index, or -1 when nobody reaches <see cref="MajorityToElect"/> (the Twelfth-Amendment contingent case, which this file reports and does not resolve - the game's count resolves it, R-US3 (a), in <see cref="UsPresidentialElection"/>).</summary>
        public static int Winner(int[] electors)
        {
            for (int c = 0; c < electors.Length; c++)
            {
                if (electors[c] >= MajorityToElect) { return c; }
            }

            return -1;
        }
    }
}
