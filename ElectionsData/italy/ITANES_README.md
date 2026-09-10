# ITANES 2013 and 2018 — the two waves E-1 asked for, verified and inventoried (2026-09-10)

**Terms (recorded at `COMPLETED.md` §149 from the portal, and unchanged):** ITANES's datasets are free for
scientific, non-commercial research on Italian elections, behind a registered account on `itanes.it`, with
citation. The account is Elias's; the download was his (E-1). Nothing in these files is redistributed by this
repo — the two CSVs beside this file are DERIVED cross-tabs, not the microdata.

## The packs, verified by content

| pack | bytes | SHA-256 | `unzip -t` |
|---|--:|---|---|
| `AssetPackArchive/Itanes-2013.zip` | 3 225 465 | `808c2799c2c16f8185da2d5fc7bba4ec7b51ddf18f90e08c4610efe8675ae2d6` | no errors |
| `AssetPackArchive/Itanes-2018.zip` | 2 300 992 | `faf78e46484b933be38119df694780128cbde5aea2de33871445cc172f7542da` | no errors |

Every member's format read from its first bytes, not its extension:

| member | bytes | first bytes | format | SHA-256 (first 16) |
|---|--:|---|---|---|
| `Itanes 2013/ITA2013_(itvers2013_11_29).dta` | 1 532 206 | `73 02 01 00 7d 01 e4 05` | Stata 115 (LOHI), 381 vars × 1 508 obs | `22929536cb19d992` |
| `Itanes 2013/ITA2013_(itvers2013_11_29).sav` | 1 046 309 | `$FL2` | SPSS system file | — |
| `Itanes 2013/ITA2013_campionamento_2014_08_07.pdf` | 302 952 | `%PDF-1.5` | PDF — the sampling note | — |
| `Itanes 2013/ITA2013_introCapi_English_2014_08_07.pdf` | 399 802 | `%PDF-1.5` | PDF | — |
| `Itanes 2013/ITA2013_introCapi_Italiano_2014_08_07.pdf` | 470 366 | `%PDF-1.5` | PDF | — |
| `Itanes 2013/ITA2013_Questionario(CAPI)_2014_08_07.pdf` | 982 650 | `%PDF-1.5` | PDF — the questionnaire | `e7e3d1916797dd57` |
| `Itanes 2013/ITA2013_Questionnaire(CAPI)_English_2014_08_07.pdf` | 744 662 | `%PDF-1.5` | PDF — its English translation | `22502175b6238681` |
| `Itanes_2018_release01_panel_pre_post.dta` | 3 069 637 | `<stata_dta>` | Stata 118 (LSF), 144 vars × 2 573 obs | `6ba3324ac5b84ded` |
| `Itanes_2018_release01_panel_pre_post.sav` | 587 203 | `$FL2` | SPSS system file | — |
| `Itanes_2018_release01_post_electoral_questionario.pdf` | 518 381 | `%PDF-1.5` | PDF — the post-electoral questionnaire | `b535690d20d99435` |
| `Itanes_2018_release01_pre_electoral.dta` | 4 362 378 | `<stata_dta>` | Stata 118 (LSF), 98 vars × 5 528 obs | `dfca3a9f715f8e49` |
| `Itanes_2018_release01_pre_electoral.sav` | 844 470 | `$FL2` | SPSS system file | — |
| `Itanes_2018_release01_pre_electoral_questionario.pdf` | 552 493 | `%PDF-1.5` | PDF — the pre-electoral questionnaire | `2058d36a7ab90b3e` |

The `.dta` files were read directly (no Stata, SPSS or Python on this machine): a reader for formats 115 and
117/118 that parses the header, the variable and value-label tables and the fixed-width records
(`stata.pl`, kept with the session; the value labels it printed are the ones quoted below and in the CSV
headers). The questionnaires were read by inflating their content streams (`pdftext.pl`).

## The deliverable — one cross-tab per wave, vote × the six age bands of §137

### 2013 — `itanes_vote_by_age_2013.csv` (weighted)

- **The vote:** `d90` — *"90. MI PUÒ DIRE PER QUALE PARTITO HA VOTATO ALLA CAMERA?"* (questionnaire D90,
  *"Can you tell me which party you voted for on the Chamber of Deputies ballot?"*). The file's own recode
  `cam15` *"Voto Camera 2013 a 15 partiti"* agrees with the mapping used on every row.
