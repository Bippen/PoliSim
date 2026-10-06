# Review - §788, PS-6 US-5 part one: the House vote by state from the Clerk's statistics, the FEC's tables beside it, 2024's candidates, and US-5's design (2026-10-06)

A workflow review (`polisim-staged-review`) of the change that sources US-5: `Tools/us_returns_prep.pl` extended (the Clerk of the House's recapitulation 2016-2024 read by `pdftotext -raw`, the FEC's House-by-party tables 2016-2022 read beside it as a cross-check, 2024's candidates by the FEC workbook's columns, `returns_2024.md`'s typed lines held to the pages), its two new CSVs and the catalog's two new arrays, `GeneratedCatalogCheck`'s new block, nine new mutation cases, the committed `elec<N>` filter, the record's readings 13 and 14, and US-5's design DECLARED in the plan before any code. Not required by the tier (`Tools/bar_tier.ps1`: SIMULATION, no money, no sentinel); run because the House series is R-US15's evidence and the catalog stands on parsers. One pass; every finding was put to a refute-first skeptic, who re-graded it; every report below is verbatim.

## The first pass - confirmed (verbatim)

The workflow `polisim-staged-review` (run wf_077d8335-dbb, 37 agents) over the staged diff at HEAD 43169a7a: four lenses (parse correctness, mutation coverage, records and claims, the catalog check and generated code), every finding put to a refute-first skeptic. Every report below is verbatim.

### 1. Staged mutation h01 never applies: its `$` anchor cannot match before the extractor's CR

- **Lens:** parse - **reviewer:** defect - **skeptic:** defect
- **Where:** G:/UNITY/Projects/PoliSim/Tools/us_returns_mutations.sh:102

**The scenario.** pdftotext 4.06 (/mingw64/bin) writes CRLF line endings, including on a pipe (clerk_statistics2018.pdf through perl's list-form open: 4472 CRLF, 0 bare LF). h01's program is `s{^(Alabama \.+ 678,687 975,737 .*?) 5,471 1,659,895$}{$1 5,472 1,659,895}m`, which needs `$` right after "1,659,895". The line actually ends "1,659,895\r\n", and under /m `$` matches only before "\n", so the substitution never fires. I reproduced the case exactly (the script's wrap() with h01's program): the tool exits 0 and writes all six outputs. I then ran the STAGED mutation script (`git show :Tools/us_returns_mutations.sh`) against a scratch root holding the index's tool and inputs. It printed `MISSED  a Clerk row's cell changed, its Total kept (2018, Alabama) (exit 0, 6 file(s) written)` and ended `caught 37, missed or broken 1`, exit 1. So in the change as staged, the Clerk row-sum refusal (us_returns_prep.pl:836) is unproved and the script fails its own bar. Someone has already fixed the line in the working tree (`1,659,895(\r?)$}{$1 5,472 1,659,895$2}m`, "the extractor's lines may end CRLF"), but that fix is UNSTAGED (`MM Tools/us_returns_mutations.sh`), and `git diff --cached` still holds the broken line.

**The fix proposed.** Stage the working-tree line (git add Tools/us_returns_mutations.sh) before committing. I ran that variant on its own: exit 255, printing `returns/clerk_statistics2018.pdf AL: its columns sum to 1659896, its Total is 1659895`, which contains the case's expected text. A full run of the repository's script, which read the edited h01 before reaching it, reported caught 38, missed 0. president_returns.md's mutation paragraph still records only the 2026-10-05 tally of twenty-nine; record the 38-case run once it is staged.

**The skeptic's evidence.** I tried to refute it and couldn't. The failure happens in the project's own environment.

1. The staged mutation script, line 102 of Tools/us_returns_mutations.sh in the index (blob 8d1cbbe7):
`h01() { wrap 'BEGIN { undef $/ } s{^(Alabama \.+ 678,687 975,737 .*?) 5,471 1,659,895$}{$1 5,472 1,659,895}m'; }`
Its `wrap()` (staged line 26-28) writes `pdftotext "$@" | perl -pe <prog>`. Nothing strips CR, and Git for Windows' perl (5.42.2, cygwin build) reads STDIN through the `unix,perlio` layers with no `:crlf`.

2. /mingw64/bin/pdftotext is version 4.06 and no xpdfrc exists. Piping `-raw clerk_statistics2018.pdf -` gives CRLF=4472 and bare LF=0. The Alabama row ends `... 5,471 1,659,895\r\n`. Under /m, `$` matches only before `\n`, so `1,659,895$` cannot match.
   - The staged regex leaves the text byte-identical: it does not fire.
   - The same regex fires once CRs are stripped, so CR is the only cause.
   - The working-tree variant `1,659,895(\r?)$ ... $2` fires.

3. The staged tool still reads the Clerk text through `open my $p, '-|', $pdftotext, '-raw', ...` (staged us_returns_prep.pl:805) with no `-eol unix`. It tolerates CRLF itself: `split ' '` drops the trailing `\r` (line 825), and the title check ends in `\s*$` (line 813). So an unmutated text passes cleanly, and the case reports MISSED rather than some other refusal.

4. I ran the staged h01 case through the staged harness functions and the staged tool, in a unique scratch root built from the index (the .cs blob converted to CRLF as autocrlf=true checks it out). Result: `MISSED  a Clerk row's cell changed, its Total kept (2018, Alabama) (exit 0, 6 file(s) written)`. The working-tree h01 on the same staged tool gives `CAUGHT`, so the refusal itself works and only its proof is broken.

5. A full run of the staged script in that root gave: `PASSES the control`, the one MISSED above, `caught 37, missed or broken 1`, exit 1 (the script's own `[ $bad -eq 0 ]`).

6. No other case reaches the row-sum refusal at staged us_returns_prep.pl:836 (`problem("$file $st: its columns sum to $parts, its Total is ...")`):
   - h03 deletes a row and trips the missing-state and column checks.
   - h04 and h05 swap non-Total header names, and the row sums do not change.
   So in the change as staged, the failure path declared at :64-66 ("a state row whose columns do not sum to its own Total") is unproved. That contradicts the header claim at :26-27 that the mutation script proves those paths, and the standing rule that every new failure path is proved by the mutation script.

7. `git status` shows `MM Tools/us_returns_mutations.sh`. `git diff` (unstaged) holds only the h01 hunk, "the extractor's lines may end CRLF". `git diff --cached` still carries the broken line.

Severity: kept as defect. The tool's runtime behaviour is correct, but a commit made from the index would ship a mutation script that fails its own bar (exit 1) and leaves a declared refusal unproved. Mitigation: the fix already exists in the working tree.

Side note, not part of the defect: the staged president_returns.md:37 still records only the 2026-10-05 run ("caught all twenty-nine"). It is historical and not false, but it does not cover the nine h-cases.

**The skeptic's corrected fix.** Stage the working-tree hunk before committing, with `git add Tools/us_returns_mutations.sh`. Only the h01 hunk differs, so this stages that one change. The staged line 102 becomes:
`h01() { wrap 'BEGIN { undef $/ } s{^(Alabama \.+ 678,687 975,737 .*?) 5,471 1,659,895(\r?)$}{$1 5,472 1,659,895$2}m'; }`
An equivalent form is `1,659,895(?=\r?$)` with the replacement unchanged.

Then re-run `bash Tools/us_returns_mutations.sh` on the staged state. Expected result: PASSES the control, caught 38, missed or broken 0, exit 0. I confirmed the fixed h01 is CAUGHT against the staged tool.

Optionally, record that 38-case run in president_returns.md's mutation paragraph, which still states the 2026-10-05 tally of twenty-nine. That is a documentation follow-up, not part of this fix.

### 2. A 'District of Columbia' row on the Clerk's page is skipped without being named, so the DC guard can never fire

- **Lens:** parse - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Tools/us_returns_prep.pl:824

**The scenario.** The row regex `^([A-Z][a-z]+(?: [A-Z][a-z]+)*)\d* (.*)$` ends a name at the first lowercase word. "District of Columbia ...." therefore yields the name "District" with 12 tokens, and line 828 drops the row before it reaches the `$st eq 'DC'` refusal at 832. "U.S. Virgin Islands" fails to match at all. Probe: a DC row (1,000 / 2,000 / ... / 3,000) wrapped into the 2024 page after Delaware gave exit 0, all outputs written and nothing named; the same row titled "Guam" gave `a row 'Guam', no state`. No wrong figure can get through this way: a DC row that the Total row includes fails the column sums at 843 (under a misleading message), and one the Total excludes is rightly ignored. But the advertised refusal "a row naming no state" never fires for DC.

**The fix proposed.** Let names take 'of' and dotted capitals without swallowing footnote digits, e.g. `^([A-Z][A-Za-z.]*(?: (?:of|[A-Z][A-Za-z.]*))*)\d* (.*)$` (not \w, which would glue a footnote digit to the name). Alternatively, name any line whose tail is 9 or 10 figure/dot tokens when its prefix is neither a state nor 'Total'.

**The skeptic's evidence.** The finding is real. I checked the staged file (git show :Tools/us_returns_prep.pl). The working tree has already adopted the proposed regex and added h14/h28, but that is not the review target.

Code paths:
- L824: `next unless $l =~ /^([A-Z][a-z]+(?: [A-Z][a-z]+)*)\d* (.*)$/;` A name stops at the first word that is not of the shape Capital-plus-lowercase.
  - "District of Columbia ......" gives $1 = "District" and 12 tokens (of, Columbia, the leader, 9 cells).
  - "U.S. ..." fails the match at its first character.
- L827 shifts the leader only when there are exactly 10 tokens. L828 `next unless @tok == 9 && ...` then drops the DC row silently.
- L832 `if (!$st || $st eq 'DC') { problem("$file: a row '$name', no state"); next; }` is never reached for DC. L272 shows that %code's only DC key is 'District of Columbia', which the regex can never capture, so the `$st eq 'DC'` test is dead code.
- L66 of the tool's header lists "a row naming no state" among its refusals.
- No staged mutation case (h01-h09) exercises either branch of L832.

Probe of the staged tool on a scratch copy, with pdftotext wrapped the way the mutation script's wrap() does it:
- Control: exit 0; house_by_state.csv, president_by_candidate.csv and president_by_state.csv byte-identical to the index.
- The working tree's h28 text (a DC row inserted after Delaware in 2024, Total unchanged): exit 0, 6 files written, 50 rows for 2024, nothing named. h28 would be MISSED against the staged tool.
- The same DC row with the Total raised to include it: refused, but only through "the states' Republican sums to 74390864, the Total row holds 74391864" and the returns_2024.md lines. DC is never named.
- "Guam": "a row 'Guam', no state".
- "U.S. Virgin Islands": exit 0.

Why the severity is minor:
- All five saved recapitulation pages carry only the 50 states plus Total, and the pages are SHA-pinned, so the committed inputs never hit this path.
- A truncated name leaves its words among the tokens, and the L828 figures-only test rejects them, so a row can never be credited to the wrong state.
- With the DC row skipped, the outputs are the same as the control.
- The real defects are a declared refusal that does not hold, a dead branch, and a misleading message.

**The skeptic's corrected fix.** The proposed regex `^([A-Z][A-Za-z.]*(?: (?:of|[A-Z][A-Za-z.]*))*)\d* (.*)$` is sound but narrow. I probed it through the working-tree tool:
- Control: byte-identical outputs.
- "District of Columbia" and "U.S. Virgin Islands" are named.
- "Commonwealth of the Northern Mariana Islands", or any name with another lowercase word, still exits 0 with all files written.

To close the whole class, anchor each row on its nine trailing cells and name whatever precedes them:
```
my @t = split ' ', $l;
next unless @t >= 10 && !grep { !/^(?:\.+|[\d,]+)$/ } @t[-9 .. -1];
my @n = @t[0 .. $#t - 9]; pop @n if @n > 1 && $n[-1] =~ /^\.+$/;
(my $name = join ' ', @n) =~ s/\d+$//;
my @v = map { /^\.+$/ ? 0 : count($_, "$file $name") } @t[-9 .. -1];
```
After that, refuse every name other than 'Total' that is not a state, as now. This makes the L827 leader shift and the 'State'/'Recapitulation' skips unnecessary.

Verified on all five saved pages: the anchored parse yields the same 51 rows with identical cells as the staged parse.

Keep h28 (the DC row) in the mutation script, and add a case for a name the old regex cannot capture. The staged script proves neither branch of the L832 refusal.

### 3. Most of the new readers' refusals are proved by no mutation case

- **Lens:** parse - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Tools/us_returns_mutations.sh:111

**The scenario.** h01-h09 assert these lines of us_returns_prep.pl: 836 (h01, broken as staged), 813, 841, 876, 892 (twice), 870, 503 with 508, and 810. Nothing reaches: the Clerk header-shape refusal (821) or header-line count (815); a second Total row (830); a row naming no state (832); a state twice (833); no Total row (842); the FEC title (850); the GENERAL ELECTION heading (853); a non-code row (860); the FEC Total-row formula refusal (869); the formula list widened to the candidate columns (481); the label refusal (504); the md section refusals (885-886); the md presidential-row hold (897); and the three-row count (900). For example, deleting the problem() at 897 or 869, or `@cand` from 481, leaves every case CAUGHT. h07 trips 897 for STEIN as a side effect but does not assert it. This falls short of the standing rule that every new failure path is proved.

**The fix proposed.** Add cases at least for the parse-shape guards. Wrap the 2024 header to lose 'Write-in' (821). Poke a `<f>` into an FEC House Total cell (869) or a candidate Total cell (481). Change Harris's figure in returns_2024.md (897; my probe printed `returns_2024.md: HARRIS typed 75017614, the FEC workbook's Total row 75017613`). Delete the Stein row (900).

**The skeptic's evidence.** The finding is correct for the staged change. Line numbers refer to the staged files (git show :Tools/...).

1. What the nine cases prove. case_ (mut_staged.sh:30-39) counts a case as CAUGHT only if every text it asserts is printed. h01-h09 (lines 111-119) assert the texts of prep lines 836, 813, 841, 876, 892 (h05 and h08), 870, 503 with 508, and 810. No case in the staged suite asserts the text of 481, 504, 815, 821, 830, 832, 833, 842, 850, 853, 860, 869, 885, 886, 897 or 900. Four more are also unproved, though the finding did not list them: 843 (the column sums against the Total row), 864 and 872 (an FEC state twice or missing) and 867 (FEC: no Total row). That is 28 new problem() calls, of which 8 are proved by a working case.
   - Two texts look like near-misses and are not. t15's "is a formula, with no typed national figure" is printed by line 441, the 2012 path, which is the only one that fires under t15. r09's "AK: a second row" comes from the state_ev_2024.csv check at 783, not from 833 or 864.
   - So deleting any of the unproved problem() calls leaves every case's outcome unchanged. h07 does trip 897 (STEIN typed 862049 against 862050), but it does not assert it.

2. h01 does nothing as staged. /mingw64/bin/pdftotext 4.06 ends lines with CRLF: in clerk_statistics2018.pdf, all 4472 line ends are \r\n. Under /m, the `1,659,895$` in h01's pattern cannot match before "\r". I rebuilt the wrapper exactly as wrap() builds it (printf %q). Its output and pdftotext's own have the same SHA-256 (9063906b...), and "5,472 1,659,895" never appears. So h01 prints MISSED and the staged suite exits 1, which leaves 836 unproved too.

3. The staged text claims more than the suite proves. prep_staged.pl:26-27 says "Tools/us_returns_mutations.sh proves the failure paths below", and lines 62-71 add the US-5 refusals to that list.

4. A case would have found a dead branch. Line 832's `$st eq 'DC'` clause cannot be reached. The name regex at 824, `^([A-Z][a-z]+(?: [A-Z][a-z]+)*)\d* (.*)$`, reads a "District of Columbia ......" row as the name 'District' with 12 tokens, so line 828 (`@tok == 9`) skips the row without a word. I tested both regexes on a sample row.

Why minor and not defect: no figure is wrong. The checks that guard the live pages (836, 843, 870, 876, 892) are present, and 843 would still catch a skipped DC row that the Total counted.

Not staged: the working tree already has h01 fixed with `(\r?)`, the regex at 824 widened, and h10-h28, which assert every line listed above plus 843, 864, 867 and 872. One gap remains there: 860 ("no state or territory") is still asserted by no case. h21 relabels the FEC Total row as 'WY' (shared string 552), which trips 864 and 867, not 860.

**The skeptic's corrected fix.** 1. Stage the working tree's h01 fix (the `(\r?)` capture, since the extractor's lines end CRLF), the widened name regex at 824, and cases h10-h28. Together they assert 481 (the @cand formula), 504, 815, 821, 830, 832 (Guam, and District of Columbia), 833, 842, 843, 850, 853, 864, 867, 869, 872, 885, 886, 897 and 900.
2. Add the one case still missing, for line 860. For example, poke the 2022 FEC House cell `<c r="A6" s="49" t="s"><v>1184</v></c>` (AK) to `<c r="A6" s="49" t="inlineStr"><is><t>Alaska</t></is></c>`, then resum. Assert "row 6: 'Alaska', no state or territory@@fec_federalelections2022.xlsx: no row for AK".
3. Run bash Tools/us_returns_mutations.sh. Require the control to PASS and every case to be CAUGHT.
4. Bring president_returns.md's mutation paragraph up to date. It still says "caught all twenty-nine" and lists only the US-3 cases; give the new count and the US-5 cases, so the tool header's "proves the failure paths below" is true.

### 4. The candidate-label guard lets control characters through

- **Lens:** parse - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Tools/us_returns_prep.pl:504

**The scenario.** The label is `trim($c->{1}{$k})`, which strips only the ends, and line 504 refuses only comma, quote and backslash. A header cell typed with an in-cell line break (e.g. "WRITE-INS\n(SCATTERED)") would pass. It would be written as two lines of president_by_candidate.csv and as a raw newline inside the Candidates C# string literal, so the tool would write its outputs and Unity's compile (CS1010) or GeneratedCatalogCheck would fail instead of the tool dying. This is latent: all 26 labels in the saved, SHA-pinned workbook are clean ASCII.

**The fix proposed.** Refuse `/[,"\\\x00-\x1f]/` at line 504.

**The skeptic's evidence.** THE CODE UNDER REVIEW (the index; the working tree differs because another session is editing it, unstaged, while this review runs):
- Staged line 504: `problem("$file: the candidate column '$label' holds a comma, a quote or a backslash") if $label =~ /[,"\\]/;`. It does not refuse CR or LF.
- Line 502: label = `trim($c->{1}{$k})`. Line 259: `trim` is `s/^\s+|\s+$//g`, so it strips the ends only.
- In `xlsx_sheet` (line 125), the `<t[^>]*>(.*?)</t>` match uses `/s`, so a raw LF inside the text survives. `ent` (line 119) turns `&#10;` and `&#13;` into LF and CR.
- Nothing later refuses it. `write_all` refuses only `/[^\x00-\x7F]/`. The general C#-literal guard (about lines 903-906) covers only the NARA names and `cast_other_to`, with `/["\\]/`; those strings are whitespace-collapsed or bounded by `.`, so they cannot hold an LF.
- The writers are `$csv_c .= join(',', @$_) . "\n"` and `"($_->[0], \"$_->[1]\", \"$_->[2]\", $_->[3]L)"`.

PROOF (scratch copies in the scratchpad; nothing in the repo was touched):
- Control: the staged tool on unmutated inputs exits 0. All five CSVs are byte-identical to the staged ones, and the .cs matches apart from CRLF.
- Mutation 1: in sharedStrings, `<t>WRITE-INS (SCATTERED)</t>` became `<t>WRITE-INS<LF>(SCATTERED)</t>`, re-pinned in the copy's SHA256SUMS. The staged tool exits 0 and writes all six outputs.
  - president_by_candidate.csv ends `2024,WRITE-INS` and then `(SCATTERED),-,210381` on its own line.
  - UsPresidentialReturns.cs lines 327-328 read `(2024, "WRITE-INS` and then `(SCATTERED)", "-", 210381L),`.
- A C# literal holding a raw line break, compiled with Add-Type, fails with "Ny rad för konstant". That is the sv-SE text of CS1010, Newline in constant.
- The same mutation against the proposed class `/[,"\\\x00-\x1f]/` exits 255 with `MISMATCH: ...the candidate column 'WRITE-INS...` and `1 mismatch(es) - nothing written`, and writes 0 files.
- Mutation 2: an inline-string `&#10;` in cell G1 (`DE LA&#10;CRUZ`). Staged tool: exit 0, 6 files written. Fixed tool: exit 255, 0 files.

WHY MINOR, AND LATENT:
- The pinned workbook (68acdee2...) holds no control character, numeric character reference or `_xHHHH_` escape in any of its 100 shared strings, and all 26 candidate rows are clean.
- Well-formed XML 1.0 can carry only TAB, LF and CR among the C0 control characters. TAB is harmless in both outputs. Non-ASCII line separators (U+0085, U+2028, U+2029) already die at `write_all`. So the real gap is CR and LF.
- The downstream failure is loud (a compile error in the generated file), not silently wrong data. But it breaks the standing rule that the tool dies on any mismatch and writes nothing, and it does so for the very property this guard protects (a CSV cell and a C# literal).

MUTATION COVERAGE:
- The staged us_returns_mutations.sh has no case for this guard at all; h07 is the sum check.
- The unstaged working tree already has exactly the proposed class at line 504 and adds h23 (a comma), but has no line-break case.

**The skeptic's corrected fix.** 1. In Tools/us_returns_prep.pl line 504, refuse `/[,"\\\x00-\x1f]/` (CR and LF are the ones that matter; also refusing TAB costs nothing). Make the header comment (line 63) and the message say "or a control character". The unstaged working tree already holds exactly this; it needs staging.
2. Under the standing rule that every new failure path is proved, add a line-break case to Tools/us_returns_mutations.sh beside h23. I tested this one on scratch copies: MISSED by the staged guard (exit 0, 6 files written), CAUGHT by the fix (exit 255, 0 files):
   h24() { local f=ElectionsData/usa/raw/returns/fec_2024presgeresults.xlsx; perl "$POKE" $f '<c r="G1" s="2" t="s"><v>68</v></c>' '<c r="G1" s="2" t="inlineStr"><is><t>DE LA&#10;CRUZ</t></is></c>'; resum $f; }
   case_ "a 2024 candidate's label with a line break" "CRUZ' holds a comma, a quote, a backslash or a control character" h24
   (The refused label prints across two lines, which is why the expected text starts at "CRUZ'".)
3. Optional: print control characters escaped in that message, e.g. `(my $shown = $label) =~ s/([\x00-\x1f])/sprintf '\\x%02x', ord $1/ge;`, so the MISMATCH line stays one line.

### 5. h01 never lands in the staged script: `$` cannot match before the extractor's CR

- **Lens:** mutations - **reviewer:** defect - **skeptic:** defect
- **Where:** Tools/us_returns_mutations.sh:102

**The scenario.** pdftotext 4.06 (/mingw64/bin) ends every line with CRLF, and it does so on a pipe too (clerk_statistics2018.pdf piped: 4472 CR, 4472 LF). The 2018 Alabama line is '... 5,471 1,659,895<CR><LF>'. Under /m, `$` matches only before LF, so `5,471 1,659,895$` never matches. The substitution does nothing, and the tool exits 0 and writes all six outputs. I ran it on a frozen snapshot of the index (scripts from `git show :Tools/...`, ElectionsData/usa hash-checked against the index). It printed 'MISSED  a Clerk row's cell changed, its Total kept (2018, Alabama) (exit 0, 6 file(s) written)', then 'caught 37, missed or broken 1', exit 1. The working tree already has an unstaged edit of this line (git status MM): `5,471 1,659,895(\r?)$}{$1 5,472 1,659,895$2}m`. I ran that version alone and it was CAUGHT with the expected text, plus L843's Write-in line. My first full run on the live tree reported 38 of 38 only because the script file was rewritten while it ran (mtime 02:26:25).

**The fix proposed.** Stage the working-tree line 102. h03, h04 and h05 already handle the CR, because they consume `[^\n]*\n`. Any later case that anchors `$` at a line end of extracted text needs the same `\r?`.

**The skeptic's evidence.** I could not refute it. I reproduced it from the index alone.

1. The extractor writes CRLF and the tool does not stop it. Staged prep.pl l.805 opens `'-|', $pdftotext, '-raw', "$raw/$file", '-'` with no `-eol unix`, so xpdf 4.06's Windows default (DOS line ends) applies. `/mingw64/bin/pdftotext -raw clerk_statistics2018.pdf -` piped gives CR=4472, LF=4472, CRLF=4472. The 2018 Alabama line, line 4321, ends `... 5,471 1,659,895<CR><LF>`.

2. Nothing in between removes the CR. `wrap()` (staged l.26-28) pipes the output through `perl -pe`. That perl is Git for Windows' perl 5.42.2 (x86_64-cygwin), and its STDIN layers are `unix,perlio`, with no `:crlf`. Staged l.102 is:
   `h01() { wrap 'BEGIN { undef $/ } s{^(Alabama \.+ 678,687 975,737 .*?) 5,471 1,659,895$}{$1 5,472 1,659,895}m'; }`
   Under /m, `$` matches only before `\n`, so the CR after `1,659,895` blocks the match.

3. The mutation does nothing. I built the wrapper exactly as `wrap`'s printf %q builds it. With the staged program, its output is byte-identical to plain pdftotext (`cmp`: identical). With the working tree's `(\r?)$ ... $2` version, line 4321 becomes `5,472 1,659,895<CR>`.

4. The case is missed in a run. I made a frozen copy of the index: prep.pl, the mutation script, xlsx_poke.pl and the catalog came from `git show :...`, with the catalog in its CRLF checkout form, and all 159 ElectionsData/usa files were checked against their index blobs with `git hash-object --path`, with no differences. I ran staged lines 1-47, 102 and 111 on that copy. Output:
   - `PASSES the control`
   - `MISSED a Clerk row's cell changed, its Total kept (2018, Alabama) (exit 0, 6 file(s) written)`
   - The working tree's line 102 gave `CAUGHT`, with the expected text `clerk_statistics2018.pdf AL: its columns sum to 1659896, its Total is 1659895`.
   Since bad is at least 1, the committed script fails `[ $bad -eq 0 ]` and exits 1. That breaks the rule that every new failure path is proved: the Clerk's row-sum check (staged prep.pl `problem("$file $st: its columns sum to $parts, its Total is ...")`) goes unproved.

5. My full staged run also printed the same MISSED line, but other agents share this scratchpad and overwrote that output file, so I rely on the isolated run in item 4.

6. Scope: the tool itself handles the CR. The row regex `(.*)$` takes the CR into `.*`, `split ' '` drops it, and the title check ends `\s*$`. The defect is only in the proof script. h01 is the only staged wrap program that anchors `$` at a line end: h03, h04 and h05 consume `[^\n]*\n`, and h02 and h09 do not anchor.

Severity: this stays a defect for the staged commit, though it is narrow. The generated data and the tool are unaffected, and the one-line fix is already written in the working tree, unstaged (git status MM).

**The skeptic's corrected fix.** Stage the working tree's line 102 before committing:
`h01() { wrap 'BEGIN { undef $/ } s{^(Alabama \.+ 678,687 975,737 .*?) 5,471 1,659,895(\r?)$}{$1 5,472 1,659,895$2}m'; }`
I ran it on the frozen copy of the index and it was CAUGHT with the expected text. Anchoring with `(?=\r?$)` would also work.

After staging, run the whole script on a frozen copy of the index, not on the live tree. Bash reads a script as it runs, so a concurrent edit changes what is executed. Expect every case caught and exit 0.

Any later wrap case that anchors `$` on pdftotext output needs `\r?` before the `$`.

### 6. L843 (each column's state sum vs the Total row) has no case, and it is the only check that catches a state figure moved together with its own Total

- **Lens:** mutations - **reviewer:** defect - **skeptic:** minor
- **Where:** Tools/us_returns_prep.pl:843

**The scenario.** No expected text contains "the states' ... sums to". h03 and the fixed h01 print that message as a side effect, but they assert L841 and L836. If a state's party figure and its own Total rise together, L836 (row sums) and L841 (every state present) both stay silent. 2024 has no FEC volume, and returns_2024.md is held only to the Total row (L890-893). For 2016-2022 the cross-check tolerates up to 15 differing states. Verified on the index snapshot: wrap 'BEGIN { undef $/ } s{^(Alabama \.+ )1,508,754( .*? )2,048,663(\r?)$}{${1}1,508,755${2}2,048,664$3}m' exits 1, writes nothing, and prints only L843: "clerk_statistics2024.pdf: the states' Republican sums to 74390865, the Total row holds 74390864" and the same line for Total. If line 843 were deleted or broken, all 38 cases would still pass, and that page error would reach house_by_state.csv.

**The fix proposed.** Add that case with the expected text "clerk_statistics2024.pdf: the states' Republican sums to 74390865, the Total row holds 74390864".

**The skeptic's evidence.** I could not refute it. Everything below was run on the index snapshot: the tool came from `git show :Tools/us_returns_prep.pl`, and I checked that ElectionsData/usa is the same in the working tree and the index. Each run used a scratch copy built the way the suite's fresh()/wrap() build theirs. Nothing in the repo was touched.

The code (index line numbers; the working-tree tool is being edited and the check has moved to line 845 there):
- Line 843: `for my $k (0 .. 8) { problem("$file: the states' $cols[$k] sums to " ...) unless ($sum{$k} // 0) == $total->[$k]; }`. This is the only place the Clerk's state rows meet the national Total row.
- Line 836 checks each row's parts against that row's own Total, and line 841 checks every state is present. Raising a state's party figure and its own Total together passes both.
- The FEC cross-check (lines 846-847, 875-876) covers only 2016-2022 and still passes with 15 states differing.
- returns_2024.md (lines 890-893) is held to `$house_tot{2024}`, which is the Total row itself.
- GeneratedCatalogCheck holds each row to its CSV, each row's parts to its total, and 50 rows a year. It has no national row to compare against.
- After line 909 the rows go straight into house_by_state.csv (line 944) and the catalog (line 989).

Reproduced:
1. The finding's wrap on clerk_statistics2024.pdf: exit 255 (die), not 1, but the suite only tests rc≠0. No files written. Exactly two MISMATCH lines, both from line 843: "the states' Republican sums to 74390865, the Total row holds 74390864" and "the states' Total sums to 149543422, the Total row holds 149543421".
2. The same wrap with line 843 removed from the copy: exit 0, all 6 files written. house_by_state.csv carries `2024,AL,1508755,518197,21712,2048664`; the repo has 1508754 / 2048663. The run's own report still prints "House 2024 by the Clerk: R 74390864", so nothing reveals the error.
3. The fixed h01 prints line 836 (which it asserts) plus "the states' Write-in sums to 64082, the Total row holds 64081" as a side effect. h03 prints line 841 (which it asserts) plus seven line-843 lines. This matches what the finding says.
4. The staged suite has 38 case_ lines, and none expects "the states'" (grep count 0). problem() only queues a message (line 87), and line 843 has no next, die or last. So deleting it cannot change whether any case is caught. Line 843 is also the backstop for "a second row" and "a row naming no state", and the only guard against a token permutation inside a row, such as R and D swapped in one state.

Two side observations:
- The staged h01 is itself MISSED on this machine. pdftotext lines end in CRLF, so `1,659,895$` never matches; the run exits 0 and writes 6 files. This is fixed only in the working tree.
- The staged record (president_returns.md line 37) still says "caught all twenty-nine".

Why minor and not defect:
- The guard exists and fires today, so no wrong figure can reach the outputs. What is missing is the proof the standing rule asks for: a regression of line 843 would pass the suite unseen.
- The US-3 review graded the same pattern as note from both reviewer and skeptic (Reviews/2026-10-05_s786_us3_presidential_returns.md, #4: "Mutations 1 and 2 no longer pin the shortfall bound"). It graded an actually missing guard as minor (#1).
- Since then the suite has been committed and the rule made explicit, which lifts this above note.
- It is not alone: about 18 other new refusals were also unpinned in the staged snapshot.
- The working tree already holds, unstaged, h10 with the finding's exact regex and expected text, plus h11-h28 "every refusal the readers added, proved".

**The skeptic's corrected fix.** The working tree already holds the fix as h10. It needs to be staged before the commit, because the index snapshot still has the gap.

- `h10() { wrap 'BEGIN { undef $/ } s{^(Alabama \.+ )1,508,754( .*? )2,048,663(\r?)$}{${1}1,508,755${2}2,048,664$3}m'; }`
- `case_ "a state's Republicans and its Total raised together (2024, Alabama)" "clerk_statistics2024.pdf: the states' Republican sums to 74390865, the Total row holds 74390864" h10`

Optionally, also expect "@@the states' Total sums to 149543422, the Total row holds 149543421", which pins the Total column too. With line 843 present this case is caught (exit 255, nothing written, only line 843 printed). With line 843 removed it exits 0 and writes files, so the suite would report it MISSED.

In the same staging:
- Stage the CRLF fix to h01 (`(\r?)$` … `$2`). The staged h01 does not land, and the suite reports it MISSED.
- Bring the count in president_returns.md line 37 ("twenty-nine") and in COMPLETED.md's US-5 entry up to the suite's new total once it passes.

### 7. returns_2024.md's presidential-row checks (L897, L900) and section checks (L885, L886) have no case

- **Lens:** mutations - **reviewer:** defect - **skeptic:** minor
- **Where:** Tools/us_returns_prep.pl:897

**The scenario.** h07 trips L897 as a side effect ("returns_2024.md: STEIN typed 862049, the FEC workbook's Total row 862050"), but it asserts only L503 and L508. No case asserts L897, L900, L885 or L886. Only L897 catches a typo in the md's Stein figure, and only L900 catches a deleted md row; in each case it is the only mismatch printed. Verified on the index snapshot; each case was CAUGHT with nothing written: sed -i 's/^| Jill Stein (Green) | 0.56 (862,049) | 0 |/| Jill Stein (Green) | 0.56 (862,050) | 0 |/' prints "returns_2024.md: STEIN typed 862050, the FEC workbook's Total row 862049"; sed -i '/^| Jill Stein (Green) |/d' prints "returns_2024.md: 2 presidential row(s) held, not the three it types"; sed -i 's/^### National result \(.*\) House/### National tally \1 House/' prints "returns_2024.md: no 'National result - House' section"; the same edit on President prints "returns_2024.md: no 'National result - President' section".

**The fix proposed.** Add the four cases on ElectionsData/usa/returns_2024.md. They need no resum, because the md is not a hashed page.

**The skeptic's evidence.** The finding holds, but it is a coverage gap, not a defect. I read the staged tool and suite (`git show :`) and ran the staged tool on scratch copies. Nothing was written to the repo. The scripts are in scratchpad/skv788_mdpaths_454173/.

**The code**

All four lines are new in this change, in the returns_2024.md block of Tools/us_returns_prep.pl:
- L885 `problem("$md: no 'National result - House' section") unless $hse;`
- L886 `problem("$md: no 'National result - President' section") unless $pres;`
- L897 `problem("$md: $who typed $v, the FEC workbook's Total row " ...) unless defined $vote{$who} && $vote{$who} == $v;`
- L900 `problem("$md: N presidential row(s) held, not the three it types") unless keys(...) == 3;`

`case_` (us_returns_mutations.sh:36) only requires that each listed string appears (`grep -qF`). Extra output is ignored. None of the 38 cases lists "FEC workbook's Total row", "presidential row(s) held" or "National result". h07 (:117) lists only the two candidate-column messages (prep L503, L508).

**Measured (exit code, files written, MISMATCH lines)**
- Control: exit 0, 6 files.
- md Stein typed 862,050: exit 255, 0 files. Only mismatch: "returns_2024.md: STEIN typed 862050, the FEC workbook's Total row 862049".
- md Trump typed +1: exit 255, 0 files. Only mismatch: L897's.
- md Stein row deleted: exit 255, 0 files. Only mismatch: "returns_2024.md: 2 presidential row(s) held, not the three it types".
- House header renamed: L885 fires, plus three L892 lines ("the House's Republican/Democratic/total typed nowhere...").
- President header renamed: L886 fires, plus L900 ("0 presidential row(s) held").
- h07 as committed: prints its two expected lines plus "returns_2024.md: STEIN typed 862049, the FEC workbook's Total row 862050". That third line is not asserted.

**A regression would go unseen**

I turned L885, L886, L897 and L900 into `0 && problem(` in a scratch copy:
- The staged suite's control printed "PASSES" and h07 printed "CAUGHT". No other case reaches the md's presidential rows, so the whole suite stays green.
- With those lines off, the Stein typo, the Trump typo and the deleted row each exit 0 and write all six outputs.

So L897 and L900 are each the only guard for their scenario, and nothing proves they fire. L885 and L886 are backstopped: without them the run still stops, through L892 or L900. A missing case there loses only the exact message.

**Why minor, not defect**
- No figure, CSV, catalog entry or record claim is wrong today. All four checks fire as intended.
- The cost is against the standing rule that every new failure path is proved by the mutation script. The header line "Tools/us_returns_mutations.sh proves the failure paths below" now lists "the three presidential rows of returns_2024.md" without a case behind it.
- At HEAD, that same header sentence already covered unproved paths: ties, 538 electors, perl warnings, quote or backslash in a name.
- s786's second pass #4 (a sole-guard bound with no asserting case) was graded note by both reviewer and skeptic. The suite is now committed and the rule is explicit, so this sits one step up, at minor.
- The gap is wider than these four. No case asserts the FEC table's title or GENERAL ELECTION heading checks, the Clerk's second-Total-row, no-Total-row, header-count, nine-columns or no-state-row checks, or the label-character check (grep over every case_ line).

**The skeptic's corrected fix.** Add standalone md cases to Tools/us_returns_mutations.sh. Like h08, they need no resum: the tool opens returns_2024.md with a plain `open`, not `page()`, it is in no SHA256SUMS.txt, and GeneratedCatalogCheck does not hash it. Do not anchor the seds on `$`: under core.autocrlf the working-tree md is CRLF. All four were measured on scratch copies: exit 255, nothing written, the expected line printed.

```
h10() { sed -i 's/^| Jill Stein (Green) | 0.56 (862,049) | 0 |/| Jill Stein (Green) | 0.56 (862,050) | 0 |/' ElectionsData/usa/returns_2024.md; }
h11() { sed -i '/^| Jill Stein (Green) |/d' ElectionsData/usa/returns_2024.md; }
h12() { sed -i 's/^### National result \(.*\) House/### National tally \1 House/' ElectionsData/usa/returns_2024.md; }
h13() { sed -i 's/^### National result \(.*\) President/### National tally \1 President/' ElectionsData/usa/returns_2024.md; }
case_ "a typed 2024 presidential row off by one vote (Stein)" "returns_2024.md: STEIN typed 862050, the FEC workbook's Total row 862049" h10
case_ "a typed 2024 presidential row deleted (Stein)" "returns_2024.md: 2 presidential row(s) held, not the three it types" h11
case_ "returns_2024.md's House section retitled" "returns_2024.md: no 'National result - House' section" h12
case_ "returns_2024.md's President section retitled" "returns_2024.md: no 'National result - President' section" h13
```

h10 and h11 are the cases that matter: their checks are the only guards. h12 and h13 pin only the exact message, since L892 and L900 already stop those runs.

Do not instead add L897's line to h07's expected strings. That would tie two checks to one case and still would not show that L897 alone catches an md typo.

Also, either add cases for the other unproved new paths (the FEC title and heading checks, the Clerk's Total-row, header and no-state checks, the label-character check), or narrow the header's "proves the failure paths below" so it no longer claims them.

### 8. Seven FEC House-table checks have no case (L850, L853, L860, L864, L867, L869, L872)

- **Lens:** mutations - **reviewer:** defect - **skeptic:** minor
- **Where:** Tools/us_returns_prep.pl:853

**The scenario.** Only L870 (h06) and L876 (h04) are proved for the FEC tables. Three checks are each the only mismatch their error prints: L850 (the title); L853 (the headings over E-G; its next skips the cross-check); and L869 (a formula in the Total row, which would make L870 hold the rows to Excel's own sum). Verified on the index snapshot with perl $POKE + resum on fec_federalelections2022.xlsx (each old string appears once, in sheet6.xml); each case was CAUGHT with nothing written: '<c r="A1" s="47" t="s"><v>11114</v>' -> '<v>11113</v>' prints "titled '2022 VOTES CAST FOR THE U.S. SENATE BY PARTY', not 2022's House vote by party"; '<c r="F4" s="50" t="s"><v>2812</v>' -> '<v>2811</v>' prints "'6. Table 5 House by Party': no heading of GENERAL ELECTION over Democratic, Republican and Other in columns E-G"; '<c r="A59" s="49" t="s"><v>552</v></c><c r="B59" s="35"><v>7546</v>' with 552 -> 627 prints "fec_federalelections2022.xlsx WI: a second row" and "fec_federalelections2022.xlsx: no row for WY", and with 552 -> 2300 prints "'6. Table 5 House by Party' row 59: 'Other', no state or territory"; '<c r="A60" s="49" t="s"><v>2796</v>' -> '<v>2300</v>' prints "'6. Table 5 House by Party': no Total row"; '<c r="F60" s="52"><v>54298205</v>' -> '<c r="F60" s="52"><f>SUM(F5:F59)</f><v>54298205</v>' prints "'6. Table 5 House by Party': the Total row's column 6 is a formula". t15's looser expected text would also match L869's message, but t15 only touches 2012's Table 2.

**The fix proposed.** Add those six pokes as cases. The A59 string needs its B59 context: A59 alone also appears in sheet3.xml, and xlsx_poke then dies, which shows up as MISSED.

**The skeptic's evidence.** I could not refute the facts. The finding holds against the staged diff, but it is a gap in the suite's proof, not a defect in the tool.

1. Coverage in the staged suite (`git show :Tools/us_returns_mutations.sh`, lines 101-119)
- Only two cases reach the FEC House tables:
  - h06 pokes `<v>942393</v>` and expects L870's text ("the rows' column 6 sums to 54298206").
  - h04 crosses the Clerk's 2020 header and expects L876's text ("agree exactly on 0 of 50").
- No case reaches L850, L853, L860, L864, L867, L869 or L872.
- t15's expected text ("is a formula, with no typed national figure") is a substring of L869's message. But t15 only empties 2012's `%typed`, so it fires L441, not L869.

2. Reproduced on a scratch copy
- Tool: the index's copy (`git show :Tools/us_returns_prep.pl`). `ElectionsData/usa` has no unstaged change.
- Control: exit 0, 6 files written.
- Every poke below exited 255 and wrote 0 files:
  - A1, 11114 -> 11113: L850 alone, 1 mismatch ("titled '2022 VOTES CAST FOR THE U.S. SENATE BY PARTY', not 2022's House vote by party").
  - F4, 2812 -> 2811: L853 alone, 1 mismatch.
  - A59 with its B59 context, 552 -> 627: L864 "WI: a second row" and L872 "no row for WY".
  - The same string, 552 -> 2300: L860 "row 59: 'Other', no state or territory" and L872.
  - A60, 2796 -> 2300: L860 eight times (row 60 and the notes rows 64-78) and L867 "no Total row".
  - F60 with `<f>SUM(F5:F59)</f>` added: L869 alone, 1 mismatch.
- The A59 string on its own occurs once in sheet3.xml and once in sheet6.xml, so xlsx_poke needs the B59 context, as the finding says.
- So if L850, L853 or L869 were deleted, its corruption would pass the tool (it was the only mismatch), and every case in the suite would still report CAUGHT.

3. Why minor and not defect
- All seven checks fire correctly. Nothing wrong is written today.
- The workbooks are SHA-pinned, so these corruptions can only arise on a re-fetch.
- By reading, L876 backs up L850 and L853 in realistic cases: a wrong table or crossed columns makes most states differ. L853's `next` only skips work after a problem is already queued.
- The FEC's Total row feeds no output: the summary re-sums the FEC's state rows, and `house_by_state.csv` is the Clerk's. So L869 only protects the power of L870.
- Precedent: s779 #6 (test power on a correct guard) was re-graded from defect to minor. s786 #4, #5 and #21 (mutation-suite gaps) were graded note. US-3 itself shipped with listed failure paths that have no case.
- It stays above a note for two reasons. The header (prep.pl:26-27) says the suite "proves the failure paths below", and the new bullet at :67-68 names the title, the heading, the formula refusal, and a state missing or twice: five claimed proofs with no case. And the standing rule asks for every new path to be proved.

4. The same gap elsewhere in the staged change
- Clerk section: L815, L821, L830, L832, L833 and L842 have no case. L843 fires under h01 and h03 but no case asserts it.
- `returns_2024.md` checks: L885, L886 and L900 have no case.
- `president_returns.md:37` still reads "caught all twenty-nine" and lists none of h01-h09.

5. The working tree is changing during this review
- The unstaged copy of `Tools/us_returns_mutations.sh` now adds h10-h28. These include h17 (L850), h18 (L853), h19 (L869), h20 (L864 and L872) and h21 (L867).
- No case in that copy asserts L860's "no state or territory".

**The skeptic's corrected fix.** Add cases to the US-5 block of Tools/us_returns_mutations.sh. Each one pokes ElectionsData/usa/raw/returns/fec_federalelections2022.xlsx, runs `resum`, and expects its own refusal:
(1) L850. Poke `<c r="A1" s="47" t="s"><v>11114</v>` to `<c r="A1" s="47" t="s"><v>11113</v>`. Expect "'6. Table 5 House by Party': titled '2022 VOTES CAST FOR THE U.S. SENATE BY PARTY'".
(2) L853. Poke `<c r="F4" s="50" t="s"><v>2812</v>` to `...<v>2811</v>` (or use the working tree's E3 3166->2811). Expect "no heading of GENERAL ELECTION over Democratic, Republican and Other in columns E-G".
(3) L869. Poke `<c r="F60" s="52"><v>54298205</v>` to `<c r="F60" s="52"><f>SUM(F5:F59)</f><v>54298205</v>`. Expect "'6. Table 5 House by Party': the Total row's column 6 is a formula".
(4) L864 and L872. Poke `<c r="A59" s="49" t="s"><v>552</v></c><c r="B59" s="35"><v>7546</v>`, changing 552 to 627. The B59 context is needed because the A59 string alone is also in sheet3.xml. Expect "fec_federalelections2022.xlsx WI: a second row@@fec_federalelections2022.xlsx: no row for WY".
(5) L860. Same string, 552 to 2300. Expect "'6. Table 5 House by Party' row 59: 'Other', no state or territory". The in-progress working tree still has no case for this one.
(6) L867. Poke `<c r="A60" s="49" t="s"><v>2796</v>` to `...<v>2300</v>`. Expect "'6. Table 5 House by Party': no Total row".
Then re-run the suite. Update president_returns.md:37 so its count and list match the cases actually caught; it still says twenty-nine. To meet the standing rule in full, also give cases to the Clerk section's and returns_2024.md's refusals that have none: L815, L821, L830, L832, L833, L842, L885, L886 and L900, plus an assertion for L843, which fires under h01 and h03 but is never checked. The in-progress h10-h16 and h24-h27 target these.

### 9. Six Clerk-page structure checks have no case (L815, L821, L830, L832, L833, L842)

- **Lens:** mutations - **reviewer:** defect - **skeptic:** minor
- **Where:** Tools/us_returns_prep.pl:830

**The scenario.** No case reaches these checks: the header count (L815), the nine-column header (L821), a second Total row (L830), a row naming no state (L832), a state twice (L833), or a missing Total row (L842). L830 and L842 are each the only mismatch their error prints. Verified on the index snapshot, all on 2022's page through wrap with BEGIN { undef $/ }; each case was CAUGHT with nothing written: s{^(Total \.+ 54,227,992 [^\n]*\n)}{$1$1}m prints "clerk_statistics2022.pdf: a second Total row"; s{^Total \.+ 54,227,992 [^\n]*\n}{}m prints "clerk_statistics2022.pdf: no Total row"; s{^(Wyoming \.+ 132,206 47,250 [^\n]*\n)}{$1$1}m prints "clerk_statistics2022.pdf WY: a second row"; s{^Wyoming( \.+ 132,206 47,250 )}{Wyomin$1}m prints "clerk_statistics2022.pdf: a row 'Wyomin', no state"; s{(\fState Republican Democratic Libertarian Independent Green Constitution Other Parties1) Write-in Total(\r?\nAlabama \.+ 942,393 )}{$1 Write in Total$2} prints "clerk_statistics2022.pdf: the header 'State Republican Democratic Libertarian Independent Green Constitution Other Parties1 Write in Total' - not nine columns"; s{\f(State Republican [^\n]*\n)(Alabama \.+ 942,393 )}{\f$1$1$2} prints "clerk_statistics2022.pdf: 2 header line(s) on the recapitulation's page, not one". The header line comes right after the page's form feed, so anchoring with ^State does nothing and the case is MISSED; that happened on my first attempt. h04 and h05's \fState already handle this.

**The fix proposed.** Add the six wrap cases above.

**The skeptic's evidence.** The finding holds for the staged change. I extracted the staged files with `git show :` and confirmed they did not change while I reviewed.

**What the six checks are (staged Tools/us_returns_prep.pl):**
- L815: `if (@hdr != 1) { problem("$file: " . scalar(@hdr) . " header line(s) ..., not one"); next; }`
- L821: `unless (@cols == 9 && $cols[-1] eq 'Total' && keys(%ix) == 9 && defined $ix{Republican} && defined $ix{Democratic}) {...}`
- L830: `problem("$file: a second Total row") if $total;`
- L832: `problem("$file: a row '$name', no state")`
- L833: `problem("$file $st: a second row") if $house{$year}{$st};`
- L842: `if (!$total) { problem("$file: no Total row"); next; }`

**No staged case expects any of these six messages (staged us_returns_mutations.sh L111-119):**
- h01 pins L836 and h03 pins L841. Both also trigger L843 without requiring its message.
- h04 and h05 swap two header names, so the header still has nine unique columns with R and D, and L821 passes.
- h09 stops at L810's `next`, before any of the six.
- h02 pins L813 and h06-h08 pin the FEC, candidate and md checks.

**Correction to the finding:** "No case reaches L815" is wrong. I ran t20 as the suite runs it (`s/\f//g`) and it prints "clerk_statistics2016..2024.pdf: 2 header line(s) on the recapitulation's page, not one" for all five years. But t20 expects only "carries no page breaks", so L815 is reached by chance and still not proved.

**My run of the reviewer's six cases:** scratch copy of ElectionsData/usa, the staged tool, pdftotext wrapped the way the suite's `wrap` does it.
- Control: exit 0, 6 files written.
- All six cases: exit 255, 0 written, the intended message printed.
- L830 and L842 each printed only their own line.
- L833 and L832 also printed L843's column-sum lines.
- L821 and L815 also printed "House 2022: ... agree exactly on 0 of 50 states".
- Anchoring the header case with `^State` gave exit 0 and 6 written, so it would be MISSED. This matches the finding's note about the form feed.

**The staged header claims these paths are proved.** L26-27 says "Tools/us_returns_mutations.sh proves the failure paths below", and L62-64 lists "headed by anything but nine columns ending Total ..., a row naming no state, a state missing or twice". So the claim is false for three of the paths it names, and the stated rule (every new failure path proved) is not met.

**Why this is minor, not a defect:** nothing the tool does is wrong today. All six checks fire, and other guards cover each case:
- L843's column sums catch a row that is duplicated or names no state.
- When a year is skipped (L815/L821), the FEC cross-check (L876, needs at least 35 of 50 states) catches 2016-2022, and the returns_2024.md House line (L892) catches 2024.
- The `$SIG{__WARN__}` hook (L71) turns any undefined read into a mismatch.
- An identical second Total row, the one case only L830 sees, changes no figure. The outputs come from the state rows in %house (L944, L988).

There is precedent: s786's review graded suite-pinning gaps as notes (second pass, #4), even where the unpinned bound was the only guard.

**The gap is wider than six:** the staged suite also has no case for L850, L853, L860, L864, L867, L869, L872, L885/886, L900, L481 (a formula in the 2024 candidates' Total row) or L504 (the label check). L843 and L897 are reached but not pinned.

The working tree, being edited during this review and not staged, already adds h10-h28. Of these, h11-h16 cover exactly these six checks.

**The skeptic's corrected fix.** Add six wrap cases, one per check, each requiring its own message. The reviewer's six programs work as given (each run with the staged tool: exit 255, nothing written, its message printed). Both header cases must anchor on `\f`, not `^State`. The main session's unstaged h11-h16 are equivalent ('Guam' for the row naming no state, 2024's header missing a column for L821, 2022's header line duplicated for L815).

Reword the finding to say "no case pins L815", since t20 reaches it by chance.

Then do one of two things:
- also pin the other new unproven paths (L481, L504, L850, L853, L860, L864, L867, L869, L872, L885/886, L900, and L843/L897's own messages); or
- narrow the header's "proves the failure paths below" (L26-27) to the paths the suite actually pins.

Finally, update president_returns.md's paragraph on the suite to the new case count.

### 10. The 2024 workbook's widened formula check (L481) and the label check (L504) have no case

- **Lens:** mutations - **reviewer:** defect - **skeptic:** minor
- **Where:** Tools/us_returns_prep.pl:504

**The scenario.** L481 now refuses a formula in every candidate column's Total cell. Those cells hold the figures president_by_candidate.csv publishes, and L503 holds them to the state rows. No case puts a formula in the 2024 workbook; t15 proves the message only for 2012's Table 2. L504 also has no case. For a column that returns_2024.md does not type (e.g. DE LA CRUZ), L504 is the only check before a comma in the label splits president_by_candidate.csv's row, or a quote breaks the Candidates C# literal. Verified on the index snapshot with $POKE + resum on fec_2024presgeresults.xlsx (sheet1.xml): '<c r="U53" s="49"><v>862049</v>' -> '<c r="U53" s="49"><f>SUM(U2:U52)</f><v>862049</v>' prints only "fec_2024presgeresults.xlsx: the Total row's column 21 is a formula, with no typed national figure to hold the state rows to"; '<c r="U1" s="2" t="s"><v>55</v>' -> '<c r="U1" s="2" t="inlineStr"><is><t>STEIN, JILL</t></is>' prints "the candidate column 'STEIN, JILL' holds a comma, a quote or a backslash", plus L897's 'nothing'.

**The fix proposed.** Add both cases.

**The skeptic's evidence.** The finding holds, but it is a gap in what the mutation suite proves. Both checks work today and nothing wrong gets written. I checked it on the staged snapshot: `git show :Tools/us_returns_prep.pl`, run against a private copy of ElectionsData/usa, never in the repo. The control exits 0, all five CSVs are byte-identical to the index, and the .cs matches the index once CRLF is normalised.

**The lines (index numbering)**
- L481: `for my $k ($evR, $evD, $vT, @cand) { problem("$file: the Total row's column $k is a formula, with no typed national figure to hold the state rows to") if $formula_at{"$file|OFFICIAL 2024 PRES GE RESULTS"}{"$r,$k"}; }`
- L504: `problem("$file: the candidate column '$label' holds a comma, a quote or a backslash") if $label =~ /[,"\\]/;`

**No staged case reaches either line**
- h07 is the only case that touches fec_2024presgeresults.xlsx. It changes a value (`<v>862049</v>` to `<v>862050</v>`) and expects the sum messages from L503 and L508.
- t15 (`my %typed = ();`) fires only L441, on 2012's Table 2. Its expected text, "is a formula, with no typed national figure", is satisfied by L441 alone.
- No case changes a header label.

**What the probes printed**
- Formula in Stein's Total: U53 `<v>862049</v>` changed to `<f>SUM(U2:U52)</f><v>862049</v>`, then resum. Exit 255, nothing written, exactly one MISMATCH: "returns/fec_2024presgeresults.xlsx: the Total row's column 21 is a formula...".
- Same mutation with L481 turned into `0 && problem(`: exit 0 and all six files written. So L481 is the only guard, and the suite would not notice if it were lost.
- Column G's header (DE LA CRUZ, which returns_2024.md does not type) changed to inlineStr "DE LA CRUZ, CLAUDIA": exactly one MISMATCH, from L504.
- Same with L504 disabled: exit 0. president_by_candidate.csv gets the 5-field row `2024,DE LA CRUZ, CLAUDIA,-,166175`.
- With the header `DE LA "CRUZ"` and L504 disabled, the catalog gets `(2024, "DE LA "CRUZ"", "-", 166175L)`.
- The reviewer's STEIN recipe reproduces exactly: L504's message plus L897's "... Total row nothing".

**Why minor, not defect**
- Both checks fire correctly now, so no output is wrong.
- L504's consequences would also be stopped later in the pipeline. GeneratedCatalogCheck.ReadUsCsv reports "a row of 5 field(s), not 4", and a quote or backslash breaks compilation.
- Part of the L481 gap was already there. The base commit's 2024 formula check (43169a7a L462, five columns) never had a case either. What is new is the widening to every candidate column, whose Total figures are now published.
- The s786 review graded the same kind of gap (Mutations 1 and 2 no longer pinning the shortfall bound) as a note.
- It still breaks the standing rule that every new failure path is proved. It also contradicts the tool header's "us_returns_mutations.sh proves the failure paths below", whose US-5 bullet names the label check.

**Same gap elsewhere:** other new US-5 failure paths also have no case, including L869 (the FEC House formula refusal) and L897 (returns_2024.md's presidential rows).

**The skeptic's corrected fix.** Add these two cases to Tools/us_returns_mutations.sh, next to h07:

h10() { local f=ElectionsData/usa/raw/returns/fec_2024presgeresults.xlsx; perl "$POKE" $f '<c r="U53" s="49"><v>862049</v>' '<c r="U53" s="49"><f>SUM(U2:U52)</f><v>862049</v>'; resum $f; }
h11() { local f=ElectionsData/usa/raw/returns/fec_2024presgeresults.xlsx; perl "$POKE" $f '<c r="G1" s="2" t="s"><v>68</v>' '<c r="G1" s="2" t="inlineStr"><is><t>DE LA CRUZ, CLAUDIA</t></is>'; resum $f; }
case_ "a formula in a 2024 candidate's Total cell (Stein), its cached value kept" "fec_2024presgeresults.xlsx: the Total row's column 21 is a formula" h10
case_ "a 2024 candidate column's label holding a comma (De la Cruz)" "the candidate column 'DE LA CRUZ, CLAUDIA' holds a comma" h11

Why these recipes:
- On the index snapshot each prints exactly one mismatch, so each case pins its own line.
- h10's expected text names the 2024 file, so 2012's L441 cannot satisfy it the way t15's generic text is satisfied.
- h11 uses column G, which nothing else types, so L897 stays out of it.
- The prefix "holds a comma" also matches the working tree's widened L504 message.

Optional:
- Add a quote variant: `<t>DE LA "CRUZ"</t>`.
- Bring president_returns.md's suite paragraph up to date; it still says "twenty-nine".
- Either add cases for the other new US-5 paths that have none (L869, L897, the Clerk header and Total-row structure checks), or narrow the header's claim that the script "proves the failure paths below".

### 11. The wrap-based h cases edit the copy's tool but carry no TOOL: label

- **Lens:** mutations - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_returns_mutations.sh:101

**The scenario.** The header (line 3) says a case changes the copy's own tool only 'where it says TOOL'. wrap (lines 26-28) rewrites the copy's $pdftotext line. t12 and t20, which use wrap, are labelled TOOL; h01-h05 and h09 are not. The section comment at line 101 explains the mechanism, so only the label convention is off.

**The fix proposed.** Prefix those six labels with 'TOOL:', or reword the header to allow wrap for reaching a page's text.

**The skeptic's evidence.** The finding is right on every point I checked.

1. The header (Tools/us_returns_mutations.sh:2-3) was edited in this change (line 2), but line 3 still says: "Each case mutates a fresh copy of Tools/us_returns_prep.pl's inputs - or, where it says TOOL, the copy's own tool".

2. wrap (lines 26-29) is still tagged "# TOOL:", and its line 28 runs `sed -i "s#^my \$pdftotext = .*#my \$pdftotext = '$T/pdft.sh';#" Tools/us_returns_prep.pl`. case_ runs every mutation as `( cd "$T" && $3 )`, so this edits the copy's tool. I applied the same sed to a scratch copy of the staged tool (`git show :Tools/us_returns_prep.pl`). It rewrites line 85, `my $pdftotext = -x '/mingw64/bin/pdftotext' ? ... : 'pdftotext';`, to point at pdft.sh. So h01-h05 and h09 (lines 102-106 and 110, all `wrap '...'`) edit the copy's own tool exactly as t12 and t20 do.

3. The labels split along the old line. t12 and t20 (lines 128 and 131) are labelled "TOOL: ...". The six h labels (lines 111-115 and 119) are not, for example "a Clerk row's cell changed, its Total kept (2018, Alabama)".

4. The convention is a ruled one, which is why this is not just taste. At 43169a7a the only callers of wrap were t12 and t20, both labelled TOOL. Reviews/2026-10-05_s786_us3_presidential_returns.md finding 21 says "four of the runs change the tool itself, not its inputs: ... R12 swaps $pdftotext for a wrapper". It was graded a note, and the resolution (line 2600) reads "the tool-side cases labelled TOOL ... the record names the suite and splits its cases". The record's split (president_returns.md:37, unchanged in the index) counts "its text extractor wrapped to ..." among the cases that "change the copy's own tool". It also still says "twenty-nine", so whoever updates it for s788 has to place these six somewhere, and going by their labels would file them as input changes. That is the mistake finding 21 corrected.

Why it stays a note: the label is descriptive only. case_ treats every case the same. If wrap's sed ever failed to land, the run would go unmutated, exit 0 and write files, so the case would print MISSED rather than pass silently. Nothing reaches the catalog or a check.

Side observation, outside this finding: the indexed h01 differs from the working tree. The index has `s{^(Alabama ...) 5,471 1,659,895$}{...}m`; the working tree adds `(\r?)` and `$2`. The extractor emits CRLF here: `perl -ne 'print if /^Alabama .../' | od -c` ends `8 9 5 \r \n`. Run on the real 2018 text, the indexed regex matches 0 times and the working-tree one matches once. So the staged suite would report h01 MISSED until the working-tree fix is staged.

**The skeptic's corrected fix.** Follow the s786 ruling, which decides by mechanism: add the "TOOL: " prefix to the labels of h01, h02, h03, h04, h05 and h09, worded in t12's style so the page and the means both show (for example "TOOL: the extractor printing a Clerk row's cell changed, its Total kept (2018, Alabama)"). Leave h06, h07 and h08 unlabelled, because they change input files (xlsx_poke plus resum, and a sed on returns_2024.md). When president_returns.md:37 is updated from "twenty-nine" to the s788 suite, count these six with the cases that change the copy's own tool, as t12 and t20 are counted ("its text extractor wrapped to ..."). Rewording header line 3 alone, the finding's second option, would leave t12/t20 and h01-h05/h09 labelled differently for the same mechanism, against the ruling. Separately, stage the working tree's CRLF-tolerant h01.

### 12. Reading 13 says the remaining FEC/Clerk differences are "explained by no saved page", but several are explained exactly by the saved pages' own footnotes

- **Lens:** records - **reviewer:** defect - **skeptic:** minor
- **Where:** ElectionsData/usa/president_returns.md:58

**The scenario.** The pasted run lists the state-years where the FEC minus the Clerk is not zero. After quoting the FEC's notes (specials, LA 2016), reading 13 says: "The other differences below are explained by no saved page." The saved pages show otherwise:
(1) ME 2022 (D -12062, R -4882). clerk_statistics2022.pdf's Maine footnote says the count is "from round 2 ... In round 1 ... Golden received 153,074 votes, and Bruce Poliquin received 141,260". Against the printed 165,136 and 146,142 that is exactly the delta. fec_federalelections2022.xlsx '8. US House Results by State' also says it shows round 1 and gives the round-2 figures.
(2) ME 2018 (D -10427, R -4747). The ME-02 footnote in fec_federalelections2018.xlsx '2018 US House Results by State' says the results are "first-choice totals", with final round Golden 142,440 and Poliquin 138,931. Those are the Clerk's figures.
(3) NC 2018 (D +138341, R +139246). The Clerk's NC page leaves the 9th district blank ("ordered a new election in this Congressional District"). The FEC's sheet carries the uncertified McCready 138,341 and Harris 139,246.
(4) LA 2020 (R +153773). clerk_statistics2020.pdf's footnote says the LA-05 count is the 5 Dec 2020 runoff, and that Harris had 51,240 and Letlow 102,533 in November. 102,533 + 51,240 = 153,773. The FEC's sheet adds the runoff column (49,183 + 30,124) to November.
(5) Label differences visible on both pages: OR 2016 +78154 (Walker: FEC "IP/R", Clerk "Independent"); WI 2018 R -51921 (Rogers/Raymond 59,091 and 7,170 swapped); CA 2022 +/-79029 (Brower: Clerk "Republican", FEC "D").

**The fix proposed.** Name each difference the saved pages explain, citing the page and the footnote. Keep "explained by no saved page" only for what is left (e.g. 2016 MN/NH/NY/RI/SC/UT, 2018 MA/OH, 2020 MO/NJ/NY/ND/OH/VT, 2022 GA/WA, and IN's residual), after checking those too.

**The skeptic's evidence.** I could not refute this. Every case the reviewer names checks out against the pages saved and SHA-held in the tree. I extracted the Clerk PDFs with /mingw64/bin/pdftotext -raw/-layout and read the FEC workbooks' sheet XML with perl. The extracts and the dumper are in my scratchpad; nothing in the repo was touched.

The claim, president_returns.md:58: "The other differences below are explained by no saved page."

1. ME 2022 (pasted: D -12062, R -4882)
   - clerk_statistics2022.pdf, Maine page: "1This vote count is from round 2 of Maine's ranked-choice general election ... In round 1 of the election, Jared F. Golden received 153,074 votes, and Bruce Poliquin received 141,260 votes." The page prints 165,136 and 146,142.
   - 153,074-165,136 = -12,062 and 141,260-146,142 = -4,882.
   - fec_federalelections2022.xlsx, sheet '8. US House Results by State', row 1650, column X: "For the general election, the results shown are for the first round of Ranked Choice Voting (RCV). Second round results were: Golden: 165,136; Poliquin: 146,142".
   - Table 5 ME is 372,827 / 270,523.

2. ME 2018 (pasted: D -10427, R -4747)
   - fec_federalelections2018.xlsx, sheet8 '2018 US House Results by State', row 1585, column W: "The results shown above represent the first-choice totals ... the final-round vote totals were: Jared Golden, 142,440 ... and Bruce Poliquin, 138,931". Its general-vote column P holds 132013 and 134184.
   - Table 5 ME is 333,208 / 245,372. The Clerk's Maine total row is 343,635 / 250,119. The Clerk's 2018 page has no round note, so here the FEC's note is the one that explains the difference.

3. NC 2018 (pasted: D +138341, R +139246)
   - The Clerk's NC page prints "1 9. See explanation below" and leaves the 9th district row blank. Its footnote: "1On February 21, 2019, the North Carolina State Board of Elections ordered a new election in this Congressional District."
   - FEC sheet8, rows 2987 and 2991: Harris 139246, McCready 138341, with the note "The November 6, 2018, general election results for the 9th Congressional District were not certified."
   - Table 5 NC is 1,771,061 / 1,846,041. The Clerk's total row is 1,632,720 / 1,706,795.

4. LA 2020 (pasted: R +153773)
   - clerk_statistics2020.pdf: "1This vote count is from Louisiana's December 5, 2020, general (runoff) election ... Lance Harris received 51,240 votes in the primary, and Luke J. Letlow received 102,533." 51,240 + 102,533 = 153,773.
   - Table 7 LA R is 1,323,561. The Clerk's is 1,169,788.
   - The FEC's '13. US House Results by State' rows 1383-1384 carry the November votes (P = 102533, 51240) and the runoff (R = 49183, 30124). The Clerk's LA-05 R of 135,524 is the runoff plus the November votes of the eliminated Republicans, so the FEC must be summing November and December.
   - Clerk 2016 carries the same kind of footnotes. They give LA 2016's delta exactly: D 80,593 is Jones; R is 91,532 + 84,912 + 70,580 = 247,024. The record gives LA 2016 to the FEC's note, so this case is not part of the defect.

5. Party labels printed differently on the two pages
   - OR 2016: the FEC 2016 sheet13 row 2973 has Walker as K=IP/R, P=78154. The Clerk prints "David W. Walker, Independent ... 78,154". Table 7 OR R is 809,048 against the Clerk's 730,894.
   - WI 2018: FEC rows 4591-4592 give Raymond IND 59091 and Rogers R 7170. The Clerk prints "Tim Rogers, Republican ... 59,091; Robert R. Raymond, Independent ... 7,170". Table 5 WI R is 1,121,043 against the Clerk's 1,172,964.
   - CA 2022: FEC row 217 has Brower as K=D, P=79029. The Clerk prints "2. Douglas Brower, Republican".

So the sentence is false for at least seven state-years. It also hides facts about the series the record declares: the Clerk's ME-02 rows are the final ranked-choice round; NC-09 2018 is missing from the Clerk's series; Louisiana's runoff finalists carry their December votes beside the other candidates' November votes. Reading 13 says "a party's vote is its own ballot line" and says none of this.

Why minor and not defect:
- No CSV, catalog figure, check or code path is wrong. house_by_state.csv holds the Clerk's figures as printed, and the >=35-of-50 cross-check is unaffected.
- The departure still stands: the FEC counts the specials twice, adds Louisiana's runoffs to November, and has no 2024 volume.
- The hidden facts move the national two-party split by about 0.01 pp.
- The project's own precedent grades a false or too-weak claim about what the saved pages carry, in a SOURCED record, as minor. In the s786 review that is first-pass finding 12, and finding 20 even though its claim grounded a ruled bullet. In the s787 review, undeclared premises (findings 2 and 7) were also minor.

**The skeptic's corrected fix.** In president_returns.md reading 13, replace "The other differences below are explained by no saved page." with each difference the saved pages explain, quoting the page, the sheet and the note:
- ME 2018 and 2022: the FEC prints the first ranked-choice round and the Clerk the final round. Cite the FEC 2018 '2018 US House Results by State' ME-02 note ("first-choice totals"), the FEC 2022 '8. US House Results by State' note ("first round of Ranked Choice Voting"), and the Clerk 2022 footnote ("from round 2 ... In round 1 ...").
- NC 2018: the Clerk leaves the 9th district blank ("ordered a new election in this Congressional District"). The FEC carries the count it marks "not certified".
- LA 2020: the Clerk prints the December 5 runoff for its two finalists, and its footnote gives their November votes. The FEC's sheet carries both the November and the runoff columns, and its table holds both.
- OR 2016, WI 2018 and CA 2022: the two pages print different parties for one candidate. Walker is "IP/R" in the FEC and "Independent" in the Clerk. The FEC's WI-04 sheet gives Rogers (R) and Raymond (Ind) each other's figures. Brower is "D" in the FEC and "Republican" in the Clerk.

DECLARE in the same reading what this means for the Clerk's series:
- Maine's ME-02 rows are the final ranked-choice round (2018, 2022, and 2024's continuing-ballot count), so for them "a party's own ballot line" does not hold.
- NC-09 is absent from 2018.
- Louisiana's runoff finalists carry their December votes, the other candidates their November votes: LA-03/04 in 2016 (by the Clerk 2016's own footnotes) and LA-05 in 2020.

Keep "explained by no saved page" only for what is still unexplained after each is checked: 2016 MN, NH, NY, RI, SC and UT; 2018 MA and OH; 2020 MO, NJ, NY, ND, OH and VT; 2022 GA and WA; and Indiana 2022's small remainder beyond the IN/02 special. SC 2016 is not explained by the FEC's combined fusion-line totals, which I checked: they do not give its delta.

Follow the claim convention: name the notes and the state-years, and add no new figures in prose.

Optionally, bring USA_STAGE_PLAN.md's House-basis bullet into line ("counts same-day specials twice and adds Louisiana's December runoffs") with the Maine round and NC-09 2018.

### 13. US-5's House-basis reason for departing from the owner's ruling is one-sided: the Clerk's own series also swaps in runoffs and final RCV rounds and drops NC-09

- **Lens:** records - **reviewer:** defect - **skeptic:** minor
- **Where:** docs/specs/USA_STAGE_PLAN.md:217

**The scenario.** The design departs from Elias's verbatim text (FEC where it serves, the Clerk for 2024) for this reason: "The FEC has no 2024 volume (403), and read side by side the FEC counts same-day specials twice and adds Louisiana's December runoffs, so a history mixing the two would mix publications."
- The 403 is no reason to depart: the ruling already named the Clerk for 2024.
- The saved Clerk pages also depart from the November vote: LA's December runoff replaces the November vote for runoff candidates (2016 LA-3/LA-4 by footnotes 2-3; 2020 LA-05). Maine's last ranked-choice round is printed (2018, 2022, 2024). NC-09 2018 is absent. The adopted series therefore has its own date mix, and the DECLARED basis never says so.
- "counts same-day specials twice" (and the tool's "same-day elections for unexpired terms counted twice", Tools/us_returns_prep.pl:69-70 and :873-874) misstates the FEC note. The note says "the votes for both terms are included": the special is counted once, the district twice. No saved 2016/2018 page dates those specials.
- The FEC notes name Louisiana's December totals for 2016 only. Yet the tool comment says every year's differences are "where the FEC's notes name" these causes. In 2020 no note names any House difference, but 7 states differ.

**The fix proposed.** In the House basis, declare that the Clerk prints final-count figures: LA's December runoff in place of November for runoff candidates, Maine's last RCV round, no 2018 NC-09. Drop the 403 as a reason. Reword the FEC comparison: it adds the December runoff to November, and it adds a district's unexpired-term vote to its full-term vote. Make the tool comments match, or ask Elias, since this overrides his text.

**The skeptic's evidence.** I checked this against the staged tree. The House-basis bullet is identical in the working tree, and I ran pdftotext -raw/-layout on the saved Clerk PDFs into the scratchpad. The finding's main claims hold. One sub-point does not.

The bullet at docs/specs/USA_STAGE_PLAN.md:217 lists only the FEC's quirks: "...the FEC counts same-day specials twice and adds Louisiana's December runoffs, so a history mixing the two would mix publications." It never says what the Clerk's own figures are. The saved Clerk pages show three things it leaves out. Each one accounts exactly for a FEC-less-Clerk difference that reading 13 prints.

- **Louisiana.** In 2016, Clerk footnotes 2 and 3 say "This vote count is from Louisiana's December 10, 2016, general (runoff) election". The primary votes they give (Angelle 91,532, Higgins 84,912, Johnson 70,580; Jones 80,593) match the gap exactly: R +247,024, D +80,593. In 2020, footnote 1 says "December 5, 2020 ... Lance Harris received 51,240 votes in the primary, and Luke J. Letlow received 102,533", which matches R +153,773. So the Clerk puts the runoff count in place of the November vote for runoff candidates.
- **Maine.** In 2018 the Clerk prints only Golden 142,440 and Poliquin 138,931 for the 2nd district. The saved FEC 2018 House-results footnote says "The results shown above represent the first-choice totals ... final-round vote totals were: Jared Golden, 142,440 ... Bruce Poliquin, 138,931", which matches D -10,427 and R -4,747. In 2022 the Clerk's footnote 1 says "round 2 of Maine's ranked-choice general election ... In round 1 ... Golden received 153,074 ... Poliquin received 141,260", which matches D -12,062 and R -4,882. In 2024 the 2nd district is printed with lines for Continuing Ballots and Exhausted Ballots. The Clerk prints Alaska's first round in 2022 and 2024 (Peltola 128,553 / Palin 67,866 / Begich 61,513; Begich 159,550 / Peltola 152,828). So the series is not even consistent about which round it prints from state to state.
- **North Carolina 2018.** The Clerk's 9th-district row is empty, with the note "On February 21, 2019, the North Carolina State Board of Elections ordered a new election in this Congressional District". The FEC keeps the uncertified vote (Harris 139,246, McCready 138,341), which is exactly the NC difference.

**The FEC notes (dumped from the saved workbooks).** 2016 says "For Louisiana, the December election vote totals have been included" and "For states that had votes for both full and unexpired terms, the votes for both terms are included in the totals" (HI/01, KY/01, PA/02). 2018 names MI/13, NY/25, PA/07 and PA/15. 2020 names only Georgia (U.S. Senate), yet 7 states differ. 2022 names IN/02. No note says "same-day" or "twice". The staged tool comments at us_returns_prep.pl:69-70 and :873-874 present the differences as the ones the FEC's notes name, and they repeat the "same-day ... counted twice" wording. In the unstaged s789 rewrite of :69-70, the "same-day ... counted twice" wording is still there.

**One more false claim, in the same change.** Reading 13 of president_returns.md (staged line 58) says "The other differences below are explained by no saved page." That is false for Maine 2018, Maine 2022, Louisiana 2020 and North Carolina 2018: the footnotes above explain each of them.

**Refuted: the 403 sub-point.** The 403 is not offered as a reason on its own. It is a premise: the FEC cannot supply 2024, so following the item's text would give a history that mixes the two publications. That argument still holds.

**Why minor, not defect.** No CSV, generated figure or check is wrong. Choosing one publication for every year is still defensible. Reading 13's own pasted run shows the two bases' two-party splits differ by at most about 0.08 pp. The problem is that the stated reason for overriding Elias's text shows only one side, and the record that goes with it says something false.

**The skeptic's corrected fix.** Keep using the Clerk's recapitulation, and keep the 403 as the premise that 2024 can only come from the Clerk. In the House-basis bullet, add what the Clerk prints: each contest's final certified count. That means Louisiana's December runoff in place of the November primary for runoff candidates, Maine's last ranked-choice round (Alaska's first), and no 2018 NC-09, since that result was not certified. Reword the FEC side to match its notes: it adds a district's unexpired-term vote to its full-term vote and the December runoff to the November vote, prints Maine's first-choice totals, and keeps NC-09 2018's uncertified vote. Leave vote figures out of the plan, per the claim convention.

In reading 13, replace "explained by no saved page" with a list of which differences the saved footnotes explain (FEC 2018 for Maine; the Clerk for Maine 2022, Louisiana 2020 and North Carolina 2018) and which nothing explains. In us_returns_prep.pl:69-70 and :873-874, drop "same-day ... counted twice" and stop tying every year's differences to the FEC's notes. Make the same change in the unstaged :69-70.

The plan itself calls this a departure from Elias's text, so put the basis to him with both sides' conventions, in the R-US15 ask or on its own line, instead of only declaring it.

### 14. The staged mutation case h01 cannot fire: pdftotext -raw writes CRLF and the regex anchors on $ after the digits, so the case is MISSED

- **Lens:** records - **reviewer:** defect - **skeptic:** defect
- **Where:** Tools/us_returns_mutations.sh:102

**The scenario.** /mingw64/bin/pdftotext -raw to stdout on this machine ends every line with \r\n (4472 CR, 4472 LF for clerk_statistics2018.pdf). The staged program s{^(Alabama \.+ 678,687 975,737 .*?) 5,471 1,659,895$}{...}m needs $ right after "895", which never matches before \r.
I rebuilt the case on a scratch copy with the staged regex: the tool exited 0, wrote all six outputs and printed no MISMATCH. The script would report "MISSED a Clerk row's cell changed, its Total kept (2018, Alabama) (exit 0, 6 file(s) written)" and exit non-zero.
With (\r?)$ the same run is caught with the expected "clerk_statistics2018.pdf AL: its columns sum to 1659896, its Total is 1659895". That fix is in the working tree but UNSTAGED.
The record's paragraph (president_returns.md:37) is unchanged: "On 2026-10-05 it caught all twenty-nine", listing 24+5 cases. It names none of h01-h09 and gives no run showing the new failure paths proved.

**The fix proposed.** Stage the (\r?) fix for h01. Run the script on the staged tree. Update president_returns.md:37 with that run's date, its count (38) and the nine US-5 cases, as the standing rule requires.

**The skeptic's evidence.** I could not refute it. I reproduced the failure on the staged tree.

1. The staged case. `Tools/us_returns_mutations.sh:102` is `h01() { wrap 'BEGIN { undef $/ } s{^(Alabama \.+ 678,687 975,737 .*?) 5,471 1,659,895$}{$1 5,472 1,659,895}m'; }`. Under /m, the `$` after "895" needs a \n immediately after it.

2. How the tool gets the text. `wrap()` writes `pdftotext "$@" | perl -pe <prog>` and points `my $pdftotext` (prep.pl:85) at that script. The tool reads it through `open my $p, '-|', $pdftotext, '-raw', "$raw/$file", '-'` (prep.pl:805). `/usr/bin/perl` is "x86_64-cygwin-thread-multi", which does not translate line endings, so any \r survives.

3. The extractor's line endings. `/mingw64/bin/pdftotext` 4.06 `-raw clerk_statistics2018.pdf -` writes 427871 bytes containing 4472 CRLF pairs, 4472 LF and 4472 CR, so every line ends CRLF. No xpdfrc exists. The bytes after "5,471 1,659,895" are `<0d><0a>`.

4. The staged program does nothing. I rebuilt `wrap`'s script exactly, using the same printf %q. Its output is byte-identical to the unmutated text (sha256 9063906b... for both). With `(\r?)$` the output differs at line 4321, the Alabama recapitulation row.

5. The staged tool's run. I copied the inputs to a unique scratch directory and ran the staged prep.pl under the staged h01 wrapper. It exited 0, wrote all 6 outputs and printed no MISMATCH line. `case_` (lines 34-41) therefore prints "MISSED  a Clerk row's cell changed, its Total kept (2018, Alabama) (exit 0, 6 file(s) written)". `bad` goes up by one, and `[ $bad -eq 0 ]` makes the script exit 1.

6. The fixed program is caught. Under the `(\r?)` program the run exits 255 and writes 0 files. It prints "MISMATCH: returns/clerk_statistics2018.pdf AL: its columns sum to 1659896, its Total is 1659895" and also "...the states' Write-in sums to 64082, the Total row holds 64081". The case's expected text is in that output, so it counts as CAUGHT.

7. The fix is unstaged. Only the working tree has `...1,659,895(\r?)$}{$1 5,472 1,659,895$2}m   # the extractor's lines may end CRLF`; `git diff --cached` does not. No other staged h-case puts an end anchor after a figure (h03/h04/h05 use `[^\n]*\n`), so h01 is the only case affected.

8. Claims in the staged files. The staged prep.pl header (lines 26-27 and 64-66) says the mutation script "proves the failure paths below", including "a state row whose columns do not sum to its own Total". No staged case other than h01 produces that message, so this path is unproved in the staged state.

9. The record. Staged `president_returns.md:37` is an unchanged context line: "On 2026-10-05 it caught all twenty-nine" (24 input cases and 5 TOOL cases). It names no Clerk or House case. Nothing in the staged diff records a run of h01-h09. The staged script has 38 `case_` lines; the working tree has 57.

Severity stays at defect. The data and the tool's own check are correct, but the committed proof script would be red on the project's only environment, and the standing rule ("every new failure path proved by the mutation script") is not met for this path.

Housekeeping: the scratchpad directory is shared with the other agents. Early on I ran `rm -rf scratchpad/copy`, which may have disturbed another agent's scratch copy at about 02:53. No repository file was touched, and git status is unchanged.

**The skeptic's corrected fix.** Stage the working tree's h01 line, which captures `(\r?)` before `$` and writes it back as `$2`. Then run `bash Tools/us_returns_mutations.sh` on exactly the tree being committed and require exit 0 with every case CAUGHT.

Rewrite `president_returns.md:37` from that run, giving its date, its count and the US-5 cases by name, the way the paragraph names the others. The count is 38 if only h01-h09 are committed. It is 57 if the working tree's h10-h28 and the changed h09 expected text are staged too.

For any later `wrap` case: never anchor `$` right after page text. Allow `\r?` before it, or end the pattern with `[^\n]*\n` as h03 does.

### 15. "typed and read back before these pages were saved" is false for returns_2024.md: its only read-back was against the saved pages

- **Lens:** records - **reviewer:** minor - **skeptic:** note
- **Where:** ElectionsData/usa/president_returns.md:34

**The scenario.** returns_2024.md was "Filed verbatim from the research agent's return, 2026-08-28", with no read-back recorded then. Its only read-back is "Read back against the saved pages on 2026-10-05" (returns_2024.md:91), and COMPLETED §786 says the same. That came after fec_2024presgeresults.xlsx (16:11) and clerk_statistics2024.pdf (19:45) were saved.
The same phrase appears at president_returns.md:66 (reading 14), Tools/us_returns_prep.pl:17 (header) and :878 (code comment). It overstates how independent the 'typed' witness is: the figures the tool now holds to these pages were already checked against those same bytes.

**The fix proposed.** Say "typed before these pages were saved (read back against them on 2026-10-05)". Keep "typed and read back before" only for state_ev_2024.csv, whose header records a check against the live workbook.

**The skeptic's evidence.** The finding is real, but it is a note, not minor.

The claim is false as written. The phrase "typed and read back before these pages were saved" was introduced by this change in four places: president_returns.md:34 and :66 (reading 14), and the staged Tools/us_returns_prep.pl:17 (header) and :878 (code comment; :880 in the working tree). The commit it builds on (43169a7a) only said "typed before these pages were saved", and only of state_ev_2024.csv.

The repo's own records place the only read-back of returns_2024.md after the pages were saved, and against them:
- returns_2024.md:69, unchanged since 104cc32f (2026-08-28): "*(Filed verbatim from the research agent's return, 2026-08-28 night.)*". No read-back is recorded there.
- USA_STAGE_PLAN.md:106: "filed 2026-08-28/29 from an agent's return with **no raw bytes**".
- returns_2024.md:91, president_returns.md:89 and COMPLETED.md §786 all say "Read back against the saved pages on 2026-10-05".
- raw/returns/fetch_log.txt:4 shows `2026-10-05T16:11:11Z 200 ... fec_2024presgeresults.xlsx`, and :21 shows `2026-10-05T19:45:32Z 200 ... clerk_statistics2024.pdf`. These are exactly the two pages the new block holds the House line and the three presidential rows to (`$house_tot{2024}` from the Clerk PDF; `@candidates` from the FEC workbook).

So the typing came before the saving, but the read-back came after it and was made against those same bytes. "Read back before these pages were saved" never happened.

On severity, the finding's stated consequence is wrong. It says the read-back "overstates how independent the typed witness is", but the witness is still independent:
- `diff` shows lines 1-69 of returns_2024.md are byte-identical between 104cc32f (August) and the staged file. The October read-back changed no figure, so the figures the tool checks are still the August typing from the live pages.
- The tool's check therefore has the same evidential value either way.

What the phrase really does is overstate the verification history: it claims an August read-back that was never recorded. No figure is wrong, and no code depends on it:
- The generated UsPresidentialReturns.cs does not hash the tool.
- The comments do not affect any output.

The project's own precedent treats this kind of overclaim as a note. In Reviews/2026-10-05_s786 (second pass), #10 ("returns_2024.md says the tool holds its ... figures, but the tool never reads" it) was graded note/note: "No figure is wrong today, so it stays a note." First-pass #15 ("refused" vs status 000) and #17 ("checked 2026-10-05" with no log) were notes too.

**The skeptic's corrected fix.** Drop "and read back" in all four places, going back to the wording the earlier commit already used for state_ev_2024.csv: "typed before these pages were saved". The read-back is already recorded with its date at president_returns.md:89 and returns_2024.md:91, so nothing is lost.

The four edits:
- president_returns.md:34: "... the three presidential rows of `returns_2024.md` — typed before these pages were saved (read back against them on 2026-10-05) — are the pages' figures"
- president_returns.md:66: "Trump, Harris, Stein, typed before these pages were saved"
- us_returns_prep.pl:17: "2024's tables typed before these pages were saved, held to them"
- us_returns_prep.pl:878: "..., typed before these pages were saved, held to them"

Do not keep "read back before" even for state_ev_2024.csv. Its header records a column-sum cross-check (312/226 against the Total row and NARA), not a row-by-row read-back.

The edits touch comments and a record only. The generated catalog does not hash the tool, so no rerun or regeneration is needed.

### 16. Maine's ranked-choice columns are called "non-votes", but they re-count ME-2's ballots, so votes_total is inflated; the loyalty basis is left ambiguous

- **Lens:** records - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/usa/president_returns.md:58

**The scenario.** Reading 13 and the generated house_by_state.csv header (from Tools/us_returns_prep.pl:942) list "Maine's continuing and exhausted ballots" among "the non-votes".
- clerk_statistics2024.pdf, Maine: ME-2 Theriault 194,445, Golden 197,151, "Continuing Ballots 391,596" = 194,445 + 197,151. The same ballots are counted twice, so ME votes_total 2024 is 1,234,099 for about 842k ballots. R's share of votes_total is 28.3%, against about 41% of ballots.
- 2022: the Clerk's ME-2 "Exhausted Ballot 322,778" is more than the district's 316,382 votes on the FEC sheet. The row totals 656,104.
- The design's loyalty basis (USA_STAGE_PLAN.md:216, "Each party's share of all votes cast, the four countries' basis") does not say which House total it uses. votes_total is the only one the CSV carries. The four countries' histories are shares of valid votes (e.g. PartySystem.cs derives Germany 2021 from 'valid 46,298,387').

**The fix proposed.** Describe the continuing ballots as the continuing candidates' votes counted again, and name Maine's double count. In the design, declare the House denominator for loyalty: votes for candidates (R + D + party lines + write-ins), not the Clerk's Total, or exclude Maine's RCV columns.

**The skeptic's evidence.** I could not refute it. The Clerk's Maine pages show the double count, and the record calls it a non-vote.

The wording (staged):
- president_returns.md:58 (reading 13) says the Total holds "the non-votes some states report — blank, over and under votes, and Maine's continuing and exhausted ballots in a ranked-choice count".
- Tools/us_returns_prep.pl:942, the house_by_state.csv header: "the non-votes some states report among them (blank, over and under votes; Maine's continuing and exhausted ballots). votes_total: the Clerk's Total."

2024, from pdftotext -raw on clerk_statistics2024.pdf, Maine's recapitulation: "2d district ... 194,445 197,151 ... 391,596 11,340 ... 794,532".
- 194,445 + 197,151 = 391,596, so the "Continuing Ballots" figure is the two finalists' votes counted a second time.
- The Total row is 1,234,099. On the same page, the Senator and Presidential electors rows are 842,447.
- The CSV row is 2024,ME,349294,446949,437856,1234099. R is 28.3% of votes_total but 41.5% of 842,447.

2022, clerk_statistics2022.pdf: "2d district ... 146,142 165,136 ... 322,778 21,655 ... 393 656,104".
- The Clerk's own footnote says this is round 2, and gives round 1 as Golden 153,074 and Poliquin 141,260.
- In the FEC 2022 workbook (xl/worksheets/sheet8.xml), 153074, 141260, 21655 and 316382 each appear once, and 322778 does not appear.
- So the "Exhausted Ballot" figure is larger than every candidate vote in the district put together. It cannot be a count of non-votes.
- The CSV row is 2022,ME,275405,384889,353948,1014242.

Other years and states:
- The 2018 ME-2 page prints only the final-round Golden/Poliquin figures, and 2016 and 2020 have no ranked-choice columns.
- Alaska 2022 and 2024 print round 1 with no continuing column.
- So the double count is Maine's, in 2022 and 2024 only.

The basis claim: USA_STAGE_PLAN.md:216 (staged) says "Each party's share of all votes cast, the four countries' basis". The four countries' histories in PartySystem.TryRealHistory are official valid-vote shares (Germany's comment: "valid 46,298,387"). The untracked UsNationalVoteCheck.SharesOf, the s789 work in the working tree, divides by Σ h.VotesTotal, the Clerk's Total. That denominator includes the non-votes and Maine's double count, so it is not the four countries' basis.

How much it matters:
- PreferenceModel.Preference (lines 160-169) normalises the prior over the roster and computes result_i = λ_i·prior_i/Σprior + (1−λ_i)·persuaded_i. A fit that returns the prior gives the prior back whatever the loyalty, which the design itself declares.
- The poll, the two-party misses and the E1 factors do not move. E1 uses the FEC's presidential total, and scoring is two-party.
- Only case (a) 2024 reads an inflated Total (T-1 = 2022). Its printed loyalty R/D is about 94.45/94.08, against about 94.17/94.36 with ME-2's 322,778 removed: a move of about 0.3 points.
- The data itself is transcribed faithfully. What is wrong is how a SOURCED record and a generated header describe a column, and a reader who takes votes_total as ballots is wrong by about 13 pp on Maine 2024. Minor fits.

**The skeptic's corrected fix.** 1. Reword reading 13 (president_returns.md:58) and the CSV header (Tools/us_returns_prep.pl:940-942, then re-run the tool so the header regenerates). Say that the Clerk's Maine 2nd-district rows of 2022 and 2024 count the district's ballots a second time:
   - 2024's "Continuing Ballots" are exactly the two finalists' final-round votes;
   - 2022's "Exhausted Ballot" is larger than every vote cast for a candidate in the district;
   - so Maine's votes_total exceeds its ballots in those years. The two-party split is untouched, and a share of votes_total is not a share of ballots.
   Optionally the tool could also check that 2024 ME-2's continuing ballots equal R + D.
2. In USA_STAGE_PLAN.md:216, and in the card's matching row in the working tree, name the House denominator: the Clerk's Total. Either drop "the four countries' basis" or state the difference (non-votes and Maine's double count are in the denominator). Note that this is inert for the two-party poll, so only the printed loyalty moves, by about 0.3 points on (a) 2024.
   - The finding's alternative denominator (R + D + party lines + write-ins) cannot be computed from house_by_state.csv, because votes_other mixes third-party lines with non-votes. It would need a new column.
3. A related error for the same edit: reading 13 says "The other differences below are explained by no saved page". That is false for ME 2022. The Clerk's own 2022 Maine footnote (round 2 printed; round 1 Golden 153,074, Poliquin 141,260) gives exactly the printed "ME D -12062 R -4882".

### 17. The design says every fit returns T-1's split through Preference "whatever the loyalty", but the free fit is VoteModel.Calibrate's grid, which leaves a residual

- **Lens:** records - **reviewer:** minor - **skeptic:** note
- **Where:** docs/specs/USA_STAGE_PLAN.md:212

**The scenario.** Line 212 says "an electorate fitted to T-1 returns T-1's split through PreferenceModel.Preference whatever the loyalty. The instrument proves that identity on every fit". The free fit (line 213) is VoteModel.Calibrate: a grid of 17x17 means x 6 sigma x 6 tau, keeping the first strict minimum. It does not reproduce a split exactly.
I re-ran that grid in perl, mirroring PredictShares, with DEM (3.73,2.41), REP (8.23,8.30) and wEcon 0.65. Against 2020's two-party REP share from the catalog (0.477302), the best grid point is REP 0.477331 (MAD 0.0029 pp; mu (6,6), sigma 3.5, tau 16). Preference then returns lambda*0.477302 + (1-lambda)*0.477331, which does move with loyalty. An exact identity check on the free fit fails. Only the held fits, solved continuously, satisfy it exactly.

**The fix proposed.** State that the identity is exact for the held fits, and holds for the free fit only within (1-lambda) x the grid residual. Declare the tolerance the instrument asserts, or print the residual beside each free fit.

**The skeptic's evidence.** The maths in the finding holds. PreferenceModel.Preference (per-party form) computes `result[i] = lambda * (priorShares[i] / priorSum) + (1.0 - lambda) * persuaded[i];` and then normalises. persuaded is the spatial split after GateReRun.ToCompatScale (`100.0 * Math.Pow(shares[i] / max, 1.0 / PreferenceModel.Sharpness)`), which PersuadedShares cubes straight back to the spatial shares. So the output equals T-1's split for every loyalty only when the spatial split equals the prior exactly.

VoteModel.Calibrate is a discrete grid: mu and ms run from 3.0 to 7.0 in steps of 0.25, sigma is one of {1,...,3.5}, tau one of {0.5,...,16}, and `if (mad < bestMad)` keeps the first strict minimum. It cannot hit a continuous target.

The project's own generated card (working tree docs/reference/US_ELECTIONS.md, the s789 block) prints the free fits' residuals as 0.0004, 0.0001, 0.0021, 0.0029 and 0.0018 pp. Presidential 2020 reads `free | 6.000, 6.000 | 3.50 | 16.00 | ... | 0.0029`, which is exactly the finding's replication. The held fits print 0.0000: HeldFit bisects 80 times and checks `< 1e-9`.

Staged line 212 still says the electorate "returns T−1's split through `PreferenceModel.Preference` whatever the loyalty. The instrument proves that identity on every fit". For the free fit that is literally false: at loyalty 0 the output is the grid's split, up to 0.0029 pp off.

Against that, the code already handles it. The as-built instrument (untracked Assets/Editor/UsNationalVoteCheck.cs:215-216, not part of the staged diff) never runs an exact check: `double off = Math.Abs(p[0] - priorSplit), residual = Math.Abs(spatial[0] - priorSplit); Check(off <= residual + 1e-12, ...)`. Its card prints a residual column and says "Every fit holds its T-1 split (free: within Calibrate's grid; held: solved)". So the scenario "an exact identity check on the free fit fails" does not occur.

The poll's distance from the prior is at most (1-lambda) times the residual. With the card's loyalties (about 89 to 98) that is about 0.0003 pp at most, below the 0.01 pp the card prints, so no pin or printed figure moves.

What remains is unqualified wording in three places: the staged declaration (working-tree line 214, unchanged), the spec's Built line ("the vote model gives the prior back, proved on every fit"), and the card's hand-written "The identity" paragraph. That is a wording note, not a minor defect.

**The skeptic's corrected fix.** Wording only, no code change. In the US-5 design bullet "What the fit reads" (staged line 212, working-tree line 214), qualify the identity: it is exact for the held fits (solved continuously), and for the free fit it holds only within (1 − loyalty) × `VoteModel.Calibrate`'s residual. That is the bound `UsNationalVoteCheck` asserts (the poll no farther from the prior than the fit's own residual), and the card prints the residual beside each fit. Do not transcribe the residual figures into the spec. Point to the card's residual column instead, per the claim convention. Carry the same qualifier into the spec's "Built:" line ("gives the prior back, proved on every fit") and into the card's hand-written "The identity" paragraph, to match the generated line "free: within Calibrate's grid; held: solved".

### 18. Derived figures transcribed into non-exempt files (source comments and the spec)

- **Lens:** records - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_returns_prep.pl:69

**The scenario.** - us_returns_prep.pl:69: "every year read differs in at most eleven" copies a run result (2016: 39 of 50 identical) into a source comment. It goes stale silently if a page is re-fetched or the parse changes.
- USA_STAGE_PLAN.md:214: "the USA's declared 0.65" duplicates VoteShareBacktest's literal. The working tree is already moving that literal into a named constant, which this copy will not follow.
- USA_STAGE_PLAN.md:222: "before 20 Jan 2021" and "begins in 2018" are facts about WorldClock.Governments' first US row and PreStartRecord.USA's first row.
- GeneratedCatalogCheck.cs:517: "five CSVs" is a count, following the old "three CSVs".
The claim convention allows such figures only as GENERATED, REFERENCED, or DELETED.

**The fix proposed.** Point to where the facts live instead: "the tool prints the states that differ each run"; `VoteShareBacktest`'s US weight; the first US row of `WorldClock.Governments`; the first row of `PreStartRecord.USA`. Drop the counts.

**The skeptic's evidence.** I tried to refute this and couldn't. All four quoted lines are in the index, each is a DERIVED fact, none is GENERATED or REFERENCED, and none of the files is exempt.

The rule. CLAUDE.md:39-41 counts "a count, a figure" as DERIVED and says "Nobody transcribes, anywhere, in any file". Only COMPLETED.md is exempt. docs/archive/CLAIM_CONVENTION_AND_DISCIPLINE.md adds: "This binds SOURCE COMMENTS exactly as it binds markdown", the fix is "a reference or a generated block - never writing today's number in place of yesterday's", and "name the command that produces it instead of its result".

The four items, read with `git show :path`:
1. Tools/us_returns_prep.pl:69 says "every year read differs in at most eleven". That is a run result. Reading 13 of president_returns.md pastes the run: 39, 42, 43 and 45 of 50 states identical (2016, 2018, 2020, 2022), so at most 11 differ. The same file's body comment at :873 already says "differs in a few", and the printf at :1011 prints the differing states on every run.
2. docs/specs/USA_STAGE_PLAN.md:214 says "the USA's declared 0.65 (`VoteShareBacktest`'s ...)". The 0.65 is the literal at VoteShareBacktest.cs:177 in the index (`}, new[] { 48.32, 49.80 }, 0.65,`).
3. USA_STAGE_PLAN.md:222 says "No world can be built before 20 Jan 2021: `GovernmentRecord.AtStart` throws, and the US pre-start record begins in 2018". Both are facts about the code:
   - the first US row in WorldClock.cs is `D(2021, 1, 20)`, with `Holds => date >= From && date < Until`;
   - AtStart throws when `TryGovernmentAt` fails (GovernmentRecord.cs:254-257);
   - the first `PreStartRecord.USA` row is `(2018, 1, 4f, float.NaN)`.
4. GeneratedCatalogCheck.cs:517 says "five CSVs and one catalog", replacing the base's "three CSVs". That is the convention's own named anti-pattern (CheckSuite's "TWENTY-ONE").

Every one of these is true today and none affects runtime. That is why this is minor, not a defect. DocumentClaimCheck (root *.md only) and CommentClaimCheck (backticked references only) would not catch any of them.

What the working tree shows. The unstaged copy already rewrites three of them the way the finding proposes:
- prep :69 becomes "differs in a few, the run prints which";
- spec :214 becomes "`VoteShareBacktest.UsEconomicWeight`";
- catalog :517 becomes "its CSVs".

So the finding's "this copy will not follow" is wrong about the working tree. But none of those fixes is staged. Spec :222 (worktree :224) is unchanged in both the index and the working tree.

The same kind of count, not named in the finding, is in both the index and the working tree:
- "All six outputs" at us_returns_prep.pl:74;
- "all six texts" at us_returns_prep.pl:911;
- "All six outputs" at president_returns.md:35.

**The skeptic's corrected fix.** Stage the working tree's rewrites before the s788 commit:
- prep :69: "every year read differs in a few, the run prints which".
- GeneratedCatalogCheck :517: "its CSVs".
- Spec :214: `VoteShareBacktest.UsEconomicWeight` exists only in the unstaged VoteShareBacktest.cs. Either stage that constant in the same commit, or write "the USA's declared weight (`VoteShareBacktest`'s US case)" so the s788 tree names no member it lacks.

Rewrite spec :222 as a pointer: "No world can be built before the first US row of `WorldClock.Governments` (`GovernmentRecord.AtStart` throws), and the term's economy cannot be read before `PreStartRecord.USA`'s first row."

Drop the output counts too: "All the outputs are built and tested before the first is written" at us_returns_prep.pl:74 and :911, and at president_returns.md:35.

### 19. The 2018 and 2022 Clerk volumes are called "Statistics of the Presidential and Congressional Election"

- **Lens:** records - **reviewer:** minor - **skeptic:** minor
- **Where:** docs/specs/USA_STAGE_PLAN.md:217

**The scenario.** The title pages of clerk_statistics2018.pdf and clerk_statistics2022.pdf read "STATISTICS OF THE CONGRESSIONAL ELECTION". The register's new rows title them correctly. The design paragraph ("Statistics of the Presidential and Congressional Election, 2016–2024"), Tools/us_returns_prep.pl:9 and :795, and the generated house_by_state.csv header (from us_returns_prep.pl:941) give every year the presidential-year title.

**The fix proposed.** Say "the Clerk's Statistics of the (Presidential and) Congressional Election", or "the Clerk's Statistics, 2016–2024".

**The skeptic's evidence.** The finding holds. I couldn't refute it from the pages or the code.

1. The 2018 and 2022 volumes carry the midterm title. Running `pdftotext -raw` on the first page of each:
   - `clerk_statistics2018.pdf` prints "STATISTICS / OF THE / CONGRESSIONAL ELECTION / FROM OFFICIAL SOURCES FOR THE ELECTION OF / NOVEMBER 6, 2018". Its running head (text line 16) is "STATISTICS OF THE CONGRESSIONAL ELECTION OF NOVEMBER 6, 2018".
   - `clerk_statistics2022.pdf` prints the same with "NOVEMBER 8, 2022".
   - A case-insensitive search for "presidential" finds nothing in either volume.
   - The 2016, 2020 and 2024 volumes are the only ones titled "PRESIDENTIAL AND CONGRESSIONAL ELECTION".

2. The staged change gives every year the presidential-year title in four places:
   - `docs/specs/USA_STAGE_PLAN.md:217` (staged numbering): "The Clerk of the House's *Statistics of the Presidential and Congressional Election*, 2016–2024, its "Recapitulation of Votes Cast for United States Representatives"".
   - `Tools/us_returns_prep.pl:9`: "(US-5, s788) the Clerk of the House's Statistics of the Presidential and Congressional Election, 2016-2024: its".
   - `Tools/us_returns_prep.pl:795`: "# The Clerk of the House's Statistics of the Presidential and Congressional Election prints on one page the "Recapitulation of Votes Cast for".
   - `Tools/us_returns_prep.pl:941` writes the CSV header "(its Statistics of the Presidential and Congressional Election)". That lands as line 3 of the generated `ElectionsData/usa/house_by_state.csv`, above the rows for every year 2016-2024, including the 2018 and 2022 rows.

3. The same change titles the two volumes correctly elsewhere, so it contradicts itself. The register rows in `ElectionsData/usa/president_returns.md` (lines 108 and 110) read "Statistics of the Congressional Election of November 6, 2018" and "... of November 8, 2022". Reading 13 says only "The Clerk of the House's *Statistics*".

4. Why this is minor and not a defect:
   - No figure depends on the cover title. The tool only matches `^Recapitulation of Votes Cast for United States Representatives, Election of ` and checks that line for the year.
   - The register, the URLs and the file names (`statistics2018.pdf`, `statistics2022.pdf`) all point to the right volumes.
   - The cost is a false source title in a DECLARED design paragraph, in the tool's comments and in a generated data header. In a repo where the record of sources is held strictly, that is a real records defect, but a small one.

5. Mitigating context: the base commit 43169a7a already had this title on the plan's source-table row (base line 692, staged line 709): "*Statistics of the Presidential and Congressional Election*, 2016–2024". The item text at :202 uses the title only for 2024, which is correct. This change widened the Clerk source from 2024 to every year and carried the 2024 title over to all of them.

**The skeptic's corrected fix.** Name the series without the presidential-year title:
- `USA_STAGE_PLAN.md:217`: write "the Clerk of the House's *Statistics* of each election (of the Presidential and Congressional Election in 2016, 2020 and 2024; of the Congressional Election in 2018 and 2022)", or simply "the Clerk of the House's *Statistics*, 2016–2024".
- `us_returns_prep.pl:9` and `:795`: make the same change.
- `us_returns_prep.pl:941`: change the header string to something like "(the Clerk's Statistics of each election)" or "(its Statistics of the (Presidential and) Congressional Election)".
- Then re-run `perl Tools/us_returns_prep.pl` so that `house_by_state.csv` and the digests in the generated `UsPresidentialReturns.cs` change together. Never hand-edit the CSV.
- Optionally also correct the older source-table row in the plan (staged line 709). It carries the same title, and after this change it is out of date in two other ways: it says "US-5 (2024's national summary table only)", and "2016–2020: not probed" although the 2016-2020 volumes were fetched with a 200 status.

### 20. The plan's sources table still limits US-5 to the Clerk's 2024 table and lists 2016–2020 as not probed

- **Lens:** records - **reviewer:** minor - **skeptic:** note
- **Where:** docs/specs/USA_STAGE_PLAN.md:709

**The scenario.** Row 709 reads "US-5 (2024's national summary table only) ... 2016–2020: not probed". The design added in this change makes the Clerk the House basis for every year, and the fetch log records 2016, 2018 and 2020 at HTTP 200 on 2026-10-05. The same document now gives two bases for US-5.

**The fix proposed.** Update the row: US-5's basis is every year 2016–2024, with the hosts reached on 2026-10-05; or mark it superseded by the design paragraph.

**The skeptic's evidence.** Line numbers are from the staged file (`git show :docs/specs/USA_STAGE_PLAN.md`).

WHAT HOLDS. Line 709: "| US-5 (2024's national summary table only), US-11 (the districts) | The Clerk of the House, *Statistics ...*, 2016–2024 (PDF; inflated by perl) | 2022 and 2024: 200; 2016–2020: not probed |". The design paragraph this change adds, line 217: "*The House basis.* The Clerk of the House's *Statistics ...*, 2016–2024, its "Recapitulation ...": 50 states ... one publication for every year." So the row's first cell gives US-5 a narrower Clerk scope than the design does. Before this change the row and the item text agreed. The row was not edited in the staged change or in the unstaged working-tree edits. fetch_log.txt does show 200 for clerk_statistics2018.pdf and clerk_statistics2020.pdf at 2026-10-05T23:51:28Z and clerk_statistics2016.pdf at 23:55:23Z.

WHAT IS REFUTED OR WEAKER THAN CLAIMED:
(1) The status half. Line 696 dates the column: "Host status is the 2026-10-04 probe's (by status code); *not probed* means unknown." That probe did not reach 2016–2020, so "not probed" is true as dated. The repo does not refresh this column: line 702 "| US-3 | FEC, *Federal Elections 2012* | not probed |" stands although US-3 fetched it at 200 (fetch_log 2026-10-05T16:15:30Z fec_federalelections2012.xls). s786 (947921ab) changed one plan line and no table row.
(2) The table is a plan-time list, never updated after a build. Line 703 still names "Maine Secretary of State tabulations ... 2012–2024", although US-3 read Maine 2012 and 2016 from the Governor's certificates. This row's own "(PDF; inflated by perl)" is plan-time too: the staged tool reads the Clerk's PDFs with `open my $p, '-|', $pdftotext, '-raw', "$raw/$file", '-'` (us_returns_prep.pl line 805).
(3) The departure is declared where US-5 is specified. Line 217: "This departs from the item's text, which named the FEC where it serves." Reading 13 of president_returns.md: "(DECLARED for US-5, docs/specs/USA_STAGE_PLAN.md)".
(4) Nothing reads the row. grep over Assets/Editor and Tools finds no reader of the sources table. The only references are us_returns_prep.pl line 797 and UsNationalVoteCheck.cs line 20, and both cite US-5's design.

Net: a stale plan-time scope note in a table the repo does not maintain after builds, with the departure declared at the item. Real, but a note, not minor.

**The skeptic's corrected fix.** Change only the US-5 scope in the row's first cell, and point it at the design instead of restating it: e.g. "US-5 (the House recapitulation, every year: US-5's design, §788), US-11 (the districts)". Leave the status column as the 2026-10-04 probe's. Writing "2016–2020: 200 (2026-10-05)" into it would contradict line 696's header and the other rows (line 702's FEC 2012 "not probed" was left after US-3 fetched it). If the owner wants live host status in this table, re-head the column and update every row together, FEC 2012 included. Otherwise leaving the row unchanged is consistent with US-3's precedent, since line 217 already declares the departure.

### 21. (Outside this lens) The staged mutation case h01 never applies because pdftotext's lines end in CRLF; the fix is unstaged

- **Lens:** catalog - **reviewer:** defect - **skeptic:** defect
- **Where:** G:/UNITY/Projects/PoliSim/Tools/us_returns_mutations.sh:102

**The scenario.** /mingw64/bin/pdftotext -raw on clerk_statistics2018.pdf writes CRLF line endings: 4472 CR and 4472 LF, and the Alabama row ends in CRLF. The staged h01 anchors '5,471 1,659,895$' under /m, and that cannot match before the \r. I ran both patterns on that exact output with perl -0777: the staged pattern does not match; the working tree's '(\r?)$' version does. So the mutation never applies, the tool exits 0, and the case prints MISSED. In the staged snapshot, the tool's check of the Clerk's columns against each row's Total (us_returns_prep.pl:836) is therefore unproved. git status shows 'MM Tools/us_returns_mutations.sh', so the working tree already holds the fix.

**The fix proposed.** Stage the working-tree version of h01 before committing, then re-run Tools/us_returns_mutations.sh on the staged state.

**The skeptic's evidence.** I could not refute it. I reproduced it from the code and by running the staged files.

1. The staged h01 at Tools/us_returns_mutations.sh:102 (index blob 8d1cbbe7) is:
   `h01() { wrap 'BEGIN { undef $/ } s{^(Alabama \.+ 678,687 975,737 .*?) 5,471 1,659,895$}{$1 5,472 1,659,895}m'; }`
   `wrap` (lines 26-29) writes `printf '#!/bin/bash\n%s "$@" | perl -pe %q\n' "$PDFT" "$1"`, so the program runs on pdftotext's raw stdout. The perl there is /usr/bin/perl, the only perl on PATH, also inside a script started from perl. Its STDIN layers are `unix,perlio`, so there is no :crlf translation.

2. `/mingw64/bin/pdftotext -raw clerk_statistics2018.pdf -` (xpdf 4.06, no xpdfrc anywhere) writes 4472 CR and 4472 LF, with 0 bare LF and 0 bare CR. The Alabama row's last bytes are `39 2c 38 39 35 0d 0a`. In perl, `"a 1\r\n" =~ /^a 1$/m` does not match, and `/^a 1\r?$/m` does.

3. I piped that output through the staged program: the result is byte-identical to the input (sha256 9063906b... both times). The working tree's `(\r?)$ ... $2` version changes it (0849097a...), and the row becomes `... 5,472 1,659,895`.

4. End to end, I built a copy of the index: tool blob 31afa19e, script blob 8d1cbbe7, and ElectionsData/usa, which has no unstaged changes. I ran the script's own fresh/wrap/case_ on it. The staged h01 printed `MISSED  a Clerk row's cell changed, its Total kept (2018, Alabama) (exit 0, 6 file(s) written)`. The working tree's h01 on the same staged tool printed `CAUGHT`. My copy's control showed BROKEN only because I used the LF index blob of the generated .cs while the tool writes CRLF. With CR stripped the two are identical, so this does not affect h01.

5. As a result, the staged snapshot never proves staged us_returns_prep.pl:836: `problem("$file $st: its columns sum to $parts, its Total is $v[$ix{Total}]") unless $parts == $v[$ix{Total}];`. The script would also end `[ $bad -eq 0 ]` with exit 1.

6. The tool itself tolerates CR (`split ' '`, `trim`, `\s*$`), so the control passes and the miss is only in the proof. The other staged h-cases do not anchor on `$`: h03/h04/h05 use `[^\n]*\n`, which takes the CR, and h02/h09 do not depend on line ends. Only h01 breaks.

7. `git status` shows `MM Tools/us_returns_mutations.sh`, and `git diff` holds exactly this h01 fix. Since the review began, the working tree has also gained h10-h28, a changed h09 message and unstaged edits to us_returns_prep.pl, UsPresidentialReturns.cs and GeneratedCatalogCheck.cs.

Severity stays at defect: if the change is committed as staged, the commit's proof script exits 1 and one of its new failure paths stays unproved, which breaks the standing rule. The impact is limited, though. The problem is only in the test harness, it fails loudly rather than silently, and the fix is already written. It just isn't staged.

**The skeptic's corrected fix.** Stage the working tree's h01 line before committing: `5,471 1,659,895(\r?)$` with `$2` in the replacement. Use `git add -p` for that hunk, or stage the whole review pass together: the mutation script plus the matching edits to us_returns_prep.pl and the regenerated UsPresidentialReturns.cs. The working tree now also holds h10-h28, which need the unstaged tool changes. Then run `bash Tools/us_returns_mutations.sh` against exactly what is staged, for example a clean checkout of the index, and confirm that h01 prints CAUGHT and the script exits 0. For any later wrap case anchored at line end, write `\r?$`, or match `[^\n]*\n`, because xpdf's pdftotext on Windows always ends lines with CRLF.

### 22. Unused local 'rows' in the candidate-year loop

- **Lens:** catalog - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/GeneratedCatalogCheck.cs:603

**The scenario.** 'rows' is declared at line 603 and incremented at 607 but never read; no test or message uses it. It produces no compiler warning (the scratch build was clean), and DeadStateCheck matches only private fields and methods, so the bar won't flag it either.

**The fix proposed.** Delete it, or use it, for example to print each year's candidate count.

**The skeptic's evidence.** Staged GeneratedCatalogCheck.cs, CheckUsPresidentialReturns, the candidate-year loop (lines 600-619):
603: `int rows = 0, nomineesR = 0, nomineesD = 0;`
607: `rows++;`
`grep -nw rows` over the staged file finds only 603 and 607 inside this method. The next matches are 649 (a doc comment) and 653 (`var rows = new List<string[]>()`, a separate local in ReadUsCsv). No interpolation, wrong.Add message or summary line reads it; the summary prints `candidates.Count` (line 645). So the local is write-only.

Compiler: a scratch copy of the pattern built with the .NET 10 SDK and AnalysisLevel latest-all gives CS0219 on a planted `int planted = 0;` but nothing on the incremented local. Roslyn counts `x++` as a read, so the build being clean is expected.

DeadStateCheck.cs: its doc says "Declarations matched: private fields, private methods, and enum members", and its PrivateField regex is anchored on `private`, so locals are never checked. The finding's claims all hold.

No missing check: `candidateYears` is built from UsPresidentialReturns.Candidates itself, so every year has at least one row and a guard on `rows` could never fire. The real checks are the row-by-row SameRow, `nomineesR != 1 || nomineesD != 1`, and the field/ticket sums against States. The doc comment promises no per-year count. No output or behavior depends on `rows`, so it is a dead-code note, not a defect.

**The skeptic's corrected fix.** Delete it: line 603 becomes `int nomineesR = 0, nomineesD = 0;` and remove `rows++;` at line 607. Do not use it instead: every year in candidateYears has at least one row by construction, so any check on `rows` would be vacuous, and the summary already prints candidates.Count.

### 23. The guard on candidate labels for C# literals does not refuse line breaks

- **Lens:** catalog - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Tools/us_returns_prep.pl:504

**The scenario.** A candidate label goes into both a CSV cell and a C# string literal, but the tool refuses only a comma, a quote or a backslash. trim() strips only the ends, and xlsx_sheet keeps a line break inside a shared string, whether written literally or as &#10; decoded by ent(). If a re-fetched workbook had a candidate header wrapped in Excel (Alt+Enter), the tool would write a CSV row split over two lines and a C# literal containing a raw newline. That is compile error CS1010, which stops every Editor check. The pinned workbook has no such label: the tool reproduces the staged bytes.

**The fix proposed.** Add the control characters (\x00-\x1f) to the refused character class.

**The skeptic's evidence.** THE CODE (staged Tools/us_returns_prep.pl)
- Line 504, new in this change: `problem("$file: the candidate column '$label' holds a comma, a quote or a backslash") if $label =~ /[,"\\]/;`. It does not refuse CR or LF.
- Line 259, trim: `$s =~ s/^\s+|\s+$//g`. It strips the ends only, so a line break inside a label survives.
- Line 127, xlsx_sheet: shared strings are matched with `<si>(.*?)</si>`gs and `<t[^>]*>(.*?)</t>`gs, then passed through ent(), which turns `&#(\d+);` into chr(). A literal LF is kept, and so is `&#10;`.
- Lines 470 and 474: `$col{trim($c->{1}{$_})} = $_`, then `@cand = grep { $_ > $evD && $_ < $vT } ...`. Every column between those two is taken by position, not by name.
- Only two labels are checked by exact name: TRUMP and HARRIS (they die if not found). STEIN is checked incidentally by the returns_2024.md lookup `$vote{$who}` (894-898). Nothing else checks the other 23 labels, such as KENNEDY, OLIVER, NONE OF THESE CANDIDATES and WRITE-INS (SCATTERED).
- write_all (912-916) tests only `/[^\x00-\x7F]/`, and LF is ASCII.
- The label is then written raw: line 948 `join(',', @$_)` into the CSV, line 984 `\"$_->[1]\"` into the C# literal, and line 990 `$cs =~ s/\n/\r\n/g` turns the LF into CR LF.

THE PROOF (scratch copies of ElectionsData/usa only)
- Control, staged tool: exit 0. All six outputs are identical to the staged ones (CSVs byte for byte, the C# apart from line endings). Roslyn csc (.NET SDK 10.0.302) compiles the control C# with no errors.
- Mutation: in the copy's sharedStrings.xml, `<si><t>NONE OF THESE CANDIDATES</t></si>` gets a literal LF between THESE and CANDIDATES, as an Excel Alt+Enter break writes it. Its SHA256SUMS line is re-pinned in the copy.
- Result with the staged tool: exit 0, no MISMATCH, and all six files written.
- The CSV row splits into `2024,NONE OF THESE` and `CANDIDATES,-,19625`.
- The C# bytes are `"NONE OF THESE\r\nCANDIDATES"`. csc reports `UsPresidentialReturns.cs(326,20): error CS1010: Newline in constant`, plus CS1026 and CS1003.
- This breaks the tool's own promise that it dies and writes nothing when an input is wrong. Because it is a compile error, nothing fails silently.

HOW LIKELY
- The pinned 2024 workbook has no line break in any of its 100 shared strings.
- Line breaks do appear in sibling workbooks. The FEC volumes for 2016, 2018 and 2020 carry raw LFs in cell text, though only trailing ones, which trim removes. The Census 2020 Table 1 wraps its header cells mid-text ("NUMBER OF APPORTIONED REPRESENTATIVES BASED ON \n2020 CENSUS2"), and the tool's `\s+` regex already tolerates that.
- Hitting this needs a deliberate re-fetch and re-pin of the 2024 workbook.

WHY MINOR, NOT NOTE
- The path is proved, and when hit it stops every check across the whole project.
- It is not reachable with the pinned bytes, and it fails loudly.

ALREADY IN PROGRESS
- The main session's unstaged working copy (02:42:51) already changes line 504 to `/[,"\\\r\n]/`, with the message "...or a line break".
- On the same mutated copy that version exits 255 with "1 mismatch(es) - nothing written" and writes no files.
- Neither the staged nor the working-copy us_returns_mutations.sh has a case for this guard, for any of its branches.

**The skeptic's corrected fix.** Refuse CR and LF at line 504: `problem("$file: the candidate column '$label' holds a comma, a quote, a backslash or a line break") if $label =~ /[,"\\\r\n]/;`. This is what the main session's unstaged working copy already holds, and I checked that it catches the case.

Why CR and LF are enough:
- An xlsx is XML 1.0, so the only control characters it can carry literally are TAB, LF and CR.
- TAB is legal both in a C# regular string literal and in the CSV that GeneratedCatalogCheck reads, which splits on '\n' and ','.
- The other C# line breaks (U+0085, U+2028, U+2029) are non-ASCII, and write_all's ASCII test already refuses them.

The finding's `\x00-\x1f` class is also acceptable; it refuses everything this one does and more.

Still owed under the standing rule that every new failure path is proved by the mutation script: a case h10 that gives a candidate header a line break in the copy and requires "holds a comma, a quote, a backslash or a line break". Tools/xlsx_poke.pl only rewrites worksheet parts, so the case has two options:
- extend xlsx_poke.pl to rewrite xl/sharedStrings.xml, or
- poke the header cell in the sheet part from `t="s"` to an inline string holding the line feed.

That case would also be the first to prove the comma/quote/backslash branch of this guard, which is new in s788 and has no mutation case yet.

### 24. 'five CSVs' in the check's docstring is a transcribed count

- **Lens:** catalog - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/GeneratedCatalogCheck.cs:517

**The scenario.** The docstring changed from 'three CSVs' to 'five CSVs and one catalog'. That is a count about the code, and it becomes wrong the day a sixth CSV is read. The claim convention in CLAUDE.md's head says a derived count must be generated, referenced or deleted. No check catches it: CommentClaimCheck reads only backticked Type.Member references.

**The fix proposed.** Write 'the CSVs and one catalog'; the count is whatever the ReadUsCsv calls below read.

**The skeptic's evidence.** I could not refute it. Here are the lines.

1. **The change.** The staged GeneratedCatalogCheck.cs:517 (hunk @@ -514) replaces "the US presidential returns - three CSVs and one catalog" with "the US presidential returns - five CSVs and one catalog (US-5, §788, added 2024's candidates and the House by state)". The finding misquotes the old text slightly ("three CSVs and one catalog" was the full phrase), but the substance is right.

2. **The count is true today.** The method makes five `ReadUsCsv(usa, ...)` calls, at :550-552 (year, state, district) and :576-577 (candidate, house_by_state). Tools/us_returns_prep.pl:991-992 `write_all(...)` writes those five CSVs plus UsPresidentialReturns.cs.

3. **It breaks the rule as written.**
   - CLAUDE.md:39 lists "a count" and "there are N of X" as DERIVED.
   - CLAUDE.md:40 allows a DERIVED claim only as GENERATED, REFERENCED or DELETED.
   - CLAUDE.md:41 says "Nobody transcribes, anywhere, in any file"; the convention's heading says it governs every source comment.
   - The long form (docs/archive/CLAIM_CONVENTION_AND_DISCIPLINE.md:38-44) says it binds source comments too. Its worked example is CheckSuite.cs, whose comment said "TWENTY-ONE" while `Suite` held twenty-five. Being right next to the counted code was no exemption there.
   - The same passage says the fix is "a reference or a generated block - never writing today's number in place of yesterday's." Changing "three" to "five" is exactly that.

4. **The failing path happened in this diff.** The convention's test is "the code can change freely and no document becomes wrong". Adding two `ReadUsCsv` calls and two `write_all` outputs made "three" wrong, and the line had to be rewritten. The next CSV makes "five" wrong in the same way. The claim also covers the tool's outputs ("all written by one run of `Tools/us_returns_prep.pl`"), which live in another file. So a sixth output at us_returns_prep.pl:991 would make it wrong without anyone touching this method.

5. **No check catches it.**
   - CommentClaimCheck.cs:43 only matches a backticked `Type.Member`.
   - DocumentClaimCheck reads .md files only.
   - PhantomGuardCheck.cs:40 only matches names ending in Check, Harness or Diagnostic.
   - Tools/claim_census.sh is "NOT A CHECK" and reads root *.md only.

6. **What argues against it, and why it doesn't hold.**
   - The s786 review left this exact line alone, twice: Reviews/2026-10-05_s786_us3_presidential_returns.md:654 ("At most a note"), :667 and :2064 ("Leave GeneratedCatalogCheck.cs:517 as it is").
   - Its reason was that "adding a year does not change it" and that the line describes the method's own hard-coded reads.
   - That reason only covered adding a year. It did not cover adding a CSV, which is what this change does.
   - The CheckSuite example shows a count sitting next to its own code is still forbidden.
   - The sibling text was already rewritten without the count at s786: .gitattributes:4 says "the generator's CSVs", and president_returns.md says "the CSVs as C# literals". This line is the last one still counting CSVs.

7. **Severity: note.** The line is true today and has no runtime effect. It is one numeral, the same grade the earlier review gave this class.

**The skeptic's corrected fix.** Comment-only edit at Assets/Editor/GeneratedCatalogCheck.cs:517. Replace "the US presidential returns - five CSVs and one catalog (US-5, §788, added 2024's candidates and the House by state), all written by one run of `Tools/us_returns_prep.pl` from saved pages" with "the US presidential returns - the CSVs and the catalog (US-5, §788, added 2024's candidates and the House by state), all written by one run of `Tools/us_returns_prep.pl` from saved pages". The count then lives only in the `ReadUsCsv` calls below and in the tool's `write_all`, and the generated catalog's header already names the CSVs. Nothing else needs to change: no CSV, no catalog bytes, no regeneration.

### 25. (Outside this lens) 'at most eleven' in the tool's header comment is a transcribed measured figure

- **Lens:** catalog - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Tools/us_returns_prep.pl:69

**The scenario.** The failure-path list says the two publications differ 'in at most eleven' states every year. That count was measured from the pages (the run prints '39 of 50 states identical' for 2016) and copied into a comment. It becomes wrong when a year is added or a page is re-fetched.

**The fix proposed.** Point to the tool's printed 'N of 50 states identical' lines instead of quoting the figure. The 35 threshold stays as the DECLARED premise.

**The skeptic's evidence.** I could not refute it. The figure is a measured count copied into a source comment, which the claim convention forbids.

1. The comment is new in this change. Tools/us_returns_prep.pl, staged, lines 68-70: "fewer than 35 of the 50 states - every year read differs in at most eleven, where the FEC's notes name same-day elections for unexpired terms counted twice and Louisiana's December runoffs added". `git show 43169a7a:Tools/us_returns_prep.pl | grep -c eleven` returns 0, so this commit added it.

2. "Eleven" is today's measured maximum, not a premise. The code holds only `unless $same >= 35` (line 876) and prints `50 - @diff` "of 50 states identical" (line 1011). I ran a control copy of the inputs in the scratchpad (no repo writes). It printed 2016: 39 of 50 identical (11 differ: HI KY LA MN NH NY OR PA RI SC UT), 2018: 42, 2020: 43, 2022: 45. The largest gap is 11, which is the comment's number exactly. The figure is true today, but it was transcribed from a run.

3. The rule covers this case. CLAUDE.md, the claim convention: DERIVED includes "a count, a figure"; "Nobody transcribes, anywhere, in any file." docs/archive/CLAIM_CONVENTION_AND_DISCIPLINE.md: "This binds SOURCE COMMENTS exactly as it binds markdown ... A true transcription and a false one fail identically the day the environment moves." The same change already words its inline comment the compliant way (lines 873-874: "every year read differs in a few"). So the header is the one place out of step.

4. Nothing in the bar would catch it going stale. CommentClaimCheck only checks backticked `Type.Member` references in Assets/**/*.cs, not counts in a .pl header.

5. How it goes wrong: add a year to the FEC cross-check list (lines 851-852), re-fetch a revised FEC workbook (with new SHA256SUMS), or widen the comparison. A year that differs in 12-15 states still passes the 35 threshold, and the header's "at most eleven" becomes false with no failure anywhere. "Every year read" is also loose today: 2024 is read but has no FEC table to compare.

6. Severity is note. It is a comment only, accurate today, and has no runtime effect. The s786 review handled the same class ("the comments named the catalog's years") as a note.

Aside (same sentence): "where the FEC's notes name ..." reads as if the notes explain every difference. Reading 13 of president_returns.md says the other differences "are explained by no saved page" (for example OR 2016, NC and WI 2018, CA 2022).

**The skeptic's corrected fix.** Replace the measured count with a pointer, and keep 35 as the stated threshold. For example, lines 68-70: "fewer than 35 of the 50 states (the threshold) - the publications differ in a few states each year read, and the run prints each one as 'N of 50 states identical' with the FEC less the Clerk; the FEC's notes explain some (same-day elections for unexpired terms counted twice, Louisiana's December runoffs added), and no saved page explains the rest; a column misread makes almost every state differ;". This matches the inline comment at lines 873-874 and also stops implying that the notes explain every difference. Comment-only edit: no output bytes change.

## The first pass - refuted by the skeptics

- [mutations] L508 can never fire alone, so h07's second expected text adds no independent proof - *The algebra holds, but it is not a defect. Line numbers are the same in the staged and working-tree Tools/us_returns_prep.pl.

What I checked:
- L489: `for my $k (@cand) { my $v = count(...); $all += $v; $csum{$k} += $v; }`. Every row that passes L484 adds to csum, duplicate rows included.
- L490: `... unless $all == $t`.
- L492 and L497-498: %sum runs over keys %{$y{2024}{st}}, which holds the last row per state. L498 tests `$sum{total} == $tot{total}`.
- L480 sets `$ctot{$_} = count(...) for @cand`, from the same last Total row as %tot.
- L503: `unless defined $ctot{$k} && ($csum{$k} // 0) == $ctot{$k}`.
- L505 and L508: `$call += $ctot{$k} // 0` ... `unless %tot && $call == $tot{total}`.

Proof that L508 follows from the other four:
- If L486 is silent, there are no duplicate states.
- Then call = sum over k of ctot(k) = sum over k of csum(k) (by L503) = sum over states of all(state) = sum over states of t(state) (by L490) = sum{total} = tot{total} (by L498).
- L486 is a real premise. A duplicate row with zero Trump and Harris votes, which the Total row counts in its candidate cells but not in TOTAL VOTES, fires only L486 and L508.
- I reproduced h07 on a scratch copy of the staged tool. It printed exactly three mismatches: L503 ('STEIN' 862049 vs 862050), L508 (155238303 vs TOTAL VOTES 155238302), and returns_2024.md 'STEIN typed 862049'.

Why this is not a problem:
1. h07's second expected text is the only thing in the suite that shows L508 fires and prints the right sum. The standing rule is "every new failure path proved by the mutation script", and h07 meets it by requiring this text. Without the text, a broken or deleted L508 would still pass h07. The rule asks for no independent firing, and no realistic input can give it for a deliberate sum-identity cross-check (L499's comment: "the columns together to the total").
2. "Never" is false in Perl's arithmetic. count() (L260-266) accepts a `\.0+` tail, so '9007199254740992.0' becomes a floating-point number. The L489 row sum then rounds 2^53+1 to 2^53, while the Total cells stay exact integers.
   - I ran a replica of L486-L508's arithmetic with count() copied verbatim.
   - State A: c1='9007199254740992.0', c2='1', TOTAL='9007199254740992'. State B: c1='0', c2='1', TOTAL='1'.
   - Total row: c1='9007199254740992', c2='2', TOTAL VOTES='9007199254740993'.
   - Only L508 fired (call 2^53+2 vs 2^53+1); L486, L490, L498 and L503 stayed silent.
   - These are absurd magnitudes for vote counts, so on real data L508 is redundant. It is still not a strict corollary, and nothing anywhere misstates it.*
- [mutations] h09's expected text names no file - *The finding is right that h09's expected text names no file. Its failure scenario cannot happen, though: the mutation can only land on 2022's page.

1. The message carries the file. Tools/us_returns_prep.pl:810 is `if (@hit != 1) { problem("$file: the House recapitulation's title on " . scalar(@hit) . " page(s), not one"); next; }`, where `$file` is "returns/clerk_statistics$year.pdf" (line 803).

2. The mutation pins its own landing. h09 (Tools/us_returns_mutations.sh:110) is `BEGIN { undef $/ } s{(Recapitulation of Votes Cast for United States Representatives, Election of November 8, 2022)}{$1\n\f$1}`. It is a literal with no newline and no /g, so each pdftotext call can make at most one substitution, and only in a text that contains the full literal. I ran `pdftotext -raw` over all 18 saved PDFs (the five Clerk books, the Nebraska books, the Maine certificates, the NARA, FEC and Court PDFs and the apportionment tables). The literal occurs exactly once in the whole set, in clerk_statistics2022.pdf. nebraska_sos_general_canvass_book_2024.pdf contains "November 8, 2022" but not the title. Each of the Clerk books for 2016, 2018, 2020 and 2024 has one title at the start of a line, with its own date. The pages are pinned by SHA256SUMS, so no other text can carry the literal unless a page is deliberately saved again.

3. I ran it. I copied fresh() and wrap() exactly into a scratch directory, applied h09 and ran the tool: exit 255, 0 files written, and the whole output was:
`MISMATCH: returns/clerk_statistics2022.pdf: the House recapitulation's title on 2 page(s), not one`
`MISMATCH: House 2022: the Clerk's and the FEC's tables agree exactly on 0 of 50 states, under the 35 a column read right gives`
`2 mismatch(es) - nothing written`
The only line containing the expected text names the 2022 file.

4. No path gives a false CAUGHT. If the literal fails to match, nothing changes and the tool exits 0, so the case is MISSED. If it matched in the middle of a line, the original page's title would no longer start a line, so @hit stays at 1 and the tool prints "0 header line(s) on the recapitulation's page" instead. That is also MISSED. A duplicate in another year's PDF needs another year's text to contain the 2022 title literal, and no saved page does.

5. The script already accepts expected texts without a file prefix. Its stated rule is "a mutation that lands anywhere else prints something else and is MISSED", which holds for h09 because its regex pins the site. t20 (line 131) expects "carries no page breaks" without the "$cert:" prefix of line 656, even though that mutation reaches every PDF. r06 expects "not that year's certificate" and t15 expects "is a formula, with no typed national figure", likewise without a file.

The only remaining issue is self-documentation: the sibling h-cases name their file or year and h09 does not. That is a matter of consistency, not a gap in coverage.*
- [mutations] The new die paths have no case - *THE FACTS ARE RIGHT: no case reaches the five dies. Line numbers are the staged blob's (in the working tree, which is being edited, L880 has since moved to L882). L805 `open my $p, '-|', $pdftotext, '-raw', "$raw/$file", '-' or die "$pdftotext: $!\n";`, L807 `close $p or die "$pdftotext $file: exit " . ($? >> 8) . "\n";`, L880 `open my $h, '<', $md or die "$md: $!\n";`, L480 `$ctot{$_} = count($c->{$r}{$_}, "$file Total col $_") for @cand;`, L861 `my @v = map { count($c->{$r}{$_}, "$file $a") } 5 .. 7;`. Cases h01-h09 only substitute text (the wrap exits 0), poke numbers (h06, h07) or edit the md (h08). No case expects 'is not a count', ': exit' or 'No such file'.

BUT THE RULE DOES NOT OWE THESE CASES, AND NOTHING CAN FAIL THROUGH THE GAP.
(1) The convention. L805 and L807 are byte-identical to L546 and L548 (the same pattern is at L649 and L651). L880 is the same kind of die as L775 and L612. count() has one die (L265), called from 21 lines at 43169a7a. None of these was ever covered by a case, yet s786 recorded 'The failure paths, proved ... twenty-nine of twenty-nine' on that basis. The suite proves mismatch classes. 'An input it cannot read' is proved once as a class, by r06 and r19. These lines add no new failure class.
(2) Every cited die is backed up by a check that a new case already proves. Probe on a scratch copy with the staged tool:
- Reaching each die: every run stopped with a non-zero exit and wrote nothing. L480 (poke x862049) gave "Total col 21: 'x862049' is not a count". L861 (poke n/a) gave "fec_federalelections2022.xlsx AL: 'n/a' is not a count". L807 gave ".../pdft.sh returns/clerk_statistics2016.pdf: exit 3". L805 (tool mutated) gave '/mingw64/bin/pdftotext: No such file or directory'. L880 (md removed) gave 'returns_2024.md: No such file or directory'.
- Removing each die and feeding the same input: every run still exited non-zero with 0 files written.
  - L480 printed "candidate column 'STEIN' sums to 862049 over the states, its Total-row figure is 0" (h07's check) plus a perl-warning mismatch.
  - L861 printed "'6. Table 5 House by Party': the rows' column 6 sums to 53355812, the Total row holds 54298205" (h06's check).
  - L807 printed 'title on 0 page(s), not one' for all five years (h09's check) and 'agree exactly on 0 of 50 states' (h04's).
  - L880 printed a readline-on-closed-filehandle warning, "no 'National result - House' section" and "the House's Republican typed nowhere" (h05's and h08's check).
(3) The claim that L807 needs its own wrapper is wrong. pdft.sh exits with perl's status, so the existing wrap() reaches it: wrap 'exit 3 if /^Recapitulation of Votes Cast for United States Representatives/'.
(4) The finding itself is graded note, with an optional fix.

SIDE NOTE: another agent's scratch script, one_case.sh (T="$1/copy"), probably shared $SCRATCH/copy while I was probing. Two of my runs saw passing ENOENTs on Clerk PDFs, and my rm -rf of $SCRATCH/copy may have disturbed that agent's runs the same way. A flaky ENOENT in their results from this window comes from that clash, not from the tool. Nothing in the repository was written.*
- [records] §788 is cited throughout, but COMPLETED.md has no §788 in the staged tree - *Facts confirmed but no defect: this is how every change looks at review time.
- Confirmed: COMPLETED.md in the index and the working tree ends at "## 787." (line 37497). §788/s788 is cited at: president_returns.md:3 and :72; returns_2024.md:91; USA_STAGE_PLAN.md:211 ("Design, DECLARED before the build (§788)"); us_returns_prep.pl:3, :9, :959; us_returns_mutations.sh:2, :101; elec_report.pl:2; GeneratedCatalogCheck.cs:517, :575; UsPresidentialReturns.cs:11.
- The finding's list is slightly off. out_of_tree.txt does not cite §788 (header: "PS-6 US-3 and US-5"). The register rows do not cite it either; line 72's "Kept out of tree" paragraph does.
- The record is written after the review on purpose. CLAUDE.md:67: "RECORDS: ONE COMMIT PER PASS ... a § record is what changed · the evidence line · the commit". The §786 and §787 records each contain "**The review** (... polisim-staged-review ...)" and a "**Bars**" line. §787's says the documents bar ran "on the tree after the review's fixes, before the record".
- Every commit lands with its section. Of the last 400 commits whose subject starts with sN/§N, every one has "## N." in COMPLETED.md at that commit (none missing). s786 and s787 were in this same state when reviewed (s786 review:1198 "no §786 exists and COMPLETED.md is not staged").
- The §788 section is already drafted in the session scratchpad: rec788.md, "## 788. PS-6 US-5, PART ONE: ... US-5'S DESIGN DECLARED BEFORE ITS CODE". It covers every staged pointer (the House series, 2024's candidates, the declared design, elec_report.pl, the GeneratedCatalogCheck change) and has no review or bars block yet. rec789.md is "PART TWO". No branch or stash has "## 788.".
- The same finding was refuted for §772, §780, §783, §786 and §787 (Reviews/2026-10-06_s787_us4_state_swing.md:840; Reviews/2026-10-05_s786_us3_presidential_returns.md:1086, :1198).
- Remaining risk: only the process guards this. There are no hooks in .git/hooks, and no check compares § pointers with COMPLETED.md headings (DocumentClaimCheck.cs:84 and ResidueCheck.cs:27 only exempt the file). Committing without the record would leave dangling pointers, but nothing in the staged change causes that, and comments and document text cannot fail at runtime.*
- [catalog] House completeness is counted by rows, so a missing year or a duplicated state passes - *The reading is right: in the staged GeneratedCatalogCheck.cs (git show :), lines 593-596 count rows for each year present and fail only on zero years. But the failing catalog cannot be reached except by a deliberate forge of three files.

(1) The only writer cannot produce it. Tools/us_returns_prep.pl writes House rows only as `for my $year (@house_years) { for my $s (@house_states) {...} }`, for the CSV (line 944) and the catalog (line 988). @house_years is typed (2016, 2018, 2020, 2022, 2024; line 799). @house_states is `grep { $_ ne 'DC' } @order` (line 800), and @order maps the sorted keys of the typed 51-entry %code hash (lines 271-280), so the 50 codes are distinct by construction.

(2) The generator stops on a gap or a duplicate. Line 833 fails on 'a second row' and line 841 on 'no row for'. Dropping a year from 2016-2022 makes the FEC cross-check (lines 875-876) find 0 of 50 matching states, under its 35 minimum. Dropping 2024 makes `$house_tot{2024}` undefined, so the returns_2024.md check fails (lines 889-892). Any of these stops the run at line 909, `die "$n mismatch(es) - nothing written"`, before write_all.

(3) Editing one file alone is caught. A CSV edit fails ReadUsCsv's digest check against HouseSourceDigest. A catalog edit fails line 585 (`house.Count != UsPresidentialReturns.House.Length`) or line 589 (SameRow, row for row).

(4) The reviewer's proof needed the CSV, the catalog and HouseSourceDigest all rewritten to match each other by hand. Under that kind of rewrite, any House figure can be changed and still pass (for example AL's R votes and total both +1000), both in the staged check and with the proposed fix. The catalog carries no national House total to check the rows against, so the fix does not close that case either.

(5) The class summary states its scope (lines 16-21): 'The one failure mode a generated catalog has: the source moved and the catalog did not ... exactly one risk: drift. This check is that risk's guard.' It also says it is deliberately not a second implementation of the generator (lines 28-31). The presidential block in the same function names no expected years either.

(6) The staged data are complete: house_by_state.csv has 2016-2024 at 50 rows each, 0 duplicates, 50 distinct codes; the catalog has 250 matching rows.

What remains is wording. Line 524's 'the House holds 50 states a year' says more than the check holds, which is a row count. Its own messages are accurate: line 595 says 'state rows, not 50' and the summary says '(50 a year, N years)'.

The working tree now holds an unstaged rewrite along the reviewer's lines (set-equality plus year gaps); I graded the staged version.*
- [catalog] The per-row parts-sum always holds on the generator's output - *The fact the finding states is right, but the defect it builds on that fact does not hold. Line numbers are from the staged copies (`git show :path`).

1. The identity is by construction. Tool :839 is `$house{$year}{$st} = { R => $r, D => $d, other => $t - $r - $d, total => $t };`. Both the CSV (:944) and the catalog (:988) are written from that same value, so GeneratedCatalogCheck.cs:590 holds on every row the current tool writes. I confirmed this on all 250 rows of house_by_state.csv and all 250 catalog rows.

2. That is true of every assertion in this block, not just :590. The block's docstring calls it "the drift questions":
   - SameRow at :589 compares a CSV row with a catalog row, and both come from the same `$e`.
   - The 50-a-year check at :595 is already guaranteed by tool :841 (`no row for $_`) and :833 (`a second row`).
   - The candidate sums at :600-619 are already guaranteed by tool :490, :498, :503 and :508.
   - Tool :909 (`die "$n mismatch(es) - nothing written"`) runs before `write_all` at :991.
   So none of these can fail on what the current generator writes. That is the normal state of a drift check, not a dead line.

3. "It fails only on a coordinated hand edit of the CSV, the catalog and the digest" is incomplete. :590 is the only guard for the identity the instrument reads if a future edit changes how :839 derives `other`, because no tool-side House check reads `$e->{other}`:
   - :836 and :843 check the raw cells.
   - The FEC cross-check compares R and D only.
   - The returns_2024.md hold checks the Total row's R, D and total.
   - The mutation script's control compares a regenerated temporary tree against the regenerated repo, so it would not notice either.

4. The docstring at :523-524 lists exactly what the code checks. It does not cite this check as evidence that the Clerk's columns were read right. The instrument does use the identity: USA_STAGE_PLAN US-5 says "Loyalty. Each party's share of all votes cast".

5. The tool already handles the misreadings the finding names (R and D crossed, a column read into the wrong field) before anything is written:
   - :836 holds each row's eight read parts to its read Total. Mutation h01 proves it ("AL: its columns sum to 1659896, its Total is 1659895").
   - The 35-of-50 FEC cross-check catches a crossed header for 2016-2022 (h04).
   - returns_2024.md held to the Clerk's Total row catches it for 2024 (h05, h08).

6. The finding's own fix does not fix what it names. Carrying the other cells as read gives the same bytes, because :836 forces the six other cells to sum to T - R - D on every row that gets written. :590 would stay just as unable to fail on generator output.*
- [catalog] A generated doc line with quotes survives comment stripping, and its word 'None' matches two public static methods - *TRUE PARTS OF THE FINDING:
- SourceText.cs:72 `if (slash >= 0 && line.IndexOf('"') < 0) { sb.Append(line, 0, slash)... } else { sb.Append(line)... }` keeps the whole of index line 299 of UsPresidentialReturns.cs (`/// Ticket: "R" or "D" ... "-" ... (None of These Candidates ...)`).
- It is the only comment line with a quote in that file. The 43169a7a version has none.
- `None` matches two public statics: PolicyDecision.cs:238 and GovernmentFormation.cs:61. "None" is not on the TooCommon list (UnwiredSubsystemCheck.cs:95-99).

WHY THE FAILING PATH DOES NOT EXIST:
- A file counts as called when any one of its public static names appears as a word in any other non-harness file under Assets/Scripts (UnwiredSubsystemCheck.cs:163-170, CountWord).
- After comment stripping, `None` already appears in the code of 24 other game files besides the two declaring files and the generated one. Examples: enum members `SpendingDriver.None` (SpendingDrivers.cs:16,74), `CampaignStrategy.None` (CampaignStrategy.cs:43), `PlayerRole.None` (GovernmentRecord.cs:11,239), `GdpStage.None` (ReleaseCalendar.cs:80,101), `MandateForm.None` (AiEnergyMinistry.cs:42), `TypeNameHandling.None` (SaveGameService.cs:77), `Selectable.Transition.None` (CountrySelectorScreen.cs:560); static properties `IssueMeasurement.None` and `AiReaction.None` (CampaignAi.cs:103,439); and real `PolicyDecision.None()` calls (PolicyImpactLedger.cs:203, ShadowBaseline.cs:66,117, SimulationManager.cs:4838,4854, and others).
- So a file whose only entry point is `None` would count as called with or without line 299. The comment never decides the result.

EMULATION:
- I wrote a perl replica of WithoutComments plus both passes of the check.
- It agrees with the real check: it reports the same GAP files as the newest bar log, PoliSim-captures/logs/cheap702b.log (Rosatellum and TacticalVoting UNWIRED; ElectoralCollege and Rosatellum UNREACHABLE).
- It is responsive: renaming PolicyDecision.None to a unique name makes it report UNWIRED 3.
- Base file, staged file, and staged file with line 299 made quote-free (the proposed fix) all give UNWIRED 2/180 and UNREACHABLE 2/287.
- The finding's own hypothetical: I removed every game call to PolicyDecision.None() (in 6 files) and applied the fix. `None` is still named in 23 game files, PolicyDecision.cs still counts as wired, and UNWIRED stays at 2 with or without the fix.

OTHER CHECKS THAT USE THE STRIPPER:
- Line 299 contains none of the tokens that NumberLocaleCheck, SharedMidpointCheck, PartyInkDrawSiteCheck, ExitDiagnostics, ChamberVerdictCacheCheck, SettingsCheck, ReviewLedgerCheck or OfficeTestDiagnostic look for.
- DeadStateCheck: only `Candidates` (private method at CampaignAi.cs:539, 11 occurrences) and `D` (257 occurrences) collide with private names. Both also appear in the generated file's own code, so the outcome is unchanged.
- DocumentClaimCheck reads only root-level .md files, and no document names `UsPresidentialReturns.None`.

NOT A NEW CLASS:
- The index has 2970 comment-only lines containing a double quote across 254 game files; 43169a7a had 2969 across 253. Every `/// <see cref="X"/>` line already survives the stripper.
- SourceText.cs:35-39 and CommentImmunityCheck.cs:193-195 state this limit: the stripper is an approximation and needs a lexer to close. Line 299 is one more line of a known kind, and it changes nothing today or in the stated hypothetical.*
- [catalog] CSV headings are skipped unread, and the House CSV lists R before D while the presidential CSVs list D before R - *The finding's facts are right, but nothing in the staged change fails. Its scenario needs a later mistaken edit to the generator.

1. Today the heading, the data, the tuple and the comparison all agree. us_returns_prep.pl:943 writes the heading `year,state,votes_r,votes_d,votes_other,votes_total`. Line 944, the next line, writes the rows in the same order: `join(',', $year, $s, $e->{R}, $e->{D}, $e->{other}, $e->{total})`. The tuple at :987 is `(int Year, string State, long VotesR, long VotesD, long VotesOther, long VotesTotal)`, and :988 writes `$e->{R}L, $e->{D}L`. GeneratedCatalogCheck.cs:589 compares by name: `SameRow(house[i], h.Year, h.State, h.VotesR, h.VotesD, ...)`. The finding admits this ("Today they agree").

2. A hand edit to the CSV's heading is caught. The heading is inside the bytes that are hashed. The check compares that hash with `HouseSourceDigest` at :657-658, and the tool computes that digest from the whole `$csv_h`, heading included (:950). The heading can only go wrong if someone edits the generator's heading text at :943 and leaves the join on the very next line alone. That is a possible future bug, not a defect in this change.

3. The risk that matters is the R and D values being swapped, and the tool already catches that. It reads the Clerk's columns by name: `($v[$ix{Republican}], $v[$ix{Democratic}], ...)`. It checks that FEC columns E-G are headed Democratic, Republican and Other. It requires the Clerk and the FEC to agree exactly on at least 35 of 50 states: `problem(... "agree exactly on $same of 50 states" ...) unless $same >= 35`. It holds 2024's Total row to the Republican and Democratic figures typed in returns_2024.md. Mutation cases h04 (2020 header crossed: "agree exactly on 0 of 50 states") and h05 (2024 header crossed: "the House's Republican typed 74390864, the Clerk's Total row 70571330") prove both paths.

4. Nothing reads house_by_state.csv by its heading. Grep finds it read only by ReadUsCsv. The only user of the House figures reads the catalog by field name: UsNationalVoteCheck.cs:445 `r += h.VotesR; d += h.VotesD;`.

5. Skipping the heading is not new in this change. `if (heading) { heading = false; continue; }` is US-3 code (HEAD GeneratedCatalogCheck.cs:616); this change only adds two calls to it.

6. Putting R before D in the House file while the presidential files put D first is a style difference, not an error. Every heading names its own columns correctly. R first matches the tool's printout ("two-party R %.4f%%"), the plan's scoring ("REP's predicted share less the record's"), and the Clerk's own order in 2016, 2022 and 2024 (president_returns.md reading 13).

Context: the unstaged working tree already has the suggested hardening. There, ReadUsCsv takes `string heading`, derives the field count from it, and reports a mismatch with `if (line != heading) { wrong.Add(file + ": its heading '" ...` (line 686).*

## What was done about the first pass

One defect survived the skeptics, reported by four lenses (1, 5, 14, 21); 14 minor, 7 notes - every finding acted on, and three the skeptics refuted acted on too.

- **The defect - h01 (1, 5, 14, 21).** The extractor's lines end CRLF, so h01's `$` never sat before its figure and the case never landed; the working tree's `(\r?)$` form is staged, and the suite was run again on the staged tree.
- **The failure paths without a case (3, 6-10).** Nineteen cases added (h10-h28), each landing where its label says and printing its exact mismatch: a state's figure and its own Total raised together (the column check), the Total row twice and gone, a state twice, a row naming no state, the header short of a column and twice, a District of Columbia row; the FEC table's title, heading, a Total-row formula, a state twice, its Total row unlabelled; a formula in a 2024 candidate's Total cell, a label with a comma; `returns_2024.md`'s Stein off by one vote and gone, each section renamed. h09's expected text names its file. The suite: fifty-seven caught of fifty-seven, the control reproducing the repository's bytes.
- **The wrap cases' labelling (11).** The script's header says an input case reaches a PDF's text by rewriting the copy's extractor output, said in its section.
- **The District of Columbia (2).** The row's name takes "of" and dotted capitals, so the row is named and refused (h28).
- **The label guard (4, 23).** Refuses every control character, beside the comma, the quote and the backslash.
- **The publications' differences (12).** Each verified on the saved pages before it was written: Maine 2018 and 2022 (the FEC's first rounds against the Clerk's final ones - the FEC's own footnotes give them, and the Clerk's 2022 footnote says "from round 2"); North Carolina's 9th in 2018 (blank by the Clerk's "ordered a new election", carried by the FEC with its "not certified" note); Louisiana 2020 (exactly the November vote of the 5th district's two Republicans the Clerk's footnote gives) and 2016 (the FEC's note); Oregon's 3rd 2016, Wisconsin's 4th 2018 and California's 2nd 2022 (the same candidate under different parties, or the figures given to the other candidate). Reading 13 names them; "no saved page has been read" for the rest, each small.
- **The House basis (13).** The design and reading 13 declare the Clerk's own conventions - each race's final count: Louisiana's December runoff in place of November, Maine's last round, no North Carolina 9th in 2018 - and drop the 403 as a reason (the item named the Clerk for 2024 anyway); the FEC comparison says what its notes say ("the votes for both terms are included"). The departure from the item's text is put to Elias as a reading.
- **The read-back (15).** "Typed before these pages were saved, and read back against them on 2026-10-05", in the record and the tool.
- **Maine's ranked-choice lines and the loyalty's denominator (16).** Described as counting some ballots again (2022's 2nd district carries an "Exhausted Ballot" line larger than its first-round vote); the design DECLARES the House history's loyalty on shares of the Clerk's Total, inert for the two-party split.
- **The identity's wording (17).** Exact for a held fit, which is solved; within the grid's residual for the free fit - as the instrument checks it.
- **Transcribed figures (18, 24, 25).** "At most eleven" gone (the run prints the states); the weight's figure replaced by `VoteShareBacktest`'s; the first government of record and the pre-start record's first row named, not dated; "five CSVs" now "its CSVs".
- **The midterm volumes' titles (19).** The design, the tool and the generated CSV's header name the Clerk's election statistics, *of the Congressional Election* in a midterm.
- **The plan's sources table (20).** US-5's rows say what is saved and why.
- **The unused local (22).** Removed.
- **Refuted, done anyway:** each House year now held to the presidential catalog's 50 states once and no House election missing between the first and the last; every CSV's heading read against the comparison's own; the generated doc line written without a quote.
