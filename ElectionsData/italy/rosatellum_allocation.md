# Italy — the Rosatellum's proportional allocation procedure (Camera 2022) [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4 gate; 2026-08-29, research agent; Normattiva's consolidated DPR 361/1957,
the Camera Servizio Studi dossiers, D.P.R. 21 luglio 2022, Eligendo). This is R-EL9's sourcing:
the ALLOCATION ARITHMETIC that was missing overnight and that kept Italy out of the backtest.
In-force check performed: art. 83 and art. 1 stamped 26-6-2019; arts. 77, 83-bis, 84, 92 stamped
12-11-2017 — all prior to 25-9-2022, and art. 83 unamended since.

## The method, in one line

**Hare quotient with largest remainders — applied twice, with the quotient TRUNCATED to an
integer both times.** Not a divisor method. Art. 83 c.1 lett. f) (between coalitions/standalone
lists) then lett. g) (inside each coalition).

## The operative provisions

- **lett. b) — the threshold denominator:** "determina il totale nazionale dei voti validi. Esso
  è dato dalla somma delle cifre elettorali circoscrizionali di **tutte** le liste" — *all* lists,
  including below-threshold ones. **2022: 28.098.196** (ITALIA excl. Valle d'Aosta). So 3 % =
  842.946, 10 % = 2.809.820, 1 % = 280.982.
- **lett. c) — coalition figures and the 1 % strip:** "Non concorrono alla determinazione della
  cifra elettorale nazionale di coalizione i voti espressi a favore delle liste collegate che
  abbiano conseguito … un numero di voti validi inferiore all'1 per cento del totale". Below
  1 %: **discarded** from the coalition figure (not redistributed; they still sit in the lett. b)
  denominator). Between 1 % and 3 %: they **do** count toward the coalition figure but the list
  is excluded from lett. g) — so in effect their votes transfer to the coalition's ≥3 % partners.
- **lett. e) — admission:** a coalition needs ≥10 % **and** at least one member list ≥3 % (or a
  recognised-minority member); a standalone list needs ≥3 %; a list stranded in a failed
  coalition falls back to the 3 % standalone test.
- **lett. f) — the first Hare pass:** "divide il totale delle cifre elettorali nazionali delle
  coalizioni di liste e delle singole liste … per il numero dei seggi da attribuire, ottenendo
  così il quoziente elettorale nazionale. **Nell'effettuare tale divisione non tiene conto
  dell'eventuale parte frazionaria del quoziente.** … La parte intera … rappresenta il numero dei
  seggi … I seggi che rimangono ancora da attribuire sono … assegnati … per le quali queste
  ultime divisioni abbiano dato **i maggiori resti**".
- **lett. g) — the second Hare pass, inside each coalition:** divides "la somma delle cifre
  elettorali delle **liste ammesse al riparto**" (NOT the coalition's own figure) by the seats
  the coalition won at lett. f), quotient again truncated, integers then largest remainders.
- **Minority route, lett. e) n.2:** a recognised-minority list standing **exclusively** in a
  special-autonomy region whose statute protects that minority is admitted on **either** ≥20 % of
  that region's valid votes **or** candidates elected in ≥¼ of the circoscrizione's uninominal
  colleges (rounded up). SVP-PATT 2022 met **both** limbs — 23,15 % regionally and 2 of Trentino-
  Alto Adige's 4 colleges — and was admitted at **0,42 % nationally**, taking 1 proportional seat.

## The two tiers are PARALLEL, not compensatory (the single most load-bearing structural fact)

Art. 1 c.4: the college seats go to the college winners and "**Gli altri seggi**" are allocated
proportionally. Art. 83 c.1 lett. f) subtracts the 146 college seats from the POOL and then
allocates "i restanti seggi … **in base alla cifra elettorale nazionale**" — votes alone; no term
in art. 83 references a party's college wins. **There is no *scorporo*.** The Camera's own
dossier proves it empirically from 2018: the centre-right won 111 colleges and still received its
full proportional share (37,03 % of votes → 39,1 % of proportional seats), while LeU won none and
received no compensation.
One real coupling exists and must not be mistaken for compensation: **art. 58 c.3** shares a
ballot marked only on the uninominal candidate among the coalition's lists in proportion to their
college votes — that moves *votes* into the proportional count, never *seats*. The published
*cifre elettorali* already include it.

## Verified recomputation of the 2022 national stage (reproduces the official result exactly)

