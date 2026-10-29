# Review: wave/dit (D-IT, Italy's election night) - 2026-10-29

Independent adversarial review of `git diff 86596a07..wave/dit` (worktree `G:/UNITY/Projects/PoliSim-wt/dit`, HEAD 24f045c7, four commits). Read-only: no code edited, Unity not run. Compile gate `Tools/cgate/gate.sh` on the worktree: **GATE OK**.

Spec read: `PoliSim-captures/design/RETURN_2026-10-29_DIT/RETURN.transcribed.md`, the board `D-IT Italy Night.dc.html`, and `night.json` (sha256 3b5d3c8fc81a4a985ba19f99dea0868863154e1fb6ee45cca27b31efd6ede6c8; it matches `ItalianNightCartogram.SourceDigest`).

Files reviewed: Assets/Editor/ItalianNightDiagnostic.cs, Assets/Editor/ItalianNightDiagnostic.cs.meta, Assets/Editor/PartyInkDrawSiteCheck.cs, Assets/Scripts/Elections/ElectionNightFromModel.cs, Assets/Scripts/Elections/Generated/ItalianNightCartogram.cs, Assets/Scripts/Elections/Generated/ItalianNightCartogram.cs.meta, Assets/Scripts/Elections/ItalianCount.cs, Assets/Scripts/Elections/ItalianNight.cs, Assets/Scripts/Elections/ItalianNight.cs.meta, Assets/Scripts/Testing/UiScreenshotDriver.cs, Assets/Scripts/UI/ElectionNightScreen.Governs.cs, Assets/Scripts/UI/ElectionNightScreen.Italy.cs, Assets/Scripts/UI/ElectionNightScreen.Italy.cs.meta, Assets/Scripts/UI/GameController.ItalianNight.cs, Assets/Scripts/UI/GameController.ItalianNight.cs.meta, Assets/Scripts/UI/GameController.cs, Assets/Scripts/UI/ItalianCellView.cs, Assets/Scripts/UI/ItalianCellView.cs.meta, Assets/Scripts/UI/PoliSimTheme.cs, Tools/dit_night_prep.pl

## High

**H1. `ItalianNightDiagnostic` is not registered in `CheckSuite`, so no bar ever runs it.** `Assets/Editor/CheckSuite.cs` lists `PolishNightDiagnostic` and `UsNightDiagnostic` (lines 217-218), and the branch adds no row for Italy. Nothing else in `Assets/Editor` or `Tools/` calls `ItalianNightDiagnostic.Run`. That makes every one of its assertions (a)-(h) inert: the table against the catalog, the order in each block, inks by stage, the lines, the call bound, F1, the 14 px floor and the inks. The code says otherwise. `GameController.ItalianNight.cs:101-103` says F1 is "`ItalianNightDiagnostic` holds it". `Tools/dit_night_prep.pl:13` says it "holds the generated table to the game's count on every run". The generated header repeats that claim. *Failure scenario:* a later change to `ItalianCount` (a new Estero rule, a lett. h change) makes the night's seats differ from the chamber's. Every bar stays green. The only signal is a runtime `Debug.LogError` on a live Italian polling day.

## Medium

**M1. The call's "certain seats" bound is not a lower bound. Near the close it can exceed the seats the leader ends with, and so call a chamber wrongly. The check that claims otherwise never samples the counts where this happens.** `ItalianNight.Certain` (`ItalianNight.cs:323-341`) re-runs `ItalianCount.Count` with every undeclared college's whole vote given to the rival's largest list. That shrinks every third list's national share. A list just over a line therefore falls under it at the bound, and its seats are redistributed:
- a list alone just above 3 %;
- a coalition list just above 1 % (lett. c strikes its votes from the coalition);
- SVP's minority route, whose TAA college wins go to the rival when those colleges are undeclared.

The leader takes part of those seats. The class doc admits this "in principle" (lines 26-28). But `ItalianNightDiagnostic` (e) states as held that "the leader's certain seats never exceed the seats it ends with in Italy". `BoundHolds` (`ItalianNightDiagnostic.cs:249-251`) steps by `Colleges / 10`. For the Camera it tests 1, 15, ..., 141 and never 142-146. For the Senato it tests 1, 8, ..., 71 and never 72-73. Those last counts are exactly where one or two undeclared colleges can push a near-threshold list under its line. The six-vote sweep does not aim at near-threshold lists. *Failure scenario:* a list alone (say Az-IV) sits at 3.02 % nationally, and the last Camera college to declare holds about 1 % of its vote. At 146 of 147 its bound share falls below 3 %, so art. 83 admits it nowhere and its roughly 8 list seats are shared out. The leading coalition gains 3-4 of them and loses about 1.7 by giving that one college to the rival, so certain = final + about 2. When the leader's Italy seats plus its abroad seats fall short of 201 but its certain seats reach 201, the Camera is stamped "called" for a coalition that ends without a majority. Under `n.CalledAt` "a call is stamped where it lands and stands", the stamp is never withdrawn.

