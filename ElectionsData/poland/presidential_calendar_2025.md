# Poland - the 2025 presidential election's legal calendar (SP-7: the presidential start's date)

Sourced 2026-10-16 for SP-7's design (`docs/specs/POLAND_PRESIDENTIAL_START_PLAN.md`): when a presidential START could open, by the statute and
by the record. The question it serves is Elias's (that plan's ask 4); nothing here is read by the game.

Sources read:

| what | file | SHA-256 |
|---|---|---|
| **The Marshal's order** - Postanowienie Marszałka Sejmu Rzeczypospolitej Polskiej z dnia 15 stycznia 2025 r. w sprawie zarządzenia wyborów Prezydenta Rzeczypospolitej Polskiej, **Dz.U. 2025 poz. 48**, with its annex, the *kalendarz wyborczy* (fetched this pass) | `raw/president_start_2025/eli_DU_2025_48_marszalek_zarzadzenie_wyborow_prezydenta.pdf` | `664ee622617bff61b230bbb6f7fcc9a5e5ee66697ec2df79a03bb40fe7889cbd` |
| its ELI metadata (announced, promulgated and in force 2025-01-15) | `raw/president_start_2025/eli_meta_DU_2025_48.json` | `007cddc2a7e94a30aaf37c74ba8b6f29f3f44359fbe4693c724740a75dd83097` |
| the Electoral Code as first published in 2011 (held since 2026-09-24/10-02) | `raw/electoral_code/eli_DU_2011_112_kodeks_wyborczy.html` | `a6a622377620f3d08adb2508af897f99903915591b2487486add04969efd6544` |
| the Electoral Code's consolidated text, Dz.U. 2026 poz. 1261 (held since 2026-10-02) | `raw/electoral_code/eli_DU_2026_1261_kodeks_wyborczy_tekst_jednolity.pdf` | `6d38ef42e121d1e0209b068828c1545e6363a4515861c4476a2fb29dbebd48b6` |
| the Konstytucja, Kancelaria Sejmu's unified text (held since 2026-10-01) | `raw/president/eli_DU_1997_483_konstytucja_U_D19970483Lj.pdf` | `9758dc31bcbcae253d0ac5499d1734954979d123397d61fc9773e287412d64b2` |

**Which text governed 2025.** The order cites the Code as *"Dz. U. z 2023 r. poz. 2408 oraz z 2024 r. poz. 721, 1572 i 1907"* - the 2023
consolidation as amended in 2024, which is not held. The 2011 text and the 2026 consolidation read the same in art. 104, 105 § 2, 289 § 1, 290
and 291 § 1, quoted below in the 2011 HTML's words (the 2026 PDF's text layer joins *"nie wcześniej"* into one word). **They do not read the same in
art. 303 § 1** (§2 below): the deadline was amended between them, so two texts agreeing is evidence about the unheld 2023 text, not proof of it -
for each date this file relies on, the 2025 order's own calendar is the primary that says which rule governed. The PDF text was read through each
font's own ToUnicode map (`raw/president_start_2025/fetch_log.md`); only line-wrap hyphens are joined. A gloss in italics follows each quotation.

## 1. The order: 15 January 2025, polling day 18 May 2025 (Dz.U. 2025 poz. 48, p. 1)

> Warszawa, dnia 15 stycznia 2025 r. ... POSTANOWIENIE MARSZAŁKA SEJMU RZECZYPOSPOLITEJ POLSKIEJ z dnia 15 stycznia 2025 r. w sprawie zarządzenia
> wyborów Prezydenta Rzeczypospolitej Polskiej
> Na podstawie art. 128 ust. 2 Konstytucji Rzeczypospolitej Polskiej z dnia 2 kwietnia 1997 r. oraz
> art. 289 § 1 i art. 290 ustawy z dnia 5 stycznia 2011 r. – Kodeks wyborczy (Dz. U. z 2023 r. poz. 2408 oraz z 2024 r. poz. 721, 1572 i 1907)
> stanowi się, co następuje:
> § 1. Zarządzam wybory Prezydenta Rzeczypospolitej Polskiej.
> § 2. Datę wyborów wyznaczam na niedzielę dnia 18 maja 2025 r.
> § 3. Dni, w których upływają terminy wykonania czynności wyborczych, określa kalendarz wyborczy, stanowiący załącznik do niniejszego postanowienia.
> § 4. Postanowienie wchodzi w życie z dniem ogłoszenia.
> Marszałek Sejmu: S. Hołownia
>
> *Gloss: the Marshal (Szymon Hołownia) orders the presidential election on 15 January 2025, for Sunday 18 May 2025; the calendar is the annex;
> in force on publication - the same day, by the page's own dateline and the ELI record.*

