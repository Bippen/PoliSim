# Poland — presidential elections 2020 and 2025 [SOURCED]

Official returns for both rounds of the 2020 and 2025 elections of the President of the Republic of Poland, for S7 (the two-round presidential model). Every figure comes from a PKW *obwieszczenie* published in the Dziennik Ustaw and is cross-checked against the KBW open-data CSVs. Accessed 2026-10-01. Data files: `presidential_votes.csv` (national, per round) and `presidential_votes_by_voivodeship.csv` (per województwo, per round). Raw sources are byte-exact under `raw/president/` (`SHA256SUMS.txt`, `fetch_log.txt`).

## Outcome at a glance

| Election | Round | Polling day | Result (PKW) | Leader | Second |
|---|---|---|---|---|---|
| 2020 | 1 | 2020-06-28 | no candidate above half of valid votes: run-off | DUDA Andrzej Sebastian 8,450,513 (43.50 %) | TRZASKOWSKI Rafał Kazimierz 5,917,340 (30.46 %) |
| 2020 | 2 | 2020-07-12 | **DUDA Andrzej Sebastian** elected | DUDA Andrzej Sebastian 10,440,648 (51.03 %) | TRZASKOWSKI Rafał Kazimierz 10,018,263 (48.97 %) |
| 2025 | 1 | 2025-05-18 | no candidate above half of valid votes: run-off | TRZASKOWSKI Rafał Kazimierz 6,147,797 (31.36 %) | NAWROCKI Karol Tadeusz 5,790,804 (29.54 %) |
| 2025 | 2 | 2025-06-01 | **NAWROCKI Karol Tadeusz** elected | NAWROCKI Karol Tadeusz 10,606,877 (50.89 %) | TRZASKOWSKI Rafał Kazimierz 10,237,286 (49.11 %) |

The 2025 run-off went to **NAWROCKI Karol Tadeusz** over TRZASKOWSKI Rafał Kazimierz, by 369,591 votes. That matches `raw/records/eli_DU_2025_714_pkw_prezydent2025_r2.pdf`, which is byte-identical to the copy fetched again here. In 2025 the first-round leader (Trzaskowski) lost the run-off. In 2020 the first-round leader (Duda) won it.

Note on the act numbers: **Dz.U. 2025 poz. 652** is the first round (obwieszczenie of 19 May 2025) and **Dz.U. 2025 poz. 714** is the run-off (obwieszczenie of 2 June 2025). For 2020, **poz. 1163** is the first round (30 June 2020) and **poz. 1238** is the run-off (13 July 2020).

## What the four totals rows mean

These are the obwieszczenie's own items, quoted from Rozdział 1 pkt 1:

- `__ELIGIBLE__`: *liczba wyborców uprawnionych do głosowania* (2025 adds *w chwili zakończenia głosowania*). The electorate when polls closed.
- `__BALLOTS__`: *liczba kart ważnych*, the valid ballot papers. The obwieszczenie annexes head this column *Karty ważne (oddane głosy)*, i.e. votes cast. PKW turnout (*frekwencja*) is `__BALLOTS__ / __ELIGIBLE__`.
- `__INVALID__`: *liczba głosów nieważnych*, invalid votes on valid ballot papers: an X beside two or more names, no X at all, or (round 1) an X only beside a struck-off candidate. The obwieszczenie gives the split.
- `__VALID__`: *liczba głosów ważnych oddanych na wszystkich kandydatów* (round 1) or *łącznie na obu kandydatów* (run-off). The majority in Art. 127 ust. 4 is measured against this.

So `__VALID__ + __INVALID__ = __BALLOTS__`. *Karty nieważne* (invalid ballot papers) are outside `__BALLOTS__`. The supplementary table below gives them, with the ballot papers taken from the urn.

## 2020, first round — 2020-06-28

Source: obwieszczenie PKW z dnia 30 czerwca 2020 r., **Dz.U. 2020 poz. 1163** (ELI `DU/2020/1163`). Shares are **computed** here as votes / `__VALID__` × 100, rounded half-up to two decimals. *PKW %* is the share the obwieszczenie prints.

| # | Candidate (PKW spelling) | Committee (PKW) | Votes | Share of valid (computed) | PKW % |
|---|---|---|---:|---:|---:|
| 1 | BIEDROŃ Robert | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ ROBERTA BIEDRONIA | 432,129 | 2.22 | 2.22 |
| 2 | BOSAK Krzysztof | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ KRZYSZTOFA BOSAKA | 1,317,380 | 6.78 | 6.78 |
| 3 | DUDA Andrzej Sebastian | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ ANDRZEJA DUDY | 8,450,513 | 43.50 | 43.50 |
| 4 | HOŁOWNIA Szymon Franciszek | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ SZYMONA HOŁOWNI | 2,693,397 | 13.87 | 13.87 |
| 5 | JAKUBIAK Marek | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ MARKA JAKUBIAKA | 33,652 | 0.17 | 0.17 |
| 6 | KOSINIAK-KAMYSZ Władysław Marcin | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ WŁADYSŁAWA KOSINIAKA-KAMYSZA | 459,365 | 2.36 | 2.36 |
| 7 | PIOTROWSKI Mirosław Mariusz | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ MIROSŁAWA PIOTROWSKIEGO | 21,065 | 0.11 | 0.11 |
| 8 | TANAJNO Paweł Jan | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ PAWŁA TANAJNO | 27,909 | 0.14 | 0.14 |
| 9 | TRZASKOWSKI Rafał Kazimierz | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ RAFAŁA TRZASKOWSKIEGO | 5,917,340 | 30.46 | 30.46 |
| 10 | WITKOWSKI Waldemar Włodzimierz | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ WALDEMARA WŁODZIMIERZA WITKOWSKIEGO | 27,290 | 0.14 | 0.14 |
| 11 | ŻÓŁTEK Stanisław Józef | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ STANISŁAWA ŻÓŁTKA | 45,419 | 0.23 | 0.23 |
| | **Sum of candidates** | | **19,425,459** | | |
| | `__VALID__` | | 19,425,459 | 100.00 | |
| | `__INVALID__` | | 58,301 | | |
| | `__BALLOTS__` (karty ważne) | | 19,483,760 | | |
| | `__ELIGIBLE__` | | 30,204,684 | | |

