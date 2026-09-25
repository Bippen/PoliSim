# France — chambers, governments and party leaders of record by date, 2022-07-01 to 2026-09-24 [SOURCED] [PROVISIONAL]

Class: SOURCED (PS-1, the world clock — `docs/specs/POLITICAL_SYSTEM_SPEC.md` §9 stage 1 and §11; sourcing pass
2026-09-24, sourcing agent). `[PROVISIONAL]` until a second session re-verifies (R-K9), and for every row marked
DERIVED until a page stating the fact outright is fetched.

**What this file is for.** Three dated tables for France over the window 2022-07-01 → 2026-09-24: which Assemblée
nationale sat, which government was in office, and who led the parties behind the Assembly's groups — plus the
constitutional rules the spec's §8 and §11 name. It exists so the selector can seat France as of any start date
in the window (§3 "Every country is seated in its chamber of record and governed by its government of record").
It names no seat allocation and no model; `returns_2024.md` holds the 2024 returns.

**Fetched, never recalled.** Every quote below was read in a page fetched on 2026-09-24 (16:11–16:28 UTC, see
`raw/records/fetch_log.txt`) and saved byte for byte under `raw/records/`. Each file's SHA-256 is in
`raw/records/SHA256SUMS.txt` and in the register below. Quotes are French and verbatim. They were checked against the
saved bytes by a script (tags stripped, HTML entities decoded, the curly apostrophe U+2019 read as `'`, non-breaking
spaces and whitespace runs collapsed). Every block quote in this file was found in the saved file it cites. English
glosses are marked *gloss:* and are mine, not the source's. Lines marked **DERIVED** state their arithmetic or reading.

**What could not be fetched (and is therefore billed, never estimated).** `legifrance.gouv.fr`, `gouvernement.fr`,
`info.gouv.fr` and `interieur.gouv.fr` all answered every request with a Cloudflare challenge page (HTTP 403, "Just a
moment..."), with plain and browser-like headers, HTTP/1.1 and HTTP/2 alike; the attempts are in the fetch log and the
challenge bodies were deleted. So: no JORF decree text (appointments, dissolution, cessation of functions), no Code
électoral article text, and no `info.gouv.fr` government-composition page is cited here. The Constitution comes from the
Conseil constitutionnel's own consolidated text; the appointment and resignation dates come from the Élysée's
communiqués; the censure motions and confidence vote from the Assemblée nationale's own registers; the two-round
legislative rule from the Assemblée's own "fiche de synthèse". Wikipedia was used only to find URLs and is cited nowhere.

**Party keys.** `Assets/Scripts/Data/PartySystem.cs` seeds France by the Ministry's 2024 nuances (`RN`, `ENS`, `LR`,
`HOR`, `UDI`, `SOC`, `ECO`, `UXD`, `UG`, …). Table 3 maps each party to the nuance key that carried it in 2024 where one
exists, and says so where none does (MoDem rode `ENS`; LFI and PCF rode `UG`; the UDR rode `UXD`). That mapping is
**DERIVED** from `returns_2024.md`'s nuance grid and is not a claim about the parties' own labels.

## Source register
(all accessed 2026-09-24; saved as `raw/records/<file>`; "page's own date" is the date the page itself shows, or what
it shows instead. The table is emitted by a script from `SHA256SUMS.txt` and the file sizes.)

