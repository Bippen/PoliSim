#!/bin/bash
set -e
cd "$(dirname "$0")/.."
J=ElectionsData/sweden/scb_BefolkningNy_2024_municipality_age_sex.json
# 1. SCB code->name pairs (values and valueTexts are parallel arrays)
grep -o '"code":"Region","text":"region","values":\[[^]]*\]' /tmp/scb.json | grep -o '"[0-9]\{2,4\}"' | tr -d '"' > /tmp/codes.txt
grep -o '"valueTexts":\["Riket"[^]]*\]' /tmp/scb.json | sed 's/"valueTexts":\[//; s/\]$//' | tr ',' '\n' | tr -d '"' > /tmp/names.txt
paste -d'|' /tmp/codes.txt /tmp/names.txt | awk -F'|' 'length($1)==4' > /tmp/muni.txt
echo "municipalities in SCB list: $(wc -l < /tmp/muni.txt)"
# 2. the statute's split lists (from the riksdagen page, saved beside)
sed 's/<[^>]*>/\n/g' ElectionsData/sweden/riksdagen_vallag_2005_837.html | grep -vE "^\s*$" | sed -n '422,456p' > /tmp/vallag_list.txt
sed -i "s/ / /g" /tmp/vallag_list.txt
# 3. mapping: default = the county's valkrets; the seven split valkretsar by the statute's municipality lists
awk -v listfile=/tmp/vallag_list.txt '
function norm(s) { gsub(/^ +| +$/, "", s); return s }
BEGIN {
  while ((getline l < listfile) > 0) {
    if (match(l, /^ *([0-9]+)\. (.*)$/, m)) {
      n = m[1]; rest = m[2]; sub(/,$/, "", rest); sub(/, och$/, "", rest); sub(/\.$/, "", rest)
      vname[n] = rest
      if (match(rest, /\(([^)]*) kommuner\)/, p)) {
        lst = p[1]; gsub(/ och /, ", ", lst); c = split(lst, parts, ", ")
        for (i = 1; i <= c; i++) { nm = norm(parts[i]); listed[nm] = n }
        sub(/ \(.*\)$/, "", vname[n])
      }
    }
  }
  # county code -> statute valkrets number for the unsplit counties
  cv["03"]=3; cv["04"]=4; cv["05"]=5; cv["06"]=6; cv["07"]=7; cv["08"]=8; cv["09"]=9; cv["10"]=10; cv["13"]=15
  cv["17"]=21; cv["18"]=22; cv["19"]=23; cv["20"]=24; cv["21"]=25; cv["22"]=26; cv["23"]=27; cv["24"]=28; cv["25"]=29
}

{
  split($0, f, "|"); code = f[1]; name = f[2]; county = substr(code, 1, 2); v = ""
  if (code == "0180") v = 1
  else if (county == "01") v = 2
  else if (code == "1280") v = 11
  else if (code == "1480") v = 16
  else if (county == "12" || county == "14") {
    if (name in listed) v = listed[name]
    else if ((name "s") in listed) v = listed[name "s"]
    else { unmatched = unmatched " " code ":" name }
  }
  else if (county in cv) v = cv[county]
  else { unmatched = unmatched " " code ":" name }
  if (v != "") { print code "," name "," v "," vname[v]; count[v]++ }
}
END {
  for (v = 1; v <= 29; v++) printf "%d:%d ", v, count[v]; print ""
  if (unmatched != "") print "UNMATCHED:" unmatched
}' /tmp/muni.txt > /tmp/map_out.txt
tail -2 /tmp/map_out.txt
grep -v '^[0-9]*:[0-9]* |^UNMATCHED' /tmp/map_out.txt | sort > /tmp/map.csv
{ echo "# municipality -> Riksdag valkrets, 2024 municipal codes (SCB) mapped by Vallagen (2005:837) 4 kap. 2 § (Lag 2014:1384), the statute's own municipality lists for the seven split valkretsar, the county otherwise. Built 2026-09-02 by Tools/… see README."; echo "kommun_code,kommun_name,valkrets_no,valkrets_name"; cat /tmp/map.csv; } > ElectionsData/sweden/valkrets_municipalities_2024.csv
echo "mapped rows: $(wc -l < /tmp/map.csv)"
# 4. aggregate the SCB table to valkrets x five-year band
tr -d '\n' < "$J" | grep -o '{"key":\["[0-9]*","[0-9+]*","[12]","2024"\],"values":\["[0-9]*"\]}' | sed 's/{"key":\["\([0-9]*\)","\([0-9+]*\)","\([12]\)","2024"\],"values":\["\([0-9]*\)"\]}/\1,\2,\3,\4/' > /tmp/rows.csv
echo "rows parsed: $(wc -l < /tmp/rows.csv)"
awk -F, 'FNR==NR { vk[$1]=$3; next } { a=$2; band = (a=="100+") ? 20 : int(a/5); if (band>20) band=20; pop[vk[$1] "," band] += $4; tot[vk[$1]] += $4; all += $4 }
END { printf "valkrets_no"; for (b=0;b<=20;b++) printf ",band_%d", b; print ",total"; for (v=1;v<=29;v++) { printf "%d", v; for (b=0;b<=20;b++) printf ",%d", pop[v "," b]; printf ",%d\n", tot[v] } print "ALL TOTAL " all > "/dev/stderr" }' /tmp/map.csv /tmp/rows.csv > /tmp/agg.csv 2> /tmp/agg_total.txt
cat /tmp/agg_total.txt
{ echo "# Population 31 December 2024 (SCB BE0101N1, both sexes) by Riksdag valkrets and five-year age band (band_0 = 0-4 … band_20 = 100+), aggregated from ElectionsData/sweden/scb_BefolkningNy_2024_municipality_age_sex.json through valkrets_municipalities_2024.csv. Built 2026-09-02; the ALL TOTAL reconciles to SCB's national figure (see README)."; cat /tmp/agg.csv; } > ElectionsData/sweden/valkrets_population_by_age_2024.csv
sha256sum ElectionsData/sweden/scb_BefolkningNy_2024_municipality_age_sex.json ElectionsData/sweden/riksdagen_vallag_2005_837.html ElectionsData/sweden/valkrets_municipalities_2024.csv ElectionsData/sweden/valkrets_population_by_age_2024.csv | cut -c1-16,64-
