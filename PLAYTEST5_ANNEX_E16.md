# Playtest 5 annex — E-16 (2026-09-05)

**What this file is.** The two text artifacts Track C's catalog (P5-C1) waits on, written into
the repo directly because chat attachments have failed to reach disk four times this month. The
third artifact — the pie screenshot — is a PNG Elias saves into `PoliSim-captures\Elias Screenshots`
beside the sixteen already bound; a session selects it by modification time and verifies it by
content, as before.

---

## 1. Elias's findings, verbatim

> **Statistics:** Looks weird with the pie chart and arrows.
>
> **Budget issue:** The spending sliders are as a percentage in reduction or increase, I want to
> move away from that now. Set numbers on the sliders instead of percentage that normalize after a
> year. I want cost and revenue to appreciate based on gdp growth or inflation and unemployment and
> other factors that impact budget which i don't feel it is now. Make sure all spending and tax
> change sliders have impact on economy or other things, some sliders don't now. I want more welfare
> factors calculated such as healthcare quality wait times and other things. See list below.

## 2. The metric list, verbatim — REFERENCE MATERIAL, NOT A SPEC

⚠ This is a foreign game's wiki (a US-centric political simulator: counties, Medicare/Medicaid,
partisan military approval). It is the *breadth* Elias wants, not the *content*. P5-C1's catalog
maps each row to a sourced equivalent for this game's six countries, or rules it N/A with a reason.
Nothing here is a figure to seed.

| Category | Metrics as listed |
|---|---|
| Economy | Poverty rate; Per-capita income |
| Education | Graduation rate; Academic score |
| Poverty | Poverty rate; Poverty effect |
| Taxation | Low-, Medium-, Upper-income tax rate (% of income) — each rated separately; note the low-income rating of 0.1% at 23%, so the rating curve is steep on the low bracket |
| Health Care | Health coverage; Retiree health coverage; Private health care quality; Medicare quality; Medicaid quality |
| Crime | Crime rate |
| Infrastructure | Road congestion; Roads in poor condition |
| National Debt | Debt (total); Debt per person |
| Environment | Electricity CO₂ per person (lbs); Car CO₂ per person (lbs) |
| Immigration | Illegal immigrant count |
| Military | Democratic / Republican / Independent approval (partisan opinion, not a metric) |
| Department Effectiveness | Education, Security, Health, Agriculture, State, Transportation, Energy, Science & Nature — effectiveness = funding received ÷ department's budget request |

**The metric tabs (the underlying simulated stats), as listed:**

- **Population** — voter registration and age demographics; party demographics per county (default
  5% independents), ancestry demographics, ideology, and turnout.
- **Education** — academic score, dropout rate and its causes, student/teacher ratios,
  higher-education attainment, general educational attainment, graduation rate.
- **Poverty** — poverty rate, unemployment and underemployment, poverty effect, homelessness.
- **Economy** — per-capita income and an employment breakdown.
- **Taxes** — tax revenue and its sources (the three bracket rates, plus flat tax,
  deductions/exemptions, property and school property tax, city income tax as levers).
- **Health** — coverage by type and by quality, life expectancy, birth/death ratio.
- **Crime** — police officers, arrest rates, crimes committed, jailed and for what offences,
  prisoner locations, guns.
- **Infrastructure** — road length, road quality, road congestion, energy sources by type.
- **Budget** — revenues, expenditures by category; mandatory (Social Security, Medicare, Medicaid,
  food stamps) vs discretionary (military, NASA).
- **Military** — no simulated metric beyond partisan approval.
- **Immigration** — illegal immigrant count.
- **Laws** — the status (active/inactive) of every law; 80+ legislation types.
- **Other** — remainder (environment lives here or in Infrastructure's energy section).

## 3. The mapping rules already ruled (so P5-C1 starts from them, not from scratch)

- **Keep** where a sourced series exists for six countries: OECD wait times, PISA, Eurostat
  graduation/attainment, Eurostat/EDGAR CO₂, WEF/OECD road quality, the national offices.
- **Map** the US-specific term to its general form: Medicare/Medicaid → public vs private coverage
  and quality; illegal immigrant count → irregular migration only where a sourced estimate exists,
  else absent and stated; Social Security/food stamps → the pension and transfer lines already
  on the book.
- **N/A with reason:** counties; partisan military approval; city and school property taxes;
  anything the six-country frame cannot hold.
- **Tax brackets** stay behind F4 (the cohort income dimension), where they already wait.
- **Department effectiveness** is lifted whole as a mechanic: funding received ÷ requested, where
  the request is P5-B2's indexed line, scaled by the minister's efficiency attribute (P2-5).
- **Order by gameplay impact:** health → education → infrastructure → environment → immigration
  and poverty depth → effectiveness, each its own BASELINE family, on the one society-stat grammar
  D15 item 3 asks Design to draw on health.
