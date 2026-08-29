# Elections & Campaigning System Specification

## 1. Core Design Goal

Create a deep but understandable election and campaigning system for a Unity political strategy/simulation game.

The player should feel that they are actually running a political campaign rather than simply selecting policies and waiting for an election result.

The election system should connect:

* Political ideology
* Policy positions
* Voter demographics
* Voter priorities
* Candidate popularity
* Party reputation
* Campaign spending
* Campaign staff
* Media coverage
* Social media
* Debates
* Rallies
* Advertising
* Grassroots organization
* Polling
* Scandals
* Political events
* Opponent campaigns
* Regional differences
* Turnout
* Tactical voting
* Coalition dynamics
* Economic conditions
* Government performance

The system should be deterministic enough that players can understand why they won or lost, but contain enough uncertainty that elections cannot be perfectly predicted.

---

# 2. Election Structure

An election should have a defined:

* Election date
* Campaign start date
* Campaign duration
* Number of seats
* Electoral system
* Electoral districts/regions
* Parties
* Candidates
* Voter groups
* Turnout
* Vote counting system

The system should support multiple election types.

### Election Types

Examples:

* National parliamentary election
* Regional election
* Local/municipal election
* Presidential election
* Party leadership election
* Referendum

The same underlying campaign framework should be reusable, with different rules determining how votes translate into seats or victory.

---

# 3. Campaign Calendar

The campaign should progress through time.

For example:

### Pre-Campaign

The player can:

* Recruit staff
* Raise money
* Establish campaign offices
* Select campaign strategy
* Develop policies
* Conduct polling
* Identify voter groups
* Train candidates
* Prepare advertisements

### Campaign

The player can:

* Hold rallies
* Run advertisements
* Give interviews
* Participate in debates
* Visit regions
* Publish policies
* Campaign online
* Attack opponents
* Defend against attacks
* Mobilize volunteers
* Respond to events
* Change campaign strategy

### Election Day

The campaign ends and the game calculates:

* Turnout
* Votes
* Regional results
* Seats
* Coalition possibilities
* Government formation

---

# 4. Political Parties

Every party should have several characteristics.

```text
Party
├── Name
├── Ideology
├── Leader
├── Popularity
├── Reputation
├── Funding
├── Organization
├── Membership
├── Media Presence
├── Grassroots Strength
├── Policy Positions
├── Campaign Effectiveness
├── Candidate Quality
└── Historical Support
```

## Party Ideology

Ideology should not simply be one left/right number.

Use several ideological dimensions.

Example:

```text
Economic Left ←→ Economic Right
Social Liberal ←→ Social Conservative
Globalist ←→ Nationalist
Environmental ←→ Industrial
Centralization ←→ Decentralization
High Tax ←→ Low Tax
Immigration Restrictive ←→ Immigration Liberal
Public Sector ←→ Private Sector
```

A party can therefore occupy a unique ideological position.

---

# 5. Voter Groups

The population should be divided into voter groups.

Example:

```text
Working Class
Middle Class
High Income
Students
Retirees
Business Owners
Farmers
Urban Professionals
Industrial Workers
Public Sector Workers
Young Voters
Older Voters
```

Each group should have:

* Population size
* Geographic distribution
* Turnout probability
* Policy priorities
* Party loyalty
* Ideological position
* Economic sensitivity
* Media consumption
* Campaign responsiveness

Example:

```text
Young Urban Voters

Population: 14%
Turnout: 58%
Party Loyalty: Low
Top Issues:
  Climate: 85
  Housing: 82
  Education: 71
  Economy: 63
Campaign Responsiveness: High
Social Media Usage: Very High
```

---

# 6. Voter Issue Priorities

Every voter should care about different issues with different weights.

Use a normalized value between 0 and 100.

Example:

```text
Economy: 85
Healthcare: 72
Immigration: 35
Climate: 48
Crime: 61
Taxes: 78
Education: 55
Housing: 80
Defense: 30
```

A party's attractiveness should depend partly on how closely its policies match those priorities.

The player should therefore NOT simply maximize overall popularity.

A party could become extremely popular among one group while losing another.

---

# 7. Party-Voter Compatibility

