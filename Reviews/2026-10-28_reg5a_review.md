# Review: wave/reg5a - Regional 5a (K146-K149 with K83), abad7ac4 against 5c38ce1c

Reviewer: independent adversarial code review (read only; Unity not run). Branch `wave/reg5a` in the worktree `PoliSim-wt/reg5a`. Scope: `git diff 5c38ce1c..abad7ac4` (27 files, 8 commits), read against `docs/RULINGS_2026-10-23.md` K83/K146-K149, `docs/specs/A_LOOP_PLAN.md` ("Built, 5a", "Built, 5b", "Built, q4") and `CLAUDE.md`, and against the code around each hunk. What I ran: the compile gate (`Tools/cgate/gate.sh` on the worktree: `GATE OK`); `perl Tools/okreg_electorates_prep.pl <scratch>`, whose output is byte-identical (`cmp`) to the committed generated table; `sha256sum` of both zips, which matches `SHA256SUMS.txt` and the generated header; a per-file count of CRLF lines (every changed `.cs` is CRLF on every line, as its base was).

**Files reviewed:**
- `Assets/Scripts/Elections/CampaignPressureRegional.cs` (whole: `ConfigureRegions`, `AddLocal`, `AddLocalAttack`, `RegionalSlot`, `RegionalAttackSlot`, `RegionEffects`, `RegionalShareEffects`, `AbsorptionFor`, `RegionalShifts`, `NationalShift`, `ApplyRegional`, `RegionalOwn.ValuePp`)
- `Assets/Scripts/Elections/CampaignActions.cs` (`CampaignPressure.Add`, `AddAttack`, `ShareEffects`'s regional branch, `BoundsPp` 553-570)
- `Assets/Scripts/Elections/CampaignPersuasion.cs` (`Moved`, `ApplyShift`; read against `ApplyBalanced`, `BalancedAbsorption`, `Coverage`, `ClassEffectPp`, `SharedClassEffectPp`, `AttackEffectPp`)
- `Assets/Scripts/Elections/CampaignRun.cs` (the setup's `RegionalOffset`, `BuyPoll` and `BuyRegionalPoll` 760-798, the scripted queue 880-912, the door and tally region 940-1003, `Finish` 1166-1175, `CurrentPreference` 1313-1330, `RegionAudiences`)
- `Assets/Scripts/Elections/CampaignAi.cs` (`Points`, `Band`, `RegionOfContact`; the candidate builder 640-700 for local-act regions; the defence/answer commitments)
- `Assets/Scripts/Elections/VoteAttribution.cs` (`Value`, `Explain`)
- `Assets/Scripts/Elections/ParliamentaryCount.cs` (`Count`, `ShiftOf`), `Assets/Scripts/Elections/NationalElection.cs` (`RegionalOf`)
- `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs`; `Assets/Scripts/Simulation/SimulationManager.cs` (`QueueCampaignDecisionNow`, `PlayerScriptOver`, `RestoreCampaign` - unchanged, read for the replay); `Assets/Scripts/Elections/PlayerCampaignRecord.cs` (`ToDecision`, unchanged)
- `Assets/Scripts/Elections/UsGroundGame.cs` (`Of`, `Centre`, `WithPattern`, `Effects`, `EffectsOnCurve`)
- `Assets/Scripts/Elections/RegionalCampaign.cs` (whole: `RegionLayer`, `RegionalPolling`), `Assets/Scripts/Elections/SwingRegions.cs` (`FromPoll`, unchanged), `Assets/Scripts/Elections/Polling.cs` (`Conduct`, unchanged)
- `Assets/Scripts/Elections/LiveCampaignSetup.cs` (`WithOffsets` and its three call sites), `Assets/Scripts/Elections/LiveCampaignSnapshot.cs` (the intelligence notes, the map, `OfferLine`), `Assets/Scripts/UI/GameController.Campaign.cs` (the POLL REGION chip)
- `Assets/Scripts/Elections/Generated/PolishOkregElectorates2023.cs` (+ `.meta`), `Tools/okreg_electorates_prep.pl`, `ElectionsData/poland/raw/okreg_electorates_2023/` (`SHA256SUMS.txt`, `fetch_log.txt`, `okregi_sejm_csv.zip`, `wyniki_gl_na_listy_po_okregach_sejm_csv.zip` - both CSVs read)
- `Assets/Editor/RegionalCampaignDiagnostic.cs` (+ `.meta`, whole), `Assets/Editor/CampaignPersuasionDiagnostic.cs` (the delta plus 90-180, 220-352), `Assets/Editor/Gate18Diagnostic.cs` (the delta)
- `Assets/Scripts/Elections/CampaignPressureRegional.cs.meta`, `Assets/Scripts/Elections/RegionalCampaign.cs.meta` (GUID-only)
- Read for the traps: `Assets/Editor/CheckSuite.cs` (registration), `Assets/Editor/UiLeverVerbCheck.cs`, `Assets/Editor/ConstantProvenanceCheck.cs` (scan root), `Assets/Editor/TrajectorySentinelCheck.cs` (the no-policy family), `Tools/bar_tier.ps1` (money paths)

## What holds (checked, not assumed)

1. **The national movement is the audience-weighted sum of the regions, exactly by construction.** `NationalShift` is Σ_r w_r·shift_r with w_r = A_r/ΣA, the same weights `RegionalShareEffects` and `UsGroundGame.Centre` use. `Moved` is `ApplyBalanced`'s movement line for line, without the floor. The movements in each region sum to zero, because W's held columns sum to one. **The DERIVED identity is correct.** A national act spread over the standing regions by A_r/A_stand covers each one at 1 − e^(−E/A_stand). Summed with weights A_r/A_all, that is (A_stand/A_all)(1 − e^(−E/A_stand)), the national r_p(1 − e^(−E/(r_p·A))), because `StandingShare` and the tally's `_allAudience` both sum `Regions[].Audience`. q̄ is spread with E, so CACE·q̄ is the same in every region.
2. **K81 per region holds.** Within a region every standing party meets every other in full. So `BalancedAbsorption`'s balanced solution is the uniform W = 1/(n_r − 1) whatever the shares. Equal effort per voter reached gives equal coverage in every region, so equal effects, so nothing moves. A lone campaigner moves w_r·e_r summed, which is its national sourced effect. Germany's reading (absorption region by region instead of the national overlap balancing) meets all three of K81's conditions and is defensible.
3. **Edge cases are handled.** A party that stands nowhere has `_standAudience` 0, so `NationalShareIn` is 0 and its national acts land nowhere, as the national formula's reach 0 does. A zero-audience region: `Coverage` returns 0 when electorate ≤ 0, so it gives no NaN, and its weight is 0. A local act where the party has no list is skipped before the tally (`CampaignRun` 892), and `RegionEffects` skips a non-standing party. An attack takes only where its target stands (`onBallot`). Others behave as any party.
4. **The no-shift count is the old arithmetic, bit for bit.** `ShiftOf` returns null when no shift is handed or every row is zero. `RegionalOf`'s five-argument form passes null, and the prior loop is skipped. A staging with fewer than two regions never calls `ConfigureRegions`, so its tally, AI, attribution and count take the old national code. The no-policy dump holds no election (`TrajectorySentinelCheck`'s own notes), so it cannot move.
5. **The US pattern is centred by the right weights.** `Centre` uses `Campaign.Audience` = `Regions[r].Audience`, the tally's own weights. Each column is zero-mean nationally. Each state's three still sum to zero, because the row sums are zero before centring and the column means cancel. `WithPattern` clamps at zero and renormalises, so shares stay valid under any campaign. `EffectsOnCurve`, US-31's registered function, is deliberately left unpatterned, which keeps the registration.
6. **The poll.** `Conduct` draws exactly `SampleSize` uniforms whatever the truth. A regional poll therefore consumes 600 draws of the shared `CampaignAi` stream, as `BuyPoll` already does. The replay rebuilds it from the queued record (`ToDecision` keeps `RegionIndex`), so a load replays the same stream counts. The price is charged once: the scripted branch `continue`s before the generic `TrySpend(d.Spend)`. The margin is the binomial 1.96·√(s(1−s)/n) at the house's own sample.
7. **The sources are genuine.** Both zips match `SHA256SUMS.txt`. The generator reproduces the committed table byte for byte, and it refuses to write unless the sums are 29,532,595 and 21,596,674. Those are the 2023 Sejm's figures (PKW notice, Dz.U. 2023 poz. 2234: eligible 29,532,595; valid 21,596,674).
8. **Performance.** The night's attribution is `Explain` for the player alone: 2^11 = 2048 coalitions × k regions × `RegionEffects` (n parties × 10 slots, three `exp` per party, n² attack lookups) plus `Moved` (n²). For Sweden (k = 29, n ≈ 8) that is about 59k region evaluations: roughly 5·10^7 flops, 1.4·10^7 `exp`, 1.5·10^6 small allocations. That is on the order of 0.1-0.3 s once per election night. `AbsorptionFor` is built once per ledger. The daily `CurrentPreference` rebuilds k uniform balancings, which converge in a couple of sweeps. Acceptable.
9. **The traps are clean.** No `{0}'s` possessive was added. The two new constants carry [AUTHORED-DRAFT]; `ConstantProvenanceCheck` scans `Assets/Scripts/Simulation` only. The chip goes through `QueueCampaignDecision`, an existing answered verb. CRLF is kept throughout. The compile gate passes.

## Findings

### High

None.

### Medium

**M1 - `RegionalCampaignDiagnostic` is registered in no suite.** `Assets/Editor/CheckSuite.cs` around 570 lists `CampaignPersuasionDiagnostic` but not `RegionalCampaignDiagnostic`, and nothing else in the tree names it. Its seven statements (the identity, K81 per region, the count, the US pattern, the poll, the AI value, the PKW table) therefore run only when someone types the `-executeMethod`. Scenario: a later change to `RegionalSlot` loses the national remainder for a mixed national-and-local slot. Every bar stays green, because only the unregistered check holds the identity and the one-region rule. Fix: add the row next to `CampaignPersuasionDiagnostic`.

**M2 - "TOO CLOSE TO CALL" understates the lead's error. This rule is newly fed by real polls.** `Assets/Scripts/Elections/SwingRegions.cs:50-52` (unchanged, but this branch is the first to show it bought data, through `RegionalPolling.Readings`/`StateOf` and the intelligence lines in `RegionalCampaign.cs:233-268`) combines the two shares' margins as √(moe_L² + moe_R²). That is right only for independent samples. In one multinomial poll, cov(p̂_L, p̂_R) = −p_L·p_R/n, so Var(p̂_L − p̂_R) = (p_L + p_R − (p_L − p_R)²)/n. Scenario: a regional poll of 600 reads 33.5 % against 28.0 %. The code's gap error is about 5.2 pp, so the region reads POLLED with "LEADS BY 5.5 ± 5.2". The correct 95 % error is about 6.2 pp, so the interval of the lead contains zero and the region should read TOO CLOSE TO CALL. That is the exact §36 promise the map makes ("must not tell the player where the race is close until they have paid"). Fix: use 1.96·100·√((s_L + s_R − (s_L − s_R)²)/n) at the poll's own sample (and in `MapRegionReading.GapErrorPp` wherever else it is read).

### Low

**L1 - The bound is still national; the effect is regional.** `CampaignActions.cs:553-570` (`BoundsPp`, read into `Result.CampaignBoundPp`) computes U·coverage·(e/all) over the national pooled exposure. When one class's own/attack split differs by region, the regional effect can exceed it. Scenario: the player's ATTACK verb on a rally in valkrets A, its own rally in valkrets B of equal size, attacks three times its own exposure, x = e/A = 0.05, q̄ at the upper limit. Regional own effect 0.0476·U; "bound" 0.0453·U. `CampaignPersuasionDiagnostic`'s "(b) every party's effect lies inside Σ_class upper x coverage" (line 238, tolerance 1e-12) then fails on a correct model, and the night prints a bound below the effect. An AI's split is the same on every contact, so its effects stay inside by Jensen. Fix: a regional `BoundsPp` (the regions' bounds, weighted) when `Regional`.

**L2 - `ShiftOf` counts movement rows, not matched regions.** `ParliamentaryCount.cs:276`. `moved` counts the non-zero rows of the handed shift before any name is matched, so `CampaignRegional`/`CampaignRegionsMoved` say "laid on N regions" even where no staged name matches the count's regions (for example, a Land renamed in one table). The doc on the field promises "matched by name". Count inside the closure's matched set instead, or after matching.

**L3 - The prior shift is clamped without renormalising.** `NationalElection.cs:484-489` holds a moved share at zero, so that region's movement row no longer sums to zero. Scenario: an attack that takes more than a small party's regional prior. The excess is absorbed by the swing's re-solve across every region instead of staying where the contact landed. This is rare and bounded, but it should be declared beside the "each moved share held at zero" note.

**L4 - `WithPattern` drops the Others column where the ballot has no other candidate, but keeps the Democratic and Republican shifts.** `UsGroundGame.cs` `WithPattern`. That state's row then no longer sums to zero, and the renormalisation rescales both nominees. The shares stay valid; the effect is a small distortion of the two-party margin in those states. Either add the dropped Others shift back to the two in proportion, or declare it.

**L5 - Regional poll details.** `BuyRegionalPoll` (`CampaignRun.cs:196-214`) has three gaps. Its comment says "one a day", but the code has no `lastOwnPollDay`-style guard: a script that queues three regional polls on one day buys three. It does not read `staff[p].Improve(...)`, as `BuyPoll` does, so the pollster role gives a national poll a larger sample but not a regional one; this is not declared. And the poll's truth uses `RegionalShifts` on the prior preference with a uniform swing (declared, and a defensible reading). Add the guard or drop "one a day"; declare the staff reading.

**L6 - Door with no region.** `CampaignRun.cs:994` (and `CampaignAi.cs:929`) tallies a door with region −1 in region 0, following the ground game's existing booking. The AI always names a region, but `QueueCampaignDecisionNow` accepts −1 from the player. Scenario: in Germany, a CSU door queued with −1 lands in region 0, where the CSU has no list. Its persuasion is tallied there, but `RegionEffects` skips it, so it is lost (its turnout was already booked there before this branch). Refuse a regionless door at the queue, or default it to a region where the party stands.

**L7 - Gaps in the diagnostics.** These are not tautologies, but each leaves a statement unproved.
- (a) In (3), the CSU and SPD lone-campaigner check compares against `alone.ShareEffects`, which is the regional code under test. Comparing against a twin national tally with reach `StandingShare` would also prove the identity under partial candidacy, the Germany case point 1 rests on.
- (b) In (6), "pays its price once" reads `PollMoney`, the ledger counter. A double debit of the chest would pass. Read the pool's money before and after.
- (c) In (4), the other-valkrets tolerance is 0.005 pp, half a percent of the 1 pp probe. Exact weights should give about 1e-12, so a weight mismatch between the staging's audiences and the count's valid votes would hide under it.
- (d) Nothing exercises Germany's count (Länder name matching), `WithPattern`'s clamp, a save/load replay with a regional poll, a zero-audience or non-standing landing, or the AI's own path (`Points` → `RegionOfContact` → `ValuePp`); (7) calls `ValuePp` directly.

`CampaignPersuasionDiagnostic` was **not** weakened. Its tolerances are unchanged, and the regional branch recomputes the absorption by hand (`BalancedByHand`) and the CACE arithmetic independently. It shares only the tally's spreading (`RegionalSlot`/`RegionalAttack`), as its note declares. Its "(c, the plumbing)" rebuild now calls `ApplyRegional`, the run's own path, as the old one called `ApplyCampaign`.

**L8 - The okreg names mislead, and the generator's comment is wrong.** The PKW's "Siedziba OKW" is the commission's seat, not the district's name. So `okreg 29 Katowice` is the Gliwice/Bytom/Zabrze district, `okreg 30 Bielsko-Biała` is the Rybnik district, and `okreg 32 Katowice` is Sosnowiec. `Tools/okreg_electorates_prep.pl:83` says only Kraków and Warszawa repeat. Okreg 19's electorate also includes voters abroad and on ships ("zagranica; statki" in the district file), which is why it exceeds its resident count. The layer is unused until a Polish staging; fix the comment and declare both facts before then.

**L9 - Housekeeping for the integrator.**
- (a) `SimulationManager.PlayerElection.cs` matches `bar_tier.ps1`'s money regex, so `ReviewLedgerCheck` needs this review's row (`Tools/review_row.ps1`).
- (b) K147 says "its own trajectory family". The branch names none. By the reading above the no-policy sentinel stays byte-identical; the integrator should record that, or the campaign family it means.
- (c) Every Swedish, German and US campaign result moves by design: local acts now saturate their region, and the AI values them there. `Gate18Diagnostic` now fits on the regional count and may report "DUE A REFIT". `CampaignPersuasionDiagnostic`'s GA-5-1 bar (the player's campaign past 0.1 point) should be read on the bar's output, which I could not run.
- (d) `UsGroundGame.cs:165` reads "a count)'s" (a typo from the edit).
- (e) The plan and COMPLETED entries ("Built, regional 5a") are not written.

## The declared readings (item 10)

- Germany's absorption region by region: meets K81's three conditions (point 2); defensible.
- The poll's truth (the national truth, plus the prior's offset, plus the campaign's regional pattern, with the swing read uniformly and no re-solve): declared. The count re-solves, so the poll can differ from the count in a region by the re-solve's spread, typically well inside the poll's own error. Defensible.
- Others treated as any party: correct.
- Poland's layer unused until a Polish staging: consistent with K146, which only defines the unit.
- No outlet choice: K149 makes it conditional on a source, and none is cited.
- No focus groups, because K148 defers groups: consistent.

## Verdict

The core is sound. The regional tally's identity with the national formula is exact by construction. K81 holds region by region. The count's no-shift path is untouched. The US pattern is centred by the tally's own weights and stays valid. The poll consumes the stream deterministically and replays. The PKW table is reproducible and its sums are the notice's. Nothing found is High. M1 (register the diagnostic) is one line for the integrator. M2 is a pre-existing formula that this item puts in front of the player, and it should be fixed in the fix round or the integration. L1-L9 are small or documentation.

**Verdict:** READY
