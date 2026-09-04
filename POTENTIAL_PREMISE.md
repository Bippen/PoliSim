# The premise of potential output - measured, no player, 100 turns (P5-B7)

**What set potential growth before P5-B7 (`MacroSystem.ApplySectorGrowthEffect`, measured 2026-09-05 as `potential01`):** `Country.PotentialGrowthRate` = clamp(`Country.BasePotentialGrowthRate` + the infrastructure adjustment + the sector adjustment, 0, 8) - a seeded trend (USA 2.0, Sweden 1.5, Germany 0.8, France 0.8, Italy 0.8, Poland 3.5 % a year) plus two ceilinged policy adjustments, read as trend labour productivity (Q3) and assigned to potential 1:1; `MacroSystem.ApplyPotentialGdpGrowthDaily` compounded `EconomyState.PotentialGDP` at that rate every day. **What it ignored:** the labour input - the 20–64 cohort, participation and the natural rate entered potential nowhere, so a country whose working-age population halved kept its potential output and, since P5-B3, lost its tax base against it. **After P5-B7 (`PotentialOutput`):** potential is its factors - the seed's potential × the labour input's ratio to the seed × a productivity index compounding at the ledger's trend, the trend re-seeded from the sourced series (Eurostat nama_10_lp_ulc, BLS PRS85006092; USA 1.613, Sweden 1.019, Germany 0.938, France 0.513, Italy 0.119, Poland 3.019) - and `Country.PotentialGrowthRate` is derived from them once a turn. The table below is whichever tree ran it; the two runs are kept in `COMPLETED.md` §322 side by side.

