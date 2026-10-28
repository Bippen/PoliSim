using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §496 (2026-09-14): THE NO-POLICY TRAJECTORY'S SENTINEL - the first <see cref="Turns"/> turns of the trajectory dump's own text
    /// (<see cref="TrajectoryBaselineDump.Build"/>, the same builder the dump writes from), at both seeds, hashed and held against the
    /// digests of the declared baseline. §490 moved the no-policy trajectory from its SECOND turn - Germany's revenue left § 32a's own
    /// arithmetic for the ramps derived from it - and no bar saw it: the pass was filmed, not dumped, and `ArtifactIdentityCheck` verifies
    /// the trajectory files already on disk, never a rerun. A change that moves this text is either a family - dump it, explain it per
    /// country, and set <see cref="BaselineLabel"/> and the digests here in the same commit - or a numeric-inertness break (§402).
    /// First in the simulation group, so no other check's static state can reach it before it runs.
    ///
    /// <para><b>FT-10 (2026-09-21): THE BOUNDS PASS - a digest certifies SAMENESS, not sanity.</b> The digests above already hash the energy
    /// layer's fields (they are public fields of the state, eight of the dump's seventy-seven), so a CHANGE in them was always seen; what no
    /// sentinel saw was GROWTH - Poland's `EnergyIndustryPrice` past 10⁶ at t361 and near 10²⁰ at t1000, in the baseline itself, found only by
    /// §538's diff. The bounds pass runs seed 777 for a century (its first twenty turns are the digest's, so the run is shared) and holds each
    /// country's energy prices OVER ITS OWN PRICE LEVEL - the household price at the seed's prices, the industry price deflated, the congestion
    /// rent deflated - to two measures: LEVEL, the largest move from turn 1 anywhere in the century, and TAIL, the move over the last twenty
    /// years, which is what separates a runaway from a level shift. A country with no recorded breach is held to <see cref="CleanBandPercent"/>
    /// on both. ⚠ FT-10's measured breaches are RECORDED, not excused: each is a named cell with its own ceiling (the measurement rounded up, so
    /// it cannot worsen by a point unseen, and cannot sit a point slack either), their count is a ratchet in the ledger, and every run prints
    /// them as OPEN. The fix lowers the cells to the band and the count to zero in its own commit - apply nothing until ruled.</para>
    ///
    /// <para><b>P-A landed (2026-09-21, §545) and lowered them in its own commit:</b> thirteen cells to eleven, Poland's two TAIL cells and France's closed (the runaway's signature is gone: Poland's
    /// tail reads 0.004 %), every LEVEL ceiling re-cut on the new measurement, and each cell now carries its CAUSE - what is left is the rule's own bounded answer to a ministry's cut (Poland, Italy),
    /// the same answer at a K the rule should not have (France, Sweden - P-A′, ruled third), and Sweden's water value (P-B, ruled second). Italy's industry LEVEL is a cell P-A OPENED, by three
    /// thousandths of a point: its path shrinks in real terms, so the old rule's growth index had been DAMPING its answer, and the share-of-path form reads the cut in full.</para>
    ///
    /// <para><b>P-B landed (2026-09-21, §552) and lowered them again:</b> eleven cells to six. Sweden's five water-value cells are DELETED - its congestion rent over the price level
    /// reads 0.000 % on both measures where it read 47.8 and 7.4, its tails 0.1 and 0.2 - and the one Swedish cell left, the industry price's LEVEL at 1.193 %, is the levy's
    /// part: K 40.2 on a levy of two hundredths of a cent, P-A′'s subject, re-cut with that cause.</para>
    ///
    /// <para><b>P-A′ landed (2026-09-21, §553) and lowered them a third time:</b> six cells to two. Sweden's, Italy's and Poland's four are DELETED - each reads 0.000 %: their
    /// lines carry none of the scheme the bill's levy funds (each sourced from the country's own 2026 budget document), so a ministry's cut of the line no longer reaches the
    /// levy. France's two stand, lower (18.394 and 4.789 where they read 27.176 and 7.075): two thirds of its line IS that support, so the rule still answers its ministry's
    /// cut - the rule's bounded answer, and the only cause left.</para>
    /// </summary>
    public static class TrajectorySentinelCheck
    {
        /// <summary>The baseline these digests are the first turns of: `traj_p6pap` - FT-10 · P-A′ (2026-09-21, §553): the levy rule reads the SUPPORT in the budget's energy line -
        /// the line's own path at its sourced support share (France's two thirds; Sweden, Italy and Poland none) and the subsidy dial's cost in full. France, Sweden, Italy and
        /// Poland move from turn 2 on their own levies - the three whose line carries no support stop answering their ministries' cuts, and their real energy prices hold the seed's
        /// figures for a thousand years; Germany moves from turn 3 through the euro zone's one rate and the USA from turn 3 through its currency, their REAL energy prices not at
        /// all. Only AI-governed books change. Before it `traj_p6pb` - FT-10 · P-B (2026-09-21, §552): Sweden's water value in the seed's prices - each neighbour's block
        /// price deflated by its own price level before the weighting, the zone price carried by Sweden's level once. Sweden moves from TURN 1 on its own clearing (the three price
        /// levels already differ at the first boundary); the USA and Poland from turn 2 through `CurrencyStrength` (a floating currency follows its rate against its partners'
        /// average), in the fifth to eighth digit; Germany, France and Italy DO NOT MOVE AT ALL - no channel reaches a shared-currency country. Before it `traj_p6pa` - FT-10 · P-A (2026-09-21, §545): the levy scale reads the support line's move as a SHARE of its own path
        /// times K, the seed's line over the seed's levy - Poland, France, Sweden and Italy move on their own levies from turn 2; the USA's (no levy) and Germany's (no line) levy scales and REAL
        /// energy prices cannot move and do not, and their other rows move from turn 3 in the fifth decimal place by propagation (Germany through the euro zone's one rate, the USA through its
        /// currency and trade). Only AI-governed books change: a player's path rides prices alone and P-A is the old arithmetic there. Before it `traj_p6e1` - P6-E1's yield (2026-09-18, §538): jobbskatteavdraget in Sweden's income-tax yield, the lever seeded at the credited average on taxed income; before it `traj_p6d1` - P6-D1 (2026-09-17, §533): every pair of the six carries a sourced trade link (Eurostat 2023, goods and services, the exporter's report), replacing ten authored pairs and five absences; all six move through the trade balance and tariff revenue (was `traj_pn3`).</summary>
        /// <summary>PN-3b (2026-09-22, §577): the wage bill's base is PotentialOutput.LabourInput × the real wage, where it was the 20–64 cohort × the 15+ participation
        /// rate - the seam PN-3 retired from potential, now off the base every wage-taxed instrument is levied on. Every AI book moves: five states take more revenue and run
        /// smaller deficits at year 100 (France +33.5 % on the budget, Italy +20.2, Poland +10.7, Sweden +4.1, Germany +2.6; debt -19.8 to -2.3 %), and the USA's fiscal rule
        /// spends the room instead - its government consumption stops being cut (4 053 at t20 and 2 056 at t60 on the old path; 4 338 at t40 and rising on this one) and its
        /// deficit is 2.4 % wider. Before it 'p6pap2' - FT-10 P-A′ (§572).</summary>
        /// <summary>FT-16 (2026-09-23, §585): the euro zone's rate blends its members' Taylor readings by their HICP country weights - household consumption at current
        /// prices, Eurostat's HFMCE basis - where it weighted by REAL GDP. Germany's weight at year 100 is 0.626 against 0.619, France 0.271 against 0.276, Italy 0.103
        /// against 0.105; the zone's rate moves +0.0007 pp at t100, and the three euro states and, through trade and currency, the other three move in late digits.
        /// A control dump of the tree without the change was byte-identical to 'pn3b', so the whole diff is this change. Before it 'pn3b' - PN-3b (§577).</summary>
        /// <summary>PN-1's participation response (2026-09-23, §596): the structural participation gains the bands a pension age has crossed since the seed, at the
        /// country's sourced rate for the year before the lower age × the hazard at it (Atav, Jongen &amp; Rabaté 2021, Table B.1). Zero at the seed; the no-policy run
        /// moves where a statute steps after 2026, and the state's rate carries each step as a level shift the day it moves (the review's D1: moved on the anchor
        /// alone, the lag read as a negative supply shock and lowered unemployment); the rate before an age is read by single year between band midpoints (D2).
        /// The shift is synced at the top of both readers of the gap, the daily reversion and the boundary's split (the verification pass: a boundary on the day a year
        /// commits, or a bill passing, reached the split before the day's step). At year 100 on both seeds: France (62 y 9 m → 64 by 2033) +0.93 % labour force,
        /// +0.93 to +0.96 % potential, +0.95 to +0.99 % GDP; Germany +0.14 %, Italy +0.08 to +0.10 %, the USA +0.06 %; Sweden and Poland by spillover alone. France's
        /// debt ratio 24.20 → 24.19 on seed 777 and 33.21 → 33.63 on 424242 - the AI ministry's threshold rule, whose net-expenditure cap reads the labour step as
        /// potential growth and cuts less (`PensionParticipationProbe`), and whose binding years move under a small perturbation. Before it 'ft16' - FT-16 (§585);
        /// 'ft14' and 'pn1d2' were byte-identical to it.</summary>
        /// <summary>§599 (2026-09-23, ruled): WHERE A COUNTRY'S EFFECT WAS MEASURED, THE MEASUREMENT IS THE STEP - France reads Rabaté &amp; Rochut's +20.9 pp and
        /// Germany Geyer &amp; Welteke's +13.5 pp directly (the formula gave France 25.3); the four unmeasured countries keep the sourced rate × the median hazard
        /// and move against 'pp3' only by spillover (Italy through the zone rate, the others through their currencies; 1.2e-4 at most). At year 100: France +0.77 % labour force, +0.78 to +0.83 % GDP; Germany +0.24 %, +0.25 to +0.26 %. France's century
        /// debt ratio moves −0.08 points on one seed and +0.70 on the other: recorded, by the same ruling, as NO MEASURABLE FISCAL EFFECT, not a finding.
        /// Before it 'pp3' - §596.</summary>
        /// <summary>§604 (2026-09-24, K-1 part (3)): THE GAME STARTS ON 1 OCTOBER 2026, after the Riksdag elected on 2026-09-13 convened. The move reaches the
        /// no-policy economy through `Country.CalendarYear` ALONE - with the year computed on the old start's day count the century is byte-identical to 'pp4'
        /// ('ep1old'). Germany, Italy and the USA first move at turn 1 (their 2027 statute steps land inside turn 0), France at turn 2, Sweden and Poland at
        /// turn 4 through their currencies alone. Debt as % of nominal GDP at turn 100: Germany 31.43 -> 33.04 on 777 and 33.61 -> 33.71 on 424242,
        /// France 24.12 -> 23.99 and 33.91 -> 31.13; the sign holds on both seeds (France lower on 99 of 100 turns, Germany higher on 94 and 95) and turns
        /// 1-5 are identical across them - a SYSTEMATIC effect of the statutes arriving about nine months earlier in game time, not seed noise, and
        /// §599's ruling (a sign that flips) does not cover it; its size at turn 100 depends on the seed. Before it 'pp4'; K-1's
        /// parts (1) and (2) ('k1ch', 'k1dl2') were byte-identical to it.</summary>
        /// <summary>§618 (2026-09-25, PS-1): THE WORLD CLOCK - the dump's world opens on Sweden's own start, 18 January 2026 (the standard run-up before 13 September),
        /// instead of K-1's 1 October 2026, and every country is seated in its chamber of record on that date (Sweden's 2022 Riksdag; the others as before, their
        /// latest elections). The move reaches the no-policy economy through the calendar year, as §604's did in the other direction - the statutes now arrive
        /// eight and a half months LATER in game time - and, where a chamber changed, through the parliament's votes; the probe `ps1probe` (this tree with the
        /// roster's own seats in every chamber) measures the second channel, recorded in §618. At turn 100 the largest movers are France's real household
        /// energy price change (−3.6 %), budget (−1.1 %) and population growth (−1.0 %) on seed 777; Germany's population growth rate moves ±10–14 % on a
        /// figure of 0.002; Sweden, Poland and the USA move in late digits. Before it 'k1ep' - K-1 part (3) (§604).</summary>
        /// <summary>§716 (2026-10-01, Elias's ruling item 7): A PARTNER HOLDING FINANCE HOLDS ITS LEVERS IN FACT - the household rates (income tax, VAT) move
        /// a quarter point a year toward the seeded rate less a quarter point per CHES `redistribution` point the partner stands right of the head
        /// ([AUTHORED-DRAFT], calibration 26th). On the dump's world (18 January 2026, no election re-forms an AI government) three partners hold Finance:
        /// Germany's SPD under the CDU (2.89 against 6.39: +0.875 points), Italy's Lega under FdI (7.71 against 7.07: -0.16), Poland's TD (no CHES position:
        /// no move); France's RN sits at Finance only in a PROVISIONAL stand-in, which no partner runs (the review's decision A). Germany first moves at turn 1
        /// (approval -0.75 from the rise); at turn 100 GDP -0.38 / -0.47 % (Germany), +0.09 / +0.11 % (Italy) on 777 / 424242; France moves in late digits through the euro-zone rate
        /// and trade, Sweden, Poland and the USA through trade. The family `fp716b` (the first dump `fp716`, before decision A, moved France too). Before it 'ps3k'.</summary>
        /// <summary>§717 (2026-10-01, Elias's ruling item 8): ITALY'S MEF SUMS ITS THREE RATED POSTS AS THE PAPER DOES (3.94; the derived mean 2.28) -
        /// Meloni's Finance goes to FdI, the head's own (a miss against the record's Giorgetti, recorded), so Italy's partner rule stops, and Tusk's to
        /// KO (the record's Domański). Against 'fp716b' Italy alone moves beyond late digits (the rest through the euro-zone rate and currency
        /// strength): t100 GDP -0.090 / -0.109 % on 777 / 424242, back to 'ps3k''s figures within
        /// late digits; against 'ps3k' Germany alone moves (its SPD partner, §716). The family `fp717`. Before it 'fp716b'.</summary>
        /// <summary>§755 (2026-10-02, Elias's ruling A2): NOTHING FROZEN - A PARTNER HOLDING FINANCE ACTS THROUGH THE FISCAL STANCE ONLY, the same rule for the
        /// player and the AI: §716's household-rate step replaced by a stance step through the AI finance ministry's uniform instrument. The dump's one partner
        /// is Germany's SPD under the CDU, the whole century (it asks +0.78 pp of GDP - about 3.1 `lrecon` points left of the CDU): four steps on 777, +0.25 in
        /// 2027, 2038 and 2041 and +0.03 in 2042, the EU rule's consolidation claiming every line 2028-2037 (the treaty's first claim) - and §716's +0.875 points on
        /// the income tax and VAT gone, so the deficit widens early and the EU rule cuts the lines harder later. Against 'fp717' Germany alone moves beyond late
        /// digits: t100 government consumption -6.08 / -1.22 %, debt -5.27 / +0.58 % on 777 / 424242 - on 777 most of it in one turn, t83, where the two paths fall
        /// on opposite sides of the Fiscal Compact's 1 % deficit limit (debt near 33 %, below 60: the ministry cut fp755's lines, not fp717's); the rest
        /// through the euro-zone rate, trade and currency strength. The family `fp755`. Before it 'fp717'.</summary>
        /// <summary>§816 (2026-10-14, GA-8-1, Elias's ruling: "At each boundary, read the BEHG rate for the calendar year, and let a passed rate stand for the
        /// year it was passed for"): GERMANY'S CARBON STATUTE READS THE CALENDAR YEAR. The dump opens on Sweden's start, so Germany's first boundary falls on
        /// 2027-01-18, past the schedule: the law's last 55 EUR/t carried by the level (56.6), where the turns' count set 2024's 45 there and 55 only a year
        /// later. Against 'fp755' Germany moves from its first boundary: on 777 the carbon revenue lowers the debt 0.05 % in turns 2-4, the AI ministry's
        /// thresholds swap the sign at t6 (-2.6 %) and t8 (+2.2 %), +1.2 % at t20, and t83 - the Fiscal Compact's 1 % deficit limit, §755's knife-edge - takes
        /// it to +5.9 % at t100 (GDP +0.70 %, approval +1.56 %); on 424242 the debt +0.04 %. Every other country moves from turn 2 in late digits, through
        /// trade, the euro-zone rate and currency strength (the largest at t100 Italy's debt, +0.13 % on 777). The family `ga81`. Before it 'fp755'.</summary>
        /// <summary>§817 (2026-10-14, GA-11-1): THE GINI'S ANCHOR RE-TAKEN WHERE F4-5 RE-SEEDS THE INCOME LEVER - the redistribution term read the income
        /// line's rate against the literal the line was built from, so the Gini's target opened displaced at the seed in the five re-seeded countries. Against
        /// 'ga81' only the Gini and approval move, from turn 1, in France, Germany, Poland, the USA and Sweden; Italy (never re-seeded) and every other field
        /// stand. At t100 on 777 / 424242 the Gini reads France 29.52 / 29.52 (31.73 / 31.73 before), the USA 38.54 / 38.53 (40.13 / 40.12), Poland 23.53 /
        /// 23.52 (25.11 / 25.09), Germany 28.55 / 28.55 (29.99 / 29.99), Sweden 26.24 / 26.26 (26.49 / 26.52); approval +3.9 to +5.4 % in the four, +0.5 to
        /// +0.7 % in Sweden. The family `ga111`. Before it 'ga81'.
        /// §859 (AF-35, q35): a fund earns on its PRINCIPAL. Against 'ga111' Sweden moves first, at turn 1, in its budget and debt alone (its contribution
        /// is below the 3 % structural draw, so its principal falls within every period); the USA (turn 3-4) and Poland (turn 5) follow through the currency
        /// and trade; France's own fund from turn 17 (777) / 22 (424242), once it outgrows its contribution; Germany and Italy through the euro rate after
        /// France (turn 21 / 27). AF-11, AF-17 and AF-23/30/31, landing in the same commit, move nothing on the no-policy path. The family `af35`.
        /// §909 (Elias's K56, 2026-10-23): every central-bank shortlist carries a no-lean candidate, so every AI head appointed leans 0. Against 'us24dl' turns 1-4
        /// are identical on both seeds; the USA moves first at turn 5 (the Fed chair's term end: the zone's interest rate 2.636 -> 2.823 on 777), Sweden and Poland the same
        /// turn through the dollar (`CurrencyStrength`, `TradeBalance`), the euro countries at turn 9 through the ECB's rate; no stream's draw count changes. K53/K80,
        /// K58, K59, K87, K31 and K33, landing in the same commit, move nothing on the no-policy path (no player, no bills). The family `form`.
        /// §926 (MF-13; K134, K135, K191): approval reverts daily at 0.81 a quarter (it kept 95 % of a gap a year), every sustained term at its old equilibrium. Against
        /// 'form' ApprovalRating ALONE moves, in every country from turn 1, on both seeds; GDP, debt, the budget, participation, unemployment and inflation are
        /// identical at turns 1, 4, 20 and 100 (approval feeds no economic field of this dump). In the first year each country moves most of the way to its own
        /// sustained equilibrium - the USA 47.8 -> 23.2 and Poland 48.2 -> 26.7 (both seeds), Sweden 45.8 -> 52.6 (777); by turn 100 approval stands at 45-55
        /// everywhere (777: France 42.6 -> 50.0, Germany 34.6 -> 49.1, Italy 34.7 -> 50.2, Poland 33.2 -> 47.7, Sweden 36.6 -> 47.0, the USA 24.5 -> 45.0) - K192's
        /// rise: the steady event stream's drag falls from about -9.7 to -0.6 points. COAL-1 (§925), landing just before, moves nothing here. The family `mf13`.
        /// §928 (MF-14, q14; K138, K199-K201): unemployment floored at the OECD harmonised series' lowest since 1990, the natural rate never below it, the Phillips
        /// slack NAIRU x ln(U/NAIRU), prices free to fall, France seeded at 7.45 (OECD 2024), no nominal pension cut in the USA and Germany. Against 'mf13'
        /// every country first differs at turn 1 (the convex slack); France moves most - unemployment 7.33 -> 7.79 at turn 1 and held at its floor (7.37 at
        /// turn 100, where 'mf13' sank below it), its debt at turn 100 +23.5 % (777); elsewhere turn 100 moves little (debt Germany +0.4 %, Italy +1.9 %,
        /// Poland +2.7 %, Sweden +0.6 %, the USA -4.4 %; unemployment under 0.04 points). Mean inflation over the run: France -0.08, the USA -0.06, the rest
        /// within 0.03 - no upward drift from the log form (`Reviews/2026-10-28_q14_integrator.md`). The family `mf14`.
        /// §929 (MF q29; K139, K140, K203, K209, K211): life expectancy derived from the sourced mortality schedules; the output gap closing at HLW's persistence
        /// against the period's trend potential, a temporary demand impulse (the demand reference), kappa fitted to D-13 (b). Against 'mf14' every country first
        /// differs at turn 1; output now rises toward potential - GDP at turn 100 (777) France +13.4 %, Germany +9.2 %, Italy +11.8 %, Poland +14.6 %, the USA
        /// +9.6 %, Sweden -1.5 %; the USA's unemployment 6.89 -> 4.34 at turn 1; life expectancy along the projections (France 82.7 -> 91.4 at turn 100); the
        /// no-policy gap inside 2 % at years 10/30/100 everywhere (`Reviews/2026-10-28_q29_integrator.md`). The family `mf29`.
        /// §930 (MF q32/q33; K141-K144, K212-K214, K217): the convex sovereign spread replacing the capped premium, the 300 % ceiling retired, the fund refusing at
        /// booking, the debt crisis at CCC. Against 'mf29' only Budget, the effective debt rate and GovernmentDebt move - every country from turn 1 (the spread),
        /// Sweden from turn 2 (its fund); GDP unchanged to the fourth figure; debt at turn 100 (777) Italy -3.6 %, Sweden -3.8 %, France +0.3 %, the rest under
        /// 0.1 %; no crisis fires in this dump (`Reviews/2026-10-28_q32_integrator.md`). The family `mf32`.
        /// §932 (MF q28; GA-7-1, K221, K222): the hospital waits read real health spending per age-cost unit at 0.821 (Siciliani, Fiorio & Riganti 2017), the
        /// effectiveness multiplier kept. Against 'mf32' only WaitCataractDays and WaitKneeDays move, in Italy, Poland and Sweden (nothing reads them downstream); at
        /// turn 100 Sweden -55.7 %, Poland -86.0 %, Italy +128.6 % - the AI books' health lines against the age-cost index (K222's declared drift; Italy's AI cuts)
        /// (`Reviews/2026-10-28_q28_integrator.md`). The family `mf28`.
        /// §937 (K210, its prereg 6c4522b8): beyond the WEO's horizon the productivity trend is pulled toward the Ageing Report's (EU five) and CBO's (USA) per-worker
        /// paths (`ProductivityPull`). Against 'mf28' every country first differs at turn 6 (the first boundary after mid-2031) in PotentialGrowthRate alone, then
        /// 369 of 468 field-readings move by turn 100 - GDP Italy +213 %, France +95 %, Germany +35 %, Sweden +24 %, Poland -74 %, the USA -24 % - the constant
        /// 2010-2025 seed trend replaced by the sources' paths (`Reviews/2026-10-28_k210_integrator.md`). The family `mf210`.
        /// §938 (PS-8; K159-K163, K238-K247): every modelled country holds its elections on its own calendar as an AI country - the dump, a world with no
        /// player (K243), now holds Sweden 2026, the US midterms and presidencies, Italy and Poland 2027, Germany 2029 and every term after. Against 'mf210' Poland
        /// first differs at turn 6 (its 2031 cabinet gives Finance to TD, the one install that reaches an AI book, K247), Sweden and the USA at turn 7 through trade,
        /// Germany at 35; 322 of 468 readings by turn 100, GDP within 1.1 % (`Reviews/2026-10-28_ps8_integrator.md`). The family `ps8`.</summary>
        public const string BaselineLabel = "ps8";   // §938 (PS-8, the family `ps8`): every country's calendar. Before it §937 (K210, the family `mf210`): the productivity pull. Before it §932 (MF q28, the family `mf28`): the waits read health spending. Before it §930 (MF q32/q33, the family `mf32`): the convex spread, the fund refusing at booking, the crisis at CCC. Before it §929 (MF q29, the family `mf29`): life expectancy from mortality, the gap at HLW's persistence with a temporary demand impulse. Before it §928 (MF-14 q14, the family `mf14`): the unemployment floor, the convex Phillips slack, deflation, France's seed 7.45. Before it §926 (MF-13, the family `mf13`): approval reverts daily at 0.81 a quarter, the sustained terms at their old equilibria - approval alone moves, every country from turn 1. Before it §909 (K56, the family `form`): every AI central-bank head appointed leans 0 - the USA from turn 5, Sweden and Poland the same turn through the dollar, the euro countries from turn 9. Before it §897 (PS-6 US-24, the family `us24dl`): R-US21 (v) retires the AI ministry's ratio-trend rule for the USA - from the first boundary that trigger would have fired (turn 3 on both seeds) the USA's discretionary lines are no longer sequestered (at turn 100 its debt +20.6 % at 777, +27.5 % at 424242; its GDP +7.6 % and +10.0 %), and Poland and Sweden move from turn 4 through the dollar alone (CurrencyStrength and TradeBalance first; at turn 100 under 0.1 %); France, Germany and Italy are identical at every turn on both seeds; turns 1-2 byte-identical everywhere. The live debt limit itself moves nothing in this dump: the AI USA reaches it and raises it by act, writing only the limit's fields, and no cut arms without the player's day tick (the s897 traj_diff, `Reviews/2026-10-22_us24_round2_reading.md`). §882 (A-LOOP q5, the family `aloop-salience`): the family is the event pool's growth 50 -> 52 (a crime event and a war event) ALONE: the Event stream draws as before, but Next(Count) picks a different event in about 74 % of fires (173 of 234 at 777, 180 of 246 at 424242; each seed's first fire unchanged), and each country moves from its first changed pick, its neighbours a turn later through the euro rate and trade. Salience moves nothing in this dump: no bill that cuts a spending line is voted in a no-policy world (the s882d baseline review). §878 (A-LOOP q37, the family `aloop-tax`): the tax term - a rate change charged against the fiscal year's booked net, a cut at the mirror, the charge fading at 0.81 a quarter - moves approval only where an AI books a rate move; in these 100 turns that is Poland alone (ApprovalRating from turn 2, 1 of 462 field-readings at the last turn), every other country byte-identical. §877 (q34) and §879 (q15) move nothing. §875-§876: AF-6's seventh portfolio and its residual rule move Germany's Finance from the SPD to the CDU at the default epoch (the partner's fiscal stance stops; Germany first at turn 1), the euro rate and trade carrying it to the other five from turn 2; AF-7's statutory terms inside the same trajectories (the dump opens on Sweden's start, 2026-01-18: the Fed's term ends 2030-01-18, turn 5; the Riksbank's and the NBP's turn 7; the ECB's turn 9 - each AI appointment matching the keyed draw, s875's baseline review). §859: AF-35's principal rule (above). §817: GA-11-1's Gini anchor (above). §816: GA-8-1's calendar clock (above). PS-6 US-2 (§783): `us2b`, on the reviewed tree, is BYTE-IDENTICAL to `fp755` on both seeds (no dump seats a player, so the record by date never acts, by day or at a load). §755: the stance (above). §717: Italy's MEF summed (above). §716: the Finance partner (above). PS-3k (§638): `ps3k` is BYTE-IDENTICAL to `ps3i` (no dump seats a player, so no election reads the record); PS-3i (§636): `ps3i` is BYTE-IDENTICAL to `ps3h` (every confidence trigger is a player verb); PS-3h (§635): `ps3h` is BYTE-IDENTICAL to `ps3g` (no dump tables a bill); PS-3g (§634): `ps3g` is BYTE-IDENTICAL to `ps3e` (the allocation reads seats and gates levers no dump pulls); PS-3e (§632): `ps3e` is BYTE-IDENTICAL to `ps3c` (the AI government's bill is the PLAYER'S country's path alone - no player, no bill); PS-3c (§630): `ps3c` is BYTE-IDENTICAL to `ps3a` (the dump passes no bill and sets no player, so the role gate is never asked); PS-3a (§628): `ps3a` is BYTE-IDENTICAL to `ps2cal` - the dump sets no player and the no-policy century no party, so the who-governs gate reads as before; `ps2cal` was identical to `ps1wc` in turn - the dump holds no election (it drives the manager, not the controller) and the campaign draws on its own streams - so the digests stand

        /// <summary>CONVENTION: twenty turns - §490's divergence showed on turn 2, and the pair of runs costs seconds.</summary>
        public const int Turns = 20;

        /// <summary>SHA-256 of the dump's text through turn 20 (the header and every row of turns 1-20), read off `traj_ps8_s{seed}_t100.csv` (before §938 `traj_mf210`'s, before §937 `traj_mf28`'s, before §932 `traj_mf32`'s, before §930 `traj_mf29`'s, before §929 `traj_mf14`'s, before §928 `traj_mf13`'s, before §926 `traj_form`'s, before §909 `traj_us24dl`'s, before §897 `traj_aloopsal2`'s, before §882 `traj_alooptax`'s, before §878 `traj_af67`'s, before §875 `traj_af35`'s, before §859 `traj_ga111`'s, before §817 `traj_ga81`'s, before §816 `traj_fp755`'s, before §755 `traj_fp717`'s, before §716 `traj_ps1wc`'s).</summary>
        private static readonly (int Seed, string Sha256)[] Expected =
        {
            (777, "0ce1e0560c1da1c616c9111342795f82a82c1895fcafe6a51a27f4041fa7573d"),
            (424242, "f450438abc75052f0bc3e5f1edeeb727ade3c271c6723cd90f6658f9ea3cb018"),
        };

        // ---- FT-10: the bounds pass ------------------------------------------------------------------------------------------------

        /// <summary>CONVENTION: the seed the century runs on - the first of <see cref="Expected"/>, so its first twenty turns are the digest's and one run serves both.</summary>
        public const int CenturySeed = 777;

        /// <summary>CONVENTION: a hundred turns - the no-policy century every long-run diagnostic reads; FT-10's runaway stood at +31 % there and needed a millennium to be noticed without a bound.</summary>
        public const int CenturyTurns = 100;

        /// <summary>CONVENTION: TAIL is measured from this turn to the century's end - twenty years, the digest's own span.</summary>
        public const int TailFromTurn = 80;

        /// <summary>CONVENTION: the band a clean country's real energy prices are held to, per cent, on both measures. Measured 2026-09-21 on `traj_p6e1`: the USA and Germany 0.000,
        /// Italy 0.77 at most (its levy scale breathes with the ministry's cut of the line) - one per cent holds all three with nothing to spare for a compounding term. On `traj_p6pa` Italy's
        /// industry LEVEL reads 1.003 and is a recorded cell (P-A reads its cut in full); the band itself is not moved.</summary>
        public const double CleanBandPercent = 1.0;

        /// <summary>CONVENTION: a recorded cell's ceiling may stand this far above its measurement and no further, per cent - the ceilings are the measurements rounded up to a whole point.</summary>
        public const double CellSlackPercent = 1.0;

        /// <summary>
        /// FT-10's recorded cells - the cells over the clean band, measured on the landing of 2026-09-22. LOWER IT AS THEY CLOSE, NEVER RAISE IT: FOUR cells in two countries (six in four on
        /// `traj_p6pb` before P-A′, eleven on `traj_p6pa` before P-B, thirteen in three on `traj_p6e1` before P-A). The compounding term is gone (§545), so is the water value read
        /// across price levels (§552), and the rule reads the support in the line, not the line (§553); what stands is the rule's bounded answer to France's ministry, named in the
        /// table's CAUSE (`EnergyRunawayDiagnostic` decomposes it).
        /// </summary>
        public const int EnergyBreachCeiling = 4;

        /// <summary>CONVENTION: the cause a recorded cell carries, printed on its line (P-B's water-value cause retired with its cells, §552; P-A′'s whole-line cause with its, §553).</summary>
        private const string RuleAnswer = "EN-4's rule answering the ministry's cut of the line at its sourced support share - bounded at 1 + 0.8 K × share on the ministry's own path, no tail";

        /// <summary>The recorded cells: country, series, measure, the ceiling in per cent (the 2026-09-21 measurement on `traj_p6pap` rounded up to a whole point), and the cause the cell waits on.</summary>
        private static readonly (string Country, string Series, string Measure, double CeilingPercent, string Cause)[] KnownBreaches =
        {
            // §572: the cells under the RULED test (Annex II's categories for all four). France's two fall with the share it reads; Poland's two stay because its
            // share is nearly the whole line; Sweden's and Italy's CLOSE - their shares are small enough that the rule's answer is inside the clean band.
            ("France", "household", "LEVEL", 18.5, RuleAnswer),   // K210 (simk4): 17.972 at t55 - more than a point under 19.5, so lowered by the check's own rule; MF q29 (sim929): 18.989 at t56 - more than a point under 20.0, so lowered by the check's own rule; 19.454 at t48 on PN-3b (20.728 at t56 before it) (27.176 before P-A′, 35.628 before P-A): K 13.4 x the share 0.763
            ("France", "industry", "LEVEL", 5.5, RuleAnswer),     // MF q29 (sim929): 4.943 at t72 - lowered by the same rule; 5.064 at t56 on PN-3b (5.396 at t62 before it) (7.075; 9.275)
            ("Poland", "household", "LEVEL", 3.0, RuleAnswer),    // 2.414 at t65 on PN-3b (2.740 before it) (2.797 before P-A′): the coal chapter is nearly the whole line
            ("Poland", "industry", "LEVEL", 3.0, RuleAnswer),     // 2.461 at t65 on PN-3b (2.793 before it; 2.851)
        };

        /// <summary>The bounds pass over the century's text. True when every cell holds: a clean cell inside the band, a recorded one under its ceiling and not slack, the count at its ratchet.</summary>
        private static bool EnergyBounds(string century, StringBuilder sb)
        {
            var level = new Dictionary<string, double[]>();   // country -> PriceLevel by turn
            var series = new Dictionary<string, Dictionary<string, double[]>>();   // country -> series -> value by turn (already over the price level where the field is nominal)
            var nominal = new Dictionary<string, Dictionary<string, double[]>>();
            foreach (string line in century.Split('\n'))
            {
                string[] p = line.Split(',');
                if (p.Length != 4 || !int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int turn) || turn < 1 || turn > CenturyTurns) { continue; }
                string key = p[2] == "PriceLevel" ? "P" : p[2] == "EnergyHouseholdPriceReal" ? "household" : p[2] == "EnergyIndustryPrice" ? "industry" : p[2] == "EnergyCongestionRent" ? "rent" : null;
                if (key == null || !double.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) { continue; }
                if (key == "P") { if (!level.TryGetValue(p[1], out double[] l)) { level[p[1]] = l = new double[CenturyTurns + 1]; } l[turn] = value; continue; }
                if (!nominal.TryGetValue(p[1], out Dictionary<string, double[]> byKey)) { nominal[p[1]] = byKey = new Dictionary<string, double[]>(); }
                if (!byKey.TryGetValue(key, out double[] values)) { byKey[key] = values = new double[CenturyTurns + 1]; }
                values[turn] = value;
            }
            foreach (KeyValuePair<string, Dictionary<string, double[]>> c in nominal)
            {
                if (!level.TryGetValue(c.Key, out double[] l)) { continue; }
                var real = new Dictionary<string, double[]>();
                foreach (KeyValuePair<string, double[]> k in c.Value)
                {
                    var r = new double[CenturyTurns + 1];
                    for (int t = 1; t <= CenturyTurns; t++) { r[t] = k.Key == "household" ? k.Value[t] : (l[t] > 0 ? k.Value[t] / l[t] : 0.0); }   // the household price is written at the seed's prices already
                    real[k.Key] = r;
                }
                series[c.Key] = real;
            }

            sb.Append($"    --- FT-10 · the bounds pass: seed {CenturySeed}, {CenturyTurns} turns, each energy price over its own country's price level · LEVEL = the largest move from turn 1, TAIL = turn {TailFromTurn} to {CenturyTurns} · clean band {CleanBandPercent.ToString("0.0", CultureInfo.InvariantCulture)} %\n");
            // The enumeration rule: a century that parsed no energy rows bounded nothing, and its silence would read as six clean countries.
            if (series.Count == 0) { sb.Append("    ⚠ NO ENERGY ROWS PARSED - the bounds pass verified nothing.\n"); return false; }
            bool ok = true; int breaches = 0, cells = 0;
            var seen = new HashSet<string>();
            var countries = new List<string>(series.Keys); countries.Sort(string.CompareOrdinal);
            foreach (string country in countries)
            {
                foreach (string key in new[] { "household", "industry", "rent" })
                {
                    if (!series[country].TryGetValue(key, out double[] x) || !(x[1] > 0.0)) { continue; }   // a series at zero from the seed (no zonal links, so no rent) has no ratio to hold
                    double levelDev = 0.0; int at = 1;
                    for (int t = 1; t <= CenturyTurns; t++) { double d = Math.Abs(x[t] / x[1] - 1.0) * 100.0; if (d > levelDev) { levelDev = d; at = t; } }
                    double tailDev = x[TailFromTurn] > 0.0 ? Math.Abs(x[CenturyTurns] / x[TailFromTurn] - 1.0) * 100.0 : 0.0;
                    foreach ((string measure, double measured, string span) in new[] { ("LEVEL", levelDev, "t" + at), ("TAIL", tailDev, "t" + TailFromTurn + "-t" + CenturyTurns) })
                    {
                        cells++;
                        double ceiling = CleanBandPercent; bool recorded = false; string cause = null;
                        foreach ((string c, string s, string m, double pct, string why) in KnownBreaches) { if (c == country && s == key && m == measure) { ceiling = pct; recorded = true; cause = why; seen.Add(c + "/" + s + "/" + m); } }
                        bool over = measured > ceiling;
                        bool slack = recorded && measured < ceiling - CellSlackPercent;
                        if (measured > CleanBandPercent) { breaches++; }
                        if (recorded || over)
                        {
                            sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0} {1,-8} {2,-9} {3,-5} {4,8:F3} % at {5} against {6:F1} %{7}\n",
                                over ? "⚠ OVER " : slack ? "⚠ SLACK" : "  OPEN ", country, key, measure, measured, span, ceiling,
                                over ? (recorded ? " - a recorded cell got WORSE" : " - a NEW breach of the clean band: a term is compounding against the price level")
                                     : slack ? " - the ceiling stands more than a point above its measurement: lower it" : " - recorded, not excused · " + cause));
                        }
                        if (over || slack) { ok = false; }
                    }
                }
            }
            foreach ((string c, string s, string m, double pct, string why) in KnownBreaches)
            {
                if (!seen.Contains(c + "/" + s + "/" + m)) { ok = false; sb.Append($"    ⚠ {c} {s} {m}: a recorded cell the century never produced - the table names a series the dump no longer carries.\n"); }
            }
            sb.Append($"    {cells} cell(s) held; {breaches} over the clean band against the recorded {EnergyBreachCeiling} (P-A, P-B and P-A′ landed - §545, §552, §553; what stands is EN-4's rule answering France's ministry at the line's support share - `EnergyRunawayDiagnostic` decomposes it; lower the ceiling if it closes).\n");
            RatchetLedger.Report("TrajectorySentinelCheck.ENERGY_BREACHES", breaches, EnergyBreachCeiling);
            if (breaches > EnergyBreachCeiling) { ok = false; }
            return ok;
        }

        /// <summary>The century's text through the last row of <paramref name="turns"/> - the same bytes <see cref="TrajectoryBaselineDump.Build"/> writes for that horizon, since the run is deterministic and the rows are in turn order.</summary>
        private static string Prefix(string century, int turns)
        {
            int cut = century.IndexOf("\n" + (turns + 1).ToString(CultureInfo.InvariantCulture) + ",", StringComparison.Ordinal);
            return cut < 0 ? century : century.Substring(0, cut + 1);
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            FieldInfo[] fields = TrajectoryBaselineDump.StateFields();
            bool ok = true;
            var sb = new StringBuilder();
            sb.Append($"=== TRAJECTORY SENTINEL: the first {Turns} turns of the no-policy dump against baseline '{BaselineLabel}' ===\n");
            string century = TrajectoryBaselineDump.Build(CenturySeed, CenturyTurns, fields);   // FT-10: one run - its first turns are the digest's, its whole length the bounds pass's
            foreach ((int seed, string expected) in Expected)
            {
                string got = Sha256(seed == CenturySeed ? Prefix(century, Turns) : TrajectoryBaselineDump.Build(seed, Turns, fields));
                bool same = got == expected;
                sb.Append($"    seed {seed}: {got} {(same ? "- the baseline's" : "- NOT the baseline's " + expected)}\n");
                if (!same)
                {
                    ok = false;
                    Debug.LogError($"TRAJECTORY SENTINEL: seed {seed}'s first {Turns} no-policy turns moved off baseline '{BaselineLabel}' - a family (dump it, explain it per country, set the label and the digests here in the same commit) or a numeric-inertness break.");
                }
            }
            bool bounded = EnergyBounds(century, sb);
            if (!bounded) { ok = false; Debug.LogError("TRAJECTORY SENTINEL: the energy layer's bounds pass failed - a real energy price left its band, a recorded FT-10 cell got worse or sits slack, or the breach count passed its ratchet. The lines above name the cell."); }
            Debug.Log(sb.ToString());
            Debug.Log(ok ? "=== TrajectorySentinelCheck: ALL ASSERTIONS PASS ===" : "=== TrajectorySentinelCheck: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static string Sha256(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(new UTF8Encoding(false).GetBytes(text));
                var hex = new StringBuilder(64);
                foreach (byte b in hash) { hex.Append(b.ToString("x2")); }
                return hex.ToString();
            }
        }
    }
}