**Sum check:** the candidates sum to 19,425,459 and `__VALID__` is 19,425,459. **Exact match.** `__VALID__ + __INVALID__` = 19,483,760 against `__BALLOTS__` 19,483,760: exact. Karty ważne + karty nieważne = 19,486,860 against cards taken from the urn 19,486,860: exact. Turnout computed as `__BALLOTS__ / __ELIGIBLE__` is 64.51 %, and the PKW prints 64.51 %: equal. Computed shares equal the PKW's printed shares for 11 of 11 candidates.

**Majority rule:** more than half of 19,425,459 valid votes means at least 9,712,730. The PKW prints *większość wymagana* 9,712,730 (equal). The leader had 8,450,513, so the PKW ordered a run-off between DUDA Andrzej Sebastian and TRZASKOWSKI Rafał Kazimierz.

## 2020, run-off (ponowne głosowanie) — 2020-07-12

Source: obwieszczenie PKW z dnia 13 lipca 2020 r., **Dz.U. 2020 poz. 1238** (ELI `DU/2020/1238`). Shares are **computed** here as votes / `__VALID__` × 100, rounded half-up to two decimals. *PKW %* is the share the obwieszczenie prints.

| # | Candidate (PKW spelling) | Committee (PKW) | Votes | Share of valid (computed) | PKW % |
|---|---|---|---:|---:|---:|
| 1 | DUDA Andrzej Sebastian | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ ANDRZEJA DUDY | 10,440,648 | 51.03 | 51.03 |
| 2 | TRZASKOWSKI Rafał Kazimierz | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ RAFAŁA TRZASKOWSKIEGO | 10,018,263 | 48.97 | 48.97 |
| | **Sum of candidates** | | **20,458,911** | | |
| | `__VALID__` | | 20,458,911 | 100.00 | |
| | `__INVALID__` | | 177,724 | | |
| | `__BALLOTS__` (karty ważne) | | 20,636,635 | | |
| | `__ELIGIBLE__` | | 30,268,460 | | |

**Sum check:** the candidates sum to 20,458,911 and `__VALID__` is 20,458,911. **Exact match.** `__VALID__ + __INVALID__` = 20,636,635 against `__BALLOTS__` 20,636,635: exact. Karty ważne + karty nieważne = 20,638,904 against cards taken from the urn 20,638,904: exact. Turnout computed as `__BALLOTS__ / __ELIGIBLE__` is 68.18 %, and the PKW prints 68.18 %: equal. Computed shares equal the PKW's printed shares for 2 of 2 candidates.

## 2025, first round — 2025-05-18

Source: obwieszczenie PKW z dnia 19 maja 2025 r., **Dz.U. 2025 poz. 652** (ELI `DU/2025/652`). Shares are **computed** here as votes / `__VALID__` × 100, rounded half-up to two decimals. *PKW %* is the share the obwieszczenie prints.

| # | Candidate (PKW spelling) | Committee (PKW) | Votes | Share of valid (computed) | PKW % |
|---|---|---|---:|---:|---:|
| 1 | BARTOSZEWICZ Artur | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ ARTURA BARTOSZEWICZA | 95,640 | 0.49 | 0.49 |
| 2 | BIEJAT Magdalena Agnieszka | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ MAGDALENY BIEJAT | 829,361 | 4.23 | 4.23 |
| 3 | BRAUN Grzegorz Michał | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ GRZEGORZA MICHAŁA BRAUNA | 1,242,917 | 6.34 | 6.34 |
| 4 | HOŁOWNIA Szymon Franciszek | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ SZYMONA HOŁOWNI | 978,901 | 4.99 | 4.99 |
| 5 | JAKUBIAK Marek | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ MARKA JAKUBIAKA | 150,698 | 0.77 | 0.77 |
| 6 | MACIAK Maciej | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ MACIEJA MACIAKA | 36,371 | 0.19 | 0.19 |
| 7 | MENTZEN Sławomir Jerzy | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ SŁAWOMIRA JERZEGO MENTZENA | 2,902,448 | 14.81 | 14.81 |
| 8 | NAWROCKI Karol Tadeusz | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ KAROLA NAWROCKIEGO | 5,790,804 | 29.54 | 29.54 |
| 9 | SENYSZYN Joanna | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ JOANNY SENYSZYN | 214,198 | 1.09 | 1.09 |
| 10 | STANOWSKI Krzysztof Jakub | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ KRZYSZTOFA JAKUBA STANOWSKIEGO | 243,479 | 1.24 | 1.24 |
| 11 | TRZASKOWSKI Rafał Kazimierz | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ RAFAŁA TRZASKOWSKIEGO | 6,147,797 | 31.36 | 31.36 |
| 12 | WOCH Marek Marian | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ MARKA WOCHA | 18,338 | 0.09 | 0.09 |
| 13 | ZANDBERG Adrian Tadeusz | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ ADRIANA ZANDBERGA | 952,832 | 4.86 | 4.86 |
| | **Sum of candidates** | | **19,603,784** | | |
| | `__VALID__` | | 19,603,784 | 100.00 | |
| | `__INVALID__` | | 85,813 | | |
| | `__BALLOTS__` (karty ważne) | | 19,689,597 | | |
| | `__ELIGIBLE__` | | 29,252,340 | | |

