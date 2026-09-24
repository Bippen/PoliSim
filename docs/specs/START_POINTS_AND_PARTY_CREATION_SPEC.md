# PoliSim — Start points and your own party (spec + work plan for Code and Design)

**Status: design, ruled in principle by Elias (2026-09-25).** Extends `POLITICAL_SYSTEM_SPEC.md` — the
world clock, the four roles and the per-country stages there are this document's foundation. The
decisions in §7 are the architect's recommendations and stand unless Elias strikes one. Every date,
rule and figure below is sourced by Code before use; this document names what to look up, never a datum
to type.

**The two references Elias gave:** Hearts of Iron IV's scenario screen (a row of dated start cards, a
*Brief History* panel under them, Back and Select) and F1 Manager's *Create a Team* (numbered origin
presets 01–06, each with a story and a stat profile shown as five-step quality pips, a step indicator
across the top, and a *Next Step* control).

---

## 1. Start points — choose the election you begin before

### 1.1 The rule

After choosing a country, the player chooses **which election to begin before**. A country offers one
start point per **important, popularly decided national election**, the latest of each kind. Where a
country has both a presidential and a parliamentary election — Poland, France, the USA — the player
chooses which contest to fight. Elections decided indirectly (Germany's and Italy's presidents, chosen
by assemblies) are not start points.

| country | start points (latest of each kind — dates sourced by Code) | the player's role at the start | status |
|---|---|---|---|
| Sweden | Riksdag, September 2026 | a party | playable after political-system stage 2 |
| Germany | Bundestag, February 2025 (snap) | a party | its stage (4) |
| Poland | Sejm, October 2023 · **presidential, May–June 2025** | a party · **a presidential candidate** | Sejm at its stage (5); presidential after the two-round model (§4, stage S7) |
| Italy | general election, September 2022 (snap) | a party | its stage (7) |
| USA | presidential, November 2024 | a presidential nominee (or your own, §2.6) | its stage (6) |
| France | **presidential, April 2022** · legislative, June–July 2024 (snap) | a presidential candidate · a party | presidential after the two-round model (§4, stage S8); **legislative locked** — its 577-constituency system is not modelled (R-EL10) |

A later option, not planned here: the US midterms as their own start point, with the player as a
congressional party's leader.

### 1.2 The screen

One card per start point, in date order, as in the HOI4 screen: the date, the kind of election, and the
card's state — **playable**, or **locked with its reason in one line** ("its electoral system is not yet
modelled"). Selecting a card fills the **brief** panel beneath; **Back** returns to the countries,
**Select** opens the party screen (§2) with the world set to that start's date.

### 1.3 The brief — derived, so it cannot go stale or invent

The brief is **generated from the sourced records of that date**, not written by hand: who governs and
since when, with how many seats of how many; the last result; the polling day; the parties in the
chamber. Every clause traces to a record Stage 1 of the political-system spec builds. The shape, with
the data filling the slots:

> *Sweden, [start date]. [Government of record] has governed since [date], with [seats] of [chamber
> size] seats. Polling day is [date]. [n] parties sit in the Riksdag.*

**An optional tagline per start point** — one line of scene-setting in the HOI4 manner — is permitted as
labelled game fiction, reviewed by Elias, and never states a fact the record doesn't hold.

---

## 2. Create your own party

### 2.1 The flow

Alongside the real parties on the party screen sits **Create a party**. It opens a stepped flow, the F1
Manager way — a step indicator across the top, **Next step** at the corner, **Back** always available:

1. **Profile** — name, short name, mark, ink, and origin (§2.2).
2. **Positions** — where the party stands (§2.3).
3. **Declarations** — who it will and will not govern with (§2.4).
4. **Leader** — the party's leader and their attributes (§2.5).
5. **Review** — the whole party on one card, then **Start**.

### 2.2 Origins — six presets, each a story and a stat profile

Like F1 Manager's origins 01–06: numbered cards, a short story in a side panel, and a **quality block of
five stats shown as five-step pips.** The stories are game fiction about a fictional founding, never a
real person. **Each stat is a real parameter of the campaign engine, not decoration:**

