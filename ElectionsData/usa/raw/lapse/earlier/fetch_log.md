# fetch_log - ElectionsData/usa/raw/lapse/earlier/ (PS-6 US-22c, Elias's ruling J7 (iii): every lapse of record with lapse-period cloture votes and an identifiable withholder joins the withholder's fit), 2026-10-20

The Senate's own roll-call lists (LIS XML, one per session) for every session a lapse of record since the 101st Congress falls in, the Senate's own roll calls (LIS XML) for every lapse-period cloture vote they list on a funding measure and the votes beside them that a reading below turns on, and the public laws that bound each lapse. `senate.gov` answers 403 directly (`ElectionsData/usa/veto_record.md`), so the Senate's files are read through the Internet Archive's raw replay (`id_`), each named by its capture; govinfo serves directly.

Commands: the Senate's - `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -o <file> "https://web.archive.org/web/20261018000000id_/<url>"` (the closest capture); govinfo's - the same agent with `-H "Accept: text/html,application/xhtml+xml,application/xml,*/*"`, direct. Bytes stored as received; digests in `SHA256SUMS.txt`.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-19T23:57:39Z | 200 | 23605 | `vote_113_1_00216.xml` | https://www.senate.gov/legislative/LIS/roll_call_votes/vote1131/vote_113_1_00216.xml (capture 20260719000703) |
| 2026-10-19T23:57:44Z | 200 | 23507 | `vote_113_1_00218.xml` | https://www.senate.gov/legislative/LIS/roll_call_votes/vote1131/vote_113_1_00218.xml (capture 20260718234416) |
| 2026-10-19T23:57:50Z | 200 | 29028 | `vote_115_2_00014.xml` | https://www.senate.gov/legislative/LIS/roll_call_votes/vote1152/vote_115_2_00014.xml (capture 20260719001508) |
| 2026-10-19T23:57:55Z | 200 | 29077 | `vote_115_2_00016.xml` | https://www.senate.gov/legislative/LIS/roll_call_votes/vote1152/vote_115_2_00016.xml (capture 20260719001508) |
| 2026-10-19T23:58:01Z | 200 | 28946 | `vote_115_2_00030.xml` | https://www.senate.gov/legislative/LIS/roll_call_votes/vote1152/vote_115_2_00030.xml (capture 20260719001442) |
| 2026-10-19T23:58:06Z | 200 | 28655 | `vote_116_1_00009.xml` | https://www.senate.gov/legislative/LIS/roll_call_votes/vote1161/vote_116_1_00009.xml (capture 20260718233126) |
| 2026-10-19T23:58:13Z | 200 | 28667 | `vote_116_1_00010.xml` | https://www.senate.gov/legislative/LIS/roll_call_votes/vote1161/vote_116_1_00010.xml (capture 20260718233124) |
| 2026-10-20T00:05:43Z | 200 | 163133 | `vote_menu_101_2.xml` | https://www.senate.gov/legislative/LIS/roll_call_lists/vote_menu_101_2.xml (capture 20260121095815) |
| 2026-10-20T00:05:50Z | 200 | 313026 | `vote_menu_104_1.xml` | https://www.senate.gov/legislative/LIS/roll_call_lists/vote_menu_104_1.xml (capture 20250508162847) |
| 2026-10-20T00:05:56Z | 200 | 158121 | `vote_menu_104_2.xml` | https://www.senate.gov/legislative/LIS/roll_call_lists/vote_menu_104_2.xml (capture 20250328002646) |
| 2026-10-20T00:06:03Z | 200 | 138085 | `vote_menu_113_1.xml` | https://www.senate.gov/legislative/LIS/roll_call_lists/vote_menu_113_1.xml (capture 20250323100423) |
| 2026-10-20T00:06:09Z | 200 | 133079 | `vote_menu_115_2.xml` | https://www.senate.gov/legislative/LIS/roll_call_lists/vote_menu_115_2.xml (capture 20250621083708) |
| 2026-10-20T00:06:15Z | 200 | 201594 | `vote_menu_116_1.xml` | https://www.senate.gov/legislative/LIS/roll_call_lists/vote_menu_116_1.xml (capture 20250805112522) |
| 2026-10-20T00:08:20Z | 200 | 48761 | `govinfo_PLAW-113publ46.htm` | https://www.govinfo.gov/content/pkg/PLAW-113publ46/html/PLAW-113publ46.htm |
| 2026-10-20T00:08:21Z | 200 | 33116 | `govinfo_PLAW-115publ96.htm` | https://www.govinfo.gov/content/pkg/PLAW-115publ96/html/PLAW-115publ96.htm |
| 2026-10-20T00:08:23Z | 200 | 42168 | `govinfo_PLAW-115publ120.htm` | https://www.govinfo.gov/content/pkg/PLAW-115publ120/html/PLAW-115publ120.htm |
| 2026-10-20T00:08:25Z | 200 | 909989 | `govinfo_PLAW-115publ123.htm` | https://www.govinfo.gov/content/pkg/PLAW-115publ123/html/PLAW-115publ123.htm |
| 2026-10-20T00:08:27Z | 200 | 1351 | `govinfo_PLAW-115publ298.htm` | https://www.govinfo.gov/content/pkg/PLAW-115publ298/html/PLAW-115publ298.htm |
| 2026-10-20T00:08:28Z | 200 | 6848 | `govinfo_PLAW-116publ5.htm` | https://www.govinfo.gov/content/pkg/PLAW-116publ5/html/PLAW-116publ5.htm |