| id | URL | publisher | page's own date | file | bytes | SHA-256 |
|---|---|---|---|---|---|---|
| [CC-CONST] | https://www.conseil-constitutionnel.fr/le-bloc-de-constitutionnalite/texte-integral-de-la-constitution-du-4-octobre-1958-en-vigueur | Conseil constitutionnel | consolidated text "en vigueur" as served; no page date shown | `cc_constitution_texte_integral.html` | 138061 | `a2eb77e550748359b4337053b0551595c540403ef268d647b6dc4e567bb986a4` |
| [CC-197] | https://www.conseil-constitutionnel.fr/decision/2022/2022197PDR.htm | Conseil constitutionnel | "Décision n° 2022-197 PDR du 27 avril 2022"; "Rendu public le 27 avril 2022" | `cc_2022-197PDR_proclamation.html` | 80725 | `c0e7bcda1e266d2d4a8a4cd37ac4b894a85a7d774ec53d9136020d6fc70998b3` |
| [AN-S28] | https://www.assemblee-nationale.fr/dyn/16/comptes-rendus/seance/session-ordinaire-de-2021-2022/seance-du-mardi-28-juin-2022 | Assemblée nationale (compte rendu) | "Séance du mardi 28 juin 2022" | `an_seance_20220628.html` | 182695 | `ccfa28984f6ec2dce35a4037a434a3e5f1224869f909e05f35bb1a46881d49b2` |
| [AN-CAL16] | https://www.assemblee-nationale.fr/dyn/actualites-accueil-hub/calendrier-d-ouverture-de-la-xvie-legislature | Assemblée nationale (actualité) | no page date shown; text dated "mardi 28 juin 2022" | `an_calendrier-ouverture-xvie.html` | 76245 | `bc389c497f43b2ad9b8ac019abc8d3cf3ef70c966c61ba3a3abe8883c5bb6427` |
| [AN-GRP22] | https://www.assemblee-nationale.fr/dyn/actualites-accueil-hub/constitution-des-groupes-politiques-a-l-assemblee-nationale | Assemblée nationale (actualité) | no page date shown; text "Mardi 28 juin"; group names carry "Nupes" (2022 vintage, DERIVED) | `an_constitution-des-groupes.html` | 70227 | `bc610a21aa5898a54f6275af3c84e54c21fec02027ce9bf1eb3611ef701d200d` |
| [AN-CAL17] | https://www.assemblee-nationale.fr/dyn/actualites-accueil-hub/calendrier-d-ouverture-de-la-17e-legislature | Assemblée nationale (actualité) | no page date shown; text dated "Jeudi 18 juillet 2024" | `an_calendrier-ouverture-17e.html` | 74223 | `39e7559165441a6135fdd143d771455f6833dfb6ccc1738855a0eeb33215ad83` |
| [AN-S18] | https://www.assemblee-nationale.fr/dyn/17/comptes-rendus/seance/session-de-droit-de-2024/seance-du-jeudi-18-juillet-2024 | Assemblée nationale (compte rendu) | "Séance du jeudi 18 juillet 2024" | `an_seance_20240718.html` | 207934 | `ebe4f841662d09c1fae2111a0e0f0a1bdd2c391352b45e8f709ec06cb4ccb618` |
| [AN-EFF] | https://www2.assemblee-nationale.fr/instances/liste/groupes_politiques/effectif | Assemblée nationale | live table as served 2026-09-24; no page date shown | `an_effectif-groupes-current.html` | 206300 | `49a1416decedc9aa08f6aa670656c796a71813e14066bb5b1e2fdc2c6b1e8d12` |
| [AN-MC17] | https://www.assemblee-nationale.fr/dyn/engagements_responsabilite-motions_censures/motions-de-censure | Assemblée nationale | live list as served 2026-09-24; newest entry "lundi 23 février 2026" | `an_motions-de-censure-17.html` | 74632 | `a94b29d5e0c97cfb243f00ddaec798488bd84bec973112c0818a89864b6c7f9a` |
| [AN-MC58] | https://www.assemblee-nationale.fr/dyn/engagements_responsabilite-motions_censures/motions-de-censure-depuis-1958 | Assemblée nationale | "Mise à jour : 15/01/2026" (yet carries rows voted 26.02.2026) | `an_motions-de-censure-depuis-1958.html` | 92474 | `c1643948a354988ae8fcce0e8a0c767f40828c91a52c7ca408b80840555424b2` |
| [AN-ADO] | https://www.assemblee-nationale.fr/dyn/actualites-accueil-hub/adoption-d-une-motion-de-censure-le-plfss-2025-est-rejete-en-lecture-cmp-art.-49.3-et-le-premier-ministre-a-presente-la-demission-du-gouvernement-art.-50 | Assemblée nationale (actualité) | no page date shown; text "Mercredi 4 décembre 2024" | `an_adoption-censure-20241204.html` | 80360 | `f0ec989147a9bdb46b0d6479a1b7b346ad07b7204cb131f937d348c583b4aedc` |
| [AN-SCR519] | https://www.assemblee-nationale.fr/dyn/17/scrutins/519 | Assemblée nationale (scrutin public) | "Unique séance du mercredi 4 décembre 2024" | `an_scrutin_519_censure-20241204.html` | 226300 | `0677dea44fe54dcb807230b20490d34270f06ccfb8595ea3b05e9e9d95585e1c` |
| [AN-CONF] | https://www.assemblee-nationale.fr/dyn/actualites-accueil-hub/vote-de-confiance-l-assemblee-nationale-a-desapprouve-la-declaration-de-politique-generale-du-gouvernement | Assemblée nationale (actualité) | no page date shown; text "Lundi 8 septembre 2025" | `an_vote-de-confiance-20250908.html` | 77297 | `253f6e83f0709081824859f183338f781397b5cdb1f2fe133187a1aebd839438` |
| [AN-REJ] | https://www.assemblee-nationale.fr/dyn/actualites-accueil-hub/rejet-de-deux-motions-de-censure-deposees-en-application-de-l-article-49-alinea-2-de-la-constitution2 | Assemblée nationale (actualité) | no page date shown; text "Jeudi 16 octobre 2025" | `an_rejet-censures-20251016.html` | 77382 | `a5acd204a553581e25dd18f00ee3b40af6917fbca85203201e304447823b948a` |
| [AN-FICHE] | https://www2.assemblee-nationale.fr/decouvrir-l-assemblee/role-et-pouvoirs-de-l-assemblee-nationale/le-depute/l-election-des-deputes | Assemblée nationale ("Fiche de synthèse n°3") | no page date shown | `an_fiche_election_deputes.html` | 95321 | `acdc997004225720c5bae574e514a8f406235076174470f674125c5ff4117d59` |
| [EL-B16] | https://www.elysee.fr/emmanuel-macron/2022/05/16/le-president-de-la-republique-a-nomme-mme-elisabeth-borne-premiere-ministre-et-la-chargee-de-former-un-gouvernement | Élysée | "Publié le 16 mai 2022" | `elysee_20220516_nomination-borne.html` | 188521 | `d750bc5b415ea2372bc52049e04bf145847851c4c2da1a213da67102c82c47c3` |
| [EL-B20] | https://www.elysee.fr/emmanuel-macron/2022/05/20/annonce-de-la-composition-du-gouvernement-1 | Élysée | "Publié le 20 mai 2022" | `elysee_20220520_composition-gouvernement-borne.html` | 192825 | `f9350ed519541e694cdf5fea43d2ae3f7c2aebeb7c48c2c0033925e24e167a16` |
| [EL-B24] | https://www.elysee.fr/emmanuel-macron/2024/01/08/mme-elisabeth-borne-a-remis-ce-jour-la-demission-du-gouvernement-au-president-de-la-republique-qui-la-acceptee | Élysée | "Publié le 8 janvier 2024" | `elysee_20240108_demission-borne.html` | 189392 | `28d1ffd0d9d4b3b07422223954d6acd536fb4e8b1693edcac3babe2091168c0b` |
| [EL-A09] | https://www.elysee.fr/emmanuel-macron/2024/01/09/le-president-de-la-republique-a-nomme-m-gabriel-attal-premier-ministre-et-la-charge-de-former-un-gouvernement | Élysée | "Publié le 9 janvier 2024" | `elysee_20240109_nomination-attal.html` | 190030 | `eaaf68f61e842b156de9a68b3f561da06cdf339d32d535ab84b79d4eba87a9a8` |
| [EL-A11] | https://www.elysee.fr/emmanuel-macron/2024/01/11/nomination-du-gouvernement-3 | Élysée | "Publié le 11 janvier 2024" | `elysee_20240111_nomination-gouvernement-attal.html` | 191236 | `5747cdc62c8b45cf9906c0c080e47371a80c0e51dac03f69dcd37c2ee8ee00f0` |
| [EL-DIS] | https://www.elysee.fr/emmanuel-macron/2024/06/09/adresse-aux-francais-4 | Élysée | "Publié le 9 juin 2024" | `elysee_20240609_adresse-aux-francais.html` | 202704 | `0cc371c420ac790d2e854f9571e09c1a6977e57426e77d594e953b110582b8da` |
| [EL-A16] | https://www.elysee.fr/emmanuel-macron/2024/07/16/le-president-de-la-republique-a-accepte-ce-jour-la-demission-du-gouvernement-de-m-gabriel-attal | Élysée | "Publié le 16 juillet 2024" | `elysee_20240716_demission-attal.html` | 189527 | `37e8129d9c30d95688ff6dde59b67dde26d0052f275b172663eeec262479a84d` |
| [EL-BA05] | https://www.elysee.fr/emmanuel-macron/2024/09/05/le-president-de-la-republique-a-nomme-monsieur-michel-barnier-premier-ministre | Élysée | "Publié le 5 septembre 2024" | `elysee_20240905_nomination-barnier.html` | 189863 | `63185bf1cb4ab80281717b84c38d708ddee5dbda38dc0259f2e537aaa9913fc1` |
| [EL-BA21] | https://www.elysee.fr/emmanuel-macron/2024/09/21/annonce-de-la-nomination-du-gouvernement | Élysée | "Publié le 21 septembre 2024" | `elysee_20240921_nomination-gouvernement-barnier.html` | 194291 | `bbc02828be5402dd8664cbc7dedb3271a8ddde9f1c7ba13f6f08fff6680bddb6` |
| [EL-BA-ADR] | https://www.elysee.fr/emmanuel-macron/2024/12/05/adresse-aux-francais-5 | Élysée | "Publié le 5 décembre 2024" | `elysee_20241205_adresse-aux-francais.html` | 214952 | `c7947293f86b393f5bce2f45d03006778608f47c00aa4341f65edf32c5f4fdb0` |
| [EL-BA-DEM] | https://www.elysee.fr/emmanuel-macron/2024/12/05/le-premier-ministre-a-remis-ce-jour-la-demission-de-son-gouvernement-au-president-de-la-republique-qui-en-a-pris-acte | Élysée | "Publié le 5 décembre 2024" | `elysee_20241205_demission-barnier.html` | 189408 | `78b0d644cc809ac7f4d90caf66740ab5a1a6bd4994efc08b3711d735f0971aa1` |
| [EL-BY13] | https://www.elysee.fr/emmanuel-macron/2024/12/13/le-president-de-la-republique-a-nomme-m-francois-bayrou-premier-ministre-et-la-charge-de-former-un-gouvernement | Élysée | "Publié le 13 décembre 2024" | `elysee_20241213_nomination-bayrou.html` | 189278 | `68ae87887575a41c6420259665d42bc8c5750061c3d7a8d97fee8bb7a63143e0` |
| [EL-BY23] | https://www.elysee.fr/emmanuel-macron/2024/12/23/annonce-de-la-nomination-du-nouveau-gouvernement | Élysée | "Publié le 23 décembre 2024" | `elysee_20241223_nomination-gouvernement-bayrou.html` | 193868 | `7209fadeab3adc436727c4efe446ff796f364f2b16b9a32f93d895fca6857c36` |
| [EL-L09] | https://www.elysee.fr/emmanuel-macron/2025/09/09/le-president-de-la-republique-a-nomme-sebastien-lecornu-premier-ministre | Élysée | "Publié le 9 septembre 2025" | `elysee_20250909_nomination-lecornu.html` | 189614 | `aea94d767a3223b1b062b6f7b19a7ecde33c9e99a777cd91d4a85f0c3be4a910` |
| [EL-L05] | https://www.elysee.fr/emmanuel-macron/2025/10/05/annonce-de-la-nomination-du-gouvernement-le-5-octobre-2025 | Élysée | "Publié le 5 octobre 2025" | `elysee_20251005_nomination-gouvernement-lecornu1.html` | 191236 | `2a6e160fa9593428682fad61cb78d4155e2b2504fae607e383723a3ddb7b9550` |
| [EL-L06] | https://www.elysee.fr/emmanuel-macron/2025/10/06/le-premier-ministre-a-remis-la-demission-de-son-gouvernement-au-president-de-la-republique-qui-la-acceptee | Élysée | "Publié le 6 octobre 2025" | `elysee_20251006_demission-lecornu.html` | 189221 | `fde699a2f4ba7d5ec9e4e5194a31b2e7644dab28dc23c641644fa16dc0219117` |
| [EL-L10] | https://www.elysee.fr/emmanuel-macron/2025/10/10/nomination-de-sebastien-lecornu-premier-ministre | Élysée | "Publié le 10 octobre 2025" | `elysee_20251010_nomination-lecornu.html` | 188545 | `76f931a39bd14206288f06e5a22c1112f1695b7164eba1825f4f524ff85976c6` |
| [EL-L12] | https://www.elysee.fr/emmanuel-macron/2025/10/12/nomination-du-gouvernement-6 | Élysée | "Publié le 12 octobre 2025" | `elysee_20251012_nomination-gouvernement-lecornu2.html` | 193484 | `b673e353f53b141102b8c003e0938afdde0178ed95b255920040259a36a4fcb2` |
| [EL-L26F] | https://www.elysee.fr/emmanuel-macron/2026/02/26/nomination-du-gouvernement-7 | Élysée | "Publié le 26 février 2026" | `elysee_20260226_nomination-gouvernement.html` | 194628 | `dfcf6c659e1170278323e0330371f9aee3f260d3d8d0ba3761644eefecbba8e1` |
| [EL-CM10] | https://www.elysee.fr/emmanuel-macron/2026/09/10/compte-rendu-du-conseil-des-ministres-du-10-septembre-2026 | Élysée | "Publié le 10 septembre 2026" | `elysee_20260910_conseil-des-ministres.html` | 216408 | `be9eeb364fe06f87f497dbbcae49ffc3d70881438163734597e9d3aa18418074` |
| [EL-ACT] | https://www.elysee.fr/toutes-les-actualites | Élysée | live listing as served 2026-09-24 | `elysee_toutes-les-actualites.html` | 536863 | `1ee322be4dd6723440d4937cdc9d5c79585f5b6e10dfaca55be4b5a4458a1e67` |
| [P-RN] | https://rassemblementnational.fr/ | Rassemblement National (party site, front page) | live page as served 2026-09-24 | `rn_site.html` | 274215 | `9c62a012f595c9ece74d6274ab16e0ff642d56d861c924763da36aad6a1acf48` |
| [P-REN] | https://parti-renaissance.fr/le-parti | Renaissance (party site) | live page; text dated "Lors du Conseil national du 8 décembre 2024" | `renaissance_le-parti.html` | 112101 | `1bd8f360d9d6aca62ca9c3a433b087810928c21a9dd8a870b177c95bb1a20e60` |
| [P-LR] | https://republicains.fr/qui-sommes-nous/ | Les Républicains (party site) | live page as served 2026-09-24 | `lr_qui-sommes-nous.html` | 1371616 | `d983b0848014d2376183f7faf835458bbec33665e430f78e03958198350e2fce` |
| [P-MODEM] | https://www.mouvementdemocrate.fr/fiche/francois-bayrou-2366 | Mouvement Démocrate (party site) | live page as served 2026-09-24 | `modem_fiche-bayrou.html` | 78018 | `1cdeaf6716fcf1112b4839d363f8eb79c2f17ea59af2356f9c04155077694051` |
| [P-HOR] | https://horizonsleparti.fr/ | Horizons (party site, front page) | live page; newest item dated 2026 | `horizons_site.html` | 135378 | `5e04909b3420f0910a9c780cc352022fe77ae4b3cc86840aeba823516b9e83f6` |
| [P-UDI] | https://www.parti-udi.fr/ | UDI (party site, front page) | live page as served 2026-09-24 | `udi_site.html` | 290887 | `1cc0e7405807e33b5e595dbc65fe78eec4857fa6f58ec82953c43a078b857f57` |
| [P-PS] | https://www.parti-socialiste.fr/ | Parti socialiste (party site, front page) | live page; newest item dated "23 septembre" | `ps_site.html` | 968197 | `e3d9cbe816adee3c106dd56694302e5ac015aadb614a52f864ba88d2abc924a1` |
| [P-ECO-H] | https://lesecologistes.fr/ | Les Écologistes (party site, front page) | live page; items dated "18 sept. 2026" | `lesecologistes_site.html` | 106915 | `dc123a538f7c87f64fee61c7580c73ee645c06eb8971798f05d3f69f8dbd48f2` |
| [P-ECO-T] | https://lesecologistes.fr/trombinoscope | Les Écologistes (party site) | live page as served 2026-09-24 | `lesecologistes_trombinoscope.html` | 667296 | `b9dd30eb4c50f06647afcbceed708f651ff84ff08ffb6f4d9b110547585f5b27` |
| [P-LFI] | https://lafranceinsoumise.fr/ | La France insoumise (party site, front page) | live page as served 2026-09-24 | `lfi_site.html` | 534533 | `ef1b90a25454ff579c5d485e93a559c901ee0e07fed50bfb6efc05a1534975d5` |
| [P-PCF] | https://www.pcf.fr/ | Parti communiste français (party site, front page) | live page; embedded post dated "September 24, 2026" | `pcf_site.html` | 98313 | `5402d4d6b734e7427bf837603c737f2196f5abce6e91773cbc5ee1e9135361c7` |


