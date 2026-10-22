# The US presidential count - model card (the states from the national vote; the national vote; the prediction; the campaign geography; the count)

What the game's US presidential count rests on, measured before the count went live - and, since US-6, the prediction it counts, which
is live (the poll and E1's factors); since US-8 the count itself, on the polling day (*The count - US-8*, at the end). The figures the measurements produce are not typed
here: each is written by the instrument that pins it, into its own marked block - under *The readings* the states from the national vote
(`PoliSim.EditorTools.UsStateSwingCheck.WriteReadings`), under *The readings (US-5)* the national vote itself
(`PoliSim.EditorTools.UsNationalVoteCheck.WriteReadings`), under *The readings (US-6)* the live prediction
(`PoliSim.EditorTools.UsPresidentialFactorCheck.WriteReadings`), under *The readings (US-9)* the campaign geography R-US16 is asked on
(`PoliSim.EditorTools.UsCampaignGeographyCheck.WriteReadings`, an Editor harness - nothing live), under *The readings (US-8)* the count (`PoliSim.EditorTools.UsPresidentialElectionDiagnostic.WriteReadings`) - and each instrument fails the cheap bar and the documents bar when its block no
longer says what it measures. The records are `COMPLETED.md` §786 (the returns by state, `ElectionsData/usa/president_returns.md`), §787
(the states), §788 (the House vote) and §789 (the national vote); US-6's, US-9's and US-8's are their own sections of the record.

## The question - R-US14

How a state's vote follows the national vote, three ways: **(a)** §689's normalised uniform swing from the previous presidential election;
**(b)** proportional swing; **(c)** plain additive swing (Poland's okręg form). The plan's rule (`docs/specs/USA_STAGE_PLAN.md`, R-US14):
code recommends the method calling the most states right on 2016 → 2020 and 2020 → 2024 at the true national shares, (a) on a tie, for one
rule across countries; 2012 → 2016 is reported as the stress case; no demographic layer is added unasked; the misses go into this card.
A tie that leaves (a) out is not the rule's to settle: the instrument fails on it, to be asked.

## The measurement

| part | what it does | basis |
|---|---|---|
| The input | each jurisdiction's previous presidential vote from the generated catalog (`UsPresidentialReturns`) | SOURCED (§786) |
| The parties | the two nominees, and all others pooled per state - each a party with one swing; the pool's share is what (b)'s ratio and (c)'s floor act on | DECLARED |
| The national vote | every method is handed the TRUE national shares of the next election, the catalog's own sums; the measurement asks how each spreads them | SOURCED |
| The weights | a state weighs what it cast in the previous election | DECLARED: no vote of the predicted election enters but its national shares |
| (a) | `RegionalVoteModel.RegionalSharesByUniformSwing`, unchanged: one swing a party, floored at zero, each state renormalised, re-solved until the states give the national shares back | §689 |
| (b) | each party's share scaled by its national ratio, each state renormalised; not re-solved, so its states need not give the national shares back (proportional swing lacks §689's property - `RegionalVoteModel`'s own summary) | - |
| (c) | each party's national change added, floored at zero, neither renormalised nor re-solved: where the floor binds, its states sum above the national shares | Poland's okręg form (`PolishSejmAllocation`) |
| The districts | Maine's and Nebraska's swing with their state, in two-party terms - (a) and (c) add the state's change in its Democratic two-party share, (b) scales each nominee's share by the state's own ratio. A district's previous row stands in for the next election's district of the same number, whatever its lines: between 2020 and 2024 both states' districts changed make-up (Maine's workbooks list towns under another district, Nebraska's books counties), so the 2020 → 2024 district misses carry the map as well as the swing | DECLARED: the district rows carry the two nominees only |
| The count | the predicted winners through the statute's allocator (`ElectoralCollege`), the electors in force from the catalog; held to the record's electors **as appointed** - each slate's, its votes for other persons counted to it (§786 has the votes as cast) | §786 |
| Not modelled | faithless electors; Maine's ranked-choice count | DECLARED |

A miss is the method's margin (Republican less Democratic) less the record's, in points of the state's vote - a district's in points of its
two-party vote. The readings' mean is of the absolute state misses, each jurisdiction (the District of Columbia among them) counted once,
unweighted; the districts are listed but not in the mean. The column "national R − D the states imply" weighs each state's predicted margin by
what it cast in the previous election - (a) gives the true figure back by construction, (b) and (c) need not, so part of their misses is a
national shift rather than the spread. Nothing is fitted.

**(a) and (c).** With every party standing everywhere, (a) is (c) renormalised and re-solved, and a renormalisation never changes which nominee
leads. Where no floor binds in a pair, (a) is (c) exactly; where one binds, (a)'s re-solve moves every state a little. R-US14's count of states
called right is therefore in effect (a) and (c) against (b); the readings say, for each pair, how far (a) and (c) part and whether any call
moves.

## The readings

<!-- GENERATED by PoliSim.EditorTools.UsStateSwingCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: eabc1d3e036e5cfbb4623db1707359162d821e617d52d742e59298b254c518ee -->
| from → to | method | electors R – D | record (as appointed) | states called wrong | districts called wrong | national R − D the states imply (pp) | mean absolute margin miss (pp) |
|---|---|---:|---:|---|---|---:|---:|
| 2012 → 2016 | (a) normalised uniform | 235 – 303 | 306 – 232 | 5: IA, MI, OH, PA, WI | ME-2 | -2.10 (true -2.10) | 5.43 |
| 2012 → 2016 | (b) proportional | 235 – 303 | 306 – 232 | 5: IA, MI, OH, PA, WI | ME-2 | -2.09 (true -2.10) | 5.64 |
| 2012 → 2016 | (c) plain additive | 235 – 303 | 306 – 232 | 5: IA, MI, OH, PA, WI | ME-2 | -2.10 (true -2.10) | 5.43 |
| 2016 → 2020 | (a) normalised uniform | 230 – 308 | 232 – 306 | 3: AZ, FL, GA | none | -4.46 (true -4.46) | 2.27 |
| 2016 → 2020 | (b) proportional | 231 – 307 | 232 – 306 | 3: AZ, FL, GA | NE-2 | -4.40 (true -4.46) | 2.57 |
| 2016 → 2020 | (c) plain additive | 230 – 308 | 232 – 306 | 3: AZ, FL, GA | none | -4.46 (true -4.46) | 2.26 |
| 2020 → 2024 | (a) normalised uniform | 312 – 226 | 312 – 226 | none | none | +1.47 (true +1.47) | 2.46 |
| 2020 → 2024 | (b) proportional | 312 – 226 | 312 – 226 | none | none | +1.28 (true +1.47) | 2.28 |
| 2020 → 2024 | (c) plain additive | 312 – 226 | 312 – 226 | none | none | +1.47 (true +1.47) | 2.46 |

States called right on 2016 → 2020 and 2020 → 2024 together: (a) normalised uniform 99; (b) proportional 99; (c) plain additive 99. **The plan's rule recommends (a) normalised uniform** - a tie, which the rule gives to (a).

Where (a) and (c) part - the largest difference between their margins in any jurisdiction, the calls (states and districts) they make differently, and the jurisdictions where each floors a party at zero:

| from → to | largest (a) − (c) margin difference (pp) | calls apart | (a) floors | (c) floors |
|---|---:|---:|---|---|
| 2012 → 2016 | 0.00 | 0 | none | none |
| 2016 → 2020 | 0.31 | 0 | AL, FL, GA, LA, MS, NJ, NC | AL, FL, GA, LA, MS, NJ |
| 2020 → 2024 | 0.00 | 0 | none | none |

**2012 → 2016** - the record's margin and each method's miss (R − D, pp; ✗ the winner called wrong):

| jurisdiction | record | (a) | (b) | (c) |
|---|---:|---:|---:|---:|
| AL | +27.73 | -3.78 | -4.56 | -3.78 |
| AK | +14.73 | +1.02 | -0.66 | +1.02 |
| AZ | +3.55 | +7.28 | +6.73 | +7.28 |
| AR | +26.92 | -1.47 | -3.15 | -1.47 |
| CA | -30.11 | +8.75 | +9.88 | +8.75 |
| CO | -4.91 | +1.30 | +1.40 | +1.30 |
| CT | -13.64 | -1.93 | -1.60 | -1.93 |
| DE | -11.37 | -5.49 | -5.06 | -5.49 |
| DC | -86.78 | +4.90 | +7.20 | +4.90 |
| FL | +1.20 | -0.31 | -0.40 | -0.31 |
| GA | +5.13 | +4.45 | +4.08 | +4.45 |
| HI | -32.18 | -8.76 | -7.57 | -8.76 |
| ID | +31.77 | +1.90 | -0.55 | +1.90 |
| IL | -17.06 | +1.95 | +2.42 | +1.95 |
| IN | +19.17 | -7.21 | -7.86 | -7.21 |
| IA | +9.41 | -13.46 ✗ | -13.37 ✗ | -13.46 ✗ |
| KS | +20.60 | +2.88 | +1.46 | +2.88 |
| KY | +29.84 | -5.38 | -6.53 | -5.38 |
| LA | +19.64 | -0.67 | -1.54 | -0.67 |
| ME | -2.96 | -10.57 | -9.84 | -10.57 |
| MD | -26.42 | +2.10 | +3.12 | +2.10 |
| MA | -27.20 | +5.82 | +6.57 | +5.82 |
| MI | +0.22 | -7.96 ✗ | -7.85 ✗ | -7.96 ✗ |
| MN | -1.52 | -4.41 | -4.19 | -4.41 |
| MS | +17.83 | -4.57 | -4.96 | -4.57 |
| MO | +18.64 | -7.50 | -8.08 | -7.50 |
| MT | +20.42 | -5.01 | -6.18 | -5.01 |
| NE | +25.05 | -1.51 | -2.87 | -1.51 |
| NV | -2.42 | -2.50 | -2.37 | -2.50 |
| NH | -0.37 | -3.45 | -3.39 | -3.45 |
| NJ | -14.10 | -1.92 | -1.65 | -1.92 |
| NM | -8.21 | -0.18 | +0.47 | -0.18 |
| NY | -22.49 | -3.93 | -3.20 | -3.93 |
| NC | +3.66 | +0.15 | -0.04 | +0.15 |
| ND | +35.73 | -14.34 | -15.96 | -14.34 |
| OH | +8.13 | -9.35 ✗ | -9.38 ✗ | -9.35 ✗ |
| OK | +36.39 | -1.09 | -1.34 | -1.09 |
| OR | -10.98 | +0.65 | +1.36 | +0.65 |
| PA | +0.72 | -4.34 ✗ | -4.30 ✗ | -4.34 ✗ |
| RI | -15.51 | -10.19 | -9.16 | -10.19 |
| SC | +14.27 | -2.03 | -2.52 | -2.03 |
| SD | +29.79 | -10.01 | -11.18 | -10.01 |
| TN | +26.01 | -3.84 | -4.75 | -3.84 |
| TX | +9.00 | +8.54 | +7.82 | +8.54 |
| UT | +18.08 | +31.72 | +28.44 | +31.72 |
| VT | -26.41 | -7.43 | -5.79 | -7.43 |
| VA | -5.32 | +3.21 | +3.20 | +3.21 |
| WA | -15.71 | +2.60 | +3.24 | +2.60 |
| WV | +42.07 | -13.56 | -15.20 | -13.56 |
| WI | +0.76 | -5.95 ✗ | -5.87 ✗ | -5.95 ✗ |
| WY | +46.30 | -3.71 | -7.46 | -3.71 |
| ME-1 (two-party) | -15.90 | -4.77 | -4.36 | -4.77 |
| ME-2 (two-party) | +11.15 | -18.74 ✗ | -18.26 ✗ | -18.74 ✗ |
| NE-1 (two-party) | +22.61 | -2.89 | -4.07 | -2.89 |
| NE-2 (two-party) | +2.43 | +7.65 | +6.52 | +7.65 |
| NE-3 (two-party) | +57.85 | -11.77 | -13.22 | -11.77 |

**2016 → 2020** - the record's margin and each method's miss (R − D, pp; ✗ the winner called wrong):

| jurisdiction | record | (a) | (b) | (c) |
|---|---:|---:|---:|---:|
| AL | +25.46 | -0.21 | +0.85 | -0.09 |
| AK | +10.06 | +2.31 | +3.85 | +2.31 |
| AZ | -0.31 | +1.50 ✗ | +1.75 ✗ | +1.50 ✗ |
| AR | +27.62 | -3.06 | -1.69 | -3.06 |
| CA | -29.16 | -3.30 | -4.42 | -3.30 |
| CO | -13.50 | +6.24 | +6.05 | +6.24 |
| CT | -20.07 | +4.08 | +3.77 | +4.08 |
| DE | -18.97 | +5.24 | +4.93 | +5.24 |
| DC | -86.75 | -2.38 | -3.56 | -2.38 |
| FL | +3.36 | -4.50 ✗ | -4.43 ✗ | -4.52 ✗ |
| GA | -0.24 | +3.00 ✗ | +3.21 ✗ | +3.01 ✗ |
| HI | -29.46 | -5.07 | -6.51 | -5.07 |
| ID | +30.77 | -1.36 | +2.25 | -1.36 |
| IL | -16.99 | -2.43 | -2.93 | -2.43 |
| IN | +16.07 | +0.74 | +1.63 | +0.74 |
| IA | +8.20 | -1.15 | -0.55 | -1.15 |
| KS | +14.64 | +3.61 | +4.90 | +3.61 |
| KY | +25.94 | +1.55 | +2.85 | +1.55 |
| LA | +18.61 | -1.42 | -0.70 | -1.33 |
| ME | -9.07 | +3.76 | +3.70 | +3.75 |
| MD | -33.21 | +4.44 | +3.61 | +4.44 |
| MA | -33.46 | +3.91 | +2.78 | +3.90 |
| MI | -2.78 | +0.65 | +0.74 | +0.65 |
| MN | -7.11 | +3.23 | +3.25 | +3.23 |
| MS | +16.55 | -1.39 | -0.71 | -1.08 |
| MO | +15.39 | +0.89 | +1.72 | +0.89 |
| MT | +16.37 | +1.70 | +3.11 | +1.70 |
| NE | +19.06 | +3.64 | +5.26 | +3.63 |
| NV | -2.39 | -2.38 | -2.40 | -2.38 |
| NH | -7.35 | +4.63 | +4.70 | +4.63 |
| NJ | -15.94 | -0.39 | -0.72 | -0.52 |
| NM | -10.79 | +0.22 | -0.34 | +0.22 |
| NY | -23.13 | -1.72 | -2.23 | -1.72 |
| NC | +1.35 | -0.05 | +0.13 | -0.05 |
| ND | +33.34 | +0.03 | +3.07 | +0.03 |
| OH | +8.03 | -2.26 | -1.89 | -2.26 |
| OK | +33.09 | +0.95 | +2.85 | +0.95 |
| OR | -16.09 | +2.75 | +2.03 | +2.75 |
| PA | -1.16 | -0.48 | -0.38 | -0.48 |
| RI | -20.77 | +2.91 | +2.32 | +2.91 |
| SC | +11.68 | +0.23 | +0.79 | +0.23 |
| SD | +26.16 | +1.27 | +3.03 | +1.27 |
| TN | +23.21 | +0.44 | +1.51 | +0.44 |
| TX | +5.58 | +1.07 | +1.44 | +1.06 |
| UT | +20.48 | -4.76 | -0.23 | -4.76 |
| VT | -35.41 | +6.65 | +4.38 | +6.65 |
| VA | -10.11 | +2.44 | +2.31 | +2.44 |
| WA | -19.20 | +1.14 | +0.09 | +1.14 |
| WV | +38.93 | +0.79 | +2.81 | +0.78 |
| WI | -0.63 | -0.96 | -0.84 | -0.97 |
| WY | +43.38 | +0.56 | +4.64 | +0.56 |
| ME-1 (two-party) | -23.77 | +5.57 | +5.62 | +5.56 |
| ME-2 (two-party) | +7.66 | +1.19 | +1.20 | +1.18 |
| NE-1 (two-party) | +15.37 | +3.67 | +5.04 | +3.70 |
| NE-2 (two-party) | -6.68 | +5.54 | +6.79 ✗ | +5.57 |
| NE-3 (two-party) | +54.27 | +0.01 | +2.02 | +0.04 |

**2020 → 2024** - the record's margin and each method's miss (R − D, pp; ✗ the winner called wrong):

| jurisdiction | record | (a) | (b) | (c) |
|---|---:|---:|---:|---:|
| AL | +30.47 | +0.92 | +0.46 | +0.92 |
| AK | +13.14 | +2.85 | +2.59 | +2.85 |
| AZ | +5.53 | +0.09 | +0.10 | +0.09 |
| AR | +30.64 | +2.91 | +2.28 | +2.91 |
| CA | -20.13 | -3.10 | -3.53 | -3.10 |
| CO | -10.98 | +3.41 | +3.30 | +3.41 |
| CT | -14.50 | +0.36 | +0.20 | +0.36 |
| DE | -14.70 | +1.66 | +1.53 | +1.66 |
| DC | -83.81 | +2.98 | -1.49 | +2.98 |
| FL | +13.10 | -3.81 | -3.78 | -3.81 |
| GA | +2.19 | +3.50 | +3.53 | +3.50 |
| HI | -23.10 | -0.43 | -0.86 | -0.43 |
| ID | +36.49 | +0.21 | -0.55 | +0.21 |
| IL | -10.90 | -0.16 | -0.28 | -0.16 |
| IN | +18.96 | +3.04 | +2.81 | +3.04 |
| IA | +13.21 | +0.92 | +0.83 | +0.92 |
| KS | +16.12 | +4.45 | +4.23 | +4.45 |
| KY | +30.53 | +1.33 | +0.83 | +1.33 |
| LA | +22.01 | +2.53 | +2.26 | +2.53 |
| ME | -6.94 | +3.79 | +3.71 | +3.79 |
| MD | -28.54 | +1.25 | +0.66 | +1.25 |
| MA | -25.20 | -2.33 | -2.92 | -2.33 |
| MI | +1.41 | +1.73 | +1.75 | +1.73 |
| MN | -4.24 | +3.06 | +3.02 | +3.06 |
| MS | +22.89 | -0.42 | -0.62 | -0.42 |
| MO | +18.41 | +2.91 | +2.71 | +2.91 |
| MT | +19.93 | +2.37 | +2.10 | +2.37 |
| NE | +20.46 | +4.52 | +4.18 | +4.52 |
| NV | +3.10 | +0.44 | +0.41 | +0.44 |
| NH | -2.78 | +1.35 | +1.34 | +1.35 |
| NJ | -5.91 | -4.10 | -4.16 | -4.10 |
| NM | -6.00 | +1.14 | +1.08 | +1.14 |
| NY | -12.59 | -4.61 | -4.82 | -4.61 |
| NC | +3.21 | +4.06 | +4.07 | +4.06 |
| ND | +36.45 | +2.82 | +1.95 | +2.82 |
| OH | +11.21 | +2.75 | +2.70 | +2.75 |
| OK | +34.26 | +4.75 | +3.94 | +4.75 |
| OR | -14.31 | +4.15 | +3.97 | +4.15 |
| PA | +1.70 | +3.06 | +3.08 | +3.06 |
| RI | -13.78 | -1.07 | -1.26 | -1.07 |
| SC | +17.87 | -0.26 | -0.37 | -0.26 |
| SD | +29.19 | +2.90 | +2.34 | +2.90 |
| TN | +29.72 | -0.58 | -1.00 | -0.58 |
| TX | +13.68 | -2.18 | -2.20 | -2.18 |
| UT | +21.58 | +4.83 | +4.34 | +4.83 |
| VT | -31.51 | +2.02 | +1.31 | +2.02 |
| VA | -5.77 | +1.58 | +1.55 | +1.58 |
| WA | -18.22 | +4.94 | +4.70 | +4.94 |
| WV | +41.86 | +3.00 | +1.96 | +3.00 |
| WI | +0.86 | +4.44 | +4.45 | +4.44 |
| WY | +45.76 | +3.55 | +2.17 | +3.55 |
| ME-1 (two-party) | -22.08 | +4.41 | +4.09 | +4.41 |
| ME-2 (two-party) | +9.23 | +4.53 | +4.40 | +4.53 |
| NE-1 (two-party) | +13.23 | +8.23 | +7.98 | +8.23 |
| NE-2 (two-party) | -4.68 | +4.10 | +4.04 | +4.10 |
| NE-3 (two-party) | +54.43 | +5.94 | +3.97 | +5.94 |
<!-- END GENERATED -->

## The national vote - R-US15

Which vote the US poll is (`docs/specs/USA_STAGE_PLAN.md`, R-US15): **(a)** the House national vote - the history is the last two House
elections, and the presidential candidates carry it by E1's factors; **(b)** the presidential popular vote as the history; **(c)** two
histories, one per contest. Code expects to recommend (a), unless its national miss is well above (b)'s. The instrument is
`PoliSim.EditorTools.UsNationalVoteCheck`; the records are `COMPLETED.md` §788 (the House vote, `ElectionsData/usa/president_returns.md`)
and §789 (this measurement).

| part | what it does | basis |
|---|---|---|
| The histories | (a) the House vote by the Clerk of the House's recapitulation - each party's own ballot line, 50 states; (b) the presidential vote, US-3's catalog - 51 jurisdictions | SOURCED (§786, §788) |
| The positions | the roster's: REP's and DEM's points on GPS 2019's economic and social scales | SOURCED, PROVISIONAL (`ElectionsData/positions/party_positions.md`) |
| The economic weight | the USA's declared weight (`PartySystems.UsEconomicWeight` since US-6, which the live electorate reads; `VoteShareBacktest.UsEconomicWeight` names it), from Gallup's table of July 2026 - after every election used. Inert for the two-party split at the fitted points; it sets where a held fit's mean sits and what happens away from those points | DECLARED |
| The fit | to each history's T-1 - the last election before the predicted one, which a game started on the eve holds as its prior - never to the predicted election. Free: `VoteModel.Calibrate` as it stands. Held: the σ and τ of each country R-US15's spread is taken over (`PartySystems.UsSpreadFrom` - every country `PartySystems.TryElectorate` holds but the USA, whose own is their median since US-6; the list checked against it), read there at run time, and - added with the measurement - the four's median (σ and τ each the median of the four); the mean on the segment from DEM's point to REP's, at the place that gives T-1's split, solved continuously | DECLARED |
| The loyalty | T-1 against T-2 (`LoyaltyModel.PartyLoyalties`), each party's share of all votes cast - the four countries' basis as near as the sources allow: the presidential history's share of the FEC's total, the House history's of the Clerk's Total, which carries the non-votes some states report and Maine's re-counted ballots. With two parties the loyalty cannot move the split (below), so the denominator moves no miss; the prior is normalised over the roster by `PreferenceModel.Preference` itself | DECLARED |
| The prediction | the live chain's order - `VoteModel.PredictShares`, the compatibility scale, `PreferenceModel.Preference`, `EconomicVote.ApplyRecordShift` - composed in the instrument; since US-6 the USA's live poll is this chain on history (a) at the four's median (`PartySystems.TryElectorate`, held equal to the instrument's fit by `UsPresidentialFactorCheck`), and the rest stays the instrument's | as built |
| The economic vote | read on a world built on the eve as a new game builds one - the epoch set before the world is created - and stepped to the election day: the record over the term (§752) at Table 9.1's presidential row on the president's party (`EconomicVote.Magnitudes`), for both histories, the House's too. Printed in its own column, never folded into the history's miss; the congressional row stays out of code (US-13's) | as built |
| The cases | 2024 by each history on its eve world. Out of sample: 2022's House on its eve world, by (a) and by (b)'s presidential 2020; 2020 with no world - (b) the case US-6 cites as "2020 from 2016", and (a) - since no world can be built before the USA's first government of record (`WorldClock.Governments`; none to seat). Each scored on its own contest, where it was held, and on the other | DECLARED |
| E1's factors | on the 2024 eve world's poll, per history: Trump's factor his share of the FEC's total ÷ REP's poll, Harris's likewise; Stein, Kennedy, Oliver and the rest at their shares of record as base | E1; in-sample by construction |

**The identity.** With two parties the spatial model pins one number, the two-party split, and the compatibility scale hands the spatial
shares back unchanged - so an electorate fitted to T-1 gives T-1's split back through the preference layer, whatever the loyalty. The
instrument checks it on every fit before it prints anything else - exactly for a held fit, which is solved, and within its grid's residual for
the free one. A history's national miss is therefore its no-change miss - T-1's split against the predicted election's - plus the economic
vote. The fits give the same split at the fitted points and differ in their spread and in where their means sit; away from those points the
spread decides two things the card prints, spatially, before the loyalty draws the poll back toward the prior: how far the split moves when a
party's point moves (US-33's own nominee off the party's point - the moved point), and what a third placed unit takes (SP-6b's third party, a
created party - the midway unit). **R-US15's rule** is run by the instrument over the 2024 cases and its verdict printed; a verdict the rule
leaves to Elias fails the instrument, to be asked. **The factors** close 2024 by construction on the world they are fitted on, so they cannot
decide R-US15: the miss table does.

### The readings (US-5)

<!-- GENERATED by PoliSim.EditorTools.UsNationalVoteCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: fd8341b363dbe018c877fd83164374563903447a56d98eeab03e5de4a0b404b7 -->
### The national vote by each history

Two-party, signed: REP's predicted share of the two parties less the record's, in points. "Before" is the poll the vote model gives (every fit gives the same - the identity holds on each); "after" adds the economic vote of the eve world, printed in its own column. The loyalty is each party's, T-1 against T-2. "(own)" marks the history's own contest; 2022 held no presidential vote, so the presidential history is scored there on the House alone.

| predicted | history | T-1 split (R) | loyalty R / D | economic vote (DEM, pp) | poll R before → after | scored on | record (R) | miss before | miss after |
|---|---|---:|---:|---:|---:|---|---:|---:|---:|
| 2024 | (a) House 2022 against 2020 | 51.40 % | 94.5 / 94.1 | +2.50 | 51.40 % → 48.90 % | House 2024 (own) | 51.32 % | +0.08 | -2.42 |
|  |  |  |  |  |  | president 2024 | 50.75 % | +0.65 | -1.85 |
| 2024 | (b) presidential 2020 against 2016 | 47.73 % | 98.4 / 93.9 | +2.50 | 47.73 % → 45.23 % | president 2024 (own) | 50.75 % | -3.02 | -5.52 |
|  |  |  |  |  |  | House 2024 | 51.32 % | -3.59 | -6.09 |
| 2022 | (a) House 2020 against 2018 | 48.44 % | 93.7 / 95.0 | -0.33 | 48.44 % → 48.78 % | House 2022 (own) | 51.40 % | -2.95 | -2.62 |
| 2022 | (b) presidential 2020 against 2016 | 47.73 % | 98.4 / 93.9 | -0.33 | 47.73 % → 48.06 % | House 2022 | 51.40 % | -3.67 | -3.33 |
| 2020 | (a) House 2018 against 2016 | 45.55 % | 91.6 / 89.4 | no world | 45.55 % | House 2020 (own) | 48.44 % | -2.89 | - |
|  |  |  |  |  |  | president 2020 | 47.73 % | -2.18 | - |
| 2020 | (b) presidential 2016 against 2012 | 48.89 % | 97.6 / 94.4 | no world | 48.89 % | president 2020 (own) | 47.73 % | +1.16 | - |
|  |  |  |  |  |  | House 2020 | 48.44 % | +0.44 | - |

**By R-US15's rule, code recommends (a)**: (a)'s 2024 miss on its own contest is not above (b)'s, before the economic vote (+0.08 against -3.02) and after it (-2.42 against -5.52). On the presidential vote, (a) is the closer as well (+0.65 and -1.85).

Out of sample, (a) against (b): 2022's House vote - (a) the closer before the economic vote (-2.95 against -3.67), (a) after it (-2.62 against -3.33); 2020's House vote - (b) the closer before the economic vote (-2.89 against +0.44) (no world); 2020's presidential vote - (b) the closer before the economic vote (-2.18 against +1.16) (no world).

The economic vote on each eve world, as the record over the term reads it:

- 2024: term index 70.8 - took office 2021-01-20, judged 2024-11-05: unemployment 6.4 % (2021-01) -> 4.1 % (2024-10); inflation 1.4 % (2021-01) -> 2.4 % (2024-09). The House's largest party REP; DEM's economic vote 0.06; DEM +2.50 pp.
- 2022: term index 48.3 - took office 2021-01-20, judged 2022-11-08: unemployment 6.4 % (2021-01) -> 3.6 % (2022-10); inflation 1.4 % (2021-01) -> 8.2 % (2022-09). The House's largest party DEM; DEM's economic vote 0.10; DEM -0.33 pp.

### The fits and what they leave undecided

Each history's electorate fitted to its T-1 (wEcon 0.65); the positions are the roster's. Free: `VoteModel.Calibrate`'s grid. Held: each country's σ and τ as `PartySystems.TryElectorate` holds them, and the four's median (σ and τ each the median of the four) - the mean on the segment from DEM's point to REP's at the place (0 at DEM, 1 at REP) that gives T-1's split. "Slide": the largest change in the split, in points, when the mean moves one or two units either way along the line dividing the two parties. "Moved point": the change in the split, in points, when DEM's point moves one unit toward REP's - a nominee off the party's point. "Midway unit": the spatial share the fit gives a third unit placed at the parties' midpoint. Both last columns are spatial, before the loyalty draws the poll back toward the prior.

| T-1 | fit | μ econ, μ soc | σ | τ | place | split (R) | residual (pp) | slide ±1 / ±2 (pp) | moved point (pp) | midway unit |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| House 2022 (R 51.40 %) | free | 6.000, 6.500 | 3.00 | 8.00 | - | 51.40 % | 0.0004 | 0.51 / 1.46 | -5.91 | 42.6 % |
|  | Sweden's spread | 6.346, 5.834 | 3.00 | 0.50 | 0.5813 | 51.40 % | 0.0000 | 0.40 / 1.38 | -7.63 | 52.2 % |
|  | Germany's spread | 6.063, 5.464 | 1.00 | 16.00 | 0.5185 | 51.40 % | 0.0000 | 0.01 / 0.05 | -4.62 | 42.0 % |
|  | Poland's spread | 6.041, 5.435 | 1.50 | 8.00 | 0.5135 | 51.40 % | 0.0000 | 0.23 / 0.75 | -7.38 | 47.8 % |
|  | Italy's spread | 6.011, 5.395 | 1.00 | 4.00 | 0.5068 | 51.40 % | 0.0000 | 0.01 / 0.08 | -12.78 | 63.7 % |
|  | the four's median | 6.023, 5.411 | 1.25 | 6.00 | 0.5096 | 51.40 % | 0.0000 | 0.09 / 0.38 | -9.35 | 53.3 % |
| House 2020 (R 48.44 %) | free | 6.750, 5.250 | 3.50 | 4.00 | - | 48.44 % | 0.0001 | 0.18 / 0.54 | -6.69 | 44.5 % |
|  | Sweden's spread | 6.169, 5.602 | 3.00 | 0.50 | 0.5420 | 48.44 % | 0.0000 | 0.37 / 1.27 | -7.57 | 52.1 % |
|  | Germany's spread | 5.887, 5.234 | 1.00 | 16.00 | 0.4794 | 48.44 % | 0.0000 | 0.00 / 0.03 | -4.23 | 42.0 % |
|  | Poland's spread | 5.926, 5.285 | 1.50 | 8.00 | 0.4881 | 48.44 % | 0.0000 | 0.19 / 0.61 | -7.08 | 47.8 % |
|  | Italy's spread | 5.946, 5.310 | 1.00 | 4.00 | 0.4924 | 48.44 % | 0.0000 | 0.01 / 0.07 | -12.43 | 63.7 % |
|  | the four's median | 5.934, 5.295 | 1.25 | 6.00 | 0.4898 | 48.44 % | 0.0000 | 0.08 / 0.32 | -9.04 | 53.3 % |
| House 2018 (R 45.55 %) | free | 5.750, 5.250 | 2.00 | 8.00 | - | 45.55 % | 0.0021 | 0.24 / 0.81 | -6.15 | 45.3 % |
|  | Sweden's spread | 5.996, 5.376 | 3.00 | 0.50 | 0.5035 | 45.55 % | 0.0000 | 0.34 / 1.16 | -7.47 | 51.7 % |
|  | Germany's spread | 5.714, 5.007 | 1.00 | 16.00 | 0.4409 | 45.55 % | 0.0000 | 0.00 / 0.01 | -3.83 | 41.9 % |
|  | Poland's spread | 5.814, 5.138 | 1.50 | 8.00 | 0.4631 | 45.55 % | 0.0000 | 0.15 / 0.46 | -6.75 | 47.7 % |
|  | Italy's spread | 5.882, 5.227 | 1.00 | 4.00 | 0.4782 | 45.55 % | 0.0000 | 0.01 / 0.06 | -12.03 | 63.6 % |
|  | the four's median | 5.846, 5.180 | 1.25 | 6.00 | 0.4703 | 45.55 % | 0.0000 | 0.06 / 0.25 | -8.69 | 53.2 % |
| presidential 2020 (R 47.73 %) | free | 6.000, 6.000 | 3.50 | 16.00 | - | 47.73 % | 0.0029 | 0.11 / 0.38 | -3.63 | 39.3 % |
|  | Sweden's spread | 6.126, 5.546 | 3.00 | 0.50 | 0.5325 | 47.73 % | 0.0000 | 0.36 / 1.24 | -7.55 | 52.0 % |
|  | Germany's spread | 5.845, 5.178 | 1.00 | 16.00 | 0.4699 | 47.73 % | 0.0000 | 0.00 / 0.03 | -4.14 | 42.0 % |
|  | Poland's spread | 5.899, 5.249 | 1.50 | 8.00 | 0.4819 | 47.73 % | 0.0000 | 0.18 / 0.57 | -7.00 | 47.8 % |
|  | Italy's spread | 5.930, 5.290 | 1.00 | 4.00 | 0.4889 | 47.73 % | 0.0000 | 0.01 / 0.07 | -12.34 | 63.7 % |
|  | the four's median | 5.912, 5.266 | 1.25 | 6.00 | 0.4850 | 47.73 % | 0.0000 | 0.07 / 0.30 | -8.96 | 53.3 % |
| presidential 2016 (R 48.89 %) | free | 5.750, 5.500 | 1.00 | 16.00 | - | 48.88 % | 0.0018 | 0.00 / 0.03 | -4.29 | 42.0 % |
|  | Sweden's spread | 6.195, 5.637 | 3.00 | 0.50 | 0.5479 | 48.89 % | 0.0000 | 0.37 / 1.28 | -7.58 | 52.1 % |
|  | Germany's spread | 5.914, 5.268 | 1.00 | 16.00 | 0.4853 | 48.89 % | 0.0000 | 0.00 / 0.03 | -4.29 | 42.0 % |
|  | Poland's spread | 5.944, 5.307 | 1.50 | 8.00 | 0.4919 | 48.89 % | 0.0000 | 0.20 / 0.63 | -7.13 | 47.8 % |
|  | Italy's spread | 5.955, 5.323 | 1.00 | 4.00 | 0.4946 | 48.89 % | 0.0000 | 0.01 / 0.07 | -12.49 | 63.7 % |
|  | the four's median | 5.947, 5.312 | 1.25 | 6.00 | 0.4927 | 48.89 % | 0.0000 | 0.08 / 0.33 | -9.09 | 53.3 % |

Every fit holds its T-1 split (free: within Calibrate's grid; held: solved). Away from the fitted points the spread decides: a nominee one unit off DEM's point moves the split by between -3.63 and -12.78 points, by the fit, and the midway unit's share runs from 39.3 % to 63.7 % - what the choice of spread decides, and the two-party split at the fitted points does not.

### E1's factors on the 2024 eve world's poll (in-sample by construction)

The field: Trump (REP) 49.80 %, Harris (DEM) 48.32 %; at their shares of record as base: Stein 0.56 %, Kennedy 0.49 %, Oliver 0.42 %, the rest 0.42 % (every other column of the FEC's workbook, None of These Candidates and the scattered write-ins among them). The nominees together hold 98.12 % of the vote, so a poll exact on the two-party split gives both factors that figure - the column of factors ÷ the nominees' share divides it out. The field's sum is one by construction (E1's normalisation), printed as the design asks.

| history | poll R / D | Trump's factor | Harris's factor | the field's sum | factors ÷ the nominees' share (R / D) | the poll's two-party miss on the presidential 2024 (pp) |
|---|---:|---:|---:|---:|---:|---:|
| (a) House 2022 against 2020 | 48.90 % / 51.10 % | 1.018390 | 0.945620 | 1.000000 | 1.0379 / 0.9637 | -1.85 |
| (b) presidential 2020 against 2016 | 45.23 % / 54.77 % | 1.100946 | 0.882315 | 1.000000 | 1.1220 / 0.8992 | -5.52 |
<!-- END GENERATED -->


## The House count - R-US18

R-US18's rule (`docs/specs/USA_STAGE_PLAN.md`, its fenced block, preregistered §791 and amended §797 by Elias's ruling G1) run by `UsHouseCountCheck` on the House of record, 2016-2024: each method's seats, its distance from the record (N), its misplaced seats (G over every state, H over the held states), its delegations right (K) and the contingent call, per cycle; the steps; the declared readings; the outcome and the verdict line. Generated, never typed - a stale block fails the bars.

<!-- GENERATED by PoliSim.EditorTools.UsHouseCountCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: 29e821d5071610890653ad679ab47ef6e0a4468f8d93f46cb19d421e08fea5d8 -->
```text
the record 2016: delegations REP 32 DEM 17 tied 1; the contingent call REP
the record 2018: delegations REP 26 DEM 22 tied 2; the contingent call REP
the record 2020: delegations REP 27 DEM 20 tied 3; the contingent call REP
the record 2022: delegations REP 26 DEM 22 tied 2; the contingent call REP
the record 2024: delegations REP 30 DEM 18 tied 2; the contingent call REP
C1 2016->2018 (swing): V 0.5055 -> 0.4555, swing -0.0499; the record REP 199 DEM 235 undecided 1; redrawn: CO(7) MN(8) PA(18)
  (a) F-old    REP 226  N +26  control REP  G  32  H  28  K 45/50  contingent REP
  (a) F-prop   REP 224  N +24  control REP  G  28  H  28  K 46/50  contingent REP
  (b)          REP 221  N +21  control REP  G  32  H  28  K 43/50  contingent REP
  (b0)         REP 200  N   0  control DEM  G  63  H  63  K 36/50  contingent no state majority
  (b U-blind)  REP 219  N +19  control REP  G  30  H  26  K 43/50  contingent REP
  (b')         REP 200  N   0  control DEM  G  29  H  27  K 44/50  contingent REP  - alpha 0.1923, k 2.3748
  (c)          no window - no (c)
  no change    REP 241  N +41  control REP  G  41  H  36  K 43/50  contingent REP
  F-old counted: CO (F-old), MN (F-old), PA (F-old)
C2 2018->2020 (swing): V 0.4555 -> 0.4844, swing +0.0289; the record REP 213 DEM 222 undecided 0; redrawn: NC(13)
  (a) F-old    REP 224  N +11  control REP  G  13  H  12  K 46/50  contingent REP
  (a) F-prop   REP 222  N  +9  control REP  G  13  H  12  K 46/50  contingent REP
  (b)          REP 212  N  -1  control DEM  G  25  H  23  K 44/50  contingent REP
  (b0)         REP 212  N  -1  control DEM  G  61  H  60  K 38/50  contingent no state majority
  (b U-blind)  REP 212  N  -1  control DEM  G  25  H  23  K 44/50  contingent REP
  (b')         REP 228  N +15  control REP  G  25  H  22  K 45/50  contingent REP  - alpha 0.3466, k 3.1424
  (c)          REP 224  N +11  control REP  - rho 1.9146 over 2016->2018
  no change    REP 200  N -13  control DEM  G  19  H  17  K 48/50  contingent REP
  F-old counted: NC (F-old, M 12, n 13)
C3 2020->2022 (printed): V 0.4844 -> 0.5140, swing +0.0295; the record REP 222 DEM 213 undecided 0; redrawn: AL(7) AR(4) AZ(9) CA(52) CO(8) CT(5) FL(28) GA(14) HI(2) IA(4) ID(2) IL(17) IN(9) KS(4) KY(6) LA(6) MA(9) MD(8) ME(2) MI(13) MN(8) MO(8) MS(4) MT(2) NC(14) NE(3) NH(2) NJ(12) NM(3) NV(4) NY(26) OH(15) OK(5) OR(6) PA(17) RI(2) SC(7) TN(9) TX(38) UT(4) VA(11) WA(10) WI(8) WV(2)
  (a) F-old    REP 237  N +15  control REP  G  33  H   1  K 42/50  contingent REP
  (a) F-prop   REP 224  N  +2  control REP  G  66  H   1  K 33/50  contingent no state majority
  (b)          REP 223  N  +1  control REP  G  25  H   1  K 43/50  contingent REP
  (b0)         REP 224  N  +2  control REP  G  66  H   1  K 33/50  contingent no state majority
  (b U-blind)  REP 224  N  +2  control REP  G  24  H   1  K 43/50  contingent REP
  (b')         REP 241  N +19  control REP  G  31  H   1  K 44/50  contingent REP  - alpha 0.1142, k 2.8013
  (c)          REP 235  N +13  control REP  - rho 1.7036 over 2016->2018, 2018->2020
  no change    REP 213  N  -9  control DEM  G  25  H   1  K 47/50  contingent REP
  F-old counted: AL (F-old), AR (F-old), AZ (F-old), CA (F-old, M 53, n 52), CO (F-old, M 7, n 8), CT (F-old), FL (F-old, M 27, n 28), GA (F-old), HI (F-old), IA (F-old), ID (F-old), IL (F-old, M 18, n 17), IN (F-old), KS (F-old), KY (F-old), LA (F-old), MA (F-old), MD (F-old), ME (F-old), MI (F-old, M 14, n 13), MN (F-old), MO (F-old), MS (F-old), MT (F-old, M 1, n 2), NC (F-old, M 13, n 14), NE (F-old), NH (F-old), NJ (F-old), NM (F-old), NV (F-old), NY (F-old, M 27, n 26), OH (F-old, M 16, n 15), OK (F-old), OR (F-old, M 5, n 6), PA (F-old, M 18, n 17), RI (F-old), SC (F-old), TN (F-old), TX (F-old, M 36, n 38), UT (F-old), VA (F-old), WA (F-old), WI (F-old), WV (F-old, M 3, n 2)
C4 2022->2024 (level): V 0.5140 -> 0.5132, swing -0.0008; the record REP 220 DEM 215 undecided 0; redrawn: AL(7) GA(14) LA(6) NC(14) NY(26)
  (a) F-old    REP 222  N  +2  control REP  G  18  H   9  K 45/50  contingent REP
  (a) F-prop   REP 219  N  -1  control REP  G  19  H   9  K 44/50  contingent no state majority
  (b)          REP 222  N  +2  control REP  G  18  H   9  K 45/50  contingent REP
  (b0)         REP 222  N  +2  control REP  G  74  H  64  K 34/50  contingent no state majority
  (b U-blind)  REP 222  N  +2  control REP  G  18  H   9  K 45/50  contingent REP
  (b')         REP 222  N  +2  control REP  G  18  H   9  K 45/50  contingent REP  - alpha -0.1275, k 2.7930
  (c)          REP 221  N  +1  control REP  - rho 1.7036 over 2016->2018, 2018->2020
  no change    REP 222  N  +2  control REP  G  18  H   9  K 45/50  contingent REP
  F-old counted: AL (F-old), GA (F-old), LA (F-old), NC (F-old), NY (F-old)
step 1 - (a)'s fallback over D's redrawn state-cycles: W F-old 14, F-prop 11 - F-prop strictly lower
step 2 - sum H over D: (a) 49, (b) 60; (b)'s switch conditions failed: H(b) < H(a) in every swing cycle; sum G(b) < sum G(a); K(b) >= K(a) in every cycle - code recommends (a)
step 3 - C1 |N| (a) 26, (b) 21; C2 |N| (a) 11, (b) 1 - S2 fires
step 4 - (c): 1 forward swing cycle(s) in D - C2 |N| (c) 11, (a) 11, no change 13 - S3 cannot fire
step 5 - (b') against (a): C1 H 27 against 28, K 44 against 45; C2 H 22 against 12, K 45 against 46; C4 H 9 against 9, K 45 against 45; sum G 72 against 63 - S6 does not fire
step 7 - every measured cycle's later election is 2024 or before: S7 cannot fire
reading (i) 2020's truth as published: (a) + S2, S5 - the same
reading (ii) D less C1: (a) + S2 printed (one swing cycle: the step-2 verdict and S5 compared) - CHANGES THE OUTCOME
reading (iii) Minnesota 2018 held: (a) + S2, S5 - the same
reading (iv) Colorado 2018 held: (a) + S2, S5 - the same
reading (v) both held: (a) + S2, S5 - the same
reading (vi) the swing on the deciding count - PRINTED ONLY (G1): (a) + S2, S5 (printed only - compared with nothing)
reading (vii) the level cycle out of the sums: (a) + S2, S5 - the same
THE OUTCOME (the run over D): (a) + S2, S5, S4 [(ii)]
THE VERDICT: R-US18's rule does not decide: S2 ((a)'s |N| above (b)'s in every swing cycle - C1 |N| (a) 26, (b) 21; C2 |N| (a) 11, (b) 1); S5 (W over D's redrawn state-cycles F-old 14, F-prop 11); S4 ((ii) gives (a) + S2 printed - S5: W over D's redrawn state-cycles F-old 10, F-prop 11; the run over D gives (a) + S2, S5); (c), printed: 1 forward swing cycle(s) in D - C2 |N| (c) 11, (a) 11, no change 13 - asked.
the decisive digest: 2df09e4c3986bda5b6030d0927929cedc273b8ba8cb3771fd405c7aa9af5d513
```
<!-- END GENERATED -->

## The Senate count - R-US19

R-US19's rule (`docs/specs/USA_STAGE_PLAN.md`, its fenced block, preregistered §794 and amended §797 by Elias's rulings G1-G3) run by `UsSenateRaceCheck` on the Senate of record: (a) the state's REP share by R-US14 (a)'s derivation and (b) the seat's base share rolled by the House's own-line swing, each called against the winner of the target race's deciding count over K1 (2018 -> 2024, Class I) and K3 (2016 -> 2022, Class III) - the per-seat table, M and N, the steps, (c) printed per parent, the declared readings, the outcome and the verdict line, and the block's printed rows. Generated, never typed - a stale block fails the bars.

