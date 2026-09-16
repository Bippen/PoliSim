# The premise of potential output - measured, no player, 100 turns (P5-B7; re-measured after FT-7's second seat, §394 - the labour input is employment, the seed potentials re-solved; re-measured after PN-3, §522 - the labour force is the pyramid's, the 15+ population at the state's rate, not the 20–64 window)

**What set potential growth before P5-B7 (`MacroSystem.ApplySectorGrowthEffect`, measured 2026-09-05 as `potential01`):** `Country.PotentialGrowthRate` = clamp(`Country.BasePotentialGrowthRate` + the infrastructure adjustment + the sector adjustment, 0, 8) - a seeded trend (USA 2.0, Sweden 1.5, Germany 0.8, France 0.8, Italy 0.8, Poland 3.5 % a year) plus two ceilinged policy adjustments, read as trend labour productivity (Q3) and assigned to potential 1:1; `MacroSystem.ApplyPotentialGdpGrowthDaily` compounded `EconomyState.PotentialGDP` at that rate every day. **What it ignored:** the labour input - the 20–64 cohort, participation and the natural rate entered potential nowhere, so a country whose working-age population halved kept its potential output and, since P5-B3, lost its tax base against it. **After P5-B7 (`PotentialOutput`):** potential is its factors - the seed's potential × the labour input's ratio to the seed × a productivity index compounding at the ledger's trend, the trend re-seeded from the sourced series (Eurostat nama_10_lp_ulc, BLS PRS85006092; USA 1.613, Sweden 1.019, Germany 0.938, France 0.513, Italy 0.119, Poland 3.019) - and `Country.PotentialGrowthRate` is derived from them once a turn. The table below is whichever tree ran it; the two runs are kept in `COMPLETED.md` §322 side by side.

**Labour input** = the labour force × (1 − `EconomyState.Unemployment` / 100) (`PotentialOutput.LabourInput`, employment since §394), where the labour force is the population aged 15 and over × `EconomyState.LaborForceParticipationRate` / 100 (`PotentialOutput.LabourForce`, since PN-3 §522 - the labour force the pyramid implies at the sourced rates by age, scaled by the levers' deviation; before §522 it was the 20–64 cohort × the same 15+ rate, a count on one base and a rate on another). **Labour × productivity** = that input times `EconomyState.Productivity` (the stat, which compounds at the ledger's trend plus the hoarding cycle), both against their seeds - what potential would read if it were built from its factors.

## USA - base trend 1.613 % a year; seed potential 29151.83, GDP 29000, labour input 167.917 M employed, productivity 90.83

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | -0.478 | 0.995 | 0.964 | 1.002 | 61.814 | 0.979 | 1.014 | 0.993 | 135.667 |
| 10 | 1.912 | 1.223 | 1.095 | 1.021 | 61.187 | 1.042 | 1.131 | 1.178 | 155.406 |
| 20 | 1.493 | 1.461 | 1.292 | 1.044 | 60.748 | 1.061 | 1.323 | 1.404 | 182.218 |
| 30 | 1.312 | 1.719 | 1.474 | 1.046 | 59.965 | 1.063 | 1.54 | 1.637 | 215.331 |
| 40 | 2.228 | 2.025 | 1.703 | 1.036 | 58.892 | 1.067 | 1.794 | 1.914 | 257.192 |
| 50 | 0.789 | 2.341 | 1.943 | 1.017 | 57.607 | 1.051 | 2.097 | 2.204 | 306.488 |
| 60 | 1.819 | 2.735 | 2.275 | 1.006 | 56.666 | 1.046 | 2.475 | 2.59 | 375.612 |
| 70 | 1.265 | 3.154 | 2.615 | 0.998 | 56.084 | 1.028 | 2.921 | 3.004 | 466.241 |
| 80 | 1.277 | 3.662 | 3.029 | 0.993 | 55.652 | 1.017 | 3.447 | 3.506 | 574.284 |
| 90 | 0.635 | 4.264 | 3.522 | 0.995 | 55.415 | 1.009 | 4.065 | 4.102 | 700.456 |
| 100 | 0.859 | 5.011 | 4.134 | 0.996 | 55.222 | 1.011 | 4.779 | 4.83 | 844.848 |

