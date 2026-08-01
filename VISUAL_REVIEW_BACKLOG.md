# Visual review backlog — everything built but never seen

**As of 2026-08-01.** Nine items are in the "built, compiles, committed, never visually confirmed" state.
They are ordered so that **items 1–6 need no game advancement at all** — enter Play mode and they are on
screen. Item 7 needs one turn, item 8 needs two, item 9 needs three or four depending on country.

One session, one fast-forward, in order. Nothing below requires restarting.

---

## ⚠ First, a correction to the record

**I told you earlier today that no macro sprites had been delivered. That was wrong.** All 42 are
present and imported (`be97ebb`, `65be9ab`): 36 `icon_stat_*`, 3 trend arrows, 1 release marker, 2
revision badges, in `Assets/Resources/Art/UI/Stats/`. My check used the pattern `stat_*.png`, which does
not match `icon_stat_gdp.png` because of the `icon_` prefix, and I read the empty result as "not
delivered" instead of "bad pattern" — the same shape as the verification failures logged in `CLAUDE.md`,
committed while writing up that very class.

**They are imported but completely unwired** — zero code references, and `IconLibrary` has no Stats
path alongside its Icons/Chrome/Portraits ones. So they are not a review item yet; there is nothing to
look at. They are ready for whenever B2's rendering gets built. Step D is delivered, not blocked.

---

# TIER 0 — no advancement needed (items 1–6)

## 1. Statistics nav icon sizing

**What / where:** `b6da098` (Phase B pilot), resized in a follow-up after your "colored speck" feedback.
The nav icon sits stacked above the label on the Statistics tab button.

**Look at:** the tab bar, immediately, at **1.0x scale, 16:9**. No sub-tab, no state.

**The judgment:** does it read as an *icon* at a glance — roughly text height plus a bit — or is it still
decorative noise you'd never actually navigate by? You asked for "genuinely right rather than a token
bump," and only your eye at native resolution can say whether it cleared that bar.

**Honest uncertainty:** you later said "All the tabs are looking great" after batch 1 rolled icons onto
all seven tabs, which *may* have been the confirmation. I cannot tell from the repo whether that
covered the resize specifically, and I have no record of a 1.0x screenshot taken after it. Treat as
unconfirmed; it is the cheapest check here.

**If rejected:** isolated. A size constant. Nothing depends on it. Redo, not a block.

## 2. Statistics restructure — Domestic / International

**What / where:** `9713c60`. Sub-tabs went `{RecentTurns, WorldMap, Trade}` → `{Domestic,
International}`, Trade was absorbed into International, and **all graphs were removed from the left
column**.

**Look at:** Statistics tab → both sub-tabs. Also glance at the **left column on any tab** to confirm it
is numbers-only now.

**The judgment:** two questions your eyes must answer. (a) Does Trade sitting inside International read
as natural, or does it feel buried — you go looking for "Trade" and find it isn't a tab any more?
(b) Does the left column feel *cleaner* without graphs, or *emptier* — did removing them take away
orientation you were actually using?

**If rejected:** this is the expensive one. Item 3's redesign was built to fill the width this
restructure freed up. Reverting the layout means the published-graph redesign no longer has a home and
would need re-siting. **Rejecting this blocks item 3; rejecting item 3 does not block this.** Review in
this order.

## 3. Published-graph redesign — initial state only

**What / where:** `dd7e323`. Full-width published-series graph with provenance overlay.

**Look at:** Statistics → Domestic, at turn 0. You will see the single inherited quarter and the message
*"…for [period]. Next release builds the trend."*

**The judgment:** does a single-point graph with that message read as *"the system is working, data is
coming"* or as *"something is broken / empty"*? This is a new player's first impression of the entire
publication mechanic, and it lasts until the first release lands.

**If rejected:** just this panel's empty state. Does not block the revision review in item 8 — that is a
different visual on the same widget.

## 4. Amber draft cue

**What / where:** Phase C batch 6. `DrawDraftLabel(text, standing, draft)` — 25 call sites.

**Look at:** any policy screen with a slider. **Drag one.** The label should turn amber to mark
"changed from standing."

**The judgment:** is amber legible against the panel background *and* distinguishable from the other
status colours already in use? And does it read as "you changed this, not yet law" rather than as a
warning that something is wrong? That distinction is entirely a colour-semantics judgment.

**If rejected:** isolated but wide — 25 sites share one helper, so a colour change is one edit. Redo,
not a block.

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

---

# TIER 1 — one turn (ends day 121 = 2026-05-02)

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

- **B2 contextual policy-screen stats** (`3dcf038`) — data layer only. `PolicyScreenStats` resolves
  which stats belong on which screen; **no rendering was written**. Nothing to look at, by design.
- **The 42 macro sprites** — imported, unwired, invisible. See the correction at the top.
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
| — | *advance 1 turn* | → 2026-05-02 | |
| 7 | First release + lag | 1 turn | shares axis with 8 |
| — | *advance 1 turn* | → 2026-08-31 | |
| 8 | **Revision treatment** | 2 turns | the payoff; keep `[DEBUG]` until approved |
| — | *advance 1–2 turns* | → Oct 1 (USA) / Jan 1 (EU) | |
| 9 | Budget Process restyle | 3–4 turns | closes 5e |

**Note on save/load:** there is still no persistence (Master Sequence item 8), so closing Unity mid-review
loses the advancement. Items 1–6 cost nothing to redo; items 7–9 would need the turns re-run. Worth doing
7–9 in one sitting.