<!-- GENERATED by PoliSim.EditorTools.UsSenateRaceCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: eb51169cc1636443171f76b770084fb04927dcec96cb1373441e131b33257259 -->
```text
K1 2018->2024 Class 1 (a presidential year): V 0.4555 -> 0.5132, swing +0.0576; 33 seats; the truth REP 14 DEM 19 O 0 Z 0
  AZ base 2018 AZ cl1 [general (the Clerk's); R 1135200 DC 1191100; DEM side one candidate] x 0.4880, the change +0.0576 | (a) 0.5286 R | (b) 0.5456 R | truth D  MISS both
  CA base 2018 CA cl1 [general (the Clerk's); no count two-sided; R 0 DC 11113364; DEM side summed, its strongest 6019422] x 0.3316 <F-H: the House CA 2018, side agrees with the base winner's>, the change +0.0576 | (a) 0.3812 D | (b) 0.3892 D | truth D
  CT base 2018 CT cl1 [general (the Clerk's); R 545717 DC 825579; DEM side one candidate] x 0.3980, the change +0.0576 | (a) 0.4281 D | (b) 0.4556 D | truth D
  DE base 2018 DE cl1 [general (the Clerk's); R 137127 DC 217385; DEM side one candidate] x 0.3868, the change +0.0576 | (a) 0.4338 D | (b) 0.4444 D | truth D
  FL base 2018 FL cl1 [general (the Clerk's); R 4099505 DC 4089472; DEM side one candidate] x 0.5006, the change +0.0576 | (a) 0.5469 R | (b) 0.5583 R | truth R
  HI base 2018 HI cl1 [general (the Clerk's); R 112035 DC 276316; DEM side one candidate] x 0.2885, the change +0.0576 | (a) 0.3799 D | (b) 0.3461 D | truth D
  IN base 2018 IN cl1 [general (the Clerk's); R 1158000 DC 1023553; DEM side one candidate] x 0.5308, the change +0.0576 | (a) 0.6123 R | (b) 0.5885 R | truth R
  MA base 2018 MA cl1 [general (the Clerk's); R 979210 DC 1633371; DEM side one candidate] x 0.3748, the change +0.0576 | (a) 0.3591 D | (b) 0.4324 D | truth D
  MD base 2018 MD cl1 [general (the Clerk's); R 697017 DC 1491614; DEM side one candidate] x 0.3185, the change +0.0576 | (a) 0.3601 D | (b) 0.3761 D | truth D
  ME base 2018 ME cl1 [general (the Clerk's); R 223502 DC 410843; the winner an I with the Democrats; DEM side summed, its strongest 344575] x 0.3523, the change +0.0576 | (a) 0.4838 D | (b) 0.4100 D | truth D
  MI base 2018 MI cl1 [general (the Clerk's); R 1938818 DC 2214478; DEM side one candidate] x 0.4668, the change +0.0576 | (a) 0.5160 R | (b) 0.5245 R | truth D  MISS both
  MN base 2018 MN cl1 [general (the Clerk's); R 940437 DC 1566174; DEM side one candidate] x 0.3752, the change +0.0576 | (a) 0.4939 D | (b) 0.4328 D | truth D
  MO base 2018 MO cl1 [general (the Clerk's); R 1254927 DC 1112935; DEM side one candidate] x 0.5300, the change +0.0576 | (a) 0.6086 R | (b) 0.5876 R | truth R
  MS base 2018 MS cl1 [general (the Clerk's); R 547619 DC 369567; DEM side one candidate] x 0.5971, the change +0.0576 | (a) 0.6139 R | (b) 0.6547 R | truth R
  MT base 2018 MT cl1 [general (the Clerk's); R 235963 DC 253876; DEM side one candidate] x 0.4817, the change +0.0576 | (a) 0.6144 R | (b) 0.5394 R | truth R
  ND base 2018 ND cl1 [general (the Clerk's); R 179720 DC 144376; DEM side one candidate] x 0.5545, the change +0.0576 | (a) 0.7027 R | (b) 0.6122 R | truth R
  NE base 2018 NE cl1 [general (the Clerk's); R 403151 DC 269917; DEM side one candidate] x 0.5990, the change +0.0576 | (a) 0.6283 R | (b) 0.6566 R | truth R
  NJ base 2018 NJ cl1 [general (the Clerk's); R 1357355 DC 1711654; DEM side one candidate] x 0.4423, the change +0.0576 | (a) 0.4493 D | (b) 0.4999 D | truth D
  NM base 2018 NM cl1 [general (the Clerk's); R 212813 DC 376998; DEM side one candidate] x 0.3608, the change +0.0576 | (a) 0.4751 D | (b) 0.4185 D | truth D
  NV base 2018 NV cl1 [general (the Clerk's); R 441202 DC 490071; DEM side one candidate] x 0.4738, the change +0.0576 | (a) 0.5181 R | (b) 0.5314 R | truth D  MISS both
  NY base 2018 NY cl1 [general (the Clerk's); R 1998220 DC 4056931; DEM side one candidate] x 0.3300, the change +0.0576 | (a) 0.4128 D | (b) 0.3876 D | truth D
  OH base 2018 OH cl1 [general (the Clerk's); R 2053963 DC 2355923; DEM side one candidate] x 0.4658, the change +0.0576 | (a) 0.5709 R | (b) 0.5234 R | truth R
  PA base 2018 PA cl1 [general (the Clerk's); R 2134848 DC 2792437; DEM side one candidate] x 0.4333, the change +0.0576 | (a) 0.5242 R | (b) 0.4909 D | truth R  MISS b only
  RI base 2018 RI cl1 [general (the Clerk's); R 144421 DC 231477; DEM side one candidate] x 0.3842, the change +0.0576 | (a) 0.4242 D | (b) 0.4418 D | truth D
  TN base 2018 TN cl1 [general (the Clerk's); R 1227483 DC 985450; DEM side one candidate] x 0.5547, the change +0.0576 | (a) 0.6485 R | (b) 0.6123 R | truth R
  TX base 2018 TX cl1 [general (the Clerk's); R 4260553 DC 4045632; DEM side one candidate] x 0.5129, the change +0.0576 | (a) 0.5584 R | (b) 0.5706 R | truth R
  UT base 2018 UT cl1 [general (the Clerk's); R 665215 DC 328541; DEM side one candidate] x 0.6694, the change +0.0576 | (a) 0.6379 R | (b) 0.7270 R | truth R
  VA base 2018 VA cl1 [general (the Clerk's); R 1374313 DC 1910370; DEM side one candidate] x 0.4184, the change +0.0576 | (a) 0.4787 D | (b) 0.4760 D | truth D
  VT base 2018 VT cl1 [general (the Clerk's); R 74815 DC 183649; the winner an I with the Democrats; DEM side one candidate] x 0.2895, the change +0.0576 | (a) 0.3476 D | (b) 0.3471 D | truth D
  WA base 2018 WA cl1 [general (the Clerk's); R 1282804 DC 1803364; DEM side one candidate] x 0.4157, the change +0.0576 | (a) 0.4314 D | (b) 0.4733 D | truth D
  WI base 2018 WI cl1 [general (the Clerk's); R 1184885 DC 1472914; DEM side one candidate] x 0.4458, the change +0.0576 | (a) 0.5270 R | (b) 0.5035 R | truth D  MISS both
  WV base 2018 WV cl1 [general (the Clerk's); R 271113 DC 290510; DEM side one candidate] x 0.4827, the change +0.0576 | (a) 0.7282 R | (b) 0.5404 R | truth R
  WY base 2018 WY cl1 [general (the Clerk's); R 136210 DC 61227; DEM side one candidate] x 0.6899, the change +0.0576 | (a) 0.7556 R | (b) 0.7475 R | truth R
  K1: misses a only 0, b only 1, both 4
  K1: M (a) 4, (b) 5; N (a) +4, (b) +3; misses (a) AZ cl1, MI cl1, NV cl1, WI cl1; (b) AZ cl1, MI cl1, NV cl1, PA cl1, WI cl1
K3 2016->2022 Class 3 (a midterm): V 0.5055 -> 0.5140, swing +0.0085; 34 seats; the truth REP 19 DEM 15 O 0 Z 0
  AK base 2016 AK cl3 [general (the Clerk's); R 138149 DC 36200; DEM side one candidate] x 0.7924, the change +0.0085 | (a) 0.5969 R | (b) 0.8009 R | truth R
  AL base 2016 AL cl3 [general (the Clerk's); R 1335104 DC 748709; DEM side one candidate] x 0.6407, the change +0.0085 | (a) 0.6558 R | (b) 0.6492 R | truth R
  AR base 2016 AR cl3 [general (the Clerk's); R 661984 DC 400602; DEM side one candidate] x 0.6230, the change +0.0085 | (a) 0.6546 R | (b) 0.6315 R | truth R
  AZ base 2020 AZ cl3 (unexpired) [general (the Clerk's); R 1637661 DC 1716467; DEM side one candidate] x 0.4883, the change +0.0295 | (a) 0.5356 R | (b) 0.5178 R | truth D  MISS both
  CA base 2016 CA cl3 [general (the Clerk's); no count two-sided; R 0 DC 12244170; DEM side summed, its strongest 7542753] x 0.3519 <F-H: the House CA 2016, side agrees with the base winner's>, the change +0.0085 | (a) 0.3628 D | (b) 0.3604 D | truth D
  CO base 2016 CO cl3 [general (the Clerk's); R 1215318 DC 1370710; DEM side one candidate] x 0.4700, the change +0.0085 | (a) 0.4915 D | (b) 0.4785 D | truth D
  CT base 2016 CT cl3 [general (the Clerk's); R 552621 DC 1008714; DEM side one candidate] x 0.3539, the change +0.0085 | (a) 0.4492 D | (b) 0.3625 D | truth D
  FL base 2016 FL cl3 [general (the Clerk's); R 4835191 DC 4122088; DEM side one candidate] x 0.5398, the change +0.0085 | (a) 0.5238 R | (b) 0.5483 R | truth R
  GA base 2020 GA cl3 (unexpired) [runoff (the Clerk's); R 2195841 DC 2289113; DEM side one candidate] x 0.4896, the change +0.0295 | (a) 0.5434 R | (b) 0.5191 R | truth D  MISS both
  HI base 2016 HI cl3 [general (the Clerk's); R 92653 DC 306604; DEM side one candidate] x 0.2321, the change +0.0085 | (a) 0.3502 D | (b) 0.2406 D | truth D
  IA base 2016 IA cl3 [general (the Clerk's); R 926007 DC 549460; DEM side one candidate] x 0.6276, the change +0.0085 | (a) 0.5659 R | (b) 0.6361 R | truth R
  ID base 2016 ID cl3 [general (the Clerk's); R 449017 DC 188249; DEM side one candidate] x 0.7046, the change +0.0085 | (a) 0.6916 R | (b) 0.7131 R | truth R
  IL base 2016 IL cl3 [general (the Clerk's); R 2184692 DC 3012940; DEM side one candidate] x 0.4203, the change +0.0085 | (a) 0.4311 D | (b) 0.4288 D | truth D
  IN base 2016 IN cl3 [general (the Clerk's); R 1423991 DC 1158947; DEM side one candidate] x 0.5513, the change +0.0085 | (a) 0.6147 R | (b) 0.5598 R | truth R
  KS base 2016 KS cl3 [general (the Clerk's); R 732376 DC 379740; DEM side one candidate] x 0.6585, the change +0.0085 | (a) 0.6239 R | (b) 0.6671 R | truth R
  KY base 2016 KY cl3 [general (the Clerk's); R 1090177 DC 813246; DEM side one candidate] x 0.5727, the change +0.0085 | (a) 0.6680 R | (b) 0.5813 R | truth R
  LA base 2016 LA cl3 [runoff (the Clerk's); R 536191 DC 347816; DEM side one candidate] x 0.6065, the change +0.0085 | (a) 0.6155 R | (b) 0.6151 R | truth R
  MD base 2016 MD cl3 [general (the Clerk's); R 972557 DC 1659907; DEM side one candidate] x 0.3694, the change +0.0085 | (a) 0.3831 D | (b) 0.3780 D | truth D
  MO base 2016 MO cl3 [general (the Clerk's); R 1378458 DC 1300200; DEM side one candidate] x 0.5146, the change +0.0085 | (a) 0.6118 R | (b) 0.5231 R | truth R
  NC base 2016 NC cl3 [general (the Clerk's); R 2395376 DC 2128165; DEM side one candidate] x 0.5295, the change +0.0085 | (a) 0.5360 R | (b) 0.5380 R | truth R
  ND base 2016 ND cl3 [general (the Clerk's); R 268788 DC 58116; DEM side one candidate] x 0.8222, the change +0.0085 | (a) 0.7067 R | (b) 0.8307 R | truth R
  NH base 2016 NH cl3 [general (the Clerk's); R 353632 DC 354649; DEM side one candidate] x 0.4993, the change +0.0085 | (a) 0.5155 R | (b) 0.5078 R | truth D  MISS both
  NV base 2016 NV cl3 [general (the Clerk's); R 495079 DC 521994; DEM side one candidate] x 0.4868, the change +0.0085 | (a) 0.5050 R | (b) 0.4953 D | truth D  MISS a only
  NY base 2016 NY cl3 [general (the Clerk's); R 2009353 DC 5221949; DEM side one candidate] x 0.2779, the change +0.0085 | (a) 0.4047 D | (b) 0.2864 D | truth D
  OH base 2016 OH cl3 [general (the Clerk's); R 3118567 DC 1996908; DEM side one candidate] x 0.6096, the change +0.0085 | (a) 0.5586 R | (b) 0.6181 R | truth R
  OK base 2016 OK cl3 [general (the Clerk's); R 980892 DC 355911; DEM side one candidate] x 0.7338, the change +0.0085 | (a) 0.7027 R | (b) 0.7423 R | truth R
  OR base 2016 OR cl3 [general (the Clerk's); R 651106 DC 1105119; DEM side one candidate] x 0.3707, the change +0.0085 | (a) 0.4580 D | (b) 0.3793 D | truth D
  PA base 2016 PA cl3 [general (the Clerk's); R 2951702 DC 2865012; DEM side one candidate] x 0.5075, the change +0.0085 | (a) 0.5213 R | (b) 0.5160 R | truth D  MISS both
  SC base 2016 SC cl3 [general (the Clerk's); R 1241609 DC 757022; DEM side one candidate] x 0.6212, the change +0.0085 | (a) 0.5893 R | (b) 0.6297 R | truth R
  SD base 2016 SD cl3 [general (the Clerk's); R 265516 DC 104140; DEM side one candidate] x 0.7183, the change +0.0085 | (a) 0.6706 R | (b) 0.7268 R | truth R
  UT base 2016 UT cl3 [general (the Clerk's); R 760220 DC 301858; DEM side one candidate] x 0.7158, the change +0.0085 | (a) 0.6316 R | (b) 0.7243 R | truth R
  VT base 2016 VT cl3 [general (the Clerk's); R 103637 DC 192243; DEM side one candidate] x 0.3503, the change +0.0085 | (a) 0.3714 D | (b) 0.3588 D | truth D
  WA base 2016 WA cl3 [general (the Clerk's); R 1329338 DC 1913979; DEM side one candidate] x 0.4099, the change +0.0085 | (a) 0.4328 D | (b) 0.4184 D | truth D
  WI base 2016 WI cl3 [general (the Clerk's); R 1479471 DC 1380335; DEM side one candidate] x 0.5173, the change +0.0085 | (a) 0.5214 R | (b) 0.5258 R | truth R
  K3: misses a only 1, b only 0, both 4
  K3: M (a) 5, (b) 4; N (a) +5, (b) +4; misses (a) AZ cl3, GA cl3, NH cl3, NV cl3, PA cl3; (b) AZ cl3, GA cl3, NH cl3, PA cl3
step 6 - S9: every target and base race of D rests on a saved page
step 1 - K1 M (a) 4, (b) 5; K3 M (a) 5, (b) 4; K1 |N| (a) 4, (b) 3; K3 |N| (a) 5, (b) 4 - S1 (the cycles part)
step 2 - after S1, control both ways - (a)'s |N| above (b)'s in every cycle True, (b)'s above (a)'s False: K1 |N| (a) 4, (b) 3; K3 |N| (a) 5, (b) 4
step 5 - every winner of D's counts has a side: O = 0, S7 does not fire
step 3 - (c) over (a) on K1, in-sample; zero by construction except O seats, races not fitted, and races whose base-count share and deciding-count winner disagree: M 0, N 0; |c_j| over 35 fitted races mean 0.136, largest 0.532, RMS 0.178; not fitted: none; the races whose call c_j reverses: 2024 AZ cl1, 2024 MI cl1, 2024 NV cl1, 2024 WI cl1
  c_j: 2024 AZ cl1 -0.164; 2024 CA cl1 +0.126; 2024 CA cl1 (unexpired) +0.131; 2024 CT cl1 -0.099; 2024 DE cl1 -0.093; 2024 FL cl1 +0.073; 2024 HI cl1 -0.216; 2024 IN cl1 -0.043; 2024 MA cl1 +0.177; 2024 MD cl1 +0.332; 2024 ME cl1 -0.532; 2024 MI cl1 -0.071; 2024 MN cl1 -0.303; 2024 MO cl1 -0.158; 2024 MS cl1 +0.060; 2024 MT cl1 -0.320; 2024 ND cl1 -0.174; 2024 NE cl1 (final_none_top stands for DEM) -0.391; 2024 NJ cl1 +0.006; 2024 NM cl1 -0.103; 2024 NV cl1 -0.107; 2024 NY cl1 -0.020; 2024 OH cl1 -0.210; 2024 PA cl1 -0.092; 2024 RI cl1 -0.102; 2024 TN cl1 +0.012; 2024 TX cl1 -0.060; 2024 UT cl1 +0.111; 2024 VA cl1 -0.094; 2024 VT cl1 -0.048; 2024 WA cl1 -0.098; 2024 WI cl1 -0.125; 2024 WV cl1 -0.079; 2024 WY cl1 +0.007; 2024 NE cl2 (unexpired) -0.011
step 3 - (c) over (b) on K1, in-sample; zero by construction except O seats, races not fitted, and races whose base-count share and deciding-count winner disagree: M 0, N 0; |c_j| over 35 fitted races mean 0.167, largest 0.745, RMS 0.230; not fitted: none; the races whose call c_j reverses: 2024 AZ cl1, 2024 MI cl1, 2024 NV cl1, 2024 PA cl1, 2024 WI cl1
  c_j: 2024 AZ cl1 -0.232; 2024 CA cl1 +0.092; 2024 CA cl1 (unexpired) +0.097; 2024 CT cl1 -0.211; 2024 DE cl1 -0.136; 2024 FL cl1 +0.027; 2024 HI cl1 -0.070; 2024 IN cl1 +0.056; 2024 MA cl1 -0.130; 2024 MD cl1 +0.263; 2024 ME cl1 -0.232; 2024 MI cl1 -0.105; 2024 MN cl1 -0.057; 2024 MO cl1 -0.070; 2024 MS cl1 -0.116; 2024 MT cl1 -0.012; 2024 ND cl1 +0.230; 2024 NE cl1 (final_none_top stands for DEM) -0.514; 2024 NJ cl1 -0.197; 2024 NM cl1 +0.126; 2024 NV cl1 -0.161; 2024 NY cl1 +0.085; 2024 OH cl1 -0.019; 2024 PA cl1 +0.041; 2024 RI cl1 -0.174; 2024 TN cl1 +0.167; 2024 TX cl1 -0.110; 2024 UT cl1 -0.302; 2024 VA cl1 -0.084; 2024 VT cl1 -0.046; 2024 WA cl1 -0.267; 2024 WI cl1 -0.031; 2024 WV cl1 +0.745; 2024 WY cl1 +0.050; 2024 NE cl2 (unexpired) -0.576
the play bases' substitutions: 2016 CA cl3 x 0.3519 by F-H: the House CA 2016, side agrees with the base winner's; 2018 CA cl1 x 0.3316 by F-H: the House CA 2018, side agrees with the base winner's; 2020 AR cl2 x 0.7148 by F-H: the House AR 2020, side agrees with the base winner's; 2022 UT cl3 x 0.6623 by F-H: the House UT 2022, side agrees with the base winner's
step 6 - S8: the identity over 137 bases (D's and the play bases read with them) holds; specials whose previous race is before the catalog's window, not read: 2018 MN cl2 (unexpired), 2018 MS cl2 (unexpired)
reading (i) D = {K1}: (a) - K1 33 seats M (a) 4 (b) 5 N (a) +4 (b) +3 (one cycle: compared less S2) - CHANGES THE OUTCOME
reading (ii) the specials in the count: S1 - K1 34 seats M (a) 4 (b) 5 N (a) +4 (b) +3, K3 35 seats M (a) 5 (b) 4 N (a) +5 (b) +4 - the same
reading (iii) targets with a side absent from the general's ballot out: S1 - K1 32 seats M (a) 4 (b) 5 N (a) +4 (b) +3, K3 33 seats M (a) 5 (b) 4 N (a) +5 (b) +4 - the same
reading (iv) a base with no two-sided count at 1 or 0 by its winner: S1 - K1 33 seats M (a) 4 (b) 5 N (a) +4 (b) +3, K3 34 seats M (a) 5 (b) 4 N (a) +5 (b) +4 - the same
reading (v) seats whose base has no two-sided count out: S1 - K1 32 seats M (a) 4 (b) 5 N (a) +4 (b) +3, K3 33 seats M (a) 5 (b) 4 N (a) +5 (b) +4 - the same
reading (vi) each side's strongest candidate alone in every base count: S1 - K1 33 seats M (a) 4 (b) 5 N (a) +4 (b) +3, K3 34 seats M (a) 5 (b) 4 N (a) +5 (b) +4 - the same
reading (vii) V_dc, the swing on the deciding count - PRINTED ONLY (B7 answered, G1): S1 - K1 33 seats M (a) 4 (b) 5 N (a) +4 (b) +3, K3 34 seats M (a) 5 (b) 4 N (a) +5 (b) +4 (printed only - compared with nothing)
reading (viii) (a) by R-US14 (b), proportional - PRINTED ONLY (G2): S1 - K1 33 seats M (a) 4 (b) 5 N (a) +4 (b) +3, K3 34 seats M (a) 5 (b) 4 N (a) +5 (b) +4 (printed only - compared with nothing)
reading (ix) (a) by R-US14 (c), plain additive - PRINTED ONLY (G2): S1 - K1 33 seats M (a) 4 (b) 5 N (a) +4 (b) +3, K3 34 seats M (a) 5 (b) 4 N (a) +5 (b) +4 (printed only - compared with nothing)
reading (x) (a) at a midterm re-derived: S1 - K1 33 seats M (a) 4 (b) 5 N (a) +4 (b) +3, K3 34 seats M (a) 5 (b) 4 N (a) +5 (b) +4 - the same
reading (xi) Sinema an independent of no side - PRINTED ONLY (G3): S1 - K1 33 seats M (a) 4 (b) 5 N (a) +4 (b) +3, K3 34 seats M (a) 5 (b) 4 N (a) +5 (b) +4 (printed only - compared with nothing)
THE OUTCOME (the run over D): S1 + S4 [(i)]
THE VERDICT: R-US19's rule does not decide: S1 (K1 M (a) 4, (b) 5; K3 M (a) 5, (b) 4; listed under S1: control both ways - (a)'s |N| above (b)'s in every cycle True, (b)'s above (a)'s False: K1 |N| (a) 4, (b) 3; K3 |N| (a) 5, (b) 4); S4 ((i) gives (a) - K1 M (a) 4, (b) 5; K1 |N| (a) 4, (b) 3; the run over D gives S1) - asked.
the decisive digest: 1ede3f625179cddfd3e1d4ea500588c774e5c046033032131db4c55cccc466ed
control K1 (a) on 2025-01-03: REP caucus 57, DEM caucus 43 - REP holds 51; cloture's 60 neither; against [SEN-DIV] the 119th D 45 R 53 I 2 (from 2025-01-21); the roster that day REP 52 DEM 47 (the 119th Congress opens)
control K1 (b) on 2025-01-03: REP caucus 56, DEM caucus 44 - REP holds 51; cloture's 60 neither; against [SEN-DIV] the 119th D 45 R 53 I 2 (from 2025-01-21); the roster that day REP 52 DEM 47 (the 119th Congress opens)
control K3 (a) on 2023-01-03: REP caucus 54, DEM caucus 46 - REP holds 51; cloture's 60 neither; against [SEN-DIV] the 118th D 47 R 49 I 4 (from 2024-06-05); the roster that day REP 49 DEM 51 (the 118th Congress opens)
control K3 (b) on 2023-01-03: REP caucus 53, DEM caucus 47 - REP holds 51; cloture's 60 neither; against [SEN-DIV] the 118th D 47 R 49 I 4 (from 2024-06-05); the roster that day REP 49 DEM 51 (the 118th Congress opens)
K1: the mean absolute two-party miss over 32 two-sided targets (not neutral - each method's own scale) (a) 3.17 pp, (b) 3.45 pp; no change M 4, N -4
K3: the mean absolute two-party miss over 33 two-sided targets (not neutral - each method's own scale) (a) 4.24 pp, (b) 4.60 pp; no change M 1, N +1
K1: (a-record), the state's own presidential share of record at 2024: M 4, N +4
K3: (a-record) - no presidential election at 2022
K1: (a) at the House V (PROVISIONAL: 2024's national REP two-party share set to V(2024), its others as recorded): M 4, N +4
(a) at a midterm on 2018's Class I (33 races) from 2016's derivation: M 4, N +2
K3: (x)'s variant from 2020's record rows: M 4, N +4
K1: (b) proportional (= logit): M 5, N +3
K3: (b) proportional (= logit): M 4, N +4
K3: the hybrid-midterm row - an illustration; not measured as play runs it (each seat's (a) share at its base year plus V(2022) - V(base year)): M 4, N 0
K1: the holder on 3 Jan 2025 beside the truth, not a reading - differs at WV: the truth R, the holder on 3 Jan 2025 none (vacant)
the game's range 2024-11-05 (a) (34 seats called, holdovers REP 38 DEM 28; PROVISIONAL): the REP caucus at V = 40% 49, 42% 49, 44% 50, 46% 51, 48% 53, 50% 57, 52% 58, 54% 61, 56% 62, 58% 65, 60% 67
the game's range 2024-11-05 (b) (34 seats called, holdovers REP 38 DEM 28; PROVISIONAL): the REP caucus at V = 40% 43, 42% 45, 44% 47, 46% 49, 48% 52, 50% 55, 52% 57, 54% 60, 56% 61, 58% 63, 60% 66
the game's range 2026-11-03 (a) (35 seats called - with the specials FL cl3, OH cl3, holdovers REP 31 DEM 34; PROVISIONAL): the REP caucus at V = 40% 44, 42% 45, 44% 47, 46% 50, 48% 52, 50% 54, 52% 55, 54% 59, 56% 60, 58% 64, 60% 65
the game's range 2026-11-03 (b) (35 seats called - with the specials FL cl3, OH cl3, holdovers REP 31 DEM 34; PROVISIONAL): the REP caucus at V = 40% 42, 42% 43, 44% 50, 46% 51, 48% 52, 50% 55, 52% 57, 54% 58, 56% 59, 58% 63, 60% 64
the exceptions named: 2016 CA cl3: two of one party, no two-sided count - a fallback; 2016 CT cl3: fusion; 2016 LA cl3: a runoff; 2016 NY cl3: fusion; 2016 SC cl3: fusion; 2018 AZ cl1: an unnamed entry; 2018 CA cl1: two of one party, no two-sided count - a fallback; 2018 CT cl1: fusion; 2018 ME cl1: an independent winner; 2018 MN cl2 (unexpired): a special; 2018 MS cl2 (unexpired): a special, a runoff, its finalists' marks read off; 2018 NY cl1: fusion; 2018 VT cl1: no Democrat, an independent winner; 2020 GA cl3 (unexpired): a runoff; 2022 AK cl3: a ranked count, two of one party; 2022 CA cl3 (unexpired): a special; 2022 CT cl3: fusion; 2022 GA cl3: a runoff; 2022 NY cl3: fusion; 2022 OK cl2 (unexpired): a special; 2022 UT cl3: no Democrat, no two-sided count; 2024 CA cl1 (unexpired): a special; 2024 CT cl1: fusion; 2024 ME cl1: an independent winner; 2024 NE cl1: no Democrat, no two-sided count; 2024 NE cl2 (unexpired): a special; 2024 NY cl1: fusion; 2024 VT cl1: no Democrat, an independent winner; California's Class I contest of 2024 never counted (one seat, two contests); West Virginia's Class I oath after 3 Jan 2025 (the holder line); Ohio 2018's FEC difference (senate_record.md)
```
<!-- END GENERATED -->

