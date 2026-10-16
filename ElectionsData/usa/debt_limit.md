# USA - the statutory debt limit (item B7)

Sourced 2026-10-02 for the designer's ruling: *"The limit is $41.1 trillion since P.L. 119-21 (July 2025, +$5.0 trillion). At the limit,
extraordinary measures buy a few months; after that Congress raises or suspends the limit, or the game forces spending down to revenue. CRS
IF10292 (21 Sep 2026) projects the limit binding in early to mid 2027."*

Sections 1-5 read `raw/debt_limit/` (SHA-256 in its `SHA256SUMS.txt`). Sections 6-11 (PS-6 US-14, 2026-10-16) read `raw/treasury_debt/`,
`raw/budget_statutes/` and `raw/lapse/`, each with its own `SHA256SUMS.txt` and `fetch_log.md`; their figures are generated into
`docs/generated/US_DEBT_LIMIT.md` by `Tools/us_debt_limit_prep.pl` and by the engine, and `UsDebtLimitCheck` (the cheap bar) re-reads them. A gloss
in italics follows each quotation.

## 1. Public Law 119-21, section 72001 - the increase

Source: `govinfo_PLAW-119publ21.htm` (and `.pdf`), the GPO's official text of **Public Law 119-21, 119th Congress, H.R. 1**, "An Act To provide
for reconciliation pursuant to title II of H. Con. Res. 14", **approved July 4, 2025**. Title VII (Committee on Finance), Subtitle C "Increase
in Debt Limit", **139 Stat. 332**. congress.gov could not be read (Cloudflare 403, see the fetch log).

