# PoliSim — The political system: start before the election, play any role

**Status: design, ruled in principle by Elias (2026-09-24).** The numbered decisions in §10 are the
architect's recommendations; each stands unless Elias strikes it. Built in the stages of §9, one
BASELINE family per pass. Every date, rule and figure below is sourced by Code before it is used —
where this document names a fact (a date, a constitutional rule), it names what to look up, not a
datum to type.

---

## 1. The idea

**Every game opens in the real run-up to the player's country's most recent election.** The chamber,
the government, the parties, their leaders and their declared positions are as they stood on that
day. The player picks a party and campaigns. From the first vote onward, the game is the player's
history, not the world's — and the world's is kept beside it, so after election night the player can
see what the country actually did.

Winning an election is not the only way to govern. In a parliamentary country the player's party can
lead the government, sit in it as a junior partner, carry it from outside as a support party, or
oppose it — and each of those is a playable role, not a game-over. In the United States the player is
the party's presidential nominee, and the office is the presidency.

## 2. What this changes in what is already ruled

| ruling | what happens to it |
|---|---|
| K-1 (3): the start moved to 1 October 2026, after Sweden's election | **Reversed.** Sweden starts in the run-up to 13 September 2026. K-1's work is not lost: the 2026 declarations and leaders are dated and take effect on their dates during the run-up; the real 2026 result becomes the reference (§7); the formation rulings (K-1f, K-1h) govern the formation after the player's election. |
| D-5 (a): lose-only — out of cabinet ends the game | **Replaced** by the four roles (§5). |
| OP-1: continue in opposition, or end? | **Resolved: continue**, in any role. |
| CL-4 / DS-7: polling day on the calendar's own date | **Required.** Elections fall on their real dates, and snap elections mid-turn must work. The day loop already runs the campaign; the incoming-government budget window (P-B2) already opens on arrival. |
| D-22: Germany's personality cast, waiting on Sweden being played | **Resolved differently** (§10 decision 5): every country's AI party casts are derived from each party's published CHES position, not hand-characterised. |
| R-EL10: France structurally out of scope | **Stands** (§8). |
| R-EL11: Italy's sub-national Rosatellum stages before Italy is playable | **Now required** for Italy (stage 7). |

## 3. One world, one clock

The game simulates six countries in one world on one calendar, so the start date cannot differ per
country inside a single game. **The start date is set by the country the player chooses.** On that
date:

- **Every country is seated in its chamber of record and governed by its government of record** — the
  parliament and cabinet actually in office on that date, sourced and dated. Much of this data is
  already on disk from the backtests (the T−1/T−2 catalogs for Sweden, Germany, Poland and Italy).
- **Every country holds its own elections on its own real calendar from then on**, simulated by the
  vote model, the seat allocator and the formation model. History diverges for all six from the start
  date, not only for the player's country.
- **Statutes read the calendar year**, as they already do (pension ages, carbon schedules), so every
  statute is true for the chosen date.
- **Economic seeds keep their sourced vintage.** A 2022 start (Italy) runs economies seeded from
  2023–24 data; the offset is stated per country in the record, not hidden and not re-seeded — re-
  sourcing six economies per start date is out of proportion to what it would change.

## 4. The six countries

| | executive | chamber(s) the game seats | latest election | the start (to be sourced and dated) | seats allocated | campaign | status |
|---|---|---|---|---|---|---|---|
| **Sweden** | prime minister; negative parliamentarism | Riksdag, 349 | 13 Sep 2026, scheduled | standard run-up before a scheduled election — the protocol's original run-up in January 2026 | exact | built | **stage 2** |
| **Germany** | chancellor; constructive vote of no confidence | Bundestag, 630 | 23 Feb 2025, snap | the day the snap election became inevitable — the coalition's collapse in November 2024 | exact (2025) | Länder catalog and cast owed | stage 4 |
| **Poland** | prime minister; directly elected president with a veto | Sejm, 460 (Senate stated absent) | 15 Oct 2023, scheduled | standard run-up | exact (41 districts) | districts and cast owed | stage 5 |
| **USA** | president via the Electoral College | House, 435; **Senate, 100** (new) | 5 Nov 2024, scheduled | standard run-up | exact (EC, with ME/NE) | states and cast owed | stage 6 |
| **Italy** | prime minister; confidence of **both** chambers | Camera, 400; **Senato, 200** (new) | 25 Sep 2022, snap | the day the government fell in July 2022 | PR stage exact; sub-national owed | collegi and cast owed | stage 7 |
| **France** | semi-presidential | Assemblée nationale, 577 | 30 Jun / 7 Jul 2024, snap | — | **not modelled** | — | §8 |

