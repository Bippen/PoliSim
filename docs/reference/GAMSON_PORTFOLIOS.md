# Gamson's law - portfolio allocation in coalition governments, from the literature's own abstracts

Fetched 2026-09-25 with `curl -sL -A "Mozilla/5.0"` from doi.org, api.crossref.org, cambridge.org (the publisher's landing pages, which now
carry the APSR, BJPS and EJPR abstracts) and web.archive.org (`id_` captures of the JSTOR page for Gamson 1961, which JSTOR itself refuses
to a curl - a "Client Challenge" page). Every page stored byte for byte under `raw/gamson/` with `SHA256SUMS` beside them. Every quote is
verbatim from the stored page (HTML entities decoded, whitespace collapsed); nothing is typed from memory. The papers' BODIES (regression
figures, the exact intercept and slope) are behind paywalls and are NOT reachable - what an abstract does not say is billed under GAPS.

Two of the five DOIs given in the task resolved to OTHER papers and were replaced by the Crossref search records stored beside them:
`10.2307/1958781` is Grofman & Muller 1973 (APSR 67(2) 514-539), not Browne & Franklin - the correct DOI is **10.2307/1958776**
[CR-BF73Q]; `10.1017/S0007123401000254` is Samuels & Snyder 2001 (BJPS 31(4) 651-671), not Warwick & Druckman - the correct DOI is
**10.1017/S0007123401000242** [CR-WD01Q]. The misfetched pages were deleted, not stored.

## The rule as the model will use it

| element | rule | traced to |
|---|---|---|
| 1. Payoff proportional to contribution | A coalition party's share of cabinet portfolios equals its share of the coalition's parliamentary seats (its seats divided by the seats of all governing parties). Gamson's own proposition: "a share of the payoff proportional to the amount of resources which they contribute to a coalition"; the measurement is "the percentage share of cabinet ministries received by parties for their percentage contribution of parliamentary seats/votes to the coalition". | [G61] as quoted in [BF73]; [WD01] "one-to-one proportion" |
| 2. The regularity is near one-to-one | "the parties in a governing coalition tend to receive portfolios in one-to-one proportion to the amount of legislative support they contribute to the coalition"; "the near-perfect relationship ... between a coalition party's seat contribution to the government and its quantitative allocation of cabinet portfolios". The exact regression intercept and slope are in the papers' bodies, not the abstracts - GAP 1. | [WD01], [WD06] |
| 3. The deviation: large parties underpaid, small overpaid | "large parties tend to be proportionately underpaid and small parties overpaid, the larger or smaller they become. This effect, however, is most pronounced when the size of the coalition is small, and tends to reverse itself as the size of the coalition increases." And: "some slight deviations from proportionality coming at the expense of larger parties that lead coalition negotiations". The size of the bonus is not in any abstract - GAP 2. | [BF73], [WD01] |
| 4. The formateur does NOT take a premium | Proposer models predict "that parties in charge of coalition negotiations ought to be able to take a disproportionately large share of portfolio benefits for themselves"; the finding is "contra proposer models and any other models based on bargaining power". The model therefore gives the formateur's party no bonus - if anything it is the party underpaid under element 3. | [WD01], [WD06] |
| 5. Salience weighting changes nothing in the aggregate | "salience-weighted portfolios payoffs overwhelmingly mirror seat contributions" (an expert survey of portfolio salience in 14 Western European countries). So the model may count portfolios unweighted, or weighted by a salience scale, and expect the same proportional shares either way. Whether the abstract's "salience-weighted" includes the prime ministership at a higher weight is NOT stated - GAP 3. | [WD06], [WD01] |
| 6. WHICH portfolio a party gets | "parties which, in their election manifestos, emphasise themes corresponding to the policy remit of specific cabinet portfolios are more likely to obtain control over these portfolios"; "policy saliency is indeed an important predictor of portfolio allocation". This is the qualitative allocation (who gets Finance, who gets Justice), separate from the quantitative share of elements 1-5. | [BDD11] |

**Reading for the model:** portfolios_i = round(N × seats_i / Σ seats_coalition), with the residual of the rounding (and the observed
bonus of element 3) going to the smaller partners rather than to the formateur; the qualitative assignment follows each party's own
emphasis (element 6). No figure for the bonus or the prime-ministerial weight is sourced here - see GAPS.

## The abstracts, verbatim

