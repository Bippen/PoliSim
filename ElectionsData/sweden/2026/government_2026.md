# Sweden — the opening of the 2026–2030 Riksdag term and the sitting government [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; K-1 part 4 sourcing pass 2026-09-23, research agent). `[PROVISIONAL]` until a second
session re-verifies (R-K9), and for the government itself until the Riksdag's prime-minister vote is on record.

**What this file is for.** The game's **day-one government** for the 2026 start is **the formation model's result on
the 2026 chamber** (`returns_2026.md` for the seats, `coalition_declarations_2026.md` for the declared lines). It stays
**provisional until the Riksdag's vote is on record**. This file gives that result its dated frame: when the new Riksdag
convenes, which constitutional rule governs the prime-minister vote, and what the real government's status was on
2026-09-23. It does not name a winner, because no fetched page records one. Election day was 2026-09-13; the result was
fixed on 2026-09-19 (`returns_2026.md`). `returns_2022.md` and the 2022 files are not touched.

**Fetched, never recalled.** Every quote below was read in a page fetched on 2026-09-23 (evening, CEST; the
data.riksdagen.se responses stamp themselves 23:27:50–23:32:45: RSS lastBuildDate 23:27:50 [RD-RSS2] / 23:28:52 [RD-RSS1];
dokumentlista 23:31:01–23:32:45; the voteringlista response carries no stamp) and saved byte for byte under `raw/government/`. Each
file's SHA-256 is in `raw/government/SHA256SUMS.txt` and in the register below. Quotes are Swedish and verbatim. They were
checked against the saved bytes by a script (tags stripped, entities decoded, whitespace collapsed). Every block quote
and every inline-quoted source string in this file was found in the saved files. English glosses are marked *gloss:* and are mine, not the source's. Lines marked **DERIVED** state their
arithmetic or reading.

## FOR THE FORMATION MODEL (the dated frame, 2026 vintage)

