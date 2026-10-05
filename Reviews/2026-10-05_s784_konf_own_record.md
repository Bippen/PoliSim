# Review - §784, Konfederacja's declarations dated by its own record (2026-10-05)

A workflow review (`polisim-staged-review`) of the change that applies Elias's ruling F2 ("a date from the party's own record always wins") to Konfederacja after the sweep of its own record - `DeclaredRedLines.PolandTimeline` facts 3 to 9, the record `ElectionsData/poland/coalition_declarations_2023.md` (its header, §1, §2, §8, §12 and register), the three pages brought into `raw/declarations_2023/Konf/`, and `PolishDeclarationsDiagnostic`'s F2 check - in three lenses: the record held to the rulings, the code and its checks, the sources themselves (each reader opening the sweep's saved pages out of tree). Every finding was put to a refute-first skeptic; the reports are verbatim. A second pass read the change after the fixes, in two lenses. Not a money path (`Tools/bar_tier.ps1` names none of these files), so no ledger row.

## The first pass - confirmed (verbatim)

Three lenses - the record held to the rulings, the code and its checks, the sources themselves - over the staged diff at `7d71fcc4`; 24 findings survived their skeptics, two graded defects.

### 1. §1 and fact 2 still refuse to start a line on words aimed at a person, while §2 and facts 8-9 now do exactly that

- **Lens:** rulings - **reviewer:** defect - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:57

**The scenario.** §1 (lines 55-57) says Kaczyński's "Ten człowiek nie może rządzić Polską" [PIS-I1] "keys KO only through the premise §2 states for Konfederacja's line to KO, so it is not read as the start". Fact 2's basis (DeclaredRedLines.cs:418) gives the same reason: "aimed at the person, not the party ... not read as the start". After this change, §2 (lines 95-99) and facts 8-9 (DeclaredRedLines.cs:430, 432) START Konfederacja's lines on [KONF-P5]. That post names only Morawiecki and Tusk, and it reaches PiS and KO only through premises, one of them new ("Morawiecki's rule is PiS's"). Before §784 the KO line's start, [KONF-I14], at least named PO in its coalition half; now no party is named. So the record answers one question two ways. Apply §1's rule and facts 8-9 cannot start on 2023-03-27. Apply §2's rule and PiS → KO is held back only by [PIS-I1]'s page under F2. The change also reverses §780's default for [KO-P5] (lines 237-240 and COMPLETED.md:37332: "power is named, PiS is not", so it was listed as earlier and less plain, not the start, and put to Elias). Here the words that name no party become the start and the plainer later words go to Elias, and no reason is given.

**The fix proposed.** Reconcile the record and the code in one of two ways. (a) State in §2 why words naming the sitting prime minister's rule or a leader's return start Konfederacja's lines, and change §1's and fact 2's reason for [PIS-I1] to its F2 page condition alone (§12 line 392 already gives that condition). (b) Keep §780's default: start facts 8-9 on own-record words that name the party (PiS: 2023-07-06 [KONF-P2]) and list [KONF-P5] as earlier and less plain, for Elias.

**The skeptic's evidence.** I could not refute the core of the finding. The staged text gives two answers to one question: does a pledge aimed at a leader or a prime minister start that party's line?

**Says no (unchanged by the diff):**
- Record line 57 (§1): [PIS-I1] "keys KO only through the premise §2 states for Konfederacja's line to KO, so it is not read as the start (§12)."
- DeclaredRedLines.cs:418 (fact 2): "Earlier and aimed at the person, not the party: ... [PIS-I1] ... - not read as the start."
- Record line 392 (§12): leaves "if a pledge against a party's leader keys the party" open for Elias.
- §11, line 337: Budka's "pogonimy Kaczyńskiego" is set aside as "aimed at PiS's chairman".

**Says yes (new in the diff):**
- Record lines 95-99 (§2) and DeclaredRedLines.cs:429-432 (facts 8 and 9) start both lines on 2023-03-27 [KONF-P5].
- I verified the bytes: created_at 2023-03-27T07:58:39Z, "Nie przyłożymy ręki ani do kontynuacji władzy Morawieckiego, ani do powrotu do władzy Tuska." It names no party.
- The record keys PiS and KO through stated premises. "Morawiecki's rule is PiS's" is new. At HEAD fact 8 started on [KONF-P2], "zakończyć rządy PiS", with no premise.
- The only alternative put to Elias, [KONF-P6] ("Chcemy odsunąć od władzy Kaczyńskiego i Tuska", published 2023-06-28T08:41:03Z), is aimed at persons too, yet it is called "F1's own forms".

**Why §1's distinction no longer holds.** At HEAD, fact 9's start [KONF-I14] named PO in the same quote ("nie zamierzamy zawierać koalicji z PO"), so §1's word "only" separated the cases. The change removes the party name from both starts and does not touch §1, fact 2, §11 or the §12 bullet. Read by its stated reason, §1 now describes fact 9's start exactly.

**What holds up against the finding (partial refutation):**
1. The KO half was already keyed through the Tusk premise at HEAD. [KONF-P5]'s Tusk clause is close to [KONF-I14]'s, so the header's "always wins, in whatever words" READING supports the earlier date.
2. The party names are on the own record six days earlier. [KONF-P4] (verified, 2023-03-21T15:15:46Z) says "ani z PiSem, ani z Platformą". But the record never states this as the distinction.
3. "Morawiecki's rule" names the sitting government. That differs from K-1f's leader-for-a-future-cabinet premise.
4. The [KO-P5] sub-claim is overstated:
   - [KO-P5] names no actor and aims at "filary" (pillars).
   - The diff does not touch [KO-P5].
   - §2 does give a reason for reading [KONF-P5] as the line.
5. [PIS-I1] has an independent second condition in §12, its "Źródło: TVN24, PAP" page, so PiS → KO would not move under fix (a).

**Severity: minor, not a defect.** Only the record and the basis strings are affected:
- No polling-day line moves, and run n784a is clean.
- Poland opens 2023-02-19 (CampaignClock: 8 + 26 weeks before 2023-10-15), before every competing start date. Even so, nothing reads those dates today:
  - GameController.CampaignDeclared.cs:30-31 says "LiveCampaignSetup stages none for Poland, so its timeline shows on no page yet".
  - Record lines 11-12 say Poland's confidence rules are Unsourced, "so no round opens".
- Precedent: Reviews/2026-10-05_s780 finding 12 was a question put to Elias "with the weaker words and the wrong condition", record only, graded minor by both reviewer and skeptic.

**Related, outside this finding.** Konf/own/SOURCES.tsv row 40 holds Bosak's own post of 2022-07-20T21:59:39Z: "Polską rządzą nieodpowiedzialni szkodnicy. Nie tylko trzeba ich odsunąć od władzy". The sweep notes it as the "earliest such own-record line found". §2's list of earlier words does not weigh it, although §2's READING ("names power") would reach it.

**The skeptic's corrected fix.** Reconcile the text without moving any date by default:

1. §1 (record line 57) and fact 2's basis (DeclaredRedLines.cs:418):
   - Drop "aimed at the person, not the party" and "keys KO only through the premise ... so" as the reason.
   - Say instead that [PIS-I1] would key KO on the premise §2 reads for Konfederacja's lines (K-1f's, pending Elias).
   - Give the reason it is not read as the start:
     - its page is credited "Źródło: TVN24, PAP", and under the header's second READING an agency's wire relayed dates nothing;
     - PiS's own page of the day, [PIS-P1], does not carry the words (§12).

2. §12's PiS → KO bullet (line 392): say that the leader-keys-party question is read as §2 reads it, pending Elias. Only the page condition then holds the start back.

