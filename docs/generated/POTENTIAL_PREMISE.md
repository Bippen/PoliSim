# The premise of potential output - measured, no player, 100 turns (P5-B7; re-measured after FT-7's second seat, §394 - the labour input is employment, the seed potentials re-solved; re-measured after PN-3, §522 - the labour force is the pyramid's, the 15+ population at the state's rate, not the 20–64 window)

No player is set, so every country is AI-governed and the AI finance ministry is live. The last column is `EconomyState.DebtToGdpRatio`: the nominal debt over nominal GDP.

**What set potential growth before P5-B7 (`MacroSystem.ApplySectorGrowthEffect`, measured 2026-09-05 as `potential01`):** `Country.PotentialGrowthRate` = clamp(`Country.BasePotentialGrowthRate` + the infrastructure adjustment + the sector adjustment, 0, 8) - a seeded trend (USA 2.0, Sweden 1.5, Germany 0.8, France 0.8, Italy 0.8, Poland 3.5 % a year) plus two ceilinged policy adjustments, read as trend labour productivity (Q3) and assigned to potential 1:1; `MacroSystem.ApplyPotentialGdpGrowthDaily` compounded `EconomyState.PotentialGDP` at that rate every day. **What it ignored:** the labour input - the 20–64 cohort, participation and the natural rate entered potential nowhere, so a country whose working-age population halved kept its potential output and, since P5-B3, lost its tax base against it. **After P5-B7 (`PotentialOutput`):** potential is its factors - the seed's potential × the labour input's ratio to the seed × a productivity index compounding at the ledger's trend, the trend re-seeded from the sourced series (Eurostat nama_10_lp_ulc, BLS PRS85006092; USA 1.613, Sweden 1.019, Germany 0.938, France 0.513, Italy 0.119, Poland 3.019) - and `Country.PotentialGrowthRate` is derived from them once a turn. The table below is whichever tree ran it; the two runs are kept in `COMPLETED.md` §322 side by side.

**Labour input** = the labour force × (1 − `EconomyState.Unemployment` / 100) (`PotentialOutput.LabourInput`, employment since §394), where the labour force is the population aged 15 and over × `EconomyState.LaborForceParticipationRate` / 100 (`PotentialOutput.LabourForce`, since PN-3 §522 - the labour force the pyramid implies at the sourced rates by age, scaled by the levers' deviation; before §522 it was the 20–64 cohort × the same 15+ rate, a count on one base and a rate on another). **Labour × productivity** = that input times `EconomyState.Productivity` (the stat, which compounds at the ledger's trend plus the hoarding cycle), both against their seeds - what potential would read if it were built from its factors.

## USA - base trend 1.613 % a year; seed potential 29151.83, GDP 29000, labour input 167.917 M employed, productivity 90.83

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | -0.426 | 0.996 | 0.964 | 1.002 | 61.846 | 0.98 | 1.014 | 0.994 | 132.761 |
| 10 | 1.868 | 1.224 | 1.102 | 1.021 | 61.228 | 1.043 | 1.132 | 1.181 | 131.426 |
| 20 | 1.548 | 1.462 | 1.288 | 1.044 | 60.765 | 1.061 | 1.322 | 1.403 | 126.773 |
| 30 | 1.27 | 1.722 | 1.517 | 1.046 | 60.043 | 1.065 | 1.55 | 1.651 | 125.672 |
| 40 | 2.226 | 2.026 | 1.734 | 1.036 | 58.925 | 1.068 | 1.803 | 1.925 | 124.264 |
| 50 | 0.769 | 2.343 | 1.992 | 1.017 | 57.642 | 1.052 | 2.111 | 2.221 | 121.836 |
| 60 | 1.812 | 2.737 | 2.333 | 1.006 | 56.691 | 1.047 | 2.493 | 2.61 | 120.26 |
| 70 | 1.255 | 3.156 | 2.682 | 0.998 | 56.107 | 1.029 | 2.944 | 3.028 | 120.416 |
| 80 | 1.268 | 3.664 | 3.109 | 0.993 | 55.674 | 1.018 | 3.474 | 3.535 | 119.972 |
| 90 | 0.636 | 4.266 | 3.615 | 0.995 | 55.436 | 1.01 | 4.097 | 4.137 | 118.894 |
| 100 | 0.836 | 5.013 | 4.245 | 0.996 | 55.244 | 1.011 | 4.818 | 4.87 | 116.633 |

