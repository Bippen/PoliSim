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
| M,KD,L ↔ SD | MajorityCoalition, cabinet S+M+C+KD+L — **changed** (as measured before K-1f; since K-1f, §607, with the S-M candidacy pair in 2022's lines: MajorityCoalition, cabinet **SD+M+KD+L** (176) — still changed) | **LOAD-BEARING**: without it SD is admissible in cabinet and the Tidö shape is gone entirely. |
| the S-M candidacy pair (K-1f, §607) | ConfidenceAndSupply, cabinet M+KD+L + SD support — **unchanged** (`Formation2026Diagnostic`, log `k1_607b_formation`) | **NOT LOAD-BEARING on 2022's chamber**: it is carried because the 2022 election reads its own dated candidacies (standing, K-1f), not because it moves the backtest. Where the Tidö lines are dropped, it decides between the grand coalition and SD+M+KD+L (row above). |

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

## PRIME-MINISTERIAL CANDIDACIES (2022 vintage, for K-1f)

Added 2026-09-24 for K-1f. The sourcing changed nothing above this heading; the wiring it fed (§607) added the candidacy
pair's row to the measured table above and re-measured the M,KD,L ↔ SD row, both marked.

**The ruling this section serves (Elias, 2026-09-24):** a declared prime-ministerial candidacy is a
constraint: *a party that declared its OWN LEADER as its candidate refuses any cabinet led by another
party's candidate.* **Standing:** *declarations are dated, and every election reads its own date's.*
This section is the record for the **2022 election (11 September 2022)** as it stood **before
polling day**. The 2026 record is in `2026/coalition_declarations_2026.md` and is not read here.

**Method.** Every quote below comes from a page fetched on **2026-09-24** and saved under
`raw/declarations_2022/`. The only change is that the HTTP content-encoding was decoded. Internet Archive
captures were fetched in `id_` mode, which returns the page as archived without the archive's toolbar.
Each file's SHA-256 is in the register and in `raw/declarations_2022/SHA256SUMS.txt`. Party
channels were searched first. News reports are marked **secondary**. The eight parties fall into three classes:
**(a)** declared its own leader (the case the ruling covers); **(b)** named another party's leader,
marked **NOT own-leader**; **(c)** nothing found, a **GAP**, left empty rather than inferred.
Quotes are in the original Swedish. English glosses are marked *gloss:* and are mine, not the sources'.

| party | own leader declared as PM candidate? | candidate | date | source | verbatim |
|---|---|---|---|---|---|
| **S** | **(a) YES** | **Magdalena Andersson** (S), the sitting prime minister | party page 2022-08-04, still standing in the 2022-08-13 capture; press 2022-08-15 and 2022-09-07 | [S-P1], [S-P1a] primary; [X-I1], [C-I1] secondary | S, [S-P1]: *"– Den 11 september väljer svenska folket riktning och ledarskap för Sverige. Vi har i Magdalena Andersson en ledare som är både kompetent och handlingskraftig. Det är tydligt att Socialdemokraterna, med Magdalena Andersson i spetsen, ser problemen och har de politiska lösningarna för att ta tag i dem, säger Tobias Baudin."* *gloss:* on 11 September the people choose direction and leadership for Sweden; in Magdalena Andersson we have a leader who is competent and decisive (the party secretary). TT via DI, [X-I1]: *"Rivalerna om statsministerposten, M-ledaren Ulf Kristersson och S-ledaren Magdalena Andersson, deltog på tisdagskvällen i TV4:s partiledarutfrågning."* and *"Inte heller S-ledaren Magdalena Andersson gav några nya besked om vilken regering hon vill bilda om de rödgröna vinner valet."* *gloss:* the rivals for the premiership, M's Kristersson and S's Andersson...; Andersson gave no new word on which government she wants to form if the red-greens win. Andersson's own words in the same report (**secondary**), asked whether she prefers a pure S government after a red-green win: *"Jag är öppen för olika möjligheter. Det finns fördelar med fler partier i en regering och det finns fördelar med en enpartiregering."* *gloss:* I am open to different possibilities; there are advantages to more parties in a government and advantages to a single-party government. SVT, [C-I1]: *"Statsminister Magdalena Andersson säger att hon inte vill diskutera specifika regeringsalternativ, om hon får chansen att bilda regering."* *gloss:* Andersson does not want to discuss specific government options if she gets the chance to form a government. Her own words in the same report (**secondary**): *"– Jag kommer inte sätta upp olika låsningar utan jag är öppen för olika former av samarbete i en regeringskonstellation, både koalitionsregering eller en ren socialdemokratisk regering, säger hon till TT."* *gloss:* I will not set up any lock-ins; I am open to different forms of cooperation in a government, either a coalition or a purely Social Democratic government, she tells TT. ⚠ **The party's own text says *ledarskap* (leadership). It does not use the word *statsministerkandidat*. It does name her *"partiordföranden och statsminister Magdalena Anderson"* (sic) *gloss:* the party chair and prime minister Magdalena Andersson. That is her sitting office, named in listing the faces on the posters, not a declared candidacy. The candidacy wording (*statsministerposten*) comes from the press.** |
| **M** | **(a) YES** | **Ulf Kristersson** (M) | party page 2022-03-26, unchanged in the 2022-09-10 23:20 UTC capture (the night before polling); his own words 2022-09-06, reported 2022-09-07 | [M-P1], [M-P1a] primary; [X-I1], [C-I1] secondary | M, [M-P1]: *"Moderaternas löfte:"* / *"Som statsminister kommer Ulf Kristersson att leda Sverige in i Nato under nästa mandatperiod."* *gloss:* M's promise: as prime minister, Ulf Kristersson will lead Sweden into NATO during the next term. Kristersson, TT via DI, [X-I1]: *"Jag går till val på att leda nästa regering efter valet."* *gloss:* I am going into the election on leading the next government. Same report: *"Men Kristersson står fast vid att det är han som ska bli statsminister om högersidan vinner."* *gloss:* but Kristersson holds that he is the one who will become prime minister if the right-hand side wins. SVT, [C-I1]: *"där M-ledaren Ulf Kristersson är statsministerkandidat"* *gloss:* where M leader Ulf Kristersson is the PM candidate (SVT on the non-socialist parties). |
| **SD** | **(c) GAP** | — | — | [SD-P1], [SD-P1b] primary (the speech names no candidate); [SD-I1], [SD-I2] older vintages, context only | **No pre-election declaration naming Åkesson, or anyone else, as SD's PM candidate was found in any source.** SD's leader gave the traditional summer speech (*"Det traditionsenliga sommartalet"*) in Sölvesborg on 13 August 2022, and SD issued the full text as a press release [SD-P1b]. It names no PM candidate and never uses the word *statsminister*. On government formation it says: *"...när vi sätter oss i regeringsförhandlingar efter den 11 september."* *gloss:* when we sit down to government negotiations after 11 September. It also asks for the helm, without naming a post or a person to hold it: *"Sätt oss vid rodret, så ska vi styra detta stolta och vackra... ...men ack så kantstötta och sargade skepp... ...mot en trygg och välmående framtid."* *gloss:* put us at the helm and we will steer this proud and beautiful, but oh so battered and wounded, ship towards a safe and prosperous future. And, appealing to undecided voters: *"Ge oss den chansen... ...ge mig den chansen, så lovar jag... ...att jag och mitt parti ska göra ALLT för att du inte ska ångra dig."* *gloss:* give us that chance, give me that chance, and I promise that my party and I will do EVERYTHING so that you will not regret it. This is a claim to govern, not a declared prime-ministerial candidacy: no office and no candidate are named, so the GAP stands. It is recorded so the classification can be weighed against S, whose (a) rests on leadership framing plus her sitting office as the party names it. The two older statements are context, not 2022 declarations. 2021-04-13, SVT [SD-I1], Åkesson: *"...kommer jag inte gå in på vilka krav vi kommer ställa på att släppa fram en statsministerkandidat, budget eller allmänt ingå i regeringsunderlag"* *gloss:* I will not go into what we will demand for letting a PM candidate through, a budget, or joining a governing base. 2019-10-25, SVT [SD-I2]: *"Jag går inte runt och säger att jag vill bli statsminister."* *gloss:* I do not go around saying I want to be prime minister. |
| **V** | **(c) GAP** | — | — | [V-I1] secondary, context only | **No saved 2022 statement from V names any PM candidate, its own leader or another party's.** SVT, 2022-08-15 [V-I1], reports what V asks in return for support. It is not a candidacy: *"Ett upplägg där V ger stöd utan att få något tillbaka är inte aktuellt, framhåller Dadgostar."* *gloss:* an arrangement where V gives support and gets nothing back is not on the table. |
| **C** | **(b) NO: another party's leader** | Magdalena Andersson (S) | 2022-08-15 (SVT, citing Lööf's DN interview; page last updated 2022-08-19) | [C-I1] secondary; no C primary found (**GAP**) | SVT: *"Centern kan tänka sig att ingå i en S-ledd regering, säger C-ledaren Annie Lööf som också beskriver Magdalena Andersson (S) som den bästa statsministerkandidaten."* Lööf: *"– Jag ser att Magdalena Andersson har det ledarskap som behövs, säger hon."* *gloss:* C could consider joining an S-led government, and Lööf calls Andersson the best PM candidate; "I see that Magdalena Andersson has the leadership that is needed." |
| **KD** | **(b) NO: another party's leader** | Ulf Kristersson (M) | 2022-08-07 (speech; page modified 2022-08-12; the sentence stands in the 2022-08-20 capture) | [KD-P1], [KD-P1a] primary | Ebba Busch, summer speech on KD's site: *"Det finns bara ett regeringsunderlag som har en plan för Sverige. Och de är de fyra partier - vars statsministerkandidat är Ulf Kristersson och - som står för en Ny Start för Sverige."* *gloss:* there is only one governing base with a plan for Sweden: the four parties whose PM candidate is Ulf Kristersson and who stand for a New Start for Sweden. ⚠ This is **KD's** statement. It speaks for "the four parties", which the speech does not name (SD is not mentioned in it; only 8 Sidor [SD-C1], after the election, lists Kristersson's side as M, SD, KD and L). It is **not any other party's own declaration** and is not counted for SD. |
| **L** | **(b) NO: another party's leader** | Ulf Kristersson (M) | reported 2022-09-07 (said "i TV4"; the page dates only the Kristersson/Andersson interviews to Tuesday evening, 6 September) | [X-I1] secondary; no L primary found (**GAP**) | Johan Pehrson, TT via DI: *"Ulf Kristersson kommer att vara vår statsministerkandidat alla dagar i veckan. Vi måste nu kämpa gata för gata för att se till att vi får ytterligare några mandat"* *gloss:* Ulf Kristersson will be our PM candidate every day of the week; we must now fight street by street for a few more seats. Same report: *"Pehrson står fast vid att L inte kan släppa fram en regering där SD ingår."* *gloss:* L cannot let through a government that includes SD. |
| **MP** | **(b) NO: another party's leader** | Magdalena Andersson (S) | reported 2022-09-07 (Per Bolund, språkrör, 'i TV4') | [X-I1] secondary: TT's paraphrase, not Bolund's own words; no MP primary found (**GAP**); [MP-I1] context | TT via DI: *"Han gjorde tydligt att Magdalena Andersson är hans statsministerkandidat."* *gloss:* he made clear that Magdalena Andersson is his PM candidate. Bolund's own words in the same report: *"Det är MP som kommer att vara avgörande för vem som kan regera Sverige"*. *gloss:* it is MP that will decide who can govern Sweden. MP is led by two *språkrör*; see `party_leaders_2022.md`. Context: SVT, 2022-01-19 [MP-I1], reports MP's rule on joining a government. It is not a candidacy: *"– Det är de krav som vi ställer. Vi röstar nej till de regeringar som vi inte ingår i, sade Märta Stenevi."* *gloss:* those are our demands; we vote no to governments we are not part of. SVT's reading: *"Det betyder att Magdalena Andersson inte kan bli statsminister med stöd från Miljöpartiet, om de inte sitter med i hennes regering."* *gloss:* this means Magdalena Andersson cannot become prime minister with MP's support unless MP sits in her government. |

