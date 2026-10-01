# The pre-start record — the incumbent's term the perceived economy reads [SOURCED]

Class: SOURCED figures, the window and the release dates the game's own rules (Elias's ruling of 2026-10-01, item 0; `COMPLETED.md` §709).
The game reads these figures from `Assets/Scripts/Elections/Generated/PreStartRecord.cs`, generated from `prestart_record.csv` by
`Tools/prestart_record_prep.pl`, which reads the five raw files under `raw/`; `PreStartRecordDiagnostic` re-reads the CSV against the table.

## The ruling

> *First, measure the pre-start record. Report what the perceived-economy reading holds at each country's start date, and whether it draws on
> any history from before the start. If the incumbent's term before the start is missing, seed it from sourced macro data for the period since
> the previous election, the retrospective economy Duch & Stevenson measure, per country. Report every backtest before and after, Germany 2025
> and Sweden first. Tune nothing.*

## What was measured first

The perceived-economy reading (`PerceivedPerformance.Perceived`) averages scores of the **latest published** unemployment and inflation
(growth is a third component, but every caller passes it as null). At every start, before this record:

- **nothing from before the start was read**: unemployment and inflation were never seeded (only GDP's one inherited quarter, which the
  reading does not take), and every print whose month began before the start is suppressed;
- so the reading at every start was **50.0, the neutral by default, with no component**; and a German game reached its campaign's opening
  (29 December 2024) with still nothing published, so the government's record was judged at 50 - no shift at all.

## The series

| series | countries | source (saved whole under `raw/`) | definition |
|---|---|---|---|
| unemployment | DE, SE, IT, PL, FR | Eurostat `une_rt_m`, fetched 2026-10-01 (updated 2026-09-22) | seasonally adjusted, total, 15-74, % of the active population |
| inflation | DE, SE, IT, PL, FR | Eurostat `prc_hicp_manr` to 2025-12 (updated 2026-02-06); `prc_hicp_minr` from 2026-01 (ECOICOP ver. 2, updated 2026-09-17) | HICP all items, annual rate of change; the two tables agree within 0.1 on every overlapping month checked (2024-06, 2024-10, 2025-06, 2025-12) |
| unemployment | USA | BLS `LNS14000000`, fetched 2026-10-01 | U-3, seasonally adjusted |
| inflation | USA | BLS `CUUR0000SA0`, fetched 2026-10-01 | CPI-U all items, not seasonally adjusted; the 12-month change computed from the index, one decimal as BLS prints it |

**October 2025 (USA) is left out**: BLS prints "-" for both series, "Data unavailable due to the 2025 lapse in appropriations". Never read as
zero.

Raw files and digests: `raw/SHA256SUMS.txt`.

## The window (the game's rules, `PublicationSystem.SeedPreStartRecord`)

- **Opens** at the month of the election that opened the incumbent's term (`PreStartWindowOpens`): the chamber's election in a parliamentary
  country; in the USA the presidential election (the president's party carries the economic vote in Duch & Stevenson's coding; a midterm
  elects no executive).
- **Closes** at the last month the game's own calendar would have published **before** the start (`ReleaseCalendar.MonthlyReleaseDate`: a
  monthly figure describes the month before the one it is published in; unemployment on the first Friday, inflation on the 12th in the USA
  and on the last working day elsewhere). Nothing published on or after the start is seeded.
- Each figure is one Final entry, as an inherited figure is. A series already holding entries (a save) is left as it is.

| start | the term opened | unemployment months | inflation months | the latest at the start |
|---|---|---:|---:|---|
| Germany 2024-11-06 | 2021-09-26 | 38 (2021-09 .. 2024-10) | 37 (.. 2024-09) | 3.4 % / 1.8 % |
| Sweden 2026-01-18 | 2022-09-11 | 40 (2022-09 .. 2025-12) | 39 (.. 2025-11) | 8.8 % / 2.2 % |
| USA 2024-03-12 | 2020-11-03 | 40 (2020-11 .. 2024-02) | 39 (.. 2024-01) | 3.9 % / 3.1 % |
| Italy 2022-07-21 | 2018-03-04 | 52 (2018-03 .. 2022-06) | 51 (.. 2022-05) | 8.2 % / 7.3 % |
| Poland 2023-02-19 | 2019-10-13 | 40 (2019-10 .. 2023-01) | 39 (.. 2022-12) | 2.7 % / 15.3 % |
| France 2024-07-18 | 2024-07-07 | 0 | 0 | none - the term is eleven days old and nothing for it was published before the start |

Only the perceived reading reads these two series; nothing in the economy does (§709's measurement of every reader).
