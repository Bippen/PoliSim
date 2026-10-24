using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;

namespace PoliSim.Elections
{
    /// <summary>
    /// GATE-18 q2 (`POLISIM_FEATURE_LIST.md` row 5, the ORDER: "turnout and the declared noise are on in play") with A-LOOP q24 piece 5b
    /// (`docs/specs/A_LOOP_PLAN.md` §5, Q6): <b>THE PARLIAMENTARY COUNT IN PLAY</b> - Sweden's, Germany's and Poland's chamber elections counted
    /// region by region by W-D1's `ElectionDay.Count`, on each region's own turnout, with the campaign's doors at 5b's turnout CACE and W-D1's
    /// declared regional noise, keyed.
    /// <list type="bullet">
    /// <item><description><b>Where it starts.</b> The national shares the count is handed - the campaign's final shares through the tactical layer, or
    /// with no campaign the prediction - spread over the regions as the night already spreads them (the uniform swing from the seated election:
    /// Sweden's 29 valkretsar, Germany's 16 Länder on the chamber seated that day, Poland's 41 okregi from 2023 - the derivation the chamber and the
    /// night read, `NationalElection.RegionalOf`, `PolishSejmAllocation.SwungVotes`).</description></item>
    /// <item><description><b>Turnout.</b> Each region's base turnout is its own record's: its valid votes over its electorate (the valkrets roll, the
    /// Land's Wahlberechtigte). A party's supporters turn out at that base plus 5b's lift on the share its doors reached (`GotvTurnout`). Poland's
    /// okregi carry no electorate on disk and no campaign is staged for Poland, so its okregi count their 2023 size at a base of one (DECLARED: with
    /// no doors anywhere a uniform rate cancels from every share; no Polish turnout is printed).</description></item>
    /// <item><description><b>The noise is keyed, never counted</b>: drawn on `SimulationRandom.RegionalElectionNoiseSeedOn` (the master seed, the
    /// country, the polling day), as the US count's (`UsGroundGame`), so the played world, a load and every fork draw the same numbers for the same
    /// count and no counted stream moves. σ is W-D1's declared regional figure, `RegionalAggregation.RegionalNoiseSigmaPp` ([AUTHORED-DRAFT] there).
    /// No national noise is drawn: the national shares are the vote-weighted sum of the regions, so the regional noise reaches the nation at the 1/√N_eff
    /// rate W-D1 measured, and nothing else.</description></item>
    /// <item><description><b>The noise at zero for the gate.</b> GATE-18 measures an idle game's reproduction of a record with the noise at zero
    /// (`NoiseAtZero`, a scope its measurement opens): at σ 0 with no doors the count returns the shares it was handed to the rounding of a vote, so
    /// the gate measures the vote model and the factors, not a draw. Play counts at the declared σ.</description></item>
    /// <item><description><b>Seats.</b> The counted national shares are what the chamber is allocated on, by each country's law as before
    /// (`NationalElection.Run`): Sweden's and Germany's allocations are national; Poland's spreads the counted national vote back over the okregi by
    /// the same swing, so its chamber and its night stay one count (§902) and the okregi's noise reaches the Sejm through the national vote alone
    /// (DECLARED). Sweden's and Germany's readout becomes the regions as counted, so the night's constituencies add up to the counted headline.</description></item>
    /// </list>
    /// </summary>
    public static class ParliamentaryCount
    {
        /// <summary>A count's result: the counted shares the chamber is allocated on, and what moved them.</summary>
        public sealed class Result
        {
            /// <summary>The counted national shares, by party key - what the chamber is allocated on.</summary>
            public Dictionary<string, double> Shares;
            /// <summary>The shares the count was handed, by party key.</summary>
            public Dictionary<string, double> Handed;
            /// <summary>The ground game's effect per party, in points of the national vote: the count less the same count with no doors (the same noise).</summary>
            public Dictionary<string, double> GroundPp;
            /// <summary>Each party's bound on `GroundPp`: its supporters reached in every region times the CACE's upper limit, over the votes cast.</summary>
            public Dictionary<string, double> GroundBoundPp;
            /// <summary>The national turnout counted (votes cast over the electorate); NaN where the regions carry no electorate (Poland).</summary>
            public double Turnout;
            /// <summary>The same with no doors.</summary>
            public double TurnoutWithoutGround;
            /// <summary>The largest per-region supporters' turnout the count reached - below 1 wherever 5b holds (never at the clamp).</summary>
            public double HighestPartyTurnout;
            public int Regions;
            public double SigmaPp;
            public int NoiseSeed;
            public string Digest;
            public string Line;
            /// <summary>The count's own national shares (the regions' vote-weighted sum), by party key - before Poland's hand-back.</summary>
            public Dictionary<string, double> CountedShares;
            /// <summary>Poland: the same count at sigma 0 with no doors, by party key - the okregi's spread of the shares handed, which the hand-back
            /// subtracts; null for a country whose regions are counted on their own electorate.</summary>
            public Dictionary<string, double> StillShares;
        }

