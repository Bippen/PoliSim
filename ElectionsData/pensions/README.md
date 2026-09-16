# Pension ages and payments — the sources (S1 of the backlog plan, 2026-09-12)

Fetched 2026-09-12 for PN-1 and PN-2 (`POLISIM_BACKLOG_PLAN.md`, ruled `COMPLETED.md` §474): each country's statutory retirement age with its paragraph, the OECD's pension indicators, and the replacement ratio the payment readout is gated against. Every file under `PoliSim-captures/sources/pensions/` is the authority's own page or dataset, unedited; every figure in the CSVs beside this README is EXTRACTED from those files by pattern in `Tools/pension_prep.pl` (`perl Tools/pension_prep.pl PoliSim-captures/sources ElectionsData/pensions`) - a pattern that does not match stops the script, so nothing here is filled from memory. Nothing in the runtime reads these files yet - PN-1's statute class will.

## The statutes, one rule per country (`statutory_ages.csv`)

| country | rule | the paragraph | the figure | the source on disk |
|---|---|---|---|---|
| Sweden | life-expectancy indexed (riktålder) | Socialförsäkringsbalken (2010:110) 2 kap. 10 a–10 c §§ (lag 2019:649): 65 + two thirds of the gain in remaining life expectancy at 65 since 1994, rounded to whole years, in force the sixth year after its calculation | 67 from 2026 to 2032 (Pensionsmyndigheten) | `se_sfb_2010_110.html` (lagen.nu), `se_pensionsmyndigheten_riktalder.html` |
| Germany | scheduled | SGB VI § 35 (the Regelaltersgrenze at 67) and § 235 Abs. 2 (the transition by birth year, 65 for 1946 to 66 years 10 months for 1963; 1964 and later at 67 - reached in 2031) | 67 | `de_sgb6_35.html`, `de_sgb6_235.html` (gesetze-im-internet.de) |
| France | scheduled | code de la sécurité sociale L161-17-2 as amended by loi n° 2023-270: 62 years 9 months for those born 1963 to March 1965, then three months a birth-year to 64 from 1969 | 64 (the end of the schedule) | `fr_service_public_F14043.html` (the authority's page; the code article itself returns 403 to this machine - BILLED) |
| Italy | life-expectancy indexed | decreto-legge 201/2011 art. 24 (the ISTAT adjustment every two years): 67 through 2026, 67 years 1 month in 2027, 67 years 3 months in 2028 | 67 | `it_inps_2027_2028.html` (INPS's notice of March 2026; normattiva is a script shell to this machine - BILLED) |
| Poland | fixed | ustawa o emeryturach i rentach z FUS art. 24 ust. 1: 60 for women, 65 for men | 65 (men; the women's 60 is the deviation the one-age model states) | `pl_arslege_art24.html` (the consolidated text Dz.U. 2024.1631; ISAP is behind a bot wall to this machine) |
| United States | scheduled | Social Security Act § 216(l), 42 U.S.C. 416(l)(1): 67 for those attaining early retirement age after 2021 - born 1960 or later | 67 | `us_42usc416.html` (Cornell LII; ssa.gov refuses this machine - BILLED) |

## The OECD's indicators (`oecd_pag_2024.csv`)

Pensions at a Glance (`OECD.ELS.SPD:DSD_PAG@DF_PAG`, SDMX, fetched 2026-09-12), the latest observation per country, measure and sex: the current and future normal retirement age for a labour-market entrant at 22 (CRPLF22 / FRPLF22), the gross and net replacement rate at the average wage (GPRR100 / NPRR100), the effective labour-market exit age (ELMEA), public pension expenditure as a share of GDP (PEP, 2021). These are the plan's "sourced seeds" for PN-1's captions and PN-2's gate; the statutes above are the rule.

## The replacement ratio (`replacement_ratio_2024.csv`)

Eurostat `ilc_pnp3` 2024 (sex T): the aggregate replacement ratio - the median individual gross pension of 65–74 over the median gross earnings of 50–59, excluding other social benefits - the five: DE 0.49, FR 0.61, IT 0.79, PL 0.60, SE 0.59. PN-2's seed gate for the five; the USA's gate is OECD's net replacement rate (NPRR100).

## The average pension (`average_pension_2022.csv`)

ESSPROS, 2022, the five: pension expenditure (`spr_exp_pens`, million EUR, means-tested and not, all schemes) over pension beneficiaries (`spr_pns_ben`, persons, sex T, each person counted once) - the DERIVED average benefit per beneficiary per year, in the unit PN-2's readout prints (the line over its headcount). Two rows per country: old-age pensions alone (the beneficiary file's OLD_TOT = old-age + anticipated old-age + partial, against the same parts of the expenditure file; a part a country does not report is named in the row's source cell and left out, never filled) and every pension type together (old-age, survivors', disability, early retirement). PN-2 names which of the two its Pensions line is before it gates against either; the USA is not in ESSPROS and gates on OECD's net replacement rate.

## The pension line's two sources, same year (`pension_line_sources_2019_2024.csv`; PN-4, fetched 2026-09-16)

The seed of every EU pension line is COFOG - Eurostat `gov_10a_exp`, sector S13 general government, function GF10.02 "Old age", item D62 social benefits in cash, % of GDP, 2024 (`WorldFactory`'s seed comments: Germany 9.0, France 12.4, Italy 13.6, Poland 10.4, Sweden 7.0 - re-read from the API 2026-09-16, all five confirmed). PN-2's gate held those lines against ESSPROS - `spr_exp_pens`, which counts EVERY scheme, occupational and other non-government schemes included - and held them in euro, the 2026 line against the 2022 band. This table puts the two sources side by side in the only form they can be compared in: the same year, as shares of GDP, for every year both publish (2019–2024; France's ESSPROS 2024 is not yet published and the flags column says so). Beside them: ESSPROS's all-types figure (the upper edge no perimeter can cross), nominal GDP in euro and national currency (`nama_10_gdp` B1GQ, current prices), and the beneficiary counts (`spr_pns_ben`, sex T, old-age and all types). `PensionPaymentDiagnostic` reads this file on the simulation bar.

What it shows, 2024 (France 2023): COFOG's old-age cash against ESSPROS's old-age .. all-types, % of GDP - Germany 9.0 against 9.38 .. 11.83, France 12.0 against 12.07 .. 14.61, Italy 13.6 against 12.85 .. 15.91, Poland 10.4 against 9.58 .. 11.61, Sweden 7.0 against 9.92 .. 10.81. The difference between ESSPROS's old-age figure and COFOG's is the PERIMETER - old-age pensions paid outside general government - and Sweden's 2.9 points of it (2.5 to 2.9 in every year of the table) are its collectively agreed occupational pensions. Poland's line, ×1.209 above the 2022 euro band in §518, is INSIDE the same year's band: the 2022→2026 gap was the year (the model's GDP in euro over 2022's, and the share's own 2022→2024 rise from 8.8 to 10.4 as Poland's indexation outran its GDP).

## What is BILLED

The code articles themselves for France (legifrance 403), Italy (normattiva) and the USA (ssa.gov 403) - each is stated through the authority that publishes it; Poland's ISAP - stated through the consolidated text a legal publisher carries. A later fetch that lands the article replaces the row's source and changes nothing else.

## The files, by digest (sha256, first sixteen hex; under `PoliSim-captures/sources/pensions/`)

| file | sha256 (16) | size |
|---|---|---|
| `de_sgb6_235.html` | `7e28241d059e66b7` | 11503 bytes |
| `de_sgb6_35.html` | `a052a5b5df102262` | 3959 bytes |
| `fr_service_public_F14043.html` | `8d4ddfc77258d2bb` | 89294 bytes |
| `gov_10a_exp_gf10_2019_2024.json` | `a1ab2de4034f1acf` | 10007 bytes (PN-4, 2026-09-16: S13, GF10/GF10.01/GF10.02/GF10.03, D62 and TE, PC_GDP and MIO_EUR, the five, 2019–2025) |
| `ilc_pnp3_2024.json` | `bba328e4bdee0253` | 3229 bytes |
| `it_inps_2027_2028.html` | `4211a1bb3ed14198` | 660915 bytes |
| `nama_10_gdp_2019_2025.json` | `4cd7ce1ff5e26d5a` | 4358 bytes (PN-4, 2026-09-16: B1GQ, CP_MEUR and CP_MNAC, the five) |
| `oecd_pag_2023_six.csv` | `69ae89ea6d10c059` | 159039 bytes |
| `pl_arslege_art24.html` | `81b1729b5f2892ef` | 42890 bytes |
| `se_pensionsmyndigheten_riktalder.html` | `6cf08870593b84c0` | 101150 bytes |
| `se_sfb_2010_110.html` | `a648e7d642a62d05` | 7429389 bytes |
| `spr_exp_pens_2019_2024.json` | `4ef63025e3a73806` | 19115 bytes (PN-4, 2026-09-16: every pension type, means-tested and not, PC_GDP and MIO_EUR, the five, 2019–2024) |
| `spr_exp_pens_2022.json` | `2ec27b7dda56a72b` | 18629 bytes |
| `spr_pns_ben_2019_2024.json` | `48ba71c309304238` | 11983 bytes (PN-4, 2026-09-16: persons, sex T, all schemes, the five, 2019–2024) |
| `spr_pns_ben_2022.json` | `a6544d9eb31fdd8f` | 8134 bytes |
| `us_42usc416.html` | `bd04463b757e1358` | 288079 bytes |

