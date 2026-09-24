# Sweden — the eight Riksdag parties' leaders at the 2026 election [SOURCED] [PROVISIONAL]

Class: **SOURCED** (§0.4: primary source + vintage + basis). K-1e, compiled 2026-09-24. `[PROVISIONAL]` until a
second session re-verifies (`ElectionsData/README.md`).

**Vintage: the 2026 election, 13 September 2026, and as of 2026-09-24.** Every name is read twice: in an Internet
Archive capture of the party's own page taken **on election day, 13 September 2026** (all captures fall between
11:05 and 17:13 UTC), and in the live page fetched on the access date **2026-09-24** (08:26–08:30 UTC; C's own page, added at K-1e's review, 11:24 UTC). **For all eight parties the two readings name the same person or persons with
the same office text**, compared on the saved bytes, office line by office line, for every page cited in both forms.
V's front page is the one pair that differs, and it is not the source for V's office (see the V row). `party_leaders_2022.md` is NOT
edited: it stays the record of 11 September 2022, and this file is the record of 13 September 2026. A leader, like a
declaration, is a dated fact, and an election reads the file of its own date.

**Basis, the same as 2022's.** Each name and office is taken from **the party's own website**. A party is the
authority on who leads it, and the election-day capture is what lets the citation speak for 13 September rather
than for today. No encyclopaedia and no news report is used: none was needed, because this year every party's own
site states the office in static HTML.

**What is SOURCED here is the NAME and the OFFICE. Nothing else.** No age, no biography, no attributes, no
relationships. Where the sentence quoted for the office also carries a date (V, KD, SD), the date is quoted and not
carried as a field; elisions are marked "…". The debate screen's `CandidateProfile` numbers
remain **`[AUTHORED-DRAFT]` game fiction**, as in 2022.

**Fetched, never recalled.** Every quote cited to a [PL-…] source was read in a page fetched on 2026-09-24 and saved
byte for byte under `raw/leaders/`. The quotes from the two cross-referenced pages are read from files already
SOURCED under `raw/declarations/`: [L-P2], accessed 2026-09-23, `liberalerna_landsmote_fornyat_fortroende.html` sha256
`66b3c6caab37b4b5fa3c68708a809e6b815dc5a2e8b2a4e6050af1a1315e3b6f`; and [C-P6], accessed 2026-09-24,
`centerpartiet_tal_tillsammans_mot_framtiden_installationstal_2025.html` sha256
`4d0140a3fbbb15b0b283616b387fd0d2e03e17223390907bbd10886abb532c96`; both are registered in
coalition_declarations_2026.md and raw/declarations/SHA256SUMS.txt. The 2022-side quotes are party_leaders_2022.md's.
Internet Archive captures were fetched with the `id_` flag, which serves the archived bytes
without the Wayback toolbar; five of them (PL-SD2a, PL-C1a, PL-V2a, PL-MP4a, PL-C3a) were served gzip-encoded and
the live front page PL-V2 brotli-encoded; all six are stored transfer-decoded (see `raw/leaders/fetch_log.txt`, which also records
each response's server date and archive headers; its per-file blocks carry each `Content-Encoding` header, and its
header comment, "Two captures were served with content-encoding gzip", undercounts: the count is the one given
here). SHA-256 values are in `raw/leaders/SHA256SUMS.txt` and in the register below. Quotes are Swedish and verbatim. English glosses are
marked *gloss:* and are mine, not the source's. Where a page sets the name and the office as separate elements (a
heading and the line under it), the quote joins them with " / " and says so.

