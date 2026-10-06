# Review - §801, PS-6 G1: R-US19's instrument `UsSenateRaceCheck` (2026-10-06)

One adversarial reviewer (a single agent) read `Assets/Editor/UsSenateRaceCheck.cs` against the whole of R-US19's block (`docs/specs/USA_STAGE_PLAN.md`, as amended in §797). It also read the first run's log (`senate801a`) and the catalogs and CSVs. It edited nothing. Below is its report, wording kept and layout lightly reformatted, followed by what was done.

## The report

**Verdict: no defect changes the OUTCOME or the decisive digest.** I re-traced the first run's figures against the CSVs. On this data the block, as written, gives `S1 + S4 [(i)]`:
- K1: M (a) 4, (b) 5.
- K3: M (a) 5, (b) 4.
- The cycles part, so the result is S1.
- Reading (i) gives (a), compared less S2, so S4 fires on (i).

**Checked and found faithful to the block:**
- The sides, including the I caucus matched by state, class and surname, and the D/I holders.
- The bases:
  - AZ's and GA's K3 bases are the 2020 specials.
  - GA's runoff takes the 2020 V.
  - California's base is the full-term race.
- x and the F-H fallbacks.
- (b) unclamped.
- The 33/34 counts asserted.
- (a) at presidential and midterm T, with the earlier election's weights, no district rows read, and DC never a seat.
- (x) and its assertion.
- Steps 1-2, including with one cycle and after S1 or TIE, and the K1 assertion.
- (c): the 35-race fitted set, final_none_top standing for Nebraska's missing DEM side, the clamps, and its printed items.
- The readings:
  - (ii) adds exactly NE 2024 Class II and OK 2022 Class II, each on its 2020 base.
  - (iii) drops NE 2024 and UT 2022, keeping VT.
  - (v) drops CA.
- S4's comparison: less S4, S7, S8 and S9, and less S2 for a one-cycle reading.
- The outcome's order.
- The digest's contents, with nothing extra.

**Findings**

1. **The 0.5 cascade would throw on presidential cycles (latent).** The polling day was taken from senate_on rows, and there is no 2016 or 2024 row.
2. **The Vice President's vote is not counted (latent).** On 2022-11-08 senate_on stands 50-50, which with Harris is a DEM majority; the code fails there instead.
3. **The decided verdict drops signed N and summed N.** It prints |N| only.
4. **S9 is not quite what the block says (latent):**
   - it tests base_count's page rather than the deciding count's;
   - its match is brittle, hard-coding "round 2";
   - "Steps 1-5 run over the cycles fully read" is not built.
5. **Control is compared with the roster count, not with [SEN-DIV].** It also gives no Vice President's side at 50.
6. **Substitutions on play bases are not printed.**
7. **The exceptions list misses Mississippi's marks.** It also calls a target's missing two-sided count "a fallback", which applies to bases only.
8. **The S8 play-base set (ambiguous).** "Each special's previous race" is taken over every special in the catalog. The other reading takes play's specials only. The broader set holds today.
9. **The game's range at 2026 (ambiguous, PROVISIONAL).** The 2026 Class III specials (OH, FL) are held by their holders rather than called on their 2022 bases.
10. **The same-seat special assertion is vacuous by construction.** The block demands it. The K1 assertion is likewise tautological.
11. **Dead code:** `natP = ... ? natP : natP`.
12. **The ruling-record placeholders were unregistered at the first run.** This is expected; register them and the plan's line.

## What was done

- **1:** the polling day is now the statutory Tuesday after the first Monday in November (2 U.S.C. 7, quoted in `records_by_date.md` §4), not a lookup.
- **2:** kept as a declared departure. The Vice President's side is not in a generated catalog; `records_by_date.md`'s president and vice president table is prose. A caucus tie on a polling day therefore fails the instrument as a defect and names the seat. No share in this run is exactly 0.5.
- **3:** the decided verdict now prints signed N per cycle and summed.
- **4:** S9 now tests that each count rests on the Clerk's or the FEC's saved page, with a ranked count required on the FEC's. It is computed first, and steps 1-5 run over the cycles fully read.
- **5:** control now prints [SEN-DIV]'s line for the Congress beside the roster's count that day. At 50-50 it says the Vice President's side is not in a catalog.
- **6:** the play bases' substitutions are printed, each with its side agreement: four, all agreeing.
- **7:**
  - The specials of D's years are named, Mississippi's marks among them.
  - "A fallback" is said of bases only.
- **8:** the broader set is kept, declared in the record.
- **9:** each seat held on 3 Nov 2026 by an appointee sworn after the 2024 election is called as a 2026 special on its latest race (Ohio's and Florida's Class III), PROVISIONAL.
- **10:** kept as the block asks.
- **11:** removed.
- **12:** registered: `S1 + S4 [(i)]`, digest `1ede3f62…`, and the plan's line.

The reruns (`senate801c`, `senate801d`) give the same outcome and digest, and both instruments hold. The card was written by `senate801w`.
