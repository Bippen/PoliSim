# Step C1 (Housing) — metric decided, basis fixed, blocked on three figures

**Status: BLOCKED on 3 lookups. No implementation started.**
**REVISED three times on 2026-08-01** — after the variant correction, after Elias's metric decision, and
after the homeownership basis re-check. The sequence is kept at the bottom because it is the useful part.

---

## DECIDED — homeownership rate is C1's primary housing metric

**This reverses the original directive**, which recommended housing cost overburden. Elias's call.

Overburden remains the **better concept** — it measures affordability *stress* rather than tenure, and
responds to interest rates and housing assistance, both already live levers here. That reasoning was
never wrong and still isn't. It lost on data honesty: 2 of 6 verified means seeding four countries from a
range, and a bound is not a value.

## DECIDED — the basis is OECD Affordable Housing Database, share of HOUSEHOLDS owning

A re-check confirmed the earlier table mixed incompatible bases. Germany alone appears three ways:

| Basis | Germany |
|---|---|
| OECD, share of households owning | **41.0** |
| Dwelling-based (2022) | ~46.7 |
| Eurostat, nationals only | 52.3 |

An **11.3-point spread** across three definitions, every one correct for its own source. Eurostat's
population-based measure (68.4% EU) is a fourth. A set mixing these would encode measurement artifacts as
if they were real national differences.

**Single-basis set now recorded:** USA 65.3, France 58.5, Germany 41.0, OECD average 70.1.

---

## ⚠ The margin is narrower than this report previously claimed

The previous version of this report justified the decision on **4-of-6 coverage** for homeownership
against 2-of-6 for overburden. **That was too strong.** On a single consistent basis, homeownership has
**3 of 6** — Poland's ~87.9 turned out to be a Eurostat *nationals* line, not the OECD household basis,
so it leaves the verified set.

**Coverage is 3–2, not 4–2.** The decision **holds** — 3 verified same-basis figures still beat 2, and
overburden's three missing countries are unobtainable by search while homeownership's are ordinary
lookups — but it holds by one country rather than two.

A second claim also needs correcting. I previously argued the decision preserved "the sharpest real
contrast in the data, **Germany ~47% against Poland ~87%**." That specific pair is **not usable as
stated**: the two figures come from different bases, which is precisely the error being corrected. What
survives is stronger for Germany and weaker overall:

- **Germany 41.0 against an OECD average of 70.1** is a real same-basis contrast, and *more* extreme than
  the ~47 figure suggested. The structural outlier that makes housing policy play differently in Germany
  is intact and better evidenced than before.
- **Poland's position is directional only** until sourced on the OECD basis — genuinely among the highest
  globally (top 10, alongside Lithuania, Bulgaria and Latvia), but without a same-basis number the
  Germany-vs-Poland spread cannot be quoted.

---

## The three remaining gaps — all on the OECD household basis

| # | Country | Note |
|---|---|---|
| 1 | **Poland** | `[PARTIAL]` — confirmed top-10 globally, but no exact OECD figure. The ~87.9 in this project's files is Eurostat nationals, not same-basis |
| 2 | **Italy** | `[GAP]` |
| 3 | **Sweden** | `[GAP]` |

Have: USA 65.3, France 58.5, Germany 41.0. Same-basis anchors for sanity-checking whatever comes back:
OECD average 70.1, Slovakia 93.5 (highest), Canada 68.6, Australia 62.7, Switzerland 38.2 (lowest).

Earlier indicative ranges for Italy (~72–73) and Sweden (~63–65) were on unknown bases and **must not be
used** — they are not merely imprecise, they may be measuring something else.

Each row should record its basis explicitly when sourced. That is instance 7's lesson applied forward
rather than after the fact.

## Not blocking

- **USA overburden** — deferred with overburden itself; homeownership gives the USA a comparable 65.3.
- **House Price Index** — `[GAP]` but resolved by convention: all six seed at index 100 and diverge.

---

## How this arrived here

Six successive claims about housing coverage, each correcting the last:

| Stage | Claim | Verdict |
|---|---|---|
| Directive | Overburden 6 of 6 | Overstated |
| Seed file, original | Overburden 4 of 6 | Wrong variant — "two adults" subset |
| First gap report | Overburden 4 of 6 | Caught the directive; trusted the numbers |
| Corrected + gap-closing | Overburden **2 of 6** | Correct |
| Metric decision | Homeownership 4 of 6 | Overstated — mixed bases |
| Basis re-check | Homeownership **3 of 6** | Correct |

Both metrics were overstated at first, and in both cases the error was invisible to a check against the
documented warning. See `CLAUDE.md`, verification-integrity instance 7 and its refinements, for why —
this is now the third indicator where an undocumented variant axis existed.

**C1 does not start until the three figures are supplied on the OECD household basis.** I have no web
access and will not infer a value from a range, a bound, or a figure on another basis.
