# The premise of potential output - measured, no player, 100 turns (P5-B7; re-measured after FT-7's second seat, §394 - the labour input is employment, the seed potentials re-solved)

**What set potential growth before P5-B7 (`MacroSystem.ApplySectorGrowthEffect`, measured 2026-09-05 as `potential01`):** `Country.PotentialGrowthRate` = clamp(`Country.BasePotentialGrowthRate` + the infrastructure adjustment + the sector adjustment, 0, 8) - a seeded trend (USA 2.0, Sweden 1.5, Germany 0.8, France 0.8, Italy 0.8, Poland 3.5 % a year) plus two ceilinged policy adjustments, read as trend labour productivity (Q3) and assigned to potential 1:1; `MacroSystem.ApplyPotentialGdpGrowthDaily` compounded `EconomyState.PotentialGDP` at that rate every day. **What it ignored:** the labour input - the 20–64 cohort, participation and the natural rate entered potential nowhere, so a country whose working-age population halved kept its potential output and, since P5-B3, lost its tax base against it. **After P5-B7 (`PotentialOutput`):** potential is its factors - the seed's potential × the labour input's ratio to the seed × a productivity index compounding at the ledger's trend, the trend re-seeded from the sourced series (Eurostat nama_10_lp_ulc, BLS PRS85006092; USA 1.613, Sweden 1.019, Germany 0.938, France 0.513, Italy 0.119, Poland 3.019) - and `Country.PotentialGrowthRate` is derived from them once a turn. The table below is whichever tree ran it; the two runs are kept in `COMPLETED.md` §322 side by side.

