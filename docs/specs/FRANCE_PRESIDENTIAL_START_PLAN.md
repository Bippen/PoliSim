# PoliSim - SP-8, France's 2022 presidential start: the run-off night, cohabitation and the start card (work plan and prereg for Code)

**Status: PREREG, written 2026-10-30 before any measurement of the record or of the tiles.** It extends `docs/specs/POLAND_PRESIDENTIAL_START_PLAN.md` (SP-7's pattern, its §7: "France's presidential start (SP-8) reuses the start point and the candidate path; its own model ... is its row's") and rests on Elias's rulings K174-K177 (`docs/RULINGS_2026-10-23.md`), the SP-8 row of `POLISIM_FEATURE_LIST.md` and Design's return `send/s8_return/RETURN.md` (2026-10-30). A bare § number is a `COMPLETED.md` record.

## 1. The decision rules, fixed before anything is measured

**(P1) The record's two counts.** The night reads two primary sources per round: the Interior Ministry's definitive count by département and for France entière (`ElectionsData/france/raw/presidential2022/`, data.gouv.fr, the publisher's own sha1), and the Conseil constitutionnel's proclamation (CC-195 for the first round, CC-197 for the run-off).
- (a) For every round, the département rows summed (every row, the abroad and the overseas included) must equal the Interior's France-entière row - registered, voters, blank, null, cast, and each candidate's votes - exactly. Any difference fails the check, and nothing is drawn from the département file.
- (b) The Conseil's proclaimed figures are compared with the Interior's France-entière row, field by field. A difference is not a failure. The counted states print the Interior's figures and the proclaimed state prints the Conseil's (Design's "until then every figure is the Interior's count"), and the check prints every field that differs.

**(P2) Shares.** A share is a candidate's votes over the round's cast votes ("suffrages exprimés"), from the same source as the votes. The ranking is by votes. A tie at a place the rule must break (the run-off's second place, or the run-off itself) is reported as "nothing is decided", never broken (Art. 7's tie is not in the Constitution; `TwoRoundElection`'s rule).

**(P3) A département's leader** is the candidate with the most votes in it on the Interior's count. With a tie for first it has no leader and its tile stays paper, said in its slip.

**(P4) The 20:00 estimate** is drawn only where an institute's own page states it. A round with none shows the estimate state with no figure, the ABSENT label and the reason BILLED. It is never derived from the count.

**(P5) The département tiles** (Design's For Code 4). Design's first pass is refined by a committed generator (`Tools/s8_tiles.pl`), deterministic, never hand-edited. Every tile has a projected position from its prefecture, by Design's own formula (`col = (lon + 4.6) / 1.1`, `row = (50.95 - lat) / 0.74`, unrounded). Its cost is the squared distance from its cell to that point. Starting from Design's placement, the generator applies the best improving move until none remains. A move is a swap of two tiles, or a move to an empty cell that touches an occupied cell on a side. Ties go to the lowest département number, then the lowest cell (row, then column).

The result is accepted only if all four hold:
- (i) 96 metropolitan tiles, one for each metropolitan code in the Interior's file;
- (ii) no two tiles in one cell;
- (iii) no tile ends farther from its point than the larger of 2.0 cells and its distance in Design's first pass;
- (iv) each of the six tiles Design named (89, 62, 82, 86, 87, 29) ends closer to its point than in Design's first pass, or within 1.0 cell of it.

The generator prints every tile's distance before and after, and fails, writing nothing, if (i)-(iv) do not hold. The overseas and abroad units sit in a row under the map, one tile for each non-metropolitan row in the Interior's file, labelled by the Interior's own code.

**(P6) What may not move.** None of this reaches the simulation: France holds no presidential election in the game (`TwoRoundElection.RuleOf` stays Poland's alone until a French vote model exists, §2), so no baseline, family or sentinel can move. A dump diff that moves anyway fails the item.

## 2. What the playable start still needs (the stop, measured 2026-10-30)

A playable candidate's start needs the game to hold France's two rounds with the player's candidate in the field. The French vote has no model. `PartySystems` returns no fitted electorate for France ("FRANCE HAS NONE": France was never fitted, because the backtest is a share model), so the game cannot form the poll that SP-7's first round multiplies by a candidate's factor. A second gap: the roster is the Ministry's 2024 nuances, so the four candidates of the left in 2022 (Mélenchon, Jadot, Roussel, Hidalgo) have no backing party in it. They would all fall inside UG, which the Ministry does not split.

Until a French vote model is specified (Elias's to rule, as Poland's ask 1 (c2) was) and fitted, the presidential card stays locked and says why. The night is built on the record. The world clock's extension to 7 Mar 2022 (K174) is unreachable while no start opens there, and it waits with the model.

## 3. Built, and what waits (TRACKING; the measurements are the record's)

**Built:** the 2022 record, fetched and generated (`Tools/s8_fr_prep.pl`, `FrenchPresidentialReturns`); France's rule from Art. 6-7 (`TwoRoundElection.RecordRuleOf`, never a gate on holding an election); Macron's two terms of record (`PresidencyOfRecord`); the locked card's opening day (CC-187) and its reason; `FrenchPresidentialRecordCheck` (§963). **Built on wave/s8fr and held there (K332):** the night on the record in board 8a's four states (`FrenchPresidentialNight`, `ElectionNightScreen.BuildFrance`, filmed by a France `-shotinterrupts` session on the merged tree, reals8frfr) and the tiles (`Tools/s8_tiles.pl`, whose `Refined` says whether P5 accepted the pass). No game path opens them while the card is LOCKED, so they land with step 3, the candidate's start.

**Waits, in order:**
1. **A French vote model** - Elias's to specify: how a first round forms in the game (the poll a candidate's factor multiplies), and which roster party backs each of the 2022 field.
2. **The world clock to 7 Mar 2022** (K174): France's XVe Assembly by nuance (the Interior's 2017 page, through the Internet Archive as SEAT-1 read 2022's), Castex's government (its appointment decree; its end is held, JD-C16), Italy's 2018 Senato.
3. **The candidate's start**: the card playable on CC-187's day, the picker, the player's candidate in the contest, the save step (SP-7b's path, `PresidentialStart`).
4. **Cohabitation** (board 8b; K175, K176): the predicate needs the prime minister's camp, which no official page states for any government of record (GAP G4), and Art. 8's verb. Until then the tab has nothing true to show.
5. **France's party inks** (a Design proposal): until one is installed, every French draw is in the Neutral.
