# Sweden - the confidence and collapse rules (Regeringsformen 1974:152) and the 2021 precedent

Fetched 2026-09-25 from riksdagen.se: Regeringsformen (RF) as consolidated on the Riksdag's statute page, the page stamping
itself "Utfärdad: 1974-02-28", "Ändrad: t.o.m. SFS 2022:1600"; the Riksdag's own explanatory pages on the no-confidence
declaration, on extra elections and on how a government is formed; and the Riksdag's own vote-result PDFs for 21 June 2021
and 7 July 2021. All stored byte for byte under `raw/confidence/` with `SHA256SUMS` beside them. Every quote is verbatim
(whitespace collapsed, the page's own line breaks inside a sentence joined, the statute's "Lag (2010:1408) ." spacing kept
as the page has it); the plain English after each is my translation, not the page's. Nothing from memory. Class: SOURCED
`[PROVISIONAL]` until a second session re-verifies (R-K9).

Fetched with `curl -sL -A "Mozilla/5.0"`; riksdagen.se answered 200 for every page used, so neither Wayback nor lagen.nu
was needed for the Riksdag's pages. The statute page came back **byte-identical** to the copy already under
`raw/records/` (571814 bytes, sha256 `5c199d0d...`), so the id **[RF-R]** is reused unchanged; quote ids below are
`[RF-R:chapter:paragraph]`. RF 13 kap. 4 § is the same text already quoted in `records_by_date.md` §4 under [RF-R].

**Correction to the brief's numbering.** On the page: 6 kap. 6 § is the new PM's naming of ministers and the change of
government (not a discharge); the discharge on request is **6 kap. 8 §**; the whole government falling with the PM is
**6 kap. 9 §**; the caretaker government ("Övergångsregering") is **6 kap. 11 §**, not 9 §.

## The rules

| rule | what it says | id |
|---|---|---|
| Talman's proposal and the vote (negative parliamentarism) | The talman consults every party group, confers with the vice talmän, proposes a PM; the Riksdag votes within four days without committee preparation; the proposal falls only if **more than half of all members vote against** it, otherwise it is approved. | [RF-R:6:4] |
| Four rejections | A rejected proposal restarts the 6:4 procedure; after four rejections the procedure stops until an election has been held; an extra election within three months unless an ordinary one falls within that time anyway. | [RF-R:6:5] |
| Taking office | The approved PM announces the other ministers to the Riksdag; the change of government takes place at a special konselj before the head of state (or the talman); the talman signs the PM's commission on the Riksdag's behalf. | [RF-R:6:6] |
| **No confidence -> discharge, or an extra election within one week** | If the Riksdag declares that the PM or another minister no longer has its confidence, **the talman shall discharge** that minister - **unless** the government can decide on an extra election and does so **within one week** of the declaration. | [RF-R:6:7] |
| Resignation on request | A minister shall be discharged on request - the PM by the talman, another minister by the PM; the PM may also discharge ministers in other cases. | [RF-R:6:8] |
| The PM falls -> the whole government falls | If the PM is discharged or dies, the talman shall discharge the other ministers. | [RF-R:6:9] |
| Caretaker government ("Övergångsregering") | When all members of the government have been discharged, they remain in post until a new government has taken office. | [RF-R:6:11] |
| The motion of no confidence | Needs **at least one tenth** of the members to be taken up (the Riksdag's page: **35**); needs **more than half of the members** voting for it (the Riksdag's page: **175 of 349**); none taken up between an election (or an announced extra election) and the new Riksdag's convening; never against a caretaker minister; not prepared in committee. | [RF-R:13:4], [RD-MF-2], [RD-MF-3] |
| The government's extra election, and its limits | The government may decide on an extra election between ordinary elections, to be held within three months of the decision; **not** until three months after the newly elected Riksdag's first sitting; **not** while it is a caretaker (all members discharged). | [RF-R:3:11] |
| What follows, in the Riksdag's words | No confidence in the PM: the whole government must resign **or** call an extra election; in a minister: the minister must resign. An extra election after a no-confidence declaration must be decided within a week. | [RD-MF-4], [RD-EV-2], [RD-SBR-2] |

