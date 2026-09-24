# Italy — chambers, governments and party leaders of record by date, 2022-07-01 → 2026-09-24 [SOURCED] [PROVISIONAL]

Class: SOURCED (PS-1, the world clock — `docs/specs/POLITICAL_SYSTEM_SPEC.md` §9 stage 1, §11; sourcing pass
2026-09-24, research agent). `[PROVISIONAL]` until a second session re-verifies (R-K9). Party keys are the
project's (`Assets/Scripts/Data/PartySystem.cs`, the Italy array): FdI, PD, Lega, M5S, FI, AzIV, AVS, NM, SVP, PlusE,
IC, ScN, MAIE, UV.

**What this file is for.** The game's Italian start is a snap-election start (§4, §10 decision 2): the day the
2022 crisis triggered the election, with Draghi's caretaker government in office and the XVIII legislature still
seated. This file dates every boundary the world clock needs — which chamber, which government, which party leader —
from 1 July 2022 to the access date, each from a page fetched and saved. `returns_2022.md` (the 2022 Camera returns)
is not touched; where this file bears on it, it says so under "What this file says about `returns_2022.md` and §4".

**Fetched, never recalled.** Every quote below was read in a page fetched on 2026-09-24 (afternoon–evening, CEST)
and saved byte for byte under `raw/records/`. Each file's SHA-256 is in `raw/records/SHA256SUMS.txt` and in the
register; `raw/records/fetch_log.txt` carries the UTC time, HTTP status, size and URL of every fetch, including the
ones not kept. Quotes are verbatim in the page's language (Italian, German, French) and were checked against the
saved bytes by a script that strips tags, decodes HTML entities and collapses whitespace — so a quote here may show
one space where the bytes hold a non-breaking space or a tag boundary, and an accented letter where the bytes hold an
entity (`&ograve;`). English glosses are marked *gloss:* and are mine. Lines marked **DERIVED** state their arithmetic
or reading. Wikipedia was used only to locate pages and is cited nowhere.

**Two official hosts refused every fetch** and are the reason for most GAPS: `senato.it` (every path, including the
open-data host's SPARQL endpoint, answers an AWS-WAF challenge: HTTP 202 with an empty body, or the Senate's own
403/5xx error page) and `quirinale.it` / `presidenti.quirinale.it` (Cloudflare challenge, HTTP 403). The attempts are
in the fetch log's tail. Where a Senate figure matters (the two Senate confidence votes, the Senate's party
composition) this file falls back to a **SECONDARY** source (ANSA), marked as such on every line that uses it.

## 1. THE CHAMBER OF RECORD BY DATE

### 1.1 Which legislature sat

| from | to | chamber of record | source ids |
|---|---|---|---|
| 2022-07-01 | 2022-10-12 | **XVIII legislature** (elected 2018-03-04; convened 2018-03-23), both chambers; dissolved by decree on 2022-07-21 but sitting until the XIX convened | [ST-18] [GV-LEG] [DPR-96] |
| 2022-10-13 | 2026-09-24 (open) | **XIX legislature** (elected 2022-09-25), both chambers; first sitting 2022-10-13 | [DPR-97] [CAM-S1] [CAM-FON] [GV-LEG] |

