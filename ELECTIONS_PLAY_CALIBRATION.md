# PoliSim — the play-calibration list

**Opened 2026-08-29, on the completion of W-E1, W-E3 and W-E4**, per the standing ruling from the
W-B3 / W-B10 review: *"Leave the named constant alone through W-E1/E3/E4, then open a
play-calibration list in the records — this constant is its first entry, and it will not be the last
thing that needs a human's hands."*

## What this list is for

Everything here is a decision that **cannot be made from a spreadsheet, a backtest or a spec
reading**, because the question it answers is *how the game feels to play* — and there is not yet a
playable loop to feel. Each entry names the thing, states what is known about it, states what would
settle it, and says explicitly what must NOT be done in the meantime.

**The rule that governs the whole list: nothing here is tuned to make a gate pass.** Every entry
exists because measurement has taken it as far as measurement can go. Changing one of these numbers
without a loop to judge it against would be substituting confidence for evidence.

---

## 1. `CampaignPressure.PersuasionPerCompatibilityPoint` = 40 000

**The entry the ruling named.** The scale on which §42's persuasion pressure converts into a
compatibility bonus, and therefore the magnitude of everything a campaign does.

**What is measured.** A hard week of campaigning — three rallies, two town halls, a television buy
and daily door-knocking — moves the campaigning party **+0.19 pp**, compounding to roughly **1.5
points over a full eight-week campaign** (W-B3's end-to-end run; shares still sum to 1, and the same
campaign with the chain severed moves nothing).

**The judgement.** That is roughly right for a *real* campaign and probably wrong for a *game*. Real
campaigns move a point or two; a game in which eight weeks of play moves a point and a half may feel
inert. But feel cannot be calibrated against a loop that does not exist.

**What would settle it.** A playable campaign loop (Track G's wiring, then a human playing it).

**What must NOT happen first.** The constant must not be raised to make a screen look more
responsive, nor to make a backtest fit better. It is one named constant precisely so that when a
human does set it, they change one number and know exactly what moved.

---

## 2. §42 derives enthusiasm without reference to salience

**Found by W-E3's film**, and a MODEL question rather than a constant.

**What is measured.** `CampaignActions.Resolve` computes `enthusiasm = exposure × credibility ×
weight`. Neither exposure nor credibility is a polled quantity, so enthusiasm carries **no
measurement uncertainty at all** — its estimate band is a point where persuasion's is a range. The
action screen prints it as a point with that reason stated.

**The question.** It is odd that how much an electorate *cares* about an issue changes how persuaded
they are but not how motivated they are to turn out. §26's turnout model has its own enthusiasm
input; whether campaign enthusiasm should share salience's dependence is a design decision.

**What would settle it.** Play, plus a look at whether §26's turnout response makes the current
split behave sensibly at election time (Track D).

**What must NOT happen.** A width invented at the drawing layer to make the two estimates look
alike. If enthusiasm should depend on salience, that is a change to `Resolve` with its own reason
and its own harness assertion.

---

## 3. Earned media is free, and therefore dominant

**Recorded at W-B3, ruled to W-B9, and made VISIBLE by W-E3's action screen.**

**What is measured.** `Interview` costs **0 kr and 2 hours** and produces 2 268 persuasion against a
500 000 kr television buy's 1 124 — and on the action screen it draws one of the longest estimate
bands on the sheet. An interviews-only player is currently optimal, which §34 says the design should
not reward.

**The ruling already made** (carried here so it is not re-discovered as a calibration problem):
interview dominance is **W-B9's, as a mechanism, not a nerf**. Earned media is free because *someone
else decides whether to book you*, so the scarce resource is **media interest** — implemented in §13
as availability driven by newsworthiness (coverage, momentum, recent events), never as a flat cost
or cap bolted onto the action. This also gives §13's coverage loop a real input: a party nobody
covers cannot buy its way onto the air.

**What must NOT happen.** A cost added to `Interview`'s spec, or a cap on how many can be run.

---

## 4. §21's polling prices and sample sizes are [AUTHORED-DRAFT]

**What is measured.** Nothing yet — the four offers on W-E4's screen (600 / 1 200 / 2 400 / 6 000 at
40 000 / 120 000 / 260 000 / 620 000 kr) are authored. **Their precision is not authored**: every ±
is derived from the sample size by `PollingSystem.MarginOfErrorPp`, so the ladder's *shape* — each
point of precision costing more than the last — is real arithmetic and not a design choice.