Each session's list was first fetched at 2026-10-19T23:56Z into a scratch folder and fetched again into this folder at the times above; the two fetches are byte-identical.

## The lapses read, and what each gives the fit

The lapses are those since the 101st Congress (1989), where the Senate's LIS record begins. Each window runs from the day after the funding horizon to the day the law that ended the lapse was approved.

| lapse | bounded by | lapse-period Senate cloture votes on a funding measure (the session's list) | withholder | joins the fit |
|---|---|---|---|---|
| October 1990 | (the 101st's list; the window as commonly dated, 6-8 Oct - its laws not read) | none from 20 Sep to 11 Oct; the next, 12 Oct, is cloture on a committee amendment to a regular appropriations bill (H.R. 5114), after the lapse | - | no: no cloture vote |
| November 1995 | (the 104th's list; the window as commonly dated, 14-19 Nov - its laws not read) | none in November or December 1995 - H.J.Res. 122 passed 60-37 on 16 Nov without cloture | - | no: no cloture vote |
| December 1995 - January 1996 | (the 104th's two lists; the window as commonly dated, 16 Dec - 6 Jan - its laws not read) | none in December 1995 or January 1996 | - | no: no cloture vote |
| 1-16 Oct 2013 | P.L. 113-46 (approved 17 Oct 2013; pay "considered to have begun on October 1, 2013") | roll 218, 16 Oct: cloture on H.R. 2775 as amended, 83-16 (roll 216, 12 Oct, is S. 1569, the debt limit - not a funding measure) | REP: the party out of the White House, whose House majority sent H.J.Res. 59 back with its amendments, which the Senate's majority tabled 54-46 on 30 Sep and 1 Oct (rolls 210-212) | yes: REP's yeas 29 of 45 |
| 20-22 Jan 2018 | P.L. 115-96 (through "January 19, 2018"); P.L. 115-120 (approved 22 Jan 2018) | roll 16, 22 Jan: cloture on the motion to concur on H.R. 195, 81-18 (roll 14, 19 Jan at 10:14 PM, is before the lapse) | DEM: the out-party, the D caucus 5 yeas of 49 at roll 14's failed cloture the night before | yes: the D caucus's yeas 33 of 49 |
| 9 Feb 2018 | P.L. 115-120 (through "February 8, 2018"); P.L. 115-123 (approved 9 Feb 2018) | roll 30, 9 Feb at 12:59 AM: cloture on the motion to concur on H.R. 1892, 73-26 | NONE: each caucus's majority voted for it (D caucus 38 of 49, REP 35 of 50); no party withheld the measure | no: no withholder |
| 22 Dec 2018 - 25 Jan 2019 | P.L. 115-298 (through "December 21, 2018"); P.L. 116-5 (approved 25 Jan 2019) | the 115th's list holds no vote after 21 Dec; roll 9, 24 Jan 2019: cloture on Shelby's amendment No. 5 to H.R. 268, 50-47; roll 10: cloture on Schumer's amendment No. 6, 52-44 | DEM against the measure of the president's party (roll 9: REP 49 of 51 for, the D caucus 1 of 46); roll 10 is the out-party's own alternative, not read (the 2025 reading) | yes: the D caucus's yeas 1 of 46 (roll 9) |
| 1 Oct - 12 Nov 2025 | `../SHA256SUMS.txt` (P.L. 119-4, P.L. 119-37) | the 13 cloture motions to proceed to H.R. 5371 (`../senate/`) | DEM | yes, as before |

Readings, DECLARED (`Assets/Scripts/Elections/UsLapseDraw.cs`): the withholder is the party out of the White House that stood against the funding measure of the president's side, identified from the lapse's own record (above); one observation per lapse-period cloture vote on that measure; the measure's alternatives offered by the withholder are not read. The lapses before 1989 are **BILLED**: the Senate's LIS roll-call record begins with the 101st Congress, and the Congressional Record's earlier roll calls were not read in this item. CRS's list of funding gaps (RS20348) is **BILLED** too: crsreports.congress.gov answered 403 with its challenge page and congress.gov's `crs_external_products` path answered 404 for every version tried (RS20348.1 to .30), so the set of lapses since 1989 is the one the lists and the laws above show, not a list read whole.
