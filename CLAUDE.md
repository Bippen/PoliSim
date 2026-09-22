# PoliSim

> ## START HERE — the reading order for a session's start (ruled 2026-09-17, `COMPLETED.md` §524 (c), built §525; re-cut §579)
>
> 1. **Run the session brief:** `powershell -NoProfile -File Tools/session_brief.ps1`. It prints, read at the moment it runs: the branch against origin and anything uncommitted, whether Unity holds the project, the last commits, the last bars, the newest record sections with their line numbers, the residue the last bar printed, the open errands, the sentinel's baseline and the newest memory file. It stores nothing and decides nothing, so it cannot go stale; a source it cannot read, it names.
> 2. **Read this file.** All of it — it is ~26 KB and it is only rules. Every rule is one to three lines with a `§` pointer into `COMPLETED.md`, which is where its story is.
> 3. **Read the newest memory file, then the record sections the brief names** — each by its line number. `COMPLETED.md` is never read whole.
> 4. **Before every commit, `powershell -NoProfile -File Tools/bar_tier.ps1 -Staged`**, and run what it says the commit owes.
>
> ⚠ **Until 2026-09-22 this file was 1.39 MB and auto-loaded** — its own instruction not to read it through arrived after it was already in context, which is the efficiency review's unattributed 846–1,220 KB of session-start reading (§573, §579). The rules stayed; the explanations and the dated findings moved to `docs/` and are searched by name.

## Where everything lives

**Root holds five documents and no others:** this file, `POLISIM_FEATURE_LIST.md` (open rows), `ERRANDS.md` (open errands), `CLAUDE_DESIGN_ASSET_REQUEST.md` (the live ask), `COMPLETED.md` (the append-only record — never read whole, read by `§` section).

| under `docs/` | what is there |
|---|---|
| `reference/MODEL_REFERENCE.md` | how the model works — the economic theory, the fiscal accounting, the tax portfolio, the political layer, the Fed, the spending and tax-base sections. Searched by name |
| `archive/CLAUDE_LESSONS_2026-08.md` | the dated findings this file used to carry, verbatim, with the two notes on how to read their numbers |
| `archive/CLAUDE_HEAD_LONGFORM.md` | this head as it stood on 2026-09-22, before it was condensed — the long form of every rule below |
| `archive/DESIGN_REQUESTS.md` | every answered design ask |
| `specs/` | `ENERGY_SPEC.md`, `TAX_SPEC.md`, `ELECTIONS_CAMPAIGN_SPEC.md` — the source specs |
| `data/` | `SOCIETY_STATS.md` (the catalog and its five family spines), `ENERGY_LAYER_SPINE.md`, `COUNTRY_CENTROIDS.md` |
| `generated/` | written by tools, never hand-edited: `LEVER_MAP.md`, `BUDGET_PREMISE.md`, `ENERGY_LAYER_PREMISE.md`, `POTENTIAL_PREMISE.md` — each named with its tool in `docs/generated/README.md` |
| `play/` | `PLAY_PROTOCOL.md`, `PLAY_SHEET.md` |
| `outbound/` | `SEND_PACKAGE.md` — the working sheet for whatever paste is pending |

## ⚠ How to read a number quoted in the record