## The midterm - R-US20

R-US20's rule (`docs/specs/USA_STAGE_PLAN.md`, its fenced block, preregistered before the instrument's first run) run by `UsMidtermCheck`: the record's midterms and presidential years by government type (`docs/generated/US_MIDTERM_RECORD.md`, written by `Tools/us_midterm_record.pl`) beside the model's 2026 midterm - a US world from the eve of 5 Nov 2024 stepped to 3 Nov 2026, US-5's House history with the economic vote applied in the instrument only (none, the presidential row, Table 9.1's US legislative cell) - the outcome and the verdict line. Generated, never typed - a stale block fails the bars.

<!-- GENERATED by PoliSim.EditorTools.UsMidtermCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: 6a3eb8c88cb2464dfab553832fdd9dd63d323c3387fd5a2c213010b6cf06aa2f -->
### The record - the president's party in the House (`docs/generated/US_MIDTERM_RECORD.md`)

Each set's least and greatest, recomputed from `ElectionsData/usa/house_midterms.csv`, the government type read over both chambers sitting on the day ([HH-DIV]'s House, [SEN-DIV]'s Senate; R-US20's block as amended under I4): the seat change by [HH-DIV], the two-party vote swing by the Clerk's recapitulations where they carry text (1998 on), and the SECONDARY compilation's (Vital Statistics on Congress, Table 2-2), printed beside and never read by the rule. Points; n counts the elections each column reads.

