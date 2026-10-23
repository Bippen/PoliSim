# The debt limit's hostile-majority episodes of record (PS-6 K78)

Elias's ruling K78 (2026-10-23): "AI hostile majorities may withhold the raise, by a rule FITTED on the record's episodes (2011, 2013, 2023 - and say whether 2021 belongs) ... the simplest honest rule ... [FITTED] with n stated, or [AUTHORED-DRAFT] with the episodes as its reading if n is too small to fit." The sources are in `raw/debtlimit_episodes/` (their log and digests there); the code carries the dates as `UsDebtLimit.HostileEpisodes`, and `UsDebtLimitLiveDiagnostic` holds each to the file it is read from.

## The episodes

A hostile majority: a chamber's majority party not the president's. The four dates of each: the day the limit was reached (extraordinary measures begun), the day the hostile chamber first refused or conditioned a clean raise, the X-date (Treasury's last projected day of the measures), and the day the act that ended it was approved.

| episode | the hostile majority | reached | first refused or conditioned | X-date | law | X-date less law |
|---|---|---|---|---|---|---|
| 2011 | the House, REP (the president DEM) | 2011-05-16 - R43389 [SECONDARY], quoting Treasury's letter (BILLED: see the log) | 2011-05-31 - `clerk_roll_2011_379.xml`: H.R. 1954, "To implement the President's request to increase the statutory limit on the public debt", on the motion to suspend the rules and pass, Failed; REP 0 yea, 236 nay | 2011-08-02 - R43389 [SECONDARY] ("exhausted on August 2") | 2011-08-02 - `govinfo_PLAW-112publ25.htm`, "Approved August 2, 2011" | 0 days |
| 2013 | the House, REP (the president DEM) | 2013-05-20 - R43389's table [SECONDARY]: suspended through 18 May 2013 (`govinfo_PLAW-113publ3.htm`: "ending on May 18, 2013"), extraordinary measures invoked 20 May | BILLED - no House vote on a clean raise, or on a debt-limit bill with conditions, between May and October 2013 is in the sources fetched | 2013-10-17 - R43389 [SECONDARY] ("no later than October 17") | 2013-10-17 - `govinfo_PLAW-113publ46.htm`, "Approved October 17, 2013" | 0 days |
| 2023 | the House, REP (the president DEM) | 2023-01-19 - the Secretary's letter of that day (`raw/treasury_debt/`, read in `debt_limit.md`) | 2023-04-26 - `clerk_roll_2023_199.xml`: H.R. 2811, the Limit, Save, Grow Act (the raise conditioned on its cuts), On Passage, Passed; REP 217 yea, 4 nay; DEM 0 yea, 211 nay | 2023-06-05 - the Secretary's letter of 26 May 2023 ("by June 5", read in `debt_limit.md`) | 2023-06-03 - `govinfo_PLAW-118publ5.htm`, "Approved June 3, 2023" | 2 days |

**2021 does not belong.** The president and both chambers were DEM; the clean suspension (H.R. 5305) fell to the Senate minority's filibuster ("On September 27, 2021, the Senate declined to close further debate on the bill", R43389 [SECONDARY]), and P.L. 117-50's $480 billion increase was approved on 14 October 2021 (`govinfo_PLAW-117publ50.htm`). A minority's filibuster is no hostile majority, and the game votes the raise by reconciliation, a majority in each chamber with no cloture (K25) - the model has nothing that 2021 would read.

**Not read into the rule.** R43389's table [SECONDARY] carries further acts under a divided government - the No Budget, No Pay Act (P.L. 113-3, 4 February 2013), S. 540 (P.L. 113-83, 15 February 2014), the Bipartisan Budget Acts of 2015 (P.L. 114-74, 2 November 2015) and 2019 (P.L. 116-37, 2 August 2019) - whose X-dates are not in the sources fetched. They are outside the ruling's named set and are not sourced now (DECLARED: Elias, 2026-10-23, chose no further episodes).

## The rule - [AUTHORED-DRAFT], n = 3 (too few to fit)

- **When the AI's hostile majority withholds:** from the day the limit is reached, while the measures run (DECLARED, and kept: Elias, 2026-10-23, did not choose to move the start to the refusal lag - the record's first refusal or conditioning came 15 days after the reach in 2011 and 97 in 2023, and none is identified in 2013).
- **When it releases:** on the X-date less the episodes' median lead - X-date less law, 0, 0 and 2 days, the median 0 (the mean 0.67) - so on the day the measures run out, before that day's vote: in every episode of record the hostile majority let a raise or a suspension become law at, or two days before, Treasury's last projected day.
- In the game the X-date is the day the measures run out (`UsDebtLimit.BindsOn`), and a raise is voted on that day (K25), so the withhold stands through the measures and lets the deadline's vote pass. A player's party never withholds by this rule; its own withhold is its verb (K22).
