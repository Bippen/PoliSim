# Sweden 2026 — coalition declarations as of the 2026 election [SOURCED] [PROVISIONAL]

Class: SOURCED (real-world facts about the eight Riksdag parties' public commitments on government
formation as they stood at the election of **13 September 2026**). `[PROVISIONAL]` until re-verified.
Written to **replace 2022's declarations in the formation model** (K-1, item 2): these are the
**declared** red lines of §29 for the 2026 chamber; the **derived** red lines still come from CHES
positions in `ElectionsData/positions/party_positions.md` and are not repeated here.
`ElectionsData/sweden/coalition_declarations_2022.md` is NOT edited — it stays the backtest's reference.
Wired by K-1 part (2) (COMPLETED.md §603): `DeclaredRedLines.cs` cites this file for the seated 2026 chamber's declared
lines and keeps the 2022 file for the backtests that pin 2022's government (see the FORMATION block and the last section).

Vintages are stated per item and are NOT smoothed: where a line was first said in 2025 and restated in
2026, both dates are given, because a declared red line is a dated fact and the date is what makes it
falsifiable. **Fetched, never recalled:** every quote below was read in a page fetched on 2026-09-23
and saved byte-for-byte under `raw/declarations/`, SHA-256 in `raw/declarations/SHA256SUMS.txt` and in
the register. Quotes are Swedish and verbatim (**bold** only where the original is bold); English
glosses are marked *gloss:* and are mine, not the source's.

## FOR THE FORMATION MODEL (2026 vintage)

The declared pairwise lines as wired in place of 2022's by K-1 part (2) (§603), in `RedLine(a, b, kind, blocksSupport,
basis, oneWay)` form. Every other pair stays with the DERIVED rule (see *What is deliberately NOT here*).

| pair | wire as | source ids |
|---|---|---|
| **C ↔ SD** | **support-blocking** (2022's strength, unchanged) | [C-P5], [C-P1], [C-I10], [C-P2], [C-I1], [C-I6] |
| **C ↔ V** | **one way, C → V**: wired one way (`RedLine.OneWay`, a shape built for it by K-1 part (2), §603); C will not sit in, support or let through a cabinet that contains V, and V may support a cabinet C sits in (see *C ↔ V, one way* below the table and §(3)) | [C-P1], [C-I10], [C-I3], [C-I9], [C-I1], [C-I6], [C-I8] |
| **M ↔ SD** | **none**: 2022's cabinet-blocking line lifted | [MSD-P1], [MSD-P2], [MSD-I1]–[MSD-I4], [MSD-I6] |
| **L ↔ SD** | **none**: 2022's cabinet-blocking line lifted | [L-P1], [L-P1a], [L-I1], [L-I2], [L-P2] |
| **KD ↔ SD** | **none**: 2022's cabinet-blocking line lifted (on secondary transmission of KD's own words; KD primary a GAP) | [KD-I2], [MSD-I2], [MSD-I3] |

**C ↔ V, one way.** C refuses any cabinet that **contains** V: it will not sit in it, support it or let it through
([C-I1], [C-I3], [C-P1], [C-I10]). That is stronger than cabinet-blocking and weaker than support-blocking, because
no fetched source that names a mechanism has C refuse V as a mere support party; the statements that V should
have no influence ([C-P1], [C-I10], [C-I6], [C-P4], [C-P3]) are broader and name no mechanism. Neither of the model's
two symmetric strengths holds it, so it is wired one way (`RedLine.OneWay`, a shape built for it by K-1 part (2), §603),
as `RedLine(C, V, Declared, blocksSupport: true, oneWay: true)` in `DeclaredRedLines.cs`: the refusal runs from C to
V only, so V may support a cabinet C sits in, and the two may both support one. Symmetric support-blocking was not
used because it over-forbids exactly those cases: C supporting from outside an S cabinet without V that V also
supports from outside (and its mirror, V supporting from outside a cabinet C sits in). Both cases need V to support
a cabinet V is not in, which V's own in-or-against demand [V-P1] refuses wherever V's votes are needed; that is why
TV4's door, below, is conditional on V dropping its demand. **No fetched source says C refuses the first case.** The one fetched
source that answers it directly, TV4 2026-01-30 [C-I10], has the door *"inte formellt stängd"* on condition that
V drops its demand to sit in government, and V held that demand ([V-I1], 2026-04-18). Nerikes Allehanda [C-P1]
records the question whether C can budget-cooperate with V *"om partiet skulle stå utanför regeringen"* called
*"hypotetisk"*, citing V's pledge to vote down every government V is not in. And the first case is not a hypothetical: it is one C itself floated. Bulletin, citing
Expressen [C-I8], 2026-09-18, reports a pure S minority as *"en lösning C tidigare fört fram"*, which V rejects
(secondary).

**MEASURED on the seated 2026 chamber** (S 99, SD 62, M 70, V 30, C 25, KD 22, MP 22, L 19; majority 175; negative
rule), `Formation2026Diagnostic`, log `k1_603_formation2`. **As wired** (C ↔ SD support-blocking, C → V one way; M, KD,
L ↔ SD lifted): ConfidenceAndSupply, cabinet **S+C+MP (146), support V (to 176), opposed 173**, the only viable
government. The same with C ↔ V as a **symmetric support-blocking** line: MajorityCoalition **S+M+C+KD+L (235)**,
opposed 114, 8 viable. With C ↔ V at **cabinet-blocking** strength: the same as wired (S+C+MP, support V). With **no
C ↔ V line**: S+M+C+KD+L (235). With **2022's declarations** on the 2026 chamber: S+M+C+KD+L (235). With the **derived
lines alone**: S+M+C+KD+L (235). **The C ↔ V line is load-bearing**: its shape decides between a five-party grand
coalition that no party declared and an S+C+MP minority carried by V. And the wired result depends on V supporting a
cabinet V is not in, which V's own in-or-against demand [V-P1] refuses: a shape the pairwise model still cannot hold
(it stays in the list below). What rules out S with M in reality, the rival prime-ministerial candidacies ([MSD-P1]
Kristersson; KD's refusal of Andersson [KD-I1], [KD-I3]), is likewise not held.

What the pairwise model cannot hold. Each needs a new shape or has to be stated as out of scope; none should be
squeezed into a pair that no party declared (C ↔ V was first on this list until K-1 part (2) built its shape, above):

- **V's in-or-against demand**: [V-P1] (primary), [V-I1], [V-I2], [C-I9]; first form [V-I3]. The wired 2026 result
  needs V to support a cabinet it is not in (MEASURED, above).
- **MP's in-or-against demand**: [MP-I1], [MP-I2] (secondary only; the wording is a GAP). MP's nuclear-power
  condition [MP-I3] has no pair either.
- **SD's in-or-against demand**: [L-C2], [MSD-I4] (SD primary a GAP; its manifesto [SD-P1] is silent).
- **KD's refusal of Magdalena Andersson as PM**: [KD-I1], [KD-I3].
- **L's per-minister consultation clause** (*avstämning*): [L-P1], [L-P1a].
- **Ulf Kristersson as the Tidö side's PM, even if SD is larger**: [MSD-P1], [MSD-I2], [MSD-I4].
- **The four-party majority government** (M, KD, L, SD): [MSD-I2], [MSD-I3], [MSD-I5], restated [MSD-I6]; on
  L's side an "ambition", not a guarantee [L-P1].

## Source register
(all accessed 2026-09-23; `raw/declarations/<file>`; publication times are the page's own metadata)

**Named claim 1 — Liberalerna (L) accepts SD in government**
- [L-P1] https://www.liberalerna.se/nyheter/sverigeloftet — **Liberalerna (primary)**, "Sverigelöftet – ett
  handslag för ansvarstagande", published 2026-03-13T13:40:50Z, modified 2026-04-01T08:55:15Z.
  `liberalerna_sverigeloftet.html` sha256 `1eb2d4ff6dedb2c6595ffaf84ddf3172c77d7fd615944a9b009f23d198a25215`
- [L-P1a] https://web.archive.org/web/20260313162213id_/https://www.liberalerna.se/nyheter/sverigeloftet —
  Internet Archive capture of [L-P1] at **2026-03-13 16:22:13 UTC**, the day of publication. Its body text
  (the agreement and its 15 points) is **identical** to the live page, compared line by line; only
  navigation and tag lines differ, so the 2026-04-01 modification did not change the agreement.
  `wayback_20260313162213_liberalerna_sverigeloftet.html` sha256 `9a4a53dfa513619cae818b29469b47036193ed5137c26bc1c4b4862f7bb032f5`
- [L-P2] https://www.liberalerna.se/nyheter/liberalernas-landsmote-fornyat-fortroende-for-simona-mohamsson —
  **Liberalerna (primary)**, published 2026-03-22T20:16:14Z.
  `liberalerna_landsmote_fornyat_fortroende.html` sha256 `66b3c6caab37b4b5fa3c68708a809e6b815dc5a2e8b2a4e6050af1a1315e3b6f`
- [L-I1] https://www.svt.se/nyheter/inrikes/partitopparnas-missnoje-efter-sd-samarbetet-inte-losningen — SVT,
  2026-03-13T16:48:50+01:00. `svt_partitopparnas_missnoje.html` sha256 `1f0ec11ac9c0688042fb2c2d1ac5096c0a6a12d9fdf4f00f8df8ea0161b1070c`
- [L-I2] https://www.svt.se/nyheter/inrikes/l-och-sd-overens-har-ar-alla-punkter — SVT, 2026-03-13T20:02:54+01:00.
  `svt_l_och_sd_overens_alla_punkter.html` sha256 `99a3ac6ca6d1d1a4bb6ba7b5937d39cc6ccd2ed9a516c0faefa4945cb2b0427a`
- [L-I3] https://www.svt.se/nyheter/inrikes/har-ar-punkterna-i-uppgorelsen-mellan-l-och-sd — SVT, published
  2026-03-13T10:21:18+01:00 (published the morning of the board meeting: the page says the board *"träffas
  idag"* and quotes a member *"på väg in till partistyrelsemötet"*), updated 2026-03-16.
  `svt_punkterna_uppgorelsen_l_sd.html` sha256 `880f8fe2261b2eec1477bb461d732d9839bfa5793817346efd9a7ffe2213b56e`
- [L-I4] https://www.altinget.se/artikel/liberalerna-vander-sager-ja-till-sd-i-regering — Altinget,
  2026-03-13 14:10 (paywalled beyond the lead). `altinget_liberalerna_vander.html` sha256 `c704693d1972eaaf3bd6a2c1bb5903cb87d6adcd4b5a3dfe112afe0f588563fb`
- [L-I5] https://www.svt.se/nyheter/inrikes/odesmote-for-liberalerna-den-avgorande-striden-om-sd — SVT,
  2026-03-22T13:30:41+01:00. `svt_odesmote_liberalerna.html` sha256 `e525933839ee2ab9816c72eddabcf5ed40e05681703c5ab73c0cc166ee1ee87e`
- [L-I6] https://www.svt.se/nyheter/inrikes/mohamson-omvald-efter-enormt-teknikstrul — SVT,
  2026-03-22T22:24:41+01:00. `svt_mohamsson_omvald.html` sha256 `6a5a104779e7a8480153ad20ea3795a14220d75d53e1a7ba0a386088058172b8`
- [L-C1] https://www.aftonbladet.se/nyheter/a/zO5lOO/liberaler-i-upprop-stoppa-sd-samarbetet — Aftonbladet,
  **2025-11-21T09:28:01Z** — the autumn 2025 congress line that March reversed (context).
  `aftonbladet_20251121_l_landsmote_nej_sd_regering.html` sha256 `41a63811aaca6962690f62dec76dceb810c7c7fc336631318a162130342503b8`
- [L-C2] https://www.svd.se/kompakt/flodet/65288 (served as `https://www.svd.se/a/JQ8MW7/svd-kompakts-nyhetsflode?pinnedEntry=65288`)
  — SvD Kompakt, "Åkesson: Regering eller full opposition", **10 okt 2025** (context for L and SD).
  `svd_akesson_regering_eller_full_opposition.html` sha256 `c83ca76e49bf640af79bdb9d25fe2aae07fe0a2978e3a55b2736e3ca5931953f`

