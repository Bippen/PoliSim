# Review - §776, Elias's ruling E2's last item: Poland's 2023 coalition declarations, sourced and read by the formation (2026-10-04)

A workflow review (`polisim-staged-review`) of section 776's files - `DeclaredRedLines.cs`, `PolishDeclarationsDiagnostic.cs`, `DeclarationDatesDiagnostic.cs`, `FormationSweepDiagnostic.cs`, `CheckSuite.cs`, the record `ElectionsData/poland/coalition_declarations_2023.md` and its pages under `raw/declarations_2023/` - in three lenses: the sourcing and the record (each sampled quote opened in the saved bytes), the wiring and everything that reads it, and the new diagnostic and the changed checks. Every finding was put to a refute-first skeptic; the reports are verbatim. Not a money path (`Tools/bar_tier.ps1`'s pattern names none of these files), so no ledger row.

## The first pass - confirmed (verbatim)

The workflow polisim-staged-review on section 776's files, three lenses (the sourcing and the record, the wiring and its readers, the new diagnostic and the changed checks), every finding put to a refute-first skeptic.

### 1. The record restates §621 as "the day the words were said"; the ruling says "by the party's own record, never by press reporting", and the deviation is not stated

- **Lens:** sourcing - **reviewer:** defect - **skeptic:** defect
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:14