Calculate ideological/policy compatibility between each party and each voter group.

Basic concept:

```text
Compatibility =
Policy Match
+ Ideological Match
+ Party Reputation
+ Leader Appeal
+ Campaign Effectiveness
```

Normalize the result to 0–100.

Example:

```text
Party A → Working Class: 71
Party A → Students: 48
Party A → Retirees: 65

Party B → Working Class: 54
Party B → Students: 82
Party B → Retirees: 41
```

This becomes the foundation for polling and election results.

---

# 8. Voter Loyalty

Not every voter should constantly change parties.

Each voter group should have a loyalty value.

Example:

```text
Strong Party Loyalist: 90
Loyal: 75
Lean: 60
Independent: 40
Swing Voter: 20
```

Loyal voters require significantly more effort to persuade.

Swing voters should be much more responsive to:

* Campaign events
* Debates
* Economic conditions
* Scandals
* Advertising
* Candidate performance
* Tactical voting

---

# 9. Campaign Resources

The campaign should operate under limited resources.

Primary resources:

### Money

Used for:

* Advertising
* Staff
* Offices
* Events
* Travel
* Polling
* Digital campaigns

### Time

Campaign actions consume time.

For example:

```text
Rally: 4 hours
Interview: 2 hours
Debate preparation: 6 hours
Regional tour: 8 hours
Policy announcement: 3 hours
```

### Staff

Different staff types provide different bonuses.

```text
Campaign Manager
Media Advisor
Pollster
Policy Advisor
Digital Strategist
Field Organizer
Fundraiser
Press Secretary
Volunteer Coordinator
```

### Volunteers

Volunteers increase grassroots campaigning and turnout.

---

# 10. Campaign Offices

Players should be able to establish regional campaign offices.

Each office provides:

* Local organization
* Volunteer recruitment
* Door-to-door campaigning
* Local polling information
* Election-day turnout operations

Offices should have:

```text
Cost
Staff Capacity
Volunteer Capacity
Regional Influence
Maintenance Cost
```

A party could therefore decide to concentrate resources in a few swing regions rather than campaign everywhere equally.

---

# 11. Campaign Strategy

Before and during the campaign, the player should be able to choose an overall strategy.

Examples:

### Broad Appeal

Attempt to become acceptable to as many voters as possible.

Effects:

* Small gains across many groups
* Lower ideological intensity
* Reduced polarization

### Base Mobilization

Focus on existing supporters.

Effects:

* Increased turnout among loyal voters
* Lower persuasion of swing voters
* Stronger grassroots organization

### Swing Voter Strategy

Target undecided voters.

Effects:

* Strong gains among independents
* Potential loss of ideological voters

### Negative Campaign

Attack opponents.

Effects:

* Can reduce opponent popularity
* High media attention
* Risk of backlash
* Increased polarization

### Populist Campaign

Focus heavily on a few high-salience issues.

Effects:

* Strong gains among voters prioritizing those issues
* Reduced support among other groups

---

# 12. Campaign Actions

Campaign actions should be actual gameplay decisions.

## Rally

Player chooses:

* Location
* Topic
* Speaker
* Size

Effects:

* Increases local awareness
* Increases enthusiasm
* Generates media coverage
* Mobilizes supporters

Large rallies should cost more but generate greater visibility.

---

## Town Hall

Smaller event with greater persuasion.

Effects:

* High persuasion
* Low media impact
* Strong local effect

---

## Door-to-Door Campaigning

Very effective but resource-intensive.

Effects:

* Strong turnout bonus
* Moderate persuasion
* Primarily local

Particularly useful during the final weeks.

---

## Television Advertisement

Player selects:

* Target region
* Target demographic
* Message
* Budget
* Frequency

Example:

```text
Target: Working Class
Issue: Cost of Living
Budget: €500,000
Duration: 7 days
```

The system calculates:

```text
Reach
× Frequency
× Message Relevance
× Candidate Credibility
```

---

## Digital Advertisement

More precise targeting.

Can target:

* Age
* Region
* Issue
* Ideology
* Interests

However, excessive targeting can create diminishing returns.

---

## Social Media

Player can publish posts.