**What would settle it.** W-F5, sourcing real party campaign finances, plus real Swedish polling
prices if they can be sourced at all.

**The calibration question that survives sourcing.** Even with true prices, whether the player finds
the trade interesting is a play question: if the cheapest poll is always right enough, the ladder is
decoration.

---

## 5. `CampaignCalendar.DefaultPreCampaignWeeks` = 26

**What is known.** The 8-week campaign proper is Sweden's real window and is not a calibration
question. The 26-week pre-campaign is authored so the player has a meaningful build-up "without the
game becoming a spreadsheet for half a year" — a stated guess about attention span.

**What would settle it.** Play. Specifically, whether §3's preparation verbs are interesting for six
months or for six weeks.

---

## 6. `CampaignEconomy.MoneyScale` = 500 000

**What is known.** Chosen so §35's four prose bands fall out of one declared curve (~18 % of the
effect at 100k, ~63 % at 500k, ~98 % at 2m, ~100 % at 10m). The curve's *shape* is the spec's; the
scale is the free parameter.

**What would settle it.** W-F5's real party spending, then play. W-B2 already recorded that a
smaller scale would make the "first krona ≫ millionth" ratio look more impressive **and** would be
the wrong curve — flattening the 500k–2m band the spec explicitly calls moderate.

---

## How to use this list

When the loop exists, take these in order 1, 3, 2, then the rest — magnitude first because
everything else is judged against it, then the dominance mechanism because it changes what a player
does with their hours, then the model question.

Each entry is a **one-line change with a named owner in the code**. That is the point of having
resisted tuning them: the day a human sits down to feel the game, they are not untangling six
interacting fudges.

---

## Addendum to 1 (W-C1, 2026-08-29): the same constant, measured at the real audience

**What is measured.** `CampaignAiHarness` runs an AI-only campaign over Sweden's real electorate
(6.48 million valid votes 2018 as the national audience). Every rational personality delivers
**+197 to +225 compatibility points** over the 56 days (persuasion / 40 000) — and `ElectionScales`
clamps at 100, so every party saturates and the final shares are the clamp's arithmetic. W-B3's
+0.19 pp for a hard week was measured at a 100 000 audience; the chain is linear in audience and in
repetition, so at 6.5 million the same week is 65× that.

**What it means for this entry.** The constant's right value depends on how reach is BOUNDED —
whether the same electorate can be "reached" six times a day by six interviews, whether a channel's
audience is a viewership or the whole country. Those are mechanism questions (W-B9's media
interest; a repeated-exposure decay; W-B4/B11's absolute ground-game reach), and this constant must
not be raised or lowered to paper over them. Entry 1 therefore waits on those items as well as on
play. **Still not tuned.**

## 7. The campaign AI's personality parameters (W-C1)

**What is known.** `PersonalityCatalog`'s five rows — affinities per action, temperature, risk
aversion, optimism, cost weight, spend pace, enthusiasm value, polling cadence, focus, acts-blind,
spend multipliers — are readings of §32's bullet lists as numbers (the full table is in
`ELECTIONS_PROTOTYPE_LOG.md`, W-C1). `RiskScale = 0.25`, `PollingHours = 1`, the candidate bounds
(4 regions, 2 issues) sit beside them. Every one is [AUTHORED-DRAFT].

**What is measured.** In W-B3's placeholder environment the chaotic and populist mixes differ from
every other's (L1 ≥ 0.50); the professional, establishment and grassroots mixes do NOT (L1 ≤ 0.024)
because a free national interview dominates every other action for any expected-value chooser.
That collapse is the environment's, and the harness records it as PENDING on W-B9 and W-B4/B11.