- **"N anomalies" is a 5-field check, not a health measure** (Elias's decision 2026-08-01, kept): `CheckFinite` covers every float, but `CheckSwing` and the range checks see only GDP, Unemployment, Inflation, InterestRate and DebtToGdpRatio. A runaway in any of the other 24 tracked values produces **zero** anomalies and a clean-looking run. The coverage table is in `docs/archive/CLAUDE_LESSONS_2026-08.md`.
- **Eight baseline discontinuities inside a month** mean a figure from one era cannot be compared with one from another; the widest is `DaysPerTurn` 121 → 365 on 2026-08-10, which changed what the simulation *was*. The dated table of all eight is in the same archive file. Before `f178263` a `-runmatrix` run silently ignored `-seed`.

# THE CLAIM CONVENTION — ruled 2026-09-01 (`COMPLETED.md` §190), governs every document AND every source comment

⚠ **It outranks everything below it.** It was ruled on a measurement: **1,009 of 6,209 live claim-lines asserted a fact about the code, 557 asserted nothing else, six were demonstrably false when measured, and not one was catchable by any check in the bar.**

> **The test: the code can change freely and no document becomes wrong** — only *incomplete*, and only where TRACKING has genuinely moved.

- Every claim is **INSTRUCTION** (what to do, what is ruled, what is owed — timeless), **TRACKING** (what is open, whose it is — stale only when the work moves), or **DERIVED** (a fact about the code or the environment: a name, a line number, a count, a figure, a hash, a path, a *"there are N of X"*).
- **A DERIVED claim has exactly three permitted destinations: GENERATED** (emitted by a tool into a marked block, re-derivable by one command), **REFERENCED** (a pointer to where the fact lives — *"the party count is whatever `BuildParties()` seeds"*, never *"53 parties"*), or **DELETED** (it was decoration; most counts in prose are).
- ⚠ **Nobody transcribes, anywhere, in any file.** `DocumentClaimCheck`, `CommentClaimCheck` and `PhantomGuardCheck` hold this; `COMPLETED.md` is exempt, because a record of what was measured on a day is not a claim about today.

# THE STANDING RULES

Each is one to three lines with the `§` its story lives under. The long form of every rule on this page is `docs/archive/CLAUDE_HEAD_LONGFORM.md`.

## What the model's numbers ARE

- **THE BOOK IS IN CURRENT PRICES** (P5-B6, §§ at 2026-09-05; supersedes R2 of 2026-08-17). The macro block is real, the book is nominal, and one price level joins them: `EconomyState.PriceLevel` is 1 at the seed and compounds daily at the Phillips curve's inflation. **Nominal with nominal, real with real, never across.** `GovernmentDebt` is nominal and carries the erosion term (−π·b, symmetric per R3).
- ⚠ **NUMERIC INERTNESS — a byte-inert instrument leaves the play path's expressions untouched** (ruled standing, §400, §402). Mono evaluates `float` intermediates at higher precision and rounds when a value is **STORED**, so refactoring one expression into locals rounds twice where it rounded once: that is a numeric change with its own family, its own dump diff and its own explanation. **Inertness is proved by the dump diff against the baseline, never by reading the code.**
- ⚠ **The IL-shape case** (EN-7b, §500, §501): inertness is about the shape of the STORE, not only about locals. A dead block writing the same local a second time moved the trajectory — even reduced to `if (false)`. What held was one expression with the flow always summed (`x + 0` is exact at any precision).
- **A seed is held against a source in the source's own year and perimeter** (PN-4, §519): share against share, same year, or the gate is measuring the calendar rather than the model.

## What a pass must do

- **BARS ARE TIERED BY WHAT A COMMIT TOUCHES** (§524). `Tools/bar_tier.ps1` prints the tiers, the paths that decided each, and the runs owed (`-Staged`, or `-Commit <rev>`). **A commit owes every tier it touches.** Documents → the document batch; tooling → the cheap bar; simulation → the cheap and simulation bars; UI → the cheap bar, the dry film and one filmed width.
- **A UI item iterates on the DRY FILM** (§524): `UiScreenshotCapture.RunDry`, `-batchmode` play mode, one session per country per geometry — the same driver and choreography, no window, the guards measuring instead of photographing. ⚠ Never run it without `-batchmode`. It does not claim pixels: the edge guard, the identity token and the frame-size traps need a real film.
- ⚠ **THE DRY FILM'S SCOPE IS DECLARED BEFORE IT RUNS** (§574): `Tools/film_scope.ps1` names the sessions the item asks for, and `DryFilmScopeCheck` fails the cheap bar on a film that ran a session the row does not name — both directions. The sitting pass ran twelve sessions on every item and measured the cost: 522 s a run against 145 s for the two an item named.
- ⚠ **A DRY FILM CANNOT CLAIM CANVAS TEXT — A CANVAS SURFACE OWES A REAL FILM** (§578): no window means a 640×480 backbuffer whatever geometry is named, and the guard says it asserted nothing. A UI commit touching `SigningScreen`, `ElectionNightScreen`, `CountrySelectorScreen` or `CanvasChrome` owes a REAL film that reaches it (the tier names the shots), and **a track's close films every Canvas surface real at 1280 and 2560**. Twenty-five days of clipped captions were drawn on every passed law before this rule existed.
- ⚠ **A MONEY PATH GETS ITS REVIEW, AND THE BAR SAYS SO** (§546). `Tools/bar_tier.ps1` defines a money path; `ReviewLedgerCheck` fails the cheap bar until the review has run, its report is under `Reviews/` and `Tools/review_row.ps1` has added the row. Rows go in BEFORE the bar. A baseline that moves needs a reviewed row for both digests.
- **ONE GREEN BAR PER COMMIT, NO EXCEPTIONS** — green for the tree being committed, not "green when I last looked". ⚠ The bar cannot run while the Unity Editor holds the project; a bar run means the Editor is closed first (`Tools/warm.ps1 -Stop`).
- ⚠ **A wrong invocation and a right one look identical in their output** (§190 §B.3). A bar can pass in seconds having run nothing; a capture can exit 0 having filmed the wrong screen. **Check what a run actually did, never that it merely exited.**
- **RECORDS: ONE COMMIT PER PASS, AND THE SHORT FORM** (§574, measured): a `§` record is **what changed · the evidence line · the commit**, and anything longer has to earn its lines.

## The loop, and the instruments

- **THE LOOP** (§573, measured): edit → `dotnet build` on the generated csproj pair (≈5 s, no Unity) → a TARGETED check (`CheckSuite.RunNamedBatch -checks=A,B,C`, 25 s; `MultiRun.Run -methods=…`, 79 s for three) → edit again. At the item's close, CHEAPEST-FAILING FIRST: cheap bar → the scoped dry film → one filmed width → the simulation bar.
- **A WARM EDITOR SERVES THE RUN, NOT THE EDIT** (§575, measured): `Tools/warm.ps1 -Start`, then `-Command "bar all"` / `"bar sim"` — a bar in a host that survives it (`CheckSuite.RunTableNamed`; the batch entries end with `EditorApplication.Exit`, which `CheckExit.Collect` cannot suppress). Host up 12–35 s, cheap bar 25–36 s, simulation bar ~10 min, both from one launch. ⚠ **It does not recompile**: after a source edit, `-Stop` and `-Start`; stop it before any film.
- ⚠ **TOOLING A PASS DEPENDS ON LIVES UNDER `Tools/` AND IS COMMITTED** (§576): the scratchpad is for what a pass throws away. `warm.ps1` lived there and a session restart took it while the host it started kept answering.
- **The bars read the tree once and run the no-policy centuries once** (§525): a check that reads source reads it through `SourceText`; `DeadStateCheck` tallies every declared name in one pass; a diagnostic needing a no-policy century reads `NoPolicyCentury.For(country)`.
- **The document checks run OUT of the engine** (§574): `Tools/textcheck` compiles the same source files under shims and answers five of the eight in ~1.2 s, against a 23 s document bar. It is a loop instrument, not the bar.
- **A digest certifies sameness, not sanity** (FT-10, §541): `TrajectorySentinelCheck` also runs a century and reads the energy layer's own bounds, because a baseline can be reproducibly wrong.
- **THE FAMILY DIFF** is `Tools/traj_diff.pl <old> <new> [seed] [turns]` (§577): per country, how many fields moved and each mover old → new with its per-cent, largest first.
- ⚠ **A binary artifact reaches this repo as a FILE, never retyped through a conversation** (ruled standing, §424, §427): four of eight PNGs carried through a context arrived corrupt with valid signatures, right dimensions and plausible sizes — the damage was inside the compressed data. To check one, walk its own structure and recompute its own checksums (`Tools/pngcheck.pl`). Small text is a different case and stays allowed; anything carrying figures is generated, not transcribed (§419).
- ⚠ **NO SHELL STATE ACROSS TOOL CALLS**, and **every file edit is one self-contained operation matched on TEXT**, never on a line number or a walked position (§190 §B.3). Prefer a direct file write to a heredoc for prose.

## What the game holds, and what reads it

- ⚠ **The game holds several worlds at once** — the played one, the shadow baseline's, the impact ledger's forks. A table keyed by a country's id and written at run time is shared between them; state that belongs to a country rides the `Country` (§544's class).
- **The preview's clone is audited by field, not by memory** (§506): `ClonePreviewCountry` is a hand-list and a hand-list drifts — it was found short by twenty-seven value fields at once. `PreviewParityDiagnostic` marks every value field and reads each back through the clone, so a field added to `Country` and not to the list fails the simulation bar by name.
- ⚠ **The live game's campaign calendar is the next election turn's boundary, not a country's own dates** (`SimulationManager.CurrentCampaignCalendar`; CL-5 ruled §579). `CampaignCalendar.Sweden2026` is the REAL election's calendar — the play protocol is cut on the game's own (`PlayProtocolStaging`, which prints the three dates every run).
- ⚠ **A check that loads a save into a MANAGER has not loaded it into the GAME** (§557): the protocol's staging passed for weeks while the player's own Load failed.
- **A NAME carries its diacritics; a STEM is ASCII** (§566), **and a KEY is ASCII while a SHORT NAME is the authority's own** (§575): `PoliticalParty.Abbrev` is the persisted key — saves, fixtures, seat tables, ink and mark tables, asset stems — and `PoliticalParty.ShortName` is what a player reads (the Bundeswahlleiter's `GRÜNE`), defaulting to the key. A site that DRAWS an abbreviation takes the short name; every lookup keeps the key.
- **A number's locale is the SURFACE's, not the call site's** (§568): `UiCulture.Install()` sets the UI thread to the machine's culture with the INVARIANT number format, so every interpolated figure reads with a point, while DATES keep the machine's culture. `NumberLocaleCheck` holds it.

