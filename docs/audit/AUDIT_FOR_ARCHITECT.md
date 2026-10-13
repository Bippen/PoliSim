# PoliSim — the gameplay audit: for the architect

At most 100 lines (`docs/audit/AUDIT_PROTOCOL.md`, QUESTIONS AS YOU GO): the S1/S2 findings, and the modelling, sourcing and ruling questions the audit raised, each marked ARCHITECT. These are not asked of Elias. Each line names its ledger row in `docs/audit/AUDIT_LEDGER.md`, where the evidence is.

## S1 and S2 findings

None confirmed yet - an S1 or S2 is filed only once a run has reproduced it from its seed twice. Read as S2-class leads, to be run down in their areas:
- GA-0-21 (area 3): Sweden's live count skips the 12 % valkrets route the built procedure carries.
- GA-0-28 (areas 2, 8): the carbon statute overrides a passed German rate; statutes read the turn, not the calendar.
- GA-0-47 (area 2): the fund's phantom drawdown, a dissolved fund's assets, the UI-only budget gates.

## Questions marked ARCHITECT

1. **GA-0-16** - the coalition dynamics the political spec lists (a partner threatening to leave or walking out, the PM dismissing one, a supporter enforcing): which are owed, and in what order against PS-7 and PS-8?
2. **GA-0-20** - should the live count carry turnout and election-day noise (the built `ElectionDay`), or stay deterministic from shares?
3. **GA-0-21** - Sweden: wire `SeatConversion.Sweden` (the 12 % route, per-valkrets seats) into the live count, or rule the national count as the game's?
4. **GA-0-23** - the campaign spec is a reference: which of its refused or absent actions are owed?
5. **GA-0-25** - should events move salience and elections beyond the economy?
6. **GA-0-27** - cabinet: an economy portfolio; Gamson's small-party bonus; which families read a portfolio's effectiveness ("eight departments" against six portfolios)?
7. **GA-0-29** - the central bank's head: each bank's own term, sourced, in place of one four-turn cycle?
8. **GA-0-45** - the stated premises no spec row holds (the constructive vote's Monday timing, "any party may move it", Art. 63(4)'s appointment, the Sunday rules, Poland's first sitting at +30, 2022's blocs for every later Swedish election): record them as rulings, or revisit?
9. **GA-0-47** - the money behaviours no spec names: should fund terms stay outside the vote now that a -10 % fund can be created and drawn while empty; should dissolving a fund credit its assets; should the yearly spending range and the budget window be the model's gates rather than the UI's?
10. **GA-0-53** - the central bank: retire the dead own-currency dial; give AI-run banks a head; damp the preview as the projection does?
11. **GA-0-55** - levers the UI writes with no manager verb (a trade override, fleet orders, the central bank's head): give them verbs (and the impact ledger a view of them), or state the rule?
12. **Slice B's two** - since the roles of §628, which counterfactual is "without your policies" meant to be: the shadow world in which the AI governs the player's country, or one in which the player's party governs with no changes? And after a load, a fork of the loaded game (as built) or the seed's?