3. §2 and §12's Konfederacja bullet:
   - State the anchor: [KONF-P4] named PiS and the Platform six days before [KONF-P5], and [KONF-P5]'s Tusk clause is the same declaration as [KONF-I14]'s (F2's "always wins").
   - Add the reading §1's old reason implies. If words aimed at a leader or prime minister do not key the party, facts 8 and 9 start on party-named words, because [KONF-P6] is aimed at persons too:
     - Konf → PiS on [KONF-P2], 2023-07-06;
     - Konf → KO, as at HEAD, on [KONF-I14], 2023-07-13, which names PO beside Tusk.
   - Note that no polling-day line changes under this reading.

4. Optional: weigh Bosak's own post of 2022-07-20 (sweep SOURCES.tsv row 40) under §2's READING, or say why it does not count. The likely reason is his role before the co-chairmanship of 2023-02-14.

### 2. Bosak's own post of 2022-07-20, which the sweep rated 'plain end-PiS-rule; earliest such own-record line found', is neither used nor listed

- **Lens:** rulings - **reviewer:** defect - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:415

**The scenario.** In SOURCES.tsv, the row for Konf/own/x_krzysztofbosak_2022-07-20_1549876718659407873.json reads "plain end-PiS-rule; earliest such own-record line found". The post is on Bosak's own account and quotes a post about "Premier @MorawieckiM"'s coal embargo: "Polską rządzą nieodpowiedzialni szkodnicy. Nie tylko trzeba ich odsunąć od władzy, ale wymienić w parlamencie." That is F1's "remove from power" form, and §784's own premise (Morawiecki's rule is PiS's) points it at PiS. So under §2's READING and under the forms-only alternative alike, fact 8 would start on 2022-07-20 and fact 3 would go. That follows the precedents of [KO-P1] ("odsunięcie PiS od władzy") and [NL-P31] ("aby odsunąć PiS od władzy"). Neither §2's list (lines 132-141) nor §12 (lines 415-421) mentions the post. §12's "an earlier plain line it missed would move these dates earlier still" leaves a reader taking 2023-03-27 as the earliest line the sweep found, which the sweep's own notes contradict. The likely reason to exclude it is that Bosak became co-chairman only on 2023-02-14 (the party's own post 1625540441478254592). That reason is stated nowhere, and fetch_log.md:57 calls @krzysztofbosak a co-chairman's account swept from January 2022. Nothing in play moves: a Polish world opens 2023-02-19, and no run-up page or round reads these lines before polling day.

**The fix proposed.** Either list the post in §2 with the reason it is not the start, written as a READING (for example, not yet a leader whose words are the party's), and give the other reading's consequence in §12 (fact 8 from 2022-07-20, fact 3 dropped). Or start fact 8 on it and bring the file into the tree.

**The skeptic's evidence.** I could not refute the omission. The finding's claim that fact 8 "would" start on 2022-07-20 under both readings is overstated, so I grade it minor rather than defect.

1. The sweep rates the post the earliest such line. SOURCES.tsv row 40 (`x_krzysztofbosak_2022-07-20_1549876718659407873.json`, sha256 204cd787...) reads "(plain end-PiS-rule; earliest such own-record line found)". The JSON shows: screen_name krzysztofbosak, created_at 2022-07-20T21:59:39Z, isEdited false. Its text is "Polską rządzą nieodpowiedzialni szkodnicy. Nie tylko trzeba ich odsunąć od władzy, ale wymienić w parlamencie. Potrzebujemy głębokiej wymiany kadr politycznych." It quotes @RobertBe15's "Premier @MorawieckiM wprowadził embargo na rosyjski węgiel...".
   - The sweep separates two claims. Row 33 calls [KONF-P5] the "earliest own-record line found" for the joint PiS+Tusk line. Row 40 calls this post the earliest end-PiS-rule line.

2. Nothing in the tree mentions the post. `git grep` over the index and the working tree for 1549876718659407873, "szkodnicy" and "2022-07-20" finds nothing in ElectionsData/poland or in the code. COMPLETED.md is not staged and has no §784.
   - Staged record §2, lines 132-141: the "Earlier and less plain, not read as the start" list does not include it.
   - Staged §12, lines 419-421: "an earlier plain line it missed would move these dates earlier still ... Earlier own-record words, less plain, are listed in §2." The post is earlier, own-record and rated plain, so it falls under neither statement.
   - §11 lists nothing the sweep's verifiers dropped.

3. The record's own standard is to list such a candidate with its reason. §5, lines 237-241, lists [KO-P5] as "Earlier and less plain, not read as the start ... power is named, PiS is not". §2 notes even Bosak's RN-account words of 2022-06-15 as "not the party's own".
   - The staged fetch_log.md, lines 57-59, sweeps "its co-chairmen @krzysztofbosak and @SlawomirMentzen ... January 2022 to 13 July 2023".
   - The plausible ground for exclusion, row 30's "co-chairmen named 2023-02-14", is written nowhere in the tree.
   - CLAUDE.md requires a premise a later session depends on to be written to the repo. The repo's convention also requires every READING to be stated.

4. Why minor, not defect:
   - **The date is defensible.** Two grounds the record already uses elsewhere would keep fact 8 on 2023-03-27:
     - The role date: co-chairmen were named on 2023-02-14 (row 30). Before that, Bosak's own account may not count as Konfederacja's own record.
     - The [KO-P5] precedent: power is named, PiS is not. Here the rulers are identified only through a third party's post about Morawiecki.
   - So the finding does not show the fact is wrong. It shows the reading that decides it is unstated, and that §12 misdescribes what the sweep found.
   - **Nothing in play moves, as the finding says:**
     - GameController.CampaignDeclared.cs:32-35 opens the DECLARED page only when a live campaign is staged.
     - LiveCampaignSetup.cs:148 stages Sweden and Germany only.
     - The record, lines 11-12, says no Polish formation round opens.
     - Fact 8 stands on 2023-10-15 from either start date, and fact 3 ends before polling day either way.

5. A side note: the §2 list is a selection. Other unlisted own-record rows include:
   - Bosak's own post of 2023-02-11, "Precz z nową komuną PiSu i Morawieckiego".
   - The party account's 2023-01-11 post quoting Winnicki: "...PiS jak i PO odesłać do lamusa".

   So §12's "are listed in §2" claims more completeness than the list has.

**The skeptic's corrected fix.** Keep fact 8 at 2023-03-27 and fact 3 as built unless Elias rules otherwise. Write the deciding reading into the record:

1. **§2.** Add Bosak's own post of 2022-07-20 to the "Earlier and less plain, not read as the start" list, with its file name. State the READING that excludes it, either or both of:
   - (a) Before 2023-02-14 a member party figure's own account is not Konfederacja's own record. Cite the party's own post 1625540441478254592, where the co-chairmen were named.
   - (b) As [KO-P5]: power is named, PiS is not. The rulers are named only through a third party's post about Morawiecki.

2. **§12.** Replace the implication that no earlier plain line was found. Name this post and give the other reading's consequence: read as counting, fact 8 starts 2022-07-20 and fact 3 goes; no polling-day line changes and nothing in play moves. Say that the §2 list is a selection, or complete it (for example Bosak 2023-02-11, and the party account's 2023-01-11 post quoting Winnicki).

3. **fetch_log.md.** If (a) is the ground, square lines 57-59 with it. They currently call @krzysztofbosak a co-chairman's account swept from January 2022.

4. **Raw files.** Bring the post, and the role-evidence post if it is cited, into raw/declarations_2023/Konf/ with SHA256SUMS only if the record cites them by tag.

### 3. The reason 'an interest, not a refusal' cannot cover Mentzen's quiz 'NIE', and applied evenly it would also dismiss [KONF-P4]

- **Lens:** rulings - **reviewer:** defect - **skeptic:** defect
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:137

**The scenario.** Lines 137-140 list two own-record posts and give them one reason, "an interest, not a refusal". The first is Mentzen's 'NIE' on the party's own account, 2023-03-01: "Red.: Jeśli obieca obniżkę podatków, poprę Tuska na premiera. @SlawomirMentzen: NIE ❌". The second is the 21 March 14:51Z post "Nie jesteśmy zainteresowani współtworzeniem rządu z @MorawieckiM". A 'NIE' is a refusal. It refuses to back Tusk for prime minister, which is the support refusal that §2's READING (lines 95-98) reads as F1's line toward KO through the K-1f premise, and Mentzen had been co-chairman since 2023-02-14. By the header's first READING it is the same declaration, earlier, so fact 9 would start 2023-03-01 rather than 2023-03-27, and fact 4 would go. Separately, "nie jesteśmy zainteresowani" (we are not interested) has the same standing as [KONF-P4]'s "Z nikim nie chcemy wchodzić w koalicję" (we do not want), from the same clip 24 minutes later. A test that dismisses one dismisses the other, which would take away the 21 March start of facts 3-7. The 14:51 post on its own moves no date, since it falls on the same day.

**The fix proposed.** Give each item its own reason. For the quiz, either state a READING (a one-word quiz answer to a conditional is not read as a declaration) with its consequence in §12 (fact 9 from 2023-03-01, fact 4 dropped), or start fact 9 there. For the 14:51 post, give a distinction that does not also catch "nie chcemy", or read it as the same day's cabinet refusal toward PiS, which moves nothing.

**The skeptic's evidence.** I could not refute either half. The quiz half is a real error in the record. The 14:51 half is a real inconsistency, but it moves no date.

**The record's sentence (staged coalition_declarations_2023.md, lines 137-140).** It ends: Mentzen's "NIE" to "poprę Tuska na premiera" in a radio quiz, 2023-03-01 (`x_KONFEDERACJA__2023-03-01_...json`), and, 24 minutes before the line, "Nie jesteśmy zainteresowani współtworzeniem rządu z @MorawieckiM" (`...1638191609131982848.json`) - an interest, not a refusal;
- Each single item in that list carries its own " - reason". This pair is joined by "and" and shares one reason, so the record calls the quiz answer "not a refusal". Read the other way, the quiz has no reason at all.
- Nothing else in the tree gives one. A grep finds no general READING on one-word answers or on an editor's propositions, and COMPLETED.md is unstaged.

**The quiz post itself (out of tree).** `x_KONFEDERACJA__2023-03-01_1630855869221990400.json` is the @KONFEDERACJA_ account, created_at 2023-03-01T09:01:53Z, isEdited false. Its text reads "Red.: Jeśli obieca obniżkę podatków, poprę Tuska na premiera.\n@SlawomirMentzen: NIE ❌".
- That is a refusal to back Tusk for prime minister.
- The sweep's own SOURCES.tsv row 60 hedges on it: "(less-plain/plain keep-Tusk-from-power: would not back Tusk as PM)".
- Mentzen was already co-chairman. `x_KONFEDERACJA__2023-02-14_1625540441478254592.json` (2023-02-14T17:00:16Z) reads "Rada Liderów powołała @krzysztofbosak i @SlawomirMentzen na współprzewodniczących".

**The record's own READINGs reach the quiz.**
- The header (lines 22-23): the party's own record dates a declaration where it carries the same declaration earlier, "in whatever words".
- §2 (lines 95-98): F1's line is "neither joins nor supports a cabinet that includes X". Lending no hand to "a leader's return to power ... names power and refuses both". Toward KO, the premise is "a cabinet holding KO is Tusk's".
- A NO to backing Tusk for PM names power (the premiership) and refuses support. Joining his cabinet would require backing him. So by these READINGs it can be fact 9's line, 26 days before [KONF-P5].
- Staged DeclaredRedLines.cs still has fact 9 at `D(2023, 3, 27)` (line 431) and fact 4 at `D(2023, 3, 21)`–`D(2023, 3, 27)` (line 421).

**§12 does not put the alternative to Elias.**
- For KO, lines 393-396 state that [KO-P5] "would start it then". For NL, lines 400-401 state that "PiS trzeba pokonać" makes the line support-blocking "from its first day".
- The Konfederacja bullet (lines 415-421) says only "Earlier own-record words, less plain, are listed in §2". No consequence is stated for the quiz.

**The 14:51 half.** The post (14:51:30Z, against [KONF-P4] at 15:15:46Z) names Morawiecki and "PiSu". The sweep's SOURCES.tsv row 45 calls it "(plain no-coalition-with-PiS)".
- The record treats similar wording as refusals elsewhere: "Z nikim nie chcemy" (line 72) and "Nie zamierzam współtworzyć z rządu ani z Koalicją Obywatelską, ani z PiS-em" (line 120).
- So "an interest, not a refusal" draws no principled line against those.
- It is the same day, so no date moves, as the finding itself concedes.

**Impact.**
- No polling-day line changes: facts 8 and 9 stand on 2023-10-15 either way.
- A Polish game opens 34 weeks before 2023-10-15, on 2023-02-19 (WorldClock.StartDate, CampaignCalendar). So 1 to 27 March falls inside a game.
- Today nothing in play reads those days. LiveCampaignSetup stages only Sweden and Germany (LiveCampaignSetup.cs:148, 176). The DECLARED page is therefore unavailable for Poland (GameController.CampaignDeclared.cs:32-35). The formation reads only polling day.
- The defect is in the record and its dating under F2 as read, not in runtime behaviour. It is graded a defect because it misdescribes a source on the exact question this change settles: the earliest own-record date for Konfederacja's lines.

**The skeptic's corrected fix.** **In §2, lines 137-140: give each item its own reason.**

1. **The quiz.** Do one of these two things.
   - (a) State a READING, for example: "a one-word answer to an editor's conditional proposition - the words are the editor's, the condition a promise - is not read as a declaration (READING, stated; §12)". Then add to §12's Konfederacja bullet: "Read as the line in its own words (§2's READING; K-1f's premise), Mentzen's 'NIE' to 'poprę Tuska na premiera' on the party's own account, 2023-03-01, starts fact 9 then, and fact 4 does not stand - no polling-day line changes; Elias's."
   - (b) Adopt it.
     - Bring `x_KONFEDERACJA__2023-03-01_1630855869221990400.json` into `raw/declarations_2023/Konf/` byte for byte as a new [KONF-P*] tag. Add its register row, its SHA256SUMS line and its fetch_log mention.
     - Set fact 9 to `D(2023, 3, 1)`, first-tagged by that page. Drop fact 4, or end it.
     - Update §2's head, §8 rows 4 and 9, §12 doubt 5 and the Konfederacja bullet, and the fact-9 basis in DeclaredRedLines.cs.
     - Rerun PolishDeclarationsDiagnostic.
2. **The 14:51 post.** Drop "an interest, not a refusal". That reason would equally catch "nie chcemy" (line 72) and "Nie zamierzam współtworzyć ... rządu" (line 120). Read it instead as the same day's cabinet refusal toward PiS: it carries the line with [KONF-P4] and moves no date (the premise: Morawiecki's government is PiS's).

### 4. The party's own post of 27 March, 07:33Z ('Interesują nas ministerstwa'), is not weighed among the words that did not lift 'z nikim'

- **Lens:** rulings - **reviewer:** defect - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:81

**The scenario.** SOURCES.tsv flags Konf/own/x_KONFEDERACJA__2023-03-27_1640255696192122882.json as a "counter-signal: open to government posts". It reads: "Współprzewodniczący @KONFEDERACJA_ @krzysztofbosak w @PolsatNewsPL: Interesują nas ministerstwa, gdzie będziemy mogli realizować nasz program ...". It was posted six days after [KONF-P4] and 25 minutes before [KONF-P5], on the party's own account, so F2 cannot set it aside the way it sets aside [KONF-I3]. Its programme condition is the one the 26 June opening turns on, and the one [KONF-P4]'s own second sentence gives. Yet 'Not lifted between them' (lines 81-83) weighs only Bosak's 24 March assessment and the 30 March portal interview. Read as an opening, the post ends facts 5-7 on 2023-03-27. The next 'z nikim' is then RMF's 20 June page [KONF-I4], because the own record's [KONF-P1] comes later. Those facts would again be dated by F2 and carry PolandSpokenWords. That contradicts the header's "no fact here is dated by F2" (line 26) and revives the verbatim doubt (lines 426-427, "changes nothing").

**The fix proposed.** Weigh the post in 'Not lifted between them' as a stated READING. For example: an interest in ministries names no partner, and the same appearance's words 25 minutes later refuse Morawiecki and Tusk, so 'z nikim' stands. Give the other reading's consequence in §12.

**The skeptic's evidence.** CONFIRMED - the post exists, is unweighed, and the sweep flagged it:
- Konf/own/x_KONFEDERACJA__2023-03-27_1640255696192122882.json: user.screen_name "KONFEDERACJA_", created_at "2023-03-27T07:33:27.000Z", isEdited false. Text: "Współprzewodniczący @KONFEDERACJA_ @krzysztofbosak w @PolsatNewsPL: Interesują nas ministerstwa, gdzie będziemy mogli realizować nasz program ...". [KONF-P5] is 07:58:39Z, 25 min 12 s later. Konf/own/SOURCES.tsv labels it "(counter-signal: open to government posts; not-a-line)".
- Staged record lines 81-83, "Not lifted between them", weigh only [KONF-I1] and [KONF-I3]. A grep of the staged record, the staged fetch_log.md and the staged DeclaredRedLines.cs (TD/NL/MN bases) for 1640255696192122882, "ministerstw" or "Interesuj" finds nothing.
- The record's own method needs a reading of this post. It treats programme-conditioned openness as a possible lift that only F2 discounts: the 26 June words (lines 83-87), and Interia's 30 March "Po wyborach zobaczymy, kto będzie chciał zrealizować nasz program" (set aside only as "a portal's report, which F2 does not count"). It also weighs "współrządzenie albo opozycja" (line 142). A post on the party's own record cannot be set aside on F2 grounds.
- The consequence chain holds. SOURCES.tsv has no own-record "z nikim" between 27 March and 20 June. The party's own post of 2023-06-20T21:48:49Z names PiS and PO only ("żeby usiąść do stolika z PiS-em, czy PO"). The Nowa Nadzieja post of 2023-05-29 is on a member party's account, which lines 140-141 hold "not the party's own". So if the post is read as an opening, facts 5-7 restart 2023-06-20 on [KONF-I4], dated by F2 and carrying PolandSpokenWords. If [KONF-I4] is not read as verbatim, they restart 2023-08-02 on [KONF-P1]. Either way the header (lines 26-32), the diagnostic's f2Pages.SetEquals(f2Firsts) "allows none" check, and lines 426-427 ("changes nothing") would fail.

WHY MINOR, NOT DEFECT:
- Under the record's most likely reading nothing it says is false. The post names no partner and no coalition. The same appearance on the same account says at 07:42:24Z "wypchnąć stamtąd ludzi z PiS, PO, Lewicy i PSL" (x_KONFEDERACJA__2023-03-27_1640257951884615680.json), and at 07:58:39Z refuses both Morawiecki and Tusk.
- Polling day is the same under both readings: facts 5-7 are open either way (line 296).
- No game surface shows Poland's run-up lines. Poland opens 2023-02-19 (8 + 26 weeks before 15 Oct), but GameController.CampaignDeclared.cs says "LiveCampaignSetup stages none for Poland", LiveCampaignSetup.cs:148 stages only Sweden and Germany, and Poland has no mid-term round (Unsourced confidence rules).
- The flaw is one unstated READING. It sits where the record says every reading is Elias's to confirm, so he never gets to see this one.

**The skeptic's corrected fix.** 1. In §2's "Not lifted between them" (record lines 81-83), weigh the post as a stated READING. Suggested wording: "the party's own post of 2023-03-27, 07:33Z, Bosak on Polsat News, 'Interesują nas ministerstwa, gdzie będziemy mogli realizować nasz program …' (out of tree, `x_KONFEDERACJA__2023-03-27_1640255696192122882.json`) - READING, stated: an interest in ministries names no partner and no coalition, so it replaces nothing (§621); on the same appearance the party's account carries, 9 minutes later, 'wypchnąć stamtąd ludzi z PiS, PO, Lewicy i PSL' (`x_KONFEDERACJA__2023-03-27_1640257951884615680.json`) and, 25 minutes later, the refusal of Morawiecki and Tusk [KONF-P5]."
2. Add a §12 "Left to Elias" bullet with the other reading's consequence. Read as an opening, facts 5-7 end 2023-03-27. They restart 2023-06-20 on [KONF-I4], dated by F2: PolandSpokenWords goes back on them, the (F2) mark goes back on [KONF-I4], and the verbatim doubt matters again. If [KONF-I4] is not verbatim, they restart 2023-08-02 on [KONF-P1]. No polling-day line changes.
3. Optional: make the header's "no fact here is dated by F2" say it rests on that reading ("as §2 reads 27 March"). Mirror the one clause in the TD/NL/MN fact bases in DeclaredRedLines.cs, which already list the 30 March and 26 June set-asides.

### 5. The forms-only alternative tests the verb rather than the party, and its PiS start rests on an unstated premise

- **Lens:** rulings - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:98

**The scenario.** Lines 98-99 and 418-419 say: "Read by F1's forms only (§4's READING ...), both lines start on Mentzen's own post of 2023-06-28 [KONF-P6]", that is, "Chcemy odsunąć od władzy Kaczyńskiego i Tuska". §4's READING (lines 200-201) separates words that name power or government from words to stop, defeat or shield against a party. [KONF-P5] names power, so §4 does not exclude it. What does exclude it is §780's party test for [KO-P5] ("PiS is not" named), and [KONF-P6] fails that test too: it names Kaczyński, and no premise keying Kaczyński to PiS is stated anywhere. The only stated PiS premise is about Morawiecki's rule, and fact 8 (DeclaredRedLines.cs:430) cites [KONF-P6] without one. Under the party test, the PiS line starts 2023-07-06 [KONF-P2] ("zakończyć rządy PiS") and fact 3 runs to 6 July, not to 28 June as §12 says. The alternative also does not weigh two own-account candidates inside its window. One is the party's post of 2023-06-25 quoting candidate Płaczek: "... żeby PiS i PO nigdy nie wróciło do władzy, jak ją stracą!". Its full text survives only in the third-party fxtwitter copy, and it is a proposal to the chairman, but §11 counts a non-leader's words "as the party published them". The other is Mentzen's 2023-04-11 retweet of Do Rzeczy's "Mentzen: Mówimy jasno: Morawiecki musi odejść".

**The fix proposed.** Either state a Kaczyński premise beside the Morawiecki and Tusk premises, or frame the alternative as the party test with its own dates (PiS from 2023-07-06 [KONF-P2], fact 3 to that day). Say why the 25 June and 11 April posts do not start it.

**The skeptic's evidence.** CONFIRMED (core): the forms-only alternative dates the PiS line from words that name Kaczyński, and the staged tree states no premise tying Kaczyński to PiS. The record's own method says such a premise has to be stated.
- Record line 97 states the PiS premise for [KONF-P5] only through the prime minister: "toward PiS (the premise, stated: Morawiecki's rule is PiS's; the model has no prime minister)". The KO premise is stated at 97-98 and 117 ("a cabinet holding KO is Tusk's, K-1f's"). Line 117 states it even though [KONF-I14] names PO in the same breath.
- Lines 98-99: "Read by F1's forms only (§4's READING ...), both lines start on Mentzen's own post of 2023-06-28 [KONF-P6]". Lines 418-419: "facts 8 and 9 start on ... [KONF-P6], \"Chcemy odsunąć od władzy Kaczyńskiego i Tuska\", and facts 3 and 4 run to that day". Line 408 (the F2-doubt bullet, which the finding does not cite) has the same gap: "Konfederacja → PiS and KO restart on Mentzen's own post of 2023-06-28 [KONF-P6]".
- DeclaredRedLines.cs:430 (fact 8) cites [KONF-P6] as "Restated in F1's own forms" toward PiS and gives no premise for Kaczyński. Fact 9 (line 432) does state the Tusk premise.
- A UTF-8 grep for "Kaczy" in the staged record, DeclaredRedLines.cs, PolishDeclarationsDiagnostic.cs and fetch_log.md, and a read of COMPLETED.md §780, find no Kaczyński-to-PiS premise.
- The record treats keying a party through its leader as open. Line 392 says Kaczyński's words of Tusk "would start it then if a pledge against a party's leader keys the party, as §2's premise keys Tusk to KO" (left to Elias). Line 57 says "aimed at the person ... not read as the start". Line 337 says Budka's "pogonimy Kaczyńskiego" is "aimed at PiS's chairman".
- K-1f's premise does not cover Kaczyński. In Sweden it reads "a cabinet holding S is hers" (the leader as prime-ministerial candidate), and §7 line 261 records that Kaczyński ruled himself out: "Nie chcę być premierem".
- The [KONF-P6] post names PiS only in a conditional forecast ("Jeżeli dowieziemy ten wynik ... koniec rządów PiS"), not in a pledge.
- Without the premise, the alternative starts the PiS line on [KONF-P2], 2023-07-06 ("chcemy zakończyć rządy PiS", which names PiS), and fact 3 runs to 07-06, not 06-28.

Impact: no game path. The alternative is not wired, the main reading keeps 2023-03-27 on [KONF-P5], no polling-day line moves, and Poland opens no formation round before polling day. But if Elias rules the alternative, the record's stated dates would be installed on a premise nobody stated. The finding is right to grade it minor.

PARTLY WRONG reasoning: "[KONF-P5] names power, so §4 does not exclude it ... What does exclude it is §780's party test" misreads the alternative. Naming power is necessary, not sufficient. The forms-only reading drops [KONF-P5] on its verb ("nie przyłożymy ręki" is none of the four forms), as the finding's own title concedes. [KONF-P5] would pass a party test, since its Morawiecki and Tusk premises are stated. §780's [KO-P5] exclusion also has two prongs, "PiS is not" named and "against its pillars", not a party test alone.

Second half:
- 2023-06-25 post (Konf/own/x_KONFEDERACJA__2023-06-25_1672921599907864576*.json), fair point. The party's own account quotes candidate Płaczek: "Ja proponuję, panie prezesie, ... żeby PiS i PO nigdy nie wróciło do władzy, jak ją stracą!" It falls inside the alternative's window, names the parties and power, and is close to the "block its return" form. §11 (lines 326-327) counts a non-leader's words "as the party published them". The syndication JSON truncates at 275 characters (display_text_range), so the full text exists only in the fxtwitter relay; there is no first-party _xcom capture, unlike [KONF-P6]. The record never weighs this post. §12's "Earlier own-record words, less plain, are listed in §2" covers only words before 03-21.
- 2023-04-11 retweet, weak. The JSON is @DoRzeczy_pl's post and its headline is the magazine's: "Mentzen: Mówimy jasno: Morawiecki musi odejść". SOURCES.tsv line 92: "Mentzen's own act is only the retweet". The record's existing rules already exclude it: §2 line 125 says F2 does not count Do Rzeczy, and "musi odejść" names no power or government (§4's READING).

**The skeptic's corrected fix.** Either state the premise wherever [KONF-P6] is read toward PiS, or re-date the alternative without it.

1. State the premise (if Elias agrees it holds). Add, as for Morawiecki: "the premise, stated: Kaczyński's power is PiS's (its chairman; the model has no prime minister)". It goes in four places: record §2 at lines 98-99 and at the "Restated in F1's own forms" paragraph, §12 at lines 408 and 418-419, and fact 8's basis string at DeclaredRedLines.cs:430. Put it to Elias, since §12 already leaves "a pledge against a party's leader keys the party" with him.

2. Without the premise, re-date the alternative. In §12 lines 418-419 and §2 lines 98-99, say that by F1's forms only the PiS line starts on [KONF-P2] 2023-07-06 ("chcemy zakończyć rządy PiS"), fact 3 runs to that day, and the KO line and fact 4 go to [KONF-P6] 2023-06-28. Make the same split at line 408 (PiS restarts on [KONF-P2], KO on [KONF-P6]). In fact 8, keep [KONF-P6] only as context toward PiS, or drop it.

3. Either way, add one clause on why the party's own post of 2023-06-25 quoting candidate Płaczek does not start the alternative line. It is a candidate's proposal to the chairman, its pledge is conditional ("jak ją stracą"), and its full text stands only on a third-party relay. Also widen §12's "Earlier own-record words, less plain, are listed in §2" so it covers the alternative's window as well.

The 2023-04-11 retweet needs no more than a clause: the words are Do Rzeczy's headline, and "musi odejść" names no power.

No code date changes under the installed reading.

### 6. Fact 8's basis puts coalition-only restatements under 'Restated in F1's own forms'

- **Lens:** rulings - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/DeclaredRedLines.cs:430

**The scenario.** Fact 8 reads: 'Restated in F1's own forms by Mentzen ... [KONF-P6] ... and [KONF-P2]; by Bosak on TVN24, 2023-07-13 [KONF-I14] ("Nie zamierzamy przedłużać władzy PiS-u") and ... [KONF-I15]; on the party's own page, 2023-08-02 [KONF-P1] ...; ... and on TVN24, 2023-10-11 [KONF-I25]'. The saved [KONF-P1] carries only "nie będzie z nikim koalicji" and "nie wejść w tą koalicję". The saved [KONF-I25] carries only "Nie zamierzam współtworzyć z rządu ani z Koalicją Obywatelską, ani z PiS-em". Both are coalition refusals. "Not prolong PiS's power" is the same kind of words as [KONF-P5]'s "lend no hand to the continuation", which the record's own alternative treats as outside F1's forms. The record itself sorts these differently in §2 (lines 99-125): [KONF-P6] and [KONF-P2] under 'in F1's own forms', TVN24 under 'Restated toward both', and [KONF-P1] and [KONF-I25] under 'The coalition half restated'. Fact 9 (line 432) has the same list after 'Restated in F1's own form'.

**The fix proposed.** Split the list as the record does: F1's own forms [KONF-P6], [KONF-P2], [KONF-I15], [KONF-I23]; restated on TVN24's page [KONF-I14]; the coalition half [KONF-P1], [KONF-I25]. Apply the same split in fact 9.

**The skeptic's evidence.** I tried to refute it and could not. The mislabel is real, and this staged diff introduced it.

1. The label comes from this change. HEAD has no "Restated in F1's own" anywhere in DeclaredRedLines.cs (grep finds nothing). In HEAD, facts 8 and 9 said only "Restated ...", and their line text still covered the coalition half: "will end PiS's rule and enter no coalition with it" and "will neither sit in a cabinet with KO nor let Tusk back to power". The staged lines 430 and 432 narrow the line to "will lend no hand to the continuation of PiS's rule" and "...to Tusk's return to power". They then open one list with "Restated in F1's own forms by ..." (430) and "Restated in F1's own form by ..." (432). Items are joined only by semicolons, so the label covers every item, including "on the party's own page, 2023-08-02 [KONF-P1] (Wipler's words on Onet Rano)" and "on TVN24, 2023-10-11 [KONF-I25]". Line 432 lists "2023-08-02 [KONF-P1], ... and 2023-10-11 [KONF-I25]" the same way.

2. What the saved pages say (tags stripped from the in-tree raw files):
- [KONF-P1] (Konf/konfederacja_koalicja-z-pis-czy-z-platforma-z-nikim.html): the 2023 article itself carries only the headline "Koalicja z PiS czy z Platformą? Z nikim!", "Jasno i klarownie mówimy: nie będzie z nikim koalicji" and "najlepsza droga ... to po prostu nie wejść w tą koalicję". It has no remove / end / block-return / not-let-govern words.
- [KONF-I25] (Konf/tvn24_mentzen-wywiad-wybory-2023.html): Mentzen's words are only "Nie zamierzam współtworzyć z rządu ani z Koalicją Obywatelską, ani z PiS-em", plus trades on bills from a "mniejszość blokującą". It has no F1-form words.

3. The record defines F1's forms narrowly. Its §4 READING (staged record, line 200) says "F1's forms name power or government (remove from power, end its rule, block its return, not let it govern)". Its §2 files [KONF-P1] and [KONF-I25] under "The coalition half restated" (lines 118-122). Doubt 5 (line 379) limits "restated in F1's own forms" to [KONF-P6], [KONF-P2] and [KONF-I14]'s KO half. The same file already marks this kind of item differently: line 436 has "Restated as a coalition refusal in the list's paid material ... [TD-P8]".

4. Fact 8's [KONF-I14] quote, "Nie zamierzamy przedłużać władzy PiS-u", is the weaker half of the finding. It is not one of §4's four forms, and it is the same kind of words as [KONF-P5]'s "kontynuacji władzy", which the record's alternative reading puts outside F1's forms (§2 line 98, §12). Neither §2 nor doubt 5 calls it F1's form, so listing it as one is unsupported but arguable.

5. Two parts of the finding go too far:
- In fact 9, [KONF-I14]'s KO half ("Nie zamierzamy umożliwić powrotu Tuskowi do władzy") and [KONF-I23] ("nie dopuścić do rządów Donalda Tuska") are F1 forms by doubt 5 and §4. Only [KONF-P1] and [KONF-I25] are wrong there.
- [KONF-I15] does carry an F1 form ("My chcemy PiS odsunąć od władzy" is on the saved page). It is the record's §2 that leaves it only under "The coalition half restated", not the code.

6. Why minor: no check reads this text. PolishDeclarationsDiagnostic reads only the tags, the first tag, the "DECLARED (F1" prefix and the F2 mark, so n784a's 4/4 clean says nothing about it. No date or flag changes under any stated reading: [KONF-P6] (2023-06-28) still starts the forms-only alternative, ahead of [KONF-P1] (2023-08-02). But the text can reach the player: FormationProposal.cs:171 builds "refuses: a red line falls inside this cabinet - " + Basis, which GameController.FormationSheet.cs:310 shows in the answer slip. So it is player-visible provenance that the record's own §2 and doubt 5 contradict.

**The skeptic's corrected fix.** Fact 8 (DeclaredRedLines.cs:430):
- Keep "Restated in F1's own forms" for [KONF-P6], [KONF-P2], [KONF-I15] ("My chcemy PiS odsunąć od władzy") and [KONF-I23] ("chcemy zakończyć rządy PiS-u").
- Move [KONF-I14] ("Nie zamierzamy przedłużać władzy PiS-u") to a neutral "restated on TVN24's own page, 2023-07-13", matching §2's "Restated toward both".
- Add a separate clause in the file's own wording (line 436): "Restated as a coalition refusal on the party's own page, 2023-08-02 [KONF-P1] (Wipler's words on Onet Rano), and on TVN24, 2023-10-11 [KONF-I25]".

Fact 9 (line 432):
- Keep [KONF-P6], [KONF-I14] (its KO half is an F1 form by doubt 5) and [KONF-I23] under "Restated in F1's own form".
- Move only [KONF-P1] and [KONF-I25] into a "Restated as a coalition refusal" clause.

Optionally, in the record's §2, also list [KONF-I15] among the F1-form restatements toward PiS, since its page carries "odsunąć od władzy". This changes text only: no dates, flags or tags change, and the diagnostics stay as they are.

### 7. The rewritten F2 check prints a count of pages under the label 'facts'

- **Lens:** rulings - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:130

**The scenario.** {1} is now f2Firsts.Count, a set of distinct first-tag pages. Before §784 it was the number of facts carrying the mark. If the mark comes back on facts 5-7, which are all first-tagged [KONF-I4] (for example under finding 4's other reading), the line prints '(1 such pages, 1 facts)' for three marked facts.

**The fix proposed.** Print facts.Count(f => f.Basis.Contains(DeclaredRedLines.PolandSpokenWords)) for {1} as before (the SetEquals test is unaffected), or relabel {1} as first-tag pages.

**The skeptic's evidence.** The code says what the finding says. Staged Assets/Editor/PolishDeclarationsDiagnostic.cs:127-131:
  var f2Firsts = new HashSet<string>(facts.Where(f => f.Basis.Contains(DeclaredRedLines.PolandSpokenWords)).Select(f => Regex.Match(f.Basis, @"\[([A-Z]+-[A-Z]+\d+)\]").Groups[1].Value));
  Check(f2Pages.SetEquals(f2Firsts) && ..., F("... ({0} such pages, {1} facts) ...", f2Pages.Count, f2Firsts.Count, ...));
f2Firsts is a set of distinct first-tag pages, so {1} counts pages, not facts. HEAD (cd1fb480) printed {1} = facts.Count(f => f.Basis.Contains(DeclaredRedLines.PolandSpokenWords)), which really was a count of facts.

Why the label is wrong, not just reworded:
- Whenever the check passes, SetEquals forces {0} == {1}. Every passing line therefore prints "N such pages, N facts", which suggests one fact per page. The check does not enforce that.

What the line would print on HEAD's data:
- At HEAD six facts carried the mark (DeclaredRedLines.cs HEAD lines 420-428 and 432): Konf -> PiS, KO, TD, NL and MN, all first-tagged [KONF-I4], plus Konf -> KO (F1), first-tagged [KONF-I14].
- HEAD's register marked those two pages (F2) (HEAD record lines 412 and 416).
- The misdated lambda is unchanged by the diff, so the staged check passes on that data and prints "ok ... (2 such pages, 2 facts)". HEAD's line said 6 facts.

Why it is latent today:
- Staged, no fact carries the mark. PolandSpokenWords appears only at its declaration (line 398) and in a doc comment (line 406).
- The record's only "(F2)" is prose at line 31, not a register row.
- So the n784a log (PoliSim-captures/logs/n784a.log:557) prints "ok ... (0 such pages, 0 facts)", which is true.
- Pass/fail is never affected.
- A repo-wide grep for "such pages" finds only the diagnostic itself, so nothing quotes or parses the line.

Can the scenario happen?
- The check deliberately accepts marks ("none at all is allowed", lines 124-126), so marks are a supported state.
- The header's "always wins ... in whatever words" READING is "Elias's to confirm (§12)". A reading under which [KONF-P4] is not the same declaration would date facts 5-7 by [KONF-I4] again. That gives three marked facts on one page, and the line would print "(1 such pages, 1 facts)".
- None of §12's own stated alternatives brings a mark back. The F2 header readings, the literal "always wins" and F1's forms only each re-date by an own-record P page ([KONF-P6], [KONF-P1]). So the path needs a ruling or a reading the record does not state.

One slip in the finding: in the staged data facts 5-7 are first-tagged [KONF-P4], with [KONF-I4] second. The scenario only works if they are re-tagged to [KONF-I4] first, as at HEAD. Otherwise misdated fails instead.

Grade: minor. It is a real but latent mislabel in a generated evidence line. Today's output is accurate and pass/fail is untouched.

**The skeptic's corrected fix.** Bring back the per-fact count for {1}: pass facts.Count(f => f.Basis.Contains(DeclaredRedLines.PolandSpokenWords)) and keep f2Pages.SetEquals(f2Firsts) in the condition. Relabelling {1} as "first-tag pages" would be accurate, but under SetEquals it would just repeat {0} on every passing run, so the restored fact count is the better fix. Optional: when SetEquals fails, add the pages from f2Pages.Except(f2Firsts) and f2Firsts.Except(f2Pages) to the NOT list. Today a stray register mark fails with counts only and names no page.

### 8. The new comments state the timeline's current content ('none since §784', 'every Polish fact'), which the claim convention keeps out of comments

- **Lens:** rulings - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/DeclaredRedLines.cs:396

**The scenario.** DeclaredRedLines.cs:396-397 says "- none since §784 ... every Polish fact is dated by the party's own record". PolishDeclarationsDiagnostic.cs:125-126 says "since §784 every Polish fact is dated by its declarer's own record". Both are DERIVED claims about PolandTimeline: a count of zero, and a property of every row. As soon as a fact carries PolandSpokenWords again (finding 4's other reading would do it), both comments are false while every check passes, because the diagnostic now accepts any matched set. Line 125's "and none at all is allowed" also reads as 'no mark is allowed', the opposite of what the code permits. Run n784a ran DocumentClaimCheck but not CommentClaimCheck, which §780's runs added when comments changed. These comments carry no backticked names, so CommentClaimCheck would pass them anyway.

**The fix proposed.** Word the comments as the rule: pages a Polish fact is dated by under F2 carry the register's (F2) mark, and PolishDeclarationsDiagnostic holds marks and facts to each other, with an empty set passing. Drop 'none since §784' and 'every Polish fact'.

**The skeptic's evidence.** The new comments state what the data holds today, not the rule. Both are true of the staged tree now, but no check would catch them going stale.

1. The comments.
- Staged DeclaredRedLines.cs:396-397: "The pages a Polish fact is dated by under F2 are marked (F2) in the record's register - none since §784, when Konfederacja's own record, swept, carried its lines earlier: every Polish fact is dated by the party's own record (§621)."
- At HEAD the same summary was in rule form: "...are marked (F2) in the record's register. Every other Polish fact is dated by the party's own record (§621)."
- Staged PolishDeclarationsDiagnostic.cs:124-126: "...so a mark left on a page that dates nothing fails, and none at all is allowed (since §784 every Polish fact is dated by its declarer's own record: Konfederacja's swept, as KO's was)".

2. True today, which is why this is minor and not a defect.
- In the staged DeclaredRedLines.cs, `PolandSpokenWords` appears only at its declaration (line 398) and in the doc text at line 406. No fact carries it.
- The only "(F2)" in the staged record is header line 31's conditional "would be first-tagged by a page the register marks (F2)". No register row is marked.

3. The rule these comments break.
- CLAUDE.md:37-41: "the code can change freely and no document becomes wrong". A count (here a count of zero) is a DERIVED claim, and the convention covers every source comment.
- COMPLETED.md §190 §A.5: "A live document may not say ... a count of code things ... This binds code comments too."
- The case that led to binding comments was CheckSuite.cs's "TWENTY-ONE since the NINTH sweep" going stale. "none since §784" is the same count-since-a-section shape.

4. The path to staleness is real.
- The new check passes on any matched set: `f2Pages.SetEquals(f2Firsts) && f2Pages.All(t => Regex.IsMatch(t, @"^[A-Z]+-I\d+$")) && misdated.Count == 0`. Table check (a) passes too once the §8 row carries "- F2".
- Nothing reads the comment text. A grep for "none since", "there is none" and "no fact here is dated" in Assets/ and Tools/ finds no check.
- The record itself lists two pending items that would put a mark back on a fact:
  - Its "Left to Elias" list: PiS -> KO would start on [PIS-I1], TVN24, "only if the page counts under F2".
  - §2: "Read the other way, the opening is dated 2023-06-26 by [KONF-I8] or [KONF-I11]".
- If either lands, both comments become false while every check passes.

5. Side points.
- "none at all is allowed" (diagnostic line 125) is ambiguous. It is meant as "an empty set passes", since HEAD's `f2Pages.Count > 0` was dropped, but it reads naturally as a prohibition.
- The point that n784a skipped CommentClaimCheck is true (§780's runs added it) but changes nothing:
  - CommentClaimCheck.cs:43 matches only backticked `Type.Member`, and these comments have none.
  - CheckSuite.cs:242 runs CommentClaimCheck in the cheap bar, which this commit owes anyway: an Assets/Scripts/Elections path puts it in the sim tier per bar_tier.ps1:56.
- The staged record header (lines 27-32: "Since `COMPLETED.md` §784 no fact here is dated by F2 ... there is none") has the same shape, describing the record's own §8.

**The skeptic's corrected fix.** Put both comments back in rule form, and say the empty-set behaviour plainly.

1. DeclaredRedLines.cs:396-397: "...The pages a Polish fact is dated by under F2 are marked (F2) in the record's register; every other Polish fact is dated by the party's own record (§621). Which facts carry this mark, if any, is whatever PolandTimeline holds (the record's §8)." Drop "none since §784 ...: every Polish fact is dated by the party's own record". The §784 history already lives in COMPLETED.md.

2. PolishDeclarationsDiagnostic.cs:124-126: "§784: the marks and the facts held to each other both ways - a page the register marks (F2) is exactly one that first-tags a fact carrying F2's mark - so a mark left on a page that dates nothing fails, and a timeline with no F2-marked fact and no marked page passes." Delete the parenthetical "(since §784 every Polish fact is dated by its declarer's own record ...)", or recast it as past-tense history, for example "§784's sweep re-dated the last F2 facts by Konfederacja's own record".

3. Optional, same shape: the record header's "there is none" and "Every fact is dated by its declarer's own record" can say what §784 did, and leave the current count to §8 and the diagnostic.

No code or behaviour change is needed.

### 9. fetch_log says playback was refused 'for the whole sweep', which the sweep's own Wayback copy contradicts; archive.ph has no source row

- **Lens:** rulings - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/raw/declarations_2023/fetch_log.md:60

**The scenario.** The out-of-tree folder holds konfederacja.pl_2022-09-20_blog_wystartuje-samodzielnie_wayback20221003204620.html, a real Wayback playback of the 2022 site. Its SOURCES.tsv row says 'Wayback id_ fetch 2026-10-05' (file saved 16:41), while the CDX row recording the 429 dates from 17:50. So playback was not refused for the whole sweep, only (at least) for the post pages. Line 58's "from archive.ph's listings" has no SOURCES.tsv row naming archive.ph (0 matches).

**The fix proposed.** Say that playback of the post pages was refused (HTTP 429), and keep "no archived copy of a post page is held". Drop archive.ph, or record what it yielded.

**The skeptic's evidence.** The playback half of the finding holds; the archive.ph half does not.

1. **The claim (staged fetch_log.md, line 60).** "The Internet Archive's playback refused (HTTP 429) for the whole sweep, so no archived copy of a post page is held".

2. **The sweep's own folder contradicts it.**
   - Konf/own/SOURCES.tsv row 35 lists `konfederacja.pl_2022-09-20_blog_wystartuje-samodzielnie_wayback20221003204620.html`, fetched from `https://web.archive.org/web/20221003204620id_/https://konfederacja.pl/blog/2022/09/20/...`, with the note "Wayback id_ fetch 2026-10-05; contemporaneous copy of the 2022-09-20 post".
   - The file's sha256 is 9dd2cfb0…, which matches the row. Its modification time is 16:41:57, inside the sweep's run (its files span 16:26–18:28).
   - It is a real archived page, not an error body: 457 KB, the 2022 site's Brooklyn theme, canonical URL on `/blog/2022/09/20/`. The live copy uses the carrino theme and has no `/blog/` path.
   - So the Internet Archive's playback served this page during the sweep, and it is held in the very folder the fetch_log points to.

3. **The sweep's transcripts say the same** (workflow wf_2f22824c-b05).
   - The site agent (a767d21cb3457d185) reported: "Wayback playback (id_) returned HTTP 429 for most of about 2 hours. Fetched: the 2022-10-03 copy of the 2022-09-20 post (saved). Homepage 2023-06-02 … /prawybory/ …, /dla-mediow/, and the 2023-06-23 landing pages …". Its retry log shows "try 1: 429 620 / try 2: 200 47893".
   - The party and Bosak X agent (a317d946026352e0b) reported: "web.archive.org replay: 429 … from about 16:40 to 18:10 … only 3 pages got through". Those three were archived nitter timeline pages, and one of them gave 16 new Bosak posts.
   - The words "for the whole sweep" come from the Mentzen agent (aff44d444d1852432), describing only its own part of the sweep. The fetch_log generalised them to the whole sweep.

4. **The second clause is true.** "No archived copy of a post page is held" holds for X post pages.
   - Every playback attempt on a status capture that I found was refused: the 2023-03-26 capture, the 2022-05-16 capture, the KONF-P5 corroboration capture 20230414110041, and the Mentzen retweet (SOURCES.tsv row 93).
   - own/ holds no archived status page.

5. **The archive.ph half is refuted.** The clause "from archive.ph's listings" on line 58 is true.
   - The Mentzen agent paged "archive.ph prefix listings for twitter.com/x.com/SlawomirMentzen*: 8 pages, 3 new in-window ids".
   - Its scratchpad file `konf_mentzen_x/q_archiveph_all.txt` holds 112 ids. `q_archiveph_new.txt` holds the three new ones (1646088253387202565, 1667181127503601669, 1668929012847067138). `fetchlog_archiveph.tsv` shows each was read through the syndication endpoint with HTTP 200. None was a candidate, so none was saved.
   - The party and Bosak agent paged archive.ph for krzysztofbosak (100 of 150), KONFEDERACJA_ (62/62 and 77/77) and Bosak on x.com (67/67).
   - SOURCES.tsv lists saved files only. Its 0 archive.ph matches mean no saved file depended on an archive.ph id, not that archive.ph went unused. Dropping archive.ph would make line 58 wrong.

6. **Severity: minor.** The staged record's §12 bullet (around line 415) points to fetch_log.md for "what was read and what refused", so line 60 is the record's authority on the sweep's limits. Still, no fact, date, code path, DeclaredRedLines entry or n784a diagnostic depends on the sentence. CLAUDE.md has no fetch_log-specific rule.

**The skeptic's corrected fix.** Change only the playback sentence on fetch_log.md line 60, from

"The Internet Archive's playback refused (HTTP 429) for the whole sweep, so no archived copy of a post page is held"

to something like

"The Internet Archive's playback refused (HTTP 429) most requests and every post page tried, so no archived copy of a post page is held; among the few pages it served is the 2022 site's copy of the 2022-09-20 post, held in the folder (its SOURCES.tsv row)".

Keep "from archive.ph's listings" on line 58. It is true: the listings gave post ids, which were read, but none was a candidate, so no SOURCES.tsv row names archive.ph. Dropping it would make the fetch_log wrong.

Optionally, the konfederacja.pl clause ("every graphic page fetched live") could add that one page of the 2022 site was read through the Internet Archive's copy.

Nothing changes in the record, DeclaredRedLines.cs, SHA256SUMS.txt or the diagnostics.

### 10. 'A clip of his words on Onet' rests on bytes that were not saved

- **Lens:** rulings - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:76

**The scenario.** The in-tree [KONF-P4] JSON names no outlet: neither its text nor its media mention Onet. The attribution comes from the sweep's note ("Onet Opinie clip"; the post id came from a nczas.com embed), and neither the nczas page nor the video is saved. The record's header says the sources it relies on are stored byte for byte.

**The fix proposed.** Drop the outlet's name, or save the page that shows it.

**The skeptic's evidence.** I could not refute the finding. Staged record lines 75-77 (added by this diff; at HEAD the record's only Onet mention was the Wipler / Onet Rano line): '("Współprzewodniczący @KONFEDERACJA_ @SlawomirMentzen:"; a clip of his words on Onet), created_at 2023-03-21T15:15:46Z, from X's syndication endpoint, isEdited false [KONF-P4]'.

1. **The cited bytes name no outlet.** The in-tree file Konf/x_konfederacja_1638197714667053056_syndication.json (sha256 d970d583..., the same bytes as the sweep's x_KONFEDERACJA__2023-03-21_1638197714667053056.json) has this text: "Współprzewodniczący @KONFEDERACJA_ @SlawomirMentzen: Z nikim nie chcemy wchodzić w koalicję, ... https://t.co/E10Foztzv7". Its user_mentions are only KONFEDERACJA_ and SlawomirMentzen. Its media is a video hosted on twimg. A case-insensitive search for "onet" in the file finds nothing.

2. **The only source of "Onet" is the sweep's own note.** SOURCES.tsv row 46 says: "id from nczas.com 2023-03-21 article embed (found while sweeping @SlawomirMentzen); party account quoting co-chair Mentzen (Onet Opinie clip)". Row 45 says the same for the 14:51 post. The pages and files behind that note were never saved:
   - No row in SOURCES.tsv has an nczas or Onet URL.
   - No file under PoliSim-captures/sources is named nczas*.
   - There is no mp4, m3u8 or poster file for media 1638197631674359809.
   - The 21 March companion post (...609131982848) does not mention Onet either.
   - In the in-tree Konf/ raw files, "onet" matches only the Wipler/Onet Rano page [KONF-P1] and OneTrust consent scripts.

3. **The record backs similar glosses with saved bytes everywhere else:**
   - [KONF-P5]'s "on Polsat News's Graffiti" rests on its JSON's "w @Graffiti_PN".
   - [KONF-P1]'s "on Onet Rano" rests on the page's "Przemysław Wipler w Onet Rano został zapytany".

4. **The header is not literally broken, but the placement misleads.** Header lines 4-7 promise that "every source this record's register lists" is stored byte for byte. The nczas page is not a register source, so that promise is not false. But the Onet gloss sits inside the [KONF-P4] citation, so a reader takes [KONF-P4] to carry it, and it does not.

5. **Why this is only a note:** nothing depends on it.
   - The fact's date and words both come from the saved JSON (created_at, text).
   - The party's own post dates the line whatever outlet the clip came from (F2's "a date from the party's own record always wins").
   - Register row 491 does not repeat Onet.
   - No fact, date, shape, diagnostic or polling-day count changes.

The gloss may well be true, but nothing saved backs it.

**The skeptic's corrected fix.** Remove "on Onet" from line 76, for example: ("Współprzewodniczący @KONFEDERACJA_ @SlawomirMentzen:"; with a video attached). Strictly, the saved JSON shows only that a video is attached; the video itself was not saved. The other way is to save the nczas.com 2023-03-21 page that the sweep found the post on, out of tree, list it in SOURCES.tsv and name it as where the outlet comes from. Since a portal page dates nothing under F2, dropping the outlet's name is the simpler fix.

### 11. Facts 3 and 4 cite a 20 June restatement although they end on 27 March

- **Lens:** rulings - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Elections/DeclaredRedLines.cs:420

**The scenario.** Facts 3-4 (until 2023-03-27) and §8 rows 3-4 cite "Restated on RMF FM's own record of its debate, 2023-06-20 [KONF-I4]". By 20 June the coalition refusal toward PiS and KO is carried by facts 8-9, whose coalition half those words restate, not by the cabinet facts that were replaced on 27 March.

**The fix proposed.** Move the 20 June restatement to facts 8-9 (coalition half), or word it as the declaration restated after the fact was replaced.

**The skeptic's evidence.** I could not refute the finding. It is real, but it is only about where the record places a citation: it changes no date, no flag and no check.

Staged DeclaredRedLines.cs, facts 3-4:
- Line 419: `new DatedFact("Konf", "PiS", ..., false, false, null, D(2023, 3, 21), D(2023, 3, 27),`
- Line 420, the basis: "...2023-03-21 [KONF-P4] ... Restated on RMF FM's own record of its debate, 2023-06-20 [KONF-I4] (\"Nie wejdę w koalicję z PiS-em. ...\"). Replaced on 2023-03-27 by the one-way support-blocking line (F1)."
- Lines 421-422 do the same for KO.
- So each basis cites a restatement made 85 days after the fact it sits on was replaced.
- §8 rows 3-4 list "[KONF-P4]; [KONF-I4]" with until 2023-03-27.

On 2023-06-20, `StandsOn` (`date >= From && date < Until`) returns facts 8-9 for Konf→PiS and Konf→KO (lines 429/431, `D(2023, 3, 27), Open`), not facts 3-4. Facts 8-9 never cite [KONF-I4], and neither do §8 rows 8-9.

The record handles the same kind of words the other way everywhere else:
- **§2 (unchanged lines):** "**The coalition half restated** - toward PiS: 2023-07-16 [KONF-I15], 2023-08-02 [KONF-P1], ... 2023-10-11 [KONF-I25]; toward KO, on the pages saved: only 2023-08-02 [KONF-P1] and 2023-10-11 [KONF-I25]". Here, coalition-only words said while the F1 line stands count as that line's coalition half.
- **Facts 8-9 in the code:** they cite [KONF-P1] ("Koalicja z PiS czy z Platformą? Z nikim!") and [KONF-I25] as restatements. Facts 3-4 cite neither.
- **The odd one out:** [KONF-I4] carries the same "z nikim"/no-coalition-with-PiS words, yet it is the only such page attached to the replaced cabinet facts.
- **The "only" list toward KO:** it was correct when fact 9 started 2023-07-13. Fact 9 now starts 27 March, and the list does not include 20 June's "W przyszłej kadencji nie wejdę w koalicję z nikim", which answered Petru's question about PiS or PO.
- **The precedent:** NL→PiS cabinet fact (lines 437-438), also replaced by an F1 line, cites only restatements dated inside its own interval (2021-11-15 [NL-P4]).

What the finding gets wrong or leaves out (refutation attempts):
- The sentence is literally true of the declaration itself: the "no coalition with anyone" words were said again on 20 June.
- §2's group block, which says "the station's page restates it", is fine, because Konf→TD, NL and MN are still open on 20 June.
- Under §12's other reading (F1's forms only), facts 3-4 would run to 2023-06-28 and the citation would fall inside them. But the code installs the 27 March reading, and the basis itself says "Replaced on 2023-03-27".

Why there is no runtime effect:
- `Basis` is text only. Formation reads `BlocksSupport`/`OneWay`, and `StandsOn` reads `From`/`Until`.
- The Polish timeline appears on no page (GameController.CampaignDeclared.cs: "LiveCampaignSetup stages none for Poland").
- `PolishDeclarationsDiagnostic` checks only the first tag, that every tag is in the register, and the F2 marks, so run n784a passes either way.

The finding's own grade, note, is right.

**The skeptic's corrected fix.** Treat [KONF-I4] as the F1 lines' coalition half, as §2 already does for [KONF-P1] and [KONF-I25].

1. **DeclaredRedLines.cs lines 420/422:** remove the sentence "Restated on RMF FM's own record of its debate, 2023-06-20 [KONF-I4] (...)". If it stays, reword it as after the fact, e.g. "The same words, after the line was replaced, restate the F1 line's coalition half (RMF FM's own record of its debate, 2023-06-20 [KONF-I4])".
2. **Lines 430/432:** add "the coalition half restated on RMF FM's own record of its debate, 2023-06-20 [KONF-I4] (\"Nie wejdę w koalicję z PiS-em. W przyszłej kadencji nie wejdę w koalicję z nikim.\")" after the [KONF-P5] opening. [KONF-P5] must stay the first tag, because the diagnostic's first-tag check reads it.
3. **Record §8:** change rows 3-4 to "[KONF-P4]", and add "[KONF-I4]" to the end of rows 8-9.
4. **Record §2:** add 2023-06-20 [KONF-I4] to "The coalition half restated" lists toward PiS and KO. Either drop the KO list's "only" or include [KONF-I4] in it.

The diagnostic still passes: the first tags are unchanged, [KONF-I4] is a register row, and it carries no (F2) mark.

### 12. The F2 check's printed fact count is now a count of pages

- **Lens:** code - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/PolishDeclarationsDiagnostic.cs:131

**The scenario.** The format string still says "({0} such pages, {1} facts)", but {1} is now `f2Firsts.Count`. That is the number of distinct first-tag pages among facts carrying F2's mark, not the number of facts. Because `SetEquals` makes {1} equal {0} whenever the check passes, the second number carries no information on a pass. Example: if the June lines were dated by [KONF-I4] again, as at HEAD (five facts first-tagged by that one page, plus fact 9 on [KONF-I14]), the line would print "(2 such pages, 2 facts)" while six facts carry `PolandSpokenWords`. HEAD printed "2 such pages, 6 facts". Today it prints "0 such pages, 0 facts", which is correct only by coincidence.

**The fix proposed.** Pass `facts.Count(f => f.Basis.Contains(DeclaredRedLines.PolandSpokenWords))` for {1}, as HEAD did. If the first-tag set is wanted as well, print it separately and label it as pages.

**The skeptic's evidence.** Staged Assets/Editor/PolishDeclarationsDiagnostic.cs L127-128: `var f2Firsts = new HashSet<string>(facts.Where(f => f.Basis.Contains(DeclaredRedLines.PolandSpokenWords)).Select(f => Regex.Match(f.Basis, @"\[([A-Z]+-[A-Z]+\d+)\]").Groups[1].Value));` builds a set of distinct first-tag PAGES. L129 `Check(f2Pages.SetEquals(f2Firsts) && ...`: set equality of two default-comparer HashSets implies equal counts, so on any pass {1} == {0}. L130-131: `"... ({0} such pages, {1} facts) ..."` with args `f2Pages.Count, f2Firsts.Count`, so a page count is printed under the label 'facts'. HEAD passed `facts.Count(f => f.Basis.Contains(DeclaredRedLines.PolandSpokenWords))`.

The example checks out against HEAD's data. In HEAD DeclaredRedLines.cs, L420/422/424/426/428 (facts 3-7, first tag [KONF-I4]) and L432 (fact 9, first tag [KONF-I14]) concatenate PolandSpokenWords, which makes 6 facts. HEAD's record register rows L412 and L416 carry '(F2)' in the third cell, which is what the L100-103 regex reads, so f2Pages = {KONF-I4, KONF-I14}. HEAD therefore printed '2 such pages, 6 facts'. Run on that same data, the staged code would PASS and print '2 such pages, 2 facts'.

Today's output is accurate only because the set is empty: PoliSim-captures/logs/n784a.log L557 reads 'ok  F2: ... (0 such pages, 0 facts)'. The staged tree has no fact with the mark and no register row with (F2); the only staged '(F2)' match is prose at record L31.

The scenario is not far-fetched. The staged record's header (L26) says the 'always wins' and own-page READINGs are 'Elias's to confirm (§12)'. §12 (staged L392) says Kaczynski's TVN24 page [PIS-I1] would start PiS -> KO 'only if the page counts under F2'. Ruling the other way on 'always wins' would put the June lines back on [KONF-I4], which is exactly HEAD's state. The check is built to accept F2 facts: L121's F2 branch remains, and the comment at L124-126 says none is 'allowed', not required.

The pass/fail verdict is never affected, and that is the reason the grade stays minor rather than defect. One related gap: when SetEquals fails because a page carries (F2) but first-tags no F2 fact, no NOT list names that page (`misdated` at L116-123 covers facts only), so the mislabelled count is the only clue.

**The skeptic's corrected fix.** At L130-131, print the fact count in {1} as HEAD did: replace `f2Firsts.Count` with `facts.Count(f => f.Basis.Contains(DeclaredRedLines.PolandSpokenWords))`. This changes nothing today: it still prints '(0 such pages, 0 facts)', and pass/fail is unchanged. Optionally, make the new direction self-diagnosing by naming the set differences on failure, e.g. `var stray = f2Pages.Except(f2Firsts).ToList(); var unmarked = f2Firsts.Except(f2Pages).ToList();` and append `stray.Count > 0 ? "; marked (F2) but first-tagging no F2 fact: " + string.Join(", ", stray) : string.Empty` (and the same for `unmarked`) to the message. Today the misdated list names facts on unmarked pages, but nothing names a stray mark. Because this edits Assets/Editor, it needs at least the compile-checked named run (e.g. PolishDeclarationsDiagnostic) per the repo's bar_tier rule.

### 13. New comments state the register's current F2 count; the check would not catch them going stale, and "none at all is allowed" reads as a ban the code does not enforce

- **Lens:** code - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/DeclaredRedLines.cs:396

**The scenario.** DeclaredRedLines.cs:396-397 says "marked (F2) in the record's register - none since §784 ... every Polish fact is dated by the party's own record". PolishDeclarationsDiagnostic.cs:124-126 says "none at all is allowed (since §784 every Polish fact is dated by its declarer's own record ...)". Both are DERIVED claims about the current state of the array and the register: a count of zero, and an "every". CLAUDE.md's claim convention gives such a claim three destinations: generated, referenced or deleted. The new check accepts a consistently marked F2 fact (`PolandSpokenWords` on the fact, (F2) on its first page in the register, "- F2" on the §8 row). So if a later sweep dates a Polish fact by a broadcaster's page, the bar stays green and both comments become false. Separately, "none at all is allowed" can be read as "no F2 mark is permitted", which the code does not enforce; the intended meaning is "an empty set passes". CommentClaimCheck only resolves backticked references, so it sees neither comment.

**The fix proposed.** Point to the source instead of stating the count, for example: "which pages, if any, is what the register marks; PolishDeclarationsDiagnostic prints how many". Reword the diagnostic comment to "an empty set passes (the old Count > 0 is dropped)". Keep the §784 history in COMPLETED.md.

**The skeptic's evidence.** I could not refute it. The failing path exists, and nothing in the bar sees these comments.

1. The new claims (staged):
- DeclaredRedLines.cs:396-397: "...marked (F2) in the record's register - none since §784, when Konfederacja's own record, swept, carried its lines earlier: every Polish fact is dated by the party's own record (§621)."
- PolishDeclarationsDiagnostic.cs:125-126: "...and none at all is allowed (since §784 every Polish fact is dated by its declarer's own record: Konfederacja's swept, as KO's was)".
- "None since" is a "there are 0 of X" claim, and "every Polish fact" is a universal statement about the array. Both are DERIVED under CLAUDE.md:36-41.
- docs/archive/CLAIM_CONVENTION_AND_DISCIPLINE.md:40-44 says the convention "binds SOURCE COMMENTS exactly as it binds markdown". Its worked example is a stale count in a comment (CheckSuite's "TWENTY-ONE"), and its fix is "a reference or a generated block".

2. The check accepts a consistently marked F2 fact. Suppose a later fact carries PolandSpokenWords, its first tag is an I-page marked "(F2)" in the register, and its §8 row reads "- F2":
- line 78: `rows[i].Basis.Contains("- F2") == f.Basis.Contains(PolandSpokenWords)` holds;
- line 103: the page enters f2Pages;
- line 121: `page = f2Pages.Contains(tag)` is true;
- line 129: `f2Pages.SetEquals(f2Firsts)` and the I-tag test hold.
The bar stays green, and both comments become false. Ruling F2 is still in force, and the code keeps PolandSpokenWords and this check alive precisely so such a fact can be added.

3. No check reads these comments:
- CommentClaimCheck.cs:43 matches only a backticked `Type.Member`;
- PhantomGuardCheck matches only names ending in Check, Harness or Diagnostic;
- DocumentClaimCheck reads markdown, not source;
- the long form (lines 92-95) forbids writing a new check for this: "the convention works by nobody writing the claim".

4. The diff swapped a sentence that was still true for one that will go stale. HEAD read: "The pages a Polish fact is dated by under F2 are marked (F2) in the record's register. Every other Polish fact is dated by the party's own record (§621)." That sentence points at where the fact lives and stays true with zero marks. The new text writes today's state into the comment instead.

5. What softens it: both claims are true today. I listed the staged PolandTimeline: all 15 facts are first-tagged by the declarer's own -P page (PIS-P1, PIS-P2, KONF-P4 x5, KONF-P5 x2, TD-P11, TD-P6, NL-P3, NL-P31, NL-P17, KO-P1). No fact carries PolandSpokenWords, no register cell has "(F2)", and no §8 row has "- F2". It has no effect at runtime or on the checks.

6. On "none at all is allowed": it can be read as a ban, which the code does not enforce. The clause before it ("exactly one that first-tags a fact carrying F2's mark") makes the intended meaning recoverable, so this half is a wording nit.

7. Aside, in the same diff: line 130-131 prints "{1} facts" from `f2Firsts.Count`. That is the number of distinct first-tag pages, not facts. Whenever SetEquals passes it equals {0}, so it never counts facts. The old code printed `facts.Count(f => f.Basis.Contains(PolandSpokenWords))`. With the pre-§784 data it would have printed "2 such pages, 2 facts" when 6 facts carried the mark.

**The skeptic's corrected fix.** Point to where the fact lives instead of writing it down, and add no new check (the convention forbids one).

1. DeclaredRedLines.cs:396-397: go back to the HEAD sentence, which stays true whether there are zero marks or many: "The pages a Polish fact is dated by under F2 are those the record's register marks (F2); every other Polish fact is dated by the party's own record (§621)." Drop "none since §784 ... every Polish fact is dated by the party's own record". The §784 story belongs in COMPLETED.md §784.

2. PolishDeclarationsDiagnostic.cs:124-126: keep the rule and drop the state claim. For example: "the marks and the facts held to each other both ways - a page the register marks (F2) is exactly one that first-tags a fact carrying F2's mark - so a mark left on a page that dates nothing fails; an empty set passes (no marked page is required since §784); the line below prints how many".

3. In the same check, change {1} back to `facts.Count(f => f.Basis.Contains(DeclaredRedLines.PolandSpokenWords))`. Then "{1} facts" counts facts, and step 2's pointer to the printed line is accurate.

### 14. The 26 June opening toward PiS/PO is no longer noted on the lines it would end (facts 8-9); [KONF-I8] is now cited by no fact

- **Lens:** code - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/DeclaredRedLines.cs:429

**The scenario.** At HEAD, facts 3 and 4 were the lines standing toward PiS and KO on 26 June. They carried the note that the opening "…to wszystko jest na stole" for PiS or PO [KONF-I8] (with [KONF-I10] and [KONF-I11]) lifts nothing under F2 as the record reads it. After the change, the lines standing toward PiS and KO through 26 June are facts 8 and 9 (from 27 March, open). Their bases (lines 430 and 432) never mention the opening, yet the record's §12 says that, read the other way, it ends facts 5-9 on 2023-06-26. Facts 3 and 4 dropped the note, which is right since they end on 27 March. Grep of the staged file: only facts 5 and 6 still carry a 26 June sentence, and [KONF-I8] appears in no basis. Facts 3 and 4 (lines 420 and 422) instead cite RMF's 2023-06-20 page [KONF-I4] as a restatement, although both facts close on 2023-03-27. A reader of facts 8 and 9 sees the F1 lines toward PiS and KO open from 27 MAR 2023 with no sign of the words that would end them. The same holds for the DECLARED page's source keys, if Poland is ever given a run-up. No check reads the basis prose, so nothing flags it.

**The fix proposed.** Move HEAD's 26 June sentence ("…to wszystko jest na stole" [KONF-I8], [KONF-I10], [KONF-I11]: it lifts nothing under F2 as read, §12 doubt 4) onto facts 8 and 9. Move the RMF 2023-06-20 restatement [KONF-I4] of the coalition half from facts 3 and 4 to facts 8 and 9. Facts 3 and 4 then keep only their own 21 March basis.

**The skeptic's evidence.** I tried to refute the finding and could not. Its main claim holds. The secondary point about [KONF-I4] does not.

**Facts 8 and 9 no longer carry the opening**
- In the staged DeclaredRedLines.cs, L429-432 are facts 8 and 9: `D(2023, 3, 27), Open`.
- Their bases cite [KONF-P5], [KONF-P6], [KONF-P2], [KONF-I14], [KONF-I15], [KONF-P1], [KONF-I23] and [KONF-I25]. Neither basis has a 26 June sentence or cites [KONF-I8], [KONF-I10] or [KONF-I11].
- Facts 5 and 6 (L424, L426) still say "The 26 June opening ... stands only on pages F2, as the record reads it, does not count [KONF-I10], [KONF-I11]". Fact 7 (MN, L428) has no such note, as at HEAD.
- At HEAD, fact 3 (L420, 06-20 to 07-06) said "His 26 June opening to PiS or PO (\"…to wszystko jest na stole\") stands only on Super Express's interview and its relays by PAP and RMF24 [KONF-I8], [KONF-I10], [KONF-I11], which F2, as the record reads it (the record's §12, doubt 4), does not count, so it lifts nothing". Fact 4 (L422) said the same with [KONF-I8], [KONF-I11].
- `grep -c 'KONF-I8\]'` gives 0 for the staged file and 2 for HEAD. `git grep --cached` finds no file under Assets that cites it.

**The record says the opening bears on facts 8 and 9**
- The change's own §12 (staged record L407-408) says: "Under either, the June opening is dated 2023-06-26: facts 5–9 end that day (3 and 4 were replaced on 27 March)".
- So the PiS/PO opening, which names PiS and PO, now bears on exactly the facts whose bases leave it out.
- Those two facts stand through 26 June only on F2's second READING, which is pending Elias. The code states that premise on facts 5 and 6 but not on 8 and 9.

**No check catches it**
PolishDeclarationsDiagnostic reads only these parts of a basis:
- the first tag (L79, L118);
- whether each tag is a register row (L88);
- the PolandSpokenWords mark (L121, L127);
- the "DECLARED (F1" prefix (L134).

**Why minor and not a defect**
- No formation, date or check result changes.
- CampaignDeclared.cs L33 says "LiveCampaignSetup stages none for Poland", so the DECLARED page shows nothing for Poland.
- On polling day, HEAD's facts 8 and 9 also lacked the note.
- The bases point to "the record's §2 and §12", and the record carries the opening (staged L68-69, L87: "so the lines run on through it").
- It is still a provenance gap this change introduced in the code, and it is cheap to fix.

**The secondary point about [KONF-I4] does not hold**
- The record's §8 rows 3 and 4 cite "[KONF-P4]; [KONF-I4]".
- §12 (L418-419) says that under the reading by F1's forms only, facts 3 and 4 "run to that day" (2023-06-28). Under that reading the 20 June page is a restatement inside the interval.
- Moving it off facts 3 and 4 would put the code out of step with §8 and lose that reading.

**The skeptic's corrected fix.** Add the 26 June note to facts 8 and 9, after their first tag so checks (a) and F2 are unaffected. All three tags are register rows, so check (b) still holds.

- **Fact 8:** add "His 26 June opening to PiS or PO (\"…to wszystko jest na stole\") stands only on Super Express's interview and its relays by PAP and RMF24 [KONF-I8], [KONF-I10], [KONF-I11], which F2, as the record reads it (the record's §12, doubt 4), does not count, so it lifts nothing."
- **Fact 9:** add "The 26 June opening stands only on pages F2, as the record reads it, does not count [KONF-I8], [KONF-I11]."

Keep [KONF-I4] on facts 3 and 4. That mirrors §8 rows 3-4, and under the reading by F1's forms only it falls inside their interval.

Optional: name [KONF-I4] on facts 8 and 9 too, as a restatement of the cabinet half. Fact 9 already does this for [KONF-I14]'s "nie zamierzamy zawierać koalicji z PO".

### 15. The 27 March start of facts 8-9 depends on readings §2 does not state for two posts the sweep itself flagged

- **Lens:** code - **reviewer:** note - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/DeclaredRedLines.cs:431

**The scenario.** Out-of-tree Konf/own/SOURCES.tsv marks two posts as candidates. (1) x_krzysztofbosak_2022-07-20_1549876718659407873.json: "Polską rządzą nieodpowiedzialni szkodnicy. Nie tylko trzeba ich odsunąć od władzy...", noted "plain end-PiS-rule; earliest such own-record line found". (2) x_KONFEDERACJA__2023-03-01_1630855869221990400.json: on the party's own account, Mentzen's "NIE ❌" to "Jeśli obieca obniżkę podatków, poprę Tuska na premiera". He had been co-chairman since 2023-02-14 according to the sweep's own role evidence. The record's §2 list of earlier own-record words does not mention (1) at all. For (2) it gives no reason of its own: "an interest, not a refusal" grammatically attaches to the 03-21 post that follows it. Under §2's own READING (words that name power and refuse to back X are F1's line), (2) would start fact 9 on 2023-03-01. Fact 4 (03-21 to 03-27) would then lie inside fact 9's span, and DeclarationDatesDiagnostic's no-overlap check would fail until fact 4 is removed. No game reader turns on it, because no Polish reader asks before polling day.

**The fix proposed.** This is for the record lens or Elias. §2 should give the reason (1) and (2) are not the start, e.g. Bosak was not yet co-chairman, PiS is not named (as for [KO-P5]), or a quiz answer is not a declaration. Otherwise the dates of facts 4 and 9 should be revisited.

**The skeptic's evidence.** I could not refute this finding. Every factual claim in it checks out against the staged files and the sweep's out-of-tree files. The gap is in the record. The code is consistent with the record and its checks pass.

1. **Post (1) is flagged by the sweep and missing from the record.** Konf/own/SOURCES.tsv row 40, x_krzysztofbosak_2022-07-20_1549876718659407873.json, quotes "Polską rządzą nieodpowiedzialni szkodnicy. Nie tylko trzeba ich odsunąć od władzy, ale wymienić w parlamencie." and notes "(plain end-PiS-rule; earliest such own-record line found)". The staged JSON has the same text with created_at 2022-07-20T21:59:39Z and quotes a post about Premier Morawiecki. The sweep gives row 57 ([KONF-P2], 2023-07-06) the same lowercase class, "plain end-PiS-rule", and the record reads [KONF-P2] as F1's own form. Grepping the staged coalition_declarations_2023.md and fetch_log.md for 2022-07-20, szkodnicy or 1549876718659407873 returns nothing. The record lists none of Bosak's own-account posts from before he became co-chairman (rows 38, 39, 40, 66, 67, 68) and gives no reason.

2. **Post (2) has no reason of its own.** The §2 list (record lines 132-141) gives the Wawer, Dziambor, Winnicki and RN items each their own "- reason". The 2023-03-01 quiz has only "in a radio quiz". It then shares the trailing "- an interest, not a refusal", which fits only "Nie jesteśmy zainteresowani..." (14:51Z on 03-21) and not a "NIE". SOURCES.tsv row 60 itself grades the quiz "less-plain/plain keep-Tusk-from-power". Row 30 is the role evidence: co-chairmen named 2023-02-14.

3. **Three record statements depend on (1) being left out without a stated reason:**
   - §12 (lines 419-421): "an earlier plain line it missed would move these dates earlier still ... Earlier own-record words, less plain, are listed in §2." But (1) was found, not missed, and it is not listed.
   - §2 (lines 98-99) and §12 (line 418): "Read by F1's forms only ... both lines start on ... 2023-06-28 [KONF-P6]". Yet (1)'s "odsunąć od władzy" is itself one of F1's forms.
   - [KONF-P6] names "Kaczyński", not PiS, and §2 keys [KONF-P5] to PiS through the premise "Morawiecki's rule is PiS's". Either premise would equally key (1)'s "those who rule Poland". The [KO-P5] reading ("power is named, PiS is not") would exclude (1), but the record never applies it to (1).

4. **The overlap consequence holds.** DeclarationDatesDiagnostic.cs lines 96-99 key spans by Kind+Party+Other and require `f.From >= b || f.Until <= a`. Fact 9 starting 2023-03-01 would fail against fact 4's [2023-03-21, 2023-03-27). Fact 8 starting 2022-07-20 would fail against fact 3 the same way.

5. **No reader uses these dates today.**
   - Poland's start is 2023-10-15 minus (8+26) weeks, i.e. 2023-02-19, which is inside the window.
   - The DECLARED page (GameController.CampaignDeclared.cs:34-35) needs a staged campaign. LiveCampaignSetup.TryFor (line 148) stages only Sweden and Germany.
   - ConfidenceProcedure.RulesOf(Poland) is Unsourced (line 34). MoveNoConfidence refuses it (SimulationManager.cs:2833), TryAiMotion returns early (3028-3029) and ParliamentRows returns early (702), so no mid-term reading happens.
   - The election reads polling day, and facts 5-9 stand on it either way.
   - So there is no runtime or check failure now, and n784a is clean.

**Severity re-grade:** this is more than a note. The sweep's own "earliest" candidate was dropped without a word, the §12 completeness claim is false, and Elias has no doubt to rule on. It has no runtime effect, so it is minor. The finding names fact 9; post (1) bears on fact 8 and fact 3.

**The skeptic's corrected fix.** No code change. Fix the record (coalition_declarations_2023.md) before commit:

(a) Add post (1) to §2's list of earlier, less plain words, with its file `x_krzysztofbosak_2022-07-20_1549876718659407873.json`, its date and an explicit reason. Possible reasons:
- before the co-chairmanship of 2023-02-14 (`x_KONFEDERACJA__2023-02-14_1625540441478254592.json`), Bosak's own account is not the party's record;
- power is named and PiS is not ([KO-P5]'s reading);
- "trzeba" gives no actor and no pledge.

(b) Give the 2023-03-01 quiz its own reason. For example: the words are the presenter's, Mentzen's answer is a yes/no to a hypothetical with a condition, so it is not a declaration. Attach "an interest, not a refusal" to the 2023-03-21 14:51Z post only.

(c) In §12, replace "an earlier plain line it missed" with a sentence admitting that the sweep found one and did not read it as the start. Note that the F1-forms-only alternative (facts 8 and 9 starting on [KONF-P6]) also rests on leaving out (1).

(d) Add a bullet for Elias stating the other reading:
- read as the start, (1) starts fact 8 on 2022-07-20 and fact 3 goes;
- read as the start, (2) starts fact 9 on 2023-03-01 and fact 4 goes, which DeclarationDatesDiagnostic's no-overlap check requires;
- no polling-day line changes;
- no game reader asks earlier, because Poland stages no run-up page and its Unsourced confidence rules open no mid-term round.

If Elias rules the other way, re-date DeclaredRedLines.PolandTimeline, remove fact 3 and/or fact 4, and update the basis strings and §8.

### 16. Bosak's own post of 2022-07-20 is left out of the record, though the sweep labels it its 'plain end-PiS-rule; earliest such own-record line found'. The F1-forms start for fact 8 is misstated.

- **Lens:** sources - **reviewer:** defect - **skeptic:** defect
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:418

**The scenario.** The post is Konf/own/x_krzysztofbosak_2022-07-20_1549876718659407873.json: @krzysztofbosak (user id 80676829, the id [KONF-P5]'s own mention gives him), created_at 2022-07-20T21:59:39Z, isEdited false. It quotes a post on 'Premier @MorawieckiM''s coal embargo and says: 'Polską rządzą nieodpowiedzialni szkodnicy. Nie tylko trzeba ich odsunąć od władzy, ale wymienić w parlamencie.'

That is F1's own form (remove from power), aimed at Morawiecki's government. It is exactly §2's premise for [KONF-P5]: 'Morawiecki's rule is PiS's'. Out-of-tree SOURCES.tsv labels it 'plain end-PiS-rule; earliest such own-record line found'.

The record never mentions it. §2's list (l.132-141) omits it. §2 l.98 and §12 l.418 say that, read by F1's forms only, facts 8 and 9 start 2023-06-28 [KONF-P6]. l.420 implies only a line the sweep *missed* could move the dates. So Elias is asked to rule on an alternative whose start the sweep's own catalogue contradicts. Under the header's 'always wins' READING plus §2's premise, fact 8 (DeclaredRedLines.cs:429, From D(2023,3,27)) would start 2022-07-20.

Separately, [KONF-P6] names Kaczyński, not PiS, and no Kaczyński→PiS premise is stated. Apply the test §5 used on [KO-P5] ('power is named, PiS is not') and fact 8's F1-forms start is [KONF-P2], 2023-07-06. So 2023-06-28 is not the start under either keying.

Bosak's other own posts before 27 March are also unlisted:
- 2022-05-16: 'Chętnie zagłosowałbym za odwołaniem premiera.'
- 2022-06-13: 'My byśmy nigdy czegoś takiego nie zaakceptowali.' (on co-forming PiS's ruling majority)
- 2023-02-11: 'Precz z nową komuną PiSu i Morawieckiego.'

**The fix proposed.** List these posts in §2 with the reason each is not the start. Possible reasons: Bosak was not yet its leader, since the party's own post x_KONFEDERACJA__2023-02-14_1625540441478254592.json names the co-chairmen that day; or 'trzeba' is impersonal. A 'PiS is not named' reason would also bar [KONF-P5] and [KONF-P6]. Otherwise, date fact 8 from 2022-07-20. Correct the F1-forms-only start for fact 8 in §2 l.98 and §12 l.418 (and in fact 8's basis if it changes). Reword l.420 so it no longer implies nothing plainer was found.

**The skeptic's evidence.** THE POST IS REAL AND SAYS WHAT THE FINDING SAYS. Konf/own/x_krzysztofbosak_2022-07-20_1549876718659407873.json: user krzysztofbosak, id_str 80676829, created_at 2022-07-20T21:59:39.000Z, isEdited false, text "...Polską rządzą nieodpowiedzialni szkodnicy. Nie tylko trzeba ich odsunąć od władzy, ale wymienić w parlamencie. Potrzebujemy głębokiej wymiany kadr politycznych." It quotes a post that begins "Premier @MorawieckiM wprowadził embargo na rosyjski węgiel". The staged [KONF-P5] JSON's own mention gives krzysztofbosak the same id 80676829.

THE SWEEP CATALOGUED IT AS OWN RECORD. SOURCES.tsv row 40 labels it "(plain end-PiS-rule; earliest such own-record line found)". The staged fetch_log says the sweep read "the X posts of ... its co-chairmen @krzysztofbosak and @SlawomirMentzen ... January 2022 to 13 July 2023", "listed with digests in its SOURCES.tsv". Note that row 33 also carries an "earliest own-record line found" label, for [KONF-P5], so the catalogue disagrees with itself. The record took row 33's label and says nothing about row 40.

THE RECORD OMITS IT ENTIRELY. A grep of the staged record, fetch_log and DeclaredRedLines.cs for 1549876718659407873, 2022-07-20 and szkodnicy finds nothing.
- §2 l.132-141 lists six earlier items, each with a reason. One is Bosak on Ruch Narodowy's account, set aside as "not the party's own". None of Bosak's own-account posts are listed: rows 38, 39, 40, 66, 67, 68 (2022-01-25, 2022-05-16, 2022-06-13 twice, 2022-07-20, 2023-02-11).
- §2 l.98-99 and §12 l.418-419 say: "Read by F1's forms only ..., facts 8 and 9 start on Mentzen's own post of 2023-06-28 [KONF-P6]".
- §12 l.419-421 says: "The sweep reads Mentzen's posts as a sample; an earlier plain line it missed would move these dates earlier still ... Earlier own-record words, less plain, are listed in §2." The sweep did not miss this line. It found it and graded it plain.

THIS BREAKS THE RECORD'S OWN PRACTICE. For KO, §5 l.237-241 lists the earlier [KO-P5] with its reason, and §12 l.393-394 offers Elias that earlier start. Konfederacja's earlier candidate gets neither.

THE KACZYŃSKI PREMISE IS MISSING. §2 l.97-98 states premises for Morawiecki→PiS and Tusk→KO only. Yet [KONF-P6] ("odsunąć od władzy Kaczyńskiego") is used toward PiS. Elsewhere the record holds "if a pledge against a party's leader keys the party" open for Elias (§1 l.57, §12 l.392). So the F1-forms-only start of 2023-06-28 for fact 8 rests on an unstated premise, against the CLAUDE.md claim convention.

WHAT IS CONDITIONAL. "Fact 8 must start 2022-07-20" holds only if Bosak's own account counted before he became co-chairman. The header (l.15-16) counts words "by the party, its leader or an authorised spokesperson". The party's own post of 2023-02-14 (row 30) names him co-chairman that day. Its own 2022-09-20 page calls Kulesza the Sejm group's chairman and gives Bosak no title. The record states no reading either way. If his account counts, then under the header's "always wins" READING and §2's Morawiecki premise, DeclaredRedLines.cs:429 (Konf→PiS, true, true, From D(2023, 3, 27)) should start 2022-07-20. Fact 3's "Replaced on 2023-03-27" would then be wrong too.

A FURTHER UNLISTED CANDIDATE. SOURCES rows 75-76 hold a party-account post of 2023-06-25 quoting candidate Płaczek: "żeby PiS i PO nigdy nie wróciło do władzy, jak ją stracą!" That is party-published words in F1's "block its return" form, earlier than [KONF-P6], and also unlisted.

RUNTIME. Poland's StartDate is CampaignCalendar(2023-10-15).PreCampaignStart, which is 8+26 weeks earlier: 2023-02-19. Fact 8's start therefore falls inside the game's world. Today nothing reads Poland's lines off polling day:
- ConfidenceProcedure.RulesOf(Poland) is Unsourced, so no mid-term round opens (SimulationManager.cs:2833).
- LiveCampaignSetup.TryFor stages Sweden and Germany only (l.148), so no DECLARED page.
- Polling day is unchanged.
So this is a defect in the record's ruling-facing claims, and in dates a future DECLARED page would show. No game result changes now.

**The skeptic's corrected fix.** 1. In §2, after l.141, list Bosak's own-account posts the sweep holds, quoting the plainest: "Polską rządzą nieodpowiedzialni szkodnicy. Nie tylko trzeba ich odsunąć od władzy..." (`x_krzysztofbosak_2022-07-20_1549876718659407873.json`). Also list 2022-05-16 "Chętnie zagłosowałbym za odwołaniem premiera.", 2022-06-13 "My byśmy nigdy czegoś takiego nie zaakceptowali." and 2023-02-11 "Precz z nową komuną PiSu i Morawieckiego.".
   - Give the reason as a stated READING, for example: "a co-chairman's own posts are the party's own record from the day its Rada Liderów named him (its own post of 2023-02-14, `x_KONFEDERACJA__2023-02-14_1625540441478254592.json`); before it his account is a member's, as his words on Ruch Narodowy's account are."
   - Do not use "PiS is not named" as the reason, because it would bar [KONF-P5] and [KONF-P6] too.
   - Do not use "trzeba is impersonal" either, because [NL-P31] and [KO-P1] were accepted in the same form.
   - List the party-published Płaczek line of 2023-06-25 (rows 75-76) as well, with its reason.

2. In §12 (l.415-421), add the other reading for Elias. If a member of the Leaders' Council before 2023-02-14 is "its leader", the 2022-07-20 post is F1's form against Morawiecki's government (§2's premise) and dates fact 8 from 2022-07-20 under the built reading. No polling-day line changes, but the line would then stand from Poland's opening day (2023-02-19).

3. Rewrite l.419-420. Say that only Mentzen's posts are a sample, and that the sweep found earlier own-account words by Bosak from before his co-chairmanship, listed in §2 and set aside by that READING. Drop "an earlier plain line it missed".

4. Fix the F1-forms-only start for fact 8 in §2 l.98-99, §12 l.418 and fact 8's basis (DeclaredRedLines.cs:430). Either state "Kaczyński's power is PiS's" as a DECLARED premise, or, matching §1's treatment of [PIS-I1], give that start as [KONF-P2], 2023-07-06, with [KONF-P6] counted only under the premise.

5. If the author instead reads Bosak as its leader in July 2022:
   - Bring the post into the tree as a new register tag with its SHA-256 (SHA256SUMS.txt, fetch_log).
   - Set DeclaredRedLines.cs:429 to From D(2022, 7, 20) and change §8 row 8 to match.
   - Reshape fact 3, which would no longer be "replaced on 2023-03-27".
   - Re-run PolishDeclarationsDiagnostic.

### 17. Mentzen's 'NIE' of 2023-03-01 to backing Tusk as prime minister is listed with no reason that fits it. Under §2's own READING it would date fact 9.

- **Lens:** sources - **reviewer:** defect - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:137

**The scenario.** The post is Konf/own/x_KONFEDERACJA__2023-03-01_1630855869221990400.json: the party's own account, created_at 2023-03-01T09:01:53Z, isEdited false. Mentzen has been co-chairman since 2023-02-14 (the party's own post). It reads: 'Krótka piłka w @Radio_ZET ... Red.: Jeśli obieca obniżkę podatków, poprę Tuska na premiera. / @SlawomirMentzen: NIE ❌'.

§2 (l.137-140) files it under 'Earlier and less plain, not read as the start' with no reason of its own. The only tag in that clause, 'an interest, not a refusal', fits the 21 March post, not a 'NIE'.

By §2's READING for [KONF-P5], lending no hand to Tusk's return to power is F1's line toward KO, through K-1f's premise. A co-chairman's refusal, on the party's own record, to back Tusk as prime minister even for a tax cut is the same refusal of support, 26 days earlier. Under the header's 'always wins' READING, fact 9 (DeclaredRedLines.cs:431, From D(2023,3,27)) would start 2023-03-01. Fact 4 (DeclaredRedLines.cs:421, the KO cabinet line of 21-27 March) would then never stand on its own. Nothing in the record tells the reader why 2023-03-27 stands instead.

**The fix proposed.** State why a quiz 'NIE' is not the line. Possible grounds: it answers a host's statement inside a game; 'poprę' is Mentzen's personal support; it is conditional on a tax promise; it refuses support without refusing to join. Keep 'an interest, not a refusal' on the 21 March item only. Otherwise, date fact 9 from 2023-03-01 and retire fact 4.

**The skeptic's evidence.** WHAT HOLDS (the gap is real):
- The source is as the finding says. `Konf/own/x_KONFEDERACJA__2023-03-01_1630855869221990400.json`: screen_name KONFEDERACJA_, created_at 2023-03-01T09:01:53.000Z, isEdited false. Its text: "Red.: Jeśli obieca obniżkę podatków, poprę Tuska na premiera.\n@SlawomirMentzen: NIE ❌".
- Mentzen was co-chairman from the party's own post of 2023-02-14 ("Rada Liderów powołała @krzysztofbosak i @SlawomirMentzen na współprzewodniczących").
- The sweep did not decide it. SOURCES.tsv row 60 tags it "(less-plain/plain keep-Tusk-from-power: would not back Tusk as PM)".
- Staged record l.132-141: every other item in "Earlier and less plain, not read as the start" has its own " - reason" before its file:
  - Wawer "- a condition, the voters its actor, no pledge"
  - Dziambor "- where it stands, not whom it would govern with"
  - Winnicki "- a purpose, not a refusal"
  - Bosak on the member party's account "- not the party's own"
- The NIE item's reason slot is empty: "Mentzen's "NIE" to "poprę Tuska na premiera" in a radio quiz, 2023-03-01 (`…1630855869221990400.json`), and, 24 minutes before the line, "Nie jesteśmy zainteresowani…" (`…1638191609131982848.json`) - an interest, not a refusal".
  - The trailing tag echoes "zainteresowani". It does not fit a "NIE".
  - "in a radio quiz" names the format but gives no reason.
- Nothing else gives a reason. `git grep --cached` finds the post only at l.137-138. §12 l.415-421 says only "Earlier own-record words, less plain, are listed in §2", and states only the LATER alternative ([KONF-P6], 2023-06-28).
- The record breaks its own pattern here. Every other arguable earlier start gets a "would start it then" bullet left to Elias: [PIS-I1] l.392, [KO-P5] l.393-396, [NL-P3] l.400-401. The header's READING (l.22-23, "the same declaration earlier, in whatever words") and §2's READING (l.95-98, names power and refuses both; K-1f's premise) leave this post arguable for fact 9, and the record says nothing about it.

WHAT IS OVERCLAIMED:
1. "Under §2's own READING it would date fact 9." It could, but the READING does not force it.
   - The post's words are the host's conditional sentence, and Mentzen's own words are only "NIE".
   - Strictly, the answer refuses a trade (a tax-cut promise for his backing), not Tusk's return to power. "poprę" is first person.
   - These are defensible grounds for "less plain", and the record half-names one ("in a radio quiz").
   - So this is a reading left unstated, not a wrong date. The finding's own fix lists the same grounds.
2. The "defect" grade. Nothing in play changes.
   - Fact 9 (DeclaredRedLines.cs:431) is Open from either date, and fact 4 (:421) ends before polling day either way. So the polling-day lines (§8 l.296) and n784a are the same.
   - Poland's world opens at the standard run-up. WorldClock.StartDate's default is CampaignCalendar.PreCampaignStart, which I worked out as 8 + 26 weeks before 2023-10-15, so March 2023 is inside the game's span.
   - But nothing in the game reads the March lines for Poland:
     - DeclaredPageAvailable (GameController.CampaignDeclared.cs:31-33) needs a staged campaign, and LiveCampaignSetup.TryFor:148 stages only Sweden and Germany.
     - ConfidenceProcedure.cs:34 gives Poland `Rules.Unsourced`, so no mid-term round opens.
     - PolishDeclarationsDiagnostic holds each fact's from date to its first tag's register cell, and the staged facts pass that check.

**The skeptic's corrected fix.** Fix the wording; do not re-date unless Elias rules otherwise.
(1) §2 l.137-139: give the quiz item its own reason, in the slot the other items use, and keep "an interest, not a refusal" on the 21 March item only. For example: "Mentzen's "NIE" to the host's "Jeśli obieca obniżkę podatków, poprę Tuska na premiera" in Radio ZET's quick-fire round, 2023-03-01 - a one-word answer to the host's sentence: it refuses a trade (a tax-cut promise for his backing), not Tusk's return to power (`x_KONFEDERACJA__2023-03-01_1630855869221990400.json`); and, 24 minutes before the line, "Nie jesteśmy zainteresowani współtworzeniem rządu z @MorawieckiM" - an interest, not a refusal (`x_KONFEDERACJA__2023-03-21_1638191609131982848.json`)".
(2) §12, the Konfederacja bullet (l.415-421): state the other reading, as the record already does for [KO-P5], [PIS-I1] and [NL-P3]. For example: "Read as the same declaration in other words (the header's first READING, K-1f's premise), the party's own post of 2023-03-01 would start fact 9 then and fact 4 would go - no polling-day line changes, and no Polish page or round reads March 2023 - Elias's."
Only if Elias rules that the quiz answer is the line: move fact 9's from date to 2023-03-01 and retire fact 4. Then add a register row for the post, bring it into raw/declarations_2023/Konf/ byte for byte with its SHA256SUMS and fetch_log entries, and update §8 rows 4 and 9.

### 18. An own-record counter-signal 25 minutes before [KONF-P5] ('Interesują nas ministerstwa') is missing from the record

- **Lens:** sources - **reviewer:** defect - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:81

**The scenario.** The post is Konf/own/x_KONFEDERACJA__2023-03-27_1640255696192122882.json: the party's own account, created_at 2023-03-27T07:33:27Z, isEdited false. It reads: 'Współprzewodniczący @KONFEDERACJA_ @krzysztofbosak w @PolsatNewsPL: Interesują nas ministerstwa, gdzie będziemy mogli realizować nasz program ...'. SOURCES.tsv calls it a 'counter-signal: open to government posts'. It is the same morning and the same co-chairman as [KONF-P5]; the 07:42 and 07:58 posts name the programme @Graffiti_PN.

§2's 'Not lifted between them' (l.81) weighs only [KONF-I1] and the portal's [KONF-I3]. Yet §621 lets a later dated own-record declaration replace a line, and §2 states the other reading for the 26 June opening.

Read as an opening, this interest in ministries, six days after 'Z nikim nie chcemy wchodzić w koalicję', would end facts 5-7 (DeclaredRedLines.cs:423-427) on 2023-03-27. They would restart on [KONF-P1], 2023-08-02.

It also bears on §2's READING (l.95-99) that [KONF-P5] refuses to *join* a PiS cabinet. A co-chairman seeking ministries while refusing only 'the continuation of Morawiecki's rule' reads as aimed at the person. §1 l.57 declines to start PiS → KO on person-aimed words. Elias would rule on the READING without the one own-record item that cuts the other way.

**The fix proposed.** Cite the post in §2 beside [KONF-P5], and say why it lifts nothing and leaves the READING standing. If it could lift the lines, state the other reading in §12 as is done for 26 June: facts 5-7 end 2023-03-27 and restart 2023-08-02, with no change on polling day.

**The skeptic's evidence.** The omission is real. The finding's conclusions overreach, so I graded it minor rather than a defect.

CONFIRMED:
- The post exists. Out of tree, `Konf/own/x_KONFEDERACJA__2023-03-27_1640255696192122882.json` carries `"created_at":"2023-03-27T07:33:27.000Z"` and `"isEdited":false`. Its text is "Współprzewodniczący @KONFEDERACJA_ @krzysztofbosak w @PolsatNewsPL: Interesują nas ministerstwa, gdzie będziemy mogli realizować nasz program oparty na trzech filarach: ...".
- [KONF-P5] (id 1640262039753969664) was posted at 07:58:39Z, 25 minutes later.
- SOURCES.tsv row 31 labels this post "(counter-signal: open to government posts; not-a-line)". It is the only row in the sweep labelled a counter-signal.
- The staged record's l.81-83, "**Not lifted between them:**", weighs only press items: [KONF-I1] (Dziennik/PAP) and [KONF-I3] (Interia).
- `git grep --cached` over *.md, *.cs and *.tsv for "ministerstw", "Interesują nas" and "1640255696192122882" finds nothing in the record, fetch_log.md or DeclaredRedLines.cs.
- So the one counter-signal on the party's own record, the source F2 says "always wins", is absent from the record. The record lists out-of-tree own-record words that support the lines, with file names (l.132-141). Elsewhere it lists counter-signals: "Against it:" (l.171), "Before it the door was open" (l.45) and "Not lifted on 2023-06-26". By its own practice, this post belongs in §2.

REFUTED OR OVERSTATED:
1. **It is unlikely to lift anything.** The record already classes the same day's "Nie jesteśmy zainteresowani współtworzeniem rządu z @MorawieckiM" as "an interest, not a refusal" (l.139-140). By that standard this post is an interest, not an opening:
   - it names no partner;
   - its condition (ministries "gdzie będziemy mogli realizować nasz program") is the reason [KONF-P4] itself gives for refusing everyone: "Żadna z tych partii nie wygląda na zainteresowaną realizacją naszego programu!" (l.72).

   The staged facts at DeclaredRedLines.cs:419-432 stand under the record's readings.
2. **The computed alternative is wrong.** The header (l.29-30) says the broadcasters' own pages "count under F2", and §12 reads [KONF-I4] as verbatim (l.424). An opening on 27 March would therefore be re-closed for TD, NL and MN on 2023-06-20 by [KONF-I4], "W przyszłej kadencji nie wejdę w koalicję z nikim". It would not restart on 2023-08-02 by [KONF-P1]. The 26 June opening lifts nothing under F2's second reading. Facts 5-7 would split, and the restarted half would carry PolandSpokenWords with [KONF-I4] marked (F2) again.
3. **The person-aimed point is already stated.** The record gives the premise "Morawiecki's rule is PiS's; the model has no prime minister" (l.97) and the F1-forms-only alternative starting on [KONF-P6] (l.98-99, l.418-419). §12 (l.392) flags the asymmetry with PiS → KO's start.
4. **Impact.**
   - No polling-day line changes under any reading.
   - No formation round opens in Poland (l.11-12).
   - Poland opens at the standard run-up, about 2023-02-19 (WorldClock.StartDate's default, CampaignCalendar 26+8 weeks).
   - Only the HQ DECLARED chip (GameController.CampaignDeclared.cs:190, `StandingOn(country, today)`) would show 27 March to 20 June differently, and only under a reading nobody adopted.

**The skeptic's corrected fix.** No code change. The fix is in the record (ElectionsData/poland/coalition_declarations_2023.md):

1. **§2, "Not lifted between them" (l.81):** add the party's own post first, ahead of the press items. Text: "on its own account, Bosak on Polsat News, 2023-03-27 07:33Z, 25 minutes before [KONF-P5]: 'Interesują nas ministerstwa, gdzie będziemy mogli realizować nasz program' - an interest, not an opening: it names no partner, and its condition is the one [KONF-P4] gives for refusing everyone". Cite it with its out-of-tree file name, `Konf/own/x_KONFEDERACJA__2023-03-27_1640255696192122882.json`, as l.132-141 already do.
2. **§12, Konfederacja own-record bullet (l.415-421):** add one clause. Read as an opening, facts 5-7 end on 2023-03-27 and restart on 2023-06-20 on RMF FM's own page [KONF-I4], which takes back F2's mark. If [KONF-I4] is not verbatim, they restart on 2023-08-02 [KONF-P1]. No polling-day line changes in either case.
3. **Same bullet:** say that the same morning's words are evidence for the F1-forms-only reading, which is already stated, so Elias rules on §2's READING with this post in view.

### 19. Member-party channels: RN's account is 'not the party's own' here, but KO's sweep counted PO's site and account; the RN quote also drops its F1-form sentence

- **Lens:** sources - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:140

**The scenario.** §2 l.140-141 excludes x_RuchNarodowy_2022-06-15_1537066142270824450.json (Bosak) as 'not the party's own'. Yet §5 l.235 counts KO's member party's channels as KO's own record:
- platforma.org is listed.
- The KO/own sweep holds 18 platforma.org pages and 75 @Platforma_org posts.
- l.241 cites x_Platforma_org.
The header (l.15) also counts words 'by ... its leader' on any channel.

The record's quote stops before the item's F1-form sentence: 'Jesteśmy od tego, żeby ich odsunąć, a nie żeby się z nimi dogadywać.' The item is filed under 'Earlier and less plain', though the sweep rates it 'PLAIN'.

Other member-party items in F1's form are not listed at all:
- Nowa Nadzieja (then Partia KORWiN) quoting Mentzen, 2022-09-05 (x_Nowa_Nadzieja__2022-09-05_1566746180289236993.json): 'Nie ma znaczenia czy rządzi PiS, PO, SLD czy PSL ... Należy ich odsunąć od władzy.'
- RN quoting Bosak, 2022-05-10, on the 'tzw. prawa i tzw. sprawiedliwości' government: 'osoby za to odpowiedzialne powinny być odsunięte od władzy'.

Under KO's boundary, and if Bosak's words then count as a leader's, facts 3-7 would date from 2022-06-15 and facts 8-9 no later than that day.

**The fix proposed.** State the own-record boundary for Konfederacja and why it differs from KO's. Quote the F1-form sentence, and file the item by its real reason (the account and/or Bosak's 2022 role, not plainness). List the other member-party F1-form items. Take it out of 'Earlier own-record words' in §12 l.420.

**The skeptic's evidence.** The core holds; some specifics are overstated.

WHAT HOLDS:
- Mislabelled item. Staged record §2 l.132 opens a list headed "**Earlier and less plain, not read as the start** (§12)". It ends at l.140-141: "Bosak on his member party's account, 2022-06-15, "Nie planujemy żadnego mariażu z istniejącymi obecnie partiami parlamentarnymi" - not the party's own (`x_RuchNarodowy_2022-06-15_1537066142270824450.json`)". §12 l.420-421 then calls that list "Earlier own-record words, less plain". By the record's own words this item is neither own-record nor argued to be less plain. The sweep's SOURCES.tsv row 80 rates it "(PLAIN, no-coalition-with-anyone; RN account, Bosak speaking)".
- Truncated quote. The full text of the file is: "...Nie planujemy żadnego mariażu z istniejącymi obecnie partiami parlamentarnymi, bo mamy skrajnie negatywną ocenę tych partii. ... Jesteśmy od tego, żeby ich odsunąć, a nie żeby się z nimi dogadywać." The record quotes only the first clause.
- Uneven, unstated boundary. §5 l.234-236 dates KO by "a sweep of KO's own pages and posts - platforma.org, koalicjaobywatelska.pl, ...". platforma.org is PO's site, and KO/own holds 18 pages from it. §3 and doubt 3 (l.377) date TD facts from its member parties' own pages: Polska 2050's [TD-P11], and PSL's [TD-P6], where the speaker is Jarubas, not a leader of the list. For Konfederacja, a member party's account is "not the party's own". Nowhere is this stated as a READING or explained, although the fetch_log (l.57) says the Konf sweep did read "its member parties' accounts".
- Unlisted member-party items in F1's form:
  - x_Nowa_Nadzieja__2022-09-05: Mentzen, "Nie ma znaczenia czy rządzi PiS, PO, SLD czy PSL ... Należy ich odsunąć od władzy."
  - x_RuchNarodowy_2022-05-10: Bosak, "osoby za to odpowiedzialne powinny być odsunięte od władzy".
  - x_RuchNarodowy_2022-07-20, in RN's own voice: "Tego rządu trzeba się pozbyć i raz na zawsze odsunąć Bandę Czworga od władzy."
  - x_RuchNarodowy_2023-01-16: "Czas zakończyć rządy Morawieckiego i Ziobry".

WHAT IS OVERSTATED OR REFUTED:
- The "75 @Platforma_org posts" are one account. All 75 KO/own/x_Platforma_org_* files decode to user id 53003895, screen_name Obywatelska_KO, "Koalicja Obywatelska". That is the account the register (KO-P2..P5) calls "KO's own account", and fetch_log l.45 says "@Obywatelska_KO (formerly @Platforma_org)". So l.241 cites KO's own account under its 2022 handle. RN's account never became Konfederacja's. The clean precedents are platforma.org and TD's member pages.
- "żeby ich odsunąć" is not F1's form under the record's own §4 READING (l.200-201: "F1's forms name power or government"). It names no power, and "ich" means all parliamentary parties, opposition included.
- The leader premise fails on the sweep's own role evidence. x_KONFEDERACJA__2023-02-14_1625540441478254592.json reads: "Rada Liderów powołała @krzysztofbosak i @SlawomirMentzen na współprzewodniczących RL". So Bosak was not co-chairman on 2022-06-15. The premise is also not needed: under a member-party boundary, party-published words count whoever speaks, as Wawer's and Dziambor's do.
- "Facts 8-9 no later than 2022-06-15" does not follow. Under §4's READING they would start in 2022 on other items, for example 2022-07-20.

EFFECT:
- Poland's start is CampaignCalendar(2023-10-15).PreCampaignStart = 2023-02-19 (WorldClock.cs:231; CampaignClock.cs:72, 75, 112-114). Every 2022 date falls before it.
- No formation round opens in Poland before polling day (record l.11-12), and no polling-day line changes.
- The only thing that would change is the run-up DECLARED page (GameController.CampaignDeclared.cs:190, `DeclaredRedLines.StandingOn(country, today)`). It would show Konfederacja's lines from day one instead of from 21 or 27 March, and only if Elias ruled member-party channels in.
- So this is a documentation and claim-convention gap (a reading applied without being stated), not a behaviour defect.

**The skeptic's corrected fix.** 1. §2: take the RN item out of the "Earlier and less plain" list and give it its own sentence with its real ground. Quote it in full, including "Jesteşmy od tego, żeby ich odsunąć, a nie żeby się z nimi dogadywać.", and note that it names no power (§4's READING).
2. State the boundary as a READING. Konfederacja's own record is its own account @KONFEDERACJA_, konfederacja.pl, and its co-chairmen's own posts; there were co-chairmen only from 2023-02-14 (x_KONFEDERACJA__2023-02-14_1625540441478254592.json). A member party's account (RN, Nowa Nadzieja) is not part of it. Say why this differs from KO and TD:
   - KO: platforma.org is swept as KO's own (§5), and @Obywatelska_KO was PO's @Platforma_org in 2022.
   - TD: its member parties' own pages date its facts ([TD-P11], [TD-P6]).
   - A candidate principle: Konfederacja had its own account and organs, and no leader before 2023-02-14; KO is led by PO's chairman; TD is a list with no organs of its own.
3. §12: state the other reading and what it would change. If member-party channels count:
   - Facts 3-7 would start 2022-06-15 (x_RuchNarodowy_2022-06-15).
   - Facts 8-9 would start in 2022 on RN's own "raz na zawsze odsunąć Bandę Czworga od władzy" (2022-07-20) or Mentzen's "Należy ich odsunąć od władzy" (x_Nowa_Nadzieja__2022-09-05). Read as keying PSL and Lewica, these would also make Konf → TD and → NL F1 lines rather than cabinet lines.
   - All of these fall before Poland's start of 2023-02-19: no polling-day line changes, and only the run-up DECLARED page would differ.
   List those files out of tree.
4. Reword §12 l.420-421 ("Earlier own-record words, less plain, are listed in §2") so it no longer covers the RN item.

### 20. The 21 March 14:51Z item is quoted short of 'PiSu', and 'an interest, not a refusal' sits beside [KONF-P4]'s 'nie chcemy', which the record reads as a refusal

- **Lens:** sources - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:139

**The scenario.** x_KONFEDERACJA__2023-03-21_1638191609131982848.json reads: 'Nie jesteśmy zainteresowani współtworzeniem rządu z @MorawieckiM, którego uważamy za kłamcę, który nie realizuje nawet swojego programu - PiSu, który w Brukseli zgadza się na te wszystkie szkodliwe dla Polski plany klimatyczne!'

The record's quote ends at '@MorawieckiM', which hides that PiS is named. It calls 'nie jesteśmy zainteresowani' (we are not interested) 'an interest, not a refusal', while [KONF-P4]'s 'nie chcemy' (we do not want), 24 minutes later, is read as the line. SOURCES.tsv calls this post 'plain no-coalition-with-PiS'. It falls on the same day as [KONF-P4], so no date moves, but the characterisation does not survive comparison with the line it precedes.

**The fix proposed.** Quote the post through 'PiSu'. Either give a reason that really distinguishes it from [KONF-P4], or call it a same-day statement toward PiS that moves no date.

**The skeptic's evidence.** I could not refute the finding. Every factual claim in it checks out against the staged record and the out-of-tree sources, and none of the distinctions the record could use holds up.

1. The staged record (`git show :ElectionsData/poland/coalition_declarations_2023.md`), lines 138-140, inside the new "Earlier and less plain, not read as the start" list: `... and, 24 minutes before the line, "Nie jesteśmy zainteresowani współtworzeniem rządu z @MorawieckiM" (x_KONFEDERACJA__2023-03-21_1638191609131982848.json) - an interest, not a refusal;`

2. The source, `Konf/own/x_KONFEDERACJA__2023-03-21_1638191609131982848.json`: created_at 2023-03-21T14:51:30Z. Text: "Współprzewodniczący @KONFEDERACJA_ @SlawomirMentzen: Nie jesteśmy zainteresowani współtworzeniem rządu z @MorawieckiM, którego uważamy za kłamcę, który nie realizuje nawet swojego programu - PiSu, który w Brukseli zgadza się na te wszystkie szkodliwe dla Polski plany klimatyczne!" The record's quote is an exact prefix that stops at the comma, before "PiSu".

3. [KONF-P4], `x_KONFEDERACJA__2023-03-21_1638197714667053056.json`: created_at 15:15:46Z, 24 min 16 s later. It is the same account, the same co-chairman and the same Onet clip series: "Z nikim nie chcemy wchodzić w koalicję, ani z PiSem, ani z Platformą!"

4. The sweep's own note disagrees with the record. SOURCES.tsv line 45 quotes the 14:51Z post as `"Nie jesteśmy zainteresowani współtworzeniem rządu z @MorawieckiM ... PiSu" (plain no-coalition-with-PiS)`, keeping "PiSu" in. No verifier note anywhere (repo or captures) explains moving it to "less plain".

5. The record's own readings contradict "an interest, not a refusal":
   - Line 23, header READING: the own record "carries the same declaration earlier, in whatever words".
   - Line 77 reads [KONF-P4]'s "nie chcemy" as "The same declaration as RMF FM's of 20 June, in other words", i.e. the same as "Nie wejdę w koalicję z PiS-em". If "we do not want" counts as a refusal, "we are not interested in co-forming a government with" has the same force.
   - Line 232 reads KO's own "Mnie interesuje wyłącznie ... odsunięcie PiSu od władzy" [KO-P4] as a restatement of KO's line. So an "interest" wording carries a line elsewhere in the same record.

6. The person-not-party escape fails too. The record has that category (line 55, "Earlier, aimed at the person"), but did not use it here. It would fail anyway: the post names PiS, and §2's own premise (line 97) reads "Morawiecki's rule is PiS's".

7. The missing ellipsis alone is only a style point. Winnicki's quote (lines 136-137) is also cut mid-sentence without one. What matters is that the cut drops "PiSu".

8. No date or code effect, as the finding itself says. The staged DeclaredRedLines.cs dates facts by day: `new DatedFact("Konf", "PiS", FactKind.PairLine, false, false, null, D(2023, 3, 21), D(2023, 3, 27), ...)`. In Warsaw time 14:51:30Z is 15:51 CET and 15:15:46Z is 16:15 CET (Poland moved to summer time on 26 March 2023), so both fall on 21 March. Fact 3 starts the same day under either reading. The 14:51Z post names only Morawiecki and PiS, so facts 4-7 are untouched. No (F2) mark or diagnostic check depends on it.

Severity stays minor. The record's account of a source is wrong and contradicted by its own READING and by the sweep's label. The record is what Elias rules from, but nothing in the data moves.

**The skeptic's corrected fix.** In the §2 list (staged lines 138-140), quote the post through "PiSu": "Nie jesteśmy zainteresowani współtworzeniem rządu z @MorawieckiM, którego uważamy za kłamcę, który nie realizuje nawet swojego programu - PiSu, …".

Replace "- an interest, not a refusal" with a description that holds. For example: "the same refusal toward PiS's government, on the party's own account 24 minutes before [KONF-P4] the same day. Facts are dated by day, so it moves no date (fact 3 starts 2023-03-21 either way)." Move it out of the "Earlier and less plain, not read as the start" list, for example into a same-day note after the [KONF-P4] gloss.

Do not try to distinguish it from [KONF-P4] instead. "An interest, not a want" fails: line 232 reads [KO-P4]'s "Mnie interesuje" as carrying KO's line, and line 77 reads "nie chcemy" as the same as RMF's "nie wejdę". "Aimed at the person" fails: the post names PiS, and §2 states "Morawiecki's rule is PiS's".

No change to DeclaredRedLines.cs, the register, SHA256SUMS or the diagnostic is needed.

### 21. §2's 'earlier and less plain' list leaves out party-account and co-chairman items the sweep saved

- **Lens:** sources - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:132

**The scenario.** §12 l.420 says 'Earlier own-record words, less plain, are listed in §2', so a reader takes §2 l.132-141 as the whole set. These items predate the dates used and are not listed:
- x_KONFEDERACJA__2023-01-11_1613099632258678785.json, in the party's own voice, PiS and PO named: 'Konfederacja dąży do tego żeby zarówno PiS jak i PO odesłać do lamusa!'
- x_KONFEDERACJA__2023-01-19_1616165254504890369.json, Mentzen on Polsat News: 'Absolutnie nie ma najmniejszej szansy na koalicje @KONFEDERACJA_ z @SolidarnaPL!' (Solidarna Polska stood on PiS's list.)
- Mentzen's own post x_SlawomirMentzen_2023-03-26_1639926659724857348.json, between [KONF-P4] and [KONF-P5]: 'Idziemy do tych wyborów nie po to, żeby usiąść z wami do stolika, tylko żeby wam ten stolik wywrócić.'
- Bosak's own posts named in the first finding.

Elias cannot judge 'less plain' for items he is not shown. The first item could key both PiS and KO from 2023-01-11 under a lenient reading of F1's forms.

**The fix proposed.** List these items with reasons, or say the list is a selection and point to Konf/own/SOURCES.tsv for the full catalogue.

**The skeptic's evidence.** I could not refute the core of this finding. Two of its four examples hold; two are weak.

The text itself (staged record, git show :ElectionsData/poland/coalition_declarations_2023.md):
- §12, lines 419-421: "The sweep reads Mentzen's posts as a sample; an earlier plain line it missed would move these dates earlier still, and no polling-day line turns on it. Earlier own-record words, less plain, are listed in §2." Nothing says the list is a selection.
- §2, lines 132-141: "**Earlier and less plain, not read as the start** (§12), all on X, out of tree under `Konf/own/` ...". It names exactly six items: Wawer 2022-05-16, Dziambor 2022-07-27, Winnicki 2022-09-05, Mentzen's "NIE" 2023-03-01, the Morawiecki "interest" 2023-03-21, and Bosak on the RN account 2022-06-15. Again no "among them" or "a selection".
- SOURCES.tsv is named only in fetch_log.md (lines 61-62). It is not named anywhere in the record.
- A grep of the staged record finds none of 1613099632258678785, 1616165254504890369, 1639926659724857348 or 1549876718659407873, and no "lamusa" or "Solidarna". "krzysztofbosak" appears only in [KONF-P5]'s quote and [KONF-P3]'s register row.

Items in the same class that the list leaves out (Konf/own/SOURCES.tsv):
- Row 71, x_KONFEDERACJA__2023-01-11: the party's own account quotes MP Robert Winnicki: "Nas interesuje reformowanie ... Konfederacja dąży do tego żeby zarówno PiS jak i PO odesłać do lamusa!" The sweep's own tag is "less-plain, end-PiS-rule and keep-PO-from-power". This is the same form as the listed Winnicki and Wawer items: the party's account quoting someone who is not a co-chairman. It names both PiS and PO, and it predates 2023-03-21.
- The fetch_log says the sweep covered "its co-chairmen @krzysztofbosak". None of Bosak's own posts from before 27 March appear anywhere in the record:
  - 2022-07-20: "Nie tylko trzeba ich odsunąć od władzy" (replying to a post about @MorawieckiM). The sweep tagged it "plain end-PiS-rule; earliest such own-record line found".
  - 2022-05-16: "Chętnie zagłosowałbym za odwołaniem premiera".
  - 2022-06-13 (two posts), 2022-01-25.
  - 2023-02-11: "Precz z nową komuną PiSu i Morawieckiego".
- Nothing in the record gives a reason for leaving these out. Row 29 (2023-02-14) dates the co-chairmanship, but the list never cites a role as a reason. Its only stated exclusion is the account ("not the party's own").

Where the finding is weak:
- 2023-01-19 refuses Solidarna Polska, which is not a keyed party.
- 2023-03-26 ("usiąść z wami do stolika ... wywrócić") comes after facts 3-7 start (2023-03-21) and names no addressee. Under neither reading is it a candidate for facts 8-9.
- 2023-01-11 is not "in the party's own voice". It is Winnicki quoted by the party's account, though the record counts that form.

Impact:
- No code path changes, and the facts are the same under the record's own READINGs. "Odesłać do lamusa" names neither power nor government (§4's READING).
- No polling-day line changes.
- Poland's `WorldClock.StartDate` is the standard run-up, 26+8 weeks before 15 Oct 2023, so mid-February 2023. Under a lenient reading of 2023-01-11, Konf's lines would stand from the game's first day instead of 21/27 March. That would show only on the HQ's DECLARED page during the run-up. No Polish formation round reads the lines then.
- The real loss is that Elias rules the alternative readings without seeing these items, while §12 tells him the list has them.

**The skeptic's corrected fix.** 1. Mark both the §12 sentence and §2's list as a selection, and point to the full catalogue with the sweep's own tags. For example, in §12: "the nearest are listed in §2; the sweep's whole catalogue, with its tags, is `Konf/own/SOURCES.tsv` (fetch_log.md)".
2. Add the party-account Winnicki post of 2023-01-11 (x_KONFEDERACJA__2023-01-11_1613099632258678785.json) to §2's list with its reason: PiS and PO are named, but "odesłać do lamusa" names neither power nor government (§4's READING).
3. State why Bosak's own posts from before 27 March are not read. If the reason is that the co-chairmanship dates from 2023-02-14 (x_KONFEDERACJA__2023-02-14), say so. His 2022-07-20 post, "trzeba ich odsunąć od władzy", is in F1's own form. If it counts as own record, it belongs in §12 as an alternative start, not in the less-plain list.
4. Drop the 2023-01-19 example (Solidarna Polska is not a key) and the 2023-03-26 example (it comes after facts 3-7 start and names no addressee).

### 22. 'a clip of his words on Onet' for [KONF-P4] rests on no saved byte

- **Lens:** sources - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:76

**The scenario.** The [KONF-P4] JSON names no Onet in its text, its mentions or its media. SOURCES.tsv's '(Onet Opinie clip)' rests on an nczas.com article embed, and no nczas page is saved anywhere under PoliSim-captures/sources/poland_declarations_2023. A reader checking from the tree cannot confirm where the clip came from.

**The fix proposed.** Drop 'on Onet', or save and register the page that shows it.

**The skeptic's evidence.** I could not refute the finding: no saved byte carries the attribution. But the claim itself is TRUE, and it dates nothing.

1. Record, staged, line 76: `("Współprzewodniczący @KONFEDERACJA_ @SlawomirMentzen:"; a clip of his words on Onet), created_at 2023-03-21T15:15:46Z`. This is the only "Onet" in the tree tied to [KONF-P4]. A repo grep for `Onet Opinie|nczas|Onet.*clip` finds only this line.

2. The [KONF-P4] bytes contain no "Onet". The staged raw copy `Konf/x_konfederacja_1638197714667053056_syndication.json` matches digest d970d583…386a1d (the same in SHA256SUMS.txt and register row 491), and `grep -o -i onet` returns 0. Its text is "Współprzewodniczący @KONFEDERACJA_ @SlawomirMentzen: Z nikim nie chcemy…". Its user_mentions are KONFEDERACJA_ and SlawomirMentzen only. Its media is a 16:9 video with a poster URL (pbs.twimg.com/ext_tw_video_thumb/1638197631674359809/...). The image itself is not saved.

3. The register row for [KONF-P4] (line 491) and the new fetch_log paragraph do not name Onet.

4. Out of tree, the only source is SOURCES.tsv line 46: "id from nczas.com 2023-03-21 article embed … (Onet Opinie clip)". There is no nczas file under PoliSim-captures (`find -iname '*nczas*'` returns nothing). The only Onet-named capture is `Konf/youtube_onetrano_2023-08-02_…` (Wipler, August), which is unrelated. The `Onet Opinie` hits under TD/ are Polska 2050's own category list.

5. The record's other outlet attributions are backed by their own bytes. [KONF-P1]'s "Onet Rano" (line 132) is on the page itself: "Przemysław Wipler w Onet Rano został zapytany…". [KONF-P5]'s "Polsat News's Graffiti" (line 93) is in the post text: "w @Graffiti_PN". Line 76 is the one outlier.

6. The claim is TRUE. I downloaded the two poster frames named in the saved JSONs to the scratchpad only. The [KONF-P4] frame shows Onet's yellow studio set and a split-screen interview with Mentzen, but no wordmark. The poster of the same interview's earlier clip (`x_KONFEDERACJA__2023-03-21_1638191609131982848.json`, 14:51Z, out of tree) shows the same interviewer, the same room and the "onet" wordmark, with the lower third "SONDAŻOWE WZROSTY KONFEDERACJI. Z CZEGO WYNIKAJĄ?". So the remark is correct but cannot be checked from the tree. "Opinie" (SOURCES.tsv) cannot be confirmed from either frame, but the record does not claim it.

7. Why this is only a note: the venue dates nothing. Facts 3-7 are dated by the party's own post's created_at under F2's "a date from the party's own record always wins", wherever the words were spoken. No fact, date, shape or check depends on "on Onet". The header's promise covers sources cited by tag ("the sources the facts and §§1–11 cite by its tags … is stored byte for byte"), and this untagged aside cites none, so that promise is not broken. The cost is an inconsistency with the record's practice and a remark a reader cannot verify from the tree.

**The skeptic's corrected fix.** Preferred: drop the venue, since it dates nothing. On line 76 write `("Współprzewodniczący @KONFEDERACJA_ @SlawomirMentzen:"; a video clip of his words)`.

If the venue is wanted: do not save the nczas page (a portal's reading, which F2 does not count). Save the clip's own frame instead. The poster of the same interview's 14:51Z clip (URL in `x_KONFEDERACJA__2023-03-21_1638191609131982848.json`'s mediaDetails[0].media_url_https) carries the "onet" wordmark. Bring it into raw/declarations_2023/Konf/ with its digest in SHA256SUMS.txt and a fetch_log line, then cite it: "an Onet interview (the onet mark on the same interview's clip posted 24 minutes earlier, [tag])". The [KONF-P4] poster alone shows only Onet's studio set, not the mark.

### 23. The [KONF-P5] attribution prefix collapses the source's double space

- **Lens:** sources - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:94

**The scenario.** The source text has '@krzysztofbosak  w @Graffiti_PN:' with two spaces. The record quotes '@krzysztofbosak w @Graffiti_PN:', while the header promises quotes 'as served'. The rendered Markdown is identical, but a byte-level quote check (like the one run here) flags it.

**The fix proposed.** Keep the two spaces in the source (Markdown renders them as one), or mark the prefix *(decoded)*.

**The skeptic's evidence.** I could not refute this finding. The discrepancy is real, but it is cosmetic only.

SOURCE. The staged file raw/declarations_2023/Konf/x_konfederacja_1640262039753969664_syndication.json has the same sha256 as the register and the out-of-tree copy (08ea3bd6...6356). Its "text" field reads: "Współprzewodniczący @KONFEDERACJA_ @krzysztofbosak  w @Graffiti_PN: Zaczepki Tuska ...". Code points 50 and 51 are both U+0020. X's own user_mentions confirm the two spaces independently: @krzysztofbosak sits at [35,50] and @Graffiti_PN at [54,66], which only fits two spaces before "w".

RECORD. Staged coalition_declarations_2023.md line 94 reads ("Współprzewodniczący @KONFEDERACJA_ @krzysztofbosak w @Graffiti_PN:"). An od dump shows a single space between "bosak" and "w".

THE RECORD'S OWN RULE. Header, line 3: "Every quote is the source's own words as served; where the bytes carry entities or markup inside the sentence, the quote is the decoded text, marked *(decoded)*." This record does not adopt records_by_date.md's "whitespace collapsed" convention, and it marks whitespace-only differences:
- Line 266 is marked *(decoded)* even though its only difference from the wpolityce bytes is a raw U+00A0 ("te wybory").
- The &nbsp; cases [TD-P11] (line 153) and [NL-P3] (line 214) are also marked *(decoded)*.

SCAN. I checked every quoted string in the staged record against all 74 register files. Line 94 is the only quote that differs from its source in plain ASCII whitespace. The declaration body ("Zaczepki Tuska ... do władzy Tuska.") and the [KONF-P4] prefix on line 76 match byte for byte. SOURCES.tsv does not carry the prefix, so the collapse came in when the record was written.

WHY ONLY A NOTE. No in-repo check reads quote text:
- PolishDeclarationsDiagnostic holds digests and fact/mark agreement only, which is why n784a ran clean.
- Markdown renders a space run mid-line as one space, so the rendered page is identical.
- No fact, date, tag or digest depends on the extra space.

**The skeptic's corrected fix.** On line 94, restore the served bytes: "Współprzewodniczący @KONFEDERACJA_ @krzysztofbosak  w @Graffiti_PN:" with two U+0020 after the handle. This matches the JSON text and its user_mentions offsets. The double space is mid-line, so it is not a Markdown hard break and renders as one space.

Do not use the finding's alternative fix of marking the prefix *(decoded)*. The header reserves that mark for entities or markup, and neither is involved here.

If the author would rather collapse whitespace runs, the header must say so the way records_by_date.md does. That would conflict with this record's practice of marking even U+00A0 differences (line 266), so keeping the bytes is the consistent fix.

### 24. fetch_log: 'playback refused (HTTP 429) for the whole sweep' is contradicted by a saved Wayback capture, and the site-sweep claims have no saved artefact

- **Lens:** sources - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/raw/declarations_2023/fetch_log.md:60

**The scenario.** Konf/own holds konfederacja.pl_2022-09-20_blog_wystartuje-samodzielnie_wayback20221003204620.html. It is a genuine Internet Archive id_ capture: a page in the site's 2022 theme with article:published_time 2022-09-20T11:13:35+00:00. SOURCES.tsv records it as 'Wayback id_ fetch 2026-10-05', so playback did serve pages during the sweep.

Line 55's 'its sitemaps and its feed read whole ... no news between 2022-12-31 and 2023-07-06' has no saved sitemap or feed in Konf/own or in SOURCES.tsv. So the claim that konfederacja.pl carries no earlier line cannot be checked from the sweep's own files.

**The fix proposed.** Say playback was 'refused for X post pages' (or give the time the refusals began). Save the sitemaps and feed listing beside SOURCES.tsv, or say they were not kept.

**The skeptic's evidence.** Part A (the 429 claim) holds up. Part B (no saved sitemaps) is real, but it is not a defect.

A. Staged fetch_log.md line 60 says: "The Internet Archive's playback refused (HTTP 429) for the whole sweep, so no archived copy of a post page is held". The sweep's own files contradict "for the whole sweep":
- Konf/own/SOURCES.tsv row 35 lists `konfederacja.pl_2022-09-20_blog_wystartuje-samodzielnie_wayback20221003204620.html`, fetched from `https://web.archive.org/web/20221003204620id_/https://konfederacja.pl/blog/2022/09/20/...`. Its note reads "Wayback id_ fetch 2026-10-05". That is a playback URL, not the CDX index.
- The file is a real capture, not an error page:
  - It is in the old Brooklyn theme ("© 2011-2022") with Yoast v18.3.
  - Its canonical/og:url is the old `/blog/2022/09/20/` URL.
  - It carries `article:published_time 2022-09-20T11:13:35+00:00`.
  - The live copy uses the carrino-child theme and Yoast v26.9.
  - Its SHA-256 (9dd2cfb0…) matches the row and appears in no other folder, so it was fetched in this sweep, not copied in.
- Its mtime is 2026-10-05 16:41:57. That is two minutes after the sweep's first X fetches ([KONF-P5]'s file at 16:39:38), so it sits inside the sweep's window.
- The only 429 the sweep recorded is row 93, at about 17:49-17:50: "Wayback playback of the capture itself refused (HTTP 429 bot block) during this sweep". That refusal was for one Mentzen status capture. Playback served at 16:41 and refused later, so "for the whole sweep" is false as written.
- The clause after it, "no archived copy of a post page is held", is true: Konf/own holds no Wayback capture of an X status page, only the CDX row file.
- The record's §12 bullet sends readers to fetch_log for "what was read and what refused", so this sentence is the provenance account a reader would rely on.
- Nothing depends on the sentence: no fact, date, check or polling-day result rests on it, and the staged record has no other 429 or playback text. That is why I grade it minor, not a defect.

B. Line 55 says "its sitemaps and its feed read whole". No sitemap or feed file is in Konf/own or anywhere under PoliSim-captures, and no SOURCES.tsv row mentions one. That part of the finding is accurate.
- It is not a defect, though. The fetch_log header promises that the out-of-tree folder "holds every page the sweep saved", not every listing it read.
- The KO own-record sweep (§780, already reviewed) did the same. Its SOURCES.tsv row 11 says "not in the news listing", and that listing was never saved.
- The negative claim can still be checked live against the site's sitemap. A short note saying the listings were not kept would be enough.

**The skeptic's corrected fix.** In the staged fetch_log.md line 60, narrow the claim to what the sweep's own files show:
- Replace "The Internet Archive's playback refused (HTTP 429) for the whole sweep, so no archived copy of a post page is held"
- with "The Internet Archive's playback refused (HTTP 429) the post captures the sweep asked for, so no archived copy of a post page is held (earlier in the sweep it served one konfederacja.pl capture, `konfederacja.pl_2022-09-20_blog_wystartuje-samodzielnie_wayback20221003204620.html`, out of tree and uncited)".

Leave clock times out of the sentence.

For line 55 (optional, matching KO's §780 paragraph, which also kept no listing): after "its sitemaps and its feed read whole", add "(read live; the listings were not kept)". Saving the sitemap XML and the feed beside SOURCES.tsv with their digests would also work, but nothing requires it.

## The first pass - refuted by the skeptics

- [rulings] Transcribed derived figures: '24 minutes', '340 read', '280 characters' - *I could not find a failing path. The three figures are not claims the convention covers, and all three are correct.

1. **What the convention covers.** CLAUDE.md:39 defines DERIVED as "a fact about the code or the environment". The test is "the code can change freely and no document becomes wrong". The banned list in COMPLETED.md §190 §A.5 is: counts of code things, line numbers, a measured figure presented as current, a build status, and environment facts. The same review, §190 §A.3, kept POLISIM_SEED_DATA_MACRO_OVERHAUL.md with the verdict "Its DERIVED claims are about the world, not the code - sourced figures with source, vintage and basis... It is already correct practice." Real-world numbers fall under discipline rule 2 instead ("source, vintage and basis", docs/archive/CLAIM_CONVENTION_AND_DISCIPLINE.md:221). None of the three figures is about the code, and no code change can make any of them wrong.

2. **The fetch log follows the accepted practice.** fetch_log.md is a dated log ("swept 2026-10-05"), which is a record of what was done on a day. Fetch logs already at HEAD carry the same kind of figures:
   - td_list_2023/fetch_log.md:3 "HTTP 200, 251,223 bytes" (a byte size, which §A.5 lists as an environment fact)
   - td_list_2023/fetch_log.md:5 "the file's 460 'Tak' rows"
   - minimum_wage_schedule/fetch_log.md:44 "same 59063 bytes"
   - minimum_wage_schedule/fetch_log.md:46 "rate-limit pages (620 B)"

   "340 read" qualifies "a sample", and §12 relies on that word, not on the number.

3. **"24 minutes before the line" is correct.**
   - SOURCES.tsv:45 and the post's JSON give created_at 2023-03-21T14:51:30Z for 1638191609131982848; the snowflake id decodes to 14:51:30.402Z.
   - The line itself, [KONF-P4], is 15:15:46Z (record §2 and the register; snowflake 15:15:46.075Z).
   - The gap is 1456 s, i.e. 24 min 16 s. Both posts are named in the same section, so the figure can be checked against its sources.
   - The record already uses this kind of relative-time phrasing without a label: "RMF's own post of the same moment" in §12.

4. **"Stops at 280 characters" is correct.**
   - The saved syndication JSON for 1673974781958340610 has display_text_range [0,278], note_tweet {id only}, and text ending "trwających już".
   - The next word, " 18", would bring it to 281, so X cut at the last word boundary inside its 280 limit. "Stops at 280" names that limit correctly.
   - The sweep's own SOURCES.tsv:55 says "truncated here at 280 chars".
   - The point the sentence carries, "short of these words", holds by a wide margin: "Chcemy odsunąć od władzy Kaczyńskiego i Tuska" sits about 2,000 characters into the post on the X status page ([KONF-P6], sha 728fa122…).

5. **"DocumentClaimCheck does not catch them" is true by design.** DocumentClaimCheck.cs:124 reads only root *.md files (TopDirectoryOnly). Its pattern at :64 matches only backticked `Type.Member`. Its own output says "WHAT THIS CANNOT SEE: a PROSE claim".

6. **Nothing uses these phrases.** No code or check reads them; the staged changes to DeclaredRedLines.cs and PolishDeclarationsDiagnostic.cs do not touch them.*
- [rulings] COMPLETED.md §784 is cited throughout but is not staged - *The facts are right, but the failure path depends on the session skipping a step that every commit takes. Nothing is wrong yet: this is how every pass looks while it is under review.

What I confirmed:
- COMPLETED.md is the same in HEAD, the index and the working tree, and has no §784. Its last heading is `## 783.` at line 37407. Searching HEAD for '§784' finds nothing.
- The citations are where the finding says. In the staged record: :26, :413, :415, :426 and the register heading at :429. Also fetch_log.md:54, DeclaredRedLines.cs:396 and PolishDeclarationsDiagnostic.cs:124-125.

Why it is not a defect:
1. The record can only be written after this review. CLAUDE.md:67 says a § record is "what changed · the evidence line · the commit". Reviews/README.md says "what was done about its findings is told in COMPLETED.md". §783 (COMPLETED.md:37407 onward) contains a "staged review (workflow `polisim-staged-review`, then a second pass...)" block and a "Bars ... on this commit's own tree" block. Neither can exist until this review and the bars are done.
2. Every recent commit adds its record in its own commit, alongside the files that cite it:
   - 7d71fcc4: COMPLETED.md +26
   - b183a332: +22
   - 99fdcf1c: +14
   - cd1fb480 (§780, the same Polish record): +48
   - d00b66aa: +31
   - f03784e7: +50
   - 22796935: +103
3. The number is already reserved for this change. The task calls it "§784 (next)", and the named run is n784a. The unstaged work in the tree takes the next number. UiScreenshotDriver.cs and UiScreenshotCapture.cs cite "§785: -shotparty=...", so no other pass competes for §784.
4. The same finding has been refuted four times before:
   - Reviews/2026-10-04_s772_e1_factors.md:550
   - Reviews/2026-10-05_s780_pl_declarations_f1f2f7.md:1547 (finding 34: "A pointer to a section not yet appended is therefore the normal state before a commit")
   - Reviews/2026-10-05_s783_us2_record_by_date.md:964 ("§783 lands with this commit, after this review")
   - the same file, second pass: "The facts are right but nothing is wrong yet."

One caveat specific to this change: it is not a money path. DeclaredRedLines does not match bar_tier.ps1's $money pattern, so ReviewLedgerCheck gives none of the mechanical block it gave s783. No check compares § pointers against COMPLETED.md either. The only safeguard is the process step, which is the finding's own fix: append §784 in this commit.*
- [code] The evidence run's tree is not the committed tree, and the run is narrower than what the commit owes - *The finding's first claim, that n784a ran on a working tree that held the §785 edits, is wrong. The two §785 files changed after n784a finished. All times below are local (+0200).
- The staged files were last written between 19:15:05 and 19:15:22 (DeclaredRedLines.cs was the last, at 19:15:22).
- Unity launched n784a at 19:15:53 (unity_launched.tsv: `15584 2026-10-05T19:15:53 n784a CheckSuite.RunNamedBatch`). Its row in Logs/bar_timing.tsv is at 17:16:29Z, and n784a.log was last written at 19:16:30.
- .git/index was written at 19:16:48, so the files were staged after the run.
- Assets/Scripts/Testing/UiScreenshotDriver.cs and Assets/Editor/UiScreenshotCapture.cs were both modified at 19:21:51, five minutes after n784a ended.
- dryx785 (`-shotparty=KO`) launched at 19:22:18. ProjectSettings/ProjectAuditorSettings.asset was rewritten at 19:23:47 during that run, and its diff is line endings only.

The Unity logs say the same thing:
- n784a.log line 433: `Asset File Changes: new=0, changed=2`. Those are the two staged .cs files. Every other staged path is outside Assets/.
- dryx785.log line 426: `changed=2`. Those are the two §785 files, which changed after n784a's import.
- real783.log: `changed=0`.

`git status` shows no untracked files, and none of the staged files has unstaged changes. Their modification times are all before the run. So n784a compiled and read exactly HEAD 7d71fcc4 plus the staged bytes, with HEAD's versions of the screenshot driver and capture. The evidence run's tree is the tree that would be committed.

The second claim is accurate but describes something not yet due. `bar_tier.ps1 -Staged` prints `BAR TIER: SIMULATION ... per item : CheckSuite.RunAllBatch (the cheap bar) + CheckSuite.RunSimulationBatch`. n784a labels itself a subset (n784a.log line 506: `(a SUBSET, never a tier's bar)`). The last cheap and simulation bars were cheap783 and sim783 (18:48 and 19:00 local), before the §784 edits, so neither bar has run on this tree yet.

No commit has been made, though. CLAUDE.md requires these bars before every commit (head item 4, plus "ONE GREEN BAR PER COMMIT"), and this session ran both bars before the s780, s782 and s783 commits (cheap780/sim780, cheap782/sim782, cheap783/sim783 → each commit). The anchor, UiScreenshotDriver.cs:93, is in an unstaged file that is not part of the staged diff. Nothing in the staged change is wrong.*


## What the author did about the first pass

**The dates stay** - facts 3 to 7 from the party's own post of 2023-03-21 [KONF-P4], facts 8 and 9 from its own post of 2023-03-27 [KONF-P5] - and every reason given for them, and against every earlier item, was made one rule, with each other reading put to Elias in §12 with its consequence:

- **1, 5, 15** (person-aimed words: §1 refused them for PiS → KO while §2 started Konfederacja's lines on them; the forms-only alternative tested the verb, not the party) - §2 anchors [KONF-P5] as the same declaration as Bosak's two halves on TVN24's page of 2023-07-13 [KONF-I14], in other words, and states the READING that a leader's name keys the party (Morawiecki's rule is PiS's; a cabinet holding KO is Tusk's). §1, fact 2's basis and §12's PiS → KO bullet now hold [PIS-I1] back by its page alone (credited TVN24, PAP). The alternative is now "a line starts only on words that name the party": facts 8 and 9 on [KONF-P2] (2023-07-06) and [KONF-I14] (2023-07-13, F2-dated again).
- **2, 16, 21** (Bosak's own post of 2022-07-20 and other saved items left out) - §2's earlier list is a marked selection pointing to the sweep's `SOURCES.tsv`, under a stated READING of whose record counts: the party's account, konfederacja.pl, and its co-chairmen's own posts from the Rada Liderów's naming of 2023-02-14; Bosak's own posts before it (2022-05-16, 2022-07-20, 2023-02-11) are a member's. The other reading (a council member before 2023-02-14 is "its leader") starts fact 8 on 2022-07-20 - in §12.
- **3, 17, 20** ("an interest, not a refusal" covered the 1 March quiz and would dismiss [KONF-P4]) - each item has its own reason: the quiz is a one-word answer to the host's conditional sentence, refusing a trade, not Tusk's return (other reading: fact 9 from 2023-03-01, in §12); the 21 March 14:51Z post, quoted to "PiSu", is the same day's refusal of PiS's government, carrying the line with [KONF-P4] and moving no date.
- **4, 18** (the party's own 07:33Z post of 27 March, "Interesują nas ministerstwa", not weighed) - weighed in §2 as a stated READING (an interest in ministries names no partner and no coalition, so it replaces nothing, §621), with the account's "seats" post nine minutes later; read as an opening, facts 5 to 7 end that day and restart on [KONF-I4] (F2-dated again) - in §12. Facts 5 to 7 carry the clause.
- **19** (member parties' channels) - stated in the same READING, with why KO (its 2022 account was PO's) and TD (a list without organs) differ; Ruch Narodowy's 2022-06-15 post quoted in full with its "Jesteśmy od tego, żeby ich odsunąć" sentence, RN's and Nowa Nadzieja's other posts listed; the other reading in §12.
- **6, 11, 14** (the bases) - fact 8's restatements grouped by form (F1's forms, then the coalition half); facts 3 and 4 no longer cite RMF FM's restatement of 20 June, after they end; facts 8 and 9 carry the 26 June opening ([KONF-I8], [KONF-I11]).
- **7, 12** (the F2 check printed pages as facts) - pages and facts are counted apart.
- **8, 13** (comments stating the register's current content) - the summary of `PolandSpokenWords` states the rule only; the check's comment says a register with no mark and no fact carrying it holds, not that none is allowed.
- **9, 24** (fetch_log overclaimed) - the Internet Archive refused the post captures asked for and served one konfederacja.pl capture, out of tree and uncited; the site's listings were read live and not kept.
- **10, 22** ("a clip of his words on Onet" rested on no saved byte) - dropped.
- **23** ([KONF-P5]'s prefix) - quoted with the source's double space.

Named run `n784b` after the fixes (`PolishDeclarationsDiagnostic`, `DeclarationDatesDiagnostic`, `FormationSweepDiagnostic`, `DocumentClaimCheck`): 4 of 4 clean.

## The second pass - confirmed (verbatim)

Two lenses - the first pass's answers, the code and the checks - over the staged diff after the first pass's fixes; 13 findings survived their skeptics, none a defect.

### 1. Reading (1)'s consequence is wrong: own-record words name the parties before 6 and 13 July, so fact 9 is not 'F2-dated again'

- **Lens:** fixes - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:449

**The scenario.** §12 (1) at line 448-449, repeated in §2 at line 106-109, says that under 'a line starts only on words that name the party' facts 8 and 9 start 2023-07-06 [KONF-P2] and 2023-07-13 [KONF-I14] '(F2-dated again)'. §2 admits [KONF-I14] because it 'names PO beside Tusk'. By that same test, [KONF-P6] qualifies earlier. It is Mentzen's own long post of 2023-06-28, which §2 already cites as a restatement. Its text (Konf/x_mentzen_1673974781958340610_xcom.html) names the parties beside the leaders: 'koniec rządów PiS w Polsce. To będzie też koniec trwających już 18 lat rządów POPiS. Jeżeli te dwie partie…' comes before 'Chcemy odsunąć od władzy Kaczyńskiego i Tuska.' So both facts would start 2023-06-28, on the party's own record. Earlier still, the party's own account published on 2023-06-25 'żeby PiS i PO nigdy nie wróciło do władzy, jak ją stracą!' (x_KONFEDERACJA__2023-06-25_1672921599907864576, full text in its _fxtwitter twin). That names both parties in F1's 'block its return' form. §2 (line 168) sets it aside only as 'Later, needed for no start', which holds under the main reading but not under (1). The record has no speaker-based exclusion for it: it weighs the Wawer, Dziambor and Winnicki party-account posts on content, and §11 says Wipler's words count as the party published them. On the strict form of (1), where the F1 words themselves must name the party, [KONF-I14]'s F1 words name Tusk, not PO, so it cannot start fact 9 either. On no form does fact 9 return to [KONF-I14] with F2's mark. If Elias rules (1) on this text, he will expect [KONF-I14] re-marked (F2) and PolandSpokenWords back on fact 9, and neither follows. The start dates he is told are also wrong.

**The fix proposed.** Restate (1)'s consequence in §2 (lines 106-109) and §12 (lines 448-449). Under (1), facts 8 and 9 start on the party's own record: 2023-06-28 [KONF-P6], or 2023-06-25 if a candidate's words the party publishes count (state that reading). No F2 mark returns. Alternatively, state why [KONF-P6] and the 25 June post do not qualify, and what the strict form does to fact 9.

**The skeptic's evidence.** The staged record (git show :ElectionsData/poland/coalition_declarations_2023.md) says what happens under reading (1) ("a line starts only on words that name the party") in two places. §2, lines 106-109: "Konf → PiS starts on Mentzen's own post of 2023-07-06 [KONF-P2] ... and Konf → KO, as at §780, on TVN24's page of 2023-07-13 [KONF-I14], which names PO beside Tusk (F2-dated again)". §12 (1), lines 448-449 says the same. Two tests give that result, and neither agrees with the rest of the record.

1. Strict form: the F1 words must name the party themselves. On [KONF-I14] (CROSS/tvn24_2023-07-13_...html) the line toward KO is "Nie zamierzamy umożliwić powrotu Tuskowi do władzy". That names Tusk. PO appears only in the coalition sentence before it ("nie zamierzamy zawierać koalicji z PO"), which is not one of F1's forms. The record's own §5 (lines 266-270) refuses exactly this structure for [KO-P5]: "power is named, PiS is not ... beside it, '...przegonimy PiS.' is defeat". So on the strict form, [KONF-I14] cannot start fact 9.

2. Loose form: the party named beside the leader, which is how the record admits [KONF-I14]. Then [KONF-P6] (Konf/x_mentzen_1673974781958340610_xcom.html, 2023-06-28) qualifies earlier, on the party's own record. Its text: "koniec rządów PiS w Polsce. To będzie też koniec trwających już 18 lat rządów POPiS. Jeżeli te dwie partie się ze sobą nie dogadają..." and later in the same post "Chcemy odsunąć od władzy Kaczyńskiego i Tuska." §2 (lines 109-114) and the code comments already cite [KONF-P6] as an F1 restatement toward both. The header's first READING (lines 22-23) is that the own record dates a declaration it carries earlier, in whatever words. So both facts would start 2023-06-28 and no F2 mark would return.

3. The 25 June post, on either form. The party's own account published Płaczek's convention line on 2023-06-25 (out of tree: x_KONFEDERACJA__2023-06-25_1672921599907864576_fxtwitter.json): "Ja proponuję, panie prezesie, ... wywalmy przez okna wszystkie krzesła, żeby PiS i PO nigdy nie wróciło do władzy, jak ją stracą!" That is F1's "block its return" form, power named, both parties named. SOURCES.tsv row 76 notes it as "speaker not a leader; party published; ... block return of PiS and PO". The record sets it aside only as "Later, needed for no start" (line 168), which is true under the main reading but not under (1). The record has no rule excluding it by speaker:
   - §2 (lines 148-152) weighs the Wawer, Dziambor and Winnicki party-account posts on their content.
   - §11 (lines 355-356) says Wipler's words "count only as the party published them".
   - Reading (6) lets Winnicki's party-account words start fact 3.

4. Later own-record words name PO with power too (konfederacja.pl):
   - 2023-07-25, Tyszka: "jesienią trzeba odsunąć ich od władzy" (PiS and PO).
   - 2023-07-30, Mentzen: "Możemy pozbawić władzy zarówno PiS jak i Platformę".
   So even on the strict form, fact 9 would be dated by the party's own record, not by [KONF-I14].

Where the finding overreaches: [KONF-I14] could still start fact 9 under a third test. That test needs two criteria the record never states: (a) the party must be named in a pledge (not a forecast) in the sentence next to the leader's F1 words, which rules out [KONF-P6]; and (b) a candidate's convention proposal does not count as the party's declaration, which rules out 25 June. Criterion (a) also conflicts with how §5 treats [KO-P5].

Impact: the only effect is on what Elias is told, so this is minor. No polling-day line moves under any of these dates, and no game reader uses them (no Polish run-up is staged, so no round opens). But if Elias rules (1) as written, the expected follow-up (re-mark [KONF-I14] as (F2) and put PolandSpokenWords back on fact 9) would contradict the record's own "always wins" READING. Fact 8's 07-06 date holds only if the 25 June post is excluded.

**The skeptic's corrected fix.** Restate reading (1)'s consequence in §2 (lines 106-109) and §12 (1) (lines 448-449), first stating which form of (1) is meant.

(a) As applied to [KONF-I14] (party named beside the leader): Mentzen's own post of 2023-06-28 [KONF-P6] names PiS and POPiS ("te dwie partie") alongside "Chcemy odsunąć od władzy Kaczyńskiego i Tuska". So facts 8 and 9 start 2023-06-28 on the party's own record, facts 3 and 4 run to that day, and no F2 mark returns.

(b) Strict form (F1's words name the party, as §5 reads [KO-P5]):
- Fact 8 starts on [KONF-P2] 2023-07-06.
- Fact 9 cannot start on [KONF-I14], because its F1 words name Tusk and PO appears only in the coalition sentence. It starts on the party's own record instead: 2023-06-25 if a candidate's words the party publishes count (§11's Wipler reading; this would also start fact 8 that day), otherwise konfederacja.pl 2023-07-25 (Tyszka, "jesienią trzeba odsunąć ich od władzy") or 2023-07-30 (Mentzen, "Możemy pozbawić władzy zarówno PiS jak i Platformę", if read as an aim). None of these is F2-dated.

In both cases, delete "(F2-dated again)".

Re-label the 2023-06-25 item at line 168 so it no longer says "needed for no start", and weigh it explicitly (who is speaking, and whether a proposal is a pledge). Note that its words survive only in the fxtwitter relay; X's own status page would have to be fetched before it dates anything.

Alternatively, keep 07-06 and 07-13 but state the READINGs that exclude [KONF-P6] (party named in a forecast, not a pledge, and not next to the leader's words) and the 25 June post (a candidate's convention proposal is not the party's declaration). Then reconcile the "names PO beside Tusk" test with §5's refusal of [KO-P5], and say what the strict form does to fact 9.

### 2. §12 says 'no polling-day line changes under any reading below', but (5)'s keying changes two polling-day lines

- **Lens:** fixes - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:448

**The scenario.** Line 448 says 'no polling-day line changes under any reading below'. Six lines later (453-454), reading (5) says 'read as keying PSL and Lewica, the lines toward TD and NL would be F1's'. That turns facts 5 and 6 from cabinet-only into one-way, support-blocking lines that stand on 2023-10-15. It changes the set R1 holds line for line (PolishDeclarationsDiagnostic check (f)) and the §8 shapes. If Elias rules (5) relying on the bullet's summary, the game's polling-day lines move when he was told they would not.

**The fix proposed.** Qualify the sentence: no polling-day line changes except under (5)'s keying, which makes facts 5 and 6 one-way and support-blocking. If a formation claim is wanted, state whether any formation turns on it as measured, not asserted.

**The skeptic's evidence.** Both sentences are '+' lines in the staged diff (fix (e)). Staged ElectionsData/poland/coalition_declarations_2023.md:
- L448: "...and no polling-day line changes under any reading below."
- L453-454, reading (5): "a member party's account is the party's own - facts 3–7 start 2022-06-15 and facts 8 and 9 in 2022 (§2's member-party posts), and read as keying PSL and Lewica, the lines toward TD and NL would be F1's".
- L167, the words (5) keys on: Nowa Nadzieja quoting Mentzen, 2022-09-05, "Nie ma znaczenia czy rządzi PiS, PO, SLD czy PSL. … Należy ich odsunąć od władzy." This is F1's form, naming PSL and SLD. L33-34: such a pledge "is a one-way, support-blocking line".
- Nothing replaces those lines before polling day:
  - L407, doubt 4: the June opening "lifts nothing; the lines of 21 and 27 March run on through it".
  - The record's practice: cabinet-only restatements after an F1 line do not end it. Facts 8 and 9 stay Open through [KONF-P1] 2023-08-02 and [KONF-I25] 2023-10-11, listed as "The coalition half restated" (L128-133).
- §8 L315 holds facts 5-7 as "cabinet | false | false | 2023-03-21 | Open", and L325 says they stand on polling day. Under (5)'s keying, facts 5 and 6 stand on 2023-10-15 one-way and support-blocking instead.
- Code (staged DeclaredRedLines.cs): ForDateSourced adds every standing PairLine fact as its own line, `lines.Add(new RedLine(a, b, RedLineKind.Declared, blocksSupport: f.BlocksSupport, oneWay: f.OneWay, ...))`. A 2022 F1 fact on Konf->TD/NL would add a support-blocking line beside the cabinet one, not be replaced by it. PolandTimeline holds Konf->TD/NL as (false, false, D(2023,3,21), Open).
- The record's own use of the phrase covers shape. In the F2 bullet (L436-438), "no polling-day line changes" holds only because facts 5-9 restart in the same shapes.
- No formation turns on it, so the header at L420 ("no formation turns on any of them") stands. Facts 8 and 9 already refuse Konfederacja's support to any cabinet holding PiS or KO, and no cabinet without both can be formed. The game's lines and the code are unaffected. Only L448's blanket sentence is false, and it can mislead Elias when he rules on (5), so minor rather than defect.

**The skeptic's corrected fix.** Qualify L448 so it covers only the readings it is true of. Suggested wording: "...and no polling-day line changes under readings (1)-(4) and (6), nor under (5) as dates alone. Read as keying PSL and Lewica, (5) changes two lines: Konf -> TD and Konf -> NL (facts 5 and 6) would stand on polling day one-way and support-blocking, from 2022 (§2's member-party posts). Nothing later replaces them: the cabinet restatements of 2023-03-21 and 2023-08-02 leave facts 8 and 9 standing in the same way, and the June opening lifts nothing (doubt 4)."
If a formation claim is wanted, give the reason without transcribed figures: Konfederacja already refuses support to every cabinet holding PiS or KO (facts 8 and 9), and no cabinet without both can be formed. Alternatively, measure it as a named reading in PolishDeclarationsDiagnostic rather than asserting it. No code change is needed.

### 3. The RMF bullet says [KONF-I4]'s verbatim question changes nothing, but reading (2) hinges on it

- **Lens:** fixes - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:461

**The scenario.** Lines 461-462 say 'Since §784 the page dates no fact … so whether it is verbatim changes nothing.' Reading (2), at lines 450-451, restarts facts 5–7 on 2023-06-20 on [KONF-I4] '(F2-dated again; on 2023-08-02 [KONF-P1] if that page is not verbatim)'. If Elias rules (2), the verbatim question decides a six-week difference in facts 5–7 and whether [KONF-I4] carries F2's mark again. The bullet tells him it does not matter.

**The fix proposed.** Change it to: 'changes nothing on §2's readings; under (2) it decides whether facts 5–7 restart on 2023-06-20 [KONF-I4] (F2) or on 2023-08-02 [KONF-P1]'.

**The skeptic's evidence.** The finding is real: two bullets in the same §12 list ("Left to Elias") contradict each other in the staged file.

1. Lines 450-451, reading (2) under "Read the other way, each Elias's", says: "(2) the 27 March interest in ministries is an opening - facts 5–7 end 2023-03-27 and restart 2023-06-20 on RMF FM's page [KONF-I4] (F2-dated again; on 2023-08-02 [KONF-P1] if that page is not verbatim)". So under (2), whether the page is verbatim decides the restart date (2023-06-20 or 2023-08-02). It also decides whether [KONF-I4] carries F2's mark again, which would bring `PolandSpokenWords` back onto facts 5–7 in DeclaredRedLines.
2. Lines 461-462, the RMF bullet, says without qualification: "Since §784 the page dates no fact - Konfederacja's own record carries the line from 2023-03-21 [KONF-P4] - so whether it is verbatim changes nothing." §2 sends the reader to this bullet for the question: line 83 reads "[KONF-I4] (read as the words as said - §12)".
3. The staged diff made both edits. HEAD's bullet listed the consequences ("If not verbatim, the June lines go, and Konfederacja's lines start on its own record (2023-07-06, 2023-08-02) and TVN24's page (2023-07-13)"). The change cut that list down to "changes nothing" and, through fix (e), added reading (2), which depends on the same question. So this is a new inconsistency created by the first pass's answer, not a re-raise.
4. The sweep's own catalogue supports reading (2)'s side, so the RMF bullet is the one to qualify. In `Konf/own/SOURCES.tsv`, no own-record "z nikim" falls between 2023-03-27 and 2023-06-20. The party account's post of 2023-06-20 at 21:48Z ("Do wyborów nie idę, żeby usiąść do stolika z PiS-em, czy PO") names only PiS and PO and is classed "less-plain". The next "z nikim" on the own record is [KONF-P1] of 2023-08-02, as (2) says.

Why it is minor, not a defect:
- No code or game reader turns on these dates (lines 446-448: no Polish run-up is staged, `GameController.DeclaredPageAvailable`; no round opens; "no polling-day line changes under any reading below").
- No check parses this prose. The F2 check only matches register marks to fact tags.
- Reading (2)'s own parenthetical already gives Elias the dependency.
- On the installed (§2) readings, the RMF bullet's claim is literally true. Every §12 bullet states its consequence against the installed record, taking readings one at a time.

Even so, it is the only bullet about the verbatim question, it sits in the list of open questions put to Elias, and it tells him the question is moot when a listed reading turns on it. That contradicts the answer's own claim that "every reason was made consistent".

**The skeptic's corrected fix.** In ElectionsData/poland/coalition_declarations_2023.md, lines 461-462, scope the claim and name the dependency on reading (2). Replace "Since §784 the page dates no fact - Konfederacja's own record carries the line from 2023-03-21 [KONF-P4] - so whether it is verbatim changes nothing." with:

"Since §784 the page dates no fact on §2's READINGs - Konfederacja's own record carries the line from 2023-03-21 [KONF-P4] - so there whether it is verbatim changes nothing; under reading (2) above it decides whether facts 5–7 restart on 2023-06-20 [KONF-I4] (F2-dated again) or on 2023-08-02 [KONF-P1]."

Reading (2)'s parenthetical at lines 450-451 needs no change. It already matches the sweep's catalogue.

### 4. The 2023-01-11 post is called the party's 'own voice', but it quotes Winnicki

- **Lens:** fixes - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:152

**The scenario.** §2 lists 'in its own voice, 2023-01-11, "Konfederacja dąży do tego żeby zarówno PiS jak i PO odesłać do lamusa!"'. The file x_KONFEDERACJA__2023-01-11_1613099632258678785.json reads '🔹@RobertWinnicki: Nas interesuje reformowanie … Konfederacja dąży do tego …'. These are Winnicki's words, quoted by the account, and the sweep's own SOURCES.tsv row says 'party account quoting MP Robert Winnicki (RN president)'. Under the whose-record READING, a reader takes this as the party speaking for itself, the strongest class. It is in fact an MP quoted, the same class as the Wawer, Dziambor and Winnicki items beside it. The attribution also matters to reading (4), Rada Liderów members as leaders.

**The fix proposed.** Write 'quoting Winnicki, 2023-01-11, …'. The content reason (parties named, power and government not) stands.

**The skeptic's evidence.** The mislabel is real. Two of the finding's claimed consequences do not hold.

1. What the doc says. Staged coalition_declarations_2023.md, lines 152-154, under "**On the party's own account:**": "in its own voice, 2023-01-11, "Konfederacja dąży do tego żeby zarówno PiS jak i PO odesłać do lamusa!" - the parties named, power and government not (§4's READING; `x_KONFEDERACJA__2023-01-11_1613099632258678785.json`)". This text is new in the staged diff; HEAD has no such item.

2. What the post says. The cited file's "text" reads "🔹@RobertWinnicki: Nas interesuje reformowanie a nie uwłaszczanie się na państwie jak to od 7 lat robi PiS. Konfederacja dąży do tego żeby zarówno PiS jak i PO odesłać do lamusa!". Its user_mentions puts RobertWinnicki at indices [1,16], so the post is Winnicki quoted by the account.

3. The sweep's own catalogue agrees. The doc names SOURCES.tsv as the sweep's whole catalogue (lines 142-143). Row 72 of SOURCES.tsv says "party account quoting MP Robert Winnicki (RN president)".

4. The doc's own usage shows the label is wrong.
   - In the member-party group (lines 163-166), RN's "💬@krzysztofbosak: …" post (2022-06-15) is labelled "quoting Bosak".
   - RN's 2022-07-20 post has no speaker prefix, and the doc labels it "in its own voice". SOURCES.tsv row 81 calls it "Ruch Narodowy own voice (no Konfederacja attribution in the text)".
   - So "in its own voice" means an unattributed post. The 2023-01-11 post has the same "@Name: quote" form as Winnicki's 2022-09-05 post (".@RobertWinnicki: … Konfederacja powstała …"), and the doc attributes that one "Winnicki, 2022-09-05" (line 151).
   - The phrase contradicts both SOURCES.tsv and the doc's own pattern.

5. Overstated consequences. No date, line or reading moves.
   - **"The strongest class."** §2's whose-record READING (lines 144-147) counts the @KONFEDERACJA_ account as own record whoever it quotes. It sets no ranking between the account's own words and its quotes. The doc's own anchors are quotes: [KONF-P4] is "Współprzewodniczący @KONFEDERACJA_ @SlawomirMentzen: Z nikim …" and [KONF-P5] is "Współprzewodniczący @KONFEDERACJA_ @krzysztofbosak w @Graffiti_PN: …".
   - **Reading (4).** Reading (4) (line 452) is about a Rada Liderów member's own account before 2023-02-14, namely Bosak's. This post is on the party's account, so it is already own record under the main READING.
   - **Content rules it out under any speaker.** §4's READING (line 229) says "F1's forms name power or government", and "odesłać do lamusa" names neither. None of §12's readings (1)-(6) (lines 448-455) touches this post.

Severity: this is a false statement about who spoke, in a record whose subject is attribution, and it contradicts the catalogue it cites. It is not a defect, because it changes no outcome. Grade: minor.

**The skeptic's corrected fix.** On line 152, replace "in its own voice," with "Winnicki again,". This follows the group's speaker-first pattern (Wawer, Dziambor, Winnicki). "quoting Winnicki," would also be accurate. Keep the content reason ("the parties named, power and government not (§4's READING)") as it is; it is the operative reason. No date, §12 reading or diagnostic changes. Do not add text claiming the label mattered to reading (4) or to a whose-record class, because it did not.

### 5. Reading (4)'s start date rests on an unnamed-target reading that §5 refuses [KO-P5]

- **Lens:** fixes - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:452

**The scenario.** (4) says Bosak's own post of 2022-07-20 'starts fact 8 then, fact 3 goes', and §2 (line 161) calls it 'F1's form, toward Morawiecki's government'. Bosak's words ('Polską rządzą nieodpowiedzialni szkodnicy. Nie tylko trzeba ich odsunąć od władzy…') name neither PiS nor a leader. Only the quoted post by another user names 'Premier @MorawieckiM'. §5 and §780 hold KO's 'obalimy te filary złej władzy' [KO-P5] back because 'power is named, PiS is not'. The main READING keys a party only by a leader's name in the words. By the record's own standard, then, (4) alone does not move fact 8; an extra, unstated reading is needed. Also, the 2022-05-16 post ('Chętnie zagłosowałbym za odwołaniem premiera', which names the PM's office) is given no content reason for not being (4)'s start. No game date turns on it: both posts predate Poland's start (2023-02-19), and no reader acts before polling day.

**The fix proposed.** In (4), state the extra reading: 'those who rule Poland' keys PiS by context, which [KO-P5] is denied. Or make the two consistent. Give the 2022-05-16 post its content reason.

**The skeptic's evidence.** Bytes (out of tree, Konf/own/x_krzysztofbosak_2022-07-20_1549876718659407873.json): Bosak's own text is "Artykuł z marca. Wszyscy z branży wiedzieli jak to się skończy. Polską rządzą nieodpowiedzialni szkodnicy. Nie tylko trzeba ich odsunąć od władzy, ale wymienić w parlamencie. Potrzebujemy głębokiej wymiany kadr politycznych." It names neither PiS nor Morawiecki. "Premier @MorawieckiM wprowadził embargo…" appears only in quoted_tweet.text, from @RobertBe15. SOURCES.tsv row 40 identifies the target by context: "on Morawiecki's coal embargo … (plain end-PiS-rule)".

Staged record:
- §2, lines 160-162: "2022-07-20, \"Polską rządzą … od władzy, ale wymienić w parlamencie.\" - F1's form, toward Morawiecki's government".
- §12, line 452: "(4) … Bosak's own post of 2022-07-20 starts fact 8 then, fact 3 goes".
- §2's stated keying readings, lines 103-108: "a leader's name keys the party" and, read the other way, "a line starts only on words that name the party". Neither covers an unnamed description of the government of the day.
- §5, lines 267-269, holds [KO-P5] "obalimy te filary złej władzy" back as "power is named, PiS is not, and the pledge is against its pillars". COMPLETED.md §780 repeats it: "power named, PiS not, the pledge against its pillars - weighed as earlier and less plain".

One mitigation: KO's pillars clause partly separates it on form, because Bosak's "odsunąć od władzy" is F1's own verb. But the record never says the naming clause doesn't count. Even setting [KO-P5] aside, the main reading requires a name, and the 07-20 post has none. So (4)'s stated consequence rests on an unstated reading.

Without that reading, (4) does not land on 07-20. Bosak's other pre-2023-02-14 posts that name PiS are not in F1's forms: 2022-06-13 "…politycy PiS czy SP… Do wymiany!" and 2023-02-11 "Precz z nową komuną PiSu i Morawieckiego." So (4) either moves fact 8 nowhere (it stays at 2023-03-27) or moves it to 2022-05-16. That post, "Chętnie zagłosowałbym za odwołaniem premiera." (it names the office, @PremierRP; SOURCES.tsv calls it "less-plain"), is listed bare at line 160 with no item-level reason. That contradicts answer (c)'s "each item with its own reason".

No game effect:
- The staged DeclaredRedLines.cs never mentions these posts (a grep for 2022-07-20, szkodnicy, odwołaniem and Rada Liderów finds nothing).
- Both posts predate WorldClock.StartDate(Poland), which is CampaignCalendar.PreCampaignStart before 2023-10-15.
- No Polish run-up reader or round acts before polling day.
- "The line stands from Poland's opening day" holds for either 2022 date.

**The skeptic's corrected fix.** In §2's 2022-07-20 gloss and in §12's (4), state the reading the start rests on. For example: "READING, stated: words against those who rule Poland key the party of the government of the day (July 2022: Morawiecki's; Morawiecki's rule is PiS's). This goes a step beyond the leader's-name reading. The quoted post's 'Premier @MorawieckiM' is another user's words."

Then make §5 agree, in one of two ways:
- Say [KO-P5] is held back by its form ("against its pillars"). Under that reading its "złej władzy" would key PiS too.
- Or keep the naming clause and add to (4) that the reading is (4)'s condition. Without it, (4) moves no fact-8 start: 2022-06-13 "Do wymiany!" and 2023-02-11 "Precz z" are not F1's forms.

Give the 2022-05-16 post its content reason, as Wawer's post of the same day has one. For example: "a wish to vote on a motion, 'nie ma okazji' - no pledge or aim; the office, not a name". Otherwise say that under (4) and the incumbent reading it would start fact 8 instead of 2022-07-20. Doc-only: no code, check or polling-day line changes.

### 6. The party's own 2023-01-19 refusal of a coalition with Solidarna Polska is not weighed or offered as an alternative

- **Lens:** fixes - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:456

**The scenario.** The sweep holds x_KONFEDERACJA__2023-01-19_1616165254504890369.json. It is the party's own account carrying Mentzen in Polsat News: 'Absolutnie nie ma najmniejszej szansy na koalicje @KONFEDERACJA_ z @SolidarnaPL!'. The sweep's own note calls SP 'PiS's coalition partner', and the game's PiS key is the PiS list (PartySystems.PolandParties). Reading (5) keys member parties to their lists for PSL and Lewica. The same keying applied here makes this plain coalition refusal on the party's own record start fact 3 on 2023-01-19, standing from Poland's opening day. The post is not in §2's selection, though it is nearer than several items listed there. It is not among §12's (1)–(6), which the first-pass answer presents as every other reading.

**The fix proposed.** List the post in §2 with its reason (SP is not the PiS key). Add the list-member keying to §12's alternatives, with its consequence: fact 3 from 2023-01-19, no polling-day change.

**The skeptic's evidence.** I tried to refute this and could not. It is a gap in the record's own list of alternatives, with no effect on the game.

**The post is real and sits on the record the change adopts.**
- SOURCES.tsv line 29 lists `x_KONFEDERACJA__2023-01-19_1616165254504890369.json` with the note "(no coalition with Solidarna Polska, PiS's coalition partner)". Its SHA-256 matches (ac2c5601…).
- The JSON has `user.screen_name` KONFEDERACJA_, `created_at` 2023-01-19T20:06:38Z and `isEdited` false. Its text is "Absolutnie nie ma najmniejszej szansy na koalicje @KONFEDERACJA_ z @SolidarnaPL! … @SlawomirMentzen w @PolsatNewsPL".
- The staged §2 READING (lines 144-147) dates only the co-chairmen's own posts from 2023-02-14; the party's own account has no date limit. §2 weighs the 2022 posts on that account (Wawer, Dziambor, Winnicki) on their content, and (6) lets Winnicki's 2022-09-05 post date fact 3. So this post counts as the party's own record. Its only weakness is the target: Solidarna Polska is not a key.

**The record never weighs it.**
- A grep of the staged record for solidarn, ziobro, suwerenn and `\bSP\b` finds nothing.
- §2 (line 142) calls its list "a selection", but §12 (lines 455-456) says "§2 lists the nearest items".
- This post is at least as near as items §2 does list. One example is the 2023-06-25 convention line, which §2 marks as "needed for no start".

**The keying reading already exists, but only inside (5).**
- §12's (5) (lines 453-454) includes "read as keying PSL and Lewica, the lines toward TD and NL would be F1's". That keys a list by a member party's name on the target side, but only together with (5)'s premise that member parties' accounts count.
- (1)-(6) have no reading that applies the same keying to the party's own account.

**The PiS key's seats include Solidarna Polska's.** `PartySystem.cs:450` seats "PiS" at 194. `td_list_2023.md` says the KBW candidate file "sum[s] by committee to the PKW's notice: PiS 194". So Solidarna Polska members elected on the PiS list are inside those seats.

**What that keying would change.** Fact 3 (Konf → PiS, cabinet) would start on 2023-01-19 instead of 2023-03-21. Nothing in the record opens toward PiS between those dates, and fact 8 still replaces it on 2023-03-27. Poland's start is `WorldClock.StartDate` = `CampaignCalendar(2023-10-15).PreCampaignStart` (8 + 26 weeks earlier). That falls after 2023-01-19 and before 2023-03-21, so fact 3 would stand from Poland's opening day.

**Why only a note.**
- No game reader sees these dates. `LiveCampaignSetup.TryFor` stages Sweden and Germany only, and `DeclaredPageAvailable` needs `_liveCampaignOpen`.
- No line in force on polling day changes.

**What weakens the finding without refuting it.**
- §12's list was never literally exhaustive: Dziambor's 2022-07-27 "Nie jesteśmy ani z PiS, ani z PO!", read as a refusal, has no listed alternative either.
- In January 2023 "koalicja" with Solidarna Polska may have meant a joint electoral list, since the party was weighing leaving PiS's camp.

Both are reasons the record should state about this post, not reasons to leave it out.

**The skeptic's corrected fix.** **§2, under "Earlier, not read as the start" (party's own account):** add the 2023-01-19 post, with its file name and its reason. Suggested wording:

> Mentzen on Polsat News, 2023-01-19: "Absolutnie nie ma najmniejszej szansy na koalicje @KONFEDERACJA_ z @SolidarnaPL!" (`x_KONFEDERACJA__2023-01-19_1616165254504890369.json`). Solidarna Polska is no key: its members ran on the PiS list (the KBW candidate file the record already holds), but the record keys no list by a member's name on the target side.

Optionally add that in January 2023 the words may refuse a joint electoral list rather than a cabinet, and that the clip would settle it.

**§12, Konfederacja bullet:** add a reading (7), or lift (5)'s "keying PSL and Lewica" rider into a reading of its own. Suggested wording:

> A refusal of a party elected on a key's list keys that list. The party's own account's 2023-01-19 refusal of Solidarna Polska starts fact 3 then, before Poland's opening day, so it stands from the start. Facts 4-9 are unchanged, and no polling-day line changes.

Say "Poland's opening day", as (4) does. Do not write the date `WorldClock.StartDate` derives: the claim convention forbids transcribing it.

### 7. 'Toward KO, on the pages saved: only [KONF-P1] and [KONF-I25]' is now stale

- **Lens:** fixes - **reviewer:** note - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:132

**The scenario.** §2 says the coalition half toward KO is restated 'on the pages saved: only 2023-08-02 [KONF-P1] and 2023-10-11 [KONF-I25]'. The §784 sweep now saves konfederacja.pl's own page of 2023-07-30, out of tree and listed in SOURCES.tsv. In its body Mentzen writes: 'Wszyscy mnie pytają, z kim chcemy po wyborach tworzyć koalicję. Gdy odpowiadam, że ani z PiS ani z PO, …'. That is an own-record restatement toward KO earlier than [KONF-P1]. The 'only' now contradicts the sweep that §2 itself points readers to.

**The fix proposed.** Drop 'only', or scope it to 'on the pages the register lists', and cite the 30 July page as a further own-record restatement.

**The skeptic's evidence.** I could not refute it. The scenario happens exactly as described.

1. The claim. Staged `coalition_declarations_2023.md` line 132 reads: "toward KO, on the pages saved: only 2023-08-02 [KONF-P1] and 2023-10-11 [KONF-I25]; both held after the vote". The line itself is unchanged since s780 (cd1fb480). It sits inside "The coalition half restated", which follows the [KONF-I14] paragraph of 13 July.

2. The page that contradicts it. The §784 sweep saved `PoliSim-captures/sources/poland_declarations_2023/Konf/own/konfederacja.pl_2023-07-30_tak-sie-nie-da-oczywiscie-ze-sie-da-trzeba-tylko-chciec.html`.
- It is row 23 of `Konf/own/SOURCES.tsv`. That row gives sha256 723b884a…855f, which I recomputed and it matches. Its note reads: 'AFTER window; Mentzen text: coalition "ani z PiS ani z PO"'.
- JSON-LD gives datePublished 2023-07-30T14:50:03+00:00 and dateModified 15:06 the same day.
- The article body opens: "Sławomir Mentzen: Wszyscy mnie pytają, z kim chcemy po wyborach tworzyć koalicję. Gdy odpowiadam, że ani z PiS ani z PO, …". Later it says: "Możemy pozbawić władzy zarówno PiS jak i Platformę."
- So the co-chairman's words, on the party's own site, fall between 13 July and 2 August. That meets the header's "What counts" (line 16: "by the party, its leader … or published by the party as its own") and the staged READING of whose record counts (line 144: konfederacja.pl).
- The doc already lets PO stand for KO: [KONF-P1] says "z Platformą" and [KONF-P3] says "ani z PiS ani z PO". So this page is a coalition-half restatement toward KO, earlier than [KONF-P1].

3. Why "the pages saved" cannot be scoped to the register's tags.
- No text in the doc scopes it that way.
- The header (lines 6-7) calls the out-of-tree material "the rest of the sweep".
- Line 543 says "Every other saved file — … all out of tree".
- Staged lines 142-143 point readers to "the sweep's whole catalogue, each item with its file and digest, … Konf/own/SOURCES.tsv".
- The staged fetch_log says the sweep's pages are held "out of tree … listed with digests in its SOURCES.tsv".
- A reader who follows §2's own pointer therefore finds the 30 July page.

4. Why this change introduced it.
- The old sentence was accurate on pages that count. The pre-sweep out-of-tree pages that refuse PO after 13 July are Radio PiK of 2023-08-10 (Wipler, not a leader) and PCh24 of 2023-09-04 (a portal). Neither counts, and the toward-PiS list likewise leaves such pages out.
- The sweep added a counting own-record page, so this change made the "only" false.
- The change also rewrote the matching "Before June, no line on the pages saved" (HEAD line 109) for the same reason but missed this one.
- None of the first pass's answers (a)-(h) covers this line.

5. Severity re-grade.
- Not a defect. No fact, date or game reader turns on it: restatements date nothing; fact 9 is dated 2023-03-27 by [KONF-P5], or 2023-07-13 under §12 reading (1), and 30 July is later than both. The §12 literal-reading bullet that once leaned on KO's own-record dates no longer does.
- But it is a demonstrably false exhaustive claim in the record, contradicted by the catalogue the same change points to. It is also a transcribed derived claim ("only N on the pages saved") of the kind CLAUDE.md's claim convention says to reference rather than state. That makes it minor rather than a note.

**The skeptic's corrected fix.** At line 132, drop the exhaustive "on the pages saved: only". Scope the list to the register and cite the sweep's page by its out-of-tree file, as §2 already cites other out-of-tree items. For example: "toward KO, on the register's pages: 2023-08-02 [KONF-P1] and 2023-10-11 [KONF-I25], and on the party's own site, 2023-07-30, Mentzen: "Gdy odpowiadam, że ani z PiS ani z PO" (`konfederacja.pl_2023-07-30_tak-sie-nie-da-oczywiscie-ze-sie-da-trzeba-tylko-chciec.html`, out of tree; the rest in the sweep's `Konf/own/SOURCES.tsv`); both held after the vote - …". The "both" after the semicolon means both directions, so it still reads correctly. No code, fact row or date changes.

### 8. §1 reads the 'TVN24, PAP' credit as a relayed wire; §11 calls the same credit TVN24's own page

- **Lens:** fixes - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:58

**The scenario.** The new §1 text holds [PIS-I1] back because its page is credited 'Źródło: TVN24, PAP', and 'a relayed agency wire dates nothing'. §11 (line 362) describes [KO-I13], which carries the identical credit, as 'TVN24's own page'. Under the header's second READING an own page counts under F2. So the record gives one credit two opposite readings, while §12 leaves the question to Elias. Separately, line 58 quotes the credit unmarked, although its bytes split it across markup ('Źródło: </span>TVN24, PAP' and 'Źródło:</dt><dd …>TVN24, PAP'). The header requires such a quote to be marked *(decoded)*, and line 362 marks the same credit that way.

**The fix proposed.** Word §1 as the record's reading: 'read as PAP's wire relayed, which the header's reading says dates nothing; Elias's (§12)'. Drop 'own' from §11's [KO-I13] description, or note the same open question there. Mark line 58's quote *(decoded)*.

**The skeptic's evidence.** PART 1 (one credit, two readings) - CONFIRMED, introduced by this diff's new reading.
- Staged coalition_declarations_2023.md:57-59 (new §1): "it is not read as the start because of its page, credited "Źródło: TVN24, PAP" - a relayed agency wire dates nothing (the header's second READING)". §2:105-106 (new) says "as §1 reads [PIS-I1] too, which only its page holds back". Staged DeclaredRedLines.cs:418 (fact 2) says "the page, credited to TVN24 and PAP, dates nothing under F2 as the record reads it". So the record now reads that credit, by itself, as disqualifying.
- :362 (§11, unchanged since cd1fb480) says "Budka at KO's campaign launch, on TVN24's own page credited "Źródło: TVN24, PAP" *(decoded)*, 2023-08-09 [KO-I13]". Everywhere else in the record, "TVN24's own page" (:101, :122, :234, :408, :411, :427) names only pages credited to TVN24 alone: [KONF-I14] "Fakty po Południu TVN24", [NL-I13] "TVN24". The header (:23-24, :29-30) uses "own page" as F2's term for a page that counts. [KO-I12] ("tvn24.pl, PAP") is called only "live blog".
- In HEAD, §1 held [PIS-I1] back on the leader-name premise and §12 left the credit open, so §11's label sat beside an open question. The staged text now takes a side on the credit, which makes §11's label contradict it.
- The bytes would support a distinction, but the record never states one. [KO-I13] carries "Źródło wideo: TVN24" (TVN24 filmed the event); [PIS-I1] has no video credit. Neither §1 nor §11 cites this, and §1's stated reason is the credit alone.
- Inert: [KO-I13] is a restatement after KO's own start (§11 "set aside by the re-dating"), and [PIS-I1] stays out under either label, with §12:421 putting the credit to Elias. No fact, date, check or formation changes. Grade: note.

PART 2 (missing *(decoded)*) - real, but the finding's precedent is wrong and its fix is incomplete.
- [PIS-I1] (digest 72249382…81da, matches SHA256SUMS) never carries the credit contiguously: `Źródło:</dt><dd class="article-top-bar__meta-item">TVN24, PAP` and `Źródło: </span>TVN24, PAP`. The literal string has 0 hits.
- [KO-I13] (84290bb2…18ce) carries it plainly: `<div>Źródło: TVN24, PAP</div>`, 1 literal hit, an ordinary 0x20 space. So :362's mark is not needed and is no precedent. The real precedent is :458, "Opracowanie: Jan Matoga" *(decoded)*, a credit split by markup.
- The s780 review (Reviews/2026-10-05_s780_pl_declarations_f1f2f7.md:2078-2091, 2590, 2603) already offered this exact [PIS-I1] mark as optional ("not sentences"). The answer added marks only "where the bytes carry &nbsp;". New :58 repeats that unmarked choice. So does :421, a line this diff modified, which the finding misses.
- No code reads the mark (no "decoded" in Assets/Editor apart from MojibakeCheck's unrelated text).

**The skeptic's corrected fix.** 1. §11 :362: change "on TVN24's own page credited "Źródło: TVN24, PAP" *(decoded)*" to "on TVN24's page credited "Źródło: TVN24, PAP"". Drop "own", and drop the mark, since the bytes are plain. If the label is to stay, state the distinction instead: "TVN24's own video ("Źródło wideo: TVN24") on a page credited "Źródło: TVN24, PAP" - whether the page counts is §12's question for [PIS-I1] too".
2. §1 :57-58 (optional clarity): "it is not read as the start because of its page, credited "Źródło: TVN24, PAP" *(decoded)* - read as PAP's wire relayed, which the header's second READING says dates nothing; Elias's (§12) - and PiS's own page of the day [PIS-P1] does not carry the words".
3. Decoded mark (optional; s780 left this class optional): if :58 is marked, mark §12's :421 "Źródło: TVN24, PAP" *(decoded)* the same way, both lines being in this diff. That matches :458's "Opracowanie: Jan Matoga" *(decoded)*. Leave the register rows (:470, :538) unmarked, per the register's practice.

### 9. §12 implies [PIS-P1] has nothing on Tusk; it carries the Tusk passage in other words

- **Lens:** fixes - **reviewer:** note - **skeptic:** minor
- **Where:** ElectionsData/poland/coalition_declarations_2023.md:421

**The scenario.** Under the header's first READING ('always wins … in whatever words'), the question is whether PiS's own page carries the same declaration. [PIS-P1] (PiS/pis_wpjson_post_11056.json and the page) carries Kaczyński's Tusk passage from the same speech: 'Tusk jest prawdziwym wrogiem polskiego Narodu! Niech pójdzie sobie ze swoją polityką do Niemiec!'. It parallels [PIS-I1]'s 'Donald Tusk - prawdziwy wróg naszego narodu. … Ten człowiek nie może rządzić Polską. Ten człowiek powinien w końcu pójść do swoich Niemiec', minus the F1 sentence. §12 presents [PIS-P1] as carrying the Konfederacja line, 'not these words', and says 'only the page holds the start back'. Elias, confirming 'in whatever words', is not told that PiS's own record of the day has the passage. Nor is he told why it is not the same declaration: under §4's READING, no power is named.

**The fix proposed.** Say that [PIS-P1] carries the passage on Tusk but not its F1 sentence. Add that under §4's READING 'let him go to Germany' names no power, so the own page does not date the line.

**The skeptic's evidence.** The finding holds; I tried to refute it and could not. The quotes are right, and no other part of the staged record covers the gap.

1. PiS's own page has the Tusk passage. raw/declarations_2023/PiS/pis_wpjson_post_11056.json (WordPress date and modified both 2023-07-23T15:19) and the saved page pis_2023-07-23_stawiski-dobro-polski.html quote Kaczyński at Stawiski: "Tusk jest prawdziwym wrogiem polskiego Narodu! Niech pójdzie sobie ze swoją polityką do Niemiec! … Nie wierzcie we wspólne rządy PiS i Konfederacji!". On the HTML page these two sentences are plain bytes, with no entities.
   [PIS-I1] (tvn24_2023-07-23_…html) reports the same passage of the same speech: "Donald Tusk - prawdziwy wróg naszego narodu. Trzeba to jasno w końcu powiedzieć. Ten człowiek nie może rządzić Polską. Ten człowiek powinien w końcu pójść do swoich Niemiec i niech tam szkodzi". The own page has this passage without its F1 sentence ("Ten człowiek nie może rządzić Polską").

2. What the staged record says (scratchpad copy of `git show :ElectionsData/poland/coalition_declarations_2023.md`):
   - §1, lines 57-59 (new in this diff): "it is not read as the start because of its page … - and PiS's own page of the day [PIS-P1] does not carry the words (§12)."
   - §12, line 421 (new in this diff: "only the page holds the start back" and the "not searched" clause): "A pledge against a party's leader keys the party as §2 reads it …, so only the page holds the start back … PiS's own page of the day [PIS-P1] carries "Nie wierzcie we wspólne rządy PiS i Konfederacji!", not these words; whether PiS's own record carries earlier words against Tusk was not searched under this reading."
   - Searching the whole staged record for "wrog" or "Niemiec" finds nothing about [PIS-P1]'s Tusk passage. It is never weighed.

3. Why this is a gap and not just loose wording:
   - The header's first READING (lines 22-23) dates a declaration where the own record carries "the same declaration …, in whatever words". §1 and §12 test the own page by its words, not by its declaration.
   - Under §2's leader-keying READING (lines 103-106, new in this diff), the own page's Tusk passage now keys KO too. Only §4's READING (lines 229-230: "F1's forms name power or government …") stops PiS's own record dating PiS → KO on 2023-07-23, whatever is decided about the TVN24/PAP page. So "only the page holds the start back" holds only under §4's READING, and line 421 never cites it.
   - "Earlier words … not searched" suggests that the page of the day had nothing against Tusk. It does.
   - Everywhere else, §12 lists own-record words that §4's READING sets aside, with their consequence: KO → PiS's [KO-P5] ("read as F1's … would start it then", lines 422-423) and NL → PiS's "PiS trzeba pokonać" (lines 429-430). PiS → KO alone leaves its item out. This goes against the change's aim that every reason be made consistent.
   - Fact 2's code comment in staged DeclaredRedLines.cs holds [PIS-I1] back by its page and does not mention [PIS-P1].

4. Why the grade is minor, not a defect: no fact or date changes under the record's stated READINGs. Fact 2 stays at 2023-09-08, and 2023-07-23 and 2023-09-08 both fall before polling day (2023-10-15). No Polish run-up or round reads it (§12, lines 446-447). PolishDeclarationsDiagnostic has no check that a quote appears in the raw page, so quoting [PIS-P1] in the fix trips no check.

5. Why minor, not note: the misleading wording comes from this diff's own fix. It sits in a ruling put to Elias, on exactly the "in whatever words" READING he is asked to confirm. The fix is one clause.

**The skeptic's corrected fix.** Make two edits in coalition_declarations_2023.md, and one optional code-comment edit.

(1) §12, line 421:
- Change "so only the page holds the start back" to "so, on §4's READING of PiS's own page (below), only the page holds the start back".
- Replace "PiS's own page of the day [PIS-P1] carries "Nie wierzcie we wspólne rządy PiS i Konfederacji!", not these words;" with: "PiS's own page of the day [PIS-P1] carries the same passage on Tusk without its F1 sentence, "Tusk jest prawdziwym wrogiem polskiego Narodu! Niech pójdzie sobie ze swoją polityką do Niemiec!". It names no power or government, so under §4's READING it dates no line. Read as F1's, it would start PiS → KO on 2023-07-23 on PiS's own record, whatever the page; no polling-day line changes."
- Keep the "earlier words … not searched" clause.

(2) §1, lines 58-59: replace "and PiS's own page of the day [PIS-P1] does not carry the words (§12)" with "and PiS's own page of the day [PIS-P1] carries the passage on Tusk without that sentence ("Tusk jest prawdziwym wrogiem polskiego Narodu! Niech pójdzie sobie ze swoją polityką do Niemiec!"), which names no power (§4's READING; §12)".

The saved HTML page carries the quote as plain bytes, so it needs no *(decoded)* mark.

(3) Optional: in the fact 2 comment in DeclaredRedLines.cs, add "PiS's own page of the day [PIS-P1] carries the passage without that sentence (the record's §12)".

### 10. The bases of facts 5–7 state §2's ministries READING as fact

- **Lens:** fixes - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Elections/DeclaredRedLines.cs:424

**The scenario.** The bases of facts 5–7 (lines 424, 426, 428) say 'Bosak's interest in ministries on the party's own account, 2023-03-27, names no partner, so it replaces nothing (the record's §2)'. The record labels this 'READING, stated' (§2, line 84), and §12's (2) puts the other reading to Elias. Fact 8's basis labels its own reading ('READING, stated (the record's §2 and §12)'); these three do not. A basis is what a DECLARED slip shows as a fact's provenance, so the conclusion reads as ruled when it is still pending.

**The fix proposed.** Write '… names no partner - READING, stated (the record's §2; §12's (2)): so it replaces nothing'.

**The skeptic's evidence.** I could not refute the wording point. The finding's account of why it matters is overstated.

**What the staged code says.** Staged DeclaredRedLines.cs lines 424, 426 and 428 (facts 5, 6 and 7: Konf → TD, NL, MN) each carry, word for word: "Bosak's interest in ministries on the party's own account, 2023-03-27, names no partner, so it replaces nothing (the record's §2)." There is no READING label and no pointer to §12. This clause is new in this change: the '-' lines have no ministries clause.

**The record treats it as a reading, not a fact.**
- Staged coalition_declarations_2023.md line 84 (§2) labels the same inference: "READING, stated: an interest in ministries names no partner and no coalition, so it replaces nothing (§621)".
- §12, lines 448-451, puts the other reading to Elias: "Read the other way, each Elias's: … (2) the 27 March interest in ministries is an opening - facts 5–7 end 2023-03-27 and restart 2023-06-20 on RMF FM's page [KONF-I4] (F2-dated again …)".
- So "so it replaces nothing" is still pending. The bases state it as a settled conclusion.

**The code labels its other readings, including in these same strings.**
- The next sentence but one in the same three bases says "The 26 June opening … stands only on pages F2, as the record reads it, does not count".
- Fact 8 (line 430) says "READING, stated (the record's §2 and §12): a leader's name keys the party".
- Fact 9 (line 432) says "The premise, stated: … (K-1f's; the record's §2 and §12)".
- Fact 2 (line 418) says "as the record's §2 reads it … as the record reads it".
- The ministries clause is the one reading these bases leave unmarked.
- There is a precedent: the s780 review (Reviews/2026-10-05_s780_pl_declarations_f1f2f7.md:393, :424) confirmed the same class, "the diagnostic states the reading as fact", and fixed it with a "READ AS" label.

**Why it stays a note.**
- The finding claims the DECLARED slip shows the basis. Today it does not. The slip shows only the sentence, the date and SourceKeysOf(f.Basis), the bracketed keys (GameController.CampaignDeclared.cs:153-159). Its own summary says keys stand "until the slip carries the basis".
- Poland's DECLARED page is not reachable yet. DeclaredPageAvailable (CampaignDeclared.cs:34-35) needs a live campaign, and no Polish run-up is staged (§12 line 446).
- Facts 5–7 are cabinet-only, so CoalitionFormation.SupportBlockBasis never returns them.
- No behaviour changes and no date moves. The defect is provenance text only, which matches the change's own claim to have made "every reason consistent".

**The skeptic's corrected fix.** In each of facts 5, 6 and 7 (DeclaredRedLines.cs lines 424, 426, 428), label the reading the way fact 8 does. Replace "Bosak's interest in ministries on the party's own account, 2023-03-27, names no partner, so it replaces nothing (the record's §2)." with "Bosak's interest in ministries on the party's own account, 2023-03-27, names no partner and no coalition - READING, stated (the record's §2 and §12): it replaces nothing." This also brings back "and no coalition", which the record's §2 has and the basis drops.

Do not add a bracketed tag before [KONF-P4]. The F2, §8-row and register checks read the first tag.

In the item's report, drop or soften the finding's line about the slip: today the slip shows only the source keys, and Poland's DECLARED page is not reachable.

### 11. §12 says no reading changes a polling-day line, but its own reading (5) changes the shape of two polling-day lines

- **Lens:** code - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/poland/coalition_declarations_2023.md:448

**The scenario.** Line 448 says "no polling-day line changes under any reading below". Reading (5), at lines 453-454, says that when PSL and Lewica are taken to key TD and NL, "the lines toward TD and NL would be F1's". Nothing lifts those lines before the vote: the 26 June opening lifts nothing under F2 as the record reads it. So on 2023-10-15, facts 5 and 6 (Konf->TD, Konf->NL) would carry blocksSupport=true, oneWay=true instead of false/false. DeclarationReading.OfElection(Poland, 2023-10-15) and For(Poland, Poland2023) would then return different R1 lines: Konf would refuse to support any cabinet that holds TD or NL. Those lines feed PolishDeclarationsDiagnostic's R1 measurement, the Polish block of the FormationSweep pin and the game's polling-day formation. Elias would be ruling (5) after being told it only moves dates. Whether a government changes was not measured; I expect it does not, because Konf's support decides no R1 government.

**The fix proposed.** Take (5) out of the sentence. Suggested wording: "no polling-day line changes under (1)-(4) and (6); under (5), with PSL and Lewica keying TD and NL, facts 5 and 6 take F1's shape on polling day (not measured)". Alternatively, measure (5) and state the result.

**The skeptic's evidence.** I could not refute the finding. The contradiction sits inside one bullet of the staged ElectionsData/poland/coalition_declarations_2023.md.

- **The claim:** line 448 says "...and no polling-day line changes under any reading below."
- **The reading that breaks it:** lines 453-455, reading (5): "a member party's account is the party's own - facts 3–7 start 2022-06-15 and facts 8 and 9 in 2022 (§2's member-party posts), and read as keying PSL and Lewica, the lines toward TD and NL would be F1's". The posts behind this are in §2 at lines 165-168: Ruch Narodowy, 2022-07-20, "odsunąć Bandę Czworga od władzy", and Nowa Nadzieja, 2022-09-05, "Nie ma znaczenia czy rządzi PiS, PO, SLD czy PSL. … Należy ich odsunąć od władzy."

**Why those F1 lines would still stand on polling day:**
1. Section 8 (line 315) has facts 5-7, Konf → TD/NL/MN, as cabinet lines (blocksSupport false, oneWay false) from 2023-03-21 to Open. Line 325 says lines 5-11 stand on polling day.
2. Nothing in the record ends a 2022 F1 line toward TD or NL:
   - Doubt 4 (line 407) says the 26 June opening "lifts nothing".
   - The record never lets a later, weaker cabinet statement replace a stronger line. Facts 8 and 9 stay Open through the cabinet-only restatements of 2023-08-02 [KONF-P1] and 2023-10-11 [KONF-I25] ("the coalition half restated", lines 128-133). TD → PiS and KO → PiS are handled the same way.
   - Reading (5) itself starts facts 8 and 9 in 2022 and does not break them at 2023-03-21's "Z nikim". So that post would not replace the TD and NL lines either.
   - The out-of-tree SOURCES.tsv lists no member-party or own-record opening toward PSL or Lewica before the vote.
3. So under (5) with that keying, facts 5 and 6 would be blocksSupport=true and oneWay=true on 2023-10-15.

**The shape reaches the formation:** in the staged DeclaredRedLines.cs, `ForDateSourced` (line 493) builds each polling-day line with `blocksSupport: f.BlocksSupport, oneWay: f.OneWay`. `DeclarationReading.OfElection` (lines 672-676) reads the timeline on polling day. So the R1 line set changes.

**Why it stays minor:**
- Section 10 (lines 343-344) has only two derived lines, PiS–NL and NL–Konf, both support-blocking. The NL half therefore changes nothing in practice.
- Konf → TD would gain a support block it does not have now. But fact 9 already stops Konf from supporting any cabinet that holds KO, so this most likely moves no R1 government. That was not measured.
- Reading (5)'s own text states the F1 shape, so Elias is not fully misled. Still, the bullet's summary sentence contradicts it.

**Not refuted:** I found no reading of the record under which the 2022 F1 lines toward TD and NL end before polling day.

**The skeptic's corrected fix.** Change line 448 so it no longer covers (5)'s keying clause. Suggested wording: "...and no polling-day line changes under (1)–(4) and (6), nor under (5) as to dates; under (5) with PSL and Lewica keying TD and NL, facts 5 and 6 stand on polling day as F1's, one way and support-blocking, in place of cabinet lines (Konf → NL's support half is already drawn by the derived NL–Konf line, §10; Konf → TD's is new) - not measured." Alternatively, run reading (5) through PolishDeclarationsDiagnostic and print each government as a MEASURED line rather than writing it into the prose. The "Left to Elias" header's "no formation turns on any of them" should also be marked unmeasured for that clause, or measured.

### 12. The new both-ways F2 check fails without naming the page or fact that broke it

- **Lens:** code - **reviewer:** minor - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PolishDeclarationsDiagnostic.cs:128

**The scenario.** Example: '(F2)' is put back on [KONF-I4]'s publisher cell but no fact gets PolandSpokenWords. This can happen when building §12 reading (2), which F2-dates facts 5-7 again, and doing half of it. f2Pages is {KONF-I4} and f2Firsts is {}, so SetEquals is false. misdated stays empty, so the message has no 'NOT:' clause. The log only reads 'FAIL F2: ... (1 such pages, 0 facts); ...' and never names [KONF-I4]. The reverse case behaves the same way: a fact first-tagged by an unmarked I-page is named only through misdated.

**The fix proposed.** Add both differences to the NOT clause, for example: f2Pages.Except(f2Firsts) as 'marked, dates no F2 fact: …' and f2Firsts.Except(f2Pages) as 'first-tags an F2 fact, unmarked: …'.

**The skeptic's evidence.** I traced the failing path and could not refute it. The check's verdict is correct. The gap is only that the FAIL line does not say what failed.

- **Lines 99-104:** `f2Pages` is filled from any register row whose publisher cell contains "(F2)".
- **Lines 116-123:** `misdated` iterates `facts` only. A fact without `PolandSpokenWords` is held to its own [X-Pn] page (line 121). So a mark on a page that no F2 fact cites changes no fact's result.
- **Lines 126-127:** `f2Firsts` holds the first tags of the F2-marked facts.
- **Lines 128-130:** `f2Pages.SetEquals(f2Firsts)` was added to the condition, but the `{2}` NOT clause is still built from `misdated` alone. The line prints only `f2Pages.Count` and the count of F2-marked facts.

**Current state.** The staged register has no "(F2)" in any row (the only match is prose at record line 31). No fact concatenates `PolandSpokenWords`; it appears only at its declaration and in a doc comment. n784b.log line 548 reads "ok ... (0 such pages, 0 facts)" with no NOT clause, so `misdated` is empty.

**The finding's scenario.** Put back HEAD's "RMF24 (its own debate) (F2)" (HEAD record line 412) without marking any fact. Then `f2Pages` is {KONF-I4} and `f2Firsts` is empty, and the check prints "FAIL F2: ... (1 such pages, 0 facts); ..." without naming [KONF-I4].

**Corrections to the finding:**
- **The reverse case is named.** It does not "behave the same way". An F2-marked fact whose first tag is unmarked fails `f2Pages.Contains(tag)` at line 121, so it lands in `misdated`. `Line()` at line 342 then prints it as its party pair and shape.
- **The two first-tag regexes agree here.** Line 118 uses `([A-Z])` and line 127 uses `[A-Z]+`, so they could pick different first tags. They pick the same one, because every register tag is a P or I kind, and an unregistered tag fails check (b) by name (lines 88-92).

**Why the grade is note, not minor:**
1. **The first pass raised this exact gap.** Findings 7 and 12 in scratchpad `rev784_first.md` had "Optional: when SetEquals fails, add the pages from f2Pages.Except(f2Firsts) and f2Firsts.Except(f2Pages) to the NOT list. Today a stray register mark fails with counts only and names no page." Answer (g) took the main fix: pages and facts are printed apart. It did not take this option. That fix is not wrong.
2. **The verdict is never affected.** With zero marks expected, the stray mark is the only "(F2)" in the register, so one search of the record finds it.
3. **The same Check already had a count-only condition.** At HEAD the `f2Pages.All(... -I\d+$)` condition was count-only, and so is check (a)'s line 81. The file does not name every offender.

**What makes the path more likely now than at the first pass.** The first pass called the gap optional partly because "None of §12's own stated alternatives brings a mark back". After the answer, §12 (record lines 448-450) lists readings that do: (1) F2-dates fact 9 on [KONF-I14] again, and (2) F2-dates facts 5-7 on [KONF-I4] again. A half-built ruling that marks two pages but only fact 9 would print "(2 such pages, 1 facts)" and name neither page.

**The skeptic's corrected fix.** This is optional and the verdict does not change. Name the stray marks in the NOT clause. Fold in the I-page condition's offenders, which were count-only before this diff.

```csharp
var not = misdated
    .Concat(f2Pages.Except(f2Firsts).OrderBy(t => t, StringComparer.Ordinal).Select(t => "[" + t + "] marked (F2) but first-tags no F2 fact"))
    .Concat(f2Pages.Where(t => !Regex.IsMatch(t, @"^[A-Z]+-I\d+$")).OrderBy(t => t, StringComparer.Ordinal).Select(t => "[" + t + "] marked (F2) but not an I page"))
    .ToList();
```

Then pass `not.Count > 0 ? "; NOT: " + string.Join("; ", not) : string.Empty` as `{2}`.

The other direction, `f2Firsts.Except(f2Pages)`, is already named as facts through `misdated`. Adding its tags as well (e.g. "first-tags an F2 fact, unmarked: [T]") would only make the line read better.

Because this edits `Assets/Editor`, the PolishDeclarationsDiagnostic named run is owed again.

Otherwise, leave the code as it is and record in the §784 answer that first-pass findings 7 and 12's optional naming was declined.

### 13. Facts 4-9 say 'dated by its own record (F2)', reusing the register's F2 mark for the other meaning of F2

- **Lens:** code - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/DeclaredRedLines.cs:422

**The scenario.** The record header (lines 26-27) says "Since §784 no fact here is dated by F2". The register uses '(F2)' to mark a page that dates a fact under F2's spoken-words clause. The bases of facts 4-9 (lines 422-432) say "dated by its own record (F2)", meaning F2's 'always wins' clause. Fact 3 (line 420) and KO->PiS spell that meaning out. Read side by side, the C# literals contradict the header's wording. No check reads '(F2)' in a fact basis today, since the diagnostic scans only the register's publisher cell, so this is wording only.

**The fix proposed.** In facts 4-9, write "(F2's 'always wins')" as fact 3 and KO->PiS already do.

**The skeptic's evidence.** The text is as the finding describes. Nothing in the code fails; it is a wording problem only.

Staged DeclaredRedLines.cs (git show :path):
- Lines 396-397 are the doc comment, rewritten in this same diff: "The pages a Polish fact is dated by under F2 are marked (F2) in the record's register; every other Polish fact is dated by the party's own record (§621)." So the file defines "(F2)" as the mark for dating by spoken words, and cites §621 for dating by the party's own record.
- Line 420 (fact 3) spells the clause out: "dated by its own record (F2: a date from the party's own record always wins)".
- Line 444 (KO->PiS, unchanged) does the same: "Dated by KO's own record - F2: a date from the party's own record always wins".
- Lines 422, 424, 426, 428, 430 and 432 (facts 4-9) have only the bare "dated by its own record (F2)". A grep finds exactly these six.

Staged record, coalition_declarations_2023.md:
- Lines 26-32: "**Since `COMPLETED.md` §784 no fact here is dated by F2:** ... swept for F2's "always wins" ... Every fact is dated by its declarer's own record; the broadcasters' own pages ... date no fact here and carry no mark ... a page the register marks **(F2)**".
- No "(F2)" mark remains in the record body or the register, apart from that definition on line 31.
- At HEAD the record used "**(F2)**" to mark these same Konfederacja facts in the spoken-words sense (HEAD lines 27-28: "The facts F2 dates are marked **(F2)** here and in §8"; line 69: "from **2023-06-20** **(F2)**"; line 89). After this change, the six bases carry the same token in the opposite sense.

Attempted refutation, which only partly holds. Fact 3 has the same shape ("dated by its own record (F2...)"), so this is not a strict logical contradiction. The words "dated by its own record" can only refer to F2's third sentence, and the header itself uses "F2's 'always wins'" on the next line. What remains is a real but minor problem: the abbreviation is inconsistent (2 forms spelled out, 6 bare), and the bare token matches the "(F2)" mark that the same file defines 25 lines above in this same diff. That is at odds with the answer's claim that "every reason was made consistent".

No reader of this token exists, so it stays a note:
- PolishDeclarationsDiagnostic.cs line 103 looks for "(F2)" only in the register's publisher cell (Groups[3]).
- Fact bases are tested only for PolandSpokenWords (lines 121 and 126), the "DECLARED (F1" prefix (line 133) and the tags (lines 79 and 88).
- GameController.CampaignDeclared.SourceKeysOf pulls out only [TAG] keys.
- A grep of Assets/ and Tools/ finds no other reader of "(F2)" in a basis.

**The skeptic's corrected fix.** In the six literals on staged DeclaredRedLines.cs lines 422, 424, 426, 428, 430 and 432, replace "(F2)" with fact 3's exact wording, "(F2: a date from the party's own record always wins)". Alternatively, to match the doc comment on lines 396-397, use "(§621; F2's \"always wins\")". Keep the colon that follows in facts 8-9. No check or table cell repeats these words: §8's basis cell holds only tags, and PolishDeclarationsDiagnostic reads only the tags, the "DECLARED (F1" prefix and PolandSpokenWords. So the edit cannot break PolishDeclarationsDiagnostic or the F2 both-ways check.

## The second pass - refuted by the skeptics

- [fixes] The whose-record READING names two member parties; the record's own TVN24 page names three - *Refuted. The facts it cites are true; the conclusion it draws from them is not.

What the finding gets right:
- Staged record line 146 does read: "a member party's account - Ruch Narodowy's, Nowa Nadzieja's - is not part of it".
- The [KONF-I14] raw page (CROSS/tvn24_2023-07-13_pis-nie-wykluczaja-koalicji-z-konfederacja.html) does say: "Konfederację tworzą trzy ugrupowania: narodowcy, korwiniści i przedstawiciele Konfederacji Korony Polskiej".
- Konf/own/SOURCES.tsv has no row from a KKP account. But SOURCES.tsv lists only the pages that were saved, not every account that was read.

What fails is "reading (5) rests on two of the three member parties, unstated". The sweep did read KKP's account:
- The sweep's own report (session tasks/wvow2fxx6.output, agent search:bosak-party) lists "the member parties @RuchNarodowy, @Nowa_Nadzieja_ (... @Partia_KORWiN ...) and @KoronyPolskiej".
- The same report gives "In-window ids: ... KoronyPolskiej 264" and "Unique in-window posts read: ... @KoronyPolskiej: 215".
- Its verifier states reading (5) as "IF member-party accounts (RN, NN, KKP) are ruled to be Konfederacja's own record".
- The scratch files confirm it:
  - scratchpad/konf_bosakx/cdx_KoronyPolskiej_tw.txt and cdx_KoronyPolskiej_x.txt exist.
  - batch3_dump.tsv holds 213 decoded KKP posts, and 218 unique together with konf_dump and embedded_dump (2022-01 to 2023-06).
  - A keyword scan of them (koalicj, odsun, władz, stolik, przystawk, Tusk, rząd) finds no coalition refusal and no "keep X from power" line, only policy attacks such as 2023-01-12 "Rząd @pisorgpl i @SolidarnaPL chce wprowadzić...".

So fetch_log line 57's "of its member parties' accounts" is accurate for RN, NN and KKP. Reading (5)'s consequences (lines 453-454) rest on all three accounts that were read; KKP's produced nothing to keep. There is no KKP gap to state.

Line 146's rule is general and excludes KKP's account in its own words. Its two names are the accounts whose posts §2 then quotes (line 163, "The member parties' accounts: Ruch Narodowy's ... Nowa Nadzieja's"), and line 142 marks that list "a selection" that points to SOURCES.tsv.

No game reader is touched:
- WorldClock.StartDate's default is CampaignCalendar(LatestElectionDay).PreCampaignStart, the standard run-up before 2023-10-15, so every reading-(5) date falls before the start.
- §12 (lines 446-448) says no page or round reads these dates.

Aside, not this finding: the sweep's copy of Bosak's own post 1535712724000415744 (2022-06-11T19:57Z) thanks RN's "koalicjantów @KoronyPolskiej @Partia_KORWiN @wolnosciowcy". That makes Wolnościowcy a fourth partner in 2022. The sweep has no CDX query and no decoded row for @wolnosciowcy, so the finding's "name all three" would itself be wrong for 2022.*
- [fixes] The fetch_log names archive.ph listings as a source of post ids; no catalogue row has one - *The finding's literal observation holds: no row of G:/UNITY/Projects/PoliSim-captures/sources/poland_declarations_2023/Konf/own/SOURCES.tsv names archive.ph, archive.today or archive.is (grep count 0). The only mention anywhere in the tree or the sweep folder is staged fetch_log.md line 58. The rest of the finding does not hold.

1. The premise is false. The finding says "Every row of Konf/own/SOURCES.tsv records where its id came from." Thirteen X-post rows record no id source at all: file lines 28-34 and 39-44. For example, line 33, which is [KONF-P5]'s own post 1640262039753969664, reads only "syndication fetch 2026-10-05; co-chair Bosak in Graffiti ...". So the catalogue cannot show which discovery routes produced the posts.

2. The fetch_log's clause is true. Line 57-59 says "post ids from the Internet Archive's index of their status URLs, from archive.ph's listings and from the pages that embed posts, each post's text and `created_at` from X's syndication endpoint". That describes how the sweep found the posts it READ ("340 read"), not the kept rows. The sweep's own transcripts (wf_2f22824c-b05) show the archive.ph listings were read and supplied ids:
- Agent a317d946026352e0b's final report: "(3) archive.ph prefix listings: krzysztofbosak 100 of 150 entries (pages 100-140 refused with 429); KONFEDERACJA_ twitter.com 62/62 and x.com 77/77; Bosak x.com 67/67". Its aph_ids.pl run flagged two new in-window Bosak ids, 1639374309167603712 and 1645149652516319238.
- Agent aff44d444d1852432: "(3) archive.ph prefix listings for twitter.com/x.com/SlawomirMentzen*: 8 pages, 3 new in-window ids". These were 1646088253387202565, 1667181127503601669 and 1668929012847067138, fetched through syndication (fetchlog_archiveph.tsv, "3 200").

3. None of those five ids that only archive.ph surfaced was kept (0 SOURCES.tsv rows each). The 13 rows with no id source came from a767d21cb3457d185, which made 0 archive.ph requests and 16 Wayback CDX requests. It found 1640262039753969664 at 14:37:45Z. The two Bosak rows (lines 39-40) were in a317's bosak_hits.tsv (transcript line 181) before its first archive.ph call (line 191). So no kept id is attributed to archive.ph anywhere, and none came only from it.

4. The auditor scenario has no failing path. The record's register rows (staged coalition_declarations_2023.md lines 526-528) evidence each cited post by its own syndication URL or X status page, its created_at and its SHA-256, with the file held in the tree. They claim no discovery route. A post id carries its own proof: the endpoint's JSON gives user.screen_name and created_at. So how an id was found never enters the evidence for a cited fact. The fetch_log ties no cited post to archive.ph.

5. The fix is wrong. "Drop archive.ph" would make the fetch_log understate a route the sweep really used and read posts from. "Say it yielded no kept id" is optional precision, owed no more than for any other route (the CDX index rows were not kept either).*
- [code] One check now finds a fact's 'first tag' with two different regexes - *The regexes are as quoted. Staged PolishDeclarationsDiagnostic.cs:118 uses `\[(([A-Z]+)-([A-Z])\d+)\]` and :79 uses `\[[A-Z]+-[A-Z]\d+\]`, which take one letter after the hyphen. :127 uses `\[([A-Z]+-[A-Z]+\d+)\]`, the same multi-letter form as the register regexes at :84, :88, :90, :100 and :140. The new line follows the register. The single-letter lines are the older ones.

1. There is no failing path today. :126 keeps only `facts.Where(f => f.Basis.Contains(DeclaredRedLines.PolandSpokenWords))`. In the staged DeclaredRedLines.cs, `PolandSpokenWords` appears only where it is declared (398) and in a doc comment (406). None of the 15 PolandTimeline facts (416-444) carries it; HEAD had 6. The staged register has no row with "(F2)" in column 3; HEAD had KONF-I4 and KONF-I14. The record's own text agrees: "Since COMPLETED.md §784 no fact here is dated by F2 ... there is none". So f2Firsts and f2Pages are both empty, and the regex at :127 runs on zero facts.

2. The trigger does not exist in Polish data. All 72 register rows have suffix I (42) or P (30). All 36 distinct tags cited in PolandTimeline are single-letter. The multi-letter tags in DeclaredRedLines.cs ([CDU-PT31], [CSU-PV23], [CSU-PT24], [BT-DHB65], [ZDF-AFD24], [GR-BDK24]) are all in GermanyTimeline (361-389), which this diagnostic never reads.

3. In the hypothetical the FAIL is the right verdict. Take an F2 fact first-tagged [X-IA1]. :127 puts X-IA1 into f2Firsts. Either it is not in f2Pages, so SetEquals fails, or it is, and then it fails `^[A-Z]+-I\d+$` at :128. The check's own rule requires an F2 fact to be first-tagged by an (F2) page, and every (F2) page has the form [X-In], so failing here is correct. :127 is the regex that sees the true first tag; :118 is the one that would skip it. The finding agrees there is no false pass.

4. The stated consequence is false. `Line` (:342) is `f.Party + " -> " + f.Other + (OneWay/BlocksSupport words)` and never prints a tag. The message at :129-130 prints f2Pages.Count, the count of F2 facts and the misdated facts by Line. It names no page, so it cannot point at the wrong one.

5. The fix as worded could break the check. If the regexes were unified on the single-letter form (:79/:118), f2Firsts would skip [X-IA1] just as misdated does. Then, with [X-I14] marked (F2) and correctly dated later in the same basis, SetEquals, the I-regex and misdated would all pass. That is a false pass the staged code does not have. Only unifying on the multi-letter form is safe.*
- [code] Fact 8's basis states '(the model has no prime minister)', a claim about the code that the code contradicts - *REFUTED. The parenthetical is accurate, and the code does not contradict it.

1. The wording is already in the code at HEAD, in the very places the finding cites as evidence against it.
- GovernmentRecord.cs:28 is the class that defines PmParty (evidence 2). Its doc says: "<b>The prime minister's party is a premise, stated.</b> The model carries no prime minister. Where the record names the head of government, its party is read from the record (Kristersson (M)); where the formation formed the cabinet, the party of a declared own-leader candidacy ... leads it (K-1f's premise), else the largest cabinet party."
- PmParty's own summary is "The prime minister's party by key - the premise above."
- DeclaredRedLines.cs:146 is the K-1f doc where candidacies are encoded (evidence 3). It says: "The model carries no prime minister, so it is encoded under ONE premise..."
- HEAD's record already had the KO line's "(K-1f's premise; the model has no prime minister)" at lines 94-95, now staged 127-128. COMPLETED.md §607 (line 33651) and §628 (line 34178) record the same premise as built.
- So the Head label, PmParty and Candidacy were all built under that sentence. The change only copies the same parenthetical to the PiS half.

2. What the code actually holds:
- WorldClock.cs:379's GovernmentOfRecord.Head is a label string, "Mateusz Morawiecki (PiS)".
- GovernmentRecord.HeadParty (GovernmentRecord.cs:309-316) only parses the "(PiS)" key out of that label.
- Cabinets, PmParty, StandingRefusals ("MOVER>PM" by party key) and every DatedFact pair are party keys. No rule keys a person.
- So Bosak's "kontynuacji władzy Morawieckiego" has to be read onto a party key. That is exactly what "the premise, stated: Morawiecki's rule is PiS's (the model has no prime minister)" says.

3. The claimed effect on Elias does not happen.
- What §12 puts to him is not whether Morawiecki was PiS's prime minister on 2023-03-27. The record says that itself: §2 line 104 ("the PiS half names the prime minister's rule") and §1 line 53 ("Morawiecki, then prime minister").
- The question is whether refusing one man's continued power is F1's line against any post-election cabinet that includes PiS. Under F1 that means joined or supported, including a PiS cabinet under another head or one with PiS as a junior partner.
- The government of record cannot settle that question. Its 2019-2023 cabinet party is itself DERIVED (cabinetDerived: true, gap G6).
- The difference between the PiS and KO halves is spelled out in the record ("the PiS half names the prime minister's rule ... and the KO half Tusk's return"). §12's alternative (1) gives the consequence. Nothing is hidden.

4. No player sees this text. The declared page draws only SourceKeysOf(f.Basis) (GameController.CampaignDeclared.cs:153), and Poland's page is not available yet.

5. The proposed fix would make things worse. Writing "on that date the government of record is Morawiecki's (PiS) - WorldClock.Governments" into the basis and the record copies a fact from the code's data. Under the claim convention that kind of DERIVED claim must be referenced or generated, not transcribed.*
- [code] DocumentClaimCheck, one of n784b's four checks, read none of this change's documents - *THE FACTS ARE ACCURATE. DocumentClaimCheck.cs:124 reads `Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly)`, and its Historical set (:82-85) skips COMPLETED.md and CLAUDE.md. Only two staged .md files exist, and neither is in the scan: ElectionsData/poland/coalition_declarations_2023.md and ElectionsData/poland/raw/declarations_2023/fetch_log.md. So n784b's DocumentClaimCheck read neither.

THERE IS NO FAILING PATH, for five reasons.

(1) NO STALE REFERENCE. I resolved every backticked `Type.Member` in the staged record using the check's own rule (the type declared once, the member present). All of them resolve:
- DeclaredRedLines.IsSourced :37, .FactKind :307, .PolandSpokenWords :398, .PolandTimeline :413, .HasTimeline :452
- DerivedRedLines (CoalitionFormation.cs:928): .IdeologicalGap, .SocialGap, .From
- PartySystems.PolandParties (PartySystem.cs:448)
- WorldClock.StartDate (WorldClock.cs:223)
- GovernmentFormation.ViewOf (:190, :201, :213)
- LiveCampaignSetup (LiveCampaignSetup.cs:39)
fetch_log.md carries only file names, which FileExtensions excludes.

(2) NOTHING CLAIMS THE COVERAGE. The staged record says only that `PolishDeclarationsDiagnostic` holds it (staged lines 8, 30, 343, 378, 394). No line says DocumentClaimCheck reads it. §784 is not written yet. The record's practice for named runs is a plain listing, for example COMPLETED.md:37353: "Named runs nf1a, n780a, n780b and n780c (seven checks, ...): clean".

(3) THE RUN IS NOT EMPTY FOR THIS CHANGE. The check indexes the edited .cs files (:114-121) and holds the root live documents to them. POLISIM_FEATURE_LIST.md:50 and :62 cite `DeclaredRedLines.ForDate`, which is at DeclaredRedLines.cs:556. The staged edit changes only string literals, one doc comment and a local `var f2Firsts`. The check's member index of DeclaredRedLines.cs is byte-identical at HEAD and in the index (diffed with the check's MemberDeclaration regex).

(4) WIDENING THE SCOPE WOULD NOT CHECK THE CHANGE'S NEW REFERENCE. The added lines carry two code references:
- `DeclaredRedLines.PolandSpokenWords` resolves.
- `GameController.DeclaredPageAvailable` names a partial class declared in 30 files under Assets/Scripts/UI. Line 160 (`if (declaring.Count > 1) { ambiguous++; continue; }`) would skip it as AMBIGUOUS.

(5) THE GAP IS NOT NEW. The root-only scope predates this change and is already on record:
- Reviews s772:608/:634: "The same root-only gap since §579 applies to DocumentClaimCheck's claim scan"
- s776:257
- s780:2167: "DocumentClaimCheck reads only root *.md and Assets *.cs ... Neither reads this file, which is why the named run passed"
§772 widened only MojibakeCheck to docs/**/*.md (COMPLETED.md:36890).

Minor slip in the finding: Historical also excludes CLAUDE.md, not only COMPLETED.md.*
- [code] bar_tier classes this change as SIMULATION, which owes both full bars; n784b ran four checks - *The finding's facts are mostly right, but they describe a step that is not due yet. Nothing is wrong in the staged diff.

1. The tier is right. `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/bar_tier.ps1 -Staged` prints "BAR TIER: SIMULATION ... per item : CheckSuite.RunAllBatch (the cheap bar) + CheckSuite.RunSimulationBatch (the simulation bar, TrajectorySentinelCheck first)".
   - "ElectionsData/** as SIMULATION" is loose. bar_tier.ps1:54 (`if ($p -match '\.md$') { $docs += $p; continue }`) runs before :56, so coalition_declarations_2023.md and fetch_log.md are documents.
   - PolishDeclarationsDiagnostic.cs counts as tooling.
   - The simulation paths are DeclaredRedLines.cs, the three raw pages and SHA256SUMS.txt.

2. The bars come after the review in the repo's order, and that order is followed every time.
   - CLAUDE.md:8 (step 4) says "Before every commit, ... bar_tier.ps1 -Staged, and run what it says the commit owes".
   - CLAUDE.md:71 (THE LOOP) says: edit, then a TARGETED check (CheckSuite.RunNamedBatch), then edit again; "At the item's close, CHEAPEST-FAILING FIRST: cheap bar -> ... -> the simulation bar".
   - n784b is that targeted check. Logs/bar_timing.tsv shows it at 2026-10-05T18:04:59Z running exactly PolishDeclarationsDiagnostic, DeclarationDatesDiagnostic, FormationSweepDiagnostic and DocumentClaimCheck, all exit 0. No cheap or simulation bar has run since HEAD 7d71fcc4 was committed (17:04:02Z).
   - Every commit today had both bars just before it (cheap, simulation, commit, all UTC):
     - s778: 10:01:58, 10:14:16, 10:19:47
     - s779: 11:06:00, 11:18:11, 11:18:36
     - s780: 14:11:19, 14:24:16, 14:29:04
     - s782: 14:33:59, 14:52:08, 14:55:02
     - s783: 16:48:17, 17:00:27, 17:04:02
   - Nothing staged claims a bar ran. A search of the staged doc hunks for n784/bar/sentinel/NamedBatch finds nothing, and COMPLETED.md §784 is not written yet.
   - So the scenario "the commit lands having run only n784b" is not on the path. The process already covers it, and the finding's fix is word for word CLAUDE.md step 4.

3. The two cheap checks the finding names would find nothing new in this change.
   - CommentClaimCheck only matches backticked `Type.Member` in comments (`new Regex(@"`([A-Z][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)`")`). The new §784 comment in PolishDeclarationsDiagnostic.cs and the rewrapped summary in DeclaredRedLines.cs have no backticks.
   - MojibakeCheck reads root and docs *.md, Assets/**/*.cs and Tools/**. It does not read ElectionsData/. I applied its rule to the staged blobs (`git show :path`): DeclaredRedLines.cs has 0 suspects and PolishDeclarationsDiagnostic.cs has 0. The only Latin-1-range Polish letter is a lone ó (0xF3), which does not decode on its own.

4. The sentinel is owed by the simulation bar before the commit. That is the process, not a fault in the diff. As a side note, the reasoning agrees: every DatedFact keeps its position and flags, so the active Konf set is identical on any date from 2023-07-13 on, including the polling day 2023-10-15 (WorldClock.cs:89).*


## What the author did about the second pass

- **1** (reading (1)'s consequence was wrong) - restated in §2 and §12: under "a line starts only on words that name the party" both lines start later on the party's OWN record either way, so no F2 mark returns - on Mentzen's own post of 2023-06-28 [KONF-P6] with the party named beside the leader (it names PiS's rule and the two parties' beside "Chcemy odsunąć od władzy Kaczyńskiego i Tuska"); read strictly, fact 8 on [KONF-P2] (2023-07-06) and fact 9 on the party's own site, 2023-07-30 ("Możemy pozbawić władzy zarówno PiS jak i Platformę", if read as an aim); or both on the convention words of 24 June if a candidate's words the party publishes count. The convention line is re-labelled: weighed only under (1), its words standing in a relay - X's own status page would be fetched before it dated anything.
- **2, 11** ("no polling-day line changes under any reading") - scoped: none under readings (1) to (4), (6) and (7), nor under (5) as dates alone; read as keying PSL and Lewica, (5) makes the lines toward TD and NL one-way and support-blocking on polling day - said so.
- **3** ([KONF-I4]'s verbatim doubt "changes nothing") - scoped to §2's READINGs; under reading (2) it decides whether facts 5 to 7 restart on 2023-06-20 or 2023-08-02.
- **4** (the 2023-01-11 post called the party's own voice) - "Winnicki again".
- **5** (reading (4)'s start rested on an unstated reading) - §2's gloss of Bosak's 2022-07-20 post states the further READING it needs (words against those who rule Poland key the governing party, a step beyond a leader's name; §5 refuses [KO-P5] power named without the party), and (4) says that without it (4) moves no start; the 2022-05-16 post has its own reason (a wish to vote on a motion there was no occasion for - no pledge or aim, the office and not a name).
- **6** (the party's own 2023-01-19 refusal of Solidarna Polska unweighed) - listed in §2 (Solidarna Polska is no key of the roster, so it keys no line) and offered as reading (7): a refusal of a party that stood within a key's list keys that list - stated as resting on a list composition this record does not hold.
- **7** ("toward KO, on the pages saved: only …" stale) - scoped to the register's pages, with [KONF-P4] added and the party's own site of 2023-07-30 cited out of tree.
- **8** (one credit read two ways) - §1 marks the credit *(decoded)* and reads it as PAP's wire relayed, Elias's (§12); §11 says "TVN24's page", not its own page.
- **9** ([PIS-P1] said to carry nothing on Tusk) - §1, §12 and fact 2 now say PiS's own page of the day carries the Tusk passage without its F1 sentence ("Tusk jest prawdziwym wrogiem polskiego Narodu! …"), which names no power, so under §4's READING it dates no line; read as F1's, it would start PiS → KO on 2023-07-23 on PiS's own record - no polling-day line changes.
- **10** (facts 5 to 7 stated the ministries READING as fact) - labelled "READING, stated (the record's §2 and §12)", with "and no coalition" back.
- **12** (the both-ways check failed without naming the page) - its NOT clause names the facts misdated, a mark on a page that first-tags no F2 fact, and a mark on a page that is not a broadcaster's.
- **13** ("dated by its own record (F2)" reused the register's mark) - every fact says "(F2: a date from the party's own record always wins)".

Named run `n784c` after the fixes (the same four checks): 4 of 4 clean.
