# Step C1 (Housing) — metric decided, blocked on two figures

**Status: BLOCKED on 2 lookups. No implementation started.**
**REVISED twice on 2026-08-01** — first after the seed-data variant correction, then after Elias's
metric decision. History kept at the bottom, because the sequence is the useful part.

---

## DECIDED — homeownership rate is C1's primary housing metric

**This reverses the original directive**, which recommended housing cost overburden. Elias's call, and
the reasoning is a data-honesty one rather than a modelling one:

Overburden remains the **better concept**. It measures affordability *stress* rather than tenure, and it
responds to interest rates and housing assistance — both already live levers in this game, which is
exactly what would have made it play well. None of that changed.

What decided it is that overburden has **2 of 6 verified coverage** (Germany 12.0, Sweden 10.6). Italy,
France and Poland are *bounded* between 4.0 and 9.0 — an honest constraint derived from Eurostat naming
only the countries above 9.0 and below 4.0 — but a bound is not a value. Seeding four countries from a
range means inventing precision, which is the exact thing the `[GAP]` discipline exists to prevent.

Homeownership has **4 of 6 verified coverage** and preserves the most interesting real contrast in the
data: **Germany ~47% against Poland ~87%**. That spread is genuine, culturally rooted (German rental
culture, rent control, no mortgage interest deduction; Polish post-communist privatization), and it makes
housing policy play differently in the two countries without any invented figure.

**Overburden is deferred, not dropped.** It becomes a secondary metric if exact `ilc_lvho07a`
whole-population figures are obtained. That needs direct Eurostat database access — search cannot do it,
and this was attempted and failed.

---

## BLOCKING PREREQUISITE — fix the measurement basis before sourcing anything

**Do not source Italy and Sweden yet.** Homeownership has the same variant problem that produced
verification-integrity instance 7, it has not been re-checked, and it is now C1's primary metric.

At least three axes: Eurostat measures share of *population* in owner-occupied dwellings (EU 68.4%),
while US Census and most OECD reporting measure share of *households* owning; Eurostat separately splits
nationals-only from all residents; and reference years are mixed (2022 / 2024 / 2025).

**Inspection of the existing table suggests the mixing may already be there** — no new sourcing needed to
see it:

- **Germany carries two figures on two bases**, 46.7 (2022) and "Eurostat nationals-only 52.3 (2024)",
  a 5.6-point spread. The row settles on ~47 without stating which basis that is.
- **Poland ~87** sits against a Eurostat line elsewhere in the same file reading "Poland nationals
  87.9%". If that is where ~87 came from, Poland is on Eurostat nationals-only while USA and France are
  on OECD.
- **USA 65.3–65.9** matches the US Census homeownership rate, which is household-based — not Eurostat's
  population base.

Each figure is likely correct for its own source. The set may still not be internally comparable, which
is exactly the trap. Sourcing two more figures before fixing this would add a fourth and fifth variant
rather than completing a set.

*Recommended basis: OECD household-based*, since USA and France are already there and the USA has no
Eurostat figure at all. Raised as an Open Question rather than settled, since it may require re-sourcing
Germany and Poland.

## The two remaining gaps — once the basis is fixed

| # | Figure | Country | Difficulty |
|---|---|---|---|
| 1 | Homeownership rate | **Italy** | Straightforward lookup — seed file indicates ~72–73 |
| 2 | Homeownership rate | **Sweden** | Straightforward lookup — seed file indicates ~63–65 |

Have: USA 65.3–65.9 (OECD), France 58.5 (OECD), Germany ~47 (basis unclear), Poland ~87 (basis unclear).

The indicative ranges above are **sourcing hints, not values**, and will not be used as figures. Both
must come from whichever basis is chosen, and each row should record that basis explicitly — the lesson
of instance 7 applied forward rather than after the fact.

## Not blocking any more

- **USA overburden** — was a methodology decision (Eurostat >40% vs US >30%/>50%, nothing comparable).
  With homeownership primary, the USA has a genuinely comparable verified figure at 65.3%. The question
  is deferred alongside overburden itself rather than being resolved.
- **House Price Index** — marked `[GAP]` but resolved by convention: seed all six at index 100 and let
  divergence emerge. A standard index convention, not an invented figure.

---

## How this arrived here — worth keeping

Four successive claims about overburden coverage, each correcting the last:

| Source | Coverage | Verdict |
|---|---|---|
| The directive | 6 of 6 ("all EU five", "complete EU coverage") | Overstated |
| Seed file, original figures | 4 of 6 | **Wrong variant** — "two adults" subset, not whole population |
| My first gap report | 4 of 6 | Caught the directive; trusted the underlying numbers |
| Corrected + gap-closing attempt | **2 of 6** verified, 3 bounded, 1 unobtainable | Correct |

The first report was right to flag the directive and wrong about the number, in the same direction it was
already investigating — coverage was worse than its pessimistic reading. See `CLAUDE.md`,
verification-integrity instance 7, for why this indicator in particular defeated ordinary sourcing care:
at least eight Eurostat variants publish under one name, and secondary sources reproduce different ones
without labelling which.

**C1 does not start until the two homeownership figures are supplied.** I have no web access and will
not infer a value from an indicative range or a bound.