Possible post categories:

* Policy announcement
* Attack
* Emotional message
* Personal story
* Rally promotion
* Economic message
* Crisis response

Posts can go viral, fail, or trigger controversy.

---

# 13. Media System

The media should act as an independent force.

News coverage is influenced by:

```text
Campaign Activity
Candidate Popularity
Scandals
Major Events
Debates
Economic Conditions
Controversial Statements
Unexpected Events
```

Media coverage can create momentum.

Example:

```text
Candidate gives strong debate performance
↓
Positive media coverage
↓
Higher public awareness
↓
Higher polling
↓
More media attention
↓
Momentum
```

But momentum should have diminishing returns so that the system does not spiral uncontrollably.

---

# 14. Media Bias / Audience Segmentation

Different media outlets should appeal to different audiences.

Example:

```text
Outlet A
Audience:
Urban
Young
Liberal

Outlet B
Audience:
Older
Conservative
Rural

Outlet C
Audience:
General Population
Moderate
High Reach
```

Players can choose which outlets to target.

The same campaign message may perform very differently depending on the audience.

---

# 15. Debates

Debates should be major campaign events.

Before a debate, the player chooses:

* Topics to emphasize
* Attack strategy
* Defensive strategy
* Tone
* Talking points

During the debate, the player can make decisions.

Example:

```text
Attack Opponent
Defend Policy
Change Subject
Appeal Emotionally
Present Statistics
Ignore Attack
Counterattack
```

Performance depends on:

```text
Candidate Skill
Preparation
Policy Knowledge
Popularity
Charisma
Opponent Performance
Issue Ownership
Random Event
```

A strong debate performance can significantly alter:

* Candidate approval
* Media coverage
* Momentum
* Polling

---

# 16. Candidate Attributes

Every candidate should have attributes.

```text
Charisma
Leadership
Debate Skill
Communication
Policy Knowledge
Credibility
Integrity
Media Skill
Campaign Skill
Popularity
Scandal Resistance
```

Candidate weaknesses should matter.

For example:

```text
Charisma: 90
Policy Knowledge: 45
Integrity: 82
Debate Skill: 91
```

This candidate may perform extremely well in debates but struggle with detailed policy discussions.

---

# 17. Scandals

Scandals should be dynamic events rather than scripted game-over events.

Potential scandals:

* Financial misconduct
* Corruption
* Personal controversy
* Offensive statement
* Old social media post
* Policy contradiction
* Internal party dispute
* Campaign finance violation

Severity:

```text
Minor
Moderate
Major
Catastrophic
```

Response options:

```text
Deny
Apologize
Explain
Attack Source
Ignore
Resign
Sacrifice Staff Member
```

Each response has different risks.

A transparent apology may reduce long-term damage but cause a short-term polling decline.

A denial can work if evidence is weak but become catastrophic if evidence later appears.

---

# 18. Political Events

The campaign should not exist in a vacuum.

Random and systemic events should influence elections.

Examples:

* Economic recession
* Economic boom
* Inflation
* Natural disaster
* War
* Terrorist attack
* Major corporate failure
* Healthcare crisis
* Government scandal
* Migration surge
* Crime increase
* Major scientific discovery

Events should affect issue salience.

Example:

```text
Inflation rises sharply
↓
Economy becomes more important
↓
Economic issue weight increases
↓
Parties perceived as responsible lose support
↓
Opposition gains
```

---

# 19. Government Performance

If the player is the incumbent government, election performance should depend partly on actual government performance.

Track:

```text
GDP Growth
Unemployment
Inflation
Real Wage Growth
Crime
Healthcare Performance
Education
Government Debt
Public Services
Immigration
Public Satisfaction
```

Voters should not automatically understand every statistic.

Instead, perceived performance should be influenced by:

* Media
* Personal economic situation
* Party messaging
* Opposition attacks
* Actual outcomes

This creates a difference between:

```text
Actual Economy
vs
Perceived Economy
```

---

# 20. Polling System

Polling should provide imperfect information.

Polls should contain:

* Sample size
* Margin of error
* Methodology
* Demographic breakdown
* Regional breakdown
* Field date

Example:

