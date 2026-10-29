# Independent review: wave/names - display names for enums and party keys, K285's Italian short names, EnumDisplayNameCheck (2026-10-29)

**What was reviewed.** Branch `wave/names` in the worktree `G:/UNITY/Projects/PoliSim-wt/names`, HEAD `a7015dda`, as `git diff cf6bd007..wave/names` (five commits: `68110868` M4 enum names, `61fdb618` K285, `24f48a40` M4 portfolios in the check, `38bbdbb8` M4 follow-up party keys, `a7015dda` the welfare column). The background is §946's filed M4 (`Reviews/2026-10-29_portfolioname_review.md`). Read-only on the repo (this file is the only write); Unity was not run.

Beyond reading, the branch was compiled with `Tools/cgate/make.pl` into the session scratchpad and a net10.0 console harness loaded the built `Assembly-CSharp.dll` / `Assembly-CSharp-Editor.dll` and called `EnumDisplayNameCheck.Evaluate` (and its internal `Scan`, `ScanKeys`, `CodeView` by reflection) OUT OF THE ENGINE. `MetaTextCheck`, `CommentImmunityCheck` and `PhantomGuardCheck` were run the same way from scratch copies of the tree in which only their engine calls (`CheckExit`, `Debug.Log`, `Application.dataPath`) were replaced by console writes. A sixth build carried six deliberate mutations to prove the check's teeth.

Files reviewed: Assets/Editor/EnumDisplayNameCheck.cs, Assets/Editor/EnumDisplayNameCheck.cs.meta, Assets/Editor/GermanFormationDiagnostic.cs, Assets/Editor/TaxTermDiagnostic.cs, Assets/Scripts/Data/DivisionRecord.cs, Assets/Scripts/Data/EnglishText.cs, Assets/Scripts/Data/EnumNames.cs, Assets/Scripts/Data/EnumNames.cs.meta, Assets/Scripts/Data/PartySystem.cs, Assets/Scripts/Elections/FormationProposal.cs, Assets/Scripts/Elections/GovernmentFormation.cs, Assets/Scripts/Elections/WorldClock.cs, Assets/Scripts/Simulation/SimulationManager.FormationProjection.cs, Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs, Assets/Scripts/Simulation/SimulationManager.cs, Assets/Scripts/Simulation/StanceModel.cs, Assets/Scripts/UI/DisplayName.cs, Assets/Scripts/UI/ElectionNightScreen.Governs.cs, Assets/Scripts/UI/GameController.BudgetV35.cs, Assets/Scripts/UI/GameController.LawsV35.cs, Assets/Scripts/UI/GameController.PolicyWebV35.cs, Assets/Scripts/UI/GameController.PresidentialStart.cs, Assets/Scripts/UI/GameController.cs, Assets/Scripts/UI/PartyCreationFlow.cs, Assets/Scripts/UI/PolicyWebRenderer.cs, Assets/Scripts/UI/PoliticalCompassRenderer.cs, Assets/Scripts/UI/SigningScreen.cs, Assets/Scripts/UI/UiPalette.cs

