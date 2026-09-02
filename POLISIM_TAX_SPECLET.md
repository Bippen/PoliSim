# The tax spec-let and sourcing bill (C-C12 / P-H1)

**Status: DOCUMENT ONLY. No tax code is built from this until Elias rules on it.** Every line below is
meant to be struck, amended or approved on its own.
**Status: RULED 2026-08-31 (Elias, D-3): (c) NOW, (b) WHEN IT RUNS.** Neither branch is built yet, and
the branch that will be built when it runs is **(b) pluggable schedules** — not (a)'s bracket table.

⚠ **Why (c) is not a deferral in disguise.** The item is **blocked on D-4 whichever branch is chosen**:
**a bracket schedule applied to a single average income is arithmetically identical to a flat rate**, so
until the cohort substrate can carry an income distribution there is nothing for brackets to bracket.
Buying (a)'s speed would therefore cost three misrepresented countries **for no time gained** — Germany's
tariff is a *formula* (§32a EStG), France's is a *quotient familial*, Italy's is three layers, and no
bracket table represents any of them.

⚠ **The trigger, so this row is re-readable a month from now:** P-I2 reaches the point where the cohort
substrate carries an income dimension. Not a date. **P-I2 stages 1 and 2 have landed** (the pyramids and
the aging step); the substrate is currently **sex-blind and income-blind**, so the trigger has NOT fired.

⚠ **One thing this ruling does NOT foreclose, and one it does.** It does not foreclose sourcing: §3's
five billed countries can be sourced at any time, and that work is not wasted whichever branch runs. It
does foreclose writing bracket tables into `TaxLine` as data, which is (a), and which would have to be
unpicked by (b).

---

Written 2026-08-31 at C-C12. The item's own instruction is *"one document before any code"*, and the
pre-ruling adds: documents only.

---

## 1. What the model has today