```text
National Poll

Party A: 31%
Party B: 28%
Party C: 17%
Party D: 9%

Margin of Error: ±2.1%
```

Polls should NOT exactly predict election results.

Factors such as:

* Late swings
* Turnout
* Undecided voters
* Polling error
* Tactical voting

should create differences.

---

# 21. Internal Polling

The player can purchase better polling.

Basic polling:

```text
Cheap
Low sample size
Large uncertainty
```

Advanced polling:

```text
Expensive
Large sample
Regional data
Demographic segmentation
Turnout modeling
```

The player should have to decide whether additional information is worth the cost.

---

# 22. Polling Momentum

Polling should have a moving average.

Do not immediately translate every action into permanent support.

Use:

```text
Current Support
+
Recent Campaign Effects
+
Momentum
+
Underlying Political Environment
```

Momentum should decay naturally.

Example:

```text
Strong debate
+2.0%

After several days:
+1.4%

After two weeks:
+0.4%

After one month:
+0.0%
```

Unless the event creates a lasting reputation change.

---

# 23. Tactical Voting

Voters should sometimes vote strategically.

For example, a voter may prefer:

```text
Party A = 40 preference
Party B = 35 preference
Party C = 25 preference
```

But if Party C has no realistic chance of winning their district, the voter may switch to Party B.

This should depend on:

* Electoral system
* Polling
* Local competition
* Voter ideology
* Strategic awareness

Tactical voting should be much stronger in certain electoral systems than others.

---

# 24. Regional Politics

The national vote should not be enough.

Every region should have unique:

* Demographics
* Economic structure
* Political history
* Major industries
* Urban/rural balance
* Party loyalty
* Issue priorities

Example:

```text
Region A
Population: 1.2m
Urbanization: 85%
Economy: Services
Priority Issues:
Housing
Climate
Education

Region B
Population: 600k
Urbanization: 32%
Economy: Agriculture
Priority Issues:
Fuel
Agriculture
Immigration
```

This allows regional campaign strategy.

---

# 25. Swing Regions

The game should identify regions where small changes can determine the result.

Example:

```text
Region A
Party A: 40.5%
Party B: 39.8%

Swing Index: 92/100
```

The player should be able to allocate campaign resources toward these areas.

However, the game should NOT explicitly tell the player the exact optimal strategy unless they invest in polling/intelligence.

---

# 26. Get-Out-The-Vote System

Winning support is not enough.

The campaign must actually get supporters to vote.

Final turnout calculation:

```text
Base Turnout
×
Political Engagement
×
Campaign Mobilization
×
Candidate Enthusiasm
×
Election Salience
```

Campaign actions such as:

* Phone banking
* Door knocking
* Transport
* Volunteer operations
* Election-day reminders

increase turnout.

This makes grassroots organization strategically important.

---

# 27. Election-Day Simulation

On election day, calculate each region independently.

For each voter group:

```text
Population
×
Eligible Voters
×
Turnout
×
Party Preference
```

Then aggregate all groups.

Introduce controlled uncertainty.

For example:

```text
Final Vote = Expected Vote + Election Noise
```

The noise should be small enough that good strategy matters but large enough that elections cannot be perfectly predicted.

---

# 28. Vote-to-Seat Conversion

Separate the vote calculation from the seat calculation.

Support should first generate raw votes.

Then apply the electoral system.

The game should support:

### Proportional Representation

```text
Vote Share → Seat Share
```

with configurable thresholds and allocation methods.

### First-Past-The-Post

Each district independently determines a winner.

### Mixed-Member Systems

Combine:

* District seats
* Party-list seats

This should make electoral strategy fundamentally different depending on the country.

---

# 29. Coalition Formation

If no party has a majority, allow coalition negotiations.

Parties should have:

```text
Ideological Compatibility
Policy Compatibility
Leader Compatibility
Coalition Red Lines
Personal Relationships
Seat Strength
Negotiating Power
```

Possible outcomes:

* Minority government
* Majority coalition
* Confidence-and-supply agreement
* New election
* Government collapse

---

# 30. Election Results Screen

After the election, provide a detailed breakdown.

Show:

