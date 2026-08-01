# Step C1 (Housing) — `[GAP]` figures Elias needs to source

**Status: BLOCKED pending these figures. No implementation started.**

The directive's standing rule for Step C is explicit: *"Before each batch: report which `[GAP]` figures
that batch needs, so Elias can source them. Do not proceed on invented numbers."* This is that report.

---

## First: the directive and its own seed data disagree about coverage

The directive states that housing cost overburden rate has **"complete EU coverage"** and is available
for **"all EU five"**, and recommends it as the primary housing metric partly on that basis.

**That is not what the seed file contains.** The verified Eurostat 2024 line reads:

> Greece 24.2, Germany 9.7, Hungary 7.9, Euro area 6.4, Poland 6.1, Sweden 5.1, France 3.9

Greece and Hungary are not playable countries. **Italy is absent.** So real coverage for the six
playable countries is **4 of 6** (Germany, Poland, Sweden, France) — not five, and not complete.

This does not overturn the recommendation. Overburden is still the better primary metric for the reasons
the directive gives: it measures affordability *stress* rather than tenure, and it responds to interest
rates and housing assistance, both of which are already live levers in this game. But it is a 4-of-6
stat, not a 5-of-6 one, and the batch cannot be planned as though Italy were covered.

---

## Gap 1 — Housing cost overburden rate, **Italy**

| Have | Germany 9.7 · Poland 6.1 · Sweden 5.1 · France 3.9 (Eurostat 2024, share spending >40% of income on housing) |
|---|---|
| **Need** | **Italy**, same Eurostat measure and year |

This should be directly obtainable — Italy is a standard Eurostat reporter and the figure exists in the
same table the other four came from. It was simply not captured during sourcing.

## Gap 2 — Housing cost overburden rate, **USA** — *and a methodology decision, not just a number*

The USA has no Eurostat figure by definition. The closest official US equivalent is the Census/HUD
**"cost-burdened"** measure, and it is **not the same statistic**:

- Eurostat overburden: households spending **>40%** of disposable income on housing.
- US cost-burdened: **>30%** of income. "Severely cost-burdened" is **>50%**.

Neither US threshold matches 40%, so no US figure can be dropped into this field without a decision
about what is being compared. Three honest options:

1. **Source a >40% US figure directly** if one exists in the underlying microdata reporting. Cleanest,
   but may not be published at that threshold.
2. **Use the US severe (>50%) figure** and document that the USA's number is measured on a stricter
   threshold — it will read *lower* than reality relative to the EU five, biasing the USA to look better
   than it is.
3. **Use the US >30% figure** and document the opposite bias — it will read substantially *higher* than
   the EU five, making US housing stress look worse than a like-for-like comparison would show.

This is the same trap the seed file already flags for Gini ("normalize the scale first... or the US will
look artificially different for measurement reasons rather than real ones"). Option 1 is worth one
lookup; failing that, **option 2 with explicit documentation** is preferable to option 3, because a
conservative bias is easier to reason about than an alarming one. Flagging for Elias rather than
choosing.

## Gap 3 — Homeownership rate, **Italy** (~72–73, per the seed file's own note)

## Gap 4 — Homeownership rate, **Sweden** (~63–65, per the seed file's own note)

Both are marked `[GAP]` in the seed file with indicative ranges attached. The ranges are **not usable as
values** — they are the sourcing hint, not the source. Have: USA 65.3–65.9 (OECD), France 58.5 (OECD),
Germany ~47, Poland ~87.

---

## Not a gap — House Price Index

The seed file marks HPI `[GAP]` but then resolves it by convention: **seed all six at index 100 at game
start** and let divergence emerge. That is a standard index convention, not an invented figure, so no
sourcing is required. Recorded here only so it is not mistaken for an outstanding item.

---

## Summary

| # | Figure | Country | Difficulty |
|---|---|---|---|
| 1 | Housing cost overburden rate (Eurostat 2024, >40%) | Italy | Straightforward — same table as the other four |
| 2 | Housing cost overburden rate | USA | **Needs a methodology decision, not just a lookup** |
| 3 | Homeownership rate | Italy | Straightforward — OECD, same source as USA/France |
| 4 | Homeownership rate | Sweden | Straightforward — OECD |

Three lookups and one decision. Germany's ~47% homeownership is already verified and will carry into the
model as the directive requires, so the outlier that makes housing policy play differently there is not
at risk from any of these gaps.

**C1 does not start until 1–4 are supplied.** I have no web access and will not infer a value from the
indicative ranges above.
