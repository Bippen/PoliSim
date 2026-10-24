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
    /// <item><description><b>The frame</b> (K18, Elias's ruling of 2026-10-23 - the US count onto GATE-18 piece one's 5b, the US anchor Gerber &amp; Green's band
    /// per K7): each jurisdiction at its own base turnout, its votes cast of record over its electorate (the staging's CVAP); a nominee's supporters there
    /// turn out at the base plus the door CACE on the share of them its opened doors reached (`GotvTurnout.DoorCacePp`, the band of that base) and the phone
    /// CACE on the share its completed calls reached; the two nominees' columns carry their parties' doors and calls, every other candidate's none (no
    /// campaigning party backs one - US-30 (c)). Until K18 the count read W-B11's mobilisation curve on one national base (US-9's frame, the one R-US16 was
    /// sized on); that function stays as US-31's registered one (`EffectsOnCurve`), read by its measurement alone.</description></item>
    /// <item><description><b>The noise is keyed, never counted</b> (<see cref="SimulationRandom.RegionalElectionNoiseSeedOn"/>): the master seed, the
    /// country and the polling day, so the played world, a load of its save and every fork of it draw the same numbers for the same count (R-US25's common
    /// random numbers, US-8b's rule), and no counted stream moves.</description></item>
    /// <item><description><b>The AI's targeting</b> (<see cref="TargetingFor"/>, [AUTHORED-DRAFT], on the play-calibration list): a jurisdiction's weight
    /// is its electors times §25's swing index (<see cref="SwingRegions.Index"/>) of its margin as the count would derive it from the last published poll;
    /// an AI weighs its local acts in the heaviest, in place of the largest audiences.</description></item>
    /// </list>
    /// The USA's alone: Sweden's and Germany's AI weighs the largest audiences (R-US16 (c) is not ruled); their chambers are counted region by region since GATE-18 q2, by `ParliamentaryCount` on 5b's turnout CACE at Cantoni &amp; Pons's figure, this count on the same 5b at Gerber &amp; Green's band (K7, K18).
    /// </summary>
    public static class UsGroundGame
    {
        /// <summary>W-D1's declared regional noise - σ in points of a party's share in a jurisdiction, `RegionalAggregation.RegionalNoiseSigmaPp`
        /// ([AUTHORED-DRAFT] there): the figure R-US16 (b) was sized on (US-9: the closest states flip in up to a quarter of counts, the presidency in none
        /// of 400) and the plan's words, "with its declared regional noise". DECLARED.</summary>
        public static double NoiseSigmaPp => ParliamentaryCount.SigmaPp;   // GATE-18: the one sigma in play, zero inside `ParliamentaryCount.NoiseAtZero` (a measurement that holds sigma 0 by design)

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

        /// <summary>What the count reads of a finished campaign: its jurisdictions by name, each one's electorate and votes cast of record (its audience), the
        /// two nominees' parties' opened doors and completed calls there (5b, the count since K18), their weighted ground contacts (W-B11's curve, the count
        /// US-31 registered its measurement on - `EffectsOnCurve`), and the frame's national base turnout.</summary>
        public sealed class Campaign
        {
            public string[] Regions;
            public double[] Eligible;
            public double[] Audience;
            public double[] DemocratDoors;
            public double[] RepublicanDoors;
            public double[] DemocratCalls;
            public double[] RepublicanCalls;
            public double[] DemocratContacts;
            public double[] RepublicanContacts;
            public double BaseTurnout;

            /// <summary>K18: a jurisdiction's own base turnout - its votes cast of record over its electorate (5b reads each region at its own, as
            /// `ParliamentaryCount` does); 0 where it has no electorate.</summary>
            public double BaseTurnoutAt(int r) => Eligible[r] > 0.0 && Audience != null ? Audience[r] / Eligible[r] : 0.0;

            /// <summary>An empty campaign on <paramref name="n"/> jurisdictions - every contact array allocated, none made.</summary>
            public static Campaign Empty(int n) => new Campaign
            {
                Regions = new string[n], Eligible = new double[n], Audience = new double[n], DemocratDoors = new double[n], RepublicanDoors = new double[n],
                DemocratCalls = new double[n], RepublicanCalls = new double[n], DemocratContacts = new double[n], RepublicanContacts = new double[n],
            };

            /// <summary>A deep copy - every array cloned.</summary>
            public Campaign Copy() => new Campaign
            {
                Regions = (string[])Regions.Clone(), Eligible = (double[])Eligible.Clone(), Audience = (double[])Audience.Clone(),
                DemocratDoors = (double[])DemocratDoors.Clone(), RepublicanDoors = (double[])RepublicanDoors.Clone(), DemocratCalls = (double[])DemocratCalls.Clone(),
                RepublicanCalls = (double[])RepublicanCalls.Clone(), DemocratContacts = (double[])DemocratContacts.Clone(), RepublicanContacts = (double[])RepublicanContacts.Clone(),
                BaseTurnout = BaseTurnout,
            };
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
            Campaign c = Campaign.Empty(n);
            double audience = 0.0, eligible = 0.0;
            for (int r = 0; r < n; r++)
            {
                c.Regions[r] = setup.Regions[r].Name;
                c.Eligible[r] = result.Gotv.Eligible(r);
                c.Audience[r] = setup.Regions[r].Audience;
                c.DemocratDoors[r] = result.Gotv.DoorContacts(r, dem);
                c.RepublicanDoors[r] = result.Gotv.DoorContacts(r, rep);
                c.DemocratCalls[r] = result.Gotv.PhoneContacts(r, dem);
                c.RepublicanCalls[r] = result.Gotv.PhoneContacts(r, rep);
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
        /// Elias's ruling of 2026-10-23 ("always, all countries"): THE COUNT WITH NO CAMPAIGN - the jurisdictions of <paramref name="pollingDay"/>'s
        /// count (`UsRegions.Regions`, the staging's own: each one's votes cast of record and its CVAP) with no contact anywhere, so a US polling day the
        /// player's campaign did not run up to is counted state by state with the same keyed regional noise and nothing else; the frame's base turnout is
        /// the staging's, votes cast over the electorate. Null, with the reason, where the jurisdictions cannot be read.
        /// </summary>
        public static Campaign NoCampaign(DateTime pollingDay, out string why)
        {
            RegionAudience[] regions = UsRegions.Regions(pollingDay, out double _, out int _, out why);
            if (regions == null || regions.Length == 0) { return null; }
            int n = regions.Length;
            Campaign c = Campaign.Empty(n);
            double audience = 0.0, eligible = 0.0;
            for (int r = 0; r < n; r++)
            {
                c.Regions[r] = regions[r].Name;
                c.Eligible[r] = regions[r].Eligible;
                c.Audience[r] = regions[r].Audience;
                audience += regions[r].Audience;
                eligible += regions[r].Eligible;
            }
            c.BaseTurnout = eligible > 0.0 ? audience / eligible : 0.0;
            return c;
        }

        /// <summary>
        /// THE STATE EFFECT of <paramref name="campaign"/>'s ground game on the derived states <paramref name="derived"/> - since K18 (Elias's ruling of
        /// 2026-10-23, GATE-18 piece one's 5b): 5b's count of every jurisdiction the campaign staged (`ElectionDay.Count`'s count in play, as
        /// `ParliamentaryCount` counts the European chambers) - its preference the derived shares, each jurisdiction at its own base turnout (its votes cast
        /// of record over its electorate, `Campaign.BaseTurnoutAt`), each nominee's supporters at that base plus the door CACE on the share of
        /// them its opened doors reached and the phone CACE on the share its completed calls reached (`GotvTurnout.RegionVotes`); the door CACE Gerber &amp;
        /// Green's band at the jurisdiction's own base (`GotvTurnout.DoorCacePp`, K7: "the US keeps GG14") - the noise of <paramref name="sigmaPp"/> drawn on
        /// <paramref name="noiseSeed"/>, against the derived shares. Pure. Null, with the reason, where a derived state has no staged jurisdiction, a
        /// jurisdiction has no base turnout, or a reached supporter's turnout would reach one (the clamp, which 5b never binds at a US base).
        /// </summary>
        public static List<StateEffect> Effects(ElectoralCollege.StateCall[] derived, Campaign campaign, int noiseSeed, double sigmaPp, out string why)
        {
            if (!Frame(derived, campaign, out string[] names, out double[][] preference, out why)) { return null; }
            int n = names.Length;
            var gotv = new RegionalMobilization((double[])campaign.Eligible.Clone(), 3);
            var baseTurnout = new double[n];
            var cace = new double[n];
            for (int r = 0; r < n; r++)
            {
                gotv.CarryDoors(r, ElectoralCollege.ShareDemocrat, campaign.DemocratDoors[r]);
                gotv.CarryDoors(r, ElectoralCollege.ShareRepublican, campaign.RepublicanDoors[r]);
                gotv.CarryCalls(r, ElectoralCollege.ShareDemocrat, campaign.DemocratCalls[r]);
                gotv.CarryCalls(r, ElectoralCollege.ShareRepublican, campaign.RepublicanCalls[r]);
                baseTurnout[r] = campaign.BaseTurnoutAt(r);
                cace[r] = GotvTurnout.DoorCacePp(CountryId.USA, baseTurnout[r]);
                if (baseTurnout[r] <= 0.0) { why = names[r] + " has no base turnout (no votes cast of record or no electorate)"; return null; }
                if (baseTurnout[r] + (GotvTurnout.DoorCaceUpperPp(CountryId.USA, baseTurnout[r]) + GotvTurnout.PhoneCaceUpperPp) / 100.0 >= 1.0) { why = names[r] + ": a reached supporter's turnout would reach one"; return null; }
            }
            ElectionDay.Result count = ElectionDay.Count(names, preference, gotv, baseTurnout, cace, new System.Random(noiseSeed), sigmaPp);   // 5b's count in play, the scaled noise (K34)
            return EffectsOf(names, preference, count, out why);
        }

        /// <summary>
        /// THE REGISTERED FUNCTION (K18 keeps it, for US-31's PLAN_PREREG alone): the state effect as the US count made it from US-31 to K18 - W-D1's count
        /// over W-B11's mobilisation curve (the two nominees' weighted contacts, one national base turnout that cancels from every share unless a rate clamps,
        /// the other attributes neutral). US-31 registered "US-9's measured amount" on this function, so its measurement is held on it unchanged while the
        /// count reads 5b (`Effects`). Called by no count. Pure; null, with the reason, as `Effects`, or where the frame would clamp.
        /// </summary>
        public static List<StateEffect> EffectsOnCurve(ElectoralCollege.StateCall[] derived, Campaign campaign, int noiseSeed, double sigmaPp, out string why)
        {
            if (!Frame(derived, campaign, out string[] names, out double[][] preference, out why)) { return null; }
            int n = names.Length;
            var gotv = new RegionalMobilization((double[])campaign.Eligible.Clone(), 3);
            for (int r = 0; r < n; r++)
            {
                gotv.Carry(r, ElectoralCollege.ShareDemocrat, campaign.DemocratContacts[r]);
                gotv.Carry(r, ElectoralCollege.ShareRepublican, campaign.RepublicanContacts[r]);
            }
            double most = 1.0;
            for (int r = 0; r < n; r++) { for (int p = 0; p < 2; p++) { most = Math.Max(most, TurnoutModel.Multiplier(gotv.Mobilization(r, p), TurnoutModel.MobilizationSpan)); } }
            if (campaign.BaseTurnout <= 0.0 || campaign.BaseTurnout * most >= 1.0) { why = "the frame would clamp a turnout rate"; return null; }
            ElectionDay.Result count = ElectionDay.Count(names, preference, gotv, campaign.BaseTurnout, Neutral, Neutral, Neutral, new System.Random(noiseSeed), sigmaPp, scaledNoise: true);
            return EffectsOf(names, preference, count, out why);
        }

        /// <summary>Each staged jurisdiction's name and its derived shares as the count's preference; false, with the reason, where they do not match.</summary>
        private static bool Frame(ElectoralCollege.StateCall[] derived, Campaign campaign, out string[] names, out double[][] preference, out string why)
        {
            why = null;
            names = null;
            preference = null;
            if (derived == null || campaign?.Regions == null) { why = "no derived states or no campaign to count"; return false; }
            int n = campaign.Regions.Length;
            var at = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < derived.Length; i++) { at[derived[i].State] = i; }
            names = new string[n];
            preference = new double[n][];
            for (int r = 0; r < n; r++)
            {
                if (!at.TryGetValue(campaign.Regions[r], out int s)) { why = campaign.Regions[r] + " is staged and has no derived state"; return false; }
                names[r] = campaign.Regions[r];
                preference[r] = (double[])derived[s].Shares.Clone();
            }
            if (at.Count != n) { why = "the derived states and the staged jurisdictions differ in number"; return false; }
            return true;
        }

        /// <summary>The counted shares less the derived, jurisdiction by jurisdiction.</summary>
        private static List<StateEffect> EffectsOf(string[] names, double[][] preference, ElectionDay.Result count, out string why)
        {
            why = null;
            int n = names.Length;
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
            return string.Format(CultureInfo.InvariantCulture, "each state counted with the campaign ground game (none where no campaign ran) and the regional noise (sigma {0:0.0} points at a share of one half, scaled by the square root of s(1-s)): the widest move of a margin {1} {2:+0.00;-0.00} points; calls changed from the derived states: {3}",
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
