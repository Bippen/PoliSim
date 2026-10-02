# Poland - 2025 presidential run-off transfers, Ipsos exit poll and late poll (item B4)

Sourced 2026-10-02 for the designer's ruling: *"[FITTED] to the Ipsos exit poll of 1 Jun 2025 (share of each first-round electorate voting
Nawrocki/Trzaskowski): Mentzen 88.1/11.9, Braun 92.5/7.5, Hołownia 13.8/86.2, Zandberg 16.2/83.8, Biejat 9.8/90.2."*

Files in `raw/exit_poll_2025/` (SHA-256 in its `SHA256SUMS.txt`). Figures are machine-readable in `runoff_transfers_2025.csv`. A gloss in
italics follows each quotation.

## What exists, and which page is primary

- **Ipsos does not publish the flow table itself.** Its exit-poll page (`ipsos_pl-pl_exit-poll.html`) explains the method and sends readers to
  the broadcasters ("Wyniki ostatnich badań exit poll dostępne są między innymi na stronach: ..." with TVN24 links). No 2025 table, PDF or
  press release was found on ipsos.com.
- The poll was made **for TVN24, TVP and Polsat News**. Their articles are the primary publication: Polsat News, TVN24 and TVN24's Konkret24.
  TVP Info's 21:00 page is a JavaScript shell with no figures in the served HTML.
- There are **two vintages**:
  - the **exit poll** (published at 21:00 on 1 Jun 2025): Trzaskowski 50.3, Nawrocki 49.7, turnout 72.8;
  - the **late poll**: 23:00 Nawrocki 50.7 / 49.3, then ~01:00 Nawrocki 51.0 / 49.0, turnout 71.7.

  The articles give flows for both vintages but never say which late-poll run their late figures come from.
- The flows are **shares of each first-round electorate among those who voted in the run-off** (each pair sums to 100). **No page gives the
  share of a first-round electorate that stayed home on 1 June**, and an exit poll cannot measure it: only run-off voters are interviewed. The
  CSV's `did_not_vote` column is therefore empty.

## 1. The exit poll, as published by Polsat News (commissioning broadcaster) - the ruling's five pairs

Source: `polsatnews_2025-06-01_przeplywy_w_ii_turze.html`, "Przepływy w II turze. Tak głosowali wyborcy innych kandydatów", published
2025-06-01 21:10 CEST.

> Z sondażu Ipsos wynika również, że najwięcej wyborców Sławomira Mentzena, który w pierwszej turze zajął trzecie miejsce, zagłosowało w
> drugiej turze na Karola Nawrockiego - 88,1 proc. Rafał Trzaskowski uzyskał w tej grupie wyborców 11,9 proc. Również wyborcy Grzegorza Brauna,
> który w pierwszej turze był czwarty, chętniej poparli kandydata popieranego przez PiS (92,5 proc.) niż kandydata KO (7,5 proc.).
>
> *Gloss: according to the Ipsos poll, **88.1 %** of Mentzen's voters went to Nawrocki and **11.9 %** to Trzaskowski. Braun's voters went to
> the PiS-backed candidate (**92.5 %**) rather than the KO candidate (**7.5 %**).*

> Rafał Trzaskowski przejął z kolei większość głosów wyborców, którzy w pierwszej turze głosowali na Szymona Hołownię (86,2 proc.). Na Karola
> Nawrockiego postawiło 13,8 proc. wyborców marszałka Sejmu. Na Trzaskowskiego chętniej głosowali również wyborcy Adriana Zandberga (83,8
> proc.). Karol Nawrocki uzyskał w tej grupie 16,2 proc. Podobnie było w przypadku wyborców Magdaleny Biejat, którzy w zdecydowanej większości
> poparli kandydata KO (90,2 proc.) zamiast kandydata popieranego przez PiS (9,8 proc.).
>
> *Gloss: Hołownia's voters split **86.2 %** Trzaskowski / **13.8 %** Nawrocki, Zandberg's **83.8 %** / **16.2 %**, and Biejat's **90.2 %**
> Trzaskowski / **9.8 %** Nawrocki.*

TVN24's own articles (`tvn24_st8487352_wyborcy_mentzena.html`, `tvn24_st8487890_wyborcy_holowni.html`, `tvn24_st8486898_wyborcy_zandberga.html`,
first published 21:02 CEST) give the same exit-poll pairs for Mentzen (88.1/11.9), Hołownia (86.2/13.8) and Zandberg (83.8/16.2), each
attributed "Według sondażu exit poll Ipsos dla TVN24, Polsat News i TVP", with "Błąd oszacowania w badaniu exit poll to +/- 2 punkty
procentowe" (an error margin of ±2 points).

## 2. The full exit-poll table, all eleven eliminated candidates [SECONDARY]

Source: `secondary_polskieradio24_3531756_przeplywy_exit_poll.html` (Polskie Radio 24, published 2025-06-01 21:56, modified 22:17), which
attributes the figures to "badania exit poll przeprowadzonego przez Ipsos dla TVP, TVN i Polsatu" and signs them "Dane exit poll z godziny 21".
It is the only page found that lists every candidate:

