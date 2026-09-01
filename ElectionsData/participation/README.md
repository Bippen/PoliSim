# Labour-force participation by age — the sources behind `ParticipationRateTable`

Fetched 2026-09-02 for F2 step 5 (the cohort spec-let's §3 derivation of participation). Every file here
is the publisher's own response, unedited; the table in `Assets/Scripts/Data/ParticipationRateTable.cs`
transcribes the values it names and nothing else.

## EU five — Eurostat `lfsa_argan`

Dissemination API, one call per country:

```
https://ec.europa.eu/eurostat/api/dissemination/statistics/1.0/data/lfsa_argan?geo=<CC>&sex=T&citizen=TOTAL&unit=PC&time=2024&age=Y15-19&age=Y20-24&age=Y25-29&age=Y30-34&age=Y35-39&age=Y40-44&age=Y45-49&age=Y50-54&age=Y55-59&age=Y60-64&age=Y65-69&age=Y70-74
```

`lfsa_argan_SE.json`, `_DE`, `_FR`, `_IT`, `_PL` — JSON-stat; the twelve values are in `value` in the
age dimension's index order (`Y15-19` = 0 … `Y70-74` = 11). The Labour Force Survey's frame is ages 15–74:
no figure exists for 75+, and the table carries 0 there and says so.

## USA — BLS Current Population Survey, LNU series (not seasonally adjusted)

Public API v1, one call per series: `https://api.bls.gov/publicAPI/v1/timeseries/data/<id>`. The 2024
annual average is the `M13` period in each response. Every id was verified against its own title page
(`https://data.bls.gov/timeseries/<id>`) before use:

| id | BLS series title |
|---|---|
| `LNU01300012` | (Unadj) Labor Force Participation Rate - 16-19 yrs. |
| `LNU01300036` | (Unadj) Labor Force Participation Rate - 20-24 yrs. |
| `LNU01300089` | (Unadj) Labor Force Participation Rate - 25-34 yrs. |
| `LNU01300091` | (Unadj) Labor Force Participation Rate - 35-44 yrs. |
| `LNU01300093` | (Unadj) Labor Force Participation Rate - 45-54 yrs. |
| `LNU01300094` | (Unadj) Labor Force Participation Rate - 55-59 yrs. |
| `LNU01300096` | (Unadj) Labor Force Participation Rate - 60-64 yrs. |
| `LNU01300097` | (Unadj) Labor Force Participation Rate - 65 yrs. & over |

⚠ `LNS11324887`, recalled as "55–64", is "16–24" — the reason ids are checked and never recalled. The
BLS `cpsaat03` table download is bot-gated (a 1.3 KB page comes back), so the series route is the one used.

## Digests (`sha256sum`, first 16 hex)

Run `sha256sum ElectionsData/participation/*.json ElectionsData/participation/bls/*.json` to re-derive;
the table's doc comment names the files, not the digests, so a refetch cannot silently disagree with prose.
