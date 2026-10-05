# The presidential vote - model card (Poland, two rounds)

What the game's presidential vote is, what it was fitted to, where it is known to miss, and what is ruled about it. The figures the
model produces are not typed here: they are written by the instrument that pins them, into the marked block under *The readings*
(`PoliSim.EditorTools.PresidentialVoteBacktest.WriteReadings`), and the instrument fails the cheap bar when the block no longer says
what it measures. The record's sections are `COMPLETED.md` §720 (the rule and the count),
§727 (the vote model measured), §764 (the transfer fitted), §765 (who may stand).

## The model

| part | what it does | ruled / sourced |
|---|---|---|
| The rule | Konstytucja Art. 127-128: more than half of the valid votes elects in the first round; otherwise the two with the most meet in a run-off on the 14th day; the run-off's larger count elects. | SOURCED (`TwoRoundElection`, §720) |
| Who stands | Only voters' committees nominate - a party cannot; 15 citizens form one, 1,000 signatures register it, 100,000 nominate. An independent stands by the same gate. | Elias's ruling B5 (§765); Kodeks wyborczy art. 84 § 3, 90 § 1, 296-299, 303 § 1 |
| The first round | In the backtest each candidate inherits the standing of the party that backs it (the previous Sejm). **In the game, the party's support in the game's own poll × the candidate's factor**: the factors of the 2025 field **fitted once** to the PKW's first round of 18 May 2025 against the game's poll that day **on the reference world** - the record's state on the eve, as a game started that day holds it (`PresidentialReferenceWorld`) - which therefore reproduces that first round (a game played from the 2023 start reads its own epoch's history and does not: its first round moves with its play - RULED, Elias's ruling F4); a candidate whose party has no row of its own (Braun, Zandberg, Stanowski and the minor ones) carries its share of record as its base; a later field starts at factor 1.0. Per-candidate data, not a second free parameter. | §727 (the backtest, measured); Elias's ruling E1 (§772) - [FITTED] (`PresidencyOfRecord.CandidateOfRecord.Factor`, refit and held by `PresidentialElectionLiveDiagnostic`) |
| The run-off transfers | Each eliminated candidate's voters split between the finalists as exp(−d²/τ), the distance in the **sovereignty space** - galtan, nationalism and the EU position, equally weighted - each candidate at its party's CHES 2024 row; an unplaced candidate splits as the finalists' first votes did. | Elias's ruling B4 (§764): the sovereignty space, as the evidence of §727 said |
| τ | **One parameter, fitted** by least squares to the Ipsos exit poll of 1 June 2025 - the share of five first-round electorates (Mentzen, Braun, Hołownia, Zandberg, Biejat) voting each finalist. | Elias's ruling B4 - [FITTED]; **no second parameter** (ruled 2026-10-03) |
| Abstention | An eliminated electorate stays home in proportion to its distance to the nearer finalist, the farthest at a drafted share. No source measures it: the exit poll interviews run-off voters only. | [AUTHORED-DRAFT] (`PresidentialElection.AbstentionDraftMax`), on the play-calibration list |

## Known misses - ruled to stand

- **Hołownia's and Zandberg's electorates.** One distance and one τ cannot part them: the sovereignty space places Polska 2050 nearer
  PiS than its voters went, and Razem farther. Ruled (2026-10-03): *"B4 stands as fitted. Don't add a second parameter."* The two misses
  are recorded, not patched - their sizes in *The readings*.
- **The 2020 run-off.** At the fitted τ the 2020 run-off lands further from the record than it did at Poland's own vote-model τ, while
  2025's lands nearer; both keep the winner of record. The margin is in *The readings* (Elias asked it recorded here, 2026-10-03).
- **Mentzen and Braun stand at one row** (Konfederacja's - Braun's party ran on its 2023 list), so the model cannot tell their
  electorates apart either; the exit poll did (§764).
- **The first round inherits a party's standing and nothing of the candidate's own pull** - the Sejm-to-president gap §727 measured. In the game the 2025 field carries its own pull as fitted factors (E1); **a later field does not** (factor 1.0) until play calibrates it.

## The readings