### Source register (PM candidacies, 2022 vintage)
(all accessed **2026-09-24**. Files are under `raw/declarations_2022/`. Dates are the page's own metadata or the
date printed on the page. Wayback timestamps are UTC.)

- [S-P1] https://www.socialdemokraterna.se/nyheter/nyheter/2022-08-04-s-slutspurtskampanj-handlar-om-ledarskap-for-sverige
  — **Socialdemokraterna (primary)**, "S slutspurtskampanj handlar om ledarskap för Sverige", published
  2022-08-04T13:05:49+02:00, modified 2022-08-04T13:09:52+02:00.
  `socialdemokraterna_20220804_slutspurt_ledarskap.html` sha256 `94f151aa5423d3ac064ef27098ec3f68b8a78ccccf33d8a95733af6929c14ace`
- [S-P1a] https://web.archive.org/web/20220813114950id_/https://www.socialdemokraterna.se/nyheter/nyheter/2022-08-04-s-slutspurtskampanj-handlar-om-ledarskap-for-sverige
  — Internet Archive capture of [S-P1] at **2022-08-13 11:49:50**; the quoted sentences stand in it.
  `wayback_20220813114950_socialdemokraterna_slutspurt.html` sha256 `dc5b7bbf67ba92dff1516e9cca00c69736cf263e727f2864746b509244fd9ecb`
