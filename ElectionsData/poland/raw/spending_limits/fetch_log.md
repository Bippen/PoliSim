# fetch_log - ElectionsData/poland/raw/spending_limits/ (PL-C, K167: the Kodeks wyborczy's spending limits cap the purse), 2026-10-28

Command: `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -o <file> <url>`; bytes stored as received, no re-encoding. Status is the final status after redirects. Digests in `SHA256SUMS.txt`. The host is the Sejm's own ELI API (api.sejm.gov.pl), which serves the same Dziennik Ustaw and Monitor Polski texts ISAP does; no host refused.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-28T20:18Z | 200 | 1142 | eli_meta_DU_2023_132.json | https://api.sejm.gov.pl/eli/acts/DU/2023/132 |
| 2026-10-28T20:18Z | 200 | 1134 | eli_meta_DU_2024_590.json | https://api.sejm.gov.pl/eli/acts/DU/2024/590 |
| 2026-10-28T20:18Z | 200 | 1036 | eli_meta_DU_2025_1496.json | https://api.sejm.gov.pl/eli/acts/DU/2025/1496 |
| 2026-10-28T20:18Z | 200 | 1046 | eli_meta_DU_2025_1501.json | https://api.sejm.gov.pl/eli/acts/DU/2025/1501 |
| 2026-10-28T20:18:35Z | 200 | 5260 | eli_DU_2023_132.html | https://api.sejm.gov.pl/eli/acts/DU/2023/132/text.html |
| 2026-10-28T20:18:36Z | 200 | 4942 | eli_DU_2024_590.html | https://api.sejm.gov.pl/eli/acts/DU/2024/590/text.html |
| 2026-10-28T20:18:36Z | 200 | 347349 | eli_DU_2023_132.pdf | https://api.sejm.gov.pl/eli/acts/DU/2023/132/text.pdf |
| 2026-10-28T20:18:37Z | 200 | 205438 | eli_DU_2024_590.pdf | https://api.sejm.gov.pl/eli/acts/DU/2024/590/text.pdf |
| 2026-10-28T20:18:38Z | 200 | 155141 | eli_DU_2025_1496.pdf | https://api.sejm.gov.pl/eli/acts/DU/2025/1496/text.pdf |
| 2026-10-28T20:18:39Z | 200 | 154119 | eli_DU_2025_1501.pdf | https://api.sejm.gov.pl/eli/acts/DU/2025/1501/text.pdf |
| 2026-10-28T20:19:34Z | 200 | 903 | eli_meta_MP_2023_867.json | https://api.sejm.gov.pl/eli/acts/MP/2023/867 |
| 2026-10-28T20:19:35Z | 200 | 1044 | eli_meta_MP_2025_66.json | https://api.sejm.gov.pl/eli/acts/MP/2025/66 |
| 2026-10-28T20:19:35Z | 200 | 1136 | eli_meta_DU_2024_605.json | https://api.sejm.gov.pl/eli/acts/DU/2024/605 |
| 2026-10-28T20:19:46Z | 200 | 176779 | eli_MP_2023_867.pdf | https://api.sejm.gov.pl/eli/acts/MP/2023/867/text.pdf |
| 2026-10-28T20:19:47Z | 200 | 147450 | eli_MP_2025_66.pdf | https://api.sejm.gov.pl/eli/acts/MP/2025/66/text.pdf |
| 2026-10-28T20:19:47Z | 200 | 180707 | eli_DU_2024_605.pdf | https://api.sejm.gov.pl/eli/acts/DU/2024/605/text.pdf |
| 2026-10-28T20:19:48Z | 200 | 4932 | eli_DU_2024_605.html | https://api.sejm.gov.pl/eli/acts/DU/2024/605/text.html |

## What each file says (read, the words quoted)

The statute is the Kodeks wyborczy already on disk (`../electoral_code/eli_DU_2011_112_kodeks_wyborczy.html`, the act as published, digest in that folder):
- **Art. 199 § 1 (the Sejm):** "komitety wyborcze mogą wydatkować na agitację wyborczą wyłącznie kwoty ograniczone limitami ... 1) wysokość limitu wyznaczona jest kwotą 82 groszy przypadającą na każdego wyborcę w kraju ujętego w rejestrze wyborców; 2) limit wydatków dla danego komitetu oblicza się według wzoru: L = (w × k × m)/460 ... m - łączną liczbę posłów wybieranych we wszystkich okręgach wyborczych, w których komitet zarejestrował listy kandydatów." § 2: the PKW publishes w, the register "według stanu na koniec kwartału poprzedzającego". Art. 200 § 1: the Minister of Finance raises k by regulation where prices rise over 5 %.
- **Art. 327 § 1 (the President):** "Komitety wyborcze mogą wydatkować na kampanię wyborczą wyłącznie kwoty ograniczone limitem, którego wysokość jest wyznaczona kwotą 60 groszy przypadającą na każdego wyborcę w kraju ujętego w rejestrze wyborców." § 3: raised by regulation as art. 200.

The regulations raising k (each "wchodzi w życie po upływie 14 dni od dnia ogłoszenia"; entry into force from each meta JSON's `entryIntoForce`):
- `eli_DU_2023_132` (Sejm, 11 Jan 2023, in force 2023-02-02): "podwyższa się do kwoty 1 złoty 10 groszy".
- `eli_DU_2024_605` (Sejm, 18 Apr 2024, in force 2024-05-07): "podwyższa się do kwoty 1 złoty 17 groszy".
- `eli_DU_2025_1496` (Sejm, 28 Oct 2025, in force 2025-11-15): "podwyższa się do kwoty 1 złoty 24 grosze" (PDF only; read by inflating its Flate streams and mapping its glyph ids, scratchpad `pdfraw.pl`).
- `eli_DU_2024_590` (President, 15 Apr 2024, in force 2024-05-03): "podwyższa się do kwoty 85 groszy".
- `eli_DU_2025_1501` (President, 28 Oct 2025, in force 2025-11-15): "podwyższa się do kwoty 90 groszy" (PDF only, read as above).

The register's size w, as the PKW announced it under art. 199 § 2 and art. 327 § 2 (PDF only, read as above):
- `eli_MP_2023_867` (M.P. 2023 poz. 867, the PKW's komunikat of 14 Aug 2023, for the Sejm of 15 Oct 2023): "liczba wyborców ujętych w rejestrach wyborców na terenie całego kraju według stanu na dzień 30 czerwca 2023 r. wynosiła 29 097 503 osoby".
- `eli_MP_2025_66` (M.P. 2025 poz. 66, the komunikat of 16 Jan 2025, for the President of 18 May 2025): "według stanu na dzień 31 grudnia 2024 r. wynosiła 28 945 200 osób".

Not fetched: an earlier presidential regulation (DU 2023 poz. 135) - no presidential election the game holds falls while it was in force (the presidential start opens on 15 Jan 2025, after 2024/590 entered into force).
