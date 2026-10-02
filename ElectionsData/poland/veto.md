# Poland - the president's veto and the Sejm's override (PS-5, part four)

Sourced 2026-10-02 for the political-system spec's stage 5 (*the president's veto and its override*) and the start-points spec's S7 (*the
president's role in a parliamentary republic - the veto and its override, and cohabitation with the Sejm of record*). Every quotation below is
verbatim from a page held byte-exact under `raw/` (its SHA-256 in that folder's `SHA256SUMS.txt`); a gloss in italics follows each. Text read
out of a PDF here is the PDF's own text layer, decoded through its fonts' ToUnicode maps; only line wraps are joined.

## 1. The veto and its override - Konstytucja Art. 122 ust. 2, 5, 6

Quoted in full, with the 460 of Art. 96 ust. 1, in `presidential_returns.md` (the section *Art. 122 ust. 5 - the veto and the 3/5 override*),
from the Kancelaria Sejmu's unified text `raw/president/eli_DU_1997_483_konstytucja_U_D19970483Lj.pdf`. In short: the President signs a statute
within **21 days** (ust. 2); instead of referring it to the Constitutional Tribunal he may send it back to the Sejm with reasons (ust. 5); the
Sejm overrides by re-passing it **"większością 3/5 głosów w obecności co najmniej połowy ustawowej liczby posłów"** - with 3/5 of the votes,
at least half the statutory number of deputies (230 of 460) present; the President then signs within **7 days** and may no longer refer it to
the Tribunal; a referral or a request for reconsideration stops the 21-day clock (ust. 6).

## 2. The budget is exempt - Konstytucja Art. 224

Source: `raw/president/eli_DU_1997_483_konstytucja_U_D19970483Lj.pdf` (the unified text, held since §720), read 2026-10-02.

> Art. 224. 1. Prezydent Rzeczypospolitej podpisuje w ciągu 7 dni ustawę budżetową albo ustawę o prowizorium budżetowym przedstawioną przez
> Marszałka Sejmu. Do ustawy budżetowej i ustawy o prowizorium budżetowym nie stosuje się przepisu art. 122 ust. 5.
>
> *Gloss: the President signs the budget act, or the act on a provisional budget, within 7 days of its presentation; **Art. 122 ust. 5 - the
> veto - does not apply to either.***

> 2. W przypadku zwrócenia się Prezydenta Rzeczypospolitej do Trybunału Konstytucyjnego w sprawie zgodności z Konstytucją ustawy budżetowej
> albo ustawy o prowizorium budżetowym przed jej podpisaniem, Trybunał orzeka w tej sprawie nie później niż w ciągu 2 miesięcy od dnia złożenia
> wniosku w Trybunale.
>
> *Gloss: he may refer the budget act to the Constitutional Tribunal before signing it, and the Tribunal rules within 2 months - a referral,
> not a veto; the referral is not modelled.*

## 3. The Sejm's procedure - Regulamin Sejmu Art. 64

Source: `raw/veto/eli_MP_2026_573_regulamin_sejmu_tekst_jednolity.pdf` - the Marshal of the Sejm's notice of 27 May 2026 of the consolidated
text of the Sejm's standing orders (M.P. 2026 poz. 573; the ELI metadata `raw/veto/eli_meta_MP_2026_573.json`, found through
`raw/veto/eli_search_MP_regulamin_sejmu.json`).

> Art. 64. 1. Ustawę, której podpisania Prezydent odmówił, przekazując ją wraz z umotywowanym wnioskiem Sejmowi do ponownego rozpatrzenia,
> Marszałek Sejmu kieruje do komisji, które rozpatrywały projekt ustawy przed uchwaleniem jej przez Sejm.
>
> *Gloss: a statute the President refused to sign and sent back with reasons goes to the committees that examined the bill.*

> 2. Marszałek Sejmu zarządza drukowanie wniosku Prezydenta, o którym mowa w ust. 1, i doręczenie go posłom.
>
> *Gloss: the President's request is printed and delivered to the deputies.*

> 3. Po rozpatrzeniu wniosku Prezydenta komisje, do których wniosek ten został skierowany, przedkładają Sejmowi sprawozdanie. W sprawozdaniu tym
> komisje przedstawiają wniosek o ponowne uchwalenie ustawy w brzmieniu dotychczasowym bądź wniosek przeciwny.
>
> *Gloss: the committees report, proposing that the Sejm re-pass the statute as it stood, or not.*

