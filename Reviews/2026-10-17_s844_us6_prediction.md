# Review - §844, US-6: the US prediction and E1's factors (2026-10-17)

One adversarial reviewer, refute-first, read-only in the worktree `PoliSim-wt/us6` (branch `sweep/us6`, 8a0fb45c). US-6 touches no money path, so the review was not required; it was run anyway. Its report is verbatim below. The implementer met every finding in 0cf192d5; the disposition follows.

## The report

VERDICT: Ready to integrate. No high-severity defect: no non-US path moves, and the sourcing, quotes and merge are clean. Four medium findings: a player-visible compass caption that is false for the USA, a wrong elec<N> expectation, done-when verifications missing from the integrator's list, and one plan departure presented as a reading.

DEFECTS (ranked)

1. MEDIUM. The compass caption is false for the USA. File: Assets/Scripts/UI/PoliticalCompassRenderer.cs:126 (not edited by the branch; US-6 makes it reachable).
   - Scenario: `PartySystems.TryElectorate(USA)` now succeeds, so in a US game the Politics compass draws the electorate point with the text "… over 2 parties, the fitted electorate over the cohorts". The US electorate is held, not fitted; US-6's own card says "The electorate is held, not fitted".
   - Fix: word the caption by basis (held for the USA, for example keyed on `PartySystems.HistoryBasis(id) != null`), then look at it on the USA dry film.

2. MEDIUM. The elec<N> diff will move more than "exactly one USA header line". Files: Assets/Editor/LoyaltyHarness.cs:112-131 and UsNationalVoteCheck.cs:397.
   - `ElectionsBacktests.Set` also runs `LoyaltyHarness`. Its NOT COMPUTABLE block changes from 4 lines to 5: three re-worded FRANCE lines plus two USA lines ("House 2022 against 2020 - loyalty REP 94.5, DEM 94.1").
   - So a non-US (France) line does move in text. §788's "Owed to US-6" foresaw this.
   - Fix: expect GateReRun's US header line plus the LoyaltyHarness block, and record the France lines as a text-only rewording.

3. MEDIUM. The integrator's list omits verifications the done-when requires.
   - The commit touches Scripts/Data, Scripts/Elections and ElectionsData, so the simulation bar is owed, with the sentinel dump diff (`TrajectorySentinelCheck` / `Tools/traj_diff.pl`, ga111).
   - The USA dry film is owed, with USA@1280x720 declared in Tools/film_scope.tsv first. It covers the compass point and caption, and party creation's electorate cross and predicted share, which now render for the USA.
   - The documents bar is owed after registration.
   - The patch step:
     - Run it on the rebased tree (the card stamp hashes positions and digests).
     - Run it in a fresh Editor: a warm host does not recompile after the patch, so `WriteReadings` would still see NaN and refuse.

4. MEDIUM (provenance). National ballot standing is a departure from the plan's text, not a reading of it. Files: PresidencyOfRecord.cs:118 and US_ELECTIONS.md:575.
   - The plan says "standing only where the FEC shows them on the ballot". The branch declares national standing instead.
   - It changes no number in US-6, because the prediction is national. It defers the per-state table to US-8.
   - Fix: record it as a departure put to Elias, as §788 did for the House basis, and name US-8 as where the per-state table lands.

5. LOW. The Republican naming. Files: PresidencyOfRecord.cs:184 and records_by_date.md:290.
   - Code: `NamedFrom = DateTime.MaxValue`, so "the Republican nominee" is shown on every day, including after the count.
   - G13 says "on every day before the count". The document is wrong against the code.
   - The asymmetry (Harris named from 6 Aug; Trump never named, even for a REP player) is an Elias-visible consequence of the BILLED date. The plan's §6 row lists "NARA's certificates naming the candidates" as the stand-in.
   - Fix: correct G13's wording and report the asymmetry to Elias. Nothing on screen reads `NomineeName` until US-8.

6. LOW. A fourth reading is not in the branch's declared list. File: PartySystem.cs:686-698.
   - The plan says "`PartySystems.HistoryNote` states the basis". The branch adds `HistoryBasis` instead, because every `HistoryNote` reader draws "LOW CONFIDENCE" (verified at ElectionNightScreen.cs:1141 and ParliamentRows.cs:422).
   - The reasoning is sound, but no screen shows the basis; only the card does.
   - Fix: declare it as a reading in the record.

