# wave/fisc - the registered passes for the K60-K64 / K98 measuring run (plan_prereg, 2026-10-24)

Drafted by the implementer of `wave/fisc` for the integrator to commit into `docs/specs/A_LOOP_PLAN.md` BEFORE the measuring run
(`Tools/audit/aloop_fisc.plan`, read with `perl Tools/audit/aloop_record_check.pl <the run's log> <its night folder>`). Rulings:
`docs/RULINGS_2026-10-23.md` K60-K64; K98 and K99 (Elias, 2026-10-24, ask mode).

## Into section 3 (4) (b) - q15's pass, as K60 restates it

> (K60) On a2_se_big the RECORD line names the approval term with its economy stock, and its size equals 0.16 x the printed net change
> (net approval on the count's day less net approval when the government took office) within 0.01 pp. Whether the governing side holds
> 175 of 349 seats on the night is a reading, printed, not a pass or a fail.

## Into section 4 (4) - q13's pass, replacing the surplus clause (K61), with K62-K64 and K98

> (K61) "A pole whose balance improves across the window gains": where BALCH_term >= 0 and BALCH_ey >= 0 (one of them > 0), as the RECORD
> line prints them, the government side's fiscal term is > 0; where both are <= 0 (one < 0) it is < 0; a window that improves on one and
> worsens on the other is a reading. Each term equals clamp(sigma_v x delta-z, +/-10 pp) (K64: sigma_v the MAD-based scale), recomputed from
> the BAL-i the line itself prints, within 0.01 pp. (K98) Where every year a line reads is sourced (no game year has closed under the
> tenure), its BAL0 is the source's too and the line names that source year. Measured on `Tools/audit/aloop_fisc.plan`: the Swedish poles at
> their second count (2030), the German poles at their first (2025-02-23), now that the first count carries the sourced years (K62) - each
> year read only under the government's own tenure, the whole year (K63, confirmed as K99).

## Updated for K100 and K101 (2026-10-24, the fix round)

In the section 4 (4) text above, read "each year read only under the government's own tenure, the whole year (K63, confirmed as K99)" as
"each year read only under the governing PARTY's own tenure, the whole year (K63, K99, K101)". Add after it:

> (K100) No figure from after the election reaches the vote: the pre-run years are the source years that had closed by the start, and the first
> count's BAL0 is the source's figure for the election's calendar year (the USA: the federal fiscal year of that number). (K101) A re-elected
> government's own previous years count, so the four-year form applies after a full term under the same governing party; a change of party
> restarts the tenure.

## The plan's restatements (L3 of the review) - for the integrator to apply to `docs/specs/A_LOOP_PLAN.md`

**Section 3 (4) (b)**, replace the bullet "(b) A 1.5 % government loses: ... split by responsibility." with:

> (b) **(K60, ruled 2026-10-23)** On a2_se_big the RECORD line names the approval term with its economy stock and b = 0.16; its size equals
> 0.16 x the net change it prints (net approval on the count's day less net approval when the government took office) within 0.01 pp. Whether
> the governing bloc falls below 175 of 349 is recorded as a reading, not a pass or a fail.

**Section 4 (3)**, replace the sigma_v bullet's first sentence with:

> **sigma_v: FITTED, a ROBUST scale (K64, ruled 2026-10-23).** The MAD-based scale (1.4826 x the median absolute deviation from the median) of the
> PM's (US: the president's) party's vote-share change between consecutive elections over the estimating sample's frame - the swings are
> fat-tailed (kurtosis printed by `FiscalVoteFitCheck`), so the standard deviation and the IQR-based scale are printed beside it, not read.

and add a bullet after the support bound:

> - **The share cap: [AUTHORED-DRAFT] (K64, interim).** shift_gov, the government side's term, is held within +/-10 pp of share before it is split.

**Section 4 (4)**, replace "the surplus poles gain;" with:

> - (K61, ruled 2026-10-23) a pole whose balance improves across the window gains, one whose balance worsens loses, and a mixed window is a
>   reading - the variables are changes, never levels;

and replace the "Fixed before the run (2026-10-19 ...)" bullet's final **Pass** sentence with the section 4 (4) text of this file.

**Section 4 (2)**, add after the BALCH_ey bullet:

> - **The years before the start (K62, K100):** the four newest source years that had closed by the start (Eurostat's B9, S13, PC_GDP for the EU
>   five; OMB's Table 1.2 for the USA), `ElectionsData/macro/fiscal_balance.md`. **The first count (K98, K100):** until a game year closes, BAL0 is
>   the source's figure for the election's calendar year. **Tenure (K63, K99, K101):** a year counts only where the whole year fell under the
>   governing party's tenure, which runs through re-elections under the same party.

**Section 4's "Built" line**, append:

> **Re-built (2026-10-24, `wave/fisc`):** K60-K64, K98, K100, K101 - the sourced pre-run years, the party's tenure, the MAD-based sigma_v,
> the +/-10 pp cap; format 61.

## The calibration-list entry (section 346's list)

> *"q13's share cap: the fiscal term held within +/-10 pp of the government side's share, interim (K64), until the support bound is measured on
> Brender & Drazen's replication data."*