- [M-P1] https://moderaterna.se/nyhet/valloften/ — **Moderaterna (primary)**, "Ulf Kristersson presenterade fem
  vallöften för att få ordning på Sverige", published 2022-03-26T10:37:52+00:00, modified 2022-03-26T11:07:09+00:00.
  `moderaterna_20220326_valloften.html` sha256 `3a74c699370c72ddcb3fcff49747f5a054a3277b04f6b7b776bcf73cd09aac57`
- [M-P1a] https://web.archive.org/web/20220910232056id_/https://moderaterna.se/nyhet/valloften/ — Internet Archive
  capture of [M-P1] at **2022-09-10 23:20:56**, the night before polling. The quoted sentence stands in it.
  `wayback_20220910232056_moderaterna_valloften.html` sha256 `61d66a7e4781b53873ab8dda836381b50eeffd27b74d50233b8c8107f28b231d`
- [KD-P1] https://kristdemokraterna.se/arkiv/nyheter/2022/2022-08-07-ebba-buschs-sommartal-2022 — **Kristdemokraterna
  (primary)**, "Ebba Buschs sommartal 2022", published 2022-08-07T14:35:02+02:00 (the page's `<time>` element; its rek:pubdate meta prints the same clock time with a Z suffix), modified 2022-08-12 ("Senast uppdaterad: 12 augusti 2022"; rek:moddate 2022-08-12T13:35:55.000Z).
  `kristdemokraterna_20220807_busch_sommartal.html` sha256 `93f5d258bbfaae6a45817ebf5548bb30910d4c8bea56903d075cfb75ffbf4e66`
- [KD-P1a] https://web.archive.org/web/20220820031033id_/https://kristdemokraterna.se/arkiv/nyheter/2022/2022-08-07-ebba-buschs-sommartal-2022
  — Internet Archive capture of [KD-P1] at **2022-08-20 03:10:33**; the quoted sentence stands in it.
  `wayback_20220820031033_kristdemokraterna_sommartal.html` sha256 `5a4ec70cef7fc5f1b97b8de9578535e561d52893187b9eb1a17522c509cac8f0`
- [SD-P1] https://via.tt.se/pressmeddelande/3328089/jimmie-akessons-sommartal-2022?publisherId=3236128 —
  **Sverigedemokraterna's own press release** (on TT's release wire), "Jimmie Åkessons sommartal 2022", dated
  *"13.8.2022 14:00:00 CEST"*, *"Sommartalet 2022 bifogas i detta pressutskick."* The page's sidebar lists
  SD's later releases, up to September 2026.
  `viatt_20220813_sd_sommartal_pressmeddelande.html` sha256 `654b2a3ad36dc0739ccc39cad7b4a6105c086d2d62a8936af32979725a6ac5ac`
