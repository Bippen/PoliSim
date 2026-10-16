# The blind design panel - §832, R-US17's rule (c) preregistered: Elias's H1-H3, a seeded draw of defectors on the USA's veto path (2026-10-16)

The workflow `rus17c-blind-rule-panel` (run `wf_d70832a9-2bf`, eight agents): three designers, each from one lens - the game's use, inference from thin data, the precedents and the record - wrote full specifications independently; a judge scored and merged them; three critics attacked the merge (fidelity and scope, computability and determinism, tunability and inference); a reviser ruled on every attack and wrote the final specification. Every prompt carried the brief's BLINDNESS section (Appendix A): no figure of the rule computed, the extract never opened past its header, no tool or Unity run, nothing edited. The session wrote the brief and had computed no figure of the rule; it had seen US-10's published results of the other rules (s802). Everything below is the agents' output verbatim, but for the headings.

## The final specification (the reviser's)

The block in `docs/specs/USA_STAGE_PLAN.md` is this text with the session's edits listed in COMPLETED.md §832 and the review's (`Reviews/2026-10-16_s832_rus17c_rule_review.md`).

```text
R-US17 RULE (c) - THE FINAL SPECIFICATION (s832 panel, revised; blind - no figure of this
rule computed)

This text becomes one ```text fence between "<!-- R-US17 RULE BEGIN -->" and
"<!-- R-US17 RULE END -->" in docs/specs/USA_STAGE_PLAN.md, after H1-H3. "sNNN" is a section
the session assigns. "Asserted" means the instrument fails while the statement is false. A
DEFECT is a code or data failure: it fails the instrument and is never put to Elias. Items
of US-21's design that this rule does not decide (ChamberVerdicts' projection, the plate,
the gate's name, US-21's 2024 run) go into US-21's text outside the markers.

==========================================================================================
THE TERMS
==========================================================================================
- THE RECORD: ElectionsData/usa/us_veto_measures.csv exactly as Tools/us_veto_prep.pl
  writes it, read as Tools/us_veto_backtest.pl's read_csv reads it (lines starting "#"
  skipped, the header taken by name, every row with the header's field count).
  SourceDigest = SHA-256 over its raw bytes. The rule reads nothing else of the US record
  and never the member XML.