**[G61]** Gamson, William A., "A Theory of Coalition Formation", American Sociological Review Vol. 26, No. 3 (Jun., 1961), pp. 373-382.
The JSTOR page's abstract (2016 Wayback capture; the 2025 capture and the live page serve only a JavaScript challenge and the citation
line): "Coalition formation is a pervasive aspect of social life. This paper presents a theory of coalition formation with a statement of
conditions and assumptions. While applicable to groups of varying sizes, it is shown to be consistent with Caplow's theory of coalitions in
the triad. It successfully handles the experimental results of Vinacke and Arkoff. Finally, the applicability of various work in n-person
game theory is discussed with the conclusion that, in its present state, it fails to provide a basis for a descriptive theory of
coalitions." JSTOR's citation line: "William A. Gamson, A Theory of Coalition Formation, American Sociological Review, Vol. 26, No. 3
(Jun., 1961), pp. 373-382". Crossref [CR-G61]: title "A Theory of Coalition Formation", author Gamson, container "American Sociological
Review", volume 26, issue 3, page "373", published-print 1961. *The abstract does not state the proportionality proposition; the
proposition's text is reachable only as Browne & Franklin quote it [BF73] - see GAP 4.*

**[BF73]** Browne, Eric C. & Franklin, Mark N., "Aspects of Coalition Payoffs in European Parliamentary Democracies", American Political
Science Review 67(2), June 1973, pp. 453-469, doi 10.2307/1958776 (Cambridge Core landing page, `citation_publication_date` 1973/06):
"One important proposition about the distribution of coalition payoffs is found in W. A. Gamson's theory of coalition formation: "Any
participant will expect others to demand from a coalition a share of the payoff proportional to the amount of resources which they
contribute to a coalition." This proposition is tested in a universe of cabinet coalitions existing in thirteen European democracies
during the postwar period. Here, payoffs to partners are indicated by the percentage share of cabinet ministries received by parties for
their percentage contribution of parliamentary seats/votes to the coalition.The proportionality proposition is shown to hold strongly.
Disproportionality, however, is observed to occur in distributions at the extremities of party size—large parties tend to be
proportionately underpaid and small parties overpaid, the larger or smaller they become. This effect, however, is most pronounced when the
size of the coalition is small, and tends to reverse itself as the size of the coalition increases."

**[WD01]** Warwick, Paul V. & Druckman, James N., "Portfolio Salience and the Proportionality of Payoffs in Coalition Governments", British
Journal of Political Science 31(4), October 2001, pp. 627-649, doi 10.1017/S0007123401000242 (Cambridge Core, `citation_publication_date`
2001/10, online 2001/09/05): "A fundamental divide has emerged over how portfolio payoffs are distributed among parties in parliamentary
coalitions. On one side lies very strong empirical evidence that the parties in a governing coalition tend to receive portfolios in
one-to-one proportion to the amount of legislative support they contribute to the coalition, with perhaps some slight deviations from
proportionality coming at the expense of larger parties that lead coalition negotiations. On the other side of the debate lies a stream of
formal theories that suggest the opposite – that parties in charge of coalition negotiations ought to be able to take a disproportionately
large share of portfolio benefits for themselves. In this article, we address this disjuncture by re-examining the empirical connection
between legislative seats and portfolio payoffs with the aid of a new and more extensive dataset, a different method of analysis, and what
we see as a more valid operationalization of the dependent variable. This operationalization involves the inclusion, for the first time,
of evidence concerning the importance or salience of the portfolios each party receives, as opposed to just their quantity. The article
concludes with an assessment of the implications of our findings for the debate over the rewards of coalition membership in parliamentary
democracies."

**[WD06]** Warwick, Paul V. & Druckman, James N., "The portfolio allocation paradox: An investigation into the nature of a very strong but
puzzling relationship", European Journal of Political Research 45(4), June 2006, pp. 635-665, doi 10.1111/j.1475-6765.2006.00632.x
(the doi now resolves to Cambridge Core, `citation_publication_date` 2006/06): "Perhaps the strongest empirical finding in political
science is 'Gamson's Law': the near-perfect relationship that exists in parliamentary systems between a coalition party's seat
contribution to the government and its quantitative allocation of cabinet portfolios. Nevertheless, doubts remain. What would happen if
the salience or importance of the various portfolios was also taken into account? Should it not be the case that payoffs correspond with
bargaining power rather than seat contributions? And perhaps most significantly, would addressing these issues produce evidence that the
parties designated to form governments extract disproportionately large payoffs for themselves, as predicted by 'proposer' models of
bargaining? Utilizing the results of a new expert survey of portfolio salience in 14 Western European countries, the authors of this
article explore each of these questions. Their basic finding is that salience-weighted portfolios payoffs overwhelmingly mirror seat
contributions, contra proposer models and any other models based on bargaining power. The article concludes with a discussion of the
implications for formal models of bargaining."

