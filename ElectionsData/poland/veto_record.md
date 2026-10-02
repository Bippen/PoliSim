# Poland - the presidential vetoes of the 10th Sejm term and the votes behind them

Sourced 2026-10-02 to backtest the designer's proposed rule: *the President vetoes an ordinary statute when a majority of his backing party's
deputies voted against it* (both presidents PiS-backed: Andrzej Duda to 6 Aug 2025, Karol Nawrocki from 6 Aug 2025). The constitutional
text (Art. 122 ust. 5, Art. 224) and the Sejm's standing orders are in `veto.md`. Raw files: `G:\UNITY\Projects\PoliSim-captures\sources\poland_vetoes\`
(`fetch_log.md`, `SHA256SUMS.txt`, 1,778 files). Extracts: `veto_record.csv` (53 rows), `third_readings_term10.csv` (579 rows).

**Coverage.** The whole 10th term as the Sejm API held it on 2026-10-02: sittings 1-65 (29 Nov 2023 - 18 Sep 2026). That runs past the brief's
31 Dec 2025, because the API carried the 2026 vetoes too.

## 1. Which lists, and why

- **prezydent.pl refused.** Both list pages (`/prawo/ustawy-zawetowane`, `/kancelaria/archiwum/andrzej-duda/prawo/zawetowane`) returned
  **HTTP 403** with a Cloudflare "Just a moment..." challenge page (kept under `prezydent/`). Nothing on them was read.
- **Primary source used: the Sejm API** (api.sejm.gov.pl, term 10). All 980 BILL processes were fetched. A veto is a process stage of type
  `Veto`, named "Wniosek Prezydenta (weto)". The veto date is the `documentDate` of the President's request as a Sejm print
  (`/prints/{n}`). It matches the stage date in 53 of 53 cases. The President's own letter may carry an earlier date. Example: print 138 is
  dated 27 Dec 2023 by the Sejm, but its PDF text layer has no legible day.
- **53 vetoes = 53 distinct veto prints.** Some vetoed acts were built from several joint processes: the KPK act of 27 Feb 2026 has eight
  and the crypto act of 15 May 2026 has four. Each such veto is one row in the CSV, with every process listed.
  - **Andrzej Duda: 8**, from 27 Dec 2023 to 5 Aug 2025.
  - **Karol Nawrocki: 45**: 20 in 2025 (21 Aug - 18 Dec) and 25 in 2026 (to 28 Aug 2026).
- **Cross-check (SECONDARY):** rp.pl's list of Nawrocki's 20 vetoes in 2025 (`secondary/rp_art43578771.html`, act dates only) matches the
  API's 20 for 2025 one to one, by act date and title. Item 8 matches by date and subject only, because rp.pl words the title loosely ("o
  nowelizacji ... i etycznych"). No dated secondary list of Duda's vetoes was fetched. His 8 rest on the API alone.

## 2. Counting conventions

- **Clubs as at the vote.** Every per-MP voting record (`/votings/{sitting}/{n}`) gives each MP's club, and the club is the one held at the
  time of that vote: the `RozwojPlus` club appears only from sitting 63 on.
- **`pis_members`** = MPs recorded in club `PiS` at that vote. **`pis_absent`** = vote `ABSENT` (did not vote). No other vote values occur.
- **`pis_majority_no`** = PiS NO > half of `pis_members` (the reading used in both CSVs). **`pis_majority_no_of_voting`** = NO > half of
  PiS YES+NO+ABSTAIN.
- **All sums are our own.** For every one of the 671 votes used, the per-MP sums equal the record's own `yes`/`no`/`abstain` fields, and the
  `ABSENT` count equals `notParticipating`. There were no mismatches.
- **Which vote is the third reading.** The whole-bill vote ("głosowanie nad całością projektu") inside the process's "III czytanie" stage whose
  decision is "uchwalono". `sejm_passed_date` is that vote's date.
- **The act's legal date** (in `act_title`) is the date of the Sejm's last vote: the Senate-amendment vote where the Sejm accepted amendments,
  otherwise the third reading. It matches `sejm_final_vote_date` in 52 of 53 cases. In the remaining case (crypto act, print 2267) the
  Senate's one amendment was rejected and the act kept its third-reading date.
- **Senate-amendment votes** (`senate_votes`) are motions to reject a Senate amendment, so YES = reject. The Sejm API's process stage carries
  none of these votes. They were taken from the sitting's voting list by the Senate print number in the title.

## 3. The rule against the record

**Vetoes, as `pis_majority_no`:**

