# Review - §789, PS-6 US-5 part two: the national-vote proof, R-US15 asked (2026-10-06)

A workflow review (`polisim-staged-review`) of the change that builds US-5's instrument: `UsNationalVoteCheck` (two eve worlds read for the economic vote; for each history's last election `VoteModel.Calibrate` and five held fits; the identity checked on every fit; the chain in the live order; six cases scored two-party and signed; the degeneracy; E1's factors; the card's second block), GateReRun's US section after its verdict, `VoteShareBacktest.UsEconomicWeight`, the check in the cheap and documents tables, the card's R-US15 section and the plan's Built line and R-US15 ask. Not required by the tier (`Tools/bar_tier.ps1`: TOOLING); run because the readings are the evidence R-US15 is ruled on. One pass - no defect survived it; every finding was put to a refute-first skeptic, who re-graded it; every report below is verbatim.

## The first pass - confirmed (verbatim)

The workflow `polisim-staged-review` (run wf_d08048ce-f02, 40 agents) over the staged diff at HEAD 40583b01: four lenses (the chain and the identity, the eve worlds and the bars, claims and the R-US15 ask, an independent recomputation), every finding put to a refute-first skeptic. Every report below is verbatim.

### 1. The card labels 2022 (b)'s House row '(own)', but history (b) is presidential and 2022 held no presidential vote

- **Lens:** method - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/UsNationalVoteCheck.cs:236

**The scenario.** Cases[3] is new Case(2022, History("(b)", Contest.President, 2020, 2016), true, Contest.House) at line 73, so Scored[0] is House. The label expression `k == 0 ? " (own)" : ""` assumes Scored[0] is always the history's own contest. The generated card (docs/reference/US_ELECTIONS.md:294) therefore prints `| 2022 | (b) presidential 2020 against 2016 | ... | House 2022 (own) | 51.40 % | -3.67 | -3.33 |`. R-US15 is ruled on this table, and this row presents (b)'s House miss as its own-contest miss. The same wrong assumption is in the Case doc (lines 55-56, 'its own first') and GateSection's doc (line 325, 'on the case's own contest'). The log and GateSection's printed lines are correct ('on the House 2022').

**The fix proposed.** Label ' (own)' only when c.Scored[k] == c.History.Of, then regenerate the block. In the Case and GateSection doc comments, say the own contest is scored first only where it was held.

**The skeptic's evidence.** The finding holds. I checked the code, the generated card and both logs.

1. Assets/Editor/UsNationalVoteCheck.cs:73 builds the case as `new Case(2022, new History("(b)", Contest.President, 2020, 2016), true, Contest.House)`. Its history is presidential (History.Of == President), but the only contest it is scored on is the House (Scored == { House }). It cannot be scored on a presidential contest: SharesOf(President, 2022) throws, because the catalog has no presidential vote for 2022.

2. Line 236 adds the tag by position, not by contest: `F("{0} {1}{2}", c.Scored[k] == Contest.House ? "House" : "president", c.Year, k == 0 ? " (own)" : "")`. For this case k == 0 is the House row, so it gets " (own)".

3. The generated card shows the result. docs/reference/US_ELECTIONS.md:294 reads `| 2022 | (b) presidential 2020 against 2016 | 47.73 % | 98.4 / 93.9 | -0.33 | 47.73 % → 48.06 % | House 2022 (own) | 51.40 % | -3.67 | -3.33 |`. us5w4.log is WriteReadings run on this tree, so this is the current output.

4. In this change, "own" means the history's own contest everywhere else:
   - Pins doc, line 81: "2024's miss by each history on its own contest".
   - The pin check, line 243: "on its own contest".
   - USA_STAGE_PLAN.md:620 (the R-US15 ask): "(a)'s 2024 miss is below (b)'s on its own contest and on the presidential one".
   - The card's declared row, US_ELECTIONS.md:270: "Each scored on its own contest and on the other, where one was held".
   - The other five cases tag the history's contest: 2024 (a) House, 2024 (b) president, 2022 (a) House, 2020 (a) House, 2020 (b) president.
   2022 (b) is the only row where the tag sits on the other contest.

5. The logs are correct. us5w4.log:555 prints "scored on the House 2022: record R 0.5140; miss -3.67 before, -3.33 after". GateSection in elec789b.log:1384 prints "on the House 2022". The defect is confined to the card's label.

**Possible counter-reading, and why it fails:** the plan (line 224) calls this case "2022's House", so "own" could mean the predicted contest. That reading clashes with every other row, with the pins and with the R-US15 ask. The Case summary (lines 55-56, "its own first") also only makes sense as the history's contest.

**Why minor, not a defect:** no figure is wrong, and the contest name printed is correct. The recommendation does not depend on the tag: plan line 620's "(a) is the closer in 2022" compares both histories on House 2022, which is correct. The harm is that, in the evidence R-US15 is ruled on, the tag presents (b)'s cross-contest score in 2022 as an own-contest score. That hides the fact that (b) gets no own-contest test in 2022. Under the claim convention this is a false statement in a generated block.

The doc comments at lines 55-56 and 325 make the same assumption. They are secondary, since GateSection's printed output names the contest correctly.

**The skeptic's corrected fix.** Line 236: tag a row by contest, not by position. Use `c.Scored[k] == c.History.Of ? " (own)" : ""` in place of `k == 0 ? " (own)" : ""`. The other five cases come out unchanged, and the 2022 (b) row becomes "House 2022" with no tag.

Then regenerate the card with `-executeMethod PoliSim.EditorTools.UsNationalVoteCheck.WriteReadings`. Run compares the whole block exactly, so it fails as STALE until the card is rewritten. The source digest does not change, because it is built from the catalog digests, the held electorates, the points and w, not from the card's lines.

Optional, for the doc comments:
- Lines 55-56: "its own first, where it was held".
- Line 325: "on the first contest the case is scored on - the history's own where it was held".

Leave line 241 as it is (`own = Scored[0]`). The pins cover 2024 only, where Scored[0] == History.Of, and SharesOf(President, 2022) would throw.

### 2. GateSection always prints 'national and +§8 agree', even next to its own figures that differ

- **Lens:** method - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/UsNationalVoteCheck.cs:354

**The scenario.** elec789c.log (the run on the staged tree) prints '2022 (b) ... free -3.66 -> -3.67' and '2020 (b) ... free +1.15 -> +1.16'. The very next line says 'Every fit holds its T-1 split, so national and +§8 agree'. The free fit's spatial share is off T-1 by its grid residual (2.9e-5 and 1.8e-5), which crosses a 0.01-pp rounding boundary. The poll is pulled back toward the prior by loyalty, so the two printed figures differ. The sentence does not depend on any check: GateSection passes a no-op Check to FitAll (line 340), so a held fit that failed to bracket would also be printed with this sentence beneath it.

**The fix proposed.** Qualify the sentence: national and +§8 agree to within the free fit's grid residual, and exactly for the held fits. Or print 'national' only for held fits. Or build the sentence from the figures, with a bracket/1e-9 failure reported in the section.

**The skeptic's evidence.** The first half happens on the current run.
- Log: G:/UNITY/Projects/PoliSim-captures/logs/elec789c.log line 1340 prints "2022 (b) ... free -3.66 -> -3.67" and line 1342 prints "2020 (b) ... free +1.15 -> +1.16". Line 1343 is the unconditional "Every fit holds its T-1 split, so national and +§8 agree". elec789b.log lines 1384/1386/1387 match, and Tools/elec_report.pl passes all of these lines into the diffed report.
- Mechanism: GateReRun.ToCompatScale followed by PreferenceModel.PersuadedShares hands the spatial shares back unchanged. Preference(compat, prior, loyaltyPerParty) computes result[i] = λ_i·prior_i/priorSum + (1-λ_i)·persuaded_i, so a free spatial split of prior + δ becomes a poll of about prior + κδ, with κ ≈ (1-ps)(1-λR) + ps(1-λD) ≈ 0.04 for loyalties 98.4/93.9.
- Measured: n789a.log line 524 "2022 (b), free: the poll 0.477303 ... prior 0.477302 ... residual 2.9e-5" and line 541 "2020 (b), free: the poll 0.488866 ... prior 0.488867 ... residual 1.8e-5". National and +§8 therefore differ by about 0.003 pp and straddle a 0.01 rounding step. Held fits have residuals of about 1e-16 and print identical pairs.
- Bound: FitAll accepts free.Mad < 0.05, and Calibrate's MAD is in pp (100·Σ|Δ|/n), so the gap could reach about 0.05 pp with every check passing.
- Wording: line 354 is a hard-coded string, and GateSection prints no residual. The card's sentence (UsNationalVoteCheck.cs:282) keeps the qualification "free: within Calibrate's grid; held: solved", and the identity check at line 216 (off <= residual + 1e-12) is written with the residual allowed. GateSection states the exact form, which is not what the code proves.

The second half is true as a code fact but not a practical failing path.
- Line 340 does pass `(ok, what) => { }` to FitAll.
- CheckSuite.cs lines 277 and 741 run UsNationalVoteCheck.Run (Measure -> FitAll with a real Check) in the cheap and documents tables. n789a.log line 561 shows "the reading holds" on this tree.
- A non-bracketing held fit would fail HeldFit's bracket check (line 405) and its 1e-9 check (line 409) there. The identity check alone would not catch it, since Preference always pulls toward the prior. Current places are 0.44 to 0.58, well inside [0,1].

Severity: minor. It is a printed-text inaccuracy in a non-gating section. No figure, pin, card line, check or gate is wrong. But it is in the evidence R-US15 is ruled on and contradicts its own printed figures.

**The skeptic's corrected fix.** Build the closing line from the figures and keep the card's qualification. In GateSection, declare `double gap = 0.0;` before the case loop. Inside the fit loop, after p is computed, add `gap = Math.Max(gap, Math.Abs(spatial[0] - p[0]));`. Replace line 354 with: `sb.Append(F("  Every fit holds its T-1 split (free: within Calibrate's grid; held: solved), so national and +§8 agree to that residual - {0:0.0000} pp at most here: the vote model returns the prior with two parties, whatever the loyalty (UsNationalVoteCheck checks each fit).\n", 100.0 * gap));`. This changes only the US section's closing line in the elec report, so the 'moves no other country's line' criterion still holds. The elections launch needs one re-run (elec789d) for the record to carry the new line. No change is needed for the no-op Check: Run carries those checks in the cheap and documents tables.

### 3. An exception in the US section now suppresses the whole R-EL13 gate output

- **Lens:** method - **reviewer:** minor - **skeptic:** note
- **Where:** Assets/Editor/GateReRun.cs:194

**The scenario.** `sb.Append(UsNationalVoteCheck.GateSection())` runs before `Debug.Log(sb.ToString())` (line 196), and nothing catches around it. Failure case: the generated catalog is regenerated without a House or presidential year. SharesOf then throws InvalidOperationException inside Parallel.For, which surfaces as an AggregateException. (A RealRoster(USA) with fewer than two units would instead throw IndexOutOfRange in Along.) GateReRun.Run throws, and the four countries' rows and the VERDICT are never logged. The elections-backtests runner records only 'GateReRun THREW'. That contradicts the change's claim that the gate's countries and their lines are untouched; a section declared outside the gate can now remove the gate's verdict.

**The fix proposed.** Log the gate's StringBuilder before computing the US section. Or wrap the GateSection call in try/catch and append the exception text, so the verdict always prints.

**The skeptic's evidence.** THE PATH IS REAL. In Assets/Editor/GateReRun.cs, `sb.Append(UsNationalVoteCheck.GateSection())` (:194) runs before the only `Debug.Log(sb.ToString())` (:196). Every gate line (the four cases' tables, the R-EL13 rows, the VERDICT) lives only in `sb`, and nothing in Run catches. In Assets/Editor/CheckSuite.cs, `CheckExit.Collect` (:106-137) is try/finally only. `RunTable` (:806-813) logs `CHECKS: {name} THREW {e.GetType().Name}: {e.Message}` and continues, so a throw in GateSection throws away the gate's whole printout. Throw sites in GateSection:
- `SharesOf` (UsNationalVoteCheck.cs:447) raises `InvalidOperationException` when a year has no rows. On a T-1 year it comes out of FreeFits' `Parallel.For` (:366-371) as an AggregateException. On a missing scored year it is thrown bare at :343.
- A trigger the finding missed and likelier than its own: a third US roster unit, which the plan foresees (SP-6b, US-33, R-US15 (iii)). `SharesOf` always returns two elements, so `PreferenceModel.Preference` (PreferenceModel.cs:141-149) would throw `ArgumentException("prior shares must be one per party")`.

WHY ONLY A NOTE:
(1) Nothing triggers it today. elec789b.log:1372-1387 and elec789c.log:1328-1343 print the VERDICT and then the US section, and no log has a THREW line.
(2) There is an earlier guard. The catalog is generated from SHA-pinned pages and checked by GeneratedCatalogCheck. `UsNationalVoteCheck.Run` is in the cheap table (CheckSuite.cs:277) and the documents table (:741). It runs everything GateSection runs (RealRoster, FreeFits, FitAll, SharesOf, LoyaltyModel, Preference) inside its own try/catch (:109-119), and it has an explicit REP/DEM roster check (:164). Both triggers therefore fail the bar at commit time, before any elections launch. The one gap: an edit that fixes `Measure` but not `GateSection`, which no bar runs.
(3) It is never silent and never wrong. GateReRun always ends `CheckExit.Finish(0)`, so its verdict is text only, and the launch already exits 1 by design (elec789c.log:2136,2173). The visible signal would be the red THREW line, `2 of 11 FAILED — ..., GateReRun`, and the elec<N> diff losing every gate line. Tools/elec_report.pl keeps CHECKS: lines, so the diff the ruling requires would show both.
(4) The pattern is old. GateReRun's own cases already go through throwing code with the same single log at the end: `row[1]` in BuildRegions (:237), and the ArgumentExceptions in RegionalVoteModel, LoyaltyModel and ItanesGroupLoyalty. What the change adds is a trigger from data unrelated to the gate.
(5) The comment at :193 ("lines untouched") is about the content of a run that completes, which the elec788a to elec789b/c diff verifies. It is not a robustness claim, so "contradicts" overstates it.

**The skeptic's corrected fix.** In GateReRun.Run, log the gate before the US section is computed, and log the section as its own entry. Let any exception propagate so RunTable still reports `GateReRun THREW`:

    Debug.Log(sb.ToString());
    // PS-6 US-5 (§789): the USA's cases, after the verdict and outside it - logged on their own, so a failure here cannot take the gate's lines with it
    Debug.Log(UsNationalVoteCheck.GateSection());
    CheckExit.Finish(0);

Tools/elec_report.pl filters line by line and drops blank lines and stack-trace lines, so the split leaves the elec<N> report byte-identical.

Do not use a try/catch that only appends the exception text. GateReRun always exits 0 and the launch already exits 1, so a swallowed exception would be quieter than today's THREW line, unless the catch also calls Debug.LogError.

### 4. The E1 'sums to one' check is an identity of its own construction and cannot fail

- **Lens:** method - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/UsNationalVoteCheck.cs:311

**The scenario.** rest is defined as 1 - trump - harris - sum(minors) (line 300), and fT*p[0] = trump and fH*p[1] = harris exactly. So the sum is 1 to rounding for any field and any poll. That includes rest < 0, which would happen if `total` were not the sum of the columns, or if a minor's votes were also inside a nominee's column. The bar reports 'ok' for a check that tests nothing. The plan's design asks only that the sum be printed.

**The fix proposed.** Check something that can fail, such as rest >= 0, and the candidates' total equal to the States' 2024 VotesTotal sum (both 155,238,302 today). Keep the printed sum as the design asks.

**The skeptic's evidence.** The finding holds. Its strongest wording ("tests nothing") goes slightly too far, and half of its proposed fix repeats a check that already exists.

How the check is built (staged Assets/Editor/UsNationalVoteCheck.cs):
- L289 `long total = field.Sum(c => c.Votes);`
- L290 trump = R votes / total, harris = D votes / total.
- L300 `double rest = 1.0 - trump - harris - minors.Sum(x => x.Share);`
- L309 `double fT = trump / p[0], fH = harris / p[1];`
- L310 `double sum = fT * p[0] + fH * p[1] + minors.Sum(x => x.Share) + rest;`
- L311 `Check(Math.Abs(sum - 1.0) < 1e-12, ...)`

(trump/p0)*p0 comes back as trump to within about one ulp. `rest` is defined as the complement. So `sum` is 1.0 to within about 1e-16 for any field, any rest and any positive or negative poll. The result does not depend on the thing the label names. The logs bear this out: us5w4.log:579/581 and n789a.log:555/557 print 1.000000000000 for both (a) and (b).

The check can only fail when p[0] or p[1] is zero or non-finite (Inf*0 or NaN gives NaN). Those cases already fail elsewhere:
- Both 2024 cases are pinned on after[0] (L84-88, L239-245). The comparison is a formatted string, so NaN or 0 cannot match "-2.42" / "-5.52".
- EconomicVote.ApplyRecordShift (EconomicVote.cs L188-208) clamps DEM at >= 0. It scales REP by (others-moved)/others, so a NaN or Inf in p[1] spreads into p[0] and the pin catches it.
- The identity check at L216 catches a NaN before the shift.
L311 therefore adds no independent evidence. This is the class the project names in EvidenceDiscriminationCheck: "the outcome of the test does not depend on the thing the test claims to be about". The same check says "a measurement is not a test".

Where the finding's scenario is weak:
- "If total were not the sum of the columns": that cannot happen, because L289 defines total as that sum.
- Overlap between a named minor and a nominee: this is caught by L291 (one R row and one D row; the rows in UsPresidentialReturns.cs L301-328 carry "-" for STEIN/KENNEDY/OLIVER). It is also caught by GeneratedCatalogCheck L631.
- With this data, rest = 649,541 / 155,238,302, which is above 0.
- The proposed check of the candidates' total against the States' sum is already enforced in the same bar. GeneratedCatalogCheck L616-634 checks the field sum against the jurisdictions' VotesTotal, plus R and D. It is registered at CheckSuite L275, two entries before this check. I confirmed both totals are 155,238,302 (R 77,302,580, D 75,017,613).
- `rest >= 0` is itself close to an identity: it is the 21 other "-" rows over their own total.

Why the severity is note: no figure, card line or pin is wrong. The design (USA_STAGE_PLAN.md L227) asks only that the field's sum be printed "beside it - in-sample by construction". The card (US_ELECTIONS.md) prints it under "(in-sample by construction)" and says the factors "cannot decide R-US15". The only harm is an "ok" log line that reads as evidence when it is arithmetic.

