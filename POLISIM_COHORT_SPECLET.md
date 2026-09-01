# The cohort spec-let (C-C13 / P-I1)

**Status: RULED 2026-08-31 (Elias, D-4 (a)): BUILD THE FIVE-YEAR COHORT SUBSTRATE AS SPECCED.** P-I2 is
now live and lands in stages, each with its own commit and its own evidence. ⚠ **Stage 1 corrected this
document where it was wrong** — see the dependency-ratio row in §3.

Written 2026-08-31 at C-C13. Elias's pre-ruling: **recommend 5-year cohorts unless the sourcing says
otherwise, and carry the collision map plus the "one demography, two consumers" join.**

---

## 1. The recommendation: FIVE-YEAR COHORTS

**Recommended, and the sourcing agrees rather than merely permitting it.** Eurostat's
**`demo_pjan` — "Population on 1 January by age and sex"** (DOI 10.2908/demo_pjan, 1960–2025) publishes
single-year ages, so both are available; the argument for five is not availability but *fitness*:

- The four things cohorts have to drive here — labour-force participation by age, pension cost weight,
  education cost weight, and the election system's voter groups — all move on five-year scales. None of
  them can tell a 42-year-old from a 43-year-old.
- **21 cohorts** (0–4 … 95–99, 100+) per country against 101, at six countries: 126 numbers to seed and
  step per year rather than 606. The step is run every turn on the day loop; the factor of five is real.
- Single-year cohorts invite single-year *rates* (fertility by exact age, mortality by exact age), which
  is a demography build, not a game substrate. **Five-year cohorts keep the model honest about its own
  resolution.**

⚠ **What would overturn this:** if the ruled tax spec-let (C-C12 §S3) needs an income distribution keyed
to age at finer resolution than five years, five-year cohorts become the binding constraint on the tax
build rather than a convenience. Nothing found at C-C13 suggests it does, and the two documents are
written to be ruled together.

---

## 2. The aging step

- **At the YEAR boundary, not the day tick.** `DaysPerTurn` is 365, so one turn is one year and the
  boundary already exists. A day-tick aging step would need fractional cohort movement, which is
  arithmetic nobody can check on screen.
- ⚠ **The step as BUILT at P-I2 stage 2 differs from the four clauses below in three ways, each because
  the data said so.** They are corrected here rather than left to be discovered in the code.
  **(1) Deaths** are not applied separately: the survival ratio is **deaths and net migration together**,
  as the published stock reports them — the fork is D-6, decided and strikeable, and its cost is that the
  immigration lever needs an additive age-profiled term rather than a hook into the survival array.
  **(2) The 1/5 uniform flow is REPLACED by an OBSERVED crossing fraction.** This document called it
  *"the standard, and standardly wrong, approximation"* and was right; since the single-year data that
  would justify 1/5 had to be fetched anyway to fold the pyramids, using it costs nothing and removes the
  assumption. The observed fractions sit near 0.20 in the young bands and fall to **0.076–0.199** in the
  old ones, where the pyramid is steepest and the dependency ratio is decided.
  **(3) Births** come from a **general** fertility rate (births per woman aged 15–49), not age-specific
  rates, because the substrate is sex-blind; the female share of 15–49 is carried as its own sourced
  figure per country, since a hard-coded 0.5 would be invented and none of the six is 0.5 (0.4849 Sweden
  to 0.5023 France).
  ⚠ **One assumption remains and is named**: the open 100+ band's inflow is unobservable in a stock
  series, so the age-99 cohort is assumed to survive at its own band's rate. It governs under 0.03 % of
  every population — and getting it wrong the first time is what the hindcast caught.
- The step as originally specified, in order, per country: (1) deaths applied per cohort from sourced age-specific mortality;
  (2) survivors shifted up one cohort, with a 1/5 flow per year if cohorts are five years wide — ⚠ **this
  is the standard, and standardly wrong, approximation**: it assumes a uniform distribution inside the
  cohort. It is acceptable here and **must be stated in the class's own doc comment** rather than
  discovered later; (3) births into cohort 0–4 from sourced age-specific fertility applied to the female
  cohorts of childbearing age; (4) net migration distributed across cohorts by a sourced age profile,
  not uniformly — migrants are young, and a uniform split would quietly age the population wrongly.

---

## 3. What the cohorts drive