**Sum check:** the candidates sum to 19,603,784 and `__VALID__` is 19,603,784. **Exact match.** `__VALID__ + __INVALID__` = 19,689,597 against `__BALLOTS__` 19,689,597: exact. Karty ważne + karty nieważne = 19,692,324 against cards taken from the urn 19,692,324: exact. Turnout computed as `__BALLOTS__ / __ELIGIBLE__` is 67.31 %, and the PKW prints 67.31 %: equal. Computed shares equal the PKW's printed shares for 13 of 13 candidates.

**Majority rule:** more than half of 19,603,784 valid votes means at least 9,801,893. The PKW prints *większość wymagana* 9,801,893 (equal). The leader had 6,147,797, so the PKW ordered a run-off between TRZASKOWSKI Rafał Kazimierz and NAWROCKI Karol Tadeusz.

## 2025, run-off (ponowne głosowanie) — 2025-06-01

Source: obwieszczenie PKW z dnia 2 czerwca 2025 r., **Dz.U. 2025 poz. 714** (ELI `DU/2025/714`). Shares are **computed** here as votes / `__VALID__` × 100, rounded half-up to two decimals. *PKW %* is the share the obwieszczenie prints.

| # | Candidate (PKW spelling) | Committee (PKW) | Votes | Share of valid (computed) | PKW % |
|---|---|---|---:|---:|---:|
| 1 | NAWROCKI Karol Tadeusz | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ KAROLA NAWROCKIEGO | 10,606,877 | 50.89 | 50.89 |
| 2 | TRZASKOWSKI Rafał Kazimierz | KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ RAFAŁA TRZASKOWSKIEGO | 10,237,286 | 49.11 | 49.11 |
| | **Sum of candidates** | | **20,844,163** | | |
| | `__VALID__` | | 20,844,163 | 100.00 | |
| | `__INVALID__` | | 189,294 | | |
| | `__BALLOTS__` (karty ważne) | | 21,033,457 | | |
| | `__ELIGIBLE__` | | 29,363,722 | | |

**Sum check:** the candidates sum to 20,844,163 and `__VALID__` is 20,844,163. **Exact match.** `__VALID__ + __INVALID__` = 21,033,457 against `__BALLOTS__` 21,033,457: exact. Karty ważne + karty nieważne = 21,034,880 against cards taken from the urn 21,034,880: exact. Turnout computed as `__BALLOTS__ / __ELIGIBLE__` is 71.63 %, and the PKW prints 71.63 %: equal. Computed shares equal the PKW's printed shares for 2 of 2 candidates.

### Supplementary figures, as printed in each obwieszczenie (Rozdział 1 pkt 1)

| Figure | 2020 R1 | 2020 R2 | 2025 R1 | 2025 R2 |
|---|---:|---:|---:|---:|
| Polling districts (obwody głosowania) | 27,227 | 27,229 | 32,143 | 32,143 |
| Voters given ballot papers (2020: *wydano karty*; 2025: *w lokalach wyborczych*) | 19,026,600 | 20,047,543 | 19,684,580 | 21,025,022 |
| Postal packages sent (*pakiety wyborcze*) | 536,821 | 704,111 | 9,698 | 12,122 |
| 2025 only: ballot papers issued, in person and postal (*łącznie*) | — | — | 19,694,278 | 21,037,144 |
| Ballot papers taken from the urn | 19,486,860 | 20,638,904 | 19,692,324 | 21,034,880 |
| Invalid ballot papers (*karty nieważne*) | 3,100 | 2,269 | 2,727 | 1,423 |
| Valid ballot papers (*karty ważne*) = `__BALLOTS__` | 19,483,760 | 20,636,635 | 19,689,597 | 21,033,457 |
| Majority required (round 1 only) | 9,712,730 | — | 9,801,893 | — |
| Turnout printed (*frekwencja*) | 64.51 % | 68.18 % | 67.31 % | 71.63 % |

## Cross-checks

- 2020: every figure above was parsed from both the ELI **PDF** and the ELI **HTML** of the same act (`text.pdf`, `text.html`). They are identical, supplementary figures included.
- KBW 2025 komitety_utf8.csv: 13 of 13 committees on the ballot carry the same name as in kandydaci_utf8.csv (44 committees registered in all)
- KBW candidate CSV 2025 round 1: 13 of 13 candidates' vote counts equal the obwieszczenie (matched by surname)
- KBW candidate CSV 2025 round 2: 2 of 2 candidates' vote counts equal the obwieszczenie (matched by surname)
- KBW candidate CSV 2020 round 2: 2 of 2 candidates' vote counts equal the obwieszczenie (matched by surname)
- Per-voivodeship (KBW `po województwach` CSVs, 16 rows per round): in every row of all four rounds the candidates sum to that row's valid votes. **2020:** the 16 rows sum exactly to the national obwieszczenie in every series (candidates and all four totals). **2025:** the 16 rows fall short of the national totals, by 465,867 valid votes in round 1 and 604,531 in the run-off. The gap is exactly the `zagranica` (abroad) and `statki` (ships) rows of the KBW `po powiatach` CSVs: 16 voivodeships + zagranica + statki equal the obwieszczenie in all 17 series of round 1 and all 6 of the run-off. Both are in `presidential_votes_by_voivodeship.csv` as their own units.
- Nothing was adjusted. No mismatch was found anywhere.

