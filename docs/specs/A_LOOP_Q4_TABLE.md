# A-LOOP q4 - the absent campaign actions, each with its sourced effect or none

**For Elias's ruling. No code follows until he rules.** This is the one table `docs/specs/A_LOOP_PLAN.md` §7 ("q4 - how the list of absent campaign actions with a sourced effect is made") says is put to him after 5a. It was made by §7's fixed method on `wave/q4` from main 7cfae549 (after §881, 5a, and §882, q5). Nothing in the game or in `ELECTIONS_CAMPAIGN_SPEC.md` is changed here: the spec migration at the end is a PROPOSED patch, applied only once he rules.

**The rule** (§7.3, verbatim in substance):
- An action with a sourced effect on vote choice or turnout is built on 5a's path (persuasion) or 5b's (turnout).
- So is a resource that sourced actions consume (money, volunteers, hours): fundraising buys contacts, never votes.
- An action with none is CUT from the spec: its text moves to the spec's own "cut by q4" section with the reason, nothing deleted unmigrated.

**Readings this table makes, stated so they can be overruled:**
- **R1. A sourced null is "none".** Where the fixed sources measure an effect and find it zero (reminders, debates, candidate visits), the row is CUT, the measured zero quoted.
- **R2. A choice that only steers a sourced action is built as a parameter of it**, with the action's own class CACE unchanged: the choice moves only *where* the effect lands (5a's q_p, the chain's relevance × credibility), never its size. Topic, targeting, outlet, rally size and TV frequency are of this kind.
- **R3. Information is not in §7's rule.** Q7 (J1) kept the run-up POLL as information ("its effect is a measurement, not votes"). Rows whose only effect is information are marked **INFO - Elias decides** rather than built or cut by this method.
- **R4. Order of search** (§7.2): Kalla & Broockman by mode → Green & Gerber by tactic → Gerber et al. for broadcast → one named search per remaining action. A row cites the first source in that order that bears on it, then any named-search source.

## 1. The table

Contact classes are 5a's: **personal** (rally, town hall, doors; Kalla & Broockman also put phone calls here), **impersonal** (TV, digital, post, social), **earned** (interview, announcement). "Code" says what was verified in the tree at 7cfae549 (file:line).

