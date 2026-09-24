# Poland — the chamber, the government, the president and the party leaders of record by date, 2022-07-01 → 2026-09-24 [SOURCED] [PROVISIONAL]

Class: SOURCED (PS-1, the world clock — `docs/specs/POLITICAL_SYSTEM_SPEC.md` §9 stage 1 and §11; sourcing pass
2026-09-24, research agent). `[PROVISIONAL]` until a second session re-verifies (R-K9).

**What this file is for.** The game seats every country in its chamber of record and governs it by its government of
record on the chosen start date (§3). For Poland this file says, for every date in the window, which Sejm sat, who was
prime minister and from which parties, who was president (the spec models the presidential veto), and who led the seated
parties. It also records, with the article text, the constitutional rules the spec's §5.4 and §11 name for Poland. The
2023 seat table itself is `returns_2023.md` and is not repeated.

**Fetched, never recalled.** Every quote below was read in a page fetched on 2026-09-24 (afternoon–evening, CEST) and
saved byte for byte under `raw/records/`. Each file's SHA-256 is in `raw/records/SHA256SUMS.txt` and in the register
below; `raw/records/fetch_log.txt` is the fetch log (UTC time, HTTP status, file, URL — the two `302` lines are the
sejm.gov.pl constitution pages, which sit behind an Incapsula cookie loop and were not obtained; see GAP G1). Quotes are
Polish and verbatim; English glosses are marked *gloss:* and are mine. Lines marked **DERIVED** state their arithmetic or
reading. Quotes from the three kinds of source are distinguished:

- **HTML/JSON pages** — quoted as served, tags stripped and whitespace collapsed; each quote was checked against the
  saved file by `grep -F` on the same stripping (the check script's result is stated at the end).
- **Dz.U./M.P. PDFs** — the Sejm's ELI service serves these acts as PDF only (`"textHTML":false,"textPDF":true` in the
  metadata). Their text is in two forms: literal strings (dates, headers, the candidates' names in the annex tables),
  which are quoted as found; and CID-hex runs of TrueType subsets, which were **DECODED** by the rule read off the
  headers of the same files (`<0030><0032><0031><002C><0037><0032><0035>` spells MONITOR, so glyph id = codepoint − 0x1D
  for basic Latin, space = 3; Polish letters from the same headers: `00E1` = ł, `012A` = ż, `0109` = ę, `0105` = ą).
  A DECODED quote is marked so and is never load-bearing on its own — every date it supports is also in the ELI
  metadata's own fields (`announcementDate`, `promulgation`) or in the act's title, which are quoted as served.
- **Sejm API JSON** (`api.sejm.gov.pl`, the Sejm's own open-data service) — fields quoted as served.

Party keys are the project's (`PartySystem.cs`, Poland): **PiS**, **KO**, **TD**, **NL**, **Konf**. A Sejm club is
named by the API's own `id`.

---

## 1. THE CHAMBER OF RECORD BY DATE

| from | to | chamber | source ids |
|---|---|---|---|
| 2022-07-01 (window start) | 2023-11-12 | **Sejm of the 9th term**, elected 2019-10-13; first sitting 2019-11-12 | [API-TERM] [PKW-2019] |
| 2023-11-13 | 2026-09-24 (today; term current) | **Sejm of the 10th term**, elected 2023-10-15; first sitting 2023-11-13 | [API-TERM] [API-V1] [PKW-2023 = `returns_2023.md`] |

**The rule.** Konstytucja RP Art. 98 ust. 1 [TK-KONST]:
> "Sejm i Senat są wybierane na czteroletnie kadencje. Kadencje Sejmu i Senatu rozpoczynają się z dniem zebrania się Sejmu na pierwsze posiedzenie i trwają do dnia poprzedzającego dzień zebrania się Sejmu następnej kadencji."

*gloss:* a term begins on the day the Sejm meets for its first sitting and runs to the day before the next Sejm's first
sitting — so the 9th term's last day is 2023-11-12 and the 10th's first day is 2023-11-13, with no gap and no overlap.

**The dates, as the Sejm's own API serves them** [API-TERM]:
> `{"current":false,"from":"2019-11-12","num":9,"prints":{"count":4142,"lastChanged":"2026-02-20T09:23:07","link":"/term9/prints"},"to":"2023-11-12"},{"current":true,"from":"2023-11-13","num":10,"prints":{"count":3345,"lastChanged":"2026-09-24T15:33:09","link":"/term10/prints"}}`

The 10th term's first sitting is also dated by the sitting's own title in the voting records [API-V1]:
> `"title":"1. posiedzenie Sejmu RP w dniach 13, 14, 21, 22, 28 i 29 listopada oraz 6, 7, 11 i 12 grudnia 2023 r."`

(the same sitting's title grows as days are added; the longest served form ends "…6, 7, 11, 12, 19, 20 i 21 grudnia 2023 r.").

### 1a. The 9th-term Sejm (2019) — seats by committee, from the PKW's notice

Source: Obwieszczenie Państwowej Komisji Wyborczej z dnia 14 października 2019 r. o wynikach wyborów do Sejmu
Rzeczypospolitej Polskiej przeprowadzonych w dniu 13 października 2019 r., Dz.U. 2019 poz. 1955 [PKW-2019], Dział I,
the passage "Listy komitetów wyborczych uprawnionych do uczestniczenia w podziale mandatów uzyskały w kraju następującą
liczbę mandatów:" (quoted lines below are the notice's own, whitespace collapsed):

| committee (the notice's name) | seats | project key (**DERIVED** mapping) | the notice's line |
|---|---|---|---|
| KOMITET WYBORCZY PRAWO I SPRAWIEDLIWOŚĆ | 235 | PiS | "listy nr 2 zgłoszone przez KOMITET WYBORCZY PRAWO I SPRAWIEDLIWOŚĆ uzyskały 235 mandatów;" |
| KOALICYJNY KOMITET WYBORCZY KOALICJA OBYWATELSKA PO .N IPL ZIELONI | 134 | KO | "listy nr 5 zgłoszone przez KOALICYJNY KOMITET WYBORCZY KOALICJA OBYWATELSKA PO .N IPL ZIELONI uzyskały 134 mandaty;" |
| KOMITET WYBORCZY SOJUSZ LEWICY DEMOKRATYCZNEJ | 49 | NL (**DERIVED**: the 2019 committee was SLD's; the 10th-term key NL is used for continuity only — see the club note) | "listy nr 3 zgłoszone przez KOMITET WYBORCZY SOJUSZ LEWICY DEMOKRATYCZNEJ uzyskały 49 mandatów;" |
| KOMITET WYBORCZY POLSKIE STRONNICTWO LUDOWE | 30 | TD (**DERIVED**: PSL alone in 2019; the TD key is the 2023 coalition committee's) | "listy nr 1 zgłoszone przez KOMITET WYBORCZY POLSKIE STRONNICTWO LUDOWE uzyskały 30 mandatów;" |
| KOMITET WYBORCZY KONFEDERACJA WOLNOŚĆ I NIEPODLEGŁOŚĆ | 11 | Konf | "listy nr 4 zgłoszone przez KOMITET WYBORCZY KONFEDERACJA WOLNOŚĆ I NIEPODLEGŁOŚĆ uzyskały 11 mandatów;" |
| KOMITET WYBORCZY WYBORCÓW MNIEJSZOŚĆ NIEMIECKA | 1 | (no key; MN) | "lista nr 10 zgłoszona przez KOMITET WYBORCZY WYBORCÓW MNIEJSZOŚĆ NIEMIECKA uzyskała 1 mandat." |

**DERIVED**: 235 + 134 + 49 + 30 + 11 + 1 = 460. ⚠ The 2019 key mapping is a reading for the game's seat dictionary
only: in 2019 PSL ran alone (with Kukiz'15 candidates on its lists — not stated by the notice) and SLD ran alone; the
2023 keys TD (Trzecia Droga = Polska 2050 + PSL) and NL (Nowa Lewica) name different committees. If the 9th-term chamber is
ever seated in the game, its keys should be the 2019 committees' own, not these.

**The 9th-term Sejm's clubs at the END of the term**, as the Sejm API serves the closed term (`/term9/clubs`; the API
gives the membership as it stood, with no dates) [API-C9], `id | membersCount | name`:
> `KO | 129 | Klub Parlamentarny Koalicja Obywatelska - Platforma Obywatelska, Nowoczesna, Inicjatywa Polska, Zieloni`; `Konfederacja | 11 | Koło Poselskie Konfederacja`; `KP | 26 | Klub Parlamentarny Koalicja Polska - PSL, UED, Konserwatyści`; `Kukiz15 | 3 | Koło Poselskie Kukiz'15 - Demokracja Bezpośrednia`; `LD | 3 | Koło Parlamentarne Lewicy Demokratycznej`; `Lewica | 42 | Koalicyjny Klub Parlamentarny Lewicy (Nowa Lewica, PPS, Razem)`; `niez. | 9 | Posłowie niezrzeszeni`; `PiS | 227 | Klub Parlamentarny Prawo i Sprawiedliwość`; `Polska2050 | 6 | Koło Parlamentarne Polska 2050`; `PS | 3 | Koło Poselskie Polskie Sprawy`

**DERIVED**: 129 + 11 + 26 + 3 + 3 + 42 + 9 + 227 + 6 + 3 = 459 — one short of 460; the API's closed-term snapshot has one
seat unaccounted for (a vacancy at the term's end, or a member the snapshot omits — not resolved; GAP G2). The club
composition on any date inside 2022-07-01 → 2023-11-12 is **not on record here** (GAP G2): the API gives no join dates
for the 9th term.

### 1b. The 10th-term Sejm (2023) — seats by committee are `returns_2023.md` (PiS 194, KO 157, TD 65, NL 26, Konf 18)

**The clubs as served on 2026-09-24** (`/term10/clubs`) [API-C10], `id | membersCount | name`:
> `Centrum | 15 | Klub Parlamentarny Centrum`; `Demokracja | 4 | Koło Poselskie Demokracja Bezpośrednia`; `KO | 156 | Klub Parlamentarny Koalicja Obywatelska - Platforma Obywatelska, Nowoczesna, Inicjatywa Polska, Zieloni`; `Konfederacja | 16 | Klub Poselski Konfederacja`; `Konfederacja_KP | 3 | Koło Poselskie Konfederacja Korony Polskiej`; `Lewica | 21 | Koalicyjny Klub Parlamentarny Lewicy (Nowa Lewica, PPS, Unia Pracy)`; `niez. | 7 | Posłowie niezrzeszeni`; `PiS | 147 | Klub Parlamentarny Prawo i Sprawiedliwość`; `Polska2050 | 15 | Klub Parlamentarny Polska 2050`; `PSL-TD | 32 | Klub Parlamentarny Polskie Stronnictwo Ludowe - Trzecia Droga`; `Razem | 4 | Koło Poselskie Razem`; `RozwojPlus | 40 | Klub Parlamentarny Rozwój Plus`

**DERIVED**: 15 + 4 + 156 + 16 + 3 + 21 + 7 + 147 + 15 + 32 + 4 + 40 = 460.

**The clubs by date (DERIVED from each member's `joinDate` in [API-C10]; the API gives join dates only, not leave
dates, so a club's *formation* is dated by its members' earliest join date, and a member's *earlier* club is not
recoverable from this file):**

| club id | chair (function `przewodniczący`/`-a`, joinDate) | members' join dates (count) | reading (**DERIVED**) |
|---|---|---|---|
| PiS | Mariusz Błaszczak, 2023-11-13 | 2023-11-13 (137), 2024-06-26 (4), 2024-06-28 (1), 2024-07-23 (1), 2025-09-09 (3), 2026-09-19 (1) | the PiS committee's 194 seats sat as one club from the first sitting; the club is 147 today |
| KO | Zbigniew Konwiński, 2023-11-13 | 2023-11-13 (138), 2024-01-26 (1), 2024-04-26 (1), 2024-05-08 (2), 2024-05-22 (1), 2024-06-26 (10), 2024-06-28 (2), 2024-11-06 (1) | one club from the first sitting; 156 today |
| PSL-TD | Krzysztof Paszyk, 2023-11-13 | 2023-11-13 (31), 2024-07-23 (1) | the TD committee's 65 seats sat as TWO clubs from the first sitting: PSL-TD and Polska2050 (31 + 13 = 44 of 65 on day one by these join dates; the rest not resolved, GAP G3) |
| Polska2050 | Szymon Hołownia, 2023-11-13 | 2023-11-13 (13), 2024-06-26 (1), 2025-10-16 (1) | as above; 15 today |
| Lewica | Anna Żukowska, 2023-11-13 | 2023-11-13 (19), 2024-06-26 (1), 2026-05-15 (1) | the NL committee's 26 sat as one club (19 on day one by these dates); the club's name today names "Nowa Lewica, PPS, Unia Pracy" — Razem, named in the 9th-term club, is a separate circle now |
| Konfederacja | Grzegorz Płaczek, 2023-11-13 | 2023-11-13 (14), 2024-06-26 (2) | the Konf committee's 18 sat as one club (14 on day one by these dates); 16 today |
| Demokracja | Jarosław Sachajko, 2024-10-17 | 2024-10-17 (3), 2026-01-26 (1) | a circle formed 2024-10-17 |
| Razem | Marcelina Zawisza, 2024-11-05 | 2024-11-05 (4) | a circle formed 2024-11-05 |
| Konfederacja_KP | Włodzimierz Skalik, 2025-06-04 | 2025-06-04 (3) | a circle formed 2025-06-04 |
| Centrum | Mirosław Suchoń, 2026-02-18 | 2026-02-18 (15) | a club formed 2026-02-18 with 15 members; Paulina Hennig-Kloska (Minister Klimatu i Środowiska, [GOV-RM]) is one of them ([API-MP]: `"club":"Centrum"`) |
| RozwojPlus | Mateusz Morawiecki, 2026-07-30 | 2026-07-30 (39), 2026-08-01 (1) | **a club formed 2026-07-30 with 39 members, chaired by the former prime minister**; 40 today |
| niez. | — | 2025-08-06 (1), 2025-11-06 (1), 2025-11-19 (1), 2026-02-18 (2), 2026-05-01 (1), 2026-06-01 (1) | unattached members, 7 today |

⚠ **What this means for the spec's §4 row.** The 2023 seat table (`returns_2023.md`) is the chamber on 2023-11-13. On
2026-09-24 the same chamber sits in twelve clubs, two of them (Centrum, Rozwój Plus) formed in 2026, and PiS's club is
147 against the committee's 194. Which parties the Rozwój Plus and Centrum members belong to is **not stated by any
fetched page** (GAP G4); the API records clubs, not party membership.

### 1c. The Senate — what the game leaves out (§10 decision 8)

- Size and election: Konstytucja Art. 97 ust. 1–2 [TK-KONST]:
  > "Senat składa się ze 100 senatorów. Wybory do Senatu są powszechne, bezpośrednie i odbywają się w głosowaniu tajnym."
- The 2023 Senate: Obwieszczenie PKW z dnia 17 października 2023 r. o wynikach wyborów do Senatu Rzeczypospolitej
  Polskiej przeprowadzonych w dniu 15 października 2023 r., Dz.U. 2023 poz. 2235 [PKW-SEN-2023], Dział I rozdz. 1 pkt 2:
  > "Wybierano 100 senatorów spośród 360 kandydatów zgłoszonych przez 49 komitetów wyborczych."
  The notice lists the winner per single-member district (Dział II, 100 chapters) and gives no tally by committee; the
  by-committee composition of the Senate is **not extracted** (GAP G5). The KO party site's own claim, for what it is
  worth as a party claim: "Senatorowie 43/100" [KO-LUDZIE].
- A Senate by-election was held 2025-03-16 (ELI search result title: "Obwieszczenie Państwowej Komisji Wyborczej z dnia
  17 marca 2025 r. o wynikach wyborów uzupełniających do Senatu Rzeczypospolitej Polskiej przeprowadzonych w dniu 16
  marca 2025 r.", Dz.U. 2025 poz. 354 — seen in the search listing, not fetched).
- The Senate's term runs with the Sejm's (Art. 98 ust. 1, quoted above), so the 10th-term Senate sat from 2023-11-13 and
  the 10th-term Senate is what the game states absent for the whole 10th term; the 9th-term Senate (elected 2019-10-13,
  Dz.U. 2019 poz. 1956 per the ELI search listing, not fetched) is what is absent for 2022-07-01 → 2023-11-12.

---

## 2. THE GOVERNMENT OF RECORD BY DATE

| from | to | prime minister (party) | cabinet's parties | event at the boundary | source ids |
|---|---|---|---|---|---|
| 2022-07-01 (window start; in office since 2019-11-15) | 2023-11-13 | **Mateusz Morawiecki** (PiS) — his second government | drawn from the PiS club (GAP G6 for the coalition's constituent parties) | appointed 2019-11-15 (M.P. 2019 poz. 1091, 1092); resignation tendered at the new Sejm's first sitting and accepted 2023-11-13 (M.P. 2023 poz. 1221), the cabinet continuing in office under Art. 162 ust. 3 | [ELI-MP-2019] [ELI-MP-2023] [TK-KONST] |
| 2023-11-13 | 2023-11-27 | Morawiecki (PiS), the dismissed cabinet carrying on ("dalsze sprawowanie obowiązków") | as above | 2023-11-13 Duda designates Morawiecki (M.P. 2023 poz. 1222) | [ELI-MP-2023] |
| 2023-11-27 | 2023-12-11 | **Mateusz Morawiecki** (PiS) — his third government | GAP G6 | appointed with the cabinet 2023-11-27 (M.P. 2023 poz. 1287, 1288); **confidence vote lost 2023-12-11**: yes 190, no 266, abstain 0 (absolute majority of 229 needed); resignation accepted 2023-12-11 (M.P. 2023 poz. 1379) | [ELI-MP-2023] [API-V1] |
| 2023-12-11 | 2023-12-13 | Morawiecki's dismissed cabinet carrying on; **Donald Tusk elected prime minister by the Sejm 2023-12-11** (yes 248, no 201, abstain 0; absolute majority of 225 needed), his cabinet elected 2023-12-12 (yes 248, no 201) | — | Art. 154 ust. 3 step: Sejm's resolutions of 2023-12-11 (M.P. 2023 poz. 1380) and 2023-12-12 (poz. 1381) | [API-V1] [ELI-MP-2023] |
| 2023-12-13 | 2026-09-24 (today) | **Donald Tusk** (KO) | KO, PSL (TD), Polska 2050 (TD), Nowa Lewica (NL) — **DERIVED** from the ministers' clubs, see 2c | appointed 2023-12-13 (M.P. 2023 poz. 1382, 1383). Later: confidence vote WON 2025-06-11 (yes 243, no 210, simple majority); cabinet changes 2024-05-13, 2024-09-26, 2024-10-16, 2025-01-16, 2025-07-24, 2026-08-14 (the last: Maciej Berek dismissed) — none a change of prime minister | [ELI-MP-2023] [ELI-MP-2024] [ELI-MP-2025] [ELI-MP-2026] [MP-808] [API-VS] [GOV-RM] |

**No change of prime minister after 2023-12-13 is on record**: the ELI search of every `Postanowienie` in Monitor Polski
2024–2026 whose title contains "Rady Ministrów" [ELI-MP-2024] [ELI-MP-2025] [ELI-MP-2026] lists only "o zmianie w
składzie Rady Ministrów" and "o powołaniu w skład Rady Ministrów" instruments — no "o powołaniu Prezesa Rady
Ministrów", no "o przyjęciu dymisji Rady Ministrów" after 2023-12-13. **DERIVED** reading of the listings; the search
is by title and would miss an instrument titled otherwise (GAP G7).

### 2a. The instruments, as the ELI service titles them (title strings verbatim from the search responses)

[ELI-MP-2019]:
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 12 listopada 2019 r. nr 1131.21.2019 o przyjęciu dymisji Rady Ministrów" (M.P. 2019 poz. 1069)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 14 listopada 2019 r. nr 1131.22.2019 o desygnowaniu Prezesa Rady Ministrów" (poz. 1090)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 15 listopada 2019 r. nr 1131.23.2019 o powołaniu Prezesa Rady Ministrów" (poz. 1091)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 15 listopada 2019 r. nr 1131.24.2019 o powołaniu w skład Rady Ministrów" (poz. 1092)

[ELI-MP-2023]:
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 13 listopada 2023 r. nr 1131.34.2023 o przyjęciu dymisji Rady Ministrów" (M.P. 2023 poz. 1221)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 13 listopada 2023 r. nr 1131.35.2023 o desygnowaniu Prezesa Rady Ministrów" (poz. 1222)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 27 listopada 2023 r. nr 1131.38.2023 o powołaniu Prezesa Rady Ministrów" (poz. 1287)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 27 listopada 2023 r. nr 1131.39.2023 o powołaniu w skład Rady Ministrów" (poz. 1288)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 11 grudnia 2023 r. nr 1131.43.2023 o przyjęciu dymisji Rady Ministrów" (poz. 1379)
> "Uchwała Sejmu Rzeczypospolitej Polskiej z dnia 11 grudnia 2023 r. w sprawie wyboru Prezesa Rady Ministrów" (poz. 1380)
> "Uchwała Sejmu Rzeczypospolitej Polskiej z dnia 12 grudnia 2023 r. w sprawie wyboru w skład Rady Ministrów" (poz. 1381)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 13 grudnia 2023 r. nr 1131.44.2023 o powołaniu Prezesa Rady Ministrów" (poz. 1382)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 13 grudnia 2023 r. nr 1131.45.2023 o powołaniu w skład Rady Ministrów" (poz. 1383)

The instruments name no person in their titles; the persons come from the Sejm's votes (2b) and the M.P. 2026 poz. 808
text (below). The 2023 Morawiecki instruments' bodies were not decoded (their PDFs were not fetched; GAP G8) — the
identity of the prime minister designated on 2023-11-13 and appointed on 2023-11-27 rests on the Sejm's vote record of
2023-12-11, whose motion names him: `"topic":"Wniosek Prezesa Rady Ministrów Pana Mateusza Morawieckiego o udzielenie wotum zaufania Radzie Ministrów"` [API-V1].

[ELI-MP-2024] [ELI-MP-2025] [ELI-MP-2026], the later changes (titles verbatim):
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 13 maja 2024 r. nr 1131.9.2024 o zmianie w składzie Rady Ministrów" (M.P. 2024 poz. 359) and "… nr 1131.10.2024 o powołaniu w skład Rady Ministrów" (poz. 360)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 26 września 2024 r. nr 1131.20.2024 o zmianie w składzie Rady Ministrów" (poz. 841)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 16 października 2024 r. nr 1131.24.2024 o zmianie w składzie Rady Ministrów" (poz. 888) and "… nr 1131.25.2024 o powołaniu w skład Rady Ministrów" (poz. 889)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 16 stycznia 2025 r. nr 1131.1.2025 o zmianie w składzie Rady Ministrów" (M.P. 2025 poz. 55) and "… nr 1131.2.2025 o powołaniu w skład Rady Ministrów" (poz. 56)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 24 lipca 2025 r. nr 1131.27.2025 o zmianie w składzie Rady Ministrów" (poz. 689) and "… nr 1131.28.2025 o zmianie w składzie Rady Ministrów" (poz. 690)
> "Postanowienie Prezydenta Rzeczypospolitej Polskiej z dnia 14 sierpnia 2026 r. nr 1131.30.2026 o zmianie w składzie Rady Ministrów" (M.P. 2026 poz. 808)

The 2026 instrument's text [MP-808], literal strings as found in the PDF: "Warszawa, dnia", "17 sierpnia 2026", "POSTANOWIENIE",
"PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ", "z dnia 14", "sierpnia 2026", "r 1131.30.2026", "zmianie w", "Na podstawie art. 161 Konstytucji Rzeczypospolitej Polskiej z dnia 2 kwietnia 1997 r., na wniosek Prezesa Rady" (the plain
words run one per line in the file), "Pana Macieja Berka ze", "Rady", "Ministra do spraw Nadzoru nad", "Polityki",
"K. Nawrocki", "D. Tusk". **DECODED** (the CID runs joined to the plain words): "na wniosek Prezesa Rady Ministrów,
odwołuję Pana Macieja Berka ze składu Rady Ministrów, z urzędu Ministra do spraw Nadzoru nad Wdrażaniem Polityki
Rządu" — signed "Prezydent Rzeczypospolitej Polskiej: K. Nawrocki", "Prezes Rady Ministrów: D. Tusk". The metadata
[MP-808-META]: `"announcementDate":"2026-08-14"`, `"promulgation":"2026-08-17"`, `"entryIntoForce":"2026-08-14"`.

### 2b. The Sejm's votes (the Sejm API, `/term10/votings/1` and the `wotum zaufania` search), fields as served

**The failed confidence vote, 2023-12-11** [API-V1]:
> `"abstain":0,"date":"2023-12-11T16:17:02","kind":"ELECTRONIC","majorityType":"ABSOLUTE_MAJORITY","majorityVotes":229,"no":266,"notParticipating":3,"present":0,"sitting":1,"sittingDay":9,"term":10,"title":"Pkt. 36 Przedstawienie przez Prezesa Rady Ministrów programu działania Rady Ministrów z wnioskiem o udzielenie jej wotum zaufania","topic":"Wniosek Prezesa Rady Ministrów Pana Mateusza Morawieckiego o udzielenie wotum zaufania Radzie Ministrów","totalVoted":456,"votingNumber":102,"yes":190`

**DERIVED**: 190 for against a required 229 (the absolute majority of the 456 voting): the motion failed; the cabinet's
resignation followed the same day (M.P. 2023 poz. 1379, Art. 162 ust. 2 pkt 1).

**The election of Donald Tusk as prime minister, 2023-12-11** [API-V1]:
> `"abstain":0,"date":"2023-12-11T18:49:03","description":"wniosek w sprawie wyboru Donalda Tuska na Prezesa Rady Ministrów ","kind":"ELECTRONIC","majorityType":"ABSOLUTE_MAJORITY","majorityVotes":225,"no":201,"notParticipating":10,"present":0,"sitting":1,"sittingDay":9,"term":10,"title":"Pkt. 37 Wybór Prezesa Rady Ministrów (druk nr 95)","topic":"Wniosek w sprawie wyboru Pana Donalda Franciszka Tuska na Prezesa Rady Ministrów","totalVoted":449,"votingNumber":103,"yes":248`

**The election of his cabinet, 2023-12-12** [API-V1]:
> `"abstain":0,"date":"2023-12-12T21:53:32","description":"wniosek Prezesa Rady Ministrów Donalda Tuska w sprawie wyboru członków Rady Ministrów ","kind":"ELECTRONIC","majorityType":"ABSOLUTE_MAJORITY","majorityVotes":225,"no":201,"notParticipating":10,"present":0,"sitting":1,"sittingDay":10,"term":10,"title":"Pkt. 38 Przedstawienie przez Prezesa Rady Ministrów programu działania oraz składu Rady Ministrów wraz z wnioskiem w sprawie wyboru członków Rady Ministrów (druk nr 96)","topic":"Wniosek Prezesa Rady Ministrów Pana Donalda Tuska w sprawie wyboru członków Rady Ministrów.","totalVoted":449,"votingNumber":105,"yes":248`

**DERIVED**: these are the Art. 154 ust. 3 step (the Sejm elects the prime minister and the cabinet he proposes, by an
absolute majority), taken because the Art. 154 ust. 2 vote failed; the president then appointed the elected cabinet on
2023-12-13 (poz. 1382, 1383; Art. 154 ust. 3 last sentence). The 10th-term Sejm never reached Art. 155.

**Tusk's confidence vote, 2025-06-11** [API-VS] (the API's `votings/search?title=wotum zaufania` returns exactly two
records for the term: the 2023-12-11 one above and this one):
> `"abstain":0,"date":"2025-06-11T16:33:00","description":"wniosek Prezesa Rady Ministrów o wyrażenie przez Sejm Rzeczypospolitej Polskiej wotum zaufania Radzie Ministrów","kind":"ELECTRONIC","majorityType":"SIMPLE_MAJORITY","majorityVotes":211,"no":210,"notParticipating":7,"present":0,"sitting":36,"sittingDay":3,"term":10,"title":"Pkt. 19 Rozpatrzenie wniosku Prezesa Rady Ministrów o wyrażenie przez Sejm Rzeczypospolitej Polskiej wotum zaufania Radzie Ministrów (druk nr 1350)","topic":"Wniosek Prezesa Rady Ministrów Pana Donalda Tuska o wyrażenie wotum zaufania Radzie Ministrów","totalVoted":453,"votingNumber":20,"yes":243`

**DERIVED**: an Art. 160 vote (the prime minister's own request; simple majority), won 243 to 210, ten days after the
presidential run-off of 2025-06-01 (section 3). Whether any constructive no-confidence motion (Art. 158) was tabled in
the term is not answered by this search (its title filter is "wotum zaufania", not "wotum nieufności"; GAP G9).

### 2c. The cabinet on 2026-09-24 and its parties (DERIVED)

The Chancellery's page "Skład Rady Ministrów" [GOV-RM], as served (names and offices verbatim, whitespace collapsed):
> "Donald Tusk Prezes Rady Ministrów Władysław Kosiniak-Kamysz Wiceprezes Rady Ministrów, Minister Obrony Narodowej Radosław Sikorski Wiceprezes Rady Ministrów, Minister Spraw Zagranicznych Krzysztof Gawkowski Wiceprezes Rady Ministrów, Minister Cyfryzacji Wojciech Balczun Minister Aktywów Państwowych Marta Cienkowska Minister Kultury i Dziedzictwa Narodowego Andrzej Domański Minister Finansów i Gospodarki Agnieszka Dziemianowicz-Bąk Ministra Rodziny, Pracy i Polityki Społecznej Jan Grabiec Minister - Członek Rady Ministrów, Szef Kancelarii Prezesa Rady Ministrów, Przewodniczący Komitetu do spraw Pożytku Publicznego Paulina Hennig-Kloska Minister Klimatu i Środowiska Marcin Kierwiński Minister Spraw Wewnętrznych i Administracji Dariusz Klimczak Minister Infrastruktury Marcin Kulasek Minister Nauki i Szkolnictwa Wyższego Stefan Krajewski Minister Rolnictwa i Rozwoju Wsi Miłosz Motyka Minister Energii Barbara Nowacka Minister Edukacji Katarzyna Pełczyńska-Nałęcz Minister Funduszy i Polityki Regionalnej Jakub Rutnicki Minister Sportu i Turystyki Tomasz Siemoniak Minister, członek Rady Ministrów, Koordynator Służb Specjalnych Jolanta Sobierańska-Grenda Minister Zdrowia Waldemar Żurek Minister Sprawiedliwości"

gov.pl states no party for anyone. The parties are **DERIVED** by reading each minister who is a deputy in the Sejm's
MP list [API-MP] (`"club"` field, as served 2026-09-24):

| minister | Sejm club [API-MP] | party reading (**DERIVED**) |
|---|---|---|
| Donald Tusk | `KO` | KO (its chairman, [KO-LUDZIE]) |
| Władysław Kosiniak-Kamysz | `PSL-TD` | PSL — the TD committee's PSL half |
| Krzysztof Gawkowski | `Lewica` | Nowa Lewica (its first vice-chairman, [NL-WLADZE]) |
| Andrzej Domański, Jan Grabiec, Marcin Kierwiński (`"active":false` — no longer a sitting deputy), Barbara Nowacka, Jakub Rutnicki, Tomasz Siemoniak | `KO` | KO |
| Agnieszka Dziemianowicz-Bąk, Marcin Kulasek | `Lewica` | Nowa Lewica (a vice-chairwoman and the secretary-general, [NL-WLADZE]) |
| Dariusz Klimczak, Stefan Krajewski | `PSL-TD` | PSL |
| Paulina Hennig-Kloska | `Centrum` | the Centrum club (formed 2026-02-18); her party is not stated by any fetched page (GAP G4) |
| Katarzyna Pełczyńska-Nałęcz | not a deputy | Polska 2050 — its chairwoman since 2026-01-31 [P2050-NEWS] [P2050-LUDZIE] |
| Radosław Sikorski, Wojciech Balczun, Marta Cienkowska, Miłosz Motyka, Jolanta Sobierańska-Grenda, Waldemar Żurek | not found in the MP list by surname | party not sourced here (GAP G10) |

**DERIVED reading**: the Tusk cabinet on 2026-09-24 is drawn from KO, PSL, Polska 2050 and Nowa Lewica — the four
parties of the three committees KO, TD and NL — with one minister sitting in the 2026 Centrum club. The cabinet's party
composition on 2023-12-13 (the M.P. 2023 poz. 1381 list) was not decoded (GAP G8); the reading that the same four
parties formed it rests on the club structure of the day-one Sejm (1b) and on nothing fetched that says so in terms.

### 2d. The presidents of record by date

| from | to | president | event | source ids |
|---|---|---|---|---|
| 2022-07-01 (window start; in office since 2020-08-06) | 2025-08-06 | **Andrzej Duda** | took the oath before the National Assembly 2020-08-06 | [ZN-2020] [ZN-2020-META] |
| 2025-08-06 | 2026-09-24 (today) | **Karol Tadeusz Nawrocki** | won the run-off of 2025-06-01 (PKW notice of 2025-06-02, Dz.U. 2025 poz. 714); took the oath before the National Assembly 2025-08-06 at 10.00 | [PKW-PREZ-2025] [ZN-2025-CONV] [ZN-2025] [ZN-2025-META] |

- The oath rule, Konstytucja Art. 130 [TK-KONST]: "Prezydent Rzeczypospolitej obejmuje urząd po złożeniu wobec Zgromadzenia Narodowego następującej przysięgi: …" *(gloss: the president assumes office on taking the oath before the National Assembly)*.
- 2020: the protocol's metadata [ZN-2020-META]: `"announcementDate":"2020-08-06"`, `"promulgation":"2020-08-12"`, title "Protokół Zgromadzenia Narodowego zwołanego w celu złożenia przysięgi przez nowo wybranego Prezydenta Rzeczypospolitej Polskiej" (M.P. 2020 poz. 712). Its text [ZN-2020], literal strings: "Warszawa, dnia 6 sierpnia 2020 r.," "sala obrad Sejmu Rzeczypospolitej Polskiej."; **DECODED**: "prosi Prezydenta Rzeczypospolitej Polskiej Andrzeja Dudę o złożenie przysięgi" and "Marszałek Sejmu stwierdza, że Prezydent Rzeczypospolitej Polskiej Andrzej Duda złożył wobec Zgromadzenia Narodo[wego przysięgę]".
- 2025: the convening order [ZN-2025-CONV] (M.P. 2025 poz. 663), literal strings: "z dnia 7 lipca 2025 r." and, **DECODED**: "POSTANOWIENIE MARSZAŁKA SEJMU RZECZYPOSPOLITEJ POLSKIEJ z dnia 7 lipca 2025 r. w sprawie zwołania Zgromadzenia Narodowego w celu złożenia przysięgi przez nowo wybranego Prezydenta Rzeczypospolitej Polskiej … § 2. Zgromadzenie Narodowe odbędzie się w Warszawie w sali posiedzeń Sejmu dnia 6 sierpnia 2025 r. o godz. 10.00." signed "Marszałek Sejmu: S. Hołownia". The protocol's metadata [ZN-2025-META]: `"announcementDate":"2025-08-06"`, `"promulgation":"2025-08-29"` (M.P. 2025 poz. 867); its text [ZN-2025], literal: "Warszawa, dnia 6 sierpnia 2025 r., sala obrad Sejmu Rzeczypospolitej Polskiej."; **DECODED**: "Marszałek Sejmu stwierdza, że Prezydent Rzeczypospolitej Polskiej Karol Tadeusz Nawrocki złożył wobec Zgromadzenia [Narodowego przysięgę]".
- The election: the PKW's notice [PKW-PREZ-2025], title as the ELI search lists it: "Obwieszczenie Państwowej Komisji Wyborczej z dnia 2 czerwca 2025 r. o wynikach ponownego głosowania i wyniku wyborów Prezydenta Rzeczypospolitej Polskiej" (Dz.U. 2025 poz. 714); the first round's: "Obwieszczenie Państwowej Komisji Wyborczej z dnia 19 maja 2025 r. o wynikach głosowania i wyniku wyborów Prezydenta Rzeczypospolitej Polskiej zarządzonych na dzień 18 maja 2025 r." (poz. 652, not fetched). In the fetched PDF the literal strings include "Warszawa, dnia 2 czerwca 2025 r." and, in the annex table of results, the two candidates' names "TRZASKOWSKI Rafał" and "NAWROCKI Karol Tadeusz"; **DECODED** from the body: "Państwowa Komisja Wyborcza stwierdziła, iż w ponownym głosowaniu w dniu 1 czerwca 2025 r., spośród dwóch kandydatów na Prezydenta Rzeczypospolitej Polskiej więcej głosów otrzymał i – stosownie do art. 127 ust. 6 Konstytucji Rzeczypospolitej Polskiej oraz art. 292 § 4 Kodeksu wyborczego – na Prezydenta Rzeczypospolitej Polskiej zo[stał wybrany] …" and "frekwencja wyniosła 71,63 %". ⚠ **The winner's name and the two vote figures are set in a third font whose runs the decoder does not reach** (the fragment "286 głosów, tj. 49,11 % liczby głosów ważnych" is the second-listed candidate's tail, unattributed) — so the vote counts are a GAP (G11) and the winner's identity rests on the National Assembly protocol of 2025-08-06, which is unambiguous.

---

## 3. PARTY LEADERS OF THE SEATED PARTIES BY DATE

Each from the party's own site as served 2026-09-24 unless marked. "Office word" is the site's own.

| party (key) | leader | office word (the site's) | from | to | source | change inside the window |
|---|---|---|---|---|---|---|
| Prawo i Sprawiedliwość (PiS) | Jarosław Kaczyński | "Prezes Partii" | before the window | today | [PIS-WLADZE]: "Jarosław Kaczyński Prezes Partii" | none on record; the page carries no dates (GAP G12) |
| Koalicja Obywatelska / Platforma Obywatelska (KO) | Donald Tusk | "Przewodniczący" | before the window | today | [KO-LUDZIE]: "Przewodniczący Donald Tusk Wiceprzewodniczący Bartosz Arłukowicz Borys Budka …" | none on record. ⚠ The site is koalicjaobywatelska.pl and names the party "Koalicja Obywatelska"; platforma.pl did not answer (connection failed). Whether and when PO became KO as a party is not stated by any fetched page (GAP G13) |
| Polskie Stronnictwo Ludowe (PSL; the TD committee's PSL half) | — | — | — | — | **GAP G14**: psl.pl timed out on every TLS variant tried (four); no official page fetched. The Sejm club "PSL-TD" is chaired by Krzysztof Paszyk [API-C10] — a club office, not the party's |
| Polska 2050 (the TD committee's other half) | Szymon Hołownia → **Katarzyna Pełczyńska-Nałęcz** | "Przewodnicząca Partii" (Pełczyńska-Nałęcz); Hołownia now "Członek Zarządu Krajowego" | Pełczyńska-Nałęcz from 2026-01-31 | today | [P2050-NEWS] (the page's own attributes: `article:published_time" content="2026-01-31T21:31:50+00:00"` and `"datePublished":"2026-01-31T22:31:50+01:00"`; shown on the page as "sob. 31 sty 2026"): "31 stycznia odbyła się druga tura wyborów przewodniczącej Polski 2050. Głosowanie zakończyło się o godz. 22:00. Państwowa Komisja Wyborcza po przeliczeniu głosów podała informację, że nową przewodniczącą Polski 2050 została Katarzyna Pełczyńska-Nałęcz z wynikiem 350 głosów. Paulina Hennig-Kloska otrzymałą 309 głosów." [sic]; [P2050-LUDZIE]: "Zarząd krajowy Katarzyna Pełczyńska – Nałęcz Przewodnicząca Partii Ewa Schädler I Wiceprzewodnicząca Partii … Szymon Hołownia Członek Zarządu Krajowego" (the dash is the page's `&#8211;` entity) | **2026-01-31**. Who held the party chair before that date is **not stated by any fetched page** (GAP G15): the article says "nową przewodniczącą" without naming the predecessor; Hołownia's own page on the site was not fetched. Hołownia chairs the Sejm club Polska2050 (function "przewodniczący", joinDate 2023-11-13 [API-C10]) and was Marshal of the Sejm on 2025-07-07 ("Marszałek Sejmu: S. Hołownia" [ZN-2025-CONV]) |
| Nowa Lewica (NL) | Włodzimierz Czarzasty | "Przewodniczący" | on record today | today | [NL-WLADZE]: "Władze i kadra Zarząd krajowy Włodzimierz Czarzasty Przewodniczący … Marcin Kulasek Sekretarz Generalny … Krzysztof Gawkowski Pierwszy Wiceprzewodniczący" | the date from which Czarzasty holds the chair alone, and any earlier co-chair arrangement, is **not stated** by the page (GAP G16) |
| Konfederacja Wolność i Niepodległość (Konf) | no single leader — a "Rada Liderów" | "Rada Liderów Konfederacji" | on record today | today | [KONF-O]: "Rada Liderów Konfederacji Wszystkie najważniejsze decyzje w Konfederacji podejmowane są przez Radę Liderów. Skład Rady Liderów : Konrad Berkowicz Bartosz Bocheńczak Krzysztof Bosak Anna Bryłka Bartłomiej Pejo Grzegorz Płaczek Marcin Sypniewski Marek Szewczyk Krzysztof Tuduj Witold Tumanowicz Stanisław Tyszka Paweł Usiądek" | no dates on the page (GAP G17). Its constituent parties' leaders (Nowa Nadzieja, Ruch Narodowy) are **not sourced**: ruchnarodowy.net redirected to an unrelated domain; the Nowa Nadzieja party's domain was not found (nowanadzieja.pl is a therapy centre) (GAP G18). Note the page's own count "W Sejmie X kadencji zasiada 18. posłów Konfederacji" is stale against the API's 16 + 3 (1b) |
| the clubs formed inside the term — Centrum (2026-02-18), Rozwój Plus (2026-07-30), Razem (2024-11-05), Konfederacja Korony Polskiej (2025-06-04), Demokracja Bezpośrednia (2024-10-17) | club chairs only: Mirosław Suchoń; Mateusz Morawiecki; Marcelina Zawisza; Włodzimierz Skalik; Jarosław Sachajko | "przewodniczący"/"przewodnicząca" of the club [API-C10] | the club's formation date (1b) | today | [API-C10] | their **party** offices and any party behind Centrum or Rozwój Plus are **not sourced** (GAP G4, G19) |

---

## 4. THE CONSTITUTIONAL AND STATUTORY RULES (§5.4, §11)

All constitution quotes are from the Constitutional Tribunal's page of the Constitution [TK-KONST] as served
(the sejm.gov.pl pages were not obtainable, GAP G1; the ISAP consolidated PDF [ISAP-KONST] was saved as the statutory
text of record but its glyph encoding was not decoded). ⚠ The Tribunal page's markup carries the ustęp numbers outside
the text nodes, so the quotes below run the ustępy together without their "1.", "2." numbers; the split is marked
**DERIVED** where it matters.

**Art. 158 — the constructive vote of no confidence:**
> "Artykuł 158 Sejm wyraża Radzie Ministrów wotum nieufności większością ustawowej liczby posłów na wniosek zgłoszony przez co najmniej 46 posłów i wskazujący imiennie kandydata na Prezesa Rady Ministrów. Jeżeli uchwała została przyjęta przez Sejm, Prezydent Rzeczypospolitej przyjmuje dymisję Rady Ministrów i powołuje wybranego przez Sejm nowego Prezesa Rady Ministrów, a na jego wniosek pozostałych członków Rady Ministrów oraz odbiera od nich przysięgę. Wniosek o podjęcie uchwały, o której mowa w ust. 1, może być poddany pod głosowanie nie wcześniej niż po upływie 7 dni od dnia jego zgłoszenia. Powtórny wniosek może być zgłoszony nie wcześniej niż po upływie 3 miesięcy od dnia zgłoszenia poprzedniego wniosku. Powtórny wniosek może być zgłoszony przed upływem 3 miesięcy, jeżeli wystąpi z nim co najmniej 115 posłów."

*gloss:* a no-confidence vote needs a majority of the statutory number of deputies (**DERIVED**: 231 of 460), a motion
by at least 46 deputies naming a candidate for prime minister; if carried, the president accepts the cabinet's
resignation and appoints the Sejm's chosen prime minister; the vote is no earlier than 7 days after tabling; a repeat
motion waits 3 months unless 115 deputies bring it.

**Art. 154 — investiture, the three steps** (ust. 1: designation and appointment; ust. 2: the confidence vote by
absolute majority within 14 days; ust. 3: failing that, the Sejm elects):
> "Artykuł 154 Prezydent Rzeczypospolitej desygnuje Prezesa Rady Ministrów, który proponuje skład Rady Ministrów. Prezydent Rzeczypospolitej powołuje Prezesa Rady Ministrów wraz z pozostałymi członkami Rady Ministrów w ciągu 14 dni od dnia pierwszego posiedzenia Sejmu lub przyjęcia dymisji poprzedniej Rady Ministrów i odbiera przysięgę od członków nowo powołanej Rady Ministrów. Prezes Rady Ministrów, w ciągu 14 dni od dnia powołania przez Prezydenta Rzeczypospolitej, przedstawia Sejmowi program działania Rady Ministrów z wnioskiem o udzielenie jej wotum zaufania. Wotum zaufania Sejm uchwala bezwzględną większością głosów w obecności co najmniej połowy ustawowej liczby posłów. W razie niepowołania Rady Ministrów w trybie ust. 1 lub nieudzielenia jej wotum zaufania w trybie ust. 2 Sejm w ciągu 14 dni od upływu terminów określonych w ust. 1 lub ust. 2 wybiera Prezesa Rady Ministrów oraz proponowanych przez niego członków Rady Ministrów bezwzględną większością głosów w obecności co najmniej połowy ustawowej liczby posłów. Prezydent Rzeczypospolitej powołuje tak wybraną Radę Ministrów i odbiera przysięgę od jej członków."

**Art. 155 — the third step and the dissolution:**
> "Artykuł 155 W razie niepowołania Rady Ministrów w trybie art. 154 ust. 3 Prezydent Rzeczypospolitej w ciągu 14 dni powołuje Prezesa Rady Ministrów i na jego wniosek pozostałych członków Rady Ministrów oraz odbiera od nich przysięgę. Sejm w ciągu 14 dni od dnia powołania Rady Ministrów przez Prezydenta Rzeczypospolitej udziela jej wotum zaufania większością głosów w obecności co najmniej połowy ustawowej liczby posłów. W razie nieudzielenia Radzie Ministrów wotum zaufania w trybie określonym w ust. 1, Prezydent Rzeczypospolitej skraca kadencję Sejmu i zarządza wybory."

**DERIVED, the 2023 walk-through against these steps:** first sitting 2023-11-13 → designation the same day (poz. 1222)
→ appointment 2023-11-27, day 14 (poz. 1287/1288; within Art. 154 ust. 1's 14 days) → confidence vote 2023-12-11, day 14
after appointment (within ust. 2's 14 days), lost 190–266 → the Sejm's own election the same evening (ust. 3), 248–201
→ appointment 2023-12-13. Art. 155 was not reached.

**Art. 160 — the confidence vote at the prime minister's request (the 2025-06-11 vote):**
> "Artykuł 160 Prezes Rady Ministrów może zwrócić się do Sejmu o wyrażenie Radzie Ministrów wotum zaufania. Udzielenie wotum zaufania Radzie Ministrów następuje większością głosów w obecności co najmniej połowy ustawowej liczby posłów."

**Art. 161–162 — cabinet changes; resignation at the first sitting:**
> "Artykuł 161 Prezydent Rzeczypospolitej, na wniosek Prezesa Rady Ministrów, dokonuje zmian w składzie Rady Ministrów."
>
> "Artykuł 162 Prezes Rady Ministrów składa dymisję Rady Ministrów na pierwszym posiedzeniu nowo wybranego Sejmu. Prezes Rady Ministrów składa dymisję Rady Ministrów również w razie: ) nieuchwalenia przez Sejm wotum zaufania dla Rady Ministrów, ) wyrażenia Radzie Ministrów wotum nieufności, ) rezygnacji Prezesa Rady Ministrów. Prezydent Rzeczypospolitej, przyjmując dymisję Rady Ministrów, powierza jej dalsze sprawowanie obowiązków do czasu powołania nowej Rady Ministrów. Prezydent Rzeczypospolitej, w przypadku określonym w ust. 2 pkt 3, może odmówić przyjęcia dymisji Rady Ministrów."

(the ") " marks are where the page's markup holds the pkt numbers 1)–3)).

**Art. 122 — the presidential veto and its override (ust. 5; the 3/5 majority):**
> "Artykuł 122 Po zakończeniu postępowania określonego w art. 121 Marszałek Sejmu przedstawia uchwaloną ustawę do podpisu Prezydentowi Rzeczypospolitej. Prezydent Rzeczypospolitej podpisuje ustawę w ciągu 21 dni od dnia przedstawienia i zarządza jej ogłoszenie w Dzienniku Ustaw Rzeczypospolitej Polskiej. Przed podpisaniem ustawy Prezydent Rzeczypospolitej może wystąpić do Trybunału Konstytucyjnego z wnioskiem w sprawie zgodności ustawy z Konstytucją. Prezydent Rzeczypospolitej nie może odmówić podpisania ustawy, którą Trybunał Konstytucyjny uznał za zgodną z Konstytucją. Prezydent Rzeczypospolitej odmawia podpisania ustawy, którą Trybunał Konstytucyjny uznał za niezgodną z Konstytucją. Jeżeli jednak niezgodność z Konstytucją dotyczy poszczególnych przepisów ustawy, a Trybunał Konstytucyjny nie orzeknie, że są one nierozerwalnie związane z całą ustawą, Prezydent Rzeczypospolitej, po zasięgnięciu opinii Marszałka Sejmu, podpisuje ustawę z pominięciem przepisów uznanych za niezgodne z Konstytucją albo zwraca ustawę Sejmowi w celu usunięcia niezgodności. Jeżeli Prezydent Rzeczypospolitej nie wystąpił z wnioskiem do Trybunału Konstytucyjnego w trybie ust. 3, może z umotywowanym wnioskiem przekazać ustawę Sejmowi do ponownego rozpatrzenia. Po ponownym uchwaleniu ustawy przez Sejm większością 3/5 głosów w obecności co najmniej połowy ustawowej liczby posłów Prezydent Rzeczypospolitej w ciągu 7 dni podpisuje ustawę i zarządza jej ogłoszenie w Dzienniku Ustaw Rzeczypospolitej Polskiej."

*gloss:* the president may return a bill to the Sejm with reasons (the veto, ust. 5); the Sejm overrides by re-passing
it with 3/5 of the votes with at least half the statutory number present (**DERIVED**: 276 of 460 if all vote), after
which the president signs within 7 days. The Senate has no part in the override.

**Art. 98 ust. 1–2 (term; when elections are called) and Art. 97 — quoted in section 1. Art. 130 — in 2d.**

**The thresholds — Kodeks wyborczy Art. 196 § 1–2 and Art. 197 § 1** [KW-2011] (the ELI service's text of Dz.U. 2011
nr 21 poz. 112 — the ORIGINAL 2011 text, the same caveat as `returns_2023.md`'s: the 2023 election ran under the
consolidated text and the PKW's 2023 notice cites these same article numbers):
> "Art. 196. § 1. W podziale mandatów w okręgach wyborczych uwzględnia się wyłącznie listy kandydatów na posłów tych komitetów wyborczych, których listy otrzymały co najmniej 5% ważnie oddanych głosów w skali kraju. § 2. Listy kandydatów na posłów koalicyjnych komitetów wyborczych uwzględnia się w podziale mandatów w okręgach wyborczych, jeżeli ich listy otrzymały co najmniej 8% ważnie oddanych głosów w skali kraju. Art. 197. § 1. Komitety wyborcze utworzone przez wyborców zrzeszonych w zarejestrowanych organizacjach mniejszości narodowych mogą korzystać ze zwolnienia list tych komitetów z warunku, o którym mowa w art. 196 § 1, jeżeli złożą Państwowej Komisji Wyborczej oświadczenie w tej sprawie najpóźniej w 5 dniu przed dniem wyborów."

---

## 5. Against the spec's §4 row and `returns_2023.md`

- **"the PiS government at the start" (§9 stage 5) and "Morawiecki's PiS government" (§4)** — true for the §4 start
  (the standard run-up before 2023-10-15): Morawiecki was prime minister from 2019-11-15 through the election and until
  2023-12-13 (2, 2a). The spec's start rule's date itself (26 weeks + 8 weeks before 2023-10-15) is not computed here.
- **"directly elected president with a veto"** — the veto is Art. 122 ust. 5, override 3/5 (4). The president on the
  §4 start date and until 2025-08-06 was Andrzej Duda; since 2025-08-06 Karol Nawrocki (2d). ⚠ A game that starts
  before 2023-10-15 and runs to today therefore crosses a presidential election on 2025-05-18 / 2025-06-01 and an
  inauguration on 2025-08-06 — the spec's Poland row does not mention the presidential calendar.
- **`returns_2023.md`** — nothing fetched contradicts its seat table (PiS 194, KO 157, TD 65, NL 26, Konf 18) or its
  threshold articles. What it does not say and this file adds: the TD committee sat as two clubs from day one (PSL-TD
  and Polska2050); the chamber's club shape on 2026-09-24 is twelve clubs, with PiS at 147 and a 40-member club under
  Morawiecki formed 2026-07-30 (1b).
- **§10 decision 8 (Senate absent)** — the Senate is 100 senators (Art. 97), elected with the Sejm in 100 single-member
  districts (`returns_2023.md`), and it has no part in investiture (Art. 154–155), confidence (Art. 158–160) or the veto
  override (Art. 122 ust. 5) — the rules the game models. What leaving it out omits is the legislative stage of Art. 121
  (not quoted here).

## Source register
(all accessed 2026-09-24; saved as `raw/records/<file>`; "page's own date" is the date the page itself shows, or its
metadata where the page shows none, as marked)

| id | URL | publisher | page's own date | basis | file | bytes | SHA-256 |
|---|---|---|---|---|---|---|---|
| [TK-KONST] | https://trybunal.gov.pl/o-trybunale/akty-normatywne/konstytucja-rzeczypospolitej-polskiej/ | Trybunał Konstytucyjny | none shown (the Constitution of 1997-04-02) | the Tribunal's HTML text of the Constitution | `trybunal_konstytucja.html` | 146282 | see SHA256SUMS |
| [ISAP-KONST] | https://isap.sejm.gov.pl/isap.nsf/download.xsp/WDU19970780483/U/D19970483Lj.pdf | Sejm (ISAP) | consolidated text ("Lj") | statutory text of record, PDF (glyph-encoded; not decoded) | `isap_WDU19970780483_konstytucja_Lj.pdf` | 293929 | see SHA256SUMS |
| [KW-2011] | https://eli.gov.pl/api/acts/DU/2011/112/text.html | Sejm (ELI) | Dz.U. 2011 nr 21 poz. 112, original text | Electoral Code, 2011 text | `eli_DU_2011_112_kodeks_wyborczy.html` | 2420401 | see SHA256SUMS |
| [PKW-2019] | https://eli.gov.pl/api/acts/DU/2019/1955/text.html | PKW via Sejm (ELI) | "z dnia 14 października 2019 r." | the PKW's 2019 Sejm notice | `eli_DU_2019_1955_pkw_sejm2019.html` | 8803181 | see SHA256SUMS |
| [PKW-SEN-2023] | https://eli.gov.pl/api/acts/DU/2023/2235/text.html | PKW via Sejm (ELI) | "z dnia 17 października 2023 r." | the PKW's 2023 Senate notice | `eli_DU_2023_2235_pkw_senat2023.html` | 2323229 | see SHA256SUMS |
| [PKW-PREZ-2025] | https://api.sejm.gov.pl/eli/acts/DU/2025/714/text.pdf | PKW via Sejm (ELI) | "Warszawa, dnia 2 czerwca 2025 r." | the PKW's 2025 run-off notice, PDF | `eli_DU_2025_714_pkw_prezydent2025_r2.pdf` | 530558 | see SHA256SUMS |
| [API-TERM] | https://api.sejm.gov.pl/sejm/term | Sejm (API) | live; term 10 `lastChanged` 2026-09-24T15:33:09 | the terms with dates | `api_sejm_term.json` | 1259 | see SHA256SUMS |
| [API-C9] | https://api.sejm.gov.pl/sejm/term9/clubs | Sejm (API) | closed-term snapshot, no dates | 9th-term clubs | `api_sejm_term9_clubs.json` | 39124 | see SHA256SUMS |
| [API-C10] | https://api.sejm.gov.pl/sejm/term10/clubs | Sejm (API) | live as served | 10th-term clubs with members' join dates | `api_sejm_term10_clubs.json` | 40270 | see SHA256SUMS |
| [API-MP] | https://api.sejm.gov.pl/sejm/term10/MP | Sejm (API) | live as served | 10th-term deputies with club | `api_sejm_term10_MP.json` | 260266 | see SHA256SUMS |
| [API-PROC] | https://api.sejm.gov.pl/sejm/term10/proceedings | Sejm (API) | live as served | sittings list (fetched; not quoted) | `api_sejm_term10_proceedings.json` | 759492 | see SHA256SUMS |
| [API-V1] | https://api.sejm.gov.pl/sejm/term10/votings/1 | Sejm (API) | sitting 1, Nov–Dec 2023 | the 147 votes of sitting 1 | `api_sejm_term10_votings_sitting1.json` | 88097 | see SHA256SUMS |
| [API-V2] | https://api.sejm.gov.pl/sejm/term10/votings/2 | Sejm (API) | sitting 2, Jan 2024 | (fetched; not quoted) | `api_sejm_term10_votings_sitting2.json` | 18723 | see SHA256SUMS |
| [API-VS] | https://api.sejm.gov.pl/sejm/term10/votings/search?title=wotum%20zaufania&limit=100 | Sejm (API) | live | the two confidence votes of the term | `api_sejm_term10_votings_search_wotum_zaufania.json` | 1317 | see SHA256SUMS |
| [ELI-MP-2019] | https://api.sejm.gov.pl/eli/acts/search?publisher=MP&year=2019&type=Postanowienie&title=Rady%20Ministr%C3%B3w&limit=300 | Sejm (ELI) | live | 2019 presidential instruments on the cabinet | `eli_search_MP_2019_postanowienia_RM.json` | 10004 | see SHA256SUMS |
| [ELI-MP-2022] | same, year=2022 | Sejm (ELI) | live | (fetched; twelve "o zmianie w składzie" instruments, none quoted) | `eli_search_MP_2022_postanowienia_RM.json` | 11514 | see SHA256SUMS |
| [ELI-MP-2023] | same, year=2023 | Sejm (ELI) | live | 2023 instruments | `eli_search_MP_2023_postanowienia_RM.json` | 14360 | see SHA256SUMS |
| [ELI-MP-2024] | same, year=2024 | Sejm (ELI) | live | 2024 instruments | `eli_search_MP_2024_postanowienia_RM.json` | 5007 | see SHA256SUMS |
| [ELI-MP-2025] | same, year=2025 | Sejm (ELI) | live | 2025 instruments | `eli_search_MP_2025_postanowienia_RM.json` | 4016 | see SHA256SUMS |
| [ELI-MP-2026] | same, year=2026 | Sejm (ELI) | live | 2026 instruments | `eli_search_MP_2026_postanowienia_RM.json` | 1335 | see SHA256SUMS |
| [MP-808-META] | https://api.sejm.gov.pl/eli/acts/MP/2026/808 | Sejm (ELI) | `announcementDate` 2026-08-14 | metadata | `eli_meta_MP_2026_808_zmiana_RM.json` | 972 | see SHA256SUMS |
| [MP-808] | https://api.sejm.gov.pl/eli/acts/MP/2026/808/text.pdf | Sejm (ELI) | "z dnia 14 sierpnia 2026 r." | the instrument, PDF | `eli_MP_2026_808_zmiana_RM.pdf` | 143022 | see SHA256SUMS |
| [ZN-2020-META] | https://api.sejm.gov.pl/eli/acts/MP/2020/712 | Sejm (ELI) | `announcementDate` 2020-08-06 | metadata | `eli_meta_MP_2020_712_protokol_ZN.json` | 768 | see SHA256SUMS |
| [ZN-2020] | https://api.sejm.gov.pl/eli/acts/MP/2020/712/text.pdf | Zgromadzenie Narodowe via Sejm (ELI) | "Warszawa, dnia 6 sierpnia 2020 r." | the protocol, PDF | `eli_MP_2020_712_protokol_ZN.pdf` | 617944 | see SHA256SUMS |
| [ZN-2020-SEARCH] | https://api.sejm.gov.pl/eli/acts/search?publisher=MP&year=2020&title=Zgromadzeni&limit=50 | Sejm (ELI) | live | finder listing | `eli_search_MP_2020_zgromadzenie_narodowe.json` | 2196 | see SHA256SUMS |
| [ZN-2025-SEARCH] | same, year=2025 | Sejm (ELI) | live | finder listing | `eli_search_MP_2025_zgromadzenie_narodowe.json` | 2095 | see SHA256SUMS |
| [ZN-2025-CONV] | https://api.sejm.gov.pl/eli/acts/MP/2025/663/text.pdf | Marszałek Sejmu via Sejm (ELI) | "z dnia 7 lipca 2025 r." | the convening order, PDF | `eli_MP_2025_663_zwolanie_ZN.pdf` | 170374 | see SHA256SUMS |
| [ZN-2025-META] | https://api.sejm.gov.pl/eli/acts/MP/2025/867 | Sejm (ELI) | `announcementDate` 2025-08-06 | metadata | `eli_meta_MP_2025_867_protokol_ZN.json` | 760 | see SHA256SUMS |
| [ZN-2025] | https://api.sejm.gov.pl/eli/acts/MP/2025/867/text.pdf | Zgromadzenie Narodowe via Sejm (ELI) | "Warszawa, dnia 6 sierpnia 2025 r." | the protocol, PDF | `eli_MP_2025_867_protokol_ZN.pdf` | 272297 | see SHA256SUMS |
| [GOV-RM] | https://www.gov.pl/web/premier/sklad-rady-ministrow | Kancelaria Prezesa Rady Ministrów (gov.pl) | live, no date shown | the cabinet as served | `govpl_premier_sklad-rady-ministrow.html` | 48952 | see SHA256SUMS |
| [PIS-HOME] | https://pis.org.pl/ | PiS | live | front page (finder) | `pis_org_pl_home.html` | 197412 | see SHA256SUMS |
| [PIS-WLADZE] | https://pis.org.pl/partia/wladze-ludzie/ | PiS | live, no date | the party's leadership page | `pis_org_pl_wladze-ludzie.html` | 215523 | see SHA256SUMS |
| [KO-HOME] | https://koalicjaobywatelska.pl/ | Koalicja Obywatelska | live | front page (finder) | `koalicjaobywatelska_pl_home.html` | 231862 | see SHA256SUMS |
| [KO-LUDZIE] | https://koalicjaobywatelska.pl/ludzie/ | Koalicja Obywatelska | live, no date | the party's leadership page | `koalicjaobywatelska_pl_ludzie.html` | 149811 | see SHA256SUMS |
| [P2050-HOME] | https://polska2050.pl/ | Polska 2050 | live | front page (finder) | `polska2050_pl_home.html` | 382074 | see SHA256SUMS |
| [P2050-LUDZIE] | https://polska2050.pl/nasi-ludzie/ | Polska 2050 | live, no date | the party's leadership page | `polska2050_pl_nasi-ludzie.html` | 545045 | see SHA256SUMS |
| [P2050-NEWS] | https://polska2050.pl/katarzyna-pelczynska-nalecz-nowa-przewodniczaca-polski-2050/ | Polska 2050 | `article:published_time` 2026-01-31T21:31:50+00:00; shown "sob. 31 sty 2026" | the party's own notice of the leadership election | `polska2050_pl_pelczynska-nalecz-nowa-przewodniczaca.html` | 353698 | see SHA256SUMS |
| [NL-HOME] | https://lewica.org.pl/ | Nowa Lewica | live | front page (finder) | `lewica_org_pl_home.html` | 450267 | see SHA256SUMS |
| [NL-WLADZE] | https://lewica.org.pl/o-nas/wladze-i-kadra/ | Nowa Lewica | live, no date | the party's leadership page | `lewica_org_pl_wladze-i-kadra.html` | 258728 | see SHA256SUMS |
| [KONF-HOME] | https://konfederacja.pl/ | Konfederacja | live | front page (finder) | `konfederacja_pl_home.html` | 200812 | see SHA256SUMS |
| [KONF-O] | https://konfederacja.pl/o-konfederacji/ | Konfederacja | live, no date | "O Konfederacji" | `konfederacja_pl_o-konfederacji.html` | 166405 | see SHA256SUMS |

41 raw files. Every file's SHA-256 is in `raw/records/SHA256SUMS.txt` (sha256sum format); the digests are not repeated
in this table because the sheet is the record and a retyped digest is a transcription (the claim convention).

## GAPS

- **G1: sejm.gov.pl's own constitution pages (Polish and English) were not obtained** — both answer `302` into an
  Incapsula cookie loop, then `403`, to curl. The Constitution is quoted from the Constitutional Tribunal's page instead
  (an official body's text, not the Sejm's), and the ISAP consolidated PDF is saved but not decoded.
- **G2: the 9th-term club composition on any date 2022-07-01 → 2023-11-12** — the API's closed-term snapshot (459 of
  460) has no dates. The 9th-term chamber is on record here only as the 2019 seat table and the end-of-term clubs.
- **G3: the day-one club split of the TD committee's 65 seats** — join dates account for 44 (31 + 13) on 2023-11-13; the
  remaining 21 members' day-one club is not recoverable from the join-date file (they may have joined later under other
  dates or sit elsewhere).
- **G4: which parties stand behind the Centrum (2026-02-18) and Rozwój Plus (2026-07-30) clubs**, and the party
  membership of any deputy — the API records clubs only; no party or official page on either club was fetched.
- **G5: the 2023 Senate's composition by committee** — the notice gives per-district winners only; not tallied.
- **G6: the coalition parties of Morawiecki's second and third governments** — no fetched instrument or page names them;
  the 9th-term API shows one PiS club of 227 and no separate club for a coalition partner.
- **G7: the "no later change of prime minister" reading rests on a title search** of Monitor Polski's Postanowienia;
  an instrument titled otherwise would be missed.
- **G8: the 2023 instruments' bodies (M.P. 2023 poz. 1221–1383) were not fetched or decoded** — the ELI serves them as
  PDF only; their titles and dates are on record, the names in them are not. The day-one cabinet list (poz. 1381) is
  therefore not on record.
- **G9: constructive no-confidence motions (Art. 158) in the 10th term** — not searched (the votings search used
  "wotum zaufania").
- **G10: six ministers' parties** (Sikorski, Balczun, Cienkowska, Motyka, Sobierańska-Grenda, Żurek) — not found in the
  MP list by surname (they may not be deputies, or the surname match failed); not read elsewhere.
- **G11: the 2025 run-off's vote counts and the winner's name in the PKW notice's own words** — set in a font the
  decoder does not reach; the winner rests on the National Assembly protocol (DECODED) and the "TRZASKOWSKI Rafał" /
  "NAWROCKI Karol Tadeusz" annex strings.
- **G12, G16, G17: no dates on the PiS, Nowa Lewica and Konfederacja leadership pages** — the leaders are on record for
  2026-09-24 only; any change inside the window is unseen.
- **G13: PO → KO as a party** — the site names the party "Koalicja Obywatelska"; when it took that name is not stated.
- **G14: PSL's leader** — psl.pl unreachable (timed out on four TLS variants).
- **G15: Polska 2050's chair before 2026-01-31** — the article names the new chair and the runner-up only.
- **G18: Nowa Nadzieja's and Ruch Narodowy's leaders** — sites not reached.
- **G19: party offices of the club chairs of Razem, Konfederacja Korony Polskiej, Demokracja Bezpośrednia** — not
  fetched.
- **G20: second-session re-verification (R-K9) is pending**, as for every SOURCED file.

**The quote check.** 71 quoted strings from the HTML and JSON files above (every constitution article, the Electoral
Code articles, the six 2019 seat lines, the Senate line, the API term/club/vote records, the ELI titles and metadata
fields, the gov.pl cabinet list and the six party-site statements) were checked as substrings of the saved files after
tag-strip, entity decode (`&nbsp;`, `&amp;`, `&#8211;`, `&quot;`) and whitespace collapse: **71 of 71 found**. The
DECODED PDF strings are outside this check by construction (they are decodings, not bytes); the literal PDF strings
quoted ("Warszawa, dnia 6 sierpnia 2025 r.", "TRZASKOWSKI Rafał", "NAWROCKI Karol Tadeusz", "K. Nawrocki", "D. Tusk",
"z dnia 7 lipca 2025 r." and the rest) are the literal-string operands of the inflated content streams and were read,
not grepped, from the saved PDFs. `sha256sum -c raw/records/SHA256SUMS.txt` passes for all 41 files.

*(Filed 2026-09-24 by the PS-1 sourcing agent. Nothing outside `ElectionsData/poland/records_by_date.md` and
`ElectionsData/poland/raw/records/` was written. No code was touched, Unity was not run, nothing was committed.)*