**The scenario.** Record line 14, the COMPLETED §776 draft ("The rules applied (§621)") and the code comment DeclaredRedLines.cs:390 ("dated by §621's rules") all give §621 as: the day the words were said, read from the page, or the page's own date marked (reported). The ruling as recorded is different: COMPLETED.md:34089 (rulings 3-6) and CLAUDE.md:53 both say "a declaration is dated by the party's own record, never by press reporting". Sweden's timeline handled press-dated spoken words in two ways: a ruled exception (§652, MP), or an open question put to Elias (K-1i (3), KD's line, "an EXTENSION of §621's precedent", POLISIM_FEATURE_LIST.md:146, still OPEN). Germany's record names its ZDF-dated candidacy as following that precedent. Here the extension is applied without saying so to: facts 2-6 (an RMF24 page), 8-9 (TVN24's page date, marked reported), 13 (PAP via GazetaPrawna), 17 (PAP/WP), and the 26 June lift (Super Express/PAP). Two concrete effects. (1) TD's own records first carry TD→PiS on 2023-10-10 (TD-P8, the list's paid material: "nie z PiS") and 2023-10-12 (TD-P9). Under the ruling as written, the run-up page would show "TD will not sit with PiS" from 10 Oct; the timeline shows it from 15 May. (2) Konfederacja's support half (facts 8-9, standing on polling day) appears on no party-own page. KONF-P1 says only "nie będzie z nikim koalicji", and KONF-P2 carries only the coalition half plus an aim. Under the ruling as written the support half cannot be dated at all, which is exactly the case §639 sent to Elias.

**The fix proposed.** Quote §621 verbatim. State the extension: a leader's own words, quoted verbatim, dated by the broadcaster's or agency's page, with §652 as its precedent. Mark each fact that rests on it (2-6, 8-9, 13, 17 and the 26 June lift). Correct the DeclaredRedLines.cs:390 comment. Add the Polish cases to K-1i (3) for Elias, or get his ruling before landing.

**The skeptic's evidence.** I could not refute it. Each part of the finding checks out against the files.

1. The rule as restated vs. the rule as ruled.
- `ElectionsData/poland/coalition_declarations_2023.md:13-14`: "**What counts:** words ... by the party, its leader or an authorised spokesperson ... **Dates** (§621): the day the words were said, read from the page; where the page gives only its own date, that date *(reported)*".
- The §776 draft in `COMPLETED.md` says the same ("The rules applied (§621): ... the date the day the words were said, or the page's own date marked *(reported)*").
- `DeclaredRedLines.cs:390`: "quoted from a saved page and dated by §621's rules".
- The ruling itself, `COMPLETED.md:34089` (rulings 3-6) and `CLAUDE.md:53`: "a declaration is dated by the party's own record, never by press reporting".
- No mention of §652, K-1i, "precedent" or "extension" appears anywhere in the record, `PolishDeclarationsDiagnostic.cs` or the Polish block of `DeclaredRedLines.cs` (grep).

2. How the repo handled this before.
- `DeclaredRedLines.cs:346-347` (Sweden, KD): "spoken words, not a party publication, so an EXTENSION of §621's precedent (a post on X), stated and put to Elias (§639)".
- `DeclaredRedLines.cs:343` (MP): dated by Helldén's words to SR, "ruled 2026-09-29, §652". That is one ruled case only.
- `POLISIM_FEATURE_LIST.md:146`: K-1i (3) is still OPEN.
- Germany, `coalition_declarations_2025.md:47`: Weidel's date is flagged "a broadcaster's report, as K-1's KD line is SVT's".
- Germany, §705 (`COMPLETED.md:35491`): Merz's 23 September date was refused because it was "press-reported".
- `Reviews/2026-09-25_s639_k1h_k1g.md:31` graded this exact pattern, one fact called "§621's precedent" when it was spoken words in a press report, as "**defect (record honesty)** ... Fixed: stated as an extension, put to Elias".

3. The Polish facts that rest on press pages (from the register).
- Facts 2-6 start on [KONF-I4], RMF24's page of its own debate.
- The 26 June lift rests on [KONF-I8] (Super Express), [KONF-I10] (Bankier/PAP) and [KONF-I11] (RMF24).
- Facts 8-9 rest on [KONF-I14], TVN24, marked (reported). This also sets the 13 July end of fact 7.
- Fact 13 rests on [TD-I5], PAP via GazetaPrawna.
- Fact 17 rests on [NL-I5/I6/I7], PAP/WP. This also sets the 27 August end of fact 16.
- Facts 1, 7, 10-12, 14, 15 and 16 do come from the party's own record.

4. Checked against the saved bytes.
- [KONF-P1] says only "Jasno i klarownie mówimy: nie będzie z nikim koalicji."
- [KONF-P2] says "Koalicji z PiS nie chcą ... chcemy zakończyć rządy PiS".
- The out-of-tree Bosak X post is dated 2023-10-16, after the vote.
- So no Konfederacja page of its own carries the support half of facts 8-9.
- For TD → PiS, its own pages before 10 October hold only aims: [TD-P6] "odsunięcie PiS od władzy", and the uncited Polska 2050 page of 15 May, "odsunąć PiS od władzy". The record's §10 says an aim is not a line. The first own-record refusal is [TD-P8], 2023-10-10 ("nie z PiS").
- The polling-day formation readings measured in `n776b.log:513-517` do not include the ruling read as written.

5. What limits the damage.
- §652 is a precedent Elias ruled himself for exactly this kind of source, so the dates may well stand once they are stated.
- Every quote names its outlet, and doubt 9 lists the article dates.
- Every fact stands on polling day either way.

Even so, the record gets §621 wrong and quietly extends an open question to nine facts. By the repo's own s639 standard that is a record-honesty defect.

**The skeptic's corrected fix.** 1. At record line 14 and in the §776 draft's "rules applied", quote §621 word for word: "dated by the party's own record, never by press reporting; a document by the decision it records; a declaration stands until a later dated one replaces it."
2. State the extension as its own rule: a leader's or spokesperson's own spoken words, quoted verbatim, dated by the broadcaster's or agency's page. Give §652 as its precedent (MP, Helldén's words to SR). Also name §621's KD application, a leader's post as a newspaper quotes it.
3. Mark every fact that rests on the extension: 2-6 ([KONF-I4]), the 26 June lift ([KONF-I8]/[KONF-I10]/[KONF-I11]), 8-9 ([KONF-I14], which also ends fact 7), 13 ([TD-I5]) and 17 ([NL-I5/I6/I7], which also ends fact 16). Mark them in the §8 table or the prose, in each `PolandTimeline` basis string, and in a block comment the way Sweden's KD fact is marked.
4. Fix `DeclaredRedLines.cs:390` to read: "dated by §621's rules and, where stated, the extension put to Elias (K-1i)".
5. Add the Polish cases to K-1i (3) in `POLISIM_FEATURE_LIST.md` and to §776's owed list. While there, note that K-1i (2) was answered by §652.
6. Measure first: add a reading that dates by the party's own record alone and print it (Konf's support half undated, so 8-9 are cabinet-only; the June facts absent; TD → PiS from [TD-P8]). Elias then rules on a measurement. Or get his ruling before landing.

### 2. "Restated" lists for the support-blocking lines cite sources that restate only the coalition half

- **Lens:** sourcing - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/DeclaredRedLines.cs:412

**The scenario.** Facts 8-9 (DeclaredRedLines.cs:412-415) and record lines 67-70 list KONF-I15, KONF-P1, KONF-I23 and KONF-I25 as restatements of the ONE-WAY SUPPORT-BLOCKING lines. In the bytes: KONF-I15 (Bosak, 16 Jul): "Nie ma i nie będzie żadnych rozmów na temat współrządzenia z PiS-em ... My chcemy PiS odsunąć od władzy" — coalition plus an aim. KONF-P1 (2 Aug): "nie będzie z nikim koalicji" — coalition only. KONF-I25 (11 Oct): "Nie zamierzam współtworzyć z rządu ...", with trades on single bills kept open — coalition only. KONF-I23 is an aim. Only KONF-I19 (24 Aug: "ani przedłużać władzy PiS-u, ani ułatwiać Tuskowi powrotu do władzy") restates the support half, and doubt 5 (line 263) says so correctly. Fact 17's "Restated 2023-09-11 [NL-P25]" ("Lewica w żadnym rządzie nie będzie stała czy siedziała przy Konfederacji") likewise restates only the cabinet half. Someone weighing doubt 5 from the basis text sees five restatements behind a support refusal that two sources carry.

**The fix proposed.** Split each list into two: the coalition half restated by ..., and the support half restated only on 2023-08-24 [KONF-I19]. Say the same for NL-P25 in fact 17.

**The skeptic's evidence.** CLAIMS (working tree): DeclaredRedLines.cs:413 (fact 8) "Konfederacja will neither sit in nor keep in power a cabinet with PiS ... Restated 2023-07-16 [KONF-I15], 2023-08-02 [KONF-P1], 2023-08-24 [KONF-I19], 2023-10-10 [KONF-I23] and 2023-10-11 [KONF-I25]". :415 (fact 9) "neither sit in nor enable ... Restated 2023-08-02 [KONF-P1], 2023-08-24 [KONF-I19], 2023-10-10 [KONF-I23] (\"nie dopuścić do rządów Donalda Tuska\") and 2023-10-11 [KONF-I25]". :431 (fact 17) "will not govern with Konfederacja, support a cabinet that includes it, or rest a cabinet on its bought votes ... Restated 2023-09-11 [NL-P25]". The record repeats the lists under "ONE-WAY, support-blocking" (coalition_declarations_2023.md:61-70) and at :130-132.

BYTES (raw/declarations_2023/):
- KONF-I15 (Konf/tvn24_bosak-nie-wejdzie-w-koalicje-z-pis.html). Every Bosak paragraph is "Nie ma i nie będzie żadnych rozmów na temat współrządzenia z PiS-em, na temat koalicji z PiS-em. My chcemy PiS odsunąć od władzy", "PiS powinien zostać odsunięty od władzy" or "dwie drogi: współrządzenie albo opozycja". The page has no "przedłuż" or "umożliw" wording.
- KONF-P1: only "Jasno i klarownie mówimy: nie będzie z nikim koalicji ... nie wejść w tą koalicję".
- KONF-I19: "Konfederacja nie zamierza ani przedłużać władzy PiS-u, ani ułatwiać Tuskowi powrotu do władzy". This is the one support-half restatement.
- KONF-I23: "chcemy zakończyć rządy PiS-u i nie dopuścić do rządów Donalda Tuska", plus "Gdybym ja teraz ... zrobił rząd z PiS-em, straciłbym całkowicie wiarygodność". That cabinet sentence names PiS alone, so for KO this page carries only the "nie dopuścić" aim.
- KONF-I25: "Nie zamierzam współtworzyć z rządu ani z Koalicją Obywatelską, ani z PiS-em", plus single-bill trades from a "mniejszość blokującą".
- NL-P25: "żadna z demokratycznych sił nie będzie współtworzyła rządu z Konfederacją. Powtórzę: Lewica w żadnym rządzie nie będzie stała czy siedziała przy Konfederacji". Cabinet half only.

THE RECORD CONTRADICTS ITSELF:
- §10 (:217-219): "an aim to remove a party from power, or keep it out, is not a line".
- Fact 7 (:411): KONF-P2's "chcemy zakończyć rządy PiS" — "its stated aim to end PiS's rule is not read as support-blocking". Yet KONF-I23's identical "chcemy zakończyć rządy PiS-u" is listed as restating the support-blocking fact 8.
- Doubt 5 (:263) rests the support half on 2023-07-13 and 2023-08-24 alone.

REACH: the basis text is player-visible. FormationProposal.cs:171 "refuses: a red line falls inside this cabinet - " + inside.Value.Basis, shown in the slip at GameController.FormationSheet.cs:310. It is also what Elias reads when weighing doubt 5.

NO BEHAVIOUR IMPACT: facts 8 and 9 (true, true, 2023-07-13, Open) rest on KONF-I14 alone, and fact 17 on NL-I5/I6/I7. The formation outcome does not move. FormationSweepDiagnostic's digest text carries no Basis. PolishDeclarationsDiagnostic's Key compares Basis strings built from the same DatedFact on both sides.

**The skeptic's corrected fix.** Text only. No flag, date or tag changes and no sweep re-pin; mirror each change in the record.

1. Fact 8 (DeclaredRedLines.cs:413; record §2 :66-70): "Its cabinet half restated 2023-07-16 [KONF-I15], 2023-08-02 [KONF-P1], 2023-10-10 [KONF-I23] and 2023-10-11 [KONF-I25]; its support half only 2023-08-24 [KONF-I19] (\"ani przedłużać władzy PiS-u\"). 16 July's \"chcemy PiS odsunąć od władzy\" and 10 October's \"chcemy zakończyć rządy PiS-u\" are aims, not read as the support half (fact 7's standard)."

2. Fact 9 (:415): "Its cabinet half restated 2023-08-02 [KONF-P1] and 2023-10-11 [KONF-I25]; its enable half only 2023-08-24 [KONF-I19] (\"ani ułatwiać Tuskowi powrotu do władzy\")." Then say how 10 October's "nie dopuścić do rządów Donalda Tuska" [KONF-I23] is read:
   - either as an aim under §10 (keep it out), in which case it restates neither half, because that page's cabinet sentence names PiS alone;
   - or, explicitly, as the builder's reading of the enable half.

3. Fact 17 (:431; record §4 :130-132): "Its cabinet half restated 2023-09-11 [NL-P25] (\"Lewica w żadnym rządzie nie będzie stała czy siedziała przy Konfederacji\"); the support half is not restated."

4. Doubt 5 then agrees with the lists as it stands, and needs no change.

### 3. Doubts quote pages that are not registered or held in tree, contrary to the header; doubt 2 misattributes the wired wording

- **Lens:** sourcing - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:260

**The scenario.** The header (line 4) says "Every source cited is stored byte for byte under raw/declarations_2023/". Doubt 2 quotes three pages that carry no tag and exist only out of tree: Konf's "nie planujemy przedłużyć o trzecią kadencję rządów PiS-u" (CROSS/rp_2023-08-24_bosak-radio-zet-ekipa-pis-musi-odejsc.html); Hołownia's "odsunąć PiS od władzy (2023-05-15)" (only in TD/pl2050_2023-05-15_trzecia-droga-polski-2050-i-psl.html, as "odsunąć PiS od&nbsp;władzy"; it is not in the cited TD-I5 page); and Kobosko's "nie ma mowy o naszej współpracy z PiS" (TD/krytykapolityczna_2023-06-30_kobosko-wywiad.html). Doubt 8's "bardzo jednoznaczną deklarację" (NL/lewica_org_20230717_dziemianowicz_bak_nigdy_skrajna_prawica.html) and doubt 3's PSL line of 2020-09-14 are also out of tree only. Doubt 9 (line 267) calls the agreement's quote "the only quote not found as text in the saved bytes", which is true only if the out-of-tree sweep counts as saved. Doubt 2 also says Konf's "nie planujemy przedłużyć ..." "is read as support-blocking". The wired line rests on KONF-I14's and KONF-I19's different words; that rp.pl rendering of the same 24 Aug interview is not cited by any fact.

**The fix proposed.** Either register those pages (a tag, an in-tree copy and a digest) or mark each as out of tree with its captures path. Correct doubt 2's attribution to KONF-I19's wording. Narrow the header's and doubt 9's claims.

**The skeptic's evidence.** The main claim is CONFIRMED.

The record makes the claim in two places:
- Lines 4-6: "Every source cited is stored byte for byte under `raw/declarations_2023/<declarer>/`".
- Line 334: the out-of-tree files are "corroborations and context the text does not cite".

But §12 (introduced at line 257 as "The sweep's own list, as its synthesis wrote it") quotes or dates pages that carry no tag, have no in-tree copy and give no URL. Each was grepped in tree under every entity and escape form, then found in the captures:
- Doubt 2 (line 260), Hołownia's "odsunąć PiS od władzy (2023-05-15)": the TD-I5 page `TD/gp_2023-05-15_tak-dla-rzadu-z-opozycja.html` has zero matches for "odsun". It exists only out of tree, in `TD/pl2050_2023-05-15_trzecia-droga-polski-2050-i-psl.html`, as "odsunąć PiS od&nbsp;władzy", and the quote is not marked (decoded).
- Doubt 2, Kobosko's "nie ma mowy o naszej współpracy z PiS": no in-tree match. It is only in the captures' `TD/krytykapolityczna_2023-06-30_kobosko-wywiad.html`.
- Doubt 8 (line 266), "bardzo jednoznaczną deklarację": no in-tree match. It is only in the captures' `NL/lewica_org_20230717_dziemianowicz_bak_nigdy_skrajna_prawica.html`.
- Doubts 3 and 12 (lines 261 and 270), PSL's refusal "from 2020-09-14": it is only in the captures' `TD/rp_2020-09-14_zadnej-koalicji-z-pis.html` ("Kosiniak-Kamysz: Żadnej koalicji z PiS-em nie będzie"). TD-I4 (2022-08-09) has no 2020 reference. This one has a consequence: doubt 3's alternative start, "2022-07-19, when both refusals stood", needs PSL's 2020 refusal, because TD-I4 postdates 2022-07-19. So a ruling option put to Elias rests on an unregistered page, and doubt 12 calls that page "verified".
- Doubt 5 (line 263), not in the finding: its path `Konf/x_bosak_1713834658134138996_syndication.json` is written like a register path, but the in-tree `raw/declarations_2023/Konf/` holds only `x_mentzen_...json`. The file exists only in the captures (digest a8d8a865… in the captures' Konf/SHA256SUMS.txt).
- Nothing catches any of this. `PolishDeclarationsDiagnostic.cs` checks (b) and (c), lines 68-95, read only the fact Basis tags and the register rows.

The "misattributes the wired wording" part is REFUTED in substance:
- The wired Konf→PiS fact (DeclaredRedLines.cs:413) cites [KONF-I19] (2023-08-24) as a restatement of the support-blocking line.
- KONF-I19's in-tree page `Konf/dorzeczy_bosak-niezadowolenie-pis-po.html` carries: sama partia nie chce "przedłużyć o trzecią kadencję rządów PiS-u", which is Do Rzeczy quoting Bosak from the same Radio ZET interview.
- Only the "nie planujemy" prefix comes from the untagged rp.pl rendering, `CROSS/rp_2023-08-24_bosak-radio-zet-ekipa-pis-musi-odejsc.html` (out of tree). That is a tagging gap, not a misattribution.

The doubt-9 part is NOT a defect. The record uses "saved" for the captures too: line 223 ("Saved and read … the files stay in the captures folder") and line 334 ("Every other saved file … all out of tree"). On that reading, doubt 9's "only quote not found as text in the saved bytes" holds.

Severity is minor. No wired fact, register row, digest or formation result is affected. The problem is a false provenance claim in a SOURCED record, plus quotes put to Elias for a ruling that cannot be traced from the repo.

**The skeptic's corrected fix.** 1. For each page §12 leans on that has no tag, do one of two things:
   - register it: a tag and URL, a byte copy under raw/declarations_2023/<declarer>/, and its digest in SHA256SUMS.txt, so the diagnostic's check (c) holds it; or
   - mark it in the doubt as "out of tree: PoliSim-captures/sources/poland_declarations_2023/<path>".
   Register the PSL 2020-09-14 rp.pl page at least, because doubt 3's 2022-07-19 alternative and doubt 12's "verified" both depend on it.
   The pages to cover are:
   - TD/rp_2020-09-14_zadnej-koalicji-z-pis.html
   - TD/pl2050_2023-05-15_trzecia-droga-polski-2050-i-psl.html
   - TD/krytykapolityczna_2023-06-30_kobosko-wywiad.html
   - NL/lewica_org_20230717_dziemianowicz_bak_nigdy_skrajna_prawica.html
   - Konf/x_bosak_1713834658134138996_syndication.json (doubt 5's path, which does not resolve in tree)
2. Narrow the header (lines 4-6) to the sources the facts' and §§1-11's tags cite, and say that §12 also quotes the sweep's captures. Make the same change to line 334's "context the text does not cite".
3. Doubt 2: quote Konfederacja as its tagged page renders it, "przedłużyć o trzecią kadencję rządów PiS-u" [KONF-I19]. This fixes the tag, not an attribution. Mark Hołownia's quote *(decoded)* if it is kept.
4. Leave doubt 9 unchanged.

### 4. fetch_log overstates what the out-of-tree folders hold

- **Lens:** sourcing - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/raw/declarations_2023/fetch_log.md:6

**The scenario.** Lines 6-8 say the sweep folders come "with each folder's checksum file and the SOURCES.tsv/urls.tsv lists of the URL requested, the publisher, the page's own date as served and the day accessed". On disk: PiS has no checksum file (the record admits this in doubt 15; the fetch log does not). CROSS, TD and PiS have no URL list. Konf/SOURCES.tsv has file, url, bytes, sha256 and accessed, with no publisher or page date. NL/urls.tsv has file, url and fetched only. Only KO/SOURCES.tsv carries publisher and page date. Anyone retracing an out-of-tree corroboration cited in the doubts (for example TD's pl2050_2023-05-15 page) finds no URL record for it.

**The fix proposed.** Say per folder what is there, and that PiS's other digests are in the record's register footer.

**The skeptic's evidence.** The claim, ElectionsData/poland/raw/declarations_2023/fetch_log.md lines 6-8: "It holds every page the sweep saved ... with each folder's checksum file and the `SOURCES.tsv`/`urls.tsv` lists of the URL requested, the publisher, the page's own date as served and the day accessed."

What is on disk in G:/UNITY/Projects/PoliSim-captures/sources/poland_declarations_2023/ (full `ls -la`, no filter):
- CROSS: SHA256SUMS.txt only, no URL list.
- KO: SHA256SUMS.txt + SOURCES.tsv. Header: file, url_requested, url_effective_if_redirected, publisher, page_date_as_served, accessed, note. This is the only list that matches the fetch log's description.
- Konf: SHA256SUMS.txt + SOURCES.tsv. Header: file, url, bytes, sha256, accessed. No publisher, no page date.
- NL: SHA256SUMS (no extension) + urls.tsv. Header: file, url, fetched. No publisher, no page date.
- PiS: no checksum file and no URL list (21 files: html pages + pis_wpjson_post_*.json).
- TD: SHA256SUMS.txt only, no URL list.

The record contradicts the fetch log in the same change. coalition_declarations_2023.md line 273 (doubt 15): "the PiS folder has no SHA256SUMS file (its digests are in the record)". Lines 334-336: "the CROSS, KO, Konf, NL and TD folders each carry one ... The PiS folder has none, so its other files are listed here".

The finding's example holds. Doubt 2 (line 260) cites "Hołownia's 'odsunąć PiS od władzy' (2023-05-15)" with no tag. The quote is in the bytes of the out-of-tree TD/pl2050_2023-05-15_trzecia-droga-polski-2050-i-psl.html and is absent from the in-tree [TD-I5] gp_2023-05-15 page. That page is not in the register, and TD has no URL list. A search of the captures tree finds pl2050_2023-05-15, krytykapolityczna_2023-06-30 (Kobosko, doubt 2) and the CROSS wayback PAP-English page (doubt 10) only in SHA256SUMS files. Only x_bosak_* (doubt 5) has a URL row, in Konf/SOURCES.tsv.

Why it is minor rather than a defect:
- Every out-of-tree page checked carries its own canonical URL (rel=canonical / og:url) in its bytes, e.g. polska2050.pl/informacja-prasowa/trzecia-droga-polski-2050-szymona-holowni-i-psl/, krytykapolityczna.pl/kraj/kobosko-holownia-kosiniak-psl/, pap.pl/en/news/far-right-leader-rules-out-coalitions-polands-main-parties. The URLs can still be traced.
- All 54 in-tree pages (CROSS 7, KO 5, Konf 10, NL 11, PiS 9, TD 12) are covered by the register's URL/publisher/date/digest rows and the in-tree SHA256SUMS.txt.
- PolishDeclarationsDiagnostic does not read fetch_log, SOURCES.tsv, urls.tsv or the captures path. No game or diagnostic behaviour depends on it.

What is wrong is a false, dated provenance claim about the environment, which the record itself contradicts.

**The skeptic's corrected fix.** Replace the overclaiming clause in fetch_log.md lines 6-8 with what the sweep actually left. For example: "...the corroborations and the dropped findings' pages included. Each folder carries the checksum file and URL list its sweep wrote, and not every folder has both. PiS has no checksum file: its other files' digests are in the record's register footer. CROSS, TD and PiS have no URL list. Konf's SOURCES.tsv and NL's urls.tsv give the URL and the day accessed. Only KO's SOURCES.tsv also gives the publisher and the page's date as served. A page with no URL row carries its own canonical URL (rel=canonical / og:url) in its saved bytes." Optionally, give the register's URL for the out-of-tree pages the doubts cite by date alone: TD/pl2050_2023-05-15 (doubt 2), TD/krytykapolityczna_2023-06-30 (doubt 2), TD/rp_2020-09-14 (doubt 3), and the CROSS wayback PAP-English and Notes from Poland pages (doubt 10). Then retracing them does not depend on the page's own markup.

### 5. The (reported) mark is applied unevenly; KONF-I8's createdDate is not recorded

- **Lens:** sourcing - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:46

**The scenario.** KONF-I4 dates facts 2-6. Its page gives only its own date ("Wtorek, 20 czerwca 2023 (20:42)"), and the body never says when the debate happened. Under the record's own rule this should be marked (reported), but it is not (line 46), and doubt 9's list of reported dates leaves it out. KONF-I15 (TVN24, 16 Jul), KONF-I23 (Polsat, 10 Oct), KONF-I25 (TVN24, 11 Oct) and KONF-I3 (Interia, 30 Mar) are in the same position, while KONF-I14 is marked. Separately, the Super Express page KONF-I8 carries createdDate "2023-06-24T22:58+02:00", so the words were said by 24 June at the latest; the 26 June end of facts 2-6 is the print date. The record notes createdDate for PIS-I5's Super Express page but not for KONF-I8, and doubt 4 mentions only the video's 22 June.

**The fix proposed.** Mark these dates (reported) and add them to doubt 9. Add KONF-I8's createdDate to doubt 4 as an alternative end of the wobble.

**The skeptic's evidence.** I could not refute the finding. One of its five examples (KONF-I23) is weak, but the rest hold.

THE RULE. Record line 14 says: "**Dates** (§621): the day the words were said, read from the page; where the page gives only its own date, that date *(reported)*". COMPLETED.md §776 repeats it: "or the page's own date marked *(reported)*".

KONF-I4 (dates facts 2-6 from 2023-06-20). The held page Konf/rmf24_petru-mentzen-pytania-sluchaczy.html gives only its own date: datePublished 2023-06-20T20:42:03+02:00, shown as "Wtorek, 20 czerwca 2023 (20:42)". The body never says when the debate took place. Its only framing is "W finałowej części debaty pomiędzy Ryszardem Petru i Sławomirem Mentzenem obydwaj politycy odpowiadali na pytania słuchaczy RMF FM." Line 46, line 41 ("2023-06-20 until 2023-06-26") and the array's comments in DeclaredRedLines.cs (lines 400-409) all leave the date unmarked.

NO "BROADCASTER'S OWN RECORD" EXCEPTION. The record does not exempt a broadcaster's own record of its own programme:
- KO-I4 is Polsat's own record of its "Graffiti" show ("08.09.2023, 09:27", no weekday in the body), and it IS marked at line 146.
- KONF-I14 is TVN24's Fakty po Południu ("13 lipca 2023, 17:56", no weekday in the body), and it IS marked at line 66.
- KONF-I15 has the same programme and the same layout ("16 lipca 2023, 17:01"; the body only says "deklaruje Krzysztof Bosak"; Terlecki speaks as "wicemarszałek Sejmu"). It is left unmarked at lines 29, 67 and 80.

OTHER UNMARKED PAGES:
- KONF-I25 (line 69) only says Mentzen "był gościem nowego cyklu rozmów w wydłużonych w tygodniu przedwyborczym 'Faktach' TVN". It gives no day.
- KONF-I3 (line 78) is an Interia interview with publish-date 2023-03-30T08:29:05+02:00 and no interview date.
- Doubt 9 (line 267) lists only SE 07-11, WP 06-08, GP 09-20, naTemat 09-01, Polsat 09-08, TVN24 07-13 and SE 06-26.

THE WEAK EXAMPLE, KONF-I23. Its page embeds Mentzen's X post "Jutro przed południem złożę publicznie propozycję..." dated "October 10, 2023", with the line "O zapowiedź polityka zapytał Bogdan Rymanowski". It also prints the weekday schedule of the Gość Wydarzeń series. So the day can be read from the page, and leaving it unmarked is defensible.

KONF-I8, THE createdDate. The page's article object is {"objectId": "aa-ooC2-bv4j-5tZN", ..., "pubdate": "2023-06-26T09:05+02:00", "createdDate": "2023-06-24T22:58+02:00"}.
- The register row (line 292) records only datePublished.
- PIS-I5's row (line 285) and doubt 9 do record that page's createdDate, so the two Super Express pages are treated differently.
- Doubt 4 (line 262) gives only "may date from 2023-06-22, its embedded video's date". That video is the VideoObject "Wieczorny Express - Sławomir Mentzen cz.1", uploadDate 2023-06-22T22:22:07+00:00, which is 23 June 00:22 Warsaw time.
- PAP [KONF-I10] says "w wywiadzie dla poniedziałkowego 'SE'", so 26 June is the print date, as the finding says.
- The 24 June createdDate is a later bound than the video date doubt 4 already gives. Adding it makes the record complete; it does not add a new alternative end.

IMPACT: none on the game.
- The dates in PolandTimeline do not change.
- PolishDeclarationsDiagnostic reads no marks (a grep for "reported" finds nothing).
- Facts 2-6 end long before polling day.
- The 20 June date itself is correct. An out-of-tree post, Konf/x_rozmowa-rmf_1671214148456251392_syndication.json (created_at 2023-06-20T17:51:17Z, #DebataRMF), quotes the line live: "w przyszłej kadencji nie będę w koalicji z nikim". Only the provenance mark is missing.

**The skeptic's corrected fix.** 1. Apply line 14's rule evenly in coalition_declarations_2023.md:
   - Mark *(reported)* on KONF-I15's 2023-07-16 (lines 29, 67, 80), KONF-I25's 2023-10-11 (line 69) and KONF-I3's 2023-03-30 (line 78).
   - Add "RMF24 2023-06-20; TVN24 2023-07-16; TVN24 2023-10-11; Interia 2023-03-30" to doubt 9.
2. For KONF-I4 (lines 41/46), choose one:
   - mark 2023-06-20 *(reported)* and list it in doubt 9; or, better,
   - bring the @Rozmowa_RMF post into tree and cite it. That is x_rozmowa-rmf_1671214148456251392_syndication.json, created_at 2023-06-20T17:51:17Z, which quotes the line live in #DebataRMF. It dates the words to the day, so the mark would not be needed. If you take this route, add the post to the register and SHA256SUMS.
3. KONF-I23 can stay unmarked. Its page embeds Mentzen's post of 10 October 2023, which Rymanowski asked him about, so the day is read from the page. Say so, or mark it to keep the treatment uniform.
4. Add KONF-I8's createdDate (2023-06-24T22:58+02:00) to its register row, as PIS-I5's row carries its createdDate. In doubt 4, add it as a bound between the video and the print date:
   - the video's uploadDate is 2023-06-22T22:22Z, which is 23 June 00:22 Warsaw time;
   - the print date is PAP's "poniedziałkowego SE", 26 June.
   It is not a separate alternative end.
5. No code change. The C# dates stay as they are, and the diagnostic reads no marks.

### 6. Claim convention: derived figures transcribed into the record and the new pin comment; the timeline's doc comment overclaims

- **Lens:** sourcing - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:212

**The scenario.** §10 (lines 212-216) and doubts 1-2 (lines 259-260) transcribe facts about the code: the thresholds (CoalitionFormation SocialGap 5.0 and IdeologicalGap 4.5) and CHES gaps computed from PartySystem seed positions (6.70, 6.66, 4.79/2.71, 2.59/1.61, 4.75/4.46). I recomputed them and they are correct today, with TD as the mean of PSL 27 and Polska 2050 31. They would go stale silently if a seed position or threshold changed; the precedent is the Sweden 2022 record's "(6.05 > 5.00)". The new PinnedDigest comment (FormationSweepDiagnostic.cs:129) transcribes formation outputs: 416, 222/248, 194/44, "2 of 250". It follows the earlier pins' pattern, and I checked it against the before776/mismatch diff. The DeclaredRedLines.cs:390 summary says "every line a party declared on or before the Sejm election". That overclaims: the members' pre-list refusals (TD-I3, TD-I4), PSL's line of 2020-09-14 and SLD's 2019 line are declared but not carried (doubts 3 and 12).

**The fix proposed.** Point to PolishDeclarationsDiagnostic, which already prints the derived lines' bases, instead of transcribing the figures. Say "the lines this record carries" in the summary.

**The skeptic's evidence.** I could not refute the two main parts. The third part (the pin comment) is not a problem, and the proposed fix rests on a false premise.

1. THE RECORD'S FIGURES ARE TRANSCRIBED CODE FACTS. They are correct today.
- What the record says:
  - §10, coalition_declarations_2023.md:212-216: "galtan gap over 5.0 support-blocking, lrgen over 4.5 cabinet", "PiS–NL (galtan 6.70)", "NL–Konf (6.66)", "PiS–KO (galtan 4.79, lrgen 2.71)", "PiS–TD (2.59, 1.61)", "KO–Konf (4.75, 4.46)".
  - The same figures again in doubts 1-2 (:259-260).
- I recomputed every figure and all of them match:
  - Seed positions, PartySystem.cs:448-452: PiS galtan 8.45 / lrgen 7.64; KO 3.66 / 4.93; NL 1.75 / 2.41; Konf 8.41 / 9.39.
  - TD is the seat-weighted mean of its members (PartySystem.cs:412-414, JointList at :430-444): PSL 27 seats, Polska 2050 31 seats, giving galtan 5.858 and lrgen 6.032.
  - Thresholds: DerivedRedLines.IdeologicalGap = 4.5 and SocialGap = 5.0 (CoalitionFormation.cs:931, :934). Both are marked [AUTHORED-DRAFT], "Calibrated on Sweden 2022".

2. NOTHING TIES THESE FIGURES TO THE CODE.
- DocumentClaimCheck reads only root *.md files (DocumentClaimCheck.cs:124, TopDirectoryOnly), and only backticked Type.Member names.
- CommentClaimCheck reads .cs comments only.
- PolishDeclarationsDiagnostic parses only the §8 table rows (:51) and the register rows (:69, :83).
- The failing path: lower SocialGap below 4.79. DerivedRedLines.From (:951-959) then draws PiS–KO and KO–Konf as support-blocking lines. §10's "two lines ... Nothing else" and doubt 1's "The derived rule draws nothing either" become false. The only check that fails is the sweep pin, which gets re-pinned without anyone touching the record.
- KO–Konf's lrgen gap of 4.46 sits 0.04 under its threshold, so this is fragile.
- What the rules say:
  - CLAUDE.md:41: "Nobody transcribes, anywhere, in any file." Only COMPLETED.md is exempt (§190 rule 5 names three historical records). §190 rule 2 forbids "a measured figure presented as current".
  - Precedent: the s773 review's item 14 graded a transcribed count that was true at the time as a note.
- Mitigations:
  - The doubts list is introduced as "The sweep's own list, as its synthesis wrote it" (:257), so it reads as a quotation.
  - §10, however, is the record's own present-tense prose.
  - The Sweden 2022 precedent (coalition_declarations_2022.md:51, "6.05 > 5.00") is still true: SD 9.00 minus C 2.95.
- Two more stale claims sit in the same paragraph:
  - §10's "measure before wiring (Doubts)" (:216-217) is out of date: §776 has already measured and wired.
  - "a PiS cabinet on TD's support (259)" is not among the viable options in the sweep, before or after the change. The after-block, Poland | seated | negative False, lists 6 viable options and none is cabinet 1 (PiS alone).

3. THE SUMMARY OVERCLAIMS. This is false today, not just at risk.
- DeclaredRedLines.cs:389-391 says the array is "every line a party declared on or before the Sejm election of 15 October 2023 ... What is not here stays DERIVED".
- The record itself lists declared lines that the array does not carry:
  - PSL→PiS, 2022-08-09 [TD-I4] (and from 2020-09-14).
  - Polska 2050→PiS, 2022-07-19 [TD-I3].
  - The record marks both "recorded, not wired" (:96-98).
  - SLD→PiS, 2019-10-17 [NL-P1], which is only cited inside NL's basis.
  - Doubt 12 (:270) says these "could key SLD and PSL ... not proposed here".
- SLD and PSL are keys in the party list (PartySystem.cs:457-458).
- GermanyTimeline's summary (:351-355) states its scope precisely.

4. THE PIN COMMENT IS NOT A PROBLEM.
- Diffing formation_sweep_before776.txt against formation_sweep_mismatch.txt puts every hunk at lines 94032-94073. All of them are inside "== Poland | seated | negative True" (94031) and "negative False" (94049, the last of 250 blocks).
- The comment's figures match:
  - Negative rule: PiS alone, 194 seats, 44 opposed.
  - Own rule: KO+TD on NL's support, 222 in cabinet, 248 supported.
  - Before the change, both rules gave cabinet 7 on support 16, i.e. PiS+KO+TD on Konf, 416 seats.
- These figures are part of the text the digest pins, so they cannot change while the pin holds. On the next re-pin the entry becomes "Before that…" history, the same pattern as §652, §683, §705 and §766.

5. THE FIX'S PREMISE IS FALSE.
- PolishDeclarationsDiagnostic prints no derived-line bases. Its output (n776b.log:507-521) contains the standing declared facts (:100), the five readings' governments (:151-155) and the line count (:166).
- "galtan gap" appears 0 times in n776a.log, n776b.log and formation_sweep_mismatch.txt.
- The gap exists only inside each RedLine.Basis (CoalitionFormation.cs:956-958), and nothing prints Poland's.
- Even if something did, From records only the gap that broke a pair. The near-miss pairs (PiS–KO, PiS–TD, KO–Konf) would never be printed.

**The skeptic's corrected fix.** 1. Make the pointer true before pointing at it. In PolishDeclarationsDiagnostic (e), right after Derived() is defined, print each derived line with its basis:
   foreach (RedLine l in Derived()) { sb.Append(F("    MEASURED  derived {0}-{1}: {2}\n", parties[l.A].Abbrev, parties[l.B].Abbrev, l.Basis)); }
   If the record must keep the near-miss pairs (PiS–KO, PiS–TD, KO–Konf), print their two gaps against the DerivedRedLines constants as well, because From records nothing for a pair under both gaps.

2. Record §10 (:212-217): replace the figures with references, for example: "What DERIVED draws here is whatever `DerivedRedLines.From` draws on the seed positions (`PartySystems.PolandParties`, TD as its D2 mean) at `DerivedRedLines.SocialGap` (support-blocking) and `DerivedRedLines.IdeologicalGap` (cabinet); `PolishDeclarationsDiagnostic` prints each line with its gap." Keep the point that matters as a dated measurement, not in the present tense: "measured 2026-10-04 (§776): PiS–NL and NL–Konf only; PiS–KO falls under both gaps, so DERIVED on PiS–KO means no line." Delete "measure before wiring (Doubts)", because §776 has done both. Delete or point at the "(351)" / "(259)" claims; the R0/R1 MEASURED lines carry what the formation actually seats.

3. Doubts 1-2 (:259-260): drop the parenthetical figures, "(galtan gap 4.79 < 5.0, lrgen 2.71 < 4.5 on the seed positions)" and "(galtan 2.59, lrgen 1.61)", or replace them with "(§10)".

4. DeclaredRedLines.cs:389-391: replace "every line a party declared on or before the Sejm election of 15 October 2023" with "the lines the record carries for the 2023 lists, declared on or before the Sejm election of 15 October 2023". Add: "the member parties' earlier refusals of PiS and the 2019 lists' lines are recorded there, not carried (its doubts 3 and 12)".

5. Leave the PinnedDigest comment at FormationSweepDiagnostic.cs:129 as it is.

### 7. Doubt 9 refers to a date (2023-09-03) that nothing in the record carries

- **Lens:** sourcing - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:267

**The scenario.** Doubt 9 says "The date 2023-09-03 is derived from 'w niedzielę'". No fact, quote or register row in the record is dated 2023-09-03. The sentence was copied verbatim from the sweep's synthesis (decl_gaps_and_doubts.json), so a reader cannot tell which source it concerns.

**The fix proposed.** Name the source and the claim it dates, or delete the sentence.

**The skeptic's evidence.** I could not refute it. The date points to a source that the record dropped.

1. Nothing in the record carries 2023-09-03 except doubt 9. A grep of G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md for "2023-09-03" finds only line 267. DeclaredRedLines.cs, PolishDeclarationsDiagnostic.cs, raw/declarations_2023/SHA256SUMS.txt and fetch_log.md have no hit for it, for KONF-I21 or for Bydgoszcz.

2. The sentence is the sweep's text, copied exactly. I compared line 267 (with "9. " removed) to item [8] of scratchpad/decl_gaps_and_doubts.json and they are IDENTICAL. Line 257 introduces the list as "The sweep's own list, as its synthesis wrote it". So the copy was deliberate, but that framing does not tell the reader which source the date belongs to.

3. The date dates a source the final record dropped. The earlier draft, scratchpad/record_full.md line 100, had: "- 2023-09-03 *(derived from "w niedzielę" on a page of 4 September)*, Mentzen in Bydgoszcz: "Nic podobnego, my chcemy z nimi walczyć, nie mamy z nimi punktów wspólnych" ... [KONF-I21]". The final §2 restatement list (lines 67-70) keeps 07-16, 08-02, 08-24, 10-10 and 10-11 only. KONF-I21 has no register row and is not in §11's "Kept but not relied on" list.

4. The page exists only outside the repo: PoliSim-captures/sources/poland_declarations_2023/Konf/pch24_mentzen-mozna-zycie-stracic.html (sha256 993630eb...) and CROSS/pch24_2023-09-04_mentzen-nie-wejdzie-w-koalicje-z-pis.html (03970ea8...). Its datePublished is 2023-09-04T07:49:08Z. It reads "zapowiedział w niedzielę w Bydgoszczy" and "Nic podobnego, ... – mówił Mentzen". The Sunday before 4 September is 2023-09-03.

5. A reader can attach the date to the wrong source. The only "w niedzielę" inside the record is [PIS-I1] (line 24, register line 281: "oświadczył w niedzielę"), which is dated 2023-07-23, not 2023-09-03.

6. No check catches it, and nothing else depends on it. PolishDeclarationsDiagnostic reads only the §8 table regex (line 51), the facts' tags (lines 69-76) and the register rows and digests (line 83 on). It never reads §12, so the orphan passes every check. No timeline fact, formation or digest depends on the sentence. Severity stays "note": the problem is that the reference cannot be traced, not that it states something false.

7. A related case with the same cause: doubt 2 (line 260) quotes "nie planujemy przedłużyć o trzecią kadencję rządów PiS-u". That is the draft's [KONF-I20], also dropped from §2, and the final record has the phrase nowhere else.

**The skeptic's corrected fix.** §12 says the list is quoted as the synthesis wrote it, so leave its text alone and add a short note in square brackets after the sentence, marked as the record's own words, that names the source. For example: "[record: this dates the PCh24/PAP page of 2023-09-04, Mentzen in Bydgoszcz, 'Nic podobnego, my chcemy z nimi walczyć, nie mamy z nimi punktów wspólnych'. That restatement of Konf -> PiS/KO is the draft's [KONF-I21]. The record no longer cites it, and the page is held out of tree only, at Konf/pch24_mentzen-mozna-zycie-stracic.html, sha256 993630eb82bccba6eb6f2d7c4ec42aadab8bd96be20f7416267b5cae424ab97c.]"

Also add the page to §11's dropped-findings list, so the restatement is not silently missing.

The other option is to delete the sentence and say in §12 that the dated restatement was dropped.

In the same pass, do the same for doubt 2's "nie planujemy przedłużyć o trzecią kadencję rządów PiS-u" (the draft's [KONF-I20]): name its source (Bosak, 2023-08-24) or mark it as not cited by the record.

### 8. KONF-P1 attribution adds details the saved page does not carry

- **Lens:** sourcing - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:76

**The scenario.** Record lines 76-77 say "the body quotes Przemysław Wipler, its Toruń list leader, on Onet Rano that morning", and fact 10's basis (DeclaredRedLines.cs:417) says "its Toruń list leader Wipler's words". The saved page says only "Przemysław Wipler w Onet Rano został zapytany ..." under the dateline "2 sierpnia, 2023". Neither "that morning" nor the Toruń list position is on it.

**The fix proposed.** Drop "that morning". Either source the list position or drop it.

**The skeptic's evidence.** I could not refute it. The cited page does not carry either detail.

The cited page is ElectionsData/poland/raw/declarations_2023/Konf/konfederacja_koalicja-z-pis-czy-z-platforma-z-nikim.html, at the register's digest cac1f618…
- Its dateline (line 904) is "2 sierpnia, 2023". The JSON-LD has datePublished 2023-08-02T12:19:38+00:00. That is when the page was published, not when the interview took place.
- Its whole body (line 1004 onward) is: "Przemysław Wipler w Onet Rano został zapytany czy gdyby z wyników wyborów wynikało, że jest możliwa koalicja rządów PiS i Konfederacji to czy byłby za taką koalicją rządową." Then comes the answer "– Jasno i klarownie mówimy: nie będzie z nikim koalicji. …", ending "(…)".
- There is no list position anywhere ("jedynk", "listy", "okręg", "kandydat": 0 hits). The only "toru" strings are a sidebar link to an unrelated post, "tych-podatkow-nie-bedzie-to-efekt-deklaracji-torunskiej".
- Nothing gives the interview's day or time. "Onet Rano" is just the programme's name.

So two places attribute to [KONF-P1] details that page does not have:
- the record, lines 76-77: "the body quotes Przemysław Wipler, its Toruń list leader, on Onet Rano that morning [KONF-P1]";
- fact 10's basis, DeclaredRedLines.cs:417: "its Toruń list leader Wipler's words".

The same pattern also appears in doubt 6 (line 264), "a list candidate's (Wipler)", which is also not on the page.

Why this stays a note:
- **Both details are true.** The sweep's own out-of-tree captures support them, but nothing cites those captures:
  - Konf/youtube_onetrano_2023-08-02_4M4_wKRpARg.html is on the Onet Rano channel, uploadDate 2023-08-02T01:51:53-07:00 (10:51 CEST), tagged TPLID:live and TPLID:fragment.
  - Konf/dorzeczy_wipler-powiem-cos-o-czym-nie-mowilem.html (2023-08-07) says "Były polityk PiS będzie \"jedynką\" partii w Toruniu" and dates the announcement 25 July.
  - Konf/radiopik_wipler-ani-z-pis-ani-z-po.html says "Przemysław Wipler, jedynka Konfederacji w Toruniu".
- **The timeline is unaffected.** Facts 10-12 are dated 2023-08-02, the party's own publication date, which is on the page. No formation changes.
- **The basis text reaches the player only rarely.** It shows through FormationProposal.cs:171 ("refuses: a red line falls inside this cabinet - " + Basis), which FormationSheet.cs:310 prints on the answer slip. That happens only if the player proposes a cabinet holding both Konfederacja and TD.
- **No check catches it, and nothing would need re-pinning.** PolishDeclarationsDiagnostic checks source tags and file digests, not the prose. The FormationSweep digest hashes outcomes and the A-B pairs of blocked lines, not basis text.

**The skeptic's corrected fix.** The simplest fix is to drop both details, because the page names the programme and not the day.
- Record, lines 76-77: replace "the body quotes Przemysław Wipler, its Toruń list leader, on Onet Rano that morning [KONF-P1]" with "the body quotes Przemysław Wipler on Onet Rano [KONF-P1]".
- DeclaredRedLines.cs:417: replace "The second quote is its Toruń list leader Wipler's words" with "The second quote is Przemysław Wipler's words on Onet Rano".
- Doubt 6 (line 264): change "a list candidate's (Wipler)" to "Wipler's" for the same reason.

If the list position and the day are wanted, cite the sweep's own captures rather than hunting for new ones:
- Add register rows for Konf/youtube_onetrano_2023-08-02_4M4_wKRpARg.html (sha256 a9645d47…, uploadDate 2023-08-02T01:51:53-07:00) and Konf/dorzeczy_wipler-powiem-cos-o-czym-nie-mowilem.html (sha256 34e9d4f6…, 2023-08-07, "jedynką" partii w Toruniu).
- Copy both files into raw/declarations_2023/Konf/ at those digests, so the diagnostic's check (c) holds them.
- Note that the Do Rzeczy and Radio PiK pages are in §11's dropped set ("Wipler's own interviews"). They also report his "dogadać się … z obecną opozycją. Ale bez Lewicy" remark from the same interview, which the party page cut. Dropping the two details avoids opening that.

Neither option needs a sweep re-pin.

### 9. The 'record's majority' was measured on a chamber play never forms; on the game's own 2023 count the wired reading (R1) seats PiS+KO with TD's support

- **Lens:** wiring - **reviewer:** defect - **skeptic:** defect
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PolishDeclarationsDiagnostic.cs:159

**The scenario.** In play the record's 2023 seats (PiS 194, KO 157, TD 65, NL 26, Konf 18) are never formed. Both Polish governments of record are cabinet-sourced (WorldClock.cs:249-250), so SeatedGovernment installs them (SeatedGovernment.cs:60-63). The only playable Polish start is 2023-02-19, and that game forms its OWN count after its own vote (GameController.cs:6400-6403 -> OfElection -> ForDate on 2023-10-15, which is exactly R1). The seed-777 no-policy count is PiS 200, KO 123, TD 78, NL 45, Konf 14 (n773f.log:2216).

I applied the code's own score to that count (CoalitionFormation.cs:446-448: 0.5*cohesion + 0.3*100*seats/460 + 0.2*100*power). Inputs were the cohesions the pinned sweep prints and the power as normalized Banzhaf. As a check, my script reproduces all six pinned R1 scores on the record's count to 6 decimals.

The same six cabinets are viable, because SupportersOf is position-only and every one still clears 231. Every option fails WouldHold (KO alone on TD+NL pays KO 1.0), so the defection loop is abandoned (CoalitionFormation.cs:330-341) just as it is on the record's count, and the score decides. Result: PiS+KO on TD's support 66.204 > KO+TD+NL 64.571 > KO+TD on NL 63.516. On the record's count KO+TD on NL won by only 0.446.

So after this change a Polish game's 2023 election would likely seat PiS+KO led by PiS, with TD supporting. Before, R0 seated PiS+KO+TD (film767pl.log:11929). PiS+KO is the outcome the record's §10 warns 'neither the record nor the outcome shows'. Only R3 (a PiS-KO line) seats KO+TD+NL on this count, so doubt 1 decides the game's 2023 government. Yet COMPLETED.md:37160-37161 tells Elias 'Measured: no 2023 effect', and check (f)'s text says 'as the game now forms it'. This was computed from the code, not run (read-only).

**The fix proposed.** Add the game's own 2023 count (PollingDayDiagnostic's seed-777 Polish path, or its seats) as a measured chamber under R0-R4 in PolishDeclarationsDiagnostic. Reword check (f), because the seed-seat chamber is never formed in play. Correct the record §12 (lines 248-249) and COMPLETED §776 (37127, 37160-37161), and put doubt 1 to Elias again with this measurement.

**The skeptic's evidence.** I could not refute it. Every step of the scenario holds in the code, and I reproduced the result by running the formation code itself, not just repeating the arithmetic.

**1. In play, the record's 2023 seats never go through the formation.**
- `WorldFactory.cs:1092` gives every country `GovernmentRecord.AtStart`.
- Both Polish governments of record are cabinet-sourced (`WorldClock.cs:249-250`), so `SeatedGovernment.cs:60-63` installs them and no formation runs.
- Only the player's country holds elections (`GameController.cs:6687`, `NationalElection.Run(PlayerCountryId…)`).
- `TryAiMotion` returns early for any country whose rules are not the Riksdag's or the Bundestag's (`SimulationManager.cs:3018-3020`). Poland's are `Rules.Unsourced` (`ConfidenceProcedure.cs:34`).
- The election-night reference uses the government of record, not a formation.

**2. A Polish game forms its own count.**
- Poland's start is 2023-02-19 (`n773f.log`), so the seated chamber then is the 2019 one.
- On 2023-10-15, `ParliamentSystem.SetSeatsFromElection` writes the game's count (`GameController.cs:6691`).
- `ResolveElectionVerdict` calls `DeclarationReading.OfElection` (`GameController.cs:6400`). Since `HasTimeline(Poland)` is now true, that is a dated reading (`DeclaredRedLines.cs:659-662`): `ForDate` on 2023-10-15, which is the derived lines plus the 10 open facts, exactly R1. The arguments are positive investiture, no in-or-against rules and `joint` null.
- `RoundsApply(Poland)` is false (`SimulationManager.cs:3223`), so `formedView` is installed directly (`GameController.cs:6429`).

**3. The diagnostic measures a different chamber.**
- Check (f) forms `seats = parties.Select(p => p.SeedSeats)` (`PolishDeclarationsDiagnostic.cs:106`), the record's count, which play never forms.
- The game's own count is PiS 200, KO 123, TD 78, NL 45, Konf 14 (`n773f.log:2216`, the seed-777, no-policy Polish path). The declarations do not feed the vote model, so §776 does not change this count.

**4. Replica run.** I compiled a scratchpad console app against the repo's own `CoalitionFormation.cs`, rebuilding the compatibility matrix and the TD mean exactly. Nothing in the repo was written.
- It reproduces the pinned record-count scores to the last digit: R1 KO+TD on NL 67.44984866045789, R0 PiS+KO+TD 81.92351155150828.
- On the game's count, negotiating power is PiS 0.5, KO, TD and NL 1/6 each, Konf 0.
- **R0:** PiS+KO+TD on Konf's support. This matches `film767pl.log:11929`.
- **R1 (what the game now reads):** PiS+KO on TD's support, score 66.20355049337165. It beats KO+TD+NL (64.57112202091494) and KO+TD on NL (63.51618087896402). The same six cabinets are viable as on the record's count; with KO alone on TD+NL among them, no option survives the defection check, so the score decides.
- **R2:** PiS+KO alone.
- **R3 and R4:** KO+TD+NL.

**5. It is not one unlucky count.**
- Over 43,335 counts near the game's (PiS 190-210, KO 115-135, TD 70-85, Konf 10-18, NL 35-55), R1 seats PiS+KO on TD in 42,800.
- R3 seats KO+TD+NL in 42,670 of them.
- On the record's count, KO+TD on NL wins by only 0.446.

**6. The claims are false for play.**
- `COMPLETED.md:37127` says the doubts "change no 2023 formation". Line 37145 says the 2023 chamber "as the game now forms it" seats KO, TD and NL. Lines 37160-37161 say "Measured: no 2023 effect".
- The record says the same at lines 248-249. Check (f)'s text says "as the game now forms it" (`PolishDeclarationsDiagnostic.cs:170`).
- On the game's own count, doubt 1 decides between PiS+KO, which the record's §10 says neither the record nor the outcome shows, and KO+TD+NL, the cabinet of record. Doubt 2 changes only TD's support role.

**Mitigating points:**
- The wiring reads R1 exactly as ruled.
- Play does not get worse: R0 already kept PiS in government.
- The record's §12 hedges with "they bear on other counts", but the "owed to Elias" list in COMPLETED drops that hedge.

**Severity:** defect, because the measured conclusion behind the change and its ruling request is wrong for the only 2023 formation the game actually runs. The code itself has no bug.

**The skeptic's corrected fix.** 1. In `PolishDeclarationsDiagnostic`, add a second measured chamber: the game's own polling-day count. Move `PollingDayDiagnostic`'s Polish path (seed 777, no policy, the `TryPredictShares` and `NationalElection.Run` steps) into a shared helper so these seats are computed when the diagnostic runs, never typed in. Print R0 to R4 on that count as MEASURED lines, and do not assert that it seats the record's majority.
2. Reword check (f) as a backtest on the record's seed seats: "the 2023 chamber of record, formed with the game's reader, seats the record's majority". Replace "as the game now forms it". Optionally, also check that `DeclarationReading.OfElection(Poland, 2023-10-15).Lines(...)` equals R1 line for line, since that is the path the game's election actually takes.
3. Correct `coalition_declarations_2023.md` §12 (lines 248-249) and `COMPLETED.md` §776 (lines 37127, 37145, 37160-37161). Say that the readings change nothing on the record's count, which they win by a narrow margin. On the game's own count, R1 seats PiS+KO on TD's support, R2 seats PiS+KO alone, and R3/R4 seat KO+TD+NL. So doubt 1 decides a Polish game's 2023 government, and doubt 2 decides only TD's support role.
4. Put doubt 1 back to Elias with both measurements, and say how sensitive the outcome is to the count. Keep every figure in the diagnostic's printed output and in `COMPLETED.md`, not in source comments (the claim convention).

### 10. 'The pairs left DERIVED say DERIVED on every formation' is false; the wiring removed Poland's only DERIVED caveat

- **Lens:** wiring - **reviewer:** defect - **skeptic:** defect
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:255

**The scenario.** Only two formation surfaces ever say DERIVED: ElectionNightScreen.cs:713-716 and GameController.cs:6437-6439. Both are gated on !DeclarationsSourced, which is DeclaredRedLines.IsSourced(country) (GovernmentFormation.cs:294, 425), and that is now true for Poland (DeclaredRedLines.cs:36).

Poland has no election night (ElectionNightFromModel.cs:38-39). Its verdict is applied at once (GameController.cs:6361) and is shown only as a game-over reason. So after a Polish election nothing anywhere says any line is derived.

The eight pairs §10 lists (PiS-KO, KO-TD, ...) have no RedLine at all; DerivedRedLines draws only PiS-NL and NL-Konf. So not even the data could 'say DERIVED' for them.

The same false claim appears at record lines 10 and 210 and at COMPLETED.md:37152. Doubt 14 (whether the derived pairs also carry LOW CONFIDENCE) is put to Elias on the premise that they already say DERIVED. If he rules on that premise, the pairs stay unlabelled everywhere.

**The fix proposed.** Correct the record (lines 10, 210, 255) and COMPLETED §776's 'E2's interim flag' paragraph: since §776 no Polish surface labels derived pairs, because the IsSourced-keyed caveat is suppressed for Poland. Put doubt 14 to Elias on that footing, or build a per-reading label.

**The skeptic's evidence.** I could not refute it. Every step of the scenario holds in the current tree, and no staged or unstaged diff touches the captions involved. This is a fault in the documentation only: the code does what ruling E2 asks, because the interim flag was only required until the sourcing.

1. The only player-facing DERIVED caveat is keyed on a whole-country flag, and that flag is now true for Poland.
   - DeclaredRedLines.cs:36: `IsSourced(...) => ... || country == CountryId.Poland;`
   - GovernmentFormation.cs:294 and :425: `declarationsSourced = DeclaredRedLines.IsSourced(country...)`
   - A grep over Assets/Scripts finds `DeclarationsSourced` read in only two places:
     - ElectionNightScreen.cs:713-716: `if (!government.DeclarationsSourced) { Wrapped(parent, "formed on DERIVED red lines only - this country's declared refusals are not sourced", ...) }`
     - GameController.cs:6437-6439: `sourcedNote = government.DeclarationsSourced ? string.Empty : " (Formed on DERIVED red lines only ...)"`. This note is used only in the in-office sentence (6442) and the out-of-parliament sentence (6454), not the out-of-office one (6461-6463).
   - So since §776 both captions are suppressed for Poland.

2. Poland has no election night.
   - ElectionNightFromModel.cs:38-39: `Available(country) => (country == CountryId.Sweden || country == CountryId.Germany) && ...`
   - GameController.cs:6494-6499 returns early, so `_electionNight` stays null, and 6361-6366 call `ApplyElectionVerdict()` at once.
   - 6716-6722: the sentence is kept only as `_gameOverReason` when it ends the game; otherwise it is set to null.
   - Before §776, then, a Polish player saw the caveat only as the end of the out-of-parliament game-over sentence. Now nothing shows it.

3. No other play surface labels a pair as DERIVED.
   - ConfidenceProcedure.cs:34 gives Poland `Rules.Unsourced`, so `RoundsApply` (SimulationManager.cs:3223) is false and the formation sheet never opens.
   - GameController.CampaignDeclared.cs:15-19 and :189 show the DECLARED block only ("THE MODEL'S DISTANCES BETWEEN PARTIES ARE NOT SHOWN HERE").
   - The per-line "on distance" labels (GameController.CampaignCoalition.cs:208, 238, 326) are reachable only through `SetCampaignCoalitionScreen`, whose one caller is Testing/UiScreenshotDriver.cs:5361. That screen is harness-only.

4. The eight pairs in doubt 14 have no RedLine at all.
   - CoalitionFormation.cs:955: `if (!socialBreak && !ideologicalBreak) { continue; }`
   - n776a.log: "the game's own reading of the seated chamber today: 2 line(s) - PiS-NL Derived support-blocking; NL-Konf Derived support-blocking"
   - n776b.log: "the seated chamber's 12 lines are the derived ones and the declarations of its polling day"
   - So for PiS-KO, PiS-MN, KO-TD, KO-NL, KO-MN, TD-NL, TD-MN and NL-MN there is no line object that could carry a "DERIVED:" basis.

5. The false claims, verbatim:
   - Record line 10: "What is NOT here stays DERIVED and is said so on every formation." This is copied from germany/coalition_declarations_2025.md:5, which has been equally false since §705.
   - Record line 210: "A formation that turns on them says so."
   - Record line 255: "The pairs left DERIVED (§10) say DERIVED on every formation; whether they also carry the flag is doubt 14's question."
   - COMPLETED.md:37152: "The pairs left DERIVED say DERIVED on every formation."
   - COMPLETED.md:37159: "...also carry the LOW CONFIDENCE flag (doubt 14)".

Why I keep it at defect even though it is text only: the repo's claim convention, which outranks every other rule, forbids exactly this kind of false statement about what the code does. Here that statement is the premise of a ruling still owed to Elias. If he declines the flag because he believes the pairs are already labelled DERIVED, nothing anywhere marks them, and the record goes on saying something does.

**The skeptic's corrected fix.** Text only; no code change is needed for the ruling as given.

1. ElectionsData/poland/coalition_declarations_2023.md
   - Line 10: replace "and is said so on every formation" with: "No surface marks it: the formation's only DERIVED caveat covers the whole country and is keyed on `DeclaredRedLines.IsSourced`, which Poland meets since §776."
   - Line 210: delete "A formation that turns on them says so.", or replace it with "Nothing in play marks a formation that turns on them (doubt 14)."
   - Line 255: replace with: "No surface labels the pairs left DERIVED. The night's caption and the verdict's note are keyed on IsSourced, and Poland is now sourced. The derived rule also draws no line on the eight pairs, so no line carries a DERIVED basis for them. Whether they are marked at all, with LOW CONFIDENCE or otherwise, is doubt 14's question."

2. COMPLETED.md §776
   - Line 37152: reword the same way.
   - Line 37159: change "also carry" to "carry any mark (none marks them now)".

3. Put doubt 14 to Elias on that footing. If he wants a mark, it cannot be keyed on IsSourced. It needs a reading-level state that the night and the verdict can print, for example "rests on a pair with no declared line". It also reaches no Polish player until D-PL's night is built, because the verdict alone is shown only on game over.

4. Correct the identical pre-existing sentence in ElectionsData/germany/coalition_declarations_2025.md:5, which has been false since §705 for the same reason.

### 11. Headers and printed text still assert only Sweden (or Sweden and Germany) are sourced

- **Lens:** wiring - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/DeclaredRedLines.cs:13

**The scenario.** The DeclaredRedLines class header still says 'only one of the six countries has its declarations on disk' (line 13). Lines 15-21 say sourced for Sweden and Germany, and that for every other country `For` returns the derived lines alone and `IsSourced` returns false. Line 36 of this same diff contradicts that.

Other stale text:
- GovernmentFormation.cs:26-27: declarations 'exist on disk for Sweden only ... every other country runs on DERIVED lines alone'.
- GameController.CampaignDeclared.cs:32: the chip is offered on 'Sweden's timeline'.
- OfficeTestDiagnostic.cs:37, and the text it prints at :549-552: 'DECLARED RED LINES ARE SOURCED FOR SWEDEN ONLY. Everywhere else ... DERIVED lines alone'. It prints this directly under its own table row 'Poland SOURCED KO+TD+NL' (n776b.log:657 and :723; sim776.log:25619).
- DeclaredRedLines.cs:277: 'No runtime surface reads the timeline yet'.

The Germany staleness predates this change (§705); §776 extends it to Poland.

**The fix proposed.** Name Poland (§776) in these headers, or replace the country lists with a pointer to IsSourced/HasTimeline. Make OfficeTestDiagnostic's footer list the sourced countries by calling IsSourced instead of fixed text.

**The skeptic's evidence.** I could not refute it. Every cited line exists and is now false. Nothing at runtime is wrong: all the readers call IsSourced or HasTimeline, so the computed state is correct. The defect is in the text.

The line §776 itself made false, in the file it edited:
- DeclaredRedLines.cs:15-20 says "SOURCED FOR SWEDEN, in two vintages, and for GERMANY since §705 ... For every other country `For` returns the DERIVED lines alone and `IsSourced` returns false".
- Line 36 of the diff now reads `IsSourced(...) => ... || country == CountryId.Poland`.
- Line 53 now reads `if (country == CountryId.Germany || country == CountryId.Poland) { return ForDateSourced(...) }`, so For(Poland) returns derived lines plus declared ones.
- In the same file, §776 updated the HasTimeline docstring (:438), the ForDateSourced docstring (:466) and IsSourced's trailing comment to name Poland. It left this header alone.

Stale before §776 (since §705 or §657); §776 adds Poland as one more counterexample:
- DeclaredRedLines.cs:12-13, "only one of the six countries has its declarations on disk" (f7264d9b, 2026-08-31). It is now three, and it is a transcribed count.
- GovernmentFormation.cs:26-27, "exist on disk for Sweden only ... every other country runs on DERIVED lines alone". Yet :294 and :425 set `declarationsSourced = DeclaredRedLines.IsSourced(...)`, which is now true for Poland.
- GameController.CampaignDeclared.cs:32, "(Sweden's timeline, §621)". Line 34 gates on `HasTimeline(_playerCountry.Id)`, so the DECLARED chip now also opens in a Polish campaign.
- DeclaredRedLines.cs:277, "No runtime surface reads the timeline yet" (24df659c). Already false through §657's StandingOn page and §705's German ForDateSourced. Line 53 now adds Poland's formation as another reader.
- OfficeTestDiagnostic.cs:37 (doc comment) and :549-552 (printed text), "DECLARED RED LINES ARE SOURCED FOR SWEDEN ONLY. Everywhere else ... DERIVED lines alone". Meanwhile :65 and :81 print `sourced ? "SOURCED" : "derived only"` from IsSourced. OfficeTestDiagnostic runs in the bar (CheckSuite.cs:545).
  - n776b.log:657 prints "Poland       SOURCED          KO+TD+NL" and :723 prints the SWEDEN ONLY footer.
  - sim776.log:25619 and :25685 show the same pair.
  - Before §776 the row was "Poland derived only" (sim770d.log:25629), so §776 flipped the row and left the footer contradicting it.

No check catches these. CommentClaimCheck matches only backticked `Type.Member` names (CommentClaimCheck.cs:43). CLAUDE.md's claim convention ("the code can change freely and no document becomes wrong"; a derived claim must be REFERENCED or GENERATED, never transcribed) is broken by each of these lines.

Grading it minor:
- No behaviour, digest or verdict changes.
- The diagnostic's printed text contradicts its own table, and a source header now states something false.
- One part is this change's own omission (the header at :15-20, plus the Poland row now printed under the "SWEDEN ONLY" footer). The rest is older staleness that §776 adds to.

**The skeptic's corrected fix.** Follow the claim convention: point to the predicate instead of listing countries.

1. DeclaredRedLines.cs class header:
   - Line 13: drop the count ("only one of the six").
   - Lines 15-20: say the lines are sourced where `DeclaredRedLines.IsSourced` holds, each country's record being ElectionsData/<country>/coalition_declarations_*.md, and that for every other country `For` returns the DERIVED lines alone. Keep the Sweden vintage sentence (K-1).
2. DeclaredRedLines.cs:277: delete "No runtime surface reads the timeline yet", or name the readers by reference (`StandingOn` for the D-PS page, `ForDateSourced` from `ForSourced`).
3. GovernmentFormation.cs:26-27: "Declared red lines exist on disk only where `DeclaredRedLines.IsSourced` says so; every other country runs on DERIVED lines alone."
4. GameController.CampaignDeclared.cs:32: "only where the country's declarations are dated (`DeclaredRedLines.HasTimeline`, §621)."
5. OfficeTestDiagnostic.cs:
   - :37: change the doc to "sourced only where IsSourced holds, and the table says so per country".
   - :549-552: generate the footer instead of hard-coding it. Collect the countries in `world.Countries` where `DeclaredRedLines.IsSourced(c.Id)`, already computed per row at :65, and print "DECLARED RED LINES ARE SOURCED FOR {list}. Everywhere else ...".

Items 1-4 are comment-only. Item 5 is an Editor diagnostic text change, so it falls in the cheap-bar tier and no digest moves.

### 12. The run-up declarations page cannot open in a Polish game; once it can, its 'would support' tag misstates Poland's cabinet-only lines

- **Lens:** wiring - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/UI/GameController.CampaignDeclared.cs:59

**The scenario.** DeclaredPageAvailable (:33-34) needs _liveCampaignOpen, which needs a PlayerPreCampaign or PlayerCampaign (GameController.Campaign.cs:152). LiveCampaignSetup.TryFor stages only Sweden and Germany (LiveCampaignSetup.cs:148, 176). AdvancePreCampaign returns without one (SimulationManager.cs:4318-4322), and PollingDayDiagnostic.cs:180 asserts none is staged for Poland. So COMPLETED.md:37134, 'The run-up's declarations page now has Poland's facts to show', describes a page a Polish game cannot open.

Once Poland's campaign is staged, SayDeclared and DeclaredSegments will tag every cabinet-only fact 'would support', under the slip 'A PARTY SAID THIS'. Examples: 'Trzecia Droga will not sit with PiS · would support', and the same for PiS->Konf, Konf->TD/NL/MN, TD->Konf and NL->PiS. The record says of these 'support is not addressed', and TD campaigned to remove PiS. The tag is right for Sweden's 2022 lines ('while accepting its support') but not for Poland's. The harness-only coalition screen does the same (CampaignCoalition.cs:288).

**The fix proposed.** Reword COMPLETED's line: the page sits behind a staged campaign, which Poland lacks. Before staging Poland's campaign, tag a cabinet-only fact by what its record says (for example 'support not addressed') instead of 'would support', or store a support wording on each DatedFact.

**The skeptic's evidence.** The first half is confirmed: the page cannot open in a Polish game.
- GameController.CampaignDeclared.cs:33-34: `DeclaredPageAvailable() => _liveCampaignOpen && _playerCountry != null && DeclaredRedLines.HasTimeline(...)`.
- The page is drawn only behind that gate (GameController.cs:2797: `_campaignScreen.HasValue && _liveDeclaredOpen && DeclaredPageAvailable()`), and the DECLARED chip sits on the HQ masthead (Campaign.cs:305).
- `_liveCampaignOpen` is set in one place, `OpenLiveCampaign` (Campaign.cs:145). It returns early when `BuildLiveCampaignSnapshot` is null, which happens whenever both PlayerCampaign and PlayerPreCampaign are null (Campaign.cs:152).
- Both properties have private setters. Every assignment (SimulationManager.cs:4286, 4333, 4530, 4557) sits behind `LiveCampaignSetup.TryFor`, which stages only Sweden and Germany (LiveCampaignSetup.cs:148) and otherwise returns false with "no campaign is staged for {country}" (:176). AdvancePreCampaign and AdvanceCampaign return on that false (SimulationManager.cs:4318-4322, 4269-4274).
- PollingDayDiagnostic.cs:180 asserts `!staged` for a Polish game walked through 15 Oct 2023.

So two new §776 lines in COMPLETED.md describe a page no Polish game can reach: line 37134 ("The run-up's declarations page now has Poland's facts to show.") and the tier line at 37156 ("the run-up page has them to show"). §776 lifted only the HasTimeline half of the gate; the campaign half still shuts Poland out. These two lines are the real defect, and they are in the record only. No code path does anything wrong.

The second half is real in substance but latent, and the finding overstates it in two places:
- SayDeclared:59 tags every pair line that is neither OneWay nor BlocksSupport "would support". Seven Polish facts standing on 2023-10-15 are like that: PiS>Konf, Konf>TD/NL/MN, TD>PiS, TD>Konf and NL>PiS. Their bases say "support is not addressed" or "A cabinet only", and the record's doubt 2 notes that TD voted against Morawiecki. They would sit under the DECLARED slip "A PARTY SAID THIS". Sweden's earlier cabinet-only lines (M/KD/L>SD) say "while accepting its support", and Germany's lines all block support, so §776 brings in the first lines this tag would misstate.
- This cannot happen today, because the page is unreachable for Poland.
- DeclaredSegments (:104-118) makes the sentence only. The tag comes from SayDeclared alone (drawn via :224-225 and :136).
- The coalition screen is not a Poland path. CampaignCoalition.cs:288 is filled only by UiScreenshotDriver.BuildCoalitionState: Sweden 2022 through CoalitionFilm.AllLines/DerivedOnly (CoalitionFilm.cs:48-55), or a made-up A/B/C chamber whose lines all block support.

**The skeptic's corrected fix.** 1. In COMPLETED.md §776, reword the "Wired" bullet at line 37134 and the tier line at 37156 so they claim no visible effect. For example: "The run-up's declarations page would read Poland's timeline, but no Polish run-up or campaign is staged (LiveCampaignSetup stages Sweden and Germany only), so no Polish game shows the page yet." Change the tier parenthesis to "(the formation reads Poland's declarations)".
2. Add a TRACKING line for the future item that stages Poland's campaign: before the page opens for Poland, its pair-line tag must follow the record and not the line's strength. SayDeclared's "would support" fits Sweden's 2022 wording but asserts support for Poland's seven cabinet-only facts, where the record says support is not addressed. Either store a support wording on each DatedFact or use a neutral tag such as "cabinet only" where the record is silent.
3. No code change is needed in §776, and the coalition screen needs nothing because it is never fed Poland.

### 13. The Polish states film forms the game's 2023 government in its warm-up; §776 neither re-ran nor mentioned it

- **Lens:** wiring - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Testing/UiScreenshotDriver.cs:527

**The scenario.** -shotstates warms up with AdvanceDays from Poland's start, and its warm-up holds the 15 Oct 2023 election without a night (GameController.cs:6339 HoldElectionWithoutTheNight). film767pl.log:11898-11947 logs 'the government is PiS+KO+TD led by PiS; the player's PiS is PrimeMinister', with '(Formed on DERIVED red lines only ...)'.

Every later frame is shot on that government: Desk, Statistics, Decisions, Budget and Policy, about 170 frames, plus the 2055 election at :26525. After §776 that formation reads R1. If the film's count is the seed-777 one, Finding 1's computation gives PiS+KO on TD's support, which changes the cabinet, the support, the portfolios and the agreements.

§776 records no film, and no Polish film log is newer than the wiring (the latest is film767pl, 2026-10-03).

**The fix proposed.** Re-run the Polish -shotstates film and diff it against film767pl, or record in §776 that it moves and why.

**The skeptic's evidence.** The movement is real. Half of the premise is wrong: the film was re-run.

TRUE, the film forms the 2023 government in its warm-up and §776 moves it:
- UiScreenshotDriver.cs:527 is `else { AdvanceDays(controller, _countryId); }`. AdvanceDays (3068-3110) calls `HoldElectionWithoutTheNight` when `sim.PollingDayToday`.
- GameController.cs:6339 calls ResolveElectionVerdict. That uses `DeclarationReading.OfElection(PlayerCountryId, latest.Date)` (6400). Poland is `Rules.Unsourced` (ConfidenceProcedure.cs:34), so `RoundsApply` is false and the formation is stored directly (6430).
- With §776, `HasTimeline(Poland)` is true. OfElection is now dated, and ForDate adds the facts standing on 2023-10-15. Among them is `TD -> PiS` cabinet, so rule 2 refuses PiS+KO+TD outright (CoalitionFormation.cs:255).

REFUTED, "§776 neither re-ran ... no Polish film log is newer than the wiring (the latest is film767pl)":
- unity_launched.tsv shows `real776`, UiScreenshotCapture.Run, 2026-10-05T01:21. Its command line is `-shotcountry=Poland -shotwidth=1280 -shotheight=720 -shotstates`. Before it ran sim776, cheap776 and drys776.
- Every §776 file was last modified before that run: DeclaredRedLines.cs 10-04 22:26, the diagnostics by 22:29.
- real776.log ends "179 captured, 0 failed ... exiting 0".
- Even before the wiring, film767pl is not the latest Polish film: film768pl, film770pl and film770pl2 (10-04 01:38) came after it.

The re-run shows the move:
- real776.log:13073 "the government is PiS+KO led by PiS".
- real776.log:13091 verdict "a PiS+KO government" (the DERIVED note is gone).
- real776.log:25569 "AGREEMENT: ... owed to TD".
- 27926 shows the 2055 election also forms PiS+KO.
- The film before the wiring (film770pl2:12410, 24906) has PiS+KO+TD, with the agreement owed to Konf.
- Same count both times. real776_07a_politics_parliament.png shows PiS 200, KO 123, TD 78, NL 45, Konf 14. The support-agreement mark moves from Konf's to TD's.
- "About 170 frames" overstates what is visible. 133 of 179 frames are byte-identical to film770pl2, including 01d_desk_held and 07c_politics_cabinet. The 46 that differ mix in E1's and E2's changes.

TRUE, nothing records the move:
- COMPLETED.md §776 (37090-37167) never mentions the film. Its tier line still holds the placeholders REVIEWTIER776 and "BARS".
- The headline says "THE 2023 CHAMBER NOW SEATS THE RECORD'S MAJORITY". The owed list says doubts 1 and 2 have "Measured: no 2023 effect".
- Both were measured on the seed seats only (PolishDeclarationsDiagnostic.cs:106, `p.SeedSeats`).
- On the game's own 2023 count, a PiS-KO line (doubt 1) would refuse the very cabinet the game forms. TD -> PiS read as support-blocking (doubt 2) would end TD's support agreement.

Re-grade: minor. The bars ran green, so this is not a code defect. It is a record that would tell Elias the played Polish game seats the record's majority, when its first government is PiS+KO.

**The skeptic's corrected fix.** No re-run is needed: real776 (Poland 1280, -shotstates, 0 failed) and drys776 already ran on the wired tree. What §776 still owes is in the record:

1. Fill its BARS line with those runs.
2. State that the game's own 2023 count (PiS 200, KO 123, TD 78, NL 45, Konf 14) now forms PiS+KO with TD's support agreement. The films before the wiring formed PiS+KO+TD with Konf's agreement. This holds for the warm-up's government, the re-seat and the 2055 election.
3. Change the owed list's "Measured: no 2023 effect" for doubts 1 and 2 to say it was measured on the seed seats only. On the played count:
   - a PiS-KO line would refuse the cabinet the game forms;
   - TD -> PiS read as support-blocking would end TD's support agreement.

### 14. The 'game reads R1' and 'as the game now forms it' checks read ElectionVintage.Seated at the ambient epoch, not the election by name or the game's own election path

- **Lens:** diagnostic - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PolishDeclarationsDiagnostic.cs:162

**The scenario.** Lines 162 and 167 call DeclaredRedLines.For(CountryId.Poland, parties) and InOrAgainstFor(CountryId.Poland, parties) with the default Seated vintage. Seated resolves through WorldClock.Resolve to SeatedVintage(Poland, SimulationManager.EpochDate), and the EpochScope at line 103 only restores the epoch; it never sets one. Under the suite's default epoch (2026-10-01) Seated resolves to Poland2023, polling day 2023-10-15, so the n776b run is genuine. Under a Polish game's epoch it breaks. WorldClock.StartDate(Poland) is the pre-campaign start, about 34 weeks before 2023-10-15, which falls inside the 2019 chamber's term. There Seated resolves to Poland2019, ElectionDayOf gives 2019-10-13, and no Polish fact stands on that day. For() then returns the 2 derived lines against the 12 recorded ones, so 'the game reads R1' FAILS, and the formed check forms R0 (PiS+KO+TD on Konf) and FAILS too, even though the game is right. A Polish epoch could leak from an earlier check that applies the Polish start without EpochScope (§618's bar found one such leak), or from an Editor session that opened a Polish game. The reverse also holds: the check never exercises the path a Polish game actually runs. Poland has no Speaker's round, so the game's election night calls GovernmentFormation.Form/ViewOf(country, DeclarationReading.OfElection(Poland, 2023-10-15)) (GameController.cs:6400, 6592), which reads ForDate and InOrAgainstAt. The two agree in content today only. CLAUDE.md (THE WORLD CLOCK): "ElectionVintage.Seated resolves at the epoch; a check pins an election by name." GermanFormationDiagnostic (c) instead calls ApplyStart and goes through ViewOf(country, OfElection(...)).

**The fix proposed.** Pin the election. Read DeclarationReading.OfElection(CountryId.Poland, PollingDay).Lines(...) and .Platforms(...), or ForDate/InOrAgainstAt on PollingDay, or For(..., ElectionVintage.Poland2023). Better still, assert on the game's own entry: GovernmentFormation.ViewOf(CountryId.Poland, abbrevs, seedSeats, null, ElectionVintage.Poland2023), or a world under ApplyStart(Poland) with ViewOf(country, OfElection(Poland, PollingDay)), as GermanFormationDiagnostic (c) does.

**The skeptic's evidence.** THE MECHANICS HOLD. PolishDeclarationsDiagnostic.cs:103 `using (SimulationManager.EpochScope())`, and SimulationManager.cs:249 `EpochScope() => new EpochRestore(EpochDate)`, which only restores and never sets. :162 `List<RedLine> gameReads = DeclaredRedLines.For(CountryId.Poland, parties);` and :167 `inOrAgainst: DeclaredRedLines.InOrAgainstFor(CountryId.Poland, parties)` both use the default Seated vintage. DeclaredRedLines.cs:52-53 `vintage = WorldClock.Resolve(country, vintage); if (Germany || Poland) return ForDateSourced(country, parties, WorldClock.ElectionDayOf(country, vintage));`. WorldClock.cs:190 resolves Seated with `SeatedVintage(id, SimulationManager.EpochDate)`. StartDate(Poland) is CampaignCalendar(2023-10-15).PreCampaignStart = 2023-10-15 minus 8 weeks minus 26 weeks = 2023-02-19. WorldClock.cs:149 has the Poland2019 chamber convened 2019-11-12 until 2023-11-13, so Seated resolves to Poland2019 and ElectionDayOf gives 2019-10-13. The earliest PolandTimeline From is 2021-05-06, so no fact stands and only derived lines are returned. sameLines is then false and the formed check falls back to R0. n776b.log:513 shows R0 as "PiS+KO+TD on Konf's support", so both (f) checks FAIL. Italy's start (2022-07-21) does the same. Measurement (e) does not depend on the epoch: PartySystems.For returns the static roster with 2023 SeedSeats, and JointMasks, Compatibility and UsesNegativeParliamentarism read no epoch. CLAUDE.md:53 says "`ElectionVintage.Seated` resolves at the epoch; a check pins an election by name". GermanFormationDiagnostic pins everything: (a) ForDate(poll25), (b) InitialSeats(Germany2025), (c) ApplyStart(Germany) then ViewOf(viewGermany, DeclarationReading.OfElection(Germany, poll25)).

PARTLY REFUTED. (1) No earlier check leaks a Polish epoch. CheckSuite.RunTable (781ff) and WarmEditor.RunOne do not reset the epoch, but every Editor epoch setter is scoped: WorldClockDiagnostic.cs:141 `finally { SetEpoch(epochBefore) }`; PollingDayDiagnostic.cs:39 is a using declaration that covers its ApplyStart(Poland) at :148; ConfidenceDiagnostic:32, PresidentialElectionLive:40, PresidentialVeto:245/650, PreStartRecord:102/141/251 and PresidentialReferenceWorld:45 are all scoped. The four unscoped save restores (SaveLoadRoundTrip, ItalyDebtCrisisSlice, SustainedObjective, ScenarioSlice) restore saves cut in the same process; SaveGameService.cs:100 stamps EpochDate at capture and :239 SetEpoch(save.EpochDate) is then a no-op. PlayRecordDump only calls LoadFromFile. n776b.log:17/496 is a cold named batch with this check first at the default epoch 2026-10-01, so lines 519-520 are a genuine pass. (2) The only reachable failure is outside the bar: GameController.cs:2173 SetEpoch(start) in Play mode; there is no playModeStateChanged reset and EditorSettings has no enterPlayModeOptions; CheckSuite.cs:378-383 RunFromMenu calls RunAll with no epoch reset. That covers an interactive menu run after a Polish or Italian game. In that run FormationSweepDiagnostic fails the same way: :59 has a bare EpochScope, and :83/:115 read Seated for Sweden (which becomes Sweden2022) and for every other country. So the suite is already red and this check adds a misleading FAIL, never a false pass. (3) The coverage half is overstated. For(Seated) at the default epoch goes through ForSourced to ForDateSourced(2023-10-15). OfElection(Poland, 2023-10-15) is dated because HasTimeline(Poland), which (f) asserts, and goes through ForDate to the same ForDateSourced(2023-10-15). CandidaciesSourced returns empty for Poland and CandidaciesAt finds no Candidacy facts. InOrAgainstForSourced is Sweden-only and InOrAgainstAtSourced finds no InOrAgainst or NoSupportRole facts. The vintage route is itself a game path (GovernmentFormation.cs:201 ViewOf(country, Seated); :332 For(..., SittingVintage)). The two routes agree today, as the finding says.

**The skeptic's corrected fix.** Pin the 2023 election at both (f) call sites: `DeclaredRedLines.For(CountryId.Poland, parties, ElectionVintage.Poland2023)` (line 162) and `DeclaredRedLines.InOrAgainstFor(CountryId.Poland, parties, ElectionVintage.Poland2023)` (line 167). A named vintage passes through Resolve unchanged and ElectionDayOf(Poland2023) is 2023-10-15, so this still runs the branch §776 added to ForSourced and makes (f) independent of the process epoch, as CLAUDE.md requires. No measured output changes, because n776b ran at the default epoch, where Seated already resolves to Poland2023. To also hold the election-night entry, add one assertion: `var r = DeclarationReading.OfElection(CountryId.Poland, PollingDay);` then check r.Dated, check that r.Lines(CountryId.Poland, parties) gives the same sorted Keys as WithDeclarations(false), and check that r.Platforms(...) is empty. Do not replace For(...) with OfElection alone, because that skips the ForSourced change. Heavier option, in the shape of German (c): inside the existing scope, call ApplyStart(Poland), build a world, set Poland's ParliamentSeats to PartySystems.InitialSeats(Poland, ElectionVintage.Poland2023), and assert that GovernmentFormation.ViewOf(country, DeclarationReading.OfElection(Poland, PollingDay)) seats KO+TD with NL carrying it and PiS outside.

### 15. A fact or key missing from the Polish roster is dropped on both sides, so 'line for line' and 'PiS outside' can pass vacuously

- **Lens:** diagnostic - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PolishDeclarationsDiagnostic.cs:123

**The scenario.** WithDeclarations skips a fact whose Party or Other is not in PartySystems.For(Poland) (line 123: if (a < 0 || b < 0) continue). ForDateSourced does exactly the same (DeclaredRedLines.cs:479). So a PolandTimeline fact keyed to a non-roster key disappears from both gameReads and recorded, and sameLines (line 164) still holds. An example is a member-party key such as 'P2050', which doubt 3 contemplates (no such roster key exists). Checks (a) to (d) pass too, because the record's §8 table carries the same key, and (f) prints the shorter line count as 'ok ... line for line' while the formation never reads the fact. Nothing compares the printed count (12 today = 2 derived + 10 standing) with standing.Count. Separately, Mask (line 112) silently ignores missing keys: if 'PiS' stopped being a roster key, Mask("PiS") would be 0 and the clause (formed.Government.Cabinet & Mask("PiS")) == 0 at line 169 would hold for any cabinet. Likewise ofRecord would shrink silently if KO, TD or NL were missing.

**The fix proposed.** Add one check that every PolandTimeline Party and Other resolves in the Polish roster (Index >= 0), or that the game's Declared line count equals standing.Count. Assert Index(k) >= 0 for PiS, KO, TD and NL before building masks.

**The skeptic's evidence.** THE TIMELINE-KEY HALF IS REAL (a latent gap in what the checks can catch; no current defect).
- The diagnostic resolves keys at PolishDeclarationsDiagnostic.cs:110 (`int Index(string abbrev) {... return -1; }`) and skips misses at :122-123 (`int a = Index(f.Party), b = Index(f.Other); if (a < 0 || b < 0) { continue; }`).
- The game does the same at DeclaredRedLines.cs:478-479 (`if (a < 0 || b < 0) { continue; }`). Its IndexOf (:621-629) is an ordinal `==` that returns -1 without a word.
- Check (f) at :162-164 compares `DeclaredRedLines.For(...)` with `WithDeclarations(false)`. Both sides go through the same skip, so `sameLines` cannot see a fact that was dropped. Line 166 prints only `gameReads.Count` ("the seated chamber's 12 lines" in n776b.log:519, which is 2 derived lines per record §10 plus 10 standing). Nothing compares that count with `standing.Count`.
- Checks (a) to (d) never resolve a key against the roster. (a), at :51/:63, compares key strings with the §8 table. (b) checks tags, (c) checks digests, and (d), at :98-99, checks dates only.
- No other check resolves these keys. PolandTimeline/TimelineOf are read only in DeclaredRedLines.cs, this diagnostic, and DeclarationDatesDiagnostic.cs:107, which only counts open facts.
- By contrast, GermanFormationDiagnostic.cs:45/50 asserts `Declared(on25) == 3`, so a dropped German fact fails there.
- Today every key (PiS, KO, TD, NL, Konf, MN) is in RealRoster(Poland) (PartySystem.cs:446-460: PiS, KO, TD, NL, Konf, SLD, PSL, MN), so the run is correct now.
- Failing path: a later edit puts a non-roster key, or a case slip such as "KONF", into both the array and the §8 table. All six checks then print ok while the formation ignores the fact, and (f)'s "line for line" becomes false.
- Non-roster keys are plausible in this domain: "Razem" (PolandUnseatedUnits, PartySystem.cs:421-423) and the TD member row "Polska 2050" (:415). As an Other, "Polska 2050" parses in (a). As a declarer it would trip (a)'s `\S+` by accident.
- The finding's example is misattributed. Doubt 3 is about TD's start date, i.e. whether member parties' lines key the joint list, still under TD. It does not propose a P2050 key.

THE MASK HALF IS REFUTED.
- At :169 the PiS clause is ANDed with `governs == ofRecord`. The cabinet is a subset of governs, so when governs equals ofRecord (KO|TD|NL) it cannot hold PiS. `(Cabinet & Mask("PiS")) == 0` is therefore redundant, not a weak point.
- If ofRecord lost KO, TD or NL, the remaining pair holds at most 222 seats, under the majority of 231. Under positive investiture `Wins` needs `supported >= chamber.Majority` (CoalitionFormation.cs:597), so no winning government can equal a shrunk ofRecord, and the check FAILS loudly instead of passing vacuously.

**The skeptic's corrected fix.** Add one check that is computed, not copied by hand: every PolandTimeline fact's Party and Other resolves in PartySystems.RealRoster(CountryId.Poland) (Index >= 0), with the unresolved keys named on failure. In (f), also assert `gameReads.Count(l => l.Kind == RedLineKind.Declared) == standing.Count`, so the printed "line for line" holds the polling day's standing set to the declared lines the game reads. This is the Polish form of GermanFormationDiagnostic's `Declared(on25) == 3`, computed instead of a hard-coded count. Skip the suggested Index(k) >= 0 asserts for PiS/KO/TD/NL: `governs == ofRecord` already implies PiS is outside, and it fails on any shrunk ofRecord.

### 16. No check holds PolandTimeline to its own date order or to non-overlap; DeclarationDatesDiagnostic's loop walks Sweden's timeline only

- **Lens:** diagnostic - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/DeclarationDatesDiagnostic.cs:90

**The scenario.** Section 3 (lines 88-97) asserts from < until and no overlap per kind, party and other, but iterates DeclaredRedLines.SwedenTimeline only. PolishDeclarationsDiagnostic (d) checks only what stands on polling day. Suppose a row is regenerated with its dates swapped, e.g. Konf -> NL from 2023-06-26 until 2023-06-20. That fact never stands on any day. Check (a) passes because the record and the array agree, (d) passes because the fact is closed and not standing, and DeclarationDatesDiagnostic's new Poland count check also passes. A July overlap between two Konf -> PiS spans would likewise put two lines on the same days with no check failing. The current 17 facts satisfy both rules (I walked every party/other/kind group).

**The fix proposed.** Run section 3's loop over TimelineOf(c) for every country with HasTimeline(c), not over SwedenTimeline alone.

**The skeptic's evidence.** The finding's claim holds. Traced from the code:

1. Section 3 checks Sweden only. DeclarationDatesDiagnostic.cs:90 is `foreach (DeclaredRedLines.DatedFact f in DeclaredRedLines.SwedenTimeline)`. It holds `Check(f.From < f.Until, ...)` (line 92) and the per-key overlap check `f.From >= b || f.Until <= a` (line 95). The after-wiring run log G:/UNITY/Projects/PoliSim-captures/logs/n776b.log lines 605-621 prints the order and overlap lines for Sweden's 14 facts only. No Polish or German fact is checked.

2. The new Poland check cannot see a swapped row. Lines 106-109 count `f.Until == DateTime.MaxValue` and compare that to `StandingOn(Poland, 2026-01-18).Count`. The log shows "its 10 open facts". A swapped closed fact, for example Konf->NL with from 2023-06-26 and until 2023-06-20, is not open. `StandsOn` (DeclaredRedLines.cs:300, `date.Date >= From && date.Date < Until`) is never true for it. Both sides stay at 10.

3. Check (a) does not test order. PolishDeclarationsDiagnostic.cs:51-66 parses `(\d{4}-\d{2}-\d{2}) \| (Open|\d{4}-\d{2}-\d{2})` and compares it field by field with the array (`f.From == rows[i].From && f.Until == rows[i].Until`). It never checks that from comes before until. Grouped rows such as "2–6 | Konf → PiS, KO, TD, NL, MN" are split into one fact per party. A table and array regenerated together with swapped dates still agree.

4. Check (d) tests polling day only. Lines 98-100 test 2023-10-15. A swapped closed fact passes because it is neither standing nor open. A July overlap also passes because the closed span is over by polling day. Example: Konf->PiS cabinet from 07-06 to 07-20 against the support-blocking line from 07-13, open.

5. The fact validates nothing. The `DatedFact` constructor (DeclaredRedLines.cs:295-298) assigns its fields and validates nothing.

6. No other reader checks Poland's timeline. A grep of Assets for TimelineOf, PolandTimeline, StandingOn, LiftedSince and ForDate finds:
- FormationSweepDiagnostic.cs:76 and AiMotionReachDiagnostic.cs:86 read SwedenTimeline only.
- The sweep's Polish entry (line 103) reads polling day only.
- GermanFormationDiagnostic reads Germany only.
- UiScreenshotDriver.FilmDeclared (lines 5936-5942) logs the rows and asserts nothing.

Why it matters beyond polling day: WorldClock.StartDate(Poland) is `new CampaignCalendar(2023-10-15).PreCampaignStart`. That is 8 + 26 weeks before polling day, so 2023-02-19. A Polish game steps through every 2023 date in the timeline. Since section 776 made HasTimeline(Poland) true, two runtime paths read the timeline on today's date:
- the run-up's declarations page, which calls StandingOn and LiftedSince (GameController.CampaignDeclared.cs:189 and :240);
- a mid-term formation, through `DeclarationReading.MidTerm`, which calls ForDate(today) (DeclaredRedLines.cs:666-667, 672-673).
A bad row would therefore reach play while every check stays green.

Why it is a note and not a defect: today's data is correct. I walked every (kind, party, other) group of the 17 Polish facts:
- Konf->PiS: from 06-20 to 06-26, from 07-06 to 07-13, from 07-13 open.
- Konf->KO, TD, NL and MN: from 06-20 to 06-26, then from 07-13 (KO) or 08-02 (the others) open.
- NL->Konf: from 07-05 to 08-27, then from 08-27 open.
- Every other group is a single fact.
All are ordered and none overlap. Germany's 7 facts (lines 363-383) each have a distinct key and are all open, so the generic loop the fix asks for would also pass today.

**The skeptic's corrected fix.** In DeclarationDatesDiagnostic section 3, loop over every CountryId c where DeclaredRedLines.HasTimeline(c). Iterate DeclaredRedLines.TimelineOf(c), and give each country its own `seen` dictionary (or put c in the key) so two countries' facts are never compared. Put the country in each check's text. Also widen the diagnostic's title ("Sweden's declarations by date") and the closing count line. Today Sweden's 14, Germany's 7 and Poland's 17 facts all pass, so the checks stay green. Optional extra guard: in PolishDeclarationsDiagnostic (a), also require From < Until on every parsed table row.

### 17. The MEASURED header prints 'positive investiture' as a literal while the rule is computed

- **Lens:** diagnostic - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PolishDeclarationsDiagnostic.cs:140

**The scenario.** Line 109 computes negative = ChamberRules.UsesNegativeParliamentarism(CountryId.Poland), and every Form call uses it. The header at line 140 types 'positive investiture' instead. The class summary (line 25) and the record's §12 (line 241) state the same as fact. If Poland's rule ever changes, the log prints a rule it did not measure. Every figure on the line is generated, but this fact about the code is transcribed.

**The fix proposed.** Print negative ? "negative investiture" : "positive investiture" (or the rule's name) from the variable.

**The skeptic's evidence.** The finding is accurate, but the problem is latent. The output is correct today, and nothing that passes or fails depends on the text.

What the code does:
- PolishDeclarationsDiagnostic.cs:109 reads the rule from the game: `bool negative = ChamberRules.UsesNegativeParliamentarism(CountryId.Poland);`
- That variable drives every formation call. Line 146 passes `CoalitionFormation.Form(..., negativeRule: negative, ...)`, line 147 passes `CoalitionFormation.Prepare(seats, compatibility, lines, negative, ...)`, and line 167 passes `Form(..., negativeRule: negative, ...)`.
- The header at line 140 does not use the variable. It types the rule as text: `"    MEASURED  the 2023 chamber (seed seats {0}; majority {1} of {2}; positive investiture) - ..."`. Every other value on that line is filled in from the run: the seats, `seats.Sum() / 2 + 1`, which matches `CoalitionMath.Majority` (CoalitionFormation.cs:178-183), the record's cabinet and its seats.

Why it is right today:
- GovernmentFormation.cs:475 reads `public static bool UsesNegativeParliamentarism(CountryId country) => country == CountryId.Sweden;`, so Poland gets false, which means positive investiture.
- That file is unchanged in the working tree, and n776b.log:512 prints "majority 231 of 460; positive investiture", which is correct.

How it would go wrong: if anyone edits `ChamberRules` for Poland, the five readings and the wiring check would all run under the new rule (lines 146/147/167) while line 140 still prints "positive investiture". Nothing would catch it.
- The diagnostic's checks never look at the header.
- CommentClaimCheck only checks backticked `Type.Member` references (its `Claim` regex).
- The change is plausible: ChamberRules' own summary (GovernmentFormation.cs:466-471) calls positive investiture only "the conservative reading elsewhere", outside Germany.
- Compare Formation2026Diagnostic.cs:87. It also prints its rule as text ("negative parliamentarism (RF 6:4)"), but it also passes a fixed `negativeRule: true`, so its text and its run cannot drift apart. This diagnostic reads the game's rule instead, so they can.

On the finding's other citations:
- The class summary at line 25 ("its seed seats, positive investiture, the game's own compatibility and groups") is a present-tense comment describing the measurement's inputs. It belongs to the same class: a fact about the code written down instead of referenced.
- The record's §12 (coalition_declarations_2023.md:240-241) is a dated record: "Measured ... (2026-10-04, §776 ...)". A record of what was measured on a day is not a claim about today (the reason the convention exempts COMPLETED.md), so that part of the finding does not hold.

Severity: note. The literal matches the rule today, the only possible damage is a wrong label in a log line, and the fix is one argument.

**The skeptic's corrected fix.** At line 140, build the rule's name from the variable the run uses. Add a sixth slot and pass it.

    sb.Append(F("    MEASURED  the 2023 chamber (seed seats {0}; majority {1} of {2}; {5}) - the record's cabinet {3} ({4} seats):\n",
        ..., Enumerable.Range(0, parties.Count).Where(p => (ofRecord & (1 << p)) != 0).Sum(p => seats[p]),
        negative ? "negative parliamentarism" : "positive investiture"));

In the class summary at line 25, replace "positive investiture" with a reference to the rule, such as "the chamber's own investiture rule (`ChamberRules.UsesNegativeParliamentarism`)". CommentClaimCheck then checks that reference.

Leave the record's §12 as it is: it is a dated record of the 2026-10-04 measurement.

### 18. The sweep's new pin comment is correct but transcribes formation figures, and its before-state drops Konf's support

- **Lens:** diagnostic - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/FormationSweepDiagnostic.cs:129

**The scenario.** Verified against the texts: the pin 6c0cd88d... equals the SHA-256 of formation_sweep.txt (byte-identical to formation_sweep_mismatch.txt). Only blocks 248 and 249 (Poland, seated, negative True and False) differ from formation_sweep_before776.txt. The after-state figures are right: cab 6 sup 8 is KO+TD on NL, 222 seats, supported 248; under the negative rule cab 1 is PiS alone, 194 seats, opposed 44. Two problems. (1) The before-state is cab 7 sup 16 under both rules, i.e. PiS+KO+TD on Konf's support, supported 434, but the comment gives it as 'PiS+KO+TD (416)' and never states the negative rule's before-state. (2) Figures transcribed into a source comment go against the claim convention, which exempts only COMPLETED.md. This has been the established pattern of this constant's comment since §683, and CommentClaimCheck resolves backticked names only, so no bar catches it.

**The fix proposed.** Say 'PiS+KO+TD on Konf's support (both rules)' for the before-state, or better, reference formation_sweep_before776.txt and the §776 record for the figures instead of restating them.

**The skeptic's evidence.** I could not refute it. Every factual claim in the finding checks out, and the only consequence is a comment that leaves things out. Nothing changes at runtime, because the bar compares only the digest, and the digest is correct.

1. The pin is correct. In G:/UNITY/Projects/PoliSim-captures/logs, sha256sum gives 6c0cd88d…f1ad for formation_sweep.txt. That equals PinnedDigest at G:/UNITY/Projects/PoliSim/Assets/Editor/FormationSweepDiagnostic.cs:129. cmp shows formation_sweep.txt is identical to formation_sweep_mismatch.txt. formation_sweep_before776.txt hashes to 5ceb43b3…, which is the §766 pin. Both files have 250 blocks. A block-numbered diff shows only blocks 249 and 250 (1-indexed) differ, which are "Poland | seated | negative True/False". So "2 of 250 blocks moved" is right.

2. The after-state figures are right. Party order is PiS 0, KO 1, TD 2, NL 3, Konf 4 (PartySystem.cs PolandParties: 194/157/65/26/18).
- Negative rule (True): "cab 1 sup 0 MinorityGovernment seats 194 supported 194 opposed 44". That is PiS alone, opposed by NL and Konf (26 + 18).
- Positive rule (False): "cab 6 sup 8 ConfidenceAndSupply seats 222 supported 248 opposed 18". That is KO+TD on NL's support.
- Poland's own rule is positive: GovernmentFormation.cs:475 `UsesNegativeParliamentarism => country == CountryId.Sweden`.

3. The before-state is described incompletely. formation_sweep_before776.txt lines 94032 and 94056 show the same government under both rules: "cab 7 sup 16 MajorityCoalition seats 416 supported 434 opposed 26". That is PiS+KO+TD on Konf's support. The comment says only "under its own rule (positive investiture) PiS+KO+TD (416) -> …; under the negative rule PiS alone (194, opposed 44)". So it drops Konf and the supported 434, and gives no before-state for the negative rule.

Every other state in the same comment names its support party:
- "PiS with TD+Konf's support (194, supported 277)"
- "CDU+CSU+SPD with the SSW (328, supported 329)"
- "KO+TD on NL's support (222, supported 248)"

So "PiS+KO+TD (416)" reads as a government with no support party. The session's own measurement is more precise than the comment. n776a.log:525 and n776b.log both print "R0 the derived lines alone: MajorityCoalition - PiS+KO+TD on Konf's support (416 in cabinet, 434 supported)".

What keeps this a note:
- Every figure the comment does give is correct.
- The negative rule's before-state can be recovered from the §766 entry in the same comment, which covers both rules.
- The omission is inherited. The §766 entry ("-> PiS+KO+TD (416)"), COMPLETED.md §766 ("to PiS+KO+TD (416)") and coalition_declarations_2023.md §12 ("The derived lines alone seat PiS, KO and TD together") all omit Konf's support too.

4. The claim-convention point is accurate.
- CLAUDE.md says: "Nobody transcribes, anywhere, in any file … COMPLETED.md is exempt".
- COMPLETED.md §190 §A.5, item 2, forbids measured figures outside a generated block and says "This binds code comments too".
- The only pattern CommentClaimCheck matches is a backticked `Type.Member` (CommentClaimCheck.cs `Claim`), so no bar catches figures.
- git history shows figures first entered this comment at §683 (1bc8ff1: "244 -> 20 … 277"). The §652 entry gave dates only. No review in Reviews/ ruled on the pattern.
- Strictly, this breaks the letter of the convention. It is a long-standing habit in history entries attached to a pin, and nothing has been built on it, so it is a note and not a defect.

**The skeptic's corrected fix.** Restate the §776 entry so it gives the before-state once, for both rules, with its support party. Suggested wording: "the seated Polish chamber's two rules only: under both, PiS+KO+TD on Konf's support -> under its own rule (positive investiture) KO+TD on NL's support - the record's majority, NL outside the cabinet where in fact it sat in it; under the negative rule PiS alone; the text before is formation_sweep_before776.txt".

The convention-compliant version goes further: drop the figures and point to the diff of formation_sweep_before776.txt against formation_sweep.txt, to PolishDeclarationsDiagnostic's R0/R1 MEASURED lines, and to the §776 record. When that record is written, it should carry the before-state the way R0 prints it ("PiS+KO+TD on Konf's support, 416 in cabinet, 434 supported").

The §766 entry's after-state ("PiS+KO+TD (416)") in the same comment has the same omission. Fix it the same way, or replace the whole history with references. COMPLETED.md §766 is append-only and stays as it is.

### 19. A file the record cites in its body is not held in tree, and the digest check cannot see it

- **Lens:** diagnostic - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:263

**The scenario.** Doubt 5 cites Konf/x_bosak_1713834658134138996_syndication.json as evidence that the line held after the vote. The path is written like the in-tree raw/ paths, but the file exists only under PoliSim-captures/sources/poland_declarations_2023/Konf/. Check (c) walks register rows only (all 54 register files verified), so a source cited outside the register is held to no digest. A reader of the header ('Every source cited is stored byte for byte under raw/declarations_2023/') would assume otherwise.

**The fix proposed.** Mark the path as out of tree (captures), or register and hold it like the others.

**The skeptic's evidence.** I could not refute it. Every step of the scenario checks out.

1. The citation. Record line 263 (doubt 5) quotes Bosak's post-vote X post, "nasza deklaracja z kampanii wyborczej, że nie wejdziemy w koalicję ani z PiS ani z PO, dzień po wyborach jest nadal aktualna", and cites it by a bare path: (Konf/x_bosak_1713834658134138996_syndication.json).

2. The file is not held in tree.
- ElectionsData/poland/raw/declarations_2023/Konf/ holds 10 files and none of them is x_bosak_*.
- The in-tree SHA256SUMS.txt (54 lines) has no line for it.
- The file exists only at G:/UNITY/Projects/PoliSim-captures/sources/poland_declarations_2023/Konf/x_bosak_1713834658134138996_syndication.json. Its digest matches line 24 of that folder's SHA256SUMS.txt, and its bytes carry the quote with created_at 2023-10-16T08:29:58Z. So the quote is accurate; only where it is stored is wrong.

3. The record says the opposite, three times.
- Line 4: "Every source cited is stored byte for byte under `raw/declarations_2023/<declarer>/`".
- Line 8: "`PolishDeclarationsDiagnostic` ... holds every cited file to its digest".
- Line 334: the out-of-tree files are "corroborations and context the text does not cite".
- Line 332 shows how the record marks a cited file that is out of tree: "(other bytes, the same text; out of tree only)". Doubt 5 has no such mark.

4. It is the only such case. I sorted every path the record mentions. Line 263 is the only body citation that is neither in tree nor marked out of tree. Line 265's KO/tvn24_2023-10-12_relacja_na_zywo.html, written the same way, is in tree. Lines 332 and 334 are marked out of tree.

5. No check sees it.
- Check (c) walks only register rows: PolishDeclarationsDiagnostic.cs:83, `Regex.Matches(record, @"^\| \[[A-Z]+-[A-Z]+\d+\] \|[^\n]*", ...)`.
- It takes paths only from cell 5 of each row (line 86).
- Check (b), lines 69-74, only resolves the tags each fact cites.
- No other Editor check or Tools script reads this record's body paths.
- n776b.log:510 passes without it: "ok every file the register names is held under raw/declarations_2023 at its digest (54 held)".

6. The diagnostic is not at fault. It does what its own doc comment says (lines 21-22: "every file the register names"). The overclaim is in the record, mainly line 8.

Why minor rather than note: under CLAUDE.md's claim convention a path is a DERIVED claim, and lines 4 and 8 are false for this citation. In a record whose whole job is provenance, that should be fixed before commit.

Why not a defect: the post is dated 2023-10-16, after the vote, so it falls outside "What counts" (line 13: words on or before 15 October 2023). It is the basis of no DatedFact, and no check result or formation depends on it. Line 257 also says the doubts are "the sweep's own list, as its synthesis wrote it", so the path is presumably relative to the captures folder. But the record never says so, and the same form at line 265 does resolve in tree.

**The skeptic's corrected fix.** Preferred: hold it like the other cited files. This keeps doubt 5's text as the sweep wrote it and makes line 4 true.
1. Copy the captures file byte for byte into ElectionsData/poland/raw/declarations_2023/Konf/.
2. Add its line to the in-tree SHA256SUMS.txt.
3. Give it a register row, shaped like [KONF-P2]'s: the X syndication URL for id 1713834658134138996, publisher X (@krzysztofbosak), its created_at as the page's own date, the file, and its digest. Check (c) then holds it to its digest with no code change.
4. Add it to fetch_log.md's X line.

Alternative: mark it the way line 332 marks cited files that are out of tree ("out of tree only", under PoliSim-captures/sources/poland_declarations_2023/), and narrow line 4 to "every source the register names".

Either way, line 8 should say the diagnostic "holds every register file to its digest", because that is all check (c) does.

### 20. DeclaredRedLines' class summary still says only Sweden and Germany are sourced and that every other country's For is derived-only

- **Lens:** diagnostic - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/DeclaredRedLines.cs:15

**The scenario.** Lines 12-21 still read 'only one of the six countries has its declarations on disk' and 'SOURCED FOR SWEDEN ... and for GERMANY since §705 ... For every other country `For` returns the DERIVED lines alone and `IsSourced` returns false'. After this change IsSourced(Poland) is true (line 36) and For(Poland) reads PolandTimeline (line 53), so the summary is now false for Poland. A caller deciding from this summary whether a Polish formation carries real declarations would be misled. GovernmentFormation.cs:26-29 ('exist on disk for Sweden only') has been stale since §705.

**The fix proposed.** Add Poland (§776) to the summary's sourced list, and replace the counts with a reference to IsSourced or HasTimeline.

**The skeptic's evidence.** The finding holds. In the working tree, the class summary of G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/DeclaredRedLines.cs is now false for Poland, and §776 did not touch it.

**What the summary says (unchanged by §776)**
- Lines 12-13 (from f7264d9b, 2026-08-31): "would hide the fact that **only one of the six countries has its declarations on disk.**" This has been false since §705.
- Line 15 (from edf0adc0, §705): "SOURCED FOR SWEDEN, in two vintages, and for GERMANY since §705".
- Lines 19-20 (from 38ce076a): "For every other country `For` returns the DERIVED lines alone and `IsSourced` returns false".

**What the code does after §776 (unstaged diff)**
- Line 36: `IsSourced(...) => country == CountryId.Sweden || country == CountryId.Germany || country == CountryId.Poland`.
- Line 53 (inside `ForSourced`): `if (country == CountryId.Germany || country == CountryId.Poland) { return ForDateSourced(country, parties, WorldClock.ElectionDayOf(country, vintage)); }`.
- Line 435: `TimelineOf` returns `PolandTimeline`. Line 439: `HasTimeline` includes Poland.
- `ForDateSourced` (lines 472-481) adds each standing PairLine from `TimelineOf(country)` as `RedLineKind.Declared`.
- `For` (lines 536-540) calls `ForSourced`.
- So `For(Poland)` returns declared lines and `IsSourced(Poland)` is true. The summary still puts Poland under "every other country".

**This omission is inside §776's scope.** The same diff updated the three member comments next to it: line 36's trailing comment, the `HasTimeline` summary at line 438 ("Poland's since §776") and the `ForDateSourced` summary at line 466 ("Sweden, Germany (§705) and Poland (§776)"). §705 (edf0adc0) set the precedent by editing this exact paragraph, changing "SOURCED FOR SWEDEN ONLY" to add Germany.

**There is no runtime effect, so this is not a defect.**
- Every consumer reads the function live: GovernmentFormation.cs lines 294 and 425 set `declarationsSourced = DeclaredRedLines.IsSourced(...)`.
- That flag reaches `Formed.DeclarationsSourced`. ElectionNightScreen.cs lines 713-716 and GameController.cs lines 6437-6439 print "formed on DERIVED red lines only" from the flag and name no country.
- OfficeTestDiagnostic.cs line 65 also reads `IsSourced` live.

**Why minor rather than note**
- CLAUDE.md's claim convention "governs every document AND every source comment" and "outranks everything below it". Its test is "the code can change freely and no document becomes wrong".
- This paragraph is a transcribed DERIVED claim: which countries `IsSourced` names, plus a count. It became false when the code changed, which is exactly what that test is meant to catch.
- Nothing in the bar catches it. CommentClaimCheck.cs only checks that a backticked `Type.Member` exists; its own summary says "It does not check that the cited member does what the sentence says".

**Same stale claim elsewhere (older, not in this diff)**
- GovernmentFormation.cs line 26 (from 38ce076a, before §705): "exist on disk for Sweden only".
- OfficeTestDiagnostic.cs line 37 and the banner its report prints at lines 549-552 (from f7264d9b): "DECLARED RED LINES ARE SOURCED FOR SWEDEN ONLY".
- Both have been wrong since §705 and now leave out Poland too.

**The skeptic's corrected fix.** Fix the class summary in DeclaredRedLines.cs by pointing to the code rather than copying the country list, as CLAUDE.md's claim convention requires:

1. **Lines 12-13:** delete "only one of the six countries has its declarations on disk", a transcribed count that has been false since §705. Or reword it to "would hide which countries have their declarations on disk (`DeclaredRedLines.IsSourced`)".
2. **Lines 15-21:** replace the country list with a pointer, for example: "SOURCED where `DeclaredRedLines.IsSourced` says so, and dated where `DeclaredRedLines.HasTimeline` does; each timeline's own summary names its record file". Keep the sentence about Sweden's two vintages.
3. **Lines 19-20:** change "For every other country `For` returns the DERIVED lines alone and `IsSourced` returns false" to "Where `IsSourced` is false, `For` returns the DERIVED lines alone, so a caller can say plainly…".

A smaller alternative that mirrors §705's edit: append "and for POLAND since §776 (its timeline, `ElectionsData/poland/coalition_declarations_2023.md`; every other Polish line derived)" to line 15. Then change "every other country" on line 19 to "every country `IsSourced` does not name".

Optionally mark the "Inventing Germany's would be…" sentence as history, since Germany is now sourced.

Follow-up for the same older staleness outside this diff (since §705):
- Reword GovernmentFormation.cs line 26 ("exist on disk for Sweden only") to point at `DeclaredRedLines.IsSourced`.
- Reword OfficeTestDiagnostic.cs line 37 and its printed banner at lines 549-552 ("DECLARED RED LINES ARE SOURCED FOR SWEDEN ONLY") to point at the report's per-country column, which line 65 already fills from `IsSourced` live.

## The first pass - refuted by the skeptics

- [sourcing] NL→Konf from 2023-07-05 is encoded cabinet-only, but the record reads that day's words as also refusing support (the run-up page says "would support") - *The failure the finding describes cannot happen. One part of it is true, but only as a wording conflict inside the record.

1) The WHO WILL GOVERN WITH WHOM page cannot open for Poland.
- GameController.CampaignDeclared.cs:33-34: `DeclaredPageAvailable() => _liveCampaignOpen && _playerCountry != null && DeclaredRedLines.HasTimeline(...)`. §776 made only the HasTimeline part true for Poland.
- `_liveCampaignOpen` is set only in OpenLiveCampaign (GameController.Campaign.cs:140-146). That needs BuildLiveCampaignSnapshot to return something, and line 152 returns null when `PlayerCampaign == null && PlayerPreCampaign == null`.
- LiveCampaignSetup.cs:148 stages a campaign only `if (country == CountryId.Sweden || country == CountryId.Germany)`. Line 176 says: "no campaign is staged for {country}: ... Sweden and Germany only". Neither the index nor the working tree changes this.
- SimulationManager.cs:4318-4322 (AdvancePreCampaign) and 4266-4271 (the campaign) both `return` when TryFor fails. So a Polish game never has a run-up or a campaign, and the page never opens.
- The test harness says the same at UiScreenshotDriver.cs:5927: "the HQ's DECLARED chip is offered only over a live campaign with a dated timeline".
- The only other screen with the same wording, the coalition screen, is marked "HARNESS ONLY" (GameController.CampaignCoalition.cs:11).

2) No formation changes on any date. The finding admits this, and the code confirms it.
- ForDateSourced adds the declared lines to `DerivedRedLines.From(...)`. It never replaces the derived ones.
- NL's galtan is 1.75 and Konf's is 8.41 (PartySystem.cs:451-452). The gap of 6.66 is over SocialGap 5.0, so the derived line between them blocks support both ways (CoalitionFormation.cs:953-959).
- `RefusesSupport` (CoalitionFormation.cs:64) covers NL→Konf for a symmetric line. So a one-way NL→Konf line would add nothing at any date.
- The election reading is taken on 15 Oct 2023, when fact 17 stands. Before 13 Nov 2023 a mid-term round uses the 2019 chamber.
- Small correction: the finding says NL is not a key in the 2019 chamber. It is, with 0 seats (`("NL", 0)`, PartySystem.cs:764). This changes nothing.

3) What is true is a conflict between the record's text and its dating.
- Record lines 127-129 credit the 5 July words with "Lewica's support for a cabinet with Konfederacja in it ("nie liczcie na nas")". Doubt 8 (line 266) and fact 17's basis also cite those words.
- But §4's heading (line 115), §8 row 16 and fact 16 (DeclaredRedLines.cs:428, `false, false`) start the support half on 27 August.
- The saved page (NL-P17, digest c5fb9907…) puts the phrase between two sentences about joining: "...na pewno Lewica nie wejdzie do żadnego rządu, w którym będzie Konfederacja. ... jeżeli taką opcję rozpatrujecie, to nie liczcie na nas. My absolutnie z Konfederacją nie pójdziemy." Read in that context, it is about entering a government, so the cabinet-only shape fits the page better than the record's prose does.
- Separately, every cabinet-only line gets the same "would support" tag, including Polish lines where support is "not addressed". Fact 16 is not special in that respect.*
- [sourcing] §10's stated standard and its application disagree (PiS's "Zablokujemy" called an aim; Konf's "chcemy ... nie dopuścić" counted as restating a line) - *The finding says §10's wording and its use disagree. I could not find a case where they do. All three cited items are classified the way §10 reads.

