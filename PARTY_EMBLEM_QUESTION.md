# PoliSim — party emblems: one question before any art

**Status: ANSWERED 2026-08-11 — decision taken, four marks delivered, awaiting import on our side.**
**Date:** 2026-08-11 (opened), answered same day.
**Relates to:** the real-parties migration (working-discipline rule 9 reversed for parties, 2026-08-11).

⚠ **GENERATED FILE — do not edit.** The body below is §1F of `CLAUDE_DESIGN_ASSET_REQUEST.md`, which is
the **source of truth**. This mirror exists so the request arrives as a new file rather than as an
in-place overwrite of a document nobody re-reads — the same reason `REVISION_REQUEST_PASS3.md` exists.

Regenerate the body after any edit to §1F:

```sh
awk '/^## 1F\./{f=1} /^## 2\./{f=0} f' CLAUDE_DESIGN_ASSET_REQUEST.md
```

Drift check — the two must report the same hash:

```sh
awk '/^## 1F\./{f=1} /^## 2\./{f=0} f' CLAUDE_DESIGN_ASSET_REQUEST.md | sha256sum
awk '/^## 1F\./{f=1} /^## 2\./{f=0} f' PARTY_EMBLEM_QUESTION.md | sha256sum
```
## 1F. PARTY EMBLEMS — ONE QUESTION, BEFORE ANY ART (2026-08-11)

**Status: ANSWERED 2026-08-11. Decision taken, four marks delivered, batch correctly not started.
Awaiting IMPORT on our side — see §1F.1.**

This was a question, not a request for assets. It gates roughly forty of them, it has a long lead time,
and answering it wrong after the batch is drawn is expensive. §1B.3 set the precedent for asking exactly
one thing when the answer changes what gets built next; this was that shape.

⚠ **This section originally read "§1E's five import blockers are unaffected and still open." THAT WAS
WRONG WHEN WRITTEN** — all five closed 2026-08-10, and Design said so on receipt. It was copied from this
document's own status header, which was stale. **That is the second time this header has misled a reader,
and the warning about the first time is four lines above it.** Rule 12's form exactly: a status
describing the outside world is a cached value and needs an expiry, and the fix is to re-derive from the
filesystem rather than read a document. Left in place as the record rather than quietly deleted.

### What changed on our side

PoliSim is adding real politics to all six playable countries. On 2026-08-11 Elias reversed
working-discipline rule 9 **for parties only**: the game now carries **real political parties with real
names, real vote shares and real seat counts** — Socialdemokraterna, CDU/CSU, Rassemblement National,
Fratelli d'Italia, Prawo i Sprawiedliwość, the Republican and Democratic parties, and so on.

**People did not move and will not.** Every minister, party leader, legislator, head of state and Fed
Chair remains original and fictional. A party is an institution; a politician is a person.

### The question

**How should a real party's identity be represented visually, given that we will not reproduce a
trademarked logo?**

Party *names* are text and we are comfortable using them. Party *logos* are marks owned by organisations,
and reproducing them in a commercial game on Steam is a different proposition entirely. But a hemicycle
legend with six identical grey dots is unreadable, and colour alone stops working the moment two parties
in one chamber share a family colour — which happens in four of our six countries.

