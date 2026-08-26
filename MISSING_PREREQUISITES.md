# PoliSim — Missing Prerequisites

**What this is:** every task that cannot proceed because something it needs does not exist yet. Created
2026-08-02 in the first consolidation pass; **slimmed to the live register 2026-08-26** (the second
consolidation) — every closed section migrated to `COMPLETED.md` §23, with tombstones below so old
citations still resolve.

**What this is not:** a backlog of work someone could pick up. Nothing here is startable. Work that is
merely *unbuilt* stays in `POLISIM_MASTER_ROADMAP.md`; work that is *waiting* lives here.

**The distinction that matters:** a task belongs here only if a named external party or a named upstream
task must act first. "Hard", "large" and "not scoped yet" are not blockers.

**The register, complete:**

| entry | waiting on | gate |
|---|---|---|
| §S — the send package | **Elias — send** | the E2 convention: sending is Elias's |
| §B — three seed quality debts | **a database re-sourcing session** | none blocks anything |
| §D1 — cabinet portraits, batch of nine | **Claude Design — delivery** | all three gates answered 2026-08-26 |
| §E2 — mark accounting + the R5 hexes | **item 10** | 13 Sept 2026, Sweden votes |
| §E3 — rasterization diff, our half | **a raster path whose OUTPUT is comparable** | compare machinery finished and waiting |

---

# S. Waiting on Elias — the send

**The next send package to Claude Design is assembled and waits only on the send** (the E2
convention: this repo writes; Elias transmits). Contents: **§8** (the calendar panel board
request), **§9** (the statistics graph weight and treatment ruling), **the §5/D1 verdict** (the
register side-by-side PASSED 2026-08-26 — Design may proceed with the batch of nine), and **the
1j-aware courtesy note** (`CLAUDE_DESIGN_BOARD_1I_NOTE.md`). Attachments are listed in each
section's own ATTACHMENTS block, verified on disk. Recorded here so the send is not lost to the
cached-status pattern — a package in a document and not in anyone's inbox is the failure the E2
convention exists to name.

---

# A. Waiting on Elias — a decision

**CLOSED — tombstone (2026-08-26).** A1 (the rating thrash → fixed by REVIEW CADENCE, not damping;
closed in full 2026-08-17 when the 5-anchor check held at HEAD unrecalibrated), A2 (SWF emergency
drawdown → a standalone tier-3 bill, ruled AND built 2026-08-02, `b1c077f`), A3 (cabinet
appointments stay UNILATERAL) — all resolved 2026-08-02. **The reasoning migrated IN FULL to
`COMPLETED.md` §23**, kept whole deliberately so none of these is reopened as an unanswered
question later.

# B. Database access — three quality debts survive (none blocks anything)

**Every figure that blocked a batch was sourced 2026-08-02** — the sourcing history is
`COMPLETED.md` §23; the values, queries and status flags are `POLISIM_SEED_DATA_MACRO_OVERHAUL.md`.
What remains is **QUALITY DEBT, not gaps**, waiting on a re-sourcing session with database access:

| Debt | Where | What would settle it |
|---|---|---|
| **The real-wage row mixes THREE bases** | seed §5 | Re-source all six from OECD Taxing Wages 2025 (one basis, in SDMX). *Correct figures, incoherent set — the housing-overburden defect again* |
| **The AHD vintage behind C1's estimates is unrecorded** | seed §1 | Find the year of the four OECD anchors. **Unrecorded vintage is exactly what made 90.86 undecidable** — the canonical example |
| **Three C1/C2 figures are `[ESTIMATED]`, not sourced** | seed §§1–5 | Italy/Sweden/Poland homeownership, Sweden real wages, USA Gini — rung 3 of the fallback ladder, stated methods and bands, replaced the moment real figures exist. *Placeholders that play correctly, not facts* |

**Standing rule, three-for-three (kept live here — it governs any re-sourcing):** for any
cross-country statistic, **assume an undocumented variant axis exists** and record the basis
alongside every value — indicator code, population base, threshold, year. Housing overburden had
8 variants where its warning implied 3; youth unemployment 4 where it implied 2; homeownership 4+
with no warning at all. A bare number is unfalsifiable later.

# C / D2 / E1 / F — tombstones (closed sections, migrated 2026-08-26)

- **C — visual review:** empty since 2026-08-02; all eleven items confirmed. Record: `COMPLETED.md` §16.
- **D2 — Round 4 scoping:** released 2026-08-02; the arc closed 2026-08-17. Record: `COMPLETED.md` §19.
- **E1 — `icon_stat_interestrate`:** delivered the same day it was recorded as awaiting. Record: `COMPLETED.md` §15.
- **F — Step C4's closure:** ✅ **CLOSED 2026-08-17 — the F register's count is ZERO.** The closure
  chain, the 1,416 → 19 measurement table and the double-count fix: `COMPLETED.md` §23. C4 and A1
  closed together; Poland's expected-fail anchor row stands as the tripwire.