Record 217-219: "an aim to remove a party from power, or keep it out, is not a line; words about the declarer's own conduct toward that party's government are ("Nie zamierzamy przedłużać władzy PiS-u")".

(a) PIS-P2. The saved page (PiS/pis_2023-09-08_tomaszow-zablokujemy-powrot-po.html) reads: "Zablokujemy powrót do władzy Platformy Obywatelskiej i D. Tuska. ... Jestem pewien, że zwyciężymy 15 października, ale proszę o mobilizację i namawianie innych do głosowania na Prawo i Sprawiedliwość". Blocking a return to power by winning the vote is the first clause's "keep it out". It is not the declarer's support role toward a government, which is what the example "nie zamierzamy przedłużać" shows. The code frames it the same way: PolishDeclarationsDiagnostic.cs:27 calls it a "keep X from power" pledge, and :130 says "a pledge to keep the other from power read as a line - PiS's [PIS-P2]". Doubt 1 (record 259) asks Elias exactly this: "whether a 'block X's return to power' pledge is a line". So the standard as worded does not make PIS-P2 a line.

(b) KONF-I23. It appears only in the "Restated" lists (DeclaredRedLines.cs:413 and :415; record 67-70). It is never a basis: the shape and date of facts 8-9 rest on KONF-I14, D(2023,7,13), with blocksSupport and oneWay both true. Those same lists already include restatements that only refuse a coalition: KONF-P1 "Koalicja z PiS czy z Platformą? Z nikim!" and KONF-I15 "nie wejdzie w koalicję z PiS-em". So "Restated" never claimed that each citation repeats the support half. The KONF-I23 page (Konf/polsatnews_2023-10-10_mentzen-gosc-wydarzen.html) calls its own words a repetition: "Bardzo wyraźnie mówimy od paru miesięcy: chcemy zakończyć rządy PiS-u i nie dopuścić do rządów Donalda Tuska." Asked "o koalicję z partią Jarosława Kaczyńskiego lub ugrupowaniem Donalda Tuska", Mentzen answers: "Gdybym ja teraz, po tych wszystkich zapowiedziach, zrobił rząd z PiS-em, straciłbym całkowicie wiarygodność". That is a refusal of a government with PiS, so the proposed fix of dropping KONF-I23 from fact 8 would remove a real restatement. The handling of KONF-P2 (record 57-59, cs:411: the aim is "not read as support-blocking") is about whether aim words can CREATE the support half. That fits §10.