## What the screens do (the standing UI notes)

- **A control that draws as a bare sentence gets the chrome's face** through `CanvasChrome.FacedButton` — brass command, stock neutral (§527).
- **A glyph in a slot is drawn through the LABEL style, never the tab style**, and `LedgerRow.CellStyle` keys its cache on source **and** size, or two sizes share one style (§534).
- **A call to action is a sentence and the family's one-width button** (`DrawSentenceAction`, §564): it lays its sentence out WRAPPED on the row's two lines at full size before letting `MeasuredLabel` shrink it — `MeasuredLabel` measures unwrapped and shrinks to one line, so a long sentence handed to it alone goes to the floor.
- **IMGUI hangs a slider's thumb from its rect's TOP** (§565): a knob taller than its track overhangs downward. `LedgerRow` centres the knob by painting the sprite through the thumb style's `overflow` while the thumb's RECT stays the track's height — paint moves, geometry does not.
- **An end-named dial's caption band holds some sixty characters at 1280**, and a range caption fits in what is clear of the END-NAMES as drawn, hanging on their line (§550, §559).
- ⚠ **A vertical layout group compresses toward its children's FLOOR** when they ask for more than the panel has (PF-13, §578): a wrapped Canvas caption needs a floor of its own, and the container's height is not the place to fix it — tuning one until a country's chamber fits is §432 repeating.
- **A Canvas `Text`'s `preferredHeight` is computed at the rect's CURRENT width** (§578), so the first layout pass measures against a width the horizontal pass has not settled: resolve the layout twice.