**The skeptic's corrected fix.** Remove the Check at L311. Keep `sum` in the card table (L313) and the log line (L314), as the design asks. If a guard is wanted, make it say what it guards and point it at something that can fail, in the style of GeneratedCatalogCheck's House comment ("a guard on a hand edit only"). For example: `Check(p[0] > 0 && p[1] > 0 && !double.IsNaN(fT + fH) && !double.IsInfinity(fT + fH), "... the poll's nominees positive and the factors finite (the field's sum is 1 by construction: rest is its complement)")`. Do not add a candidates-total-against-States check here. GeneratedCatalogCheck L616-634 already makes that check in the same bar.

### 5. The card text says 'the last column divides it out', but the division is in the second-to-last column

- **Lens:** method - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/UsNationalVoteCheck.cs:301

**The scenario.** The E1 table header (line 304) ends with '... | factors ÷ the nominees' share (R / D) | the poll's two-party miss on the presidential 2024 (pp) |'. The last column is the miss; the column that divides by the nominees' 98.12 % is the one before it. The generated card (docs/reference/US_ELECTIONS.md:346) points the reader to the wrong column.

**The fix proposed.** Name the column ('the column "factors ÷ the nominees' share" divides it out'), then regenerate the block.

**The skeptic's evidence.** Assets/Editor/UsNationalVoteCheck.cs:301 (staged, new file) writes "...so a poll exact on the two-party split gives both factors that figure - the last column divides it out." The next table's header (:304) has seven columns: "| history | poll R / D | Trump's factor | Harris's factor | the field's sum | factors ÷ the nominees' share (R / D) | the poll's two-party miss on the presidential 2024 (pp) |". The row (:313) fills column 6 with `fT / (trump + harris), fH / (trump + harris)`, which is the division by the nominees' share. Column 7, the last, is `Pp(100.0 * (p[0] - record))` with record = TwoParty(SharesOf(Contest.President, 2024)), and TwoParty (:451) is `shares[0] / (shares[0] + shares[1])`. Column 7 never touches the factors. The generated card (docs/reference/US_ELECTIONS.md:346-351) bears this out. Row (a) reads `1.018390 | 0.945620 | 1.000000 | 1.0379 / 0.9637 | -1.85`: 1.018390/0.9812 = 1.0379 and 0.945620/0.9812 = 0.9637, which is column 6, while -1.85 = 48.90 - 49.80/98.12·100 is the poll's miss. Row (b) agrees (1.1220 / 0.8992, -5.52). No reading saves the sentence: the fits table above also ends on a column that divides nothing (the midway unit's share). Nothing computed, checked or ruled depends on this text, and the header names the division itself, so the slip is a wrong pointer in the R-US15 evidence and no more. It also bears on how to fix it. The stale check (:113-117) compares the whole block text (CardBlock vs CardBlockOf). The block's digest (:320) covers the catalog source digests, the held electorates, the party points and w, but not the wording. So the code and the card line must change together, and running WriteReadings would change only line 346.

**The skeptic's corrected fix.** Change the string at UsNationalVoteCheck.cs:301 to end "...gives both factors that figure - the column 'factors ÷ the nominees' share' divides it out." ("the column before the last divides it out" also works). Then regenerate the block with -executeMethod PoliSim.EditorTools.UsNationalVoteCheck.WriteReadings. The digest does not cover the wording, so if Unity is unavailable the identical one-line edit at docs/reference/US_ELECTIONS.md:346 is exactly what WriteReadings would write, and the next Run confirms it. Moving column 6 to the end would also fix it but touches :304, :305 and :313 and both card rows.

### 6. The plan says 'four figures pinned'; Pins holds five distinct figures

- **Lens:** method - **reviewer:** note - **skeptic:** minor
- **Where:** docs/specs/USA_STAGE_PLAN.md:211

**The scenario.** Pins (UsNationalVoteCheck.cs:84-88) hold the four 2024 own-contest misses (+0.08, -2.42, -3.02, -5.52) plus the economic vote (+2.50), which appears in both rows. The Pins doc comment counts the economic vote; the Built line's 'four figures pinned' does not. Under the claim convention, this count disagrees with the code.

**The fix proposed.** Say 'four misses and the economic vote pinned', or 'five figures'.

**The skeptic's evidence.** I tried to refute the finding and could not. The plan's count disagrees with the code's own description of what it pins.

- **The plan.** Staged docs/specs/USA_STAGE_PLAN.md line 211 says: "`UsNationalVoteCheck` (the cheap bar and the documents bar; four figures pinned; a stale block fails it)".
- **The pin table.** Staged Assets/Editor/UsNationalVoteCheck.cs lines 81-88. The doc comment reads: "The figures R-US15 is ruled on, pinned as the card prints them: 2024's miss by each history on its own contest, before and after the economic vote (points, two-party), and the economic vote itself (DEM, points)." So the code's own list of "the figures" is four misses plus the economic vote. The table matches: `Pins = { (2024, "(a)", "+0.08", "-2.42", "+2.50"), (2024, "(b)", "-3.02", "-5.52", "+2.50") }`. That is five distinct figures across six slots.
- **What gets checked.** Lines 239-244 compare all three fields of each row (`b == pin.Before && a == pin.After && s == pin.Shift`), so six values are checked and five are distinct.
- **The logs agree.** n789a.log:505 and :515, and us5w4.log:529 and :539, print "(pinned +0.08, -2.42, +2.50)" and "(pinned -3.02, -5.52, +2.50)".
- **No other pinned figures.** The file has no second pin table. The magnitude checks compare against `EconomicVote.UsPresidentUnified`/`UsPresidentDivided`, which are constants, not pinned literal values.

**The refutation tried.** "Four" holds only if the economic vote is not counted as a figure. The Pins doc comment counts it explicitly, though. The shift equals before minus after at full precision, but it is still pinned and compared as its own rounded string. So "four" undercounts by the code's own words.

**Why it is minor rather than a note.** CLAUDE.md's claim convention says a count is a DERIVED claim. It may only be GENERATED, REFERENCED or DELETED, and "nobody transcribes, anywhere, in any file". COMPLETED.md is exempt; this plan is not. Line 211 is the only counted pin claim anywhere under docs/. The US-4 Built line in the same plan (line 197) set the precedent without a count: "`UsStateSwingCheck` (the cheap bar; pinned; a stale block fails it)". No check catches this, because DocumentClaimCheck only checks backticked `Type.Member` references, which is why n789a passed the documents bar. The impact is documentation only: no runtime effect, and R-US15's figures and the card are unaffected. But the line is new in this diff, so it should be fixed before the commit.

**The skeptic's corrected fix.** Drop the count instead of correcting it. In docs/specs/USA_STAGE_PLAN.md line 211, write "(the cheap bar and the documents bar; pinned; a stale block fails it)", following line 197's US-4 Built line. If the line must say what is pinned, describe it without a number: "R-US15's 2024 own-contest misses and the economic vote pinned". The reviewer's suggestions ("five figures", or "four misses and the economic vote") would still put a count about the code into a document covered by the claim convention, and that count goes wrong the moment Pins changes. If the COMPLETED.md §789 record or the commit message gives a count, it should be five distinct figures (six compared values), not four.

### 7. GateReRun's own R-EL13 report now depends on the US section succeeding

- **Lens:** worlds - **reviewer:** minor - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/GateReRun.cs:194

**The scenario.** GateSection() runs before the gate's only Debug.Log (line 196), and it is not wrapped in a try/catch. Any exception in it skips that log and leaves GateReRun.Run, so the four countries' rows and the VERDICT are never printed. Two ways this can happen: UsNationalVoteCheck.SharesOf throws InvalidOperationException for a catalog year it cannot find (UsNationalVoteCheck.cs:447), or FreeFits' Parallel.For throws an AggregateException (line 366). Example: the House returns are re-cut to 2018-2024. Case 2020 (a) needs House 2016 as its T-2 (line 342), so GateSection throws. CheckSuite.RunTable then logs 'CHECKS: GateReRun THREW' as an error (CheckSuite.cs:810), and elec<N> loses every GateReRun line, the VERDICT included. That contradicts the design's 'after the verdict and outside it'. The normal path logs nothing; the elec788a log and the elec789b/elec789c logs show no GateReRun error.

**The fix proposed.** Print the gate before computing the US section: Debug.Log(sb) and then Debug.Log(UsNationalVoteCheck.GateSection()). Alternatively, wrap the call in a try/catch that appends 'USA section: threw ...' so the R-EL13 report always prints.

**The skeptic's evidence.** The finding's description of the code is correct, but it can only happen on a tree whose cheap bar is already red, and the failure is loud.

**What holds.** In G:/UNITY/Projects/PoliSim/Assets/Editor/GateReRun.cs, line 194 calls `sb.Append(UsNationalVoteCheck.GateSection());` with no guard. That call comes before the method's only `Debug.Log(sb.ToString())` at line 196. GateReRun runs through `ElectionsBacktests.Set` (ElectionsBacktests.cs:26), which `CheckSuite.RunTable` drives. `CheckExit.Collect` does not catch, so a throw lands in RunTable's catch at CheckSuite.cs:810 (`CHECKS: {name} THREW ...`), and the four countries' rows and the VERDICT are never printed. The trigger chain is real: `SharesOf` throws at UsNationalVoteCheck.cs:447. Case 2020 (a) needs House 2016 only as its T-2 (Cases line 74, read at line 342), and `FreeFits` reads only T-1 keys.

**Why it is a note.**

1. **It cannot happen on a tree that passes the cheap bar.** The staged CheckSuite.cs registers `("UsNationalVoteCheck", UsNationalVoteCheck.Run)` in the cheap Suite. Its `Measure` makes every call `GateSection` makes:
   - `FreeFits` (line 187 = line 335)
   - `FitAll` on the same T-1 (line 192 = line 340)
   - `SharesOf` on T-1 and T-2 (lines 206-207 = lines 341-342)
   - `SharesOf(c.Scored[k], c.Year)` for every k (line 229), which covers line 343

   The inputs are identical, static and deterministic:
   - `RealRoster` returns the static `UsaParties` array.
   - `TryElectorate` returns constants (PartySystem.cs:662-700).
   - The catalog is static generated arrays.
   - `VoteModel.PredictShares` and `Calibrate` are pure (VoteModel.cs:85-187). So the `Parallel.For` has no race; it can only rethrow `SharesOf`'s exception wrapped in an AggregateException.

   Any data or roster state that throws in `GateSection` throws first in `Measure`. `Run` catches that at line 119 as `FAIL threw:`, so the cheap bar goes red.
2. **The current tree does not trigger it.** The generated House array holds 50 rows each for 2016, 2018, 2020, 2022 and 2024. States holds 51 each for 2012, 2016, 2020 and 2024. Running `elec_report.pl` on elec788a and elec789c gives a diff that only adds the ten USA lines after the VERDICT, with no THREW.
3. **When it does happen, it is loud.** RunTable logs `CHECKS: GateReRun THREW InvalidOperationException: the catalog holds no House vote of 2016` as an error, and the summary becomes `2 of 11 FAILED — OutOfSample2026Diagnostic, GateReRun`. `elec_report.pl` keeps `CHECKS:` lines, so the elec&lt;N&gt; diff shows the missing lines and their cause. Only the exit code can't tell it apart, because the batch already exits 1 by design (`OutOfSample2026Diagnostic`).
4. **The single-log pattern was already there.** `GateReRun.Run` already had unguarded throw paths that would suppress the same log: the ArgumentExceptions in `LoyaltyModel`, `PreferenceModel`, `ItanesGroupLoyalty.Build` and `RegionalVoteModel`.
5. **"Contradicts the design" overstates it.** USA_STAGE_PLAN.md:228 says "outside the gate ... The four cases, the verdict and BuildCases are untouched". That concerns the verdict's computation, and every run where the section succeeds honours it.
6. **The finding's second fix is worse than the gap.** GateReRun ends with `CheckExit.Finish(0)` and never arms the log fold. A try/catch that appends "USA section: threw" would make GateReRun count as clean in RunTable, which breaks RunTable's own rule that a throw is not a pass.

**The skeptic's corrected fix.** Optional hygiene: change only the order, and do not add a try/catch. In GateReRun.Run, log the gate first, then log the US section, then call Finish:

    Debug.Log(sb.ToString());
    // PS-6 US-5 (§789): the USA's cases, after the verdict and outside it - logged after the gate so a US-only failure cannot take the gate's lines with it; a throw still reaches RunTable
    Debug.Log(UsNationalVoteCheck.GateSection());
    CheckExit.Finish(0);

A throw still reaches CheckSuite.RunTable and is reported as THREW, but the R-EL13 rows and the VERDICT have already printed. The change does not move the elec&lt;N&gt; diff. `elec_report.pl` filters out the second log's stack-trace, (Filename:) and blank lines, and the section's leading "\n" becomes a blank line that is also filtered. So the report text stays the same.

### 8. GateSection prints its closing claim unconditionally, and its own figures already disagree with it

- **Lens:** worlds - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsNationalVoteCheck.cs:354

**The scenario.** GateSection passes FitAll a no-op Check (line 340), so on this path the bracket, the 1e-9 solve and the identity are never tested. Line 354 then always prints 'Every fit holds its T-1 split, so national and +§8 agree'. In elec789c the free fit's lines read '2022 (b) ... free -3.66 -> -3.67' and '2020 (b) ... free +1.15 -> +1.16', so the two figures differ at the printed precision. They agree only to within Calibrate's grid residual; the card says 'free: within Calibrate's grid' but this line does not. The header (line 333) says 'held at each fitted country's spread', yet a sixth fit, 'the four's median', is printed too. If the identity broke (for example a change to ToCompatScale, PreferenceModel.Sharpness or its MinimumCompatibility clamp), the launch would print lines that contradict the sentence under them. Only the bar's identity check at line 216 would catch it.

**The fix proposed.** Build the closing line from the measurement: the worst |p - prior| against the free fit's residual (or 'within the free fit's grid residual'). Count the failures from FitAll's Check instead of discarding them and print them. Name the median fit in the header.

**The skeptic's evidence.** The contradiction is real today, in the launch the record will cite.
- G:/UNITY/Projects/PoliSim-captures/logs/elec789c.log:1340: `2022 (b) ... free -3.66 -> -3.67`
- elec789c.log:1342: `2020 (b) ... free +1.15 -> +1.16`
- elec789c.log:1343, emitted by UsNationalVoteCheck.cs:354 with no condition: `Every fit holds its T-1 split, so national and +§8 agree: ...`
- Tools/elec_report.pl keeps this section, so the sentence is also in the filtered elec<N> report that gets diffed.

Why the two figures differ. With two parties ToCompatScale followed by PersuadedShares hands back the spatial shares (c^3 = 100^3*s/max). Preference (PreferenceModel.cs:165) then gives p = λ·prior + (1-λ)·spatial, renormalised. The loyalties are about 94-98, so p is pulled almost onto the prior.
- us5w4.log:548: `2022 (b), free: the poll 0.477303 ... prior 0.477302 ... residual 2.9e-5`
- us5w4.log:554: `every fit within 1.1e-6 of the prior`
- us5w4.log:460: Calibrate's free-fit MAD is 0.0029 pp for President 2020, and the card shows 0.0018 for President 2016.
- So the free fit's "national" (spatial[0]-record, line 349) carries the whole grid residual of about 0.003 pp, while "+§8" (p[0]-record) carries about 1e-6. The 0.003 pp is enough to flip the last printed digit in 2 of the 6 rows.

The qualifier exists elsewhere and line 354 drops it.
- The declared design (USA_STAGE_PLAN.md) says: "exactly for a held fit, which is solved, and within its grid's residual for the free fit".
- The card's own sentence (Measure, line 282) says: "Every fit holds its T-1 split (free: within Calibrate's grid; held: solved)".

The sentence is not tested on this path.
- Line 340 passes `(ok, what) => { }` to FitAll, so the bracket check (405), the 1e-9 solve check (409) and the MAD check (382) are thrown away.
- Lines 345-350 never run the identity test. The card's sentence is different: it is backed by the checks in the same Measure run, because Run fails and WriteReadings refuses (line 140) when any of them fails.

What the finding overstates. The "if the identity broke" scenario is already caught.
- The identity check (line 216) runs in UsNationalVoteCheck.Run.
- That check is in CheckSuite's cheap Suite and in its documents table (the staged diff).
- bar_tier.ps1 makes every code item owe RunAllBatch (the cheap bar).
- A change to ToCompatScale, Sharpness or MinimumCompatibility would therefore fail the bar before the launch's sentence could mislead.
- GateReRun.Run always exits with Finish(0) and the US section sits outside the verdict, so no figure, verdict or pin is wrong. The defect is wording only: a 0.003 pp rounding flip.

The header is stale (cosmetic).
- Line 333 reads "held at each fitted country's spread", but a sixth fit, "the four's median" (FitAll line 395), is printed on every line (elec789c:1337-1342).
- The median fit first appears in us5w3 (02:39), after elec789b (02:36), which shows five fits. The header was written before the median was added and was not updated.

**The skeptic's corrected fix.** Make line 354 true of what is printed.

Minimum fix:
- Qualify it as the card and the plan do, for example: "Every fit holds its T-1 split (held: solved; free: within Calibrate's grid residual, which can move national's last digit), so +§8 gives back the prior: the vote model returns the prior with two parties, whatever the loyalty."
- At line 333 say "held at each fitted country's spread and at the four's median".

Better fix:
- Compute the sentence in GateSection. Keep the worst |p[0]-priorSplit| and the free fit's |spatial[0]-priorSplit| per T-1, and run the same identity test as line 216 (off <= residual + 1e-12).
- Pass FitAll a counting Check instead of the no-op.
- Print the claim only when nothing failed. Otherwise print "N check(s) FAILED - run UsNationalVoteCheck" in its place.
- Measured residuals belong in this log line; the claim convention covers documents and comments, and the record (COMPLETED.md) is exempt.

Not required:
- The present-day risk is covered by the cheap bar's identity check, so counting failures is optional.

Cost of either fix:
- It changes only the US section's lines in the elec report, so the elec<N> launch must be re-run (elec789d) before the record cites it. That still moves no other country's line.

### 9. The eve checks test internal consistency, not the facts: the TookOffice conjunct cannot fail, and neither the seated House nor the term start is asserted

- **Lens:** worlds - **reviewer:** minor - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsNationalVoteCheck.cs:177

**The scenario.** Line 177 checks e.Term.TookOffice == e.TookOffice.Value. Both sides come from EconomicVote.TookOffice on the same state: RecordOverTerm passes TookOffice(country) into OverTerm (EconomicVote.cs:152-154), so once HasValue holds the comparison is always true. Nothing asserts that the term began before the eve, unlike PresidentialElectionLiveDiagnostic.cs:71 (Term.TookOffice < FirstVoteDay-1). Line 180 recomputes unified/divided from the same ParliamentSeats that Magnitudes reads. Nothing asserts that the seated House is the chamber of record on the eve, i.e. one elected before the predicted election (the 118th on 2024-11-04, the 117th on 2022-11-07). Failure case: a WorldClock or InitialSeats regression seats the House the predicted election elects. Every eve check still passes. For 2024 the figure cannot move (REP is largest either way). For 2022 the government flips from unified to divided, DEM's shift moves from -0.33 to about -0.20 pp, and only the 2024 rows are pinned (lines 84-88). So WriteReadings rewrites the card with no FAIL, and the predicted election's own result becomes an input to its own economic vote.

