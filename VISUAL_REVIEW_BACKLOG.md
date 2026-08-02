# Visual review backlog — REVIEWED 2026-08-02

**Elias reviewed all eleven items in one live session, playing as USA.** Results below. This file is no
longer a "never seen" list — it is now the record of what was seen and what came back.

## 🔴 Master Sequence step 5 does NOT close

Closure needs items 1–9 confirmed. **Items 3, 7, 8 and 9 all failed.**

**The `[DEBUG]` dump stays live** at `GameController.cs:2589` — item 8 has not passed.

## Results

| Item | Result | Status |
|---|---|---|
| 1. Statistics nav icon | ✅ PASS — *"it reads like an icon"* | **CLOSED** |
| 2. Statistics restructure | ✅ PASS — *"natural"* | **CLOSED** |
| 3. Published graph, empty state | ❌ **FAIL** — unit bug | **FIXED `628d78e`, needs re-review** |
| 4. Amber draft cue | ✅ PASS — *"says it is a draft"* | **CLOSED** |
| 5. Policy/Laws restyle | ⚠️ PASS with defect — *"trade is cut off"* | open, P4 |
| 6. Budget full-screen | ⚠️ PASS with defect — text above icons clipped | open, P4 |
| 7. First release + reporting lag | ❌ **FAIL** — graphs unreadable | open, P3 — **unblocked** |
| 8. Revision treatment | ❌ **FAIL** — graphs unreadable | open, P3 — **unblocked** |
| 9. Budget Process restyle | 🔴 **HARD FAIL** — black screen | **FIXED, needs re-review** |
| 10. B2 contextual stat row | ✅ PASS | ⚠️ see caveat below |
| 11. Credit Rating tile | ✅ PASS — placement confirmed | **CLOSED** |

---

## ✅ P1 — Item 9's black screen: root-caused and FIXED (`GraphRenderer`)

**It was an `OnGUI` exception, and it was mine — not the full-screen layout, not the pause banner.**
Found in `Logs/Editor.log` from Elias's own session, 2,309 occurrences:

```
IndexOutOfRangeException: Index was outside the bounds of the array.
  at GraphRenderer.SetPixelSafe          (GraphRenderer.cs:815)
  at GraphRenderer.DrawLine              (GraphRenderer.cs:794)
  at GraphRenderer.DrawSparkline         (GraphRenderer.cs:769)
  at PolicyScreenStatsRenderer.DrawChip  (PolicyScreenStatsRenderer.cs:143)
  at GameController.DrawBudgetProcessTab (GameController.cs:3893)
  at GameController.OnGUI                (GameController.cs:833)
```

**The defect.** `SetPixelSafe` bounds-checked against `TextureWidth`/`TextureHeight` — the full-size
graph's **300×90** — and indexed with a stride of 300. `DrawSparkline` passes a **72×20 = 1,440**-element
buffer. At y≥5 the index reached 1,500+ and threw; below that it silently wrote to the wrong pixels. An
exception inside `OnGUI` aborts the rest of the frame, which is exactly a black screen.

**This came from B2's own "reuse, don't duplicate" decision backfiring.** `5701a04` deliberately reused
`DrawLine` *"so a sparkline can't disagree with its full-size counterpart"* — right instinct, but the
shared helper was not dimension-agnostic. **Sharing the algorithm was correct; sharing the constants was
not.** Both helpers now take the buffer's dimensions.

**Why it reached a live session:** the only entry point was `DrawSparkline`, which calls
`GUI.DrawTexture` and therefore cannot run outside `OnGUI` — so nothing headless could ever exercise it.
The pixel maths is now `GraphRenderer.BuildSparklinePixels`, with no GUI dependency, and
`GraphRendererDiagnostic` covers it: **336 width×height×series-shape combinations, all passing**, plus the
exact 72×20 failing case.

### ⚠ This puts item 10's PASS in doubt — it needs re-review after advancement

