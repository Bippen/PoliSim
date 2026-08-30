# Issue salience — Eurobarometer 105 (Spring 2026) + Gallup MIP (July 2026) [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; overnight 2026-08-28→29, research agent). `[PROVISIONAL]` until
re-verified (R-K9) — note especially the extraction caveat below (the annex PDF's table
alignment was verified against distinctive values before any row was read).

## SALIENCE — Standard Eurobarometer 105 (Spring 2026), QA3 "two most important issues facing (OUR COUNTRY)", fieldwork EU27 12 March–5 April 2026, 26,415 interviews (Data Annex PDF + EB105 factsheet); USA: Gallup "Most Important Problem" trend page, July 2026 column

| country | #1 issue (%) | #2 | #3 | #4 | #5 |
|---|---|---|---|---|---|
| Sweden | Threats to democracy (26, tied) | Environment/climate (26, tied) | Crime (18) | Security and defence (17) | Education system (16) |
| Poland | Rising prices/inflation/cost of living (30) | Security and defence (26) | Russia's invasion of Ukraine (19) | International situation (16) | Health (13; next: Middle East conflict 10) |
| Germany | Rising prices/inflation/cost of living (36) | Economic situation (20) | International situation (15) | Immigration (14) | Conflict in the Middle East (13) |
| France | Rising prices/inflation/cost of living (40) | Economic situation (17, tied) | Health (17, tied) | Government debt (16) | Crime (15) |
| Italy | Rising prices/inflation/cost of living (31) | Economic situation (21) | International situation (20) | Conflict in the Middle East (19) | Security and defence (14) |
| USA (Gallup, Jul 2026) | Government/poor leadership (28) | Immigration (12) | Economy in general (11, tied) | High cost of living/Inflation (11, tied) | Poverty/hunger/homelessness (6, tied with Unifying the country 6) |

### Source register + caveats
- **EB row-label alignment**: the annex PDF's country tables mis-align labels in plain-text extraction (labels print one row above their data). Re-extracted with pdftotext table mode and cross-verified against distinctive values (Ireland housing 59%, Finland unemployment 48%, Portugal health 40%, Cyprus Middle East 30%) before the SE/PL/DE/FR/IT rows were read. Figures are Mar/Apr 2026; multiple responses allowed (max 2), columns don't sum to 100.
- **EB source URLs**: Data Annex https://euneighbourseast.eu/wp-content/uploads/2026/05/standard-eurobarometer-105-spring-2026_data-annex_en.pdf (EC-published PDF mirrored by an official EU programme site); fieldwork dates confirmed in the EB105 factsheet (cdn.edupedu.ro mirror of the EC factsheet). The europa.eu survey page (europa.eu/eurobarometer/surveys/detail/3613) is a JS app and would not render for direct citation — the canonical-source citation is billed.
- **Gallup**: read from the embedded monthly table on https://news.gallup.com/poll/1675/most-important-problem.aspx (accessed 2026-08-28; July 2026 the latest column — no August release yet). The trends page does not print exact July fieldwork dates. "Economic problems (NET)" = 28% is a net, excluded from the top-5 in favour of specific categories.

*(Filed verbatim from the research agent's return, 2026-08-28 night.)*

---

## W-F3 (2026-08-30) — the gaps this file has, named rather than left implicit

The table above is SOURCED and stays. What follows is what it does NOT cover, so a reader does not
mistake its five rows for a complete salience model.

### ⚠ Sweden's TOP issue is not in the model, and is silently dropped

Eurobarometer 105 puts Sweden's most-mentioned national concern at **"threats to democracy" (26 %)**
— ahead of everything the game can represent. **§6's issue set has no slot for it**, so it is
dropped, and the four issues the harness runs on (climate .26, crime .18, defence .17, education .16)
are Sweden's SECOND through FIFTH concerns presented as if they were its first four.

**Billed, not forced.** Squeezing it into an existing slot would be worse than dropping it: it is not
crime, not defence, and not "the economic situation". Either §6 gains an eighth issue or the model
states that its issue set is a subset of what voters actually name. **That is a spec question, not a
data one.**

### ⚠ VINTAGE MISMATCH: the salience is 2026, the election being backtested is 2022

Every figure above is Spring 2026 fieldwork. The backtest, the seat reproduction, the AI campaign
staging and W-F1's returns are all **2022**. So the model campaigns on 2026's priorities toward
2022's result. For a prototype whose target is a 2026 election that is the right way round for
PLAY and the wrong way round for VALIDATION, and no figure anywhere should be read as "these were
the issues in 2022".

**A 2022-vintage reading is billed.** Eurobarometer 97/98 (Spring/Autumn 2022) covers the period and
is fetchable; it was not fetched because the live game is set in 2026 and the 2026 wave is the one
the player experiences.

### ⚠ SOM Institute: absent, and it is the right source for Sweden

The SOM Institute (Göteborgs universitet) runs the authoritative annual Swedish survey of issue
importance, with a far longer run and a finer issue set than Eurobarometer's two-mention cap.
**Nothing from it is on disk.** For a game whose anchor country is Sweden this is the largest single
source gap in the salience layer.

**Not fetched**: SOM's published tables are principally PDF report chapters and its microdata sits
behind SND registration. Cost is real and it is a session of its own.

### What is NOT wrong with this file

The EB105 figures were re-extracted with `pdftotext` in table mode and cross-verified against four
distinctive values in other countries' rows before Sweden's were trusted — the annex PDF
mis-aligns labels by one row in plain extraction, which is exactly the kind of error that would have
gone unnoticed. That check is recorded above and stands.