- **The age:** `d1` — *"POTREBBE DIRMI GENTILMENTE LA SUA ETÀ ?"*, in years, banded 18–24 · 25–34 · 35–44 ·
  45–54 · 55–64 · 65+; the file's pre-coded `eta6` (value labels `18-24 / 25-34 / 35-44 / 45-54 / 55-64 /
  65 +`) agrees on all 1 173 rows used.
- **The weight:** `weight` — the wave's weight variable (double, unlabelled; n 1 508, mean 1.0001, range
  0.502–2.008). The sampling note describes the design (random extraction from the electoral rolls,
  stratified by municipality size, 172 comuni, 189 sezioni, 8 interviews per sezione) but not the weight's
  construction; it is used as the wave's own and not re-derived.
- **Codes → columns:** FdI 16 · PD 3 · M5S 5 · Lega 13 (*Lega Nord*) · FI 12 (*Il Popolo della Libertà*, the
  PdL lineage the model joins FI to) · AVS_lineage 2 (*Sinistra Ecologia Libertà*) · other = every other list.
  **Excluded:** 24/26 blank, null or did not vote (33), 27 refused (300), one missing (a non-voter, `d86`).

### 2018 — `itanes_vote_by_age_2018.csv` (unweighted, necessarily)

- **The file: the panel, not the pre-electoral wave.** The pre-electoral release carries only the vote
  *intention* (`voto1`); the post-electoral vote *report* exists only in the panel (`voto*_post`), and a
  cross-tab of the vote cast needs the report. All 2 573 panel rows carry `prepost = 1`.
- **The vote:** `votocheck_post` — *"Riassumendo, quale tra le seguenti affermazioni esprime meglio ciò che
  ha fatto in occasione delle elezioni del 4 marzo scorso?"* — chosen over `voto4_post` (*"Per quale dei
  seguenti partiti ha votato alla CAMERA lo scorso 4 Marzo?"*) because `voto4_post` is asked only of
  respondents who marked a party symbol (`voto3_post` 1 or 3; 617 missing), while `votocheck_post` is asked
  of every voter and assigns candidate-only voters their party through `voto5_post`/`voto6_post`.
- **The age:** `dem02_post` — *"Eta"*, in years (`dem02` where the post value is missing), banded as 2013.
  The release's own `dem03_post` *"Cleta"* is three classes (18–34 / 35–54 / 55+) and is not used.
- **The weight: there is none.** Every variable name of both 2018 files was listed (144 and 98); no weight
  variable exists, and the pre-electoral questionnaire describes a quota design (`dem07` *"Area Geografica per
  quota"*). The cross-tab is therefore unweighted, and the catalog says so in the column's own name.
- **Codes → columns:** FdI 7 · PD 2 · M5S 4 · Lega 6 · FI 5 · AVS_lineage 1 (*Liberi e Uguali*) · other =
  3 Più Europa, 8 Noi con l'Italia, 9 Insieme, 10 Civica Popolare, 11 another list. **Excluded:** 12 blank or
  null (51), 13 did not vote (25), 99 refused (379), missing = did not vote by `voto1_post` (250).

### The whole-sample check, and what it says about using the surveys

| wave | FdI | PD | M5S | Lega | FI / PdL | SEL / LeU |
|---|--:|--:|--:|--:|--:|--:|
| 2013 survey (weighted) | 0.67 | 27.59 | 19.71 | 1.64 | 14.05 | 3.61 |
| 2013 official (Camera) | 1.96 | 25.43 | 25.56 | 4.09 | 21.56 | 3.20 |
| 2018 survey (unweighted) | 4.12 | 18.52 | 39.13 | 14.83 | 7.23 | 6.80 |
| 2018 official (Camera) | 4.35 | 18.76 | 32.68 | 17.35 | 14.00 | 3.39 |

Both surveys' party marginals sit off the official returns, in different directions by wave (the 2013 CAPI
sample under-reports M5S and PdL; the 2018 web panel over-reports M5S and LeU and under-reports FI). ⚠ A
loyalty derived from the surveys' own levels would import that mode difference as if it were voter movement.
So the model takes from the surveys **only the distribution across age bands** — each band's propensity for a
party relative to the whole sample — and anchors it to the official national share of that wave
(`GroupLoyaltyModel.AnchoredGroupShares`). **FdI in 2013 rests on 8 respondents** (0 in 25–34 and 65+); the
per-band 2013 figures for it are that thin, and the record says so where they are used.
