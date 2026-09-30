# Party families — CHES 2024's own classification [SOURCED]

Class: SOURCED (2026-09-30, `COMPLETED.md` §681). Read by `PoliSim.Data.PartyFamilies`, whose every value `EntrantLayerDiagnostic` proves
against the CSV below. The grouping the entrant layer applies is the expert survey's, not ours.

## Source register (fetched byte-exact 2026-09-30, the same release the positions were read from, `party_positions.md`)

| file (`raw/`) | URL | sha256 |
|---|---|---|
| `CHES_2024_final_v2.csv` | https://github.com/chesdata/chesdata.github.io/releases/download/ches-europe/CHES_2024_final_v2.csv | `1c1ec0532afa2a0a13317122cbbe40eb9ff35425191892d1fff24fbef6acc6a8` |
| `CHES.2024.Codebook.pdf` | https://github.com/chesdata/chesdata.github.io/releases/download/ches-europe/CHES.2024.Codebook.pdf | `20b46e1b25f3b43473f979ad9abe1f4f0ae1fa3f6ee5ab3e55b5df53839fc0f5` |

Citation: Rovny, J., et al. 2025, "The 2024 Chapel Hill Expert Survey", *Electoral Studies* 97, doi:10.1016/j.electstud.2025.102981.

## The variable, as the codebook states it

`family` — "This classification was initially based on Hix and Lord (1997) and Marks and Wilson (2000), except that we place confessional and
agrarian parties in separate categories. Family association for parties in Central/Eastern Europe was initially based on the Derksen
classification (now incorporated in Wikipedia). Classifications are triangulated by a) membership or affiliation with international and EU
party associations, b) self-identification, and c) extant categorizations including Parlgov (Doring and Manow 2011) and the Comparative
Manifesto Project (Volkens et al. 2020). We update family codings based on ideological shifts or organizational changes as documented on the
party's website or Wikipedia." The codebook adds: "Some parties may fit into two or more categories … CHES users are advised to review family
codings to ensure they match their own research goals and definitions."

| id | family | | id | family |
|---|---|---|---|---|
| 1 | Radical Right | | 7 | Green |
| 2 | Conservatives | | 8 | Regionalist |
| 3 | Liberal | | 9 | No family |
| 4 | Christian-Democratic | | 10 | Confessional |
| 5 | Socialist | | 11 | Agrarian/Centre |
| 6 | Radical Left | | | |

(The codebook's text layer is glyph-encoded; the table was read by decoding its fonts' ToUnicode maps, `Tools`-free, and matches the CSV's use.)

## The game's keys (CHES country code / party name → family)

- **Sweden (16):** S (SAP) 5 · SD 1 · M 2 · V 6 · C 11 · KD 4 · MP 7 · L 3
- **Germany (3):** CDU 4 · CSU 4 · AfD 1 · SPD 5 · Grune (Grunen) 7 · Linke (DL) 6 · **BSW 6** · FDP 3 · SSW — (not in CHES 2024)
- **France (6):** RN 1 · LR 2 · ENS (RE) 3 · HOR (Horizons) 3 · SOC (PS) 5 · UG, UXD, ECO, DVD, DVG, DVC, REG, UDI, DIV, EXD — (alliances or labels CHES does not classify)
- **Italy (8):** FdI 1 · Lega 1 · PD 5 · FI 2 · AzIV (A) 3 · PlusE (+E) 3 · SVP 8 · M5S (MS5) 9 = no family · AVS — (SI 5 and EV 7) · NM, IC, ScN, MAIE, UV —
- **Poland (26):** PiS 1 · Konf (Konfederacja) 1 · KO (PO) 4 · NL (Nowa Lewica) 5 · PSL 11 · TD — (Polska 2050 3 and PSL 11) · SLD — (the 2019 Lewica list) · MN —

**No family (—)** where CHES classifies no single party for the key — an electoral alliance whose members sit in different families (UG, AVS, TD,
SLD 2019), a party CHES does not cover, or CHES's own 9 ("No family"). Such a key is its own nest: the grouping never draws on it first.

**A created party** has no expert classification: a Splinter takes its parent's family; any other takes the family of the real party nearest it
on the vote model's two axes — [AUTHORED-DRAFT], on the play-calibration list.