(c) TD-P6. The page (TD/psl_2023-08-10_rejestracja-kkw.html) reads: "Komitet, który doprowadzi do odsunięcia PIS-u od władzy i nie dopuści do rządów populistów z Konfederacji". These are §10's two aim forms word for word: remove from power, keep out. "Nie dopuści do rządów" means "will keep them from governing". It is not KONF-I14's "Nie zamierzamy umożliwić", which is about the declarer's own enabling. The record treats TD-P6 as context only (record 105-106, cs:425) and as evidence to measure in doubt 2, which matches §10.

The "real criterion" the finding names (doubt 1's pairing with a coalition refusal) is an extra distinction that gives the same verdicts; it does not replace §10. Fact 7 (KONF-P2) shows this: there an aim sits next to an explicit coalition refusal ("Koalicji z PiS nie chcą ... władze Konfederacji"), and it is still read as cabinet-only.

No formation changes either way. In both n776a.log and n776b.log, R3 (R1 plus a PiS-KO line) forms the same government as R1: "KO+TD on NL's support (222 in cabinet, 248 supported)". The TD→Konf line "changes no formation" (record 108-109), and the 2019 chamber has no TD key (doubt 12).*
- [sourcing] Next-term pledges become readable by mid-term rounds in the sitting (2019) chamber - *The mechanism the finding describes is right: with Poland in HasTimeline, DeclarationReading.MidTerm (DeclaredRedLines.cs:666-667) returns a reading dated today. But the failing path cannot happen, the behaviour is the owner's ruling, and the scope is already written down.

1) No Polish mid-term round can open today.
- ConfidenceProcedure.cs:34 maps Sweden to Riksdag, Germany to Bundestag and everything else to Unsourced, so Poland is Unsourced.
- Only two places create a round with MidTerm = true:
  - SimulationManager.cs:3063, DraftSuccessor. This is the Bundestag constructive vote.
  - SimulationManager.cs:4128, DischargeAndRound. It runs only after g.NoConfidenceOn is set, and only a Riksdag motion sets it (:2858, :3043).
- For Poland, all of these are blocked:
  - TryAiMotion returns at :3020 (`if (RulesOf(country.Id) != Riksdag) { return; }`).
  - MoveNoConfidence refuses at :2824 with "THIS COUNTRY'S CONFIDENCE RULES ARE NOT YET MODELLED".
  - DrawConfidence (GameController.ParliamentRows.cs:616) returns early for Unsourced, so even the projected vote on CurrentDate is never computed.
- The only Polish round is the one after the election. It reads OfElection(2023-10-15), and facts 2-6 are not standing on that day.

2) Reading pair lines on the day of a mid-term round is the owner's ruling.
- COMPLETED.md:34453 (§644): the reading on the day was reverted once because SD's platform is "scoped by SD's own words to the formation after the next election".
- COMPLETED.md:34607 (§653), Elias: "A mid-term round reads the dated timeline's pair lines and candidacies, with the platforms held to the election." So the scope question was weighed, and only platforms were held to the election.
- The finding's analogy is a platform. Sweden's "Scoped by its own words" sentence is SdNoSupportRole (DeclaredRedLines.cs:262-266, FactKind.NoSupportRole). None of Sweden's pair-line bases carry such a sentence: C→V 2026-01-30, KD→S 2026-09-02, and the M→SD lift that SpeakerRoundDiagnostic (6) reads in a mid-term round on 1 June 2026.

