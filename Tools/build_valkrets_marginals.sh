#!/bin/bash
# F3's third clause (2026-09-02): the INCOME and EDUCATION marginals per Riksdag valkrets, from SCB's
# own municipality tables through the statute mapping valkrets_municipalities_2024.csv (§213's).
# Re-derives ElectionsData/sweden/valkrets_income_by_age_class_2024.csv and
# valkrets_education_by_age_2024.csv from the five saved SCB responses; the fetch queries are recorded
# below (PxWeb v1 is POST-only; the education table exceeds the 150 000-cell limit, so it was fetched in
# four parts: sex x two age halves).
#   income:    START/HE/HE0110/HE0110A/SamForvInk1  - Region=290 municipalities, Kon=1+2, Alder=tot16+,16-19,
#              20-24..85+ (15 bands), Inkomstklass=TOT + 26 classes, ContentsCode=HE0110J9 (Antal personer), Tid=2024
#   education: START/UF/UF0506/UF0506B/UtbBefRegionR - Region=290 municipalities, UtbildningsNiva=1..7,US,
#              Alder=16..94,95+ (80 values), Kon=1|2, ContentsCode=000000I2 (Antal), Tid=2024
set -e
cd "$(dirname "$0")/.."
S=ElectionsData/sweden
W=${TMPDIR:-/tmp}/vkm; mkdir -p "$W"
cat > "$W/px2csv.pl" <<'PL'
local $/; my $j = <STDIN>;
while ($j =~ /\{"key":\[([^\]]*)\],"values":\["([^"]*)"\]\}/g) { my ($k,$v)=($1,$2); $k =~ s/"//g; $v = "" if $v eq ".."; print "$k,$v\n"; }
PL
perl "$W/px2csv.pl" < "$S/scb_SamForvInk1_2024_municipality_age_incomeclass.json" > "$W/inc.csv"
for p in 11 12 21 22; do perl "$W/px2csv.pl" < "$S/scb_UtbBefRegionR_2024_municipality_age_sex_level_part$p.json"; done > "$W/edu.csv"
grep -v '^#\|^kommun_code' "$S/valkrets_municipalities_2024.csv" > "$W/map.csv"
echo "income cells: $(wc -l < "$W/inc.csv") (suppressed $(awk -F, '$6==""' "$W/inc.csv" | wc -l)); education cells: $(wc -l < "$W/edu.csv") (suppressed $(awk -F, '$6==""' "$W/edu.csv" | wc -l))"
# --- income: valkrets x age band x income class; the band's TOT class is published everywhere, so the
# residual TOT - sum(published classes) is exactly the mass SCB suppressed (cells under 3 persons).
awk -F, '
FNR==NR { vk[$1]=$3; next }
{ v=vk[$1]; b=$3; c=$4; if (b=="tot16+") next; if (c=="TOT") { tot[v","b]+=$6 } else if ($6!="") { cell[v","b","c]+=$6; pub[v","b]+=$6 } else { supp[v","b]++ } seen[b]=1 }
END {
  nb=split("16-19 20-24 25-29 30-34 35-39 40-44 45-49 50-54 55-59 60-64 65-69 70-74 75-79 80-84 85+", B, " ")
  nc=split("0 1-19 20-39 40-59 60-79 80-99 100-119 120-139 140-159 160-179 180-199 200-219 220-239 240-259 260-279 280-299 300-319 320-339 340-359 360-379 380-399 400-499 500-599 600-799 800-999 1000+", C, " ")
  printf "valkrets_no,age_band"; for (j=1;j<=nc;j++) printf ",tkr_%s", C[j]; print ",published_sum,total,unpublished,suppressed_cells"
  for (v=1;v<=29;v++) for (i=1;i<=nb;i++) { k=v","B[i]; printf "%d,%s", v, B[i]; for (j=1;j<=nc;j++) printf ",%d", cell[k","C[j]]; printf ",%d,%d,%d,%d\n", pub[k], tot[k], tot[k]-pub[k], supp[k]; T+=tot[k]; U+=tot[k]-pub[k] }
  print "INCOME all valkretsar 16+: total " T ", unpublished " U > "/dev/stderr"
}' "$W/map.csv" "$W/inc.csv" > "$W/inc_agg.csv" 2> "$W/inc_tot.txt"
cat "$W/inc_tot.txt"
{ echo "# Persons 16+ resident all of 2024 by sammanräknad förvärvsinkomst class (tkr, SCB HE0110 SamForvInk1, HE0110J9 Antal personer, both sexes), by Riksdag valkrets and age band, aggregated from scb_SamForvInk1_2024_municipality_age_incomeclass.json through valkrets_municipalities_2024.csv. 'total' is the band's published TOT class; 'unpublished' = total - published_sum is exactly the mass SCB suppressed ('..', cells under 3 persons) in 'suppressed_cells' cells. Built 2026-09-02 by Tools/build_valkrets_marginals.sh; see VALKRETS_POPULATION_README.md."; cat "$W/inc_agg.csv"; } > "$S/valkrets_income_by_age_class_2024.csv"
# --- education: valkrets x five-year band (band_3 = 16-19 ... band_19 = 95+, SCB's top class) x level, both sexes
awk -F, '
FNR==NR { vk[$1]=$3; next }
{ v=vk[$1]; a=$3; if (a=="95+") b=19; else { b=int(a/5); if (b>19) b=19 } cell[v","b","$2]+=$6; tot[v","b]+=$6; ALL+=$6 }
END {
  nl=split("1 2 3 4 5 6 7 US", L, " ")
  printf "valkrets_no,band"; for (j=1;j<=nl;j++) printf ",level_%s", L[j]; print ",total"
  for (v=1;v<=29;v++) for (b=3;b<=19;b++) { k=v","b; printf "%d,%d", v, b; for (j=1;j<=nl;j++) printf ",%d", cell[k","L[j]]; printf ",%d\n", tot[k] }
  print "EDUCATION all valkretsar 16+: " ALL > "/dev/stderr"
}' "$W/map.csv" "$W/edu.csv" > "$W/edu_agg.csv" 2> "$W/edu_tot.txt"
cat "$W/edu_tot.txt"
{ echo "# Population 16+ at 31 December 2024 by education level (SUN 2000 level codes: 1 förgymnasial <9 år, 2 förgymnasial 9 (10) år, 3 gymnasial <=2 år, 4 gymnasial 3 år, 5 eftergymnasial <3 år, 6 eftergymnasial >=3 år, 7 forskarutbildning, US uppgift saknas; SCB UF0506 UtbBefRegionR, both sexes), by Riksdag valkrets and five-year age band (band_3 = 16-19 ... band_19 = 95+), aggregated from the four scb_UtbBefRegionR_2024_municipality_age_sex_level_part*.json through valkrets_municipalities_2024.csv. Built 2026-09-02 by Tools/build_valkrets_marginals.sh; see VALKRETS_POPULATION_README.md."; cat "$W/edu_agg.csv"; } > "$S/valkrets_education_by_age_2024.csv"
sha256sum "$S"/scb_SamForvInk1_2024_municipality_age_incomeclass.json "$S"/scb_UtbBefRegionR_2024_municipality_age_sex_level_part*.json "$S"/valkrets_income_by_age_class_2024.csv "$S"/valkrets_education_by_age_2024.csv | cut -c1-16,64-
