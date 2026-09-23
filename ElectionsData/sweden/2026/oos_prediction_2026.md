# The out-of-sample test - the model's own 2026 prediction from the 2022 seed, against the real result (K-1 part 0)

> **Class: DERIVED** - written once by `Assets/Editor/OutOfSample2026Diagnostic.cs` at commit 3f53655, before any part of K-1 refreshed the seed; the diagnostic refuses to run again once the seed moves, and refuses to overwrite this file. Real figures read at run time from `ElectionsData/sweden/2026/valkrets_votes_2026.csv` and `ElectionsData/sweden/2026/valkrets_seats_2026.csv` (SOURCED, Valmyndigheten's final result, Dnr VAL-735-2026, fixed 2026-09-19).

```
GUARD: the live prior equals the 2022 catalog's shares for all eight, and the seeded seats equal the 2022 count through SeatConversion.Sweden - the seed is 2022's.
REAL (Valmyndigheten, fixed 2026-09-19): 6767429 valid votes, 6660230 for the eight parties the model carries, 107199 (1.58 %) for all others - unrepresentable by the model; 349 seats.
  catalog: ElectionsData/sweden/2026/valkrets_votes_2026.csv sha256 72aff6538c86534a0c5b8c3b7a8a557299fc3a29f9ccf70e511d6c0423b6dd77; ElectionsData/sweden/2026/valkrets_seats_2026.csv sha256 f7c52babc7c0fa9ff94720539eda1c25e3ccdcdf1354fbd237322ae2a937d28a

PER PARTY (shares over the eight; seats of 349)
party   real%8   real%raw  model%   dev pp   2022%(no-change) dev pp   spatial% dev pp   loyalty  | seats real  model(game)  model(two-tier)
S        28.47     28.02   30.47   +2.00    30.80          +2.34    30.41   +1.95     93.2   |     99        106          106
SD       17.77     17.48   20.57   +2.81    20.86          +3.10    20.32   +2.55     85.3   |     62         72           72
M        20.17     19.85   18.83   -1.34    19.40          -0.77     9.43  -10.74     96.3   |     70         66           66
V         8.54      8.40    6.85   -1.69     6.86          -1.68     7.28   -1.26     84.4   |     30         24           24
C         7.14      7.03    6.29   -0.86     6.81          -0.33     4.71   -2.43     77.9   |     25         22           22
KD        6.27      6.17    6.62   +0.35     5.42          -0.84    13.58   +7.32     84.5   |     22         23           23
MP        6.22      6.12    5.46   -0.76     5.16          -1.06     7.88   +1.66     86.8   |     22         19           19
L         5.42      5.34    4.90   -0.52     4.68          -0.74     6.38   +0.96     84.0   |     19         17           17

MEAN ABSOLUTE DEVIATION over the eight: the model 1.29 pp · the no-change forecast (2022 shares) 1.36 pp · the bare spatial layer 3.61 pp
SEATS, total absolute error of 349: the model as the game seats 36 · the model through the two-tier procedure 36 · the no-change chamber (2022 seats) 38

ALLOCATOR CONTROL on the REAL 2026 counts: national modified Sainte-Lague 0 seats off the real chamber; the full two-tier procedure 0 seats off; its fixed seats per valkrets, derived from the election-day eligible counts, 4 seats off the decision's own column (the statute has them decided by 30 April from the roll, not derived).

INPUTS (the prediction's own): electorate mu=(3.25,6.25) sigma=3.00 tau=0.50, economic weight 0.15
  S    compatibility 100.00  prior  30.33  loyalty   93.2
  SD   compatibility  87.42  prior  20.54  loyalty   85.3
  M    compatibility  67.68  prior  19.10  loyalty   96.3
  V    compatibility  62.10  prior   6.75  loyalty   84.4
  C    compatibility  53.70  prior   6.71  loyalty   77.9
  KD   compatibility  76.44  prior   5.34  loyalty   84.5
  MP   compatibility  63.75  prior   5.08  loyalty   86.8
  L    compatibility  59.41  prior   4.61  loyalty   84.0

NOT REPRESENTED: the parties outside the eight (their votes above), anything between 2022 and 2026 (the economy, approval, leaders, polls - the electorate does not move with the simulation), turnout, differential regional swing, campaign effects and tactical voting.
```