## Sweden - base trend 1.019 % a year; seed potential 630.109, GDP 620, labour input 5.338 M employed, productivity 89.95

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 4.248 | 1.042 | 1.038 | 1.006 | 65.309 | 1.032 | 1.004 | 1.036 | 33.565 |
| 10 | 1.668 | 1.193 | 1.205 | 1.05 | 64.681 | 1.078 | 1.094 | 1.179 | 36.59 |
| 20 | 1.241 | 1.368 | 1.375 | 1.096 | 63.987 | 1.117 | 1.21 | 1.352 | 44.556 |
| 30 | 1.586 | 1.554 | 1.561 | 1.108 | 62.959 | 1.146 | 1.332 | 1.527 | 53.891 |
| 40 | 1.2 | 1.743 | 1.746 | 1.122 | 61.331 | 1.161 | 1.471 | 1.707 | 60.84 |
| 50 | 0.999 | 1.937 | 1.946 | 1.129 | 60.043 | 1.165 | 1.634 | 1.903 | 75.763 |
| 60 | 0.882 | 2.126 | 2.148 | 1.115 | 58.637 | 1.154 | 1.814 | 2.093 | 94.779 |
| 70 | 1.039 | 2.329 | 2.342 | 1.108 | 57.392 | 1.141 | 2.016 | 2.301 | 116.288 |
| 80 | 0.568 | 2.542 | 2.566 | 1.106 | 56.511 | 1.125 | 2.254 | 2.535 | 139.906 |
| 90 | 1.102 | 2.8 | 2.802 | 1.106 | 56.122 | 1.118 | 2.511 | 2.807 | 163.593 |
| 100 | -0.099 | 3.062 | 3.077 | 1.106 | 55.874 | 1.104 | 2.799 | 3.09 | 193.158 |

## Germany - base trend 0.938 % a year; seed potential 4709.741, GDP 4700, labour input 42.192 M employed, productivity 94.54

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.201 | 1.002 | 1.004 | 0.993 | 60.777 | 0.993 | 1.009 | 1.001 | 65.118 |
| 10 | 1.025 | 1.068 | 1.032 | 0.929 | 58.863 | 0.974 | 1.06 | 1.033 | 71.247 |
| 20 | 0.722 | 1.143 | 1.1 | 0.936 | 57.228 | 0.95 | 1.179 | 1.12 | 79.563 |
| 30 | 0.207 | 1.217 | 1.159 | 0.913 | 56.347 | 0.922 | 1.301 | 1.2 | 93.577 |
| 40 | 1.27 | 1.311 | 1.219 | 0.892 | 55.362 | 0.904 | 1.424 | 1.288 | 110.106 |
| 50 | 0.764 | 1.418 | 1.318 | 0.891 | 54.454 | 0.891 | 1.572 | 1.4 | 120.345 |
| 60 | 0.795 | 1.53 | 1.414 | 0.877 | 53.66 | 0.875 | 1.73 | 1.514 | 142.377 |
| 70 | 0.752 | 1.651 | 1.516 | 0.858 | 52.613 | 0.861 | 1.893 | 1.629 | 163.811 |
| 80 | -0.396 | 1.765 | 1.593 | 0.854 | 51.705 | 0.84 | 2.085 | 1.751 | 186.633 |
| 90 | 0.828 | 1.926 | 1.741 | 0.854 | 51.335 | 0.84 | 2.294 | 1.926 | 214.846 |
| 100 | 0.831 | 2.088 | 1.885 | 0.854 | 51.089 | 0.836 | 2.509 | 2.097 | 255.915 |

## France - base trend 0.513 % a year; seed potential 3193.096, GDP 3200, labour input 29.719 M employed, productivity 86.32

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.952 | 1.01 | 1.017 | 1 | 55.651 | 1.004 | 1.006 | 1.01 | 119.834 |
| 10 | 0.105 | 1.051 | 1.039 | 0.992 | 54.436 | 1 | 1.036 | 1.036 | 144.848 |
| 20 | -0.205 | 1.086 | 1.049 | 0.972 | 52.895 | 0.982 | 1.082 | 1.062 | 164.214 |
| 30 | -0.528 | 1.114 | 1.035 | 0.954 | 51.569 | 0.958 | 1.133 | 1.085 | 178.541 |
| 40 | 0.61 | 1.152 | 1.025 | 0.947 | 50.654 | 0.943 | 1.184 | 1.116 | 194.076 |
| 50 | 0.341 | 1.176 | 1.042 | 0.917 | 49.55 | 0.916 | 1.239 | 1.135 | 223.329 |
| 60 | -0.007 | 1.203 | 1.054 | 0.898 | 48.431 | 0.893 | 1.303 | 1.164 | 240.673 |
| 70 | 0.261 | 1.241 | 1.097 | 0.887 | 47.748 | 0.877 | 1.38 | 1.21 | 213.589 |
| 80 | 0.245 | 1.283 | 1.134 | 0.88 | 47.253 | 0.862 | 1.462 | 1.259 | 234.198 |
| 90 | 0.527 | 1.345 | 1.188 | 0.88 | 47.054 | 0.859 | 1.545 | 1.326 | 223.823 |
| 100 | 0.505 | 1.411 | 1.249 | 0.88 | 46.928 | 0.855 | 1.632 | 1.396 | 216.993 |