| set | n | seat change: least / greatest | n (the Clerk) | vote swing (the Clerk): least / greatest | n (SECONDARY) | vote swing (SECONDARY): least / greatest |
|---|---:|---:|---:|---:|---:|---:|
| every midterm, 1946-2022 | 20 | -64 / +8 | 6 | -8.97 / +2.26 | 20 | -9 / +2.3 |
| unified midterms | 11 | -64 / -6 | 4 | -8.97 / -2.95 | 11 | -9 / -2.3 |
| divided midterms | 9 | -50 / +8 | 2 | -3.71 / +2.26 | 9 | -5.8 / +2.3 |
| every presidential year, 1948-2024 | 20 | -35 / +75 | 7 | -1.42 / +4.12 | 20 | -3.2 / +7.9 |
| unified presidential years | 5 | -35 / +37 | 1 | -1.07 / -1.07 | 5 | -3.2 / +4.8 |
| divided presidential years | 15 | -24 / +75 | 6 | -1.42 / +4.12 | 15 | -1.4 / +7.9 |

**R-US20's band** - the rule's record, the midterms 2002, 2006, 2010, 2014, 2018, 2022 by the Clerk, both government types together: [L, U] = [-8.97, +2.26] points (full precision: L = -8.974131, U = 2.256382). Every earlier midterm's vote swing is BILLED - the Clerk's statistics of 1944-1996 are page images.

