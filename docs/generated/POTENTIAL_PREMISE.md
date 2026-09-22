# The premise of potential output - measured, no player, 100 turns (P5-B7; re-measured after FT-7's second seat, §394 - the labour input is employment, the seed potentials re-solved; re-measured after PN-3, §522 - the labour force is the pyramid's, the 15+ population at the state's rate, not the 20–64 window)


> ⚠ **THE WORLD THIS DUMP MEASURES: no player, and THE AI FINANCE MINISTRY IS LIVE** (`SimulationManager.AiFinanceMinistryEnabled` defaults true and no `PlayerCountryId` is set, so every country is AI-governed). The last column is debt over GDP. **FT-13 (`COMPLETED.md` §579) is open on it**: these figures and §388's table - *every AI state inside the 60 % reference by year 100* - disagree by an order of magnitude, and the dump's own harness (one `PolicyDecision.None()` handed to `AdvanceTurn` for a century, which the ministry writes into) is the first thing to check.
**What set potential growth before P5-B7 (`MacroSystem.ApplySectorGrowthEffect`, measured 2026-09-05 as `potential01`):** `Country.PotentialGrowthRate` = clamp(`Country.BasePotentialGrowthRate` + the infrastructure adjustment + the sector adjustment, 0, 8) - a seeded trend (USA 2.0, Sweden 1.5, Germany 0.8, France 0.8, Italy 0.8, Poland 3.5 % a year) plus two ceilinged policy adjustments, read as trend labour productivity (Q3) and assigned to potential 1:1; `MacroSystem.ApplyPotentialGdpGrowthDaily` compounded `EconomyState.PotentialGDP` at that rate every day. **What it ignored:** the labour input - the 20–64 cohort, participation and the natural rate entered potential nowhere, so a country whose working-age population halved kept its potential output and, since P5-B3, lost its tax base against it. **After P5-B7 (`PotentialOutput`):** potential is its factors - the seed's potential × the labour input's ratio to the seed × a productivity index compounding at the ledger's trend, the trend re-seeded from the sourced series (Eurostat nama_10_lp_ulc, BLS PRS85006092; USA 1.613, Sweden 1.019, Germany 0.938, France 0.513, Italy 0.119, Poland 3.019) - and `Country.PotentialGrowthRate` is derived from them once a turn. The table below is whichever tree ran it; the two runs are kept in `COMPLETED.md` §322 side by side.

**Labour input** = the labour force × (1 − `EconomyState.Unemployment` / 100) (`PotentialOutput.LabourInput`, employment since §394), where the labour force is the population aged 15 and over × `EconomyState.LaborForceParticipationRate` / 100 (`PotentialOutput.LabourForce`, since PN-3 §522 - the labour force the pyramid implies at the sourced rates by age, scaled by the levers' deviation; before §522 it was the 20–64 cohort × the same 15+ rate, a count on one base and a rate on another). **Labour × productivity** = that input times `EconomyState.Productivity` (the stat, which compounds at the ledger's trend plus the hoarding cycle), both against their seeds - what potential would read if it were built from its factors.

## USA - base trend 1.613 % a year; seed potential 29151.83, GDP 29000, labour input 167.917 M employed, productivity 90.83

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | -0.478 | 0.995 | 0.964 | 1.002 | 61.814 | 0.979 | 1.014 | 0.993 | 135.645 |
| 10 | 1.868 | 1.223 | 1.102 | 1.021 | 61.199 | 1.042 | 1.132 | 1.18 | 155.395 |
| 20 | 1.547 | 1.461 | 1.287 | 1.044 | 60.736 | 1.061 | 1.322 | 1.403 | 181.078 |
| 30 | 1.269 | 1.721 | 1.517 | 1.046 | 60.011 | 1.065 | 1.55 | 1.65 | 218.513 |
| 40 | 2.224 | 2.025 | 1.733 | 1.036 | 58.892 | 1.067 | 1.803 | 1.924 | 260.119 |
| 50 | 0.77 | 2.341 | 1.991 | 1.017 | 57.609 | 1.051 | 2.111 | 2.22 | 309.101 |
| 60 | 1.813 | 2.735 | 2.332 | 1.006 | 56.66 | 1.046 | 2.493 | 2.609 | 376.364 |
| 70 | 1.255 | 3.154 | 2.681 | 0.998 | 56.075 | 1.028 | 2.944 | 3.027 | 465.617 |
| 80 | 1.267 | 3.661 | 3.107 | 0.993 | 55.642 | 1.017 | 3.474 | 3.533 | 572.199 |
| 90 | 0.636 | 4.263 | 3.613 | 0.995 | 55.404 | 1.009 | 4.097 | 4.134 | 698.093 |
| 100 | 0.837 | 5.01 | 4.243 | 0.996 | 55.211 | 1.01 | 4.818 | 4.868 | 841.033 |

