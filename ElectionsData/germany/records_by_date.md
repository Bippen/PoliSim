# Germany — chambers, governments and party leaders of record by date, 2022-07-01 → 2026-09-24 [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; PS-1 the world clock, `docs/specs/POLITICAL_SYSTEM_SPEC.md` §9 stage 1 and §11;
sourcing pass 2026-09-24, research agent). `[PROVISIONAL]` until a second session re-verifies (R-K9).

**What this file is for.** The game seats every country in its chamber of record and its government of record on the
chosen start date (§3). For Germany this file gives the three dated tables the spec's §11 bills — the chamber, the
government and the seated parties' leaders, each with from/to dates and the event at every boundary — and the
constitutional rules §5.4 names (Art. 63, 67, 68 GG), quoted from gesetze-im-internet.de. Seats for the 2025 chamber
are `returns_2025.md`'s (not repeated here); the 2021 chamber's seat table is given below with its source.

**Fetched, never recalled.** Every quote below was read in a page fetched on 2026-09-24 (afternoon–evening UTC; the
per-file times are in `raw/records/fetch_log.txt`) and saved byte for byte under `raw/records/`. Each file's SHA-256 is
in `raw/records/SHA256SUMS.txt` and in the register below (the register's bytes and hashes were emitted by a script from
the files on disk, not typed). Quotes are German and verbatim; they were checked against the saved bytes by grep after
tags were stripped, entities decoded and whitespace collapsed — so a quote may differ from the bytes only in
whitespace and in `&#223;`-style entities rendered as their characters. Two files are PDFs whose text was read by
inflating their FlateDecode streams; their quotes are marked. English glosses are marked *gloss:* and are mine. Lines
marked **DERIVED** state their arithmetic or reading. Party keys are the project's (`PartySystem.cs`, Germany): `CDU`,
`CSU`, `SPD`, `Grune`, `Linke`, `FDP`, `AfD`, `SSW`, `BSW`.

## 0. The dated spine (one line per boundary event)

| date | event | sources |
|---|---|---|
| 2021-09-26 | election of the 20th Bundestag | [BWL-21] (page title "Bundestagswahl 2021"), [BT-WW24] ("Wahl zum 20. Deutschen Bundestag im September 2021") |
| 2021-10-26 | 20th Bundestag's constituent sitting | [BT-K20] |
| 2021-12-08 | Scholz elected chancellor (395 of 707); cabinet SPD + Grüne + FDP announced | [BT-KW21] [BT-BR21] |
| 2024-02-02 | Bundestag grants group status: Gruppe Die Linke (28), Gruppe BSW (10) | [BT-DHB] |
| 2024-03-01 | Bundeswahlausschuss re-determines the 2021 result after the Berlin partial repeat: 736 → 735 members, FDP −1 | [BT-WW24] |
| 2024-11-06 | Scholz asks the Bundespräsident to dismiss Lindner (FDP); announces a confidence question | [BREG-ST24] [BP-ST24] ("seit gestern Abend") |
| 2024-11-07 | Lindner, Buschmann, Stark-Watzinger (FDP) dismissed; Kukies and Wissing appointed; the chancellor and the remaining ministers stay in office | [BP-ENT24] [BP-ST24] |
| 2024-12-11 | the Art. 68 motion filed | [BREG-VF24] [BT-VF24] |
| 2024-12-16 | confidence vote lost: 207 yes, 394 no, 116 abstentions (717 cast) | [BT-VF24] |
| 2024-12-27 | Bundespräsident dissolves the 20th Bundestag (BGBl. 2024 I Nr. 434) and sets election day 2025-02-23 (Nr. 435) | [BP-AUF24] [BWL-WT25] |
| 2025-02-23 | election of the 21st Bundestag | [BWL-WT25]; returns in `returns_2025.md` |
| 2025-03-25 | 21st Bundestag's constituent sitting; the Scholz government's term ends (Art. 69(2)) and it carries on the business at the Bundespräsident's request (Art. 69(3)) | [BT-K21] [BP-GF25] [GG-69] |
| 2025-05-05 | Bundespräsident proposes Merz (Art. 63(1)) | [BP-ERN25] |
| 2025-05-06 | 1st ballot 310 (316 needed) — not elected; 2nd ballot 325 of 618 — elected; appointed; cabinet CDU + CSU + SPD (+1 non-party) announced | [BT-KW25] [BP-ERN25] [BT-BR25] |
| 2026-07-29 | Merz cabinet reshuffle (four ministers; see §2) | [BP-ENT26] [BREG-KAB] |

## 1. THE CHAMBER OF RECORD BY DATE

| from | to | chamber | elected | convened | size | seat table | sources |
|---|---|---|---|---|---|---|---|
| 2022-07-01 (window start) | 2025-03-24 | 20. Deutscher Bundestag | 2021-09-26 | 2021-10-26 | 736 members until the re-determination of 2024-03-01, then 735 | §1.1 below | [BT-K20] [BT-WW24] [BWL-21] |
| 2025-03-25 | 2026-09-24 (open) | 21. Deutscher Bundestag | 2025-02-23 | 2025-03-25 | 630 | `returns_2025.md` | [BT-K21] [BWL-WT25] |

**The rule joining the two rows** — Art. 39(1) GG [GG-39]: "Seine Wahlperiode endet mit dem Zusammentritt eines neuen
Bundestages." and, on the snap, "Im Falle einer Auflösung des Bundestages findet die Neuwahl innerhalb von sechzig
Tagen statt." Art. 39(2): "Der Bundestag tritt spätestens am dreißigsten Tage nach der Wahl zusammen." **DERIVED:** the
dissolution of 2024-12-27 did not end the 20th Bundestag; it sat until the 21st convened on 2025-03-25 (30 days after
2025-02-23, the Art. 39(2) limit). The 21st Bundestag's convening date is the boundary for both tables.

**20th Bundestag, constituent sitting** [BT-K20]:
> "Der Bundestag hat in seiner konstituierenden Sitzung am Dienstag, 26. Oktober 2021 , die Duisburger SPD-Abgeordnete Bärbel Bas zu seiner neuen Präsidentin gewählt. In geheimer Wahl erhielt Bas 576 von 724 Stimmen. Es gab 90 Gegenstimmen und 58 Enthaltungen. Für die Wahl war eine Mehrheit von 369 Stimmen erforderlich."

**21st Bundestag, constituent sitting** [BT-K21]:
> "Der Bundestag hat in seiner konstituierenden Sitzung am Dienstag, 25. März 2025, die rheinland-pfälzische Abgeordnete Julia Klöckner zu seiner neuen Präsidentin gewählt. In geheimer Wahl erhielt die CDU/CSU-Abgeordnete aus dem Wahlkreis Kreuznach 382 von 630 Stimmen. Es gab 204 Gegenstimmen und 31 Enthaltungen. Für die Wahl war eine Mehrheit von 316 Stimmen erforderlich."

### 1.1 The 20th Bundestag's seat table (2021 election)

The Bundeswahlleiterin's result page for 2021 [BWL-21] now serves the result **as re-determined after the Berlin
partial repeat of 11 Feb 2024** ("Ergebnis der Wiederholungswahl in Teilen Berlins am 11.02.2024."), not the
26 Sep 2021 determination. Its table, verbatim (tags stripped):
> "Partei Sitze Differenz zu 2017 CDU 152 -48 SPD 206 +53 AfD 83 -11 FDP 91 +11 DIE LINKE 39 -30 GRÜNE 118 +51 CSU 45 -1 SSW 1 +1"

The change between the two determinations, from the Bundestag's report of 1 March 2024 [BT-WW24]:
> "Rund zweieinhalb Jahre nach der Wahl zum 20. Deutschen Bundestag im September 2021 hat der Bundeswahlausschuss das endgültige Ergebnis erneut festgestellt. Demnach sinkt die Zahl der Mitglieder des Bundestages von 736 auf 735 Abgeordnete. Gegenüber dem Ergebnis der Hauptwahl entfällt auf die FDP ein Sitz weniger, wie Bundeswahlleiterin Dr. Ruth Brand am Freitag, 1. März 2024, verkündete."

| key | party as printed | seats at the 2021 determination (736) | seats from 2024-03-01 (735) |
|---|---|---|---|
| SPD | SPD | 206 | 206 |
| CDU | CDU | 152 | 152 |
| Grune | GRÜNE | 118 | 118 |
| FDP | FDP | 92 (**DERIVED**: 91 + the one seat [BT-WW24] says the FDP lost) | 91 |
| AfD | AfD | 83 | 83 |
| CSU | CSU | 45 | 45 |
| Linke | DIE LINKE | 39 | 39 |
| SSW | SSW | 1 | 1 |
| BSW | — | 0 (the party did not exist at the 2021 election; see below) | 0 by election; 10 members sat as "Gruppe BSW" from 2024-02-02 |