### The model's midterm - 3 Nov 2026

- **The world**: the epoch 2024-11-04 (the eve of 5 Nov 2024), seed 777, the USA the player's country with DEM its party (the AI governs the president's party through the term), no decision on any turn, stepped one day at a time in the controller's order to 2026-11-03 (729 days). The 2024 election's House from 2025-01-03, Trump (REP) from 2025-01-20 - elected by the world's own count of 5 Nov 2024 (US-8), as the record elected him. On the polling day: the president's party REP, the House's largest party REP, the Senate's majority REP (both chambers, R-US20's block as amended under I4) - **unified**.
- **The poll**: US-5's House history (R-US15 (a), G2) - held at the four countries' median spread (σ 1.25, τ 6.00), fitted to the 2024 House split (REP 51.32 %); the loyalty 2024 against 2022, REP 99.48 DEM 99.80; REP 51.32 % before the economic vote - the identity holds (within 0.0e+0 of the prior).
- **The economic vote**: term index 43.4 - took office 2025-01-20, judged 2026-11-03: unemployment 5.2 % (2025-01) -> 6.0 % (2026-09); inflation 2.3 % (2025-01) -> 1.6 % (2026-09); 0.131833 units of deterioration on the book's scale.

| economic vote on the president's party (REP) | its cell | REP's shift (pp) | REP's two-party share | the model's midterm m (pp) |
|---|---:|---:|---:|---:|
| none - the history alone | - | +0.00 | 51.32 % | +0.00 |
| the presidential row (the live constants; US-5's House history in 2024) | -0.10 (unified) | -1.32 | 50.00 % | -1.32 |
| **Table 9.1's US legislative cell [draft] - R-US20 (a)** | -0.016 (0.030) n = 2 | -0.21 | 51.11 % | **-0.21** |
| the legislative cell less its parenthesis | -0.046 | -0.61 | 50.71 % | -0.61 |
| the legislative cell plus its parenthesis | +0.014 | +0.18 | 51.50 % | +0.18 |

- **The model's seat change**: not computed here - R-US18 is RULED (§856) by (b), and R-US20 (b) replaces this m with a fitted term; the seat change of that term by (b) is US-27's.
- **The 2026 midterm's own result**: BILLED until the Clerk's 2026 statistics are saved (expected 2027); then it is printed beside m, never read by the rule.

**By R-US20's rule, code recommends (a): the model's midterm -0.21 lies inside the record's range [-8.97, +2.26]** (full precision: m = -0.210932, L = -8.974131, U = 2.256382; unified; the legislative cell -0.016). R-US20 is asked with this table.

**The rule's reach** (printed, never deciding): the poll is fitted to the 2024 House split the midterm is measured from, so m without the economic vote is the fit's residual (+0.00); the economic vote moves m by 100 x the cell x the units of deterioration, which the term index bounds to [-1, 1]. So |m| can never exceed 1.60 points (the cells: unified -0.016, divided +0.012), and the band's nearer edge is 2.26 points from zero: under this rule NO world that holds the record can give (b) - the verdict follows from the band and Table 9.1's cells, not from the world's economy. R-US20 is asked with this stated.

The ruling record: RULED (§856) - (b): I4 - (b), a FITTED midterm term on the president's party from the record since 1946, n and its interval printed (US-27 builds it); revisited against (c) once approval reaches the vote; the both-chambers amendment ruled yes.
<!-- END GENERATED -->

## The prediction - US-6

The game's US poll and E1's factors (`docs/specs/USA_STAGE_PLAN.md`, US-6; R-US2 (a) and R-US15 (a), ruled). The poll is R-US15's ruled
choice composed by the live chain (`NationalElection.TryPredictShares`); the presidential candidates carry it by E1's factors, fitted once to the
FEC's national result against the game's poll on the reference world and refit on every bar by `PoliSim.EditorTools.UsPresidentialFactorCheck`,
which fails where a stored factor is no longer the fit. Since US-8 the count reads it on the polling day (*The count - US-8*, below).

| part | what it does | basis |
|---|---|---|
| The poll | the House national vote: the prior the House election whose chamber the world seats, the loyalty that election against the one before, each party's share of all votes cast by the Clerk's recapitulation - read from the generated catalog (`PartySystems.TryHistory`) | RULED (R-US15 (a), G2) |
| The electorate | held, not fitted: σ and τ each the median of the four fitted countries', the mean on the DEM-REP segment where the history's last House election falls - solved at run time (`PartySystems.UsElectorate`) and held equal to US-5's fit at the four's median | RULED (R-US15 (ii), G2) |
| Its vintage | the chamber seated at the world's epoch - the vintage the history reads; a game from the start reads the start's for its whole run, the record's later chamber moving the seats and not the history (the standing history gap, §767); each vintage's electorate in the readings | DECLARED (US-6) |
| The reference world | `PresidentialReferenceWorld`, generalised by country: the epoch set to the eve of the general election of record before the world is created, as a new game builds one, and one day stepped by the game's loop; never a start world with its clock moved | E1; the s772 review's fix |
| The factors | each nominee's share of the FEC's national total ÷ the party's poll on that world; a later field at factor 1.0 | E1 [FITTED] (`PresidencyOfRecord.CandidatesOf`); F4 - the world that follows history |
| The field | every column of the FEC's workbook: the two nominees NARA names stand for their parties at their parties' positions; every other column - candidates, None of These Candidates, the scattered write-ins - at its national share of record as base, standing only where on the ballot of record: the jurisdictions whose cell of the workbook is roman, its note printing write-in votes in italics (`PresidencyOfRecord.StandingOfRecord`; `Tools/us_ballot_standing.pl` prints each column's); the scattered write-ins, on no ballot, where the workbook reports them. The national prediction reads each share whole, so the standing moves no factor; the count's states spread it (*The count - US-8*) | R-US2 (a); RULED - Elias's ruling J3 (2026-10-19, §870), reversing US-6's national standing (US-6c); the scattered write-ins' standing DECLARED |
| The naming | the nominee by name from the nomination's date of record, "the Democratic/Republican nominee" before it: the DNC's date sourced; the Republican date BILLED, so "the Republican nominee" on every day before the count (a table no screen reads yet); from the count on, US-8's count names each nominee by NARA's name for the ticket (the contest, the oath's government, the Parliament page's president row) - the asymmetry and that naming put to Elias as one question, NARA's certificates the plan's stand-in | R-US2 (a); `ElectionsData/usa/records_by_date.md` §2a, G13 |

### The readings (US-6)

<!-- GENERATED by PoliSim.EditorTools.UsPresidentialFactorCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: c160cdbad70d3a3abef542eb7ced7a6a816bb7e4438f53992a65fa2c20cc0cfc -->
### The poll (R-US15, ruled)

The USA's poll is the House national vote: the prior is the House election whose chamber the world seats at its epoch, the loyalty that election against the one before - each party's own ballot line, its share of all votes cast by the Clerk of the House's recapitulation; the presidential candidates carry it by E1's factors, the electorate held at the four fitted countries' median spread. The electorate is held, not fitted - σ 1.25 and τ 6.00, each the median of Sweden, Germany, Poland, Italy's (`PartySystems.UsSpreadFrom`), wEcon 0.65 - its mean on the segment from DEM's point to REP's where the seated history's last House election falls (0 at DEM, 1 at REP), solved for each chamber of record:

| chamber of record (vintage) | history read | T-1 split (R) | loyalty R / D | place | μ econ, μ soc |
|---|---|---:|---:|---:|---|
| Usa2020 (elected 2020) | House 2020 against 2018 | 48.44 % | 93.7 / 95.0 | 0.4898 | 5.934, 5.295 |
| Usa2022 (elected 2022) | House 2022 against 2020 | 51.40 % | 94.5 / 94.1 | 0.5096 | 6.023, 5.411 |
| Usa2024 (elected 2024) | House 2024 against 2022 | 51.32 % | 99.5 / 99.8 | 0.5090 | 6.021, 5.408 |
| Usa2026 (elected 2026 - read as the latest sourced) | House 2024 against 2022 | 51.32 % | 99.5 / 99.8 | 0.5090 | 6.021, 5.408 |

A game from the USA's start (12 Mar 2024) seats Usa2022 and its poll reads House 2022 against 2020 for the whole run: the record seats Usa2024 on 3 Jan 2025, which moves the chamber and not the history - the history is the chamber's seated at the world's epoch (the standing history gap, §767; the reading DECLARED with US-6, and held: the reference world below, stepped on by the game's loop to 3 Jan 2025, seats Usa2024 and its poll still reads House 2022 against 2020). The other rows say what a world opened on another epoch would read.

### E1's factors - by construction

**The reference world** (`PresidentialReferenceWorld`, generalised by country): the epoch set to 4 Nov 2024, the eve of the general election of 5 Nov 2024, before the world is created - as a new game builds one - and one day stepped by the game's loop. It seats Joseph R. Biden Jr. (DEM), president and the House elected in 2022; its poll is REP 48.90 %, DEM 51.10 %, the record over the term shifting DEM +2.50 pp (term index 70.8 - took office 2021-01-20, judged 2024-11-05: unemployment 6.4 % (2021-01) -> 4.1 % (2024-10); inflation 1.4 % (2021-01) -> 2.4 % (2024-09)).

**The factors** (R-US2 (a): the November nominees of record carry them from the start): each nominee's share of the FEC's national total ÷ the party's poll on that world - Donald J. Trump 1.018391, Kamala D. Harris 0.945620. Every other column of the FEC's workbook carries its national share of record as its base, standing only where on the ballot of record (below): Stein 0.56 %, Kennedy 0.49 %, Oliver 0.42 %, the rest 0.42 % (every other candidate, None of These Candidates and the scattered write-ins). A later field starts at factor 1.0 (E1).

**The factors reproduce the 2024 national shares on the reference world by construction**: they are fitted there, so the field's largest miss on it (0.0000 pp, the table below) shows only that the stored factors are still the fit. The acceptance of the US count rests elsewhere - on the states, US-4's layer moving 2020's states to 2024's national shares (*The readings*, above), and on the out-of-sample case, US-5's 2020 predicted from 2016 (*The readings (US-5)*, above).

**E1's premise for the USA** (RULED, F4): a game played from 12 Mar 2024 reaches 5 Nov 2024 on its own economy and its own seed vintage - the economy is seeded to mid-2026 data, 2 year(s) 4 month(s) after this start - one seed vintage per country, the offset stated (§621) - and on its own play, so its poll on the day is its own and its count is not the record's.

| candidate | party | factor | share of record (FEC) | the field on the reference world | miss (pp) |
|---|---|---:|---:|---:|---:|
| Ayyadurai | - | base | 0.0183 % | 0.0183 % | +0.0000 |
| Bowman | - | base | 0.0038 % | 0.0038 % | +0.0000 |
| De La Cruz | - | base | 0.1070 % | 0.1070 % | +0.0000 |
| Duncan | - | base | 0.0082 % | 0.0082 % | +0.0000 |
| Ebke | - | base | 0.0006 % | 0.0006 % | +0.0000 |
| Everylove | - | base | 0.0017 % | 0.0017 % | +0.0000 |
| Fruit | - | base | 0.0027 % | 0.0027 % | +0.0000 |
| Garrity | - | base | 0.0034 % | 0.0034 % | +0.0000 |
| Kamala D. Harris | DEM | 0.945620 | 48.3242 % | 48.3242 % | +0.0000 |
| Huber | - | base | 0.0014 % | 0.0014 % | +0.0000 |
| Kennedy | - | base | 0.4872 % | 0.4872 % | +0.0000 |
| Kishore | - | base | 0.0030 % | 0.0030 % | +0.0000 |
| Oliver | - | base | 0.4188 % | 0.4188 % | +0.0000 |
| Preston | - | base | 0.0018 % | 0.0018 % | +0.0000 |
| Skousen | - | base | 0.0082 % | 0.0082 % | +0.0000 |
| Sonski | - | base | 0.0283 % | 0.0283 % | +0.0000 |
| Stein | - | base | 0.5553 % | 0.5553 % | +0.0000 |
| Stodden | - | base | 0.0002 % | 0.0002 % | +0.0000 |
| Supreme | - | base | 0.0006 % | 0.0006 % | +0.0000 |
| Terry | - | base | 0.0266 % | 0.0266 % | +0.0000 |
| Donald J. Trump | REP | 1.018391 | 49.7961 % | 49.7961 % | +0.0000 |
| Wells | - | base | 0.0002 % | 0.0002 % | +0.0000 |
| West | - | base | 0.0532 % | 0.0532 % | +0.0000 |
| Wood | - | base | 0.0007 % | 0.0007 % | +0.0000 |
| None Of These Candidates | - | base | 0.0126 % | 0.0126 % | +0.0000 |
| Write-Ins (Scattered) | - | base | 0.1355 % | 0.1355 % | +0.0000 |

### The field of record and its naming (R-US2, ruled; R-US23, ruled even-handed)

The nominees are named from their conventions' dates of record, and neither before both are dated (R-US23, ruled even-handed): "the Democratic/Republican nominee" until then; the withdrawal is not replayed. The Democratic convention's date is 6 Aug 2024 - the DNC's release certifying the nominees (`ElectionsData/usa/records_by_date.md` §2a, its page held to its SHA-256 and its date). The Republican convention's date is 15 Jul 2024 (its saved page held to its SHA-256 and its date), so the naming table (`PresidencyOfRecord.NomineeName`, which no screen reads yet) names Kamala D. Harris and Donald J. Trump from 6 Aug 2024. From the count on, the game's own count (US-8) names each nominee by NARA's name for the ticket (the contest, the oath's government, the Parliament page's president row).

**Every other candidate stands only where on the ballot of record** (Elias's ruling J3, 2026-10-19, reversing US-6's national standing; built as US-6c): a column stands in the jurisdictions whose ballot listed it - the FEC's workbook prints write-in votes in italics, by its own note, so a roman cell is a listing (`PresidencyOfRecord.StandingOfRecord`; `Tools/us_ballot_standing.pl` prints each column's) - Stein in 37, Kennedy in 31, Oliver in 47 of the 51 jurisdictions; Write-Ins (Scattered), on no ballot, stands where the workbook reports it. The prediction is national and reads each column's national share whole, its write-in votes included, so the factors are the same as with national standing; the count's states spread each column's share over where it stands, in the record's own pattern there (*The count - US-8*).
<!-- END GENERATED -->

## The campaign geography - R-US16 (US-9)

What "fought state by state" means (`docs/specs/USA_STAGE_PLAN.md`, R-US16): **(a)** national in effect, as Germany's Länder are today, Campaign HQ saying so;
**(b)** for the USA, a per-state count with the ground game on the derived shares, plus a DECLARED targeting rule for the AI (electors × closeness on
the last published poll); **(c)** (b) for every staged country. The instrument is `PoliSim.EditorTools.UsCampaignGeographyCheck` - an Editor harness
only: it stages a US campaign on the jurisdictions in the Editor and wires nothing. It measures the plan's three things (US-9) - (a) where the AI's
local acts land, (b) that a local act reaches a state only through the national total, (c) how far W-D1's per-region count would move one state under
a maximal ground game, against the 2024 margins of record - and sizes R-US16's options beside them: the declared rule's picks, the scripted ground game
and the AI's under the per-state count, W-D1's declared noise, and the same count over every staged country (R-US16 (c)). Its parameters are declared
in the instrument before its first run and none is tuned on a result. The record is US-9's section of `COMPLETED.md`.

| part | what it does | basis |
|---|---|---|
| The jurisdictions | every jurisdiction of US-3's catalog; a jurisdiction's audience (what a local act can reach) its votes cast in the prior election, as the plan names it; its prior shares that election's - the two nominees, and all others pooled | SOURCED (§786); the plan's US-9 |
| The electorate | the University of Florida Election Lab's 2024 VEP per state (`ElectionsData/usa/raw/returns/`, its digest held to the folder's list) - who a ground game can mobilise. US-30's reading of the electorate is unruled; this stands in for it in the Editor only | SOURCED (an academic estimate); DECLARED for the harness |
| The vote model | US-6's: `NationalElection.TryCompatibility` at the USA's start - the compatibility, the prior and the loyalty a US game's prediction is made of | RULED (R-US15 (a), G2) |
| The casts and the staging | each party cast by `CampaignCasts.Of` (§695's rule); the chest, the volunteers, the offices, the staff and the price table are `LiveCampaignSetup`'s - the model's one table, in kronor; a US table (the war chest in dollars) is BILLED, as Germany's is | §695; [AUTHORED-DRAFT] |
| The salience | Gallup's most-important-problem table of July 2026, the two issues the model has a slot for - dated after the campaign it stands in for; the geography measured here does not read it | SOURCED; DECLARED |
| The runs | the whole campaign proper on the USA's calendar, one seed, every party an AI unless scripted. The idle run has no act, no debate and no story, and is the prediction; then the rule's cast, and each of §32's personalities cast onto both parties (§695 casts no chaotic party, so a fixture fields them all, as `CampaignAiHarness`'s does) | DECLARED |
| The national shares a run implies | E1's construction - each nominee's support the party's share times its factor, every other column of the field at its share of record (`PresidencyOfRecord.CandidatesOf`) - with the idle run standing for the reference world: the idle run gives 2024's national shares exactly, as US-6's factors do on its reference world, and a run moves them by its own change to each party's share | E1; DECLARED (US-9) |
| The states | R-US14 (a): `RegionalVoteModel.RegionalSharesByUniformSwing` on the prior election's jurisdictions - the mirror of the call `NationalElection.DeriveRegional` makes, proved equal to it on every staged country's campaign (the runtime derives no US state until US-8); the electors through `ElectoralCollege`, Maine's and Nebraska's districts as US-4 reads them | RULED (R-US14 (a)); §689 |
| (a) | each run's local acts by jurisdiction, against the largest prior-election audiences, the closest prior-election states (as many of each as `CampaignAi.LocalCandidateRegions`) and the declared rule's picks on the run's last published poll | the plan's US-9 |
| (b) | a scripted party - the player's stand-in - works one jurisdiction with the program the instrument declares (its one office there and door-to-door every day), against the same campaign with it idle; the jurisdiction's derived move held against the national move and against every other jurisdiction's | the plan's US-9 |
| (c) | W-D1's per-region count (`ElectionDay.Count`), each jurisdiction's preferences its derived shares, one base turnout (it cancels from every share; no rate clamps), no noise: the maximal ground game - one party's mobilisation at the curve's ceiling in one jurisdiction - against the 2024 margins of record; the scripted programs' and the AI's own ground games; W-D1's declared regional noise over replays | the plan's US-9; W-D1 |
| R-US16 (b)'s rule | a jurisdiction's electors × §25's swing index (`SwingRegions.Index`) of its margin derived from the last published poll | DECLARED for this measurement - the rule US-31 would build |
| R-US16 (c) | every country `LiveCampaignSetup.TryFor` stages, its campaign run whole on the vote model and counted the same way: the ground game's effect, and W-D1's frame alone, in shares and in seats | DECLARED |
| Not modelled | a minor candidate's campaign (every minor column stands nationally at its share of record); ballot access; faithless electors; Maine's ranked-choice count | DECLARED |

