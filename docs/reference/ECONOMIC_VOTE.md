# The economic vote - its size by country and how responsibility divides it, from Duch & Stevenson's own texts

Fetched 2026-09-25 with `curl -sL -A "Mozilla/5.0"` from doi.org, api.crossref.org, cambridge.org (the book page and every chapter page
of Duch & Stevenson 2008), and web.archive.org (`id_` captures of ScienceDirect, JSTOR, and Raymond Duch's own site
`raymondduch.com`, where the authors posted their **draft book manuscript, version 1.7, "Draft Book Manuscript (Apr 07 Update)"**,
the Electoral Studies article's accepted draft, and their published Journal of Politics 2010 and Political Analysis 2005 articles). Every
file is stored byte for byte under `raw/economic_vote/` with `SHA256SUMS` beside them. HTML quotes are verbatim from the stored pages
(entities decoded, whitespace collapsed). PDF quotes are verbatim from text inflated out of the stored PDFs' FlateDecode streams (the
extractor walks the page tree; line breaks are joined, and a word the extractor split at a text-run boundary - e.g. "Catala n" - is
rejoined; the JoP 2010 PDF's text runs carry no space glyphs, so its quotes have their word spaces restored and its fi/fl ligatures
written out). Nothing is typed from memory.

**The one caution that governs everything below.** The printed book (Cambridge UP, March 2008) is paywalled: Cambridge serves only a
book description and a first-page preview of each chapter. The book's tables and figures are quoted here from the **authors' April 2007
draft (v1.7)** [BK-D], which is NOT the printed text: the printed chapter 3 preview says "678 economic vote measures for 113 political
parties in eighteen countries over a twenty-two–year period" [CH-3], where the draft (p. 58) says "678 economic vote measures for 105
political parties in 18 countries over a 20-year period" [BK-D]; the printed chapter 9 opens with the same two hypotheses in the same
words as the draft's chapter 9 (see element 5). Every draft figure is marked **[draft]**; the printed values may differ (GAP 1).

## The rule as the model will use it

