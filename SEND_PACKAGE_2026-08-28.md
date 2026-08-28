# The send package — 2026-08-28, regenerated for UI v3.0 Phase C (paste-and-glance; sending is Elias's)

**This paste supersedes every earlier package** (the two 2026-08-28 generations for Phase A and the
2026-08-27 send): the boards it asked for have landed and are built, so the ask and its annex captures
are no longer in it. Two documents, each with its SHA-256 as on disk (CRLF), per the hash-verified send
precedent (`MISSING_PREREQUISITES.md` §S):

1. **The courtesy note, now 1i–1n** — `CLAUDE_DESIGN_BOARD_1I_NOTE.md`, rewritten 2026-08-28 (v3.0
   Phase C): the two v3.0 boards built the day they landed, the three build calls stated (the ways home,
   the two refused rows and why, the FOLDED lock) — the same three your board already carries as accepted
   corrections — 1n as the re-skin it was, and a plain thanks for the same-day boards. A note, not an
   ask; nothing in it needs an answer.
2. **The request doc** — `CLAUDE_DESIGN_ASSET_REQUEST.md`, its one live ask being **§E5's hatch re-cut with
   the measured figures** (the 2026-08-28 re-export's stripes sit at a 32 px period where the shipped PNG's
   is 16; the phase is fine, the duty ≈8 px along x; lines at `x + y = 16k`, perpendicular stroke ≈5.7).
   §1 (the boards ask) is migrated to `COMPLETED.md` §41 and stands in the doc only as a stub; §0/§4/§5 are
   context Design already holds.

**Where each goes:** the note to a NEW dated path (`send/design_note_2026-08-28c/CLAUDE_DESIGN_BOARD_1I_NOTE.md`
— new, because a fresh path is what shows as new in Design's inbox; the earlier note path, if it was ever
pasted, stays as history); the request doc to BOTH `uploads/CLAUDE_DESIGN_ASSET_REQUEST.md` (the path every
earlier send used and Design has read) AND a new dated copy at
`send/design_request_2026-08-28c/CLAUDE_DESIGN_ASSET_REQUEST.md`. *The dated `…-28c` paths are established
hygiene — ratified standing 2026-08-28 as R-PC4a (the consolidation rider).*

| artifact | SHA-256 (as on disk) | bytes | where it goes |
|---|---|---|---|
| `CLAUDE_DESIGN_BOARD_1I_NOTE.md` - the courtesy note, 1i-1n (rewritten 2026-08-28, v3.0 Phase C) | `26892355f4ff6bba1639c89382aa8b41f9272a3c4e5a34dbd9fb94597da4b8ad` | 11 959 | `send/design_note_2026-08-28c/CLAUDE_DESIGN_BOARD_1I_NOTE.md` |
| `CLAUDE_DESIGN_ASSET_REQUEST.md` - the request doc; the one live ask is section E5 (the hatch re-cut with the measured figures) | `b545233b1e3cc88d8b245bbca54e8f381254172bdd3a5e6a5d5506f0ce3c98a6` | 20 099 | `uploads/CLAUDE_DESIGN_ASSET_REQUEST.md` AND `send/design_request_2026-08-28c/CLAUDE_DESIGN_ASSET_REQUEST.md` |

**The glance, after the paste:** read each document back (`get_file`) and hash the readback; the digests
above are what the readback must equal (`sha256sum` in Git Bash on the CRLF file; a LF-normalized readback
hashes differently — compare like with like). Then mark §S in `MISSING_PREREQUISITES.md` SENT with the
date, the way the 2026-08-27 send is marked.

**What comes back, and where it lands:** one thing only — **Design's hatch re-cut** (`svg/ui_hatch_draft.svg`
at the 16 px period). The day it lands: import it over `Assets/Resources/Art/UI/Chrome/Source/ui_hatch_draft.svg`,
run `StripCutDiffCheck` with the external rasterizer (`-stripcutrasterizer=G:\UNITY\Projects\PoliSim-captures\tools\resvg-0.47.0\resvg.exe`),
and if the pair sits in budget remove `ui_hatch_draft` from `DeferredPairs` in the same commit (R-D3's close
condition). If it still misses, measure the residual on the PNG as before and say which. The note asks for
nothing and expects nothing back.

**Not in this package, by design:** the capture films (reference material out of tree, named in the note —
no import on Design's side); the request doc's §0/§4/§5 (context Design already holds); the boards ask and
its annexes (answered, migrated).