## Candidates: committees, party membership, party backing

The PKW names every presidential committee in the form *KOMITET WYBORCZY KANDYDATA NA PREZYDENTA RZECZYPOSPOLITEJ POLSKIEJ* + the candidate's name in the genitive, i.e. a candidate's committee. None is a *komitet wyborczy wyborców* or a party committee. Party **membership** comes from the PKW's own candidate list (uchwała PKW on *listy kandydatów*, Monitor Polski: 2025 poz. 376, 2020 poz. 532) and the KBW column *Przynależność do partii*. The KBW 2025 candidate file also has a column **Poparcie** (party support), and it is **empty for all 13 candidates**: the PKW data record no party support for any 2025 candidate. The 2020 KBW file has no such column. Party **backing** is therefore never primary here. Where given, it is marked [SECONDARY] and cited.

### 2025

| # | Candidate | Party membership (PKW) | Backing |
|---|---|---|---|
| 1 | BARTOSZEWICZ Artur | nie należy do partii politycznej | — (none sourced) |
| 2 | BIEJAT Magdalena Agnieszka | nie należy do partii politycznej | Nowa Lewica [SECONDARY: lewica.org.pl, 2024-12-15: *„Magdalena Biejat kandydatką Lewicy w nadchodzących wyborach prezydenckich – zdecydowała Rada Krajowa Nowej Lewicy poprzez aklamację!”*] |
| 3 | BRAUN Grzegorz Michał | członek partii politycznej: Konfederacja Korony Polskiej | — (member of the party named; no backing statement sourced) |
| 4 | HOŁOWNIA Szymon Franciszek | członek partii politycznej: Polska 2050 Szymona Hołowni | — (member of the party named; no backing statement sourced) |
| 5 | JAKUBIAK Marek | członek partii politycznej: Federacja dla Rzeczypospolitej | — (member of the party named; no backing statement sourced) |
| 6 | MACIAK Maciej | nie należy do partii politycznej | — (none sourced) |
| 7 | MENTZEN Sławomir Jerzy | członek partii politycznej: Konfederacja Wolność i Niepodległość | — (member of the party named; no backing statement sourced) |
| 8 | NAWROCKI Karol Tadeusz | nie należy do partii politycznej | Prawo i Sprawiedliwość [SECONDARY: pis.org.pl/nawrocki2025/, a speech on the PiS site: *„motywy partii Prawa i Sprawiedliwości, która podjęła decyzję o wysunięciu kandydata bezpartyjnego, kandydata niezależnego”*; pis.org.pl/aktualnosci/rada-polityczna-prawa-i-sprawiedliwosci: *„Rada Polityczna Prawa i Sprawiedliwości jednogłośnie kandydaturę dr. Karola Nawrockiego na Prezydenta RP.”* (sic, verb missing on the page)] |
| 9 | SENYSZYN Joanna | nie należy do partii politycznej | — (none sourced) |
| 10 | STANOWSKI Krzysztof Jakub | nie należy do partii politycznej | — (none sourced) |
| 11 | TRZASKOWSKI Rafał Kazimierz | członek partii politycznej: Platforma Obywatelska RP | — (member of the party named; no backing statement sourced) |
| 12 | WOCH Marek Marian | członek partii politycznej: Bezpartyjni Samorządowcy - Łączy nas Polska | — (member of the party named; no backing statement sourced) |
| 13 | ZANDBERG Adrian Tadeusz | członek partii politycznej: Razem | — (member of the party named; no backing statement sourced) |

### 2020

| # | Candidate | Party membership (PKW) | Backing |
|---|---|---|---|
| 1 | BIEDROŃ Robert | członek partii politycznej: Wiosna Roberta Biedronia | — (member of the party named; no backing statement sourced) |
| 2 | BOSAK Krzysztof | członek partii politycznej: Konfederacja Wolność i Niepodległość | — (member of the party named; no backing statement sourced) |
| 3 | DUDA Andrzej Sebastian | nie należy do partii politycznej | — (not sourced; see NOT REACHED) |
| 4 | HOŁOWNIA Szymon Franciszek | nie należy do partii politycznej | — (none sourced) |
| 5 | JAKUBIAK Marek | członek partii politycznej: Federacja dla Rzeczypospolitej | — (member of the party named; no backing statement sourced) |
| 6 | KOSINIAK-KAMYSZ Władysław Marcin | członek partii politycznej: Polskie Stronnictwo Ludowe | — (member of the party named; no backing statement sourced) |
| 7 | PIOTROWSKI Mirosław Mariusz | członek partii politycznej: Ruch Prawdziwa Europa - Europa Christi | — (member of the party named; no backing statement sourced) |
| 8 | TANAJNO Paweł Jan | nie należy do partii politycznej | — (none sourced) |
| 9 | TRZASKOWSKI Rafał Kazimierz | członek partii politycznej: Platforma Obywatelska RP | — (member of the party named; no backing statement sourced) |
| 10 | WITKOWSKI Waldemar Włodzimierz | członek partii politycznej: Unia Pracy | — (member of the party named; no backing statement sourced) |
| 11 | ŻÓŁTEK Stanisław Józef | członek partii politycznej: Kongres Nowej Prawicy (the PKW list in M.P. 2020 poz. 532 reads *„członek Kongresu Nowej Prawicy oraz Polexitu”*) | — (member of the party named; no backing statement sourced) |

