# PoliSim — Realistic politics and elections

**Opened 2026-08-11 at Elias's direction.** Live work only; finished phases move to `COMPLETED.md`,
blocked ones to `MISSING_PREREQUISITES.md`, per the Master Roadmap's three-way test.

---

## 0. The four decisions, taken before any design

Asked and answered 2026-08-11. Recorded here because three of them close Open Questions and one
**reverses a standing rule**.

| Decision | Chosen | Consequence |
|---|---|---|
| Party naming | **Real party names, fictional people** | Reverses working-discipline rule 9 for parties. See §0.1 |
| Institutional depth | **Full institutions** | Bicameralism, head of state ≠ head of government, cohabitation, veto, divided government |
| Vote model | **Hybrid** | National swing decides shares; per-cohort turnout modulates. See §3 |
| First vertical slice | **USA** | The outlier system, built first. See §7 |

### 0.1 REVERSAL of working-discipline rule 9 (2026-08-11)

Rule 9 reads: *"All new named entities (cabinet ministers, party names, legislators) are original and
fictional — never real people or real political parties."*

**It is now split.** Rule 10's precedent requires a reversal be recorded explicitly rather than left to
look like drift, so:

- **PARTIES — reversed.** Real party names, real vote shares, real seat counts, real thresholds. The
  Riksdag holds Socialdemokraterna and Sverigedemokraterna, not Progressive Alliance.
- **PEOPLE — UNCHANGED, and this half is not negotiable.** Cabinet ministers, party leaders,
  legislators, Fed Chairs and heads of state remain original and fictional. The Fed Chair rule stands
  exactly as written. **A real party is an institution; a real politician is a person**, and only the
  first is being reversed.

**The cost this buys, stated plainly so nobody rediscovers it:** real party data goes stale. Sweden votes
**13 September 2026 — roughly one month from today** — and Italy's replacement electoral law
("Stabilicum", passed the Camera 16 July 2026) is before the Senato in September. Seed data is now a
**cached value with an expiry**, which is rule 12's shape. Every seeded figure carries its retrieval date
for exactly this reason.

---

## 1. What exists today, and what of it survives

| Today | Verdict |
|---|---|
| `PartyArchetype` — 4 shared fictional archetypes across all six countries | **Replaced.** Becomes per-country real party sets |
| `ParliamentConstants.TotalSeats = 200` for every country | **Replaced.** Real chamber sizes |
| Seats drift off `ApprovalRating` with bounded inertia + jitter | **Kept as the between-election model**, re-pointed at real parties. It is a good drift model; it was never an election |
| `ElectionSystem` — `ApprovalRating >= 35f` wins | **Replaced entirely.** 56 lines, no votes, no seats, no turnout |
| `ParliamentSystem` bill scoring on `FiscalStance` | **Kept.** Real parties get a real `FiscalStance`; the seat-weighted scoring is unchanged |
| `HemicycleRenderer`, `PoliticalCompassRenderer` | **Kept, extended.** Already draw real seat data |
| `PublicationSystem` / `ReleaseCalendar` / `PublishedData` | **Kept and reused for polling.** See §6.3 |

### 1.1 The landmine — do not touch `ElectionCycle`

`MacroSystem.YearsPerTurn` is derived as `4f / ElectionSystem.ElectionCycle`. `ElectionCycle` is
therefore **a statement about how long a turn is**, not about how long a term is, and it only looks like
the latter because a US presidential term happens to be 4 years.

Per-country terms (FR 5y, IT 5y, US/DE/SE/PL 4y, plus the US House's 2y and the Senate's 6y) **must
never** be expressed by changing it. Terms get their own per-country fields; `ElectionCycle` keeps its
current value and its current meaning, and gets a doc comment saying so.

---

## 2. Phase 1 — the data model

New types under `Assets/Scripts/Data/`:

- **`PoliticalParty`** — real name (native + English), short code, `FiscalStance` (−1..+1, the axis
  `ParliamentSystem` already scores against), social axis for the compass, `BaselineVoteShare`,
  `CohortAppeal[]`, coalition-compatibility set. **Instances, not an enum** — six countries have 40+
  parties between them and an enum cannot carry per-country data.
