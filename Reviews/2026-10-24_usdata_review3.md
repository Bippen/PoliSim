# Second delta review: wave/usdata - M-1, L-1, prereg amendment 1 (K114) and M-2

**Branch:** `wave/usdata` in `G:/UNITY/Projects/PoliSim-wt/usdata`. Range `732ee831..c82b8d63`, three commits:
- `a84cf09d`: the delta review's M-1 and L-1;
- `80af3dbb`: prereg amendment 1, alone;
- `c82b8d63`: M-2 in the check.

Read against:
- the delta review `Reviews/2026-10-24_usdata_review2.md`;
- the prereg `Reviews/2026-10-24_usdata_prereg.md` as of `f8065952`, and its amendment;
- K114 in main's `docs/RULINGS_2026-10-23.md:166`;
- `CLAUDE.md` and `Tools/cgate/IMPL_BRIEF.md`.

**Reviewer:** independent and read-only. No Unity was run, nothing was pushed, and the only file written is this one. What was run:
- the compile gate, `bash Tools/cgate/gate.sh G:/UNITY/Projects/PoliSim-wt/usdata`: `GATE OK`;
- a perl line-ending and encoding audit of every changed file, both blobs and the work tree (section 5);
- `git show --stat` and parent checks on the three commits, and `git diff f8065952 80af3dbb -- Reviews/2026-10-24_usdata_prereg.md`.

**Files reviewed:**
- `Assets/Scripts/Elections/LiveCampaignSnapshot.cs`: the run-up HQ's notes, 270-335;
- `Assets/Scripts/UI/GameController.CampaignAction.cs`: `DrawActionOptions`, 90-140;
- `Assets/Editor/UsCampaignDiagnostic.cs`: (m), 640-690; `ClosedFormMovePp`, 388; `Margin`, 383;
- `Assets/Editor/CampaignPersuasionDiagnostic.cs`: the K84 mailing check, 780-790;
- `Assets/Scripts/Simulation/SpatialBudgetVote.cs`: the `TryUsScale` summary at 69; `Applies`, `ScaleAt`, `RateAt` and `RatesOf`;
- `Assets/Scripts/Simulation/UsMajorityReconciliation.cs`: `TryPartyRate`, 81-90;
- `Reviews/2026-10-24_usdata_prereg.md`.