**The chain the statute fixes** (**DERIVED**, by reading the paragraphs together): a no-confidence declaration against the
PM [RF-R:13:4] -> within one week, either the government decides an extra election (only if 3:11 lets it: not within
three months of a new Riksdag's first sitting, not as a caretaker) [RF-R:6:7], [RF-R:3:11] -> or the talman discharges
the PM [RF-R:6:7], and with the PM the whole government [RF-R:6:9] -> the discharged government stays on as caretaker
[RF-R:6:11] -> the talman's proposal procedure, negative parliamentarism, up to four proposals [RF-R:6:4], [RF-R:6:5]
-> the approved PM takes office at a konselj [RF-R:6:6]. A PM may also shortcut the week by asking to be discharged
[RF-R:6:8] - which is what happened in 2021 (below).

## The statute, verbatim - Regeringsformen (1974:152), 6 kap. "Regeringen"

**[RF-R:6:4]** 6 kap. 4 § (under the heading "Regeringsbildningen") "När en statsminister ska utses, kallar talmannen företrädare för varje partigrupp inom riksdagen till samråd. Talmannen överlägger med vice talmännen och lämnar sedan förslag till riksdagen."
"Riksdagen ska inom fyra dagar, utan beredning i utskott, pröva förslaget genom omröstning. Om mer än hälften av riksdagens ledamöter röstar mot förslaget, är det förkastat. I annat fall är det godkänt. Lag (2010:1408) ."
*Translation (mine):* When a prime minister is to be appointed, the Speaker calls representatives of each party group in the Riksdag to consultation. The Speaker confers with the Deputy Speakers and then submits a proposal to the Riksdag. The Riksdag shall within four days, without committee preparation, try the proposal by a vote. If more than half of the Riksdag's members vote against the proposal, it is rejected. Otherwise it is approved.

**[RF-R:6:5]** 6 kap. 5 § "Förkastar riksdagen talmannens förslag, ska förfarandet enligt 4 § upprepas. Har riksdagen fyra gånger förkastat talmannens förslag, ska förfarandet avbrytas och återupptas först sedan val till riksdagen har hållits. Om inte ordinarie val ändå ska hållas inom tre månader, ska extra val hållas inom samma tid. Lag (2010:1408) ."
*Translation (mine):* If the Riksdag rejects the Speaker's proposal, the procedure under 4 § is repeated. If the Riksdag has rejected the Speaker's proposal four times, the procedure is broken off and resumed only after an election to the Riksdag has been held. Unless an ordinary election is due within three months anyway, an extra election shall be held within that time.

**[RF-R:6:6]** 6 kap. 6 § "När riksdagen har godkänt ett förslag om ny statsminister, ska han eller hon så snart det kan ske anmäla de övriga statsråden för riksdagen. Därefter äger regeringsskifte rum vid en särskild konselj inför statschefen eller, om statschefen har förhinder, inför talmannen. Talmannen ska alltid kallas till konseljen."
"Talmannen utfärdar förordnande för statsministern på riksdagens vägnar. Lag (2010:1408) ."
*Translation (mine):* When the Riksdag has approved a proposal for a new prime minister, he or she shall as soon as possible announce the other ministers to the Riksdag. The change of government then takes place at a special council before the head of state or, if the head of state is prevented, before the Speaker. The Speaker shall always be summoned to the council. The Speaker issues the prime minister's commission on the Riksdag's behalf.

**[RF-R:6:7]** 6 kap. 7 § (under the heading "Entledigande av statsministern eller annat statsråd") "Förklarar riksdagen att statsministern eller något annat statsråd inte har riksdagens förtroende, ska talmannen entlediga statsrådet. Om regeringen kan besluta om extra val till riksdagen och gör det inom en vecka från misstroendeförklaringen, ska något entledigande dock inte ske."
"I 3 § finns bestämmelser om entledigande av statsministern med anledning av en statsministeromröstning efter val. Lag (2010:1408)."
*Translation (mine):* If the Riksdag declares that the prime minister or any other minister does not have the Riksdag's confidence, the Speaker shall discharge the minister. If the government can decide on an extra election to the Riksdag and does so within one week of the declaration of no confidence, no discharge shall however take place. Provisions on discharging the prime minister following a prime-minister vote after an election are in 3 §.

**[RF-R:6:8]** 6 kap. 8 § "Ett statsråd ska entledigas om han eller hon begär det, statsministern av talmannen och ett annat statsråd av statsministern. Statsministern får även i andra fall entlediga statsråd. Lag (2010:1408) ."
*Translation (mine):* A minister shall be discharged if he or she requests it - the prime minister by the Speaker and another minister by the prime minister. The prime minister may also discharge ministers in other cases.

**[RF-R:6:9]** 6 kap. 9 § "Om statsministern entledigas eller dör, ska talmannen entlediga de övriga statsråden. Lag (2010:1408) ."
*Translation (mine):* If the prime minister is discharged or dies, the Speaker shall discharge the other ministers.

**[RF-R:6:11]** 6 kap. 11 § (under the heading "Övergångsregering") "Har regeringens samtliga ledamöter entledigats, uppehåller de sina befattningar till dess en ny regering har tillträtt. Har ett annat statsråd än statsministern entledigats på egen begäran, uppehåller han eller hon sin befattning till dess en efterträdare har tillträtt, om statsministern begär det. Lag (2010:1408) ."
*Translation (mine):* If all members of the government have been discharged, they remain in post until a new government has taken office. If a minister other than the prime minister has been discharged at his or her own request, he or she remains in post until a successor has taken office, if the prime minister so requests.

(6 kap. 3 §, the post-election prime-minister vote, and 6 kap. 12 §, a vice talman acting for the talman, are quoted in
`2026/government_2026.md` Fact 2 from the same statute page; 6 kap. 10 §, the PM's deputy, was read on the page and is not
a confidence rule.)

## The statute, verbatim - 13 kap. "Kontrollmakt" and 3 kap. "Riksdagen"

**[RF-R:13:4]** 13 kap. 4 § (under the heading "Misstroendeförklaring") "Riksdagen kan förklara att ett statsråd inte har riksdagens förtroende. Ett yrkande om en sådan misstroendeförklaring ska väckas av minst en tiondel av riksdagens ledamöter för att tas upp till prövning. För en misstroendeförklaring krävs att mer än hälften av riksdagens ledamöter röstar för den."
"Ett yrkande om misstroendeförklaring tas inte upp till prövning om det väcks under tiden från det att ordinarie val har ägt rum eller beslut om extra val har meddelats till dess den genom valet utsedda riksdagen samlas. Ett yrkande avseende ett statsråd som efter att ha entledigats uppehåller sin befattning enligt 6 kap. 11 § får inte i något fall tas upp till prövning."
"Ett yrkande om misstroendeförklaring ska inte beredas i utskott. Lag (2010:1408) ."
*Translation (mine):* The Riksdag may declare that a minister does not have the Riksdag's confidence. A motion for such a declaration must be raised by at least one tenth of the Riksdag's members to be taken up. A declaration of no confidence requires that more than half of the Riksdag's members vote for it. A motion is not taken up if raised in the period from an ordinary election having been held, or a decision on an extra election having been announced, until the Riksdag chosen by that election convenes. A motion concerning a minister who, having been discharged, remains in post under 6 kap. 11 § may in no case be taken up. A motion of no confidence shall not be prepared in committee.
*(Same text as quoted in `records_by_date.md` §4 under [RF-R].)* **DERIVED:** one tenth of 349 = 34.9, so 35 members; more than half of 349 = more than 174.5, so **175**. Both figures are stated by the Riksdag itself in [RD-MF-2] and [RD-MF-3].

**[RF-R:3:11]** 3 kap. 11 § (under the heading "Extra val") "Regeringen får besluta om extra val till riksdagen mellan ordinarie val. Extra val ska hållas inom tre månader efter beslutet."
"Efter val till riksdagen får regeringen inte besluta om extra val förrän tre månader har gått från den nyvalda riksdagens första sammanträde. Regeringen får inte heller besluta om extra val under den tid då dess ledamöter, efter det att samtliga har entledigats, uppehåller sina befattningar till dess en ny regering ska tillträda."
"Bestämmelser om extra val i visst fall finns i 6 kap. 5 §. Lag (2010:1408)."
*Translation (mine):* The government may decide on an extra election to the Riksdag between ordinary elections. The extra election shall be held within three months of the decision. After an election to the Riksdag the government may not decide on an extra election until three months have passed from the newly elected Riksdag's first sitting. Nor may the government decide on an extra election while its members, all having been discharged, remain in post until a new government is to take office. Provisions on an extra election in a certain case are in 6 kap. 5 §.

**DERIVED for the live window** (not a page's words): the 2026 Riksdag's first sitting is scheduled for Monday 28
September 2026 (`2026/government_2026.md`, [RD-KAL]); by 3:11 second paragraph no government may decide an extra election
before three months have passed from that sitting (reckoned to 28 December 2026 - the statute does not state how the
months are counted, see GAPS), and the present caretaker cabinet cannot decide one at all; by 13:4 second paragraph no
no-confidence motion can be taken up before the new Riksdag convenes, and never one against a caretaker minister. So,
until a new government takes office, the 6:7 extra-election exception is unavailable to the sitting cabinet.

## The Riksdag's own explanatory pages, verbatim

**[RD-MF-1]** "Misstroendeförklaring" (riksdagen.se, page undated): "Om riksdagen inte har förtroende för statsministern eller för en minister kan riksdagen tvinga regeringen eller ministern att avgå genom att besluta om en misstroendeförklaring."
*Translation (mine):* If the Riksdag has no confidence in the prime minister or a minister, it can force the government or the minister to resign by deciding on a declaration of no confidence.

**[RD-MF-2]** (same page, "Förslag om misstroendeförklaring") "Minst 35 ledamöter måste först gå ihop och föreslå att riksdagen ska rösta om att göra en misstroendeförklaring för att en omröstning ska bli av."
*Translation (mine):* At least 35 members must first join together and propose that the Riksdag vote on a declaration of no confidence for a vote to take place.

**[RD-MF-3]** (same page, "Omröstning") "Minst 175 ledamöter måste sedan rösta ja till förslaget för att riksdagen ska förklara sitt misstroende mot regeringen eller en minister. Det är en majoritet av riksdagens 349 ledamöter."
*Translation (mine):* At least 175 members must then vote yes for the Riksdag to declare its lack of confidence in the government or a minister. That is a majority of the Riksdag's 349 members.

**[RD-MF-4]** (same page, "Om majoriteten säger ja") "Om riksdagen kommer fram till att den inte har förtroende för statsministern måste hela regeringen avgå eller utlysa ett extra val. Om riksdagen kommer fram till att den inte har förtroende för en minister måste ministern avgå."
*Translation (mine):* If the Riksdag concludes it has no confidence in the prime minister, the whole government must resign or call an extra election. If it concludes it has no confidence in a minister, the minister must resign.

**[RD-MF-5]** (same page) "Riksdagen har röstat om misstroendeförklaring 14 gånger. En gång har riksdagen röstat ja till en misstroendeförklaring. Det var den 21 juni 2021 då riksdagen röstade ja till en begäran om att rikta en misstroendeförklaring mot dåvarande statsminister Stefan Löfven (S)."
*Translation (mine):* The Riksdag has voted on a declaration of no confidence 14 times. Once it has voted yes: on 21 June 2021, when it voted yes to a request to direct a declaration of no confidence against the then prime minister Stefan Löfven (S).

**[RD-MF-6]** (same page, the tab headed "Juni – 2021 Stefan Löfvén (S)" - the accent is the page's) "På begäran av Sverigedemokraterna genomfördes en omröstning om misstroende mot statsminister Stefan Löfven (S) den 21 juni 2021. 181 ledamöter röstade ja till misstroendeförklaringen, 109 ledamöter röstade nej och 51 ledamöter avstod från att rösta. 8 ledamöter var frånvarande."
*Translation (mine):* At the Sweden Democrats' request a vote of no confidence in prime minister Stefan Löfven (S) was held on 21 June 2021. 181 members voted yes, 109 no, 51 abstained, 8 were absent.

**[RD-EV-1]** "Extra val" (riksdagen.se, "Publicerad 17 augusti 2026 Uppdaterad 17 augusti 2026"): "Om regeringen väljer att utlysa ett extra val efter en misstroendeförklaring så måste det beslutet tas inom en vecka från misstroendeförklaringen."
*Translation (mine):* If the government chooses to call an extra election after a declaration of no confidence, that decision must be taken within one week of the declaration.

**[RD-EV-2]** (same page) "Om en majoritet i riksdagen inte har förtroende för statsministern eller en annan minister kan den rikta en så kallad misstroendeförklaring mot ministern. Om riksdagen riktar en misstroendeförklaring mot statsministern måste statsministern och regeringen avgå eller besluta om ett extra val."
*Translation (mine):* If a majority of the Riksdag has no confidence in the prime minister or another minister, it can direct a so-called declaration of no confidence against the minister. If it directs one against the prime minister, the prime minister and the government must resign or decide on an extra election.

**[RD-EV-3]** (same page) "Beslut om extra val får inte fattas av en övergångsregering, det vill säga en regering som har avgått men som sitter kvar för att sköta det löpande arbetet till dess att en ny regering har utsetts." / "Efter ett ordinarie val får extra val utlysas tidigast tre månader efter att den nya riksdagen har samlats för första gången efter det ordinarie valet." / "Extra val ändrar inte tidpunkterna för de ordinarie valen. De riksdagsledamöter som väljs vid ett extra val påbörjar alltså inte en ny fyraårig mandatperiod, utan sitter till nästa ordinarie val."
*Translation (mine):* A caretaker government - one that has resigned but stays on to run current business until a new government is appointed - may not decide on an extra election. / After an ordinary election, an extra election may be called at the earliest three months after the new Riksdag first convened. / An extra election does not change the dates of ordinary elections; members elected at one sit only until the next ordinary election.

**[RD-SBR-1]** "Så bildas regeringen" (riksdagen.se, "Publicerad 27 april 2023"; the same URL is [RD-SBR] in `2026/government_2026.md`): "En regering kan sitta kvar så länge den har tillräckligt stöd i riksdagen. Om en regering förlorar riksdagens stöd kan den tvingas att avgå. När en statsminister avgår betyder det att hela regeringen avgår samtidigt."
*Translation (mine):* A government can remain as long as it has sufficient support in the Riksdag. If it loses that support it can be forced to resign. When a prime minister resigns, the whole government resigns at the same time.

**[RD-SBR-2]** (same page, "Riksdagen kan rösta om en misstroendeförklaring") "Minst 35 riksdagsledamöter kan när som helst begära att riksdagen ska uttala sitt misstroende mot en statsminister och därmed mot regeringen. Då ska riksdagen rösta om en misstroendeförklaring. Om minst hälften av ledamöterna, det vill säga minst 175, röstar för en sådan misstroendeförklaring måste regeringen avgå eller utlysa ett extra val."
*Translation (mine):* At least 35 members can at any time request that the Riksdag declare its lack of confidence in a prime minister and thereby in the government. The Riksdag must then vote on it. If at least half the members, that is at least 175, vote for it, the government must resign or call an extra election.
*Note:* this explainer's "när som helst" (at any time) and "minst hälften" (at least half) are looser than the statute, which bars motions between an election and the new Riksdag's convening and requires **more than** half [RF-R:13:4]. The figure 175 is the same either way for 349 members. The statute governs.

**[RD-SBR-3]** (same page) "En övergångsregering hanterar främst löpande och brådskande ärenden. Den får inte besluta om extra val och brukar enligt praxis inte ta nya politiska initiativ." / "När en regering avgår är det talmannen som fattar beslut om att bevilja statsministerns och de övriga ministrarnas avgång."
*Translation (mine):* A caretaker government mainly handles current and urgent business. It may not decide on an extra election and by practice does not take new political initiatives. / When a government resigns, it is the Speaker who decides to grant the resignation of the prime minister and the other ministers.

## The 2021 sequence - dated facts

| date | fact | id |
|---|---|---|
| 2021-06-17 | 36 members requested that the Riksdag try a motion of no confidence in PM Stefan Löfven (S) (above the 35 threshold, **DERIVED**). | [RD-TRB-1] |
| 2021-06-21, 10:52:24 | Vote no. 943, riksmöte 2020/21, "Prövning av yrkande om misstroendeförklaring mot statsminister Stefan Löfven (S)": **Ja 181, Nej 109, Avstår 51, Frånv. 8**. By party: S 0/94/0/6; M 70/0/0/0; SD 62/0/0/0; C 0/0/30/1; V 27/0/0/0; KD 22/0/0/0; L 0/0/19/0; MP 0/15/0/1; "-" (no party) 0/0/2/0 (Ja/Nej/Avstår/Frånv.). **DERIVED:** 181 >= 175, carried; totals sum to 349; the yes votes are M+SD+V+KD. The request came from SD. | [RD-VOT-0621], [RD-VOT-0621S], [RD-MF-6], [RD-TRB-1] |
| 2021-06-28 | Löfven asked the talman to be discharged as PM (the 6:8 route, not an extra election under 6:7). **DERIVED:** 21 June + 7 days = 28 June; no fetched page says whether the week had run or how it is counted. | [RD-TRB-1] |
| 2021-06-29 | The talman began the task of producing a PM proposal. | [RD-TRB-2] |
| 2021-07-05 (Monday) | Talman Andreas Norlén again proposed Stefan Löfven as PM. | [RD-TRB-2] |
| 2021-07-07 (Wednesday), 14:58:15 | Vote no. 1037, riksmöte 2020/21, "Prövning av förslag till statsminister": **Ja 116, Nej 173, Avstår 60, Frånv. 0**. By party: S 100/0/0/0; M 0/70/0/0; SD 0/62/0/0; C 0/0/31/0; V 0/0/27/0; KD 0/22/0/0; L 0/18/1/0; MP 16/0/0/0; "-" 0/1/1/0. The Riksdag approved the proposal. **DERIVED:** 173 against < 175, so approved under 6:4 although only 116 voted yes; totals sum to 349. | [RD-VOT-0707], [RD-TRB-2] |
| (after 2021-07-07) | The government formed is named "Löfven III 2021" / "Löfven III, S+MP, 2021" on the Riksdag's page. The date of the change of government (6:6 konselj) is **not sourced** (GAPS). | [RD-TRB-3] |

**[RD-TRB-1]** "Tidigare regeringsbildningar och statsministrar" (riksdagen.se, "Publicerad 27 april 2023"; the same URL is
[RD-TRB] in `2026/government_2026.md`), under "Regeringsbildningen sommaren 2021 (Löfven III 2021)": "Den 17 juni 2021 begärde 36 ledamöter att riksdagen skulle pröva ett yrkande om misstroendeförklaring mot statsminister Stefan Löfven (S). Den 21 juni 2021 röstade riksdagen ja till misstroendeförklaringen mot statsministern och den 28 juni bad Stefan Löfven talmannen om att bli entledigad som statsminister."
*Translation (mine):* On 17 June 2021, 36 members requested that the Riksdag try a motion of no confidence in prime minister Stefan Löfven (S). On 21 June 2021 the Riksdag voted yes to the declaration against the prime minister, and on 28 June Stefan Löfven asked the Speaker to be discharged as prime minister.

**[RD-TRB-2]** (same page, same section) "Talmannen inledde den 29 juni uppdraget att ta fram ett förslag till statsminister för riksdagen att ta ställning till. Måndagen den 5 juli föreslog talman Andreas Norlén åter Stefan Löfven till ny statsminister och onsdagen den 7 juli godkände riksdagen förslaget."
*Translation (mine):* On 29 June the Speaker began the task of producing a proposal for prime minister for the Riksdag to decide on. On Monday 5 July Speaker Andreas Norlén again proposed Stefan Löfven as the new prime minister, and on Wednesday 7 July the Riksdag approved the proposal.

**[RD-TRB-3]** (same page, the list of governments) "Löfven III, S+MP, 2021"
*Translation:* Löfven III, S+MP, 2021.

**[RD-VOT-0621S]** the Riksdag's scanned vote printout (image-only PDF; page 1 of 6 read as an image, the scanner's metadata
"CreationDate D:20210621111018"), text as printed: "Voteringsnr: 943" / "Riksmöte: 2020/21" / "Ärende: 2020/21:Prövning av yrkande om misstroendeförklaring mot statsminister Stefan Löfven (S)" / "Voteringstyp: Övrig" / "Stegnivå: 1 Huvudvotering" / "Datum & tid: 2021-06-21 10:52:24" / "Totalt: Ja: 181 Nej: 109 Avstår: 51 Frånv.: 8" / footer "Utskrivet 2021-06-21 10:53".
*Translation (mine):* vote no. 943, session 2020/21, item: trial of a motion of no confidence in PM Stefan Löfven (S); vote type: other; step 1, main vote; 21 June 2021 10:52:24; totals yes 181, no 109, abstain 51, absent 8; printed 21 June 2021 10:53.

**[RD-VOT-0621]** the Riksdag's text-layer copy of the same printout (linked from [RD-TRB] as "Misstroendeomröstning 21 juni
2021"; "Microsoft: Print To PDF", created 2022-10-26), text decoded through the PDF's own ToUnicode maps: the same header
fields as [RD-VOT-0621S] and the party table "S | 0 | 94 | 0 | 6", "M | 70 | 0 | 0 | 0", "SD | 62 | 0 | 0 | 0", "C | 0 | 0 | 30 | 1", "V | 27 | 0 | 0 | 0", "KD | 22 | 0 | 0 | 0", "L | 0 | 0 | 19 | 0", "MP | 0 | 15 | 0 | 1", "- | 0 | 0 | 2 | 0" under "Parti Ja Nej Avstår Frånv.", and "Ja: 181", "Nej: 109", "Avstår: 51", "Frånv.: 8" (the "Totalt" value is not in the text layer; 349 is **DERIVED** by sum). The party table was also read on the scan's page 1 and matches.

**[RD-VOT-0707]** the Riksdag's text-layer vote printout (linked from [RD-TRB] as "Voteringsresultat prövning av
talmannens förslag till statsminister 7 juli 2021"; "Microsoft: Print To PDF", created 2022-10-26), decoded the same way: "Voteringsnr: 1037", "Riksmöte: 2020/21", "Ärende: 2020/21:Prövning av förslag till statsminister", "Voteringstyp: Övrig", "Stegnivå: 1 Huvudvotering", "Datum & tid: 2021-07-07 14:58:15", "Ja: 116", "Nej: 173", "Avstår: 60", "Frånv.: 0", party table "S | 100 | 0 | 0 | 0", "M | 0 | 70 | 0 | 0", "SD | 0 | 62 | 0 | 0", "C | 0 | 0 | 31 | 0", "V | 0 | 0 | 27 | 0", "KD | 0 | 22 | 0 | 0", "L | 0 | 18 | 1 | 0", "MP | 16 | 0 | 0 | 0", "- | 0 | 1 | 1 | 0". The printout does not name the proposed PM; the name is [RD-TRB-2]'s.
*Translation (mine):* vote no. 1037, session 2020/21, item: trial of the proposal for prime minister; 7 July 2021 14:58:15; yes 116, no 173, abstain 60, absent 0.

## Register of ids

| id | URL | publisher | page date | file (under `raw/confidence/`) | bytes | sha256 |
|---|---|---|---|---|---|---|
| [RF-R] (quotes [RF-R:6:4], [RF-R:6:5], [RF-R:6:6], [RF-R:6:7], [RF-R:6:8], [RF-R:6:9], [RF-R:6:11], [RF-R:13:4], [RF-R:3:11]) | https://www.riksdagen.se/sv/dokument-och-lagar/dokument/svensk-forfattningssamling/kungorelse-1974152-om-beslutad-ny-regeringsform_sfs-1974-152/ | Sveriges riksdag (SFS consolidated text; källa Regeringskansliet fulltext) | Utfärdad 1974-02-28; ändrad t.o.m. SFS 2022:1600 | `riksdagen_sfs-1974-152_regeringsformen.html` | 571814 | 5c199d0d0c10190e53f66ebfb3d8e1d0097e91de71e2f7800449a14a533ca66e (identical to `raw/records/`'s copy) |
| [RD-MF-1] ... [RD-MF-6] | https://www.riksdagen.se/sv/sa-fungerar-riksdagen/riksdagens-uppgifter/kontrollerar-regeringen/misstroendeforklaring/ | Sveriges riksdag | **no date on the page** (its latest event is 2024-01-17) | `riksdagen_misstroendeforklaring.html` | 426897 | ba14ec6d036bb1b98116f488478fdc18c43f5ec13fa71ab7c342780517fa064d |
| [RD-EV-1] ... [RD-EV-3] | https://www.riksdagen.se/sv/sa-fungerar-riksdagen/demokrati/val-till-riksdagen/extra-val/ | Sveriges riksdag | Publicerad 17 augusti 2026, uppdaterad 17 augusti 2026 | `riksdagen_extra-val.html` | 366669 | d8a0e05be25e148225004b6ce2df8d4994345f129c1e82e0ecf3628c71f47cc4 |
| [RD-SBR-1] ... [RD-SBR-3] (page = [RD-SBR]) | https://www.riksdagen.se/sv/sa-fungerar-riksdagen/demokrati/sa-bildas-regeringen/ | Sveriges riksdag | Publicerad 27 april 2023 | `riksdagen_sa-bildas-regeringen.html` | 403362 | 1384edf4c019db95643ce7ea1e89f3e4c74f98236453a067eb35f9ea75be9bb1 |
| [RD-TRB-1] ... [RD-TRB-3] (page = [RD-TRB]) | https://www.riksdagen.se/sv/sa-fungerar-riksdagen/demokrati/sa-bildas-regeringen/tidigare-regeringsbildningar-och-statsministrar/ | Sveriges riksdag | Publicerad 27 april 2023 | `riksdagen_tidigare-regeringsbildningar.html` | 408405 | 92eded0f8ad5974c8c28b541b7545a8d2e550e5e975cacfc67822dc832de1e35 |
| [RD-VOT-0621S] | https://www.riksdagen.se/globalassets/05.-sa-fungerar-riksdagen/riksdagens-uppgifter/kontrollerar-regeringen/20210621-misstro-lofven.pdf | Sveriges riksdag (vote printout, scanned) | vote 2021-06-21 10:52:24; printed 2021-06-21 10:53 | `riksdagen_20210621-misstro-lofven.pdf` | 1412805 | a227ee82d44bb711196e83ed61e66d613f371037f0326021df0e9812740f2354 |
| [RD-VOT-0621] | https://www.riksdagen.se/globalassets/05.-sa-fungerar-riksdagen/demokrati/sa-bildas-regeringen/tidigare-regeringsbildningar/misstroendeomrostning-21-juni-2021.pdf | Sveriges riksdag (vote printout) | vote 2021-06-21 10:52:24 | `riksdagen_misstroendeomrostning-21-juni-2021.pdf` | 370218 | d9bb725d6c49cb5947ad20a40143f2299404caf8cb6a1b97682b719d7cf1cd2b |
| [RD-VOT-0707] | https://www.riksdagen.se/globalassets/05.-sa-fungerar-riksdagen/demokrati/sa-bildas-regeringen/tidigare-regeringsbildningar/voteringsresultat-provning-av-talmannens-forslag-till-statsminister-7-juli-2021.pdf | Sveriges riksdag (vote printout) | vote 2021-07-07 14:58:15 | `riksdagen_voteringsresultat-talmannens-forslag-7-juli-2021.pdf` | 367743 | b92a7d18a6d5579e85d697aa341fd7fb97ee28358c4d02bd12458d0c8ca6c552 |

All eight fetched 2026-09-25 with `curl -sL -A "Mozilla/5.0"`, HTTP 200, stored unaltered; `raw/confidence/SHA256SUMS`
lists them (a new folder) and `sha256sum -c` passes. [RD-SBR] and [RD-TRB] were fetched before (`2026/raw/government/`,
different bytes because riksdagen.se serves its page chrome fresh); the sentences quoted here were checked present in both
copies by grep.

## GAPS

- **G12 CLOSES.** RF 6 kap. 7 § - what follows a carried no-confidence motion (the talman discharges the minister,
  unless the government, able to, decides an extra election within one week) - is now quoted verbatim [RF-R:6:7] from
  [RF-R], the same bytes as `records_by_date.md`'s copy. The consequence chain (6:9 whole government, 6:11 caretaker)
  is quoted with it.
- **How "inom en vecka" and "tre månader" are counted** is not stated by the statute or any fetched page (whether the
  week of 21 June 2021 ended on 28 June or 27 June; whether three months from 28 Sep 2026 end on 28 Dec). The
  Riksdagsordningen and any KU commentary were not read for this. The 2021 dates are sourced; their fit to the week is
  **DERIVED** only.
- **The government's own 2021 pages were not obtained.** regeringen.se no longer serves its 2021 press pages (the
  28 June 2021 press-conference notice and "Hålltider för regeringsskiftet 9 juli" both answer with a generic page of
  282896 bytes, not the item); the Wayback Machine was "Temporarily Offline" (HTTP 429 then an outage page) on the run.
  So Löfven's own resignation statement and **the date of the 2021 change of government (the 6:6 konselj)** are not
  sourced; the Wayback CDX listing seen earlier on the run names a regeringen.se page "halltider-for-regeringsskiftet-9-juli",
  which is a URL, not a quoted fact.
- **The Riksdag's chamber protocols** for 21 June, 28 June-7 July 2021 (the talman's announcement of the discharge, the
  6:7 week, the proposal of 5 July) were not fetched; the sequence rests on [RD-TRB] and the two vote printouts.
- **Who tabled the 17 June 2021 motion** by name: [RD-MF-6] says "På begäran av Sverigedemokraterna"; [RD-TRB-1] says
  36 members; the motion document itself was not fetched.
- **The misstroendeförklaring page carries no date** [RD-MF]; its content runs to 2024-01-17 (its latest listed vote).
- **The scanned printout** [RD-VOT-0621S] was read on its first page only (pages 2-6, the per-member list, were not
  viewed); the member-level record rests on [RD-VOT-0621]'s text layer, which was decoded but not checked member by member.
- **Pending constitutional amendments** (`2026/raw/government/riksdagen_20260616_vilande-grundlagsandringar.html`,
  `2026/government_2026.md` G7) were not read for whether any touches 3 kap. 11 §, 6 kap. 4-11 §§ or 13 kap. 4 §. The
  statute page is consolidated only through SFS 2022:1600.
- **Second-session re-verification (R-K9) is pending**, as for every SOURCED file.

*(Filed 2026-09-25 by a sourcing agent. Written: this file and `ElectionsData/sweden/raw/confidence/` (eight saved
pages/PDFs, `SHA256SUMS`). No code touched, Unity not run, nothing committed.)*