Item 10 is Tier 0 (no advancement), so Elias reviewed it at turn 0. `DrawSparkline` returns early when
`history.Count < 2`, and quarterly history has fewer than two entries that early — **so no sparkline ever
rendered during item 10's review.** The same component then crashed the Budget tab at day 273 once history
had filled.

Item 10's chips, icons and layout are confirmed. **Its sparklines are not**, and they are the part that
failed. Re-review item 10 alongside item 9.

---

## Remaining work, in Elias's priority order

**P2 — Item 3, the unit bug. ✅ FIXED 2026-08-02 (`628d78e`), awaiting re-review.** GDP now renders
`$29.0T` everywhere, through one `UiFormat.Money(value, MoneyUnit)` whose unit is a **required**
parameter on every graph and pie-chart entry point — ~30 display sites, more than the 21 the
investigation enumerated. `MoneyFormatDiagnostic` passes 6 of 6 in real Unity. **What still needs
Elias's eye is item 3's original question**, which the fix does not answer: does a single-point
published graph with the "next release builds the trend" message read as *working* or as *broken*?
The brief below stands. The original finding, for the record: GDP `29000` rendered as **"29k"** for $29
**trillion** — unit-wrong, not arithmetic-wrong, and the third instance on this same value after
`StatTile`'s "9,3". Tiles showed `28999,3` raw and unlabelled, as did spending lines, tax revenue, SWF
assets and budget balance. **The game stated its units nowhere.**

**P3 — Items 7 and 8, unreadable graphs. 🟢 NOW REVIEWABLE — the P2 blocker is cleared.** *"hard to make
out any of the graphs what they are saying"* at one turn; *"still hard to tell"* at two. The axis no
longer misreports magnitude, so what remains is a genuine density/marker/readability judgment. **This is
now the top of the list.** Re-review 7 and 8 first; only treat residual unreadability as a design finding
after seeing it with correct units.

**P4 — Items 5 and 6, text clipping** (not started). Item 5: "trade is cut off". Item 6: text above the
icons clipped on Debt-to-GDP and similar tiles. This is the **label-measurement class already fixed at
least five times** (Manufacturing sector label, World Map country names, TaxLine/WelfareProgram rows,
Policy Web category headers). **Audit for the pattern rather than fixing two instances** — measure against
the style the text actually renders in, recompute per frame since sizes rescale with the window, leave
real margin. Worth asking whether a shared measured-label helper ends the class rather than a sixth
site-specific fix.

---

*Original per-item review briefs follow, retained for the items still open.*

---

# TIER 0 — no advancement needed (items 1–6, plus 10 and 11 at the end of this tier)

## Items 1, 2, 4 and 11 — CLOSED, briefs moved to `COMPLETED.md` section 14

Confirmed by Elias 2026-08-02. Removed from this file per the standing three-way test: finished work
lives in `COMPLETED.md`, and this file holds only what is still open.

## 3. Published-graph redesign — initial state only

**What / where:** `dd7e323`. Full-width published-series graph with provenance overlay.

**Look at:** Statistics → Domestic, at turn 0. You will see the single inherited quarter and the message
*"…for [period]. Next release builds the trend."*

**The judgment:** does a single-point graph with that message read as *"the system is working, data is
coming"* or as *"something is broken / empty"*? This is a new player's first impression of the entire
publication mechanic, and it lasts until the first release lands.

**If rejected:** just this panel's empty state. Does not block the revision review in item 8 — that is a
different visual on the same widget.

## 5. Policy / Laws tab restyle

**What / where:** Phase C batch 4. Marked in the roadmap as **built, never live-confirmed**.

**Look at:** the Policy/Laws tab, on load.

**The judgment:** does it match the visual language batches 1–3 established on the tabs you already
approved? Consistency across tabs is the whole point of the sprite rollout, and drift is only visible
side by side.

**If rejected:** isolated to one tab.

## 6. Budget tab full-screen

**What / where:** `2909d30`, built to your directive *"from now on i want the budget tab to be opened up
to cover the full screen in order to give a better overview."*