**DERIVED:** 206+152+118+92+83+45+39+1 = 736; with FDP 91, 735. The 2021-determination column rests on the
re-determined table plus the one-seat statement; the original 26 Sep 2021 page was not fetched (the site's
"Vergleich zur Hauptwahl" sub-page for the federal total returned 404 — see `fetch_log.txt` and G3).
**Caution for the data step:** the 735 figure is the chamber's size from the re-determination; the exact day the
lapsed FDP seat left the chamber's roll is not on the fetched page (G3).

**Groups inside the 20th Bundestag (parties, not seats, changed).** Bundestag Datenhandbuch 5.1 [BT-DHB] (PDF text
as extracted; umlauts in the extracted text are glyph codes and are elided here with "…"):
> "…hrt, bis am 2. Februar 2024 der Bundestag die Rechtsstellung der parlamentarischen Gruppen Die Linke (28 Mitglieder) und B…ndnis Sahra Wagenknecht (BSW) (10 Mitglieder) beschloss."
> "…neben der Gruppe Die Linke sitzen die Mitglieder der Gruppe BSW."

**DERIVED:** from 2024-02-02 the 39 members elected on Die Linke's lists sat as two groups, Die Linke (28) and BSW (10)
(the 39th is not accounted for on the extracted text — G2). So `BSW` is a party with seats in the 20th chamber from
2024-02-02 even though it won none at either election. The date the Linke Fraktion itself dissolved (late 2023) is
not on the extracted text (G2).

## 2. THE GOVERNMENT OF RECORD BY DATE

| from | to | head of government | party | cabinet parties | majority / support | boundary event at "from" | sources |
|---|---|---|---|---|---|---|---|
| 2022-07-01 (window start; in office since 2021-12-08) | 2024-11-06 | Olaf Scholz, Bundeskanzler | SPD | SPD, Grune, FDP | majority coalition (**DERIVED**: 206+118+92 = 416 of 736) | (before the window) Scholz elected 2021-12-08 with 395 of 707 | [BT-KW21] [BT-BR21] |
| 2024-11-07 | 2025-03-24 | Olaf Scholz, Bundeskanzler | SPD | SPD, Grune | **minority government**; no support agreement on record; "auf Unterstützung aus dem Oppositionslager angewiesen" | FDP ministers dismissed 2024-11-07 after Scholz's request of 2024-11-06 | [BREG-ST24] [BP-ENT24] [BP-ST24] [BT-MR24] |
| 2025-03-25 | 2025-05-06 | Olaf Scholz, Bundeskanzler (geschäftsführend) | SPD | SPD, Grune | caretaker under Art. 69(3): the office ended with the 21st Bundestag's convening (Art. 69(2)); asked to carry on the business | 21st Bundestag convened | [BP-GF25] [GG-69] |
| 2025-05-06 | 2026-09-24 (open) | Friedrich Merz, Bundeskanzler | CDU | CDU, CSU, SPD (+ one minister "parteilos") | majority coalition (**DERIVED**: `returns_2025.md` 164+44+120 = 328 of 630) | elected 2025-05-06, 2nd ballot 325 of 618; appointed the same day | [BT-KW25] [BP-ERN25] [BT-BR25] |

**2.1 Scholz's coalition** — the cabinet read out on 2021-12-08 [BT-BR21] (every minister with a party):
> "…wonach der Bundesregierung auf Vorschlag von Bundeskanzler Olaf Scholz (SPD) folgende Ministerinnen und Minister angehören: Dr. Robert Habeck (Bündnis 90/Die Grünen, Bundesminister für Wirtschaft und Klimaschutz, Vizekanzler), Christian Lindner (FDP, Bundesminister der Finanzen), Nancy Faeser (SPD, Bundesministerin des Innern und für Heimat), Annalena Baerbock (Bündnis 90/Die Grünen, Bundesministerin des Auswärtigen), Dr. Marco Buschmann (FDP, Bundesminister der Justiz), Hubertus Heil (SPD, Bundesminister für Arbeit und Soziales), Christine Lambrecht (SPD, Bundesministerin der Verteidigung), Cem Özdemir (Bündnis 90/Die Grünen, Bundesminister für Ernährung und Landwirtschaft), Anne Spiegel (Bündnis 90/Die Grünen, Bundesministerin für Familie, Senioren, Frauen und Jugend), Prof. Dr. Karl Lauterbach (SPD, Bundesminister für Gesundheit), Dr. Volker Wissing (FDP, Bundesminister für Digitales und Verkehr), Steffi Lemke (Bündnis 90/Die Grünen, Bundesministerin für Umwelt, Naturschutz nukleare Sicherheit und Verbraucherschutz), Bettina Stark-Watzinger (FDP, Bundesministerin für Bildung und Forschung), Svenja Schulze (SPD, Bundesministerin für wirtschaftliche Zusammenarbeit und Entwicklung), Klara Geywitz (SPD, Bundesministerin für Wohnen, Stadtentwicklung und Bauwesen), Wolfgang Schmidt (SPD, Bundesminister für besondere Aufgaben, Chef des Bundeskanzleramtes)."

The election [BT-KW21]:
> "Der Bundestag hat den SPD-Abgeordneten am Mittwoch, 8. Dezember 2021 , mit 395 von 707 abgegebenen Stimmen zum Kanzler in der 20. Wahlperiode (2021 bis 2025) gewählt. Für die Wahl erforderlich waren 369 Stimmen."

Individual ministers changed between 2021-12-08 and 2024-11-06 without the party set changing; those changes are
not sourced here (G4).