What the instrument measures today, at the fitted τ: the five exit-poll electorates, then each run-off from the record's own first round; then **the game's own 2025, both rounds**, on a fresh world taken straight to 1 June 2025 (E1's acceptance: it elects Nawrocki).
A miss is the model less the record, in percentage points.

The game's first-round rows are **in-sample**: the factors are fitted on this same reference world, so these rows reproduce the PKW by construction, and `PresidentialElectionLiveDiagnostic` fails the bar where they do not - they show the stored factors still hold, not what a world stepped day by day from Poland's start would poll. The game's run-off reads that first round at B4's τ, B4's positions and the draft abstention, so its row is B4's draft-abstention row reached through the game's own day loop; what it adds is the acceptance - who is elected.

<!-- GENERATED by PoliSim.EditorTools.PresidentialVoteBacktest.WriteReadings. DO NOT EDIT BY HAND. source-digest: 86abb59fcb9a1c0e68898efb5c860cad8eb4458b58752259d462d92c5cbe20e5 -->
| reading | model | record | miss (pp) |
|---|---:|---:|---:|
| τ, fitted to the five exit-poll pairs (least squares, one parameter) | 18.301 | - | - |
| the fit's root-mean-square miss over the five pairs | - | - | 6.16 |
| Mentzen's electorate voting Nawrocki (the exit poll) | 91.0 | 88.1 | +2.9 |
| Braun's electorate voting Nawrocki (the exit poll) | 91.0 | 92.5 | -1.5 |
| Hołownia's electorate voting Nawrocki (the exit poll) | 23.7 | 13.8 | +9.9 |
| Zandberg's electorate voting Nawrocki (the exit poll) | 7.3 | 16.2 | -8.9 |
| Biejat's electorate voting Nawrocki (the exit poll) | 8.1 | 9.8 | -1.7 |
| 2020 run-off, Duda's share - the sovereignty space at Poland's own τ | 52.89 | 51.03 | +1.86 |
| 2020 run-off, Duda's share - the fitted τ (B4) | 54.83 | 51.03 | +3.80 |
| 2020 run-off, Duda's share - the fitted τ with the draft abstention | 54.83 | 51.03 | +3.80 |
| 2025 run-off, Nawrocki's share - the sovereignty space at Poland's own τ | 52.81 | 50.89 | +1.92 |
| 2025 run-off, Nawrocki's share - the fitted τ (B4) | 52.54 | 50.89 | +1.65 |
| 2025 run-off, Nawrocki's share - the fitted τ with the draft abstention | 52.32 | 50.89 | +1.43 |
| 2025 first round, the game's (fresh world): Karol Nawrocki | 29.54 | 29.54 | +0.00 |
| 2025 first round, the game's (fresh world): Rafał Trzaskowski | 31.36 | 31.36 | +0.00 |
| 2025 first round, the game's (fresh world): Sławomir Mentzen | 14.81 | 14.81 | +0.00 |
| 2025 first round, the game's (fresh world): Grzegorz Braun | 6.34 | 6.34 | +0.00 |
| 2025 first round, the game's (fresh world): Szymon Hołownia | 4.99 | 4.99 | +0.00 |
| 2025 first round, the game's (fresh world): Adrian Zandberg | 4.86 | 4.86 | +0.00 |
| 2025 first round, the game's (fresh world): Magdalena Biejat | 4.23 | 4.23 | +0.00 |
| 2025 first round, the game's (fresh world): Krzysztof Stanowski | 1.24 | 1.24 | +0.00 |
| 2025 first round, the game's (fresh world): Joanna Senyszyn | 1.09 | 1.09 | +0.00 |
| 2025 first round, the game's (fresh world): Marek Jakubiak | 0.77 | 0.77 | +0.00 |
| 2025 first round, the game's (fresh world): Artur Bartoszewicz | 0.49 | 0.49 | +0.00 |
| 2025 first round, the game's (fresh world): Maciej Maciak | 0.19 | 0.19 | +0.00 |
| 2025 first round, the game's (fresh world): Marek Woch | 0.09 | 0.09 | +0.00 |
| 2025 run-off, the game's (fresh world): Nawrocki's share of the two - Karol Nawrocki elected | 52.32 | 50.89 | +1.43 |
<!-- END GENERATED -->

## Where it stands in the game

The game's own presidential election reads it (§770, `PresidentialElection`): the run-off at `PresidentialElection.TransferTau` with the
draft abstention `PresidentialElection.AbstentionDraftMax`, the instrument failing the bar where the runtime's τ is no longer its fit or its arithmetic
differs from the runtime's. In the game a candidate of record stands **where the fit placed it** - Hołownia at Polska 2050's own row, not Trzecia
Droga's joint position (D2) - and the instrument fails where the runtime's position and its own row differ; a later candidate stands at the
backing party's position; Zandberg stands at Razem's own row and Braun at Konfederacja's (E1). The live first round reads the live prediction as *The model*'s first-round row says (E1) - a candidate of record with no roster row on its share of record, which the poll moves only through the normalisation - the prediction for Poland carrying its
history by lineage - LOW CONFIDENCE (Elias's ruling D1, §767; `PartySystems.HistoryNote`).