For the game, the PKW **membership** column is primary and covers most candidates: TRZASKOWSKI (Platforma Obywatelska RP), MENTZEN and BOSAK (Konfederacja Wolność i Niepodległość), ZANDBERG (Razem), HOŁOWNIA 2025 (Polska 2050 Szymona Hołowni), BRAUN (Konfederacja Korony Polskiej), KOSINIAK-KAMYSZ (Polskie Stronnictwo Ludowe), BIEDROŃ (Wiosna Roberta Biedronia). The two winners, NAWROCKI (2025) and DUDA (2020), were **not party members** according to the PKW.

## The Constitution (Konstytucja RP, Dz.U. 1997 nr 78 poz. 483)

The text below is verbatim from the Kancelaria Sejmu unified text (`D19970483Lj.pdf`, last amendment noted: Dz.U. 2009 nr 114 poz. 946), fetched through the Sejm ELI API. Only PDF line wraps are joined. It was compared with the original Dz.U. 1997 nr 78 poz. 483 (ELI `text.pdf`, a scanned print with an OCR layer): Arts 127 and 128 read the same apart from hyphenation and OCR noise (the scan prints *100000*, the unified text *100 000*). The English glosses are this file's working translations, not an official text.

**Correction to the brief:** the five-year term and the single re-election limit are in **Art. 127 ust. 2**. **Art. 128** covers when the term begins and when the Marshal of the Sejm must call the election.

### Art. 127 — election of the President

> Art. 127. 1. Prezydent Rzeczypospolitej jest wybierany przez Naród w wyborach powszechnych, równych, bezpośrednich i w głosowaniu tajnym.
>
> *Gloss:* The President is elected by the Nation in universal, equal and direct elections by secret ballot.

> 2. Prezydent Rzeczypospolitej jest wybierany na pięcioletnią kadencję i może być ponownie wybrany tylko raz.
>
> *Gloss:* The President is elected for a five-year term and may be re-elected only once.

> 3. Na Prezydenta Rzeczypospolitej może być wybrany obywatel polski, który najpóźniej w dniu wyborów kończy 35 lat i korzysta z pełni praw wyborczych do Sejmu. Kandydata zgłasza co najmniej 100 000 obywateli mających prawo wybierania do Sejmu.
>
> *Gloss:* Eligible: a Polish citizen who is at least 35 on polling day and has full electoral rights to the Sejm. A candidate is nominated by at least 100,000 citizens with the right to vote for the Sejm.

> 4. Na Prezydenta Rzeczypospolitej wybrany zostaje kandydat, który otrzymał więcej niż połowę ważnie oddanych głosów. Jeżeli żaden z kandydatów nie uzyska wymaganej większości, czternastego dnia po pierwszym głosowaniu przeprowadza się ponowne głosowanie.
>
> *Gloss:* The candidate who receives **more than half of the validly cast votes** is elected. If no candidate wins that majority, a second vote (run-off) is held **on the fourteenth day after the first**.

> 5. W ponownym głosowaniu wyboru dokonuje się spośród dwóch kandydatów, którzy w pierwszym głosowaniu otrzymali kolejno największą liczbę głosów. Jeżeli którykolwiek z tych dwóch kandydatów wycofa zgodę na kandydowanie, utraci prawo wyborcze lub umrze, w jego miejsce do wyborów w ponownym głosowaniu dopuszcza się kandydata, który otrzymał kolejno największą liczbę głosów w pierwszym głosowaniu. W takim przypadku datę ponownego głosowania odracza się o dalszych 14 dni.
>
> *Gloss:* The run-off is between **the two candidates with the most votes** in the first vote. If either of them **withdraws consent, loses electoral rights or dies**, the candidate with the next-highest first-round vote takes their place, and the run-off is **postponed by a further 14 days**.

> 6. Na Prezydenta Rzeczypospolitej wybrany zostaje kandydat, który w ponownym głosowaniu otrzymał więcej głosów.
>
> *Gloss:* The candidate who receives **more votes in the run-off** is elected.

> 7. Zasady i tryb zgłaszania kandydatów i przeprowadzania wyborów oraz warunki ważności wyboru Prezydenta Rzeczypospolitej określa ustawa.
>
> *Gloss:* A statute (the Kodeks wyborczy) sets the rules for nominations, the conduct of the election and the conditions of its validity.

### Art. 128 — the term and the calling of the election

> Art. 128. 1. Kadencja Prezydenta Rzeczypospolitej rozpoczyna się w dniu objęcia przez niego urzędu.
>
> *Gloss:* The term begins on the day the President takes office.

> 2. Wybory Prezydenta Rzeczypospolitej zarządza Marszałek Sejmu na dzień przypadający nie wcześniej niż na 100 dni i nie później niż na 75 dni przed upływem kadencji urzędującego Prezydenta Rzeczypospolitej, a w razie opróżnienia urzędu Prezydenta Rzeczypospolitej – nie później niż w czternastym dniu po opróżnieniu urzędu, wyznaczając datę wyborów na dzień wolny od pracy przypadający w ciągu 60 dni od dnia zarządzenia wyborów.
>
> *Gloss:* The Marshal of the Sejm orders the election for a day no earlier than 100 and no later than 75 days before the incumbent's term ends. If the office falls vacant, the Marshal orders it within 14 days of the vacancy, for a non-working day within 60 days of the order.

### Art. 122 ust. 5 — the veto and the 3/5 override (with ust. 2 and ust. 6 for context)

> 2. Prezydent Rzeczypospolitej podpisuje ustawę w ciągu 21 dni od dnia przedstawienia i zarządza jej ogłoszenie w Dzienniku Ustaw Rzeczypospolitej Polskiej.
>
> *Gloss:* The President signs a statute within 21 days and orders its publication.

