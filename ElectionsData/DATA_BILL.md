# The data bill — what the night could not fill (2026-08-28→29), with candidate sources

Per R-N4/Part 3.6: everything below is billed, not guessed. Each line names the gap, why it
stayed open, and the candidate source for the morning (or a later session).

## Returns
- **Germany — exact absolute party counts** (national + per Land): the three-decimal shares
  are inside the recorded seat-sensitive band, but the constraint is counts. Candidate:
  `kerg2.csv` itself (already located, machine-readable; parse its Bund/Land rows).
- **Poland — per-district ABSOLUTE counts**: the night carries per-district percent-of-valid
  (all 41) and national counts. Candidate: the obwieszczenie's per-district vote tables
  (eli.gov.pl, fetched but not transcribed at that depth) or KBW's absolute-votes CSV
  (`wyniki_gl_na_listy_po_okregach_sejm_csv.zip`, sibling of the percent file).
- **Italy — party TOTAL seats from a primary source**: Eligendo publishes only the PR seat
  column; totals are [UNCONFIRMED] (Wikipedia, PR components corroborated exactly). Candidate:
  the Interior Ministry's results dossier
  https://dait.interno.gov.it/documenti/dossier-elezioni-politiche-2022.pdf (exceeded the
  night's fetch size limit) or the Camera's own seat register. Also the ScN / SVP–PATT
  PR-vs-uninominal discrepancy to resolve from the same source.
- **Italy — Impegno Civico's national vote line** (present per circoscrizione, not itemized in
  the national extraction).
- **France — per-circonscription results** (both rounds): needed before ANY seat model of the
  two-round system; the Ministry's data.gouv dataset publishes them (same dataset family as
  the régions file). Deliberately not fetched (577 × 2 rounds is a table the night did not
  need — no French seat model is claimed).
- **Sweden — none.** Exact national counts landed (RD_S.json follow-up); per-constituency
  exact fractions live in the xlsx already cited.
- **USA — none major.** WA/AK rules pages were quoted via search extracts of the official
  pages ([UNCONFIRMED-direct-fetch]); Stein's party label unconfirmed as a label; the FEC
  biennial compilation (when posted) supersedes the Clerk's file as the single source.

## Positions
- **USA lrgen and EU position**: no item in GPS-2019; CHES-USA unpublished ("Coming soon").
  Candidate: CHES-USA when it publishes; V-Party/Manifesto RILE as interim named alternatives.
- **US positions vintage**: GPS-2019 is pre-2020 — flag any use with the vintage; re-source
  when CHES-USA lands.
- **Italia Viva** (no CHES 2024 row), **KO as a coalition** (PO row stands in), **AVS**
  (components only). State the mapping wherever these feed a model.

## Salience
- **Eurobarometer canonical citation**: the europa.eu survey page is a JS app; the night cites
  the EC PDF via an official programme mirror. Candidate: the EC's own data portal record for
  EB105 for a canonical URL.
- **Gallup August 2026** (not yet released at fetch time); July's exact fieldwork dates.

## Voter groups (Part 3.5 — deliberately not attempted as sourcing)
- DERIVED-first per the queue: construct from the existing census/demographic seeds where the
  model holds the marginals (age structure, urbanization, sector employment are in
  `WorldFactory`/the seed docs). The derivation is a DAY task with the spec's group definitions
  in hand — blocked-on-the-spec tonight (the spec defines the group axes), billed rather than
  invented (the queue's own hard rule: no authored demographics).