**What would settle it.** Those two mechanisms first — then play, against opponents that feel
different (§32's own bar: "genuinely different"). Whether a populist that spends its whole chest by
week five is fun to beat or merely dead is a play question.

**What must NOT happen.** An affinity raised so the grassroots party knocks doors in an environment
where door-knocking is worthless — that is a number chosen to make a test pass. The `PEND` lines
in `CampaignAiHarness` exist so nobody is tempted: they flip to assertions when the mechanism lands,
with the affinities untouched.

## 8. §11's strategy magnitudes (W-B6)

**What is known.** The five strategies' SHAPES are the spec's own bullets (a loyal group's turnout
lifts under Base Mobilization, a swing group's persuasion under Swing Voter, a focus group's under
Populist, an opponent's standing falls under Negative, Broad reaches more and persuades less per
head); the MAGNITUDES — ×1.15 / ×0.85 / ×0.5, 0.6 and 0.5 per loyalty and swing, 0.7 + 0.8·swing
and 0.3·loyalty, 0.6 / ×0.8 / ×0.9 / ×1.5 / ×1.5, ×1.5 / ×1.3 / ×0.6 — are [AUTHORED-DRAFT].

**What is measured.** In a 30-electorate sweep (W-B3's week under each strategy, the outcome in
the model's own units) Base Mobilization wins 21, Broad Appeal 6, Populist 3, **Swing Voter and
Negative Campaign 0**. Base's lead is the enthusiasm conversion (60 000 per turnout point against
40 000 per compatibility point) as much as its own multipliers; Swing's loyal-group cost outweighs
its swing gain at every loyal share; Negative against a symmetric opponent loses to its own cut.

**What would settle it.** §26/W-D1 deciding how turnout weighs against persuasion at election
time (that conversion is the sweep's metric), W-F4's voter groups (the strategies target groups;
today the electorate is one), then play — is a Swing Voter campaign ever the right call, and does
going negative feel like a gamble or a mistake?

**What must NOT happen.** A Swing or Negative multiplier raised so the sweep table looks balanced.
The done-when asked for no DOMINANT strategy, and there is none; a strategy that never wins is a
measured statement to carry to play, not a bug to tune away.

## 9. The media system's constants (W-B9)

**What is known.** All [AUTHORED-DRAFT]: the news-cycle half-life 3 days (distinct from momentum's
7 by design), `CoverageScale` 1, `MomentumPpPerCoverage` 1.5, the interest weights (coverage 1,
|momentum| 0.15 per pp, polled share 0.8, events 1), the newsworthiness per action (policy
announcement .25, interview .20, rally .15, television .10, town hall .05, digital .05, social .03,
door-to-door .01), `PlatformReach` .55, `FollowingRatio` .30, and the outlet ARCHETYPES (public
broadcaster .45 / 3 slots / threshold .15, commercial television .35 / 2 / .25, tabloid .30 / 2 /
.10, broadsheet .15 / 2 / .30) with their two-group compositions. Real Swedish outlets' reach
(Kantar / Orvesto) and real follower counts are billed as data lines.

**What is measured.** Coverage decays on its curve and cannot spiral (ceiling 4.85); the same
message performs ×1.9–2.0 differently by outlet audience; bookings are availability (a party at
4 % with no coverage is booked nowhere, the same party after a day of news is booked seven times);
over the AI campaign every party is booked 50–81 times in 56 days under the ledger. The compat-
ibility bonuses fell from +197…+225 (everything national, everyone saturated) to +7…+37 once
television, the platforms, a party's following and the press's interest bounded what a national
action can reach — entry 1's saturation finding is now mostly W-B3's placeholder audiences, not
the constant.

**What would settle it.** Sourced reach and follower figures first; then play — does the media
feel like an independent force (does making news get you booked, does a quiet week cost you air)?

**What must NOT happen.** A slot count or threshold moved so a particular personality gets
interviewed; a newsworthiness figure moved so the establishment "feels" like traditional media.
The `PEND` lines in `CampaignAiHarness` name what those claims actually wait on (W-B4/B11, W-B5).

## 10. The ground game's scale and worth (W-B11)

**What is known.** GOTV's operations are [AUTHORED-DRAFT] per contact (a call 3 kr / 0.10 h /
weight 0.5, a door 5 kr / 0.25 h / 1.0, a lift 60 kr / 0.50 h / 3.0, a reminder 1 kr / 0.02 h /
0.25) and `MobilizationScale` 0.5 weighted contacts per eligible voter; the staging gives every
party 800 volunteers (2 400 volunteer-hours a day). W-B3's door-to-door persuasion weight is 0.55
(a town hall's is 1.0, a rally's 0.30).

**What is measured.** With doors counted as the volunteers can knock them (W-B11), a door-to-door
action reaches ~3 000 doors, and at 0.55 per contact it is not worth its five hours against a
post to a party's whole following — so no rational personality knocks doors and the grassroots
party's separation from the media personalities (asserted at W-B9 on the 2 %-of-a-region
placeholder, 16 000 doors an afternoon) is gone: L1 0.20 / 0.17. On election-day GOTV the same
volunteers move a worked valkrets +0.8 to +4.9 points of turnout and the nation +0.32.