**The fix proposed.** Assert that WorldClock.ChamberAt(USA, e.Epoch).ElectionDay is before the predicted chamber's ElectionDay, and that usa.ParliamentSeats equals PartySystems.InitialSeats(USA, that chamber's Vintage). Replace the tautological conjunct with e.TookOffice.Value < e.Epoch, or with equality to the presidency of record's From (WorldClock.TryGovernmentAt(USA, e.Epoch).From).

**The skeptic's evidence.** CONFIRMED, the tautology. UsNationalVoteCheck.cs:488-489 calls `eve.TookOffice = EconomicVote.TookOffice(usa)` and then `eve.Shift = EconomicVote.RecordOverTerm(usa, sim.CurrentDate, out eve.Term)` on the same, unchanged state. RecordOverTerm (EconomicVote.cs:152-154) recomputes `TookOffice(country)` and passes `took.Value` to OverTerm. OverTerm (PerceivedPerformance.cs:206) returns `new TermReading(reading, tookOffice, asOf, u, p)`, storing that date unchanged. So once `e.TookOffice.HasValue` holds, `e.Term.TookOffice == e.TookOffice.Value` at :177 cannot fail. The null branch is already failed by `HasValue`. The precedent guard it stands in for is PresidentialElectionLiveDiagnostic.cs:71, `world.Term.TookOffice < world.FirstVoteDay.AddDays(-1)`. Line 180 does recompute unified/divided from the same ParliamentSeats that Magnitudes reads (EconomicVote.cs:64-66), so it checks the mapping, not which House is of record.

REFUTED IN PART, the impact. (1) Run is not silent. Run compares the whole generated block (:114-117). The block prints `EconomicVote.Describe(Term)` and the largest party (:254), i.e. "took office 2021-01-20" and "The House's largest party DEM; DEM's economic vote 0.10; DEM -0.33 pp" for 2022. So the scenario fails the cheap bar and the documents bar as STALE before WriteReadings is ever run. (2) The term-start half is covered by the pins. Both eves read the same Biden row: GovernmentRecord.cs:260 sets `FormedOn = record.AsOf`, and TookOffice walks back to `rows[at].From` (EconomicVote.cs:108-124), giving 2021-01-20 on both eves (us5w4.log:438, :442). Any regression that moves the term start also moves 2024's pinned shift of +2.50 (:84-88), so WriteReadings refuses. (3) The seated-House half has a guard in the same cheap Suite: CongressOfRecordDiagnostic (CheckSuite.cs:222). Its check (a) opens a world on 2024-12-28, after the 2024-11-05 election, and asserts the 118th. Its check (g) asserts that a US world with no player keeps the House it opened with, and the eve world sets no player. ChamberAt returns the FIRST chamber that `Holds` (Convened..Until; WorldClock.cs:45, :308). So the 2022 eve world could seat the 118th only if two rows (WorldClock.cs:281-282) were wrong together: Usa2020's Until and Usa2022's Convened, both before 2022-11-07. The finding's failure path therefore needs that double data error AND a regeneration past a red bar without reading the card diff. The finding's own numbers are right: the 2024 figure is unmoved because REP is the largest party in both the 118th and the 119th; 0.06 x 0.034 gives about -0.20 pp; only the 2024 rows are pinned. Today's facts are correct (us5w4.log:439, :443: 2024 REP divided 0.06, 2022 DEM unified 0.10). Also, the finding's suggested `ParliamentSeats == InitialSeats(USA, ChamberAt(...).Vintage)` is self-referential for an InitialSeats regression.

**The skeptic's corrected fix.** In ReadEve, keep the predicted chamber (`chamber`) on Eve. In Measure, replace the tautological conjunct on :177 with the precedent's `e.TookOffice.Value < e.Epoch` (PresidentialElectionLiveDiagnostic.cs:71). Add one fact check that does not lean on ChamberAt's date logic: the world seats the House elected at the previous House election. Take `prev` as the row before `chamber` in `WorldClock.Chambers(USA)`. Check `prev.ElectionDay < chamber.ElectionDay` and `usa.ParliamentSeats` equal to `PartySystems.InitialSeats(USA, prev.Vintage)`, read after the step at e.Day. Optionally, pin 2022's shift (-0.33) beside the 2024 rows so a regenerated card cannot move it unseen. That covers every route, including seat-table data, but it extends the declared pin set (the R-US15 figures). That is a design call, not a bug fix.

### 10. The eve reading uses the revised BLS unemployment series, not the figure published before the election (pre-existing, from §709)

- **Lens:** worlds - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/PublicationSystem.cs:75

**The scenario.** OverTerm's contract (PerceivedPerformance.cs:179-181) is 'the latest figure published BEFORE asOf: what the electorate had in hand', and SeedPreStartRecord's comment says 'as the real releases had them'. The US unemployment series, though, is BLS LNS14000000 as fetched on 2026-10-01 (ElectionsData/macro/prestart_record.md:30): seasonally adjusted, in its current, revised vintage. The 2022 eve reads Oct 2022 = 3.6 %. BLS's release of 4 Nov 2022, four days before the election, printed 3.7 %. That release is outside the repo and I did not verify it here. With 3.7 the 2022 term index would be 47.5 rather than 48.3, and DEM's shift -0.50 pp rather than -0.33. This moves the out-of-sample 2022 'miss after' column, not the 2024 pins: Jan 2021 = 6.4 had been revised before the 2024 eve, and Oct 2024 = 4.1 is the figure printed before 5 Nov 2024. It does not come from this change, but it is a figure the new card prints.

**The fix proposed.** Outside this change: for the US unemployment series, either source a real-time vintage (e.g. ALFRED) that matches the date of each eve, or state the vintage on the card's economic-vote lines.

**The skeptic's evidence.** I could not refute it. I traced the path and checked the outside figure against archived sources, read-only.

The path:
- `UsNationalVoteCheck.ReadEve` (lines 470-490) sets the epoch to the House election day less one (2022-11-07). `SetWorld` then calls `SeedPublishedHistory` → `PublicationSystem.SeedInheritedHistory` → `SeedPreStartRecord` (`PublicationSystem.cs`:66, 84-113). That seeds each month as a Final entry dated by the game's release calendar, with the value from `Generated/PreStartRecord.cs`:77 `(2022, 10, 3.6f, 7.7f)`.
- That table is the BLS series `LNS14000000`, "fetched 2026-10-01", seasonally adjusted (`prestart_record.md`:30). The saved file `raw/bls_LNS14000000_2026-10-01.json` gives 2022-10 = 3.6.
- After `AdvanceDay`, `RecordOverTerm(usa, 2022-11-08)` → `OverTerm` reads `Now` as the latest entry published before the day it is judged: October 2022, published 4 Nov.
- `us5w4.log`:442 prints "term index 48.3 ... unemployment 6.4 % (2021-01) -> 3.6 % (2022-10)". Lines 546 and 554 print "DEM -0.33 pp", and the card carries the same figures.

What the contracts say:
- `PerceivedPerformance.cs`:179-180: "the latest figure published BEFORE asOf: what the electorate had in hand".
- `PublicationSystem.cs`:75: "as the real releases had them".

What the outside record says:
- The archived BLS release `empsit_11042022` reads "The unemployment rate increased by 0.2 percentage point to 3.7 percent in October".
- ALFRED, `UNRATE`, vintage 2022-11-07: 2022-10-01 = 3.7 and 2021-01-01 = 6.4. So the 2022 figure the eve reads differs from what was published before the election.
- ALFRED, vintage 2024-11-04: 2021-01-01 = 6.4 and 2024-10-01 = 4.1. Both match the repo, so 2024 is unaffected.
- CPI-U not seasonally adjusted is not revised, so the inflation ends (1.4, 8.2, 2.4) stand.

Arithmetic, from `LowerIsBetter` (`PerceivedPerformance.cs`:81-84) and `EconomicVote.Deterioration`/`RecordShiftOf`:
- The inflation component clamps to 0, since |8.2-2| - |1.4-2| = 5.6.
- With 3.6: (50 + 2.8·50/3) / 2 = 48.3. With 3.7: 95 / 2 = 47.5.
- At the unified magnitude 0.10, DEM's shift is -0.33 pp with 3.6 and -0.50 pp with 3.7.
- With two parties, REP's two-party share absorbs DEM's shift exactly (`ApplyRecordShift`). So the 2022 "miss after" would be about -2.45 (a) and -3.17 (b), not -2.62 and -3.33.

Why only a note:
- The pins at `UsNationalVoteCheck.cs`:84-88 are 2024 only, so none fails.
- Both 2022 histories move by the same amount, so (a) is still the closer in 2022 and R-US15's ask text holds.
- The data choice comes from §709, and the ruling says to read the economic vote "through s709's US pre-start record". The staged diff makes no real-time claim of its own; a grep for in hand / real release / revis / vintage finds nothing relevant.
- The overclaim is in the existing comment at `PublicationSystem.cs`:75. `prestart_record.md` records the fetch date but does not say that seasonally adjusted rates are revised after their first print.

**The skeptic's corrected fix.** Outside this change; the data choice belongs to §709. The smallest honest fix is to declare the vintage:
- In `PublicationSystem.SeedPreStartRecord`'s comment, replace "as the real releases had them" with words saying the figures are the sources' vintage at the fetch, not the first print.
- In `ElectionsData/macro/prestart_record.md`, add a line saying that seasonally adjusted unemployment (BLS `LNS14000000`, and Eurostat `une_rt_m` for the five) is revised after its first print, so a month can differ from what the electorate had in hand, and mark it DECLARED.
- Add the same premise to the economic-vote row of the card's R-US15 table, by reference and without transcribing a figure.

If a real-time reading is wanted instead, source the US unemployment rows from ALFRED's `UNRATE` vintages, using each eve's vintage date. That would be its own family, because the 2022 eve reading would move: term index 48.3 → 47.5, DEM -0.33 → -0.50 pp. The 2024 pins and the game's USA start would not move. Leave the 2024 pins and R-US15's recommendation as they are, since neither changes.

### 11. The spread rationale is false: a spread also sets how far the two-party split moves, and US-33's nominee is not a third unit

- **Lens:** claims - **reviewer:** defect - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/docs/specs/USA_STAGE_PLAN.md:621

**The scenario.** Three places say the spread decides only what a third placed unit takes: plan 621 ('a spread decides only what a third placed unit takes'), card US_ELECTIONS.md:276-277 ('The fits differ only in their spreads, which decide what a third placed unit takes - US-33's own nominee off the party's point ... - and never the two-party split') and the class doc UsNationalVoteCheck.cs:33 ('the fits part only in what they give a third placed unit'). The card's own readings contradict this. (1) The slide column differs by fit, from 0.00 to 1.46 pp. (2) The place column shows the spreads give the split very different sensitivity to position. The same 5.85 pp move in the split (House 2018 45.55 % to House 2022 51.40 %) takes a place change of 0.0778 under Sweden's pair (0.5035 to 0.5813) but only 0.0286 under Italy's (0.4782 to 0.5068), about 2.7 times less. US-33 (plan 450) puts the nominee 'at the head of the DEM or REP ticket, with positions inside the party's range'. That moves a major party's point, which moves the dividing line, and the held spread then decides how much the two-party split changes (VoteModel.cs:104-116: Gaussian sigma, logit tau). The free fit's mean is also off the DEM-REP segment, so 'differ only in their spreads' is false even as written. The ask never says the card does not measure this response.

**The fix proposed.** Say the spread decides (1) what a third unit takes and (2) how far the two-party split moves when a party's point moves away from the fitted configuration, as US-33's nominee does; (2) is not measured here. Move US-33 out of the third-unit list and call it a moved point. Keep (iii) holding (ii), but name (2) as something the measurement cannot decide. Make the same edit in the card's identity paragraph and in the class doc.

**The skeptic's evidence.** The claim is false, but nothing it touches changes a figure, the recommendation or anything live.

What the docs say:
- Plan 621: "a spread decides only what a third placed unit takes".
- Card US_ELECTIONS.md 276-277: "The fits differ only in their spreads, which decide what a third placed unit takes - US-33's own nominee off the party's point, SP-6b's third party, a created party - and never the two-party split".
- UsNationalVoteCheck.cs 32-33: "the fits part only in what they give a third placed unit".
- Plan 217, a line this change edits, keeps "the spread deciding only a third placed unit".

US-33's nominee is not a third unit:
- Plan 450 says US-33 is "a leader at the head of the DEM or REP ticket, with positions inside the party's range".
- Plan 452 says the count "reads that nominee's position through the poll". Spec §2.6 says the same.
- So the US roster stays REP and DEM (PartySystem.cs 528-529), and the nominee moves one of those two points.

