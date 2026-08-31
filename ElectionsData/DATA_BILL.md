# The data bill — what the night could not fill (2026-08-28→29), with candidate sources

Per R-N4/Part 3.6: everything below is billed, not guessed. Each line names the gap, why it
stayed open, and the candidate source for the morning (or a later session).

## Returns
- ~~**Germany — exact absolute party counts**~~ **FILLED the same night** (second fetch):
  `germany/national_counts_2025.csv` — kerg2.csv Bund rows, the Gültige total confirmed
  exactly (per-Land exact counts remain fetchable from the same file if ever needed).
- ~~**Poland — per-district ABSOLUTE counts**~~ **FILLED the same night** (second fetch):
  `poland/district_votes_2023.csv` — the KBW absolute-votes CSV, all 41 okręgi × six
  committees, verified by exact national-sum cross-check on all six.
## Filled on Day-1 (2026-08-29) — struck from the bill

- ~~Italy's allocation FORMULA~~ **SOURCED** → `italy/rosatellum_allocation.md` (DPR 361/1957
  art. 83 consolidated, in force at 25-9-2022, quoted clause by clause; implemented as
  `Rosatellum.cs`; the 2022 national stage reproduces exactly).
- ~~USA full 51-state EV table~~ **SOURCED** (overnight, third fetch) → `usa/state_ev_2024.csv`.
- ~~USA district-method statutes + ME/NE district results~~ **SOURCED** →
  `usa/district_method_2024.md` (Me. 21-A §802; Neb. §32-710 + §32-1038(1); seven pluralities,
  cross-footed to zero residual).

## Still owed

- ~~**France — per-circonscription results**~~ **NOT A BILL — R-EL10 (2026-08-29) rules France
  STRUCTURALLY OUT OF SCOPE.** Two-round SMD needs a 577-constituency model with runoff behaviour;
  that is a large sourced build serving one country, and nothing before 13 September needs it.
  It is a named future item on the roadmap ("France constituency model", unsized, unstarted) —
  **not a gap, not a placeholder, and not to be approximated.** The data (the Ministry's data.gouv
  family) is where it always was if the item is ever taken.
- **Italy — the sub-national stages** (R-EL11, 2026-08-29): source them to the Rosatellum statute
  standard **before Italy is playable**, not before the model is trusted — the proportional stage
  already reproduces exactly. Needs the per-circoscrizione and per-collegio *cifre elettorali*
  (Eligendo Archivio, HTML only) plus the art. 84 cascade. Billed, deliberately not urgent.
- **Italy — per-circoscrizione and per-collegio *cifre elettorali*** (27 circoscrizioni, 49
  collegi) for the sub-national stages (art. 83 lett. h/i, art. 83-bis) and the art. 84
  cascade. Available on Eligendo Archivio as HTML only (`…&tpe=C…`, `…&tpe=P…`). ⚠ The
  comune-level open-data CSV is NOT a substitute — its `VOTILISTA` sums undershoot the
  published *cifre elettorali* by 2.6–4.6 %.
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
  when CHES-USA lands. **Day-1 note:** this is now load-bearing — Phase 4's USA case runs on
  these coordinates.
- ~~**Nebraska's district method may be repealed**~~ **RESOLVED 2026-08-29 (R-EL12): LB3 was NOT
  ENACTED** — cloture failed 31–18 on 8 April 2025, indefinitely postponed at sine die
  17 April 2026; LR24CA never floor-debated; the initiative route withdrawn June 2026. The
  district method stands. Full finding, sources and its sourcing gap: `usa/district_method_2024.md`.
  ⚠ **The finding has an expiry** — the 110th Legislature convenes January 2027 and the Governor
  intends to keep pursuing winner-take-all before 2028. Re-check before modelling a post-2026
  cycle; if it ever passes, R-EL12 requires a **dated rule variant**, never an edit.
- **Italia Viva** (no CHES 2024 row), **KO as a coalition** (PO row stands in), **AVS**
  (components only). State the mapping wherever these feed a model.

## Salience
- **Eurobarometer canonical citation**: the europa.eu survey page is a JS app; the night cites
  the EC PDF via an official programme mirror. Candidate: the EC's own data portal record for
  EB105 for a canonical URL.
- **Gallup August 2026** (not yet released at fetch time); July's exact fieldwork dates.

## Voter groups (Part 3.5) — ⚠ W-F4 STOPPED AND REPORTED 2026-08-30: THE PREMISE BELOW IS FALSE

⚠ **CORRECTION (2026-08-30, W-F4).** The struck paragraph below claimed the marginals were already
in the model. **They are not.** Verified directly rather than assumed: a grep for
`Urbanization|MedianAge|Urban|AgeStructure|SectorEmployment` across all of `Assets/Scripts/` returns
**exactly one hit**, and it is a **display string** — the list of group NAMES the results screen
draws as ABSENT (`GameController.CampaignResults.cs:250`). `WorldFactory` seeds no age structure, no
urbanization and no sector employment at any level.

**And the deeper problem is structural, not missing data.** `ELECTIONS_GAP_TABLE.md` already records
it: *"the game's 'regions' are countries; sub-national regions do not exist as model objects."*
There is nothing for a per-region demographic marginal to attach TO.

