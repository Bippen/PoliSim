# Sweden — the eight Riksdag parties' leaders at the 2022 election [SOURCED]

Class: **SOURCED** (§0.4: primary source + vintage + basis). W-F6, compiled 2026-08-30.

**Vintage: 11 September 2022**, the election day this prototype counts. Leaders change; several of
these had changed by the time this file was written, and this file is deliberately the 2022 record
rather than a current one, because it exists to name the people who fought the election the model
replays.

**Basis, and why it is this and not an encyclopaedia.** Each name below is taken from **the party's
own website as it stood within days of the election**, retrieved through the Internet Archive so the
citation carries an exact capture timestamp. A party is the authority on who leads it, and an
archived capture is the only way to ask that question of September 2022 rather than of today.

**What is SOURCED here is the NAME and the OFFICE. Nothing else.** No age, no biography, no
attributes, no relationships. The debate screen's `CandidateProfile` numbers (charisma, competence,
authenticity, and the rest) remain **`[AUTHORED-DRAFT]` game fiction and the screen says so** —
sourcing a real person's *name* does not license inventing their *character*, and §0.4's ban on
invented data is not suspended because a public figure is famous.

| party | leader at 2022-09-11 | office as the party names it | party's own source (Internet Archive capture) |
|---|---|---|---|
| **S** | Magdalena Andersson | *partiordförande* (and Sweden's prime minister) | `socialdemokraterna.se/magdalenaandersson` — capture **2022-08-10 11:51:06**; the page's own text: "partiordförande och Sveriges statsminister" |
| **SD** | Jimmie Åkesson | *partiledare* | `sd.se/vart-parti/jimmie-akesson-sverigedemokraternas-partiledare/` — capture **2022-09-30 22:48:44**; the party's own page title states the office |
| **M** | Ulf Kristersson | *partiledare* | `moderaterna.se` — capture **2022-09-12 01:06:23** (election night); named five times on the party's front page |
| **V** | Nooshi Dadgostar | *partiledare* | `vansterpartiet.se` — capture **2022-09-05 11:51:16**; the page's own text: "Nooshi Dadgostar är Vänsterpartiets partiledare sedan den 31 oktober 2020" |
| **C** | Annie Lööf | *partiledare* | `centerpartiet.se` — capture **2022-09-12 00:56:53** (election night); named five times on the party's front page |
| **KD** | Ebba Busch | *partiledare* | ⚠ **the weakest citation in this table, stated rather than smoothed over** — KD's own site is JavaScript-rendered, so the archived captures carry no static role text. The party's own representatives page exists at `kristdemokraterna.se/om-oss/foretradare/ebba/` (capture **2022-10-06 11:18:10**) and its news archive publishes her speeches as the party's voice in the Riksdag (e.g. capture **2022-09-11 17:15:18**). The OFFICE is carried instead by the **Tidö agreement**, a primary document already SOURCED on disk (`coalition_declarations_2022.md`), which the four party leaders signed. |
| **L** | Johan Pehrson | *partiledare* | `liberalerna.se` — capture **2022-09-11 23:01:42** (election night); named three times on the party's front page |
| **MP** | Märta Stenevi **and** Per Bolund | *språkrör* (**two**, not one) | `mp.se/om/per-bolund/` — capture **2022-09-11**; the page's own text: "Språkrör och riksdagsledamot". Stenevi likewise on `mp.se`. |

## ⚠ MP has two leaders, and the model has room for one

The Green Party is led by **two spokespeople** (*språkrör*), by its own statutes one of each gender,
and in 2022 they were Märta Stenevi and Per Bolund. **Nothing in the model represents this.** A
debate slot, a leader's attributes and a leader-level relationship all assume one person per party.

This is recorded as a **finding, not fixed here**: the fix is a design question (does the player
face one of the two, both, or an aggregate?), it touches §15's debate and §29's leader
compatibility, and W-F6's done-when is to source the names. **It is billed in
`MISSING_PREREQUISITES.md` rather than resolved by quietly dropping Bolund**, which is what taking
"the first one" would amount to.

## What this file deliberately does NOT carry

- **Leader attributes** — charisma, competence, authenticity, and the rest of `CandidateProfile`.
  `[AUTHORED-DRAFT]` and named as such on every screen that shows them.
- **Leader relationships** — §29's personal compatibility between leaders. **Still deferred and
  still asserted ABSENT by reflection** in `CoalitionHarness.cs`; that assertion stands and this
  file does not weaken it.
- **Anything after 2022-09-11.** C, L, S, MP and V have all changed leader or spokesperson since.
  A "current leaders" file is a different item with a different vintage.