**Labour input at the natural rate** = the 20–64 cohort (`SpendingDrivers.Level`, WorkingAge20To64) × `EconomyState.LaborForceParticipationRate` / 100 × (1 − `Country.NaturalUnemploymentRate` / 100). **Labour × productivity** = that input times `EconomyState.Productivity` (the stat, which compounds at the ledger's trend plus the hoarding cycle), both against their seeds - what potential would read if it were built from its factors.

## USA - base trend 1.613 % a year; seed potential 33260, GDP 29000, labour input 117.194 M at the natural rate, productivity 90.83

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 1.722 | 1.017 | 0.998 | 1.002 | 61.859 | 1.001 | 1.014 | 1.015 | 127.676 |
| 10 | 1.705 | 1.188 | 1.148 | 1.021 | 61.336 | 1.012 | 1.159 | 1.173 | 133.153 |
| 20 | 1.628 | 1.418 | 1.371 | 1.044 | 61.041 | 1.029 | 1.377 | 1.417 | 131.474 |
| 30 | 1.369 | 1.648 | 1.572 | 1.045 | 60.388 | 1.02 | 1.627 | 1.659 | 136.152 |
| 40 | 1.145 | 1.885 | 1.776 | 1.034 | 59.489 | 0.993 | 1.94 | 1.927 | 136.81 |
| 50 | 1.247 | 2.123 | 2.03 | 1.013 | 58.274 | 0.954 | 2.302 | 2.195 | 136.703 |
| 60 | 1.462 | 2.423 | 2.333 | 1 | 57.363 | 0.927 | 2.711 | 2.513 | 135.821 |
| 70 | 1.428 | 2.797 | 2.677 | 0.991 | 56.963 | 0.912 | 3.181 | 2.901 | 136.269 |
| 80 | 1.596 | 3.248 | 3.063 | 0.985 | 56.681 | 0.902 | 3.735 | 3.369 | 137.993 |
| 90 | 1.609 | 3.812 | 3.61 | 0.985 | 56.683 | 0.902 | 4.437 | 4.002 | 133.382 |
| 100 | 1.626 | 4.476 | 4.221 | 0.985 | 56.706 | 0.903 | 5.273 | 4.759 | 129.828 |

## Sweden - base trend 1.019 % a year; seed potential 614.25, GDP 620, labour input 3.677 M at the natural rate, productivity 89.95

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 1.541 | 1.015 | 0.98 | 1.006 | 65.258 | 1.005 | 1.004 | 1.009 | 36.297 |
| 10 | 1.418 | 1.152 | 1.105 | 1.05 | 64.752 | 1.041 | 1.1 | 1.145 | 33.02 |
| 20 | 1.272 | 1.316 | 1.264 | 1.096 | 64.044 | 1.075 | 1.215 | 1.306 | 34.977 |
| 30 | 0.58 | 1.45 | 1.419 | 1.108 | 63.071 | 1.07 | 1.349 | 1.443 | 36.789 |
| 40 | 1.057 | 1.584 | 1.538 | 1.122 | 61.463 | 1.056 | 1.499 | 1.582 | 34.787 |
| 50 | 0.666 | 1.728 | 1.704 | 1.129 | 60.218 | 1.041 | 1.665 | 1.733 | 32.226 |
| 60 | 0.709 | 1.849 | 1.818 | 1.115 | 58.948 | 1.006 | 1.856 | 1.868 | 32.636 |
| 70 | 0.789 | 1.998 | 1.955 | 1.108 | 57.892 | 0.982 | 2.08 | 2.044 | 31.564 |
| 80 | 0.923 | 2.176 | 2.105 | 1.106 | 57.081 | 0.967 | 2.312 | 2.236 | 34.461 |
| 90 | 0.996 | 2.401 | 2.269 | 1.106 | 56.916 | 0.964 | 2.575 | 2.483 | 32.51 |
| 100 | 1 | 2.655 | 2.576 | 1.106 | 56.866 | 0.963 | 2.871 | 2.765 | 32.418 |

## Germany - base trend 0.938 % a year; seed potential 4700, GDP 4700, labour input 28.608 M at the natural rate, productivity 94.54

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.172 | 1.002 | 1.004 | 0.993 | 60.776 | 0.992 | 1.009 | 1.001 | 63.809 |
| 10 | 0.228 | 0.993 | 0.999 | 0.929 | 59.156 | 0.904 | 1.102 | 0.997 | 66.445 |
| 20 | 0.692 | 1.068 | 1.058 | 0.937 | 57.448 | 0.885 | 1.207 | 1.069 | 67.974 |
| 30 | 0.407 | 1.132 | 1.139 | 0.914 | 56.854 | 0.855 | 1.333 | 1.14 | 66.088 |
| 40 | 0.643 | 1.201 | 1.161 | 0.894 | 56.14 | 0.826 | 1.467 | 1.211 | 67.345 |
| 50 | 0.797 | 1.299 | 1.276 | 0.893 | 55.399 | 0.814 | 1.635 | 1.33 | 68.08 |
| 60 | 0.564 | 1.389 | 1.367 | 0.879 | 54.788 | 0.792 | 1.803 | 1.428 | 70.248 |
| 70 | 0.576 | 1.471 | 1.447 | 0.86 | 54.005 | 0.764 | 1.998 | 1.527 | 67.343 |
| 80 | 0.892 | 1.587 | 1.531 | 0.856 | 53.336 | 0.751 | 2.225 | 1.671 | 64.981 |
| 90 | 0.909 | 1.733 | 1.687 | 0.856 | 53.048 | 0.747 | 2.46 | 1.838 | 66.896 |
| 100 | 0.927 | 1.902 | 1.857 | 0.856 | 52.998 | 0.746 | 2.708 | 2.02 | 66.116 |

## France - base trend 0.513 % a year; seed potential 3200, GDP 3200, labour input 19.581 M at the natural rate, productivity 86.32

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 0.567 | 1.006 | 1.018 | 1 | 55.653 | 1.001 | 1.006 | 1.006 | 116.473 |
| 10 | 0.058 | 1.026 | 1.027 | 0.993 | 54.605 | 0.974 | 1.06 | 1.033 | 126.755 |
| 20 | -0.042 | 1.028 | 1.042 | 0.973 | 53.068 | 0.928 | 1.114 | 1.034 | 130.8 |
| 30 | 0.192 | 1.041 | 1.047 | 0.956 | 51.918 | 0.892 | 1.181 | 1.054 | 130.182 |
| 40 | 0.18 | 1.073 | 1.071 | 0.949 | 51.215 | 0.874 | 1.245 | 1.088 | 127.17 |
| 50 | -0.104 | 1.074 | 1.086 | 0.92 | 50.241 | 0.831 | 1.315 | 1.093 | 131.532 |
| 60 | 0.326 | 1.087 | 1.078 | 0.902 | 49.261 | 0.799 | 1.388 | 1.109 | 132.047 |
| 70 | 0.255 | 1.118 | 1.106 | 0.892 | 48.693 | 0.781 | 1.472 | 1.149 | 131.269 |
| 80 | 0.47 | 1.158 | 1.142 | 0.885 | 48.241 | 0.768 | 1.541 | 1.184 | 131.386 |
| 90 | 0.5 | 1.218 | 1.209 | 0.885 | 48.194 | 0.767 | 1.628 | 1.249 | 129.147 |
| 100 | 0.512 | 1.281 | 1.259 | 0.885 | 48.158 | 0.767 | 1.711 | 1.311 | 131.679 |

## Italy - base trend 0.119 % a year; seed potential 2300, GDP 2300, labour input 15.38 M at the natural rate, productivity 78.2

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | -0.272 | 0.997 | 1.004 | 0.996 | 48.773 | 0.996 | 1.002 | 0.998 | 137.815 |
| 10 | -1.095 | 0.922 | 0.913 | 0.941 | 47.244 | 0.911 | 1.006 | 0.917 | 150.56 |
| 20 | -0.751 | 0.825 | 0.801 | 0.873 | 45.067 | 0.806 | 1.027 | 0.828 | 156.978 |
| 30 | -0.385 | 0.781 | 0.76 | 0.843 | 43.671 | 0.754 | 1.049 | 0.791 | 154.721 |
| 40 | -0.327 | 0.761 | 0.746 | 0.818 | 43.315 | 0.726 | 1.061 | 0.77 | 149.961 |
| 50 | -0.718 | 0.719 | 0.697 | 0.771 | 42.903 | 0.678 | 1.074 | 0.728 | 154.637 |
| 60 | -0.317 | 0.681 | 0.649 | 0.738 | 41.941 | 0.634 | 1.081 | 0.686 | 159.171 |
| 70 | -0.073 | 0.672 | 0.641 | 0.727 | 41.499 | 0.619 | 1.091 | 0.675 | 155.984 |
| 80 | 0.146 | 0.674 | 0.637 | 0.722 | 41.489 | 0.614 | 1.101 | 0.676 | 156.44 |
| 90 | 0.095 | 0.683 | 0.644 | 0.722 | 41.512 | 0.614 | 1.118 | 0.687 | 157.15 |
| 100 | 0.127 | 0.691 | 0.654 | 0.722 | 41.531 | 0.615 | 1.133 | 0.697 | 155.795 |

## Poland - base trend 3.019 % a year; seed potential 840, GDP 840, labour input 11.576 M at the natural rate, productivity 54.09

| turn | potential growth (%) | potential ÷ seed | GDP ÷ seed | 20–64 cohort ÷ seed | participation (%) | labour input ÷ seed | productivity ÷ seed | labour × productivity ÷ seed | debt (% GDP) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 2.105 | 1.021 | 0.984 | 0.992 | 57.136 | 0.991 | 1.029 | 1.019 | 66.287 |
| 10 | 2.155 | 1.251 | 1.161 | 0.952 | 55.838 | 0.929 | 1.313 | 1.22 | 70.322 |
| 20 | 1.171 | 1.471 | 1.391 | 0.871 | 53.291 | 0.811 | 1.782 | 1.445 | 71.782 |
| 30 | 1.146 | 1.624 | 1.545 | 0.765 | 49.711 | 0.665 | 2.422 | 1.611 | 73.396 |
| 40 | 2.503 | 1.962 | 1.82 | 0.724 | 47.166 | 0.597 | 3.264 | 1.947 | 71.493 |
| 50 | 2.367 | 2.525 | 2.347 | 0.698 | 46.688 | 0.57 | 4.413 | 2.516 | 66.753 |
| 60 | 2.516 | 3.22 | 2.964 | 0.662 | 46.622 | 0.54 | 5.998 | 3.238 | 65.764 |
| 70 | 2.991 | 4.251 | 3.882 | 0.653 | 46.356 | 0.529 | 8.049 | 4.26 | 66.388 |
| 80 | 3.019 | 5.735 | 5.196 | 0.654 | 46.391 | 0.53 | 10.969 | 5.817 | 62.948 |
| 90 | 3.003 | 7.717 | 6.863 | 0.654 | 46.354 | 0.53 | 14.908 | 7.9 | 64.189 |
| 100 | 3.023 | 10.39 | 9.367 | 0.654 | 46.343 | 0.53 | 20.261 | 10.734 | 63.593 |

**Reading it:** where `potential ÷ seed` runs ahead of `labour × productivity ÷ seed`, potential is carrying output that no worker produces; the debt column is the fiscal book paying for the difference since P5-B3 put the tax bases on the wage bill.