**Our provisional answer, offered so you have something to disagree with:** an original abstract mark per
party, in the house style, in that party's real colour. Recognisable by hue and silhouette, owned by us,
and defensible. We hold this loosely — you have made a better call than our brief twice already (D4's hue
cap and D7's resort ladder), and this is more your domain than ours.

### What we would like back

1. **A decision, with your reasoning** — our approach, a better one, or a reason the whole framing is
   wrong.
2. **A proof of concept on three parties, not forty.** Our suggestion is one two-party system and one
   crowded one: the US Republicans and Democrats, plus Sweden's eight-party Riksdag where the problem is
   hardest. If three marks work at both sizes below, the batch is derisked; if they do not, we have spent
   three drawings finding out.
3. **Nothing else yet.** The screens these live on — election night, the campaign screen, the coalition
   board — are not built. Art delivered before them repeats the `menu_pattern_tile.png` outcome, where a
   delivered asset sat unimported for weeks while three documents called it a gap.

### The two sizes that have to work

An emblem is legible at both or it is not usable:

| Where | Size | Notes |
|---|---|---|
| Hemicycle / results legend | ~14-18px square | Beside a party's short code. The demanding case — six to eight of these stack vertically and must be told apart at a glance |
| Results and coalition screens | ~48-64px square | Room for real silhouette |

### Technical conventions — §3 and §4 apply unchanged

Same prefix rules, the §3.1 tint rule, PNG delivery alongside any SVG source, and the `Zone.Identifier`
origin check on receipt. Two of §1E's five blockers are convention violations rather than design
disagreements, so it is worth re-reading §3 before the proof of concept rather than after it.

### 1F.1 THE ANSWER, and what it obliges us to build

**Decision (Design, 2026-08-11): the framing holds, reframed as BALLOT STAMPS** — the mark a game's
election authority assigns a party, one ink, silhouette-first. Real electoral commissions do this so
parties survive one-colour printing.

**This is better than what we asked for, and the reason is worth keeping.** Our version was an
original abstract mark — a workaround for a trademark problem, with no answer to "why do forty marks look
related?" A ballot stamp is diegetic: within the game's fiction the marks share a language because one
authority issues them. It is period-true, it is definitionally not the trademark, and it explains the
family resemblance rather than excusing it.

**Rules that came with it**, each of which constrains our seed data as much as their drawing:

| Rule | What it obliges |
|---|---|
| Silhouette classes unique per chamber; collision pairs must differ in class | A party's class is a property of the party, per chamber it sits in |
| Solid ink, one counter ≥2px at 16px | Legibility floor at legend size |
| Never the subject of the party's own registered mark (no rose for S) | The trademark distance is structural, not stylistic |
| National iconography stays in state chrome (no stars in the US set) | A party mark is not a flag |
| Ink-safe colours required — **SD's yellow flagged** | **Our `DisplayColor` seed values are now a legibility constraint, not just branding.** Sweden's set must be checked before it is seeded |

**The convention call, which is the part that reaches code.** Marks ship **white-on-alpha** in a new
`mark_party_*` family and are **tinted at draw time from the party's seed-data colour**. `emblem_*` keeps
its already-coloured, never-tint meaning and retires with the archetypes.

This is the right call and it lands exactly on the roadmap's Open Question 3 — *"seed data lives in one
file with retrieval dates, so a refresh is a data edit and never a code change."* A rebrand, or Sweden's
13 September election changing a whole party set, is now a `DisplayColor` edit. No redelivery.

**Built on our side, 2026-08-11:** `IconLibrary.GetPartyMark`, `PoliticalParty.MarkName`, and the
Parliament screen drawing each chamber row's mark tinted from the same `DisplayColor` the label is inked
in — so mark and text cannot disagree about a party's colour. US marks wired to `mark_party_us_rep` /
`mark_party_us_dem`.

**Argued overshoot, accepted: four drawings rather than three.** Our brief asked for one two-party system
and one crowded one, but named only S from Sweden — which cannot exhibit the red-red collision the
crowded case exists to test. V is the minimum that makes it testable. The brief was wrong and the
delivery was right.

### 1F.2 IMPORTED 2026-08-11 from `PoliSim v2 Design Progress3.zip`

**This section read "NOT YET IMPORTED" for about an hour**, which was true when written and stopped being
true when Elias delivered the pack. Kept as the record: the gap between delivered and imported is this
project's most-repeated failure (`icon_stat_interestrate` registered "awaiting delivery" on the day it
arrived; `menu_pattern_tile.png` delivered then unimported for weeks while three documents called it a
gap), and it was worth one hour of visible open status rather than none.

