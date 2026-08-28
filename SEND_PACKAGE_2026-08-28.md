# The send package — 2026-08-28 (paste-and-glance; sending is Elias's)

Two artifacts, both final at HEAD, each with its SHA-256 as on disk (CRLF), per the hash-verified send
precedent of 2026-08-27 (`MISSING_PREREQUISITES.md` §S). Where each goes is one line. Nothing else is
outstanding to Design.

| artifact | SHA-256 (as on disk, CRLF) | bytes | where it goes |
|---|---|---|---|
| `CLAUDE_DESIGN_BOARD_1I_NOTE.md` — the courtesy note, 1i–1l-aware, with R-C8's convergence paragraph (nothing in it needs an answer) | `1a4fd172fa592fa288768922971fb78c5076f0b084e926fc8e8ec7f9a6f12a8a` | 6,664 | Design's project (`PoliSim v2 Design Progress`, `b3dec27b-620b-452a-9783-e8317cbec4d9`) at a NEW dated path, `send/design_note_2026-08-28/CLAUDE_DESIGN_BOARD_1I_NOTE.md` — new, because Design has never read a note at any path and a fresh path is what shows as new in their inbox |
| `CLAUDE_DESIGN_ASSET_REQUEST.md` — the standing request through §E5 (the two strip-cut findings: the hatch tile's tiling rotation, the slider strip's source) | `4f7ee0d25b9f53c41b528f88dadd31eb11d28a58f8c6361a483044686bc4cca0` | 17,632 | the same two paths the 2026-08-27 send used — in place at `uploads/CLAUDE_DESIGN_ASSET_REQUEST.md` (the path every earlier send used and Design has read) AND a new dated copy at `send/design_request_2026-08-28/CLAUDE_DESIGN_ASSET_REQUEST.md` — an in-place overwrite alone produced nothing that looked new last time |

**The glance, after the paste:** read each file back (`get_file`) and hash the readback; the digests above
are what the readback must equal (`sha256sum` in Git Bash on the CRLF file; a LF-normalized readback
hashes differently — compare like with like). Then mark §S in `MISSING_PREREQUISITES.md` SENT with the
date, the way the 2026-08-27 send is marked.

**What comes back, and where it lands:** Design's §E5 answer — either the SVG source re-exported to match
the shipped hatch PNG (our presumption) or the PNG re-cut, and the slider strip's real source or its stated
derivation. The day it lands: import it, remove `ui_hatch_draft` from `StripCutDiffCheck.DeferredPairs`
(R-D3) in the same commit, and re-run the 90-pair sweep — the pair must then pass on its own.

**Not in this package, by design:** the request doc's §0/§4/§5 are context Design already holds; no
capture attachments this time (§E5's two findings name their files, and the check's own renderings sit
beside them in the temp folder on this machine, not in the project).