- [SD-P1b] https://via.tt.se/data/attachments/00406/6c6917c6-d29e-483a-a204-f9e6b9006a29.pdf — the speech text
  attached to [SD-P1], marked *"Det talade ordet gäller"* (*gloss:* check against delivery). Its extracted
  text contains no *statsminister*, no *Kristersson* and no named PM candidate. PDF metadata CreationDate and ModDate 2022-08-13T13:10:44+02:00.
  `viatt_20220813_sd_sommartal_2022.pdf` sha256 `bbbd9f65d4b199e8975d4f8d0354b1b1cba38e3d74776821c52555858f5e85cb`
- [X-I1] https://web.archive.org/web/20220907052535id_/https://www.di.se/nyheter/inget-sd-i-kristerssons-regering-mitt-besked-ligger-fast/
  — **secondary**: Dagens Industri carrying TT, "Inget SD i Kristerssons regering: ”Mitt besked ligger fast”",
  published 2022-09-07T06:13:00+02:00. This is the Internet Archive capture at **2022-09-07 05:25:35**, four days before polling.
  It reports Kristersson's and Andersson's TV4 party-leader interviews on Tuesday evening (6 September) and undated TV4 remarks by Pehrson and Bolund.
  `wayback_20220907052535_di_tt_mitt_besked_ligger_fast.html` sha256 `5fd777748d24c4d602213d146b5153270544f3e8ef984d55a2da29bf642a9c51`
