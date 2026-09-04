# The party-ink harness print, as text (the E-12 return, 2026-09-04)

Board 7b (2026-09-04) read the return and said the print never arrived: the transport keeps markdown and drops every binary and log, so `e12_partyink.log` was a digest with no file behind it. This is that print, verbatim from the log (the bar run `bar92_hue_RunAllBatch.log` printed the same), so row 4's OUTCOME is readable where the ruling already was.

```
--- The eight sourced inks: published hue in, desk-seated hex out (the R5 exchange) ---
    party  published  desk-seated   hue    nearest area accent        gap
    S      #FF0000    #753838         0.0  CrimeJustice               6.0 ** TOO CLOSE **
    SD     #4E83A3    #385E75       202.6  Global                     2.8 ** TOO CLOSE **
    M      #66BEE6    #386275       198.8  Global                     6.6 ** TOO CLOSE **
    V      #C40000    #753838         0.0  CrimeJustice               6.0 ** TOO CLOSE **
    C      #63A91D    #577538        90.0  Political                 47.7 ok
    KD     #1B5CB1    #385375       214.0  Fiscal                     0.9 ** TOO CLOSE **
    MP     #008000    #387538       120.0  Trade                     53.3 ok
    L      #3399FF    #385775       210.0  Global                     4.6 ** TOO CLOSE **

    Every desk-seated ink is at saturation 0.52 and value 0.46 by construction.

--- ⚠ TWO PARTIES, ONE INK ---
    #753838 is drawn for 2 DIFFERENT parties: Sweden/S, Sweden/V
    The seating keeps the published HUE and replaces saturation and value, so two
    published colours differing only in darkness collapse onto one desk ink. A
    hemicycle, a legend swatch and an election-night row would draw these parties
    identically. ⚠ NOT fixed here: any fix either stops using the authority's own
    hue or picks a replacement by eye, and both are D8-2's ruling to make.

--- THE NUDGE (P3-C6, ruled 2026-09-03): the published hue is the identity; a MEASURED collision (oklab distance below the tolerance) moves the smaller party's lightness by the least that separates it - never its hue, never the order ---
--- THE FORK, RULED (Elias, 2026-09-03, COMPLETED.md §279): Valmyndigheten's published table is the base and the identity; Design's quoted set is the ALTERNATIVE, recorded with its source and consulted only where ours produces a measured collision - tried first for the smaller party, taken only if it clears the tolerance ---
    tolerance 0.06 oklab, cap 0.10 L (both [AUTHORED-DRAFT], confirmed by Design on board 6b row 4)
    M: 0.012 from SD (< 0.06) → the alternative #52BDEC consulted: 0.014 from SD - below the tolerance, NOT taken; the lightness nudge follows
    M: 0.012 from SD (< 0.06) → L +0.050
    V: 0.000 from S (< 0.06) → the alternative #DA291C consulted: 0.011 from S - below the tolerance, NOT taken; the lightness nudge follows
    V: 0.000 from S (< 0.06) → L +0.060
    KD: 0.036 from SD (< 0.06) → the alternative #000077 consulted: 0.115 from SD - TAKEN, its hue seated the desk's way, lightness unmoved
    MP: 0.034 from C (< 0.06) → the alternative #83CF39 consulted: 0.001 from C - below the tolerance, NOT taken; the lightness nudge follows
    MP: 0.034 from C (< 0.06) → L -0.035
    L: 0.024 from SD (< 0.06) → the alternative #006AB3 consulted: 0.006 from SD - below the tolerance, NOT taken; the lightness nudge follows
    L: 0.024 from SD (< 0.06) → L -0.040
    party      bloc          mandates  seated   L(seated)  nudge   drawn     L(drawn)
    Sweden/S   the left bloc      107  #753838      0.421    0.00   #753838       0.421
    Sweden/SD  the right bloc       73  #385E75      0.465    0.00   #385E75       0.465
    Sweden/M   the right bloc       68  #386275      0.474   +0.05   #467184       0.524
    Sweden/V   the left bloc       24  #753838      0.421   +0.06   #884948       0.481
    Sweden/C   the left bloc       24  #577538      0.525    0.00   #577538       0.525
    Sweden/KD  the right bloc       19  #385375      0.436   -0.06   #383875       0.376
    Sweden/MP  the left bloc       18  #387538      0.507   -0.03   #2E6B2E       0.472
    Sweden/L   the right bloc       16  #385775      0.446   -0.04   #2E4C6A       0.406

    THE TWO TABLES: ours = Valmyndigheten fargkod, the identity; the alternative = board 6b row 4, PoliSim v2 Screens.dc.html (Design, 2026-09-03)
    party      ours      alternative  drawn from
    Sweden/S   #FF0000   #E8112D      ours, unmoved
    Sweden/SD  #4E83A3   #DDDD00      ours, unmoved
    Sweden/M   #66BEE6   #52BDEC      ours, lightness nudged
    Sweden/V   #C40000   #DA291C      ours, lightness nudged
    Sweden/C   #63A91D   #009933      ours, unmoved
    Sweden/KD  #1B5CB1   #000077      the ALTERNATIVE (a measured collision, its hue clears the tolerance)
    Sweden/MP  #008000   #83CF39      ours, lightness nudged
    Sweden/L   #3399FF   #006AB3      ours, lightness nudged
    The marks keep the seated hex; the nudged ink draws where ink is the only channel - the per-seat hemicycle and the legend's swatch and bar.
    An ink no collision touched is its seated hex, unmoved. The pairs above go to Design as a per-party confirmation, not a re-derivation.

--- What has NO ink, and is not given one ---
    8 of 53 seeded parties carry a published colour; 45 carry none.
      USA       2 parties, no published colour table on disk
      Germany   9 parties, no published colour table on disk
      France   15 parties, no published colour table on disk
      Italy    14 parties, no published colour table on disk
      Poland    5 parties, no published colour table on disk
    ⚠ These are NOT given a colour by this project. Picking 30 colours by eye for
      real organisations would be invention, and would probably be wrong - these are
      real parties with real colours a player may already know. `HasPartyInk`
      returns false so a caller can say "not yet coloured" instead of asserting one.
      The ruling is Design's: asset request D8-2, register row D-8.2.

=== PartyInkHarness: ALL ASSERTIONS PASS (8 inked, 45 uninked by design) ===
```