---

# D. Waiting on another task

## 🟡 D1. Cabinet portraits — all three gates ANSWERED; the batch of nine waits on DESIGN's delivery

✅ **The register side-by-side PASSED (2026-08-26, Elias's live Editor session):** the painted
plate belongs beside the existing register — Design's own named gate for the batch. All three
gates are answered (envelope ACCEPTED, vignette FRAME-OWNED, register PASSED — recorded in the
request doc's header and §5). **The verdict travels in the send package (§S above); Design may
proceed with the batch of nine per the approved §5 PoC.**

**Task:** portrait art for Defense, Foreign Affairs and Education ministers — 9 portraits, request
in `CLAUDE_DESIGN_ASSET_REQUEST.md` §5, filenames derived from the signed names.

**History:** blocked on the portfolios being authored → R4-4 authored all nine (ruling R1, signed)
→ the request SENT 2026-08-17 (Elias) → Design answered with a PROOF, not the batch
(`portrait_cabinet_defense_katarzyna_ekelund`, 512×640 opaque painted plate, imported) → the
register gate cleared 2026-08-26. Delivery lands per the E2 convention when it lands — import per
§3's treatment rules; `ImporterSettingsCheck`/`DeliveredAssetCheck` pick up the 18 files (9 × 2).

**Blocks:** nothing. The game renders the procedural placeholder for the nine until art lands —
coverage of the EXISTING 16 (9 ministers + 7 Fed chairs) is unaffected.

---

# E. Waiting on Claude Design

## 🟡 E2. `mark_party_us_lib` — delivered and imported 2026-08-17; the branch-side accounting is the residual (gate: item 10, 13 Sept 2026)

The sprite-side conditions are met: imported to `Emblems/` (meta from the MARK family, fresh GUID,
`ImporterSettingsCheck` green — the WoA classification read from pixels, not the label). ⚠ This
entry's close condition was "PartyMarkCoverageCheck reports it resolving at RGBA32" — and on MAIN
that check honestly reports **"PARTY SYSTEM NOT PRESENT on this branch... VERIFIED NOTHING"**: the
party seeds live on `stranded/politics-elections`, item-10-gated. The accounting half runs when
the branch does — orphan-by-sequencing, the same recorded status as the other four marks.
Delivery story: `COMPLETED.md` §24 (the §1G record) and CLAUDE.md's 2026-08-17 import entry.

**Riding the same gate: the R5 hex exchange.** Design's flag ("LP gold needs an ink-safe darkened
`DisplayColor` — pass it with Sweden's set") is GATED BY NAME on item 10 — no party seeds exist on
main, so no hexes exist to send. The exchange fires when the gate opens; Design is waiting on a
calendar, not on us.

⚠ **Zone.Identifier check CLOSED (2026-08-26, Windows-side):** all five `mark_party_*.png` carry
only the `:$DATA` stream — no mark-of-the-web ADS exists on any of them (the files were extracted
from the verified pack by tooling, so none was ever stamped). The §1F.2 outstanding item is
answered: nothing to observe, nothing blocked.

## 🟡 E3. Design's rasterization diff — our half, gated on a raster path whose OUTPUT is comparable

`CLAUDE_DESIGN_ASSET_REQUEST.md` §1F.1: Design asked that their strip-cut PNGs be diffed against
our own rasterization once before the pipeline is trusted. **Design's HALF is CLOSED**
(2026-08-17, the Progress2 manifest): they re-rasterized the six per-state button PNGs fresh from
SVG and pixel-diffed 6/6 identical. **Our half stays exactly this entry.**

⚠ **The gate, sharpened (2026-08-17) — the blocker is named to the component:** a rasterizer DOES
exist on this machine (Unity's built-in vectorgraphics module, demonstrably tessellating every
Source/ SVG at import), and `StripCutDiffCheck` exists with the full tolerant-compare machinery —
but the module's `RenderSpriteToTexture2D` path yields a BLANK texture under the batch harness,
probed and viewed rather than inferred (mismatch shares equal to ink coverage, identical across
two framings; the dumped artifact is an empty sheet). **E3 closes when the render path works or an
external rasterizer lands** — the compare is finished and waiting.

⚠ **Attribution corrected (2026-08-26, consolidation sweep):** the earlier note that
`ui_slider_track` "additionally hits the module's one true parse limit (SVG `<pattern>`)" was
wrong — the file's actual features are `linearGradient` + `currentColor`. The blank-render blocker
stands regardless; it was never attributable to a `<pattern>` parse limit. Corrected rather than
repeated, per the capture-vintage rule on cited evidence.