**[BDD11]** Bäck, Hanna, Debus, Marc & Dumont, Patrick, "Who gets what in coalition governments? Predictors of portfolio allocation in
parliamentary democracies", European Journal of Political Research 50(4), June 2011, pp. 441-478, doi 10.1111/j.1475-6765.2010.01980.x
(Cambridge Core, `citation_publication_date` 2011/06): "Ministerial portfolios are the most obvious payoffs for parties entering a
governing coalition in parliamentary democracies. This renders the bargaining over portfolios an important phase of the government
formation process. The question of 'who gets what, and why?' in terms of ministerial remits has not yet received much attention by
coalition or party scholars. This article focuses on this qualitative aspect of portfolio allocation and uses a new comparative dataset to
evaluate a number of hypotheses that can be drawn from the literature. The main hypothesis is that parties which, in their election
manifestos, emphasise themes corresponding to the policy remit of specific cabinet portfolios are more likely to obtain control over these
portfolios. The results show that policy saliency is indeed an important predictor of portfolio allocation in postwar Western European
parliamentary democracies."

## Register of ids

| id | URL | publisher | page date | file (under `raw/gamson/`) | bytes | sha256 |
|---|---|---|---|---|---|---|
| [G61] | https://web.archive.org/web/20161106033904id_/http://www.jstor.org/stable/2090664 | JSTOR (ASA), Wayback capture 2016-11-06 | article Jun. 1961 | `wayback_2016_jstor_2090664.html` | 74012 | ba6bcd9b402fd3e009aaba6c65d5c3462724615dccffaff2479d661f6538fa47 |
| [G61-WB25] | https://web.archive.org/web/20250612220849id_/https://www.jstor.org/stable/2090664 | JSTOR, Wayback capture 2025-06-12 (citation line only) | - | `wayback_jstor_2090664.html` | 12022 | 2183dbbeba1d00f0b4c7f935edb0806a67c41316d30a401a6315803b2d8d5510 |
| [G61-JS] | https://doi.org/10.2307/2090664 → https://www.jstor.org/stable/2090664?origin=crossref | JSTOR live: "Client Challenge" page, no content | - | `landing_10_2307_2090664.html` | 3038 | 32ed63159c77e21ee19ca1b9aa3213ccf0218eb59539560b132a8e68ef0e18ea |
| [CR-G61] | https://api.crossref.org/works/10.2307/2090664 | Crossref | 1961 | `crossref_10_2307_2090664.json` | 1475 | 8f08ee823100cf5d9b8281bbc40c61d4fd5ae47c8f71b700dfbe24b88f198c5a |
| [BF73] | https://doi.org/10.2307/1958776 → https://www.cambridge.org/core/journals/american-political-science-review/article/abs/aspects-of-coalition-payoffs-in-european-parliamentary-democracies/0E7C4BA92EA01477DEB3A568B8400D0D | Cambridge University Press | 1973/06 | `landing_10_2307_1958776.html` | 822532 | ba36810eb7b989d93884e644c29dc95c058102e1bea440b16d4574360fc1595d |
| [CR-BF73] | https://api.crossref.org/works/10.2307/1958776 | Crossref | 1973 | `crossref_10_2307_1958776.json` | 4638 | fc29ae909f252707c15b5738a74d0212e20b1bf47a9c626331d55dcf3a2b0ec3 |
| [CR-BF73Q] | https://api.crossref.org/works?query.bibliographic=Aspects+of+Coalition+Payoffs+in+European+Parliamentary+Democracies+Browne+Franklin&rows=3&select=DOI,title,author,container-title,volume,issue,page,issued | Crossref search (first hit 10.2307/1958776) | - | `crossref_query_browne_franklin_1973.json` | 1604 | 673598b1d9ccedd99b27e7839a887a6397c4e7eb35da7e5f0be9038421b33500 |
| [WD01] | https://doi.org/10.1017/S0007123401000242 → https://www.cambridge.org/core/journals/british-journal-of-political-science/article/abs/portfolio-salience-and-the-proportionality-of-payoffs-in-coalition-governments/58CD2A88669FFF25E1173A68CD1F9B00 | Cambridge University Press | 2001/10 | `landing_10_1017_S0007123401000242.html` | 747524 | eb83375b7784bf69fa18f7058bb9e92c7e44b5afc40dd9c9b6dd23944b377efd |
| [CR-WD01] | https://api.crossref.org/works/10.1017/S0007123401000242 | Crossref | 2001 | `crossref_10_1017_S0007123401000242.json` | 3715 | 4d90488e3ecb6f5a8ad2c534c2a248495936cec8d836eb9dec1dffe3db3d76c3 |
| [CR-WD01Q] | https://api.crossref.org/works?query.bibliographic=Portfolio+Salience+and+the+Proportionality+of+Payoffs+in+Coalition+Governments+Warwick+Druckman&rows=3&select=... | Crossref search (first hit 10.1017/s0007123401000242) | - | `crossref_query_warwick_druckman_2001.json` | 1664 | e07e7757e10b1ca757ab4b03991d8b84b13ba05a4f86fcf6650ee80a933fa18c |
| [WD06] | https://doi.org/10.1111/j.1475-6765.2006.00632.x → https://www.cambridge.org/core/journals/european-journal-of-political-research/article/abs/portfolio-allocation-paradox-an-investigation-into-the-nature-of-a-very-strong-but-puzzling-relationship/242AB26DF616171505DFB472FDFAB37E | Cambridge University Press (EJPR) | 2006/06 | `landing_10_1111_j_1475-6765_2006_00632_x.html` | 862794 | 6923c27d96aa58fc29df43e78d3b431b53c39331eae55609e230041c018d3e28 |
| [CR-WD06] | https://api.crossref.org/works/10.1111/j.1475-6765.2006.00632.x | Crossref | 2006 | `crossref_10_1111_j_1475-6765_2006_00632_x.json` | 8583 | 97b4d63e0ccdd0e3d95595404d000c39f8bd2c37c82a605a8715c674f2197218 |
| [BDD11] | https://doi.org/10.1111/j.1475-6765.2010.01980.x → https://www.cambridge.org/core/journals/european-journal-of-political-research/article/abs/who-gets-what-in-coalition-governments-predictors-of-portfolio-allocation-in-parliamentary-democracies/F7A41650492FF1D2A4CF45D00270AB23 | Cambridge University Press (EJPR) | 2011/06 | `landing_10_1111_j_1475-6765_2010_01980_x.html` | 951117 | 5cae2f262b0ea7de44def5ec0d5f1c42bfc374227678e88f1554dad23ac92d46 |
| [CR-BDD11] | https://api.crossref.org/works/10.1111/j.1475-6765.2010.01980.x | Crossref | 2011 | `crossref_10_1111_j_1475-6765_2010_01980_x.json` | 18321 | e78151a0751a4668096d7c8d6a3269f11efdb4818a6931256dc071e8f4bbc641 |

