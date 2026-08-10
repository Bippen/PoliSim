# PoliSim — Pass 3 revision request

**Status: OPEN — nine items with Design, four blocking.**
**Date:** 2026-08-10.
**From:** the eight screen boards (Claude Design project `b3dec27b`, `PoliSim v2 Screens.dc.html`).

⚠ **GENERATED FILE — do not edit.** The body below is §1D of `CLAUDE_DESIGN_ASSET_REQUEST.md`, which is
the **source of truth**. This mirror exists so the request arrives as a new file rather than as an
in-place overwrite of a document a week old, which is why nobody found it the first time.

Regenerate the body after any edit to §1D:

```sh
awk '/^## 1D\./{f=1} /^## 2\./{f=0} f' CLAUDE_DESIGN_ASSET_REQUEST.md
```

Drift check — the two must report the same hash:

```sh
for f in CLAUDE_DESIGN_ASSET_REQUEST.md REVISION_REQUEST_PASS3.md; do
  awk '/^## 1D\./{f=1} /^## 2\./{f=0} f' "$f" | sha256sum
done
```

---

## 1D. REVISION REQUEST — the eight screen boards, 2026-08-10

**The boards are good and most of this document is now answered by them.** The surface ladder, the ink
set, the rule-weight vocabulary and the letterspaced-caps section head give the idiom a spine that
survives at data density. Two things in particular are better than what was asked for:

- **The in-row slider.** Standing value as a hard `2px` tick, draft value as the knob, and the span
  between them hatched in `#BE8A00` — **in both directions**, so a cut reads as clearly as a rise. That
  is behaviour 1 rendered as *distance* rather than as decoration, and it is the strongest single idea
  in the pack.
- **The dual-siting answer.** Unambiguous, and it resolves the constraint §1 flagged: plate, frame and
  title band ship as separate sprites from the interior furniture, so the embedded path skips three draw
  calls rather than needing a second design.

Both measured architectural constraints hold. No board interleaves the two renderers, and 1e states the
render-order finding in its own words. **§1D.4 proposes a wording fix to one sentence that contradicts
this**, and it is a text fix rather than a design change.

What follows is **nine items that are Design's calls, not ours to resolve unilaterally.** Four block
implementation. Each is stated with its evidence rather than as a preference, and two arrive with a
proposed answer rather than only a question.

### 1D.1 — The four blockers

#### D1 ⛔ The draft marker glyph does not exist in any font the game ships

The boards make `✎` (U+270F) the primary carrier of **behaviour 1**. It appears on all four IMGUI boards:
the `✎ 3 DRAFTS OPEN` header, the `STANDING ✎ DRAFT` column header, every drafted row (`22,0% ✎ 24,5%`),
both ledger subtotals, the bill rail's three figure rows, and the `✎ DRAFT — NOT ENACTED` stamp.

Measured against the three fonts actually in `Assets/Resources/Art/UI/Fonts/`, by reading their `cmap`
tables directly:

| glyph | Pagella Regular | Pagella Bold | Courier Prime |
|---|---|---|---|
| `U+270F` ✎ pencil | **absent** | **absent** | **absent** |
| `U+270E` ✎ pencil (lower) | **absent** | **absent** | **absent** |
| `U+26A0` ⚠ warning sign | **absent** | **absent** | **absent** |
| `U+25C4` ◄ pointer | **absent** | **absent** | **absent** |
| `U+25B2` ▲ / `U+25BC` ▼ | present | present | **absent** |
| `U+2212` − / `U+00B1` ± | present | present | present |

This is **behaviour 11's failure mode landing on top of behaviour 1** — *"a font or glyph set lacking it
renders a blank box on a readout the player is meant to trust."* Shipped as drawn, every draft marker in
the game renders as `□`, on the one cue that may change form but may not become nothing.

Behaviour 11 itself still holds: `U+2212` and `U+00B1` are present everywhere and the boards use them
correctly. The regression is only in glyphs introduced after that audit.

