# Poland - the 2023 Trzecia Droga list's seats by party, and TD's position (Elias's ruling D2)

The ruling (2026-10-03): *"TD gets the seat-weighted average of PSL's and Polska 2050's positions, weighted by the seats each party won
on the 2023 TD list (sourced by read). This is the rule Italy's joint lists already use."* (`COMPLETED.md` §621's joint-list rule.)

## 1. The seats each party won on the list - read

Source: the KBW's candidate file for the 2023 Sejm, `raw/td_list_2023/kandydaci_sejm_utf8.csv` (inside `kandydaci_sejm_csv.zip`,
https://danewyborcze.kbw.gov.pl/dane/2023/sejmsenat/kandydaci_sejm_csv.zip; SHA-256 in `raw/td_list_2023/SHA256SUMS.txt`). Each row
is a candidate; the columns read are *Nazwa komitetu*, *Przynależność do partii* (party membership) and *Czy przyznano mandat* (seat
won). The 460 rows marked *Tak* sum by committee to the PKW's notice (Dz.U. 2023 poz. 2234): PiS 194, KO 157, TD 65, NL 26, Konf 18.

The 65 elected on *KOALICYJNY KOMITET WYBORCZY TRZECIA DROGA POLSKA 2050 SZYMONA HOŁOWNI - POLSKIE STRONNICTWO LUDOWE*, by the
membership the file states:

| membership as the file writes it | seats | counted as |
|---|---|---|
| członek partii politycznej: Polska 2050 Szymona Hołowni | 30 | Polska 2050 |
| członek partii politycznej: PL2050 Szymona Hołowni | 1 | Polska 2050 (the same party, abbreviated) |
| członek partii politycznej: Polskie Stronnictwo Ludowe | 26 | PSL |
| członek partii politycznej: PSL | 1 | PSL (the same party, abbreviated) |
| nie należy do partii politycznej | 4 | no party - no weight |
| członek partii politycznej: Centrum dla Polski | 3 | no CHES row - no weight |

**Polska 2050 31, PSL 27.** The seven others carry no CHES position of their own and are left out of the weights, stated. (CHES 2024's
own `seat` column gives the clubs as they sat - PSL 32, Polska 2050 33 - not the seats won on the list; the ruling's weights are the list's.)

## 2. The positions averaged

Each of the fourteen CHES 2024 fields the roster carries (`positions/raw/CHES_2024_final_v2.csv`, Poland's rows `PSL` and
`Polska 2050`, at two decimals as every roster row is typed) is averaged with the weights above, field by field
(`PartySystems.JointList`). The check (`PolishSejmAllocationDiagnostic`) re-reads both files each run: the weights from the KBW file,
the member rows from CHES, and TD's position as their mean.