```text
TOTAL VOTES
Party A: 32.4%
Party B: 29.1%
Party C: 17.8%

SEATS
Party A: 112
Party B: 101
Party C: 61

TURNOUT
78.4%
```

Then show regional results.

Also show:

```text
Largest Gains
Largest Losses
Swing Regions
Young Voters
Older Voters
Urban Voters
Rural Voters
Income Groups
Issue-Based Voting
Turnout Changes
```

---

# 31. Post-Election Analysis

The game should explain WHY the player won or lost.

Example:

### Why You Won

```text
Strong economic performance       +3.2%
Successful debate                 +1.4%
Strong urban campaign             +0.8%
High youth turnout                +0.7%
Opposition scandal                +1.2%

Total estimated impact            +7.3%
```

### Why You Lost

```text
Poor rural organization           -1.8%
Weak immigration policy            -1.2%
Negative economic perception       -2.4%
Low voter turnout                  -1.1%
Opponent debate performance        -0.9%
```

This is extremely important.

The player should understand the causal chain behind the result rather than simply seeing "You lost."

---

# 32. Campaign AI

AI-controlled parties should use the same campaign system as the player.

Each AI party should have a strategy personality.

Examples:

### Professional Campaigner

* Uses polling
* Targets swing voters
* Allocates money efficiently
* Reacts quickly to events

### Populist

* Focuses on high-salience issues
* Large rallies
* Social media heavy
* Aggressive attacks

### Establishment

* Strong traditional media
* Broad messaging
* Moderate policies

### Grassroots

* Low advertising budget
* Strong volunteers
* Door-to-door campaigns
* High turnout

### Chaotic

* Inconsistent strategy
* High-risk decisions
* Unpredictable messaging

This should make different opponents feel genuinely different.

---

# 33. AI Decision-Making

AI should evaluate campaign actions based on expected value.

Conceptually:

```text
Action Score =
Expected Vote Gain
×
Target Importance
×
Probability of Success
-
Cost
-
Risk
```

For example, an AI may decide:

```text
TV Advertisement
Expected Gain: +0.8%
Cost: €2m
Risk: Low

Rally
Expected Gain: +0.4%
Cost: €300k
Risk: Low

Attack Opponent
Expected Gain: +1.2%
Cost: €100k
Risk: High
```

The AI can then select the action with the highest expected strategic value.

---

# 34. Campaign Mistakes

The player should be able to make bad decisions.

Examples:

* Overspending in safe regions
* Ignoring swing voters
* Focusing too much on one issue
* Responding poorly to scandals
* Alienating the party base
* Running too many negative ads
* Ignoring grassroots organization
* Choosing the wrong debate strategy

The system should make these mistakes recoverable when possible.

---

# 35. Diminishing Returns

Campaign spending should have diminishing returns.

For example:

```text
€0 → €100k:
Huge impact

€100k → €500k:
Large impact

€500k → €2m:
Moderate impact

€2m → €10m:
Small impact
```

This prevents the richest party from automatically winning every election.

---

# 36. Hidden Variables

Some information should remain hidden from the player.

Examples:

* Exact voter preferences
* Exact turnout probability
* True candidate enthusiasm
* Exact impact of advertisements
* Probability of a scandal
* Exact opponent strategy

The player can reduce uncertainty through:

* Polling
* Intelligence
* Field reports
* Focus groups
* Staff
* Experience

This creates a strategic information economy.

---

# 37. Campaign Staff Progression

Campaign staff can gain experience.

Example:

```text
Junior Pollster
↓
Experienced Pollster
↓
Senior Pollster
↓
Elite Strategist
```

Staff can develop specialties.

For example:

```text
Digital Strategy +15
Polling +20
Grassroots +12
Media +8
```

This allows long-term campaign management between elections.

---

# 38. Long-Term Political Capital

Winning an election should not reset everything.

Track:

* Party reputation
* Leader reputation
* Voter trust
* Organizational strength
* Donor network
* Grassroots network
* Media relationships
* Political momentum

A successful campaign should make future campaigns easier.

A disastrous campaign should create lasting consequences.

---

# 39. Core Simulation Formula

The final vote share should emerge from multiple layers.