> SEC. 72001. MODIFICATION OF LIMITATION ON THE PUBLIC DEBT.
>
> The limitation under section 3101(b) of title 31, United States Code, as most recently increased by section 401(b) of Public Law 118-5 (31
> U.S.C. 3101 note), is increased by $5,000,000,000,000.
>
> *Gloss: the debt limit, as last raised by the Fiscal Responsibility Act of 2023 (P.L. 118-5 § 401(b)), is **increased by
> $5,000,000,000,000**. The section adds an increment and **states no total**; it is classified as a note to 31 U.S.C. 3101 ("<<NOTE: 31 USC
> 3101 note.>>").*

## 2. 31 U.S.C. 3101(b) - the codified limit and the 2023 rule the increase builds on

Source: `govinfo_USCODE-2024-title31-sec3101.htm`, United States Code **2024 Edition** (GPO). uscode.house.gov returned only an "Under
Maintenance" page, so the House's prelim edition could not be read.

> (b) The face amount of obligations issued under this chapter and the face amount of obligations whose principal and interest are guaranteed by
> the United States Government (except guaranteed obligations held by the Secretary of the Treasury) may not be more than $14,294,000,000,000,
> outstanding at one time, subject to changes periodically made in that amount as provided by law through the congressional budget process
> described in Rule XLIX of the Rules of the House of Representatives or as provided by section 3101A or otherwise.
>
> *Gloss: the dollar figure written into § 3101(b) is still **$14,294,000,000,000** (the 2010 level). Every later change, including the 2023
> suspension and the 2025 increase, sits in uncodified notes ("or otherwise"). **"31 U.S.C. 3101(b) as amended" does not itself read $41.1
> trillion.***

The note the 2025 increase is added to (Statutory Notes, "Temporary Debt Limit Extension", Pub. L. 118-5, div. D, § 401, June 3, 2023, 137
Stat. 48):

> "(a) In General.--Section 3101(b) of title 31, United States Code, shall not apply for the period beginning on the date of the enactment of
> this Act [June 3, 2023] and ending on January 1, 2025.
> "(b) Special Rule Relating to Obligations Issued During Extension Period.--Effective on January 2, 2025, the limitation in effect under
> section 3101(b) of title 31, United States Code, shall be increased to the extent that-- "(1) the face amount of obligations [...]
> outstanding on January 2, 2025, exceeds "(2) the face amount of such obligations outstanding on the date of the enactment of this Act.
>
> *Gloss: the limit was suspended from 3 June 2023 to 1 January 2025 and reinstated on 2 January 2025 at whatever debt was then outstanding.
> The base the $5 trillion was added to is therefore a **figure Treasury computed**, not a number in any statute. (The 2024 edition's notes
> do not yet carry P.L. 119-21.)*

## 3. The limit as Treasury carries it - Monthly Statement of the Public Debt, table 2

Source: `treasury_mspd_table_2_2024-12_to_2026-08.json` (Treasury Fiscal Data API, amounts in USD millions as given).

| Month-end | Statutory Debt Limit ($ mil) | Total Public Debt Subject to Limit ($ mil) | Balance ($ mil) |
|---|---|---|---|
| 2024-12-31 | 0 (suspended) | 36,103,970.6509215 | 0 |
| 2025-01-31 | 36,103,995.660231 | 36,103,970.6303193 | 25.0299117341638 |
| 2025-06-30 | 36,103,995.660231 | 36,103,970.6292302 | 25.0310008078814 |
| 2025-07-31 | **41,103,995.660231** | 36,803,012.8598038 | 4,300,981.80042721 |
| 2025-08-31 | 41,103,995.660231 | 37,161,539.6250034 | 3,942,456.03522759 |
| 2026-06-30 | 41,103,995.660231 | 39,287,682.9203162 | 1,816,312.73991475 |
| 2026-07-31 | 41,103,995.660231 | 39,588,824.7711909 | 1,515,170.88904006 |
| 2026-08-31 | 41,103,995.660231 | **39,989,942.1672381** | **1,114,054.49299288** |

*Read: the limit reinstated on 2 Jan 2025 was **$36,103,995,660,231**. With P.L. 119-21's $5,000,000,000,000 it is **$41,103,995,660,231**,
carried unchanged from July 2025 to August 2026, the last month published. From January to June 2025 debt subject to limit sat about $25
million under the limit: that is the extraordinary-measures period. On 31 Aug 2026 debt subject to limit was $39.99 trillion, $1.114 trillion
below the limit.*

## 4. CRS In Focus IF10292, *The Debt Limit* - date, projection, extraordinary measures

Source: `crs_IF10292.22.pdf`, from congress.gov's CRS file store (`.../IF10292/IF10292.22.pdf`). Page 1 reads **"Updated September 21,
2026"** and the footer **"IF10292 · VERSION 22 · UPDATED"**. The author line is Grant A. Driessen, Acting Section Research Manager. The file
is byte-identical to EveryCRSReport's copy dated 2026-09-21. The previous edition, version 21 (`crs_IF10292.21.pdf`, "Updated December 5, 2025"), is kept
for comparison. It already has the "approach the debt limit sometime in FY2027" sentence. **The "Outside projections from June 2026 ... early
to mid-2027" paragraph is new in version 22.**

> The debt limit was increased by $5.0 trillion, to $41.1 trillion, in July 2025 by P.L. 119-21.
>
> *Gloss: the CRS's rounded figures, matching section 72001 and Treasury's $41,103,995,660,231.*

> Current law provides a nominal debt limit value that exceeds debt subject to limit. Recent projections suggest such borrowing will approach
> the debt limit sometime in FY2027, though such estimates are subject to considerable uncertainty (as discussed below).
>
> *Gloss: borrowing is projected to **approach** the limit sometime in **FY2027** (1 Oct 2026 - 30 Sep 2027).*

> Debt subject to limit was $40.0 trillion as of August 31, 2025, about $1.1 trillion below the debt limit. Outside projections from June 2026
> estimated that the debt would reach the limit in early to mid-2027, with extraordinary measures delaying a binding debt limit by another
> several months should they be implemented.
>
> *Gloss: **outside projections (June 2026) put the debt reaching the limit in early to mid-2027, and extraordinary measures would then
> delay a binding limit by several more months.** The "August 31, 2025" is a slip in the CRS text for 2026. Treasury's MSPD (section 3) gives
> $39.99 trillion and a $1.114 trillion balance on 31 Aug **2026**; on 31 Aug 2025 the figures were $37.16 trillion and a $3.94 trillion
> balance.*

> The authority for using such extraordinary measures, which include suspensions and delays of some debt sales and auctions, underinvestment
> and disinvestment of certain government funds, and exchange of debt securities for debt not subject to the debt limit, rests with the
> Treasury Secretary. Invocation of extraordinary measures has delayed required action on the debt limit by periods ranging from a few weeks
> to several months.
>
> *Gloss: extraordinary measures are the Treasury Secretary's to invoke. They include suspending or delaying some debt sales and auctions,
> under- and disinvesting certain government funds, and swapping debt for debt not subject to the limit. **Historically they have bought
> "a few weeks to several months".***

> When debt levels approach the statutory debt limit, Congress may choose to (1) leave the existing debt limit in place; (2) increase the debt
> limit to allow for further federal borrowing; or (3) temporarily suspend or abolish the debt limit. Maintaining the current debt limit may
> lead Treasury to implement "extraordinary measures" to postpone a binding debt limit. Such measures, however, do not prevent a binding debt
> limit indefinitely.
>
> *Gloss: Congress's three options are keep, raise, or suspend/abolish the limit. Extraordinary measures only postpone a binding limit.*

> P.L. 119-21, the 2025 reconciliation law, increased the debt limit by $5.0 trillion, to $41.1 trillion. Prior to enactment of that law,
> Treasury had been implementing extraordinary measures for several months to prevent a binding debt limit.

## 5. Against the ruling

| Ruling | As read | Note |
|---|---|---|
| $41.1 trillion since P.L. 119-21 (July 2025) | P.L. 119-21 approved **4 July 2025**; Treasury carries **$41,103,995,660,231** from the July 2025 statement | agrees. The statute itself only adds $5,000,000,000,000 to the limit reinstated on 2 Jan 2025 ($36,103,995,660,231) |
| +$5.0 trillion | "is increased by $5,000,000,000,000" (§ 72001, 139 Stat. 332) | agrees |
| 31 U.S.C. 3101(b) as amended | § 3101(b)'s own text still says $14,294,000,000,000 | **differs in form**: the $41.1 trillion lives in notes (P.L. 118-5 § 401(b) + P.L. 119-21 § 72001), not in § 3101(b)'s text |
| extraordinary measures buy a few months | CRS: "a few weeks to several months"; for the next episode, "another several months" | agrees in substance. The CRS range starts at a few weeks |
| Congress raises or suspends the limit | CRS: leave in place, increase, or "temporarily suspend or abolish" | agrees. The CRS also lists abolition. "Forces spending down to revenue" is the game's own rule; the CRS only names "conflicting directives" and possible consequences of a binding limit |
| IF10292 (21 Sep 2026) | "Updated September 21, 2026", version 22 | agrees |
| projects the limit **binding** in early to mid 2027 | the CRS reports **outside projections from June 2026** that debt would **reach** the limit in early to mid-2027, with extraordinary measures delaying a **binding** limit "by another several months"; its own text says borrowing will "approach the debt limit sometime in FY2027" | **differs**. Early to mid-2027 is when debt *reaches* the limit; it *binds* several months later if extraordinary measures are used. The projection is cited by the CRS, not made by it |

## 6. The bridge between the seed and debt subject to limit (US-14; PN-4: same year, same perimeter)

Treasury's perimeters at the seed's vintage are MSPD table 1's "Total Public Debt Outstanding", split into "Debt Held by the Public" and
"Intragovernmental Holdings" (`raw/treasury_debt/treasury_mspd_table_1_2024-12-31_and_2026-06-30.json`), and MSPD table 2's "Total Public Debt
Subject to Limit" - the statutory perimeter, total public debt outstanding less the debt not subject to the limit (chiefly the unamortized
discount) plus guaranteed debt (DTS table III-C's lines). The month read is the last month-end before `WorldClock.SeedVintage` (`UsDebtLimitCheck` holds the two together).

The seed's debt is the USA's `GovernmentDebt` as `WorldFactory.CreateDefault` builds it; `WorldFactory` states no perimeter for it. Its figure, its
residual against each Treasury perimeter at that month-end and against debt subject to limit at the 2 January 2025 reinstatement, and the ratio R-US21
(i)(a) recommends, are the engine block of `docs/generated/US_DEBT_LIMIT.md` (written by `UsDebtLimitCheck.WriteBridge`; the cheap bar fails while it
differs). *The plan's premise 5 read the gap at "about $4.0 trillion"; the bridge states its own residual at the seed's vintage and does not inherit
that figure.*

## 7. The statute as dated data (US-14)

The limit's steps, each from its act's text and Treasury's own tables, are generated in `docs/generated/US_DEBT_LIMIT.md` ("The limit by date").

> That the limitation under section 3101(b) of title 31, United States Code, as most recently increased by Public Law 117-50 (31 U.S.C. 3101
> note), is increased by $2,500,000,000,000.
>
> Approved December 16, 2021.
>
> *Gloss: P.L. 117-73 (S.J. Res. 33), `raw/budget_statutes/govinfo_PLAW-117publ73.htm`. The limit in force from 16 December 2021 until the suspension - a
> start the world opens before 3 June 2023 (`WorldClock.StartDate`) opens under it; MSPD table 2 carries it from 31 December 2021 to 31 May 2023.*

> (a) [...] In General.--Section 3101(b) of title 31, United States Code, shall not apply for the period beginning on the date of the enactment of this Act
> and ending on January 1, 2025.
>
> (b) Special Rule Relating to Obligations Issued During Extension Period.-- [...] Effective on January 2, 2025, the limitation in effect under section
> 3101(b) of title 31, United States Code, shall be increased to the extent that-- (1) the face amount of obligations issued under chapter 31 of such
> title and the face amount of obligations whose principal and interest are guaranteed by the United States Government (except guaranteed
> obligations held by the Secretary of the Treasury) outstanding on January 2, 2025, exceeds (2) the face amount of such obligations outstanding on
> the date of the enactment of this Act.
>
> (c) [...] (1) [...] An obligation shall not be taken into account under subsection (b)(1) unless the issuance of such obligation was necessary to
> fund a commitment incurred pursuant to law by the Federal Government that required payment before January 2, 2025.
>
> *Gloss: P.L. 118-5 s401, the GPO slip law (`raw/budget_statutes/govinfo_PLAW-118publ5.htm`, approved June 3, 2023). (c)(1) keeps the
> reinstatement from counting borrowing the government did not need - the game holds no cash balance to stockpile, so its own debt is the
> necessary one.*

> On January 2, 2025, the new debt limit will be established at the amount of outstanding debt subject to the statutory limit at the end of the
> previous day. However, on January 2, the outstanding debt subject to the limit is projected to decrease by approximately $54 billion, mostly due
> to a scheduled redemption of nonmarketable securities held by a federal trust fund associated with Medicare payments.
>
> *Gloss: the Secretary's letter of 27 December 2024 (`raw/treasury_debt/treasury_letter_2024-12-27_yellen.pdf`, read from the page image - no text
> layer). **"On January 2" is read as the end of 1 January**, so the reinstatement takes 2 January's OPENING. The reproduction in the generated
> document follows it: the limit in force + 2 January 2025's opening - the close of the last business day before enactment (Friday 2 June 2023;
> 3 June was a Saturday), from the Daily Treasury Statement, comes to Treasury's figure within the DTS's whole-million rounding; 2 January's close
> would not. To the dollar, Treasury's figure is its own: the DTS publishes whole millions, and MSPD table 2 has no 2 June 2023 column.*