> Tak rozłożyły się głosy wyborców kandydatów, którzy nie przeszli do drugiej tury wyborów prezydenckich: - Bartoszewicz Artur - Karol
> Nawrocki 67.3 proc. ; Rafał Trzaskowski 32.7 proc. - Biejat Magdalena - Karol Nawrocki 9.8 proc. ; Rafał Trzaskowski 90.2 proc. - Braun
> Grzegorz - Karol Nawrocki 92.5 proc. ; Rafał Trzaskowski 7.5 proc. - Hołownia Szymon - Karol Nawrocki 13.8 proc. ; Rafał Trzaskowski 86.2
> proc. - Jakubiak Marek - Karol Nawrocki 90.3 proc. ; Rafał Trzaskowski 9.7 proc. - Maciak Maciej - Karol Nawrocki 72.9 proc. ; Rafał
> Trzaskowski 27.1 proc. - Mentzen Sławomir - Karol Nawrocki 88.1 proc. ; Rafał Trzaskowski 11.9 proc. - Senyszyn Joanna - Karol Nawrocki
> 18.9 proc. ; Rafał Trzaskowski 81.1 proc. - Stanowski Krzysztof - Karol Nawrocki 51.2 proc. ; Rafał Trzaskowski 48.8 proc. - Woch Marek -
> Karol Nawrocki 65.4 proc. ; Rafał Trzaskowski 34.6 proc. - Zandberg Adrian - Karol Nawrocki 16.2 proc. ; Rafał Trzaskowski 83.8 proc.
>
> *Gloss: "This is how the votes of the candidates who did not reach the second round were distributed" - Nawrocki first, Trzaskowski second,
> for each of the eleven.*

Corroboration [SECONDARY]: Polityka (`secondary_polityka_2302461_exit_poll.html`, "wstępne szacunki") lists the same pairs for Bartoszewicz,
Biejat, Braun, Hołownia, Jakubiak, Mentzen and Zandberg. i.pl (`secondary_ipl_c1p2-27645385_exit_poll_przeplywy.html`, 21:57) lists them
for Mentzen, Braun, Hołownia, Zandberg, Biejat, Senyszyn (*"Nawrocki zdobył też uznanie 18,9 proc. wyborców Joanny Senyszyn, a Rafał
Trzaskowski – 81,1 proc."*) and Stanowski (*"51,2 proc. wyborców Krzysztofa Stanowskiego zagłosowało na Karola Nawrockiego, a 48,8 proc. na
Rafała Trzaskowskiego"*). The small electorates (Maciak 0.19 %, Woch 0.09 % of the first-round vote, PKW per `presidential_returns.md`) rest on very few interviews.

Two more exit-poll flows, both [SECONDARY]:

- **The finalists' own first-round voters** (i.pl): *"1 proc. wyborców Trzaskowskiego w drugiej turze zagłosował na Nawrockiego (99 proc. na
  Trzaskowskiego). Z kolei 0,7 proc. wyborców Nawrockiego zagłosowało na Trzaskowskiego (99,3 proc. na Nawrockiego)."*
- **First-round non-voters** (Polityka): *"Trzaskowski skuteczniej zmobilizował tych, którzy w pierwszej turze nie głosowali. 55,5 proc. z
  nich głosowało na kandydata KO, a 44,5 proc. na Karola Nawrockiego."*

## 3. The late poll (TVN24 and Konkret24, commissioning broadcaster)

TVN24 updated its exit-poll articles with late-poll figures. The late poll's error margin is "+/- 0,5 punktu procentowego".

- Mentzen (`tvn24_st8487352`): *"Natomiast według late poll Karola Nawrockiego poparło 87,2 procent wyborców Mentzena, a 12,8 procent jego
  elektoratu zagłosowało na Rafała Trzaskowskiego. W elektoracie kandydata PiS wyborcy Mentzena stanowili 24,7 procent."*
- Hołownia (`tvn24_st8487890`): *"Natomiast według late poll na Trzaskowskiego głosowało 85,4 procent elektoratu Hołowni, a na Nawrockiego
  14,6 procent."*
- Zandberg (`tvn24_st8486898`): *"Natomiast według late poll na Trzaskowskiego zagłosowało 83,5 procent wyborców Zandberga, a na Nawrockiego
  16,5 procent."*
- First-round non-voters (`tvn24_st8491383_nieglosujacy_w_i_turze.html`, 2 Jun 00:18): *"Wśród tej grupy wyborców kandydat Koalicji
  Obywatelskiej otrzymał 51,4 procent głosów, Nawrocki - 48,6 procent."*

Konkret24 (`konkret24_st8492152_przeplywy_late_poll.html`, 2 Jun 2025 18:10):