Conceptually:

```text
Base Party Support
+
Policy Compatibility
+
Ideological Compatibility
+
Candidate Appeal
+
Government Performance
+
Campaign Effects
+
Regional Effects
+
Media Effects
+
Momentum
+
Turnout Effects
+
Tactical Voting
+
Election Noise
=
Final Vote Share
```

Do not make any single variable overwhelmingly powerful.

The purpose of the system is to create emergent election outcomes.

---

# 40. Unity Architecture

Use modular systems rather than putting the entire election system into one MonoBehaviour.

Suggested architecture:

```text
ElectionManager
├── CampaignManager
├── PollingManager
├── VoterSimulation
├── MediaManager
├── EventManager
├── DebateManager
├── ScandalManager
├── RegionalManager
├── TurnoutManager
├── ElectionCalculator
├── CoalitionManager
└── ElectionResultsManager
```

Data should primarily use ScriptableObjects where appropriate.

Example:

```text
PartyData
CandidateData
VoterGroupData
RegionData
IssueData
CampaignActionData
MediaOutletData
ElectionRulesData
```

Runtime state should be kept separately from static configuration.

---

# 41. Recommended Data Model

### PartyData

```text
partyName
ideology
baseSupport
funding
organization
reputation
leader
policyPositions
```

### VoterGroupData

```text
groupName
populationShare
turnoutBase
issueWeights
ideology
partyLoyalty
mediaPreferences
regionalDistribution
```

### RegionData

```text
regionName
population
demographics
economicData
historicalVoting
issuePriorities
electoralSeats
```

### CandidateData

```text
name
charisma
debateSkill
communication
credibility
integrity
policyKnowledge
campaignSkill
popularity
```

---

# 42. Important Design Principle

Do NOT create a system where:

```text
Campaign Action → +2% Votes
```

This becomes predictable and gamey.

Instead:

```text
Campaign Action
↓
Reach
↓
Issue Salience
↓
Voter Exposure
↓
Message Relevance
↓
Candidate Credibility
↓
Persuasion / Enthusiasm
↓
Party Preference
↓
Turnout
↓
Regional Vote
↓
Electoral System
↓
Seats
```

This creates a much more believable simulation.

---

# 43. Example Campaign Scenario

The player controls a center-right party.

Current polling:

```text
Party A: 31%
Party B: 29%
Party C: 18%
Party D: 9%
Others: 13%
```

The economy is weak.

The player's strongest demographic is older voters.

Their weakest demographic is young urban voters.

The player discovers through polling that housing has become the second-most important issue among young voters.

The player has three options:

### Option A — Tax Cuts

Strong with business owners and higher-income voters.

### Option B — Housing Program

Strong with young voters and urban voters.

### Option C — Crime Campaign

Strong with older and suburban voters.

The player chooses the housing program.

They then:

1. Announce the policy.
2. Hold an urban rally.
3. Run targeted digital advertisements.
4. Appear on television.
5. Defend the policy in a debate.

The system calculates that the campaign increases support among young urban voters.

However, the policy is expensive and older voters perceive it as fiscally irresponsible.

Result:

```text
Young voters: +4.1%
Urban voters: +2.0%
Older voters: -0.8%
High income: -1.1%
```

National polling rises only from:

```text
31.0% → 31.8%
```

but several swing districts move significantly.

The player therefore learns an important lesson:

**National polling is not the same thing as electoral victory.**

---

# 44. Design Philosophy

The election system should make the player constantly ask:

> "Where can I actually gain votes?"

rather than:

> "Which button gives me the most popularity?"

The strongest gameplay loop should be:

```text
Observe
↓
Analyze
↓
Choose Target
↓
Develop Message
↓
Campaign
↓
Monitor Reaction
↓
Adapt
↓
Respond to Opponents
↓
Manage Resources
↓
Election Day
↓
Analyze Result
↓
Prepare for Next Election
```

The best campaign strategy should therefore emerge from the interaction between **political positioning, voter psychology, geography, resources, information and uncertainty**.

The system should be deep enough to produce unexpected election results while remaining transparent enough that, after every election, the player can understand what decisions caused the outcome.