**The start rule:** a scheduled election starts at the standard run-up (Sweden's 26-week run-up plus
8-week campaign, applied to the country's own polling day); a snap election starts on the day it was
triggered, with whatever campaign that leaves — a short, sharp campaign is what those elections were.
The government of record at each start (Kristersson's M+KD+L with SD's support; Scholz's minority
after the coalition broke; Morawiecki's PiS government; Draghi's caretaker government; Biden's
administration and the 118th Congress) is **verified and dated by Code from primary sources**, not
taken from this line.

## 5. Parliamentary countries — the player's party and its roles

### 5.1 Choosing a party

Any party seated in the chamber of record at the start, plus any party that won seats in the latest
election (so a newcomer can be played). The party's leader of record at the start is the player's
character, named from the party's own sources as K-1e did. Its positions are its CHES positions;
its declarations are its dated declarations.

### 5.2 The four roles

| role | what the player controls | what the player can do | how the role ends |
|---|---|---|---|
| **Prime minister's party** | the government: the budget, laws, every dial, appointments, the cabinet | everything the game offers today; build the coalition; offer portfolios and support agreements | a no-confidence vote passes; a partner or supporter walks out and no majority remains; an election |
| **Junior coalition partner** | the levers of the portfolios the party holds, and its ministers; a say in the budget through the coalition agreement | negotiate the budget in cabinet; threaten to leave; leave (the government may fall) | the player leaves; the prime minister's party dismisses the partner; the coalition falls; an election |
| **Support party** (confidence and supply) | no government levers; the party's votes; the support agreement's items | vote on every bill; enforce the agreement — each item is a law or dial target the government owes; threaten or withdraw support | the player withdraws (the government faces a confidence vote); the government finds another majority; an election |
| **Opposition** | the party's votes | vote; table motions and a shadow budget; move no confidence; campaign | an election, or a formation that brings the party in |

**Every role keeps the party's own machinery:** the campaign HQ, polling, media and the run-up to the
next election work the same whether the party governs or not.

**When the player is not the prime minister's party, the country is governed by the AI** — the same
machinery that governs the five AI countries today (the AI finance ministry, the budget rule, the
cabinet), now pointed at the player's own country. The player's role then decides what reaches that
government: a partner's portfolios and budget say, a supporter's agreement items, an opponent's votes.

### 5.3 Formation and agreements

After election night the formation model runs as ruled (dated declarations, declared candidacies,
one-way red lines, feasible hold-outs). What is new is that **the player's party takes part**:

- **If the player's party is the formateur**, the player builds the government: invites partners,
  offers portfolios, offers support agreements. AI parties accept or refuse by their compatibility and
  their declarations — never by an authored willingness.
- **If another party forms**, the player's party is offered whatever the formation model says it
  would be offered: a coalition seat with portfolios, a support agreement, or nothing.
- **Portfolios** are allocated in proportion to seat contribution — the regularity political science
  calls Gamson's law, sourced and stated — with the prime minister's party keeping the head of
  government.
- **A support agreement** is a list of demands the supporting party chooses from its own positions,
  each one a law to enact or a dial target to reach; the formateur accepts the demands its own
  compatibility allows. The agreement is the support party's leverage for the whole term: the game
  tracks each item as owed, delivered or broken.

**The formateur's premises** (ruled by Elias 2026-09-26, `COMPLETED.md` §643; written here before any of it
is built, per the standing rule that a design a later session depends on lives in the repo):

1. **The formation sheet.** When the Speaker asks the player's party, a formation sheet opens: the player
   proposes a cabinet (which parties, which posts to each) and support agreements (which supporters,
   which of their demands to accept).
2. **Posts by Gamson's law.** Partners expect posts roughly in proportion to their seats (Gamson's law,
   `docs/reference/GAMSON_PORTFOLIOS.md`); offering less lowers acceptance. Each post carries its
   portfolio's levers to the partner (§634).