3) The scope is already stated.
- Code, fact 2: "will enter no coalition with PiS, nor with anyone in the next term". Facts 3-6: "will enter no coalition with anyone in the next term". Each also quotes "W przyszłej kadencji nie wejdę w koalicję z nikim."
- Record title, line 1: "declarations for the formation after the Sejm election of 15 October 2023".
- Record §2, line 44, the gloss: "In the next term I will not enter a coalition with anyone."
- Doubt 4 (record §12) lays out the June wobble and the alternative of omitting the one-week facts.
- Doubt 12 (record §12) covers the 2019 chamber's keys (Poland2019: SLD 49, PSL 30, MN 1) for a mid-term round and leaves them out on purpose, because E2's scope is the 2023 declarations.

4) The finding picks the narrowest case. Every row of the record is a pledge for the formation after 2023, for example fact 13 (TD→PiS, "po wyborach") and facts 8-9 (Open from 2023-07-13). Those would stand for months of the 9th Sejm under the same ruled reading, compared with six days for facts 2-6. A scope note on facts 2-6 alone would be inconsistent with how the other rows are written.*
- [wiring] The vintage candidacy path omits Poland - not exactly the Germany pattern - *The asymmetry exists in the text (DeclaredRedLines.cs:161 routes Germany to CandidaciesAtSourced; :162 returns empty for Poland), but no path can make the vintage and dated readings disagree:
(1) No data triggers it. All 17 PolandTimeline facts are FactKind.PairLine with no Candidacy. The record's §7 (coalition_declarations_2023.md:161-178) says none was declared and the timeline 'carries no candidacy facts'. So CandidaciesAtSourced(Poland, 2023-10-15) also returns empty.
(2) The hypothetical data change is caught. PolishDeclarationsDiagnostic.cs:63-64 requires every Polish fact to be 'f.Kind == DeclaredRedLines.FactKind.PairLine ... && f.Candidate == null', row for row with the record's §8 table. §776 registers it in CheckSuite.cs, so adding a Polish candidacy fails the cheap bar and forces this wiring to be revisited.
(3) Even with one added, no reader of the vintage candidacies would observe it for Poland:
- ForDateSourced adds candidacy lines only 'if (CandidacyRefuses(country))' (:474), and CandidacyRefuses is Sweden-only (:43).
- AddCreatedLines uses the candidacies only under CandidacyRefuses (:599).
- Formateur.Answer reads reading.Candidacies only 'if (DeclaredRedLines.CandidacyRefuses(country.Id))' (FormationProposal.cs:146-149), so the finding's FormationProposal claim does not apply to Poland.
- SpeakerOrder and PmOf (SimulationManager.cs:3245, :3266, :3394) run only in Speaker's rounds. ConfidenceProcedure.cs:34 gives Poland Rules.Unsourced, so RoundsApply (:3223-3225) is false. Every round opening is gated:
  - OpenRoundAfterElection (:4004)
  - GameController.cs:6409
  - UiScreenshotDriver.cs:942
  - DischargeAndRound needs NoConfidenceOn, which MoveNoConfidence refuses for Unsourced (:2824) and TryAiMotion skips (:3019-3020).
- GovernmentRecord.FromView reads the dated CandidaciesAt (GovernmentRecord.cs:301).
- CandidacyParties has no callers.
- A Polish election reads DeclarationReading.OfElection, which is dated because HasTimeline(Poland) is now true (:662).
(4) §776 does follow §705's pattern. §705 routed one vintage reader per fact kind Germany's timeline carries (3 PairLine + 4 Candidacy: ForSourced and CandidaciesSourced). InOrAgainstForSourced (:239) still returns empty for Germany. Poland carries PairLine only and §776 routed ForSourced only. The COMPLETED.md §776 'Wired' list names exactly that and makes no candidacy-route claim.*
- [wiring] The 2023 facts are open-ended, so the 2027 (and 2055) formations read 2023's pre-election words; the record never says so - *The mechanics in the finding are right. The claim that "the record never says so" is not.

Mechanics, confirmed:
- DeclaredRedLines.cs:300 `StandsOn => date.Date >= From && date.Date < Until`, and :306 `Open = DateTime.MaxValue`.
- Ten Polish facts stand on 2023-10-15, all with Until = Open (:398, :412, :414, :416, :418, :420, :422, :424, :426, :430).
- WorldClock.TryNextPollingDay(Poland) gives 2027-10-17 (FirstSundayOfTermWindow from the 2023-11-13 first sitting). SimulationManager.cs:4033 opens the round with electionDay: held, and RoundReading (:3283) calls DeclarationReading.OfElection.
- OfElection (DeclaredRedLines.cs:659-663) takes VintageOfElection, which returns Poland2023 (the latest election before). Because HasTimeline(Poland) is now true, the reading is dated with LinesOn = 2027-10-17. ForDate then adds all ten open facts. So the 2027 formation does read 2023's words.

The claimed omission does not hold. Both documents state the carry-forward in rule form:
- The record's head, lines 8-10: "**The formation reads them (§776):** Poland is dated as Sweden and Germany are ... An election reads every fact standing on its own polling day; a mid-term round reads the lines standing that day." That covers any election, 2027's included.
- Record line 15 (§621): "a declaration stands until a later dated one replaces it." §8's `until` column says Open for every fact standing on polling day.
- Record §12, line 249: "So doubts 1 and 2 below change no 2023 formation; they bear on other counts." This says outright that the facts reach counts other than 2023's.
- Record doubt 13, line 271: "A later vintage may lift the line." This is the finding's own proposed sentence ("stand ... until a later vintage is sourced"), already there.
- COMPLETED.md §776, line 37133 (Wired): "An election reads every fact standing on its own polling day; a mid-term round reads the lines standing that day." Line 37113 has NL → PiS standing "by §621's third rule".
- The code comment at WorldClock.cs:450 (VintageOfElection): "the latest dated declarations stand until newer ones are sourced".
- The carry-forward is checked as well as stated. The new DeclarationDatesDiagnostic check, "its {0} open facts stand on 18 Jan 2026, the ones closed in 2023 do not" (n776b.log:624, ok), holds the open facts standing after the 2023 vote.

Precedent: neither Sweden's 2026 record nor Germany's 2025 record names a later election, and neither states the stands-until rule. Poland's is the only record that states it (grep: only coalition_declarations_2023.md:15). The Sweden-only line in CLAUDE.md:54 is a standing rule about Sweden's 2026 set, not a requirement on a record.

The fix's second half would cut against the repo's claim convention (CLAUDE.md head). Naming "2027 as the next formation" in the record would transcribe a date the code computes (TryNextPollingDay, ruling D3); that is a DERIVED claim, which must be generated, referenced or deleted.

