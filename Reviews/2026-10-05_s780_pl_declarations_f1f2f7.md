# Review - §780, Elias's rulings F1, F2 and F7: Poland's 2023 declarations read again, KO → PiS dated by KO's own record (2026-10-05)

A workflow review (`polisim-staged-review`) of section 780's files - `DeclaredRedLines.cs` (`PolandTimeline`), `PolishDeclarationsDiagnostic.cs`, `DeclarationDatesDiagnostic.cs`, `FormationSweepDiagnostic.cs`, the record `ElectionsData/poland/coalition_declarations_2023.md` and its pages under `raw/declarations_2023/`, CLAUDE.md's standing rules and the K-1i row - in four lenses: the facts against the pages, the rulings' reading, the checks' power, and the record's and the rules' hygiene. Every finding was put to a refute-first skeptic; the reports are verbatim. A second pass read the fixes. Not a money path (`Tools/bar_tier.ps1`'s pattern names none of these files), so no ledger row.

## The first pass - confirmed (verbatim)

The workflow polisim-staged-review on section 780's files, four lenses (the facts against the pages, the rulings' reading, the checks' power, the record's and the rules' hygiene), every finding put to a refute-first skeptic.

### 1. The record describes Biedroń's TVN24 page wrongly; the description is the stated reason fact 14 (NL → Konf) stays cabinet-only

- **Lens:** facts - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:160

**The scenario.** §4 (lines 159-161) and §12's item for Elias (line 324) say Biedroń's words at the Łódź convention, on TVN24's own page of 2023-09-02, are 'a pledge against the PiS–Konfederacja coalition, not toward Konfederacja itself'. That page is the sweep's G:/UNITY/Projects/PoliSim-captures/sources/poland_declarations_2023/NL/tvn24_st7322187_konwencja_lodz.html (datePublished 2023-09-02T11:18:55Z, customSource TVN24). Right after 'My tę czarno-brunatną koalicję zatrzymamy' it quotes Biedroń, whom the page calls 'lider Nowej Lewicy', verbatim: '- Po pierwsze, musimy być tarczą przeciwko Konfederacji.' Those words are aimed at Konfederacja itself. So Elias is asked to rule NL → Konf's support half on a summary that leaves out the sentence most relevant to it. If he reads it as F1's 'not let it govern', fact 14 (DeclaredRedLines.cs:439-440, cabinet-only from 07-05, Open) should be replaced on 2023-09-02 by a one-way, support-blocking NL → Konf fact dated by F2. Formation is unchanged, because the derived NL–Konf line is already support-blocking both ways.

**The fix proposed.** Quote 'Po pierwsze, musimy być tarczą przeciwko Konfederacji' in §4 and in §12's NL → Konf item, and say why it is or is not F1's pledge. If it is ruled F1, split fact 14 at 2023-09-02 with a PolandSpokenWords fact, and bring the page into the tree with a register row.

**The skeptic's evidence.** I could not refute the finding. Every factual premise checks out against the saved bytes.

1. What the record says. Lines 159-161 (§4) and line 324 (§12, "Left to Elias") describe the Łódź page only through "My tę czarno-brunatną koalicję zatrzymamy". Both conclude "a pledge against the PiS–Konfederacja coalition, not toward Konfederacja itself". The word "tarcz" appears nowhere in the record, in DeclaredRedLines.cs, in the diagnostic or in the K-1i row.

2. What the page says. The page is PoliSim-captures/sources/poland_declarations_2023/NL/tvn24_st7322187_konwencja_lodz.html. Its datePublished is "2023-09-02T11:18:55.000Z", and customSource and og:site_name are both TVN24.
   - Decoded body paragraph 4 ends "...Państwo koalicji PiS i Konfederacji wisi nad nami. ... My tę czarno-brunatną koalicję zatrzymamy - zapowiedział."
   - Paragraph 5 follows immediately: "Biedroń wymienił najważniejsze zadania, jakie stoją dzisiaj przed Lewicą. - Po pierwsze, musimy być tarczą przeciwko Konfederacji. Po drugie, w tym wyścigu musimy zająć trzecie miejsce. ... To nasze zadanie na te wybory i na najbliższe lata - oświadczył".
   - This is direct speech marked with a dash, the same form the record already accepts for [KONF-I14].
   - Paragraph 3 calls him "lider Nowej Lewicy".
   - TVN24's own meta and og:description lead with "Musimy być tarczą przeciwko Konfederacji".

3. The session saw the sentence. The finder's NL-28 note in the session scratchpad (f12/find_NL.txt) reads: "On the same page Biedroń says 'musimy być tarczą przeciwko Konfederacji' (also not F1 in terms)". That judgement never reached the record, so the ruling item Elias receives leaves out the one sentence aimed at Konfederacja itself.

4. A correction to the title. The coalition sentence is described accurately. The defect is what was left out: because of it, "not toward Konfederacja itself" misstates the page. Whether "tarcza" counts as F1 is a real open question. The sentence names no power or government, and it stands in a list next to "take third place".

5. No change to how the game behaves:
   - Fact 14 at DeclaredRedLines.cs:439-440 is `new DatedFact("NL", "Konf", FactKind.PairLine, false, false, null, D(2023, 7, 5), Open, ...)`.
   - ForDateSourced adds declared lines beside `DerivedRedLines.From` and replaces none (`lines.Add`, around line 491).
   - In PartySystem.cs PolandParties, NL's galtan is 1.75 and Konf's is 8.41. The gap of 6.66 is above `SocialGap` 5.0, so the derived line is drawn with socialBreak = true and blocks support.
   - `RefusesSupport` is `BlocksSupport && (OneWay ? A == p && B == q : Covers(p, q))`, so that derived line already refuses support in both directions. A one-way, support-blocking NL → Konf fact would not change any formation.

6. Two related gaps:
   - The citation of the Łódź page has no register row and lacks the "out of tree" mark that Budka's TVN24 citation carries.
   - NL's own page of 2023-08-26 (lewica_org_20230826_wiec_czestochowa.html; datePublished 2023-08-26, dateModified 2026-01-29) has words of the same kind aimed at Konfederacja. Biejat says "Pokonamy ich! Zatrzymamy tę brunatną falę"; Zandberg says "Trzeba zatrzymać brunatną falę skrajnej prawicy ... I zatrzymamy to!". The record does not cite that page either.

**The skeptic's corrected fix.** Make the change in the record only; no code changes unless Elias rules.

1. In §4, after "My tę czarno-brunatną koalicję zatrzymamy", quote the next sentence on the same TVN24 page: "Po pierwsze, musimy być tarczą przeciwko Konfederacji." Give its context, a list of campaign tasks next to "w tym wyścigu musimy zająć trzecie miejsce". State the builder's reading: it is not F1 in terms, because it names no power or government. Then rewrite the "not toward Konfederacja itself" clause so it no longer claims the page has nothing aimed at Konfederacja.

2. Rewrite §12's NL → Konf item so Elias rules on both sentences: the coalition sentence and the shield sentence.

3. Mark the citation "out of tree", as Budka's is, or give the page a register row.

4. Add to the same §12 item that NL's own Częstochowa page of 2023-08-26 carries words of the same kind (Biejat's "Zatrzymamy tę brunatną falę", Zandberg's "Trzeba zatrzymać brunatną falę skrajnej prawicy ... I zatrzymamy to!"). Note that this page carries dateModified 2026-01-29.

If Elias rules either set of words to be F1, split fact 14 into a one-way, support-blocking NL → Konf fact. Under F2's "a date from the party's own record always wins", a ruling that counts the Częstochowa words starts it on 2023-08-26. Only the TVN24 page alone would start it on 2023-09-02, with PolandSpokenWords. Bring the page used into the tree with a SHA256SUMS row.

Formation does not change either way, because the derived NL–Konf line (galtan gap 6.66, above SocialGap 5.0) already blocks support in both directions.

### 2. Fact 13's basis misparaphrases NL-P31: 'te listy' are the opposition's lists, not Nowa Lewica's

- **Lens:** facts - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/DeclaredRedLines.cs:438

**The scenario.** The basis opens 'Nowa Lewica's lists must win to remove PiS from power'. In NL-P31 (NL/lewica_org_20220920_czarzasty_deklaracja_koalicji.html) Czarzasty is addressing 'moich przyjaciół z opozycji': 'I wreszcie bezpieczeństwo to odpowiedzialna opozycja… Nie jest najważniejsze czy będzie lista jedna, czy dwie, czy trzy. Ważne jest to, aby te listy były zwycięskie, aby odsunąć PiS od władzy.' 'These lists' are the opposition's list or lists, one, two or three. The line itself still follows from the page, because NL's co-chairman pledges PiS's removal. But the citation the game carries attributes to NL a claim about its own lists that the page does not make. §4 of the record quotes the page correctly; only the code's gloss is wrong.

**The fix proposed.** Rewrite the gloss, for example 'NL's co-chairman: the opposition's lists - one, two or three - must win, to remove PiS from power', or drop the gloss and keep the quote.

**The skeptic's evidence.** I could not refute it. The page shows the gloss is wrong.

THE CODE. DeclaredRedLines.cs line 438 (fact 13, NL -> PiS, one-way, support-blocking, from 2022-09-20) reads:
"DECLARED (F1): Nowa Lewica's lists must win to remove PiS from power - Czarzasty on the party's own page, 2022-09-20 [NL-P31] (\"Ważne jest to, aby te listy były zwycięskie, aby odsunąć PiS od władzy.\")..."

THE PAGE. I decoded NL/lewica_org_20220920_czarzasty_deklaracja_koalicji.html (as UTF-8, then its entities).
- The footer reads: "Włodzimierz Czarzasty, współprzewodniczący Nowej Lewicy i wicemarszałek Sejmu Spotkanie liderów opozycji podczas konferencji 'Bezpieczeństwo wschodniej flanki NATO – rola Polski'". So this is a speech to a meeting of opposition leaders.
- It opens: "Serdecznie witam moich przyjaciół z opozycji".
- The passage: "I wreszcie bezpieczeństwo to odpowiedzialna opozycja. Taka, która ze sobą rozmawia, wspiera się, spotyka. Nie jest najważniejsze czy będzie lista jedna, czy dwie, czy trzy. Ważne jest to, aby te listy były zwycięskie, aby odsunąć PiS od władzy."

"te listy" points back to "lista jedna, czy dwie, czy trzy", which is the opposition running one, two or three lists (the joint-list question). The subject of the paragraph is "odpowiedzialna opozycja". The page says nothing about Nowa Lewica's own lists. Reading the plural as NL's constituency lists does not survive the "one, two or three" sentence.

The quote in the code is verbatim and correct. Only the English gloss in front of it narrows the opposition's lists to Nowa Lewica's.

WHAT IS UNAFFECTED
- The record is correct. §4 of coalition_declarations_2023.md (line 169) gives only the quote, with no "NL's lists" gloss. The gloss appears nowhere else: I grepped the record, fetch_log, CLAUDE.md, the diagnostic and the K-1i row.
- The line still follows from the page under F1. NL's co-chairman, on NL's own page, pledges removing PiS from power. The flags (true, true), the date and the Open end are unchanged.
- No computed result moves.

WHY IT STILL MATTERS (minor, not a note)
- The basis string can reach a player. DeclaredRedLines.cs:491 puts f.Basis into RedLine.Basis. FormationProposal.cs:171 writes "refuses: a red line falls inside this cabinet - " + inside.Value.Basis. GameController.FormationSheet.cs:310 shows that reason on the answer slip.
- So in a Polish game, NL offered a seat beside PiS would show the misattributed paraphrase on screen.
- The repo's standard (every DECLARED premise true to the page; F2's "a paraphrase never counts") makes a wrong gloss on a DECLARED citation a real, if small, defect.

**The skeptic's corrected fix.** In DeclaredRedLines.cs line 438, change only the gloss and keep the rest of the string, for example:

"DECLARED (F1): Nowa Lewica will remove PiS from power - its co-chairman Czarzasty to the opposition's leaders, on the party's own page, 2022-09-20 [NL-P31]: whether the opposition runs one list, two or three, those lists must win, to remove PiS from power (\"Ważne jest to, aby te listy były zwycięskie, aby odsunąć PiS od władzy.\"). Restated by its National Board's resolution ... [unchanged] ..." + PolandSource

Keep [NL-P31] as the first tag and keep PolandSpokenWords off this fact. PolishDeclarationsDiagnostic's F2 check reads the first tag as an own-page date, and it also checks the DECLARED prefix and the PolandSource ending; all three still hold.

No pin moves:
- FormationSweepDiagnostic's hashed text prints only "blocked <cabinet> by A-B", never the basis.
- PolishDeclarationsDiagnostic (f) compares the same Basis on both sides.

The record's §4 needs no change.

### 3. NL → PiS: fact 12's own cited page also says 'PiS trzeba pokonać'; the record never says whether 'defeat' counts under F1

- **Lens:** facts - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Elections/DeclaredRedLines.cs:435

**The scenario.** Fact 12 (cabinet-only, 2021-05-06 to 2022-09-20) quotes NL-P3: 'Z PiS-em nigdy w życiu nie wejdę w żadną koalicję'. The next sentence in the same bytes is 'PiS trzeba pokonać, opozycja musi to zrobić i za dwa lata wydawać te pieniądze', and the paragraph before has 'za dwa lata opozycja przejmie władzę'. The record reads 'odsunąć PiS od władzy' (NL-P31, 2022-09-20) as F1 but never says whether 'pokonać PiS' (defeat PiS) counts. The same verb recurs on NL's own page of 2022-01-31 ('Podstawowa sprawa to pokonanie PiS', out of tree), in NL-P25's title and in KO-I11 ('pokonamy PiS'). If it counts, fact 12 should be one-way and support-blocking from 2021-05-06, and the 2022-09-20 'replacement' is spurious. There is no game effect: Poland opens on 2023-02-19, and NL holds no seat in the 2019 chamber.

**The fix proposed.** State the reading of 'pokonać' in F1's paragraph (header or §10), or start the F1 line at 2021-05-06 and make NL-P31 a restatement.

**The skeptic's evidence.** I could not refute it. Every factual claim in the finding holds against the saved bytes, and the record is silent on the point.

1. **The code.** DeclaredRedLines.cs:435-436 is fact 12: NL->PiS, cabinet only (false/false), 2021-05-06 to 2022-09-20, citing [NL-P3]. Its text says "A cabinet only. Replaced on 2022-09-20 by the one-way support-blocking line (F1)". Lines 437-438 are fact 13: one-way and support-blocking from 2022-09-20, citing [NL-P31] ("aby odsunąć PiS od władzy").

2. **The NL-P3 page.** File: NL/lewica_org_20210506_czarzasty_rozmawial_z_diablem.html. Its sha256 8c90f556... matches SHA256SUMS line 28. The decoded body reads: "– Rozmawiałem z premierem w sprawie pieniędzy. Z PiS-em nigdy w życiu nie wejdę w żadną koalicję. PiS trzeba pokonać, opozycja musi to zrobić i za dwa lata wydawać te pieniądze – podkreślał." The paragraph before it reads: "Dodał, że jego zdaniem „za dwa lata opozycja przejmie władzę i będzie wydawała te pieniądze”." So the sentence right after the quoted refusal is "PiS trzeba pokonać", as claimed.

3. **The same verb on other pages.**
   - NL-P31 itself carries both phrases: "aby odsunąć PiS od władzy" and "Będzie się liczył w tym, żeby pokonać PiS".
   - KO-I11: "pokonamy PiS i że PiS nie będzie zwycięzcą tych wyborów".
   - NL's own page of 2022-01-31 (out of tree, lewica_org_20220131_czarzasty_musimy_wspolrzadzic.html): "Podstawowa sprawa to pokonanie PiS."
   - NL-P25's URL contains "nie-pokona-brunatnej-sily-i-pis".

4. **The record never decides it.** In coalition_declarations_2023.md, "pokona" appears only inside NL-P25's URL (line 373). F1's standard, in the header and in §10, lists "remove from power, end its rule, block its return, not let it govern" and says nothing about "defeat". §4's NL->PiS paragraph quotes only the coalition sentence from NL-P3. §12's "Left to Elias" list names the earlier, less plain candidates for PiS->KO ("Ten człowiek nie może rządzić Polską") and KO->PiS ("pogonimy Kaczyńskiego", "żeby pogonić to zło"), but has no entry for NL->PiS.

5. **The reading is also applied unevenly.** §3 reads TD-P10's slogan "Trzecia Droga, albo trzecia kadencja PiS" as an F1 restatement. That slogan is looser than "PiS trzeba pokonać, opozycja musi to zrobić i za dwa lata wydawać te pieniądze".

6. **The session's own verification flagged this choice for Elias.** In scratchpad f12/find_NL.txt, item NL-5 reads: "'PiS must be defeated': an electoral aim, not one of F1's power forms … Read strictly, this is not F1. If the owner reads 'pokonać PiS' as 'remove from power', the one-way line toward PiS would start 2021-05-06". The NL->PiS proposal lists the same point under "Reading choices for the owner: (a)…". So the builder made the reading but never wrote it into the record.

7. **No game effect, confirmed.**
   - WorldClock.StartDate(Poland) = CampaignCalendar(2023-10-15).PreCampaignStart = 2023-10-15 minus (8+26) weeks = 2023-02-19. That is after 2022-09-20, so fact 13 stands on every date a game reads.
   - PartySystem.cs:764 gives the Poland2019 chamber ("NL", 0) seats.
   - §10 says the derived PiS-NL line already blocks support.
   - No diagnostic reads a Polish date between 2021-05-06 and 2022-09-20. The dates read are 2023-02-19, 2023-10-15 and 2026-01-18.

The result is a gap in what the record discloses, with no change to any formation. The grade stays at note.

Aside, outside this finding: the same verifier file raises a second choice for this pair that the record also does not list. Żukowska's refusal on NL's own page of 2021-04-28 (out of tree) would be an earlier start for the cabinet-only segment.

**The skeptic's corrected fix.** Leave the array and the §8 table alone. No ruling asks for a change, and F7 keeps the built reading. State the reading and list the choice, the same way PiS->KO and KO->PiS are handled:

(1) Record §4, NL->PiS paragraph: add "Earlier and less plain, on the same page [NL-P3]: 'PiS trzeba pokonać, opozycja musi to zrobić i za dwa lata wydawać te pieniądze' (and NL's own page of 2022-01-31, out of tree: 'Podstawowa sprawa to pokonanie PiS'). 'Defeat' is read as an electoral aim, not one of F1's forms, so it is not read as the start (§12)."

(2) Record §12, "Left to Elias": add "NL->PiS's start: if 'pokonać PiS' is read as F1's 'remove from power', NL-P3 starts the one-way, support-blocking line on 2021-05-06, and facts 12 and 13 become one fact. No formation turns on it: Poland opens 2023-02-19, and NL holds no 2019 seat."

(3) Optionally, add one clause to fact 13's basis string, as the KO->PiS fact does: "Earlier and less plain: 'PiS trzeba pokonać' [NL-P3], 2021-05-06 - not read as the start (§12)". [NL-P3] is already in the register, so PolishDeclarationsDiagnostic's check (b) still passes, and fact 13's first tag stays [NL-P31], so the F2 check still passes. Do not put the 2022-01-31 page in a basis string unless it is first saved in tree with a register row and a SHA256SUMS digest.

### 4. §5 cuts KO-I4's sentence just before an F1-type clause; that candidate is missing from KO → PiS's start list

- **Lens:** facts - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:184

**The scenario.** §5 quotes PO's spokesman Grabiec on Polsat News's own page of 2023-09-08 [KO-I4] as 'A jeśli nie pójdą, to może uda się stworzyć chociażby rząd mniejszościowy' and stops. The bytes continue: ', który pozwoli odsunąć PiS od tych narzędzi władzy, które są nadużywane'. That is a conditional pledge to remove PiS from power, verbatim on a broadcaster's own page and earlier than fact 15's 10-12. The KO → PiS start list for Elias (line 323) names Budka (a club chairman) and Tusk 09-19, but not this. F2's words ('a leader's spoken words') probably exclude a spokesman, but the record does not say so, while it does list Budka, who is not the leader either. Fact 15 stands on polling day either way.

**The fix proposed.** Quote the full sentence in §5, and add it to the KO → PiS start item with the reason it does not date the line (a spokesman's words, and conditional).

**The skeptic's evidence.** I tried to refute this and could not. The omission is real, and it has no effect on how the formation comes out.

1. **The quote in §5 stops at the page's bold.** coalition_declarations_2023.md:182-184 quotes [KO-I4] as "A jeśli nie pójdą, to może uda się stworzyć chociażby rząd mniejszościowy" and ends there. The saved bytes in raw/declarations_2023/KO/polsatnews_2023-09-08_grabiec_konfederacja.html read: `<strong>A jeśli nie pójdą, to może uda się stworzyć chociażby rząd mniejszościowy</strong>, który pozwoli odsunąć PiS od tych narzędzi władzy, które są nadużywane - podkreślił w "Graffiti" Jan Grabiec.`
   - The cut falls exactly at `</strong>`, so the quote is accurate as far as it goes. It was written under §776, when keep-from-power words did not count.
   - The words left out use F1's own verb, "odsunąć PiS od … władzy". The record reads that verb as a line everywhere else: [TD-P11], [TD-P6], [NL-P31], [NL-P32], [KO-I12].
   - The page is Polsat News's own, dated "08.09.2023, 09:27". It calls Grabiec "rzecznik Platformy Obywatelskiej" and gives the words as direct speech after a dash.

2. **This candidate is missing from every list of earlier starts.**
   - §12, line 323 ("Left to Elias"): the KO → PiS start item names Budka on 2023-08-09 ("a club chairman's words at a press conference") and Tusk on 2023-09-19 [KO-I7]. Grabiec is not there.
   - §5, lines 178-180 ("Earlier and less plain") has the same gap.
   - So does the code: DeclaredRedLines.cs:442, fact 15's basis, "Earlier and less plain: Budka … and Tusk on Polsat News, 2023-09-19 [KO-I7] - not read as the start (§12)".
   - Outside raw/, "Grabiec" occurs only at record lines 182, 184 and 260, and "narzędzi władzy" occurs nowhere.

3. **It was not excluded on purpose.** The session's own sweep (scratchpad f12/find_KO.txt) says: "Under F1, 'odsunąć PiS od tych narzędzi władzy' is a keep-from-power aim toward PiS, hedged on feasibility ('może uda się') … the first fallback start (2023-09-08) if KO-7 is not admitted". The record does not name the speaker's role as a reason either:
   - Its header (lines 15-16) counts "its leader or an authorised spokesperson".
   - F2's wording says only "a leader's", and the record never applies that to spokespeople.
   - Budka is listed even though he is not the leader.

4. **Impact.** Fact 15 (from 2023-10-12) stands on polling day, 2023-10-15, either way. Poland opens no mid-term round (record lines 11-12: confidence rules `Unsourced`, so no round opens). The FormationSweep text prints "blocked X by A-B" without any basis text, so the pin does not depend on the wording. The only thing the start date could change is the run-up DECLARED page (GameController.CampaignDeclared.cs:190, `StandingOn(country, today)`), and only if Elias picked 09-08. So the defect is an incomplete list of choices put to Elias.

**The skeptic's corrected fix.** 1. **§5, line 184.** Quote the whole sentence: "A jeśli nie pójdą, to może uda się stworzyć chociażby rząd mniejszościowy, który pozwoli odsunąć PiS od tych narzędzi władzy, które są nadużywane". Mark it *(decoded)*: it now crosses the page's `</strong>`, and the record's header requires that mark when a quote crosses markup.

2. **§12, line 323, and §5's "Earlier and less plain" (lines 178-180).** Add Grabiec as a third candidate between Budka (08-09) and Tusk (09-19): PO's spokesman on Polsat News's Graffiti, 2023-09-08 [KO-I4]. Say that it would start KO → PiS on 2023-09-08 only if both of these hold:
   - a spokesman's words count under F2. F2's words say "a leader's", while the header's "What counts" admits an authorised spokesperson.
   - a purpose that is hedged and conditional ("może uda się", "jeśli nie pójdą") is read as F1's pledge.

3. **DeclaredRedLines.cs:442, fact 15's basis string.** Add the same candidate, e.g. "Budka … (2023-08-09), Grabiec, PO's spokesman, on Polsat News, 2023-09-08 [KO-I4], and Tusk … [KO-I7]". This is safe for the diagnostics:
   - KO-I4 is already a row of the register, so check (b) still passes.
   - The first tag stays [KO-I11], so the F2 first-tag check still passes.
   - The sweep digest's text carries no basis, so it does not move.

### 5. Two first-tag pages were modified after the dates they give facts; the record does not say so

- **Lens:** facts - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:348

**The scenario.** KONF-I4's JSON-LD reads datePublished 2023-06-20T20:42:03+02:00 and dateModified 2023-08-04T13:00:00+02:00, in both captures. Facts 3-7 (DeclaredRedLines.cs:417-426) are dated 'by that page' under F2, but the saved Q&A is the page as it stood after 07-06 and 07-13, when facts 3 and 4 are replaced. The only independent same-day source, RMF's X post of 2023-06-20T17:51Z (out of tree), gives the answer in other words. TD-P11 is the only saved source of fact 10's F1 words ('odsunąć PiS od władzy'): GazetaPrawna's PAP copy of the same day [TD-I5] lacks them. TD-P11's dateModified is 2023-07-01T01:43:25+02:00. The record notes 'never modified' for PiS's pages, so it treats modification as relevant, yet registers neither of these. If either edit changed the words, the from-dates of facts 3-7 or of fact 10's F1 shape lose their page. Polling-day lines are unchanged.

**The fix proposed.** Add the dateModified values to the KONF-I4 and TD-P11 register rows, and to §12's RMF24 verbatim item, so Elias rules with them in view.

**The skeptic's evidence.** I tried to refute this and could not. The finding's facts hold against the saved bytes, with one inaccuracy. The KONF-I4 half carries little weight. The TD-P11 half is the part that matters. It changes no game behaviour.

**What the pages say**
- KONF-I4, in both captures (`Konf/rmf24_petru-mentzen-pytania-sluchaczy.html` and the out-of-tree `CROSS/rmf24_2023-06-20_debata-petru-mentzen-rmf-fm.html`): `"datePublished":"2023-06-20T20:42:03+02:00"`, `"dateModified":"2023-08-04T13:00:00+02:00"`.
- TD-P11 (`TD/pl2050_2023-05-15_trzecia-droga-polski-2050-i-psl.html`): `"datePublished":"2023-05-15T11:36:42+02:00"`, `"dateModified":"2023-07-01T01:43:25+02:00"` (`article:modified_time` 2023-06-30T23:43:25+00:00).
- The other five Polska 2050 pages saved (03-25, 08-09, 09-17, 10-12, 10-13) were each modified 1 to 4 seconds after publishing. That includes a page published before 07-01. So TD-P11's later edit is specific to that page, not a site-wide restamp.

**What the record says**
- It records that a page or post was not modified wherever that supports a date: line 35 [PIS-P1] and line 47 [PIS-P2] ("its WordPress record: …, never modified"); line 79 [KONF-P2] and register line 388 [KONF-P3] ("isEdited false").
- It records modification for neither page that was modified:
  - Register line 348 (KONF-I4) gives only "Wtorek, 20 czerwca 2023 (20:42)".
  - The TD-P11 row quotes `datePublished` from the JSON-LD object and leaves out the `dateModified` in the same object.
  - Section 12's RMF24 verbatim item does not mention the edit date.
- Section 8 row 10 (TD → PiS, F1, from 2023-05-15) cites [TD-P11] alone, and doubt 2 says the line runs "from 2023-05-15" on that page.

**TD-P11 is the only saved source for fact 10's words**
- TD-I5 (`gp_2023-05-15_tak-dla-rzadu-z-opozycja.html`) has no "odsun" and no "od władzy". Its pledge is only "a nie z PiS".
- Searching every capture: no other 15 May page carries "odsunąć".
- If the 07-01 edit added the pledge, TD → PiS would start on PSL's own page [TD-P6] on 2023-08-10.

**The KONF-I4 half carries little weight**
- Every RMF24 capture has a dateModified exactly on the hour. RMF24's unrelated 2023-07-11 Kaczyński article carries the identical `2023-08-04T13:00:00+02:00`. That pattern points to a batch restamp across the site, not an edit to this page.
- Two pages from the same day, both earlier than 08-04, already carry the substance:
  - RMF's own X post, `created_at 2023-06-20T17:51:17Z`: "Mentzen: nie zamierzam robić koalicji z PiS; w przyszłej kadencji nie będę w koalicji z nikim".
  - WP's out-of-tree report, `CROSS/wp_2023-06-20_petru-mentzen-debata.html` (published 17:30:23Z, modified 19:42:38Z the same day): "- Nie będę robił koalicji z PiS-em - ripostował Mentzen", plus a paraphrase of the rest.
- Section 12 already states what follows if the transcript is not verbatim.
- The inaccuracy: the finding calls the X post "the only independent same-day source". It is RMF's own account, so not independent, and it is not the only one, because WP's report gives the sentence a third way.

**No effect on the game**
- The finding's "If either edit changed the words" cannot be shown from the saved pages.
- Either way, the polling-day set (lines 1, 2, 5–11, 13–15) is unchanged.
- The record's header says no Polish mid-term round opens, so the game never reads the from-dates inside 2023.

**The skeptic's corrected fix.** Add both edit dates to the record. Write them as the pages state them, with no counted day spans (the claim convention).

**KONF-I4**
- In its register row and in section 12's RMF24 verbatim item, add: "dateModified 2023-08-04T13:00:00+02:00 - the same stamp as RMF24's unrelated 2023-07-11 article, and every RMF24 page saved is stamped on the hour: a site restamp, not an edit anyone saw. Before it, the same day's own X post (17:51Z) and WP's report (17:30Z, out of tree) carry the substance, each in other words."
- Correct the finding's wording: the X post is not an independent source.

**TD-P11**
- In its register row, add the page's own `dateModified` value: 2023-07-01T01:43:25+02:00.
- In section 3, doubt 2 and section 12's "Left to Elias", add: "The saved page was edited 2023-07-01, after the date it gives; no other Polska 2050 page saved was. No other saved 15 May source carries the pledge ([TD-I5] lacks it). If the edit added it, TD → PiS's support-blocking line starts on PSL's own page 2023-08-10 [TD-P6]. No polling-day line changes."
- Optional: a dated Wayback capture of TD-P11 from before 2023-07-01, if one exists, would settle the question.

### 6. Doubt 4 ('decided by F2') rests on two unstated readings of 'the broadcaster's own page'

- **Lens:** facts - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:71

**The scenario.** The record says the 26 June opening has no qualifying page because 'Super Express is a newspaper' and RMF24 only relays. Super Express's own page [KONF-I8] presents the words as 'Najnowszy wywiad ze Sławomirem Mentzenem, gościem "Wieczornego Expressu"', its own video programme, with that programme's video embedded (uploadDate 2023-06-22T22:22:07Z). PAP's relay calls it 'wywiad dla poniedziałkowego "SE"', the print edition. RMF24 [KONF-I11] is a broadcaster's own page carrying the words verbatim, dated 06-26. Both exclusions are readings of F2's scope, and neither is among §12's items for Elias. Read the other way, facts 3-7 end on 2023-06-22 or 06-26, and Konf → TD/NL/MN restart on 2023-08-02 [KONF-P1]. Polling day is unchanged.

**The fix proposed.** Name both readings, a newspaper's own video programme and a broadcaster relaying another outlet, among §12's items left to Elias, alongside the 'always wins' reading.

**The skeptic's evidence.** I tried to refute the finding and could not. Every fact it cites checks out against the saved bytes, and §12's list does leave both readings out. One correction to its title: the two exclusions are stated in the record. What the record does not do is mark them as readings, name the programme fact behind one of them, or list either one for Elias.

RECORD (coalition_declarations_2023.md):
- Line 71: "Under F2 none of these dates the opening: Super Express is a newspaper, and the PAP and RMF24 pages relay its interview - no broadcaster's or agency's own page carries the words as said".
- Line 308, doubt 4, says "decided by F2", on the grounds that none of the pages is "the broadcaster's or the agency's own page for those words".
- The header (lines 20-22) gives both exclusions as plain consequences of F2: "So a page relaying another outlet's interview or an agency's wire - ... RMF24 relaying Super Express - a newspaper and a portal date nothing here".
- The only thing the header marks "READING, stated" is "always wins", and that reading also gets its own bullet in §12 (line 325).
- §12's "Left to Elias" list (lines 321-330) has five bullets. None covers Super Express's programme or RMF24's relay. One of the five, [KONF-I4]'s verbatim-ness, already governs the start of the same facts 3-7.

BYTES:
- Konf/se_mentzen-braun-ministrem-kultury.html:
  - The lead reads: "Najnowszy wywiad ze Sławomirem Mentzenem, gościem &quot;Wieczornego Expressu&quot;".
  - Its VideoObject has "name": "Wieczorny Express - Sławomir Mentzen cz.1" and "uploadDate": "2023-06-22T22:22:07+00:00", served from the publisher's stream.smcdn.pl.
  - The page is dated "2023-06-26 9:05".
  - The record never names the programme. Grep finds no "Wieczorn"; lines 69-70 say only "the interview's video is dated 22 June, UTC".
- CROSS/bankier-pap_2023-06-26: "Mentzen w wywiadzie dla poniedziałkowego &#34;SE&#34;", which points to the print edition.
- CROSS/rmf24_2023-06-26:
  - "Jeśli obie partie, czy to PiS, czy PO, zgodzą się realizować nasz program, to wszystko jest na stole - mówi Sławomir Mentzen w wywiadzie dla "Super Expressu"". This is word for word the same as Super Express's text.
  - The page credits "Źródło: RMF24/PAP" and is dated "Poniedziałek, 26 czerwca 2023 (08:31)".
- No pap.pl page is saved, either in the tree or in the captures.

HISTORY:
- At HEAD (§776) the table dated this same lift by Super Express's page: "2–6 | Konf → PiS, KO, TD, NL, MN | ... | 2023-06-20 | 2023-06-26 | [KONF-I4]; replaced by [KONF-I8] - the extension".
- HEAD's K-1i (3) put "the June lines and their lift" to Elias as dated by "a broadcaster's or an agency's page".
- So dropping the lift is this change's own reading of F2's restated "own page". F2 does not say it by name.

IMPACT:
- PolandTimeline now has Konf→PiS from 06-20 to 07-06, Konf→KO from 06-20 to 07-13, and Konf→TD/NL/MN from 06-20 with no end.
- Read the other way, those lines end on 2023-06-26, the page's own date. 06-22 is the video's upload date, not "that page's" date. Konf→TD/NL/MN would then restart on 2023-08-02 [KONF-P1], which is HEAD's shape.
- On polling day the same cabinet lines stand either way, so R1 and F7's acceptance test do not move.
- A Polish world opens on 2023-02-19 (15 Oct minus 8 and 26 weeks), so only a mid-term round between 26 June and 2 August could see the difference. The seated 2019 chamber gives PiS 235 of 460, so no such round is reachable.

WHY IT IS ONLY A NOTE: The record's choice is the stricter and more natural reading. F2 names only broadcasters and agencies, PAP itself calls this a print interview, and RMF24's page is a relay sourced from PAP. The finding is a completeness gap; nothing in the facts is wrong.

**The skeptic's corrected fix.** Add two bullets to §12's "Left to Elias (no formation turns on any of them)":
(a) Does a newspaper's own video programme make it "the broadcaster" for words spoken on it? [KONF-I8] presents the interview as Super Express's "Wieczorny Express" with the video embedded, while PAP [KONF-I10] calls it an interview for Monday's print edition. If it does count, the page's own date (2023-06-26) dates the opening. Whether its Q&A, laid out for print, is verbatim would then still be open, as it is for [KONF-I4].
(b) Does a broadcaster's own page count when it relays another outlet's interview from an agency wire? This is [KONF-I11]: "Źródło: RMF24/PAP", 2023-06-26 08:31.
Under either reading, facts 3-7 end on 2023-06-26 (not 06-22, which is the video's upload date) and Konf→TD/NL/MN restart on 2023-08-02 [KONF-P1]. No polling-day line changes.
Also:
- On lines 69-71, name the programme rather than only "the interview's video".
- Make doubt 4 say "decided by F2 on the two readings listed below".

### 7. The DECLARED page's slip contradicts F2 dating (predates this diff; F2 now makes the dating a standing rule)

- **Lens:** facts - **reviewer:** note - **skeptic:** minor
- **Where:** Assets/Scripts/UI/GameController.CampaignDeclared.cs:184

**The scenario.** The DECLARED head's slip says 'EACH ITEM IS FROM THE PARTY'S OWN RECORD AND DATED BY IT', and the class comment (line 18) says the same. Under F2, now in CLAUDE.md, Sweden's KD → S (SVT's live page, from 2026-09-02) and MP's rule (Sveriges Radio) are dated by broadcasters' pages. Both stand on Sweden's 2026 run-up page today. 7 of Poland's 15 facts would contradict the slip once Poland's run-up is staged. The file is not in this diff.

**The fix proposed.** Reword the slip, for example 'EACH ITEM IS DATED BY THE PARTY'S OWN RECORD, OR BY A LEADER'S WORDS ON A BROADCASTER'S OWN PAGE'.

**The skeptic's evidence.** Could not refute. Failing path, Sweden, today: Sweden's game opens at the run-up before 13 Sep 2026 (WorldClock.cs:87, :116-117), and LiveCampaignSetup.TryFor stages Sweden and Germany (LiveCampaignSetup.cs:148). So DeclaredPageAvailable (GameController.CampaignDeclared.cs:34-35) is true, and the page draws DeclaredRedLines.StandingOn(country, today) (:190) under the DECLARED head's slip (:182-187, line 184: "EACH ITEM IS FROM THE PARTY'S OWN RECORD AND DATED BY IT").

Two Swedish facts stand on that page before polling day, and neither is dated by the party's own record:
- MP's rule: DeclaredRedLines.cs:350, from D(2026,8,10) to Open. Its basis (:257) is "Helldén's own words to Sveriges Radio 2026-08-10 ([MP-I1]...)", and :254 says "MP's own record is a GAP".
- KD -> S: :353, from D(2026,9,2) to Open. Its comment (:351-352) reads "Busch's own words ... as SVT's live report quotes them ... RULED by Elias's ruling F2". The row's own keys, from SourceKeysOf, are the press keys [KD-I1] and [KD-I3] (:131-133, "the KD primary is a GAP").

The diff's own standing rule (CLAUDE.md:53) separates the two sources: "a leader's spoken words count, quoted verbatim on the broadcaster's ... own page and dated by that page ... and a date from the party's own record always wins". The diff updated that rule and SwedenTimeline's summary (DeclaredRedLines.cs:312-314) to name F2, but not this player-visible restatement. The class comment at :18 ("from its own date (the party's record, §621)") is stale the same way.

Predates the diff: git log -S shows the slip was added in 1fdd1bb3 (§657, 2026-09-29). At that commit SwedenTimeline already held MP from 2026-08-10 and KD->S from 2026-09-02, so the slip was never true for those two rows.

Broader than the finding says: Germany's page is staged too (start 2024-11-06, WorldClock.cs:113). The AfD candidacy stands from D(2024,12,7) (DeclaredRedLines.cs:385-386), dated by ZDF's report [ZDF-AFD24]. Germany's record (coalition_declarations_2025.md:47) calls it "a broadcaster's report, as K-1's KD line is SVT's".

The Polish part is latent: no Polish campaign is staged (CampaignDeclared.cs:32-33), though the count holds. 7 of 15 facts carry PolandSpokenWords (DeclaredRedLines.cs:418, 420, 422, 424, 426, 430, 442).

Why minor, not note: the false provenance claim is on pages players can open today in Sweden and Germany, and the repo's §776 review graded a latent player-visible misstatement on this same page as minor. No model outcome changes, and it need not block this change.

Two problems with the proposed fix:
- The proposed line is about 96 characters; slip lines are capped at 64 by convention (FormationSheet.cs:419-427).
- "A LEADER'S WORDS" would still be false for Germany's AfD row, where the quoted words are ZDF's own about a board decision.

**The skeptic's corrected fix.** In GameController.CampaignDeclared.cs, replace line 184 with two lines of at most 64 characters each. They must be true for every timeline the page can show (Sweden, Germany, and Poland once staged):
.Add("EACH ITEM IS DATED BY ITS SOURCE: THE PARTY'S OWN RECORD,")
.Add("OR A BROADCASTER'S OR NEWS AGENCY'S OWN PAGE THAT REPORTS IT")

Correct the class comment at line 18: "(the party's record, §621)" becomes "(dated by §621's rules, §652's ruling or ruling F2 - each fact's basis says which)".

This is a UI string change, so it owes the UI tier's bar, including a film of the DECLARED slip. No check pins the old string; grep finds it only at :184.

Optional, separate from this fix: tell Elias that Germany's AfD candidacy is dated by ZDF's report of a board decision. That is a newsroom's words, not a leader's quoted words, so standing F2 does not cover it.

### 8. The diagnostic's F2 check tests the tag letter, not the page class

- **Lens:** facts - **reviewer:** note - **skeptic:** minor
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:86

**The scenario.** The check passes a fact whenever its first tag is an [X-In] tag and it carries PolandSpokenWords. A fact first-tagged [KONF-I8] (Super Express) or [TD-I5] (GazetaPrawna's PAP copy) with the mark would pass, though F2 counts neither page. Today's three I-first facts (KONF-I4 on RMF24, KONF-I14 and KO-I11 on TVN24) are broadcasters, so nothing fails now.

**The fix proposed.** Hold the first I-tag's register publisher cell to the pages F2 counts (the broadcaster's or agency's own page), or mark F2-qualifying rows in the register and check that mark.

**The skeptic's evidence.** The finding holds. I could not refute it.

1. What the check tests. In Assets/Editor/PolishDeclarationsDiagnostic.cs, lines 88-90 read only the letter after the hyphen in the first tag:
`Match first = Regex.Match(f.Basis, @"\[[A-Z]+-([A-Z])\d+\]"); bool ownRecord = first.Success && first.Groups[1].Value == "P", spoken = f.Basis.Contains(DeclaredRedLines.PolandSpokenWords); return !first.Success || ownRecord == spoken;`
So any fact with the mark passes when its first tag is not a P tag. Nothing reads the register's publisher cell. Check (a)'s regex for §8 also stops at the `until` column, so §8's "- F2" basis notes are not compared with the mark either.

2. What the check claims. The comment at lines 84-85 says "a broadcaster's or an agency's page ([X-In]) ... a fact dated by a page of neither kind cannot be written so". The ok-line at line 92 prints "carrying F2's mark, a leader's words on a broadcaster's own page". The register disagrees. Its I rows include pages the record says F2 does not count: [KONF-I8] Super Express (line 349), [KONF-I10] Bankier.pl (PAP) (350), [KONF-I11] RMF24 relaying Super Express (351), [TD-I5] GazetaPrawna.pl (PAP) (364) and [NL-I5] GazetaPrawna.pl (PAP) (377). The record's header (lines ~19-21) says such relays "date nothing here". Check (b) accepts all of them as register rows.

3. The failing path is real, not just possible. I ported lines 86-91 to perl (scratchpad f2check.pl) and ran them on HEAD cba1bd04's PolandTimeline, using PolandExtension as the mark. All 17 facts pass. Two of them are the ones F2 overturned:
- TD -> PiS, first tag [TD-I5] (GazetaPrawna's PAP copy), marked: passes.
- NL -> Konf from 2023-08-27, first tag [NL-I5] (GazetaPrawna PAP), marked: passes.
This change re-dated or removed both facts by hand ([TD-P11]; the fact dropped). If the change had only renamed the constant, the new check would have passed both.

4. Nothing fails today. All 15 working-tree facts pass. The 7 marked facts are first-tagged [KONF-I4] (5 facts, "RMF24 (its own debate)"), [KONF-I14] (TVN24 Fakty po południu) and [KO-I11] (TVN24 Fakty). That is 7 facts on 3 distinct tags, not "three I-first facts" as the finding says, but the miscount does not change the claim. The 8 unmarked facts are all P-first.

Severity: minor rather than note. There is no wrong result now. But this bar check (CheckSuite.cs:214) prints an ok-line, and a code comment states a guarantee, about the one distinction F2 makes: a page that is the broadcaster's or agency's own versus a relay. The check cannot see that distinction, and it would not have caught the error class this change fixed.

**The skeptic's corrected fix.** Mark in the register the rows F2 counts, then check each marked fact's first tag against that set. Today that means adding "(F2)" to the publisher cell (cells[3]) of [KONF-I4], [KONF-I14] and [KO-I11]. In PolishDeclarationsDiagnostic, after the `register` set is built:

```csharp
var f2Pages = new HashSet<string>(Regex.Matches(record, @"^\| \[([A-Z]+-I\d+)\] \|[^|\n]*\|[^|\n]*\(F2\)[^|\n]*\|", RegexOptions.Multiline).Cast<Match>().Select(m => m.Groups[1].Value));
var misdated = facts.Where(f =>
{
    Match first = Regex.Match(f.Basis, @"\[([A-Z]+-([A-Z])\d+)\]");
    bool spoken = f.Basis.Contains(DeclaredRedLines.PolandSpokenWords);
    return !first.Success || (spoken ? !f2Pages.Contains(first.Groups[1].Value) : first.Groups[2].Value != "P");
}).Select(Line).ToList();
```

Also require `f2Pages.Count > 0` in the Check, so the check cannot pass with an empty set. Optionally, extend check (a)'s §8 regex to capture the basis column and require "- F2" exactly on the marked rows.

If no register mark is wanted, the minimum fix is to narrow the comment at lines 84-85 and the message at line 92 to what is actually tested: "a marked fact's first tag is a non-party [X-In] page; whether F2 counts that page is read by hand". Drop "cannot be written so" in that case.

### 9. F7's acceptance passes only because the seed-777 count is unlike history's; §12 credits F7 with a choice F7 never made

- **Lens:** ruling - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:297

**The scenario.** In nf1a, R1 on the played count (seed 777: PiS 200, KO 123, TD 78, NL 45, Konf 14) ranks KO+TD+NL first and KO+TD on NL's support second. With the same R1 lines on 2023's own seats (PiS 194, KO 157, TD 65, NL 26, Konf 18), the order flips: KO+TD on NL's support first (score 67.450), KO+TD+NL second. So the check at PolishDeclarationsDiagnostic.cs:277 passes because seed 777 gives NL 45 seats instead of 26. A vote model or seed that lands closer to history's count would make 'F7's ACCEPTANCE' FAIL without any declaration changing. Lines 300-302 call the chamber-of-record result 'the formation's own preference for the smaller cabinet, kept as built (F7)'. But that preference was never one of the 15 doubts F7 answered, F7's test does not reach that chamber, and on the played count the same formation picks the larger cabinet. The reading that 'a world that follows history' means one seed's no-policy count is stated as fact (diagnostic :232) and is not among §12's 'Left to Elias'.

**The fix proposed.** Remove '(F7)' from 'kept as built'. State that the acceptance holds on seed 777's count and that on history's own seats R1 seats KO+TD with NL outside. Add to 'Left to Elias' what 'a world that follows history' means: one seed's no-policy count, or history's seats. If history's seats, the cabinet-versus-support ranking is the thing blocking KO+TD+NL, which F7 says to decide in favour of the record. Optionally hold the check on more than one seed.

**The skeptic's evidence.** The core of the finding holds. One sub-claim does not.

CONFIRMED: the acceptance passes only on the played count.
- nf1a.log:569, chamber of record, R1 (the game's reading): "KO+TD on NL's support (222 in cabinet, 248 supported, score 67.450); the record's cabinet viable, ranked 2 of 5; best: KO+TD on NL 248 | KO+TD+NL 248".
- nf1a.log:574, check (g), the game's own reader on the record's seats: "KO+TD on NL's support".
- nf1a.log:578, played count, R1: "KO+TD+NL (... score 64.571) ... ranked 1 of 5; best: KO+TD+NL 246 | KO+TD on NL 246".
- PolishDeclarationsDiagnostic.cs:277 checks `gameCabinet.SetEquals(Keys(ofRecord))` on the played count only. On the record's seats the same lines and the same reader would fail it.
- Mechanism, CoalitionFormation.cs:446-448: `baseScore = 0.5*Cohesion + 0.3*100*cabinetSeats/total + 0.2*100*PowerOf(...)`.
  - Cohesion (:873) does not depend on seats. Seat strength and the Banzhaf power term (:189) do.
  - Rebuilt from the logged scores: NL's seat weight plus power gain about 6.3 against a cohesion cost of about 5.2 on the played count (KO+TD+NL wins by about 1). On the record's seats the gain is about 3.2 (KO+TD wins by about 2).
- The decisive difference is not mainly seed noise. COMPLETED.md:36725 (§767) gives the played count as "PiS 200, KO 123, TD 78, NL 45, Konf 14 (the record's: 194, 157, 65, 26, 18 - the lineage's misses, KO under and NL over, as the flag says)". So the pass rests on D1's flagged LOW CONFIDENCE weakness, which Elias ruled "state it, don't patch it".
- Neither the record nor the diagnostic mentions this dependence: a grep for "lineage" in both returns nothing.
- coalition_declarations_2023.md:297-302 says "No doubt blocks it", and the diagnostic states the reading as fact at :232: "(the world stepped from the start with no policy is the one that follows history)".
- The session's own premise map flagged this as an open choice. scratchpad fmap/relabels.md: "Which of these two chambers is 'a world that follows history' is your call"; fmap/declarations.md:202: "if 'a world that follows history' means the chamber of record ... The cabinet-size preference would then block the acceptance test".
- §12's "Left to Elias" lists the comparable "always wins" READING but not this one.
- F7 says "name which one in the commit", so the reading should have been stated.

REFUTED: "§12 credits F7 with a choice F7 never made".
- The 2026-10-05 report Elias answered (session transcript c8a2f04f…jsonl, line 11708), question 7: "Smaller doubts, listed in the declarations record's §12: doubts 3, 4, 6, 7 and 14, the formation's preference for smaller cabinets (it keeps NL outside), and the budget preview...". F7 answers exactly that question.
- §776's owed list says the same: scratchpad COMPLETED.pre3.md:37176, "The formation's cabinet-size preference".
- So "kept as built (F7)" is the right citation, given the installed reading. Removing "(F7)" would be wrong.

ALREADY STATED in the record:
- The seed: the §12 table header reads "seed 777".
- The chamber-of-record outcome, NL outside: lines 300-301.
- The fix's second item is therefore mostly redundant.

MITIGATING:
- "A Polish game's own 2023 election" plainly points to the game's count.
- The same session installed F4 at PresidentialReferenceWorld.cs:18-19 with "the world that follows history" as the record's state on the eve, on which the game computes its own result. That can be reconciled with the played-count reading.
- If the count moves, the check fails loudly.
- So this is an unflagged ruling-reading and dependence, not wrong runtime behaviour today.

**The skeptic's corrected fix.** Keep "(F7)". Elias's question 7 put "the formation's preference for smaller cabinets (it keeps NL outside)" to F7 by name.

In coalition_declarations_2023.md §12, after "F7's acceptance holds", add one READING sentence. Write it qualitatively, with no transcribed scores:

"READING, stated: 'a world that follows history' is read as a Polish game's own count on a world stepped from the start with no policy. The acceptance holds there because that count carries D1's flagged lineage misses (KO under, NL over, §767), which give NL the pivotality to outscore the KO+TD minority. On the record's own count the same lines, with the game's reader, seat KO+TD with NL outside (the table; check (g))."

Add a bullet to "Left to Elias":

"Which world F7's test means: the game's own count (as installed: it passes, and the cabinet-size preference is kept as built), or the record's count. If the record's count, the formation's preference for the smaller cabinet is the doubt that blocks KO+TD+NL. F7 then decides it in favour of the record (NL in the cabinet), and a formation change is owed and named in the commit. On either reading, correcting the lineage misses would also fail the check."

In PolishDeclarationsDiagnostic.cs:232, change the parenthetical from a statement of fact to "(READ AS: the world stepped from the start with no policy - the reading §12 states and puts to Elias)".

Holding the check on more seeds is optional and secondary: the dependence is the vote model's systematic miss, not mainly seed noise.

### 10. The F2 check reads only the tag letter, so it passes exactly the relayed pages F2 excludes

- **Lens:** ruling - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PolishDeclarationsDiagnostic.cs:84

**The scenario.** The check only asks whether a fact's first tag is [X-Pn] or [X-In]. Every page F2 excludes also carries an I tag: [TD-I5] (GazetaPrawna's PAP copy), [NL-I5] (GazetaPrawna/PAP), [NL-I6] (WP), [KONF-I8] (Super Express), [KONF-I10] (Bankier/PAP), [KONF-I11] (RMF24's relay), [KONF-I19] (Do Rzeczy), [TD-I10] (naTemat). Put back HEAD's TD -> PiS fact (first tag [TD-I5]) or its NL -> Konf support-blocking fact (first tag [NL-I5]) with PolandSpokenWords appended, and the check prints 'ok F2: ... a leader's words on a broadcaster's own page'. Those are the two F2 violations this change removed. The comment at :85 says such a fact 'cannot be written so', and the class summary (:29) repeats the claim. The check also never compares a fact's From date with its first tag's date.

**The fix proposed.** Key the check on the register's publisher column (or an explicit list of qualifying broadcaster or agency tags such as KONF-I4, KONF-I14, KO-I11), and compare From with the register's 'page's own date'. Otherwise, reword the check and comment to claim only what is actually tested.

**The skeptic's evidence.** I could not refute it. The failing path is real.

THE CHECK (PolishDeclarationsDiagnostic.cs:86-91)
- `Regex.Match(f.Basis, @"\[[A-Z]+-([A-Z])\d+\]")` captures only the letter of the first tag. Then `ownRecord = letter == "P"`, `spoken = Basis.Contains(PolandSpokenWords)`, and a fact is misdated iff `!first.Success || ownRecord == spoken`.
- So a marked fact passes whenever its first tag is any non-P tag. The check never reads f.From or f.Until.

WHAT THE CHECK CLAIMS
- :85: "a fact dated by a page of neither kind cannot be written so".
- :29: marked facts are dated by "a broadcaster's" page.
- :92, the printed ok line: "a leader's words on a broadcaster's own page".

THE REGISTER (coalition_declarations_2023.md, `| id | URL | publisher | page's own date |`)
The I class is every non-party page, and it includes the pages F2 excludes:
- [TD-I5] GazetaPrawna.pl (PAP)
- [NL-I5] GazetaPrawna.pl (PAP)
- [NL-I6] Wirtualna Polska
- [KONF-I8] Super Express
- [KONF-I10] Bankier.pl (PAP)
- [KONF-I11] RMF24
- [KONF-I19] Do Rzeczy
- [TD-I10] naTemat.pl
- also Rzeczpospolita, Interia, OKO.press, Wprost, Krytyka Polityczna

The tag letter cannot tell [KONF-I4] (RMF24's own debate) from [KONF-I11] (RMF24's relay of Super Express).

SIMULATED
A perl replica of the check is at scratchpad/skeptic_f2firsttag/sim.pl.
- Working tree: 15 facts, 7 marked, misdated=0. The current data is right.
- HEAD's 17 facts, with PolandExtension read as the mark: misdated=0, so the check prints ok. That includes:
  - fact 13, TD->PiS: first tag [TD-I5], GazetaPrawna (PAP), marked, passes;
  - fact 17, NL->Konf, blocks=true: first tag [NL-I5], GazetaPrawna (PAP), marked, passes.

These are exactly the two F2 violations this change removed. HEAD's June lift (Until 2023-06-26, resting on [KONF-I8]/[KONF-I10]/[KONF-I11]) is invisible to the check too, because Until is never examined.

NO OTHER GUARD
- Check (a)'s §8 regex reads only party, other, blocksSupport, oneWay, from and until. It never reads the basis column with its "- F2" notes, so if the array and §8 are reverted together, (a) still passes.
- Check (b) only requires that a tag is in the register, and [TD-I5] and [NL-I5] are still register rows.
- A grep finds no other reader of PolandSpokenWords.
- F7's acceptance test does not move: the TD->PiS dating does not change who governs, and NL-Konf's support half is already blocked by the derived line (§10).

SEVERITY
Minor, not a defect. Nothing in the game or the current data is wrong. The issue is a provenance guard, and its comments, that claim the very property F2 rules on but test it only by proxy (CLAUDE.md: "is the evidence the thing itself and not a proxy").

**The skeptic's corrected fix.** Either option fixes it.

(1) Narrow the claims at :29, :84-85 and the ok text at :92 to what is tested. For example: "a fact without F2's mark is first-tagged by a party's own page [X-Pn]; a fact with it, by a non-party page [X-In]. Whether that page is the broadcaster's or agency's own, and its date, is the record's reading, not checked here."

(2) Make the check test F2.
- Hold each marked fact's first tag to a named set of qualifying pages. Use a dedicated register column, or a set the diagnostic holds (today KONF-I4, KONF-I14, KO-I11), not the publisher name: "RMF24" covers both I4 and the I11 relay. HEAD's [TD-I5] and [NL-I5] would then fail.
- Compare each fact's From with its first tag's "page's own date" cell. Containment of yyyy-MM-dd, dd.MM.yyyy or "d <Polish genitive month> yyyy" is enough, and all 15 current facts match (scratchpad/skeptic_f2firsttag/datecheck.pl).
- Require every non-Open Until to equal the From of a later fact for the same pair. That would also have caught HEAD's June 26 lift, which rested on relayed pages.

### 11. F2's 'party's own record always wins' was never applied to KO -> PiS: KO's own record was not searched, and no gap is stated

- **Lens:** ruling - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:176

**The scenario.** The header's reading (line 22) is that the party's own record dates a declaration wherever it carries the same words earlier. For Konfederacja's F2 facts the own record was checked: [KONF-P2] (2023-07-06) has no PO or Tusk words, and [KONF-P1] is later. For KO -> PiS (F2, 2023-10-12, DeclaredRedLines.cs:441) the register has no [KO-Pn] row, and the sweep's KO/SOURCES.tsv lists only press and broadcaster pages. Neither the record nor fetch_log.md says KO's own record was searched or is a GAP, as Sweden's record does for KD. If a PO/KO page or a Tusk post said 'odsunąć PiS od władzy' before 10-12, the record's own reading would date the line from it, and nothing would show that. No polling-day change.

**The fix proposed.** Search KO's own record (PO/KO sites, Tusk's own posts) for the pledge before 2023-10-12, or state in §5 and the header that KO's own record is a GAP. Add this to §12's KO -> PiS item so 'always wins' is applied visibly to all three F2-dated lines.

**The skeptic's evidence.** I could not refute it. The record says the rule applies to every F2 fact, but it never applies it to KO -> PiS and does not state the gap.

1. **The rule as the record states it.** Record lines 22-25 give the READING: "always wins" means the party's own record dates a declaration where it carries the same words earlier. The F2 facts are listed as "the June lines…, Konfederacja → KO (TVN24's) and KO → PiS (TVN24's)".
2. **What §5 cites for KO -> PiS.** At line 176 the fact (DeclaredRedLines.cs:441) is dated 2023-10-12 from [KO-I11], Tusk on TVN24's Fakty. The only earlier candidates named are broadcaster pages: Budka on TVN24 (2023-08-09) and Tusk on Polsat (2023-09-19, [KO-I7]). §12's "KO → PiS's start" item (line 323) lists only those two. Neither place says anything about KO's own record.
3. **No KO own-record source anywhere.**
   - The register's KO rows are KO-I3, I4, I7, I8, I10, I11 and I12. There is no [KO-Pn], while every other declaring party has own-page rows (PIS-P, KONF-P, TD-P, NL-P).
   - The out-of-tree sweep's KO/SOURCES.tsv holds only Do Rzeczy, GazetaPrawna, OKO.press, Polsat News, RMF24, TVN24 and Wprost.
   - A grep for platforma.org and koalicjaobywatelska.pl across the whole sweep finds nothing.
4. **No gap stated.** fetch_log.md says nothing about a KO search. In the record, "searched" appears only for Morawiecki's candidacy, and the record has no GAP statement for any party. By contrast, Sweden's record writes "KD primary: GAP", and K-1i writes "MP's own record a GAP".
5. **The check that was dropped.** HEAD's header applied §652's condition fact by fact: "on this record none does - TD's own record [TD-P8] is later…". The new header drops that sentence. The own record is still applied visibly for Konfederacja: line 92 says toward KO "only 2023-08-02 [KONF-P1]", and line 329 says "Konfederacja's lines start on its own record (2023-07-06, 2023-08-02)". KO gets no such sentence.
6. **The session's own verification looked only at saved pages.** Its KO pass (scratchpad f12/find_KO.txt) says: "No saved KO own-record page carries the pledge, so the broadcaster's date stands."
7. **A known KO own-record item was never fetched.** Tusk's own X post of 2023-06-09 reads "Z PiS możemy konsultować warunki ich kapitulacji…". It is held only on Interia's page (out of tree, CROSS/interia_2023-06-09_tusk-warunki-kapitulacji-pis.html).
   - §11 (lines 271-272) still files it under the pre-F1 heading "Not a refusal".
   - It is not weighed under F1 and is not in §12's start list.
   - The find agent says it fails only because "the post is not saved", and that if admitted it would move KO -> PiS to 2023-06-09.
   - Mentzen's post was fetched as [KONF-P2] through X's syndication endpoint; Tusk's never was.
8. **The diagnostic cannot catch this.** PolishDeclarationsDiagnostic's new F2 check only tests the form of a fact's first tag (P without the mark, I with it). With no [KO-Pn] row it passes without testing anything.

**Impact: none on behaviour.**
- Any start on or before 10-12 still stands on polling day (10-15).
- Before 10-12, PiS -> KO (from 09-08) already blocks PiS+KO cabinets.
- In the 2019 chamber PiS governs alone with a majority and needs no KO support, so no mid-term round turns on the date.
- FormationSweepDiagnostic samples only Sweden's timeline edges.

What is at stake is the date of a SOURCED, DECLARED fact, which may be months late, and an F2 clause applied to two of the three F2 facts but not this one.

**Severity: minor.** It is more than a note because a known own-record candidate sits unweighed under the new ruling.

**The skeptic's corrected fix.** A text-only fix is enough unless the date moves.

**Minimum fix:**
1. In §5 (after line 180), in the header's F2 paragraph and in fetch_log.md, say that KO's own record was not swept and is a GAP. This covers platforma.org, koalicjaobywatelska.pl (the sweep holds only a 2023 candidates page) and PO's and Tusk's own X posts. Then say that KO -> PiS's 2023-10-12 date stands by F2 on the saved record only.
2. In §12's "KO → PiS's start" item, add the own-record candidate: Tusk's X post of 2023-06-09, "Z PiS możemy konsultować warunki ich kapitulacji". It is held only on Interia's page and the post itself was not fetched. If it is read as an F1 pledge, it would start the line on 2023-06-09, and F2's "always wins" would then date it.
3. In §11, move that tweet out of "Not a refusal or not a candidacy" into the F1 restatements list, with this status.

**Better fix:**
1. Fetch Tusk's post through X's syndication endpoint (the way [KONF-P2] was fetched).
2. Search PO's and KO's own pages and X posts for a keep-PiS-from-power pledge before 10-12.
3. If one qualifies, date fact 15 from it and register it as [KO-P1]. Change §8 row 15 and DeclaredRedLines.cs:441 together, so check (a) still holds the record's table to the array.

**What does not move:**
- No re-measure is needed: polling-day lines are unchanged, so R0-R3 and F7's acceptance stay the same.
- No re-pin is needed: FormationSweepDiagnostic samples only Sweden's timeline edges.

### 12. KO -> PiS's earlier start: §11 and §12 contradict each other about Budka's TVN24 page, and both candidate pages are quoted by their weaker words

- **Lens:** ruling - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:271

**The scenario.** §11 (line 271) files Budka's 2023-08-09 'prawdziwą alternatywą' under 'Restatements of KO -> PiS under F1, on pages that do not date it'. §12 (line 323) offers Budka at the same launch on TVN24 as a page that 'would start it earlier'. The saved page (sweep KO/tvn24_2023-08-09_budka_inauguracja_kampanii.html, '9.08.2023, 07:53', 'Źródło wideo: TVN24') is TVN24's own and carries both, dash-quoted: 'Października 15. pogonimy Kaczyńskiego' and '...jest prawdziwą i jedyną alternatywą, żeby zatrzymać tę złą władzę'. The only F2 question is whether a club chairman counts as 'a leader'. §12 puts the person-aimed 'pogonimy Kaczyńskiego' to Elias, not the plainer F1 wording. [KO-I7] (Polsat News's own page, 19.09.2023) likewise carries, dash-quoted, 'jeśli naprawdę pogonimy ludzi, którzy z rządzenia ojczyzną zrobili brudny biznes', which is plainer than the quoted 'żeby pogonić to zło' (lines 179, 323).

**The fix proposed.** Take Budka's 2023-08-09 words out of §11's 'pages that do not date it' and say the page is TVN24's own. Put the strongest F1 wording from each candidate page into §12's KO -> PiS item, and state the question of whether a club chairman counts as a leader.

**The skeptic's evidence.** I could not refute it. Every factual claim in the finding matches the files.

**What the record says (coalition_declarations_2023.md)**
- Lines 270-271 (§11) put Budka's words under "Restatements of KO → PiS under F1, on pages that do not date it": Budka's "prawdziwą alternatywą" (2023-08-09, earlier than the start, §12).
- Line 323 (§12) says: "Budka at KO's campaign launch (TVN24, 2023-08-09, "pogonimy Kaczyńskiego"; a club chairman's words at a press conference) or Tusk on Polsat News (2023-09-19, "żeby pogonić to zło" [KO-I7]) would start it earlier if read as F1's pledge."
- Lines 178-180 (§5) call both of these "Earlier and less plain".
- Everywhere else in the record, "a page dates" is used in F2's sense: lines 22, 71, 156 and 313.

**Why §11 and §12 disagree.** Both sections are about the same page.
- §11 says the page does not date the line, yet already reads its words as F1.
- §12 says the page would date the line, but only if its words are read as F1.
- §5 says the words are "less plain".
- Read either way, the record is inconsistent with itself.

**What the saved page shows** (sweep `KO/tvn24_2023-08-09_budka_inauguracja_kampanii.html`, decoded with perl)
- It is on tvn24.pl, dated "9.08.2023, 07:53", with "Źródło wideo: TVN24". The source line reads "Źródło: TVN24, PAP".
- It carries two dash-quoted lines:
  - "- … Października 15. pogonimy Kaczyńskiego. … - mówił."
  - "- … Dlatego Koalicja Obywatelska jest prawdziwą alternatywą dla rządu PiS , jest prawdziwą i jedyną alternatywą, żeby zatrzymać tę złą władzę - ocenił Budka."
- The lead calls him "szef klubu Koalicji Obywatelskiej".
- A TVN24 page co-credited to PAP is not an excuse for §11's wording. The record already uses `KO/tvn24_2023-10-12_relacja_na_zywo.html` ("Źródło: tvn24.pl, PAP") for Gawkowski, a club chair, restating NL → PiS (line 171).
- So F2's own text admits this page. What stays open is whether a club chairman counts as "a leader". The record's own practice on club chairs is mixed: Gawkowski and Terlecki are used, while Trela is dropped at line 264.

**The quote §12 chose for Budka.** "pogonimy Kaczyńskiego" is aimed at a person. Reading it as a pledge against PiS needs the leader-keys-party premise, which §12's first bullet (line 322) leaves open for PiS → KO. The other sentence on the page names "rządu PiS" and needs no such premise. The session's own verification says exactly this (scratchpad `f12/find_KO.txt`, note for KO-7): "The second sentence names PiS's government, so no leader-for-party premise is needed… Budka is the club chairman… If either is not admitted, the fallbacks are…". `f12/verify_KO-7.txt` also says: "whether a club chairman counts as 'a leader' under F2 is a ruling question." §12 asks Elias about the person-aimed words instead, and frames the condition as how to read F1.

**[KO-I7]** (`raw/.../KO/polsatnews_2023-09-19_tusk_gosc_wydarzen.html`, "19.09.2023, 19:50") carries both lines dash-quoted:
- "- Polska będzie normalna, … jeśli naprawdę pogonimy ludzi, którzy z rządzenia ojczyzną zrobili brudny biznes . W każdej dziedzinie życia - mówił dalej." The sentence before it reads "PiS i jego ludzie zarabiają te pieniądze".
- "Jestem tutaj po to, żeby pogonić to zło…"
- The verifier quoted the first one first. Calling it "plainer" is a judgment call, so this half of the finding is the weaker one.

**Related, and outside this finding.** Line 4 says the pages §12's doubts lean on are stored in tree, but this page is out of tree only (line 179 says so, and it is not in the register).

**Severity:** minor. Only the record is affected. KO → PiS stands on polling day from 08-09, 09-19 or 10-12, so R1 and F7's acceptance result do not change. Still, a question left for Elias to rule on is put to him with the weaker words and the wrong condition.

**The skeptic's corrected fix.** **§11 (lines 270-271).** Take Budka's 2023-08-09 words out of "on pages that do not date it". Either list them under a heading such as "not read as the start (§12)", or say plainly that the page is TVN24's own (TVN24 video; "Źródło: TVN24, PAP") with both lines dash-quoted. Do not call them an F1 restatement while §5 and §12 leave the F1 reading open.

**§12 (line 323).** Rewrite the KO → PiS item so that:
- For Budka, it quotes "jest prawdziwą i jedyną alternatywą, żeby zatrzymać tę złą władzę" (aimed at "rządu PiS", no leader premise), and gives "pogonimy Kaczyńskiego" as the leader-keyed reading tied to the PiS → KO item's premise.
- It states Budka's open condition outright: whether a club chairman's press-conference words, on the broadcaster's own page, count as "a leader's spoken words" under F2.
- For Tusk on [KO-I7], it quotes "jeśli naprawdę pogonimy ludzi, którzy z rządzenia ojczyzną zrobili brudny biznes" next to "żeby pogonić to zło". Tusk's standing and the page both qualify, so the only open question for him is the F1 reading.

**§5 (lines 178-180).** Align its "Earlier and less plain" wording with these changes.

**Register.** If §12 keeps leaning on Budka's page, register it in tree, as line 4 promises.

### 13. The NL -> Konf item leaves out the candidates aimed at Konfederacja itself, including one on NL's own record

- **Lens:** ruling - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:159

**The scenario.** §4 (159-161) and §12 (324) show Biedroń's Łódź page (TVN24, 2 Sept 2023 13:18, 'Źródło: TVN24') only through 'My tę czarno-brunatną koalicję zatrzymamy', read as aimed at the coalition. The same page has Biedroń, dash-quoted: 'Po pierwsze, musimy być tarczą przeciwko Konfederacji', aimed at Konfederacja itself. NL's own page of the Częstochowa rally (sweep NL/lewica_org_20230826_wiec_czestochowa.html, datePublished 2023-08-26T18:22:49Z) carries Biejat (co-chair of Razem, on the Lewica list): 'Pokonamy ich! Zatrzymamy tę brunatną falę' and 'Trzeba zatrzymać brunatną falę skrajnej prawicy ... I zatrzymamy to!'. Being NL's own record, that page raises no F2 question. If read as F1, NL -> Konf becomes one-way and support-blocking from 2023-08-26 on NL's own record. Then §4's 'its support half has no page F2 counts' and doubt 8's 'decided by F2' no longer hold. No formation turns on it (the derived NL-Konf line is support-blocking both ways).

**The fix proposed.** List both in §12's NL -> Konf item, with Biejat's own-record words first since they would date the line. Otherwise, state the standard by which 'stop / defeat / be a shield against' is not F1.

**The skeptic's evidence.** I could not refute it. The words are in the saved bytes, the record leaves them out, and the session's own sweep had already flagged them as Elias's choice.

THE RECORD (coalition_declarations_2023.md):
- l.142: "NL → Konf — cabinet-blocking from 2023-07-05, open; its support half has no page F2 counts".
- l.159-161: "Not read as F1's toward Konfederacja: Biedroń ... 'My tę czarno-brunatną koalicję zatrzymamy' - a pledge against the PiS–Konfederacja coalition, not toward Konfederacja itself (§12)".
- l.312, doubt 8: "decided by F2: its words (2023-08-27, 2023-08-31) stand only on PAP copies and a portal".
- l.324: "Biedroń's pledge against the PiS–Konfederacja coalition ... is not read as a line toward Konfederacja itself."
- A grep for "tarcz", "Biejat" and "Częstochow" finds nothing in the record, in DeclaredRedLines.cs (the l.440 basis says the same) or in the K-1i row.

THE BYTES:
1. TVN24 Łódź page (out of tree, NL/tvn24_st7322187_konwencja_lodz.html). Its SHA 789facee…4777 matches NL/SHA256SUMS. It carries "2 września 2023, 13:18 Źródło: TVN24" and datePublished 2023-09-02T11:18:55Z. The same page has "Biedroń wymienił najważniejsze zadania... - Po pierwsze, musimy być tarczą przeciwko Konfederacji. Po drugie, w tym wyścigu musimy zająć trzecie miejsce." This is dash-quoted and aimed at Konfederacja itself. The lead repeats it. So the record's only stated reason, "not toward Konfederacja itself", does not cover the page.
2. NL's own Częstochowa page (out of tree, NL/lewica_org_20230826_wiec_czestochowa.html). Its SHA 9f100db0…5d9d matches. datePublished is 2023-08-26T18:22:49+00:00. The party's title is "15 października wyborcy zdecydują czy przyszły rząd współtworzyć będzie Lewica czy Konfederacja". Biejat says: "– 15 października zdecydujemy czy przyszły rząd tworzyć będzie Lewica czy Konfederacja. Zwycięstwo Konfederacji oznacza rządy skrajnej ... prawicy ... Pokonamy ich! Zatrzymamy tę brunatną falę ... – mówiła posłanka i współprzewodnicząca partii Razem Magdalena Biejat". The record's HEAD doubt 8 counted Razem's co-chairs through NL's own pages.
   - Correction to the finding: "Trzeba zatrzymać brunatną falę skrajnej prawicy ... I zatrzymamy to!" is ZANDBERG's ("– mówił ... współprzewodniczący partii Razem Adrian Zandberg"), not Biejat's.
   - Caveat the finding omits: this page alone carries dateModified 2026-01-29T17:26:47Z, and its images sit under uploads/2026/01. The bytes therefore do not prove the 2023 text.

THE SESSION'S OWN INPUTS (scratchpad f12/):
- find_NL.txt NL-22 calls the Częstochowa words "a BORDERLINE F1 case ... If the owner reads it as keeping Konfederacja out of government, the NL->Konf one-way line starts 2023-08-26".
- NL-28: "'musimy być tarczą przeciwko Konfederacji' (also not F1 in terms)".
- verify_NL-26.txt: "NL's own site carries pledges of the same kind, aimed at Konfederacja itself, on 2023-08-26".
- None of this reached the record. Its unstated filter (the words must name power or government) is also stricter than the one it uses at l.323. There it lists KO → PiS's "pogonimy Kaczyńskiego" and "żeby pogonić to zło" as Elias's choices, and those name no power either.

NO FORMATION EFFECT (this confirms the finding's own caveat):
- DeclaredRedLines.ForDateSourced (l.483-491) always builds DerivedRedLines.From first, then appends the declared lines.
- DerivedRedLines.From (CoalitionFormation.cs l.951-959) draws a symmetric line, support-blocking past SocialGap 5.0.
- The PolandParties galtan values (the 4th constructor argument) are NL 1.75 and Konf 8.41, a gap of 6.66. So NL–Konf blocks support both ways whatever NL → Konf's declared shape. F7's acceptance test does not depend on it.

What fails: the record misdescribes the candidate set for a choice it leaves to Elias, and its support-half claim does not hold if the own-record words are read as F1.

**The skeptic's corrected fix.** Rewrite §4 (l.159-161) and §12's NL → Konf item (l.324) to list the words aimed at Konfederacja itself, own-record first:
- NL's own Częstochowa page (published 2023-08-26T18:22:49Z; say that it was modified 2026-01-29, so the bytes are a 2026 revision). Give its title "15 października wyborcy zdecydują czy przyszły rząd współtworzyć będzie Lewica czy Konfederacja", Biejat's (Razem co-chair) "Pokonamy ich! Zatrzymamy tę brunatną falę", and Zandberg's (not Biejat's) "Trzeba zatrzymać brunatną falę skrajnej prawicy ... I zatrzymamy to!".
- Biedroń's "Po pierwsze, musimy być tarczą przeciwko Konfederacji" on the same TVN24 page of 2023-09-02.

Then do one of these two:
(a) State the standard that keeps them out of F1: a pledge must name power or government, and "stop the wave / defeat / be a shield against" does not. Then square that with l.323, which offers "pogonimy"/"pogonić" for KO → PiS on a looser standard.
(b) Name the reading for Elias: if read as F1, NL → Konf is one-way and support-blocking from 2023-08-26 on NL's own record. In that case §4's "its support half has no page F2 counts" and doubt 8's "decided by F2" must say the question is F1's reading, not F2's.

No code or formation change is needed: the derived NL–Konf line blocks support both ways. If the Częstochowa page is cited, bring it into the tree with a tag and digest as [NL-P31] and [NL-P32] were, or mark it "out of tree" as §5 marks Budka's page. The Łódź page has the same gap: §4 and §12 already rely on it with no tag and no out-of-tree mark.

### 14. Two pages §12's 'Left to Elias' items rest on are not held in the repo, against the header's own rule

- **Lens:** ruling - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:4

**The scenario.** The header says the register's sources include 'the pages §12's doubts lean on', stored byte for byte under raw/declarations_2023/. The new KO -> PiS and NL -> Konf items rest on TVN24 2023-08-09 (Budka) and TVN24 2023-09-02 (Biedroń, Łódź). Both exist only in the out-of-tree sweep and have no register tag; fetch_log.md brought in only [NL-P31] and [NL-P32]. Check (c) in PolishDeclarationsDiagnostic covers only registered files, so it still passes. Elias is asked to rule on words that are not in the repo.

**The fix proposed.** Tag both pages, copy them into raw/declarations_2023/KO and NL, and add their digests to SHA256SUMS.txt, as was done for [NL-P31] and [NL-P32]. Otherwise, narrow the header's claim.

**The skeptic's evidence.** I could not refute any step, and the finding understates the problem: three pages, not two.

1. The record makes the claim twice.
- coalition_declarations_2023.md:4-5 (unchanged from HEAD): "Every source this record's register lists - the sources the facts and §§1–11 cite by its tags, and the pages §12's doubts lean on - is stored byte for byte under `raw/declarations_2023/<declarer>/`".
- raw/declarations_2023/fetch_log.md:18 says the same: "Since §776's review they include the pages §12's doubts lean on."

2. §12's "Left to Elias" list leans on untagged pages.
- :323, KO → PiS: "Budka at KO's campaign launch (TVN24, 2023-08-09, "pogonimy Kaczyńskiego"…)".
- :324, NL → Konf: "Biedroń's pledge … (TVN24's own page, 2023-09-02)".
- A third item has the same problem and the finding missed it. :326-327 (the RMF24 verbatim question) leans on "RMF's own post of the same moment … ("nie zamierzam robić koalicji z PiS; w przyszłej kadencji nie będę w koalicji z nikim")".
- §4 (:159-160) and §5 (:178-179) cite the two TVN24 pages without a tag. §5 even writes "out of tree" but gives no path.
- §11 (:271) also files Budka's 2023-08-09 page under "Saved and read, dropped … the files stay in the captures folder". So the same page is both a dropped finding (out of tree) and a page a doubt leans on (in tree).
- All three items are new in this change. HEAD's copy of the record contains none of "pogonimy", "czarno", "Łódź", "nie zamierzam robi" or "Matoga".

3. The bytes are out of tree only. They exist in PoliSim-captures/sources/poland_declarations_2023/, each with its digest in that folder's checksum file:

| Captures path | SHA-256 starts | What the page carries |
|---|---|---|
| `KO/tvn24_2023-08-09_budka_inauguracja_kampanii.html` | 84290bb2 | datePublished 2023-08-09T05:53:35Z; "Października 15. pogonimy Kaczyńskiego." |
| `NL/tvn24_st7322187_konwencja_lodz.html` | 789facee | datePublished 2023-09-02T11:18:55Z; "My tę czarno-brunatną koalicję zatrzymamy - zapowiedział." |
| `Konf/x_rozmowa-rmf_1671214148456251392_syndication.json` | a14369ee | "Mentzen: nie zamierzam robić koalicji z PiS; …" |

- None of them is under raw/declarations_2023/.
- None has a register row.
- No path, URL or tag for any of them appears in ElectionsData/, Assets/, CLAUDE.md or POLISIM_FEATURE_LIST.md.
- In tree, a search with entities decoded, tags stripped and \u escapes decoded finds "nie zamierzam robi" nowhere. The in-tree [KONF-I4] page has only "Zobacz wpis na X Opracowanie: Jan Matoga".

4. What the change brought in. fetch_log.md's diff adds only [NL-P31] and [NL-P32], and SHA256SUMS.txt gains only those two lines.

5. No check catches it. PolishDeclarationsDiagnostic.cs:95-111, check (c), walks only the register rows matched by `^\| \[[A-Z]+-[A-Z]+\d+\] \|`, so it passes.

6. There is precedent. Reviews/2026-10-04_s776_pl_declarations.md finding 3 was confirmed and graded minor by both reviewer and skeptic: "Doubts quote pages that are not registered or held in tree, contrary to the header … a ruling option put to Elias rests on an unregistered page". The fix then was to register the five pages and add the clause "and the pages §12's doubts lean on" to the header. This change brings the same class back.

7. The counter-reading does not hold. One could argue "§12's doubts" means only the numbered 1-15. But §12's own title includes "what is left", and at HEAD §12 was titled "Doubts — what the record leaves to Elias". These items are questions put to Elias for a ruling, which is exactly what finding 3 covered.

8. Why minor. No fact, digest, check or formation depends on these pages; the record itself says "no formation turns on any of them". What is wrong is a false provenance claim in a SOURCED record, and a ruling question whose words cannot be read from the repo.

**The skeptic's corrected fix.** Register all three pages, not two, as §776's review did for the doubts' pages.

1. Give each page a tag and a register row. Use the next free numbers: the highest in use are KO-I12, NL-I12 and KONF-I25.
   - [KO-I13]: TVN24, https://tvn24.pl/polska/wybory-parlamentarne-2023-koalicja-obywatelska-zainaugurowala-swoja-kampanie-wyborcza-borys-budka-jestesmy-gotowi-do-najwazniejszego-od-1989-roku-boju-o-przyszlosc-polski-st7284534, datePublished 2023-08-09T05:53:35Z.
   - [NL-I13]: TVN24, https://tvn24.pl/wybory-parlamentarne-2023/wybory-parlamentarne-2023-konwencja-nowej-lewicy-w-lodzi-st7322187, datePublished 2023-09-02T11:18:55Z.
   - [KONF-I26]: @Rozmowa_RMF's X post id 1671214148456251392, through the syndication endpoint (URL in the captures' Konf/SOURCES.tsv row 7), with its created_at.
2. Copy each file byte for byte from PoliSim-captures/sources/poland_declarations_2023/ into raw/declarations_2023/KO, NL and Konf.
3. Add the digests (84290bb2…, 789facee…, a14369ee…) to SHA256SUMS.txt and to the register rows.
4. Tag the citations:
   - §4 (:159-160);
   - §5 (:178-179), dropping "out of tree";
   - §11 (:271), which also needs an exception to "the files stay in the captures folder";
   - §12 (:323, :324, :326-327).
5. Extend fetch_log.md's "Added 2026-10-05" paragraph to name the three pages.

Check (c) then holds all three to their digests with no code change.

Fallback, if they are to stay out of tree: mark each citation "out of tree: PoliSim-captures/sources/poland_declarations_2023/<path>", as §11 does for the PCh24 page. Then narrow both the header (coalition_declarations_2023.md:4) and fetch_log.md:18 so they no longer claim the pages §12's doubts lean on are held in tree.

### 15. CLAUDE.md's standing F2 rule drops 'with §652's condition' and so states the 'always wins' reading the record leaves open

- **Lens:** ruling - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/CLAUDE.md:53

**The scenario.** Elias's F2 opens 'Accepted, with §652's condition.' The standing rule keeps only '... and a date from the party's own record always wins'. The record reads 'always wins' as §652's condition (the own record wins where it carries the same words EARLIER, line 22) and lists that reading for Elias (line 325). A later session applying CLAUDE.md literally would date Konf -> TD, NL and MN from Konfederacja's own page [KONF-P1] of 2023-08-02 rather than RMF's 2023-06-20, contradicting the record and PolandTimeline.

**The fix proposed.** Carry 'with §652's condition' in the standing rule, noting that 'always wins' is read as that condition and is open for Elias, as the PolandSpokenWords summary in DeclaredRedLines.cs already does.

**The skeptic's evidence.** I could not refute it. The standing rule leaves out a condition that is part of the ruling, and the build depends on that condition.

- **CLAUDE.md:53 (working tree):** "...**and (F2, 2026-10-05, §780) a leader's spoken words count, quoted verbatim on the broadcaster's or news agency's own page and dated by that page - a journalist's paraphrase never counts, and a date from the party's own record always wins.**" Neither CLAUDE.md nor docs/archive/CLAUDE_HEAD_LONGFORM.md mentions §652 or its condition anywhere.
- **The ruling opens with the condition:** "Accepted, with §652's condition."
  - §652 (COMPLETED.md:34592) says: "If a saved MP publication is earlier, use that instead."
  - §776 (COMPLETED.md:37097) put the extension to Elias in these words: "with its condition - unless the party's own record carries the same words earlier".
- **The record adopts the narrow reading and leaves it open:**
  - coalition_declarations_2023.md:22: "READING, stated: 'always wins' as §652's condition words it - the party's own record dates a declaration where it carries the same words earlier."
  - Line 325, under "Left to Elias": "'Always wins' read as §652's condition (the header's READING)."
- **The build follows the narrow reading:**
  - PolandTimeline dates Konf->TD, Konf->NL and Konf->MN as `D(2023, 6, 20), Open`. Each cites [KONF-I4] first: RMF's page, with the F2 mark.
  - The party's own page [KONF-P1] ("2 sierpnia, 2023") appears only as a later restatement. The §8 row reads "| 5–7 | Konf → TD, NL, MN | ... | 2023-06-20 | Open | [KONF-I4] - F2; [KONF-P1] |".
- **What the literal rule would do:** read as CLAUDE.md states it, "a date from the party's own record always wins" puts the start of those lines at 2023-08-02. The record itself says those lines would start there without RMF (line ~334): "Konfederacja's lines start on its own record (2023-07-06, 2023-08-02)". So the standing rule, as written, contradicts the dates the record and PolandTimeline carry, and it settles an item the record leaves open for Elias.
- **Only one place in the changed files keeps the condition:** the PolandSpokenWords summary (DeclaredRedLines.cs:395, "always wins (§652's condition, accepted with it)"). These repeat the bare "always wins":
  - the PolandTimeline summary (DeclaredRedLines.cs:405);
  - the PolishDeclarationsDiagnostic summary (line 28);
  - the K-1i row (POLISIM_FEATURE_LIST.md:148).
- **No check holds the narrow reading.** The new F2 check only requires that a first tag of [X-Pn] goes with no mark. A fact re-dated to [KONF-P1] 2023-08-02 with the mark removed would pass.
- **Why minor:**
  - At runtime the change would show only in the run-up. A Polish start opens 34 weeks before 2023-10-15 (CampaignCalendar's defaults of 8 + 26 weeks, StartDate's default branch). The run-up declarations page between 06-20 and 08-02 would lose those lines.
  - Polling day does not change, because every one of these lines stands open on it. So the formation, nf1a and F7's acceptance test (KO+TD+NL) are unaffected.
  - What is at risk is that a future session follows the standing rule. It could "correct" the Polish dates, or date a new country's facts under the broad reading before Elias has ruled on it.

**The skeptic's corrected fix.** Put the condition and the reading into the standing rule. CLAUDE.md:53's F2 clause would read: "and (F2, 2026-10-05, §780, accepted with §652's condition) a leader's spoken words count, quoted verbatim on the broadcaster's or news agency's own page and dated by that page - a journalist's paraphrase never counts, and a date from the party's own record always wins, read as §652's condition: the party's own record dates a declaration where it carries the same words earlier (that reading is Elias's to confirm, `coalition_declarations_2023.md` §12)". Line 54 already states a reading separately from the ruling's words in the same way ("the implementation's default, not the ruling's words"). Make the same change, so the rule says the same thing everywhere, in:
- the K-1i row's "(3) was answered by F2" sentence (POLISIM_FEATURE_LIST.md:148);
- the PolandTimeline summary (DeclaredRedLines.cs:405);
- the PolishDeclarationsDiagnostic summary (line 28).
If Elias later rules for the literal reading instead, the build changes and these lines drop the qualifier.

### 16. Sweden's 2026 record still calls KD -> S's date an open extension 'for Elias' after K-1i (3) was answered

- **Lens:** ruling - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/sweden/2026/coalition_declarations_2026.md:39

**The scenario.** The K-1i row now says '(3) was answered by F2 ... KD's date stands', and DeclaredRedLines.cs:351-352 says 'RULED by Elias's ruling F2'. Sweden's record, which is not in this diff, still dates KD -> S as 'spoken, not a party publication, so an EXTENSION of §621's precedent, for Elias'. Someone reading Sweden's record sees a closed question as open.

**The fix proposed.** Reword line 39: dated by F2, Busch's words quoted verbatim on SVT's own live page [KD-I1].

**The skeptic's evidence.** I could not refute the finding. The stale line is still in the working tree, and nothing in this change edits it (`git status` shows no change under ElectionsData/sweden/).

1. **The stale line.** ElectionsData/sweden/2026/coalition_declarations_2026.md:39 sits in the live "FOR THE FORMATION MODEL" table and reads: "dated 2026-09-02 (Busch's own words in SVT's live report - spoken, not a party publication, so an EXTENSION of §621's precedent, for Elias)".

2. **The change says the question is closed in four places.**
   - The K-1i row of POLISIM_FEATURE_LIST.md (line 148): "**(3) was answered by F2** (Elias's ruling, 2026-10-05, `COMPLETED.md` §780) ... KD's date stands (Busch's words on SVT's own live page)".
   - DeclaredRedLines.cs:351-352: "quoted verbatim on the broadcaster's own page and dated by it: RULED by Elias's ruling F2 (K-1i (3), first put with §639)".
   - The `SwedenTimeline` summary (DeclaredRedLines.cs:313-314) now says "by Elias's ruling F2".
   - The Poland record, line 18: "Elias's ruling F2 (2026-10-05, answering K-1i (3))".

   KD's basis string cites the Sweden record through `SwedenSource`. So the code and the record it cites now disagree on whether the question is open.

3. **F2 really does decide KD's date, so the record is wrong, not the K-1i row.**
   - The saved page is raw/declarations/svt_live_busch_nej_till_andersson.html. Its sha256 e2464367...0b9 matches the record's register.
   - The quote is verbatim on svt.se: "– Vi kommer att vara beredda att rösta nej till Magdalena Andersson ända fram till ett nyval, säger Busch."
   - The page dates itself: article:published_time 2026-09-02T11:58:33+02:00.
   - KD has no own-record source (DeclaredRedLines.cs:133, "the KD primary is a GAP"), so no earlier date from the party's own record can win.

4. **Precedent says the ruling's commit updates this row.** When K-1i (2) was answered, commit 76a0dd10 (§652) rewrote the MP row of this same table (line 37) in the ruling's own commit. The row went from "...a stated deviation from §621's first rule, for Elias" to "dated 2026-08-10 - Helldén's own words to Sveriges Radio [MP-I1] (ruled 2026-09-29, §652 ...)". This change leaves the matching KD row alone.

5. **Why minor and not defect.**
   - Nothing at runtime or in the bar reads this text. In Assets, `coalition_declarations_2026` appears only in comments and in OfficeTestDiagnostic's printed footer, and no tool scans records for "for Elias".
   - The record's §639 "AS WIRED" paragraph (lines 709-711, "Two dates stretch §621's rules, for Elias") is a dated section. §652 left its MP half as written, so by that precedent it can stay as is.
   - The cost: the record the code cites tells a reader that a ruled question is still owed to Elias, which invites asking him again.

6. **The fix needs §780 entry.** COMPLETED.md has no §780 yet (grep finds none). The K-1i row and CLAUDE.md already cite it, so the commit has to write that entry too.

**The skeptic's corrected fix.** In ElectionsData/sweden/2026/coalition_declarations_2026.md line 39, replace "dated 2026-09-02 (Busch's own words in SVT's live report - spoken, not a party publication, so an EXTENSION of §621's precedent, for Elias)" with this text, which follows the §652 MP row's pattern: "dated 2026-09-02 - Busch's own words, quoted verbatim on SVT's own live page [KD-I1] (ruled 2026-10-05, Elias's ruling F2, §780: a leader's spoken words count when quoted verbatim on the broadcaster's own page, dated by that page; a date from the party's own record always wins, and the KD primary is a GAP); Kvartal's report [KD-I3] of 2026-06-05 first carried it, the newsroom's words, which never count". Optionally, add "(since ruled: MP's by §652, KD's by F2, §780)" after "Two dates stretch §621's rules, for Elias" at lines 709-711; §652 did not touch that dated section. Make the edit in the same commit as F2, and make sure that commit also writes the COMPLETED.md §780 entry that the K-1i row and CLAUDE.md cite.

### 17. Doubt 4 is 'decided by F2' only by reading Super Express as not a broadcaster; that reading is not put to Elias

- **Lens:** ruling - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:71

**The scenario.** The 26 June opening lifts nothing because 'Super Express is a newspaper' (lines 71, 308). The saved [KONF-I8] page introduces the Q&A as 'wywiad ze Sławomirem Mentzenem, gościem "Wieczornego Expressu"' and embeds Super Express's own programme (VideoObject 'Wieczorny Express - Sławomir Mentzen cz.1', uploadDate 2023-06-22T22:22:07Z). Whether a newspaper airing its own interview programme is 'the broadcaster' F2 names is a reading of the ruling, and it decides doubt 4. It is not among 'Left to Elias'. No polling-day change: the lines would restart on 07-06, 07-13 and 08-02.

**The fix proposed.** State the reading at doubt 4 and add it to 'Left to Elias'.

**The skeptic's evidence.** I could not refute it. Every factual claim in the finding matches the saved bytes, and the record's text.

1. **What the record says.** `coalition_declarations_2023.md` decides doubt 4 by calling Super Express a newspaper, stated as a fact:
   - Line 71: "Under F2 none of these dates the opening: Super Express is a newspaper, and the PAP and RMF24 pages relay its interview - no broadcaster's or agency's own page carries the words as said".
   - Line 21: "a newspaper and a portal date nothing here".
   - Line 308, doubt 4: "decided by F2: ... none the broadcaster's or the agency's own page for those words, so it lifts nothing".
   - The code comment says the same: `DeclaredRedLines.cs` line 418, "stands only on Super Express's interview ... [KONF-I8], [KONF-I10], [KONF-I11], which F2 does not count, so it lifts nothing".
   - The record's only "READING, stated" is the "always wins" one (line 21).
   - "Left to Elias" (lines 322-331) has five items: PiS→KO's start, KO→PiS's start, NL→Konf's support half, "always wins", and RMF24's verbatim-ness. None is about Super Express.
   - No changed file mentions "Wieczorny Express". I searched the whole diff, the K-1i row and the fetch log.

2. **What the page says.** The saved page is `Konf/se_mentzen-braun-ministrem-kultury.html`; its sha256 94b53e2c... matches SHA256SUMS line 19.
   - Its lead: "Najnowszy wywiad ze Sławomirem Mentzenem, gościem "Wieczornego Expressu"."
   - Then the Q&A, from "„Super Express”: - Czy będziecie rządzić z PiS..." to "Rozmawiał: KAMIL SZEWCZYK".
   - Then `<!-- ARTICLE_BLOCK_VIDEO_NEW -->`: a player titled "Wieczorny Express - Sławomir Mentzen cz.1", length 2229 s.
   - The VideoObject's uploadDate is "2023-06-22T22:22:07+00:00".
   - So the page's own words tie the interview to Super Express's own programme. The record knows the video exists (line 70, "the interview's video is dated 22 June, UTC") but never addresses it.

3. **Why the newspaper call alone decides doubt 4.**
   - The record reads RMF24's write-up of its own debate as verbatim, though an editor laid out the Q&A ("Opracowanie: Jan Matoga", [KONF-I4]). The Super Express Q&A of its own programme is the same kind of text.
   - So, on the record's own standard, only "newspaper, not broadcaster" keeps the 26 June opening from lifting the June lines.
   - The opening also changed status. At HEAD, doubt 4 read "26 June as replacing all five lines". §776's extension, which Elias then "Accepted" as F2, dated "the June lines and their lift". Question 7 to Elias gave doubt 4 no recommendation. The change reverses that lift, and this unstated reading is its only basis.
   - "Decided by F2" therefore overstates the ruling. It is F2 plus a reading Elias was not asked about.

4. **What it would change: no polling-day effect, so severity stays note.**
   - If Super Express counted, the June facts would end on 2023-06-26.
   - Konfederacja's lines would restart on 07-06 [KONF-P2], 07-13 [KONF-I14] and 08-02 [KONF-P1].
   - The polling-day set would be unchanged, so no 2023 formation moves.
   - Only a mid-term round in the 2019 chamber between 06-26 and 07-06/07-13 would read differently. A Polish game opens on 2023-02-19, but PiS holds a majority there, so such a round is unlikely.
   - This is the same kind of item the record already puts to Elias (the RMF24 verbatim question, "no polling-day line changes"). Leaving it out is an inconsistency, not a behaviour defect.

**The skeptic's corrected fix.** At §2 (line 71) and doubt 4 (line 308), replace the flat "Super Express is a newspaper" with a stated reading:

"READING, stated: Super Express is read as a newspaper, not the broadcaster F2 names. Yet [KONF-I8] gives the Q&A as its interview with Mentzen as 'gościem "Wieczornego Expressu"', Super Express's own video programme, whose part 1 the page embeds (VideoObject uploadDate 2023-06-22T22:22:07Z). If a newspaper's own programme counts as the broadcaster's own page, with its Q&A read as verbatim as [KONF-I4]'s is, then the opening is dated by that page, 2023-06-26. The June lines would end that day, and Konfederacja's lines would restart on 07-06 [KONF-P2], 07-13 [KONF-I14] and 08-02 [KONF-P1]. No polling-day line changes."

Add the same question as a sixth bullet under "Left to Elias". Change the code comment at DeclaredRedLines.cs line 418 from "which F2 does not count" to "which F2, as the record reads it (§12, doubt 4), does not count".

### 18. 'R2 shows what F1 changed' overstates R2, which removes only the PiS-KO pair

- **Lens:** ruling - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:299

**The scenario.** R2 (PolishDeclarationsDiagnostic.cs:141, :153) drops only the PiS-KO facts. F1's other new support halves stay in it: TD -> PiS, TD -> Konf, NL -> PiS, and Konf -> PiS from 07-06. HEAD's R1 on the same played count was 'PiS+KO on TD's support', while R2 prints 'PiS+KO'. So R2 shows what the PiS-KO lines change, not what F1 changed, and 'as before F1' in its label describes doubt 1 only.

**The fix proposed.** Reword as 'R2 shows what F1's PiS-KO lines change'.

**The skeptic's evidence.** I could not refute this. It is a wording note about one sentence in the record. Nothing in the code is wrong.

1. R2 removes only the PiS-KO pair. PolishDeclarationsDiagnostic.cs:141 is `if (withoutPisKo && ((f.Party == "PiS" && f.Other == "KO") || (f.Party == "KO" && f.Other == "PiS"))) { continue; }`. Line 142, `bool blocks = !cabinetOnly && f.BlocksSupport;`, keeps every other fact as built.

2. F1 changed more lines than the pair, by the record's own account.
   - The §8 table marks seven facts "(F1)": 2, 8, 9, 10, 11, 13 and 15. R2 drops only 2 and 15.
   - §12 says items 2, 5 and 7 were "decided by F1" or "answered under F1".
   - In DeclaredRedLines.cs, three lines that were cabinet-only at HEAD are now `true, true` and tagged "DECLARED (F1)":
     - TD->PiS from 2023-05-15
     - TD->Konf from 2023-08-10 (at HEAD it started 10-10)
     - NL->PiS from 2022-09-20
   - All three stay in R2.

3. R2 is therefore not the reading before F1.
   - HEAD's §12 table gave the played count (seed 777) under R1 as "PiS+KO on TD's support".
   - HEAD's own R2 (TD->PiS read support-blocking) gave "PiS+KO".
   - In nf1a.log:579 the new R2 gives "MajorityCoalition - PiS+KO (323 in cabinet, 323 supported)".
   - So the new R2 still carries F1's answer to doubt 2: the TD->PiS line removes the support TD gave before F1. Going from the old reading to R2 also swaps NL->Konf's F2 change, but the derived NL-Konf line (nf1a.log:566) still blocks support, so that changes no formation.

4. Line 299 says "R2 shows what F1 changed", and the record nowhere says that R2 keeps F1's other lines. That is broader than what R2 isolates.

Why this is only a note:
- The words after the colon are accurate: "read as aims, the PiS–KO pledges leave the game's count to PiS+KO".
- The label at :287, "R1 without the PiS–KO pair (doubt 1 read as aims, as before F1)", is also accurate.
- F1's change to the cabinet does rest on the pair. In nf1a.log:580, R3 (every support half read cabinet-only) still forms KO+TD+NL.

One correction to the finding itself: Konf->PiS is not a new support half. It was already support-blocking at HEAD from 2023-07-13 [KONF-I14]. F1 only moved its start to 2023-07-06 [KONF-P2], and on polling day it is the same line. The truly new support halves are TD->PiS, TD->Konf and NL->PiS. Only TD->PiS moves R2's government. NL->PiS is already covered by the derived PiS-NL support-blocking line (nf1a.log:565). TD->Konf has no bearing on PiS+KO.

The diagnostic's comment at PolishDeclarationsDiagnostic.cs:147-148 has the same imprecision: "so the measurement shows what that ruling changes".

**The skeptic's corrected fix.** At coalition_declarations_2023.md:299, replace "R2 shows what F1 changed: read as aims, the PiS–KO pledges leave the game's count to PiS+KO." with "R2 shows what F1's PiS–KO pair changes, F1's other lines standing in it (TD → PiS among them, which takes away the TD support §776's R1 measured): read as aims, the PiS–KO pledges leave the game's count to PiS+KO." Make the matching change to the comment at PolishDeclarationsDiagnostic.cs:147-148: "...so the measurement shows what that pair changes, F1's other lines standing; R3 reads...". Leave the R2 label (:287 and :153) as it is. It is already accurate.

### 19. The PiS -> KO start item offers [PIS-I1] as TVN24's words, but the page credits PAP as well

- **Lens:** ruling - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:322

**The scenario.** The item presents 'Kaczyński's TVN24 words of 2023-07-23' ('Ten człowiek nie może rządzić Polską', dash-quoted) as a possible F2-dated start, and asks only the person-keying question. The page's credit reads 'Źródło: TVN24, PAP'. Under the header's own rule that a page relaying an agency's wire dates nothing, whether this page qualifies is a second question the item does not raise. [KO-I12] ('Źródło: tvn24.pl, PAP') is used only as a restatement, so it is harmless.

**The fix proposed.** Add the PAP-credit question to the item.

**The skeptic's evidence.** I could not refute it; every part of the scenario is in the bytes and the record.

1. **The page credits PAP.** The saved [PIS-I1] page is `ElectionsData/poland/raw/declarations_2023/PiS/tvn24_2023-07-23_kaczynski-nie-bedziemy-rzadzili-z-konfederacja.html` (digest re-checked: 72249382…81da). Its credit reads `<dt class="article-top-bar__meta-title">Źródło:</dt><dd class="article-top-bar__meta-item">TVN24, PAP</dd>`, repeated as `<span class="custom-source-header">Źródło: </span>TVN24, PAP` and `"customSource":[{"fields":[{"value":"TVN24, PAP"}]}]`.
   - The Tusk passage ("Donald Tusk - prawdziwy wróg naszego narodu. … Ten człowiek nie może rządzić Polską. … - stwierdził Kaczyński") is in the article body, covered only by that page-wide credit.
   - The page's own TVN24 video is a 55 s clip titled "Prezes PiS: nie będziemy rządzili z Konfederacją". Nothing on the page says whether the Tusk words are TVN24's own or PAP's wire.

2. **The record's own reading makes this a live question.** The header (coalition_declarations_2023.md lines 20-22) says: "a page relaying another outlet's interview or an agency's wire - GazetaPrawna, Bankier, Forsal and wnp carrying PAP, RMF24 relaying Super Express - … date nothing here".
   - RMF24 counts as a broadcaster's own page, yet it is excluded because it relays, so the record judges where the words came from, not who owns the page.
   - §776 put the extension to Elias as words "dated by the broadcaster's or agency's page that reports them" (COMPLETED.md line 37097).

3. **The item asks only one question.** Line 322 reads: "PiS → KO's start: Kaczyński's TVN24 words of 2023-07-23, "Ten człowiek nie może rządzić Polską.", of Tusk [PIS-I1], would start it then if a pledge against a party's leader keys the party". Person-keying is its only condition; the PAP credit is never raised.
   - The register row for [PIS-I1] (line 338) gives the publisher as plain "TVN24".
   - The record does flag the same credit elsewhere: [KO-I3] (line 381; its page says "Źródło: tvn24.pl, PAP") is listed as "TVN24 (with PAP)".

4. **The party's own record cannot settle it.** PiS's own page of the same day, [PIS-P1] (`pis_2023-07-23_stawiski-dobro-polski.html` and its wp-json record), has "Tusk jest prawdziwym wrogiem polskiego Narodu! Niech pójdzie sobie ze swoją polityką do Niemiec!". It does not have "Ten człowiek nie może rządzić Polską", and no other in-tree or sweep page has those words about Tusk. So F2's "always wins" clause cannot make the question moot.

5. **The pages that date facts today have no such credit.** [KO-I11] is credited "TVN24" and [KONF-I14] "Fakty po Południu TVN24", and in both the words were said on TVN24's own programme. [PIS-I1] would be the first F2-dated page with an agency co-credit, and the first that reports a speech given elsewhere.
   - PolishDeclarationsDiagnostic's new F2 check only matches the tag kind against the PolandSpokenWords mark, so it would not catch this if Elias answers "yes".

6. **[KO-I12] is harmless, as the finding says.** Its page is credited "tvn24.pl, PAP" (register: "TVN24 (live)"), but it is cited only as a restatement in doubt 7. NL → PiS (fact 13) is dated by [NL-P31], and [KO-I12] is not the first tag of any fact in §8.

7. **The same gap is next door.** The KO → PiS start item (line 323) offers Budka's "TVN24, 2023-08-09" words. That page is out of tree (`PoliSim-captures/sources/poland_declarations_2023/KO/tvn24_2023-08-09_budka_inauguracja_kampanii.html`) and also reads "Źródło: TVN24, PAP".

**Severity: note.** This is a gap in a question left to Elias. Nothing in the game changes: both candidate starts fall before polling day, and the item list itself says "no formation turns on any of them".

**The skeptic's corrected fix.** 1. **Item at line 322:** add the second question. For example: "…and only if the page counts under F2: its credit reads 'Źródło: TVN24, PAP', so whether these words are TVN24's own report or PAP's wire relayed (which the header's reading says dates nothing) is Elias's too. PiS's own page of the day [PIS-P1] carries 'Tusk jest prawdziwym wrogiem polskiego Narodu!', not these words, so the party's own record cannot date them."
2. **Register row for [PIS-I1] (line 338):** change the publisher to "TVN24 (with PAP)", the way [KO-I3]'s row reads. Optionally mark [KO-I12] "TVN24 (live, with PAP)" as well.
3. **KO → PiS item (line 323):** add the same note to the Budka candidate, whose page also credits "TVN24, PAP".

### 20. The new F2 check only reads the first tag's letter, so it cannot tell a broadcaster's own page from a relay. Run on HEAD's timeline it passes all 17 facts, including the ones F2 overturned

- **Lens:** tests - **reviewer:** defect - **skeptic:** minor
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:86

**The scenario.** Lines 86-93 take the first [X-Ln] tag and require L=='P' exactly when the basis lacks PolandSpokenWords. The check never reads three things: the register's publisher column, its 'page's own date' column, or any fact's Until. The register's I-tags mix broadcasters' own pages with relays and portals: [TD-I5], [NL-I5] and [NL-I7] are 'GazetaPrawna.pl (PAP)', [KONF-I10] is 'Bankier.pl (PAP)', [NL-I6] is WP and [KONF-I19] is Do Rzeczy.

I ran the same regex and XOR (scratchpad f2_on_head.pl) on HEAD's 17 facts, with HEAD's PolandExtension constant as the mark. Every fact passes, including:
- TD->PiS from 2023-05-15, first tag [TD-I5];
- NL->Konf support-blocking from 2023-08-27, first tag [NL-I5] (both are GazetaPrawna PAP copies);
- the five June lines closed on 2023-06-26 on Super Express/PAP/RMF24 relays.
These are exactly the facts F2 re-dated or removed.

Concrete miss 1: re-add the NL->Konf support-blocking fact (from 2023-08-27, [NL-I5] first, with PolandSpokenWords), close fact 14 that day and edit §8 to match. The result passes (a), (b), F2, (c) and (d), plus DeclarationDatesDiagnostic's overlap test. It also moves no formation, because the derived NL-Konf line is already support-blocking both ways (CoalitionFormation.cs:959; nf1a 'MEASURED the derived line NL-Konf'). So (e)-(h), F7's acceptance and the sweep all stay green.

Concrete miss 2: move fact 15's From to 2023-09-19 (Tusk on Polsat [KO-I7], one of §12's earlier candidates) while [KO-I11] stays first. This also passes, because From is never compared with the first tag's date.

The ok line still prints 'a leader's words on a broadcaster's own page'.

**The fix proposed.** Make the check see F2's distinction:
- Give the register an explicit F2 column, or hold a fixed list in the diagnostic of the pages F2 admits (the record names [KONF-I4], [KONF-I14] and [KO-I11]). Require every marked fact's first tag to be on it.
- Compare each fact's From with its first tag's register date.
- Require an unmarked fact's P tag to carry the declarer's own prefix.

If none of these is done, reword the ok line and the class summary (lines 26-29) to what is actually tested: 'the first tag's kind agrees with F2's mark'.

**The skeptic's evidence.** I could not refute the finding. The facts it states are correct. I would grade it minor rather than a defect, because nothing it prints today is false.

**What the check tests.** PolishDeclarationsDiagnostic.cs:86-91 reads one thing from the Basis text: the first letter after the dash in the first tag, matched by `\[[A-Z]+-([A-Z])\d+\]`. It requires that letter to be 'P' exactly when the Basis does not contain `PolandSpokenWords`.
- It never reads the register's publisher column or its "page's own date" column.
- It never reads From or Until.
- No other check reads those columns either. Check (a), line 57, compares §8 with the array but stops before §8's basis column. Check (b), line 75, takes only the tag id. Check (c), lines 101-103, takes only cells[5] (files) and cells[6] (digests). DeclarationDatesDiagnostic tests only that From comes before Until and that spans do not overlap.

**The I-tags mix broadcasters with relays.** The record's own header (lines 20-22) says that "GazetaPrawna, Bankier, Forsal and wnp carrying PAP, RMF24 relaying Super Express - a newspaper and a portal date nothing here". Yet these register rows are all I-tags:
- [TD-I5] (line 364), [NL-I5] (377) and [NL-I7] (379) are "GazetaPrawna.pl (PAP)";
- [KONF-I10] (350) is "Bankier.pl (PAP)";
- [NL-I6] (378) is "Wirtualna Polska";
- [KONF-I19] (354) is "Do Rzeczy".

**I re-ran the check's logic in perl** (scratchpad f2sim.pl), on HEAD's PolandTimeline with HEAD's `PolandExtension` as the mark. Result: 17 facts, misdated=0. That includes:
- #13 TD->PiS, first tag [TD-I5], marked: passes;
- #17 NL->Konf support-blocking, first tag [NL-I5], marked: passes;
- #2-6, the June lines whose Until of 2023-06-26 was set by the Super Express/PAP/RMF24 relays: pass, because Until is never read.

These are the facts F2 re-dated or removed. So the check would not have flagged the state F2 overturned.

**The two concrete misses hold.** Both need §8 edited to match, which keeps (a) green.
- Miss 1 (re-adding NL->Konf on [NL-I5]) passes (b), the F2 check, (c), (d) and the no-overlap test. The NL-Konf line this would declare is already drawn by the derived lines as support-blocking (CoalitionFormation.cs:959; record §10), so the formation does not move.
- Miss 2 (moving KO->PiS's From to 2023-09-19) passes, because From is never compared with any register date.

**Why it is minor, not a defect.**
- The check is not empty. It does force the mark onto any fact dated by a page that is not the party's own, and it rejects the mark on an own-page fact.
- The working-tree data meets the stronger property. In perl (f2dates.pl), every one of the 15 facts' From equals its first tag's page date. The 7 marked facts' first tags are RMF24's record of its own debate [KONF-I4] (5 facts), TVN24 [KONF-I14] and TVN24 Fakty [KO-I11]. Each of the 8 unmarked facts cites the declarer's own page first.
- No formation result is wrong.

**The real problem is the wording.** The ok line (line 92), the class summary (line 29) and the comment (lines 84-85) say "dated by its first tag … a leader's words on a broadcaster's own page". What is actually tested is only that the first tag's kind agrees with the mark. The comment even equates [X-In] with "a broadcaster's or an agency's page", which the register contradicts. A green bar therefore suggests F2 is held mechanically, when its central distinction (the broadcaster's own page versus a relay) is not tested at all.

**The skeptic's corrected fix.** **Minimum fix (honest wording).** Reword line 92's ok line, the comment at lines 84-85 and the summary at line 29 to say what is actually tested: "F2's mark agrees with the first tag's kind - an own-page tag [X-Pn] without the mark, any other tag [X-In] with it". Drop "a leader's words on a broadcaster's own page" and "dated by its first tag".

**Stronger fix (make the check see F2's distinction).**
1. Give the register an explicit admission cell, for example "F2: own page / relay / newspaper", or hold a fixed list in the diagnostic of the tags F2 admits: [KONF-I4], [KONF-I14], [KO-I11]. Fail any marked fact whose first tag is not admitted.
2. Give the register an ISO date column, or have the diagnostic hold a map from each dating tag to its date. Require each fact's From to equal its first tag's date.
3. Require an unmarked fact's first P-tag to carry the declarer's own prefix (PiS->PIS, Konf->KONF, TD->TD, NL->NL, KO->KO).

Before relying on any of this, confirm it with the same perl probe: HEAD's #13 ([TD-I5]) and #17 ([NL-I5]) must then FAIL, while the working tree's 15 facts pass.

### 21. Nothing reads SHA256SUMS.txt, so a register file missing from it, or a stale line in it, passes

- **Lens:** tests - **reviewer:** minor - **skeptic:** note
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:99

**The scenario.** Check (c), lines 95-111, hashes each file named in a register row against that row's own digest cell. No .cs, .pl, .ps1 or .sh file in the repo opens SHA256SUMS.txt (grep).

So adding a new fact's page (say [NL-P33]) to the register with its file and digest, but forgetting its SHA256SUMS.txt line, passes every check. So does leaving a stale line after a re-fetch. Yet the record's head says the digests are 'in the register below and in its SHA256SUMS.txt'.

Today the two agree: 61 lines with the same paths and digests as the register's 61 files, each matching its bytes. The two new NL pages are byte-identical to the sweep's out-of-tree copies and listed in that folder's SHA256SUMS.

**The fix proposed.** In (c), parse raw/declarations_2023/SHA256SUMS.txt and require its path-to-digest map to equal the register's, with every line matching its file's bytes.

**The skeptic's evidence.** The gap the finding describes is real, but this change did not create it, and nothing is wrong in the tree today.

1. Check (c) never opens SHA256SUMS.txt. PolishDeclarationsDiagnostic.cs:99-110 reads only each register row's file cell (cells[5]) and digest cell (cells[6]) and hashes the bytes (`File.Exists(path) && Sha256(path) == digests[i]`). A grep for "SUMS" in every .cs, .pl, .ps1, .sh and .pm file in the repo (Tools/textcheck included) finds no reader of any SHA256SUMS file. So if a future edit adds a register row and forgets its SUMS line, or leaves a stale line after a re-fetch, every check still passes.

2. The check claims no more than it does:
- Line 111 says "every file the register names is held under raw/declarations_2023 at its digest".
- The record's head says the diagnostic "holds every register file to its digest".
- The phrase "digests in the register below and in its `SHA256SUMS.txt`" (record line 5) is a statement about the files, not a claim about what the check covers.

3. The gap is older than this change. `git show HEAD:` has the same check (c) (HEAD line 82, "(c) every register file held in tree at its digest") and the same record line 5. Both came in with s776 (22796935). This diff only adds the F2 first-tag check above (c) and leaves (c) alone.

4. It is the repo's convention, not a slip here. The repo tracks 33 SHA256SUMS manifests (france, germany, italy, macro, poland x7, rules, sweden, usa, docs/reference). Code reads none of them. COMPLETED.md records them being checked by hand with `sha256sum -c` (for example "148 of 148 index blobs match their SHA256SUMS" at §607, and "64 of 64" at the n724 documents tier).

5. Today the files agree. I checked them in a separate perl pass that parses the register the way (c) does:
- The register has 59 rows naming 61 files. SHA256SUMS.txt has 61 lines, all distinct.
- Every register file has a SUMS line with the same digest, and every SUMS line matches the file's bytes.
- raw/declarations_2023 holds 63 files: the 61, plus SHA256SUMS.txt and fetch_log.md. No file on disk is missing from the manifest.
- The two new NL pages (cc4f6da5..., 27fe8ae2...) are byte-identical to the out-of-tree sweep copies and appear in that folder's NL/SHA256SUMS.

So the scenario needs a future mistake. Check (c) does what it says, and the gap is the same repo-wide one every raw folder has. That makes this a note, not minor.

**The skeptic's corrected fix.** This change needs no fix. If Elias wants the extra coverage, add it as one repo-wide check rather than inside (c). That check would read every tracked **/raw/**/SHA256SUMS* manifest (the names vary: SHA256SUMS, SHA256SUMS.txt, SHA256SUMS.results.txt), parse lines of the form `<64 hex> *<path>` with the binary '*' marker and CRLF allowed, and check each line against the file's bytes. It would also require every file in the folder to be listed, except the manifest, the fetch log and any declared URL lists. Optionally, (c) could then also require each register file-to-digest pair to appear in raw/declarations_2023/SHA256SUMS.txt.

### 22. No check holds a Polish replacement chain together, so a gap between a line and its successor passes

- **Lens:** tests - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/DeclarationDatesDiagnostic.cs:98

**The scenario.** Section 3 tests from<until and no overlap per (kind, party, other). The LiftedSince checks (lines 115-117) cover Sweden only, and Poland's only standing test runs on 18 Jan 2026.

Scenario: move fact 8 (Konf->PiS, F1) to start 2023-07-13 in both the array and §8. That is HEAD's start and [KONF-I14]'s date. Fact 3 still ends 2023-07-06. This passes:
- the no-overlap test, since the spans are [06-20,07-06) and [07-13,open);
- (a), since both copies agree;
- F2, since the first tag is still [KONF-P2] and its date is not compared;
- (d), (e)-(h) and the sweep, which read polling day only.

Yet Konf->PiS then stands nowhere from 07-06 to 07-12. LiftedSince (DeclaredRedLines.cs:463) would then list it as lifted, and the run-up page shows that list (GameController.CampaignDeclared.cs:241) as 'LIFTED 6 JUL 2023'. Meanwhile fact 3's basis says 'Replaced on 2023-07-06'.

**The fix proposed.** Assert that DeclaredRedLines.LiftedSince(CountryId.Poland, <any day before 2021-05-06>, PollingDay) is empty. The record lifts nothing since F2 dropped the 26 June lift, and this holds today. Alternatively, require every closed Polish fact to have a successor of the same party, other and kind that starts on its Until.

**The skeptic's evidence.** I could not refute the finding: no check catches the gap. One impact it claims cannot happen today, so I am keeping it at minor and not raising it.

Why every check passes the scenario:
- DeclarationDatesDiagnostic.cs:95-99 checks only that From < Until and that two spans with the same `Kind + Party + Other` key do not overlap. Facts 3 and 8 share the key "PairLine Konf PiS". If fact 8 starts on 07-13, `f.From (07-13) >= b (07-06)` is true, so the gap passes.
- The LiftedSince checks at lines 104 and 115-117 read Sweden only (`LiftedSince(CountryId.Sweden, ...)`).
- Poland's StandingOn check at lines 110-113 compares the count of open facts on 18 Jan 2026. Moving fact 8 leaves that count unchanged.
- PolishDeclarationsDiagnostic:
  - (a), lines 57-72: the array and §8 are compared row against row, so moving fact 8 in both copies passes.
  - F2, lines 86-91: compares the first tag's letter with the mark and never compares a date. [KONF-P2] has no mark, so it passes.
  - (d), lines 114-116, and (e) to (h): read polling day only.
- FormationSweepDiagnostic.cs:103 reads `For(id, parties)` for the seated chamber, which is polling day for Poland.
- No Editor diagnostic or tool checks "Replaced on" text against dates, or checks that a closed fact has a successor.

I simulated the timeline in perl, parsing the 15-fact PolandTimeline array (lines 411-443) and copying the logic of LiftedSince (DeclaredRedLines.cs:463-475) and of section 3:

| | `LiftedSince(2019-10-13, 2023-10-15)` | section 3 | open facts | standing on polling day |
|---|---|---|---|---|
| As built | [] | passes | 12 | 12 |
| Fact 8 moved to 2023-07-13 | [Konf>PiS until 2023-07-06] | still passes | 12 | 12 |

With the move, Konf>PiS stands on no day from 07-06 to 07-12, while fact 3's basis (line 418) still says "Replaced on 2023-07-06" and "so it lifts nothing". §12 item 4 of the record says the same.

What the finding overstates: the run-up page can never show "LIFTED 6 JUL 2023" for Poland today.
- `DeclaredPageAvailable` (GameController.CampaignDeclared.cs:32-35) requires `_liveCampaignOpen`.
- `LiveCampaignSetup.TryFor` (LiveCampaignSetup.cs:147-176) stages a campaign for Sweden and Germany only.
- The code's own comment says Poland's "timeline shows on no page yet".

What a gap would actually affect:
- LiftedSince's public result.
- A mid-term round in a Polish game: the game starts 2023-02-19, before the gap. `SimulationManager.RoundReading` takes `SittingReading`, which calls `ForDate(CurrentDate)` (SimulationManager.cs:3289). On a gap day that reading would lack the declared Konf→PiS line.

The edit is realistic. Two choices §12 leaves to Elias would rewrite these chains: the "always wins" reading, and NL→Konf's support half. Sweden's chains are already guarded, because their lifts are pinned at lines 115-117.

**The skeptic's corrected fix.** Assert that Poland lifts nothing. In DeclarationDatesDiagnostic after line 117 (or in PolishDeclarationsDiagnostic after (d)), call `var pl = DeclaredRedLines.LiftedSince(CountryId.Poland, WorldClock.ElectionDayOf(CountryId.Poland, ElectionVintage.Poland2019), new DateTime(2023, 10, 15));` and check `pl.Count == 0`. Use a message such as "Poland: every closed line is replaced on its until - F2's 26 June opening lifts nothing", and list each `f.Party>f.Other f.Until` on failure.

The 2019 polling day, 2019-10-13, is the `since` the run-up page itself would pass. It falls before fact 12, so all three closed facts (3, 4 and 12) are inside the window. The assert holds today and fails on the scenario.

A more general alternative covers every dated country: in section 3, require each closed fact to have a successor with the same Kind, Party and Other whose From equals its Until. Exempt only the lifts already pinned for Sweden (M>SD, L>SD, KD>SD).

### 23. F1's support halves are held only by §8 matching the array: the formation checks cannot tell R1 from R3, and the derived PiS-NL line hides NL->PiS's support half

- **Lens:** tests - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:277

**The scenario.** nf1a measures R1 and R3 (every support half read cabinet-only) as identical on both chambers:
- chamber of record: KO+TD on NL's support, ranked 2 of 5;
- played count: KO+TD+NL, ranked 1 of 5.

So (e), (g), (h) and F7's acceptance hold under either reading. The acceptance pins the PiS-KO cabinet block (R2 forms PiS+KO), not F1's shape.

Flip fact 13 (NL->PiS) to false/false in both the array and §8. The derived PiS-NL galtan line is support-blocking both ways (CoalitionFormation.cs:959) and comes ahead of the declared lines (TryFindInternalRedLine, line 725, takes the first). So every refusal, every opposed count and the sweep's 'blocked ... by 0-3' prints are unchanged. Only check (a) could catch the flip, and (a) compares two hand-written copies with each other. It also ignores §8's 'game shape' and basis cells, including their '(F1)' and '- F2' marks.

**The fix proposed.** Tie a fact's shape to the ruling's mark, as F2's mark is tied to the tag. Assert that a basis starts 'DECLARED (F1' exactly when BlocksSupport && OneWay; this holds for all 15 facts today (checked). Also compare §8's shape text and its '- F2' marks with the array.

**The skeptic's evidence.** Real, and a note. The fact-13 path holds as written, but the title overreaches: the sweep does hold the support halves that change a formation.

**1. The fact-13 flip passes every check.**
- Both line lists put the derived lines first: `PolishDeclarationsDiagnostic.WithDeclarations` (L134-146) and `DeclaredRedLines.ForDateSourced` (L482-491).
- `DerivedRedLines.From` (`CoalitionFormation.cs` L942-959) draws PiS-NL with `blocksSupport = socialBreak`, `OneWay` false. nf1a.log L565 shows the gap: "PiS-NL: DERIVED: CHES galtan gap 6.70 > 5.00".
- `RefusesSupport` already makes NL refuse a cabinet holding PiS, and PiS refuse one holding NL, whatever fact 13 says. So `SupportBlocked` (L844), `RedLinedMask`, `passesOnLines` and the opposed masks are unchanged.
- Whatever reports a line inside a cabinet takes the first match, which is the derived line. That covers `TryFindInternalRedLine` (L723-729), `SupportBlockBasis`, `FormationProposal.LineInside` (L240-246) and the sweep's "blocked 11 by 0-3".
- The shared-support loops (`SupportersOf` condition 2, `SharedSupport`) skip a line that is one-way or does not block support. Both versions of fact 13 are skipped.
- Nothing writes `LrGen`/`Galtan` for a roster party, so the derived line is on every Polish chamber.
- Check (a) never reads §8's shape or basis cells: its regex (L57) leaves the game-shape cell `[^|]+` uncaptured and stops before the basis cell. L69-70 compares only party, other, blocks, one-way, from and until. A consistent edit of both copies passes.
- No other check reads the shape. Checks (f), (g) and (h) build both sides from the array. `DeclarationDatesDiagnostic` checks only Poland's order, overlap and open count. `FilmDeclared` logs Kind and Party>Other. No code reads "(F1" or §8's shape column.
- The only place the flip shows is the run-up's declarations page (`CampaignDeclared.cs` L59-60, L111). The sentence and its tag would change from "will not sit in or back a cabinet with" / "one way" to "will not sit with" / "would support", and nothing guards that text.
- nf1a.log L568-571 and L577-580 confirm R1 = R3 on both chambers, so (e), (g), (h) and F7 hold under either reading.

**2. The overreach.**
- `FormationSweepDiagnostic` pins the seated Polish chamber's text under both investiture rules, including the opposed counts. The sweep line "cab 6 sup 8 … opposed 212" is PiS 194 plus Konfederacja 18.
- Under R3 those two blocks fall away, so the printed counts move.
- Under the negative rule, PiS alone would then face only NL's 26 against. Its base score (50 + 30·194/460 + 20·0.3846 ≈ 70.34) beats KO+TD's 67.45, so PiS alone would govern and the digest would move.
- So the halves of PiS→KO, Konf→KO, TD→PiS and KO→PiS are pinned by the sweep, not only by (a).
- What (a) alone holds:
  - NL→PiS: structurally masked, on every Polish chamber.
  - On these two chambers' arithmetic, Konf→PiS and TD→Konf as well:
    - The only admissible PiS cabinet is PiS alone. It fails with or without Konfederacja: 266 or 248 against, at least 231; 194 or 212 supported, below 231; played count 214.
    - The only admissible Konfederacja cabinet is Konf alone. TD votes against it anyway by holding out, since its best own cabinet scores 67.45 against 52.7.

**Why a note.** The masked half changes no formation. Hitting it takes a deliberate edit in two places. The only visible effect is page text. The check the reviewer proposes holds for all 15 facts today: perl over the array shows "DECLARED (F1" exactly on the seven true/true facts.

**The skeptic's corrected fix.** Keep the reviewer's check, beside the F2 block: for every Polish fact, assert `f.Basis.StartsWith("DECLARED (F1", StringComparison.Ordinal) == (f.BlocksSupport && f.OneWay)`. This ties the shape to the ruling's mark, as F2's mark is tied to the first tag.

Widen (a)'s regex to capture §8's game-shape cell and basis cell, and assert both:
- The shape cell starts with "one-way, support-blocking (F1" exactly when the row is true/true, and reads "cabinet" exactly when it is false/false.
- The basis cell carries "- F2" exactly when the row's facts carry `PolandSpokenWords`.

Narrow the title. The sweep's pinned digest already holds the support halves that change a formation (PiS→KO, Konf→KO, TD→PiS, KO→PiS). What only these text checks can hold is NL→PiS, which the derived PiS-NL galtan line masks on every Polish chamber because party positions never move. On these two chambers' arithmetic, Konf→PiS and TD→Konf are masked as well.

### 24. DeclarationDatesDiagnostic's Poland message now says 'the ones closed in 2023', but one fact closes in 2022

- **Lens:** tests - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/DeclarationDatesDiagnostic.cs:113

**The scenario.** Fact 12 (NL->PiS cabinet line) now closes on 2022-09-20, so nf1a printed 'its 12 open facts stand on 18 Jan 2026, the ones closed in 2023 do not', which is inaccurate. The test runs on Sweden's start date, so it cannot see any 2021-2023 date edit to a Polish fact, except an Until moved past 2026-01-18.

**The fix proposed.** Say 'the closed ones do not', or test StandingOn on Poland's own polling day.

**The skeptic's evidence.** I could not refute the label part. The part about the check's power is true but is not a defect.

1. The label is unchanged from HEAD and was written when every closed fact closed in 2023. Assets/Editor/DeclarationDatesDiagnostic.cs:110-113 is not in the diff:
   int polandOpen = 0; foreach (... PolandTimeline) { if (f.Until == DateTime.MaxValue) polandOpen++; }
   Check(DeclaredRedLines.StandingOn(CountryId.Poland, new DateTime(2026, 1, 18)).Count == polandOpen,
       F("StandingOn: Poland's timeline (§776) - its {0} open facts stand on 18 Jan 2026, the ones closed in 2023 do not", polandOpen));
   `git log` dates this line to 22796935 (s776). At HEAD the 7 closed facts all closed in 2023: the five Konf 'z nikim' lines on 06-26, Konf-PiS on 07-13 and NL-Konf on 08-27.

2. The change adds a fact that closes in 2022. In the working-tree PolandTimeline the 12th of 15 facts is:
   new DatedFact("NL", "PiS", FactKind.PairLine, false, false, null, D(2021, 5, 6), D(2022, 9, 20), ...
   The closed set is now Konf-PiS (until 2023-07-06), Konf-KO (until 2023-07-13) and NL-PiS (until 2022-09-20). The other 12 facts are open.

3. The change's own run printed the stale line. G:/UNITY/Projects/PoliSim-captures/logs/nf1a.log:662 reads:
   "ok        StandingOn: Poland's timeline (§776) - its 12 open facts stand on 18 Jan 2026, the ones closed in 2023 do not"
   Read literally the sentence is still true of the two facts closed in 2023. It now describes the closed set wrongly and understates the check, which requires every closed fact to stand nowhere, whatever its year. The data changed and a hard-coded claim about it went stale, against the spirit of the claim convention in CLAUDE.md. CLAUDE.md has no rule about check labels as such. The check's logic is still right, so nothing behaves differently. Severity: note.

4. The power argument is accurate but not a defect of this change. StandsOn is `date.Date >= From && date.Date < Until`, and every Polish From falls in 2021-2023, so on 2026-01-18 the check only separates open facts from closed ones. That was its design at s776. Polish dates are held elsewhere:
   - PolishDeclarationsDiagnostic (a) holds every fact's From and Until to the record's §8 table, row for row.
   - (d) holds the polling-day set ("exactly the facts still open stand").
   The suggested fix of testing on Poland's polling day adds nothing beyond (d). On 2023-10-15 the same 12 facts stand (KO>PiS from 10-12, PiS>KO from 09-08) and the same 3 do not.

**The skeptic's corrected fix.** Drop the year from the label in DeclarationDatesDiagnostic.cs:113 so it stops stating a fact about the data, for example: F("StandingOn: Poland's timeline (§776) - its {0} open facts stand on 18 Jan 2026, its {1} closed ones do not", polandOpen, DeclaredRedLines.PolandTimeline.Count - polandOpen). Do not add a polling-day StandingOn check, because PolishDeclarationsDiagnostic (d) already holds the polling-day set and (a) holds every From and Until to the record's table.

### 25. (Outside this lens) The run-up page's DECLARED slip still says every item is dated by the party's own record

- **Lens:** tests - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/UI/GameController.CampaignDeclared.cs:184

**The scenario.** The slip on the run-up page's DECLARED head reads 'EACH ITEM IS FROM THE PARTY'S OWN RECORD AND DATED BY IT'. The same page lists StandingOn(Poland, today). On a Polish run-up day such as 2023-10-14, that list includes the F2-dated facts: Konf->TD/NL/MN and Konf->KO (dated by RMF24's or TVN24's page) and, from 10-12, KO->PiS. Sweden's KD->S (SVT) and MP (Sveriges Radio) are in the same position.

This predates the diff, but F2 now rules that kind of dating, so the slip contradicts a ruling.

**The fix proposed.** Reword the slip to match F2, for example: 'DATED BY THE PARTY'S OWN RECORD, OR BY A BROADCASTER'S OWN PAGE QUOTING ITS LEADER'. The wording is Elias's or Design's call.

**The skeptic's evidence.** I could not refute the slip's wrong claim, but the finding's main scenario is wrong. Poland can't reach this page. Sweden can, and the problem was there before this diff.

**Polish path: cannot happen today.**
- The slip at GameController.CampaignDeclared.cs:182-187 is drawn only when `_liveDeclaredOpen && DeclaredPageAvailable()` (GameController.cs:2797).
- `DeclaredPageAvailable()` (CampaignDeclared.cs:34-35) needs `_liveCampaignOpen`. That flag is set only by `OpenLiveCampaign` (Campaign.cs:140-146), which needs a `PlayerPreCampaign` or a `PlayerCampaign` (Campaign.cs:152).
- Every place those two are assigned (SimulationManager.cs:4273-4279, 4325-4329, 4530-4532, 4550-4556) is gated by `LiveCampaignSetup.TryFor`. That method stages Sweden and Germany only: `if (country == CountryId.Sweden || country == CountryId.Germany)` (LiveCampaignSetup.cs:148), else "no campaign is staged" (:176).
- PollingDayDiagnostic.cs:180 asserts `!staged` for a Polish game walked through 15 Oct 2023. The page's own doc says it too (CampaignDeclared.cs:32-33): "LiveCampaignSetup stages none for Poland, so its timeline shows on no page yet".
- The screenshot driver also opens the page through the gated `OpenLiveDeclared` (UiScreenshotDriver.cs:5927).
- So the F2-marked Polish facts never appear under this slip, and the diff adds no reachable instance.

**Swedish path: reachable, and older than this diff.**
- `StartDate(Sweden)` is the run-up of 2026-09-13 (WorldClock.cs:87, 116-117). From CampaignClock.cs:112-114 the run-up opens 2026-01-18 and the campaign runs 2026-07-19 to 09-12.
- On any day from 2026-09-02 to 09-12, `StandingOn(Sweden, today)` (DeclaredRedLines.cs:453-458) includes two broadcaster-dated rows:
  - MP's in-or-against rule (:350), from 2026-08-10, dated by "Helldén's own words to Sveriges Radio" (§652).
  - KD→S (:353), from 2026-09-02, dated by SVT's live report, "RULED by Elias's ruling F2" (:351-352).
- The K-1i row calls (2) "a deviation from §621's first rule" and (3) "an extension of §621's precedent". F2's own words, "a date from the party's own record always wins", treat the broadcaster's page as a separate source. So for these rows, "EACH ITEM IS FROM THE PARTY'S OWN RECORD AND DATED BY IT" (:184) is false. It has been false since §657 built the page on the day §652 dated MP's rule.
- Germany has the same problem, also older. TryFor stages Germany, and the German page lists:
  - the AfD candidacy, dated by ZDF's own sentence about the party board's nomination (:385-386);
  - the Union's candidacy, dated by the CSU's page and marked DERIVED (:381-382).
  The same page's CANDIDACIES slip (:219) already says the opposite: "EACH FROM THE DAY ITS SOURCE DATES IT".
- The class doc at :18, "(the party's record, §621)", carries the same stale claim.

**What the diff changes.** CampaignDeclared.cs is not in the diff. The dates and bases of Sweden's rows are unchanged; only the comment at :351-352 changed. The diff's one bearing on this is that CLAUDE.md:53 now makes F2 a standing rule, so the slip now paraphrases the rule as it stood before F2.

**Grade: note.** The wrong claim is real and visible to players in Sweden and Germany, but it is older than this change and outside it.

**The skeptic's corrected fix.** Keep this out of the F1/F2/F7 commit. A player-visible text change would bring in the UI tier and owe a film. File it instead, on the K-1i row or as a D-PS follow-up put to Design: board 21c made "the DECLARED rule's paragraph" the head's slip, so the wording is Design's call.

Any new wording must also be true for Germany. The proposed "OR BY A BROADCASTER'S OWN PAGE QUOTING ITS LEADER" is still false there:
- the AfD candidacy is dated by ZDF's own sentence about a board decision, not a leader's quoted words;
- the Union's candidacy is dated by the CSU's page.

A form that fits all three countries is the CANDIDACIES slip's own phrase, for example "EACH ITEM IS DATED BY ITS SOURCE - THE PARTY'S OWN RECORD, OR WHERE A RULING SAYS SO A LEADER'S WORDS ON THE BROADCASTER'S PAGE". Change CampaignDeclared.cs:184 and the class doc at :18, "(the party's record, §621)", together. Review the slip again before Poland's campaign is staged, alongside §776's tracking note on the 'would support' tag.

### 26. Three register rows were orphaned by the §12 rewrite. Their doubt pointers, the header's claim, the fetch log and a code pointer are now false.

- **Lens:** hygiene - **reviewer:** defect - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:390

**The scenario.** The rewrite condensed §12 and dropped the only text citing three pages: [TD-I13] (old doubt 2, Kobosko 2023-06-30), [TD-I14] (old doubts 3 and 12, PSL's 2020-09-14 refusal) and [NL-P30] (old doubt 8, Dziemianowicz-Bąk). A scripted scan of §§1-12 finds them only in their own register rows (:390, :391, :392). Those rows still say '(doubt 2)', '(doubts 3 and 12)' and '(doubt 8)', and the doubts they point to no longer mention them. Four other places now say something false: (1) the header :4-5 says every register source is one 'the facts and §§1–11 cite by its tags, and the pages §12's doubts lean on'; (2) fetch_log.md:18 says the tree holds 'the pages §12's doubts lean on'; (3) DeclaredRedLines.cs:406 says 'the 2019 lists' lines are recorded there, not carried (its doubts 3 and 12)', but doubt 12 (:316) now says only 'not proposed', and PSL's 2020 line is recorded nowhere. Result: a reader following [TD-I14]'s pointer finds nothing, and the tree holds three digest-checked evidence files that no text uses.

**The fix proposed.** Either restore one clause per page in §12 (doubt 2: Kobosko [TD-I13]; doubts 3/12: PSL 2020-09-14 [TD-I14]; doubt 8: Dziemianowicz-Bąk [NL-P30]), or drop the three rows, their in-tree files and their SHA256SUMS lines and say so in the fetch log. Either way, correct the pointers and the code comment.

**The skeptic's evidence.** The core of the finding holds; two of its four side claims are overstated.

1. The three rows are orphaned. I scanned the working record with perl, tag by tag, leaving out each tag's own register row. [TD-I13], [TD-I14] and [NL-P30] are cited nowhere else (NONE). Every other register tag has at least one citation in the body. A grep of the repo, excluding Library, finds the three tags only in the record's register (:390-392), in SHA256SUMS.txt and in the old review Reviews/2026-10-04_s776_pl_declarations.md. At HEAD (cba1bd04) they were cited in the doubts:
   - [TD-I13]: doubt 2 (:295, Kobosko, "nie ma mowy o naszej współpracy z PiS") and doubt 9 (:302).
   - [TD-I14]: doubt 3 (:296, "PSL from 2020-09-14"), doubt 9 (:302) and doubt 12 (:305).
   - [NL-P30]: doubt 8 (:301, Dziemianowicz-Bąk).
   The new doubts no longer mention them: 2 (:306) cites [TD-P11] only, 3 (:307) cites [TD-P11] and [TD-P6], 8 (:312) cites only the PAP copies, and 12 (:316) reads "The 2019 chamber's keys - as built: not proposed (E2's scope is 2023)." §11 does not list the three pages either. The review s776 brought them in tree for exactly these doubts (review :1130: "The five pages the doubts lean on are held in tree and registered: [KONF-P3], [TD-P11], [TD-I13], [TD-I14] and [NL-P30]").

2. The register rows' pointers are now false:
   - :390 "(doubt 2)"
   - :391 "(doubts 3 and 12)"
   - :392 "(doubt 8)"
   The finding missed one more of the same kind. §2 :92 reads "both held after the vote (doubt 5, [KONF-P3])" and the row at :388 reads "(after the vote; doubt 5)". But the new doubt 5 (:309) cites only [KONF-P2] and [KONF-I14]. HEAD's doubt 5 carried Bosak's post of 2023-10-16.

3. The header (:4) is unchanged context. It describes the register as "the sources the facts and §§1–11 cite by its tags, and the pages §12's doubts lean on". Three rows are now in neither set. The part that does the work ("is stored byte for byte") still holds: all three files are tracked and match their SHA256SUMS digests (sha256sum checked).

4. Overstated, fetch_log.md:18: "Since §776's review they include the pages §12's doubts lean on" is still literally true. It says the tree includes those pages, and every page the new §12 cites is in tree. It no longer explains why three of the files are there, but it is not false.

5. Partly overstated, DeclaredRedLines.cs:406 (unchanged context): "the 2019 lists' lines are recorded there, not carried (its doubts 3 and 12)". Most of it still holds:
   - SLD 2019-10-17 is recorded in §4 [NL-P1] (:168).
   - PSL 2022-08-09 is recorded in §3 [TD-I4] (:122).
   - Doubt 3 (:307) still says "the members' earlier refusals are recorded, not carried".
   - Doubt 12 still says "not proposed".
   What is lost: PSL's 2020-09-14 line is no longer stated anywhere in the prose, and doubt 12 no longer lists the lines it points at.

Why minor and not a defect: these are cross-references in the documentation only. No fact, date, line, figure or formation changes, and no check fails. PolishDeclarationsDiagnostic.cs:75-83 only checks that tags cited by facts are register rows, and check (c) at :95-111 holds every row's file to its digest, so the orphans still pass. [NL-P30] is not load-bearing under F1: its own-page words, "Lewica nigdy nie wejdzie do rządu, w którym miałaby być skrajna, brunatna i faszyzująca prawica", are a cabinet-only restatement and contain no pledge to keep anyone from power. The review s776 graded the mirror case minor, on both its reviewer and skeptic passes (pages cited by the doubts but not registered, contrary to the header; review :105-107). The reason was the same: no game or diagnostic behaviour depends on it.

**The skeptic's corrected fix.** Keep the three files and re-cite each one where it now belongs, so the evidence stays in tree:
- Doubt 2: add a clause that Kobosko's 2023-06-30 magazine interview [TD-I13] is context under F2.
- Doubt 3 or doubt 12: add PSL's earliest refusal, 2020-09-14 [TD-I14] (Rzeczpospolita reporting Polsat News), which dates nothing under F2. §3's 2022-08-09 [TD-I4] stays the recorded member refusal. Doubt 12 should again list the 2019 lists' lines: PSL 2020-09-14 [TD-I14] and 2022-08-09 [TD-I4], SLD 2019-10-17 [NL-P1].
- §4: add Dziemianowicz-Bąk on NL's own page, 2023-07-15 [NL-P30], as a cabinet restatement of NL → Konf.
- Doubt 5: add back "held after the vote: Bosak on X, 2023-10-16 [KONF-P3]", or change :92 and :388 to point at §2 instead of doubt 5.

Then make each register row's parenthetical name the section that now cites it.

Leave the header (:4) and fetch_log.md:18 as they are once every row is cited again. Change DeclaredRedLines.cs:406's "(its doubts 3 and 12)" only if doubt 12 does not regain the list (for example to "its §§3-4 and doubt 12").

Alternative: drop the three rows, their files and their SHA256SUMS lines (all three are tracked at HEAD), say so in the fetch log, and narrow the code comment. That loses digest-held evidence for no gain.

### 27. §11 contradicts §5 and §12 on Budka's 2023-08-09 TVN24 page

- **Lens:** hygiene - **reviewer:** defect - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:270

**The scenario.** §11 lists Budka's "prawdziwą alternatywą" (2023-08-09) under 'Restatements of KO → PiS under F1, on pages that do not date it'. That quote and §5's "pogonimy Kaczyńskiego" are on the same saved page, KO/tvn24_2023-08-09_budka_inauguracja_kampanii.html (TVN24, datePublished 2023-08-09T05:53:35Z, out of tree). TVN24 is a broadcaster's own page, which F2 counts. §5 (:178-180) and §12 (:323) offer that same page to Elias as a possible earlier START for KO → PiS. Two problems follow: words dated before the line's 2023-10-12 start cannot 'restate' it, and 'pages that do not date it' tells Elias that the choice §12 puts to him is already closed. The page's own words are KO "jest prawdziwą alternatywą dla rządu PiS ... żeby zatrzymać tę złą władzę".

**The fix proposed.** Keep only OKO.press 2023-10-13 in §11's restatement bullet. Cite Budka 2023-08-09 once, in §5 and §12, as the earlier-start candidate, with its out-of-tree path.

**The skeptic's evidence.** THE CONTRADICTION (introduced by this change; at HEAD both items sat under "Not a refusal or not a candidacy").
- §11, record lines 270-271: "Restatements of KO → PiS under F1, on pages that do not date it: Tusk's "Polska będzie wolna od PiS-u" (2023-10-13, OKO.press); Budka's "prawdziwą alternatywą" (2023-08-09, earlier than the start, §12)."
- §5, lines 178-180: "Earlier and less plain: Budka at KO's campaign launch (TVN24, 2023-08-09, out of tree: "pogonimy Kaczyńskiego") ... - not read as the start (§12)".
- §12, line 323: the same page "would start it earlier if read as F1's pledge".

BOTH QUOTES ARE ON ONE PAGE. The capture is PoliSim-captures/sources/poland_declarations_2023/KO/tvn24_2023-08-09_budka_inauguracja_kampanii.html. Its SHA-256 is 84290bb2...018ce, which matches KO/SHA256SUMS.txt.
- Page facts: JSON-LD datePublished 2023-08-09T05:53:35Z; canonical URL on tvn24.pl (...st7284534); footer "Źródło: TVN24, PAP".
- Both quotes are in the body as dash direct speech:
  - "- Dzisiaj o poranku ... Października 15. pogonimy Kaczyńskiego. ... - mówił."
  - "- Dzisiaj jak nigdy ... Dlatego Koalicja Obywatelska jest prawdziwą alternatywą dla rządu PiS, jest prawdziwą i jedyną alternatywą, żeby zatrzymać tę złą władzę - ocenił Budka."
- The lead also puts the second quote inside quotation marks.

THE PAP CO-CREDIT DOES NOT RESCUE "DO NOT DATE IT".
- [PIS-I1] carries the identical footer "Źródło: TVN24, PAP".
- §12 (line 322) still offers [PIS-I1] as a possible start for PiS → KO.
- [KONF-I14] and [KO-I11] are TVN24 pages that date facts under F2.

"RESTATEMENT" BREAKS THE RECORD'S OWN VOCABULARY.
- Every other "restate" in the record comes after its line's start: lines 82, 88, 93, 96, 119, 131, 138, 157, 167, 170, 172, 267, 274.
- Words from before a start are called "Earlier" (§1 line 48, §5 line 178).
- The 2023-08-09 words come before KO → PiS's start of 2023-10-12 (§8 row 15; DeclaredRedLines.cs:441).

WORSE THAN THE FINDING SAYS.
- §11 treats the "żeby zatrzymać tę złą władzę" sentence as F1 content outright ("under F1").
- §12 offers Elias only the weaker "pogonimy Kaczyńskiego" from the same page, and only conditionally.
- The session's own verifier (scratchpad f12/verify_KO-7.txt and f12/find_KO.txt) found that the "prawdziwą alternatywą" sentence names "rządu PiS". It therefore needs no premise that a pledge against a leader keys his party, and it is the earliest qualifying KO → PiS statement.
- The real open questions are the speaker (a club chairman) and the setting (a KO press conference that TVN24 filmed). Whether the page can date anything is not one of them.

WHY MINOR, NOT DEFECT.
- Only record prose is wrong. PolishDeclarationsDiagnostic reads only §8's table and the register (lines 56-110), never §11 or §12.
- The code fact (DeclaredRedLines.cs:441-442) and §8 row 15 keep the 2023-10-12 start, consistent with §5 and §12.
- Every candidate start comes before polling day, so the polling-day reading and F7's acceptance test (R1 = KO+TD+NL) are unchanged.
- §11's "(..., §12)" cross-reference partly points the reader to the open choice.

**The skeptic's corrected fix.** 1. §11, lines 270-271: keep only Tusk's OKO.press words of 2023-10-13 in the restatement bullet. The page is a portal and the words come after the start, so both labels fit.
2. §5's "Earlier" sentence and §12's KO → PiS start item (line 323): cite Budka's page once, with both sentences:
   - "Dlatego Koalicja Obywatelska jest prawdziwą alternatywą dla rządu PiS, jest prawdziwą i jedyną alternatywą, żeby zatrzymać tę złą władzę" (it names PiS's government, so it is the plainer of the two);
   - "Października 15. pogonimy Kaczyńskiego".
3. Cite the page by its out-of-tree path, KO/tvn24_2023-08-09_budka_inauguracja_kampanii.html, as §11 cites the PCh24 page.
4. Name the real open questions: a club chairman's words, at a KO press conference that TVN24 filmed, on a page credited "TVN24, PAP".
5. If the page gets a register tag instead, it must be brought in tree at its digest, because the diagnostic's check (c) requires every register file to be held in tree.
6. Optional: change "Earlier and less plain" in DeclaredRedLines.cs:442 (and §5 line 178) to give that same reason.

### 28. New quotes lack the record's own *(decoded)* mark although the saved bytes carry &nbsp; inside them

- **Lens:** hygiene - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:112

**The scenario.** The header (:3-4) requires *(decoded)* wherever the bytes carry entities or markup inside the quoted sentence. The saved bytes are: [TD-P11] 'po&nbsp;to, żeby&nbsp;odsunąć PiS od&nbsp;władzy, ale&nbsp;odsuniemy...' (§3 :112; the pre-F1 doubt 2 marked these same words *(decoded)*); [NL-P31] 'aby te&nbsp;listy ... od&nbsp;władzy' (§4 :169); [NL-P32] 'by&nbsp;odsunąć PiS od&nbsp;władzy' and '<b>w&nbsp;sprawie wspólnych rządów opozycji</b>' (§4 :170-171). The register's 'z dnia 21 stycznia 2023 r. w sprawie wspólnych rządów opozycji' (:394) spans two <p> elements, and 'Opracowanie: Jan Matoga' (:327) also has markup inside. A literal search of each saved file for the quoted text fails, which is exactly what the mark is there to warn.

**The fix proposed.** Mark each of these quotes *(decoded)*, as the older §3 and §4 quotes are.

**The skeptic's evidence.** I could not refute it. I searched the saved bytes literally with perl, as UTF-8 bytes via index(), with no decoding. Both the in-tree and out-of-tree copies were checked, and their SHA-256 digests match.

**The rule still stands.** Header lines 3-4 are outside the diff (the first hunk starts at line 10). They say: "where the bytes carry entities or markup inside the sentence, the quote is the decoded text, marked *(decoded)*."

**The new quotes that carry no mark:**
- **[TD-P11], §3 line 112.** The bytes in TD/pl2050_2023-05-15_trzecia-droga-polski-2050-i-psl.html read "Musimy wygrać te wybory po&nbsp;to, żeby&nbsp;odsunąć PiS od&nbsp;władzy, ale&nbsp;odsuniemy PiS od&nbsp;władzy tylko wtedy, kiedy ludzie będą mieli wybór." The quoted sentence has 0 literal hits anywhere in the file, including the meta tags, and 1 hit once &nbsp; is read as a space. The gloss at lines 114-116 ("pon. 15 maj 2023" [TD-P11]) carries no mark either.
- **[NL-P31], line 169.** The bytes read "aby te&nbsp;listy były zwycięskie, aby odsunąć PiS od&nbsp;władzy." That is 0 literal hits and 1 after &nbsp;.
- **[NL-P32], lines 170-171.** The bytes read `<b>w&nbsp;sprawie wspólnych rządów opozycji</b>` and "Ciężko pracujemy, by&nbsp;odsunąć PiS od&nbsp;władzy <br>". Each is 0 literal hits and 1 after &nbsp;.
- **Doubt 7, line 311 (the finding missed this one).** It quotes "odsunąć PiS od władzy" for [NL-P31] and [NL-P32] with no mark. Those words are not plain text on either page; they are plain text only on [KO-I12].

**The convention does cover &nbsp;-only decodes.** Every quote HEAD marks *(decoded)* that I tested needs only that decode: 0 literal hits and at least 1 after &nbsp;. That covers [NL-P17] (two quotes), [NL-P25] (two), [NL-P3], [NL-P29], [TD-P9], and HEAD's doubt 2, "odsunąć PiS od władzy" *(decoded)* [TD-P11].

**This repeats a miss that was already fixed.** The §776 review flagged exactly this one: Reviews/2026-10-04_s776_pl_declarations.md:1438-1439 ("[TD-P11]'s bytes read 'odsunąć PiS od&nbsp;władzy' (0 plain matches) … quotes it without the mark"). Its fix was :1463, recorded as done at :2061. This rewrite deleted that marked doubt-2 text and quotes the same words from the same page again, now unmarked.

**Not affected.** The other new quotes are plain text in their bytes: [PIS-P2], [PIS-I1], [TD-P6] (two quotes), [KO-I12], [KO-I11], [TD-I5], [KONF-I14], [KONF-I19], [KONF-I23]'s "chcemy zakończyć…", [KO-I7], plus the out-of-tree pages for Biedroń, Budka and RMF's post.

**Narrowing two of the finding's examples:**
- **Register line 394.** Register datelines are unmarked by practice. The [NL-P17], [NL-P25], [NL-P29] and [NL-P30] rows all carry "2023&nbsp;r." in their bytes and have no mark. §4 does mark [NL-P17]'s dateline, and the §776 fix added [NL-P30]'s dateline unmarked. So that example follows the register's own practice and is not a defect here.
- **"Opracowanie: Jan Matoga", line 327.** Markup does sit inside it (`Opracowanie:` then a newline and span/a tags before `Jan Matoga`), so it has 0 literal hits. But it is a byline, not a sentence, so this one is weak.

**Severity.** No code reads the marks (the §776 review, line 1448), so the game is unaffected. This is record hygiene against the record's own stated rule: minor.

**The skeptic's corrected fix.** Add *(decoded)* to the prose quotes whose bytes carry &nbsp; inside, placed the way the record already does it:
- **§3, [TD-P11] block quote (line 112).** Put the mark in the gloss just before the tag, as §4 does for [NL-P3], or right after the block quote.
- **§4, line 169.** Write it as "…aby odsunąć PiS od władzy." *(decoded)* [NL-P31].
- **§4, line 170.** Write "w sprawie wspólnych rządów opozycji" *(decoded)*.
- **§4, line 171.** Write "Ciężko pracujemy, by odsunąć PiS od władzy" *(decoded)* [NL-P32].
- **§12, doubt 7 (line 311).** Mark "odsunąć PiS od władzy" *(decoded)*. It is decoded on [NL-P31] and [NL-P32] and plain text on [KO-I12].
- **Optional.** Mark "Opracowanie: Jan Matoga" *(decoded)* at line 327.

Leave the register row at line 394 alone. Register datelines are unmarked throughout ([NL-P17], [NL-P25], [NL-P29], [NL-P30] all carry "2023&nbsp;r." without a mark). Changing that would be a separate, register-wide decision.

### 29. '(doubt 5, [KONF-P3])' points to a doubt that no longer discusses the after-the-vote post

- **Lens:** hygiene - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:92

**The scenario.** §2 :92 says the coalition halves were 'both held after the vote (doubt 5, [KONF-P3])', and the register row :388 says '(after the vote; doubt 5)'. The rewritten doubt 5 (:309) rules the support half by F1 and never mentions [KONF-P3] or the 2023-10-16 post. So the evidence the pointer promises (Bosak on X: 'nie wejdziemy w koalicję ani z PiS ani z PO, dzień po wyborach jest nadal aktualna', coalition half only) is no longer anywhere in the record.

**The fix proposed.** Restore that clause to doubt 5, or carry the quote in §2 and drop the 'doubt 5' pointer in both places.

**The skeptic's evidence.** I tried to refute it and could not. The pointer is stale.

Working tree, G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:
- :92 (§2, inside "The coalition half restated" sentence): "toward KO: only 2023-08-02 [KONF-P1] and 2023-10-11 [KONF-I25]; both held after the vote (doubt 5, [KONF-P3])."
- :388 (register): "| [KONF-P3] | ... | created_at 2023-10-16T08:29:58Z, isEdited false (after the vote; doubt 5) |"
- :309, the rewritten doubt 5, in full: "5. Konfederacja's support half - decided by F1: "chcemy zakończyć rządy PiS" (its own post, 2023-07-06 [KONF-P2]) and "Nie zamierzamy umożliwić powrotu Tuskowi do władzy" (TVN24, 2023-07-13 [KONF-I14]) are lines; one way, as built." It does not mention [KONF-P3], 2023-10-16 or Bosak.

Searching the working-tree record for "KONF-P3" finds only :92 and :388. Searching for "nadal aktualna", "Nie sprzedamy" and "coalition half only" finds nothing. HEAD's doubt 5 (HEAD:298) did carry the post: "The line held after the vote: Bosak on X, 2023-10-16, "nasza deklaracja z kampanii wyborczej, że nie wejdziemy w koalicję ani z PiS ani z PO, dzień po wyborach jest nadal aktualna" [KONF-P3]. [record: [KONF-P3] carries the coalition half only ...]". The diff removes that (-351, +378), but :92 and :388 were not changed to match.

The saved bytes match the old quote. Konf/x_bosak_1713834658134138996_syndication.json decodes to "...że nie wejdziemy w koalicję ani z PiS ani z PO, dzień po wyborach jest nadal aktualna. Nie sprzedamy się...". Its sha256 is a8d8a865..., matching SHA256SUMS.txt:22.

One correction to the finding's wording: the source is not entirely gone from the record. The [KONF-P3] tag still leads to the register row, and check (c) holds the file at its digest. What is lost is the quote and its "coalition half only" bracket. The pointer is also worse than merely dangling. Reviews/2026-10-04_s776_pl_declarations.md:1271-1311 (finding 3) added that bracket to stop the post being read as backing the SUPPORT half. The pointer now sends the reader to a doubt that is only about the support half, with the bracket gone. §2's placement inside the coalition-half sentence still names the half, so the impact is a broken cross-reference, not a wrong fact.

No check catches it. PolishDeclarationsDiagnostic.cs:74-83 checks only that fact tags are register rows, not doubt pointers or unreferenced rows.

The same rewrite left three more register pointers stale in the same way:
- :390 [TD-I13] "(doubt 2)"
- :391 [TD-I14] "(doubts 3 and 12)"
- :392 [NL-P30] "(doubt 8)"

Searching the record finds each of these tags only on its own register row. The new doubts 2, 3, 8 and 12 (:306, :307, :312, :318) do not cite them. That contradicts the header at :4, which says the register lists "the pages §12's doubts lean on". [TD-P11] "(doubt 2)" at :389 is still correct.

Severity is minor: a documentation cross-reference with no effect on the game, the formation or any test, graded as the s776 review graded the same post's citation.

**The skeptic's corrected fix.** Prefer the finding's second option, and keep the half named. At :92 replace "(doubt 5, [KONF-P3])" with: Bosak on X, 2023-10-16, "nasza deklaracja z kampanii wyborczej, że nie wejdziemy w koalicję ani z PiS ani z PO, dzień po wyborach jest nadal aktualna" [KONF-P3] - the coalition half only. At :388 change "(after the vote; doubt 5)" to "(after the vote; §2)".

If the clause goes back into doubt 5 instead, it must bring back the bracket "[record: [KONF-P3] carries the coalition half only ("nie wejdziemy w koalicję ani z PiS ani z PO"); its "Nie sprzedamy się" is not a support line in terms]". Otherwise it reopens s776 review finding 3, because doubt 5 is now about the support half.

In the same pass, fix the other stale register pointers: [TD-I13] "(doubt 2)" at :390, [TD-I14] "(doubts 3 and 12)" at :391 and [NL-P30] "(doubt 8)" at :392. Either cite each tag again where its doubt still discusses it, or re-mark them as "(§776's doubt N; not relied on since F1/F2)". Then align the header at :4 ("the pages §12's doubts lean on") with whichever choice is made.

### 30. Excluding RMF24's relay is an unstated reading of F2, and doubt 4 rests on it

- **Lens:** hygiene - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:20

**The scenario.** F2's words admit 'a leader's spoken words ... quoted verbatim on the broadcaster's ... own page, dated by that page'. RMF24's own page of 26 June [KONF-I11] quotes Mentzen verbatim: 'Jeśli obie partie, czy to PiS, czy PO, zgodzą się realizować nasz program, to wszystko jest na stole - mówi Sławomir Mentzen w wywiadzie dla "Super Expressu"'. It is excluded only by an added condition: the words must have been said to that broadcaster. The record presents this as a consequence of F2, not as a reading: 'So a page relaying another outlet's interview ... RMF24 relaying Super Express ... date nothing' (:20-22), :71, and doubt 4 'decided by F2' (:308). The code says the same in DeclaredRedLines.cs:418/420/422/424 ('which F2 does not count'). §12's 'Left to Elias' list (:321-330) lists 'always wins' as a READING but not this one. Under the literal reading, facts 3-7 end on 2023-06-26 and Konf → TD/NL/MN restart on 2023-08-02 [KONF-P1]. That gives a different §8 table; polling day is unchanged.

**The fix proposed.** State it as 'READING, stated', next to the 'always wins' reading, and add it to §12's list for Elias.

**The skeptic's evidence.** I couldn't refute this. The finding is right on every point, and one fact the record leaves out makes the case slightly more mixed than the finding says.

1. **The saved RMF24 page qualifies under F2's plain words.** The file is `raw/declarations_2023/CROSS/rmf24_2023-06-26_mentzen-zmienia-zdanie-koalicja-z-pis.html` [KONF-I11].
   - It is dated "Poniedziałek, 26 czerwca 2023 (08:31)" (`datePublished` 2023-06-26T08:31:44+02:00).
   - It carries: "Jeśli obie partie, czy to PiS, czy PO, zgodzą się realizować nasz program, to wszystko jest na stole - mówi Sławomir Mentzen w wywiadzie dla "Super Expressu". Jeśli ktoś będzie chciał realizować nasz program dotyczący obniżania i upraszczania podatków, to ja nie mam nic przeciwko - dodaje polityk."
   - Both sentences match Super Express's own transcript [KONF-I8] word for word, so this is direct speech, not a paraphrase.
   - RMF24 is a broadcaster's own page; the record itself treats RMF24 as RMF FM's own record for [KONF-I4].
   - Taken literally ("the broadcaster's ... own page, dated by that page"), this page dates the 26 June opening.

2. **The exclusion depends on a reading, and the record presents it as F2 itself.**
   - Lines 20-22: "So a page relaying another outlet's interview or an agency's wire - ... RMF24 relaying Super Express - ... date nothing here".
   - Line 71: "no broadcaster's or agency's own page carries the words as said".
   - Doubt 4 (line 308): "decided by F2: ... none the broadcaster's or the agency's own page for those words".
   - `DeclaredRedLines.cs` lines 418, 420, 422 and 424: "which F2 does not count" and "pages F2 does not count".
   - The conditions "for those words" and "as said" (the words must have been said to that outlet) are not in F2. §652's condition ("If a saved MP publication is earlier, use that instead") does not supply them either.
   - In the same paragraph, line 22 labels only "always wins" as "READING, stated". §12's list for Elias (lines 321-330) has "always wins" and a different RMF24 question ([KONF-I4]'s verbatim-ness), but not this one.

3. **The 26 June opening was one of the cases Elias accepted.**
   - At HEAD, K-1i (3) put to Elias the facts dated "as a broadcaster's or an agency's page reports them - the June lines and their lift".
   - The s776 review (`Reviews/2026-10-04_s776_pl_declarations.md`:37) says the lift rests on [KONF-I8], [KONF-I10] and [KONF-I11].
   - F2's answer begins "Accepted". The record now reverses that listed case by a reading and does not flag it.

4. **The fact the record leaves out.** The page's credit is "Źródło: RMF24/PAP", which the register gives as "RMF24" only. So the page is a broadcaster's page carrying PAP's wire, which in turn relays Super Express. That makes the record's reading more defensible, but F2's broadcaster half still covers the page if read literally, so it stays a reading.

5. **Why minor, not a defect.** Nothing in play moves today:
   - On polling day the same lines stand, so F7's acceptance test (KO+TD+NL) holds either way.
   - `LiveCampaignSetup` stages no Polish run-up or campaign, so the DECLARED page shows Poland's timeline on no screen (`GameController.CampaignDeclared.cs`:33-34).
   - No Polish mid-term round opens, because its confidence rules are Unsourced.
   - What differs is the dated record and `PolandTimeline`: facts 3-7 would end on 2023-06-26, and Konf → TD/NL/MN would restart on 2023-08-02 [KONF-P1]. A Polish world opens on 2023-02-19, so these dates fall inside its run-up.

**The skeptic's corrected fix.** 1. **Record header (lines 20-22).** After "So a page relaying ...", add a second stated reading next to the "always wins" one: "READING, stated: F2's 'the broadcaster's or news agency's own page' is read as the page of the outlet that took the words down. A broadcaster's page relaying another outlet's interview or carrying an agency's wire dates nothing - RMF24's 26 June page [KONF-I11], credited 'RMF24/PAP', relays Super Express's interview. Read literally, [KONF-I11] would date the 26 June opening: facts 3-7 would end that day and Konf → TD/NL/MN would restart on 2023-08-02 [KONF-P1]. Polling day is unchanged."
2. **Doubt 4 (line 308).** Change "decided by F2" to "decided by F2 as read (the header's second READING)".
3. **Line 71.** Name the reading there as well.
4. **§12's "Left to Elias" list.** Add the item beside "always wins".
5. **Register.** Write the publisher as "RMF24 (RMF24/PAP)".
6. **Code comments.** In `DeclaredRedLines.cs` at lines 418, 420, 422 and 424, change "which F2 does not count" to "which F2, as the record reads it, does not count". Follow the claim convention: no figures transcribed.

### 31. Sweden's record still calls KD's date an open extension after F2 ruled it

- **Lens:** hygiene - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/sweden/2026/coalition_declarations_2026.md:39

**The scenario.** The KD → S row in the wired-lines table reads 'dated 2026-09-02 (Busch's own words in SVT's live report - spoken, not a party publication, so an EXTENSION of §621's precedent, for Elias)'. Three places now say it is ruled: DeclaredRedLines.cs:351-352 ('RULED by Elias's ruling F2'), the K-1i row ('(3) was answered by F2 ... KD's date stands') and CLAUDE.md:53. The KD fact's basis sends readers to this file (SwedenSource), where the question still reads as open. The MP row two lines up (:37) was updated when §652 ruled MP's date, so the table's own practice is to update.

**The fix proposed.** Mark row 39 'ruled by F2 (2026-10-05)'. The dated 'AS WIRED' note at :710-712 can stay as history.

**The skeptic's evidence.** I tried to refute the finding and could not. The record row is stale, and this change is what made it inconsistent.

1. The row is unchanged. `git diff HEAD --stat -- ElectionsData/sweden` and `git diff --cached` are both empty, so neither the working tree nor the staged changes touch the file. Line 39 still reads: "dated 2026-09-02 (Busch's own words in SVT's live report - spoken, not a party publication, so an EXTENSION of §621's precedent, for Elias) | [KD-I1], [KD-I3]".

2. The change marks the question as ruled in three other places:
   - `DeclaredRedLines.cs:351-352`: "-// ... so an EXTENSION of §621's precedent (a post on X), stated and put to Elias (§639)" is replaced by "+// ... RULED by Elias's ruling F2 (K-1i (3), first put with §639)". The `SwedenTimeline` summary at :312-314 is also rewritten from "the extension K-1i (3) puts to Elias" to "Elias's ruling F2".
   - `POLISIM_FEATURE_LIST.md:148` (K-1i): "**(3) was answered by F2** ... KD's date stands (Busch's words on SVT's own live page)".
   - `CLAUDE.md:53`: "and (F2, 2026-10-05, §780) a leader's spoken words count, quoted verbatim on the broadcaster's or news agency's own page and dated by that page".

3. The KD fact's basis sends readers to this record. `KdRefusesAndersson` (`DeclaredRedLines.cs:131-134`) ends `+ SwedenSource`, and `SwedenSource` at :32 is "See ElectionsData/sweden/2026/coalition_declarations_2026.md". A reader who follows that pointer finds the question still marked open.

4. F2 does cover KD's date:
   - The record's own line 446 quotes SVT verbatim: *"Vi kommer att vara beredda att rösta nej till Magdalena Andersson ända fram till ett nyval, säger Busch."*
   - KD's primary is a GAP (lines 34 and 446), so no date from the party's own record can win.
   - The date stands, ruled, and only the "for Elias" status is out of date.

5. The table's own practice is to update the row. §652's commit (76a0dd10) rewrote the MP row (line 37) from "...a stated deviation from §621's first rule, for Elias" to "(ruled 2026-09-29, §652: ...)". It left the dated AS WIRED paragraph alone: :709 still says "MP 2026-04-04", and :710-712 still says "Two dates stretch §621's rules, for Elias". That is the same split the finding proposes.

6. No runtime effect. Nothing in `Assets/Editor` or `Tools` parses this row. The only reference, `OfficeTestDiagnostic.cs:330`, is a message string. So this is record hygiene, not a defect: the record a basis string cites still lists a ruling as owed to Elias that he has already given. The rating stays minor.

**The skeptic's corrected fix.** In `ElectionsData/sweden/2026/coalition_declarations_2026.md:39`, replace "(Busch's own words in SVT's live report - spoken, not a party publication, so an EXTENSION of §621's precedent, for Elias)" with this, following the MP row's §652 wording: "(Busch's own words, quoted verbatim on SVT's own live page [KD-I1]; ruled 2026-10-05, F2, §780: a leader's spoken words count when quoted verbatim on the broadcaster's or news agency's own page, dated by that page; KD's own record is a GAP, so no earlier own-record date wins; the June report [KD-I3] is the newsroom's paraphrase and never counts)". Leave the dated AS WIRED, K-1h and K-1g paragraph (:706-712) as history, as §652 did. Also list the file in §780's Records line.

### 32. Bare '(§10)' and '(§12)' in the new basis strings mean the record's sections, but everywhere else in the file '§N' means a COMPLETED.md section

- **Lens:** hygiene - **reviewer:** minor - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/DeclaredRedLines.cs:440

**The scenario.** Every other '(§NNN)' in DeclaredRedLines.cs (§618, §621, §639, §652) is a COMPLETED.md section. COMPLETED.md §10 is 'Master Sequence step 5e — Phases A and B, Phase C batches 1–3' and §12 is 'Step A design and audit artifacts'. So a reader of fact 14's '...the derived NL-Konf line blocks support (§10)' (:440), or of facts 2 and 15's 'not read as the start (§12)' (:416, :442), lands on unrelated sections. The same summary already uses the unambiguous '(its doubts 3 and 12)'.

**The fix proposed.** Write '(the record's §10)' and '(the record's §12)'.

**The skeptic's evidence.** The finding is real but small. I'm grading it note, down from minor.

What I confirmed:
- **The three bare references are new.** DeclaredRedLines.cs :416 (PiS→KO) and :442 (KO→PiS) end with "not read as the start (§12)". :440 (NL→Konf) ends with "the derived NL-Konf line blocks support (§10)". HEAD's version of the file has no unqualified one- or two-digit § anywhere.
- **The file's own convention is the other way.** Every other unqualified § in the file is a COMPLETED.md section: §618 (:53), §621 (:270, :312, :326, :329, :341, :403), §639 (:276, :352), §644, §652 (:313, :348, :395), §653, §657, §679, §705 (:357), §776 (:399). The one record section it cites is named by its path: "`ElectionsData/germany/coalition_declarations_2025.md` §3" (:42). The same summary also writes "(its doubts 3 and 12)" (:406).
- **The wrong targets are unrelated.** COMPLETED.md :439 is "## 10. Master Sequence step 5e — Phases A and B, Phase C batches 1–3". COMPLETED.md :526 is "## 12. Step A design and audit artifacts".
- **The intended targets are right.** The record's §10 (coalition_declarations_2023.md :245, "Not here — DERIVED") says "PiS–NL and NL–Konf, both support-blocking". The record's §12 (:276) lists, under "Left to Elias", "PiS → KO's start: Kaczyński's TVN24 words of 2023-07-23…" and "KO → PiS's start: Budka … or Tusk on Polsat News…". So the pointers are correct, just not qualified.
- **The same change qualifies this record elsewhere.** PolishDeclarationsDiagnostic.cs :20 says "the record's timeline table (its §8)" and :52 says "(a) the record's §8 is the array". Both were already there at HEAD.

Why the trailing pointer doesn't settle it. Each string ends with `+ PolandSource` ("See ElectionsData/poland/coalition_declarations_2023.md"), so the record is named right after "(§12).". But Sweden's facts in this file use the same shape with § meaning COMPLETED.md: "(ruled §621: the party's own record, 8 September). " + SwedenSource2022 (:326). In Elections code a bare §12 has a third reading too: the campaign spec's §12 (CampaignActions.cs :6 "SPEC §12 — the eight campaign actions"; CampaignAi.cs :143, :483).

Why it's only a note:
- It changes no behaviour, and it doesn't breach the claim convention.
- The two-digit number next to this file's three-digit COMPLETED.md references, plus the trailing "See <record>", means a reader is unlikely to be misled for more than a moment.
- The Declared page shows only the source keys (GameController.CampaignDeclared.cs :153, SourceKeysOf). The full text is printed only by FormationProposal.cs :171 and the diagnostics, where the record path follows immediately.

**The skeptic's corrected fix.** In DeclaredRedLines.cs, change the text inside three basis strings: at :416 and :442 write "not read as the start (the record's §12)", and at :440 write "the derived NL-Konf line blocks support (the record's §10)". This matches the diagnostic's existing "the record's §8" and the path-named §3 at :42. Nothing else needs to move:
- PolishDeclarationsDiagnostic reads only these parts of the basis text: the [X-Yn] tags (:79, :88), the DECLARED prefix and the PolandSource suffix (:81), and the PolandSpokenWords mark (:89). The wiring key at :202 compares Basis against itself.
- FormationSweepDiagnostic's digest prints no basis, only outcome, options, power and blocked A-B pairs (:51-55, :131), so PinnedDigest stays as it is.

Nothing in the record needs changing: inside coalition_declarations_2023.md a bare §N already means the record's own section.

### 33. The new F2 check proves less than its message prints

- **Lens:** hygiene - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PolishDeclarationsDiagnostic.cs:84

**The scenario.** The check tests only the letter of the first tag: P without the mark, I with it. But the register's I-class also holds newspapers, portals and agency relays that the record says date nothing: [TD-I5] GazetaPrawna (PAP), [KONF-I10] Bankier (PAP), [NL-I6] WP, [PIS-I5] Super Express. If fact 10 were re-dated to [TD-I5] with `PolandSpokenWords` added, the check would pass and print 'a leader's words on a broadcaster's own page', which is the exact F2 violation the header describes.

**The fix proposed.** Hold the first tag to an explicit set of qualifying broadcaster or agency own pages (for example, from the register's publisher column), or narrow the printed claim to 'an [X-In] page'.

**The skeptic's evidence.** The finding holds. I could not refute it.

1. **What the check actually tests.** In G:/UNITY/Projects/PoliSim/Assets/Editor/PolishDeclarationsDiagnostic.cs, lines 86-91 take the first tag's single class letter (`Regex.Match(f.Basis, @"\[[A-Z]+-([A-Z])\d+\]")`). They set `ownRecord = letter == "P"` and `spoken = f.Basis.Contains(PolandSpokenWords)`, and fail a fact only if `!first.Success || ownRecord == spoken`. So any non-P first tag plus the mark passes. Line 92 then prints "ok F2: every fact is dated by its first tag - ... carrying F2's mark, a leader's words on a broadcaster's own page".
   - The check never compares the fact's From date with the page's date either, so "dated by its first tag" is not tested.
   - Nothing else in the file holds the marked facts to particular pages. A grep for PolandSpokenWords, KONF-I4, KONF-I14, KO-I11 and publisher finds only lines 29, 89 and 92-93.

2. **What the I-class holds.** The register in ElectionsData/poland/coalition_declarations_2023.md (from line 332) has these I rows, among others:
   - [TD-I5] "GazetaPrawna.pl (PAP)"
   - [KONF-I10] "Bankier.pl (PAP)"
   - [NL-I5] and [NL-I7] "GazetaPrawna.pl (PAP)"
   - [NL-I6] "Wirtualna Polska"
   - [PIS-I5] "Super Express"
   - [KONF-I19] "Do Rzeczy (Gość Radia ZET)"
   - [TD-I10] "naTemat.pl (TOK FM)"
   - [KO-I10] "Rzeczpospolita"

   The record's header says such pages "date nothing here". Line 118 says "GazetaPrawna's PAP copy [TD-I5], which F2 does not count", and line 148 says "PAP via *GazetaPrawna* ... words F2 does not count".

3. **The failing path, traced** (scratchpad emulation of lines 86-91, f2check_sim.pl):
   - On today's 15 facts, 0 are misdated and the result is true: the 7 marked facts' first tags are [KONF-I4] five times (RMF24's own debate), [KONF-I14] (TVN24 Fakty po południu) and [KO-I11] (TVN24 Fakty).
   - The finding's scenario passes: fact 10 (TD->PiS) with first tag [TD-I5] and the mark gives misdated=0. So do [PIS-I5], [NL-I6], [KONF-I10] and [KONF-I19] as first tag with the mark.
   - **This is not hypothetical.** HEAD (cba1bd04) has the same shape. Running the predicate over HEAD's PolandTimeline with only PolandExtension renamed to PolandSpokenWords gives "misdated now: 0" for all 17 facts. That includes TD->PiS (first [TD-I5]) and NL->Konf support-blocking (first [NL-I5]): exactly the two F2 violations this change fixed by hand (TD->PiS re-dated to [TD-P11]; NL->Konf's support fact removed). The new check would have printed "ok ... a leader's words on a broadcaster's own page" over both.

4. **The comment's guarantee is false.** Lines 84-85 say "a broadcaster's or an agency's page ([X-In]) ...; a fact dated by a page of neither kind cannot be written so". A fact dated by a newspaper, portal or PAP relay is of neither kind, and it can be written with the mark and pass. Under CLAUDE.md's claim convention that is a false claim about the code in a source comment. Neither the record nor the diagnostic states this limit anywhere.

**Severity: minor, not a defect.** The data is correct today and no output is wrong now. But the guard's result does not depend on the thing its message names, which is the failure class EvidenceDiscriminationCheck calls this project's dominant one. Against its own predecessor's data, it misses exactly the violations F2 exists to rule out.

**The skeptic's corrected fix.** **Preferred fix:** make the check able to fail on the scenario, and keep the record as the one source.
- Mark each register row that F2 counts in ElectionsData/poland/coalition_declarations_2023.md (for example "(F2)" in the publisher cell). Today that is the rows for [KONF-I4], [KONF-I14] and [KO-I11]: a broadcaster's or news agency's own page, never a relay ("(PAP)", "reporting ...", "an RMF FM interview", "Gość Radia ZET"), a newspaper or a portal.
- In PolishDeclarationsDiagnostic, read the publisher cell with the same `Split('|')` that check (c) already uses (cells[3]). A fact that carries PolandSpokenWords must have a first tag on a row marked F2; a fact without the mark must have a first [X-Pn] tag.
- Optionally, also hold each fact's From to the first tag's page date, which is the "dated by" half of the message.
- A closed set of qualifying tags in the diagnostic would also work, but the marker keeps the ruling's application in the record.

**Minimum fix, if the semantic test is left out:**
- Narrow line 92's message to what the predicate tests: "F2: every fact's first tag is a party's own page ([X-Pn]) exactly when the fact carries no F2 mark; whether a marked fact's page is a broadcaster's own and verbatim is the register's to state, not this check's".
- Delete "a fact dated by a page of neither kind cannot be written so" from lines 84-85, and stop equating [X-In] with "a broadcaster's or an agency's page". The I-class also holds newspapers, portals and PAP relays.

### 34. §780 is not anchored, and the record and the re-pin cite no section for F1/F2

- **Lens:** hygiene - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/CLAUDE.md:53

**The scenario.** CLAUDE.md:53, CLAUDE.md:54 and the K-1i row (POLISIM_FEATURE_LIST.md:148) point to COMPLETED.md §780. COMPLETED.md ends at §777, and nothing in the working tree claims §778 or §779 (the staged veto work cites §770/§773/§775; F8's PS-6 row cites none). If this ruling's record lands as §778, all three pointers dangle. Meanwhile the record header (:3, '§776'), the DeclaredRedLines comments and the FormationSweepDiagnostic.cs:129 re-pin ('Elias's rulings F1 and F2: re-pinned') cite no section, although every earlier re-pin entry names its §.

**The fix proposed.** Fix the number when the record section is written, and cite it in the record header and in the re-pin entry.

**The skeptic's evidence.** The finding has two halves, and only the second one holds, and only in a narrow form.

HALF A, "§780 is not anchored, the pointers dangle": REFUTED.
- The facts it states are true. CLAUDE.md:53 reads "(F2, 2026-10-05, §780)", CLAUDE.md:54 reads "(F1, 2026-10-05, §780)", and POLISIM_FEATURE_LIST.md:148 (the K-1i row) reads "`COMPLETED.md` §780".
- COMPLETED.md is unchanged against HEAD, and its last section is "## 777." at line 37193.
- `git grep` of both the working tree and the index finds no §778 or §779.
- The "lands as §778" path is not the plan. The session scratchpad lays the series out:
  - rec778.md opens with "## 778. RULING F3 INSTALLED: B1 WIDENED, THEN A SEEDED DRAW ...", so F3 (the staged veto work) is §778.
  - s779_edits.pl:2 reads "# §779: F5's gate hook ...", and rev778_done1.md:20 reads "F5 is the next commit (§779)".
  - claude_f1f2.pl and fl_k1i.pl, the scripts that wrote §780 into CLAUDE.md and the K-1i row, were written at 11:10-11:11, knowing that series. This change is the third commit, so §780 is its number.
- The repo appends the record in the change's own commit. s776 (22796935) added COMPLETED.md §776 (+103 lines) in the same commit that cited §776 across 11 other files. A pointer to a section not yet appended is therefore the normal state before a commit. It dangles only if the series is reordered.

HALF B, "the record and the re-pin cite no section for F1/F2": TRUE for the re-pin entry and the code comments, FALSE for the record's header.
- The new entry at FormationSweepDiagnostic.cs:129 begins "// Elias's rulings F1 and F2: re-pinned - ...", with no § and no date. Every earlier entry on that line names its §: "§776: re-pinned", "§766: re-pinned", "§705: re-pinned", "§683: re-pinned", "(§652)".
- Outside CLAUDE.md and the K-1i row, §780 appears nowhere. Two examples:
  - DeclaredRedLines.cs:393: "Elias's ruling F2 (2026-10-05; K-1i (3) answered)".
  - PolishDeclarationsDiagnostic: "Read again under Elias's rulings F1 ... F2 ... F7".
- In HEAD's committed code, 94 of the 97 lines that cite "ruling [A-E]n" carry a § on the same line. Sweden's 2026 record cites "(ruled 2026-09-29, §652: ...)".
- "F2" is not unique in COMPLETED.md, which weakens a label-only pointer: W-F2 (§83), P-F2, P6-F2 (line 29805), F2e (line 30811), and "F2 — the rate-cap note" (line 20152).
- The record's header at :3 is NOT a deviation. Its Class line names the sourcing pass. Germany's record still cites "COMPLETED.md §705" on its :3 even though §776 edited its body. The Polish record already cites F1 and F2 by label, date and verbatim words at :18-:26.

Why the severity is a note:
- The omission runs through the whole batch, not just this change. The staged F3 code cites "F3 (Elias's ruling, widening B1)" with no §778, and CheckSuite.cs:221 cites "Elias's ruling F5" with no §779.
- No check resolves § anchors; MetaTextCheck only matches "§" in meta text.
- `git blame` on line 129 will reach the s780 commit, so the gap is cosmetic: traceability is one step weaker, and nothing fails.

**The skeptic's corrected fix.** Do not renumber. §780 is the planned number (F3 is §778 and F5/F6 is §779), so:
1. Keep the commit order and append the §780 record in this change's own commit. If the order ever changes, renumber CLAUDE.md:53, CLAUDE.md:54 and the K-1i row (POLISIM_FEATURE_LIST.md:148) along with it.
2. To match the pin history, start the entry at FormationSweepDiagnostic.cs:129 with "§780: re-pinned - Elias's rulings F1 and F2 ...".
3. Optionally, add "§780" next to "Elias's ruling F1"/"F2" in these places:
   - the record's dating paragraph (coalition_declarations_2023.md:18 and :25);
   - the doc comments in DeclaredRedLines.cs (the PolandSpokenWords summary at :393 and the PolandTimeline summary);
   - the class comment in PolishDeclarationsDiagnostic.
4. Leave the record's Class line (:3) on §776. It names the sourcing pass, as Germany's record keeps §705.

### 35. CLAUDE.md's F2 rule drops 'with §652's condition', and both additions grow bullets already past the 'one to three lines' rule

- **Lens:** hygiene - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/CLAUDE.md:53

**The scenario.** The ruling opens 'Accepted, with §652's condition'. That phrase is what the record's READING of 'always wins' rests on (:22-23, put to Elias at :325). A session applying CLAUDE.md:53 alone would date Konf → TD/NL/MN from the party's own page (2023-08-02, 'always wins'), not from RMF FM's 2023-06-20 as built. Both rules are appended inside existing bullets: the WORLD CLOCK bullet goes from 2,579 to 2,823 characters and the K-1f bullet from 1,140 to 1,289, against the file's 'one to three lines' rule.

**The fix proposed.** Add 'with §652's condition' (or the record's reading) to the F2 clause. Consider giving the declarations rules a bullet of their own.

**The skeptic's evidence.** CONFIRMED (first half); second half real but pre-existing.

1) The F2 clause in CLAUDE.md:53 (working tree) reads: "**and (F2, 2026-10-05, §780) a leader's spoken words count, quoted verbatim on the broadcaster's or news agency's own page and dated by that page - a journalist's paraphrase never counts, and a date from the party's own record always wins.**". The ruling's opening "Accepted, with §652's condition." is missing, and `grep 652 CLAUDE.md` finds nothing.
- What the condition is: §652 (COMPLETED.md:34592) says "If a saved MP publication is earlier, use that instead." §776 (COMPLETED.md:37097) put the question to Elias as "§652 is its precedent ... with its condition - unless the party's own record carries the same words earlier".
- Other files in this change keep the condition:
  - coalition_declarations_2023.md:19 quotes the ruling with it.
  - :22-23 says: "READING, stated: "always wins" as §652's condition words it - the party's own record dates a declaration where it carries the same words earlier."
  - :325 leaves that reading to Elias.
  - DeclaredRedLines.cs:393-395 (the PolandSpokenWords doc comment) says: "a date from the party's own record always wins (§652's condition, accepted with it)".
  - Only CLAUDE.md:53 and the K-1i row (POLISIM_FEATURE_LIST.md:148, answer (3)) drop it.
- The failing path:
  - DeclaredRedLines.cs:421-426 dates Konf->TD/NL/MN as D(2023, 6, 20), Open: RMF FM 2023-06-20 [KONF-I4], "restated on the party's own page, 2023-08-02 [KONF-P1]".
  - Read literally, "a date from the party's own record always wins" puts the party's later 08-02 date ahead of the broadcaster's earlier one.
  - The record itself (:330-333) says that if the June lines go, "Konfederacja's lines start on its own record (2023-07-06, 2023-08-02)".
  - So the head's literal wording disagrees with the built dates, and the words that permit the narrow reading are not in the file every session reads in full. COMPLETED.md is never read whole, and §780 is not yet written: the last section is §777.
- Why not a defect: the record says "no polling-day line changes" and Poland opens no mid-term round, so no game result moves. The harm is to the rules document and to future dating of other countries' declarations.

2) Bullet length. The figures are correct in bytes: HEAD 2,579 / 1,140 -> working tree 2,822 / 1,288.
- At HEAD both bullets were already the longest and third-longest of 58 standing-rule bullets. Only 4 bullets exceed 600 characters.
- §579 set "every rule one to three lines" when the head was 18.1 KB.
- No check in Assets/Editor or Tools measures CLAUDE.md's bullet length or size: DocumentClaimCheck and ResidueCheck only list it as a historical file.
- This change makes an existing breach worse; it does not create one. Note-level.

**The skeptic's corrected fix.** In CLAUDE.md:53, add the ruling's condition to the F2 clause, e.g. "**and (F2, 2026-10-05, §780, accepted with §652's condition) a leader's spoken words count, quoted verbatim on the broadcaster's or news agency's own page and dated by that page - a journalist's paraphrase never counts, and a date from the party's own record always wins**". Do not present the narrow reading as ruled; at most add "(read, pending Elias, as §652's condition words it: where the party's own record carries the same words earlier)". Make the same addition to the K-1i row's answer (3) in POLISIM_FEATURE_LIST.md:148, so the head, the row, the record (:19-23) and DeclaredRedLines.cs:393-395 agree. The length half needs nothing in this change. Optionally move the "Declarations by date" sub-rule (§621 + F2) out of the WORLD CLOCK bullet, into the K-1f declarations bullet or a bullet of its own, keeping every word (§579: every moved line keeps its words).

### 36. §12 carries an unverifiable count and two citations with no tag or file

- **Lens:** hygiene - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:328

**The scenario.** 'four of the verifiers' five' counts a verification whose findings live in the session scratchpad, not the repo, so no reader can check it (claim convention: GENERATED, REFERENCED or DELETED). The same bullet's 'RMF's own post' (out of tree: Konf/x_rozmowa-rmf_1671214148456251392_syndication.json, created_at 2023-06-20T17:51:17Z) and §4's Biedroń TVN24 page (:159-161, :324; out of tree: NL/tvn24_st7322187_konwencja_lodz.html) are cited with no tag and no path. §5 marks its untagged page 'out of tree' and §11 gives a file path.

**The fix proposed.** Drop the count, and give both pages their out-of-tree paths.

**The skeptic's evidence.** I could not refute it. Every part checks out, and all of the flagged text is new in this diff (the "+" lines at §4 :159-161, §5 :178-180 and §12 :322-330; none of it is in HEAD's version).

1. **The count can't be checked from the repo.** Line 328-329 says "Read as verbatim here (§776's reading, and four of the verifiers' five)".
   - The count is correct, but the only place it lives is the Temp scratchpad, `f12/verify_Konf-10a..e.txt`. Verifiers a, c, d and e have `verbatimCorrect: true`. Verifier b has `false` ("the words are not shown to be verbatim … a question for the owner").
   - COMPLETED.md §776 has no such count (its only verification note is "A second agent verified each finding").
   - COMPLETED.md has no §778-§780 in the working tree, and no draft in the scratchpad mentions the count.
   - So Elias is given a basis for a ruling he has to make, and neither the basis nor the dissent can be traced.

2. **Three pages are cited with no tag, no path and no URL.** All three exist only out of tree, under `PoliSim-captures/sources/poland_declarations_2023/`, with their digests in their folders' checksum files:
   - RMF's post (:327) is `Konf/x_rozmowa-rmf_1671214148456251392_syndication.json`. It has created_at 2023-06-20T17:51:17Z, the text "nie zamierzam robić koalicji z PiS; w przyszłej kadencji nie będę w koalicji z nikim", and is line 27 of Konf/SHA256SUMS.txt.
   - Biedroń's page (:159-161, :324) is `NL/tvn24_st7322187_konwencja_lodz.html`. It has datePublished 2023-09-02T11:18:55Z, the words "My tę czarno-brunatną koalicję zatrzymamy", and is line 43 of NL/SHA256SUMS.
   - Budka's page (§5 :179, §12 :323) is `KO/tvn24_2023-08-09_budka_inauguracja_kampanii.html`, which contains "pogonimy Kaczyńskiego". The finding left this one out. §12 cites it with neither a tag nor an "out of tree" mark.
   - I scanned every in-tree .html and .json, decoding entities, and found 0 hits for the RMF words or the Biedroń words. The in-tree [KONF-I4] page embeds the post only as an X iframe placeholder with its id, not its text.

3. **The new text contradicts the record's own header.**
   - Lines 4-5 say: "Every source this record's register lists - the sources the facts and §§1–11 cite by its tags, and the pages §12's doubts lean on - is stored byte for byte under `raw/declarations_2023/<declarer>/`."
   - §12's "Left to Elias" items lean on all three pages above, and none of them is registered or held in tree.
   - PolishDeclarationsDiagnostic can't catch this. Check (b) reads only the tags in the facts' Basis, and check (c) reads only register rows (PolishDeclarationsDiagnostic.cs:75-111).

4. **This exact class was already reviewed and graded minor.**
   - `Reviews/2026-10-04_s776_pl_declarations.md` item 3 ("Doubts quote pages that are not registered or held in tree, contrary to the header") was graded minor by both reviewer and skeptic. The reason given: "a false provenance claim in a SOURCED record, plus quotes put to Elias for a ruling that cannot be traced from the repo".
   - Item 19 (a cited file not held in tree) was graded minor by the skeptic.
   - The same review (:221, :228) named the RMF post's path and recommended bringing it into tree.
   - §776 fixed item 3 by registering every page §12 then leaned on ([TD-I13], [TD-I14], [NL-P30], [KONF-P3] and [TD-P11] carry "(doubt N)" in the register). It then wrote the header as quoted above. This change brings the gap back.

5. **The finding's comparison is accurate but partial.**
   - §5 marks Budka's page "out of tree" but gives no path.
   - §11 gives a path only for the PCh24 page (:267). Its other context pages carry none, which line 398 ("context the record does not cite by tag … has its digest in its folder's checksum file") allows.
   - That allowance covers §4's passing mention of Biedroń. It does not cover §12, which the header holds to the register.

**Severity.** I'm raising it from note to minor, following item 3. No formation, fact or check result depends on these pages (§12 says no formation turns on any of them). But the header's provenance promise is false for three pages, and the inputs to a pending ruling can't be traced from the repo.

**Something nearby that isn't part of this finding.** The register still marks [TD-I13] "(doubt 2)", [TD-I14] "(doubts 3 and 12)" and [NL-P30] "(doubt 8)". After the rewrite, nothing in the record's body cites any of the three, so those pointers are stale.

**The skeptic's corrected fix.** 1. Drop "and four of the verifiers' five" from §12 (:328-329). Alternatively, record the Konf-10 verification in COMPLETED.md §780, which is exempt from the claim convention: four of five verifiers read it verbatim, and the fifth objected to "Opracowanie: Jan Matoga" and the post's other wording. Then point the record at §780 by section. The scratchpad copy, f12/verify_Konf-10a..e.txt, will not survive.

2. For each page §12 leans on, follow §776's own fix for the s776 review's item 3. Preferably register the page:
   - give it a tag, its URL and a byte copy under raw/declarations_2023/<declarer>/;
   - add a line to SHA256SUMS.txt, a register row and a fetch_log line, so that check (c) holds it and the header stays true.

   The pages are:
   - RMF's post, `Konf/x_rozmowa-rmf_1671214148456251392_syndication.json`, with the X syndication URL for id 1671214148456251392, publisher @Rozmowa_RMF, created_at 2023-06-20T17:51:17Z;
   - Biedroń's page, `NL/tvn24_st7322187_konwencja_lodz.html`, at https://tvn24.pl/wybory-parlamentarne-2023/wybory-parlamentarne-2023-konwencja-nowej-lewicy-w-lodzi-st7322187, datePublished 2023-09-02T11:18:55Z;
   - Budka's page, `KO/tvn24_2023-08-09_budka_inauguracja_kampanii.html`, which the finding missed (§12 :323 and §5 :179).

   Then cite each by its tag in §4, §5 and §12.

   The minimum alternative: in §4, §5 and §12, mark each page "out of tree: PoliSim-captures/sources/poland_declarations_2023/<path>", and narrow header lines 4-5 to say that §12's remaining choices also lean on the sweep's captures.

3. While editing the register, remove the stale "(doubt 2)", "(doubts 3 and 12)" and "(doubt 8)" notes on [TD-I13], [TD-I14] and [NL-P30], or re-point them.

### 37. 'Left to Elias' omits two parallel candidates found on NL's own pages

- **Lens:** hygiene - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:321

**The scenario.** Both pages are out of tree. (1) NL → PiS: Czarzasty on NL's own page, 2022-01-31: 'Podstawowa sprawa to pokonanie PiS' (NL/lewica_org_20220131_czarzasty_musimy_wspolrzadzic.html). This is earlier than the 2022-09-20 start and the same class as KO → PiS's 'pogonić to zło', which §12 does list. (2) NL → Konf: Razem co-chair Biejat at NL's Częstochowa rally, NL's own page, 2023-08-26: 'stoczymy decydująca bitwę z Konfederacją ... Pokonamy ich! Zatrzymamy tę brunatną falę' (NL/lewica_org_20230826_wiec_czestochowa.html). This targets Konfederacja itself, on an own page that needs no F2. That cuts against §4's framing that the support half 'has no page F2 counts' (:142) and against §12's single NL → Konf candidate (Biedroń, read as aimed at the coalition). No formation turns on either.

**The fix proposed.** List both candidates for Elias, or say why they are not candidates.

**The skeptic's evidence.** I could not refute the finding. The gap is also wider than claimed, because two pages the record already cites carry the same kind of words.

Both quotes are in the saved bytes. The pages show them with &nbsp; entities; the text below is decoded. Each page's digest matches the captures folder's NL/SHA256SUMS.
- NL/lewica_org_20230826_wiec_czestochowa.html (9f100db0…, article:published_time 2023-08-26T18:22:49Z, article:modified_time 2026-01-29T17:26:47Z). Biejat, "współprzewodnicząca partii Razem", on NL's own page: "stoczymy decydująca bitwę z Konfederacją – i gwarantuje wam, że ją wygramy! Lewica to zrobi! Pokonamy ich! Zatrzymamy tę brunatną falę". The speech is framed by "15 października zdecydujemy czy przyszły rząd tworzyć będzie Lewica czy Konfederacja". Zandberg on the same page: "Trzeba zatrzymać brunatną falę skrajnej prawicy … I zatrzymamy to!"
- NL/lewica_org_20220131_czarzasty_musimy_wspolrzadzic.html (2aa2ff6b…, published 2022-01-31T08:35:20Z). Czarzasty: "Podstawowa sprawa to pokonanie PiS."

Two pages already in tree and cited carry the same verbs:
- [NL-P3], digest 8c90f556…, matches. This is the page fact 12 cites as "A cabinet only" (DeclaredRedLines.cs:435-436). Its next sentence reads: "Z PiS-em nigdy w życiu nie wejdę w żadną koalicję. PiS trzeba pokonać, opozycja musi to zrobić". That is dated 2021-05-06, earlier than the finding's 2022-01-31 candidate.
- [NL-P25], digest ebf4de42…, matches. Dated 2023-09-11: "Jedna partia na opozycji nie pokona brunatnej siły i PiS", where the same page says "Konfederacja to brunatna siła".

The record never addresses these words. Grepping 'pokon' in coalition_declarations_2023.md finds only [NL-P25]'s URL. What the record does say:
- :142 says NL → Konf's "support half has no page F2 counts".
- Doubt 8 (:312) says that half was "decided by F2".
- :159-161 and :324 list Biedroń's "zatrzymamy" as the only NL → Konf candidate. The stated reason it is not a line is that it targets the coalition, "not toward Konfederacja itself". Biejat's "Zatrzymamy tę brunatną falę" does target Konfederacja itself, on NL's own page, which needs no F2.
- Doubt 7 (:311) names only "odsunąć PiS od władzy" from 2022-09-20. Meanwhile :323 lists KO's less-plain "pogonić" as a candidate for an earlier start, but nothing parallel is listed for NL → PiS.
- So the record treats "pokonać" (defeat) as not a pledge under F1 without ever saying so.

No formation turns on any of this:
- ForDateSourced (DeclaredRedLines.cs:478-494) adds declared lines on top of the lines DerivedRedLines.From draws. §10 records the derived NL–Konf line as support-blocking both ways.
- NL → PiS's start would only move within 2021–2022. Polling day is 2023-10-15, and in the 2019 chamber NL has 0 seats (PartySystem.cs:764).

**The skeptic's corrected fix.** In §12's "Left to Elias", add these items, or state the reading that rules them out:

(a) NL → PiS's start. [NL-P3] itself says "PiS trzeba pokonać, opozycja musi to zrobić" (2021-05-06, in tree). Read as a pledge under F1, that makes the line support-blocking from its first day, and fact 12's cabinet-only interval disappears. Name the 2022-01-31 "Podstawowa sprawa to pokonanie PiS" as a restatement.

(b) NL → Konf's support half, from NL's own pages:
- Biejat's "Pokonamy ich! Zatrzymamy tę brunatną falę" and Zandberg's "…I zatrzymamy to!" (Częstochowa, 2023-08-26). Note that the page's own modified_time is 2026-01-29.
- [NL-P25]'s "nie pokona brunatnej siły i PiS" (2023-09-11).

If the record keeps these out by a reading (for example, that "pokonać"/"zatrzymać" without words about power is not a pledge under F1), state it as a READING, the way the header states "always wins". Then say why Biedroń's "zatrzymamy" and KO's "pogonić" are still listed. Also reword :142 and doubt 8 so that NL → Konf's cabinet-only shape rests on that reading, not on F2 alone.

Any out-of-tree page §12 then leans on should come into tree with its digest, as the header requires. Biedroń's and Budka's TVN24 pages are already out of tree today.

### 38. Stale or unhedged sentences left by the rewrite

- **Lens:** hygiene - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:29

**The scenario.** (1) §1's heading 'no joint government with Konfederacja' omits the new PiS → KO line; the §4 and §5 headings were updated. (2) §3 :135-136, 'The line changes no formation: Konfederacja's "z nikim" already refuses the pair's cabinet', justifies only the cabinet half of a line that is now support-blocking. (3) :67 asserts '(F2: ... the words as said)', while §12 :326-330 leaves the verbatim question to Elias. (4) DeclaredRedLines.cs:428, 'Restated by Bosak on TVN24, 2023-07-13 [KONF-I14] ..., 2023-07-16 [KONF-I15] ..., 2023-08-02 [KONF-P1], 2023-10-10 [KONF-I23] ... and 2023-10-11 [KONF-I25]', attributes to Bosak on TVN24 a party page (Wipler's words) and Mentzen's Polsat and TVN24 interviews.

**The fix proposed.** Update the heading, give §3 a reason that covers the support half, hedge :67 with '(read so, §12)', and attribute per source at :428.

**The skeptic's evidence.** All four parts hold when checked against the working tree, HEAD (cba1bd04) and the saved bytes.

(1) The §1 heading was left behind. coalition_declarations_2023.md:29 still reads "## 1. Prawo i Sprawiedliwość — no joint government with Konfederacja", the same as HEAD:26. But §1 now carries a second line at :43, "**PiS → KO** — ONE-WAY, support-blocking, from **2023-09-08** (F1)". In HEAD that place said "PiS toward KO, TD and NL: no line declared". The same change did update the §4 and §5 headings: "never with PiS (2021)" became "never with PiS, and PiS out of power", and "no line declared" became "PiS kept from power".

(2) The §3 reason covers only half of the new line. :125 now makes TD → Konf "ONE-WAY, support-blocking, from 2023-08-10 (F1)", and :130 says TD "neither joins nor supports a cabinet that includes Konfederacja". Yet :135-136, "The line changes no formation: Konfederacja's "z nikim" already refuses the pair's cabinet.", is carried word for word from HEAD:118-119, where the line was cabinet-only from 2023-10-10. The model now uses the support half: CoalitionFormation.cs:64 `RefusesSupport(p,q) => BlocksSupport && (OneWay ? A == p && B == q : Covers(p,q))` stops TD supporting any cabinet that contains Konf. Nothing else blocks that. "Z nikim" is a cabinet line (§8 rows 5–7), and the only derived lines are PiS–NL and NL–Konf (:252). R3 matches R1 on both counts (:290-295), so no measured formation moves. That is not the reason the sentence gives, though.

(3) :67 states something §12 leaves open. :67 says "**(F2: the station's own record of its debate, the words as said)**". §12, under "Left to Elias" (:321, :326-330), asks "Whether RMF24's write-up of its own debate quotes Mentzen verbatim ... Read as verbatim here ... if not, the June lines go". Elsewhere the record points open readings to §12, for example "not read as the start (§12)" at :50 and :180. :67 has no such pointer.

(4) DeclaredRedLines.cs:428 gets the speakers wrong. It reads: "Restated by Bosak on TVN24, 2023-07-13 [KONF-I14] (…), 2023-07-16 [KONF-I15] (…), 2023-08-02 [KONF-P1], 2023-10-10 [KONF-I23] (…) and 2023-10-11 [KONF-I25]." Read naturally, "by Bosak on TVN24" covers every item in the list. I checked each page's decoded bytes:
- I14: "zapewnia Krzysztof Bosak" on TVN24. Correct.
- I15: "My chcemy PiS odsunąć od władzy - deklaruje Krzysztof Bosak" on TVN24. Correct.
- P1: konfederacja.pl, "Przemysław Wipler w Onet Rano został zapytany…". Wrong speaker and wrong outlet.
- I23: polsatnews.pl, "Mentzen: Chcemy zakończyć rządy PiS-u … mówił Sławomir Mentzen". Wrong speaker and wrong outlet.
- I25: TVN24, "Mentzen: nie zamierzam współtworzyć rządu ani z KO, ani z PiS-em". Wrong speaker.

The same file's own Konf → TD fact credits [KONF-P1] to "Przemysław Wipler's words on Onet Rano". The Basis text becomes RedLine.Basis (DeclaredRedLines.cs:491) and reaches FormationProposal.cs:171 ("refuses: a red line falls inside this cabinet - " + Basis), which is player-facing once a Polish formation sheet opens. Today it is not shown: no Polish round opens, and the Desk shows only the source keys (GameController.CampaignDeclared.cs:153). No check would catch this. PolishDeclarationsDiagnostic's F2 check reads only the first tag ([KONF-P2]), and FormationSweepDiagnostic's digest does not read Basis.

The md has the same problem at :90: "he named PiS alone" follows Bosak's name, but the words are Mentzen's.

Re-graded from note to minor because of (4): it is a factual misattribution in the code's citation text, not a matter of style. (1)–(3) are notes. None of the four changes behaviour.

**The skeptic's corrected fix.** Text-only edits. None of them moves a pin.

(1) md:29: change the heading to "## 1. Prawo i Sprawiedliwość — no joint government with Konfederacja; KO and Tusk kept from power", matching the F1 wording of §4 and §5.

(2) md:135-136: replace the sentence with: "Its cabinet half changes no formation: Konfederacja's "z nikim" already refuses the pair's cabinet; its support half reaches only a cabinet of Konfederacja alone (its own lines refuse every partner, §2) - R3 (§12) reads it cabinet-only." This points to the measurement instead of claiming as a general fact that the support half never matters.

(3) md:67: change to "**(F2: the station's own record of its debate, read as the words as said - §12)**".

(4) DeclaredRedLines.cs:428: give each source its own speaker and outlet: "Restated by Bosak on TVN24, 2023-07-13 [KONF-I14] (…) and 2023-07-16 [KONF-I15] (…); on the party's own page, 2023-08-02 [KONF-P1] (Wipler's words on Onet Rano); by Mentzen on Polsat News, 2023-10-10 [KONF-I23] (…), and on TVN24, 2023-10-11 [KONF-I25]." Keep [KONF-P2] as the first tag so the F2 check still passes.

Also fix md:90: change "he named PiS alone" to "Mentzen named PiS alone".

### 39. Provenance notes: one date and one verb are off

- **Lens:** hygiene - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:332

**The scenario.** The register heading says [NL-P31] and [NL-P32] were saved 2026-10-05. NL's urls.tsv (accessed 2026-10-04), the file times and fetch_log.md:3-4 ('as it was served on 2026-10-04') show they were saved on 2026-10-04 and brought into the tree on 2026-10-05, as the fetch log's own note says. Separately, fetch_log.md:31-32 says the pages were 'brought in for the line they date', but [NL-P32] restates the line; only [NL-P31] dates it.

**The fix proposed.** Write 'saved 2026-10-04, brought into the tree 2026-10-05', and 'the line [NL-P31] dates and [NL-P32] restates'.

**The skeptic's evidence.** The date half holds and is clear-cut. The verb half holds too, but only as a wording nit.

1. The date. The heading at coalition_declarations_2023.md:332 now reads "## Source register (saved 2026-10-04, [NL-P31] and [NL-P32] 2026-10-05 for F1; ...)". The bytes show both pages were saved on 2026-10-04:
- PoliSim-captures/sources/poland_declarations_2023/NL/urls.tsv rows 15-16 give "lewica_org_20220920_czarzasty_deklaracja_koalicji.html ... 2026-10-04" and "lewica_org_20230123_apelujemy_o_wspolprace.html ... 2026-10-04" (column "fetched").
- The sweep copies were created and last modified on 2026-10-04 at 20:23:18 and 20:23:15.
- The sweep's SHA256SUMS (written 2026-10-04 20:36:57) already lists cc4f6da5... and 27fe8ae2... (lines 14-15). The in-tree copies hash to the same digests and keep the 2026-10-04 mtimes.
- Only the in-tree copies are dated 2026-10-05: they were created there at 11:05:59.
- fetch_log.md:3-4 says "Each is stored byte for byte as it was served on 2026-10-04". Lines 31-32 say "held out of tree since the sweep of 2026-10-04 and brought in", so the record contradicts its own fetch log.

The heading also breaks its own convention. Five register pages were brought into the tree on 2026-10-05 at 01:54: [KONF-P3] x_bosak, [TD-P11] pl2050_2023-05-15, [TD-I13] krytykapolityczna, [TD-I14] rp_2020-09-14 and [NL-P30] dziemianowicz. The sweep saved all five on 2026-10-04 between 20:21 and 20:35, and the heading still lists them under "saved 2026-10-04", both at HEAD and now. So in this heading "saved" means the sweep's save date, and on that meaning the two NL pages were saved on 2026-10-04 as well.

2. The verb. fetch_log.md:31-32 says the pair was "brought in for the line they date". In the change itself, only [NL-P31] dates the line:
- DeclaredRedLines.cs:438: "Czarzasty on the party's own page, 2022-09-20 [NL-P31] ... Restated by its National Board's resolution of 21 January 2023 [NL-P32]".
- Record §4: "Under F1 from 2022-09-20 ... [NL-P31] ... restated by the National Board's resolution ... [NL-P32]".
- Register row 393 marks [NL-P31] "(F1: NL → PiS from this day)" and row 394 gives [NL-P32] no such mark.
- PolishDeclarationsDiagnostic.cs:84 holds that "the first tag a fact cites is the one that dates it".
The fetch log's own parenthetical does not say [NL-P32] starts the line, so this half is a loose word, not a false claim.

Grade: minor. A misdated save date is a real factual error in a SOURCED provenance record, and the record disagrees with the fetch log beside the bytes. It touches no code, check, digest or formation: PolishDeclarationsDiagnostic holds register files to their digests, not to dates. The verb half alone would be a note.

**The skeptic's corrected fix.** coalition_declarations_2023.md:332 - drop the date claim and keep the event: "## Source register (saved 2026-10-04; [NL-P31] and [NL-P32] brought into the tree 2026-10-05 for F1; each file held under ...)". The simpler alternative is to delete the inserted clause, because "saved 2026-10-04" is already true of every row (the five pages copied in at 01:54 on 10-05 carry no such note either). fetch_log.md:31-32 - "brought in for the line [NL-P31] dates and [NL-P32] restates".

### 40. 'under four readings' transcribes the size of the readings list

- **Lens:** hygiene - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PolishDeclarationsDiagnostic.cs:32

**The scenario.** Both the diagnostic's summary (:32) and the record (:284) state the count of the `readings` list. It was 'six' before this edit, the same pattern. The readings are listed in full right after, so the number adds nothing and goes stale when the next reading is added.

**The fix proposed.** Drop the number.

**The skeptic's evidence.** The finding is right about the source comment and wrong about the record.

THE SOURCE COMMENT IS A REAL VIOLATION (PolishDeclarationsDiagnostic.cs:32)
- Line 32 is in the present-tense class summary: "...the game's compatibility and groups, under four readings: the derived lines alone; ...; and every support half read cabinet-only." At HEAD it said "under six readings", so the diff rewrote this line and carried the count forward.
- The count is true today. The list at :149-155 has four entries:
  - R0: `Derived()`
  - R1: `WithDeclarations()`
  - R2: `WithDeclarations(withoutPisKo: true)`
  - R3: `WithDeclarations(cabinetOnly: true)`
- Nothing holds the comment to the code:
  - No check asserts `readings.Count`.
  - The run prints each reading by name and government through `foreach ((string name, List<RedLine> lines) in readings)` at :179-190. That printout is the generated form of the same fact.
  - CommentClaimCheck.cs:43 matches only a backticked `Type.Member` (regex ``"`([A-Z][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)`"``), so it cannot see a count written as a word.
- How it fails: add an R4 to :149-155. Line 32 still says "four" while the run measures five, and the bar stays green. The convention's test is "no document becomes wrong - only incomplete". After that edit the list would only be incomplete, but the number would be wrong.
- The rules it breaks:
  - The head of CLAUDE.md treats "a count ... a 'there are N of X'" as DERIVED. It allows only GENERATED, REFERENCED or DELETED ("most counts in prose are" decoration), and says "Nobody transcribes, anywhere, in any file".
  - COMPLETED.md:17909 (§190 rule 2) forbids "a count of code things (... 'twenty-one checks')".
  - CheckSuite.cs:149-150: "a count is referenced or generated, never transcribed - in a comment exactly as in a document."
- Precedent: item 14 of the s773 review (Reviews/2026-10-04_s773_e2_statute_acts.md:548-582) is the same case: a count in a comment that is accurate today, confirmed and graded note.

THE RECORD'S INSTANCE IS NOT A VIOLATION (coalition_declarations_2023.md:283-284)
- The words sit inside a dated measurement: "**Measured** (2026-10-05, `PolishDeclarationsDiagnostic`, the run `nf1a`; ...). Two chambers, each formed by its own investiture rule (positive) with the game's compatibility, under four readings:".
- Run nf1a did measure four readings, so the sentence stays true when the code gains a reading. The convention gives exactly this reason for exempting COMPLETED.md: "a record of what was measured on a day is not a claim about today".
- The s776 review's skeptic already decided this block's predecessor the same way (Reviews/2026-10-04_s776_pl_declarations.md:765 and :777: "Leave the record's §12 as it is: it is a dated record of the 2026-10-04 measurement").
- Reading it the reviewer's way would also condemn the table that follows, which is transcribed from nf1a's log in the same dated block.

SEVERITY: note. The line has no runtime effect, it is accurate today, and the pattern was inherited from HEAD.

**The skeptic's corrected fix.** Fix only PolishDeclarationsDiagnostic.cs:32. Drop the number, and let the run's printed lines carry the count. For example: "...and the game's compatibility and groups, under each of its readings (each printed by name with the government it forms): the derived lines alone; the declarations of polling day as recorded (what the game reads); those without the PiS-KO pair (doubt 1 read as aims, as before F1); and every support half read cabinet-only."

Leave the record's §12 (coalition_declarations_2023.md:284) as it is. It is a dated record of run nf1a, and its "four readings" cannot go stale.

Optional, for consistency: "on two chambers" in the same summary sentence (:30) follows the same pattern. That line is unchanged context outside this diff, so dropping its "two" is not owed by this change.

## The first pass - refuted by the skeptics

- [ruling] The re-pin comment's evidence pointer names two identical files - *The facts on disk are as the reviewer says, but they describe the moment before the bar runs, not the change that will be committed.

Confirmed on disk (PoliSim-captures/logs):
- formation_sweep.txt and formation_sweep_before_f1.txt both hash 6c0cd88d, and cmp finds them identical. Both carry the same mtime, 02:59:54.556221200. That means before_f1 is a timestamp-preserving copy, made as line 115 tells you to: "keep the old text beside the new before re-running".
- formation_sweep_mismatch.txt hashes 49a05aa3. It was written by nf1a: nf1a.log:683 says "written to ..\formation_sweep_mismatch.txt" and :699 says "digest 49a05aa3... is not the pinned 6c0cd88d...". nf1a was a named SUBSET run, not a bar (nf1a.log:510 "a SUBSET, never a tier's bar"). It ran before the pin was edited: FormationSweepDiagnostic.cs mtime is 11:09:01, the mismatch file 11:07:58.
- No bar has run since. The last row in Logs/bar_timing.tsv (09:24:33Z) is a named F3 subset: PresidentialVeto plus ElectionLive.

Why this cannot reach a commit:
- FormationSweepDiagnostic.cs:112 is `string path = Path.Combine(dir, digest == PinnedDigest ? "formation_sweep.txt" : "formation_sweep_mismatch.txt");`.
- Lines 117-121 fail the check whenever digest != PinnedDigest.
- CheckSuite.cs:207 puts FormationSweepDiagnostic in the cheap `Suite` (starts at :171).
- Tools/bar_tier.ps1:56 classes Assets/Scripts/Elections as SIMULATION, and :91 makes every such commit owe CheckSuite.RunAllBatch (the cheap bar).
- CLAUDE.md:8 says "Before every commit, ... bar_tier.ps1 -Staged" and CLAUDE.md:65 says "ONE GREEN BAR PER COMMIT, NO EXCEPTIONS".
- So a green bar on this tree needs the sweep's digest to equal 49a05aa3. That same run writes formation_sweep.txt with that text, and the pointer "(formation_sweep_before_f1.txt against formation_sweep.txt)" becomes exactly the diff it describes.

Precedent: §776 used the same pointer form. Its formation_sweep.txt was rewritten under the new pin at 02:59:54, before commit 22796935 at 03:19:09. The §776 review (Reviews/2026-10-04_s776_pl_declarations.md:790) found formation_sweep.txt equal to the new pin.

Substance checked: diffing before_f1 against the new text, only lines 94031-94071 move, which are the two "Poland | seated" blocks.
- Negative rule: before, cab 1 (PiS alone, 194); after, cab 6 sup 8.
- Positive rule: cab 6 sup 8 as before; cab 3 is gone from viable, now "blocked 3 by 0-1".
This matches the comment.

Side note, not this finding and older than this change: once this change's bar runs, the older §776 entry's "(formation_sweep_before776.txt against formation_sweep.txt)" will point at F1's text. §776's after-text will then be only in formation_sweep_before_f1.txt. The chain convention works this way by design.*
- [tests] The sweep's re-pin comment points at formation_sweep.txt, which still holds the pre-F1 text, and no run has passed under the new pin yet - *The facts in the finding are accurate. The defect is not: this is a step of the re-pin workflow that has not run yet, and the code and the repo's pre-commit rule both cover it.

FACTS CONFIRMED
- sha256: formation_sweep.txt and formation_sweep_before_f1.txt are both 6c0cd88d (cmp says IDENTICAL). formation_sweep_mismatch.txt is 49a05aa3, the new pin.
- nf1a.log:683 says "digest 49a05aa3… written to ..\formation_sweep_mismatch.txt". Line 699 says "is not the pinned 6c0cd88d" and line 884 says "CHECKS: 1 of 5 FAILED — FormationSweepDiagnostic".
- unity_launched.tsv shows one launch after nf1a (11:06:33): nf3b at 11:23:42, with -checks=PresidentialVetoDiagnostic,PresidentialElectionLiveDiagnostic. The last FormationSweepDiagnostic row in Logs/bar_timing.tsv is 09:07:58Z with exit 1. So no run has passed under the new pin.
- Of the 250 blocks, only "== Poland | seated | negative True/False" differ (lines 94031-94071).

WHY IT IS NOT A DEFECT
1. The convention writes the file pair this way on purpose. FormationSweepDiagnostic.cs:111-113 has the comment "The text is written where it matches the pin; a mismatch is written beside it…" and the code `Path.Combine(dir, digest == PinnedDigest ? "formation_sweep.txt" : "formation_sweep_mismatch.txt"); File.WriteAllText(path, body);`. Lines 117-118 say "sets the pin in the same commit with the diff of the text (keep the old text beside the new before re-running)". The re-run is the intended next step. The §776 comment uses the same pair, "(formation_sweep_before776.txt against formation_sweep.txt)". That pair became true when cheap776c ran (log line 8462: digest 6c0cd88d written to formation_sweep.txt; 96 of 96 clean) before commit 22796935 at 03:19:09.
2. The repo requires that run before any commit. CLAUDE.md:8 says "Before every commit, Tools/bar_tier.ps1 -Staged, and run what it says the commit owes." bar_tier.ps1:56 puts Assets/Scripts/Elections/ and ElectionsData/ in the SIMULATION tier. That tier owes CheckSuite.RunAllBatch, the cheap bar. CheckSuite.cs:198/207/214 put DeclarationDatesDiagnostic, FormationSweepDiagnostic and PolishDeclarationsDiagnostic in that bar. The owed bar either writes formation_sweep.txt at 49a05aa3, which makes the comment's pair correct, or it fails and blocks the commit. Either way the wrong state cannot reach the committed tree.
3. The next run will reproduce the pin. Nothing that feeds the hashed body changed after nf1a. DeclaredRedLines.cs was last modified at 11:01:52, before nf1a started at 11:06:33. The PinnedDigest constant is not part of `body`. The record, CLAUDE.md and the feature list are not read by the sweep. F3's staged SimulationManager diff has no epoch, formation, coalition or red-line lines. The unstaged F5 edits to CabinetSystem, ParliamentSystem and Country (BillLostOn and the pressure check) were already in nf1a's tree, and none of them feeds the formation.
4. "As before" is accurate. In both texts the government is "cab 6 sup 8": the same cabinet (KO+TD) and the same support (NL). Only "opposed 18" becomes "opposed 212". The comment does not say the opposed seats are unchanged, and it points the reader to the two texts for figures ("the figures are the two texts'"), which is what the claim convention asks. Leaving PiS's opposition out of the summary is an omission, not a misstatement. The re-attributed blocked lines are also unmentioned: "blocked 7 by 2-0" becomes "0-1", and "blocked 22 by 4-1" becomes "4-2".*

## What the author did about the first pass

- **11** (KO's own record was never searched, so F2's "always wins" was not applied to KO → PiS) - swept: platforma.org, koalicjaobywatelska.pl, and the X posts of Tusk, KO's own account (@Obywatelska_KO) and Grabiec, from January 2022 to polling day (out of tree, `KO/own/`, with its `SOURCES.tsv`). The earliest pledge found is Tusk's own post of 2022-02-09, "odsunięcie PiS od władzy to nasze wspólne być albo nie być" [KO-P1], restated on KO's own account [KO-P2] to [KO-P4]: KO → PiS is dated by KO's own record from that day, F2 no longer marks it, and 2021 is stated as not swept. Its four posts are in the tree with their digests. No polling-day line moves.
- **8, 10, 20, 33** (the F2 check read only the tag's letter) - the register marks the pages F2 counts **(F2)**; the check holds every fact carrying F2's mark to one of them, every other fact to its declarer's own [X-Pn] page, and each fact's from to its first tag's own date. It can fail: a relay, a portal or a mis-dated fact each break it.
- **23** (F1's support halves held only by §8) - a fact is one-way and support-blocking exactly when its basis carries F1's mark; (a) also holds each row's shape words and its F2 mark.
- **22** (no replacement chain held) - `DeclarationDatesDiagnostic`: Poland lifts nothing between the 2019 and 2023 elections.
- **6, 17, 30** (F2's unstated readings) - the header states the second READING: the broadcaster's or agency's own page is the outlet that took the words down - a relay dates nothing - and a newspaper is not a broadcaster on its own video programme (Super Express's "Wieczorny Express", named in §2). Doubt 4 is "decided by F2 as read"; §12 lists both readings for Elias with what each would move (the June opening dated 2023-06-26; no polling-day line); the code's basis strings say "as the record reads it".
- **15, 35** (F2 stated without §652's condition) - CLAUDE.md, the K-1i row, the record and `DeclaredRedLines` carry "accepted with §652's condition", and the reading: the party's own record dates a declaration where it carries the same declaration earlier, in whatever words (pending Elias).
- **9** (F7's acceptance rests on one count) - §12 states the READING ("a world that follows history" is a Polish game's own count stepped from the start with no policy), says the acceptance holds there through the vote model's flagged lineage misses and fails on the record's own seats, and puts the choice to Elias; the diagnostic says "READ AS". "Kept as built (F7)" stays: F7 answered that preference by name.
- **1, 13, 37** (NL → Konf's candidates aimed at Konfederacja) - §4 quotes them, own record first: Razem's co-chairs on NL's Częstochowa page [NL-P33] (modified 2026-01-29, said), NL's own page of 2023-09-11 [NL-P25], and Biedroń's "tarczą przeciwko Konfederacji" [NL-I13]; the READING that keeps them out of F1 (F1's forms name power or government) is stated, and §12 puts NL → Konf's support half to Elias. No formation turns on it.
- **3** (NL → PiS's "PiS trzeba pokonać") - §4 lists it and the 2022-01-31 page [NL-P34] as earlier and less plain, under the same READING; §12 lists the start; fact 13's basis notes it.
- **2** (fact 13's gloss) - the opposition's lists, as the page says.
- **4, 12, 27** (Grabiec's sentence cut; Budka's page contradicting itself across sections) - Grabiec's sentence whole, *(decoded)*; Budka's TVN24 page in the tree [KO-I13], cited once with both sentences; both, with Tusk's Polsat words, are now restatements after KO's own start (§11), so the start candidates they were offered as are moot.
- **19** ([PIS-I1] credits PAP) - its register row and §12's PiS → KO item say "Źródło: TVN24, PAP" and put that to Elias with the item.
- **5** (two first-tag pages edited after their dates) - [KONF-I4]'s dateModified is in its row and §12's RMF item (a site restamp); [TD-P11]'s edit of 2023-07-01 is in §3, its row and §12 (if the edit added the pledge, TD → PiS starts on PSL's page of 2023-08-10; no polling-day line).
- **14, 36** (pages §12 leans on not in the tree; an unverifiable count) - [NL-P33], [NL-P34], [NL-I13], [KO-I13] and RMF's own post [KONF-I26] are in the tree with digests and register rows; the verifiers' count is recorded in `COMPLETED.md` §780, not the record.
- **16, 31** (Sweden's KD row still an open extension) - ruled by F2, its own record stated a GAP.
- **26** (orphaned register rows) - [TD-I13] is context in doubt 2, [TD-I14] is in doubt 12 with the 2019 lists' lines, [NL-P30] restates NL → Konf in §4, [KONF-P3] is quoted in §2; the rows point there.
- **18** (R2 overstated) - "what F1's PiS–KO pair changes, F1's other lines standing", in §12 and the diagnostic.
- **28** (missing decoded marks), **29** (doubt 5's pointer), **32** (bare §N in basis strings), **34** (§780 unanchored), **38** (stale sentences), **39** (provenance wording), **40** (a transcribed count) - each fixed as proposed; the sweep's re-pin comment begins "§780".
- **24** (DeclarationDates' message) - "its closed ones", no year.
- **21** (nothing reads SHA256SUMS) - no change, as the skeptic advised: a repo-wide manifest check is its own item.
- **7, 25** (the run-up page's DECLARED slip still says every item is dated by the party's own record) - not in this commit: a player-visible string on Design's board 21c, already untrue for Sweden's MP and KD since §652; put to Design with the wording the skeptic proposed for all three countries.
## The second pass - confirmed (verbatim)

A second workflow (s780-fix-pass) on the fixes made after the first pass - their patch read against the record, the code and the rules - in three lenses (the new and changed citations against the saved bytes, the rulings' reading and the record's consistency, the checks and the hygiene), every finding put to a refute-first skeptic. (Its first launch died on a usage limit before any agent ran; it was re-run whole.)

### 1. KO → PiS: an earlier own-record pledge from 2022-01-29 is in the sweep but the record neither cites nor discusses it

- **Lens:** facts - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:201

**The scenario.** §5 (lines 201-204) and §12 (line 355) call [KO-P1] (2022-02-09) 'the earliest pledge found by a sweep of KO's own pages and posts'. The sweep's own listing (PoliSim-captures/sources/poland_declarations_2023/KO/own/SOURCES.tsv, rows 106-107) holds two earlier posts. Both are on the same account [KO-P2]..[KO-P4] are cited from (user id 53003895), dated 2022-01-29, quoting Tusk at PO's programme congress. (1) x_Platforma_org_2022-01-29_1487416047955324930.json: 'Dla Was wszystkich ... - obalimy te filary złej władzy.' This is a first-person pledge that names power; PiS is not named. (2) x_Platforma_org_2022-01-29_1487416468652367877.json: 'Przyrzekam Wam: w najbliższych miesiącach dogonimy, a w wyborach przegonimy PiS.' This is a sworn promise to defeat PiS. By §4's READING the second is not F1's, but it is still a pledge, so 'earliest pledge found' is not true as written. The first is close to F1's form on the record's own standard: §11 (lines 295-296) lists Budka's 'żeby zatrzymać tę złą władzę' as a restatement of KO → PiS, so the record already reads 'zła władza' as PiS's rule. KO-P1 itself passes on NL-P31's standard: removing PiS from power is stated as the aim, with no first-person 'odsuniemy' as in TD-P11. No game date changes (Poland opens 2023-02-19). But the reason 2022-01-29 is not the start is never stated, whereas §4 names NL's earlier, less plain words ([NL-P3], [NL-P34]) and puts them to Elias.

**The fix proposed.** In §5 and in §12's 'KO → PiS's start' bullet, name both 2022-01-29 posts as 'earlier and less plain, not read as the start' and give the reason: PiS is not named; the target is 'the pillars of' the bad power; 'przegonimy' is defeat. This matches what §4 does for NL. Put 'obalimy te filary złej władzy' to Elias. If it is read as F1's, re-date KO → PiS to 2022-01-29: copy the file into raw/declarations_2023/KO/, register it (sha256 3cf41c010b71819463b261d8fc2405a1e79c57f254c2d0a77b085a518695df34), and move §8 row 15 and PolandTimeline's D(2022, 2, 9).

**The skeptic's evidence.** The bytes match the finding. Both files are in G:/UNITY/Projects/PoliSim-captures/sources/poland_declarations_2023/KO/own/, SOURCES.tsv rows 106-107; the Polish is raw UTF-8, so nothing needed decoding.
- x_Platforma_org_2022-01-29_1487416047955324930.json (sha256 3cf41c01...df34): user id_str 53003895, "Koalicja Obywatelska" / Obywatelska_KO. That is the same account as the in-tree [KO-P2]..[KO-P4], which I checked. created_at 2022-01-29T13:23:15Z, isEdited false. Text: "Przewodniczący @donaldtusk: Dla Was wszystkich i z Wami wszystkimi, którzy macie dość brudu, który zalał Polskę - obalimy te filary złej władzy. #KongresProgramowyPO".
- ..._1487416468652367877.json, 13:24:55Z, same account: "Przyrzekam Wam: w najbliższych miesiącach dogonimy, a w wyborach przegonimy PiS."
- The sweep classes both "less-plain", and [KO-P1] (row 65) "plain". It also classes Wcisło's "Ten rząd obalą kobiety!" (row 73) "plain-ish: topple this government". So the builder did weigh "topple the pillars" against "topple the government", but that judgment exists only in the out-of-tree notes column.
- Row 64, Tusk's own post of 2022-01-17 ("Sześć miesięcy później PiS oddał władzę ... nie ma przypadków"), is also earlier. It is a forecast, not a pledge.

What is refuted: point (2). The record uses "pledge" as F1's term: header line 30 ("A pledge to remove a party from power, end its rule, block its return or not let it govern") and §10 line 279. §4's stated READING (lines 172-173) already excludes "defeat" words, so "przegonimy PiS" does not make "earliest pledge found" false.

What stands: point (1).
- The header's first READING (lines 22-23) says the own record dates a declaration "where it carries the same declaration earlier, in whatever words".
- §4's READING says "F1's forms name power or government ... words to stop, defeat or shield against a party name neither". "obalimy te filary złej władzy" names power ("władza") and uses "obalić" (topple), so that test does not exclude it.
- The only grounds that would exclude it are that PiS is not named and that the target is "the pillars of" the power. Neither is stated anywhere. §11 (lines 295-297) even lists Budka's "żeby zatrzymać tę złą władzę" as a restatement of KO → PiS, so the record reads "zła władza" as PiS's rule.
- Yet §5 (lines 201-203) and §12 (line 355) call [KO-P1] "the earliest pledge the sweep of KO's own record found (from January 2022)". The §12 bullet's "not excluded" caveat covers only unswept 2021, not an item inside the swept window.
- For NL the record does exactly this work: lines 182-183 ("Earlier and less plain, not read as the start ... [NL-P3], [NL-P34]") and lines 361-362 put it to Elias, even though no formation turns on it. For PiS → KO, line 54 and line 354 do the same.
- Nothing else handles it. A grep of the tree for 1487416, filary, przegonimy and less-plain finds nothing. COMPLETED.md has no §780 entry yet (only a forward reference in §778's bars line).

Impact: documentation only. Poland opens 2023-02-19, both dates fall before it and the line is Open, so §8 row 15, PolandTimeline and the six checks are unaffected. It is still an unstated reading in a ruling record, about a declaration's start that the record's own READINGs do not rule out. That is why I grade it minor rather than a defect.

**The skeptic's corrected fix.** This is a change to the record only. Leave the date, §8 and the code alone unless Elias rules otherwise.

1. ElectionsData/poland/coalition_declarations_2023.md §5: after line 203 ("...(`raw/declarations_2023/fetch_log.md`).") add: "**Earlier and less plain, not read as the start** (§4's READING; §12): on the same account, then @Platforma_org, quoting Tusk at PO's programme congress, 2022-01-29, "obalimy te filary złej władzy" [KO-P5]. Power is named, PiS is not, and the pledge is against its pillars, not the power. Beside it, "w wyborach przegonimy PiS" is defeat."

2. §12, the "KO → PiS's start" bullet (lines 355-357): before "No game date turns on it", insert: "read as F1's, the account's "obalimy te filary złej władzy" (2022-01-29 [KO-P5], §5) starts it then;".

3. Register the page. Once §12 leans on it, the header (lines 3-4: "the pages §12's doubts lean on" are held in tree) and the [NL-P34] precedent both require it.
   - Copy KO/own/x_Platforma_org_2022-01-29_1487416047955324930.json byte for byte to raw/declarations_2023/KO/x_obywatelska_ko_1487416047955324930_syndication.json.
   - Add "3cf41c010b71819463b261d8fc2405a1e79c57f254c2d0a77b085a518695df34 *KO/x_obywatelska_ko_1487416047955324930_syndication.json" to SHA256SUMS.txt.
   - Add a register row: | [KO-P5] | https://cdn.syndication.twimg.com/tweet-result?id=1487416047955324930&token=a | X (@Obywatelska_KO, KO's own account) | created_at 2022-01-29T13:23:15Z, isEdited false (§5, §12) | `KO/x_obywatelska_ko_1487416047955324930_syndication.json` | `3cf41c01...df34` |
   - Name the file in fetch_log.md's KO paragraph.
   - Leave the publisher cell without "(F2)" so the F2 check is untouched.

If Elias rules the words F1's: set PolandTimeline's KO → PiS to D(2022, 1, 29) with [KO-P5] as the Basis's first tag, and set §8 row 15's from to 2022-01-29.

### 2. Sweden KD row: the new 'KD's own record is a GAP - its site was not tried' is contradicted by the same file

- **Lens:** facts - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/sweden/2026/coalition_declarations_2026.md:39

**The scenario.** The F2 text the fix wrote into the KD → S row relies on line 489's 'KD's site was not tried'. Lines 767-769 of the same file call that line outdated: KD's own page [KD-P1] was read (kristdemokraterna.se, 2026-04-17, raw/declarations/kristdemokraterna_20260417_ebbas_tal_kd_dagarna_2026.html; digest e68080af... verified). The manifesto [KD-P2] was fetched and found silent. I read KD-P1: it attacks Andersson but carries no refusal. It says 'Vi har den enkla principen ... att rösta ja till varje regering som vi själva sitter i ... Därför vill vi se fyra år till för vårt samarbete', with no 'nyval' and no 'rösta nej'. So under F2's 'always wins' (§652's condition: a saved publication that is earlier) the 2026-09-02 date stands, but the reason given is false. What remains unsearched is Busch's posts on X (lines 489-490).

**The fix proposed.** Replace 'KD's own record is a GAP - its site was not tried, the GAPs below' with: 'KD's saved publications - its 2026-04-17 page [KD-P1] and its manifesto [KD-P2] - do not carry it; Busch's posts on X were not fetched (a GAP)'.

**The skeptic's evidence.** Line 39 of ElectionsData/sweden/2026/coalition_declarations_2026.md was written by the fix delta (patch line 585). It now says "...a date from the party's own record always wins, and KD's own record is a GAP - its site was not tried, the GAPs below".

The GAPs list it points to (lines 488-490) says "KD's site was not tried (party_leaders_2022.md, line 28, found it JavaScript-rendered)". The same file withdraws that sentence at lines 767-769: "The line *"KD's site was not tried"* in the GAPs above is now outdated: one KD page ([KD-P1]) was served as static HTML and read, and the manifesto PDF [KD-P2] was fetched and found silent." The reason behind the old sentence is also withdrawn elsewhere: party_leaders_2026.md line 122 says "2022's KD gap is closed: kristdemokraterna.se/ebba is now server-rendered".

I checked the bytes:
- raw/declarations/kristdemokraterna_20260417_ebbas_tal_kd_dagarna_2026.html has sha256 e68080af...5dbc, and kristdemokraterna_valmanifest_2026.pdf has 6aafa901...c4630. Both match the register at lines 795-805.
- I decoded the KD-P1 text. Its red-lines passage reads "Vi har den enkla principen att rösta ja till våra förslag. Och att rösta ja till varje regering som vi själva sitter i ... Därför vill vi se fyra år till för vårt samarbete." Andersson is named only in policy attacks. Neither the raw HTML nor the text contains nyval, extraval or "rösta nej".
- In the decoded manifesto KD-P2 (scratchpad kd_manifest_check.txt, 10 pages), there is no Andersson, nyval or extraval. "regering" appears only in the two lines the register names.
- The saved Busch page [PL-KD1] (raw/leaders/kd_kristdemokraterna_ebba.html) is also KD's own page and carries no refusal.

So the reason line 39 gives is false, and it cites a line the same file has withdrawn. The conclusion still holds. KD-P1 (2026-04-17) and KD-P2 (ModDate 2026-08-21) are both earlier than 2026-09-02 and do not carry the refusal, so under F2, read with §652's condition, the 2026-09-02 date stands. The error is in the document only: no code, date or formation outcome moves, so the grade is minor.

One correction to the finding's own fix. The record shows two KD pages read for the K-1f candidacy question, not a sweep of KD's site for the refusal. The finding's text implies that only Busch's X posts remain unsearched, which claims more than the record shows.

**The skeptic's corrected fix.** At line 39, replace "and KD's own record is a GAP - its site was not tried, the GAPs below" with:

"and KD's own record, as far as it was read, does not carry it earlier: its 2026-04-17 speech [KD-P1] and its manifesto [KD-P2] are silent on it (the K-1f section's GAPs withdraw the GAPs list's 'KD's site was not tried'); the site was not swept for the refusal and Busch's posts on X were not fetched (GAPs)"

Nothing else needs to change. Line 489's old sentence is already marked outdated at lines 767-769.

### 3. The (F2) register mark is not 'the pages F2 counts': several TVN24-only pages with leaders' verbatim words are unmarked

- **Lens:** facts - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:26

**The scenario.** The header (lines 26-27) says 'The register marks the pages F2 counts (F2)'. Only KONF-I4, KONF-I14 and KO-I11 carry the mark. Several other pages are broadcasters' own pages carrying a leader's words verbatim, and they are unmarked: KONF-I15 (line 401; 'Źródło: Fakty po Południu TVN24'; Bosak, 'My chcemy PiS odsunąć od władzy'), KONF-I25 (line 404; 'Źródło: TVN24'; Mentzen), NL-I12 ('Źródło: tvn24'; Czarzasty) and the new NL-I13 (line 449; 'Źródło: TVN24'; Biedroń). §12 (lines 359-360) relies on NL-I13 counting. KO-I11 keeps its mark although, after the re-dating, no fact is dated by it. A reader would take an unmarked TVN24 page as one F2 does not count. The diagnostic is unaffected: it only needs the first tags of F2-marked facts to be marked.

**The fix proposed.** Either describe the mark as what it is ('the register marks (F2) the pages a fact is dated by under F2, and KO-I11, which dated KO → PiS before §5's sweep'), or mark every broadcaster's or agency's own page that carries a leader's words verbatim (at least NL-I13, which §12 relies on).

**The skeptic's evidence.** I could not refute it. I read the record, the fix-delta patch and the saved page bytes (decoded).

1. **What the docs say the mark means.** Three places describe it as marking the pages F2 counts:
   - The record, lines 26-27: "The register marks the pages F2 counts **(F2)**".
   - `DeclaredRedLines.cs:396`: "The pages F2 counts are marked (F2) in the record's register."
   - `PolishDeclarationsDiagnostic.cs:30` and `:92`: "the record's reading of 'the broadcaster's or news agency's own page'".
   
   The record uses "F2 counts / does not count" to mean "admits as a dating source" everywhere else (lines 64, 102, 124, 153, 162 and 344).

2. **Which rows carry it.** `grep` finds the mark on exactly three register rows: [KONF-I4] (line 396), [KONF-I14] (line 400) and [KO-I11] (line 438).

3. **Unmarked pages of the same kind.** These pages are TVN24-only and quote leaders verbatim in the dash-quote style, but have no mark:
   - [KONF-I15], line 401: "Źródło: Fakty po Południu TVN24", the same credit as the marked [KONF-I14]. Bosak: "- … My chcemy PiS odsunąć od władzy - deklaruje Krzysztof Bosak".
   - [KONF-I25], line 404: "Źródło: TVN24", the same credit as the marked [KO-I11]. Mentzen: "- Nie zamierzam współtworzyć z rządu ani z Koalicją Obywatelską, ani z PiS-em".
   - [NL-I12], line 428: "Źródło: tvn24.pl". Czarzasty.
   - [NL-I13], line 449: "Źródło: TVN24". Biedroń: "My tę czarno-brunatną koalicję zatrzymamy - zapowiedział … - Po pierwsze, musimy być tarczą przeciwko Konfederacji".
   - [PIS-I4], line 389: "Źródło: TVN24". Kaczyński.

4. **The record treats [NL-I13] as a counted page.** It calls [NL-I13] "TVN24's own page" at line 172 (§4) and line 344 (doubt 8). At lines 359-360 (§12) it says [NL-I13] "would date it from 2023-09-02 only if the own page did not count", which assumes F2 admits it.

5. **[KO-I11] keeps a mark it no longer earns.** KO → PiS is now first-tagged [KO-P1] and is not F2-marked, so no fact is dated by [KO-I11].

So the marked set {KONF-I4, KONF-I14, KO-I11} fits neither reading of the mark. It is not every page F2 admits, and it is not only the pages that date a fact under F2.

**Runtime impact: none.** The diagnostic builds `f2Pages` from the "(F2)" cells and checks three things: the set is non-empty, every member is an -I tag, and each F2 fact's first tag is in the set. No count is pinned, and no code mentions [KO-I11]'s mark. This is a documentation claim error only, so it stays a note.

**The skeptic's corrected fix.** The smallest correct fix is to reword the description, not to mark more rows. Marking every broadcaster page would need a judgement on each page, and the "TVN24, PAP" pages ([PIS-I1], [KO-I3], [KO-I12], [KO-I13]) are Elias's question under §12.

**Record, lines 26-27.** Replace "The register marks the pages F2 counts **(F2)**, and `PolishDeclarationsDiagnostic` holds every fact carrying F2's mark to one of them." with:

"The register marks **(F2)** the pages a fact here is dated by under F2 ([KONF-I4], [KONF-I14]) and [KO-I11], which dated KO → PiS until §5's sweep. The other broadcasters' own pages it lists ([PIS-I4], [KONF-I15], [KONF-I25], [NL-I12], [NL-I13]) count under F2 but date no fact. `PolishDeclarationsDiagnostic` holds every fact carrying F2's mark to a marked page."

**`DeclaredRedLines.cs:396`.** Change "The pages F2 counts are marked (F2) in the record's register." to "The pages a Polish fact is dated by under F2 are marked (F2) in the record's register."

**Simpler alternative.** Drop "(F2)" from [KO-I11]'s publisher cell (line 438), so that the mark means exactly "the pages F2 dates a fact by", and use the same wording in both places. The diagnostic still passes either way: `f2Pages` becomes {KONF-I4, KONF-I14}, which is non-empty and holds only -I tags, and those two tags remain the first tags of all six F2 facts.

### 4. §11 now calls 'pogonimy Kaczyńskiego' and 'żeby pogonić to zło' restatements of KO → PiS, against §4's READING

- **Lens:** facts - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:295

**The scenario.** §4's READING (lines 167-168) says F1's forms name power or government, and that words to stop, defeat or shield against a party name neither. That is why NL's 'Zatrzymamy tę brunatną falę', 'PiS trzeba pokonać' and 'Podstawowa sprawa to pokonanie PiS' are not read as F1's. §11 (lines 295-298) lists two KO phrases under 'Restatements of KO → PiS', and neither names power or government: Budka's 'Października 15. pogonimy Kaczyńskiego' [KO-I13] and Tusk's 'Jestem tutaj po to, żeby pogonić to zło' [KO-I7] (both verified on the saved pages). Before this fix the record put these same words to Elias as 'would start it earlier if read as F1's pledge', i.e. not F1's. The relabelling applies a looser standard to KO's words than to NL's. No date turns on it now.

**The fix proposed.** List these two as 'later words toward PiS, not F1's in terms (§4's READING)'. Keep 'Restatements' for words that name power: 'zatrzymać tę złą władzę', 'odsunąć PiS od tych narzędzi władzy', 'Polska będzie wolna od PiS-u' if so read, and the 2023-06-09 refusal.

**The skeptic's evidence.** I could not refute this finding.

**§11 (lines 295-298)** lists two phrases under "Restatements of KO → PiS, all later than KO's own start (§5)":
- Budka's "Października 15. pogonimy Kaczyńskiego" [KO-I13].
- Tusk's "żeby pogonić to zło" [KO-I7].

**What the saved pages say:**
- KO/tvn24_2023-08-09_budka_inauguracja_kampanii.html: "…że wygramy te wybory. Października 15. pogonimy Kaczyńskiego. Z tym optymistycznym okrzykiem wchodzimy w kampanię wyborczą - mówił." It names a person. It names neither power nor government. The next sentence, "KO wygra z obecną władzą", is the journalist's paraphrase.
- KO/polsatnews_2023-09-19_tusk_gosc_wydarzen.html: "Jestem tutaj po to, żeby pogonić to zło, żeby ludzie mogli znowu oddychać swobodnie, żeby Polska była znowu normalna". It names neither PiS, nor power, nor government.

**The record's own test** is the READING at lines 167-168: "F1's forms name power or government … words to stop, defeat or shield against a party name neither". The record applies it in three places:
- NL → Konf (lines 167-175).
- NL → PiS (lines 182-183): "PiS trzeba pokonać" and "Podstawowa sprawa to pokonanie PiS" are "not read as the start (the READING above)".
- Doubt 8 (line 344).

§1 goes further. Kaczyński's "Ten człowiek nie może rządzić Polską" names governing, but it is aimed at a person, so it is not read as keying KO. Yet §11 counts "pogonimy Kaczyńskiego", which is aimed at PiS's chairman and names no power, as a restatement of KO → PiS. That is a looser standard for KO's words.

**Where the label came from:** the fix itself.
- Before the fix, §5 called these words "Earlier and less plain … not read as the start".
- Before the fix, §12 said they "would start it earlier if read as F1's pledge".
- The author's note rev780_done1.md (items 4, 12, 27) says "both … are now restatements after KO's own start". That treats words that no longer matter for the start date as if they were restatements.

**Why it is only a note:**
- KO → PiS is dated 2022-02-09 by [KO-P1].
- No check or code reads §11's wording. KO-I13 and KO-I7 appear in neither DeclaredRedLines nor either diagnostic.
- No date, fact or formation moves. The problem is consistency inside the record that Elias rules from, next to his open item on "PiS trzeba pokonać" (§12).

**Related stale pointer:** [KO-I13]'s register row (line 450) still says "(§5, §12)", and the register heading (line 380) says it was brought in "for §12". KO-I13 is now cited only in §11.

**The skeptic's corrected fix.** This is a wording change in ElectionsData/poland/coalition_declarations_2023.md §11 (lines 295-298) only. No code or check changes.

1. Keep under "Restatements of KO → PiS, all later than KO's own start (§5)" only the words that name power, plus the refusal:
   - Budka's "żeby zatrzymać tę złą władzę" [KO-I13].
   - Grabiec's "odsunąć PiS od tych narzędzi władzy" [KO-I4].
   - Tusk's own post of 2023-06-09, "a refusal in other words".
2. Give the other two their own clause, for example: "Later words toward PiS, not F1's in terms (§4's READING): Budka's "Października 15. pogonimy Kaczyńskiego" [KO-I13], aimed at PiS's chairman, and Tusk's "żeby pogonić to zło" (2023-09-19 [KO-I7])".
3. Tusk's "W poniedziałek Polska będzie wolna od PiS-u" (OKO.press, a portal) also names neither power nor government. Either move it into that clause too or mark it as a stated READING.
4. Optionally, in the same edit, point [KO-I13]'s register row (line 450) at §11 instead of "(§5, §12)".

### 5. *(decoded)* marks: TD-P11's mark sits on its plain date while its &nbsp; quote is unmarked; five new &nbsp; quotes are unmarked

- **Lens:** facts - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:120

**The scenario.** The header (lines 3-4) says a quote is marked *(decoded)* where its bytes carry entities or markup inside it. The record follows this elsewhere: NL-P17's plain block quote is unmarked, and its '5 lipca 2023&nbsp;r.' dateline is marked. The fix added the mark to TD-P11's 'pon. 15 maj 2023' (line 120), which is plain bytes on the page. Meanwhile TD-P11's block quote (line 117) carries 'po&nbsp;to', 'żeby&nbsp;odsunąć', 'od&nbsp;władzy' and is unmarked. Five newly quoted strings also carry &nbsp; inside and are unmarked: NL-P30's 'Gdańsk, 15 lipca 2023 r.' (line 165, '2023&nbsp;r.'); NL-P33's 'Pokonamy ich! Zatrzymamy tę brunatną falę' ('tę&nbsp;brunatną') and '… I zatrzymamy to!' ('I&nbsp;zatrzymamy') (lines 170-171); NL-P3's 'PiS trzeba pokonać, opozycja musi to zrobić' ('to&nbsp;zrobić') and NL-P34's 'Podstawowa sprawa to pokonanie PiS' ('to&nbsp;pokonanie') (line 183). The words are verbatim once decoded; only the labels are wrong.

**The fix proposed.** Move TD-P11's *(decoded)* from the date on line 120 to the block quote on line 117. Add *(decoded)* after each of the five strings named.

**The skeptic's evidence.** The finding holds. Its first half is weaker than stated: the TD-P11 mark is a placement ambiguity, not a clear error. I checked the bytes with perl, decoding entities, JSON \u escapes and markup. Each page I read matches its register SHA-256.

The rule is the record's own, in header lines 3-4: "where the bytes carry entities or markup inside the sentence, the quote is the decoded text, marked *(decoded)*". Neither CLAUDE.md nor PolishDeclarationsDiagnostic.cs mentions "decoded", so no check reads the marks.

**The five new strings.** All five are on "+" lines of s780_fixdelta.patch. Each has 0 literal hits on its page and 1 hit after decoding, and no mark sits near any of them:
- :165, [NL-P30]: the page reads "Gdańsk, 15 lipca 2023&nbsp;r. ".
- :170, [NL-P33]: the page reads "Pokonamy ich! Zatrzymamy tę&nbsp;brunatną falę".
- :171, [NL-P33]: the page reads "I&nbsp;zatrzymamy to!". The quote's first part, "Trzeba zatrzymać brunatną falę skrajnej prawicy", is plain.
- :183, [NL-P3]: the page reads "PiS trzeba pokonać, opozycja musi to&nbsp;zrobić".
- :183, [NL-P34]: the page reads "Podstawowa sprawa to&nbsp;pokonanie PiS".

The first review's finding 28 (rev780_first.md:1290, graded minor) reported this same miss. This delta fixed it for [NL-P31] and [NL-P32], then repeated it here.

**TD-P11.** The block quote at :117 has 0 literal hits; the page reads "po&nbsp;to, żeby&nbsp;odsunąć PiS od&nbsp;władzy, ale&nbsp;odsuniemy…". The date "pon. 15 maj 2023" is plain ASCII inside `<span class="is-date is-created">`. The mark at :120 follows the record's own practice of putting it at the end of the attribution, before the tag: [NL-P3] does this at :181 ("2021-05-06 *(decoded)* [NL-P3]"), and finding 28 offered exactly that placement. So it is defensible. But it sits right after a quoted plain date, in the same form :153 uses to mark [NL-P17]'s decoded dateline. ([NL-P17]'s block quote is plain twice, in the meta descriptions; its dateline reads "2023&nbsp;r.") A reader will take :120 as marking the date.

**The rest of the record.** I scanned every quote in the record's prose against every register page. One more new case turned up: :354 "Źródło: TVN24, PAP", credited to [PIS-I1]. On that page the words are split by markup (`Źródło:</dt><dd …>TVN24, PAP`). It is a credit line, like "Opracowanie: Jan Matoga", which this delta did mark at :374. The [TD-P8] quotes at :136-138 are older and are covered by the mark at :137.

The words are verbatim once decoded; only the labels are wrong. The game is unaffected.

**The skeptic's corrected fix.** In ElectionsData/poland/coalition_declarations_2023.md, add *(decoded)* after each of these strings:
- :165 "Gdańsk, 15 lipca 2023 r." *(decoded)*:
- :170 "Pokonamy ich! Zatrzymamy tę brunatną falę" *(decoded)*,
- :171 "Trzeba zatrzymać brunatną falę skrajnej prawicy … I zatrzymamy to!" *(decoded)*;
- :183 "PiS trzeba pokonać, opozycja musi to zrobić" *(decoded)* [NL-P3]
- :183 "Podstawowa sprawa to pokonanie PiS" *(decoded)* [NL-P34]

For [TD-P11], move *(decoded)* from after "pon. 15 maj 2023" on :120 to the end of the block quote on :117, so it reads `> "Musimy … wybór." *(decoded)*`. This is the other placement finding 28 offered, and it stops the plain date reading as decoded.

Optional, to match "Opracowanie: Jan Matoga" *(decoded)* at :374: mark :354 as "Źródło: TVN24, PAP" *(decoded)*. Leave the register rows alone; by the register's practice they carry no mark.

### 6. Stale section pointers: KO-I13 is cited only in §11, and §12 does not cite NL-P34

- **Lens:** facts - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:450

**The scenario.** Three places say KO-I13 serves §5 and §12: its register row ('(§5, §12)'), the register heading (line 380, '... [KO-I13] ... for §12') and the fetch log (raw/declarations_2023/fetch_log.md lines 36-37, 'brought in because the record's §4, §5 and §12 now lean on them'). After KO → PiS was re-dated, the record cites [KO-I13] only at line 296, in §11 ('Kept but not relied on'). NL-P34's row (line 448) says '(§4, §12)', but §12 never cites [NL-P34]; its only citation is line 183, in §4.

**The fix proposed.** KO-I13's row: '(§11)'. NL-P34's row: '(§4)'. Heading: 'for §4, §11 and §12'. Fetch log: 'the record's §4, §11 and §12 now lean on them'.

**The skeptic's evidence.** I checked the bytes and the finding holds. I could not refute it.

Section headings in G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md: §4 starts at line 146, §5 at 190, §11 at 282, §12 at 304, and the register at 380.

A perl pass mapped every [tag] occurrence to its section:
- KO-I13 is cited only at line 296, in §11 ("Restatements of KO → PiS, all later than KO's own start (§5): Budka at KO's campaign launch ... [KO-I13]"). §5 (lines 190-210) and §12 (lines 304-378) never cite it. They also never mention Budka, "pogonimy" or 2023-08-09.
- NL-P34 is cited only at line 183, in §4. §12's "NL → PiS's start" bullet (lines 361-362) names only [NL-P3], the earlier page, so it has no use for NL-P34.
- The other three of the five pages brought in are pointed correctly: NL-I13 (§4 lines 172; §12 lines 344, 359), NL-P33 (§4 line 169; §12 lines 344, 358) and KONF-I26 (§12 line 375).

The register's section pointers mean "where this page is cited". Other rows show this: TD-I14 says "(doubt 12)", NL-P30 says "(§4)", KO-P1..P4 say "(§5)", KONF-P3 says "(§2)", and each matches its citations.

So these are stale:
- Line 450, KO-I13's row: "(§5, §12)".
- Line 448, NL-P34's row: "(§4, §12)".
- Line 380, the register heading: "[KO-I13], [NL-I13], [NL-P33], [NL-P34] and [KONF-I26] for §12". This is wrong for KO-I13 and NL-P34.
- G:/UNITY/Projects/PoliSim/ElectionsData/poland/raw/declarations_2023/fetch_log.md lines 36-37: "brought in because the record's §4, §5 and §12 now lean on them". None of the five is cited in §5, and §11 (KO-I13) is missing.

The patch shows how this happened. KO → PiS was re-dated to 2022-02-09 [KO-P1], and Budka's words moved from §5 and §12 to §11, but the pointers were not updated. Before the move, §5 and §12 named Budka with no tag ("out of tree").

The edit is safe for the checks:
- Only the record and fetch_log.md mention either tag. No .cs file does, so neither is the first tag of any PolandTimeline fact.
- PolishDeclarationsDiagnostic reads that column only through DatedBy, for a fact's first tag, so changing the bracket does not affect any check.

This is a note: a wrong pointer in the record about itself. It does not change the game or any check.

**The skeptic's corrected fix.** coalition_declarations_2023.md:
- Line 450, KO-I13's row: change `"W środę rano" (§5, §12)` to `"W środę rano" (§11)`.
- Line 448, NL-P34's row: change `published 2022-01-31T08:35:20+00:00 (§4, §12)` to `published 2022-01-31T08:35:20+00:00 (§4)`.
- Line 380, the heading: change `[NL-P34] and [KONF-I26] for §12;` to `[NL-P34] and [KONF-I26] for §4, §11 and §12;`.

raw/declarations_2023/fetch_log.md, line 37: change `record's §4, §5 and §12 now lean on them` to `record's §4, §11 and §12 now cite them`. Use "cite" rather than "lean on", because §11 is titled "Kept but not relied on".

### 7. Sweden's KD row gives a GAP reason that its own file retracts, and leaves out §652's condition

- **Lens:** reading - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/sweden/2026/coalition_declarations_2026.md:39

**The scenario.** The row added by the fix says F2 dates KD → S because "a date from the party's own record always wins, and KD's own record is a GAP - its site was not tried, the GAPs below". Lines 767-769 of the same file say that GAP line is outdated: "one KD page ([KD-P1]) was served as static HTML and read, and the manifesto PDF [KD-P2] was fetched and found silent". So the reason the row gives is false.

It also misses the positive check that F2's "with §652's condition" asks for. §652's condition (COMPLETED.md:34592) reads "If a saved MP publication is earlier, use that instead". KD's saved own publications before 2026-09-02 do not carry the refusal:
- [KD-P1], 2026-04-17: on votes it says only that KD will "rösta ja till varje regering som vi själva sitter i".
- [KD-P2], the manifesto, ModDate 2026-08-21: silent on the government and PM question.
So no earlier own-record date wins.

The row also:
- drops "accepted with §652's condition" and the "(read, pending Elias ...)" that CLAUDE.md:53 and the K-1i row carry;
- does not say that KD's own site and Busch's posts were never swept for the refusal (line 491: "posts on X were not fetched"), although Poland's KO record was swept for exactly this clause.

**The fix proposed.** Replace "a date from the party's own record always wins, and KD's own record is a GAP - its site was not tried, the GAPs below" with words to this effect: "accepted with §652's condition ('always wins' read, pending Elias, as `ElectionsData/poland/coalition_declarations_2023.md` states it); KD's saved own publications before that day, [KD-P1] (2026-04-17) and [KD-P2] (the manifesto), do not carry the refusal, so no earlier own-record date wins; KD's site and Busch's own posts were not swept for it".

**The skeptic's evidence.** The fix delta (patch lines 576-589) rewrote line 39 of ElectionsData/sweden/2026/coalition_declarations_2026.md. The row now dates KD → S by F2 and gives this reason: "KD's own record is a GAP - its site was not tried, the GAPs below". That points at line 489 ("KD's site was not tried ... Busch's and Åkesson's posts on X were not fetched", lines 489-491). The same file retracts that line at 767-769: "The line 'KD's site was not tried' in the GAPs above is now outdated: one KD page ([KD-P1]) was served as static HTML and read, and the manifesto PDF [KD-P2] was fetched and found silent." So the reason the row states is false.

I checked the bytes, and both hashes match SHA256SUMS:
- [KD-P1] (e68080aa..., 2026-04-17): its only vote sentence is "Vi har den enkla principen att rösta ja till våra förslag. Och att rösta ja till varje regering som vi själva sitter i ...". There is no nej, nyval or extraval, and no refusal of Andersson in any wording.
- [KD-P2] (6aafa901...), inflated and decoded through its ToUnicode CMaps: no Andersson, nyval, extraval, rösta/röstar, statsminister, Kristersson or blågul. "regering" appears only in the two lines the register names.

So §652's condition, applied to KD, finds no own-record date. The condition reads "If a saved MP publication is earlier, use that instead" (COMPLETED.md:34592). The 2026-09-02 date stands. The row's conclusion is right, but its reason is wrong, and it leaves out the check that the MP row on line 37 states in this form ("MP's one saved publication [MP-P1] ... is later and does not state the condition").

The row also leaves out "accepted with §652's condition" and the pending READING that CLAUDE.md:53 and POLISIM_FEATURE_LIST.md:148 carry.

No sweep of KD's record exists. From KD the tree holds only KD-P1 and KD-P2: no X capture, and no Sweden folder under PoliSim-captures/sources. The Poland record, by contrast, swept KO's own record for this clause (lines 29 and 201-204).

The impact is on the record only:
- DeclaredRedLines.cs's KD comment (lines 351-353) does not repeat the claim.
- The wired D(2026, 9, 2) is unchanged.
- DocumentClaimCheck reads only root *.md and Assets *.cs, and MetaTextCheck reads only C#. Neither reads this file, which is why the named run passed.

The finding's own replacement has two slips:
- It drops F2's "always wins" words.
- It calls the manifesto "before that day", but only its file ModDate (2026-08-21) is known, and §621 dates a document by the decision it records.

**The skeptic's corrected fix.** In ElectionsData/sweden/2026/coalition_declarations_2026.md, line 39 (the KD → S row), make two changes inside the parenthesis:

(a) Change "`COMPLETED.md` §780:" to "`COMPLETED.md` §780, accepted with §652's condition:".

(b) Replace "; a date from the party's own record always wins, and KD's own record is a GAP - its site was not tried, the GAPs below)" with "; a date from the party's own record always wins (read, pending Elias, as §652's condition words it: where the party's own record carries the same declaration earlier, in whatever words) - KD's two saved publications, [KD-P1] (2026-04-17) and the manifesto [KD-P2], do not state the refusal, so no own-record date wins; KD's site was not swept for it and Busch's posts on X were not fetched, the GAPs below)".

Leave everything else as it is: the 2026-09-02 date, the source-ids column (the MP row cites [MP-P1] only in its text), DeclaredRedLines.cs, and lines 489-491, which lines 767-769 already mark as outdated.

### 8. §12's 'Left to Elias' heading says no formation turns on any item, but its F7 item owes a formation change

- **Lens:** reading - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:353

**The scenario.** The fix put the F7-world bullet (lines 363-366) under the existing heading "Left to Elias (no formation turns on any of them):".

That bullet says that if F7's world is the record's count, "F7 decides it in favour of the record (NL in the cabinet), and a formation change is owed". It is the only item whose other reading changes a formation; every other item says that no polling-day line or formation changes.

The heading therefore tells Elias that the one question that can force a formation change under F7's binding acceptance test is as inert as the others.

**The fix proposed.** Reword the heading, for example "(none changes a polling-day line; one reading of F7's world owes a formation change)", or move the F7 item above the list under its own heading.

**The skeptic's evidence.** The finding holds. In G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md, line 353 reads "**Left to Elias (no formation turns on any of them):**". Lines 363-366, under that heading, read: "Which world F7's test means: ... If the record's, the formation's preference for the smaller cabinet is the doubt that blocks KO+TD+NL, F7 decides it in favour of the record (NL in the cabinet), and a formation change is owed." The heading says no item affects a formation, and this bullet says one reading owes a formation change.

How it happened: the heading is not at HEAD; it is new in §780. In the reviewed earlier state (scratchpad decl_stash.md:321) it sat over five items, and none of them moved a formation. The fix delta (s780_fixdelta.patch:213 is unchanged context; :227-230 is the added bullet) put the F7 item under the heading and left the heading as it was.

The bullet's premise is measured. Check (g) in Assets/Editor/PolishDeclarationsDiagnostic.cs:243-252 forms the chamber of record with the game's reader, and the record's own table (line 321, R1) gives "KO+TD on NL's support" there. On the record's-count reading, Elias's choice decides both the formation code (the preference for the smaller cabinet, which every formation uses) and that chamber's result.

The heading matters to readers. The first review's verifiers graded severity from it: rev780_first.md:683 and :1641 say "the record itself says 'no formation turns on any of them'". So the false claim hides the one item that F7 makes binding.

The finding's proposed wording is itself wrong. Its "(none changes a polling-day line; ...)" fails on the NL → Konf item (line 358; §4 line 174). Fact 14 is NL → Konf, cabinet, Open from 2023-07-05 (line 256), and it stands on polling day (line 259). Read as F1's, it becomes one-way and support-blocking from 2023-08-26. That changes a polling-day line, though no formation, because the derived NL–Konf line already blocks support both ways.

No code or check reads the heading. This is a documentation defect only, so it stays minor.

**The skeptic's corrected fix.** Change the heading at line 353 so that only the F7 item is excluded, and keep the formation claim for the rest. For example: "**Left to Elias (no formation turns on any of them but which world F7's test means - read as the record's count, it owes one):**". Another option is to move the "Which world F7's test means" bullet (lines 363-366) out of the list, under its own lead-in such as "**Left to Elias, and a formation turns on it:**".

Do not use the finding's suggested "none changes a polling-day line". The NL → Konf support-half item, read as F1's, turns fact 14 (cabinet, standing on polling day) into a one-way, support-blocking line from 2023-08-26.

### 9. 'Always wins' is credited to §652's wording, which does not say it, and §12 does not say what the other reading would move

- **Lens:** reading - **reviewer:** minor - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:22

**The scenario.** The fix changed the header's reading from "where it carries the same words earlier" (the first-reviewed text, and §776's at COMPLETED.md:37097) to "the same declaration earlier, in whatever words". It still introduces the reading as "as §652's condition words it". CLAUDE.md:53, the K-1i row (POLISIM_FEATURE_LIST.md:148) and DeclaredRedLines.cs:395-396 repeat that attribution.

§652's condition reads only "If a saved MP publication is earlier, use that instead" (COMPLETED.md:34592). It sets no words test and covers saved publications only. So "in whatever words" and "the party's own record" are the record's gloss, not §652's words. The second of these is what made KO's sweep necessary.

§12's item at line 372 ("'Always wins' read as §652's condition") also omits what the other reading moves, which every other item states. The other reading is the literal one, as Elias wrote it and as the report put it to him: "a date from the party's own record wins". Under it, a later own-record date also wins:
- facts 5-7 would start 2023-08-02 on [KONF-P1];
- facts 3-4 would give way to the own record ([KONF-P2], 2023-07-06), whose words come only on or after the dates those facts end;
- no polling-day line would change.

KO → PiS does not depend on the reading: [KO-P1] is KO's own F1 pledge and dates the line under §621 alone.

**The fix proposed.** Call it the record's reading of §652 as applied ([MP-P1] was tested on whether it "states the condition"), not §652's wording, and note that it replaces the record's earlier "the same words". In §12's item, add what the literal reading would move (facts 3-7 move to the own record's dates; no polling-day line) and that KO → PiS's date holds either way. Use the same wording in CLAUDE.md:53, the K-1i row and DeclaredRedLines.cs:395.

**The skeptic's evidence.** Only half of the finding holds up.

**What holds: the §12 item gives no stakes.** Line 372 of ElectionsData/poland/coalition_declarations_2023.md is the only item of the nine under "Left to Elias" (lines 355-379) that does not say what the other reading would move. A grep finds no statement anywhere in the record of what the literal reading would move. Under the literal reading, the own record's date wins even when it is later. I traced it against §8 (lines 247-251) and the saved pages:
- The F2-marked facts are 3-7 and 9.
- Konfederacja's own pages are [KONF-P2], 2023-07-06 (decoded: about PiS only), [KONF-P1], 2023-08-02 ("nie będzie z nikim koalicji"; the article body has no words about Tusk or power), and [KONF-P3], after the vote.
- Fact 3 would start on its own until (07-06), so it goes.
- Fact 4's own words are dated 08-02, after its until (07-13), so it goes too. The finding's "([KONF-P2], 2023-07-06)" fits fact 3 only.
- Facts 5-7 would start 2023-08-02 on [KONF-P1].
- Fact 9 stands from 07-13: no Konfederacja page of its own carries the pledge on Tusk.
- No line standing on polling day changes. This is the same outcome line 379 gives for "if not verbatim".

**What does not hold: the attribution.** §652's condition (COMPLETED.md:34592) reads "If a saved MP publication is earlier, use that instead". It sets no words test, so "in whatever words" restates it. The earlier "the same words" (decl_stash.md:23, COMPLETED.md:37097) was the wording that went beyond §652. "The party's own record" is F2's own phrase (record line 20), so KO's sweep followed F2's words, not a gloss. The change from "the same words" to "the same declaration" moves no fact: no Konfederacja page of its own carries an F2-marked declaration earlier, in any words. KO → PiS is dated by [KO-P1] under §621 alone.

So CLAUDE.md:53, POLISIM_FEATURE_LIST.md:148 and DeclaredRedLines.cs:395 need no change. With the attribution half gone and no formation or polling-day line at stake, I re-grade this from minor to note.

**The skeptic's corrected fix.** Change only line 372 of ElectionsData/poland/coalition_declarations_2023.md, wrapped like its neighbours, to:

"- "Always wins" read as §652's condition (the header's first READING): the own record dates a declaration only where it carries it earlier. Read literally - the own record's date wins even when later - the June lines give way: facts 3 and 4 go (Konfederacja's own words come toward PiS on 2023-07-06 [KONF-P2] and toward KO on 2023-08-02 [KONF-P1], neither before the fact ends), facts 5–7 start 2023-08-02 [KONF-P1], and fact 9 stands from 2023-07-13 (no page of its own before the vote carries the pledge on Tusk) - no polling-day line changes. KO → PiS holds either way: [KO-P1] is KO's own pledge and dates it under §621 alone."

Leave the "as §652's condition words it" wording in CLAUDE.md:53, POLISIM_FEATURE_LIST.md:148 and DeclaredRedLines.cs:395 as it is. If you want the move from "the same words" to "the same declaration, in whatever words" on record, note it once in the COMPLETED.md §780 entry at commit, not in the record.

### 10. 'Always wins' was applied by a sweep to KO only; the facts F2 still dates rest on Konfederacja's unswept own record

- **Lens:** reading - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:28

**The scenario.** The header says F2 still dates the June lines and Konfederacja → KO, and that KO → PiS left F2 "since KO's own record was swept for F2's 'always wins'". Under the same reading, those F2 dates hold only if Konfederacja's own record does not carry the same declarations earlier.

That record was checked only on the pages the §776 sweep happened to save:
- two konfederacja.pl pages;
- two own posts, Mentzen's of 2023-07-06 and Bosak's of 2023-10-16 (fetch_log.md:24; PoliSim-captures/sources/poland_declarations_2023/Konf/SOURCES.tsv).
Nothing like KO's January-2022-to-polling-day sweep of posts and site was run on Mentzen's or Bosak's posts or on konfederacja.pl before 20 June or 13 July.

[KONF-P2] itself (line 80) says "Cały czas mówimy, że chcemy zakończyć rządy PiS", which points at earlier own-record words. Under the record's reading, such words would date fact 3's refusal, or start fact 8, earlier.

§12 states KO's unswept 2021 but says nothing about Konfederacja's own record being checked on saved pages only. No polling-day line or formation turns on it: Poland opens no mid-term round.

**The fix proposed.** Either sweep Konfederacja's own record as KO's was swept (Mentzen's and Bosak's posts through the archive's index and X's syndication endpoint, plus konfederacja.pl), looking for the June refusal and the PO/Tusk pledge; or state in the header, §2 and §12 that Konfederacja's own record was checked on the saved pages only, the way KO's 2021 gap is stated.

**The skeptic's evidence.** I could not refute it. Each of the finding's claims holds against the files.

1. **What the header says.** At coalition_declarations_2023.md:22-23 the record reads "always wins" as: the party's own record dates a declaration where it carries the same declaration earlier. Lines 27-29 say F2 dates "the June lines … and Konfederacja → KO (TVN24's)". They say every other fact is dated by the own record, "KO → PiS among them, since KO's own record was swept for F2's 'always wins' (§5)".

2. **KO's sweep and its limits are stated. Konfederacja's are not.**
   - KO's sweep is described at §5 (:201-204), at §12 (:355-357, "2021 was not swept") and in fetch_log.md:43-51.
   - A grep of the record for swept, sweep and GAP finds only KO's.
   - Nothing in the header, §2, §12 or fetch_log says how far Konfederacja's own record was searched.
   - §2's "Before June, no line" and "toward KO: only 2023-08-02 [KONF-P1] and 2023-10-11 [KONF-I25]" rest on saved pages. So does §12's "Konfederacja's lines start on its own record (2023-07-06, 2023-08-02)" (:377).

3. **Konfederacja's own record was only partly checked.**
   - Out of tree, Konf/SOURCES.tsv lists two konfederacja.pl pages (2022-09-20, and 2023-08-02 [KONF-P1]) and two own posts: Mentzen's of 2023-07-06 and Bosak's of 2023-10-16. fetch_log.md:24 says the same.
   - There is no Konf/own folder; only KO/own exists. The scratchpad holds KO's index sweeps (ko_own/, x/, ko_platforma/) and nothing for Mentzen, Bosak or konfederacja.pl.
   - The find pass (scratchpad f12/find_Konf.txt) concludes "No Konf own record carries these words, so the broadcaster's date stands". That is the same saved-pages wording the first review's finding 11 rejected for KO.

4. **It can occur.**
   - The same find pass logs an unsaved Konfederacja own post that the record never mentions: Mentzen on X, 2023-03-26, known only from Wprost's page (out of tree). Wprost calls these "jednoznaczne wpisy na Twitterze" on a PiS coalition, quoting "Idziemy do tych wyborów nie po to, żeby usiąść z wami do stolika, tylko żeby wam ten stolik wywrócić". That is before the June line's start of 2023-06-20.
   - The record now reads Tusk's "warunki ich kapitulacji" post as "a refusal in other words" (§11). This post would need the same weighing.
   - Two saved pages point to earlier own-record words: [KONF-P2] "Cały czas mówimy…" (:80), and [KONF-I23] "Bardzo wyraźnie mówimy od paru miesięcy: chcemy zakończyć rządy PiS-u i nie dopuścić do rządów Donalda Tuska".

5. **The same change keeps this convention elsewhere.** Sweden's KD row, under the same F2 clause, says "KD's own record is a GAP - its site was not tried".

**Impact.** No polling-day line or formation moves, because Poland opens no mid-term round. The start dates are visible to the player, though: GameController.CampaignDeclared.cs:190 shows StandingOn(country, today) under "DECLARED AS OF" during a run-up that opens 2023-02-19. That makes this minor, the same grade as finding 11.

**The skeptic's corrected fix.** **Smallest fix (text only):**
1. **Header** (coalition_declarations_2023.md:27-29): after "and Konfederacja → KO (TVN24's)", add: "Konfederacja's own record was not swept as KO's was. 'Always wins' was checked for these facts only on the own pages and posts the §776 sweep saved: [KONF-P1], [KONF-P2], [KONF-P3] and konfederacja.pl's page of 2022-09-20. A GAP (§12)."
2. **§2:** qualify "Before June, no line" and "toward KO: only 2023-08-02 … 2023-10-11" as "on the saved pages".
3. **§12, the RMF item** (:377): write "on its saved own record".
4. **§12, "Left to Elias":** add one item. "Konfederacja's F2 dates: its own record (konfederacja.pl, and the X posts of Mentzen, Bosak and the party) was not swept. An earlier own-record refusal would date facts 3-7 from it, an earlier pledge to keep Tusk from power would date fact 9, and an earlier 'zakończyć rządy PiS' would start fact 8 earlier. [KONF-P2]'s 'Cały czas mówimy' and [KONF-I23]'s 'od paru miesięcy' point to earlier words, and Mentzen's own post of 2023-03-26 ('…żeby usiąść z wami do stolika…') is unsaved and unweighed. No polling-day line changes; the run-up's DECLARED page would show the lines from an earlier date."
   - If the item cites Wprost's page by name, bring that page into raw/declarations_2023/Konf/ with a register row and a SHA256SUMS line, as the record's own rule requires.
5. **fetch_log.md:** after the KO paragraph, add one line: "Konfederacja's own record was not swept; its own pages here are the §776 sweep's."

**Better fix:** run KO's sweep on Konfederacja.
- Use the Internet Archive's index of the status URLs for @SlawomirMentzen, @krzysztofbosak and the party's account from January 2023 to 13 July, with text and created_at taken from X's syndication endpoint. Do the same for konfederacja.pl through the archive's index.
- Fetch the 2023-03-26 post and weigh it the way §11 weighed Tusk's 2023-06-09 post.
- If any fact moves, change §8 and DeclaredRedLines.PolandTimeline together, so that PolishDeclarationsDiagnostic's check (a) still matches them.

### 11. The header says the register marks the pages F2 counts, but it marks three, and one of them dates nothing

- **Lens:** reading - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:26

**The scenario.** The header says "The register marks the pages F2 counts **(F2)**". Only [KONF-I4], [KONF-I14] and [KO-I11] carry the mark, and [KO-I11] has dated no fact since KO → PiS moved to [KO-P1].

Broadcasters' own pages that quote a leader verbatim are unmarked:
- [NL-I13]: TVN24, "Źródło: TVN24", Biedroń;
- [KONF-I15]: "Źródło: Fakty po Południu TVN24";
- [KONF-I25]: "Źródło: TVN24";
- [NL-I12]: "Źródło: tvn24.pl";
- [KONF-I23] and [KO-I7]: Polsat News's own pages.
§12 (lines 359-360) treats [NL-I13] as a page that would date NL → Konf's support half. A reader of the header concludes that F2 does not count these pages.

Separately, [KO-I12]'s row reads "TVN24 (live)", although the page credits "Źródło: tvn24.pl, PAP". The rows for [PIS-I1] and [KO-I13] record their PAP credit, which the header's reading treats as decisive.

**The fix proposed.** Either describe the mark as what it is ("the pages F2 dates a fact by") and remove it from [KO-I11], or note that [KO-I11] dated KO → PiS before §5's sweep; or mark every page F2 counts. Record [KO-I12]'s PAP credit in its row.

**The skeptic's evidence.** The finding holds. I checked the record, the code and the saved bytes.

1. **What the record and code say the mark means.** Record lines 26-27: "The register marks the pages F2 counts **(F2)**". Two code comments say the same:
   - `DeclaredRedLines.cs:396`: "The pages F2 counts are marked (F2) in the record's register".
   - `PolishDeclarationsDiagnostic.cs:30` and `:92`: the marked set is "the record's reading of 'the broadcaster's or news agency's own page'".

2. **What is actually marked.** Only three register rows carry "(F2)": line 396 [KONF-I4], line 400 [KONF-I14] and line 438 [KO-I11]. All three marks were added in this fix delta.

3. **[KO-I11] dates nothing now.** I listed the first tag of every `PolandTimeline` fact. Only [KONF-I4] (five June facts) and [KONF-I14] (Konf → KO) are first tags of facts that carry `PolandSpokenWords`. KO → PiS now first-tags [KO-P1] and carries no F2 mark.

4. **Broadcasters' own pages that quote leaders verbatim are unmarked.** I decoded the saved bytes:
   - [NL-I13] (line 449): "Źródło: TVN24", Biedroń quoted ("tarczą przeciwko Konfederacji").
   - [KONF-I15] (line 401): "Źródło: Fakty po Południu TVN24". This is the same programme as the marked [KONF-I14]. Bosak is quoted: "My chcemy PiS odsunąć od władzy - deklaruje Krzysztof Bosak".
   - [KONF-I25] (line 404): "Źródło: TVN24", Mentzen quoted: "Nie zamierzam współtworzyć z rządu…".
   - [NL-I12] (line 428): "Źródło: tvn24.pl".
   - [KONF-I23] and [KO-I7] (lines 403 and 435): publisher "Polsat News", reporting its own programme, "Gość Wydarzeń".
   - [PIS-I4] (line 389): "Źródło: TVN24".

5. **The record itself treats [NL-I13] as a page F2 counts.** §4 (line 172) and §12 (lines 344 and 359-360) call it "TVN24's own page", and line 360 says it "would date it from 2023-09-02". [KO-I11] and [KONF-I15] are now in the same position: each is a broadcaster's own page restating a line that an earlier page dates. One is marked and the other is not. So the marked set fits neither "the pages F2 counts" nor "the pages F2 dates a fact by".

6. **[KO-I12]'s PAP credit is missing.** The page credits "Źródło: tvn24.pl, PAP", but row 439 reads "TVN24 (live)". The rows for [PIS-I1], [KO-I13] and [KO-I3] do record their PAP credit, and the header's reading treats an agency-wire page as dating nothing.

**Why minor and not a defect:** nothing runs differently. The diagnostic uses the mark only to check that facts carrying F2's mark are first-tagged by a marked I-tag page. Those facts still pass, and no fact, date or formation changes.

**The skeptic's corrected fix.** Make the wording match what is marked, rather than marking every broadcast page. Marking them all would need rulings on the PAP-credited pages, and [PIS-I1] is already left to Elias.

1. **Record, lines 26-27:** change "The register marks the pages F2 counts **(F2)**" to "The register marks **(F2)** the pages a fact is dated by under F2 ([KONF-I4], [KONF-I14])". Keep "and `PolishDeclarationsDiagnostic` holds every fact carrying F2's mark to one of them."

2. **Record, line 438:** remove "(F2)" from [KO-I11]'s publisher cell, leaving "TVN24 (Fakty)". Doubt 1 (line 337) already records that it dated KO → PiS before KO's own record was swept.

3. **Record, line 439:** change [KO-I12]'s publisher cell to `TVN24 (live; with PAP: "Źródło: tvn24.pl, PAP")`.

4. **Code comments, same meaning:**
   - `DeclaredRedLines.cs:396`: "The pages F2 dates a fact by are marked (F2) in the record's register."
   - `PolishDeclarationsDiagnostic.cs:30` and `:92`: change "- the record's reading of 'the broadcaster's or news agency's own page'" to "- the pages the record dates an F2 fact by (each the broadcaster's own page, never a relay, a newspaper or a portal)".

After the change, check (b) still passes. The marked set becomes {KONF-I4, KONF-I14}, both I-tags. The six F2-marked facts first-tag these two pages, and their dates match the register cells ("20 czerwca 2023" and "13 lipca 2023").

### 12. Budka's page [KO-I13] still points to §5 and §12, which no longer cite it, and §11's intro misdescribes it

- **Lens:** reading - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:450

**The scenario.** Since the re-dating, [KO-I13] is cited only in §11 (lines 295-296). Several pointers still say otherwise:
- its register row still says "(§5, §12)";
- the register heading (line 380) says it was brought into the tree "for §12";
- fetch_log.md:36-37 says the five pages came in "because the record's §4, §5 and §12 now lean on them";
- [NL-P34]'s row says "(§4, §12)", but §12 does not cite it.

§11's intro (line 284) says its items were "dropped by the sweeps' verifiers — the files stay in the captures folder". [KO-I13], [KO-I4] and [KO-I7] in the KO bullet are restatements held in the tree, not pages the verifiers dropped.

The heading's "saved 2026-10-04" also covers [KO-P1] to [KO-P4], which the 2026-10-05 sweep saved, and its list of later additions leaves them out.

**The fix proposed.** Point [KO-I13]'s row to §11 and [NL-P34]'s to §4. Correct the heading's "for §12" and the fetch log's reason. Give §11's KO bullet its own lead-in (restatements, held in the tree). Add [KO-P1] to [KO-P4] to the heading's 2026-10-05 additions.

**The skeptic's evidence.** Confirmed against the working tree. No formation, date or check result changes. Two pointers in the finding are stale and fully wrong: [KO-I13]'s, and the fetch log's mention of §5. The rest are smaller inaccuracies.

1. [KO-I13] (line 450, "(§5, §12)"). In the record the tag appears only at line 296 (§11, lines 282-302), the heading (380) and its own row (450). No line in §5 (190-210) or §12 (304-378) mentions Budka, "pogonimy" or 2023-08-09. The fix delta removed Budka from §5 ("Earlier and less plain: Budka at KO's campaign launch...") and from §12's "KO → PiS's start" bullet when KO → PiS was re-dated to 2022-02-09 [KO-P1]. The row kept the pointers written when the page came into the tree. The same delta updated three other rows to their current citations: [TD-I14] "doubts 3 and 12" → "doubt 12", [NL-P30] "doubt 8" → "§4", [KONF-P3] "doubt 5" → "§2". So the register's convention is "where the page is cited now", and this row breaks it.

2. Register heading (line 380). It says the five pages came in "for §12", but [NL-P34] is cited only in §4 and [KO-I13] only in §11. It also dates the whole register "saved 2026-10-04". [KO-P1] to [KO-P4] were saved later: rows 45, 65, 108 and 124 of PoliSim-captures/.../KO/own/SOURCES.tsv each say "syndication JSON fetched Mon Oct 5 ... 2026 UTC". The heading's list of 2026-10-05 additions leaves these four out.

3. fetch_log.md lines 36-37: "because the record's §4, §5 and §12 now lean on them". §5 cites none of the five pages, and [KO-I13] is in §11. A related gap, not in the finding: fetch_log.md line 4, "as it was served on 2026-10-04", also misdates [KO-P1] to [KO-P4], though the log's own 2026-10-05 entry (lines 43-51) corrects it.

4. [NL-P34] (line 448, "(§4, §12)"). This is the weakest part. The tag is cited only at line 183 (§4). §12's "NL → PiS's start" bullet (361-362) cites [NL-P3] only. However, the §4 sentence at line 183 itself points to §12 ("the READING above; §12"), so the §12 pointer is loose rather than wrong.

5. §11's intro (line 284): "dropped by the sweeps' verifiers — the files stay in the captures folder". The KO bullet (295-300) now cites [KO-I13], [KO-I4] and [KO-I7], and all three files are in the tree under raw/declarations_2023/KO/ (tvn24_2023-08-09_budka..., polsatnews_2023-09-08_grabiec..., polsatnews_2023-09-19_tusk...). [KO-I4] and [KO-I7] are also relied on in §5's KO → Konf paragraph. Their words reached §11 because of the re-dating, not because a verifier dropped them. Budka's words already sat under this intro before the fix (the stash's §11, line 271), so only the tags and the in-tree files are new.

None of the six named checks covers any of this. PolishDeclarationsDiagnostic reads only the register rows' tag, publisher and date cells; it never reads the "(§N)" pointers, the heading or the fetch log. DocumentClaimCheck does not read the record.

**The skeptic's corrected fix.** Text only; no code changes. Line references are to ElectionsData/poland/coalition_declarations_2023.md unless a different file is named.

1. Line 450, [KO-I13]'s row: change "(§5, §12)" to "(§11)".

2. Line 380, the register heading: replace "and [KO-I13], [NL-I13], [NL-P33], [NL-P34] and [KONF-I26] for §12" with "[NL-P33], [NL-P34], [NL-I13] and [KONF-I26] for §4 and §12, and [KO-I13] (now cited in §11); [KO-P1] to [KO-P4] saved 2026-10-05 by the sweep of KO's own record, for §5".

3. fetch_log.md, lines 36-37: change "the record's §4, §5 and §12 now lean on them" to "the record's §4, §11 and §12 now lean on them". Optionally add: "[KO-I13] came in for KO → PiS's start; since KO's own record re-dated the line it is cited only in §11". Optionally also qualify line 4 ("as it was served on 2026-10-04", except where an entry below says otherwise).

4. Line 295, §11's KO bullet: give it its own lead-in, e.g. "**Restatements of KO → PiS, all later than KO's own start (§5)** - set aside by the re-dating, not by the verifiers; the tagged pages are held in the tree:".

5. Optional: line 448, [NL-P34]'s row: change "(§4, §12)" to "(§4)".

### 13. CLAUDE.md carries F2's pending 'always wins' reading but not its pending 'own page' reading

- **Lens:** reading - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/CLAUDE.md:53

**The scenario.** The standing rule now gives F2 with "(read, pending Elias, as §652's condition ...)", but quotes "the broadcaster's or news agency's own page" without any reading.

The record's second READING is equally pending Elias: the own page is that of the outlet that took the words down, and a broadcaster's relay of another outlet's interview or of an agency's wire, or a newspaper's own video programme, dates nothing. That reading decides doubt 4 (record line 340).

A session that dates another country's declarations from the standing rule alone would count, for example, a broadcaster's page carrying an agency's wire, against the Polish record.

**The fix proposed.** Add the second reading to the F2 clause in the same "(read, pending Elias, …)" form, or point the clause to the record's header for both readings.

**The skeptic's evidence.** Every fact in the finding checks out against the working tree and the saved bytes.

1. CLAUDE.md:53, the F2 clause. It quotes the ruling's own words, "quoted verbatim on the broadcaster's or news agency's own page and dated by that page". It then adds one gloss: "(read, pending Elias, as §652's condition words it: where the party's own record carries the same declaration earlier, in whatever words)". A grep of CLAUDE.md finds no "relay", "outlet", "newspaper" or "took the words down". docs/archive/CLAUDE_HEAD_LONGFORM.md does not mention F2 or §780.

2. The record, ElectionsData/poland/coalition_declarations_2023.md:
   - Lines 22-26 state two readings: "always wins" as §652's condition, and the own page as the page of the outlet that took the words down. Under the second, a broadcaster's relay of another outlet's interview or of an agency's wire dates nothing, and neither does a newspaper's own video programme. The header says: "Both readings are Elias's to confirm (§12)."
   - Line 340, doubt 4: "decided by F2 as read (the header's second READING)".
   - Line 75: "Read the other way, the opening is dated 2023-06-26 by [KONF-I8] or [KONF-I11]".
   - §12's "Left to Elias" list gives both readings and what each would move.

3. The bytes, CROSS/rmf24_2023-06-26_mentzen-zmienia-zdanie-koalicja-z-pis.html. Its sha256 dbff146a… matches the register.
   - Publisher "RMF24"; datePublished 2023-06-26T08:31:44+02:00; credit "Źródło: RMF24/PAP".
   - It carries Mentzen's words verbatim: "Jeśli obie partie, czy to PiS, czy PO, zgodzą się realizować nasz program, to wszystko jest na stole - mówi Sławomir Mentzen w wywiadzie dla "Super Expressu"".
   - Read generically, CLAUDE.md's words admit this page. The first review's finding 30 skeptic reached the same conclusion; the record's reading excludes it.
   - The two readings give different PolandTimeline dates. Under the other reading, facts 3-7 would end on 2023-06-26 and Konf → TD/NL/MN would restart on 2023-08-02. That is the same kind of change the literal "always wins" caused, and for that one the first review's findings 15/35 drove a gloss into CLAUDE.md.

4. How the fix handled each first-review finding (rev780_done1.md):
   - Findings 6/17/30 (the own-page reading) were fixed in the record and in DeclaredRedLines' basis strings only.
   - Findings 15/35 added only the "always wins" reading to CLAUDE.md and to the K-1i row (POLISIM_FEATURE_LIST.md:148). That row has the same gap.
   - No feature-list or ERRANDS row tracks the own-page reading as open. Only the record's §12 and the uncommitted §780 draft (rec780.md, which lists both readings as owed) do.

Why this stays a note:
- COMPLETED.md has no §780 yet. The draft states both readings, so the § pointer will lead to them once it is written.
- No fact in the tree is mis-dated today. PolishDeclarationsDiagnostic holds Poland's F2-dated facts to the register's (F2) marks.
- Sweden's only F2-dated fact, [KD-I1], is SVT asking Busch its own question ("SVT: Om det inte råder kris eller krig…"), so it qualifies under either reading.
- CLAUDE.md quotes the ruling faithfully. The gap is an asymmetric, missing gloss, not a misquote.
- The risk is a later session applying the standing rule to another country and counting a broadcaster's relay page that the Polish record excludes.

**The skeptic's corrected fix.** In CLAUDE.md:53, extend the existing parenthetical. After "...where the party's own record carries the same declaration earlier, in whatever words", add: "; and the own page as the page of the outlet that took the words down - a broadcaster's relay of another outlet's interview or of an agency's wire, or a newspaper's own video programme, dates nothing (both readings: `ElectionsData/poland/coalition_declarations_2023.md`'s header)". Make the same addition to the K-1i row's answer (3) in POLISIM_FEATURE_LIST.md:148, so the head and the row agree, as the fix for finding 35 intended. No code or data change.

### 14. F2 date check: DatedBy accepts wrong from-dates and rejects right ones

- **Lens:** checks - **reviewer:** defect - **skeptic:** minor
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:103

**The scenario.** DatedBy runs cell.Contains over the whole page's-own-date cell. The Polish day is unpadded with no left boundary, and every date in the cell counts. I replicated it over 2019-2023 against the register. [TD-P11] (fact 10) accepts 2023-07-01 and [KONF-I4] (facts 3-7) accepts 2023-08-04: these are the dateModified stamps this fix wrote into those cells. [KONF-I14] (fact 9) accepts 2023-07-03, because "3 lipca 2023" sits inside "13 lipca 2023". [NL-P30] accepts 2023-07-05 and [KO-I11] accepts 2023-10-02. So re-dating TD -> PiS to its page's edit day (array and row 10) passes (a), F1, F2, LiftedSince and the formation checks. So does a D(2023,7,3) typo on Konf -> KO with fact 4's until moved in step. The reverse also happens: [KONF-P1]'s cell "2 sierpnia, 2023" accepts no date at all (the comma). The restart that §12 names under F2's other reading (Konf -> TD, NL, MN from 2023-08-02 [KONF-P1]) would therefore fail on a correct edit. [TD-P9] and [TD-P10] ("12 paź 2023") are the same.

**The fix proposed.** Compare from against the cell's FIRST date token only, which is the page's own date before any dateModified/createdDate/published note. Parse with anchored patterns: \d{4}-\d{2}-\d{2}, \d{2}\.\d{2}\.\d{4}, and (?<!\d)(\d{1,2}) (stycznia|...|grudnia),? (\d{4}). Require equality, and fail on a cell with no parseable date.

**The skeptic's evidence.** I rebuilt DatedBy (PolishDeclarationsDiagnostic.cs:102-105) in perl. It uses the same register regex, group 4 as the date cell, ordinal Contains and an unpadded day, read from the record (CRLF, UTF-8, no BOM). I ran it over every day from 2019 to 2023 (scratchpad s780v/datedby.pl).

Every claim in the finding holds:
- **[TD-P11]** accepts 2023-05-15 and 2023-07-01 (its dateModified).
- **[KONF-I4]** accepts 2023-06-20 and 2023-08-04 (the record's own "site restamp").
- **[KONF-I14]** accepts 2023-07-03 and 2023-07-13 ("3 lipca 2023" sits inside "13 lipca 2023").
- **[NL-P30]** accepts 07-05, 07-15 and 07-17. **[KO-I11]** accepts 2023-10-02 and 10-12. **[NL-P32]** also accepts 2023-01-01.
- **[KONF-P1]** accepts no date. The saved page (raw/declarations_2023/Konf/konfederacja_koalicja-z-pis-czy-z-platforma-z-nikim.html) really reads "2 sierpnia, 2023", so the cell is a faithful quote and DatedBy is what fails.
- **[TD-P9] and [TD-P10]** ("paź") accept nothing.

No other check covers this. Check (a) only holds the §8 table equal to the array, so a mirrored edit passes it. The LiftedSince check in DeclarationDatesDiagnostic skips open facts. DocumentClaimCheck checks code-member names, not dates. So re-dating TD → PiS to 2023-07-01 in both the array and row 10 passes every check, and the record says that date is wrong (if the edit added the words, the fallback is TD-P6 2023-08-10).

The reverse case can be reached too. §12 says that if [KONF-I4] is not verbatim, Konf → TD, NL and MN start on 2023-08-02 [KONF-P1]. That correct edit would turn this check red.

Why minor and not defect: all 15 current froms equal the first date in their first tag's cell (PIS-P1, PIS-P2, KONF-I4 x5, KONF-P2, KONF-I14, TD-P11, TD-P6, NL-P3, NL-P31, NL-P17, KO-P1). Nothing is misdated today and the printed "ok" is true. The weakness only shows on a future edit made in both places, and the false rejection fails loudly. One gap in the finding's own fix: its full-month alternation still cannot read "paź" (TD-P9/TD-P10).

**The skeptic's corrected fix.** At PolishDeclarationsDiagnostic.cs:102-105, compare the from against the FIRST date the cell gives, parsed and required equal. Use the 3-letter month stems so both "października" and "paź" parse:

string[] months = { "sty", "lut", "mar", "kwi", "maj", "cze", "lip", "sie", "wrz", "paź", "lis", "gru" };
// the page's own date is the FIRST date the register's cell gives; a dateModified, createdDate or second stamp after it dates nothing
bool DatedBy(DateTime from, string cell)
{
    if (cell == null) { return false; }
    Match d = Regex.Match(cell, @"(?<!\d)(?:(\d{4})-(\d{2})-(\d{2})|(\d{2})\.(\d{2})\.(\d{4})|(\d{1,2}) (sty|lut|mar|kwi|maj|cze|lip|sie|wrz|paź|lis|gru)\p{L}*,? (\d{4}))");
    if (!d.Success) { return false; }
    int G(int g) => int.Parse(d.Groups[g].Value, CultureInfo.InvariantCulture);
    DateTime day = d.Groups[1].Success ? new DateTime(G(1), G(2), G(3)) : d.Groups[4].Success ? new DateTime(G(6), G(5), G(4))
        : new DateTime(G(9), Array.IndexOf(months, d.Groups[8].Value) + 1, G(7));
    return day == from;
}

I checked the same parser in perl (scratchpad s780v/firsttoken.pl):
- All 15 current facts still pass.
- The alternatives §12 names pass: KONF-P1 2023-08-02 and TD-P6 2023-08-10.
- TD-P11 2023-07-01, KONF-I4 2023-08-04 and KONF-I14 2023-07-03 are rejected.
- Every register cell parses, TD-P9 and TD-P10 included.

Also say "the first date the register's cell gives" in the comment at :92-94 and in the Check message at :115.

### 15. Register heading, two register rows and the fetch log point at sections that no longer cite them

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:450

**The scenario.** The KO -> PiS re-dating moved Budka's words to §11, and [KO-I13] is now cited only there. Its row still says "(§5, §12)". The heading (line 380) lists it "for §12". fetch_log.md:37 says the five pages came in because "the record's §4, §5 and §12 now lean on them", but none of the five is cited in §5. [NL-P34] (line 448) says "(§4, §12)", yet §12 never cites it: its NL -> PiS item names only [NL-P3]. The heading says "saved 2026-10-04" and its list of exceptions leaves out [KO-P1]-[KO-P4], which the KO sweep saved on 2026-10-05. The heading also says every file is out of tree at the same relative path. Those four are absent there: they sit renamed under KO/own/ (every other register file is present and byte-identical).

**The fix proposed.** Change [KO-I13] to "(§11)" and [NL-P34] to "(§4)". In the heading, list [KO-I13] for §11 and [NL-P34] for §4, and add "[KO-P1]-[KO-P4] saved 2026-10-05 by the sweep of KO's own record, out of tree renamed under KO/own/". In fetch_log.md:37, change "§4, §5 and §12" to "§4, §11 and §12".

**The skeptic's evidence.** I could not refute any part of this finding; I checked each claim against the file bytes.

1. **[KO-I13] row (coalition_declarations_2023.md:450) says "(§5, §12)".** A grep of the record finds `[KO-I13]` only at line 296 (§11, "Restatements of KO → PiS, all later than KO's own start (§5)"), at the heading (380) and at its own row (450). "Budka" appears only at lines 295 and 300, both in §11.
   - §5 (lines 190–210) now cites [KO-P1]–[KO-P4], [KO-I11], [KO-I4] and [KO-I7], and no longer mentions Budka.
   - §12's "KO → PiS's start" item (355–357) names only [KO-P1].
   - So both pointers went stale when the fix delta moved Budka's words from §5 and §12 into §11.

2. **[NL-P34] row (448) says "(§4, §12)".** The tag is cited only at line 183 (§4). §12's "NL → PiS's start" item (361) names only [NL-P3]. §4 does hand the reading to §12 with "(the READING above; §12)", so the §12 pointer is loose rather than wholly wrong.

3. **Heading (380) lists [KO-I13] and [NL-P34] "for §12", and says the register was saved 2026-10-04.**
   - The heading's exceptions leave out [KO-P1]–[KO-P4]. Out of tree, `KO/own/SOURCES.tsv` rows 45, 65, 108 and 124 record those four as fetched "Mon Oct 5 … 2026 UTC", and the file mtimes are 2026-10-05.
   - The five pages and [NL-P31]/[NL-P32] really were saved 2026-10-04: their mtimes are 2026-10-04 20:17–20:28, and NL's urls.tsv, Konf's SOURCES.tsv and KO's SOURCES.tsv also say 2026-10-04.

4. **Heading's out-of-tree claim.** I read the file column of all 70 register rows and hashed each file in the tree and out of tree.
   - 66 files are present out of tree at the same relative path, byte-identical to the register digest.
   - The four files below are missing at that path. They exist only renamed under `KO/own/`, with the register's digests (6b5e5906…, 0feb4a30…, 03ce1691…, 516b4509…):
     - `KO/x_donaldtusk_1491474675771355145_syndication.json`
     - `KO/x_obywatelska_ko_1494201933447544834_syndication.json`
     - `KO/x_obywatelska_ko_1526989992060502016_syndication.json`
     - `KO/x_obywatelska_ko_1704443105661886953_syndication.json`
   - The renamed copies are `x_donaldtusk_2022-02-09_1491474675771355145.json` and `x_Platforma_org_<date>_<id>.json`.
   - Only fetch_log.md:47–50 records the renaming; the record does not.

5. **fetch_log.md:37** says "the record's §4, §5 and §12 now lean on them". Where the five are cited:
   - [NL-P33]: §4 and §12
   - [NL-P34]: §4
   - [NL-I13]: §4 and §12
   - [KO-I13]: §11
   - [KONF-I26]: §12

   None is cited in §5, and §11 is not named.

None of the six checks would catch this, and the fix breaks none of them:
- No code parses the heading or the fetch log; a grep of Assets for "Source register" and "fetch_log" finds nothing.
- DocumentClaimCheck checks only code-member claims.
- PolishDeclarationsDiagnostic reads a register row's date cell only for a fact's first tag. Neither [KO-I13] nor [NL-P34] is a first tag, so editing those cells is safe.

This is a real but minor inaccuracy in a SOURCED record's provenance and cross-references. It has no effect on game behaviour.

**The skeptic's corrected fix.** Text-only edits:

1. **coalition_declarations_2023.md:450** ([KO-I13]'s date cell): change "(§5, §12)" to "(§11)".

2. **coalition_declarations_2023.md:448** ([NL-P34]'s date cell): change "(§4, §12)" to "(§4)". This is for consistency with rows that point to the section citing the tag. Keeping "§12" is defensible, because §4 hands its reading to §12.

3. **coalition_declarations_2023.md:380** (heading): change "and [KO-I13], [NL-I13], [NL-P33], [NL-P34] and [KONF-I26] for §12;" to "and [KO-I13], [NL-I13], [NL-P33], [NL-P34] and [KONF-I26] for §4, §11 and §12; [KO-P1]–[KO-P4] saved 2026-10-05 by the sweep of KO's own record (§5), held out of tree renamed under `KO/own/` (`fetch_log.md`);". The rest of the heading stays as it is.

4. **fetch_log.md:37**: change "the record's §4, §5 and §12 now lean on them" to "the record's §4, §11 and §12 now lean on them".

5. **Optional, same cause, fetch_log.md:4**: after "as it was served on 2026-10-04", add "(KO's four posts [KO-P1]–[KO-P4] on 2026-10-05, below)".

### 16. New NL quotes decoded from &nbsp; lack *(decoded)*; TD-P11's literal date gained one

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:170

**The scenario.** The record's own rule (lines 3-4) is that a quote whose bytes carry entities inside the sentence is marked *(decoded)*. The fix applied the mark to [NL-P31], [NL-P32] and [KO-I4]. The quotes it added have entities in the saved bytes but no mark. [NL-P33]: Biejat's "Zatrzymamy tę&nbsp;brunatną falę" and Zandberg's "I&nbsp;zatrzymamy to!" (lines 170-171). [NL-P34]: "Podstawowa sprawa to&nbsp;pokonanie PiS" (line 183). [NL-P3]: "opozycja musi to&nbsp;zrobić" (line 183). The opposite case is line 120: "pon. 15 maj 2023" [TD-P11] occurs literally in the bytes, with no entity or markup, but now carries *(decoded)*.

**The fix proposed.** Add *(decoded)* after the Biejat and Zandberg quotes and after the [NL-P3] and [NL-P34] quotes. Drop it from line 120.

**The skeptic's evidence.** The finding is half right. The four missing marks are real. The claim about line 120 is wrong, and its fix would undo a confirmed earlier fix.

The record's rule (coalition_declarations_2023.md:3-4): "where the bytes carry entities or markup inside the sentence, the quote is the decoded text, marked *(decoded)*". The rule covers quotes whose only decode is &nbsp;. The fix itself marked [NL-P31] and [NL-P32] (:185-187) for that case alone.

I counted each quote's hits in its saved page with perl: literal bytes against &nbsp; read as a space. None of these four quotes is in HEAD, and none carries a mark:
- **Biejat, [NL-P33], :170.** NL/lewica_org_20230826_wiec_czestochowa.html reads "Pokonamy ich! Zatrzymamy tę&nbsp;brunatną falę". 0 literal hits, 1 decoded.
- **Zandberg, [NL-P33], :171.** The same page reads "I&nbsp;zatrzymamy to!". 0 literal, 1 decoded. The part before the ellipsis is plain.
- **[NL-P34], :183.** NL/lewica_org_20220131_czarzasty_musimy_wspolrzadzic.html reads "Podstawowa sprawa to&nbsp;pokonanie PiS". 0 literal, 1 decoded.
- **[NL-P3], :183.** NL/lewica_org_20210506_czarzasty_rozmawial_z_diablem.html reads "PiS trzeba pokonać, opozycja musi to&nbsp;zrobić". 0 literal, 1 decoded.

The shorter "PiS trzeba pokonać" at :361 is plain, so it needs no mark.

**Severity.** No code reads the mark: a grep of Assets for "(decoded)" finds nothing. This is record hygiene against the record's own stated rule, so minor. The first review graded the same class (its finding 28) minor too.

**Line 120 is refuted.** "pon. 15 maj 2023" is plain in the page (its line 88), but the mark does not belong to that date. It sits in the gloss just before the tag. That is where the record puts a block quote's mark: :181 reads "published 2021-05-06 *(decoded)* [NL-P3]", which marks the block quote at :178, whose bytes are "Z&nbsp;PiS-em nigdy w&nbsp;życiu…".

The [TD-P11] block quote at :117 has 0 literal hits. Its bytes read "po&nbsp;to, żeby&nbsp;odsunąć PiS od&nbsp;władzy, ale&nbsp;odsuniemy…". The first review's finding 28 told the author to put this exact mark "in the gloss just before the tag, as §4 does for [NL-P3]", and that is what was done. Dropping it would leave the block quote unmarked and reopen finding 28, which was itself a repeat of a miss from the §776 review.

**Other quotes the fix added are plain in their bytes:**
- the titles of [NL-P33], [NL-P25] and [NL-P30];
- Biedroń's three quotes [NL-I13];
- [KO-P1] to [KO-P4], [KONF-P3] and [KONF-I26];
- [KO-I13]'s two quotes and [KO-I7].

**Two items of the same class that are not sentences:**
- **:165, "Gdańsk, 15 lipca 2023 r." [NL-P30].** The bytes read "2023&nbsp;r.". The same section marks [NL-P17]'s dateline at :153.
- **:354, "Źródło: TVN24, PAP" [PIS-I1].** The bytes read `<dt>Źródło:</dt><dd>TVN24, PAP</dd>`. The fix marked "Opracowanie: Jan Matoga" at :374 for the same reason, markup inside a credit line.

**The skeptic's corrected fix.** In ElectionsData/poland/coalition_declarations_2023.md, add the mark after the four quotes:

- **:170.** `Biejat, "Pokonamy ich! Zatrzymamy tę brunatną falę" *(decoded)*,`
- **:170-171.** `Zandberg, "Trzeba zatrzymać brunatną falę skrajnej prawicy … I zatrzymamy to!" *(decoded)*;`
- **:183.** `"PiS trzeba pokonać, opozycja musi to zrobić" *(decoded)* [NL-P3]`
- **:183.** `"Podstawowa sprawa to pokonanie PiS" *(decoded)* [NL-P34]`

Leave line 120's mark as it is. It marks the [TD-P11] block quote at :117, whose bytes carry &nbsp;, in the gloss position :181 uses for [NL-P3]. Removing it would reopen the first review's finding 28.

Optional, for consistency (a dateline and a credit line, not sentences):
- `"Gdańsk, 15 lipca 2023 r." *(decoded)*` at :165;
- `"Źródło: TVN24, PAP" *(decoded)*` at :354.

### 17. Sweden KD -> S row repeats 'its site was not tried', which the same file marks outdated

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/sweden/2026/coalition_declarations_2026.md:39

**The scenario.** The row's new F2 text says "KD's own record is a GAP - its site was not tried, the GAPs below". Lines 767-769 of the same file say that line (489) is outdated. [KD-P1], KD's own page of 2026-04-17, was served and read, and the manifesto [KD-P2] was fetched and found silent. Neither carries the refusal of Andersson (checked). So under F2's "always wins" as read, 2026-09-02 stands on the pages that were read. What stays unswept is Busch's own posts on X (line 491). That is the kind of own record whose sweep moved KO -> PiS back about 20 months in this same change. POLISIM_FEATURE_LIST.md:148's "KD's date stands" carries the same gap without saying so.

**The fix proposed.** Reword to: "KD's own pages as read ([KD-P1], [KD-P2]) do not carry it; Busch's posts on X were not fetched (a GAP)". Qualify K-1i's "KD's date stands" the same way, or sweep Busch's X before 2026-09-02 as KO's record was swept.

**The skeptic's evidence.** I could not refute this. The new text was added by this change: `git diff HEAD` shows line 39's KD → S row gaining "a date from the party's own record always wins, and KD's own record is a GAP - its site was not tried, the GAPs below".

The same file contradicts that claim, and the contradiction was already in HEAD (d00b66aa) before this change:
- Line 489 is the old GAP line, "KD's site was not tried (... party_leaders_2022.md ... JavaScript-rendered)".
- Lines 767-769 say that line "in the GAPs above is now outdated: one KD page ([KD-P1]) was served as static HTML and read, and the manifesto PDF [KD-P2] was fetched and found silent."
- Both pages are on disk under ElectionsData/sweden/2026/raw/declarations/. Their sha256 values match the register and SHA256SUMS: [KD-P1] e68080af..., [KD-P2] 6aafa901...

So "its site was not tried" is false by the file's own record, and it is pointed at the very GAP the file marks outdated.

Under F2 as read in this change, the party's own record decides the date, so what was checked matters. I checked it:
- **[KD-P1]** (Busch's speech, 2026-04-17), decoded: "Andersson" appears 3 times, all policy attacks (two of them as "Magdalena Andersson"). There is no "nyval", "extraval" or "rösta nej". The only words near the subject are "rösta ja till varje regering som vi själva sitter i". That is a yes-rule, not a refusal of Andersson.
- **[KD-P2]** (the manifesto), extracted with the scratchpad's pdftext.pl: no "Andersson", "Magdalena", "nyval", "extraval" or "Socialdemokrat".

The date 2026-09-02 ([KD-I1], SVT's own live page) therefore still holds on the pages read, and no date or wiring changes. What is genuinely unswept is the rest of KD's own record: Busch's posts on X (line 490-491, "not fetched") and KD's static news archive between Kvartal/DI's first report (2026-06-05, [KD-I3]) and 2026-09-02. No KD sweep exists under PoliSim-captures/sources. This is the same kind of own-record sweep that moved KO → PiS back about 20 months in this change.

This is in scope (the task lists the KD row among the fixes) and is a documentation defect only.

The K-1i part of the finding (POLISIM_FEATURE_LIST.md:148, "KD's date stands") is weaker. That row makes no false statement; it gives a conclusion without saying how far the sweep went. Qualifying it is optional.

**The skeptic's corrected fix.** ElectionsData/sweden/2026/coalition_declarations_2026.md line 39: replace "and KD's own record is a GAP - its site was not tried, the GAPs below" with "and KD's own pages as read - [KD-P1] (2026-04-17) and the manifesto [KD-P2] - do not carry it; the rest of KD's own record (its news archive, Busch's posts on X) was not swept, a GAP". This drops the pointer to line 489's GAP, which lines 767-769 already mark outdated.

Optional, POLISIM_FEATURE_LIST.md:148: change "KD's date stands (Busch's words on SVT's own live page)" to "KD's date stands on the record as swept (Busch's words on SVT's own live page; KD's own pages read do not carry it, Busch's X not fetched)".

The alternative to rewording is to sweep KD's own record between 2026-06-05 and 2026-09-02, as KO's was swept.

### 18. §12 transcribes Poland's start date, a figure the code derives

- **Lens:** checks - **reviewer:** minor - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:356

**The scenario.** "No game date turns on it: Poland opens 2023-02-19" (lines 356-357) and "(Poland opens 2023-02-19)" (line 362) copy WorldClock.StartDate(CountryId.Poland). That value is not ruled: it is CampaignCalendar's standard run-up counted back from 2023-10-15. The claim convention allows a derived figure only as generated, referenced or deleted. If the run-up length changes, both lines become wrong while every check stays green; DocumentClaimCheck sees only Type.Member references.

**The fix proposed.** Write "both dates fall before Poland's start (`WorldClock.StartDate`)". The argument only needs the start to fall after 2022-09-20.

**The skeptic's evidence.** The finding is real, but I'd grade it note, not minor.

- **The lines are new in this change.** Lines 356-357 and 362 of ElectionsData/poland/coalition_declarations_2023.md are '+' lines in s780_fixdelta.patch (patch lines 220-221 and 226). HEAD has no "2023-02-19" anywhere in this file. The wording came from the first review's own suggested fix (rev780_first.md:139, "No formation turns on it: Poland opens 2023-02-19, ...").
- **The date is computed in code, not ruled.** `WorldClock.StartDate(Poland)` (WorldClock.cs:111-121) takes the default branch, `new CampaignCalendar(LatestElectionDay(id)).PreCampaignStart`. That is 2023-10-15 minus 7×8 days minus 7×26 days, which gives 2023-02-19, so the record is right today.
- **Both inputs are marked as drafts.** CampaignClock.cs:70-75 tags `DefaultCampaignWeeks = 8` and `DefaultPreCampaignWeeks = 26` as [AUTHORED-DRAFT], and the 26 as "Strikeable". The 26 is still entry 5 of §189's play-calibration list, waiting to be judged.
- **No ruling sets Poland's date.** The §618 rulings fix only the snap starts and France. StartPoints.cs:72 gives Poland's basis as "the run-up by the standard window (§618)". The Poland records file chose not to transcribe the date: records_by_date.md:330-331 says "the standard run-up before 2023-10-15 ... The spec's start rule's date itself (26 weeks + 8 weeks before 2023-10-15) is not computed here."
- **The convention covers this.** CLAUDE.md:36-41 calls "a figure" about the code DERIVED and allows it only as GENERATED, REFERENCED or DELETED: "Nobody transcribes, anywhere, in any file." Only COMPLETED.md is exempt. The first review's skeptic also exempted the dated "Measured (2026-10-05, ..., the run nf1a)" block. These bullets are in "Left to Elias", which is open work, written in the present tense and citing no run.
- **No check would catch a change.** DocumentClaimCheck.cs:121 scans only root *.md files (TopDirectoryOnly), and it matches only backticked `Type.Member`. PolishDeclarationsDiagnostic reads only the record's §8 table and register rows. If the run-up length changes, both sentences become false and every check stays green.
- **This repo has already called this a breach.** Reviews/2026-10-05_s782_us1_start_words.md:724 judged the USA's run-up date copied into a comment "computed, not ruled ... a DERIVED figure", a breach of the claim convention.

**Why note and not minor.** The sentences are true today and nothing at runtime reads them. The conclusion ("no game date / no formation turns on it") holds for any run-up shorter than about 56 weeks, because the start then still falls after 2022-09-20. This repo grades a transcription that is accurate today as note: first review #40 and s773 item 14.

**One correction to the proposed fix.** "both dates fall before ..." fits line 362 only. Line 356 names a single date (2022-02-09) plus a year that was not swept.

**The skeptic's corrected fix.** In ElectionsData/poland/coalition_declarations_2023.md, replace the transcribed date with the ruled rule and a pointer to the code. records_by_date.md §5 and the spec's §4 row already use this form.

Lines 356-357: change "No game date turns on it: Poland opens 2023-02-19, and the line stands from the start either way." to "No game date turns on it: Poland opens at the standard run-up before 2023-10-15 (`WorldClock.StartDate`), and the line stands from the start either way."

Line 362: change "no formation turns on it (Poland opens 2023-02-19)." to "no formation turns on it (both dates fall before Poland's start, the standard run-up before 2023-10-15 - `WorldClock.StartDate`)."

Leave the rest of the record unchanged. Code and checks need no change.

### 19. 'The register marks the pages F2 counts' overstates the (F2) marks the F2 check trusts

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:26

**The scenario.** Only [KONF-I4], [KONF-I14] and [KO-I11] carry (F2). The record itself treats [NL-I13] as "TVN24's own page" ("Źródło: TVN24"), yet it has no mark; nor do [KONF-I15] ("Fakty po Południu TVN24") and [KONF-I25] ("Źródło: TVN24"). [KO-I11] is marked although no fact is dated by it now. The same wording is at DeclaredRedLines.cs:396. The F2 check (PolishDeclarationsDiagnostic.cs:100) trusts the marks as given. Adding "(F2)" to a relay such as [KONF-I11] ("Źródło: RMF24/PAP") or [PIS-I1] ("TVN24, PAP") and dating a fact by it passes. Under §12's NL -> Konf alternative, a correct fact first-tagged [NL-I13] fails until someone marks it.

**The fix proposed.** Reword the record (line 26) and DeclaredRedLines.cs:396 to "the pages a fact is dated by under F2", or mark every broadcaster or agency own page. In the check, refuse "(F2)" on a publisher cell that names PAP or "reporting".

**The skeptic's evidence.** The claim checks out. The record's header (coalition_declarations_2023.md:26-27) says "The register marks the pages F2 counts **(F2)**". DeclaredRedLines.cs:396 says the same. PolishDeclarationsDiagnostic.cs:30 and :92 go further and call the mark "the record's reading of 'the broadcaster's or news agency's own page'". The register's actual marks fit neither reading of that sentence.

Which rows are marked:
- Only three of the 68 register rows carry (F2): [KONF-I4] (line 396), [KONF-I14] (line 400) and [KO-I11] (line 438).
- The six facts carrying F2's mark (PolandSpokenWords) all first-tag [KONF-I4] or [KONF-I14].
- KO -> PiS now first-tags [KO-P1] (DeclaredRedLines.cs:444) and cites [KO-I11] only as a restatement. So [KO-I11] dates nothing, yet it is marked. That rules out the narrow reading ("the pages a fact is dated by").

Unmarked pages F2 counts by the record's own terms:
- **[NL-I13] (line 449):** the saved bytes credit "Źródło: TVN24" and give Biedroń's words as direct speech ("My tę czarno-brunatną koalicję zatrzymamy - zapowiedział"). The record itself calls it "TVN24's own page" in §4, in doubt 8 and in §12. §12 says it "would date it from 2023-09-02 only if the own page did not count".
- **[KONF-I15] (line 401):** the credit is "Fakty po Południu TVN24", the same as the marked [KONF-I14]. The speaker is the same co-chairman, Bosak, and the quote style is the same dash-led direct speech ("My chcemy PiS odsunąć od władzy - deklaruje Krzysztof Bosak").
- **[KONF-I25] (line 404):** the credit is "TVN24", with Mentzen's direct speech ("- Nie zamierzam współtworzyć ... - dodał").

That rules out the broad reading ("every page F2 counts").

No runtime effect: the check at PolishDeclarationsDiagnostic.cs:100/114 only holds marked facts to marked pages, and it passes. Nothing else quotes the marked set or its size.

The parts of the finding I refute:
- **Mis-marking a relay "passes":** this describes a hypothetical future edit, not a present defect. The check's message says exactly what it verifies. Refusing "(F2)" on any cell naming PAP would build in a reading the record leaves to Elias ([PIS-I1], §12).
- **A fact dated by [NL-I13] failing until the page is marked:** that is the check working as designed.

**The skeptic's corrected fix.** Text only; the check and the data stay as they are.
1. Record lines 26-27: replace "The register marks the pages F2 counts **(F2)**, and `PolishDeclarationsDiagnostic` holds every fact carrying F2's mark to one of them." with "The register marks **(F2)** the pages a fact here is, or was, dated by under F2 ([KO-I11] dated KO → PiS until KO's own record was swept, §5). This is not every page F2 would count: TVN24's own pages [NL-I13], [KONF-I15] and [KONF-I25] date nothing here and carry no mark. `PolishDeclarationsDiagnostic` holds every fact carrying F2's mark to a marked page."
2. DeclaredRedLines.cs:396: replace "The pages F2 counts are marked (F2) in the record's register." with "The pages a Polish fact is, or was, dated by under F2 are marked (F2) in the record's register."
3. PolishDeclarationsDiagnostic.cs:30 and :92: replace "- the record's reading of \"the broadcaster's or news agency's own page\"" with "- a page the record dates a fact by under its reading of \"the broadcaster's or news agency's own page\"".
An equally small alternative: drop "(F2)" from [KO-I11]'s publisher cell (line 438) and say "is dated by" instead of "is, or was, dated by". The check still passes: two marked pages, both -I tags, and all six marked facts first-tag [KONF-I4] or [KONF-I14].
The check needs no hardening for this finding.

### 20. (a) checks the §8 basis cell's F2 mark but not its first tag

- **Lens:** checks - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:77

**The scenario.** Row 15's basis was "[KO-I11] - F2" before this fix and is "[KO-P1], [KO-P2]" after it. Suppose the date and the "- F2" had been updated but the tag left as [KO-I11]. Then (a) passes because the dates and F2 marks agree, and the F2 check passes because it reads only the array. The table would name a page that no longer dates the fact. Today every row's first tag equals its fact's first tag, and no check enforces that.

**The fix proposed.** Add && FirstTag(rows[i].Basis) == FirstTag(f.Basis), using the same \[[A-Z]+-[A-Z]+\d+\] regex.

**The skeptic's evidence.** I tried to refute this and could not. Every factual claim in the finding holds against the working tree. Nothing is wrong in the data today.

1. Check (a) captures the basis cell but holds only its F2 mark.
   - The fix widened the §8 regex so group 8 captures the basis cell (PolishDeclarationsDiagnostic.cs:59, :64).
   - The only test on that cell is `rows[i].Basis.Contains("- F2") == f.Basis.Contains(PolandSpokenWords)` (line 77).
   - The table's tags are never compared with the array.

2. Nothing else reads §8's basis tags.
   - The F2 check takes its first tag from `f.Basis`, the array (line 108). It dates the fact by that tag's register cell.
   - Check (b) reads only the array's tags (lines 84-87).
   - DocumentClaimCheck matches only backticked `Type.Member` references.
   - Neither MetaTextCheck nor FormationSweepDiagnostic reads ElectionsData or the record.

3. Row 15 changed as the finding says. The patch's §8 hunk goes from "| 2023-10-12 | Open | [KO-I11] - F2 |" to "| 2022-02-09 | Open | [KO-P1], [KO-P2] |".

4. The data is consistent today. A perl script (scratchpad/s780_firsttag_check.pl) parsed the exact bytes of §8 and of PolandTimeline. It read 15 rows and 15 facts, and every row's first tag equals its fact's first tag (0 mismatches). The proposed check therefore passes today.

5. The missed edit is one the record expects to make. Its own §12 "Left to Elias" list names re-datings that change only a row's first tag, with no F2 mark change:
   - Line 370: TD → PiS would move to 2023-08-10 on [TD-P6] instead of [TD-P11].
   - Line 361: NL → PiS would start 2021-05-06 on [NL-P3] instead of [NL-P31].

   Suppose the array and the table's from cell are updated, but row 10's basis stays [TD-P11]:
   - (a) passes, because from, until, shape and the absent F2 mark all agree.
   - The F2 check passes, because [TD-P6] is TD's own page and its register date cell contains 2023-08-10.
   - The table then cites a 15 May page for a line it dates 10 August.

6. The broader text claims more than the check holds. The record's header says "§8 is that array, and `PolishDeclarationsDiagnostic` holds the two to each other" (lines 8-9). CheckSuite.cs:214 says "the timeline table" is held. The diagnostic's own summary (lines 20-21) and message (line 79) list what (a) holds and do not claim tags.

Grade: note. It changes no game behaviour or formation, it is only a stale citation in a documentation table, and the current data is correct.

**The skeptic's corrected fix.** In Assets/Editor/PolishDeclarationsDiagnostic.cs, extend check (a)'s conjunction at line 77 with:

    && rows[i].Basis.Contains("- F2") == f.Basis.Contains(DeclaredRedLines.PolandSpokenWords)
    && Regex.Match(rows[i].Basis, @"\[[A-Z]+-[A-Z]\d+\]").Value == Regex.Match(f.Basis, @"\[[A-Z]+-[A-Z]\d+\]").Value;

This uses the same first-tag pattern as the F2 check at line 108. The row then must name the page that check dates the fact by.
- If a row cites no tag, its empty match fails against the fact's tag.
- Check (b) already requires every fact to cite a tag.
- Today all 15 rows match, so the bar stays green.

Then make the words match the check:
- Line 79 message: "its shape words, F2 marks and first tags included".
- Lines 73-74 comment and the summary at lines 20-21: add "and its first tag (the page that dates it)".

## The second pass - refuted by the skeptics

- [facts] KO-P2..KO-P4 were posted by @Platforma_org (PO's account), not '@Obywatelska_KO, KO's own account' - *The historical fact in the finding is true. The claim that the cell "misstates who published" does not hold.

1. It is the same publisher. All three in-tree files (KO/x_obywatelska_ko_1494201933447544834, _1526989992060502016 and _1704443105661886953_syndication.json) carry "user":{"id_str":"53003895","name":"Koalicja Obywatelska","screen_name":"Obywatelska_KO"}. Their digests match the out-of-tre*
- [reading] F7's world is read without the definition F4 installed for the same phrase on the same day - *Refuted. The record's reading is the one F7's own words admit. F4's installed definition does not separate the two worlds at the 2023 polling day, and the first review already settled this question.

1. F7's words pin the world. "a Polish game's own 2023 election" repeats the report Elias answered. That report (main transcript c8a2f04f...jsonl, line 11708) said: "a Polish game's own 2023 result no*
- [reading] Sweden's §639 'AS WIRED' paragraph is the one place left that still puts KD's date to Elias - *The text the finding quotes is there, but it is not a problem with this change.

1. **The change never touched the paragraph.** `git diff HEAD --stat` shows 2 changed lines in ElectionsData/sweden/2026/coalition_declarations_2026.md, and both are the KD → S row (line 39). That row now records the ruling: "ruled 2026-10-05, Elias's ruling F2, COMPLETED.md §780 ...". Lines 707-713 come from the §639*
- [checks] §780 is cited for the verifiers' reading, but COMPLETED.md has no §780 yet - *The facts are true, but they describe the normal state before a commit, and the fix the finding asks for is already drafted.

- Line 375 of ElectionsData/poland/coalition_declarations_2023.md reads "(§776's reading; the verifiers' reading is recorded in `COMPLETED.md` §780)". COMPLETED.md is unmodified and its last heading is "## 779." (line 37292).
- The repo adds a COMPLETED.md section in its ch*

## What the author did about the second pass

- **1** (KO's own account carries "obalimy te filary złej władzy" on 2022-01-29, earlier than [KO-P1], and the record did not weigh it) - §5 weighs it as earlier and less plain (power named, PiS not; the pledge is against its pillars) and the same day's "przegonimy PiS" as defeat; §12 puts it to Elias (read as F1's, it starts KO → PiS then); the post is in the tree as [KO-P5]; "the earliest pledge in F1's form" where the record said "the earliest pledge".
- **14** (the date check matched substrings: "3 lipca" inside "13 lipca") - it parses the FIRST date the register's cell gives (ISO, dd.MM.yyyy, or a Polish day, month stem and year) and requires it equal to the fact's from; a dateModified after it dates nothing. Named run n780c: 7 of 7.
- **20** ((a) did not hold the row's first tag) - each §8 row's first tag must be its fact's first tag.
- **3, 11, 19** (the (F2) mark did not mean "the pages F2 counts") - it now marks the pages a fact is dated by under F2 ([KONF-I4], [KONF-I14]); [KO-I11] loses it, since KO's own record dates KO → PiS; the header says other broadcasters' own pages count but date nothing and carry no mark; the code and the diagnostic say the same.
- **10** (Konfederacja's own record unswept, though the facts F2 still dates are its own) - stated as a GAP in the header, §2 ("on the pages saved"), §12 and the fetch log; the sweep KO had is running for Konfederacja, its result the next record's.
- **9** ("always wins" read as §652's condition, the other reading unmeasured) - §12 says what the literal reading would move (the June lines; no polling-day line).
- **13** (CLAUDE.md carried one pending reading of two) - CLAUDE.md and the K-1i row carry both.
- **2, 7, 17** (Sweden's KD row cited a GAP its own file withdraws, and omitted §652's condition) - the row says KD's own pages as read ([KD-P1], [KD-P2]) do not carry the refusal, the rest of its own record is a GAP, and F2 is accepted with §652's condition.
- **4** (§11 called "pogonić" restatements against §4's READING) - §11 splits the words that name power from the later words toward PiS that are not F1's in terms.
- **5, 16** (decoded marks) - added where the bytes carry &nbsp; ([TD-P11]'s mark stays, in the gloss position §4 uses).
- **6, 12, 15** (stale pointers) - [KO-I13] points to §11, [NL-P34] to §4; the register heading and the fetch log name what each page is for.
- **8** (§12's heading said no formation turns on any item) - it excepts F7's world.
- **18** (a transcribed start date) - `WorldClock.StartDate` named instead.
- **Refuted (4):** KO's posts are on KO's own account (the same account id, renamed from @Platforma_org); F7's world is read consistently with F4's; Sweden's §639 paragraph is history; the §780 reference resolves with this record.
