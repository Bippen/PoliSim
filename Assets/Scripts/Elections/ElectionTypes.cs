using System;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// The elections system's DATA SHAPES — spec §41's recommended data model, and §4/§5/§6's
    /// dimensions, as plain C# value types. PURE DATA, WIRED TO NOTHING (R-N2).
    ///
    /// ⚠ **A deliberate, ruled deviation from spec §40.** The spec asks for ScriptableObjects.
    /// **R-EL1 (standing, Elias) rules that the PoliSim idiom wins: catalogs in code, not
    /// ScriptableObjects** — the same decision `FederalReserveSystem.CandidatePool` and
    /// `WorldFactory` already embody. The spec's field lists are followed closely; only the Unity
    /// container is different, and the gap table records this as §40's N/A with its reason.
    ///
    /// Scales follow the spec: every ideological axis, issue position, issue weight and attribute
    /// is **0–100**, and compatibility normalises to 0–100 (§7).
    /// </summary>
    public static class ElectionScales
    {
        public const double Min = 0.0;
        public const double Max = 100.0;

        public static double Clamp(double value) => value < Min ? Min : (value > Max ? Max : value);
    }

    /// <summary>Spec §4's ideological dimensions — "ideology should not simply be one left/right number". An axis set to <see cref="double.NaN"/> is UNDEFINED and is skipped by every comparison rather than treated as a centre value: until F5 (2026-09-02) the sourced CHES data defined three of these eight and the model refused to invent the other five; since F5 all eight are sourced for the CHES-positioned units (<see cref="IdeologyVector.FromParty"/>), and a unit without a published position stays NaN.</summary>
    public enum IdeologyAxis
    {
        EconomicLeftRight = 0,        // CHES lrecon (sourced)
        SocialLiberalConservative = 1, // CHES galtan (sourced)
        GlobalistNationalist = 2,      // CHES eu_position, rescaled (sourced)
        // F5 (2026-09-02): the five below are CHES 2024 columns (D-18 lifted - the codebook's endpoints are
        // QUOTED on PoliticalParty's fields). The name's FIRST word is the 0 end, as for the three above
        // (Economic LEFT = 0 ↔ lrecon 0 = left; Social LIBERAL = 0 ↔ galtan 0 = libertarian), so two of the
        // five run against their CHES column and are inverted in FromParty - stated there, per S-37.
        EnvironmentalIndustrial = 3,        // CHES environment (0 = protection … 10 = growth): same direction
        CentralizationDecentralization = 4, // CHES regions (0 = FAVORS decentralisation): INVERTED
        TaxHighLow = 5,                     // CHES spendvtax (0 = improving services … 10 = reducing taxes): same direction
        ImmigrationRestrictiveLiberal = 6,  // CHES immigrate_policy (0 = LIBERAL … 10 = restrictive): INVERTED
        PublicPrivate = 7,                  // CHES deregulation (0 = opposes … 10 = favors deregulation): same direction
    }

    /// <summary>Spec §6's issue list, the weights and positions both sides of a compatibility calculation are expressed in.</summary>
    public enum IssueId
    {
        Economy = 0,
        Healthcare = 1,
        Immigration = 2,
        Climate = 3,
        Crime = 4,
        Taxes = 5,
        Education = 6,
        Housing = 7,
        Defense = 8,
    }

    /// <summary>A position on each of §4's eight axes, 0–100; NaN = this axis is not defined for this actor.</summary>
    public readonly struct IdeologyVector
    {
        public const int AxisCount = 8;
        private readonly double[] _axes;

        public IdeologyVector(double[] axes)
        {
            if (axes == null || axes.Length != AxisCount)
            {
                throw new ArgumentException($"an ideology vector has exactly {AxisCount} axes (NaN where undefined)");
            }

            _axes = axes;
        }

        public double this[IdeologyAxis axis] => _axes[(int)axis];

        public bool IsDefined(IdeologyAxis axis) => !double.IsNaN(_axes[(int)axis]);

        /// <summary>The three axes the sourced CHES data actually fills, with the rest undefined — the honest constructor for a real party.</summary>
        public static IdeologyVector FromSourcedAxes(double economic, double social, double globalistNationalist)
        {
            var axes = new double[AxisCount];
            for (int i = 0; i < AxisCount; i++) { axes[i] = double.NaN; }
            axes[(int)IdeologyAxis.EconomicLeftRight] = economic;
            axes[(int)IdeologyAxis.SocialLiberalConservative] = social;
            axes[(int)IdeologyAxis.GlobalistNationalist] = globalistNationalist;
            return new IdeologyVector(axes);
        }

        /// <summary>
        /// F5 (2026-09-02): a real party's vector on ALL EIGHT axes from its sourced CHES/GPS positions,
        /// each rescaled to the spec's 0–100 and oriented so the axis name's first word is the 0 end.
        /// NaN stays NaN - an axis the survey did not publish for this unit stays undefined and
        /// <see cref="Compatibility.IdeologicalMatch"/> skips it, exactly as before. The quotations that
        /// fix each direction are on <see cref="PoliticalParty"/>'s fields; the two inversions are
        /// `regions` (CHES 0 = favors decentralisation, our 0 = centralisation) and `immigrate_policy`
        /// (CHES 0 = liberal, our 0 = restrictive). `eu_position` runs 1–7 with 7 = strongly in favour
        /// of integration, which is the GLOBALIST end, so it maps to (7 − eu) / 6 × 100 - the one
        /// direction here that is a reading of the axis name rather than a quotation, and it says so.
        /// </summary>
        public static IdeologyVector FromParty(PoliticalParty party)
        {
            var axes = new double[AxisCount];
            axes[(int)IdeologyAxis.EconomicLeftRight] = Tenfold(party.LrEcon);
            axes[(int)IdeologyAxis.SocialLiberalConservative] = Tenfold(party.Galtan);
            axes[(int)IdeologyAxis.GlobalistNationalist] = float.IsNaN(party.EuPosition) ? double.NaN : (7.0 - party.EuPosition) / 6.0 * 100.0;
            axes[(int)IdeologyAxis.EnvironmentalIndustrial] = Tenfold(party.Environment);
            axes[(int)IdeologyAxis.CentralizationDecentralization] = Inverted(party.Regions);
            axes[(int)IdeologyAxis.TaxHighLow] = Tenfold(party.SpendVsTax);
            axes[(int)IdeologyAxis.ImmigrationRestrictiveLiberal] = Inverted(party.ImmigratePolicy);
            axes[(int)IdeologyAxis.PublicPrivate] = Tenfold(party.Deregulation);
            return new IdeologyVector(axes);
        }

        private static double Tenfold(float ches0To10) => float.IsNaN(ches0To10) ? double.NaN : ches0To10 * 10.0;
        private static double Inverted(float ches0To10) => float.IsNaN(ches0To10) ? double.NaN : 100.0 - ches0To10 * 10.0;

        public static IdeologyVector Uniform(double value)
        {
            var axes = new double[AxisCount];
            for (int i = 0; i < AxisCount; i++) { axes[i] = value; }
            return new IdeologyVector(axes);
        }
    }

    /// <summary>A position (0–100) on each of §6's issues; NaN = the actor takes no position, and the issue is skipped rather than counted as neutral.</summary>
    public readonly struct IssueVector
    {
        public const int IssueCount = 9;
        private readonly double[] _values;

        public IssueVector(double[] values)
        {
            if (values == null || values.Length != IssueCount)
            {
                throw new ArgumentException($"an issue vector has exactly {IssueCount} entries (NaN where undefined)");
            }

            _values = values;
        }

        public double this[IssueId issue] => _values[(int)issue];

        public bool IsDefined(IssueId issue) => !double.IsNaN(_values[(int)issue]);

        public static IssueVector Uniform(double value)
        {
            var values = new double[IssueCount];
            for (int i = 0; i < IssueCount; i++) { values[i] = value; }
            return new IssueVector(values);
        }
    }

    /// <summary>Spec §41 PartyData + §4's characteristics, as far as the compatibility core needs them. `BaseSupport`, `Funding` and `Organization` are carried for the layers above (§39's base-support term, §9's resources) and are not read by §7.</summary>
    public readonly struct PartyProfile
    {
        public readonly string Name;
        public readonly IdeologyVector Ideology;
        public readonly IssueVector PolicyPositions;
        public readonly double BaseSupport;
        public readonly double Reputation;
        public readonly double LeaderAppeal;
        public readonly double CampaignEffectiveness;
        public readonly double Funding;
        public readonly double Organization;

        public PartyProfile(string name, IdeologyVector ideology, IssueVector policyPositions,
            double baseSupport = 0, double reputation = 50, double leaderAppeal = 50,
            double campaignEffectiveness = 50, double funding = 0, double organization = 50)
        {
            Name = name;
            Ideology = ideology;
            PolicyPositions = policyPositions;
            BaseSupport = baseSupport;
            Reputation = reputation;
            LeaderAppeal = leaderAppeal;
            CampaignEffectiveness = campaignEffectiveness;
            Funding = funding;
            Organization = organization;
        }
    }

    /// <summary>
    /// Spec §41 VoterGroupData + §5's per-group fields. Two issue vectors, not one, and the
    /// distinction is load-bearing: <see cref="IssueWeights"/> is how much the group CARES
    /// (§6's priorities, 0–100) and <see cref="IssuePositions"/> is what it WANTS (the stance a
    /// party is close to or far from). The spec states the first explicitly and requires the
    /// second implicitly — "how closely its policies match those priorities" is undefined without
    /// a stance — so the second is added here and flagged as an addition rather than smuggled in.
    /// </summary>
    public readonly struct VoterGroupProfile
    {
        public readonly string Name;
        public readonly double PopulationShare;
        public readonly double TurnoutBase;
        public readonly IssueVector IssueWeights;
        public readonly IssueVector IssuePositions;
        public readonly IdeologyVector Ideology;
        public readonly double PartyLoyalty;
        public readonly double CampaignResponsiveness;
        public readonly double PoliticalEngagement;

        public VoterGroupProfile(string name, double populationShare, double turnoutBase,
            IssueVector issueWeights, IssueVector issuePositions, IdeologyVector ideology,
            double partyLoyalty = 50, double campaignResponsiveness = 50, double politicalEngagement = 50)
        {
            Name = name;
            PopulationShare = populationShare;
            TurnoutBase = turnoutBase;
            IssueWeights = issueWeights;
            IssuePositions = issuePositions;
            Ideology = ideology;
            PartyLoyalty = partyLoyalty;
            CampaignResponsiveness = campaignResponsiveness;
            PoliticalEngagement = politicalEngagement;
        }
    }

    /// <summary>Spec §41 RegionData, as far as §27's election-day aggregation needs it. `GroupShares` is this region's share of each voter group (index-aligned with the group table it is used against).</summary>
    public readonly struct RegionProfile
    {
        public readonly string Name;
        public readonly long Population;
        public readonly double EligibleShare;
        public readonly int Seats;
        public readonly double[] GroupShares;
        public readonly IssueVector IssuePriorities;

        public RegionProfile(string name, long population, double eligibleShare, int seats,
            double[] groupShares, IssueVector issuePriorities)
        {
            Name = name;
            Population = population;
            EligibleShare = eligibleShare;
            Seats = seats;
            GroupShares = groupShares;
            IssuePriorities = issuePriorities;
        }
    }

    /// <summary>Spec §41 CandidateData / §16's attributes. Carried for §15 and §39's candidate-appeal term; §7 reads only what a party's LeaderAppeal summarises.</summary>
    public readonly struct CandidateProfile
    {
        public readonly string Name;
        public readonly double Charisma;
        public readonly double DebateSkill;
        public readonly double Communication;
        public readonly double Credibility;
        public readonly double Integrity;
        public readonly double PolicyKnowledge;
        public readonly double CampaignSkill;
        public readonly double Popularity;
        public readonly double ScandalResistance;

        public CandidateProfile(string name, double charisma, double debateSkill, double communication,
            double credibility, double integrity, double policyKnowledge, double campaignSkill,
            double popularity, double scandalResistance)
        {
            Name = name;
            Charisma = charisma;
            DebateSkill = debateSkill;
            Communication = communication;
            Credibility = credibility;
            Integrity = integrity;
            PolicyKnowledge = policyKnowledge;
            CampaignSkill = campaignSkill;
            Popularity = popularity;
            ScandalResistance = scandalResistance;
        }
    }
}