| consumer | today | after |
|---|---|---|
| `LaborForceParticipationRate` | a standalone scalar on `EconomyState` | **derived**: Σ(cohort × sourced participation rate for that cohort) / working-age population |
| `DependencyRatio` | a standalone scalar | **derived**: ⚠ **CORRECTED at P-I2 stage 1 — 65+ / 15–64, the OLD-AGE ratio, NOT the total.** This line originally read *"(0–14 + 65+) / 15–64, the standard definition"*, which is a standard definition but not this model's. Measured: Sweden 33.08 computed vs 33.0 seeded, Germany 35.23 vs 35.0, USA 27.91 vs 28.0 — while the total ratio gives 60.52, 57.14, 55.14. **Building on the original wording would have doubled every country's ratio silently.** |
| `PopulationGrowthRate` | a standalone scalar | **derived**: the aging step's own net result |
| `Population` | a scalar stepped by a rate | **derived**: Σ cohorts |
| pension cost weight | not modelled as such | 65+ share × a sourced per-head cost |
| education cost weight | not modelled as such | 0–19 share × a sourced per-head cost |
| **the election system's voter groups** | **one group, "the electorate"** | **a VIEW over the same cohorts** — see §5 |

---

## 4. ⚠ THE COLLISION MAP

This is the part that makes the build dangerous, and it is why the spec-let exists before the code.
`EconomyState` today carries **eight demographic scalars that would become derived quantities**:

`Population` · `BirthRate` · `DeathRate` · `NetMigrationRate` · `NaturalBirthRate` ·
`NaturalNetMigrationRate` · `DependencyRatio` · `PopulationGrowthRate`

plus two that would be *fed* by cohorts rather than replaced: `LaborForceParticipationRate` and
`LifeExpectancy`.

The collisions, each of which is a way the build goes wrong:

1. **Double-stepping.** Every one of those scalars is stepped today by its own rule. If the cohort step
   is added and the scalar rules are left in, population is advanced twice by different arithmetic. ⚠ The
   build must **delete** the old rules, not run both and reconcile.
2. **`NaturalBirthRate` / `NaturalNetMigrationRate` are ANCHORS, not observations** — each field's own
   doc calls it the *policy-independent trajectory*, and the design is deliberate: holding the slider at
   a fixed value produces a **constant offset from the natural trend rather than a compounding one**, so
   the rate keeps following its secular decline merely shifted. A cohort substrate must keep an
   equivalent anchor, or every demographic policy effect loses its zero **and starts compounding**.
   **This is the single most likely silent breakage.**
3. **The trajectory dump reflects `EconomyState`'s public fields.** Turning eight of them from stepped to
   derived is a **BASELINE change of the largest kind in this project so far**, and the family must be
   explained per country by layer. Expect every country to move.
4. **The player's two demographic levers reach the rates through TWO hops, and both must be re-pointed.**
   `SimulationManager.ApplyDemographicPolicyChanges` writes `Country.FamilyPolicyLevel` and
   `Country.ImmigrationPolicyLevel`; MacroSystem's ApplyDemographicRates (retired at F2 step 4, 2026-09-02 - the substrate steps the rates now: `CohortDemographics.Apply`) then offsets `BirthRate` and
   `NetMigrationRate` off their natural trajectories by those levels. If the cohort step takes over the
   rates and the second hop is not re-pointed at the cohort step's own inputs, **the player's two
   demographic levers become no-ops the day the substrate lands** — the exact failure C-C11 measured for
   the tax dials and S-18 recorded for the rate lever. **Three dead levers would be a pattern, not an
   accident.**
5. **`DependencyRatio` becomes exactly computable**, which means today's seeded value is either right or
   wrong, and the build will find out. That is a feature; it is also a per-country re-seed.

---

## 5. One demography, two consumers

⚠ **The election system's voter groups become a VIEW over the cohort substrate, never a parallel
population.** Today `VoterGroupProfile` (`ElectionTypes.cs:157`) carries a `PopulationShare` and the
live path runs **one group, "the electorate"** — the standing gap C-D1 owns.

The join, stated so it cannot be built as two populations by accident:

- A voter group is defined as a **predicate over cohorts plus a non-demographic axis** (education,
  urban/rural, sector), and its `PopulationShare` is **computed** from the cohorts it covers, never
  seeded independently.
- `TurnoutBase` stays per-group and sourced — turnout by age is real, published, and is exactly the kind
  of thing a shared substrate should carry rather than duplicate.
