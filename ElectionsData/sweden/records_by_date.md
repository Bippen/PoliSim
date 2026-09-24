# Sweden — chambers, governments and party leaders of record BY DATE, 1 July 2022 – 24 September 2026 [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; PS-1 sourcing pass 2026-09-24, research agent; `docs/specs/POLITICAL_SYSTEM_SPEC.md`
§9 stage 1 and §11). `[PROVISIONAL]` until a second session re-verifies (R-K9), and — for the government since
2026-09-17 — until the Riksdag's prime-minister vote is on record.

**What this file is for.** The world clock: for any date in the window the three tables name the chamber the game
must seat, the government it must seat and the party leaders it must show, each with the event at its boundary and the
page that records it. The seat tables themselves are not repeated here: the 2022 and 2026 chambers are on disk
(`returns_2022.md`, `2026/returns_2026.md`) and the leaders at the two elections are on disk (`party_leaders_2022.md`,
`2026/party_leaders_2026.md`); this file supplies the DATES and the pages that carry them. The 2026 formation's frame
(convening rule, the prime-minister-vote procedure, the status on 2026-09-23) is `2026/government_2026.md`; this file
references it and adds what the live pages said on the run date, 2026-09-24.

**Fetched, never recalled.** Every quote below was read in a page fetched on **2026-09-24 between 16:13:00 and 16:17:41
UTC** (18:13–18:17 CEST; the two data.riksdagen.se feeds stamp themselves 18:12:51 and 18:13:02 +0200) and saved byte
for byte under `raw/records/`. Each file's SHA-256 is in `raw/records/SHA256SUMS.txt` and in the register below; the
URL, HTTP status, byte count and fetch time of every request are in `raw/records/fetch_log.txt` (38 files, all HTTP 200).
Quotes are Swedish and verbatim, read from the saved bytes (tags stripped, entities decoded, whitespace collapsed).
English glosses are marked *gloss:* and are mine, not the source's. Lines marked **DERIVED** state their arithmetic or
reading. Wikipedia was not used, as a finder or otherwise; the finder was a domain-restricted web search over the
official and party sites, and every page it found was then fetched and read.

Party keys: S, M, SD, V, C, KD, MP, L. Dates are ISO.

---

## 1. THE CHAMBER OF RECORD BY DATE

Rule: a Riksdag sits *"från det att den nyvalda riksdagen har samlats till dess den närmast därefter valda riksdagen
samlas"* and the new one convenes on the fifteenth day after election day (RF 3 kap. 10 §, quoted in
`2026/government_2026.md` Fact 1 from [RF-R] there; not re-fetched). So a chamber's boundary is the convening
(*upprop*), not election day. **DERIVED**: 2022-09-11 + 15 = 2022-09-26; 2026-09-13 + 15 = 2026-09-28 — both dates are
also stated outright by the pages below.

| from | to | chamber | seats (S, M, SD, V, C, KD, MP, L) | the boundary event and its source |
|---|---|---|---|---|
| 2022-07-01 (window start; chamber elected 2018-09-09) | 2022-09-26 11.00 | the Riksdag elected 2018 | **S 100, M 70, SD 62, V 28, C 31, KD 22, MP 16, L 20** = 349 (**DERIVED** sum: 100+70+62+31+28+22+20+16 = 349) | seats: [VAL-18] (below); end: the 2022 upprop [RD-KAL22] [RD-PROT1] |
| 2022-09-26 11.00 | 2026-09-28 11.00 | the Riksdag elected 2022-09-11 | `returns_2022.md`: S 107, M 68, SD 73, V 24, C 24, KD 19, MP 18, L 16 = 349 | start: [RD-KAL22] [RD-PROT1] [RD-PK14]; end: [RD-N24] [RD-KAL26] |
| 2026-09-28 11.00 (**scheduled**; not yet convened on the run date) | — | the Riksdag elected 2026-09-13 | `2026/returns_2026.md`: S 99, M 70, SD 62, V 30, C 25, KD 22, MP 22, L 19 = 349 | start: [RD-N24] [RD-KAL26]; see §2.4 |

Seats are **as elected**. Members who left their party group during a term, and substitutes, are not tracked (GAP G5).

**The 2018 chamber** [VAL-18], Valmyndigheten's summary page "Tidigare valresultat 2002–2018" (page stamp
"Publicerad: 20 april 2026"), heading "Sammanfattning av valet till riksdagen":
> "En sammanfattning av resultatet i valet till riksdagen 2018. Redovisat som partiernas andel av rösterna och fördelning av riksdagens 349 mandat. Valdag den 9 september 2018."

Its table (party / Andel röster / Antal mandat), read row by row: "Arbetarepartiet Socialdemokraterna" 28,26 % 100;
"Moderaterna" 19,84 % 70; "Sverigedemokraterna" 17,53 % 62; "Centerpartiet" 8,61 % 31; "Vänsterpartiet" 8,00 % 28;
"Kristdemokraterna" 6,32 % 22; "Liberalerna (tidigare Folkpartiet)" 5,49 % 20; "Miljöpartiet de gröna" 4,41 % 16;
"Feministiskt initiativ" 0,46 % 0; "Övriga anmälda partier" 1,07 % 0. The 2018 result presentation [VAL-18H]
(historik.val.se, stamped "2018-09-16 06:50:46", "Samtliga 6325 valdistrikt räknade.") carries the same shares with the
vote counts (M 1284698, C 557500, L 355546, KD 409478, S 1830386, V 518454, MP 285899, SD 1135627); its seat column was
not read (GAP G4).