# THE WORKING DISCIPLINE

Migrated from `POLISIM_MASTER_ROADMAP.md` at its retirement (2026-09-01) and condensed here (§579). ⚠ **This is INSTRUCTION.** Its history — the embedded corrections, the narratives, the instances that taught each rule — is `COMPLETED.md` §35 and §181, and numbered references to *"rule N"* across the record resolve against §35. The long form is `docs/archive/CLAUDE_HEAD_LONGFORM.md` and the feature list's own section.

- **Truth = real Unity, explicit project path, validation scaled to risk — and the risk is read off what the commit touches** (rule 1; the tiers, §524).
- **Measure before you propose, and propose before you build.** A finding is a measurement with its instrument named; a proposal without a figure is not a proposal.
- **Ruling-first on anything that is the owner's to rule** — a design call, a model specification, a figure with no source. Measure it, state the options with their costs, and stop.
- **STOPPING IS A RESULT.** A premise that fails is closed with the measurement that killed it, not worked around.
- **The three-way test every task gets:** is it the thing that was asked for; is the evidence the thing itself and not a proxy; would a reader with no memory of the session reach the same verdict from what is written down.
- **A source is fetched, not remembered** — and where it cannot be reached, the row is BILLED with what is missing and why, never estimated.
- **Nothing is deleted that has not first been migrated**, and a document that loses a line says where the line went.

## Tooling lessons that cost a pass

- **Tooling lessons, 2026-09-21:** the Design project's files are reachable from the MAIN session only (a subagent has no DesignSync - fetch first, hand it the file); a long Bash heredoc holding apostrophes, backticks or `\\n` inside quoted C# fails or silently rewrites the escape - write a long perl patch or record with the Write tool; never trust a session summary's *"the user said"* for a standing rule - go to the transcript (the commit attribution line, §555).
- ⚠ **A quoted heredoc still collapses `\` to `\`** in this environment (§577): a C# `"\n"` written through one becomes a real newline and splits the string literal. Build such text with `chr(92)`, or write it with the file-writing tool.
- **`grep -c` returning 0 exits 1 and breaks a `&&` chain**; `Select-String` is case-insensitive by default; there is no `python` here, and no PDF renderer — inflate FlateDecode streams with `perl Compress::Zlib`.

---

*Everything below `## Genre & Scope` used to live here. It is now `docs/reference/MODEL_REFERENCE.md` (how the model works) and `docs/archive/CLAUDE_LESSONS_2026-08.md` (the dated findings), both verbatim, both searched by name.*
