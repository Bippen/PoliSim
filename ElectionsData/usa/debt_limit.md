# USA - the statutory debt limit (item B7)

Sourced 2026-10-02 for the designer's ruling: *"The limit is $41.1 trillion since P.L. 119-21 (July 2025, +$5.0 trillion). At the limit,
extraordinary measures buy a few months; after that Congress raises or suspends the limit, or the game forces spending down to revenue. CRS
IF10292 (21 Sep 2026) projects the limit binding in early to mid 2027."*

All files are in `raw/debt_limit/` (SHA-256 in its `SHA256SUMS.txt`). A gloss in italics follows each quotation.

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