        /// <summary>CONVENTION: the smallest ground-game move the count line names, in points - a reading floor, not a figure about the world.</summary>
        private const double PrintFloorPp = 0.005;

        private static int _noiseAtZero;

        /// <summary>The σ a count in play draws at: W-D1's declared regional noise, or zero inside `NoiseAtZero`.</summary>
        public static double SigmaPp => _noiseAtZero > 0 ? 0.0 : RegionalAggregation.RegionalNoiseSigmaPp;

        /// <summary>GATE-18's "the noise at zero for this gate": every count inside the scope draws σ 0 (dispose to restore). The gate's own
        /// measurement opens it around an idle game; play never does.</summary>
        public static IDisposable NoiseAtZero()
        {
            _noiseAtZero++;
            return new Release();
        }

        private sealed class Release : IDisposable
        {
            private bool _done;
            public void Dispose() { if (!_done) { _done = true; _noiseAtZero--; } }
        }

        /// <summary>Whether <paramref name="country"/>'s chamber is counted here: Sweden, Germany and Poland - the three with a chamber count and regions.</summary>
        public static bool CountsIn(CountryId country) => country == CountryId.Sweden || country == CountryId.Germany || country == CountryId.Poland;

        /// <summary>
        /// The count of <paramref name="country"/>'s polling day <paramref name="pollingDay"/> on the national <paramref name="shares"/> (fractions of the valid vote summing to one, as play hands them - the uniform swing reads them so, DECLARED), with the ground
        /// game of <paramref name="gotv"/> (a finished campaign's mobilization, its regions and parties those of <paramref name="setup"/>; null - no
        /// campaign ran - counts no doors), at `SigmaPp`. Pure but for the readout, which a Swedish or German count sets to its counted
        /// regions (<paramref name="writeReadout"/>). Null, with nothing written, for a country not counted here or shares it cannot spread.
        /// </summary>
        public static Result Count(CountryId country, DateTime pollingDay, IReadOnlyDictionary<string, double> shares, CampaignRun.Setup? setup, RegionalMobilization gotv, bool writeReadout = true)
        {
            if (!CountsIn(country) || shares == null || shares.Count == 0) { return null; }
            // the review 2's L6: every count runs on the party keys in ordinal order, so a party's noise draw is tied to its key, not to the order a
            // world's dictionary happens to hold - a played world and its shadow draw the same numbers for the same party whatever their insertion
            // order (Poland's okregi already read this order, `PolishSejmAllocation.Allocate`); the readout is written back in the handed order
            var handedOrder = new List<string>(shares.Keys);
            var keys = new List<string>(shares.Keys);
            keys.Sort(StringComparer.Ordinal);
            var national = new double[keys.Count];
            for (int i = 0; i < keys.Count; i++) { national[i] = shares[keys[i]]; }

            string[] names;
            double[][] preference;
            double[] eligible, baseTurnout;
            bool hasElectorate;
            if (country == CountryId.Poland)
            {
                double[][] votes = PolishSejmAllocation.SwungVotes(keys, shares);
                names = new string[votes.Length];
                preference = votes;
                eligible = new double[votes.Length];
                baseTurnout = new double[votes.Length];
                for (int d = 0; d < votes.Length; d++)
                {
                    names[d] = "okreg " + (d + 1).ToString(CultureInfo.InvariantCulture);
                    foreach (double v in votes[d]) { eligible[d] += v; }
                    baseTurnout[d] = 1.0;
                }
                // DECLARED (the review 2's L3): a base of one is a frame, not a turnout - every supporter already votes, so a door could add nothing
                // here (the clamp in `GotvTurnout.PartyTurnout`). No Polish campaign is staged, so no door reaches this count; a Polish staging must bring
                // each okreg's electorate before its doors can count.
                hasElectorate = false;
            }
            else
            {
                preference = NationalElection.RegionalOf(country, keys, national, pollingDay, out names, out double[] valid);
                if (preference == null) { return null; }
                eligible = new double[names.Length];
                baseTurnout = new double[names.Length];
                for (int r = 0; r < names.Length; r++)
                {
                    eligible[r] = country == CountryId.Sweden ? SwedishRegions.EligibleAt(r) : GermanRegions.EligibleAt(r, pollingDay);
                    baseTurnout[r] = eligible[r] > 0.0 ? valid[r] / eligible[r] : 0.0;
                }
                hasElectorate = true;
            }

            int n = names.Length;
            var cace = new double[n];
            for (int r = 0; r < n; r++) { cace[r] = GotvTurnout.DoorCacePp(country, baseTurnout[r]); }
            RegionalMobilization ground = DoorsOf(names, keys, eligible, setup, gotv);
            var none = new RegionalMobilization((double[])eligible.Clone(), keys.Count);
            double sigma = SigmaPp;
            int seed = SimulationRandom.RegionalElectionNoiseSeedOn(country, pollingDay);
            ElectionDay.Result counted = ElectionDay.Count(names, preference, ground, baseTurnout, cace, new System.Random(seed), sigma);
            ElectionDay.Result idle = ElectionDay.Count(names, preference, none, baseTurnout, cace, new System.Random(seed), sigma);
            // Poland: the okregi's swing floors a list at zero and is not re-solved (`PolishSejmAllocation.SwungVotes`), so their vote-weighted sum can sit off the
            // shares handed; the count hands back the shares handed plus what the count moved (the count less the same count at sigma 0 with no doors), so
            // the floor's residue never becomes a share (DECLARED; the review's M1)
            ElectionDay.Result still = hasElectorate ? null : ElectionDay.Count(names, preference, none, baseTurnout, cace, new System.Random(seed), 0.0);

            var result = new Result
            {
                Shares = new Dictionary<string, double>(), Handed = new Dictionary<string, double>(), GroundPp = new Dictionary<string, double>(), GroundBoundPp = new Dictionary<string, double>(),
                Turnout = hasElectorate ? counted.NationalTurnout : double.NaN, TurnoutWithoutGround = hasElectorate ? idle.NationalTurnout : double.NaN,
                Regions = n, SigmaPp = sigma, NoiseSeed = seed, Digest = counted.Digest,
            };
            double highest = 0.0;
            // Poland's hand-back, held at zero and renormalised so the 5 % and 8 % lines are read on shares summing to one (the review 2's L2)
            var back = new double[keys.Count];
            double backSum = 0.0;
            for (int i = 0; i < keys.Count; i++)
            {
                back[i] = still == null ? counted.NationalShares[i] : Math.Max(0.0, national[i] + counted.NationalShares[i] - still.NationalShares[i]);
                backSum += back[i];
            }
            result.CountedShares = new Dictionary<string, double>();
            result.StillShares = still == null ? null : new Dictionary<string, double>();
            for (int i = 0; i < keys.Count; i++)
            {
                result.Shares[keys[i]] = still != null && backSum > 0.0 ? back[i] / backSum : back[i];
                result.CountedShares[keys[i]] = counted.NationalShares[i];
                if (still != null) { result.StillShares[keys[i]] = still.NationalShares[i]; }
                result.Handed[keys[i]] = national[i];
                result.GroundPp[keys[i]] = 100.0 * (counted.NationalShares[i] - idle.NationalShares[i]);
                double reach = 0.0;
                for (int r = 0; r < n; r++)
                {
                    double sum = 0.0;
                    foreach (double v in preference[r]) { sum += v; }
                    double supporters = sum > 0.0 ? eligible[r] * preference[r][i] / sum : 0.0;
                    double doors = ground.DoorContacts(r, i), calls = ground.PhoneContacts(r, i);
                    reach += supporters * GotvTurnout.Coverage(doors, supporters) * GotvTurnout.DoorCaceUpperPp(country, baseTurnout[r]) / 100.0
                             + supporters * GotvTurnout.Coverage(calls, supporters) * GotvTurnout.PhoneCaceUpperPp / 100.0;   // K82: the calls' bound beside the doors'
                    highest = Math.Max(highest, GotvTurnout.PartyTurnout(baseTurnout[r], doors, supporters, cace[r], calls));
                }
                result.GroundBoundPp[keys[i]] = counted.VotesCast > 0.0 ? 100.0 * reach / counted.VotesCast : 0.0;
            }
            result.HighestPartyTurnout = hasElectorate ? highest : double.NaN;   // Poland's base of one is a frame, not a turnout (the review's L3)

            if (writeReadout && hasElectorate)
            {
                var regional = new double[n][];
                var weights = new double[n];
                for (int r = 0; r < n; r++)
                {
                    double[] v = counted.Regions[r].Votes;
                    double sum = 0.0;
                    foreach (double x in v) { sum += x; }
                    regional[r] = new double[handedOrder.Count];
                    for (int j = 0; j < handedOrder.Count; j++) { int i = keys.IndexOf(handedOrder[j]); regional[r][j] = sum > 0.0 ? v[i] / sum : 0.0; }   // the night's columns in the handed order
                    weights[r] = sum;
                }
                NationalElection.SetCountedReadout(handedOrder, names, regional, weights);
            }
            result.Line = LineOf(country, result, keys, gotv != null);
            return result;
        }