**Named claim 2 — the M–SD agreement of 1 April 2026**
- [MSD-P1] https://moderaterna.se/nyhet/ulf-kristersson-leder-en-majoritetsregering-i-host/ — **Moderaterna
  (primary)**, published 2026-04-01T11:19:08Z. `moderaterna_kristersson_leder_majoritetsregering.html` sha256 `c29a8f168a21f8ed03a2255f2ee7bd094a9d661bcd6dcc7e2676f048543541d9`
- [MSD-P2] https://via.tt.se/pressmeddelande/4316145/presstraff-med-ulf-kristersson-och-jimmie-akesson?publisherId=3235744&lang=sv
  — **Moderaterna's own press release** (Via TT), 1.4.2026 09:29:57 CEST.
  `tt_via_m_presstraff_kristersson_akesson.html` sha256 `d3d209ef07c8438a6bfc364e87346db30f93faac960bcee1eb282f12f7e78732`
- [MSD-I1] https://www.svt.se/nyheter/inrikes/m-slapper-in-sd-i-regering-om-tidopartierna-vinner-valet — SVT,
  2026-04-01T12:26:23+02:00. `svt_m_slapper_in_sd_20260401.html` sha256 `eb443b142df85abed01d4ac8134aed31a3f9f61e37cb4d9f33d0b0f2894f3547`
- [MSD-I2] https://www.sverigesradio.se/artikel/moderaterna-vill-styra-med-sverigedemokraterna-efter-valet —
  Sveriges Radio (Radio Sweden på lätt svenska), 2026-04-01T14:21:00Z.
  `sr_moderaterna_vill_styra_med_sd.html` sha256 `c8f80391fb367186a17fc59c75da768e4b60c831ac3a4d731624aa0c9739a080`
- [MSD-I3] https://www.svd.se/a/GxxmLV/uppgifter-m-lovar-slappa-in-sd-i-regeringen — SvD,
  2026-04-01T08:43:53Z, modified 12:30:51Z. `svd_klart_m_slapper_in_sd.html` sha256 `a3d06d4dba31ddb8fe96bee2f33b7e4d22cf5885e044c55cae32b539ddca8f5b`
- [MSD-I4] https://www.tv4.se/artikel/3YyULIehxT1GocPqU1rAye/uppgifter-moderaterna-slaepper-in-sd-i-regering-efter-valet
  — TV4 Nyheterna, 2026-04-01T08:42:37Z, modified 12:12:25Z. `tv4_uppgifter_m_slapper_in_sd.html` sha256 `c61df45352b868f2bc3910a2b81d81d66b044745479a5e65e5542887bc0849f1`
- [MSD-I5] https://efn.se/sverigedemokraterna-och-moderaterna-overens-om-majoritetsregering — EFN /
  Nyhetsbyrån Direkt, 2026-04-01 12:10. `efn_sd_m_majoritetsregering.html` sha256 `e398fbc28afef8aa3cc96056279a75c5ea31f16ec1018a5acef4052d8facc499`
- [MSD-I6] https://www.svt.se/nyheter/inrikes/kristerssons-besked-det-blir-inte-12-sd-ministrar — SVT,
  2026-09-02T16:29:05+02:00 — the pledge as it stood eleven days before the vote.
  `svt_kristersson_inte_tolv_sd_ministrar.html` sha256 `1bb3583753e7ac6542ef1fc185e3226cdbd77ef692a4bd3fc71eabe3b56db57e`