All fetched 2026-09-25, HTTP 200, stored unaltered; `raw/gamson/SHA256SUMS` lists all fourteen files (a new folder).

## GAPS

1. **The regression's intercept and slope** (the "Gamson's law" line, portfolio share = a + b × seat share, with b near 1 and a small
   positive intercept) - no abstract states the figures; they are in the papers' bodies, behind Cambridge Core's paywall. Not reachable
   without the PDFs. The model has only "one-to-one" [WD01] and "near-perfect" [WD06].
2. **The small-party bonus's size** - [BF73] states its direction and its dependence on coalition size; no figure. Same cause.
3. **The prime ministership's weight** - no abstract says how the PM's office is counted or weighted; [WD06]'s salience survey exists
   but its weights are in the body. Not sourced. The model must either count the PM as one portfolio or choose a weight and mark it
   `[AUTHORED-DRAFT]`.
4. **Gamson's own text** - JSTOR refuses curl ("Client Challenge") and the Wayback captures carry only the abstract (2016) or the
   citation line (2025); the proportionality proposition is quoted here only as Browne & Franklin quote it in their abstract [BF73].
5. **Laver & Schofield (Multiparty Government, 1990)** - a book, not fetched; no attempt made beyond the five articles named.
6. **Two DOIs in the task were wrong** (10.2307/1958781 = Grofman & Muller; 10.1017/S0007123401000254 = Samuels & Snyder); corrected via
   the Crossref bibliographic search [CR-BF73Q], [CR-WD01Q]. The wrong papers' pages were deleted, not kept.
7. **The EJPR landing pages' `citation_online_date` reads 2026/01/01** - the journal's move to Cambridge; the print dates 2006/06 and
   2011/06 are the dates used.