No failing path: the behaviour is the ruled one (§621's third rule), it is held by a check, and the record and §776 both state it.*
- [wiring] IsSourced is country-wide, and the new Polish mid-term reading has no caller - *I checked every code fact in the finding against the working tree, and each one is accurate. None of them leads to a failing path, and the reviewer itself says the scenario is "unreachable in play today".

1. The flag is country-wide, but it is only ever read on election-day readings. GovernmentFormation.cs:425 sets `declarationsSourced = DeclaredRedLines.IsSourced(country);`. That line is older than this change (§776 does not touch GovernmentFormation.cs); §776 only adds Poland to IsSourced. The flag has two readers:
   - GameController.cs:6437 reads `government.DeclarationsSourced`, where `government` comes from `Form(_playerCountry, DeclarationReading.OfElection(PlayerCountryId, latest.Date))` at :6400-6401.
   - ElectionNightScreen.cs:713 reads `government.DeclarationsSourced`, where the view comes from GameController.cs:6592, `ViewOf(_playerCountry, DeclarationReading.OfElection(PlayerCountryId, pollingDay))` with pollingDay = CurrentDate.

   GovernmentRecord.FromView (:291-306) stores no flag. Both readings are election-day readings, and a Polish game's first polling day is 2023-10-15 (WorldClock.LatestElectionDay). On that day the timeline's open facts stand among seated parties: PiS-Konf, Konf->PiS/KO, Konf->TD/NL/MN, TD->PiS/Konf, NL->PiS/Konf. Every one of them is Open-ended, so they also stand on 2027-10-17. Wherever the flag is shown, "sourced" is therefore true.

   The Poland2019 reading (2019-10-13, with no fact standing) is only reached through Seated-vintage paths, and none of them shows the flag:
   - TryFormChamber returns the installed Morawiecki record (SeatedGovernment.TryInstalled, WorldClock Governments(Poland): CabinetSourced) until the game holds an election.
   - ViewOfSitting with no date is called only from editor diagnostics.

2. It is true that the mid-term branch has no Polish caller, but that is dormant ruled behaviour, not a defect. DeclarationReading.MidTerm is reached only through GovernmentFormation.SittingReading, which has three callers:
   - RedLinedFrom(asOf), via ConfidenceProcedure.Vote. Its callers all stop Poland first: MoveNoConfidence refuses at SimulationManager.cs:2824 ("THIS COUNTRY'S CONFIDENCE RULES ARE NOT YET MODELLED"), TryAiMotion returns at :3020, and DrawConfidence returns at GameController.ParliamentRows.cs:616.
   - ViewOfSitting(asOf), whose only runtime caller is TryAiMotion (:3034).
   - RoundReading on a MidTerm round:
     - DischargeAndRound needs NoConfidenceOn, which is set only on the Riksdag paths at :2858 and :3043.
     - DraftSuccessor is Bundestag-only.
     - The film harness is gated by RoundsApply (UiScreenshotDriver.cs:942), and RoundsApply (:3223-3225) is false for Poland because ConfidenceProcedure.RulesOf(Poland) is Unsourced (:34).

   E2's unstaged SimulationManager changes touch none of these gates.

3. The documentation is accurate:
   - Line 10 of the record states §653's ruled reading rule ("a mid-term round reads the lines standing that day"). Since HasTimeline(Poland) is now true, DeclarationReading.MidTerm does read Poland that way. The sentence does not claim a Polish round runs, and doubt 12 (record §12) already treats a mid-term round on the 2019 chamber as not proposed.
   - COMPLETED.md is a dated record and is exempt from the claim convention.
   - Under the convention's test, nothing here becomes wrong.*
- [diagnostic] The platforms are read through the vintage API, which ignores the timeline for every country but Sweden 2026; the game's election night reads the dated API - *The code reading is accurate, but nothing fails in the current tree. The scenario needs a future data edit, and a check is already built to trip on that edit.

1. The asymmetry is real but carries no data today. DeclaredRedLines.cs:239 (`if (country != CountryId.Sweden || vintage != ElectionVintage.Sweden2026) { return rules; }`) never reads PolandTimeline, while InOrAgainstAtSourced (495-506) reads only InOrAgainst/NoSupportRole facts. All 17 PolandTimeline facts (396-432) are FactKind.PairLine. The doc comment at 392 and record §8 line 196 both say: "No candidacy, in-or-against or support-role fact." So for Poland InOrAgainstFor and InOrAgainstAt both return only AddCreatedRules, the same output. Candidacies are also empty both ways (CandidaciesSourced 162; no Candidacy facts), and CandidacyRefuses(Poland) is false (43), so a Polish candidacy draws no line on either path.

2. The gap is older than §776. Line 239 is unchanged from HEAD, where Germany has had the same in-or-against gap since §705. The §776 diff touched only IsSourced, ForSourced:53, TimelineOf and HasTimeline.

3. (f) matches the game today. n776b.log:520 has (f) ok: KO+TD on NL's support, 248. The game's Polish formations all read the dated path:
- GameController.cs:6400-6403 and 6592 use OfElection.
- SimulationManager.cs:3034 passes CurrentDate (MidTerm).
- The start and picker use the installed PiS record (WorldClock Poland CabinetSourced true; GovernmentRecord.AtStart:258, PickerViewOf:567-570).
- The vintage overloads of Formateur and SpeakerOrder are never reached for Poland: ConfidenceProcedure.RulesOf(Poland) == Unsourced, so RoundsApply is false.
- On 2023-10-15, ForSourced(Poland, Seated) and ForDate both resolve to ForDateSourced.

4. A check already trips. PolishDeclarationsDiagnostic.cs:59-66 (a) requires rows.Count == facts.Count and `f.Kind == PairLine` for every fact. Adding any NoSupportRole, InOrAgainst or Candidacy fact turns (a) red: the table regex needs "→ other", and the Kind test fails either way. The finding concedes this.

5. Even the finding's named hypothetical would not flip (f). Record doubt 6 (line 264) says the Konfederacja NoSupportRole reading "changes no 2023 formation". Konf already blocks support for KO one way, and (f) tests only cabinet|support == KO|TD|NL with PiS outside.

6. FormationSweepDiagnostic:103 is not a claim about the game's path. It reads InOrAgainstFor for every non-Swedish country, Germany included. Per its docstring it is a pin that a refactor of the evaluator changes nothing.

Regrade: a latent hardening note for when Elias rules doubt 6, not a defect in §776.*
- [diagnostic] The R0-R4 print would name a nonexistent line ('refused (PiS)') when the cabinet is refused for a seatless member or a split group - *The mechanism the finding describes is accurate. In CoalitionFormation.cs:524-531, Evaluate sets InternalLine only when TryFindInternalRedLine finds a line. A refusal for a seatless member (528) or a split group (531) leaves InternalLine at default(RedLine), with A = B = 0. Line 154 would then print Names(1), and in the working-tree roster parties[0] is PiS (PartySystem.cs:448). But neither of those refusals can happen in this diagnostic.

1. Seatless member. Line 106 builds the seats as `parties.Select(p => p.SeedSeats)`, and SeedSeats is `public readonly int SeedSeats;` (PartySystem.cs:77). Its values are literals: KO 157, TD 65, NL 26 (PartySystem.cs:449-451). Only SLD, PSL and MN hold 0 seats (457-459). Created parties are appended after the real roster (PartySystem.cs:681-682), so they cannot move KO, TD or NL. The cabinet is fixed as `ofRecord = Mask("KO", "TD", "NL")` (line 139), so `(ofRecord & chamber.SeatlessMask)` is always 0 (CoalitionFormation.cs:420, 527).

2. Split group. Line 108 calls `ChamberRules.JointMasks(CountryId.Poland, ...)`. JointGroups is `country == CountryId.Germany ? new[] { ("CDU", "CSU") } : Array.Empty` (GovernmentFormation.cs:484-485), so for Poland JointMasks returns null (502). `SplitsJoint(cabinet, null)` returns false (CoalitionFormation.cs:740).

So for this cabinet, Admissible is false only when a line is found, and line 525 then sets InternalLine, so the print names the real pair. The runs agree: n776a.log:525-529 and n776b.log:513-517 print "the record's cabinet viable" under all five readings, so the refused branch is never taken.

To reach the misprint you would have to change code: a different seats array, the readonly seeds, or a Polish joint group. The finding itself says it "cannot happen on the 2023 seed seats". Even after such a change, line 150 (`recordFormable &= recordEval.Admissible && recordEval.Wins`) would make the Check at line 157 FAIL. The verdict would still be correct and loud; only the reason in brackets would be wrong. This is a latent cosmetic weakness in a diagnostic's print, not a failing path in the change.*
- [diagnostic] The record's §12 claims every reading seats the record's majority; the diagnostic asserts that for R1 only - *The finding is right on one point: only R1's government is asserted. R0 and R2–R4 are printed but not checked. That is deliberate, and the failure it describes can't happen through the changes it names.

1. **The design says so.** PolishDeclarationsDiagnostic.cs:28 says "The readings are measured, not asserted." Line 31 says "Which of the other readings the game runs is Elias's." The repo already does this: Formation2026Diagnostic.cs:19 says "Prints only.", and the Swedish records carry MEASURED tables that name the diagnostic and the log (coalition_declarations_2026.md:652, :717). Section 12 follows the same form. Its line 240 dates the measurement and points at the diagnostic: "Measured before the formation read them (2026-10-04, §776; `PolishDeclarationsDiagnostic` prints each reading's government as a MEASURED line)".

2. **R3 and R4 can never seat PiS+KO.** Line 129 builds the reading as `new RedLine(Index("PiS"), Index("KO"), RedLineKind.Declared, true, ...)`. `TryFindInternalRedLine` (CoalitionFormation.cs:723) rejects any cabinet that contains both parties of any line.

3. **A weights change can't move R2 without also moving R1.** R2's "PiS+KO 351" is the same cabinet as R1's "PiS+KO on TD 416". In n776b.log:514-515 it ranks 2nd in both. A cabinet's score doesn't depend on its supporters (CoalitionFormation.cs:428, "The score uses only cohesion, seats and pivotality, so it does NOT depend on who supports whom"; :446 `baseScore[cabinet] = WeightCohesion*...`; :343 sorts by Score). So a weights change that puts PiS+KO first in R2 puts it first in R1 too, and line 169 then fails on `(formed.Government.Cabinet & Mask("PiS")) == 0`.

4. **A hold-out change has no effect on Poland.** `holdsOut` (:556) feeds only the opposition mask. :597 is `Wins = supported >= Majority || (NegativeRule && opposed < Majority)`. GovernmentFormation.cs:475 is `UsesNegativeParliamentarism(country) => country == CountryId.Sweden`. Poland forms under positive investiture, so only supported seats decide.

5. **Under the current rules, R1's result carries over to R2–R4.**
   - The defection test (`WouldHold` :705, with `Payoff` :682) uses seat share times cohesion and no weights.
   - KO+TD on NL can never pass that test while the KO+TD+NL cabinet exists: NL, outside the cabinet, scores 0 there and does better inside KO+TD+NL. Line 157 checks that KO+TD+NL can form under every reading.
   - So KO+TD on NL wins only when the defection round is dropped for emptying the set (:339 `kept.Count == 0 -> break`). It then wins as the top score among all viable cabinets.
   - R2–R4 add lines to R1, so their viable cabinets are a subset of R1's. That subset still holds KO+TD on NL, with the same support (NL), so it is still the top score.

6. **Something would go red.** Any weights change moves FormationSweepDiagnostic's pinned digest, because every option's score is printed into it (FormationSweepDiagnostic.cs:131, `score {7:R}`). That is a bar failure, and the same bar prints the new MEASURED lines.

7. **DocumentClaimCheck doesn't apply.** It only resolves backticked `Type.Member` names, so it could not check an outcome sentence wherever it scanned.

Two things are left, neither the finding's:
- Only a rewrite of how defection or support works could separate R2–R4 from R1. That is speculative, and it would itself move the pinned sweep.
- Small separate point: section 12's exact shape ("KO and TD in the cabinet on NL's support, 248 seats") and its "one miss left" sentence aren't checked even for R1. Line 169 accepts any arrangement whose cabinet plus support is KO, TD and NL. That paragraph is dated, though, and an arrangement change would leave "doubts 1 and 2 change no 2023 formation" true.*

## What the author did about the first pass

Three defects, all confirmed; all fixed. Two changed what §776 tells Elias.

- **1 (defect) - fixed as the corrected fix asks.**
  - **The rule, quoted.** The record quotes §621 as ruled: *"dated by the party's own record, never by press reporting; a document by the decision it records; a declaration stands until a later dated one replaces it"*.
  - **The extension, stated.** A leader's or spokesperson's own spoken words, quoted verbatim, dated by the broadcaster's or agency's page. It is put to Elias as K-1i (3), with §652 as its precedent (ruled for MP) and §639's KD line the same open question.
  - **Its nine facts, marked** in the prose, in §8's table, and in the code (`DeclaredRedLines.PolandExtension`, appended to each basis): the June lines and their lift (2-6), Konfederacja's support half (8-9, which also end 7), TD → PiS (13) and NL → Konf's support half (17, which also ends 16).
  - **The code doc.** `PolandTimeline`'s doc states the rule and the extension.
  - **The feature list.** K-1i carries the Polish cases, and notes (2) as answered by §652.
  - **Measured, so Elias rules on numbers.** `PolishDeclarationsDiagnostic` adds R5, the party's own record alone. No party's own page carries a support half, so on polling day every declared line reads cabinet-only.
- **9 (defect) - fixed, and it changed the conclusion.**
  - **The played count is now measured.** The diagnostic replays a Polish game's own path (seed 777, no policy, the live prediction counted through the 41 districts, as `PollingDayDiagnostic` (5) does). It forms that count by the game's own entry and under every reading. The game's entry is held to R1's cabinet.
  - **The result** (n776c), as the skeptic computed:
    - R1 seats PiS+KO on TD's support;
    - R2 seats PiS+KO;
    - R3 and R4 seat KO+TD+NL, the cabinet of record;
    - R5 seats PiS+KO on TD's and Konfederacja's support.
  - **The film agrees.** Its warm-up formed PiS+KO after the wiring (`real776`), and PiS+KO+TD before (`film767pl`).
  - **The claims, corrected.** Check (f) pins the 2023 election by name. The chamber of record's check is now (g), a backtest. The record's §12 and §776's record carry the two-chamber table. Doubt 1 goes back to Elias as the question that decides a Polish game's 2023 government.
- **10 (defect) - fixed.** Every "said DERIVED on every formation" is corrected: Poland's record (header, §10, §12), Germany's record (the same sentence, false since §705), and §776's record. Doubt 14 is put on the true footing: nothing marks the pairs left DERIVED; a mark needs a reading-level state, not `IsSourced`; and it reaches no Polish player until D-PL's night exists.
- **2 - fixed.** Facts 8, 9 and 17, and the record's §2 and §4, name the half each restatement carries. Konfederacja's support half is restated only on 2023-08-24 [KONF-I19]. 10 October's "nie dopuścić" is read as an aim. NL's support half is not restated.
- **3 and 19 - fixed.** The five pages the doubts lean on are held in tree and registered: [KONF-P3] (Bosak's post of 2023-10-16), [TD-P11], [TD-I13], [TD-I14] and [NL-P30]. Check (c) now holds 59 files at their digests. The header is narrowed to the sources the facts and §§1-11 cite by tag, plus the pages the doubts lean on. Doubt 2's quotation is tagged as its page renders it.
- **4 - fixed.** The fetch log says what each out-of-tree folder holds, and which have no URL list.
- **5 - fixed.** The *(reported)* mark is applied evenly (RMF24 2023-06-20, TVN24 2023-07-16 and 2023-10-11, Interia 2023-03-30). KONF-I8's createdDate is in its register row and is doubt 4's bound.
- **6 - fixed.** §10's figures are replaced by references to `DerivedRedLines.From` and its two gaps, with a dated measurement. Doubts 1 and 2's figures are bracketed as the record's notes. The diagnostic prints each derived line with its basis. `PolandTimeline`'s doc says "the lines the record carries for the 2023 lists", the member parties' earlier refusals being recorded, not carried.
- **7 - fixed.** Doubt 9 names the page its 2023-09-03 date belongs to, and §11 lists it as dropped.
- **8 - fixed.** KONF-P1's attribution is narrowed to "Wipler on Onet Rano" in the record, the code and doubt 6.
- **11 and 20 - fixed.** These now point to `IsSourced` or `HasTimeline`:
  - `DeclaredRedLines`' class summary;
  - `GovernmentFormation`'s doc;
  - the run-up chip's doc;
  - `OfficeTestDiagnostic`'s doc and printed banner.

  `DeclaredRedLines`' "no runtime surface reads the timeline yet" is replaced by its readers.
- **12 - taken in the record.** §776 claims no visible page. The run-up page sits behind a staged run-up or campaign, which Poland lacks. A TRACKING note holds the "would support" tag to the record before the page opens for Poland.
- **13 - answered.** `real776` and `drys776` ran on the wired tree, and §776 records the warm-up's government.
- **14 - fixed.** (f) reads `ElectionVintage.Poland2023` by name and also holds the election's own dated reading (`DeclarationReading.OfElection`): same lines, no platforms.
- **15 - fixed.** New (a2): every fact's keys are Polish roster keys. (f) also holds the declared lines to exactly the standing set's count.
- **16 - fixed.** `DeclarationDatesDiagnostic` holds every dated timeline, not Sweden's alone, to its own order and to no overlap.
- **17 - fixed.** The MEASURED header prints the investiture rule from its variable, and the class summary names `ChamberRules.UsesNegativeParliamentarism`.
- **18 - fixed.** The pin comment gives the before-state once for both rules (PiS+KO+TD on Konf's support), and points to the two texts and the MEASURED lines for the figures.
- **The nine refuted findings** are recorded verbatim; none needed a change.

**Measured after the fixes:** n776c, 11 of 11 clean - the Polish declarations, the declaration dates, the formation sweep (its new pin), the office test, Germany's formation, the dead-state and unwired checks, and the four text checks.

## The second pass - confirmed (verbatim)

The same workflow on section 776's files after the first pass's fixes, the author's response read first; two lenses (the record and the timeline, the rewritten diagnostic and the claims built on it).

### 1. Germany's corrected sentence says no surface marks a derived line, but the German formation sheet prints the line's DERIVED basis

- **Lens:** record - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/germany/coalition_declarations_2025.md:5

**The scenario.** Rounds apply in Germany: RoundsApply covers the Bundestag (SimulationManager.cs:3223-3225), and the player's party reaches PlayerAsked (:3432), so the formation sheet opens. Say a German player drafts CDU+CSU+Grüne. FormationProposal.cs:171 then sets each refusing partner's answer to "refuses: a red line falls inside this cabinet - " plus the line's Basis. For a derived line, CoalitionFormation.cs:957 makes that Basis "DERIVED: CHES galtan gap 5.93 > 5.00". GameController.FormationSheet.cs:310 draws it on the answer slip, and supporters get the same text through CoalitionFormation.cs:614 (SupportBlockBasis). G:/UNITY/Projects/PoliSim-captures/logs/n754.log:692 shows that exact string for CSU and Grüne, and Germany has 10 derived lines on polling day (n776c.log:2028). So "What is NOT here stays DERIVED - and no surface marks it" is false for Germany, and COMPLETED.md:37163 ('Stale text corrected') repeats it. Poland's line 10 holds only because RulesOf(Poland) is Unsourced, so its sheet never opens and Poland has no election night. It does not hold because the country-wide caveat is the formation's 'only' DERIVED caveat.

**The fix proposed.** For Germany, state what is true. The country-wide caveat (the night's caption and the verdict's note) is keyed on IsSourced, so it never shows. A derived line shows its DERIVED basis only on the formation sheet's answer slip, when it refuses a drafted cabinet or a supporter. A pair with no line is marked nowhere. In Poland's line 10, give the real reason: no formation sheet (Rules.Unsourced) and no night.

**The skeptic's evidence.** I could not refute it. Every step of the scenario holds in the working tree.

**The sentence §776 wrote.** ElectionsData/germany/coalition_declarations_2025.md:5 now reads: "What is NOT here stays DERIVED - and no surface marks it: the formation's only DERIVED caveat covers a whole country and is keyed on `DeclaredRedLines.IsSourced`, which Germany meets since §705".

**Germany's lines include derived ones, listed first.**
- DeclaredRedLines.cs:53 sends Germany to ForDateSourced.
- In ForDateSourced (:477-492), `List<RedLine> lines = DerivedRedLines.From(lrGen, galtan);` comes first; the declared facts are appended after it.
- n776c.log:2028: "3 declared beside 10 derived" on 23 Feb 2025. The three declared lines are CDU-AfD, CDU-Linke and CSU-AfD, so CSU-Grüne is one of the pairs the record does not carry.
- CoalitionFormation.cs:957 writes the basis "DERIVED: CHES galtan gap {0:F2} > {1:F2}".
- PartySystem.cs:391 and :393 give galtan 1.61 for Grüne and 7.54 for the CSU. The gap is 5.93, past 5.00, so this line exists in the current tree.
- Prepare (CoalitionFormation.cs:402-418) keeps the lines in their order.

**The German formation sheet opens.**
- SimulationManager.cs:3223-3225: RoundsApply holds for `Rules.Bundestag`, and ConfidenceProcedure.cs:34 maps Germany to Bundestag.
- :3432 sets `PlayerAsked` when the party asked is the player's. Phase 1's order is CDU, AfD, SPD, Grune (n776c.log).
- GameController.cs:9647-9656 (AutoOpenFormationSheet) opens the sheet on `PlayerAsked`.
- FormationSheet.cs:120-140: any seated party can be toggled ● IN / ○ OUT.

**The derived basis reaches the slip.**
- The sheet's verdict goes GameController.cs:9675 → PreviewFormation (SimulationManager.cs:4056-4062) → Formateur.Answer.
- A partner: FormationProposal.cs:171 sets `answer.Reason = "refuses: a red line falls inside this cabinet - " + inside.Value.Basis`.
- A supporter: :201 calls SupportRefusal, which at CoalitionFormation.cs:614 adds `SupportBlockBasis`; that returns `line.Basis` (:661-668).
- GameController.FormationSheet.cs:310 builds `SlipWrapped(..., Name(answer.Party) + " " + answer.Reason)`, and the class doc says "the reason is the slip".
- n754.log:692 shows the string itself: "CSU refuses: a red line falls inside this cabinet - DERIVED: CHES galtan gap 5.93 > 5.00; Grune refuses: ... DERIVED: CHES galtan gap 5.93 > 5.00".
- So a CDU player who drafts CDU+CSU+Grüne, or asks Grüne to support CDU+CSU, sees the line marked DERIVED. "No surface marks it" and "the formation's only DERIVED caveat" are false for Germany.

**Two related texts.**
- The German record now contradicts itself. Its own §5, line 70 (not in the diff), still says "A formation that turns on them says it formed on derived lines." The first pass's Polish analog of that sentence was corrected (poland line 217, "Nothing in play marks…"); this one was missed.
- COMPLETED.md:37163 records the German correction as made.

**The Poland half is only a nit.** Poland's conclusion holds:
- ConfidenceProcedure.cs:34 gives Poland `Rules.Unsourced`, so there is no round and no sheet.
- ElectionNightFromModel.cs:38-39 makes the night Sweden's and Germany's only.
- The eight unsourced pairs carry no line at all. For them, the country-wide caveat really is the only thing that could mark them.

**An aside, older than §776 (since §705).** LineInside (FormationProposal.cs:239-247) and SupportBlockBasis name the first matching line, and derived lines come first. So where a pair has both a derived and a declared line, the slip names the derived basis. An example is CDU-Linke: an lrgen gap of 5.16 against 4.50, beside the CDU's declared 2018 resolution.

**Severity: minor.** The fault is in the record's text only and changes no computation. No owed ruling rests on the German sentence. It is still a false claim about the code, which CLAUDE.md's claim convention forbids, and it puts the record at odds with its own §5.

**The skeptic's corrected fix.** The fix is text only, and no figures should be transcribed into the record.

1. **Germany's record, line 5.** Replace "and no surface marks it: ..." with a true statement:
   - No caveat says so for the country. The night's caption (ElectionNightScreen) and the verdict's note (GameController) are keyed on `DeclaredRedLines.IsSourced`, which Germany meets since §705.
   - A derived line's own basis ("DERIVED: CHES ... gap") shows only on the formation sheet's answer slip, when it is the line that a partner's refusal or a supporter's refusal names (FormationProposal / CoalitionFormation.SupportRefusal → FormationSheet's answer slip).
   - A pair with no line is marked nowhere.

2. **Germany's record, §5 (line 70).** Correct "A formation that turns on them says it formed on derived lines." the same way, or point it to the header. The author's "every 'said DERIVED on every formation' is corrected" missed this sentence, and it now contradicts line 5.

3. **COMPLETED.md:37163.** Describe what Germany's sentence now says: the country-wide caveat never shows, and the sheet's slip names a derived line's basis.

4. **Poland's line 10 and COMPLETED.md:37165 (optional).** The conclusion holds. If they are touched, give the real reasons:
   - Poland has no sheet (`Rules.Unsourced`) and no night.
   - The eight unsourced pairs carry no line, so only a country-wide or reading-level mark could reach them.
   - Drop "the formation's only DERIVED caveat" as a general claim; the sheet names derived bases in Sweden and Germany.

5. **A caution for the new wording.** The sheet names the first matching line, and derived lines are listed first (ForDateSourced). Where a pair also has a declared line (Germany's CDU-Linke), the slip names the DERIVED basis, not the declared one. The new text must not claim that the slip names the declared line. That ordering is a separate issue, older than §776.

### 2. Record §2: the coalition-half list quotes an aim for [KONF-I23] and credits PiS-only restatements to the KO line

- **Lens:** record - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:73

**The scenario.** Under 'Konf → PiS and Konf → KO', §2 says the coalition half is restated by [KONF-I15], [KONF-P1], [KONF-I23] and [KONF-I25]. In the saved bytes, KONF-I15 refuses PiS only ("Nie ma i nie będzie żadnych rozmów na temat współrządzenia z PiS-em"). KONF-I23's only cabinet sentence also names PiS alone ("Gdybym ja teraz, po tych wszystkich zapowiedziach, zrobił rząd z PiS-em, straciłbym całkowicie wiarygodność"). Fact 9 in the code (DeclaredRedLines.cs:425) correctly gives KO's coalition half only [KONF-P1] and [KONF-I25], and says [KONF-I23] restates neither half. The quote §2 gives for [KONF-I23] is "chcemy zakończyć rządy PiS-u i nie dopuścić do rządów Donalda Tuska". Both clauses of that sentence are aims under §10, and fact 8 (DeclaredRedLines.cs:423) calls "chcemy zakończyć rządy PiS-u" an aim, yet §2 flags only "nie dopuścić". The result: Elias, weighing doubt 5 from §2, sees four coalition restatements behind Konf → KO where the bytes and the code carry two, and an aim offered as a cabinet refusal.

**The fix proposed.** Split the list by pair. Konf → PiS: 07-16 [KONF-I15], 08-02 [KONF-P1], 10-10 [KONF-I23] (quote its 'zrobił rząd z PiS-em' sentence) and 10-11 [KONF-I25]. Konf → KO: only 08-02 [KONF-P1] and 10-11 [KONF-I25]. Read 10 October's whole sentence as an aim, as fact 8 does.

**The skeptic's evidence.** THE RECORD (ElectionsData/poland/coalition_declarations_2023.md):
- Line 67 is one heading for both pairs: "**Konf → PiS and Konf → KO** — ONE-WAY, support-blocking, from 2023-07-13".
- Lines 73-76 give one undivided list under it: "**The coalition half restated** 2023-07-16 *(reported)* [KONF-I15], 2023-08-02 [KONF-P1], 2023-10-10 ("chcemy zakończyć rządy PiS-u i nie dopuścić do rządów Donalda Tuska." [KONF-I23] - its "nie dopuścić" read as an aim, §10) and 2023-10-11 ... [KONF-I25]".
- §2 has no other list of KO restatements, so a reader cannot get fact 9's narrower list from it.
- §10 (lines 217-219): "an aim to remove a party from power, or keep it out, is not a line".
- §2's own lines 64-65 already read KONF-P2's "chcemy zakończyć rządy PiS" as an aim.

THE CODE:
- DeclaredRedLines.cs:423 (fact 8, Konf→PiS): "Its cabinet half restated 2023-07-16 [KONF-I15], 2023-08-02 [KONF-P1], 2023-10-10 [KONF-I23] and 2023-10-11 [KONF-I25]; ... 10 October's \"chcemy zakończyć rządy PiS-u\" are aims".
- DeclaredRedLines.cs:425 (fact 9, Konf→KO): "Its cabinet half restated 2023-08-02 [KONF-P1] and 2023-10-11 [KONF-I25]; ... 10 October's \"nie dopuścić do rządów Donalda Tuska\" [KONF-I23] is read as an aim, restating neither half."
- So the code splits the list by pair and the record does not.

THE SAVED BYTES (raw/declarations_2023/Konf/):
- KONF-I15 (tvn24_bosak-nie-wejdzie-w-koalicje-z-pis.html): Bosak says "Nie ma i nie będzie żadnych rozmów na temat współrządzenia z PiS-em, na temat koalicji z PiS-em. My chcemy PiS odsunąć od władzy". "Tusk" appears 0 times and "Platform* Obywatelsk" 0 times. "Koalicji Obywatelskiej" appears only in a KO MP's own words. So it says nothing about KO.
- KONF-I23 (polsatnews_2023-10-10_mentzen-gosc-wydarzen.html) has the sentence §2 quotes: "chcemy zakończyć rządy PiS-u i nie dopuścić do rządów Donalda Tuska". Under §10 both clauses are aims.
- KONF-I23's only sentence about a cabinet is: "Naszym zadaniem jest zatrzymać wariactwa PiS-u i Platformy. Gdybym ja teraz, po tych wszystkich zapowiedziach, zrobił rząd z PiS-em,&nbsp;straciłbym całkowicie wiarygodność - mówił Sławomir Mentzen, pytany o koalicję z partią Jarosława Kaczyńskiego lub ugrupowaniem Donalda Tuska." He was asked about both parties and refused PiS alone.
- KONF-P1 ("Koalicja z PiS czy z Platformą? Z nikim!") and KONF-I25 ("ani z Koalicją Obywatelską, ani z PiS-em") name both pairs.

WHY THIS IS NOT AN ANSWERED FINDING:
- First-pass finding 2's corrected fix gave KO the narrower list (KONF-P1 and KONF-I25). It said KONF-I23's cabinet sentence "names PiS alone", and it said to "mirror each change in the record".
- The author's answer (Review line 1131) says "the record's §2 ... name the half each restatement carries". That is true for the half but not for the pair: only fact 8's list was copied into the record's joint entry.
- The KONF-I23 quote the fix put in the record offers two aims as a coalition restatement, and flags only "nie dopuścić" as an aim.

REACH:
- Nothing checks the record's prose against the code: PolishDeclarationsDiagnostic.cs:75 only checks that every tag in a Basis is registered.
- No behaviour changes. Facts 8 and 9 keep their flags, dates and tags, and the code's player-visible Basis is already correct.
- The defect is only in the record Elias reads. The finding's "weighing doubt 5" overstates this a little: doubt 5 is about the support half, and §2 gets that right (KONF-I19 alone).

**The skeptic's corrected fix.** Text only, in the record's §2, lines 73-77. Change no code, flag, date or tag, and do not re-pin. Split the list by pair to match facts 8 and 9:

"**The coalition half restated** - toward PiS: 2023-07-16 *(reported)* [KONF-I15], 2023-08-02 [KONF-P1], 2023-10-10 ("Gdybym ja teraz, po tych wszystkich zapowiedziach, zrobił rząd z PiS-em, straciłbym całkowicie wiarygodność" *(decoded)* [KONF-I23]; asked about a coalition with PiS or Tusk's party, he named PiS alone) and 2023-10-11 *(reported)* [KONF-I25]; toward KO: only 2023-08-02 [KONF-P1] and 2023-10-11 [KONF-I25]. 10 October's "chcemy zakończyć rządy PiS-u i nie dopuścić do rządów Donalda Tuska" [KONF-I23] is two aims (§10), restating neither half of either line."

Keep the support-half clause ("only 2023-08-24 ... [KONF-I19]") and the "held after the vote" clause as they are.

The full "Gdybym" sentence must carry the *(decoded)* mark, because the page's <p> text has "&nbsp;" between "PiS-em," and "straciłbym". Otherwise, quote only the span that appears verbatim once in the bytes: "Gdybym ja teraz, po tych wszystkich zapowiedziach, zrobił rząd z PiS-em,".

COMPLETED §776's generic line ("the other restatements carry the coalition half") stays true and needs no change.

### 3. [KONF-P3] is cited as the support half holding after the vote, but the post carries only the coalition half

- **Lens:** record - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:77

**The scenario.** The fix restructured §2, which now reads "the support half only 2023-08-24 (... [KONF-I19]); held after the vote (doubt 5, [KONF-P3])". Before, 'held after the vote' followed the whole restatement list. Doubt 5 (line 288), a doubt about the support half, likewise says "The line held after the vote: Bosak on X ... [KONF-P3]". The newly registered post (Konf/x_bosak_1713834658134138996_syndication.json, digest a8d8a865… verified) says only "nie wejdziemy w koalicję ani z PiS ani z PO": the coalition half, with no word on support. Elias, deciding whether the support half is solid, is told it held after the vote on a source that never mentions support.

**The fix proposed.** In §2 write "its coalition half held after the vote ([KONF-P3])". Add a bracket to doubt 5: "[record: [KONF-P3] carries the coalition half only]".

**The skeptic's evidence.** CLAIM, record line 77 (section 2, after the fix): "**the support half only 2023-08-24** ("Konfederacja nie zamierza ani przedłużać władzy PiS-u, ani ułatwiać Tuskowi powrotu do władzy" [KONF-I19]); held after the vote (doubt 5, [KONF-P3])."

CLAIM, line 288 (doubt 5, about the support half, no record bracket): "The line held after the vote: Bosak on X, 2023-10-16, "nasza deklaracja z kampanii wyborczej, że nie wejdziemy w koalicję ani z PiS ani z PO, dzień po wyborach jest nadal aktualna" [KONF-P3]."

BYTES:
- File: raw/declarations_2023/Konf/x_bosak_1713834658134138996_syndication.json. sha256 a8d8a865a45815a3336c0b85c854863f2937c9b74b37f2a663c92a3dbfa1b714, matching SHA256SUMS.txt:22 and register line 356.
- Its whole text: "W związku z licznymi pytaniami dziennikarzy pragnę poinformować, że nasza deklaracja z kampanii wyborczej, że nie wejdziemy w koalicję ani z PiS ani z PO, dzień po wyborach jest nadal aktualna. Nie sprzedamy się. Będziemy dobrze reprezentować naszych wyborców!"
- The "że ..." clause defines the declaration as the coalition refusal. There is no przedłużać, umożliwić or ułatwiać wording.
- The out-of-tree polsatnews_2023-10-16_bosak-jasna-deklaracja.html frames the same post only as "Konfederacja wejdzie w koalicję z PiS lub KO?".

BEFORE THE FIX:
- scratchpad record776.bak:70 reads "Restated 2023-07-16 [KONF-I15], ... [KONF-I25]); held after the vote (Doubts)." So the finding's "before" is accurate.
- fix776_record.pl:27-28 ('s2c') split the list into halves and left the clause last, now tagged [KONF-P3] and placed right after the support-half clause.

WHY IT IS NOT REFUTED:
- Under either parse the citation over-claims. Read as the support half, the post says nothing on support. Read as the whole line, the 13 July line is "ONE-WAY, support-blocking", and the post backs only its cabinet half.
- The author's answer to finding 2 says "§2 and §4 name the half each restatement carries". [KONF-P3] is the only citation in that list left without its half.
- A party-own P tag beside the support half sits against the R5 premise in PolishDeclarationsDiagnostic.cs:30 and :134: "No party's own page carries a support half".
- The first pass's skeptic on finding 2 said "Doubt 5 ... needs no change" only about doubt 5 resting the support half on 2023-07-13 and 2023-08-24. The after-the-vote sentence was never examined, so this is not an answered finding.

WHY MINOR, NOT A DEFECT:
- The post is dated after polling day ("What counts", line 13), so it dates no DatedFact.
- The code's basis strings (DeclaredRedLines.cs:423 and :425) carry no after-the-vote claim, so nothing reaches a player.
- Every PolishDeclarationsDiagnostic check reads only the section 8 table and the register rows.
- It still misinforms Elias on whether Konfederacja's support half held. That bears on doubt 5 and on the K-1i (3)/R5 question in section 776's owed list, where R5 measurably adds Konfederacja's support to PiS+KO on the played count.
- The first pass graded the same class of error minor (findings 2 and 19).

**The skeptic's corrected fix.** Text only. No flag, date, tag, register row, digest or code changes; no check reads this prose.

1. Record section 2, lines 73-77: move the after-the-vote clause into the coalition half so it names its half. The sentence becomes: "**The coalition half restated** 2023-07-16 *(reported)* [KONF-I15], 2023-08-02 [KONF-P1], 2023-10-10 (... [KONF-I23] - its "nie dopuścić" read as an aim, §10) and 2023-10-11 *(reported)* (... [KONF-I25]), and held after the vote (doubt 5, [KONF-P3]); **the support half only 2023-08-24** ("Konfederacja nie zamierza ani przedłużać władzy PiS-u, ani ułatwiać Tuskowi powrotu do władzy" [KONF-I19])." The minimal alternative keeps the order: "...[KONF-I19]); its coalition half held after the vote (doubt 5, [KONF-P3])."

2. Doubt 5 (line 288): keep the sweep's words and append a bracket, following section 12's convention: "[record: [KONF-P3] carries the coalition half only ("nie wejdziemy w koalicję ani z PiS ani z PO"); its "Nie sprzedamy się" is not a support line in terms (§10's standard)]".

### 4. The 'as ruled' quotations of §621 are not verbatim, and COMPLETED §776 silently drops a whole clause

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:15

**The scenario.** The ruling at COMPLETED.md:34089 and CLAUDE.md:53 reads "...; a document is dated by the decision it records, not its file date; ...". The record's quotation, labelled 'as ruled', has "a document by the decision it records": it drops 'is dated' and 'not its file date'. COMPLETED.md:37096's 'as ruled' quotation goes further: "by the party's own record, never by press reporting; a declaration stands until a later dated one replaces it", the middle clause gone with no ellipsis. The dropped 'not its file date' is the clause the record's own [TD-P2] touches: its register row dates the 27 April agreement from "no dateline; XMP CreateDate", which is a file date. DeclaredRedLines.cs:268-270 already carries the ruling word for word.

**The fix proposed.** Quote COMPLETED.md:34089 verbatim in both places, or mark the elision.

**The skeptic's evidence.** I could not refute it. Every quoted line matches the files.

The ruling's three canonical texts agree word for word:
- COMPLETED.md:34089: "Declarations are dated by the party's own record, never by press reporting; a document is dated by the decision it records, not its file date; a declaration stands until a later dated one replaces it."
- CLAUDE.md:53 (unmodified by this item) has the same wording.
- DeclaredRedLines.cs:269-271 has the same wording, starting "Three rules - a declaration is dated by...".

A grep of the whole repo for "press reporting" and "a document by the decision" turns up no other source with a shortened form. "As ruled" can only mean those words.

**1. The record, ElectionsData/poland/coalition_declarations_2023.md:15-16 (untracked, new):**
"**Dates** - §621 as ruled: *"dated by the party's own record, never by press reporting; a document by the decision it records; a declaration stands until a later dated one replaces it."*"
- "is dated" and ", not its file date" are gone.
- There is no ellipsis. The record marks elisions with "…" elsewhere, for example line 80: "Z nikim!" … "Jasno i klarownie mówimy".

**2. COMPLETED.md:37096 (new working-tree text, git diff hunk +36820):**
"They are dated by §621 as ruled: *"by the party's own record, never by press reporting; a declaration stands until a later dated one replaces it"*."
- The whole middle rule is dropped, again with no ellipsis.
- A reader of §776 sees two of §621's three rules. The repo numbers these rules elsewhere ("§621's first rule" in K-1i; "§621's third rule" at record line 154).

**Where the abridgement came from.** The first pass asked for exactly this fix. Its finding 1 proposed "Quote §621 verbatim". The skeptic's corrected fix (Reviews/2026-10-04_s776_pl_declarations.md:58) said "word for word" but itself gave the shortened text, and the author copied it. Line 1113 of the review answers: "fixed as the corrected fix asks ... The record quotes §621 as ruled". That answer is wrong on this point, so raising it again is allowed.

**Why it stays a note, not more:**
- No behaviour reads these texts.
- In the record, the meaning survives: the dropped verb is gapped, and "by the decision it records" already excludes the file date.
- No Polish timeline fact (§8, facts 1-17) is a document dated by its decision. All of them are dated pages or posts, so the clause COMPLETED drops has no Polish application.
- The [TD-P2] point is weak. Register line 325, "no dateline; XMP CreateDate 2023-04-27T14:12:18+02:00", honestly reports the page's own date in the "page's own date" column. TD-P2 dates no fact: line 100 cites it only to say the agreement names no party.
- Small: DeclaredRedLines.cs:268 is the separator line; the rule is at 269-271.

So the finding is real as a misquotation, at the same grade as first-pass finding 8 (attribution wording, graded note).

**The skeptic's corrected fix.** Quote COMPLETED.md:34089 word for word in both places.

1. ElectionsData/poland/coalition_declarations_2023.md:15-16 should read: §621 as ruled: *"dated by the party's own record, never by press reporting; a document is dated by the decision it records, not its file date; a declaration stands until a later dated one replaces it."*
2. COMPLETED.md:37096 should read: They are dated by §621 as ruled: *"by the party's own record, never by press reporting; a document is dated by the decision it records, not its file date; a declaration stands until a later dated one replaces it"*. If the document clause is meant to be left out because no Polish fact is a document, mark the gap with "…" and say so.
3. Leave Reviews/...:58 alone, because it is the skeptic's report recorded verbatim. In the second-pass answer, note that line 1113's "as ruled" quotation was abridged and is now corrected.
4. The [TD-P2] register row needs no change: it reports the page's own date and dates no fact.

### 5. R5's premise and the stated extension conflict with the record's own reading of [NL-P17]

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:134

**The scenario.** New text says no party's own page carries a support half, so under §621 as written NL's support half is undated. It appears here, in the class summary (:30), in the record (:255) and in COMPLETED.md:37171. Unchanged record text says the opposite. In §4 (record :134-136) the record refuses "Lewica's support for a cabinet with Konfederacja in it ('nie liczcie na nas')", quoting NL's own page [NL-P17] of 2023-07-05. Fact 17's basis (DeclaredRedLines.cs:441) and doubt 8 cite the same words as part of the support-blocking basis. The stated extension also leaves out §652's condition (COMPLETED.md:34592: 'If a saved MP publication is earlier, use that instead'). On the record's §4 reading, that condition would date NL's support half from 07-05 (its own page), not from 08-27 (PAP). Elias is asked to rule the extension on a record that gives both readings. No measurement moves, because the derived NL–Konf line blocks support anyway. The first pass accepted the §4/§8 wording conflict as harmless; the new statements now take one side of it without saying so.

**The fix proposed.** Do one of two things. Either correct §4 and fact 17's basis to read 'nie liczcie na nas' as the cabinet refusal, which is the reading the first pass's refutation found on the page. Or state in R5's description and in the extension that §652's own-publication-first condition is not applied to [NL-P17], and why.

**The skeptic's evidence.** The conflict is in the text, and nothing reconciles it. A grep for "liczcie" across the code, the record, COMPLETED.md and the feature list finds no note explaining which reading applies.

NEW TEXT from the first-pass fixes (claims no own page carries a support half):
- PolishDeclarationsDiagnostic.cs:30, the class summary: "§621 as written - no party's own page carries a support half, so every declared line reads cabinet-only".
- PolishDeclarationsDiagnostic.cs:134, R5's comment: "No party's own page carries a support half - the support halves rest on broadcasters' and agencies' pages".
- Record :255: "R5 ... §621 as written, so every declared line is cabinet-only".
- COMPLETED.md:37171, in the list owed to Elias: "By §621 as written (R5), Konfederacja's and NL's support halves are undated, so every line on polling day is cabinet-only."

UNCHANGED TEXT (credits NL's own page with a support refusal):
- Record :134-136: "the record refuses all three: a shared cabinet; a Lewica cabinet resting on Konfederacja's negotiated votes; Lewica's support for a cabinet with Konfederacja in it ("nie liczcie na nas")".
- [NL-P17] is the party's own page. Its register row (:340) is lewica.org.pl, dated "Rzeczpospolita, 5 lipca 2023 r.".
- DeclaredRedLines.cs:441, fact 17's basis: "...support a cabinet that includes it... With 2023-07-05's \"nie liczcie na nas\" [NL-P17]."
- Doubt 8 (:291): "The basis: ... plus \"nie liczcie na nas\" (2023-07-05)".

Under the game's semantics (CoalitionFormation.cs:44-64), a refusal of "Lewica's support for a cabinet with Konfederacja in it" is a one-way NL→Konf support refusal (RefusesSupport(NL, Konf)). So on §4's reading, §621 as written does date a support half to 2023-07-05.

THE OTHER SIDE (the encoded reading, which R5 follows):
- §8 row 16 (:200) is cabinet-only from [NL-P17]. Row 17 (:202) is support-blocking, from [NL-I5], [NL-I6] and [NL-I7] only.
- Fact 16's basis (cs:439) puts "nie liczcie na nas" inside a cabinet-only line.
- The saved page is ambiguous: "na pewno Lewica nie wejdzie do żadnego rządu, w którym będzie Konfederacja. Mówimy naszym przyjaciołom ...: jeżeli taką opcję rozpatrujecie, to nie liczcie na nas." The first pass read it as being about joining.

So the first pass's conceded §4-vs-§8 conflict is still there. The new categorical sentences, which go to Elias with K-1i (3), take the §8 side without saying so.

§652'S CONDITION:
- The ruling (COMPLETED.md:34592) says "If a saved MP publication is earlier, use that instead."
- The extension as stated leaves this out: record :16-18, COMPLETED §776, and the PolandExtension and PolandTimeline docs.
- On the encoded reading the omission moves no Polish date. TD's own record [TD-P8] (2023-10-10) is later than 2023-05-15. No Konfederacja page of its own carries a support half. [NL-P17] is encoded cabinet-only.
- It matters only on §4's reading, where it would date NL→Konf's support half from 07-05.

WHY NOTHING MOVES:
- DerivedRedLines.From (CoalitionFormation.cs:952-959) builds `new RedLine(a, b, Derived, socialBreak, ...)`, a symmetric line. For NL–Konf socialBreak is true (n776c.log:561, "galtan gap 6.66 > 5.00").
- So RefusesSupport is already true in both directions. A one-way NL→Konf support line from 07-05 adds nothing, and R5's rows (n776c.log:568 and :579) cannot change.
- FormationSweepDiagnostic and the other Editor checks pin no Polish Basis text, so a text-only fix needs no re-pin.
- The severity stays at note: this concerns record honesty in what Elias rules on, not behaviour.

**The skeptic's corrected fix.** Text only. No flag, date or tag changes, no re-pin and no re-measure: no reading's row can move, because the derived NL–Konf line is symmetric and support-blocking.

Make the record use one reading: the encoded one, which §8, fact 16, the first pass's reading of the page and R5 all share.

1. Record :136. Stop crediting the third refusal to "nie liczcie na nas". Suggested wording: "Lewica's support for a cabinet with Konfederacja in it - carried by the symmetric shape the 27 August words need; 5 July's \"nie liczcie na nas\", read with its sentence (\"nie wejdzie do żadnego rządu, w którym będzie Konfederacja\"), is the cabinet refusal (fact 16)".
2. DeclaredRedLines.cs:441. Replace "With 2023-07-05's \"nie liczcie na nas\" [NL-P17]." with "It replaces 2023-07-05's cabinet line [NL-P17]." Keep the tag, so check (b) still sees it registered.
3. Doubt 8 (:291). Move "nie liczcie na nas" (2023-07-05) out of "The basis", or mark it as the cabinet line's words.
4. Add §652's condition wherever the extension is stated: record :16-18, COMPLETED §776's "rules applied", and the PolandExtension and PolandTimeline doc comments. Suggested wording: "unless the party's own record carries the same words earlier (§652: 'If a saved MP publication is earlier, use that instead'); on this record none does - TD's own record [TD-P8] is later, and [NL-P17] is read cabinet-only."

Alternative, not recommended because it touches §8, the array and possibly the sweep's timeline-edge samples: keep §4's reading. Then make fact 16 a one-way support-blocking line (true, true) from 2023-07-05, and change R5's premise (cs:30, :134), record :255 and COMPLETED.md:37171 to say that §621 as written does date NL's one-way support half (from [NL-P17]).

### 6. The pages registered for the doubts are not marked by the header's rules, and one dates doubt 3's alternative by a newspaper's page

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:286

**The scenario.** Line 21's rule marks *(reported)* any date a page gives only as its own date. Two newly registered pages are dated in doubts 2, 3 and 12 without the mark, and are missing from doubt 9's bracketed list. [TD-I13] (Krytyka Polityczna) gives only '30.06.2023' and no interview day. [TD-I14] (rp.pl) says 'powiedział na antenie Polsat News' and 'Źródło: Polsat News', with no day. TD-I14 is a newspaper's report of a broadcaster's interview, so it falls outside both §621 and the stated extension ('the broadcaster's or the agency's page'). Yet COMPLETED.md:37174 puts doubt 3's 2022-07-19 alternative to Elias, and that alternative's PSL leg needs this page (TD-I4 is dated 2022-08-09). Smaller points: (1) Doubt 2's [TD-P11] quote 'odsunąć PiS od władzy' is decoded from 'od&nbsp;władzy' without the *(decoded)* mark the first pass asked for; §4's 'w naszym imieniu' (line 139; bytes 'w&nbsp;naszym') is the same. (2) [NL-P30]'s register row (line 360) gives the publication stamp but not the page's dateline 'Gdańsk, 15 lipca 2023 r.', which the NL-P17, P25 and P29 rows do give. (3) §2 line 55 says the video is 'dated 22 June'; doubt 4's bracket says '23 June in Warsaw'.

**The fix proposed.** Mark TD-I13's and TD-I14's dates *(reported)* and add them to doubt 9's bracket. Note in doubt 3 and in COMPLETED's owed list that PSL's date in the 2022-07-19 alternative is a newspaper's report of Polsat News. Mark the decoded quotes. Give NL-P30's dateline. Reconcile 22 June with 23 June.

**The skeptic's evidence.** I could not refute the finding's main claims. One of its three small points is wrong. Every claim was checked against the saved bytes under raw/declarations_2023/.

1. The (reported) marks are missing for both new pages.
- The record's rule (line 21): "Where a page gives only its own date, that date is marked *(reported)*." The doubts carry no inline marks. Doubt 9 (line 292) is where their reported dates are listed, and its record bracket names only RMF24 2023-06-20, TVN24 2023-07-16, TVN24 2023-10-11 and Interia 2023-03-30.
- [TD-I13] (TD/krytykapolityczna_2023-06-30_kobosko-wywiad.html): datePublished 2023-06-30T06:00:41+02:00 and the dateline "Opublikowano 30 czerwca, 2023.", with no interview day. The body mentions Braun's "konwencji 24 czerwca", so the words fall between 24 and 30 June. Doubt 2 (line 285) dates them "2023-06-30 [TD-I13]" without a mark.
- [TD-I14] (TD/rp_2020-09-14_zadnej-koalicji-z-pis.html): "powiedział na antenie Polsat News lider ludowców", "Źródło: Polsat News", meta "source:polsat news", datePublished 2020-09-14T20:01:50. No day word and no PAP credit. Doubts 3 and 12 (lines 286 and 295) date it without a mark. Its register row (359) gives the publisher as plain "Rzeczpospolita", where KO-I8 says "Wprost (reporting Polsat News)" and NL-I2 says "wPolityce.pl (Polsat News)".
- The author's answer to finding 5, "applied evenly" (review line 1132), covers only the first pass's four dates. The two pages that fix 3 registered (review line 1130) were never added.

2. Doubt 3's 2022-07-19 alternative rests on a page outside both rules.
- COMPLETED.md:37174 says "or from 2022-07-19, when both members' own refusals stood".
- For PSL's refusal to stand on 2022-07-19, the record needs [TD-I14]. The record's §3 (lines 103-104) and fact 13's basis (DeclaredRedLines.cs:433) give PSL's refusal only from 2022-08-09 [TD-I4], Forsal's PAP interview ("we wtorek").
- [TD-I14] is not the party's own record (§621). It is also not "the broadcaster's or the agency's page" (record line 17; COMPLETED.md:37097).
- Polska 2050's leg, [TD-I3], is PAP ("Źródło PAP", "we wtorek w Radomiu"). The record reads PAP through a portal as within the extension (fact 13's [TD-I5]), so only PSL's leg falls outside, as the finding says.
- Under the rules the record states, the joint start would be 2022-08-09. Nothing in §776 says so (grep).

3. Two decoded quotes are not marked *(decoded)*.
- [TD-P11]'s bytes read "odsunąć PiS od&nbsp;władzy" (0 plain matches), and doubt 2 quotes it without the mark. The first pass asked for it: "Mark Hołownia's quote *(decoded)* if it is kept" (review line 148). The answer at line 1130 covers only the [KONF-I19] quote.
- [NL-P25]'s bytes read "w&nbsp;naszym imieniu" (0 plain matches), and it is unmarked at line 139 and again in doubt 8 (line 291).
- Every other unmarked quote I checked is plain text in its bytes: TD-P10, TD-P6 (two quotes), PIS-P1, PIS-P2, KONF-P1, KONF-I19, NL-P30 and TD-I13. So these two are the only misses.

4. [NL-P30]'s dateline: real but cosmetic. Its bytes carry "Gdańsk, 15 lipca 2023&nbsp;r.". Row 360 gives only the publication stamp, while the P17, P25 and P29 rows give their datelines. Doubt 8 dates nothing by this page ("an MP in July").

5. REFUTED: the 22 June / 23 June point. [KONF-I8]'s video uploadDate is "2023-06-22T22:22:07+00:00": 22 June in UTC, 23 June at 00:22 in Warsaw. Line 55 and doubt 4's bracket give the same timestamp two ways, so they do not contradict.

Impact on the game: none.
- No code reads the marks. A grep for "(reported)" or "(decoded)" in Assets finds only a variable name in MojibakeCheck.
- TD has 0 seats in the 2019 chamber (PartySystem.cs:764), so a TD → PiS line starting before 2023-05-15 affects no formation. The line stands on 2023-10-15 whichever start is chosen.
- This is record honesty about doubts and an item put to Elias, so note is the right grade.

Aside, outside this finding: fix 1's quotation of §621 is not word for word. COMPLETED.md:37096 drops "a document is dated by the decision it records, not its file date" without an ellipsis, and the record's line 15 shortens that clause. The ruling's own text is at COMPLETED.md:34089.

**The skeptic's corrected fix.** Text only: no code change and no sweep re-pin. Add no "|" to any register row, because check (c) (PolishDeclarationsDiagnostic.cs:85-89) splits each row on "|".

1. Doubt 9's record bracket: add "Krytyka Polityczna 2023-06-30 [TD-I13] (the interview falls after the 24 June convention it mentions); Rzeczpospolita 2020-09-14 [TD-I14] (words on Polsat News, no day given)".

2. Add a record bracket to doubt 3, and the same note to COMPLETED.md:37174.
   - The note: PSL's leg of the 2022-07-19 alternative rests on [TD-I14], Rzeczpospolita's report of Kosiniak-Kamysz on Polsat News. That page is neither the party's own record (§621) nor a broadcaster's or agency's page (the extension).
   - Under the stated rules, PSL's first dated refusal is 2022-08-09 [TD-I4], as §3 gives it, so the joint start those rules support is 2022-08-09. 2022-07-19 would need a further extension.
   - In TD-I14's register row, write the publisher as "Rzeczpospolita (reporting Polsat News)".

3. Mark *(decoded)* on doubt 2's "odsunąć PiS od władzy" [TD-P11], and on "w naszym imieniu" [NL-P25] both in §4 (line 139) and in doubt 8.

4. In NL-P30's register row, add its dateline "Gdańsk, 15 lipca 2023 r." beside the publication stamp.

5. Drop the 22/23 June item, since the two statements agree. Optionally, write "dated 22 June (UTC)" at line 55.

### 7. Doubts list: a refuted claim is left unbracketed, and some edits are unbracketed although the header says corrections are bracketed

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:285

**The scenario.** (1) Doubt 2 still says "The model can therefore seat a PiS cabinet on TD's support (194 + 65 = 259)". The fix removed this claim from §10 only. In formation_sweep.txt's 'Poland | seated | negative False' block the viable cabinets are 6, 3, 14, 2, 10 and 12, with no PiS-alone cabinet. On the played count, R1 seats PiS+KO on TD's support. (2) Doubt 2's 'Measure both readings ... before Elias rules' has no '[record: measured - R2]', although doubt 1 got one. (3) Line 282 says the synthesis's text is kept, with the record's corrections in square brackets. But doubt 6's 'a list candidate's (Wipler)' became 'Wipler's', and doubt 5's file path was removed, with no brackets; scratchpad decl_gaps_and_doubts.json holds the originals. (4) Doubt 4's bracket calls 26 June 'PAP's print date', but PAP's page says 'w wywiadzie dla poniedziałkowego SE', so it is Super Express's print date.

**The fix proposed.** Bracket doubt 2's 259 claim (no PiS-alone cabinet is viable on the record's count; R1 on the played count seats PiS+KO on TD) and its measuring request. Bracket the doubt 5 and doubt 6 edits, or reword the header. Write 'Super Express's print date, as PAP gives it'.

**The skeptic's evidence.** I checked all four parts against the record, the synthesis's original text, the sweep, n776c and the formation code. Each one holds. None of them touches code, the timeline, digests or any formation result.

(1) Doubt 2's 259 claim (record :285). The sentence is still unbracketed: "The model can therefore seat a PiS cabinet on TD's support (194 + 65 = 259)."
- The model can never form PiS alone on TD's support. `CoalitionFormation.SupportersOf` (CoalitionFormation.cs:788-797) drops a would-be supporter when `toCabinet < bestOutside`. For a PiS-alone cabinet, TD's best party left outside is KO, so PiS alone gets no supporter. Red lines only remove supporters, so this holds under every reading.
- The sweep confirms it. Under the negative rule: "viable cab 1 sup 0 ... seats 194" (formation_sweep.txt:94034). Under the positive rule (:94049-94056), the viable cabinets are 6, 3, 14, 2, 10 and 12. Cabinet 1 is neither viable nor blocked.
- The first pass's finding 6 skeptic already called this "(259)" stale in §10. The fix deleted it there only; grep finds "259" only at :285.
- The broader words "a PiS cabinet on TD's support" do hold for PiS+KO:
  - chamber of record, R1's best three: "KO+TD on NL 248 | PiS+KO on TD 416 | KO+TD+NL 248";
  - played count, R1: "PiS+KO on TD's support (323 in cabinet, 401 supported)".
- So what is false is the arithmetic example (PiS alone), not the whole sentence.

(2) Doubt 2 has no measured bracket. It ends "Measure both readings as a diagnostic row before Elias rules." with no bracket. Doubt 1 (:284) got "[record: measured above - on the played count this doubt decides the government]". The answer does exist: §12 (:252, :273) and COMPLETED §776 ("Measured: on the played count it decides only TD's support of PiS+KO"). Only the pointer is missing.

(3) Unbracketed edits under the new header. The header is new in this fix (:282): "as its synthesis wrote it, the record's corrections and notes in square brackets". I word-diffed scratchpad decl_gaps_and_doubts.json against :284-298:
- doubt 6: "a list candidate's (Wipler)," became "Wipler's,";
- doubt 5: "(Konf/x_bosak_1713834658134138996_syndication.json)." became "[KONF-P3]." and the removed path is not marked.
- Neither carries a [record: ...] mark. Both are the first pass's own corrected fixes (findings 8 and 3), so the edits are right; the header's description of them is what is wrong.
- Tag insertions ([TD-P11], [TD-I13], [TD-I14], [NL-P30]) are square-bracketed by form, but cannot be told apart from the synthesis's own tags.

(4) Doubt 4's bracket (:287) says "PAP's print date of 26 June". The PAP page (CROSS/bankier-pap_2023-06-26_mentzen-stawia-warunek.html) says: "Mentzen w wywiadzie dla poniedziałkowego \"SE\"". So 26 June is Super Express's Monday print date, as PAP gives it. The bracket's other dates match the SE page: uploadDate 2023-06-22T22:22:07+00:00, createdDate 2023-06-24T22:58+02:00, datePublished 2023-06-26T09:05:05+02:00. The bound itself holds.

Why only a note:
- The list is presented as a quotation.
- §12's table and bullets directly above it, and COMPLETED §776, give Elias the measured truth.

Aside: §2 (:55) dates the video "22 June", while doubt 4's bracket says "23 June in Warsaw". The upload was 22:22Z on 22 June, so both are true, in different time zones.

**The skeptic's corrected fix.** 1. Doubt 2 (record :285): after the 259 sentence, add "[record: not formable on either count - `CoalitionFormation.SupportersOf` gives a PiS-alone cabinet no supporter, because TD's best party left outside is KO; what the model seats with TD's support is PiS+KO (R1, played count)]". After "Measure both readings as a diagnostic row before Elias rules." add "[record: measured above as R2 - on the played count this doubt decides only TD's support of PiS+KO]". Give references, not figures.

2. Doubt 4's bracket (:287): replace "PAP's print date of 26 June" with "Super Express's Monday print date, 26 June, as PAP gives it [KONF-I10]".

3. Line 282, either of two:
   - Bracket the in-place edits:
     - doubt 6: "[record: Wipler's - 'a list candidate's' is not on [KONF-P1]]";
     - doubt 5: "[record: [KONF-P3], the synthesis's out-of-tree path now held in tree]".
   - Or reword the header: "... the record's notes in square brackets; register tags added and doubt 6's attribution narrowed in place (§776's review, findings 3 and 8)".

None of these needs a re-run or a re-pin.

### 8. The K-1i line puts a measured outcome into a live document as a present-tense claim

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** POLISIM_FEATURE_LIST.md:146

**The scenario.** "on the played 2023 count it adds Konfederacja's support to the PiS+KO the recorded reading seats" is an undated, present-tense result of R5 in a root document. The claim convention binds that document; only COMPLETED.md is exempt. If the formation's weights, the vote model or seed 777's path changes, the sentence stays and becomes false while the diagnostic prints something else, and no check reads it. 'nine facts' is likewise a count of PolandExtension marks. The heading still reads 'SD's reading, and two dates', although (2) is answered and (3) now holds ten cases.

**The fix proposed.** Reference the result instead of stating it: 'measured by PolishDeclarationsDiagnostic's R5 (COMPLETED.md §776)'. Drop the count or point to it. Retitle the heading.

**The skeptic's evidence.** The finding is real in part. The R5 sentence and the count are real but minor. The heading part does not hold up.

THE R5 SENTENCE IS A MEASURED OUTCOME STATED IN THE PRESENT TENSE
- POLISIM_FEATURE_LIST.md:146 (working tree): "The reading by §621 as written is measured in `PolishDeclarationsDiagnostic` (R5): on the played 2023 count it adds Konfederacja's support to the PiS+KO the recorded reading seats."
- The feature list binds itself to the convention (its own head: "The claim convention governs every document"). CLAUDE.md says "Nobody transcribes, anywhere, in any file". COMPLETED §190 §A.5 rule 2 forbids "a measured figure presented as current" and "a count of code things", and says to "name the command that produces it instead of its result". The sentence names the command and also states its result.
- It is true today. n776c.log:575 has R1 on the played count = "PiS+KO on TD's support", and :579 has R5 = "PiS+KO on TD+Konf's support".

THE FAILING PATH (the outcome depends on the count, and nothing holds the count or the outcome)
- On the chamber of record, R1 and R5 both form "KO+TD on NL's support" (n776c.log:564 and :568). So R5 adds nothing there.
- The played count comes from seed 777's whole simulation path: `NationalElection.TryPredictShares` with `EconomicVote.RecordOverTerm` (PolishDeclarationsDiagnostic.cs:228-232).
- PollingDayDiagnostic.cs:186-187 checks only that the count used the district method and seated 460. The seats themselves are not pinned.
- In PolishDeclarationsDiagnostic, the played count is "Measured, not asserted" (:208-211). Its only check (:252) is that the game's entry forms R1's cabinet. Nothing asserts what R1 or R5 forms.
- DocumentClaimCheck.cs:64 only checks that backticked `Type.Member` names exist.
- So a change to the macro or vote model can move the count, and the sentence would go false with every bar still green.

THE SAME MEASUREMENT IS DATED EVERYWHERE ELSE
- The fixes themselves put this measurement in dated form elsewhere:
  - the Polish record's §10 (line 221: "Measured 2026-10-04 (§776)");
  - its §12 table (line 248);
  - COMPLETED §776, which is exempt ("Measured on the played count, that adds Konfederacja's support to PiS+KO").
- The first pass's finding-6 skeptic asked for exactly this: "a dated measurement, not in the present tense". Only the feature-list copy is undated.

"NINE FACTS" IS A COUNT OF CODE THINGS
- It is true today: nine `DatedFact`s carry `PolandExtension` (DeclaredRedLines.cs, the 411-441 block).
- A code change would falsify it without the tracked work moving, for example extending Konfederacja's "z nikim" rule to another roster key.
- It adds nothing. The list that follows it and the pointer to `DeclaredRedLines.PolandExtension` already identify the facts.

WHY ONLY A NOTE
- K-1i's own (1), on the same line, keeps a present-tense formation outcome with a pointer to "`Formation2026Diagnostic`'s row". It was written in §639 (commit 2c32377) and accepted after the s639 review's item 12, which removed only the figures from that row. The new sentence follows that form and carries no figures.
- The item is open and gets rewritten when Elias rules.
- The first pass graded analogous transcriptions as a note.

REFUTED: THE HEADING
- "K-1i — SD's reading, and two dates" is the item's name as filed. K-1f, K-1g and K-1h (lines 143-145) keep their filed titles after being ruled.
- TRACKING text that is incomplete because the work moved is exactly what the convention's test allows ("only incomplete, and only where TRACKING has genuinely moved").
- No retitle is owed.

SIDE NOTES
- The finding says "only COMPLETED.md is exempt". The long form and DocumentClaimCheck's Historical set also exempt CLAUDE.md. This does not affect the verdict.
- "No check reads it" is by design: the long form forbids writing a new check for this. That is the reason the claim must not be written, not a separate defect.

**The skeptic's corrected fix.** Change only the sentence and the count in POLISIM_FEATURE_LIST.md:146. Leave the heading as filed.

1. Replace "The reading by §621 as written is measured in `PolishDeclarationsDiagnostic` (R5): on the played 2023 count it adds Konfederacja's support to the PiS+KO the recorded reading seats." with either form below.
   - A reference: "The reading by §621 as written is R5 in `PolishDeclarationsDiagnostic`; what it seats on the played count, against the recorded reading, is that diagnostic's MEASURED lines (`COMPLETED.md` §776's table)."
   - The dated form the fixes already use in the Polish record's §10 and §12: "Measured 2026-10-04 (§776, `PolishDeclarationsDiagnostic` R5): on the played 2023 count it added Konfederacja's support to the PiS+KO the recorded reading seated."

2. Drop the count: "nine facts of Poland's 2023 timeline are dated by..." becomes "Poland's 2023 facts dated by...". The list that follows and the `DeclaredRedLines.PolandExtension` pointer already carry them.

No code change is owed.

### 9. Two provenance statements still claim slightly too much

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/raw/declarations_2023/fetch_log.md:14

**The scenario.** (1) 'Where a folder has no URL row for a page, the URL is the page's own (its canonical link, as the register gives it)' fails for the out-of-tree TD/pl2050_2023-04-27_umowa-skan.pdf. TD has no URL list, the file has no register row, and its bytes carry no URL (no canonical link, no http string), so where it came from cannot be recovered. (2) The record's header (line 4) and COMPLETED §776 ('every source the facts and the record's sections cite by tag ... held under raw/declarations_2023') also cover tags that are not held there: §7's [TK-KONST] and §9's [ELI-MP-2023] and [API-V1] (records_by_date.md's register), and, under COMPLETED's wider wording, §12's '[KONF-I21]', which is out of tree only.

**The fix proposed.** Say the PDF has no recorded URL, or record one. Narrow the header to 'every source this register lists', and say that §§7 and 9 cite records_by_date.md's register.

**The skeptic's evidence.** Both halves hold.

PART 1 - fetch_log.md:14: "Where a folder has no URL row for a page, the URL is the page's own (its canonical link, as the register gives it)." The sentence sits in the paragraph about the whole out-of-tree sweep. It fails for PoliSim-captures/sources/poland_declarations_2023/TD/pl2050_2023-04-27_umowa-skan.pdf (4220914 bytes, line 26 of TD/SHA256SUMS.txt):
- TD has only SHA256SUMS.txt and no URL list (fetch_log line 10 says so too).
- No register row: none of the record's 57 rows names it, and grep finds "umowa-skan" in no repo file.
- Its bytes carry no URL:
  - `grep -a -c http` gives 0;
  - the /Info dictionary is only /Title (Polska 2050), /Producer (iOS Version 16.0.3 ... Quartz PDFContext), /Creator (Notes) and the dates; there is no XMP;
  - the 9 Flate streams, inflated, are draw operators ("q 507 0 0 728 53 32 cm /Im1 Do Q") plus one ICC profile, and the 8 pages are DCT images.
- No saved TD page links to it.

It is the only file the rule misses:
- every other out-of-tree file in CROSS/TD/PiS has rel=canonical/og:url (HTML) or a JSON link (pis_wpjson);
- every Konf/NL file has a SOURCES.tsv/urls.tsv row;
- psl_2023-04-27_umowa-pl2050-psl.pdf also has no URL in its bytes, but the register gives it as [TD-P2].

One correction to the finding: "cannot be recovered" holds for the repo and the captures folder only. The sweep's own transcripts (this session's subagent workflow journals, wf_b19b0daf) record https://polska2050.pl/assets/uploads/2023/04/4df0e7ac-10fe-717b-fc12-b7d6a6b5c91f.pdf, "linked from Polska 2050's 27 April release". A draft register gave it a "TD-P2 (scan)" row, which the final register dropped.

PART 2 - coalition_declarations_2023.md:4-5: "Every source the facts and §§1–11 cite by tag, and every page §12's doubts lean on, is stored byte for byte under `raw/declarations_2023/<declarer>/` (digests in the register below ...)". A diff of the body's tags against the register (57 rows) leaves exactly four unregistered tags:
- [TK-KONST] at :182 (§7, "`records_by_date.md` §4 [TK-KONST]");
- [ELI-MP-2023] and [API-V1] at :208 (§9, "`records_by_date.md` §2 [ELI-MP-2023] [API-V1]");
- [KONF-I21] at :292 (doubt 9).

The first three are records_by_date.md's register rows (records_by_date.md:351, :362, :367). Their files are raw/records/trybunal_konstytucja.html, api_sejm_term10_votings_sitting1.json and eli_search_MP_2023_postanowienia_RM.json, and all three match raw/records/SHA256SUMS.txt (re-hashed). So the claim is wrong about where they are, not about whether they are held. The inline citations name records_by_date.md, which softens it.

COMPLETED.md §776 says the same twice:
- "Every source the facts and the record's sections cite by tag ... is held byte for byte under `ElectionsData/poland/raw/declarations_2023/<declarer>/`";
- "a register of every cited source with its URL, publisher, the page's own date, file and SHA-256".

[KONF-I21] is the weakest of the four: doubt 9 itself says "(the draft's [KONF-I21]), which the record no longer cites: §11, out of tree only".

Not a re-raise: the first pass (Reviews/2026-10-04_s776_pl_declarations.md) never mentions these tags or the scan. Both statements are text the fixes wrote or narrowed (findings 3 and 4).

Severity: note. No check, wired fact or formation reads either statement. The three tags are held in tree under raw/records, and the scan is cited nowhere. They are still false provenance claims in a SOURCED record.

**The skeptic's corrected fix.** 1. fetch_log.md:14. Replace the sentence with what holds: "Where a folder has no URL row for a page, the page's saved bytes carry its canonical link (rel=canonical / og:url), or its `link` field for pis.org.pl's REST records, and the register gives the URL of every page it lists." Then name the one exception. Either record the scan's URL, or say it has no recorded URL. The URL is known from the sweep's transcript: "TD/pl2050_2023-04-27_umowa-skan.pdf (Polska 2050's image-only scan of the 27 April agreement, uncited; [TD-P2] is the cited copy): https://polska2050.pl/assets/uploads/2023/04/4df0e7ac-10fe-717b-fc12-b7d6a6b5c91f.pdf, as the sweep fetched it". Check the URL before writing it in, or say "no URL is recorded for it".
2. coalition_declarations_2023.md:4-5. Narrow the claim to "Every source this record's register lists - the sources the facts and §§1–11 cite by its tags, and the pages §12's doubts lean on - is stored byte for byte under `raw/declarations_2023/<declarer>/`". Add: "§7's [TK-KONST] and §9's [ELI-MP-2023] and [API-V1] are `records_by_date.md`'s register, held under `raw/records/`".
3. COMPLETED.md §776. Make the same narrowing in two bullets:
   - "The pages, in tree": every source the register lists; the three records_by_date tags are held under raw/records;
   - "a register of every cited source": "of every source it cites by its own tags".
   [KONF-I21] needs no change: doubt 9 already marks it as no longer cited and out of tree only.

### 10. The rewritten class summary says each timeline's summary names its record file; Sweden's does not, and it says every Swedish date is the party's own record

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Elections/DeclaredRedLines.cs:15

**The scenario.** Line 15-16 is new: 'each timeline's own summary names its record file'. SwedenTimeline's summary (line 311) names no file. It also says 'every date the party's own record's (the rules of §621)'. The same timeline's MP date, though, is Helldén's words to Sveriges Radio (§652, comment :345), and KD → S is Busch's spoken words in SVT's live report, marked 'an EXTENSION of §621's precedent' (:349). Those are exactly the two cases the new PolandTimeline doc (:397-399) and the Polish record cite as the extension's precedents. So DeclaredRedLines states both 'every Swedish date is own-record' and 'two Swedish dates are the extension's precedents'. SwedenTimeline's summary predates this change; the new sentence leans on it.

**The fix proposed.** Write 'Germany's and Poland's timeline summaries name their record files; Sweden's are named above'. Correct SwedenTimeline's summary: dated by §621's rules, MP's date by §652's ruling, and KD → S by the extension put to Elias (K-1i (3)).

**The skeptic's evidence.** I could not refute either half from the code. Both are comment text only, with no runtime effect.

**Half 1: the new pointer is wrong for Sweden.**
- DeclaredRedLines.cs:15-16 is uncommitted §776 text (git blame: "Not Committed Yet"). It reads: "dated where `DeclaredRedLines.HasTimeline` does - each timeline's own summary names its record file (Germany's since §705, Poland's since §776; ...)".
- `HasTimeline` (:449) returns true for Sweden, so "each timeline" includes `SwedenTimeline`.
- `SwedenTimeline`'s only doc comment (:311, commit 87630c09, 2026-09-24) is: "Sweden's declarations as dated facts - every date the party's own record's (the rules of §621), each fact standing until the dated one that replaced it." It names no file.
- The `//` block above it (:268-281) names tags but no .md path either.
- Germany's summary (:353-357) and Poland's (:399) do name their files.
- The wording is the first-pass skeptic's own suggestion (review line 917), taken verbatim. It is still a claim about the code that is false for one of the three timelines.

**Half 2: Sweden's summary contradicts its own facts, now sharpened by §776's text.**
- :311 says every Swedish date is the party's own record under §621's rules.
- In the same array:
  - :345-346 dates MP by "Helldén's own words to Sveriges Radio ... (ruled 2026-09-29, §652)".
  - :348-349 dates KD → S by "spoken words, not a party publication, so an EXTENSION of §621's precedent ... put to Elias (§639)".
- The new §776 text classifies exactly that dating as outside §621:
  - :389 (`PolandExtension`'s summary): "dated by the EXTENSION, not by §621 as written - a leader's or spokesperson's spoken words ... (§652's precedent, ruled for MP)".
  - :391 (the constant): "the words as a broadcaster's or an agency's page dates them, not the party's own record".
  - :397-399 (the `PolandTimeline` doc) says the same.
- The Polish record (coalition_declarations_2023.md:16-18) cites §652's MP as the precedent and §639's KD line as "the same question, still open".
- The feature list's K-1g line (POLISIM_FEATURE_LIST.md:144) already says "two dates stretch §621's rules - K-1i".

**Why only a note.**
- The class summary names both Swedish files in the very next sentence (:16-19), and every Swedish basis ends with `SwedenSource` or `SwedenSource2022`. A reader still finds the files.
- The parenthetical lists only Germany and Poland.
- :311's dating claim is older than §776: it has been false for KD → S since §639 (2c323775). §776 only added the new text that contradicts it.
- The per-fact comments already state each true basis.

**One overstatement in the finding:** the `PolandTimeline` doc cites only §652 (MP). It is the record that also names §639's KD line, and as an open question rather than a precedent.

This was not raised or answered in the first pass: the review's mentions of `SwedenTimeline` (lines 701-718) are about `DeclarationDatesDiagnostic`'s Sweden-only loop.

**The skeptic's corrected fix.** Comment-only. Either edit makes the sentence at :15-16 true; doing both also removes the contradiction inside the file.

(1) At DeclaredRedLines.cs:15-16, narrow the pointer to what holds. Replace "each timeline's own summary names its record file (Germany's since §705, Poland's since §776; the lines a record does not carry stay derived)" with "Germany's and Poland's timeline summaries name their record files (`GermanySource`, `PolandSource`; since §705 and §776; the lines a record does not carry stay derived), and Sweden's two are named below".

(2) At :311, correct `SwedenTimeline`'s summary by reference rather than by a list of facts, per the claim convention: "Sweden's declarations as dated facts (records: `SwedenSource2022`, `SwedenSource`) - dated by §621's rules, or, where a fact's comment says so, by §652's ruling or by the extension K-1i (3) puts to Elias; each fact standing until the dated one that replaced it."

Do not write "MP and KD → S" into the summary as a list: that is a transcribed count which goes stale when a fact is added.

### 11. (h) checks only the game's cabinet against R1, and reads it from a display string

- **Lens:** diagnostic - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:248

**The scenario.** On the played count, R1, R2 and R5 all seat the same cabinet, PiS+KO. They differ only in support: on TD's support, no support, and on TD's and Konf's support (n776c). `GovernmentFormation.Formed` carries no support mask, and lines 248-252 only compare `CabinetDescription`, split on '+', with R1's cabinet. Suppose the game's formation path came to read every declared line cabinet-only (R5, the K-1i (3) alternative). The game would then store PiS+KO on TD+Konf's support, and (h) would still print 'ok the game's own entry forms the played count as R1 does'. TD's support (doubt 2) and Konf's support (K-1i (3)) are exactly what two of the owed rulings move.

The parse can also fail when nothing changed. `CabinetDescription` joins `ShortName` (GovernmentFormation.cs:107-111, whose comment says the description is 'READ BY THE PLAYER ... and by no lookup'), while `r1Cabinet` holds `Abbrev` keys. If a Polish party got a published short name that differs from its key (§575, as Germany's GRÜNE for Grune), (h) fails on identical formations.

The ' led by ' cut at line 249 never fires: that text comes from the ROLE log line (GameController.cs:6430), not from the description.

The claims built on (h) say more than it checks. The record's §12 (line 267) and COMPLETED.md:37132 say the entry 'forms that count as R1 does'. The class summary (line 33, 'forms what it forms') and the (h) comment (line 211, 'asserted only that the game's entry forms what R1 forms') describe the assertion two different ways. It cannot pass vacuously: HasGovernment is required, and an empty description yields {""}.

**The fix proposed.** Compare `GovernmentFormation.ViewOf(player, reading)` with R1. It is the view GameController.cs:6403/6429 stores via `GovernmentRecord.FromView`. Hold its Cabinet and Support key lists to `r1Played.Government.Cabinet` and `.Support`, and drop the description parse and the ' led by ' cut. Otherwise, reword §12, COMPLETED and the class summary to 'R1's cabinet'.

**The skeptic's evidence.** CONFIRMED (code facts):
- PolishDeclarationsDiagnostic.cs:248-252 compares only cabinets. It builds `named` from `gameFormed.CabinetDescription.Split('+')`, builds `r1Cabinet` from `r1Played.Government.Cabinet`, and runs `Check(gameFormed.HasGovernment && named.SetEquals(r1Cabinet), "...forms the played count as R1 does...")`. Support is never compared, and it cannot be: `Formed` (GovernmentFormation.cs:34-62) carries no support mask. Its `PlayerSupports` covers only the player's party (PiS), which sits in the cabinet.
- n776c.log:575/576/579: on the played count, R1 = "PiS+KO on TD's support", R2 = "PiS+KO" and R5 = "PiS+KO on TD+Konf's support", so all three seat the same cabinet. Line 580 prints "ok the game's own entry forms the played count as R1 does: PiS+KO (R1: PiS+KO on TD's support)". The game-side string is exactly R2's printed result. The evidence cannot tell R1 from R2 or R5, which are the readings that doubt 2 and K-1i (3) move between.
- The support half matters in play:
  - The night stores `GovernmentRecord.FromView(ViewOf(player, electionReading))` (GameController.cs:6403, 6429). For Poland, `RulesOf` is `Unsourced` (ConfidenceProcedure.cs:34), so `RoundsApply` is false and line 6429 runs.
  - `FromView` copies `view.Support` into `record.Support` (GovernmentRecord.cs:297), and every vote reads support from that record (`TryGovernment`, GovernmentFormation.cs:137-148).
  - `Form` only feeds the verdict sentence, yet §12:267 names `GovernmentFormation.Form` as "the game's own entry".
- The " led by " cut at :249 is dead code. `CabinetDescription` is `string.Join("+", ShortName...)` (GovernmentFormation.cs:104-111), and " led by " belongs only to the ROLE log line (GameController.cs:6430).
- The claims say more than the check verifies:
  - The record's §12:266-267 and COMPLETED.md:37132 say the game's entry "forms that count as R1 does".
  - The class summary :33 ("forms what it forms") and the (h) comment :211 ("asserted only that the game's entry forms what R1 forms") disagree with each other.
  - COMPLETED.md:37153 and the review's line 1120 say "held to R1's cabinet", which is accurate.

NARROWED (why minor, not defect):
- Today the claim is true, support included. The game's path (TryFormSeats, GovernmentFormation.cs:416-427) and R1Played run the same `CoalitionFormation.Form` on identical inputs:
  - seats: `SetSeatsFromElection` copies them by Abbrev (ParliamentSystem.cs:86-95), which equals `played`;
  - lines: `ForDateSourced` (DeclaredRedLines.cs:477-492) builds the derived lines and then the standing facts in timeline order, the same content and order as `WithDeclarations(false)`;
  - compatibility, rule and joint masks come from the same calls;
  - platforms are empty on both sides, held by (f).
- The finding's R5 example slips past only if the change is local to GovernmentFormation's path, where `extraLines` and platforms are already threaded. A change in the reader or the timeline data is caught by (f) (its key at :183 includes BlocksSupport and OneWay) or by (a).
- On a GovernmentFormation-local change, nothing else catches it:
  - (g) and the sweep (FormationSweepDiagnostic.cs:35) call `CoalitionFormation.Form` directly;
  - on the chamber of record, R2 and R5 equal R1 (n776c:565, 568);
  - (h) is the only Polish check that exercises the path, and it is cabinet-only.
- The ShortName parse is latent and fails loudly. No Polish row passes `shortName` (PartySystem.cs:448-459; `JointList` :439), so ShortName equals Abbrev for every Polish party. A future difference would be a false FAIL, never a silent pass.
- The check cannot pass vacuously, as the finding itself grants.

**The skeptic's corrected fix.** In (h), hold `GovernmentFormation.ViewOf(player, DeclarationReading.OfElection(CountryId.Poland, PollingDay))` to R1. That is the view the night stores (GameController.cs:6403, 6429).
- Require `view.HasGovernment`.
- Require the set of `view.Cabinet` abbrevs to equal R1's cabinet mask's abbrevs.
- Require the set of `view.Support` abbrevs to equal R1's support mask's abbrevs.
- Print the game's support beside R1's, e.g. "PiS+KO on TD's support (R1: PiS+KO on TD's support)".
- Drop the CabinetDescription parse and the dead " led by " cut. The comparison is safe from line-order flakiness: the reader builds lines in the same order as `WithDeclarations(false)` (DeclaredRedLines.cs:477-492).
- Keep `Form` only for the verdict path (HasGovernment, PlayerInCabinet).

Then align the prose:
- Name `ViewOf`, the stored government, as the game's entry in the record's §12:267 and in COMPLETED.md:37132.
- Make the class summary :33 say what :211 asserts.

If the check stays cabinet-only instead, reword the printed line, §12:267, COMPLETED.md:37132 and :33 to "R1's cabinet", as COMPLETED.md:37153 and the review's line 1120 already say. Either way, delete the " led by " cut.

### 12. R5's premise that no party's own page carries a support half conflicts with the record's own §4 for NL; R5 ignores the extension marks

- **Lens:** diagnostic - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:134

**The scenario.** The record's §4 (coalition_declarations_2023.md:135-136) cites [NL-P17], the party's own page of 2023-07-05, for Lewica's refusal to support a cabinet with Konfederacja in it. The words are "nie liczcie na nas". Fact 17's basis and doubt 8 (line 291) cite the same words inside the support half. They are words about Lewica's own conduct, which §10's standard counts as a line.

So by §621 as written, NL → Konf carries at least NL's direction of the support refusal from 2023-07-05. Yet R5 reads it cabinet-only. The diagnostic's summary (line 30) and comment (lines 134-135) say no party's own page carries a support half. COMPLETED.md:37171 tells Elias that 'Konfederacja's and NL's support halves are undated'.

The measured R5 government does not move, but only because the derived NL–Konf line is already symmetric and support-blocking (n776c: 'the derived line NL-Konf: DERIVED: CHES galtan gap 6.66 > 5.00').

R5 is also hard-coded, not read from the marks: line 127 sets `blocks = !cabinetOnly && ...` for every fact. A later support half dated by a party's own page would be read cabinet-only under the label 'the party's own record alone', and no check would notice.

The comment's list of what else that reading moves ('the June facts, TD's first date') is incomplete. It omits that facts 7 and 16 would stand open, and that Konf → KO would start on KONF-P1's 2023-08-02. None of these changes a polling-day line.