Checked numerically:
- I ported VoteModel.PredictShares to Perl (81x81 grid, Gaussian sigma, softmax over -d2/tau, wEcon 0.65, the roster's float positions). Scripts: scratchpad/spread_moved_point.pl and scratchpad/live_chain.pl.
- The port matches the card's House 2022 places exactly (0.5813 / 0.5185 / 0.5135 / 0.5068 / 0.5096). It also matches the midway shares (52.2 / 42.0 / 47.8 / 63.7 / 53.3, free 42.6).
- At each fit, I moved DEM's point toward the centre and read REP's spatial two-party share:

| fit | DEM moved 0.5 units (pp) | DEM moved 1.0 unit (pp) |
|---|---:|---:|
| Sweden's spread | -3.81 | -7.63 |
| Germany's spread | -2.47 | -4.62 |
| Poland's spread | -3.87 | -7.38 |
| Italy's spread | -6.70 | -12.78 |
| the four's median | -4.90 | -9.35 |
| free | -3.04 | not run |

- So the spread decides how far the two-party split moves when a party's point moves, by up to about 2.7 times between spreads. That contradicts "never the two-party split".
- The card's own place column shows the same thing. The House 2018 to 2022 change of 5.85 pp needs a place change of 0.0778 under Sweden's spread, 0.0776 under Germany's and 0.0286 under Italy's.

"Differ only in their spreads" is also false as written:
- The free fit's mean (6.000, 6.500) is off the DEM-REP segment. The segment's soc coordinate at econ 6.0 is about 5.38.
- The held fits' means sit at different places along the segment.

Why this is minor, not a defect:
- Option (iii) already names US-33 among the items the choice is left to, so the recommendation still holds.
- Nothing is live. TryElectorate's doc says "THE ELECTORATE DOES NOT YET MOVE WITH THE SIMULATION", and no live code writes the roster's positions.
- In the live chain the effect is damped by loyalty. Preference blends lambda*prior with (1-lambda)*spatial (PreferenceModel.cs 165), and ToCompatScale plus PersuadedShares return the spatial shares unchanged.
- At the card's loyalty (94.5 / 94.1), a 0.5-unit move shifts the poll by -0.14 pp (Germany) to -0.38 pp (Italy). A 1.0-unit move shifts it by -0.26 to -0.73 pp. That is tenths of a point, but the size still depends on the spread.

The finding's slide argument (point 1) is weak:
- The slide is a counterfactual move of the mean along the dividing line. With an untruncated Gaussian that move changes nothing; the nonzero slides come from cutting the Gaussian at the [0,10] grid edge.
- The electorate never moves in play, so the slide is a degeneracy measure, not something the spread decides in the game.
- The place-column argument and the US-33 argument stand.

**The skeptic's corrected fix.** Change the wording in four places, keep the recommendation, and change no code logic.

1. Plan 621: say that at T−1's points every spread holds T−1's split, and that a spread decides two things:
   - what a third placed unit takes (the card's midway unit shows how much);
   - how far the two-party split moves when a party's point moves away from the fitted points, as US-33's nominee does. The card does not measure this second effect.

2. Plan (iii): keep "(iii) holding (ii)". Reword the list as "the first items that leave the fitted configuration". Name US-33's own nominee as a moved point of DEM or REP, and SP-6b's third party and a created party as third units. Say that the moved-point response is to be measured at US-33.

3. Card 276-277: say that the fits give the same split at T−1's points and differ in their spread and in where their means sit. Then say what the spread decides, as in step 1. Drop "never the two-party split" and take US-33 out of the third-unit list.

4. Class doc UsNationalVoteCheck.cs 32-33: say that the fits give the same poll and that what they give a third unit, and how the split answers a moved point, are left to the spread. Say that the second is not measured here.

Optionally, fix plan 217's "the spread deciding only a third placed unit" (this change edits that line) the same way, and plan 216's wEcon line "inert for the two-party split, decisive for a third placed unit", which predates this change. Or add a generated column with the split's move when one party's point moves one unit toward the other, so the ask can cite a figure for it.

### 12. The E1 text says 'the last column divides it out', but the division is in the second-to-last column

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsNationalVoteCheck.cs:301

**The scenario.** The generated line at card US_ELECTIONS.md:346 ends 'so a poll exact on the two-party split gives both factors that figure - the last column divides it out.' The table header that follows (code line 304) puts 'factors ÷ the nominees' share (R / D)' in the sixth column and 'the poll's two-party miss on the presidential 2024 (pp)' in the last. A reader who goes to the last column finds the miss, not the divided factors.

**The fix proposed.** Name the column ('the column "factors ÷ the nominees' share" divides it out'), or move that column to the end. Then regenerate.

**The skeptic's evidence.** The finding holds. I could not find a reading of the sentence that makes it true.

The prose, at Assets/Editor/UsNationalVoteCheck.cs:301:
  "...The nominees together hold {4} of the vote, so a poll exact on the two-party split gives both factors that figure - the last column divides it out."

The header, at :304, has seven columns:
  "| history | poll R / D | Trump's factor | Harris's factor | the field's sum | factors ÷ the nominees' share (R / D) | the poll's two-party miss on the presidential 2024 (pp) |"

The row, at :313, puts the division in cell 6 and the miss in cell 7:
  "| {0} | {1} / {2} | {3:0.000000} | {4:0.000000} | {5:0.000000} | {6:0.0000} / {7:0.0000} | {8} |"
  ... fT / (trump + harris), fH / (trump + harris), Pp(100.0 * (p[0] - record))

The sentence's logic needs the division column. Since fT = trump / p[0], a poll exact on the two-party split (p[0] = trump/(trump+harris)) gives fT = trump+harris, which is 98.12 %. Dividing by (trump+harris) brings that to 1. Only cell 6 does this division, and its own header says so. Cell 7 is the signed two-party miss, p[0] less the record, and it does not divide anything out.

The card matches the code. In docs/reference/US_ELECTIONS.md, line 346 ends "- the last column divides it out." Line 348 is the same seven-column header. In the rows, (a) has "1.0379 / 0.9637 | -1.85" and (b) has "1.1220 / 0.8992 | -5.52". So a reader who follows "the last column" lands on -1.85 and -5.52, which are misses, not the divided factors. n789a.log:560 confirms that the card on disk is the block the code emits ("say what this measures - 69 lines").

Why minor and not higher:
- It is a wrong pointer in generated prose only.
- No figure, check, digest or the R-US15 ask depends on it.
- The correct column's header names the operation, so a careful reader can recover.
- It is still a false statement in the record this change delivers, and fixing it costs a Unity run (see below).

**The skeptic's corrected fix.** 1. In UsNationalVoteCheck.cs:301, point at the column by its header rather than its position. For example: "...gives both factors that figure - the column of the factors ÷ the nominees' share divides it out." Moving that column to the end of the header at :304 and the row format at :313 would work too.
2. Then run -executeMethod PoliSim.EditorTools.UsNationalVoteCheck.WriteReadings. A source edit alone is not enough. Run compares the whole block text (onDisk == CardBlock(card, digest) at :113-:117), and the source-digest covers only the inputs, not the prose. So after the edit, the card fails Run as STALE until it is regenerated.
3. Do not hand-edit the card. Its block is marked DO NOT EDIT BY HAND.

### 13. 'It widens both histories' 2024 misses by the same amount' is not what the card shows

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/docs/specs/USA_STAGE_PLAN.md:627

**The scenario.** On the card's 2024 rows, (a) on the House goes from +0.08 to -2.42, so its absolute miss grows by 2.34. (a) on the presidential contest goes from +0.65 to -1.85, a growth of 1.20. (b) goes from -3.02 to -5.52 and from -3.59 to -6.09, a growth of 2.50 each. Only the signed shift is shared. Because (a)'s misses before the shift are positive and (b)'s are negative, a shared DEM shift is not neutral between the two histories: a smaller one would have narrowed (a)'s miss.

**The fix proposed.** Write 'it moves both histories' 2024 polls by the same amount toward DEM (the card's term line)', with no figure, and drop 'widens ... by the same amount'.

**The skeptic's evidence.** The flagged sentence is at docs/specs/USA_STAGE_PLAN.md:627 (staged): "...so 2022's inflation peak is not in it, and it widens both histories' 2024 misses by the same amount."

The card defines its misses as signed: "REP's predicted share of the two parties less the record's". The 2024 rows below are in the staged generated block of docs/reference/US_ELECTIONS.md and in the run log G:/UNITY/Projects/PoliSim-captures/logs/us5w4.log, lines 526-539:
- (a) House 2022 against 2020, scored on House 2024 (own): +0.08 before, -2.42 after. Unrounded, the poll goes from 0.5140 to 0.4890 against a record of 0.5132. The miss grows from 0.08 to 2.42, an increase of 2.34.
- (a), scored on president 2024: +0.65 before, -1.85 after. The miss grows by 1.20.
- (b) presidential 2020 against 2016, own contest: -3.02 before, -5.52 after. The miss grows by 2.50.
- (b), scored on House 2024: -3.59 before, -6.09 after. The miss grows by 2.50.

UsNationalVoteCheck.cs lines 84-88 pin these figures: (2024,"(a)","+0.08","-2.42","+2.50") and (2024,"(b)","-3.02","-5.52","+2.50"). So the mismatch is locked in. A refreshed card cannot remove it without a pin failing.

What both histories really share is one shift: DEM +2.50 pp from the same 2024 eve world (UsNationalVoteCheck.cs lines 222-223: `after = EconomicVote.ApplyRecordShift(keys, poll, eve.Shift)` with `eve = eves[c.Year]`). So the signed miss moves by -2.50 on every row. "Widens" is about size, though, and the sizes do not grow by the same amount. (a)'s misses before the shift are positive, so the DEM shift first closes them, then crosses zero. (b)'s are negative, so the whole shift adds to them.

The sentence also suggests the economic vote is neutral between the histories, and it is not. (a)'s lead over (b) goes from 2.94 to 3.10 on the own contests and from 2.37 to 3.67 on the presidential contest.

I could not refute the finding. "Widens both" is true. "By the same amount" is false on the card's own figures. The impact is limited:
- The line is labelled "Noted, not asked".
- The recommendation's rule, "(a)'s 2024 miss is below (b)'s ... with the economic vote and without it", holds on every row.
- No code or live path is involved.

It is still a false derived claim in the R-US15 ask that the project owner will read beside the card. That is the class of error the claim convention targets, so I keep the grade at minor.

**The skeptic's corrected fix.** At docs/specs/USA_STAGE_PLAN.md:627, replace "and it widens both histories' 2024 misses by the same amount." with: "and since both histories read the same eve world, it moves both 2024 polls toward DEM by the same amount (the card's term line). It widens every 2024 miss the card prints, though not equally: (a)'s misses before it lean REP, so part of the shift closes them first (the card's before and after columns)." This uses no transcribed figures, so it follows the claim convention. The shorter alternative also works: say only that it moves both histories' 2024 polls toward DEM by the same amount, and drop "widens ... by the same amount".

### 14. The ask does not address R-US15's option (c), and does not say the out-of-sample cases split

- **Lens:** claims - **reviewer:** minor - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/docs/specs/USA_STAGE_PLAN.md:620

**The scenario.** Elias is asked to choose among (a), (b) and (c). The rule applied ('unless its national miss is well above (b)'s') compares only (a) and (b), and the ask says nothing about (c). The card scores each history on both contests, so it bears on (c). In 2024 the House history predicts the presidential contest better (+0.65 / -1.85) than the presidential history does (-3.02 / -5.52), so (c) does worse than (a). In 2020, (b) beats (a) on both contests, and (a)'s own miss (-2.89) is about 2.5 times (b)'s (+1.16). US-6 makes '2020 from 2016' its out-of-sample acceptance case (plan 239). The ask states the direction ('(b) in 2020') but not the consequence: beyond 2024 the measurement cannot rank (a) above (b).

**The fix proposed.** Add one sentence on what the card's cross-scored rows say about (c). State that the two out-of-sample cases split, so the recommendation rests on 2024 alone, by the rule.

**The skeptic's evidence.** Only the first half survives, and only as a missing sentence. Nothing the ask says is false, and its recommendation holds against (c).

REFUTED half ("does not say the out-of-sample cases split"): docs/specs/USA_STAGE_PLAN.md:620 reads "- **The vote: code recommends (a)**, by the rule above. (a)'s 2024 miss is below (b)'s on its own contest and on the presidential one, with the economic vote and without it. Out of sample, (a) is the closer in 2022 and (b) in 2020." So the split is stated, and the recommendation is said to rest on 2024 by the rule. The second sentence of the proposed fix is already in the ask.

TRUE half: grep finds "(c)" and "one per contest" only in the option lists (plan line 615; US_ELECTIONS.md lines 256-257). They appear nowhere in the ask (plan lines 619-628) or in the generated block. The rule the owner ruled (plan line 617, "Code expects to recommend (a), unless its national miss is well above (b)'s") compares only (a) and (b), and the ask applies it as written.

Why only a note:
1. The ask's own words "and on the presidential one" are the 2024 comparison that decides (c) against (a). Option (c) predicts the presidency from the presidential history: +0.65 for (a) against -3.02 for (b), and -1.85 against -5.52 after the economic vote.
2. The card prints every cross-scored row. The block is current: n789a.log line 560 "say what this measures - 69 lines"; the pins pass at lines 505 and 515.
3. On those rows, (c) takes the House row from (a) and the presidential row from (b).
   - 2024: it matches (a) on the House and is farther on the presidency.
   - 2022: it is (a).
   - 2020: it matches (b) on the presidency and is farther on the House (-2.89 against +0.44).
   - (c) is never closer than the year's better single history. Summed absolute misses before the economic vote: (a) 8.75, (c) 10.10, (b) 11.88 pp. Naming (c) cannot change the ruling.
4. The reviewer's 2020 ratio is right (2.89 / 1.16 is about 2.5). But "its national miss" in the rule is the item's 2024 prediction (plan lines 203-206; "predict 2024 ... prints the national miss per history"). 2020 is a DECLARED out-of-sample case (plan lines 222-224), and the claim convention bars transcribing the ratio.

A point the finding brushes past but does not fix: US-6's acceptance (plan line 239, "the out-of-sample case (US-5's 2020 from 2016)") is (b)'s 2020 case by plan line 224. Under the recommended (a), the matching case is (a)'s 2020 row, the one (b) beats. The ask does not name this consequence.

**The skeptic's corrected fix.** Add one qualitative clause to the vote bullet at docs/specs/USA_STAGE_PLAN.md:620, with no figures transcribed. For example, after "on the presidential one": "- so (c), each contest from its own history, is farther than (a) in 2024 (it reads the presidency from (b)); on the card's rows (c) is never closer than the year's better single history: it matches that history on one contest and is farther on the other in 2024 and 2020, and in 2022 it is (a)." Do not add the finding's second sentence: line 620 already states the out-of-sample split and that the recommendation is by the rule on 2024. Optionally, add one clause noting that US-6's acceptance case "2020 from 2016" (line 239) is (b)'s 2020 case (line 224), so a ruling of (a) moves US-6's out-of-sample case to (a)'s 2020 row.

### 15. The Built line types a count, 'four figures pinned', and the count is wrong

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/docs/specs/USA_STAGE_PLAN.md:211

**The scenario.** The Pins array (UsNationalVoteCheck.cs:84-88) pins five quantities: four misses and the economic vote, which appears as Shift on both rows. Its own doc (lines 81-82) says 'and the economic vote itself'. 'Four' therefore undercounts. It is also a DERIVED count typed into a non-exempt document, against the claim convention. US-4's Built line says only 'pinned'.

**The fix proposed.** Write 'pinned' with no count, or point to the instrument's pins.

**The skeptic's evidence.** I could not refute it. The count is wrong however you count, and writing it in this document breaks the claim convention.

1. What the plan says. docs/specs/USA_STAGE_PLAN.md:211 (staged; the working tree has no other edits to this file) reads: "`UsNationalVoteCheck` (the cheap bar and the documents bar; four figures pinned; a stale block fails it)".

2. What the code pins. Assets/Editor/UsNationalVoteCheck.cs:84-88 has two rows of three strings each:
   (2024, "(a)", "+0.08", "-2.42", "+2.50"),
   (2024, "(b)", "-3.02", "-5.52", "+2.50")
   Line 243 compares all three strings on each row: `Check(b == pin.Before && a == pin.After && s == pin.Shift, ...)`. That is six pinned cells and five distinct values. The fifth value is the economic vote, +2.50, checked on both rows.
   - The code's own summary at :81-82 counts it among the figures: "2024's miss by each history on its own contest, before and after the economic vote ... and the economic vote itself (DEM, points)".
   - The logged runs agree. us5w4.log:529 and :539 (n789a.log:505 and :515) print "(pinned +0.08, -2.42, +2.50)" and "(pinned -3.02, -5.52, +2.50)".
   - No other literal pins exist in the file. The other checks at :164-409 compare against constants or tolerances, not pinned figures.
   - Every reading gives a number other than four: six cells, five distinct values, two rows, or three independent quantities (each "after" is "before" minus 2.50).
   - The likely intent was "the four misses". The text as written undercounts and contradicts the code's own summary.

3. Why the count is not allowed in this document. CLAUDE.md's claim convention lists "a count ... a 'there are N of X'" as DERIVED. It says "Nobody transcribes, anywhere, in any file", and exempts only COMPLETED.md.
   - USA_STAGE_PLAN.md is a live spec, and no check exempts it.
   - DocumentClaimCheck matches only backticked `Type.Member` references (its `Reference` regex), so no bar can catch this count. It is wrong on its first day and becomes wrong again whenever a pin is added.
   - The precedent in the same file, US-4's Built line at :197, says only "`UsStateSwingCheck` (the cheap bar; pinned; a stale block fails it)".

Severity stays minor: the error is one word of prose, behaviour is unaffected, and the shipped pins are correct.

**The skeptic's corrected fix.** On docs/specs/USA_STAGE_PLAN.md:211, delete the count and match US-4's Built line: "`UsNationalVoteCheck` (the cheap bar and the documents bar; pinned; a stale block fails it)". If the line should say what is pinned, describe it without a number, for example "2024's miss by each history before and after the economic vote, and the economic vote, pinned". If a count is wanted at all, it belongs in COMPLETED.md §789 (exempt, not yet written), and it must be correct there: five figures, with the economic vote (DEM +2.50 pp) checked on both rows, six cells in all.

### 16. GateReRun's US section prints a closing claim its own lines contradict, and never checks it

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsNationalVoteCheck.cs:354

**The scenario.** In elec789c.log (the run on the staged tree), the US section prints 'free -3.66 -> -3.67' (2022 (b)) and 'free +1.15 -> +1.16' (2020 (b)) just above 'Every fit holds its T-1 split, so national and +§8 agree'. The free fit holds T-1 only to within Calibrate's residual (plan 215: 'within its grid's residual for the free fit'). GateSection passes a no-op Check to FitAll (line 340) and tests neither the identity nor the REP-first roster order, so the sentence prints whatever the numbers say. The header (line 333) says 'held at each fitted country's spread', but every line also prints 'the four's median'.

**The fix proposed.** Write 'agree to within the free fit's grid residual', and name the four's median in the header. Better: check the identity and print the result.

**The skeptic's evidence.** The finding's scenario does happen, but one half of it is overstated.

1. The closing sentence is contradicted at the printed precision. This happens in the staged tree's run. elec789c.log is the post-change run: its lines carry "the four's median", which elec789b's lines lack. Line 1338 prints `2022 (b) ... | free -3.66 -> -3.67 | Sweden's spread -3.67 -> -3.67 ...` and line 1340 prints `2020 (b) ... | free +1.15 -> +1.16 | ...`. Directly below them comes line 354, unconditional: `sb.Append("  Every fit holds its T-1 split, so national and +§8 agree: the vote model returns the prior with two parties, whatever the loyalty.\n");`

   Why the numbers differ: line 349 prints `Pp(100.0 * (spatial[0] - record))` as "national" and `Pp(100.0 * (p[0] - record))` as "+§8".
   - For a held fit, `spatial[0]` equals T-1 to 1e-9 (HeldFit's bisection, line 409).
   - For the free fit it is off by Calibrate's residual. n789a.log line 436 gives `President 2020: ... MAD 0.0029 pp` and line 481 gives `President 2016: ... MAD 0.0018 pp`.
   - The preference layer pulls `p` back to within about 1e-6 of the prior. n789a.log line 524: `2022 (b), free: the poll 0.477303 is no farther from the prior 0.477302 than the fit's residual 2.9e-5`. Algebraically, p_R − π_R ≈ ε·[(1−λ_R) − π_R(λ_D−λ_R)], from PreferenceModel.Preference lines 165–169, where ToCompatScale followed by PersuadedShares returns the spatial shares unchanged.
   - So "national" and "+§8" differ by about the residual, and on two lines that difference crosses a 0.01 rounding boundary.

   The design and the card both carry the qualification the gate sentence drops:
   - Plan line 214 (the finding cites 215): "exactly for a held fit, which is solved, and within its grid's residual for the free fit".
   - Measure line 282 (the card): "Every fit holds its T-1 split (free: within Calibrate's grid; held: solved)".

2. The header is stale. Line 333 reads `free (VoteModel.Calibrate) and held at each fitted country's spread`. The text is identical in elec789b, which had no median fit. FitAll line 395 added `"the four's median"` with the measurement (plan line 216: "Added with the measurement (§789): a fifth"). The header was not updated, so it does not describe the fit behind R-US15's recommended holding value (ii) (plan line 623), even though every line prints that fit.

3. "Never checks it" is overstated, and this part is refuted.
   - It is true that GateSection itself checks nothing. Line 340 passes `(ok, what) => { }` to FitAll, and there is no identity test and no REP-first roster test.
   - But GateSection recomputes exactly what Run computes, and Run checks all of it:
     - same `RealRoster(CountryId.USA)` (lines 162/329), `UsEconomicWeight`, `FreeFits`, `FitAll` and chain (lines 213–214 / 347–348)
     - the identity on every fit (line 216)
     - the free MAD < 0.05 pp (line 382)
     - bracketing and the 1e-9 split (lines 405, 409)
     - the roster order (line 164)
   - Run sits in both the cheap and the documents tables (CheckSuite diff). The log figures match the card's (-3.67, +1.16, +0.08, -3.02).
   - So a broken identity fails the bar. The sentence can only be wrong in a log whose tree fails the cheap bar. The visible disagreement is capped near 0.05 pp by line 382, against histories that differ by about 3 pp.

Re-graded as minor: the problem is the wording of R-US15's evidence (an unqualified summary and a stale header). No figure, pin, card line, gate line or verdict is wrong.

**The skeptic's corrected fix.** Line 333: name every fit the lines print: "free (VoteModel.Calibrate), held at each fitted country's spread and at the four's median;".

Line 354: either qualify the sentence, or better, compute it. To qualify: "Every held fit gives its T-1 split exactly, the free fit within Calibrate's grid residual (UsNationalVoteCheck checks the identity on each), so national and +§8 agree to within that residual: ...".

Preferred: make GateSection measure the sentence rather than assert it. In the loop at lines 345–350, take `priorSplit = TwoParty(prior)`. Per fit, test `Math.Abs(p[0] - priorSplit) <= Math.Abs(spatial[0] - priorSplit) + 1e-12` and track the worst `Math.Abs(p[0] - spatial[0])`. Replace the no-op lambda at line 340 with one that counts failed FitAll checks. Then print "the identity holds on every fit; national and +§8 within {worst:0.0e+0}", or "IDENTITY BROKEN on N fits - see UsNationalVoteCheck". The figure is emitted at run time, so the claim convention is respected.

Duplicating the roster or the other FitAll checks is not needed, because Run already holds them in the cheap bar.

### 17. The median fit is dated 'DECLARED before the build' in the class doc and has no row in the card's table of parts

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsNationalVoteCheck.cs:20

**The scenario.** Class doc lines 20-22: 'The design is DECLARED before the build ... held at the σ and τ of each country ... and at the four's median'. The plan (216) marks the median 'Added with the measurement (§789)'. The card's 'The fit' row (US_ELECTIONS.md:266, basis DECLARED) lists only the four countries. Yet the median is option (ii), the spread R-US15 recommends holding. A reader of the card's premises cannot find its basis, and the class doc gives it the wrong date.

**The fix proposed.** In the class doc, say the median was added with the measurement (§789). Add the median to the card's fit row, with that basis.

**The skeptic's evidence.** The finding holds. I could not refute either half.

1. The class doc gives the median the wrong date. Staged UsNationalVoteCheck.cs:20-22 (the index copy matches the working tree) says: "The design is DECLARED before the build (`docs/specs/USA_STAGE_PLAN.md`, US-5): ... free by Calibrate, and held at the σ and τ of each country <see cref="PartySystems.TryElectorate"/> holds and at the four's median". The staged plan at USA_STAGE_PLAN.md:216 says the opposite: "Added with the measurement (§789): a fifth, held at the four's median (σ and τ each the median of the four)". The parent commit confirms the plan. At 40583b01 the plan's declared design (lines 214-215) had "Four held fits" and "the five fits' different spreads", which is the free fit plus four, with no median. The §788 record says the same: "The design, DECLARED before any code ... held at the σ and τ of each country PartySystems.TryElectorate holds", again with no median. So the class doc's sentence puts the median inside the design declared before the build. It was not. The plan marks the date, but the class doc's summary drops it.

2. The card's premises table has no median. US_ELECTIONS.md:266 is the 'The fit' row, basis DECLARED. Its held fits are only "the σ and τ of each country `PartySystems.TryElectorate` holds (Sweden, Germany, Poland, Italy)". The median appears only inside the GENERATED block: the prose at line 307, written by UsNationalVoteCheck.cs:262, and the rows at 316/322/328/334/340. None of these gives a basis or a date. Yet the plan's R-US15 ask (lines 619-627) recommends "(iii), holding (ii)", and (ii) is the four's median. The spread the ask recommends holding is therefore the one fit with no basis on the card. The code (FitAll, lines 377-395) adds it as a fifth fit next to the four.

Why minor and not a defect: no figure, check or behaviour is wrong. The plan, which is where the design lives, tells the truth, and nothing in the bar is affected. Still, in a repo whose rules turn on what was declared before the build and on the backtest direction, a misdated premise that the ask recommends holding is more than a note.

The fix does not stale the generated block. Its stamp, per lines 157-158 and 320, is a digest of the catalog's source digests and the held electorates. It does not cover the class source or the hand-written card text, so the fix needs no WriteReadings run.

**The skeptic's corrected fix.** UsNationalVoteCheck.cs:20-22: take "and at the four's median" out of the "DECLARED before the build" sentence. Add a separate sentence after it: "Added with the measurement (§789), not declared before it: a fifth fit held at the four's median σ and τ, R-US15's candidate (ii)."

US_ELECTIONS.md:266, 'The fit' row: add the median to the held fits, for example "...; and, added with the measurement (§789), held at the four's median (σ and τ each the median of the four), R-US15's candidate (ii)". Change the row's basis to say "DECLARED (§788); the median DECLARED with the measurement (§789)". You could instead give the median its own row with that basis.

Keep both edits outside the GENERATED block. The block's digest does not cover them, so no WriteReadings run or Unity bar is needed for staleness. Only the comment-only source tier applies to the .cs edit.

### 18. 'None of the four fits lies inside Calibrate's grid' is a derived claim nothing prints or checks

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/docs/specs/USA_STAGE_PLAN.md:626

**The scenario.** Plan 216 and 626 carry it. It is true today: Calibrate's grid runs μ 3-7, σ 1-3.5 and τ 0.5-16 (VoteModel.cs:166-172), and each pair has a value on an edge (Sweden τ 0.50; Germany σ 1.00 and τ 16; Poland μsoc 7.00; Italy σ 1.00 and μsoc 7.00; PartySystem.cs:666-673). But the card's block does not print it and no check asserts it. A refit, or a wider grid, would make it false and the plan would not notice. It is also the stated reason for option (ii).

**The fix proposed.** Have the instrument print each held pair's grid-edge parameters in the block (or check them), and cite the block.

**The skeptic's evidence.** I could not refute this finding. Every part of it checks out against the staged tree.

1. **The claim is new in this diff and appears twice.** USA_STAGE_PLAN.md:216 ends: "since none of the four fits lies inside `VoteModel.Calibrate`'s grid on all four of its parameters". Line 626 says: "each is its own election's fit, and none lies inside `VoteModel.Calibrate`'s grid on all four parameters". At both places it is the stated reason for option (ii), and so for the recommended (iii)-holding-(ii).

2. **It is true today.** The grid is in VoteModel.cs:
   - line 166: `for (double mu = 3.0; mu <= 7.0001; mu += 0.25)`, and line 168 is the same for μ soc;
   - line 170: σ in `{ 1.0, 1.5, 2.0, 2.5, 3.0, 3.5 }`;
   - line 172: τ in `{ 0.5, 1.0, 2.0, 4.0, 8.0, 16.0 }`.

   The four electorates are in PartySystem.cs, in the order (μ econ, μ soc, σ, τ). Each has at least one parameter on a bound:

   | Country | Line | Electorate | On a bound |
   |---|---|---|---|
   | Sweden | 667 | 3.25, 6.25, 3.00, 0.50 | τ 0.50 (floor) |
   | Germany | 669 | 4.50, 6.50, 1.00, 16.00 | σ 1.00 (floor), τ 16.00 (ceiling) |
   | Poland | 671 | 3.50, 7.00, 1.50, 8.00 | μ soc 7.00 (ceiling) |
   | Italy | 673 | 4.25, 7.00, 1.00, 4.00 | σ 1.00 (floor), μ soc 7.00 (ceiling) |

3. **Nothing prints it.** The card's fit table (UsNationalVoteCheck.cs:262-279) prints `fit.Electorate`'s μ, σ and τ. For a held fit, that μ is the US mean solved by `Along()`, not the country's own. In the card, Poland's spread shows μ 6.041, 5.435 and σ 1.50, τ 8.00, both interior. Poland's only bound (μ soc 7.00) appears nowhere in the block. The grid's bounds are not printed either. In us5w4.log, the only grid line is "Calibrate's free fit within its grid's reach - MAD".

4. **Nothing checks it.** The checks in `FitAll`/`HeldFit` (lines 382, 388, 405, 409) cover the free fit's MAD, that each electorate is held, the bracket, and the split to 1e-9. None compares a country's electorate with the grid.

5. **A change in the code can make it false without anything noticing.**
   - The block's digest (lines 319-321) includes the held electorates. A refit therefore makes the card stale and fails `Run`, but rewriting the card does not touch the plan.
   - The grid is not in the digest. Raising μ's ceiling would put Poland inside on all four parameters, so 216 and 626 would become false. The card might stay fresh, and no check would fail.
   - This breaks CLAUDE.md:37-41 (DERIVED includes "there are N of X"; "Nobody transcribes"). It also breaks the plan's own head (line 3: "this plan carries pointers, never ... counts of code"). DocumentClaimCheck only resolves backticked `Type.Member` references, so it cannot catch this.

6. **Line 216 is also ambiguous.** It follows "Four held fits", so "the four fits" there most naturally means the four held US fits. Read that way, Poland's held fit (μ near 5.9/5.3, σ 1.50, τ 8.00) is inside on all four, and the sentence is false. The meaning that is true, the countries' own fits, is the one at 626.

**Severity: note.** The claim is true today, has no runtime effect, and is a single claim. The s788 review (Reviews/2026-10-06_s788_us5_house_returns.md, findings 24 and 25) graded single transcribed derived claims as notes. It stays at note even though the claim is a reason given to Elias.

**The skeptic's corrected fix.** This is a wording fix and needs no code change.

- **At 216:** drop "since none of the four fits lies inside `VoteModel.Calibrate`'s grid on all four of its parameters". End the clause at "a candidate for R-US15 that favours no one country".
- **At 626:** replace "and none lies inside `VoteModel.Calibrate`'s grid on all four parameters" with a pointer, for example "each is its own election's fit on `VoteModel.Calibrate`'s grid, as `PartySystems.TryElectorate` holds it". The recommendation already rests on the card's claim that every spread holds T-1's split.

If the grid-edge argument should stay as a reason for (ii), it has to be generated rather than written by hand:
1. `WriteReadings` prints each held country's whole `TryElectorate` electorate (μ econ, μ soc, σ, τ) and names the parameters that sit on Calibrate's bounds.
2. The bounds come from grid axes that `VoteModel` exposes and that `Calibrate` itself loops over, so there is one copy. Only Editor code calls `Calibrate`, so the play path is unaffected, though the change moves the commit's tier.
3. A check asserts that the printed statement matches.
4. The plan then cites the card's block instead of stating the conclusion.

Either way, at 216 say that "the four" means the countries' own fits, not the four held US fits. Read the second way, Poland's held fit is inside the grid on all four parameters.

### 19. 'Each country TryElectorate holds' points to a set the code hard-codes and never checks

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsNationalVoteCheck.cs:78

**The scenario.** The Held doc (line 78), the class doc (22), the card (266) and the plan (216) all describe the held fits by reference to TryElectorate's set. The code fixes the list at {Sweden, Germany, Poland, Italy}, and FitAll only checks that those four are held. US-6 (plan 233) adds the USA to TryElectorate. After that, every one of these sentences names five countries while the instrument holds four, and no check fails.

**The fix proposed.** Phrase it as 'the four fitted countries (Sweden, Germany, Poland, Italy)', as the ruling does. Or check that Held equals TryElectorate's set less the USA.

**The skeptic's evidence.** The finding holds as a latent drift. It only bites once US-6 lands, and only in text. Everything is true on today's tree.

Today: PartySystem.cs:662-677 `TryElectorate` has cases for exactly Sweden, Germany, Poland and Italy, and `default: return false`. UsNationalVoteCheck.cs:79 types the same four: `Held = { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy }`. So each sentence is accurate now.

The failure path, traced:
- Plan line 233 (US-6, the item R-US15 blocks): "`PartySystems.TryElectorate` and `PartySystems.TryRealHistory` gain the USA in R-US15's unit". Plan line 113 says why: NationalElection.cs:131 reads `TryElectorate` for the live poll.
- After that change, `Held` still lists four. FitAll (lines 385-393) only checks each listed country, via `Check(has, "{1}'s electorate is held")`. Nothing compares `Held` with TryElectorate's set.
- The digest (lines 318-321) hashes only `Held`'s four electorates. Adding the USA changes neither the digest nor the generated block, so Run stays green. n789a.log shows it and DocumentClaimCheck passing on this tree.
- A grep of Assets finds no other check that pins TryElectorate's set. The only nearby text is PartySystem.cs:653's prose ("ONLY THE FOUR BACKTESTED COUNTRIES HAVE ONE").
- Result: line 78 ("The held spreads: the countries whose electorates <see cref="PartySystems.TryElectorate"/> holds, read there at run time") would then mean five countries while the code holds four.

This fails the test at the head of CLAUDE.md: "the code can change freely and no document becomes wrong — only incomplete". The sentence is written in the REFERENCED form ("whatever X holds"). But the set really lives in the typed `Held` array, so the pointer aims at the wrong source. It is right today only because the two lists happen to match.

Corrections to the finding:
1. "Every one of these sentences names five countries" overstates it. Line 78 is the clean case. Line 22 is pinned in part by "and at the four's median". Card line 266 lists "(Sweden, Germany, Poland, Italy)" explicitly. Plan line 216 says "Four held fits". After US-6 those three would contradict themselves rather than name five.
2. Plan line 216's "one per country `PartySystems.TryElectorate` holds" was already in the s788 design. This diff only adds the median sentence after it.
3. The code's behaviour is correct. Typing four is what the ruling ("the four fitted countries'") requires. Reading the set from TryElectorate at run time would wrongly add the USA's own spread after US-6. Only the wording and the missing guard are at fault.

Severity stays at note: nothing is wrong at run time, no figure moves, and every generated table lists its four "X's spread" rows by name.

**The skeptic's corrected fix.** 1. Point the wording at the ruling's set, not at TryElectorate's domain. Do not list the names in the comment directly above the array: a list of names is a DERIVED claim under the convention.
   - Line 78: "The held spreads: the ruling's four fitted countries (DECLARED, US-5), typed here; each one's σ and τ read from <see cref="PartySystems.TryElectorate"/> at run time."
   - Line 22: "held at the σ and τ of each of the four fitted countries (<see cref="Held"/>, read from TryElectorate at run time) and at the four's median".
   - Card line 266: "Held: the σ and τ of each of the four fitted countries (Sweden, Germany, Poland, Italy), read from `PartySystems.TryElectorate` at run time".
   - The generated line from code line 262 can say "each fitted country's σ and τ, read from `PartySystems.TryElectorate`". Plan line 216 is s788's declared design; touch it only to say "one per fitted country".

2. Add a guard in Measure so the reference cannot drift silently. For example:
   `var fitted = ((CountryId[])Enum.GetValues(typeof(CountryId))).Where(id => id != CountryId.USA && PartySystems.TryElectorate(id, out _, out _)).OrderBy(x => x);`
   `Check(fitted.SequenceEqual(Held.OrderBy(x => x)), "the held countries are TryElectorate's, less the USA: ...");`
   It keeps passing after US-6. If a fifth country is ever fitted (France), it fails and puts that question to a ruling instead of leaving the country out silently.

### 20. The card's loyalty row drops the plan's 'as near as the sources allow'

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/docs/reference/US_ELECTIONS.md:267

**The scenario.** The card says 'each party's share of all votes cast - the four countries' basis'. The plan's declared design (218) adds 'as near as the sources allow' for a reason. The House denominator is the Clerk's Total, which includes non-votes and Maine's re-counted ballots (the catalog's House doc). Germany's history, by contrast, is of valid votes (PartySystem.cs:610, 'valid 46,298,387'). This does not change any miss (the identity), but it does change the printed loyalty column.

**The fix proposed.** Restore the qualifier, or point to the plan's loyalty bullet.

**The skeptic's evidence.** I could not refute it. The card states the basis without the qualifier, the code divides the House shares by a denominator that is not the four countries' basis, and that denominator changes the printed loyalty column (though no miss).

The card and the plan:
- docs/reference/US_ELECTIONS.md:267 reads: "| The loyalty | T-1 against T-2 (`LoyaltyModel.PartyLoyalties`), each party's share of all votes cast - the four countries' basis; ... | DECLARED |". It names no denominator.
- Nothing else in the card's R-US15 section mentions the non-votes in the Clerk's Total or Maine's lines. Line 31's "Maine's ranked-choice count" belongs to the presidential R-US14 table.
- docs/specs/USA_STAGE_PLAN.md:218 (the declared design) reads: "the four countries' basis as near as the sources allow: the presidential history's share of the FEC's total, the House history's of the Clerk's Total, which carries the non-votes and Maine's re-counted ballots (below) ... the denominator moves no miss."

The code and data:
- Assets/Editor/UsNationalVoteCheck.cs:442-449 (SharesOf) sums `t += h.VotesTotal` and returns `{ r / t, d / t }`. So the House denominator is the Clerk's Total.
- Assets/Scripts/Elections/Generated/UsPresidentialReturns.cs:331-333 says: "VotesOther every other column - the non-votes some states report and Maine's ranked-choice lines, which count some ballots again, among them; VotesTotal the Clerk's."
- ElectionsData/usa/president_returns.md:58 (reading 13) says: "a party's share of the Total is not its share of the votes cast for candidates".
- The four countries' histories are valid-vote shares. Assets/Scripts/Data/PartySystem.cs:609-611 derives Germany 2021 "from the per-Land absolute counts on disk (land_votes_2021.csv, valid 46,298,387)". Sweden's and Poland's rows are official final shares.

The printed loyalty moves; no miss does:
- Summing the catalog's House rows gives 2022 R 54,227,992, D 51,280,463, Total 108,443,387, and 2020 R 72,466,576, D 77,122,690, Total 153,431,405.
- LoyaltyModel.PartyLoyalty then gives R 94.45 and D 94.08. That matches us5w4.log:526 and the card's "94.5 / 94.1".
- Remove ME-2 2022's "Exhausted Ballot" line (322,778, from the s788 review's reading of clerk_statistics2022.pdf) from the 2022 Total, and the loyalty becomes R 94.17 and D 94.36. At the card's precision that is 94.2 / 94.4.
- No miss moves. Preference normalises the prior over the roster, and every fit returns T-1's split (us5w4.log:526: "every fit within 2.0e-7 of the prior").

Where it comes from:
- Reviews/2026-10-06_s788_us5_house_returns.md:760 (finding 16, the skeptic's fix item 2) asked: "In USA_STAGE_PLAN.md:216, and in the card's matching row in the working tree, name the House denominator: the Clerk's Total. Either drop 'the four countries' basis' or state the difference."
- The plan half of that fix was applied (line 218). The card half was not.
- The same unqualified wording is in UsNationalVoteCheck.cs:24 ("each party's share of all votes cast") and :440 ("as shares of all votes cast").

Why note and not minor:
- It does not change any miss, fit, check or the R-US15 choice. The card says itself that loyalty is inert ("whatever the loyalty").
- Only the prose basis of the printed loyalty column is overstated.
- In s788, a comparable wording issue about an inert premise (finding 17) was graded note.

**The skeptic's corrected fix.** Make a docs-only edit outside the GENERATED block. In docs/reference/US_ELECTIONS.md:267, replace "each party's share of all votes cast - the four countries' basis;" with the plan's wording:

"each party's share of all votes cast - the four countries' basis as near as the sources allow: the presidential history's share of the FEC's total, the House history's of the Clerk's Total, which carries the non-votes some states report and Maine's re-counted ballots; inert for every miss (the identity), it moves only the printed loyalty;"

Do not put a figure in the prose: the claim convention forbids transcribing the 0.3-point move.

Optionally, align the doc comments at Assets/Editor/UsNationalVoteCheck.cs:24 and :440 the same way, for example "each party's share of its contest's total - the FEC's for the president, the Clerk's Total for the House, its non-votes and Maine's re-counted ballots among them".

Neither edit makes the block stale. The block's digest covers only the catalog's source digests and the held electorates (UsNationalVoteCheck.cs:157-158, 320), so WriteReadings does not need to run again.

### 21. 'US-6 places the mean on the DEM-REP segment' states as fact something US-6 does not say

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/docs/specs/USA_STAGE_PLAN.md:626

**The scenario.** US-6's text (plan 232-236) says only that TryElectorate gains the USA 'in R-US15's unit'. It says nothing about where the mean sits. The ask's sentence reads as a description of the plan, but it is part of the recommendation.

**The fix proposed.** Write 'under (ii), US-6 would place the mean ...', and carry it into US-6's text when R-US15 is ruled.

**The skeptic's evidence.** I could not refute this. US-6's text never says where the mean sits, and line 626 is the only place in the plan that commits US-6 to the DEM-REP segment. Lines checked are from the staged blob (no unstaged edits; the sentence is at line 626 of `git show :docs/specs/USA_STAGE_PLAN.md`).

- **US-6, plan lines 232-241:** "`PartySystems.TryElectorate` and `PartySystems.TryRealHistory` gain the USA in R-US15's unit, each vintage's history pair sourced, and `PartySystems.HistoryNote` states the basis." The rest covers `PresidencyOfRecord`, E1's factors and the reference world. Nothing places the mean.
- **R-US15 as ruled into the plan, line 617:** "The electorate is fitted free where it is identifiable, and otherwise with its spread held at the fitted countries' values (DECLARED)." This holds the spread but says nothing on where the mean goes.
- **Line 216:** "The mean sits on the segment from DEM's point to REP's, at the place that reproduces T−1's split" is the US-5 instrument's design list (DECLARED before the build, §788). It covers the instrument, not US-6.
- **Search:** a grep for `mean`/`US-6` in the plan finds only lines 216 and 626 tying a mean to the segment.
- **Line 626:** "US-6 places the mean on the DEM-REP segment where the history's last election falls, as the instrument does." It is present tense, outside options (i)-(iii), and has no DECLARED tag.

**This is a real choice that US-6 has to make, not just wording:**
- `PartySystem.cs:662-678`: `TryElectorate` holds a full `Electorate(μecon, μsoc, σ, τ)` for each country, so US-6 must set a US mean.
- `UsNationalVoteCheck.cs:399-421`: `HeldFit`/`Along` put the mean on the segment by bisection, but only for held fits. The free fit's mean is `Calibrate`'s grid point and lies off the segment. Card row 311: free House 2022 μ 6.000, 6.500, against held fits around 6.0-6.35 / 5.4-5.8.
- Putting the mean on the segment is a tie-break among means that give almost the same split (the card's slide column). Since the midway unit sits on the line dividing the two parties, a mean on the segment near the midpoint affects what a third unit takes. The ask makes that same argument for the spread ("a spread decides only what a third placed unit takes").
- So the mean placement is a new premise for US-6. It is stated as settled, not offered as an option, and not marked DECLARED, although the head rule says every DECLARED premise must say so.

**Why it is only a note:**
- The sentence sits inside the "Code recommends" paragraph.
- In this plan, present tense describes planned behaviour.
- "as the instrument does" points to the DECLARED design at line 216.
- US-6 cites "(R-US2, R-US15)", so whoever builds it would find the sentence.
- Nothing is wrong in code or in the generated figures. The only risk is that the R-US15 ruling ("(iii) holding (ii), as recommended") leaves it unclear whether the mean premise was ruled too.

**On the reviewer's fix:** "under (ii)" is too narrow. The segment placement applies to any held spread ((i), (ii), or (iii) holding (ii)), not only the median.

**The skeptic's corrected fix.** At USA_STAGE_PLAN.md:626, mark the sentence as part of the recommendation and as a declared premise, covering any held spread rather than only (ii). For example: "Whichever spread is held, US-6 would place the mean on the DEM-REP segment where the history's last election falls, as the instrument does (DECLARED, part of this recommendation; like the spread, it decides what a third unit off the parties' points takes)." When R-US15 is ruled, the RULED line should name the mean placement with the spread (or US-6's text should name it next to "in R-US15's unit"), so US-6 does not depend on the wording of an unruled ask.

### 22. The card uses two names for the misses before and after the economic vote

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/docs/reference/US_ELECTIONS.md:269

**The scenario.** Line 269 says the economic vote is 'never folded into the history's miss'. Lines 275-276 say 'A history's national miss is therefore its no-change miss ... plus the economic vote'. R-US15's rule speaks of 'its national miss'. The ask sidesteps this by quoting both, but the card's two terms pull against each other.

**The fix proposed.** Use the table's terms, 'miss before' and 'miss after', in both places.

**The skeptic's evidence.** I could not refute it. The wording is as the finding says, but it is harmless: no figure, check or recommendation depends on it.

What the card says (docs/reference/US_ELECTIONS.md, hand-written, outside the GENERATED block):
- Line 269 (economic-vote row): "Printed in its own column, never folded into the history's miss".
- Lines 275-276: "A history's national miss is therefore its no-change miss - T-1's split against the predicted election's - plus the economic vote."
- Line 257 quotes R-US15: "unless its national miss is well above (b)'s". Line 278 says "the miss table does" decide it. Neither line says which miss column the rule reads.

What the code does (UsNationalVoteCheck.cs):
- Line 223: `after = EconomicVote.ApplyRecordShift(keys, poll, eve.Shift)`.
- Line 230: `before = 100*(poll[0]-record)` and `afterMiss = 100*(after[0]-record)`.
- The generated text (card line 285) says "'after' adds the economic vote of the eve world, printed in its own column". The table header (line 287) has "miss before | miss after".

So "miss after" does include the economic vote. Line 269 is true only if "the history's miss" means "miss before" (the no-change miss). Lines 275-276 use "a history's national miss" for "miss after". The two near-identical phrases name two different quantities, and a reader cannot tell which one R-US15's "its national miss" means.

The same pair appears in one paragraph of the instrument's doc comment (UsNationalVoteCheck.cs lines 32-34): "...a history's national miss is its no-change miss plus the economic vote... The economic vote is printed apart, never folded into the history's miss." GateReRun's US section also labels the miss without the economic vote "national" (elec789b.log: "national -> +§8 | free +0.08 -> +0.08"), but that is R-EL13's existing layer label.

Why it stays a note:
- The plan's declared design (USA_STAGE_PLAN.md line 221, written in s788) says it consistently: "Printed with and without, so the history's miss and the economic vote's are never read as one". The card's line 269 is a lossy paraphrase of that line: it dropped "with and without".
- The ask (plan line 620) answers on both columns: "with the economic vote and without it".
- Every comparison comes out the same on either column. 2024, own contest: (a) +0.08/-2.42 against (b) -3.02/-5.52. 2022: (a) -2.95/-2.62 against (b) -3.67/-3.33. 2020 has a before-miss only.
- The economic vote is the same for both histories (+2.50 in 2024, -0.33 in 2022).
- The card's digest does not read the prose, so nothing goes stale.

**The skeptic's corrected fix.** Bring the card back to the declared design's wording and tie the prose to the table's column names. Do not change the owner's R-US15 quote (line 257) or the plan's declared design.

(1) docs/reference/US_ELECTIONS.md line 269: replace "Printed in its own column, never folded into the history's miss" with "Printed in its own column, and every miss given without it and with it (the table's 'miss before' and 'miss after'), so the history's miss and the economic vote's are never read as one".

(2) Lines 275-276: replace the sentence with "A history's national miss - the table's 'miss after' - is therefore its no-change miss ('miss before': T-1's split against the predicted election's) plus the economic vote." Keep "national miss": it is R-US15's term and the design's.

(3) Optionally, line 278: "the miss table does, before the economic vote and after it".

(4) UsNationalVoteCheck.cs doc comment, lines 33-34: replace "The economic vote is printed apart, never folded into the history's miss." with "The economic vote is printed apart, and every miss is given before it and after it." This is a comment-only edit: the card's digest does not read the source, so no WriteReadings run is needed.

### 23. The ask's comparisons are typed into the plan, where US-4 generated its verdict

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/docs/specs/USA_STAGE_PLAN.md:620

**The scenario.** US-4's block generates its verdict ('The plan's rule recommends (a) ...', US_ELECTIONS.md block 1). US-5's verdict and its comparisons ('below (b)'s ... on the presidential one', '(a) is the closer in 2022 and (b) in 2020') are typed prose in the plan. The pins freeze the 2024 own-contest inputs. The out-of-sample comparisons and the cross-scores are not pinned, so a regenerated card could contradict the plan without anything failing.

**The fix proposed.** Generate the rule's verdict and the out-of-sample comparison inside the block, and have the plan cite it.

**The skeptic's evidence.** Every premise checks out against the staged tree (diff vs 40583b01).

1. US-4 generates its verdict and checks it. UsStateSwingCheck.cs:257-264 builds `verdict` ("**The plan's rule recommends {0}**", or "does not decide ... to be asked") and runs `Check(leaders.Count == 1 || leaders.Contains(0), ...)`. That output is card line 59, inside block 1. In the plan, US-4 types no result; its Built line says only "the card, whose readings carry ... the rule's recommendation".

2. US-5's verdict and comparisons are typed only in the plan. USA_STAGE_PLAN.md:620 reads: "(a)'s 2024 miss is below (b)'s on its own contest and on the presidential one, with the economic vote and without it. Out of sample, (a) is the closer in 2022 and (b) in 2020." The US-5 block (US_ELECTIONS.md:282-352) has no verdict line and no comparison line; Measure emits only the tables, the term lines, the midway-unit range and the E1 table.

3. Only two pin tuples exist: `(2024,"(a)","+0.08","-2.42","+2.50")` and `(2024,"(b)","-3.02","-5.52","+2.50")`. The pin loop scores only `c.Scored[0]`, the own contest. The 2022 and 2020 cases have no pin, and neither do the 2024 cross rows ((a) on the presidential vote +0.65/-1.85, (b) on the House -3.59/-6.09). The logs show the same: us5w4.log:529/539 and n789a.log:505/515 are the only `pinned:` lines.

4. Nothing reads the plan. `Run` compares only `CardBlockOf(card)` with the measured block. `WriteReadings` refuses only on a failed check. `DocumentClaimCheck` checks only backticked Type.Member names.

The failing path: re-read the catalog's House 2020 rows so that 2020's two-party REP share falls below the presidential 2020 share (the card shows 48.44 % against 47.73 %).
- `HouseSourceDigest` changes, so Run reports the card stale.
- House 2020 enters no pin. In 2024 (a) it is only T-2 for the loyalty, and the identity check shows the loyalty cannot move the two-party split. So WriteReadings passes and rewrites the block.
- The block now shows (b) closer in 2022, against plan:620. Nothing fails.

How likely that path is:
- 2022 needs a 0.71 pp move in a national House split. The Clerk/FEC differences are per-district conventions, far smaller than that.
- 2020 needs about 2 pp.
- The 2024 presidential cross-score needs House 2022 and House 2024 to move together by 2.4 pp or more.
- The 2022 economic vote moves (a) and (b) equally; it would have to reach about 3.3 pp REP-ward (it is DEM -0.33) to flip the order.
- The operative verdict rests on the pinned 2024 own-contest misses and cannot drift unseen.
So this is mechanically real but remote.

It is still a breach of the claim convention. CLAUDE.md:39-41 counts DERIVED claims as GENERATED, REFERENCED or DELETED only, with COMPLETED.md the only exemption. The s788 review's #18 graded derived facts transcribed into this plan as minor, and s789 itself fixed them (`UsEconomicWeight`). The s787 review's #4/#12 ("the recommendation can drift unseen after a re-pin") were graded note and fixed by generating the verdict behind a Check.

The same ask also types "none lies inside VoteModel.Calibrate's grid on all four parameters". It is true today: each TryElectorate electorate sits on a grid edge (Sweden τ 0.5; Germany σ 1, τ 16; Poland μs 7; Italy μs 7, σ 1). But it is the same class of claim and is not generated.

All the typed comparisons match the card today. Nothing is wrong now; it is just unguarded. The grade stays at note.

**The skeptic's corrected fix.** Preferred, following US-4's precedent:
- In UsNationalVoteCheck.Measure, emit into the US-5 block the rule's verdict from the pinned 2024 own-contest misses: (a) when its |miss| is not above (b)'s, before and after the economic vote.
- When (a)'s |miss| is above (b)'s, `Check(false, ...)` with "the rule's 'well above' is the owner's to judge - to be asked", mirroring UsStateSwingCheck.cs:257-264. The instrument must not settle a judgment the rule leaves to Elias.
- Emit one generated line per predicted year (2022, 2020) and per scored contest naming the history with the smaller |miss|, computed from the values the table prints. Regenerate with WriteReadings (cheap bar plus documents bar).
- Rewrite USA_STAGE_PLAN.md:620 to keep the recommendation ("Code recommends (a), by the rule above") and cite the card's verdict and out-of-sample lines in place of the two comparison sentences.

Minimal fix, plan only, no Unity run: delete those two sentences and point at the card's 2024 own-contest rows (pinned) and its 2022/2020 rows.

Either way, also reference or generate the "none lies inside Calibrate's grid on all four parameters" clause, which appears in the same ask and in the design bullet at :216.

### 24. elec789b.log was run before the median fit existed; elec789c.log is the run on the staged tree

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/GateReRun.cs:194

**The scenario.** elec789b (02:36) shows five fits per case. The staged GateSection prints six, adding the four's median, and so does elec789c (03:29:55, after the last source edit at 03:25:39). Both diff against elec788a as an insertion only, so the claim 'moves no other line' holds on the staged tree. A record citing 789b would cite a run of a different tree.

**The fix proposed.** Cite elec788a against elec789c in §789.

**The skeptic's evidence.** The finding is correct on every point. It is a note, not a defect: nothing in the staged change cites either log.

1. **The staged tree prints six fits per case.** In `UsNationalVoteCheck.cs`, `GateSection` (lines 345-350) prints one part per fit in `fitted[key]`. `FitAll` (377-396) builds that list from:
   - the free fit;
   - one held fit per country in `Held` (Sweden, Germany, Poland, Italy);
   - the four's median, added at line 395 when all four are held.

   `GateReRun.cs` line 194 calls `sb.Append(UsNationalVoteCheck.GateSection());`. Working tree and index match for every staged file (git status has no unstaged change in them).

2. **The logs.**
   - `elec789b.log` (02:36:37), lines 1381-1386: five parts per case (free, Sweden's, Germany's, Poland's, Italy's). "four's median" appears 0 times.
   - `elec789c.log` (03:29:55), lines 1337-1342: six parts per case, ending `| the four's median +0.08 -> +0.08` and so on. "four's median" appears 6 times.

3. **Timeline.**
   - `us5w3` (02:39:39) is the first log that carries the median, so it was added after `elec789b`.
   - 40583b01 was committed at 03:25:28. Every staged .cs file has mtime 03:25:39 (the stash pop); nothing is later.
   - `us5w4` recompiled after that ("AssetDatabase observed changes", CompileScripts 20164 ms).
   - `n789a` (855 ms) and `elec789c` (965 ms) reused that build, and `elec789c` prints the median. So `elec789c` is the run on the staged tree and `elec789b` is not.

4. **The diffs, through `Tools/elec_report.pl`.**
   - `elec788a` vs `elec789b`: `360a361,370`, an insertion only.
   - `elec788a` vs `elec789c`: `360a361,370`, an insertion only.
   - `elec789b` vs `elec789c`: only the six case lines differ (`364,369c364,369`).

   So the plan's Built line ("The `elec<N>` diff inserts the US section and moves no other line") holds on the staged tree.

5. **Why it is only a note.** `git diff --cached` contains no `elec78`/`789b`/`789c`, and `COMPLETED.md` has no §789 yet. The only place that calls 789b the "after" run is the review brief ("elec788a.log and elec789b.log (... before and after)"). §789's record would therefore misstate its evidence if it followed the brief. That breaks the repo's "on this commit's own tree" convention for cited runs (see §788's Bars line).

**Aside, outside this finding:** GateSection's header (line 333, "held at each fitted country's spread") does not name the four's median fit that the lines print.

**The skeptic's corrected fix.** In §789's record:
- Cite `elec788a` (before) against `elec789c` (after, on the staged tree). The filtered diff is `360a361,370`: the 10-line US section inserted after the R-EL13 verdict, nothing else moved.
- List `elec789b` only as "on the way": it has five fits per case and ran before the four's median was added.
- Say that `elec788a` ran on 43169a7a ("this HEAD before the change" in §788's record), so the diff covers §788 and §789 together. §788 added only catalog arrays that nothing read before §789, and the diff shows no other line moved.
- Optionally, make GateSection's header line 333 name the median fit, e.g. "and held at each fitted country's spread and at the four's median".

### 25. The card labels 2022 (b)'s scored contest "House 2022 (own)", but the House is not that history's own contest

- **Lens:** recompute - **reviewer:** minor - **skeptic:** note
- **Where:** Assets/Editor/UsNationalVoteCheck.cs:236

**The scenario.** Cases[3] (line 73) is new Case(2022, new History("(b)", Contest.President, 2020, 2016), true, Contest.House), so Scored[0] is House while History.Of is President. Line 236 adds " (own)" whenever k == 0, so the generated card (docs/reference/US_ELECTIONS.md:294) prints "| 2022 | (b) presidential 2020 against 2016 | ... | House 2022 (own) | 51.40 % | -3.67 | -3.33 |". There was no presidential election in 2022, so this history has no own contest that year. Someone ruling R-US15 from the card would read the 2022 comparison as both histories scored on their own contests. The card's own hand-written row says each case is scored 'on its own contest and on the other, where one was held'. The same wrong premise is in the Case summary (lines 55-56: 'scored on the contests it held - its own first') and in GateSection's summary (lines 324-325: 'on the case's own contest').

**The fix proposed.** Add " (own)" only when c.Scored[k] == c.History.Of. Reword the Case and GateSection summaries the same way, then run WriteReadings again. Only the label on line 294 changes; no figure moves.

**The skeptic's evidence.** The label does print as the finding says, but its effect is cosmetic, and the GateSection part of the finding is wrong.

Confirmed:
- Line 73 is `new Case(2022, new History("(b)", Contest.President, 2020, 2016), true, Contest.House)`.
- Line 236 adds `k == 0 ? " (own)" : ""` without comparing `c.Scored[k]` to `c.History.Of`.
- The generated card at docs/reference/US_ELECTIONS.md:294 reads `| 2022 | (b) presidential 2020 against 2016 | ... | House 2022 (own) | 51.40 % | -3.67 | -3.33 |`.
- The same table contradicts itself for history (b). At lines 291-292 the 2024 pair is `president 2024 (own)` and `House 2024`, so there the House is not (b)'s own contest. At line 294 it is. The Pins summary (line 81, "2024's miss by each history on its own contest") and the R-US15 ask (plan line 620, "(a)'s 2024 miss is below (b)'s on its own contest") both mean the history's contest.

Why it is only a note:
- The declared design names the case "2022's House, on its own eve world ... from House 2020 against 2018, and from (b)'s presidential 2020" and says "Each case is scored on its own contest and on the other" (USA_STAGE_PLAN.md lines 223-226). The card's hand-written row says the same (line 270). Read that way, "own" belongs to the case, and House 2022 is the 2022 case's own contest. So the label is ambiguous, not plainly false.
- The cell names "House 2022" outright, and 2022 held no presidential election, so the contest cannot be misread.
- No figure, pin or check moves. The pins cover 2024 only, where `Scored[0] == History.Of`, and the digest hashes the sources, not the card lines.
- The recommendation cannot flip. (a) is closer in 2022 (-2.95/-2.62 vs -3.67/-3.33), and in 2024 on both contests.

The finding is wrong about GateSection. It prints the contest by name ("2022 (b) presidential 2020 against 2016 on the House 2022"), and its summary says "the case's own contest", which matches the declared case "2022's House".

Aside, not this finding: elec789b.log (02:36) is older than the last edit to UsNationalVoteCheck.cs (03:25). Its US section shows five fits per case and no "the four's median", while us5w4.log and n789a.log show 16 median lines. So the elec<N> evidence comes from an earlier tree than the one staged.

**The skeptic's corrected fix.** Change line 236's suffix to `c.Scored[k] == c.History.Of ? " (own)" : ""`. Reword the Case summary (lines 55-56) to "its own first, where that election held it". GateSection's summary needs no change, because it prints the contest by name. Then run WriteReadings: only card line 294 changes (to "House 2022"), the stamp's digest stays the same, and no pin or figure moves.

### 26. The plan says the 2024 economic vote widens both histories' misses by the same amount; the card's figures show different widenings

- **Lens:** recompute - **reviewer:** minor - **skeptic:** minor
- **Where:** docs/specs/USA_STAGE_PLAN.md:627

**The scenario.** The economic vote moves both 2024 polls by the same signed amount (REP -2.50 pp). The size of each miss does not grow by the same amount, because (a)'s miss before the vote is positive and the shift crosses zero. Card values: (a) on its own contest +0.08 -> -2.42 (|miss| grows by 2.34); (a) on presidential 2024 +0.65 -> -1.85 (grows by 1.20); (b) on its own contest -3.02 -> -5.52 (grows by 2.50); (b) on House 2024 -3.59 -> -6.09 (grows by 2.50). My values: +0.0794 -> -2.4206, +0.6468 -> -1.8532, -3.0198 -> -5.5198, -3.5871 -> -6.0871.

**The fix proposed.** Say that it moves both histories' 2024 polls by the same signed amount, shown in its own column. Do not say it widens both misses equally.

**The skeptic's evidence.** The claim is at docs/specs/USA_STAGE_PLAN.md:627 and nowhere else in the change: "it widens both histories' 2024 misses by the same amount".

What the code makes the same:
- Both 2024 cases use the same eve world. `Case(2024, (a) House 2022/2020, true, ...)` and `Case(2024, (b) President 2020/2016, true, ...)` both resolve to `eves[c.Year]`, so they get one `eve.Shift`.
- The roster is REP and DEM only (`Check(keys.SequenceEqual(new[] { "REP", "DEM" }))`).
- `EconomicVote.ApplyRecordShift` (EconomicVote.cs:188-207) adds the shift to DEM and scales REP by `(others - moved) / others`. With two parties, REP's share falls by exactly the shift.
- The miss is signed: `afterMiss = 100.0 * (after[0] - record)`. The card's heading also says "Two-party, signed".

So the signed miss moves by exactly -2.50 pp for both histories. "Widens" is about the size of the miss, though, and the sizes do not grow by the same amount. The figures in us5w4.log (lines 526-538) match the card's block:
- (a) on its own contest, House 2024: +0.08 -> -2.42. The size grows by 2.34.
- (a) on president 2024: +0.65 -> -1.85. The size grows by 1.20.
- (b) on its own contest, president 2024: -3.02 -> -5.52. The size grows by 2.50.
- (b) on House 2024: -3.59 -> -6.09. The size grows by 2.50.

(a)'s misses start positive, so the -2.50 shift crosses zero. Its size grows by 2.50 minus twice the miss before the shift. (b)'s misses start negative, so they grow by the full 2.50.

A literal reading of the sentence therefore contradicts the card it cites. It also hides an asymmetry: the economic vote widens (a)'s lead over (b). The lead goes from 2.94 to 3.10 pp on the own contests and from 2.37 to 3.67 pp on the presidential 2024 contest. The sentence suggests the economic vote treats the two histories the same.

Why only minor: the first bullet of the ask is correct. It says (a) is below (b) on both contests, with the economic vote and without it. The pins, the checks and the recommendation are unaffected. The error is only in what the text tells the owner, Elias, about the measurement while he rules R-US15. CLAUDE.md's claim convention makes such a misstated derived claim worth fixing before the commit.

**The skeptic's corrected fix.** In docs/specs/USA_STAGE_PLAN.md:627, replace "and it widens both histories' 2024 misses by the same amount" with a statement of what is actually the same. Suggested wording: "and the one eve world's shift moves both histories' 2024 polls by the same signed amount (its own column) - which widens (b)'s misses by that amount and (a)'s by less, (a)'s poll lying on REP's side of the record before it". If a shorter line is wanted, the reviewer's version also works: "it moves both histories' 2024 polls by the same signed amount, shown in its own column". Either way, drop the claim that the two misses widen equally. No code change is needed. Optionally, in the same pass, make "distance ... plus the economic vote" in the plan's Built line say "signed" explicitly. That phrase has the same looseness, but it is still correct when read as a signed difference.

### 27. GateSection's last line says national and +§8 agree for every fit, but its own free-fit entries differ in two of six lines

- **Lens:** recompute - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/UsNationalVoteCheck.cs:354

**The scenario.** elec789c.log (the run on the final tree) prints '2022 (b) ... free -3.66 -> -3.67' and '2020 (b) ... free +1.15 -> +1.16', then 'Every fit holds its T-1 split, so national and +§8 agree'. The free fit matches its T-1 split only to within Calibrate's grid residual: 2.9e-5 for presidential 2020 and 1.8e-5 for presidential 2016. My recomputation: presidential-2020 free spatial share 0.4773308226 against +§8 0.4773027465 gives -3.6637 / -3.6665 against House 2022; presidential-2016 free share 0.4888490745 against 0.4888663951 gives +1.1547 / +1.1565. So the two columns differ at two decimals. The header (lines 332-334) also names only 'free' and 'each fitted country's spread', yet every line prints a sixth entry, 'the four's median'.

**The fix proposed.** Word the closing line as the card does: held fits hold the split exactly (solved), the free fit within its grid's residual, so the columns agree to that residual. Name the four's median in the header.

**The skeptic's evidence.** The tool's own output contradicts the fixed closing sentence, and the computation is deterministic, so this happens on every run.

1. Line 349 prints each fit as `Pp(100*(spatial[0]-record)) -> Pp(100*(p[0]-record))`, where p = PreferenceModel.Preference(GateReRun.ToCompatScale(spatial), prior, loyalty). PreferenceModel.cs:165-169 computes `result[i] = lambda*(prior_i/priorSum) + (1-lambda)*persuaded_i` and then renormalises. So p[0] equals the prior only when the spatial share equals the prior. For a held fit that is true to about 1e-16, because the bisection solves it (HeldFit, lines 404-409). For the free fit it is true only to within Calibrate's grid residual, and p[0] lands between the spatial share and the prior.

2. us5w4.log shows those residuals: line 548 `2022 (b), free: the poll 0.477303 ... prior 0.477302 ... residual 2.9e-5` and line 565 `2020 (b), free: the poll 0.488866 ... prior 0.488867 ... residual 1.8e-5`. The card's fits table shows free residuals of 0.0029 pp (presidential 2020) and 0.0018 pp (presidential 2016). It even prints the 2016 free split as 48.88 % against 48.89 %.

3. The residual straddles a rounding edge in two lines of elec789c.log, the run on the staged tree with six entries per line. Line 1340 reads `2022 (b) ... free -3.66 -> -3.67`, which comes from (0.477331 - 0.513968) against (0.477303 - 0.513968). Line 1342 reads `2020 (b) ... free +1.15 -> +1.16`. Line 1343 then says `Every fit holds its T-1 split, so national and +§8 agree`. The reviewer's figures (-3.6637 / -3.6665 and +1.1547 / +1.1565) match these log values.

4. Line 354 is a fixed string, and nothing in GateSection checks it. GateSection passes a no-op check to FitAll at line 340 and never tests the identity. The same claim in the generated card is qualified at line 282: "Every fit holds its T-1 split (free: within Calibrate's grid; held: solved)". GateSection dropped that qualifier.

5. The header sub-claim is also confirmed. Line 333 names only "free (VoteModel.Calibrate) and held at each fitted country's spread". Yet every line at 1337-1342 prints a sixth entry, "the four's median", which is not any fitted country's spread. elec789b.log, the earlier run, shows five entries per line, so the median was added after the header was written and the header was never updated.

Why only a note: the computed values are right, and the gap is about 0.003 pp. The US section comes after GateReRun's verdict and is outside the gate (GateReRun.cs:193-194). The identity is enforced where it matters, by the Check at UsNationalVoteCheck.cs:216 in Run and WriteReadings. No pin, verdict or R-US15 recommendation depends on the 0.01 rounding flip. Still, the printed evidence for R-US15 contradicts its own closing sentence.

**The skeptic's corrected fix.** Compute the gap instead of asserting it, as the claim convention asks. In the loop at lines 345-350, track `double gap = Math.Max(gap, Math.Abs(spatial[0] - p[0]));` across all cases and fits. Then replace line 354 with:

`sb.Append(F("  Every held fit holds its T-1 split (solved) and the free fit within Calibrate's grid, so national and +§8 agree to {0:0.0e+0} (the free fit's two columns can part in the last printed digit): the vote model returns the prior with two parties, whatever the loyalty.\n", gap));`

If a fixed string is preferred, use the card's wording at line 282: "held: solved; free: within Calibrate's grid, so the columns agree to that residual".

In the header at line 333, name the sixth fit: "free (VoteModel.Calibrate), held at each fitted country's spread, and held at the four's median;".

### 28. E1's 'the field sums to one' check cannot fail, and the card's 'field's sum' column always prints 1.000000

- **Lens:** recompute - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/UsNationalVoteCheck.cs:310

**The scenario.** Line 300 defines rest = 1 - trump - harris - (named minors). Line 310 adds fT*p[0] (= trump) + fH*p[1] (= harris) + the minors + rest, which is identically 1. The check at line 311 is therefore always true and would not notice a catalog in which Candidates and States disagree, for example a Candidates total that differs from the States' 2024 total. I checked that the data are consistent today: both totals are 155,238,302, TRUMP equals the States' 2024 R sum (77,302,580) and HARRIS equals the D sum (75,017,613). So nothing is wrong now; the check guards nothing.

**The fix proposed.** Replace the tautology with a check that can fail: the 2024 Candidates total equals the States' 2024 VotesTotal sum, and the R and D nominee rows equal the States' VotesR and VotesD sums. Alternatively, drop the sum column and the check.

**The skeptic's evidence.** The finding is right that the check is an algebraic identity. It is wrong that a Candidates-against-States disagreement would go unnoticed, so its fix is not needed.

The identity, in Assets/Editor/UsNationalVoteCheck.cs:
- Lines 289-290: `total` is the sum of the 2024 Candidates rows. `trump` and `harris` are the R and D rows divided by `total`.
- Line 297: each named minor's share is its row divided by the same `total`.
- Line 300: `rest = 1.0 - trump - harris - minors.Sum(x => x.Share)`.
- Line 309: `fT = trump / p[0]`, `fH = harris / p[1]`.
- Line 310: `sum = fT * p[0] + fH * p[1] + minors.Sum(...) + rest`. This reduces to trump + harris + minors + (1 - trump - harris - minors), which is 1. Rounding error is a few times 1e-16, against a tolerance of 1e-12.

Can the check fail at all? Only if p[0] or p[1] is zero, NaN or infinite, or if the 2024 field is empty. Each of those already fails an earlier check:
- An empty field fails the field checks at lines 291 and 296.
- A NaN in the poll before the economic vote fails the identity check at line 216 (`off <= residual + 1e-12` is false for NaN).
- A NaN in either share after the economic vote fails the 2024 pins at lines 239-245. In EconomicVote.cs:199-206, `Math.Max(0.0, NaN + by)` gives NaN, so `moved` is NaN, `scale` is NaN, and REP's share `after[0]` becomes NaN, which no longer matches its pin string.
- A share of exactly 0 (or 1) breaks the pinned miss.

So line 311 has no failing path of its own.

The column always prints 1.000000:
- The card, docs/reference/US_ELECTIONS.md:350-351, shows `| 1.000000 |` in both rows.
- The logs show "the field with E1's factors sums to one - 1.000000000000" at us5w4.log:579-582 and n789a.log:555-558.

E1's live mechanism (PresidentialElection.cs:240-260) is poll × factor, with the field normalised afterwards. There the sum before normalisation could differ from one, if a roster party outside the field of record stood. Here it cannot:
- The US roster is checked to be REP and DEM only (line 164).
- The factors are defined as record ÷ poll.
- The card itself says the factors "close 2024 by construction" (US_ELECTIONS.md:278, and the section heading at 344).

Data recomputed from Assets/Scripts/Elections/Generated/UsPresidentialReturns.cs:
- The 2024 Candidates rows: total 155,238,302, R 77,302,580, D 75,017,613.
- The 51 State rows for 2024: total 155,238,302, R 77,302,580, D 75,017,613.

These match the finding's figures.

Where the finding's reasoning fails: it says the check "would not notice a catalog in which Candidates and States disagree" and proposes adding that cross-check. The cross-check already exists:
- GeneratedCatalogCheck.CheckUsPresidentialReturns, lines 616-636, committed in s788 at HEAD 40583b01. For every candidate year it requires `field == total`, `ticketR == votesR`, `ticketD == votesD`, and one R and one D nominee. Its doc comment (lines 523-525) gives the reason: "what US-5's instrument leans on: a year's candidates sum to its jurisdictions' votes, each nominee's row to its ticket's".
- It runs in the cheap bar, at CheckSuite.cs:275, just before UsNationalVoteCheck at line 277.
- It passed in the same run: n789a.log:699 reads "26 candidate row(s) (their sum the jurisdictions')".

Grade: note. The check only restates its own arithmetic and guards nothing, but it reports nothing false. CLAUDE.md has no rule against a check that holds by construction.

**The skeptic's corrected fix.** Remove the Check at line 311 and the "the field's sum" column (lines 304, 310 and 313; drop "the field sums to" from line 314). The card's prose already says the factors close by construction (US_ELECTIONS.md:278 and the section heading), and WriteReadings regenerates the block. If the column stays as a display of the closure, drop the Check, or say in its own text that the sum is one by definition (fT·p_R = Trump's share, fH·p_D = Harris's share, rest = the complement). That is how GeneratedCatalogCheck.cs:591 marks its own by-construction guard. Do NOT add a Candidates-against-States check here. GeneratedCatalogCheck.CheckUsPresidentialReturns (lines 616-636) already checks that the 2024 candidates' total, R row and D row equal the States' VotesTotal, VotesR and VotesD sums. It runs in the same cheap bar, and its comment names this instrument as the reason it exists. A second copy would duplicate it.

### 29. The plan's Built line says 'four figures pinned'; the Pins array pins the four misses and the economic vote

- **Lens:** recompute - **reviewer:** note - **skeptic:** minor
- **Where:** docs/specs/USA_STAGE_PLAN.md:211

**The scenario.** UsNationalVoteCheck.cs:84-88 has two pins, each with Before, After and Shift strings (+0.08/-2.42/+2.50 and -3.02/-5.52/+2.50), and line 243 checks all three. That is five distinct figures in six pinned strings, as the instrument's own summary at lines 81-83 says ('... and the economic vote itself'). The count in the plan is also a hand-typed derived count in prose, which the claim convention in CLAUDE.md says to reference or delete.

**The fix proposed.** Delete the count, or replace it with a reference, e.g. 'the R-US15 figures pinned (UsNationalVoteCheck.Pins)'.

**The skeptic's evidence.** I could not refute this finding. The count is wrong by the instrument's own account, and it is the kind of hand-typed count the repo's claim convention forbids.

1. The staged plan, docs/specs/USA_STAGE_PLAN.md:211, says: "`UsNationalVoteCheck` (the cheap bar and the documents bar; four figures pinned; a stale block fails it)".

2. In Assets/Editor/UsNationalVoteCheck.cs:81-88, the doc comment says the pins hold "2024's miss by each history on its own contest, before and after the economic vote (points, two-party), and the economic vote itself (DEM, points)". The array is:
   Pins = { (2024, "(a)", "+0.08", "-2.42", "+2.50"), (2024, "(b)", "-3.02", "-5.52", "+2.50") }
   - At :239-245 every pin is checked on all three strings: `Check(b == pin.Before && a == pin.After && s == pin.Shift, ...)`.
   - That makes six pinned strings and five distinct figures: four misses plus the economic vote, pinned twice. "Four" leaves out the Shift figure, which the code checks.
   - The logged runs confirm it. us5w4.log:529/539 and n789a.log:505/515 print "(pinned +0.08, -2.42, +2.50)" and "(pinned -3.02, -5.52, +2.50)".

3. Under the convention in CLAUDE.md's head, a count is a DERIVED claim. It may only be GENERATED, REFERENCED or DELETED, and "Nobody transcribes, anywhere, in any file". Only COMPLETED.md is exempt, and this is the plan.
   - The convention's test fails here: adding one pin (for example 2022's cases) would make the plan wrong without anyone touching it.
   - The sibling Built line for §787 in the same file (line 197) already uses the allowed form with no count: "(the cheap bar; pinned; a stale block fails it)".

4. The bar does not catch this. DocumentClaimCheck only checks backticked `Type.Member` references (its Reference regex), so the passing n789a run says nothing about this prose count.

Severity: this is docs-only and has no runtime effect. I still grade it above a note because it is a false statement about the code in a document, which is exactly what the top-ranked rule exists to prevent, and the fix is a one-word deletion.

**The skeptic's corrected fix.** On docs/specs/USA_STAGE_PLAN.md:211, delete the count and follow the §787 line's form. Change "(the cheap bar and the documents bar; four figures pinned; a stale block fails it)" to "(the cheap bar and the documents bar; R-US15's figures pinned; a stale block fails it)", or simply "...; pinned; ...". The figures and how many there are stay in UsNationalVoteCheck's Pins and the card's generated block.

## The first pass - refuted by the skeptics

- [method] The 'before' column comes from the free fit, the one fit whose identity is bounded only by its residual - *The finding's mechanism is right. Its example cannot happen with these six cases.

What is right:
- Line 218 (`if (poll == null || fit.Name == "free") { poll = p; }`) takes the free fit's poll. FitAll (line 381) puts the free fit first.
- The round trip is exact. ToCompatScale (GateReRun.cs:339) computes 100·(s/max)^(1/3), and PersuadedShares cubes it back. So Preference (PreferenceModel.cs:160-169) gives |p0-a| = |d|·N/D with N = (1-a)(1-lR) + a(1-lD) and N - D = -(lR(1-s) + lD·s) <= 0. The check at line 216 therefore always passes for the free fit. Only the held fits make it a real test.

Why the scenario cannot happen:
- The 0.02 pp example needs loyalties near 50. Here the loyalties come from SharesOf (lines 442-449), which reads the generated catalog of historical returns. The six cases' loyalties are 89.42 to 98.37 (us5w4.log lines 526-571).
- The log matches the algebra exactly. Each free fit's poll offset divided by its residual: 2.0e-7/3.5e-6, 1.1e-6/2.9e-5, 7.1e-8/1.3e-6, 2.0e-6/2.1e-5 and 7.2e-7/1.8e-5. These equal N = 0.057, 0.038, 0.057, 0.094 and 0.040.
- The largest N is 0.0942, for 2020 (a): a = 0.455534, loyalties 91.55 / 89.42.
- With two parties and actual = {t, 1-t} (line 369), MAD = 100·|d|. The gate at line 382 (MAD < 0.05 pp) means |d| < 5e-4.
- So the free poll is at most 0.0942 × 5e-4 = 4.71e-5 (0.0047 pp) from every held poll. That is under half the card's 0.01 pp unit. The finding's fix (b), a check that worst < 5e-5, is already implied by the existing gate and could never fire.
- ApplyRecordShift (EconomicVote.cs:199-206) adds the shift to DEM, and REP absorbs exactly that shift. The offset passes into 'after' and the pins unchanged, not enlarged.
- Today, every 'before' and 'miss before' figure on the card equals what a held fit gives. In elec789b.log lines 1381-1386, the +§8 column agrees across all fits on all six lines.

What is left: a one-digit rounding flip between the 'T-1 split' and 'poll before' columns. It would need both of these:
- a change to the US positions or to UsEconomicWeight (both are in the digest, so the card would be regenerated);
- a new free residual of at least about 0.017 to 0.043 pp, in one particular direction. Today's residuals are at most 0.0029 pp.

Fix (b) would not catch that flip either.

Related problem that is real today, but a different path (worth filing on its own):
- GateSection line 349 prints the free fit's spatial ('national') miss with its full residual (not damped by N). Line 354 then says 'national and +§8 agree'.
- elec789b.log line 1384 shows 'free -3.66 -> -3.67' and line 1386 shows 'free +1.15 -> +1.16'. Every held fit prints equal pairs on those lines.
- Also, the E1 factors at line 313 are printed to 6 decimals from the free-fit poll. Row (b)'s factors move about 3e-6 depending on which fit is used. No text on the card says they are the same for every fit.*
- [method] The entrant layer is left out of the stated 'live chain' without saying why - *The observation is accurate. NationalElection.cs:201-203 runs Preference, then EntrantLayer.ApplyFor, then ApplyRecordShift. The instrument (UsNationalVoteCheck.cs:213-214, 223) and its doc (lines 24-26), plus the card's "The prediction" row, have no entrant step. But neither harm the finding names can happen.

1. The skipped step changes nothing on any input the instrument can build:
   - Line 164 pins the roster to exactly {REP, DEM}, and the bar fails otherwise.
   - RealRoster (PartySystem.cs:688-700) never includes a created party.
   - SharesOf (lines 442-448) returns the real national R/t and D/t of a contest-year, and throws if the total is zero. So no prior can be 0.
   - EntrantLayer.Apply (EntrantLayer.cs:79-81) and EntrantSimilarity.Apply (77-79) then hand back the input array itself.
   - So the composed chain is the live chain, down to the same array.

2. The "a future history with a zero prior would diverge silently" case is wrong even if someone reached it. A zero prior needs a rewrite of the line-164 check and SharesOf, which means editing the chain anyway. And for a real entrant under the default rule (EntrantLayer.Active = FamilyForCreated):
   - With no created party, GroupingOf returns null (EntrantLayer.cs:46-48) and so does EntrantAwareness (NationalElection.cs:185-187). So every nest has λ = 1 (line 106) and awareness a = 1 (line 123).
   - Then y_i = s_i = pref_i/(1-pref_e) (line 115), and the entrant solves y_e/(1+y_e) = pref_e. Each established party ends at s_i·(1-pref_e) = pref_i. That is the input again, up to rounding.
   - The class doc says the same: the proportional draw is "the preference model's own property" (EntrantLayer.cs:19-20).
   - Only a created party (a < 1, or grouping 0.5) or the SimilarityForAll measuring rule moves the shares. Neither exists in a backtest of real elections.

3. Precedent: GateReRun's own +§8 chain (GateReRun.cs:98) also composes Preference with no entrant layer and no remark. The live call site explains itself at NationalElection.cs:202: "none, the very array". EntrantLayerDiagnostic.cs:66-76 already proves "no entrant: the very array" for the live predictions.

4. The identity is measured on the composed chain: us5w4.log lines 520-570 show "ok ... - the identity" on all 36 fits.

The doc lists the four steps the instrument actually composes, in the live order. The repo's claim convention allows a document to be incomplete but not wrong. No wrong figure or conclusion follows.*
- [worlds] elec789b was not run on the staged tree; elec789c was - *The finding's facts are right, but the failure it describes is not happening.

What is true:
- elec789b (02:34:58 to 02:36:37) prints five fits per USA line and no "the four's median". elec789c (03:28:38 to 03:29:55) prints six. The median first appears in us5w3 (02:39).
- Staged UsNationalVoteCheck.cs line 395 adds the median fit (`if (sigmas.Count == Held.Length) { result.Add(HeldFit(..., "the four's median", ...)); }`). GateSection (lines 345-350) prints one entry per fit.
- Every changed code file was last written at 03:25:39 and the card at 03:26:48. All are earlier than elec789c. `git diff --quiet -- Assets/Editor Tools docs` exits 0, so the working tree matches the index.
- Filtered with elec_report.pl:
  - elec788a vs elec789b: 10 USA lines inserted after line 360, nothing else.
  - elec788a vs elec789c: the same 10 lines, with the median column.
  - elec789b vs elec789c: only lines 364-369 differ, and the only difference is the median column.

Why the failure does not occur:
1. No staged file names an elec run. Grepping the repo for elec789, us5w or n789 finds nothing. The plan's Built line says only "The `elec<N>` diff inserts the US section and moves no other line", which is true of both runs.
2. The s789 record already cites the right run. COMPLETED.md has no §789 yet. The draft in the session scratchpad (rec789.md, last written 03:31:30, after elec789c finished) says: "**The elections-backtests diff** (`Tools/elec_report.pl`; `elec788a` on §787's tree against `elec789c` on this one): **ten lines inserted ... no other line moved.** The launch 60.8 s, GateReRun 8.2 s of it."
3. Those timings belong to elec789c. Its TIMING line reads "total 60.8 s ... GateReRun 8.2 s". elec789b's reads "total 69.9 s ... VoteShareBacktest 60.9 s, GateReRun 8.2 s". So the draft's figures come from the run on the staged tree.
4. The only place elec789b is called the "after" run is review-harness text, not the change or its record:
   - the workflow's computed brief;
   - review789_lenses.json (written 02:51:32, before elec789c existed), which says "compare ... elec788a.log and elec789b.log".

The fix the finding asks for (cite elec789c in the s789 record) is already in the draft record.*
- [worlds] The bar evidence covers only a named subset with the check first; the cost is mostly the fits - *I refuted the main claim: a full cheap-bar run on this exact tree exists, and it shows the check in its registered position.

1. Launch and timing. G:/UNITY/Projects/PoliSim-captures/logs/unity_launched.tsv, last row: "18948  2026-10-06T03:31:53  cheap789  PoliSim.EditorTools.CheckSuite.RunAllBatch". Every staged file was last written by 03:26:48 (US_ELECTIONS.md, written by us5w4's WriteReadings). `git diff` (worktree against index) is empty for all eight staged paths. So cheap789 ran this tree, after us5w4 (03:26) and n789a (03:27).

2. Order. cheap789.log:320 reads "CHECKS: running all 100 in one pass - ... PresidentialElectionLiveDiagnostic, CabinetPressureDiagnostic, CongressOfRecordDiagnostic, ... DocumentClaimCheck, GeneratedCatalogCheck, UsStateSwingCheck, UsNationalVoteCheck, D18InventoryCheck, MojibakeCheck, ...". That is the registered cheap order.

3. Result. cheap789.log:18504-18506:
   - "measured in 9566 ms (the fits 7287 ms)"
   - "ok  the model card's US-5 readings ... - 69 lines"
   - "=== UsNationalVoteCheck: the reading holds ===" (130 ok lines, no FAIL)
   Then :22372 "CHECKS: 100 of 100 clean." and :22412 "suite exiting 0". Logs/bar_timing.tsv:59920 has "2026-10-06T01:35:00Z cheap UsNationalVoteCheck 9577 0".

4. Order independence. The check gave the same 69-line card twice:
   - run first and cold (n789a:321);
   - run after about 75 checks, including the world builders (cheap789).

5. What the commit owes. Tools/bar_tier.ps1 -Staged (read-only) prints "BAR TIER: TOOLING ... per item : CheckSuite.RunAllBatch (the cheap bar) ... review : skipped (tooling)". DOCUMENTS is added only when nothing else is touched (`if (-not $touched.Count -and $docs.Count)`), so RunDocumentBatch is not owed. Every Documents member is also in Suite: CheapGroup is built from Suite (CheckSuite.cs:573-576) and enforced at :759. So every document check already ran on this tree inside cheap789.

6. The cost figures are true but not a defect.
   - The fits are 7287 of 9566 ms (cheap789), 7433 of 10646 (us5w4) and 7612 of 10933 (n789a).
   - There are five distinct (Contest, T1) keys in FreeFits (UsNationalVoteCheck.cs:364).
   - The instrument already says so: UsNationalVoteCheck.cs:360-361 reads "the grid searches run side by side ... since together they are most of this instrument's time".
   - CheckSuite.cs:741 ("two eve worlds, so a card-only commit pays for them") is accurate: two distinct OnWorld years, 2024 and 2022, read by ReadEve at :170-172. It is only incomplete.
   - For scale, docs787.log shows the documents bar took 9.5 s before this check. The comment's "pays for them" already admits the cost.*
- [claims] The generated '(own)' label marks the presidential history's 2022 row as scored on its own contest - *The row is printed exactly as the finding says. UsNationalVoteCheck.cs:73 declares `new Case(2022, new History("(b)", Contest.President, 2020, 2016), true, Contest.House)`, and line 236 appends `k == 0 ? " (own)" : ""`. US_ELECTIONS.md:294 prints "| 2022 | (b) presidential 2020 against 2016 | ... | House 2022 (own) | 51.40 % | -3.67 | -3.33 |", and us5w4.log lines 554-555 match.

The finding assumes "own" means the history's contest. The declared design means the case's contest. USA_STAGE_PLAN.md:223 says "2022's House, on its own eve world (7 Nov 2022, stepped to 8 Nov): from House 2020 against 2018, and from (b)'s presidential 2020;". Line 226 says "Each case is scored on its own contest and on the other". So the 2022 (b) case is "2022's House", and its own contest is the House. The card's hand-written row (US_ELECTIONS.md:270) says the same: "Out of sample: 2022's House on its eve world, by (a) and by (b)'s presidential 2020 ... Each scored on its own contest and on the other, where one was held". Read this way, the prose is true for all six cases. In 2022 the case's own contest (the House) is scored, and the other contest was not held.

The code uses the same meaning. GateSection's doc (UsNationalVoteCheck.cs:324-325) says "on the case's own contest" and scores c.Scored[0] (line 343). Line 351 prints "2022 (b) presidential 2020 against 2016 on the House 2022" (elec789b.log:1384). The Case doc (lines 55-56) says "A case: ... scored on the contests it held - its own first". Its subject is the case, and the House is first.

R-US15 (b) itself says "midterms then reading the last two presidential votes". In 2022 the presidential history's task is the House, which is the contest the (b) option is judged on that day.

The two meanings differ only on this row. In the other five cases Scored[0] == History.Of. So the pins (lines 84-88, 2024 only) and their "on its own contest" message (line 243) are correct whichever meaning is used.

No figure, pin, comparison or check reads the label. The only hits for "(own)" are line 236 and the card. The ask's text ("(a) is the closer in 2022 and (b) in 2020", USA_STAGE_PLAN.md:620) does not rely on it.

The proposed fix (mark "(own)" only when Scored[k] == History.Of) would show the 2022 House case as not scored on its own contest. That contradicts the design's naming of the case, so it trades one reading for the other without being more correct.

The remaining point is a clarity note, not a defect: the generated block never defines "(own)", and the 2024 rows invite reading it as the history's contest.*
- [claims] COMPLETED.md §789 does not exist yet, but this change cites it throughout - *The facts are right, but they describe the normal state of every change under review. Nothing in this change is wrong.

What the finding gets right:
- COMPLETED.md ends at "## 788." (line 37518) at HEAD, in the index and in the working tree. There is no "## 789." anywhere.
- §789 is cited where the finding says: US_ELECTIONS.md:8 and :259; USA_STAGE_PLAN.md:211, :216 and :619; UsNationalVoteCheck.cs:19, :106 and :130; CheckSuite.cs:277 and :741; GateReRun.cs:193; Runner.cs:11 (all in the index blobs).

Why it is not a defect:
1. The record is written after the review on purpose, because it records the review. §788's own section has a "**The review** (`Reviews/2026-10-06_s788_...`: the workflow `polisim-staged-review` ...)" paragraph and a "**Bars**" paragraph. A section like that cannot exist before its review. The s788 review says it reviewed "the staged diff at HEAD 43169a7a", which had no §788.
2. Every pass commit lands with its section. Commits 40583b01, 43169a7a, 947921ab, 5fe3c031, 1b8ba4ed and 7d71fcc4 each add COMPLETED.md, plus the Reviews/ report where there was a review. CLAUDE.md:67 says "RECORDS: ONE COMMIT PER PASS".
3. §789 is already drafted. The session scratchpad has rec789.md, headed "## 789. PS-6 US-5, PART TWO: THE NATIONAL-VOTE PROOF ...". It already holds the miss table, the degeneracy, E1's factors and the fitted spreads. It already names the after-run the fix asks for: "`elec788a` on §787's tree against `elec789c` on this one" (elec789c.log exists, 03:29). It ends with the placeholders "REVIEW789" and "BARS789", which wait for this review and the bars. §787 and §788 were drafted the same way (rec787.md and rec788.md).
4. Earlier skeptics refuted this same finding for §772, §780, §783, §786, §787 and §788. Reviews/2026-10-06_s788_us5_house_returns.md:1135 says: "Facts confirmed but no defect: this is how every change looks at review time". The s787 review says the same at :835-840, and the s786 review at :1086 and :1198.
5. Nothing can fail because of it. No check compares § pointers with COMPLETED.md headings: DocumentClaimCheck.cs:84 and ResidueCheck.cs:27 only exempt the file. The pointers sit in comments, log headers and document text.

What remains is older than this change: only the commit step guards the pointers. A commit made without the record would break the one-commit-per-pass rule, but nothing in the staged change causes that.*
- [recompute] 'None of the four fits lies inside Calibrate's grid' holds only if 'inside' means strictly interior; all four are grid points - *The reviewer's facts are right, but they show the sentence is true, not false.

Calibrate's grid (Assets/Scripts/Elections/VoteModel.cs):
- line 166: `for (double mu = 3.0; mu <= 7.0001; mu += 0.25)`
- line 168: `for (double ms = 3.0; ms <= 7.0001; ms += 0.25)`. A step of 0.25 is exact in binary, so the ends are exactly 3.0 and 7.0.
- line 170: sigma in `{ 1.0, 1.5, 2.0, 2.5, 3.0, 3.5 }`
- line 172: tau in `{ 0.5, 1.0, 2.0, 4.0, 8.0, 16.0 }`

The four fits (Assets/Scripts/Data/PartySystem.cs):
- line 667, Sweden (3.25, 6.25, 3.00, 0.50): tau at the low end.
- line 669, Germany (4.50, 6.50, 1.00, 16.00): sigma at the low end, tau at the high end.
- line 671, Poland (3.50, 7.00, 1.50, 8.00): mu_soc at the high end.
- line 673, Italy (4.25, 7.00, 1.00, 4.00): mu_soc at the high end, sigma at the low end.

The plan's two sentences:
- docs/specs/USA_STAGE_PLAN.md:216: "none of the four fits lies inside `VoteModel.Calibrate`'s grid on all four of its parameters"
- docs/specs/USA_STAGE_PLAN.md:626: "none lies inside `VoteModel.Calibrate`'s grid on all four parameters"

"Inside ... on all four parameters" tests each parameter for being away from the grid's ends, which is the usual meaning of an optimum lying inside its search grid rather than at its edge. Each fit has at least one parameter at an end, which is exactly what both sentences say. The reviewer's own recomputation confirms this.

The "off the grid" reading cannot be meant. The four fits are Calibrate's own outputs on this same grid. The grid has not changed since commit cb17c851. That commit's calibrated MADs (3.25 Sweden, 5.78 Germany, 6.99 Poland, 5.61 Italy) are the figures GateReRun.BuildCases carries beside these same electorates (GateReRun.cs:266/290/309/327). So the fits are grid points by construction, and "none is a grid point" would be absurd on its face.

The sentence also carries the argument the plan needs: every country's fit is a boundary solution. The card's generated table (US_ELECTIONS.md, the fits table) prints the same sigma and tau values, read at run time. No misreading changes the recommendation, because its main ground is "each is its own election's fit."

At most this is optional rewording, not a defect.*

## What was done about the first pass

No defect survived the skeptics (12 minor, 17 notes); every finding was acted on, and the card rewritten from the revised instrument.

- **What the spread decides (11).** The ask, the card and the class doc said the spread decides only a third unit; it also decides how far the two-party split moves when a party's point moves - US-33's own nominee is a moved point, not a third unit. The instrument now prints both, spatially: a "moved point" column (the split's answer when DEM's point moves one unit toward REP's) beside the midway unit - its figures are the skeptic's independent port's to the hundredth. Option (iii) now leaves the choice to the first items that leave the fitted points, each measuring its own response.
- **R-US15's verdict generated (23, 14).** The instrument runs the rule over the 2024 rows - (a) not above (b) on its own contest, before and after the economic vote - prints the verdict in the card and fails, to be asked, where the rule would leave "well above" to Elias; the out-of-sample comparisons (the closer history per earlier election and contest) are generated beside it. The plan's ask cites them instead of typing them, and answers option (c).
- **The "(own)" tag (1, 25).** By contest, not position: 2022's presidential history is scored on the House alone, untagged, and the card says why.
- **GateReRun (3, 7; 2, 8, 16, 27).** The gate's report is logged before the US section, which is its own guarded message (a section that cannot be built is a named error); its closing lines are computed - each held fit's national and +§8 agree within the measured gap, the free fit's differ by its measured grid residual - and a failed fit check is named there.
- **The eve's facts (9).** The tautology replaced: the term begins before the eve, on the government of record's first day; the House seated on the eve is the one the previous House election elected, its seats the record's.
- **The held list (19).** Checked against every country `PartySystems.TryElectorate` answers for.
- **E1's sum (4, 28).** The tautological check replaced by a real one: the field's votes the jurisdictions' total, each nominee the jurisdictions' own figure; the sum column kept, as the design asks, and said to be one by construction.
- **The wording (5, 12, 6, 15, 29, 13, 26, 17, 18, 20, 21, 22).** The factors' column named; "four figures pinned" replaced by what is pinned; "widens by the same amount" replaced by "moves both polls toward DEM by the same amount"; the median fit dated with the measurement in the class doc and named in the card's table of parts; "none lies inside the grid" replaced by "each sits on a bound in at least one parameter", referenced; the card's loyalty row names its denominator; "US-6 places the mean" replaced by what the instrument does; the misses named before and after the economic vote.
- **The runs (24, 10).** The record cites the elections launch on the final tree (`elec789d`); the eve's unemployment series being the revised one (§709, pre-existing) is noted in the record.
