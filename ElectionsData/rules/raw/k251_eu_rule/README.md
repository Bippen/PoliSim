# K251: the EU rule's sources (2026-10-29)

Fetched byte-exact with curl from the Publications Office Cellar (EUR-Lex itself answers a JavaScript challenge, `ElectionsData/rules/fiscal_rules.md` NOT REACHED). The digests are in `SHA256SUMS.txt` beside this file.

| file | URL | what the rule reads in it |
|---|---|---|
| `eu_32024R1263_cellar.xhtml` | http://publications.europa.eu/resource/celex/32024R1263 | Regulation (EU) 2024/1263, Art. 7 (the debt sustainability safeguard: the ratio falls by at least 1 percentage point a year above 90 %, 0,5 between 60 and 90 %) and Art. 8 (the deficit resilience safeguard: adjustment continues until a structural deficit 1,5 % of GDP inside the 3 % reference, at 0,4 points a year, 0,25 with an extended period) |
| `eu_32024R1264_cellar.xhtml` | http://publications.europa.eu/resource/celex/32024R1264 | Regulation (EU) 2024/1264, Art. 1(2) (the new Art. 3(4) of Regulation (EC) No 1467/97): the corrective arm's minimum annual structural adjustment of 0,5 % of GDP while the deficit criterion is breached; recitals (15) and (23) say the same |

## BILLED

- **The Treaty on Stability, Coordination and Governance (TSCG), Art. 3(1)(b) and (d)**, the lower limit on the structural deficit: 0.5 % of GDP, or 1 % where the debt ratio is significantly below 60 %. It was not reached on 2026-10-29. The Cellar has no CELEX resource for it (`/resource/celex/42012A0302(01)` answered 404), and the Council's PDF (`https://www.consilium.europa.eu/media/20399/st00tscg26_en12.pdf`) answered 403 to a script. `AiFinanceMinistry` reads the two limits as DECLARED, from the European Parliament's Fact Sheet *The EU framework for fiscal policies*, which the class has always cited. The Fact Sheet is not on disk either. Regulation 2024/1263 recital (20) carries "the substance of Title III (Fiscal Compact) of the TSCG" into Union law, which is the ground for reading the 0.5 % limit above 60 % as stricter than Art. 8's 1.5 % (declared in `AiFinanceMinistry.EuHighDebtDeficitLimitPercent`). When the Treaty is fetched, read whether Title III binds Sweden and Poland: they are non-euro contracting parties, bound only by declaration.