- [ST-18] (the Camera's historical portal): "XVIII Legislatura della Repubblica italiana dal 23 marzo 2018 al 12 ottobre 2022 Presidenti della Camera Roberto Fico".
- [GV-LEG] (governo.it, the legislatures list): "XVIII Legislatura (dal 23 marzo 2018 al 12 ottobre 2022) elezioni politiche del 4 marzo 2018 Mario Draghi (dal 13 febbraio 2021 al 22 ottobre 2022) Giuseppe Conte II (dal 5 settembre 2019 al 13 febbraio 2021)" and "XIX Legislatura (dal 13 ottobre 2022) elezioni politiche del 25 settembre 2022 Governo Meloni (dal 22 ottobre 2022 - in carica)". *gloss:* the page, as served on the access date, shows the XIX open and Meloni in office.
- [DPR-97] fixes the first sitting: "La prima riunione delle Camere avrà luogo il giorno 13 ottobre 2022." (both chambers; the Senate's own first-sitting record could not be fetched — G1).
- [CAM-S1] is that sitting's stenographic record: "Seduta n. 1 di giovedì 13 ottobre 2022" — "PRESIDENZA DEL PRESIDENTE PROVVISORIO ETTORE ROSATO La seduta comincia alle 10."
- [CAM-FON]: "La prima seduta della XIX legislatura si è aperta alle 10 di giovedì 13 ottobre." and, on the Camera's President: "L'elezione è avvenuta alla quarta votazione, nella giornata di venerdì 14 ottobre, dove il quorum richiesto era della maggioranza assoluta dei voti. Lorenzo Fontana ha ottenuto 222 voti."

### 1.2 The snap election's trigger chain, July 2022

| date | event | status | source ids |
|---|---|---|---|
| 2022-07-14 | Draghi tenders the government's resignation to the President, who does not accept it and sends the government to Parliament | **on record** (the letter read to the Camera on 18 July) | [CAM-726] |
| 2022-07-14 | the M5S does not take part in the Senate's confidence vote on the *decreto Aiuti* | **GAP** on an official page (G2): the Senate record is unreachable; the only fetched text is ANSA's account of the 20 July vote | — |
| 2022-07-20 | Senate: Draghi's communications and the confidence vote on the Casini resolution — 95 in favour, 38 against; M5S, Lega and FI do not vote | **SECONDARY** (ANSA) — the Senate's own record is unreachable (G2) | [ANSA-0720] |
| 2022-07-21 | Draghi tells the Camera he is going to the President "alla luce del voto espresso ieri sera dal Senato"; reiterates the resignation; the President takes note and the government stays for current business | **on record** | [CAM-729] [CAM-C21] |
| 2022-07-21 | the President dissolves both chambers (DPR 96/2022, Art. 88 Cost.) | **on record** | [DPR-96] |
| 2022-07-21 | the election is called for 2022-09-25 and the first sitting for 2022-10-13 (DPR 97/2022) | **on record** | [DPR-97] |
| 2022-09-25 | election day | **on record** | [DPR-97] [GV-LEG] |
| 2022-10-13 | XIX legislature convenes | **on record** | [CAM-S1] [CAM-FON] |

- [CAM-726], sitting n. 726 of 18 July 2022, "Annunzio delle dimissioni del Governo":
  > "PRESIDENTE. Comunico che, in data 14 luglio 2022, il Presidente del Consiglio dei Ministri ha inviato al Presidente della Camera la seguente lettera: “Onorevole Presidente, La informo che in data odierna ho rassegnato al Capo dello Stato le dimissioni del Governo da me presieduto. Il Presidente della Repubblica non ha accolto le dimissioni da me rassegnate e ha invitato il Governo a presentarsi al Parlamento per rendere comunicazioni. Firmato: Mario Draghi”."
- [ANSA-0720] (**SECONDARY**; ANSA, "20 luglio 2022, 21:47"): title "Il giorno più lungo: dal Senato fiducia a Draghi con soli 95 sì"; standfirst "Trentotto i voti contrari. 192 i senatori presenti, 133 i votanti, 67 la maggioranza."; body: "il non voto in Senato da parte non solo del Movimento 5 Stelle ma anche del "centrodestra di governo", come hanno continuato a definirsi fino all'ultimo Lega e Forza Italia, certifica la fine delle larghissime intese."
- [CAM-729], sitting n. 729 of 21 July 2022. Draghi, before the suspension: "Alla luce del voto espresso ieri sera dal Senato della Repubblica, chiedo di sospendere la seduta, perché mi sto recando dal Presidente della Repubblica per comunicare le mie determinazioni" (the record's full stop follows after a space). Then the President of the Camera:
  > "Comunico che, in data odierna, il Presidente del Consiglio dei Ministri mi ha inviato la seguente lettera: “Onorevole Presidente, La informo che in data odierna ho reiterato al Capo dello Stato le dimissioni mie e del Governo da me presieduto. Il Presidente della Repubblica ne ha preso atto e il Governo resta in carica per il disbrigo degli affari correnti. Firmato: Mario Draghi”"
- [CAM-C21] (the Camera's news page of 21 July 2022): "Nella seduta di giovedì 21 luglio, il Presidente della Camera, Roberto Fico, ha dato conto della lettera con la quale il Presidente del Consiglio dei ministri, Mario Draghi, comunica di aver rassegnato le proprie dimissioni al Capo dello Stato, che ha invitato il Governo a rimanere in carica per il disbrigo degli affari correnti."
- [DPR-96] (D.P.R. 21 luglio 2022, n. 96, consolidated text on normattiva): "Visto l' articolo 88 della Costituzione ; Sentiti i Presidenti del Senato della Repubblica e della Camera dei deputati; Decreta: Il Senato della Repubblica e la Camera dei deputati sono sciolti." — "Dato a Roma, addì 21 luglio 2022 MATTARELLA".
- [DPR-97] (D.P.R. 21 luglio 2022, n. 97): "I comizi per le elezioni della Camera dei deputati e del Senato della Repubblica sono convocati per il giorno di domenica 25 settembre 2022. La prima riunione delle Camere avrà luogo il giorno 13 ottobre 2022."

**DERIVED — which day "the government fell":** the fetched record gives three dated steps, not one: 14 July (resignation
tendered, refused), 20 July (the Senate vote in which the M5S, Lega and FI did not vote — SECONDARY), 21 July
(resignation reiterated and taken note of, caretaker status begins, dissolution signed). The caretaker period the spec's
§4 names ("Draghi's caretaker government") runs from **2022-07-21** ("il Governo resta in carica per il disbrigo degli
affari correnti", [CAM-729]) to 2022-10-22 ([GV-DR]). Which of the three days is the game's trigger day is Elias's to rule;
this file only dates them.

### 1.3 The 2022 Senato (200 seats) — composition by party

The primary page reachable is the Interior Ministry's historical portal (Eligendo Archivio) [EL-S22], basis
"Senato 25/09/2022, Area ITALIA (escl. Valle d'Aosta)". It publishes the proportional seats **per list** and the
uninominal seats **per coalition**, not per party. Figures verbatim from its table:

| list (Eligendo's name) | key | votes | % | proportional seats |
|---|---|---|---|---|
| FRATELLI D'ITALIA CON GIORGIA MELONI | FdI | 7.168.875 | 26,00 | 34 |
| LEGA PER SALVINI PREMIER | Lega | 2.437.406 | 8,84 | 13 |
| FORZA ITALIA | FI | 2.281.258 | 8,27 | 9 |
| NOI MODERATI/LUPI - TOTI - BRUGNARO - UDC | NM | 248.308 | 0,90 | — |
| PARTITO DEMOCRATICO - ITALIA DEMOCRATICA E PROGRESSISTA | PD | 5.220.256 | 18,93 | 31 |
| ALLEANZA VERDI E SINISTRA | AVS | 972.780 | 3,53 | 3 |
| +EUROPA | PlusE | 810.441 | 2,94 | — |
| IMPEGNO CIVICO LUIGI DI MAIO - CENTRO DEMOCRATICO | IC | 161.773 | 0,59 | — |
| MOVIMENTO 5 STELLE | M5S | 4.290.194 | 15,56 | 23 |
| AZIONE - ITALIA VIVA - CALENDA | AzIV | 2.131.023 | 7,73 | 9 |
| SUD CHIAMA NORD | ScN | 272.462 | 0,99 | — |

Uninominal seats by coalition, same table (the "CANDIDATI UNINOMINALI" rows): centre-right (FdI+Lega+FI+NM) **56**;
centre-left (PD+AVS+PlusE+IC) **5**; M5S **5**; ScN **1**; every other coalition/list 0. Table totals: "TOTALE CANDIDATI
… 67" and "LISTE … 122". Turnout on this basis: "Elettori 45.210.950", "Votanti 28.850.840 | 63,81 %".

**DERIVED:** 122 + 67 = 189 seats on the Area-Italia basis; the Senate has 200 ([DOS] quoted below: "duecento, quattro dei
quali eletti nella circoscrizione Estero"; the Valle d'Aosta college elects one). 200 − 189 − 4 − 1 = **6 seats are
outside the fetched national aggregate**; which region's they are is not stated on the fetched page and is not asserted
here (G3). No SVP–PATT row appears in the fetched Senate table.

**Per-party totals (SECONDARY, G3):** no reachable primary page states them; the Senate's composition pages are behind the
WAF. The figures found by the search pass (FdI 66, PD 37, Lega 29, M5S 28, FI 18, AzIV 9, AVS 4, plus 4 Estero seats)
were read only in search-result summaries and are **not in any saved file**; they are listed here as the thing to
verify, not as a sourced row. **The Senate's composition by party is therefore a GAP** until senato.it can be fetched
(or the Interior Ministry publishes a per-party total the way the Camera's `returns_2022.md` also lacks).

### 1.4 The 2018 Camera (630 seats) — the XVIII's seat table

Primary [EL-C18] (Eligendo Archivio, "Camera 04/03/2018, Area ITALIA (escl. Valle d'Aosta)"), same shape as 1.3:

| list (Eligendo's name) | votes | % | proportional seats |
|---|---|---|---|
| LEGA | 5.698.687 | 17,35 | 73 |
| FORZA ITALIA | 4.596.956 | 14,00 | 59 |
| FRATELLI D'ITALIA CON GIORGIA MELONI | 1.429.550 | 4,35 | 19 |
| NOI CON L'ITALIA - UDC | 427.152 | 1,30 | — |
| MOVIMENTO 5 STELLE | 10.732.066 | 32,68 | 133 |
| PARTITO DEMOCRATICO | 6.161.896 | 18,76 | 86 |
| +EUROPA | 841.468 | 2,56 | — |
| ITALIA EUROPA INSIEME | 190.601 | 0,58 | — |
| CIVICA POPOLARE LORENZIN | 178.107 | 0,54 | — |
| SVP - PATT | 134.651 | 0,41 | 2 |
| LIBERI E UGUALI | 1.114.799 | 3,39 | 14 |

Uninominal by coalition: centre-right **111**, M5S **92**, centre-left (PD++Europa+Insieme+Civica Popolare+SVP–PATT)
**28**; totals "TOTALE CANDIDATI … 231", "LISTE … 386". Turnout "Elettori 46.505.350", "Votanti 33.923.321 | 72,94 %".
**DERIVED:** 386 + 231 = 617; + 1 (Valle d'Aosta) + 12 (Estero, the pre-2020 number) = 630. Per-party totals for 2018
need the uninominal split by party, which the fetched page does not give (G4).

**The XVIII Camera as it sat at the end of the legislature** — the composition nearest the game's July 2022 start —
is on camera.it [CAM-46], "Composizione dei gruppi parlamentari — Consistenza a fine Legislatura":

| group | members |
|---|---|
| FORZA ITALIA - BERLUSCONI PRESIDENTE | 68 |
| FRATELLI D'ITALIA | 40 |
| INSIEME PER IL FUTURO - IMPEGNO CIVICO | 49 |
| ITALIA VIVA-ITALIA C'E' | 32 |
| LEGA - SALVINI PREMIER | 131 |
| LIBERI E UGUALI-ARTICOLO 1-SINISTRA ITALIANA | 10 |
| MOVIMENTO 5 STELLE | 96 |
| PARTITO DEMOCRATICO | 97 |
| MISTO | 107 |
| — of which: ALTERNATIVA 14 · AZIONE-+EUROPA-RADICALI ITALIANI 6 · CENTRO DEMOCRATICO 5 · CORAGGIO ITALIA 11 · EUROPA VERDE-VERDI EUROPEI 5 · MAIE-PSI-FACCIAMOECO 5 · MANIFESTA, POTERE AL POPOLO, PARTITO DELLA RIFONDAZIONE COMUNISTA-SINISTRA EUROPEA 4 · MINORANZE LINGUISTICHE 4 · NOI CON L'ITALIA-USEI-RINASCIMENTO ADC 5 · VINCIAMO ITALIA - ITALIA AL CENTRO CON TOTI 10 | |

**DERIVED:** 68+40+49+32+131+10+96+97+107 = 630. This is the composition at 2022-10-12, not on 2022-07-21; the page
does not date each change, so any movement between July and October 2022 is unread (G4).

## 2. THE GOVERNMENT OF RECORD BY DATE

| from | to | president of the council | party | cabinet's parties | boundary event | source ids |
|---|---|---|---|---|---|---|
| 2022-07-01 | 2022-07-21 | Mario Draghi | **GAP** (G5: no fetched page states a party; governo.it lists no affiliation) | **GAP** (G5) | in office since 2021-02-13 [GV-DR]; resignation tendered 14 July and refused [CAM-726] | [GV-DR] [GV-LEG] [CAM-726] |
| 2022-07-21 | 2022-10-22 | Mario Draghi, **caretaker** ("per il disbrigo degli affari correnti") | GAP (G5) | GAP (G5) | resignation reiterated and taken note of; chambers dissolved the same day | [CAM-729] [DPR-96] [GV-DR] |
| 2022-10-22 | 2026-09-24 (open) | Giorgia Meloni | FdI | FdI, Lega, FI, NM (**DERIVED**, see below) | government in office from 22 Oct 2022 per governo.it; confidence: Camera 25 Oct, Senate 26 Oct | [GV-ME] [GV-LEG] [CAM-S4] [ANSA-1026] |

- [GV-DR] (governo.it, the Draghi government's page): "Governo Draghi (dal 13 febbraio 2021 al 22 ottobre 2022) XVIII Legislatura Presidente del Consiglio dei Ministri Mario Draghi".
- [GV-ME] (governo.it, the Meloni government's page, as served on the access date): "Governo Meloni (dal 22 ottobre 2022) XIX Legislatura Presidente del Consiglio dei Ministri Giorgia Meloni". The same page carries changes into 2026, e.g. "Turismo Ministro: Gianmarco Mazzi (dal 03/04/2026) [Daniela Garnero Santanché, dimissioni Dpr 26/03/2026; Giorgia Meloni, ad interim, dal 26/03/2026, dimissioni Dpr 03/04/2026]". **DERIVED:** the page shows a government still being amended in April 2026 and [GV-LEG] shows it "in carica" — no change of government to the access date. The swearing-in as an event (the oath before the President) is on quirinale.it, unreachable: the date rests on governo.it's "dal 22 ottobre 2022" (G6).
- **Camera confidence, 2022-10-25** [CAM-S4] ("Seduta n. 4 di martedì 25 ottobre 2022"):
  > "Comunico il risultato della votazione per appello nominale sulla mozione di fiducia Foti, Molinari, Cattaneo e Lupi n. 1-00002 : Presenti: […] 394 Votanti: […] 389 Astenuti: […] 5 Maggioranza: […] 195 Hanno risposto sì : […] 235 Hanno risposto no : […] 154 (La Camera approva) (Applausi dei deputati dei gruppi Fratelli d'Italia, Lega-Salvini Premier, Forza Italia-Berlusconi Presidente-PPE e Misto-Noi Moderati (Noi con l'Italia, Coraggio Italia, UDC e Italia al Centro)-MAIE)"

  (the "[…]" stand for the record's dotted leaders). **DERIVED — the cabinet's parties:** no fetched page lists the
  ministers' parties. The motion's four signatories are the group leaders of FdI, Lega, FI and NM, and the applause line
  names those four groups (with MAIE inside the Misto-Noi Moderati component): the majority is FdI+Lega+FI+NM. Whether
  every minister belongs to one of them is not on a fetched page (G5).
- **Senate confidence, 2022-10-26** [ANSA-1026] (**SECONDARY**; ANSA, datePublished 2022-10-26T21:04): "Giorgia Meloni nell'Aula al Senato dove incassa la fiducia facendo l'en plein con 115 sì"; the article's canonical URL carries its headline "il-senato-vota-la-fiducia-al-governo-meloni-115-si-79-no-e-5-astenuti". The Senate's own record is unreachable (G2).
- **Art. 94's ten-day clock, DERIVED:** 22 Oct + 10 days = 1 Nov 2022; the Camera vote fell on day 3 and the Senate's on day 4.

**The President of the Republic** throughout the window: **Sergio Mattarella**, elected 2022-01-29 and sworn in
2022-02-03 [CAM-PR] [CAM-GIU] — before the window opens, and his seven-year term runs past its close (**DERIVED**; the
term is Art. 85's, not quoted here). quirinale.it is unreachable; the dates are the Camera's:
- [CAM-PR] (comunicazione.camera.it, the election event page) lists "29/01/2022 Sergio Mattarella eletto Presidente della Repubblica" and "03/02/2022 Giuramento del Presidente della Repubblica Sergio Mattarella".
- [CAM-GIU] (the ceremony page): "Il Presidente Fico dichiara aperta la seduta ed invita il Capo dello Stato a prestare giuramento a norma dell'articolo 91 della Costituzione." — dated in the page "giovedì 3 febbraio".

## 3. PARTY LEADERS OF THE SEATED PARTIES BY DATE (XIX Camera)

Office words are the party's own. "as served" means the page shows the office holder today and gives no date for the
appointment; the register carries the page's own date where it has one.

| key | leader | office word (the party's) | from | to | status | source ids |
|---|---|---|---|---|---|---|
| FdI | Giorgia Meloni | Presidente nazionale | 2014-03-08 | open | on record (page last modified 2024-03-22) | [FDI] |
| PD | — | segretario/segretaria | — | — | **GAP** (G7): partitodemocratico.it answers 403 to every fetch | — |
| Lega | Matteo Salvini | Segretario federale | (no date on the page) | open | as served (page modified 2025-12-16) | [LEGA] |
| M5S | Giuseppe Conte | Presidente | 2021-08-06 (elected); re-elected, proclaimed 2025-10-26 | open | on record | [M5S-21] [M5S-25] |
| FI | Antonio Tajani | Segretario Nazionale | 2024-02-24 (congress) | open | on record | [FI] |
| FI | — | — | 2022-07-01 | 2024-02-24 | **GAP** (G8): who led FI before the 2024 congress is on no fetched page | — |
| AzIV (Azione) | Carlo Calenda | segretario nazionale | 2025-02-16 (**DERIVED** year) | open | on record | [AZ] |
| AzIV (Italia Viva) | Matteo Renzi | Presidente dell'Associazione | (no date on the page) | open | as served | [IV] |
| AVS (Europa Verde) | Angelo Bonelli and Fiorella Zabatta | co-portavoce | 2024-11-30 | open | on record; the pair before that date is a **GAP** (G9) | [EV] |
| AVS (Sinistra Italiana) | — | segretario nazionale | — | — | **GAP** (G9): sinistraitaliana.si serves only a cookie wall | — |
| NM | Maurizio Lupi | (no office word on the page: "sotto la guida di Maurizio Lupi") | — | open | partial (G10) | [NM] |
| SVP | Philipp Achammer → Dieter Steger | Obmann (Parteiobmann) | Steger from 2024-05-04 | open | on record | [SVP] |
| PlusE | Riccardo Magi | Segretario | 2023-02-28 (page date) | open | on record; the predecessor is a **GAP** (G11) | [PE] |
| IC | — | — | — | — | **GAP** (G12): no party site found | — |
| ScN | — | — | — | — | **GAP** (G12): the site was fetched and carries no leader text | [SCN] |
| MAIE | — | — | — | — | **GAP** (G12): maie.it's pages carry no leader text | — |
| UV | Joël Farcoz | président | "à la suite de la Réunion de 2024" (no day on the page) | open | on record | [UV-P] |

- [FDI]: "L'8 marzo 2014, dopo essersi candidata alle primarie di FdI-An, viene eletta Presidente nazionale di Fratelli d'Italia-Alleanza Nazionale dal congresso di Fiuggi."
- [LEGA] (organigramma): "Organigramma Segretario federale Sen. Matteo Salvini Vicesegretario federale Sen. Claudio Durigon".
- [M5S-21] (6 Aug 2021): "Giuseppe Conte eletto Presidente del MoVimento 5 Stelle" — "Postato il 6 agosto 2021 da Vito Claudio Crimi (Presidente del Comitato di Garanzia)".
- [M5S-25] (26 Oct 2025): "Alle ore 18 di oggi 26 ottobre 2025 si sono concluse le votazioni degli iscritti per l'elezione del presidente del MoVimento 5 Stelle." — "Su 101.783 iscritti aventi diritto al voto hanno votato in 59.720 pari al 58,67%." — "Hanno votato SI: 53.353 Hanno votato NO: 6.367 Pertanto, ai sensi dell'art. 12 lett. h) dello Statuto, Giuseppe Conte è proclamato eletto presidente del MoVimento 5 Stelle."
- [FI]: "Risultati del Congresso Nazionale del 23 – 24 febbraio 2024 Eletto all'unanimità Antonio Tajani Segretario Nazionale di Forza Italia".
- [AZ]: "Congresso 2024/2025 La Commissione Congressuale, verificati i risultati delle votazioni nel congresso di Azione alle ore 01:00 del 16 febbraio, ha proclamato l'elezione a segretario nazionale di Carlo Calenda con l'85,7% dei voti, mentre Giulia Pastorella ha ottenuto il 14,3%." **DERIVED:** the year 2025 is read from the heading "Congresso 2024/2025"; the page's own metadata is dated 2026-03-20.
- [IV]: "COMITATO NAZIONALE ITALIA VIVA Membri di diritto: Matteo Renzi (Presidente dell'Associazione)".
- [EV] (verdisinistra.it, published 2024-11-30): "Nella prima giornata dell'Assemblea Nazionale di Europa Verde, in corso a Chianciano, Fiorella Zabatta e Angelo Bonelli sono stati eletti, a larga maggioranza, co-portavoce".
- [NM]: "Fin dall'inizio, sotto la guida di Maurizio Lupi, abbiamo scelto di essere parte integrante della coalizione di centrodestra."
- [SVP] (04.05.2024): "Unter langanhaltendem Applaus wurde heute bei der 66. SVP-Landesversammlung im Meraner Kursaal Philipp Achammer verabschiedet; er stand seit zehn Jahren an Spitze der Südtiroler Volkspartei. Seine Nachfolge tritt Dieter Steger an, auf den bei der Wahl 95,47 Stimmrechte entfielen." *gloss:* Achammer led the party for ten years to 2024-05-04; Steger succeeds him.
- [PE] (28/02/2023): "Riccardo Magi eletto Segretario, Carla Taibi Tesoriera e Federico Pizzarotti Presidente".
- [UV-P]: "Joël Farcoz Élu président à la suite de la Réunion de 2024".

## 4. THE CONSTITUTIONAL RULES (§5.4, §11) — normattiva's consolidated text of the Costituzione

Source [COST-88] [COST-92] [COST-94]: normattiva.it, "Costituzione della Repubblica Italiana", codice redazionale
047U0001, article pages (the Gazzetta's text as consolidated; senato.it and quirinale.it are unreachable — G13).

**Art. 88 — dissolution** ("Testo in vigore dal: 23-11-1991"):
> "Il Presidente della Repubblica può, sentiti i loro Presidenti, sciogliere le Camere o anche una sola di esse. ((Non può esercitare tale facoltà negli ultimi sei mesi del suo mandato, salvo che essi coincidano in tutto o in parte con gli ultimi sei mesi della legislatura)) ."

(the double parentheses and the space before the final stop are normattiva's own marking of the 1991 amendment)

**Art. 92 — the government's appointment** ("Testo in vigore dal: 1-1-1948"):
> "Il Governo della Repubblica è composto del Presidente del Consiglio e dei ministri, che costituiscono insieme il Consiglio dei ministri. Il Presidente della Repubblica nomina il Presidente del Consiglio dei ministri e, su proposta di questo, i ministri."

**Art. 94 — confidence in BOTH chambers** ("Testo in vigore dal: 1-1-1948"):
> "Il Governo deve avere la fiducia delle due Camere. Ciascuna Camera accorda o revoca la fiducia mediante mozione motivata e votata per appello nominale. Entro dieci giorni dalla sua formazione il Governo si presenta alle Camere per ottenerne la fiducia. Il voto contrario di una o d'entrambe le Camere su una proposta del Governo non importa obbligo di dimissioni. La mozione di sfiducia deve essere firmata da almeno un decimo dei componenti della Camera e non può essere messa in discussione prima di tre giorni dalla sua presentazione."

*gloss (for the wiring):* confidence is granted or revoked by each chamber separately, by a reasoned motion voted by
roll call; a new government presents itself to both chambers within ten days; a lost vote on a government proposal does
not oblige resignation; a censure motion needs one tenth of a chamber's members and a three-day wait. The 2022 crisis
was not a censure motion: it was a government-requested confidence vote in the Senate followed by a resignation
(section 1.2), and the dissolution was the President's act under Art. 88 ([DPR-96] cites the article by name).

## What this file says about `returns_2022.md` and the spec's §4

- **Nothing here contradicts `returns_2022.md`.** Its Camera seat totals stay [UNCONFIRMED]; this pass found no
  reachable primary per-party total for the Camera either, and the same holds for the Senate (G3).
- **One of its caveats is wrong and is corrected here:** it names `dait.interno.gov.it/documenti/dossier-elezioni-politiche-2022.pdf`
  as "the candidate primary source for the seat totals". The PDF was fetched whole (12,655,890 bytes, HTTP 200) and
  its text inflated: it is the Ministry's **pre-election** dossier, signed "Roma, 21 settembre 2022 Claudio Sgaraglia
  Capo Dipartimento", with "tabelle analitiche aggiornate alla data del 20 settembre 2022" — it contains no results. It
  was not kept (the fetch log marks it) [DOS]. What it does state, and this file uses once (section 1.3): "Il numero dei
  senatori elettivi è di duecento, quattro dei quali eletti nella circoscrizione Estero."
- **§4's "the day the government fell in July 2022"** is three dated days on the record (14, 20, 21 July); the
  caretaker period begins 21 July. See the DERIVED note under 1.2. §4's "25 Sep 2022, snap" and "Draghi's caretaker
  government" are consistent with the record.
- **§4's status column** says "Senato, 200 (new)" — the Senato's composition by party is the one item in §11's bill
  this pass could not source from a primary page (G3).

## Source register
(all accessed 2026-09-24; saved as `raw/records/<file>`; "page's own date" is the date the page shows, or its
metadata where marked; sizes and SHA-256 as in `raw/records/SHA256SUMS.txt`)

| id | URL | publisher | page's own date | basis | file | bytes | SHA-256 |
|---|---|---|---|---|---|---|---|
| [COST-88] | https://www.normattiva.it/atto/caricaArticolo?art.versione=2&art.idGruppo=8&art.flagTipoArticolo=0&art.codiceRedazionale=047U0001&art.idSottoArticolo=1&art.idSottoArticolo1=10&art.dataPubblicazioneGazzetta=1947-12-27&art.progressivo=0&art.idArticolo=88 | normattiva.it (Istituto Poligrafico e Zecca dello Stato) | "Testo in vigore dal: 23-11-1991" | consolidated constitutional text, article view (session cookie needed) | `normattiva_costituzione_art88.html` | 4761 | `74b18b7b88afb2bd4e622de1f6f4af11414d7723e49998d117301a32d493f242` |
| [COST-92] | same, `art.versione=1&art.idGruppo=9` … `art.idArticolo=92` | normattiva.it | "Testo in vigore dal: 1-1-1948" | as above | `normattiva_costituzione_art92.html` | 4140 | `0e4225efd2ca29d17b0c45be0d074aa71910e3f1ef0737655fae0ba62a4db471` |
| [COST-94] | same, `art.versione=1&art.idGruppo=9` … `art.idArticolo=94` | normattiva.it | "Testo in vigore dal: 1-1-1948" | as above | `normattiva_costituzione_art94.html` | 4439 | `91cc64f22570493806f31a6a335210cc97138ee7191f997a9b96b1d5c13ed553` |
| [COST-0] | https://www.normattiva.it/uri-res/N2Ls?urn:nir:stato:costituzione:1947-12-27 | normattiva.it | — | the act's shell page (article index; no article text) | `normattiva_costituzione.html` | 165727 | `a44c993780172cefc9833ff4311db9b34ffcb2bd2e4c76550833af4ba0063943` |
| [DPR-96] | https://www.normattiva.it/uri-res/N2Ls?urn:nir:stato:decreto.del.presidente.della.repubblica:2022-07-21;96 | normattiva.it | "Dato a Roma, addì 21 luglio 2022" | D.P.R. 96/2022 (dissolution) | `normattiva_dpr_2022-07-21_96_scioglimento.html` | 60621 | `5c503d3884153245ae38e714f0f597edc79b920b81cb86516eeff753780f45fb` |
| [DPR-97] | https://www.normattiva.it/uri-res/N2Ls?urn:nir:stato:decreto.del.presidente.della.repubblica:2022-07-21;97 | normattiva.it | 21 luglio 2022 | D.P.R. 97/2022 (comizi, first sitting) | `normattiva_dpr_2022-07-21_97_comizi.html` | 62055 | `68d111ab98fbfcfa8011fe4c06483cda1986cf6453adcb9932f91372f0b5dfa1` |
| [ST-18] | https://storia.camera.it/legislature/leg-repubblica-XVIII | Camera dei deputati, Portale storico | — | legislature page | `storia_camera_leg_XVIII.html` | 61325 | `3a65ad8d82165d05fdb3b732a8d89dc2adfa19824db02f08ad5c53708f617308` |
| [GV-LEG] | https://www.governo.it/it/i-governi-dal-1943-ad-oggi/i-governi-nelle-legislature/192 | Presidenza del Consiglio dei Ministri | live, as served | governments by legislature | `governo_governi-nelle-legislature_192.html` | 58978 | `a5ff9a844502e1b70f3707eb3eddb000b301c86278da24b46c7533ff480c1ecb` |
| [GV-DR] | https://www.governo.it/it/i-governi-dal-1943-ad-oggi/xviii-legislatura-dal-23-marzo-2018/governo-draghi/16211 | Presidenza del Consiglio dei Ministri | — | the Draghi government's page | `governo_governo-draghi_16211.html` | 57274 | `088074b785a2d12e917ab138a549a56025e943c8349ea09fa7a1f650ecc659b6` |
| [GV-ME] | https://www.governo.it/it/i-governi-dal-1943-ad-oggi/governo-meloni/20727 | Presidenza del Consiglio dei Ministri | live, as served (changes to 03/04/2026 shown) | the Meloni government's page | `governo_governo-meloni_20727.html` | 57355 | `30727b84c87f693eae7dc8c2b9007efe297b53f16ffc25fbd353d5c897482a63` |
| [CAM-726] | https://www.camera.it/leg18/410?idSeduta=0726&tipo=stenografico | Camera dei deputati | "Seduta n. 726 di lunedì 18 luglio 2022" | stenographic record | `camera_leg18_seduta0726_stenografico_2022-07-18.html` | 49480 | `2c0cbfd7fdfb60a358c0f45a66822855be31b9dfe1abb97e09ff12b6bf0d77cb` |
| [CAM-729] | https://www.camera.it/leg18/410?idSeduta=0729&tipo=stenografico | Camera dei deputati | "Seduta n. 729 di giovedì 21 luglio 2022" | stenographic record | `camera_leg18_seduta0729_stenografico_2022-07-21.html` | 61723 | `1ba661ebb17fd8a90a0ee5749fde14c845399bb302d95a8edb152f1572b96487` |
| [CAM-C21] | https://comunicazione.camera.it/archivio-prima-pagina/18-26711 | Camera dei deputati (comunicazione) | "notizia pubblicata il 21 Luglio 2022" | news page | `comunicazione_camera_18-26711_draghi-dimissioni.html` | 22969 | `cb666d4a43d463684e2c3c4eab115696f82a88f8c9deec9c4748ad5689dee365` |
| [CAM-S1] | https://www.camera.it/leg19/410?idSeduta=0001&tipo=stenografico | Camera dei deputati | "Seduta n. 1 di giovedì 13 ottobre 2022" | stenographic record | `camera_leg19_seduta0001_stenografico.html` | 203923 | `766d8b815a10bf2a9a03066a384b8c60e7bae9bb159530e9dd07a73c31d16f4b` |
| [CAM-S4] | https://www.camera.it/leg19/410?idSeduta=0004&tipo=stenografico | Camera dei deputati | "Seduta n. 4 di martedì 25 ottobre 2022" | stenographic record (confidence vote) | `camera_leg19_seduta0004_stenografico_2022-10-25.html` | 582602 | `ac7c214002ade3334fac8681a115254931428cb39f9a262b8d05f2d71192c4e4` |
| [CAM-S5] | https://www.camera.it/leg19/410?idSeduta=0005&tipo=stenografico | Camera dei deputati | "Seduta n. 5 di mercoledì 26 ottobre 2022" | fetched while locating the vote; not quoted | `camera_leg19_seduta0005_stenografico.html` | 86298 | `1325b18dcf57a4c18fa6736864ab979f5242acb56eb456f5bbbd76bd57983974` |
| [CAM-FON] | https://comunicazione.camera.it/archivio-prima-pagina/18-27036 | Camera dei deputati (comunicazione) | (Oct 2022; first sitting and Fontana's election) | news page | `comunicazione_camera_18-27036_fontana-eletto.html` | 21544 | `ed275faabf662325035946cc0396b8e2e6d733e884fbc03eabb374332503b3e5` |
| [CAM-PR] | https://comunicazione.camera.it/eventi/presidentedellarepubblica2022 | Camera dei deputati (comunicazione) | items dated 29/01/2022–07/02/2022 | event page, the 2022 presidential election | `comunicazione_camera_eventi_presidentedellarepubblica2022.html` | 122186 | `71aa486a28cd50271f36e10ee08dcf0970c566fbcf222e11ce3d936d5b021e89` |
| [CAM-GIU] | https://comunicazione.camera.it/archivio-prima-pagina/18-25122 | Camera dei deputati (comunicazione) | "giovedì 3 febbraio" (2022) | the swearing-in ceremony page | `comunicazione_camera_18-25122_giuramento-mattarella.html` | 22159 | `83e47b74ad964b540b400db5c5d21220b88477b01af2d638a97871260cbbceed` |
| [CAM-46] | https://www.camera.it/leg18/46 | Camera dei deputati | "Consistenza a fine Legislatura" | XVIII group composition | `camera_leg18_46_gruppi.html` | 43773 | `41bbec52dc911c90805dd7847d40807d4dd5f3c7e7b5771bed7d4778c6874e04` |
| [CAM-842] | https://www.camera.it/leg18/842 | Camera dei deputati | — | XVIII "Elezioni" page (links only; not quoted) | `camera_leg18_842_elezioni-2018.html` | 47128 | `510ff0e659e5a1fb86a9945a199170a2328bea2bbcbaa84e4b2ead11edf12f2d` |
| [EL-S22] | https://elezionistorico.interno.gov.it/index.php?tpel=S&dtel=25/09/2022&tpa=I&tpe=A&lev0=0&levsut0=0&es0=S&ms=S | Ministero dell'Interno – DAIT (Eligendo Archivio) | Senato 25/09/2022 | Area ITALIA (escl. Valle d'Aosta) | `eligendo_senato_2022_area-italia.html` | 45692 | `647562c18c3072abf0805f6a0a9c401845611ba281c6ff6f1334c193896203ff` |
| [EL-C18] | https://elezionistorico.interno.gov.it/index.php?tpel=C&dtel=04/03/2018&tpa=I&tpe=A&lev0=0&levsut0=0&es0=S&ms=S | Ministero dell'Interno – DAIT (Eligendo Archivio) | Camera 04/03/2018 | Area ITALIA (escl. Valle d'Aosta) | `eligendo_camera_2018_area-italia.html` | 50518 | `44153fd6edd0749262e5f52092fdc736368865ce2adfae867b48c57a3dd7b10b` |
| [ANSA-0720] | https://www.ansa.it/sito/notizie/politica/2022/07/20/la-crisi-di-governo-dal-senato-fiducia-a-draghi-con-95-si.-il-premier-annuncera-le-dimissioni-alla-camera_20b10df7-5fc0-45b2-a1a7-0ba87fe5153c.html | ANSA (**SECONDARY**) | "20 luglio 2022, 21:47" | news article; body partly behind a consent wall — the standfirst is in the bytes | `ansa_2022-07-20_senato-fiducia-draghi-95.html` | 234245 | `466314578d41f200cce4ef7a1980a1128ce20697d332846441580112133f5c6c` |
| [ANSA-1026] | https://www.ansa.it/sito/notizie/politica/2022/10/26/il-senato-vota-la-fiducia-al-governo-meloni-115-si-79-no-e-5-astenuti_1f316e11-70d5-491d-801f-7e5cea2d1b71.html | ANSA (**SECONDARY**) | datePublished 2022-10-26T21:04 | news article | `ansa_2022-10-26_senato-fiducia-meloni-115.html` | 235562 | `96fc4846a68a9a47d9b9f3dd75b6977c9b6a95ac09e49b9521b9ece0e48814b7` |
| [FDI] | https://www.fratelli-italia.it/giorgia-meloni/ | Fratelli d'Italia | modified 2024-03-22 (metadata) | party page | `fdi_giorgia-meloni.html` | 153244 | `ea6c03527f506d8ddd180fdd4e0f48f772392b33b67051bce485025345c86d3e` |
| [LEGA] | https://legaonline.it/organigramma/ | Lega per Salvini Premier | modified 2025-12-16 (metadata) | party organigramma (browser UA needed) | `lega_organigramma.html` | 331277 | `83c3cd3953547a26b653eb5ac8baf38cc72171f58f27339a3de5ffcbe4ab03b6` |
| [LEGA-H] | https://www.legaonline.it/ | Lega per Salvini Premier | live | home page (links only; not quoted) | `lega_legaonline_home.html` | 387650 | `f6ff77d8d6746e224d310451256ee4bb5c9223ff4f752a95d9f911ecb29f528e` |
| [M5S-21] | https://www.movimento5stelle.eu/giuseppe-conte-eletto-presidente-del-movimento-5-stelle/ | Movimento 5 Stelle | "Postato il 6 agosto 2021" | party notice | `m5s_conte-eletto-presidente.html` | 115158 | `c84808703624f2efa0bfd8dd5476a417c874583de252e98a88afe24afcf056d4` |
| [M5S-25] | https://www.movimento5stelle.eu/elezioni-del-presidente-del-movimento-5-stelle-risultati/ | Movimento 5 Stelle | "Ottobre 26, 2025 18:51" | party notice (results) | `m5s_elezioni-presidente-risultati.html` | 115276 | `79edb9398a0b7c65ffd111aa934ee9666080f451f1f74553fc47ed0a4b01abac` |
| [FI] | https://forzaitalia.it/risultati-congresso-nazionale-forza-italia/ | Forza Italia | "24 Febbraio 2024" | party notice (congress results) | `forzaitalia_risultati-congresso-nazionale.html` | 83064 | `907c92b4fb94bfc8a9cbef765dd7945a2e04944408a86a28371e4adec4f5981a` |
| [AZ] | https://www.azione.it/congresso-2024-2025/ | Azione | published 2026-03-20 (metadata); the event "16 febbraio" | party page | `azione_congresso-2024-2025.html` | 100505 | `c903fbbe2db9aa91b7143a5ada020681a248e3a6b2727f13c56bd2a7e5a453de` |
| [IV] | https://www.italiaviva.it/organi_di_italia_viva | Italia Viva | — ("Assemblea #010") | party organs page | `italiaviva_organi.html` | 91179 | `37af85cb79cd17677b5d152614a957fdd5b35be1cfd00c3af981f634858403d6` |
| [EV] | https://verdisinistra.it/assemblea-nazionale-di-europa-verde-fiorella-zabatta-e-angelo-bonelli-eletti-co-portavoce-del-partito/ | Alleanza Verdi e Sinistra (verdisinistra.it) | published 2024-11-30T19:09:45+00:00 | party notice | `verdisinistra_zabatta-bonelli-coportavoce.html` | 79627 | `34c153dd337eb7149ce4066652ffd8441c15cb86d32e2dd7645a64b789deca8c` |
| [NM] | https://www.noimoderati.it/il-partito | Noi Moderati | — | party page | `noimoderati_il-partito.html` | 124947 | `52cfb09fed77b84aeeb1c766931d9e8daf5417da87457ce653003ac9e2257f4e` |
| [SVP] | https://www.svp.eu/de/svp-obmannschaft-dieter-steger-folgt-auf-philipp-achammer--1-4533.html | Südtiroler Volkspartei | "04.05.2024" | party notice | `svp_steger-folgt-auf-achammer.html` | 50810 | `88f9bf7d4b675d1c4a55f06674e9c7253c06878053dbb1233f66dc125f259526` |
| [PE] | https://www.piueuropa.eu/riccardo_magi_eletto_segretario | +Europa | "28/02/2023" | party notice | `piueuropa_magi-eletto-segretario.html` | 89097 | `50ee366e8dbb45c237b685e6922b6edc18e5c8963aabb8fda735ba53d4398a97` |
| [SCN] | http://www.sudchiamanord.it/ | Sud chiama Nord | — | home page (a script shell; no leader text) | `sudchiamanord_home.html` | 18185 | `857ddda80390070d6d4910def80224af78d70fd7908592761d851ddbc568cb65` |
| [UV-M] | https://www.unionvaldotaine.org/mouvement/ | Union Valdôtaine | — | "Mouvement" page (links only; not quoted) | `unionvaldotaine_mouvement.html` | 164323 | `dcb6b62098f65b892b19e588a45677b77b2910c73dd85cd1804fa970aba73fb8` |
| [UV-P] | https://www.unionvaldotaine.org/mouvement/president/ | Union Valdôtaine | modified 2026-04-22 (metadata) | "Président" page | `unionvaldotaine_president.html` | 163351 | `65433c92c8cf90675bb98d0bceae37c1da2efb4119299e0eb669b89523efcd38` |
| [DOS] | https://dait.interno.gov.it/documenti/dossier-elezioni-politiche-2022.pdf | Ministero dell'Interno – DAIT | "Roma, 21 settembre 2022" | pre-election dossier, 12,655,890 bytes; **fetched, read, NOT KEPT** (see the fetch log) | — | — | — |

Fetched and not kept (all in the fetch log): `storia.camera.it/legislature/leg-repubblica-XIX` (HTTP 500 twice);
`gazzettaufficiale.it/anteprima/codici/costituzione` (a listing without article text); `partitodemocratico.it` pages
(HTTP 403 with three header sets); every `senato.it` and `quirinale.it` URL (challenge pages, no content).

## GAPS

- **G1: the Senate's own first-sitting record for 2022-10-13** was not fetched (senato.it unreachable). The date rests on
  [DPR-97] ("prima riunione delle Camere") and the Camera's own sitting.
- **G2: the Senate's 2022 confidence votes are on no fetched official page.** 20 July 2022 (95–38; M5S, Lega, FI not
  voting) and 26 October 2022 (115–79–5) rest on ANSA, marked SECONDARY on every line. The 14 July M5S non-participation
  on the *decreto Aiuti* is in no saved file at all — the row in 1.2 is billed, not sourced.
- **G3: the 2022 Senate's composition by party (200 seats)** — the spec's §10 decision 8 item — has no reachable primary.
  Eligendo gives the proportional seats per list and the uninominal per coalition (section 1.3); 6 seats are outside its
  national aggregate; the per-party totals seen in search summaries are not in any saved file.
- **G4: the 2018 Camera's per-party totals** have the same shape (proportional per list, uninominal per coalition); the
  end-of-legislature group table [CAM-46] is dated 2022-10-12, not July 2022.
- **G5: Draghi's party affiliation and the parties of the Draghi and Meloni cabinets** are on no fetched page; the Meloni
  majority's four groups are DERIVED from the confidence motion's signatories and the applause line.
- **G6: the Meloni government's swearing-in as an event** (the oath at the Quirinale) is unreachable; the date is
  governo.it's "dal 22 ottobre 2022".
- **G7: the PD's segretario/segretaria by date** — partitodemocratico.it refused every fetch (HTTP 403).
- **G8: Forza Italia's leadership from 2022-07-01 to the 2024-02-24 congress** is on no fetched page.
- **G9: AVS's two components** — Sinistra Italiana's segretario (site serves a cookie wall only) and Europa Verde's
  co-portavoce before 2024-11-30.
- **G10: Noi Moderati's office word** — the page names Lupi's leadership without an office.
- **G11: +Europa's segretario before 2023-02-28.**
- **G12: IC, ScN and MAIE** — no leader text on any reachable page (IC has no site found; ScN's home is a script shell;
  maie.it's pages carry no leader line).
- **G13: the Constitution from senato.it or quirinale.it** — both unreachable; the text is normattiva's consolidated
  version, article pages, which the Gazzetta's own site does not serve to a plain fetch.
- **G14: second-session re-verification (R-K9) is pending**, as for every SOURCED file.

*(Filed 2026-09-24 by the PS-1 Italy sourcing agent. Nothing outside `ElectionsData/italy/records_by_date.md` and
`ElectionsData/italy/raw/records/` was written. No code was touched, Unity was not run, nothing was committed. Files
under `raw/records/` whose names begin `archives_`, `bidenwhitehouse_`, `constitution_a`, `history_`, `senate_` or
`whitehouse_` were written there by a sibling agent's USA pass during this session, are not this file's, and are not
in `SHA256SUMS.txt`.)*
