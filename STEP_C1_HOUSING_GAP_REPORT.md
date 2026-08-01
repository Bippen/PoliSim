# Step C1 (Housing) — `[GAP]` figures Elias needs to source

**Status: BLOCKED. No implementation started.**
**REVISED 2026-08-01** after a correction to `POLISIM_SEED_DATA_MACRO_OVERHAUL.md`. The first version of
this report was built on figures that turned out to be the wrong Eurostat variant — see below.

The directive's standing rule for Step C is explicit: *"Before each batch: report which `[GAP]` figures
that batch needs, so Elias can source them. Do not proceed on invented numbers."* This is that report.

---

## The coverage question is now resolved — and it went the opposite way

Three different claims have been made about housing cost overburden coverage for the six playable
countries. Only the third is correct:

| Source | Claim | Verdict |
|---|---|---|
| The directive | "complete EU coverage" / "all EU five" | **Wrong** — overstated |
| Seed file, original | Germany 9.7, Poland 6.1, Sweden 5.1, France 3.9 → 4 of 6 | **Wrong variant entirely** |
| Seed file, corrected | Germany 12.0, Sweden 10.6 → **2 of 6** | Correct |

My previous report caught the directive's overstatement and concluded coverage was 4 of 6. That was
right to flag and wrong in its number: the four figures it trusted were Eurostat's **"Two adults"**
household subset, not the headline whole-population indicator (`ilc_lvho07a`). The gap between variants
is not cosmetic — Sweden is 5.1 on the two-adults measure and **10.6** whole-population, more than 2x.

**Real whole-population coverage is 2 of 6: Germany 12.0 and Sweden 10.6.**

---

## This changes the metric decision, and it is Elias's to make

The directive recommended housing cost overburden as the **primary** housing metric over homeownership
rate, and gave two reasons: it measures affordability *stress* rather than tenure, and it responds to
interest rates and housing assistance — both already live levers in this game. Both reasons remain
sound. What has collapsed is the third, unstated premise: that it was the better-covered stat.

Coverage now runs the other way:

| Metric | Verified | Gaps |
|---|---|---|
| Housing cost overburden | **2 of 6** — Germany, Sweden | Italy, France, Poland (all known <9.0), USA (not a lookup) |
| Homeownership rate | **4 of 6** — USA 65.3–65.9, France 58.5, Germany ~47, Poland ~87 | Italy, Sweden |

Homeownership is now the better-covered metric by 2x, and its remaining gaps are ordinary OECD lookups
rather than methodology problems. Overburden's gaps include one country (USA) where no comparable figure
exists at any threshold.

**Escalated to Open Questions rather than decided.** The options as I see them:

1. **Switch primary to homeownership rate**, with overburden riding alongside as the affordability
   signal once sourced. Best coverage today; loses the direct interest-rate responsiveness that made
   overburden attractive, since homeownership is structurally slow-moving.
2. **Keep overburden primary and source the three EU gaps.** All three are known to sit below 9.0, so
   they exist and are obtainable — this is three lookups, not a dead end. USA still needs a separate
   decision.
3. **Use a different primary per country** — overburden where available, homeownership for the USA. The
   seed file's own option 3 for the USA. Honest about the data, but means the headline housing number
   is not comparable across countries, which is precisely the trap the file warns about for Gini.

*No recommendation offered here*, because option 1 versus 2 turns on whether interest-rate
responsiveness or data coverage matters more for how C1 should play, and that is a design judgment about
the game rather than a data question.

---

## The gaps themselves

### If overburden stays primary — 4 gaps

| # | Country | Difficulty |
|---|---|---|
| 1 | Italy | Straightforward — known <9.0, same Eurostat table (`ilc_lvho07a`, whole population) |
| 2 | France | Straightforward — known <9.0, same table |
| 3 | Poland | Straightforward — known <9.0, same table |
| 4 | **USA** | **A decision, not a lookup** — see below |

### If homeownership becomes primary — 2 gaps

| # | Country | Difficulty |
|---|---|---|
| 1 | Italy | Straightforward — OECD, ~72–73 indicated |
| 2 | Sweden | Straightforward — OECD, ~63–65 indicated |

Indicative ranges in the seed file are **sourcing hints, not values**. They will not be used as figures.

### The USA overburden decision

Eurostat measures >40% of disposable income. US convention is >30% ("cost-burdened") or >50%
("severely"). Nothing matches. The seed file lays out three options: import with the bias documented,
mark USA `[GAP]` and seed only the EU five, or use homeownership for the USA instead. **Escalated to
Elias** rather than chosen — every option changes what the number means for one of six countries.

---

## Not a gap — House Price Index

Marked `[GAP]` in the seed file but resolved by convention: seed all six at index 100 at game start and
let divergence emerge. A standard index convention, not an invented figure. Recorded so it is not
mistaken for outstanding work.

---

## Unaffected

Germany's ~47% homeownership — lowest in Europe, and the outlier that makes housing policy play
differently there — is verified and survives every option above.

**C1 does not start until the metric decision is made and its gaps are supplied.** I have no web access
and will not infer a value from an indicative range.