**What would settle it.** W-B4's offices (volunteers grow with organisation — 800 is a guess) and
a sourced or played answer to how much a personal contact persuades relative to a broadcast
impression; the literature says a great deal more per contact (Gerber & Green's field experiments
on canvassing are the obvious source — billed, not typed).

**What must NOT happen.** The door-to-door persuasion weight raised so the grassroots party
knocks doors and `CampaignAiHarness` 2a-iv / 2c pass.

## 11. The debate's magnitudes (W-B7)

**What is known.** All [AUTHORED-DRAFT]: `PreparationScale` 12 h, `PreparationFloor` 0.7,
ownership 0.8–1.2, `EventSigma` 4 index points, `CoveragePerMarginPoint` 0.10,
`MomentumPpPerMarginPoint` 0.20 (anchored on §22's "strong debate ≈ +2 pp"), the seven attribute
blends, the clash table, six exchanges, a fixed 8 hours' preparation for every AI, the five plans,
the five unnamed candidates.

**What is measured.** With a 20-point skill gap the stronger debater wins 400 of 400 seeds; the
prepared twin beats the unprepared 400 of 400; the home-ground twin 400 of 400. `ChangeSubject` on
home ground hits the index ceiling of 100.

**What would settle it.** Play: should a weaker debater ever win a debate (a larger `EventSigma`,
or an event term that scales with the stakes), and does a +2 pp swing from one evening feel like
a debate or like a lottery? W-E5's screen is where it will be felt.

**What must NOT happen.** `EventSigma` raised so the harness's 400-of-400 lines look "more
realistic" — a debate's randomness is a design decision with its own reason, to be made against a
screen and a played loop, not against a table.

## 12. The scandal table (W-B8)

**What is known.** All [AUTHORED-DRAFT]: the four severities' base damage (Minor 0.5 pp / 0.02
credibility / 2 days / 0.15 coverage; Moderate 1.5 / 0.06 / 4 / 0.40; Major 3.0 / 0.12 / 7 /
0.80; Catastrophic 5.0 / 0.25 / 12 / 1.50), the seven responses' multipliers (momentum /
credibility / duration / coverage: Deny 0.3 / 0.3 / 1.0 / 1.0, Apologize 1.3 / 0.5 / 0.6 / 0.8,
Explain 0.8 / 0.7 / 0.9 / 0.9, AttackSource 0.5 / 0.6 / 1.3 / 1.5, Ignore 0.8 / 0.9 / 1.5 / 1.1,
Resign 1.6 / 0.2 / 0.7 / 1.4, SacrificeStaffMember 0.7 / 0.5 / 0.8 / 1.0), the escalations (a
caught denial ×6 on the lasting cost, a caught attack ×2, momentum ×1.5 for both),
`SurfaceRatePerDay` 0.15, `EvidenceEstimateError` ±0.25, `CynicalSacrificeMultiplier` 1.6, the
AI's deny-threshold (0.3 as seen) and its responses by personality.

**What is measured.** Over 400 seeds on a MAJOR corruption scandal at evidence 0.5 (damage =
100 × credibility cost − momentum pp): Deny 11.5 ± 9.0 (152 caught out), Apologize 9.9, Explain
10.8, AttackSource 12.3 ± 4.0, Ignore 13.2, Resign 7.2, SacrificeStaffMember 11.7. Against strong
evidence (0.95) the denial averages 16.0, the next worst 14.4; against weak (0.05) 5.3 against
7.2. The worst case (catastrophic, denied, certain evidence, caught) costs 45 % of credibility.