### The readings (US-9)

<!-- GENERATED by PoliSim.EditorTools.UsCampaignGeographyCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: 5a45d3927849ab4ec900900a339d45b57103f9375cf2ddc8dc72c5a0eaae38f5 -->
### The staging

A US campaign in the Editor only: REP and DEM on the 51 jurisdictions - audience each one's 2020 votes cast (158,429,631 together), electorate UF's 2024 VEP (240,748,332) - staged on US-6's vote model at the USA's start (12 Mar 2024), the campaign 10 Sep to 4 Nov 2024; casts by `CampaignCasts.Of` (REP Professional, DEM Grassroots); the model's one price table and its [AUTHORED-DRAFT] staging (`LiveCampaignSetup`'s chest, volunteers, offices and staff); salience Gallup's July 2026 (immigration 0.12, the economy 0.11 - dated after the campaign); both parties on every ballot. The idle run - no act, no debate, no story - is the prediction (REP 51.40 %, DEM 48.60 %); by E1's construction its national shares are 2024's exactly (D 48.32 %, R 49.80 %, others 1.88 %), and derived to the jurisdictions by R-US14 (a) they are US-4's row: electors R 312 – D 226, 0 states called wrong.

### (a) Where the AI's local acts land

The AI weighs a local act only in its candidate regions - the 4 largest audiences (`CampaignAi.LocalCandidateRegions`, [AUTHORED-DRAFT]), and, for a reactive personality, the regions it sees an opponent working. Each run below is the whole campaign (10 Sep to 4 Nov 2024, seed 777), every party an AI; its local acts (rally, town hall, door-to-door) counted by jurisdiction. "The rule's 4" are the jurisdictions R-US16 (b)'s declared rule would pick on that run's last published poll (below).