## 8. The record's extraordinary measures (US-14, for R-US21 (iii))

> On January 21, 2025, the outstanding debt subject to the limit reached the statutory debt limit. On March 14, 2025, the Secretary determined the
> DISP would continue through June 27, 2025. On June 25, 2025, the Secretary determined the DISP would continue through July 24, 2025. On July 4,
> 2025, Public Law 119-21 increased the statutory debt limit by $5 trillion, rendering further use of this authority unnecessary.
>
> *Gloss: Treasury's report on the CSRDF and PSRHBF, 30 January 2026 (`raw/treasury_debt/treasury_2025_csrdf_report.pdf`, text layer). **The record's
> last episode: reached 21 January 2025, ended 4 July 2025 by the increase** - the span the measures were used, not the span they could have
> lasted. The DTS shows the debt subject to limit pinned within Treasury's working margin of the limit from 21 January (and well below it on
> 17 January, the business day before).*

> This letter serves to notify you, pursuant to 5 U.S.C. § 8348(l)(2), of the extraordinary measures that Treasury will begin using on January 21.
> [...] The period of time that extraordinary measures may last is subject to considerable uncertainty, including the challenges of forecasting
> the payments and receipts of the U.S. Government months into the future.
>
> *Gloss: the Secretary's letter of 17 January 2025 (page image).*

