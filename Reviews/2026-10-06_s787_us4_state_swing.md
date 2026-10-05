# Review - §787, PS-6 US-4: the state-swing proof, R-US14 asked (2026-10-06)

A workflow review (`polisim-staged-review`) of the change that builds US-4: `UsStateSwingCheck` (the states derived from the national vote by R-US14's three methods on each consecutive pair of elections the generated catalog holds, the electors counted, nine figures pinned), the US model card `docs/reference/US_ELECTIONS.md` with its readings written by the instrument, and the plan's Built line. Not required by the tier (`Tools/bar_tier.ps1`: TOOLING, no money, no sentinel); run because the readings are the evidence R-US14 is ruled on. One pass - no defect survived it; every finding was put to a refute-first skeptic, who re-graded it; every report below is verbatim.

## The first pass - confirmed (verbatim)

The workflow `polisim-staged-review` (run wf_a20898e7-0a1, 27 agents) over the staged diff at HEAD 947921ab: three lenses (the methods against the ruling, the card and its claims, an independent recomputation), every finding put to a refute-first skeptic. Every report below is verbatim.

### 1. (b)'s per-state renormalisation takes its states off the TRUE national shares, so its reported margin misses are not measured at the ruling's national vote

- **Lens:** method - **reviewer:** minor - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs:243

**The scenario.** Proportional() divides each state by its own sum (line 243). Weighted the way the check weights them (previous VotesTotal), (b)'s states add up to D 0.4842 / R 0.4970 in 2020->2024 against the true 0.4832 / 0.4980. That puts the national margin at R+1.28 instead of R+1.48. The same drift is D 0.5127/R 0.4687 against 0.5131/0.4685 in 2016->2020, and D 0.4820/R 0.4611 against 0.4818/0.4609 in 2012->2016. The card's row at docs/reference/US_ELECTIONS.md:20 says the measurement spreads the TRUE national shares. The calls do not change: renormalising is one factor per state, and it cancels out of (b)'s district two-party transform. So the nine pins and the recommendation stand. The margins do change. The card's 2020->2024 mean margin miss reads (b) 2.28 against (a)/(c) 2.46, which says (b) spreads 2024's vote best. Without renormalising, (b) reproduces the national shares exactly on the same weights and gives 2.46, the same as (a)/(c). It also gives 5.58 against the card's 5.64 for 2012->2016, and an unchanged 2.57 for 2016->2020. The 0.18 gain comes from the renormalisation drifting toward D, while (a)'s 2020->2024 misses are positive (R over-predicted) in 39 of 51 jurisdictions. Measured by an independent recomputation from the generated catalog (scratchpad us4_review.pl).

**The fix proposed.** Either keep (b) unnormalised, as (c) is (proportional swing then closes on the national shares on the previous weights), or re-solve it to the national shares as (a) does. Alternatively, DECLARE in the card's (b) row that its renormalised states sum to a national vote off the true one, and have Measure print each method's implied national shares into the GENERATED block, so the mean-miss column is read alongside them.

**The skeptic's evidence.** WHAT HOLDS. `Proportional()` in G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs (lines 238-245) scales each share by its national ratio and then divides by the state's own sum (`s[i] /= sum`). There is no closure step: the only closure check is (a)'s `residual < 1e-9` at line 125. (a) is weighted by the previous election's totals (line 122, `RegionInput(s.State, s.VotesTotal, null)`).

I recomputed everything from the generated catalog with my own scripts, not the reviewer's (scratchpad: skeptic_b_renorm.pl, skeptic_b_resolve.pl, skeptic_b_shift.pl). Every number in the finding reproduces:
- **Implied national shares of (b) as built, on the previous election's weights:**
  - 2020→2024: D 0.4842 / R 0.4970, margin R+1.28. True: 0.4832 / 0.4980.
  - 2016→2020: 0.5127 / 0.4687 against 0.5131 / 0.4685.
  - 2012→2016: 0.4820 / 0.4611 against 0.4818 / 0.4609.
- **Mean |miss| of (b):** 5.64 / 2.57 / 2.28 as built (matches the card), 5.58 / 2.57 / 2.46 without the renormalisation.
- **(a)'s 2020→2024 misses:** positive in 39 of 51.
- **One slip:** the true 2024 margin is R+1.4719, not the finding's R+1.48.

The calls do not move. Line 145 compares `predicted[1] > predicted[0]`, which a common divisor cannot change, and the divisor cancels in the district transform at lines 160-162. Measured: states and districts are called alike, with the largest district-share difference at 1.1e-16. The nine pins, the electors and the recommendation ((a) on a 99/99/99 tie) all stand.

WHAT IS REFUTED OR OVERSTATED.
1. **Only about half of the 0.18 gap is drift.** I kept (b) as the card defines it (each state renormalised) and closed it on the true national two ways:
   - Re-solving the per-party ratios until the previous-weighted states sum to the true national, the way (a) re-solves its swings: 2020→2024 mean miss 2.3684 (2012→2016: 5.65; 2016→2020: 2.55).
   - Shifting every state by the national drift of +0.194 pp: 2.3680.
   
   Both give about 2.37, still below (a)/(c)'s 2.46. So the drift accounts for about 0.09 of the 0.18, and (b) keeps the lowest 2020→2024 mean miss either way. The card's direction holds; only the size of the lead is inflated.
2. **The unnormalised control is not clean.** Its 2.46 changes the spread as well as the national, and its states' shares no longer sum to one.
3. **"Off the true national" only holds on the previous election's weights.** Weighted by what each state actually cast in 2024, (a) implies R+1.83 and (b) implies R+1.64, against a true R+1.47. (a) closes on the previous weights only because it is solved on them.
4. **The card does not claim closure for (b).** Line 22 claims it for (a) alone. Line 23 says (b) is "each state renormalised". Line 20 states the true national shares as the input. The project's own record already says proportional swing does not reproduce the national (RegionalVoteModel.cs lines 221-226; COMPLETED.md line 11049). The ruling defines (ii) only as "proportional swing" and does not require it to close.

What is left is a note: in an informational column that the R-US14 rule does not use, (b)'s 2020→2024 lead is inflated, and the card does not say why.

**The skeptic's corrected fix.** Keep (b) as the card declares it and do not switch to unnormalised (b). That change alters (b)'s spread as well as its national vote and leaves state shares that do not sum to one, so its 2.46 does not isolate the drift.

The cheap fix:
1. Add one DECLARED clause to the card's (b) row (docs/reference/US_ELECTIONS.md line 23): its renormalised states need not sum to the national shares on the previous election's weights, so its misses carry a national part as well as the spread. Proportional swing has no closure guarantee; the record already says so in RegionalVoteModel.cs lines 221-226. Write no figure in that prose.
2. Optionally, have Measure write each method's implied national margin on the previous weights into the GENERATED block, so the mean-miss column is read next to it.

If Elias wants (b) closed, re-solve (b)'s per-party ratios until those weighted states sum to the true national, as (a) re-solves its swings. That puts (b)'s 2020→2024 mean miss at about 2.37, still below (a)/(c)'s 2.46, and moves no pin, call, elector count or recommendation.

### 2. The 2020->2024 district swing crosses the post-2020-census redraw of the ME/NE districts; the premise is not declared

- **Lens:** method - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs:160

**The scenario.** dPrev and dNext are paired by district number (lines 150-151, 160): a district's prior two-party share is the previous year's row with the same number. Both Nebraska and Maine redrew their congressional districts after the 2020 census (in 2021). This is general knowledge, not on a saved page in the repo. So the 2020 rows describe different districts from 2024's. The catalog's own figures show it. Each district's share of its state's two-party vote drifts gradually on the fixed map from 2012 to 2020: NE-2 0.338/0.344/0.355, NE-3 0.327/0.324/0.309, ME-1 0.528/0.530/0.541. In 2024 they snap back toward equal shares: NE 0.336/0.334/0.330, ME 0.521/0.479. From 2020 to 2024 NE-2's two-party vote fell 5.6% and NE-3's rose 7.1% while Nebraska moved +0.4%; ME-1 fell 1.5% and ME-2 rose 6.6% while Maine moved +2.2%. The card's 2020->2024 district misses (for example NE-1 +8.23, NE-2 +4.10) therefore include the map change as well as the swing. Neither the card's DECLARED districts row (US_ELECTIONS.md:25), the code's DECLARED comment (lines 154-155), nor the ruling says the map moved. No call changes in the present data.

**The fix proposed.** DECLARE in the card's districts row and the comment at line 154 that the previous election's district rows stand in for the next election's districts across the 2021 redraw (2020->2024 only; 2012->2016 and 2016->2020 are on one map). If the redraw is stated as a fact, source it; USA_STAGE_PLAN.md premise 9 already holds the House analogue.

**The skeptic's evidence.** I could not refute this. The code pairs districts across a change of lines that the repo's own saved pages record, and nothing declares it.

THE CODE (G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs)
- Lines 150-151 select the previous and next year's rows by state, ordered by district number.
- Line 153 checks only that the district counts match.
- Line 160 takes the prior share d2 from dPrev[k]. Line 167 sets d2Next = d2 + (stateNext - statePrior). Line 169 compares that with dNext[k].
- So 2020's district k is the base for 2024's district k, whatever its lines.
- None of these mention lines, maps or a redraw: the DECLARED text in the class doc (lines 26-28), the comment at 154-155, the card's districts row (docs/reference/US_ELECTIONS.md:25) or the plan's US-4 Built line (USA_STAGE_PLAN.md:197). A git grep for redistrict/redraw over the tracked .cs and .md files (COMPLETED.md excluded) finds nothing relevant either.

THE FINDING'S FIGURES HOLD
From president_by_district.csv, each district's share of its state's two-party vote, 2012 / 2016 / 2020, then 2024:
- NE-2: 0.338 / 0.344 / 0.355, then 0.334
- NE-3: 0.327 / 0.324 / 0.309, then 0.330
- ME-1: 0.528 / 0.530 / 0.541, then 0.521

Two-party totals, 2020 to 2024: NE-2 -5.56%, NE-3 +7.12%, Nebraska +0.36%; ME-1 -1.47%, ME-2 +6.61%, Maine +2.24%.

THE REDRAW IS ON SAVED PAGES (the finding says it is not)
Nebraska canvass books, ElectionsData/usa/raw/district/nebraska_sos_general_canvass_*.pdf (pdftotext -raw), "Results by Congressional District":
- 2012, 2016 and 2020 list the same counties. CD1: Burt, Butler, Cass, Colfax, Cuming, Dixon (2 precincts), Dodge, Lancaster, Madison, Otoe, Platte, Polk, Sarpy (23 precincts), Saunders, Seward, Stanton, Thurston, Washington. CD2: Douglas and Sarpy (31 precincts).
- 2024 CD1: Butler, Cass, Colfax, Cuming, Dodge, Lancaster, Madison, Platte, Polk (3 precincts), Sarpy (57 precincts), Seward, Stanton.
- 2024 CD2: Douglas, Sarpy (24 precincts), Saunders.

Maine Secretary of State by-district workbooks, 2020 against 2024 (maine_sos_president_by_cd_*.xlsx):
- CG1 to CG2: Augusta, Chelsea, Farmingdale, Hallowell, Manchester, Readfield, Winthrop.
- CG2 to CG1: Albion, Benton, Clinton, Litchfield, West Gardiner.

SIZE
I first re-ran the card's figures on the old base, and every district figure matched.
- Maine, exact: re-adding 2020's town figures onto the 2024 towns puts ME-2's 2020 Democratic two-party share at 0.4685 (not 0.4617) and ME-1's at 0.6174 (not 0.6189).
  - ME-2's 2020→2024 miss: +4.53 would be +3.17 under (a)/(c); +4.40 would be +3.06 under (b).
  - ME-1's: +4.41 would be +4.71 under (a)/(c); +4.09 would be +4.39 under (b).
  - No Maine call changes.
- Nebraska cannot be re-added exactly, because Sarpy's precinct split is not on the pages. A rough estimate puts about half of NE-1's +8.23 on the map.
- NE-2's predicted 2024 margin is only -0.58 pp ((a)/(c)) and -0.64 pp ((b)). Its call flips if a base on the 2024 lines is at least 0.29 / 0.32 points of Democratic two-party share lower than 0.5334.
  - My estimate puts that base about 0.26 points lower, so the call holds by a few hundredths of a point.
  - So the pinned 312-226 (zero districts called wrong) holds on the stand-in. The redraw is about the same size as the margin that decides NE-2's elector, so the ruled answer depends on this premise.
- The recommendation is not affected: line 188 counts states only.

Scratch scripts: C:/Users/elias/AppData/Local/Temp/claude/C--Users-elias/c8a2f04f-adf9-462b-8470-14c260fdbdb5/scratchpad/me_rebase.pl and skeptic_redraw_x/dist2024.pl.

SEVERITY: minor. No figure is wrong by the card's own definition and no pin moves, but the 2020→2024 district misses and NE-2's elector rest on a premise that is not declared.

**The skeptic's corrected fix.** Keep the computation: the ruling says to derive the districts from the previous election. Declare the stand-in in four places:
- the class doc's DECLARED list (UsStateSwingCheck.cs:26-28);
- the comment at lines 154-155;
- the card's districts row (US_ELECTIONS.md:25);
- the plan's US-4 Built line.

Suggested wording: "a district's previous-election row stands in for the next election's district of the same number, whatever its lines; 2020→2024 crosses a change of lines in both states, so those district misses carry the map as well as the swing."

On sourcing:
- Cite the pages already saved under ElectionsData/usa/raw/district/: the Nebraska canvass books' county rows by district, and Maine's by-district workbooks' town rows. No new fetch is needed.
- Those pages show that the districts' make-up changed. They do not show when or why, so do not write "2021" or "post-2020-census" unless that is fetched.
- Do not say 2012→2016 and 2016→2020 are "on one map" for Maine. Its 2012 and 2016 rows are READ from the Governor's certificates, which list no towns. Only Nebraska's books show the same county lists for 2012-2020.

The House analogue in the plan is §7 risk 9 ("Boundaries move"), not a premise. It may also be worth saying in the record that NE-2's 2020→2024 call is decided by a margin about the size of the redraw's effect, without transcribing figures into the card.

### 3. The tie wording contradicts itself when (b) and (c) tie above (a)

- **Lens:** method - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs:220

**The scenario.** Take rightOnRuled = {98, 99, 99}. Line 213 picks (b), the lowest index among the tied, and lines 216/220 add the tie clause whenever more than one method shares the best count. The card would then read 'The plan's rule recommends (b) proportional - a tie, which the rule gives to (a)'. The rule ('(a) on a tie') says nothing about a tie between (b) and (c). This cannot happen on the present pins, which are a three-way tie at 99.

**The fix proposed.** Add the tie clause only when (a) is among the tied (rightOnRuled[0] == rightOnRuled[best]). Report a (b)/(c) tie as an open question for Elias rather than resolving it by index.

**The skeptic's evidence.** The trace is correct. The branch is wrong as written, but the staged figures cannot reach it.

The wrong branch, in G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs:
- Line 213 is `int best = Enumerable.Range(0, Methods.Length).OrderByDescending(m => rightOnRuled[m]).ThenBy(m => m).First();`. For {98, 99, 99} this gives best = 1, "(b) proportional".
- Lines 216 and 220 add the clause when `Count(m => rightOnRuled[m] == rightOnRuled[best]) > 1`. Here the count is 2, so:
  - the card would read "**The plan's rule recommends (b) proportional** - a tie, which the rule gives to (a)."
  - the log would read "recommends (b) proportional (a tie, so (a))".
- R-US14 (docs/specs/USA_STAGE_PLAN.md:591) says only "the method calling the most states right on 2016→2020 and 2020→2024 ..., (a) on a tie". It does not cover (b) and (c) tied above (a). The code settles that case by index ((b) before (c)) and then says the rule gave it to (a).

Why the staged figures cannot reach it:
- Line 188 sums `p.Length - wrongBy[m].Count` for prev >= 2016 only.
- The catalog has 51 jurisdictions a year (204 state rows).
- The Pins (lines 43-44) fix StatesWrong at 3/3/3 for 2016 and 0/0/0 for 2020. So while line 211 holds, rightOnRuled = {99, 99, 99}, best = 0, and the card is right: docs/reference/US_ELECTIONS.md:47 reads "(a) normalised uniform 99; (b) proportional 99; (c) plain additive 99. **The plan's rule recommends (a) normalised uniform** - a tie, which the rule gives to (a)."
- If a pin moves, Run fails at line 211 and WriteReadings refuses to write (line 85).

The only path that reaches it is the instrument's own re-pin step. Line 85 says "re-pin, then write". Three things must happen together:
1. The catalog is regenerated.
2. The Pins are edited.
3. The new data puts (b) and (c) level above (a).

WriteReadings would then write the contradictory sentence. Run would pass, because the staleness check (line 59) compares the card with output from the same code.

Such a tie is also unlikely. In every row D + R + others = total (0 of 204 rows differ). Call (a) at line 124 passes no PartyAvailable and weights by the previous totals, so its prior national equals natP. That makes (a) identical to (c) (line 251) unless (c)'s zero floor binds somewhere. So (a) can lose a state that (b) and (c) both call right only when its re-solve after a floor flips a call.

Grade: note. It is a latent wording flaw, and today's output is correct.

**The skeptic's corrected fix.** Change only lines 213-220. Work out the tied set once:
`int top = rightOnRuled.Max(); var tied = Enumerable.Range(0, Methods.Length).Where(m => rightOnRuled[m] == top).ToList();`

Then use three branches:
1. **tied.Count == 1:** "**The plan's rule recommends {Methods[tied[0]]}**." with no clause.
2. **tied.Contains(0):** keep today's wording exactly, "**The plan's rule recommends (a) normalised uniform** - a tie, which the rule gives to (a).", and the log's " (a tie, so (a))".
3. **Otherwise ((b) and (c) tied above (a)):** name no method. Write e.g. "**The plan's rule does not decide**: (b) proportional and (c) plain additive tie above (a), a case R-US14 does not cover - asked." Use the same wording in the log line, and stop choosing by index.

Today's three-way tie at 99 takes branch 2, so the card's GENERATED block and the Pins stay byte-identical and WriteReadings does not need to run again. The C# edit still needs the cheap bar for the recompile.

### 4. The recommendation sums `prev >= 2016`, not the ruling's two named pairs

- **Lens:** method - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs:188

**The scenario.** Once the catalog holds 2028, the 2024->2028 pair enters the states-right sum, while the card still says 'on 2016 -> 2020 and 2020 -> 2024 together'. The pins check fails first (12 tuples against 9). After someone re-pins, though, the recommendation silently counts three pairs.

**The fix proposed.** if (prev == 2016 || prev == 2020), naming the ruled pairs.

**The skeptic's evidence.** Every claim in the finding checks out against the code, but the path can only open in the future, and two checks stop it until someone re-pins.

TODAY IT IS EXACT. The catalog holds 2012, 2016, 2020 and 2024 only (UsPresidentialReturns.cs lines 45-48). Tools/us_returns_prep.pl:67 hard-codes `my @years = (2012, 2016, 2020, 2024);` and parses each year's FEC sheet with its own code (lines 380-450). So `if (prev >= 2016)` (UsStateSwingCheck.cs:188) selects exactly 2016->2020 and 2020->2024, the ruled pairs. The sum is 48 + 51 = 99 per method, which matches the card ("(a) 99; (b) 99; (c) 99 ... a tie"). The proposed fix changes nothing today.

THE PATH IS REAL ONCE 2028 IS ADDED. Measure loops over the catalog's own years (line 114, `for (int i = 1; i < years.Count; i++)`), and the new plan line says the card covers "every pair of elections the catalog holds". With 2028 in the catalog, i=4 gives prev=2024, line 188 passes, and the 2024->2028 states are added to rightOnRuled. The labels at lines 214 and 218 are hard-coded: "States called right on 2016 → 2020 and 2020 → 2024 together".

THE GUARDS STOP IT ONLY UNTIL A RE-PIN:
- Line 211 (`pinned.Count == Pins.Length`) fails, 12 against 9.
- The card's digest changes, because the source digests change (line 222), so line 59 fails as stale.
- WriteReadings refuses while a pin fails (line 85).
The recommendation itself is not pinned: the Pins tuples (line 40) say nothing about which pairs it sums. So once 12 tuples are pinned, WriteReadings writes a card whose recommendation adds three pairs under a two-pair label, and nothing names the drift.

WHERE THE PREDICATE CAME FROM. The cited prototype (scratchpad us4_proto.pl) loops over a fixed list, `for my $pair ([2012, 2016], [2016, 2020], [2020, 2024])` (line 34), with the same `if $p >= 2016` (line 73). There a range test is exact. Carried into a loop over the catalog's years, it no longer is.

WHY ONLY A NOTE:
- It cannot happen until real 2028 returns exist and someone extends the generator. Today is 2026-10-06.
- A human must edit Pins in this same file first.
- The recommendation is a one-time ask for R-US14, which will have been ruled long before then.

**The skeptic's corrected fix.** Name the ruled pairs once and use them for both the sum and the label. Also check that each ruled pair is present, which the finding's `prev == 2016 || prev == 2020` would not catch if a pair were missing:

private static readonly (int Prev, int Next)[] RuledPairs = { (2016, 2020), (2020, 2024) };
// in Measure: var ruledSeen = new HashSet<(int, int)>();
// line 188:
if (RuledPairs.Contains((prev, next))) { rightOnRuled[m] += p.Length - wrongBy[m].Count; ruledSeen.Add((prev, next)); }
// after the loop:
Check(RuledPairs.All(ruledSeen.Contains), F("the ruled pairs ({0}) are in the catalog", RuledLabel));
// lines 214/218: build the "2016 → 2020 and 2020 → 2024" text from RuledPairs (RuledLabel = string.Join(" and ", RuledPairs.Select(x => F("{0} → {1}", x.Prev, x.Next)))).

With today's catalog the sums (99/99/99), the tie, the recommendation (a) and the card text all stay byte-identical, so neither the pins nor the card need regenerating.

### 5. A moved pin's FAIL line names neither the pin nor its pinned value

- **Lens:** method - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs:211

**The scenario.** If one tuple moves, the run prints 'FAIL the pinned figures (...) - 8 of 9 as pinned'. The line does not say which pair and method moved or what they were pinned at; the reader has to diff the printed per-method lines against Pins. PresidentialVoteBacktest gives each pin its own Check showing the measured value and the pinned one (PresidentialVoteBacktest.cs:276).

**The fix proposed.** Use one Check per pin, for example F("{0} -> {1} {2}: electors R {3}, states wrong {4}, districts wrong {5} (pinned {6}, {7}, {8})", ...).

**The skeptic's evidence.** The finding is accurate, and I could not refute it. UsStateSwingCheck.cs:211-212 checks all nine pins with a single Check:
  Check(pinned.Count == Pins.Length && Pins.All(x => pinned.Contains(x)),
      F("the pinned figures (electors, states and districts called wrong, per pair and method) - {0} of {1} as pinned", pinned.Count(x => Pins.Contains(x)), Pins.Length));
Check (line 52) prints only the message text after "FAIL". Trace with one moved tuple, for example (2016,1,231,3,1) measured as (2016,1,230,3,0): pinned.Count == Pins.Length (9 == 9), Pins.All(...) is false, and 8 measured tuples are in Pins. The line therefore reads "FAIL ... - 8 of 9 as pinned". The draft record says the same of n787b ("8 of 9 as pinned", "STALE", scratchpad rec787.md line 13).

The run never prints a pinned value. They exist only in the source (lines 42-44). The per-method lines (183-184) print the measured electors and the states-wrong count. Districts appear only as a list of names, so DistrictsWrong has to be counted by hand. When a pin moves, WriteReadings (line 85) says "re-pin, then write", so the developer has to compare the log against Pins by eye.

There is a second path the reviewer did not name. If the catalog gains an election (it now holds 2012/2016/2020/2024, so 3 pairs and 9 tuples), pinned.Count becomes 12 and the check fails while its line reads "9 of 9 as pinned", which contradicts itself.

The sister instrument checks each pin separately and prints both values: PresidentialVoteBacktest.cs:276, F("{0}: {1:0.000} (pinned {2:0.000})", what, got, want).

Why the grade stays at note: the check's verdict is correct. Any moved, missing or added tuple turns the bar red, and the ruling's "Done when" (pinned in the cheap bar, a stale block fails) is met. The pins are written to sb, not to the card, so the card is unaffected. Only the failure's legibility is at stake, and nine tuples can be compared by hand.

**The skeptic's corrected fix.** Replace lines 211-212 with one Check per pin, matched by (Prev, Method) and naming the pair, the method and both values. Add one more Check for measured pairs that have no pin. The reviewer's format string uses "{0} -> {1}", but Pins holds only Prev. Take the next year from the measurement: add Next to the `pinned` tuple at line 187 (pinned.Add((prev, next, m, ...))). Do not assume prev+4. Sketch:
  foreach (var pin in Pins) {
      var got = pinned.Where(x => x.Prev == pin.Prev && x.Method == pin.Method).ToArray();
      Check(got.Length == 1 && got[0].ElectorsR == pin.ElectorsR && got[0].StatesWrong == pin.StatesWrong && got[0].DistrictsWrong == pin.DistrictsWrong,
          got.Length == 0 ? F("{0} → ? {1}: pinned R {2}, states wrong {3}, districts wrong {4} - NOT MEASURED", pin.Prev, Methods[pin.Method], pin.ElectorsR, pin.StatesWrong, pin.DistrictsWrong)
          : F("{0} → {1} {2}: electors R {3}, states wrong {4}, districts wrong {5} (pinned {6}, {7}, {8})", pin.Prev, got[0].Next, Methods[pin.Method], got[0].ElectorsR, got[0].StatesWrong, got[0].DistrictsWrong, pin.ElectorsR, pin.StatesWrong, pin.DistrictsWrong));
  }
  var unpinned = pinned.Where(x => !Pins.Any(q => q.Prev == x.Prev && q.Method == x.Method)).ToList();
  Check(unpinned.Count == 0, unpinned.Count == 0 ? "every pair and method measured is pinned"
      : F("measured but not pinned: {0}", string.Join("; ", unpinned.Select(x => F("{0} → {1} {2}: R {3}, {4}, {5}", x.Prev, x.Next, Methods[x.Method], x.ElectorsR, x.StatesWrong, x.DistrictsWrong)))));
These lines go only to sb, so the card block, its digest and the n787a reading are unchanged.

### 6. A docs-only commit's bar does not run the check that guards the card's GENERATED block

- **Lens:** method - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/CheckSuite.cs:729

**The scenario.** Tools/bar_tier.ps1 (lines 70, 84-88) bars a commit that touches only .md files at the DOCUMENTS tier, which runs RunDocumentBatch. The Documents table (CheckSuite.cs:729) does not contain UsStateSwingCheck. A hand-edit inside US_ELECTIONS.md's block therefore passes that commit's bar and fails only at the next cheap bar. PresidentialVoteBacktest has the same gap, but it builds a world. UsStateSwingCheck is pure arithmetic over the compiled catalog that reads a document, which is the Documents tier's stated criterion.

**The fix proposed.** Also register ("UsStateSwingCheck", UsStateSwingCheck.Run) in Documents. It is already in Suite, which RunDocumentBatch requires.

**The skeptic's evidence.** The failing path is real.

1. Tools/bar_tier.ps1:54 puts every `.md` path in $docs: `if ($p -match '\.md$') { $docs += $p; continue }`. Line 75 (not 70, which is blank) sets `if (-not $touched.Count -and $docs.Count) { $touched += 'DOCUMENTS' }`. Lines 84-88 then print only `CheckSuite.RunDocumentBatch` and return, so no cheap bar runs. CLAUDE.md:58 says "Documents → the document batch", and :65 says the bar must be green "for the tree being committed".

2. The comment at CheckSuite.cs:716-719 says a documents-only commit "runs the checks that READ documents". The table at :729-739 holds 8 entries: Upstream, DocumentClaim, PreWiringPremise, D18Inventory, DesignNotification, PlaySheet, Mojibake and Residue. The staged diff adds UsStateSwingCheck to Suite only (:276). None of those 8 compares US_ELECTIONS.md's block with anything. D18InventoryCheck reads CLAUDE_DESIGN_ASSET_REQUEST.md, PlaySheetCheck reads docs/play/PLAY_SHEET.md, and MojibakeCheck catches only text decoded the wrong way.

3. UsStateSwingCheck.cs:58-61 reads docs/reference/US_ELECTIONS.md and fails with "STALE" when the block differs from the measurement. So a commit that touches only that card and changes its block (a hand edit, a deleted marker, a reformatted table, a move) passes RunDocumentBatch. It first fails at the next cheap bar, under an unrelated commit. There is no git hook: .git/hooks has only samples and core.hooksPath is unset.

4. The proposed fix is cheap. The check is pure static arithmetic over UsPresidentialReturns, RegionalVoteModel.RegionalSharesByUniformSwing and ElectoralCollege.Allocate. Logs/bar_timing.tsv shows it at 107 ms and 100 ms in named runs, against 1074-1235 ms for PresidentialVoteBacktest, which holds a PresidentialReferenceWorld (PresidentialVoteBacktest.cs:279-282).

Why this is only a note:
- The ruling's done-when ("pinned in the cheap bar ... a stale block fails the bar") is met by the cheap bar.
- The change copies the pattern the ruling names. PresidentialVoteBacktest's PRESIDENTIAL_VOTE.md block (§769) is also missing from Documents.
- The gap is wider than the finding says. The Documents table is the same as at s524 (its only edit since was a s579 comment change). Because line 54 runs before the ElectionsData test on line 56, ElectionsData/**/*.md files are also DOCUMENTS paths. Other cheap checks that read .md files are missing from Documents too: FiscalRulesCheck, OutOfSample2026Diagnostic, PolishDeclarationsDiagnostic, PolishSejmAllocationDiagnostic, PresidentialElectionDiagnostic, PresidentialVetoDiagnostic and DeliveredAssetCheck.
- The failure is caught at the next cheap bar, named, with the command to regenerate. Nothing reaches the game.

**The skeptic's corrected fix.** Add `("UsStateSwingCheck", UsStateSwingCheck.Run),   // the US model card's readings against the measurement` to CheckSuite.Documents. It is already in Suite, so RunDocumentBatch's guard holds, and it costs about 0.1 s.

The same edit makes two written claims about the tier's size untrue, so fix them in the same commit, preferably as REFERENCED rather than a count:
- CLAUDE.md:77 says "answers five of the eight".
- Tools/textcheck/Runner.cs:7 says "IT RUNS FIVE OF THE TIER'S EIGHT, AND THE THREE IT LEAVES ARE NAMED". Name UsStateSwingCheck as staying in the Editor, because it compiles against PoliSim.Elections and the generated catalog.

A broader fix would close the gap for both cards and any future one without growing the table. Make Tools/bar_tier.ps1 send a documents-only commit to a named run of <X> whenever a staged .md carries a `<!-- GENERATED by PoliSim.EditorTools.<X>.` stamp. That also covers PresidentialVoteBacktest's card, at about 1.1 s with its world.

### 7. Pooling 'all others' is a ruled DECLARED premise, but the card labels it SOURCED

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/docs/reference/US_ELECTIONS.md:19

**The scenario.** The ruling's DECLARED list has three items: districts swing with their state; minor candidates pooled per state; faithless electors and Maine's ranked-choice count not modelled. The card labels the districts row (25) and the not-modelled row (27) DECLARED. 'All others pooled' appears only in The input row, whose basis is 'SOURCED (§786)'. The code comment does say DECLARED (UsStateSwingCheck.cs:26). The premise carries weight. All others swing as one party, so on 2016→2020 McMullin's 2016 vote in Utah moves with the national change in all others: the card shows (a) -4.76 against (b) -0.23 there. It is also the pooled others that (c)'s floor binds on in six states. A reader of the card takes the pooling as a fact of the record, not a modelling choice.

**The fix proposed.** Give the pooling a DECLARED basis, e.g. The input → 'SOURCED (§786); DECLARED: all others pooled per state and swung as one party'.

**The skeptic's evidence.** I could not refute it. The card is the one document that labels each premise, and it leaves out one of the three premises the ruling marks DECLARED.

- **The ruling marks the pooling DECLARED.** docs/specs/USA_STAGE_PLAN.md:197 (kept verbatim above the new Built line) says: "DECLARED: the ME/NE districts swing with their state; minor candidates pooled per state; faithless electors and Maine's ranked-choice count not modelled." The plan's standing grain at :137 says: "Premises named: DECLARED or ruled".
- **The card labels it SOURCED.** docs/reference/US_ELECTIONS.md:19 reads `| The input | each jurisdiction's previous presidential vote - the two nominees, all others pooled - from the generated catalog (UsPresidentialReturns) | SOURCED (§786) |`. I searched the card for pool|others|DECLARED|minor. Line 19 is the only place the pooling appears. The only DECLARED rows are :21 (the weights), :25 (the districts, "the district rows carry the two nominees only", which is about district rows, not the state-level pool) and :27 (not modelled). The R-US14 section (:8-13) and the miss note (:29-30) say nothing about it. The two ruled premises the card does carry are labelled DECLARED; the pooling is not.
- **The code comment does say DECLARED.** UsStateSwingCheck.cs:26 reads "DECLARED: the parties are the two nominees and all others pooled per state". So the code and the card disagree.
- **"SOURCED (§786)" does not cover the premise.** §786's catalog sums the others (president_by_state.csv header: "the two nominees, all others together"), so the pooled figure is a sourced sum. Swinging that pool as one party is US-4's own premise, which the ruling marks DECLARED. §786 does not declare it.
- **The premise carries weight in every method.** Shares() (:226-227) returns [D, R, Other] and National() (:233) sums VotesOther. RegionalSharesByUniformSwing gives one swing per column (RegionalVoteModel.cs:285). Proportional (:242) scales the pool by natN[2]/natP[2], and Additive (:251) floors it at zero.
- **Utah shows the effect.** In the 2016 CSV row, others are 305523 of 1131430 (about 27%, mostly McMullin), while national others fall from 5.73% to 1.84% on 2016→2020. The card's line 158 is `| UT | +20.48 | -4.76 | -0.23 | -4.76 |`, as the finding says.
- **The floor claim checks out.** I recomputed method (c) from the CSV with a short perl script, using the same sums as National(). The floor binds only on the others share, in exactly six states on 2016→2020 (AL, FL, GA, LA, MS, NJ), and nowhere on 2012→2016 or 2020→2024.
- **No check catches it.** CardBlockOf (:279-285) compares only the block between the GENERATED stamp and the END marker. The table at :17-27 sits above the stamp, and no Editor check scans for DECLARED labels.

Severity is minor, not a defect. Nothing at runtime changes and every pinned figure stands. The premise is still recorded in the ruling text and the code comment. But the card is what R-US14 goes to the project owner with, and there the pooling reads as sourced data instead of a modelling choice.

**The skeptic's corrected fix.** Change only the hand-written table, above the GENERATED stamp. Nothing needs regenerating, because CardBlockOf compares only the block. Either change line 19's basis to "SOURCED (§786); DECLARED: the minor candidates pooled per state, the pool one party with one swing", or add a separate row, e.g. "| The parties | the two nominees and all others pooled per state, each a party with one swing - the pool's share is what (c)'s floor and (b)'s ratio act on | DECLARED |". The second option keeps line 19's SOURCED basis for the figures themselves. The plan's Built line (USA_STAGE_PLAN.md:197) needs no change: its "DECLARED in the card" list names only the two as-built additions, and the ruling text above it already names the pooling.

### 8. 'The measurement asks only how a national vote is spread over the states' is true of (a) only: (b), and (c) where its floor binds, do not add back to the shares put in

- **Lens:** claims - **reviewer:** minor - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/docs/reference/US_ELECTIONS.md:20

**The scenario.** Only (a) is re-solved to close; its residual is checked at UsStateSwingCheck.cs:125. (b) renormalises each state and stops; RegionalVoteModel.cs's own summary says proportional swing lacks this property. On 2020→2024, (b)'s states weighted by the previous totals (the card's declared weights) sum to D 48.42 % and R 49.70 %, R+1.28, against the R+1.47 put in. On 2016→2020, (c)'s states sum to 100.11 %, with others at 1.95 % against 1.84 %, because the floor binds in AL, FL, GA, LA, MS and NJ. Part of (b)'s state misses is therefore a shift in the national figure, not a spreading effect. The card names closure only in the (a) row and never says (b) or (c) lack it.

**The fix proposed.** State in the (b)/(c) rows, or in The national vote row, that they are not re-solved and their states need not sum to the national shares; only (a) closes.

**The skeptic's evidence.** I confirmed every number in the finding, but its main claim goes too far, and what is left changes nothing the card reports.

Code. Only (a) has a closure check: UsStateSwingCheck.cs:124-125 calls RegionalSharesByUniformSwing and checks `residual < 1e-9`. `Proportional` (238-245) renormalises each state and returns. `Additive` (248-253) does `Math.Max(0.0, prior[i] + natN[i] - natP[i])` and returns. RegionalVoteModel's own summary says "proportional swing does not have that property". On the card, line 22 gives the closure to (a) only. Lines 23 and 24 say neither that (b) and (c) close nor that they do not.

Measured from the generated catalog with the declared previous-election weights (scratchpad skeptic_closure.pl):
- (b) does not close on any pair. The R−D gap between what it puts out and what is put in is +0.009 pp (2012→16), +0.057 pp (2016→20) and −0.194 pp (2020→24). For 2020→24 the states sum to D 48.4207 and R 49.6982 against 48.3242 and 49.7961 put in.
- (c) on 2016→20: the states sum to 100.1074 %, others to 1.9518 against 1.8444. The floor binds in AL, FL, GA, LA, MS and NJ. It binds on the pooled others only. D and R never floor in any pair, so (c)'s weighted D, R and R−D margin add back exactly (difference 0.0000 on every pair).

Why the (c) half has no effect: the card grades only `predicted[1]-predicted[0]` (line 148). Winners compare R with D (145), and districts use `predicted[0]/(predicted[0]+predicted[1])` (156). The floor on others therefore touches no figure the card prints.

Why the (b) half is small: I re-solved (b)'s ratios until its states close (skeptic_closedb.pl and skeptic_closedb_ne2.pl).
- The states called wrong are identical on every pair: IA MI OH PA WI; AZ FL GA; none.
- ME-2 stays wrong on 2012→16. NE-2 stays wrong on 2016→20 (+0.117 becomes +0.058).
- So no pin, elector count or recommendation moves (99/99/99, a tie, so (a)).
- The mean |miss| moves 5.6428→5.6493, 2.5655→2.5482 and 2.2803→2.3684.

The one way a reader could be misled: on 2020→24 the card shows (b) 2.28 against (a) 2.46. Half of that 0.18 edge is (b)'s implied national shift toward D, not better spreading. Under §689's standing rule (regional breakdowns sum to their national figures), a proportional rule adopted would have to close.

Line 20 itself is not false. It says what goes in: all three methods get only the true national shares, and none is graded on forecasting them. Most of the gap is something the card leaves unsaid. So I grade it a note, not minor.

**The skeptic's corrected fix.** Leave line 20's sentence alone, or reword it to: "every method is handed the true national vote; the measurement asks only how it is spread - (a) alone is re-solved to give it back". In the method rows, add wording that does not depend on any measured figure:
- (b): "not re-solved: its states need not sum to the national shares (RegionalVoteModel's summary: proportional swing lacks §689's property)".
- (c): "not re-solved: where the floor binds its states sum above the national shares; the R − D margins it is graded on add back exactly while no nominee's share is floored".

Do NOT type the measured sizes (R+1.28, 100.11 %, 0.09 pp) into the card or into comments, because CLAUDE.md's claim convention forbids it. If the size is wanted, have Measure print each method's implied national D/R/others under the declared weights, both to the log and as a column in the GENERATED block. The bar then holds it.

### 9. 'mean margin miss' is the unweighted mean of |miss| over the 51 jurisdictions, districts left out, while the card defines a miss as signed

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs:111

**The scenario.** Line 149 adds Math.Abs of each miss and line 180 divides by p.Length. That makes it absolute, unweighted (DC counts as much as CA), and states plus DC only. The card's prose (US_ELECTIONS.md:29) defines 'A miss' as the signed margin difference, and the column header written at line 111 says only 'mean margin miss (pp)'. On 2012→2016 (a) the card prints 5.43, but the signed mean of the same per-state column is -1.93 (recomputed from the card's own table). A reader applying the card's own definition reads a 5-point Republican bias that is not there. The perl prototype prints 'mean |margin miss|'; the C# label dropped the bars.

**The fix proposed.** Label it 'mean |margin miss| (pp, unweighted over the jurisdictions; districts not included)' and add the same words to the definition at US_ELECTIONS.md:29.

**The skeptic's evidence.** I could not refute it. Every claim in the finding checks out against the code and the card.

1. The code computes a plain absolute mean over the states and DC only (UsStateSwingCheck.cs):
- L148: `misses[m][r] = 100.0 * ((predicted[1] - predicted[0]) - (actual[1] - actual[0]));` (this is the signed miss)
- L149: `absMiss += Math.Abs(misses[m][r]);`
- L180: `double mean = absMiss / p.Length;`. Here `p` is `UsPresidentialReturns.States` for the earlier year: 50 states plus DC, each counted once, with no vote weight.
- District misses go only into `districtMisses` (L173-174), never into `absMiss`.

2. The labels never say "absolute". The card header written at L111 reads `mean margin miss (pp)`, and the log line at L183 reads `mean margin miss {7:0.00} pp`.

3. The card defines a miss as signed. US_ELECTIONS.md:29 says: "A miss is the method's margin (Republican less Democratic) less the record's, in points of the state's vote." The tables under it print signed misses (`{0:+0.00;-0.00}`, L255). Nothing anywhere in the card says the mean column is absolute (I grepped for mean, absolute and weigh).

4. I recomputed the figures from the card's own per-state tables (perl, 51 jurisdiction rows per pair):
- **2012→2016:** the absolute means for (a), (b), (c) are 5.43, 5.64, 5.43, exactly the printed column. The signed means are -1.93, -2.22, -1.93.
- **2016→2020:** absolute 2.27, 2.57, 2.26; signed 0.73, 1.21, 0.74.
- **2020→2024:** absolute 2.46, 2.28, 2.46; signed 1.56, 1.19, 1.56.
- If the 5 districts were included, (a) on 2012→2016 would be 5.77, so the column clearly excludes them.

The format `0.00` prints a positive signed mean with no sign, and two of the three pairs have positive signed means. So an all-positive column does not show on its face that it is absolute. Read by the card's own definition, the 5.43 on 2012→2016 says the methods overstated the Republican by about 5 points. In fact they understated him by about 2 on the unweighted average.

5. The column departs from the repo's own conventions. The pattern instrument labels the same statistic in words: PresidentialVoteBacktest.cs:212 `"mean absolute miss over the field"`. The perl prototype (us4_proto.pl:74) prints `mean |margin miss|`, and the C# variable is named `absMiss`. The intent was absolute; the label lost it.

Why only minor:
- Not affected: the pins (electors, states and districts called wrong), the R-US14 recommendation (states called right) and the stale-block check.
- Affected: only the meaning of one summary column in the table R-US14 is asked with.

The proposed fix itself has a flaw. Putting `mean |margin miss| ...` into the L111 header adds bare pipes inside a GFM table header. That splits the header into 9 cells against a 7-cell delimiter row, and GFM then does not recognise the table, so the whole readings table would render as plain text.

**The skeptic's corrected fix.** 1. Header at L111: name the column in words, following PresidentialVoteBacktest's pattern, for example `mean absolute margin miss (pp; jurisdictions unweighted, districts not counted)`. If the bars are wanted, escape them as `\|margin miss\|`; a bare | breaks the table.
2. Log line at L183: change it to `mean |margin miss|` or `mean absolute margin miss`. Bars are fine there because it is not a table.
3. Card prose after line 29 (outside the GENERATED block, so no figure is transcribed): add one sentence, for example "The readings' mean is of the absolute state misses, each jurisdiction (DC among them) counted once, unweighted; the districts are listed but not in the mean."
4. Regenerate the block with `-executeMethod PoliSim.EditorTools.UsStateSwingCheck.WriteReadings`. The header sits inside the GENERATED block, so Run reports the card STALE until it is rewritten. No pin changes.

### 10. The summary's 'record' electors are as appointed (CastR + OthersRSlate), not NARA's as cast, and the card does not say so

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs:127

**The scenario.** The 2012→2016 rows show 'record 306 – 232'. The catalog's own Years row (CastR 304, CastD 227) and NARA say 304 – 227 as cast. Only §786 and the plan's US-3 line ('pledged against … cast') explain the difference. A reader of the card sees a record figure with no explanation; 'faithless electors not modelled' is only a hint.

**The fix proposed.** Head the column 'record (as appointed)', or add one sentence: each slate's electors, with faithless votes counted to their slate.

**The skeptic's evidence.** I tried to refute this and could not. Every factual claim in the finding holds.

1. The record is the as-appointed split. UsStateSwingCheck.cs:127 reads `int recR = record.CastR + record.OthersRSlate, recD = record.CastD + record.OthersDSlate;` and has no comment. The catalog row at UsPresidentialReturns.cs:47 is `(2016, "Hillary Clinton", "Donald J. Trump", 538, 227, 304, 5, 2)`, so recR = 304 + 2 = 306 and recD = 227 + 5 = 232. The card's three 2012 → 2016 rows print `| 306 – 232 |` under a column headed only `record` (:111). The log line at :128 also prints "the record {8} - {9}" with no qualifier.

2. Nothing in the change names the basis. In the staged diff, grep for "appointed", "pledged", "as cast", "slate" and "other persons" finds nothing in the card or the code. The only hints are:
   - the card's `| Not modelled | faithless electors; Maine's ranked-choice count | DECLARED |`
   - the count row `the electors in force from the catalog | §786`
   - the summary comment at :28

3. The repo spells this split out everywhere else:
   - SeatAllocationBacktest.cs:193-194 has the comment "The record's split is NARA's table - each nominee's electoral votes plus those the nominee's own electors cast for other persons (2016's, named in the catalog and not modelled)".
   - SeatAllocationBacktest.cs:201 names the variable `pledgedR`.
   - SeatAllocationBacktest.cs:211 prints "({CastR}/{CastD} as cast; … cast for other persons, … not modelled)".
   - USA_STAGE_PLAN.md:192 says "2016 306–232 pledged against 304–227 cast".
   - president_returns.md, its item 2, says "2016's split as the electors were appointed — DERIVED from NARA's own cells. NARA prints 304 – 227 as cast."
   
   The new code and card drop that qualifier.

Why this stays a note:
- The effect is limited to the 2012 → 2016 stress row's record column. The 2020 and 2024 rows carry `0, 0` in OthersDSlate and OthersRSlate.
- The pairs R-US14 is ruled on (2016 → 2020, 2020 → 2024), their 312 – 226 test and the recommendation are not affected.
- No pin reads recR or recD: Pins hold evR, states wrong and districts wrong. The value is used only for display and the " = the record" suffix at :184.
- In 2012 → 2016 every method gives 235 Republican electors, so the gap of 2 and 5 electors changes no conclusion.
- The explanation is one step away: line 6 of the card points to §786 and president_returns.md.
- The comparison basis itself is correct. The allocator assigns all 538 electors, so the as-cast count, which sums to 531, would be the wrong yardstick.

Bottom line: the figure and the code are right. What is missing is a one-line definition of what the record column counts.

**The skeptic's corrected fix.** Add one definitional sentence, with no figures, to the hand-written part of docs/reference/US_ELECTIONS.md. This needs no regeneration. For example, extend "The count" row to: "...the electors in force from the catalog; held to the record's electors as appointed: each slate's electors, with any votes they cast for other persons counted to their slate, since faithless electors are not modelled (§786 has the votes as cast)".

At UsStateSwingCheck.cs:127, add a one-line comment using SeatAllocationBacktest.cs:193-194's wording, e.g. "// the record's split as appointed: each nominee's electoral votes plus those the nominee's own electors cast for other persons (§786; faithless votes not modelled)". Optionally qualify the log text at :128 the same way ("the record as appointed").

Alternatively, head the generated column "record (as appointed)" at :111. This changes the GENERATED block, so WriteReadings must be re-run; otherwise Run fails as stale.

Do not write 306–232, 304–227 or "seven" in the prose; the claim convention forbids transcribed derived figures.

### 11. Plan Built line says 'every pair of elections the catalog holds'; the code runs consecutive pairs only

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/docs/specs/USA_STAGE_PLAN.md:197

**The scenario.** UsStateSwingCheck.cs:114-116 iterates years[i-1] → years[i], which is three of the catalog's six pairs. 2012→2020, 2012→2024 and 2016→2024 are never measured, but the line says every pair.

**The fix proposed.** 'each consecutive pair of elections the catalog holds'.

**The skeptic's evidence.** The finding is correct, but it matters very little.

1. The plan line. In docs/specs/USA_STAGE_PLAN.md:197, the Built line says the card's "readings carry the three methods on every pair of elections the catalog holds and the rule's recommendation".

2. The catalog. Assets/Scripts/Elections/Generated/UsPresidentialReturns.cs:45-48 holds four elections in `Years`: 2012, 2016, 2020 and 2024. Read literally, "every pair" of four elections is six pairs.

3. The code. Assets/Editor/UsStateSwingCheck.cs:114-116 only walks consecutive pairs:
`for (int i = 1; i < years.Count; i++) { int prev = years[i - 1], next = years[i]; ...`
That gives 2012→2016, 2016→2020 and 2020→2024. The Pins (lines 42-44) have Prev values of 2012, 2016 and 2020 only. The card's GENERATED table in docs/reference/US_ELECTIONS.md has rows for those three pairs only. 2012→2020, 2012→2024 and 2016→2024 are never measured, so the line says more than was built.

Why this is only a note:
- The first sentence of the same paragraph defines the method as deriving "from the previous presidential election ... for 2012→2016, 2016→2020 and 2020→2024".
- The code's own summary at line 38 ("per pair of elections and method") uses "pair" the same consecutive way, and line 212 does too.
- A reader in context would probably not be misled.
- No pin, check, recommendation or ruling depends on the wording. Line 188 counts R-US14's tally from the two pairs starting at 2016 or later, which is the ruling's own definition.

Where else the wording appears: "every pair" occurs nowhere else in the plan, and not in the card. The plan uses "pair" only on this line, so nothing else in the plan pins down what it means.

**The skeptic's corrected fix.** In docs/specs/USA_STAGE_PLAN.md:197, change "on every pair of elections the catalog holds" to "on each consecutive pair of elections the catalog holds". An equivalent alternative is "on each election the catalog holds, from the one before it".

Do not list the years or count the pairs. Pointing at the catalog without a figure is the form the claim convention requires. The line then stays true if the catalog gains an election, and DocumentClaimCheck is unaffected.

No other change is needed. The card does not use this wording. The code comments at UsStateSwingCheck.cs:38 and :212 can stay, because the Pins they describe show the Prev years.

### 12. The recommendation line's text is hard-coded and will disagree with its own sum after a re-pin (latent)

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs:213

**The scenario.** Line 188 sums every pair with prev >= 2016, yet lines 214/218 print 'on 2016 → 2020 and 2020 → 2024'. Lines 216/220 append 'a tie, which the rule gives to (a)' whenever the top count is shared. If (b) and (c) tie above (a), ThenBy picks (b), and the card would read 'recommends (b) proportional - a tie, which the rule gives to (a)'. A catalog extended to 2028 would also count 2024→2028 under a label naming only the two ruled pairs. Today the pins block this: 12 pinned results against 9 pins fail, and WriteReadings refuses. The first re-pin would write the mislabelled line.

**The fix proposed.** Sum exactly prev == 2016 || prev == 2020, the ruled pairs. Print the tie suffix only when (a) is among the tied methods, and say what happens otherwise.

**The skeptic's evidence.** G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs
- L188 `if (prev >= 2016) { rightOnRuled[m] += p.Length - wrongBy[m].Count; }` adds every pair whose earlier year is 2016 or later. The labels are fixed text: L214 "states called right on 2016 → 2020 and 2020 → 2024 together" and L218 "States called right on 2016 → 2020 and 2020 → 2024 together". The doc comment (L32) and the ruling name exactly these two pairs.
- L213 `best = ...OrderByDescending(m => rightOnRuled[m]).ThenBy(m => m).First()`. L216 and L220 add the tie text whenever `Count(m => rightOnRuled[m] == rightOnRuled[best]) > 1`, without checking whether (a) is one of the tied methods.
- Traced: if rightOnRuled = [98, 99, 99], then best = 1 and the card reads "**The plan's rule recommends (b) proportional** - a tie, which the rule gives to (a)." The bar log (L216) prints "(b) proportional (a tie, so (a))". The ruling says nothing about a (b)/(c) tie above (a), so the code settles it by index order.
- The pins guard the figures, not the sentence. With a 2028 catalog there would be 12 pinned results against 9 pins, so L211 fails and L85 refuses the write. After a re-pin, Measure makes the sentence and L59 compares the card to that same output, so Run passes. Nothing checks what the sentence says.

Why it is latent, not live:
- docs/reference/US_ELECTIONS.md L47 today reads "(a) 99; (b) 99; (c) 99 ... recommends (a) normalised uniform - a tie, which the rule gives to (a)". That is correct: 51 - 3 (2016→2020) plus 51 - 0 (2020→2024) = 99 for each method, all three tied, so best = 0.
- Tools/us_returns_prep.pl L67 `my @years = (2012, 2016, 2020, 2024);` plus parsers written per year. A 2028 pair needs new generator code and can't exist before the 2028 count. Adding an earlier year (2008) is harmless because `prev >= 2016` leaves it out.
- The (b)=(c)>(a) tie needs the data or one of the methods to change. The likeliest route: RegionalSharesByUniformSwing is the live Sweden/Germany model (NationalElection.cs L317, L335), and R-US14 is "one rule across countries", so a ruling for (b) or (c) could touch it. Today (a) and (c) call the same states wrong on every pair.
- The plan's Built line says the readings cover "every pair of elections the catalog holds", so the table is meant to grow while the rule's sum is not. That supports half 1 of the finding.

**The skeptic's corrected fix.** In UsStateSwingCheck.Measure:
(1) Make the ruled pairs one constant, e.g. `RuledPairs = { (2016, 2020), (2020, 2024) }`. At L188, add only when `RuledPairs.Contains((prev, next))` and record each pair counted. After the loop, `Check` that both ruled pairs were counted. Build the L214/L218 label from that constant, so the sum and the label can't disagree when the catalog grows (the table can keep every pair).
(2) Work out the leaders first: `top = rightOnRuled.Max()`, `leaders = methods with rightOnRuled[m] == top`.
- If there is one leader, recommend it with no tie text.
- If (a) is among two or more leaders, recommend (a) with the tie text as it stands.
- If two or more leaders tie and (a) is not one of them, do not print a recommendation. Fail a `Check`: "a tie between methods other than (a) is not decided by R-US14 - ask". WriteReadings already refuses on any failure. If it does, make the L85 message say "a check failed", not "a pin moved".
The code should not settle a case the ruling leaves open by index order.

### 13. 'Refuses while a pin fails' / 'a pin moved' fire on any failed check, not only a pin (inherited from the pattern)

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/UsStateSwingCheck.cs:85

**The scenario.** Measure also fails on the catalog's year count (108), on jurisdictions that differ or change order (119), on (a)'s residual (125) and on district counts (153). Any of these makes WriteReadings print 'a pin moved - the card is not written; re-pin, then write', which points the reader at the Pins array. The doc comment at 70-71 says the same. Both are copied verbatim from PresidentialVoteBacktest.cs:100/114.

**The fix proposed.** 'Refuses while any check fails' / 'a check failed (above) - the card is not written'.

**The skeptic's evidence.** Confirmed from the staged file (the working tree matches the index for this path).

1. One counter for every check. In WriteReadings, :76-77 `int failures = 0; void Check(bool ok, string what) { if (!ok) { failures++; } ... }` and :81 `string digest = Measure(sb, Check, card);`. So every Check inside Measure adds to the same `failures`.

2. Most of Measure's checks are not pins: :108 (the catalog holds at least two years), :119 (the same jurisdictions in the same order), :125 `Check(residual < 1e-9, ...)` ((a) closes on the national shares) and :153 (the district counts match). The only pin check is :211-212 (`pinned.Count == Pins.Length && Pins.All(...)`).

3. The refusal at :85 is `if (failures > 0) { Check(false, "a pin moved - the card is not written; re-pin, then write"); }`, so it fires on any of those failures.

4. A failing path. RegionalVoteModel.RegionalSharesByUniformSwing (RegionalVoteModel.cs:242 on; its correction loop has a cap of 200, :293) reports its residual through `out worstAbsError`. Suppose a later change to the model leaves that residual at or above 1e-9:
   - :125 fails.
   - The shares that come back differ only negligibly, so the pinned integers (R electors, states wrong, districts wrong) do not move, and :211 prints `ok ... 9 of 9 as pinned`.
   - Measure returns normally, and :85 then prints `FAIL a pin moved - ... re-pin, then write`.
   That line is false, and its advice is wrong: re-pinning would not fix anything. The same thing happens at :153 when the earlier year has more district rows than the later one, because the loop runs only to dNext.Length and nothing throws.

5. Where the finding goes too far ("any of these"):
   - If a mismatch at :119 or :153 makes `n[r]` or `dPrev[k]` run out of range, Measure throws. The catch at :95 prints "threw" and :85 is never reached.
   - If years.Count < 2, `pinned` is empty, so :211 fails as well (0 of 9). There "a pin moved" is roughly true.
   So the false message shows up on the residual path and on one direction of the district-count path.

6. The doc comment at :70-71, "Refuses while a pin fails: the card is never written over a reading the pins no longer hold", is true but incomplete. By the claim convention's own test (CLAUDE.md, "only incomplete") that is not a claim error.

7. Both texts are copied verbatim from PresidentialVoteBacktest.cs:100 and :114. That file's Measure also has non-pin checks (:137, :141, :142, :154, :171, :188, :257, :284), so it has the same imprecision, outside this change.

Impact is cosmetic only:
- The bar's verdict, the card, the pins and every figure are unaffected.
- The refusal itself (do not write while anything fails) is correct.
- WriteReadings is a manual command.
- The specific FAIL line, and an `ok` pins line that contradicts the message, print just above it in the same log.
Grade: note.

**The skeptic's corrected fix.** At UsStateSwingCheck.cs:85, change the message to: `Check(false, "a check above failed - the card is not written; fix what failed (re-pin only where a pin moved), then write");`.

At :70-71, change the comment to: "Refuses while any check of the reading fails, the pins among them: the card is never written over a reading this check no longer holds."

Measure's summary at :102-104 says "each pin through Check" and could say "each check through Check".

To keep the pattern as one text, make the same two edits at PresidentialVoteBacktest.cs:100 and :114. That file is outside this change, so it is optional here.

### 14. A commit that touches only the card never runs this check (inherited from PRESIDENTIAL_VOTE.md's pattern)

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/CheckSuite.cs:729

**The scenario.** bar_tier.ps1 classes a commit that changes only .md files as DOCUMENTS, which owes RunDocumentBatch. That batch's Documents group (729-739) does not include UsStateSwingCheck. A hand edit inside US_ELECTIONS.md's GENERATED block, committed alone, passes the bar it owes and fails only at the next cheap bar. The card's own sentence ('fails the cheap bar') is accurate, but the Done-when's 'a stale block fails the bar' holds only for commits that owe the cheap bar.

**The fix proposed.** Optional: add UsStateSwingCheck to the Documents group. It reads only the catalog and the card and builds no World.

**The skeptic's evidence.** I could not refute it. The failing path exists, and nothing in the code catches it at the commit that makes the card stale.

1. **A card-only commit owes the document batch.** Tools/bar_tier.ps1:54 `if ($p -match '\.md$') { $docs += $p; continue }`. Line 75 sets DOCUMENTS only when nothing else was touched. Lines 84-88 print `per item : CheckSuite.RunDocumentBatch` and return. CLAUDE.md:58 says "A commit owes every tier it touches. Documents -> the document batch".

2. **That batch does not run the new check.** CheckSuite.cs:729-739 `Documents` lists UpstreamCheck, DocumentClaimCheck, PreWiringPremiseCheck, D18InventoryCheck, DesignNotificationCheck, PlaySheetCheck, MojibakeCheck and ResidueCheck. Line 765 runs `RunTable(Documents, ...)` and nothing else. The staged diff adds UsStateSwingCheck only to `Suite` (line 276).

3. **No other doc-batch check reads the card.** UsStateSwingCheck.cs:58-61 is the only reader of the block in docs/reference/US_ELECTIONS.md. DocumentClaimCheck.cs:124 and PreWiringPremiseCheck.cs:137 read only root `*.md` files (TopDirectoryOnly). There is no git hook: .git/hooks holds only samples and core.hooksPath is unset.

**Result.** Three kinds of commit pass the bar they owe and fail only the next cheap bar, which runs on some unrelated commit:
- a hand edit inside the GENERATED block;
- a docs sweep that touches the block;
- a move or rename of the card (`CardPath` then throws).

COMPLETED.md s524 Decision 1 names exactly this as what the tiers are meant to avoid ("would first fail an unrelated commit"). CLAUDE.md asks for one green bar "for the tree being committed".

**It also breaks the group's own rule.** The doc comment at 717-719 says the checks left out "read code, art and artifacts such a commit cannot have moved". This check reads a document that such a commit can move. PlaySheetCheck and D18InventoryCheck have the same shape (regenerate in memory, compare the document) and both are in `Documents`.

**Inherited, as the finding says.** PresidentialVoteBacktest (Suite:217) guards PRESIDENTIAL_VOTE.md the same way and is also missing from `Documents`.

**Why it stays a note:**
- The block carries "DO NOT EDIT BY HAND" (US_ELECTIONS.md:34).
- The card's own sentence (line 5, "fails the cheap bar") is accurate.
- The Done-when reads "pinned in the cheap bar ... a stale block fails the bar", so "the bar" is the cheap bar and the item is met as written. The finding over-reads this a little; the gap is in the tiering, not in US-4's acceptance.
- The failure is delayed, not lost. The STALE message names the card and the regeneration command.

**The skeptic's corrected fix.** Optional, one line: add `("UsStateSwingCheck", UsStateSwingCheck.Run),` to `CheckSuite.Documents` (Assets/Editor/CheckSuite.cs:729-739), commented e.g. "docs/reference/US_ELECTIONS.md's readings against the measurement".
- It reads only the compiled catalog and the one card and builds no World.
- It is already in `Suite`, so RunDocumentBatch's in-the-cheap-suite guard (756-763) still holds.
- PresidentialVoteBacktest has the same gap. Add it the same way only after checking its cost, since it holds a PresidentialReferenceWorld (line 282); otherwise record the gap as known.

Out of this change's scope, for the record: bar_tier.ps1:54 also classes ElectionsData/**/*.md source records as DOCUMENTS, because that test runs before the ElectionsData rule at :56. FiscalRulesCheck, PolishSejmAllocationDiagnostic, PresidentialVetoDiagnostic, PolishDeclarationsDiagnostic, PresidentialElectionDiagnostic and PresidentialVoteBacktest all read such files. So the document tier does not cover every check that reads a document, well beyond this one card.

### 15. 2020→2024 district rows cross the 2021 Maine/Nebraska redraw, and the card never DECLARES it

- **Lens:** recompute - **reviewer:** minor - **skeptic:** minor
- **Where:** docs/reference/US_ELECTIONS.md:25

**The scenario.** UsStateSwingCheck.cs:150-151 pairs each 2020 district with the 2024 district of the same number and swings it with its state. The repo's own raw pages show that both states redrew their districts between those two elections.

Maine (ElectionsData/usa/raw/district/maine_sos_president_by_cd_2020.xlsx and _2024.xlsx, the town rows on each district's sheet; the 2020 sheet totals equal the catalog's district rows exactly):
- Augusta, Chelsea, Farmingdale, Hallowell, Manchester, Readfield and Winthrop moved from CD1 to CD2.
- Albion, Benton, Clinton, Litchfield and West Gardiner moved from CD2 to CD1.
- Re-summed onto the 2024 lines, 2020's ME-2 is R+6.30 (two-party), not R+7.66.
- So the card's 2020→2024 ME-2 miss of +4.53 under (a)/(c) is +3.17 like for like, and +4.40 under (b) is +3.06. ME-1 goes from +4.41 to +4.71 under (a)/(c) and from +4.09 to +4.39 under (b). No winner call changes.

Nebraska (nebraska_sos_general_canvass_book_2020.pdf and _2024.pdf, the 'Results by Congressional District' page):
- NE-2 was Douglas plus 31 Sarpy precincts in 2020. In 2024 it is Douglas plus 24 Sarpy precincts plus Saunders County, which was in NE-1 in 2016 and 2020.
- Saunders' 2020 vote (Trump 9,108, Biden 3,331) alone moves NE-2's 2020 base from -6.68 to -4.75: 1.92 pp toward R.
- That is more than the 0.58 pp (a, c) and 0.64 pp (b) by which the card predicts NE-2 Democratic in 2024.
- The Sarpy cut (61,336 two-nominee votes in NE-2 in 2020, 29,382 in 2024) pulls the other way by an amount the county-level pages cannot settle.
- A rough estimate, not a measurement, that holds the 2024 Sarpy split's lean at 2020 turnout puts the net at +0.55 pp, at the line.

So the card's 2020→2024 results, 'districts called wrong: none' and '312 – 226 = the record' under every method (the answer US-4's Done-when asks for), rest on a premise the card does not declare. The plan itself flags this premise: USA_STAGE_PLAN.md:722, risk 9, 'a district swing misses where the map moved'; and :275, where US-11 drops district swing on the 2020→2022 redraw cycle. The 2012→2016 and 2016→2020 rows are on one map in each state as far as the books show.

**The fix proposed.** DECLARE in the card's districts row, and in the plan's US-4 Built line, that 2020→2024 swings each district's 2011-map result onto its 2021-map record. Name the moved Maine towns and Nebraska's Saunders and Sarpy changes, and carry the premise into R-US14's ask.

Maine can be re-based from the town rows already on disk, either in place of the current figures or printed beside them. NE-2 cannot be re-based from county-level pages; say so, together with how close it sits to a flip (0.58 to 0.64 pp).

**The skeptic's evidence.** I could not refute it. Each step of the scenario checks out against the code and the saved pages.

THE CODE pairs districts by number with nothing about maps. Assets/Editor/UsStateSwingCheck.cs:150-151 sorts dPrev and dNext by district. :153 only compares how many districts each year has. :160 takes the base from dPrev[k] and :169 takes the record from dNext[k]. Nothing in the catalog (president_by_district.csv: year, state, district, votes_d, votes_r, basis) says which map a row was drawn on.

THE CARD does not declare it. docs/reference/US_ELECTIONS.md:25, the districts row, declares only "the district rows carry the two nominees only". The hand-written part of the card says nothing about a redraw, and neither does the plan's US-4 Built line or the record (COMPLETED.md §786). The plan does name the risk: USA_STAGE_PLAN.md:722 "a district swing misses where the map moved", and :275 leaves district swing out across the House redraw.

MAINE (I unzipped the saved by-CD workbooks; the 2020 CG totals equal the catalog's rows):
- Augusta, Chelsea, Farmingdale, Hallowell, Manchester, Readfield and Winthrop are on the CG1 sheet in 2020 and the CG2 sheet in 2024.
- Albion, Benton, Clinton, Litchfield and West Gardiner go the other way.
- Every other difference between the two years' town lists is a change of name within the same district.
- Re-summed onto the 2024 lines, 2020's ME-2 is D 176,984 / R 200,794, which is R+6.30 two-party against R+7.66 as drawn. ME-1 is -23.48 against -23.77.
- An independent perl run of (b) and (c) matches the card to 0.01 for all five districts. On the 2024 lines it gives ME-2 +3.17 (c) and +3.06 (b), and ME-1 +4.71 (c) and +4.39 (b). These are the finding's figures exactly. No winner call changes.

NEBRASKA (pdftotext of the canvass books):
- 2012, 2016 and 2020 have the same lists: NE-1 holds Saunders and Sarpy's 23 precincts, NE-2 is Douglas plus Sarpy's 31 precincts.
- In 2024 NE-2 is Douglas, Sarpy's 24 precincts and Saunders. NE-1 has lost Burt, Dixon's 2 precincts, Otoe, Thurston, Washington, most of Polk (3 precincts left) and Saunders, and holds 57 Sarpy precincts.
- Saunders' 2020 vote (9,108 R / 3,331 D) moves NE-2's 2020 base from -6.68 to -4.75.
- Holding the 2024 Sarpy split's lean at 2020's county vote gives a base of about -6.15, a net +0.53 toward R. The predicted 2024 NE-2 margin is then about -0.05 (c) and -0.11 (b) against the card's -0.58 and -0.64: still called right, but at the line.

The impact is wider than the finding says. By the same rough estimate, NE-1's 2020->2024 miss of +8.23 ((a)/(c)) and +7.98 ((b)) is about +4.0 and +3.9 on like-for-like lines. That miss is the largest in the whole 2020->2024 table, and about half of it is the map moving, not the method. NE-3's +5.94 (c) becomes about +4.6.

WHY MINOR AND NOT A DEFECT:
- None of the nine pins changes under the best estimate: ME stays R, and NE-2 most likely stays D.
- The R-US14 recommendation counts states only (UsStateSwingCheck.cs:188, rightOnRuled += p.Length - wrongBy.Count), so the tie that goes to (a) is unaffected.
- The code does what the owner's ruling literally says.
- What is wrong is an undeclared premise behind the card's 2020->2024 district rows, and the '312 – 226 = the record' answer leans on NE-2 at the line.

**The skeptic's corrected fix.** 1. Declare the premise in words in three places:
   - the card's hand-written districts row (US_ELECTIONS.md:25);
   - the plan's US-4 Built line;
   - the R-US14 ask.
   The words to say: 2012->2016 and 2016->2020 are on one map in each state. 2020->2024 pairs each district's 2020 result on the 2011 lines with its 2024 record on the 2021 lines. The 2020->2024 district misses are therefore not like for like, NE-1's above all. NE-2's call cannot be re-based from county-level pages.
2. Point to where the moved towns and counties show, rather than typing them: raw/district/maine_sos_president_by_cd_2020.xlsx and _2024.xlsx, and the Nebraska canvass books' 'Results by Congressional District' pages.
3. Do not type derived figures into the hand-written prose: not the +6.30 / +7.66 bases and not the 0.58 to 0.64 pp distance from a flip. The claim convention and DocumentClaimCheck forbid that.
4. If the like-for-like Maine figures are wanted:
   - have Tools/us_returns_prep.pl emit Maine's 2020 districts on the 2021 lines from the saved town rows, as catalog rows that carry their map;
   - have UsStateSwingCheck.WriteReadings print those figures beside the current ones in the GENERATED block;
   - have it mark Nebraska's re-base as not determinable from county pages.
5. Pins and the R-US14 recommendation need no change: the recommendation counts states only.

### 16. The 'mean margin miss' column is the mean of |miss|, but the card defines a miss as signed

- **Lens:** recompute - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/UsStateSwingCheck.cs:111

**The scenario.** Card line 29 defines a miss as 'the method's margin ... less the record's', a signed quantity. The column headed 'mean margin miss (pp)' (header written at :111, figure computed at :149/:180, Debug line at :183) prints sum|miss|/51: unweighted, over the 51 jurisdictions, districts excluded.

Read with the card's own definition, the column suggests a directional bias, and the signed means differ in size and sometimes in sign:
- 2012→2016 (a): printed 5.43, signed mean -1.93 (toward D).
- 2016→2020 (b): printed 2.57, signed mean +1.21.
- 2020→2024 (a): printed 2.46, signed mean +1.56.

**The fix proposed.** Head the column 'mean |margin miss| (pp, unweighted, 51 jurisdictions)' and use the same words in the Debug line at :183. Optionally add the signed mean as a generated column, since the direction is what R-US14's reader will ask about.

**The skeptic's evidence.** The finding holds. The figure the column prints is right, but its label does not match the card's own definition of a miss.

1. The code computes the mean of absolute misses over states only (Assets/Editor/UsStateSwingCheck.cs):
   - :148 `misses[m][r] = 100.0 * ((predicted[1] - predicted[0]) - (actual[1] - actual[0]));` is signed.
   - :149 `absMiss += Math.Abs(misses[m][r]);` adds only the 51 jurisdiction rows. District misses go into `dm[m + 1]` at :174 and never reach `absMiss`.
   - :180 `double mean = absMiss / p.Length;` counts each jurisdiction once, with no weighting.

2. Neither label says "absolute":
   - :111 heads the column `mean margin miss (pp)`.
   - :183 writes `mean margin miss {7:0.00} pp`.
   - A grep of the card and the .cs for "absolute", "|miss|" or "unweighted" finds only the variable name `absMiss`.

3. The card defines a miss as signed. docs/reference/US_ELECTIONS.md:29 reads "A miss is the method's margin (Republican less Democratic) less the record's", and the per-state tables (:53-230) print signed misses.

4. The house pattern says "absolute". The precedent this instrument copies, Assets/Editor/PresidentialVoteBacktest.cs:212-213, writes "mean absolute miss over the field" and names its pin "...the field's mean absolute miss". GermanRegionsDiagnostic, OutOfSample2026Diagnostic, PreStartRecordDiagnostic and VoteShareBacktest also say "mean absolute deviation".

5. I recomputed both figures in perl from the card's own 2-dp rows, 51 per pair, districts excluded.
   - Mean |miss| matches every printed figure exactly: 5.43/5.64/5.43, 2.27/2.57/2.26 and 2.46/2.28/2.46.
   - The signed means are:
     - 2012→2016: a -1.93, b -2.22, c -1.93
     - 2016→2020: a +0.73, b +1.21, c +0.74
     - 2020→2024: a +1.56, b +1.19, c +1.56
   - The finding's three quoted figures are confirmed (rounding error at most ±0.005).
   - A reader who takes the line-29 definition at its word would read the printed 5.43 for 2012→2016 as a 5.43 pp lean toward the Republicans. The signed mean is actually a 1.93 pp lean toward the Democrats.

Why minor and not a defect:
- The figure is computed correctly for what was intended (the task text itself says "mean |margin miss|").
- It enters no pin (Pins :40-45) and does not affect the recommendation. That rests on states called right, :188 and :213.
- So the problem is a generated label that misstates its statistic, not a wrong number.

**The skeptic's corrected fix.** Rename the statistic where it is generated, and use the precedent's word.
- Line 111: head the column with something like `mean absolute margin miss (pp)`, or `mean |margin miss| (pp)`.
- Line 183: change the Debug text to `mean absolute margin miss {7:0.00} pp`.
- In words, say that each jurisdiction counts once and the districts are not included.

Do not hard-code "51 jurisdictions" as the finding's fix suggests. Under the claim convention a typed count is a transcribed DERIVED figure. Either word it without the number or format `p.Length` in.

The header line at :111 is one of the card lines that Run compares against the file (:56-61). So changing only the code leaves the on-disk block STALE and fails the bar. After the code change, regenerate the block with `-executeMethod PoliSim.EditorTools.UsStateSwingCheck.WriteReadings`. Do not hand-edit it; it is GENERATED. The pins and the digest are unaffected.

Optional:
- Add a generated signed-mean column, since direction is what R-US14's reader will ask about.
- Add a sentence to the hand prose at card :29-30 saying the table's mean is of the misses' absolute values. That is a definition, not a figure.

### 17. (a) and (c) are the same method on 2012→2016 and 2020→2024; the table separates them only on 2016→2020

- **Lens:** recompute - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/UsStateSwingCheck.cs:218

**The scenario.** Where no zero floor binds, (a)'s closure returns at pass 0 and (a) equals (c) to 1e-12 pp. That is the case for 2012→2016 (smallest prior+swing share 0.040) and 2020→2024 (0.0095).

Only in 2016→2020 does the others' national share fall 3.89 pp. There the floor bites in AL, FL, GA, LA, MS and NJ, and the closure needs 13 corrections, which makes (a) and (c) differ by up to 0.31 pp (MS).

So the 99/99/99 tie, and the recommendation of (a) over (c), contain no measured difference between (a) and (c) on two of the three pairs. The card does not say this. A reader could take three columns for three independent tests.

**The fix proposed.** Add one generated line per pair naming the states where a floor binds, and therefore where (a) and (c) can differ. R-US14 is then read as (a)/(c) against (b) except on 2016→2020.

**The skeptic's evidence.** I could not refute the finding. Every figure in it reproduces, and the card never says that (a) and (c) coincide.

Why (a) equals (c) when no floor binds:
- UsStateSwingCheck.cs:122 builds the regions with PartyAvailable = null. RegionalVoteModel.cs:280 and :285 then set standingWeight = totalWeight and swing[p] = natN[p] - priorNational[p].
- priorNational (lines 259-268, weighted by VotesTotal) equals National(p).
- ApplySwing (lines 339-347) adds prior + swing, floors at zero, then divides by the row sum.
- In president_by_state.csv, votes_d + votes_r + votes_other = votes_total on all 204 rows, so each row sums to 1. The swing sums to 0. With no floor the divisor is 1, the rebuilt total equals natN, and line 310 breaks at pass 0.
- Additive (UsStateSwingCheck.cs:248-253) computes max(0, prior + natN - natP), which gives the same numbers.

Recomputed independently (scratchpad us4_skeptic/ac_diff.pl):
- 2012→2016: smallest prior+swing 0.0400 (OK, others); 0 corrections; largest |(a)-(c)| margin 1.8e-14 pp.
- 2020→2024: smallest 0.0095 (FL, others); 0 corrections; 1.1e-14 pp.
- 2016→2020: others fall 3.89 pp. Floors bind in AL, FL, GA, LA, MS and NJ on the first pass; 13 corrections; largest difference 0.31 pp, in MS.

What the card shows: rows 37/39 and 43/45 are identical. Rows 40/42 have the same calls (AZ, FL, GA; 230-308; no districts) and mean misses of 2.27 against 2.26. Line 47 says only "a tie, which the rule gives to (a)". A grep finds no line saying that (a) and (c) coincide where no floor binds.

The finding understates its own point:
- The state call (line 145) and the district call (line 156, the two-party share) do not change under row renormalisation, and only "others" is ever floored.
- So (a) and (c) calls can differ only through the closure's swing correction. On 2016→2020 that correction is D +0.064 pp and R +0.066 pp, a 0.002 pp change in margin. On the rule's metric, (a) and (c) are tied by construction on all three pairs.

Why this is a note, not a defect:
- The instrument does exactly what was ruled: "normalised" against "plain" additive swing.
- Lines 22 and 24 of the card state the only difference between them.
- The tables show the identical columns, and the recommendation is openly called a tie-break.
- Nothing is misreported. The card just never says that the US data can barely tell (a) from (c), which matters for a rule meant to hold "across countries".

**The skeptic's corrected fix.** The finding's own fix is imprecise. "The states where a floor binds" does not mark where (a) and (c) differ:
- Once any floor binds, (a)'s re-solve moves every state's swing. On 2016→2020, all 51 jurisdictions differ by more than 1e-9 pp.
- (a)'s final swing also floors NC: 7 states for (a) against 6 for (c).

Instead, Measure should emit one generated card line per pair. It should give:
- the largest |(a)-(c)| margin difference;
- how many state and district calls differ between (a) and (c) (0 on all three pairs);
- the floor-bound states for (a) at its final swing and for (c).

Keep it in the generated block, so the claim convention holds.

The card should then say what that line means. With every party standing everywhere and the pooled rows summing to one, (a) is (c) renormalised and re-solved. Renormalisation never changes whether D or R leads. So R-US14's states-called-right comparison is in effect (a)/(c) against (b) on every pair. The choice between (a) and (c) is only about margins and floors, and the US data tests that only on 2016→2020.

### 18. The 'record' column for 2016 is the slates' 306–232, not NARA's as-cast 304–227

- **Lens:** recompute - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/UsStateSwingCheck.cs:127

**The scenario.** recR = CastR + OthersRSlate and recD = CastD + OthersDSlate, so 2016's record prints as 304+2 / 227+5 = 306 – 232. That is the right comparison, since faithless electors are not modelled. But the column is headed only 'record', and a reader checking it against NARA's well-known 304 – 227 sees a mismatch the card does not explain.

**The fix proposed.** Head the column 'record (electors won by each slate)', or add the faithless votes to the card's 'Not modelled' row.

**The skeptic's evidence.** I could not refute it. The figure is right, and the card does not say what kind of count it is.

1. How 306–232 comes about. Line 127 of UsStateSwingCheck.cs is `int recR = record.CastR + record.OthersRSlate, recD = record.CastD + record.OthersDSlate;`. The catalog's 2016 row (UsPresidentialReturns.Years) is `(2016, "Hillary Clinton", "Donald J. Trump", 538, 227, 304, 5, 2)`, so recR = 304+2 = 306 and recD = 227+5 = 232. Line 111 heads the column just `| record |`. Line 185 writes `{5} – {6}` = recR – recD into that column. Card lines 37-39 (all three 2012 → 2016 rows) print `306 – 232`.

2. Nothing in the card or the check says the figure is 'as appointed' or 'pledged'. I searched US_ELECTIONS.md and UsStateSwingCheck.cs for pledg, appoint, 'as cast' and 'other persons' and got no hits. The only related line is card line 27: `| Not modelled | faithless electors; Maine's ranked-choice count | DECLARED |`. That row says the model leaves faithless electors out. It does not say the record column counts electors as appointed, so a reader can only infer it. Line 6 points generally to §786 and president_returns.md, where the explanation is, but it does not tie that pointer to this column.

3. The project labels this figure everywhere else, and the new card is the exception:
- president_returns.md reading 2: "2016's split as the electors were appointed — DERIVED ... NARA prints 304 – 227 as cast".
- COMPLETED.md §786: "2016 Trump 306 - Clinton 232 as appointed, against NARA's 304 - 227 as cast".
- USA_STAGE_PLAN.md line 192: "2016 306–232 pledged against 304–227 cast".
- SeatAllocationBacktest.cs, the US-3 instrument, lines 201 and 210-211: it names the variable `pledgedR` and adds `({record.CastR}/{record.CastD} as cast; {n} cast for other persons, named in the catalog, not modelled)` whenever OthersRSlate + OthersDSlate > 0. The new check copies the sum at line 127 but drops that note, both on the card and on the console line at 128-129.

4. Why only a note. recR and recD feed two places only: the ' = the record' console tag at line 184 and the card cell at line 185. None of these reads them: the Pins (lines 40-45), the states and districts called wrong, the mean miss, or the recommendation (line 213). Using the pledged split is also the right comparison, because the allocator models no faithless electors. The 7-elector gap is small next to the 2012 → 2016 miss (235 against 306), so the R-US14 table does not mislead the ruling. The problem is a confusing label, not a wrong number.

**The skeptic's corrected fix.** Do not take the finding's second option, adding the faithless votes to the 'Not modelled' row. That row is hand-written text outside the GENERATED block, so typing counts there breaks the claim convention (CLAUDE.md), and DocumentClaimCheck would flag it.

Generate the label inside the block instead, following SeatAllocationBacktest.cs lines 210-211:

1. At line 111, head the column `record (as appointed)`.

2. At line 185, build the cell from the catalog:
```csharp
string recCell = record.OthersRSlate + record.OthersDSlate == 0
    ? F("{0} – {1}", recR, recD)
    : F("{0} – {1} ({2} – {3} as cast; {4} cast for other persons, not modelled)", recR, recD, record.CastR, record.CastD, record.OthersRSlate + record.OthersDSlate);
```
Pass recCell in place of `{5} – {6}`.

3. Make the same change to the console line at lines 128-129.

4. Re-run UsStateSwingCheck.WriteReadings so the block is regenerated. Run will report the old block as stale until then, which is expected.

5. Optionally, the hand-written 'Not modelled' row can carry a reference with no figures in it, e.g. "the record's electors are each slate's as appointed (`ElectionsData/usa/president_returns.md`, its reading on 2016's split)".

## The first pass - refuted by the skeptics

- [method] (c)'s margin is read on shares that sum to more than one where the floor binds, but the card says 'in points of the state's vote' - *The finding's figures are all correct. I checked them with my own perl script over ElectionsData/usa/president_by_state.csv (scratchpad skeptic_c_margin.pl). In 2016->2020 the floor at UsStateSwingCheck.cs:251 (`s[i] = Math.Max(0.0, prior[i] + natN[i] - natP[i]);`, no renormalisation) binds in exactly AL, FL, GA, LA, MS and NJ. (c)'s shares there sum to between 1.00297 and 1.01941. The floor never binds in 2012->2016 or in 2020->2024. Line 148 (`misses[m][r] = 100.0 * ((predicted[1] - predicted[0]) - (actual[1] - actual[0]));`) reads the margin on the unnormalised shares. For MS the margin is +15.469 unnormalised and +15.175 normalised. MS's (c) miss is -1.076 as read, -1.371 if normalised, and (a)'s is -1.388.

What does not hold is the claim that the card is inaccurate.
(1) The floor zeroes only the 'others' share, and 'others' is not part of R - D. So (c)'s margin is exactly the previous margin plus the national change in margin. That is the plain additive (uniform) swing on the margin, the form R-US14's (c) names.
(2) The card defines a miss as "the method's margin ... less the record's" (US_ELECTIONS.md:29), and line 24 says (c) is "floored at zero, not renormalised". By the card's own definitions, then, (c)'s miss is read on its unnormalised shares. The code comment (UsStateSwingCheck.cs:23-24) says the same.
(3) "In points of the state's vote" says which denominator is used, as opposed to the districts' "two-party vote". Each (c) share is a fraction of the state's vote. Under the card's DECLARED weights (line 21: a state weighs what it cast in the previous election, and no figure of the predicted election enters), the state's vote in the model is the previous total. The finding's own proposed wording, "points of the previous total", names that same quantity. So the sentence is not demonstrably false.

Nothing that decides anything moves. Line 145 (`predicted[1] > predicted[0]`) and line 156 (`stateNext = predicted[0] / (predicted[0] + predicted[1])`) do not change when the shares are scaled by S. So every pin, call, elector count, district call and the recommendation (by states called right) is the same either way. The only visible effect is six (c) cells for 2016->2020, each changing by at most 0.30 pp, and (c)'s 2016->2020 mean miss: 2.2634 as read (the card shows 2.26) against 2.2697 normalised. (a)'s is 2.2711, so (c) stays below (a) either way.

The finding's alternative fix should not be applied. Scoring (c) on normalised shares would score 'additive, floored, renormalised, not re-solved', which is (a) without its re-solve. That would erase the very difference between (a) and (c) that R-US14 asks to be ruled on.*
- [claims] 'No figure of the predicted election enters but its national shares' is false: its electors and its district list enter the count - *The finding gets the code right but misreads the claim. The parenthetical is accurate where it applies, and the count's electors are written down where the count is described.

1. The claim is about the weights. In the card, "DECLARED: no figure of the predicted election enters but its national shares" is the basis cell of the row "The weights | a state weighs what it cast in the previous election" (US_ELECTIONS.md:21). The count has its own row and basis two rows down (:26, §786). In UsStateSwingCheck.cs:26-27 the same parenthetical hangs on "a state weighs what it cast in the previous election".
   - Within that scope it is exactly true. The derivation reads natN, regions built from p's VotesTotal, and prior built from p (:121-124). Proportional and Additive read prior, natP and natN (:143). The districts read dPrev, prior and predicted (:156-167).
   - n[r] and dNext's votes appear only as the record each prediction is compared against (:144, :169-170).
   - A fully global reading cannot be what is meant anyway: the predicted election's state votes plainly enter the misses.

2. The electors are not "a figure of the predicted election". In this repo, "the electors in force" is a statutory term. Tools/us_returns_prep.pl:22-26 defines them by 3 U.S.C. 3 and 2 U.S.C. 2a(b): the Representatives under the latest census at least two years older, plus two (DC three). COMPLETED.md §786 says "the electors in force derived from the apportionment, not typed". They are fixed by the census years before the vote, so using them passes no information from the vote. The count row says exactly "the electors in force from the catalog" (:26), the same wording as ElectoralCollege.cs:54, UsPresidentialReturns.cs:51 and president_returns.md:8. "In force" means at the election being counted. The class summary also says so (UsStateSwingCheck.cs:18-19): "ElectoralCollege.FromCatalog's shape, the predicted winners in place of the record's". FromCatalog(year) takes that year's state.Electors (ElectoralCollege.cs:76-78).

3. The "district list" has no effect. president_by_district.csv lists ME 1-2 and NE 1-3 in every year from 2012 to 2024, and :153 holds dPrev.Length == dNext.Length.

4. The ruling requires this choice. The Done-when asks "whether 2020's states give 2024's 312-226". I measured from president_by_state.csv: on 2020's electors even the record's own 2024 winners give 311-227, and on 2024's they give 312-226. So only the predicted year's electors can answer the question. The independent prototype counts the same way (us4_proto.pl:54, `$S{$n}{$s}{electors}`).
   - The electors are identical in 2012, 2016 and 2020 (no state changed), so only 2020 → 2024 is touched at all.

5. No reader is led to a wrong figure. The generated table prints the record beside each method on the same electors (card :43-45, "312 – 226 | 312 – 226"). Anyone who reproduced the count on 2020's electors would get 311-227 and see at once that it differs from the record printed in the same row.

The suggested rewording ("no VOTE of the predicted election", "the predicted election's electors in force") is harmless if the author wants it spelled out, but nothing in the card or the comment is false.*
- [claims] (c)'s margins come from shares that are not renormalised, so 'in points of the state's vote' fails where its floor binds - *The finding's arithmetic is right, but its conclusion is not. I recomputed it from the catalog with read-only perl (scratchpad c_floor.pl). On 2016->2020 the floor binds only on the pooled others, and only in AL, FL, GA, LA, MS and NJ (D and R are never floored). The sums run 100.30-101.94 %, and the renormalised misses are -0.18/-4.51/+3.00/-1.40/-1.37/-0.41, each within 0.03 of (a). Why the card is not wrong:
(1) The card declares it. Card line 24: "(c) | each party's national change added, floored at zero, not renormalised | Poland's okreg form". The code says the same at UsStateSwingCheck.cs:23-24 and :247 ("floored at zero, not renormalised - Poland's okreg form"), and :251 does exactly that: `s[i] = Math.Max(0.0, prior[i] + natN[i] - natP[i]);`. Line 148, `misses[m][r] = 100.0 * ((predicted[1] - predicted[0]) - (actual[1] - actual[0]));`, reads the method's own shares, the only shares a method's margin can be read from.
(2) It is the ruled form. R-US14's (c) is Poland's okreg form, and PolishSejmAllocation.cs:73, `Math.Max(0.0, (double)votes[d][c] / size + (national - national2023[c]))`, never renormalises either. The independent prototype (us4_proto.pl:48) reads (iii) on the same unrenormalised shares.
(3) "In points of the state's vote" holds literally. Each of D' and R' is the state's previous share plus a national change in points, so each is a fraction of the state's vote. That vote is what the state cast in the previous election, declared at card line 21. The over-fill above 100 is the declared "not renormalised", not a change of unit.
(4) The margin follows uniform swing exactly. With only the others floored, D' = D + dD and R' = R + dR, so (c)'s margin is the previous margin plus the national margin change (MS: 17.83 - 2.37 = 15.47). Plain additive swing is defined to do exactly that.
(5) The fix would remove (c) from those rows. Dividing by D'+R'+O' is (a)'s ApplySwing renormalisation (RegionalVoteModel.cs:345-347) without (a)'s re-solve, which is why it lands within 0.03 of (a). So the (a)/(c) gap in those rows (MS -1.39 vs -1.08) is the declared difference between card lines 22 and 24: a real difference in how each method spreads the national vote, which the card exists to show, not an effect of scale.
(6) Nothing ruled on moves. Winners, the nine pins, the states-called-right counts and the recommendation do not depend on scale (the finding concedes this). The 2016->2020 mean miss for (c) would move by about 0.006 pp.*
- [claims] The rule's 'states' are read as the 51 jurisdictions (DC included, ME/NE districts excluded), and that reading is not stated - *The finding's arithmetic is right, but its conclusion is not. UsStateSwingCheck.cs:117 sets p to UsPresidentialReturns.States for the year. The catalog calls those rows `States` (UsPresidentialReturns.cs:53) and holds 51 of them each year, DC included (lines 114/165/216). The five ME/NE rows are in a separate `Districts` array (line 263). Line 188 adds `p.Length - wrongBy[m].Count`, so the card's 99 is 2x51-3, and (b)'s NE-2 miss (card line 41, "districts called wrong" column) is not counted.

The verdict does not depend on that reading. I counted the card's own ✗ marks on 2016->2020 and 2020->2024 (read-only perl over docs/reference/US_ELECTIONS.md):
- 51 rows, no districts: 99/99/99
- 50 states, DC left out: 97/97/97. Every method calls DC right in both pairs: its record is -86.75 and -83.81 and no miss is marked.
- 51 rows plus districts: 109/108/109
- 50 states plus districts: 107/106/107

In all four, (a) has the top count and shares it with at least one other method. So line 220's text "- a tie, which the rule gives to (a)" (card line 47) is true under every reading. The card never says "three-way". The only thing that depends on the reading is the count 99, and the code emits it (GENERATED), so nothing is transcribed. The card's verdict does not rest on the reading, as the finding implies.

The reading is also visible on the card. The readings table (card lines 35-45) has "states called wrong" and "districts called wrong" as separate columns, and the recommendation line uses the same word "States" as that column. The tables for each pair list DC as a "jurisdiction" row. The ruling splits them the same way: US-4 "derives every jurisdiction and the five ME/NE districts" and asks for "every state's miss, every state called wrong" (USA_STAGE_PLAN.md:197). So in the ruling, "state" already means the 51 catalog rows. This is the ruling's own word applied to the catalog's own rows. It is not a modelling premise of the kind the plan line's "Read as built" lists (the previous-election weights, the two-party district swing).

No reader reaches a different recommendation from it, and the table R-US14 is asked with already shows (b)'s NE-2 miss. Acting on the finding would cost a Unity WriteReadings run plus a bar run and would change no verdict.*
- [claims] The §787 pointers lead nowhere until the record is written in the same commit - *The facts in the finding are correct. There are six citations of §787: UsStateSwingCheck.cs:16, :50 and :75, CheckSuite.cs:276, US_ELECTIONS.md:6, and USA_STAGE_PLAN.md:197 ("**Built: `COMPLETED.md` §787**"). COMPLETED.md is unchanged in both the index and the working tree, so `git status --short COMPLETED.md` prints nothing. Its last heading is "## 786." at line 37466.

What it describes is the state every change is in at this point in the workflow, not a defect. Five things show this:
1. The task itself says "COMPLETED.md s787, the record not yet written".
2. The rule at CLAUDE.md:67 is "RECORDS: ONE COMMIT PER PASS ... a § record is what changed · the evidence line · the commit". Every recent pass commit adds its record next to its code: 947921ab (COMPLETED.md +31), 5fe3c031 (+12), 7d71fcc4 (+26), b183a332 (+22).
3. The record is written after the staged review on purpose, so at review time its pointer always leads nowhere. §786's record contains "**The review** (`Reviews/2026-10-05_s786_us3_presidential_returns.md`: the workflow `polisim-staged-review` ...)" and "**Bars** (... on this commit's own tree)". Commit 947921ab shipped its own §786 pointers in the same commit as the record: GeneratedCatalogCheck.cs:125 and :517, SeatAllocationBacktest.cs:37, ElectoralCollege.cs:13, and USA_STAGE_PLAN.md:188.
4. The record is already drafted. The scratchpad file rec787.md is headed "## 787. PS-6 US-4: THE STATE-SWING PROOF ...". It ends with the placeholders "REVIEW787" and "BARS787", which wait for this review and the bar.
5. Two earlier skeptics refuted this same finding. Reviews/2026-10-05_s783_us2_record_by_date.md:964 says "§783 is not missing by mistake ... §783 lands with this commit, after this review". Reviews/2026-10-04_s772_e1_factors.md:1304 and :1314 drop the dangling-pointer half: "forward pointers to the next § have precedent ... The pointer resolves at §773's commit."

One thing remains: only the process guards this. There are no active hooks in .git/hooks. No check in Assets/Editor matches pointers to COMPLETED.md sections (DocumentClaimCheck.cs:84 and ResidueCheck.cs:27 only exempt the file). Tools/bar_tier.ps1 does not require COMPLETED.md to be staged. A commit made without the record would therefore be a break from the one-commit-per-pass rule. Nothing in the staged change causes it, and nothing can fail at runtime: these are comments, a log header and document text.*
- [claims] R-US14 is quoted faithfully, but the card does not say what 'one rule across countries' would change - *The facts behind the finding are correct, but its conclusion does not follow. Nothing shows a path where the ruling is made with a hidden consequence.

1. What each country runs today, checked:
- NationalElection.cs:317 (Sweden) and :335 (Germany) call RegionalVoteModel.RegionalSharesByUniformSwing. Both are readouts only ("a readout of the national result, never an input to it", :332-333).
- NationalElection.cs:395 (Poland) calls PolishSejmAllocation.Allocate. Its line 73, `Math.Max(0.0, (double)votes[d][c] / size + (national - national2023[c]))`, adds the national change, floors at zero and never renormalises. That is (c).

2. The card already gives that mapping next to the table:
- US_ELECTIONS.md:10-11: "(a) §689's normalised uniform swing ... (c) plain additive swing (Poland's okręg form)".
- :22: the (a) row is "`RegionalVoteModel.RegionalSharesByUniformSwing`, unchanged ... | §689".
- :24: the (c) row is "| Poland's okręg form (`PolishSejmAllocation`)".
- The plan's line 99 names (a) as "Germany's regional machinery: §689's normalised uniform swing".
- So a reader sees that Poland runs (c) beside the (a) recommendation. Elias ruled §689 himself, and the plan carrying these labels was ruled with R-US1..R-US13 at F8 (R-US14 itself is asked now, with this table).

3. The consequence the finding assumes is not in the rule or in any code:
- R-US14 is marked "(US-4's table; blocks US-8)" (USA_STAGE_PLAN.md:586). Its only consumer is US-8 (:234: "the states by R-US14 ... → the electors by `ElectoralCollege`").
- A grep finds R-US14 only in USA_STAGE_PLAN.md and the card. No spec routes it into PolishSejmAllocation.
- "(a) on a tie, for one rule across countries" reads naturally as the reason for the tie-break. §689 made (a) "the one function both countries' breakdowns come through - generic, so any country wired later inherits it" (COMPLETED.md:35172).
- Changing Poland's seat count would move Poland's own backtest baseline (its sentinel family), so under CLAUDE.md:109 (ruling-first) it would need its own ruling. Nothing changes silently if Elias rules (a).

4. The question itself goes to Elias in the §787 record, which is not written yet. So the card alone cannot show that he would rule without this in view.

5. The proposed fix would add a sentence on "what a single rule would change". That reads the owner's own unruled phrase as cross-country, and the card should not make that claim.

One small gap remains: the (a) row's basis cell says only "§689" and does not name Sweden's and Germany's readouts. That is a wording choice, not a defect.*

## What was done about it

No defect survived the skeptics (5 minor, 13 notes; several pairs report one thing - 2 and 15, 6 and 14, 9 and 16, 10 and 18, 3 and 12); every finding was acted on.

- **The districts across the change of lines (2, 15).** DECLARED in the instrument's doc and comment, the card's districts row and the plan's Built line: a district's previous row stands in for the next election's district of the same number, whatever its lines; between 2020 and 2024 both states' districts changed make-up - verified on the saved pages (Maine's workbooks list eleven towns under another district; Nebraska's books five counties under another district and one split), with no date typed that the pages do not carry.
- **What each method gives back (1, 8).** The card says (a) alone is re-solved to the national shares, (b) is not (proportional swing lacks §689's property), and (c) sums above them where its floor binds; the readings now carry each method's implied national margin beside the true one, so a mean miss is read with the national shift it may contain.
- **The pooled others (7).** A DECLARED "parties" row: the two nominees and all others pooled per state, each a party with one swing.
- **The mean (9, 16).** The column says what it is - the mean absolute margin miss, jurisdictions unweighted, districts not counted - and the card says so in words.
- **The record (10, 18).** The column reads "record (as appointed)", and the card says the record is each slate's electors with its votes for other persons counted to it.
- **The rule (3, 4, 12).** The ruled pairs are one constant that both the sum and the label read, each checked present; a tie that leaves (a) out is not settled by index order - it fails the check, to be asked.
- **The pins (5).** One check per pin, naming the pair, the method and both values; a measured pair without a pin fails.
- **The messages (13).** "A check above failed", not "a pin moved".
- **(a) and (c) (17).** The readings carry, per pair, the largest margin difference between (a) and (c), the calls they make differently, and where each floors a party; the card explains why they call alike unless a floor binds, so the count of states called right is in effect (a) and (c) against (b).
- **The plan's wording (11).** "Each consecutive pair", not "every pair".
- **The documents bar (6, 14).** `UsStateSwingCheck` joins the documents table, so a commit touching only the card still runs the check that guards it; the two texts that counted the tier (`CLAUDE.md`, `Tools/textcheck/Runner.cs`) now name rather than count. `PresidentialVoteBacktest`'s card keeps the same gap (it builds a world; not this item's).

Re-proved after the fixes: the card regenerated, the check clean, a pin and a card figure broken together failing by name (`n787c`), restored; the documents bar 9 of 9 (`docs787`).
