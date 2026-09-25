# Sweden - the actual portfolio allocation: the Kristersson government (2022-) and the Andersson government (2021-2022)

Fetched 2026-09-25 with `curl -sL -A "Mozilla/5.0"` from regeringen.se (the government's own site) and, for the Andersson government whose
pages the live site no longer serves, from web.archive.org `id_` captures dated May-July 2022. Stored byte for byte under `raw/portfolios/`
with `SHA256SUMS` beside them (102 files; `fetch_log.txt` records every fetch, including three first attempts that answered 000 and were
retried). Every quote is verbatim (HTML entities decoded, soft hyphens and whitespace collapsed); nothing from memory.

**Where the party is stated.** The ministers' own pages (`/sveriges-regering/<departement>/<namn>/`) do NOT name a party; each minister's
CV page (`.../cv-<namn>/`) carries the line "Partitillhörighet <parti>", and the government's fact sheet PDF ("Faktablad regeringen",
linked from the listing page) prints "(M)", "(KD)", "(L)" after every name. Both are used below; every row is traced to both where both
exist. The listing page `/sveriges-regering/` is the source for department and title; the URLs guessed in the task
(`/regeringen/`, `/regeringen/sveriges-regering/`, `/regeringskansliet/departement/`, `/tidoavtalet/`) all answered 404 ("Sidan kan inte
hittas") and were not stored.

## 1. The Kristersson government as regeringen.se lists it on the access date (the caretaker government since 2026-09-17)

**[RG-LIST]** "Sveriges regering" page: "Sveriges regering består av en statsminister och 23 statsråd." Its listing, department by
department (department heading, then "name, title" for each statsråd):

> Statsrådsberedningen Ulf Kristersson, Statsminister Jessica Rosencrantz, EU-minister Arbetsmarknadsdepartementet Johan Britz,
> Arbetsmarknadsminister Nina Larsson, Jämställdhetsminister Finansdepartementet Elisabeth Svantesson, Finansminister Erik Slottner,
> Civilminister Niklas Wykman, Finansmarknadsminister Försvarsdepartementet Pål Jonson, Försvarsminister Carl-Oskar Bohlin, Minister för
> civilt försvar Justitiedepartementet Gunnar Strömmer, Justitieminister Johan Forssell, Migrationsminister Klimat- och
> näringslivsdepartementet Ebba Busch, Energi- och näringsminister samt vice statsminister Romina Pourmokhtari, Klimat- och miljöminister
> Kulturdepartementet Parisa Liljestrand, Kulturminister Landsbygds- och infrastrukturdepartementet Peter Kullgren, Landsbygdsminister
> Andreas Carlson, Infrastruktur- och bostadsminister Socialdepartementet Jakob Forssmed, Socialminister Anna Tenje, Äldre- och
> socialförsäkringsminister Camilla Waltersson Grönvall, Socialtjänstminister Elisabet Lann, Sjukvårdsminister Utbildningsdepartementet
> Simona Mohamsson, Utbildnings- och integrationsminister Lotta Edholm, Gymnasie-, högskole- och forskningsminister Utrikesdepartementet
> Maria Malmer Stenergard, Utrikesminister Benjamin Dousa, Bistånds- och utrikeshandelsminister

Counted from the page: 24 names = the statsminister + 23 statsråd (the page's own sentence). Departments: Statsrådsberedningen + 10
(Arbetsmarknads-, Finans-, Försvars-, Justitie-, Klimat- och näringslivs-, Kultur-, Landsbygds- och infrastruktur-, Social-,
Utbildnings-, Utrikesdepartementet), matching [RK-ORG] "Regeringskansliet består av Statsrådsberedningen, tio departement och
Förvaltningsavdelningen." (page "Uppdaterad 15 augusti 2025").

**[RG-FAKTA25]** "Faktablad regeringen" PDF, "Senast uppdaterad: September 2025": "Sverige styrs av en regering bestående av Moderaterna
(M), Kristdemokraterna (KD) och Liberalerna (L). Den 18 oktober 2022 tillträdde en regering med Ulf Kristersson (M) som statsminister."
Its 24 name plates, as the PDF's text streams print them (name, party, title, department):
Ulf Kristersson (M) Statsminister Statsrådsberedningen · Carl-Oskar Bohlin (M) Minister för civilt försvar Försvarsdepartementet ·
Jakob Forssmed (KD) Socialminister Socialdepartementet · Elisabet Lann (KD) Sjukvårdsminister Socialdepartementet · Ebba Busch (KD)
Energi- och näringsminister och vice statsminister Klimat- och näringslivsdepartementet · Benjamin Dousa (M) Bistånds- och
utrikeshandelsminister · Andreas Carlson (KD) Infrastruktur- och bostadsminister Landsbygds- och infrastrukturdepartementet · Anna Tenje
(M) Äldre- och socialförsäkringsminister Socialdepartementet · Camilla Waltersson Grönvall (M) Socialtjänstminister Socialdepartementet ·
Elisabeth Svantesson (M) Finansminister Finansdepartementet · Erik Slottner (KD) Civilminister Finansdepartementet · Gunnar Strömmer (M)
Justitieminister Justitiedepartementet · Johan Britz (L) Arbetsmarknadsminister Arbetsmarknadsdepartementet · Simona Mohamsson (L)
Utbildnings- och integrationsminister Utbildningsdepartementet · Jessica Rosencrantz (M) EU-minister Statsrådsberedningen · Niklas Wykman
(M) Finansmarknadsminister Finansdepartementet · Romina Pourmokhtari (L) Klimat- och miljöminister Klimat- och näringslivsdepartementet ·
Maria Malmer Stenergard (M) Utrikesminister Utrikesdepartementet · Parisa Liljestrand (M) Kulturminister Kulturdepartementet · Lotta
Edholm (L) Gymnasie-, högskole- och forskningsminister Utbildningsdepartementet · Johan Forssell (M) Migrationsminister
Justitiedepartementet · Peter Kullgren (KD) Landsbygdsminister Landsbygds- och infrastrukturdepartementet · Pål Jonson (M)
Försvarsminister Försvarsdepartementet · Nina Larsson (L) Jämställdhetsminister Arbetsmarknadsdepartementet.

### The table

Party from the CV page's "Partitillhörighet" line [CV-*] and the fact sheet's "(M)/(KD)/(L)" [RG-FAKTA25]; department and title from
[RG-LIST]. The CV page's own date is given (its "Publicerad"/"Uppdaterad" line).

| department | minister (title) | party | source |
|---|---|---|---|
| Statsrådsberedningen | Ulf Kristersson (Statsminister) | M | [CV-KRISTERSSON] "Partitillhörighet Moderaterna" (Publicerad 07 november 2022); [RG-FAKTA25] "(M)" |
| Statsrådsberedningen | Jessica Rosencrantz (EU-minister) | M | [CV-ROSENCRANTZ] "Partitillhörighet Moderaterna" (Publicerad 11 oktober 2024); [RG-FAKTA25] |
| Arbetsmarknadsdepartementet | Johan Britz (Arbetsmarknadsminister) | L | [CV-BRITZ] "Partitillhörighet Liberalerna" (Uppdaterad 15 juni 2026); [RG-FAKTA25] "(L)" |
| Arbetsmarknadsdepartementet | Nina Larsson (Jämställdhetsminister) | L | [CV-LARSSON] "Partitillhörighet Liberalerna" (Uppdaterad 02 juli 2025); [RG-FAKTA25] |
| Finansdepartementet | Elisabeth Svantesson (Finansminister) | M | [CV-SVANTESSON] "Partitillhörighet Moderaterna" (Publicerad 25 oktober 2022); [RG-FAKTA25] |
| Finansdepartementet | Erik Slottner (Civilminister) | KD | [CV-SLOTTNER] "Partitillhörighet Kristdemokraterna" (Uppdaterad 24 november 2023); [RG-FAKTA25] "(KD)" |
| Finansdepartementet | Niklas Wykman (Finansmarknadsminister) | M | [CV-WYKMAN] "Partitillhörighet Moderaterna" (Publicerad 25 oktober 2022); [RG-FAKTA25] |
| Försvarsdepartementet | Pål Jonson (Försvarsminister) | M | [CV-JONSON] "Partitillhörighet Moderaterna" (Publicerad 02 november 2022); [RG-FAKTA25] |
| Försvarsdepartementet | Carl-Oskar Bohlin (Minister för civilt försvar) | M | [CV-BOHLIN] "Partitillhörighet Moderaterna" (Publicerad 02 november 2022); [RG-FAKTA25] |
| Justitiedepartementet | Gunnar Strömmer (Justitieminister) | M | [CV-STROMMER] "Partitillhörighet Moderaterna" (Publicerad 24 oktober 2022); [RG-FAKTA25] |
| Justitiedepartementet | Johan Forssell (Migrationsminister) | M | [CV-FORSSELL] "Partitillhörighet Moderaterna" (Publicerad 26 oktober 2022); [RG-FAKTA25] |
| Klimat- och näringslivsdepartementet | Ebba Busch (Energi- och näringsminister samt vice statsminister) | KD | [CV-BUSCH] "Partitillhörighet Kristdemokraterna" (Publicerad 05 december 2022); [RG-FAKTA25] |
| Klimat- och näringslivsdepartementet | Romina Pourmokhtari (Klimat- och miljöminister) | L | [CV-POURMOKHTARI] "Partitillhörighet Liberalerna" (Publicerad 02 november 2022); [RG-FAKTA25] |
| Kulturdepartementet | Parisa Liljestrand (Kulturminister) | M | [CV-LILJESTRAND] "Partitillhörighet Moderaterna" (Publicerad 26 oktober 2022); [RG-FAKTA25] |
| Landsbygds- och infrastrukturdepartementet | Peter Kullgren (Landsbygdsminister) | KD | [CV-KULLGREN] "Partitillhörighet Kristdemokraterna" (Publicerad 16 november 2022); [RG-FAKTA25] |
| Landsbygds- och infrastrukturdepartementet | Andreas Carlson (Infrastruktur- och bostadsminister) | KD | [CV-CARLSON] "Partitillhörighet Kristdemokraterna" (Publicerad 24 november 2023); [RG-FAKTA25] |
| Socialdepartementet | Jakob Forssmed (Socialminister) | KD | [CV-FORSSMED] "Partitillhörighet Kristdemokraterna" (Publicerad 02 november 2022); [RG-FAKTA25] |
| Socialdepartementet | Anna Tenje (Äldre- och socialförsäkringsminister) | M | [CV-TENJE] "Partitillhörighet Moderaterna" (Uppdaterad 13 september 2024); [RG-FAKTA25] |
| Socialdepartementet | Camilla Waltersson Grönvall (Socialtjänstminister) | M | [CV-WALTERSSON] "Partitillhörighet Moderaterna" (Publicerad 08 december 2022); [RG-FAKTA25] |
| Socialdepartementet | Elisabet Lann (Sjukvårdsminister) | KD | [CV-LANN] "Partitillhörighet Kristdemokraterna" (Publicerad 20 oktober 2025); [RG-FAKTA25] |
| Utbildningsdepartementet | Simona Mohamsson (Utbildnings- och integrationsminister) | L | [CV-MOHAMSSON] "Partitillhörighet Liberalerna" (Publicerad 03 juli 2025); [RG-FAKTA25] |
| Utbildningsdepartementet | Lotta Edholm (Gymnasie-, högskole- och forskningsminister) | L | [CV-EDHOLM] "Partitillhörighet Liberalerna" (Uppdaterad 13 september 2024); [RG-FAKTA25] |
| Utrikesdepartementet | Maria Malmer Stenergard (Utrikesminister) | M | [CV-MALMER] "Partitillhörighet Moderaterna" (Publicerad 11 september 2024); [RG-FAKTA25] |
| Utrikesdepartementet | Benjamin Dousa (Bistånds- och utrikeshandelsminister) | M | [CV-DOUSA] "Partitillhörighet Moderaterna" (Publicerad 02 oktober 2024); [RG-FAKTA25] |

**Counts (counted from the 24 rows; the fact sheet's 24 plates give the same):** M 13, KD 6, L 5 = 24 (statsminister + 23 statsråd).
Departments headed: of the ten departments plus Statsrådsberedningen, M holds the departementschef post where the first-listed minister
is M - the listing page does not say which statsråd is departementschef ([RK-ORG]: "I ledningen för varje departement finns ett eller
flera statsråd, varav ett är departementschef"), so heads are NOT counted here (GAP 3).

**The government at its formation, 2022-10-18 [RG-NYA]:** "Sveriges nya regering består av statsministern och 23 statsråd." followed by
the same department-by-department list with these differences from the 2026 listing: Jessika Roswall, EU-minister; Johan Pehrson,
arbetsmarknads- och integrationsminister; Paulina Brandberg, jämställdhets- och biträdande arbetsmarknadsminister; Maria Malmer Stenergard,
migrationsminister; Ebba Busch, energi- och näringsminister (no "vice statsminister" in that list); Acko Ankarberg Johansson,
sjukvårdsminister; Mats Persson, utbildningsminister; Lotta Edholm, skolminister; Tobias Billström, utrikesminister; Johan Forssell,
bistånds- och utrikeshandelsminister. The six who have since left (Roswall, Pehrson, Brandberg, Ankarberg Johansson, Persson, Billström)
have no CV page on the live site and their parties are NOT sourced here (GAP 1); the per-party count of the 2022 cabinet is therefore
not stated.

**SD's role [RG-TIDO]:** "Den 18 oktober 2022 bildade statsminister Ulf Kristersson en regering bestående av Moderaterna,
Kristdemokraterna och Liberalerna. Regeringspartierna och samarbetspartiet Sverigedemokraterna hade dessförinnan enats om ett
reformprogram för mandatperioden, Tidöavtalet: Överenskommelse för Sverige." SD appears in no minister row above: 0 ministers. (The
agreement's own sentence "Samarbetsparti utanför regeringen är Sverigedemokraterna" is already quoted in `records_by_date.md` from the
Tidö PDF stored under `raw/records/`.)

### Against the 2022 chamber (seats from `returns_2022.md`: M 68, KD 19, L 16; SD 73; 349 in all)

DERIVED, InvariantCulture:

| party | seats | share of the three governing parties' 103 seats | portfolios of 24 | portfolio share | Gamson-proportional portfolios (24 × seat share) | portfolio share ÷ seat share |
|---|---|---|---|---|---|---|
| M | 68 | 0.6602 | 13 | 0.5417 | 15.84 | 0.82 |
| KD | 19 | 0.1845 | 6 | 0.2500 | 4.43 | 1.36 |
| L | 16 | 0.1553 | 5 | 0.2083 | 3.73 | 1.34 |
| sum | 103 | 1.0000 | 24 | 1.0000 | 24.00 | - |

The observed ratio: the largest (and formateur's) party underpaid by about a fifth, the two small partners overpaid by about a third -
the direction Browne & Franklin's abstract states ("large parties tend to be proportionately underpaid and small parties overpaid",
`docs/reference/GAMSON_PORTFOLIOS.md` [BF73]). Counting the support party into the bloc that voted the PM in (176 seats, [RD-PK14] in
`records_by_date.md`): SD 73/176 = 0.4148 of the bloc's seats, 0 of 24 portfolios - the "samarbetsparti utanför regeringen" is outside
the proportional count altogether.

## 2. The Andersson government as regeringen.se listed it in June 2022 (Wayback `id_` captures)

**[WB-LIST22]** "Sveriges regering", capture 2022-06-02 22:21:07: "Sveriges regering består av en statsminister och 22 statsråd. Sveriges
regering på Lejonterrassen, Kungliga Slottet, efter skifteskonseljen den 30 november 2021." Its listing:

> Statsrådsberedningen Magdalena Andersson, Statsminister Hans Dahlgren, EU-minister Arbetsmarknadsdepartementet Eva Nordmark,
> Arbetsmarknads- och jämställdhetsminister Johan Danielsson, Bostadsminister och biträdande arbetsmarknadsminister Finansdepartementet
> Mikael Damberg, Finansminister Max Elger, Finansmarknadsminister Ida Karkiainen, Civilminister Försvarsdepartementet Peter Hultqvist,
> Försvarsminister Infrastrukturdepartementet Tomas Eneroth, Infrastrukturminister Khashayar Farmanbar, Energi- och
> digitaliseringsminister Justitiedepartementet Morgan Johansson, Justitie- och inrikesminister Anders Ygeman, Integrations- och
> migrationsminister med ansvar för idrottsfrågor Kulturdepartementet Jeanette Gustafsdotter, Kulturminister Miljödepartementet Annika
> Strandhäll, Klimat- och miljöminister Näringsdepartementet Karl-Petter Thorwaldsson, Näringsminister Anna-Caren Sätherberg,
> Landsbygdsminister Socialdepartementet Lena Hallengren, Socialminister Ardalan Shekarabi, Socialförsäkringsminister
> Utbildningsdepartementet Anna Ekström, Utbildningsminister Lina Axelsson Kihlblom, Skolminister Utrikesdepartementet Ann Linde,
> Utrikesminister Matilda Ernkrans, Biståndsminister Anna Hallberg, Utrikeshandelsminister samt minister med ansvar för nordiska frågor

Counted: 23 names = statsminister + 22 statsråd. Eleven departments then (Infrastruktur-, Miljö- and Näringsdepartementet in place of
today's Klimat- och näringslivs- and Landsbygds- och infrastrukturdepartementet).

**[WB-FAKTA22]** "Faktablad regeringen" PDF as captured 2022-06-05 (its text: "Senast uppdaterad: Januari 2022"): "Sverige styrs av en
regering bestående av Socialdemokraterna (S).Den 30 november 2021 tillträdde en ny regering med Magdalena Andersson (S) som
statsminister." Its 23 plates carry no per-name party letter (only the two "(S)" above).

| department | minister (title) | party | source |
|---|---|---|---|
| Statsrådsberedningen | Magdalena Andersson (Statsminister) | S | [WBCV-ANDERSSON] "Partitillhörighet Socialdemokraterna" (Uppdaterad 28 april 2022); [WB-FAKTA22] |
| Statsrådsberedningen | Hans Dahlgren (EU-minister) | S | [WBCV-DAHLGREN] "Partitillhörighet Socialdemokraterna" (Uppdaterad 06 december 2021) |
| Arbetsmarknadsdepartementet | Eva Nordmark (Arbetsmarknads- och jämställdhetsminister) | S | [WBCV-NORDMARK] (Uppdaterad 07 december 2021) |
| Arbetsmarknadsdepartementet | Johan Danielsson (Bostadsminister och biträdande arbetsmarknadsminister) | S | [WBCV-DANIELSSON] (Publicerad 07 december 2021) |
| Finansdepartementet | Mikael Damberg (Finansminister) | S | [WBCV-DAMBERG] "Partitillhörighet Socialdemokraterna" |
| Finansdepartementet | Max Elger (Finansmarknadsminister) | S | **CV page carries no "Partitillhörighet" line** (Publicerad 07 december 2021; its posts include "Socialdemokraternas riksdagskansli"); party from the whole-government sentence [WB-FAKTA22] only - GAP 2 |
| Finansdepartementet | Ida Karkiainen (Civilminister) | S | [WBCV-KARKIAINEN] (Publicerad 02 december 2021) |
| Försvarsdepartementet | Peter Hultqvist (Försvarsminister) | S | [WBCV-HULTQVIST] (Uppdaterad 06 december 2021) |
| Infrastrukturdepartementet | Tomas Eneroth (Infrastrukturminister) | S | [WBCV-ENEROTH] (Uppdaterad 06 december 2021) |
| Infrastrukturdepartementet | Khashayar Farmanbar (Energi- och digitaliseringsminister) | S | [WBCV-FARMANBAR] (Uppdaterad 23 december 2021) |
| Justitiedepartementet | Morgan Johansson (Justitie- och inrikesminister) | S | [WBCV-JOHANSSON] (Uppdaterad 06 december 2021) |
| Justitiedepartementet | Anders Ygeman (Integrations- och migrationsminister med ansvar för idrottsfrågor) | S | [WBCV-YGEMAN] (Uppdaterad 06 december 2021) |
| Kulturdepartementet | Jeanette Gustafsdotter (Kulturminister) | S | [WBCV-GUSTAFSDOTTER] (Publicerad 11 januari 2022) |
| Miljödepartementet | Annika Strandhäll (Klimat- och miljöminister) | S | [WBCV-STRANDHALL] (Publicerad 03 december 2021) |
| Näringsdepartementet | Karl-Petter Thorwaldsson (Näringsminister) | S | [WBCV-THORWALDSSON] (Publicerad 03 december 2021) |
| Näringsdepartementet | Anna-Caren Sätherberg (Landsbygdsminister) | S | [WBCV-SATHERBERG] (Publicerad 03 december 2021) |
| Socialdepartementet | Lena Hallengren (Socialminister) | S | [WBCV-HALLENGREN] (Uppdaterad 07 december 2021) |
| Socialdepartementet | Ardalan Shekarabi (Socialförsäkringsminister) | S | [WBCV-SHEKARABI] (Uppdaterad 06 december 2021) |
| Utbildningsdepartementet | Anna Ekström (Utbildningsminister) | S | [WBCV-EKSTROM] (Uppdaterad 07 december 2021) |
| Utbildningsdepartementet | Lina Axelsson Kihlblom (Skolminister) | S | [WBCV-AXELSSON] (Publicerad 03 december 2021) |
| Utrikesdepartementet | Ann Linde (Utrikesminister) | S | [WBCV-LINDE] |
| Utrikesdepartementet | Matilda Ernkrans (Biståndsminister) | S | [WBCV-ERNKRANS] |
| Utrikesdepartementet | Anna Hallberg (Utrikeshandelsminister samt minister med ansvar för nordiska frågor) | S | [WBCV-HALLBERG] |

Every [WBCV-*] row's quote is the same line "Partitillhörighet Socialdemokraterna" (22 of the 23 CV pages carry it; Elger's does not).

**Counts:** S 23 of 23 (statsminister + 22). A single-party government: seat share of the governing party's seats 100/100 = 1, portfolio
share 1 - Gamson's law is trivially met and says nothing here. (The 2018 chamber's S 100 of 349: `records_by_date.md` [VAL-18].)

## Register of ids

The whole-government pages:

| id | URL | publisher | page date | file (under `raw/portfolios/`) | bytes | sha256 |
|---|---|---|---|---|---|---|
| [RG-LIST] | https://www.regeringen.se/sveriges-regering/ | Regeringskansliet | no date on the page (photo caption "augusti 2025"); access 2026-09-25 | `regeringen_sveriges-regering.html` | 237006 | 282d51403d5a41cda2accfd5af65ad53fda0cfb0dce8014026303308a63ad7be |
| [RG-FAKTA25] | https://www.regeringen.se/contentassets/b7a372535d4d4115b2c9b9ef2c256c6e/faktablad-regeringen_2025_september.pdf | Regeringskansliets kommunikationsenhet | "Senast uppdaterad: September 2025" | `regeringen_faktablad-regeringen_2025_september.pdf` | 1234716 | c6bcc128de617ada699f2e4438476c82641e0f44b332fefe4d8166ae926f002c |
| [RG-NYA] | https://www.regeringen.se/pressmeddelanden/2022/10/sveriges-nya-regering/ | Statsrådsberedningen, pressmeddelande | Publicerad 18 oktober 2022 | `regeringen_20221018_sveriges-nya-regering.html` | 198704 | c1db101b707b905ff85d1fb33f088ffecc2ec676759f32ecae6fcf7ca3e92423 |
| [RG-TIDO] | https://www.regeringen.se/regeringens-politik/regeringens-prioriteringar/genomfort-bokslut-over-tidoavtalet/ | Regeringskansliet | undated on the page (its pressträff "Den 5 maj") | `regeringen_tidoavtalet_bokslut.html` | 207232 | 2df4ea289bf587dd4e125cd18d38a76137ccdd123db91e4213c6c4a96a8d00cb |
| [RK-ORG] | https://www.regeringen.se/regeringskansliet/organisation/ | Regeringskansliet | Uppdaterad 15 augusti 2025 | `regeringen_regeringskansliet_organisation.html` | 197455 | bed8306455a1f7f6c7bf22f55d1355c9251b94c7d82f8e7d7f5f851a2cd755ce |
| [RK-HOME] | https://www.regeringen.se/regeringskansliet/ | Regeringskansliet | - | `regeringen_regeringskansliet.html` | 249190 | f6930bb9739731705f6e61ffc8be4c0a05111a02135c17484cde8ea0715a92d6 |
| [WB-LIST22] | https://web.archive.org/web/20220602222107id_/https://www.regeringen.se/sveriges-regering/ | Regeringskansliet via Wayback | capture 2022-06-02 | `wayback_20220601_sveriges-regering.html` | 228327 | 097337808a2270c2ff05519b278127b04fcf7bb79685f3658929a9ac37cb7fc6 |
| [WB-FAKTA22] | https://web.archive.org/web/20220605070344id_/https://www.regeringen.se/498ff1/contentassets/b7a372535d4d4115b2c9b9ef2c256c6e/faktablad-regeringen-2022_04_28_sv.pdf | Regeringskansliet via Wayback | "Senast uppdaterad: Januari 2022"; capture 2022-06-05 | `wayback2022_faktablad-regeringen-2022_04_28_sv.pdf` | 2687150 | 983da750202687946f77e196bbb8c316c348fc0c7db8ede02dde07a18c11ae14 |

The per-minister pages - `live_<departement>__<namn>.html` is the minister's page, `..._cv.html` the CV page that carries
"Partitillhörighet"; `[CV-<NAME>]` in the table above is the live CV file for that name, `[WBCV-<NAME>]` the Wayback one. Wayback
captures are dated by the stamp in their URL (all May-July 2022):

| file | bytes | sha256 | URL |
|---|---|---|---|
| `live_arbetsmarknadsdepartementet__johan-britz.html` | 274663 | 3e3be3a107be0bebce55ccd62910b1dfc226e675359b811db3f2ddedb01ef87a | https://www.regeringen.se/sveriges-regering/arbetsmarknadsdepartementet/johan-britz/ |
| `live_arbetsmarknadsdepartementet__johan-britz__cv.html` | 201481 | b803c53e2b3450d9675decdcef4dc5dc7b6c1fd02ab2ea49edf63465aaa0e6b7 | https://www.regeringen.se/sveriges-regering/arbetsmarknadsdepartementet/johan-britz/cv-johan-britz/ |
| `live_arbetsmarknadsdepartementet__nina-larsson.html` | 282812 | 26cec2f145d7bf88cee29191e818cd9cda1cbc02f7d280b8240bc9bb02b0c7ab | https://www.regeringen.se/sveriges-regering/arbetsmarknadsdepartementet/nina-larsson/ |
| `live_arbetsmarknadsdepartementet__nina-larsson__cv.html` | 201302 | d8087b77730211df85894e229d2ef369ac50b55fbd653321950c922dd78974e0 | https://www.regeringen.se/sveriges-regering/arbetsmarknadsdepartementet/nina-larsson/cv-nina-larsson/ |
| `live_finansdepartementet__elisabeth-svantesson.html` | 285825 | 68e9776f70d1be6131a85d80e7fdf602eb9bc8cc2c00e7c793aa0958f3ab441f | https://www.regeringen.se/sveriges-regering/finansdepartementet/elisabeth-svantesson/ |
| `live_finansdepartementet__elisabeth-svantesson__cv.html` | 196721 | 721df2bbe6de7579990c2c976bdb33c18c7e8300da072cd91b7ea722c20b5d80 | https://www.regeringen.se/sveriges-regering/finansdepartementet/elisabeth-svantesson/cv-elisabeth-svantesson/ |
| `live_finansdepartementet__erik-slottner.html` | 285840 | de710f5dddc3e3e3c66546b24f0aef680ef56bc5b8ae32161abadc314b10bf3d | https://www.regeringen.se/sveriges-regering/finansdepartementet/erik-slottner/ |
| `live_finansdepartementet__erik-slottner__cv.html` | 198448 | 23823763b2debc3839c93e462ab9986e2e9a4257e59710cd209a3bbb72903986 | https://www.regeringen.se/sveriges-regering/finansdepartementet/erik-slottner/cv-erik-slottner/ |
| `live_finansdepartementet__niklas-wykman.html` | 281350 | 6afc7844ebd56aeb81c2ec3845bcdf196561aa0f834a3a470dc0a05ca7283555 | https://www.regeringen.se/sveriges-regering/finansdepartementet/niklas-wykman/ |
| `live_finansdepartementet__niklas-wykman__cv.html` | 197724 | 9d456fe458ea5a079e083f683b4c51f8abef9e2d57d03538a34ae89508aeb2ea | https://www.regeringen.se/sveriges-regering/finansdepartementet/niklas-wykman/cv-niklas-wykman/ |
| `live_forsvarsdepartementet__carl-oskar-bohlin.html` | 268449 | 139d4be5100123e71f5f467c82926ab7c0dd696c799f35551fe0b4ea817d90b1 | https://www.regeringen.se/sveriges-regering/forsvarsdepartementet/carl-oskar-bohlin/ |
| `live_forsvarsdepartementet__carl-oskar-bohlin__cv.html` | 194045 | b4620441de6172f1a1f1ff93b0004901c6487d55175a13444f258131831ba789 | https://www.regeringen.se/sveriges-regering/forsvarsdepartementet/carl-oskar-bohlin/cv-carl-oskar-bohlin/ |
| `live_forsvarsdepartementet__pal-jonson.html` | 263996 | 8d5497cc21692d5802410d049726741896316c09aa9e84db3fc7065e502f446e | https://www.regeringen.se/sveriges-regering/forsvarsdepartementet/pal-jonson/ |
| `live_forsvarsdepartementet__pal-jonson__cv.html` | 201122 | 9c5436dbb44abb400602f41fae292c835dab193eb704d70bab03bec835290408 | https://www.regeringen.se/sveriges-regering/forsvarsdepartementet/pal-jonson/cv-pal-jonson/ |
| `live_justitiedepartementet__gunnar-strommer.html` | 276903 | 9892fdd0a97bdeade4eb04dd364e4208a4719ae995d010c82656b370889f2998 | https://www.regeringen.se/sveriges-regering/justitiedepartementet/gunnar-strommer/ |
| `live_justitiedepartementet__gunnar-strommer__cv.html` | 199902 | 1fe190ef993a616ed4f25f4d85c4999befb8ee82a4710bbb7b86a29fcba57eee | https://www.regeringen.se/sveriges-regering/justitiedepartementet/gunnar-strommer/cv-gunnar-strommer/ |
| `live_justitiedepartementet__johan-forssell.html` | 275493 | bedc094d42b3e820f65bfc1873667764a9c1f7510cd9fee8999b9e14e4915605 | https://www.regeringen.se/sveriges-regering/justitiedepartementet/johan-forssell/ |
| `live_justitiedepartementet__johan-forssell__cv.html` | 196452 | 12eb0e13e278eb69b178f6662475092f8ab4b5dcb1bec00ea7a0637e45d53393 | https://www.regeringen.se/sveriges-regering/justitiedepartementet/johan-forssell/cv-johan-forssell/ |
| `live_klimat--och-naringslivsdepartementet__ebba-busch.html` | 280193 | b2e9f8bb907c68fd535966c822e98e51db007ac866931a05986452127fe218be | https://www.regeringen.se/sveriges-regering/klimat--och-naringslivsdepartementet/ebba-busch/ |
| `live_klimat--och-naringslivsdepartementet__ebba-busch__cv.html` | 195432 | f65499a1bf35b773a06bbba7e52951db1439a6e7c4ec4529c3dfa6294c1fef6d | https://www.regeringen.se/sveriges-regering/klimat--och-naringslivsdepartementet/ebba-busch/cv-ebba-busch/ |
| `live_klimat--och-naringslivsdepartementet__romina-pourmokhtari.html` | 279668 | ce10eb9d28e7ffe897baa08180c4fa439c3ee99de1fff48e967188cf3d92662b | https://www.regeringen.se/sveriges-regering/klimat--och-naringslivsdepartementet/romina-pourmokhtari/ |
| `live_klimat--och-naringslivsdepartementet__romina-pourmokhtari__cv.html` | 196906 | 2621fb4801eced3024ef26b19e897d8d4de2d046c80181e40b951866f2509010 | https://www.regeringen.se/sveriges-regering/klimat--och-naringslivsdepartementet/romina-pourmokhtari/cv-romina-pourmokhtari/ |
| `live_kulturdepartementet__parisa-liljestrand.html` | 271618 | 7d376f7caeef2c84e83e8dade769a5d0f5e117510107c685842a61fbd88cc0f1 | https://www.regeringen.se/sveriges-regering/kulturdepartementet/parisa-liljestrand/ |
| `live_kulturdepartementet__parisa-liljestrand__cv.html` | 196721 | 0044762536175e4929e686151a0b9797a08d97ee1b2eba608b2d540dcbd9138a | https://www.regeringen.se/sveriges-regering/kulturdepartementet/parisa-liljestrand/cv-parisa-liljestrand/ |
| `live_landsbygds--och-infrastrukturdepartementet__andreas-carlson.html` | 279027 | ef2bf5daca9eeb91dea1857accaaa248dc3bf7df80c8f38edb74b6585cc00879 | https://www.regeringen.se/sveriges-regering/landsbygds--och-infrastrukturdepartementet/andreas-carlson/ |
| `live_landsbygds--och-infrastrukturdepartementet__andreas-carlson__cv.html` | 199007 | ef974551009cc6bd089473204b9c0fac4d0d7ef296fd691682c2c872b6d71abd | https://www.regeringen.se/sveriges-regering/landsbygds--och-infrastrukturdepartementet/andreas-carlson/cv-andreas-carlson/ |
| `live_landsbygds--och-infrastrukturdepartementet__peter-kullgren.html` | 276198 | 8b0295df69fb51d21b76f9c26b35a6fb7e9943bafa5372a0d1f3fe903ac33f45 | https://www.regeringen.se/sveriges-regering/landsbygds--och-infrastrukturdepartementet/peter-kullgren/ |
| `live_landsbygds--och-infrastrukturdepartementet__peter-kullgren__cv.html` | 196061 | b0ec1507384971c0ee5c9e82cb8062e16b03caa2902ffc55b9407a6e386f4901 | https://www.regeringen.se/sveriges-regering/landsbygds--och-infrastrukturdepartementet/peter-kullgren/cv-peter-kullgren/ |
| `live_socialdepartementet__anna-tenje.html` | 276984 | 2d7a97a259c50a0d159f10aa988597fc001fb16cb5541a16f6330d027c48997a | https://www.regeringen.se/sveriges-regering/socialdepartementet/anna-tenje/ |
| `live_socialdepartementet__anna-tenje__cv.html` | 199637 | 5d625382c310db931118f61e404a8b69d59c1deb4813a8c8001295675b0963cd | https://www.regeringen.se/sveriges-regering/socialdepartementet/anna-tenje/cv-anna-tenje/ |
| `live_socialdepartementet__camilla-waltersson-gronvall.html` | 278074 | 497e4e1a905da1d66946e9c3e53ec478675a98006a2dd7c7f049d62720ed0f94 | https://www.regeringen.se/sveriges-regering/socialdepartementet/camilla-waltersson-gronvall/ |
| `live_socialdepartementet__camilla-waltersson-gronvall__cv.html` | 200129 | d9c77c4ab30a82e2e9b95e4dce210363302cfe2f72f8dd5c1b47fcbb288514f5 | https://www.regeringen.se/sveriges-regering/socialdepartementet/camilla-waltersson-gronvall/cv-camilla-waltersson-gronvall/ |
| `live_socialdepartementet__elisabet-lann.html` | 270740 | 61bf0a0118cb4f51702ebad09e12d64d6537f784bd5b4521e79e09a306abf65d | https://www.regeringen.se/sveriges-regering/socialdepartementet/elisabet-lann/ |
| `live_socialdepartementet__elisabet-lann__cv.html` | 200162 | 254882ff630391464f16910045aa1dc8547ef7464b606b4e1b20e8df25af350d | https://www.regeringen.se/sveriges-regering/socialdepartementet/elisabet-lann/cv-elisabet-lann/ |
| `live_socialdepartementet__jakob-forssmed.html` | 286169 | d0e7dd8b0707c1ed9c061622f9851a49107e38700065c4d1195004bafd704c90 | https://www.regeringen.se/sveriges-regering/socialdepartementet/jakob-forssmed/ |
| `live_socialdepartementet__jakob-forssmed__cv.html` | 201855 | e4686da9570ae2044375003bd7d77e683e6fc46c26c50573ecc03ba1a7125ec2 | https://www.regeringen.se/sveriges-regering/socialdepartementet/jakob-forssmed/cv-jakob-forssmed/ |
| `live_statsradsberedningen__jessica-rosencrantz.html` | 267852 | cb0271d5f78e2c9964e1847585f7aa902b92e6506bfc4ac478367f5fb549f49f | https://www.regeringen.se/sveriges-regering/statsradsberedningen/jessica-rosencrantz/ |
| `live_statsradsberedningen__jessica-rosencrantz__cv.html` | 197132 | 642e363f21c6c2d8f281160d126307e180adeb37711752a20a02963279dad445 | https://www.regeringen.se/sveriges-regering/statsradsberedningen/jessica-rosencrantz/cv-jessica-rosencrantz/ |
| `live_statsradsberedningen__ulf-kristersson.html` | 278693 | 99fa0bf9ed0095998c195399fca0732229b1a989632853dba45fa07dbb584652 | https://www.regeringen.se/sveriges-regering/statsradsberedningen/ulf-kristersson/ |
| `live_statsradsberedningen__ulf-kristersson__cv.html` | 200855 | dcb4de0036e13d30c3b3bba53b219708de5bdc7a413ab61bbefc0142d5b873d0 | https://www.regeringen.se/sveriges-regering/statsradsberedningen/ulf-kristersson/cv-ulf-kristersson/ |
| `live_utbildningsdepartementet__lotta-edholm.html` | 269480 | bc800d49b948e907f42cdeff8a3c80dc1658d1922c25bc02bb81567433658147 | https://www.regeringen.se/sveriges-regering/utbildningsdepartementet/lotta-edholm/ |
| `live_utbildningsdepartementet__lotta-edholm__cv.html` | 198913 | fbc8a8ff3a362545b2ccd564ded967a37317439ba6f0b09afd9d452fb0fd84b2 | https://www.regeringen.se/sveriges-regering/utbildningsdepartementet/lotta-edholm/cv-lotta-edholm/ |
| `live_utbildningsdepartementet__simona-mohamsson.html` | 268272 | 130d8bf8bbf3c0421ae7543f184cd25e72b4bdf2c80c5f640534a878e0280359 | https://www.regeringen.se/sveriges-regering/utbildningsdepartementet/simona-mohamsson/ |
| `live_utbildningsdepartementet__simona-mohamsson__cv.html` | 201595 | f1b931d087dc8e9a3718f7e2c0ccc071330f857dd6ee3946df9fe3d4e22c7009 | https://www.regeringen.se/sveriges-regering/utbildningsdepartementet/simona-mohamsson/cv-simona-mohamsson/ |
| `live_utrikesdepartementet__benjamin-dousa.html` | 266337 | 50f0057cbe45667c229790e4a500791be6c0a897bf0458c02e686febd5d5148e | https://www.regeringen.se/sveriges-regering/utrikesdepartementet/benjamin-dousa/ |
| `live_utrikesdepartementet__benjamin-dousa__cv.html` | 197303 | 3bbb8c879c195390249deead746078d59f6eddc5c39a585366c40ad5062dde79 | https://www.regeringen.se/sveriges-regering/utrikesdepartementet/benjamin-dousa/cv-benjamin-dousa/ |
| `live_utrikesdepartementet__maria-malmer-stenergard.html` | 266326 | 675e03eb6a253c039cee7a13460550229283ddb48204bb63a87f39f5ae50b2a1 | https://www.regeringen.se/sveriges-regering/utrikesdepartementet/maria-malmer-stenergard/ |
| `live_utrikesdepartementet__maria-malmer-stenergard__cv.html` | 199099 | ce3dae553495279f38fa6170315b820a4937b19f384909331dd96c2392936ec3 | https://www.regeringen.se/sveriges-regering/utrikesdepartementet/maria-malmer-stenergard/cv-maria-malmer-stenergard/ |
| `wayback2022_arbetsmarknadsdepartementet__eva-nordmark.html` | 261736 | 53385d6ccf5ced0b7d99965d16f5dd77fb5e30e8a309b24c9b6024070006e80b | https://web.archive.org/web/20220603053115id_/https://www.regeringen.se/sveriges-regering/arbetsmarknadsdepartementet/eva-nordmark/ |
| `wayback2022_arbetsmarknadsdepartementet__eva-nordmark__cv.html` | 189639 | c05ef7b2c35cf3d7b80401e2da13ef2e86442aab2a9a40a9704dbc45d677cc55 | https://web.archive.org/web/20220520172328id_/https://www.regeringen.se/sveriges-regering/arbetsmarknadsdepartementet/eva-nordmark/cv-eva-nordmark/ |
| `wayback2022_arbetsmarknadsdepartementet__johan-danielsson.html` | 250047 | 9e26bc967f44f8427b890391ec3d1156eadf92f8cbc8eee3ee4cf810fa3bc540 | https://web.archive.org/web/20220603053323id_/https://www.regeringen.se/sveriges-regering/arbetsmarknadsdepartementet/johan-danielsson/ |
| `wayback2022_arbetsmarknadsdepartementet__johan-danielsson__cv.html` | 189199 | 02836e06df69dc14ff3425f268c0b1ff80a1ca7dd14a40540d593e8e492b45d1 | https://web.archive.org/web/20220523111050id_/https://www.regeringen.se/sveriges-regering/arbetsmarknadsdepartementet/johan-danielsson/cv-johan-danielsson/ |
| `wayback2022_finansdepartementet__ida-karkiainen.html` | 241126 | a418b4496ff13ab137426bd307285188c84e9874195d185e1bb21a16b2d86303 | https://web.archive.org/web/20220603073037id_/https://www.regeringen.se/sveriges-regering/finansdepartementet/ida-karkiainen/ |
| `wayback2022_finansdepartementet__ida-karkiainen__cv.html` | 190291 | 5c526c5cfb3241d6067eaf6efe4ffcc1d3ab620564040cb6f8eac0c18b2a4787 | https://web.archive.org/web/20220525172503id_/https://www.regeringen.se/sveriges-regering/finansdepartementet/ida-karkiainen/cv-ida-karkiainen/ |
| `wayback2022_finansdepartementet__max-elger.html` | 240803 | d530f449e5f5bcd160348ec0b5fe90a2cafebc56b9a77ac1ee8bd1e03d35eeb8 | https://web.archive.org/web/20220603071018id_/https://www.regeringen.se/sveriges-regering/finansdepartementet/max-elger/ |
| `wayback2022_finansdepartementet__max-elger__cv.html` | 188369 | 7ed46d734ba970cbd37c6e0a187f123addecd3a3fac71d2eafb17ca95b3b80e3 | https://web.archive.org/web/20220525160258id_/https://www.regeringen.se/sveriges-regering/finansdepartementet/max-elger/cv-max-elger/ |
| `wayback2022_finansdepartementet__mikael-damberg.html` | 253051 | a8fe3715aff61804aca6180f63d87a45500a27dca5b84292e7b726a6fb03131c | https://web.archive.org/web/20220603070826id_/https://www.regeringen.se/sveriges-regering/finansdepartementet/mikael-damberg/ |
| `wayback2022_finansdepartementet__mikael-damberg__cv.html` | 196671 | 28049db2cbacac37465e41e6a349c4be98b084af90dc58383621feb6f3eb5c30 | https://web.archive.org/web/20220525153359id_/https://www.regeringen.se/sveriges-regering/finansdepartementet/mikael-damberg/cv-mikael-damberg/ |
| `wayback2022_forsvarsdepartementet__peter-hultqvist.html` | 268790 | 871ae384493cbb35e727022c8195289650260f852fd5b09da46e0b6cd1637369 | https://web.archive.org/web/20220603073828id_/https://www.regeringen.se/sveriges-regering/forsvarsdepartementet/peter-hultqvist/ |
| `wayback2022_forsvarsdepartementet__peter-hultqvist__cv.html` | 190557 | 9c9c9ec83b3ca64eb608a9f389e6bd28dc131f24870fe1e1a7461c57cbce56f6 | https://web.archive.org/web/20220521050135id_/https://www.regeringen.se/sveriges-regering/forsvarsdepartementet/peter-hultqvist/cv-peter-hultqvist/ |
| `wayback2022_infrastrukturdepartementet__khashayar-farmanbar.html` | 246956 | f143f3c183a8bfc307dd5734627f1ead2ae6608dad03b1c412927ad3e17dd2e1 | https://web.archive.org/web/20220603091735id_/https://www.regeringen.se/sveriges-regering/infrastrukturdepartementet/khashayar-farmanbar/ |
| `wayback2022_infrastrukturdepartementet__khashayar-farmanbar__cv.html` | 189532 | e898dc7cb643b8ef7bff9580902c5e42162de54f3aac927ef4c5784da73ad701 | https://web.archive.org/web/20220519225306id_/https://www.regeringen.se/sveriges-regering/infrastrukturdepartementet/khashayar-farmanbar/cv-khashayar-farmanbar/ |
| `wayback2022_infrastrukturdepartementet__tomas-eneroth.html` | 247969 | 0af932987cdcf6ff64c89b1fc378ccc059da0a35ee453397b54e5c5fa8a79174 | https://web.archive.org/web/20220603090213id_/https://www.regeringen.se/sveriges-regering/infrastrukturdepartementet/tomas-eneroth/ |
| `wayback2022_infrastrukturdepartementet__tomas-eneroth__cv.html` | 190549 | eb3bad6390e414d549fca1979e2cbc6f4be401bbc8d3f0a8715a99b6b63ac701 | https://web.archive.org/web/20220521125822id_/https://www.regeringen.se/sveriges-regering/infrastrukturdepartementet/tomas-eneroth/cv-tomas-eneroth/ |
| `wayback2022_justitiedepartementet__anders-ygeman.html` | 256430 | d1ddc892dae5a14f21a0965d510a4c4933cdeb9ffa88647efd6585062ec351c3 | https://web.archive.org/web/20220603092748id_/https://www.regeringen.se/sveriges-regering/justitiedepartementet/anders-ygeman/ |
| `wayback2022_justitiedepartementet__anders-ygeman__cv.html` | 190766 | cf3eddb55cf3135a6cb9ea2e9d00ed0f65deca6b49a6e6344167164eb21241c9 | https://web.archive.org/web/20220521094207id_/https://www.regeringen.se/sveriges-regering/justitiedepartementet/anders-ygeman/cv-anders-ygeman/ |
| `wayback2022_justitiedepartementet__morgan-johansson.html` | 257181 | 1a0d5b3124f5e40e236d6d0197fabfcb3145505bfb61840f410eea36125bf399 | https://web.archive.org/web/20220603092607id_/https://www.regeringen.se/sveriges-regering/justitiedepartementet/morgan-johansson/ |
| `wayback2022_justitiedepartementet__morgan-johansson__cv.html` | 190730 | c18c3ef8a46fe7d755b49622e5cac821801d3fab367127094ec5d6b915e3ffcd | https://web.archive.org/web/20220528112858id_/https://www.regeringen.se/sveriges-regering/justitiedepartementet/morgan-johansson/cv-morgan-johansson/ |
| `wayback2022_kulturdepartementet__jeanette-gustafsdotter.html` | 259053 | 686dd371072f1506718f955abfd929436707915e6e39fa8530d273b210b8f0c2 | https://web.archive.org/web/20220603095135id_/https://www.regeringen.se/sveriges-regering/kulturdepartementet/jeanette-gustafsdotter/ |
| `wayback2022_kulturdepartementet__jeanette-gustafsdotter__cv.html` | 191370 | b98c9f02f8c14aad9147b49bd3d272d6a3c083405f88a302d2abe288801439e1 | https://web.archive.org/web/20220519205553id_/https://www.regeringen.se/sveriges-regering/kulturdepartementet/jeanette-gustafsdotter/cv-jeanette-gustafsdotter/ |
| `wayback2022_miljodepartementet__annika-strandhall.html` | 273363 | 4dd8b6a6fcd30ae305677775752adc2ebe9af8da7525ac559edb9a51c418b260 | https://web.archive.org/web/20220603115344id_/https://www.regeringen.se/sveriges-regering/miljodepartementet/annika-strandhall/ |
| `wayback2022_miljodepartementet__annika-strandhall__cv.html` | 190794 | 7770169206fa83a1df3269aee7a98b2cb5e342e617ddb404d05571ff7655c581 | https://web.archive.org/web/20220519223732id_/https://www.regeringen.se/sveriges-regering/miljodepartementet/annika-strandhall/cv-annika-strandhall/ |
| `wayback2022_naringsdepartementet__anna-caren-satherberg.html` | 243064 | c891982ce960282dce04d2fa4b51421eaa9736305e157f52d59d156f05fa8c8d | https://web.archive.org/web/20220603115855id_/https://www.regeringen.se/sveriges-regering/naringsdepartementet/anna-caren-satherberg/ |
| `wayback2022_naringsdepartementet__anna-caren-satherberg__cv.html` | 189867 | f737243f95b34b748ce5894951a70b99875baa954dea9418efbff8af5a2bd20a | https://web.archive.org/web/20220519213508id_/https://www.regeringen.se/sveriges-regering/naringsdepartementet/anna-caren-satherberg/cv-anna-caren-satherberg/ |
| `wayback2022_naringsdepartementet__karl-petter-thorwaldsson.html` | 249207 | 3a1e51202f08e3e6ed01e636ff83616bfad61774bb31e3c1bdf2b7473398b666 | https://web.archive.org/web/20220603115742id_/https://www.regeringen.se/sveriges-regering/naringsdepartementet/karl-petter-thorwaldsson/ |
| `wayback2022_naringsdepartementet__karl-petter-thorwaldsson__cv.html` | 189429 | 3212337f3f035893e25fca973a80fc567ff37d583c11ee643b0a3517a2731ebb | https://web.archive.org/web/20220519224312id_/https://www.regeringen.se/sveriges-regering/naringsdepartementet/karl-petter-thorwaldsson/cv-karl-petter-thorwaldsson/ |
| `wayback2022_socialdepartementet__ardalan-shekarabi.html` | 249522 | fef619f941175c46a73c94a0807c6c19cef57c0fe294aea46dd6c607e30e386e | https://web.archive.org/web/20220603135652id_/https://www.regeringen.se/sveriges-regering/socialdepartementet/ardalan-shekarabi/ |
| `wayback2022_socialdepartementet__ardalan-shekarabi__cv.html` | 189826 | 8b89e8c0a2c1574a803c40d473cb497bd510c670626ef46e8d4ca92beb58b85d | https://web.archive.org/web/20220714143910id_/https://www.regeringen.se/sveriges-regering/socialdepartementet/ardalan-shekarabi/cv-ardalan-shekarabi/ |
| `wayback2022_socialdepartementet__lena-hallengren.html` | 265856 | cfeaaa5524c6243415c85c0aac8fb6eb391eb4539362a5fbf45c4255c9c6c1df | https://web.archive.org/web/20220601043107id_/https://www.regeringen.se/sveriges-regering/socialdepartementet/lena-hallengren/ |
| `wayback2022_socialdepartementet__lena-hallengren__cv.html` | 189343 | 99ab33b8756b7e2b8e6d1baa9201c9a4fa5f239dd521f9beda47cbf69ea20dc8 | https://web.archive.org/web/20220701205959id_/https://www.regeringen.se/sveriges-regering/socialdepartementet/lena-hallengren/cv-lena-hallengren/ |
| `wayback2022_statsradsberedningen__hans-dahlgren.html` | 246599 | 1a8fd40dacf64567a76b7a49923d901f9f59b5ddf1a9fcee2c372695ee728d21 | https://web.archive.org/web/20220603042359id_/https://www.regeringen.se/sveriges-regering/statsradsberedningen/hans-dahlgren/ |
| `wayback2022_statsradsberedningen__hans-dahlgren__cv.html` | 189641 | 4498d013faeb716ce9dfa1b66002397705d61adc3d978442c1407144ac830354 | https://web.archive.org/web/20220605130332id_/https://www.regeringen.se/sveriges-regering/statsradsberedningen/hans-dahlgren/cv-hans-dahlgren/ |
| `wayback2022_statsradsberedningen__magdalena-andersson.html` | 265394 | 6d8d931189b7e738f60853f1e02df5a563b41c39dc2f51af9da671bd325fc571 | https://web.archive.org/web/20220602105946id_/https://www.regeringen.se/sveriges-regering/statsradsberedningen/magdalena-andersson/ |
| `wayback2022_statsradsberedningen__magdalena-andersson__cv.html` | 189496 | a598839ca1e5e66ff4eb4c667843ea7a8ac2869fb365ab24e40cb4fa5799fc7e | https://web.archive.org/web/20220605105149id_/https://www.regeringen.se/sveriges-regering/statsradsberedningen/magdalena-andersson/cv-magdalena-andersson/ |
| `wayback2022_utbildningsdepartementet__anna-ekstrom.html` | 264417 | 4a45bb02752035e3851b83a6c34c3508592b381ecce70266da6967087def2525 | https://web.archive.org/web/20220603150653id_/https://www.regeringen.se/sveriges-regering/utbildningsdepartementet/anna-ekstrom/ |
| `wayback2022_utbildningsdepartementet__anna-ekstrom__cv.html` | 191003 | b4a60fb0de9c28082d222ba24a8bbf2e135989e2cbe8ada34be42faed90ea164 | https://web.archive.org/web/20220630191327id_/https://www.regeringen.se/sveriges-regering/utbildningsdepartementet/anna-ekstrom/cv-anna-ekstrom/ |
| `wayback2022_utbildningsdepartementet__lina-axelsson-kihlblom.html` | 250143 | 087e2188c66bae6485d5c9f86cf195fe93b89f204f28d3e0c28ffbe70d3bb33c | https://web.archive.org/web/20220603151036id_/https://www.regeringen.se/sveriges-regering/utbildningsdepartementet/lina-axelsson-kihlblom/ |
| `wayback2022_utbildningsdepartementet__lina-axelsson-kihlblom__cv.html` | 189765 | 75e354e78f679ec1d4351eb4309a982221e56e1baed2b60cf76c9f6854241e25 | https://web.archive.org/web/20220525155039id_/https://www.regeringen.se/sveriges-regering/utbildningsdepartementet/lina-axelsson-kihlblom/cv-lina-axelsson-kihlblom/ |
| `wayback2022_utrikesdepartementet__anna-hallberg.html` | 251673 | 971670e34af36f8c5c7f9976873aecb22c91c47f43c78f56139eccdbbc353e0f | https://web.archive.org/web/20220603165139id_/https://www.regeringen.se/sveriges-regering/utrikesdepartementet/anna-hallberg/ |
| `wayback2022_utrikesdepartementet__anna-hallberg__cv.html` | 189426 | 0e677a34fb1be871ab618e72f5ccbdf3d29e2d373b6ea61455f7a827b26129bd | https://web.archive.org/web/20220525160220id_/https://www.regeringen.se/sveriges-regering/utrikesdepartementet/anna-hallberg/cv-anna-hallberg/ |
| `wayback2022_utrikesdepartementet__ann-linde.html` | 252670 | 3484f0e6ef23445f29ee12f717f03eaf375ce4e8e7f6731e31fc46ae8dc09b99 | https://web.archive.org/web/20220603155819id_/https://www.regeringen.se/sveriges-regering/utrikesdepartementet/ann-linde/ |
| `wayback2022_utrikesdepartementet__ann-linde__cv.html` | 190942 | 77310749618c7c1f99e8a3b65d716e4b0e5979d813fe70eeef7c8d37e7583d9c | https://web.archive.org/web/20220703052828id_/https://www.regeringen.se/sveriges-regering/utrikesdepartementet/ann-linde/cv-ann-linde/ |
| `wayback2022_utrikesdepartementet__matilda-ernkrans.html` | 251096 | c8d35cd10f2115a00c5fb07d593e704212a7ae1633996fdfb1eeea5f81676603 | https://web.archive.org/web/20220603164922id_/https://www.regeringen.se/sveriges-regering/utrikesdepartementet/matilda-ernkrans/ |
| `wayback2022_utrikesdepartementet__matilda-ernkrans__cv.html` | 196626 | 663f12ebc3b8e933b251f2fe830f7c7444526d9d530fa6b654ed783820361923 | https://web.archive.org/web/20220705003811id_/https://www.regeringen.se/sveriges-regering/utrikesdepartementet/matilda-ernkrans/cv-matilda-ernkrans/ |

All 102 files fetched 2026-09-25, HTTP 200, stored unaltered (the Wayback pages via `id_`, so the archive's own banner is absent; two
served gzip-compressed on the first try were re-fetched with `--compressed` and the stored copies are the pages themselves).
`raw/portfolios/SHA256SUMS` lists every file (a new folder); `raw/portfolios/fetch_log.txt` is the fetch record.

## GAPS

1. **The parties of the six 2022 ministers who have since left** (Roswall, Pehrson, Brandberg, Ankarberg Johansson, Persson, Billström)
   - the live site has no CV page for them and no 2022-2024 fact sheet was fetched; the 2022 cabinet's per-party count is therefore
   not stated. Candidate: a Wayback `id_` capture of `/sveriges-regering/` and their CV pages dated late 2022.
2. **Max Elger's party** - his 2022 CV page has no "Partitillhörighet" line; sourced only by the whole-government sentence "en regering
   bestående av Socialdemokraterna (S)" [WB-FAKTA22].
3. **Which statsråd is departementschef** - not on the listing page; [RK-ORG] says each department has one. Not counted per party.
4. **The Andersson government's formation press release (30 November 2021)** - not found: the Wayback CDX index for
   `regeringen.se/pressmeddelanden/2021/11/` answered 504 on one query and a truncated list on another; not retried further. The listing
   page's own sentence "efter skifteskonseljen den 30 november 2021" [WB-LIST22] and the fact sheet's "Den 30 november 2021 tillträdde
   en ny regering" [WB-FAKTA22] date the government instead.
5. **The Tidö agreement's own page** - the guessed `/tidoavtalet/` URL is 404; the bokslut page [RG-TIDO] names SD as "samarbetspartiet"
   and the agreement's PDF is under `raw/records/` (quoted in `records_by_date.md`), not re-fetched here.
6. **Ministers who changed between October 2022 and September 2025 with dates** - the reshuffles (Roswall → Rosencrantz, Pehrson →
   Britz, Brandberg → Larsson, Ankarberg Johansson → Lann, Persson → Mohamsson, Billström → Malmer Stenergard, Forssell → Dousa) are
   visible only by comparing [RG-NYA] with [RG-LIST]; no page fetched dates them.