> However, after reviewing receipts from the recent April tax filing season, there is a reasonable probability that the federal government's cash
> and extraordinary measures will be exhausted in August while Congress is scheduled to be in recess. Therefore, I respectfully urge Congress to
> increase or suspend the debt limit by mid-July, before its scheduled break [...]
>
> *Gloss: the Secretary's letter of 9 May 2025 (page image). **Treasury's projection of when the measures would have run out: August 2025** - a
> month, so the span to it is a range (its first and last days). The letter of 25 June 2025 (page image) extends the DISP "through July 24, 2025".*

> DEBT SUBJECT TO LIMIT EXTRAORDINARY MEASURES ($ billions) [...] January 21, 2025 19 (12) 6 299 0 299 20 0 20 338 (12) 325
>
> *Gloss: Treasury's daily table of the measures, 3 July 2025 (`raw/treasury_debt/treasury_dsl_activity_2025-07-03.pdf`, text layer), the first
> row: CSRDF/PSRHBF, G Fund and ESF, each "Total Measures / Measures Used / Measures Remaining", then the totals. **The measures' size on the
> episode's first day, in dollars** - a reading of (iii) the plan does not list, measured beside the days (the game holds no cash balance, which
> the X-date also draws on).*

> In my letter of January 13, 2023, I noted that Public Law 117-73 increased the statutory debt limit to a level of $31.381 trillion, and informed
> you that beginning on January 19, the outstanding debt of the United States was projected to reach the statutory limit. This letter serves to
> notify you, pursuant to 5 U.S.C. § 8348(l)(2), of the extraordinary measures Treasury began using today.
>
> [...] we now estimate that Treasury will have insufficient resources to satisfy the government's obligations if Congress has not raised or
> suspended the debt limit by June 5.
>
> *Gloss: the Secretary's letters of 19 January and 26 May 2023 (page images) - the episode before: reached 19 January 2023, projected to run out
> on 5 June, ended by P.L. 118-5's suspension on 3 June. Both episodes sit inside the CRS's "a few weeks to several months" (section 4). The day
> counts of both are generated in `docs/generated/US_DEBT_LIMIT.md`.*