> 5. Jeżeli Prezydent Rzeczypospolitej nie wystąpił z wnioskiem do Trybunału Konstytucyjnego w trybie ust. 3, może z umotywowanym wnioskiem przekazać ustawę Sejmowi do ponownego rozpatrzenia. Po ponownym uchwaleniu ustawy przez Sejm większością 3/5 głosów w obecności co najmniej połowy ustawowej liczby posłów Prezydent Rzeczypospolitej w ciągu 7 dni podpisuje ustawę i zarządza jej ogłoszenie w Dzienniku Ustaw Rzeczypospolitej Polskiej. W razie ponownego uchwalenia ustawy przez Sejm Prezydentowi Rzeczypospolitej nie przysługuje prawo wystąpienia do Trybunału Konstytucyjnego w trybie ust. 3.
>
> *Gloss:* **Veto.** If the President has not referred the statute to the Constitutional Tribunal, the President may send it back to the Sejm with reasons. The Sejm overrides by re-passing it with **3/5 of the votes, with at least half the statutory number of deputies present** (statutory number 460, Art. 96 ust. 1: *„Sejm składa się z 460 posłów.”*). The President must then sign within 7 days and may no longer refer it to the Tribunal.

> 6. Wystąpienie Prezydenta Rzeczypospolitej do Trybunału Konstytucyjnego z wnioskiem w sprawie zgodności ustawy z Konstytucją lub z wnioskiem do Sejmu o ponowne rozpatrzenie ustawy wstrzymuje bieg, określonego w ust. 2, terminu do podpisania ustawy.
>
> *Gloss:* A referral to the Tribunal or a request for reconsideration stops the 21-day clock.

### Art. 121 — the Senate stage (for completeness)

> Art. 121. 1. Ustawę uchwaloną przez Sejm Marszałek Sejmu przekazuje Senatowi.
>
> *Gloss:* The Marshal of the Sejm sends a passed statute to the Senate.

> 2. Senat w ciągu 30 dni od dnia przekazania ustawy może ją przyjąć bez zmian, uchwalić poprawki albo uchwalić odrzucenie jej w całości. Jeżeli Senat w ciągu 30 dni od dnia przekazania ustawy nie podejmie stosownej uchwały, ustawę uznaje się za uchwaloną w brzmieniu przyjętym przez Sejm.
>
> *Gloss:* The Senate has 30 days to accept it unchanged, amend it or reject it. If it does nothing, the Sejm's text stands.

> 3. Uchwałę Senatu odrzucającą ustawę albo poprawkę zaproponowaną w uchwale Senatu uważa się za przyjętą, jeżeli Sejm nie odrzuci jej bezwzględną większością głosów w obecności co najmniej połowy ustawowej liczby posłów.
>
> *Gloss:* A Senate rejection or amendment stands unless the Sejm overturns it by an **absolute majority** with at least half the statutory number of deputies present.

Also on record in the obwieszczenia: the PKW cites **Art. 127 ust. 4** with art. 292 § 1 Kodeksu wyborczego when it orders the run-off, and **Art. 127 ust. 6** with art. 292 § 4 when it declares the run-off winner elected.

## Sources

All fetched on 2026-10-01 (UTC times in `raw/president/fetch_log.txt`). sha256 is of the byte-exact file in `raw/president/`.