| # | Action (spec §) | Code today (verified) | Class | Sourced effect: estimate [95 % CI], outcome, timing | Source · digest | Proposed |
|---|---|---|---|---|---|---|
| 1 | **FUNDRAISE** - "Raise money" (§3, §9) | refused: `PreCampaignRun.Refusal` "no donor model exists to raise from" (PreCampaignRun.cs:67); the HQ draws the chip dimmed (GameController.Campaign.cs:818-831) | resource (money) | **Money, not votes.** A candidate's letter to partisans: about 0.8 % of recipients gave (1.3 % District A, 0.5 % District B); revenue per solicitation $0.401 (positive letter) and $0.305 (negative), tone no difference (p = 0.59); a letter raised the giving rate by 0.9 pp (positive) and 0.7 pp (negative) against no letter. US local general elections, 2010. | [BCP12] Table 4, pp. 19-21 · `2380b00b…` (the 2012 working paper; the 2016 JEBO article BILLED) | **BUILD as resource**: a fundraising letter costs a mailing and returns its revenue per solicitation into the chest; it buys contacts, never votes. The transfer to SE/DE is [AUTHORED-DRAFT] (calibration list) |
| 2 | **DEVELOP POLICY** - "Develop policies" (§3) | refused: "no price and no seam in the run" (PreCampaignRun.cs:68) | earned | **None on vote choice.** "voters do not systematically adjust their perceptions of parties' positions in response to shifts in parties' policy statements during election campaigns ... no evidence that voters adjust their Left-Right positions or their partisan loyalties in response" (pooled European election surveys). Abstract only; the full text BILLED. | [AES11] Essex record abstract · `9e40e632…` | **CUT** (the policy itself already acts through compatibility; publishing one in the campaign is the built POLICY action) |
| 3 | **TRAIN CANDIDATE** - "Train candidates" (§3) | refused: "no price in the model - a figure would be invented" (PreCampaignRun.cs:69) | - | **None found.** Named search (candidate training, field experiment, vote share): no study measuring a vote or turnout effect; the one field experiment the search surfaced measures political ambition, not votes. Nothing fetched. | named search, no source | **CUT** |
| 4 | **SET / CHANGE STRATEGY** - "Select campaign strategy", "Change campaign strategy" (§3, §11) | run-up refused: "no seam in the run" (PreCampaignRun.cs:70); not offered in the campaign (GameController.Campaign.cs:875-879); the AI's five sets are [AUTHORED-DRAFT] modifiers (CampaignStrategy.cs) | composite | **None of its own.** A strategy is a bundle of choices each sourced (or not) in its own row: Base Mobilization = 5b's allocation (row 14); Swing Voter = targeting (row 10); Negative = attack (row 6); Populist = issue focus (row 9); Broad Appeal = nothing. Message-strategy search: the negative-campaign meta-analysis [LSR07] is BILLED (403); [BCP12] Table 6 is the one stored tone estimate (row 6). | (rows 6, 9, 10, 14) · [LSR07] BILLED | **CUT** as its own modifier; its sourced parts are the rows named. Ties to §881's open ask (NegativeCampaign wins all 30 cells) |
| 5 | **OFFICE** + **office siting** (§3, §10) | refused until 5b: "an office's doors reach no count until turnout is counted on polling day" (PreCampaignRun.cs:71); auto-sited to the largest region without one (`NextOfficeRegion`, PreCampaignRun.cs:182-191; SimulationManager.cs:5595); no player siting | resource (volunteers' hours → doors) | **Through its doors (5b):** door-to-door, control-group turnout 70 % and higher: CACE 0.6 [-1.5, 2.8] turnout points (Table 4). Cross-check, observational and not added on top: "a field office in a county increases turnout by 0.4%, and Democratic vote share by approximately 1%" (US 2004-2012). | [GG14] Table 4 · `5c809520…`; [DL14] LSE blog by the authors · `acada52b…` and Crossref abstract · `e9ad25b2…` (APR article BILLED, 403); [MAS09] BILLED (403) | **BUILD on 5b** (Q7 already rules it out until 5b counts its contacts); siting = where its doors go |
| 6 | **ATTACK OPPONENTS** (§3, §11 Negative, §12 post "Attack") | no player verb: `QueuedDecisionRecord.ToDecision` passes no target party (PlayerCampaignRecord.cs:109-116, `AiDecision(..., againstParty = -1)`); the AI attacks (CampaignRun.cs:863-873) | any | **No vote-choice estimate in hand.** Kalla & Broockman pool persuasion contacts without a tone split. The one stored tone estimate is on turnout among the sender's own partisans: negative letter vs no letter +0.3 pp (SE 2.3; Table 6 col. 5, "0.003 (0.023)"), positive letter -4.4 pp (SE 2.4); negative vs positive +4.7 pp (one-sided p = 0.008). Local US general elections 2010. | [BCP12] Table 6, pp. 21-22 · `2380b00b…`; [LSR07] BILLED (403) | **CUT** (provisional: [LSR07] could reverse it). See ask A2: 5a's AI attacks use the class CACE against the target |
| 7 | **DEFEND AGAINST ATTACKS** (§3, §15) | no player verb; the AI answers attacks (W-C2, CampaignRun.cs:709-772) | earned | **None found** in the fixed order or the message-strategy search. | - | **CUT** |
| 8 | **DEBATE CHOICES** - topics, attack/defend, tone, in-debate moves (§3, §15) | no player input: the run plans each side's moves from its personality (`DebatePlanFor`, CampaignRun.cs:1114-1121; called at :951-953) | earned | **None on vote choice.** 56 TV debates, 62 elections, 10 countries: debates "decrease individual vote choice consistency by a non-significant 0.7 percentage point in the three following days ... we can reject any impact of debates on vote choice consistency higher than 0.5 percentage point at the 5% level"; on the distance to final vote shares "positive, small (0.1 percentage point), and not statistically significant". Event study on survey waves, not a field experiment. | [LPP22] PDF pp. 42-45 · `4893c412…`; Crossref record of the QJE article · `eadac440…` | **CUT** (the player's debate choices; see ask A3 on the AI debates' shocks) |
| 9 | **MESSAGE TOPIC / RESPOND TO EVENTS** - rally "Topic", TV "Message", §3 "Respond to events" | the HQ queues every action with no issue (`QueueCampaignDecision(..., null, ...)`, GameController.Campaign.cs:890); the record carries an Issue field (PlayerCampaignRecord.cs:103) | the action's own | **The action's own class CACE, unchanged** (R2): personal 0.58 [-0.50, 1.66], impersonal 0.28 [-0.27, 0.83] vote-share points, contact within 2 months of election day; earned 0.125 (DERIVED, §881). The topic moves only q_p (relevance), which since §882 reads the event-moved salience. | [KB18] Fig. 4(a), 4(b), p. 160 · `19912a3a…` | **BUILD on 5a** as a parameter of the action |
| 10 | **TARGETING** - TV region/demographic, digital targeting (§12), "Identify voter groups" as a target (§3) | the player's target is region or national only; group is always -1 (`ActionTarget`, PlayerCampaignRecord.cs:111-112) | the action's own | **The class CACE, unchanged** (R2). Kalla & Broockman's only significant close-to-election rows come from individually pre-identified targets: a canvass "carefully targeted based on the results of the prior experiments" (OH DID, President) 2.75 [0.30, 5.20]; mail to voters identified pro-choice in a pre-survey (Rogers & Nickerson) 3.90 [1.16, 6.64] (appendix B.18: 3.9, SE 1.4). The authors call such strategies "not easily scalable" and possible "statistical flukes". Digital: online ads 0.00 [-3.92, 3.92] and 1.10 [-4.78, 6.98]. | [KB18] Fig. 4, p. 153 n. 15, p. 160 · `19912a3a…`; [KB18-A] B.18 · `754534de…` | **BUILD on 5a** as a parameter; the targeted rows NOT used as a larger CACE (ask A4) |
| 11 | **OUTLET CHOICE** (§14) | absent as a player choice (C-N8 open; inventory D §2, §14) | impersonal / earned | **The class CACE, unchanged** (R2): choosing an outlet is choosing whom an impersonal or earned contact reaches. No outlet-specific source was searched separately (folded into row 10). | [KB18] Fig. 4(b) · `19912a3a…` | **BUILD on 5a** as a parameter |
| 12 | **RALLY SIZE** and **TV FREQUENCY** (§12) | fixed spend per action (`CampaignActions.Spec`, CampaignActions.cs:112-145) | personal / impersonal | **Reach, not effect size.** More reach adds exposures, which 5a's coverage 1 − e^(−E/(r·N)) saturates (persuadable once). TV frequency is GRPs: the run-up TV effect is "4.73 percentage points per 1,000 GRPs (SE = 1.42)" in its week, gone a week later. | [KB18] Fig. 4 · `19912a3a…`; [GGGS11] p. 144 · `b24cf0ee…` | **BUILD on 5a** as reach/GRP parameters |
| 13 | **RALLY SPEAKER** (§12) and **REGIONAL TOUR / VISIT REGIONS** (§3, §9 "Regional tour: 8 hours") | no speaker choice; no tour verb; local actions take a region (Sweden's map, GameController.CampaignMap.cs) | personal | **None for the candidate's own presence.** Italy 2014 municipal: candidates' visits left turnout 0.2 pp lower (insignificant) and the list's vote share 0.3 pp lower (insignificant), while volunteers' visits raised turnout 1.8 pp (CACE 5.01 %). Kalla & Broockman's one candidate-canvass row, 20.70 [0.32, 41.08] (US county commission, coded not competitive), is one of the two significant studies they call "not easily scalable". | [CP20] PDF pp. 6, 17, 21 · `ac31a040…`; [KB18] Fig. 4(a), p. 153 n. 15 · `19912a3a…`; [KB18-A] B.4.1 · `754534de…` | **CUT** (the tour as a verb and the speaker; a region's rallies and town halls are already built) |
| 14 | **PHONE BANKING** (§26) | `GotvModel` lists it with an [AUTHORED-DRAFT] spec (GotvModel.cs:44, 81) but no player verb; `GetOutTheVote` has no executor in `CampaignRun` | personal | **Turnout:** commercial phone banks "one additional voter for every 280 people they spoke with" (2002), "one vote was generated per fifty-five completed calls" (2004), "one vote generated per 500 contacts" (2009); high-quality banks "raised turnout among those they reached by 0.9 and 1.4 percentage points", low-quality 0.2; calls in the final week only. Volunteer phone banks: the book's figures BILLED. **Vote choice:** phone rows in Fig. 4(a): -1.60 [-11.60, 8.40], -4.20 [-14.59, 6.19], 7.50 [-7.00, 22.00], -22.00 [-51.40, 7.40] (inside the personal pool). | [GG14] pp. 16-18 · `5c809520…`; [KB18] Fig. 4(a) · `19912a3a…`; [KB18-A] B.2.2, B.3.2, B.15 · `754534de…` | **BUILD on 5b** (turnout, with Q6: lands with GATE-18's q2) |
| 15 | **MOBILIZE VOLUNTEERS / VOLUNTEER OPERATIONS** (§3, §9 Volunteers, §26) | volunteers are a stored count recruited by offices; no player verb | resource (hours) | **Through the doors and calls they make** (rows 5, 14): GG14 Table 4's 70 %+ band 0.6 [-1.5, 2.8]; [CP20] volunteers' canvass CACE 5.01 % on turnout (Italy). | [GG14] Table 4 · `5c809520…`; [CP20] PDF p. 6 · `ac31a040…` | **BUILD as resource on 5b** (with the field organiser, Q7) |
| 16 | **TRANSPORT** to the polls (§26) | `GotvOperation.Transport` [AUTHORED-DRAFT] (GotvModel.cs:46, 83), no verb | personal | **None found.** Named search (rides to the polls, randomized, turnout): news and company announcements only, no experiment. Nothing fetched. | named search, no source | **CUT** |
| 17 | **ELECTION-DAY REMINDERS** (§26) | `GotvOperation.ElectionDayReminders` [AUTHORED-DRAFT] (GotvModel.cs:47, 84), no verb | impersonal | **Zero** (R1). "there is ample evidence that reminding people that an election is coming has negligible effects [on] turnout"; Facebook 2010, 61 million users: "turnout among those who received the informational message was identical to turnout among those in the control group (treatment effect 0.00%, s.e.m., 0.28%)". (The social message with friends' faces: +0.39 %, s.e.m. 0.17 % - a platform's nonpartisan feature, not a party's verb.) | [GG14] p. 24 n. 9 · `5c809520…`; [BOND12] PMC XML · `d4bafc1a…` | **CUT** |
| 18 | **SOCIAL POST CATEGORIES and VIRALITY** (§12) | one SOCIAL POST action with no category; "virality is not modelled" (MediaSystem.cs:169) | impersonal | **None.** Kalla & Broockman's impersonal pool has mail, TV, online ads and a lit drop, no social posts by category; an "Attack" post is row 6. | [KB18] Fig. 4(b) · `19912a3a…` | **CUT** (the categories and virality; the SOCIAL POST action stays at the impersonal CACE) |
| 19 | **HIRE POLLSTER** (§5 table; §9) | hired at 1 800/day; effect: the party's poll sample ×1.5 [AUTHORED-DRAFT] (CampaignStaff.cs:126) | information | **No vote or turnout effect.** Its effect is a narrower ±, as the POLL's. Staff search: consultant studies are observational (US House 1990/1992); the full text BILLED, the abstract states no estimate. | [MED97] abstract · `ce0f3a5f…` | **INFO - Elias decides** (R3): CUT by the literal rule, or kept as information under Q7's POLL precedent |
| 20 | **HIRE MEDIA** advisor (§5 table; §9) | interview and announcement reach ×1.20 [AUTHORED-DRAFT] (CampaignStaff.cs:124, 141) | earned | **None of its own.** A larger reach only adds earned exposures, which coverage saturates; no source measures what a media advisor adds. | [MED97] abstract · `ce0f3a5f…` | **CUT** |
| 21 | **HIRE DIGITAL** strategist (§5 table; §9) | social post and digital ad reach ×1.25 [AUTHORED-DRAFT] (CampaignStaff.cs:125, 142) | impersonal | **None of its own.** Online ads themselves: 0.00 [-3.92, 3.92], 1.10 [-4.78, 6.98]. | [KB18] Fig. 4(b) · `19912a3a…`; [MED97] · `ce0f3a5f…` | **CUT** |
| 22 | **The four missing roles** - Policy Advisor, Fundraiser, Press Secretary, Volunteer Coordinator (§9; §68 deferred, FL F-1) | absent (`StaffRole` has five, CampaignStaff.cs:7-14) | - | **None of their own.** The fundraiser's yield is row 1's; the volunteer coordinator's is the field organiser's (Q7); policy advisor and press secretary have no source. | [MED97] · `ce0f3a5f…` | **CUT** |
| 23 | **BUY A POLL IN THE CAMPAIGN; POLL BREAKDOWNS** (§20-§21) | the player cannot queue a poll in the campaign (the HQ offers only the eight, GameController.Campaign.cs:875-879; the party auto-buys, inventory D SD-20); no demographic or regional breakdowns | information | **No vote effect** (information). | - | **INFO - Elias decides** (R3; Q7's POLL precedent) |
| 24 | **THE SWING INDEX; INTELLIGENCE, FIELD REPORTS, FOCUS GROUPS** (§25, §36); "Identify voter groups" as research (§3) | swing index never live (`SwingRegions.Unknown`, LiveCampaignSnapshot.cs:193); §36's three absent | information | **No vote effect** (information). | - | **INFO - Elias decides** (R3) |

Not rows (built, ruled, or not campaign actions; checked so nothing is silently dropped):
- **Built:** rally, town hall, doors, TV ad, digital ad, social post, interview, policy announcement (§12, `CampaignActions.TheEight`); the run-up's TV BUY, POLL and hires (Q7); scandal answers except Resign (§17).
- **Ruled elsewhere:** Resign (refused by ruling, §503, trigger T-9); OFFICE's timing (Q7); events' salience (§882, q5).
- **Not actions:** staff progression (§37), long-term capital (§38), momentum and turnout as layers of the share (§39), the results sheet (§30), other election types (§2), per-group loyalty (§8).

## 2. Sources

All under `docs/reference/raw/aloop/q4/` unless marked "(aloop/)", which are the plan's §8 files. Digests: `q4/SHA256SUMS.txt` and `aloop/SHA256SUMS.txt`. Fetch log: `q4/fetch_log.txt` (curl -sL with a desktop browser user agent, 2026-10-19 23:38-23:43 UTC). Quotes are from `pdftotext` of the stored files (layout and raw modes). For Kalla & Broockman's Figure 4 the raw-mode order was used to pair studies with estimates, because the layout mode misaligns them; the pairing was checked against the online appendix's own entries (B.2.1, B.2.2, B.3.1, B.3.2, B.4.1, B.15.1, B.15.2, B.18, all of which match).

| id | what | file · sha256 |
|---|---|---|
| [KB18] | Kalla & Broockman, APSR 112(1) 2018 (aloop/) | `gwern_mirror_stanford_kalla_broockman.pdf` · 19912a3a821c… |
| [KB18-A] | its online appendix (aloop/) | `cambridge_S0003055417000363sup001_kalla_broockman_2018_appendix.pdf` · 754534de7fa2… |
| [GG14] | Gerber & Green 2014 draft, "[EARLY DRAFT: NOT FOR CITATION]" (aloop/) | `nber_conf_f80841_gerber_green_gotv_overview.pdf` · 5c809520ae0b… |
| [GGGS11] | Gerber, Gimpel, Green & Shaw, APSR 105(1) 2011 (aloop/) | `gwern_mirror_2011_gerber_gimpel_green_shaw_tv_ads.pdf` · b24cf0ee0d07… |
| [LPP22] | Le Pennec & Pons, "How Do Campaigns Shape Vote Choice? Multi-Country Evidence from 62 Elections and 56 TV Debates", NBER WP 26572 (Dec 2019, rev. Dec 2022); published QJE 138(2) 703-767 (Crossref record beside it) | `nber_w26572_lepennec_pons.pdf` · 4893c412a05c…; `crossref_lepennec_pons2023.json` · eadac440555d… |
| [CP20] | Cantoni & Pons, "Do Interactions with Candidates Increase Voter Support and Participation? Experimental Evidence from Italy", NBER WP 27433 (June 2020) | `nber_w27433_cantoni_pons.pdf` · ac31a040d7bf… |
| [BCP12] | Barton, Castillo & Petrie, "Going Negative: The Persuasive Effect of Tone and Information on Campaign Fundraising and Voter Turnout", ICES (GMU) discussion paper, Nov 2012 - the working paper of JEBO 121 (2016) 99-113 (Crossref record beside it) | `gmu_ices_wp1037_barton_castillo_petrie.pdf` · 2380b00b4267…; `crossref_barton2016.json` · 0dbbfb447ee2… |
| [DL14] | Darr & Levendusky's own summary of "Relying on the Ground Game", APR 42(3) 529-548 (LSE USAPP blog, 12 May 2014, from LSE Research Online), and the article's Crossref abstract | `lse_blog_2014_darr_levendusky_field_offices.pdf` · acada52bd7ef…; `crossref_darr_levendusky2014.json` · e9ad25b2bd2d… |
| [AES11] | Adams, Ezrow & Somer-Topcu, AJPS 55(2) 370-382 (2011): the University of Essex repository record with its abstract; Crossref beside it | `essex_repository_3438_adams2011.html` · 9e40e6329d63…; `crossref_adams2011.json` · b67f802f1e2e… |
| [BOND12] | Bond et al., "A 61-million-person experiment in social influence and political mobilization", Nature 489 (2012): the PMC full text via NCBI E-utilities; the Europe PMC record beside it | `eutils_pmc_PMC3834737_bond2012.xml` · d4bafc1abec8…; `europepmc_search_bond2012_core.json` · e51c0488de98… |
| [MED97] | Medvic, "Is there a spin doctor in the house? The impact of political consultants in congressional campaigns", Purdue dissertation: the abstract page | `purdue_AAI9808489_medvic_dissertation.html` · ce0f3a5f398d… |

**BILLED (refused or not found; the refusal bodies are kept):**
- **Lau, Sigelman & Rovner 2007**, "The Effects of Negative Political Campaigns: A Meta-Analytic Reassessment", JOP 69(4) 1176-1209: doi.org → HTTP 403 (`doi_10_1111_j_1468-2508_2007_00618_x_lau2007_HTTP403.html`); the Crossref record has no abstract. It would decide rows 4 and 6.
- **Masket 2009**, "Did Obama's Ground Game Matter?", POQ 73(5) 1023-1039, doi 10.1093/poq/nfp077: HTTP 403 (`doi_10_1093_poq_nfp077_masket2009_HTTP403.html`); Crossref beside it, no abstract. A cross-check for row 5 only.
- **Darr & Levendusky 2014, the article**: SAGE via doi.org → HTTP 403 (`..._darr_levendusky_HTTP403.html`). The authors' summary and the abstract stand in.
- **Barton, Castillo & Petrie 2016 (JEBO), the published article**: the CSU repository copy → HTTP 403 (`calstate_barton_castillo_petrie_HTTP403.html`); doi.org gives only Elsevier's redirect to a paywall (`..._barton2016_REDIRECT_paywall.html`). The 2012 working paper stands in; its tables may differ from the published ones.
- **Adams, Ezrow & Somer-Topcu 2011, full text**: Wiley → HTTP 403 (`..._adams2011_HTTP403.html`). The abstract stands in.
- **Bond et al. 2012, the PDF**: Europe PMC full-text service HTTP 500, render HTTP 403, NCBI OA service HTTP 404 (all kept); the E-utilities XML is the full text used.
- **Medvic, the dissertation's or book's estimates** (Ohio State UP 2001): not online. Observational in any case.
- **Green & Gerber, *Get Out the Vote*, and the 2017 handbook chapter** (already BILLED in the plan's §8): the volunteer phone-bank figures for row 14.
- **Candidate training (row 3) and transport (row 16)**: named searches found no study; nothing to fetch.

## 3. What the method could not decide (asks for Elias)

- **A1. Information rows (19, 23, 24).** §7's rule knows votes, turnout and resources. Q7 kept the run-up POLL as information. Does that precedent extend to the pollster, a campaign poll with breakdowns, the swing index and §36's intelligence (priced, their effect a narrower ±), or are they CUT for having no vote effect?
- **A2. Attack (row 6) and the AI's attacks.** No stored source gives a negative message's effect on vote choice; the meta-analysis that would ([LSR07]) is BILLED. If the player's attack is CUT, 5a's AI attacks (the class CACE with the sign against the target, CampaignRun.cs:863-873) rest on the same missing source. Cut both, keep both as [AUTHORED-DRAFT] until [LSR07] is read, or keep the AI's only? This joins §881's open ask (NegativeCampaign wins all 30 cells of CampaignStrategyHarness).
- **A3. Debates (row 8) and the AI's debates.** [LPP22] finds no effect of TV debates on vote choice. The player's choices are CUT by the rule, but the existing debates still give the winner coverage and momentum shocks on the polls (CampaignRun.cs:937-963, [AUTHORED-DRAFT]). Should those shocks go too, for consistency? That is outside q4's list (built, not absent), so it is asked, not proposed.
- **A4. Targeting's size (row 10).** Kalla & Broockman's two significant close-to-election rows (2.75 and 3.90) come from individually pre-identified targets, which they call not scalable and possibly flukes. This table builds targeting at the unchanged class CACE. Should a pre-identified target (bought through polling, row 23) ever earn the larger figure?
- **A5. Strategy (row 4).** Cutting the strategy as a modifier leaves the AI's five personalities, which choose actions by strategy, and §11's five sets as their reference text. The proposed patch moves only the player's choosing; is that the intended scope?
- **A6. A transfer note for 5b, found on the way.** [CP20]'s volunteer canvass in Italy (2014 municipal) has a turnout CACE of 5.01 %, against GG14's 0.6 for the 70 %+ band that 5b plans to use. The control turnout of that sample is not quoted here; whether [CP20] is a better European anchor for 5b is a 5b question, raised now so it is not lost.

## 4. Proposed patch to ELECTIONS_CAMPAIGN_SPEC.md (NOT applied)

Applied only after Elias rules, and only for the rows he CUTs. Each moved line is removed from its § and appended verbatim under a new final section, with its reason, so nothing is deleted unmigrated. The lines below are copied exactly from the spec at 7cfae549.

```
# 45. Cut by q4 (A-LOOP, ruled <date>; docs/specs/A_LOOP_Q4_TABLE.md)

Text moved here from its section because no field-experiment or named-search source gives the action an effect on
vote choice or turnout (A_LOOP_PLAN §7.3). Each entry names its old place, the row of the q4 table, and the reason.

## From §3 Pre-Campaign
* Develop policies            [row 2: voters do not respond to campaign policy statements, AES11]
* Train candidates            [row 3: no study found]
* Select campaign strategy    [row 4: no effect of its own; its parts are rows 6, 9, 10, 14]

## From §3 Campaign
* Participate in debates      [row 8: the player's debate choices; LPP22 finds no effect of TV debates on vote choice]
* Visit regions               [row 13: candidates' visits move neither turnout nor the list's share, CP20]
* Attack opponents            [row 6: no vote-choice estimate; LSR07 BILLED - provisional]
* Defend against attacks      [row 7: no source]
* Change campaign strategy    [row 4]

## From §9 Time
Regional tour: 8 hours        [row 13]

## From §9 Staff
Media Advisor                 [row 20: no source for what the role adds]
Pollster                      [row 19: only if Elias does not keep it as information]
Policy Advisor                [row 22]
Digital Strategist            [row 21]
Fundraiser                    [row 22: the yield is FUNDRAISE's, row 1]
Press Secretary               [row 22]
Volunteer Coordinator         [row 22: the field organiser carries volunteers, Q7]

## From §11 Campaign Strategy
Before and during the campaign, the player should be able to choose an overall strategy.
                              [row 4; the five sets stay in §11 as the AI personalities' reference - ask A5]

## From §12 Rally
* Speaker                     [row 13: CP20]

## From §12 Social Media
Possible post categories:
* Policy announcement
* Attack
* Emotional message
* Personal story
* Rally promotion
* Economic message
* Crisis response
Posts can go viral, fail, or trigger controversy.
                              [row 18: no source for categories or virality; the post itself stays]

## From §15 Debates
Before a debate, the player chooses:
* Topics to emphasize
* Attack strategy
* Defensive strategy
* Tone
* Talking points
During the debate, the player can make decisions.
                              [row 8; the example move list and "Performance depends on" stay as the run's reference]

## From §26 Get-Out-The-Vote System
* Transport                   [row 16: no study found]
* Election-day reminders      [row 17: GG14 and BOND12 measure zero]
```

Lines that move only if Elias CUTs the information rows (A1): §20 "Demographic breakdown", "Regional breakdown"; §21's "Advanced polling" block; §25 "Swing Index: 92/100" and its paragraph; §36's "Intelligence", "Field reports", "Focus groups".

The BUILD rows (1, 5, 9-12, 14, 15) change no spec text: the spec already names them; what they need is their place on 5a's or 5b's path, which is code for after the ruling.
