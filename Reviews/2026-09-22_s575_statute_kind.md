# The adversarial review of F2e's third source kind — Germany's cap as EEG § 28's statutory tender volumes (§575, 2026-09-22)

**What was read.** `Assets/Scripts/Simulation/EnergyConnectionQueue.cs` as this change leaves it (the new `SourceKind`, `Line.Volume`/`CapMwIn`, Germany's row, and every reader that now takes a year), `Assets/Scripts/Simulation/EnergyFleet.cs` (`CannotPlaceWhy`, `Place`), `Assets/Scripts/Simulation/AiEnergyMinistry.cs` (the ministry's clip and its deferral), and beside them the two callers that are not money paths - `Assets/Scripts/UI/GameController.Energy.cs` and `Assets/Scripts/Testing/UiScreenshotDriver.cs` - plus `Assets/Editor/EnergyMinistryDiagnostic.cs`, which is where the new rule's assertions live. The statute itself was fetched on the day from `gesetze-im-internet.de` (§ 28 and § 28a EEG 2023) rather than recalled.

**The ruling** (Elias, 2026-09-21, recorded §571 item 2): *"Germany's F2e cap: EEG § 28's statutory tender volumes are a third source kind - a legislated limit on annual connections, sourced, named as its kind."*

## 1 · The statute, and whether the code carries what it says

Fetched 2026-09-22, `www.gesetze-im-internet.de/eeg_2014/__28.html` and `__28a.html`:

- **§ 28 Abs. 2 (Windenergie an Land):** 12 840 MW *zu installierende Leistung* in 2023, then **10 000 MW in each of 2024 to 2028**, spread equally over Abs. 1's four Gebotstermine (1 Feb, 1 May, 1 Aug, 1 Nov).
- **§ 28a Abs. 2 (Solaranlagen des ersten Segments):** 5 850 MW in 2023, 8 100 MW in 2024, then **9 900 MW in each of 2025 to 2029**, over Abs. 1's three Gebotstermine (1 Mar, 1 Jul, 1 Dec).

The code carries both as `Line.Schedule` entries with the year each figure runs **from** and **through**, and the `Made` string on each line quotes the paragraph and the figures. Two limits of the source are stated on the row and in the class note rather than quietly widened: **offshore wind is the WindSeeG's**, not § 28's, and the **second segment's rooftop solar is § 28b's**, not § 28a's - so neither stands in a line here. Germany's coal and gas have no line at all, which the page prints as `THE STATUTE NAMES NO VOLUME FOR IT · CAPACITY BILLED, ANY SIZE STANDS`, the same treatment France's and Italy's gas and nuclear already get.

## 2 · The kind's arithmetic — and the defect the year parameter prevents

The two sourced kinds are **not** counted the same way, and this is the whole of what the third kind means:

- an **operator queue** is a STOCK (projects in process at a date): what stands against it is every build order placed and **not yet landed**, and a landed order gives its megawatts back;
- a **statute** is a FLOW (what may be awarded in one calendar year): what stands against it is every build order placed **in that year, landed or not** - a volume is taken when it is awarded, not when the plant connects - and a year that ends gives nothing back.

⚠ **The defect this review found by reading, before any test ran.** A per-year rule is only a limit if it counts the orders placed against it. The obvious implementation reads the year off `Country.CalendarYear`. That would have been wrong, and wrong in exactly the case §544 opened this cap for: `SimulationManager.cs:2752` calls the ministry with `PensionAgeStatute.SeedYear + CurrentTurn + 1` - the year **about to be played** - while the country still carries the year just finished (`CommitCalendarYear`). The ministry's orders would then have carried a year the tally never looked at, every call would have re-read an empty year, and **Germany's ministry would have been uncapped while the page showed a cap**. Every reader therefore takes the year as an argument - the year the order will CARRY - and `EnergyFleet.Place` passes its own. The class note states this and names the line that forces it.

## 3 · Four defects - three found by this review before the bar, one by the item's filmed width after it