## 9. The budget statutes, read for US-22 (United States Code, 2024 edition, GPO - `raw/budget_statutes/`)

> The fiscal year of the Treasury begins on October 1 of each year and ends on September 30 of the following year.
>
> *Gloss: 31 U.S.C. 1102.*

> On or after the first Monday in January but not later than the first Monday in February of each year, the President shall submit a budget of the
> United States Government for the following fiscal year.
>
> *Gloss: 31 U.S.C. 1105(a).*

> First Monday in February - President submits his budget. [...] April 15 - Congress completes action on concurrent resolution on the budget.
> [...] June 15 - Congress completes action on reconciliation legislation. June 30 - House completes action on annual appropriation bills. [...]
> October 1 - Fiscal year begins.
>
> *Gloss: 2 U.S.C. 631, the timetable (excerpted rows). 2 U.S.C. 632(a): "On or before April 15 of each year, the Congress shall complete action on a
> concurrent resolution on the budget for the fiscal year beginning on October 1 of such year." The dates are the statute's targets; the record
> misses them (P.L. 119-4 funded a whole fiscal year by continuing resolution, section 10).*

> (a)(1) Except as specified in this subchapter or any other provision of law, an officer or employee of the United States Government [...] may
> not— (A) make or authorize an expenditure or obligation exceeding an amount available in an appropriation or fund for the expenditure or
> obligation; (B) involve either government in a contract or obligation for the payment of money before an appropriation is made unless authorized
> by law;
>
> *Gloss: 31 U.S.C. 1341(a)(1), the Antideficiency Act - with no appropriation, no obligation: the shutdown R-US9 (F8) rules in.*