| element | rule | traced to |
|---|---|---|
| 1. The unit | The economic vote of a party is the change in its vote probability (averaged over the survey's respondents) when each respondent's retrospective national-economy perception moves **one category worse on a three-point scale** (better / same / worse). The chief executive's economic vote is that change for "the party of the incumbent chief executive" (the PM's party; the president's party in the USA). Negative = the party loses as the economy worsens. It is NOT a one-standard-deviation effect. | [JOP10] p. 118; [BK-D] pp. 48, 51-52 fn 41; [ES-D] p. 17 |
| 2. The typical size | About **5 points** of vote probability for the chief executive's party: published, "the median impact of economic evaluations on the vote probabilities of incumbent PM parties at approximately 5%". [draft] "The average economic vote of the chief executive is -5 percent", range "about -20 percent to about 8 percent with a standard deviation of .04", "about 50 percent have values less than -4.5 percent". | [ES06] abstract; [BK-D] p. 59 |
| 3. Size by country | Only an ORDINAL ranking and a few verbal magnitudes are quotable (table below). Sweden ranks 3rd of 19 series; Germany 8th; France 9th; Italy 16th; US presidential 4th; US congressional 19th (last). No country-level numeric table exists in any reachable text - GAP 2. Poland is not in the sample - GAP 3. | [ES-D] p. 19-20 Figure 2; [BK-D] p. 68 Figure 3.3; [BK-D] pp. 62-67 |
| 4. Size by cabinet type | The PM party's economic vote, predicted by cabinet type [draft Table 9.4]: single-party majority **.067 / .069** (50 cases); single-party minority **.053 / .048** (19); coalition majority **.044 / .042** (66); coalition minority **.030 / .038** (11) - the two numbers per cell are the two specifications of Table 9.3. Presidents: US presidential elections -0.06 (divided government, n = 5) and -0.10 (unified, n = 1); French legislative studies, presidential party, 0.01 under cohabitation (n = 4) vs -0.05 unified (n = 8) [Table 9.1]. This is the quotable per-government magnitude the model can apply to each country's current cabinet. | [BK-D] pp. 247, 253-254 |
| 5. Division among governing parties | "Parties with a greater share of the status quo distribution of administrative responsibility will receive a greater share of the economic vote than parties with a smaller share" (printed book). Average shares of the NEGATIVE economic vote [draft Figure 9.5]: PM & opposition only - **PM 83.4 %, opposition 16.6 %** (55 cases); PM & partners only - **PM 74.08 %, partners 25.92 %** (11); PM & partners & opposition - **PM 51 %, partners 31.74 %, opposition 17.25 %** (18); opposition & partners only - partners 69.54 %, opposition 30.46 % (17). "in coalition cabinets, prime ministerial parties receive the lion's share of the overall economic vote and their cabinet partners most of the rest". | [CH-9] (printed preview); [BK-D] pp. 258, 273 |
| 6. Within the partners: seats and the finance ministry | A cabinet partner's economic vote grows with its cabinet share: "Economic Vote for Cabinet Partners = 0.007 (1.25) - 0.057 (2.42) X % of Cabinet Seats Held. R2=.10. Observations=84." [draft Figure 9.7]. Holding the finance ministry adds nearly as much as holding the premiership [draft Table 9.5]: not PM, not finance **.005**; PM, not finance **.028**; not PM, finance **.020**; PM and finance **.043**. | [BK-D] pp. 260-261, 264 |
| 7. Dispersion shrinks the total | "As the status quo distribution of administrative responsibility over parties becomes more equal, the overall economic vote declines" (printed). [draft Figure 9.8] "Economic Vote of Chief Executive = -0.002 (0.08)-0.066 (2.81)*Concentration of Responsibility. R2=.10. Observations=152." Published: Table 1 of [PA05], coefficient on concentration of authority ".081 (0.023)" (all cabinets, 152 obs.) and ".075 (.033)" (coalition cabinets, 76 obs.). Powell & Whitten's "clarity of its political responsibility" is the line of work this extends. | [CH-9]; [BK-D] p. 266; [PA05] p. 405; [PW93] |
| 8. Support parties | **GAP 4 - not estimated.** The book's "cabinet partners" are "non-prime ministerial parties who hold seats in the cabinet", so a party supporting a minority cabinet from outside is counted with the OPPOSITION. The closest sourced quantities: (a) the opposition's share of the negative economic vote, 16.6 % / 17.25 % (element 5), which pools support parties with every other non-cabinet party; (b) the book's own measure of how a minority cabinet and a strong committee system hand responsibility to non-cabinet parties - a party's share of administrative responsibility is "(1- w)c_i + w s_i" (cabinet-seat share c, legislative-seat share s), with w = "(institutional strength of opposition score + majority status score)/4" - of which the authors say "this impact is quite small compared to the impact of the distribution of responsibility that stems from the role that parties play in cabinet". | [BK-D] pp. 255 fn 219, 267-271 |

**Reading for the model (the parts it may apply, and the parts it must author):**

- `economic vote of the PM party` = the Table 9.4 cell for the current cabinet type (element 4), per one-category worsening of the
  retrospective perception (element 1). Sweden's day-one cabinet type, if single-party minority, reads .053 / .048 **[draft]**.
- The country ranking (element 3) may order countries within a cell; it supplies no magnitude, so any per-country scaling beyond the cabinet
  type is `[AUTHORED-DRAFT]` until GAP 2 is closed.
- The total negative economic vote divides PM > partners > opposition by the Figure 9.5 shares matching which kinds of parties lose
  (element 5), a partner's portion rising with its cabinet-seat share and with the finance ministry (element 6).
- A support party (confidence and supply, outside the cabinet) has **no sourced share of its own**. If the model gives it one, the only
  sourced handle is the opposition-influence formula (element 8b) - a party's responsibility share = (1-w)×cabinet share + w×seat share -
  applied through hypothesis 1 ("a greater share of ... administrative responsibility will receive a greater share of the economic vote");
  the resulting number is `[AUTHORED-DRAFT]`, not a literature figure.

## Size by country - the six model countries

The country codes in the two box-plots are the codes printed on their axes; the 19 codes pair one-to-one with the 19 full names printed in
[BK-D] Figure 3.2 (p. 63: "australia belgium canada denmark france germany greece iceland ireland italy netherlands new zealand norway
portugal spain sweden uk us congress us president"), so `sw` = Sweden (Switzerland is not in the sample), `de` = Germany (Denmark is `dk`).
The axis order is by magnitude, largest first: the draft article's text confirms "Britain ranks as having the second highest level" and
"U.S. Presidential elections (usp) rank as having the fourth highest level of economic voting out of our 19 countries", and "The median
country in the sample is Portugal" (`pt`, 10th of 19) [ES-D] p. 19. Axis order, [ES-D] Figure 2 p. 20 ("PM Party Economic Vote by
Country"; note: "PM Vote responding to economic perceptions that move one unit in worse direction"): **nz uk sw usp ca sp no de fr pt dk gc
ir au ic it be nl usc**. The book draft's Figure 3.3 (p. 68, "Magnitude of the Economic Vote by Country") prints the same order (with
`uspr`/`usco`), but its text names a different median: "The median country in this sample is Canada with an economic vote of the chief
executive of about -5 percent" [BK-D] p. 66 - an inconsistency inside the draft; the rank order itself is the same in both.

| country | rank (of 19) | what the text says, verbatim | source |
|---|---|---|---|
| Sweden | 3 (`sw`) | No sentence gives a Swedish figure. Opposition strength "High" (Table 9.7); Appendix 9.1 lists 4 Swedish cases, all at opposition-influence weight 0.75. | [ES-D] p. 20; [BK-D] pp. 68, 268, 275 |
| Germany | 8 (`de`) | "Note that until 1986 the German economic vote is in the neighborhood of -10 percent and it drops rather significantly to -.04 in 1987." "Ireland and Germany, for example, seem to have decreasing levels of economic voting throughout our sample period". | [BK-D] pp. 62, 64 |
| France | 9 (`fr`) | "Note that the magnitude of the French economic vote ranges between -6 to -8 percent prior to 1987 (the first full year of cohabitation) but then falls rather precipitously to around -3 percent in 1987 and 1988. It recovers to around -5 percent in 1991 but then falls again during the next period of cohabitation, 1993 and 1994." Table 9.1 (presidential party, legislative studies): cohabitation 0.01 (0.03) n = 4; unified -0.05 (0.03) n = 8. | [BK-D] pp. 65, 247 |
| Italy | 16 (`it`) | "we find almost no economic voting (although 1986 is an exception) prior to the electoral reforms of 1993. We do though find a very large economic vote in 1994 which was the first election under the new election laws of 1993". "the economic vote of the chief executive is usually close to zero: Italy, The Netherlands, and U.S. congressional elections". | [BK-D] pp. 62, 64-65 |
| Poland | - | Not in the sample (18 countries, all Western; the 19 series are listed above). See GAP 3. | [BK-D] p. 63 |
| USA (presidential) | 4 (`usp`) | "The median economic voting score for U.S. presidential elections is about -6 percent and falls slightly above the median level of economic voting in our sample. Note though that the variation in U.S. economic voting is significant, rising to as high as -10 percent in 1996 but falling to almost zero percent in the 2000 election study." Table 9.1: divided government -0.06 (0.02) n = 5; unified -0.10 n = 1. | [BK-D] pp. 67, 247 |
| USA (congressional) | 19 (`usc`) | "the median economic vote in U.S. congressional elections ranks as the lowest case of economic voting in our sample". Table 9.1 (US legislative, president's party): divided 0.012 (0.038) n = 9; unified -0.016 (0.03) n = 2. | [BK-D] pp. 67, 247 |

## The texts, verbatim

**[DS08]** Duch, Raymond M. & Stevenson, Randolph T., *The Economic Vote: How Political and Economic Institutions Condition Election
Results*, Cambridge University Press, print 2008/03, online 2010/07, doi 10.1017/CBO9780511755934. Cambridge's book description (also its
`citation_abstract`): "This book proposes a selection model for explaining cross-national variation in economic voting: Rational voters
condition the economic vote on whether incumbents are responsible for economic outcomes, because this is the optimal way to identify and
elect competent economic managers under conditions of uncertainty. This model explores how political and economic institutions alter the
quality of the signal that the previous economy provides about the competence of candidates. The rational economic voter is also attentive
to strategic cues regarding the responsibility of parties for economic outcomes and their electoral competitiveness. Theoretical
propositions are derived, linking variation in economic and political institutions to variability in economic voting. The authors
demonstrate that there is economic voting, and that it varies significantly across political contexts. The data consist of 165 election
studies conducted in 19 different countries over a 20-year time period."

**Chapter pages** (Cambridge serves each chapter's first-page preview; quoted where it bears on the rule):

- **[CH-3]** ch. 3 "Patterns of Retrospective Economic Voting in Western Democracies", pp. 62-93: "The analyses of the 163 surveys
  described in the previous chapter generate a wealth of data: a total of 678 economic vote measures for 113 political parties in eighteen
  countries over a twenty-two–year period."
- **[CH-9]** ch. 9 "The Distribution of Responsibility and the Economic Vote", pp. 252-286: "Two theoretical propositions come out of the
  discussion of administrative responsibility in the last chapter, each of which produces a straightforward hypothesis. The first concerns
  the distribution of the economic vote across parties. Parties with a greater share of the status quo distribution of administrative
  responsibility will receive a greater share of the economic vote than parties with a smaller share. The second concerns the overall size
  of the economic vote across all parties in an election. As the status quo distribution of administrative responsibility over parties
  becomes more equal, the overall economic vote declines. The main task in testing these empirical hypotheses is the measurement of voters'
  beliefs about the share of administrative responsibility that each party holds. A variety of indicators of the status quo distribution of
  policy-making responsibility has been discussed in the literature: the current distribution of cabinet membership, the current
  distribution of cabinet portfolios, the coalition status of the government, the majority status of the government, the influence of the
  opposition on the government, the extent of collective cabinet responsibility, the distribution of legislative seats, the distribution
  of ministries specifically dealing with economic matters, and the role of the president."
- **[CH-III]** Part III "A Contextual Theory of Rational Retrospective Economic Voting: Strategic Voting", pp. 207-208: "We show that in
  political contexts in which policy-making authority is widely shared, the rational voter's ability to extract an informative signal about
  the competence of any one party declines. Chapter 9 explores empirically how variations in the distribution of policy-making
  responsibility among parties currently in government affect the economic vote."
- **[CH-11]** ch. 11 "Conclusion", pp. 337-358: "Our estimates of the economic vote are consistent with the limited comparative evidence
  already available in the literature."
- The other chapter pages (introduction pp. 1-36; ch. 2 pp. 39-61; ch. 4 pp. 94-128; Part II pp. 129-130; ch. 5 pp. 131-147; ch. 6
  pp. 148-177; ch. 7 pp. 178-206; ch. 10 pp. 287-334; preface) carry prose previews with no figure bearing on the rule. Ch. 8
  "Responsibility, Contention, and the Economic Vote" (pp. 209-251), Part I, Part IV, the three appendices (pp. 359-372), contents,
  frontmatter, references, index and the series page all read: "A summary is not available for this content so a preview has been
  provided." All 24 are stored.

**[ES06]** Duch & Stevenson, "Assessing the magnitude of the economic vote over time and across nations", *Electoral Studies* 25(3)
528-547 (Crossref [CR-ES06]). doi.org lands on Elsevier's linkinghub redirect [ES06-LH]; ScienceDirect answers curl with 403; the 2024-04-16
Wayback capture of the ScienceDirect abstract page carries the abstract: "By analyzing a wealth of survey data (163 national surveys) from
19 countries over two decades and by applying a methodology designed to make this evidence comparable, we offer for the first time a
comprehensive map of the extent of economic voting across countries, over time, and for different parties. All told, we estimate voter
preference functions for over 900 political parties. In this essay we analyze these data with the goal of establishing the extent to
which there in fact is an economic vote in developed democracies. We find that the economic vote varies significantly across national
contexts and over time. We also establish that the economy is a significant determinant of vote choice. We situate the median impact of
economic evaluations on the vote probabilities of incumbent PM parties at approximately 5%."

**[ES-D]** The same article's accepted draft as posted by the authors ("February, 2005 / Forthcoming / Electoral Studies"), linked from
their site as "Electoral Studies 2005 Assessing the Magnitude of the Economic Vote over Time and Across Nations" [DS-MAIN]. p. 17: "The
median economic vote effect for this sample of 163 voter preference studies is -4.4. This suggests that in a typical election the
incumbent PM party can expect a 4.4 percent vote lose if overall economic evaluations decline one unit on a standard three-unit economic
assessment scale." p. 19: "The median country in the sample is Portugal with a PM Party economic vote score of just under -5 percent
(again suggesting that a unit deterioration in economic evaluations would reduce the incumbent PM party's vote share by 5 percent)."
p. 21: "Up until 1988 the PM economic vote measure hovered around 8 percent. After 1988, the median PM economic vote magnitude in any
particular year was closer to the 5 percent level." *The draft's -4.4 median and the published abstract's "approximately 5%" differ; the
published figure is the one used in element 2.*

**[JOP10]** Duch & Stevenson, "The Global Economy, Competency, and the Economic Vote", *The Journal of Politics* 72(1) (January 2010)
105-123, doi 10.1017/S0022381609990508 - the publisher's typeset PDF as posted on Duch's site [DS-JOPPOST]; the publisher's page
(journals.uchicago.edu) answers curl with 403 [JOP10-LP]. p. 118: "For each of the 163 voter preference studies in our sample we estimate
the economic vote of the Chief Executive defined as any decrease (increase) in support for the party of the incumbent Chief Executive that
is caused by worsening (improving) economic perceptions." ... "We define a ''meaningful change'' as a change in opinion that results from
moving each respondent's economic perception one unit in the direction of a worsening economy." ... "This gives us estimates of the change
in the Chief Executive party's vote probabilities associated with a unit deterioration in economic perceptions." Abstract, p. 105:
"Finally, the essay demonstrates that open economies, subject to exogenous economic shocks, have a smaller economic vote than countries
with economies less dependent on global trade." *Its per-country values appear only as points in scatter plots (Figures 5 and 6), not in
a table.*

**[BK-D]** Duch & Stevenson, "Voting in Context: How Political and Economic Institutions Condition the Economic Vote", *Draft Book
Manuscript*, Version 1.7 (the site's link text: "Draft Book Manuscript (Apr 07 Update)" [DS-MAIN]), 395 pp. - the book's working title;
the chapter titles match the printed book's. Page numbers are the draft's own running heads ("Duch and Stevenson, Draft Page N").

- p. 48 (method): "Finally, all our estimates of economic voting rely on the same "given change" in economic perceptions. This change is a
  move in each respondent's reported economic perception one unit in the direction of a worsening economy." fn 41: "We always code
  economic perceptions into a three point scale indicating whether the economy got better, stayed the same, or got worse, over the
  previous year."
- pp. 51-52: "We define this concept as any decrease (increase) in support for the party of the incumbent chief executive that is caused by
  worsening (improving) economic perceptions."
- p. 59: "The economic votes of the chief executive range between about -20 percent to about 8 percent with a standard deviation of .04.
  Approximately 90 percent of the chief executive votes have negative values and about 50 percent have values less than -4.5 percent. The
  average economic vote of the chief executive is -5 percent". p. 60: "A moderate decline in perceptions of economic performance typically
  results in a 5 percent drop in support for chief executive parties." And, on Powell & Whitten: "in their analysis of 93 parliamentary
  elections, Powell and Whitten (1993) estimated that the average single party majority government – the equivalent of our chief
  executive party – lost 3.6 percent of the vote." *(A secondary quote; Powell & Whitten's own body is not reachable - GAP 5.)*
- p. 65 (a minority cabinet with outside support): "The 1993 economic vote could also have been influenced by the narrow election victory
  for the González government that forced the Socialists to govern in a minority with the parliamentary support of the Catalan
  nationalists. Again, we will argue in subsequent chapters that as policy making responsibility is shared more equally declines – which
  would be the case with a minority as opposed to majority government – economic voting will decline." *(sic)*
- p. 247, **Table 9.1** "Average Economic Vote for Presidential Parties in Different Contexts" - columns Divided Government or Cohabitation
  (C1) / Unified Government (C2) / p-value for C1 = C2: US Presidential Election "-0.06 (0.02) n = 5" / "-0.10 -- -- n = 1" / "0.29";
  French Legislative Election "0.01 (0.03) n = 4" / "-0.05 (0.03) n = 8" / "0.004"; US Legislative Election "0.012 (0.038) n = 9" /
  "-0.016 (0.03) n = 2" / "0.19".
- p. 249, **Table 9.2** "Average Economic Vote for Presidential and Prime Ministerial Parties" - single-party prime ministers (all
  legislative election studies): divided government "-.052 (0.03) 19", unified "-.068 (0.04) 49". Note: "Numbers in cells are mean
  economic vote of the chief executive, the standard deviation of this variable (in parenthesis), and the number of observations".
  "'Unified Government' for parliamentary systems means the party of the prime minister controls a majority in the legislature."
- pp. 250-251 (France): "in the other two cases (in which the prime ministerial and the presidential parties were the only two to have a
  negative economic vote) the prime ministerial party's share of the economic vote was almost three times larger than that for the
  presidential party."
- p. 253, **Table 9.3** "Economic Voting for Prime Ministerial Parties: The Impact of Coalition and Majority Status", dependent variable
  "Negative of the Economic Vote": Coalition Government "-0.023 (-2.38)" / "-0.026 (-2.31)"; Minority Government "-0.014 (-2.11)" /
  "-0.021 (-2.32)"; Coalition Government * Minority Government "-- --" / "0.017 (1.19)"; Constant "0.067 (9.93)" / "0.069 (7.85)"; 146
  observations; adjusted R-squared 0.08 / 0.09.
- p. 254, **Table 9.4** "Predicted Size of the Economic Vote: Different Types of Parliamentary Cabinets": "Single Party, Majority .067 .069
  50"; "Single Party, Minority .053 .048 19"; "Coalition, Majority .044 .042 66"; "Coalition, Minority .030 .038 11". Note: "The first and
  second numbers in each cell are the predicted sizes of the economic vote for the type of party based on the coefficients in columns 1
  and 2, respectively, of Table 9.3. The last number is the number of cases in the category." And: "these cases are all large parties that
  hold a dominant position on at least one side of the political spectrum in each country. Some are "almost majority" governments, while
  others (like the Scandinavian Socialists) tend to rule when they face a badly divided opposition."
- p. 255 fn 219: "As in the other parts of this book, whenever we refer to "cabinet partners" or a "cabinet partner" we mean non-prime
  ministerial parties who hold seats in the cabinet." p. 256: "Our hypothesis is that the lion's share of the negative economic vote should
  go to the prime ministerial party, followed by other cabinet parties, with the least economic voting going to opposition parties." "the
  economic vote was negative for 80% of the prime ministers and 65% of the cabinet partners. Of course opposition parties also experience
  negative economic voting in some of our cases, but this is much less common (41% of opposition parties)."
- p. 258, **Figure 9.5** "Average Distribution of the Negative Economic Vote: Prime Ministerial, Partner and Opposition Parties" (pie
  charts; note "Economic vote of multiple opposition or partner parties are summed"). The four pies' printed labels, in stream order:
  "16.6% 83.4% Opposition PM PM & Opposition Only (55 cases)"; "25.92% 74.08% Partner PM PM & Partners Only (11 cases)"; "17.25% 31.74% 51%
  Opposition Partner PM PM & Partners & Opposition (18 cases)"; "30.46% 69.54% Opposition Partner Opposition & Partners Only (17 cases)".
  *The pairing of each percentage with the label in the same position is read from that order; it agrees with the text that follows:*
  "whatever the overall size of the negative economic vote in an election, most of it goes to the prime ministerial party - no matter
  whether the opposition, other cabinet partners, or both also get a negative economic vote. Further, this evidence also shows that cabinet
  partners get a bigger share of the economic vote than opposition parties." (p. 259: "even the apparently small difference between the
  shares for opposition and partner parties in the lower left graph can be statistically distinguished from zero, with a p-value of
  .056").
- pp. 259-260: "if we look at all coalition cabinets from 1960 to 2002 (for the countries in our sample), the prime minister's party
  controlled the finance ministry 59% of the time." p. 260 fn 226: "Economic Vote for Parties Holding Finance Ministry in a Coalition
  Government = 0.005 (1.96) + 0.023 (1.74) X Dummy Variable Indicating Party Holds Prime Ministry + 0.015 (2.84) X Dummy Variable
  Indicating Party Holds Finance Ministry - 0.0004 (-0.03) Prime Ministry*Finance Ministry." p. 261, **Table 9.5** "Predicted Size of the
  Economic Vote: Parties Holding the Finance Ministry, the Prime Ministry, or Both": "Not PM, Not Finance Ministry .005 (.0001, .011)";
  "PM, Not Finance Ministry .028 (.002, .053)"; "Not PM, Finance Ministry .020 (.012, .03)"; "PM, Finance Ministry .043 (.024, .061)". And:
  "when different parties hold these two positions the electoral fate of the finance minister is almost as dependent on the economy (an
  economic vote of .02) as is the electoral fortunes of the prime minister (an economic vote of .028)."
- p. 264, **Figure 9.7** "Economic Vote and Share of Cabinet Seats, Cabinet Partner Parties Only": "Economic Vote for Cabinet Partners =
  0.007 (1.25) - 0.057 (2.42) X % of Cabinet Seats Held. R2 =.10. Observations=84." p. 263 fn 229: "if we include the PM parties in the plot and
  regression the relationship is much stronger."
- p. 266, **Figure 9.8**: "Economic Vote of Chief Executive = - 0.002 (0.08)-0.066 (2.81)*Concentration of Responsibility. R2 =.10.
  Observations=152."
- pp. 267-269 (the opposition-influence weight): "we seek to develop a measure akin to the concentration of authority measure used above,
  but one that weights opposition parties more heavily in situations of single-party minority or coalition minority government and
  institutionally powerful oppositions." Table 9.7 "Classification of Institutional Strength of the Opposition": High "Belgium, Denmark,
  Germany, Italy, Netherlands, Norway, Spain, Sweden"; Medium "Canada, United States"; Low "Australia, France, Greece, Ireland, New Zealand,
  United Kingdom" ("From Powell (2000) with one exception" - Italy moved from Medium to High). fn 237: "Institutional strength of
  opposition was coded 0 for Low, 1 for Medium, and 2 for High. Majority Status of the government was coded 0 for majority, 1 for
  single-party minority, and 2 for coalition minority. The opposition influence weight was calculates as follows: (institutional strength
  of opposition score + majority status score)/4." p. 269: "if c_i is party i's cabinet seat share, s_i is its legislative seat share, and
  [w] is the opposition influence weight for the case, then the party's share of administrative responsibility is (1- [w])c_i + [w] s_i.
  This measure will sum to one across all the parties". *(The weight's Greek letter is an unmapped glyph in the PDF; written [w].)*
- pp. 270-271, **Figure 9.9**: "Economic Vote of Chief Executive = -0.015 (1.18) -0.057 (3.40) X Concentration of Responsibility with
  Opposition Influence. R2 =0.10. Observations=137." "the impact of an empowered opposition on the distribution of administrative
  responsibility is to make it more equal and that this depresses the magnitude of economic voting in the system. Overall, however, this
  impact is quite small compared to the impact of the distribution of responsibility that stems from the role that parties play in
  cabinet."
- p. 273 (summary): "prime ministers leading single party majority cabinets have a larger share of the economic vote than prime ministers
  leading coalition or minority cabinets; in coalition cabinets, prime ministerial parties receive the lion's share of the overall economic
  vote and their cabinet partners most of the rest; parties that control the finance ministry have a larger economic vote than parties who
  do not, and the size of this effects is almost as large as the impact of holding the prime ministry; and that a governing party's
  economic vote is closely tied to its share of cabinet seats."
- p. 275, **Appendix 9.1** "Influence of the Opposition weight ..." (count of cases at weight 0 / 0.25 / 0.5 / 0.75 / 1): "France 9 2 1 0
  0"; "Germany 0 0 12 0 0"; "Italy 0 0 6 0 0"; "Sweden 0 0 0 4 0" (the USA is not listed: "For the U.S. case, the presidential party is
  coded as holding all administrative responsibility and only presidential election studies are used", p. 266 fn 233).

**[PA05]** Duch & Stevenson, "Context and the Economic Vote: A Multilevel Analysis", *Political Analysis* (2005) 13:387-409, doi
10.1093/pan/mpi028 - the publisher's typeset PDF as posted on Duch's site. Abstract: "We test one hypothesis: As policy-making
responsibility is shared more equally among parties, economic evaluations will be more important in the vote decision." *(sic - the
same wording recurs on p. 390 and again in section 3, yet p. 390's own reasoning just before it reads "the reason for this smaller economic vote is not
that voters cannot attribute responsibility over parties, but that a more equal distribution of responsibility weakens the signal that
the previous economy provides about the competence of the incumbent parties", and every other text here finds shared responsibility makes
the economy LESS important; quoted as printed.)* p. 405, Table 1 "Concentration of authority and economic voting for the PM (two-stage method)":
Concentration of authority ".081 (0.023)" (All Cabinets) / ".075 (.033)" (Coalition Cabinets); observations 152 / 76; note: "The
dependent variable is economic vote for chief executive (the party of the prime minister in all cases except the United States, where it
is vote for the president)." (The PDF's minus signs are unmapped glyphs, so the table's signs are not recoverable from the text.)

**[PW93]** Powell, G. Bingham & Whitten, Guy D., "A Cross-National Analysis of Economic Voting: Taking Account of the Political
Context", *American Journal of Political Science* 37(2) (May 1993) 391- (Crossref [CR-PW93]). doi.org → JSTOR's live "Client Challenge"
page [PW93-JS]; the 2016-06-23 Wayback capture carries the abstract: "A large literature has demonstrated that such economic factors as
growth, inflation, and unemployment affect the popularity of incumbents within many democratic countries. However, cross-national
aggregate analyses of "economic voting" show only weak and inconsistent economic effects. We argue for the systematic incorporation of
political factors that shape the electoral consequences of economic performance. Multivariate analyses of 102 elections in 19
industrialized democracies are used to estimate the cross-national impact of economic and political factors. The analyses show that
considerations of the ideological image of the government, its electoral base, and the clarity of its political responsibility are
essential to understanding the effects of economic conditions on voting for or against incumbents."

**[DPS-R]** Duch, Przepiorka & Stevenson, "Responsibility Attribution for Collective Decision Makers", manuscript dated "December 7,
2013" (Duch's site, file `dps_ajps_2013_12_06_revision.pdf`), abstract: "Our results show that recipients punish unfair allocations and
mainly target the decision maker with proposal power and with the largest vote share. We find rather weak evidence that decision makers
with veto power are targeted or that recipients engage in punishment proportional to weighted voting power." *(A laboratory study, not an
electoral estimate; it supports element 5's PM-first ordering, not a support-party share.)*

**[D01]** Duch, Raymond M., "A Developmental Model of Heterogeneous Economic Voting in New Democracies", *American Political Science
Review* 95(4) (2001/12) 895-910, doi 10.1017/S0003055400400080 (Crossref search [CR-D01Q]). Cambridge abstract: "Economic voting develops
in postcommunist electorates as ambiguity regarding the link between government policy and economic outcomes declines. ... Economic voting
increases as these levels of information on, and trust in, government rise. The analysis that tests these propositions is based on a
public opinion survey conducted in Hungary in 1997. The test is replicated with a 1997 Polish election survey." The author-posted PDF
[D01-PDF] is a page scan with no text layer - its Polish figures cannot be read without OCR (GAP 3).

## Register of ids

| id | URL | publisher | page date | file (under `raw/economic_vote/`) | bytes | sha256 |
|---|---|---|---|---|---|---|
| [DS08] | https://doi.org/10.1017/CBO9780511755934 → https://www.cambridge.org/core/books/economic-vote/57D49941B6465119EA9CA9D2D8518903 | Cambridge University Press | 2008/03 (online 2010/07) | `landing_book_10_1017_CBO9780511755934.html` | 992536 | 4533d64fc008aa0d866b9c7b458b6d30747e81977a5f93b62dd25b8aa19e6412 |
| [CR-DS08] | https://api.crossref.org/works/10.1017/CBO9780511755934 | Crossref | 2008 | `crossref_10_1017_CBO9780511755934.json` | 2978 | 019bd29d7fd6893c9f8f32e135aea996b704eb3dec554d08cfae1c44eed3b712 |
| [CH-1] | https://www.cambridge.org/core/books/economic-vote/introduction/C7A1CEB616CAF117A15DEFAD805CCA73 | CUP | pp. 1-36 | `chapter_introduction.html` | 726806 | 33fac3b6229cbf602049a4654fd698776c77326510176477174118d16fad7d11 |
| [CH-I] | https://www.cambridge.org/core/books/economic-vote/describing-the-economic-vote-in-western-democracies/B5258DDEAB41603541CD4AE8F47237EE | CUP | pp. 37-38 | `chapter_describing-the-economic-vote-in-western-democracies.html` | 724739 | 5d5f44faec5c5e23b76064cbb563563dc3dff3347bf4e0023f506218847dfd94 |
| [CH-2] | https://www.cambridge.org/core/books/economic-vote/defining-and-measuring-the-economic-vote/58A9E73E078F647FD4CCE129C81E641A | CUP | pp. 39-61 | `chapter_defining-and-measuring-the-economic-vote.html` | 730315 | 5b0ede3edd3e254c808276a1d0dd1769e48c11de120d1ad18afc9c8e71a437d2 |
| [CH-3] | https://www.cambridge.org/core/books/economic-vote/patterns-of-retrospective-economic-voting-in-western-democracies/1B20B15F5CC27F6AA873A65C6D4C70A2 | CUP | pp. 62-93 | `chapter_patterns-of-retrospective-economic-voting-in-western-democracies.html` | 730493 | eaa218d22c4c3d5224cba0006287f63e600d5c0185d77a74a414b5127fbfe9ae |
| [CH-4] | https://www.cambridge.org/core/books/economic-vote/estimation-measurement-and-specification/DD1F243D003E0A264265F4AC686D4952 | CUP | pp. 94-128 | `chapter_estimation-measurement-and-specification.html` | 730156 | 34fb4d90402f6a3694b1f1770d8a8ee97b5a1b859fcd0f8eefabb8d03e019b73 |
| [CH-II] | https://www.cambridge.org/core/books/economic-vote/contextual-theory-of-rational-retrospective-economic-voting-competency-signals/64359BA60C7BD59CC1BD5271769871D2 | CUP | pp. 129-130 | `chapter_contextual-theory-of-rational-retrospective-economic-voting-competency-signals.html` | 728272 | ef506f71c3c56746f71dc6cef787eea705defc52ccbc2b4c04e4e27bf9efa0df |
| [CH-5] | https://www.cambridge.org/core/books/economic-vote/competency-signals-and-rational-retrospective-economic-voting/475FE577D9C719979F597301DFB5605E | CUP | pp. 131-147 | `chapter_competency-signals-and-rational-retrospective-economic-voting.html` | 730245 | 623d3a5bef4aae6df9c9d307c60f0b1523972b326722fdc1635fd6a04d7ca206 |
| [CH-6] | https://www.cambridge.org/core/books/economic-vote/what-do-voters-know-about-economic-variation-and-its-sources/2F15F3C6A68DDDF6FAF44B985D0579B0 | CUP | pp. 148-177 | `chapter_what-do-voters-know-about-economic-variation-and-its-sources.html` | 729946 | 3f66355d352339a18de6c89921515fc7f04dd1ef9cc5324ae1070be205ad2351 |
| [CH-7] | https://www.cambridge.org/core/books/economic-vote/political-control-of-the-economy/A6EF4D7142313F90BEBDBB40ADCEF91F | CUP | pp. 178-206 | `chapter_political-control-of-the-economy.html` | 729431 | f31c9f41c536c26d782dd8dd33005b8591eb6b03ad7ebd503ae739fa815748a5 |
| [CH-III] | https://www.cambridge.org/core/books/economic-vote/contextual-theory-of-rational-retrospective-economic-voting-strategic-voting/5C9D4E889294CD8ADF5DB2944CCBB335 | CUP | pp. 207-208 | `chapter_contextual-theory-of-rational-retrospective-economic-voting-strategic-voting.html` | 728485 | 5cd67d375fd1106f929c4b5e5e9675968838965df57b22efb658dd94aeb6b1b4 |
| [CH-8] | https://www.cambridge.org/core/books/economic-vote/responsibility-contention-and-the-economic-vote/9F9F58AC4BD9BB8308D7C6EBB4AE21F2 | CUP (no summary) | pp. 209-251 | `chapter_responsibility-contention-and-the-economic-vote.html` | 727466 | f71b9911b1a1b894af21d9d109e5a72b3a68debca1ab354b49d935eccd34c10f |
| [CH-9] | https://www.cambridge.org/core/books/economic-vote/distribution-of-responsibility-and-the-economic-vote/7DE191954DA5C39D88ED45EF33B32297 | CUP | pp. 252-286 | `chapter_distribution-of-responsibility-and-the-economic-vote.html` | 730279 | 37f48963f23b2950079397e1155c559b3b8b4d909376f6d827a8b7d7ce8c2f53 |
| [CH-10] | https://www.cambridge.org/core/books/economic-vote/pattern-of-contention-and-the-economic-vote/8585E2E856062113F94DF10815EA24B3 | CUP | pp. 287-334 | `chapter_pattern-of-contention-and-the-economic-vote.html` | 729744 | 4eb048cc64dcea7bb10c74c663df95bdf3132b2b48ed4b3cafdc0d8ca406061d |
| [CH-IV] | https://www.cambridge.org/core/books/economic-vote/conclusion-and-summary/BFF40433F23D9F26023FAE5071312E72 | CUP (no summary) | pp. 335-336 | `chapter_conclusion-and-summary.html` | 723967 | e42ff2f8210f94ffff649a1b5e5091b5d77d65ab62c2fb75dca2c64b0de55aa2 |
| [CH-11] | https://www.cambridge.org/core/books/economic-vote/conclusion/7DFF46FE4574C4154B48ECE867A9D2E2 | CUP | pp. 337-358 | `chapter_conclusion.html` | 727380 | 66e06699a13b1af610f1fd5af2cadf12901f273c9fc60d6239f636986794d2c0 |
| [CH-AA] | https://www.cambridge.org/core/books/economic-vote/appendix-a/62952F31C34F91F25B3B505E7C56F0FE | CUP (no summary) | pp. 359-366 | `chapter_appendix-a.html` | 724121 | f3a8a6cb2b53f03227a33576261404cc50cb984c8f1278a5da35dd68a97e45bf |
| [CH-AB] | https://www.cambridge.org/core/books/economic-vote/appendix-b/A9B9FA8D41A0FC68D5729051DD35CA8C | CUP (no summary) | pp. 367-369 | `chapter_appendix-b.html` | 724121 | d7501eda4fbaf08a00553c755300bdc0c4161923cc5f1b4f6772dce07bfed1b0 |
| [CH-AC] | https://www.cambridge.org/core/books/economic-vote/appendix-c/52778EC37560B5EF8BEB56D48E1619A1 | CUP (no summary) | pp. 370-372 | `chapter_appendix-c.html` | 724122 | 4cd92d0dc0bcff3206781ec969709b6b0cb884c3f77657c85e24f267da6bfabf |
| [CH-REF] | https://www.cambridge.org/core/books/economic-vote/references/A852FF6E90D4A23A29F845BF6BB2CF52 | CUP (no summary) | pp. 373-390 | `chapter_references.html` | 1730462 | 53d926403a12d0665dd05df5a3abebac19b633718e70b82304a1a3c3650e000c |
| [CH-IDX] | https://www.cambridge.org/core/books/economic-vote/index/C0920728B47E77B48DF7B014B9BA271D | CUP (no summary) | pp. 391-399 | `chapter_index.html` | 723451 | 25e67813ca4c424707f3df2370426855a38a212636f92439733273fd0d758946 |
| [CH-SER] | https://www.cambridge.org/core/books/economic-vote/cambridge-cultural-social-studies/4C52E9B5DDCE78735C7D9A648B39C3FB | CUP (no summary) | pp. 400-402 | `chapter_cambridge-cultural-social-studies.html` | 724719 | d076822b723760a447f777c3fdf4cbbdee10d80c6d7a7c5f64688f732709d0f3 |
| [CH-PRE] | https://www.cambridge.org/core/books/economic-vote/preface/589EE0748FE62A00D36DE8C12F801C9F | CUP | pp. xi-xiv | `chapter_preface.html` | 726385 | 0ab583d1e8e01e1d922604de0df51c7eb34ad86d7d5b07a93ddac42e42eef702 |
| [CH-TOC] | https://www.cambridge.org/core/books/economic-vote/contents/A80C9937C488381AB895DA1862E29F89 | CUP (no summary) | pp. ix-x | `chapter_contents.html` | 723505 | 6069402612d5323fcc7fba342b7b778e4c8e2eaa9a5bbc8e1fca5e18db70f802 |
| [CH-FM] | https://www.cambridge.org/core/books/economic-vote/frontmatter/C32E6D4FA1060AC1D6DFFE5C60247A7D | CUP (no summary) | pp. i-viii | `chapter_frontmatter.html` | 723600 | 45d1c8f8bb15a54341eab96b7a161283c515eaccf8b2f2199768796af6883f30 |
| [BK-D] | https://web.archive.org/web/20070613222321id_/http://www.raymondduch.com/economicvoting/duchstevensonbook_v1_7_final.pdf | authors' site (Wayback 2007-06-13) | draft v1.7, Apr 2007 | `wayback_raymondduch_duchstevensonbook_v1_7_final.pdf` | 2249972 | b5c99f8c522ff9d636352bc0af2681125ba66c408f061c7a8f777151b75a3898 |
| [DS-MAIN] | https://web.archive.org/web/20070613222248id_/http://www.raymondduch.com/economicvoting/mainpage.htm | authors' project page (Wayback 2007-06-13) | - | `wayback_raymondduch_economicvoting_mainpage.htm` | 10922 | 08f9d27f24b4be8c710daaf90c45da6981fedf9ab2e66701530a10a4ab124007 |
| [ES06] | https://web.archive.org/web/20240416104924id_/https://www.sciencedirect.com/science/article/abs/pii/S0261379405000636 | Elsevier / ScienceDirect (Wayback 2024-04-16) | 2006 | `wayback_2024_sciencedirect_S0261379405000636.html` | 258898 | 0e5c79ac246861c025925c10ca242cfdb22b7226ebdc6a1962be0a34468582ce |
| [ES06-LH] | https://doi.org/10.1016/j.electstud.2005.06.016 → https://linkinghub.elsevier.com/retrieve/pii/S0261379405000636 | Elsevier redirect page (no abstract) | - | `landing_10_1016_j_electstud_2005_06_016.html` | 2690 | b1adf15aecfb7335d46735afde908b880acd763d0c712e45ee5c8d638b0174a3 |
| [CR-ES06] | https://api.crossref.org/works/10.1016/j.electstud.2005.06.016 | Crossref | 2006 | `crossref_10_1016_j_electstud_2005_06_016.json` | 10390 | d038307a844e3bd56a98e2aff61022dd667bff63af630fc83140f11b3fb99bba |
| [ES-D] | https://web.archive.org/web/20070613222400id_/http://www.raymondduch.com/economicvoting/articles/ecpr03special%20editionjan72005.pdf | authors' site (Wayback 2007-06-13) | draft Feb 2005 | `wayback_raymondduch_ecpr03special_editionjan72005.pdf` | 104826 | cf17495c715066d757398672c0221ef9d1e587832028aee7a2d84daecdefdd1b |
| [JOP10] | https://web.archive.org/web/20180721223342id_/http://www.raymondduch.com/wp-content/uploads/2013/09/duch_JOP2010.pdf | JoP typeset PDF on authors' site (Wayback 2018-07-21) | Jan 2010 | `wayback_raymondduch_duch_JOP2010.pdf` | 408420 | 137999667a69593d426b2bf8836d550997e1b596c547e26f06faf14233dfc627 |
| [DS-JOPPOST] | https://web.archive.org/web/20150301024637id_/http://www.raymondduch.com/2010/09/08/the-global-economy-competency-and-the-economic-vote/ | authors' site post linking [JOP10] (Wayback 2015-03-01) | - | `wayback_raymondduch_jop2010_post.html` | 44041 | 1f8d6328f6d7e888bf0de339107a457bf2f7731544e96cd579b466c987786c1d |
| [JOP10-LP] | https://doi.org/10.1017/S0022381609990508 → https://www.journals.uchicago.edu/doi/10.1017/S0022381609990508 | University of Chicago Press: HTTP 403 page | - | `landing_10_1017_S0022381609990508.html` | 5566 | 46555826f1b418f8d14eeea893e479646b2da64e49ae25bae2fe56a40b64ca1c |
| [CR-JOP10] | https://api.crossref.org/works/10.1017/S0022381609990508 | Crossref | 2010 | `crossref_10_1017_S0022381609990508.json` | 4453 | 5ecb32ae9aa2be75cb262e6519de14f7e5aa972f2a7243b564c24bb755c42311 |
| [PA05] | https://web.archive.org/web/20170808050104id_/http://www.raymondduch.com/wp-content/uploads/2013/09/political_analysis_2005.pdf | Political Analysis typeset PDF on authors' site (Wayback 2017-08-08) | 2005 | `wayback_raymondduch_political_analysis_2005.pdf` | 272158 | fbdf070b2b956323e197b988b88660de8c691b160486eb21738de83b300a0d29 |
| [PW93] | https://web.archive.org/web/20160623113501id_/http://www.jstor.org/stable/2111378 | JSTOR (Wayback 2016-06-23) | May 1993 | `wayback_2016_jstor_2111378.html` | 79277 | 9764d696f3024c0828a270e7055eadef2a472dc5e35c6181ff86b16c678f4eb1 |
| [PW93-JS] | https://doi.org/10.2307/2111378 → https://www.jstor.org/stable/2111378?origin=crossref | JSTOR live: "Client Challenge" page, no content | - | `landing_10_2307_2111378.html` | 3038 | 32ed63159c77e21ee19ca1b9aa3213ccf0218eb59539560b132a8e68ef0e18ea |
| [CR-PW93] | https://api.crossref.org/works/10.2307/2111378 | Crossref | 1993 | `crossref_10_2307_2111378.json` | 1699 | 3fcb4b520969a26634a1217f1b7a6fb611fbd3a60fdc6c3d30458aa346b729ac |
| [DPS-R] | https://web.archive.org/web/20160213043338id_/http://www.raymondduch.com:80/wp-content/uploads/2014/01/dps_ajps_2013_12_06_revision.pdf | authors' site (Wayback 2016-02-13) | ms. 2013-12-07 | `wayback_raymondduch_dps_ajps_2013_12_06_revision.pdf` | 328014 | 192db5266434158801cff1abe1b316538c66bf6b9d59282ee31ca0d8f35e4e3a |
| [D01] | https://doi.org/10.1017/S0003055400400080 → https://www.cambridge.org/core/journals/american-political-science-review/article/abs/developmental-model-of-heterogeneous-economic-voting-in-new-democracies/462098284C1E638CBFACEB63DEB241C8 | Cambridge University Press | 2001/12 | `landing_10_1017_S0003055400400080.html` | 957825 | 79ab8d53e23a846c40df77c327f03434c35167be2fae1e87755a8f801194946e |
| [CR-D01Q] | https://api.crossref.org/works?query.bibliographic=A+Developmental+Model+of+Heterogeneous+Economic+Voting+in+New+Democracies+Duch&rows=3&select=DOI,title,author,container-title,volume,issue,page,issued | Crossref search (first hit 10.1017/s0003055400400080) | - | `crossref_query_duch_2001_apsr.json` | 1324 | d1bd94d339a0d798f5afe569920ebaee1a118cf897422c7c7d218840d5be0571 |
| [D01-PDF] | https://web.archive.org/web/20070923055142id_/http://www.raymondduch.com/economicvoting/articles/duch_apsr_2001.pdf | authors' site (Wayback 2007-09-23); image scan, no text layer | 2001 | `wayback_raymondduch_duch_apsr_2001.pdf` | 3521133 | 78eef2d45d0d945d03f6a4ef808a72db3be99157c6db6444b13ec466f966f91b |

All fetched 2026-09-25, HTTP 200 unless the row says otherwise, stored unaltered; `raw/economic_vote/SHA256SUMS` lists all 44 files (a
new folder).

## GAPS

1. **The printed book's tables.** Cambridge Core serves only the description and first-page previews; every table and figure above
   (Tables 9.1-9.5 and 9.7, Figures 9.5-9.9, Appendix 9.1, the Figure 3.3 order) comes from the authors' April 2007 draft v1.7 [BK-D]. The
   printed book is known to differ in at least its party count (113 printed vs 105 in the draft, [CH-3] vs [BK-D] p. 58) and its chapter
   pagination (ch. 9 printed pp. 252-286, draft pp. 241-273). The printed values are NOT reachable; the draft values are billed as draft.
2. **A numeric economic vote per country.** No reachable text tabulates the chief executive's economic vote by country or country-year:
   the draft book, the Electoral Studies draft and JoP 2010 show them only as box-plots and scatter points (the book draft's Figures 3.2
   and 3.3; [ES-D] Figure 2; [JOP10] Figures 5-6). The project site's "First Stage Estimation" country pages (e.g.
   `firststage/countrypages/sweden.htm`, 2007 captures) were inspected and hold survey-variable codings, not estimates - not stored. What is
   quotable is the ORDER (Sweden 3rd of 19) and the verbal magnitudes in the country table above. **Sweden has no quoted number at all.** The
   box-plot medians could be measured from the draft PDF's vector drawing (Figure 3.3, p. 68), but that is a measurement of a figure, not a
   quote; not done here, and not to be done without a ruling.
