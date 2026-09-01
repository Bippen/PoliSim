#!/usr/bin/env bash
# migrate_docs_batch1.sh - retire nine FINISHED documents into COMPLETED.md, verbatim.
#
# One self-contained operation (P1). Each file is appended under its own numbered section with every
# heading demoted by exactly one '#' so a retired document cannot open a top-level section of the
# record; no other byte of its content is changed. Then the file is removed and every LIVE reference
# to it is repointed at the section. Historical documents (CLAUDE.md, COMPLETED.md) keep their
# references untouched - a citation in a log is history working correctly.
set -euo pipefail
cd "$(dirname "$0")/.."

DATE=2026-09-02
declare -a FILES=(
  "182|LAW_BROWSER_BOARD_RULINGS.md"
  "183|ELECTIONS_ARCHITECTURE.md"
  "184|ELECTIONS_GAP_TABLE.md"
  "185|ELECTIONS_PROTOTYPE_LOG.md"
  "186|POLISIM_UI_V3_DIRECTION.md"
  "187|POLISIM_V2_SCREEN_SPEC.md"
  "188|POLISIM_SEED_DATA_MACRO_OVERHAUL.md"
  "189|ELECTIONS_PLAY_CALIBRATION.md"
  "190|POLISIM_REVIEW_2026-09-01.md"
)

# Live documents and source files whose references get repointed. Everything else at root is a
# record and is left alone by design.
LIVE_DOCS="POLISIM_FEATURE_LIST.md POLISIM_BACKLOG.md MISSING_PREREQUISITES.md ERRANDS.md CLAUDE_DESIGN_ASSET_REQUEST.md POLISIM_COHORT_SPECLET.md POLISIM_TAX_SPECLET.md ELECTIONS_CAMPAIGN_SPEC.md SEND_PACKAGE.md CLAUDE_DESIGN_BOARD_1I_NOTE.md"

for entry in "${FILES[@]}"; do
  n="${entry%%|*}"; f="${entry#*|}"
  [ -f "$f" ] || { echo "MISSING: $f"; exit 1; }
  lines=$(wc -l < "$f")
  {
    printf '\n\n## %s. `%s` — RETIRED %s, migrated verbatim (%s lines)\n\n' "$n" "$f" "$DATE" "$lines"
    printf '⚠ **Mechanical migration.** Every heading below is demoted by exactly one `#` so this document cannot open a top-level section of the record; no other byte of its content was changed. Anything it recorded as OPEN is carried by `POLISIM_FEATURE_LIST.md` or its appendix; anything it recorded as a ruling or a finding stands here as the record.\n\n---\n\n'
    sed -e 's/^#/##/' "$f"
  } >> COMPLETED.md
  git rm -q "$f"
  # repoint live references: `NAME.md` -> `COMPLETED.md` §N
  for d in $LIVE_DOCS; do
    [ -f "$d" ] && sed -i "s/\`$f\`/\`COMPLETED.md\` §$n/g; s/\b$f\b/COMPLETED.md §$n/g" "$d"
  done
  # repoint source COMMENTS that cite it (the citations are provenance and stay meaningful)
  grep -rl --include=*.cs "$f" Assets/ | while read -r cs; do
    sed -i "s/$f/COMPLETED.md §$n/g" "$cs"
  done
  echo "migrated $f -> COMPLETED.md §$n"
done

# The two checks held the prototype log in a NAMED SET as a historical document. The file is gone, so the
# entry goes with it (S-36): a set naming a file that does not exist is a claim about nothing.
perl -0pi -e 's/\s*\("COMPLETED\.md §185",[^\)]*\),\n//s' Assets/Editor/PreWiringPremiseCheck.cs
sed -i 's/"COMPLETED.md", "CLAUDE.md", "COMPLETED.md §185",/"COMPLETED.md", "CLAUDE.md",/' Assets/Editor/DocumentClaimCheck.cs

# The record's own deletion note.
printf '\nRetired %s under this rule, each migrated verbatim into its numbered section: §§182–190 — the law-browser board rulings, the elections architecture note, the gap table, the prototype log, the UI v3 direction, the v2 screen spec, the seed-data overhaul (its nine `[GAP]` figures now an appendix line, Elias'"'"'s), the play-calibration list, and the 2026-09-01 review.\n' "$DATE" >> COMPLETED.md

echo "--- leftover references to the retired names in LIVE docs and code (expect none) ---"
for entry in "${FILES[@]}"; do f="${entry#*|}"; grep -ln "$f" $LIVE_DOCS Assets/Editor/*.cs Assets/Scripts/*/*.cs 2>/dev/null || true; done
echo "done"