**What would settle it.** Play: is ignoring a story ever the right call (at these numbers never
— the worst mean of the seven); is a resignation ever taken (the AI never resigns; W-C2's
reactivity and W-F6's candidate are what would make it a choice); does a caught denial FEEL
catastrophic on W-E5's screen, or merely expensive. The one change already made — the caught
denial's escalation ×3 → ×6 — was made because at ×3 a caught denial was no worse than ignoring,
which contradicts §17's sentence; it is recorded as the shape the spec demands, not as a fit.

**What must NOT happen.** Any multiplier moved so `ScandalHarness` 2a's pairwise distinctness or
2c's "denial widest" line passes, or so the C1 harness's staged scandal costs a rounder number.

## 13. The tactical layer's doubt and the bloc's willingness (W-A4)

**What is known.** [AUTHORED-DRAFT]: `BeliefSigmaPp` 1.0 pp (fixed from the worklist's own
window before the 2022 run), `MaxLendFraction` 0.15, awareness 0.5 (staging), the need target
T + one belief-sigma, the weights 4P(1 − P) for lending and ((1 − P)(1 − 2P))² for abandonment,
the blocs as staged. SOURCED: the PSU May figures and ± (SCB), CHES `lrgen` for affinity.

**What is measured.** On the 2022 staging L gains +1.18 pp at 3.5 %, +1.00 at 4.0 %, +0.43 at
4.5 %, nothing at 6 %, loses 0.62 pp at 1.5 %. May 2022 → the count: near-threshold error
3.12 → 1.00 pp, the whole vector 13.27 → 10.08. May 2018 → the count: KD 3.09 → 3.84 against
6.42 (a quarter of the rise). The PSU's May cross-tab: M → L 0.7 ± 0.6 % of M's sympathisers,
M → KD 1.0 ± 0.7 % — the lending before the final weeks.

**What would settle it.** A final-week poll of record for 2018 and 2022 (billed) — the size of
the last week's switch, which is what the layer models; the 2026 May PSU against the 2026 count
as the first out-of-sample test of the form; play on W-E1's screen: does a bloc voter FEEL the
threshold, and is lending to 5.0 % (the overshoot at even odds) what a player would do.

**What must NOT happen.** `BeliefSigmaPp` or `MaxLendFraction` moved so 2018's KD reproduces
better — the May → September Δ mixes tactical lending with four months of genuine movement, and
fitting it would put the campaign inside the layer. The form was chosen with the 2022 figures on
the table; the honest next test is one the model has not seen.

## 14. The office's five attributes and the staged plan (W-B4)

**What is known.** All [AUTHORED-DRAFT]: `OpenCost` 100 000 kr, `MaintenancePerDay` 2 000,
`StaffCapacity` 3, `VolunteerCapacity` 150, `RecruitPerDay` 5, `VisitFraction` 0.25,
`StarvationPerDay` 0.10, `LocalPollSampleSize` 300; the C1 staging's plan (grassroots 6, populist
4, professional 3, establishment 2, chaotic 1, in the largest valkretsar) at 2 000 kr a day of
operations each; the harness's 200 headquarters volunteers and 1.5 M kr ground budget.

**What is measured.** An office at capacity: 450 volunteer-hours a day, 1 800 doors a day at
most, 81 900 doors over 60 days on a 10 000 kr operation; mobilisation 58.0 in Stockholms län,
+7 859 votes. Three offices against ten on the same money: 14 248 votes against 0 at 1.5 M kr,
22 534 against 4 087 at 2.4 M, 22 534 against 31 536 at 4 M — the crossover. In the AI campaign
the six-office parties spend 1.67 M of a 2.4 M war chest on offices; the one-office party
278 300.

**What would settle it.** Play: does opening an office feel like a decision (100 000 kr and a
month to full strength against 500 000 for one television buy)? Is 150 volunteers an office or a
committee? Does the crossover at 4 M kr sit where a player with a real budget meets it? The
door-to-door ACTION (entry 10) against the office's own operation: the same door knocked at two
prices — 5 kr and 0.25 h on the office, 15 000 kr and 5 h for 3 000 on the action — is a seam a
player will find.

