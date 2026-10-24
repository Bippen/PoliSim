# wave/fisc - the registered passes for the K60-K64 / K98-K105 measuring run (plan_prereg, version 2, 2026-10-24)

Drafted by the implementer of `wave/fisc` for the integrator to commit into `docs/specs/A_LOOP_PLAN.md` BEFORE the measuring run
(`Tools/audit/aloop_fisc.plan`, read with `perl Tools/audit/aloop_record_check.pl <the run's log> <its night folder>`). Rulings:
`docs/RULINGS_2026-10-23.md` K60-K64; K98-K101 and K104-K105 (Elias, 2026-10-24, ask mode). **Version 2 supersedes version 1** (after the
second review, `Reviews/2026-10-24_fisc_review2.md`: H1, M1, L2, L3, L5, L6, and K104, K105).

## The registered passes

**Section 3 (4) (b) - q15's pass, as K60 restates it.**

> (K60) On a2_se_big the RECORD line names the approval term with its economy stock and b = 0.16; its size equals 0.16 x the printed net change
> (net approval on the count's day less net approval when the government took office) within 0.01 pp. Whether the governing side holds 175 of 349
> seats on the night is a reading, printed, not a pass or a fail.

**Section 4 (4) - q13's pass, replacing the surplus clause.**

> (K61) "A pole whose balance improves across the window gains": where BALCH_term >= 0 and BALCH_ey >= 0 (one > 0), as the RECORD line prints
> them, the government side's fiscal term the GAME prints is > 0, and where both are <= 0 (one < 0) it is < 0 - a term too small to print
> (|sigma_v x delta-z| < 1e-4 pp) is a reading; a window that improves on one and worsens on the other is a reading. (K64) Each term equals
> clamp(sigma_v x delta-z, +/-10 pp), sigma_v the MAD-based scale, recomputed from the BAL-i the line itself prints, within 0.01 pp.
> (K98, K100, K105) On a first count (no game year closed) the line names the election's year E and reads every year from the source: BAL0 is E,
> each BAL-i is older than E, no source year is read twice, and on a count line E is the count's own year; a year still open on the count's day
> is never read, E excepted (a declared hindsight). (K63, K99, K101) Each year is read only where the whole year fell under the governing party's
> tenure, which runs through re-elections under the same party; a line reading four years prints the four-year form. Measured on
> `Tools/audit/aloop_fisc.plan`: the Swedish poles at their second count (2030), the German poles at their first (2025-02-23), the idle first
> counts of Sweden, Germany, Poland and the USA; each run prints `FISCAL TENURE:` at each formation, so the four-year clause is read against the
> formation that made it reachable, not assumed.

What each falsifier is: the 0.01 pp recomputation and the 1e-4 index check (the term's size), the K61 sign on the game's own print (independent of
the recomputation), the K98/K100/K105 year assertions and the four-year-form assertion - all in `aloop_record_check.pl`, each failing while its
clause is false (tested on planted lines).

## The plan's restatements - for the integrator to apply to `docs/specs/A_LOOP_PLAN.md`

**Section 3 (4) (b)**: replace the bullet "(b) A 1.5 % government loses: ... split by responsibility." with the K60 text above.

**Section 4 (2)**, add after the BALCH_ey bullet:

> - **The first count (K62, K98, K100, K105):** until a game year closes, every year is the source's (Eurostat's B9, S13, PC_GDP for the EU five;
>   OMB's Table 1.2 for the USA; `ElectionsData/macro/fiscal_balance.md`), read by the election's calendar year E: BAL0 is E, BAL-1..BAL-4 are
>   E-1..E-4, each closed by the election's day. After it, the game's own years, then the sourced years the world entered with (those that had
>   closed by the start).
> - **Tenure (K63, K99, K101, K104):** a year counts only where the whole year fell under the governing party's tenure, which runs through
>   re-elections and re-formations under the same party and restarts where another party leads; at a start it is read off the governments of
>   record (Poland's PiS from 16 November 2015).

**Section 4 (3)**, replace the sigma_v bullet's first sentence with:

> **sigma_v: FITTED, a ROBUST scale (K64).** The MAD-based scale (1.4826 x the median absolute deviation from the median) of the PM's (US: the
> president's) party's vote-share change between consecutive elections over the estimating sample's frame - the swings are fat-tailed (the
> kurtosis printed by `FiscalVoteFitCheck`), so the standard deviation and the IQR-based scale are printed beside it, not read.

and add after the support bound: "- **The share cap: [AUTHORED-DRAFT] (K64, interim).** shift_gov is held within +/-10 pp of share before it is split."

**Section 4 (4)**: replace "the surplus poles gain;" with "(K61) a pole whose balance improves across the window gains, one whose balance
worsens loses, a mixed window is a reading - the variables are changes, never levels;" and the "Fixed before the run (2026-10-19 ...)" bullet's
final **Pass** sentence with the section 4 (4) text above.

**Section 4's "Built" line**, append: "**Re-built (2026-10-24, `wave/fisc`):** K60-K64, K98-K101, K104, K105 - the first count read off the
source by the election's year, the party's tenure, the MAD-based sigma_v, the +/-10 pp cap."

## The calibration-list entry (section 346's list)

> *"q13's share cap: the fiscal term held within +/-10 pp of the government side's share, interim (K64), until the support bound is measured on
> Brender & Drazen's replication data."*