**M2. In the Called state the lane always draws ✓ and "for at least N", but N is the *current* leader's bound, so it can sit below the line. The entity the call was stamped for is never read.** `ElectionNightScreen.Italy.cs:999-1006` hard-codes `glyph = "✓"` and prints `c.LeaderCertain`. `ItalianNight.Build` recomputes `LeaderCertain` at each index for whoever leads by counted votes then. The bound is not monotone in declared colleges: the rival can change identity, and the threshold exclusions from M1 can switch on and off. A chamber called at index i can therefore show, at a later index, a ✓ beside a figure below the line, or beside a different entity than the one called. `ChamberNight.CalledFor` (`ItalianNight.cs:85`, written at line 273) is never read anywhere. *Failure scenario:* the Camera is called at 127 for CDX. At 135 the rival changes from CSX to M5S, and CDX's bound falls to 199. The lane reads "✓ 199 for at least · 201".

## Low

**L1. The doc comment moved to the wrong method.** `GameController.cs:6597-6600`: `StandingBudgetDivision` was inserted between `ShowElectionNight`'s `<summary>` block and the method itself. The long election-night summary now attaches to `StandingBudgetDivision`, which carries two `<summary>` elements, and `ShowElectionNight` has no doc.

**L2. The generated file is not UTF-8.** `ItalianNightCartogram.cs` lines 444 and 878 carry a raw Latin-1 byte 0xFC in "Trentino-Alto Adige/Südtirol". `dit_night_prep.pl:25` decodes the JSON as UTF-8 into character strings, and `:186` prints them through `>:raw`, so characters below 256 go out as single bytes (the perl encode trap). Roslyn falls back to a platform-dependent codepage on invalid UTF-8, and `SourceText`-based checks read U+FFFD. The data is dead today: `Chamber.BlockName` has no reader (see L4). Fix: `binmode` with `:encoding(UTF-8)` before the CRLF rewrite, or drop `BlockName`.

**L3. The night's minority-route test differs from the count's.** `ItalianNight.MinorityRouteMet` (`ItalianNight.cs:185-198`) tests both routes without the count's precondition. `ItalianCount.MinorityQualifies:367` skips any circoscrizione where the minority list has no list votes. *Failure scenario:* SVP's share is 0 but its linked group wins TAA colleges. The Camera row prints "TAA ≥ 20 ✓" and "met", while the count gives SVP nothing.

**L4. Dead state, and comments the code does not back.**
- `ChamberNight.CalledFor`: never read.
- `ChamberNight.CountedValid`: written, never read for Italy.
- `ItalianNightCartogram.Chamber.BlockName`: no reader.
- `ElectionNightScreen.Italy.cs:46-49` says "the check reads their sizes" of `ItalyTexts`, and lines 38-39 make the same claim for `ItalyCameraMap`. The diagnostic reads neither: it parses source. Only the film's `LogItalianNight` reads them, and it logs the smallest size without asserting it against 21 units.

**L5. Slip text states facts the code does not compute.**
- `ElectionNightScreen.Italy.cs:419`: the Other row reads "no list clears 3 %; none wins a college". That is a constant, true of 2022's record only.
- `:434`: the Senato SVP row reads "with no list vote counted there". But `SwungVotes` gives SVP Senato votes wherever the 2018 Senato base holds them, so on any non-record vote this can be false.

**L6. Weaknesses in the diagnostic beyond H1.**
- (a) The digest assertion resolves `../PoliSim-captures` against the project root. In a worktree that is `PoliSim-wt/PoliSim-captures`, which does not exist, so the check downgrades to a "note" and asserts nothing. It only asserts from the main checkout.
- (d) "No literal line" is a regex for `201|104|206`; an expression like `200 + 1` evades it.
- (e) "The call stands" re-runs the identical `Certain` call that set the call, which is tautological. "At the close the certain seats are the count's own" is true by construction of `Certain`'s `declared >= Colleges` branch.
- (g) The size-plus-constant regex only matches identifiers named `size`/`fontSize`/`Size`, so `Px(14f) + 2` would pass. No instance exists today.
- On the good side: `Plan()`'s reflection throws if `MapPlan` is renamed, so it fails loudly rather than passing silently. The loops are not vacuous: `calls > 40` holds, and the cell counts are pinned.

**L7. The frame rects are asserted nowhere, and my arithmetic shows them close but not exact.**
- Horizontal: the shell's content is 1700.8 canvas units (1133.9 board px). The columns need 318 + 20 + 447 + 20 + 330 = 1135. `minWidth = board - 2` absorbs the gap, so the map lands at about x410.6 and Who governs at about x877.4, against 411 and 878. That is within a pixel.
- Vertical: the shared shell's document spans 0.04-0.96 of the canvas height, which is 662 board px at a 720 px window against the board's 643. Who governs at y94 and Continue at y617 therefore depend on the real film. That film is owed under the Canvas-film rule at 1280 and 2560, and this review could not run it.