✅ **PROPOSED RESOLUTION — please confirm or override.** `icon_pencil_draft.svg` already shipped in pass 1
and is still unwired. **Make the draft carrier that sprite rather than a text glyph**: white-on-alpha,
tinted `#BE8A00`, drawn inline before the draft figure. This costs nothing new, removes the font
dependency permanently, and is consistent with the pack's own tint rule. If a typographic mark is wanted
instead, it must be one Pagella actually carries — but the sprite looks like the better answer.

Two riders either way: **▲/▼ are safe in Pagella but must never be set in Courier Prime**, and `⚠` needs
replacing wherever it appears in shipped UI copy (it is fine in board annotations).

#### D2 ⛔ The division bar depicts a quantity the simulation does not compute

The boards' central legislative visual is a seat headcount: `PASSES · 186 – 164`, a bar filled 53.1% aye
against nay meeting at a threshold tick, and `aye 186 · 176 to pass · margin 10`. It carries 1b's bill
rail, 1c's division records (`212 – 138`) and 1g's `DIVISION No. 215`.

`ParliamentSystem.GetSeatWeightedAlignment` documents the opposite, in its own comment:

> *"Worth understanding before displaying it: this is NOT a headcount, and there is no seats-based
> majority threshold anywhere in this model. Each party contributes its seat share…"*

What the model does produce, and what `DrawBillLiveEstimate` renders across all five screens today, is a
direction label, a WOULD PASS / WOULD FAIL verdict, and a **diverging lean bar** of that alignment. That
renderer's comment records the choice as deliberate:

> *"Deliberately not `PoliSimWidgets.SupportBar` — this model has no seats-based majority for it to draw…
> the Parliament card already shipped that exact bug once."*

So the boards ask for a re-run of a bug this codebase already found, fixed and wrote down. **The same
applies to 1b's per-row `VOTES` column** (`−9`, `+6`, `−12`, `+4`, `N/A`) — per-instrument legislative
support does not exist at any granularity; bills are scored whole.

**Two ways out, and the choice is Design's:** give the diverging alignment a period-correct treatment —
it is a real quantity, it is what the vote turns on, and a ledger has perfectly good ways to draw a lean
— or tell us the headcount is worth building in the simulation. The first is cheap and honest. The
second is a simulation change, not a UI one, and would need Elias's sign-off separately.

Note that **seat counts themselves are real** (`Country.ParliamentSeats`), so 1c's government/opposition
bar and all of 1h's election figures are fine as drawn. It is specifically the *per-bill division* and
the *per-instrument vote* that have nothing behind them.

#### D3 ⛔ The density board tested half the density

1b is captioned *"the density stress test: 19 live line items"* and draws 11 tax rows and 8 spending
rows at `44px`. The actual data model:

| | board 1b | actual |
|---|---|---|
| `TaxType` | 11 | **13** |
| `SpendingCategory` | 8 | **29** |
| `WelfareProgramType` | — | 6 |
| `InfrastructureType` | — | 4 |

W1's own argument used the right figure — *"Budget has ~40 rows"* — and then the board drew nineteen.
29 spending rows at 44px is **1276px of content in a column roughly 800px tall.**

Three things follow, all needing a decision:

1. **Row height comes down, or the column scrolls.** Both are legitimate; they produce different
   designs.
2. **No board draws a scrollbar anywhere** — despite §1B.1 establishing 16 scroll views and pass 2
   delivering six sprites for them. The thumb's width, its inset from the paper edge, and whether the
   channel is recessed into paper or desk are unspecified on every screen that scrolls.
3. **1b shows revenue and appropriations side by side while the sub-tab row highlights `Tax`.** The
   implementation shows one category at a time (`DrawBudgetProcessTab` → `BudgetProcessCategory`: Tax,
   Spending, Welfare, Infrastructure, SWF). Either Budget is meant to abandon its sub-tabs for a
   permanent two-column ledger — which the row arithmetic above makes impossible — or the board is a
   composite. Please say which.

#### D4 ⛔ Four data visualisations were never aged