Otherwise R5 checks out: the standing support-blocking facts (8, 9, 17) are exactly the PolandExtension-marked support halves.

**The fix proposed.** Build R5 from the marks: drop BlocksSupport only where the basis carries `DeclaredRedLines.PolandExtension`. Then state NL's case one way or the other, in the comment, §12 and the owed list. Either R5 reads [NL-P17] as NL → Konf one-way and support-blocking from 2023-07-05, or the record says it does not read the words as a support half and reconciles §4, fact 17 and doubt 8. Complete the comment's list of what moves.

**The skeptic's evidence.** I could not refute it. The contradiction is real, but no number changes.

1. What R5 claims. PolishDeclarationsDiagnostic.cs:30-31 and :134-136 say "no party's own page carries a support half ... so on polling day every declared line reads cabinet-only". Line :127 (`bool blocks = !cabinetOnly && (f.BlocksSupport || ...)`) strips support from every standing line and reads no mark. Elias is told the same thing in record §12 :255, COMPLETED.md:37121 and :37171 ("By §621 as written (R5), Konfederacja's and NL's support halves are undated"), and review :1118.

2. What the record says. coalition_declarations_2023.md:134-137 reads: "the record refuses all three: ... Lewica's support for a cabinet with Konfederacja in it ("nie liczcie na nas"). That is the CDU's shape ..., not K-1's one-way one, which would let Konfederacja carry a cabinet Lewica sits in — the arrangement 27 August names." The words are [NL-P17], "on the party's own page" (:127; register :340, lewica.org.pl). Fact 17's support-blocking basis cites them too (DeclaredRedLines.cs:441, "With 2023-07-05's \"nie liczcie na nas\" [NL-P17]"), and so does doubt 8 (:291, "plus \"nie liczcie na nas\" (2023-07-05)").

3. Why the two conflict. In the model, a one-way line A→B means "A will not sit in, or support, a cabinet that contains B" (CoalitionFormation.cs:45-47, :64). That is exactly §4's reading of the phrase. So on §4's own logic, §621 as written dates a one-way NL→Konf line from 2023-07-05, and R5 reads that pair cabinet-only.

