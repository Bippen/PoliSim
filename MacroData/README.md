# Government consumption — the identity's G at the seed (T-3, form A, 2026-09-15)

Fetched 2026-09-15 for T-3 (`COMPLETED.md` §376 sourced the figures on the cross-check gate, §505 measured the two forms, form A ruled the same day): general government final consumption expenditure as a share of GDP in the seed year, for the six countries. Every file under `PoliSim-captures/sources/govcons/` is the authority's own response, unedited; every figure in `government_consumption_2023.csv` is DERIVED from those files by `Tools/govcons_prep.pl` (`perl Tools/govcons_prep.pl ../PoliSim-captures/sources/govcons MacroData`), never typed. `GovernmentConsumptionCatalogGenerator` turns the CSV into `Assets/Scripts/Data/Generated/GovernmentConsumptionData.cs`, and `GeneratedCatalogCheck` holds that table to the CSV's digest.

## The file

| column | what |
|---|---|
| `consumption`, `gdp` | the levels the share is computed from: Eurostat nama_10_gdp P3_S13 and B1GQ in national currency, millions (CP_MNAC) for the five; BEA NIPA A955RC (government consumption expenditures, table 3.9.5 line 2) and A191RC (GDP, table 1.1.5 line 1) in USD millions for the USA |
| `share_pct` | consumption over GDP, unrounded - the figure the model reads |
| `published_pct` | the share the publisher prints: Eurostat nama_10_gdp PC_GDP to one decimal for the five (the tool stops if the levels' share is more than 0.05 from it); the World Bank's NE.CON.GOVT.ZS for the USA (the tool stops if it is not BEA's ratio) |
| `federal_share_pct` | the USA only: BEA A957RC, federal consumption, over GDP |
| `flag` | Eurostat's provisional flag (`p`) |

## ⚠ The USA's reading is one source

P3_S13 is GENERAL government (13.54 % of GDP in 2023); the model's USA spending lines are the FEDERAL budget (`WorldFactory.SeedUsaSpendingLines`, real federal dollars; discretionary lines 6.04 % of GDP at the seed, §505's table), and BEA's federal consumption alone is 4.82 %. The World Bank's figure is BEA's ratio to ten digits, so the "second reading" §376 counted is the same number in a second copy. BEA's own flat files are on disk now (they downloaded without a key); what stays missing is an independent reading of US general government consumption.

## The files, by digest (sha256, first sixteen hex; under `PoliSim-captures/sources/govcons/`)

| file | sha256 (16) | size |
|---|---|---|
| `eurostat_nama_10_gdp_P3_S13_B1GQ_CP_MNAC_2023_2024.json` | `1a2317063f72b799` | 3484 bytes |
| `eurostat_nama_10_gdp_P3_S13_PC_GDP_2023_2024.json` | `2d4eb251112f244b` | 3197 bytes |
| `worldbank_NE.CON.GOVT.ZS_2023_2024.json` | `a18b8e184942b71d` | 3080 bytes |
| `bea_NipaDataA.txt` | `93b9fcb111500d71` | 12044093 bytes |
| `bea_SeriesRegister.txt` | `a4ea3f149ac20167` | 971461 bytes |
| `bea_TablesRegister.txt` | `f5395223c4433235` | 30449 bytes |
| `bea_NipaDataA.headers.txt` | `e96aae9b4919950f` | 457 bytes |
| `bea_SeriesRegister.headers.txt` | `ef6d8fb932fe9d0f` | 455 bytes |
| `bea_TablesRegister.headers.txt` | `9c5e7443ae6b3a04` | 453 bytes |

Eurostat's release is `updated 2026-09-08T11:00:00+0200`; the World Bank's `lastupdated 2026-07-13`; BEA's files `Last-Modified 2026-08-26 12:30:03 GMT` (the saved headers).