- **Duda: 7 of 8.** The exception is the "okołobudżetowa" act of 21 Dec 2023: PiS was 172 ABSENT of 191.
- **Nawrocki: 38 of 45.** Of the 7 misses, 6 are bloc abstentions by PiS: the sexual-offences act (172 abstained), road traffic (170), the
  Kodeks cywilny/insurance act (168), health care (176), and two acts after the split (135 and 131 of 147). The seventh is the animal-protection
  act, where PiS split 49 YES / 84 NO / 30 ABSTAIN / 25 absent. That is a majority NO of those voting but not of members.

**Precision base** (vetoable acts the President decided on). The President is assigned by the date of the decision. The 4 budget acts are
left out; all 4 were signed.

| President | PiS majority NO | vetoed | signed | Tribunal |
|---|---|---|---|---|
| Duda | yes | 7 | 58 | 5 |
| Duda | no | 1 | 149 | 3 |
| Nawrocki | yes | 38 | 57 | 1 |
| Nawrocki | no | 7 | 220 | 2 |

- **False positives** (PiS opposed and the President signed): **58 under Duda, 57 under Nawrocki**.
- **A looser rule tried for comparison:** "PiS not a majority YES". It catches all 8 + 45 vetoes but brings 82 + 126 signed acts.

## 4. The three quoted override votes (and the other four)

`override_required_stated` is the record's `majorityVotes` (type `MAJORITY_THREE_FIFTHS`). Every stated figure equals
ceil(0.6 x (yes + no + abstain)) = ceil(0.6 x the record's `totalVoted`).

| Vote | Date | Act | Yes / No / Abstain | Stated | ceil(0.6 x total) | Result |
|---|---|---|---|---|---|---|
| 46/75 | 2025-12-05 | crypto-assets, 7 Nov 2025 | 243 / 192 / 0 | 261 | 261 | failed |
| 48/8 | 2025-12-17 | animal protection, 7 Nov 2025 | 246 / 192 / 0 | 263 | 263 | failed |
| 65/10 | **2026-09-17** | close-person status, 29 May 2026 | 232 / 199 / 0 | 259 | 259 | failed |
| 55/13 | 2026-04-17 | crypto-assets, 18 Dec 2025 | 243 / 191 / 3 | 263 | 263 | failed |
| 54/27 | 2026-03-27 | KPK, 27 Feb 2026 | 244 / 180 / 16 | 264 | 264 | failed |
| 64/15 | 2026-09-04 | crypto-assets, 15 May 2026 | 241 / 198 / 3 | 266 | 266 | failed |
| 65/11 | 2026-09-17 | close-person introductory act | 232 / 200 / 0 | 260 | 260 | failed |

- **All three quoted votes match.** The third ("232+199 -> 259") is from **17 Sep 2026, not 2025**.
- **Abstentions count toward the 3/5.** Three votes had abstentions (54/27, 55/13, 64/15), and in each the Sejm's stated figure counts them:
  264, not 255; 263, not 261; 266, not 264. This is the Sejm's own practice as recorded, not the text of a statute. It settles empirically
  the [PROVISIONAL] point in `veto.md` §4.
- **Every override in the term failed.** These seven are the only override votes. The other 46 vetoes had no override vote by sitting 65.

## 5. What is missing or read elsewhere

- **The President's own signing dates for the vetoes:** see §1.
- **Two outcomes not read from a Sejm API stage.**
  - Process 219 (the KRS act of 12 Jul 2024) stops at "sent to President". It is recorded as referred to the Tribunal (Kp 2/24, request of
    1 Aug 2024) from the Tribunal's own communiqué (`secondary/trybunal_krs_komunikat.html`).
  - Process 752 has no signature stage. It is recorded as signed because it carries the ELI Dz.U. 2024 poz. 1855.
- **27 rows are undecided:** 15 pending at the President, 10 at the Senate, and 2 with Senate amendments not yet voted.
- **The PiS split.** From sitting 63 (31 Jul 2026) 40-41 deputies sit in a club the API calls `RozwojPlus`, and PiS drops from 186 to 147.
  The PiS columns count only `PiS`, and the third-readings CSV carries `rozwojplus_*` columns as well. Whether that club counts as the
  President's backing party is not sourced here; it is for the designer to rule.
- **Print 1640's title as the Sejm wrote it** repeats "o zmianie ustawy z dnia 25 czerwca 2025 r.". It is kept verbatim.
- **No constitutional amendment** passed the Sejm in the term. The **budget acts** for 2024, 2025 and 2026 and the 2024 budget amendment are
  flagged in `veto_exempt`. The "okołobudżetowa" acts are ordinary statutes and can be vetoed; one of them was (Duda, Dec 2023).