4. Not a plain re-raise. The first pass's refuted finding confirmed this same wording conflict was true (review :947-950), and the author changed nothing (:1150). The second-pass fix then built a stated premise on fact 16's side of that conflict, while §4, fact 17 and doubt 8 still say the opposite.

5. Which side the saved page supports. The saved NL-P17 page (digest c5fb9907… re-verified) has the phrase between two sentences about joining: "...na pewno Lewica nie wejdzie do żadnego rządu, w którym będzie Konfederacja. Mówimy naszym przyjaciołom z Platformy, od Hołowni, od Kosiniaka-Kamysza: jeżeli taką opcję rozpatrujecie, to nie liczcie na nas. My absolutnie z Konfederacją nie pójdziemy." That fits fact 16's cabinet reading, so the record's prose is the likelier side to be wrong.

6. No number moves. The derived NL–Konf line is symmetric and support-blocking: CoalitionFormation.cs:959 (oneWay is false) and n776c:561 ("CHES galtan gap 6.66 > 5.00"). It already covers RefusesSupport(NL,Konf). The R5 results in n776c (:568, :579) match the record's table, and a one-way NL→Konf line would remove no viable option.

7. Smaller points, both note-level:
   - **Hard-coding.** R5's premise is unchecked, but it holds today: the only standing support-blocking facts are 8, 9 and 17, and all three carry PolandExtension (DeclaredRedLines.cs:423, :425, :441).
   - **The "what else moves" list.** The comment's "(the June facts, TD's first date)" and COMPLETED:37171 both leave out three things: facts 7 and 16 would stand open, and Konf→KO would start on 2023-08-02 [KONF-P1]. None of these is a polling-day line, so "changes no polling-day line" still holds.

Severity: minor. The first pass graded a false claim in comments or docs under CLAUDE.md's claim convention as minor (review :904-907), and here one of two texts in the same change has to be wrong.

**The skeptic's corrected fix.** Reconcile on the record's side, which is what the page supports.

1. **The three passages that over-read the phrase:** §4 :134-137, fact 17's basis (DeclaredRedLines.cs:441) and doubt 8 (:291). They should stop crediting "nie liczcie na nas" as Lewica's support refusal. The page puts it between two sentences about joining a government, which is how fact 16 and §8 row 16 already read it.

2. **The symmetric shape's support direction.** Say which of 27 August's words carry NL's own support direction of fact 17's symmetric shape. That may be "nie będzie takiego rządu, w którym Lewica będzie razem z Konfederacją", said in reply to the 210–210 seat-buying question. Otherwise say that direction rests on the derived line. After that, R5's premise is true as written.

3. **If §4's reading is kept instead:**
   - R5 adds NL→Konf as a one-way, support-blocking line from 2023-07-05 (`new RedLine(Index("NL"), Index("Konf"), RedLineKind.Declared, true, basis, oneWay: true)`).
   - The diagnostic's :30 and :134, record §12 :255 and COMPLETED :37121/:37171 say NL's direction is dated by its own page.
   - They also say no measured government moves, because the derived NL–Konf line is already symmetric and support-blocking.

4. **Either way:**
   - Add a check that every standing BlocksSupport fact R5 strips carries `DeclaredRedLines.PolandExtension`, or build R5 from the marks, so the label "the party's own record alone" cannot silently go false.
   - Complete the "what else that reading moves" list in the comment and in COMPLETED :37171: facts 7 and 16 stand open, and Konf→KO starts on 2023-08-02 [KONF-P1]. None of these is a polling-day change.

### 13. COMPLETED §776: 'every reading seats the record's majority' is false for R0

- **Lens:** diagnostic - **reviewer:** minor - **skeptic:** minor
- **Where:** COMPLETED.md:37135

**The scenario.** The table just above lists R0, the derived lines alone, as one of the six readings. On the chamber of record R0 forms PiS+KO+TD on Konf's support (n776c R0 line, and the table's own R0 row), not the record's majority. The record's own §12 (coalition_declarations_2023.md:276) has it right: 'every reading of the declarations'.

**The fix proposed.** Write 'every reading of the declarations (R1-R5)', as §12 does.

**The skeptic's evidence.** The sentence is false as written. I tried to refute it three ways, and all three fail.

1) In §776, R0 is one of "every reading".
- COMPLETED.md:37121 sets the section's scope: "on two chambers under six readings: R0 the derived lines alone; R1 ...; R5 ...".
- The table at :37123-37130 lists R0 as a row headed "reading".
- The code says the same. PolishDeclarationsDiagnostic.cs:139 puts ("R0 the derived lines alone", Derived()) first in the `readings` list. `Measure` (:160) forms each chamber under every entry of that list, and (e) at :177-179 calls this "under every reading".
- Nothing between :37121 and :37135 narrows "reading" to the declarations.

2) "The record's majority" means KO+TD+NL, and R0 does not seat it.
- Check (g) defines the term in code: `ofRecord = Mask("KO","TD","NL")` (:147), and the test is `governs == ofRecord && (Cabinet & Mask("PiS")) == 0` (:202-203).
- Its printed text contrasts R0 with it: "...seats the record's majority: KO+TD on NL's support ... PiS outside (R0 above is what the derived lines alone seated)" (:204; n776c.log:571).
- n776c.log:563 shows what R0 seats on the chamber of record: "R0 the derived lines alone: MajorityCoalition - PiS+KO+TD on Konf's support (416 in cabinet, 434 supported...)". PiS is in the cabinet and Konf is the support, so governs is not ofRecord. COMPLETED's own table row at :37125 says the same.
- Only R1 to R5 seat "KO+TD on NL's support" (log :564-568).
- The record's §12 is correct and scoped: coalition_declarations_2023.md:276 says "every reading of the declarations seats the record's majority". COMPLETED.md:37135 dropped "of the declarations": "On the chamber of record every reading seats the record's majority, with NL outside the cabinet."

3) This was not answered in the first pass, and the fixes introduced it.
- The refuted first-pass item (Reviews/...s776...md:1084) was about the diagnostic asserting R1 only. It never dealt with whether R0 counts in this prose.
- COMPLETED.md's §776 is entirely new in the working tree (`git diff --stat`: 408 insertions), rewritten by the fixes.

Why minor and not a note:
- Finding 9 was a defect about this very phrase, "the record's majority", being overclaimed. The rewrite brings back a wrong claim in the record Elias reads.
- Taken literally, the sentence says the derived lines alone already pass the backtest. That hides the measured fact that the declarations are what take PiS out on the chamber of record. It matters for E2, whose flag concerns the derived lines.

Why not a defect:
- No code is affected and no ruling depends on it.
- The table 10 lines above, the record's §12 and the diagnostic's (g) text all state R0's result correctly.

A weaker form of the same slip is at COMPLETED.md:37170: "On the chamber of record no reading changes the government". There, doubt 1's bullet scopes it to aims vs line (R1 vs R3), so it is defensible as it stands.

**The skeptic's corrected fix.** At COMPLETED.md:37135, write: "On the chamber of record every reading of the declarations (R1-R5) seats the record's majority, with NL outside the cabinet. R0, the derived lines alone, seats PiS+KO+TD on Konf's support, as the table shows. NL outside the cabinet is the formation's cabinet-size preference, not a declaration's." This matches the record's §12 (coalition_declarations_2023.md:276). Optionally, for consistency, write :37170 as "On the chamber of record neither reading of doubt 1 changes the government."

### 14. Measure prints '(PiS)' when the record's cabinet is refused for a seatless member, not a line

- **Lens:** diagnostic - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:170

**The scenario.** `CoalitionFormation.Evaluate` (CoalitionFormation.cs:519-531) sets Admissible to false for a seatless member or a split group without setting InternalLine, which stays default (A = B = 0). Line 170 then prints the refusal as 'refused (' + Names(1) + ')', which is 'refused (PiS)'. (h) now measures a live count, so a played count where NL or TD falls under its threshold would print that the record's cabinet is 'refused (PiS)'. (e) would also fail without naming the real reason.

**The fix proposed.** Check `recordEval.SeatlessMember` and `recordEval.SplitsJointGroup` before `InternalLine`, and print 'a member holds no seat' or 'splits a group'.

**The skeptic's evidence.** The mechanism is real, and the first-pass rewrite reopened it, but only for (h)'s seatless case. It is latent: the current tree does not reach it.

1. The first pass already raised this finding and refuted it (Reviews/2026-10-04_s776_pl_declarations.md:1075-1083). It rested on two premises:
   - the seats were the readonly SeedSeats literals ("To reach the misprint you would have to change code: a different seats array");
   - a refusal would still fail the Check "loud".

   The fix's new (h) breaks both:
   - PolishDeclarationsDiagnostic.cs:242 builds `played` from a live count (`counted.Seats.TryGetValue(p.Abbrev, ...) ? s : 0`).
   - :243 calls `Measure("the played count ...", played);` and discards the bool. Nothing fails, so the misprint would be quiet. Only (e) at :179 wraps Measure in a Check.

   That makes this a legitimate re-raise under "look for anything the fixes broke".

2. The path, traced:
   - PolishSejmAllocation.cs:25-38: a party's line is 0.05 and a coalition's 0.08 (KO and TD), so a list under its line gets 0 seats.
   - CoalitionFormation.cs:420: `seats[p] <= 0` sets the seatless mask.
   - :524-525: `InternalLine` is set only when `TryFindInternalRedLine` finds a line. When none is found, :734 gives `found = default`, so A = B = 0.
   - :527-528: `SeatlessMember` makes `Admissible` false.
   - PartySystem.cs:448: PiS is roster index 0.
   - So :170 prints `"refused (" + Names((1<<0)|(1<<0)) + ")"`, which is "refused (PiS)". PiS is not even in KO+TD+NL.

3. Narrowed:
   - **Split group: unreachable for Poland.** GovernmentFormation.cs:484-485 has JointGroups for Germany only, so JointMasks returns null and `SplitsJoint(cabinet, null)` returns false (CoalitionFormation.cs:740).
   - **"(e) would also fail" is wrong.** (e) uses SeedSeats literals: KO 157, TD 65, NL 26 (PartySystem.cs:449-451).
   - **Not reached today.** The seed-777, no-policy prediction is KO 26.1%, TD 17.3%, NL 11.8% (cheap776b.log:41), against lines of 8%, 8% and 5%. n776c.log:574-579 prints "viable" under all six readings. A trigger needs NL to lose about 7 points or TD about 9, which is past even the real 2023 result (NL 8.6%, TD 14.4%).
   - **The cause would still show.** The block's header (:156-158) lists only seated parties, so the missing member would appear beside the wrong bracket.
   - **No record carries the wrong text.** COMPLETED.md §776 and the record do not quote the bracket.

Grade: a latent, cosmetic misprint in a MEASURED-only line of a standing suite check (CheckSuite.cs:214), which re-runs as the model drifts. A note, not a defect.

**The skeptic's corrected fix.** Name the refusal's actual cause from the evaluation and the seats Measure already holds. Use the line's `Basis` to tell whether a line was found (default(RedLine) has a null Basis; CreatedPartyDeclarationsDiagnostic.cs:112 uses the same test). Report every cause that applies, because Evaluate can set both an internal line and SeatlessMember.

Inside Measure (seats is in scope):

string Refusal(CoalitionFormation.CabinetEvaluation e)
{
    var why = new List<string>();
    if (e.InternalLine.Basis != null) { why.Add(Names((1 << e.InternalLine.A) | (1 << e.InternalLine.B))); }
    if (e.SeatlessMember) { why.Add(Names(Enumerable.Range(0, parties.Count).Where(p => (e.Cabinet & (1 << p)) != 0 && seats[p] <= 0).Aggregate(0, (m, p) => m | (1 << p))) + " holds no seat"); }
    if (e.SplitsJointGroup) { why.Add("splits a parliamentary group"); }
    return "refused (" + string.Join("; ", why) + ")";
}

Then at line 170, replace the "refused (" + Names(...) + ")" branch with:

!recordEval.Admissible ? Refusal(recordEval) : ...

The SplitsJointGroup branch cannot fire for Poland today; it is kept so the print stays true if a Polish joint group is ever added. Leave the (e) Check message (:179) unchanged: seatless cannot reach it on the seed seats.

### 15. Doubt 2's '(194 + 65 = 259)' still stands without a bracketed correction

- **Lens:** diagnostic - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:285

**The scenario.** The quoted sweep text says 'The model can therefore seat a PiS cabinet on TD's support (194 + 65 = 259)'. Under R1 on the chamber of record, the sweep's 'Poland | seated | negative False' block (formation_sweep.txt:94049-94056) lists six viable cabinets, and none is cab 1 (PiS alone). The first pass's skeptic said so. Doubt 1's '(351)' gets a bracket pointing at the measurement; doubt 2's figure gets none. The response to finding 6 says doubts 1 and 2's figures are bracketed, but only the gap figures were replaced.

**The fix proposed.** Add a record note, for example '[record: not viable on the chamber of record; on the played count R1 seats PiS+KO on TD's support - §12's table]', or delete the figure, as the skeptic's fix 2 asked.

**The skeptic's evidence.** I could not refute it. One detail of the finding is wrong, but the problem it points at is real.

**What the record says.**
- Line 282 sets up the list: "The sweep's own list, as its synthesis wrote it, the record's corrections and notes in square brackets".
- Line 285, doubt 2: "The model can therefore seat a PiS cabinet on TD's support (194 + 65 = 259)." This has no bracketed note. Neither does the doubt's last sentence, "Measure both readings as a diagnostic row before Elias rules.", although R2 has since measured it.
- Line 284, doubt 1, does get one after its "(351)": "[record: measured above - on the played count this doubt decides the government]".

**What the fix script did.** In the author's fix script (scratchpad `fix776_record.pl`):
- `d1b` (lines 66-67) adds doubt 1's bracketed note.
- `d2a` (line 68) only replaces doubt 2's gap figures ("(galtan 2.59, lrgen 1.61)" becomes "[record: under both gaps, §10]").
- The backup `record776.bak` (doubt 2 at its line 260) shows "(194 + 65 = 259)" was already there before the fix and was left alone.

**The detail the finding gets wrong.** The first pass's fix 2 (review line 302) and the skeptic's stale-claim note (review line 271) were about §10's own copy of this claim. In `record776.bak`, §10 read: "a PiS cabinet on TD's support (259) — neither what the record nor the outcome shows". That copy is gone; §10 at lines 219-223 now says "§12 measures what that does". The response's "Doubts 1 and 2's figures are bracketed" refers to fix 3's gap figures, and those were bracketed. So that answer is not wrong. What remains is the same false claim, left unmarked in doubt 2.

**The claim is false as written** (PiS alone, with TD supporting from outside):
- `formation_sweep.txt`, lines 94049-94056 (Poland | seated | negative False, which is R1 on the chamber of record), lists six viable cabinets: cab 6 sup 8, cab 3 sup 4 (PiS+KO on TD, 416), cab 14, cab 2 sup 12, cab 10 sup 4 and cab 12 sup 2. Cab 1, PiS alone, is not there.
- Under positive investiture a cabinet wins only if `supported >= chamber.Majority` (`CoalitionFormation.cs:597`). 194 + 65 = 259 ≥ 231, so TD cannot be among PiS-alone's supporters.
- Who supports a cabinet is decided by `SupportersOf` step 1 (`CoalitionFormation.cs:789-798`). It compares compatibility from `GovernmentFormation.Compatibility` (`GovernmentFormation.cs:433-458`), which uses positions only, never seats. Step 2 only removes supporters. So PiS alone gets no TD support on any seat count.
- On the played count, `n776c.log:573-579` shows R1 seating "PiS+KO on TD's support (323 in cabinet, 401 supported)". TD's support goes to PiS+KO, not to PiS alone.

**Why only a note.**
- The false figure sits inside a quotation that is labelled as one.
- §12's own measured text (lines 270-274: "Doubt 2 decides only TD's support.") states the result correctly.
- The owed-to-Elias list in COMPLETED.md (line 37172: "on the played count it decides only TD's support of PiS+KO") also states it correctly.
- No code is affected.

**The skeptic's corrected fix.** In ElectionsData/poland/coalition_declarations_2023.md, line 285 (doubt 2), make two text-only changes. No code change is needed.

1. Replace "(194 + 65 = 259)" with a record note in the list's own style, as was done for the gap figures. For example: "[record: measured above - the formation seats no PiS cabinet alone on TD's support; TD's support goes to PiS+KO (the played count, R1), and R2 removes only that support - §12's table]". Pointing at §12's table keeps this within the claim convention and keeps every figure in the diagnostic's printed output.
2. After "Measure both readings as a diagnostic row before Elias rules." add "[record: measured above as R1 and R2]", matching doubt 1's "measured above" note.

### 16. 'A mark reaches no Polish player until D-PL's night is built' misses the game-over verdict

- **Lens:** diagnostic - **reviewer:** note - **skeptic:** note
- **Where:** COMPLETED.md:37173

**The scenario.** Take a Polish player whose party wins no seat on 15 Oct 2023. Their verdict is the out-of-parliament sentence, which appends the formation's DERIVED caveat (`sourcedNote`, GameController.cs:6437-6454). Poland has no night (`ElectionNightFromModel.Available` covers Sweden and Germany only, ElectionNightFromModel.cs:38-39). So CheckElection calls ApplyElectionVerdict at once, the sentence becomes `_gameOverReason` (GameController.cs:6719), and the GAME OVER banner prints it (GameController.cs:1961). A reading-level mark carried in the same caveat would reach that player today. Doubt 14's note in §12 (coalition_declarations_2023.md:297) says the same.

**The fix proposed.** Say it reaches a Polish player only on a run-ending verdict until D-PL's night is built.

**The skeptic's evidence.** I could not refute it. Every step of the path holds in the working tree. The GameController.cs edits in the tree belong to other items and do not touch the verdict code.

1. **The caveat is on the run-ending sentence.**
   - GameController.cs:6437-6439 builds `sourcedNote` from `government.DeclarationsSourced`.
   - It is appended in only two places:
     - the in-office sentence (6442);
     - the out-of-parliament sentence (6451-6456): `"...won no seat, and a run ends when its party leaves the chamber.{sourcedNote}"`, with `_pendingElectionVerdictEndsGame = true`.
   - Poland does not take the earlier Speaker's-round branch, whose out-of-parliament line carries no note. `RoundsApply` (SimulationManager.cs:3223) needs `Rules.Riksdag` or `Rules.Bundestag`, and ConfidenceProcedure.cs:34 gives Poland `Rules.Unsourced`.

2. **Without a night, the verdict lands at once.**
   - ElectionNightFromModel.cs:38-39: `Available` covers Sweden and Germany only.
   - `ShowElectionNight` returns early (GameController.cs:6494-6498).
   - `CheckElection` therefore calls `ApplyElectionVerdict()` at once (6361-6366).
   - That method sets `_isGameOver = true; _gameOverReason = _pendingElectionVerdict` (6716-6720).
   - The full reason is printed in three places: `"GAME OVER - {_gameOverReason}"` at 1961 and 5866, and the Desk's game-over plate (GameController.Desk.cs:1144, `reasonText`).
   - The in-office sentence is simply nulled (6722), so it never shows. The run-ending sentence is the only route, which is exactly what the finding's wording says.

3. **The path is reachable in an ordinary Polish game.**
   - Poland votes on 15 Oct 2023: `WorldClock.TryNextPollingDay`, and `NationalElection`'s `PolandDistricts` branch.
   - The party picker reads the chamber at the start (`WorldClock.PickerViewOf`, `SeatedVintage` = Poland2019: SLD 49, PSL 30, MN 1).
   - `IsPlayable` (PartySystem.cs:809-813) admits any party seated there, so SLD and PSL can be chosen.
   - Both have NaN positions (PartySystem.cs:456-458) and a prior of 0.0 in both vintages (PartySystem.cs:620-627: "SLD and PSL carry nothing").
   - `TryCompatibility` (NationalElection.cs:150-166) gives each zero compatibility and a zero prior, so each gets zero share and zero seats.
   - The game's own entry forms a government on that count (the n776c played count gives R1: PiS+KO). Such a player therefore gets the out-of-parliament verdict, `{sourcedNote}` included, as the GAME OVER reason.

4. **The text drops the review's own qualifier.**
   - The first-pass skeptic said so itself: review line 480, "a Polish player saw the caveat only as the end of the out-of-parliament game-over sentence". Its corrected fix (line 513) kept the qualifier "because the verdict alone is shown only on game over".
   - The author's text makes the claim absolute in three places:
     - COMPLETED.md:37173, "it reaches no Polish player until D-PL's night is built";
     - coalition_declarations_2023.md:297 (doubt 14's note), the same words;
     - the review's response at line 1128.
   - A reading-level mark carried in that same caveat would reach a Polish player today, but only on the run-ending verdict.

**Severity: note.** This is wording in the record and in doubt 14's framing for Elias; there is no code defect. What the sentence says in substance stays true: no Polish player who keeps playing sees a formation mark until a Polish night exists. So the overstatement changes very little about the ruling.

**The skeptic's corrected fix.** Text only, in COMPLETED.md §776 (the doubt-14 bullet in the owed list) and in ElectionsData/poland/coalition_declarations_2023.md doubt 14's bracketed note.

Replace "and it reaches no Polish player until D-PL's night is built" with:

"and until D-PL's night is built it reaches a Polish player only on the verdict that ends the run. The out-of-parliament sentence is the one verdict that carries the formation's caveat and is shown without a night, as the game-over reason; the in-office sentence is dropped unshown."

Names only, no line numbers or counts, per the claim convention.

Optionally, the author's response in Reviews/2026-10-04_s776_pl_declarations.md ("it reaches no Polish player until D-PL's night exists") can carry the same qualifier.

### 17. (h)'s seed and path copy PollingDayDiagnostic's; only a comment ties them

- **Lens:** diagnostic - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:271

**The scenario.** `PlayedSeed = 777` duplicates PollingDayDiagnostic's private `Seed`, and the day loop is a copy rather than the shared helper the first pass's skeptic asked for. The copies already differ: (5) calls TryPlayerPollingDay, which is pure, and runs to 2023-10-20, while (h) stops at the count. Today both print the same count (cheap776b's (5) and n776c's (h): KO 123, Konf 14, NL 45, PiS 200, TD 78). If either seed or either loop changes, the doc comment at line 271 ('so the two read the same Polish game') goes false with nothing failing.

**The fix proposed.** Move the seed and the Polish path into one helper that both diagnostics call, or have (h) assert that its count equals the helper's.

**The skeptic's evidence.** The tie is a comment only. PolishDeclarationsDiagnostic.cs:271-272: `/// <summary>The played count's seed - PollingDayDiagnostic's, so the two read the same Polish game.</summary>` over `private const int PlayedSeed = 777;`. PollingDayDiagnostic.cs:25 has its own `private const int Seed = 777;`. Being private, it cannot be referenced, and nothing references it.

The loops are copies. (5) at PollingDayDiagnostic.cs:148-178 and (h) at PolishDeclarationsDiagnostic.cs:212-236 do the same setup:
- ApplyStart(Poland), Seed, ResetCalibration, CreateDefault, AddComponent, SetWorld, PlayerCountryId, PlayerPartyAbbrev "PiS";
- the same daily step: AdvanceDay, then AdvanceTurn(none);
- the same count: TryPredictShares with RecordOverTerm, then NationalElection.Run.

That is GameController.RunNationalElection's no-campaign path (GameController.cs:6672-6687), copied twice.

The differences the finding lists exist and do not move the count:
- (5) calls TryPlayerPollingDay. SimulationManager.cs:4200-4207 only reads PlayerCountryId, CurrentDate and _extraElectionDate. WorldClock.TryNextPollingDay (WorldClock.cs:415-427) is date arithmetic. AdvanceDay calls it every day anyway (SimulationManager.cs:477).
- (5) reads the staged flags and runs on to 2023-10-20; (h) stops at the count (`counted == null && CurrentDate <= PollingDay`).

The helper was never built. The first pass's corrected fix asked for "a shared helper" (Reviews/2026-10-04_s776_pl_declarations.md:446). The author's answer (:1120) says only "as PollingDayDiagnostic (5) does" and does not mention the helper. PollingDayDiagnostic.cs's only working-tree diff is E2's basis label (lines 80-81), not the Polish path.

Today the two agree. cheap776b.log:5339 is (5) and n776c.log:572 is (h); cheap776b.log:11088 is (h) again. All three read KO 123, Konf 14, NL 45, PiS 200, TD 78.

No check compares them. (5) asserts only `Method == PolandDistricts && seated == 460` (PollingDayDiagnostic.cs:187). (h) asserts only the date and a seat sum of 460 (PolishDeclarationsDiagnostic.cs:237).

How it fails: edit PollingDayDiagnostic.cs:25, or its Polish loop. Then line 271 ("the two read the same Polish game"), the class doc at :27 ("by `PollingDayDiagnostic`'s path") and the comment at :208 are all false, and the bar stays green. The claim convention's test, which covers source comments (CLAUDE.md:33, :37), is "the code can change freely and no document becomes wrong". These comments fail it.

What limits the damage, and why this is a note:
- COMPLETED.md:37121-37123 cites (h)'s own run (n776c) and records "seed 777" beside the count.
- (h)'s printed line names its seed (:238).
- So a drift would make only the three cross-reference comments wrong, not any claim to Elias.
- 777 is the project-wide film seed, written as a literal in dozens of diagnostics, so a lone change is unlikely.
- First-pass finding 17 (review :737-767) is the same class: a fact about the code written down instead of referenced, correct today, wrong only latently. It was graded note/note, confirmed and fixed.

The repo already has the idiom for the fix: LeverProbes.cs:21 `public const int Seed = 777;` is referenced by name at BudgetPremiseDump.cs:97 and LeverMapDump.cs:39 and :76.

**The skeptic's corrected fix.** 1. The seed, by reference (the LeverProbes.Seed idiom).
   - PollingDayDiagnostic.cs:25: change to `internal const int Seed = 777;`.
   - PolishDeclarationsDiagnostic.cs:272: change to `private const int PlayedSeed = PollingDayDiagnostic.Seed;`.
   - The summary at :271 then states what the compiler holds.

2. The path: pick one of two.
   - (a) Build the helper the first pass asked for. Move the Polish start-to-count path into one internal method in PollingDayDiagnostic, for example `internal static ElectionRecord CountPolishGame(SimulationManager sim, Country player, DateTime runTo, Action<SimulationManager> eachDay = null)`. It advances days, AdvanceTurn(none) on a boundary, and counts on PollingDayToday through TryPredictShares with RecordOverTerm, then NationalElection.Run. Setup and teardown stay with each caller. (5) passes its flagged and staged observer and 2023-10-20; (h) passes PollingDay.
   - (b) Keep the copy, and point the comments at :27, :208 and :271 to the path both copy, GameController.RunNationalElection's no-campaign path. Drop "so the two read the same Polish game".

3. In the review's answer to finding 9, say whether the helper was built and why. That answer is currently silent on the departure.

No figure changes. n776c's count and COMPLETED §776's table stand.

## The second pass - refuted by the skeptics


## What the author did about the second pass

No defect; six minors and eleven notes, all fixed. Nothing the game computes moved: the code changes are in the diagnostic and in the timeline's
text, and n776d measures the tree after them.

- **1 - fixed.** Germany's record says what holds:
  - no country-wide caveat shows, because the night's caption and the verdict's note are keyed on `IsSourced`;
  - a derived line's basis shows only on the formation sheet's answer slip, when a partner's or a supporter's refusal names it - the first matching line, and derived lines come first;
  - a pair with no line is marked nowhere.

  Its §5 sentence is corrected the same way. Poland's header gives the real reasons nothing marks its derived pairs: no formation sheet (`Unsourced` confidence rules), no night, a country-wide caveat keyed on `IsSourced`, and no line at all on the eight pairs. §776's record follows.
- **2 and 3 - fixed.** §2's restated list is split by pair:
  - toward PiS, 10 October's restatement is "Gdybym … zrobił rząd z PiS-em, straciłbym całkowicie wiarygodność" *(decoded)*;
  - toward KO, only [KONF-P1] and [KONF-I25];
  - 10 October's "zakończyć … nie dopuścić" is two aims, restating neither half;
  - the after-the-vote clause names the coalition halves. Doubt 5's bracket says [KONF-P3] carries the coalition half only.
- **4 - fixed.** §621 is quoted word for word, document clause included, in the record and in §776's record. The first pass's response at its "as ruled" line was abridged; this note corrects it.
- **5 and 12 - fixed on the record's side, as the skeptics recommend.**
  - §4, fact 17's basis and doubt 8 stop crediting 5 July's "nie liczcie na nas" as a support refusal. Read with its sentence, it is fact 16's cabinet refusal.
  - The 27 August words refuse a shared cabinet, and a Lewica cabinet resting on Konfederacja's bought votes.
  - Lewica's own support direction is not in its words and rests on the derived NL–Konf line.
  - R5's premise is therefore true as written. A new check holds every support half R5 strips to the extension's mark.
  - §652's condition (an earlier own-record date wins; none does here) joins the extension's statement in the record, §776's record and both doc comments.
  - The comment and §776's record list everything else R5 moves; none of it is a polling-day change.
- **6 - fixed.**
  - Doubt 9 lists the two newly registered pages' dates.
  - Doubt 3's bracket says PSL's leg of the 2022-07-19 alternative rests on a newspaper's report ([TD-I14]), so the stated rules support 2022-08-09.
  - [TD-I14]'s publisher reads "Rzeczpospolita (reporting Polsat News)", and [NL-P30] carries its dateline.
  - *(decoded)* marks are on "odsunąć PiS od władzy" and "w naszym imieniu".
  - The video's date reads "22 June, UTC".
- **7 and 15 - fixed.**
  - Doubt 2's "(194 + 65 = 259)" is replaced by a bracketed correction: the model seats no PiS cabinet alone on TD's support, and TD's support goes to PiS+KO. Its "measure both readings" gets "measured above as R1 and R2".
  - Doubt 4's bracket names Super Express's print date as PAP gives it.
  - The list's intro says the register tags and doubt 6's attribution were changed in place.
- **8 - fixed.** The K-1i line drops its count and dates its measured outcome.
- **9 - fixed.**
  - The fetch log's URL sentence says what the saved bytes carry, and names the one uncited scan with its fetched URL.
  - The record and §776's record narrow the in-tree claim to the register's sources, and name the three tags borrowed from `records_by_date.md`, held under `raw/records/`.
- **10 - fixed.** The class summary names `GermanySource` and `PolandSource`, and Sweden's two records below. `SwedenTimeline`'s summary states its dating rule by reference: §621, or §652's ruling or the extension where a fact's comment says so.
- **11 - fixed.** (h) holds the government the night stores (`GovernmentFormation.ViewOf` on the election's dated reading) to R1's cabinet and support, as sets of keys. The display-string parse is gone, and the record, §776's record and the class summary name `ViewOf`. Measured: PiS+KO on TD's support, R1's own.
- **13 - fixed.** §776's record says every reading of the declarations (R1-R5) seats the record's majority on the chamber of record, and R0 seats PiS+KO+TD on Konf's support. Its owed list says neither reading of doubt 1 changes that chamber's government.
- **14 - fixed.** `Measure` names a refusal by its cause: a line inside, a seatless member, a split group.
- **16 - fixed.** Doubt 14's bracket and §776's owed list say a mark reaches a Polish player, until D-PL's night exists, only on the verdict that ends the run (the out-of-parliament sentence, shown as the game-over reason).
- **17 - option (b), stated.** The helper the first pass suggested was not built. Building it would put a §776 change into `PollingDayDiagnostic.cs`, which §773's commit carries. (h) and the seed say what they are: a copy of `GameController.RunNationalElection`'s no-campaign path, which `PollingDayDiagnostic` (5) also copies, with the seed tied only by the note.

**Measured after the fixes:** n776d, 11 of 11 clean:
- the Polish declarations, with R5's own-record check and (h)'s stored government;
- the declaration dates, every timeline in order;
- the formation sweep at its pin;
- the office test, Germany's formation, the dead-state and unwired checks, and the four text checks.

The review ends at two passes: the second found no defect, and its fixes are measured.