`Country.TaxLines` holds **twelve `TaxLine`s**, seeded per country by `WorldFactory.SeedTaxLines`
(`:1022`). Each is `(TaxType, Rate, IsImplemented)` — **one flat rate per instrument, no brackets, no
thresholds, no base definition**. The player sets a rate as an absolute target
(`PolicyDecision.TaxRateOverrides`, clamped to that type's `TaxTypeRateRanges`); revenue is computed by
`SimulationManager.GetTotalTaxRevenue`, and `Country.CollectionEfficiency` scales the theoretical figure
into the actual one.

Sweden is seeded `IncomeTax 52`, `CorporateTax 20.6`, `VAT 25`.

⚠ **The single Swedish "income tax" of 52 is already a blend** — municipal plus state, at the top. That
is a defensible reading of the headline rate, but it means the dial the player moves is **the top
marginal rate applied to all income**, which is neither of the two real instruments and cannot be
reconciled with either without brackets. Naming that is half the reason this document exists.

⚠ **The other half is C-C11's finding:** the tax multiplier in this model is **exactly 0.000** at every
horizon (`COMPLETED.md` §107). Making the tax instruments realistic and leaving them with no output
channel would produce a more detailed model of a lever that still does nothing. **The two items are
coupled, and the coupling should be ruled on before either is built.**

---

## 2. The real instruments — Sweden (SOURCED)

Every figure here was read from a primary or near-primary source at C-C12 and carries its vintage.

| instrument | 2026 figure | source |
|---|---|---|
| Kommunal skattesats, national average | **32.38 %** (lowest Österåker 28.93, highest Dorotea 35.65) | SCB, *Kommunalskatterna 2026* |
| Statlig inkomstskatt | **20 %** above the brytpunkt | Skatteverket, *Belopp och procent 2026* |
| Brytpunkt, under 66 | **660 400 kr** | Skatteverket, ibid. |
| Brytpunkt, 66+ | **760 500 kr** | Skatteverket, ibid. |
| Skiktgräns | **643 000 kr** | Skatteverket, ibid. |
| Arbetsgivaravgifter, born 1959+ | **31.42 %** | Skatteverket, ibid. |
| Egenavgifter, born 1959+ | **28.97 %** | Skatteverket, ibid. |
| Moms | **25 / 12 / 6 %** | Skatteverket, ibid. |
| Särskild löneskatt | **24.26 %** | Skatteverket, ibid. |
| Prisbasbelopp | **59 200 kr** | Skatteverket, ibid. |
| Bolagsskatt | **20.6 %** | secondary sources only at C-C12 — ⚠ **BILLED** to Skatteverket's own page |
| Kapitalinkomstskatt | commonly 30 % | ⚠ **NOT VERIFIED** at C-C12 and therefore **NOT ASSERTED HERE** |

⚠ **Two rows are deliberately unfilled rather than filled from memory.** The standing rule is that no
figure is invented; a rate everybody knows is still a rate somebody has to check.

## 3. The real instruments — the other five (BILLED, NOT SOURCED)

Nothing is written for the USA, Germany, France, Italy or Poland, because nothing was checked. The bill
below names where each must come from; **this is the deliverable, not a placeholder for one.**

| country | primary source to fetch | what to take |
|---|---|---|
| USA | IRS Revenue Procedure (annual, inflation-adjusted brackets); Social Security Administration for the OASDI wage base | federal brackets + rates, standard deduction, payroll rate and cap, corporate rate. ⚠ **State income tax is a second layer this model has no place for** — a design question, not a data one | ✅ **SOURCED 2026-09-02 — Rev. Proc. 2025-32 (tax year 2026), `ElectionsData/tax/README.md`; ⚠ the OASDI wage base still BILLED (ssa.gov refuses a batch fetch)** |
| Germany | Bundesministerium der Finanzen; §32a EStG | the *Einkommensteuertarif* is a FORMULA, not brackets — a genuinely different shape from every other country here, and the strongest argument for "brackets as data" being insufficient | ✅ **SOURCED 2026-09-02 — §32a EStG for 2026, verbatim in `ElectionsData/tax/README.md`** |
| France | Direction générale des Finances publiques, *barème de l'impôt sur le revenu* | brackets, the *quotient familial* (household-size division — a second genuinely different shape), CSG/CRDS | ✅ **SOURCED 2026-09-02 — barème 2026 (2025 income) from economie.gouv.fr and service-public.fr, `ElectionsData/tax/README.md`; ⚠ CSG/CRDS still BILLED** |
| Italy | Agenzia delle Entrate | IRPEF brackets, plus the regional and municipal *addizionali* — a three-layer instrument | ⚠ **STILL BILLED 2026-09-02 — the Agenzia and normattiva pages are JavaScript portals that hand a batch fetch no article text; a primary PDF is the ask** |
| Poland | Ministerstwo Finansów / KAS | PIT brackets, the *danina solidarnościowa*, ZUS contributions | ✅ **PIT scale SOURCED 2026-09-02 — podatki.gov.pl, `ElectionsData/tax/README.md`; ⚠ danina solidarnościowa and ZUS still BILLED** |
| Sweden | SCB + Skatteverket (§2 above) | done, bar the two billed rows |

⚠ **Three of the six do not fit a flat bracket table** (Germany's formula, France's quotient familial,
Italy's three layers). A design that assumes "brackets as data" will fit Sweden, Poland and the USA and
will misrepresent the other three. **This is the single most important thing in this document.**

---

## 4. The design, to be struck or approved line by line

- **S1. Granularity: brackets as data, per instrument, per country.** A `TaxBracket { Threshold, Rate }`
  list on `TaxLine`, empty meaning "flat, as today". Backwards-compatible; every existing call site keeps
  working on the flat path.
- **S2. ⚠ S1 is NOT sufficient for Germany, France or Italy.** Either (a) accept an approximation for
  those three and **say so on the screen**, in the `[AUTHORED-DRAFT]` idiom, or (b) give `TaxLine` a
  pluggable schedule with three implementations. (a) is cheap and honest; (b) is correct and is a
  sizeable build. **This is the first thing to rule on.**
- **S3. Revenue needs an income DISTRIBUTION, which this model does not have.** A bracket schedule
  applied to a single average income is arithmetically identical to a flat rate at the average's bracket
  — i.e. building S1 without a distribution buys nothing. The distribution comes from **the cohort
  substrate (C-C13 / P-I1)** or from an interim sourced decile table. ⚠ **Therefore the tax spec-let is
  DOWNSTREAM of the cohort spec-let, not parallel to it.**
- **S4. Player granularity: the player moves a bracket's RATE, not its threshold**, in the first
  version. Thresholds are indexed data that real governments move rarely and by formula, and making them
  draggable multiplies the design surface for very little play.
- **S5. Mapping onto the existing budget decompositions.** Today's revenue lines are per `TaxType`; a
  bracketed instrument still reports one revenue figure per `TaxType`, so **the decompositions need no
  change**. This is a deliberate constraint on S1: any design that changes the revenue line's shape also
  changes the fiscal ledger, the debt attribution and the budget screens, and is a much larger item.
- **S6. Sweden's blended 52 is retired at this build**, replaced by kommunal + statlig as two
  instruments. ⚠ **BASELINE**: it changes seeded revenue and therefore every Swedish trajectory. The
  family must be explained per country under the full sim-math bar.
- **S7. Do not build S1–S6 while the tax multiplier is zero.** C-C11's R-C11a is the prerequisite: a
  detailed instrument with no output channel is detail without consequence. **Strike this line if Elias
  wants the instruments first as data work.**

---

## 5. Sizing

| block | size | depends on |
|---|---|---|
| The five countries' sourcing (§3) | **one session per country**, three of them (DE/FR/IT) longer because the instrument shape is not a bracket table | nothing |
| S1 + S5 (brackets as data, flat path preserved) | one session | S3's distribution |
| S2(b) pluggable schedules | two to three sessions | S2 ruled |
| S3 interim decile distribution | one session; **free if C-C13 is built first** | C-C13 |
| S6 Sweden re-seeding + explained family | one session | S1 |

**Total, if S2(a) is ruled: roughly eight sessions. If S2(b): eleven.** Neither should start before
C-C13, and neither should start before C-C11's R-C11a is ruled.
