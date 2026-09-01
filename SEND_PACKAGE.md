# The send package — regenerated 2026-09-01 (paste-and-glance; **sending is Elias's**)

⚠ **THIS IS A RETURN PACKAGE, NOT A SEND.** The C-F1 package went out and **D9 was answered in full** —
all eleven rows, on `PoliSim v2 Screens.dc.html`. Verified 2026-09-01 rather than assumed:
`uploads/CLAUDE_DESIGN_ASSET_REQUEST-347e3be8.md` reads back at **77 510 bytes / `347e3be8…`**, which is
the package's own glance, passed; `uploads/CLAUDE_DESIGN_BOARD_1I_NOTE-948fd2a6.md` is present at its
digest. **Both rows accounted for.**

⚠ **THE OLD PACKAGE'S GLANCE INSTRUCTION WAS WRONG AND IS FIXED HERE.** It called the digest *"as on
disk, CRLF"* and warned that *"an LF-normalized readback hashes differently"*. **The file on disk is LF**
— `sha256sum` on it gives `347e3be8…`, and so does the LF form of the readback; the CRLF form gives
`05ae6eb4…`. The old wording would have made a **correct** readback look like a failed paste, which is
the same class of defect as the stale digest it replaced: an instruction that fails the thing it exists
to prove.

⚠ **P-F2 stands, narrowed.** There is still no receipt for the **D7-era** paste — `85690abf…` appears
nowhere in `uploads/`. That paste was never made. **D9's was**, and the repo said otherwise for a day.

---

## What Design is waiting on — two returns and one word

### Return 1 of 2 — D9 row 6: the mandate column (**derived, and half the ask corrected**)

Design asked for *"the mandate column (fixed seats per valkrets) from `valkrets_votes_2022.csv`, and the
built cartogram's own cell order from `SwingRegions.cs`"*, refusing to guess tile area. ⚠ **Both halves
rest on a wrong premise, and the honest return corrects them rather than satisfying them:**

- The mandate column is **not in the CSV.** The file carries `eligible`; the 310 fixed seats are DERIVED
  from it by the statute's own rule, which `SeatConversion.FixedSeatsPerRegion` implements. The column
  below is computed by the shipping allocator and **sums to exactly 310**.
- **`SwingRegions.cs` holds no cell order and no geometry.** It takes a name and a weight per region from
  its caller. **There is no built cartogram to disagree with**, so the north-to-south, coast-to-coast
  arrangement is Design's to make. The order below is the CSV's own row order = Valmyndigheten's valkrets
  numbering 01–29, which is a numbering, not a geography.

| # | valkrets | eligible | fixed seats |
|---|---|---|---|
| 1 | Stockholms län | 1 006 456 | **40** |
| 2 | Stockholms kommun | 728 089 | **29** |
| 3 | Uppsala län | 292 255 | 12 |
| 4 | Södermanlands län | 223 767 | 9 |
| 5 | Östergötlands län | 356 210 | 14 |
| 6 | Jönköpings län | 271 666 | 11 |
| 7 | Kronobergs län | 147 910 | 6 |
| 8 | Kalmar län | 189 781 | 8 |
| 9 | Gotlands län | 48 274 | **2** |
| 10 | Blekinge län | 121 789 | 5 |
| 11 | Skåne läns västra | 236 712 | 9 |
| 12 | Skåne läns södra | 299 809 | 12 |
| 13 | Skåne läns norra och östra | 244 489 | 10 |
| 14 | Malmö kommun | 251 172 | 10 |
| 15 | Hallands län | 258 794 | 10 |
| 16 | Västra Götalands läns västra | 285 927 | 11 |
| 17 | Västra Götalands läns norra | 208 144 | 8 |
| 18 | Västra Götalands läns södra | 170 107 | 7 |
| 19 | Västra Götalands läns östra | 207 560 | 8 |
| 20 | Göteborgs kommun | 434 273 | **17** |
| 21 | Värmlands län | 216 666 | 9 |
| 22 | Örebro län | 232 024 | 9 |
| 23 | Västmanlands län | 208 376 | 8 |
| 24 | Dalarnas län | 221 344 | 9 |
| 25 | Gävleborgs län | 221 395 | 9 |
| 26 | Västernorrlands län | 188 542 | 8 |
| 27 | Jämtlands län | 101 363 | **4** |
| 28 | Västerbottens län | 209 485 | 8 |
| 29 | Norrbottens län | 193 011 | 8 |

**310 fixed + 39 adjustment = 349.** Re-derivable at any time:
`-executeMethod PoliSim.EditorTools.ValkretsMandateColumnDiagnostic.Run`.

### Return 2 of 2 — D9 row 9: the stat-icon convention Design cannot see

Design asked for *"one crop of any two"* delivered stat icons so the two new ones adopt the frame and
baseline convention **before** delivery rather than after. ⚠ **Two whole files beat a crop** — they carry
the convention exactly rather than at a crop's mercy:

- `Assets/Resources/Art/UI/Stats/icon_stat_unemployment.png` — 8 895 bytes, SHA-256
  `d86110a5e50b379c6a4eb8f79bf6d35791c24fd6d339bdff17ff2c3344ed3aa6`
- `Assets/Resources/Art/UI/Stats/icon_stat_crimeindex.png` — 12 271 bytes, SHA-256
  `bbc4debdeb25dbd0a2c42ee3cd3b91fda7c06b34ff73f8b9991accaae86eb369`

Both go to `uploads/` under their own names. The two icons to be drawn are
`icon_stat_youthunemployment` and `icon_stat_lifeexpectancy`, already reported as **GAPS not failures** by
`StatIconCoverageCheck` (R-CL4) — which is what made them drawable.

### The one word — D9 row 2's *"say GO"*

Design will cut **seven Swedish party marks** on one word, then the other forty-five as a batch. ⚠ **This
is Elias's and nothing here presumes it.** The vocabulary (5 silhouettes × 4 cuts × 2 fills) is ruled on
the board; what waits is only the go-ahead.

---

## The glance, after the paste

Read each artifact back (`get_file`) and hash the readback **as LF** — `sha256sum` in Git Bash on these
files gives the LF digest, because the working copy is LF. ⚠ **Do not CRLF-normalize the readback**; that
was the old package's error. **Every row accounted for = the paste was whole.**

⚠ **`get_file` caps at 256 KiB**, so a PNG larger than ~192 KB cannot be hash-verified through it at all —
only its leading bytes. Both icons above are far under the cap and verify whole.

## What has already been built from D9, so Design is not asked twice

- **Row 1 (board 2b)** — the Policy Web full sheet, installed.
- **Row 3** — built 2026-09-01: a party with no published colour draws a **hairline swatch, not a fill**,
  with one caption naming the absence. `HasPartyInk` had existed since W-G1 with **no caller anywhere in
  the game**; this is its first. Filmed at 1280 (`d9r3_07a_politics_parliament.png`).
- **Rows 4 and 5** — ruled; the cross-channel hue floor is answered as a **wrong constraint**, not a
  failure to fix (see `COMPLETED.md` §146).

**Not in this package, by design:** the capture films (out of tree), and every figure this repo has
measured rather than been given.