| stat (pips 1–5) | what it sets in the engine that already exists |
|---|---|
| **Funding** | the opening war chest (W-B2's resources) |
| **Organisation** | offices and staff at the start (W-B4, W-B5) |
| **Recognition** | the baseline of media interest and name awareness (W-B9) |
| **Activists** | volunteers and door-to-door capacity (W-B11) |
| **Leader** | the leader's attributes in debates and appeal (W-B7, §16) |

The six origins, each with its own rule:

| # | origin | its profile | its special rule |
|---|---|---|---|
| 01 | **Grassroots movement** | high Activists, low Funding and Recognition | none — the pure newcomer; the hardest start |
| 02 | **Splinter party** | the parent's Recognition, low Organisation | choose a parent party: begins at the parent's positions (adjustable within a range), **inherits a chosen slice of the parent's last result as its prior**, and may take a number of the parent's MPs into the chamber on day one; the parent declares against it |
| 03 | **Protest movement** | high Recognition (conflict draws coverage), low Organisation | positions locked toward the people-versus-elite end |
| 04 | **Business-backed technocrats** | high Funding, low Activists | none |
| 05 | **Single-issue party** | middling, with one issue chosen | its chosen issue carries raised salience among the voter groups that care about it, and little elsewhere |
| 06 | **Regional party** | strong Organisation in one region | its support concentrates in the region chosen, through the regional layer (§27) — which already pays exactly where support is regionally confined |

The pip-to-parameter values are **`[AUTHORED-DRAFT]` game design**, each with its line, and go on the
play-calibration list — only play can judge whether a Grassroots start is an uphill battle or a wall.
Origins are presets; a free point-buy is a later option, not planned here.

### 2.3 Positions — placed against the real electorate

The player places the party on the **compass** the game already draws (CHES economic left–right against
GAL–TAN). The other CHES dimensions **default from that placement by the relationship the 53 real
parties show** between the compass axes and each other dimension — a derived default, not an authored
one — and an **Advanced** panel adjusts each. While placing, the screen shows **the nearest real parties
and where the electorate sits** (the voter groups over the cohorts), so the player can see where the
votes are and who they would be competing with.

### 2.4 Declarations — the formation rules, chosen by the player

The same declarations the formation model already reads, now set by the player: **red lines** against
chosen parties (symmetric, or one-way as RedLine.OneWay), **a prime-ministerial candidacy** or not, and
**whether the party will support a government from outside** (V's rule, or its opposite). Declarations
are dated at creation and bind the party's behaviour in formation, as every party's do.

### 2.5 The leader

A name the player writes, and the leader's attributes shown as pips — the attributes the debate and
appeal models already use (§16), set by the Leader stat and adjustable within the origin's budget.

### 2.6 In the USA

The two-party Electoral College makes a new national party a different proposition, so the USA offers
two forms: **your own nominee** — a leader the player creates, at the head of the Democratic or
Republican ticket, with positions inside that party's range; or **a third-party run** — the Grassroots
origin in the hardest form, where the Electoral College shows honestly how far a third party is from a
single electoral vote.

### 2.7 How the model treats a new party — using its own logic

- **Votes:** a new party has no electoral history, so it has **no loyal base**; the vote model's loyalty
  derivation (from the two previous elections) gives it nothing to hold, and it competes only for the
  share of other parties' voters who are *not* loyal to them. That is the model's own logic, not an
  authored penalty — and it is why the Splinter's inherited slice (02) and the Regional concentration
  (06) matter.
- **The threshold** applies as it does to every party; missing it means no seats.
- **Party funding follows each country's real rule**, sourced — where state support is paid per seat or
  per vote above a line, a party below it starts and stays without it.
- **Getting on the ballot:** where a country requires a new party to collect signatures or register
  before it may stand, that becomes a **run-up task** for a created party, from the country's sourced
  rule — a real hurdle, and real play.
- **Formation, campaigns, reactivity, election night, the hemicycle, polling and media** treat the new
  party as any other; its declarations and positions are what the formation reads.

### 2.8 Mark and ink

The mark is chosen from **the unspent cells of Design's fifty-cell mark sheet** (the hatched half and
the split and spine cuts — 35 cells already delivered and held in the pack), shown as a picker; no new
art is needed. The ink is chosen from a palette, and **the fence the parliament's inks already pass runs
live**: an ink too close to a party in the same bloc is refused with the measured distance shown, so a
created party can never be the S/V collision again.

---

## 3. How it plugs into the political system

- **Start points need Stage 1** of the political-system spec (chambers, governments and leaders of
  record by date): a start point is a country, an election, and the date that election's run-up began.
- **A created party needs Stage 2** (the whole Swedish loop) **and Stage 3** (the roles): a newcomer will
  most often live as an opposition party or a support party before it ever leads — the support party is
  the newcomer's natural road to power.
- **Every country's own stage unlocks its start points and its created parties together**: a created
  party works wherever the country's elections work.
- **The two-round presidential model** (§4, S7–S8) is new: a national popular vote in two rounds, the top
  two meeting in a runoff. It is far smaller than France's legislative system, which needs 577
  constituency contests — which is why France's *presidential* start becomes reachable before its
  legislative one does.

## 4. The work plan — Code

Each stage lands whole with its evidence; any stage that moves a trajectory is its own BASELINE family;
two families never share a pass. **These stages run after the political-system spec's Stages 0–3.**

**S0 — install and map (records only).** Install this document under `docs/specs/`. Collision map: every
place the code assumes the party list is fixed and real — the party catalog, marks and inks, loyalty from
history, declarations, the party picker, the save's party key. Sourcing bill (§8), sized.

**S1 — start points as data, and the start screen.** Per country, the start points of §1.1 with their
dates and trigger rules, reading the by-date records; the screen of §1.2 built structurally in the v3
idiom, locked cards with their reason. It is a Canvas surface, so real films per the Canvas clause.
**Done when:** every country offers its start points, each opens the world on its own date, and locked
cards say why.

**S2 — the derived brief.** The generator of §1.3 fed from the records, with a check that every clause
resolves to a sourced record; the optional tagline slot, labelled as fiction. **Done when:** every
playable start point shows a brief that traces clause by clause to the record.

**S3 — the created party in the model.** A `CreatedParty` type — name, a display name and an ASCII key
(the diacritics lesson), mark, ink, origin, nine CHES positions, declarations, leader, stats — saved and
loaded with the game. The origins' presets and special rules as data; the pip-to-parameter table as
`[AUTHORED-DRAFT]` with its lines. The party enters the vote model with no loyal base (§2.7), the
allocator, the formation, the campaign, the ballot-access task and the funding rule. **Asserts:** a
created party placed exactly on a real party's positions with that party's stats competes for that
party's voters; a Grassroots newcomer polls near zero until it campaigns; a Splinter begins at its
inherited slice. **Done when:** a created party can fight a Swedish election end to end and every
number it gets traces to the engine.

**S4 — the creation flow, structurally.** The five steps of §2.1 in the v3 idiom: the origin cards with
their stories and pips, the compass placement with the nearest parties and the electorate shown, the
Advanced sliders, the declarations picker, the leader, the review card; the mark picker over the unspent
cells and the ink picker with the live fence verdict. Canvas surfaces, real films. **Done when:** a player
can create a party and start a game with it, and a colliding ink is refused on screen with its distance.

**S5 — created parties in every country as its stage lands**, including the origins' country-specific
rules (thresholds, funding, ballot access, regions).

**S6 — the USA's two forms** (§2.6), with the USA's stage.

**S7 — the two-round presidential model, and Poland's presidential start.** A national two-round popular
vote; the president's role in a parliamentary republic — the veto and its override, and cohabitation with
the Sejm of record. **S8 — France's presidential start**, on the same model, with its National Assembly
seated from the records and **no dissolution until the legislative model exists**, said plainly on
screen.

**Size:** S0–S4, which make start points and created parties playable in Sweden, about 8–12 sessions
once the political-system stages 0–3 have landed; S5–S8 grow with the countries.

## 5. The work plan — Design (ask D23)

**Asked the D19 way — against the built flow after S1 and S4, not against this text.** One ask, one
annex set, one zip. Rows:

1. **The start screen** in the desk's idiom, from the HOI4 reference: the start cards (date, kind,
   playable or locked with its reason), the brief panel, Back and Select. **Card imagery is Design's
   call** — the architect's suggestion is imagery the game already derives (the flag, the date as a
   stamp, a small hemicycle of the chamber of record in its inks) rather than illustrations; if Design
   wants illustrated cards, that is a costed case with the asset count stated.
