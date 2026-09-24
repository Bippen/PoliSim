# Sweden - the ordinary election calendar (PS-2 / CL-4, `COMPLETED.md` §619)

Fetched 2026-09-24 from riksdagen.se, the Riksdag's own consolidated statute texts (Svensk författningssamling), stored byte for
byte under `raw/calendar/` with `SHA256SUMS` beside them - the consolidations "t.o.m. SFS 2022:1600" (regeringsformen) and "t.o.m. SFS 2026:989" (vallagen), as the pages stamp themselves.

| id | statute | the clause, verbatim | file |
|---|---|---|---|
| [RF-3-3] | Regeringsformen (1974:152) 3 kap. 3 § | "Ordinarie val till riksdagen hålls vart fjärde år. Lag (2010:1408)." | `raw/calendar/kungorelse-1974152-om-beslutad-ny-regeringsform_sfs-1974-152.html` |
| [VL-1-3] | Vallagen (2005:837) 1 kap. 3 § | "Ordinarie val till riksdagen och ordinarie val till region- och kommunfullmäktige ska hållas samma dag. Valdag ska vara den andra söndagen i september." | `raw/calendar/vallag-2005837_sfs-2005-837.html` |

**The rule in code** (`WorldClock.TryNextPollingDay`, `WorldClock.SecondSundayOfSeptember`): the ordinary election is the second Sunday of
September every fourth year, the cycle anchored on the latest election of record (13 September 2026 - `2026/returns_2026.md`). It
reproduces the record's own polling days - 9 September 2018 [VAL-18], 11 September 2022 (`returns_2022.md`), 13 September 2026 - and
gives 8 September 2030 next (1 September 2030 is a Sunday). Asserted by `PollingDayDiagnostic` in the cheap bar.

**Not modelled here, stated:** an extra election (regeringsformen 3 kap. 11 §, and the one after four failed nominations of a prime
minister, 6 kap. 5 §) - the dissolution is stage 3's (the political-system spec's §5.4). The other five countries' calendars are their
stages'; until then each holds no election in a run and its chamber stands as of record (§618's ruling 4).
