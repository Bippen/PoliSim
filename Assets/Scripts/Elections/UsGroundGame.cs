using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-6 US-31 (`docs/specs/USA_STAGE_PLAN.md`, US-31; R-US16 (b), RULED - Elias's ruling I3, 2026-10-18: "(b) for the USA ... The ground game's state
    /// effect enters the presidential share, the Senate race and R-US18 (b)'s House swing alike"): <b>THE STATE-BY-STATE COUNT AND THE AI'S TARGETING</b>.
    /// <list type="bullet">
    /// <item><description><b>The count with the ground game.</b> Where the player's campaign ran to the polling day, each jurisdiction is counted by W-D1's
    /// <see cref="ElectionDay.Count"/> over the campaign's regional mobilization, with W-D1's declared regional noise (<see cref="NoiseSigmaPp"/>), on top of
    /// R-US14's derived shares: the derived shares are each jurisdiction's preference, the two nominees' ground contacts its mobilization. The state effect -
    /// the counted shares less the derived - is kept on the contest (<see cref="StateEffect"/>), so a stored contest re-derives its own states and a save
    /// carries what no later run can re-draw. A game with no campaign, and a midterm (US-27: none runs), count as before.</description></item>
    /// <item><description><b>The frame</b> (DECLARED, US-9's, the frame R-US16 was sized on): one base turnout, the staging's votes cast over its electorate,
    /// which cancels from every share unless a rate clamps; the other turnout attributes neutral (50, untouched); the two nominees' columns carry their
    /// parties' contacts, every other candidate's none (no campaigning party backs one - US-30 (c)).</description></item>
    /// <item><description><b>The noise is keyed, never counted</b> (<see cref="SimulationRandom.RegionalElectionNoiseSeedOn"/>): the master seed, the
    /// country and the polling day, so the played world, a load of its save and every fork of it draw the same numbers for the same count (R-US25's common
    /// random numbers, US-8b's rule), and no counted stream moves.</description></item>
    /// <item><description><b>The AI's targeting</b> (<see cref="TargetingFor"/>, [AUTHORED-DRAFT], on the play-calibration list): a jurisdiction's weight
    /// is its electors times §25's swing index (<see cref="SwingRegions.Index"/>) of its margin as the count would derive it from the last published poll;
    /// an AI weighs its local acts in the heaviest, in place of the largest audiences.</description></item>
    /// </list>
    /// The USA's alone: Sweden's and Germany's campaigns are counted nationally and their AI weighs the largest audiences (R-US16 (c) is not ruled).
    /// </summary>
    public static class UsGroundGame
    {
        /// <summary>W-D1's declared regional noise - σ in points of a party's share in a jurisdiction, `RegionalAggregation.RegionalNoiseSigmaPp`
        /// ([AUTHORED-DRAFT] there): the figure R-US16 (b) was sized on (US-9: the closest states flip in up to a quarter of counts, the presidency in none
        /// of 400) and the plan's words, "with its declared regional noise". DECLARED.</summary>
        public static double NoiseSigmaPp => RegionalAggregation.RegionalNoiseSigmaPp;

        /// <summary>CONVENTION: §26's turnout attributes held at 50, untouched - the count's frame reads the ground game's mobilization and nothing else (US-9's frame).</summary>
        private const double Neutral = 50.0;

        /// <summary>One jurisdiction's state effect: the counted shares less the derived, in <see cref="ElectoralCollege.ShareDemocrat"/>'s order, and the
        /// Republican share of the two nominees' vote, counted less derived - the House swing reads it (R-US18 (b), I3). Saved on the contest.</summary>
        public sealed class StateEffect
        {
            public string State;
            public double Democrat;
            public double Republican;
            public double Others;
            public double RepublicanTwoParty;
        }

        /// <summary>What the count reads of a finished campaign: its jurisdictions by name, each one's electorate, the two nominees' parties' weighted ground
        /// contacts there, and the frame's base turnout.</summary>
        public sealed class Campaign
        {
            public string[] Regions;
            public double[] Eligible;
            public double[] DemocratContacts;
            public double[] RepublicanContacts;
            public double BaseTurnout;
        }

        /// <summary>The ground game of a finished campaign on <paramref name="setup"/> (<paramref name="result"/>'s mobilization), the two parties found by key
        /// (REP, DEM); null where either is not staged or the mobilization does not match the regions.</summary>
        public static Campaign Of(CampaignRun.Setup setup, CampaignRun.Result result)
        {
            if (result?.Gotv == null || setup.Parties == null || setup.Regions == null || result.Gotv.RegionCount != setup.Regions.Length) { return null; }
            int rep = -1, dem = -1;
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                if (setup.Parties[p].Name == "REP") { rep = p; }
                else if (setup.Parties[p].Name == "DEM") { dem = p; }
            }
            if (rep < 0 || dem < 0 || rep >= result.Gotv.PartyCount || dem >= result.Gotv.PartyCount) { return null; }
            int n = setup.Regions.Length;
            var c = new Campaign { Regions = new string[n], Eligible = new double[n], DemocratContacts = new double[n], RepublicanContacts = new double[n] };
            double audience = 0.0, eligible = 0.0;
            for (int r = 0; r < n; r++)
            {
                c.Regions[r] = setup.Regions[r].Name;
                c.Eligible[r] = result.Gotv.Eligible(r);
                c.DemocratContacts[r] = result.Gotv.WeightedContacts(r, dem);
                c.RepublicanContacts[r] = result.Gotv.WeightedContacts(r, rep);
                audience += setup.Regions[r].Audience;
                eligible += setup.Regions[r].Eligible;
            }
            c.BaseTurnout = eligible > 0.0 ? audience / eligible : 0.0;
            return c;
        }

        /// <summary>The seed a count's regional noise is drawn on - <see cref="SimulationRandom.RegionalElectionNoiseSeedOn"/>, keyed, never counted.</summary>
        public static int NoiseSeedOf(CountryId country, DateTime pollingDay) => SimulationRandom.RegionalElectionNoiseSeedOn(country, pollingDay);

        /// <summary>
        /// THE STATE EFFECT of <paramref name="campaign"/>'s ground game on the derived states <paramref name="derived"/>: W-D1's count of every jurisdiction
        /// the campaign staged (its preference the derived shares, its mobilization the two nominees' contacts), the noise of <paramref name="sigmaPp"/> drawn on
        /// <paramref name="noiseSeed"/>, against the derived shares. Pure. Null, with the reason, where a derived state has no staged jurisdiction or the
        /// frame would clamp a turnout rate (the count would no longer cancel the base).
        /// </summary>
        public static List<StateEffect> Effects(ElectoralCollege.StateCall[] derived, Campaign campaign, int noiseSeed, double sigmaPp, out string why)
        {
            why = null;
            if (derived == null || campaign?.Regions == null) { why = "no derived states or no campaign to count"; return null; }
            int n = campaign.Regions.Length;
            var at = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < derived.Length; i++) { at[derived[i].State] = i; }
            var names = new string[n];
            var preference = new double[n][];
            var gotv = new RegionalMobilization((double[])campaign.Eligible.Clone(), 3);
            for (int r = 0; r < n; r++)
            {
                if (!at.TryGetValue(campaign.Regions[r], out int s)) { why = campaign.Regions[r] + " is staged and has no derived state"; return null; }
                names[r] = campaign.Regions[r];
                preference[r] = (double[])derived[s].Shares.Clone();
                gotv.Carry(r, ElectoralCollege.ShareDemocrat, campaign.DemocratContacts[r]);
                gotv.Carry(r, ElectoralCollege.ShareRepublican, campaign.RepublicanContacts[r]);
            }
            if (at.Count != n) { why = "the derived states and the staged jurisdictions differ in number"; return null; }
            double most = 1.0;
            for (int r = 0; r < n; r++) { for (int p = 0; p < 2; p++) { most = Math.Max(most, TurnoutModel.Multiplier(gotv.Mobilization(r, p), TurnoutModel.MobilizationSpan)); } }
            if (campaign.BaseTurnout <= 0.0 || campaign.BaseTurnout * most >= 1.0) { why = "the frame would clamp a turnout rate"; return null; }
            ElectionDay.Result count = ElectionDay.Count(names, preference, gotv, campaign.BaseTurnout, Neutral, Neutral, Neutral, new System.Random(noiseSeed), sigmaPp);
            var effects = new List<StateEffect>(n);
            for (int r = 0; r < n; r++)
            {
                double[] votes = count.Regions[r].Votes;
                double sum = votes[0] + votes[1] + votes[2];
                if (sum <= 0.0) { why = names[r] + " counts no vote"; return null; }
                double[] before = preference[r];
                double d = votes[ElectoralCollege.ShareDemocrat] / sum, rp = votes[ElectoralCollege.ShareRepublican] / sum, o = votes[ElectoralCollege.ShareOthers] / sum;
                // a column that stands nowhere in the state (every other candidate where no minor is on its ballot, US-6c / J3) gets no vote from the noise:
                // DECLARED - its share stays 0 and the two nominees share the state as the count gave them
                if (before[ElectoralCollege.ShareOthers] == 0.0 && o != 0.0) { double two = d + rp; d /= two; rp /= two; o = 0.0; }
                effects.Add(new StateEffect
                {
                    State = names[r],
                    Democrat = d - before[ElectoralCollege.ShareDemocrat],
                    Republican = rp - before[ElectoralCollege.ShareRepublican],
                    Others = o - before[ElectoralCollege.ShareOthers],
                    RepublicanTwoParty = rp / (rp + d) - before[ElectoralCollege.ShareRepublican] / (before[ElectoralCollege.ShareRepublican] + before[ElectoralCollege.ShareDemocrat]),
                });
            }
            return effects;
        }

        /// <summary>
        /// The calls with each jurisdiction's state effect added to its derived shares (<paramref name="calls"/>, untouched) - the statewide winner the larger
        /// nominee (an exact tie drawn, US-8c), Maine's and Nebraska's districts moved by the state's change in its Democratic two-party share (US-4's DECLARED
        /// form) - and the college they give, the electors those of each call. A call with no effect is carried as it stands. Pure.
        /// </summary>
        public static ElectoralCollege.Jurisdiction[] Apply(ElectoralCollege.StateCall[] calls, IReadOnlyList<StateEffect> effects, int electorsYear, int tieSeed, out ElectoralCollege.StateCall[] applied)
        {
            var byState = new Dictionary<string, StateEffect>(StringComparer.Ordinal);
            if (effects != null) { foreach (StateEffect e in effects) { if (e?.State != null) { byState[e.State] = e; } } }
            applied = new ElectoralCollege.StateCall[calls.Length];
            var college = new ElectoralCollege.Jurisdiction[calls.Length];
            for (int i = 0; i < calls.Length; i++)
            {
                ElectoralCollege.StateCall call = calls[i];
                if (!byState.TryGetValue(call.State, out StateEffect e)) { applied[i] = call; college[i] = CollegeRow(call); continue; }
                var shares = new[] { call.Shares[ElectoralCollege.ShareDemocrat] + e.Democrat, call.Shares[ElectoralCollege.ShareRepublican] + e.Republican, call.Shares[ElectoralCollege.ShareOthers] + e.Others };
                int win = shares[ElectoralCollege.ShareRepublican] > shares[ElectoralCollege.ShareDemocrat] ? ElectoralCollege.Republican
                    : shares[ElectoralCollege.ShareDemocrat] > shares[ElectoralCollege.ShareRepublican] ? ElectoralCollege.Democrat : ElectoralCollege.TieBreak(tieSeed, electorsYear, call.State);
                var moved = new ElectoralCollege.StateCall
                {
                    State = call.State, Electors = call.Electors, Shares = shares, PriorShares = call.PriorShares, Winner = win,
                    OthersLead = shares[ElectoralCollege.ShareOthers] > Math.Max(shares[ElectoralCollege.ShareDemocrat], shares[ElectoralCollege.ShareRepublican]),
                    Districts = (int[])call.Districts.Clone(),
                    DistrictDemocraticTwoParty = new double[call.Districts.Length],
                    DistrictWinners = new int[call.Districts.Length],
                };
                double stateBefore = call.Shares[ElectoralCollege.ShareDemocrat] / (call.Shares[ElectoralCollege.ShareDemocrat] + call.Shares[ElectoralCollege.ShareRepublican]);
                double stateAfter = shares[ElectoralCollege.ShareDemocrat] / (shares[ElectoralCollege.ShareDemocrat] + shares[ElectoralCollege.ShareRepublican]);
                for (int k = 0; k < call.Districts.Length; k++)
                {
                    double d2 = call.DistrictDemocraticTwoParty[k] + (stateAfter - stateBefore);
                    moved.DistrictDemocraticTwoParty[k] = d2;
                    moved.DistrictWinners[k] = d2 > 0.5 ? ElectoralCollege.Democrat : d2 < 0.5 ? ElectoralCollege.Republican
                        : ElectoralCollege.TieBreak(tieSeed, electorsYear, call.State + "-" + call.Districts[k].ToString(CultureInfo.InvariantCulture));
                }
                applied[i] = moved;
                college[i] = CollegeRow(moved);
            }
            return college;
        }

        private static ElectoralCollege.Jurisdiction CollegeRow(ElectoralCollege.StateCall call) =>
            call.Districts == null || call.Districts.Length == 0
                ? new ElectoralCollege.Jurisdiction(call.State, call.Electors, call.Winner)
                : new ElectoralCollege.Jurisdiction(call.State, call.Electors, call.Winner, call.Electors - call.Districts.Length, call.DistrictWinners);

        /// <summary>The House swing each state's ground game adds (R-US18 (b) - I3: "alike"): the Republican share of the two nominees' vote, counted less
        /// derived, by state; null where the contest carries no effect.</summary>
        public static Dictionary<string, double> TwoPartySwing(IReadOnlyList<StateEffect> effects)
        {
            if (effects == null || effects.Count == 0) { return null; }
            var swing = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (StateEffect e in effects) { if (e?.State != null) { swing[e.State] = e.RepublicanTwoParty; } }
            return swing;
        }

        /// <summary>The count line's words for the ground game: the widest move of a margin (Republican less Democratic, points of the state's vote) and the
        /// calls it changed from the derived states.</summary>
        public static string LineOf(ElectoralCollege.StateCall[] derived, ElectoralCollege.StateCall[] counted)
        {
            double widest = 0.0;
            string widestAt = null;
            var changed = new List<string>();
            for (int i = 0; i < derived.Length && i < counted.Length; i++)
            {
                double move = 100.0 * ((counted[i].Shares[ElectoralCollege.ShareRepublican] - counted[i].Shares[ElectoralCollege.ShareDemocrat])
                                       - (derived[i].Shares[ElectoralCollege.ShareRepublican] - derived[i].Shares[ElectoralCollege.ShareDemocrat]));
                if (widestAt == null || Math.Abs(move) > Math.Abs(widest)) { widest = move; widestAt = counted[i].State; }
                bool districts = derived[i].DistrictWinners.Length == counted[i].DistrictWinners.Length;
                for (int k = 0; districts && k < derived[i].DistrictWinners.Length; k++) { districts = derived[i].DistrictWinners[k] == counted[i].DistrictWinners[k]; }
                if (derived[i].Winner != counted[i].Winner || !districts) { changed.Add(counted[i].State); }
            }
            return string.Format(CultureInfo.InvariantCulture, "each state counted with the campaign ground game and the regional noise (sigma {0:0.0} points): the widest move of a margin {1} {2:+0.00;-0.00} points; calls changed from the derived states: {3}",
                NoiseSigmaPp, widestAt ?? "none", widest, changed.Count == 0 ? "none" : string.Join(", ", changed));
        }

        /// <summary>
        /// THE AI'S TARGETING RULE for a US campaign polled on <paramref name="pollingDay"/> - [AUTHORED-DRAFT], on the play-calibration list (R-US16 (b)'s
        /// words, "electors x closeness on the last published poll"; docs/specs/ELECTIONS_CAMPAIGN_SPEC.md §25's swing regions and §10's "concentrate resources in a few
        /// swing regions"): each jurisdiction of <paramref name="regions"/> weighs its electors in force times §25's swing index of its margin, the count's own
        /// derivation of the poll's shares (<paramref name="keys"/>, the staging's party order) - E1's factors, then the states (R-US14 (a); in a game that has
        /// counted, the game's own previous count's states, R-US24 (b)) - so the AI reads the race as the count would read it, and nothing true. A jurisdiction
        /// at or past the index's full scale weighs nothing: it is not contested (§25's own words for the scale). DECLARED: the previous count read at the
        /// staging (<paramref name="played"/>; none handed, the record's).
        /// </summary>
        public static RegionTargeting TargetingFor(DateTime pollingDay, string[] keys, RegionAudience[] regions, Country played)
        {
            if (keys == null || regions == null) { return null; }
            UsPresidentialElection.Contest previous = UsPresidentialElection.PreviousCount(played, pollingDay.Date);
            ElectoralCollege.StateCall[] previousStates = previous != null ? UsPresidentialElection.StatesOf(played, previous) : null;
            IReadOnlyList<UsTermLimit.Nominee> nominees = played != null ? UsTermLimit.NomineesFor(played, pollingDay.Date) : null;   // the field the count will read (the player's own nominee among them)
            string[] names = new string[regions.Length];
            for (int r = 0; r < regions.Length; r++) { names[r] = regions[r].Name; }
            string[] parties = (string[])keys.Clone();
            double[] WeightsOf(Poll poll)
            {
                var weights = new double[names.Length];
                if (poll.PartyCount != parties.Length) { return weights; }
                var shares = new Dictionary<string, double>(StringComparer.Ordinal);
                for (int i = 0; i < parties.Length; i++) { shares[parties[i]] = poll.Share(i); }
                UsPresidentialElection.Contest read = UsPresidentialElection.Count(CountryId.USA, pollingDay.Date, shares, nominees, 0, previous, previousStates, out ElectoralCollege.StateCall[] states, out string _);
                if (read == null || states == null) { return weights; }
                var at = new Dictionary<string, ElectoralCollege.StateCall>(StringComparer.Ordinal);
                foreach (ElectoralCollege.StateCall c in states) { at[c.State] = c; }
                for (int r = 0; r < names.Length; r++)
                {
                    if (!at.TryGetValue(names[r], out ElectoralCollege.StateCall c)) { continue; }
                    weights[r] = c.Electors * SwingRegions.Index(100.0 * (c.Shares[ElectoralCollege.ShareRepublican] - c.Shares[ElectoralCollege.ShareDemocrat]));
                }
                return weights;
            }
            return new RegionTargeting("electors x the swing index of the margin the count derives from the last published poll", WeightsOf);
        }
    }
}
