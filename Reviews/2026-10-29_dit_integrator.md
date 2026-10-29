# wave/dit: the integrator's reading at the merge (2026-10-29)

**Files reviewed:** `Assets/Scripts/Elections/ItalianNight.cs`, `Assets/Scripts/Elections/ItalianCount.cs`, `Assets/Scripts/Elections/ElectionNightFromModel.cs`, `Assets/Scripts/Elections/Generated/ItalianNightCartogram.cs`, `Assets/Scripts/UI/ElectionNightScreen.Italy.cs`, `Assets/Scripts/UI/ElectionNightScreen.Governs.cs`, `Assets/Scripts/UI/GameController.ItalianNight.cs`, `Assets/Scripts/UI/GameController.cs`, `Assets/Scripts/UI/ItalianCellView.cs`, `Assets/Scripts/UI/PoliSimTheme.cs`, `Assets/Scripts/Testing/UiScreenshotDriver.cs`, `Assets/Editor/ItalianNightDiagnostic.cs`, `Assets/Editor/ItalianCountDiagnostic.cs`, `Assets/Editor/PartyInkDrawSiteCheck.cs`, `Assets/Editor/CheckSuite.cs`, `Tools/dit_night_prep.pl`, `Tools/film_scope.tsv`.

**Why this exists.** Design's D-IT return (2026-10-29: boards 34a-34d, `night.json` 3b5d3c8f…, filed out of tree at `PoliSim-captures/design/RETURN_2026-10-29_DIT/` with SHA256SUMS) asked for Italy's election night. It wanted the Camera and the Senato drawn one cell per seat, the count as the legend, Who governs in D-NIGHT's rect, and the † view, all on the game's own count.

**The reviews.**

| Review | Verdict | What it found, and what followed |
|---|---|---|
| Review 1 | NOT READY | H1: the check was unregistered (the integrator's). M1: the board's "all uncounted votes to the largest rival" can call a chamber wrongly. M2: the Called lane read the live leader. L1-L11. |
| Review 2 | NOT READY | N1: a minimum over a set of allocations is still not a lower bound. It built a wrong call: three lists at 3.01 %, the Camera called at 146 with 201-202 certain, ending at 198-200. The integrator ruled a provable floor (K281). |
| Review 3 | NOT READY | M1: a count bug on main since PS-7. A zero lett. i / art. 17 quotient seated no one (238 of 245 Camera list seats on its vote). Fixed in the count (K283). |
| Review 4 | READY | 18,081 votes conserve after K283 and nothing moves where nothing was lost. The floor never overshoots across 18,083 votes. N1-N3 landed. |

**The integration films found what no review could** (realdit / realditw, 2026-10-29):
- Who governs drew two layers in every Italian frame: the † view was built active and never hidden.
- The chamber pick ran under Who governs: the map key's note widened its column.
- The foot's verdict clipped (67 against 45).
- MetaTextCheck failed on four hand-made possessives.
- The log's "smallest map label 3 units" read the hidden Senato map's default rect.

All five were fixed on the branch (0f623653) and re-filmed clean (realdit2 / realditw2: 19 captured, 0 failed, no CLIP, map labels 21 units = 14 px at 1280). At the merge the integrator reordered one width expression (`+ 2f + 2f * Px(8f)`, the same arithmetic) that tripped the check's size-plus-constant pattern.

**The measurement (this tree).**
- ndit3: 5 of 5.
- drydit (Italy 1280): exit 0.
- cheapdit: 147 of 147.
- simdit: 91 of 91. This run was stopped mid-chain by the host for memory; its Unity ran on and finished clean.
- dumpdit2: BYTE-IDENTICAL to `k274` on both seeds. No in-run Italian count hit K283's case.
- The 2022 count is byte-identical before and after K283.
- The 2022 vote is called at Camera 128 and Senato 137 of 147.

**Asks answered for Design:**
- the four DERIVED keys (K278: SICILIA 1-U04's FI refuted);
- art. 84 (K279: one FdI/M5S swap against Design's reconstruction);
- the life senators (K280).

**Verdict:** READY. The merged state equals the reviewed branch (aded5445 + 0f623653) plus the one reordered expression.