> (2) Each employee of the United States Government [...] furloughed as a result of a covered lapse in appropriations shall be paid for the period
> of the lapse in appropriations, and each excepted employee who is required to perform work during a covered lapse in appropriations shall be paid
> for such work, at the employee's standard rate of pay, at the earliest date possible after the lapse in appropriations ends, regardless of
> scheduled pay dates, and subject to the enactment of appropriations Acts ending the lapse.
>
> *Gloss: 31 U.S.C. 1341(c)(2), for a lapse beginning on or after 22 December 2018 - the pay is delayed, not lost; CBO's letter (section 10) counts
> it as delayed spending made up when appropriations resume.*

> An officer or employee of the United States Government [...] may not accept voluntary services for either government or employ personal services
> exceeding that authorized by law except for emergencies involving the safety of human life or the protection of property.
>
> *Gloss: 31 U.S.C. 1342 - the emergencies exception is where "excepted" work comes from.*

> Sections 305(b)(2), 305(c)(4), 306, 310(d)(2), 313, 904(c), and 904(d) of this Act [sections 636(b)(2), (c)(4), 637, 641(d)(2), and 644 of this
> title and subsecs. (c) and (d) of this note] may be waived or suspended in the Senate only by the affirmative vote of three-fifths of the Members,
> duly chosen and sworn.
>
> *Gloss: the Congressional Budget Act's s904(c)(1), set out in the note to 2 U.S.C. 621; s904(d)(2) requires the same three fifths to sustain an
> appeal of the Chair's ruling on those points of order. **The Byrd rule (s313, 2 U.S.C. 644) is waived under s904(c)(1) - the pointer G10 gave as
> "s644(e)" is corrected to the note to 2 U.S.C. 621.***

> Any amount of budget authority proposed to be rescinded or that is to be reserved as set forth in such special message shall be made available
> for obligation unless, within the prescribed 45-day period, the Congress has completed action on a rescission bill [...]
>
> *Gloss: 2 U.S.C. 683(b) (the Impoundment Control Act, read from 681); 684 governs proposed deferrals by special message.*

> Within 15 calendar days after Congress adjourns to end a session there shall be a sequestration to eliminate a budget-year breach, if any, within
> any category. [...] Each non-exempt account within a category shall be reduced by a dollar amount calculated by multiplying the enacted level of
> sequestrable budgetary resources in that account at that time by the uniform percentage necessary to eliminate a breach within that category.
>
> *Gloss: 2 U.S.C. 901(a)(1)-(2) - the discretionary limits' sequester, sized to the breach. **The AI ministry's comment cites "2 U.S.C. s 902" for
> the sequester sized to a cap's breach; in the 2024 edition s902 is the pay-as-you-go sequester ("any legislation enacted before October 1, 2002,
> affecting direct spending or receipts that increases the deficit will trigger an offsetting sequestration") and the cap's breach is s901.**
> Recorded for US-24 and R-US21 (v); `AiFinanceMinistry` is not touched here.*

## 10. The lapse of record (US-14, for R-US9 as ruled, F8)

> Making further continuing appropriations and other extensions for the fiscal year ending September 30, 2025, and for other purposes.
>
> *Gloss: P.L. 119-4, the "Full-Year Continuing Appropriations and Extensions Act, 2025", approved March 15, 2025 (`raw/lapse/govinfo_PLAW-119publ4.htm`,
> byte-identical to the out-of-tree capture of 2026-10-05).*