7. LOW. Two documents claim bar coverage that is true only after registration.
   - PRESIDENTIAL_VOTE.md:5 says PresidentialVoteBacktest fails "the documents bar".
   - US_ELECTIONS.md:8 says UsPresidentialFactorCheck fails "the cheap bar and the documents bar".
   - Both must land in the same commit as the `CheckSuite` registrations (the cheap table near line 277 and the documents table near 745).

8. LOW. A statement that can go stale. File: LoyaltyHarness.cs:118-119.
   - It hard-codes `ElectionVintage.Usa2022` but prints "on the start's chamber". If the US start or the chamber table moves, the line becomes false while the harness passes.
   - Fix: use `WorldClock.SeatedVintage(USA, WorldClock.StartDate(USA))`.

9. LOW. Two checks in UsPresidentialFactorCheck cannot really fail.
   - Line 155: `Same(live, electorates[atStart])` compares `UsElectorate`'s cache with itself, and `atStart` comes from the same `Resolve` call `TryElectorate` makes. "The start seats Usa2022" is never asserted against the record.
   - Line 209: `historyKept` holds by construction, because `TryHistory(Seated)` resolves at the process epoch that `Hold`'s EpochScope pins. It can fail only if the steps throw.
   - The statements printed are true; the checks just prove little.

10. LOW. Tools/us_factor_patch.pl does not check where its lines came from.
    - It takes the last FIT line per party from any log, including another worktree's run if the shared Editor.log is used.
    - Only an exact `--dry` suppresses the write; any other second argument writes.
    - The behaviour is safe: a missing party, a partial log or a NaN line all die (tested with `--dry` on synthetic logs). The mandatory rerun catches wrong figures.

11. LOW. Static state left behind. The new check calls `TryPredictShares(USA)` twice (inside `Hold` and at line 192). That leaves `NationalElection.LastRegionalKeys` set to REP and DEM and `LastRegionalShares` null for the next check in the batch. Poland's check already does the same; PartyCreationFlow and EntrantLayerDiagnostic re-run Sweden's prediction afterwards to restore it.

12. INFO. GameController.cs:6545, a note for US-8. `TryPredictShares(USA)` now succeeds, so once US-8 adds a polling day the log will read "ELECTION: held for USA … by NotImplemented" instead of "not held". It is unreachable today because the USA has no polling day.

Runtime trace (question 1): every new runtime branch is USA-only.
- `TryRealHistory`, `TryElectorate`, `UsElectorate` and the `PresidencyOfRecord` USA branches.
- The only runtime callers are UI surfaces (the compass, party creation) and `HoldPresidentialRound`, which is gated by `TwoRoundElection.RuleOf`. `GameController.RunNationalElection` is gated by `PollingDayToday`.
- `SeatTheRecordOnItsDate` needs a player country, so the dump (no player) and the shadow world never reach it.
- `PoliticalParty` is a readonly struct, so the `UsSolved` cache cannot go stale.
- The Poland path through `PresidentialReferenceWorld.Hold` runs the same statements in the same order (`RecordOverTerm` was always evaluated before `TryPredictShares`). The extra reads (`Seats` copy, `Kind`, `Day`) have no side effects.
- No float intermediate changed in any non-US expression.
- No money path is touched, so no review is required.

RE-DERIVED
- SHA-256 of all 7 new raw pages: recomputed, all match SHA256SUMS. `sha256sum -c` on the whole file: 45 of 45 OK.
- Byte sizes match fetch_log and the register. Blob bytes equal working-tree bytes (`**/raw/** -text`).
- Every quote in records_by_date §2a matches the saved pages verbatim (my perl, with tags stripped, entities decoded and whitespace collapsed). The raw 6 Aug page contains "August 6, 2024" and "officially accepted the nominations today", as check (8) needs.
- 24 Jul 2024 was a Wednesday, so "this past Sunday" is 21 Jul. The 1–5 Aug roll-call window is confirmed on the 30 Jul page.
- From the catalog:
  - 2024 FEC total: 155,238,302; Trump 49.7961 %, Harris 48.3242 %.
  - House two-party R split: 2020 0.484437, 2022 0.513968, 2024 0.513174.
