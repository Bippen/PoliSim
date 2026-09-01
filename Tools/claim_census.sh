#!/usr/bin/env bash
# claim_census.sh - the coupling census for PoliSim's LIVE documents.
#
# NOT A CHECK. It never fails, never gates a bar, and R-N5 does not apply to it:
# it reports, it does not judge. Re-run it and the figures in any report that
# cites it are re-derived rather than transcribed.
#
# LIVE = every root *.md except the three HISTORICAL records, which is the same
# ruling DocumentClaimCheck.cs makes (see its `Historical` set). History naming a
# deleted member is history working correctly.
#
# UNIT: the claim-line - a non-blank line outside a fenced code block that is not
# a pure rule/separator. A line may carry claims of more than one class, so the
# three class counts are NOT a partition and deliberately sum to more than the
# total; the overlap is reported.
#
# usage: bash Tools/claim_census.sh [--per-marker]
set -u
cd "$(dirname "$0")/.."

PER_MARKER=0
[ "${1:-}" = "--per-marker" ] && PER_MARKER=1

printf '%-38s %7s %7s %7s %7s %7s\n' DOCUMENT LINES DERIVED TRACKING INSTR "D-ONLY"
printf '%.0s-' $(seq 1 76); printf '\n'

for f in *.md; do
  case "$f" in
    COMPLETED.md|CLAUDE.md|ELECTIONS_PROTOTYPE_LOG.md) continue ;;
  esac
  awk -v FNAME="$f" -v PM="$PER_MARKER" '
    /^```/ { fence = !fence; next }
    fence { next }
    /^[[:space:]]*$/ { next }
    /^[[:space:]]*[-=|:[:space:]]+$/ { next }
    {
      total++
      d = 0; t = 0; i = 0

      # ---- DERIVED: a fact about the code, stale the moment the code moves ----
      # D1 a backticked member or call: `Type.Member` / `Thing()`
      if ($0 ~ /`[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*`/ ||
          $0 ~ /`[A-Za-z_][A-Za-z0-9_]*\(\)`/)                     { d = 1; m_id++ }
      # D2 a file path carrying a code-ish extension
      if ($0 ~ /[A-Za-z0-9_\/.-]+\.(cs|ps1|csv|json|png|svg|html|asset|unity|prefab)/) { d = 1; m_path++ }
      # D3 a git commit hash (7-40 hex, not a date, not a pure decimal)
      if ($0 ~ /(^|[^0-9A-Za-z])[0-9a-f]{7,40}([^0-9A-Za-z]|$)/ &&
          $0 ~ /(^|[^0-9A-Za-z])[0-9a-f]*[a-f][0-9a-f]*[0-9][0-9a-f]*([^0-9A-Za-z]|$)/) { d = 1; m_hash++ }
      # D4 a pointer INTO another document or file by position
      if ($0 ~ /(line|lines|§§|§)[[:space:]]*[0-9]/)                { d = 1; m_pos++ }
      # D5 a transcribed COUNT or MEASURED figure
      if ($0 ~ /(^|[^-0-9A-Za-z])[0-9]+([.,][0-9]+)?(%|px|M|k|ms|s\b)?([[:space:]]+(of|parties|checks|rows|entries|constituencies|seats|sections|categories|bands|fields|items|findings|textures|files|marks|axes|levers|scalars|countries|screens|sweeps|steps|instances|runs|call sites|call-sites))/ ||
          $0 ~ /(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|twenty|twenty-five|forty-five|fifty-three|TWENTY-ONE)[[:space:]]+(of|parties|checks|rows|entries|constituencies|seats|sections|categories|bands|fields|items|findings|marks|axes|levers|scalars|countries|screens|sweeps|steps|instances|runs|pieces|things)/) { d = 1; m_count++ }

      # ---- TRACKING: what is open, whose it is, what it unblocks ----
      if ($0 ~ /(^|[^A-Za-z0-9])[A-Z]{1,2}-[A-Z]?[0-9]+(\.[0-9]+)?([^A-Za-z0-9]|$)/) { t = 1; m_itemid++ }
      if ($0 ~ /(OPEN|CLOSED|BLOCKED|TAKEN|STOPPED|COMPLETE|DONE|RETIRED|SUPERSEDED|waits on|blocks|unblocks|blocker|Elias.s|Design.s|✅|\[ \]|\[x\])/) { t = 1; m_status++ }

      # ---- INSTRUCTION: what to do, what is ruled, what is owed ----
      if ($0 ~ /(must|never|always|may not|should|required|RULED|ruled|the rule is|Do not|do not|no exceptions|stands\b|binding|convention|shall)/) { i = 1; m_instr++ }

      if (d) derived++
      if (t) tracking++
      if (i) instr++
      if (d && !t && !i) donly++
      if (d && (t || i)) overlap++
    }
    END {
      printf "%-38s %7d %7d %7d %7d %7d\n", FNAME, total, derived, tracking, instr, donly
      if (PM) {
        printf "%-38s   markers: id=%d path=%d hash=%d pos=%d count=%d | itemid=%d status=%d | instr=%d\n", \
               "", m_id, m_path, m_hash, m_pos, m_count, m_itemid, m_status, m_instr
      }
      # emit machine-readable totals for the grand sum
      printf "TOTALS\t%d\t%d\t%d\t%d\t%d\n", total, derived, tracking, instr, donly > "/dev/stderr"
    }
  ' "$f"
done 2> /tmp/claim_census_totals.tsv

printf '%.0s-' $(seq 1 76); printf '\n'
awk -F'\t' '{L+=$2; D+=$3; T+=$4; I+=$5; O+=$6}
  END { printf "%-38s %7d %7d %7d %7d %7d\n", "ALL LIVE DOCUMENTS", L, D, T, I, O }' /tmp/claim_census_totals.tsv