- [C-I1] https://www.svt.se/nyheter/inrikes/annie-loofs-besked-vill-sitta-i-s-ledd-regering — **secondary**: SVT,
  "Annie Lööf öppnar för att sitta i S-ledd regering", published 2022-08-15T21:35:32+02:00, updated
  2022-08-19T09:32:27+02:00. It cites Lööf's interview with DN.
  `svt_20220815_loof_s_ledd_regering.html` sha256 `98da0243c5590ec69dd8370837189b084eb34183f73139119c2f6cb5bec8c378`
- [V-I1] https://www.svt.se/nyheter/inrikes/dadgostar-1 — **secondary**: SVT, "Dadgostar om Lööfs regeringsutspel:
  ”Märkligt”", published 2022-08-15T22:35:26+02:00, updated 2022-08-19T13:14:41+02:00.
  `svt_20220815_dadgostar_markligt.html` sha256 `707d60417215d27faf1ffb223d0d8389b094bab2bdb794e4facd60e4c48a1364`
- [MP-I1] https://www.svt.se/nyheter/miljopartiets-budskap-till-andersson-vi-rostar-nej-till-de-regeringar-som-vi-inte-ingar-i
  — **secondary**: SVT, "Miljöpartiets budskap till Andersson: Vi röstar nej till de regeringar som vi inte
  ingår i", published 2022-01-19T16:58:59+01:00.
  `svt_20220119_mp_rostar_nej.html` sha256 `896905c93c33dbc42dfacd8dc7463be49e4e0b0c417636663484fe70f61aa121`
- [SD-I1] https://www.svt.se/nyheter/inrikes/sd-ger-besked-i-regeringsfragan — **secondary**, 2021 vintage: SVT, "SD
  ger inget besked i regeringsfrågan", published 2021-04-13T09:00:18+02:00, updated 2021-04-22T11:04:47+02:00.
  `svt_20210413_sd_inget_besked_regeringsfragan.html` sha256 `fc43ef0bf58f63d3d089b17daa2874c39e265d29b235f32e99091737537b9cae`
- [SD-I2] https://www.svt.se/nyheter/inrikes/akesson-i-morgonstudion-jag-ar-sa-redo-jag-kan-bli-att-bli-statsminister
  — **secondary**, 2019 vintage: SVT, "Åkesson om att bli statsminister: ”Är så redo jag skulle kunna vara”",
  published 2019-10-25T09:37:58+02:00, updated 2019-10-25T20:08:44+02:00.
  `svt_20191025_akesson_sa_redo.html` sha256 `5a74c51261633425818fb0cfe3b3f550925b778b6058e53aad10c699c1484dad`
- [SD-C1] https://8sidor.se/sverige/2022/09/han-blir-inte-statsminister/ — **secondary of secondary, and AFTER the
  election**: 8 Sidor, "Han blir inte statsminister", dated *"15 september 2022"*, citing Expressen.
  Context only (see below).
  `8sidor_20220915_han_blir_inte_statsminister.html` sha256 `a586acda8defdfb784e14d0382e0a7ba251da2b0ffe6c964120032d462d4ffa9`

### Found in passing: recorded, not wired, not part of the candidacy table

- **SD after the election [SD-C1].** On 2022-09-15, four days after polling, 8 Sidor, citing Expressen, wrote:
  *"Det är bara Sverigedemokraterna / som vill att Åkesson / blir statsminister."* *gloss:* only the
  Sweden Democrats want Åkesson to become prime minister. The page gives no source from SD itself, and the report
  **postdates 11 September**. Under the standing rule it is **not a 2022 pre-election declaration**, so it
  does not fill SD's GAP. It is flagged because a dated, pre-election SD source saying the same thing
  would move SD from (c) to (a).