## Table 1 — THE CHAMBER OF RECORD BY DATE

| from | to | chamber | how the boundary is dated | source ids |
|---|---|---|---|---|
| 2022-07-01 (window start; the chamber opened 2022-06-28) | 2024-06-09 | **XVIe législature** of the Assemblée nationale, 577 seats | opened at the sitting of Tuesday 28 June 2022, presided by the doyen d'âge; the President of the Assembly elected the same day (Yaël Braun-Pivet, 242 votes at the second ballot). Its election dates are **not on a fetched page** (GAP G1) | [AN-S28] [AN-CAL16] |
| 2024-06-09, evening | 2024-07-18 | **no Assembly**: the XVIe dissolved by the President; elections called for 30 June and 7 July 2024 | the President's televised address of 9 June 2024 announces the dissolution and the two polling days; the decree itself (JORF) is not fetched (GAP G2) | [EL-DIS]; rule: Art. 12 [CC-CONST] |
| 2024-07-18 | 2026-09-24 (window end; still sitting) | **XVIIe législature**, 577 seats | opened at the sitting of Thursday 18 July 2024 as the "session de droit prévue par l'article 12 de la Constitution"; the President of the Assembly elected the same day (Yaël Braun-Pivet, 220 votes at the third ballot). **DERIVED**: 18 July 2024 is the second Thursday after the second round of 7 July, as Art. 12 requires. Seats by nuance: `returns_2024.md`. Groups as served 2026-09-24: below | [AN-S18] [AN-CAL17] [AN-EFF]; rule: Art. 12 [CC-CONST] |

**The XVIe législature's opening** [AN-S28], "Compte rendu de la séance du mardi 28 juin 2022", "Présidence de M. José
Gonzalez" (doyen d'âge), item "1. Ouverture de la XVIe législature":
> "Je déclare ouverte la XVI e législature de l'Assemblée nationale."

Second ballot for the presidency of the Assembly, same sitting:
> "Mme Yaël Braun-Pivet : 242 voix"

[AN-CAL16], the Assembly's opening calendar:
> "L'Assemblée nationale reprend ses travaux à partir du mardi 28 juin 2022:"

**The XVIe's composition by group at its opening** [AN-GRP22] (the Assembly's notice "Constitution des groupes politiques
à l'Assemblée nationale"; the page shows no date of its own — **DERIVED**: it is the 2022 notice, because its text
says "Mardi 28 juin" and its group names carry "Nupes", the 2022 alliance). The notice:
> "Mardi 28 juin : remise au Secrétariat général de la Présidence des déclarations politiques des groupes, signées de leurs membres, accompagnées de la liste de ces membres et des députés apparentés et du nom du président du groupe ; cette déclaration peut mentionner l'appartenance du groupe à l'opposition."

Its table "Effectif des groupes :" (the group name and its figure are consecutive lines in the source; the reading of
name → figure is **DERIVED** from that layout):

| group as named on the page | effectif | party behind it (DERIVED, see Table 3) | 2024 nuance key |
|---|---|---|---|
| "Renaissance" | 172 | Renaissance | ENS |
| "Rassemblement National" | 89 | Rassemblement National | RN |
| "La France insoumise – Nouvelle Union Populaire écologique et sociale" | 75 | La France insoumise | (UG in 2024) |
| "Les Républicains" | 62 | Les Républicains | LR |
| "Démocrate (MoDem et Indépendants)" | 48 | Mouvement Démocrate | (ENS in 2024) |
| "Socialistes et apparentés (membre de l'intergroupe Nupes)" | 31 | Parti socialiste | SOC |
| "Horizons et apparentés" | 30 | Horizons | HOR |
| "Écologiste – Nupes" | 23 | Les Écologistes | ECO |
| "Gauche démocrate et républicaine – Nupes" | 22 | Parti communiste français and others | (UG in 2024) |
| "Libertés, Indépendants, Outre-mer et Territoires" | 16 | no single party | — |
| "TOTAL" | 568 | | |

**DERIVED**: 577 − 568 = 9 deputies outside any group at the opening. The 2022 dates on which the groups were declared
(28 June) and published (29 June) are given by [AN-CAL16] as the calendar's own items: "avant 18 heures : remise au
Secrétariat général de la Présidence (direction de la Séance) des déclarations politiques des groupes" and
"publication au Journal officiel (lois et décrets) des déclarations politiques et de la composition des groupes".

**The dissolution** [EL-DIS], "Adresse aux Français", "Publié le 9 juin 2024":
> "C'est pourquoi, après avoir procédé aux consultations prévues à l'article 12 de notre Constitution, j'ai décidé de vous redonner le choix de notre avenir parlementaire par le vote. Je dissous donc ce soir l'Assemblée nationale. Je signerai dans quelques instants le décret de convocation des élections législatives qui se tiendront le 30 juin pour le premier tour et le 7 juillet pour le second."

The page's own summary:
> "Les prochaines élections législatives auront lieu le 30 juin pour le premier tour et le 7 juillet pour le second."

*gloss:* the trigger, in the spec's §4 sense, is the evening of 2024-06-09. **DERIVED**: 30 June is 21 days and 7 July
28 days after 9 June, inside Art. 12's "vingt jours au moins et quarante jours au plus après la dissolution".

**The XVIIe législature's opening** [AN-S18], "Compte rendu de la séance du jeudi 18 juillet 2024", "Session de droit de
2024", "Présidence de M. José Gonzalez, doyen d'âge", item "1. Ouverture de la XVIIe législature":
> "Je déclare ouverte la XVII e législature de l'Assemblée nationale et la session de droit prévue par l'article 12 de la Constitution."

Third ballot for the presidency of the Assembly, same sitting:
> "Mme Yaël Braun-Pivet : 220 voix"

[AN-CAL17] dates the group declarations "Jeudi 18 juillet 2024" and their publication "Vendredi 19 juillet 2024":
> "Publication au Journal officiel (lois et décrets) des déclarations politiques et de la composition des groupes."

**The XVIIe's groups as served on 2026-09-24** [AN-EFF] ("Tableau des effectifs des groupes politiques"; columns
"Groupe", "Membres", "Membres apparentés", "Total"). This is the live table on the access date, **not** the opening
composition of July 2024 (GAP G3):

| group | members | apparentés | total | party behind it (DERIVED) | 2024 nuance key |
|---|---|---|---|---|---|
| Rassemblement National | 119 | 3 | 122 | Rassemblement National | RN |
| Ensemble pour la République | 77 | 13 | 90 | Renaissance | ENS |
| La France insoumise - Nouveau Front Populaire | 69 | 2 | 71 | La France insoumise | UG |
| Socialistes et apparentés | 59 | 8 | 67 | Parti socialiste | UG / SOC |
| Droite Républicaine | 41 | 7 | 48 | Les Républicains | LR |
| Écologiste et Social | 38 | 0 | 38 | Les Écologistes | UG / ECO |
| Les Démocrates | 35 | 2 | 37 | Mouvement Démocrate | ENS |
| Horizons & Indépendants | 30 | 6 | 36 | Horizons | HOR |
| Libertés, Indépendants, Outre-mer et Territoires | 23 | 0 | 23 | no single party | — |
| Gauche Démocrate et Républicaine | 17 | 0 | 17 | Parti communiste français and others | UG |
| Union des droites pour la République | 17 | 0 | 17 | UDR | UXD |
| "Total groupe" | 525 | 41 | | | |

**DERIVED**: 525 + 41 = 566 in groups; 577 − 566 = 11 outside any group on the access date.

## Table 2 — THE GOVERNMENT OF RECORD BY DATE

**The President of record throughout the window: Emmanuel Macron**, proclaimed re-elected by the Conseil
constitutionnel on 27 April 2022 with effect from 14 May 2022, 0 h [CC-197]; a five-year term (Art. 6), so
**DERIVED**: the term runs to May 2027 and the next presidential election falls "vingt jours au moins et trente-cinq
jours au plus avant l'expiration des pouvoirs du président en exercice" (Art. 7).

[CC-197], "Décision n° 2022-197 PDR du 27 avril 2022":
> "22. Les résultats du second tour pour l'élection du Président de la République, auquel il a été procédé les 23 et 24 avril 2022, sont les suivants :"
>
> "Suffrages exprimés : 32 057 325"
>
> "M. Emmanuel MACRON : 18 768 639"
>
> "Mme Marine LE PEN : 13 288 686"
>
> "Ainsi, M. Emmanuel MACRON a recueilli la majorité absolue des suffrages exprimés requise pour être proclamé élu."
>
> "M. Emmanuel MACRON Président de la République française à compter du 14 mai 2022 à 0 heure."
>
> "Rendu public le 27 avril 2022."

**The governments.** Every appointment and resignation below is the Élysée's own communiqué of that day; the JORF decree
is not fetched (GAP G2). The parties of each government are **not stated on any fetched official page** (GAP G4); the
only party facts on record are the PM's own party office where a party page states it (Table 3).