**Look at:** click the Budget tab (Tax and Spending are merged into it now).

**The judgment:** does full-screen actually *deliver* the overview you asked for, or does it just make
one tab behave differently from every other and feel jarring? You specified the mechanism; only you can
say whether it achieved the goal. Also worth checking: the merged Tax+Spending sub-tabs still make sense
at full width.

**If rejected:** isolated. A layout flag.

## 10. B2 contextual stat row — the one thing here that can be *wrong*, not just ugly

**What / where:** `4869476`. Added 2026-08-02, numbered 10 rather than renumbering 1–9, which are
referenced from the roadmap. It is a **Tier 0** item despite the number — it
is on screen the moment you open either tab, no advancement needed.

**Look at:** Policy/Laws → each of Labor Market, Crime & Justice, Economic Sectors, Trade. Then
Budget → each of Tax, Spending, Welfare, Infrastructure, Sovereign Wealth Fund.

**The judgment — two separate questions, and the second matters more:**

1. *Does it look right?* Chip spacing, sparkline legibility at 72×20, whether the icons read at 22px, and
   whether the row competes with the bill card underneath it instead of introducing it.
2. **Are the stats on each screen the ones that screen's levers actually move?** This is a correctness
   question wearing a visual disguise. The list is derived from the Policy Web's edge list rather than
   authored, so a wrong entry means a wrong *edge* — a claimed policy→stat relationship that isn't real.
   That is worth catching here, because the same edge list drives the Policy Web itself.

**Two expected results that are not bugs:**

- **Infrastructure shows no row at all.** No Infrastructure policy node has a single Policy Web edge. The
  gap is real and pre-existing; the row will appear on its own the day an edge is added.
- **Policy Web shows no row**, deliberately — it *is* the full edge list, so a 4-stat summary above it
  would be a worse view of the same data.

**Also check:** Tax and Spending show the *same* four stats. That is intended — both run through the same
two channels — but if it reads as a bug to you, say so, because it will read that way to a player too.

**If rejected:** isolated to `PolicyScreenStatsRenderer` and two call sites; nothing else consumes it.

## 7. First published release + the reporting lag

**Look at:** Statistics → Domestic. Q1 2026 closes 31 March; its advance estimate fires ~30 April, so
**one turn is enough** to see the first real published point appear, plus the date axis and release
markers (`f1996e1`).

**The judgment:** is the *lag itself* legible? A player should be able to see that this figure describes
January–March but arrived in April, and understand that without reading documentation. If the date axis
and the reference-period labelling do not make that obvious at a glance, the entire Step A mechanic is
invisible to the player and the work does not pay off.

**Also:** monthly inflation publishes ~4 times per turn, so the inflation series will have several
points by now — a good check on whether the marker density is right.

**If rejected:** affects the axis/marker design in `GraphRenderer`, shared with item 8. Redo, and it
would be worth doing before item 8's ghosts are judged, since they sit on the same axis.

---

# TIER 2 — two turns (ends day 242 = 2026-08-31)

## 8. Revision treatment — the core of `dd7e323`

**Look at:** Statistics → Domestic, GDP graph. By day 242, Q1 2026 has published **three times**:
advance ~30 Apr (Preliminary), second ~30 May (Revised), third ~29 Jun (Final). Q2's advance has also
landed ~30 Jul.

**What is on screen:** filled vs hollow markers by revision status, a **ghost of the superseded value**
with a connector line to the value that replaced it, and markers drawn larger than the line thickness.

**The judgment — the single most important one in this backlog.** Can you look at that graph and
immediately see *"this number was revised, here is what it used to say, here is what it says now"*? The
whole point of Step A is that a player acts on a preliminary figure and later discovers it moved. If the
ghost reads as clutter, or as a second data series, or is simply too faint to notice, the mechanic is
built but not *communicated*, and the player never learns the lesson the system exists to teach.

Secondary: with two quarters × three revisions plus monthly inflation, is the graph **busy**? This is the
first point where density becomes judgeable.