- **MP's participation rule, 2022 vintage [MP-I1].** 2022-01-19: *"Vi röstar nej till de regeringar som vi
  inte ingår i"*. This has the same shape as the V line the ruling gives a mirror. It is recorded here only
  because it was found while checking MP's candidacy. It is **not wired**, and nothing here asks for it to be.
  (Measured since, at K-1f's wiring, §607: wired, it would form an S minority on 2022's chamber (107, opposed 86)
  ahead of M+KD+L, so the backtest's record rests on it staying unwired. The source is **secondary**: SVT's report
  of Stenevi's words. Filed as K-1g for Elias.)
- **V's mirror-shaped line, 2022 vintage: GAP.** The ruling's V rule ("refuses to support a cabinet it
  is not in") was **not found in a 2022 source saved this run**. V's 2022 statement [V-I1] is weaker: support
  in exchange for something, not support only from inside the cabinet. Two further V statements, both reported in SVT pages published **2022-08-15** and updated 2022-08-19 (the tweet's own date is not printed; both are before polling day either way) and both **secondary** (SVT quoting V's leader Nooshi Dadgostar), are in the saved pages.
  [C-I1], Dadgostar on Twitter: *"Inte kan hon väl tro att hon skulle sitta i en regering på våra mandat men
  inte samarbeta med oss? Nej, Sverige behöver en majoritetsregering, då ingår V om SD ska hållas borta från
  inflytande"* *gloss:* surely she cannot think she would sit in a government on our seats without cooperating
  with us? No, Sweden needs a majority government, in which V is included if SD is to be kept from influence.
  [V-I1]: *"– Hon tycks begära vårt stöd för att sitta i en regering utan att vilja samarbeta med oss, säger hon
  i Aktuellt."* *gloss:* she seems to be asking for our support to sit in a government without wanting to
  cooperate with us, she says on Aktuellt. V demanded a place in a majority government. No saved 2022 source
  states the ruled form "refuses to support a cabinet it is not in", so the ruled form stays a GAP for 2022.
  Because every election reads its own date's declarations, the 2022
  backtest cannot borrow the 2026 wording for V.

### GAPs: stated, not filled

- **SD**: no dated pre-election declaration of a PM candidate, its own leader or another's (see above).
- **V**: no 2022 statement naming any PM candidate.
- **MP**: the named candidate (Andersson, another party's leader) rests only on TT's paraphrase in [X-I1]. No MP primary naming a candidate was found.
- **C, L**: the named candidate (another party's leader) is sourced only from the press. No C or L primary
  naming a candidate was found.
- **S**: the party's own channel frames Andersson as *ledarskap* and as sitting *statsminister*, never in the words "PM candidate"; see the S row.

## FOR THE FORMATION MODEL (K-1f, 2022 vintage)

**Own-leader candidacies sourced for the 2022 election, in class (a) and so subject to the ruling:**

| party | declared candidate | vintage | basis |
|---|---|---|---|
| **S** | Magdalena Andersson (S) | 2022-08-04 → 2022-09-07 | [S-P1]/[S-P1a] primary (wording *ledarskap*; names her sitting *statsminister*); [X-I1], [C-I1] press |
| **M** | Ulf Kristersson (M) | 2022-03-26 → 2022-09-10 | [M-P1]/[M-P1a] primary (*"Som statsminister kommer Ulf Kristersson..."*); [X-I1] his own words |

- **Not own-leader, class (b):** C → Andersson (S); KD → Kristersson (M); L → Kristersson (M); MP → Andersson (S). As the
  ruling is worded, these four declarations **do not** trigger the refusal. They are recorded so the wiring pass can see
  them, not so it acts on them.
- **GAP, class (c):** SD, V. These parties carry **no** candidacy constraint in the 2022 vintage because none is sourced.
  The model should not supply one.
- As sourced, this section added facts only and claimed nothing about what the formation would produce with these
  constraints held; that was left for the wiring pass to measure, and the next bullet records what it measured.
- **As wired (K-1f, §607):** the pair is two `RedLine.OneWay` lines, S → M and M → S, in `DeclaredRedLines.For(...,
  ElectionVintage.Sweden2022)`, generated from `DeclaredRedLines.Candidacies`. Measured on 2022's chamber
  (`Formation2026Diagnostic`, log `k1_607b_formation`), it changes nothing: M+KD+L carried by SD with the pair or
  without it. It moves CoalitionHarness 4c's counterfactual (the Tidö lines dropped) from S+M+C+KD+L to SD+M+KD+L:
  see the table *What each declaration actually does*.