**Named claim 3 — Centerpartiet (C) excludes SD and V**
- [C-P1] https://www.centerpartiet.se/centerpartiet-lokalt/orebro-lan/orebro-lan/nyheter/nyhetsarkiv/2026-01-30-vi-tanker-aldrig-slappa-in-vansterpartiet-i-en-regering
  — **Nerikes Allehanda, 2026-01-28** (the page's own line under the headline: *"Nerikes Allehanda 260128"*),
  **reposted** by Centerpartiet Örebro län on its district news page 2026-01-30T14:29:18+01:00; reports the
  press conference at which the party's valplan was presented. **A newspaper article, not a party statement:**
  only its dash-quoted lines are Thand Ringqvist's words, and *"Partiledaren ger besked om att …"* is the
  newspaper's paraphrase. (The id is kept for stability; its P marks only where the page was found.)
  `centerpartiet_orebro_20260130_aldrig_slappa_in_v.html` sha256 `748727d22fa0b87d50f8f1440b7f31ffddb9253e326818f8156cbb52bbd935e9`
- [C-P2] https://www.centerpartiet.se/nyheter/arkiv-2026/2026-08-11-elisabeth-thand-ringqvist-akesson-visar-varfor-sd-inte-hor-hemma-i-en-svensk-regering
  — **Centerpartiet (primary)**, 2026-08-11T15:06:55+02:00.
  `centerpartiet_20260811_akesson_visar_varfor_sd.html` sha256 `8028fdf6b0129e5b81f54e1dc533a2d2821a4b3935189f73d418f69f1d892a3f`
- [C-P3] https://www.centerpartiet.se/nyheter/arkiv-2026/2026-08-19-centerpartiet-lanserar-nytt-alternativ-en-stark-mittenregering
  — **Centerpartiet (primary)**, 2026-08-19T10:12:13+02:00. `centerpartiet_20260819_mittenregering.html` sha256 `9d364ab9bf773ac2d1ef283481eb3ede5bd9255f888225a9e55b913d3734ddcb`
- [C-P4] https://www.centerpartiet.se/nyheter/arkiv-2026/2026-08-26-annie-loof-sverige-behover-en-regering-forankrad-i-mitten
  — **Centerpartiet (party's own domain)**: the party's **summary of former leader Annie Lööf's interview with
  Dagens Nyheter**, 2026-08-26T18:45:21+02:00. The line quoted below is Lööf's point 3, not a statement by the
  party leader. `centerpartiet_20260826_loof_regering_i_mitten.html` sha256 `88078075897a6638ce23a88f3f8edcac2ee769f92483d6e82dad7c57d4479685`
- [C-P5] https://svenskatal.se/tal/elisabeth-thand-ringqvist-installationstal-pa-partistamman-2025 — Svenska
  tal (third-party transcript), Thand Ringqvist's installation speech, **13 november 2025**, Karlstad; the
  page states *"Manuskript hämtat från centerpartiet.se (2025-11-13)"*. A **404** for the centerpartiet.se
  original was noted on 2026-09-23, but **no URL for it was recorded** and the Svenska tal page carries that note
  as plain text with no link, so the 404 cannot be reproduced and is **unverifiable**; no centerpartiet.se copy
  is saved. `svenskatal_thand_ringqvist_installationstal_2025.html` sha256 `4ddcbb129dcdd45677d593ac81f046862ffe74b450ae00fd9c5e56b74f44ffa5`
- [C-I1] https://www.svt.se/nyheter/inrikes/thand-ringqvist-hellre-extraval-an-v-i-regering — SVT,
  2026-09-08T21:41:44+02:00 (updated 09-10). `svt_thand_ringqvist_hellre_extraval.html` sha256 `31c25449c5b9b825ea0cbcda9da74c8aa3551b3ea92066aefc0fe568d883239d`
- [C-I2] https://www.dn.se/sverige/c-magdalena-andersson-ar-var-mest-sannolika-statsministerkandidat/ — DN,
  2026-06-09T12:31:53+02:00 (paywalled; lead only). `dn_20260609_c_andersson_mest_sannolika.html` sha256 `e6c88790d6d62c8ab0d11cfe815873f3b261d1c87b7110b4d269f18978f73670`
- [C-I3] https://www.aftonbladet.se/nyheter/a/V6gWz4/thand-ringqvist-om-v-inbjudan-inte-seriost — Aftonbladet,
  2026-04-21T08:16:22Z, headline "”Vi kan inte sitta i en sådan regering”" (paywalled; lead only).
  `aftonbladet_20260421_thand_ringqvist_v_inbjudan.html` sha256 `23b49cab7e41b54415ca1bbd3b735cfd057cd03c5bb7b808461ba27920b63c33`
- [C-I4] https://www.aftonbladet.se/nyheter/a/pB4oq1/c-ledaren-om-sin-onskeregering-later-omojligt — Aftonbladet,
  2026-08-27T03:30:12Z. `aftonbladet_20260827_c_onskeregering.html` sha256 `28ccc47334a6b0e0a534eaf93048e5fb271f987271dc021597c35608d6483878`
- [C-I5] https://bulletin.nu/thand-ringqvist-tydligt-nej-till-v-i-regeringen — Bulletin (citing Expressen),
  2026-09-09. `bulletin_thand_ringqvist_tydligt_nej_v.html` sha256 `0624bdebfda0ec3f3438b30be93b0d5af4f65f15cb45d855cc895840be9f2347`
- [C-I6] https://www.tv4.se/artikel/2Ksx0jADYrZf4ZsMOVedWd/beskedet-fran-c-distrikten-roed-linje-mot-v-ska-ligga-fast
  — TV4 Nyheterna, **2026-09-14** (after the election). `tv4_c_distrikten_rod_linje_v.html` sha256 `f93690c6320a223dab0febc84d7d6231ae6865ec826fd9a29c76f3f0c51c6c53`
- [C-I7] https://www.gp.se/politik/centerns-forsta-ord-valjarna-vill-ha-mittenpragel.fcc87875-36bb-48a5-b347-acb54838f445
  — Göteborgs-Posten, **2026-09-14** (after the election). `gp_centerpartiet_bryter_tystnaden.html` sha256 `c6d1d39d286d252f15f59dc9bdea16e1e78c690ac917d1c8c2846d5e71e68c25`
- [C-I8] https://bulletin.nu/lasningen-mellan-c-och-v-bestar-helt-uteslutet — Bulletin (citing Expressen),
  **2026-09-18** (after the election). `bulletin_lasningen_c_v.html` sha256 `1e62cff4d69e5e0a0bf5667ad28aa5373de5fc599c4e92ce7e7b9474ab938296`
- [C-I9] https://www.svt.se/nyheter/inrikes/har-overtag-i-opinionen-har-gar-oppositionens-roda-linjer — SVT,
  2026-08-25T10:35:12+02:00 (also carries V). `svt_oppositionens_roda_linjer.html` sha256 `7519459ebe1b70f5a1dc34fbb0f08b864193d38dfe9127a342f438ae1ad4e7b0`
- [C-I10] https://www.tv4.se/artikel/6Udk3cco5xbl91GgAkhkqq/c-ledaren-doerren-inte-staengd-formellt-foer-v-samarbete
  — TV4 Nyheterna, "C-ledaren: Dörren inte stängd formellt för V-samarbete", 2026-01-30T12:40:17Z (added by the
  verification pass). `tv4_c_ledaren_dorren_inte_stangd_formellt_v.html` sha256 `4c76599e796b0535a3dd116b876e38ad861a1169f0fde43d633a4714052a2902`

**The other parties (kept apart from the three named claims)**
- [S-I1] https://borsvarlden.com/artiklar/magdalena-andersson-socialdemokraterna-gar-till-val-som-ett-enskilt-parti
  — Börsvärlden (citing DN), 2026-04-09T13:35:11Z. `borsvarlden_andersson_enskilt_parti.html` sha256 `e16ce64a548f437ac874141cccb9ccb800f8b426436c78d903837bd9dfc04554`
- [S-I2] https://www.svt.se/nyheter/inrikes/magdalena-andersson-vill-bli-statsminister-igen-men-vagrar-saga-hur —
  SVT, 2026-08-27T12:54:59+02:00. `svt_andersson_vill_bli_statsminister_vagrar_saga_hur.html` sha256 `0387bb7053a76ddd23b20388c1c6767326804ad11cb2e0c9400174e89820319d`
- [S-I3] https://nyheter24.se/nyheter/politik/1512663-andersson-oppnar-for-att-styra-ensam-det-ar-en-mojlighet —
  Nyheter24 (citing SVT and TT), 2026-09-12T17:02:00Z. `nyheter24_andersson_styra_ensam.html` sha256 `1ae20eefb25beafc584538930f1804f3c4e6c82c58260ba8eecfdcf38e3ea0e6`
- [S-I4] https://www.altinget.se/artikel/darfor-undviker-andersson-besked-i-regeringsfraagan — Altinget,
  2026-06-17, **an opinion column** (Ulf Bjereld) — the weakest source in this file, used only to corroborate
  [S-I1]. `altinget_darfor_undviker_andersson_besked.html` sha256 `79cf111747a235ae0afcb27df91dd0c217af375c3e1261a0d59aa09a825b491e`
- [V-P1] https://www.vansterpartiet.se/wp-content/uploads/2026/04/Preliminar-Valplattform-efter-beslut-pa-kongressen-2026.pdf
  — **Vänsterpartiet (primary)**, "Vänsterpartiets valplattform efter beslut på kongressen 2026", header
  "Preliminär version kongress 2026", 4 pages. `vansterpartiet_valplattform_kongress_2026.pdf` sha256 `c29dc98da5e2a7b77a8c6b7d66d5ee1bc162bdedbed9e9d05a3f6b94b5dfe9ef`
- [V-P2] https://www.vansterpartiet.se/kongress2026/ — **Vänsterpartiet (primary)**, congress dates.
  `vansterpartiet_kongress2026.html` sha256 `e28f250b737acba4e44474b382d8d4f32b673d7a041dc0689d7f7179757e852c`
- [V-I1] https://www.sverigesradio.se/artikel/vansterpartiet-star-fast-vid-regeringskrav — Sveriges Radio (Ekot),
  2026-04-18T12:45:00Z. `sr_v_star_fast_vid_regeringskrav.html` sha256 `ff96c4faddd37975ade7685138d7f681e16f44c4e8e20d36a1ee72abdc89395d`
- [V-I2] https://www.sverigesradio.se/artikel/vansterpartiet-kraver-att-fa-vara-med-i-en-regering-efter-valet —
  Sveriges Radio (lätt svenska), 2026-04-20T11:21:00Z. `sr_v_kraver_att_fa_vara_med_i_regering.html` sha256 `e0f06123a47c3424d7e7547d3da22e34d6e6a5969b0657e0db2b247d0ed32d19`
- [V-I3] https://www.svt.se/nyheter/inrikes/kravet-fran-vansterpartiet-ministerposter-eller-rod-knapp — SVT,
  2025-09-30T11:52:00+02:00 (the line's first form). `svt_kravet_fran_v_ministerposter_eller_rod_knapp.html` sha256 `ede9452dd785e7291f40fdc156972491be84d856e1819d9beb55933b4fb3a7ba`
- [MP-P1] https://www.mp.se/just-nu/sprakrorens-sommartal-2026/ — **Miljöpartiet (primary)**, "Språkrörens
  sommartal 2026", 2026-08-12T19:30:59+02:00. `mp_sprakrorens_sommartal_2026.html` sha256 `537828bf0f1528fb2a8e7a5f7a2ec9709c8c17c70046279f82e70a6ed4b66416`
- [MP-I1] https://www.sverigesradio.se/artikel/helldens-mp-krav-vi-ska-sitta-i-nasta-regering — Sveriges Radio,
  2026-08-10T09:59:00Z (**the fetched HTML carries the headline and summary only**, not the condition's wording).
  `sr_hellden_mp_krav_sitta_i_regering.html` sha256 `96477e5c0255a323c803ae1604cac392359ce6929e4f28a3324bcc8fdf4a3790`
- [MP-I2] https://bulletin.nu/vi-ska-sitta-i-nasta-regering — Bulletin (citing Sveriges Radio), 2026-08-10.
  `bulletin_vi_ska_sitta_i_nasta_regering.html` sha256 `322de34f91f8170f41048f428953ba42ca7cb3df2808b7c77ac7d66c6598891d`
- [MP-I3] https://www.svt.se/nyheter/inrikes/daniel-hellden-mp-vi-kan-inte-sitta-i-en-regering-som-bygger-ny-karnkraft
  — SVT, 2026-01-22T17:43:44+01:00. `svt_hellden_karnkraft_regering.html` sha256 `4aca640972330c7b027fb4280d3adddf1c1c9d1b24932185729a5ccd9a475ce1`
- [KD-I1] https://www.svt.se/nyheter/inrikes/senaste-nytt-om-val-2026?inlagg=9921e87d8857608bb79ee35589fbe1c1 —
  SVT live post "Busch: Nej till Andersson fram till ett nyval", **2 sep 11:58** (article:published_time
  2026-09-02T11:58:33+02:00). `svt_live_busch_nej_till_andersson.html` sha256 `e24643678cb75583d96e41f3334d461e420acf938c87192a0243d56c5fd070b9`
- [KD-I2] https://bulletin.nu/busch-inte-givet-att-sd-ska-ha-nagra-ministerposter — Bulletin (quoting Busch's
  post on X), 2026-09-08T13:36:10+02:00. `bulletin_busch_inte_givet_sd_ministerposter.html` sha256 `f737cfb9bb95aaf5d5d2e032a683a26c429c6de588505a1bc1644954cbd96002`
- [KD-I3] https://kvartal.se/nyheter/artiklar/kd-hellre-nyval-an-sitta-i-s-regering/cG9zdDoxMjY5MDc — Kvartal
  (citing Dagens Industri), 2026-06-05T12:38:02+02:00. `kvartal_kd_hellre_nyval.html` sha256 `a3939eba55f88ea0cbcf20c9bf435060a71a1fe12bcdd09f46535629fd9c2cc5`
- [SD-P1] https://via.tt.se/files/3236128/4533611/410703/sv — **Sverigedemokraterna (primary)**, "Valmanifest
  2026.pdf", attached to SD's press release of 7.9.2026. **Checked and found silent on the government
  question** (text extracted from its content streams; no sentence on government participation). Kept as the
  record of that negative check. `sd_valmanifest_2026.pdf` sha256 `7ebe576c2cd22a0a59ada68aace0a439a795c7c33dcdcf0fd145d1dc588e6e75`

**Lead only — no claim rests on it**
- [W] https://sv.wikipedia.org/wiki/Riksdagsvalet_i_Sverige_2026 — used to find the sources above; four
  citation errors found in it are listed at the end. `wiki_sv_riksdagsvalet_2026.html` sha256 `8bda7e804d5f7b4b8fbdc0b425ce5b092bfce3ae413b0936dda572fb23aefd66`

## The three named declarations — verdict against the sources

| named in the ruling | verdict | date as sourced | the one thing to know |
|---|---|---|---|
| (1) L's March 2026 acceptance of SD in government | **MATCHES** | **13 March 2026** (party board + joint L–SD announcement); stood after the extra congress of **22 March 2026** | It is an **ambition with a consultation clause**, not a guarantee, and the congress never voted on the line itself — it re-elected the leader who made it |
| (2) the M–SD agreement of 1 April 2026 | **MATCHES** | **1 April 2026** (joint press conference 11.30, Hotel Skeppsholmen) | **No written text was found.** The terms are the two leaders' spoken words, quoted verbatim by four newsrooms; M's own page confirms the majority government and Kristersson as PM but does not itself spell out "SD in government" |
| (3) C excluding both SD and V | **MATCHES in substance — but the two exclusions are NOT the same strength** | SD line from **13 Nov 2025**; V line first found **28 Jan 2026** (a Nerikes Allehanda article, reposted on C's Örebro district page 30 Jan); both restated to **8 Sep 2026**; both held **after** the election (14 and 18 Sep) | SD is excluded **from government and as the base of any government C backs** (support-blocking). V is excluded from **any cabinet V sits in**: C will not sit in, support or let through such a cabinet. That is stronger than the model's cabinet-blocking and weaker than its support-blocking, a third shape neither symmetric strength holds; no fetched source that names a mechanism refuses V as a mere support party (the statements that V should have no influence name none). Wired **one way** (`RedLine.OneWay`, a shape built for it by K-1 part (2), §603) |

### (1) Liberalerna — the 2022 line lifted

| pair | strength | vintage | basis |
|---|---|---|---|
| **L ↔ SD** | 2022's cabinet-blocking line **LIFTED**: L now aims for a government in which **SD sits**, with each minister subject to consultation between that minister's party leader and the PM | reversed the congress line of **2025-11-21**; decided **2026-03-13**; stood after **2026-03-22** | see below |

- **What was agreed, in L's own words** — Liberalerna, [L-P1], "Fredag 13 mars 2026": *"Liberalerna och
  Sverigedemokraterna har enats om en gemensam överenskommelse för fortsatt samarbete inför nästa
  mandatperiod."* and *"Om Tidöpartierna vinner väljarnas förtroende i nästa val är ambitionen att **alla
  fyra partier ska ingå i regeringen**."* Point 1 of the fifteen: *"1. Om Tidöpartierna vinner valet är det
  eftersträvansvärt att alla fyra partier (M-SD-KD-L) sitter i regering. Statsråden i denna regering ska visa
  prov på det ansvarstagande och den trovärdighet som krävs i allvarstid och ska därför vid utnämnande vara
  föremål för avstämning mellan respektive partiledare och statsministern."* The same sentences stand in the
  Internet Archive capture of 13 March 16:22 UTC [L-P1a].
- **Which body decided, and by how much** — SVT, [L-I1], 2026-03-13: *"Under fredagsförmiddagen träffades
  Liberalernas partistyrelse för att diskutera regeringsfrågan och om partiet ska öppna upp för att låta
  Sverigedemokraterna ingå i en regering efter nästa val – något man på höstens landsmöte beslutat att man
  ska säga nej till."* and *"Enligt källor till SVT blev röstsiffrorna 13 för och åtta emot i frågan om att
  öppna för SD i regering."* ⚠ The 13–8 count is **anonymously sourced**; no L page fetched states a count.
- **Independent confirmation of the content** — SVT, [L-I2], 2026-03-13: *"Liberalerna har svängt – partiet
  accepterar att Sverigedemokraterna kan sitta i en regering."* Altinget, [L-I4], 2026-03-13: *"Liberalerna
  går ifrån sin röda linje och säger ja till att släppa in SD i en framtida regering."*, quoting Mohamsson:
  *"Med den här uppgörelsen fördjupas vårt blågula samarbete på flera områden som är avgörande för att vi i
  Liberalerna ska kunna sitta i en regering tillsammans med Sverigedemokraterna."*
- **What the congress of 22 March did and did not vote on** — SVT, [L-I5]: *"Dagens extrainsatta landsmöte
  har bara en punkt på dagordningen: att besluta om Simona Mohamsson ska bli omvald som partiledare."* and
  *"I praktiken gäller frågan om partiet står bakom beslutet att slopa den röda linjen mot
  Sverigedemokraterna."* The count — SVT, [L-I6]: *"Simona Mohamsson får fortsatt förtroende som
  Liberalernas partiledare – 95 röstade för och 80 avstod."* L's own record — [L-P2]: *"I dag beslutade
  Liberalernas landsmöte om att ge förnyat förtroende till Simona Mohamsson som partiordförande."*, quoting
  her: *"Nu har vi fattat ett viktigt beslut för vårt partis framtid. Tio års vånda är över. Genom
  Sverigelöftet finns nu ett samlat regeringsalternativ på vår sida av politiken."*
- **The line it reversed** (context) — Aftonbladet, [L-C1], 2025-11-21: *"Liberalernas landsmöte säger nej
  till att släppa in SD i en ny Tidöregering efter nästa val."* and *"Landsmötets beslut innebär att L inte
  ska släppa in SD i en regering."*

*gloss:* the model should read L's 2026 position as "SD in cabinet: accepted", and should NOT read it as a
promise of a four-party cabinet — L's own words are "ambition" and "desirable", with a per-minister
consultation clause (*avstämning*) that no mechanism in the model represents.

### (2) Moderaterna and Sverigedemokraterna — 1 April 2026

| pair | strength | vintage | basis |
|---|---|---|---|
| **M ↔ SD** | 2022's cabinet-blocking line **LIFTED**: M will form a **four-party majority government** in which SD has "important ministerial posts" | announced **2026-04-01**, restated **2026-09-02** | see below |
| **prime minister** | **Ulf Kristersson (M)** is the Tidö side's PM candidate; SD accepts this **even if SD is larger** | **2026-04-01** | see below |

- **The event, from M's own press office** — Moderaterna, [MSD-P2], 1.4.2026 09:29:57 CEST: *"Idag, onsdagen
  den 1 april, bjuder Moderaternas partiledare Ulf Kristersson och Sverigedemokraternas partiledare Jimmie
  Åkesson in till pressträff."* *"Tid: Klockan 11.30"*, *"Plats: Hotel Skeppsholmen, Gröna gången 1, 111 49
  Stockholm"*.
- **M's own page** — Moderaterna, [MSD-P1], 2026-04-01: *"Ulf Kristersson leder en majoritetsregering i höst
  om vi får väljarnas stöd."* and *"Att Sverigedemokraterna och Jimmie Åkesson nu sluter upp bakom Ulf
  Kristersson är välkommet."* ⚠ This page does **not** itself say that SD will sit in the government; that
  term is carried by the independent reports below, which quote Kristersson verbatim.
- **The three terms, in Kristersson's words** — Sveriges Radio, [MSD-I2], 2026-04-01: *"Vi är överens om, för
  det första, att vi efter valet bildar en fyrparti majoritetsregering. För det andra, att jag kommer att
  bilda och som statsminister leda den regeringen. Och för det tredje, att i den regeringen kommer
  Sverigedemokraterna att ha ett stort sakpolitiskt inflytande och viktiga ministerposter."* The same three
  terms in TV4's transcription, [MSD-I4]: *"Vi är överens om att vi efter valet bildar en
  fyrpartimajoritetsregering som håller kursen från den här mandatperioden."*
- **Who is PM, whoever is larger** — TV4, [MSD-I4]: *"Även om SD skulle bli största parti är det enligt
  överenskommelsen inte aktuellt för Åkesson att bli statsministerkandidat framför Ulf Kristersson i en
  högerregering. Det uppger båda partiledarna."* Sveriges Radio, [MSD-I2]: *"Jimmie Åkesson säger att de är
  överens om att Ulf Kristersson blir statsminister, även om Sverigedemokraterna är större än
  Moderaterna."*
- **Which parties** — SvD, [MSD-I3]: *"Regeringen ska bestå av M, KD, L och SD, enligt partiledarna."*
  SVT, [MSD-I1]: *"Moderaterna släpper in Sverigedemokraterna i regering och Ulf Kristersson är
  statsministerkandidat om Tidöpartierna vinner valet i höst."* EFN / Direkt, [MSD-I5]: *"Sverigedemokraterna
  och Moderaterna är överens om att efter valet försöka bilda en majoritetsregering med fyra partier."*
- **Why then** — SvD, [MSD-I3], quoting Kristersson: *"Nu har de parlamentariska förutsättningarna ändrats
  på vår sida av politiken efter Liberalernas modiga beslut"*; and *"Han har tidigare sagt att han inte tänkt
  ge besked om SD före valet."* — i.e. claim (2) follows from claim (1), nineteen days later.
- **Still standing at the election** — SVT, [MSD-I6], 2026-09-02: *"Men statsminister Ulf Kristersson, som har
  lovat att Sverigedemokraterna kommer ingå i regeringen, skrattar när SVT frågar honom om det kan bli så
  många SD-ministrar i en regering som han leder."*; Kristersson: *"Nej, vi är fyra partier så det kan det
  inte bli."*

### (3) Centerpartiet — two exclusions of different strength

| pair | strength | vintage | basis |
|---|---|---|---|
| **C ↔ SD** | will not let SD into government **and will not support a government dependent on SD** — support-blocking, the 2022 line's strength | **2025-11-13** (installation speech); **2026-01-28** (Nerikes Allehanda, reposted 01-30); **2026-01-30** (TV4); **2026-08-11**; **2026-09-08**; held after the election **2026-09-14** | see below |
| **C ↔ V** | will not sit in, support or let through **any cabinet that contains V**; would rather go to an extra election than let such a cabinet through. **Stronger than cabinet-blocking, weaker than support-blocking** (no source that names a mechanism refuses V as a mere support party); wired **one way** (`RedLine.OneWay`, a shape built for it by K-1 part (2), §603) | first found **2026-01-28** (Nerikes Allehanda, reposted 01-30); **2026-01-30** (TV4); **2026-04-21**; **2026-08-25**; **2026-09-08**; held after the election **2026-09-14**, **2026-09-18** | see below |
| **C → PM candidate** | Jan 2026: Andersson **or** Kristersson; by June 2026 Kristersson is ruled out while bound to SD | **2025-11-13** (conditional), **2026-01-28** (Nerikes Allehanda, reposted 01-30), **2026-06-09** | see below |

- **Both lines together, in a newspaper article reposted on a district page** — Nerikes Allehanda, 2026-01-28,
  reposted by Centerpartiet Örebro län 2026-01-30, [C-P1] (the page's line under the headline: *"Nerikes
  Allehanda 260128"*). This is the first statement of the V line in the fetched set, but it is **not a party
  statement**: only the dash-quoted lines are Thand Ringqvist's words. Hers: *"– Vi kommer aldrig att acceptera
  en regering med Vänsterpartiet, säger Centerledaren Elisabeth Thand Ringqvist."* and, on an extra election,
  *"På den raka frågan, är vi beredda att gå till nyval, så är svaret ja."* The newspaper's paraphrase, not her
  words: *"Partiledaren ger besked om att C aldrig tänker släppa in Vänsterpartiet i en regering och heller inte
  stötta en regering som är beroende av Sverigedemokraterna."* The partners, in the newspaper's reported speech:
  *"C kan sitta i regering eller samarbeta med fem av dagens åtta riksdagspartierModeraterna,
  Kristdemokraterna, Liberalerna, Socialdemokraterna och Miljöpartiet, fortsätter hon."* [sic — the two words
  run together in the source]. The PM candidates, in the newspaper's words: *"Det är bara två
  statsministerkandidater som C kan samarbeta med, S-ledaren Magdalena Andersson eller M-ledaren Ulf
  Kristersson."* On budget cooperation with V — left open, in the newspaper's words: *"På frågan om Centern kan
  budgetsamarbeta med V, om partiet skulle stå utanför regeringen, så säger Thand Ringqvist att frågan är
  hypotetisk"*.
- **Two days later, the one fetched source that answers the question of V as a mere support party directly**
  (the budget question in [C-P1], above, was called *"hypotetisk"*, and she *"hänvisar till att V-ledaren Nooshi Dadgostar sagt att hon tänker rösta ner varje regering som V inte sitter i"*) — TV4, [C-I10], 2026-01-30 (reporting
  her speech to the party's kommundagar in Västerås, and an interview): *"Centerpartiet kommer inte att släppa fram en regering som
  Vänsterpartiet ingår i."* and *"C kommer inte stödja en regering som SD har inflytande över. C kommer inte
  heller stödja en regering som V ingår i."* But: *"Men möjligheten för ett budgetsamarbete eller att stödja en
  S-regering som i sin tur tar stöd av V, är inte uteslutet, enligt Elisabeth Thand Ringqvist."*, and her words:
  *"– Om det är så att Vänsterpartiet backar och säger att de inte behöver sitta i regering och kan samarbeta
  på något sätt så är det ny information och då får jag ta tillbaka det till min partistyrelse och diskutera
  det."* TV4 asks *"Den dörren är inte formellt stängd?"*; she answers *"– Den är inte formellt stängd, nej."*
  This is the **earlier** statement. The September statements below ([C-I1], [C-I6], [C-I8]) supersede it as the
  latest word on **V in government**, which they restate. [C-I1] and [C-I6] do not revisit this door; [C-I8]
  reports that C had floated a pure S minority, which V rejects; no fetched source says the door was closed.
- **The SD line is older than the V line** — installation speech, [C-P5], 2025-11-13 (transcript of the
  centerpartiet.se manuscript): *"Ulf Kristersson har valt att prioritera ett samarbete med ett parti som vi
  aldrig haft ett samarbete med, och där ett framtida samarbete är otänkbart. Om Centerpartiet och
  Moderaterna ska kunna samarbeta igen, då behöver Ulf Kristersson släppa sitt beroende av
  Sverigedemokraterna."* The speech does **not** name Vänsterpartiet at all (0 occurrences).
- **The SD line on the national site** — Centerpartiet, [C-P2], 2026-08-11, quoting her X post: *"Jimmie
  Åkesson visar i P1 varför Sverigedemokraterna inte hör hemma i en svensk regering."*
- **Both together, as the campaign's frame** — Centerpartiet, [C-P3], 2026-08-19: *"Centerpartiet vill se ett
  samarbete i mitten för att bygga ett Sverige med möjligheter, framtidstro och nybyggaranda, utan inflytande
  från ytterkantspartierna."* and *"En regering med Vänsterpartiet eller Sverigedemokraterna skulle föra
  Sverige i en annan riktning."* Centerpartiet's summary of former leader Annie Lööf's DN interview, [C-P4],
  2026-08-26, her point 3, not a statement by the party leader: *"Varken Vänsterpartiet eller
  Sverigedemokraterna bör ha inflytande över regeringsbildningen. Det är en linje som Lööf delar med
  Centerpartiets partiledare Elisabeth Thand Ringqvist"*.
- **Independent, five days before the vote** — SVT, [C-I1], 2026-09-08 (Utfrågningen): *"Centerpartiet går
  hellre till extraval än att släppa fram en regering där Vänsterpartiet ingår."*; *"Men hon säger absolut
  nej till en regering med Vänsterpartiet och kan inte heller tänka sig att samarbeta med
  Sverigedemokraterna."*; her words: *"Vi har ett tydligt nej till Vänsterpartiet i regering, säger hon."*
  Also SVT, [C-I9], 2026-08-25: *"Centerpartiet däremot vill se en mittenregering bestående av S, C, MP och
  KD och har en röd linje mot V i regeringen."*
- **The PM question** — DN, [C-I2], 2026-06-09 (lead): *"Enligt C är Ulf Kristersson diskvalificerad på grund
  av sitt samarbete med Sverigedemokraterna."* and *"Därför är Magdalena Andersson och Socialdemokraterna den
  mest sannolika samarbetspartnern för C efter valet, säger C-ledaren Elisabeth Thand Ringqvist."* Preferred
  cabinet — Aftonbladet, [C-I4], 2026-08-27: *"Elisabeth Thand Ringqvist har gjort klart att hon vill sitta i
  regering tillsammans med S, KD och kanske MP efter valet."*
- **Held after the election** — TV4, [C-I6], 2026-09-14 (a ring-round of district chairs): *"Partiet bör hålla
  fast vid de röda linjerna mot både Vänsterpartiet och Sverigedemokraterna."* and, on the party secretary,
  *"Samtidigt slog han fast att Centerpartiet inte vill se vare sig Sverigedemokraterna eller Vänsterpartiet få
  inflytande."* Bulletin (citing Expressen), [C-I8], 2026-09-18: *"Centerpartiet säger blankt nej till varje
  regering där V ingår."*, and Thand Ringqvist: *"Eftersom syftet med mötet är att regeringsförhandla är det
  ingen större poäng att gå till det mötet eftersom vi menar att vi inte kan sitta i regering med
  Vänsterpartiet"*.
- **Why the V line is neither of the model's two symmetric strengths, and why it is wired one way** — the
  model's cabinet-blocking line means *"I will not sit in a cabinet with you"* (`CoalitionFormation.cs`
  lines 27-31). A cabinet-only C–V line would still let C support an S+V cabinet from outside, and the sources
  rule that out: *"Centerpartiet går hellre till extraval än att släppa fram en regering där Vänsterpartiet
  ingår."* (SVT, [C-I1], 2026-09-08);
  Aftonbladet's free lead, [C-I3], 2026-04-21, on a red-green government V sits in: *"Det finns inte en chans
  att C stödjer en sådan regering, enligt Centerledaren."*; *"C kommer inte heller stödja en regering som V ingår
  i."* (TV4, [C-I10], 2026-01-30); and *"aldrig att acceptera en regering med Vänsterpartiet"* (Nerikes
  Allehanda, [C-P1], 2026-01-28). So what C refuses is **any cabinet that contains V**, whether C would sit in
  it, support it or let it through. It is **not** support-blocking either: every fetched statement of the V line
  that names a mechanism is about V **in** the government (*"en regering med Vänsterpartiet"*, *"en regering där
  Vänsterpartiet ingår"*, *"regering där V ingår"*), and no fetched source that names a mechanism refuses V as a
  mere support party. Only the SD line is stated against a government **dependent on** a party (*"en regering
  som är beroende av Sverigedemokraterna"*, the newspaper's paraphrase in [C-P1]; *"C kommer inte stödja en
  regering som SD har inflytande över."*, [C-I10]). The statements on V's **influence** are broader and name no
  mechanism: her own words in [C-I10], *"- Jag kommer att arbeta hårt för att Sverigedemokraterna och
  Vänsterpartiet inte ska ha något verkligt inflytande, sa Centerledaren vidare."*, though the same article
  leaves the support door *"inte formellt stängd"*; the party secretary in [C-I6], *"inte vill se vare sig
  Sverigedemokraterna eller Vänsterpartiet få inflytande"*; Lööf's point 3 in [C-P4], *"Varken Vänsterpartiet
  eller Sverigedemokraterna bör ha inflytande över regeringsbildningen."*; the campaign frame [C-P3],
  *"utan inflytande från ytterkantspartierna"*; and, in Nerikes Allehanda's reported speech [C-P1], *"”Ytterkantspartier” ska hållas borta från inflytande, säger hon."*, though the same article calls the budget question *"hypotetisk"*. And Bulletin, [C-I8], reports that C had itself floated a pure S
  minority: *"V-ledaren Nooshi Dadgostar avvisar tanken på en ren S-minoritet, en lösning C tidigare fört
  fram."* (secondary, citing Expressen). The line is therefore a **third shape** that neither symmetric strength of the pairwise `RedLine`
  holds, next to the in-or-against demands. It is wired **one way** (`RedLine.OneWay`, a shape built for it by
  K-1 part (2), §603; the ruling itself says "C excluding both SD and V"): `RedLine(C, V, Declared, blocksSupport:
  true, oneWay: true)` refuses any cabinet with both in it, refuses C's support to a cabinet V sits in and counts C
  against it at the investiture, and refuses nothing in V's direction (`RedLine.RefusesSupport`,
  `CoalitionFormation.cs` line 64; the oppose loop in `Form`, lines 300-309). Symmetric support-blocking was not used
  because it over-forbids two cases that no fetched source naming a mechanism has C refuse: C supporting from outside an S cabinet without V
  that V also supports from outside, which the model would not allow because supporters that red-line each other
  cannot both stay and the weaker by negotiating power drops (`SupportersOf`, lines 450-468, which skips a one-way
  line, lines 459-460); and its mirror, V supporting from outside a cabinet C sits in, refused by the model's two-way
  support rule for symmetric lines (lines 220-222). On the seated 2026 chamber that mirror decides the government
  (MEASURED, in the FORMATION block). Both cases need V to support a cabinet V is not in, which V's own in-or-against demand [V-P1]
  refuses wherever V's votes are needed (*"vi inte kommer att stödja eller släppa fram en regering som vi inte
  ingår i"*); that is why TV4's door is conditional on V dropping its demand. **No fetched source says C refuses the first case.** The one fetched source that answers it
  directly is TV4's of 2026-01-30 [C-I10], above: *"inte formellt stängd"*, conditional on V dropping its demand
  to sit in government. Nerikes Allehanda [C-P1] records the budget question called *"hypotetisk"*, and Bulletin,
  citing Expressen [C-I8], 2026-09-18, reports a pure S minority as *"en lösning C tidigare fört fram"*, which V
  rejects.

## The other parties' declared positions as of the 2026 election (not named by the ruling)

Kept apart from the three named claims on purpose: none of these was ruled on, and several rest on
secondary reports only, which the basis column says.

| party | declared position | vintage | basis |
|---|---|---|---|
| **M — PM candidate** | **Ulf Kristersson**, as the four Tidö parties' candidate | 2026-04-01 → 2026-09-02 | Moderaterna, [MSD-P1]: *"Därför är Ulf Kristersson den självklara regeringsbildaren efter valet 2026."* SVT, [MSD-I1]: *"Ulf Kristersson är samarbetets statsministerkandidat och ett stort inflytande ska ges till Sverigedemokraterna."* |
| **SD** | **in government or in opposition** — will not again support a government from outside; accepts Kristersson as PM; expects ministerial posts in proportion to its size | 2025-10-10 → 2026-04-01 → 2026-09-02 | SvD Kompakt, [L-C2], 2025-10-10, quoting Åkesson's social-media post: *"Efter nästa val kommer vi antingen att sitta i regering eller i full opposition. Något mellanläge, motsvarande det vi har idag, kommer inte att vara aktuellt för oss"*. TV4, [MSD-I4], 2026-04-01, Åkesson: *"Vi har varit tydliga ända sedan Tidöavtalet slöts att efter nästa val så är Sverigedemokraterna antingen ett regeringsparti eller ett oppositionsparti."* SVT, [MSD-I6], 2026-09-02: *"I Aftonbladets partiledarutfrågning under onsdagen säger Jimmie Åkesson att hans utgångspunkt är att antalet ministerposter vid en Tidövinst ska fördelas proportionerligt baserat på partiernas storlek. Utifrån dagens opinionsläge skulle det innebära mellan 12 och 14 SD-ministrar."* ⚠ SD primary: **GAP** (its 2026 manifesto, [SD-P1], is silent on the question; the social-media original was not fetched). |
| **KD** | a "blue-yellow" government of **one to four parties**; SD ministers **not a given**; will vote **no to Magdalena Andersson** as PM all the way to an extra election | 2026-04-01 (reported), 2026-06-05, 2026-09-02, 2026-09-08 | SVT live, [KD-I1], 2026-09-02, Busch: *"Vi kommer att vara beredda att rösta nej till Magdalena Andersson ända fram till ett nyval, säger Busch."* and *"Vi kommer att göra det."* Bulletin, [KD-I2], 2026-09-08, quoting Busch on X: *"”Vi har tydligt sagt att vi vill se en blågul regering. En blågul regering kan i teorin bestå både av 4, 3, 2 eller 1 parti”, skriver Busch på X."* and *"”För mig är det till exempel inte givet att SD ska ha några ministerposter alls”, skriver Busch."* Kvartal (citing DI), [KD-I3], 2026-06-05: *"Om alternativet står mellan en socialdemokratisk regering och ett extraval föredrar KD att låta väljarna gå till valurnorna igen."* Earlier, as reported: SR, [MSD-I2]: *"Kristdemokraterna har tidigare sagt att alla fyra partier kan vara i en regering."* SvD, [MSD-I3]: *"KD har sagt att ett nytt Tidösamarbete kan bestå av fyra partier."* ⚠ KD primary: **GAP**. On these sources 2022's KD ↔ SD cabinet line is **LIFTED**: KD's own words (Busch on X, as quoted by [KD-I2]) admit a four-party blue-yellow cabinet, SD included, and no fetched 2026 source has KD refusing SD in cabinet. SD in cabinet is **acceptable but not guaranteed** (*"inte givet att SD ska ha några ministerposter alls"*). The basis is secondary transmission of KD's own words. |
| **S** | goes to the election **as a single party**, aiming at an S-led government; willing to cooperate and negotiate with **every party except SD**; would not say with whom; a pure S minority government "a possibility" | 2026-04-09 → 2026-09-12 | Börsvärlden (citing DN), [S-I1], 2026-04-09: *"Jag går till val för att bilda en socialdemokratiskt ledd regering och vi går till val som ett enskilt parti, säger hon till Dagens Nyheter."* and *"Enligt Magdalena Andersson är Socialdemokraterna efter valet beredda att samarbeta och förhandla med alla partier utom Sverigedemokraterna."* SVT, [S-I2], 2026-08-27: *"Nu vill Magdalena Andersson bli Sveriges statsminister igen – men vägrar berätta med hjälp av vem."* Nyheter24 (citing SVT/TT), [S-I3], 2026-09-12: *"Det är en möjlighet, svarade Andersson, enligt SVT Nyheter."* Opinion column, [S-I4]: *"Socialdemokraterna kan tänka sig att efter valet samarbeta med alla partier utom Sverigedemokraterna, upprepar Magdalena Andersson."* ⚠ S primary: **GAP**. |
| **V** | **in or against**: if V's votes are needed, V must sit in the government, and will neither support nor let through a government it is not part of; seeks S and MP first, C if needed for a majority | congress **17–19 April 2026** (decided Saturday 18 April); first form 2025-09-30 | Vänsterpartiet, [V-P1]: *"Sverige behöver en rödgrön regering där Vänsterpartiet ingår efter valet 2026."*; *"Om våra röster behövs för att bilda regering så ska vi också ingå i den. Det betyder att vi inte kommer att stödja eller släppa fram en regering som vi inte ingår i."*; *"Vi söker därför samarbete med, i första hand, Socialdemokraterna och Miljöpartiet men även med Centerpartiet om det skulle behövas för att säkra en majoritet i riksdagen."* Congress dates, [V-P2]: *"Vänsterpartiet håller kongress i Örebro 17-19 april."* Sveriges Radio, [V-I1], 2026-04-18: *"Vänsterpartiet står fast vid kravet att sitta i en eventuell rödgrön regering efter valet. Beslutet klubbades på partiets kongress i Örebro på lördagen."* SVT, [C-I9], 2026-08-25: *"Partiets linje är att rösta nej till varje regeringskonstellation där V inte själva ingår – även en rödgrön regering."* ⚠ [V-P1] is headed "Preliminär version kongress 2026". |
| **MP** | expects to govern; **condition**: will vote no to a PM unless MP sits in the government (secondary only); will not sit in a government that builds new nuclear power | 2026-01-22, 2026-08-10, 2026-08-12 | Miljöpartiet, [MP-P1], 2026-08-12 (Amanda Lind): *"Daniel och jag ser fram emot att kavla upp ärmarna i regering om en månad."* Sveriges Radio headline, [MP-I1]: *"Daniel Helldéns krav: ”Vi ska sitta i nästa regering”"*. Bulletin (citing SR), [MP-I2]: *"Partiet ställer samtidigt ett villkor om att sitta i regering, uppger språkröret Daniel Helldén (MP) för Sveriges Radio."* SVT, [MP-I3], 2026-01-22: *"Vi kan inte sitta i en regering som bygger ny kärnkraft. Det är inte vår uppgift, säger Daniel Helldén."* ⚠ The **no-vote condition's own wording** is only in a paywalled secondary: **GAP** for a primary. |

## Against 2022 — what changes in the declared set

| 2022 declared line (`DeclaredRedLines.cs`) | 2026 status on these sources |
|---|---|
| **C ↔ SD**, support-blocking | **HOLDS** — restated 2025-11-13 → 2026-09-08, held after the election |
| **M ↔ SD**, cabinet-blocking, not support-blocking | **LIFTED** 2026-04-01 (claim 2) |
| **L ↔ SD**, cabinet-blocking, not support-blocking | **LIFTED** 2026-03-13 (claim 1) |
| **KD ↔ SD**, cabinet-blocking, not support-blocking | **LIFTED** as of 2026: SD in cabinet acceptable but not guaranteed — KD's own words, Busch on X as quoted by Bulletin 2026-09-08 [KD-I2] (*"En blågul regering kan i teorin bestå både av 4, 3, 2 eller 1 parti"*; *"inte givet att SD ska ha några ministerposter alls"*), and SR and SvD 2026-04-01 [MSD-I2], [MSD-I3]; secondary transmission, the KD primary a GAP |
| — | **NEW: C ↔ V** (claim 3): no cabinet that contains V, whether C sits in it, supports it or lets it through. Stronger than cabinet-blocking, weaker than support-blocking; a third shape neither symmetric strength holds, wired **one way** (`RedLine.OneWay`, a shape built for it by K-1 part (2), §603) |
| — | **NEW, and not a pair**: V, MP and SD each declare **in-or-against** (a party that will not support a government it is not part of); KD declares a refusal of **one named PM** (Andersson) |

## What is deliberately NOT here

- **V ↔ SD and MP ↔ SD are not listed as declarations.** No fetched source has either party declaring a
  refusal as a pair. V's platform names the parties it seeks (*"Socialdemokraterna och Miljöpartiet men även
  med Centerpartiet"*) and SD is not among them, but a party left off a list has not declared anything. As in
  2022, these are left to the DERIVED rule.
- **S ↔ SD is recorded as S's stated position** ("every party except SD", secondary sources only), not as a
  new declared line. 2022 left S ↔ SD to the derived rule, and so does the 2026 wiring (K-1 part (2), §603:
  `DeclaredRedLines.cs` writes only C ↔ SD and C → V); making it a declared line should be measured the way 2022
  measured its lines.
- **Events after election day are not declarations.** The Speaker's rounds, the exploratory mandate and the
  Riksdag's vote belong to item (4) of the ruling. The post-election sources above ([C-I6], [C-I7], [C-I8]) are
  used only to show that C's two lines **held** after the vote. They add no new line.

## GAPs — what could not be sourced, stated rather than filled

- **The M–SD agreement as a document.** No written text was found; the terms rest on the leaders' spoken
  words at the press conference, quoted by SR, TV4, SVT, SvD and Direkt. If a text exists, it was not
  located in this run.
- **The L party board's decision minute and official vote.** The 13–8 figure is SVT's anonymous sourcing;
  no L page fetched carries a count or a minute.
- **A formal C party-organ decision on the two lines.** Sourced as the leader's and party secretary's
  statements in the press and on the party's own pages; the V line's first fetched statement, [C-P1], is a
  Nerikes Allehanda article of 2026-01-28 reposted by a district on 2026-01-30, not a party statement, and
  [C-P4] is the party's summary of Annie Lööf's interview, not the leader's words. A district chair says *"Vi tagit beslut om att vi har två
  röda linjer."* (TV4, [C-I6]), but the decision itself was not found.
- **Primary statements for S, KD and SD**, and the wording of **MP's** no-vote condition — secondary only (see
  the table). KD's site was not tried (`ElectionsData/sweden/party_leaders_2022.md`, line 28, found it
  JavaScript-rendered); Busch's and Åkesson's
  posts on X were not fetched.
- **Paywalls:** DN 2026-06-09 [C-I2], Aftonbladet 2026-04-21 [C-I3], Altinget 2026-03-13 [L-I4] and Bulletin
  [MP-I2] are used for their free leads only.
- **The installation speech's original** on centerpartiet.se: a 404 was noted, but no URL was recorded and the
  Svenska tal page links none, so the 404 is **unverifiable**; the Svenska tal transcript [C-P5] is used and
  labelled as a transcript.
- **Leader compatibility and personal relationships** (§29) — still no source, still DEFERRED, as in 2022.

## Errors found in the lead, reported and not followed

The Swedish Wikipedia article [W] was used only to find sources. Four of its citations disagree with the
pages they cite; none changes a claim here, because every claim above rests on the page itself:

1. It dates SvD's *"Klart: M släpper in SD i regering vid valvinst"* **13 mars 2026**; SvD's own metadata
   says **2026-04-01T08:43:53Z** [MSD-I3]. The M–SD agreement is 1 April, as the ruling says.
2. It dates Aftonbladet's *"L säger nej till att släppa in SD i regering"* **10 oktober 2025**; Aftonbladet's
   metadata says **2025-11-21** [L-C1]. 10 October 2025 is instead the date of SvD Kompakt's report of
   Mohamsson's earlier besked [L-C2].
3. Its references for C swap two titles: *"”Vi kan inte sitta i en sådan regering”"* is **Aftonbladet's**
   headline of 2026-04-21 [C-I3]; *"Elisabeth Thand Ringqvist: Åkesson visar varför SD inte hör hemma i en
   svensk regering"* is **Centerpartiet's** page of 2026-08-11 [C-P2].
4. It gives "Liberalerna" as the publisher of the Sveriges Radio article on MP [MP-I1].

## What each declaration does — only the C ↔ V line and the vintage measured, once, by the wiring pass

The 2022 file measured every line by dropping it and re-running `CoalitionHarness`. **The sourcing pass measured
nothing**, and it touches nothing outside `ElectionsData/sweden/2026/`. The one measurement recorded here is the
wiring pass's (K-1 part (2), §603: `Formation2026Diagnostic`, log `k1_603_formation2`, in the FORMATION block); it
varies the C ↔ V line and the vintage, and does not drop the C ↔ SD line alone.
`Assets/Scripts/Elections/DeclaredRedLines.cs` now cites this file for the seated chamber and adds C ↔ SD
support-blocking and C → V one way; the 2022 file stays the source of the lines the backtests pin
(`ElectionVintage.Sweden2022`: C ↔ SD support-blocking; M, KD and L ↔ SD cabinet-blocking). Two findings the
wiring left standing, stated rather than fixed:

- **The declared mechanism is pairwise.** `RedLine(a, b, kind, blocksSupport, basis, oneWay)` in
  `DeclaredRedLines.cs` holds one pair, one strength and, since K-1 part (2), whether it runs both ways or one way (`oneWay`). The **in-or-against**
  demands of V (primary-sourced), MP and SD have no pair to sit on, and neither does KD's refusal of one named PM.
  C ↔ V was a third shape beside them: no cabinet that contains V, which is more than cabinet-blocking and less
  than support-blocking. It is now wired one way (`RedLine.OneWay`, a shape built for it by K-1 part (2), §603;
  see §(3)). The rest either need a new shape or have to be stated as out of scope. They should not be squeezed
  into pairs that no party declared.
- **L's consultation clause** (each minister subject to *avstämning* between that minister's party leader and
  the PM) has no representation either. It is a condition on who the ministers are, not on which parties sit.