**2.2 The coalition breaks (6–7 Nov 2024).** The chancellor, 6 Nov 2024 [BREG-ST24] ("Mitschrift Pressekonferenz
Mittwoch, 6. November 2024"):
> "Bundeskanzler Olaf Scholz hat den Bundespräsidenten um die Entlassung des Bundesministers der Finanzen, Christian Lindner gebeten."
>
> "Gleich in der ersten Sitzungswoche des Bundestages im neuen Jahr werde ich dann die Vertrauensfrage stellen, damit der Bundestag am 15. Januar darüber abstimmen kann. So können die Mitglieder des Bundestages entscheiden, ob sie den Weg für vorgezogene Neuwahlen freimachen. Diese Wahlen könnten dann unter Einhaltung der Fristen, die das Grundgesetz vorsieht, spätestens bis Ende März stattfinden."

The Bundespräsident, 7 Nov 2024 [BP-ST24]:
> "Erlauben Sie mir bitte einige Worte zur aktuellen politischen Lage in Deutschland, wie sie sich seit gestern Abend entwickelt hat. In der 75-jährigen Geschichte der Bundesrepublik ist es selten vorgekommen, dass eine regierende Koalition vor Ablauf der Legislaturperiode keine Mehrheit im Deutschen Bundestag mehr hatte."
>
> "Der Bundeskanzler und die verbleibenden Minister bleiben – so sieht es die Verfassung vor – im Amt. Der Bundeskanzler hat erklärt, im Bundestag die Vertrauensfrage"

The dismissals and appointments, 7 Nov 2024 [BP-ENT24]:
> "Bundespräsident Steinmeier hat am 7. November Bundesfinanzminister Christian Lindner, Bundesjustizminister Marco Buschmann und Bundesbildungsministerin Bettina Stark-Watzinger die Entlassungsurkunden aus ihrem Amt überreicht. Jörg Kukies ernannte er zum neuen Bundesminister der Finanzen, Volker Wissing zum Bundesminister der Justiz."

The Bundestag's own description of what followed [BT-MR24] (its explainer; the page shows no article date, G5):
> "Mit dem Ausscheiden der FDP aus der Koalition hat die Regierung von Bundeskanzler Olaf Scholz (SPD) die absolute Mehrheit im Parlament verloren. Um trotzdem Gesetze beschließen zu können, ist eine Minderheitsregierung auf Unterstützung aus dem Oppositionslager angewiesen."

**DERIVED:** the cabinet-parties set after 7 Nov 2024 is SPD + Grüne. Wissing, elected for the FDP in 2021, stayed on
as Justice minister; the fetched pages give him no party label after 7 Nov, so his affiliation from that day is a GAP
(G6), and the "cabinet parties" cell above counts parties, not persons. Kukies's party is likewise not on the fetched
pages. **DERIVED:** the minority's own strength was 206+118 = 324 of 735 (< 368).

**2.3 The confidence question and the dissolution.** The motion, 11 Dec 2024 [BREG-VF24]:
> "Bundeskanzler Olaf Scholz hat heute (Mittwoch) Vormittag in seinem Büro im Bundeskanzleramt den schriftlichen Antrag zur Vertrauensfrage gemäß Artikel 68 Grundgesetz unterzeichnet. Das Schreiben lautet: „Sehr geehrte Frau Bundestagspräsidentin, gemäß Artikel 68 des Grundgesetzes stelle ich den Antrag, mir das Vertrauen auszusprechen. Ich beabsichtige, vor der Abstimmung am Montag, dem 16. Dezember 2024, hierzu eine Erklärung abzugeben.“"

The vote, 16 Dec 2024 [BT-VF24]:
> "Bundeskanzler Olaf Scholz (SPD) hat am Montag, 16. Dezember 2024 , die Vertrauensfrage im Bundestag verloren. Damit ist der Weg für Neuwahlen frei. 207 Abgeordnete stellten sich hinter Scholz, für eine Mehrheit hätte er mindestens 367 Stimmen benötigt. 394 Abgeordnete stimmten gegen ihn, 116 enthielten sich, 16 Abgeordnete nahmen an der namentlichen Abstimmung nicht teil."
>
> "Den Antrag gemäß Artikel 68 des Grundgesetzes ( 20/14150 (Dokument, öffnet ein neues Fenster) ) hatte der Bundeskanzler am Mittwoch, 11. Dezember, in den Bundestag eingebracht."
>
> "Gesamt: 717 Ja: 207 Nein: 394 Enthaltungen 116 … abgelehnt"

**DERIVED:** 207+394+116 = 717 cast; 717+16 = 733 members present-or-absent on the roll that day, against a chamber
of 735 — the two-member difference is not explained on the page (G3). The majority-of-members bar the page states,
367, is **DERIVED** by the Bundestag from 733 (367 = ⌊733/2⌋+1), which implies the chamber's roll on 16 Dec 2024 was 733,
not 735 — a discrepancy with [BT-WW24] that this file records and does not resolve (G3).

The dissolution, 27 Dec 2024 [BP-AUF24]:
> "Bundespräsident Steinmeier hat am 27. Dezember entschieden, den 20. Deutschen Bundestag aufzulösen. Er setzte Neuwahlen für den 23. Februar 2025 an."

The Bundeswahlleiterin, 27 Dec 2024 [BWL-WT25]:
> "Der Bundespräsident hat auf Ersuchen des Bundeskanzlers den 20. Deutschen Bundestag am 27. Dezember 2024 aufgelöst ( BGBl. 2024 I Nr. 434 ) und den Wahltag auf den 23. Februar 2025 bestimmt ( BGBl. 2024 I Nr. 435 )."

**DERIVED (Art. 68 clock):** 16 Dec 2024 + 21 days = 6 Jan 2025 ≥ 27 Dec 2024, inside the "binnen einundzwanzig
Tagen"; 27 Dec 2024 + 60 days = 25 Feb 2025 ≥ 23 Feb 2025, inside Art. 39(1)'s sixty days.

**2.4 The caretaker interval, 25 Mar – 6 May 2025** [BP-GF25]:
> "Bundespräsident Frank-Walter Steinmeier hat heute den Bundeskanzler gebeten, die Geschäfte bis zur Ernennung einer Nachfolgerin oder eines Nachfolgers weiterzuführen. Das Ersuchen nach Artikel 69 Absatz 3 Grundgesetz erfolgt aufgrund der heutigen Konstituierung des 21. Deutschen Bundestages."

**2.5 Merz's government.** The election, 6 May 2025 [BT-KW25]:
> "Der Bundestag hat den CDU-Abgeordneten am Dienstag, 6. Mai 2025 , mit 325 von 618 abgegebenen Stimmen im zweiten Wahlgang zum Kanzler in der 21. Wahlperiode (2025 bis 2029) gewählt. In der geheimen Wahl mit verdeckten Stimmkarten gab es 289 Gegenstimmen und eine Enthaltung. Drei Stimmen waren ungültig. Für die Wahl erforderlich waren 316 Stimmen."
>
> "Den zweiten Wahlgang hatte Bundestagspräsidentin Julia Klöckner um 15.15 Uhr aufgerufen, nachdem der erste Wahlgang am Vormittag nicht zur Wahl von Merz geführt hatte. Der Bundespräsident hatte Merz als Bundeskanzler vorgeschlagen (Artikel 63 Absatz 1 des Grundgesetzes). Statt der erforderlichen 316 Stimmen erhielt der Kandidat aber nur 310 Stimmen."
>
> "Friedrich Merz hat die nötige Mehrheit von mind. 316 Stimmen erhalten und ist gem Artikel 63 Abs 2 Grundgesetz zum Bundeskanzler gewählt worden."

The proposal and the appointment [BP-ERN25]:
> "Der Bundespräsident hat mit einem Schreiben an die Präsidentin des Deutschen Bundestages am 5. Mai dem Deutschen Bundestag vorgeschlagen, Friedrich Merz zum Bundeskanzler zu wählen. Am 6. Mai hat der Deutsche Bundestag Friedrich Merz im zweiten Wahlgang zum Bundeskanzler der Bundesrepublik Deutschland gewählt. Der Bundespräsident hat ihn ernannt."

The cabinet read out on 6 May 2025 [BT-BR25]:
> "…wonach der Bundesregierung auf Vorschlag von Bundeskanzler Friedrich Merz (CDU) folgende Ministerinnen und Minister angehören: Lars Klingbeil (SPD, Bundesminister der Finanzen, Vizekanzler), Alexander Dobrindt (CSU, Bundesminister des Innern), Dr. Johann David Wadephul (CDU, Bundesminister des Auswärtigen), Boris Pistorius (SPD, Bundesminister der Verteidigung), Katherina Reiche (CDU, Bundesministerin für Wirtschaft und Energie), Dorothee Bär (CSU, Bundesministerin für Forschung, Technologie und Raumfahrt), Dr. Stefanie Hubig (SPD, Bundesministerin der Justiz und für Verbraucherschutz), Karin Prien (CDU, Bundesministerin für Bildung, Familie, Senioren, Frauen und Jugend), Bärbel Bas (SPD, Bundesministerin für Arbeit und Soziales), Dr. Karsten Wildberger (parteilos, Bundesminister für Digitalisierung und Staatsmodernisierung), Patrick Schnieder (CDU, Bundesminister für Verkehr), Carsten Schneider (SPD, Bundesminister für Umwelt, Klimaschutz, Naturschutz und nukleare Sicherheit), Nina Warken (CDU, Bundesministerin für Gesundheit), Alois Rainer (CSU, Bundesminister für Landwirtschaft, Ernährung und Heimat), Reem Alabali-Radovan (SPD, Bundesministerin für wirtschaftliche Zusammenarbeit und Entwicklung), Verena Hubertz (SPD, Bundesministerin für Wohnen, Stadtentwicklung und Bauwesen), Thorsten Frei (CDU, Bundesminister für besondere Aufgaben und Chef des Bundeskanzleramtes)."

**2.6 The reshuffle of 29 Jul 2026** [BP-ENT26]:
> "Bundespräsident Steinmeier hat mehreren Mitgliedern der Bundesregierung gemäß Artikel 64 Abs. 1 des Grundgesetzes auf Vorschlag des Bundeskanzlers ihre Entlassungs- beziehungsweise Ernennungsurkunden ausgehändigt."
>
> "Thorsten Frei, der bisherige Bundesminister für besondere Aufgaben, erhielt seine Entlassungsurkunde. Anschließend entließ der Bundespräsident Nina Warken als Bundesministerin für Gesundheit und ernannte sie zur Bundesministerin für besondere Aufgaben. Zum neuen Bundesminister für Gesundheit ernannte der Bundespräsident Carsten Linnemann. Patrick Schnieder entließ er als Bundesminister für Verkehr. Seinem Nachfolger Steffen Bilger überreichte er die Ernennungsurkunde."

The cabinet page as served on 2026-09-24 [BREG-KAB] lists "Dr. Carsten Linnemann Lebenslauf Bundesminister für
Gesundheit", "Steffen Bilger Lebenslauf Bundesminister für Verkehr" and "Nina Warken Lebenslauf Bundesministerin für
besondere Aufgaben/Chefin des Bundeskanzleramtes"; it carries no party labels. **DERIVED:** the outgoing ministers were
CDU per [BT-BR25]; Linnemann was elected the CDU's Generalsekretär on 2026-02-20 [CDU-26] ("Generalsekretär Dr.
Carsten LINNEMANN 864 Stimmen 90,47 %"), so the head-of-cabinet party set (CDU, CSU, SPD) is unchanged by the
reshuffle on the evidence fetched; Bilger's party is not on any fetched page (G6).

## 3. PARTY LEADERS OF THE SEATED PARTIES BY DATE

Parties with seats in either chamber in the window: `SPD` `CDU` `CSU` `Grune` `FDP` `AfD` `Linke` `SSW` (both
chambers) and `BSW` (a ten-member Gruppe in the 20th Bundestag from 2024-02-02, §1.1). Where an office is held jointly,
both names are given. "office word" is the party's own.

| key | from | to | leader(s) | office word (party's own) | boundary event at "from" | sources |
|---|---|---|---|---|---|---|
| SPD | 2022-07-01 (window start; elected 2021-12-11) | 2025-06-26 | Saskia Esken; Lars Klingbeil | Vorsitzende | (before the window) elected 2021-12-11: Esken 465 of 606 (76,7%), Klingbeil 523 of 606 (86,3%) | [SPD-21] |
| SPD | 2025-06-27 | open | Bärbel Bas; Lars Klingbeil | Vorsitzende | elected 2025-06-27: Bas 589 of 620 (95,0%), Klingbeil 402 of 619 (64,9%) | [SPD-25] |
| CDU | 2022-07-01 (window start; in office "seit Februar 2022") | open | Friedrich Merz | Parteivorsitzender (the PDF's word; bundestag.de: "Vorsitzender der CDU") | (before the window) — re-elected 2026-02-20 with 878 votes, 91,17 % | [BT-BIO-MERZ] [CDU-26] |
| CSU | 2022-07-01 (window start; "2019: Vorsitzender der CSU") | open | Markus Söder | Parteivorsitzender | (before the window) | [CSU] |
| Grune | 2022-07-01 (window start) | 2024-11-15 (**DERIVED**) | Ricarda Lang; Omid Nouripour | Bundesvorsitzende | (before the window; their election date is a GAP, G8); attested in office on 2024-07-18; resignation announced 2024-09-25 "mit Wirkung zum Bundesparteitag im November" | [GRU-24a] [GRU-24b] |
| Grune | 2024-11-16 | open | Franziska Brantner; Felix Banaszak | Parteivorsitzende / Bundesvorsitzende | elected at the 50th congress, Wiesbaden: Brantner 78,15 %, Banaszak (percentage not on the fetched page, G8) | [GRU-24c] |
| FDP | 2022-07-01 (window start) | 2025-05-15 (**DERIVED**) | Christian Lindner | Bundesvorsitzender | (before the window); re-elected 2023-04-21 with 88 Prozent | [FDP-23] |
| FDP | 2025-05-16 | open | Christian Dürr | Bundesvorsitzender | elected at the 76th congress with 82,25 Prozent | [FDP-25] |
| AfD | 2022-07-01 (window start) | open | Tino Chrupalla (since 2019); Alice Weidel (since 2022) | Bundessprecher / Bundessprecherin | (before the window; Weidel's 2022 election date is a GAP, G8); both confirmed 2026-07-04 at the 17th congress | [AFD-BV] [AFD-26] |
| Linke | 2022-07-01 (window start; "Seit Juni 2022") | 2024-10-19 (**DERIVED**, see G9) | Janine Wissler; Martin Schirdewan | Parteivorsitzende | (before the window) elected at the Erfurt congress 2022 | [LIN-22] [LIN-SCH] |
| Linke | 2024-10-20 (**DERIVED**, see G9) | open | Ines Schwerdtner; Jan van Aken | Parteivorsitzende | elected at the Halle congress: Schwerdtner 79,7 %, van Aken 88,0 % | [LIN-24] [LIN-PV] [LIN-SWE] |
| SSW | 2022-07-01 (window start; elected "im Oktober 2021") | 2025-04-04 (**DERIVED**) | Christian Dirschauer | Landesvorsitzender | (before the window); announced 2025-03-03 that he would lay the office down at an extraordinary congress in April | [SSW-25a] |
| SSW | 2025-04-05 (**DERIVED**, see G7) | open | Sybilla Lena Nitsch | Landesvorsitzende | the extraordinary congress of 2025-04-05 had "Landesvorsitzende(r) (bisher Christian Dirschauer)" on its agenda; Nitsch is the sitting Landesvorsitzende on the party's board page | [SSW-25b] [SSW-LV] |
| BSW | 2024-01-27 (founding congress; the chairs' names at founding are a GAP, G10) | 2025-12-05 (**DERIVED**) | Sahra Wagenknecht; Amira Mohamed Ali | Parteivorsitzende / Co-Vorsitzende | founding congress 2024-01-27 elected the Parteivorstand | [BSW-24] [BSW-25] ("bisherige") |
| BSW | 2025-12-06 | open | Amira Mohamed Ali; Fabio De Masi | Parteivorsitzende / Co-Vorsitzender | elected/confirmed at the 3rd congress, Magdeburg: Mohamed Ali 530 of 642 (82,6 Prozent), De Masi 599 of 642 (93,3 Prozent) | [BSW-25] |

**The quotes behind the table.**

- SPD [SPD-21] (11.12.2021): "Saskia Esken und Lars Klingbeil wurden heute auf dem Ordentlichen SPD- B undesparteitag in Berlin zu den Vorsitzenden der Sozialdemokratischen Partei Deutschlands gewählt." *[sic: the source's spacing]*. Tallies: "Wahlergebnis Saskia Esken: Abgegebene Stimmen 606 davon gültige Stimmen 606 Ja-Stimmen 465 (76,7%) Nein-Stimmen 104 Enthaltungen 37 Wahlergebnis Lars Klingbeil: Abgegebene Stimmen 606 davon gültige Stimmen 606 Ja-Stimmen 523 (86,3%) Nein-Stimmen 60 Enthaltungen 23"
- SPD [SPD-25] (27.06.2025): "Bärbel Bas und Lars Klingbeil wurden heute auf dem Ordentlichen SPD-Bundesparteitag in Berlin als Vorsitzende der Sozialdemokratischen Partei Deutschlands wiedergewählt." *[sic: the release says "wiedergewählt" of both, although Bas is not on the 2021 release]*. Tallies: "Wahlergebnis Bärbel Bas: Abgegebene Stimmen 620 davon gültige Stimmen 620 Ja-Stimmen 589 Nein-Stimmen 16 Enthaltungen 15 Prozentuale Zustimmung 95,0% Wahlergebnis Lars Klingbeil: Abgegebene Stimmen 619 davon gültige Stimmen 619 Ja-Stimmen 402 Nein-Stimmen 166 Enthaltungen 51 Prozentuale Zustimmung 64,9%"
- CDU [BT-BIO-MERZ]: "seit Februar 2022 Vorsitzender der CDU". [CDU-26] (PDF text): "ERGEBNISSE DER WAHLEN ZUM BUNDESVORSTAND DER CDU DEUTSCHLANDS BEIM 38. PARTEITAG, STUTTGART AM 20. FEBRUAR 2026 Parteivorsitzender Friedrich MERZ 878 Stimmen 91,17 %". The party's own article on his 2022 election could not be fetched: cdu.de redirects it to `www.home.cdu.de`, which refused every connection on 2026-09-24 (G8).
- CSU [CSU]: "Bayerischer Ministerpräsident und Parteivorsitzender Dr. Markus Söder, MdL" and, in the CV, "2019: Vorsitzender der CSU".
- Grüne [GRU-24a] (18.07.2024): "Unsere Bundesvorsitzenden Ricarda Lang und Omid Nouripour stellen euch vor, welche Erkenntnisse wir aus zahlreichen Gesprächen und Datenerhebungen gewonnen haben". [GRU-24b] (25.09.2024): "Deshalb hat der Bundesvorstand entschieden, dass es Zeit ist, die Geschicke dieser großartigen Partei in neue Hände zu geben. Der Bundesvorstand legt mit Wirkung zum Bundesparteitag im November seine Ämter nieder." [GRU-24c] (16.11.2024): "Franziska Brantner und Felix Banaszak sind die neuen Parteivorsitzenden von BÜNDNIS 90/DIE GRÜNEN. Sie wurden auf dem 50. Parteitag in Wiesbaden mit großer Mehrheit der Delegierten gewählt." and "Franziska Brantner wurde mit 78,15 Prozent der Stimmen gewählt." The party's page on the 2022 election ("neue-parteispitze-bestaetigt") returned 404 (G8).
- FDP [FDP-23] (21.04.2023): "Der 74. Ord. Bundesparteitag der Freien Demokraten hat das Präsidium mit folgenden Ergebnissen neu gewählt: Bundesvorsitzender: Christian Lindner, 88 Prozent". [FDP-25] (16.05.2025): "Der 76. Ord. Bundesparteitag der Freien Demokraten hat das Präsidium mit folgenden Ergebnissen neu gewählt: Bundesvorsitzender: Christian Dürr, 82,25 Prozent". **DERIVED:** Lindner's tenure ends the day before Dürr's election; the 2025 release does not name Lindner.
- AfD [AFD-BV]: "Seit 2019 ist er Bundessprecher der AfD und seit 2021 gemeinsam mit Alice Weidel Vorsitzender der AfD-Bundestagsfraktion." and "Seit 2022 ist sie Bundessprecherin der AfD und seit 2021 gemeinsam mit Tino Chrupalla Vorsitzende der AfD-Bundestagsfraktion." [AFD-26] (04.07.2026): "Der 17. Bundesparteitag der Alternative für Deutschland hat am 4. Juli 2026 einen neuen Bundesvorstand gewählt. Die Delegierten bestätigten die bisherigen Bundessprecher Alice Weidel und Tino Chrupalla in ihren Ämtern".
- Linke [LIN-22]: "Wahl des Parteivorstandes Wir gratulieren den neugewählten Mitgliedern des Parteivorstandes! Parteivorsitzende Name Janine Wissler Martin Schirdewan". [LIN-SCH]: "Seit Juni 2022 Vorsitzender der Partei DIE LINKE". [LIN-24]: "Wahl des Parteivorstands Wir gratulieren dem neugewählten Parteivorstand! Parteivorsitzende Ines Schwerdtner (79,7 %) Jan van Aken (88,0 %)". [LIN-PV]: "Parteivorstand 2024 - 2026 … Ines Schwerdtner Parteivorsitzende … Jan van Aken Parteivorsitzender".
- SSW [SSW-25a] (03.03.2025): "Der Landesvorsitzende des SSW, MdL Christian Dirschauer, wird auf einem außerordentlichen Parteitag im April sein Amt niederlegen" and "Christian Dirschauer war im Oktober 2021 als Nachfolger von Flemming Meyer zum SSW-Landesvorsitzenden gewählt worden." [SSW-25b] (agenda of 5 April 2025): "Wahlen zum Landesvorstand gemäß § 20 und § 21 der Satzung - Landesvorsitzende(r) (bisher Christian Dirschauer) - evtl. 1. stellv. Landesvorsitzende(r) (bisher Sybilla Nitsch)". [SSW-LV]: "Landesvorsitzende Sybilla Lena Nitsch". The SSW is a Land party (Schleswig-Holstein); its top office is the Landesvorsitz.
- BSW [BSW-24]: "27. Januar 2024, haben wir im Berliner Kino Kosmos unseren Bundesparteitag und unsere Aufstellungsversammlung für die EU-Wahl abgehalten." and "Wir haben beim #bswbpt u.a. den Parteivorstand gewählt". [BSW-25] (6. Dezember 2025): "…am heutigen Samstag in Magdeburg die bisherige Parteivorsitzende Mohamed Ali im Amt und wählten De Masi, den Sprecher der BSW-Delegation im Europäischen Parlament, zum neuen Co-Vorsitzenden. Mohamed Ali erhielt 530 von 642 abgegebenen Stimmen (82,6 Prozent), De Masi 599 von 642 Stimmen (93,3 Prozent). Die Parteigründerin und bisherige Co-Vorsitzende Sahra Wagenknecht übernimmt künftig die Leitung der neuen Grundwertekommission."

## 4. THE CONSTITUTIONAL RULES (§5.4) — Grundgesetz, from gesetze-im-internet.de

Each article page is saved whole; the texts below are the articles' full operative text as served (entities decoded).

**Art. 63 GG — the chancellor's election** [GG-63]:
> "(1) Der Bundeskanzler wird auf Vorschlag des Bundespräsidenten vom Bundestage ohne Aussprache gewählt.
> (2) Gewählt ist, wer die Stimmen der Mehrheit der Mitglieder des Bundestages auf sich vereinigt. Der Gewählte ist vom Bundespräsidenten zu ernennen.
> (3) Wird der Vorgeschlagene nicht gewählt, so kann der Bundestag binnen vierzehn Tagen nach dem Wahlgange mit mehr als der Hälfte seiner Mitglieder einen Bundeskanzler wählen.
> (4) Kommt eine Wahl innerhalb dieser Frist nicht zustande, so findet unverzüglich ein neuer Wahlgang statt, in dem gewählt ist, wer die meisten Stimmen erhält. Vereinigt der Gewählte die Stimmen der Mehrheit der Mitglieder des Bundestages auf sich, so muß der Bundespräsident ihn binnen sieben Tagen nach der Wahl ernennen. Erreicht der Gewählte diese Mehrheit nicht, so hat der Bundespräsident binnen sieben Tagen entweder ihn zu ernennen oder den Bundestag aufzulösen."

*gloss:* first ballot on the president's proposal, absolute majority of members; failing that, the Bundestag has
fourteen days to elect someone by absolute majority (Merz's second ballot of 6 May 2025 was this phase, the same
day); failing that, a plurality ballot, after which the president appoints or dissolves within seven days.

**Art. 67 GG — the constructive vote of no confidence** [GG-67]:
> "(1) Der Bundestag kann dem Bundeskanzler das Mißtrauen nur dadurch aussprechen, daß er mit der Mehrheit seiner Mitglieder einen Nachfolger wählt und den Bundespräsidenten ersucht, den Bundeskanzler zu entlassen. Der Bundespräsident muß dem Ersuchen entsprechen und den Gewählten ernennen.
> (2) Zwischen dem Antrage und der Wahl müssen achtundvierzig Stunden liegen."

**Art. 68 GG — the confidence question and dissolution** [GG-68]:
> "(1) Findet ein Antrag des Bundeskanzlers, ihm das Vertrauen auszusprechen, nicht die Zustimmung der Mehrheit der Mitglieder des Bundestages, so kann der Bundespräsident auf Vorschlag des Bundeskanzlers binnen einundzwanzig Tagen den Bundestag auflösen. Das Recht zur Auflösung erlischt, sobald der Bundestag mit der Mehrheit seiner Mitglieder einen anderen Bundeskanzler wählt.
> (2) Zwischen dem Antrage und der Abstimmung müssen achtundvierzig Stunden liegen."

**Art. 69 GG — end of office and caretaking** [GG-69] (the rule behind the 25 Mar – 6 May 2025 row):
> "(1) Der Bundeskanzler ernennt einen Bundesminister zu seinem Stellvertreter.
> (2) Das Amt des Bundeskanzlers oder eines Bundesministers endigt in jedem Falle mit dem Zusammentritt eines neuen Bundestages, das Amt eines Bundesministers auch mit jeder anderen Erledigung des Amtes des Bundeskanzlers.
> (3) Auf Ersuchen des Bundespräsidenten ist der Bundeskanzler, auf Ersuchen des Bundeskanzlers oder des Bundespräsidenten ein Bundesminister verpflichtet, die Geschäfte bis zur Ernennung seines Nachfolgers weiterzuführen."

**Art. 39 GG — term and convening** [GG-39]: quoted under §1.

## 5. What this file says against the spec's §4 line and `returns_2025.md`

- **§4 "the coalition's collapse in November 2024"** — on record: the chancellor asked for Lindner's dismissal on
  **2024-11-06** [BREG-ST24]; the FDP ministers were dismissed on **2024-11-07** [BP-ENT24]; the Bundespräsident dated the
  loss of the majority to "gestern Abend" on 7 Nov [BP-ST24]. Both dates are November 2024; which of them is "the day
  the snap election became inevitable" is a ruling, not a fact: on 6 Nov the chancellor announced a confidence
  question for **15 January 2025** and elections "spätestens bis Ende März" [BREG-ST24]; the vote was in fact held on
  **16 Dec 2024** and the election set for **23 Feb 2025** — so the calendar the spec's start would give the campaign
  depends on which November day is chosen, and the vote date actually used was moved forward after 6 Nov.
- **§4 "Scholz's minority after the coalition broke"** — confirmed: the Bundestag's own text calls it a
  Minderheitsregierung of SPD and Grüne dependent on the opposition [BT-MR24]; **DERIVED** 324 of 735 (or of 733, see
  G3). One nuance the line does not carry: from 2025-03-25 to 2025-05-06 that government was **geschäftsführend** under
  Art. 69(3) [BP-GF25], its office having ended by Art. 69(2) — a caretaker of a minority, not a minority government.
- **`returns_2025.md`** — nothing here contradicts it. Its 630 seats match [BT-K21] ("382 von 630 Stimmen") and the
  316-vote majority the Bundestag applied [BT-KW25]. Its "BSW 0 seats" is right for the 21st chamber; note that in the
  **20th** chamber BSW held a ten-member Gruppe from 2024-02-02 [BT-DHB] without ever winning a seat — a party with
  seats by defection, which the world clock's seat tables by party must decide how to carry (§1.1).
- **The 20th chamber's size** is not one number: 736 at convening, 735 from the re-determination of 2024-03-01
  [BT-WW24], and the Bundestag's own majority bar on 2024-12-16 implies 733 (G3).

## GAPS

- **G1: the Grundgesetz pages show no "Stand".** gesetze-im-internet.de's single-article pages carry no consolidation
  date; the articles' text is as served on 2026-09-24. The framework page with the amendment history was not fetched.
- **G2: the Datenhandbuch PDF's text was read by inflating its streams**; umlauts came out as glyph codes and one
  sentence is quoted with elisions. The date the Linke Fraktion dissolved (late 2023) and the whereabouts of the 39th
  Linke-elected member (28 + 10 = 38) are not on the extracted text.
- **G3: the 20th chamber's roll.** The original 26 Sep 2021 determination (736, FDP 92) is DERIVED from the re-determined
  page [BWL-21] and the one-seat statement [BT-WW24]; the Bundeswahlleiterin's own comparison page for the federal
  total returned 404. The day the lapsed seat left the roll is not fetched. The Bundestag's majority bar of 367 on
  16 Dec 2024 implies a roll of 733, not 735; the two vacancies are not explained on any fetched page.
- **G4: minister changes inside Scholz's coalition** (2022-07-01 → 2024-11-06) are not sourced; the party set did not
  change, which is all the government table carries.
- **G5: the Bundestag's Minderheitsregierung explainer shows no article date** (URL week kw45 of 2024; footer "Stand:
  24.09.2026").
- **G6: parties of Wissing (from 2024-11-07), Kukies, and Bilger (from 2026-07-29)** are on no fetched page; the
  cabinet-parties cells count parties of ministers whose party the sources label.
- **G7: the SSW succession's result page was not found.** The 5 April 2025 congress page fetched is the convening notice
  with the agenda; Nitsch's election is attested only by the sitting-board page [SSW-LV]. Her start date 2025-04-05 is
  DERIVED from the agenda and the announcement [SSW-25a].
- **G8: pre-window and unreachable leader pages.** CDU: cdu.de's article on Merz's 2022 election redirects to
  `www.home.cdu.de`, which refused every connection (curl 28) — his start is cited from bundestag.de's biography
  ("seit Februar 2022"), not the party. Grüne: the 2022 election page returned 404; Lang/Nouripour's start is not
  sourced, only their tenure inside the window (18 Jul 2024) and its end; Banaszak's percentage is not on the fetched
  16 Nov 2024 page (it names his election, not his tally). AfD: Weidel's 2022 election date is "Seit 2022" only.
- **G9: the Linke congress days.** Neither the Erfurt 2022 nor the Halle 2024 page carries a date; the Erfurt page
  names its year in its heading and Schirdewan's page says "Seit Juni 2022"; the Halle congress's day is not on the
  fetched pages, and the boundary 2024-10-19/20 is DERIVED from the congress name and the "2024 - 2026" board page
  only — a second session should fetch a dated page (the party's press release) before this date is wired.
- **G10: BSW's chairs at founding.** The founding-congress page says the Parteivorstand was elected on 2024-01-27 but
  does not name the chairs; that Wagenknecht and Mohamed Ali held the office is attested by the 6 Dec 2025 page's
  "bisherige" only. Their election date and tallies are not fetched.
- **G11: page count.** 48 raw pages were kept (the bill asked for under ~40); ten more were fetched, read and deleted
  as useless (listed as DISCARDED in `fetch_log.txt`). Two of the kept pages (AfD) were served gzip and are saved
  decompressed, as the log notes.
- **G12: second-session re-verification (R-K9) is pending**, as for every SOURCED file.

## Source register
(all accessed 2026-09-24; saved as `raw/records/<file>`; "page's own date" is the date the page itself shows, or the
dated sentence relied on where the page shows none; bytes and SHA-256 emitted by script from the files on disk)

| id | URL | publisher | page's own date | basis | file | bytes | SHA-256 |
|---|---|---|---|---|---|---|---|
| [GG-39] | https://www.gesetze-im-internet.de/gg/art_39.html | Bundesministerium der Justiz / juris, gesetze-im-internet.de | none shown on the article page (see G1) | consolidated Grundgesetz, single article | `gg_art_39.html` | 4159 | `9c547bfc68937e27cef0a709c876fa0a8b70e3c3e01817a1dd55b322bdcbe8af` |
| [GG-63] | https://www.gesetze-im-internet.de/gg/art_63.html | Bundesministerium der Justiz / juris, gesetze-im-internet.de | none shown on the article page (see G1) | consolidated Grundgesetz, single article | `gg_art_63.html` | 4416 | `192729d89bc4b4384fe4933aada655329bf27f70474401c4866fd5a6623b888e` |
| [GG-67] | https://www.gesetze-im-internet.de/gg/art_67.html | Bundesministerium der Justiz / juris, gesetze-im-internet.de | none shown on the article page (see G1) | consolidated Grundgesetz, single article | `gg_art_67.html` | 3781 | `537cfcd9e6831b29e50992b83a272fe50f00b0f1f3cbc69850cb5f6aed9cf40e` |
| [GG-68] | https://www.gesetze-im-internet.de/gg/art_68.html | Bundesministerium der Justiz / juris, gesetze-im-internet.de | none shown on the article page (see G1) | consolidated Grundgesetz, single article | `gg_art_68.html` | 3861 | `9c3e87d23e4ee8f00470c55e033e8cf4aaf338c44438bba8c0a8b6e564bf76b4` |
| [GG-69] | https://www.gesetze-im-internet.de/gg/art_69.html | Bundesministerium der Justiz / juris, gesetze-im-internet.de | none shown on the article page (see G1) | consolidated Grundgesetz, single article | `gg_art_69.html` | 3943 | `e717dcb1520ef7304bf2865615351a459e563234b5275538456c6abdff67c7d5` |
| [BWL-21] | https://www.bundeswahlleiterin.de/bundestagswahlen/2021/ergebnisse/bund-99.html | Die Bundeswahlleiterin | as served: "Ergebnis der Wiederholungswahl in Teilen Berlins am 11.02.2024" (the page now carries the RE-DETERMINED result, not the 26 Sep 2021 one) | official final result page, 2021 election, as it stands after the Berlin partial repeat | `bwl_2021_bund-99.html` | 81727 | `f6d0ab892048683f5d65c097d4cdf0e7847ab5d948e93e385309324d1daff4fb` |
| [BWL-WT25] | https://www.bundeswahlleiterin.de/mitteilungen/bundestagswahlen/2025/20241227_verkuerzte-fristen.html | Die Bundeswahlleiterin | "27. Dezember 2024" | official notice: dissolution and election day, with BGBl. citations | `bwl_2024-12-27_wahltermin-verkuerzte-fristen.html` | 14218 | `6ba7c4b96560b3ff3aef8b94ddf34b1060026d6b7c1fd8d2e9acefef95890e96` |
| [BT-K20] | https://www.bundestag.de/dokumente/textarchiv/2021/kw42-konstituierende-sitzung-865146 | Deutscher Bundestag (Textarchiv) | sitting date in text: "Dienstag, 26. Oktober 2021" | Bundestag's own report of the 20th Bundestag's constituent sitting | `bt_2021-10-26_konstituierende-sitzung.html` | 324514 | `971b8c89e48d62389ab9e4f42bea2b156d1908f1ef90b3309c9234a4e39853c0` |
| [BT-KW21] | https://www.bundestag.de/dokumente/textarchiv/2021/kw49-de-kanzlerwahl-870142 | Deutscher Bundestag (Textarchiv) | sitting date in text: "Mittwoch, 8. Dezember 2021" | Bundestag's own report of the chancellor's election, with the tally | `bt_2021-12-08_kanzlerwahl.html` | 266377 | `72d7c02f102f929b8eead2b36d01232892357328d06adf109208e71524b908c0` |
| [BT-BR21] | https://www.bundestag.de/dokumente/textarchiv/2021/kw49-de-kanzlerwahl-bundesregierung-870146 | Deutscher Bundestag (Textarchiv) | same sitting, 8 Dec 2021 | Bundestag's report of the cabinet list read out from the Bundespräsident's letter (party of every minister) | `bt_2021-12-08_bundesregierung.html` | 267960 | `d643a8c354d60bdad6701dd2c14a4d0556185231af68ca06de3bf85f238a87f2` |
| [BT-WW24] | https://www.bundestag.de/dokumente/textarchiv/2024/kw09-pa-bundeswahlausschuss-ergebnis-990594 | Deutscher Bundestag (Textarchiv) | event date in text: "Freitag, 1. März 2024" | Bundestag's report of the Bundeswahlausschuss re-determination after the Berlin partial repeat (736 -> 735) | `bt_2024-02_wahlwiederholung-berlin-verkleinerung.html` | 258892 | `a19e200a11ed83d58573cda2715ff8f42792578bf45c103bb433ccf01324a42a` |
| [BT-DHB] | https://www.bundestag.de/resource/blob/196146/3dd33ed319f1a85407e031ac9068c9b5/Kapitel_05_01_Bildung_von_Fraktionen_und_Gruppen.pdf | Deutscher Bundestag, Datenhandbuch, Kapitel 5.1 | "05.02.2025" (chapter head), table "Stand: 2.2.2024" | Bundestag's data handbook chapter on Fraktionen and Gruppen (PDF; text read by inflating its FlateDecode streams, see G2) | `bt_dhb_kap-05-01_fraktionen-und-gruppen.pdf` | 215372 | `29cb3e3e464655a3ee76f0bc15eb3254175532fba88d8b2e8e0076d4f658b071` |
| [BT-MR24] | https://www.bundestag.de/dokumente/textarchiv/2024/kw45-minderheitsregierung-1028778 | Deutscher Bundestag (Textarchiv) | no article date shown; URL week "kw45" (2024); page footer "Stand: 24.09.2026" | Bundestag explainer written after the FDP left the coalition | `bt_2024-11_minderheitsregierung.html` | 259793 | `f4b3c31bba359238f768af69f8d3a2bc908a9ac979d28fffe5adab045bac9c0f` |
| [BT-VF24] | https://www.bundestag.de/dokumente/textarchiv/2024/kw51-de-vertrauensfrage-1033624 | Deutscher Bundestag (Textarchiv) | sitting date in text: "Montag, 16. Dezember 2024" | Bundestag's own report of the confidence vote, with the roll-call tally | `bt_2024-12-16_vertrauensfrage.html` | 398205 | `5bd7bfd25b7790eafa29401cd80d8629473f88677f76961c5f091962c9538a64` |
| [BT-K21] | https://www.bundestag.de/dokumente/textarchiv/2025/kw13-de-konstituierung-bericht-1058004 | Deutscher Bundestag (Textarchiv) | sitting date in text: "Dienstag, 25. März 2025" | Bundestag's own report of the 21st Bundestag's constituent sitting | `bt_2025-03-25_konstituierung.html` | 326589 | `b773ca821829a89e037ac94631d34b51fdc9943b7b07ed008ec12be3bc6a0957` |
| [BT-KW25] | https://www.bundestag.de/dokumente/textarchiv/2025/kw19-de-kanzlerwahl-1062470 | Deutscher Bundestag (Textarchiv) | sitting date in text: "Dienstag, 6. Mai 2025" | Bundestag's own report of the chancellor's election, both ballots | `bt_2025-05-06_kanzlerwahl.html` | 317116 | `1a14d5dbab76e9df7e5bd9961c5b784bab657e40b2d9ef635b31004f6da96ded` |
| [BT-BR25] | https://www.bundestag.de/dokumente/textarchiv/2025/kw19-de-kanzlerwahl-bundesregierung-1063886 | Deutscher Bundestag (Textarchiv) | same sitting, 6 May 2025 | Bundestag's report of the cabinet list read out from the Bundespräsident's letter (party of every minister) | `bt_2025-05-06_bundesregierung.html` | 267712 | `40eb43efc48f0e6e1c2626de25b30f3bb8f71d2474a90aadb89d0e95d2f93e48` |
| [BT-BIO-MERZ] | https://www.bundestag.de/abgeordnete/biografien/M/merz_friedrich-1046080 | Deutscher Bundestag (member biography) | none shown (live page as served 2026-09-24) | official member biography | `bt_biografie_merz_friedrich.html` | 274264 | `a2d13573bb740f3d4d384b30eb8417ceb45666b49eb25a328b7439b6de62b845` |
| [BP-ENT24] | https://www.bundespraesident.de/SharedDocs/Reden/DE/Frank-Walter-Steinmeier/Reden/2024/11/241107-Entlassung-Ernennung-Minister.html | Bundespräsidialamt | "7. November 2024" | the Bundespräsident's speech at the dismissals and appointments | `bp_2024-11-07_entlassung-ernennung-minister.html` | 134073 | `ac09374f5c67e278cc45228a256c575b52a23cb6e6ac968932b384702c9b2c28` |
| [BP-ST24] | https://www.bundespraesident.de/SharedDocs/Pressemitteilungen/DE/2024/11/241107-Statement-politische-Lage.html | Bundespräsidialamt | 7 Nov 2024 (in URL and text: "seit gestern Abend") | the Bundespräsident's statement on the coalition's end | `bp_2024-11-07_statement-politische-lage.html` | 119448 | `319d5a5b29febb3a576b6bafa90047aa00ece78313631d6b3e2d5555caeae4a5` |
| [BP-AUF24] | https://www.bundespraesident.de/SharedDocs/Reden/DE/Frank-Walter-Steinmeier/Reden/2024/12/241227-Entscheidung-Aufloesung-BT.html | Bundespräsidialamt | "27. Dezember 2024" | the Bundespräsident's statement on the dissolution under Art. 68 | `bp_2024-12-27_entscheidung-aufloesung-bt.html` | 124234 | `0b576b91325dccbedf283f7c3b1484dc010ac8ccc5eb0f26378d5acea661b388` |
| [BP-GF25] | https://www.bundespraesident.de/SharedDocs/Pressemitteilungen/DE/2025/03/250325-Beauftragung-Bundeskanzler.html | Bundespräsidialamt | "25. März 2025" | press release: the chancellor asked to carry on the business under Art. 69(3) | `bp_2025-03-25_beauftragung-bundeskanzler.html` | 117648 | `a8e86c108ab2c3e5f941e5625d9dc294461d9256d9e75c67b356a9f7fe14ee2b` |
| [BP-ERN25] | https://www.bundespraesident.de/SharedDocs/Berichte/DE/Frank-Walter-Steinmeier/2025/05/250506-Ernennung-BK-BReg.html | Bundespräsidialamt | "6. Mai 2025" | report: the chancellor's proposal (5 May), election and appointment (6 May) | `bp_2025-05-06_ernennung-bk-breg.html` | 129590 | `3ae68355f950bbd236decfebe35efe08abf2b0bd4399a5b556d054ca2bbd1980` |
| [BP-ENT26] | https://www.bundespraesident.de/SharedDocs/Berichte/DE/Frank-Walter-Steinmeier/2026/07/260729-Entlassungen-Ernennungen-BM.html | Bundespräsidialamt | "29. Juli 2026" | report: cabinet reshuffle under Art. 64(1) | `bp_2026-07-29_entlassungen-ernennungen-bm.html` | 132250 | `7551fcfabee04310377553b096edc198d9643b7e6b634a6f13787c7e236de251` |
| [BREG-ST24] | https://www.bundesregierung.de/breg-de/aktuelles/pressekonferenzen/bk-statement-zur-entlassung-des-finanzministers-2319062 | Bundesregierung (Presse- und Informationsamt) | "Mittwoch, 6. November 2024" | transcript of the chancellor's statement on Lindner's dismissal | `breg_2024-11-06_statement-entlassung-finanzminister.html` | 107830 | `8ea4cd4449977b5fbff8d73ef9d8e0efac1ecae1a1778580af8431e9128c8a4b` |
| [BREG-VF24] | https://www.bundesregierung.de/breg-de/aktuelles/pressemitteilungen/bundeskanzler-scholz-beantragt-die-vertrauensfrage-2324868 | Bundesregierung (Presse- und Informationsamt) | "Pressemitteilung 305 Mittwoch, 11. Dezember 2024" | press release with the text of the Art. 68 motion | `breg_2024-12-11_antrag-vertrauensfrage.html` | 90954 | `85b642304e0189a555d7932723b1f62bde94dbe0dc9f7c94035e33e457fce875` |
| [BREG-KAB] | https://www.bundesregierung.de/breg-de/bundesregierung/bundeskabinett | Bundesregierung | none shown (live page as served 2026-09-24) | the cabinet list as it stands today (no party labels on the page) | `breg_bundeskabinett.html` | 157665 | `33b446f0d45b01f49df082b0049c7ba3f1357059bb4465475c2d4c93e5e1f5a6` |
| [SPD-21] | https://www.spd.de/service/pressemitteilungen/detail/news/saskia-esken-und-lars-klingbeil-als-spd-vorsitzende-gewaehlt/11/12/2021/ | SPD | "11.12.2021 | 190/21" | party press release with the tally | `spd_2021-12-11_esken-klingbeil-gewaehlt.html` | 54322 | `3f808de02089d4d9d14c6847dd36750a826c739b222e194699920d43346857d1` |
| [SPD-25] | https://www.spd.de/service/pressemitteilungen/detail/news/baerbel-bas-und-lars-klingbeil-als-spd-vorsitzende-gewaehlt/27/06/2025 | SPD | "27.06.2025 | 115/25" | party press release with the tally | `spd_2025-06-27_bas-klingbeil-gewaehlt.html` | 54322 | `9be71c5f20b56fad17a82d000a698f00fe6d27a3d05c05bd5f6a42ad31954893` |
| [GRU-24a] | https://www.gruene.de/artikel/im-gespraech-mit-ricarda-und-omid | BÜNDNIS 90/DIE GRÜNEN | "18.07.2024" | party article naming the sitting Bundesvorsitzende (used as an in-window attestation of who held the office) | `gruene_2022_im-gespraech-mit-ricarda-und-omid.html` | 193319 | `7ae5d7ad563f28edbb5d74d13180fd3817fcd36b001e15bf0769203906cbe53a` |
| [GRU-24b] | https://www.gruene.de/artikel/bundesvorstand-legt-aemter-nieder | BÜNDNIS 90/DIE GRÜNEN | "25.09.2024" | party article: the Bundesvorstand resigns with effect from the November congress | `gruene_2024-09_bundesvorstand-legt-aemter-nieder.html` | 191871 | `5e9d85cc7e0efb35a01fd3e0c101c1d8430483c943d5860364fa95603d135ba3` |
| [GRU-24c] | https://www.gruene.de/artikel/neustartklar-mit-neuem-bundesvorstand | BÜNDNIS 90/DIE GRÜNEN | "16.11.2024" | party article: the new Bundesvorsitzende elected at the 50th congress, Wiesbaden, with tallies | `gruene_2024-11_neuer-bundesvorstand.html` | 205293 | `ac1da35e45999b233f5ba1bd59b495e87a3a08f36e26bd024396a36db4174c0d` |
| [LIN-22] | https://www.die-linke.de/partei/parteidemokratie/parteitag/erfurter-parteitag-2022/live/wahl-des-parteivorstandes/ | Die Linke | none shown; the page's own heading "Erfurter Parteitag 2022: Wahl des Parteivorstandes" | party page: the Parteivorstand elected at the Erfurt congress | `linke_2022-06_erfurt-wahl-parteivorstand.html` | 60373 | `698fa1180ae061e6f50b1d1ecc2880d13e67d9d2e6c67fb8726b18d0ffafe8ca` |
| [LIN-SCH] | https://www.die-linke.de/partei/parteidemokratie/parteivorstand/parteivorstand-2022-2024/mitglieder-des-parteivorstandes/schirdewan-martin-parteivorsitzender/ | Die Linke | none shown (archived 2022-2024 board page) | party member page with the office's start month | `linke_pv-2022-2024_schirdewan-martin.html` | 58819 | `7b5b3f8e9b05aea516c5f045ec423f48430a697e6a5a8018d7672d4bdc7f056c` |
| [LIN-24] | https://www.die-linke.de/partei/parteidemokratie/parteitag/hallescher-parteitag-2024/hallescher-parteitag/wahl-des-parteivorstands/ | Die Linke | none shown; URL names the 2024 Halle congress | party page: the Parteivorstand elected at the Halle congress, with tallies | `linke_2024-10_halle-wahl-parteivorstand.html` | 54604 | `c754f4d0920e391941c953d2809872e6011e44e4e40ee76f8becf9ceb29a057e` |
| [LIN-PV] | https://www.die-linke.de/partei/parteidemokratie/parteivorstand/parteivorstand-2024-2026/ | Die Linke | none shown (live page as served 2026-09-24); heading "Parteivorstand 2024 - 2026" | party page: the sitting board | `linke_parteivorstand-2024-2026.html` | 69208 | `fb26c1d6d24359b744e799d2a4a21c0ad727f559f366561c6c0b25084c0790d3` |
| [LIN-SWE] | https://www.die-linke.de/partei/parteidemokratie/parteivorstand/parteivorstand-2024-2026/mitglieder-des-parteivorstandes/ines-schwerdtner/ | Die Linke | none shown (live page) | party member page (title "Parteivorsitzende") | `linke_pv-2024-2026_schwerdtner-ines.html` | 59577 | `46336660a9769ee9873dc32db9ad9d0fe40b952e9028e579c6d35f356965ac3a` |
| [FDP-23] | https://www.fdp.de/pressemitteilung/fdp-bundesparteitag-wahlergebnisse-praesidium-2 | FDP | "21.04.2023" | party press release: presidium tallies, 74th congress | `fdp_2023-04-21_wahlergebnisse-praesidium.html` | 129214 | `c2c89b6588f50656b1797b8029ca0560015f84474db6a629609a5f1763327b6c` |
| [FDP-25] | https://www.fdp.de/pressemitteilung/fdp-bundesparteitag-wahlergebnisse-praesidium-3 | FDP | "16.05.2025" | party press release: presidium tallies, 76th congress | `fdp_2025-05_wahlergebnisse-praesidium.html` | 129316 | `be8a91f507d3c5ceb1084246fc43cd45215bd170266441c276d83e470eacfba6` |
| [CDU-26] | https://www.cdu.de/app/uploads/2026/02/38-PT-Wahlergebnisse-2026.pdf | CDU Deutschlands | "38. Parteitag, Stuttgart am 20. Februar 2026" (in the PDF's own heading) | party PDF: Bundesvorstand election results (text read by inflating its streams) | `cdu_2026-02-20_38-parteitag-wahlergebnisse.pdf` | 104488 | `e657a74970fc006a15f0c4d156439bbd3ed014dbfa68659b6791bed4a9de7c1b` |
| [CSU] | https://www.csu.de/partei/vorstand/mitglieder-im-parteivorstand/dr-markus-soeder-mdl/ | CSU | none shown (live page as served 2026-09-24) | party board member page with a dated CV line | `csu_vorstand_markus-soeder.html` | 31547 | `ccc7877deae6f697103386251b344608adccb95d93c9ddb68d69c6c635c21032` |
| [AFD-BV] | https://www.afd.de/partei/bundesvorstand/ | Alternative für Deutschland | none shown (live page as served 2026-09-24; served gzip, saved decompressed) | party board page with the Bundessprecher's CV lines | `afd_bundesvorstand.html` | 446292 | `919d2985bdf9d045cb93641fc38bcde4ea5fc7e4292297644a113daf9726584c` |
| [AFD-26] | https://www.afd.de/pressemitteilung-der-afd-bundesparteitag-waehlt-neuen-bundesvorstand/ | Alternative für Deutschland | "Erfurt, 04. Juli 2026" | party press release: 17th congress re-elects the Bundessprecher (served gzip, saved decompressed) | `afd_2026-07_bundesparteitag-neuer-bundesvorstand.html` | 297182 | `144fa69c50c7d3dbaf687f6b6dd4c22e320b63061057428f78c9db29ab825349` |
| [SSW-25a] | https://www.ssw.de/themen/dirschauer-gibt-ssw-landesvorsitz-ab | SSW Landesverband | "03.03.2025" | party press release: the chair announces he will step down at an April congress; his own start (Oct 2021) stated | `ssw_2025-03-03_dirschauer-gibt-landesvorsitz-ab.html` | 47907 | `736907f0dc2bf82f72c4e591833dccbc764b2cf1c7f6de42396cc54457f2dc60` |
| [SSW-25b] | https://www.ssw.de/themen/ekstraordinaert-landsmoede-ausserordentlicher-parteitag-5-april-2025 | SSW Landesverband | congress date in title: 5 April 2025 | party page: the extraordinary congress's convening notice and agenda (an INVITATION, not a result page - see G7) | `ssw_2025-04-05_ausserordentlicher-parteitag.html` | 50805 | `012fd179d067bb107df9fd1b746a5673312c5fff454eeee68cb2467a4973ffc3` |
| [SSW-LV] | https://www.ssw.de/die-partei/landesvorstand | SSW Landesverband | none shown (live page as served 2026-09-24) | party page: the sitting Landesvorstand | `ssw_landesvorstand.html` | 43429 | `d98e0e5fb37ede9c509e353938c68d8666af41f21fdfe6a81b15412e5300c4d8` |
| [BSW-24] | https://bsw-vg.de/partei/bundesparteitag/ | Bündnis Sahra Wagenknecht | event date in text: "27. Januar 2024" | party page on the founding congress | `bsw_2024-01-27_1-bundesparteitag.html` | 96545 | `abe8afb6187b2e73a056bd46b3e0eb77483d0cf1da1d2c0e1b1c950b28e69fc5` |
| [BSW-25] | https://bsw-vg.de/amira-mohamed-ali-und-fabio-de-masi-zu-bsw-parteivorsitzenden-gewaehlt/ | Bündnis Sahra Wagenknecht | "6. Dezember 2025" | party news item: the 3rd congress elects the chairs, with tallies | `bsw_2025-12_mohamed-ali-de-masi-gewaehlt.html` | 37113 | `10f745e88d6714961a8b34f372f6fbdd212c0248e6c9b6e52b0ce54557cd3fe3` |

*(Filed 2026-09-24 by the PS-1 Germany sourcing agent. Nothing outside `ElectionsData/germany/records_by_date.md` and
`ElectionsData/germany/raw/records/` was written. No code was touched, Unity was not run, nothing was committed.)*