| from | to | prime minister | PM's party (only where sourced) | the boundary event, dated | source ids |
|---|---|---|---|---|---|
| 2022-05-16 (before the window) | 2024-01-08 | Élisabeth Borne | GAP | appointed 16 May 2022; ministers' decree 20 May 2022; she handed in the Government's resignation on 8 January 2024, accepted the same day, and handled current business until the new Government was named | [EL-B16] [EL-B20] [EL-B24] |
| 2024-01-09 | 2024-07-16 | Gabriel Attal | Renaissance (secretary general on the party's page as of 8 Dec 2024, [P-REN]; his party on 9 Jan 2024 is not on a fetched page) | appointed 9 January 2024; ministers' decree 11 January 2024; resignation accepted 16 July 2024; current business until the next Government (**DERIVED**: until 5 September 2024 at the earliest, when the next PM was appointed) | [EL-A09] [EL-A11] [EL-A16] |
| 2024-09-05 | 2024-12-05 | Michel Barnier | GAP | appointed 5 September 2024; ministers' decree 21 September 2024; **censured** 4 December 2024 (Art. 49 al. 3 motion adopted, 331 votes for); resignation handed in 5 December 2024, "pris acte"; current business until the next Government | [EL-BA05] [EL-BA21] [AN-ADO] [AN-SCR519] [EL-BA-DEM] [EL-BA-ADR] |
| 2024-12-13 | 2025-09-09 (**DERIVED**) | François Bayrou | Mouvement Démocrate ("Président du Mouvement Démocrate", [P-MODEM]) | appointed 13 December 2024; ministers' decree 23 December 2024; **confidence refused** 8 September 2025 (Art. 49 al. 1: 194 for, 364 against); no Élysée page on his resignation was found (GAP G5) — **DERIVED**: his Government ended with the next PM's appointment on 9 September 2025 (Art. 8, first paragraph) | [EL-BY13] [EL-BY23] [AN-CONF] [EL-L09] |
| 2025-09-09 | 2025-10-06 | Sébastien Lecornu (first) | GAP | appointed 9 September 2025; ministers' decree 5 October 2025; resignation accepted 6 October 2025 | [EL-L09] [EL-L05] [EL-L06] |
| 2025-10-10 | 2026-09-24 (window end; **DERIVED** still in office, see below) | Sébastien Lecornu (second) | GAP | re-appointed 10 October 2025; ministers' decree 12 October 2025; two Art. 49 al. 2 motions rejected 16 October 2025 (144 and 271 for, 289 required); further motions rejected 14 January 2026 and 26 February 2026; a new ministers' decree 26 February 2026 | [EL-L10] [EL-L12] [AN-REJ] [AN-MC58] [EL-L26F] |

**The appointments and resignations, verbatim** (each is the communiqué's headline or body; "Publié le" is the page's
own date):

- [EL-B16], "Publié le 16 mai 2022":
  > "Le Président de la République a nommé Mme Elisabeth BORNE, Première ministre et l'a chargée de former un Gouvernement."
- [EL-B20], "Publié le 20 mai 2022" ("Annonce de la composition du Gouvernement").
- [EL-B24], "Publié le 8 janvier 2024":
  > "Mme Elisabeth Borne a remis ce jour la démission du Gouvernement au Président de la République, qui l'a acceptée. Elle assure, avec les membres du Gouvernement, le traitement des affaires courantes jusqu'à la nomination du nouveau Gouvernement."
- [EL-A09], "Publié le 9 janvier 2024":
  > "Le Président de la République a nommé M. Gabriel Attal Premier ministre, et l'a chargé de former un Gouvernement."
- [EL-A11], "Publié le 11 janvier 2024":
  > "La composition du Gouvernement résultant du décret signé ce jour sur la proposition du Premier ministre"
- [EL-A16], "Publié le 16 juillet 2024":
  > "M. Gabriel ATTAL a remis la démission du Gouvernement au Président de la République, qui l'a acceptée ce jour. Il assure, avec les membres du Gouvernement, le traitement des affaires courantes jusqu'à la nomination d'un nouveau Gouvernement."
- [EL-BA05], "Publié le 5 septembre 2024":
  > "Le Président de la République a nommé Monsieur Michel Barnier Premier ministre. Il l'a chargé de constituer un gouvernement de rassemblement au service du pays et des Français."
- [EL-BA21], "Publié le 21 septembre 2024":
  > "La composition du Gouvernement résultant du décret signé ce jour sur la proposition du Premier ministre"
- [EL-BA-DEM], "Publié le 5 décembre 2024":
  > "Le Premier ministre a remis ce jour la démission de son Gouvernement au Président de la République qui en a pris acte."
  >
  > "Michel Barnier assure, avec les membres du Gouvernement, le traitement des affaires courantes jusqu'à la nomination d'un nouveau Gouvernement."
- [EL-BA-ADR], the President's address, "Publié le 5 décembre 2024":
  > "Hier l'Assemblée nationale a voté à la majorité absolue le rejet du budget de la sécurité sociale et ce faisant la censure du gouvernement de Michel Barnier. Aujourd'hui, le Premier ministre m'a remis sa démission et celle de son gouvernement et j'en ai pris acte."
- [EL-BY13], "Publié le 13 décembre 2024":
  > "Le Président de la République a nommé M. François Bayrou Premier ministre, et l'a chargé de former un Gouvernement."
- [EL-BY23], "Publié le 23 décembre 2024":
  > "La composition du Gouvernement résultant du décret signé ce jour sur la proposition du Premier ministre"
- [EL-L09], "Publié le 9 septembre 2025":
  > "Le Président de la République a nommé monsieur Sébastien Lecornu Premier ministre."
  >
  > "A la suite de ces discussions, il appartiendra au nouveau Premier ministre de proposer un Gouvernement au Président de la République."
- [EL-L05], "Publié le 5 octobre 2025":
  > "La composition du Gouvernement résultant du décret signé ce jour sur la proposition du Premier ministre"
- [EL-L06], "Publié le 6 octobre 2025":
  > "Monsieur Sébastien Lecornu a remis la démission de son Gouvernement au Président de la République, qui l'a acceptée."
- [EL-L10], "Publié le 10 octobre 2025":
  > "Le Président de la République a nommé Sébastien Lecornu Premier ministre, et l'a chargé de former un Gouvernement."
- [EL-L12], "Publié le 12 octobre 2025":
  > "La composition du Gouvernement résultant du décret signé ce jour sur la proposition du Premier ministre"
- [EL-L26F], "Publié le 26 février 2026":
  > "La composition du Gouvernement résultant du décret signé ce jour sur la proposition du Premier ministre est la suivante :"

**The censure of 4 December 2024** [AN-ADO] and [AN-SCR519]:
> "Mercredi 4 décembre 2024, l'Assemblée nationale a examiné les deux motions de censure, et adopté l'une d'entre elles"
>
> "La motion de censure déposée par Mme Mathilde PANOT, M. Boris VALLAUD, Mme Cyrielle CHATELAIN, M. André CHASSAIGNE et 181 de leurs collègues en application de l'article 49, alinéa 3, de la Constitution, ayant été adoptée , la motion de censure déposée par Mme Marine LE PEN, M. Éric CIOTTI et 138 de leurs collègues en application de l'article 49, alinéa 3, de la Constitution , n'a pas été mise aux voix"

The scrutin, "Analyse du scrutin n°519", "Unique séance du mercredi 4 décembre 2024":
> "Scrutin public n°519 sur la motion de censure déposée en application de l'article 49, alinéa 3, de la Constitution par Mme Mathilde Panot, M. Boris Vallaud, Mme Cyrielle Chatelain, M. André Chassaigne et 181 membres de l'Assemblée."
>
> "Pour l'adoption : 331"
>
> "L'Assemblée nationale a adopté"

**The confidence vote of 8 September 2025** [AN-CONF]:
> "Lundi 8 septembre 2025, l'Assemblée nationale n'a pas approuvé la déclaration de politique générale du Gouvernement ( article 49 alinéa 1 de la Constitution )."
>
> "Nombre de votants : 573 | Suffrages exprimés : 558 | Pour l'approbation : 194 | Contre l'approbation : 364"
>
> "En application de l' article 50 de la Constitution , le Premier ministre doit remettre au Président de la République la démission du Gouvernement."

**The censure motions of the window, Art. 49 al. 2** [AN-MC58] (the Assembly's register "Motions de censure depuis
1958", "Mise à jour : 15/01/2026" as printed, yet carrying rows voted 26.02.2026). The register's columns are
"Dépôt", "Auteurs", "Objet", "Date du vote", "Majorité requise", "Pour"; the reading of each row into those columns is
**DERIVED** from the page's layout. None was adopted (every "Pour" is below the majority required):

| deposited | authors (as printed) | object (as printed) | vote | majority required | for |
|---|---|---|---|---|---|
| 06.07.2022 | "Mme Mathilde Panot (LFI-NUPES), MM. Boris Vallaud (Soc-NUPES) et Julien Bayou (Ecolo-NUPES), Mme Cyrielle Chatelain (Ecolo-NUPES), M. André Chassaigne (GDR-NUPES) et 145 membres de l'Assemblée" | "Politique générale du Gouvernement" (Élisabeth Borne) | 11.07.2022 | 289 | 146 |
| 15.02.2023 | "Mme Marine Le Pen et 87 membres de l'Assemblée" | (no object printed) | 17.02.2023 | 287 | 89 |
| 09.06.2023 | "M. Boris Vallaud, Mmes Mathilde Panot, Cyrielle Chatelain, M. André Chassaigne et 145 membres de l'Assemblée" | (no object printed) | 12.06.2023 | 289 | 239 |
| 30.01.2024 | "Mathilde Panot, Boris Vallaud, Cyrielle Chatelain, André Chassaigne et 146 membres de l'Assemblée" | "Politique générale du Gouvernement" (Gabriel Attal) | 05.02.2024 | 289 | 124 |
| 31.05.2024 | "Mathilde Panot, André Chassaigne et 104 membres de l'Assemblée" | "Politique générale du Gouvernement" (Gabriel Attal) | 03.06.2024 | 289 | 222 |
| 31.05.2024 | "Marine Le Pen et 87 membres de l'Assemblée" | "Politique générale du Gouvernement" (Gabriel Attal) | 03.06.2024 | 289 | 89 |
| 04.10.2024 | "Boris Vallaud, Mathilde Panot, Cyrielle Chatelain, André Chassaigne et 188 membres de l'Assemblée" | "Politique générale du Gouvernement" (Michel Barnier) | 08.10.2024 | 289 | 197 |
| 14.01.2025 | "Mathilde Panot et 57 membres de l'Assemblée" | "Politique générale du Gouvernement" (François Bayrou) | 16.01.2025 | 288 | 131 |
| 17.02.2025 | "Boris Vallaud et 65 membres de l'Assemblée" | "Politique générale du Gouvernement" (François Bayrou) | 19.02.2025 | 288 | 181 |
| 30.05.2025 | "Aurélie Trouvé et 57 membres de l'Assemblée" | "Politique générale du Gouvernement" (François Bayrou) | "04.05.2025" *[sic, as printed; before its own deposit date]* | 288 | 116 |
| 26.06.2025 | "Boris Vallaud et 65 membres de l'Assemblée" | "Politique générale du Gouvernement" (François Bayrou) | 01.07.2025 | 288 | 189 |
| 13.10.2025 | "Marine Le Pen, Éric Ciotti et 56 membres de l'Assemblée" | "Politique générale du Gouvernement" (Sébastien Lecornu) | 16.10.2025 | 289 | 144 |
| 13.10.2025 | "Mathilde Panot et 86 membres de l'Assemblée" | "Politique générale du Gouvernement" (Sébastien Lecornu) | 16.10.2025 | 289 | 271 |
| 09.01.2026 | "Mathilde Panot et 57 membres de l'Assemblée" | "Mercosur et souveraineté du Venezuela (Sébastien Lecornu)" | 14.01.2026 | 288 | 256 |
| 12.01.2026 | "Marine Le Pen et 57 membres de l'Assemblée" | "Mercosur et budget (Sébastien Lecornu)" | 14.01.2026 | 288 | 142 |
| 23.02.2026 | "Jean-Philippe Tanguy et 59 membres de l'Assemblée" | "Programmation pluriannuelle de l'énergie (PPE3)" (Sébastien Lecornu) | 26.02.2026 | 289 | 140 |
| 23.02.2026 | "Mathilde Panot et 57 membres de l'Assemblée" | "Programmation pluriannuelle de l'énergie (PPE3)" (Sébastien Lecornu) | 26.02.2026 | 289 | 108 |

The same page's per-PM tally, as printed: "Élisabeth Borne (2022-2024)" 3, "Gabriel Attal (2024-2024)" 3,
"Michel Barnier (2024-2024)" 1, "François Bayrou (2024-2025)" 4, "Sébastien Lecornu (depuis 2025)" 4.

[AN-REJ], the Assembly's notice of 16 October 2025:
> "Jeudi 16 octobre 2025, l'Assemblée nationale n'a pas adopté, en séance publique :"

**Art. 49 al. 3 motions** (the register above covers al. 2 only). [AN-MC17] lists, under "Application de l'article 49,
alinéa 3 de la Constitution", the two of 2 December 2024 (one adopted, above) and four of February 2025 (deposited
3, 3, 6 and 10 February 2025 by Mathilde Panot and colleagues), with no result printed. Their vote counts, and the
al. 3 motions on the 2026 finance bill (January–February 2026), are **not on a fetched page** (GAP G6).

**Still in office on 2026-09-24 — DERIVED, no page says it outright** (GAP G7):
- [AN-MC58] prints "Sébastien Lecornu (depuis 2025)" in its per-PM tally.
- [AN-MC17], the live list of the XVIIe's censure motions as served on 2026-09-24, has no entry after
  "lundi 23 février 2026", and the last two were rejected (140 and 108 for, [AN-MC58]).
- [EL-L26F] is the newest Government-composition communiqué in the Élysée listing [EL-ACT] as served 2026-09-24; the
  listing's September 2026 items are visits, declarations and the Council of Ministers, none a PM appointment.
- [EL-CM10], "Publié le 10 septembre 2026", records a Council of Ministers acting "Sur proposition du Premier ministre :" —
  a Government in office two weeks before the window's end, unnamed.

## Table 3 — PARTY LEADERS OF THE SEATED PARTIES BY DATE

Each row gives what the party's own site says on 2026-09-24. **No fetched page dates a leadership change inside the
window**, so every "since" is a GAP unless the page itself carries a date. Office words are the page's own.

| party | 2024 nuance key | leader on 2026-09-24 | office word (the page's) | since / changes in the window | source ids |
|---|---|---|---|---|---|
| Rassemblement National | RN | Jordan Bardella appears only as co-signatory of a communiqué with Marine Le Pen; the front page on 2026-09-24 is Marine Le Pen's presidential-campaign page | **GAP G8**: no office word on the fetched page | GAP | [P-RN] |
| Renaissance | ENS | Gabriel Attal | "Secrétaire général"; Emmanuel Macron "Président d'honneur" | the page dates his words "Lors du Conseil national du 8 décembre 2024"; his predecessor and the change date are **GAP G9** | [P-REN] |
| Les Républicains | LR | Bruno Retailleau | "Président des Républicains" | GAP G9 (predecessor and change date) | [P-LR] |
| Mouvement Démocrate | (ENS) | François Bayrou | "Président du Mouvement Démocrate, président du Parti Démocrate européen" | GAP G9 (no date on the page; no change known from a fetched page) | [P-MODEM] |
| Horizons | HOR | Édouard Philippe — the site's title is "Horizons Le parti - Édouard Philippe" | **GAP G8**: no office word on the fetched page | GAP | [P-HOR] |
| UDI | UDI | Hervé Marseille | "présidé par Hervé Marseille" (the page's meta description) | GAP G9 | [P-UDI] |
| Parti socialiste | SOC (UG) | Olivier Faure | "premier secrétaire du Parti socialiste en 2018" ("Le Premier Secrétaire") | since 2018 per the page; re-elections inside the window are not on the page | [P-PS] |
| Les Écologistes | ECO (UG) | Marine Tondelier | "Secrétaire nationale" ("Secrétariat exécutif") | GAP G9 | [P-ECO-T] [P-ECO-H] |
| La France insoumise | (UG) | **GAP G10**: the fetched pages name no office-holder; the front page carries Jean-Luc Mélenchon's 2027 candidacy | — | — | [P-LFI] |
| Parti communiste français | (UG) | Fabien Roussel appears (a signed declaration, an embedded post) | **GAP G8**: no office word on the fetched page | GAP | [P-PCF] |
| Union des droites pour la République | UXD | **GAP G11**: the party's site did not answer (curl exit 000 twice, logged) | — | — | — |
| LIOT and GDR groups | — | no single party; not leaders in the spec's sense | — | — | — |

The party pages, verbatim:

- [P-PS]:
  > "Figure clé du rassemblement de la gauche en France, Olivier Faure a mené une carrière de conseiller politique et de député avant de devenir premier secrétaire du Parti socialiste en 2018"
- [P-REN] ("Notre Bureau exécutif"): the lines "Gabriel", "Attal", "Secrétaire général"; then, after his message,
  > "Lors du Conseil national du 8 décembre 2024"

  and the lines "Emmanuel", "Macron", "Président d'honneur".
- [P-LR] ("équipe dirigeante"): the lines "Président", "Bruno Retailleau", "Président des Républicains", "Sénateur de Vendée".
- [P-MODEM]:
  > "Président du Mouvement Démocrate, président du Parti Démocrate européen"
- [P-UDI], meta description of the front page (in the saved bytes as an attribute, so not in the visible text):
  > "présidé par Hervé Marseille"
- [P-ECO-T] ("trombinoscope"): the lines "Secrétaire nationale", "Secrétariat exécutif", "Marine Tondelier".
- [P-HOR]: page title "Horizons Le parti - Édouard Philippe".
- [P-RN]:
  > "Communiqué de Marine Le Pen et Jordan Bardella sur la détérioration de la situation sécuritaire en Europe et les actions hybrides russes"
- [P-LFI]:
  > "Soutenez la candidature de Jean-Luc Mélenchon pour l'élection présidentielle de 2027 !"
- [P-PCF]:
  > "Interdiction de la « liste unie » en Israël : Netanyahou musèle l'opposition Déclaration de Fabien Roussel"

## The constitutional rules (§8 and §11)

All from [CC-CONST], the Conseil constitutionnel's "Texte intégral de la Constitution du 4 octobre 1958 en vigueur".

**The President: term and election (Art. 6, 7).**
> "Le Président de la République est élu pour cinq ans au suffrage universel direct."
>
> "Nul ne peut exercer plus de deux mandats consécutifs."
>
> "Le Président de la République est élu à la majorité absolue des suffrages exprimés. Si celle-ci n'est pas obtenue au premier tour de scrutin, il est procédé, le quatorzième jour suivant, à un second tour. Seuls peuvent s'y présenter les deux candidats qui, le cas échéant après retrait de candidats plus favorisés, se trouvent avoir recueilli le plus grand nombre de suffrages au premier tour."
>
> "L'élection du nouveau Président a lieu vingt jours au moins et trente-cinq jours au plus avant l'expiration des pouvoirs du président en exercice."

*gloss:* the presidential two-round system is in the Constitution itself; the Code électoral's own text on it was not
reachable (GAP G12).

**The President names the Prime Minister (Art. 8).**
> "Le Président de la République nomme le Premier ministre. Il met fin à ses fonctions sur la présentation par celui-ci de la démission du Gouvernement."
>
> "Sur la proposition du Premier ministre, il nomme les autres membres du Gouvernement et met fin à leurs fonctions."

**Dissolution and its one-year bar (Art. 12).**
> "Le Président de la République peut, après consultation du Premier ministre et des présidents des assemblées, prononcer la dissolution de l'Assemblée nationale."
>
> "Les élections générales ont lieu vingt jours au moins et quarante jours au plus après la dissolution."
>
> "L'Assemblée nationale se réunit de plein droit le deuxième jeudi qui suit son élection. Si cette réunion a lieu en dehors de la période prévue pour la session ordinaire, une session est ouverte de droit pour une durée de quinze jours."
>
> "Il ne peut être procédé à une nouvelle dissolution dans l'année qui suit ces élections."

**DERIVED**: after the elections of 30 June / 7 July 2024, no new dissolution was possible before July 2025. The doyen
d'âge said so at the opening [AN-S18]:
> "nous entrons dans une période qui place l'Assemblée nationale au cœur du jeu politique, au moins pour un an, jusqu'au moment où le chef de l'État retrouvera constitutionnellement le droit de dissolution"

**The Government and its responsibility (Art. 20).**
> "Le Gouvernement détermine et conduit la politique de la nation."
>
> "Il dispose de l'administration et de la force armée."
>
> "Il est responsable devant le Parlement dans les conditions et suivant les procédures prévues aux articles 49 et 50."

**Confidence, censure and 49.3 (Art. 49).**
> "Le Premier ministre, après délibération du conseil des ministres, engage devant l'Assemblée nationale la responsabilité du Gouvernement sur son programme ou éventuellement sur une déclaration de politique générale."
>
> "L'Assemblée nationale met en cause la responsabilité du Gouvernement par le vote d'une motion de censure. Une telle motion n'est recevable que si elle est signée par un dixième au moins des membres de l'Assemblée nationale. Le vote ne peut avoir lieu que quarante-huit heures après son dépôt. Seuls sont recensés les votes favorables à la motion de censure qui ne peut être adoptée qu'à la majorité des membres composant l'Assemblée. Sauf dans le cas prévu à l'alinéa ci-dessous, un député ne peut être signataire de plus de trois motions de censure au cours d'une même session ordinaire et de plus d'une au cours d'une même session extraordinaire."
>
> "Le Premier ministre peut, après délibération du conseil des ministres, engager la responsabilité du Gouvernement devant l'Assemblée nationale sur le vote d'un projet de loi de finances ou de financement de la sécurité sociale. Dans ce cas, ce projet est considéré comme adopté, sauf si une motion de censure, déposée dans les vingt-quatre heures qui suivent, est votée dans les conditions prévues à l'alinéa précédent. Le Premier ministre peut, en outre, recourir à cette procédure pour un autre projet ou une proposition de loi par session."

**DERIVED**: "la majorité des membres composant l'Assemblée" is 289 of 577; the register [AN-MC58] prints 289, or 288
or 287 when seats were vacant. The 4 December 2024 motion's 331 [AN-SCR519] cleared it.

**The consequence (Art. 50).**
> "Lorsque l'Assemblée nationale adopte une motion de censure ou lorsqu'elle désapprouve le programme ou une déclaration de politique générale du Gouvernement, le Premier ministre doit remettre au Président de la République la démission du Gouvernement."

**Cohabitation.** The word appears in no article of the fetched text. What the spec calls cohabitation is the
**DERIVED** reading of Art. 8 (the President names the PM), Art. 20 (the Government determines policy and answers to
Parliament) and Art. 49–50 (the Assembly can remove it): a President whose camp lacks an Assembly majority must name a
PM the Assembly will not censure. The 2024–2026 rows above are the case in point — three governments and one
confidence refusal — without a formal cohabitation.

**The Assembly's size (Art. 24).**
> "Les députés à l'Assemblée nationale, dont le nombre ne peut excéder cinq cent soixante-dix-sept, sont élus au suffrage direct."

**The two-round legislative system.** The Code électoral (L. 123, L. 126, L. 162) was not reachable (GAP G12;
`returns_2024.md` cites its own 2026-08-28 fetch of those articles). The Assembly's own "Fiche de synthèse n°3 :
L'élection des députés" [AN-FICHE] states the rule:
> "Les députés sont élus au suffrage universel direct, au scrutin uninominal majoritaire à deux tours par tous les Français âgés de dix-huit ans au moins, jouissant de leurs droits civils et politiques et n'étant pas dans un cas d'incapacité prévu par la loi."
>
> "Pour être élu dès le premier tour, il faut obtenir la majorité absolue, c'est-à-dire plus de la moitié des suffrages exprimés, et un nombre de suffrages au moins égal au quart des électeurs inscrits."
>
> "Si aucun candidat n'y parvient, il y a lieu de procéder à un second tour de scrutin auquel ne peuvent se présenter que les candidats ayant obtenu au premier tour un nombre de suffrages au moins égal à 12,5 % des électeurs inscrits. Pour être élu au second tour, la majorité relative suffit : le candidat ayant obtenu le plus grand nombre de suffrages l'emporte."
>
> "Le scrutin a lieu un dimanche, le second tour se déroulant, s'il y a lieu, le dimanche qui suit le premier tour."
>
> "L'Assemblée nationale se renouvelle en principe intégralement tous les cinq ans. Ses pouvoirs expirent ainsi « le troisième mardi de juin de la cinquième année qui suit son élection » (loi organique n° 2001-419 du 15 mai 2001 modifiant la date d'expiration des pouvoirs de l'Assemblée nationale) et les élections législatives doivent avoir lieu dans les soixante jours qui précèdent cette date."

**DERIVED**: with no further dissolution, the XVIIe's powers expire on the third Tuesday of June 2029 and the next
scheduled legislative election falls in the sixty days before it.

## Against the spec's §4 and §8

- §4 "semi-presidential; Assemblée nationale 577; 30 Jun / 7 Jul 2024, snap": the polling days match [EL-DIS]; 577 is
  the Constitution's ceiling (Art. 24) and the Assembly's own count ("577 députés", [AN-EFF] [AN-FICHE]); nothing fetched
  contradicts "snap". The word "semi-presidential" is the spec's, not the Constitution's — no contradiction, just no
  source for the label.
- §4's start rule ("a snap election starts on the day it was triggered"): the trigger is the address of the evening of
  2024-06-09 [EL-DIS]; the government of record on that day is Attal's (appointed 2024-01-09, resignation accepted
  2024-07-16), and the chamber of record is none — the XVIe is dissolved that evening and the XVIIe convenes 2024-07-18.
- §8 "the president's powers in cohabitation": the Constitution has no cohabitation article; the rule is Art. 8 + 20 +
  49 + 50 as quoted. The spec should say "the president's powers without an Assembly majority" if it wants a sourced phrase.
- One correction to the task's own framing: the XVIe's election dates ("2022-06-12/19") are not on any fetched page and
  are billed (G1), not confirmed.

## GAPS

- **G1: the XVIe législature's polling days (June 2022) are not on a fetched page.** Its opening (28 June 2022) is.
- **G2: no JORF decree text.** Légifrance is behind a Cloudflare challenge; every appointment, government-composition,
  dissolution and cessation-of-functions decree is cited through the Élysée's communiqué of the same day instead.
- **G3: the XVIIe's composition by group at its opening (18–19 July 2024) is not on a fetched page**; [AN-EFF] is the
  live table on 2026-09-24. `returns_2024.md` has the seats by nuance.
- **G4: the parties of each government are not stated on any fetched official page.** The Élysée composition pages
  list ministers without party. Only the PM's party office is on record where a party page states it (Attal, Bayrou).
- **G5: the Bayrou Government's resignation has no fetched page.** A guessed Élysée URL for 9 September 2025 served the
  6 October 2025 Lecornu page (logged and deleted). The end date 2025-09-09 is DERIVED from the next appointment.
- **G6: the Art. 49 al. 3 motions' counts other than 4 December 2024** (February 2025; the 2026 finance bill's) are not
  on a fetched page; the al. 2 register [AN-MC58] does not cover them.
- **G7: no fetched page dated 2026-09-24 names the Prime Minister in office.** Lecornu's continuation is DERIVED
  (Table 2's last block). `gouvernement.fr` and `info.gouv.fr`, which would state it, are behind the challenge.
- **G8: office words for Bardella (RN), Philippe (Horizons) and Roussel (PCF)** are not on the fetched party pages.
- **G9: leadership change dates inside the window** (Renaissance's 8 December 2024 is the only date on a page; LR,
  MoDem, UDI, Écologistes give none) and every predecessor are not on a fetched page.
- **G10: La France insoumise names no office-holder on the fetched pages.**
- **G11: the UDR's site did not answer** (two attempts, curl exit 000).
- **G12: the Code électoral's own text** (presidential and legislative two-round articles) was not reachable; the
  Constitution's Art. 7 and the Assembly's fiche stand in.
- **G13: second-session re-verification (R-K9) is pending**, as for every SOURCED file.

*(Filed 2026-09-24 by the PS-1 France sourcing agent. Nothing outside `ElectionsData/france/records_by_date.md` and
`ElectionsData/france/raw/records/` was written. No code was touched, Unity was not run, nothing was committed.)*


---

# THE EXECUTIVE OF RECORD, 2022-07-01 to 2026-09-25 — president and governments, with the JORF decrees [SOURCED] [PROVISIONAL]

Class: SOURCED (executive pass, 2026-09-25, sourcing agent; the window's end moves to 2026-09-25). `[PROVISIONAL]` until a
second session re-verifies (R-K9). This section supplements Table 2 above: it adds the JORF decrees behind the Élysée
communiqués (Légifrance read through Wayback `id_` captures, since `legifrance.gouv.fr` refuses this machine — see the
head of this file), the first round of the 2022 presidential election, the Bayrou Government's end by decree, and the
newest official composition page. It repeats no quote already above; every Élysée id it cites ([EL-*]) is in the
register above, with its raw file under `raw/records/`.

**Fetched, never recalled.** Every new quote below was read in a page fetched on 2026-09-25 (07:00–07:15 UTC, see
`raw/executive/fetch_log.txt`) and saved byte for byte under `raw/executive/`; SHA-256 in `raw/executive/SHA256SUMS.txt`
and in the register below. Quotes are verbatim and checked against the saved bytes by the same script rule as above (tags
stripped, entities decoded, U+2019 read as `'`, non-breaking spaces and whitespace runs collapsed). Wayback `id_` captures
serve the archived page's original bytes; the capture served is logged per file. Légifrance's pages print the text with
"Versions" widgets between articles; those words are the page's, not the decree's, and are not quoted.

**Party keys.** As above: `Assets/Scripts/Data/PartySystem.cs`'s France roster (`UG ENS RN LR DVD UXD DVG REG HOR DVC UDI
SOC ECO DIV EXD`). Renaissance rides `ENS`; MoDem rode `ENS` in 2024 (Table 3's DERIVED mapping); Les Républicains is `LR`.

## Register of the new ids
(all accessed 2026-09-25; saved as `raw/executive/<file>`; "page's own date" is what the page itself shows)

| id | URL | publisher | page's own date | file | bytes | SHA-256 |
|---|---|---|---|---|---|---|
| [CC-195] | https://www.conseil-constitutionnel.fr/decision/2022/2022195PDR.htm | Conseil constitutionnel | "Décision n° 2022-195 PDR du 13 avril 2022"; "Rendu public le 13 avril 2022" | `cc_2022-195PDR_premier-tour.html` | 69358 | `91dc2a2f691bc433aab45365d96d5c38e8ecb0a62a0770d182e421b8e46366c2` |
| [EL-SM] | https://www.elysee.fr/sitemap.xml | Élysée (sitemap index) | `lastmod` 2026-06-09 on every child sitemap | `elysee_sitemap.xml` | 1106 | `d0ef9c6e91ccc24e4e4392c030f746083cd6bdaf88bf57efda721aebcb7b54d0` |
| [EL-SMP] | https://www.elysee.fr/sitemap.publication.xml | Élysée (publication sitemap, every dated article URL) | as served 2026-09-25; carries URLs dated up to 2026-09 | `elysee_sitemap.publication.xml` | 12174683 | `b929f579cec9cec6faaa18980ecaa1b5ad8c04ab242db3e91ab08af7b2e7c31f` |
| [JO-0114] | https://www.legifrance.gouv.fr/jorf/jo/2022/05/17/0114 | Légifrance (JORF issue sommaire) via Wayback | "JORF n° 0114 du 17 mai 2022" | `wb_jorf_2022_05_17_0114.html` | 327805 | `e2f8bbc87f66caa09b83af7cf1d0b05abec63d9dcde873013567b56ccc2e5e83` |
| [JO-0007] | https://www.legifrance.gouv.fr/jorf/jo/2024/01/10/0007 | Légifrance via Wayback | "JORF n° 0007 du 10 janvier 2024" | `wb_jorf_2024_01_10_0007.html` | 333638 | `fc330b5836e1fd14654d3d547df8f2581e63795cdeeaac58a360e38d2cf8f62e` |
| [JO-0169] | https://www.legifrance.gouv.fr/jorf/jo/2024/07/17/0169 | Légifrance via Wayback | "JORF n° 0169 du 17 juillet 2024" | `wb_jorf_2024_07_17_0169.html` | 353745 | `1c342f14dda75697bfe4b58a6262b994411211e9567544e04ae1bc6d94ed29c6` |
| [JO-0212] | https://www.legifrance.gouv.fr/jorf/jo/2024/09/06/0212 | Légifrance via Wayback | "JORF n° 0212 du 6 septembre 2024" | `wb_jorf_2024_09_06_0212.html` | 296844 | `9f008121d86f9e14167cc5156c220219a13e17c363ce812e402fff5259888dba` |
| [JO-0295] | https://www.legifrance.gouv.fr/jorf/jo/2024/12/14/0295 | Légifrance via Wayback | "JORF n° 0295 du 14 décembre 2024" | `wb_jorf_2024_12_14_0295.html` | 315947 | `f27c3a25e62f80a19664ffcc9da09140e985e283e290707c86a2b9ba5e7db8aa` |
| [JO-0210] | https://www.legifrance.gouv.fr/jorf/jo/2025/09/10/0210 | Légifrance via Wayback | "JORF n° 0210 du 10 septembre 2025" | `wb_jorf_2025_09_10_0210.html` | 324286 | `d151e5dca43143feaaf9674a35a2352b7457e838013bcaf98f10d688f33971ae` |
| [JD-C16] | https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000045802689 | Légifrance (decree text) via Wayback | "Fait le 16 mai 2022."; "JORF n°0114 du 17 mai 2022 Texte n° 1" | `wb_legifrance_JORFTEXT000045802689.html` | 96124 | `3cdc39bf9a6c4deda11e5d9e88bd5ae043009f6d77b04b6c17c33bffcc66924e` |
| [JD-B16] | https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000045802692 | Légifrance via Wayback | "Fait le 16 mai 2022."; "JORF n°0114 du 17 mai 2022 Texte n° 2" | `wb_legifrance_JORFTEXT000045802692.html` | 100491 | `b0278dd3e9c4e8bb74eac936535bf47b5b00e303615fcb058d5afab80c12146c` |
| [JD-B09] | https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000048898744 | Légifrance via Wayback | "Fait le 9 janvier 2024."; "JORF n°0007 du 10 janvier 2024 Texte n° 1" | `wb_legifrance_JORFTEXT000048898744.html` | 90564 | `9ae7b26bb02bf94052688f64651d6b1eb7ee32703b16595bd8321967df6cac1e` |
| [JD-A09] | https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000048898747 | Légifrance via Wayback | "Fait le 9 janvier 2024."; "JORF n°0007 du 10 janvier 2024 Texte n° 2" | `wb_legifrance_JORFTEXT000048898747.html` | 95344 | `553a022ad3c6cff7c5f835e67a6e93a0472582c81443cdc7c2753cb4b6905a6d` |
| [JD-A16] | https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000050000932 | Légifrance via Wayback | "Fait le 16 juillet 2024."; "JORF n°0169 du 17 juillet 2024 Texte n° 1" | `wb_legifrance_JORFTEXT000050000932.html` | 96301 | `6dce5f1e87cf6c5a757d5fd1f478a303fcdf1de56f0d761cec04d6710c1b4975` |
| [JD-BA05] | https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000050185112 | Légifrance via Wayback | "Fait le 5 septembre 2024."; "JORF n°0212 du 6 septembre 2024 Texte n° 1" | `wb_legifrance_JORFTEXT000050185112.html` | 100390 | `87d482b4d65ace0203548a652e9d65c12db2188bcdaab7ccea07bfd16cd51c67` |
| [JD-BY13] | https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000050774288 | Légifrance via Wayback | "Fait le 13 décembre 2024."; "JORF n°0295 du 14 décembre 2024 Texte n° 1" | `wb_legifrance_JORFTEXT000050774288.html` | 100408 | `cfc73489b0c692952559eceabc59aac467344c85565d3cfa7e498053d7f82535` |
| [IG-COMP] | https://www.info.gouv.fr/composition-du-gouvernement | info.gouv.fr (Service d'information du Gouvernement) via Wayback, capture 2026-09-09 15:02:02 UTC | "Annoncé le 26/02/2026" | `wb_infogouv_composition_2026.html` | 223756 | `f29eed50e8de02f69eb79dfc070fed1b0907fa572dce16419de5803631095db0` |

## Table E1 — THE PRESIDENT OF RECORD

| from | until | president | party | basis |
|---|---|---|---|---|
| 2022-05-14, 0 h (re-elected 2022-04-24; the window opens 2022-07-01) | 2027-05-13 (**DERIVED**: five years, Art. 6 [CC-CONST]; the next election "vingt jours au moins et trente-cinq jours au plus" before, Art. 7) | Emmanuel Macron | Renaissance → `ENS` (the party's page names him "Président d'honneur" as served 2026-09-24 [P-REN]; the label he ran under in April 2022 is on no fetched page — GAP E1) | rounds 2022-04-10 [CC-195] and 2022-04-24 [CC-197]; proclamation 27 April 2022 [CC-197]; term Art. 6–7 [CC-CONST] |

**The first round, 10 April 2022** [CC-195], "Décision n° 2022-195 PDR du 13 avril 2022", "Déclaration du 13 avril 2022
relative aux résultats du premier tour de scrutin de l'élection du Président de la République":
> "Article 1 er . - Les résultats du scrutin pour l'élection du Président de la République, auquel il a été procédé les 9 et 10 avril 2022, sont les suivants :"
>
> "M. Emmanuel MACRON : 9 783 058"
>
> "Mme Marine LE PEN : 8 133 828"
>
> "Rendu public le 13 avril 2022."
>
> "JORF n°0088 du 14 avril 2022, texte n° 1"

*gloss:* "les 9 et 10 avril" — the Saturday 9 April is the overseas and Americas polling day; metropolitan France voted
Sunday 10 April. **The second round is "les 23 et 24 avril 2022"** in [CC-197], quoted in Table 2 above. So the 2022
rounds are 10 and 24 April 2022 on the Conseil's own two proclamations — **E-49 closes** on [CC-195] + [CC-197].

**The president's party.** [P-REN] (Table 3 above) prints the lines "Emmanuel", "Macron", "Président d'honneur" on the
Renaissance party page. No fetched page states his party on the day of the election or of his re-investiture; the
roster key `ENS` is the DERIVED mapping of Renaissance to the 2024 nuance (Table 3).

## Table E2 — THE GOVERNMENTS OF RECORD (Table 2 above, re-cut with the decrees)

Dates are the decrees' own ("Fait le …"), which coincide with the Élysée communiqués' "Publié le" in every case fetched.
"until" is the day the next Government's appointment decree is signed (Art. 8: the President "met fin à ses fonctions
sur la présentation par celui-ci de la démission du Gouvernement"); between a resignation and that day the outgoing
Government handles current business ("affaires courantes", the Élysée's own words in [EL-B24] [EL-A16] [EL-BA-DEM]).

| from | until | prime minister | cabinet parties (keys) | basis |
|---|---|---|---|---|
| 2022-05-16 (before the window) | 2024-01-09 | Élisabeth Borne | **GAP G4** (no official page states them); PM's own party: **GAP** (no party page fetched names her) | appointment decree of 16 May 2022 [JD-B16] ("Mme Elisabeth BORNE est nommée Première ministre."), JORF n°0114 texte 2 [JO-0114]; Élysée [EL-B16]; ministers [EL-B20]; resignation 8 Jan 2024 [EL-B24]; cessation decree of 9 Jan 2024 [JD-B09] |
| 2024-01-09 | 2024-09-05 (resigned 2024-07-16, current business to 2024-09-05) | Gabriel Attal | **GAP G4**; PM's own party: Renaissance → `ENS` ("Secrétaire général" as served 2026-09-24 [P-REN]; his party on 9 Jan 2024 not on a fetched page) | appointment decree of 9 Jan 2024 [JD-A09] ("M. Gabriel ATTAL est nommé Premier ministre."), JORF n°0007 texte 2 [JO-0007]; Élysée [EL-A09]; ministers [EL-A11]; resignation accepted 16 July 2024 [EL-A16]; cessation decree of 16 July 2024 [JD-A16] (the resignation letter dated 8 July 2024), JORF n°0169 texte 1 [JO-0169] |
| 2024-09-05 | 2024-12-13 (censured 2024-12-04, resignation "pris acte" 2024-12-05, current business to 2024-12-13) | Michel Barnier | **GAP G4**; PM's own party: Les Républicains → `LR` ("Président du Conseil national" of LR as served 2026-09-24 [P-LR]; his party on 5 Sept 2024 not on a fetched page) | appointment decree of 5 Sept 2024 [JD-BA05] ("M. Michel BARNIER est nommé Premier ministre."), JORF n°0212 texte 1 [JO-0212]; Élysée [EL-BA05]; ministers [EL-BA21]; censure [AN-SCR519] [AN-ADO]; resignation [EL-BA-DEM]; the cessation decree is **not fetched** (GAP E3) |
| 2024-12-13 | 2025-09-09 (confidence refused 2025-09-08) | François Bayrou | **GAP G4**; PM's own party: Mouvement Démocrate → `ENS` by the 2024 nuance mapping ("Président du Mouvement Démocrate" [P-MODEM]) | appointment decree of 13 Dec 2024 [JD-BY13] ("M. François BAYROU est nommé Premier ministre."), JORF n°0295 texte 1 [JO-0295]; Élysée [EL-BY13]; ministers [EL-BY23]; confidence vote [AN-CONF]; **the end is now sourced**: "Décret du 9 septembre 2025 relatif à la cessation des fonctions du Gouvernement", JORF n°0210 of 10 Sept 2025 [JO-0210] — GAP G5 closes (the decree's own body is not fetched, GAP E3) |
| 2025-09-09 | 2025-10-10 (resignation accepted 2025-10-06) | Sébastien Lecornu (first) | **GAP G4**; PM's own party: **GAP** (no fetched party page names him) | "Décret du 9 septembre 2025 portant nomination du Premier ministre", JORF n°0210 [JO-0210]; Élysée [EL-L09]; ministers 5 Oct 2025 [EL-L05]; resignation [EL-L06]; the cessation decree is not fetched (GAP E3) |
| 2025-10-10 | 2026-09-25 (window end; in office) | Sébastien Lecornu (second) | **GAP G4**; PM's own party: **GAP** | Élysée [EL-L10] (the appointment decree's JORF issue is not captured, GAP E3); ministers 12 Oct 2025 [EL-L12] and 26 Feb 2026 [EL-L26F]; **in office on 2026-09-09**: [IG-COMP], the Government's own composition page as captured that day, "Annoncé le 26/02/2026 Premier ministre Sébastien Lecornu"; 2026-09-09 → 2026-09-25 remains **DERIVED** from [EL-SMP] (no Élysée article dated after 2026-02-26 names a Prime Minister's appointment or a Government's resignation) and [EL-CM10] |

**The decrees, verbatim.** Each is Légifrance's page for the JORF text, read through Wayback; the page's own date is
the decree's "Fait le". Every one is signed "Emmanuel Macron" and cites "Vu l'article 8 de la Constitution".

- [JD-C16], "Décret du 16 mai 2022 relatif à la cessation des fonctions du Gouvernement":
  > "Vu la lettre, en date du 16 mai 2022, par laquelle le Premier ministre a présenté au Président de la République la démission du Gouvernement,"
  >
  > "Article 1 Il est mis fin, sur la présentation de la démission du Gouvernement, aux fonctions de M. Jean CASTEX, Premier ministre, et des autres membres du Gouvernement."
  >
  > "Fait le 16 mai 2022."
- [JD-B16], "Décret du 16 mai 2022 portant nomination du Premier ministre":
  > "Article 1 Mme Elisabeth BORNE est nommée Première ministre."
  >
  > "Fait le 16 mai 2022."
- [JD-B09], "Décret du 9 janvier 2024 relatif à la cessation des fonctions du Gouvernement":
  > "Vu la lettre, en date du 8 janvier 2024, par laquelle la Première ministre a présenté au Président de la République la démission du Gouvernement,"
  >
  > "Article 1 Il est mis fin, sur la présentation de la démission du Gouvernement, aux fonctions de Mme Elisabeth Borne, Première ministre, et des autres membres du Gouvernement."
  >
  > "Fait le 9 janvier 2024."
- [JD-A09], "Décret du 9 janvier 2024 portant nomination du Premier ministre":
  > "Article 1 M. Gabriel ATTAL est nommé Premier ministre."
  >
  > "Fait le 9 janvier 2024."
- [JD-A16], "Décret du 16 juillet 2024 relatif à la cessation des fonctions du Gouvernement":
  > "Vu la lettre, en date du 8 juillet 2024, par laquelle le Premier ministre a présenté au Président de la République la démission du Gouvernement,"
  >
  > "Article 1 Il est mis fin, sur la présentation de la démission du Gouvernement, aux fonctions de M. Gabriel ATTAL, Premier ministre, et des autres membres du Gouvernement."
  >
  > "Fait le 16 juillet 2024."

  *gloss:* the letter is dated 8 July 2024, the day after the second round; the President accepted it on 16 July
  ([EL-A16]: "qui l'a acceptée ce jour"). The Government of record stayed Attal's through the interval.
- [JD-BA05], "Décret du 5 septembre 2024 portant nomination du Premier ministre":
  > "Article 1 M. Michel BARNIER est nommé Premier ministre."
  >
  > "Fait le 5 septembre 2024."
- [JD-BY13], "Décret du 13 décembre 2024 portant nomination du Premier ministre":
  > "Article 1 M. François BAYROU est nommé Premier ministre."
  >
  > "Fait le 13 décembre 2024."

**The JORF issues' sommaires** (each issue page lists its texts under "Présidence de la République"; the two decrees
are consecutive entries, "Texte n° 1" the cessation and "Texte n° 2" the appointment where both appear):

- [JO-0114], "JORF n° 0114 du 17 mai 2022": "Décret du 16 mai 2022 relatif à la cessation des fonctions du Gouvernement";
  "Décret du 16 mai 2022 portant nomination du Premier ministre".
- [JO-0007], "JORF n° 0007 du 10 janvier 2024": "Décret du 9 janvier 2024 relatif à la cessation des fonctions du
  Gouvernement"; "Décret du 9 janvier 2024 portant nomination du Premier ministre".
- [JO-0169], "JORF n° 0169 du 17 juillet 2024": "Décret du 16 juillet 2024 relatif à la cessation des fonctions du Gouvernement".
- [JO-0212], "JORF n° 0212 du 6 septembre 2024": "Décret du 5 septembre 2024 portant nomination du Premier ministre".
- [JO-0295], "JORF n° 0295 du 14 décembre 2024": "Décret du 13 décembre 2024 portant nomination du Premier ministre".
- [JO-0210], "JORF n° 0210 du 10 septembre 2025":
  > "Décret du 9 septembre 2025 relatif à la cessation des fonctions du Gouvernement"
  >
  > "Décret du 9 septembre 2025 portant nomination du Premier ministre"

  *gloss:* this is the Bayrou Government's end and the first Lecornu appointment, both dated 9 September 2025 — the date
  Table 2 had DERIVED. The two decree pages themselves (JORFTEXT000052221929, JORFTEXT000052221932, the ids the sommaire
  links) have no Wayback capture (fetch log); their titles are quoted from the sommaire.

**The Government in office, the newest official page** [IG-COMP], `info.gouv.fr` "Composition du Gouvernement", Wayback
capture of 2026-09-09 15:02:02 UTC (the live site refuses this machine):
> "Composition du Gouvernement Annoncé le 26/02/2026 Premier ministre Sébastien Lecornu Premier ministre"

The page lists the ministers by ministry with no party for any of them (so GAP G4 stands even on the Government's own
page). It names, among others: "Laurent Nunez Ministre de l'Intérieur", "Gérald Darmanin Garde des Sceaux, ministre de la
Justice", "Roland Lescure Ministre de l'Économie, des Finances et de la Souveraineté industrielle, énergétique et numérique".

**No later appointment on the Élysée's site** [EL-SMP]: the publication sitemap as served 2026-09-25 lists every dated
article URL. Its only entry under `/emmanuel-macron/2025/09/09/` is
`https://www.elysee.fr/emmanuel-macron/2025/09/09/le-president-de-la-republique-a-nomme-sebastien-lecornu-premier-ministre`
— there is no Élysée article for the Bayrou resignation (GAP G5's page does not exist; the decree [JO-0210] stands in).
Among its 2026 entries from March onward, every URL containing "nomination" is a "proposition-de-nomination-de-…" for an
agency head (AFD, ANSES, CNES, Banque de France, CNRS, ANJ), and none contains "premier-ministre" as a French office or
"demission"; the "premier-ministre" URLs of that span are meetings with foreign prime ministers (the Netherlands, the
United Kingdom, Lebanon, Norway, Hungary). **DERIVED**: no Prime Minister was appointed and no Government resigned
between 26 February 2026 and the sitemap's serving; with [IG-COMP] on 9 September 2026 and [EL-CM10] on 10 September
2026, Lecornu's second Government is the Government of record to the window's end.

## Against the task's framing

- "Borne (2022-05-16 → 2024-01-09)": confirmed on the decrees [JD-B16] [JD-B09].
- "Attal (2024-01-09 → 2024-09-05, resigned 2024-07-16 caretaker)": confirmed; the resignation letter is dated 8 July
  2024 and accepted 16 July [JD-A16] [EL-A16]; his Government's functions end with Barnier's appointment on 5 September
  2024 [JD-BA05].
- "Barnier (2024-09-05 → 2024-12-13, censured 2024-12-04)": confirmed [JD-BA05] [AN-SCR519] [JD-BY13].
- "Bayrou (2024-12-13 → ?)": the "?" is 2025-09-09 [JO-0210].
- "whatever followed": Lecornu, appointed 9 September 2025, resigned 6 October 2025, re-appointed 10 October 2025, in
  office on 9 September 2026 [IG-COMP]; nothing later on any fetched page.
- The 2022 rounds: 10 April [CC-195] and 24 April [CC-197] — E-49 closes.

## GAPS of this pass

- **E1: the president's party label on election day (April 2022) is on no fetched page.** [P-REN] gives his party office
  as served in 2026 only. `ENS` is a DERIVED key.
- **G4 stands: no official page states any cabinet's party composition.** The Élysée composition pages and [IG-COMP]
  list ministers by portfolio only. The cabinet-party column is billed GAP for every Government; only the PM's own
  party office is on record for Attal, Barnier, Bayrou (party pages, as served 2026-09-24). Borne and Lecornu: no
  fetched party page names them (the Renaissance page's "Membres de droit" list, [P-REN], names Attal, not them).
- **E3: decrees not fetched** (no Wayback capture found; the live site refuses): the ministers' composition decrees
  (every Government); the cessation decrees of the Barnier (JORF of 6 or 14 December 2024 — the 0295 issue's sommaire
  carries no such decree), Bayrou (title only, [JO-0210]) and first Lecornu Governments; the appointment decrees of
  9 September 2025 (title only, [JO-0210]) and 10 October 2025 (issue numbers 0233–0241 of 11 October 2025 probed, all
  404 on Wayback). The Élysée communiqués [EL-L06] [EL-L10] stand for those dates.
- **G5 closes** on [JO-0210]. **G2 narrows** to E3. **G7 narrows**: the Government is named on an official page dated
  2026-09-09 [IG-COMP]; the last sixteen days of the window remain DERIVED from [EL-SMP] and [EL-CM10].
- **E4: the ministers of each Government by party** would need the Assembly's or each party's own pages per minister;
  not attempted (out of the ask's scope; billed so the next pass knows).
- **G13 stands**: second-session re-verification (R-K9) pending.

*(Filed 2026-09-25 by the executive sourcing pass. Nothing outside `ElectionsData/france/records_by_date.md` and
`ElectionsData/france/raw/executive/` was written. No code was touched, Unity was not run, nothing was committed.)*