- ⚠ **The eligible population is not the population.** Voting age, citizenship and residence all cut the
  cohort total, and a group's share must be of the *electorate*, not of the country. Getting this wrong
  inflates every young group.
- **The test that the join held:** Σ(group shares) == 1 to float tolerance, and the sum of the cohorts
  each group covers equals the eligible population. Assert both, at every build, the way the approval
  ledger asserts its identity.

C-D1's sourced per-*valkrets* marginals and this substrate are the same data problem seen twice. **If
C-D1 is billed and closed as billed, this spec-let inherits the bill rather than creating a second one.**

---

## 6. The sourcing bill

| what | source | state |
|---|---|---|
| population by age and sex, all five EU countries | **Eurostat `demo_pjan`** (DOI 10.2908/demo_pjan), `sex=T`, `time=2024`, 1 January 2024 | ✅ **FETCHED at P-I2 stage 1** — single years folded into fives; all five reconcile to the person against the dataset's own TOTAL |
| the same for the USA | **US Census Bureau PEP, vintage 2024, `nc-est2024-agesex-res.csv`**, `POPESTIMATE2024`, `SEX=0` | ✅ **IDENTIFIED AND FETCHED at P-I2 stage 1**, discharging this bill. ⚠ Reference date **1 July 2024**, not 1 January — no 1 January US series exists to match Eurostat, and the offset is stated rather than hidden |
| age-specific fertility | Eurostat `demo_frate` family / national statistical offices | ⚠ **SUPERSEDED at P-I2 stage 2, not discharged.** `demo_frate` was fetched and answers for the EU five; it is not used, because the step takes a **general** fertility rate the substrate can actually apply (see §2). Age-specific rates become buildable the day the substrate carries sex |
| age-specific mortality / life tables | Eurostat `demo_mlifetable` (`PROBSURV` by single year) — ⚠ **and NOTHING equivalent for the USA** | ⚠ **SUPERSEDED at P-I2 stage 2 by D-6.** Fetched and confirmed for the EU five. For the USA: SSA's actuarial table returns **HTTP 403**, the Census PEP API needs a key, the CDC portal has life expectancy and state tables but no national q(x) by age, and the `nc-est2024-alldata-*` files are stock only. One uniform derived method was chosen over the best data for five and something else for the sixth |
| migration by age | Eurostat `migr_imm8` / `migr_emi2` | ⚠ **STILL BILLED, and it is now the ONE thing blocking the immigration lever.** Both fetched and both answer for the EU five; the USA has no equivalent. Needed as an age PROFILE for the additive inflow term, since D-6's survival ratio cannot be split |
| labour-force participation by age | Eurostat `lfsa_argan` 2024 (the five); BLS CPS LNU series 2024 annual (USA) | ✅ **FETCHED at F2 step 5, 2026-09-02** — `ElectionsData/participation/`, `ParticipationRateTable` |
| turnout by age (Sweden) | SCB's *valdeltagandeundersökning* | ⚠ **BILLED** |

⚠ **Written at C-C13 as *"one dataset id asserted, six billed"* — and every one of the seven has now been
OPENED, which is the only way a bill is discharged.** Two are seeded (`demo_pjan`, the Census PEP file),
two are superseded by a method that needs less than they offer (`demo_frate`, `demo_mlifetable`), one is
now the single blocker on the immigration lever (`migr_imm8`/`migr_emi2`), and two remain genuinely
untouched (participation by age, turnout by age). ⚠ **A dataset code recalled rather than checked is an
invented figure wearing a technical costume**, and that is why the two that are still billed say BILLED.

---

## 7. Sizing (P-I2)

| block | size |
|---|---|
| fetch and normalise the pyramids, six countries | 2 sessions |
| fetch the four rate families (fertility, mortality, migration, participation) | 2–3 sessions |
| the cohort type, the aging step, and its own harness | 2 sessions |
| ⚠ retiring the eight scalars, with the explained baseline family per country | **2–3 sessions, and it is the risky half** |
| re-pointing the two demographic policy levers (§4.4) | 1 session |
| migrating the voter groups onto the substrate + re-running the backtest to prove no regression | 2 sessions |

**Total: roughly 11–13 sessions**, of which the scalar retirement is the one that can go wrong quietly.
**Not to be started before this spec-let is ruled, and not in the same pass as C-C12's build** — two
BASELINE families landing together cannot be explained apart.
