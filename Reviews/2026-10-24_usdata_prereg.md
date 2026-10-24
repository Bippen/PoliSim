# Preregistration: the US count's 5b ceiling and "able to flip it" (K112, K113), wave/usdata

Written and committed BEFORE the check that reads it is changed and before any Unity run measures it (Elias's rulings K112 and K113 of 2026-10-24, on the review `Reviews/2026-10-24_usdata_review.md`, M-1). Nothing below is tuned on a result.

## The run

`UsCampaignDiagnostic` (the simulation bar), section (m), on the USA's start staging (seed 777). Two lines read this file:
- `(m) K112, THE REGISTERED 5b CEILING` - rule 1;
- `(m) THE DONE-WHEN, able to flip it` - rule 2, with K113's reading.

US-31's original clause ("US-9's measured amount") stays held on the function it was registered on (`UsGroundGame.EffectsOnCurve`, W-B11's curve) against the card's US-9 block, unchanged.

## Rule 1 - the 5b ceiling, per state (K112)

The maximal ground game is US-9's: 40 opened doors per eligible voter in one jurisdiction for one nominee's party, noise off, through the count's own function since K18, `UsGroundGame.Effects`.

The expected move is DERIVED from 5b's own arithmetic, not measured. For a jurisdiction with:
- `b` its base turnout (its votes cast of record over its CVAP, `Campaign.BaseTurnoutAt`),
- `c` Gerber and Green's door CACE at that base, as a rate (`GotvTurnout.DoorCacePp(USA, b) / 100`: 0.046, 0.062, 0.014, 0.006 by the floors 0, .30, .50, .70),
- `s_p`, `s_q`, `s_o` the worked party's, the other nominee's and every other candidate's shares on the map,
- `k = 1 - exp(-40 / s_p)` the share of the worked party's supporters its doors reach,

the worked party's supporters vote at `b + k c` and everyone else at `b`, so its margin over the other nominee moves by

`D = (s_p (b + k c) - s_q b) / (b + s_p k c) - (s_p - s_q)` (as a share; times 100 in points).

**Pass:** on US-9's own map (US-4's (a) row, 2024's national shares on 2020's states) and on the game's staging map, for every one of the 51 jurisdictions and either party, the measured margin move equals `100 D` within 1e-3 points (the count's whole-vote rounding), and no unworked state moves by more than 1e-3 points. The registered band is the closed form's own range, printed per map.

What the closed form gives (DERIVED, for the reader; the check computes it): about `0.5 c / b` points. Roughly 1.0 to 1.2 points in a state whose base is 50-70 % (c = 1.4 points), at most about 0.43 points at a base of 70 % or more (c = 0.6), and up to about 7 points in any state whose base falls in the 30-50 % band (c = 6.2). Against US-9's registered 2.90 to 9.00 on the curve, 5b moves a margin far less in most states.

## Rule 2 - "able to flip it", on the live path (K112, K113)

Unchanged in form from US-31's PLAN_PREREG: the closest winner-take-all state on the count with its keyed noise, worked at the ceiling by the trailing party through the count's own path (`UsPresidentialElection.Count`, so 5b since K18).

**Pass:** the call flips, every other state's shares are bit for bit the same, and the trailing party gains exactly that state's electors.

**K113's reading - not a failure:** if the call does not flip, but every other state's shares are bit for bit the same and the worked state's move equals rule 1's closed form on that state (within 1e-3 points), the diagnostic prints the named line `REGISTERED OUTCOME CHANGED (K113): ...` - the state, its margin, 5b's move and the shortfall - and does not fail. US-31's registered outcome is then recorded as changed by K18, and 5b is kept. Any other departure (another state moved, the electors wrong, the move off the closed form) fails.

## The planted tooth (the review's L-5)

With no contact anywhere, the widest move is below rule 1's smallest closed-form move on the staging map, so rule 1 bites.

## Amendment 1 (2026-10-24, the delta review's M-2; K114) - committed before the check reads it

Elias's K114 (2026-10-24, correcting K113's wording) stands as rule 2 already reads: a lost flip under the live 5b count prints the named `REGISTERED OUTCOME CHANGED (K113)` line - the state, its margin, the move and the shortfall - and does not fail the bar; every other departure fails.

What changes (stricter, never looser): the named line now also requires that the live path itself was judged, not a separate call. The no-flip branch prints the named line only when ALL of these hold; any one failing FAILS the check:
- every other state's shares bit for bit the same (as before);
- the trailing party gains no elector (`gained == 0`), and the count kept a ground-game effect for every state (`GroundGame.Count` = the states counted) - the two conditions the flip branch asserts;
- THE LIVE MOVE IS THE COUNT'S OWN: the worked state's move on the live path (`UsPresidentialElection.Count`, the margin printed) equals, to 1e-9 points, the count's own function on the live inputs - `UsGroundGame.Effects` on the count's derived states with the worked campaign, less the same with no contact, at the count's keyed noise seed and sigma. A live path that dropped or mis-routed the doors fails here instead of being labelled the ruled outcome;
- the worked state's 5b move with the noise off (on its noisy-map shares) equals rule 1's closed form within 1e-3 points (as before; the live move itself is not held to the closed form at 1e-3, because the scaled noise's sqrt(s(1-s)) term shifts it by about 5e-4 times the draw).