**If rejected:** contained to `GraphRenderer.DrawPublishedPointOverlay`. Nothing else depends on it. But
it is the payoff for Step A and B1 both, so a rejection means the most valuable visual in the queue needs
another pass — worth getting right rather than fast.

**Keep the `[DEBUG]` dump until this one passes.** It is still live at
`GameController.cs:2589-2592` and prints reference period, publication date, value and status per entry.
It is your cross-check that the picture matches the data. **Once you approve item 8, tell me and I will
remove it** — it should not ship.

---

# TIER 3 — three to four turns, country-dependent

## 9. Budget Process screen restyle

**What / where:** Phase C batch 5. Built, never live-confirmed.

**Getting there:** the screen only opens on the mandatory fiscal-year pause. **Playing as the USA:
1 October = day 273, reachable in 3 turns.** Playing as any European country: 1 January = day 365,
**4 turns**. If you are mid-review as a European country, this is the one item worth deferring rather
than fast-forwarding a whole extra turn for.

**The judgment:** does the restyle hold up on the most information-dense screen in the game? The Budget
Process screen already had two real layout bugs found in your own screenshots during 5b (a header
clipping mid-word, a catastrophically narrow preview panel). It is the screen most likely to break under
restyling, and the one where a subtle regression is easiest to miss.

**If rejected:** isolated to that screen, but it is the **last outstanding item in 5e** — the roadmap
records Phase C as functionally complete with only live confirmation pending. Rejecting it keeps
Master Sequence step 5 open. Approving items 1–9 closes 5e entirely.

---

# Explicitly NOT in this backlog

- ~~**B2 contextual policy-screen stats** (`3dcf038`) — data layer only.~~ **No longer true as of
  2026-08-02.** Rendering was built (`5701a04`) and wired (`4869476`); it is now review **item 10**.
- ~~**The 42 macro sprites** — imported, unwired, invisible.~~ **No longer true.** `IconLibrary` gained
  its Stats path in `5701a04` and item 10 is the first thing that draws them, so this is the review where
  the macro sprites become visible for the first time. **All 43 as of 2026-08-02** —
  `icon_stat_interestrate` was delivered and imported, so the Interest Rate chip now draws its icon like
  every other. ⚠ **This changes what item 10 shows**: it was reviewed with that chip's label sitting
  flush left where the missing icon would have been, so the row's spacing is not what was confirmed.
  Re-review it with the sparkline caveat above.
- **Sub-tab label fix** (`f5c25ac`) — you confirmed this one ("It looks better") before asking for the
  Budget full-screen change. Considered done.

---

# One-session route

| Order | Item | Advancement | Why here |
|---|---|---|---|
| 1 | Statistics icon size | none | cheapest, isolated |
| 2 | Statistics restructure | none | **gates item 3** |
| 3 | Published graph, empty state | none | needs 2 approved |
| 4 | Amber draft cue | none | drag a slider |
| 5 | Policy/Laws restyle | none | on load |
| 6 | Budget full-screen | none | on load |
| 10 | **B2 stat row** | none | after 5 and 6, since it sits *on* both those screens |
| 11 | **Credit Rating tile** | none | use USA, Italy or Poland - the other three thrash, already logged |
| — | *advance 1 turn* | → 2026-05-02 | |
| 7 | First release + lag | 1 turn | shares axis with 8 |
| — | *advance 1 turn* | → 2026-08-31 | |
| 8 | **Revision treatment** | 2 turns | the payoff; keep `[DEBUG]` until approved |
| — | *advance 1–2 turns* | → Oct 1 (USA) / Jan 1 (EU) | |
| 9 | Budget Process restyle | 3–4 turns | closes 5e |

**Note on save/load:** there is still no persistence (Master Sequence item 8), so closing Unity mid-review
loses the advancement. Items 1–6 and 10 cost nothing to redo; items 7–9 would need the turns re-run. Worth
doing 7–9 in one sitting.