## Sweden - base trend 1.019 % a year; seed potential 630.109, GDP 620, labour input 5.338 M employed, productivity 89.95

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 4.248 | 1.042 | 1.038 | 1.006 | 65.309 | 1.032 | 1.004 | 1.036 | 33.61 |
| 10 | 1.675 | 1.192 | 1.219 | 1.05 | 64.611 | 1.077 | 1.098 | 1.183 | 34.304 |
| 20 | 1.206 | 1.363 | 1.386 | 1.096 | 63.69 | 1.113 | 1.219 | 1.356 | 40.092 |
| 30 | 1.519 | 1.542 | 1.572 | 1.108 | 62.387 | 1.137 | 1.348 | 1.532 | 47.113 |
| 40 | 1.141 | 1.722 | 1.755 | 1.122 | 60.494 | 1.146 | 1.495 | 1.714 | 51.934 |
| 50 | 0.956 | 1.905 | 1.95 | 1.129 | 58.954 | 1.145 | 1.669 | 1.91 | 63.565 |
| 60 | 0.84 | 2.082 | 2.15 | 1.115 | 57.335 | 1.129 | 1.861 | 2.102 | 79.174 |
| 70 | 1.016 | 2.273 | 2.349 | 1.108 | 55.919 | 1.113 | 2.078 | 2.313 | 98.769 |
| 80 | 0.506 | 2.475 | 2.579 | 1.106 | 54.895 | 1.094 | 2.333 | 2.552 | 121.677 |
| 90 | 1.088 | 2.719 | 2.818 | 1.106 | 54.383 | 1.084 | 2.609 | 2.828 | 145.426 |
| 100 | -0.17 | 2.97 | 3.108 | 1.106 | 54.043 | 1.069 | 2.918 | 3.118 | 178.782 |

## Germany - base trend 0.938 % a year; seed potential 4709.741, GDP 4700, labour input 42.192 M employed, productivity 94.54

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.201 | 1.002 | 1.004 | 0.993 | 60.777 | 0.993 | 1.009 | 1.001 | 65.105 |
| 10 | 1.155 | 1.069 | 1.038 | 0.929 | 58.858 | 0.976 | 1.061 | 1.035 | 66.121 |
| 20 | 0.72 | 1.142 | 1.105 | 0.935 | 57.216 | 0.95 | 1.181 | 1.121 | 73.106 |
| 30 | 0.214 | 1.217 | 1.167 | 0.912 | 56.334 | 0.921 | 1.304 | 1.202 | 86.182 |
| 40 | 1.285 | 1.311 | 1.229 | 0.892 | 55.351 | 0.904 | 1.427 | 1.29 | 101.54 |
| 50 | 0.78 | 1.419 | 1.33 | 0.89 | 54.446 | 0.891 | 1.576 | 1.404 | 111.792 |
| 60 | 0.809 | 1.53 | 1.427 | 0.877 | 53.655 | 0.875 | 1.735 | 1.518 | 133.387 |
| 70 | 0.774 | 1.653 | 1.534 | 0.858 | 52.616 | 0.861 | 1.9 | 1.636 | 154.094 |
| 80 | -0.361 | 1.769 | 1.616 | 0.854 | 51.704 | 0.84 | 2.097 | 1.761 | 175.383 |
| 90 | 0.845 | 1.934 | 1.769 | 0.854 | 51.327 | 0.839 | 2.312 | 1.941 | 202.402 |
| 100 | 0.847 | 2.101 | 1.92 | 0.854 | 51.079 | 0.836 | 2.534 | 2.118 | 242.581 |

## France - base trend 0.513 % a year; seed potential 3193.096, GDP 3200, labour input 29.719 M employed, productivity 86.32

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.952 | 1.01 | 1.017 | 1 | 55.651 | 1.004 | 1.006 | 1.01 | 119.835 |
| 10 | 0.112 | 1.051 | 1.026 | 0.992 | 54.42 | 1 | 1.032 | 1.032 | 141.031 |
| 20 | -0.216 | 1.086 | 1.037 | 0.972 | 52.892 | 0.982 | 1.078 | 1.059 | 158.188 |
| 30 | -0.512 | 1.114 | 1.024 | 0.954 | 51.567 | 0.958 | 1.129 | 1.081 | 170.559 |
| 40 | 0.569 | 1.152 | 1.015 | 0.947 | 50.653 | 0.943 | 1.18 | 1.113 | 186.062 |
| 50 | 0.329 | 1.176 | 1.032 | 0.917 | 49.549 | 0.917 | 1.235 | 1.132 | 207.874 |
| 60 | -0.014 | 1.205 | 1.057 | 0.898 | 48.464 | 0.894 | 1.303 | 1.165 | 199.584 |
| 70 | 0.23 | 1.241 | 1.097 | 0.887 | 47.788 | 0.876 | 1.382 | 1.21 | 199.431 |
| 80 | 0.281 | 1.287 | 1.14 | 0.88 | 47.312 | 0.863 | 1.463 | 1.262 | 194.14 |
| 90 | 0.529 | 1.35 | 1.197 | 0.88 | 47.122 | 0.86 | 1.547 | 1.33 | 183.993 |
| 100 | 0.506 | 1.416 | 1.259 | 0.88 | 47.002 | 0.857 | 1.635 | 1.401 | 180.209 |