**The 2022 convening**, 2022-09-26 [RD-KAL22] (the Riksdag calendar's event page):
> "Måndag 26 september 2022 klockan 11.00"
>
> "Den nya riksdagen samlas för upprop av riksdagsledamöterna."
>
> "Efter uppropet väljer riksdagen talman och tre vice talmän för valperioden 2022–2026."

The minutes of that sitting, "Protokoll 2022/23:1 Måndagen den 26 september" [RD-PROT1]: "§ 7 Upprop" / "Upprop av
kammarens ledamöter företogs." and under "§ 9 Val av talman": "Kammaren valde med acklamation Andreas Norlén (M) till
talman." The talman's own account [RD-PK14]: "Måndagen den 26 september samlades den nya riksdagen för första gången
och jag omvaldes som talman".

**The 2026 convening**, scheduled 2026-09-28 — see §2.4 (the live state), [RD-N24].

---

## 2. THE GOVERNMENT OF RECORD BY DATE

| from | to | prime minister | cabinet parties | support arrangement | the boundary event | source ids |
|---|---|---|---|---|---|---|
| 2022-07-01 (window start; in office since 2021-11-30) | 2022-09-15 | Magdalena Andersson (S) | **S** alone | — (not sourced here; outside the window's questions) | 2022-09-15: the PM asks to be dismissed and the talman so decides. **DERIVED**: the post-election vote of RF 6 kap. 3 § was therefore not held (its second paragraph, quoted in `2026/government_2026.md` Fact 2) | start: [RD-TRB] [RG-SMA] (both on disk under `2026/raw/government/`); end: [RD-PK14] [RD-N1017] [RD-TRB] |
| 2022-09-15 | 2022-10-18 13.00 | Magdalena Andersson (S), **caretaker** (*övergångsregering*) | S | — | 2022-09-19 sounding mandate to Kristersson; 2022-10-14 the talman's proposal; 2022-10-17 the vote, **176–173**; 2022-10-18 regeringsförklaring 9.30 and skifteskonselj 13.00 | [RD-PK14] [RD-N1017] [RD-PROT9] [RG-HALL] [RG-NYA] |
| 2022-10-18 13.00 | 2026-09-17 | Ulf Kristersson (M) | **M + KD + L** | **SD** as *samarbetsparti utanför regeringen* under the **Tidö agreement** (*Tidöavtalet: Överenskommelse för Sverige*), the four-party agreement the talman reported on **2022-10-14** | 2026-09-17: the PM asks to be dismissed; the talman dismisses the whole cabinet, which stays on as caretaker | [RG-NYA] [RG-RF] [TIDO] [RG-BOK] [RG-FAKTA]; end: `2026/government_2026.md` [RD-N17a] [RD-N17b] [RG-ART] |
| 2026-09-17 | — (open on the run date) | Ulf Kristersson (M), **caretaker** | M + KD + L (post-election pages name ministers of all three parties: [RD-AKT]) | the caretaker cannot call an extra election (RF 3 kap. 11 §, §4 below) | 2026-09-18 sounding mandate to Magdalena Andersson (S), to report to the new Riksdag's talman; no report, no proposal, no vote on record on 2026-09-24 (§2.4) | `2026/government_2026.md` [RD-N18]; live: [RD-N24] [RD-RSS1] [RD-RSS2] [OD-VOT] [RD-KAL26] [RG-START] [RG-PRESS] [RD-AKT] |

### 2.1 Andersson's government and its end (2022)

The 2022 formation in the Riksdag's own words, in the notice of the vote [RD-N1017] (published "Måndag 17 oktober 2022
klockan 12.11"):
> "Söndagen den 11 september var det val till riksdagen. Den 15 september begärde statsminister Magdalena Andersson (S) att få sluta sitt uppdrag som statsminister och talmannen beslutade om detta."
>
> "Fram till dess att en ny regering tillträtt i samband med en skifteskonselj är Magdalena Andersson (S) statsminister i övergångsregeringen som består av företrädare för Socialdemokraterna."

*gloss:* until a new government takes office at a change-of-government council, Andersson (S) is prime minister in
the caretaker government, which consists of representatives of the Social Democrats. The talman's own chronology,
his press-conference opening of 2022-10-14 [RD-PK14] (published "Fredag 14 oktober 2022 klockan 13.14"):
> "Torsdagen den 15 september begärde statsminister Magdalena Andersson sitt entledigande och jag beslutade om detta. Därefter inleddes processen med att bilda Sveriges 53:e regering och utse Sveriges 35:e statsminister sedan det moderna statsministerämbetet inrättades år 1876."
>
> "Jag inväntade det slutliga valresultatet från Valmyndigheten innan jag måndagen den 19 september genomförde en så kallad talmansrunda. Därefter gav jag Moderaternas partiledare Ulf Kristersson uppdraget att sondera förutsättningarna för att bilda en regering som kan tolereras av riksdagen."

The start of Andersson's government is before the window and is on disk: [RD-TRB] (`2026/raw/government/`, quoted in
`2026/government_2026.md`'s register) reads "Den 29 november föreslog talmannen att riksdagen skulle godkänna Magdalena
Andersson (S) som ny statsminister och efter att riksdagen godkänt förslaget tillträdde hon den 30 november." and
lists "Andersson, S, 2021–2022"; [RG-SMA] reads "Magdalena Andersson tillträdde den 30 november 2021 … Hon ledde en
socialdemokratisk regering och avgick den 18 oktober 2022." Neither page was re-fetched (GAP G6 on the cabinet's
composition page).

### 2.2 Kristersson's investiture (2022-10-14 → 2022-10-18)

**The talman's proposal and the agreement, 2022-10-14** [RD-PK14]:
> "Ulf Kristersson har nu redovisat för mig att Sverigedemokraterna, Moderaterna, Kristdemokraterna och Liberalerna har nått en överenskommelse om hur Sverige ska styras under de kommande fyra åren. Överenskommelsen innebär bland annat att dessa partier, med 176 mandat, är redo att rösta för att han ska utses till Sveriges statsminister. Förslaget till statsminister får inte ha 175 mandat emot sig."
>
> "Klockan 13.30 idag kommer jag därför att föreslå riksdagen att Ulf Kristersson ska utses till Sveriges 35:e statsminister. Han har för avsikt att bilda en regering som består av företrädare för Moderaterna, Kristdemokraterna och Liberalerna."
>
> "Förslaget bordläggs samtidigt en första gång. Förslaget bordläggs för andra gången på lördag klockan 11 och riksdagen tar sedan ställning till förslaget på måndag klockan 11."

**The vote, 2022-10-17** [RD-N1017]:
> "Måndagen den 17 oktober godkände riksdagen talmannens förslag att utse Ulf Kristersson (M) till statsminister."
>
> "Ja: 176" / "Nej: 173" / "Avstår: 0" / "Frånvarande: 0"

By party, the same page: S "Nej – 107", SD "Ja – 73", M "Ja – 68", V "Nej – 24", C "Nej – 24", KD "Ja – 19", MP
"Nej – 18", L "Ja – 16" (each with "Avstår – 0 röster, Frånvarande – 0"). The chamber's minutes [RD-PROT9], "Protokoll
2022/23:9 Måndagen den 17 oktober", § 1 "Prövning av förslag till statsminister":
> "Votering:" / "176 för godkännande" / "173 för avslag"
>
> "Talmannen konstaterade att mindre än hälften av riksdagens ledamöter hade röstat mot förslaget. Riksdagen hade således utsett Ulf Kristersson (M) till statsminister."
>
> "För godkännande: 73 SD, 68 M, 19 KD, 16 L" / "För avslag: 107 S, 24 V, 24 C, 18 MP"

**DERIVED**: 73+68+19+16 = 176; 107+24+24+18 = 173; 176+173 = 349.

**Taking office, 2022-10-18** — Regeringskansliet's timetable [RG-HALL] (published 17 October 2022):
> "Måndagen den 17 oktober röstade riksdagen för talman Andreas Norléns förslag att utse Ulf Kristersson (M) till statsminister. Regeringsskiftet sker tisdagen den 18 oktober."
>
> "Tisdagen den 18 oktober klockan 9.30 avger den tillträdande statsministern en regeringsförklaring i kammaren och anmäler vilka övriga statsråd som ska ingå i regeringen."
>
> "Det formella regeringsskiftet sker klockan 13.00 vid en särskild konselj på Kungl. Slottet med H.M. Konungen som ordförande."

The press release of the day [RG-NYA] (published 18 October 2022, meta `published` 2022-10-18 10:36:58):
> "I dag presenterade tillträdande statsminister Ulf Kristersson regeringsförklaringen i riksdagen och anmälde i samband med det de statsråd som ingår i den kommande regeringen. Regeringsskiftet äger rum vid en konselj på slottet under ledning av H.M. Konungen. Konseljen inleds klockan 13.00."
>
> "Sveriges nya regering består av statsministern och 23 statsråd."

### 2.3 The cabinet's parties and the support arrangement (2022-10-18 → 2026-09-17)

**The government declaration, 2022-10-18** [RG-RF] (page "Regeringsförklaringen den 18 oktober 2022", "Statsminister
Ulf Kristersson, riksdagen, den 18 oktober 2022"):
> "Moderaterna, Kristdemokraterna och Liberalerna är överens om att tillsammans med Sverigedemokraterna ta ansvar för Sverige. Moderaterna, Kristdemokraterna och Liberalerna kommer att ingå i regeringen. Sverigedemokraterna samarbetar med regeringen i riksdagen, med politiska tjänstemän på Regeringskansliet."

**The Tidö agreement itself** [TIDO] — the PDF published by one signatory, Liberalerna ("Tidöavtalet: Överenskommelse
för Sverige"; the file's PDF metadata: CreationDate `D:20221014060449+02'00'` and `D:20221014082011+02'00'`, a page
LastModified `D:20221014081942+02'00'` — i.e. built on 2022-10-14; the text extracted carries no date line of its
own, GAP G3). Page 2, heading "Samarbete":
> "Samarbetspartierna Sverigedemokraterna, Moderaterna, Kristdemokraterna och Liberalerna är överens om att ta ansvar för Sverige i ett gemensamt samarbete under mandatperioden 2022–2026. Samarbetspartier i regeringen är Moderaterna, Kristdemokraterna och Liberalerna. Samarbetsparti utanför regeringen är Sverigedemokraterna."

*gloss:* the cooperation parties SD, M, KD and L agree to take responsibility for Sweden in a joint cooperation for the
2022–2026 term; the cooperation parties in government are M, KD and L; the cooperation party outside government is SD.
The same page: "Samarbetspartierna förbinder sig att rösta på regeringens budget så att budgeten i sin helhet röstas
igenom i riksdagen." *gloss:* the cooperation parties commit to voting for the government's budget. The agreement's
own word for SD is *samarbetsparti* (cooperation party) with *"fullt och lika inflytande"*; "support" is the spec's
word, not the agreement's.

**The date, as the government itself states it** [RG-BOK] (regeringen.se, "Genomfört – bokslut över Tidöavtalet",
meta `published` 2026-05-05):
> "Den 18 oktober 2022 bildade statsminister Ulf Kristersson en regering bestående av Moderaterna, Kristdemokraterna och Liberalerna. Regeringspartierna och samarbetspartiet Sverigedemokraterna hade dessförinnan enats om ett reformprogram för mandatperioden, Tidöavtalet: Överenskommelse för Sverige."

The agreement's **date of 2022-10-14** rests on the talman's statement of that day that the four parties "har nått en
överenskommelse" [RD-PK14] and on the PDF's creation stamps; no fetched page says "signed on 14 October" in terms
(GAP G3). (`coalition_declarations_2022.md` gives the same date from a Wikipedia-based register; this file's is the
primary-source reading.)

**The cabinet's composition as dated by Regeringskansliet's fact sheet** [RG-FAKTA] (PDF "Sveriges regering", its own
line "Senast uppdaterad: September 2025"; PDF CreationDate `D:20250910114055+02'00'`):
> "Sverige styrs av en regering bestående av Moderaterna (M), Kristdemokraterna (KD) och Liberalerna (L). Den 18 oktober 2022 tillträdde en regering med Ulf Kristersson (M) som statsminister."

It lists every minister with a party letter: "Ulf Kristersson (M)" Statsminister; Carl-Oskar Bohlin (M); Jakob
Forssmed (KD); Elisabet Lann (KD); "Ebba Busch (KD)" Energi- och näringsminister och vice statsminister; Benjamin
Dousa (M); Andreas Carlson (KD); Anna Tenje (M); Camilla Waltersson Grönvall (M); Elisabeth Svantesson (M); Erik
Slottner (KD); Gunnar Strömmer (M); Johan Britz (L); Simona Mohamsson (L); Jessica Rosencrantz (M); Niklas Wykman (M);
Romina Pourmokhtari (L); Maria Malmer Stenergard (M); Parisa Liljestrand (M); Lotta Edholm (L); Johan Forssell (M);
Peter Kullgren (KD); Pål Jonson (M); Nina Larsson (L). (Read through a crude text extraction of the PDF's content
streams; the party letters are the fact sheet's own.) Reshuffles between October 2022 and September 2025 are not
tracked (GAP G7). The live page [RG-SR] ("Sveriges regering", as served on the run date) lists the same names by
department with the line "Sveriges regering består av en statsminister och 23 statsråd." but gives no party letters.

### 2.4 THE LIVE STATE ON THE RUN DATE, 2026-09-24 (fetched 16:13–16:17 UTC)

**Has the new Riksdag convened? No — it convenes on Monday 2026-09-28.** The Riksdag published, on the run date
itself, a notice [RD-N24] ("Upprop, talmansval och riksmötets öppnande", "Publicerad : Torsdag 24 september 2026
klockan 15.03"):
> "Nästa vecka samlas den nya riksdagen och inleder arbetsåret. Måndagen den 28 september är det upprop för ledamöterna. Därefter är det talmansval och anmälan av gruppledare. Samma dag inleds även den allmänna motionstiden. Dagen efter, tisdagen den 29 september, äger riksmötets öppnande rum."
>
> "Uppropet till riksdagen äger rum dagen före riksmötets öppnande, den 28 september klockan 11.00. Därefter väljer riksdagen talman och vice talmän samt anmäler vilka som blir partiernas gruppledare. Riksdagen utser även en valberedning med uppdrag att föreslå ledamöter till riksdagens utskott."
>
> "Riksmötets öppnande äger rum den 29 september 2026. Det är starten på riksdagens och de 349 ledamöternas nya arbetsår, riksmötet 2026/27."

The calendar for 24 Sep–31 Oct as served [RD-KAL26] lists under "Vecka 40" / "Måndag 28 september": "Upprop" ("Denna
händelse kommer att direktsändas måndag 28 september 11.00") and "Val av talman och vice talmän"; and events on
"Tisdag 29 september" at 14.00. Its filter counts read "Upprop (1)" and "Val (1)".

**Has a talman's proposal been made, or a prime-minister vote held or scheduled? Nothing on record.**
- The vote register for riksmöte 2026/27 [OD-VOT] returns, in full: `villkor=": rm=2026/27 " antal="0"`.
- The calendar [RD-KAL26] lists no prime-minister vote and no regeringsförklaring through 31 October (the same weak
  evidence as in `2026/government_2026.md`: that calendar also omits chamber decisions that other pages date).
- The Riksdag's two news feeds [RD-RSS1] [RD-RSS2] (built 18:13:02 and 18:12:51 +0200 on the run date): the only item
  after 21 September is the 24 September notice above (pubDate "Thu, 24 Sep 2026 15:13:27 +0200"); the newest
  formation item is still "Talmannen ger sonderingsuppdrag till Magdalena Andersson (S)" ("Fri, 18 Sep 2026 15:41:16
  +0200"). The "Aktuellt" listing [RD-AKT] as served shows the same sequence (24, 22, 22, 21, 19, 18 September).
- regeringen.se's front page [RG-START] still carries, under "Aktuellt från regeringen och Regeringskansliet", the
  17 September item: "Statsministern begärde den 17 september sitt entledigande, och därför har talmannen entledigat
  statsministern och övriga statsråd. De upprätthåller dock sina befattningar till dess en ny regering har tillträtt.
  Regeringen är därmed en övergångsregering." Its "Veckans regeringssammanträde" block reads "Regeringsärenden vecka
  39, 2026".
- The press-release listing [RG-PRESS] as served: the items of 17–24 September 2026 are "Ny kabinettssekreterare på
  Utrikesdepartementet", "Prisbasbelopp för 2027 fastställt", "Pål Jonson deltar i EU:s försvarsministermöte",
  "Strategisk teknik och företagskoncentrationer i fokus när konkurrenskraftsrådet möts i Bryssel" (all 24 Sep),
  "Nya ambassadörer till Sverige", "EU-minister Jessica Rosencrantz deltar i ministerrådsmöte i Bryssel" (22 Sep),
  "Regeringen deltar vid öppnandet av FN:s generalförsamling i New York" (21 Sep), "Statsbesök från Singapore"
  (18 Sep), "Regeringen tillstyrker ansökan om EU-stöd för 14 infrastrukturprojekt" and "Utredningen om tillbörlig
  aktsamhet …" (17 Sep). **None concerns the formation.** **DERIVED**: the caretaker cabinet was issuing routine press
  releases on the run date.

**The sounding mandate.** No fetched page after 18 September reports on Magdalena Andersson's mandate. Under
[RD-N18] (quoted in `2026/government_2026.md` Fact 4) she reports "till den nya riksdagens talman", who is elected on
28 September; the mandate's outcome is therefore not expected on any page before that date. **DERIVED**: as of the run
date the government of record is the caretaker Kristersson cabinet, and the formation stands where it stood on
2026-09-23 — nothing has moved except the Riksdag's own notice of next week's roll-call.

**The caretaker cabinet's parties on a post-election page.** [RD-AKT] carries two EU-nämnden notices naming ministers
with party letters: "Fredagen den 25 september 2026 klockan 9 sammanträder regeringen med EU-nämnden … Från regeringen
deltar försvarsminister Pål Jonson (M), migrationsminister Johan Forssell (M), jämställdhetsminister Nina Larsson (L)
och statssekreterare Daniel Liljeberg." and "Fredagen den 18 september 2026 klockan 9 … Från regeringen deltar energi-
och näringsminister Ebba Busch (KD) och statssekreterare Cristian Danielsson." **DERIVED**: ministers of M, L and KD
were serving in the caretaker cabinet in the week of the run date, consistent with [RG-FAKTA]; this narrows
`2026/government_2026.md`'s G5 without closing it (no post-election page lists the whole cabinet with parties).

---

## 3. PARTY LEADERS BY DATE

The leaders at the two elections are on disk and are not repeated: `party_leaders_2022.md` (vintage 2022-09-11) and
`2026/party_leaders_2026.md` (vintage 2026-09-13, read twice). S, M, SD, V and KD did not change leader between the two
vintages (`2026/party_leaders_2026.md`, its "What changed" list). C, L and MP did. The dated changes inside the window,
each from the party's own site:

| party | from | to | leader(s) | the boundary event and its source |
|---|---|---|---|---|
| **L** | (before the window; *tillförordnad* since 2022-04-08) | 2022-11-26 | Johan Pehrson, acting (*tillförordnad partiledare*) | [L-PA]: "Johan Pehrson tillträdde som tillförordnad partiledare den 8 april 2022 och valdes sedan den 26 november 2022 till partiledare på ett extra landsmöte." |
| **L** | 2022-11-26 | 2025-06-24 | Johan Pehrson, elected | [L-PV] (page dated "Lördag 26 november 2022"): "Liberalernas extra landsmöte har idag enhälligt valt Johan Pehrson som ny partiledare för partiet." Departure announced 2025-04-28 [L-PA]: "Idag har Johan Pehrson informerat valberedningen om att han beslutat att lämna rollen som partiordförande." and "Fram till dess kommer Johan Pehrson att fortsätta i rollen som partiledare och statsråd." |
| **L** | 2025-06-24 | — | Simona Mohamsson | [L-SM] (page dated "Tisdag 24 juni 2025"): "Idag har ett enhälligt landsmöte valt Simona Mohamsson till ny partiordförande för Liberalerna." / "183 ombud från hela landet samlades under tisdagen för ett extrainsatt landsmöte för att välja ny partiordförande." [L-EL]: "Den 24 juni genomförs ett extra landsmöte för att välja ny partiledare." Re-elected at the ordinary landsmöte 2026-03-22 ([L-P2], on disk under `2026/raw/declarations/`). |
| **C** | (before the window) | 2023-02-02 | Annie Lööf | her departure was announced in September 2022 — [C-D1]: "den extrainsatta partistämman som aviserades strax efter Annie Lööfs avgångsbesked i september" — the date of that announcement is on no fetched page (GAP G1); she is named as the predecessor at the 2023 election: "efter Annie Lööf" [C-D1] |
| **C** | 2023-02-02 | 2025-05-03 | Muharrem Demirok | [C-D1] (page dated 2023-02-02): "På en extrainsatt partistämma i Helsingborg valdes Muharrem Demirok under torsdagen till ny partiledare för Centerpartiet, efter Annie Lööf." Departure announced 2025-02-24 [C-D2]: "Muharrem Demirok kommer att fortsätta som partiledare till dess att hans efterträdare har valts vid en extrastämma." |
| **C** | 2025-05-03 | 2025-11-13 | Anna-Karin Hatt | [C-H1] (page dated 2025-05-03): "Anna-Karin Hatt är Centerpartiets nya partiledare. Det står klart efter att hon enhälligt valdes på partiets extrastämma på lördagen." / "Anna-Karin Hatt är nu Centerpartiets 14:e partiledare, när hon efterträder Muharrem Demirok." Departure announced 2025-10-15 [C-H2]: "Anna-Karin Hatt valdes enhälligt till partiordförande på Centerpartiets extrastämma, den 3 maj 2025." / "Anna-Karin Hatt kommer att fortsätta som partiledare fram till partistämman i Karlstad i november där hennes efterträdare väljs." |
| **C** | 2025-11-13 | — | Elisabeth Thand Ringqvist | [C-T1] (page dated 2025-11-13): "Under torsdagseftermiddagen valdes Elisabeth Thand Ringqvist till ny partiledare för Centerpartiet vid partistämman i Karlstad och möttes med stående ovationer." / "Anna-Karin Hatt, tidigare partiordförande, lämnade över en symboliskt stafettpinne och en stor bukett blommor till den nyvalda partiledaren." |
| **MP** | (before the window) | 2023-11-18 | Märta Stenevi **and** Per Bolund (*språkrör*, two) | `party_leaders_2022.md`; Bolund's departure: [MP-H] "ersätter avgående Per Bolund" |
| **MP** | 2023-11-18 | 2024-02-09 (**announcement**; the end of her office is not dated by a fetched page, GAP G2) | Märta Stenevi **and** Daniel Helldén | [MP-H] (published "2023-11-18 16:26:25"): "Partiet har fattat ett beslut och Daniel Helldén blir Miljöpartiets nya manliga språkrör! Han fick 131 av kongressombudens röster i dagens språkrörsval och ersätter avgående Per Bolund. Helldén kommer att leda partiet tillsammans med Märta Stenevi som fick förnyat förtroende i språkrörsuppdraget." The kongress: [MP-RVB] "Partistyrelsen väljs på partiets kongress den 15–17 november i Örebro." (the språkrör vote fell on the 18th per [MP-H]'s date and "dagens språkrörsval"). Stenevi's departure: [MP-K] (a local branch page, published 2024-02-10): "Igår meddelade Märta Stenevi att hon avgår från posten som språkrör för Miljöpartiet." **DERIVED**: announced 2024-02-09. |
| **MP** | 2024-04-28 | — | Amanda Lind **and** Daniel Helldén | [MP-L] (published "2024-04-28 13:45:23"): "Miljöpartiets extra kongress har valt Amanda Lind till nytt språkrör. Tillsammans med Daniel Helldén kommer hon nu att leda partiet." / "Miljöpartiets extrakongress genomfördes digitalt söndagen den 28 april." / "… tillsammans med Daniel Helldén, språkrör sedan 2023." Whether the seat was vacant or held by Stenevi between 2024-02-09 and 2024-04-28 is not stated by any fetched page (GAP G2). |

What is SOURCED here is the NAME, the OFFICE WORD the page uses and the DATE. Nothing else, as in the two leaders files.
The office words vary by page (*partiledare* / *partiordförande* / *språkrör*) and are quoted as each page has them.

---

## 4. THE CONSTITUTIONAL RULES THE SPEC'S §5.4 NAMES

All from [RF-R], Regeringsformen as consolidated on riksdagen.se ("Utfärdad : 1974-02-28", "Ändrad : t.o.m. SFS
2022:1600"), re-fetched on the run date (its SHA-256 differs from the 2026-09-23 copy under `2026/raw/government/`
because the page's chrome is served fresh; the statute text quoted is identical in both — checked for the paragraphs
below by grep on both files).

**Investiture and the caretaker: RF 6 kap. 3, 4, 5, 11 §§** — already quoted verbatim in `2026/government_2026.md`
Fact 2; referenced, not re-quoted.

**The no-confidence vote: RF 13 kap. 4 §** (heading "Misstroendeförklaring"):
> "4 § Riksdagen kan förklara att ett statsråd inte har riksdagens förtroende. Ett yrkande om en sådan misstroendeförklaring ska väckas av minst en tiondel av riksdagens ledamöter för att tas upp till prövning. För en misstroendeförklaring krävs att mer än hälften av riksdagens ledamöter röstar för den."
>
> "Ett yrkande om misstroendeförklaring tas inte upp till prövning om det väcks under tiden från det att ordinarie val har ägt rum eller beslut om extra val har meddelats till dess den genom valet utsedda riksdagen samlas. Ett yrkande avseende ett statsråd som efter att ha entledigats uppehåller sin befattning enligt 6 kap. 11 § får inte i något fall tas upp till prövning."
>
> "Ett yrkande om misstroendeförklaring ska inte beredas i utskott. Lag (2010:1408) ."

*gloss:* a motion needs one tenth of the members to be taken up and more than half of all members (**DERIVED**: at
least 175 of 349) to pass; none can be taken up between an election and the new Riksdag's convening, and never against
a caretaker minister. What follows a successful motion against the prime minister is in 6 kap. (the government's
dismissal, with the extra-election alternative) — the consequence paragraphs of 6 kap. 7 § were not quoted here; the
spec's rule set should read them from [RF-R] (GAP G12).

**The extra election: RF 3 kap. 11 §** (under "Extra val"):
> "11 § Regeringen får besluta om extra val till riksdagen mellan ordinarie val. Extra val ska hållas inom tre månader efter beslutet."
>
> "Efter val till riksdagen får regeringen inte besluta om extra val förrän tre månader har gått från den nyvalda riksdagens första sammanträde. Regeringen får inte heller besluta om extra val under den tid då dess ledamöter, efter det att samtliga har entledigats, uppehåller sina befattningar till dess en ny regering ska tillträda."
>
> "Bestämmelser om extra val i visst fall finns i 6 kap. 5 §. Lag (2010:1408)."

*gloss:* the government may call an extra election, to be held within three months; not within three months of a new
Riksdag's first sitting, and not while it is a caretaker. The "visst fall" of 6 kap. 5 § is the fourth rejected
talman's proposal (quoted in `2026/government_2026.md`). **DERIVED** for the live window: the caretaker Kristersson
cabinet cannot call an extra election, and no no-confidence motion can be taken up before 2026-09-28.

---

## 5. Against the spec's §4 line

`docs/specs/POLITICAL_SYSTEM_SPEC.md` §4 gives Sweden's government of record at its start (the run-up in January 2026)
as "Kristersson's M+KD+L with SD's support". **Nothing fetched contradicts it**: M + KD + L in cabinet [RG-RF]
[RG-FAKTA], SD outside it as *samarbetsparti* under the Tidö agreement [TIDO], from 2022-10-18 [RG-NYA] [RG-BOK]. Two
refinements the spec should carry as data, not as a correction: (i) the agreement's own word is *samarbetsparti* with
"fullt och lika inflytande", and it binds SD to the budget — a stronger arrangement than a bare "support"; (ii) from
2026-09-17 the same cabinet is a **caretaker** government (dismissed at the PM's request), so a game start on or after
that date seats the caretaker, not a confidence-holding government, and the formation is open. At the January 2026
start the leaders on record are those of `2026/party_leaders_2026.md` (C's Thand Ringqvist since 2025-11-13; L's
Mohamsson since 2025-06-24; MP's Lind and Helldén), not 2022's.

---

## Source register
(all fetched 2026-09-24, 16:13:00–16:17:41 UTC; saved as `raw/records/<file>`; "page's own date" is the date the page
itself shows, or its metadata where the page shows none, as marked; bytes as saved)

| id | URL | publisher | page's own date | basis | file | bytes | SHA-256 |
|---|---|---|---|---|---|---|---|
| [RD-N24] | https://www.riksdagen.se/sv/aktuellt/aktuelltnotiser/2026/sep/24/upprop-talmansval-och-riksmotets-oppnande_cms0c266a8f-9503-4372-b2a8-c53c90a25f3bsv/ | Sveriges riksdag | "Publicerad : Torsdag 24 september 2026 klockan 15.03" | official Riksdag news notice | `riksdagen_20260924_upprop-talmansval-och-riksmotets-oppnande.html` | 228772 | `0c23681577f450eef85f986d0173cdb8cfc762932255e6c697f1cbf3af22c22e` |
| [RD-RSS1] | https://data.riksdagen.se/dokumentlista/?cmskategori=valet2026&avd=aktuellt&aktuelltnotistomdatum=1&lang=sv&utformat=rss&sort=datum&sortorder=desc | Sveriges riksdag ("Valet 2026" feed) | lastBuildDate "Thu, 24 Sep 2026 18:13:02 +0200" | news feed as served | `riksdagen_rss_valet2026.xml` | 6618 | `bd99b220a98d82a1d365aa1a2c7926582cc1c3f049b198517448a495fc2bd727` |
| [RD-RSS2] | https://data.riksdagen.se/dokumentlista/?cmskategori=startsida&avd=aktuellt&aktuelltnotistomdatum=1&lang=sv&utformat=rss&sort=datum&sortorder=desc | Sveriges riksdag (front-page feed) | lastBuildDate "Thu, 24 Sep 2026 18:12:51 +0200" | news feed as served | `riksdagen_rss_startsida-aktuellt.xml` | 15464 | `5344f08ef254f2075443d7681ae2f5945b54f3d9a0c39fa126e21e86af03577d` |
| [RD-VAL26] | https://www.riksdagen.se/sv/aktuellt/valet-2026/ | Sveriges riksdag | no page date; embedded items to 24 Sep 2026 | explainer/listing page as served | `riksdagen_aktuellt_valet2026_listing.html` | 323551 | `55820938b2b73020eedcbd39e2385c25b515e0583bd45ee8d741c61a1b89f199` |
| [RD-AKT] | https://www.riksdagen.se/sv/aktuellt/?cmskategori=regeringsbildning | Sveriges riksdag | live listing; newest item "Torsdag 24 september" | the "Aktuellt" listing as served (the category filter did not narrow it: general items are listed) | `riksdagen_aktuellt_regeringsbildning_listing.html` | 317991 | `0209b8e11849e316472ae3f978dca6540137162e48451829f72d3bc9a8785dba` |
| [RD-KAL26] | https://www.riksdagen.se/sv/aktuellt/kalendersida/?from=2026-09-24&tom=2026-10-31 | Sveriges riksdag | live calendar 24 Sep–31 Oct 2026 as served | live calendar | `riksdagen_kalender_2026-09-24_2026-10-31.html` | 598711 | `ed07b91033c774811ab1c8ba9e29f2d8c772cd13d07befd25eb09c7ab65a3414` |
| [OD-VOT] | https://data.riksdagen.se/voteringlista/?rm=2026%2F27&bet=&punkt=&valkrets=&rost=&iid=&sz=500&utformat=xml&gruppering= | Sveriges riksdag, open data | as served: `villkor=": rm=2026/27 " antal="0"` | API response | `datariksdagen_voteringlista_rm2026-27.xml` | 159 | `d64572de9fd871fe20b7f1fe479f48e9d8ad22c645cf0d425124de6491630e43` |
| [RG-START] | https://www.regeringen.se/ | Regeringskansliet | live; "Regeringsärenden vecka 39, 2026"; items dated to 24 September 2026 | front page as served | `regeringen_startsida.html` | 282976 | `9147b8203b5a4f6582dbb4bcdb69ad3f7bad25384e2efecd540f60dfb612e906` |
| [RG-PRESS] | https://www.regeringen.se/pressmeddelanden/ | Regeringskansliet | live; newest items 24 September 2026 | press-release listing as served | `regeringen_pressmeddelanden_listing.html` | 252165 | `7092ad1e5d367c72872447d25be2013b5813f299e5c5668c0fef9072d6508881` |
| [RG-SR] | https://www.regeringen.se/sveriges-regering/ | Regeringskansliet | meta `published` 2014-09-23 (a standing page, served live) | the ministers page as served | `regeringen_sveriges-regering.html` | 237006 | `e04887ee59f78debb72ad8311875316c33cc43a22dc8c93ab64344067e9e63e4` |
| [RG-FAKTA] | https://www.regeringen.se/contentassets/b7a372535d4d4115b2c9b9ef2c256c6e/faktablad-regeringen_2025_september.pdf | Regeringskansliet | "Senast uppdaterad: September 2025"; PDF CreationDate 2025-09-10 | fact sheet, the cabinet with party letters | `regeringen_faktablad-regeringen_2025_september.pdf` | 1234716 | `c6bcc128de617ada699f2e4438476c82641e0f44b332fefe4d8166ae926f002c` |
| [RD-N1017] | https://www.riksdagen.se/sv/aktuellt/aktuelltnotiser/2022/okt/17/ulf-kristersson-godkandes-som-statsminister_cms40110c18-74aa-4e0b-a28e-19df166633dbsv/ | Sveriges riksdag | "Publicerad : Måndag 17 oktober 2022 klockan 12.11" | official Riksdag news notice, the vote by party | `riksdagen_20221017_ulf-kristersson-godkandes-som-statsminister.html` | 222819 | `4d8d718ae17f378a2495a7f6d6805f070f1bbe702c19fbb403f4fd57687926f4` |
| [RD-PROT9] | https://www.riksdagen.se/sv/dokument-och-lagar/dokument/protokoll/protokoll-2022239-mandagen-den-17-oktober_ha099/html/ | Sveriges riksdag | "Protokoll 2022/23:9 Måndagen den 17 oktober" | chamber minutes | `riksdagen_protokoll_2022-23-9_20221017.html` | 325532 | `fbb9f2fc3a27865fc004242ddffdb18b6d59a64c1d0499481f0c8461bc9644f8` |
| [RD-PROT1] | https://www.riksdagen.se/sv/dokument-och-lagar/dokument/protokoll/protokoll-2022231-mandagen-den-26-september_ha091/html/ | Sveriges riksdag | "Protokoll 2022/23:1 Måndagen den 26 september" | chamber minutes, the 2022 upprop and talman election | `riksdagen_protokoll_2022-23-1_20220926.html` | 530703 | `e3f7ab64acd2345922ca975db7aa8cef21d32154919219f509c0e81a72c2cf16` |
| [RD-KAL22] | https://www.riksdagen.se/sv/aktuellt/kalendersida/kalenderhandelse/upprop/2022/sep/26/upprop_rdhac120220926up/ | Sveriges riksdag | "Måndag 26 september 2022 klockan 11.00" | calendar event page | `riksdagen_kalender_upprop_20220926.html` | 213172 | `26aef2bb595fad1bf5306a6ff66f2d07f6976160e099356c907a09b1057979a4` |
| [RD-PK14] | https://www.riksdagen.se/sv/aktuellt/aktuelltnotiser/2022/okt/14/talmannens-inledning-vid-presstraff-den-14-oktober_cms2f4c0336-8ec4-4ba2-8815-3cbcae3f2bbdsv/ | Sveriges riksdag | "Publicerad : Fredag 14 oktober 2022 klockan 13.14" | the talman's press-conference opening, the 2022 chronology | `riksdagen_20221014_talmannens-inledning-vid-presstraff.html` | 222271 | `e84e326e52c2cb544116f146150f5cd74d51413201d4808ee728445ccb883e43` |
| [RF-R] | https://www.riksdagen.se/sv/dokument-och-lagar/dokument/svensk-forfattningssamling/kungorelse-1974152-om-beslutad-ny-regeringsform_sfs-1974-152/ | Sveriges riksdag (text: Regeringskansliet) | "Utfärdad : 1974-02-28", "Ändrad : t.o.m. SFS 2022:1600" | consolidated statute | `riksdagen_sfs-1974-152_regeringsformen.html` | 571814 | `5c199d0d0c10190e53f66ebfb3d8e1d0097e91de71e2f7800449a14a533ca66e` |
| [VAL-18] | https://val.se/valresultat/riksdag-region-och-kommun/2018/valresultat.html | Valmyndigheten | "Publicerad: 20 april 2026" | official summary of the 2018 Riksdag result, seats per party | `val_tidigare-valresultat-2018.html` | 236785 | `98bf364edf46229707f6e1813c57e714912833fb86e98610d56882782e662833` |
| [VAL-18H] | https://historik.val.se/val/val2018/slutresultat/R/rike/index.html | Valmyndigheten (historik) | "2018-09-16 06:50:46"; "Samtliga 6325 valdistrikt räknade." | the 2018 result presentation, votes per party | `historik_val_2018_slutresultat_R_rike.html` | 87722 | `3fdc30494a1c3c0889e3fa23f32969354adb92a9d34333257ea5d3c1db45badc` |
| [RG-NYA] | https://www.regeringen.se/pressmeddelanden/2022/10/sveriges-nya-regering/ | Regeringskansliet (Statsrådsberedningen) | "Publicerad 18 oktober 2022" (meta 2022-10-18 10:36:58) | press release | `regeringen_20221018_sveriges-nya-regering.html` | 198608 | `00a58712404f388ebc34b7706fa72fdf20522c1e283fda6fed5211e0bb063022` |
| [RG-RF] | https://www.regeringen.se/tal/2022/10/regeringsforklaringen-den-18-oktober-2022/ | Regeringskansliet | "Publicerad 18 oktober 2022" | the government declaration | `regeringen_20221018_regeringsforklaringen.html` | 246576 | `acb1efcf3d3a0654c4a727bc2aded8bed6ed0f555c3bec324ccdb1f71a974e06` |
| [RG-HALL] | https://www.regeringen.se/pressmeddelanden/2022/10/halltider-for-regeringsskiftet/ | Regeringskansliet | "Publicerad 17 oktober 2022" | press release, the timetable of 18 Oct | `regeringen_202210_halltider-for-regeringsskiftet.html` | 199991 | `e15ef6045f40bc7ce185517182597ec702049e1558cfa6d80170d7756369309c` |
| [RG-TRE] | https://www.regeringen.se/regeringens-politik/regeringens-prioriteringar/tre-ar-med-regeringen/ | Regeringskansliet | meta `published` 2025-10-17 | explainer ("Regeringen har sedan tillträdet den 18 oktober 2022 …") | `regeringen_tre-ar-med-regeringen.html` | 287420 | `eb83ca36570320f7b900b41527ca4ee9868013083f24a14c0b74b4595d84caf2` |
| [RG-BOK] | https://www.regeringen.se/regeringens-politik/regeringens-prioriteringar/genomfort-bokslut-over-tidoavtalet/ | Regeringskansliet | meta `published` 2026-05-05 | explainer, the Tidö agreement's status | `regeringen_tidoavtalet_bokslut.html` | 207232 | `79aa84d902428424ee623c7829a006c40ce04aee764b70adb4298f39d6f0c8a0` |
| [TIDO] | https://www.liberalerna.se/wp-content/uploads/tidoavtalet-overenskommelse-for-sverige-slutlig.pdf | Liberalerna (a signatory's copy) | PDF CreationDate 2022-10-14 (06:04 and 08:20 +02:00); page LastModified 2022-10-14 08:19:42 | the agreement's text | `liberalerna_tidoavtalet-overenskommelse-for-sverige-slutlig.pdf` | 473575 | `5a08f97d6d36d60527cde23c8e8ddb5fc2c1d2c77dd42786c7eaede9abb14ae2` |
| [C-D1] | https://www.centerpartiet.se/nyheter/2023/2023-02-02-muharrem-demirok-vald-till-ny-partiledare-i-helsingborg | Centerpartiet | "2023-02-02" | party news | `centerpartiet_20230202_demirok-vald-till-ny-partiledare.html` | 70472 | `3b72776ab64d59abbb9749eb466b0793d53c12ff5d5bddb93ff486d23579cc71` |
| [C-D2] | https://www.centerpartiet.se/press/nyheter/nyhetsarkiv-2024/2025-02-24-muharrem-demirok-lamnar-som-partiledare | Centerpartiet | "2025-02-24" | party news | `centerpartiet_20250224_demirok-lamnar-som-partiledare.html` | 67226 | `42e1aa7cbd0dd0db08acfbefa03595f9a0539d753f85673001180a5787effcc5` |
| [C-H1] | https://www.centerpartiet.se/nyheter/2025/2025-05-03-anna-karin-hatt-vald-till-ny-partiledare | Centerpartiet | "2025-05-03" | party news | `centerpartiet_20250503_hatt-vald-till-ny-partiledare.html` | 70711 | `f3aa77e168363a3b7b8eb5d77a6f8aa5b9f6b2134e937922ab6143205f4bfbda` |
| [C-H2] | https://www.centerpartiet.se/press/nyheter/nyhetsarkiv-2025/2025-10-15-anna-karin-hatt-lamnar-som-partiledare | Centerpartiet | "2025-10-15" | party news | `centerpartiet_20251015_hatt-lamnar-som-partiledare.html` | 67625 | `95165f3d7251f0ddcf851e4c7a4e28c8aec009c1cc1d2ce30c19fa5a487bd0be` |
| [C-T1] | https://www.centerpartiet.se/nyheter/2025/2025-11-13-elisabeth-thand-ringqvist-vald-till-ny-partiledare-for-centerpartiet | Centerpartiet | "2025-11-13" | party news | `centerpartiet_20251113_thand-ringqvist-vald-till-ny-partiledare.html` | 67921 | `b4a3de31bed28620db7353f0f89ad63818ad2ac2a7ab34d53fab63c419a29b42` |
| [L-PV] | https://www.liberalerna.se/nyheter/johan-pehrson-vald-till-partiledare-for-liberalerna | Liberalerna | "Lördag 26 november 2022" | party news | `liberalerna_johan-pehrson-vald-till-partiledare.html` | 79172 | `aa5e39dc80e9a268e285aa0377d19104008933171185cb3685a35ddd5f7576c9` |
| [L-PA] | https://www.liberalerna.se/nyheter/johan-pehrson-avgar-som-partiledare-for-liberalerna-2 | Liberalerna | "Måndag 28 april 2025" | party news | `liberalerna_johan-pehrson-avgar-som-partiledare.html` | 80257 | `938ae89e4070aa822d901884718f071e81aea5c45a49a8f21a9b21c2b90ed360` |
| [L-SM] | https://www.liberalerna.se/nyheter/simona-mohamsson-ny-partiordforande-for-liberalerna | Liberalerna | "Tisdag 24 juni 2025" | party news | `liberalerna_simona-mohamsson-ny-partiordforande.html` | 79977 | `1a8a81267be697850b98e5f77a846d2e5b4b9d33091a6518325b23e4350e7ef1` |
| [L-EL] | https://www.liberalerna.se/evenemang/extra-landsmote-2025 | Liberalerna | "Tisdag 24 juni 2025 09:00" | event page | `liberalerna_extra-landsmote-2025.html` | 77013 | `78e0c874085449293c999bc99921ea4b5268c0736185db809f8ad92084dffe4b` |
| [MP-H] | https://www.mp.se/just-nu/daniel-hellden-ar-miljopartiets-nya-manliga-sprakror/ | Miljöpartiet | "Publicerad 2023-11-18 16:26:25", "Uppdaterad 2023-11-18 17:43:13" | party news | `mp_daniel-hellden-nya-manliga-sprakror.html` | 227835 | `70e4cdab5392f64e5008392ce3c926ed46c156cfe5dc5efad9617382fee3c2cd` |
| [MP-L] | https://www.mp.se/just-nu/amanda-lind-nytt-sprakror-for-miljopartiet-de-grona/ | Miljöpartiet | "Publicerad 2024-04-28 13:45:23" | party news | `mp_amanda-lind-nytt-sprakror.html` | 226311 | `579f948a97d1977cfe5157ba6ac82bf719075dc29374542a2f19ca8623ae996a` |
| [MP-RVB] | https://www.mp.se/just-nu/dessa-foreslas-leda-miljopartiet/ | Miljöpartiet | "Publicerad 2023-10-20 10:51:32" | party news, the 2023 nominations and the kongress dates | `mp_dessa-foreslas-leda-miljopartiet.html` | 228901 | `7bb0dd5da8b3db22e7714b4918b20223debeacae5f7d745c4a9e8a3941635017` |
| [MP-K] | https://www.mp.se/kalmar/just-nu/tack-for-allt-marta/ | Miljöpartiet, Kalmar branch (**local page, weak**) | "Publicerad 2024-02-10" | local branch news | `mp_kalmar_tack-for-allt-marta.html` | 220018 | `3bef31b5c8b8d3b2f560c7281e4b2d27bc0282b21ce1824a927b3e9b1a0c2855` |

Referenced, not re-fetched (already on disk, registered in `2026/government_2026.md` under `2026/raw/government/`):
[RD-TRB], [RG-SMA], [RD-N17a], [RD-N17b], [RD-N18], [RG-ART]; and in `2026/coalition_declarations_2026.md` under
`2026/raw/declarations/`: [L-P2].

## GAPS

- **G1: Annie Lööf's resignation announcement is undated here.** [C-D1] says only "Annie Lööfs avgångsbesked i
  september" (2022). She is treated as C's leader until 2023-02-02 on that page's "efter Annie Lööf".
- **G2: the end of Märta Stenevi's office.** Her departure is on a local branch page [MP-K] ("Igår meddelade …",
  published 2024-02-10, so **DERIVED** 2024-02-09); no national mp.se page was found, and no page says whether she
  held the office until the 2024-04-28 election of Amanda Lind or left it earlier.
- **G3: the Tidö agreement's signing date is not stated in terms on any fetched page.** 2022-10-14 rests on the
  talman's statement of that day [RD-PK14] and the PDF's creation stamps [TIDO]; the PDF's text was read through a crude
  content-stream extraction, not a full render.
- **G4: the 2018 seats come from Valmyndigheten's summary page** [VAL-18] (its 2026 revision); the 2018 decision PDF
  was not fetched, and [VAL-18H]'s seat column was not read. The sum is 349 (**DERIVED**).
- **G5: chambers are given as elected.** Members who left their party group, and substitutes, are not tracked for any
  of the three chambers.
- **G6: Andersson's cabinet composition (S alone)** rests on [RD-N1017]'s sentence and on [RG-SMA] ("en
  socialdemokratisk regering"); Regeringskansliet's own 2021 cabinet page was not fetched.
- **G7: reshuffles of the Kristersson cabinet** between 2022-10-18 and September 2025 are not tracked; [RG-FAKTA] is the
  one dated composition, and the live page [RG-SR] carries no party letters.
- **G8: the live "nothing on record" is a reading of silence** (vote register empty, calendar without a vote, feeds
  without a formation item after 18 Sep) with the same caveats as `2026/government_2026.md` G1; no page says in terms
  that no proposal has been made.
- **G9: the new talman is unknown** until 2026-09-28; the pending constitutional amendments (`2026/government_2026.md`
  G7) were not read.
- **G10: Pehrson's acting leadership from 2022-04-08** (after Sabuni) is before the window and is noted from [L-PA]
  only.
- **G11: the Riksdagsordningen was not fetched**; the tabling procedure is cited only through the talman's account.
- **G12: RF 6 kap. 7 §** (what follows a no-confidence declaration against the prime minister: dismissal, or an extra
  election within a week) was not quoted; it is in [RF-R] for the spec's rule set to read.
- **G13: second-session re-verification (R-K9) is pending**, as for every SOURCED file.

*(Filed 2026-09-24 by the PS-1 sourcing agent. Written: this file and `ElectionsData/sweden/raw/records/` (38 saved
pages, `SHA256SUMS.txt`, `fetch_log.txt`). No code was touched, Unity was not run, nothing was committed. One stray:
nine log lines with HTTP 000 and no file were appended to `ElectionsData/germany/raw/records/fetch_log.txt` by a
scratchpad script-name collision with a parallel agent and were removed again; that folder holds none of this pass's
files.)*