## 2. The calendar (the annex, pp. 2-3) - the rows a start can be cut on

| the deadline as printed | the act | days before 18 May |
|---|---|---|
| *do dnia 24 marca 2025 r.* | *zawiadamianie Państwowej Komisji Wyborczej o utworzeniu komitetów wyborczych kandydatów na Prezydenta Rzeczypospolitej Polskiej* - the committees notify the PKW (art. 299 § 4: *"najpóźniej w 55 dniu przed dniem wyborów"*, `presidential_nomination.md` §3) | 55 |
| *do dnia 31 marca 2025 r.* | *powołanie okręgowych komisji wyborczych* - the district commissions appointed | 48 |
| *do dnia 4 kwietnia 2025 r. do godz. 16.00* | *zgłaszanie do Państwowej Komisji Wyborczej kandydatów na Prezydenta Rzeczypospolitej Polskiej* - the candidates filed with the 100,000 signatures (art. 303 § 1 **as the 2026 consolidation reads it**, Dz.U. 2026 poz. 1261: *"najpóźniej do godziny 16⁰⁰ w 44 dniu przed dniem wyborów"*; the 2011 text read *"najpóźniej do godziny 24 00 w 45 dniu przed dniem wyborów"* - amended after 2011, the amending act not identified here; the 2025 calendar's 4 April at 16.00, the 44th day, shows the amended rule governed 2025) | 44 |
| *do dnia 28 kwietnia 2025 r.* | *podanie do publicznej wiadomości danych o kandydatach na Prezydenta Rzeczypospolitej Polskiej* - the candidates published | 20 |
| *od dnia 3 maja 2025 r. do dnia 16 maja 2025 r. do godz. 24.00* | *nieodpłatne rozpowszechnianie audycji wyborczych w programach publicznych nadawców radiowych i telewizyjnych przygotowanych przez komitety wyborcze* - the free broadcasts | 15 to 2 |
| *w dniu 16 maja 2025 r. o godz. 24.00* | *zakończenie kampanii wyborczej* - the campaign ends | 2 |
| *w dniu 18 maja 2025 r. godz. 7.00–21.00* | *głosowanie* - the vote | 0 |

The other rows (voters' certificates, polling stations, postal and proxy voting, the abroad and ships' registers) set no point a start could be
cut on and are not transcribed. The annex's footnote restates art. 9 § 2-3: a deadline falling on a Saturday or a public holiday moves to the next
working day. *The day counts are DERIVED (the calendar's date against 18 May); the 55th and 44th days are the Code's own counts, so the calendar
reproduces them exactly.*

## 3. The statute behind the dates (the Electoral Code)

> Art. 104. Kampania wyborcza rozpoczyna się z dniem ogłoszenia aktu właściwego organu o zarządzeniu wyborów i ulega zakończeniu na 24 godziny
> przed dniem głosowania.
>
> *Gloss: **the campaign begins on the day the act ordering the election is published** and ends 24 hours before polling day - for 2025, from
> 15 January to the end of 16 May.*

> Art. 105. § 2. Agitację wyborczą można prowadzić od dnia przyjęcia przez właściwy organ zawiadomienia o utworzeniu komitetu wyborczego na
> zasadach, w formach i w miejscach, określonych przepisami kodeksu.
>
> *Gloss: electioneering may be carried on from the day the committee's notification is accepted - each committee's own day, by 24 March at the
> latest.*

> Art. 289. § 1. Wybory zarządza Marszałek Sejmu nie wcześniej niż na 7 miesięcy i nie później niż na 6 miesięcy przed upływem kadencji urzędującego
> Prezydenta Rzeczypospolitej i wyznacza ich datę na dzień wolny od pracy przypadający nie wcześniej niż na 100 dni i nie później niż na 75 dni przed
> upływem kadencji urzędującego Prezydenta Rzeczypospolitej.
>
> *Gloss: the Marshal orders the election **no earlier than 7 months and no later than 6 months** before the incumbent's term ends, for a non-working
> day 100 to 75 days before it. With Duda's term ending 6 August 2025 (`records_by_date.md` §2d; `PresidencyOfRecord`): the order falls between
> 6 January and 6 February 2025, the vote between 28 April and 23 May 2025 (DERIVED). 15 January and 18 May lie inside both.*

> Art. 290. § 1. Marszałek Sejmu zarządza wybory Prezydenta Rzeczypospolitej w drodze postanowienia. Postanowienie Marszałka Sejmu podaje się do
> publicznej wiadomości i ogłasza w Dzienniku Ustaw Rzeczypospolitej Polskiej najpóźniej w 3 dniu od dnia zarządzenia wyborów. § 2. W postanowieniu,
> o którym mowa w § 1, Marszałek Sejmu, po zasięgnięciu opinii Państwowej Komisji Wyborczej, określa dni, w których upływają terminy wykonania
> czynności wyborczych przewidzianych w kodeksie (kalendarz wyborczy).
>
> *Gloss: the order is a Marshal's decision, published in the Dziennik Ustaw within 3 days; it carries the calendar, set after the PKW's opinion -
> so the PKW's opinion precedes the order. That opinion is NOT HELD and was not fetched: the design leans on art. 104 (the campaign opens on the
> order's publication), not on the order being the first act of the election.*

> Art. 291. § 1. Nowo wybrany Prezydent Rzeczypospolitej składa przysięgę wobec Zgromadzenia Narodowego w ostatnim dniu urzędowania ustępującego
> Prezydenta Rzeczypospolitej.
>
> *Gloss: the newly elected president takes the oath on the outgoing president's last day in office - the oath day Elias ruled with E2 (§773).*

## 4. Two articles of the Konstytucja the design reads (pp. 29 and 31 of the unified text)

> Art. 132. Prezydent Rzeczypospolitej nie może piastować żadnego innego urzędu ani pełnić żadnej funkcji publicznej, z wyjątkiem tych, które są
> związane ze sprawowanym urzędem.
>
> *Gloss: the President may hold no other office and perform no other public function, except those tied to the office.*

> Art. 144. ... 3. Przepis ust. 2 nie dotyczy: ... 4) inicjatywy ustawodawczej, ... 6) podpisywania albo odmowy podpisania ustawy, ...
>
> *Gloss: among the President's acts that need **no countersignature by the prime minister** are the legislative initiative and **signing or
> refusing to sign a statute** - the veto is the President's own act.*