`UiPalette.GetCategoricalColor` is still `Color.HSVToRGB(hue, 0.65f, 0.9f)` walking a golden angle —
saturated screen colour, untouched by the v2.0 pass. It draws:

| call site | series length |
|---|---|
| `HemicycleRenderer` seats **and legend** | 4 parties |
| sector employment pie | 8 sectors |
| **spending pie** | **29 categories** |
| tax revenue pie | 13 types |

The boards draw hemicycle seats in aged inks (`#9C4238`, `#62579F`, `#A8842E`, `#35619E`), so the design
**assumes an aged categorical set exists.** None was delivered — `polisim_palette.json` covers eleven
area hues and four semantic colours and is silent on categorical series.

This is Elias's eleven-hue ruling one level down, and by its own stated reasoning:

> *"colour is load-bearing wherever it also keys a data visualisation… A seal, emblem or typographic mark
> cannot substitute there, because the mark is not what the chart is drawn in."*

The eleven were kept as a floor for exactly this. These four charts are the same case — and **29 mutually
distinguishable aged hues is a materially harder problem than eleven**, which is why it needs Design
rather than a runtime desaturation we invent. Left as-is, the Statistics tab renders a bright HSV
rainbow on aged paper: a more visible break than the grey scrollbars §1B.1 worried about.

If 29 distinguishable aged hues is not achievable — a defensible answer — then say so, and the spending
pie needs a different chart form rather than a worse palette.

### 1D.2 — Five smaller items

#### D5 ⚠ Party inks are the area inks, on the same screens

| party | ink | already means |
|---|---|---|
| National Labor Front | `#9C4238` | CrimeJustice — **and semantic `bad`** |
| Reform Union | `#62579F` | Sectors |
| Agrarian League | `#A8842E` | **Political** |
| Centrist Coalition | `#35619E` | Fiscal |

On the Politics tab the tab's own ink is `#A8842E` and the Agrarian League swatch is `#A8842E`. On 1c the
`majority of 1` warning prints `#9C4238`, the same ink as the largest party's seats two rows above.

This is the defect §1B.5 just resolved for draft amber and Political, arriving from another direction:
two load-bearing meanings sharing one hex. Behaviour 9 requires a legend swatch to match *its own arc*;
it does not require the arc to match an unrelated area accent.

Related and cosmetic, but it has to be re-keyed: **the board's party names are invented.**
`PartyArchetype` is `ProgressiveAlliance · ConservativeUnion · CentristCoalition · NationalistFront`, and
`emblem_party_*` sprites exist for those four. Only Centrist Coalition matches.

#### D6 ⚠ A third hue tint is used but not delivered

Inactive tab swatches use a knocked-back tint that is in neither the `ink` nor the `lifted` table and not
in `polisim_palette.json`. Six of the eleven appear on the boards — Fiscal `#3D6494`, Political `#96762A`,
Labor `#A2653E`, CrimeJustice `#8E4A40`, Sectors `#5B5187`, Global `#4E7291` — and five do not, with no
stated derivation to compute them from. **Either the five missing values, or the rule that produces
them.**

#### D7 ⚠ Behaviour 4 is satisfied in the sprites and broken in the layout

§1C.2 is right that no *sprite* is a fixed text plate. At the **layout** level, 1b fixes the instrument-
name column at `168px` and clips overflow, and the stat-tile labels are set not to wrap.

Program names are longer than that at these sizes, and cabinet-minister names are generated at runtime.
*"A clipped number is a plausible wrong number"* applies to a clipped label too: `Veterans Benefits
Mandatory` clipped to `Veterans Benefits` is a different programme. **Please confirm every fixed-measure
text cell shrinks to fit rather than clipping**, and note where that changes the ledger's column widths.

#### D8 ⚠ Behaviour 6 is stated backwards between the two documents

§1C.2 reads: *"published = printed bulletin (solid frame + ref period + date + badge chip); live = desk
reading (dashed rule, unbadged)."*