## Italy - base trend 0.119 % a year; seed potential 2295.011, GDP 2300, labour input 23.378 M employed, productivity 78.2

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | -1.362 | 0.986 | 0.986 | 0.996 | 48.763 | 0.985 | 1.002 | 0.987 | 144.156 |
| 10 | -0.299 | 0.975 | 0.939 | 0.94 | 47.001 | 0.965 | 0.977 | 0.943 | 160.824 |
| 20 | -0.903 | 0.918 | 0.854 | 0.871 | 44.704 | 0.901 | 0.98 | 0.883 | 190.645 |
| 30 | -0.592 | 0.879 | 0.816 | 0.84 | 43.366 | 0.855 | 1.001 | 0.855 | 214.42 |
| 40 | 0.214 | 0.84 | 0.764 | 0.814 | 42.862 | 0.811 | 1.017 | 0.825 | 253.363 |
| 50 | -0.437 | 0.8 | 0.722 | 0.766 | 42.187 | 0.768 | 1.019 | 0.783 | 288.782 |
| 60 | -1.02 | 0.767 | 0.69 | 0.732 | 41.102 | 0.733 | 1.024 | 0.751 | 316.373 |
| 70 | -0.235 | 0.744 | 0.662 | 0.72 | 40.497 | 0.709 | 1.037 | 0.735 | 262.553 |
| 80 | -0.294 | 0.733 | 0.655 | 0.714 | 40.361 | 0.694 | 1.054 | 0.731 | 270.473 |
| 90 | 0.683 | 0.741 | 0.665 | 0.714 | 40.319 | 0.698 | 1.062 | 0.741 | 267.67 |
| 100 | 0.253 | 0.742 | 0.666 | 0.713 | 40.277 | 0.695 | 1.069 | 0.743 | 291.806 |

## Poland - base trend 3.019 % a year; seed potential 843.552, GDP 840, labour input 16.789 M employed, productivity 54.09

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.343 | 1.003 | 0.99 | 0.992 | 57.145 | 0.974 | 1.029 | 1.002 | 66.564 |
| 10 | 2.319 | 1.275 | 1.203 | 0.951 | 55.521 | 0.947 | 1.301 | 1.232 | 61.294 |
| 20 | 0.607 | 1.554 | 1.446 | 0.87 | 52.376 | 0.857 | 1.733 | 1.486 | 65.506 |
| 30 | 1.604 | 1.884 | 1.741 | 0.765 | 48.103 | 0.772 | 2.337 | 1.804 | 71.106 |
| 40 | 2.6 | 2.313 | 2.086 | 0.724 | 44.93 | 0.703 | 3.252 | 2.287 | 84.361 |
| 50 | 2.162 | 2.873 | 2.561 | 0.7 | 43.765 | 0.648 | 4.522 | 2.932 | 99.882 |
| 60 | 2.848 | 3.661 | 3.23 | 0.665 | 43.099 | 0.613 | 6.162 | 3.78 | 126.155 |
| 70 | 2.96 | 4.807 | 4.221 | 0.657 | 42.537 | 0.598 | 8.373 | 5.005 | 113.965 |
| 80 | 2.241 | 6.407 | 5.628 | 0.659 | 42.269 | 0.591 | 11.354 | 6.715 | 92.196 |
| 90 | 3.135 | 8.63 | 7.583 | 0.659 | 42.095 | 0.592 | 15.358 | 9.087 | 121.538 |
| 100 | 2.713 | 11.527 | 10.13 | 0.66 | 41.932 | 0.588 | 20.735 | 12.187 | 125.111 |

**Reading it:** where `potential ÷ seed` runs ahead of `labour × productivity ÷ seed`, potential is carrying output that no worker produces; the debt column is the fiscal book paying for the difference since P5-B3 put the tax bases on the wage bill.