**Labour input at the natural rate** = the 20–64 cohort (`SpendingDrivers.Level`, WorkingAge20To64) × `EconomyState.LaborForceParticipationRate` / 100 × (1 − `Country.NaturalUnemploymentRate` / 100). **Labour × productivity** = that input times `EconomyState.Productivity` (the stat, which compounds at the ledger's trend plus the hoarding cycle), both against their seeds - what potential would read if it were built from its factors.

## USA - base trend 1.613 % a year; seed potential 29151.83, GDP 29000, labour input 116.584 M employed, productivity 90.83

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | -2.612 | 0.974 | 0.927 | 1.002 | 61.764 | 0.958 | 1.014 | 0.972 | 141.488 |
| 10 | 1.762 | 1.194 | 1.022 | 1.021 | 61.259 | 1.017 | 1.13 | 1.15 | 160.112 |
| 20 | 1.634 | 1.43 | 1.216 | 1.044 | 61.054 | 1.038 | 1.344 | 1.396 | 193.078 |
| 30 | 1.581 | 1.668 | 1.379 | 1.046 | 60.486 | 1.032 | 1.596 | 1.647 | 245.56 |
| 40 | 0.934 | 1.919 | 1.56 | 1.036 | 59.677 | 1.012 | 1.922 | 1.944 | 313.225 |
| 50 | 1.726 | 2.171 | 1.761 | 1.017 | 58.537 | 0.975 | 2.31 | 2.251 | 405.822 |
| 60 | 1.175 | 2.464 | 2.026 | 1.006 | 57.685 | 0.943 | 2.762 | 2.604 | 519.831 |
| 70 | 1.449 | 2.859 | 2.334 | 0.999 | 57.293 | 0.932 | 3.269 | 3.047 | 660.003 |
| 80 | 1.855 | 3.325 | 2.672 | 0.995 | 57.032 | 0.924 | 3.859 | 3.564 | 845.483 |
| 90 | 2.339 | 3.922 | 3.16 | 0.996 | 57.048 | 0.928 | 4.59 | 4.26 | 1049.675 |
| 100 | 2 | 4.617 | 3.727 | 0.998 | 57.101 | 0.931 | 5.454 | 5.078 | 1297.365 |

## Sweden - base trend 1.019 % a year; seed potential 630.109, GDP 620, labour input 3.618 M employed, productivity 89.95

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 2.873 | 1.029 | 0.992 | 1.006 | 65.274 | 1.018 | 1.004 | 1.023 | 35.835 |
| 10 | 0.992 | 1.171 | 1.146 | 1.05 | 64.797 | 1.058 | 1.11 | 1.175 | 38.562 |
| 20 | 1.45 | 1.345 | 1.298 | 1.096 | 64.108 | 1.098 | 1.237 | 1.357 | 46.707 |
| 30 | 0.453 | 1.483 | 1.456 | 1.108 | 63.19 | 1.093 | 1.388 | 1.517 | 59.152 |
| 40 | 1.031 | 1.629 | 1.579 | 1.122 | 61.68 | 1.084 | 1.574 | 1.706 | 72.483 |
| 50 | 0.681 | 1.779 | 1.748 | 1.129 | 60.452 | 1.069 | 1.783 | 1.906 | 93.241 |
| 60 | 0.689 | 1.905 | 1.874 | 1.115 | 59.179 | 1.033 | 2.032 | 2.099 | 123.814 |
| 70 | 0.602 | 2.06 | 2.026 | 1.109 | 58.118 | 1.008 | 2.322 | 2.341 | 158.862 |
| 80 | 1.046 | 2.24 | 2.177 | 1.107 | 57.299 | 0.989 | 2.624 | 2.595 | 209.136 |
| 90 | 0.942 | 2.469 | 2.348 | 1.107 | 57.087 | 0.984 | 2.942 | 2.895 | 246.261 |
| 100 | 1.638 | 2.735 | 2.662 | 1.107 | 57.023 | 0.984 | 3.29 | 3.238 | 298.129 |

## Germany - base trend 0.938 % a year; seed potential 4709.741, GDP 4700, labour input 28.549 M employed, productivity 94.54

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.023 | 1 | 1.003 | 0.993 | 60.776 | 0.991 | 1.009 | 0.999 | 65.167 |
| 10 | 0.068 | 1.006 | 1.003 | 0.929 | 59.25 | 0.917 | 1.114 | 1.022 | 73.597 |
| 20 | 0.67 | 1.076 | 1.039 | 0.936 | 57.599 | 0.893 | 1.256 | 1.122 | 92.219 |
| 30 | 0.462 | 1.14 | 1.104 | 0.914 | 56.937 | 0.861 | 1.401 | 1.207 | 107.609 |
| 40 | 0.406 | 1.212 | 1.12 | 0.893 | 56.223 | 0.834 | 1.557 | 1.298 | 124.449 |
| 50 | 0.848 | 1.307 | 1.217 | 0.892 | 55.506 | 0.818 | 1.754 | 1.435 | 154.138 |
| 60 | 0.7 | 1.398 | 1.296 | 0.878 | 54.914 | 0.797 | 1.953 | 1.556 | 189.186 |
| 70 | 0.342 | 1.485 | 1.379 | 0.86 | 54.179 | 0.771 | 2.19 | 1.688 | 220.583 |
| 80 | 1.569 | 1.613 | 1.449 | 0.855 | 53.512 | 0.765 | 2.462 | 1.883 | 265.089 |
| 90 | 0.824 | 1.727 | 1.569 | 0.855 | 53.193 | 0.751 | 2.719 | 2.041 | 322.973 |
| 100 | 0.793 | 1.875 | 1.712 | 0.855 | 53.124 | 0.749 | 2.973 | 2.227 | 387.872 |

## France - base trend 0.513 % a year; seed potential 3193.096, GDP 3200, labour input 19.623 M employed, productivity 86.32

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.974 | 1.01 | 1.015 | 1 | 55.65 | 1.005 | 1.006 | 1.011 | 119.999 |
| 10 | 0.205 | 1.027 | 1.002 | 0.992 | 54.628 | 0.977 | 1.064 | 1.039 | 152.594 |
| 20 | 0.223 | 1.029 | 0.991 | 0.972 | 53.127 | 0.93 | 1.138 | 1.058 | 181.501 |
| 30 | 0.502 | 1.039 | 0.959 | 0.955 | 51.892 | 0.893 | 1.222 | 1.091 | 202.406 |
| 40 | -0.018 | 1.07 | 0.961 | 0.947 | 51.073 | 0.875 | 1.299 | 1.136 | 221.546 |
| 50 | -0.117 | 1.063 | 0.956 | 0.917 | 50.042 | 0.827 | 1.388 | 1.148 | 268.236 |
| 60 | 0.377 | 1.073 | 0.942 | 0.898 | 49.018 | 0.795 | 1.49 | 1.184 | 307.309 |
| 70 | 0.271 | 1.098 | 0.969 | 0.888 | 48.399 | 0.774 | 1.601 | 1.239 | 292.156 |
| 80 | 0.536 | 1.132 | 0.999 | 0.881 | 47.935 | 0.758 | 1.695 | 1.286 | 335.362 |
| 90 | 0.44 | 1.188 | 1.063 | 0.881 | 47.85 | 0.756 | 1.797 | 1.36 | 349.161 |
| 100 | 0.493 | 1.248 | 1.107 | 0.88 | 47.803 | 0.755 | 1.892 | 1.429 | 363.37 |

## Italy - base trend 0.119 % a year; seed potential 2295.011, GDP 2300, labour input 15.413 M employed, productivity 78.2

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | -0.926 | 0.991 | 1.002 | 0.996 | 48.77 | 0.99 | 1.002 | 0.992 | 141.876 |
| 10 | -0.855 | 0.93 | 0.898 | 0.94 | 47.31 | 0.92 | 1.018 | 0.936 | 176.678 |
| 20 | -0.022 | 0.841 | 0.767 | 0.872 | 45.172 | 0.823 | 1.074 | 0.884 | 218.219 |
| 30 | -0.413 | 0.779 | 0.707 | 0.84 | 43.66 | 0.755 | 1.128 | 0.852 | 266.87 |
| 40 | -0.894 | 0.747 | 0.679 | 0.814 | 43.071 | 0.718 | 1.154 | 0.828 | 311.6 |
| 50 | -0.886 | 0.7 | 0.62 | 0.767 | 42.456 | 0.669 | 1.17 | 0.783 | 373.545 |
| 60 | -0.121 | 0.656 | 0.565 | 0.733 | 41.372 | 0.623 | 1.194 | 0.743 | 448.43 |
| 70 | -0.255 | 0.637 | 0.548 | 0.722 | 40.718 | 0.602 | 1.215 | 0.732 | 395.937 |
| 80 | 0.315 | 0.631 | 0.544 | 0.715 | 40.546 | 0.595 | 1.229 | 0.731 | 375.23 |
| 90 | -0.317 | 0.63 | 0.548 | 0.715 | 40.476 | 0.591 | 1.243 | 0.734 | 377.701 |
| 100 | -0.086 | 0.634 | 0.554 | 0.714 | 40.441 | 0.592 | 1.253 | 0.742 | 379.39 |

## Poland - base trend 3.019 % a year; seed potential 843.552, GDP 840, labour input 11.527 M employed, productivity 54.09

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.113 | 1.001 | 0.986 | 0.992 | 57.138 | 0.972 | 1.029 | 1 | 66.967 |
| 10 | 2.268 | 1.264 | 1.14 | 0.951 | 55.891 | 0.939 | 1.324 | 1.243 | 68.823 |
| 20 | 2.063 | 1.509 | 1.343 | 0.87 | 53.506 | 0.832 | 1.847 | 1.538 | 84.264 |
| 30 | 1.304 | 1.672 | 1.482 | 0.765 | 50.145 | 0.685 | 2.646 | 1.811 | 112.344 |
| 40 | 2.168 | 2.011 | 1.728 | 0.724 | 47.661 | 0.611 | 3.758 | 2.298 | 144.493 |
| 50 | 2.28 | 2.572 | 2.208 | 0.7 | 47.035 | 0.58 | 5.175 | 3.004 | 170.287 |
| 60 | 2.325 | 3.272 | 2.786 | 0.665 | 46.913 | 0.548 | 7.062 | 3.872 | 197.684 |
| 70 | 2.996 | 4.324 | 3.637 | 0.656 | 46.684 | 0.538 | 9.513 | 5.119 | 227.96 |
| 80 | 3.303 | 5.86 | 4.872 | 0.658 | 46.753 | 0.542 | 12.935 | 7.016 | 257.381 |
| 90 | 2.803 | 7.852 | 6.443 | 0.658 | 46.784 | 0.541 | 17.518 | 9.483 | 271.163 |
| 100 | 3.224 | 10.562 | 8.796 | 0.659 | 46.771 | 0.543 | 23.739 | 12.896 | 290.22 |

**Reading it:** where `potential ÷ seed` runs ahead of `labour × productivity ÷ seed`, potential is carrying output that no worker produces; the debt column is the fiscal book paying for the difference since P5-B3 put the tax bases on the wage bill.
