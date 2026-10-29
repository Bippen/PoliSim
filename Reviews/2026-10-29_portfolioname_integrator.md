# Three filed Lows closed: the portfolio's raw name, the run-off chip, the run-off debate - the integrator's reading (2026-10-29)

**What was owed.** Three Lows filed from films:
- pldesk: the minister alert read "A leak from InteriorJustice";
- PL-C, two Lows:
  - on the run-off HQ, the strip's polling chip read the next Sejm date (17 Oct 2027), not the run-off's 1 Jun 2025;
  - the footer announced a debate on 7 Jun, after the run-off's polling day, although K258 gives the run-off no debate.

**Files reviewed:** `Assets/Scripts/Data/PortfolioEffectiveness.cs`, `Assets/Scripts/Simulation/CabinetSystem.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/UI/GameController.cs`, `Assets/Scripts/UI/GameController.PoliticsV35.cs`, `Assets/Scripts/UI/GameController.TopStrip.cs`, `Assets/Scripts/Elections/LiveCampaignSnapshot.cs`, `Assets/Editor/PortfolioDisplayNameCheck.cs`, `Assets/Editor/CommentImmunityCheck.cs`, `Assets/Editor/CheckSuite.cs`.

**Built (on main).**
- **The portfolio's name.**
  - `Effectiveness.DisplayName` (Data) is now R4-4's one scoped lookup, moved from the UI's private `GetPortfolioName`, which now delegates to it.
  - `CabinetSystem`'s resignation and leak texts and `SimulationManager`'s approval-ledger line read it.
  - The reshuffle's ledger label (`GameController.PoliticsV35.cs`) now uses the same name, not `DisplayName.Of(portfolio.ToString())` ("Interior Justice"); this was the review's M2.
- **`PortfolioDisplayNameCheck`** (cheap, enrolled in CommentImmunityCheck's MustStrip; the review's H1). It checks:
  - every portfolio displays as words;
  - the comment-stripped runtime holds no interpolated string that writes a portfolio raw (including `.ToString()`, format suffixes and `DisplayName.Of(portfolio…)`; the review's M3);
  - its teeth: 5 planted raw writes caught and 4 display-name writes passed.

  It prints its limits (another variable name, a concatenation, a non-portfolio enum).
- **The run-off chip.** `GameController.TopStrip.cs`: a campaign under way votes on its own day. When the player's running campaign's election date comes before the calendar's next polling day, the chip shows the campaign's.
- **The run-off debate.** `LiveCampaignSnapshot`: only debate days before polling day are announced or listed. The run-off's 14 days never reach the default days 20 and 41. The setup is untouched, so the simulation reads the same array.

**The review** (`Reviews/2026-10-29_portfolioname_review.md`) returned NOT READY with H1, M1-M4 and Lows.
- H1 (CommentImmunity enrolment): fixed.
- M1 (ledger rows): added at s946 with this file as evidence.
- M2 (the reshuffle label): fixed.
- M3 (the regex's reach): widened, with the remaining limits stated.
- M4 (other enums raw in player text: `TaxType` keys in `DescribeBudgetBill`, `{bill.Type}` on the desk list and ledger labels, `({result.Outcome})` in formation texts): pre-existing, FILED as its own item.
- Lows: the `Debug.Log` line exclusion is kept, stated as a limit; the stray `PlayerBuild.cs`/`ERRANDS.md`/`ProjectAuditorSettings.asset` stat-dirt is kept out of the commit.

**Measured.**
- `cheappdn` (before H1's fix): 144 of 146. ReviewLedgerCheck failed as expected (the rows had not been written yet), and CommentImmunityCheck flagged H1.
- PortfolioDisplayNameCheck: CLEAN (teeth 3/3, 7 names, 398 files, 0 raw writes).
- The review's perl run of the pattern over HEAD's files finds the 3 raw writes the Low came from, so the check fails while the defect stands.
- `realpdn` (Poland 1280, `-shotinterrupts`): exit 0, 22 captured, 0 failed. Frame `realpdn_plc04_runoff_campaign_hq.png` against `realplci3_plc04_runoff_campaign_hq.png`:
  - the chip reads "in 12 days" where it read "17 Oct 2027";
  - the footer's "DEBATE IN 17 DAYS · 7 JUN" is gone.
  - (The strip counts from the world date, 20 May; the HQ's own clock shows the day stepped to, 21 May, and "11 D TO POLLING DAY". This is the HQ's convention, as before.)
- No baseline can move: the texts feed no digest or save (the review verified this), and the debate filter and the chip are reads.

**Verdict:** READY once the final cheap bar is clean.