- **`Chamber`** — name, seat count, term length in days, `ElectoralFormula`, threshold rules,
  constituency count, levelling-seat count, whether directly elected, renewal pattern (whole /
  staggered thirds / staggered halves).
- **`ElectoralFormula`** — enum: `SainteLagueModified`, `DHondt`, `LargestRemainder`, `Fptp`,
  `TwoRound`, `MixedMemberProportional`, `ElectoralCollege`, `IndirectlyElected`.
- **`ThresholdRule`** — national %, alternative constituency %, coalition %, basic-mandate count,
  minority exemption. Every one of these is load-bearing for at least one country.
- **`HeadOfState`** / **`HeadOfGovernment`** — office name, selection method, term, and a **powers
  bitmask**: dissolve, veto, refuse-signature, appoint-PM, command-forces, emergency. The Swedish
  monarch's mask is empty and that is the point.
- **`Legislature`** — a country's chambers plus the current composition of each.

`Country` gains `Legislature`, `PartySet`, `GovernmentComposition`, `NextElectionDate`.

**Phase 1 exit test:** every seeded country's chamber sizes and party lists load, and the sum of seeded
seats equals the real chamber size exactly for all six. No behaviour change yet.

---

## 3. Phase 2 — the allocation engine

Pure static functions, no Unity types, unit-testable outside the Editor — the standalone-harness
property this codebase already values.

| Formula | Used by | Detail that must not be fudged |
|---|---|---|
| Modified Sainte-Laguë | Sweden | First divisor **1.2** (not 1.4 — changed 2018), then 3, 5, 7… |
| Sainte-Laguë/Schepers + MMP | Germany | 630 fixed seats; `Zweitstimmendeckung` caps a party's Land seats at its second-vote entitlement — **23 constituency winners went unseated in 2025** and the engine must reproduce that |
| D'Hondt | Poland | 41 constituencies, **no levelling seats** — disproportionality is a feature |
| Largest remainder | Italy | Rosatellum: 147/400 Camera FPTP + 245 PR; 74/200 Senato FPTP + 122 PR |
| Two-round | France | >50% and ≥25% of registered wins round 1; else top two plus anyone ≥12.5% **of registered voters** |
| FPTP + Electoral College | USA | 435 districts; 538 electors, 270 to win, winner-take-all except ME/NE |
| Indirect | DE Bundesrat, FR Sénat | Not directly elected. Bundesrat = Land governments, 3–6 bloc votes each, 69 total. Sénat = ~150,000 grands électeurs, half renewed every 3 years |

**Thresholds, each of which exists in exactly one country and would be silently wrong if generalised:**
SE 4% national *or* 12% in one constituency · DE 5% *or* 3 direct mandates (basic-mandate clause,
reinstated by the Bundesverfassungsgericht 30 July 2024) · PL 5% party / **8% coalition**, minorities
exempt · IT 3% party / 10% coalition, or 20% within one region · SSW and SVP are threshold-exempt
minority parties and must stay seated.

### 3.1 The exit test that makes this phase real

**Feed each country its real vote shares; the engine must return its real seat counts exactly.** Not
approximately. Sweden 2022 → 107/73/68/24/24/19/18/16. Germany 2025 → 164/152/120/85/64/44/1 = 630.
Poland 2023 → 194/157/65/26/18 = 460. Italy 2022 → 119/66/45/… = 400. A formula that cannot reproduce a
known result is not implemented, it is approximated — and this is the one phase where that distinction is
cheap to test and expensive to skip.

### 3.2 RUN 2026-08-11, BEFORE THE ENGINE SHIPPED — and it moved this phase's scope

The allocator was ported to a throwaway script and run against the three real results above the same day
it was written. Three findings, all measured:

| Country | Method tried | Result |
|---|---|---|
| Sweden 2022 | National modified Sainte-Laguë, first divisor 1.2, 349 seats | **EXACT — all eight parties, 0 seats of error** |
| Germany 2025 | National Sainte-Laguë/Schepers, 630 seats | Off by 1: CDU 165, SPD 119 |
| Poland 2023 | National D'Hondt, 460 seats | **Off by 70 seats.** PiS 169 (−25), Konfederacja 34 (+16) |