## Sweden - base trend 1.019 % a year; seed potential 630.109, GDP 620, labour input 5.338 M employed, productivity 89.95

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 4.248 | 1.042 | 1.038 | 1.006 | 65.309 | 1.032 | 1.004 | 1.036 | 32.962 |
| 10 | 1.675 | 1.192 | 1.219 | 1.05 | 64.611 | 1.077 | 1.098 | 1.183 | 28.192 |
| 20 | 1.206 | 1.363 | 1.386 | 1.096 | 63.69 | 1.113 | 1.219 | 1.356 | 26.922 |
| 30 | 1.519 | 1.542 | 1.572 | 1.108 | 62.387 | 1.137 | 1.348 | 1.532 | 25.886 |
| 40 | 1.141 | 1.722 | 1.755 | 1.122 | 60.494 | 1.146 | 1.495 | 1.714 | 23.406 |
| 50 | 0.956 | 1.905 | 1.95 | 1.129 | 58.954 | 1.145 | 1.669 | 1.91 | 23.121 |
| 60 | 0.84 | 2.082 | 2.15 | 1.115 | 57.335 | 1.129 | 1.861 | 2.102 | 23.31 |
| 70 | 1.016 | 2.273 | 2.349 | 1.108 | 55.919 | 1.113 | 2.078 | 2.313 | 23.479 |
| 80 | 0.506 | 2.475 | 2.579 | 1.106 | 54.895 | 1.094 | 2.333 | 2.552 | 23.187 |
| 90 | 1.088 | 2.719 | 2.818 | 1.106 | 54.383 | 1.084 | 2.609 | 2.828 | 22.259 |
| 100 | -0.17 | 2.97 | 3.108 | 1.106 | 54.043 | 1.069 | 2.918 | 3.118 | 22.06 |

## Germany - base trend 0.938 % a year; seed potential 4709.741, GDP 4700, labour input 42.192 M employed, productivity 94.54

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.26 | 1.003 | 1.004 | 0.993 | 60.812 | 0.993 | 1.009 | 1.002 | 63.239 |
| 10 | 1.043 | 1.071 | 1.04 | 0.929 | 59.008 | 0.977 | 1.062 | 1.037 | 54.357 |
| 20 | 0.731 | 1.145 | 1.109 | 0.936 | 57.336 | 0.952 | 1.181 | 1.124 | 49.761 |
| 30 | 0.219 | 1.22 | 1.171 | 0.913 | 56.475 | 0.924 | 1.304 | 1.205 | 47.333 |
| 40 | 1.283 | 1.314 | 1.233 | 0.892 | 55.485 | 0.906 | 1.428 | 1.294 | 45.732 |
| 50 | 0.783 | 1.422 | 1.335 | 0.891 | 54.569 | 0.893 | 1.576 | 1.408 | 41.027 |
| 60 | 0.813 | 1.534 | 1.433 | 0.877 | 53.79 | 0.878 | 1.735 | 1.523 | 39.837 |
| 70 | 0.778 | 1.657 | 1.54 | 0.858 | 52.747 | 0.863 | 1.901 | 1.64 | 37.823 |
| 80 | -0.358 | 1.774 | 1.622 | 0.854 | 51.83 | 0.842 | 2.098 | 1.767 | 35.071 |
| 90 | 0.851 | 1.943 | 1.785 | 0.854 | 51.455 | 0.842 | 2.318 | 1.951 | 33.989 |
| 100 | 0.86 | 2.113 | 1.939 | 0.854 | 51.202 | 0.838 | 2.545 | 2.132 | 33.038 |

## France - base trend 0.513 % a year; seed potential 3193.096, GDP 3200, labour input 29.719 M employed, productivity 86.32

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.952 | 1.01 | 1.017 | 1 | 55.651 | 1.004 | 1.006 | 1.01 | 116.174 |
| 10 | -0.008 | 1.057 | 1.031 | 0.992 | 54.797 | 1.006 | 1.032 | 1.038 | 113.688 |
| 20 | -0.242 | 1.093 | 1.042 | 0.972 | 53.254 | 0.989 | 1.077 | 1.065 | 105.836 |
| 30 | -0.505 | 1.121 | 1.028 | 0.954 | 51.908 | 0.964 | 1.128 | 1.088 | 94.305 |
| 40 | 0.517 | 1.16 | 1.022 | 0.947 | 51.023 | 0.95 | 1.18 | 1.12 | 85.178 |
| 50 | 0.294 | 1.185 | 1.041 | 0.917 | 49.918 | 0.923 | 1.235 | 1.141 | 78.115 |
| 60 | -0.007 | 1.213 | 1.064 | 0.898 | 48.804 | 0.9 | 1.303 | 1.173 | 60.021 |
| 70 | 0.241 | 1.249 | 1.104 | 0.887 | 48.14 | 0.882 | 1.382 | 1.218 | 49.209 |
| 80 | 0.272 | 1.296 | 1.149 | 0.88 | 47.672 | 0.87 | 1.463 | 1.272 | 38.791 |
| 90 | 0.533 | 1.36 | 1.206 | 0.88 | 47.482 | 0.867 | 1.546 | 1.34 | 29.987 |
| 100 | 0.506 | 1.427 | 1.269 | 0.88 | 47.363 | 0.863 | 1.634 | 1.411 | 23.985 |

