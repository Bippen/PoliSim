# The send package — 2026-08-28, regenerated for UI v3.1 Phase A (paste-and-glance; sending is Elias's)

**This paste supersedes every earlier package** (the Phase C paste of the same day — landed, see
`MISSING_PREREQUISITES.md` §S — and everything before it). Two documents, each with its SHA-256 as on
disk (CRLF), plus the two sitting screenshots from Elias's own machine:

1. **The request doc** — `CLAUDE_DESIGN_ASSET_REQUEST.md`, through **§1, the ninth request: UI v3.1, "one
   frame, denser, instruments"** — Elias's six asks from the first live sitting (D1 the one-frame ruling,
   for Design's awareness; D2 board 1n-r2; D3 board 1m-r2; D4 the density token table; D5 board 2a; D6 the
   contrast pass) with the six annexes measured by engineering (A the duty audit; B the rail's icons at
   the real cells; C the paddings and dead-space shares; D the sitting's findings; E the Statistics census;
   F the ink pairs with their contrast ratios) — and **§E5**, still one re-cut away.
2. **The courtesy note** — `CLAUDE_DESIGN_BOARD_1I_NOTE.md`, **unchanged since the Phase C paste** (the same
   digest; already in Design's `uploads/`) — listed so the readback can be checked, not because it is new.
3. **Annex D's two screenshots** — Elias's, not on this machine: `sitting_1_desk_density.png` and
   `sitting_2_rail_icons.png`, to `send/design_request_2026-08-28d/annex_d/`. Annex B's four rail crops
   (`annex_b_rail_<size>.png`, out of tree beside the captures) go to `send/design_request_2026-08-28d/annex_b/`.

**Where each goes:** the request doc to BOTH `uploads/CLAUDE_DESIGN_ASSET_REQUEST.md` (the path every earlier
send used) AND a new dated copy at `send/design_request_2026-08-28d/CLAUDE_DESIGN_ASSET_REQUEST.md` (the app
also names a duplicate by its digest, which is how the Phase C paste was recognised); the note nowhere new
(it is there); the images as above. *The dated `…-28d` paths are established hygiene (R-PC4a).*

| artifact | SHA-256 (as on disk) | bytes | where it goes |
|---|---|---|---|
| `CLAUDE_DESIGN_ASSET_REQUEST.md` - the request doc through section 1, the ninth request (v3.1) with Annexes A-F, and section E5 | `9a98b00e7b86efaf6db27aa1b14339b817bc560dfb4c1c4c74a3c548b8385d2b` | 51 558 | `uploads/CLAUDE_DESIGN_ASSET_REQUEST.md` AND `send/design_request_2026-08-28d/CLAUDE_DESIGN_ASSET_REQUEST.md` |
| `CLAUDE_DESIGN_BOARD_1I_NOTE.md` - the courtesy note, 1i-1n, unchanged since the Phase C paste (already in Design's uploads/) | `26892355f4ff6bba1639c89382aa8b41f9272a3c4e5a34dbd9fb94597da4b8ad` | 11 959 | nowhere new - listed for the readback check |
| `annex_b_rail_1280.png` - Annex B, the rail at the 1280 cell (a document beside the Desk) | `332eb36a7ed1d498c8b6a1db69ba1dcbad763559a93663d1502b7205819ddc63` | 43 768 | `send/design_request_2026-08-28d/annex_b/annex_b_rail_1280.png` |
| `annex_b_rail_1600.png` - Annex B, the rail at the 1600 cell (a document beside the Desk) | `7b2b8749a393dd80eb4074092d7b3df0e64b4630a6282e72c67cac380ff27847` | 57 034 | `send/design_request_2026-08-28d/annex_b/annex_b_rail_1600.png` |
| `annex_b_rail_1920.png` - Annex B, the rail at the 1920 cell (a document beside the Desk) | `aa0d860a7ec42f10b6bc7e4892e40f36399aa631087ebf8f70fbbfe336a71c4b` | 68 843 | `send/design_request_2026-08-28d/annex_b/annex_b_rail_1920.png` |
| `annex_b_rail_2560.png` - Annex B, the rail at the 2560 cell (a document beside the Desk) | `df9afb596832b3a2609b792aeee531c353280617fe8d94231db092baddff91e7` | 89 388 | `send/design_request_2026-08-28d/annex_b/annex_b_rail_2560.png` |

**The glance, after the paste:** read the request doc back (`get_file`) and hash the readback; the digest
above is what the readback must equal (`sha256sum` in Git Bash on the CRLF file; a LF-normalized readback
hashes differently). The images are verified by listing. Then mark §S in `MISSING_PREREQUISITES.md` SENT
with the date, the way the Phase C paste is marked.

**What comes back, and where it lands:** (1) **board 1n-r2** — the rail: on arrival, the home cell's face
replaces the flag interim and the rail's width follows Design's number if legibility earned one (v3.1 Phase
B, `v31b_*` film); (2) **board 1m-r2** — the Desk revised for density with the Year-0 empty states designed:
built against the board as 1m was, the Desk's layout constants replaced as a set; (3) **the density token
table** — one number per token against Annex C's current values, applied mechanically (`GameController`'s
fractions and the `PoliSimTheme` / `PoliSimWidgets` constants), then a full matrix; (4) **board 2a,
Statistics drawn** — built against the board with the honesty channels intact, the eight-series cap and
the palette unchanged; (5) **the contrast pass** — new values for existing tokens in `PoliSimTheme.cs`,
then a full matrix and Annex F re-measured; (6) **the hatch re-cut** (§E5) — **already on the live project
as of 2026-08-28 evening** (`svg/ui_hatch_draft.svg`: nine 45°-rotated rects at 11.314 spacing, 5.657 wide —
a 16 px horizontal period and an 8 px duty, the measured figures exactly); its import, the resvg diff and
the deferral's lift are the §E5-close micro-pass's, the next pass after this one. Each lands in its own
pass with its own film; none is started before its board.

**Not in this package, by design:** the capture films beyond the four rail crops (reference material out of
tree, named in the annexes); the request doc's §0/§4/§5 (context Design already holds); the answered asks.