1. **The page line would have overflowed, the §553 way.** The first cut built Germany's drawn source line out of `Publisher + Whose + AsOf + Holds` - about **300 characters** in a lane whose longest line already drawn is the USA's **155** at 1280. Measured, not eyeballed. Fixed: a statute's drawn line is the law and what it holds (`EEG 2023 § 28, § 28a · A STATUTE, NOT AN OPERATOR'S QUEUE · WHAT MAY BE AWARDED A CONNECTION IN A CALENDAR YEAR · ONSHORE WIND AND GROUND-MOUNTED SOLAR ONLY`, 155), and `Whose`/`AsOf` stay for the record, the diagnostic and the refusal sentence. The dry film would have caught this; it should not have had to.
2. **The convention's boundary was read off the wrong field.** `LastNamedYear` first took the schedule's last **from**-year - 2024 for wind, 2025 for solar - so the page would have told the player the statute stops speaking in 2025 when § 28 names volumes **through 2028**. Fixed by giving each volume a `Through` (the last year the statute names it for); the diagnostic now asserts each entry's `Through >= Year` and that entries neither gap nor overlap, so a schedule that lied about where the law stops would fail the bar.
3. **A stale claim in the film driver.** `UiScreenshotDriver.cs` said *"Where the capacity is billed (Germany) no step is ever refused: nothing to film"*. False as of this change: Germany's solar line is now the EEG's annual volume and the steps fill it, so the queue-full shot is filmed for Germany too - in the statute's own words. Recut.

4. **The convention was disclosed for the ROW, and one statute's lines stop in different years.** Found by the filmed width, not by this review: the Germany frame is in **2029**, where § 28a still names solar's volume but § 28 stopped naming wind's after 2028 - and the page said nothing, because the first cut appended one row-wide clause only past the LAST line's year (2029). Wind ran a year on the convention unannounced. Fixed: each line carries `NamedThrough` and the disclosure is ON THE LINE (`WIND 0 OF 10 000 MW (2028'S VOLUME, BY CONVENTION)`), from the year after that line's statute stops; `LastNamedYear` is gone rather than kept beside it. The diagnostic now asserts, per line, that the year the statute names reads clean and the year after ends with the mark. **The lesson is the record's own: verify captions on film** - the diagnostic tested the row-wide clause exactly as written and passed.

## 4 · What a reader must accept on the record, not on a document

⚠ **Past 2028 (wind) and 2029 (solar) the figure is a CONVENTION, not the statute.** The law names volumes for the years it names; this game runs a century. The last volume stands on, and the alternative - no limit at all past 2028 - would be authored too, and a looser claim. Three things keep this honest rather than hidden: `CapMwIn`'s doc states it, the diagnostic asserts the behaviour in both directions (past the last year and before the first), and **the page discloses it on the line it applies to, in the years it applies to** - wind from 2029, solar from 2030 (defect 4). The diagnostic asserts both halves per line.

⚠ **A statute's year is the calendar's; the game's turn is 365 days.** The turn drifts against the calendar by about a quarter-day a turn, so over a century two turns can report one calendar year (sharing that year's volume) and another can be skipped (its volume never drawn). That is the statute read as written rather than a per-turn allowance invented to be tidy. Disclosed in the class note; the page prints the year it counted.

⚠ **The layer has ONE wind label, so the onshore volume bounds every German wind order.** § 28 is onshore; the WindSeeG's offshore auctions sit outside it, and the game cannot place an offshore order apart from an onshore one. The line is therefore TIGHTER than the law - the understating direction - and it is the same treatment Sweden's row has had since §551 (Svenska kraftnät's queue excludes offshore wind, and its line bounds all wind). The page says ONSHORE on the row; splitting the label is a layer change, not this item's.

## 5 · What this change does NOT do

It does not move money and it does not move the sentinel. `AiEnergyMinistry.Live` is **false** (HELD since §544), so no AI state places fleet orders in a run; the cap binds the player's own orders and the ministry's diagnostic. No save field is added - `Order.OrderedYear` was already serialized, so a loaded save's German orders count in their own year with no format bump. `StandingMw` is **gone**, not kept beside `TakenMw`: one counting rule, because two would have drifted.

## 6 · Evidence

- `dotnet build` on both generated projects, 0 errors 0 warnings (the loop's compile check, ~6 s against a 23 s Unity launch).
- `Tools/textcheck`, **5 of 5 clean in 1.09 s**.
- `EnergyMinistryDiagnostic.QueueHoldsWhatIsPublished`, rewritten for the two kinds: for every line of every sourced country the whole figure stands and a megawatt more is refused in the source's own words; a retirement takes and gives no room; the second world's line is empty; and then **the kinds part** - an operator's landed order frees its megawatts in the same year, a statute's frees nothing in its own year and the next year's volume is whole at its own figure and refuses a megawatt past it.
- The cheap bar and the simulation bar of this commit, with the scoped dry film declared in `Tools/film_scope.tsv`.

**Noted, not fixed here (tooling, §574's):** `Tools/textcheck/TextCheck.csproj` sets `BaseIntermediateOutputPath` after `Microsoft.Common.props` is imported, so MSBuild warns MSB3539 and NuGet still writes `Tools/textcheck/obj/` **inside** the repository. Only text files land there and the path is gitignored, so no check fails - but §574's note claims the build lands outside the repository and that is true of `bin`, not `obj`. A `Directory.Build.props` would make it true of both.