- THE PINS (filled by the session at the preregistering commit by hashing bytes, before
  the block's digest is taken; no figure is computed):
    SOURCE PIN: sha256 <64 hex>      PREP PIN: git blob <40 hex> of Tools/us_veto_prep.pl
  A run whose SourceDigest or prep blob differs from a pin is a DEFECT until an amendment
  naming Elias's ruling re-pins it.
- B1'S YARDSTICK: 45 named of 166, recomputed from
  ElectionsData/poland/third_readings_term10.csv exactly as us_veto_backtest.pl does. Any
  other result is a DEFECT.
- A MEASURE'S IDENTITY: "<congress>|<measure>" (e.g. "116|H.R. 6395").
- ORIGIN: the House if measure begins "H.", the Senate if it begins "S."; else a DEFECT.
- VETOED: the outcome contains "veto". NOT VETOED: "signed" or "law without signature".
  Any other outcome is a DEFECT.
- UNPREDICTABLE (H2): a vetoed row where neither house_final nor senate_final begins
  "roll ". Asserted: exactly {119|H.R. 131, 119|H.R. 504}. Any other set is a DEFECT until
  the block is amended by a ruling.
- COUNTED MEASURES: every row not UNPREDICTABLE, in CSV row order; M = their number.
  COUNTED VETOES: the vetoed counted measures; V_COUNT = their number, computed, never
  typed (23 on the preregistered extract).
- COUNTED ONLY: every count, sum, denominator, cell, rate and digest of this rule reads
  counted measures only. The ONE exception is THE US-10 IDENTITIES (THE FIRST RUN, item
  10), which read all rows only to verify the re-implementation. They print as "US-10's
  record (s802), not this rule's figure" and reprint no 25-based rate.
- A ROLL CALL: a house_final or senate_final that begins "roll <n>"; any other final is NO
  ROLL CALL. Its IDENTITY is "<H|S>|<congress>|<that final date yyyy-MM-dd>|<n>". A roll
  call final for several counted measures is ONE roll call everywhere. Its OWNER is the
  first of them in CSV row order, and every shared roll call is printed with its measures.
  Asserted: every roll call of a counted measure is dated 2017-01-03 or later (H1, "since
  2017").
- PRESIDENT, PP, OP: the row's president and president_party (the decision president;
  veto_record.md reading 2). OP is whichever of R and D is not the PP. Asserted: every
  counted row's PP is R or D. In play the president is the one of the vote's day
  (PresidentialElection.PresidentAt). A president of no party has no PP, so no draw is
  made and (a) never fires (the panel's default, by analogy to Poland's G5 items 14-15).
- A LABEL'S SPLIT on a roll call (R, D or I): its yea, nay, present and not_voting columns.
  A yea of "-" is no split. MEMBERS = the four summed; VOTING = yea + nay + present.
- BLOC STANCE of R or D on a roll call (us_veto_backtest.pl bloc_passes and
  party_bloc_opposed): YEA iff yea > nay + present, otherwise NAY (a tie is NAY; present
  stands against, F3).
- F3 SET: the counted measures on which opposed_f3 holds in the House or the Senate
  (nay + present > yea for the PP); US-10's 34 is its count over all rows.
- INDEPENDENTS:
  - A Senate independent adds to the SEATS and to S of the bloc that senate_I_caucus names
    (each space-separated "C:n" entry with C in {R, D}; any other entry stays outside and
    is printed). He is in no split, stance or observation, but a drawn crosser may be any
    member of S.
  - A House independent is in no bloc. He sits inside the House sworn count, counting
    against.
- SEATS OF A CONGRESS (literally us_veto_backtest.pl's %seatsOf, over counted rows):
  1. Order the counted rows by house_final_date with ordinal comparison, stable, ties in
     CSV row order.
  2. For each row in that order and each chamber whose final is a roll call: let t = (R
     members, D members, sworn = R + D + I members), with the Senate's caucus entries
     added to R or D.
  3. Replace the stored entry for "<congress> <chamber>" iff there is none or the row's
     <ch>_final_date >= the stored date (ordinal). Then set the stored date to the row's
     <ch>_final_date IN EVERY CASE (the code's overwrite, kept on purpose).
  Asserted: R, D and sworn per Congress and chamber equal the seats table printed in
  docs/generated/US_VETO_BACKTEST.md. A difference is a DEFECT.
- KINDS: EXPEDITED iff kind is exactly one of "CRA disapproval", "arms-sale disapproval",
  "War Powers", "national emergency", "D.C. disapproval" (ordinal, case-sensitive). These
  are 5 U.S.C. 802, P.L. 94-329 s601(b), 50 U.S.C. 1622 and D.C. Code s1-206.04. Every
  other kind is RULE XXII, and a kind other than "bill" or "joint resolution" is printed.
- RECORD BUDGET CLASS, from the title field, upper-cased invariant, ordinal Contains:
  - RECONCILIATION: "PROVIDE FOR RECONCILIATION", "PROVIDING FOR RECONCILIATION",
    "RECONCILIATION PURSUANT TO", "PURSUANT TO SECTION 2002 OF THE CONCURRENT RESOLUTION";
  - APPROPRIATION: "MAKING APPROPRIATIONS", "MAKING CONTINUING APPROPRIATIONS",
    "MAKING FURTHER CONTINUING APPROPRIATIONS", "CONTINUING APPROPRIATIONS",
    "APPROPRIATIONS ACT";
  - DEBT LIMIT: "DEBT LIMIT", "FISCAL RESPONSIBILITY ACT", "BIPARTISAN BUDGET ACT".
  Every match is printed with its test. Every counted title containing "APPROPRIATION",
  "RECONCILIATION" or "BUDGET" that matches no test is printed as a NEAR MISS.
- SENATE PROCEDURE of a counted measure: MAJORITY (expedited) if its kind is EXPEDITED;
  else MAJORITY (reconciliation, US-18's 2 U.S.C. 641(e) path) if it matches a
  RECONCILIATION test; else RULE XXII.
- THE VETO PATH (record), V: the counted measures with at least one roll call on which the
  PP's bloc stance is NAY. |V| is printed beside F3's 34, with every measure in one and not
  the other (they differ only at yea = nay + present).
- V_XXII: the measures of V whose SENATE PROCEDURE is RULE XXII.
- THE VETO PATH (play): an ORDINARY LEGISLATION bill on which the PP's side in a chamber's
  division (as US-18 sets it) is AGAINST.
- BUDGET VOTE (play): every act the US budget procedure tables (WorldClock.BudgetProcedureOf,
  US-22: the appropriation, a continuing resolution, the pension-age act), a reconciliation
  bill (US-18), and US-24's debt-limit act. The debt-limit act is the panel's default
  (US-22 has the debt limit ride reconciliation; 641(a)(3)), with R18 printed. Every other
  US bill is ORDINARY LEGISLATION.
- SENT: both chambers pass under the model. NAMED: sent, and rule (a) fires. VETOABLE SENT:
  a measure of V that is sent.
- THE CELLS: HP and SP, the PP standing NAY in the House and the Senate. HO and SO, the OP
  standing YEA on those same roll calls.
- N_MIN = 12 observations: a floor preregistered with no data read. A data-free note: at n
  = 12 the 95% Wilson interval of a share at p = 1/2 is no wider than 0.5 (0.492; 0.509 at
  11).
- EXACTNESS: every comparison in THE DECISION is made on rationals (BigInteger numerator
  and denominator). Equal is never "below".

==========================================================================================
THE MODEL (what is drawn, from what, per what)
==========================================================================================
1. Parties stay blocs by default (H1): a party votes as one, and its bloc carries every
   seat it holds in the chamber. Nothing is drawn or recorded outside item 2.
2. WHERE A DRAW IS MADE (H1, "on the USA's veto path only"): only for a bill on the veto
   path, once per party per chamber per bill.
   - In play the bill is on THE VETO PATH (play). On the record the measure is in V, under
     premise (B) (item 9), and outside the RECORD BUDGET CLASS.
   - The PP standing AGAINST (NAY) draws from HP or SP. The OP standing FOR (YEA) draws
     from HO or SO.
   - No draw is made for: an OP standing AGAINST; a side UNDECIDED; a third or created
     party (it votes its bloc); a party with S = 0; anything off the veto path; a budget
     vote (play) or a RECORD BUDGET CLASS measure (record); anything outside CountryId.USA;
     a president with no PP.
3. WHAT IS DRAWN: an index j into the cell's drawn list L (THE FIT), n = |L|, in FIT ORDER.
   - j = floor(u * n) in IEEE-754 double, clamped to n - 1. The drawn observation is
     (d, m).
   - K = 0 if S = 0; otherwise K = min( floor((S - 1) / 2), floor((2*d*S + m) / (2*m)) ),
     where S is the party's bloc seats in that chamber on that bill (Senate: members plus
     caucus entries). This is the recorded crossing share carried onto the bloc's size,
     rounded half up, and capped so that crossers stay a strict minority.
   - An empty list (only in a LOO list, a bootstrap replicate or an INFLUENCE removal) is
     the single index "K = 0" (n := 1) in Crossers, the enumeration and the realizations
     alike. Its occurrences are counted and printed.
4. The K crossers move to the other side: the PP's from nay to yea, the OP's from yea to
   nay.
5. ONE DRAW per party per chamber per bill serves every vote of that chamber on it
   (cloture, passage, concurrence). PREMISE, stated: the draws of the two parties and of
   the two chambers are independent (R7 prints the joint draw).
6. IN PLAY the drawn crossers enter a chamber's recorded division only where they change
   its outcome (WHEN THE DRAW MATTERS). They enter as each party's crossers on the other
   side, flagged as defectors. Everywhere else the division is the blocs', byte for byte.
7. The cells pool over presidents, Congresses, kinds and outcomes. They are split by
   chamber, role and stance only (four cells), and no split is chosen from the data.
8. A projection reads exact probabilities and never draws. Only the gate draws.
9. THE BACKTEST'S PREMISE (B), ONE STANCE (play's premise, US-18; spec-risk 4). For a
   measure in V:
   - the PP's one stance is NAY in both chambers;
   - the OP's one stance is its bloc stance on the House roll call if the PP's House
     stance there is NAY, otherwise on the Senate roll call;
   - seats are the SEATS OF A CONGRESS;
   - draws are made in both chambers whether or not a chamber has a roll call (none for a
     RECORD BUDGET CLASS measure).
   Off V a measure is never named and draws nothing. R1, reading (A) chamber by chamber,
   is printed beside it.

==========================================================================================
THE FIT (data, members counted, measures, defector definition, estimator)
==========================================================================================
- DATA: the roll calls of counted measures, which are the only roll calls on disk. No
  cloture vote, failed measure or override party split is saved. The RECORD BUDGET CLASS
  roll calls stay in the fit (H1: "the recorded chamber-by-chamber votes since 2017"; R16
  (b) removes them).
- SELECTION ON PASSAGE, DECLARED PREMISE: the record holds passed measures only, so every
  observation comes from a measure that passed. Sending is estimated with a bias toward
  sending. Per chamber, the instrument prints how many PP-NAY roll calls failed at K = 0
  on their recorded bloc result, as x of y, not fitted. R6 prints the fit without
  selection.
- THE FITTED ROLL CALLS, per chamber: every distinct roll call on which the PP of its OWNER
  has bloc stance NAY. Each gives:
  - a PP observation: d = PP yea, m = PP voting;
  - an OP observation only if the OP's bloc stance is YEA: d = OP nay + OP present, m = OP
    voting. A roll call where the OP also stands NAY gives no OP observation and is
    listed.
  Not-voting members are outside m. Independents are in no observation. An observation
  with m = 0 is excluded and listed. Asserted: every share d/m <= 1/2.
- DEFECTOR: a member voting against his party's bloc stance as recorded on that roll call.
  It is not the president's position: no saved page records it, and reading it from the
  outcome would be circular.
- A CELL'S LIST L: its observations. n of a cell = |L|, which is also its number of roll
  calls with an observation. That one n is used everywhere: N_MIN, the union, the draw
  index, CP and the label. Members are never the unit.
- FIT ORDER: ascending share (compare d1*m2 with d2*m1 in integers), ties by the roll
  call's IDENTITY in ordinal order.
- ESTIMATOR: each cell's empirical distribution of (d, m), the nonparametric
  maximum-likelihood estimate of that party-role's crossing-share MARGINAL. It has no
  parameter, no optimiser and no convergence failure. Reasons: it assumes no shape for a
  distribution heavy at zero and overdispersed; each marginal draws only shares the record
  showed; and each figure is a share of n roll calls. R8 prints the binomial at the pooled
  rate. The pairing of the two parties' draws is item 5's independence premise.
- THIN CELLS (decided once on the full fit, and held fixed in every LOO list, bootstrap
  replicate, INFLUENCE removal and realization):
  - A cell with n < 12 is POOLED with the same role-and-stance cell of the other chamber
    (HP with SP, HO with SO). It draws from the union, sorted in FIT ORDER.
  - The partner keeps its own list unless it too is below 12, in which case both draw from
    the union.
  - A union with n < 12 leaves the cell UNFITTED, and S3 fires.
  - R11 pools with the other role of the same chamber instead.
- LEAVE ONE OUT (LOO): measure j's draws in the backtest read every cell minus every
  observation of every roll call final for j (all copies). Play and the catalog use the
  full cells. In-sample is R5.
- THE CELL FIGURES: q_any, the share of observations with d >= 1; q10, the share with
  10*d >= m; the mean share.

==========================================================================================
WHEN THE DRAW MATTERS (the margin test per chamber and procedure)
==========================================================================================
Notation for a chamber: Y0 and N0 are the bloc yeas and nays with every K = 0. Kp is the
PP's draw and Ko the OP's (0 where none is made). D = Kp - Ko, Y = Y0 + D, N = N0 - D.

- HOUSE (a majority of those voting; a tie fails). PREMISE of this block: suspension's two
  thirds is not modelled.
  - (B): passes iff 2*Y > W_H, where W_H is the House sworn count of the SEATS
    (independents inside, counting against). With the OP standing YEA, Y = S_OP - Ko + Kp;
    with the OP standing NAY, Y = Kp.
  - (A): passes iff Y > N over the R and D blocs as the roll call lists them (members),
    independents outside.
  - Play: passes iff Y > N among the seated members, every one voting his bloc's side or
    as a crosser.
- SENATE, MAJORITY procedure (expedited or reconciliation):
  - (B): passes iff 2*Y > W_S, the SEATS' sworn count with independents in their caucus.
  - (A): passes iff Y > N, with the caucus entries in their blocs.
  - THE VICE PRESIDENT breaks a tie with the PP's stance in that chamber and is never a
    crosser. He votes under (B), under R1 and in play. On V under (B) that stance is NAY,
    so a tie fails. A vacant vice presidency (play) fails the tie.
- SENATE, RULE XXII (every other measure; every ordinary game bill): cloture needs
  Y >= floor((3*W + 4) / 5), three fifths of the senators duly chosen and sworn, never a
  fixed 60.
  - W is: (B) the SEATS' sworn; (A) the roll call's members, all labels; play, the sworn
    count on the day.
  - Passage then follows by a majority on the same draw. Cloture implies a majority, and
    the Vice President has no cloture vote.
- A CHAMBER WITH NO ROLL CALL (record): (A) passes undrawn ("unanimous under blocs",
  US-10). (B), for a measure of V, is modelled on the SEATS with the draw.
- UNCONTESTED (both blocs on one side, or the PP not AGAINST): no draw, and the test at
  K = 0. This applies off V, under (A) and in play. On V under (B) the PP always draws,
  both-NAY included.
- RECORD BUDGET CLASS on V: the test at K = 0 in each chamber, with no draw.
- THE DRAW MATTERS iff the outcome at the drawn (Kp, Ko) differs from the outcome at
  (0, 0). This is H1's "the chamber's margin within the drawn count", made exact.
- MARGIN: the smallest |D|, with the sign that reverses the K = 0 outcome, among D in
  [-cap_OP, cap_PP], where cap = floor((S - 1) / 2) for a party that draws and 0 otherwise.
  It prints "unreachable" where none exists.
- QUORUM: in the model every bloc seat votes, so floor(W/2) + 1 is always present. On the
  record it is asserted for every roll call read.
- VACANCIES enter only through the bases (members listed, sworn, seated). A vacant seat is
  in no bloc and no base.
- SAME-DAY ACTIONS: the final agreement is the extract's (veto_record.md reading 4; the
  guards of s802 and s831). same_day is printed only. The two chambers' draws are keyed
  apart.
- US-10 MODE (used only by THE US-10 IDENTITIES): kinds as US-10 reads them (EXPEDITED or
  Rule XXII, with no reconciliation path and no budget class), no Vice President (a tie
  fails), all rows.

==========================================================================================
THE VETO (rule (a) on a bill in play)
==========================================================================================
- A bill both chambers send goes to the president of the day (US-21).
- (a) fires iff S_PP,H >= 1 and 2 * (the PP's House nays in the chamber's division) >
  S_PP,H. "More than half" is strict, as in B1.
  - S_PP,H is the PP's House bloc seats (play, (B)), or its House members as the roll call
    lists them ((A); US-10's member count).
  - The nays are S_PP,H - Kp where the PP stands NAY, and 0 otherwise.
  - A president whose party holds no House seat vetoes nothing.
- IDENTITY, asserted in every run, reading and replicate: under the cap, (a) fires exactly
  where the PP's House stance is NAY and S_PP,H >= 1. The draw decides whether a bill
  REACHES the president, never whether (a) fires. Under (B), NAMED = VETOABLE SENT and
  E[named] = E[vetoable sent], asserted. A bill the PP opposed only in the Senate is signed
  under (A).
- (a) is DETERMINISTIC (H1: "Rule (a) then decides"). R12 prints a drawn veto at (a)'s
  fitted precision.
- (a) reads every act presented to an AI president, budget acts included (US-22: "both acts
  may be vetoed"), on their bloc divisions. Only the draw is withheld from budget votes.
- A player-president chooses (US-21/US-23), and (a) is not read for him. Congress's draw is
  the same whoever is president.

==========================================================================================
THE OVERRIDE
==========================================================================================
- BASE (CRS RS22654): "two-thirds of the Members voting, a quorum being present". A chamber
  overrides iff Y >= 2*N.
- SIDES: US-21's "voted by the same sides" means the passage's RECORDED division in that
  chamber. Crossers count only where they entered under item 6, and no fresh draw is made.
  Reason: the extract holds override totals without a party split, so no override crossing
  can be fitted. Per premise:
  - (B): Y = the YEA side of that division over the SEATS; N = W - Y, where W_H counts
    House independents against and W_S includes the caucus.
  - (A): Y and N over the roll call's R and D blocs (members), plus the Senate caucus
    entries; House independents outside.
  - (A), chamber with no roll call: the SEATS, with the PP's bloc AGAINST, the OP's FOR and
    no crossers.
- ORDER: the ORIGIN chamber votes first, and the veto falls iff both chambers override.
  Where the origin fails, the other chamber does not vote. Both votes fall on the passage's
  day (Poland's premise). Cloture on an override is not modelled, and with every senator
  voting, the two-thirds bar is never below three fifths of the sworn (asserted).
- PRINTED ONLY, never deciding:
  - omega_j = P(sent and both chambers override), exact. Each chamber's (pass and override)
    probability is enumerated, and the two are multiplied.
  - P(override | named) and the expected overrides.
  - Every recorded override vote beside the model's, and whether 116|H.R. 6395 is in V.
  - US-21's sentence "under it no Congress of record overrides", measured again with the
    draw.
  R15 prints (a) the passage's full drawn crossers applied to the override and (b) a fresh
  keyed draw ("|override") from the same cells.

==========================================================================================
THE PRINT (H2)
==========================================================================================
- A FITTED FIGURE is any number computed from the cells' observations, under the default
  or any reading: the cell figures, every per-measure probability, every expected count,
  precision, hit rate and P(NONE), the INFLUENCE figures, and every in-game chance. It
  prints in the label below on EVERY surface: the document, the catalog's comments, any
  screen and any reply.
- NOT FITTED, printed plainly as "x of y": counts of the record (measures, vetoes, roll
  calls, n, |V|, |V_XXII|, the draw-free share of V), the companions, B1's yardstick and
  the identities at K = 0.
- DRAWN (not fitted; a check): the seeded realizations' statistics, each naming the FITTED
  figure it checks.
- n: the number of observations in the cells the figure reads (full cells; a pooled cell
  counts its union).
- m, for a derived figure: 1 for a per-measure figure; |V| for the sums over V, precision
  and the pooled P(NONE); |V_XXII| for E_XXII and P(NONE_XXII); V_COUNT for the hit rate.
  Cell figures carry no m.
- INTERVALS, all at 95%:
  - CELL PROPORTIONS (q_any, q10): Clopper-Pearson exact, CP. With x of n, lo solves
    P(X >= x | n, p) = 0.025 and hi solves P(X <= x | n, p) = 0.025, each by bisection on
    the exact binomial sum to |delta p| <= 1e-12. lo = 0 at x = 0, and hi = 1 at x = n.
  - EVERY OTHER FITTED FIGURE: the percentile bootstrap, BOOT, B = 2000 (record
    resampling).
    - Replicate b (1..2000) makes M picks with replacement from the counted measures in
      CSV order. Pick t (0-based) is index floor(Unit("R-US17|boot|" + b + "|" + t) * M).
    - Its cells hold the observations of the roll calls OWNED by the picked measures, with
      the picks' multiplicity. Pooling and fitted status are held from the full fit.
    - PER-MEASURE figures: every measure j of the full V is scored from the replicate's
      cells, LOO (every copy of j's roll calls removed), whether or not j was picked.
    - TOTALS: taken over the replicate's picks with multiplicity, using those per-measure
      figures. The replicate's V_COUNT is its picked counted vetoes with multiplicity.
    - Computed in IEEE double, with sums in pick order t = 0..M-1.
    - A replicate whose ratio has a zero denominator is UNDEFINED, and k is the number of
      such replicates. The UNDEFINED values sort first. If k >= 50 the interval prints
      "UNDEFINED IN <k> OF 2000". Otherwise lo = sorted[49] and hi = sorted[1950]
      (0-based), and for 1 <= k < 50 " (<k> of 2000 replicates undefined)" is appended.
- THE LABEL, exactly:
    "<value> FITTED n=<n> 95% [<lo>, <hi>] CP"            for a cell proportion;
    "<value> FITTED n=<n> 95% [<lo>, <hi>] BOOT"          for the mean share;
    "<value> FITTED n=<n> m=<m> 95% [<lo>, <hi>] BOOT"    for every derived figure.
  For each pooled cell the figure reads, " (<XX> pooled with <YY>)" follows n=<n>.
- NUMBER FORMAT: shares, probabilities and rates print as percent to one decimal ("41.3 %");
  expected counts print to two decimals. An exact rational rounds half up on its exact
  value; a double rounds half up on its exact decimal value. Invariant culture, "." as the
  decimal mark.
- TOO THIN (H4's form):
  - an UNFITTED cell prints "UNFITTED n=<n> - fewer than 12 observations (pooled n=<n'>)",
    with no figure and no interval;
  - every figure that reads it prints "UNFITTED", with no value;
  - a derived figure with m = 0 prints "UNFITTED m=0";
  - a ratio whose exact denominator is 0 prints "UNDEFINED (denominator 0)", with no
    interval.
- DRAWN FORM: "<mean> DRAWN 1000 worlds [<26th>, <975th>] - seed noise of a single game,
  not fit uncertainty; checks <figure> FITTED n=<n> above".

==========================================================================================
SEEDS AND DETERMINISM
==========================================================================================
- UNIT(key): FNV-1a 64 over the key's UTF-8 bytes (offset 14695981039346656037, prime
  1099511628211), finished with MurmurHash3's fmix64. The result is (h >> 11) / 2^53. This
  is PresidentialVeto.Draw's arithmetic, implemented in UsVeto. PresidentialVeto.cs is not
  edited. Asserted: UsVeto's Unit on Poland's key format equals PresidentialVeto.Draw on 8
  fixed (masterSeed, date, title) inputs outside any plant scope.
- No SimulationRandom stream is created, drawn or moved, and its enum is unchanged. Reasons
  (Poland's precedent): a stream draw shifts every later draw; a projection must consume
  nothing; a reload, the shadow and a fork that reach the vote on the same day must draw
  alike.
- THE PLAY KEY: masterSeed (invariant) + "|USA|" + yyyy-MM-dd + "|" + title + "|" +
  ("House" | "Senate") + "|" + ("REP" | "DEM") + "|defectors".
  - The date is the date of THAT CHAMBER'S FIRST DIVISION on the bill. It is carried on the
    bill for every later vote of that chamber on it, and US-21's diagnostic asserts one key
    per chamber per bill.
  - Two divisions with one title in one chamber on one day are one act. A bill retitled,
    or first divided on another day, draws apart (Poland's premise, stated).
  - UsVeto has its own plant scope and never reads PresidentialVeto's _planted.
- THE BACKTEST'S DECISION FIGURES are EXACT, and no seed decides anything.
  - Per chamber: P(pass) = (number of index pairs (j_PP, j_OP) over the LOO lists that
    pass) / (n_PP * n_OP), as a reduced rational. A party with no draw, or an empty list,
    is the single index "K = 0" (n := 1).
  - P(sent_j) = P_H * P_S. Histograms of K may implement this but must equal the
    enumeration.
- THE SEEDED REALIZATIONS (DRAWN, printed only):
  - master seeds 1..1000 run the play key over every measure of V, on the LOO lists;
  - the title is "<congress>th Congress <measure>", and party R -> REP, D -> DEM;
  - the date is the chamber's final date, or decision_date where it has none, and it
    serves every vote of that chamber.
  - Asserted (a failure is a DEFECT): for E[vetoable sent], E_XXII[vetoable sent] and
    E[hits], |mean - exact| <= 4 * sd / sqrt(1000). sd is the sample sd (1/(N-1)) in
    double, summed in seed order; equality is required where sd = 0.
- The bootstrap keys ("R-US17|boot|", and R17's "R-US17|cboot|") carry no master seed and
  are disjoint from every play key, which begins with an integer.
- A second run in one session gives the same decisive digest (asserted).

==========================================================================================
THE FIRST RUN (what the backtest on the record measures and prints)
==========================================================================================
THE INSTRUMENT: a new Editor check, Assets/Editor/UsVetoDrawCheck.cs, in the cheap and
documents groups. It calls the pure runtime class Assets/Scripts/Elections/UsVeto.cs
(Applies, Unit, Key, Crossers, ChamberOutcome, PassProbability, NamedByA, OverrideCarries)
on chambers built from the CSV, so the backtest runs the game's arithmetic.
- It writes docs/generated/US_VETO_DRAW.md, stamped with SourceDigest, the pins, the
  block's digest, the decisive digest and the ruling-record state.
- It writes the catalog (HOW THE FIGURES REACH THE GAME).
- us_veto_backtest.pl, US_VETO_BACKTEST.md and UsVetoBacktestCheck stay byte-unchanged, as
  the record of s802.
- The first run is on the block as preregistered (H1, H3). A choice changed after a result
  is in sight takes a new digest and is reported to Elias (G1).

Per measure j of V, under (B), exact and LOO: P_H(j), P_S(j), P(sent_j), P(named_j) and
omega_j; VETO_j = 1 iff j is a counted veto.

Totals, pooled and per president. A term with no counted veto prints "no counted veto
(H2)".
- E[vetoable sent] = sum over V of P(sent_j); E_XXII[vetoable sent] = the same over V_XXII
- E[named] = sum over V of P(named_j) (asserted equal to E[vetoable sent])
- E[hits] = sum over V of VETO_j * P(named_j)
- E[false positives] = E[named] - E[hits]; E[misses] = V_COUNT - E[hits]
- PRECISION = E[hits] / E[named]; HIT RATE = E[hits] / V_COUNT
- P(NONE_XXII) = product over V_XXII of (1 - P(sent_j)); P(NONE) = the same over V;
  P(NONE_EXP) = the same over V's MAJORITY-procedure measures.
Each prints FITTED with its BOOT interval and, where it is a count, its DRAWN spread. B1's
yardstick prints beside the precision.

PRINTED, in this order:
1. THE RECORD per president: measures and vetoes, counted and in all. The UNPREDICTABLE
   line. |V| beside F3's 34, with the differences. |V_XXII|. The RECORD BUDGET CLASS
   matches and NEAR MISSES.
2. THE CELLS: n; every observation (identity, owner, d, m) in fit order; pooled or
   UNFITTED; q_any and q10 (CP); the mean share (BOOT). The selection count per chamber
   (x of y).
3. PER MEASURE OF V, in CSV order: kind, Senate procedure, budget class, president,
   recorded outcome; each chamber's procedure, bloc margin at K = 0, MARGIN and roll-call
   presence; the cells read; P_H, P_S, P(sent), P(named) and omega (FITTED); and P(the draw
   matters) in each chamber.
4. THE TOTALS above, with the SENDING CALIBRATION beside them: E[vetoable sent] beside
   "|V| - all sent on the record" and E_XXII beside "|V_XXII| - all sent on the record",
   both not fitted. E[named] prints beside V_COUNT. The precision prints beside the
   draw-free share of V (counted vetoes in V, as x of |V|, not fitted).
5. Every counted veto with P(named), FITTED (or "not in V"); every measure named with P > 0
   and not vetoed.
6. THE OVERRIDE's prints.
7. THE SEEDED REALIZATIONS (DRAWN).
8. COMPANIONS, not fitted, on the counted base only (V_COUNT as the denominator), by
   us_veto_backtest.pl's own definitions: (a) named and vetoed; both chambers; one stance;
   chamber by chamber, vetoable and sendable vetoes. A pointer gives
   docs/generated/US_VETO_BACKTEST.md for US-10's figures over all rows.
9. Every shared roll call; every caucus entry ignored as neither R nor D; every unknown
   kind; every roll call with no OP observation; every observation with m = 0; every
   empty-list occurrence.
10. THE IDENTITIES, asserted (a failure is a DEFECT):
    - SEATS equal US_VETO_BACKTEST.md's seats table;
    - US-10 IDENTITIES, in US-10 MODE, printed as "US-10's record (s802), not this rule's
      figure":
      (i) per measure of F3, at K = 0 with the OP forced YEA on full seats, the (B) send
          verdict equals US-10's one-stance verdict, and the count equals the document's;
      (ii) (A) at K = 0 over all rows reproduces the document's chamber-by-chamber counts
           (measures sent, vetoable, vetoes sendable);
      (iii) (a) on the recorded House votes reproduces the document's (a) pooled row;
      the measures of V minus F3, and those where the OP's recorded stance is NAY, are
      printed with their K = 0 verdicts;
    - every share <= 1/2;
    - (a) fires exactly where the PP's House stance is NAY, and NAMED = VETOABLE SENT
      under (B);
    - the UNPREDICTABLE set; B1 = 45 of 166; Unit = PresidentialVeto.Draw on the 8
      inputs; the seeded means agree with the exact figures; the pins.
11. INFLUENCE (printed only): for each distinct fitted roll call, its observations are
    removed from every cell, LOO lists included, with pooling and fitted status held. The
    outcome is recomputed exactly. Every roll call whose removal changes the outcome is
    listed with the new outcome, and k = their number.
12. DECLARED READINGS, each with its outcome and headline figures (E_XXII[vetoable sent],
    P(NONE_XXII), E[vetoable sent], E[hits], precision, hit rate), labelled.
13. THE DECISION, the verdict line and the decisive digest.

==========================================================================================
THE DECISION (the outcome of a run defined once)
==========================================================================================
H1 has ruled (c). A run installs (c) or re-asks; it never chooses among options. The
outcome reads only the default's exact LOO figures under (B).

THE STOPS, evaluated in this order:
- S3, TOO THIN (the panel's addition, not one of R-US17's two STOPs): any cell is UNFITTED
  after pooling. A decision cannot be computed on an unfitted cell (H4), so the rule
  re-asks. When S3 fires, S1 and S2 are not evaluated, and their figures print UNFITTED.
- S2, NEVER SENDS (R-US17's second STOP, as worded: "a party-bloc Congress under Rule XXII
  never sends a vetoable bill"; the record's measures of V_XXII stand for the vetoable
  bills). It fires iff:
  - case "no Rule XXII measure on the veto path": V_XXII is empty (its figures print
    "UNFITTED m=0"); or
  - case "never sends": E_XXII[vetoable sent] = 0 exactly (that is, P(NONE_XXII) = 1).
- S1, FAR MORE (R-US17's first STOP as G5 reads it, PROVISIONAL; also R-US17's test "(a)
  only if its precision ... is at least B1's"), pooled over every kind. It is evaluated
  only where E[named] > 0, and it fires iff 166 * E[hits] < 45 * E[named]. Where
  E[named] = 0, it prints "not evaluated (denominator 0)".

THE OUTCOME is exactly one of: INSTALL (no stop fires); S3; S1; S2; S1 + S2. Its figures
are not part of it.

PRINTED ONLY, never part of the outcome:
- every interval, and where 27.1 % lies against the precision's BOOT interval (below,
  inside or above);
- the pooled and expedited-only P(NONE), the hit rate, the per-president figures, the
  override, the realizations, the companions and INFLUENCE;
- every declared reading and its outcome. A reading whose outcome differs is named in the
  verdict line, and no reading ever stops the rule.

THE VERDICT LINE, generated and never typed:
- INSTALL: "By R-US17's rule (c) as ruled (H1-H3), the draw installs: under Rule XXII a
  bloc Congress with the draw sends <E_XXII[vetoable sent]> vetoable measures of the record
  FITTED n=.. m=.. 95% [..] BOOT (all <|V_XXII|> were sent on the record), P(none under
  Rule XXII) <q> FITTED ...; over every kind it sends and names <E[named]> FITTED ...,
  <E[hits]> of them vetoed FITTED ...; precision <p> FITTED ... against B1's 27.1 % (45 of
  166), the draw-free share of V <x> of <|V|>; hit rate <h> of the <V_COUNT> counted
  vetoes FITTED ...; every cell fitted; H.R. 131 and H.R. 504 (119th) declared
  unpredictable, in no count (H2); <FRAGILE: <k> roll calls each flip the outcome | no
  single roll call flips the outcome>; <the same under every declared reading | the
  outcome differs under <readings>: <outcomes>> - held INSTALLED-PENDING-E4: the rates go
  to Elias before US-21 wires them (E4)."
- STOPPED: "R-US17's rule (c) does not install: <each stop with its figures and, for S2,
  its case>; <FRAGILE clause>; <readings clause> - asked."
Whatever the outcome, the full document goes to Elias (E4).

THE STOPS' OPTIONS, each with "amend the rule as ruled" beside it:
- S3: pool all four cells into one; pool per chamber across roles (R11); or source more
  roll calls (the Senate's cloture and failed votes, BILLED).
- S2: install (c) as measured; R1, reading (A), as play's premise (this amends US-18's one
  stance, re-pinned); source the Senate's cloture and failed votes (BILLED) and run again;
  or keep the blocs with no draw (an AI president never vetoes; US-21 builds the plate for
  a player-president).
- S1: install (c) as measured; (c) with R12's drawn veto; or keep the blocs, as in S2's
  last option.

==========================================================================================
WHAT STAYS AS IT IS AND ITS PROOF
==========================================================================================
- US-10: Tools/us_veto_prep.pl, us_veto_measures.csv (held to the pins),
  us_veto_backtest.pl and US_VETO_BACKTEST.md are byte-identical, and UsVetoBacktestCheck
  is unchanged and green. Tools/us_veto_mutations.sh stays 13 of 13 (s831) and is re-run
  whenever the prep tool changes, followed by this instrument.
- BUDGET VOTES draw nothing (H1):
  - UsVeto refuses every budget vote, and its draw-call counter reads 0 on that path.
  - On the record, no RECORD BUDGET CLASS measure draws (asserted).
  - Planted cases: a contested budget act at margin 1 draws zero.
  - US-21's diagnostic plants j = n - 1 (the largest share): every US budget division is
    identical to the unplanted run, and the counter reads 0. This is installed at US-21
    and re-run at US-22.
- THE FIVE OTHER COUNTRIES (Sweden, Germany, France, Italy, Poland):
  - UsVeto.Applies is true for CountryId.USA alone, asserted over all six values.
  - The draw counter reads 0 on every non-US path the diagnostic runs, and SimulationRandom
    is unchanged.
  - The simulation bar's trajectory sentinel holds byte-identical against its pins on both
    seeds (777 and 424242), and the dump diff is inert. Nothing calls UsVeto in play before
    US-21.
- POLAND'S B1/F3 (H1: "proved unchanged on the simulation bar"):
  - PresidentialVeto.cs, Generated/PolishVetoRates.cs and docs/generated/VETO_B1_BACKTEST.md
    are byte-identical (the review row shows an empty git diff over the three).
  - PresidentialVetoGate's body is untouched.
  - PresidentialVetoDiagnostic with its D4 and E2 cases, and GovernmentBudgetBillDiagnostic's
    Poland pins, pass unmoved.
  - The sentinel holds.
  - All of this is required in the first-run commit and again at US-21's.
- R-US18's and R-US19's block digests are unchanged (5c89b0cf..., 1caec656...).

==========================================================================================
TRUMP'S 2025 VETOES
==========================================================================================
H.R. 131 and H.R. 504 (119th) passed both chambers without a roll call; both were vetoed
and sustained in the House on 8 January 2026. Under H2 they are UNPREDICTABLE and never
counted for or against. They are:
- not in the fit (no roll call; asserted to own no observation);
- not in V or V_XXII, and not in any expected count, precision, hit rate, P(NONE), V_COUNT,
  M or the bootstrap population;
- not in the realizations or INFLUENCE;
- not in the override's counts: their sustained votes print on their own line only;
- not in the per-president rows (Trump's second term: "0 counted vetoes; 2 declared
  unpredictable");
- not in the companions (counted base only).
They are read by THE US-10 IDENTITIES only as US-10 read them, printed as US-10's record,
and they enter the decisive digest by name only.
One line prints them: "UNPREDICTABLE (H2): H.R. 131, H.R. 504 (119th) - passed both
chambers without a roll call; never counted for or against."

==========================================================================================
HOW THE FIGURES REACH THE GAME (US-21)
==========================================================================================
- THE CATALOG: Assets/Scripts/Elections/Generated/UsVetoDefections.cs (namespace
  PoliSim.Elections.Generated, "GENERATED - DO NOT EDIT"), written by UsVetoDrawCheck.
  - It holds SourceDigest, the pins, RuleDigest, DecisiveDigest, Outcome,
    MinimumObservations = 12 and M.
  - For each of HP, HO, SP and SO it holds Fitted, PooledWith and the EXACT LIST THE CELL
    DRAWS FROM: already merged for a pooled cell, in fit order, each entry (Identity,
    OwnerIndex in counted CSV order, Defectors, Voting), with Identity and OwnerIndex as
    fields.
  - The runtime indexes these arrays and never sorts or merges them. LOO stays
    instrument-side.
  - The cheap bar regenerates the catalog in memory and fails on any byte of difference
    (Generated.PolishVetoRates' pattern).
- THE RUNTIME: UsVeto.cs, pure, lands with the instrument and stays unwired until US-21.
  The gate alone draws, and a projection reads exact probabilities and never draws.
- H2 IN PLAY: any surface that shows a fitted chance shows it in THE PRINT's label, with
  its BOOT interval. The interval is computed from the catalog by THE PRINT's replicate
  rule (the keyed picks over M, owners' multiplicities), so it is the same interval the
  instrument would compute. Whether a surface shows a fitted chance at all is US-21's
  design.
- US-21'S VETO DIAGNOSTIC asserts:
  - the ruling record INSTALLED (with its E4 section) or RULED with a buildable option, at
    the current block and catalog digests;
  - the projection's draw counter at 0, and the budget and five-country counters at 0;
  - one play key per chamber per bill;
  - for every counted veto in V, enumerating the runtime's planted indices reproduces the
    instrument's exact P(sent) and P(named) as the same rationals (on the full cells).

==========================================================================================
DECLARED READINGS (each runs the whole rule with one change; outcome and headline figures
printed; never a stop)
==========================================================================================
- R1: (A), chamber by chamber as recorded (s802's declared reading, with the draw and the
  Vice President), in place of (B).
- R2: the fit over every contested roll call (R and D bloc stances opposite). Each party's
  observation is taken against its own stance, with cells by chamber and role, pooled over
  stances. These are the cells a draw on every contested ordinary bill would use, printed
  split by stance.
- R3: cells split by Senate procedure, MAJORITY apart from RULE XXII (eight cells, each
  under the thin-cell rule).
- R4: cells by party label (R/D) in place of role.
- R5: in-sample, no LOO.
- R6: selection removed. The PP cells keep only roll calls whose chamber's recorded bloc
  result at K = 0 already passed. Every cell left empty is printed with "no
  selection-free observation on disk" and falls under the thin-cell rule.
- R7: a joint draw: one index per chamber over the PP cell's roll calls gives both parties'
  shares (Ko = 0 where that roll call has no OP observation).
- R8: the binomial, K ~ Binomial(S, sum d / sum m), capped as in the model (doubles, exact
  pmf).
- R9: the role taken from the vote-day president in place of the decision president.
- R10: present outside the stance and the observation (stance YEA iff yea > nay; d over
  yea + nay); any share above 1/2 is capped by the model.
- R11: a thin cell pooled with the other role of its chamber.
- R12: the veto drawn at (a)'s FITTED precision (F3's form): expected vetoes = E[named] *
  precision.
- R13: S2 at P(NONE_XXII) >= 1/2; S2 at E_XXII[vetoable sent] < 1; S2 over every kind (V
  in place of V_XXII); S1 on the BOOT upper bound (strict) and on the lower bound (lax).
- R14: N_MIN = 20.
- R15: the override with (a) the passage's full drawn crossers, whether or not they
  entered the passage, and (b) a fresh keyed draw ("|override") from the same cells.
- R16: the RECORD BUDGET CLASS (a) scored as its kind with the draw (reconciliation as
  RULE XXII), and (b) with its roll calls removed from the fit.
- R17: the cell bootstrap: the scoring set fixed (the real V, VETO_j), with the owners of
  observations resampled. With O owners in CSV order, pick t of replicate b is
  floor(Unit("R-US17|cboot|" + b + "|" + t) * O). It changes intervals only.
- R18: debt-limit titles outside the RECORD BUDGET CLASS (scored as their kind with the
  draw).

SETTLED, NOT READINGS: present stands against (US-10, F3); the independents (US-10); the
Vice President (equal to US-10 on V); the override base (sourced); the UNPREDICTABLE set
(H2); the narrow scope (H1).

PREMISES, stated and not read:
- the record holds passed measures only (selection toward sending);
- the parties' and chambers' draws are independent;
- the House majority without suspension;
- the record's budget class read from titles (the extract has no budget column), with
  unmatched reconciliation acts scored as their kind;
- the ten days and the pocket veto are not modelled;
- retitling a bill, or first dividing it on another day, redraws (Poland's premise).

==========================================================================================
QUESTIONS FOR ELIAS
==========================================================================================
None. H1-H3, with G1, G5, E4 and H4, settle every choice. Each remaining choice is a
default with its reason and, where it is contestable, a reading printed beside it and
named in the verdict line where its outcome differs.

==========================================================================================
THE RULING RECORD AND THE BARS
==========================================================================================
- THE BLOCK sits between "<!-- R-US17 RULE BEGIN -->" and "<!-- R-US17 RULE END -->",
  after H1-H3, in plain ASCII inside a ```text fence, with the pins filled. Its digest is
  SHA-256 over the UTF-8 bytes (no BOM) of the lines strictly between the marker lines,
  fence lines included, each ending LF with no CR. It is read from the index blob's raw
  bytes and required equal to the working copy's.
- THE DIGEST LIST: the preregistered digest is recorded in COMPLETED.md sNNN and in the
  plan's "The block's digests" line (R-US18's form) at the preregistering commit.
  - UsVetoDrawCheck's RuleDigests is created in the first-run commit, with that digest as
    its first entry ("preregistered (sNNN)"), then one entry per amendment naming Elias's
    ruling ("AMENDED (Hn, sNNN): old -> new").
  - The block must equal the list's last entry. A change without a ruling fails.
  - The first-run commit's review row shows the preregistering commit's plan blob hashed
    to the first entry, and that commit preceding the one that adds UsVetoDrawCheck.cs.
- THE DECISIVE DIGEST: SHA-256 over these lines, UTF-8 without BOM, each "key=value"
  ending LF, in this order:
    source=<SourceDigest>
    cell.<XX>.status=FITTED|UNFITTED        (XX in HP, HO, SP, SO, in that order)
    cell.<XX>.pooled=<YY>|-
    cell.<XX>.n=<n>
    cell.<XX>.obs.<i>=<identity>|<congress>|<measure>|<d>|<m>   (i 0-based, drawn list,
                                                                 fit order)
    unpredictable.<i>=<congress>|<measure>  (CSV order)
    counted=<M>
    v_count=<V_COUNT>
    v=<|V|>
    v_xxii=<|V_XXII|>
    p_sent.<congress>|<measure>=<p/q>       (every measure of V, CSV order; then p_named)
    p_named.<congress>|<measure>=<p/q>
    e_sent=<p/q>  e_sent_xxii=<p/q>  e_named=<p/q>  e_hits=<p/q>  p_none_xxii=<p/q>
                                                    (one line each, in this order)
    outcome=INSTALL|S3|S1|S2|S1+S2
  Rationals are "p/q" in lowest terms, q > 0, zero written "0/1", with no sign. Nothing
  else enters: no interval, reading, (A), override, realization, INFLUENCE or companion.
- THE RULING RECORD, held by the instrument:
  - NONE: the state before the instrument exists. The instrument fails while it reads
    NONE, so the first-run commit registers the outcome.
  - INSTALLED-PENDING-E4 (sNNN): a first run that gives INSTALL, with its decisive digest.
  - INSTALLED (sNNN; E4 sMMM): once a COMPLETED.md section sMMM records the reply to Elias
    that carried the rates.
  - ASKED (sNNN), with the outcome and the digest.
  - RULED (Elias's ruling, sNNN), with its option, the outcome it was made on and the
    digest.
  - Each state passes while the generated outcome and the decisive digest equal the
    registered ones.
  - A CHANGED OUTCOME, in either direction and from any state, fails until it is
    registered ASKED with the old and new outcomes and digests. Only Elias's ruling moves
    ASKED to RULED.
  - A MOVED DIGEST WITH THE SAME OUTCOME fails until a commit re-pins it, naming the old
    digest, the new one, its cause (source, code or amendment) and "outcome unchanged".
    The next reply to Elias lists the per-measure P(sent) differences (E4, G1). A changed
    SourceDigest after the first run is always reported this way.
  - A ruling must name a buildable option: (c) as measured, a stop's option, or (c)
    amended by name. Otherwise the record stays ASKED.
  - THE MIRROR: the line after the block mirrors the state, section and ruling, never a
    digest: "**Ruling record:** INSTALLED-PENDING-E4 (sNNN): (c)." /
    "**Ruling record:** INSTALLED (sNNN; E4 sMMM): (c)." /
    "**Ruling record:** ASKED (sNNN): S1 + S2." /
    "**Ruling record:** RULED (sNNN): <option>."
    The instrument fails while the mirror and the record differ.
- THE MUTATION SUITE: Tools/us_veto_draw_mutations.sh. Each case must fail the check with
  its own message, and the control reproduces the document and the catalog byte for byte.
  The cases:
  - a third vetoed row with no roll call;
  - H.R. 131 given a roll call;
  - a share above 1/2;
  - a CSV byte changed (the source pin);
  - an edited catalog list;
  - the block edited without a listed digest;
  - the mirror line changed;
  - a RECORD BUDGET CLASS measure made to draw;
  - planted runs proving the UNFITTED, S3, S2 (both cases) and S1 paths (s830's form).
- THE BARS:
  - THE PREREGISTERING COMMIT (documents only, no instrument): the block with its pins;
    the digest in COMPLETED.md sNNN and in the plan's digests line; the documents batch,
    with MojibakeCheck, DocumentClaimCheck, ResidueCheck and PreWiringPremiseCheck by name.
  - THE FIRST-RUN COMMIT: UsVeto.cs, UsVetoDrawCheck.cs (RuleDigests first entry = the
    preregistered digest), the catalog, the document and the mutation suite; the cheap bar
    with UsVetoDrawCheck added; UsVetoBacktestCheck unchanged and green; the documents
    bar; the simulation bar (Poland's proof and the five countries inert, H1); R-US18's
    and R-US19's digests unchanged; the review row's preregistration proof; the ruling
    record registered (INSTALLED-PENDING-E4 or ASKED).
  - US-21: everything above, plus its veto diagnostic as stated in HOW THE FIGURES REACH
    THE GAME, and its own done-when.

BLINDNESS (the reviser's own): I read the brief, the plan's US-10, US-18 to US-22 and
R-US17 lines, the end of R-US18's block, us_veto_backtest.pl's source, us_veto_prep.pl's
kind_of, measure labels and title source, veto_record.md's readings, PresidentialVeto.Draw
and UsHouseCountCheck's RuleDigests. I computed no figure of the record or of any model,
ran no tool or Unity process, and edited nothing.
```

## Departures from the rulings' words (the reviser's list)

- S3 (TOO THIN) is the panel's addition. It is not one of R-US17's two STOPs, and H4 never made an unfitted cell a stop of a whole rule. The rule re-asks because a decision cannot be computed on an unfitted cell.
- H1 says the draw is 'fitted on the recorded chamber-by-chamber votes since 2017'. The fit reads only the veto-path roll calls, those where the PP's bloc stood NAY, not every recorded vote. R2 prints the fit over every contested roll call.
- H1 says 'a seeded draw of defectors per party per chamber'. A draw is made only where crossing could move a veto-path bill toward passage: the PP standing against, the OP standing for. No draw is made for an OP standing against, a third party, a budget vote or an unfitted president.
- H1 says 'the draw matters only where the chamber's margin is within the drawn count'. This is made exact as 'the outcome at the drawn crossers differs from the outcome at zero', and drawn crossers enter a recorded division only then.
- H1 says 'seeded'. Play draws by seed, but the backtest's decisive figures are exact enumerations, so no seed decides the outcome. Seeded realizations are printed only, as a check.
- H1 says budget votes 'stay as they are'. On the record, budget measures are identified by fixed title tests and scored with no draw, and reconciliation takes US-18's majority path. Their roll calls still enter the fit. Reconciliation acts whose display title lacks the words are missed and scored as their kind, a declared premise.
- In play, US-24's debt-limit act is classed as a budget vote, which draws nothing. This is the panel's default, and R18 prints the other class.
- R-US17's second STOP is read literally ('never sends' means E_XXII[vetoable sent] = 0 exactly), over V_XXII. The record's veto-path measures under Rule XXII stand in for 'a vetoable bill'.
- H2 says Trump's two 2025 vetoes are 'never counted for or against'. The US-10 identity checks still read all rows, those two included, only to verify the re-implementation, and they print as US-10's record, never as this rule's figure.
- H2's label 'FITTED n=...' is extended. A derived figure also prints m= (the measures in its denominator), and every label names its interval method (CP or BOOT). The seeded realizations print as DRAWN, not FITTED.
- US-21's 'voted by the same sides' is read as the passage's recorded division, with crossers only where they entered. The full passage draw and a fresh draw are reading R15.
- H3's preregistration is made stricter: the CSV's SHA-256 and the prep tool's blob id are pinned in the block. A changed source before the first run is a DEFECT until Elias's ruling re-pins it.
- Under E4, an INSTALL outcome is held as INSTALLED-PENDING-E4 until the reply carrying the rates is recorded. After the first run, any change of outcome in either direction returns the record to ASKED, and only Elias's ruling settles it again.
- The no-party president (no draw, (a) never fires) is the panel's default by analogy to Poland's G5 items 14-15, not a US ruling.
- Suspension's two thirds in the House is not modelled. That is this block's own premise; US-18 does not say it.

## The judge's scores, spine and grafts

| design | fidelity | computability | untunable | game fit | form |
|---|---:|---:|---:|---:|---:|
| game | 7 | 7 | 8 | 9 | 8 |
| inference | 6 | 8 | 8 | 7 | 7 |
| precedent | 8 | 8 | 9 | 8 | 9 |

**game:** Strongest on the game side. The instrument calls the runtime's own pure functions (UsVeto). Draw counters prove budget and five-country inertness. The gate is a new UsVetoGate, so PresidentialVetoGate is never touched. The key format is correct (CountryId.USA prints "USA"; parties REP/DEM checked in PartySystem.cs). S2 survives H1 in a real probabilistic form, P(NONE) >= 1/2. Weaknesses: (1) It defaults the draw to EVERY ordinary bill with opposed blocs (R10), against H1's "on the USA's veto path only" and "parties stay blocs by default". (2) The defector is defined against one stance summed over both chambers, not the "recorded chamber-by-chamber votes". This lets shares exceed 1/2, which can flip a bloc and break (a)'s link to the stance. (3) The thin-cell threshold's reason is arithmetically wrong: the 95% Wilson width at p=1/2 is 0.527 at n=10, so it is still wider than half there. (4) It proposes changing us_veto_prep.pl (new caucus columns) before the first run. That is unnecessary, because US-10 already keeps independents out of every split. (5) Rule (a) reads the RECORDED division, which differs from the drawn one. (6) The backtest's one-stance premise is not tied to US-10's %seatsOf, so the 0-of-34 identity is lost.

**inference:** Best statistical craft. The cap K <= floor((S-1)/2) keeps the bloc's stance and gives the clean identity that (a) fires exactly where PP's House stance is AGAINST, so the draw decides only reach. Clopper-Pearson for cell proportions is honest at small n. The measure-level bootstrap carries both the fit's and the scoring's thinness. Seed noise, fit uncertainty and decision figures are kept apart. Weaknesses: (1) It replaces R-US17's second STOP with a hit rate below B1's 45 of 53. That changes what the stop means and needs a question. (2) S4 lets a structural reading stop the rule, though H1 has already ruled the option. (3) The backtest uses chamber-by-chamber recorded stances, not the plan's one-stance premise, which is the premise s802 stopped on. (4) Senate independents are dropped from the blocs when the caucus is mixed. That departs from US-10's seats. (5) A separate Perl tool plus a C# check duplicates the arithmetic away from the runtime. (6) Its N_MIN of 20 has no derivation.

**precedent:** Best spine. The draw is scoped to the veto path (PP AGAINST); I read H1 the same way. The backtest default (B) reuses US-10's %seatsOf and bloc rule exactly. With every K=0 it reproduces US-10's 0 of 34 and its 13 vetoable / 5 of 25 under reading (A), as asserted identities. Cells are by role AND stance (HP/HO/SP/SO), which matches the use. The present handling (an abstention against, F3) keeps every share <= 1/2 consistent with the stance rule. N_MIN = 12 is derived without data, and the Wilson widths check out (0.492 at 12, 0.509 at 11). Decision figures are exact rationals. The outcome is defined once, readings never stop, and the block, digest and ruling record follow R-US18/R-US19 (the R-US18/R-US19 digests it cites are verified). Weaknesses: (1) The key uses "Usa" and CountryId.Usa, but the enum member is USA. (2) An UNFITTED cell draws K=0 and still counts in the decision, against H4's spirit. (3) S2 is the bare E[named] = 0, which almost never fires under a draw. (4) K rounds half up with no cap, so at a share of exactly 1/2 and odd S it can flip the PP majority. (5) The cluster bootstrap over roll calls ignores the scoring's thinness, which H2's 13 and 25 name. (6) The fit order assumes no roll call is shared between measures.

**Spine.** The spine is the precedent designer's specification. Its draw is scoped to the veto path: a party standing AGAINST the president's party's interest draws, and only there. Its backtest premise (B) is US-10's own one-stance arithmetic (%seatsOf, the bloc rule, independents in seats only), so with every K = 0 the identities reproduce s802's 0 of 34 and, under reading (A), its 13 vetoable / 5 of 25. Its cells are by role and stance, with present counted against as in F3, so every share is <= 1/2. N_MIN is 12, derived without data. Decision figures are exact rationals. The outcome is defined once, readings print and never stop, and the block, digest and ruling record follow R-US18/R-US19's form.

**Grafts.**

GRAFTS, AND THE CONFLICTS RESOLVED
1. Key string. From game and inference: masterSeed|USA|date|title|House or Senate|REP or DEM|defectors. CountryId.USA.ToString() is "USA" (checked in CountryId.cs), so the precedent's "Usa" is corrected. Party codes REP/DEM were checked in PartySystem.cs.
2. The cap. From inference: K = min(floor((S-1)/2), round-half-up of S*d/m), with K = 0 at S = 0. With it the asserted identity holds: (a) fires exactly where PP's House stance is AGAINST and S_PP,H >= 1. This also settles the game designer's recorded-versus-drawn division conflict: both give the same (a).
3. Thin cells. From game, modified: a cell below 12 is pooled with the SAME role-and-stance cell of the other chamber (HP with SP, HO with SO), never with the other role, because that would mix the directions of crossing. If the union is still below 12, the cell is UNFITTED and S3 stops. This replaces the precedent's K = 0, which let an unfitted cell count in the decision against H4. Inference's all-pooled fallback is rejected for the same reason.
4. S2. From game: P(no vetoable measure sent over the record) >= 1/2, exact. "Never" is read as "in the typical world", and it implies E[vetoable sent] < ln 2. The precedent's E[named] = 0 and the literal E[vetoable sent] = 0 are printed as cases of it. Inference's hit-rate form is rejected: it changes the stop's meaning and needed a question. S2 is PROVISIONAL as G5 is and goes to Elias in E4; it is not asked.
5. Intervals. From inference: Clopper-Pearson for cell proportions. From game and inference: a percentile bootstrap that resamples COUNTED MEASURES, which carries the scoring's thinness as well as the fit's, with B = 2000 (the precedent's count and ranks 50/1951). The keys are disjoint from the play keys. The precedent's roll-call cluster bootstrap is rejected.
6. Runtime parity. From game: a pure, unwired Assets/Scripts/Elections/UsVeto.cs is called by the instrument, so the backtest runs the game's arithmetic. US-21 adds SimulationManager.UsVetoGate and never edits PresidentialVetoGate. Draw-call counters prove budget, off-path and five-country inertness, and Applies is asserted over all six CountryId values.
7. Shared roll calls and the window. From inference: a roll call has one identity, a shared final gives one observation, and every final roll call is asserted on or after 2017-01-03.
8. Fit order. From game: ascending share, ties by roll-call identity, so a planted j = n-1 is the largest share for diagnostics.
9. Unpredictable set. From game and inference: any set other than {H.R. 131, H.R. 504 (119th)} is a data defect until the block is amended by a ruling. This is chosen over the precedent's automatic admission because H2 names two vetoes.
10. Role. The decision president (the row's president_party) sets V, the cells and the scoring, as in precedent and inference. The vote-day president is a reading.
11. Independents. As US-10 and the precedent have them: in seats by caucus, never in a split. The game designer's change to us_veto_prep.pl is rejected as unnecessary.
12. The instrument. A new UsVetoDrawCheck that writes US_VETO_DRAW.md, as the precedent proposed. US-10's tool, document and check stay byte-frozen. Inference's separate Perl tool is rejected.
13. Questions. One only: the draw's scope in play (the precedent's Q1). It binds US-21 and does not change the first run.
14. Readings. Pooled from all three. None of them stops the rule; inference's S4 is rejected because H1 ruled the option.

## The attacks and the reviser's rulings (38 attacks; accepted 31, in part 7, rejected 0)

### fidelity-F1 (high; THE DECISION - S2, NEVER SENDS)

**Attack.** R-US17's second STOP is worded "if a party-bloc Congress under Rule XXII never sends a vetoable bill". The plan also calls it "the US risk": the game's own Congress never sending one. S2 is computed over every counted measure of V, the EXPEDITED kinds included (CRA, War Powers, arms sale, national emergency, D.C.). Those pass the Senate by a majority with no cloture. The spec itself says "no game bill is expedited" and "Play's bills are all Rule XXII bills". s802 found that every expedited veto passed the Senate on defectors. So S2 can be held off by CRA-type measures alone while every Rule XXII measure still has P(sent) near zero. In that case (c) installs and the game, which has only Rule XXII bills, still never sends a vetoable bill. The STOP's own words, "under Rule XXII", are dropped without a declaration.

**Fix proposed.** Evaluate S2 over the Rule XXII counted measures of V only (kind not EXPEDITED): P(NONE | Rule XXII) and E[vetoable sent | Rule XXII]. Print the all-kinds figure beside it as printed-only. The verdict line names the Rule XXII figure as the one that decides. If no Rule XXII counted measure is in V, S2 fires with the case "no Rule XXII measure is vetoable". S1 stays pooled over all kinds: R-US17's precision test reads "on the US record".

**Ruling:** yes - R-US17's second STOP says "under Rule XXII", and play's ordinary bills are all Rule XXII bills. S2 now reads V_XXII only: the veto-path measures whose Senate procedure is RULE XXII, so expedited kinds and reconciliation are left out. The all-kinds figures are printed only. S1 stays pooled, because R-US17's precision test reads "on the US record". If V_XXII is empty, S2 fires with its own case.

### fidelity-F2 (high; THE DECISION - S2's threshold)

**Attack.** R-US17 says "never sends". The spec turns "never" into "P(NONE) >= 1/2", which no ruling says. It then labels this "PROVISIONAL as G5 is". G5 read only R-US17's FIRST stop ("far more" = pooled precision below B1's). It said nothing about the second, so G5's authority and PROVISIONAL status are borrowed for a threshold the panel invented. This threshold is the most consequential choice in the decision. It is silently stronger than the ruling's word, and the literal reading is pushed down to declared reading R13.

**Fix proposed.** Make the literal reading the default: S2 fires iff E[vetoable sent] = 0 exactly (equivalently P(NONE) = 1) over the Rule XXII measures of V, as in F1. Print P(NONE) >= 1/2 and E[named] < 1 as R13, named in the verdict line where its outcome differs. Strike "PROVISIONAL as G5 is" and "carried to Elias in E4" from S2. If the panel keeps 1/2 as the default, the sentence must say it is the panel's reading, and it becomes a question for Elias, since no ruling covers it.

**Ruling:** yes - No ruling gives 1/2. "Never sends" is now read literally: S2 fires iff E_XXII[vetoable sent] = 0 exactly, which is the same as P(NONE_XXII) = 1. P(NONE_XXII) >= 1/2 and E_XXII < 1 become R13 and are named in the verdict line where their outcome differs. "PROVISIONAL as G5" is struck: G5 read only the first STOP.

### fidelity-F3 (medium; HOW THE FIGURES REACH THE GAME - screen print)

**Attack.** H2: "every fitted figure prints its n and an interval beside it, labelled FITTED n=...". The spec lets a screen print "<p> FITTED n=<n>" with "The intervals stay in the document and the E4 reply". That removes H2's interval on every in-game fitted chance (ChamberVerdicts' P(reversed), P(reaches), P(override)). The departure is not declared.

**Fix proposed.** Have every screen that prints a fitted figure print its interval too: "<p> FITTED n=<n> 95% [<lo>, <hi>]", with the CP or BOOT interval carried in the catalog beside each cell figure and projection. Or declare it as a departure from H2 and put it to Elias. The faithful default is the interval on screen.

**Ruling:** yes - H2 covers every fitted figure, on any surface. The block now requires the full label, interval included, wherever a fitted figure is shown. The catalog carries what a surface needs to recompute the BOOT interval: the drawn lists, each observation's owner index, M and the bootstrap key.

### fidelity-F4 (medium; THE PRINT - SEED SPREAD vs A FITTED FIGURE)

**Attack.** The spec's own definition makes "the seeded realizations' statistics" a FITTED FIGURE. Yet they print as "<mean> DRAWN 1000 worlds [<26th>, <975th>]", with no "FITTED n=" and with the seed spread in place of the fit interval. By the spec's own terms, a fitted figure is printed without H2's label.

**Fix proposed.** Either remove the seeded realizations' statistics from the definition of a FITTED FIGURE and say why (they are a check on the exact figures, not new estimates), or print each one as "<mean> FITTED n=<n> m=<m> 95% [..] BOOT" with the DRAWN seed spread after it as a second bracket. Pick the first: the means are asserted equal to the exact figures, which already carry the label.

**Ruling:** yes - Taking the first option. The seeded realizations check the exact figures and are not estimates. They are removed from the FITTED definition and print as DRAWN, naming the FITTED figure each one checks.

### fidelity-F5 (medium; THE TERMS (COUNTED MEASURES, SEATS) / THE FIRST RUN items 1, 8, 10)

**Attack.** H2 says the two 2025 vetoes are "never counted for or against". The spec's own clause says "Every count, sum, denominator ... reads counted measures only". Several clauses contradict both: SEATS OF A CONGRESS is "computed over every row"; identity 10(2) is "computed over all 1,564 rows"; identity 10(3) and the companions print "US-10's, all 25 vetoes" (hit rate 22 of 25; 5 of 25). Those 25-based figures count H.R. 131 and H.R. 504 as misses, which is counting them against.

**Fix proposed.** Add an explicit exception list to the counted-only clause: the identities against US-10 alone may read all rows, and each is printed as "US-10's record (s802), not this rule's figure". Compute SEATS over counted rows and assert they equal US-10's %seatsOf; the two rows hold no roll call, so the assertion holds and carries the identity. Print the companions only on the counted base (V_COUNT), with a pointer to US_VETO_BACKTEST.md for the 25-based figures instead of reprinting them.

**Ruling:** yes - The counted-only clause gets one named exception: the US-10 identities. They read all rows only to verify the re-implementation and print as "US-10's record (s802), not this rule's figure", with no 25-based rate reprinted. SEATS are computed over counted rows and asserted equal to US-10's seats table. The companions print on the counted base only, with a pointer to US_VETO_BACKTEST.md.

### fidelity-F6 (medium; THE TERMS (KINDS, BUDGET VOTE) / PREMISES)

**Attack.** H1: "Budget votes ... stay as they are", and play never draws on a budget vote. The backtest, however, scores the record's budget measures with the draw ("the record's appropriations are scored as their kinds"; reconciliation titles "listed by name and scored as its kind is"). It also fits on their roll calls. So the instrument measures a rule play does not run. The listing is also lopsided: reconciliation is matched by title, while appropriations, continuing resolutions and the debt-limit acts are not listed at all.

**Fix proposed.** Define a RECORD BUDGET CLASS from the title with a fixed list: "Providing for reconciliation", "pursuant to section 2002 of the concurrent resolution", "Making appropriations", "Making continuing appropriations", "Making further continuing appropriations", "Consolidated Appropriations Act", "Further Consolidated Appropriations Act" and "Fiscal Responsibility Act". Print every match. By default these measures are outside V and draw nothing in the backtest, as in play (H1). Their roll calls stay in the fit, a premise that is stated. Scoring them as their kinds becomes a declared reading.

**Ruling:** part - Accepted: a RECORD BUDGET CLASS from fixed, printed title tests. Its measures draw nothing in the backtest, as in play. Reconciliation takes US-18's majority path, and the near-misses are printed so that misses show. Rejected: putting them outside V. US-22 says budget acts may be vetoed and (a) reads every act presented, so they stay in V, scored at K = 0. The critic's title list was replaced by display-title tests that fit the BILLSTATUS <title> field.

### fidelity-F7 (medium; QUESTIONS FOR ELIAS - Q1)

**Attack.** Q1 asks what H1 already says. H1's heading is "(c), on the USA's veto path only", it says "Parties stay blocs by default", and "Rule (a) then decides the veto on a bill that reaches the president against his party". The broad reading, drawing on contested bills the president backs, contradicts "stay blocs by default". The spec's own default quotes H1 as its reason. The question costs Elias a turn and leaves US-21 waiting on an answer. Its claim that "an answer needs no new fit" and "does not change the first run" is also unshown: the broad answer would put R2's cells (pooled over stances) on veto-path bills and move P(sent) on V.

**Fix proposed.** Drop Q1. State the narrow scope as settled by H1, and keep R2 printed as the broad reading's cells. The block then asks nothing. If Q1 is kept, it must state which cells veto-path bills read under the broad answer, and that an answer moving the decisive digest re-asks.

**Ruling:** yes - H1's heading ("on the USA's veto path only") and "parties stay blocs by default" settle the narrow scope. Q1 is dropped. R2 keeps printing the broad reading's cells.

### fidelity-F8 (medium; THE OVERRIDE - SIDES / THE MODEL item 6)

**Attack.** US-21 declares the override "voted by the same sides". Item 6 says the drawn crossers enter the recorded division only where they change its outcome; otherwise "the division is the blocs', byte for byte". THE OVERRIDE, however, applies "the passage's drawn crossers" in every case. Take a chamber where the OP holds a majority: passage carries at K = 0, so the plate shows a pure bloc division. The override in the same chamber then carries Kp PP crossers that the passage never showed (with the cap, up to half the PP, enough to reach two thirds). The override is then not voted by the sides the passage recorded. This departs from US-21's text, and the departure is not declared.

**Fix proposed.** Override sides = the passage's RECORDED division (crossers only where they entered under item 6). The override draw then "matters" under H1's test on its own threshold only through those recorded crossers. Applying the full draw to the override becomes a declared reading beside R15. If the panel keeps the full draw, it must declare in the block that this amends US-21's "same sides", and say so in US-21's text.

**Ruling:** yes - US-21's "voted by the same sides" is now read as the passage's RECORDED division, with crossers only where they entered under the margin test. That is consistent with item 6. Applying the full passage draw, and a fresh draw, are reading R15 (a) and (b).

### fidelity-F9 (medium; HOW THE FIGURES REACH THE GAME / THE VETO (budget acts) / THE BARS (US-21))

**Attack.** The R-US18 precedent keeps arithmetic, scoring and decision in the digest-bound block, and inputs and use in play in the item's Design, outside the markers. This block preregisters US-21's and US-22's design: ChamberVerdicts.Veto's USA projection, the screen label, "By default the plate shows the votes cast", the catalog layout, SimulationManager.UsVetoGate, US-21's diagnostic and done-when additions, and "(a) reads every act presented, budget acts included". Under the block's own amendment rule ("A change without a ruling fails"), every later US-21 or US-22 UI or wiring change would need a ruling from Elias. That is scope creep into items R-US17 does not own.

**Fix proposed.** Keep in the block only what decides or scores: the model, the fit, the margin test, (a), the override base, the print labels, seeds, the decision, the ruling record, and the catalog's FIGURES and digests. Move the projection, screen, plate, gate placement, US-21 diagnostic and bars, and the budget-act veto into US-21's and US-22's text (or a Design list outside the markers), as US-11's Design holds R-US18's use in play.

**Ruling:** part - Moved to US-21's text outside the markers: ChamberVerdicts' projection, the plate, the gate's name, US-21's 2024 run and its done-when. Kept in the block, because they decide or bind this rule: (a)'s scope over every act presented, since R-US17 is "which bills the president vetoes"; H2's print rule for any surface; the catalog's figures and digests; and the US-21 diagnostic's ruling-state, counter and planted-reproduction assertions. R-US18's block likewise holds US-16's diagnostic line.

### fidelity-F10 (low; THE DECISION - S3, TOO THIN)

**Attack.** R-US17 names two STOPs. S3 adds a third and cites H4. But H4 says an unfitted figure "prints UNFITTED and never counts in a decision". It did not make an unfit a stop of the whole rule: R-US18's unfitted (b') simply drops out of its decision. Grounding a new STOP in H4 reinterprets H4.

**Fix proposed.** Keep S3, but label it the panel's addition, not H4's: "S3 is not R-US17's; a decision cannot be computed on an unfitted cell (H4), so the rule re-asks". List it as a departure in the verdict text. Alternatively, follow H4's form: an UNFITTED cell draws K = 0 (bloc) and prints UNFITTED. The panel should pick one and declare it.

**Ruling:** yes - S3 is kept as a stop, labelled the panel's addition and listed as a departure. A decision cannot be computed on an unfitted cell (H4), so the rule re-asks rather than silently drawing K = 0.

### fidelity-F11 (low; THE TERMS (PRESIDENT) / WHEN THE DRAW MATTERS (HOUSE) / BUDGET VOTE / THE FIRST RUN (G1))

**Attack.** Four clauses cite authority that does not cover them. (1) "Suspension's two thirds is not modelled, as US-18 states": US-18 never mentions suspension. (2) "mirrors Poland's G5 items 14-15 (PROVISIONAL)": those are Polish readings from s778, not a US ruling. (3) "The run is on the defaults as printed (G1)": G1 answered R-US18's and R-US19's choices, and the authority to run R-US17 preregistered is H1/H3. (4) US-24's debt-limit act counted as a BUDGET VOTE: an undeclared default, since H1 says "budget votes" and US-24 sends the raise "through both chambers ... and the veto".

**Fix proposed.** (1) Declare suspension as this block's premise. (2) Declare the no-party president as the panel's default ("by analogy to G5 14-15"). (3) Cite H1 and H3 for the first run, keeping G1 only for "a choice changed after a result is in sight takes a new digest and is reported". (4) State the debt-limit act's class as a default with its reason (it rides reconciliation, 641(a)(3)), with the other class as a declared reading.

**Ruling:** yes - All four fixed. Suspension is this block's own premise. The no-party president is the panel's default by analogy to G5 items 14-15. The first run rests on H1 and H3, with G1 cited only for the report-a-change rule. The debt-limit act counts as a budget vote by default, with its reason, and R18 prints the other class.

### fidelity-F12 (low; THE RULING RECORD AND THE BARS - preregistering commit)

**Attack.** H3 requires the digest to be taken before the first run. The spec says "THE PREREGISTERING COMMIT (documents only, no instrument): ... the digest committed in RuleDigests". But RuleDigests is a list inside UsVetoDrawCheck.cs, the instrument, so a commit without the instrument cannot hold it. As written, the preregistration H3 asks for cannot be proved from history.

**Fix proposed.** The preregistering commit records the digest in COMPLETED.md sNNN and in the plan's "block's digests" line (R-US18's form). The first-run commit creates RuleDigests with that digest as its first entry. The instrument asserts that the first entry equals the digest of the block as it stands at the preregistering commit's blob, which must predate the commit adding UsVetoDrawCheck.cs.

**Ruling:** part - Accepted: the preregistering commit records the digest in COMPLETED.md and in the plan's "The block's digests" line, as R-US18 did. RuleDigests is created in the first-run commit. Rejected leg: the instrument reading git history. The ordering proof goes in the first-run commit's review row instead, so the bars never depend on the history being present.

### fidelity-F13 (low; THE RULING RECORD / HOW THE FIGURES REACH THE GAME (US-21 diagnostic))

**Attack.** E4 and US-10's done-when require "the rates go into the reply to Elias before anything goes live". The first run registers INSTALLED ("the reply that carries the rates") before any reply exists. US-21's diagnostic accepts INSTALLED alone, so US-21 could wire the draw without the E4 reply ever being recorded.

**Fix proposed.** Split the state: INSTALLED-PENDING-E4 at the first run, and INSTALLED (sNNN of the reply) only once a COMPLETED.md section records the reply that carried the rates. US-21's diagnostic accepts only INSTALLED with that section, or RULED.

**Ruling:** yes - A first-run INSTALL now registers INSTALLED-PENDING-E4. It becomes INSTALLED (sMMM) only once a COMPLETED.md section records the reply that carried the rates. US-21's diagnostic accepts only INSTALLED with that section, or RULED.

### computability-A1 (high; THE TERMS - SEATS OF A CONGRESS)

**Attack.** The prose ('the latest final roll call by <ch>_final_date ... a later-or-equal date replaces the earlier one') is NOT what us_veto_backtest.pl lines 182-190 do. Line 190 sets $seatsOf{$k}{date} unconditionally, even when line 189 did not replace the entry. A row with an EARLIER date therefore overwrites the stored date of the kept entry. The next row with any date at or after that earlier date then replaces the seats with an older roll call's. For the Senate this matters, because rows are visited in house_final_date order and not in senate_final_date order. One implementer codes 'latest by senate date', another copies the code, and the two can disagree on S_R, S_D and sworn for a Senate key. Those feed every (B) Senate test, K and the K=0 identity. The sort is also unspecified: Perl's mergesort is stable with codepoint cmp, but C# List.Sort is unstable and string.Compare is culture-aware. Rows with equal or empty house_final_date can come out in a different order.

**Fix proposed.** Spell out the algorithm literally. Order the rows by house_final_date with ordinal comparison, stable, ties in CSV row order (OrderBy with StringComparer.Ordinal). For each row and chamber with a roll call, replace the stored seats iff there is none yet or the row's <ch>_final_date >= the stored date. Then set the stored date to the row's <ch>_final_date in every case (the code's overwrite, kept on purpose). Then assert that each Congress's R, D and sworn equal the seats table US_VETO_BACKTEST.md already prints (the brief's 235-197 ... 53-47). A difference is a DEFECT.

**Ruling:** yes - Confirmed in us_veto_backtest.pl: the line setting $seatsOf{$k}{date} runs unconditionally. The algorithm is now written out literally: a stable ordinal sort with ties in CSV order, replace iff the date is later or equal, then overwrite the date in every case. The result is asserted equal to US_VETO_BACKTEST.md's seats table, and any difference is a DEFECT.

### computability-A2 (high; THE FIT - THIN CELLS / N_MIN; THE CELLS)

**Attack.** 'n of a cell is its number of roll calls' and 'a cell with n < 12'. A fitted roll call can give NO observation: an OP cell skips a roll call where the OP also stands NAY, and any observation with m = 0 is excluded and listed. So 'roll calls in the cell' (fitted roll calls of that chamber) and |L| (observations kept) can differ. Item 3 also draws j = floor(u*n) with n = |L|. One implementer tests N_MIN on fitted roll calls and another on |L|. Pooling and S3 (a decisive outcome) can come out differently.

**Fix proposed.** Define n of a cell as |L|, the number of observations it keeps after the OP-NAY and m=0 exclusions, and use that one n everywhere: the N_MIN test, the union's size, the draw index, the CP x of n and the label. Print the fitted roll calls with no observation separately, as now.

**Ruling:** yes - n of a cell is now |L|, the observations kept after the OP-NAY and m = 0 exclusions. That one n is used for N_MIN, the union, the draw index, CP and the label.

### computability-A3 (high; THE PRINT - INTERVALS (BOOT) / per-measure figures)

**Attack.** A replicate's 'scoring reads the picked measures'. A given measure j is absent from a replicate with probability about (1-1/M)^M, roughly 0.368, so in about 736 of 2000 replicates. If an unpicked j has no P_H(j), P_S(j), P(sent_j), P(named_j) or omega_j, every per-measure BOOT interval is UNDEFINED in far more than 50 replicates and prints 'UNDEFINED IN ~736 OF 2000'. If instead it is scored from the replicate's cells, that is a different estimand. The spec says neither, and two implementers will differ.

**Fix proposed.** State that in every replicate each measure j of the FULL V is scored from that replicate's cells, LOO (every copy of j's roll calls removed), whether or not j was picked. Totals (E[...], P(NONE), V_COUNT) are taken over the replicate's picks with multiplicity. The replicate's V_COUNT is its picked counted vetoes with multiplicity, and that is the hit-rate denominator.

**Ruling:** yes - In every replicate, each measure of the full V is scored from that replicate's cells, LOO, whether or not it was picked. Totals run over the picks with multiplicity, and the replicate's V_COUNT counts its picked counted vetoes.

### computability-A4 (high; WHEN THE DRAW MATTERS - SENATE EXPEDITED, THE VICE PRESIDENT; THE FIRST RUN item 10 (A) identity)

**Attack.** The VP 'breaks a tie with the PP's stance'. Under (A), and in the (A) K=0 identity 'computed over all 1,564 rows', the PP's Senate stance is often YEA: off V entirely, or on a V measure the PP opposed only in the House. The 117th Senate was 50-50, so expedited ties occur, and the VP breaks them YEA. US-10's bloc_passes never breaks a tie ('A tie is not broken'). The spec's claim 'this equals US-10, so the identity holds' is true only on V under (B). The identity '(A) reproduces US-10's chamber-by-chamber counts' (allSend, 13 vetoable, 5 of 25) can then fail as a DEFECT purely by construction.

**Fix proposed.** Choose one. (i) The VP casts the tie vote only under (B) and in play; under (A), and in every K=0 identity against US-10, a tie fails as in US-10. Or (ii) keep the VP under (A) as well, compute the identity check with the tiebreak switched off, and print the measures the VP moves under R1. Either way, state which (A) outcome is the identity and which is the reading.

**Ruling:** yes - Taking option (i). The Vice President votes only under (B), under R1 and in play. Every identity against US-10 runs in US-10 MODE, where a tie fails and kinds are read as US-10 reads them.

### computability-A5 (medium; THE FIRST RUN item 10 - (B) identity at K=0)

**Attack.** 'at every K = 0, (B) sends exactly US-10's one-stance count' compares sets that differ in two ways. First, US-10 counts over F3's 34 (nay+present > yea) and the spec counts over V (bloc NAY, which includes yea = nay+present). Second, US-10 always assumes the OP carries its whole seat count (2*h{other} > sworn), while the spec's (B) uses the OP's recorded bloc stance (Y = Kp when the OP stands NAY). Both counts happen to be 0 on today's record, but the identity is not defined in a way that survives a grown extract. A count-only check also hides offsetting differences.

**Fix proposed.** Make the identity per measure on V ∩ F3: at K = 0 with the OP's stance forced to YEA, the (B) send verdict equals US-10's one-stance verdict for each measure. Separately print the measures in V \ F3 and F3 \ V, and the measures where the OP's recorded stance is NAY, with their K=0 verdicts. A per-measure mismatch on V ∩ F3 is a DEFECT.

**Ruling:** yes - The identity is now per measure on F3 (F3 is a subset of V): at K = 0, with the OP forced YEA on full seats and in US-10 MODE. V minus F3, and the measures where the OP's recorded stance is NAY, are printed with their verdicts.

### computability-A6 (medium; HOW THE FIGURES REACH THE GAME - THE CATALOG)

**Attack.** The catalog stores each cell's own observations (Defectors, Voting), and the roll call's identity only as a COMMENT. A pooled cell draws from the union 'sorted in FIT ORDER, ties by IDENTITY'. Equal shares from different (d,m), such as 1/10 against 2/20, are ordered only by identity. A runtime that merges HP and SP at load time cannot break those ties, so index j maps to a different (d,m) and a different K than the instrument used. The planted-index reproduction in US-21's diagnostic would then fail, or worse, pass on the pairs it happens to test.

**Fix proposed.** Have the catalog emit, for every cell, the exact list it draws from, already merged for a pooled cell and in fit order, with the identity as a string FIELD and not a comment. The runtime indexes that array and never sorts or merges. LOO and the bootstrap stay instrument-side.

**Ruling:** yes - The catalog emits each cell's exact drawn list, already merged for a pooled cell and in fit order. Identity, owner and congress are string fields, not comments. The runtime never sorts or merges.

### computability-A7 (medium; SEEDS AND DETERMINISM - THE PLAY KEY; THE MODEL item 5)

**Attack.** The key holds 'that chamber's vote day', but item 5 says one draw serves cloture, passage and override in that chamber. If a chamber's cloture and passage, or its first passage and a later concurrence, fall on different days, the key dates differ and the draws differ. That breaks 'one draw serves every vote', and which day keys the draw is undefined. The seeded realization has the same gap: 'the chamber's final date' is used, while cloture is implicitly on the same draw.

**Fix proposed.** Key the draw on ONE defined date: the date of the chamber's first division on that bill, recorded on the bill so later votes read it, or its final passage date if US-18 guarantees one-day resolution. Assert in US-21's diagnostic that every vote of a chamber on a bill reads the same key. For the record realization keep '<ch>_final_date, else decision_date', and say that it serves all of that chamber's votes.

**Ruling:** yes - The play key's date is the chamber's first division on that bill. It is carried on the bill for every later vote of that chamber, and US-21's diagnostic asserts that one key per chamber per bill. The record realization keeps <ch>_final_date, else decision_date.

### computability-A8 (medium; THE OVERRIDE - BASE / SIDES)

**Attack.** 'A chamber overrides iff Y >= 2*N' does not say what N is in each premise. Under (B), are the House independents in N, as they are in W_H for passage? Under (A), N could be only the R and D blocs, House independents outside, or all labels. A chamber with no roll call under (A) has no Y or N at all, since it 'passes undrawn, unanimous'. 'The originating chamber votes first' needs the origin, which the CSV does not hold as a field. omega_j, P(override | named) and the expected overrides cannot be computed the same way twice.

**Fix proposed.** Define it. (B): N = W - Y for each chamber, with W_H counting House independents against and W_S including the caucus. (A): N = the bloc nays of the roll call's R and D blocs plus the Senate caucus entries; House independents are outside both sides. (A) with no roll call: use the SEATS OF A CONGRESS with the passage's draw, as under (B). Origin: the House if the measure starts with 'H.' and the Senate if it starts with 'S.'; anything else is a DEFECT.

**Ruling:** yes - Adapted to F8: the override's Y and N are defined per premise from the recorded division. (B) counts on W with House independents against. (A) counts the R and D blocs, plus the Senate caucus, with House independents outside. (A) with no roll call uses SEATS with the veto path's stances. The origin is read from the measure prefix ("H." or "S."), and anything else is a DEFECT.

### computability-A9 (medium; WHEN THE DRAW MATTERS - MARGIN; UNCONTESTED)

**Attack.** 'The least D that reverses the bloc outcome' is ambiguous. D = Kp - Ko is signed: a bloc pass is reversed by negative D and a bloc fail by positive D. 'Least' could mean the most negative value or the smallest |D|. Nothing is said for the case where no feasible D reverses, which happens whenever the caps floor((S-1)/2) cannot reach the threshold. Separately, UNCONTESTED says 'both blocs on one side: no draw', while (B) item 9 and the House (B) formula ('with the OP standing NAY, Y = Kp') draw for the PP when both stand NAY. The outcome is the same (Kp < S_PP/2 can never pass), but the draw counter and the 'cells read' print differ.

**Fix proposed.** MARGIN = the smallest |D|, with the sign that reverses the K=0 outcome, among D in [-cap_OP, cap_PP], where cap = floor((S-1)/2) or 0 with no draw. Print 'unreachable' when none exists. On V under (B) the PP always draws, both-NAY included, and UNCONTESTED applies only off V and under (A)/play: say so.

**Ruling:** yes - MARGIN is now the smallest |D| with the reversing sign over the feasible range, or "unreachable". On V under (B) the PP always draws, both-NAY included. UNCONTESTED applies only off V, under (A) and in play.

### computability-A10 (medium; THE PRINT - labels, m, zero denominators)

**Attack.** m is defined only for sums over V and the hit rate. A per-measure probability's BOOT label needs m= but has none. PRECISION's m is undefined (its denominator is E[named], not a count of measures). A per-president ratio with E[named] = 0 but m > 0 (e.g. Trump's second term, if V holds non-vetoed measures that are never sent) is a division by zero. 'UNFITTED m=0' does not cover it, and V_COUNT = 0 has its own wording only for the hit rate.

**Fix proposed.** Set m = 1 for a per-measure figure and m = |V| for precision and P(NONE). Make any ratio whose exact denominator is 0 print 'UNDEFINED (denominator 0)' with no interval, and keep it out of S1, which is already gated on pooled E[named] > 0.

**Ruling:** yes - m is now defined for every derived figure: 1 for a per-measure figure, |V| for precision and the pooled sums, |V_XXII| for the S2 figures, and V_COUNT for the hit rate. A zero exact denominator prints "UNDEFINED (denominator 0)" with no interval.

### computability-A11 (low; THE PRINT - BOOT UNDEFINED rule)

**Attack.** Off-by-one. With UNDEFINED replicates sorted below every value, lo is the 50th sorted value. When exactly k = 50 are UNDEFINED, lo IS an UNDEFINED value, but the rule switches to 'UNDEFINED IN k OF 2000' only when k exceeds 50. A symmetric rule for hi is also needed if UNDEFINED could reach position 1951 (k >= 1951).

**Fix proposed.** Print 'UNDEFINED IN <k> OF 2000' when k >= 50. Otherwise lo = sorted[49] and hi = sorted[1950] (0-based) over all 2000, with the UNDEFINED values at the front.

**Ruling:** yes - Off by one, fixed: "UNDEFINED IN <k> OF 2000" prints when k >= 50. Otherwise lo = sorted[49] and hi = sorted[1950] (0-based), with the UNDEFINED values at the front.

### computability-A12 (low; SEEDS AND DETERMINISM - empty lists in the exact enumeration)

**Attack.** 'P(pass) = count / (n_PP * n_OP)' divides by zero when a LOO list or a bootstrap replicate list is empty. Item 3 says an empty list gives K = 0, but that is not carried into the enumeration, and the histogram implementation could diverge from it.

**Fix proposed.** State that an empty list counts as the single index 'K = 0' (n := 1) in the enumeration and in Crossers alike, and that it is printed and counted per replicate.

**Ruling:** yes - An empty list is the single index K = 0 (n := 1) in the enumeration, in Crossers and in the realizations alike. It is counted and printed.

### computability-A13 (low; SEEDS AND DETERMINISM - realization assertion; BOOT arithmetic; printing)

**Attack.** The DEFECT test |mean - exact| <= 4*sd/sqrt(1000) leaves sd unspecified: population (1/N) or sample (1/(N-1)). At the boundary the two can disagree on a deterministic run. The BOOT figures are 'recomputed in IEEE double' with no summation order given, and printing 'to one decimal' gives no rounding rule for a rational or a double at x.x5. Two implementers can print different digits.

**Fix proposed.** sd = sample sd with 1/(N-1), in double, summed in seed order 1..1000. BOOT sums run in pick order t = 0..M-1. Exact rationals print by rounding half up on the exact rational, and doubles via decimal conversion of the exact double with half up. Invariant culture.

**Ruling:** yes - Fixed: the sample sd (1/(N-1)) summed in seed order, BOOT sums in pick order, and half-up rounding on the exact rational, or on the exact decimal value of a double, in invariant culture.

### computability-A14 (low; THE RULING RECORD - THE DECISIVE DIGEST)

**Attack.** 'key=value' lines with no key names and no rational format (e.g. '3/4' or '0.75'; '0' or '0/1'; how negative zero or a sign is written). The registered digest depends on choices the block leaves open. A re-implementation, for example after a refactor, would register a 'moved digest' with nothing in fact moved.

**Fix proposed.** Fix the grammar in the block: keys cell.<XX>.status|pooled|n|obs.<i>, unpredictable.<i>, v_count, counted, p_sent.<congress>|<measure>, p_named.<...>, e_sent, e_named, e_hits, p_none, outcome. Rationals are written 'p/q' in lowest terms with q > 0 and zero as '0/1'. Observations are written '<identity>|<congress>|<measure>|<d>|<m>'.

**Ruling:** yes - The decisive digest's grammar is now fixed: every key, the rational form p/q in lowest terms with zero as 0/1, and the observation format. SourceDigest is included (T3).

### computability-A15 (low; THE TERMS - KINDS; INDEPENDENTS)

**Attack.** The EXPEDITED kinds are given as legal descriptions, not as the kind column's exact strings ('CRA disapproval', 'arms-sale disapproval', 'War Powers', 'national emergency', 'D.C. disapproval' in %EXPEDITED). The reconciliation title test does not say whether it is case-sensitive. The model's S is 'bloc seats', which in the Senate includes caucusing independents, so K is drawn over a base that contains members said to be 'in no split, stance or observation'. That is allowed, but it is not stated, and under (A) the Senate S (roll-call members plus the caucus entries) is not stated either.

**Fix proposed.** List the five exact kind strings, compared ordinal and case-sensitive, and treat any unknown kind as Rule XXII and print it. Make the title tests ordinal and case-sensitive on the CSV title field. State that S in the Senate = the label's members plus its caucus entries, under (A) from the roll call and under (B) from the SEATS OF A CONGRESS, and that a crosser can be any of them.

**Ruling:** yes - The five exact kind strings, as kind_of writes them, are compared ordinal and case-sensitive, and an unknown kind is Rule XXII and printed. The title tests are fixed. Senate S is the label's members plus its caucus entries, and a crosser may be any of them.

### tunability-T1 (high; THE DECISION - S2)

**Attack.** S2 pools every kind, but R-US17's own STOP is "a party-bloc Congress UNDER RULE XXII never sends a vetoable bill", and play's bills are all Rule XXII bills. The EXPEDITED disapprovals pass the Senate by a simple majority, and the brief records that every one of them that was vetoed passed on defectors. Those measures alone can push the pooled P(NONE) below 1/2 even if no Rule XXII measure is ever sent. The rule would then INSTALL a draw that in play reproduces exactly the US risk the STOP was written to catch. The pooled S2 does not test the condition that was ruled.

**Fix proposed.** Define V_XXII as the measures of V whose kind is under RULE XXII (the listed reconciliation titles stay as their kind). S2 fires iff P(NONE_XXII) = product over V_XXII of (1 - P(sent_j)) >= 1/2, exact and LOO. If V_XXII is empty, S2 prints "UNFITTED m=0" and fires, because the Rule XXII claim is then untested. Print the pooled P(NONE) and the EXPEDITED-only P(NONE) beside it, never deciding. The verdict line names the Rule XXII figure.

**Ruling:** yes - Merged with F1: S2 reads V_XXII, an empty V_XXII fires S2, and the pooled and expedited-only figures are printed beside it.

### tunability-T2 (high; THE FIT - SELECTION ON PASSAGE; READING R6)

**Attack.** The Senate cell is selected on success by construction, and R6 cannot undo it. The brief's seat facts show the PP held at least 50 Senate seats in every Congress of 115th-119th, with a Vice President of its own party in the 117th. So no Senate roll call with the PP standing NAY passes at K = 0. That makes every SP observation a passed vote whose PP crossing (net of OP crossing) was enough to carry it: the empirical distribution has no mass below the threshold it is later used to clear. LOO removes only j's own roll calls, and sibling disapprovals carry the same selected shares. So under the default, S2 is an upper bound on sending and close to unable to fire. R6 keeps only roll calls whose bloc result already passed at K = 0, so its SP cell is EMPTY and gets pooled with House observations. The declared selection reading therefore never touches the chamber that decides sending.

**Fix proposed.** State in the block, as a premise, that SP is selection-complete: the record holds no Senate roll call with the PP NAY that failed, so Senate sending under the default is an upper bound and P(NONE) a lower bound. The S2 verdict then reads "does not fire on an upper bound of sending". Rename R6 to what it is: "R6, House selection only (SP has no selection-free observation on disk)", and print SP's emptiness under it. Add "source cloture and failed Senate votes (BILLED)" to S2's options as well as S3's, so Elias sees that the bias can only be fixed by new data.

**Ruling:** part - Accepted: selection on passage is a declared premise, biased toward sending. R6 prints which cells empty under it and says "no selection-free observation on disk". Sourcing the Senate's cloture and failed votes (BILLED) is added to S2's options. Rejected: asserting in the block, from the brief's seat facts, that SP is selection-complete. The instrument computes and prints that as a record count, x of n per chamber, which is not a block premise derived by hand.

### tunability-T3 (high; THE TERMS - THE RECORD; THE RULING RECORD)

**Attack.** The preregistration digests the rule text but pins neither the CSV's SourceDigest nor Tools/us_veto_prep.pl. The prep tool's choices (which agreement is FINAL, the superseded-vote guards) decide directly which roll calls and splits enter the cells. The spec even expects the prep tool to change ("re-run whenever Tools/us_veto_prep.pl changes"). After the figures are seen, a prep "fix" or a re-fetch of the ongoing 119th moves the cells. That arrives as a DEFECT fix or a "grown extract", is never put to Elias as a choice, and stays outside G1's report-the-change rule.

**Fix proposed.** Put the CSV's SHA-256 and the prep tool's git blob id in the block, taken in the preregistering commit (hashing bytes computes no figure). The first run must read that exact SourceDigest. Any other value is a DEFECT until an amendment naming Elias's ruling re-pins it. Every later SourceDigest change is reported to Elias with the old and new decisive digests, whether or not the outcome moved.

**Ruling:** yes - The CSV's SHA-256 and Tools/us_veto_prep.pl's git blob id are pinned in the block at the preregistering commit. Hashing bytes computes no figure. A mismatch is a DEFECT until an amendment naming Elias's ruling re-pins them, and every later source change is reported with the old and new decisive digests.

### tunability-T4 (high; THE RULING RECORD - outcome changes)

**Attack.** The ratchet is asymmetric and favours INSTALL. A run that moves from INSTALL to a stop goes back to ASKED. A run that moves from ASKED to INSTALL is just "registered INSTALLED and reported", with no ruling. Combined with DEFECT fixes to UsVeto.cs ("never put to Elias") and grown extracts, any sequence of post-result code or data changes that happens to land on INSTALL makes itself binding. The block digest covers the text, not the implementation, so a fix made after the figures are seen is exactly the tuning G1 forbids.

**Fix proposed.** After the first run, any change of outcome in either direction sets the record to ASKED with both digests and both outcomes. Only Elias's ruling moves it to INSTALLED or RULED. Any commit that changes UsVeto.cs or UsVetoDrawCheck.cs and moves the decisive digest must list the old digest, the new digest and the per-measure P(sent) differences in the next reply to Elias (E4), even when the outcome is unchanged.

**Ruling:** yes - Any change of outcome, in either direction, now registers ASKED. Only Elias's ruling moves the record to RULED. A moved digest with the same outcome needs a re-pin commit naming its cause, and per-measure P(sent) differences in the next reply to Elias (E4, G1).

### tunability-T5 (medium; THE TERMS - N_MIN; THE FIT - ESTIMATOR)

**Attack.** N_MIN's reason does not fit the estimator. A Wilson width at p = 1/2 bounds a share, but the outcome depends on the upper tail of the empirical distribution: the one or two roll calls whose shares clear cloture or the majority. At n = 12 each observation carries 1/12 of the mass, so one roll call's presence can move P(pass) for a whole class of measures by 1/12 or more. The decision can turn on a single sibling vote while being described as "fitted". This rewards overfitting a handful of disapproval roll calls.

**Fix proposed.** Drop the Wilson justification. State N_MIN = 12 as a preregistered floor with no data-based reason (R14 still prints 20). Add a printed INFLUENCE section: the outcome recomputed exactly with each fitted roll call removed in turn (its observations taken out of every cell; pooling held fixed). List every roll call whose removal changes the outcome, and print "FRAGILE: <k> roll calls each flip the outcome" in the verdict line. Also print, per chamber and procedure, the CP interval of the decisive tail share (observations whose K reaches the margin, as x of n).

**Ruling:** part - Accepted: N_MIN is stated as a preregistered floor, its Wilson arithmetic kept only as a data-free note. A printed INFLUENCE section and a FRAGILE clause in the verdict line are added. Rejected: the decisive-tail CP interval. INFLUENCE shows the same dependence directly, and a second interval form adds nothing to the decision.

### tunability-T6 (medium; THE PRINT - BOOT)

**Attack.** The bootstrap labels record-composition noise as fit uncertainty. Replicates resample all M counted measures (about 1,560), so the scored set V, its size and its vetoes change from replicate to replicate. The intervals on E[named], E[hits] and the precision then mostly show how many V measures and vetoes a replicate happened to pick, not how uncertain the cells are. Yet they print as "FITTED ... BOOT", next to H2's FITTED n label. Readers will take the width as the fit's.

**Fix proposed.** Make the default BOOT a cell bootstrap. Hold the scoring set fixed (the real V and VETO_j). Resample the fitted roll calls clustered by owner measure, with the same keyed picks over the owners that hold observations. LOO still removes every copy of j's roll calls. Print the full measure resampling as a declared reading ("record resampling"), labelled as such.

**Ruling:** part - Totals keep record resampling, because H2 names the thinness in measures (13 vetoable, 25 vetoes), so composition noise is part of what Elias asked to see. Per-measure figures already vary only with the cells (A3). The cell bootstrap with a fixed scoring set is printed as R17.

### tunability-T7 (medium; THE PRINT - BOOT UNDEFINED)

**Attack.** Off by one: UNDEFINED replicates sort below every defined value and lo is the 50th sorted value, so at k = 50 lo is itself UNDEFINED. The spec switches to "UNDEFINED IN <k> OF 2000" only when k > 50. At k = 50 the lower bound is either mis-printed or silently replaced. For 1 <= k < 50, the defined lower bound quietly drops exactly the never-sends worlds S2 is about.

**Fix proposed.** Print "UNDEFINED IN <k> OF 2000" when k >= 50. For 1 <= k < 50, append "(<k> of 2000 replicates undefined: no measure sent)" to the interval.

**Ruling:** yes - Merged with A11. For 1 <= k < 50, the note naming the undefined replicates is appended to the interval.

### tunability-T8 (medium; THE DECISION - S1; THE VETO - IDENTITY)

**Attack.** Under the asserted identity, every sent measure of V is named and nothing off V is ever named. So precision = sum VETO_j P(sent_j) / sum P(sent_j) over V: a sending-weighted veto share of a set V that is fixed before any draw. The false-positive pool is just V's non-vetoed measures, and vetoes outside V are free misses. S1 therefore cannot register a rule that "names far more bills than the record vetoed". A draw that sends one vetoed measure in eight years scores 100 % and passes S1. S2's second case ("sends some but names none") cannot occur under (B) and is dead text in the verdict.

**Fix proposed.** Keep S1 as G5 reads it, but print beside the precision the draw-free share of V (vetoed counted measures of V, as x of |V|, NOT FITTED), so Elias sees how much of the precision is V's composition rather than the draw. Also print E[named] beside V_COUNT as a count comparison. Delete S2's "names none" case, or assert that it cannot arise under (B).

**Ruling:** yes - The draw-free share of V (x of |V|, not fitted) is printed beside the precision, and E[named] beside V_COUNT. E[named] = E[vetoable sent] under (B) is asserted. S2's "names none" case is deleted.

### tunability-T9 (medium; THE DECISION - S2 threshold)

**Attack.** P(NONE) >= 1/2 is given no reason. It fires only when E[vetoable sent] is below about 0.69 over five Congresses, while the record sent every measure of V (it holds passed measures only). A draw that sends about 1 of roughly 34 measures the record actually sent clears S2 and installs. Nothing in the decision checks how well sending is calibrated against the record.

**Fix proposed.** Print the sending calibration, E[vetoable sent] FITTED beside |V| ("all sent on the record", not fitted), pooled and for V_XXII. Give the 1/2 a stated, data-free reason in the block ("more likely than not, the five-Congress window sends no vetoable measure"), or replace it with E[vetoable sent] < 1, keeping the other as R13. The threshold must be fixed in the preregistering commit either way.

**Ruling:** part - Accepted: the sending calibration, E[vetoable sent] beside |V| and E_XXII beside |V_XXII| ("all sent on the record"), pooled and per president. The threshold follows F2's literal reading. Both the 1/2 threshold and E < 1 are R13 and are named in the verdict line where they differ, so Elias sees them under E4 without a new question.

### tunability-T10 (low; THE MODEL item 5; THE FIT - ESTIMATOR reasons)

**Attack.** The estimator's stated reason ("it never draws a share the record never showed") contradicts the default's independent PP and OP draws. Independence produces (Kp, Ko) pairs no roll call showed, such as a high PP crossing together with a high OP crossing on the same vote. Since D = Kp - Ko decides passage, that pairing changes the tail. The nonparametric MLE of the pair is R7's joint draw, so the default is justified by an argument that supports the reading instead.

**Fix proposed.** Either make R7, the joint per-roll-call draw, the default (Ko = 0 where a roll call has no OP observation; play draws one index per chamber from the PP cell), or keep independence and restrict the NPMLE reason to the marginals, stating independence as a premise with R7 printed beside it.

**Ruling:** yes - Taking the second option. Independence is kept as a stated premise: each cell keeps its own n and thin-cell rule, and HO and SO lack the OP-NAY roll calls. The NPMLE reason is restricted to the marginals, and R7, the joint draw, is printed beside it.

## Appendix A. The brief (`facts.md`, written by the session)

### Brief for the R-US17 rule (c) design panel (PoliSim, s832) - written by the session, 2026-10-16

#### The task

Specify **R-US17's rule (c)**, as Elias ruled it on 2026-10-13 (H1-H3, below), as ONE preregistered rule for
`docs/specs/USA_STAGE_PLAN.md`: the text a later instrument will implement and be held to by a SHA-256 digest taken
BEFORE that instrument's first run. You write the specification; the session turns it into the fenced block. Nothing
is built in this step and nothing is measured.

Repository: `G:\UNITY\Projects\PoliSim` (Unity C#; tools in Perl; Git Bash on Windows). The out-of-tree data root is
`G:\UNITY\Projects\PoliSim-captures\`.

#### BLINDNESS - binding on every agent of the panel

The session that wrote this brief has computed NO figure of the new rule, and neither may you. A rule written after
its figures are in sight can be tuned to them.
- NEVER compute any defection count, defection rate, fitted parameter, interval, or any result of any candidate
  model on the record - not from `ElectionsData/usa/us_veto_measures.csv`, not from the roll-call XML under
  `PoliSim-captures/sources/usa_us10/rolls/`, not by hand, not "roughly", not for one measure "as an example".
- NEVER open `ElectionsData/usa/us_veto_measures.csv` past its first three lines (two comment lines and the header,
  copied below). NEVER open the roll-call XML files. NEVER run `Tools/us_veto_backtest.pl`, `Tools/us_veto_prep.pl`,
  `Tools/us_veto_fetch.pl` or any Unity process.
- NEVER read any session scratch folder (`G:\Claude\tmp\...`).
- Edit NOTHING anywhere. You are read-only.
- You MAY read: the plan, `COMPLETED.md` records, `docs/generated/US_VETO_BACKTEST.md` and
  `docs/generated/VETO_B1_BACKTEST.md` (they print results of OTHER rules, measured at s802 and s757 - already the
  record; do not derive defection figures from them), `ElectionsData/usa/veto_record.md`, all C# under `Assets/`,
  the Perl tools' SOURCE (reading code is fine; running it is not).

#### The rulings, verbatim as recorded in the plan (lines ~716-728)

R-US17 as asked: "Which bills the president vetoes, and the override's base (US-10's document; blocks US-21).
(a) B1 transposed: a bill a majority of the president's party's House members voted against; (b) a bill the
president's party opposed in either chamber; (c) a rule fitted on the record. The override is two thirds of those
present and voting, a quorum present, as the interpretive source reads it. Code will recommend on the confusion
matrix - (a) only if its precision on the US record is at least B1's on Poland's - and STOPS and re-asks if every
rule names far more bills than the record vetoed, or if a party-bloc Congress under Rule XXII never sends a vetoable
bill. That last is the US risk, the mirror of E4's 43 of 43."

STOPPED AND RE-ASKED at s802: under the plan's premise - each party voting as one, one stance in both chambers
(spec-risk 4) - a party-bloc Congress never sends a vetoable measure (0 of the 34 the president's party opposed in a
chamber; the other party never held both chambers in the 115th-119th Congresses). Read chamber by chamber as the
members voted there, a bloc Congress sends 13 vetoable measures (Trump first term 9, Biden 4) and 5 of the 25 vetoed.
Every CRA, War Powers, arms-sale, national-emergency and D.C. disapproval vetoed passed the Senate on defectors.
Trump's two second-term vetoes passed both chambers without a roll call.

RULED (Elias, H1-H3, 2026-10-13):
- **H1 - the third option, (c), on the USA's veto path only.** Parties stay blocs by default. A seeded draw of
  defectors per party per chamber is added, fitted on the recorded chamber-by-chamber votes since 2017; the draw
  matters only where the chamber's margin is within the drawn count. Rule (a) then decides the veto on a bill that
  reaches the president against his party. Budget votes, the other five countries and Poland's B1 stay as they are,
  and Poland is proved unchanged on the simulation bar.
- **H2 - the fit is thin** (13 vetoable measures, 25 vetoes): every fitted figure prints its n and an interval beside
  it, labelled `FITTED n=...`. Trump's two 2025 vetoes, with no recorded vote, stay declared unpredictable and are
  never counted for or against.
- **H3 - preregistered:** the new rule is preregistered and its digest taken before its first run; the veto tool's
  mutation suite is re-run, plus one case for the superseded-vote guard. (The second sentence is DONE - s831. This
  panel serves the first.)

Elias's companion rulings to know: G1 (2026-10-06) "run on the defaults as printed" - and a choice changed after a
result is seen is reported to him; G5 - "far more" read, before the first run, as a pooled precision below B1's on
Poland (45 of 166, 27.1 %), PROVISIONAL; E4 - the rates go into the reply to Elias before anything goes live. H4
(R-US18): an unfitted figure prints UNFITTED and never counts in a decision. Elias wants work fast but right: keep
questions to him to the ones the rulings genuinely cannot settle - everything else gets a DEFAULT and, where
contestable, a declared alternative reading printed beside it.

#### What the rule must settle (the panel decides how; nothing here is a default)

1. What exactly is drawn: a count of defectors per party per chamber per bill, a per-member probability, or other;
   from which distribution; with which parameters; per president, per Congress, per party, per chamber, per kind of
   measure (the record's kinds: CRA disapproval, War Powers, arms sale, national emergency, D.C. disapproval,
   ordinary bill/resolution...), pooled or split - and what decides the split.
2. What the fit reads: "the recorded chamber-by-chamber votes since 2017" - which roll calls (the extract's final
   agreements of the 1,564 measures of the 115th-119th Congresses are what is on disk; nothing else is saved), which
   members count (yea/nay; present; not voting; independents by caucus), which measures (all, the contested, the
   vetoable...), and how "defector" is defined against a bloc's stance (the bloc's stance = ? its majority as
   recorded? the president's party's position?).
3. When the draw "matters": "only where the chamber's margin is within the drawn count" - defined exactly, including
   the Senate's Rule XXII (cloture, 60) and the expedited procedures the record reads (5 U.S.C. 802 for the CRA,
   P.L. 94-329 s601(b), 50 U.S.C. 1622, D.C. Code s1-206.04 - simple majority, no cloture), ties, the Vice President.
4. How rule (a) decides the veto on a bill that reaches the president against his party (a = "a majority of the
   president's party's House members voted against") - note (a) reads the House; with drawn defectors in the House,
   what is "the majority of the president's party's House members" for a bill in play?
5. The override: two thirds of those present and voting, a quorum present (sourced). US-21 currently DECLARES Poland's
   premise - the re-pass voted by the same sides - "measured and said: under it no Congress of record overrides". Does
   the draw apply to the override vote? (one override of record: H.R. 6395, 116th, Trump first term).
6. H2's print: what is a "fitted figure", what is its n, which interval (method, level), the exact label form
   `FITTED n=...`, and what a figure with too little data prints (cf. H4's UNFITTED).
7. Seeding and determinism: the game draws through seeded streams (`Assets/Scripts/Simulation/SimulationRandom.cs`);
   Poland's veto draw is `PresidentialVeto.Draw` (`Assets/Scripts/Elections/PresidentialVeto.cs`, gate
   `SimulationManager.PresidentialVetoGate`). The instrument's backtest is a seeded simulation over the record: how
   many seeds, which, how results are aggregated across seeds.
8. The instrument's first run - the backtest on the record: what it measures (bills reaching the president, vetoes
   named, hit rate, precision vs B1's 27.1 %, the 25 vetoes, the 13/5 chamber-by-chamber figures as companions...),
   and THE DECISION: what outcome installs (c) as ruled, what STOPS and re-asks (R-US17's own two STOP conditions
   must survive in some form: "far more" bills named than vetoed; a bloc Congress that never sends a vetoable bill),
   and what is merely printed. The outcome must be defined once, generated, and recorded (cf. R-US18's ruling
   record: ASKED / RULED per run).
9. What stays as it is and how each is proved: budget votes; the other five countries (Sweden, Germany, Poland,
   Italy, France - the game's six are SE, DE, PL, IT, FR, US); Poland's B1 (proved unchanged on the simulation bar).
10. Trump's two 2025 vetoes: unpredictable, never counted for or against - in every count, every denominator.
11. How the fitted figures reach the game later (US-21): a generated catalog with its source digest, as
    `Generated.PolishVetoRates` does for Poland (`Tools/veto_b1_backtest.pl`) and
    `Assets/Editor/ElectionsDataCatalogGenerator.cs` for elections data.

#### Facts of the record (no figure of the new rule)

- `Tools/us_veto_prep.pl` writes `ElectionsData/usa/us_veto_measures.csv`: every public law and every vetoed measure
  of the 115th-119th Congresses, 1,564 measures (Trump first term 795, Biden 649, Trump second term 120), each
  chamber's FINAL agreement from its roll call (by voice vote, unanimous consent or a rule's adoption it names no
  roll call), the superseded-vote guards (s802, s831). The readings are in `ElectionsData/usa/veto_record.md`.
- Its header (the only lines you may read):
  ```
  # GENERATED by Tools/us_veto_prep.pl from the GPO's BILLSTATUS records and the roll calls they name (out of tree
  #  held to ElectionsData/usa/raw/vetoes/out_of_tree.txt) - DO NOT EDIT; the record is ElectionsData/usa/veto_record.md.
  congress,measure,kind,outcome,decision_date,president,president_party,law,house_final,house_final_date,house_question,house_R_yea,house_R_nay,house_R_present,house_R_not_voting,house_D_yea,house_D_nay,house_D_present,house_D_not_voting,house_I_yea,house_I_nay,house_I_present,house_I_not_voting,senate_final,senate_final_date,senate_question,senate_R_yea,senate_R_nay,senate_R_present,senate_R_not_voting,senate_D_yea,senate_D_nay,senate_D_present,senate_D_not_voting,senate_I_yea,senate_I_nay,senate_I_present,senate_I_not_voting,senate_I_caucus,override_house,override_house_yea,override_house_nay,override_house_result,override_senate,override_senate_yea,override_senate_nay,override_senate_result,same_day,title
  ```
  (house_final / senate_final name the roll call, e.g. "roll 76", or say there was none; senate_I_caucus e.g. "D:2".)
- The member-level roll calls the extract names are saved out of tree (House Clerk XML by year, Senate XML by
  Congress-session), digests pinned in `ElectionsData/usa/raw/vetoes/out_of_tree.txt`. Only those roll calls exist on
  disk - no other votes of the period.
- `Tools/us_veto_backtest.pl` writes `docs/generated/US_VETO_BACKTEST.md`; `Assets/Editor/UsVetoBacktestCheck.cs`
  (cheap bar) recomputes its tables from the CSV and fails when it is stale. These are the instrument the new rule
  will most likely extend.
- The 25 vetoes: Trump first term 10 (1 overridden), Biden 13, Trump second term 2. (a) pooled: named 33, vetoed among
  them 22 (66.7 %), hit rate 22 of 25. Both chambers: 20 of 21. B1 on Poland: 45 of 166 (27.1 %).
- Seats (each Congress's last roll call): House R-D 115th 235-197, 116th 195-232, 117th 213-218, 118th 219-211, 119th
  218-214; Senate R-D (independents in their caucus) 51-49, 52-48, 50-50, 49-51, 53-47.

#### Precedents to read for FORM (not for figures)

- R-US18's preregistered block: `docs/specs/USA_STAGE_PLAN.md` between `<!-- R-US18 RULE BEGIN -->` (line ~739) and
  `<!-- R-US18 RULE END -->` (~951); R-US19's between its markers (~979-1216). Plain ASCII inside a ```text fence:
  THE TERMS, the steps, the stops and their options, the outcome of a run defined once, the verdict line generated,
  the ruling record, the bars. The checks that hold them: `Assets/Editor/UsHouseCountCheck.cs` (its `RuleDigests`
  list, the block digest read from the plan), `Assets/Editor/UsSenateRaceCheck.cs`.
- How those were made: `COMPLETED.md` s791 (line ~37589) and s794 (~37668); amended s797 (~37726); run s800/s801.
- The digest convention: SHA-256 over the UTF-8 bytes of the lines strictly between the two marker lines, each
  ending LF, no CR, read from the index blob's raw bytes and required equal to the working copy's.
- US-10 (plan line ~279-300), US-21 (line ~440: "Every bill both chambers pass goes to the president of the day - the
  record's or the game's - who vetoes by R-US17's rule; a player-president chooses. The override needs two thirds of
  each House on the sourced base, voted by the same sides (Poland's premise, DECLARED ...)").
- Poland's B1/F3: `Assets/Scripts/Elections/PresidentialVeto.cs`, `docs/generated/VETO_B1_BACKTEST.md`.

## Appendix B. The designer's specification - lens: game

```text
THE R-US17 (c) RULE - SPECIFICATION FROM THE GAME'S-USE LENS (blind: no figure of the rule computed, no CSV line past the header read, no tool or Unity run, nothing edited)

What I read: the brief; USA_STAGE_PLAN.md (US-10, US-18 to US-23, R-US17, the blocks of R-US18 and R-US19 and their asks, H1-H5, G5); COMPLETED.md sections 802 and 803; veto_record.md; the source of Tools/us_veto_backtest.pl and Tools/veto_b1_backtest.pl; PresidentialVeto.cs, SimulationRandom.cs, SimulationManager.PresidentialVetoGate, ParliamentSystem.WouldBillPass, DivisionSide, PolishVetoRates.cs; and the section headings and summary lines of US_VETO_BACKTEST.md only. I did not open its table of every veto.

================================================================
THE TERMS
================================================================
- THE CSV: ElectionsData/usa/us_veto_measures.csv, generated by Tools/us_veto_prep.pl. The rule reads nothing else of the US record. B1's yardstick is read from ElectionsData/poland/third_readings_term10.csv as us_veto_backtest.pl reads it now: 45 of 166, asserted.
- A MEASURE: one row of the CSV. VETOED: its outcome contains "veto" (vetoed; pocket vetoed; vetoed, overridden). NOT VETOED: signed, or law without signature.
- THE EXCLUDED SET (H2): every vetoed measure whose House final AND Senate final both name no roll call. It is asserted equal to {H.R. 131, H.R. 504 (119th)}. Any other set is a data defect and fails the instrument; a later member of the set enters only by amendment.
- A COUNTED MEASURE: a measure outside the excluded set. COUNTED VETOES: the vetoed counted measures.
- A FINAL ROLL CALL: a chamber's house_final or senate_final that reads "roll <n>". Any other final (voice vote, unanimous consent, a rule's adoption) is NO ROLL CALL.
- PARTY OF A MEMBER: R or D by the CSV columns. A Senate independent belongs to the party senate_I_caucus names (R-US10 (a); G3 rules Sinema's reading). An independent whose caucus is N, and every House independent, belongs to no party.
  - Where senate_I_caucus names more than one caucus and the CSV cannot split the I counts, that is a data defect. Before the first run, a catalog step (measuring nothing) adds per-caucus columns to us_veto_prep.pl from the member XML it already reads, with a mutation case.
- PRESIDENT OF A DAY: Obama (D) up to 2017-01-19; Trump (R) from 2017-01-20 to 2021-01-19; Biden (D) from 2021-01-20 to 2025-01-19; Trump (R) from 2025-01-20. A day dated 20 January belongs to the incoming president (veto_record.md reading 2).
  - THE DECISION PRESIDENT is the CSV's president and president_party.
  - PP is the president's party and OP the other party. In the backtest a chamber's PP is set by the president on that chamber's final date. In the game it is the president of the vote's day (PresidentialElection.PresidentAt for the USA). A president whose party holds no seat in the chamber makes both parties OP there.
- ONE STANCE (the game's premise: a party votes as one, one stance in both chambers). For party P on a measure:
  - sigma_P = +1 if Y_P > N_P, -1 if N_P > Y_P, and 0 otherwise, including where no chamber has a roll call.
  - Y_P and N_P are P's yeas and nays summed over the measure's final roll calls in both chambers.
  - Present and not voting are outside both sums.
- CONTESTED: sigma_R * sigma_D = -1.
- SEATS S_P,C of party P in chamber C:
  - in the backtest, every member of P listed on C's final roll call (yea + nay + present + not voting);
  - in the game, P's seated members in C on the day.
- SWORN_C:
  - in the backtest, every member listed on the Senate roll call (R + D + I);
  - in the game, the senators seated that day. Vacancies are outside both.
- A BLOC: a party's seats in a chamber voting sigma_P. A bloc with sigma 0 is outside every count of the vote, but its members stay in SWORN.
- A SENATE PATH, by the CSV's kind:
  - EXPEDITED for "CRA disapproval", "arms-sale disapproval", "War Powers", "national emergency" and "D.C. disapproval" (5 U.S.C. 802; P.L. 94-329 s601(b); 50 U.S.C. 1622; D.C. Code s1-206.04): a simple majority, no cloture.
  - Every other measure is ORDINARY, under Rule XXII.
  - In the game every non-budget US bill is ORDINARY (US-18). No game bill is expedited.
- A BUDGET VOTE: any division the US budget procedure records (US-22: the appropriation, the continuing resolution, reconciliation, the pension-age act), and US-24's debt-limit act. Every other US division of a bill is ORDINARY LEGISLATION.
- THE CELLS: (House, PP), (House, OP), (Senate, PP), (Senate, OP). Each holds OBSERVATIONS (THE FIT).
- d_P,C: the defectors drawn for party P in chamber C (THE MODEL).

================================================================
THE MODEL (what is drawn, from what, per what)
================================================================
1. Parties are blocs by default.
   - A vote is drawn only in the USA, only on ordinary legislation, and only in a chamber where the two blocs take opposite sides (+1 against -1).
   - Everywhere else the bloc count is the count, and nothing is drawn or recorded.
2. Per bill, per chamber, per party, per vote day, one uniform u in [0, 1) is drawn by the keyed draw (SEEDS AND DETERMINISM).
3. From what: the party's cell, set by chamber and role. It is a list of N observations (k_j, n_j), sorted by share k_j/n_j ascending (compared as k1*n2 against k2*n1 in integers), ties in CSV row order.
   - j = floor(u * N) in IEEE-754 double, clamped to N - 1.
   - d = floor((2*k_j*S + n_j) / (2*n_j)) in integers: the share scaled to the party's seats S, a half rounded up.
   - An empty list gives d = 0.
4. Per what:
   - A party's defectors in a chamber vote against its sigma: a yea bloc's defectors vote nay, a nay bloc's vote yea.
   - The same d counts in every vote on that bill in that chamber: cloture, passage, override. One key drawn once.
   - The House and Senate draws are independent, and so are the two parties' draws.
   - Third parties, if ever seated, vote as blocs and draw nothing (no fit).
5. Pooled over kinds and over presidents; split by chamber and by role (PP/OP), never by party label. That is four cells, reduced by the thin-cell rule (THE FIT).
   - Why role: R held the presidency for two terms and D for one, so a split by label would confound party with role. The game needs the president's party.
   - Why kinds are pooled: the game has no expedited kinds, and a split would leave the game reading an "ordinary" cell that excludes the disapprovals H1 was ruled for. Both splits are printed (R2, R3).
6. A projection reads probabilities and never a draw. Only the gate draws (HOW THE FIGURES REACH THE GAME).

================================================================
THE FIT (data, members counted, measures, defector definition, estimator)
================================================================
- Data: the CSV's final roll calls of the counted measures, 115th-119th Congresses ("the recorded chamber-by-chamber votes since 2017").
  - Override roll calls are not in the fit.
  - Records of measures that failed are not on disk. The fit is therefore conditional on passage: DECLARED, printed as a premise, not sourced.
- Measures: contested counted measures. Each chamber with a final roll call gives one OBSERVATION per party with n > 0:
  - n = P's yeas + nays in that chamber (Senate independents in their caucus; present and not voting outside);
  - k = those of them voting against sigma_P (nays where sigma_P = +1, yeas where -1);
  - its cell is (that chamber, P's role on that chamber's final date).
  - A roll call that is final for two measures gives one observation per measure, and the instrument prints those measures by name.
- Defector: a member voting against his party's ONE STANCE on the measure, not against his chamber's party majority. Shares can therefore exceed one half where a chamber's party majority went against the stance. The president's position is not read: it is on no saved page, and inferring it from the outcome would be circular.
- Estimator: the empirical distribution of (k, n) in each cell, the nonparametric maximum-likelihood estimate. No parameter, no optimiser, no failure to converge.
- Thin-cell rule:
  - A cell with fewer than 10 observations is POOLED with the other role of its chamber. Both parties then draw from the union, sorted as in THE MODEL.
  - A union with fewer than 10 is UNFITTED (S3).
  - Why 10: below ten, the 95% Wilson interval on a proportion near one half spans more than half of [0, 1].
  - Pooling is decided on the full fit and held fixed in every bootstrap replicate.
- The game's catalog holds the full fit.
- The backtest predicts each measure LEAVE-ONE-OUT (LOO): the cells less every observation of that measure. The in-sample run is printed beside it (R4).

================================================================
WHEN THE DRAW MATTERS (the margin test per chamber and procedure)
================================================================
For a vote in chamber C:
- Y0 = the seats of the +1 blocs; N0 = the seats of the -1 blocs.
- x = (sum of d over the -1 blocs) - (sum of d over the +1 blocs).
- Y = Y0 + x; Nn = N0 - x.

The rule for each procedure:
- HOUSE PASSAGE: passes iff Y > Nn (a majority of those voting; a tie fails). Suspension's two thirds is not modelled, as US-18 states.
- SENATE, ORDINARY (Rule XXII): passes iff Y >= floor((3*SWORN + 4)/5), three fifths of the senators duly chosen and sworn, computed from the count on the day.
  - Passage after cloture follows on the same d.
  - The Vice President has no cloture vote.
- SENATE, EXPEDITED (backtest only): passes iff Y > Nn. Where Y = Nn, the Vice President votes PP's sigma in the Senate: +1 passes; -1 or 0 fails; a vacant vice presidency fails.
- A CHAMBER WITH NO ROLL CALL (backtest only): passes, with no draw and no observation.
- NOT CONTESTED (blocs agree, or a bloc at 0): the bloc count decides, with x = 0.

IN PLAY:
- A vote is IN PLAY iff its outcome with x differs from its outcome with x = 0. This is "the chamber's margin is within the drawn count", for every procedure above and for the override.
- A vote not in play is recorded as the blocs' division, and the draw leaves no trace.
- A vote in play is recorded with each party's d > 0 as its own side, flagged as defectors (a DivisionSide field: a format step at US-21).
- Every outcome is computed with x. Only the record and rule (a) read the recorded division.

================================================================
THE VETO (rule (a) on a bill in play)
================================================================
- An ordinary bill passed by both chambers goes to the president of the day.
- An AI president (the record's or a game-elected one) vetoes iff 2 * NAY_PP > S_PP,House, where NAY_PP is PP's House members voting nay on the RECORDED House passage division:
  - S_PP - d_PP if PP's sigma is -1 and the House was in play;
  - S_PP if its sigma is -1 and the House was not in play;
  - d_PP if its sigma is +1 and the House was in play;
  - otherwise 0.
- This is B1 transposed, as us_veto_backtest.pl's rule (a) reads it: members, nays only.
  - A PP that abstained (sigma 0) has voted against nothing, so nothing is vetoed.
  - A president whose party holds no House seat vetoes nothing. This mirrors Poland's reading (G5 items 14-15, PROVISIONAL).
- The veto is deterministic: "rule (a) then decides" (H1). The rate-draw form (F3's) is printed (R8).
- A player-president chooses on the Docket, where the bill waits until decided (US-23's form). No rule decides for him.
- In the backtest the decision president's party is read: NAMED = reaches the president and (a) fires on the simulated House division.
- A House final with no roll call names nothing.

================================================================
THE OVERRIDE
================================================================
- In each chamber it carries iff 3*Y >= 2*(Y + Nn): two thirds of those present and voting (CRS RS22654, "two-thirds of the Members voting, a quorum being present").
  - Y and Nn are on the passage's sides with the SAME d: the same blocs, the same drawn defectors, no fresh draw.
  - This is US-21's declared premise ("voted by the same sides"), now carrying the drawn defectors.
- The quorum is present >= floor(seated/2) + 1. Every seated member votes in the game, so the quorum is asserted, not drawn.
- The originating chamber votes first; where it fails, the other does not vote. The veto and both votes fall on the passage's day (Poland's premise).
- Cloture on an override is not modelled. With every senator voting, ceil(2v/3) >= ceil(3v/5) for every v >= 1, asserted.
- Each override vote has its own in-play test and is recorded as in WHEN THE DRAW MATTERS.
- Why no fresh draw: the record holds one override (H.R. 6395, 116th) and about twenty sustained votes, which cannot carry a fit of their own (H2). A fresh draw is printed (R11). Overrides never decide (THE DECISION).

================================================================
THE PRINT (H2)
================================================================
- A FITTED FIGURE is any figure computed from the cells: each cell's mean share and its share of observations with k >= 1, and every model output (the expected sent, vetoable, named, hits, precision, hit rate, P(none)).
  - Record counts, B1's 45 of 166, the counted vetoes and the s802 companions are data, printed plain.
- Its n:
  - for a cell figure, the cell's observations;
  - for a model output, the distinct measures that contribute an observation to any cell the figure reads (full fit).
- Its interval: a nonparametric percentile bootstrap at 95%, B = 1000.
  - Replicate b (b = 1..1000) resamples, with replacement, the M contested counted measures that hold at least one observation, ordered by CSV row.
  - Pick i (0-based, M picks) takes index floor(U * M), where U is the keyed draw with master seed b, date 2017-01-20 and billKey "bootstrap|<i>".
  - Each pick brings all its observations, in both chambers and both roles. Cells are re-sorted, and the figure is recomputed exactly (LOO within the replicate: every copy of the predicted measure removed).
  - lo and hi are the 25th and 976th of the 1000 sorted values.
  - A replicate where a ratio's denominator is zero is UNDEFINED. Their count is printed, and the interval reads "UNDEFINED IN <k> OF 1000" when k > 25.
- The exact label: "<value> FITTED n=<n> 95% [<lo>, <hi>]", with " (roles pooled)" after n where pooled.
  - Shares, probabilities and precision are printed to 3 decimals, expected counts to 2.
- Too thin: "UNFITTED n=<n> - fewer than 10 observations", with no value. Every model output that reads such a cell prints UNFITTED too (H4's form).
- The draw's own spread is printed apart, never as the FITTED interval: "<mean> DRAWN 1000 worlds [<2.5th>, <97.5th>]" (THE FIRST RUN).

================================================================
SEEDS AND DETERMINISM
================================================================
- The draw is keyed, not a stream draw. SimulationRandom.Stream gains no value, so every stream's seed and position is untouched.
  - Reasons (Poland's precedent, PresidentialVeto.Draw): a stream draw shifts every later draw on it; a projection must consume nothing; and a reload, the shadow and a fork that reach the vote on the same day must draw alike.
- The construction: u = FNV-1a 64 over the key's UTF-8, finished with MurmurHash3's fmix64, the top 53 bits as the fraction. It is byte-for-byte PresidentialVeto.Draw's construction, but implemented in a new UsVeto class.
  - PresidentialVeto.cs is not edited, and its PlantDraw scope does not plant US draws. UsVeto has its own plant scope.
- KEY = masterSeed (invariant) + "|USA|" + yyyy-MM-dd (the chamber's vote day) + "|" + billKey + "|" + chamber ("House" or "Senate") + "|" + party ("REP" or "DEM") + "|defectors".
  - billKey in the game is the division's title.
  - In the backtest it is "<congress>th Congress <measure>", with R mapped to REP and D to DEM, and the date taken from that chamber's final date in the CSV.
  - Two divisions with one title on one day are one act to the draw (Poland's premise). A world that resolves the bill on another day draws apart (Poland's PREMISE, inherited and stated).
- The backtest uses no stream:
  - DECISION figures are exact expectations, computed by enumerating every pair (j_PP, j_OP) of each contested chamber. P per chamber = count / (N_PP * N_OP), as reduced fractions in 64-bit integers.
  - The DRAWN WORLDS: master seeds 1..1000 run the game's own keyed draw over every counted measure on the LOO cells.
  - BOOTSTRAP replicates: master seeds 1..1000, keyed "bootstrap|<i>", a key domain disjoint from the worlds'.
- The identity: for expected named, hits and vetoable sent, |mean over the 1000 worlds - exact| <= 4 * (sd / sqrt(1000)). A failure is a defect, never an ask.
- A second run in one session gives the same decisive digest, asserted.

================================================================
THE FIRST RUN (what the backtest on the record measures and prints)
================================================================
The instrument is UsVetoBacktestCheck, extended. It is C# and calls UsVeto's runtime functions (the draw, d, chamber outcome, exact probability, (a), override) on chambers built from the CSV, so the backtest runs the arithmetic the game runs.
- It writes docs/generated/US_VETO_C_BACKTEST.md, stamped with the CSV's digest, the rule block's digest, the decisive digest and the ruling-record state.
- It writes the catalog (HOW THE FIGURES REACH THE GAME).
- us_veto_backtest.pl and its s802 document stay as the record of s802.

Per counted measure i, each chamber C is built from the CSV:
- sides sigma_R and sigma_D (one stance); seats as in THE TERMS; the path by kind.
- Contested chambers with a roll call are drawn; other chambers follow WHEN THE DRAW MATTERS.
- p_reach = P_House * P_Senate.
- p_named = P(House passes and (a) fires) * P_Senate.
- p_vetoable = p_reach if the decision president's party has sigma != +1, else 0.
- p_override = P(named and both overrides carry) / p_named.

Printed:
1. The record per president and pooled, with the excluded set on its own line (TRUMP'S 2025 VETOES).
2. The cells: n; each observation (measure, k/n), sorted; pooled or UNFITTED; mean share and share with k >= 1, each FITTED.
3. Per president and pooled, each FITTED and each also DRAWN:
   - E[sent] = sum of p_reach; E[vetoable sent]; E[named]; E[hits] = sum of p_named over counted vetoes; E[false positives]; E[misses];
   - PRECISION = E[hits]/E[named]; HIT RATE = E[hits]/(counted vetoes);
   - P(NONE) = the product of (1 - p_vetoable) in CSV row order.
   - Sums are in IEEE double, in CSV row order.
4. B1's yardstick, 45 of 166 (27.1%), read again from the Sejm's CSV.
5. Every counted veto: p_reach, p_named, p_override (FITTED), the kind, and the record's override votes beside them.
6. The s802 companions, recomputed by s802's own functions and printed plain:
   - one stance under blocs: 0 of 34;
   - chamber by chamber: 13 vetoable, 5 of the vetoes;
   - (a) on the votes as cast: 22 of 33, hit rate 22 of 25.
   - Each is reprinted beside on H2's counted base, with 23 counted vetoes as the denominator.
7. The zero-draw run of this rule's arithmetic (every d = 0), beside the one-stance companion, with every difference named.
8. E[named] by kind (printed only).
9. Each declared reading's headline figures and outcome (DECLARED READINGS).
10. The identities: the worlds against exact; B1 = 45/166; N >= 0 and every share in [0, 1]; the excluded set; PresidentialVeto.Draw on 8 pinned keys reproduced by UsVeto's hash with Poland's key format.

================================================================
THE DECISION
================================================================
H1 has ruled (c). The run does not choose between options: it installs (c) or re-asks. The outcome is set by the default reading's LOO exact figures alone.
- S1, "far more" (R-US17's first STOP, and its condition that (a) reach at least B1's precision; both rest on B1's yardstick, read as G5, PROVISIONAL): pooled PRECISION < 45/166, tested as E[hits]*166 < E[named]*45 in double; equal is not below. E[named] = 0 also fires S1.
- S2, "a bloc Congress under Rule XXII never sends a vetoable bill", in the form that survives H1 (blocs plus the draw): P(NONE) >= 0.5. That is, in at least half the worlds the record's whole window sends no vetoable measure to the president.
- S3: any cell UNFITTED.

THE OUTCOME is INSTALL when none fires; otherwise it is the set of stops that fire. Its figures are not part of it. Everything else is PRINTED ONLY, including hit rate, kinds, overrides, presidents, readings and companions. Declared readings never stop. A reading whose outcome differs is named in the verdict line.

The verdict line is generated, never typed:
- "By R-US17's rule (c) as ruled (H1-H3): INSTALLED - precision <x> FITTED n=.. 95% [..] against B1's 27.1% (45 of 166); P(none vetoable) <y> FITTED ...; every cell fitted; [the same under every declared reading | the outcome differs under R..]. The figures go to Elias before US-21 wires them (E4)."
- or "R-US17's rule (c) does not install: <each stop with its figures> - asked."

The stops' options, each with "amend the rule as ruled" besides:
- S1: install as measured; the veto as a rate draw at (a)'s precision (R8); the reading of "far more" re-ruled; or keep the blocs (no US veto in play, US-21 says so).
- S2: install as measured; chamber-by-chamber stances (s802's second option, R1c); or keep the blocs.
- S3: all four cells pooled; zero defectors in that cell, printed UNFITTED; or source failed roll calls (not on disk).

================================================================
WHAT STAYS AS IT IS AND ITS PROOF
================================================================
- BUDGET VOTES draw nothing.
  - US-21's diagnostic plants the US draw at u -> j = N-1 (the largest share). Every US budget division must be identical to the unplanted run, and UsVeto's draw-call counter must read 0 on the budget path.
  - Before US-22 no US budget division exists. The assertion is installed at US-21 and re-run at US-22.
  - A budget act may still be vetoed by (a) on its bloc division (US-22, US-23).
- THE FIVE OTHER COUNTRIES (SE, DE, PL, IT, FR):
  - UsVeto.Applies is true for the USA alone, asserted for all six CountryId values.
  - The draw counter reads 0 in every non-US code path the diagnostic runs.
  - SimulationRandom's enum is unchanged.
  - The trajectory sentinel and the dump diff are byte-identical for every start. The no-policy dump meets no US ordinary bill, asserted by the counter at 0; a nonzero count stops the item and reports, with Tools/traj_diff.pl.
- POLAND'S B1 (F3 as live), on the simulation bar:
  - the sentinel holds against its pin on both seeds;
  - PresidentialVetoDiagnostic is green with its pins unmoved;
  - Generated.PolishVetoRates' digests are unchanged;
  - PresidentialVeto.Draw reproduces its 8 pinned values;
  - PresidentialVetoGate's body is untouched, because the US goes through a new UsVetoGate;
  - GovernmentBudgetBillDiagnostic's Poland pins are unmoved.

================================================================
TRUMP'S 2025 VETOES
================================================================
- H.R. 131 and H.R. 504 (119th): DECLARED UNPREDICTABLE (H2), with no recorded vote in either chamber.
- They are out of every count and every denominator: measures, counted vetoes, the fit (they hold no observation, asserted), P(NONE), precision, hit rate, the override print (their House override votes of 8 Jan 2026 are printed in their own line only), the per-president table (Trump's second term: 118 counted measures, no counted veto, precision "-"), the companions, and the digest's figures.
- They are printed in one line beside the record table.

================================================================
HOW THE FIGURES REACH THE GAME (US-21)
================================================================
- The instrument generates Assets/Scripts/Elections/Generated/UsDefectorCells.cs ("GENERATED - DO NOT EDIT"). It holds:
  - SourceDigest (the CSV's SHA-256), RuleDigest (the block's current digest), DecisiveDigest, Outcome;
  - per cell: its chamber, role, pooled and fitted flags, n, its FITTED figures with their intervals, and the full-fit observation arrays (k, n) in the sorted order, each with its measure as a comment.
  - The cheap bar regenerates it in memory and fails on any byte of difference (the PolishVetoRates and ElectionsDataCatalogGenerator pattern).
- Runtime: Assets/Scripts/Elections/UsVeto.cs (pure functions: Applies, the keyed DefectorDraw, Defectors, ChamberOutcome, PassProbability, NamedByA, OverrideCarries).
  - It is built with the instrument, which measures it.
  - It is wired only at US-21 by SimulationManager.UsVetoGate, a new method beside PresidentialVetoGate, which it never edits.
  - The gate refuses to wire unless Outcome is INSTALLED, or RULED with a buildable (c). US-21's diagnostic asserts this and that the digests are current.
- The gate alone draws.
- ChamberVerdicts.Veto's USA branch projects, exactly and without a draw:
  - the bloc verdict per chamber with P(reversed) where it is above 0;
  - P(reaches);
  - (a)'s answer for an AI president, or "your pen" for a player-president;
  - P(override).
  - The diagnostic asserts the projection's call counter on the draw is 0.
- Screens print a projected chance with "fitted on n=<n> recorded votes". The intervals stay in the document and the E4 reply (R12).

================================================================
DECLARED READINGS (each run and printed beside; never a stop; a different outcome is named in the verdict line)
================================================================
- R1, the stance basis: default, one stance pooled over both chambers' members. Alternatives:
  - (b) the House stance alone;
  - (c) chamber-by-chamber stances (s802's declared reading), with shares then <= 1/2.
- R2: cells by role (default) / by party label.
- R3: kinds pooled (default) / ordinary and expedited cells apart.
- R4: LOO (default) / in-sample.
- R5: role by the vote day's president (default) / by the decision president.
- R6: present outside n and the stance (default) / present counted with nay (the s802 bloc reading).
- R7: S1 on the point estimate (default) / on the 95% upper bound; and the literal E[named]/(counted vetoes) printed.
- R8: (a) deterministic (default) / the veto drawn at (a)'s FITTED precision per president (F3's form).
- R9: the draw only where the blocs oppose (default) / in every chamber vote of ordinary legislation.
- R10: the draw on every ordinary bill with opposed blocs (default; budget votes are carved out because they too go to the president, so "the veto path" is every bill that goes to him) / only where PP's sigma is -1.
- R11: the override on the same d (default) / a fresh keyed draw ("|override").
- R12: H2 governs the instrument's prints and the reply to Elias, and screens print n (default) / screens also print the interval.
- R13: the Vice President votes PP's sigma (default) / a tie fails.
- R14: a pooling threshold of 10 (default) / 20.

Premises, stated and not read:
- the record holds passed measures only;
- independent draws between the parties and between the chambers;
- the House majority rule without suspension;
- a chamber without a roll call passes undrawn;
- the record's appropriations are treated as ordinary measures (the CSV carries no budget class);
- the ten days and the pocket veto are not modelled;
- save-scumming by retitling or delaying a bill redraws (Poland's premise).

================================================================
QUESTIONS FOR ELIAS
================================================================
None that H1-H3 cannot settle. Every open choice above has a default and a printed alternative. The two that carry the most game weight, R10 (defections on every ordinary bill, the player's own included) and R8 (a deterministic veto), go into the E4 reply with their figures, and he can amend either by a ruling.

================================================================
THE RULING RECORD AND THE BARS
================================================================
- The block sits in USA_STAGE_PLAN.md between "<!-- R-US17 RULE BEGIN -->" and "<!-- R-US17 RULE END -->".
  - Its digest is SHA-256 over the UTF-8 bytes of the lines strictly between the markers, LF only, read from the index blob and required equal to the working copy.
  - The digest is committed in UsVetoBacktestCheck's RuleDigests list BEFORE the first run (H3). Amendments are appended, each naming Elias's ruling. A change without a ruling fails.
  - A choice changed after a result is in sight takes a new digest and is reported (G1).
- The decisive digest is SHA-256 over:
  - each cell's sorted observations (measure, k, n) and its pooled and fitted flags;
  - per counted measure, p_reach, p_named and p_vetoable as reduced fractions under the default reading;
  - the excluded set; the outcome.
  - Nothing else.
- The ruling record:
  - NONE before the first run;
  - INSTALLED, with the section whose reply carries the figures (E4) and the decisive digest;
  - ASKED, with the section, the outcome and the digest;
  - RULED, with the ruling, its option, the outcome and the digest.
  - The line after the block mirrors the state, section and ruling, never a digest; the instrument fails while the two differ.
  - NONE fails every run after the first-run commit.
  - INSTALLED and ASKED pass while the digest and the outcome equal the registered ones.
  - A moved digest with the same outcome fails until a commit re-pins it, naming the old digest, the new one and "outcome unchanged", with the moved figures in the next reply to Elias (E4).
  - A changed outcome asks R-US17 again.
  - A ruling must name a buildable option: (c) as measured, or a stop's option.
- Cheap bar (UsVetoBacktestCheck), which fails on:
  - a stale document or catalog;
  - a block digest that is not the last on the list;
  - a mirror line that differs;
  - an identity failing;
  - a data defect (the excluded set, an inseparable caucus, B1's yardstick);
  - the s802 document stale (as now).
- Mutation suite: Tools/us_veto_mutations.sh is re-run whenever us_veto_prep.pl changes (the superseded-vote case t04 is DONE, s831), with new cases for any catalog-step columns. Planted runs prove the UNFITTED, S1, S2 and S3 paths (section 830's form).
- Simulation bar on the US-21 commit: Poland's proof and the six-country dump diff as above.
- US-21's veto diagnostic asserts INSTALLED, or RULED-(c), with current digests before the gate wires.
```

## Appendix C. The designer's specification - lens: inference

```text
R-US17'S RULE (c): THE SPECIFICATION (blind panel, lens: inference from thin data)

Plain ASCII, in the order the brief asks for. Section numbers the session has not yet assigned are written <N>. "Asserted" means the instrument fails while the statement is false. A defect means a code or data failure; it is never something put to Elias.

======================================================================
THE TERMS
======================================================================
- THE RECORD. ElectionsData/usa/us_veto_measures.csv exactly as Tools/us_veto_prep.pl generates it. Every output is stamped with its SHA-256. Nothing in it is typed or recomputed.
- UNPREDICTABLE (H2). H.R. 131 and H.R. 504 of the 119th Congress.
  - Asserted: these two are exactly the vetoed rows whose house_final and senate_final both name no roll call.
  - Any other vetoed row with no roll call in either chamber fails the instrument until the block is amended by a ruling. H2 names two vetoes and is never extended silently.
- COUNTABLE MEASURES: every row except the two. COUNTABLE VETOES: the countable rows whose outcome is vetoed, pocket vetoed, or vetoed and overridden (US-10's VETOED). V_count is their number, computed and never typed.
- A FINAL ROLL CALL is a house_final or senate_final of the form "roll <n>".
  - Its identity is (chamber, congress, year of its final date, n).
  - A roll call that is the final agreement of several measures is one roll call, everywhere.
  - Asserted: every final roll call of a countable measure is dated 2017-01-03 or later ("since 2017", H1).
- THE PRESIDENT OF A DAY follows veto_record.md reading 2: terms change at noon on 20 January, and a date of 20 January belongs to the incoming president.
  - The DECISION PRESIDENT and his party are the row's president and president_party.
  - The VOTE PRESIDENT is the president of the roll call's own date.
  - The VICE PRESIDENT's party is the vote president's party (Pence R, Harris D, Vance R; each the same party as his president).
- BLOCS. On a final roll call, a chamber has an R bloc and a D bloc: every member of that party the roll call lists (yea, nay, present, not voting).
  - A Senate independent joins the bloc senate_I_caucus names, but only when every independent on that roll call is in one caucus, R or D. Otherwise every independent on it stays outside both blocs.
  - House independents always stay outside both blocs.
  - X is the members outside both blocs. They cast nothing in any passage test, as in US-10.
- For a bloc P (with its joined independents): Y_P yeas, N_P nays, Pr_P present, NV_P not voting. Seats S_P = Y_P + N_P + Pr_P + NV_P.
  - Asserted: every bloc with seats on a final roll call has Y_P + N_P + Pr_P >= 1.
- STANCE (US-10's, F3's count): yea if Y_P > N_P + Pr_P, otherwise nay.
- CONTESTED: a chamber's final roll call is contested when the R and D stances differ.
- CROSSERS ON THE RECORD: k_P = N_P when the stance is yea, Y_P when it is nay. TRIALS: m_P = Y_P + N_P. Present and not voting are neither crossers nor trials.
- ROLE, relative to a stated president: PP (the president's party) or OP (the other party).
- SWORN W: every member the roll call lists, X included. In play, the members seated that day.
- PROCEDURES, by the extract's kind (veto_record.md reading 7):
  - House: majority of those voting.
  - Senate, kind "bill": cloture, needing C = floor((3W + 4) / 5) yeas (Rule XXII: three fifths of senators duly chosen and sworn).
  - Senate, the five expedited kinds (CRA, War Powers, arms sale, national emergency, D.C.): simple majority, no cloture (5 U.S.C. 802; P.L. 94-329 s601(b); 50 U.S.C. 1622; D.C. Code s1-206.04).
- B1'S YARDSTICKS, recomputed from the Sejm's CSV as Tools/us_veto_backtest.pl does (the tool dies unless they are equal):
  - precision 45 of 166;
  - hit rate 45 of 53.
- N_MIN = 20 roll calls.
- EQUALITY. Every comparison is made exactly as written. Comparisons in floating point treat a difference of at most 1e-9 as equal, and equal is never "below".

======================================================================
THE MODEL (what is drawn, from what, per what)
======================================================================
1. WHEN A DRAW IS MADE. Only on a non-budget bill, in a chamber whose two blocs take different sides, separately for each of the two blocs: per bill, per chamber, per party (H1).
   - Where the blocs agree, or (on the record) the final names no roll call, nothing is drawn and the blocs vote whole.
2. WHAT IS DRAWN: a crossing count c for the bloc.
   - The bloc's CELL holds a list L of recorded (k, m) pairs (THE FIT). L is sorted ascending by k/m (compare k1*m2 with k2*m1), ties broken by the roll call identity's ASCII string. n = |L|.
   - From u in [0, 1): take (k, m) = L[floor(u * n)].
   - c = min( floor((S - 1) / 2), floor((2*k*S + m - 1) / (2*m)) ), where S is the bloc's seats. This is the recorded crossing share carried onto the bloc's size, rounded to the nearest integer with a half going to the bloc, and capped so that crossers stay a minority and the bloc keeps its stance.
3. THE CELLS: (chamber, role) = House-PP, House-OP, Senate-PP, Senate-OP.
   - The role is relative to the president who will receive the bill. In play that is the president of the day. In the record backtest it is the decision president, because play takes the vote and the decision on one day.
   - Pooled over presidents, Congresses, kinds and outcomes. No split is ever chosen from the data.
4. THIN CELLS. A cell whose full-record n is below N_MIN is UNFITTED and draws from its chamber's pooled list.
   - The chamber's pooled list holds both roles' observations; its n counts roll calls.
   - If the chamber's pooled list also has fewer than N_MIN roll calls, it draws from the all-pooled list.
   - If the all-pooled list has fewer than N_MIN roll calls, S0 fires.
   - This structure is fixed once, on the full record, and holds in every replicate and every leave-one-out list.
5. HOW THE DRAWN DIVISION IS CAST.
   - Loyal members vote their bloc's stance. Crossers of a yea bloc vote nay; crossers of a nay bloc vote yea.
   - In play only, a bloc whose side is "abstain" draws as a nay bloc: its loyal members cast nothing and its crossers vote yea.
6. INDEPENDENCE. The draws of the two blocs and of the two chambers are independent. The draw for a bloc is not conditioned on whether the bloc is for or against the bill.
7. U. A keyed fraction, never a stream (PresidentialVeto.Draw's form): the FNV-1a 64 hash of the key's UTF-8, finished with MurmurHash3's 64-bit mix, top 53 bits taken as the fraction.
   - The key is: masterSeed + "|USA|" + yyyy-MM-dd + "|" + title + "|" + ("House" | "Senate") + "|" + ("REP" | "DEM") + "|defectors".
   - Two divisions with one title, in one chamber, on one day are one act and draw the same.
   - It has its own plant scope; Poland's _planted is never read.
8. BUDGET VOTES DRAW NOTHING (H1). In play, every act WorldClock.BudgetProcedureOf tables for the US (US-22: the appropriation, a continuing resolution, reconciliation, the pension statute) is voted by blocs with no draw.
   - On the record, the extract carries no budget flag. Its display titles cannot pick out reconciliation (the enacted short titles hide "reconciliation pursuant to"). So the backtest reads every measure under US-10's kinds (see Declared Readings D9).

======================================================================
THE FIT (data, members counted, measures, defector definition, estimator)
======================================================================
- DATA: every final roll call of every countable measure, of every kind and outcome, signed and vetoed alike, that is contested in its chamber. Nothing else is on disk: override roll calls carry no party split, and failed votes were never saved.
- CELL ASSIGNMENT: each contested roll call gives one observation (k_P, m_P) to each party, filed by the party's role relative to the VOTE president.
  - A party with m_P = 0 on a roll call gives no observation.
  - n of a cell is its number of roll calls. Member votes are never the unit, because members within one roll call are not independent.
- MEMBERS COUNTED:
  - yea and nay are trials;
  - present and not voting are outside the trials (and count only in the stance and in S);
  - Senate independents are counted by caucus as in THE TERMS, outside when the caucus is mixed;
  - House independents are outside.
- DEFECTOR: a member voting the opposite of his bloc's recorded stance, where the stance is the bloc's own majority as recorded (yea iff Y > N + Pr). It is not the president's position.
- ESTIMATOR: the empirical distribution of recorded crossing shares per cell (nonparametric resampling). Reasons:
  - It assumes no shape for a distribution that is overdispersed and heavy at zero. A binomial with one rate would understate exactly the large crossings that decide close votes.
  - It has no optimizer that can fail, so it is never unfitted except by n.
  - It never draws more crossing than the record shows.
  - Each of its figures is a share of n roll calls, so it has an exact interval.
- THE FITTED FIGURES OF A CELL, each a proportion of its n roll calls:
  - q0, the share with k = 0;
  - q05, q10 and q20, the shares with k/m >= 0.05, 0.10 and 0.20;
  - the largest share, printed without an interval as an order statistic.
- LEAVE ONE OUT. In the backtest, measure j draws from lists that omit every roll call that is a final of j. Each pi_j below is computed this way.

======================================================================
WHEN THE DRAW MATTERS (the margin test per chamber and procedure)
======================================================================
In a contested chamber:
- F = seats of the yea bloc, A = seats of the nay bloc, X = seats outside. In play, A_cast = A, or 0 where the nay bloc abstains.
- Drawn crossers: f from F, a from A.
- Yeas Y = F - f + a. Nays N = (A_cast - a) + f. In play's abstaining case, N = f.

The passage tests:
- HOUSE: passes iff Y > N. With a nay-voting bloc: (F - A) + 2(a - f) > 0. A tie fails. Suspension's two thirds is not modelled, as US-10 and play read it (D10).
- SENATE, CLOTURE (kind "bill", Rule XXII): passes iff Y >= C, with C = floor((3W + 4) / 5) from the sworn count on the day, never a fixed 60. Cloture is read on the final passage roll call, as US-10 reads it. The Vice President never votes on cloture.
- SENATE, MAJORITY (the five expedited kinds; in play, reconciliation is a budget vote and draws nothing): passes iff Y > N. At Y = N it passes iff the Vice President's party bloc is the yea bloc. The Vice President votes his party bloc's stance, is never a crosser, and is not fitted.
- UNCONTESTED, or (record only) no roll call: no draw; the test at f = a = 0. A final with no roll call passes, unanimous under blocs, as in US-10.
- QUORUM: Y + N + (members cast nothing by an abstaining bloc) >= floor(W/2) + 1. On the record it is met by every final roll call. In play every seated member votes.
- THE DRAW MATTERS iff the test at the drawn (f, a) differs from the test at (0, 0). This is H1's "the chamber's margin within the drawn count", made exact. The instrument also prints the equivalent margin: the smallest a with f = 0 that passes a failing bloc vote, or the smallest f with a = 0 that fails a passing one.
- VACANCIES: W and the blocs are what the roll call lists (on the record) or who is seated (in play). Cloture's C moves with W.
- SAME-DAY ACTIONS: the final agreement is the extract's (reading 4: the later action by the record's own time; the guards of s802 and s831). The instrument reads only house_final and senate_final, makes no further choice, and prints same_day.

======================================================================
THE VETO (rule (a) on a bill in play)
======================================================================
- A bill that both chambers send goes to the president of the day.
- (a) fires iff 2 * (the nays of the president's party's House bloc in the DRAWN division) > S_PP(House). The members are the bloc's seats: in play every seat votes, and on the record S counts present and not voting.
- A fired (a) is a VETO WITH CERTAINTY. H1 says "rule (a) then decides", and (a) is B1 as first ruled, which drew nothing. The alternative, a draw at (a)'s fitted precision in F3's form, is D8.
- IDENTITY, asserted in every run and every replicate: under the model, (a) fires exactly where the president's party's House bloc stance is nay. The cap keeps its loyal nays above half of S. So the draw decides only whether the bill REACHES the president, never whether (a) fires on it.
- A bill opposed by the president's party only in the Senate is signed, because (a) reads the House.
- A House final with no roll call names nothing.
- On the record, the stance "nay" includes presents (F3's count). So the model's named base is F3-transposed's House base, not (a)'s member count as US-10 printed it. The instrument lists every measure where the two differ.
- The veto rule reads every act presented, budget acts included (US-22: "both acts may be vetoed"). Only the draw is withheld from budget votes.
- A player-president chooses (US-21). The record backtest uses the decision president.

======================================================================
THE OVERRIDE
======================================================================
- BASE (CRS RS22654, saved): "two-thirds of the Members voting, a quorum being present". Those voting are yea plus nay, and present is outside.
- A chamber overrides iff Y_o >= 2 * N_o and the quorum is met. The veto falls iff both chambers override.
- THE DIVISION. The re-pass reuses the passage's DRAWN division in each chamber: the same members, the same crossers. This is US-21's declared premise, "voted by the same sides", carried onto the drawn split.
- NOTHING IS FITTED FOR THE OVERRIDE. The extract holds override totals without a party split, so no override crossing can be fitted, and a fresh draw would rest on nothing.
- On the record: omega_j = the product over the two chambers of P(passes and Y >= 2N), by the same exact enumeration. Expected overrides O_hat = sum over j of A_j * omega_j. It is printed beside the record's single override (H.R. 6395, 116th). The instrument states whether (a) names that measure.
- Printed only, never deciding.

======================================================================
THE PRINT (H2)
======================================================================
- A FITTED FIGURE is any number computed from the cells' lists: a cell's q's; pi_j; and the totals N_hat, T_hat, P_hat, H_hat, W_hat, O_hat and their per-president forms.
- Record counts (measures, V_count, the record's vetoes and override, US-10's companions) and B1's yardsticks are record figures. They print plainly as "x of y".
- n:
  - for a cell figure, the cell's roll calls;
  - for a derived figure, n = the distinct contested roll calls in the cells it reads, and m = the countable measures in its denominator (for P_hat, the measures with A_j = 1; for H_hat, V_count).
- INTERVALS, all at 95%:
  - CELL FIGURES: Clopper-Pearson exact (CP). It is honest at small n: its coverage is at least 95% at every n and every true share, and it never collapses to zero width at 0 of n or n of n, as the Wald interval does.
  - DERIVED FIGURES: the percentile bootstrap (BOOT) over countable measures.
    - 999 replicates; each resamples the countable measures with replacement, M picks.
    - The fit then reads the resampled measures' roll calls with their multiplicity, the scoring reads the resampled measures, and leave-one-out removes every copy of j's roll calls.
    - Replicate r's pick t is index floor(U("R-US17|boot|" + r + "|" + t) * M), with U the hash of item 7 of THE MODEL.
    - The interval is the 25th and 975th of the 999 sorted values.
    - A replicate with N_hat = 0 has an undefined precision. It sorts below every defined value.
    - Resampling measures carries both the fit's thinness and the scoring's (H2's "13 vetoable, 25 vetoes").
    - BOOT is approximate and runs narrow at small n. It is labelled so, and it never decides (THE DECISION decides on exact point figures), so its imperfection cannot steer a verdict.
- THE LABEL, exactly:
  - cell figures: "<value> FITTED n=<n> [<lo>, <hi>] 95% CP"
  - derived figures: "<value> FITTED n=<n> m=<m> [<lo>, <hi>] 95% BOOT"
  - Shares print as percent to one decimal, expected counts to two decimals, in invariant culture with "." as the decimal mark.
- TOO THIN:
  - A cell with n < N_MIN prints "UNFITTED n=<n> (fewer than 20 roll calls) - drawn from <fallback> FITTED n=<n'>", with no figure of its own.
  - A derived figure with m = 0 prints "UNFITTED m=0", with no figure.
  - An UNFITTED figure never counts in a decision (H4). The decision reads only the fitted fallback and FITTED totals.

======================================================================
SEEDS AND DETERMINISM
======================================================================
Three things are kept apart:
1. DECISION FIGURES HAVE NO SEED NOISE. Each pi_j is computed EXACTLY by enumerating every pair of list indices (n_F * n_A pairs per chamber, leave-one-out lists).
   - pi_j = pi_j(House) * pi_j(Senate).
   - Every total is a sum of exact expectations, which is the limit over infinitely many seeds. It can be implemented as count histograms and cumulative distributions; the result must equal the enumeration.
2. SEED NOISE IS SHOWN, NOT USED.
   - The exact Poisson-binomial distribution of the realized named count and the realized hit count (p_j = pi_j * A_j and pi_j * A_j * V_j) gives its 2.5% and 97.5% points: the smallest x with CDF >= 0.025, and with CDF >= 0.975. It is labelled "a single game's spread on the record (seed noise), not fit uncertainty".
   - Two illustrative realized runs use the keyed play draw on seeds 777 and 424242 (the trajectory suite's), keyed by each chamber's final date and title "<measure> (<congress>th)". They are labelled "one draw each; not a figure of the rule".
3. FIT UNCERTAINTY is the BOOT interval only.

- No random stream is created or advanced. SimulationRandom.Stream is not appended to.
- In play, the same act on the same day draws the same in the played world, a reload, the shadow and a fork that reaches it that day (Poland's premise).

======================================================================
THE FIRST RUN (what the backtest on the record measures and prints)
======================================================================
For every countable measure j:
- pi_j(House), pi_j(Senate) and pi_j;
- A_j = 1 iff the House final is a roll call and the decision president's party's House bloc stance is nay;
- V_j = 1 iff j is a countable veto;
- O_j = 1 iff the president's party's bloc stance is nay in either chamber's final roll call (US-10's VETOABLE);
- omega_j.

Totals, pooled and per president. Trump's second term has no countable veto and prints "no countable veto (H2)" where V_count = 0.
- N_hat = sum of A_j * pi_j: the model's vetoes, since (a) vetoes with certainty.
- T_hat = sum of A_j * V_j * pi_j: the record's vetoes reproduced.
- P_hat = T_hat / N_hat: precision.
- H_hat = T_hat / V_count: hit rate.
- W_hat = sum of O_j * pi_j: vetoable measures sent.
- sum of V_j * pi_j: the record's vetoes sent.
- O_hat.
Each prints with its FITTED label, its seed spread where it is a count, and B1's yardstick beside it.

Printed:
- the cells under each reading: n, q0, q05, q10, q20, the largest share, and the UNFITTED fallbacks;
- every measure with A_j = 1, or V_j = 1, or 0 < pi_j < 1: kind, procedure, both chambers' blocs, the margin and the crossers needed, pi_j(House), pi_j(Senate), pi_j, A_j, V_j, omega_j;
- every Senate independent left outside by a mixed caucus;
- every measure where F3's House count and (a)'s member count differ;
- COMPANIONS, recomputed and never typed: US-10's chamber-by-chamber bloc figures (vetoable measures sent; the record's vetoes a bloc Congress could send), with the two UNPREDICTABLE excluded (H2), beside W_hat and the sum of V_j * pi_j;
- the decision, the readings and the verdict line.

======================================================================
THE DECISION
======================================================================
STOPS, on the default's exact point figures:
- S0 (too thin): the all-pooled list has fewer than N_MIN roll calls. Nothing is fitted; no other stop is evaluated.
- S1 ("far more", R-US17 read by G5, PROVISIONAL): N_hat > 0 and 166 * T_hat < 45 * N_hat (precision below B1's 45 of 166). Where N_hat = 0, S1 is UNFITTED and S2 fires.
- S2 ("a bloc Congress never sends a vetoable bill", in a form that survives H1, PROVISIONAL):
  - fires iff 53 * T_hat < 45 * V_count, a hit rate below B1's 45 of 53;
  - the reason: R-US17 calls this stop "the mirror of E4's 43 of 43" (too few vetoes against too many). G5 reads its partner against B1's precision; the exact mirror reads this one against B1's other ruled figure, its hit rate. Both stops then rest on the one yardstick Elias accepted;
  - the literal form (W_hat < 1) is printed beside it. It is not the default, because under any draw with a non-zero tail it can practically never fire, so it would always pass (Question Q1).
- S4 (structure): under reading D2 (cells pooled per chamber, two cells), the set of {S0, S1, S2} that fires differs from the default's. The data cannot then choose the cell structure. S4 names D2. It is the only declared reading that can stop the rule.

WHAT S1 CAN SEE. On disk S1 has little power. The record holds only measures that passed both chambers, so a bill the model would send that failed in reality cannot be counted. S1 can fire only if the draw sends the named measures that were not vetoed far more readily than the vetoed ones. This is a limit of the data, declared (D13). "Far more" in play is printed at US-21's run.

THE OUTCOME of a run is the set of stops that fire. Its figures are not part of it.
- The empty set: INSTALL, meaning (c) as ruled by H1. The fitted catalog is the one US-21 reads, and the rates are reported to Elias before anything goes live (E4).
- Otherwise: STOPPED, with each stop listed with its figures, and R-US17 asked again.

PRINTED ONLY, never part of the outcome:
- the per-president figures, the BOOT intervals, the seed spread and illustrations, the override, the companions, every reading but D2;
- the "within the fit's uncertainty" marks: the verdict line adds "within the fit's uncertainty" after any S1 or S2 comparison whose BOOT interval contains its yardstick.

THE VERDICT LINE, generated and never typed:
- INSTALL: "By R-US17's rule (c) as ruled (H1), the draw is installed: precision <P_hat label> against B1's 45 of 166 (27.1 %); hit rate <H_hat label> against B1's 45 of 53 (84.9 %)[; within the fit's uncertainty on <figure>]; the same under the two-cell reading; vetoes the model names <N_hat label> against the record's <V_count>; H.R. 131 and H.R. 504 (119th) not counted (H2)."
- STOPPED: "R-US17's rule (c) does not install: <each stop with its figures> - asked."

THE STOPS' OPTIONS, each with "amend the rule as ruled" beside it:
- S0: pool everything; or source more roll calls (cloture and failed votes, BILLED).
- S1: (c) as measured; (c) with D8's drawn veto; or source the record's failed votes before ruling.
- S2: (c) as measured; the literal threshold; or source the record's failed and cloture votes.
- S4: four cells or two cells, put as conventions and never as "X with its verdict".

======================================================================
WHAT STAYS AS IT IS AND ITS PROOF
======================================================================
- BUDGET VOTES:
  - the draw class refuses every act of the US budget procedure, and returns no crossers outside CountryId.USA;
  - its check carries planted cases: a contested budget act at margin 1 draws zero; a Swedish, German, Polish, Italian and French division each draw zero;
  - US-22's diagnostic re-asserts this when the US budget procedure exists.
- THE OTHER FIVE COUNTRIES: the trajectory dump diff on seeds 777 and 424242 is byte-inert for SE, DE, PL, IT and FR. The US is inert too: nothing is wired before US-21.
- POLAND'S B1/F3 (H1: "proved unchanged on the simulation bar"), run on the simulation bar in the commit that lands the draw and catalog:
  - PresidentialVetoDiagnostic's cases and the D4 and E2 cases unchanged;
  - PresidentialVeto.cs, Generated.PolishVetoRates and VETO_B1_BACKTEST.md byte-unchanged.
- US-10's DOCUMENT: US_VETO_BACKTEST.md byte-unchanged, and UsVetoBacktestCheck green. The new instrument is a new tool, so the record of the s802 stop is never rewritten.

======================================================================
TRUMP'S 2025 VETOES
======================================================================
- H.R. 131 and H.R. 504 (119th) are outside EVERY count and denominator:
  - measures, V_count, N_hat, T_hat, W_hat, O_hat;
  - the companions, the per-president figures, the bootstrap population, the seed spread;
  - and their sustained override votes of 8 January 2026.
- They contribute nothing to the fit (they have no roll call).
- One line prints them: "UNPREDICTABLE (H2): H.R. 131, H.R. 504 (119th) - passed both chambers without a roll call; never counted for or against."

======================================================================
HOW THE FIGURES REACH THE GAME (US-21)
======================================================================
- The tool writes Assets/Scripts/Elections/Generated/UsDefectionRecord.cs, as Generated.PolishVetoRates is written. It holds:
  - each cell's sorted full-record list of (k, m, roll call identity), its n, and its FITTED or UNFITTED status with its fallback;
  - the CSV's digest, the block's digest and the decisive digest.
- It is re-read by the check, and a stale or edited catalog fails it.
- The runtime class Assets/Scripts/Elections/UsDefectorDraw.cs is pure:
  - Applies(USA) only;
  - Crossers(masterSeed, date, title, chamber, party, role, seats);
  - its own plant scope.
- US-21 wires it into the US division and the veto. A projection (ChamberVerdicts) reads the exact passage probability with its "FITTED n=..." label, never a decision.
- US-21 asserts R-US17 REPORTED, or RULED with a buildable option, at the current digests.
- US-21's run from 12 Mar 2024 to 20 Jan 2025 prints the bills reaching the president and those vetoed beside the record's 118th. Printed, never deciding.

======================================================================
DECLARED READINGS (default; the alternative printed beside it)
======================================================================
- D1. Cells by role (PP/OP). Alternative printed: by party label (R/D). Reason: the veto turns on the president's party, and labels mix Trump's terms with Biden's.
- D2. Four cells. Alternative: two cells pooled per chamber. This one DECIDES through S4. Reason: whether role matters cannot be assumed at this n.
- D3. Leave one measure out. Alternative printed: in-sample, labelled "in-sample". Reason: in-sample, a vetoed measure draws from its own crossing, a leak of one observation at thin n.
- D4. The empirical distribution. Alternative printed: a beta-binomial per cell.
  - Fitted by maximum likelihood over a grid, ln(alpha) and ln(beta) each in {-5.0, -4.9, ..., 5.0}.
  - Its maximum on the grid's edge prints UNFITTED.
  - Draw: p from Beta(alpha, beta), then c from Binomial(S, p), capped as in the model.
  - Exact expectations from its pmf.
- D5. Pooled over kinds. Alternative printed: split into the expedited kinds and "bill", each with its n and fallback, each measure drawing from its own kind. Reason: H1 says "the recorded votes", all of them. Play's bills are ordinary, so the "bill" cells print for US-21.
- D6. Trials are yea plus nay. Alternative printed: not voting counted as loyal.
- D7. The Vice President breaks a majority tie. Alternative printed: a tie fails (US-10's convention).
- D8. A certain veto where (a) fires. Alternative printed: a veto drawn at the default's P_hat (F3's form), the expected vetoes N_hat * P_hat.
- D9. On the record, no budget class (the extract cannot name one), so budget measures draw and are fitted like any other. Printed: every measure whose Senate final carried fewer than C yeas while the model requires cloture (reconciliation's misroute made visible).
- D10. House majority throughout (play has no suspension). Printed: the contested House finals whose house_question names a suspension, and the figures with two thirds on those.
- D11. In play, the drawn crossers stand in every contested division. Alternative printed: crossers recorded only where they change passage (O_hat under it).
- D12. The role in the fit is taken from the vote president. Alternative printed: from the decision president.
- D13. NOT MEASURABLE, declared:
  - the fit reads only measures that passed (selection on passage);
  - failed votes and cloture votes are not on disk, so the record cannot show false sends;
  - US-21's run is where "far more" is seen in play;
  - correlation across chambers and parties is not modelled;
  - an abstaining bloc's crossing has no record analogue and uses its role's cell.
- D14. The strict reading of uncertainty (S1 if the BOOT upper bound is below 45/166; S2 if the BOOT upper bound is below 45/53), and the lax one (using the lower bounds), are each printed with the outcome they would give. Neither decides, so the rule neither always asks nor always passes.

======================================================================
QUESTIONS FOR ELIAS
======================================================================
Q1. S2's threshold, R-US17's "never sends a vetoable bill" under H1. Default if unanswered: a hit rate below B1's 45 of 53, PROVISIONAL as G5 is. Alternative: the literal form, expected vetoable measures sent on the record below one. H1-H3 do not set it, and the literal form cannot fire under a draw.

Nothing else is asked. Every other choice above is a default with its reason.

======================================================================
THE RULING RECORD AND THE BARS
======================================================================
- THE BLOCK sits between "<!-- R-US17 RULE BEGIN -->" and "<!-- R-US17 RULE END -->", after H1-H3 in USA_STAGE_PLAN.md.
  - Its digest is SHA-256 over the UTF-8 bytes (no BOM) of the lines strictly between the markers, fences included, each ending LF with no CR. It is read from the index blob and required equal to the working copy's.
  - The instrument's RuleDigests list holds the preregistered digest (COMPLETED.md section <N>), taken before the first run, then one entry per amendment naming Elias's ruling ("AMENDED (Hn): old -> new").
  - A choice changed after a result is seen needs a new digest and is reported to Elias (G1).
- THE INSTRUMENT:
  - Tools/us_veto_fit.pl writes docs/generated/US_VETO_FIT.md and the catalog;
  - Assets/Editor/UsVetoFitCheck.cs runs in the cheap bar and the documents bar. It recomputes every figure, the intervals and the outcome from the CSV; holds the digest list and the ruling record; and checks the mirror line.
- THE DECISIVE DIGEST: SHA-256 over, under the default and under D2:
  - each cell's (chamber, role, n, status, fallback);
  - V_count and the count of countable measures;
  - the sorted identities of the measures with A_j = 1;
  - N_hat, T_hat and W_hat pooled, to 9 decimals;
  - the stops that fire.
  Nothing else.
- THE RULING RECORD:
  - NONE: every run fails the bars until its outcome is registered.
  - REPORTED: INSTALL, with the section that sent the rates to Elias (E4) and the decisive digest.
  - ASKED: a stop, with the ask's section, the outcome and the digest.
  - RULED: Elias's answer, with its option, the outcome it was made on and the digest.
  - The line after the block mirrors the state, section and ruling, never a digest; the check fails while they differ.
  - REPORTED and RULED pass while the outcome equals the registered one. A moved digest with the same outcome is re-pinned, naming the old digest, the new one and "outcome unchanged". A changed outcome is registered again and Elias is told.
  - ASKED passes while both the digest and the outcome equal the registered ones.
  - Buildable options: (c) with four cells; (c) with two cells; either with D8's drawn veto.
- THE MUTATION SUITE: Tools/us_veto_fit_mutations.sh. Each case must stop the tool or fail the check with its own message:
  - a third vetoed row with no roll call;
  - H.R. 131 given a roll call;
  - a bloc with seats and no voting member;
  - a catalog list edited;
  - the block edited without a listed digest;
  - the ruling line changed;
  - the control reproduces the document and the catalog byte for byte.
  The veto tool's own suite and its superseded-vote case (H3) are already done (s831).
- THE BARS: the cheap bar (UsVetoFitCheck, UsVetoBacktestCheck unchanged); the documents bar; the simulation bar (Poland's proof and the five countries inert), all in the landing commit.

======================================================================
PANEL'S BLINDNESS STATEMENT
======================================================================
- I computed no defection count or rate, fitted parameter, interval or model result.
- I ran no tool and no Unity process, and edited nothing.
- I did not open us_veto_measures.csv or any roll call XML. The only file I read under G:\Claude\tmp was the brief itself, G:\Claude\tmp\claude\C--Users-elias\36780c75-eec4-4fba-9c32-e50d358cf646\scratchpad\s832\facts.md.
- I read the plan, veto_record.md, PresidentialVeto.cs, SimulationRandom.cs, CountryId.cs, UsVetoBacktestCheck.cs (in part), us_veto_backtest.pl, us_veto_prep.pl (by search) and both generated documents, as the brief allows.
- G:\UNITY\Projects\PoliSim\docs\generated\US_VETO_BACKTEST.md prints each veto's party splits and US-10's rule results. I saw them, and no choice above uses or derives from them.
```

## Appendix D. The designer's specification - lens: precedent

```text
R-US17 RULE (c) - SPECIFICATION FROM THE PRECEDENTS-AND-RECORD DESIGNER (blind panel, s832)

Plain ASCII. It is written to be condensed into one fenced block between `<!-- R-US17 RULE BEGIN -->` and `<!-- R-US17 RULE END -->` in docs/specs/USA_STAGE_PLAN.md, and digested in the same way as R-US18 and R-US19. Section numbers appear as "section NNN". The provenance note at the end is not part of the rule.

======================================================================
THE TERMS
======================================================================

- THE RECORD. ElectionsData/usa/us_veto_measures.csv as Tools/us_veto_prep.pl writes it. It is read as Tools/us_veto_backtest.pl reads it: lines starting "#" are skipped, the header is taken by name, and every row must have the header's field count. Its SHA-256 over the raw bytes is printed and goes into the stamp. Nothing else is read for the fit. The member-level XML is never read by the rule: the extract's party splits are already held to those member votes by the prep tool's party-sum check.

- B1'S YARDSTICK is 45 named of 166, recomputed from ElectionsData/poland/third_readings_term10.csv exactly as Tools/us_veto_backtest.pl recomputes it. If the result is anything other than 45 of 166, the instrument fails as a defect.

- VETOED: an outcome containing "veto" (vetoed, pocket vetoed, vetoed and overridden). NOT VETOED: signed, or law without signature.

- UNPREDICTABLE (H2): a vetoed row in which neither chamber's final agreement is by roll call. On the extract this rule is preregistered on, the instrument asserts that these are exactly H.R. 131 and H.R. 504 of the 119th Congress. A later row in this class is printed by name and treated the same way. It moves the decisive digest, which the ruling record then handles (THE RULING RECORD AND THE BARS).

- COUNTED MEASURES: every row that is not UNPREDICTABLE. Every count, sum, denominator, cell, rate and digest of this rule reads counted measures only. COUNTED VETOES: the vetoed counted measures.

- PRESIDENT and PP (the president's party): the row's `president` and `president_party` columns (reading 2 of veto_record.md). OP: whichever of R and D is not the PP. A president of no party has no PP. In play he has no veto path: no draw is made and (a) never fires. This mirrors Poland's G5 item 15, PROVISIONAL.

- A CHAMBER'S ROLL CALL: `house_final` or `senate_final` begins with "roll ". Any other final agreement (voice vote, unanimous consent, or no recorded vote under a rule's adoption, reading 9) is "no roll call".

- A LABEL'S SPLIT on a roll call: its yea, nay, present and not_voting columns, where the label is R, D or I. MEMBERS = all four summed. VOTING = yea + nay + present. Not-voting members are outside VOTING.

- BLOC STANCE of R or D in a chamber (US-10's bloc rule, Tools/us_veto_backtest.pl `bloc_passes`): YEA if the label's yea > nay + present, otherwise NAY. It is computed from the label's own counts. A tie (yea = nay + present) is NAY. A present answer stands against, as in F3's "an abstention against".

- INDEPENDENTS:
  - A Senate independent joins the bloc of the caucus that senate_I_caucus names. This applies to seats only, as in US-10's bloc reading.
  - A Senate independent is in no label's split, stance or defector count (veto_record.md reading 6: "a senator caucusing with it is in no count of it").
  - A House independent is in no bloc. Under reading (A) he is in no count. Under the default (B) he sits inside `sworn` and counts against. Both are US-10's own two readings, inherited unchanged.

- SEATS OF A CONGRESS (used by the default premise (B)): exactly US-10's %seatsOf.
  - For each Congress and chamber, take the counted-or-not row whose final agreement in that chamber is the latest by roll call (`<ch>_final_date`, the later winning, with US-10's tie handling).
  - R and D are that roll call's members by label. In the Senate, independents are added to their caucus.
  - sworn = R + D + I members.
  - US-10's code computes these from every row. The rule reuses them unchanged, so its identity with US-10 holds.

- KINDS: the `kind` column. EXPEDITED kinds:
  - CRA disapproval (5 U.S.C. 802);
  - War Powers and arms-sale disapproval (P.L. 94-329 s601(b));
  - national emergency (50 U.S.C. 1622);
  - D.C. disapproval (D.C. Code s1-206.04).

  Each passes the Senate by a majority, with no cloture. Every other kind ("bill") is under Rule XXII cloture: three fifths of the senators duly chosen and sworn, ceil(3W/5) = floor((3W + 4)/5). Reconciliation is not read off the record: US-10's kinds do not distinguish it. A counted measure on the veto path whose title begins "Providing for reconciliation" or contains "pursuant to section 2002 of the concurrent resolution" is listed by name and scored under cloture as US-10 scores it.

- THE VETO PATH, V: the counted measures with at least one roll call on which the PP's bloc stance is NAY. The instrument prints |V| beside US-10's F3 count (34) and lists every measure in one and not the other. They can differ only at an exact tie yea = nay + present.

- SENT: both chambers pass under the model (THE MODEL). NAMED: sent, and rule (a) fires on the modelled House division (THE VETO).

- THE CELLS (THE FIT): HP, HO, SP and SO. The first letter is the House or the Senate. P means the president's party standing NAY. O means the other party standing YEA on the same roll calls.

- N_MIN = 12. A cell holding fewer than 12 roll calls is UNFITTED. Reason, which uses no data: from n = 12 the 95% Wilson interval of a share at p = 1/2 is no wider than 0.5 (0.492 at n = 12, 0.509 at n = 11). Below that, a cell cannot tell a rare defection from an even split.

- Every comparison in THE DECISION is made exactly on rationals (BigInteger numerators and denominators), with no noise band. Equal is never "below".

======================================================================
THE MODEL (what is drawn, from what, per what)
======================================================================

- Parties stay blocs by default (H1). A party votes as one: its bloc carries every seat it holds in the chamber, as US-10's bloc carries every member the roll call lists, not-voting members included. "Present" is not modelled: every bloc seat votes yea or nay.

- THE DRAW (H1: "a seeded draw of defectors per party per chamber"). It is made only for a bill on the veto path, meaning the PP's stance is AGAINST (play) or the measure is in V (record). It is made once per party, per chamber, per bill, and only for a party that has a cell for its stance:
  - the PP standing NAY draws from HP (House) or SP (Senate);
  - the OP standing YEA draws from HO or SO.

  No draw is made for:
  - an OP standing NAY;
  - a bloc that is UNDECIDED in play;
  - any third or created party;
  - an UNFITTED cell (K = 0 there: the bloc default);
  - any bill off the veto path;
  - any budget act;
  - any reconciliation bill in play.

- WHAT IS DRAWN. An index j is drawn uniformly over the cell's observations, in the fit's order (THE FIT). The observation is (d_j, m_j): the defectors and voting members of that party on that recorded roll call. The party's drawn defector count is

      K = floor((2 * d_j * S + m_j) / (2 * m_j))

  that is, round-half-up of S * d_j / m_j in exact integers, where S is the party's bloc seats in that chamber on that bill. The K defectors move to the other side: PP defectors from nay to yea, OP defectors from yea to nay. Draws are independent across parties and chambers.

- ONE DRAW SERVES EVERY VOTE OF THAT CHAMBER ON THAT BILL: Senate cloture and passage alike, and the override (THE OVERRIDE). Two bills with one title, voted in one chamber on one day, draw alike. This is Poland's premise: "the same text the same way that day".

- WHAT IS SHOWN IN PLAY. The drawn defectors enter a chamber's division only where they change its outcome from the bloc outcome (WHEN THE DRAW MATTERS). Everywhere else the division is the bloc's, byte for byte. Reason: H1's "the draw matters only where the chamber's margin is within the drawn count", and US-18's divisions, plates and pass rates stay unchanged wherever no outcome moves.

- THE PREMISE OF THE BACKTEST (default (B), one stance). Play holds one stance per party per bill in both chambers (US-18; the plan's spec-risk 4). On the record:
  - For a measure in V, the PP's one stance is NAY in both chambers.
  - The OP's one stance is its recorded bloc stance in the House if the PP's House stance is NAY, otherwise in the Senate.
  - Seats are the Congress's SEATS OF A CONGRESS.
  - Draws are made in BOTH chambers, whether or not a chamber has a roll call.
  - Off V, every measure goes as US-10's chamber-by-chamber bloc reading sends it: no draw, never named.

  Reading (A), chamber by chamber as recorded (s802's declared reading), is printed beside (DECLARED READINGS).

- "Per party" is read as one draw for each party, with its parameters chosen by the party's role (PP or OP) and its stance. They are not chosen by its label. This is declared under Departures.

======================================================================
THE FIT (data, members counted, measures, defector definition, estimator)
======================================================================

- DATA. H1's "recorded chamber-by-chamber votes since 2017" are the final agreements by roll call in the record. They are the only roll calls on disk. No cloture vote, no failed measure and no override party split is saved. The fit reads nothing else.

- SELECTION, DECLARED. The record holds only measures that passed both chambers. The fit therefore sees defection on measures that passed, never a defection that failed to carry a measure. Its rates are those of passed measures. Reading (vi) prints the fit without the roll calls whose passage needed the president's party's defectors.

- THE ROLL CALLS FITTED, per chamber: every roll call of a counted measure on which the PP's bloc stance is NAY. Each gives:
  - a PP observation: d = PP yea, m = PP voting (yea + nay + present);
  - an OP observation if the OP's bloc stance on it is YEA: d = OP nay + OP present, m = OP voting. If the OP also stands NAY, there is no OP observation, and the roll call is listed.

  Not-voting members are outside m. Independents are in no observation. An observation with m = 0 is excluded and listed. By the stance rule every share satisfies d/m <= 1/2; this is asserted, and a failure is a defect.

- THE CELLS:
  - HP = the House PP observations;
  - HO = the House OP observations;
  - SP = the Senate PP observations;
  - SO = the Senate OP observations.

  Each is pooled over the three terms, the five Congresses and every kind. No split is chosen by a figure; the splits are fixed here. Reason for pooling: the fit is thin (H2), and the game's presidents and bills are not the record's. Per-kind and per-president splits are printed as readings (iii) and (iv).

- FIT ORDER: within a cell, by congress ascending, then by the measure string in ordinal order. A measure has at most one final roll call per chamber, so the order is total.

- THE ESTIMATOR. The empirical distribution of the defector share d/m over the cell's observations, which is the nonparametric maximum-likelihood estimate of the share's distribution. It has no parameter and no optimisation.

  Reason: a binomial at a pooled rate understates the record's clustering of defections, and a beta-binomial adds a two-parameter numerical fit with boundary exits that cells this small cannot support. The empirical draw cannot produce a share the record never showed, and every figure derived from it carries its n directly. The binomial at the cell's pooled rate is printed as reading (ix).

- UNFITTED: a cell with n < N_MIN on the full cell. Its K is 0 (the bloc default, H1's "parties stay blocs by default"). The fitted/UNFITTED state is decided on the full cell only, never on a leave-one-out pool or a bootstrap replicate.

- LEAVE-ONE-OUT in the backtest. A measure's own draws read each fitted cell less that measure's own observation. A fitted cell whose pool is then empty gives K = 0, and this is printed. Play uses the full cells. In-sample is printed as reading (v).

======================================================================
WHEN THE DRAW MATTERS (the margin test per chamber and procedure)
======================================================================

Notation:
- Y0 and N0 are the bloc yeas and nays with every K = 0.
- Kp is the PP's drawn count, Ko is the OP's drawn count, and D = Kp - Ko is the net crossing toward yea.
- Y = Y0 + D and N = N0 - D.
- W is the base the chamber's rule reads.

The outcome with the draw is computed exactly. The draw MATTERS where it differs from the bloc outcome, which happens only where D reaches the margin: "the chamber's margin is within the drawn count".

- HOUSE, passage (a majority of those voting).
  - (B): passes iff 2Y > W_H, where W_H = sworn (independents inside, counting against), Y = S_OP - Ko + Kp.
  - (A): passes iff Y > N, over the R and D blocs as recorded, with no independents.
  - A tie fails.
  - Margin for a failing bloc result: the least D that passes.

- SENATE, an EXPEDITED kind (5 U.S.C. 802(d); P.L. 94-329 s601(b)(4)(B); 50 U.S.C. 1622(c); D.C. Code s1-206.04(h)): a majority of those voting, no cloture.
  - (B): passes iff 2Y > W_S, where W_S = sworn with independents in their caucus.
  - (A): passes iff Y > N.
  - THE VICE PRESIDENT breaks a tie with the PP's stance. Every Vice President of the window shares the president's party, and on the veto path that stance is NAY, so a tie fails. In play the Vice President of the day votes his president's party's stance; with the office vacant, a tie fails. This equals US-10's "a tie is not broken", so the identity holds.

- SENATE, under RULE XXII (every other kind): cloture needs Y >= floor((3W + 4)/5), where W = the senators duly chosen and sworn.
  - (B): W is SEATS OF A CONGRESS sworn.
  - (A): W is the roll call's members, all labels.
  - In play, W is the sworn count on the day (US-18).
  - Passage follows by a majority, the Vice President as above. Cloture binds, because Y >= 3W/5 implies a majority.
  - The same draw serves cloture and passage.

- A CHAMBER WITH NO ROLL CALL.
  - (A): passes (US-10: "unanimous under blocs"), with no draw.
  - (B), for a measure in V: modelled like any chamber, on SEATS OF A CONGRESS, with the draw.
  - Off V: passes, with no draw.

- SAME-DAY ACTIONS. The rule reads each chamber's final agreement as the extract chose it (reading 4); `same_day` is printed only. The two chambers' draws are keyed apart (SEEDS AND DETERMINISM), so a same-day House and Senate vote draw independently.

- VACANCIES enter only through the bases: members listed, or sworn. A vacant seat is in no bloc and no base.

- BUDGET AND RECONCILIATION in play: no draw (WHAT STAYS AS IT IS).

======================================================================
THE VETO (rule (a) on a bill in play)
======================================================================

- H1: "Rule (a) then decides the veto on a bill that reaches the president against his party." The rule is R-US17 (a), "a bill a majority of the president's party's House members voted against", read on the MODELLED House division. It is deterministic: (a) fires and the president vetoes, or (a) does not fire and he signs. "Decides" is read as no further draw.

- (B): with S = the PP's House seats (SEATS OF A CONGRESS; in play, its House seats on the day), (a) fires iff 2 * (S - Kp_H) > S. It never fires with S = 0: no members means no majority, which is B1's "a backing party with no seat" reading.

- (A): (a) fires iff the House has a roll call, the PP's House stance is NAY, and 2 * (M - Kp_H) > M, where M = the PP's House members listed. It does not fire otherwise.

- Off the veto path, (a) can never fire. A majority of members voting nay implies the stance is NAY, so (a) is contained in V's House part. This is asserted.

- "More than half" is strict, as in B1. At exactly half, (a) does not fire.

- A player-president chooses, and (a) is not read (US-21). The Congress's draw is the same whoever is president.

- A bill not against his party is signed. This includes a bill the PP stands UNDECIDED on in play, which has no draw and gets no (a), because its nays are zero.

======================================================================
THE OVERRIDE
======================================================================

- THE BASE. Two thirds of those present and voting, a quorum present. CRS RS22654: "two-thirds of the Members voting, a quorum being present". The quorum is a majority of the chamber's members duly chosen and sworn.

- In the model every bloc seat votes, so the quorum is always present. The override needs Y >= ceil(2(Y + N)/3), computed as floor((2(Y + N) + 2)/3), in the House and then the Senate. It carries only if both carry.

- THE SIDES. The re-pass is voted by the passage's sides with the passage's drawn defectors. There is no fresh draw. This is US-21's declared premise ("the same sides"), with the draw carried.

  Reason: H1's draw is per bill per chamber, and no fresh draw can be fitted. The extract holds override yeas and nays only, with no party split, so no override cell can exist. Departure declared.

- PRINTED ONLY, never in THE DECISION:
  - for each measure named, P(the override carries | named);
  - the expected overrides;
  - every recorded override vote beside the model's: the one override of record, H.R. 6395 (116th), and every sustained one, with H.R. 131 and H.R. 504's sustained votes printed but not counted;
  - US-21's sentence "under it no Congress of record overrides", measured again with the draw and printed.

======================================================================
THE PRINT (H2)
======================================================================

- A FITTED FIGURE is any number computed from the cells' observations: every cell figure, every per-measure probability, and every expected count, precision and hit rate the draw produces, under the default and under every reading.

  The following are NOT fitted and carry no label:
  - counts of the record (measures, vetoes, roll calls, n);
  - US-10's companions;
  - B1's yardstick;
  - the identities computed with every K = 0.

- ITS n is the number of distinct recorded roll calls (chamber, roll, date) in the FITTED cells the figure reads, after leave-one-out where it applies. Aggregates over measures read the full cells. The cell table prints each cell's own n.

- THE INTERVAL. A 95% percentile cluster bootstrap, B = 2000 replicates.
  - Each replicate resamples, with replacement, each chamber's fitted roll calls: n_c draws from the n_c. A roll call's PP and OP observations stay together.
  - The figure is recomputed on the replicate, with leave-one-out removing every copy of the measure's own roll call.
  - The interval runs from the 50th to the 1951st of the 2000 sorted replicate values.
  - Bootstrap arithmetic is in doubles and is printed only. Decision figures are exact.
  - A degenerate replicate distribution prints [v, v].

- THE LABEL, exactly:

      <value> FITTED n=<N> 95% [<lo>, <hi>]

  - Probabilities and rates print in percent to one decimal, as in US-10's document ("41.3 %").
  - Expected counts print to two decimals.
  - A figure reading an UNFITTED cell adds "(cell <XX> UNFITTED: blocs)".

- TOO THIN. An UNFITTED cell prints `UNFITTED n=<n>`, with no figure and no interval (H4's form). Its K = 0 is the bloc default, not a fitted figure. Figures computed with it count in the decision as bloc figures, which is declared, because H4 bars only the unfitted figure itself.

- EACH CELL PRINTS:
  - n;
  - every observation (congress, measure, roll, d, m), which is the record;
  - and, FITTED, the share of observations with d >= 1, the mean share, and the lower median share (the ceil(n/2)-th smallest).

======================================================================
SEEDS AND DETERMINISM
======================================================================

- THE UNIT FUNCTION. Unit(key) = FNV-1a 64 over the key's UTF-8 bytes, finished with MurmurHash3's fmix64. The fraction is (h >> 11) / 2^53. This is PresidentialVeto.Draw's arithmetic, in a US helper of its own. PresidentialVeto.cs is not edited. The instrument asserts Unit(key) equals PresidentialVeto.Draw(...) on three fixed Polish keys. No SimulationRandom stream is created, drawn or moved.

- PLAY. The key is

      masterSeed|Usa|<H or S>|<yyyy-MM-dd of that chamber's vote>|<bill title>|<party abbreviation>

  with j = floor(Unit(key) * n). A planted-index scope, as in PresidentialVeto.PlantDraw, serves diagnostics and films.

- THE RECORD BACKTEST'S DECISION FIGURES are EXACT: an enumeration over the cells' observations with rational arithmetic. No seed decides anything.

- THE BOOTSTRAP. The resample index is floor(Unit("R-US17|bootstrap|<b>|<H or S>|<t>") * n_c), for b = 1..2000 and t = 1..n_c. Fixed, with no master seed.

- SEEDED REALIZATIONS, printed only.
  - The play draw runs over the record for master seeds 1..1000.
  - The key's date is `<ch>_final_date`, or `decision_date` where empty. The title is the CSV's `title`. Leave-one-out pools are used as in the exact run.
  - Printed: per president and pooled, the mean, the 50th and the 951st of the 1000 sorted counts of measures sent on V, named, and named-and-vetoed.
  - Asserted (a failure is a defect, not a stop): each seeded mean lies within 4 * sd / sqrt(1000) of the exact expectation, or equals it where sd = 0.

======================================================================
THE FIRST RUN (what the backtest on the record measures and prints)
======================================================================

The first run is the instrument's first measurement, after the block's digest is pinned (H3). It runs on the defaults as printed. No choice is asked blind: H1-H3 rule the option, and E4 carries every figure to Elias.

It measures and prints, in E4's form (VETO_B1_BACKTEST.md's):

1. THE RECORD.
   - Measures and vetoes per president: counted, and in all (1,564 and 25).
   - The two UNPREDICTABLE measures in their own line.
   - |V| beside F3's 34, with the differences.

2. THE CELLS (THE PRINT).

3. PER MEASURE OF V, ordered by congress and measure:
   - kind, president, recorded outcome;
   - each chamber's bloc margin under (B), and whether a roll call exists;
   - the cells read;
   - P(sent) and P(named), each FITTED, with the probability the draw matters in each chamber.

4. THE CONFUSION MATRIX IN EXPECTATION, per president and pooled.
   - Named: E[named] = the sum of P(named) over V.
   - Vetoed among the named: E[hits] = the sum over counted vetoes.
   - Named, not vetoed.
   - Vetoes outside: 23 - E[hits] pooled.
   - Hit rate = E[hits] / counted vetoes. Precision = E[hits] / E[named].
   - E[vetoable sent] = the sum of P(sent) over V.
   - Every figure FITTED, and "-" where a denominator is 0.

5. EVERY COUNTED VETO with P(named), FITTED. H.R. 6395 is listed as off V (its PP stood YEA), so it can never be named. Every measure named with P > 0 and not vetoed is listed.

6. THE OVERRIDE (THE OVERRIDE).

7. THE SEEDED REALIZATIONS.

8. COMPANIONS, not fitted, reprinted from docs/generated/US_VETO_BACKTEST.md with its CSV digest:
   - (a) 22 of 33 (66.7 %);
   - both chambers 20 of 21;
   - the one-stance bloc 0 of 34;
   - chamber by chamber 13 vetoable and 5 of the 25 vetoed;
   - all labelled "US-10's, all 25 vetoes".

9. THE IDENTITIES. Asserted; a failure is a defect.
   - With every K = 0, (B) sends exactly US-10's one-stance count (0).
   - With every K = 0, (A) reproduces US-10's chamber-by-chamber counts (1,512 sent, 13 vetoable, 5 of 25), computed over all 1,564 rows for this check alone.
   - (a) on the recorded House votes gives US-10's 33 named and 22 vetoed.
   - Every share is at most 1/2.
   - Exactly the two named measures are UNPREDICTABLE.
   - B1's yardstick is 45 of 166.

10. THE DECLARED READINGS, each with its outcome and its headline figures, labelled.

11. THE DECISION, THE VERDICT LINE AND THE DECISIVE DIGEST.

======================================================================
THE DECISION (the outcome of a run defined once)
======================================================================

All figures are under the default (B), exact, over counted measures.

- S2, NEVER VETOES (R-US17's second STOP, surviving H1). Fires iff E[named] = 0. R-US17's words, "a party-bloc Congress under Rule XXII never sends a vetoable bill", are its first case: E[vetoable sent] = 0. Its second case is "sends some but names none". Both are the plan's spec-risk 4 risk, "zero vetoes - the mirror of E4's 43 of 43". The verdict names which case.

- S1, FAR MORE (R-US17's first STOP, read as G5, PROVISIONAL). Evaluated only where E[named] > 0. Fires iff 166 * E[hits] < 45 * E[named], that is, precision below B1's 45 of 166. With one rule left, "every rule" is that rule. S1 is also R-US17's original recommendation test, "(a) only if its precision ... is at least B1's", and the two coincide.

- THE OUTCOME of a run is exactly one of:
  - INSTALLS: neither stop fires;
  - S1;
  - S2.

  The two stops exclude each other. Figures are not part of the outcome. The rule reaches (c) as ruled, or an ask, and never anything else by itself.

- PRINTED ONLY, never deciding:
  - every interval, and where B1's 27.1 % lies against the precision's interval (below, inside or above);
  - the readings, and the outcome under each;
  - reading (viii)'s alternative S2 threshold;
  - (A);
  - per-president figures;
  - the override;
  - the seeded realizations;
  - the companions.

- THE VERDICT LINE is generated, never typed:
  - INSTALLS: "By R-US17's rule (c) as ruled (H1-H3), the draw installs: a bloc Congress with the draw sends <E[vetoable sent]> vetoable measures of the record FITTED ..., names <E[named]> FITTED ..., <E[hits]> of them vetoed FITTED ...; precision <p> FITTED n=.. 95% [..] against B1's 27.1 % (45 of 166); hit rate <h> of the 23 counted vetoes FITTED ...; Trump's two 2025 vetoes declared unpredictable, in no count; <the same under every declared reading | the outcome differs under reading (x): <outcome>; ...> - the rates go to Elias before US-21 (E4)."
  - S1: "R-US17's rule (c) does not install: S1 - precision <p> FITTED ... below B1's 27.1 % (45 of 166) <readings as above> - asked."
  - S2: "R-US17's rule (c) does not install: S2 - a bloc Congress with the draw <sends no vetoable measure of the record | sends <x> FITTED ... but names none> - the mirror of E4's 43 of 43 <readings> - asked."

- WHATEVER THE OUTCOME, R-US17 goes to Elias with the full document (E4).

- THE STOPS' OPTIONS, each with "amend the rule as ruled" beside:
  - S1:
    - (c) as measured, installed with its precision declared;
    - (c) under a named declared reading in place of its default, by amendment;
    - the plan's bloc premise with no draw (an AI president never vetoes; US-21 builds the plate for a player-president).
  - S2:
    - the bloc premise with no draw, as S1's last option;
    - reading (A)'s chamber-by-chamber stance as play's premise (amends US-18's one-stance premise, US-18 re-pinned);
    - a draw fitted otherwise, by amendment, specified before its run.

======================================================================
WHAT STAYS AS IT IS AND ITS PROOF
======================================================================

- US-10's TOOLS, EXTRACT AND DOCUMENT.
  - Tools/us_veto_prep.pl, us_veto_measures.csv (digest 505c3fa1...), us_veto_backtest.pl and docs/generated/US_VETO_BACKTEST.md are byte-identical.
  - UsVetoBacktestCheck is unchanged and passing.
  - The mutation suite stays 13 of 13 (s831). It is re-run only if a commit touches the prep tool.

- BUDGET VOTES. The draw never applies to a budget act, a reconciliation bill or the US budget procedure (WorldClock.BudgetProcedureOf, US-22). At US-21, a diagnostic counts draws:
  - zero on every budget and reconciliation bill, and on every bill off the veto path;
  - US-18's pass-rate sweep re-run, unchanged in every row but the veto-path rows whose outcome a draw flipped, each listed.

  At the first run nothing in Assets/Scripts changes.

- THE OTHER FIVE COUNTRIES (SE, DE, PL, IT, FR). The draw's only entry point is CountryId.Usa, and no SimulationRandom stream is touched. Proof: the simulation bar's trajectory sentinel holds byte-identical against its pinned fingerprints on both seeds.

- POLAND'S B1 AND F3 (H1: "proved unchanged on the simulation bar").
  - PresidentialVeto.cs, Generated/PolishVetoRates.cs and docs/generated/VETO_B1_BACKTEST.md are byte-identical: the review row shows an empty git diff over the three paths.
  - PresidentialVetoDiagnostic's cases and the D4/E2 cases pass unchanged.
  - The simulation bar passes.

  Required at the commit that first runs the rule and again at US-21's.

- R-US18's and R-US19's block digests are unchanged (5c89b0cf..., 1caec656...).

======================================================================
TRUMP'S 2025 VETOES
======================================================================

H.R. 131 and H.R. 504 (119th) passed both chambers without a roll call: House voice vote, and Senate voice vote or unanimous consent. Both were vetoed and both were sustained in the House on 8 January 2026.

Under H2 they are UNPREDICTABLE, in no count and no denominator:
- not in the fit (they have no final roll call, and override roll calls are never fitted);
- not in V;
- not in the confusion matrix, hit rate, precision, expected counts or seeded realizations;
- not in the override's counts (their sustained votes are printed only);
- not in the per-president rows (Trump's second term: 0 vetoes counted, "2 declared unpredictable");
- in the decisive digest by name only.

Counted vetoes: 23 (Trump's first term 10, Biden 13). US-10's companions keep their 25 and are labelled as US-10's.

======================================================================
HOW THE FIGURES REACH THE GAME (US-21)
======================================================================

- THE CATALOG. Assets/Scripts/Elections/Generated/UsVetoDefections.cs (namespace PoliSim.Elections.Generated) holds:
  - SourceDigest: the CSV's SHA-256;
  - RuleDigest: the block's current digest;
  - DecisiveDigest: the digest the ruling was made on;
  - MinimumRollCalls = 12;
  - for each of HP, HO, SP and SO: Fitted and its observations (Defectors, Voting) in fit order, the full cells.

  It is written by the instrument at US-21's commit, from the run the ruling record holds, and only while the record reads RULED ... INSTALLS. Nothing reaches Assets/Scripts before US-21. The cheap bar regenerates it in memory and fails on any byte difference, on Poland's Generated.PolishVetoRates pattern and the generated-catalog check's.

- THE RUNTIME. A US class beside PresidentialVeto, whose Poland paths are unchanged (US-20: "PresidentialVeto holds its rule per country"). It holds:
  - the draw (Unit, the key, the K formula, the planted scope);
  - the margin tests above;
  - (a);
  - the override on the same sides.

- US-21'S VETO DIAGNOSTIC asserts:
  - the ruling record RULED with INSTALLS (or a ruled buildable option);
  - the block digest the list's last;
  - the catalog's SourceDigest and DecisiveDigest current;
  - for every counted veto in V, that enumerating the runtime's planted indices reproduces the instrument's exact P(sent) and P(named) under (B), as the same rationals, on full cells with the measure's leave-one-out pool supplied.

- THE GAME prints no fitted probability unless it carries `FITTED n=<n> 95% [..]`. By default it prints none: the plate shows the votes cast.

======================================================================
DECLARED READINGS (contestable defaults; the alternative printed beside each)
======================================================================

Each reading runs the whole rule again with one change, and prints its outcome and its headline figures (E[vetoable sent], E[named], E[hits], precision, hit rate), all labelled. A reading whose outcome differs is named in the verdict line. No reading ever stops the rule. This is a departure from R-US18's and R-US19's S4: H1 already ruled the option, and E4 carries every reading to Elias.

- (i) Chamber by chamber as recorded (A) in place of one stance (B): s802's declared reading, US-10's chamber-by-chamber bloc with the draw.

- (ii) The fit over every contested roll call (R and D bloc stances opposite). Each party's observation is taken against its own stance, with cells by chamber and role (PP or OP), pooled over stances. Applied to the same veto-path draws.

- (iii) Cells split by path: EXPEDITED kinds apart from Rule XXII kinds, in both chambers (eight cells). Play's bills are all Rule XXII bills, so the default carries the expedited disapprovals' defection into play's cloture bills.

- (iv) Cells split by president (three terms). The PP's label varies with the president.

- (v) In-sample: no leave-one-out.

- (vi) Selection removed. The PP cells keep only roll calls whose chamber's recorded bloc result (US-10's rule, every K = 0) already passed. The OP cells are unchanged.

- (vii) A joint draw: one index per chamber over that chamber's fitted roll calls gives both parties' shares. K_OP = 0 where that roll call has no OP observation.

- (viii) S2 read as "fewer than one veto named in expectation over the record" (E[named] < 1), in place of zero.

- (ix) The binomial: K ~ Binomial(S, p), with p = sum d / sum m over the cell. Doubles, printed only.

SETTLED, NOT READINGS:
- presence counted with the nays (US-10's rule, F3);
- independents (US-10's two readings);
- the Vice President (equal to US-10 on the veto path);
- the override's sides (no fit possible);
- the unpredictable class (equal to H2's two on today's extract).

DEPARTURES FROM THE RULINGS' WORDS, declared:

1. H1 "per party": one draw per party, with parameters by role and stance, not by label. Reading (iv) prints the label split.

2. H1 "fitted on the recorded chamber-by-chamber votes since 2017": only final agreements are on disk. The fit is selected on passage (reading (vi)).

3. H1 "the draw matters only where the margin is within the drawn count": the drawn count is always computed, and it enters a division only where it changes the outcome.

4. H1 "Rule (a) then decides": deterministic, on the modelled House division, not the recorded one.

5. H1 "the third option, (c), on the USA's veto path only": read as the draw applying only to bills the president's party stands against (QUESTION 1).

6. H2: n is distinct roll calls, the interval is a percentile cluster bootstrap, and a cell below 12 roll calls is UNFITTED.

7. R-US17's STOPs:
   - "every rule names far more bills" applies to the one ruled rule, as G5's pooled precision;
   - "never sends a vetoable bill" is widened to "never vetoes" (E[named] = 0), its literal case printed by name.

8. US-21's override premise "the same sides" now carries the drawn defectors.

9. The precedents' S4: the readings print and never stop.

10. THE INSTRUMENT. A new Editor check, UsVetoDrawCheck, in the cheap and docs groups. It writes docs/generated/US_VETO_DRAW.md, which carries the CSV's digest and the block's digest in its stamp. It does not extend the Perl tool. Reason: play's Unit function, BigInteger and the R-US18/R-US19 digest machinery are C#, and US-10's document stays byte-identical as proof. US_VETO_BACKTEST.md is never edited.

11. US-10's 34 and 13 use F3's count and the bloc rule respectively. This rule uses the bloc rule throughout and prints the difference.

======================================================================
QUESTIONS FOR ELIAS (each with its default if unanswered)
======================================================================

1. "On the USA's veto path only". Does H1's draw apply only to bills the president's party stands against (the veto path, so US-18's blocs are unchanged on every other bill), or to every ordinary US bill's votes (budget excepted)? The second would let contested bills the president backs pass cloture on drawn crossings, and would move US-18's pass-rate sweep.

   DEFAULT: the veto path only. Reason: "Parties stay blocs by default", and s802 put a general defection change "with the party model".

   This does not change the first run: no bill off the veto path is ever named. It binds US-21. Under the default, the instrument also prints the cells the broad reading would use (every contested roll call by role and stance, reading (ii)'s observations split by stance), so an answer needs no new fit.

No other question. H1-H3 with G1, G5, E4 and H4 settle everything else above. A choice changed after the first run is an amendment in sight of the figures, reported to Elias (G1).

======================================================================
THE RULING RECORD AND THE BARS
======================================================================

- THE BLOCK AND ITS DIGESTS.
  - The block sits between `<!-- R-US17 RULE BEGIN -->` and `<!-- R-US17 RULE END -->`, plain ASCII, inside a ```text fence.
  - Its digest is SHA-256 over the UTF-8 bytes, no BOM, of the lines strictly between the marker lines (the fence lines included), each ending LF, no CR. It is read from the index blob's raw bytes and must equal the working copy's.
  - UsVetoDrawCheck holds RuleDigests: the preregistered digest (COMPLETED.md section NNN), then one per amendment naming Elias's ruling, "AMENDED (Hn): old -> new".
  - The block must match the list's last entry. A change without a ruling fails. After an amendment the instrument runs again, and the ruling record is checked against the new outcome.

- THE RULING RECORD is held by the instrument:
  - NONE, before the first run;
  - RULED (H1, section 803) with the run's outcome INSTALLS, the section of the run and the decisive digest;
  - ASKED, with the ask's section, the outcome (S1 or S2) and the decisive digest.

  The line after the block mirrors it, never a digest:
  - `**Ruling record:** RULED (H1, s803; run sNNN): INSTALLS.`
  - `**Ruling record:** ASKED (sNNN): S1.` (or S2)

  The instrument fails while the mirror and the record differ.

- THE DECISIVE DIGEST is SHA-256 over these lines, "key=value", one per line, UTF-8, LF, in this order:
  - each of HP, HO, SP and SO: FITTED or UNFITTED, n, and its observations (congress, measure, roll, d, m) in fit order;
  - the UNPREDICTABLE measures by identity;
  - for every measure of V in order: P(sent) and P(named) under (B) as reduced fractions p/q;
  - E[vetoable sent], E[named] and E[hits] under (B), pooled and per president, as reduced fractions;
  - the outcome.

  Nothing else: no interval, reading, (A), override, seeded realization or companion.

- NONE: every run fails the bars until its record is registered. The commit that first runs the rule registers it: RULED ... INSTALLS, or ASKED. NONE holds only before that commit.

- RULED / INSTALLS: the bars pass while the run's outcome is INSTALLS and the decisive digest equals the registered one.
  - A moved digest with INSTALLS fails until a commit re-pins it, naming the old digest, the new one and "outcome unchanged". Elias is told the rates moved (E4). A grown extract (the record's reach) takes this path.
  - A changed outcome fails until R-US17 is ASKED again, and is never re-pinned.

- ASKED: the bars pass while the outcome and the decisive digest equal the registered ones. Otherwise they fail until the ask is registered again with the new table, and Elias is told. A ruling must name an option that can be built (THE STOPS' OPTIONS, or (c) amended by name). Otherwise the record stays ASKED.

- US-21's veto diagnostic asserts RULED / INSTALLS or a buildable ruled option, the current block digest, and the catalog's digests. While not, it fails.

- THE BARS.
  - The preregistering commit: the documents batch; MojibakeCheck, DocumentClaimCheck, ResidueCheck and PreWiringPremiseCheck by name. It holds documents only, with no instrument.
  - The commit that first runs the rule:
    - the cheap bar, with UsVetoDrawCheck added;
    - the instrument by name;
    - UsVetoBacktestCheck unchanged;
    - the simulation bar (Poland's proof, H1);
    - R-US18's and R-US19's block digests unchanged.
  - US-21: everything above, plus its own done-when.
  - A commit touching Tools/us_veto_prep.pl re-runs Tools/us_veto_mutations.sh (13 of 13, control byte for byte) and then this instrument.

======================================================================
PROVENANCE OF THIS SPECIFICATION (not part of the rule)
======================================================================

Read:
- the brief;
- USA_STAGE_PLAN.md: US-10, US-18, US-19, US-21, spec-risk 4, R-US17 to R-US19 and the G and H registers;
- COMPLETED.md sections 791, 800, 801, 802, 803 and 831;
- veto_record.md;
- docs/generated/US_VETO_BACKTEST.md, including its every-veto table with party splits;
- docs/generated/VETO_B1_BACKTEST.md;
- the source of Tools/us_veto_backtest.pl, PresidentialVeto.cs and SimulationRandom.cs;
- the heads of UsVetoBacktestCheck.cs and DivisionRecord.cs;
- UsHouseCountCheck.cs's digest and ruling-record lines.

Not read: us_veto_measures.csv (never opened), the roll-call XML, and nothing in G:\Claude\tmp but the brief. No tool and no Unity process was run. Nothing was edited.

No defection count, rate, interval or model result was computed. No choice above uses any figure: the N_MIN arithmetic is a Wilson formula at p = 1/2, which reads no data.
```