> Otóż sondaż late poll Ipsos pokazał, że: wyborcy Adriana Zandberga (partia Razem) w 83,5 proc. wybrali Rafała Trzaskowskiego - ale 16,5
> proc. głosowało na Karola Nawrockiego ; wyborcy Magdaleny Biejat (Nowa Lewica) w 88,3 proc. wybrali Rafała Trzaskowskiego - ale 11,7 proc.
> głosowało na Karola Nawrockiego ; wyborcy Szymona Hołowni (Trzecia Droga - Polska 2050) w 85,4 proc. wybrali Rafała Trzaskowskiego - ale
> 14,6 proc. głosowało na Karola Nawrockiego ; wyborcy Sławomira Mentzena (Konfederacja) w 83,5 proc. wybrali Karola Nawrockiego - ale 16,5
> proc. głosowało na Rafała Trzaskowskiego ; wyborcy Grzegorza Brauna (Konfederacja Korony Polskiej) w 92,6 proc. wybrali Karola Nawrockiego -
> ale 7,4 proc. głosowało na Rafała Trzaskowskiego ; wyborcy Marka Jakubiaka (koło Wolni Republikanie) w 89,5 proc. wybrali Karola Nawrockiego
> - ale 10,5 proc. głosowało na Rafała Trzaskowskiego .
>
> *Gloss: the late poll's flows for six electorates. **Its Mentzen pair (83.5/16.5) contradicts TVN24's own Mentzen article (87.2/12.8, late
> poll).** 83.5/16.5 is exactly the Zandberg pair mirrored, so it is probably a transcription slip, but the files cannot settle it. Use
> 87.2/12.8 for Mentzen's late-poll pair only with this caveat.*

Late-poll composition of the run-off electorates (Konkret24 and `tvn24_st8491383`). Nawrocki's run-off voters were 51.1 % his own first-round
voters, 24.7 % Mentzen's, 9.6 % Braun's and 5.9 % first-round non-voters. Trzaskowski's were 61.7 % his own, 8.6 % Zandberg's, 7.2 % Biejat's,
7.2 % Hołownia's and 6.5 % non-voters.

## 4. Summary table (share of each first-round electorate among its run-off voters, % Nawrocki / % Trzaskowski)

| First-round candidate | Exit poll 21:00 | Source | Late poll | Source |
|---|---|---|---|---|
| Mentzen | **88.1 / 11.9** | Polsat, TVN24 | 87.2 / 12.8 (TVN24) or 83.5 / 16.5 (Konkret24) - conflict | TVN24, Konkret24 |
| Braun | **92.5 / 7.5** | Polsat | 92.6 / 7.4 | Konkret24 |
| Hołownia | **13.8 / 86.2** | Polsat, TVN24 | 14.6 / 85.4 | TVN24, Konkret24 |
| Zandberg | **16.2 / 83.8** | Polsat, TVN24 | 16.5 / 83.5 | TVN24, Konkret24 |
| Biejat | **9.8 / 90.2** | Polsat | 11.7 / 88.3 | Konkret24 |
| Jakubiak | 90.3 / 9.7 | PR24, Polityka [SECONDARY] | 89.5 / 10.5 | Konkret24 |
| Bartoszewicz | 67.3 / 32.7 | PR24, Polityka [SECONDARY] | - | not found |
| Senyszyn | 18.9 / 81.1 | PR24, i.pl [SECONDARY] | - | not found |
| Stanowski | 51.2 / 48.8 | PR24, i.pl [SECONDARY] | - | not found |
| Maciak | 72.9 / 27.1 | PR24 [SECONDARY] | - | not found |
| Woch | 65.4 / 34.6 | PR24 [SECONDARY] | - | not found |
| Nawrocki (own R1 voters) | 99.3 / 0.7 | i.pl [SECONDARY] | - | not found |
| Trzaskowski (own R1 voters) | 1.0 / 99.0 | i.pl [SECONDARY] | - | not found |
| Did not vote in round 1 | 44.5 / 55.5 | Polityka [SECONDARY] | 48.6 / 51.4 | TVN24 |
| Share of any electorate that abstained on 1 June | not given | - | not given | - |

## 5. Against the ruling

**The ruling's five pairs match the Ipsos exit poll exactly** (Mentzen 88.1/11.9, Braun 92.5/7.5, Hołownia 13.8/86.2, Zandberg 16.2/83.8,
Biejat 9.8/90.2), as published by Polsat News at 21:10 on 1 June 2025 and, for three of them, by TVN24.

The **late poll moved the centre-left electorates a little toward Nawrocki and Mentzen's toward Trzaskowski**; Braun's barely moved:

| Candidate | Exit poll -> late poll | Change |
|---|---|---|
| Hołownia | Nawrocki 13.8 -> 14.6 | +0.8 toward Nawrocki |
| Zandberg | Nawrocki 16.2 -> 16.5 | +0.3 toward Nawrocki |
| Biejat | Nawrocki 9.8 -> 11.7 | +1.9 toward Nawrocki |
| Mentzen | Nawrocki 88.1 -> 87.2 | -0.9 (or -4.6 per Konkret24) |
| Braun | Nawrocki 92.5 -> 92.6 | +0.1 |

So the late poll put more left-of-centre voters with Nawrocki and more Mentzen voters with Trzaskowski. If the fit should track the count
(Nawrocki 50.89 %, PKW, per `presidential_returns.md`), the late poll is the better-calibrated vintage. Its error margin is ±0.5 points against the exit poll's ±2. Its
Mentzen figure is disputed between two TVN24 pages, and it has no published pairs for five of the minor candidates.