3. **Poland.** Not in Duch & Stevenson's sample (18 Western countries). The only Duch text on Poland is [D01] (a 1997 Polish election
   survey replicating a Hungarian test); its author-posted PDF is a page scan with no text layer, and there is no OCR in this environment.
   Its abstract gives the direction (economic voting rises with information and trust in new democracies), no magnitude.
4. **Support parties (confidence and supply).** No fetched source estimates the economic vote of a party that supports a minority cabinet
   from outside. Duch & Stevenson define "cabinet partners" as parties holding cabinet seats [BK-D] p. 255 fn 219, so support parties sit in
   their "opposition" group (16.6 % of the negative economic vote where only the PM and the opposition lose it; 17.25 % where PM, partners
   and opposition all do), pooled with every other non-cabinet party and counted only when their own economic vote is negative ("Of course,
   here we mean only those opposition parties that experience a "negative economic vote" when the perceptions worsen. Other opposition
   parties may (and usually do) have large "positive economic votes"", p. 265 fn 231). The nearest sourced handle is the
   opposition-influence weight (element 8b), which moves responsibility toward legislative seat shares under minority government - a
   responsibility share, not an estimated economic vote. The Spanish 1993 minority "with the parliamentary support of the Catalan
   nationalists" is discussed (p. 65) but no Catalan economic vote is given. Any support-party share the model uses is `[AUTHORED-DRAFT]`.
5. **Powell & Whitten's body.** JSTOR's live page is a "Client Challenge"; the 2016 capture carries only the abstract. Their figures (e.g.
   the 3.6 % loss) are quoted only as Duch & Stevenson report them ([BK-D] p. 60).
6. **The Electoral Studies 2006 body.** ScienceDirect refuses curl (403); only the abstract is reachable, from the 2024 Wayback capture.
   The accepted draft [ES-D] has the tables' prose but its median (-4.4) differs from the published abstract's "approximately 5%".
7. **The unit asked for vs the unit found.** The task anticipated a one-standard-deviation effect; every source measures the effect of
   "moving each respondent's economic perception one unit in the direction of a worsening economy" on a three-point scale ([JOP10] p. 118,
   [BK-D] p. 48 fn 41). The model's perception variable must be mapped onto that three-category move - how is not sourced here.
8. **PA05's signs and abstract.** The PDF's minus glyphs are unmapped, so Table 1's signs are not recoverable from the text; and the
   abstract's "economic evaluations will be more important" as responsibility is shared reads opposite to every other source (quoted as
   printed, flagged, not used for direction).
