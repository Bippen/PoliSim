# Step A — published series design

Written while the Editor is unavailable. A document changes no simulation code, so it does not
invalidate the requirement that the pre-change baseline be captured before Step A's first line of code.

Grounded in `STEP_A_LIVE_VALUE_AUDIT.md` (55 live reads across 11 simulation files) and the seed data in
`POLISIM_SEED_DATA_MACRO_OVERHAUL.md`.

---

## 1. The shape, and why

```
Country
├── State : EconomyState        ← LIVE. 29 fields. Untouched by Step A.
└── Published : PublishedData   ← NEW. UI reads this. Simulation never does.
```

The audit's central finding drives this: every simulation call site reads `country.State.X`. Keeping
published values off `EconomyState` entirely makes a leak a **compile-time impossibility** rather than a
review obligation across 55 sites. Any diff to `EconomyState.cs` beyond comments is therefore itself
evidence the design drifted — a free check that runs before the expensive trajectory comparison.

## 2. Data model

```
PublishedData
└── Series : Dictionary<StatId, PublishedSeries>

PublishedSeries
└── Entries : List<PublishedEntry>       (append-only, ordered by publication date)

PublishedEntry
├── ReferencePeriodStart : DateTime      what the figure MEASURES
├── ReferencePeriodEnd   : DateTime
├── PublicationDate      : DateTime      when the player could first SEE it
├── Value                : float
└── Status               : Preliminary | Revised | Final
```

Reference period and publication date are separate fields, not derived from each other — that separation
*is* the reporting lag, and it is what Step B's graphs draw as the gap between "period covered" and
"release point".

Revisions **append**; they never mutate an existing entry. A player who acted on a preliminary figure
must still be able to see what they were looking at when they acted. Same reference period + later
publication date + `Revised` status = a revision of the earlier entry.

## 3. Publication rules (from the seed file, as rules not dates)

| Country group | Stat | Rule |
|---|---|---|
| USA | Unemployment | First Friday monthly |
| USA | CPI | ~12th monthly |
| USA | GDP | advance t+30, second t+60, third t+90 after quarter end |
| EU five | Inflation | flash last working day of reference month; full ~t+17 |
| EU five | GDP | flash t+30, t+45, then ~t+65 and ~t+110 |
| All | Annual stats (poverty, population, demographics, crime, infrastructure) | once yearly |

USA GDP is the clearest revision case: three entries for one reference quarter, the first two
`Preliminary`, the third `Final`.

## 4. Revision generation

The preliminary is a **noisy early estimate of the true value**, not an independent random number:

```
preliminary = trueValue + noise
revised     = trueValue          (or closer to it, for multi-stage revisions)
```

This matters for honesty — the directive asks that revisions be "small and plausible, not arbitrary".
Deriving from the true value means a revision always moves *toward* reality, which is what real revisions
do and what makes acting on a preliminary figure a genuine, fair risk rather than a coin flip.

**Noise draws from `SimulationRandom`** (Step A0), so a seeded run publishes identical figures — without
which the identical-trajectory proof would be defeated by the very feature being added.

## 5. Tier 0 derived stats — display-time arithmetic only

GDP per capita, tax burden % GDP, spending % GDP, deficit % GDP, real GDP growth, sector shares. No
stored state, no new ceilings. Each computed from already-tracked values at the moment of display.

Open question for implementation: derive these from **live** or **published** inputs? They are shown to
the player, so published is consistent — but GDP per capita computed from a published GDP and a live
Population would mix vintages incoherently. **Recommendation: compute from published inputs throughout,
and only for stats whose inputs are all published.** Flag any that cannot be, rather than silently
mixing.

## 6. What proves this correct

1. `EconomyState.cs` unchanged beyond comments.
2. No simulation file gains a reference to `Published`. Greppable, and worth doing as a literal check.
3. Seeded before/after runs produce **identical** per-turn values — compared as values, not anomaly
   counts.

(1) and (2) are cheap and catch design drift immediately; (3) is the real proof but only means something
once determinism is demonstrated, which is still outstanding.

## 7. Not decided yet — for Elias when implementation starts

- **Which stats get published series in Step A?** The directive implies all tracked stats, but the
  release calendar only specifies rules for unemployment, CPI/inflation, GDP, and the annual group.
  Recommendation: publish only those with real specified rules, and leave the rest reading live in the
  UI for now — inventing a release cadence for `ConsumerConfidence` would be exactly the kind of
  fabrication the seed file's `[GAP]` discipline forbids.
- **What does the UI show before a stat's first publication?** Early game, no entry exists yet.
  Recommendation: show the seeded starting value marked as such, rather than a blank or a zero.