The 4 largest 2020 audiences: CA, TX, FL, NY. The 4 closest 2020 states: GA, AZ, WI, PA.

| run | casts | local acts | in the 4 largest | in the 4 closest | in the rule's 4 | jurisdictions reached | most acts | offices (in the 4 largest) | compatibility at the close, before the clamp |
|---|---|---:|---:|---:|---:|---:|---|---:|---|
| the rule's cast (the game's) | REP Professional, DEM Grassroots | 0 | - | - | - | 0 | - | 9 (7) | REP past the ceiling (100), DEM 98.15 |
| both Professional | REP Professional, DEM Professional | 0 | - | - | - | 0 | - | 6 (6) | REP past the ceiling (100), DEM 98.15 |
| both Populist | REP Populist, DEM Populist | 0 | - | - | - | 0 | - | 8 (8) | REP past the ceiling (100), DEM 98.15 |
| both Establishment | REP Establishment, DEM Establishment | 0 | - | - | - | 0 | - | 4 (4) | REP past the ceiling (100), DEM 98.15 |
| both Grassroots | REP Grassroots, DEM Grassroots | 1 | 100.0 % | 0.0 % | 0.0 % | 1 | CA 1 | 12 (8) | REP past the ceiling (100), DEM 98.15 |
| both Chaotic | REP Chaotic, DEM Chaotic | 134 | 100.0 % | 0.0 % | 43.3 % | 4 | NY 39, CA 37, FL 31, TX 27 | 2 (2) | REP past the ceiling (100), DEM 98.15 |

**R-US16 (b)'s targeting rule** (DECLARED for this measurement: a jurisdiction's electors × §25's swing index - `SwingRegions.Index`, [AUTHORED-DRAFT] full scale - of its margin derived from the last published poll's national shares, by E1's construction and R-US14 (a)):

On the rule's-cast run's last published poll (REP 54.67 %, DEM 45.33 %) the rule's 4 are IL (19 electors, index 77), NY (28 electors, index 46), VA (13 electors, index 89), NJ (14 electors, index 82) - 31.6 % of the rule's whole weight over 25 jurisdictions with any.

### (b) A local act reaches a state only through the national total

A scripted party - the player's stand-in, W-C2's seam - works one jurisdiction: its one office there from day 0 and, every campaign day, 2 DoorToDoor there (15,000 kr and 5 hours each, the model's one price table - a US table is BILLED), its staging otherwise its cast's; the other party is an AI at its cast. Each program is held against the same campaign with the scripted party idle (no office, no act). The jurisdictions worked: GA, the closest 2020 state, and CA, the largest 2020 audience.

| party | works | acts made | its mobilisation there (50 untouched) | its nominee nationally (pp) | that jurisdiction's derived move (pp) | largest departure of any jurisdiction's move from the national (pp) | its margin R − D, (a)'s count: idle → program | (b)'s count: idle → program (of which turnout) | electors R − D, (a): idle → program | (b): idle → program |
|---|---|---:|---:|---:|---:|---:|---|---|---|---|
| REP | GA | 109 | 54.274 | +1.07e-2 | +1.07e-2 | 1.7e-14 | +5.314 → +5.335 | +5.314 → +6.125 (+0.789) | 312 – 226 → 312 – 226 | 312 – 226 → 312 – 226 |
| REP | CA | 109 | 51.326 | -1.30e-2 | -1.30e-2 | 1.7e-14 | -23.615 → -23.641 | -23.626 → -23.410 (+0.231) | 312 – 226 → 312 – 226 | 312 – 226 → 312 – 226 |
| DEM | GA | 112 | 54.380 | +2.90e-2 | +2.90e-2 | 1.7e-14 | +6.017 → +5.959 | +6.017 → +5.156 (-0.803) | 312 – 226 → 312 – 226 | 312 – 226 → 312 – 226 |
| DEM | CA | 112 | 51.359 | +4.88e-3 | +4.88e-3 | 1.7e-14 | -22.912 → -22.922 | -22.897 → -23.159 (-0.237) | 312 – 226 → 312 – 226 | 312 – 226 → 312 – 226 |

Under the count as built ((a)'s columns) the worked jurisdiction moved by what the nation moved to within 5.6e-15 points, and every jurisdiction to within 1.7e-14: the derivation is a function of the national shares and the prior alone (R-US14 (a)'s uniform swing gives every jurisdiction one move, re-solved only where a floor binds), so working a state moves it as much as every state it was not worked in. The ground game's turnout - the mobilisation the program built - reaches no count at all: W-D1's per-region count, its one reader, is wired to nothing. (b)'s columns are that count, the derived shares as its preferences (below).

The national move itself is at most 2.90e-2 points. Since A-LOOP q24 5a the campaign moves the shares by the sourced effects (`CampaignPersuasion`: Kalla & Broockman's CACEs per contact class times each class's coverage of the electorate) and writes no compatibility, so no clamp decides it: the widest campaign effect on any party in the 6 runs of this table is 0.234 points, every one inside its sourced bound. The compatibility entering the preference model is the staging's own; in 0 of the 6 runs both parties' sat at the scale's ceiling (`ElectionScales.Max` 100; the least 98.15).

### (c) W-D1's per-state count under a maximal ground game

The count (`ElectionDay.Count`, no noise): each jurisdiction's preferences its derived shares on the idle map (2024's national shares from 2020's jurisdictions, R-US14 (a) - US-4's row), its electorate UF's VEP, one base turnout (0.6581, the votes cast over the electorate - it cancels from every share; no rate clamps), the other attributes neutral. **The maximal ground game** is one party's mobilisation at the curve's ceiling in one jurisdiction (`GotvModel.Mobilization` at 100, its supporters' turnout × 1.20 - `TurnoutModel.MobilizationSpan`, [AUTHORED-DRAFT]): no budget buys more.

**The ceiling moves one jurisdiction's margin by 2.90 to 9.00 points** (R − D, either party's, over every jurisdiction), and could flip 13 of the idle map's calls - 139 electors: AZ, CO, GA, ME, MI, MN, NV, NH, NM, NC, PA, VA, WI. The 4 closest 2024 states of record: WI +0.86 (the ceiling's move for the trailing party -8.95), MI +1.41 (the ceiling's move for the trailing party -8.97), PA +1.70 (the ceiling's move for the trailing party -8.98), GA +2.19 (the ceiling's move for the trailing party -8.99).

**The staged ground game** (the scripted programs above, the model's one price table): its turnout move in the jurisdiction it worked - REP in GA +0.789; REP in CA +0.231; DEM in GA -0.803; DEM in CA -0.237.

**The AI's own ground game** (the rule's-cast run): its national result REP 51.37 %, DEM 48.63 % - D 48.35 %, R 49.77 % by E1's construction; under the per-state count its widest turnout move is IL's, -0.035 points of margin; electors R − D 312 – 226 under (a)'s count, 312 – 226 under (b)'s.

**W-D1's declared regional noise** (σ 1.2 points a party a jurisdiction, `RegionalAggregation.RegionalNoiseSigmaPp`; 400 replays on the idle map, seed 777, no ground game): 8 jurisdictions change hands in at least one replay; the map's 4 closest - MN (margin -1.18) 23.3 %, NH (margin -1.42) 18.3 %, ME (margin -3.14) 4.3 %, MI (margin +3.14) 5.3 %; the presidency changes hands in 0 of 400, no majority in 0.

Each jurisdiction - its electors, its margin of record, the idle map's (R − D, points of the jurisdiction's vote), and the ceiling's move for each party:

| jurisdiction | electors | 2024 of record | the idle map | the ceiling, DEM | the ceiling, REP | the ceiling flips the map's call |
|---|---:|---:|---:|---:|---:|---|
| AL | 9 | +30.47 | +31.39 | -8.27 | +7.89 | - |
| AK | 3 | +13.14 | +15.99 | -8.55 | +8.43 | - |
| AZ | 11 | +5.53 | +5.62 | -8.97 | +8.89 | flips |
| AR | 6 | +30.64 | +33.55 | -7.98 | +7.68 | - |
| CA | 54 | -20.13 | -23.24 | -8.29 | +8.55 | - |
| CO | 10 | -10.98 | -7.57 | -8.77 | +8.85 | flips |
| CT | 7 | -14.50 | -14.15 | -8.69 | +8.87 | - |
| DE | 3 | -14.70 | -13.04 | -8.73 | +8.90 | - |
| DC | 3 | -83.81 | -80.82 | -2.90 | +2.97 | - |
| FL | 30 | +13.10 | +9.29 | -9.00 | +8.87 | - |
| GA | 16 | +2.19 | +5.69 | -8.99 | +8.91 | flips |
| HI | 4 | -23.10 | -23.54 | -8.28 | +8.56 | - |
| ID | 4 | +36.49 | +36.70 | -7.76 | +7.46 | - |
| IL | 19 | -10.90 | -11.06 | -8.75 | +8.89 | - |
| IN | 11 | +18.96 | +22.00 | -8.61 | +8.35 | - |
| IA | 6 | +13.21 | +14.13 | -8.83 | +8.65 | - |
| KS | 6 | +16.12 | +20.56 | -8.63 | +8.40 | - |
| KY | 8 | +30.53 | +31.86 | -8.20 | +7.84 | - |
| LA | 8 | +22.01 | +24.54 | -8.55 | +8.25 | - |
| ME | 4 | -6.94 | -3.14 | -8.82 | +8.86 | flips |
| MD | 10 | -28.54 | -27.28 | -8.07 | +8.35 | - |
| MA | 11 | -25.20 | -27.53 | -8.07 | +8.36 | - |
| MI | 15 | +1.41 | +3.14 | -8.97 | +8.93 | flips |
| MN | 10 | -4.24 | -1.18 | -8.89 | +8.90 | flips |
| MS | 6 | +22.89 | +22.47 | -8.67 | +8.37 | - |
| MO | 10 | +18.41 | +21.32 | -8.66 | +8.40 | - |
| MT | 4 | +19.93 | +22.30 | -8.55 | +8.31 | - |
| NE | 5 | +20.46 | +24.99 | -8.43 | +8.18 | - |
| NV | 6 | +3.10 | +3.54 | -8.91 | +8.87 | flips |
| NH | 4 | -2.78 | -1.42 | -8.92 | +8.93 | flips |
| NJ | 14 | -5.91 | -10.01 | -8.82 | +8.96 | - |
| NM | 5 | -6.00 | -4.86 | -8.85 | +8.91 | flips |
| NY | 28 | -12.59 | -17.20 | -8.59 | +8.82 | - |
| NC | 16 | +3.21 | +7.28 | -8.97 | +8.87 | flips |
| ND | 3 | +36.45 | +39.27 | -7.58 | +7.28 | - |
| OH | 17 | +11.21 | +13.96 | -8.88 | +8.70 | - |
| OK | 7 | +34.26 | +39.01 | -7.70 | +7.33 | - |
| OR | 8 | -14.31 | -10.16 | -8.68 | +8.78 | - |
| PA | 19 | +1.70 | +4.77 | -8.98 | +8.91 | flips |
| RI | 4 | -13.78 | -14.85 | -8.63 | +8.81 | - |
| SC | 9 | +17.87 | +17.61 | -8.80 | +8.57 | - |
| SD | 3 | +29.19 | +32.09 | -8.09 | +7.78 | - |
| TN | 11 | +29.72 | +29.14 | -8.33 | +8.00 | - |
| TX | 40 | +13.68 | +11.51 | -8.92 | +8.77 | - |
| UT | 6 | +21.58 | +26.41 | -8.20 | +8.01 | - |
| VT | 3 | -31.51 | -29.48 | -7.90 | +8.16 | - |
| VA | 13 | -5.77 | -4.19 | -8.89 | +8.94 | flips |
| WA | 12 | -18.22 | -13.28 | -8.59 | +8.72 | - |
| WV | 4 | +41.86 | +44.86 | -7.35 | +6.90 | - |
| WI | 10 | +0.86 | +5.30 | -8.95 | +8.88 | flips |
| WY | 3 | +45.76 | +49.31 | -6.72 | +6.45 | - |

