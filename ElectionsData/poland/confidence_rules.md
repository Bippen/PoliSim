# Poland - the confidence rules (Konstytucja RP of 1997-04-02, Art. 158 and Art. 160)

Fetched 2026-10-27 for COAL-1 (Elias's ruling K133: "Poland's Konstytucja Art. 158 and Art. 160 fetched and sourced inside COAL-1") from the
Trybunał Konstytucyjny's HTML text of the Constitution - the page `records_by_date.md` already cites as [TK-KONST] - with
`curl -sS -L -A "Mozilla/5.0"`, stored byte for byte as `raw/confidence/trybunal_konstytucja.html` with `SHA256SUMS.txt` and
`fetch_log.txt` beside it. The page answered 200. It differs from the copy of 2026-09-24 under `raw/records/` only in the site's
navigation and a stylesheet cache stamp: the span from "Artykuł 158" to "Artykuł 163" is byte-identical in the two files
(2,575 bytes, the same MD5), so [TK-KONST] and the new id below quote the same text. Every quote is verbatim (the page's tabs
between paragraphs collapsed to a space); the English after each is my gloss, not the page's. Nothing from memory. Class:
SOURCED `[PROVISIONAL]` until a second session re-verifies (R-K9).

Quote ids: **[TK-KONST-C:article]**, the file above.

## Art. 158 - the constructive vote of no confidence [TK-KONST-C:158]

> "Artykuł 158 Sejm wyraża Radzie Ministrów wotum nieufności większością ustawowej liczby posłów na wniosek zgłoszony przez co najmniej 46 posłów i wskazujący imiennie kandydata na Prezesa Rady Ministrów. Jeżeli uchwała została przyjęta przez Sejm, Prezydent Rzeczypospolitej przyjmuje dymisję Rady Ministrów i powołuje wybranego przez Sejm nowego Prezesa Rady Ministrów, a na jego wniosek pozostałych członków Rady Ministrów oraz odbiera od nich przysięgę. Wniosek o podjęcie uchwały, o której mowa w ust. 1, może być poddany pod głosowanie nie wcześniej niż po upływie 7 dni od dnia jego zgłoszenia. Powtórny wniosek może być zgłoszony nie wcześniej niż po upływie 3 miesięcy od dnia zgłoszenia poprzedniego wniosku. Powtórny wniosek może być zgłoszony przed upływem 3 miesięcy, jeżeli wystąpi z nim co najmniej 115 posłów."

*(gloss: the Sejm expresses no confidence in the Council of Ministers by a majority of the statutory number of deputies, on a motion
moved by at least 46 deputies that names a candidate for prime minister. If the Sejm adopts the resolution, the President accepts the
cabinet's resignation, appoints the new prime minister the Sejm chose and, on his motion, the other ministers, and swears them in.
(2) The motion may be put to the vote no sooner than 7 days after it was moved. A repeated motion may be moved no sooner than 3 months
after the previous one - sooner if at least 115 deputies move it.)*

## Art. 160 - the vote of confidence the prime minister asks for [TK-KONST-C:160]

> "Artykuł 160 Prezes Rady Ministrów może zwrócić się do Sejmu o wyrażenie Radzie Ministrów wotum zaufania. Udzielenie wotum zaufania Radzie Ministrów następuje większością głosów w obecności co najmniej połowy ustawowej liczby posłów."

*(gloss: the prime minister may ask the Sejm for a vote of confidence in the Council of Ministers; it is given by a majority of votes
with at least half of the statutory number of deputies present.)*

## What the game reads (COAL-1)

- `ConfidenceProcedure.RulesOf(Poland)` is the Sejm's rule, a constructive vote of Art. 158's shape: the motion names its successor, carries
  by a majority of the statutory number of deputies (Art. 96 ust. 1's 460, whatever is seated), and the successor's cabinet takes office the
  same day. Who moves it: at least 46 deputies, read as the members who would choose the candidate (the Bundestag's quarter is read so).
- The AI moves an Art. 158 vote only after a coalition partner's exit (Elias's ruling K131); the daily weighing an AI party makes in the
  Riksdag and the Bundestag (ruling (2), §641) is not ruled for Poland and is not run there.
- A PREMISE, stated: Art. 158 ust. 2's seven days between the motion and its vote are not waited - the vote is taken on the motion's day,
  as Germany's forty-eight hours (Art. 67 (2) GG) are.
- Not modelled, stated: the three-month bar on a repeated motion (the AI moves only a motion that carries, and a carried motion installs a
  new cabinet), and Art. 160's vote of confidence - no verb asks for it.
- The neighbouring articles already quoted in `records_by_date.md` (Art. 154, 155, 162) are not repeated here.