| date (2026) | event | status on 2026-09-23 | source ids |
|---|---|---|---|
| Thu 17 Sep | PM Ulf Kristersson (M) asks to be dismissed. The talman dismisses him and every other minister, and they stay on as a caretaker government (*övergångsregering*) | **on record** | [RD-N17a] [RD-N17b] [RG-ART] [RG-START] |
| Fri 18 Sep | Talman Andreas Norlén consults the other seven parties (M's consultation took place when the PM asked to go), then gives Magdalena Andersson (S) a sounding mandate (*sonderingsuppdrag*), to report to the **new** Riksdag's talman | **on record**; no report on record | [RD-N17b] [RD-N18] |
| Mon 28 Sep, 11.00 | the new Riksdag convenes (roll-call, *upprop*) and elects a talman and three vice talmän. The old Riksdag sits until then | scheduled | [RD-VAL] [RD-N19] [RD-N21] [RD-RMO] [RD-FAQ] [RD-POST] [RD-KAL]; rule: RF 3 kap. 10 § [RF-R] [RF-L] |
| Tue 29 Sep, 14.00 | opening of the riksmöte 2026/27 | scheduled | [RD-VAL] [RD-N19] [RD-RMO] [RD-KAL] |
| Wed 30 Sep | the Riksdag decides the number of committee seats | scheduled | [RD-VAL] |
| Tue 6 Oct | committees, EU-nämnden, riksdagsstyrelsen and Utrikesnämnden elected | scheduled | [RD-VAL] [RD-POST] |
| after the opening on Tue 29 Sep (week 40), at the earliest | the earliest point at which the Riksdag can elect a new PM: *"efter riksmötets öppnande under vecka 40"* | **no date on record** | [RG-ART] |
| — | the prime-minister vote (the talman's proposal, RF 6 kap. 4 §) | **not held** as of 23 Sep (**DERIVED**, see Fact 4); **no date on record** | not held: [RG-ART] [RD-N18] [RD-N21] (Fact 4); open-data corroboration [OD-VOT] [OD-SMO] [OD-FTS] [OD-PROT] (weak, see Fact 4); no date: [RD-KAL] lists none (see the note below) |

Note on the last row (**DERIVED**: a reading of the query parameters and the saved responses): the [OD-SMO], [OD-PROT] and [OD-FTS] searches cover documents dated up to `tom=2026-09-23` only, so
they cannot see a vote scheduled for a later date (the 2022 control's kam-zz item "Prövning av förslag till statsminister"
is dated 2022-10-17 but has systemdatum 2022-10-14 [OD-SMO-C]); [OD-VOT] records votes held, not scheduled. [RD-KAL]
also omits the 30 Sep and 6 Oct chamber decisions that [RD-VAL] dates, so its silence does not show that nothing is
scheduled.

Wiring notes (for whoever wires K-1 part 4):
- **Which vote applies.** The post-election confidence vote on the sitting PM (RF 6 kap. 3 §) is **not held** when the
  PM has already been dismissed (6:3, second paragraph), and the talman dismissed Kristersson on 17 Sep. The prime
  minister is therefore chosen by the talman's-proposal procedure of 6 kap. 4–5 §§. Under it a proposal is rejected only
  if **more than half of the members (at least 175 of 349, as riksdagen.se puts it)** vote against, the vote falls within
  four days of the proposal, and the talman gets four proposals before an extra election. **DERIVED** reading of the
  quoted statute and the dated facts. riksdagen.se's own scenario page describes the same path for a PM who resigns
  ([RD-SCN] scenario 2).
- **No clock on the first proposal.** 6 kap. 4–5 §§ as fetched set no date by which the talman must make the first
  proposal. They set the four-day vote window per proposal and the limit of four rejections. The two-week deadline of
  6 kap. 3 § (**DERIVED**: 28 Sep + 14 days = 12 Oct 2026) belongs to the 6:3 vote, which is not held (see above).
- **What governs until the vote.** A caretaker Kristersson government governs until then. It was M with KD and L as of
  17 March 2026 [RG-SMA] (see GAP G5 for today's composition). It is not a result of the 2026 chamber. It
  *"fattar främst beslut i löpande eller brådskande ärenden"* and cannot call an extra election ([RG-ART]; RF 3 kap.
  11 §, 6 kap. 11 § [RF-R]). If the game's day one is set before the vote, the real-world government on that day is this
  caretaker cabinet, and the formation model's result is the one flagged PROVISIONAL.
- **2022 precedent (context, not a 2026 fact)** [RD-TRB]: resignation request 15 Sep 2022, sounding mandate 19 Sep 2022,
  proposal approved 17 Oct 2022. **DERIVED**: 32 days from the 2022 resignation request to the vote.

## Source register
(all accessed 2026-09-23; saved as `raw/government/<file>`; "page's own date" is the date the page itself shows, or its
metadata where the page shows none, as marked)

| id | URL | publisher | page's own date | basis | file | bytes | SHA-256 |
|---|---|---|---|---|---|---|---|
| [RF-R] | https://www.riksdagen.se/sv/dokument-och-lagar/dokument/svensk-forfattningssamling/kungorelse-1974152-om-beslutad-ny-regeringsform_sfs-1974-152/ | Sveriges riksdag (text: Regeringskansliet, "Fulltext (Regeringskansliet)") | SFS 1974:152, "Utfärdad: 1974-02-28", "Ändrad: t.o.m. SFS 2022:1600" | consolidated statute text t.o.m. SFS 2022:1600 | `riksdagen_sfs-1974-152_regeringsformen.html` | 572346 | `d1c2cb60b02b9769be6c30d533b9243728b6d32f22eeae2c9c2a880fecc0b9d6` |
| [RF-L] | https://lagen.nu/1974:152 | lagen.nu (source: beta.rkrattsbaser.gov.se) | "Ändring införd t.o.m. SFS 2022:1600" | consolidated statute text t.o.m. SFS 2022:1600 | `lagennu_1974-152_regeringsformen.html` | 8928449 | `4e8e0f4bddcc524665923c9cbf00afd2e25bb8f02cf68786d62dab0b9b480a4a` |
| [RD-VAL] | https://www.riksdagen.se/sv/aktuellt/valet-2026/ | Sveriges riksdag | no page date shown (its embedded news items: 19 and 21 Sep 2026) | Riksdag explainer page | `riksdagen_valet-2026.html` | 324501 | `b530c12ecfc03bb4489012a6fc891eb8bd66ee81c150e449f2e413ac06f10e20` |
| [RD-N19] | https://www.riksdagen.se/sv/aktuellt/aktuelltnotiser/2026/sep/19/den-nya-riksdagen-efter-valet_cmsad780a37-f1ae-4d47-a4a7-b6ea2a69a21esv/ | Sveriges riksdag | "Publicerad: Lördag 19 september 2026 klockan 21.40"; "Uppdaterad: Måndag 21 september 2026 klockan 13.16" | official Riksdag news notice | `riksdagen_20260919_den-nya-riksdagen-efter-valet.html` | 222180 | `dd39a5d88bd49a4d90820287405123ee68861197d673a955a67019c122fcbfe0` |
| [RD-N21] | https://www.riksdagen.se/sv/aktuellt/aktuelltnotiser/2026/sep/21/invalda-ledamoter-efter-valet-2026_cmsb5703984-9f71-4b8b-9ab1-dbf921381a77sv/ | Sveriges riksdag | "Publicerad: Måndag 21 september 2026 klockan 14.55" | official Riksdag news notice | `riksdagen_20260921_invalda-ledamoter-efter-valet-2026.html` | 269889 | `c70275f151d1f3517acd7cacbf50269e4b5b8dea5ca38c41b2b4177ad50ba6e7` |
| [RD-RMO] | https://www.riksdagen.se/sv/aktuellt/riksmotets-oppnande/ | Sveriges riksdag | "Publicerad 9 juni 2025", "Uppdaterad 8 juni 2026" | Riksdag explainer page | `riksdagen_riksmotets-oppnande-2026.html` | 280911 | `5a1fd75241f1e1cd694afed05d5ae2a868e90c5dd7677782806f07b8196d7c3b` |
| [RD-FAQ] | https://www.riksdagen.se/sv/aktuellt/valet-2026/vanliga-fragor-och-svar-om-valet/ | Sveriges riksdag | "Publicerad 5 maj 2026", "Uppdaterad 5 maj 2026" | Riksdag explainer page | `riksdagen_valet-2026_vanliga-fragor-och-svar.html` | 273506 | `bd1f2af42a73b79ccd4dd18bbd48f9528c342cdbde8ad2d45b358b3c136fb691` |
| [RD-SCN] | https://www.riksdagen.se/sv/aktuellt/valet-2026/tre-scenarier-som-kan-uppsta-efter-valet/ | Sveriges riksdag | "Publicerad 18 juni 2026" (before the election) | Riksdag explainer page | `riksdagen_valet-2026_tre-scenarier.html` | 236864 | `0cde0fafa60e396b9b9480204ff69a6d7ae3775de1d51c53991e10c500352389` |
| [RD-POST] | https://www.riksdagen.se/sv/aktuellt/valet-2026/val-till-olika-poster-i-riksdagen-efter-ett-riksdagsval/ | Sveriges riksdag | "Publicerad 18 juni 2026" | Riksdag explainer page | `riksdagen_valet-2026_val-till-olika-poster.html` | 240031 | `27f8413ca84847d1e0bb9858fd5a43054ff69c5eeac146ae6e6da5f899974135` |
| [RD-SBR] | https://www.riksdagen.se/sv/sa-fungerar-riksdagen/demokrati/sa-bildas-regeringen/ | Sveriges riksdag | "Publicerad 27 april 2023" | Riksdag explainer page | `riksdagen_sa-bildas-regeringen.html` | 404277 | `660d05a9a08ce795146efcd38bb8263841088cf1eea25a286a5c21c1234050ca` |
| [RD-TRB] | https://www.riksdagen.se/sv/sa-fungerar-riksdagen/demokrati/sa-bildas-regeringen/tidigare-regeringsbildningar-och-statsministrar/ | Sveriges riksdag | "Publicerad 27 april 2023" | Riksdag explainer page | `riksdagen_tidigare-regeringsbildningar-och-statsministrar.html` | 409095 | `439b57a64668a2cf5ddec49566bec236c95b149c64d8eef56c019d566f8de23c` |
| [RD-N17a] | https://www.riksdagen.se/sv/aktuellt/aktuelltnotiser/2026/sep/17/talmannen-inleder-process-for-regeringsbildning_cms6291f93c-a728-4498-a0b9-e30f36627e3fsv/ | Sveriges riksdag | "Publicerad: Torsdag 17 september 2026 klockan 14.41" | official Riksdag news notice | `riksdagen_20260917_talmannen-inleder-process-for-regeringsbildning.html` | 213628 | `d4f111cf031360dc3e4baba368823207f02da65823fa2a4b30b227cddf7a36c9` |
| [RD-N17b] | https://www.riksdagen.se/sv/aktuellt/aktuelltnotiser/2026/sep/17/talmannen-traffar-partiforetradare_cmse079b81f-1751-4f2b-9207-259be4dc8450sv/ | Sveriges riksdag | "Publicerad: Torsdag 17 september 2026 klockan 19.57" | official Riksdag news notice | `riksdagen_20260917_talmannen-traffar-partiforetradare.html` | 216262 | `ce64158830cc2ca25f1d9dfdb41a48edac392a255ee7e364a7016850f0eced5b` |
| [RD-N18] | https://www.riksdagen.se/sv/aktuellt/aktuelltnotiser/2026/sep/18/talmannen-ger-sonderingsuppdrag-till-magdalena_cmse396c91a-ef46-4f54-88c6-10a144af63fcsv/ | Sveriges riksdag | "Publicerad: Fredag 18 september 2026 klockan 15.41" | official Riksdag news notice | `riksdagen_20260918_talmannen-ger-sonderingsuppdrag-till-magdalena-andersson.html` | 214386 | `321ea0bb86d4c9deceafa0dde8437d104ba9db1adb7e6c93957a2b6d131bed8c` |
| [RD-VIL] | https://www.riksdagen.se/sv/aktuellt/aktuelltnotiser/2026/juni/16/vilande-grundlagsandringar_cms2d688979-b1a3-4158-90eb-f6bf86a98087sv/ | Sveriges riksdag | "Publicerad: Tisdag 16 juni 2026 klockan 13.50" | official Riksdag news notice | `riksdagen_20260616_vilande-grundlagsandringar.html` | 229918 | `75d326e7102ce39d1c323baa481025bd1a8a587fb59c396559f3038153852a4b` |
| [RD-KAL] | https://www.riksdagen.se/sv/aktuellt/kalendersida/?from=2026-09-24&tom=2026-10-31 | Sveriges riksdag | live calendar, 24 Sep–31 Oct 2026 as served | live calendar as served 23 Sep | `riksdagen_kalender_2026-09-24_2026-10-31.html` | 599921 | `15629e9e1c41d911763886edbdadcadbc885d58e3e62a8fe5ab56341b3dbf17b` |
| [RD-RSS1] | https://data.riksdagen.se/dokumentlista/?cmskategori=valet2026&avd=aktuellt&aktuelltnotistomdatum=1&lang=sv&utformat=rss&sort=datum&sortorder=desc | Sveriges riksdag (the "Valet 2026" news feed) | lastBuildDate "Wed, 23 Sep 2026 23:28:52 +0200"; newest item 21 Sep 2026 14:55:09 | news feed as served | `riksdagen_rss_valet2026.xml` | 5734 | `3c24055148592c017935cd37a3f4953cf4757b1b20840c834181ea478e5a613a` |
| [RD-RSS2] | https://data.riksdagen.se/dokumentlista/?cmskategori=startsida&avd=aktuellt&aktuelltnotistomdatum=1&lang=sv&utformat=rss&sort=datum&sortorder=desc | Sveriges riksdag (front-page news feed) | lastBuildDate "Wed, 23 Sep 2026 23:27:50 +0200"; newest item 22 Sep 2026 11:48:14 | news feed as served | `riksdagen_rss_startsida-aktuellt.xml` | 15483 | `233738d8d31cd97cbe1e2743d1fef40a3291e7263db6274ff55a803a4616492c` |
| [OD-VOT] | https://data.riksdagen.se/voteringlista/?rm=2026%2F27&bet=&punkt=&valkrets=&rost=&iid=&sz=500&utformat=xml&gruppering= | Sveriges riksdag, open data | as served: `villkor=": rm=2026/27 " antal="0"` | open-data API response as served | `datariksdagen_voteringlista_rm2026-27.xml` | 159 | `d64572de9fd871fe20b7f1fe479f48e9d8ad22c645cf0d425124de6491630e43` |
| [OD-SMO] | https://data.riksdagen.se/dokumentlista/?sok=statsministeromr%C3%B6stning&from=2026-09-13&tom=2026-09-23&sort=datum&sortorder=desc&utformat=xml&a=s | Sveriges riksdag, open data | served `datum="2026-09-23 23:32:45"`, `traffar="0"` | open-data API response as served | `datariksdagen_sok_statsministeromrostning_2026-09-13_2026-09-23.xml` | 367 | `bb6d69d98542d2d13b3749e348a169d502f1f4fb3f08ef88653fbb6419031a69` |
| [OD-SMO-C] | https://data.riksdagen.se/dokumentlista/?sok=statsministeromr%C3%B6stning&from=2022-09-01&tom=2022-10-31&sort=datum&sortorder=desc&utformat=xml&a=s | Sveriges riksdag, open data (**control**) | served `datum="2026-09-23 23:31:01"`, `traffar="3"` | open-data API response as served | `datariksdagen_sok_statsministeromrostning_2022-09-01_2022-10-31_CONTROL.xml` | 29376 | `87d4039810985e3ca0936e7eba9299de03876c688fa5f76782aae00a6e64628a` |
| [OD-PROT] | https://data.riksdagen.se/dokumentlista/?doktyp=prot&from=2026-09-13&tom=2026-09-23&sort=datum&sortorder=desc&utformat=xml&a=s | Sveriges riksdag, open data | served `datum="2026-09-23 23:31:13"`, `traffar="0"` | open-data API response as served | `datariksdagen_prot_2026-09-13_2026-09-23.xml` | 346 | `e63bb05a694c3b894df9b222592bb02d6eff1f5401829090a0358c5f1a277f8d` |
| [OD-PROT-C] | https://data.riksdagen.se/dokumentlista/?doktyp=prot&from=2022-09-26&tom=2022-10-17&sort=datum&sortorder=desc&utformat=xml&a=s | Sveriges riksdag, open data (**control**) | served `datum="2026-09-23 23:32:45"`, `traffar="9"` | open-data API response as served | `datariksdagen_prot_2022-09-26_2022-10-17_CONTROL.xml` | 28319 | `debe27f8c56a4102ebcbb9ef8710cd0663015610c1eea073bdc1bf629b189b53` |
| [OD-FTS] | https://data.riksdagen.se/dokumentlista/?sok=f%C3%B6rslag+till+statsminister&from=2026-09-13&tom=2026-09-23&sort=datum&sortorder=desc&utformat=xml&a=s | Sveriges riksdag, open data | served `datum="2026-09-23 23:31:12"`, `traffar="6"` | open-data API response as served | `datariksdagen_sok_forslag-till-statsminister_2026-09-13_2026-09-23.xml` | 21181 | `4de815f21c83fc24d29cec18891c5304d888ed9427313784cbbf22fb9f37a7b1` |
| [RG-ART] | https://www.regeringen.se/artiklar/2026/09/statsministern-leder-en-overgangsregering/ | Regeringskansliet ("Artikel från Regeringskansliet") | "Publicerad 17 september 2026" (meta `published` 2026-09-17 16:55:20) | Regeringskansliet article | `regeringen_20260917_statsministern-leder-en-overgangsregering.html` | 198558 | `34c5daa208545db3af546b05011c8259a0ccf78e9de2ab4cef63ce141ecc90e4` |
| [RG-START] | https://www.regeringen.se/ | Regeringskansliet | live front page; top item dated "17 september 2026"; "Regeringsärenden vecka 39, 2026" dated 23 September 2026 | live page as served | `regeringen_startsida.html` | 283265 | `c5cd06b2cb39553fceb3aa049ef51576b83f23a02a1be0d1e8f44646332dbd56` |
| [RG-SOK] | https://www.regeringen.se/sokresultat/?query=%C3%B6verg%C3%A5ngsregering | Regeringskansliet (site search) | live; "Din sökning på övergångsregering gav 49 träffar" | live page as served | `regeringen_sokresultat_overgangsregering.html` | 260257 | `254d2b048fd581d0d61f56eb881f0080486552a4800f9c1fe24491ab05294d5c` |
| [RG-SMA] | https://www.regeringen.se/sa-styrs-sverige/statsministerambetet-i-sverige/ | Regeringskansliet | "Publicerad 17 mars 2026" (**before the election**) | explainer page, pre-election | `regeringen_statsministerambetet-i-sverige.html` | 311337 | `a447d21aac04a11614ad68108a54f520748e3a4bf8abc34291e4aac32ac8980b` |

## Fact 1 — when the newly elected Riksdag convenes

**The rule: Regeringsformen 3 kap. 10 §** (heading "Valperiod"), [RF-R]. The text is identical in [RF-L]:
> "Varje val gäller för tiden från det att den nyvalda riksdagen har samlats till dess den närmast därefter valda riksdagen samlas."
>
> "Den nyvalda riksdagen samlas på den femtonde dagen efter valdagen, dock tidigast på den fjärde dagen efter det att valresultatet har kungjorts. Lag (2010:1408)."

*gloss:* the newly elected Riksdag convenes on the fifteenth day after election day, but no earlier than the fourth day
after the result has been announced. **DERIVED**: 13 Sep 2026 + 15 days = **Monday 28 Sep 2026** ([RD-KAL] lists
"Måndag 28 september" under "Vecka 40"). riksdagen.se states the date outright (below), so the date does not rest on
the arithmetic.

**riksdagen.se's own statement of the date:**
- [RD-VAL], "Viktiga datum efter valet", the entry dated "28 september" and headed "Den nya riksdagen samlas":
  > "Den nyvalda riksdagen samlas till sitt första sammanträde. Vid sammanträdet genomförs ett upprop av riksdagsledamöterna. Efter uppropet väljer riksdagen talman och tre vice talmän."
- [RD-N19] (19 Sep, updated 21 Sep):
  > "Den nya riksdagen samlas vid uppropet måndagen den 28 september. Efter uppropet väljer riksdagen talman och vice talmän för valperioden 2026–2030. Ceremonin för riksmötets öppnande äger rum den 29 september."
- [RD-N21] (21 Sep):
  > "Den nya riksdagen börjar sitt arbete i samband med uppropet den 28 september. Fram till dess sitter den gamla riksdagen kvar."
- [RD-RMO]:
  > "Uppropet till riksdagen äger rum dagen före riksmötets öppnande, den 28 september klockan 11.00. Därefter väljer riksdagen talman och vice talmän samt anmäler vilka som blir partiernas gruppledare."
- [RD-KAL]: Monday 28 September, 11.00: "Upprop", with "Val av talman, vice talmän och riksdagsdirektör"; the same
  hour: "Allmänna motionstiden börjar".

## Fact 2 — the rule for the prime-minister vote after an election

**Correction to the brief:** the sentence that sets the **deadline** is in **6 kap. 3 §**, not 4 §. The rule that a
vote fails only on an absolute majority against (*negative parliamentarism*) appears twice. In **6 kap. 3 §** it
governs the post-election vote on a sitting PM. In **6 kap. 4 §** it governs the vote on the talman's proposal of a new
PM. All quotes are from [RF-R], consolidated through SFS 2022:1600. The same paragraphs, word for word, are in [RF-L].
Each paragraph carries "Lag (2010:1408)".

**6 kap. 3 §**, heading "Statsministeromröstning efter val" (the deadline):
> "En nyvald riksdag ska senast två veckor efter det att den samlats genom omröstning pröva frågan om statsministern har tillräckligt stöd i riksdagen. Om mer än hälften av riksdagens ledamöter röstar nej, ska statsministern entledigas."
>
> "Omröstningen ska inte hållas om statsministern redan har entledigats. Lag (2010:1408)."

**6 kap. 4 §**, heading "Regeringsbildningen" (the talman's proposal; elected unless more than half vote against):
> "När en statsminister ska utses, kallar talmannen företrädare för varje partigrupp inom riksdagen till samråd. Talmannen överlägger med vice talmännen och lämnar sedan förslag till riksdagen."
>
> "Riksdagen ska inom fyra dagar, utan beredning i utskott, pröva förslaget genom omröstning. Om mer än hälften av riksdagens ledamöter röstar mot förslaget, är det förkastat. I annat fall är det godkänt. Lag (2010:1408)."

**6 kap. 5 §** (four proposals, then an extra election):
> "Förkastar riksdagen talmannens förslag, ska förfarandet enligt 4 § upprepas. Har riksdagen fyra gånger förkastat talmannens förslag, ska förfarandet avbrytas och återupptas först sedan val till riksdagen har hållits. Om inte ordinarie val ändå ska hållas inom tre månader, ska extra val hållas inom samma tid. Lag (2010:1408)."

**6 kap. 11 §**, heading "Övergångsregering", and **3 kap. 11 §** (a caretaker cannot call an extra election):
> "Har regeringens samtliga ledamöter entledigats, uppehåller de sina befattningar till dess en ny regering har tillträtt."
>
> "Regeringen får inte heller besluta om extra val under den tid då dess ledamöter, efter det att samtliga har entledigats, uppehåller sina befattningar till dess en ny regering ska tillträda."

**6 kap. 12 §** (who acts if the talman cannot):
> "Om talmannen har förhinder övertar en vice talman de uppgifter som talmannen har enligt detta kapitel."

**riksdagen.se's reading of the same rules** [RD-SBR]:
> "Talmannens förslag till statsminister bordläggs en andra gång och ska därefter prövas av riksdagsledamöterna i kammaren i en omröstning senast på fjärde dagen efter den dag då förslaget lagts fram. Om mer än hälften av riksdagens ledamöter, det vill säga minst 175, röstar mot förslaget går det inte igenom. Annars är det godkänt."
>
> "Om statsministern väljer att sitta kvar efter ett val ska en statsministeromröstning hållas i riksdagen inom två veckor efter att den nyvalda riksdagen har samlats. Om mer än hälften av riksdagens ledamöter då röstar nej till statsministern måste statsministern avgå. I annat fall kan statsministern sitta kvar. Sedan valet 2014 är det obligatoriskt att det hålls en statsministeromröstning efter val."
>
> "När riksdagen har valt en ny talman är det denne som ska lägga fram förslag till ny statsminister för riksdagen. Men för att inte förlora tid förbereder talmannen från valperioden före valet regeringsskiftet genom att börja prata med partiledarna direkt efter att en regering har avgått eller entledigats. Talmannen överlämnar sedan arbetet med regeringsbildningen till den nya talmannen, som fortsätter arbetet."

**DERIVED:** the 6:3 deadline would be 28 Sep + 14 days = **12 Oct 2026**, but the PM was dismissed on 17 Sep (Fact 4),
so by 6:3's second paragraph that vote is not held. The live procedure is 6:4–6:5.

**Pending amendments** [RD-VIL]: the 2022–2026 Riksdag adopted, as pending (*vilande*), amendments to the
regeringsformen (among them KU2, KU8, KU32, KU34). They take effect only after a second decision by the new Riksdag:
> "För att ändringarna ska börja gälla krävs att riksdagen fattar ett beslut till med likadant innehåll under nästa valperiod."

**DERIVED**: the second decision must come from the new Riksdag, which first convenes on 28 Sep ([RD-N19], [RD-N21]).
So none of them was in force on 2026-09-23. Whether any of them touches 3 kap. 10–11 § or 6 kap. 3–5, 11 § is not
stated on that page (GAP G7).

## Fact 3 — riksdagen.se's own description of what happens after the 2026 election

**Roll-call and talman election, 28 September:**
- [RD-VAL] (quoted under Fact 1) and:
  > "I samband med uppropet utses en valberedning inför de interna valen i riksdagen. Vid sitt första sammanträde samma dag fastslår valberedningen förslag till antalet platser i utskotten och EU-nämnden. Riksdagen fattar beslut om förslaget den 30 september."
- [RD-FAQ]:
  > "Talmansvalet äger rum efter uppropet när den nya riksdagen samlats för första gången. Upprop och talmansval sker i år den 28 september."
- [RD-POST]:
  > "I år väljer riksdagen talman och vice talmän den 28 september."

**Opening of the riksmöte, 29 September:**
- [RD-VAL], the entry dated "29 september" and headed "Riksmötets öppnande":
  > "Ceremonin för riksmötets öppnande äger rum. Det markerar starten på riksdagens nya arbetsår, riksmötet 2026/27."
- [RD-RMO]:
  > "Riksmötets öppnande äger rum den 29 september 2026."
  >
  > "Klockan 14 inleds öppnandeceremonin i plenisalen På talmannens begäran förklarar H.M. Konungen riksmötet 2026/27 öppnat." *[sic: no full stop after "plenisalen" in the source]*
- [RD-FAQ]:
  > "Den nya riksdagen samlas vid uppropet måndagen den 28 september. Den dagen startar arbetsåret för den nya riksdagen, riksmötet 2026/27. Ceremonin för riksmötets öppnande äger rum den 29 september."

**Committees, 6 October:**
- [RD-VAL], the entry dated "6 oktober" and headed "Ledamöter väljs till utskotten":
  > "Riksdagen väljer ledamöter till bland annat de olika utskotten och EU-nämnden. Den här dagen väljs även ledamöter till riksdagsstyrelsen och Utrikesnämnden."
- [RD-POST]:
  > "I år väljer riksdagen ledamöter till utskotten och EU-nämnden den 6 oktober."

**The PM-vote window.** the fetched riksdagen.se pages give **no date** for the case that happened (the PM resigned). Their dated
sentences all belong to the cases where the PM stays:
- [RD-SCN] (published 18 June 2026, before the election). Scenario 1, where the PM stays and the Riksdag votes yes:
  > "Riksdagen ska då hålla en statsministeromröstning, senast två veckor efter att den nyvalda riksdagen har samlats. Som tidigast kan det ske den 29 september."

  Scenario 3, where the PM stays and the Riksdag votes no:
  > "Riksdagen håller statsministeromröstning, som tidigast sker tisdagen den 29 september."

  Scenario 2, where the PM resigns, is the path taken on 17 Sep. It gives no date:
  > "Det preliminära valresultatet visar att den sittande regeringen inte längre har stöd i riksdagen och statsministern väljer därför att avgå."

  The premise sentence is the page's pre-election hypothetical. No fetched page states why Kristersson asked to be
  dismissed; only the procedure that follows is the path taken.
  > "Talmannen inleder då arbetet med att ta fram ett förslag på en ny statsminister. Talmannen talar med samtliga partiledare, och föreslår en statsministerkandidat."
  >
  > "Om riksdagen har valt en ny talman tar denne över regeringsbildningen och lägger fram ett förslag om ny statsminister för riksdagen."
- [RD-FAQ]:
  > "När talmannen lagt fram ett förslag på statsminister ska riksdagsledamöterna rösta om förslaget senast fyra dagar därefter."
  >
  > "Om mer än hälften av riksdagsledamöterna, det vill säga minst 175, röstar mot förslaget går det inte igenom. Annars godkänns det."
- The only dated sentence on the window after the resignation in the fetched pages is the Government Offices', [RG-ART] (17 Sep):
  > "Riksdagen kan tidigast välja en ny statsminister efter riksmötets öppnande under vecka 40."

  **DERIVED**: week 40 of 2026 runs Mon 28 Sep–Sun 4 Oct ([RD-KAL] heads 28 Sep "Vecka 40"), and the opening is 29 Sep.

## Fact 4 — status on 2026-09-23: has a prime-minister vote been held?

**Answer: No** (**DERIVED**: no fetched page says so in terms; the reading rests on the dated statements under "No vote held" below). No prime-minister vote has been held in the new Riksdag, which has not yet convened. The Kristersson
government **has resigned**: the talman dismissed the PM and every other minister on 17 Sep 2026, at the PM's request.
It **serves as a caretaker government** (*övergångsregering*) until a new government takes office. The talman gave
Magdalena Andersson (S) a sounding mandate on 18 Sep [RD-N18]. No fetched page after 18 Sep reports on it (G2).

**The resignation and the caretaker status:**
- [RD-N17a] (17 Sep 2026, 14.41):
  > "Talmannen har i dag tagit emot begäran om entledigande från statsminister Ulf Kristersson (M). Mot bakgrund av detta inleder talmannen uppdraget att ta fram förslag till ny statsminister."
- [RD-N17b] (17 Sep 2026, 19.57):
  > "Ulf Kristersson (M) begärde den 17 september sitt entledigande som statsminister och talmannen har beslutat om detta. Talmannen har nu inlett uppdraget att ta fram förslag till ny statsminister."
  >
  > "Talmannen samtalar enskilt med företrädare för alla riksdagspartier utom Moderaterna, där samtalet ägde rum i samband med statsministerns begäran om entledigande."
  >
  > "Partiföreträdarna är kallade till samtal enligt partiernas storleksordning i den befintliga riksdagen som valdes 2022."
- [RG-ART] (Regeringskansliet, 17 Sep 2026):
  > "Statsministern har i dag begärt sitt entledigande, och därför har talmannen entledigat statsministern och övriga statsråd. De upprätthåller dock sina befattningar till dess en ny regering har tillträtt. Regeringen är därmed en övergångsregering."
  >
  > "En övergångsregering fattar främst beslut i löpande eller brådskande ärenden. Den enda uttryckliga begränsningen av en övergångsregerings befogenheter är att den inte får besluta om extra val."
- [RG-START] (the front page as served on 23 Sep) carries, under "Aktuellt från regeringen och Regeringskansliet", the
  item "Statsministern leder en övergångsregering" dated 17 September 2026:
  > "Statsministern begärde den 17 september sitt entledigande, och därför har talmannen entledigat statsministern och övriga statsråd. De upprätthåller dock sina befattningar till dess en ny regering har tillträtt. Regeringen är därmed en övergångsregering."

  The same page lists "Regeringsärenden vecka 39, 2026", dated 23 September 2026. **DERIVED**: the caretaker government
  was still handling cases on the access date. [RG-SOK]'s top hit for "övergångsregering" (relevance order; only the first
  page of the 49 hits was read) is the same 17 Sep article.
- [RD-SBR] (general rule):
  > "Om regeringen i stället avgår sitter den ändå kvar som en övergångsregering tills riksdagen har tagit ställning till vem som ska bilda ny regering och en ny regering har tillträtt. På så vis står landet aldrig utan regering."
  >
  > "En övergångsregering hanterar främst löpande och brådskande ärenden. Den får inte besluta om extra val och brukar enligt praxis inte ta nya politiska initiativ."
- Composition, **pre-election vintage** [RG-SMA] (17 Mar 2026):
  > "Ulf Kristersson tillträdde som statsminister 18 oktober 2022 och är Sveriges nuvarande statsminister. Han tillhör Moderaterna och leder en koalitionsregering där även Kristdemokraterna och Liberalerna ingår."

**The formation process so far:**
- [RD-N18] (18 Sep 2026, 15.41):
  > "Talman Andreas Norlén har i dag, efter samtal med företrädare för riksdagspartierna och en överläggning med de vice talmännen, gett Magdalena Andersson (S) i uppdrag att sondera förutsättningarna för att bilda regering."
  >
  > "– Magdalena Andersson har i dag fått mitt uppdrag att sondera förutsättningarna för att bilda en regering som tolereras av riksdagen. Hon ska rapportera sitt uppdrag till den nya riksdagens talman, säger Andreas Norlén."
  >
  > "Den nya riksdagens talman kommer därefter att informera om den fortsatta regeringsbildningen."
- Neither fetched riksdagen.se news feed has a formation item after 18 Sep. The newest "Valet 2026" item is 21 Sep, "Invalda
  ledamöter efter valet 2026" [RD-RSS1]. The newest front-page item is 22 Sep, about the UN General Assembly [RD-RSS2].

**No vote held: the direct evidence.**
- [RG-ART] (17 Sep): "Riksdagen kan tidigast välja en ny statsminister efter riksmötets öppnande under vecka 40." The
  opening is on 29 Sep (Fact 3).
- [RD-N18] (18 Sep): Andersson is to report her mandate to the **new** Riksdag's talman
  ("Hon ska rapportera sitt uppdrag till den nya riksdagens talman"), who is elected on 28 Sep (Fact 1).
- [RD-N21] (21 Sep), quoted in full under Fact 1:
  "Den nya riksdagen börjar sitt arbete i samband med uppropet den 28 september. Fram till dess sitter den gamla riksdagen kvar."

**Corroboration: the open-data searches.** The OD-PROT and OD-SMO searches are paired with 2022 controls. **DERIVED**:
the controls show the query shape works; they were fetched four years after the fact, so they do not test indexing lag.
OD-VOT has no saved control, and it only covers riksmöte 2026/27, which does not begin until 28 Sep ([RD-FAQ]).
- [OD-VOT]: the vote register for riksmöte 2026/27 returns `villkor=": rm=2026/27 " antal="0"`. **DERIVED** reading:
  no vote of any kind has been recorded in the new riksmöte.
- [OD-PROT]: chamber minutes (`doktyp=prot`) dated 13–23 Sep 2026 give `traffar="0"`: no minutes for 13–23 Sep
  2026 were indexed as of 23:31:13. **DERIVED** from the control: its systemdatum values run 21–28 days after eight
  of the nine 2022 sittings, but systemdatum is not shown to be the first-index time (the ninth, protocol 2022/23:6
  of 12 Oct, carries 2026-04-21). So the control does not measure how soon minutes are indexed, and this empty result does not by itself
  show that no sitting took place. Control [OD-PROT-C]: 26 Sep–17 Oct 2022
  gives `traffar="9"`, the newest being "Protokoll 2022/23:9 Måndagen den 17 oktober".
- [OD-SMO]: a full-text search for "statsministeromröstning" over 13–23 Sep 2026 gives `traffar="0"`. Control
  [OD-SMO-C]: the same search over 1 Sep–31 Oct 2022 gives `traffar="3"`, among them "Prövning av förslag till
  statsminister" dated 2022-10-17 11:00 (doktyp kam-zz).
- [OD-FTS]: a search for "förslag till statsminister" over 13–23 Sep 2026 gives `traffar="6"`. The hits are the two
  17 Sep talman notices, the talman's press conference of 18 Sep ("Pressträff med talman Andreas Norlén", doktyp
  sam-pk, listed twice), and two unrelated documents (a motions list and an EU-nämnden agenda). **None is a vote.**

**Scheduling (24 Sep–31 Oct), not holding:**
- [RD-KAL]: the calendar for 24 Sep–31 Oct 2026 lists the roll-call and talman election (28 Sep), the opening
  (29 Sep) and committee meetings. It lists **no prime-minister vote and no regeringsförklaring**; its filter counts
  show "Val (1)" and "Upprop (1)". It also omits the 30 Sep and 6 Oct chamber decisions that [RD-VAL] dates, so this
  silence does not show that nothing is scheduled (see the note under the table).

## GAPS

- **G1: the prime-minister vote has no date.** No fetched page sets a date for the talman's proposal or the vote. The
  only bound on record is "tidigast välja en ny statsminister efter riksmötets öppnande under vecka 40" [RG-ART], and [RD-KAL] shows nothing
  up to 31 Oct (weak evidence: the calendar also omits the 30 Sep and 6 Oct chamber decisions that [RD-VAL] dates).
- **G2: the sounding mandate has no reported outcome.** Andersson is to report to the **new** talman [RD-N18]. No fetched page after 18 Sep reports on it, and the talman's 18 Sep press conference (open-data document HDC220260918pk1, a web-tv
  item) was **not fetched or transcribed**. Only its search-result summary in [OD-FTS] was seen.
- **G3: the new talman is unknown.** The talman is elected on 28 Sep. The talman on 17-18 Sep was Andreas Norlén of the 2022 Riksdag ([RD-N17a], [RD-N18]); **DERIVED**: he is still talman on 23 Sep, since the old Riksdag sits until 28 Sep ([RD-N21]).
- **G4: the date Valmyndigheten announced the result (kungörande) was not fetched.** RF 3:10's floor ("tidigast på den
  fjärde dagen efter … kungjorts") is therefore not checked against a date. It is not load-bearing, because
  riksdagen.se states 28 Sep outright (Fact 1). `returns_2026.md` records the result fixed (fastställt) on 19 Sep.
- **G5: the caretaker cabinet's party composition on 23 Sep is not sourced.** The only party statement is [RG-SMA]
  (17 Mar 2026: M with KD and L). No fetched post-election page lists the ministers' parties. [RD-N17a] and [RD-N17b]
  label only Kristersson (M).
- **G6: the fetched riksdagen.se pages state no PM-vote window for the resignation case.** Their dated sentences (earliest vote 29 September)
  belong to the scenarios where the PM stays ([RD-SCN] 1 and 3), which did not occur.
- **G7: the content of the pending amendments was not read.** [RD-VIL] lists pending changes to the regeringsformen
  that the new Riksdag may adopt. Whether any touches 3 kap. 10–11 § or 6 kap. 3–5, 11 § was not read. KU's summary
  PDF ("Vilande grundlagsförslag som ska föreläggas riksdagen efter 2026 års val") was not fetched.
- **G8: the Riksdagsordningen was not fetched.** The Riksdag Act's rules on the first sitting, the talman election
  and the tabling (*bordläggning*) of the PM proposal are cited here only through riksdagen.se's summaries ([RD-SBR],
  [RD-RMO]).
- **G9: no primary statement by the PM was found on regeringen.se.** No press release or statement by Kristersson himself on
  the resignation was found. The only item is the Regeringskansliet article [RG-ART].
- **G10: second-session re-verification (R-K9) is pending**, as for every SOURCED file.

*(Filed 2026-09-23 by the K-1 part 4 sourcing agent. Nothing outside `ElectionsData/sweden/2026/government_2026.md` and
`ElectionsData/sweden/2026/raw/government/` was written. No code was touched and Unity was not run.)*