**Read for tracing:**
- `UsPresidentialElection.Count`, the ground-game overload, 477-530;
- `UsGroundGame.Effects`, `Frame`, `EffectsOf`, `Apply`, 162-290; `NoiseSigmaPp`; `NoiseSeedOf`;
- `ElectionDay.Count`, the 5b overload, 120-175, where whole-vote rounding happens inside `Effects`;
- `ParliamentaryCount.SigmaPp` / `NoiseAtZero`;
- `GameController.Campaign.cs`: `Kronor`, `Purse` and every `Kronor` call site;
- `GameController.CampaignPolling.cs:241/261`, `CampaignActions.Spec` / `TheEight`, `PreCampaignRun` (`FundraiseLine`, `Price`, `StepDay`'s FUNDRAISE case, every refusal string);
- `Congress.Applies`, `ParliamentSystem.cs:1160-1170`, `SimulationManager.MajorityReconciliation.cs`;
- every Editor file that touches a US purse: `UsCampaignSpendingCheck`, `UsCampaignGeographyCheck`, `GroundGameCountDiagnostic`, `UsDebtLimitLiveDiagnostic`.

## Verdict: READY

Nothing blocks.
- **H-1** is withdrawn by K114 ("does not fail the bar"). Rule 2's original text already says that, so neither the code nor the prereg owed a change for it.
- **M-1** is closed. Every purse amount printed with a currency is converted exactly once. Sweden and Germany are bit-identical.
- **The amendment** was committed alone, before the check that reads it. It is stricter only and consistent with K114. The original text is untouched.
- **M-2** is closed. The named line now prints only when the live path itself has been judged, and every guard the review asked for is in the condition. I traced each way a broken live path could fail, and each one fails the check rather than reaching the named line.
- **The Lows** below are wording and record-keeping only.

## 1. M-1: the purse printed in the national currency

**The YESTERDAY note** (LiveCampaignSnapshot.cs:290-296):
- `perUnit = LiveCampaignSetup.PursePerUnit(country.Id)` multiplies `e.Cost`, `e.Raised` and `(e.Raised - e.Cost)` once each.
- Those three are units, set in `StepDay`: cost and revenue are each divided once by `PursePerUnit` where they enter the purse (PreCampaignRun.cs:361-366). So the note now prints `letters × tariff` and `letters × revenue` again, up to FP. That is the same figure as the FUNDRAISE line (`FundraiseLine`, national and never converted).
- A television buy's `e.Cost` is `Price` in units. Multiplied once, it equals `Kronor(MoneyCost)` on the queue.
- The refusal branch prints no amount. No `PreCampaignRun` refusal string carries a money figure (checked: 105, 107, 137-140, 299-315).
- Sweden and Germany multiply by exactly 1.0, so their output is bit-identical.

**The rest of the run-up and campaign HQ.**
- Every other printed purse figure goes through `Kronor`, which multiplies once (GameController.Campaign.cs:1340). The call sites are 541, 639, 667, 678, 857 and 1065, `CampaignAction.cs:138`, and `CampaignPolling.cs:241/261`.
- None of their arguments was converted before. The snapshot's pools, the queue's `MoneyCost`, `Spec` costs, upkeep and payroll, the poll's `offer.Cost` and `perPoint` are all units.
- `LiveCampaignSnapshot` builds `ResourcePool(pre.Money, ...)` in units (330), and the hero converts it.
- There is no other `CurrencyCode` or `" kr"` money print in the campaign code. The other `CurrencyCode` uses are per-tonne energy-tax rates, which are a different path and are not purse figures.
- Nothing is double-converted.

**The act-price column** (GameController.CampaignAction.cs:106-108).
- `widestCost = Kronor(max over TheEight of Spec(k).MoneyCost)`. The rows print `Kronor(option.MoneyCost)`, and the only `ActionOption` construction (`UiScreenshotDriver.cs:6497`) passes `spec.MoneyCost`. So the maximum over `TheEight` is the widest printed figure.
- The table's maximum is 500,000 units, which is "500,000", 7 characters. For Sweden and Germany the template therefore stays `"000,000"`, and the width is bit-identical to before.
- For the USA it is `Kronor(500,000)` = "72,297,335" (10 characters), so the column widens to the real string.
- The cost face is mono (the comment at 110-111), so equal-length digit strings measure the same.

**The K84 diagnostic** (CampaignPersuasionDiagnostic.cs:786).
- `mail.Raised`, `Log[0].Cost` and the chest's change are each multiplied once. The postage `LetterTariff(c)` stays national. The line now labels the factor.
- The pass condition (784-785) is unchanged and still in units.
- For Germany the figures are identical, but the line's text gains the label. No golden or sentinel reads that text.

**Other Editor prints.** `UsCampaignDiagnostic` (f) and (l) already print dollars (197-200, 475-477). `UsCampaignGeographyCheck`, `GroundGameCountDiagnostic` and `UsDebtLimitLiveDiagnostic` print no purse unit with a currency. No miss remains.

## 2. The amendment (80af3dbb)

**Ordering.**
- `80af3dbb` touches one file, the prereg (+10 lines, no deletions). Its parent is `a84cf09d`.
- `c82b8d63`, the only commit whose code reads the amendment, has `80af3dbb` as its parent (21:07:52, then 21:08:37). No Unity run lies between them.

**The original text is unchanged.** The diff is one hunk at `@@ -39,3 +39,13 @@` that only appends after the planted-tooth section. Lines 1-41 at `f8065952` are byte-identical in `c82b8d63`.

**Stricter, never looser.**
- The amendment adds three conjuncts to the named branch's condition: `gained == 0`, `GroundGame.Count == states`, and the live-move identity at 1e-9.
- It removes none. "Every other state bit for bit" and the noise-off closed form at 1e-3 are kept.
- Rule 1 and the flip branch's pass are untouched, except that the flip pass also gains the identity.

**Consistent with K114.** It quotes K114's words and keeps "does not fail the bar; every other departure fails". It also records that the named line is printed, not checked, which is the opposite of the delta review's H-1 remedy, now withdrawn by K114.

## 3. M-2: the K113 branch (UsCampaignDiagnostic.cs:661-688)

**The guard.** The named line (685) is reached only when the negation of the condition at 678 holds. All of these must be true:
- the call does not flip;
- every other state's winner and shares are bit for bit the same;
- the noise-off move is within 1e-3 of the closed form;
- `flipped != null`;
- `gained == 0`;
- `flipped.GroundGame.Count == noisy.Length`;
- `liveIsCount`.

Any failure goes to `Check(...)`, and that check cannot pass without a flip. Every NaN path fails: a null `after` gives a NaN `liveMove`, and a null `liveWorked` or `liveNone` gives a NaN `liveExpected`. Since `NaN <= 1e-9` is false, `liveIsCount` is false.

**The identity is a real test of the live path, not a tautology.**
- **The left side** (`liveMove`, 669) is read off the live count. `UsPresidentialElection.Count` (UsPresidentialElection.cs:511-515) calls `Effects(states, groundGame, noiseSeed, NoiseSigmaPp)` on its own internal derived states, with the campaign and seed it was handed. `Apply` then adds each effect to the shares. The margin is differenced against the noise-only count's.
- **The right side** (`liveExpected`, 664-668) is the diagnostic's own direct `Effects` on `derived` (537, the no-campaign count on the same `polled`, `atStart`, `tieSeed`, with no previous count; so it has the same `CollegeOf` inputs), with `worked` and `none` at the same seed and sigma.
- Algebraically the two sides are equal exactly when the count routed the campaign, the seed and the sigma unchanged into `Effects` and applied the result additively. In FP the residual is about 1e-14 points, far under 1e-9.
- Whole-vote rounding happens inside `ElectionDay.Count`, so both sides carry it. The 1e-9 can therefore never refuse a correct live path.

**The live-path defects I traced, each of which now fails:**
- **Doors dropped or mis-routed in the count** (for example `none` passed for `groundGame`). `liveMove` is about 0 (the same noise on both counts), while `liveExpected` is the move on the worked map, so the identity fails. Doors going to the wrong state also break `othersSame`.
- **A noise-seed or sigma mismatch inside the count.** For example, the count ignores `noiseSeed` and keys its own seed, or counts at sigma 0.
  - The noise is scaled by sqrt(s(1-s)) on the post-turnout shares (ElectionDay.cs:139-142), so the worked state's move depends on the draw. Two seeds differ by about 5e-4·Δz points.
  - That is roughly 1e5 times the tolerance, so the identity cannot "trivially hold".
  - The diagnostic opens no `NoiseAtZero` scope, so `NoiseSigmaPp` is the declared 1.2.
  - A count that keyed its own seed by the same `NoiseSeedOf(USA, polled)` would give the same draw. That is not a defect.
- **The count's `Effects` failing (null).** The states then stand as derived, without noise. `GroundGame` keeps its empty default, so `Count != noisy.Length` fails, and the shares differ from `noisy` everywhere, so `othersSame` fails too.
- **A live path that mutated the campaign object it was handed**, for example zeroing its doors before counting. Both `Effects` calls would then read the zeroed doors, and the identity alone could hold. But `flipClosed` takes its doors from the local `ceilingDoors` (673), not from `worked`, while `flipMove` reads `worked`. So `asClosed` fails. The guard set closes this case as a whole.

  For the record, `Count` and `Effects` are pure in the campaign today: `Effects` clones `Eligible` and only reads the doors.

**What the identity does not test:** `Effects`' own arithmetic with the noise on. Both sides call the same function. Its noise-off arithmetic is held to the independent closed form, by rule 1's band and by `asClosed`. Its noise-on behaviour is held by nothing independent here. The amendment registers exactly this ("the count's own function on the live inputs"), and it explains why the noisy move is not held to the closed form at 1e-3. This is by design, not a finding.

**The redundant guard.** `gained == 0` is implied: no flip, and every other winner identical, in a state chosen with `Districts.Length == 0`. It is harmless.

## 4. L-1: the comments

- **SpatialBudgetVote.cs:69.** "Its callers gate it on `Applies` (`Congress.Applies`: a US Congress with a sworn Senate - `RatesOf` and `UsMajorityReconciliation.TryPartyRate`...); that it is the player's Congress rests on those callers running on the player's day tick." This is accurate:
  - `RatesOf` returns null unless `Applies` (109). `TryPartyRate` gates on it (84).
  - `ScaleAt(Country,...)` and `RateAt(Country,...)`, the other `TryUsScale` callers, are reached only through those two.
  - `ParliamentSystem.cs:1163` also tests `Applies` before `RatesOf`.
  - The Editor checks call `TryUsScale` directly, which is expected in a harness.
- **UsMajorityReconciliation.cs:84.** "Congress.Applies - a US Congress with a sworn Senate; the player's by this method's callers, all on the player's day tick." This is accurate for the game's callers (`Book` and `Applied`, from `SimulationManager.MajorityReconciliation.cs:55/75/103`). Strictly, "all" leaves out the harnesses: `UsMajorityReconciliationDiagnostic.cs:72/121/145` and `UiScreenshotDriver.cs:2121`. See L-2.

## 5. Encoding

| file | blob at c82b8d63 | work tree | CR-CR | non-ASCII bytes (732ee831 -> c82b8d63) | `\xC3\x82`/`\xC3\x83` |
|---|---|---|---|---|---|
| CampaignPersuasionDiagnostic.cs | LF (821) | CRLF 821/821 | 0 | 31 -> 31 | 0 |
| UsCampaignDiagnostic.cs | LF (980) | CRLF 980/980 | 0 | 18 -> 18 | 0 |
| LiveCampaignSnapshot.cs | LF (402) | CRLF 402/402 | 0 | 73 -> 73 | 0 |
| SpatialBudgetVote.cs | LF (155) | CRLF 155/155 | 0 | 0 -> 0 | 0 |
| UsMajorityReconciliation.cs | LF (216) | CRLF 216/216 | 0 | 8 -> 8 | 0 |
| GameController.CampaignAction.cs | LF (442) | CRLF 442/442 | 0 | 128 -> 128 | 0 |
| 2026-10-24_usdata_prereg.md | LF (51) | CRLF 51/51 | 0 | 0 -> 0 | 0 |

- No file is mixed.
- No added C# string literal contains `//`.
- No `<see cref` is added.
- No gendered word is added (he, she, his, her and him all grep empty on the added lines).
- The added non-ASCII is none. The `·` in the YESTERDAY strings was there before.

## Findings

### High

None. H-1 is withdrawn by K114.

### Medium

None. M-1 and M-2 are closed.

### Low

**L-1. The named line's "shortfall" is the noise-off figure, and the line does not carry the live identity it was judged on.**
- **Where:** UsCampaignDiagnostic.cs:685-686 (the `sb.Append` at 685, its shortfall argument at 686).
- **What happens now:** the shortfall is `|margin| - |flipClosed|`, the closed form with the noise off. The live remaining margin printed in `flipLine` (`after[t]`) differs from it by the scaled-noise term, about 5e-4·z points. So the two figures on one line can disagree in the third decimal, and a shortfall near zero could print with the wrong sign.
- **What to change:** K114 asks for "the move and the shortfall" of the live 5b count. Either:
  - print `|Margin(after[t].Shares)|`, the live shortfall, beside the closed-form one; or
  - label the printed one "noise off".

  Append `liveMove` against `liveExpected` (as the failing branch already does at 681) so that the record shows the guard held.
- **Cosmetic:** this does not change pass or fail.

**L-2. "all on the player's day tick" (UsMajorityReconciliation.cs:84) leaves out the harnesses.** `UsMajorityReconciliationDiagnostic` and `UiScreenshotDriver` call `TryPartyRate` and `Book` off the day tick. Say "the game's callers". This is a nit.

**L-3. Integration carry (unchanged from the delta review's L-2 and L-4, now three commits).**
- Keep `f8065952`, `80af3dbb` and `c82b8d63` unsquashed, in order. Alternatively, record the prereg and amendment 1 in `docs/specs/USA_STAGE_PLAN.md`'s US-31 block before the named run, per IMPL_BRIEF's `plan_prereg` convention.
- Bump the save format past both K110's meaning and wave/fed's 63.
- The first review's M-3, L-1, L-2 and L-8, and the delta review's L-3 (the per-act dollar figures on the play-calibration list), still belong in the integrator's draft records.

## What a passing named run must print

Everything the delta review listed under "What a passing named run must print", with these changes:
- **(m)'s able-to-flip outcome:** either `(m) THE DONE-WHEN, able to flip it (K112, preregistered; amendment 1) ... FLIPS ...` `ok`, with the live move equal to the count's own function to six decimals; or the unchecked `REGISTERED OUTCOME CHANGED (K113)` line. That line is the ruled record (K114) and goes to Elias per K112. A failing `(m) THE DONE-WHEN` line in the no-flip case is a defect, never the ruled outcome.
- **A film of the US run-up HQ** (the Canvas film rule) after a FUNDRAISE day. The YESTERDAY note must read the postage and revenue the FUNDRAISE line priced (for example `82,000 USD ... RAISED 40,100 USD ... NET -41,900 USD`), not 567 / 277 / -290.
- **The act screen's price column** must show eight-digit dollar prices unclipped.