**What must NOT happen.** `VisitFraction` or `VolunteerCapacity` moved so C1's 2a-ii / 2b lines
(the populist's rallies) pass — they pend on WHERE the offices are sited (W-B5/W-C2), not on how
much an office is worth; `OpenCost` or `MaintenancePerDay` moved so the crossover lands on a
rounder budget.

## 15. The staff's salaries and bonuses, and the manager's plan (W-B5)

**What is known.** All [AUTHORED-DRAFT]: `SalaryPerDay` 1 800 kr (every role alike at the
prototype's depth), `MediaAdvisorReach` 1.20, `DigitalReach` 1.25, `PollsterSample` 1.5,
`FieldOrganizerScale` 1.5 (on the recruit rate AND the capacity), `ManagerFundShare` 0.5; the C1
staging's hires and television buys per personality (professional manager + pollster, 1 buy;
populist manager + digital strategist, 1; establishment manager + media advisor, 2; grassroots
field organizer; chaotic nobody).

**What is measured.** An interview 9 720 → 11 664 persuasion with the advisor; a post 75 854 →
94 818 reach with the strategist; the party's poll ± 2.59 → 2.12 pp with the pollster; an office
150 → 225 volunteers, full on day 29 not 30, with the organizer; the fund 500 000 kr on day 24 of
an even 40 000 kr-a-day release. In the AI campaign the payroll costs a two-hire party 183 600–
190 800 kr over 60 days and every party with fixed costs runs out of money before polling day
(SD 38 of 120 staff-days unpaid; S 10; M 6; V 12).

**What would settle it.** Play on W-E1's screen: is 1 800 kr a day a hire a player weighs against
a 100 000 kr office or a 500 000 kr television buy? Does the establishment's second buy FEEL like
a strategy or like a staged number (the count is the plan's; the plan is staged)? Should the
manager's plan cover the organisation's fixed costs first (finding 2) — and if it does, does the
populist still go broke, which §32 might want? Should the field organizer make an office FAST
(rate ≫ capacity) or LARGE (capacity ≫ rate)?

**What must NOT happen.** A multiplier or the fund share moved so C1's 2a-iii (professional /
establishment) or 2e (television + interview) passes — the first pends on equal money and a plan
that does not react, the second on the media's own interest; neither is a staff bonus. The
staged buys moved so 2e-ii looks less staged than it is.

## 16. Reactivity's thresholds, tempo and weights (W-C2)

**What is known.** All [AUTHORED-DRAFT]: `PublicActivity.HalfLifeDays` 7 (half a visible act's
memory gone in a week); the record's weights — a rally 1.0, a town hall or a canvassing day 0.5;
`DefenceThreshold` 1.0 and `AnswerThreshold` 1.0, each divided by the personality's reactivity, so a
less reactive party needs proportionally more before it moves and reactivity 0 never moves;
`OfficeUpkeepDaysReserved` 10 (a party opens an office in a contested region only if it can also keep
it ten days); `DefenceCooldownDays` 3; `AnswerCooldownDays` 7; `PressuredRegions` 2 (how many
contested regions enter the AI's own candidate list); and §32's reactivities — professional 1.0,
establishment 0.7, grassroots 0.6, populist 0.5, chaotic 0.0.

**What is measured.** Against a scripted player working one small valkrets for forty days: the
professional puts 8 of its 9 local acts there (0 of 8 in the control) and opens an office there in
seed 777; the establishment 4 of 8; the chaotic party 0 of 598. First act a mean 2.0 days after the
script begins, in 8 of 10 seeds. Answers: the professional 6.9 → 7.0 a campaign — **at the weekly
cooldown's ceiling in both arms**, so the count is tempo-bound and what the attack changes is whom it
answers; the chaotic party 0.0 in both; the establishment never crosses its own threshold (0.4 → 0.0)
because the negative campaign is aimed at the polled leader. Local acts a campaign: professional 0.9,
establishment 0.8, chaotic 59.8.

**What would settle it.** Play. Does a three-day defence cooldown feel like an opponent noticing you,
or like an opponent shadowing you? Is a week the right silence before a party answers an attack —
and should the answer be visible to the player as an answer, or only as a message? Is 10 days of
upkeep the right caution before an AI plants an office in a region you are working, or should it
need to believe it can hold the region to polling day? Should a *professional* break its flat-affinity
tie on cost (the cheapest defence) rather than on §12's order (a rally)?

**What must NOT happen.** A threshold, a cooldown, a reactivity or an affinity moved so C1's 2a-iv
(est/grass 0.291) crosses 0.300 — the line pends on **unequal money and unequal paces (W-F5)**, which
is what separates two parties that both react, and moving any of these to clear it would be tuning a
constant to open a gate. **RULED 2026-08-30 (Elias): the line stays at 0.291. Nine thousandths is
precisely the distance at which a threshold move stops being calibration and becomes making the
test pass. If W-F5 lands and it still reads 0.291, that is a FINDING ABOUT THE MODEL to report and
explain, not a rounding problem to close.** The half-life or the act weights moved so a particular party reacts in a
particular scenario. The public record given anything a party could not see — a share, a preference,
a true salience, or the doors an office knocked.

## 17. Coalition compatibility, the red-line gaps, and what a party is holding out for (W-D3)

**What is known.** All [AUTHORED-DRAFT]: `CoalitionCompatibility.WeightIdeological` 0.55 /
`WeightPolicy` 0.45; `DerivedRedLines.IdeologicalGap` 4.5 on CHES `lrgen` and `SocialGap` 5.0 on
`galtan`; the formation's ranking weights `WeightCohesion` 0.5 / `WeightSeatStrength` 0.3 /
`WeightPower` 0.2; `DefectionMargin` 0.01. **The two gaps were chosen knowing Sweden 2022's answer.
They are a calibration on that case, not a prediction of it, and the log says so.**

**What is measured.** The 2022 chamber returns the government that actually formed: cabinet M+KD+L
(103), carried from outside by SD (73) = 176 of 349, cohesion 88.9, as confidence-and-supply. 120
arithmetic majorities are refused by red lines, S+SD (180) among them. Banzhaf pivotality: S 34.5 %,
SD 23.6 %, M 23.6 %, and V/C/KD/MP/L 3.6 % each. A single `lrgen` threshold in **[1.79, 2.58)** —
window 0.79 — separates the four parties that refused SD from the three that governed with it.
Dropped one at a time, the C ↔ SD declaration changes nothing (the galtan rule already reaches C at
6.05 > 5.00) and the no-SD-ministers declaration changes everything.

**What would settle it.** Play, and a second country. Does a coalition negotiation the player
watches feel like arithmetic or like politics — and does the player understand WHY a government they
wanted was refused? Is 0.55/0.45 the right split between "where a party stands" and "what it wants
this year", or should policy lead in a campaign and ideology between them? Should the ranking weigh
cohesion at all once defection has removed the unstable governments, or is defection doing the whole
job? And the real test of the derived gaps: **do 4.5 / 5.0 produce sane refusals in Germany, Italy or
Poland**, whose CHES rows are already on disk — because a gap fitted on Sweden and applied unchanged
elsewhere is the first place this will break.

**What must NOT happen.** Either gap moved so a particular coalition forms or fails in Sweden —
that is fitting a known answer twice over. A per-pair compatibility table, or any number attached to
a pair of parties by hand: §29's whole discipline is that compatibility, red lines and negotiating
power come from party data, and a hand-set pair value is the authored coalition score the item
exists to avoid. Leader compatibility or personal relationships filled in with game fiction to
"complete" §29 — they are deferred for want of a source, and the harness asserts the absence.

## 18. The organisation's bill, and what a plan is allowed to hold back (W-B12)

**What a player will feel.** Hire a manager and the campaign stops running out of money in the last
fortnight. That is the whole felt difference, and it is deliberately the ONLY difference — the
manager does not spend better, does not pick better actions and does not raise more. **The manager
keeps the organisation's bill back before the pace is allowed to release anything.** A party without
one still goes broke, and should.

**The numbers, and what each rests on.**

| figure | value | class | why this and not another |
|---|---|---|---|
| `DailyFixedCost` | payroll + every office's maintenance and operation | **DERIVED** | Summed from `SalaryPerDay` and `MaintenancePerDay`, which W-B4/W-B5 already set and logged. **W-B12 introduces no new constant** — deliberately, so it could not be accused of buying its own done-when. |
| recomputed | **every morning** | DERIVED | A party that hires today has a different bill tomorrow. Setting it once would let a mid-campaign hire go unfunded, which is precisely the defect being fixed. |
| horizon | `daysLeft` to polling day | DERIVED | §9's plan is a plan to an end date. Any shorter horizon reintroduces the bug at the tail; any longer is money the campaign will never need. |
| capital | **NOT in the daily bill** | ⚠ a judgement, logged | An office's 100 000 kr opening and a planned television buy are one-off commitments. Folding them into a per-day figure would misstate both — and **this is the choice SD's residual six days sit on.** Named here so it can be reversed knowingly. |

**What to feel for, and the honest doubt.** Play the populist. SD is the one managed party that
still ends with unpaid staff-days (6, from 38), because `spendPace` 1.6 is the most front-loaded of
the five and four offices' opening capital lands early. **The question for the seat is whether that
feels like a populist overreaching — which is characterful — or like the game failing to warn you.**
If it is the second, the fix is a *warning*, not a bigger reserve: a manager who silently prevents
every mistake removes the decision §9 exists to pose.

**And one number moved that nobody predicted.** C1's `prof/est` separation went **0.061 → 0.306**,
crossing 0.30 for the first time, on an item that touched no affinity and no weight. W-B5 had sent
that convergence to W-F5 expecting *unequal money* to separate the two rational planners. It was not
money — **it was the plan itself**, a planner and a non-planner spending differently on equal chests.
Meanwhile `est/grass` moved the WRONG way, 0.291 → 0.269. Both are reported, neither is tuned.

## 19. Election night's clock, and the two thresholds a call rests on (W-E6, backfilled)

**What a player will feel.** The results arrive over four hours, constituency by constituency, and
the calls come when they are safe rather than when they are exciting. **They will feel late.** That
is the design, and this entry exists so that when it feels wrong, the numbers to argue with are
here.

| figure | value | class | why this and not another |
|---|---|---|---|
| `NightMinutes` | 240 | ⚠ **[AUTHORED-DRAFT]** | Four hours from first declaration to last. Sweden's real count runs longer and the preliminary result lands sooner; four hours is a PLAYABLE evening, not a measured one. |
| `FirstDeclarationMinute` | the schedule's floor | DERIVED | Monotone in electorate — small constituencies declare first, which is what actually happens and needs no constant. |
| `ScheduleJitterMinutes` | seeded, bounded | ⚠ **[AUTHORED-DRAFT]** | So two nights on two seeds do not arrive in identical order. Bounded so the monotonicity above survives it. |
| the call rule | safe at BOTH extremes of the outstanding eligible bound | DERIVED, **structural** | **Not tunable.** A call is made only when it holds whichever way every uncounted vote falls. 1834 call-instants over 8 seeds, **0 contradicted**. |

**What to feel for.** The guarantee calls **L (the smallest party over the line) and the bloc
majority only at 29 of 29** — the last constituency. If that reads as broken rather than careful,
the fix is a SOURCED turnout ceiling that would narrow the outstanding bound, not a softer rule. That
rider was deliberately not taken.

## 20. The chamber, the election, and what changed for a player at W-G1 (backfilled)

**What a player will feel.** Three things, and the third is the one to watch.

1. **The parliament is real.** Sweden's Riksdag is 349 seats held by S, SD, M, V, C, KD, MP and L —
   not 200 seats held by four invented parties. Every other country likewise.
2. **The chamber stops twitching.** It used to drift a few seats every turn off the government's
   approval. It now changes **only at an election**, which is what parliaments do.
3. ⚠ **And therefore: between elections, nothing about the chamber moves at all.** If that reads as
   dead rather than stable, the answer is by-elections, defections and splits — §29 territory — not
   a return to drift.

| figure | value | class | why |
|---|---|---|---|
| chamber sizes | 349 / 630 / 577 / 400 / 460 / 435 | **SOURCED** | Each country's own returns file; each reconciles exactly. |
| seats at seed | that country's last real election | **SOURCED** | A new game starts in the chamber the country actually has. |
| fiscal stance | `(5 − lrecon) / 5` | **DERIVED** | Replaces four hand-set placeholders. Linear and stated so it can be checked in one step. |
| party ink | published hue, desk saturation 0.52 / value 0.46 | **hue SOURCED, S/V a stated derivation** | Measured off the four inks it replaces (0.23–0.58, 0.35–0.49). |

⚠ **The one number to distrust.** A German game's first election seats **BSW at 91 and the FDP at
47 — both really won zero.** Both missed the 5 % threshold by under a point (BSW by 0.02 pp), and the
model carries ~1.5 pp of error, so it lands on the wrong side of that cliff about as often as the
right one. **A threshold is where this model is weakest.** Sweden has no party that close to its 4 %
line and lands within a seat or two everywhere. Nothing was tuned; this is the finding, not a bug to
fix by moving a number.