| Id | What | URL | File | sha256 |
|---|---|---|---|---|
| Dz.U. 2020 poz. 1163 | Obwieszczenie PKW z dnia 30 czerwca 2020 r. o wynikach głosowania i wyniku wyborów Prezydenta RP (round 1, 28.06.2020) | https://api.sejm.gov.pl/eli/acts/DU/2020/1163/text.pdf | `eli_DU_2020_1163_pkw_prezydent2020_r1.pdf` | `757e33f84fef66c4ac303790feab1ae38ee242ff74ed32b2231ce860b1111556` |
| Dz.U. 2020 poz. 1163 (HTML) | same act, ELI HTML text (cross-check) | https://api.sejm.gov.pl/eli/acts/DU/2020/1163/text.html | `eli_DU_2020_1163_pkw_prezydent2020_r1.html` | `c42ee9aebf1a9342472f26b48ac9e4f202408800b0a93ee6667365d394990280` |
| Dz.U. 2020 poz. 1238 | Obwieszczenie PKW z dnia 13 lipca 2020 r. o wynikach ponownego głosowania i wyniku wyborów Prezydenta RP (run-off, 12.07.2020) | https://api.sejm.gov.pl/eli/acts/DU/2020/1238/text.pdf | `eli_DU_2020_1238_pkw_prezydent2020_r2.pdf` | `81a2b341c6be584104a8ec31a619141f483c0a462938adda36f6a8f9276cd691` |
| Dz.U. 2020 poz. 1238 (HTML) | same act, ELI HTML text (cross-check) | https://api.sejm.gov.pl/eli/acts/DU/2020/1238/text.html | `eli_DU_2020_1238_pkw_prezydent2020_r2.html` | `fc3cec19f58adaca3249fc4b5f58041d8c8bf6a3cd6b8e17f8e90f78d2d283f4` |
| Dz.U. 2025 poz. 652 | Obwieszczenie PKW z dnia 19 maja 2025 r. o wynikach głosowania i wyniku wyborów Prezydenta RP zarządzonych na dzień 18 maja 2025 r. (round 1) | https://api.sejm.gov.pl/eli/acts/DU/2025/652/text.pdf | `eli_DU_2025_652_pkw_prezydent2025_r1.pdf` | `b4b9816666aeab47bb26778219ba4cd6d5c1de8dc3955b311d49c37749e738b9` |
| Dz.U. 2025 poz. 714 | Obwieszczenie PKW z dnia 2 czerwca 2025 r. o wynikach ponownego głosowania i wyniku wyborów Prezydenta RP (run-off, 01.06.2025) | https://api.sejm.gov.pl/eli/acts/DU/2025/714/text.pdf | `eli_DU_2025_714_pkw_prezydent2025_r2.pdf` | `1e5748473db5d3e172b581fbfc29f533912475ef5f934774f655053526aa3a2d` |
| M.P. 2025 poz. 376 | Uchwała nr 163/2025 PKW z dnia 23 kwietnia 2025 r. w sprawie listy kandydatów (membership) | https://api.sejm.gov.pl/eli/acts/MP/2025/376/text.pdf | `eli_MP_2025_376_pkw_lista_kandydatow_2025.pdf` | `efdf4e79cfba6de77c8027725dfad205996e9b7eafa0e168a12ad3d2facbb414` |
| M.P. 2020 poz. 532 | Uchwała nr 190/2020 PKW z dnia 12 czerwca 2020 r. w sprawie listy kandydatów (membership) | https://api.sejm.gov.pl/eli/acts/MP/2020/532/text.pdf | `eli_MP_2020_532_pkw_lista_kandydatow_2020.pdf` | `cc7cc9f8228553310f9cb657140ce07b0435544d7284b55c90951655ed6781d8` |
| KBW 2025/1 kandydaci | candidates, committees, membership, Poparcie, votes | https://danewyborcze.kbw.gov.pl/dane/2025/prezydent/1/kandydaci_csv.1752152970.zip | `kbw_2025_1_kandydaci_csv.1752152970.zip` | `6b3b2ddb1237d619e15aa4d06a8eaa514c83475a244b35f05c928ac5c1d3ac61` |
| KBW 2025/1 komitety | all 2025 committees (names cross-checked) | https://danewyborcze.kbw.gov.pl/dane/2025/prezydent/1/komitety_csv.1752152970.zip | `kbw_2025_1_komitety_csv.1752152970.zip` | `c6de949695c6ea190a90ab703ddbc097f7e6c7ee55419a572e86e58ff833f7e8` |
| KBW 2025/2 kandydaci | run-off candidates and votes | https://danewyborcze.kbw.gov.pl/dane/2025/prezydent/2/kandydaci_w_drugiej_turze_csv.1752152970.zip | `kbw_2025_2_kandydaci_w_drugiej_turze_csv.1752152970.zip` | `6696a7b31a4e012d6edfec57d0da4a1df103239535e896418d531d963ad336a7` |
| KBW 2020/1 kandydaci | candidates, committees, membership | https://danewyborcze.kbw.gov.pl/dane/2020/prezydent/1/kandydaci_csv.zip | `kbw_2020_1_kandydaci_csv.zip` | `1ed4c05e3549c175be556e9ec72b620797ed00a108f5e7a8fa0cc8ec1f6cc4e0` |
| KBW 2020/2 kandydaci | run-off candidates and votes | https://danewyborcze.kbw.gov.pl/dane/2020/prezydent/2/kandydaci_csv.zip | `kbw_2020_2_kandydaci_csv.zip` | `385aac70557c6afefce8c0e7daa3e568ce717054c1d99321f5b41c5fbd51b607` |
| KBW 2020/1 województwa | per-voivodeship, round 1 | https://danewyborcze.kbw.gov.pl/dane/2020/prezydent/1/wyniki_gl_na_kand_po_wojewodztwach_csv.zip | `kbw_2020_1_wyniki_gl_na_kand_po_wojewodztwach_csv.zip` | `87abcfe163baa9c8b50fa8ed48a24b96b4bb0aa969eb6f0327fbb99c37c49261` |
| KBW 2020/2 województwa | per-voivodeship, run-off | https://danewyborcze.kbw.gov.pl/dane/2020/prezydent/2/wyniki_gl_na_kand_po_wojewodztwach_csv.zip | `kbw_2020_2_wyniki_gl_na_kand_po_wojewodztwach_csv.zip` | `126d61eef750b94c851cd860bcdbe43b57212e07fd3e7d4bb6e47b12560c9144` |
| KBW 2025/1 województwa | per-voivodeship, round 1 | https://danewyborcze.kbw.gov.pl/dane/2025/prezydent/1/wyniki_gl_na_kandydatow_po_wojewodztwach_csv.1752152970.zip | `kbw_2025_1_wyniki_gl_na_kandydatow_po_wojewodztwach_csv.1752152970.zip` | `b87f9dac93cf15acc12320b706c5d88656bf4c0b3939047c9b5b6e2cab1d9615` |
| KBW 2025/2 województwa | per-voivodeship, run-off | https://danewyborcze.kbw.gov.pl/dane/2025/prezydent/2/wyniki_gl_na_kandydatow_po_wojewodztwach_w_drugiej_turze_csv.1752152970.zip | `kbw_2025_2_wyniki_gl_na_kandydatow_po_wojewodztwach_w_drugiej_turze_csv.1752152970.zip` | `6dacdba518dd0cae3c5531cff4f83c619eb4e78d98d9908210af0ed94d48627b` |
| KBW 2025/1 powiaty | per-powiat (zagranica, statki rows), round 1 | https://danewyborcze.kbw.gov.pl/dane/2025/prezydent/1/wyniki_gl_na_kandydatow_po_powiatach_csv.1752152970.zip | `kbw_2025_1_wyniki_gl_na_kandydatow_po_powiatach_csv.1752152970.zip` | `46f24f327af9c7b8981b16fcf4d39c8ea5babfc3edbcb552ff84206e02f249d9` |
| KBW 2025/2 powiaty | per-powiat (zagranica, statki rows), run-off | https://danewyborcze.kbw.gov.pl/dane/2025/prezydent/2/wyniki_gl_na_kandydatow_po_powiatach_w_drugiej_turze_csv.1752152970.zip | `kbw_2025_2_wyniki_gl_na_kandydatow_po_powiatach_w_drugiej_turze_csv.1752152970.zip` | `f2bb0566ca466403d7e97a87ed15eb1ef437e4e05ca7075c238dbd33db8e87a1` |
| Konstytucja RP (U) | Dz.U. 1997 nr 78 poz. 483, Kancelaria Sejmu unified text D19970483Lj.pdf (quoted) | https://api.sejm.gov.pl/eli/acts/DU/1997/483/text/U/D19970483Lj.pdf | `eli_DU_1997_483_konstytucja_U_D19970483Lj.pdf` | `9758dc31bcbcae253d0ac5499d1734954979d123397d61fc9773e287412d64b2` |
| Konstytucja RP (O) | Dz.U. 1997 nr 78 poz. 483, original print (compared) | https://api.sejm.gov.pl/eli/acts/DU/1997/483/text.pdf | `eli_DU_1997_483_konstytucja.pdf` | `014b25f1d5690f11f58fafdd505650b36fc8835530b20f16c08b2577a928877f` |
| [SECONDARY] PiS | PiS site, #Nawrocki2025 | https://pis.org.pl/nawrocki2025/ | `secondary_pis_org_pl_nawrocki2025.html` | `693e655e8d9ba1f24a6fd7b20ff057fb038c9473941260d24b63007ea45f8485` |
| [SECONDARY] PiS | PiS site, Rada Polityczna | https://pis.org.pl/aktualnosci/rada-polityczna-prawa-i-sprawiedliwosci | `secondary_pis_org_pl_rada-polityczna-prawa-i-sprawiedliwosci.html` | `4e69be5f3904308d056745c05cc88ca49ef339175362056e52a4d08c737ba08f` |
| [SECONDARY] Lewica | Lewica site, 2024-12-15 | https://lewica.org.pl/aktualnosci/magdalena-biejat-kandydatka-lewicy-w-wyborach-prezydenckich/ | `secondary_lewica_org_pl_magdalena-biejat-kandydatka-lewicy.html` | `42c2a9f98f9bd65fa28e745b37867f1f54cbf1aa77d18b014324d013282ce94c` |