**Inspection before extraction**, per the origin-verification discipline established for the first pack:
77 files, 54 PNG / 19 SVG / 3 MD / 1 JSON, **no executables or scripts, no path-escape entries, no
compression anomalies**. Extracted to scratch outside the repo and inspected there before anything was
copied in.

⚠ **Mark-of-the-web could NOT be checked.** Windows alternate data streams are not visible across the
Linux mount this session reaches the repo through, so `Zone.Identifier` was neither present nor absent —
it was unobservable. That check remains outstanding and can only be done Windows-side.

**The WoA claim was verified rather than trusted**, because the tint path depends on it and a coloured
PNG run through a tint would double-apply colour: all four marks are 128×128 RGBA with **exactly one
unique RGB value among visible pixels, and it is white**. Ink coverage 19-36%.

| Mark | Silhouette class | Note from MANIFEST |
|---|---|---|
| `mark_party_us_rep` | crest | |
| `mark_party_us_dem` | torch | |
| `mark_party_se_s` | banner | not a rose — that is the subject of their registered mark |
| `mark_party_se_v` | star | the fourth drawing; the S/V red-red collision is untestable with three |

**Imported**: four PNGs to `Assets/Resources/Art/UI/Emblems/`, four SVG sources to `Emblems/Source/`,
with `.meta` files copied from the existing `emblem_party_*` importer settings and given fresh GUIDs — so
the marks import with settings already proven at legend size rather than Unity defaults.

**No regression from the re-delivery.** Of the pack's other 65 assets, **57 are byte-identical to what
is already on disk and 0 differ** — which rules out a silent re-export at different settings, and is the
whole of what a diff over shared files can establish.

⚠ **This section also read "§1E independently confirmed closed by the same import". THAT INFERENCE WAS
WITHDRAWN 2026-08-11.** Identity is not compliance: a designer re-exporting their working set produces
zero diffs whether or not they read the blocker list; a blocker satisfied by an undelivered file shows
up as absence, which a diff cannot see; and E1/E2 are about NAMING, which is not a byte-level property
at all. §1E **is** closed — verified per item against disk, which is the method that answers it. See
`CLAUDE_DESIGN_ASSET_REQUEST.md` §1F.2 for the table.

**The eight SVG sources are now filed** in `Chrome/Source/` alongside the other 19, and
`DeliveredAssetCheck` is back to **0 missing**. They were never blocked: the stated reason — that the
`Chrome/` namespace question was open — named E2, and E2 is the blocker that *resolved* it.

✅ **The marks resolve, verified 2026-08-11 — but NOT by the check this section originally named.** It
said to run `StatIconCoverageCheck`, which enumerates `StatNodeId` plus `menu_pattern_tile` and never
touches `Emblems/`. It passes **19 of 19** whether the marks are present, absent or corrupt, so it
cannot answer the question it was cited for. `PartyMarkCoverageCheck` asks properly: **4 of 4 resolve at
128×128, RGBA32**, behind a self-test on a known-good emblem.

⚠ **And the import settings were wrong, which only a format check could have caught.** The metas were
copied from `emblem_party_*` — but that family is FULL-COLOUR (§3.1: never tinted) while `mark_party_*`
is WHITE-ON-ALPHA and tinted at draw time. The emblem settings carry `textureCompression: 1`, so all
four marks imported as **DXT5**: block compression on white-on-alpha at icon size, the exact damage
vector §3's Chrome correction exists to prevent. Corrected to `textureCompression: 0` / `nPOTScale: 0`
and re-verified as RGBA32. **"Settings already proven at legend size" was proven for a different art
category.**

### Scale this gates, for planning only

Roughly forty parties across six countries once every seed set lands — three seeded today (USA), the rest
following per country. **Sweden's set changes on 13 September 2026**, when it holds a general election, so
Swedish emblems drawn before then may need revisiting. That is an argument for the proof of concept now
and the batch later, not for waiting.