## 5. On the record beside the calendar: when the five backing parties named their candidates

Each from the party's own page, dated by the page itself (its post header and its `datePublished`) and, where the page names the decision's day,
by that day (§621: a document is dated by the decision it records). Two pages were already held (`presidential_returns.md`, *Candidates:
committees, party membership, party backing*); the other three parties' were fetched this pass, after the review of the design found them
missing (`raw/president_start_2025/fetch_log.md`).

| party - candidate | the page | what it says | dated | days before 18 May |
|---|---|---|---|---|
| **Konfederacja - Mentzen** | konfederacja.pl, *To już oficjalnie! Sławomir Mentzen kandydatem na prezydenta*, posted *21 sierpnia, 2024* (`raw/president_start_2025/konfederacja_pl_to-juz-oficjalnie-slawomir-mentzen-kandydatem.html`, SHA-256 `ff8d276479a8cea43f1f5e6ed2c9a67ad6a7bd4589aa63b71c637420a8df4902`) | *„We wtorek 20 sierpnia 2024 r. Rada Liderów Konfederacji podjęła decyzję, że kandydatem Konfederacji na prezydenta będzie Sławomir Mentzen.”* | **20 Aug 2024** (the decision's day the page names) | 271 |
| **Trzecia Droga - Hołownia** | polska2050.pl, press release *Szymon Hołownia będzie kandydował w wyborach prezydenckich w 2025 roku*, posted *śr. 13 lis 2024* (`raw/president_start_2025/polska2050_pl_szymon-holownia-bedzie-kandydowal-2025.html`, SHA-256 `d2f57698362d965de931bd0838fdf5bb3f2ce93032150ed633a954c6a40737d4`; the same day's news post beside it, `…/polska2050_pl_szymon-holownia-kandydatem-w-wyborach-na-prezydenta.html`, `74c1169f2099e51a81c41dab1e533a9462ba6b6525fb6c02679eeb52893b2a63`) | *„13 listopada podczas spotkania otwartego w Jędrzejowie, Szymon Hołownia ogłosił swój start w wyborach prezydenckich w 2025 roku jako kandydat niezależny.”* | **13 Nov 2024** | 186 |
| **KO - Trzaskowski**, still open | platforma.org (the PO site carrying KO's news), *PRAWYBORY: SONDAŻ*, posted *18 listopada 2024* (`raw/president_start_2025/platforma_org_prawybory-sondaz-2024-11-18.html`, SHA-256 `cb4ca3d1f75b2d212f98408684f5fdeb16893454786df9df06a6dc5454227b66`) | *„… kandydatów startujących w prawyborach Koalicji Obywatelskiej – Radosław Sikorskiego i Rafała Trzaskowskiego …”* (sic, *Radosław*) - the primary still has two contenders | **not named by 18 Nov 2024** | 181 |
| **KO - Trzaskowski**, named | platforma.org, *Cała Polska naprzód!*, posted *03 grudnia 2024*, reporting the convention of *„W sobotę (7.12)”* (`raw/president_start_2025/platforma_org_konwencja-koalicji-obywatelskiej-w-gliwicach.html`, SHA-256 `36848e832717446a12d90d036b117338248284e823a4af9b351958ae44a2d6cc`) | *„Jesteśmy tutaj dzisiaj po to, żeby jeszcze raz głośno i wyraźnie powiedzieć wszystkim Polkom, wszystkim Polakom, że kandydatem Koalicji Obywatelskiej w wyborach prezydenckich jest Prezydent Rafał Trzaskowski - mówił Premier Donald Tusk”* - *"jeszcze raz"*: said again, so named before | **named by 7 Dec 2024** (the convention the page records; its post header predates it) | 162 |
| **PiS - Nawrocki** | pis.org.pl, *Rada Polityczna Prawa i Sprawiedliwości*, posted **30.11.2024** (`raw/president/secondary_pis_org_pl_rada-polityczna-prawa-i-sprawiedliwosci.html`, SHA-256 `4e69be5f3904308d056745c05cc88ca49ef339175362056e52a4d08c737ba08f`) | *„Rada Polityczna Prawa i Sprawiedliwości jednogłośnie kandydaturę dr. Karola Nawrockiego na Prezydenta RP.”* (sic, verb missing) | **30 Nov 2024** (the post header; the page names no decision day) | 169 |
| **Lewica - Biejat** | lewica.org.pl, dated 2024-12-15 (`raw/president/secondary_lewica_org_pl_magdalena-biejat-kandydatka-lewicy.html`) | *„Magdalena Biejat kandydatką Lewicy w nadchodzących wyborach prezydenckich – zdecydowała Rada Krajowa Nowej Lewicy poprzez aklamację!”* | **15 Dec 2024** | 154 |

**Stated:** Hołownia announced himself *"kandydat niezależny"* on his party's page; that Trzecia Droga (Polska 2050 with PSL) backed him is the game's
reading (§770: *"Trzecia Droga's candidate (Polska 2050's leader; PSL backed him)"*), not a PSL page held here. KO's exact day of naming is not on a
held page - it lies after 18 Nov and on or before 7 Dec 2024, and nothing below needs it closer.

*What this bears on (the plan's ask 4): a start at the standard run-up (22 September 2024, DERIVED from 18 May 2025) opens after Konfederacja
named Mentzen (20 August) and before the other four backing parties named theirs; a start on the Marshal's order (15 January 2025) opens after
all five had.*
