# Sweden - the debt anchor and the balance target (item B6)

Sourced 2026-10-02 for the designer's ruling: *"The 35 % debt anchor is a benchmark: when debt is more than 5 points of GDP away from it, the
government must explain why to the Riksdag in a written report with the spring budget (skr. 2025/26:76). Also dated in that report: the
surplus target (1/3 % of GDP) becomes a balance target (0 % over the cycle) from 2027."*

Source read: **Regeringens skrivelse 2025/26:76, *Det finanspolitiska ramverket*** (Finansdepartementet; signed "Stockholm den 4 december
2025" by Ulf Kristersson and Elisabeth Svantesson; tabled 9 Dec 2025; Riksdag id HD0376). Files in `raw/sweden_fiscal_framework/`: the
printed skrivelse `skr_2025-26_76_Skrivelse_76_202526.pdf` and the Riksdag's HTML/text renderings `prop_2025-26_76_HD0376.html` / `.txt`
(the API files a skrivelse under `doktyp=prop`; its `typrubrik` is "Regeringens skrivelse 2025/26:76"). Quotations are verbatim from the
Riksdag HTML text and were checked word for word against the PDF's text layer; page numbers are the skrivelse's printed pages. A gloss in
italics follows each. The skrivelse describes itself as a code of conduct ("fungerar som en uppförandekod för såväl regeringen som de aktörer
som granskar finanspolitiken") stating the framework as it stands once the budget-law changes of 31 Dec 2025 are in force.

## 1. The four targets (p. 3, summary under the figure)

> Målet för den offentliga förvaltningens finansiella sparande är en tredjedels procent av BNP i genomsnitt över en konjunkturcykel. Från och
> med 2027 och tills vidare är målet att uppnå balans över en konjunkturcykel. Skuldankaret på 35 procent av BNP är ett riktmärke för den
> offentliga förvaltningens konsoliderade bruttoskuld (den s.k. Maastrichtskulden) på medellång sikt.
>
> *Gloss: the general-government net-lending target is **one third of a per cent of GDP on average over a business cycle**. **From 2027, and
> until further notice, the target is balance over a business cycle.** The **debt anchor of 35 per cent of GDP** is a benchmark (riktmärke) for
> the consolidated gross debt (Maastricht debt) over the medium term.*

## 2. The balance target and its decision (p. 5, section 3.1)

> Målet för den offentliga förvaltningens finansiella sparande är en tredjedels procent av BNP i genomsnitt över en konjunkturcykel (prop.
> 2017/18:1 Förslag till statens budget, finansplan m.m. avsnitt 5.6, bet. 2017/18:FiU1, rskr. 2017/18:54). Riksdagen har fastställt att målet
> för den offentliga förvaltningens finansiella sparande fr.o.m. 2027 ska vara ett sparande i balans över en konjunkturcykel (prop. 2025/26:1
> Förslag till statens budget, finansplan m.m. avsnitt 5.5.4, bet. 2025/26:FiU1, rskr. 2025/26:64). Det innebär att det offentliga finansiella
> sparandet ska uppgå till 0 procent av BNP i genomsnitt över en konjunkturcykel.
>
> *Gloss: the current target, 1/3 % of GDP over the cycle, dates from the 2018 budget. **The Riksdag has fixed that from 2027 the target is
> net lending in balance over a business cycle, i.e. 0 per cent of GDP on average over the cycle** (budget bill 2025/26:1, committee report
> 2025/26:FiU1, Riksdag communication rskr. 2025/26:64).*

**Decision date (not in the skrivelse; read from the Riksdag record).** Bet. 2025/26:FiU1, *Statens budget 2026 - Rambeslutet*, point 2 a
(`raw/sweden_fiscal_framework/utskottsforslag_HD01FiU1.xml`):

> a) Målet för den offentliga förvaltningens finansiella sparande
> Riksdagen fastställer målet för den offentliga förvaltningens finansiella sparande till ett sparande i balans över en konjunkturcykel
> fr.o.m. 2027.
>
> *Gloss: "The Riksdag fixes the general-government net-lending target as net lending in balance over a business cycle from 2027."*

Point 2 went to a vote (committee proposal against reservation 5, S). The activity log of `dokumentstatus_HD01FiU1.json` records **"Beslut"
on 2025-11-26** (debated the same day). The skrivelse gives no date of its own; the Riksdag decision on the balance target was **26 Nov 2025**.

Related rule (p. 6) for how a miss of the net-lending target is judged: *"Riktmärket för en tydlig avvikelse är att det strukturella
sparandet avviker med mer än 0,5 procent av potentiell BNP från målnivån"*. A clear deviation means structural net lending more than **0.5 %
of potential GDP** from the target. It is explained in the budget bill and the spring bill, with a plan for returning to the target.

## 3. The debt anchor, its ±5 band and the reporting duty (p. 7, section 3.2 "Skuldankaret")

> Skuldankaret är ett riktmärke för den offentliga förvaltningens konsoliderade bruttoskuld (Maastrichtskulden). Skuldankaret är satt till 35
> procent av BNP och gäller tills vidare. Till skillnad mot målet för den offentliga förvaltningens finansiella sparande är skuldankaret inte
> ett operativt mål i den meningen att det styr finanspolitiken på kort sikt [...].
>
> *Gloss: the debt anchor is a **benchmark** for consolidated gross (Maastricht) debt, **set at 35 per cent of GDP until further notice**;
> unlike the net-lending target it is **not an operational target** steering fiscal policy in the short run.*

> Regeringen ska årligen redogöra för bruttoskuldens utveckling i den ekonomiska vårpropositionen. Om skuldkvoten avviker, uppåt eller nedåt,
> från skuldankaret med mer än 5 procent av BNP enligt utfallet i nationalräkenskaperna för det föregående året, eller enligt prognoserna för
> det innevarande eller nästkommande budgetåret, ska regeringen i samband med den ekonomiska vårpropositionen i en skrivelse till riksdagen
> förklara vad som har orsakat avvikelsen och hur den ska hanteras. Finansutskottet kan vid sin behandling av skrivelsen besluta om en
> offentlig utfrågning av finansministern om skuldsituationen.
>
> *Gloss: every year the government reports gross-debt developments in the **spring fiscal policy bill**. **If the debt ratio deviates, up or
> down, from the anchor by more than 5 per cent of GDP** - on the national-accounts outcome for the previous year, or on the forecasts for the
> current or the next budget year - **the government must, together with the spring bill, explain in a skrivelse (written communication) to
> the Riksdag what caused the deviation and how it will be handled.** The Finance Committee may then decide on a public hearing of the
> Minister for Finance on the debt situation.*

## 4. Against the ruling

| Ruling | As read | Note |
|---|---|---|
| 35 % debt anchor, a benchmark | "Skuldankaret är satt till 35 procent av BNP och gäller tills vidare"; "ett riktmärke", "inte ett operativt mål" | agrees |
| >5 points of GDP away | "avviker, uppåt eller nedåt, ... med mer än 5 procent av BNP" | agrees. The text says either direction, and the test is the previous year's outcome **or** the forecast for the current or next budget year |
| explain why to the Riksdag in a written report with the spring budget | "i samband med den ekonomiska vårpropositionen i en skrivelse till riksdagen förklara vad som har orsakat avvikelsen och hur den ska hanteras" | agrees. It also requires saying *how the deviation will be handled*, and the Finance Committee may hold a public hearing of the finance minister |
| surplus target 1/3 % of GDP | "en tredjedels procent av BNP i genomsnitt över en konjunkturcykel" | agrees |
| balance target (0 % over the cycle) from 2027 | "fr.o.m. 2027 ... sparande i balans över en konjunkturcykel ... 0 procent av BNP i genomsnitt över en konjunkturcykel" | agrees. The skrivelse records it as already fixed by the Riksdag (rskr. 2025/26:64); the decision was 26 Nov 2025 |
| skr. 2025/26:76 | "Regeringens skrivelse 2025/26:76", signed 4 Dec 2025, tabled 9 Dec 2025 | agrees. The Riksdag placed it on file ("lägger skrivelse 2025/26:76 till handlingarna", bet. 2025/26:FiU14) on 2026-02-25 |

No figure differs.
