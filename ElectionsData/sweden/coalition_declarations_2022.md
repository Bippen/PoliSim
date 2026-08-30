# Sweden 2022 — coalition declarations and the government that formed [SOURCED] [PROVISIONAL]

Class: SOURCED (real-world facts about parties' public commitments and about the government that
formed). `[PROVISIONAL]` until re-verified. Read by `CoalitionHarness` (W-D3) as the **declared**
red lines of §29; the **derived** red lines come from CHES 2024 positions in
`ElectionsData/positions/party_positions.md` and are not repeated here.

Vintages are stated per item and are NOT smoothed: where the verbatim statement verified is from
2017 and the corroborating conduct is from 2022, both are given, because a declared red line is a
dated fact and the date is what makes it falsifiable.

## Source register
(all accessed 2026-08-30)
- https://sv.wikipedia.org/wiki/Tid%C3%B6avtalet — the Tidö agreement: signatories, date, the
  cabinet's composition and the supporting party's status.
- https://www.liberalerna.se/wp-content/uploads/tidoavtalet-overenskommelse-for-sverige-slutlig.pdf
  — the agreement itself, as published by one of its signatories.
- https://www.svt.se/nyheter/inrikes/akesson-pressade-loof-pa-samarbete-med-sd — SVT, Annie Lööf's
  refusal in her own words (published 2017-05-14, updated 2017-05-18).
- https://en.wikipedia.org/wiki/2022_Swedish_general_election — the final seat distribution and the
  government-formation sequence.

## The government that formed (the harness's done-when reads this)

| fact | value | basis |
|---|---|---|
| agreement | Tidöavtalet ("Överenskommelse för Sverige") | signed **14 October 2022** |
| signatories | M, KD, L **and SD** | four parties |
| cabinet | **M + KD + L** (103 of 349 seats) | SD took no ministerial post |
| supporting party | **SD** (73 seats) | nine officials in the government's coordination office |
| prime minister | Ulf Kristersson (M) | elected 17 October, took office 18 October 2022 |
| arrangement | **minority government with confidence-and-supply**, 176 of 349 | §29's third outcome |

Seat figures cross-checked against `ElectionsData/sweden/returns_2022.md` (Valmyndigheten final
count): S 107, SD 73, M 68, V 24, C 24, KD 19, MP 18, L 16 = 349; majority 175.

## Declared red lines

| pair | strength | vintage | basis |
|---|---|---|---|
| **C ↔ SD** | will not sit in **or support** a government dependent on SD | statement **2017-05-14**, conduct **2022** | Annie Lööf (C), SVT Agenda: *"Jag säger bestämt nej till att samtala eller förhandla med dig i regeringsställning"* and *"För att du och ditt parti har en alldeles för stor skillnad i synen på människovärdet som jag inte kan dela."* Corroborated by C's 2022 conduct: it backed Magdalena Andersson (S) for prime minister rather than Ulf Kristersson (M), because Kristersson sought SD participation. |
| **M ↔ SD**, **KD ↔ SD**, **L ↔ SD** | will not let SD **sit in government** — but will accept its **support** | promised in the **2022 campaign**, executed **2022-10-14** | All three promised during the 2022 campaign not to let SD into government. The Tidö agreement executes exactly that: M, KD and L in cabinet; SD a cooperation partner outside it, with officials in the government's coordination office and **no ministerial post**. SVT, "Liberalerna: SD behövs inte i regeringen". **This is a cabinet-blocking line that is NOT support-blocking** — the distinction the model draws, and the one that gives the arrangement its shape. |

## What each declaration actually does (measured by `CoalitionHarness`, not asserted)

Each line was dropped on its own, everything else unchanged, and the formation re-run:

| dropped | outcome | verdict |
|---|---|---|
| nothing (all lines) | ConfidenceAndSupply, cabinet M+KD+L + SD support | the 2022 arrangement |
| C ↔ SD | ConfidenceAndSupply, cabinet M+KD+L + SD support — **unchanged** | **CORROBORATED, not load-bearing**: the DERIVED galtan rule already separates C from SD (6.05 > 5.00), so this declaration changes nothing here. Recorded rather than quietly kept as though it were doing work. |
| M,KD,L ↔ SD | MajorityCoalition, cabinet S+M+C+KD+L — **changed** | **LOAD-BEARING**: without it SD is admissible in cabinet and the Tidö shape is gone entirely. |

A declaration that changes nothing *here* still earns its place: it is the only mechanism that
can express the Liberals' reversal between 2018 and 2022, which **no position distance moved**.

## What is deliberately NOT here

- **V ↔ SD and S ↔ SD are not listed as declarations.** They are reached by the DERIVED rule from
  position distance, and adding a declaration that changes nothing would dress a derivation as a
  citation. The harness measures and reports which refusals the derived rule reaches.
- **The Liberals' reversal is the reason the declared mechanism exists at all**, and it is recorded
  here as context rather than as an active red line: L declined to back Kristersson before 2018
  because he sought SD participation, and signed the Tidö agreement with SD in 2022. No CHES
  distance moved to license that. A model with only derived red lines cannot express it.
- **Leader compatibility and personal relationships** (§29 lists both) have **no source** and are
  DEFERRED; the harness asserts by reflection that no member carries them, so they cannot be
  quietly filled in with game fiction.
