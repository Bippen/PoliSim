# Review — s753 merged ministries coded as Druckman & Warwick code them, Germany included (Elias's ruling A3) (2026-10-02)

Reviewed: the first form of the change (Germany's BMBF summed to one undated 1.75, the mean 1.33, the family `fp753` byte-identical to `ps3k`) against HEAD a0a4156 - `Assets/Scripts/Elections/PortfolioSalience.cs`, `ElectionsData/portfolios/portfolio_salience.md`, `Assets/Editor/PortfolioSalienceDiagnostic.cs`, `Assets/Editor/FinancePartnerDiagnostic.cs`, `Assets/Editor/TrajectorySentinelCheck.cs`. One independent read-only reader; its report verbatim below, then what the rework did.

## The pass (verbatim)

Reviewed: staged diff vs a0a4156; PortfolioSalience.cs, portfolio_salience.md, PortfolioSalienceDiagnostic.cs, FinancePartnerDiagnostic.cs, FinancePartner.cs (all whole); TrajectorySentinelCheck.cs:95-128; GovernmentRecord.cs:40-319; PartySystem.cs (seat rows); CabinetPortfolio.cs; SimulationManager.cs (2630-50, 2855-85, 3690-3725, 3798-3803); ReviewLedgerCheck.cs (excerpts); Tools/review_ledger.tsv; Tools/bar_tier.ps1 ($money); COMPLETED.md:35851; Reviews/2026-10-01_s717_mef_summed.md:39-42; dw05.txt (415-460, 1425-1535, Italy/Sweden rows); logs n753a, n753b, cheap752, sim753, dump753; sha256 of the dumps.

Verdict: NOT READY

## Defects
1. **PortfolioSalience.cs:44: the BMBF weight is also applied to a cabinet that has no BMBF.** The md itself says (md:79-80) that the 2025 cabinet split the BMBF on 6 May 2025 into the BMFTR and the BMBFSFJ. Druckman & Warwick's coding of that cabinet's education post would not be 1.75. By the paper's own rules it is Science & Education 0.82 (or half of it) plus Families & Youth 0.68, so somewhere from 1.09 to 1.50.
   - **Both German results turn on one comparison.** Heaviest-first allocation puts Finance with the head's party only when Education weighs more than Finance's 1.58. With any Education weight below 1.58, the 2025 chamber gives Finance to the SPD, which is the record.
   - **So the 2025 miss is not the ruling's doing.** It comes from keeping one weight per country, and so does fp753 removing Germany's Finance partner from the 2026 world.
   - **Elias was not told about 2025.** The question put to him (COMPLETED.md:35851) named only the 2021 move.
   - **What it needs:** either date the weight (the BMBF until 2025-05-05), or tell Elias the 2025 and live-world consequence and let him rule on it.
2. **portfolio_salience.md:82-84:** "The ruling's tests" still says the 2025 chamber gives Finance to the SPD and 2021 to the FDP. That contradicts md:77-79 and the diagnostic.
3. **GovernmentRecord.cs is now false in three places:**
   - :139-140 says Finance goes "to the partner in the 2025 and 2021 chambers, as the record has it".
   - :143-144 cites Finance gaps of 0.32 and 1.30 of 1.58, which no longer hold.
   - :55 implies the same.
4. **Poland's other posts moved, and nothing records it.**
   - **Before:** KO Interior/Finance/Defense, TD Health/Education, NL Foreign (cheap752.log:9039).
   - **After:** KO Interior/Finance/Foreign, TD Health/Defense, NL Education (n753b.log:422).
   - **Player-visible:** the §634 gate follows the posts. Neither the md nor the diagnostic's message mentions the change.
   - **Fragile:** NL takes Education from TD by 0.0085, under the near-tie threshold of 0.133.

## The questions asked
(1) **Yes on both counts.** dw05.txt:446-451 is the summing passage, with Ireland's 1.24 + 0.91 = 2.15. Germany's Research & Technology is 0.93 at :1497 and Science & Education/Science 0.82 at :1508. Table 2 (:416) gives Germany a 6.2% merged-post share.
   - **No other merger is in the mapping for the 2021 cabinet.**
   - **Unexamined:** France's Bercy titles from 2022 to 2025 include industry ("Souveraineté industrielle", or "et de l'Industrie" under Armand). Industry is rated 1.06, and the md checked only France's Budget.
   - **The reason given for leaving out Italy's universities ministry (0.69) and France's Research (0.81) is the wrong test.** "Separate ministries" is not md:41's criterion, which sums separate ministries a portfolio stands for. It holds only if the game's Education means "the post that holds education", and the md should say so.

(2) **Yes.** 5.32/4 = 1.33. The other mean rows also check.

(3) **Yes, by hand with the method.**
   - **2021:** total weight 12.28; entitlements SPD 3.961, Grüne 3.483, FDP 2.716.
     - The heaviest posts go first: Interior to the SPD, Health to the Grüne, Education (1.75) to the FDP.
     - Finance then goes to the SPD, at 1.671 against the Grüne's 1.473. The gap of 0.198 is above the 0.158 near-tie.
   - **2025:** CDU 4.02, SPD 4.493, CSU 1.647.
     - Interior to the SPD, Health to the CDU, then Education to the SPD (2.203 against the CDU's 2.010). That gap of 0.193 is just over the 0.175 near-tie threshold.
     - Then Finance to the CDU, Foreign to the CSU, and Defense to the CDU by near-tie.
   - **Matches the log:** both results match n753b.log:414-415.
   - **Close margin:** with NearTieShare at 0.111 or more, 2025's Finance returns to the SPD.

(4) **Poland's Finance stays with KO, but three other posts move** (defect 4). The USA's cabinet is one party, so nothing changes there. A grep of Assets/Editor found nothing else pinned.

(5) **The attribution is right.**
   - **Byte-identical:** fp753, ps3k and ps1wc share the same sha256 at both seeds (dbe9a64a…, 538b8f10…).
   - **Reviewed rows:** the digests have reviewed rows (ledger rows 94-95, s618), and the sentinel passed (sim753.log:452-454).
   - **No partner left:** Holder returns null when Finance sits with the head's party. On the dump's world that is Germany's CDU, Italy's FdI and Poland's KO; France's government is a provisional stand-in.
   - **PortfolioSalience.cs is not a money path** by bar_tier's $money pattern.

(6) **The plant is sound.**
   - **Planted before any reader:** it runs before SetWorld and before anything reads the government.
   - **No re-allocation during its turns:** the manager re-seats only through Install, InstallSuccessor and LeaveCabinet, which are round or player verbs. n753b.log:977-983 shows the FDP still at Finance at the boundary.
   - **Matches the record:** the SPD gets Defense.
   - **One caveat:** it skips §642's Version bump, which is harmless before any cache.

## Notes for the record
- **§716 is no longer exercised at any start.** After §753 no country starts with a Finance partner: at Germany's 2024 start the SPD holds Finance, and in 2026 the CDU does. §716 is reachable only through formation, and only the plant tests it.
- **A caveat goes unquoted.** The paper warns that summed weights "may not be perfectly accurate … one could downgrade" (dw05.txt:451-455). The md doesn't quote it.
- **The simulation tier was still running at review time.** sim753.log ends at LawCompositionDiagnostic, so only the sentinel's result is confirmed.

## The rework (the author's)

1. **Dated weights** (`PortfolioSalience.Weight(country, post, day)`; the record's `FormedOn`, a formation's current date, `GamsonPosts(…, asOf)`): Germany's Education 1.75 (the BMBF) for a cabinet of the 2021 chamber and 1.50 (the BMBFSFJ: 0.82 + Families & Youth 0.68) for one of the 2025 chamber - keyed on that chamber's ELECTION day (2025-02-23), because the split was its government's and a formation drafted the day after polling day and installed weeks later must weigh one structure (keyed on 6 May, the first run had the 2025 formation draft CDU+CSU alone on the BMBF weights). The 2025 chamber's Finance goes to the SPD (the record), its formation forms CDU+CSU+SPD; only 2021's Finance is a miss. The family does NOT move: the dumps are byte-identical to `fp717` (the sentinel reverted; the scratch dumps deleted). The review's question (examined) on France taken: Bercy has carried industry since 2022-05-20 (the stored JORF of 2024-09-06 and 2024-12-14) - 1.92 + 1.06 = 2.98; the mean is now computed from the four at the day (Finance 2.545).
2. The md's tests line, 3. `GovernmentRecord`'s three doc lines - corrected. 4. Poland's moved posts recorded (KO Finance/Interior/Defence, TD Health/Foreign, NL Education), in the md and the record. The paper's caveat on summing quoted. Italy's Education stated as the post that holds education.

(The author first wrote here that no second reader was needed - false: `SimulationManager.cs`, touched by the rework, is a money path by name. The second pass follows.)


## The second pass (verbatim) - on the dated rework; required after all, `SimulationManager.cs` being a money path by name

Reviewed: Reviews/2026-10-02_s753_merged_ministries.md. The staged diff (all 9 files) against a0a4156. PortfolioSalience.cs (whole). GovernmentRecord.cs:30-349. SeatedGovernment.cs:25-80. FormationProposal.cs:100-200. FinancePartner.cs:50-90. FinancePartnerDiagnostic.cs:28-47 and 185-205. SimulationManager.cs: 229-267, 3040-3080, 3220-3260, 3436-3460, 3785-3800, 4790-4800, plus greps for readers. GameController.cs: 2156-2240, 6405-6420, 9620-9640. WorldClock.cs:60-125. TrajectoryBaselineDump.cs:100-102. CabinetPortfolio.cs. CountryId.cs. Tools/bar_tier.ps1:36-40. portfolio_salience.md:56-84. France raw/executive JORF pages (grep). Logs: n753e.log, sim753b.log, dump753b.log header, unity_launched.tsv.

Verdict: NOT READY

## Defects
1. **portfolio_salience.md:72-80 is still the first form's prose.**
   - :78 says the 2025 chamber's Finance goes "to the CDU". The code, the diagnostic and n753e:415 all give the SPD.
   - :80 says "the game's Education keeps one weight per country", but the weights are now dated.
   - :75-77 checks France's posts without mentioning Industry, which :49 and the code sum in.
   - The rework's items 2 and 4 claim the md now records Poland's moved posts, the paper's caveat and Italy's Education. None of it is there (grep finds no KO/TD/NL and no "downgrade"). The Italy line exists only at PortfolioSalience.cs:101.
2. **FinancePartnerDiagnostic.cs:47 no longer tests decision A.**
   - The check dropped `FinanceHolder(france) != PmParty`.
   - With ENS, the head's party, at Finance, `Holder` returns null through the head-party clause anyway.
   - So decision A's guard (FinancePartner.cs:66, `government.Provisional`) has no test that can fail: delete it and the check stays green.
   - Fix: plant a non-head party at France's Finance, as `PlantRecordFinance` does for Germany.
3. **PortfolioSalience.cs:105-106:** the comment says "1.33 before 2025-05-06", but the key is 2025-02-23.
4. **Reviews/…s753…md:70:** "No second reader… neither holds" is false. SimulationManager.cs is a $money path (bar_tier.ps1:37).

## The questions asked
(1) **Yes, every caller passes the right day, and the epoch default is safe.**
   - **FormedOn is set in the initializer, before `AllocatePortfolios`, on every path:**
     - AtStart (installed): `record.AsOf`, which `SeatedGovernment.TryAt` sets to the start date (:62, :73).
     - The stand-in: FromView :293, then AtStart's second allocation on the same date.
     - The what-if: :283.
     - FromView at GameController:6417: the polling day. 2025-02-23 counts as split (`<` is strict), as intended.
     - FromProposal copies the posts and stamps the install day.
     - LeaveCabinet and ConfidenceDiagnostic:234 reuse the record's own date.
   - **No start can seat the outgoing Scholz cabinet on the split weights:** no start date falls in [2025-02-23, 2025-05-06).
   - **The epoch default:** only Sweden callers rely on it (AiMotionReach ×4, Confidence:411, Formateur:45), and Sweden's weights are undated. Every runtime caller passes a date.

(2) **Yes.**
   - `Answer` uses its `date` for the expected posts and both weights.
   - PreviewFormation (the call tabling makes) and the sheet both use CurrentDate, and the clock holds while the sheet waits.
   - The draft's posts are allocated on the draft day and weighed again on the vote day. The only date a formation can straddle is the Poland/USA mean's 2025-02-23 (Education 1.33 → 1.2675). Only a Polish AI round open across that day reaches it. Latent.

(3) **Yes.** Only lines 3072 and 3454, each adding `CurrentDate`.

(4) **No.**
   - The only no-player reader of who holds a post is `FinancePartner.Holder`/`AiHolder`, for Finance only. SimulationManager 2449/2476/4796 and the Desk are player-only.
   - France's stand-in is provisional, so `Holder` returns null before and after the change.
   - The dump opens on Sweden's start (TrajectoryBaselineDump:101). There Germany's Finance stays with the SPD, Italy's weights are undated, and Poland's Finance stays with KO.
   - The sentinel passes against fp717 in sim753b:581-605, which launched at 10:31:58, after the last code save. The byte-identical dumps cannot be re-checked because they were deleted.

(5) **n753e: 8 of 8 clean (:5274), with two caveats.**
   - n753e ran 10:27:17-10:28:38, but GovernmentRecord.cs was saved at 10:30:15. Its three-argument calls must predate the run to compile, and sim753b covers the later save.
   - sim753b has no CHECKS summary (it ends at LawCompositionDiagnostic), so only its sentinel result is confirmed.

## Notes for the record
- **The diagnostic reports a planted fact as the world's.** n753e:947/951 print a planted FDP at Finance ("holder Germany … Finance FDP"; "the FDP holds Finance") with no mark that it was planted. Unplanted, Germany's 2024 start gives the SPD Finance, so §716 is not exercised at any start Germany can open on. That is player-visible, and it is the 2021 move Elias was told about.
- **A latent crash:** the mean now calls `Weight(Germany, …)`. A `CabinetPortfolio` outside the six (a corrupt save) would recurse until the stack overflows, where HEAD returned 1.0. A default guard closes it.
- **A premise for Elias:** Poland's and the USA's weights now move on German and French reorganisation dates.
- **The France date is not on a stored page.** The stored JORF of 2022-05-17 still reads "et de la relance", and the industry title first appears in the one of 2024-01-10. The 2022-05-20 boundary itself is on no stored page. It is inert, since no start or formation falls in between.

## What was done about the second pass (the author's)
1. The md's paragraph replaced whole (the first rework's pattern had missed it - the sentence read "the game's Education keeps one weight per country"): the dating, France's Industry, the paper's caveat, Italy's Education, Poland's moved posts, the premise that the mean moves on German and French dates. 2. `FinancePartnerDiagnostic` plants RN at France's Finance, and decision A's check again requires a non-head holder; both planted holders are marked PLANTED in the output. 3. The comment's date corrected. 4. This note: the pass above is the second reader. Notes taken: a guard - a post outside the six weighs 1.0, never the recursing mean; France's key moved to the earliest stored decree naming industry (the JORF of 2024-01-10), every French start later. **A process slip, stated:** the guard, the France key and the plant were written while `sim753b` was still running (it had compiled at its launch, so its 57 of 57 is the tree before them) - against the standing rule; the bar was run again on the final tree.
5. **The France key, again.** Moved to 2024-01-10, it was not inert: the mean Poland takes reads France at the same day, so on Tusk's day (2023-12-13) Poland's Finance mean fell to 2.28 and `PortfolioSalienceDiagnostic`'s mean check failed (named run `n753f`, the one failure). So the boundary was sourced instead of moved: the *Décret du 20 mai 2022 relatif à la composition du Gouvernement* (art. 1, "M. Bruno LE MAIRE, ministre de l'économie, des finances et de la souveraineté industrielle et numérique") is now stored from an archived copy (`ElectionsData/france/raw/executive/wb_legifrance_JORFTEXT000045819551.html`, with its issue page `wb_jorf_2022_05_21_0118.html`; Légifrance refused the live page, 403), and the key is 2022-05-20 again, with its citation in the code and the md.