> Failed of passage/not agreed to in Senate: Under the order of 9/18/2025, not having achieved 60 votes in the affirmative, failed of passage in
> Senate by Yea-Nay Vote. 44 - 48. Record Vote Number: 528. (2025-09-19)
>
> Cloture on the motion to proceed to the measure not invoked in Senate by Yea-Nay Vote. 55 - 45. Record Vote Number: 537. (2025-10-01)
>
> Upon reconsideration, the fifth cloture motion on the motion to proceed to the measure invoked in Senate by Yea-Nay Vote. 60 - 40. Record Vote
> Number: 610. (2025-11-09)
>
> Passed Senate with an amendment by Yea-Nay Vote. 60 - 40. Record Vote Number: 618. (2025-11-10)
>
> On motion that the House agree to the Senate amendment Agreed to by the Yeas and Nays: 222 - 209 (Roll no. 285). (2025-11-12)
>
> Became Public Law No: 119-37. (2025-11-12)
>
> *Gloss: H.R. 5371's actions (`raw/lapse/govinfo_BILLSTATUS-119hr5371.xml`, byte-identical to the 2026-10-05 capture; the House passed it on 19
> September 2025, 217 - 212). The continuing resolution was a bill of its own and needed sixty votes in the Senate; it became P.L. 119-37, the
> "Continuing Appropriations, Agriculture, Legislative Branch, Military Construction and Veterans Affairs, and Extensions Act, 2026", approved
> November 12, 2025, its continuing appropriations available until "January 30, 2026" (s106(3)) at the latest.*

> The federal government experienced a funding gap beginning on October 1, 2025--the start of FY2026--and ending when the Continuing Appropriations,
> Agriculture, Legislative Branch, Military Construction and Veterans Affairs, and Extensions Act, 2026 (P.L. 119-37), was signed into law on
> November 12, 2025. [...] In the 2025 shutdown, funding lapsed for all appropriated programs and agencies--CRS could not locate any authoritative
> information on the total number of programs and agencies that were excepted--and the shutdown lasted for six weeks.
>
> *Gloss: CRS R48832, version 2, 29 January 2026 (`raw/lapse/crs_R48832.2.pdf`).*

> Using information from federal agencies' contingency plans and the Office of Personnel Management (OPM), CBO estimates that under the lapse in
> appropriations, about 600,000 employees will be considered excepted and continue to work each week, and 650,000 employees will be furloughed, on
> average.
>
> During the shutdown, federal outlays will be lower than they would have been otherwise: by $33 billion in the four-week shutdown scenario,
> $54 billion in the six-week shutdown scenario, and $74 billion in the eight-week shutdown scenario [...] CBO expects that when appropriations
> resume, the spending that did not occur during the shutdown will be made up.
>
> *Gloss: CBO's letter of 29 October 2025 to the House Budget Committee's chairman (`raw/lapse/house_arrington_61823_quantitative_effects_of_a_shutdown.pdf`,
> the addressee's copy - cbo.gov refused with a captcha). **THE EXCEPTED SHARE OF DISCRETIONARY SPENDING IN A LAPSE IS BILLED**: no primary read
> states it, and the CRS says no authoritative count of what was excepted exists. The two nearest figures are CBO's - a headcount (excepted against
> furloughed employees; active-duty military are paid as scheduled and outside both) and the outlays a lapse delays - and neither is a share of
> discretionary spending without a reading that turns it into one. OMB's agency contingency plans page now redirects to a page listing only the
> Executive Office of the President's plans (`raw/lapse/fetch_log.md`). US-22 asks Elias (sweep plan, section 5, item 8).*

## 11. How every episode of record ended (US-14, for R-US21 (vi))

> Congress has always acted when called upon to raise the debt limit. Since 1960, Congress has acted 78 separate times to permanently raise,
> temporarily extend, or revise the definition of the debt limit – 49 times under Republican presidents and 29 times under Democratic presidents.
>
> *Gloss: Treasury's Debt Limit page (`raw/treasury_debt/treasury_debt_limit_page.html`, fetched 2026-10-16; the page carries no date for the count).
> The two latest episodes, sections 7 and 8: a suspension (P.L. 118-5, 2023) and an increase (P.L. 119-21, 2025), each before the measures ran out.*