        /// <summary>The count's mobilization: each counted region's doors per party key, carried from the campaign's own (its regions and parties found by
        /// name); none where no campaign ran, or for a region or party the campaign did not stage.</summary>
        private static RegionalMobilization DoorsOf(string[] names, List<string> keys, double[] eligible, CampaignRun.Setup? setup, RegionalMobilization gotv)
        {
            var doors = new RegionalMobilization((double[])eligible.Clone(), keys.Count);
            if (gotv == null || !setup.HasValue || setup.Value.Regions == null || setup.Value.Parties == null) { return doors; }
            CampaignRun.Setup s = setup.Value;
            var regionAt = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int r = 0; r < s.Regions.Length && r < gotv.RegionCount; r++) { regionAt[s.Regions[r].Name] = r; }
            var partyAt = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int p = 0; p < s.Parties.Length && p < gotv.PartyCount; p++) { partyAt[s.Parties[p].Name] = p; }
            for (int r = 0; r < names.Length; r++)
            {
                if (!regionAt.TryGetValue(names[r], out int sr)) { continue; }
                for (int i = 0; i < keys.Count; i++)
                {
                    if (partyAt.TryGetValue(keys[i], out int sp)) { doors.CarryDoors(r, i, gotv.DoorContacts(sr, sp)); doors.CarryCalls(r, i, gotv.PhoneContacts(sr, sp)); }   // K82: and the calls
                }
            }
            return doors;
        }

        private static string LineOf(CountryId country, Result r, List<string> keys, bool campaign)
        {
            var moved = new StringBuilder();
            double noiseMost = 0.0;
            foreach (string k in keys)
            {
                double ground = r.GroundPp[k];
                if (Math.Abs(ground) >= PrintFloorPp) { moved.Append(moved.Length > 0 ? ", " : string.Empty).Append(k).Append(' ').Append(ground.ToString("+0.00;-0.00", CultureInfo.InvariantCulture)).Append(" pp"); }
                noiseMost = Math.Max(noiseMost, Math.Abs(100.0 * (r.Shares[k] - r.Handed[k]) - ground));
            }
            string turnout = double.IsNaN(r.Turnout)
                ? "no turnout is counted (the okregi carry no electorate on disk)"
                : string.Format(CultureInfo.InvariantCulture, "turnout {0:0.00} % ({1:0.00} % without the doors)", 100.0 * r.Turnout, 100.0 * r.TurnoutWithoutGround);
            return string.Format(CultureInfo.InvariantCulture,
                "{0} - {1} regions counted, each on its own turnout, the doors at the turnout effect ({2}) and the regional noise (sigma {3:0.0} points at a share of one half, scaled by the square root of s(1-s), keyed); {4}; the ground game moved {5}; the noise moved a national share by at most {6:0.00} points; digest {7}",
                country, r.Regions, country == CountryId.USA ? "the US band" : string.Format(CultureInfo.InvariantCulture, "{0:0.00} points among the reached", GotvTurnout.EuropeanDoorCacePp),
                r.SigmaPp, turnout, !campaign ? "nothing (no campaign ran)" : moved.Length == 0 ? "no share by 0.005 points" : moved.ToString(), noiseMost, r.Digest);
        }
    }
}
