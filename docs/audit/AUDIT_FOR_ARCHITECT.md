# PoliSim — the gameplay audit: for the architect

At most 100 lines (`docs/audit/AUDIT_PROTOCOL.md`, QUESTIONS AS YOU GO): the S1/S2 findings, and the modelling, sourcing and ruling questions the audit raised, each marked ARCHITECT. These are not asked of Elias. Each line names its ledger row in `docs/audit/AUDIT_LEDGER.md`, where the evidence is.

## S1 and S2 findings

Filed - each reproduced from its seed twice (an S1 or S2 is filed only then); both are clear bugs, put to Elias as fixes:
- **GA-2-1** (S2, area 2) - money from an empty fund (D-02): a contribution below zero goes on paying the budget after the fund is floored at 0. Germany and Poland create a fund at nothing and draw it; Sweden's empties under the draw; an ordinary French budget at -6 % reaches it.
- **GA-2-2** (S2, area 2) - France's income tax at a lever of 0 keeps the seed's whole revenue: `TaxSchedule.YieldRatio` answers 1.0 for a yield at or below zero (D-04). Four other countries lose 5-11 % of GDP a year on the same bill.

Filed as ARCHITECT (S2: an election or office result, reproduced in every run - the model's, not a code defect; questions 18-19):
- **GA-3-1** (area 3) - an idle German game's 2025 count: SPD first (150-156 seats against the record's 120), AfD 59-64 against 152, FDP and BSW about 70 each against 0, and Scholz re-elected where the record has Merz.
- **GA-3-2** (area 3) - an idle Swedish game's 2026 count gives the right bloc 183-190 seats where the record's gave it 173 (SD +7 to +10, V -6 to -7), and installs SD+M+KD+L in every run, again in 2030.

S2-class leads still to run down in their areas:
- GA-0-21 (area 3): Sweden's live count skips the 12 % valkrets route the built procedure carries.
- GA-0-28 (areas 2, 8): the carbon statute overrides a passed German rate; statutes read the turn, not the calendar.
- GA-0-47 (areas 2, 12): a dissolved fund's assets, the UI-only budget gates (its phantom drawdown is GA-2-1).

## Questions marked ARCHITECT

1. **GA-0-16** - the coalition dynamics the political spec lists (a partner threatening to leave or walking out, the PM dismissing one, a supporter enforcing): which are owed, and in what order against PS-7 and PS-8?
2. **GA-0-20** - should the live count carry turnout and election-day noise (the built `ElectionDay`), or stay deterministic from shares?
3. **GA-0-21** - Sweden: wire `SeatConversion.Sweden` (the 12 % route, per-valkrets seats) into the live count, or rule the national count as the game's?
4. **GA-0-23** - the campaign spec is a reference: which of its refused or absent actions are owed?
5. **GA-0-25** - should events move salience and elections beyond the economy?
6. **GA-0-27** - cabinet: an economy portfolio; Gamson's small-party bonus; which families read a portfolio's effectiveness ("eight departments" against six portfolios)?
7. **GA-0-29** - the central bank's head: each bank's own term, sourced, in place of one four-turn cycle?
8. **GA-0-45** - the stated premises no spec row holds (the constructive vote's Monday timing, "any party may move it", Art. 63(4)'s appointment, the Sunday rules, Poland's first sitting at +30, 2022's blocs for every later Swedish election): record them as rulings, or revisit? Measured in area 3: Poland's government changes on polling day itself (KO+TD+NL from 2023-10-15; the record's Tusk cabinet from 13 December).
9. **GA-0-47** - the money behaviours no spec names: should fund terms stay outside the vote now that a -10 % fund can be created and drawn while empty; should dissolving a fund credit its assets; should the yearly spending range and the budget window be the model's gates rather than the UI's?
10. **GA-0-53** - the central bank: retire the dead own-currency dial; give AI-run banks a head; damp the preview as the projection does?
11. **GA-0-55** - levers the UI writes with no manager verb (a trade override, fleet orders, the central bank's head): give them verbs (and the impact ledger a view of them), or state the rule?
12. **Slice B's two** - since the roles of §628, which counterfactual is "without your policies" meant to be: the shadow world in which the AI governs the player's country, or one in which the player's party governs with no changes? And after a load, a fork of the loaded game (as built) or the seed's?
13. **GA-2-3** (X-07, X-04) - debt, deficits and the rating reach no approval or vote term: Sweden borrows from 35 % to 95 % of GDP in two years while its approval rises to 65, and full welfare buys +23 points as the debt doubles. Should they reach approval or the vote, and by what sourced coefficient?
14. **GA-2-4** - demand has no capacity ceiling: two years of deficits leave real GDP 24 % above potential with unemployment held at the 0.00 % clamp and inflation at 5 %, and prices never fall (0 % with 13 % unemployment). A floor at frictional unemployment, a ceiling on the output gap, deflation?
15. **GA-2-5** (GA-0-24, measured) - approval does not reach the vote: a government at 1.5 % approval on polling day takes 9 seats fewer than one at 50.5 % and keeps its majority; months at 0 % bring no motion, fall or vote. Should approval feed the vote, motions or game over?
16. **GA-2-6** (with PS-6) - the US House adopts the Democrats' budget at every tax ceiling and every spending floor, and refuses the one at every tax floor: what does a bloc's budget vote read?
17. **GA-2-9** (§220) - the closed year's balance is the year's flow by §220's design, the one-time settlements (cabinet and interrupt decisions, drawdowns) named beside it only in `DomesticMoneyBasisDiagnostic`: the balance, the deficit rule's ink and the annual series read a deficit 0.1-0.6 % of GDP smaller than the debt's own change, in five countries. Count the one-offs in the year the player reads (the protocol's "debt change = deficit"), or name them beside it?
18. **GA-3-1, GA-3-2, GA-3-3** - an idle game's count against the record, which each night prints beside it: Germany 2025 seats SPD first and re-elects Scholz, Sweden 2026 hands the right bloc a majority the record did not give it, Poland 2023 has KO 34 seats short and the 2025 run-off at 57 % against 51 %. Should the vote model reproduce the record's counts for a player who does nothing (§346 bills the 2026 count as an out-of-sample test), and within what tolerance?
19. **GA-3-2** (D-22) - the 2022 blocs carry into every later Swedish election, and SD+M+KD+L is installed in 2026 and 2030 in every run: re-form the blocs after each count, or record 2022's as the rule?
20. **GA-3-5** (D-21) - the night's formation ("WHO GOVERNS - FORMED ON THE NEW CHAMBER") and the round's (the talman's proposals) disagree once the player has broken the bloc - the night told SD "IN OFFICE" before four rejections and an extra election: one formation for both, or the night's named a projection?