### R-US16 (c): every staged country

Every country `LiveCampaignSetup.TryFor` stages, on the vote model at its own start, its campaign run whole (every party an AI at its cast, seed 777); its regions derived from the run's national shares by `NationalElection.SharesFromCampaign` (the runtime's own derivation, which the mirror equals to the last bit) and counted by W-D1 as above - one base turnout, the staging's votes over its electorate. The run's ground game's effect is the count with the run's mobilisation against the count with none; the count with none against the run's national shares is W-D1's frame alone (one base turnout weighs each region by its electorate, not its votes). Seats by `NationalElection.Run` on each; no tactical layer in either.

| country | regions | the ground game: largest party move (pp) | seats changing hands | W-D1's frame alone: largest party move (pp) | seats changing hands | parties past the compatibility ceiling at the close |
|---|---:|---:|---:|---:|---:|---|
| Sweden | 29 | 0.026 | 0 | 0.062 | 0 | 1 of 8 |
| Germany | 16 | 0.003 | 0 | 0.298 | 2 | 1 of 9 |
<!-- END GENERATED -->

## The count - US-8

The game's US presidential count (`docs/specs/USA_STAGE_PLAN.md`, US-8 - checkpoint 1; R-US3 (a), R-US14 (a), R-US13 (a) and R-US1 (a), ruled). On the
polling day the day loop counts the player's country (`UsPresidentialElection.Count`, a pure function of the country, the day and the poll - R-US5's
row then needs only its gate) and the verdict lands at once; the president-elect takes office at the oath through US-7. The acceptance is
`PoliSim.EditorTools.UsPresidentialElectionDiagnostic`, which writes the readings below and fails where they no longer say what it measures.

| part | what it does | basis |
|---|---|---|
| The day | the Tuesday next after the first Monday in November, every fourth year from the election of record (`WorldClock.TryNextPollingDay`); the electors meet on the first Tuesday after the second Wednesday in December, Congress counts on 6 January, the oath is on 20 January | 3 U.S.C. §21(1), §7, §15(a); the Twentieth Amendment §1 - their saved pages |
| The national vote | the game's poll on the day (the live prediction, the government's record over its term shifting it - the call E1's reference world reads) × E1's factors (US-6); where the election is of record the field of record, every other column at its share of record; later fields unnamed at factor 1.0 | E1 [FITTED]; R-US2 (a) |
| The states | the previous presidential election of record's states, moved to the national vote by §689's normalised uniform swing; Maine's and Nebraska's districts with their state, in two-party terms. Where the election is of record (2024), every other candidate stands only where on the ballot of record - each column's share of the count spread over the jurisdictions that listed it in the record's own pattern there, renormalised to its national share - and the two nominees are swung over what is left on their two-party split of the prior election, each jurisdiction weighing its prior votes cast less that (`ElectoralCollege.FromNationalShares`, the overload with the standing); a later field, with no minor candidate, takes US-4's form, the two nominees and every other candidate together - the derivation US-4's instrument reads its (a) column from | R-US14 (a), RULED (G2); J3, RULED (US-6c); the record's pattern where a column stands DECLARED; US-4's DECLARED form |
| The electors | the statutes in force per jurisdiction (`ElectoralCollege`): winner-take-all, Maine's and Nebraska's district method; the electors in force the catalog holds for the year, carried forward from 2032 and labelled so on the Parliament page's president row | R-EL8; R-US13 (a), RULED (F8) |
| No majority | the House seated on 6 January chooses by states - one vote a state, a majority of ALL the states to elect (from the apportionment, not from the delegations listed); a delegation votes its majority party's nominee, a tied delegation casts none; the House the game elected where it sits (US-16), else the House of record by its districts. A House that cannot choose hands the Vice President to the Senate the same day, a majority of the whole Senate to choose; from the oath that Vice President acts as President, else the Speaker - never the sitting president | R-US3 (a), RULED (F8) - the delegation's vote DECLARED; R-US22, RULED (I6) |
| The chamber | elected on the same day by US-16's count (below), seated on 3 January; the controller's chamber path records that no chamber is elected through it | R-US1 (a), RULED (F8); US-16 |
| The save | the contest rides format 39; the states are re-derived from the national shares it keeps (`UsPresidentialElection.StatesOf`, the overload with the country, in the form the contest's own basis names; `UsPresidentialElection.StatesOnRecord` is the record form alone), so no per-state field is saved | US-7's step for the pair |
| A later count | a game that has counted swings from its own previous count's states, each re-derived from its stored contest back to the game's first count (which swings from the record's) - each contest in the form its own basis names, so a count saved by a build before US-26 re-derives on the record's states it was counted on, each jurisdiction weighing its votes cast of record (the game draws no turnout); a count with no game behind it (a planted night) swings from the record's | R-US11 (a) and R-US24 (b), RULED (I8, §856) - US-26's, built; the weights DECLARED |

### The readings (US-8)

<!-- GENERATED by PoliSim.EditorTools.UsPresidentialElectionDiagnostic.WriteReadings. DO NOT EDIT BY HAND. source-digest: 19d2879a0287da49002aaf171923bd355e8b22d523fc76b29d2320e4d49aff3d -->
**E1's acceptance for the USA** - the reference world (US-6's: the epoch the eve of 5 Nov 2024, one day stepped by the game's loop) holds its own count on the poll E1's factors are fitted to, and elects Donald J. Trump (REP): **312 to 226** electors of 538, against the record's **312 to 226** as appointed. Its national vote is the FEC's by construction - REP 49.80 %, DEM 48.32 %, every other candidate 1.88 % (the largest gap 0.0000 pp).

States called wrong: none. Districts called wrong: none.

Its states against US-4's (a) column at the true national shares (*The readings*, above): 0 calls apart, the largest margin difference 0.580597 pp (DC) - the difference is the count's form on the ballots of record: (a) swings all three columns, every other candidate pooled, from 2020's states, where the count stands every other candidate only where listed (below) and swings the two nominees on their two-party split over the rest. The stored contest re-derives these states exactly from the national shares it keeps, so the save carries no per-state field (format 39, the step US-7 took for the pair).

**Every other candidate stands only where on the ballot of record** (Elias's ruling J3, 2026-10-19; US-6c): each column of the FEC's workbook but the nominees' takes its part of the count's share of every other candidate (by its national vote) only in the jurisdictions whose ballot listed it - the workbook's roman cells, by its note that italics indicate write-in votes - spread over them in the record's own pattern there (its share of record of each one's vote, renormalised to its national share on the count's weights, 2020's votes cast); a write-in count gives no standing, and its votes go where the column stands; the scattered write-ins, on no ballot, stand where the workbook reports them. Jurisdictions listing Kennedy 31, Oliver 47, Stein 37. The two nominees share the rest of each jurisdiction by the normalised uniform swing on their two-party split of 2020 (R-US14 (a)), each jurisdiction weighing its 2020 votes cast less every other candidate's part. Against the record's 2024 shares of every other candidate the largest gap is 0.778 pp (NY), the mean 0.102 pp.

**The dates**: the polling day 5 Nov 2024 (3 U.S.C. §21(1)), the electors meet 17 Dec 2024 (§7), Congress counts 6 Jan 2025 (§15(a)), the oath 20 Jan 2025 (the Twentieth Amendment §1) - NARA's key dates for 2024.

**R-US3's path, planted** - the record's 2024 college with Pennsylvania, Michigan and Wisconsin to the Democratic nominee and Nebraska's 2nd district to the Republican gives 269 to 269: no candidate has a majority of the 538 electors appointed. On 6 Jan 2025 the House elected in 2024 (seated by date) chooses by states - 30 for Donald J. Trump, 18 for Kamala D. Harris, 2 tied, casting no vote, of 50; a majority of all the states is 26 - Donald J. Trump elected, sworn on 20 Jan 2025.

**A later day**, on a planted even poll: 7 Nov 2028 counts 302 to 236 - the field unnamed ("Republican Party's candidate (2028)") at E1's future factor and no minor candidate, the states from the record's 2024 - a planted count with no game behind it (a game's own later count swings from the game's own 2024 states, R-US24 (b), US-26) - the swing closing to within 5.1E-006 of the national shares (with no minor candidate to absorb the floor it closes only approximately; no call moves), the electors those in force in 2024 (the 2020 census's, by law in force for 2028; carried forward from 2032, R-US13 (a), and labelled so on the president row).

**Not modelled, stated**: faithless electors; Maine's ranked-choice count; a single minor candidate's win of a state (the states pool every other candidate - the count names a state where they lead together); the Vice President's own electoral count (the tickets are one: where the House cannot choose, the Senate chooses the Vice President, who acts from the oath - R-US22, RULED, held by PresidencyDiagnostic).; a minor candidate's own swing (every other candidate stands at the record's pattern where on the ballot of record, scaled with the field).

The rulings it runs on: R-US1 (a), R-US3 (a) and R-US13 (a) (Elias's ruling F8), R-US14 (a) (G2) and J3's minor candidates on the ballot of record, each RULED; R-US11 (a) and R-US24 (b), RULED, are US-26's, built: a game's own later count swings its states from its own previous count's (HistoryRolledForwardDiagnostic (d)).
<!-- END GENERATED -->

## The Congress - US-16

The game's Congress count (`docs/specs/USA_STAGE_PLAN.md`, US-16 - checkpoint 2; R-US18 (b) and R-US19 (c) over (a), ruled). On the presidential polling day the
day loop counts the House and the Senate class whose terms expire beside the president (`UsCongressElection.Count`, on the poll the presidential count reads and
the night's states), and seats both on 3 January; from then the record seats no House in that world, and only the Senate seats the game did not elect. The
acceptance is `PoliSim.EditorTools.CongressElectionDiagnostic`, which writes the readings below and fails where they no longer say what it measures.

| part | what it does | basis |
|---|---|---|
| The day | the presidential polling day; the new Congress seated at noon on the 3rd of January after it (a date) | 2 U.S.C. §§1 and 7; 3 U.S.C. §21(1); the Twentieth Amendment §2 |
| The House | state delegations by a state swing: each state's base REP share of REP+DEM moved by the national swing in the poll (REP / (REP + DEM) on the day against the base's own-line vote), its seats anchored on the base's own; the base the record's latest House election, then the game's own last count | R-US18 (b), RULED (I1); R-US15 (a) (G2) |
| The seats in force | the apportionment the catalog reads for the year, carried forward past the last read; the House's total asserted on every count | 2 U.S.C. 2a; R-US13 (a) |
| The Senate | the class whose terms expire, and the specials of the day the record holds: each seat's share its state's from the presidential count that night, moved by its race's factor fitted on 2024's class only; later fields at factor 1.0; a seat at exactly a half by the block's cascade | R-US19 (c) over (a), RULED (I2); Art. I §3 cl. 2 |
| The Senate seated | the record's seats by date, with every seat the game elected over them from its 3 January | R-US1 (a), RULED (F8); R-US10 (a) |
| Ties | an exact state or district tie in the presidential count is a seeded draw, never in one party's favour | J4 (US-8c) - the district tie Code's reading |
| The save | the contests ride format 49: each state's share, delegation and unopposed seats are the next count's base | US-11's *In play* |

### The readings (US-16)

<!-- GENERATED by PoliSim.EditorTools.CongressElectionDiagnostic.WriteReadings. DO NOT EDIT BY HAND. source-digest: 2fc40be2052af06e328b6299535bd81c841b2f4e379a60259943860256194b7d -->
**The Congress election of 5 Nov 2024** - a fresh world on its eve (the epoch 4 Nov 2024, seed 777, the USA the player's country, REP), stepped in the controller's order: its own count holds the House and the Senate class on the presidential polling day, on the poll the presidential count reads, and seats both on 3 Jan 2025.

**The House** (R-US18 (b), RULED - I1): REP **216**, DEM **219** of 435, against the record's **220 / 215** - N -4. V(T) 0.4890 (the poll on the day, REP / (REP + DEM)) against 2022's own-line 0.5140, a swing of -0.0250. Delegations REP 25, DEM 22, tied 3, against the record's 30 / 18 / 2; K 44/50. Seats misplaced, G 20: AK REP 0 (record 1), AL REP 6 (record 5), CA REP 11 (record 9), CO REP 3 (record 4), FL REP 19 (record 20), MI REP 6 (record 7), MS REP 2 (record 3), NC REP 7 (record 10), NY REP 11 (record 7), OR REP 2 (record 1), PA REP 8 (record 10), TX REP 24 (record 25), WI REP 5 (record 6).

At the record's own swing (2024's own-line V 0.5132) the same count gives REP 222 - US-11's C4 (b) row above reads REP 222: the game counts the rule R-US18 was measured on.

**The Senate** (R-US19 (c) over (a), RULED - I2): class 1's seats and the specials of the day, 34 seats - REP **15**, DEM **19**, against the record's **15 / 19**; called wrong: none. Each seat takes its state's share from the presidential count that night, moved by its race's factor c_j, fitted on 2024's class only (34 seats seated with a factor; |c_j| mean 0.136, largest 0.532); at 2024's true national vote the factors reproduce the class by construction (none called wrong).

**The 119th the world seats** from 3 Jan 2025: the House DEM 219, REP 216 (435); the Senate REP 53, DEM 47 (100) - the seats the game did not elect stay the record's, by date. The record's own 119th Senate on that day: REP 52, DEM 47, 1 vacant. The economic vote reads the government as divided - the president's party DEM, the House's largest DEM, the Senate's majority REP.

**Every later election** (planted even polls, each on the game's own previous count), the House and the Senate as each sits on its 3 January: 2028 (class 3, from the game's 2024): the House REP 220 DEM 215 = 435, the Senate seated 3 Jan 2029 = 100; 2032 (class 2, from the game's 2028): the House REP 220 DEM 215 = 435, the Senate seated 3 Jan 2033 = 100; 2036 (class 1, from the game's 2032): the House REP 220 DEM 215 = 435, the Senate seated 3 Jan 2037 = 100.

**Ties** (US-8c, Elias's ruling J4): an exact state tie in the presidential count - and, by Code's reading, a district's - is broken by a seeded draw, never in one party's favour: over 1,000 seeds Pennsylvania's tie goes REP 516 times and Nebraska's 2nd district's 500. The House count's own halves are R-US18's block's (the base winner, the half rule), and a Senate seat at exactly a half goes by R-US19's cascade.

**Declared**: the Senate seats the game did not elect keep the record's resignations and appointments, those that follow from the record's own presidential result included - Vance's Ohio seat vacant from 10 Jan 2025 and Rubio's Florida seat on 20 Jan, both filled on 21 Jan, whoever the game elected - so "435 and 100" is held on each 3 January, the day each Congress sits. Under a president acting after an undecided contest (R-US22) no Vice President is in office, and the acting president's party decides an equal Senate. An acting president of the player's party is the player's.

**Not modelled, stated**: third-party winners; runoffs, ranked-choice transfers and Georgia's majority rule; specials the record does not hold; party switches; a delayed oath (every winner sits from 3 January). The midterms are US-27's (`UsMidtermDiagnostic`).

The rulings it runs on: R-US18 (b) (I1) and R-US19 (c) over (a) (I2), each RULED; R-US15 (a) (G2); R-US22's order (I6) where the House cannot choose.
<!-- END GENERATED -->
