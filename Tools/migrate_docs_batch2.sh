#!/usr/bin/env bash
# migrate_docs_batch2.sh - retire the two REGISTERS into COMPLETED.md, verbatim, and leave the open rows
# as a pointer index in the governing document.
#
# One self-contained operation (P1), the shape of migrate_docs_batch1.sh: each file is appended under its
# own numbered section with every heading demoted by exactly one '#'; no other byte changes. Then the file
# is removed, every LIVE reference is repointed at the section, and the one check that anchored on a row
# of the register loses that entry (S-36). Before the register goes, its OPEN rows (no ✅ in the row) are
# listed by id and hook into POLISIM_FEATURE_LIST.md's appendix - pointers into the record, never a
# transcription of their evidence (the claim convention). Historical documents (CLAUDE.md, COMPLETED.md)
# keep their references untouched.
set -euo pipefail
cd "$(dirname "$0")/.."
DATE=2026-09-02
declare -a FILES=(
  "197|POLISIM_BACKLOG.md"
  "198|MISSING_PREREQUISITES.md"
)
LIVE_DOCS="POLISIM_FEATURE_LIST.md ERRANDS.md CLAUDE_DESIGN_ASSET_REQUEST.md CLAUDE_DESIGN_BOARD_1I_NOTE.md SEND_PACKAGE.md POLISIM_COHORT_SPECLET.md POLISIM_TAX_SPECLET.md ELECTIONS_CAMPAIGN_SPEC.md"

# 1. The open-row index, derived from the register BEFORE it moves: every row whose id cell is a
#    register id and whose row carries no ✅. Hook = the row's next cell, bold stripped, cut at 140.
INDEX=$(mktemp)
{
  printf '\n## The register'"'"'s open rows — RETIRED INTO `COMPLETED.md` §197 on %s; this is the pointer index\n\n' "$DATE"
  printf '⚠ **Pointers, not a register.** Each line is an id and a hook; the evidence, the done-when, the owner and\n'
  printf 'the ruling live in the row itself, verbatim, in `COMPLETED.md` §197 (`grep -n "| **ID** |"`). A row is\n'
  printf 'listed here because it carried no ✅ when the register was retired — which includes shelves, triggers and\n'
  printf 'Design'"'"'s asks, not only startable work; the row says which. Move a row'"'"'s state by adding a line to\n'
  printf 'the record and striking it here. Derived mechanically by `Tools/migrate_docs_batch2.sh`; the shape is\n'
  printf 'the generated-block idiom without a digest, because the source no longer exists to digest.\n\n'
  awk -F'|' '/^\| \*?\*?[A-Z]+-[0-9A-Za-z.]+\*?\*? \|/ && !/✅/ {
      id=$2; gsub(/\*/,"",id); gsub(/^ +| +$/,"",id);
      hook=$3; gsub(/\*\*/,"",hook); gsub(/^ +| +$/,"",hook); gsub(/\|/,"/",hook);
      if (length(hook) > 140) hook = substr(hook, 1, 137) "…";
      printf "- `%s` — %s\n", id, hook }' POLISIM_BACKLOG.md
  printf '\n## The prerequisites document'"'"'s queues — RETIRED INTO `COMPLETED.md` §198 on %s\n\n' "$DATE"
  printf 'Its sections are queues by owner and they read the same in the record: **§S** the send (one paste, Elias'"'"'s),\n'
  printf '**§A** decisions waiting on Elias, **§D** waiting on another task, **§E** waiting on Claude Design, **§V** the\n'
  printf 'visual review (built, not seen), **§P** the playtest and its felt verdicts. `grep -n "^### [SADEVP]\\." COMPLETED.md`\n'
  printf 'past the §198 heading finds them. Nothing in them is startable by a session; every one is Elias'"'"'s or Design'"'"'s.\n'
} > "$INDEX"

# 2. Migrate verbatim, remove, repoint.
for entry in "${FILES[@]}"; do
  n="${entry%%|*}"; f="${entry#*|}"
  [ -f "$f" ] || { echo "MISSING: $f"; exit 1; }
  lines=$(wc -l < "$f")
  {
    printf '\n\n## %s. `%s` — RETIRED %s, migrated verbatim (%s lines)\n\n' "$n" "$f" "$DATE" "$lines"
    printf '⚠ **Mechanical migration.** Every heading below is demoted by exactly one `#` so this document cannot open a top-level section of the record; no other byte of its content is changed. Its open rows are indexed by pointer in `POLISIM_FEATURE_LIST.md`'"'"'s appendix.\n\n'
    sed -e 's/^#/##/' "$f"
  } >> COMPLETED.md
  git rm -q "$f"
  for d in $LIVE_DOCS; do
    [ -f "$d" ] && sed -i "s/\`$f\`/\`COMPLETED.md\` §$n/g; s/\b$f\b/COMPLETED.md §$n/g" "$d"
  done
  grep -rl --include=*.cs "$f" Assets/ | while read -r cs; do
    sed -i "s/$f/COMPLETED.md §$n/g" "$cs"
  done
  echo "migrated $f -> COMPLETED.md §$n"
done

# 3. The index lands at the foot of the appendix's CODE block's neighbour: appended after the
#    "Standing findings that own no item" section, i.e. before THE WORKING DISCIPLINE heading.
awk -v f="$INDEX" '/^# THE WORKING DISCIPLINE/ && !done {while ((getline line < f) > 0) print line; print ""; done=1} {print}' POLISIM_FEATURE_LIST.md > POLISIM_FEATURE_LIST.tmp && mv POLISIM_FEATURE_LIST.tmp POLISIM_FEATURE_LIST.md
rm -f "$INDEX"

# 4. The check that anchored on a row of the register: the row now lives in a historical record the check
#    skips, so the anchor is a claim about nothing (S-36) and goes.
perl -0pi -e 's/\n\s*\("COMPLETED\.md §197", "\| C-0\.2 \|", [^\n]*\),//s' Assets/Editor/PreWiringPremiseCheck.cs

# 5. The record's own deletion note.
printf '\nRetired %s under the same rule: §197 the register of findings and rulings (`POLISIM_BACKLOG.md`), §198 the prerequisites queues (`MISSING_PREREQUISITES.md`). The open rows are indexed by pointer in `POLISIM_FEATURE_LIST.md`; the governing document is now the only live list.\n' "$DATE" >> COMPLETED.md

echo "--- leftover references (expect none outside CLAUDE.md/COMPLETED.md) ---"
for entry in "${FILES[@]}"; do f="${entry#*|}"; grep -ln "$f" $LIVE_DOCS Assets/Editor/*.cs Assets/Scripts/*/*.cs 2>/dev/null || true; done
echo "done"