**So W-F4 is NOT a derivation and cannot be delivered as the worklist words it.** What it actually
needs is **SOURCED per-valkrets marginals from SCB** (Statistiska centralbyrån) — age, urban/rural
and income distribution per constituency — which is a different and much larger item. §0.4 forbids
inventing them, so the electorate stays ONE GROUP with its **12 `W-F4` call sites intact** across
8 files, each already written to expect a group layer that does not exist.

**This is the honest outcome of the item, not a failure to deliver it** (worklist rule 0.8: an item
whose stated premise is contradicted stops and reports rather than proceeding on a false basis).

**Billed:** SCB per-valkrets marginals. Candidate source — SCB's statistical database
(statistikdatabasen.scb.se), which publishes population by age and by region; **the join from
municipality to valkrets is itself work**, since the 29 riksdagsvalkretsar are not identical to
counties.

### ⚠ C-D1 (2026-08-31): THE BILL, MADE EXACT — and closed as billed

The pre-ruling was *source it if reachable under the cross-check gate; otherwise **bill the exact
series** and close as billed.* Reachability was tested rather than assumed, and the bill below names
what a session should fetch instead of leaving the next reader to find it again.

**What is now located precisely:**

| piece | where it is | state |
|---|---|---|
| the 29 constituencies, by name | already on disk — `sweden/valkrets_votes_2022.csv`, Valmyndigheten | ✅ have it |
| **kommun → valkrets membership** | **Vallagen (2005:837) 4 kap. 2 §** — the statute enumerates them municipality by municipality (e.g. Skåne läns västra = *"Bjuvs, Eslövs, Helsingborgs, Höganäs, Hörby, Höörs, Landskrona och Svalövs kommuner"*). riksdagen.se / lagen.nu | ✅ located to the paragraph, ⚠ **not fetched — see the vintage note** |
| population by age and sex, per municipality | SCB PxWeb `START/BE/BE0101/BE0101A/BefolkningNy`. ⚠ **Metadata endpoint fetched and confirmed at C-D1**: variables `Region · Civilstand · Alder · Kon · ContentsCode · Tid`, with municipality-level `Region` values present | ✅ confirmed to exist with the right dimensions |
| education level, per municipality | SCB PxWeb `START__UF__UF0506__UF0506B/Utbildning` — *"Befolkning 16–74 år efter region, utbildningsnivå, ålder och kön"*, 1985– | ✅ series named |
| income, per municipality | SCB subject area **HE0110** (*Inkomster och skatter*), `START__HE__HE0110` — municipality-level extractions available. ⚠ The exact table within it is **not asserted here**, because it was not opened | ⚠ area named, table billed |
| turnout by age (for `TurnoutBase`) | SCB's *valdeltagandeundersökning* | ⚠ billed |

**Two constraints found while testing reachability, both worth more than the links:**

1. ⚠ **PxWeb serves DATA by POST, not GET.** The metadata endpoint answers a plain fetch — which is how
   the dimensions above were confirmed — but an extraction needs a POSTed JSON query. A session whose
   only tool is a GET fetch can **confirm the series and cannot pull it**. That is the actual blocker on
   the data half, and it is a tooling fact, not a data one.
2. ⚠ **The kommun→valkrets mapping has a VINTAGE.** Constituency boundaries are set by law and amended
   between elections, so a mapping fetched today and used a year later is silently wrong. It must be
   fetched **with its vintage stated, at the time of the build** — which is a reason not to bank it now.

**Why the item closes as billed rather than proceeding.** ⚠ **The blocker is no longer the data — it is
the ORDER.** `POLISIM_COHORT_SPECLET.md` §5 rules that voter groups are a **view over the cohort
substrate**, with `PopulationShare` computed and never seeded, precisely so the game never carries two
populations. That substrate does not exist yet (C-C13 is written, P-I2 unbuilt). Sourcing per-valkrets
marginals now and attaching them to a new group layer would build **the second population that spec-let
exists to forbid** — and it would have to be unpicked by the very item that follows it. **C-D1 is
downstream of P-I2**, and that dependency is the item's real finding.

The electorate therefore stays ONE GROUP with its 12 `W-F4` call sites intact.

> ~~DERIVED-first per the queue: construct from the existing census/demographic seeds where the
> model holds the marginals (age structure, urbanization, sector employment are in
> `WorldFactory`/the seed docs). The derivation is a DAY task with the spec's group definitions in
> hand — blocked-on-the-spec tonight, billed rather than invented.~~ **— struck 2026-08-30: the
> parenthesis was wrong, and everything after it followed from the parenthesis.**

## Polls (W-A4, 2026-08-29)
- **Sweden — a final-week poll of record for 2018 and 2022** (the newspapers' commissioned polls, or the SVT/Valu exit poll's recalled vote): the tactical layer models the LAST WEEK's switch, and the only official pre-election figure on disk is SCB's May PSU (`sweden/psu_2018_2022.md`) — four months before the day. Billed, not approximated; the May figure is a lower bound on the lending, not its size.