Board 1a draws the opposite — the **dashed**-bordered block is the one carrying the `PRELIMINARY` badge
and the publication date, while the live desk readings sit on solid plates under a `DOMESTIC BULLETIN —
DESK READINGS, LIVE` caption.

**The board's version is arguably the better one** — a dashed rule reads as *provisional*, which is what
preliminary means. But two documents now state opposite rules for the same behaviour, and this is
precisely the behaviour where getting it backwards stays invisible until a player trusts a wrong figure.
One of them has to be struck. Please say which.

#### D9 ⚠ Eight sprite names in the captions have no file behind them

`ui_event_card` · `ui_status_ok` · `ui_stamp_holds` · `ui_stamp_verdict` · `emblem_state_seal` ·
`canvas_folder_country` · `canvas_btn_brass` · `canvas_btn_paper`

The first four have plausible substitutes in the delivered pack, and we will use them unless told
otherwise: the event card as a tinted `ui_panel_paper` with a drawn left rule, and the stamps as tinted
`ui_stamp_carried` / `ui_stamp_rejected`. **The last four do not** — the Canvas path has no button or
folder art at all, which re-opens §1B.3 after `CANVAS_SPEC.md` appeared to close it. 1f and 1g both
depend on them.

### 1D.3 — The locale decision nobody has taken

Every board sets decimals with a comma (`$29,3T`, `4,38%`, `−$0,51T`) and dates in Swedish
(`12 maj 2026`, `14 november 2027`), while all UI copy is English (`Send to the floor`, `Open dossier`).

⚠ **This is not an art-direction choice and it should not be settled in art.** `UiFormat` pins money to
`InvariantCulture` on purpose, and its doc comment names this exact string as the reason:

> *"Money renders in InvariantCulture, and deliberately so. This machine's locale is sv-SE, so the first
> version of this class produced `"$29,0T"` — a Swedish decimal comma against a US dollar sign… a
> locale-dependent formatter cannot have a fixed-string regression test, which this function above all
> others needs. Note the project's own history here — the "9,3" incident was a comma-decimal figure
> clipped in a narrow rect — so the separator is not a cosmetic detail."*

Board 1f prints USA GDP as **`$29,0T`** — character for character the string that comment names as the
bug. Elias's machine is sv-SE, so this is the development environment leaking into the boards rather
than a decision anyone made.

**No change is needed from Design here**; it is flagged so the boards are not read as settling it. The
separator belongs to `UiFormat` and behaviour 3, and the date format belongs beside it. If a Swedish
locale is genuinely wanted as a product decision, that is Elias's call and a much larger piece of work
than the boards imply.

### 1D.4 — One sentence to change, in this document rather than in the art

§1C.3 says `ui_banner_hold` *"survives the whole sequence"*, and 1e's caption repeats it. Read against
1e panel 3's own `IMGUI LAYER SUPPRESSED`, that sentence describes IMGUI drawing over a live Canvas
screen — element-granularity interleaving, and the exact thing the render-order spike ruled out.

✅ **PROPOSED RESOLUTION — a text fix, not a design change.** Board 1g already does it correctly in the
art: the banner is drawn *by the Canvas screen*, pinned to the bottom edge (`rgba(20,16,11,0.9)` on
`1px #3A2F1E`, padding `10/24`). So the rule should read:

> **Every Canvas takeover redraws the hold banner itself.** The IMGUI banner does not persist across the
> hand-off — it cannot, because IMGUI is suppressed from t=180ms. Time-hold state is never invisible
> because both sides draw it, not because one side survives.

That preserves behaviour 8 exactly and keeps screen granularity intact. **One knock-on: 1h omits the
banner entirely** — the one board that should carry it and does not. Please add it, or say why election
night is the exception.

### 1D.5 — What we are building meanwhile

Unblocked by all nine, and starting only once Elias has reviewed the existing chrome wiring live: the
surface ladder and ink set (already wired), the tab strip's three-state treatment minus D6's five missing
tints, the sub-tab and plate treatment, both status-line states, the dossier card and the generic stamp
treatment, the dual-siting build rule, and the envelope timings.

---