2. **The creation flow**, from the F1 reference: the step indicator, the origin cards 01–06 with the story
   panel, the five-stat quality block as pips (aligned with the stepped rule the game already uses), the
   compass placement with the nearest parties and the electorate, the declarations picker, the leader, and
   the review card.
3. **The mark and ink pickers**: the unspent mark cells as a picker, and the ink palette with the fence's
   live verdict and its distance.
4. **A created party across the existing screens** — the hemicycle, the party screen, election night, the
   coalition screen — confirming its mark and ink sit with the real parties.
5. **Locked states**: how a locked start point and an unavailable origin (a Regional party in a country with
   no regional layer yet) read.
6. **Assets, if any**: origin emblems are **not** assumed — the origin cards can carry their number and
   pips as F1's do; any illustration is Design's costed case.

## 6. Order across both specs

Political-system stages 0–3 (the world clock, Sweden end to end, the roles) → S0–S2 (start points) →
S3–S4 (created parties in Sweden) → D23 against the built flow → the countries' stages, each unlocking its
start points and created parties (S5, S6) → the two-round presidential model (S7, S8).

## 7. The decisions, each standing unless struck

1. **Start points are the latest of each important, popularly decided national election**; indirectly
   elected presidents are not start points.
2. **Locked start points are shown, with their reason**, rather than hidden.
3. **The brief is generated from the records**; taglines are optional labelled fiction.
4. **Origins are six presets**; each stat sets a real engine parameter; the values are `[AUTHORED-DRAFT]`
   and go on the play-calibration list; point-buy is a later option.