**Sweden validates the whole approach.** Its 39 levelling seats exist precisely to make the national
result proportional, so a national allocation reproduces the real chamber exactly. Sweden needs no
constituency model.

**Germany is an input-precision problem, not an algorithm problem** — and this was confirmed rather than
assumed. Re-running across the ±0.05 band that one-decimal published shares permit, the exact real result
is reachable at CDU 22.55–22.58%. So: **the seed data must carry exact vote COUNTS, never published
percentages.** A rounded share is enough to move a Bundestag seat, and it would have looked like a bug in
the allocator forever.

**Poland is a genuine scope discovery, and it invalidates a plan this roadmap held an hour earlier.**
D'Hondt run once nationally is not an approximation of D'Hondt run 41 times — it is a different and much
more proportional system. The real Sejm is far more disproportionate than the national calculation
suggests because each of the 41 constituencies rounds in the large parties' favour independently. **Poland
therefore requires constituency-level allocation**, which means per-constituency seed data (41 × 5) or an
explicitly-modelled and explicitly-labelled disproportionality correction. Phase 2 is bigger than it
looked. The same question is now open for Italy's 28 Camera constituencies and 20 Senato regions and must
be measured the same way before its allocator is trusted.

**The transferable point:** two of the three countries disagreed with the plan, and both disagreements
were found in minutes by a script with no engine, no Editor and no compile — before a line of it was
depended on.

### 3.3 The Riksdag allocator confirmed against the shipped `SeatAllocation.cs`, 2026-08-11

3.2 validated the *design* on a throwaway script before `SeatAllocation.cs` existed. Once the file was
written, it was checked again — this time against the actual shipped code, with the script kept in the
repo instead of discarded: `seat_allocation_check.py` at the repo root, alongside
`screenshot_edge_check.py`. Re-run with `python3 seat_allocation_check.py` — no engine, no Editor, no
compile, exit 0 on a clean run.

**Sweden 2022, full pipeline (`ApplyThreshold` at the Riksdag's 4% national bar, then
`Allocate(SainteLagueModified)` at 349 seats) — EXACT, 0 seats of error, all eight parties**, against
vote counts cross-checked across three independent sources (val.se's own results page and two Wikipedia
pages, all agreeing digit-for-digit) rather than trusted from one fetch. This is the standing
confirmation that the Riksdag allocator is right for the vote distribution that matters today — the
1.2-divisor law that has applied since 2018 and that Sweden's next election (13 September 2026) will use
again.

