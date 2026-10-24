# The pre-run fiscal years - the balance the first count's fiscal term reads [SOURCED]

Class: SOURCED figures; the game years they are read into are the game's own rule (Elias's ruling K62 of 2026-10-23, `docs/RULINGS_2026-10-23.md`).
The game reads them from `Assets/Scripts/Elections/Generated/FiscalBalanceRecord.cs`, generated from `fiscal_balance.csv` by
`Tools/fiscal_balance_prep.pl`, which reads the two raw files under `raw/fiscal_balance/`; `FiscalBalanceRecordCheck` re-reads the raw files and
the CSV against the table.

## The ruling

> **K62.** The pre-run fiscal years are seeded from a sourced annual balance series (Eurostat, the US federal budget).

Before it, the first count of every start carried no fiscal term: Brender & Drazen's two variables read closed fiscal years, and no game year
had closed (`COMPLETED.md` section 880, its ask A1).

## The series

| countries | source (saved whole under `raw/fiscal_balance/`) | definition | the year |
|---|---|---|---|
| Sweden, Germany, France, Italy, Poland | Eurostat `gov_10dd_edpt1` (the EDP notification tables), JSON-stat API | `na_item` B9 net lending (+) / net borrowing (-), `sector` S13 general government, `unit` PC_GDP, annual; Eurostat's status flag carried | the calendar year |
| USA | OMB, Budget of the U.S. Government FY 2027, Historical Tables, Table 1.2 (`hist01z2_fy2027.xlsx`) | Total / Surplus or Deficit (-) as a percentage of GDP: the unified federal budget | the federal fiscal year, from 1 October of the year before its number |

The perimeters are the ones each country's book models: general government for the EU five, the federal budget for the USA. Brender & Drazen's
own Balance is the central government's (the s880 review's L8) - a stated difference, not corrected.

Fetched 2026-10-24 with curl, byte-exact; `raw/fiscal_balance/fetch_log.txt` holds each URL, its time and status, and each response's headers are
saved beside its file. No host refused. Digests: `raw/fiscal_balance/SHA256SUMS.txt`.

## How a count reads them (`FiscalVote.SeedPreRun` and `FiscalVote.Judge`, their declared readings 4, 8 and 8b)

- **The book a world enters with (K62):** the four newest source years that had closed by the start, each with its own opening and closing day,
  at its published ratio carried at the start's nominal GDP. They are read only after the first game year closes, beside the game's own years.
- **The first count (K98, K100, K105, 2026-10-24):** until the first game year closes, every year is read off the record by the ELECTION's
  calendar year E: BAL0 is E (for the USA the federal fiscal year numbered E, which closes on 30 September, before November's polling day), and
  BAL-1..BAL-4 are E-1..E-4 - only years older than BAL0's, so no year is read twice - each one closed by the election's day. A year open at the
  start but closed before the count is read (Germany's 2025 count reads 2024 as BAL-1); a year still open on the count's day is never read,
  E itself excepted. A campaign's opening is judged for its election's year, not its own. Where the source does not yet publish E, the first
  count carries no election-year variable.
- **A declared hindsight (the review's L5):** E's figure is a full-year one, so it closes - and is published - after polling day (Germany's
  2025 figure after the 23 February count, Poland's 2023 after 15 October, the USA's FY2024 on 30 September before the 5 November count is the
  one that does not). K100 and K98 rule it so; it is the record's year, not a forecast.
- **K63 / K99 / K101, tenure:** a count reads only the years that opened on or after the day the governing party's tenure began - the whole
  year. The tenure follows the governing party (the prime minister's party, the president's party) through re-elections and re-formations,
  and restarts where another party leads; at a start it is read off the governments of record (Poland's PiS rows from 16 November 2015, K104).
- What each start and each E1 reference world reads is printed by `FiscalVoteDiagnostic` (its section 8), never transcribed here.