- Median σ 1.25 and τ 6.0.
- For House 2022: place 0.5096, μ (6.023, 5.411), loyalty 94.5 / 94.1. All match US-5's card and the LoyaltyHarness output.
- The two-party identity holds: preference equals T-1's split.
- With the eve shift of DEM +2.50 pp, the factors come out at Trump 1.01839 and Harris 0.94562. Expect the first FIT lines to show these (US-5's card: 1.018390 / 0.945620).
- Compile gate (scratchpad cgate) passes on the US-6 worktree and on the merged tree.
- textcheck rebuilt in the scratchpad against the worktree:
  - DocumentClaimCheck, PreWiringPremiseCheck, DesignNotificationCheck and ResidueCheck pass.
  - UpstreamCheck fails only because the branch has no upstream (environmental).
  - CommentClaimCheck out of engine shows the same 5 shim artefacts as main and nothing new.
- I read every format string in UsPresidentialFactorCheck: placeholder indices match their arguments.

REBASE
- `git merge-tree --write-tree origin/main sweep/us6` returns tree 4ab85adf with no conflicts.
- PartySystem.cs (US-32, c3bccf9d) merges cleanly: main's edits are the `using`, the doc comments and the `UsaParties` initialisers; US-6's are around lines 627-800.
- US-32 leaves the positions unchanged (`UsGps2019` V4/V6 = 8.23, 8.30, 3.73, 2.41); only people-versus-elite moved. So US-6's electorate, factors and card stamp are unaffected.
- Main's `CheckSuite` (+2 lines) and the plan and record (§836-§839) are mechanical; US-6's section number will be §840 or later.
- Semantic note for US-13: §839's preregistered poll is "fitted to the 2024 House split (T-1), the loyalty 2024 against 2022" on a world with epoch 4 Nov 2024 stepped to 3 Nov 2026. US-6's live `TryElectorate`/`TryHistory` on that world read House 2022 against 2020, because they are pinned to the epoch. US-13 must call `UsElectorate(Usa2024)` and `TryHistory(USA, …, Usa2024)` explicitly.

NOT CHECKED
- Nothing in Unity, by instruction. Not run: the four named checks, the patch → WriteReadings → rerun flow, the Poland byte check, the elec<N> diff, the bars, the sentinel dump diff and the dry film. UsPresidentialFactorCheck has never executed (its card block is a placeholder; I traced its logic by reading).
- That the 60-day step to 3 Jan 2025 runs without an error log. `SeatTheRecordOnItsDate` runs after the date increment, so the 119th should be seated on 3 Jan.
- Batch-order static leaks from that 60-day step, such as EnergyMarket calibration being reset only before the hold, as in US-5's pattern.
- That the reference world's economic shift equals US-5's eve world's. US-6 sets `PlayerCountryId` to the USA and US-5 does not; I expect the same shift but nothing holds the two cards' factors equal.
- No live re-fetch of the pages or the gop.com refusal; I relied on the saved bytes and fetch_log. The House catalog figures are US-5's and were not re-read against the Clerk's PDFs.
- The cost of the check in the cheap and documents bars.

Scratch files (no repo writes; worktree clean at 8a0fb45c) are in G:/Claude/tmp/claude/C--Users-elias/36780c75-eec4-4fba-9c32-e50d358cf646/scratchpad/review_us6/: quotes.pl, the rederive/ project, the textcheck/ copy, and merged/, an extract of the merge tree.

## Disposition (0cf192d5, integrated in §844)

- **M1:** the compass caption is worded by basis: the USA reads "held, not fitted, on the House vote", and every other country's caption is byte-identical. The surface is IMGUI, so it is seen on the USA dry film and the real film.
- **M2:** the `elec<N>` expectation includes LoyaltyHarness's block. The diff moved exactly the foreseen lines, France's text-only.
- **M3:** the integration ran all of it: the simulation bar, the sentinel, Poland's byte check, the dry film, the documents bar and a real film.
- **M4:** national ballot standing is recorded as a DEPARTURE put to Elias. `Tools/us_ballot_standing.pl` measures it, and the per-state table lands in US-8.
- **L5:** G13's wording matches the code: "the Republican nominee" on every day. The naming asymmetry is put to Elias.
- **L6:** `HistoryBasis` is declared as a reading.
- **L8:** LoyaltyHarness reads the start's chamber.
- **L9:** the start's vintage is checked against `WorldClock.ChamberAt`. `TryElectorate` is compared with `UsNationalVoteCheck.HeldAtMedian`, and the later-day check runs the game's own poll.
- **L10:** the patch tool requires the log, the named-run header and a complete block.
- **L11:** the regional statics are captured and restored.
- **INFO 12:** a note for US-8.