3. **AI answers.** Each AI party answers from its compatibility with the proposed cabinet and its dated
   declarations - red lines, candidacies, V's in-or-against - accepting or refusing with its reason,
   never from an authored willingness. The player may revise and re-offer.
4. **The investiture.** Submitting the proposal runs the investiture vote under the country's rules; a
   failed vote counts as one of the Speaker's proposals, and the sourced limit leads to an extra
   election (§640's loop).
5. **Another party asked.** When another party is asked, it offers the player's party what the
   formation model computes - posts, an agreement, or nothing. The player accepts or declines;
   declining leaves the party in opposition, and if its seats were needed, the Speaker moves to the
   next candidate.
6. **The Speaker's order.** The formation's prime-minister party first; otherwise the largest party
   with a declared candidate.
7. **Dated time.** Each round takes dated time where the constitution sets a deadline; otherwise one
   week per round [AUTHORED-DRAFT].
8. **The caretaker.** The outgoing government governs as caretaker throughout (§640).
9. **Election night.** The board ends with a line naming who the Speaker asks first - and, when it is
   the player, the control that opens the formation sheet.

The build, one commit each: the evaluator (a proposal's investiture and each party's answer, reproducing
the current formation exactly), the Speaker's round (save format 35), the formation sheet, and the
election-night line.

### 5.4 Confidence and collapse

Each country's rules, sourced from its constitution and dated, as data: Sweden's no-confidence vote
and the extra election after repeated failed nominations; Germany's constructive vote of no
confidence and the chancellor's confidence question; Poland's constructive no-confidence; Italy's
confidence in both chambers. A partner leaving or a supporter withdrawing triggers the country's own
procedure — a new formation from the sitting chamber, or an extra election if the country's rules call
for one. The election system already runs an election on any date.

### 5.5 Between elections

A government's record feeds the next vote through perceived performance, as it already does. A
support party shares part of the government's record in the voters' eyes — the fraction is
`[AUTHORED-DRAFT]` with its line and goes on the play-calibration list, since only play can judge it.

## 6. The United States — playing the president

- **The player is a party's presidential nominee** — Democratic or Republican. Third parties are not
  playable: there is no Electoral College path to show them.
- **The campaign is fought state by state** on the Electoral College allocator that already reproduces
  2024 exactly, including Maine's and Nebraska's district methods. The House is elected the same day
  (the chamber exists) and a third of the Senate with it.
- **The Senate is new and required.** US legislation lives or dies in it, and a House-only Congress
  would misrepresent the country. 100 seats in three classes, sourced by state; the cloture threshold
  on legislation and the budget-reconciliation exception, sourced from the Senate's own rules.
- **Winning makes the player president.** Presidential powers are those the model can honestly hold:
  proposing the budget (Congress passes it, through both chambers); signing or vetoing bills, with the
  two-thirds override in both chambers; appointments (the Fed-chair machinery exists). Divided
  government is the mechanic: a hostile House or Senate is what makes the presidency hard.
- **Midterms every two years** — the whole House and a Senate class; the player's party fights them.
- **Losing does not end the run.** A defeated president may run again four years later; the
  constitutional two-term limit ends the run when it binds.

## 7. Winning, losing, and history as the reference

**There is no single win.** A run is a career, measured by elections won, seats, years in each role,
agreement items delivered, and the country's outcomes. **A run ends** when the player's party falls
out of parliament, when the player ends it, or — in the USA — when the term limit binds or the player
declines to run again.

**After the player's first election night, the game shows what actually happened** — the real result
and the government that actually formed — beside the player's. For Sweden that is the 2026 Riksdag
K-1 already sourced, and whichever government the real Riksdag installs. It is the game's signature
view: your country against history.

## 8. France

France cannot hold an election in this game until its two-round, 577-constituency system and its
semi-presidential executive are modelled (R-EL10). Until then **France stays selectable in governing
mode** — its government of record, no election — **and the selector says so in plain words.** The
model it needs is a named future item: constituency-level first- and second-round returns, the
runoff's transfer behaviour, and the president's powers in cohabitation.

## 9. The plan

Each stage lands whole with its evidence; any stage that moves a trajectory is its own BASELINE family,
explained per country; two families never share a pass.

**Stage 0 — install and map (records only).** Install this document under `docs/specs/`. Write the
collision map against the repo — every place the single-start, lose-only and after-the-election
assumptions live — and the sourcing bill of §11 with each item's exact source. Size stages 1–7 in
sessions. **Done when:** the map and bill are on record and stage 1 can start.

**Stage 1 — the world clock.** Chambers, governments and leaders of record **by date** for all six,
from the earliest start (July 2022) to today — reusing the backtest catalogs, sourcing the rest. The
selector starts each country on its §4 date and seats all six as of that date. **Done when:**
choosing any playable country opens a world in which every chamber and government is the one really in
office on that day, asserted per country against the sourced table.

**Stage 2 — Sweden, end to end, on the roles that exist today.** Start in the run-up to 13 September
2026 with Kristersson's government in office; any of the eight parties pickable; the 2026
declarations taking effect on their dates; the campaign; polling day on its date (CL-4); election night;
the formation; the player as prime minister's party or in opposition; the reference view after the
night. The play save re-staged on it. **Done when:** a player can pick any Swedish party, fight the
2026 campaign, and govern or oppose afterward — and then see what Sweden actually did.

**Stage 3 — the roles machinery.** Junior partner and support party (§5.2), the formation's offers and
agreements (§5.3), role-gated levers, the AI governing the player's own country when the player does
not lead it, confidence and collapse (§5.4) — Sweden's rules first, every country's as data.
**Done when:** each of the four roles can be played for a full term in Sweden, and a supporter's
withdrawal can bring a government down by the country's own procedure.

**Stage 4 — Germany.** The Länder catalog and campaign regions; casts derived from CHES; the snap start
with Scholz's minority in office; the constructive vote of no confidence; an election-night map of the
Länder. **Stage 5 — Poland.** The 41 districts; parties and coalition committees with their different
thresholds (sourced); the PiS government at the start; the president's veto and its override.
**Stage 6 — the USA.** §6 whole: the state-by-state campaign, the presidency, the Senate, midterms,
term limits. **Stage 7 — Italy.** The Rosatellum's sub-national stages (R-EL11); the Senato and
confidence in both chambers; the snap start with Draghi's caretaker government. The order of stages
4–7 is the architect's; Elias may move the USA forward.

**Rough size: 30–45 sessions across all stages.** Stage 2 alone makes the whole loop playable for one
country, and it is the smallest.

## 10. The decisions, each standing unless struck

1. **One world clock, set by the chosen country** — the only coherent form in a six-country world.
2. **Snap elections start on the day they were triggered**, scheduled ones at the standard run-up.
3. **The five other countries hold their real calendars and are simulated from the start date** —
   history diverges for everyone.
4. **Losing office never ends a run in a parliamentary country** — OP-1 resolved.
5. **AI party casts are derived from each party's CHES position** (the people-versus-elite item, left–
   right, GAL–TAN) by a stated rule — never a hand-written characterisation of a real party.
6. **The US player is the party's nominee of record; third parties are not playable.**
7. **France is selectable in governing mode, labelled, with no election.**
8. **The Senate is required for the USA and Italy to be playable**; Poland's Senate is stated absent.
9. **Portfolios follow seat contribution (Gamson's law), sourced.**
10. **The stage order of §9.**

## 11. The sourcing bill

Per country, dated: the latest election's calendar and, for snap elections, its trigger date; the
chambers of record by date (much on disk); the governments of record by date; party leaders by date;
declarations by date; the constitutional rules for investiture, confidence, dissolution and extra
elections; Poland's thresholds for parties and coalition committees and the veto override; the US
Senate's composition by class and its cloture and reconciliation rules; the US veto override and term
limit; the Italian Senato's composition; Gamson's law's regularity from the literature. **Nothing typed
from this document; every item fetched, verified and dated, or billed.**

## 12. The Design ask (D22), once stage 3 has built parts

A role indicator on the masthead; the formation screen with the player's offers; the support
agreement as an instrument (items owed, delivered, broken); the confidence-vote moment; the reference
view (your country against history); and, as their stages land, the US presidency (the Electoral
College map, the veto, a Congress view) and the Länder, district and collegio election-night maps.
Asked against built pages, the D19 way.