## Italy - base trend 0.119 % a year; seed potential 2295.011, GDP 2300, labour input 23.378 M employed, productivity 78.2

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | -1.334 | 0.987 | 0.986 | 0.996 | 48.777 | 0.985 | 1.002 | 0.987 | 140.064 |
| 10 | -0.296 | 0.976 | 0.939 | 0.94 | 47.05 | 0.966 | 0.977 | 0.944 | 133.773 |
| 20 | -0.907 | 0.919 | 0.854 | 0.871 | 44.747 | 0.902 | 0.98 | 0.884 | 132.907 |
| 30 | -0.591 | 0.879 | 0.816 | 0.84 | 43.403 | 0.856 | 1.001 | 0.856 | 120.635 |
| 40 | 0.214 | 0.841 | 0.765 | 0.814 | 42.902 | 0.812 | 1.017 | 0.826 | 114.695 |
| 50 | -0.437 | 0.801 | 0.722 | 0.766 | 42.231 | 0.769 | 1.019 | 0.784 | 107.594 |
| 60 | -1.022 | 0.768 | 0.691 | 0.732 | 41.14 | 0.734 | 1.024 | 0.751 | 97.052 |
| 70 | -0.234 | 0.745 | 0.662 | 0.72 | 40.534 | 0.709 | 1.037 | 0.736 | 64.876 |
| 80 | -0.293 | 0.733 | 0.656 | 0.714 | 40.4 | 0.695 | 1.054 | 0.732 | 53.68 |
| 90 | 0.683 | 0.742 | 0.666 | 0.714 | 40.358 | 0.699 | 1.062 | 0.742 | 43.415 |
| 100 | 0.254 | 0.743 | 0.666 | 0.713 | 40.316 | 0.696 | 1.069 | 0.744 | 38.754 |

## Poland - base trend 3.019 % a year; seed potential 843.552, GDP 840, labour input 16.789 M employed, productivity 54.09

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.343 | 1.003 | 0.99 | 0.992 | 57.145 | 0.974 | 1.029 | 1.002 | 65.378 |
| 10 | 2.319 | 1.275 | 1.203 | 0.951 | 55.521 | 0.947 | 1.301 | 1.232 | 50.966 |
| 20 | 0.607 | 1.554 | 1.446 | 0.87 | 52.376 | 0.857 | 1.733 | 1.486 | 43.605 |
| 30 | 1.604 | 1.884 | 1.741 | 0.765 | 48.103 | 0.772 | 2.337 | 1.804 | 37.036 |
| 40 | 2.6 | 2.313 | 2.086 | 0.724 | 44.93 | 0.703 | 3.252 | 2.287 | 32.612 |
| 50 | 2.162 | 2.873 | 2.561 | 0.7 | 43.765 | 0.648 | 4.522 | 2.932 | 28.221 |
| 60 | 2.848 | 3.661 | 3.23 | 0.665 | 43.099 | 0.613 | 6.162 | 3.78 | 26.955 |
| 70 | 2.96 | 4.807 | 4.221 | 0.657 | 42.537 | 0.598 | 8.373 | 5.005 | 18.636 |
| 80 | 2.241 | 6.407 | 5.628 | 0.659 | 42.269 | 0.591 | 11.354 | 6.715 | 11.596 |
| 90 | 3.135 | 8.63 | 7.584 | 0.659 | 42.095 | 0.592 | 15.358 | 9.087 | 11.772 |
| 100 | 2.713 | 11.527 | 10.13 | 0.66 | 41.932 | 0.588 | 20.735 | 12.187 | 9.365 |

**Reading it:** where `potential ÷ seed` runs ahead of `labour × productivity ÷ seed`, potential is carrying output that no worker produces; since P5-B3 put the tax bases on the wage bill, that gap is revenue the book does not collect, and the debt column is where it would show - read as the ratio the game itself reports, never as a stock over real output (FT-13, §584).
