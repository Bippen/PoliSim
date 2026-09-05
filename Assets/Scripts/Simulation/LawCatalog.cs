using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// The static catalog of every authored law. Started as the MVP slice's proof-of-architecture
    /// four (2026-08-24); 50 Crime &amp; Justice laws by the first content marathon (2026-08-25), and
    /// a second marathon category - Labor Market - opened by pass 3 (2026-08-26). Authored in
    /// batches of ~10, real-world-grounded where a real policy exists, honestly labeled
    /// where it doesn't. Plain hardcoded data with no logic tying it to how it's consumed - the same
    /// "swappable later, e.g. for AI-generated content" idiom EventSystem.EventPool/
    /// FederalReserveSystem.CandidatePool/CabinetSystem's DecisionPool already establish for this
    /// codebase's other content pools.
    ///
    /// <para><b>The magnitude taxonomy</b> (stated once here, applied consistently rather than
    /// re-derived per law - "a number chosen to feel balanced" is exactly what this table exists to
    /// prevent): MINOR +-3 to 6 (a narrow, administrative, or single-issue change); MODERATE +-7 to
    /// 14 (a real, felt policy shift with a defined scope); MAJOR +-15 to 22 (a substantial reform -
    /// most of the real, landmark policies below land here); SWEEPING +-23 to 30 (the ceiling this
    /// codebase's own ~30 magnitude convention already sets - full decriminalization, full bail
    /// abolition, and similarly total reorientations only). A law's own doc comment states which
    /// tier each of its deltas sits in and why, not just the number.</para>
    ///
    /// <para><b>Per-dial magnitude scales (pass 3, the Labor Market category, ruled 2026-08-26)</b>:
    /// the same four-tier grid applies to every category, but two labor dials speak real units -
    /// MinimumWageDelta in Kaitz points, PaidFamilyLeaveWeeksDelta in weeks - so the tier
    /// comparison normalizes each delta by <see cref="DialMagnitudeScales"/> first (MinimumWage x2:
    /// a ~+6-Kaitz uprating like Germany's EUR 12 Mindestlohn step reads MODERATE, the ~+16-Kaitz
    /// Fight-for-$15 doubling reads SWEEPING; PaidFamilyLeave x1: weeks sit the grid naturally - a
    /// 12-week FMLA-scale mandate is MODERATE, a +26-week Nordic-scale build-out is SWEEPING; all
    /// 0-100 dial-point deltas x1). Band labels state units per dial where they differ.</para>
    ///
    /// <para><b>Real-world grounding, labeled per law</b>: CONFIRMED (a specific, well-documented
    /// real policy/law/program), DIRECTIONAL (a real trend or debate, not tied to one documented
    /// figure), or GENRE-IDIOM (a plausible governance-simulation abstraction in the Democracy-4
    /// idiom, not tied to one real policy) - stated in each law's own doc comment, never left
    /// implicit. Where a real policy's documented effect has a stated direction, the magnitude
    /// tracks it; where the delta is this project's own judgment call, the comment says so instead
    /// of dressing a guess as research (rule 5). <b>Also carried as LawDefinition.Citation</b>
    /// (2026-08-25, the browser rebuild) - a one-sentence, UI-facing distillation of the same
    /// grounding, surfaced in the law browser's detail pane for the first time. The four original
    /// MVP-slice laws predate the marathon's per-law research comment discipline and never had one
    /// to distill from; their Citation values below are grounded independently, in genuinely
    /// well-known real policy, and labeled with the same honesty rather than backfilled with false
    /// precision the original entries never claimed.</para>
    ///
    /// <para><b>The wanted-effects log</b> - real policy ideas researched for this marathon whose
    /// actual effect does NOT fit these six dials, kept here rather than silently discarded or
    /// force-fit, per the marathon's own explicit instruction that this list is probably the most
    /// valuable byproduct of the pass:
    /// <list type="bullet">
    /// <item>No-knock warrant restrictions, stop-and-frisk authority, qualified immunity, civil
    /// asset forfeiture (routine and organized-crime/anti-mafia variants alike) - all need a
    /// "permitted police tactics / search-seizure-liability authority" axis distinct from funding,
    /// severity, or any dial here; a few (stop-and-frisk, anti-mafia confiscation) get a WEAK
    /// funding/severity proxy below because a real law bundles other effects alongside the
    /// unrepresentable core, stated explicitly where that happens.</item>
    /// <item>Predictive policing / gang and criminal databases, DNA/forensic database expansion,
    /// biometric facial-recognition matching - a "surveillance/data-collection scope" axis, distinct
    /// from plain camera/equipment funding (which DOES fit PoliceFunding cleanly).</item>
    /// <item>Prison ownership/privatization, prison labor programs, minimum-space/conditions
    /// standards (the ECHR's Torreggiani v. Italy ruling) - a "custody capacity/conditions" axis,
    /// independent of how harsh sentences are on paper; a country can be severe with good conditions
    /// or lenient with an overcrowding crisis (Sweden is living this tension right now).</item>
    /// <item>Plea-bargaining scope, jury composition, speedy-trial deadlines, court digitization/
    /// case-backlog technology - a "court process capacity/efficiency" axis, distinct from
    /// JudicialFunding (money spent on salaries doesn't guarantee faster throughput without
    /// procedural/technology reform too - Italy's own PNRR reform bundles both for exactly this
    /// reason).</item>
    /// <item>Judicial appointment/independence structure (Poland's 2015-2023 disputes) - a
    /// structural "who appoints and disciplines judges" axis; JudicialFunding is at best a weak,
    /// dishonest proxy for what was actually a governance dispute, not a budget line.</item>
    /// <item>Needle exchange/naloxone/harm-reduction funding, supervised consumption sites - a
    /// public-health spending category, not enforcement or drug-policy STANCE; DrugPolicy captures
    /// the legal STANCE toward use, not the health-system response alongside it.</item>
    /// <item>US-style state-level cannabis legalization under a stricter federal regime - needs a
    /// federalism/sub-national-divergence axis a single national dial cannot represent (legal in
    /// some jurisdictions, felony in others, simultaneously).</item>
    /// <item>Officer/department civil liability rules, police union collective bargaining,
    /// independent oversight boards, body-camera footage public-release mandates - an
    /// "accountability/transparency regime" axis, distinct from the funding that buys the cameras.</item>
    /// </list>
    /// Not logged here (out of this marathon's category scope entirely, not a dial-space gap): gun
    /// control, victim compensation funds as their own regime, wrongful-conviction compensation,
    /// welfare-adjacent reentry/housing-first programs (a genuine cross-category tension - reentry
    /// support is arguably as much WelfareProgramType's territory as Crime &amp; Justice's).</para>
    ///
    /// <para><b>The labor wanted-effects log (pass 3, the Labor Market marathon, 2026-08-26)</b> -
    /// real labor-policy ideas researched for this category whose actual effect does NOT fit the
    /// six labor dials, kept per the same instruction as the log above:
    /// <list type="bullet">
    /// <item>Union/collective-bargaining structure (the Wagner Act, Taft-Hartley/right-to-work,
    /// sectoral bargaining extension, works councils/codetermination - Germany's Mitbestimmung) -
    /// no unionization/coverage axis exists anywhere in the model (grep-confirmed zero fields);
    /// a wage or overtime proxy would be dishonest for laws whose core is WHO bargains, so none
    /// is authored on a proxy alone.</item>
    /// <item>Gig/platform worker classification (California AB5, the EU Platform Work Directive) -
    /// a worker-classification axis; classification changes WHOSE work the standards cover, not
    /// any standard's level.</item>
    /// <item>Employment-protection legislation strictness (dismissal rules - Italy's Article 18
    /// and its Jobs Act rewrite, Spain's temporary-contract dualism) - an EPL axis distinct from
    /// hours regulation; OvertimeRegulationLevel covers working-TIME rules only.</item>
    /// <item>Unemployment-insurance generosity/duration (Hartz IV/Buergergeld, US pandemic UI
    /// extensions) - benefits are the non-player automatic stabilizer
    /// (Country.BenefitRatePerUnemployed), not a dial; a real UI-reform law has no lever to
    /// preset.</item>
    /// <item>Statutory minimum-wage INTRODUCTION where none exists (the EU Adequate Minimum Wages
    /// Directive pressing Sweden/Italy's bargaining models) - MinimumWageImplemented is a
    /// structural bool outside the delta space; flipping it by law is a new mechanism, said here
    /// rather than smuggled (the pass-3 charter's own words).</item>
    /// <item>Occupational-licensing scope, workplace-safety regimes (OSHA and kin), and
    /// retirement-age/pension-eligibility rules - each its own axis (safety is neither hours nor
    /// training; retirement age belongs to the pension system's own machinery).</item>
    /// </list></para>
    /// </summary>
    public static class LawCatalog
    {
        /// <summary>Code-review pass (2026-08-25): the magnitude taxonomy's own boundaries, stated as
        /// prose in this class's doc comment above ("MINOR +-3 to 6... MODERATE +-7 to 14... MAJOR
        /// +-15 to 22..."), now also as the actual constants GameController.LawMagnitudeTier reads -
        /// previously that method hardcoded 6f/14f/22f independently, a second, silent copy of the
        /// same three numbers this doc comment already commits to.</summary>
        /// <remarks>CONVENTION - a taxonomy boundary, not a measurement. The tiers exist so a law's size can be named on screen; the cut points are this project's own and are stated twice on purpose (see the summary above).</remarks>
        public const float MinorMagnitudeMax = 6f;
        /// <remarks>CONVENTION - a taxonomy boundary, not a measurement. The tiers exist so a law's size can be named on screen; the cut points are this project's own and are stated twice on purpose (see the summary above).</remarks>
        public const float ModerateMagnitudeMax = 14f;
        /// <remarks>CONVENTION - a taxonomy boundary, not a measurement. The tiers exist so a law's size can be named on screen; the cut points are this project's own and are stated twice on purpose (see the summary above).</remarks>
        public const float MajorMagnitudeMax = 22f;

        /// <summary>Per-dial normalization scales for the magnitude grid (pass 3 ruling,
        /// 2026-08-26) - index-locked to LawDefinition.DialDeltas' documented order (the C&amp;J six,
        /// then the labor six), read by GameController.LawMagnitudeTier as
        /// tier = max(|delta_i| x scale_i) against the three boundary consts above. 1f everywhere
        /// except MinimumWageDelta (x2 - Kaitz points run roughly half the numeric range of a
        /// 0-100 dial swing; the class doc's calibration cases anchor the choice). MUST grow in
        /// lockstep with DialDeltas, exactly like ParliamentSystem.LawDialSigns.</summary>
        public static readonly float[] DialMagnitudeScales =
        {
            1f, 1f, 1f, 1f, 1f, 1f,
            2f, 1f, 1f, 1f, 1f, 1f
        };

        public static readonly List<LawDefinition> All = new List<LawDefinition>
        {
            new LawDefinition
            {
                Id = "truth_in_sentencing_act",
                Name = "Truth in Sentencing Act",
                Description = "Requires offenders to serve a much larger share of their imposed sentence before parole eligibility, and narrows the discretion judges have to depart from guideline sentences.",
                Category = LawCategory.CrimeJustice,
                Citation = "The US Truth in Sentencing Incentive Grants program (1994 Crime Act), tying federal funding to states requiring violent offenders serve at least 85% of their sentence.",
                SentencingSeverityDelta = 15f,
                BailReformDelta = -8f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "cash_bail_reform_act",
                Name = "Cash Bail Reform Act",
                Description = "Replaces cash bail with risk-based pretrial release for most non-violent charges, and directs new funding toward the court staff needed to run individualized release hearings.",
                Category = LawCategory.CrimeJustice,
                Citation = "The broader US pretrial-reform trend toward risk-based release, distinct from and less extreme than this catalog's own full Cash Bail Abolition Act.",
                BailReformDelta = 18f,
                JudicialFundingDelta = 6f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "border_security_act",
                Name = "Border Security Act",
                Description = "Expands border enforcement staffing and surveillance infrastructure, and redirects a portion of drug-interdiction resources toward enforcement rather than treatment diversion.",
                Category = LawCategory.CrimeJustice,
                Citation = "The shape of the US Secure Fence Act (2006) and subsequent staffing/surveillance appropriations, generalized rather than tied to one bill's exact figures.",
                BorderEnforcementDelta = 20f,
                DrugPolicyDelta = -5f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "community_policing_initiative",
                Name = "Community Policing Initiative",
                Description = "Funds neighborhood policing programs and community liaison officers, paired with stricter enforcement of drug offenses in the areas they cover.",
                Category = LawCategory.CrimeJustice,
                Citation = "The US COPS Office (Community Oriented Policing Services), created by the 1994 Violent Crime Control Act, funding local community-policing programs nationally.",
                PoliceFundingDelta = 15f,
                DrugPolicyDelta = 10f,
                EnactmentApprovalCost = 0.5f
            },

            // ================================================================================
            // BATCH 1 of the content marathon (2026-08-25) - 10 laws, dial spread deliberately
            // uneven: SentencingSeverity draws by far the richest real-world documentation (it's
            // the most contested, most-legislated axis of criminal-justice policy globally), so 8
            // of 10 touch it (4 as the primary target); BailReform and DrugPolicy each get a real
            // opposed PAIR (one law pushing each direction, proving the dial swings both ways
            // under real policy, not just one); PoliceFunding/JudicialFunding/BorderEnforcement
            // each get exactly one clean primary law here - genuinely thinner in this batch, not
            // yet a coverage gap (composition finding, reported in full in CLAUDE.md).
            // ================================================================================

            new LawDefinition
            {
                Id = "three_strikes_law",
                Name = "Three Strikes Law",
                Description = "Mandates a lengthy or life sentence for a third serious or violent felony conviction, regardless of the individual circumstances.",
                Category = LawCategory.CrimeJustice,
                Citation = "California's 1994 law (25-to-life for any third felony), copied by 20+ US states; narrowed by a 2012 ballot measure (Prop 36) to require the third strike itself be serious/violent.",
                // CONFIRMED - California's 1994 law (25-to-life for any third felony), copied by
                // 20+ US states; later narrowed by a 2012 ballot measure (Prop 36, ~70% approval)
                // to require the third strike itself be serious/violent. MAJOR: one of the most
                // severe real sentencing regimes on record, but California's own 1994 form (before
                // the 2012 narrowing) is the anchor, not the softened version - kept below SWEEPING
                // since it targets only repeat offenders, not the sentencing code generally.
                SentencingSeverityDelta = 20f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "mandatory_minimum_sentencing_act",
                Name = "Mandatory Minimum Sentencing Act",
                Description = "Sets fixed minimum prison terms for a range of offenses that judges cannot depart below regardless of circumstances, with the heaviest minimums attached to drug offenses.",
                Category = LawCategory.CrimeJustice,
                Citation = "The US federal 1986 Anti-Drug Abuse Act (the 100:1 crack/powder cocaine disparity) and France's 'peines plancher' (2007-2014, repealed after evidence found no deterrent effect).",
                // CONFIRMED - US federal 1986 Anti-Drug Abuse Act (the 100:1 crack/powder cocaine
                // disparity); France's "peines plancher" (2007-2014, repealed after evidence found
                // no deterrent effect, only longer sentences for the same recidivism rate - a real
                // policy this game's own model would be able to show reversing, see the repeal
                // path). MAJOR on severity (removes judicial discretion, the defining feature of a
                // mandatory-minimum regime); MODERATE secondary on drug policy, since the 1986 Act
                // was substantially drug-offense-driven.
                SentencingSeverityDelta = 16f,
                DrugPolicyDelta = 8f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "first_step_act",
                Name = "First Step Act",
                Description = "Retroactively reduces some mandatory minimums, expands judges' discretion to depart from sentencing guidelines in qualifying cases, and expands access to rehabilitation programming.",
                Category = LawCategory.CrimeJustice,
                Citation = "The US First Step Act (2018); ~30,000 released early by 2023, with recidivism ~37% lower among those released under it (Council on Criminal Justice).",
                // CONFIRMED - US federal law, 2018. ~30,000 released early by 2023 and recidivism
                // ~37% lower among those released under it (Council on Criminal Justice), though
                // implementation was widely reported as uneven. MODERATE, not MAJOR: a real,
                // measured effect, but scoped to federal prisoners meeting specific criteria, not a
                // sentencing-code-wide rewrite.
                SentencingSeverityDelta = -14f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "rehabilitation_centered_corrections_model",
                Name = "Rehabilitation-Centered Corrections Model",
                Description = "Reorients the corrections system around reintegration rather than punishment - short sentences, open-prison placements, and heavy use of fines and community sanctions over incarceration.",
                Category = LawCategory.CrimeJustice,
                Citation = "As a standing model - Germany's constitutional Resozialisierungsgebot (rehabilitation mandate) and Sweden's own tradition before its 2023 reversal.",
                // CONFIRMED as a real, standing model, not a single bill - Germany's constitutional
                // "Resozialisierungsgebot" (rehabilitation mandate, rooted in 1970s Federal
                // Constitutional Court rulings; German incarceration runs about 76/100k against the
                // US's 716/100k, with roughly half the US recidivism rate) and Sweden's own
                // tradition until its 2023 reversal (see the Gang Crime Sentencing Escalation law,
                // batch 2). Modeled here as the foundational stance a government adopts on
                // enactment - SWEEPING on severity (this is the widest real-world swing available on
                // this dial), MODERATE secondary on bail (rehabilitation-oriented systems also lean
                // toward non-cash pretrial release). The higher approval cost reflects a genuinely
                // major reorientation, not a routine bill.
                SentencingSeverityDelta = -25f,
                BailReformDelta = 8f,
                EnactmentApprovalCost = 2.0f
            },
            new LawDefinition
            {
                Id = "cash_bail_abolition_act",
                Name = "Cash Bail Abolition Act",
                Description = "Eliminates monetary bail entirely - pretrial release or detention is decided solely on a judge's assessment of flight and public-safety risk.",
                Category = LawCategory.CrimeJustice,
                Citation = "Illinois' Pretrial Fairness Act, upheld by the state supreme court in July 2023 and effective that September - the first full statewide bail abolition in the US.",
                // CONFIRMED - Illinois' Pretrial Fairness Act, upheld by the state supreme court in
                // July 2023 and effective that September - the first full statewide abolition in the
                // US (distinct from and going further than this catalog's own "Cash Bail Reform
                // Act," which is a risk-based-release REFORM that still permits bail in some form).
                // SWEEPING: full abolition is as far as this dial goes by construction.
                BailReformDelta = 28f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "bail_reform_rollback",
                Name = "Bail Reform Rollback",
                Description = "Restores judges' authority to detain a defendant pretrial based on perceived danger to the public, partially reversing an earlier bail-reform law under political pressure.",
                Category = LawCategory.CrimeJustice,
                Citation = "New York's 2019 reform was partially rolled back in 2020/2022/2023 after a 2020 NYC murder spike, despite researchers (including the NYCLU) finding no clear causal link to the original reform.",
                // CONFIRMED - New York's 2019 reform narrowed bail-eligible offenses; after a 2020
                // NYC murder spike (+40% in a year) and sustained political pressure, the
                // legislature enacted partial rollbacks in 2020, 2022, and 2023, despite researchers
                // (including the NYCLU) finding no clear causal link between the original reform and
                // the crime rise. Included deliberately as the real, documented case that this
                // dial's real-world history is NOT one-directional. MODERATE: a partial walk-back,
                // not a full reversion to unrestricted cash bail; MINOR secondary severity uptick
                // (the restored "dangerousness" standard is itself a harshness-adjacent judicial
                // power).
                BailReformDelta = -12f,
                SentencingSeverityDelta = 4f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "drug_decriminalization_act",
                Name = "Drug Decriminalization Act",
                Description = "Shifts personal possession of small amounts of all drugs from a criminal matter to a civil one - a fine and a referral to a treatment-and-assessment panel rather than prosecution. Trafficking remains a criminal offense.",
                Category = LawCategory.CrimeJustice,
                Citation = "Portugal's Law 30/2000 (Lei n. 30/2000), decriminalizing personal drug possession from July 2001 - this dial's standard real-world calibration touchstone (associated with lower HIV transmission and, for many years, below-EU-average drug deaths).",
                // CONFIRMED, cited here as the standard real-world touchstone for this dial's low
                // end (Portugal's 2001 decriminalization, per this project's own instruction to use
                // it for calibration even though Portugal isn't one of the six seeded countries) -
                // associated with lower HIV transmission and, for many years, below-EU-average drug
                // deaths, though honestly noted (2020s reporting shows funding strain and rising
                // visible use in Lisbon - not treated here as an unqualified success story). SWEEPING
                // on drug policy (all-drug personal decriminalization is close to the floor of this
                // dial); MODERATE secondary on severity (a real, felt softening of the broader
                // punitive posture, short of a full corrections-model reorientation).
                DrugPolicyDelta = -26f,
                SentencingSeverityDelta = -8f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "germany_cannabis_legalization",
                Name = "Cannabis Legalization Act",
                Description = "Legalizes adult possession and home cultivation of cannabis in limited amounts, plus non-commercial cultivation clubs; commercial retail is deferred to separate pilot programs.",
                Category = LawCategory.CrimeJustice,
                Citation = "Germany's Cannabisgesetz, effective April 1, 2024, passed by the Scholz coalition; courts had to retroactively review and expunge prior convictions.",
                // CONFIRMED, real and recent - Germany's Cannabisgesetz, effective April 1, 2024,
                // passed by the Scholz coalition over opposition. Courts had to retroactively review
                // and expunge prior convictions - a genuine administrative burden, which is where
                // the secondary judicial-funding term comes from. MODERATE, not SWEEPING or even
                // MAJOR: cannabis-only, and without commercial retail its real black-market impact is
                // honestly limited (this project's own DIRECTIONAL label on that specific claim,
                // since the law is too recent for settled long-run evidence).
                DrugPolicyDelta = -14f,
                JudicialFundingDelta = 4f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "sweden_zero_tolerance_drug_policy",
                Name = "Zero-Tolerance Drug Policy",
                Description = "Criminalizes drug use itself, not just possession or sale, and empowers police to compel testing on suspicion of use alone, in pursuit of an explicit national goal of a drug-free society.",
                Category = LawCategory.CrimeJustice,
                Citation = "Sweden's own regime since 1988, one of the strictest in the EU by the EU drug agency's (EMCDDA) comparative reporting.",
                // CONFIRMED - Sweden's own regime since 1988, one of the strictest in the EU by the
                // EU drug agency's (EMCDDA) comparative reporting. Comparative drug-death-rate claims
                // for this policy are genuinely contested methodologically, so none is asserted here
                // - only the policy's own real, documented shape. MAJOR on drug policy (criminalizing
                // use itself, not just possession, is a categorically stricter regime than most
                // comparators); MODERATE secondary on severity (the compelled-testing power is itself
                // an enforcement-harshness signal).
                DrugPolicyDelta = 18f,
                SentencingSeverityDelta = 6f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "public_defender_funding_act",
                Name = "Public Defender & Legal Aid Funding Act",
                Description = "Substantially increases funding for public defenders and legal aid, reducing attorney caseloads and expanding access to counsel for defendants who cannot afford one.",
                Category = LawCategory.CrimeJustice,
                Citation = "As a standing question - the US constitutional right to counsel (Gideon v. Wainwright, 1963) is chronically underfunded; all six seeded countries run their own legal-aid systems facing the same funding-adequacy debate.",
                // CONFIRMED as a real, persistent policy question, not one single bill - the US
                // constitutional right to counsel (Gideon v. Wainwright, 1963) is chronically
                // underfunded in practice, a widely-documented crisis; all six seeded countries have
                // some form of state legal aid (Germany's Prozesskostenhilfe, France's aide
                // juridictionnelle, Sweden's rattshjalp among them) with funding adequacy a
                // recurring, DIRECTIONAL complaint rather than one confirmed figure. MAJOR: this is
                // this catalog's cleanest, most direct real-world lever for JudicialFunding - a
                // genuine, substantial appropriations increase, not a marginal one. MINOR secondary
                // softening on severity (better-resourced defense correlates with less punitive
                // outcomes, an indirect and modest effect, not asserted as causally proven).
                JudicialFundingDelta = 16f,
                SentencingSeverityDelta = -4f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "immigration_detention_expansion_act",
                Name = "Immigration Detention Expansion Act",
                Description = "Expands detention capacity and staffing for individuals awaiting immigration proceedings or removal, distinct from physical border-barrier construction.",
                Category = LawCategory.CrimeJustice,
                Citation = "US ICE immigration-detention capacity, a routine, contested appropriations line that has expanded and contracted repeatedly across administrations.",
                // CONFIRMED as a real, recurring policy lever - US immigration-detention capacity
                // has expanded and contracted repeatedly across administrations (ICE detention
                // funding is a routine, contested appropriations line), kept deliberately distinct
                // from a border-wall-style physical barrier (already this catalog's own "Border
                // Security Act") since capacity expansion and barrier construction are genuinely
                // different real levers governments pull independently. MAJOR on border enforcement
                // (a substantial capacity commitment); MINOR secondary on police funding (detention
                // staffing draws on the same broader law-enforcement labor market).
                BorderEnforcementDelta = 16f,
                PoliceFundingDelta = 4f,
                EnactmentApprovalCost = 1.0f
            },

            // ================================================================================
            // BATCH 2 of the content marathon (2026-08-25) - 11 laws, deliberately weighted
            // toward the three dials batch 1 left thin (PoliceFunding, JudicialFunding,
            // BorderEnforcement), while still drawing real content for Sentencing/Drug where it
            // was strong. BorderEnforcement specifically gets a genuine BOTH-directions pair
            // (Frontex/Sanctuary) matching the Bail/Drug pairs batch 1 already established.
            // ================================================================================

            new LawDefinition
            {
                Id = "body_worn_camera_program",
                Name = "Body-Worn Camera Program",
                Description = "Equips police officers with body-worn cameras and funds the storage and review infrastructure the footage requires.",
                Category = LawCategory.CrimeJustice,
                Citation = "Rapid US adoption after Ferguson (2014). A DC Metro randomized trial (2017) found modest-to-no effect on use of force but materially better evidence and complaint resolution.",
                // CONFIRMED - rapid US adoption after Ferguson (2014), driven by DOJ grant funding.
                // A DC Metro randomized trial (2017) found modest-to-no effect on use of force but
                // materially better evidence and complaint resolution - the real headline effect
                // (officer accountability/transparency) isn't literally any of the six dials, so
                // funding is the honest, if imperfect, proxy used here (see the wanted-effects log's
                // accountability/transparency axis). MODERATE: real equipment and storage cost, not
                // a sweeping department overhaul.
                PoliceFundingDelta = 8f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "militarized_police_equipment_program",
                Name = "Militarized Police Equipment Program",
                Description = "Transfers surplus military-grade equipment and vehicles to local police departments at little or no cost, expanding their tactical capability.",
                Category = LawCategory.CrimeJustice,
                Citation = "The US DoD 1033 Program (1997 NDAA), restricted by the Obama administration in 2015 after Ferguson, reversed by the Trump administration in 2017.",
                // CONFIRMED - the US DoD 1033 Program (created by the 1997 NDAA), which came under
                // national scrutiny after Ferguson 2014 (armored vehicles facing protesters);
                // restricted by the Obama administration in 2015, reversed by the Trump
                // administration in 2017. MAJOR on police funding (an in-kind capability transfer,
                // not just a cash line); MINOR secondary severity uptick (heavier tactical equipment
                // is a real, if modest, enforcement-posture signal).
                PoliceFundingDelta = 18f,
                SentencingSeverityDelta = 4f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "cybercrime_investigation_unit",
                Name = "Cybercrime Investigation Unit",
                Description = "Establishes a dedicated police unit and funding stream for investigating online fraud, hacking, and digital exploitation offenses.",
                Category = LawCategory.CrimeJustice,
                Citation = "Every modern police force has built out some form of cybercrime capability over the past two decades, not tied to one single confirmed founding law across all six countries.",
                // GENRE-IDIOM/DIRECTIONAL - every modern police force has built out some form of
                // cybercrime capability over the past two decades, but this project isn't citing one
                // single confirmed founding law across all six countries the way, say, Germany's
                // cannabis law can be. MODERATE: a real, standalone funding commitment, clean single-
                // dial fit.
                PoliceFundingDelta = 10f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "gang_crime_sentencing_escalation",
                Name = "Gang Crime Sentencing Escalation",
                Description = "Sharply raises maximum sentences for crimes committed within organized criminal networks and removes reduced sentencing discounts for young offenders in gang cases.",
                Category = LawCategory.CrimeJustice,
                Citation = "Sweden's Tido Agreement (Oct 2022, in force April 2024): gang-related sentences up to double the previous maximum; 2023 prison sentences up 25% year-on-year.",
                // CONFIRMED - Sweden's Tidö Agreement (Oct 2022), in force from April 2024:
                // gang-related sentences up to double the previous maximum (capped at 18 years), and
                // abolished the sentencing discount for 18-21-year-olds in gang crime cases. Real
                // 2023 result: prison sentences up 25% year-on-year, prisons at capacity, and gangs
                // reportedly responded by recruiting MORE minors (who face the separate juvenile
                // system) - a genuine, documented backlash effect, not asserted here as this law's
                // OWN dial effect (that would need a "gang recruitment of minors" mechanic this
                // catalog doesn't model), only named honestly as real-world context. MAJOR on
                // severity; MODERATE secondary on police funding (the same package included expanded
                // search powers, whose own core effect belongs in the wanted-effects log's
                // police-tactics axis - only the funding-shaped remainder is represented here).
                SentencingSeverityDelta = 20f,
                PoliceFundingDelta = 6f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "court_backlog_reduction_program",
                Name = "Court Backlog Reduction Program",
                Description = "Invests in additional judges, court staff, and procedural reform aimed at cutting multi-year case backlogs and excessive trial delays.",
                Category = LawCategory.CrimeJustice,
                Citation = "Italy's 2001 Pinto Law and its post-COVID EU Recovery Plan (PNRR, 2021+), which made judicial-backlog reduction a large, EU-monitored funding condition.",
                // CONFIRMED - Italy is the standard European reference case: its 2001 "Pinto Law"
                // compensates citizens for excessive trial delays (a tacit admission of the
                // problem), and its post-COVID EU Recovery Plan (PNRR, 2021+) made judicial-backlog
                // reduction a large, EU-monitored funding condition. Honestly noted: backlog
                // reduction is really a court-process-efficiency question (see the wanted-effects
                // log), and money alone doesn't guarantee faster throughput without procedural/
                // technology reform alongside it - JudicialFunding is the closest real lever this
                // catalog has, not a perfect one. MAJOR: PNRR-scale investment, not a marginal line
                // item.
                JudicialFundingDelta = 20f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "court_interpreter_funding",
                Name = "Court Interpreter & Translation Funding",
                Description = "Funds qualified interpreters and translated materials for non-native speakers in criminal proceedings.",
                Category = LawCategory.CrimeJustice,
                Citation = "The US Court Interpreters Act (1978) and the EU's Directive 2010/64/EU on the right to interpretation and translation, binding on Germany, France, Italy, Sweden, and Poland.",
                // CONFIRMED - the US Court Interpreters Act (1978, federal courts) and the EU's
                // Directive 2010/64/EU on the right to interpretation and translation in criminal
                // proceedings (binding on Germany, France, Italy, Sweden, and Poland alike - a
                // genuinely significant EU-wide harmonization). MINOR: the cleanest, least
                // controversial funding-direction fit in this catalog, but narrow in scope - rarely
                // a major line item on its own.
                JudicialFundingDelta = 5f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "victim_witness_support_funding",
                Name = "Victim & Witness Support Services Funding",
                Description = "Funds victim counseling, court accompaniment, case-status notification services, and protection against witness intimidation.",
                Category = LawCategory.CrimeJustice,
                Citation = "The US Victims of Crime Act (VOCA, 1984) and the EU's Victims' Rights Directive (2012), binding across Germany, France, Italy, Sweden, and Poland.",
                // CONFIRMED - the US Victims of Crime Act (VOCA, 1984, funded via criminal fines
                // rather than general taxation - a distinctive mechanism that faced a well-publicized
                // real funding shortfall in the 2020s as fine revenue declined) and the EU's Victims'
                // Rights Directive (2012, binding across Germany, France, Italy, Sweden, and Poland).
                // MODERATE: a real, standalone service-funding commitment.
                JudicialFundingDelta = 7f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "frontex_border_cooperation_agreement",
                Name = "Frontex Border Cooperation Agreement",
                Description = "Deepens funding and operational cooperation with the EU's joint border and coast guard agency for surveillance, patrol, and deportation-flight support.",
                Category = LawCategory.CrimeJustice,
                Citation = "Frontex (founded 2004), its budget expanded roughly 40x after the 2015 migrant crisis, targeting a 10,000-officer standing corps by 2027.",
                // CONFIRMED - Frontex, founded 2004, with its budget expanded roughly 40x after the
                // 2015 migrant crisis and a standing corps of 10,000 officers targeted by 2027; the
                // agency has also been repeatedly investigated for complicity in Mediterranean/Aegean
                // pushbacks (an OLAF probe led to its director's resignation in 2022, noted honestly
                // rather than omitted). Only meaningfully applicable to the four EU member states
                // among the six (Germany, France, Italy, Poland) in the real world, though the model
                // doesn't currently gate laws by country - a scope note, not a blocker. MODERATE:
                // deeper cooperation versus the baseline, not the sweeping end of this dial (a full
                // physical barrier, this catalog's own Border Security Act, sits higher).
                BorderEnforcementDelta = 12f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "refugee_asylum_fast_track_processing",
                Name = "Refugee & Asylum Fast-Track Processing",
                Description = "Expands staffing and streamlines procedure for asylum claims, reducing detention time and processing backlogs for people awaiting a decision.",
                Category = LawCategory.CrimeJustice,
                Citation = "A real, recurring EU migration-policy debate over asylum-system capacity and processing time, not tied to one single confirmed law.",
                // GENRE-IDIOM/DIRECTIONAL - a real, recurring policy debate across all six countries
                // (asylum-system capacity and processing-time reform is a standing item in EU
                // migration policy generally), not tied here to one single confirmed law the way the
                // Belarus border wall or Frontex's own founding are. Included deliberately as the
                // genuine LENIENT-direction counterweight to this catalog's existing Border Security
                // Act/Immigration Detention Expansion Act, the same both-directions balance the
                // Bail/Drug dials already have. MODERATE on border enforcement (toward lenient);
                // MINOR secondary on judicial funding (the staffing needed to actually process claims
                // faster).
                BorderEnforcementDelta = -14f,
                JudicialFundingDelta = 5f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "sanctuary_city_policy",
                Name = "Sanctuary City Policy",
                Description = "Bars local police from asking about immigration status or assisting federal immigration enforcement in routine policing.",
                Category = LawCategory.CrimeJustice,
                Citation = "A real US policy adopted by hundreds of jurisdictions since the 1980s-2000s, repeatedly contested in court over threatened federal funding cutoffs.",
                // CONFIRMED - a real US policy adopted by hundreds of jurisdictions since the
                // 1980s-2000s, repeatedly contested in court over federal funding cutoffs threatened
                // in retaliation. The real mechanism is INTERIOR enforcement cooperation (or its
                // absence) rather than the physical border itself, which BorderEnforcement as this
                // catalog defines it centers on - a related but distinct axis, noted honestly as an
                // imperfect fit rather than a clean one. MODERATE.
                BorderEnforcementDelta = -10f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "prosecutorial_discretion_guidelines",
                Name = "Prosecutorial Discretion Guidelines",
                Description = "Directs prosecutors to deprioritize charging low-level, non-violent offenses - including simple drug possession - freeing capacity for serious crime.",
                Category = LawCategory.CrimeJustice,
                Citation = "The US 'progressive prosecutor' trend (Philadelphia's Larry Krasner, elected 2017; Los Angeles' George Gascon, elected 2020), a documented, politically contested real trend.",
                // CONFIRMED - the real US "progressive prosecutor" trend (Philadelphia DA Larry
                // Krasner, elected 2017; Los Angeles DA George Gascon, elected 2020, among others),
                // each adopting office-wide charging-deprioritization policies for low-level offenses
                // - a genuinely documented, politically contested real trend, not a single national
                // law. MODERATE on severity; MINOR secondary on drug policy (possession specifically
                // named in real guidelines of this kind).
                SentencingSeverityDelta = -10f,
                DrugPolicyDelta = -6f,
                EnactmentApprovalCost = 1.0f
            },

            // ================================================================================
            // BATCH 3 of the content marathon (2026-08-25) - 12 laws. Fills the remaining
            // sentencing/corrections real-world content (restorative justice, juvenile justice,
            // hate crime, electronic monitoring), adds genuinely triple-dial laws (drug courts,
            // mental health courts) to exercise composition beyond simple pairs, and rounds out
            // BorderEnforcement with a real strict/lenient spread (physical barrier, Schengen
            // reimposition, amnesty). One law (Strict Drug Classification Equalization Act) is
            // included specifically because its REAL history includes a court striking it down -
            // good material for the marathon's own repeal-path exercise, not just its enact path.
            // ================================================================================

            new LawDefinition
            {
                Id = "electronic_monitoring_program",
                Name = "Electronic Monitoring Program",
                Description = "Substitutes GPS ankle monitoring for a portion of short prison terms or as a release condition, rather than incarceration.",
                Category = LawCategory.CrimeJustice,
                Citation = "Scope varies by country - France's electronic-monitoring custody alternative (in genuine use since ~2000) versus Germany's narrower post-sentence supervision use (since 2011).",
                // CONFIRMED, though real scope varies by country - France's "placement sous
                // surveillance electronique" (1997 law, in genuine use as a custody alternative
                // since ~2000) versus Germany's narrower post-sentence supervision use (since 2011,
                // mainly for high-risk offenders rather than a general alternative). Honestly flagged
                // as an imperfect fit even where used: its real function is adding a THIRD custody
                // modality (a capacity/cost lever), which SentencingSeverity only approximates - see
                // the wanted-effects log's custody-capacity axis. MODERATE on severity; MINOR
                // secondary on judicial funding (monitoring infrastructure/compliance staff).
                SentencingSeverityDelta = -8f,
                JudicialFundingDelta = 3f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "restorative_justice_program",
                Name = "Restorative Justice & Victim-Offender Mediation",
                Description = "Establishes a formal mediation process between victim and offender that can reduce or substitute for a criminal sentence.",
                Category = LawCategory.CrimeJustice,
                Citation = "For Germany - Tater-Opfer-Ausgleich, codified into the Criminal Code (Sec. 46a StGB) in 1994. The others follow the same direction.",
                // CONFIRMED for Germany - "Tater-Opfer-Ausgleich," codified into the Criminal Code
                // (Sec. 46a StGB) in 1994, can reduce or substitute punishment.
                // DIRECTIONAL/GENRE-IDIOM elsewhere - pilot programs exist in France and Poland but
                // aren't comparably institutionalized. MODERATE.
                SentencingSeverityDelta = -10f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "juvenile_justice_reform",
                Name = "Juvenile Justice Reform",
                Description = "Raises the age of adult-court jurisdiction and channels youth offenders into a separate, rehabilitation-focused juvenile justice track.",
                Category = LawCategory.CrimeJustice,
                Citation = "Germany's Jugendgerichtsgesetz (a separate juvenile code since 1953) and the US 'raise the age' state trend (roughly ten states, 2007-2019).",
                // CONFIRMED - Germany's Jugendgerichtsgesetz (a separate, strongly rehabilitative
                // juvenile criminal code since 1953) and the US "raise the age" state trend (e.g.
                // New York 2017; roughly ten states raised the age to 18 between 2007 and 2019).
                // MODERATE, deliberately not higher: honestly scoped to juveniles only, an
                // age-carve-out rather than a general-population policy - it moves the same
                // SentencingSeverity dial every other law here does, but its real effect is narrower
                // than that single number can show (a scenario/eligibility dimension would represent
                // it more precisely - noted, not solved, in the wanted-effects log's spirit even
                // though this one law is still usable as a real, if imperfect, MODERATE nudge).
                SentencingSeverityDelta = -8f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "hate_crime_sentencing_enhancement",
                Name = "Hate Crime Sentencing Enhancement",
                Description = "Adds an additional penalty on top of the underlying offense when the crime is proven to be motivated by bias against the victim's identity.",
                Category = LawCategory.CrimeJustice,
                Citation = "For the US - the federal Hate Crimes Sentencing Enhancement Act (1994). For the European five, which typically use a separate hate-speech offense instead.",
                // CONFIRMED for the US - the federal Hate Crimes Sentencing Enhancement Act (1994)
                // plus nearly all states' own enhancement statutes. DIRECTIONAL/GENRE-IDIOM for the
                // European five, which typically criminalize hate speech/incitement as a SEPARATE
                // offense (e.g. Germany's Volksverhetzung, Sec. 130 StGB) rather than bolting an
                // enhancement onto an existing charge - a meaningfully different legal mechanism,
                // noted honestly rather than glossed over. MINOR: narrow, category-specific, doesn't
                // move overall severity much on its own.
                SentencingSeverityDelta = 5f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "risk_based_pretrial_assessment",
                Name = "Risk-Based Pretrial Assessment",
                Description = "Replaces cash-bail decisions with an algorithmic risk score for flight and public-safety danger, expanding summons-in-lieu-of-arrest for low-risk defendants.",
                Category = LawCategory.CrimeJustice,
                Citation = "New Jersey's 2017 Criminal Justice Reform Act; pretrial jail population fell over 40% in two years, violent crime fell about 44% from 2015 to 2019.",
                // CONFIRMED - New Jersey's 2017 Criminal Justice Reform Act (Public Safety
                // Assessment). Pretrial jail population fell over 40% in two years; the
                // court-appearance rate barely moved (92.7% to 89.4%); violent crime fell about 44%
                // from 2015 to 2019; summons use rose from 54% to 71% of cases - a real, substantial,
                // well-measured reform, deliberately kept distinct from and less extreme than this
                // catalog's own Cash Bail Abolition Act (which eliminates bail outright; this keeps a
                // structured assessment process instead). MAJOR on bail reform; MINOR secondary on
                // judicial funding (the assessment infrastructure itself).
                BailReformDelta = 14f,
                JudicialFundingDelta = 4f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "mental_health_diversion_courts",
                Name = "Mental Health Diversion Courts",
                Description = "Establishes specialized court dockets that divert defendants with serious mental illness into judicially-supervised treatment instead of standard prosecution.",
                Category = LawCategory.CrimeJustice,
                Citation = "The first mental health court, Broward County, Florida, 1997, with federal support following via the America's Law Enforcement and Mental Health Project Act (2000).",
                // CONFIRMED - the first mental health court, Broward County, Florida, 1997, with
                // federal support following via the America's Law Enforcement and Mental Health
                // Project Act (2000). Evidence generally shows lower recidivism for graduates versus
                // traditional incarceration, with capacity/access limits as a common real criticism.
                // Genuinely dual-primary: MODERATE reduction in severity for the diverted population,
                // MODERATE increase in judicial funding (the specialized staffing/infrastructure a
                // dedicated docket requires) - a real law where neither dial is clearly "secondary."
                SentencingSeverityDelta = -9f,
                JudicialFundingDelta = 8f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "drug_courts_program",
                Name = "Drug Courts Program",
                Description = "Establishes specialized court dockets that divert eligible non-violent drug offenders into supervised treatment instead of prosecution or incarceration.",
                Category = LawCategory.CrimeJustice,
                Citation = "For the US - the first drug court, Miami-Dade County, 1989; roughly 3,000 now operate nationally. The others follow the same direction.",
                // CONFIRMED for the US - the first drug court, Miami-Dade County, 1989; roughly
                // 3,000 now operate nationally. DIRECTIONAL/GENRE-IDIOM for the other five countries,
                // which have structurally different analogues (Germany's "Therapie statt Strafe"
                // under BtMG Sec. 35; Italy's "messa alla prova" probation-diversion) rather than the
                // specific US drug-court institutional model. A genuinely triple-dial law: MODERATE
                // toward decriminalized on drug policy (the primary target), with MODERATE secondary
                // reductions on both severity and a judicial-funding increase (specialized docket
                // staffing) - composition across three dials at once, deliberately included to
                // exercise that shape before fifty laws makes it common.
                DrugPolicyDelta = -10f,
                SentencingSeverityDelta = -6f,
                JudicialFundingDelta = 6f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "strict_drug_classification_equalization_act",
                Name = "Strict Drug Classification Equalization Act",
                Description = "Equalizes the legal treatment and penalties for cannabis with those for harder drugs, removing distinctions in sentencing between drug categories.",
                Category = LawCategory.CrimeJustice,
                Citation = "Italy's 2006 Fini-Giovanardi law equalized cannabis and hard-drug penalties; struck down by Italy's Constitutional Court in 2014 on procedural grounds.",
                // CONFIRMED - Italy's 2006 Fini-Giovanardi law equalized cannabis and hard-drug
                // penalties; Italy's Constitutional Court struck it down in 2014 on procedural
                // grounds, reverting the country to its earlier, more lenient framework. Included
                // deliberately as REAL history where the law was later reversed by a court, not by a
                // repeal bill - this project's own repeal MECHANISM still models it as a standard
                // Crime & Justice repeal bill (the constitutional-strike-down route itself is a
                // structural/judicial-review mechanism this catalog doesn't represent, noted honestly
                // rather than silently assumed away). MAJOR on drug policy; MODERATE secondary on
                // severity (equalizing penalties upward is itself a real harshness increase).
                DrugPolicyDelta = 16f,
                SentencingSeverityDelta = 8f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "physical_border_barrier_construction",
                Name = "Physical Border Barrier Construction",
                Description = "Constructs a physical steel-and-sensor barrier along the border, paired with a surge in border-guard personnel to block irregular crossings.",
                Category = LawCategory.CrimeJustice,
                Citation = "Poland's Belarus border wall (built Jan-Jun 2022, 187km, ~$407M), later supplemented by a ~206km electronic sensor layer.",
                // CONFIRMED - Poland's Belarus border wall, built January-June 2022 (187km,
                // 5.5-meter steel construction, roughly $407M), later supplemented by a roughly
                // 206km electronic sensor layer (about EUR72M, completed roughly 2023), in response
                // to the 2021 Belarus-engineered migrant crisis; a border-zone state of emergency
                // was declared in 2021, and pushback practices drew real rights-group criticism
                // (noted honestly, not omitted). Deliberately distinguished from this catalog's own
                // Border Security Act (staffing/surveillance infrastructure, MAJOR) as the more
                // extreme, capital-intensive, physical-construction end of this dial. SWEEPING - as
                // strict as this dial realistically gets.
                BorderEnforcementDelta = 26f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "schengen_border_reimposition",
                Name = "Schengen Border Reimposition",
                Description = "Reinstates border checks at internal frontiers that would normally be open under a shared free-movement area, citing migration and cross-border crime concerns.",
                Category = LawCategory.CrimeJustice,
                Citation = "Real and recent - Germany reinstated checks at all nine of its land borders by September 2024, citing migration and serious-crime concerns.",
                // CONFIRMED, real and recent - Germany reinstated checks at all nine of its land
                // borders by September 2024 (Poland, Czechia, Austria, and Switzerland from October
                // 2023), with the interior minister citing migration and "acute dangers... serious
                // crime"; a much-criticized break from open Schengen movement. MODERATE-MAJOR: a
                // real, substantial enforcement escalation, though procedural (checkpoints, not new
                // infrastructure) rather than the sweeping, capital-intensive end this catalog's own
                // Physical Border Barrier Construction occupies.
                BorderEnforcementDelta = 14f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "amnesty_regularization_program",
                Name = "Amnesty & Regularization Program",
                Description = "Grants legal status to undocumented residents who meet defined criteria (length of residence, clean record, employment), rather than pursuing removal.",
                Category = LawCategory.CrimeJustice,
                Citation = "Several real historical amnesty/regularization programs exist across immigration-receiving countries, not tied to one confirmed law common to the six seeded countries.",
                // DIRECTIONAL/GENRE-IDIOM - several real historical amnesty/regularization programs
                // exist across immigration-receiving countries, but this project isn't citing one
                // single confirmed law common to the six seeded countries the way, say, Poland's
                // border wall is confirmed. Included deliberately as the genuine SWEEPING-lenient
                // counterweight on this dial, opposite the Physical Border Barrier Construction law
                // above - the same both-directions balance every other dial in this catalog already
                // has. MAJOR toward lenient.
                BorderEnforcementDelta = -20f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "human_trafficking_task_force",
                Name = "Human Trafficking Task Force",
                Description = "Funds a dedicated, cross-agency police unit investigating human trafficking and forced-labor networks, coordinating with border authorities on victim identification.",
                Category = LawCategory.CrimeJustice,
                Citation = "Dedicated anti-trafficking task forces are a common institutional response across many countries; the US TVPA framework (2000) is the closest single anchor.",
                // GENRE-IDIOM/DIRECTIONAL - dedicated anti-trafficking task forces are a real,
                // common institutional response across many countries (the US TVPA framework since
                // 2000 is the closest single confirmed anchor, though this law isn't citing that Act
                // specifically), not tied to one single law across all six seeded countries.
                // MODERATE on police funding (the primary lever - a real, standalone unit); MINOR
                // secondary on border enforcement (the victim-identification/interdiction
                // coordination with border authorities).
                PoliceFundingDelta = 10f,
                BorderEnforcementDelta = 6f,
                EnactmentApprovalCost = 0.5f
            },

            // ================================================================================
            // BATCH 4 OF 5 (2026-08-25, resuming post-browser-fix): six laws, 38 -> 44.
            // Deliberately diversified AWAY from SentencingSeverity - the marathon's own close-out
            // composition test found 19 of 38 laws already touch it, the dial most at saturation
            // risk. Of these six, only three touch it at all (two lenient, one severe - still net-
            // balanced), while PoliceFunding gets three genuine primaries and BorderEnforcement,
            // JudicialFunding each get one - the thinnest dials in the pre-batch-4 catalog. One law
            // (the anti-mafia confiscation law) deliberately exercises the wanted-effects log's own
            // pre-authorized "weak proxy, stated explicitly" pattern for a real mechanism (non-
            // conviction asset forfeiture) this six-dial space cannot represent directly.
            // ================================================================================

            new LawDefinition
            {
                Id = "ice_287g_agreements_law",
                Name = "287(g) Immigration Enforcement Agreements",
                Description = "Authorizes state and local police to enter formal agreements with federal immigration authorities, deputizing officers to identify, detain, and process individuals for immigration violations during routine policing.",
                Category = LawCategory.CrimeJustice,
                Citation = "The US 287(g) program (Immigration and Nationality Act Sec. 287(g), added by IIRIRA 1996), formalizing ICE-local police enforcement agreements; participation expanded sharply from the mid-2000s onward.",
                // CONFIRMED - US 287(g) program, added to the Immigration and Nationality Act by the
                // 1996 IIRIRA; dormant for its first several years, then adopted by a growing number
                // of state and local agencies from the mid-2000s on, and expanded further in several
                // later administrations. This is the direct real-world OPPOSITE of this catalog's own
                // Sanctuary City Policy, which bars exactly this cooperation - the same "opposed pair
                // proves the dial swings both ways" shape batch 1's own header established for
                // BailReform/DrugPolicy, applied here to BorderEnforcement's interior-enforcement
                // side. MODERATE primary on border enforcement (a real delegation of federal
                // enforcement authority to local police, not full-scale detention/barrier
                // construction); MINOR secondary on police funding (the federal training and
                // reimbursement support that comes bundled with program participation).
                BorderEnforcementDelta = 12f,
                PoliceFundingDelta = 4f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "hot_spot_policing_program",
                Name = "Hot Spot Policing Program",
                Description = "Concentrates additional patrol presence and targeted enforcement on the small number of locations and individuals responsible for a disproportionate share of violent crime, rather than spreading resources evenly.",
                Category = LawCategory.CrimeJustice,
                Citation = "The Kansas City Gun Experiment (1992-93) and Boston's Operation Ceasefire (1996), among the most consistently evidence-backed policing strategies in criminology (Braga et al. meta-analyses).",
                // CONFIRMED - place-based and focused-deterrence policing, proven out by the Kansas
                // City Gun Experiment (1992-93, a randomized hot-spot patrol trial) and Boston's
                // Operation Ceasefire (1996, a focused-deterrence intervention credited with a sharp
                // youth-homicide drop) - since replicated widely, with Braga et al.'s repeated
                // meta-analyses finding it among the most consistently evidence-backed strategies in
                // policing research (unlike several more contested entries already in this catalog).
                // MODERATE, single-dial: a real, well-evidenced patrol-ALLOCATION strategy, not an
                // across-the-board funding increase, but genuinely felt in the areas it targets.
                PoliceFundingDelta = 10f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "veterans_treatment_courts",
                Name = "Veterans Treatment Courts",
                Description = "Establishes specialized court dockets for justice-involved military veterans, linking eligible defendants to VA benefits, mentorship, and treatment in lieu of standard prosecution.",
                Category = LawCategory.CrimeJustice,
                Citation = "The first veterans treatment court, Buffalo, New York (2008, Judge Robert Russell); several hundred now operate across the US.",
                // CONFIRMED - the first veterans treatment court opened in Buffalo, New York in
                // January 2008 under Judge Robert Russell, explicitly modeled on the existing drug-
                // and mental-health-court diversion shape already in this catalog; several hundred
                // now operate nationally. Same dual-primary shape as Mental Health Diversion Courts
                // for the same real reason (a genuinely specialized docket, not a funding line with
                // an incidental severity side effect): MODERATE reduction in effective severity for
                // the diverted population, MODERATE-low increase in judicial funding for the
                // specialized staffing a dedicated docket requires.
                SentencingSeverityDelta = -5f,
                JudicialFundingDelta = 7f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "financial_crimes_aml_unit",
                Name = "Financial Crimes & Anti-Money-Laundering Unit",
                Description = "Establishes a dedicated police and prosecutorial unit investigating money laundering, large-scale fraud, and illicit financial flows, distinct from digital/cybercrime investigation.",
                Category = LawCategory.CrimeJustice,
                Citation = "The US FinCEN (Financial Crimes Enforcement Network, established 1990) and the EU's Anti-Money Laundering Directives (in force since 1991, tightened repeatedly), both funding dedicated financial-crime investigative capacity.",
                // CONFIRMED - the US Treasury's FinCEN (established 1990) and the EU's own Anti-Money
                // Laundering Directive framework (first adopted 1991, through several later
                // tightenings) both stand up real, dedicated financial-crime investigative capacity.
                // Deliberately kept distinct from this catalog's own Cybercrime Investigation Unit:
                // that law is digital fraud/hacking/exploitation; this one is money laundering and
                // illicit financial flows - a genuinely different investigative discipline, funded
                // and staffed separately in every real jurisdiction cited above. MODERATE primary on
                // police funding (the investigative unit itself); MINOR secondary on judicial funding
                // (the specialized prosecutorial capacity complex financial cases require).
                PoliceFundingDelta = 8f,
                JudicialFundingDelta = 4f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "stand_your_ground_law",
                Name = "Stand Your Ground Law",
                Description = "Removes the legal duty to retreat before using deadly force in self-defense in any place a person is lawfully present, and grants broad immunity from prosecution when the claim is upheld.",
                Category = LawCategory.CrimeJustice,
                Citation = "Florida's 2005 Stand Your Ground law, copied in some form by over 30 US states; associated in multiple peer-reviewed studies with more justifiable-homicide rulings and, in several states, higher homicide rates overall.",
                // CONFIRMED - Florida enacted the first modern "Stand Your Ground" law in 2005,
                // removing the common-law duty to retreat and adding a pretrial immunity hearing;
                // more than 30 US states have adopted some form since. Multiple peer-reviewed studies
                // (e.g. RAND's research synthesis, state-level difference-in-differences work) find
                // more justifiable-homicide rulings and, in several states, a measurable rise in
                // homicide rates following adoption - a real, contested, and measured effect, not a
                // GENRE-IDIOM guess. MODERATE toward lenient: a real, documented reduction in
                // prosecutorial reach for a specific class of violent-crime claims, kept below MAJOR
                // since it applies only where a self-defense claim is actually raised, not a
                // sentencing-code-wide rewrite.
                SentencingSeverityDelta = -10f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "antimafia_asset_confiscation_law",
                Name = "Anti-Mafia Asset Confiscation Law",
                Description = "Empowers courts to seize assets from individuals with proven organized-crime associations even without a criminal conviction, based on a documented mismatch between lawful income and accumulated wealth.",
                Category = LawCategory.CrimeJustice,
                Citation = "Italy's Rognoni-La Torre law (1982), creating the mafia-association offense and non-conviction-based asset confiscation, strengthened repeatedly since (most recently the 2011 Anti-Mafia Code).",
                // CONFIRMED - Italy's Rognoni-La Torre law (1982, passed after the Mafia assassination
                // of its co-author, General Carlo Alberto Dalla Chiesa) created the "mafia
                // association" criminal offense (Art. 416-bis) and non-conviction-based asset
                // confiscation for proven organized-crime wealth; strengthened repeatedly since,
                // consolidated into the 2011 Anti-Mafia Code. This is exactly the case this file's
                // own wanted-effects log names in advance: the real, distinctive mechanism here -
                // seizing assets WITHOUT a conviction, based on an income/wealth mismatch - has no
                // representation in this six-dial space and is not claimed to. PoliceFundingDelta
                // below is a WEAK, honestly-labeled proxy for the law's genuine investigative/asset-
                // tracing capacity increase, not an assertion that funding is the real mechanism.
                // SentencingSeverityDelta is the real, direct, MODERATE effect of the mafia-
                // association offense itself, which the same law actually created and which does
                // carry real prison terms independent of the confiscation power.
                PoliceFundingDelta = 6f,
                SentencingSeverityDelta = 8f,
                EnactmentApprovalCost = 1.0f
            },

            // ================================================================================
            // BATCH 5 OF 5 (2026-08-25): six laws, 44 -> 50, closing the marathon at its original
            // target. Continues batch 4's deliberate move away from SentencingSeverity - only one
            // of these six touches it, as a MINOR secondary. BailReform (the thinnest real dial
            // pre-batch-5, at four laws) gets three here, genuinely opposed (one restrictive, two
            // toward access), the same "opposed pair proves the dial swings both ways" shape as
            // batch 1's own BailReform/DrugPolicy pair and batch 4's own 287(g)/Sanctuary City pair.
            // DrugPolicy and BorderEnforcement each get two; JudicialFunding one. The catalog now
            // sits at the population the marathon's own "saturating composition re-run" (recorded in
            // CLAUDE.md) checks against - see that entry for the validation run itself, not claimed
            // here.
            // ================================================================================

            new LawDefinition
            {
                Id = "federal_bail_reform_preventive_detention_act",
                Name = "Pretrial Preventive Detention Act",
                Description = "Authorizes courts to detain a defendant before trial based on a finding of danger to the community, not flight risk alone - the first legal basis for denying bail on dangerousness grounds rather than only to secure appearance.",
                Category = LawCategory.CrimeJustice,
                Citation = "The US federal Bail Reform Act of 1984, upheld by the Supreme Court in United States v. Salerno (1987), establishing pretrial detention for dangerousness as constitutional.",
                // CONFIRMED - the US federal Bail Reform Act of 1984 was the first federal law to
                // authorize pretrial detention based on a finding of DANGER to the community, not
                // flight risk alone - a structural break from the prior presumption (bail exists only
                // to secure appearance at trial). Upheld against a due-process challenge by the
                // Supreme Court in United States v. Salerno, 481 U.S. 739 (1987). MAJOR toward
                // restrictive: this is the foundational modern basis for the dangerousness-based
                // detention standard this catalog's own Bail Reform Rollback later cites as already
                // being restored in New York - included here as the earlier, structural law that
                // rollback returns TO, not the incremental swing back itself.
                BailReformDelta = -16f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "percentage_bail_deposit_program",
                Name = "Percentage Bail Deposit Program",
                Description = "Lets a defendant pay a fraction (typically 10%) of the court-set bail amount directly to the court to secure release, refunded on appearance, rather than paying a commercial bail bondsman's non-refundable premium.",
                Category = LawCategory.CrimeJustice,
                Citation = "Illinois pioneered the '10 percent bail' cash-deposit program in 1964, since adopted in some form by roughly half of US states.",
                // CONFIRMED - Illinois introduced the "10 percent bail" deposit program in 1964,
                // letting defendants pay a refundable 10% deposit to the court instead of a
                // commercial bondsman's non-refundable premium (typically also ~10%, but never
                // returned); adopted in some form by roughly half the US states since. Genuinely
                // distinct MECHANISM from this catalog's other three bail laws: not a risk-based
                // release standard (Cash Bail Reform Act), not full abolition (Cash Bail Abolition
                // Act), not an algorithmic score (Risk-Based Pretrial Assessment) - this leaves cash
                // bail itself intact and only removes the bondsman's cut. MODERATE toward access.
                BailReformDelta = 9f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "drug_free_zone_sentencing_enhancement",
                Name = "Drug-Free Zone Sentencing Enhancement",
                Description = "Imposes an additional mandatory sentence enhancement for drug offenses committed within a defined distance of a school, park, or other designated protected zone.",
                Category = LawCategory.CrimeJustice,
                Citation = "Widespread US state and federal 'drug-free zone' enhancement laws adopted from the 1970s through the 1990s (New Jersey's 1987 law a frequently cited model; codified federally at 21 U.S.C. Sec. 860).",
                // CONFIRMED - drug-free zone enhancement laws spread across nearly every US state
                // from the 1970s through the 1990s (New Jersey's 1987 law a frequently cited model),
                // with a federal equivalent codified at 21 U.S.C. Sec. 860; real, widely documented
                // criticism (including from the US Sentencing Commission) found the zones so broad in
                // dense urban areas that they cover nearly an entire city, applying the enhancement
                // almost regardless of any actual proximity to children - noted honestly rather than
                // asserting the law worked as designed. MODERATE primary toward punitive on drug
                // policy; MINOR secondary on severity (a real but narrowly-scoped mandatory add-on,
                // not a sentencing-code-wide change).
                DrugPolicyDelta = 10f,
                SentencingSeverityDelta = 5f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "counter_narcotics_interdiction_funding_act",
                Name = "Counter-Narcotics Interdiction Funding Act",
                Description = "Funds joint military and law-enforcement interdiction operations against drug trafficking and production, including cross-border coordination with source and transit countries.",
                Category = LawCategory.CrimeJustice,
                Citation = "Plan Colombia (launched 2000), a joint US-Colombia counter-narcotics and interdiction program totaling roughly $10 billion in US aid over two decades.",
                // CONFIRMED - Plan Colombia, launched in 2000 under the Clinton and Pastrana
                // administrations, funded aerial coca eradication, interdiction operations, and
                // military/police training against drug production and trafficking; roughly $10
                // billion in cumulative US aid over the following two decades. A genuinely dual-dial
                // real law: MODERATE toward punitive on drug policy (interdiction and eradication, the
                // enforcement end of the drug-policy spectrum) and MODERATE on border enforcement (the
                // cross-border interdiction coordination is itself a real border-enforcement
                // mechanism, distinct from Frontex's EU-specific migration focus).
                DrugPolicyDelta = 9f,
                BorderEnforcementDelta = 8f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "national_guard_border_deployment",
                Name = "National Guard Border Deployment",
                Description = "Deploys National Guard troops to support border-patrol operations with surveillance, logistics, and infrastructure work, distinct from a standing physical barrier or federal agency staffing increase.",
                Category = LawCategory.CrimeJustice,
                Citation = "Repeated US National Guard deployments to the southern border (Operation Jump Start 2006, a 2010 deployment, Operation Guardian Support 2018-19), plus state-level deployments such as Texas's Operation Lone Star since 2021.",
                // CONFIRMED - the US has repeatedly deployed National Guard troops to the US-Mexico
                // border in a support (not law-enforcement-authority) role: Operation Jump Start
                // (2006, Bush), a 2010 deployment (Obama), and Operation Guardian Support (2018-19,
                // Trump), plus state-funded deployments such as Texas's Operation Lone Star since
                // 2021. Genuinely distinct from this catalog's Physical Border Barrier Construction
                // (infrastructure, not personnel) and its Frontex Border Cooperation Agreement
                // (EU-specific standing agency, not a domestic troop deployment). MODERATE: real and
                // recurring, but each deployment is a temporary support surge, not a standing
                // structural change to border-enforcement capacity the way a permanent barrier or
                // agency expansion is.
                BorderEnforcementDelta = 10f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "pretrial_services_agency_establishment",
                Name = "Pretrial Services Agency Establishment",
                Description = "Establishes a dedicated agency to supervise and support defendants released before trial - court-date reminders, check-ins, and referrals - giving judges a real supervised-release option instead of a binary cash-or-jail choice.",
                Category = LawCategory.CrimeJustice,
                Citation = "The US federal Pretrial Services Act of 1982, establishing a pretrial services agency in every federal district, building on the pioneering DC Pretrial Services Agency (established 1968).",
                // CONFIRMED - the DC Pretrial Services Agency (established 1968, the first of its
                // kind) demonstrated supervised pretrial release as a real alternative to cash bail or
                // detention; the federal Pretrial Services Act of 1982 then established an equivalent
                // agency in every US federal judicial district. Genuinely distinct from this
                // catalog's Risk-Based Pretrial Assessment (that law is the ALGORITHMIC SCORE used to
                // decide release; this one is the SUPERVISION INFRASTRUCTURE that makes a release
                // decision practically workable once made) - a real law can and did precede the
                // scoring tools by decades. MODERATE primary on judicial funding (the agency itself);
                // MINOR secondary toward bail access (judges have a real supervised-release option to
                // point to instead of defaulting to cash bail).
                JudicialFundingDelta = 8f,
                BailReformDelta = 5f,
                EnactmentApprovalCost = 0.5f
            },

            // ── LABOR MARKET BATCH 1 (pass 3, 2026-08-26): 10 laws, 50 -> 60 ──
            // Charter: open the second category with its two flagship dials at full documented
            // depth. MinimumWage gets FOUR laws with both directions proven (the UK Wages
            // Councils abolition is the real down direction) and Overtime a genuinely opposed
            // pair (EU Working Time Directive vs the El Khomri loosening). PaidFamilyLeave gets
            // its two landmark real shapes (a first payroll-insurance mandate; a Nordic-scale
            // build-out) - both positive, stated honestly: real statutory-leave ROLLBACKS proved
            // rare enough that no clean citation was found, so none is invented (rule 5).
            // Retraining and Immigration get one clean primary each; FamilyPolicy enters only as
            // an honest secondary (Elterngeld's own explicitly pro-natalist framing). Batch 2
            // rebalances toward the demographic dials this batch leaves thin. Magnitudes cite the
            // per-dial scale ruling: MinimumWage deltas are KAITZ POINTS tiered at x2, PaidLeave
            // deltas are WEEKS at x1 (see DialMagnitudeScales).
            new LawDefinition
            {
                Id = "raise_the_wage_act",
                Name = "Federal Minimum Wage Increase Act",
                Description = "Roughly doubles the statutory minimum wage in annual steps, lifting the wage floor from the bottom quarter of the wage distribution toward 60% of the median wage.",
                Category = LawCategory.LaborMarket,
                Citation = "The US Raise the Wage Act (House-passed 2019, reintroduced 2021/2023), phasing the federal minimum to $15/hr - roughly doubling the federal Kaitz index - with the CBO's 2019 median estimates of ~1.3M jobs cost and ~1.3M people lifted from poverty.",
                // CONFIRMED - the flagship contested labor bill of the era. SWEEPING primary
                // (+16 Kaitz points; x2 scale -> 32): the USA's seeded Kaitz is 29, and the CBO's
                // modeled $15 federal floor lands the effective index near ~45 - this delta IS
                // that documented jump, not a chosen number. The employment effect the pane
                // derives is honestly (contested) - Card/Krueger vs the neoclassical consensus is
                // THE canonical labor-economics dispute, and the CBO's own range spans "about
                // zero" to 2.7M jobs. Inert for Sweden/Italy (no statutory minimum wage -
                // MinimumWageImplemented false; the recompute, direction and pane all gate on it).
                MinimumWageDelta = 16f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "minimum_wage_indexation_act",
                Name = "Minimum Wage Indexation Act",
                Description = "Ties annual minimum-wage upratings to median-wage growth by statutory formula, ending ad-hoc political uprating rounds.",
                Category = LawCategory.LaborMarket,
                Citation = "France's SMIC statutory indexation formula (price- and wage-linked, with discretionary coups de pouce on top) and US state indexation amendments (Florida 2004, Colorado 2006).",
                // CONFIRMED - indexation is a real, widespread mechanism. MINOR (+3 Kaitz; x2 ->
                // 6): a formula locks in modest upward real drift versus a nominally frozen floor
                // that erodes - the delta prices the drift a formula protects, not a headline
                // raise. The +3 is this project's own judgment of that protected drift's scale
                // (stated as judgment, not dressed as research).
                MinimumWageDelta = 3f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "subminimum_wage_abolition_act",
                Name = "Subminimum Wage Abolition Act",
                Description = "Phases out tipped, youth and disability subminimum wages so the full statutory floor applies to every covered worker.",
                Category = LawCategory.LaborMarket,
                Citation = "Washington DC's Initiative 82 (2022), phasing out the tipped minimum wage by 2027; seven US states already apply the full minimum to tipped workers.",
                // CONFIRMED - a real, recurring reform with a named enacted instance. MODERATE
                // (+4 Kaitz; x2 -> 8): abolishing subminimums raises the EFFECTIVE economy-wide
                // wage floor for the covered groups - real and felt, but smaller than a headline
                // rate change since it reaches subsets of low-wage workers.
                MinimumWageDelta = 4f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "wage_floor_restraint_act",
                Name = "Wage Floor Restraint Act",
                Description = "Freezes the nominal minimum wage and narrows its sectoral coverage, letting the floor erode against the median wage over time.",
                Category = LawCategory.LaborMarket,
                Citation = "The UK's abolition of the Wages Councils (Trade Union Reform and Employment Rights Act 1993), removing sectoral minimum-wage floors entirely until the 1998 National Minimum Wage Act restored a statutory floor.",
                // CONFIRMED - the dial's real down direction, proven by an enacted national
                // rollback. MODERATE (-6 Kaitz; x2 -> 12): the 1993-1998 UK window is the
                // documented case of a wage floor eroding by policy choice; the magnitude is a
                // judgment sized to a multi-year freeze-plus-narrowing, well short of full
                // abolition (which would be the dial's floor, not a delta).
                MinimumWageDelta = -6f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "paid_family_leave_insurance_act",
                Name = "Paid Family Leave Insurance Act",
                Description = "Creates a payroll-funded insurance program paying about twelve weeks of wage replacement for new parents and family caregivers.",
                Category = LawCategory.LaborMarket,
                Citation = "The US FAMILY Act model and the enacted state programs it generalizes (California 2002, first in the nation; New Jersey, New York, Washington) - all roughly 8-12 weeks, payroll-insurance funded.",
                // CONFIRMED - the standard first-mandate shape. MODERATE (+12 weeks; x1 - the
                // class doc's own FMLA-scale calibration case): for the USA (baseline 0 weeks)
                // this is the from-zero mandate the FAMILY Act proposes; for a high-baseline
                // country the same +12 reads as an incremental build-out - a delta is honest in
                // both readings. The LFPR and approval effects the pane derives ride the model's
                // own paid-leave couplings.
                PaidFamilyLeaveWeeksDelta = 12f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "parental_leave_expansion_act",
                Name = "Parental Leave Expansion Act",
                Description = "Extends paid parental leave toward a Nordic-scale entitlement, with months reserved for each parent to equalize uptake.",
                Category = LawCategory.LaborMarket,
                Citation = "Sweden's 480-day parental insurance with three reserved months per parent (2016) and Germany's Elterngeld reform (2007), the explicit redesign of parental benefits as pro-natalist family policy.",
                // CONFIRMED - two landmark systems, one shape. SWEEPING primary (+26 weeks; x1 -
                // half a year of additional entitlement, the Nordic-scale build-out the class
                // doc's calibration names). MINOR secondary FamilyPolicyDelta +5: Elterngeld's
                // own framing was explicitly natalist family policy, so a small family-policy
                // component is the honest reading of the real law, not a bundled guess.
                PaidFamilyLeaveWeeksDelta = 26f,
                FamilyPolicyDelta = 5f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "working_time_regulation_act",
                Name = "Working Time Regulation Act",
                Description = "Caps average weekly working hours, mandates daily and weekly rest periods, and sets a paid annual leave minimum.",
                Category = LawCategory.LaborMarket,
                Citation = "The EU Working Time Directive (2003/88/EC): a 48-hour average weekly cap, daily/weekly rest requirements, and four weeks' paid annual leave.",
                // CONFIRMED - the canonical working-time statute. MODERATE (+10): a real, felt
                // strictness shift with defined scope - short of the French 35-hour framework's
                // reach (which would grade MAJOR on this dial). The unemployment effect the pane
                // derives is honestly (contested) via the table's work-sharing caveat.
                OvertimeRegulationDelta = 10f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "working_hours_deregulation_act",
                Name = "Working Hours Deregulation Act",
                Description = "Loosens statutory hour caps and lets firm-level agreements override sectoral working-time rules on overtime terms.",
                Category = LawCategory.LaborMarket,
                Citation = "France's El Khomri labor law (2016), letting company-level accords set overtime terms below branch agreements - the loosening of the 35-hour framework that drove the Nuit debout protests.",
                // CONFIRMED - the opposed pair's other half, an enacted national loosening.
                // MODERATE (-9): the El Khomri reform inverted the norm hierarchy for working
                // time without abolishing the caps themselves - a real, felt deregulation short
                // of removing the framework (which would grade MAJOR/SWEEPING downward).
                OvertimeRegulationDelta = -9f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "active_labor_market_programs_act",
                Name = "Active Labour Market Programs Act",
                Description = "Funds job-search assistance, retraining guarantees and activation requirements at the scale of the strongest real systems.",
                Category = LawCategory.LaborMarket,
                Citation = "Denmark's flexicurity active-labor-market system (~2% of GDP on active measures, the OECD's highest) and Germany's Hartz III/IV job-center activation reforms (2003-05).",
                // CONFIRMED - two named systems define the shape. MODERATE (+12): a
                // flexicurity-scale ALMP build-out is a substantial, defined program expansion -
                // real spending and real activation rules - but the dial is a 0-100 abstraction
                // with no per-country seed, so the tier, not the number, is the claim. Funding-
                // and-program approval tier (0.5) per the catalog's own cost conventions.
                RetrainingProgramDelta = 12f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "skilled_worker_immigration_act",
                Name = "Skilled Worker Immigration Act",
                Description = "Opens points-tested work visas, eases credential recognition and lowers salary thresholds for skilled migrants.",
                Category = LawCategory.LaborMarket,
                Citation = "Germany's Fachkraefteeinwanderungsgesetz (2020, expanded 2023 with the Chancenkarte points card) and the EU Blue Card framework it builds on.",
                // CONFIRMED - a named, twice-legislated national opening. MODERATE (+9): a real
                // widening of labor migration channels for a defined population (skilled
                // workers), well short of open-borders scale on a 0-100 openness dial. The
                // migration and (via the existing migration-gap term) LFPR effects the pane
                // derives ride the model's own single-channel design.
                ImmigrationPolicyDelta = 9f,
                EnactmentApprovalCost = 1.0f
            },

            // ── LABOR MARKET BATCH 2 (pass 3, 2026-08-26): 10 laws, 60 -> 70 ──
            // Charter: rebalance toward the demographic dials batch 1 left thin - FamilyPolicy
            // goes 0 -> 4 primaries (with a real DOWN direction: the UK two-child limit) and
            // ImmigrationPolicy 1 -> 4 with a genuinely opposed pair (the EU temporary-protection
            // opening vs the Danish paradigm shift). Retraining gains its apprenticeship and
            // individual-account shapes; Overtime gains the shorter-week MINOR. The PaidLeave
            // DOWN-direction search stayed dry a second time (real statutory-leave rollbacks:
            // still no clean citation) - recorded again, not invented.
            new LawDefinition
            {
                Id = "universal_child_benefit_act",
                Name = "Universal Child Benefit Act",
                Description = "Pays a flat monthly benefit per child to every family, unconditionally, as an explicit pro-natalist and child-poverty measure.",
                Category = LawCategory.LaborMarket,
                Citation = "Poland's Rodzina 500+ program (2016, raised to 800+ in 2024): a flat monthly per-child payment, explicitly pro-natalist in its framing, one of Europe's largest family-benefit expansions.",
                // CONFIRMED - a named, enacted flagship. MODERATE (+12): 500+ was a genuinely
                // large reorientation of family policy (~1.5% of GDP at launch), short of the
                // total-reorientation SWEEPING tier. Its documented fertility effect was small -
                // which is exactly what the model's own deliberately-small BirthRate coupling
                // (+/-1.5 at dial extremes) already encodes; the delta grades the POLICY size,
                // the coupling grades the outcome.
                FamilyPolicyDelta = 12f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "universal_childcare_act",
                Name = "Universal Childcare Act",
                Description = "Guarantees subsidized childcare places at a low flat parent fee, universally rather than means-tested.",
                Category = LawCategory.LaborMarket,
                Citation = "Quebec's $5-a-day universal childcare (1997, the canonical natural experiment for maternal labor-supply effects) and Sweden's maxtaxa fee cap (2002).",
                // CONFIRMED - two named systems. MODERATE (+10) on FamilyPolicy. Cross-category
                // tension stated honestly: childcare's best-documented effect is maternal LABOR
                // SUPPLY, and in this model that channel belongs to WelfareProgramType.
                // ChildcareSubsidies (the unemployment-reversion bonus) - this law presets the
                // family-policy STANCE (its BirthRate channel), it does not duplicate the welfare
                // program's labor channel. The same one-variable-one-channel discipline the
                // immigration lever records.
                FamilyPolicyDelta = 10f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "child_tax_credit_expansion_act",
                Name = "Child Tax Credit Expansion Act",
                Description = "Expands the per-child tax credit, makes it fully refundable and pays it monthly, reaching the lowest-income families for the first time.",
                Category = LawCategory.LaborMarket,
                Citation = "The US American Rescue Plan's 2021 Child Tax Credit expansion ($3,000-3,600, fully refundable, paid monthly July-December 2021), which roughly halved measured child poverty while in force.",
                // CONFIRMED - enacted, measured, and lapsed (the lapse is the political story, not
                // this law's). MODERATE (+7): a large one-parameter expansion of an existing
                // instrument - real and felt, below the create-a-new-program tier of the two laws
                // above. Funding-tier cost (0.5) - it passed inside a broader package.
                FamilyPolicyDelta = 7f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "family_benefit_retrenchment_act",
                Name = "Family Benefit Retrenchment Act",
                Description = "Caps means-tested family support at two children and freezes child-benefit rates, retrenching family policy for fiscal savings.",
                Category = LawCategory.LaborMarket,
                Citation = "The UK's two-child limit on Child Tax Credit/Universal Credit (announced 2015, in force 2017) plus the 2010s child-benefit freezes - the clearest recent retrenchment of a rich-country family-benefit system.",
                // CONFIRMED - the dial's real DOWN direction, enacted and still in force. MODERATE
                // (-8): a coverage cap plus rate freeze retrenches genuinely but leaves the
                // benefit architecture standing (abolition would grade deeper). The batch
                // charter's point: this dial swings both ways under real policy.
                FamilyPolicyDelta = -8f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "humanitarian_admissions_expansion_act",
                Name = "Humanitarian Admissions Expansion Act",
                Description = "Opens group-based humanitarian admission with immediate work rights, bypassing case-by-case asylum queues for a designated displaced population.",
                Category = LawCategory.LaborMarket,
                Citation = "The EU's first-ever activation of the Temporary Protection Directive (March 2022) for people fleeing Ukraine: immediate residence and LABOR MARKET ACCESS across the bloc, ~4 million registrations within a year.",
                // CONFIRMED - the largest opening of European labor-market access to a displaced
                // population in the modern era (Germany's 2015 opening is the directional
                // precedent, but 2022's is the statutory instrument). MODERATE (+11) on the
                // openness dial: group-based admission with work rights is a real, large widening
                // - short of open-borders scale. Hottest political tier (1.5).
                ImmigrationPolicyDelta = 11f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "immigration_restriction_act",
                Name = "Immigration Restriction Act",
                Description = "Tightens asylum criteria, raises income and language thresholds for residence, and ends preferential regional free-movement admission.",
                Category = LawCategory.LaborMarket,
                Citation = "Denmark's 2015-2019 restrictive 'paradigm shift' (temporary-protection-first asylum, the 2016 L87 tightening package) and the UK's post-Brexit points system ending EU free movement (2021) - two enacted national restrictions.",
                // CONFIRMED - the opposed pair's other half, twice over. MODERATE (-12): ending a
                // free-movement channel plus systematic asylum tightening is a real, large
                // narrowing of the openness dial, short of a closed-borders reorientation.
                // Hottest political tier (1.5), same as its opposite - restriction and opening
                // are both flashpoint politics.
                ImmigrationPolicyDelta = -12f,
                EnactmentApprovalCost = 1.5f
            },
            new LawDefinition
            {
                Id = "seasonal_guest_worker_program_act",
                Name = "Seasonal Guest Worker Program Act",
                Description = "Creates capped, employer-sponsored seasonal work visas for agriculture and tourism, with mandatory return and no settlement track.",
                Category = LawCategory.LaborMarket,
                Citation = "The US H-2A/H-2B seasonal visa programs and Germany's bilateral seasonal-worker agreements (the 1990s Polish agreements; the 2020 harvest-worker exceptions) - the standard bounded-opening shape.",
                // CONFIRMED - a recurring, deliberately bounded instrument. MINOR (+5): capped,
                // sector-specific, non-settlement admission is the openness dial's smallest real
                // positive step - the point of the design is smallness. Uncontroversial
                // funding-and-program tier (0.5).
                ImmigrationPolicyDelta = 5f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "apprenticeship_system_act",
                Name = "Apprenticeship System Act",
                Description = "Establishes a statutory dual apprenticeship system - firm-based training with wage subsidies paired with vocational schooling and recognized credentials.",
                Category = LawCategory.LaborMarket,
                Citation = "Germany's Berufsbildungsgesetz (1969, modernized 2005/2020), the statute behind the dual vocational system routinely credited for Germany's low youth unemployment; Switzerland's VET law is the sibling case.",
                // CONFIRMED - a named statute behind the world's benchmark system. MODERATE (+9):
                // building a statutory dual system is a substantial, defined training-capacity
                // reform on the 0-100 retraining dial - the flexicurity-scale ALMP act (batch 1)
                // sits just above it because it bundles activation with training. Funding tier
                // (0.5).
                RetrainingProgramDelta = 9f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "lifelong_learning_accounts_act",
                Name = "Lifelong Learning Accounts Act",
                Description = "Gives every worker an individual, portable training account credited annually, spendable on accredited courses at their own initiative.",
                Category = LawCategory.LaborMarket,
                Citation = "France's Compte Personnel de Formation (2014, monetized in euros 2018) and Singapore's SkillsFuture credits (2015) - the two flagship individual-training-account systems.",
                // CONFIRMED - two named systems, one instrument shape. MINOR (+6): individual
                // accounts widen ACCESS to training without building delivery capacity the way
                // the apprenticeship statute or an ALMP build-out does - the smallest real
                // positive step on this dial, and honestly so (French take-up skews toward
                // short/driving-license courses, a documented caveat). Funding tier (0.5).
                RetrainingProgramDelta = 6f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "shorter_workweek_pilot_act",
                Name = "Shorter Workweek Pilot Act",
                Description = "Grants public-sector workers a right to reduced weekly hours at full pay and funds matched private-sector trials of a shorter week.",
                Category = LawCategory.LaborMarket,
                Citation = "For Iceland's 2015-2019 public-sector trials (which led ~86% of the workforce to gain reduced-hours rights by 2021); as a general statutory instrument, the UK's 2022 four-day-week pilot was large but private and voluntary.",
                // Hybrid label, honestly split: Iceland's outcome is documented; a general
                // statutory shorter week is still a live debate, not an enacted national norm.
                // MINOR (+6) on working-time strictness: rights-to-reduce plus pilots move the
                // dial genuinely but stop well short of the Working Time Regulation Act's
                // economy-wide caps. Funding tier (0.5).
                OvertimeRegulationDelta = 6f,
                EnactmentApprovalCost = 0.5f
            },

            // ── LABOR MARKET BATCH 3 (pass 3, 2026-08-26): 10 laws, 70 -> 80 ──
            // Charter: deepen the leave and working-time dials with their real second shapes
            // (paternity equalization, quotas, disconnection rights, the UK opt-out as a real
            // DOWN direction), land the EU minimum-wage directive, and - the C&J batch-3
            // precedent - add genuinely MULTI-DIAL laws to exercise composition beyond simple
            // pairs, including the labor category's first use of the wanted-effects log's
            // pre-authorized "weak proxy, stated explicitly" pattern (flexicurity's EPL core is
            // unrepresentable; only the representable remainder is authored).
            new LawDefinition
            {
                Id = "paternity_leave_equalization_act",
                Name = "Paternity Leave Equalization Act",
                Description = "Raises paid paternity leave to full parity with maternity leave, non-transferable between parents and paid at full replacement rate.",
                Category = LawCategory.LaborMarket,
                Citation = "Spain's 2021 equalization of paternity and maternity leave at 16 weeks each, non-transferable, 100% pay - the first large economy to reach full parity.",
                // CONFIRMED - a named, enacted parity reform. MODERATE (+8 weeks): Spain's path
                // added ~11 paternity weeks over 2017-2021; +8 grades the parity step itself, a
                // real expansion of the household's total paid entitlement smaller than the
                // Nordic build-out (SWEEPING +26).
                PaidFamilyLeaveWeeksDelta = 8f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "parental_quota_act",
                Name = "Parental Leave Quota Act",
                Description = "Reserves a use-it-or-lose-it share of the parental leave entitlement for each parent, modestly extending the total to fund the reserved weeks.",
                Category = LawCategory.LaborMarket,
                Citation = "Norway's 1993 'daddy quota' (the first reserved-weeks scheme, now 15 weeks per parent), the model Sweden's and Iceland's reserved months follow.",
                // CONFIRMED - the instrument that invented reserved leave. MINOR (+4 weeks): a
                // quota mostly REALLOCATES existing entitlement; the delta prices only the modest
                // extension that funds the reserved share - grading the reallocation as a large
                // week-count would double-count entitlement the household already had.
                PaidFamilyLeaveWeeksDelta = 4f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "right_to_disconnect_act",
                Name = "Right to Disconnect Act",
                Description = "Obliges employers above a size threshold to negotiate binding rules on out-of-hours contact and email, with working-time enforcement behind them.",
                Category = LawCategory.LaborMarket,
                Citation = "France's right-to-disconnect provision (Article 55 of the 2016 El Khomri law, in force 2017), the first national disconnection statute, since echoed in Belgium and Portugal.",
                // CONFIRMED - a named first-in-kind statute. MINOR (+5): disconnection rules
                // extend working-time protection into a new margin (availability) without
                // touching the headline hour caps - a narrow, real strictness step. Note the
                // honest irony the citation carries: it rode the same El Khomri law whose
                // overtime provisions this catalog codes as deregulation - one real statute,
                // two directions, two laws here.
                OvertimeRegulationDelta = 5f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "working_time_opt_out_act",
                Name = "Working Time Opt-Out Act",
                Description = "Lets individual workers sign away the statutory weekly hours cap, making the ceiling advisory wherever employer and worker agree.",
                Category = LawCategory.LaborMarket,
                Citation = "The UK's blanket use of the Working Time Directive's Article 22 individual opt-out from the 48-hour cap, the standing exception the European Parliament repeatedly (and unsuccessfully) voted to phase out.",
                // CONFIRMED - a real, standing, contested carve-out. MINOR (-6): an individual
                // opt-out hollows the cap's bindingness without repealing the framework - the
                // El Khomri-style norm-hierarchy inversion (-9 MODERATE) cuts deeper because it
                // moves the default, not just the exception. Standard contested tier (1.0) -
                // this fight ran for two decades in Brussels.
                OvertimeRegulationDelta = -6f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "adequate_minimum_wage_directive_act",
                Name = "Adequate Minimum Wage Directive Act",
                Description = "Commits statutory minimum-wage setting to an adequacy framework benchmarked at 60% of the median wage, with regular reference-tested upratings.",
                Category = LawCategory.LaborMarket,
                Citation = "The EU Adequate Minimum Wages Directive (2022/2041): a 60%-of-median indicative adequacy reference for members with statutory floors, plus a collective-bargaining-coverage pillar for those without.",
                // CONFIRMED - the directive is real and in force (transposition due 2024).
                // MODERATE (+5 Kaitz; x2 -> 10): an adequacy framework pulls a below-reference
                // floor toward 60% of median over successive upratings - real convergence
                // pressure, not a single headline jump. Honestly inert for Sweden/Italy here
                // TWICE over: they have no statutory floor for the framework to bind (the
                // model's gate), and the directive's OTHER pillar - bargaining-coverage action
                // plans - is exactly the unionization axis the labor wanted-effects log records
                // as having no dial. Stated, not proxied.
                MinimumWageDelta = 5f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "trade_adjustment_assistance_act",
                Name = "Trade Adjustment Assistance Act",
                Description = "Funds extended retraining, income support during training, and relocation allowances for workers displaced by trade competition.",
                Category = LawCategory.LaborMarket,
                Citation = "The US Trade Adjustment Assistance program (Trade Expansion Act 1962, expanded 1974/2002/2015, lapsed 2022) - the canonical trade-displacement retraining instrument.",
                // CONFIRMED - six decades of enacted history. MINOR (+5): TAA is real but
                // narrow - it reaches certified trade-displaced workers only, a targeted slice
                // of the retraining dial next to the economy-wide ALMP and apprenticeship
                // statutes above it. Funding tier (0.5).
                RetrainingProgramDelta = 5f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "citizenship_modernization_act",
                Name = "Citizenship Modernization Act",
                Description = "Permits dual citizenship, shortens naturalization residence requirements, and eases the path for the second generation born in-country.",
                Category = LawCategory.LaborMarket,
                Citation = "Germany's citizenship modernization law (in force June 2024): general dual citizenship and naturalization after five years (three with exceptional integration), replacing the renunciation requirement.",
                // CONFIRMED - a named, just-enacted reform. MINOR (+6) on the openness dial:
                // citizenship terms shape long-run settlement attractiveness and integration
                // rather than admission volume itself - a real but indirect widening, graded at
                // the dial's small tier. Standard contested tier (1.0).
                ImmigrationPolicyDelta = 6f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "family_housing_support_act",
                Name = "Family Housing Support Act",
                Description = "Grants escalating housing subsidies and loan forgiveness per child, tying family-formation support to explicit natalist targets.",
                Category = LawCategory.LaborMarket,
                Citation = "Hungary's CSOK housing-subsidy scheme (2015) and the 2019 Family Protection Action Plan (loan forgiveness per child, lifetime income-tax exemption for mothers of four) - the era's most aggressive natalist package.",
                // CONFIRMED - named, enacted, explicitly natalist. MODERATE (+9): a large,
                // multi-instrument commitment - short of SWEEPING because it reshapes incentives
                // within the existing family-policy architecture rather than reorienting it.
                // The housing INSTRUMENT is cross-category (the declined housing pass owns
                // zoning/stock dials); the family-policy STANCE is the honest in-category
                // component this delta grades - stated, per the reentry-programs precedent in
                // the C&J log.
                FamilyPolicyDelta = 9f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "flexicurity_package_act",
                Name = "Flexicurity Package Act",
                Description = "Adopts the flexicurity triangle: easier hiring and dismissal, generous transitional support, and guaranteed retraining for every displaced worker.",
                Category = LawCategory.LaborMarket,
                Citation = "Denmark's flexicurity model (the 1990s Rasmussen-era labor reforms), the OECD's standing reference case for combining flexible dismissal rules with strong active support.",
                // CONFIRMED as a model; THE WEAK-PROXY PATTERN, exercised in labor for the first
                // time (the wanted-effects log's own pre-authorized rule): flexicurity's CORE is
                // employment-protection loosening, and EPL has NO dial in this model (logged
                // axis) - so the deltas below are the representable REMAINDER only, stated
                // explicitly, never a claim that retraining is the mechanism dismissal reform
                // works through. MODERATE primary RetrainingProgramDelta +8 (the guaranteed-
                // retraining leg is real and directly on-dial); MINOR secondary
                // OvertimeRegulationDelta -4 (the flexibility leg's working-time component).
                RetrainingProgramDelta = 8f,
                OvertimeRegulationDelta = -4f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "demographic_response_package_act",
                Name = "Demographic Response Package Act",
                Description = "Bundles family benefits, leave expansion and managed labor migration into one statutory response to workforce aging.",
                Category = LawCategory.LaborMarket,
                Citation = "The aging-response packages every fast-aging economy now legislates in some form: Japan's Children's Future Strategy (2023, 3.6T yen), Germany's Demografiestrategie framework - a real policy genre, not one single statute.",
                // DIRECTIONAL - the genre is real, the bundle is this catalog's own composition.
                // A genuinely TRIPLE-DIAL law (the batch charter's composition exercise):
                // MODERATE primary FamilyPolicyDelta +7 (the benefit leg), MINOR secondaries
                // ImmigrationPolicyDelta +5 (the managed-migration leg) and
                // PaidFamilyLeaveWeeksDelta +4 (the leave leg). Tiers stated per delta; the
                // package's breadth, not any single leg's size, is its point.
                FamilyPolicyDelta = 7f,
                ImmigrationPolicyDelta = 5f,
                PaidFamilyLeaveWeeksDelta = 4f,
                EnactmentApprovalCost = 1.0f
            },

            // ── LABOR MARKET BATCH 4 (pass 3, 2026-08-26): 10 laws, 80 -> 90 ──
            // Charter: diversify AWAY from the dials the running composition puts nearest their
            // ceilings (FamilyPolicy and Retraining both sit above +90 on the USA all-enacted
            // sum after batch 3 - the C&J batch-4 lesson applied before, not after, saturation).
            // This batch is deliberately MINOR-heavy: the taxonomy's administrative tier is
            // where most real labor law actually lives (conventions, carve-outs, enforcement
            // funding, visa classes), and the catalog should read that way. Down-directions on
            // three dials keep the sums honest; FamilyPolicy gets zero laws.
            new LawDefinition
            {
                Id = "youth_minimum_wage_act",
                Name = "Youth Minimum Wage Act",
                Description = "Introduces reduced statutory minimum-wage rates for workers under 21, stepped by age, to price young entrants into their first jobs.",
                Category = LawCategory.LaborMarket,
                Citation = "The Netherlands' statutory youth minimum wage (age-stepped rates from 15 to the adult floor at 21, softened in 2017/2019 but standing), the canonical differentiated-floor design.",
                // CONFIRMED - a standing national design. MINOR (-3 Kaitz; x2 -> 6): an age
                // carve-out lowers the EFFECTIVE economy-wide floor modestly - the mirror of the
                // subminimum-abolition act above, and the honest small shape of the dial's down
                // direction (the Wages Councils rollback stays the bigger one). Contested tier
                // (1.0) - youth subminimums are a live fairness fight.
                MinimumWageDelta = -3f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "living_wage_procurement_act",
                Name = "Living Wage Procurement Act",
                Description = "Requires government contractors and subsidy recipients to pay a living-wage rate above the statutory floor.",
                Category = LawCategory.LaborMarket,
                Citation = "Baltimore's 1994 living-wage ordinance (the first) and the ~140 US city/county ordinances that followed; the UK's public-sector London Living Wage adoption is the sibling case.",
                // CONFIRMED - a thirty-year enacted family. MINOR (+3 Kaitz; x2 -> 6): procurement
                // coverage raises the effective floor for the contractor workforce only - real,
                // narrow, exactly the administrative tier. Funding tier (0.5).
                MinimumWageDelta = 3f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "wage_theft_enforcement_act",
                Name = "Wage Theft Enforcement Act",
                Description = "Funds wage-and-hour inspection capacity, adds treble damages for unpaid wages, and lets regulators pursue violations without a worker complaint.",
                Category = LawCategory.LaborMarket,
                Citation = "California's wage-theft statutes (criminalization 2021, the Private Attorneys General Act) and repeated US DOL Wage and Hour Division enforcement expansions.",
                // CONFIRMED - named instruments. MINOR (+2 Kaitz; x2 -> 4): enforcement raises
                // the floor workers actually RECEIVE toward the floor the statute already
                // promises - a real effective-wage effect, the smallest on this dial. Funding
                // tier (0.5).
                MinimumWageDelta = 2f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "maternity_protection_act",
                Name = "Maternity Protection Act",
                Description = "Guarantees a minimum of fourteen weeks' paid maternity leave with dismissal protection and health safeguards, per the international standard.",
                Category = LawCategory.LaborMarket,
                Citation = "ILO Maternity Protection Convention C183 (2000): a 14-week paid-leave minimum with cash benefits and dismissal protection, ratified by 43 states.",
                // CONFIRMED - the international floor itself. MINOR (+6 weeks): ratifying the
                // C183 floor is a real but bounded step - most of this model's countries already
                // exceed it (the delta reads as topping-up and hardening protection), and for a
                // zero-baseline country it is the minimal international-standard entry, well
                // below the FAMILY-Act-scale mandate (+12 MODERATE). Funding tier (0.5).
                PaidFamilyLeaveWeeksDelta = 6f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "carers_leave_act",
                Name = "Carers' Leave Act",
                Description = "Adds short statutory paid leave for workers caring for sick relatives, plus protected paternity days at birth.",
                Category = LawCategory.LaborMarket,
                Citation = "The EU Work-Life Balance Directive (2019/1158): five days' carers' leave a year and ten working days' paternity leave, transposition due 2022.",
                // CONFIRMED - a named directive in force. MINOR (+3 weeks): days-scale
                // entitlements summed across the new categories - the smallest real expansion
                // shape on this dial. Funding tier (0.5).
                PaidFamilyLeaveWeeksDelta = 3f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "night_work_restriction_act",
                Name = "Night Work Restriction Act",
                Description = "Caps average night-shift hours, mandates health assessments for night workers, and grants transfer rights to day work on medical grounds.",
                Category = LawCategory.LaborMarket,
                Citation = "The EU Working Time Directive's night-work provisions (8-hour average cap, health assessments) and ILO Night Work Convention C171 (1990).",
                // CONFIRMED - standing international and EU law. MINOR (+4): a real strictness
                // step on one margin of working time, inside the WTD framework the MODERATE act
                // above establishes wholesale. Funding tier (0.5).
                OvertimeRegulationDelta = 4f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "annualized_hours_act",
                Name = "Annualized Hours Act",
                Description = "Lets working-time limits average over a full year by agreement, trading weekly caps for seasonal flexibility.",
                Category = LawCategory.LaborMarket,
                Citation = "The Working Time Directive's own 12-month averaging reference period via collective agreement, and the French/German annualization accords (modulation du temps de travail) built on it.",
                // CONFIRMED - a standard enacted flexibility instrument. MINOR (-5): annualization
                // keeps the caps but hollows their week-by-week bite - a smaller loosening than
                // the opt-out (-6) since the annual ceiling still binds. Funding tier (0.5) -
                // routinely agreed, rarely a flashpoint.
                OvertimeRegulationDelta = -5f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "workfare_activation_act",
                Name = "Workfare Activation Act",
                Description = "Conditions out-of-work benefits on mandatory job search, training participation and work placements, with sanctions for refusal.",
                Category = LawCategory.LaborMarket,
                Citation = "Wisconsin Works (1996, the US workfare template) and the UK's Universal Credit conditionality regime - benefit conditionality with mandatory activation.",
                // CONFIRMED - enacted twice over. THE WEAK-PROXY RULE, second labor use, stated:
                // workfare's CORE is benefit conditionality, and benefit rules have no dial
                // (unemployment insurance is the non-player automatic stabilizer - the
                // wanted-effects log's own entry). Only the mandatory-TRAINING leg is
                // representable: MINOR (+4) on Retraining, explicitly not a claim that
                // conditionality works through training capacity. Contested tier (1.0).
                RetrainingProgramDelta = 4f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "remote_work_visa_act",
                Name = "Remote Work Visa Act",
                Description = "Creates a residence visa for foreign employees of foreign firms working remotely, with income thresholds and no local labor-market test.",
                Category = LawCategory.LaborMarket,
                Citation = "Estonia's digital-nomad visa (2020, the first), followed by Portugal (2022) and Spain (2023, in the startup law).",
                // CONFIRMED - a new but thrice-enacted class. MINOR (+4): a genuinely novel
                // admission channel with deliberately small volumes - openness moves, a little.
                // Funding tier (0.5).
                ImmigrationPolicyDelta = 4f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "labor_migration_quota_act",
                Name = "Labor Migration Quota Act",
                Description = "Imposes annual numerical caps on work-based residence permits, allocated by lottery or priority ranking once the cap binds.",
                Category = LawCategory.LaborMarket,
                Citation = "Switzerland's 2014 'against mass immigration' initiative (a constitutional quota mandate) and the US H-1B annual cap with its lottery - two standing quota designs.",
                // CONFIRMED - the openness dial's structural down-shape. MODERATE (-7): a binding
                // numerical cap narrows admission mechanically - deeper than any single-channel
                // tweak, short of the paradigm-shift restriction act (-12). Hottest tier (1.5) -
                // the Swiss initiative rewrote a treaty relationship.
                ImmigrationPolicyDelta = -7f,
                EnactmentApprovalCost = 1.5f
            },

            // ── LABOR MARKET BATCH 5 (pass 3, 2026-08-26): 10 laws, 90 -> 100 - the category
            // closes at 50. ──
            // Closing charter: each dial gets its remaining real shape (sectoral wage boards for
            // MinWage; public-sector-first and adoption parity for PaidLeave; telework rights and
            // the overtime salary threshold for Overtime; the Swedish transition guarantee for
            // Retraining; Elterngeld Plus for Family; reunification, day-one work rights and the
            // skills-levy dual-dial for Immigration). The all-enacted composition now genuinely
            // SATURATES two ceilings (Retraining raw 104, Family raw 101 on USA bases) - by
            // design, so the end-of-category saturating re-run exercises the clamp reached AND
            // released, the exact claim the C&J close-out proved at 27-of-50. The saturating
            // re-run itself is LaborLawCompositionDiagnostic's record, not claimed here.
            new LawDefinition
            {
                Id = "sectoral_wage_boards_act",
                Name = "Sectoral Wage Boards Act",
                Description = "Empowers tripartite boards to set binding minimum pay above the statutory floor for named low-wage sectors.",
                Category = LawCategory.LaborMarket,
                Citation = "New York's 2015 fast-food wage board (a $15 sectoral floor by administrative order under a 1930s wage-board statute) and Australia's modern-award system of binding sectoral minimums.",
                // CONFIRMED - two living designs. MODERATE (+4 Kaitz; x2 -> 8): sectoral boards
                // lift effective floors for whole low-wage industries - broader than procurement
                // coverage, narrower than a headline-rate change. Contested tier (1.0).
                MinimumWageDelta = 4f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "public_sector_family_leave_act",
                Name = "Public Sector Family Leave Act",
                Description = "Grants the government's own workforce paid parental leave first, setting the employer-of-reference standard private mandates later follow.",
                Category = LawCategory.LaborMarket,
                Citation = "The US Federal Employee Paid Leave Act (2019): 12 weeks' paid parental leave for ~2 million federal workers, in a country with no general mandate.",
                // CONFIRMED - a named, enacted first-mover shape. MINOR (+5 weeks): covering the
                // public workforce moves the economy-wide entitlement average genuinely but
                // partially - the general mandate (MODERATE +12) is the next law over. Funding
                // tier (0.5).
                PaidFamilyLeaveWeeksDelta = 5f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "adoption_leave_parity_act",
                Name = "Adoption Leave Parity Act",
                Description = "Extends the full paid parental leave entitlement to adoptive and surrogate parents at parity with birth parents.",
                Category = LawCategory.LaborMarket,
                Citation = "The UK's statutory adoption leave (Employment Act 2002, aligned to maternity-leave parity in 2015) - the standard parity design.",
                // CONFIRMED - enacted parity. MINOR (+3 weeks): parity extends coverage to a
                // small population rather than lengthening the entitlement - the smallest honest
                // shape on this dial. Funding tier (0.5).
                PaidFamilyLeaveWeeksDelta = 3f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "telework_rights_act",
                Name = "Telework Rights Act",
                Description = "Makes flexible and remote working a day-one statutory right to request, refusable only on enumerated business grounds.",
                Category = LawCategory.LaborMarket,
                Citation = "The Netherlands' Flexible Working Act (2016, extended toward telework in 2022) and the UK's day-one right to request flexible working (2024).",
                // CONFIRMED - two enacted rights. MINOR (+3): a right to REQUEST with enumerated
                // refusal grounds regulates the working-arrangement margin lightly - real, small,
                // administrative. Funding tier (0.5).
                OvertimeRegulationDelta = 3f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "overtime_pay_threshold_act",
                Name = "Overtime Pay Threshold Act",
                Description = "Raises the salary ceiling under which workers must receive overtime premiums, restoring coverage eroded by inflation.",
                Category = LawCategory.LaborMarket,
                Citation = "The US Department of Labor's FLSA overtime-threshold rules (2019's $35,568; 2024's two-step raise toward $58,656, partially enjoined) - the recurring coverage-restoration fight.",
                // CONFIRMED - a named, twice-fought rule. MINOR (+5): threshold restoration
                // re-extends premium-pay protection to salaried workers priced out of it - a
                // real strictness step below the framework-scale acts. Contested tier (1.0) -
                // both rules drew immediate litigation.
                OvertimeRegulationDelta = 5f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "national_retraining_guarantee_act",
                Name = "National Retraining Guarantee Act",
                Description = "Guarantees mid-career workers a year of funded study at high wage replacement to retrain for shortage occupations.",
                Category = LawCategory.LaborMarket,
                Citation = "Sweden's omstallningsstudiestod (2022): up to a year of transition study support at ~80% wage replacement for established workers, the era's largest retraining entitlement.",
                // CONFIRMED - a named, just-built entitlement. MODERATE (+7): an individual
                // GUARANTEE at high replacement is a genuine capacity-and-entitlement step
                // beyond accounts (+6 MINOR) and below the flexicurity/ALMP system builds
                // (+8/+12). Funding tier (0.5).
                RetrainingProgramDelta = 7f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "parental_benefit_modernization_act",
                Name = "Parental Benefit Modernization Act",
                Description = "Restructures the parental benefit to be part-time compatible, with bonus months when both parents share work and care.",
                Category = LawCategory.LaborMarket,
                Citation = "Germany's ElterngeldPlus (2015): benefit months usable alongside part-time work, plus partnership bonus months - the modernization layer on the 2007 Elterngeld.",
                // CONFIRMED - a named second-generation reform. MODERATE (+9) on FamilyPolicy:
                // restructuring the flagship benefit's architecture is a real, felt shift inside
                // the existing system - the create-a-program tier sits above it. Funding tier
                // (0.5) - it passed with broad consensus.
                FamilyPolicyDelta = 9f,
                EnactmentApprovalCost = 0.5f
            },
            new LawDefinition
            {
                Id = "family_reunification_act",
                Name = "Family Reunification Act",
                Description = "Grants settled residents a statutory right to bring spouses and minor children, with income and housing conditions harmonized down.",
                Category = LawCategory.LaborMarket,
                Citation = "The EU Family Reunification Directive (2003/86/EC), the standing statutory right for third-country nationals across the bloc.",
                // CONFIRMED - standing directive. MINOR (+6): reunification is a large real
                // admission channel, but the directive CODIFIES rights more than it widens
                // volumes - graded at the dial's small tier, honestly. Contested tier (1.0).
                ImmigrationPolicyDelta = 6f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "refugee_work_authorization_act",
                Name = "Refugee Work Authorization Act",
                Description = "Grants asylum seekers labor-market access from day one of their claim instead of after a waiting period.",
                Category = LawCategory.LaborMarket,
                Citation = "Sweden's day-one work exemption (AT-UND) for asylum seekers, against Germany's 3-month and the USA's 180-day waiting rules - the openness margin is the WAIT, and it is legislated.",
                // CONFIRMED - a real cross-country design margin. MINOR (+5): work authorization
                // changes what admitted people may DO, not how many are admitted - a genuine
                // but bounded openness step. Contested tier (1.0).
                ImmigrationPolicyDelta = 5f,
                EnactmentApprovalCost = 1.0f
            },
            new LawDefinition
            {
                Id = "immigration_skills_levy_act",
                Name = "Immigration Skills Levy Act",
                Description = "Charges employers a levy per sponsored foreign worker and earmarks the proceeds for domestic workforce training.",
                Category = LawCategory.LaborMarket,
                Citation = "The UK's Immigration Skills Charge (2017): GBP 1,000 per sponsored worker per year, framed and hypothecated as domestic-skills funding.",
                // CONFIRMED - a named levy. The category's closing DUAL-DIAL law, both legs
                // real: MINOR ImmigrationPolicyDelta -3 (a per-head sponsorship cost narrows
                // employer demand at the margin) and MINOR RetrainingProgramDelta +3 (the
                // hypothecated training fund). Funding tier (0.5).
                ImmigrationPolicyDelta = -3f,
                RetrainingProgramDelta = 3f,
                EnactmentApprovalCost = 0.5f
            },
            // ================================================================================================
            // P4-C3, category three - LABOUR-MARKET INSTITUTIONS (2026-09-04): the first category built from the
            // lever map's unreached set (LEVER_MAP.md §2), reaching the NATURAL RATE OF UNEMPLOYMENT, which no dial
            // and no law reached before. Ten laws, each with one thirteenth-effect delta in percentage points
            // (DialMagnitudeScales x15 for the tier grid) and its own reading on the economic axis (LrEconToward10),
            // because a NAIRU move's sign does not tell a law's politics. Citations carry the catalog's classes:
            // CONFIRMED where a measured structural-unemployment effect exists, CONFIRMED-DIRECTION where the sign is
            // the record's and the size the game's, DIRECTIONAL where the literature argues.
            // ================================================================================================

            // CONFIRMED-scale: Krebs & Scheffel (IMF Economic Review, 2013) put the Hartz reforms' long-run effect
            // on unemployment at about -1.4 pp; Dustmann, Fitzenberger, Schönberg & Spitz-Oener (JEP, 2014) on the
            // wage moderation beneath it. MAJOR (-1.2 x15 = 18); the approval cost is the reform's own politics
            // (the SPD lost Länder elections and its chancellor over it).
            new LawDefinition
            {
                Id = "hartz_benefit_reform_act",
                Name = "Unemployment Benefit Consolidation Act",
                Description = "Merges long-term unemployment assistance into a flat, means-tested benefit, tightens the duty to accept work and shortens the insured benefit's duration.",
                Category = LawCategory.LabourInstitutions,
                Citation = "Germany's Hartz IV (2005) - Arbeitslosenhilfe and Sozialhilfe merged into Arbeitslosengeld II, with the Hartz I-III activation and placement reforms of 2003-04 beneath it.",
                Structural = new[] { new StructuralDelta(StructuralParameter.NaturalUnemploymentRate, -1.2f) },
                LrEconToward10 = 1f,
                EnactmentApprovalCost = 2.0f
            },
            // CONFIRMED-DIRECTION: Denmark's structural unemployment fell from about 9 % (1993) to about 4 % through
            // the 2000s (Danish Economic Councils; OECD) as benefit duration went from nine years toward four (1994-99)
            // and later two (2010); how much of the fall is the duration and how much the activation beside it is
            // argued. MAJOR (-1.0 x15 = 15).
            new LawDefinition
            {
                Id = "benefit_duration_reform_act",
                Name = "Benefit Duration Reform Act",
                Description = "Halves the maximum duration of insured unemployment benefit and makes continued benefit conditional on activation from the first year.",
                Category = LawCategory.LabourInstitutions,
                Citation = "Denmark's labour-market reforms of 1994-99 (benefit duration 9 → 4 years, activation from year one) and the 2010 halving to two years.",
                Structural = new[] { new StructuralDelta(StructuralParameter.NaturalUnemploymentRate, -1.0f) },
                LrEconToward10 = 0.5f,
                EnactmentApprovalCost = 1.5f
            },
            // DIRECTIONAL: the Swedish Fiscal Policy Council and the National Institute of Economic Research put the
            // earned-income tax credit's long-run effect on equilibrium unemployment at roughly -0.5 to -1 pp; the
            // estimates rest on labour-supply elasticities rather than an observed break. MODERATE (-0.5 x15 = 7.5).
            new LawDefinition
            {
                Id = "in_work_tax_credit_act",
                Name = "In-Work Tax Credit Act",
                Description = "A tax credit on earned income only, widening the gap between wages and benefits so that work pays at the margin.",
                Category = LawCategory.LabourInstitutions,
                Citation = "Sweden's jobbskatteavdrag (2007-), the earned-income tax credit built out in five steps; the US EITC is the older model.",
                Structural = new[] { new StructuralDelta(StructuralParameter.NaturalUnemploymentRate, -0.5f) },
                LrEconToward10 = 0.5f,
                EnactmentApprovalCost = 1.0f
            },
            // DIRECTIONAL and CONTESTED: Hagedorn, Manovskii & Mitman (2015) attribute a large share of the 2014 US
            // employment gain to the expiry of extended benefits; Rothstein (2011) and Farber & Valletta (2015) find
            // small effects on job finding. MODERATE (-0.4 x15 = 6) - the smallest MODERATE, for a contested case.
            new LawDefinition
            {
                Id = "ui_duration_cut_act",
                Name = "Unemployment Insurance Duration Act",
                Description = "Ends the extended-benefit tiers and caps insured unemployment at its ordinary duration.",
                Category = LawCategory.LabourInstitutions,
                Citation = "The expiry of the US Emergency Unemployment Compensation programme (December 2013), which had extended benefits to up to 99 weeks after 2008.",
                Structural = new[] { new StructuralDelta(StructuralParameter.NaturalUnemploymentRate, -0.4f) },
                LrEconToward10 = 1f,
                EnactmentApprovalCost = 1.5f
            },
            // DIRECTIONAL: OECD Employment Outlook (2013, 2020) - stricter employment protection lengthens unemployment
            // spells and lowers flows; the effect on the LEVEL of structural unemployment is small and disputed.
            // MODERATE (-0.4 x15 = 6).
            new LawDefinition
            {
                Id = "employment_protection_reform_act",
                Name = "Employment Protection Reform Act",
                Description = "Lowers severance costs, narrows the grounds on which a dismissal can be reversed and lets firms adjust hours and pay in a downturn.",
                Category = LawCategory.LabourInstitutions,
                Citation = "Spain's labour reform of 2012 (Real Decreto-ley 3/2012) and Italy's Jobs Act (2015), both loosening dismissal rules for open-ended contracts.",
                Structural = new[] { new StructuralDelta(StructuralParameter.NaturalUnemploymentRate, -0.4f) },
                LrEconToward10 = 1f,
                EnactmentApprovalCost = 1.5f
            },
            // DIRECTIONAL: Calmfors & Driffill (Economic Policy, 1988) - the hump: fully centralised and fully
            // decentralised bargaining both outperform the middle; the Netherlands' Wassenaar Agreement (1982) and
            // Sweden's Industrial Agreement (1997) are the coordination cases. MINOR (-0.3 x15 = 4.5).
            new LawDefinition
            {
                Id = "bargaining_decentralisation_act",
                Name = "Wage Bargaining Reform Act",
                Description = "Moves wage-setting from the national round to the sector and the firm, with a coordinating norm set by the exposed sector.",
                Category = LawCategory.LabourInstitutions,
                Citation = "The Netherlands' Wassenaar Agreement (1982) and Sweden's Industrial Agreement (Industriavtalet, 1997) - wage moderation coordinated on the exposed sector.",
                Structural = new[] { new StructuralDelta(StructuralParameter.NaturalUnemploymentRate, -0.3f) },
                LrEconToward10 = 0.5f,
                EnactmentApprovalCost = 1.0f
            },
            // DIRECTIONAL: Card, Kluve & Weber (JEEA, 2018), a meta-analysis of 200 programme evaluations - modest
            // positive effects in the medium run, larger for training than for public employment. MINOR (-0.3 x15 = 4.5);
            // the law reads LEFT on the economic axis - this is spending, not a cut.
            new LawDefinition
            {
                Id = "active_labour_market_expansion_act",
                Name = "Active Labour-Market Programmes Expansion Act",
                Description = "Funds training, subsidised placements and job-search support for the long-term unemployed, with participation a condition of benefit.",
                Category = LawCategory.LabourInstitutions,
                Citation = "Denmark's and Sweden's active labour-market spending, at 1-2 % of GDP the OECD's highest (OECD Employment Database).",
                Structural = new[] { new StructuralDelta(StructuralParameter.NaturalUnemploymentRate, -0.3f) },
                LrEconToward10 = -0.5f,
                EnactmentApprovalCost = 1.0f
            },
            // CONFIRMED-DIRECTION: Launov & Wälde (International Economic Review, 2016) attribute about half of the
            // post-Hartz fall in unemployment to the placement-service reform (Hartz III) rather than the benefit cut.
            // MINOR (-0.3 x15 = 4.5); administrative, so it reads near the centre.
            new LawDefinition
            {
                Id = "public_employment_service_reform_act",
                Name = "Public Employment Service Reform Act",
                Description = "Merges benefit administration and job placement into one office with caseloads, targets and a duty to place.",
                Category = LawCategory.LabourInstitutions,
                Citation = "The UK's Jobcentre Plus (2002) and Germany's Hartz III reorganisation of the Bundesagentur für Arbeit (2004).",
                Structural = new[] { new StructuralDelta(StructuralParameter.NaturalUnemploymentRate, -0.3f) },
                LrEconToward10 = 0.25f,
                EnactmentApprovalCost = 0.5f
            },
            // DIRECTIONAL and CONTESTED: Crépon & Kramarz (JPE, 2002) found job losses from France's earlier 39-hour
            // step; Chemin & Wasmer (JOLE, 2009) find no employment effect of the 35 hours in the Alsace-Moselle
            // comparison. The sign here is the sceptics' (+0.3, MINOR); the law reads LEFT.
            new LawDefinition
            {
                Id = "working_time_reduction_act",
                Name = "Statutory Working-Time Reduction Act",
                Description = "Cuts the statutory working week with pay maintained, on the argument that shared hours are shared jobs.",
                Category = LawCategory.LabourInstitutions,
                Citation = "France's Aubry laws (1998, 2000) - the 35-hour week.",
                Structural = new[] { new StructuralDelta(StructuralParameter.NaturalUnemploymentRate, 0.3f) },
                LrEconToward10 = -1f,
                EnactmentApprovalCost = 1.0f
            },
            // DIRECTIONAL: Layard, Nickell & Jackman (Unemployment, 1991) - the replacement rate and benefit duration
            // are the two institutions that most raise equilibrium unemployment across the OECD panel; later panels
            // (Nickell, Nunziata & Ochel 2005) agree on the sign and argue the size. MODERATE (+0.5 x15 = 7.5); LEFT.
            new LawDefinition
            {
                Id = "benefit_generosity_act",
                Name = "Unemployment Benefit Generosity Act",
                Description = "Raises the insured benefit's ceiling and replacement rate and lengthens the period before it tapers.",
                Category = LawCategory.LabourInstitutions,
                Citation = "Sweden's permanent raise of the unemployment-insurance ceiling (2022), making the 2020 pandemic increase standing.",
                Structural = new[] { new StructuralDelta(StructuralParameter.NaturalUnemploymentRate, 0.5f) },
                LrEconToward10 = -1f,
                EnactmentApprovalCost = 1.0f
            },

            // ================================================================================================
            // P4-C3, category four - THE FISCAL FRAMEWORK (2026-09-04): the second category from the lever map's
            // unreached set, reaching five structural parameters no dial reaches (the debt-comfort anchor the fiscal
            // reaction reads, the debt stock's average maturity, the market's risk-premium sensitivity, tax-collection
            // coverage, the baseline spending share). Ten laws, each a structural delta in the parameter's own unit
            // (StructuralParameters carries the tier-grid scale) and its own reading on the economic axis.
            // ================================================================================================

            // CONFIRMED-DIRECTION: Germany's Schuldenbremse (Grundgesetz Art. 109/115, 2009; binding on the Bund from
            // 2016) - a structural-deficit ceiling of 0.35 % of GDP; the debt ratio fell from 82 % (2010) to 59 %
            // (2019). Modelled as the comfort anchor moving DOWN ten points (the automatic reaction tightens earlier),
            // MODERATE (10 x1). Reads right.
            new LawDefinition
            {
                Id = "constitutional_debt_brake_act",
                Name = "Constitutional Debt Brake Act",
                Description = "Writes a structural-deficit ceiling into the constitution, with an escape clause for emergencies and a repayment plan when it is used.",
                Category = LawCategory.FiscalFramework,
                Citation = "Germany's Schuldenbremse (Basic Law Articles 109 and 115, 2009), binding on the federation from 2016 and on the Länder from 2020.",
                Structural = new[] { new StructuralDelta(StructuralParameter.ComfortableDebtToGdpPercent, -10f) },
                LrEconToward10 = 1f,
                EnactmentApprovalCost = 1.5f
            },
            // CONFIRMED-DIRECTION: Sweden's fiscal framework gained a debt anchor of 35 % of GDP (Maastricht debt) in 2019,
            // beside the surplus target and the expenditure ceiling. MINOR (5 x1).
            new LawDefinition
            {
                Id = "debt_anchor_act",
                Name = "Debt Anchor Act",
                Description = "Sets a public-debt anchor as a share of GDP and obliges the government to explain to Parliament any drift of more than five points from it.",
                Category = LawCategory.FiscalFramework,
                Citation = "Sweden's debt anchor (skuldankaret) of 35 % of GDP, in force from 2019 within the fiscal policy framework.",
                Structural = new[] { new StructuralDelta(StructuralParameter.ComfortableDebtToGdpPercent, -5f) },
                LrEconToward10 = 0.5f,
                EnactmentApprovalCost = 0.5f
            },
            // CONFIRMED-DIRECTION: the brake's emergency clause was invoked 2020-22 and a defence exemption added in 2025;
            // the anchor moving UP fifteen points is the loosening. MAJOR (15 x1). Reads left of centre.
            new LawDefinition
            {
                Id = "debt_brake_suspension_act",
                Name = "Debt Brake Suspension Act",
                Description = "Invokes the escape clause and exempts a named class of spending from the deficit ceiling for the parliamentary term.",
                Category = LawCategory.FiscalFramework,
                Citation = "The Bundestag's emergency-clause resolutions of 2020-22 and the March 2025 amendment exempting defence spending above 1 % of GDP from the brake.",
                Structural = new[] { new StructuralDelta(StructuralParameter.ComfortableDebtToGdpPercent, 15f) },
                LrEconToward10 = -0.5f,
                EnactmentApprovalCost = 1.0f
            },
            // CONFIRMED: Sweden's expenditure ceiling (utgiftstaket, 1997) - a nominal cap on central-government spending set
            // three years ahead; central-government spending fell from about 40 % of GDP to under 30 % over the following
            // decade. Modelled as the baseline spending share moving down one point of GDP, MODERATE (1.0 x10). Reads right.
            new LawDefinition
            {
                Id = "expenditure_ceiling_act",
                Name = "Expenditure Ceiling Act",
                Description = "Sets a nominal ceiling on central-government spending three years ahead, voted before the budget and binding on it.",
                Category = LawCategory.FiscalFramework,
                Citation = "Sweden's expenditure ceiling (utgiftstaket), introduced in the 1997 budget reform and part of the fiscal framework since.",
                Structural = new[] { new StructuralDelta(StructuralParameter.GovernmentSpendingRate, -1.0f) },
                LrEconToward10 = 0.5f,
                EnactmentApprovalCost = 1.0f
            },
            // DIRECTIONAL: Debrun & Kinda (IMF, 2017) find independent fiscal councils associated with more accurate
            // forecasts and better compliance with rules; the market channel (a lower premium per point of debt) is the
            // game's reading of that credibility. MODERATE (0.15 x60 = 9). Reads near the centre.
            new LawDefinition
            {
                Id = "fiscal_council_act",
                Name = "Independent Fiscal Council Act",
                Description = "Establishes an independent council that audits the government's forecasts and its compliance with the fiscal rules, reporting to Parliament.",
                Category = LawCategory.FiscalFramework,
                Citation = "Sweden's Fiscal Policy Council (Finanspolitiska rådet, 2007) and the UK's Office for Budget Responsibility (2010).",
                Structural = new[] { new StructuralDelta(StructuralParameter.RiskPremiumSensitivity, -0.15f) },
                LrEconToward10 = 0.25f,
                EnactmentApprovalCost = 0.5f
            },
            // CONFIRMED: the UK Debt Management Office issues long - the gilt stock's average maturity is about 14 years,
            // the longest in the OECD - so the effective rate reprices slowly. Two years more, MODERATE (2 x5 = 10).
            new LawDefinition
            {
                Id = "debt_office_long_issuance_act",
                Name = "Debt Office Long-Issuance Mandate",
                Description = "Directs the debt office to lengthen the stock's average maturity, trading a higher term premium today for slower repricing.",
                Category = LawCategory.FiscalFramework,
                Citation = "The UK Debt Management Office's issuance remit - a gilt stock with an average maturity near 14 years; Sweden's Riksgälden steers its own by a time-to-refixing range.",
                Structural = new[] { new StructuralDelta(StructuralParameter.AverageDebtMaturityYears, 2f) },
                LrEconToward10 = 0f,
                EnactmentApprovalCost = 0.5f
            },
            // DIRECTIONAL: issuing at the short end saves the term premium and exposes the stock to repricing - the
            // Italian Treasury's shortening in the 1990s and the US Treasury's post-2020 bill share are the cases.
            // MODERATE (1.5 x5 = 7.5).
            new LawDefinition
            {
                Id = "short_issuance_savings_act",
                Name = "Short-Issuance Savings Act",
                Description = "Directs the debt office to fund at the short end to cut the interest bill now, accepting faster repricing when rates move.",
                Category = LawCategory.FiscalFramework,
                Citation = "The US Treasury's rising bill share after 2020 and Italy's short-dated funding of the early 1990s.",
                Structural = new[] { new StructuralDelta(StructuralParameter.AverageDebtMaturityYears, -1.5f) },
                LrEconToward10 = 0.25f,
                EnactmentApprovalCost = 0.5f
            },
            // CONFIRMED-DIRECTION: Italy's mandatory e-invoicing (FatturaPA for B2B, 2019) - the revenue agency put the
            // first-year VAT gain near EUR 3.5 bn; the EU VAT-gap series fell after. Coverage +0.03, MODERATE (0.03 x300 = 9).
            new LawDefinition
            {
                Id = "electronic_invoicing_mandate_act",
                Name = "Electronic Invoicing Mandate",
                Description = "Requires every business invoice to pass through the tax authority's exchange system, closing the gap between invoiced and declared sales.",
                Category = LawCategory.FiscalFramework,
                Citation = "Italy's mandatory B2B e-invoicing through the Sistema di Interscambio (FatturaPA, 2019).",
                Structural = new[] { new StructuralDelta(StructuralParameter.CollectionEfficiency, 0.03f) },
                LrEconToward10 = 0f,
                EnactmentApprovalCost = 0.5f
            },
            // CONFIRMED: Kleven, Knudsen, Kreiner, Pedersen & Saez (Econometrica, 2011), the Danish audit experiment -
            // evasion near zero on third-party-reported income, substantial on self-reported. Coverage +0.02, MINOR (6).
            new LawDefinition
            {
                Id = "third_party_reporting_act",
                Name = "Third-Party Reporting and Withholding Act",
                Description = "Extends withholding and third-party information reporting to the income the tax authority cannot yet see - platform earnings, rents, capital income.",
                Category = LawCategory.FiscalFramework,
                Citation = "Denmark's third-party reporting regime, the setting of the 2011 Danish tax-audit experiment.",
                Structural = new[] { new StructuralDelta(StructuralParameter.CollectionEfficiency, 0.02f) },
                LrEconToward10 = -0.25f,
                EnactmentApprovalCost = 0.5f
            },
            // DIRECTIONAL: Alm, Martinez-Vazquez & Wallace (2009) and the Italian scudi fiscali (2001-09) - repeated
            // amnesties raise revenue once and erode compliance after; the sign here is the erosion. Coverage -0.02, MINOR.
            new LawDefinition
            {
                Id = "tax_amnesty_act",
                Name = "Tax Amnesty Act",
                Description = "Lets undeclared income and assets be regularised at a flat rate without penalty, for one year.",
                Category = LawCategory.FiscalFramework,
                Citation = "Italy's scudi fiscali (2001, 2003, 2009) - repatriation amnesties at 2.5-5 %.",
                Structural = new[] { new StructuralDelta(StructuralParameter.CollectionEfficiency, -0.02f) },
                LrEconToward10 = 0.5f,
                EnactmentApprovalCost = 0.5f
            },

            // ================================================================================================
            // P4-C3, THE THIRD CATEGORY (2026-09-05, ruling (a)): THE MONETARY REGIME - ten laws on the central
            // bank's target, its mandate and its reaction, reaching the CURRENCY ZONE's four Taylor-rule parameters
            // (StructuralParameter.InflationTarget .. UnemploymentGapWeight; TaylorRule reads the zone). Every one
            // carries RequiresOwnCurrency: within a parliament's competence only where the country owns its bank
            // (Sweden, Poland, the USA) - a euro member's House does not reach the ECB's target or mandate (TFEU
            // Articles 127 and 130), which the browser states instead of offering Enact. Magnitudes are the stepped
            // rule's bands on the table's scales ([AUTHORED-DRAFT] in size, cited in direction); the tier reads
            // from the largest scaled delta as for every structural law.
            // ================================================================================================
            // DIRECTIONAL: Blanchard, Dell'Ariccia & Mauro (2010, IMF SPN/10/03, "Rethinking Macroeconomic Policy")
            // and Ball (2014, IMF WP/14/92, "The Case for a Long-Run Inflation Target of Four Percent") argue a
            // higher target buys room above the zero lower bound. +1.0 pp of target = MODERATE (10 x1). Reads left.
            new LawDefinition
            {
                Id = "inflation_target_increase_act",
                Name = "Inflation Target Increase Act",
                Description = "Raises the central bank's announced inflation target by one percentage point, giving the policy rate more room above zero in a downturn.",
                Category = LawCategory.MonetaryRegime,
                Citation = "Blanchard, Dell'Ariccia & Mauro 2010 (IMF Staff Position Note 10/03); Ball 2014 (IMF WP/14/92) - the case for a higher target.",
                Structural = new[] { new StructuralDelta(StructuralParameter.InflationTarget, 1f) },
                RequiresOwnCurrency = true,
                LrEconToward10 = -0.5f,
                EnactmentApprovalCost = 1f
            },
            // CONFIRMED-DIRECTION: the ECB's 1998 definition of price stability ("below 2 %") and the Bundesbank's
            // normative 1.5-2 % - a stricter definition sits below the round number. -0.5 pp = MINOR (5 x1). Reads right.
            new LawDefinition
            {
                Id = "price_stability_definition_act",
                Name = "Price Stability Definition Act",
                Description = "Writes a stricter definition of price stability into the bank's mandate, lowering the target it steers to by half a point.",
                Category = LawCategory.MonetaryRegime,
                Citation = "ECB Governing Council, October 1998: price stability as a year-on-year HICP increase below 2 %; the Bundesbank's normative inflation rate of 1.5-2 % in the 1990s.",
                Structural = new[] { new StructuralDelta(StructuralParameter.InflationTarget, -0.5f) },
                RequiresOwnCurrency = true,
                LrEconToward10 = 0.5f,
                EnactmentApprovalCost = 0.75f
            },
            // CONFIRMED: the ECB's 2021 strategy review replaced "below, but close to, 2 %" with a symmetric 2 % target,
            // a small upward move of the steering point; the Riksbank's 2017 move to CPIF with a 1-3 % variation band
            // made the symmetry explicit. +0.25 pp = MINOR (2.5 x1). Reads slightly left.
            new LawDefinition
            {
                Id = "symmetric_target_act",
                Name = "Symmetric Target Act",
                Description = "Replaces a ceiling-shaped target with a symmetric one, so undershoots count as failures as much as overshoots.",
                Category = LawCategory.MonetaryRegime,
                Citation = "ECB monetary policy strategy statement, 8 July 2021 (symmetric 2 %); Sveriges Riksbank, September 2017 (CPIF target with a variation band).",
                Structural = new[] { new StructuralDelta(StructuralParameter.InflationTarget, 0.25f) },
                RequiresOwnCurrency = true,
                LrEconToward10 = -0.25f,
                EnactmentApprovalCost = 0.25f
            },
            // CONFIRMED-DIRECTION: the Federal Reserve's dual mandate (Federal Reserve Reform Act 1977; Full Employment
            // and Balanced Growth Act 1978) puts employment beside prices in the bank's reaction. +0.5 on the
            // unemployment-gap weight = MODERATE (10 x1). Reads left.
            new LawDefinition
            {
                Id = "full_employment_mandate_act",
                Name = "Full Employment Mandate Act",
                Description = "Adds maximum employment to the bank's statutory objectives beside price stability, so a slack labour market pulls the policy rate down harder.",
                Category = LawCategory.MonetaryRegime,
                Citation = "Federal Reserve Reform Act of 1977 and the Full Employment and Balanced Growth Act of 1978 (Humphrey-Hawkins) - the dual mandate.",
                Structural = new[] { new StructuralDelta(StructuralParameter.UnemploymentGapWeight, 0.5f) },
                RequiresOwnCurrency = true,
                LrEconToward10 = -1f,
                EnactmentApprovalCost = 1f
            },
            // CONFIRMED: New Zealand's Reserve Bank of New Zealand (Economic Objective) Amendment Act 2023 removed the
            // maximum-sustainable-employment objective added in 2018, returning the bank to a single mandate. -0.5 on
            // the unemployment-gap weight = MODERATE (10 x1). Reads right.
            new LawDefinition
            {
                Id = "single_mandate_act",
                Name = "Single Mandate Act",
                Description = "Strikes the employment objective from the bank's statute, leaving price stability as its one goal.",
                Category = LawCategory.MonetaryRegime,
                Citation = "Reserve Bank of New Zealand (Economic Objective) Amendment Act 2023 - the employment objective repealed in December 2023.",
                Structural = new[] { new StructuralDelta(StructuralParameter.UnemploymentGapWeight, -0.5f) },
                RequiresOwnCurrency = true,
                LrEconToward10 = 1f,
                EnactmentApprovalCost = 1f
            },
            // CONFIRMED (the literature's two rules): Taylor (1993) weights the inflation gap at 0.5; Taylor (1999)
            // and the Fed's "balanced approach" variants raise the response, and Clarida, Gali & Gertler (2000, QJE)
            // find the post-1979 Fed's inflation response above one (the Taylor principle) where the pre-1979 Fed's
            // was below it. +0.5 on the inflation-gap weight = MODERATE (10 x1). Reads right.
            new LawDefinition
            {
                Id = "taylor_principle_codification_act",
                Name = "Taylor Principle Codification Act",
                Description = "Instructs the bank to raise the policy rate more than one-for-one with inflation above target, and to publish the rule it follows.",
                Category = LawCategory.MonetaryRegime,
                Citation = "Taylor 1993 (Carnegie-Rochester 39) and Taylor 1999; Clarida, Gali & Gertler 2000 (QJE 115) - the inflation response above one after 1979.",
                Structural = new[] { new StructuralDelta(StructuralParameter.InflationGapWeight, 0.5f) },
                RequiresOwnCurrency = true,
                LrEconToward10 = 0.5f,
                EnactmentApprovalCost = 0.75f
            },
            // DIRECTIONAL: estimated policy rules carry interest-rate smoothing (Clarida, Gali & Gertler 2000 report
            // a smoothing parameter near 0.8), so the within-year response to a gap is a fraction of the long-run
            // coefficient - a gradualism mandate reads as a smaller weight in a rule evaluated each year. -0.25 on
            // the inflation-gap weight = MINOR (5 x1). Reads left.
            new LawDefinition
            {
                Id = "gradualism_mandate_act",
                Name = "Gradualism Mandate Act",
                Description = "Requires the bank to move the policy rate in measured steps and to look through transitory price shocks, damping its response to the inflation gap.",
                Category = LawCategory.MonetaryRegime,
                Citation = "Clarida, Gali & Gertler 2000 (QJE 115): interest-rate smoothing near 0.8 in estimated Fed reaction functions; Sack & Wieland 2000 (Journal of Economics and Business) on gradualism.",
                Structural = new[] { new StructuralDelta(StructuralParameter.InflationGapWeight, -0.25f) },
                RequiresOwnCurrency = true,
                LrEconToward10 = -0.25f,
                EnactmentApprovalCost = 0.5f
            },
            // DIRECTIONAL: the Federal Reserve's August 2020 Statement on Longer-Run Goals adopted flexible average
            // inflation targeting - inflation "moderately above 2 percent for some time" after shortfalls - which in
            // a rule evaluated on the current gap reads as a weaker response to overshoots. -0.25 on the
            // inflation-gap weight = MINOR (5 x1). Reads left.
            new LawDefinition
            {
                Id = "average_inflation_targeting_act",
                Name = "Average Inflation Targeting Act",
                Description = "Directs the bank to aim for its target on average over time, tolerating a period above it after a period below.",
                Category = LawCategory.MonetaryRegime,
                Citation = "Federal Open Market Committee, Statement on Longer-Run Goals and Monetary Policy Strategy, 27 August 2020 (flexible average inflation targeting).",
                Structural = new[] { new StructuralDelta(StructuralParameter.InflationGapWeight, -0.25f) },
                RequiresOwnCurrency = true,
                LrEconToward10 = -0.5f,
                EnactmentApprovalCost = 0.5f
            },
            // DIRECTIONAL: Holston, Laubach & Williams (2017, Journal of International Economics 108) estimate r* for
            // the US, euro area, UK and Canada falling to about half a percent or below after 2008, and the FOMC's
            // longer-run federal funds rate projection fell from 4.25 % (2012) to 2.5 % (2019) - a framework review
            // that adopts a lower neutral rate lowers the whole rule. -0.5 pp of r* = MINOR (5 x1). Reads left.
            new LawDefinition
            {
                Id = "neutral_rate_reassessment_act",
                Name = "Neutral Rate Reassessment Act",
                Description = "Mandates a periodic review of the bank's framework that adopts the current estimate of the neutral real rate, lowering the anchor the rule builds on.",
                Category = LawCategory.MonetaryRegime,
                Citation = "Holston, Laubach & Williams 2017 (JIE 108) - r* estimates near or below 0.5 % after 2008; FOMC Summary of Economic Projections, longer-run rate 4.25 % (Jan 2012) to 2.5 % (2019).",
                Structural = new[] { new StructuralDelta(StructuralParameter.NeutralRealRate, -0.5f) },
                RequiresOwnCurrency = true,
                LrEconToward10 = -0.25f,
                EnactmentApprovalCost = 0.25f
            },
            // DIRECTIONAL: the 2023-24 reassessment upward - the FOMC's longer-run median rose from 2.5 % to about 3 %
            // through 2024, and the "higher r*" reading (Summers; the BIS Annual Economic Report 2023, chapter II)
            // holds that fiscal deficits and investment demand lifted the neutral rate. +0.5 pp of r* = MINOR (5 x1).
            // Reads right.
            new LawDefinition
            {
                Id = "monetary_normalisation_act",
                Name = "Monetary Normalisation Act",
                Description = "Instructs the bank to steer by a higher neutral rate, treating the low-rate decade as the exception rather than the anchor.",
                Category = LawCategory.MonetaryRegime,
                Citation = "FOMC Summary of Economic Projections, longer-run rate median 2.5 % (2023) to about 3 % (2024); BIS Annual Economic Report 2023, chapter II, on a higher neutral rate.",
                Structural = new[] { new StructuralDelta(StructuralParameter.NeutralRealRate, 0.5f) },
                RequiresOwnCurrency = true,
                LrEconToward10 = 0.5f,
                EnactmentApprovalCost = 0.5f
            },
        };

        /// <summary>P4-C3 third category, ruling (a) (2026-09-05): whether this law is within the country's parliament's competence -
        /// every law is, except one that reaches the currency zone's parameters (RequiresOwnCurrency) in a country that shares its
        /// zone (World.OwnsCurrencyZone false): the ECB's target and mandate are the treaty's, not a member's House's.</summary>
        public static bool IsWithinCompetence(World world, Country country, LawDefinition law)
            => law == null || !law.RequiresOwnCurrency || (world != null && world.OwnsCurrencyZone(country));

        /// <summary>The reason a law outside competence is not offered - the browser's line and the bill's refusal.</summary>
        public const string OutsideCompetenceReason = "TREATY COMPETENCE - the target and the mandate are the currency union's, not this House's";

        /// <summary>Looks up a law by its stable Id, or null if no such law exists (e.g. an old save citing a since-removed law - the caller decides how to degrade, matching PolicyWebRenderer/DisplayName's own "missing entry, not a crash" idiom).</summary>
        public static LawDefinition GetById(string lawId)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].Id == lawId)
                {
                    return All[i];
                }
            }

            return null;
        }
    }
}