| party | leader(s) at 2026-09-13 (unchanged at 2026-09-24) | office as the party names it | the party's own source (capture, page date, the page's own text) |
|---|---|---|---|
| **S** | Magdalena Andersson | *partiordförande* in its visible text and *Partiledare* in its structured data; the page uses both | `socialdemokraterna.se/vart-parti/vara-politiker/magdalena-andersson`, capture **2026-09-13 16:37:30 UTC** [PL-S1a], live [PL-S1]. The page's own date: first published 2021-11-04T16:26:24Z (`rek:pubdate`); last modified 2026-09-05T10:38:17Z (`rek:moddate`), the same in [PL-S1a] and [PL-S1]. Text: "Magdalena Andersson är Socialdemokraternas partiordförande." *gloss: Magdalena Andersson is the Social Democrats' party chair.* The same page's structured data (JSON-LD Person), identical in [PL-S1a] and [PL-S1], gives "jobTitle":"Partiledare". *gloss: job title: party leader.* |
| **SD** | Jimmie Åkesson | *partiordförande* on the party-board page and in his own page's metadata; *partiledare* in his own page's text; the party uses both | `sd.se/partistyrelse/`, capture **2026-09-13 17:13:45 UTC** [PL-SD1a], live [PL-SD1], dateModified 2025-11-24T12:30:06Z. Under the headings "Sverigedemokraternas partistyrelse 2025-2027" and "Presidium": "Jimmie Åkesson, Sölvesborg (Partiordförande)". *gloss: party chair.* And `sd.se/jimmie/`, capture **2026-09-13 11:05:52 UTC** [PL-SD2a], live [PL-SD2], dateModified 2026-06-12T03:42:59Z: "… till att år 2005 bli vald till Sverigedemokraternas partiledare." / "I dag har Jimmie Åkesson lett partiet i över 20 år." (two paragraphs) *gloss: … to being elected the Sweden Democrats' party leader in 2005. / Today Jimmie Åkesson has led the party for more than 20 years.* The same page's meta description (also og:description and the JSON-LD description), identical in [PL-SD2a] and [PL-SD2], opens: "År 2005 valdes Jimmie till partiordförande …" *gloss: in 2005 Jimmie was elected party chair …* |
| **M** | Ulf Kristersson | *partiledare* | `moderaterna.se` (the front page), capture **2026-09-13 17:02:21 UTC** [PL-M1a], live [PL-M1], dateModified 2026-09-11T19:28:03Z. A heading: "Ulf Kristersson, Moderaternas partiledare". *gloss: the Moderates' party leader.* His own page, `moderaterna.se/politiker/ulf-kristersson/`, capture 17:02:32 UTC [PL-M2a], live [PL-M2], dateModified 2026-09-01T16:29:39Z, shows the state office under his name: "Ulf Kristersson" / "Statsminister" (the heading and the line under it) *gloss: prime minister*; its own meta description (also og:description and the JSON-LD description), identical in [PL-M2a] and [PL-M2], names the party office: "Ulf Kristersson är partiledare för Moderaterna." *gloss: Ulf Kristersson is the Moderates' party leader.* |
| **V** | Nooshi Dadgostar | *partiledare* | `vansterpartiet.se/nooshi-dadgostar/`, capture **2026-09-13 14:01:56 UTC** [PL-V1a], live [PL-V1]. The page's own stamp: "Uppdaterad: 8 september 2025". Text: "Nooshi Dadgostar är Vänsterpartiets partiledare sedan den 31 oktober 2020." *gloss: Nooshi Dadgostar has been the Left Party's party leader since 31 October 2020.* The same sentence is on the live front page [PL-V2]. The front page's election-day capture [PL-V2a] does not carry it (it was the campaign front page). |
| **C** | Elisabeth Thand Ringqvist | *partiledare* on her own page (in its photographs' captions), on the party's about page and in its news; *partiordförande* on her CV subpage; the party uses both | Her own page, `centerpartiet.se/elisabeth-thand-ringqvist`, capture **2026-09-13 11:45:39 UTC** [PL-C4a], live [PL-C4], `rek:pubdate` 2026-01-27T23:17:13Z, `rek:moddate` 2026-09-09T10:45:29Z, the same in both: its heading is "Elisabeth Thand Ringqvist", and the page's only office word is in the captions (alt text) of its photographs, "Elisabeth Thand Ringqvist, partiledare Centerpartiet" and "Centerpartiets partiledare Elisabeth Thand Ringqvist i samtal under kampanjtillfälle"; it carries no *partiordförande*. *gloss: Elisabeth Thand Ringqvist, party leader, Centre Party / the Centre Party's party leader Elisabeth Thand Ringqvist in conversation at a campaign event.* (Added at K-1e's review: the CV page below is a child of this one in the site's own navigation.) `centerpartiet.se/elisabeth-thand-ringqvist/cv`, capture **2026-09-13 11:46:27 UTC** [PL-C1a], live [PL-C1], `rek:pubdate` 2026-02-17T16:24:00Z (on centerpartiet.se the `rek:` values are local time under a Z suffix; see the register's header): "Elisabeth Thand Ringqvist" / "Partiordförande Centerpartiet" (two lines of one paragraph). *gloss: party chair, Centre Party.* `centerpartiet.se/om-centerpartiet`, capture 16:45:05 UTC [PL-C2a], live [PL-C2], `rek:pubdate` 2025-12-03T00:25:06Z. A heading: "Centerpartiets partiledare Elisabeth Thand Ringqvist". *gloss: the Centre Party's party leader, Elisabeth Thand Ringqvist.* The party's news item of **2026-09-12**, the day before the vote (`rek:pubdate` 2026-09-12T17:40:24Z), capture 16:44:49 UTC [PL-C3a], live [PL-C3]: "… säger partiledare Elisabeth Thand Ringqvist." *gloss: … says party leader Elisabeth Thand Ringqvist.* |
| **KD** | Ebba Busch | *partiordförande* in the list of her posts and *partiledare* in her own first-person text; the party uses both | `kristdemokraterna.se/ebba`, capture **2026-09-13 17:09:39 UTC** [PL-KD1a], live [PL-KD1]. The page's own stamp: "Senast uppdaterad: 2 januari 2025" (`rek:pubdate` 2021-05-07T13:51:44Z). Under the heading "Uppdrag", a list: "Statsminister, vice" / "Statsråd" / "Partiordförande". *gloss: posts: deputy prime minister / cabinet minister / party chair.* And: "Jag har varit partiledare för Kristdemokraterna sedan april 2015 …" *gloss: I have been the Christian Democrats' party leader since April 2015 …* **This year the text is static, server-rendered HTML**, in both the capture and the live page (2022's gap is closed; see GAPS). |
| **L** | Simona Mohamsson | *partiledare* on her own page and *partiordförande* on the party's congress news page [L-P2]; the party uses both | `liberalerna.se/liberaler/simona-mohamsson`, capture **2026-09-13 16:57:17 UTC** [PL-L1a], live [PL-L1], dateModified 2026-06-04T08:04:35Z: "Simona Mohamsson" / "Partiledare, utbildnings- och integrationsminister" (the heading and the line under it). *gloss: party leader, minister for education and integration.* The party's own news page of 2026-03-22, already SOURCED as [L-P2] in `coalition_declarations_2026.md` (`raw/declarations/liberalerna_landsmote_fornyat_fortroende.html`; not re-captured here): "I dag beslutade Liberalernas landsmöte om att ge förnyat förtroende till Simona Mohamsson som partiordförande." *gloss: today the Liberals' national congress decided to give renewed confidence to Simona Mohamsson as party chair.* |
| **MP** | Amanda Lind **and** Daniel Helldén | *språkrör* (**two**, not one) | `mp.se/om/sprakror/`, capture **2026-09-13 14:24:50 UTC** [PL-MP1a], live [PL-MP1]. The page's own stamps: "Publicerad 2022-02-26", "Uppdaterad 2024-05-20". Text: "Miljöpartiet har två språkrör istället för en partiledare." *gloss: the Green Party has two spokespeople instead of one party leader.* And: "Just nu är Daniel Helldén och Amanda Lind språkrör för partiet." *gloss: at present Daniel Helldén and Amanda Lind are the party's spokespeople.* Each on their own page: `mp.se/om/amanda-lind/`, capture 14:55:32 UTC [PL-MP2a], live [PL-MP2]: "Amanda Lind" / "Språkrör". `mp.se/om/daniel-hellden/`, capture 15:03:28 UTC [PL-MP3a], live [PL-MP3]: "Daniel Helldén" / "Språkrör och riksdagsledamot i Utrikesnämnden och Krigsdelegationen." *gloss: spokesperson, and member of the Riksdag on the Advisory Council on Foreign Affairs and the War Delegation.* |

## The office word: five parties use two words

SD, C, KD and L each use both *partiordförande* and *partiledare* on their own pages (SD: *Partiordförande* on
[PL-SD1] and *partiordförande* in [PL-SD2]'s metadata, *partiledare* in [PL-SD2]'s text; C: [PL-C1] and
[PL-C2]/[PL-C3]/[PL-C4]; KD: [PL-KD1], which uses both on one page; L: *Partiledare* on [PL-L1], *partiordförande* on
[L-P2]). S uses *partiordförande* in its visible text (the h2 and the
sentence) and in its meta, og: and twitter: descriptions ([PL-S1a]/[PL-S1]), and the same page's structured data
(JSON-LD Person) gives "jobTitle":"Partiledare". M and V use *partiledare*. MP has
*språkrör*. This file records the words the pages use and does not choose between them. For the model's purpose, the
name of the person who leads the party, the two words do not conflict. Which word a party's **statutes** use is not
established here (see GAPS).

## MP: two språkrör, and the statutes re-read for 2026

MP's statutes were fetched again on 2026-09-24, in the election-day capture of **2026-09-13 14:25:00 UTC** [PL-MP4a]
and live [PL-MP4] (`mp.se/om/stadgar/`, "Publicerad 2022-03-09", "Uppdaterad 2025-11-12", the same in both). § 11,
word for word the same in both:

- "11.1 Kongressen väljer, före val av partistyrelse, två jämställda språkrör." *gloss: before electing the party
  board, the congress elects two equal spokespeople.*
- "11.2 Språkrören ska vara av olika kön." *gloss: the spokespeople shall be of different genders.*
- "11.4 Språkrörens uppgift är att företräda partiet och föra ut dess åsikter." *gloss: the spokespeople's task is to
  represent the party and carry its views.*

The word *debatt* does not occur anywhere in the statutes page, in the capture or in the live fetch. So the statutes still name no
one of the two for a debate, and C-D3's ruling (`party_leaders_2022.md`: carry both; if neither the statutes nor the
campaign materials resolve the seat, seat neither and say so) applies to 2026 unchanged, with the **2026 names**:
Amanda Lind and Daniel Helldén. The 2022 file records that its names are in the model (`PoliticalParty.Leaders`).
The model's roster was brought to this file by K-1e's build after this file was written (`PartySystem.SwedenParties`, `COMPLETED.md` §608).

## Changed since 2022 (compared with `party_leaders_2022.md`)

Three of the eight changed (C, L, MP, and MP in both seats). Five did not (S, SD, M, V, KD). Each claim rests on the
2022 file's capture on one side and on this file's pages on the other.

- **S: not changed.** Magdalena Andersson in 2022 (capture 2022-08-10, "partiordförande och Sveriges statsminister")
  and in 2026 ([PL-S1a], [PL-S1]: "Socialdemokraternas partiordförande"). The office text no longer adds "och Sveriges
  statsminister".
- **SD: not changed.** Jimmie Åkesson in both years. SD's own page states the continuity: "I dag har Jimmie Åkesson lett
  partiet i över 20 år" [PL-SD2a]. The 2022 file gave *partiledare*, from a page title. In 2026 the pages give
  *Partiordförande* [PL-SD1a] and *partiledare* [PL-SD2a].
- **M: not changed.** Ulf Kristersson in both years, *partiledare* in both ([PL-M1a]).
- **V: not changed.** Nooshi Dadgostar in both years. The 2026 capture carries, word for word, the sentence the 2022
  file quoted from its 2022-09-05 capture: "Nooshi Dadgostar är Vänsterpartiets partiledare sedan den 31 oktober 2020."
  ([PL-V1a]).
- **C: CHANGED.** Annie Lööf in 2022. Elisabeth Thand Ringqvist in 2026 ([PL-C1a], [PL-C2a], [PL-C3a]). As a
  cross-reference, the party's own page of her installation speech, "Elisabeth Thand Ringqvists Installationstal
  partistämman 2025" (the page dated 2026-01-27; the speech's date, 2025-11-13, rests there on [C-P5]), is already
  SOURCED on disk as [C-P6] in `coalition_declarations_2026.md`
  (`raw/declarations/centerpartiet_tal_tillsammans_mot_framtiden_installationstal_2025.html`).
  It reads: "… i samarbete med samtliga partiledare sedan Olof Johansson. Anna-Karin Hatt såklart. Men också Muharrem
  Demirok och Annie Lööf." *gloss: … working with every party leader since Olof Johansson. Anna-Karin Hatt, of course.
  But also Muharrem Demirok and Annie Lööf.* It names people and no dates; tenure is not carried (GAPS 5).
- **KD: not changed.** Ebba Busch in both years. Her page: "Jag har varit partiledare för Kristdemokraterna sedan april
  2015 …" ([PL-KD1a]).
- **L: CHANGED.** Johan Pehrson in 2022. Simona Mohamsson in 2026 ([PL-L1a]). As a cross-reference, the party's own page
  "Liberalernas landsmöte: Förnyat förtroende för Simona Mohamsson" (2026-03-22) is already SOURCED on disk as [L-P2]
  in `coalition_declarations_2026.md` (`raw/declarations/liberalerna_landsmote_fornyat_fortroende.html`).
- **MP: CHANGED, in both seats.** Märta Stenevi and Per Bolund in 2022. Amanda Lind and Daniel Helldén in 2026
  ([PL-MP1a], [PL-MP2a], [PL-MP3a]). The party's own pages carry their own markers, quoted and not carried as fields.
  Lind's list of posts reads "2024-" / "Språkrör" [PL-MP2]. The språkrör page lists a party news item dated "18 november
  2023" with the heading "Daniel Helldén är Miljöpartiets nya manliga språkrör" [PL-MP1]. *gloss: Daniel Helldén is the
  Green Party's new male spokesperson.*

**A correction owed to `party_leaders_2022.md` (flagged here; made at K-1e's build, §608, as a dated note in that file).** Its last section says "C, L, S, MP and V have all
changed leader or spokesperson since." These pages contradict that for **S and V**: each names the same person at
both dates, and V's page states that its leader has held the office continuously since 2020. Read against this file,
the correct set is **C, L and MP**.

## GAPS

1. **No party lacks static office text this year.** 2022's KD gap is closed: `kristdemokraterna.se/ebba` is now
   server-rendered, and the office ("Partiordförande", "partiledare") is in static HTML in both the election-day capture
   and the live page. In 2022 the KD office had to be carried by the Tidö agreement.
2. **Page dates are weak for C, V, KD and MP, so they rest on the capture.** C's pages carry `rek:pubdate` and `rek:moddate`
   (CV 2026-02-17T18:05:48Z; about page 2026-02-02T13:10:01Z; news item 2026-09-12T17:41:56Z; on centerpartiet.se
   these are local time under a Z suffix, see the register's header), and the CV page and
   the about page were last modified months before the vote. V's leader page says "Uppdaterad: 8 september 2025", a
   year before the vote. KD's page says "Senast uppdaterad: 2 januari 2025" (`rek:moddate` 2025-01-02T13:13:52Z),
   twenty months before the vote. For those four pages, the office text is dated to the election by the Internet Archive
   capture of 13 September 2026 and to today by the access date, not by any stamp on the page; C's news item is dated
   by its own stamps to the eve of the vote. (S's page is not weak: its own `rek:moddate`, 2026-09-05T10:38:17Z, dates
   its office text to eight days before the vote.) MP's språkrör page says "Uppdaterad 2024-05-20", more than two years
   before the vote, and its person pages' stamps are not read as edit dates (below), so MP's names too rest on the
   capture. SD's board page (dateModified 2025-11-24) also predates the vote by months, but SD's own page [PL-SD2] is
   stamped 2026-06-12T03:42:59Z. The MP person
   pages' "Uppdaterad" stamps read 2026-09-13 01:05 (+02:00) in the captures and 2026-09-22 01:05 (+02:00) live, with the office text
   unchanged. This file does not read those stamps as edit dates.
3. **M's leader page shows only the state office ("Statsminister") in its visible text.** The party office is in that
   page's metadata ("Ulf Kristersson är partiledare för Moderaterna.") and in the visible front-page heading "Ulf
   Kristersson, Moderaternas partiledare" ([PL-M1a], [PL-M1]).
4. **The statutory title is not established for S, SD, M, V, C, KD or L.** Their statutes were not fetched. Only MP's
   were ([PL-MP4a], [PL-MP4]). This is a gap only if the model ever needs the statutory word rather than the name.
5. **Tenure is not carried.** When C's and L's 2026 leaders took office, and whether anyone led C or L between the 2022
   leader and the 2026 one, is not established by any page fetched here. For MP the only markers are Lind's "2024-" /
   "Språkrör" and the news item dated "18 november 2023" (Helldén); they are quoted, not carried. The only other dates
   this file shows are the ones inside the quoted office sentences (V, KD, SD); they too are quoted, not carried. The
   dates given with the two cross-referenced pages ([L-P2] 2026-03-22; [C-P6]'s speech 2025-11-13) are the dates of
   that page and that speech, not tenure, and are not carried.

## What this file deliberately does NOT carry

- **Leader attributes**: charisma, competence, authenticity and the rest of `CandidateProfile`. These stay
  `[AUTHORED-DRAFT]`.
- **Leader relationships**: §29's personal compatibility between leaders stays deferred and asserted ABSENT, as in 2022.
- **Anything before 13 September 2026 as a separate vintage, and any wiring.** This file names the people who fought
  the 2026 election. Putting the 2026 names into the model was K-1e's build (§608).

## Source register
(all accessed 2026-09-24; `raw/leaders/<file>`; publisher is the party unless the line says Internet Archive; live
fetch times are the server's `Date` header, capture times are `memento-datetime`, both UTC, all in
`raw/leaders/fetch_log.txt`. `rek:pubdate`/`rek:moddate` are given with the Z the meta carries (its `.000` milliseconds dropped). On centerpartiet.se the
page's own `<time>` shows they are local time under a Z suffix ([PL-C3]: rek:pubdate 17:40:24.000Z = `<time>`
17:40:24+02:00 = 15:40:24Z on [PL-C2]'s listing). S's and KD's pages carry no `<time>` element to check theirs
against.)

**S — Socialdemokraterna**
- [PL-S1] https://www.socialdemokraterna.se/vart-parti/vara-politiker/magdalena-andersson — **Socialdemokraterna
  (primary)**, `rek:pubdate` 2021-11-04T16:26:24Z, `rek:moddate` 2026-09-05T10:38:17Z, fetched 2026-09-24 08:26:34. `s_socialdemokraterna_magdalena-andersson.html`
  sha256 `e3f2102db9a8252df0ad3e1e4d1ba81a9b68cf6954e724c2b3cba799f2fa98ab`
- [PL-S1a] https://web.archive.org/web/20260913163730id_/https://www.socialdemokraterna.se/vart-parti/vara-politiker/magdalena-andersson
  — Internet Archive capture of [PL-S1] at **2026-09-13 16:37:30**. `wayback_20260913163730_s_socialdemokraterna_magdalena-andersson.html`
  sha256 `e6cfb5912eedeb0e18d72c621e2e3b16e005374fe5ca119c50dca77e0f7b67fd`

**SD — Sverigedemokraterna**
- [PL-SD1] https://www.sd.se/partistyrelse/ — **Sverigedemokraterna (primary)**, "Partistyrelse", datePublished
  2022-06-30T09:47:18Z, dateModified 2025-11-24T12:30:06Z, fetched 08:26:39. `sd_partistyrelse.html` sha256
  `937aedcd77d6112d81f4fc74bcd42944337e5ad4a91af7f7bbd9f0e1723b7519`
- [PL-SD1a] https://web.archive.org/web/20260913171345id_/https://www.sd.se/partistyrelse/ — Internet Archive capture
  of [PL-SD1] at **2026-09-13 17:13:45**. `wayback_20260913171345_sd_partistyrelse.html` sha256
  `0dc0bdabaab6ec4a2bf63e382882e09dc39c1f6be10c6c9124221d78bb29fc57`
- [PL-SD2] https://www.sd.se/jimmie/ — **Sverigedemokraterna (primary)**, "Jimmie", datePublished 2022-06-30T09:46:50Z,
  dateModified 2026-06-12T03:42:59Z, fetched 08:26:39. `sd_jimmie.html` sha256
  `ba5d91f193d5f557c681df39744765bb87cdbf87947b3b3eee6897b11b5a9476`
- [PL-SD2a] https://web.archive.org/web/20260913110552id_/https://www.sd.se/jimmie/ — Internet Archive capture of
  [PL-SD2] at **2026-09-13 11:05:52**. It was served gzip-encoded and is stored transfer-decoded.
  `wayback_20260913110552_sd_jimmie.html` sha256 `fbd44022f4fa6598bce451a184e394e30708f0940c64cc7990b2ce5d5b270b2e`

**M — Moderaterna**
- [PL-M1] https://moderaterna.se/ — **Moderaterna (primary)**, front page, dateModified 2026-09-11T19:28:03Z, fetched
  08:26:45. `m_moderaterna_startsida.html` sha256 `0d1f9ef880e230ebb848f9268d7d3ae3882da1cf28590dd4141de7bd907b1500`
- [PL-M1a] https://web.archive.org/web/20260913170221id_/https://moderaterna.se/ — Internet Archive capture of [PL-M1]
  at **2026-09-13 17:02:21**. `wayback_20260913170221_m_moderaterna_startsida.html` sha256
  `27b0b5fe80367bbafb3c755024c7f01293af11f073a76e0164db7e8acfb43eb8`
- [PL-M2] https://moderaterna.se/politiker/ulf-kristersson/ — **Moderaterna (primary)**, dateModified
  2026-09-01T16:29:39Z, fetched 08:26:45. `m_moderaterna_ulf-kristersson.html` sha256
  `65ed89a8547ea59ba9ee3420c9466143e9ecfebc356d7597e8d871494b8eee2d`
- [PL-M2a] https://web.archive.org/web/20260913170232id_/https://moderaterna.se/politiker/ulf-kristersson/ — Internet
  Archive capture of [PL-M2] at **2026-09-13 17:02:32**. `wayback_20260913170232_m_moderaterna_ulf-kristersson.html`
  sha256 `f861a64cf30bd2551b9f63552e5aabbf0e4139e013aa0ec979af1f92df63a312`

**V — Vänsterpartiet**
- [PL-V1] https://www.vansterpartiet.se/nooshi-dadgostar/ — **Vänsterpartiet (primary)**, "Om Nooshi Dadgostar", the
  page's own stamp "Uppdaterad: 8 september 2025", fetched 08:26:52. `v_vansterpartiet_nooshi-dadgostar.html` sha256
  `e655ca4eb35b74e154054c0d927eddc19f77f8f594f9b870adaf386689d58a8d`
- [PL-V1a] https://web.archive.org/web/20260913140156id_/https://www.vansterpartiet.se/nooshi-dadgostar/ — Internet
  Archive capture of [PL-V1] at **2026-09-13 14:01:56**. `wayback_20260913140156_v_vansterpartiet_nooshi-dadgostar.html`
  sha256 `2f7f688e56fe27ec930b457d9c33980f823993865cc6664e3d04f5c064835d3d`
- [PL-V2] https://www.vansterpartiet.se/ — **Vänsterpartiet (primary)**, front page, no page date in its HTML, fetched 08:30:23; it carries the
  [PL-V1] sentence. It was served brotli-encoded and is stored transfer-decoded. `v_vansterpartiet_startsida.html` sha256 `919c07600f2f9dc19957b21571cb99d7a66f23680921c207c1973734520bc219`
- [PL-V2a] https://web.archive.org/web/20260913140119id_/https://www.vansterpartiet.se/ — Internet Archive capture of
  [PL-V2] at **2026-09-13 14:01:19**. It does **not** state the office. It is kept as the record that the election-day
  front page did not carry the sentence. It was served gzip-encoded and is stored transfer-decoded.
  `wayback_20260913140119_v_vansterpartiet_startsida.html` sha256
  `4ab2597c64d7d40873fbc230d0adc183d261bd4af12fd5e6e82ca029deb23023`

**C — Centerpartiet**
- [PL-C1] https://www.centerpartiet.se/elisabeth-thand-ringqvist/cv — **Centerpartiet (primary)**, "CV", `rek:pubdate`
  2026-02-17T16:24:00Z, `rek:moddate` 2026-02-17T18:05:48Z (the same in [PL-C1a]), fetched 08:26:54. `c_centerpartiet_elisabeth-thand-ringqvist_cv.html` sha256
  `0bfd4d5ad9003e2c54515e039dc6817a12bfe7a1f9e95ab7024606a305ae6f14`
- [PL-C1a] https://web.archive.org/web/20260913114627id_/https://www.centerpartiet.se/elisabeth-thand-ringqvist/cv —
  Internet Archive capture of [PL-C1] at **2026-09-13 11:46:27**. It was served gzip-encoded and is stored
  transfer-decoded. `wayback_20260913114627_c_centerpartiet_elisabeth-thand-ringqvist_cv.html` sha256
  `1112462d7ca03dc5b1695e7e72f6a8d425823c63c03cb8d9cdfca79816909f2e`
- [PL-C2] https://www.centerpartiet.se/om-centerpartiet — **Centerpartiet (primary)**, "Om Centerpartiet",
  `rek:pubdate` 2025-12-03T00:25:06Z, `rek:moddate` 2026-02-02T13:10:01Z (the same in [PL-C2a]), fetched 08:26:55.
  `c_centerpartiet_om-centerpartiet.html` sha256
  `bc6462c29f1f1240d7da238f8ce8666f06e9750ae9f953041569581bfea6ed71`
- [PL-C2a] https://web.archive.org/web/20260913164505id_/https://www.centerpartiet.se/om-centerpartiet — Internet
  Archive capture of [PL-C2] at **2026-09-13 16:45:05**. `wayback_20260913164505_c_centerpartiet_om-centerpartiet.html`
  sha256 `001bccd46865132cf5fcc6acab2a6a65b11a27f71fc8220dd50ba4a90dfb7135`
- [PL-C3] https://www.centerpartiet.se/nyheter/arkiv-2026/2026-09-12-elisabeth-thand-ringqvist-holl-valspurtstal-pa-medborgarplatsen
  — **Centerpartiet (primary)**, "Elisabeth Thand Ringqvist höll valspurtstal på Medborgarplatsen", NYHET 2026-09-12,
  `rek:pubdate` 2026-09-12T17:40:24Z, `rek:moddate` 2026-09-12T17:41:56Z, fetched 08:26:55.
  `c_centerpartiet_20260912_valspurtstal.html` sha256
  `19949aafe2b4188eb17b0af104ae377a62bcf7fc73498bb9d63040f522291275`
- [PL-C3a] https://web.archive.org/web/20260913164449id_/https://www.centerpartiet.se/nyheter/arkiv-2026/2026-09-12-elisabeth-thand-ringqvist-holl-valspurtstal-pa-medborgarplatsen
  — Internet Archive capture of [PL-C3] at **2026-09-13 16:44:49** (`rek:pubdate` and `rek:moddate` the same as
  [PL-C3]). It was served gzip-encoded and is stored transfer-decoded.
  `wayback_20260913164449_c_centerpartiet_20260912_valspurtstal.html` sha256
  `7449545fcf0671af3821337b297615bb746e4abf19c218182fbfc8bd5e84e677`
- [PL-C4] https://www.centerpartiet.se/elisabeth-thand-ringqvist — **Centerpartiet (primary)**, her own page ("Elisabeth
  Thand Ringqvist | Centerpartiet"), `rek:pubdate` 2026-01-27T23:17:13Z, `rek:moddate` 2026-09-09T10:45:29Z (the same in
  [PL-C4a]), fetched 11:24:40, added at K-1e's review. `c_centerpartiet_elisabeth-thand-ringqvist.html` sha256
  `8ddf3fa0f97d019a55ac5cf7bb5cc0081b64225a2496a01044e0e2fdfbcd8530`
- [PL-C4a] https://web.archive.org/web/20260913114539id_/https://www.centerpartiet.se/elisabeth-thand-ringqvist —
  Internet Archive capture of [PL-C4] at **2026-09-13 11:45:39**. It was served gzip-encoded and is stored
  transfer-decoded. `wayback_20260913114539_c_centerpartiet_elisabeth-thand-ringqvist.html` sha256
  `7a8afd4c9571a2fb81a83ae920b6244d834b1cbb356d7800bed08430491c10e3`

**KD — Kristdemokraterna**
- [PL-KD1] https://kristdemokraterna.se/ebba — **Kristdemokraterna (primary)**, "Ebba", the page's own stamp "Senast
  uppdaterad: 2 januari 2025", `rek:pubdate` 2021-05-07T13:51:44Z, `rek:moddate` 2025-01-02T13:13:52Z, fetched 08:27:00. `kd_kristdemokraterna_ebba.html`
  sha256 `b465bf9c62e04ebb3bb5341d1edd4fde7a1f131c3dfa80855b4ebc498949a99a`
- [PL-KD1a] https://web.archive.org/web/20260913170939id_/https://kristdemokraterna.se/ebba — Internet Archive capture
  of [PL-KD1] at **2026-09-13 17:09:39**. `wayback_20260913170939_kd_kristdemokraterna_ebba.html` sha256
  `b595198e0d4df0c4cdbc0d9ffeeea300ddf57771178157a67faa1f1ffba9b2c7`

**L — Liberalerna**
- [PL-L1] https://www.liberalerna.se/liberaler/simona-mohamsson — **Liberalerna (primary)**, "Simona Mohamsson",
  datePublished 2021-11-21T09:50:29Z, dateModified 2026-06-04T08:04:35Z, fetched 08:27:03.
  `l_liberalerna_simona-mohamsson.html` sha256 `dda082218a444396b5e691c05fd8e5774037a6e2ccbe0b0b152823f51dbde6dd`
- [PL-L1a] https://web.archive.org/web/20260913165717id_/https://www.liberalerna.se/liberaler/simona-mohamsson —
  Internet Archive capture of [PL-L1] at **2026-09-13 16:57:17**. `wayback_20260913165717_l_liberalerna_simona-mohamsson.html`
  sha256 `aec4a278655f2be05b460ce6c0cad41253c570630bb4eb8926354530487de24c`

**MP — Miljöpartiet de gröna**
- [PL-MP1] https://www.mp.se/om/sprakror/ — **Miljöpartiet (primary)**, "Våra språkrör", "Publicerad 2022-02-26",
  "Uppdaterad 2024-05-20", fetched 08:27:14. `mp_sprakror.html` sha256
  `fb6a50c48d700ef4c0fcb50c3e9f47026b447359ca6ff3b528a3048b4a6c0ac8`
- [PL-MP1a] https://web.archive.org/web/20260913142450id_/https://www.mp.se/om/sprakror/ — Internet Archive capture of
  [PL-MP1] at **2026-09-13 14:24:50**. `wayback_20260913142450_mp_sprakror.html` sha256
  `4c7a067d32518671da51b77f1f0a2018119d14242f9c24d42827950dd3950fae`
- [PL-MP2] https://www.mp.se/om/amanda-lind/ — **Miljöpartiet (primary)**, "Amanda Lind", "Publicerad 2022-04-05",
  "Uppdaterad 2026-09-22", fetched 08:27:16. `mp_amanda-lind.html` sha256
  `f4e872ea261faf788ace29927d913be8ccf988fbbca019c72b1631ea1967d246`
- [PL-MP2a] https://web.archive.org/web/20260913145532id_/https://www.mp.se/om/amanda-lind/ — Internet Archive capture
  of [PL-MP2] at **2026-09-13 14:55:32** (the capture shows "Uppdaterad 2026-09-13"). `wayback_20260913145532_mp_amanda-lind.html`
  sha256 `ce6518e63e2a7b0eb46203615d1e2ee060c9809d7affbcaa58b19453a5a81ca6`
- [PL-MP3] https://www.mp.se/om/daniel-hellden/ — **Miljöpartiet (primary)**, "Daniel Helldén", "Publicerad
  2022-04-05", "Uppdaterad 2026-09-22", fetched 08:27:18. `mp_daniel-hellden.html` sha256
  `cfeacd536a6066892a05b016f2a245763401dd5cc217ed508efc40bede9d2d32`
- [PL-MP3a] https://web.archive.org/web/20260913150328id_/https://www.mp.se/om/daniel-hellden/ — Internet Archive
  capture of [PL-MP3] at **2026-09-13 15:03:28** (the capture shows "Uppdaterad 2026-09-13").
  `wayback_20260913150328_mp_daniel-hellden.html` sha256 `8d5b3f09af35520cbe261d1cd6066c98210453c83466aef03aff0aa2ec9e4f90`
- [PL-MP4] https://www.mp.se/om/stadgar/ — **Miljöpartiet (primary)**, "Stadgar för Miljöpartiet de gröna", "Publicerad
  2022-03-09", "Uppdaterad 2025-11-12", fetched 08:27:19. `mp_stadgar.html` sha256
  `979f3eb762944a75d2a9ebda7a1b2263e0e4c9e1794971facf38e41ee9d0d001`
- [PL-MP4a] https://web.archive.org/web/20260913142500id_/https://www.mp.se/om/stadgar/ — Internet Archive capture of
  [PL-MP4] at **2026-09-13 14:25:00** ("Publicerad 2022-03-09", "Uppdaterad 2025-11-12"). It was served gzip-encoded
  and is stored transfer-decoded. `wayback_20260913142500_mp_stadgar.html` sha256
  `7d9db7c880bff387a9a717ae9ae3b609f94b2b226cd4ba098648b8980e7a8914`

**Fetch record**
- [PL-LOG] `fetch_log.txt`: for each saved file, its URL, HTTP status, server `Date`, and for captures
  `memento-datetime`, `x-archive-orig-date`, `x-archive-orig-last-modified` where the archive sent it, and
  `x-archive-src`. The blocks for [PL-MP4a] and [PL-C3a] record fetches made later the same day (server Date 09:04:42 and 09:04:43 UTC) and were appended after the first run; the blocks for [PL-C4] and [PL-C4a] (11:24:40 and 11:25:10 UTC) were appended at K-1e's review. sha256
  `7c8f2b4ce9d5207a91341a784def2fcb0d01842ed27fd218c1789b9683e15d2d`