Admitted figures after the 1 % strip — centre-right 12.050.887 (Noi Moderati 254.127 struck),
centre-left 7.166.541 (Impegno Civico 173.555 struck), M5S 4.335.494, Azione–IV 2.186.505,
SVP-PATT 117.032; **sum 25.856.459**. lett. f): 25.856.459 ÷ 245 = 105.536,57 → quotient
**105.536** → integers 114 / 67 / 41 / 20 / 1 = 243, the two remaining seats to the largest
remainders (centre-left 0,9061; Azione–IV 0,7180) → **114 / 68 / 41 / 21 / 1**.
lett. g) centre-right: 12.050.887 ÷ 114 → 105.709 → FdI 69, Lega 23, FI 21 + the remainder seat
to FI (0,5617) → **FdI 69, Lega 23, FI 22**. lett. g) centre-left: divisor is the ADMITTED lists
only, PD + AVS = 6.370.484 (+Europa's 796.057 excluded at 2,83 %) ÷ 68 → 93.683 → PD 57, AVS 10 +
the remainder seat to AVS (0,9071) → **PD 57, AVS 11**.

**Final: FdI 69, Lega 23, FI 22, PD 57, AVS 11, M5S 41, Azione–IV 21, SVP-PATT 1 = 245** —
matching Eligendo's proportional seat column list for list.

⚠ **The ambiguous clause, settled numerically:** reading lett. g)'s divisor as the coalition's
FULL figure instead of the admitted lists' sum yields PD 50 / AVS 9 — **off by nine seats**. The
admitted-lists reading is therefore the correct one, and this is why the implementation states it.

## What is implementable, and what is not

**The national stage: fully implementable from national list votes alone** (plus three structural
inputs: the coalition-membership map, the seat count 245, and the minority flag) — verified end
to end above. **The sub-national stages are NOT** derivable from national aggregates: lett. h)
(into 28 circoscrizioni), lett. i) (coalition → member lists per circoscrizione) and art. 83-bis
(into the 49 collegi plurinominali) each need per-circoscrizione and per-collegio *cifre
elettorali*, available on Eligendo Archivio as HTML only. Per-circoscrizione seat entitlements are
in D.P.R. 21 luglio 2022 (Tab. A/B; circoscrizione quotient 151.616 = 59.433.744 ÷ 392).

⚠ **Do not use the Ministry's comune-level open-data CSV for allocation.** Its `VOTILISTA` sums
undershoot Eligendo's published figures by ~2,6–4,6 % (FdI 7.098.555 vs 7.301.303; PD 5.128.861 vs
5.348.676) — most likely because the CSV carries raw list-marked votes while art. 83 operates on
the *cifra elettorale* AFTER the art. 58 c.3 redistribution. Unverified, and load-bearing.

## Implementation traps, each recorded

1. **The quotient is floored** at lett. f), g), h), i) and art. 83-bis — an exact Hare quotient drifts by a seat or two.
2. **Different divisor bases by stage:** lett. f)/g) divide by ADMITTED parties' votes; art. 83-bis divides by "le cifre elettorali di collegio di **tutte** le liste". Deliberate in the enacted text.
3. **Decimal-round exclusion:** at lett. h)/i)/83-bis a party already at its higher-tier entitlement is excluded from the largest-remainder round — omitting this reintroduces the pre-2017 *slittamento*.
4. **The compensation loop is an ordered search**, not a tidy formula (largest excess first, tie-broken by national figure; surrender where the used decimal is smallest and a deficit party has an unused decimal; walk the ascending order if none; split across circoscrizioni only as a last resort).
5. **Seats move between collegi** (2018: 20 seats across 34 collegi), so a collegio's elected deputies need not equal its DPR assignment.
6. **Art. 84 *incapienza*** can cascade a seat to another collegio, to best losers, to coalition partners, even across circoscrizioni (2018: six M5S seats) — it changes WHO sits, not the per-list national totals.
7. The 10 % coalition base is textually ambiguous (gross vs net of the 1 % strip); immaterial in 2022.
8. **SVP-PATT's coalition status** is tabulated standalone by Eligendo; art. 14-bis c.2 would also permit in-coalition. Both readings give identical per-list seats in 2022, so the case does not discriminate — a general model must pick, and the answer is the pre-election *collegamenti* list in the Gazzetta Ufficiale.

## The official 2022 record, for the day someone wants the sub-national stages

Corte di Cassazione, Ufficio Elettorale Centrale Nazionale, *verbali* published 12 October 2022 —
`verbale_camera_3_10_prospetti.pdf` (91 pp.), `…_4_10_prospetti.pdf` (37 pp.), `…_8_10.pdf`
(37 pp.), index `https://www.cortedicassazione.it/it/dettaglio_elezioni.page?contentId=ELE27502`.
**All three are scanned images with no text layer — OCR required.** The Ministry's own calculation
tables are gone (the Eligendo "Reportistica" 2022 endpoint now serves an empty SPA shell).
The best published worked example is the Camera Servizio Studi dossier for 2018 under the
identical procedure: `https://documenti.camera.it/leg18/dossier/pdf/AC0125.pdf`.