5. **A new party has no loyal base** — the model's own logic, not an authored handicap; only the Splinter
   inherits a prior, derived from its parent's result.
6. **Default positions beyond the compass are derived** from the real parties' relationships.
7. **Created parties exist only in the player's country.**
8. **Marks come from the unspent sheet cells; inks pass the live fence.**
9. **Funding and ballot access follow each country's real rules**, sourced.
10. **The USA offers your own nominee or a third-party run.**
11. **The two-round presidential model unlocks Poland's and France's presidential starts; France's
    legislative start stays locked.**

## 8. The sourcing bill

Each start point's election date and, for snap elections, its trigger date (from the political-system
bill); per country, the rules for **new-party registration and ballot access** and for **state party
funding**; Poland's and France's two-round presidential rules and the 2025 and 2022 returns by round;
Poland's presidential veto and override; France's presidential powers and the dissolution rule;
the US nominee and third-party ballot-access rules by state. **Nothing typed from this document; every
item fetched, verified and dated, or billed.**

---

## 9. The main menu and settings (addendum, 2026-09-25)

**9.1 The main menu — the game's first screen.** Continue (the most recent save; absent when there is
none), New game (→ the countries → the start points → the party screen), Load game (the saves screen
that exists), Settings, Quit. The title and the desk's own materials; no text beyond the items and the
title. A Canvas surface, so real films.

**9.2 Settings — only what is real.** Every setting must change something the game measurably does; a
toggle that does nothing is not shipped (the lever-liveness rule applied to preferences). **And every
setting is pinned by the film harness and restored when the harness finishes** — preferences leaking
into films has cost twice already (the maximized Game view filming 962-pixel frames; the PROVENANCE
preference flipping later films into false overflows), which meets R-N5's bar for a check: **a check
fails any preference the harness does not pin.**

- **Audio:** master volume and mute, moved here from the head of the Saves screen.
- **Display:** window mode (windowed, borderless, fullscreen) and resolution, offered only at the
  geometries the harness films; the 1280×720 minimum stands. **No free UI-scale slider** — every layout
  guard assumes the filmed type sizes, so scaling is a Design and film question, not a setting.
- **Game:** the default speed; which interrupts hold the clock (the campaign opening, election night,
  budget windows — the ones that exist); the provenance (†) default; autosave frequency and slots.
- **Named, not built:** language (the game is English); colour-vision modes (a palette question for
  Design); key rebinding.

Preferences are stored as player preferences, outside the save; a save never carries them.

**9.3 Stages.** **M1** — the main menu and its flow, wired into the existing selector; filmed real.
**M2** — the settings screen: audio moved in, display and game added; each setting proved to change
what it names; the pinning check armed and proved both ways.

**9.4 Design** — two rows join the Design asks: the main menu (the game's first impression; key art is
Design's costed case, not assumed) and the settings screen, both asked against the built screens.
