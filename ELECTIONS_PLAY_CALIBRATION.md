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