**L8. Ink side effects.**
- `Italy/PD` #75383B is byte-identical to `USA/REP` #75383B. That is cross-country, so `CountryInkFenceCheck` (country inks only) and `InkContrastCheck` (no party inks) are unaffected. `PartyInkHarness`'s "two parties, one ink" will list the pair. That harness is not in the bar, and it already fails every non-Swedish ink through `PublishedOf`.
- Adding the ten hues to `PartyHues` makes every Italian surface that calls `Party` / `PartyLaddered` draw Design's PROPOSED inks game-wide: the hemicycle, polls and sitting rows, not just the night. The comment's "adopted for the Italian night - K-line drafted" is not backed by any K-line in this branch.
- The diagnostic's (h) confirms that the nudge leaves all ten unmoved.

**L9. Layout literals are unmarked.** The box positions (`4f`, `404f`, `344f`, `ElectionNightScreen.Italy.cs:722-731`) and the dot sizes (`3f : 5f`, line 648) are board-sourced layout constants with no `[AUTHORED]` mark. Only a prose comment ("34a's own placement") covers them.

**L10. The dissolution dates are computed twice.** `ItalianNightInputs` (`GameController.ItalianNight.cs:56-58`) recomputes the dissolution dates instead of reading the projection's. It calls the same two functions as `SimulationManager.Dissolution`, so on the live night the results agree (13 Oct + 70 days gives 22 Dec, so polling falls on Sunday 18 Dec 2022, matching 34b). But the term's first sitting comes from different places: this count's, against `LastItalianCount`. They can drift apart if the night is ever built from a record that is not the latest.

**L11. A to-come college's dot sits one board pixel up-left of centre** (`ItalianCellView.cs:127`, `- 1f`). The inked dot is centred, so the dot jumps when its cell inks.

## Checked and clean

- **`ItalianCount.cs` is additive and preserves behaviour.** `Estero` keeps its integer `seatsOut` updates unchanged and only adds an out dictionary. `SenatorsForLifeOn` now aggregates `SenatorsForLifeSeatedOn` in the same order, with the same keys and the same insertion order. `Result.EsteroByRipartizione` is never persisted (`ElectionRecord` does not hold a `Result`).
- **The re-run is pure.** `Count`, `Colleges` and `AssignCollegeSeats` have no mutable statics, caches or RNG; sorts carry full tie-breaks; the bound passes `estero = null`. It cannot move the game's count or a simulation baseline.
- **The night counts on the game's own inputs.** It uses `record.Shares`, the same `RecordFactors.TryFor` roster rule, the Senato's 2022 roster share, and `ItalyFirstSittingAfter(pollingDay)` with polling day equal to `CurrentDate`. So at the close its Camera seats equal `NationalElection.Run` and its Senato equals `SenatoSeated`, and the runtime F1 compare logs any difference.
- **The cell readings are correct.** List seats per block come from `ByCircoscrizione` (overflow and empties detectable). Abroad seats come from `EsteroByRipartizione`, with unkeyed seats in the Neutral. Life senators come from the record as of the first sitting. The lines come out at 201/400 and 104/206.
- **No regressions to other nights.** `Available(Italy)` is read only by `ShowElectionNight`, which routes Italy to `ShowItalianNight`, and `At()` still returns null for Italy. `ApplyItalyViews()` returns at once when `_italyViews` is null, which covers Poland, Germany, the USA and Sweden.
- **The dissolution dates are right.** Art. 61 via `ConfidenceProcedure.ItalianElectionDay` and art. 88 c. 2 via `WorldClock.ItalyDissolutionBarred` are the manager's own functions.
- **The film restores the game and stays silent.** The planted hostile Senato is restored in a `finally` on every path, exceptions included, before the frame. `ProjectFormation` / `ItalianFormation.Run` write nothing. `AudioListener.volume = 0f` is set at the film's start, so the night's `AudioDirector.Fire` stays silent.
- **The generator's assertions are real and die on failure.** It checks colleges against the catalog by name and order, U order, each block within one circoscrizione and complete, magnitude against Tabella A, cell indices unique and in range, positions unique, 392/196, and ranks as a permutation. It does not validate labels' block indices or the segs.
- **Line endings and sizes are fine.** Every committed blob, including the generated file, is CRLF like the repo; the worktree's LF copies are a checkout artifact. Every Italian text size is `Px(N)` with N ≥ 14, and fixed cells and lane marks are sized from `Measure`.

## Verdict

NOT READY. H1 has to be fixed (register the diagnostic in `CheckSuite` and run it in the bar). M1 needs either a bound that really is a lower bound or a diagnostic that samples every declared count and holds the stated property honestly. M2 needs the Called lane to read `CalledFor` and stop hard-coding ✓. The real 1280/2560 film the Canvas rule owes still has to confirm the vertical rects.
