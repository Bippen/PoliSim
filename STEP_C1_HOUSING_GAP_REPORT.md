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

## The two remaining gaps

| # | Figure | Country | Difficulty |
|---|---|---|---|
| 1 | Homeownership rate | **Italy** | Straightforward OECD lookup — seed file indicates ~72–73 |
| 2 | Homeownership rate | **Sweden** | Straightforward OECD lookup — seed file indicates ~63–65 |

Have: USA 65.3–65.9 (OECD), France 58.5 (OECD), Germany ~47, Poland ~87.

The indicative ranges above are **sourcing hints, not values**, and will not be used as figures. Both
should come from OECD, the same source as the USA and France entries, so the six are measured on one
basis.

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