States read (SHA-256 of the bytes with carriage returns removed, first 16 hex - the review ledger's convention):

| file | state |
|---|---|
| `Assets/Scripts/Simulation/SimulationManager.cs` (money path), at HEAD | `fc5af90f607895bd` |
| `Assets/Scripts/Simulation/SimulationManager.cs` (money path), as `68110868` left it (an intermediate commit) | `cc7a54ed3e185e60` |
| `Assets/Scripts/Simulation/SimulationManager.FormationProjection.cs` (money path) | `f42de7cbb9078112` |
| `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs` (money path) | `67d1a3f527a8824e` |
| `Assets/Editor/EnumDisplayNameCheck.cs` (new) | `ddc763fc4e5bc808` |
| `Assets/Scripts/Data/EnumNames.cs` (new) | `544bbcd287eddb10` |

**Compile gate:** `bash Tools/cgate/gate.sh G:/UNITY/Projects/PoliSim-wt/names` printed `GATE OK`.

## What was verified clean

### 1. Behaviour beyond text: none found

- **The programme-bill title is byte-identical.** `EnumNames.ProgrammeBillKey` (`EnumNames.cs:107`, `:110`) is `(isAdd ? "Implement " : "Remove ") + x.ToString()`; the old title was `$"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type}"`, and an enum in a hole with no format string formats through the same `Enum.ToString()`, so the two are equal for every member, defined or not. Only two sites title a programme bill (every `Divide(` caller was listed): `SimulationManager.cs:2562` (tax) and `:2636` (welfare); both use the key. The check's part (3) rebuilds the old interpolation for all 19 members and both verbs every run (38 of 38).
- **The draws it keys.** `PresidentialVeto.Draw(..., passage.Title)` (`SimulationManager.cs:1752`; `PresidentialVeto.Applies` is Poland only, `PresidentialVeto.cs:45`) and `UsVeto.Draw` (`Congress.cs:736`, `:740`; USA only, `UsVeto.cs:30`; a budget vote is refused). `UsLapseDraw.Draw` (`Congress.cs:686`) is reached only through `DrawWithholders`, which runs where the concern carries `WithheldBy` - set only for a continuing resolution (`ParliamentSystem.cs:1171`) - so no programme bill ever meets it; the class note's list (`EnumNames.cs:21`) over-names one draw (L4).
- **Other title identities.** `SamePassage` (`SimulationManager.cs:1978-1979`, FORK-2's docket identity) compares titles built by the same code in both worlds. No runtime code finds a division by its title (grep of `Entries.Find/Exists/...` with `Title`: none). `IsStatuteActTitle` and `PolishDeskRecord`'s Art. 158 `EndsWith` read titles this branch does not build.
- **`DescribeBudgetBill` is also a title, and stays safe.** It now prints "Income Tax at 25.0 %" (`:2034`) and feeds the US appropriation's division title through `UsActTitle` (`:2439`), a key. But the appropriation's part is `bill.SpendingOnly(country)` (`UsBudgetProcedure.cs:63`) -> `Without(Rates)` -> `WithoutRateChanges` (`BudgetBill.cs:150-156`), which empties `TaxLines`, so the changed clause never reaches a title. Its other readers are logs and the desk's sentence.
- **Breaks.** The two parsers - `PolishDeskRecord.TryLeftOn` (`PolishDeskRecord.cs:76-90`, exact against `LeftTheGovernmentLine`) and `WorldCalendar.FormationFailure` (`WorldCalendar.cs:166-177`, the text between " - " and the last "; ", read for the Riksdag only) - read lines whose writers the branch did not touch. The AI failure line (`SimulationManager.WorldCalendar.cs:174`) can now carry a German projection reason with "GRÜNE", but `FormationFailure` reads that line only under Riksdag rules, and every Swedish key is its own short name.
- **The round's log.** Its only runtime reader is the Parliament page's slip (`GameController.ParliamentRows.cs:247-249`, `DeskDated` on the date prefix, unchanged). `GermanFormationDiagnostic`'s log assertions ("GO-BT § 4 Abs. 2", "Linke", "sits in", "the game's premise") still hold; its two title assertions moved to "(GRÜNE)" (`:178`, `:189`) because `CandidateOf` now prints the short name. No other diagnostic or harness asserts "Grune", "(NewElection)", "IncomeTax at" or "Implement NegativeIncomeTax" as text (grep over `Assets/Editor`, `Assets/Scripts/Testing`, `Tools`); `UsVetoDiagnostic`'s planted "Implement IncomeTax" (`:131`) and its `(add ? "Implement " : "Remove ") + tax.Type` (`:561`) are draw keys, and stay equal to the play's.
- **`.Reason` readers.** `Formed.Reason` is read once, as prose (`GameController.cs:6539`; the doubled sentence is gone). `View.Reason` reaches the night's slip and `ItalianFormation`'s `Failure` text, where only `Failure`'s null-ness is a flag (`ItalianFormation.cs:31`). `ProposalVerdict.Reason` reaches the sheet and the round's log. The one reason parser at runtime, `PolishDeskRecord.MoverOf`'s `StartsWith("moved it")`, reads Art. 158 sides this branch does not write.
- **`StanceModel.ReasonShort` after the reasons changed.** Its "cuts " branch cuts at the first " by " and adds the arrow's tail. None of the 17 names `LineBands` and `ProgramBands` can produce (Social Security, Federal Retirement, Medicare, Medicaid, HHS (Healthcare), Income Security, Labor, Education, Housing (Spending), Justice (Spending), Defense, UBI, Negative Income Tax, Means-Tested Welfare, Universal Healthcare, Housing Assistance, Childcare Subsidies) contains " by " or the arrow, so "cuts Social Security -0.00" parses as "cuts SocialSecurity" did. `line` feeds reasons only.
- **Approval-ledger labels.** `RecordEvent` stores the label and nothing else (`ApprovalLedgerRecorder.cs:58-71`); no code reads `Label` for logic. Identifier -> words is one-to-one, so no two events can merge.
- **Numbers, draws, digests, saves.** No `RandomSource` call, float expression or statement order moves: every changed statement builds a string. `TrajectorySentinelCheck`'s digests and the trajectory dump hold no text; the campaign digests (`CampaignRun.cs:1507-1511`, `ElectionDay.cs`) hash keys, regions and figures, never a short name; `AiVoteKey` reads keys. The save format is unchanged (still 69, correctly): new saves carry the new words in text fields (titles, sides, logs, ledger labels, `DivisionSide.ShortName`), old saves keep the old words, and no load path parses them.

### 2. The moved lookups keep their names

- **The 33 Policy Web names are identical.** cf6bd007's `PolicyWebRenderer` literals compared mechanically with `EnumNames`' `case ... return "..."` lines: 33 the same, 0 different; and every node reads its own same-named member (33 of 33).
- **`EnglishText.Spaced`** is `DisplayName.Split`'s code verbatim. `DisplayName.Of` and `DisplayName.Spaced` were evaluated for every member of every enum in the runtime assembly (832 identifiers) on cf6bd007's build and on the branch's: identical output, line for line.
- **`UiPalette.AreaName`** is the old private `PolicyWebAreaName` (three cases and `ToString`). **`OutcomeShort`** is unchanged for the three members it can draw; NewElection and Collapse are reached only where `HasGovernment` is false, where it is never called, and `CoalitionOutcomeKind.Collapse` is produced nowhere.
- **R4-4.** Every new lookup is typed - an overload per enum, `Effectiveness.DisplayName` for portfolios, `AreaName` for areas. The one name-keyed parse is `ShownTitle`'s `ProgrammeName` (TaxType first, then WelfareProgramType): the two enums share no member name today, and part (3) walks both, so a future shared name with different words would fail the check. No `DisplayName.Of` call was added on an identifier outside policy space. The check's simple-name `TypeMap` has one collision (`View`: `GovernmentFormation.View` and `UsCongressView.View`), which can only add a false alarm, and it says so in its limits.

### 3. EnumDisplayNameCheck

- **On the branch, out of the engine: CLEAN in about 2 s** - teeth 11 of 11 caught and 8 of 8 passed; 89 members of 6 enums read as words; 65 members agree, 33 nodes checked; 38 keys; 397 runtime files, 0 raw writes; party keys 15 of 15 typed by reflection, teeth 9 of 9 and 6 of 6, 0 raw keys.
- **On cf6bd007's `Assets/Scripts`: 12 raw writes and 32 raw keys** - the sites the branch fixed (the commit's 11 plus the portfolio, `FormationProposal.cs:127`).
- **Every part fails while its statement is false** (one mutation build, six plants): `ProgrammeBillKey` returning words -> 24 key faults ("every draw keyed on it would move"; VAT and UBI pass, their key is their name); the Policy Web's IncomeTax literal re-typed "Income tax" -> 2 one-set faults; UiPalette's CrimeJustice case deleted -> a names fault; `KeyMembers`' "Asked" renamed -> the reflection fault plus 2 party-key teeth missed; the desk's pending line reverted to `{bill.Type}` -> 2 scan faults; a round-log line reverted to `{country.PlayerPartyAbbrev}` -> 1 party-key fault.
- **The lexer fix is correct.** `CodeStep` reaches a `//` only outside a literal (each literal is consumed whole by its own branch first), and blanks to the line's end. Probed with `//` inside plain, interpolated, verbatim and escaped strings, a URL, `'/'`, `'\''`, `'"'` inside a hole, `$@"{x:hh\:mm}//"` and a division operator: no code blanked, every trailing comment blanked. What reaches it is `SourceText.WithoutComments`' output, which keeps a `//` only on a line holding a quote, so a `//` outside a literal there is a comment. A residual it shares with every MustStrip reader: the stripper's block-comment regex runs through literals; no runtime literal holds `/*` today (the two grep hits are real block comments; the one `*/` inside a literal is in `Testing/`, which this check skips).
- **Ready for MustStrip.** It reads only through `SourceText.ReadWithoutComments`, which matches the census's routing regex, and sits in top-level `Assets/Editor`. Proven: with `"EnumDisplayNameCheck"` added to MustStrip in a scratch copy, `CommentImmunityCheck` printed "ok EnumDisplayNameCheck ... 0 UNENROLLED" and exit 0.
- **The party-key reflection is safe on a rename.** A renamed member leaves `GetField`/`GetProperty` null and the check FAILS by name (proven above); a renamed owner type is a compile error; a field turned property, or a list turned array, still types.

### 4. K285: nothing keys on a short name

Every `ShortName` use was read: none compares, sorts, hashes or keys on it. Keys drive the save (`PlayerPartyAbbrev`), seats, inks, marks (`mark_party_it_aziv`, `mark_party_it_pluse`), slip anchors (`"compass:party:" + Abbrev`, `"party/" + Abbrev`), GameObject names (`Party_`, `Seg_`, `Lbl_` + Abbrev), `ItalianCount`'s '+'-split list keys (`Generated/ItalianCount2022.cs` holds `PlusE`, `AzIV`), and the created party's key collision test (`PartyCreationFlow.KeyFrom` tests keys). The Italian night measures its code column on what it draws (`ElectionNightScreen.Italy.cs:337`, `:370`). No code treats a leading '+' specially. `DivisionSide.ShortName` is saved as text, and the corrected comment (`DivisionRecord.cs:174-178`) says so; a division drawn before K285 keeps "AzIV", by design.

### 5. "GRÜNE" in titles

No German or Swedish division title is a key: the title-keyed draws apply to Poland and the USA only (above), and no lookup reads a German title. No Swedish text changed: only Grune, AzIV, PlusE and Misto declare a `shortName`, and every Swedish key is its own short name.

### 6. CRLF, encoding, MetaTextCheck

All 26 changed `.cs` files are CRLF throughout in the working tree (CR count = line count), `i/lf w/crlf` in the index, valid UTF-8, no BOM, no mojibake byte sequences. The two new `.meta` files are LF with no final newline - cosmetic (they normalise to the same blob); each must be committed with its `.cs`, as they are. `MetaTextCheck` out of the engine: cf6bd007 194 files / 12,384 literals, 0 hits; the branch 195 files / 12,396 literals, 0 hits; its possessive pass 0 found, teeth 11 of 11 and 0 of 9. `PhantomGuardCheck`: exit 0 (every guard a new comment names exists).

### 7. The money paths (`SimulationManager*.cs`), hunk by hunk - all text-only

- `SimulationManager.cs`: `:889` the yearly-range refusal's line name (`EnumNames.Of(SpendingCategory)`, upper-cased; read as text, `FundTermsDiagnostic` matches only its second half); `:2034` `DescribeBudgetBill` (above); `:2562`, `:2636` the programme-bill titles (byte-identical keys); `:2572`, `:2644` the ledger labels; `:4902-4903` the order line; `:4932-4943` the new private `Short` / `Shorts` (pure lookups); `:4947-4948` `CandidateOf`; `:4994-4995`, `:5008`, `:5012`, `:5052`, `:5077-5078`, `:5278`, `:5324`, `:5360`, `:5691-5692`, `:5812`, `:5840`, `:5845`, `:5852` round-log lines and the plurality tally (log only); `:4999` / `:5019` `Table` takes the country (one caller, updated).
- `SimulationManager.FormationProjection.cs`: `:161`, `:165` the two German first-ballot reasons (`Short`).
- `SimulationManager.PlayerElection.cs`: `:61` a round-log line.
- The intermediate state of `SimulationManager.cs` (`68110868`, `cc7a54ed3e185e60`) is the first six hunks above and nothing else (read in isolation).

## Findings

### MEDIUM

**M1. The election night's outcome slip still prints raw party keys - on Italy's night too, which K285 says it covers.** `ElectionNightScreen.Governs.cs:1178-1183` `Parties(list)` writes `abbrev + " " + seats` from `GovernmentFormation.View.Cabinet` / `.Support`, which hold KEYS (`GovernmentFormation.cs:371-372`; `SimulationManager.FormationProjection.cs:485-486`). It is called at `ElectionNightScreen.Governs.cs:364-365` ("IN CABINET  ...", "SUPPORT FROM OUTSIDE  ...") and at `ElectionNightScreen.Italy.cs:912-913` (the outcome slip and its dense lines). Scenario: an Italian count where the centre-left forms reads "Support from outside · PlusE 2" or "... · AzIV 21" on Italy's night; a German cabinet with the Greens reads "IN CABINET  CDU 164 · CSU 44 · Grune 85". The sibling `ShortParties(c, list)` (`:1185-1190`) already does it right, and the slip's own neighbours (`:376`, `:381`) use it. Pre-existing (D-NIGHT, `8718776a`), not caused here, but squarely in what `61fdb618` ("the Italian night") and `38bbdbb8` ("the night's government line print short names") claim, and outside the check's sinks. Fix: `ShortParties(n.Country, ...)` at `Governs.cs:364-365`, `ShortParties(CountryId.Italy, ...)` at `Italy.cs:912-913`, then delete `Parties` (no other caller) so it cannot come back. Text-only; the night's films are owed anyway.

**M2. Integration owed in the same merge, or the cheap bar goes red and the guard never runs.** The commit leaves the registration and the MustStrip line as drafts (the brief keeps `CheckSuite.cs` with the integrator). As the branch stands: (a) `CommentImmunityCheck` FAILS - run out of the engine on the branch it printed "EnumDisplayNameCheck reads source and is enrolled in neither list ... 1 UNENROLLED", exit 1; add `"EnumDisplayNameCheck",` after `"PortfolioDisplayNameCheck",` (`CommentImmunityCheck.cs:77`) with a one-line reason (proven green above). (b) The check is registered nowhere, so no bar runs it: add `("EnumDisplayNameCheck", EnumDisplayNameCheck.Run),` to the cheap `Suite` beside `PortfolioDisplayNameCheck` (`CheckSuite.cs:402`); it builds no World. (c) `ReviewLedgerCheck` rows are owed for the three money paths at the states above, with this file as evidence - and, if the branch is merged with its history rather than squashed, also for `SimulationManager.cs` at `cc7a54ed3e185e60` (pass 2 audits every commit's blob). None of the money-path edits is comment-only, so §584's exemption does not apply. `Tools/bar_tier.ps1` will also name `TaxTermDiagnostic` as a money hit outside the ledger's rows; its change is a label built the way the game now builds it, read by nothing.

### LOW

**L1. Two more raw-key reasons a player reads, outside the check's reach.** (a) `StanceModel.cs:350` `party.Abbrev + " tabled the bill: its author votes for it"` - `concern.Author` is the player's key for a non-government bill (`ParliamentSystem.cs:1110`), the line becomes `DivisionSide.Reason` through `ReasonLine` (`ParliamentSystem.cs:1367`, `Congress.cs:500`) and is drawn at `SigningScreen.cs:671`, `:746` and `GameController.cs:8773`. Scenario: playing Az-IV, a member's bill's sides read "AZIV TABLED THE BILL"; playing the Greens, "GRUNE TABLED THE BILL". (b) `FormationProposal.cs:258-263` `Describe` joins `Abbrev`s into `where`, which a refusing partner's `PartyAnswer.Reason` carries (`:195-197`, `:213`) to the formation sheet's answer slip (`GameController.FormationSheet.cs:310`), the Riksdag's rejection line (`SimulationManager.cs:5077-5078`) and the projection's refusals (`SimulationManager.FormationProjection.cs:230`): "... in CDU+CSU+Grune" on a German sheet. Both are §575's class, pre-existing; `PartySystems.ShortName` / `FormationProjection.Join` are the one-token fixes.

**L2. The check cannot guard three of the sites the branch fixed.** `KeyKind` (`EnumDisplayNameCheck.cs:386-395`) types no `foreach` variable over a key list and no `KeyValuePair<string, ...>.Key`, so `FormationProposal.cs:121`, `:122` and `:126` - fixed here - would pass if reverted (probe: the reverted forms `"...: " + key` and `"...: " + kv.Key` under a `ProposalVerdict` sink scan 0 hits; the base run did not flag them either). Its printed limits say so ("a foreach variable over keys ... not typed"). Typing `foreach (string x in <key list>)` as a key, and a `Dictionary<string, ...>` key whose dictionary is a known key holder, would close the case it was built for.

**L3. "+E" joined with "+" reads ambiguously.** `FormationProjection.Join` (`FormationProjection.cs:55-60`), the election verdict's cabinet description (`GovernmentFormation.cs:109-110`), the compass legend and WorldClock's government line join short names with "+", so an Italian centre-left reads "PD+AVS++E+IC", and the verdict says "Expected to form: a PD+AVS++E+IC government" (`GameController.ItalianVerdict.cs:44`). The night itself joins with " · " and reads cleanly. Worth a line to Elias (K285 ruled the name, not the join); a " + " join for Italy, or the night's " · ", would remove the doubled sign.

**L4. Comments that are now inaccurate.** `PartySystem.cs:63-69`: `Abbrev` is "FOLDED TO ASCII where the published one is not", and `ShortName` "defaults to the key, which is right for the fifty-two units whose authority publishes an ASCII abbreviation" - Az-IV and +E are ASCII and still differ from their keys (a key must also be identifier-safe), and the count moved. `DisplayName.cs:16-21`: the curated names "already existed in PolicyWebRenderer's node metadata" - they live in `EnumNames` now and the table reads them. `EnumNames.cs:21`: `UsLapseDraw.Draw` is listed among the draws a programme bill's title keys; it is a continuing resolution's draw only.

## Not found

No behaviour change beyond text: no draw key, identity, save field, digest, RNG call, numeric path or exact-match parser moves. The moved names are identical (33 Policy Web names; 832 `DisplayName` outputs; the area names; the three drawable outcome names). The check is sound within its printed limits, fails each statement it makes when that statement is made false, its lexer fix blanks no code, and it is ready for MustStrip. K285 keys nothing on a printed name. CRLF, encoding and MetaTextCheck are clean.

## Owed runs (the integrator's)

The cheap bar after M2 (CommentImmunityCheck, EnumDisplayNameCheck, PortfolioDisplayNameCheck, MetaTextCheck, PhantomGuardCheck, ReviewLedgerCheck); `GermanFormationDiagnostic` and `TaxTermDiagnostic` (both changed); the trajectory dump's six CSVs SHA-identical and `TrajectorySentinelCheck` green (expected: nothing numeric changed); a REAL film for the Canvas text (the Italian selector, Italy's night after M1, the German night, the signing screen's title) and the dry film for the IMGUI text (the desk's pending list and calendar, the Budget and Laws count slips, the compass legend, the Parliament page's round slip, the welfare page).

## Verdict

**NOT READY** - for one branch-side fix and the merge's own steps, nothing deeper. The change is text-only and safe (keys byte-identical, no save, digest, draw or numeric effect, names preserved, gate OK) and the check is good. Fix M1 (two call sites, one helper deleted), then take M2's three steps in the merge (MustStrip line, CheckSuite registration, ledger rows on the final states and, unsquashed, on `cc7a54ed3e185e60`); with those it is READY. L1-L4 can follow as their own items.

## Round 2 (2026-10-29): the answer on the branch, `a7015dda..d01408a3`

**What was reviewed.** One commit, `d01408a3` ("names review (NOT READY) fixed on the branch"), 13 files, the worktree at that HEAD and clean. Read-only on the repo; Unity was not run. The compile gate printed `GATE OK`. The harness was rebuilt at `d01408a3` and run as in round 1: `EnumDisplayNameCheck.Evaluate` on this tree, on `a7015dda`'s and on cf6bd007's `Assets/Scripts`; a mutation build with four plants; probes of `PartySystems.JoinShortNames` and `StanceModel.ReasonShort` by reflection; and `MetaTextCheck`, `CommentImmunityCheck` (with the check enrolled in a scratch copy only) and `PhantomGuardCheck` from a scratch copy with their engine calls replaced.

Files reviewed (round 2): Assets/Editor/EnumDisplayNameCheck.cs, Assets/Scripts/Data/EnumNames.cs, Assets/Scripts/Data/PartySystem.cs, Assets/Scripts/Elections/FormationProjection.cs, Assets/Scripts/Elections/FormationProposal.cs, Assets/Scripts/Elections/GovernmentFormation.cs, Assets/Scripts/Elections/StartBrief.cs, Assets/Scripts/Simulation/SimulationManager.cs, Assets/Scripts/Simulation/StanceModel.cs, Assets/Scripts/UI/DisplayName.cs, Assets/Scripts/UI/ElectionNightScreen.Governs.cs, Assets/Scripts/UI/ElectionNightScreen.Italy.cs, Assets/Scripts/UI/GameController.ParliamentRows.cs

States read (SHA-256 with carriage returns removed, first 16 hex):

| file | state |
|---|---|
| `Assets/Scripts/Simulation/SimulationManager.cs` (money path), at `d01408a3` | `b65ff65e29f1da03` |
| `Assets/Scripts/Simulation/SimulationManager.FormationProjection.cs` (money path), unchanged since round 1 | `f42de7cbb9078112` |
| `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs` (money path), unchanged since round 1 | `67d1a3f527a8824e` |
| `Assets/Editor/EnumDisplayNameCheck.cs` | `51a83721e6cc11f5` |

### Round 1's findings

- **M1 - FIXED.** The night's four sites print `ShortParties`: `ElectionNightScreen.Governs.cs:364-365` with `n.Country`, `ElectionNightScreen.Italy.cs:912-913` with `CountryId.Italy`. `Parties` is deleted, and no caller remains (grep).
- **M2 - STILL OWED, the integrator's, unchanged.** The delta touches neither `CommentImmunityCheck.cs` nor `CheckSuite.cs`. As the branch stands, `CommentImmunityCheck` still fails on the unenrolled check; enrolled in a scratch copy it prints "ok EnumDisplayNameCheck ... 0 UNENROLLED", exit 0. The registration line is still owed. The ledger rows are now owed for `SimulationManager.cs` at `b65ff65e29f1da03` (it moved), `SimulationManager.FormationProjection.cs` at `f42de7cbb9078112` and `SimulationManager.PlayerElection.cs` at `67d1a3f527a8824e`. If the branch is merged with its history, rows are also owed for `SimulationManager.cs`'s intermediate states `cc7a54ed3e185e60` (`68110868`) and `fc5af90f607895bd` (`38bbdbb8`). This file is the evidence for all of them.
- **L1 - FIXED.** `StanceModel.cs:350` begins the author's reason with `party.ShortName`. `FormationProposal.cs:258-263` `Describe` collects `ShortName` and joins through `JoinShortNames`, so a refusal reads "... in CDU+CSU+GRÜNE". (L1's StanceModel fix opens M3 below.)
- **L2 - FIXED, and it bites.** `ForeachKey` and `ForeachEntry` type a `foreach` variable over a key list as a key, and an entry over a party-keyed dictionary (`FormationProposal.Posts`, `Country.ParliamentSeats`, both proved dictionaries keyed by strings by reflection) as an entry whose `Key` is a key. On cf6bd007's sources the check now flags `FormationProposal.cs:121`, `:122` (`key`) and `:126` (`kv.Key`), the three sites round 1 found it blind to. In the mutation build, `:121` reverted to `+ key` was caught.
- **L3 - FIXED by K312.** `PartySystems.JoinShortNames` (`PartySystem.cs:326-333`) joins with the separator handed to it ("+" by default), or with " · " when any name holds a '+'. It is used by `FormationProjection.Join`, the verdict's cabinet description, `Describe`, the round's log, the investiture title, the Parliament page's round slips, the constructive-vote row's names and the start brief's two rows. The remaining hand "+" joins in the runtime are keys in logs, breaks, the AI vote memo's key, Italy's mandate signature and `StartBrief`'s clauses. No player reads those: `StartBrief.Text`/`Clauses` have no runtime caller, only `StartBriefDiagnostic`. `CoalitionScreenSnapshot` is built only by the film harness.
- **L4 - FIXED.** `PartySystem.cs:63-72` (Abbrev "made an ASCII identifier ... a sign spelled out, AzIV for Az-IV and PlusE for +E"; ShortName no longer counts units), `DisplayName.cs:21-23` (the names live in `EnumNames`), `EnumNames.cs:20-22` (`UsLapseDraw` is a continuing resolution's draw only). All three are now accurate against the code.

### Does anything new change behaviour?

- **The investiture title (`SimulationManager.cs:5072`) is byte-identical for every real Swedish party.** The investiture path is the Riksdag's alone: `RulesOf` is `Riksdag` for Sweden only (`ConfidenceProcedure.cs:45`), and a Bundestag round returns to `PersonBallot` one line above. No Swedish party declares a `shortName`, so `Short(country, k) == k`. No Swedish name holds a '+', so `JoinShortNames` joins with "+" exactly as `string.Join("+", ...)` did, and `Possessive` sees the same string. The title changes only for a party the player created whose typed short name differs from its derived key (`PartyCreationFlow.KeyFrom`: upper-case ASCII). That is intended, and the title is no key for Sweden: Sweden has no title-keyed draw, `SamePassage` is the Polish and US docket's, and `SpeakerRoundDiagnostic`'s `StartsWith("Investiture")` holds.
- **`TryLeftOn`:** its writer, `LeftTheGovernmentLine` (key), is untouched. **`FormationFailure`:** it reads the AI failure line between " - " and the last "; ". `JoinShortNames`' output contains neither, Swedish lists keep "+", and a created party never stands in an AI country's formation. So the parse is unchanged.
- **`ReasonShort`:** for every real short name the author's reason is still dropped (probe: "Az-IV", "+E", "GRÜNE", "M" -> ""). But a created party's typed short name is now matched against its prefix chain - see M3.
- **The rest of the delta is text** (round slips, start-brief rows, refusal reasons, the night's slips). The money path's five hunks (`SimulationManager.cs:5008`, `:5012`, `:5052`, `:5072`, `:5691-5692`) are round-log lines and the Swedish investiture title, all text. No RNG, digest, save field or numeric path moves.

### The check's new teeth bite

- **On `d01408a3`: CLEAN in about 2.2 s.** The output reads: 17 of 17 key members typed; K312's join 5 of 5; teeth 12 of 12 caught and 9 of 9 passed; 0 raw keys or hand joins.
- **On `a7015dda`'s sources: 15 faults, all hand joins.** These are exactly the sites the delta converted: `StartBrief.cs:320`, `:325`; `SimulationManager.cs:5008`, `:5012`, `:5052`, `:5691`, `:5692`; `GameController.ParliamentRows.cs:300`, `:301`, `:319`, `:346`. On cf6bd007's sources: 12 raw writes and 42 raw keys or hand joins.
- **Mutation build: all four plants caught.**
  - `FormationProposal.cs:121` reverted to `+ key` -> 1 party-key fault.
  - `GameController.ParliamentRows.cs:300` reverted to `string.Join("+", ...ConvertAll(Name))` -> 1 hand-join fault.
  - `JoinShortNames`' '+' test disabled -> 3 K312 faults ("PD+AVS++E+IC"; "Az-IV + +E"; Italy's centre-left by its keys).
  - `KeyMembers`' `Posts` renamed -> the reflection fault "is not a dictionary keyed by strings", plus the entry plant's teeth missed.
- **The hand-join scan's reach is narrow and says so.** It flags only a literal "+" or " + " separator whose list visibly is short names (`Shorts(`, `ShortName`, `ConvertAll(Name)`), outside a diagnostic call.

### `JoinShortNames` at the edges (probed)

| input | separator | result |
|---|---|---|
| empty list | "+" | "" |
| single "M" | "+" | "M" |
| single "+E" | "+" | "+E" |
| "+E" first: "+E", "PD", "AVS" | "+" | "+E · PD · AVS" |
| "PD", "+E" | "+" | "PD · +E" |
| "M", "KD", "L" | "+" | "M+KD+L" (unchanged) |
| "S", "MP" | " + " | "S + MP" (unchanged) |
| "PD", "+E" | " + " | "PD · +E" |
| "CDU", "CSU", "GRÜNE" | "+" | "CDU+CSU+GRÜNE" |
| "PD", null, "IC" | "+" | "PD++IC" |
| a null list | | `ArgumentNullException`, as `string.Join` threw |

The null element gives the same output `string.Join` gave. No caller hands one: `PartySystems.ShortName` returns null only for a null key, and every list reaching the join is a cabinet or support list of keys or `ShortName`s.

### MetaTextCheck, PhantomGuardCheck, CRLF

- `MetaTextCheck` out of the engine at `d01408a3`: 195 files, 12,390 literals, 0 hits; possessive pass 0, teeth 11 of 11 and 0 of 9. The new " · " literal matches no banned token.
- `PhantomGuardCheck`: exit 0.
- All 13 files are CRLF throughout (CR count = line count), `i/lf w/crlf`, valid UTF-8, no BOM.

## Round 2 findings

### MEDIUM

**M3. A party the player names "cuts" crashes `ReasonShort`, and so the vote it tables. This is new with L1's fix.** `StanceModel.cs:350` now starts the author's reason with the typed `party.ShortName`, and `ReasonShort`'s prefix chain matches it. The "cuts " branch (`StanceModel.cs:567`) runs `reason.Substring(0, reason.IndexOf(" by ", ...))`, and with no " by " that is `Substring(0, -1)` -> `ArgumentOutOfRangeException` (probe: "cuts" THROWS).

`ReasonShort` runs in the simulation as well as on the page: `ParliamentSystem.RecordDivision` (`ParliamentSystem.cs:1367`, every contested division) and `Congress.cs:500`, and on screen at `GameController.BudgetV35.cs:1421`, `GameController.LawsV35.cs:1346` and `GameController.cs:4636`.

Scenario: the player founds a party with the short name "cuts" (the SHORT field takes any 12 characters, `GameController.PartyCreation.cs:239`, `PcField`). In opposition it tables a budget alternative or a member's bill (`concern.Author` is the player's key, `ParliamentSystem.cs:1110`). The Budget page's count slip throws every frame, and the division throws inside the day tick that resolves the bill.

Lesser misreads come from the same chain: "undecided" -> "undecided: inside the band", "bloc party" -> "bloc line ", "opinion cost" -> "opinion cost below 0.01" on the plate.

Before the delta the line began with the key, and `KeyFrom` makes a created key upper-case ASCII ("CUTS"), which no lower-case ordinal prefix matches. No real party's short name reaches any branch (probed).

Fix (one line each, text-only):
- In `ReasonShort`, skip the author's reason first by its fixed tail (`reason.EndsWith(" tabled the bill: its author votes for it", StringComparison.Ordinal)` -> `continue`, which is what it does for every real name today).
- Guard the "cuts " branch on `IndexOf(" by ") > 0`.

The check does not see this; it is a parser, not a print.

## Round 2 verdict

**NOT READY.** One branch-side line is left (M3, the `ReasonShort` guard) plus M2's integration steps.

Everything round 1 asked of the branch is fixed and verified:
- M1, and L1-L4.
- K312's join, correct at its edges.
- The check's new teeth bite.
- The Swedish investiture title is byte-identical for the real roster.
- No parser of the joined strings moves.

With the M3 guard on the branch, and the MustStrip line, the CheckSuite registration and the ledger rows (`b65ff65e29f1da03` and the two unchanged states; unsquashed, also `cc7a54ed3e185e60` and `fc5af90f607895bd`) taken in the merge, it is READY. The films owed in round 1 stand; the night's slips are now in them.

## Round 3 (2026-10-29): M3's answer, `d01408a3..c2e7c4ad` (Assets/Scripts/Simulation/StanceModel.cs, Assets/Editor/StanceModelDiagnostic.cs)

Read-only on the repo, no Unity; `GATE OK`. Both files are CRLF throughout and valid UTF-8. Two harness probes were run:

- **`ReasonShort` (`StanceModel.cs:541`, built at `d01408a3` and at `c2e7c4ad`)** over 28 reason lists, real and planted.
  - **No short name throws or misreads now.** The planted names were "cuts", "cuts by", "on the", " by ", "x tabled the bill", the empty name, "undecided", "bloc party", "opinion cost", "no published" and "a:b→c". Their author lines are dropped by `AuthorTail`, and their position lines read "spendvtax 2.5 → +0.50" from the last " on the " (a 12-character name cannot hold " axis, the bill toward its "). The two former throws ("cuts tabled the bill ...", "cuts by") now read "", and the old "undecided" misread is gone.
  - **Real parties: unchanged.** Every list gives the same short form before and after the fix. That holds both with the key and with the short name in the position line (S, SD, M, Grune and GRÜNE, AzIV and Az-IV, PlusE and +E): the short form never carries the name, and for a real line the parse finds the same " on the ", the same " axis" and the same colon.
  - **`ChamberVerdictCacheCheck` is untouched.** It compares the cache's reasons with the model's (`ChamberVerdictCacheCheck.cs:506-519`), both written by the same code, so its comparison does not change. The full line now names Grune, AzIV, PlusE and Misto by short name on both sides alike. It needs the engine (a GameObject) and was not run.
- **The new test bites.** `StanceModelDiagnostic` was run out of the engine with only its log and exit calls replaced:
  - on `c2e7c4ad`: "4 of 4 read without a throw ... ALL ASSERTIONS PASS";
  - with the `AuthorTail` skip and the "cuts " guard reverted in a scratch copy: "0 of 4 ... 4 FAILURE(S)", namely "cuts" threw `ArgumentOutOfRangeException`, and "undecided", "bloc party" and "opinion cost" gave a non-empty author short form;
  - the test also pins the position line's short name (`memberLine.StartsWith(planted + " ")`).

`StanceModel.cs` is no money path; no ledger row is owed for it. M2's integration steps from round 2 still stand: the MustStrip line, the CheckSuite registration, and the ledger rows at `b65ff65e29f1da03`, `f42de7cbb9078112` and `67d1a3f527a8824e` (unsquashed, also `cc7a54ed3e185e60` and `fc5af90f607895bd`).

**Round 3 verdict: READY.** M3 is fixed and the new test bites. Merge with M2's three integration steps.
