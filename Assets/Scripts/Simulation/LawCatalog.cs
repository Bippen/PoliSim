using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// The static catalog of every authored law. Started as the MVP slice's proof-of-architecture
    /// four (2026-08-24); now a content marathon target of 50 in one category (Crime &amp; Justice),
    /// authored in batches of ~10, real-world-grounded where a real policy exists, honestly labeled
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
    /// <para><b>Real-world grounding, labeled per law</b>: CONFIRMED (a specific, well-documented
    /// real policy/law/program), DIRECTIONAL (a real trend or debate, not tied to one documented
    /// figure), or GENRE-IDIOM (a plausible governance-simulation abstraction in the Democracy-4
    /// idiom, not tied to one real policy) - stated in each law's own doc comment, never left
    /// implicit. Where a real policy's documented effect has a stated direction, the magnitude
    /// tracks it; where the delta is this project's own judgment call, the comment says so instead
    /// of dressing a guess as research (rule 5).</para>
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
    /// </summary>
    public static class LawCatalog
    {
        public static readonly List<LawDefinition> All = new List<LawDefinition>
        {
            new LawDefinition
            {
                Id = "truth_in_sentencing_act",
                Name = "Truth in Sentencing Act",
                Description = "Requires offenders to serve a much larger share of their imposed sentence before parole eligibility, and narrows the discretion judges have to depart from guideline sentences.",
                Category = LawCategory.CrimeJustice,
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
                // CONFIRMED - the real US "progressive prosecutor" trend (Philadelphia DA Larry
                // Krasner, elected 2017; Los Angeles DA George Gascon, elected 2020, among others),
                // each adopting office-wide charging-deprioritization policies for low-level offenses
                // - a genuinely documented, politically contested real trend, not a single national
                // law. MODERATE on severity; MINOR secondary on drug policy (possession specifically
                // named in real guidelines of this kind).
                SentencingSeverityDelta = -10f,
                DrugPolicyDelta = -6f,
                EnactmentApprovalCost = 1.0f
            }
        };

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