Navigation pages kept for provenance: KBW wiki pages `kbw_page_*.html` (the download links), ELI search results `eli_search_*.json`, ELI metadata `eli_meta_*.json`. Among them is `eli_meta_DU_2020_967.json`, Uchwała nr 129/2020 PKW z dnia 10 maja 2020 r. *w sprawie stwierdzenia braku możliwości głosowania na kandydatów*: the 2020 election was first ordered for 10 May 2020, voting could not take place, and it was re-held on 28 June 2020. The ISAP copy of the unified Constitution, fetched on 2026-09-24 as `raw/records/isap_WDU19970780483_konstytucja_Lj.pdf` (sha256 `9758dc31bcbcae253d0ac5499d1734954979d123397d61fc9773e287412d64b2`), is **byte-identical** to the ELI copy quoted here.

## NOT REACHED

- **ISAP direct** (`https://isap.sejm.gov.pl/isap.nsf/DocDetails.xsp?id=WDU19970780483`, `.../download.xsp/WDU19970780483/U/D19970483Lj.pdf`, `.../O/D19970483.pdf`), 2026-10-01 ~11:49Z. Without cookies: HTTP 302 back to the same URL (Incapsula cookie loop, 0 bytes). With a cookie jar: HTTP 403 *„Request unsuccessful. Incapsula incident ID: 7236000740115808237-277220743358714509”*. The 403 bodies were deleted, not kept as sources. The same Kancelaria Sejmu PDFs came from the Sejm ELI API, and the ELI unified text is byte-identical to the ISAP copy already on file from 2026-09-24, so the Constitution text is ISAP's.
- **Party backing for DUDA Andrzej Sebastian (2020)**: no PKW record (not a party member, and the 2020 KBW file has no support column), and no committee or party document was fetched. A web search restricted to pis.org.pl turned up only 2015 campaign pages. Left blank.
- **Party backing for other non-member candidates** (2025: BARTOSZEWICZ, MACIAK, SENYSZYN, STANOWSKI; 2020: HOŁOWNIA, TANAJNO): not researched. Left blank.
- **Candidates' own committee websites** as the PKW lists them in `komitety_utf8.csv`, tried 2026-10-01 for a committee statement on party support: `https://karolnawrocki2025.pl/` (no response, curl 000), `https://kw.trzaskowski.pl` (HTTP 403), `http://komitet.mentzen2025.pl` (no response), `http://www.holownia2025.pl` (redirects to the unrelated `pchig.pl`), `https://biejat2025.pl/` (now an unrelated portal), `https://www.braun2025.pl/komitet` (HTTP 200; a text search found no party-support statement). None was relied on or saved.
- `https://lewica.org.pl/aktualnosci/11355-magdalena-biejat-kandydatka-lewicy-w-wyborach-prezydenckich` (the URL the search index gave) returned HTTP 404 (the site now serves the article under a new slug). The same article was fetched at its new URL (above), and the 404 body was deleted.
- No PKW *wybory.gov.pl* pages were needed: the Dz.U. obwieszczenia and the KBW CSVs cover every figure.
