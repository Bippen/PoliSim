using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-B8 / SPEC §17 — scandals as DYNAMIC events with a lifecycle: they break, the party
    /// responds, the story decays or escalates, and the damage lands on two different stocks at
    /// two different speeds. PURE FUNCTIONS, WIRED TO NOTHING (R-N2).
    ///
    /// **What a scandal does, and what it cannot.** A <see cref="ScandalOutcome"/> carries a
    /// coverage shock per day of the story (§13, `MediaCoverage.AddShock`), a momentum shock
    /// (§22, the short-term polling decline), and a CREDIBILITY cost — the lasting damage, on the
    /// stock §42's chain multiplies by and §38 calls reputation. It has no share, no preference
    /// and no party member (the W-B3 bar, asserted by reflection); a scandal moves what the press
    /// says, where the race appears to be, and how much the party is believed — and the vote
    /// follows from the last of those through the same chain as everything else.
    ///
    /// **Nothing is scripted as game over.** No response ends the campaign — `Resign` replaces the
    /// candidate and the party campaigns on with a new face (the outcome says so, the caller acts
    /// on it); the worst a catastrophic scandal does is a large credibility cost that the chain
    /// then prices. There is no outcome field that could end anything, and the harness asserts
    /// there is not.
    ///
    /// **The lifecycle, per §17:**
    /// 1. The scandal BREAKS with an evidence strength (0–1) the party can only estimate
    ///    (`EvidenceAsSeen` — the true value plus the party's own uncertainty, §36).
    /// 2. The party RESPONDS once, from §17's seven. Each response is a row of the
    ///    `[AUTHORED-DRAFT]` table below: its immediate momentum cost, its lasting credibility cost,
    ///    how long it keeps the story in the news, and — for `Deny` and `AttackSource` — its exposure
    ///    to the evidence.
    /// 3. The AFTERMATH runs for the story's days: each day the evidence may SURFACE (one seeded
    ///    draw at `evidence × SurfaceRatePerDay`), and if it does, a denial or an attack on the
    ///    source ESCALATES — the cost multiplies and the story restarts; an apology or an
    ///    explanation given before the evidence surfaced is protected from it.
    /// 4. The outcome sums the days: coverage per day (so the media system decays it on its own
    ///    half-life), one momentum shock, one lasting credibility cost.
    ///
    /// §17's own two sentences are the design: *a transparent apology may reduce long-term
    /// damage but cause a short-term polling decline* (Apologize: the largest momentum cost, the
    /// smallest lasting cost, the shortest story); *a denial can work if evidence is weak but
    /// become catastrophic if evidence later appears* (Deny: the smallest immediate cost, the
    /// largest exposure). Every response therefore has a DIFFERENT outcome distribution over
    /// seeds, and the harness measures them.
    /// </summary>
    public enum ScandalKind
    {
        FinancialMisconduct = 0,
        Corruption = 1,
        PersonalControversy = 2,
        OffensiveStatement = 3,
        OldSocialMediaPost = 4,
        PolicyContradiction = 5,
        InternalPartyDispute = 6,
        CampaignFinanceViolation = 7,
    }

    public enum ScandalSeverity
    {
        Minor = 0,
        Moderate = 1,
        Major = 2,
        Catastrophic = 3,
    }

    public enum ScandalResponse
    {
        Deny = 0,
        Apologize = 1,
        Explain = 2,
        AttackSource = 3,
        Ignore = 4,
        Resign = 5,
        SacrificeStaffMember = 6,
    }

    /// <summary>A scandal as it breaks: what, how bad, and how strong the evidence really is (which the party only estimates).</summary>
    public readonly struct Scandal
    {
        public readonly ScandalKind Kind;
        public readonly ScandalSeverity Severity;
        /// <summary>The TRUE evidence strength, 0–1. Never handed to a decision-maker directly — see <see cref="Scandals.EvidenceAsSeen"/>.</summary>
        public readonly double Evidence;

        public Scandal(ScandalKind kind, ScandalSeverity severity, double evidence)
        {
            Kind = kind; Severity = severity; Evidence = evidence < 0 ? 0 : (evidence > 1 ? 1 : evidence);
        }

        /// <summary>§17's kinds that a staff member can plausibly carry — the ones `SacrificeStaffMember` answers.</summary>
        public bool StaffCanCarry => Kind == ScandalKind.FinancialMisconduct || Kind == ScandalKind.CampaignFinanceViolation || Kind == ScandalKind.InternalPartyDispute;
    }

    /// <summary>
    /// What a scandal did. **Deliberately contains no share, no preference, no party member and
    /// nothing that could end a campaign** — the ceiling of what a scandal may do is a coverage
    /// shock per day, a momentum shock, a credibility cost and a change of candidate.
    /// </summary>
    public readonly struct ScandalOutcome
    {
        public readonly ScandalResponse Response;
        /// <summary>Raw newsworthiness per day of the story, in order — the caller feeds each to `MediaCoverage.AddShock` on its day.</summary>
        public readonly double[] CoverageShockPerDay;
        /// <summary>The short-term polling decline, in percentage points of §22 momentum (negative).</summary>
        public readonly double MomentumShockPp;
        /// <summary>The lasting damage: the fraction of the party's credibility lost (0–1).</summary>
        public readonly double CredibilityCost;
        /// <summary>The evidence surfaced during the aftermath and the response could not survive it.</summary>
        public readonly bool Escalated;
        /// <summary>The candidate goes; the caller replaces them. The campaign continues.</summary>
        public readonly bool CandidateReplaced;
        /// <summary>A staff member went instead of the candidate.</summary>
        public readonly bool StaffMemberSacrificed;

        public ScandalOutcome(ScandalResponse response, double[] coverageShockPerDay, double momentumShockPp, double credibilityCost,
            bool escalated, bool candidateReplaced, bool staffMemberSacrificed)
        {
            Response = response; CoverageShockPerDay = coverageShockPerDay; MomentumShockPp = momentumShockPp; CredibilityCost = credibilityCost;
            Escalated = escalated; CandidateReplaced = candidateReplaced; StaffMemberSacrificed = staffMemberSacrificed;
        }

        public int DaysInTheNews => CoverageShockPerDay.Length;

        /// <summary>One number for comparing responses: lasting cost in credibility points (of 100) plus the momentum decline, both positive as damage.</summary>
        public double TotalDamage => 100.0 * CredibilityCost - MomentumShockPp;
    }

    public static class Scandals
    {
        public static readonly ScandalResponse[] TheSeven =
        {
            ScandalResponse.Deny, ScandalResponse.Apologize, ScandalResponse.Explain, ScandalResponse.AttackSource,
            ScandalResponse.Ignore, ScandalResponse.Resign, ScandalResponse.SacrificeStaffMember,
        };

        /// <summary>[AUTHORED-DRAFT] the base damage of a scandal by severity: momentum decline in pp, lasting credibility cost as a fraction, days in the news, and the raw coverage it makes on its first day.</summary>
        public static void Base(ScandalSeverity severity, out double momentumPp, out double credibility, out int days, out double coverage)
        {
            switch (severity)
            {
                case ScandalSeverity.Minor: momentumPp = 0.5; credibility = 0.02; days = 2; coverage = 0.15; break;
                case ScandalSeverity.Moderate: momentumPp = 1.5; credibility = 0.06; days = 4; coverage = 0.40; break;
                case ScandalSeverity.Major: momentumPp = 3.0; credibility = 0.12; days = 7; coverage = 0.80; break;
                default: momentumPp = 5.0; credibility = 0.25; days = 12; coverage = 1.50; break;
            }
        }

        /// <summary>[AUTHORED-DRAFT] the probability per aftermath day that the evidence surfaces, per unit of evidence strength.</summary>
        public const double SurfaceRatePerDay = 0.15;

        /// <summary>[AUTHORED-DRAFT] the width of the party's own estimate of the evidence (a uniform error either side of the truth).</summary>
        public const double EvidenceEstimateError = 0.25;

        /// <summary>
        /// [AUTHORED-DRAFT] the response table — §17's two sentences as rows, and the rest to
        /// match. Multipliers on the base momentum cost, the base credibility cost and the base
        /// days in the news; `exposed` marks the responses the evidence can catch out, and
        /// `escalation` what catching out multiplies the credibility cost by.
        /// </summary>
        public static void Table(ScandalResponse response, out double momentum, out double credibility, out double days,
            out bool exposed, out double escalation, out double coverageMultiplier)
        {
            switch (response)
            {
                case ScandalResponse.Deny: momentum = 0.3; credibility = 0.3; days = 1.0; exposed = true; escalation = 6.0; coverageMultiplier = 1.0; break;   // caught out, a denial is the worst outcome on the table - section 17's "catastrophic"
                case ScandalResponse.Apologize: momentum = 1.3; credibility = 0.5; days = 0.6; exposed = false; escalation = 1.0; coverageMultiplier = 0.8; break;
                case ScandalResponse.Explain: momentum = 0.8; credibility = 0.7; days = 0.9; exposed = false; escalation = 1.0; coverageMultiplier = 0.9; break;
                case ScandalResponse.AttackSource: momentum = 0.5; credibility = 0.6; days = 1.3; exposed = true; escalation = 2.0; coverageMultiplier = 1.5; break;
                case ScandalResponse.Ignore: momentum = 0.8; credibility = 0.9; days = 1.5; exposed = false; escalation = 1.0; coverageMultiplier = 1.1; break;
                case ScandalResponse.Resign: momentum = 1.6; credibility = 0.2; days = 0.7; exposed = false; escalation = 1.0; coverageMultiplier = 1.4; break;
                case ScandalResponse.SacrificeStaffMember: momentum = 0.7; credibility = 0.5; days = 0.8; exposed = false; escalation = 1.0; coverageMultiplier = 1.0; break;
                default: throw new ArgumentException($"{response} is not one of §17's seven responses");
            }
        }

        /// <summary>[AUTHORED-DRAFT] a staff sacrifice for a scandal no staff member could carry reads as cynical: the credibility cost is this multiple of the table's instead.</summary>
        public const double CynicalSacrificeMultiplier = 1.6;

        /// <summary>The evidence as the party sees it (§36): the truth plus a uniform error of ± <see cref="EvidenceEstimateError"/>, from the caller's random.</summary>
        public static double EvidenceAsSeen(Scandal scandal, System.Random random)
        {
            if (random == null) { throw new ArgumentNullException(nameof(random)); }
            double seen = scandal.Evidence + (random.NextDouble() * 2.0 - 1.0) * EvidenceEstimateError;
            return seen < 0 ? 0 : (seen > 1 ? 1 : seen);
        }

        /// <summary>
        /// Run the lifecycle: the response, then the aftermath day by day with the evidence's
        /// chance to surface. Deterministic under the caller's random — the harness passes
        /// `SimulationRandom`'s appended `Scandal` stream.
        /// </summary>
        public static ScandalOutcome Resolve(Scandal scandal, ScandalResponse response, System.Random random)
        {
            if (random == null) { throw new ArgumentNullException(nameof(random)); }

            Base(scandal.Severity, out double baseMomentum, out double baseCredibility, out int baseDays, out double baseCoverage);
            Table(response, out double m, out double c, out double d, out bool exposed, out double escalation, out double coverageMultiplier);

            bool staffSacrificed = response == ScandalResponse.SacrificeStaffMember;
            if (staffSacrificed && !scandal.StaffCanCarry) { c *= CynicalSacrificeMultiplier; }

            int days = Math.Max(1, (int)Math.Round(baseDays * d));
            var coverage = new List<double>();
            bool escalated = false;

            // Day 0: the story breaks and the response lands on the same news cycle.
            coverage.Add(baseCoverage * coverageMultiplier);

            // The aftermath: each day the evidence may surface. A response that is exposed to it
            // escalates: the cost multiplies and the story runs its base length again from here.
            for (int day = 1; day < days; day++)
            {
                double surfaces = scandal.Evidence * SurfaceRatePerDay;
                if (exposed && !escalated && random.NextDouble() < surfaces)
                {
                    escalated = true;
                    days = day + baseDays;   // the story restarts
                    coverage.Add(baseCoverage * escalation * 0.5);
                    continue;
                }

                coverage.Add(baseCoverage * coverageMultiplier * Math.Pow(0.6, day));   // the story's own fade inside the news cycle
            }

            double credibilityCost = baseCredibility * c * (escalated ? escalation : 1.0);
            double momentumPp = -baseMomentum * m * (escalated ? 1.5 : 1.0);
            if (credibilityCost > 1.0) { credibilityCost = 1.0; }

            return new ScandalOutcome(response, coverage.ToArray(), momentumPp, credibilityCost, escalated,
                response == ScandalResponse.Resign, staffSacrificed);
        }
    }
}