**A second real election was tried as a negative control, and it complicates 3.2's claim rather than
simply confirming it.** Sweden 2014 (349 seats, the pre-2018 law, first divisor 1.4) does NOT reproduce
exactly through the same pipeline — 6 seats of absolute error (S −1, M +1, SD −2, FP +1, KD +1) — and,
surprisingly, the error is byte-identical whether the divisor used is 1.4 (historically correct for
2014) or 1.2 (today's value). The votes and the real seats were each cross-checked against three
independent sources before this was trusted over the code (val.se, svt.se, and Wikipedia's "List of
members of the Riksdag, 2014–2018" all agree).

**This narrows 3.2's "a national allocation reproduces the real chamber exactly" to "confirmed for 2022,
not established in general," and that correction is recorded here explicitly rather than left for 3.2 to
be quietly wrong.** The leading explanation is NOT a bug in `SeatAllocation.cs` — the same code
reproduced 2022 exactly, and the 2014 error pattern (small, offsetting, total still exactly 349) has the
same shape as Poland's national-vs-41-constituency gap in 3.2: a party's directly-won constituency
("fasta") seats can, in some election years, already meet or exceed its national proportional
entitlement in a way that perturbs how the remaining levelling seats settle. Sweden's 39-seat levelling
pool absorbs most of this, which is presumably why 2022 came out exact and Poland's national D'Hondt
(zero levelling seats at all) was off by 70. **This is unconfirmed** — resolving it needs all 29
constituencies' 2014 vote data, which was not fetched. Recorded as an open gap, not asserted as fact and
not silently dropped.

**The first divisor was proven decisive, but not by real Swedish data.** Neither 2022 nor 2014 has a
party marginal enough — every seated party won 16+ seats in both years — for the 1.2-vs-1.4 choice to
change a national total, for a real structural reason: the first divisor only ever decides a party's OWN
first seat, and none of these parties' first seats were ever close contests. `seat_allocation_check.py`
proves the parameter still has teeth in the code with a constructed case (900 votes vs 125 votes, 5
seats: divisor 1.4 gives [5, 0], divisor 1.2 gives [4, 1]) — but that is a fact about the code in
isolation, not something either real election could have caught if it were wrong.

**A second, unrelated finding turned up while reading `ApplyThreshold` for this check:
`ThresholdRule.CoalitionShare` (Poland's 8%, Italy's 10%) is never read anywhere in `ApplyThreshold`'s
body** — only `NationalShare`, `AlternativeConstituencyShare` and `BasicMandateSeats` are used.
`ApplyThreshold` also has no parameter carrying coalition membership or a coalition's combined vote
share, so this is not a one-line fix, it is a missing piece of the method's signature. **Does not affect
Sweden** — `ThresholdRule.Sweden` never sets `CoalitionShare`, so the Riksdag path this section verifies
never reaches the gap. Left open for Phase 2's Poland/Italy threshold work; see 10.5.

---

## 4. Phase 3 — the hybrid vote model

Two terms, deliberately separable so each can be validated alone:

**Share** = `BaselineVoteShare` swung by governing-approval delta, economic performance (the
Kramer/Fair-style incumbency-vote-share relation on GDP growth, unemployment and inflation), time in
office, and incumbency drag. This is the existing `ApprovalSensitivity` idea, re-pointed at real
baselines instead of invented ones.

**Turnout** = per-cohort. Age bands × cohort turnout rate × cohort share of the electorate, aggregated
to a national figure that reweights the shares. This is why the hybrid was chosen over pure national
swing: **it gives the campaign layer something mechanical to act on** (§6).

Seed turnout by age, sourced:

| Country | Turnout | By-age availability |
|---|---|---|
| Sweden 2022 | 84.21% | SCB register data — **[APPROX]**, chart-read, exact PxWeb table outstanding |
| Germany 2025 | 82.5% | Official representative electoral statistics — real ballots. 21–24 lowest at 78.3%, 50–69 highest at 85.5%. **35–49 band [GAP]** |
| Poland 2023 | 74.38% | 18–29 = 68.8% (up from 46.4% in 2019). **All other bands [GAP]** |
| Italy 2022 | 63.85% | **[GAP] — no official age breakdown exists.** Only pre-election survey projections |
| France 2024 | 66.71% R1 | Ipsos survey, not INSEE. Under-25 57%, 25–34 51%, 60–69 74%, 70+ 80%. **35–59 [GAP]** |
| USA 2024 | 64.1% VEP / 65.3% CPS | Census CPS supplement — the best of the six |

**Four of six have real gaps.** Per rule 5, a modelled cohort curve fills them and is labelled as
modelled — never as data. Italy's is a total gap and its curve is an assumption in full.

---

## 5. Phases 4 and 5 — government formation, and the institutions

**Phase 4 — formation.** Coalition search over compatibility sets; investiture where it exists (Italy:
confidence in *both* chambers within 10 days; Sweden: *negative* parliamentarism — a PM is confirmed
unless ≥175 vote against, which is why Sweden runs minority governments); confidence and no-confidence
(Germany's **constructive** vote — the Bundestag must elect a successor simultaneously, so a government
cannot simply be voted out; France's motion de censure under 49.2/49.3); snap elections and dissolution
rules. Minority government and confidence-and-supply are first-class, not edge cases: Sweden's current
government is M+KD+L on 103 seats with SD supplying 73 from outside cabinet, and France's is a minority
surviving on PS abstention.

**Phase 5 — institutions.** Bicameralism (IT perfect bicameralism — either chamber can fell a
government; DE Bundesrat consent vs objection bills; US Senate; PL Senate's 30-day suspensive veto).
Head of state with the powers bitmask actually wired: PL president's veto overridable by 3/5 of the
Sejm, IT president's single send-back under Art. 74 with no absolute veto, FR president's Art. 12
dissolution at will, US presidential veto with a 2/3 override. Cohabitation as a state the French game
can enter. Divided government as a state the American one can.

---

## 6. Phase 6 — campaign gameplay

**Added at Elias's direction mid-session, 2026-08-11.** An election you cannot campaign in is a dice
roll with extra steps; this is the phase that makes the cycle playable.

### 6.1 The campaign window

Opens a country-specific number of days before election day and closes on it. The existing continuous
calendar already supports this — no new time machinery.

### 6.2 The core tension: attention is the currency

Campaign actions cost **days**, and days spent campaigning are days not spent governing. Policy still
moves, bills still resolve, the economy still runs. A player who campaigns flat-out arrives at election
day with a strong poll lead and a neglected budget.

| Action | Feeds | Notes |
|---|---|---|
| Ground game / canvassing | **Turnout term**, targeted by cohort | Mobilises your own; the cheap lever, and the reason the hybrid model was chosen |
| Advertising buy | **Share term**, targeted by cohort | Persuasion, with diminishing returns |
| Rallies and tours | Both, regionally | High variance |
| Televised debate | Share, one-shot | A scheduled event with a real downside |
| Coalition signalling | Formation, pre-declared blocs | Decisive in SE/DE/IT/PL, meaningless in the USA |
| Attack messaging | Opponent's share down, own turnout down | Asymmetric, with backfire risk |

Funded from **party funds accrued over the term** — never the treasury. Spending state money on a
campaign is available, and is a corruption scandal with a real probability of detection. `CorruptionIndex`
already exists.

### 6.3 Polls — reusing machinery that already exists

**Opinion polls are `PublicationSystem` exactly as built.** That system already models a noisy
preliminary estimate of a true value which is later revised toward reality, on a release calendar, with
the history preserved so a player can see the number they acted on. A poll is a noisy estimate of a vote
share with a house effect and a sampling error, published on a schedule.

This is a reuse, not a new subsystem — and it means the player campaigns against **published polls that
can be wrong**, not against the true vote share. That is the correct information asymmetry for the genre
and it falls out of existing code.

### 6.4 Election night

Results arrive progressively over simulated hours rather than resolving instantly — constituency by
constituency, with early returns unrepresentative. The machinery is the same day-loop that already
exists.

---

## 7. Phase 7 — the USA vertical slice

Built first because it shares almost nothing with the other five, so it proves the engine is general
rather than a Swedish PR calculator with special cases bolted on.

- **President** — 4-year term, Electoral College (538, 270 to win, winner-take-all except ME/NE's
  district method), term limit.
- **House** — 435 seats, **2-year** terms, all seats every cycle, FPTP.
- **Senate** — 100 seats, **6-year** terms, **staggered thirds**. Class 2 is up in November 2026. The
  staggering is the mechanically interesting part: a president cannot win the whole chamber at once.
- **Midterms** — the off-cycle election with its own turnout collapse (presidential ~64% vs midterm
  ~46%) and its historical anti-incumbent bias.
- **Divided government** — the default American state, and the thing that makes the existing
  Parliament bill-gating bite.
- **Veto** with a 2/3 override; the 60-vote cloture convention as a soft threshold.

**Seed, at `EpochDate` 2026-01-01 — the 119th Congress:** Senate R 53 / D 47. House R 220 / D 215 as
elected. 2024 presidential: 312–226 Electoral College, Trump 49.8% of the popular vote, turnout 64.1%
VEP. 2024 House national popular vote: R 49.75% (74,390,864) / D 47.19% (70,571,330) / Libertarian
0.47%. **The 2026 midterms fall on 3 November 2026 — inside the first game year**, so a new player meets
the whole election system in their first term rather than reading about it.

*Named office-holders remain fictional per §0.1. The player is the President.*

---

## 8. Phase 8 — assets

**Added at Elias's direction mid-session, 2026-08-11.** Appends to `CLAUDE_DESIGN_ASSET_REQUEST.md` as
**§1F** — that document is the single standing request and new requests append rather than starting a
new file. Not a new document.

Needed, at minimum:

- **Party emblems** — one per real party across six countries (~40). The existing spec already has one
  emblem per `PartyArchetype`; this is the same slot at real scale. Abstract marks, not reproduced
  trademarks — a design question §1F must ask explicitly.
- **Chamber furniture** — hemicycle refinements at real seat counts (349, 400, 435, 460, 577, 630 —
  `HemicycleRenderer` was built for 200 and must not just scale), plus a US-style two-chamber layout.
- **Campaign screen** — rally, advertising, canvassing, debate and attack-ad icons; a poster or banner
  frame; a campaign-funds meter.
- **Election night** — a results ticker, a called-seat marker, a swing arrow, a projection band.
- **Institutions** — head-of-state vs head-of-government portrait frames (crown/sash/seal treatments
  that read as office, not person), a coalition-negotiation board, a veto stamp.
- **Maps** — per-country constituency-level results maps. `MapRenderer` currently draws trade topology
  only.

§1F must also carry the §3 technical conventions (prefix rules, tint rules, PNG delivery, the
`Zone.Identifier` origin check) — the five §1E import blockers are exactly what happens when it does not.

---

## 9. Validation plan

1. **Reproduction test, per country** — real vote shares in, real seat counts out, exactly (§3.1).
2. **Turn-0 identity** — a game seeded and immediately inspected shows the real composition of every
   chamber in all six countries.
3. **`BatchSimulationRunner` matrix** — the existing 100/500-turn scenarios, plus a new `electionstress`
   scenario driving approval to both extremes across many cycles. Watching for: seat totals that stop
   summing to the chamber size, coalition search failing to terminate, negative or >100% vote shares,
   and turnout escaping [0,1].
4. **Long-run plausibility** — over 100 simulated years, no party should sit at 0% or 90% permanently.
   The existing `CheckSwing` covers five economic fields and **will not see any of this**; per the top
   of `CLAUDE.md`, that is stated rather than assumed.
5. **Live-Editor confirmation by Elias** — the gates and interrupts live in `GameController` and are
   invisible to batch runs. An election is a gate.
6. **`screenshot_edge_check.py`** on the capture pass — the new screens are the densest yet.

---

## 10. Open questions — flagged, not guessed

1. **Does the player have a party?** Today's design explicitly does not assign one, and
   `PartyArchetypeData` documents approval as benefiting "establishment stability broadly" *because*
   there is no player party. Real parties make that untenable — you cannot campaign for nobody.
   **Recommendation: the player picks a party at country selection**, and approval splits into personal
   approval and party support.
2. **What is losing?** Today it is game over below 35% approval. With coalitions, losing your majority
   but staying in government, or governing as a minority, are ordinary outcomes. **Recommendation:
   game over only on leaving office**, with opposition as a survivable state.
3. **How do real parties stay current?** Sweden votes in a month. **Recommendation: seed data lives in
   one file with retrieval dates, so a refresh is a data edit and never a code change.**
4. **Trademark exposure on emblems** — real party names are text; reproducing party logos is not.
   Recommendation: original abstract marks in the house style, in each party's real colour.
5. **`ApplyThreshold` cannot enforce a coalition threshold at all.** `ThresholdRule.CoalitionShare`
   exists on the struct (Poland 8%, Italy 10%) but is never read in `SeatAllocation.ApplyThreshold` -
   found while verifying the Riksdag allocator, see 3.3. `ApplyThreshold` has no parameter for coalition
   membership or a coalition's combined vote share either, so this needs a design decision, not a
   one-line fix, before Poland/Italy threshold work starts: most likely a `coalitionId` per party plus a
   caller-computed combined share. Not decided here because Sweden, this phase's first target, never
   exercises it.