> 4. Na posiedzeniu Sejmu przedstawiciel Prezydenta, w jego imieniu, przedstawia motywację wniosku o ponowne rozpatrzenie ustawy przez Sejm,
> a następnie poseł sprawozdawca przedstawia stanowisko komisji.
>
> *Gloss: the President's representative states his reasons in the chamber; the rapporteur gives the committees' position.*

> 5. O ponownym uchwaleniu przez Sejm ustawy w brzmieniu dotychczasowym, większością 3/5 głosów w obecności co najmniej połowy ustawowej liczby
> posłów, Marszałek Sejmu powiadamia niezwłocznie Prezydenta.
>
> *Gloss: where the Sejm re-passes it as it stood - 3/5 of the votes, at least half the statutory number present - the Marshal tells the
> President at once (who signs within 7 days, Art. 122 ust. 5).*

> 6. Jeżeli Sejm ponownie nie uchwali ustawy w brzmieniu dotychczasowym, postępowanie ustawodawcze ulega zamknięciu.
>
> *Gloss: **if the Sejm does not re-pass it, the legislative procedure is closed** - the vetoed statute is dead.*

## 4. Ruled 2026-10-02 (B1, B2), and what is still not modelled

- **How "3/5 of the votes" treats an abstention - RULED (B2):** *"3/5 of the deputies voting, abstentions included in the base. The Sejm
  computes qualified majorities from the number voting, with 230 present as the quorum. Required = ceil(0.6 × (yes + no + abstain))."*
  Neither the Konstytucja nor the standing orders' consolidated text define the denominator; the Sejm's own record does: in all seven
  override votes of the 10th term the stated required figure is ceil(3/5 of yes + no + abstain), and in the three with abstentions it counts
  them (264 not 255, 263 not 261, 266 not 264) - `veto_record.md` §4. The [PROVISIONAL] mark of the first build is struck.
- **Who vetoes what - RULED (B1):** *"The President vetoes an ordinary statute when a majority of his backing party's deputies voted against
  it. Two kinds of act can't be vetoed: the budget act (Constitution art. 224: he signs within 7 days and can only refer it to the Tribunal)
  and constitutional amendments (art. 235(7)). Budget-related acts are ordinary statutes and can be vetoed."* Duda and Nawrocki, PiS-backed
  (Nawrocki's backing [SECONDARY], `presidential_returns.md`); "a majority of his backing party's deputies" read as more than half of the
  club's MEMBERS at the vote, absent ones included (`veto_record.md` §2).
- **Art. 235 ust. 7** (`raw/records/trybunal_konstytucja.html`, the Tribunal's copy of the Konstytucja): *"Po zakończeniu postępowania
  określonego w ust. 4 i 6 Marszałek Sejmu przedstawia Prezydentowi Rzeczypospolitej uchwaloną ustawę do podpisu.
  Prezydent Rzeczypospolitej podpisuje ustawę w ciągu 21 dni od dnia przedstawienia i zarządza jej ogłoszenie w Dzienniku Ustaw
  Rzeczypospolitej Polskiej."* - the President signs an amendment of the Konstytucja; Art. 122 ust. 5's return is not given to him.
- **The record of vetoes and override votes in the 10th Sejm** - fetched (`veto_record.md`, `veto_record.csv`, `third_readings_term10.csv`;
  prezydent.pl refused with a 403, rp.pl's dated list of Nawrocki's 20 vetoes to 31 Dec 2025 matched): the backtest is the instrument's (g).
- **The Senate** (Art. 121) and **the Tribunal referral** (Art. 122 ust. 3, Art. 224 ust. 2): not modelled; the Senate is stated absent in
  the political-system spec.
- **LIVE since `COMPLETED.md` §761** (`PresidentialVeto`, `SimulationManager.PresidentialVetoGate`): every ordinary statute the game's Sejm
  passes goes to the president of record on the day; the rule and the override are B1's and B2's above. Premises, stated there: the veto and
  the vote on it are taken on the passage's day (the 21 days are not waited), and the re-pass is voted by the passage's own sides. The game's
  budget bill is exempt as a whole (Art. 224), though it carries rates that in Poland are budget-related ordinary statutes - owed to Elias.
