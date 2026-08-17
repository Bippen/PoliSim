using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Political Systems Overhaul Part A (Cabinet) - Master Sequence step 1. Two independent
    /// mechanics, both scoped to whichever country the UI lets the player manage (see Country.
    /// CabinetMinisters' own doc comment for why no NPC/player branching is needed anywhere in this
    /// class):
    ///
    /// 1. PASSIVE competence effect: each appointed minister's CompetenceBias lands on their
    ///    portfolio's existing, already-audited channel - see GetCompetenceBias and its four call
    ///    sites (SimulationManager.ApplyRevenueAndSpending for FinanceTreasury, MacroSystem.
    ///    ApplyCrimeIndex for InteriorJustice, MacroSystem.ApplyPovertyRate for HealthSocialAffairs,
    ///    MacroSystem.ApplyYouthUnemployment for Education - R4-4, ruling R3).
    ///    Applied at point-of-use every turn, never mutating a stored structural field - the same
    ///    idiom FederalReserveSystem.ApplyFedChairInterestRate already established for FedChair.
    ///    RateBias, chosen for FinanceTreasury specifically per the Master Roadmap's own "may be safer
    ///    to land somewhere more contained" guidance (CollectionEfficiency has no reversion mechanism
    ///    of its own to correct a permanently-mutated value).
    ///    ⚠ R4-4's OTHER TWO portfolios are DECISIONS-ONLY by ruling R3: Defense has no audited
    ///    channel to land on (no military state; the G-term is fiscal-engine contact the F1
    ///    sequencing keeps closed), and ForeignAffairs' candidates would need rule-11 ceiling
    ///    audits this batch doesn't otherwise require. Their CompetenceBias values are authored
    ///    but INERT - GetCompetenceBias is simply never called with those portfolios - recorded
    ///    at the pool entries below and in CabinetPortfolio's own doc, so the asymmetry reads as
    ///    the ruling it is rather than an omission.
    ///
    /// 2. INTERACTIVE decisions: reuses EventSystem's overall shape (a rolled-per-turn pool draw) but,
    ///    unlike EconomicEvent's auto-applied shock, a fired CabinetDecision needs a player-picked
    ///    response before its effect lands - see SimulationManager.TryRollCabinetDecisions (called
    ///    once per turn, mirrors EventSystem.TryRollEvent's own call site) and ApplyDecisionOption
    ///    (called from GameController once the player picks a response, mirrors
    ///    DrawFedChairCandidateButton's own "apply immediately on pick" idiom).
    ///
    /// Every CabinetMinister here (CandidatePool) is an ORIGINAL FICTIONAL character - never a real
    /// person (Master Roadmap working-discipline rule 9; the R4-4 nine were additionally signed by
    /// Elias with the collision search recorded in POLISIM_R4_4_PREREPORT.md section 4). Six of the
    /// six confirmed-scope portfolios are implemented since R4-4 - see CabinetPortfolio's own doc
    /// comment for the history and the one remaining convention (Economy/Trade &amp; Industry stays
    /// undefined until built).
    /// </summary>
    public static class CabinetSystem
    {
        /// <summary>Chance PER APPOINTED MINISTER per turn that a decision fires - matches EventSystem.EventChancePerTurn, the same "~12%/period baseline" the Master Roadmap's own Part A spec calls for.</summary>
        private const float DecisionChancePerTurn = 0.12f;

        /// <summary>Modest one-time ApprovalRating cost for reshuffling a minister anytime - same small magnitude class as EventSystem's own ApprovalEffect range (-2 to -5), not a separately invented scale.</summary>
        public const float ReshuffleApprovalCost = 2f;

        private static System.Random RandomSource => SimulationRandom.For(SimulationRandom.Stream.Cabinet);

        private static readonly Dictionary<CabinetPortfolio, List<CabinetMinister>> CandidatePool = new Dictionary<CabinetPortfolio, List<CabinetMinister>>
        {
            {
                CabinetPortfolio.FinanceTreasury, new List<CabinetMinister>
                {
                    new CabinetMinister(
                        "Elena Voskresenskaya", CabinetPortfolio.FinanceTreasury, CabinetMinisterPhilosophy.Reformist,
                        "Wants to modernize tax administration wholesale - digital reporting, real-time enforcement, closing loopholes she considers relics of a paper-ledger era.",
                        0.020f),
                    new CabinetMinister(
                        "Marcus Ferreira", CabinetPortfolio.FinanceTreasury, CabinetMinisterPhilosophy.Pragmatic,
                        "A career budget technocrat who prizes predictable, boring competence over any grand fiscal vision.",
                        0.028f),
                    new CabinetMinister(
                        "Harold Whitmore", CabinetPortfolio.FinanceTreasury, CabinetMinisterPhilosophy.Traditionalist,
                        "Believes the Treasury's job is to collect what's owed under existing law, nothing more ambitious, and distrusts sweeping reform of any system that already works.",
                        0.015f),
                }
            },
            {
                CabinetPortfolio.InteriorJustice, new List<CabinetMinister>
                {
                    new CabinetMinister(
                        "Amara Osei-Bonsu", CabinetPortfolio.InteriorJustice, CabinetMinisterPhilosophy.Reformist,
                        "Argues that rehabilitation and root-cause investment do more for public safety long-term than another enforcement crackdown.",
                        1.5f),
                    new CabinetMinister(
                        "Jonas Lindqvist", CabinetPortfolio.InteriorJustice, CabinetMinisterPhilosophy.Pragmatic,
                        "Weighs enforcement and reform case-by-case, wary of either camp's ideological certainty.",
                        2.5f),
                    new CabinetMinister(
                        "Bruno Castellano", CabinetPortfolio.InteriorJustice, CabinetMinisterPhilosophy.Traditionalist,
                        "A former prosecutor who believes visible, consistent enforcement is what actually keeps neighborhoods safe.",
                        2.0f),
                }
            },
            {
                CabinetPortfolio.HealthSocialAffairs, new List<CabinetMinister>
                {
                    new CabinetMinister(
                        "Ingrid Solberg", CabinetPortfolio.HealthSocialAffairs, CabinetMinisterPhilosophy.Reformist,
                        "Wants to expand the social safety net significantly, pointing to peer countries with far lower hardship rates.",
                        1.2f),
                    new CabinetMinister(
                        "Wei-Lin Tanaka", CabinetPortfolio.HealthSocialAffairs, CabinetMinisterPhilosophy.Pragmatic,
                        "Supports the existing safety net but insists every expansion be paid for, not financed by hope.",
                        0.8f),
                    new CabinetMinister(
                        "Otto Baumgartner", CabinetPortfolio.HealthSocialAffairs, CabinetMinisterPhilosophy.Traditionalist,
                        "Favors targeted, family-centered support over broad new entitlements, wary of dependency.",
                        0.6f),
                }
            },
            // --- ROUND 4 BATCH R4-4: the three remaining portfolios, names signed per ruling R1 ---
            // ⚠ Defense and ForeignAffairs CompetenceBias values are AUTHORED BUT INERT this pass
            // (ruling R3 - no call site reads them; see the class header). They exist so a future
            // channel ruling starts from authored, comparable values rather than zeros, and their
            // magnitudes deliberately sit in the small per-point class the eventual channel would
            // plausibly use. Education's are LIVE: youth-unemployment points off the reversion
            // target - see MacroSystem.ApplyYouthUnemployment.
            {
                CabinetPortfolio.Defense, new List<CabinetMinister>
                {
                    new CabinetMinister(
                        "Katarzyna Ekelund", CabinetPortfolio.Defense, CabinetMinisterPhilosophy.Reformist,
                        "Wants procurement audited in the open - believes opaque contracting, not budget size, is where readiness actually dies.",
                        1.0f),
                    new CabinetMinister(
                        "Rafael Iwasaki", CabinetPortfolio.Defense, CabinetMinisterPhilosophy.Pragmatic,
                        "A capability-planning technocrat who buys what the threat assessment says, not what the parade needs.",
                        1.4f),
                    new CabinetMinister(
                        "Gunnar Petrakis", CabinetPortfolio.Defense, CabinetMinisterPhilosophy.Traditionalist,
                        "Deterrence through visible strength - distrusts any reform that could read as weakness abroad.",
                        1.2f),
                }
            },
            {
                CabinetPortfolio.ForeignAffairs, new List<CabinetMinister>
                {
                    new CabinetMinister(
                        "Camille Adeyemi", CabinetPortfolio.ForeignAffairs, CabinetMinisterPhilosophy.Reformist,
                        "An institution-builder who thinks the multilateral table is where middle powers actually win.",
                        1.1f),
                    new CabinetMinister(
                        "Zofia Nakamura", CabinetPortfolio.ForeignAffairs, CabinetMinisterPhilosophy.Pragmatic,
                        "An interests-first dealmaker who judges every communiqué by what it moves, not what it says.",
                        1.5f),
                    new CabinetMinister(
                        "Aleksander Whitfield", CabinetPortfolio.ForeignAffairs, CabinetMinisterPhilosophy.Traditionalist,
                        "Alliances and protocol - believes predictability is a foreign policy, and a good one.",
                        1.0f),
                }
            },
            {
                CabinetPortfolio.Education, new List<CabinetMinister>
                {
                    new CabinetMinister(
                        "Yuki Dahlberg", CabinetPortfolio.Education, CabinetMinisterPhilosophy.Reformist,
                        "A curriculum modernizer who argues the system trains students for an economy that no longer exists.",
                        1.2f),
                    new CabinetMinister(
                        "Nadia Fitzgerald", CabinetPortfolio.Education, CabinetMinisterPhilosophy.Pragmatic,
                        "An evidence-based incrementalist: pilots before mandates, data before both.",
                        0.9f),
                    new CabinetMinister(
                        "Tobias Marchetti", CabinetPortfolio.Education, CabinetMinisterPhilosophy.Traditionalist,
                        "Standards and fundamentals - wary of every reform that trades rigor for relevance.",
                        0.7f),
                }
            },
        };

        // Dictionary<TKey,TValue>'s "{ key, value }" collection-initializer shorthand doesn't parse
        // reliably when TKey is itself a tuple - index-initializer syntax ("[key] = value") sidesteps
        // the ambiguity entirely and is equally valid for building a static readonly dictionary.
        private static readonly Dictionary<(CabinetPortfolio, CabinetMinisterPhilosophy), List<CabinetDecision>> DecisionPool =
            new Dictionary<(CabinetPortfolio, CabinetMinisterPhilosophy), List<CabinetDecision>>
        {
            [(CabinetPortfolio.FinanceTreasury, CabinetMinisterPhilosophy.Reformist)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Digital Tax Enforcement Pilot",
                    "Treasury analysts have identified a wave of unreported income moving through online marketplaces. A mandatory digital-reporting pilot could close the gap - but merchants warn of real compliance costs.",
                    new CabinetDecisionOption("Launch the pilot", budgetImpact: 180f, approvalEffect: -1f),
                    new CabinetDecisionOption("Delay for further study", approvalEffect: 0.5f)),
                new CabinetDecision(
                    "Loophole Closure Package",
                    "Your Finance Minister has drafted a package closing several long-standing corporate deduction loopholes. Business groups are already lobbying against it.",
                    new CabinetDecisionOption("Push it through", budgetImpact: 220f, approvalEffect: -1.5f),
                    new CabinetDecisionOption("Water it down first", budgetImpact: 90f)),
            },
            [(CabinetPortfolio.FinanceTreasury, CabinetMinisterPhilosophy.Pragmatic)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Audit Staffing Shortfall",
                    "The tax authority is understaffed for the coming filing season, risking a real drop in collections. Emergency contract hires would help, but cost money now.",
                    new CabinetDecisionOption("Approve emergency hiring", budgetImpact: -60f),
                    new CabinetDecisionOption("Have staff cover the gap", budgetImpact: -150f)),
                new CabinetDecision(
                    "Unexpected Revenue Windfall",
                    "A one-time settlement with a major delinquent taxpayer has landed in Treasury's lap. Your minister wants direction on where it goes.",
                    new CabinetDecisionOption("Bank it against the debt", budgetImpact: 200f),
                    new CabinetDecisionOption("Announce it publicly as a win", budgetImpact: 150f, approvalEffect: 1f)),
            },
            [(CabinetPortfolio.FinanceTreasury, CabinetMinisterPhilosophy.Traditionalist)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Routine Compliance Sweep",
                    "A standard, unglamorous compliance sweep of existing filings has turned up more back-taxes owed than usual this cycle.",
                    new CabinetDecisionOption("Collect in full", budgetImpact: 140f, approvalEffect: -0.5f),
                    new CabinetDecisionOption("Offer a payment-plan amnesty", budgetImpact: 70f, approvalEffect: 0.5f)),
                new CabinetDecision(
                    "Legacy System Failure Risk",
                    "Your minister warns the Treasury's aging filing infrastructure is one bad quarter from real failures. He wants a modest maintenance budget, not a rebuild.",
                    new CabinetDecisionOption("Fund the maintenance", budgetImpact: -80f),
                    new CabinetDecisionOption("Push it another year", budgetImpact: 0f, approvalEffect: -0.5f)),
            },
            [(CabinetPortfolio.InteriorJustice, CabinetMinisterPhilosophy.Reformist)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Diversion Program Expansion",
                    "Your Interior Minister proposes diverting more low-level offenders into treatment and community programs instead of prosecution.",
                    new CabinetDecisionOption("Expand the program", crimeIndexShock: -1.5f, approvalEffect: -0.5f),
                    new CabinetDecisionOption("Keep it small-scale", crimeIndexShock: -0.5f)),
                new CabinetDecision(
                    "Community Trust Initiative",
                    "A pilot program pairing officers with neighborhood outreach workers shows early promise in a handful of districts.",
                    new CabinetDecisionOption("Fund it nationwide", crimeIndexShock: -1f, budgetImpact: -70f),
                    new CabinetDecisionOption("Let the pilot run longer", crimeIndexShock: -0.3f)),
            },
            [(CabinetPortfolio.InteriorJustice, CabinetMinisterPhilosophy.Pragmatic)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Cross-Agency Case Backlog",
                    "A backlog of unresolved cases is straining relations between police and prosecutors. Your minister wants a targeted surge team.",
                    new CabinetDecisionOption("Approve the surge team", crimeIndexShock: -1.2f, budgetImpact: -50f),
                    new CabinetDecisionOption("Let agencies sort it out", approvalEffect: -1f)),
                new CabinetDecision(
                    "High-Profile Case Outcome",
                    "A closely-watched prosecution just concluded. Public reaction is split on whether the outcome was fair.",
                    new CabinetDecisionOption("Publicly back the verdict", approvalEffect: 1f),
                    new CabinetDecisionOption("Stay neutral", approvalEffect: 0f)),
            },
            [(CabinetPortfolio.InteriorJustice, CabinetMinisterPhilosophy.Traditionalist)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Visible Patrol Surge",
                    "Your Interior Minister wants a highly visible patrol surge in high-crime districts ahead of the next reporting period.",
                    new CabinetDecisionOption("Authorize the surge", crimeIndexShock: -2f, budgetImpact: -90f, approvalEffect: 1f),
                    new CabinetDecisionOption("Hold current posture", crimeIndexShock: 0f)),
                new CabinetDecision(
                    "Repeat Offender Crackdown",
                    "A crackdown targeting repeat offenders is ready to launch, though civil-liberties groups are raising concerns.",
                    new CabinetDecisionOption("Launch the crackdown", crimeIndexShock: -1.5f, approvalEffect: -1f),
                    new CabinetDecisionOption("Scale it back", crimeIndexShock: -0.5f)),
            },
            [(CabinetPortfolio.HealthSocialAffairs, CabinetMinisterPhilosophy.Reformist)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Emergency Hardship Fund",
                    "Your Health & Social Affairs Minister wants to stand up a fast, no-paperwork emergency fund for families in acute hardship.",
                    new CabinetDecisionOption("Stand it up now", povertyRateShock: -0.8f, budgetImpact: -100f, approvalEffect: 1f),
                    new CabinetDecisionOption("Study it for next budget cycle", approvalEffect: -0.5f)),
                new CabinetDecision(
                    "Benefit Enrollment Drive",
                    "An outreach campaign to enroll eligible families in existing programs they aren't claiming is ready to launch.",
                    new CabinetDecisionOption("Fund the outreach drive", povertyRateShock: -0.5f, budgetImpact: -40f),
                    new CabinetDecisionOption("Skip it this cycle", povertyRateShock: 0f)),
            },
            [(CabinetPortfolio.HealthSocialAffairs, CabinetMinisterPhilosophy.Pragmatic)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Program Efficiency Review",
                    "A review of existing welfare-program overhead has found real savings without cutting a single benefit.",
                    new CabinetDecisionOption("Implement the savings", budgetImpact: 90f),
                    new CabinetDecisionOption("Leave it as-is for now", budgetImpact: 0f)),
                new CabinetDecision(
                    "Regional Hardship Spike",
                    "One region is reporting a sharp, localized rise in hardship cases tied to a local plant closure.",
                    new CabinetDecisionOption("Approve targeted relief", povertyRateShock: -0.6f, budgetImpact: -50f),
                    new CabinetDecisionOption("Route through normal channels", povertyRateShock: -0.2f)),
            },
            [(CabinetPortfolio.HealthSocialAffairs, CabinetMinisterPhilosophy.Traditionalist)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Family Support Pilot",
                    "Your minister proposes a modest, targeted support pilot for struggling families, deliberately narrower than a universal program.",
                    new CabinetDecisionOption("Approve the pilot", povertyRateShock: -0.5f, budgetImpact: -30f),
                    new CabinetDecisionOption("Keep current programs only", povertyRateShock: 0f)),
                new CabinetDecision(
                    "Benefit Fraud Investigation",
                    "An investigation into benefit-fraud claims has concluded, recovering misallocated funds without touching legitimate recipients.",
                    new CabinetDecisionOption("Recover the funds", budgetImpact: 100f),
                    new CabinetDecisionOption("Close it quietly", budgetImpact: 40f)),
            },
            // --- ROUND 4 BATCH R4-4: 9 pools x 2 scenarios, Part A parity (ruling R5). Every
            // magnitude sits inside the pre-report's authored envelope: |Approval| <= 2,
            // |Budget| <= 250, |YouthUnemployment| <= 1.5, |TradeBalance| <= 0.5. Defense writes
            // Budget/Approval only and NEVER SpendingCategory.Defense - spending lines stay the
            // player's lever (the ruled build directive). No scenario promises a legislative act:
            // bills are Part B's channel, decisions are executive.
            [(CabinetPortfolio.Defense, CabinetMinisterPhilosophy.Reformist)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Procurement Transparency Audit",
                    "Your Defense Minister wants to publish a full audit of the major procurement programs. Contractors are lobbying hard against it, and two service chiefs warn of awkward disclosures.",
                    new CabinetDecisionOption("Publish the audit", budgetImpact: 100f, approvalEffect: -1f),
                    new CabinetDecisionOption("Quiet internal review only", budgetImpact: 40f)),
                new CabinetDecision(
                    "Legacy Program Cancellation",
                    "A long-running weapons program keeps failing its tests and eating its contingency reserve. Cancelling it outright saves real money - and costs real jobs in three districts.",
                    new CabinetDecisionOption("Cancel it outright", budgetImpact: 180f, approvalEffect: -1.5f),
                    new CabinetDecisionOption("Restructure with penalties", budgetImpact: 80f)),
            },
            [(CabinetPortfolio.Defense, CabinetMinisterPhilosophy.Pragmatic)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Readiness Gap Report",
                    "An internal review found maintenance backlogs quietly degrading readiness across two branches. Funding the backlog now costs; deferring it risks the story leaking first.",
                    new CabinetDecisionOption("Fund the backlog now", budgetImpact: -90f),
                    new CabinetDecisionOption("Stagger it over the cycle", budgetImpact: -40f, approvalEffect: -0.5f)),
                new CabinetDecision(
                    "Joint Procurement Offer",
                    "A partner country proposes buying the next equipment tranche jointly, cutting unit costs - at the price of dropping the domestic bid.",
                    new CabinetDecisionOption("Join the program", budgetImpact: 60f, approvalEffect: -0.5f),
                    new CabinetDecisionOption("Buy domestic instead", budgetImpact: -80f, approvalEffect: 1f)),
            },
            [(CabinetPortfolio.Defense, CabinetMinisterPhilosophy.Traditionalist)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Show of Strength Exercise",
                    "Your Defense Minister wants a large national exercise this year - visible, expensive, and unambiguous.",
                    new CabinetDecisionOption("Authorize the full exercise", budgetImpact: -70f, approvalEffect: 1.5f),
                    new CabinetDecisionOption("Scale it down", budgetImpact: -25f)),
                new CabinetDecision(
                    "Veterans' Commemoration Package",
                    "A major service anniversary is coming. The minister proposes pairing the ceremony with a modest veterans' benefits package.",
                    new CabinetDecisionOption("Fund ceremony and package", budgetImpact: -50f, approvalEffect: 1.5f),
                    new CabinetDecisionOption("Ceremony only", budgetImpact: -10f, approvalEffect: 0.5f)),
            },
            [(CabinetPortfolio.ForeignAffairs, CabinetMinisterPhilosophy.Reformist)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Multilateral Climate Compact",
                    "A new multilateral compact is open for signature. Joining means binding commitments your exporters will feel; staying out means empty chairs at tables you want to sit at.",
                    new CabinetDecisionOption("Sign and commit", budgetImpact: -60f, approvalEffect: 1f, tradeBalanceShock: -0.2f),
                    new CabinetDecisionOption("Take observer status", approvalEffect: -0.5f)),
                new CabinetDecision(
                    "International Development Initiative",
                    "Your Foreign Minister proposes a flagship development fund - generosity with strings, since the procurement would run through domestic firms.",
                    new CabinetDecisionOption("Fund the initiative", budgetImpact: -80f, approvalEffect: 0.5f, tradeBalanceShock: 0.3f),
                    new CabinetDecisionOption("Co-fund with partners", budgetImpact: -30f, tradeBalanceShock: 0.1f)),
            },
            [(CabinetPortfolio.ForeignAffairs, CabinetMinisterPhilosophy.Pragmatic)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Export Market Access Deal",
                    "Months of quiet negotiation have produced market access for your producers - in exchange for concessions the opposition will call a giveaway.",
                    new CabinetDecisionOption("Close the deal", tradeBalanceShock: 0.4f, approvalEffect: -0.5f),
                    new CabinetDecisionOption("Hold out for better terms", approvalEffect: 0f)),
                new CabinetDecision(
                    "Consular Crisis Abroad",
                    "A regional crisis has stranded several hundred of your citizens. Chartering an evacuation is expensive; telling them to find commercial routes is cheaper and colder.",
                    new CabinetDecisionOption("Charter the evacuation", budgetImpact: -40f, approvalEffect: 1.5f),
                    new CabinetDecisionOption("Advise commercial routes", approvalEffect: -1.5f)),
            },
            [(CabinetPortfolio.ForeignAffairs, CabinetMinisterPhilosophy.Traditionalist)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Alliance Summit Hosting",
                    "It is your turn to host the annual alliance summit. Hosting in full is costly and photogenic; sending a delegation reads as retreat to those who keep score.",
                    new CabinetDecisionOption("Host in full", budgetImpact: -70f, approvalEffect: 1f),
                    new CabinetDecisionOption("Send the delegation instead", approvalEffect: -0.5f)),
                new CabinetDecision(
                    "Reciprocal Expulsions",
                    "A rival government has expelled two of your diplomats on a pretext. Protocol suggests reciprocating; commerce suggests not.",
                    new CabinetDecisionOption("Expel in kind", approvalEffect: 1f, tradeBalanceShock: -0.3f),
                    new CabinetDecisionOption("Protest formally, keep channels open", approvalEffect: -0.5f)),
            },
            [(CabinetPortfolio.Education, CabinetMinisterPhilosophy.Reformist)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Vocational Fast-Track Program",
                    "Your Education Minister has employer commitments for a modern apprenticeship track - training routed straight at the sectors that are actually hiring.",
                    new CabinetDecisionOption("Launch nationwide", budgetImpact: -80f, youthUnemploymentShock: -1.2f, approvalEffect: 0.5f),
                    new CabinetDecisionOption("Pilot in three regions", budgetImpact: -25f, youthUnemploymentShock: -0.4f)),
                new CabinetDecision(
                    "Curriculum Modernization Push",
                    "The drafted overhaul modernizes what schools teach - and has already united two teaching unions and one opposition party against it.",
                    new CabinetDecisionOption("Push the overhaul through", budgetImpact: -40f, approvalEffect: -1.5f, youthUnemploymentShock: -0.5f),
                    new CabinetDecisionOption("Phase it in over years", budgetImpact: -15f, youthUnemploymentShock: -0.2f)),
            },
            [(CabinetPortfolio.Education, CabinetMinisterPhilosophy.Pragmatic)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "Teacher Shortage Response",
                    "Vacancy data shows real shortages in key subjects. Emergency recruitment with signing bonuses works and costs; redeployment and larger classes is free and unpopular.",
                    new CabinetDecisionOption("Emergency recruitment and bonuses", budgetImpact: -60f, approvalEffect: 0.5f),
                    new CabinetDecisionOption("Redeploy and enlarge classes", approvalEffect: -1f)),
                new CabinetDecision(
                    "Graduate Placement Review",
                    "A placement-outcomes review found programs whose graduates consistently can't find work in field. Tying funding to placement would redirect money - and start a fight with the universities.",
                    new CabinetDecisionOption("Tie funding to placement", budgetImpact: 50f, youthUnemploymentShock: -0.6f, approvalEffect: -1f),
                    new CabinetDecisionOption("Publish the data only", youthUnemploymentShock: -0.2f)),
            },
            [(CabinetPortfolio.Education, CabinetMinisterPhilosophy.Traditionalist)] = new List<CabinetDecision>
            {
                new CabinetDecision(
                    "National Standards Examination",
                    "Your minister wants rigorous national exams reinstated - a clear bar, clearly measured, and clearly controversial.",
                    new CabinetDecisionOption("Reinstate nationally", budgetImpact: -20f, approvalEffect: -0.5f),
                    new CabinetDecisionOption("Voluntary adoption only", approvalEffect: 0f)),
                new CabinetDecision(
                    "Apprenticeship Guilds Revival",
                    "Employer guilds in the skilled trades want formal standing to train and certify again - an old model with modern placement numbers behind it.",
                    new CabinetDecisionOption("Back the guild model", budgetImpact: -35f, youthUnemploymentShock: -0.8f),
                    new CabinetDecisionOption("Encourage without funding", youthUnemploymentShock: -0.2f)),
            },
        };

        /// <summary>Draws every candidate for a portfolio (currently exactly 3, one per CabinetMinisterPhilosophy) in shuffled order - simpler than FederalReserveSystem.GenerateCandidates' own sampling, since the pool per portfolio is already exactly "2-3 candidates" without needing to sample a subset.</summary>
        public static List<CabinetMinister> GenerateCandidates(CabinetPortfolio portfolio)
        {
            var candidates = new List<CabinetMinister>(CandidatePool[portfolio]);
            Shuffle(candidates);
            return candidates;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = RandomSource.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>This minister's passive, always-on skill effect for the given portfolio, or 0 if no minister is currently appointed there - see the three point-of-use call sites in MacroSystem/SimulationManager.</summary>
        public static float GetCompetenceBias(Country country, CabinetPortfolio portfolio)
        {
            return country.CabinetMinisters.TryGetValue(portfolio, out CabinetMinister minister) ? minister.CompetenceBias : 0f;
        }

        /// <summary>
        /// Rolls, independently per appointed minister, whether a decision fires this turn - mirrors
        /// EventSystem.TryRollEvent's own per-turn roll, just once per minister instead of once per
        /// country. Returns the fired (portfolio, decision) pairs (usually empty; occasionally more
        /// than one if multiple ministers roll a hit the same turn) for the caller to present to the
        /// player - unlike EconomicEvent, nothing here is auto-applied, since a decision needs a
        /// player-picked response (see ApplyDecisionOption).
        /// </summary>
        public static List<(CabinetPortfolio Portfolio, CabinetDecision Decision)> TryRollDecisions(Country country)
        {
            var fired = new List<(CabinetPortfolio, CabinetDecision)>();
            foreach (KeyValuePair<CabinetPortfolio, CabinetMinister> appointment in country.CabinetMinisters)
            {
                if (RandomSource.NextDouble() > DecisionChancePerTurn)
                {
                    continue;
                }

                if (!DecisionPool.TryGetValue((appointment.Key, appointment.Value.Philosophy), out List<CabinetDecision> pool) || pool.Count == 0)
                {
                    continue;
                }

                fired.Add((appointment.Key, pool[RandomSource.Next(pool.Count)]));
            }

            return fired;
        }

        /// <summary>Applies a chosen CabinetDecisionOption's one-time, bounded effect immediately - see CabinetDecisionOption's own doc comment for why each field lands where it does.</summary>
        public static void ApplyDecisionOption(Country country, CabinetDecisionOption option)
        {
            EconomyState state = country.State;
            state.CrimeIndex = Mathf.Clamp(state.CrimeIndex + option.CrimeIndexShock, 0f, 100f);
            state.PovertyRate = Mathf.Clamp(state.PovertyRate + option.PovertyRateShock, 0f, 100f);
            // F1: routed through the one real path (stock + accumulator together) - see
            // SimulationManager.ApplyOneTimeBudgetImpact for the routing claim and the boundary.
            SimulationManager.ApplyOneTimeBudgetImpact(country, option.BudgetImpact);
            state.ApprovalRating = Mathf.Clamp(state.ApprovalRating + option.ApprovalEffect, 0f, 100f);
            // R4-4 (ruling R2): unclamped like ApplyMeetingOption's identical trade shock - the
            // trade system recomputes the balance every turn, which is the fade that qualifies it.
            state.TradeBalance += option.TradeBalanceShock;
            // R4-4 (ruling R2): [0, 100] mirrors the Crime/Poverty lines above; the stat's own
            // model re-asserts its tighter [0, 60] historical clamp at the next daily step.
            state.YouthUnemployment = Mathf.Clamp(state.YouthUnemployment + option.YouthUnemploymentShock, 0f, 100f);
        }
    }
}