## Italy - base trend 0.119 % a year; seed potential 2295.011, GDP 2300, labour input 23.378 M employed, productivity 78.2

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | -1.362 | 0.986 | 0.986 | 0.996 | 48.763 | 0.985 | 1.002 | 0.987 | 144.208 |
| 10 | -0.336 | 0.975 | 0.919 | 0.94 | 46.984 | 0.964 | 0.972 | 0.938 | 166.167 |
| 20 | -0.901 | 0.918 | 0.832 | 0.871 | 44.699 | 0.901 | 0.974 | 0.877 | 199.651 |
| 30 | -0.589 | 0.879 | 0.794 | 0.84 | 43.371 | 0.855 | 0.993 | 0.849 | 225.155 |
| 40 | 0.213 | 0.841 | 0.744 | 0.814 | 42.879 | 0.812 | 1.009 | 0.819 | 265.567 |
| 50 | -0.422 | 0.802 | 0.702 | 0.767 | 42.215 | 0.769 | 1.011 | 0.777 | 303.073 |
| 60 | -1.018 | 0.769 | 0.669 | 0.732 | 41.135 | 0.734 | 1.014 | 0.745 | 345.579 |
| 70 | -0.235 | 0.744 | 0.635 | 0.721 | 40.497 | 0.709 | 1.025 | 0.727 | 379.589 |
| 80 | -0.398 | 0.728 | 0.616 | 0.714 | 40.252 | 0.693 | 1.034 | 0.717 | 356.396 |
| 90 | 0.484 | 0.731 | 0.627 | 0.713 | 40.155 | 0.695 | 1.042 | 0.724 | 331.875 |
| 100 | 0.216 | 0.729 | 0.622 | 0.713 | 40.056 | 0.691 | 1.047 | 0.723 | 355.146 |

## Poland - base trend 3.019 % a year; seed potential 843.552, GDP 840, labour input 16.789 M employed, productivity 54.09

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.343 | 1.003 | 0.99 | 0.992 | 57.145 | 0.974 | 1.029 | 1.002 | 66.551 |
| 10 | 2.437 | 1.274 | 1.166 | 0.951 | 55.51 | 0.947 | 1.291 | 1.222 | 65.899 |
| 20 | 0.764 | 1.558 | 1.415 | 0.87 | 52.419 | 0.859 | 1.72 | 1.478 | 73.663 |
| 30 | 1.658 | 1.887 | 1.703 | 0.765 | 48.153 | 0.773 | 2.321 | 1.794 | 85.189 |
| 40 | 2.594 | 2.316 | 2.045 | 0.724 | 44.967 | 0.704 | 3.231 | 2.276 | 100.496 |
| 50 | 2.138 | 2.874 | 2.512 | 0.7 | 43.784 | 0.649 | 4.496 | 2.917 | 115.224 |
| 60 | 2.875 | 3.663 | 3.169 | 0.665 | 43.102 | 0.614 | 6.129 | 3.761 | 143.385 |
| 70 | 2.888 | 4.804 | 4.147 | 0.657 | 42.529 | 0.597 | 8.336 | 4.98 | 169.388 |
| 80 | 2.24 | 6.393 | 5.475 | 0.659 | 42.228 | 0.59 | 11.287 | 6.663 | 186.724 |
| 90 | 3.136 | 8.616 | 7.391 | 0.659 | 42.077 | 0.591 | 15.243 | 9.016 | 183.116 |
| 100 | 2.706 | 11.495 | 9.87 | 0.66 | 41.921 | 0.588 | 20.559 | 12.08 | 168.582 |

**Reading it:** where `potential ÷ seed` runs ahead of `labour × productivity ÷ seed`, potential is carrying output that no worker produces; the debt column is the fiscal book paying for the difference since P5-B3 put the tax bases on the wage bill.
