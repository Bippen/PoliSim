#!/usr/bin/env bash
# A-LOOP q5 (docs/specs/A_LOOP_PLAN.md section 6 (3), section 8's BILLED "Eurobarometer QA3 trend series ... Fetched in piece 6"):
# fetches, byte-exact with curl, the Standard Eurobarometer "Volume A" table workbook (every question by country, weighted shares) of each
# wave the salience fit reads, and each wave's data.europa.eu dataset record (the European Commission's DG COMM catalogue entry that lists
# the workbook's download link - the provenance of the link). Nothing is opened, unzipped or re-saved: the files land as served, their
# digests in docs/reference/raw/aloop/eb/SHA256SUMS.txt and every request in fetch_log.txt (url, file, UTC time, HTTP status, bytes).
# Tools/aloop_salience_fit.pl reads them.
# Usage (from the project root): bash Tools/aloop_eb_fetch.sh
set -u
D=docs/reference/raw/aloop/eb
mkdir -p "$D"
UA="Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
LOG="$D/fetch_log.txt"
[ -f "$LOG" ] || printf 'url\tfile\tfetched_utc\thttp_status\tbytes\n' > "$LOG"

fetch() {
  local u="$1" f="$2" st
  st=$(curl -sL -m 300 -A "$UA" -o "$D/$f" -w "%{http_code}" "$u")
  printf '%s\t%s\t%s\t%s\t%s\n' "$u" "$f" "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$st" "$(wc -c < "$D/$f" | tr -d " ")" >> "$LOG"
}

# wave <tab> data.europa.eu dataset id <tab> stored name <tab> the record's download link for its Volume A
while IFS=$'\t' read -r wave id name url; do
  [ -z "$wave" ] && continue
  fetch "https://data.europa.eu/api/hub/search/datasets/$id" "dataeuropa_${id}.json"
  fetch "$url" "$name"
done <<'EOF'
83	s2099_83_3_std83_eng	eb83_VolumeAEB833COMMcomplet20150619_xls.zip	https://webgate.ec.europa.eu/eurobarometer/api/public/odp/download?key=546421185E5D70B523A194E58C3AACD1
84	s2098_84_3_std84_eng	eb84_VolumeAEB843COMMstandard20151119_xls.zip	https://webgate.ec.europa.eu/eurobarometer/api/public/odp/download?key=DD0D0511ADA485576EDBCFB34FA84F62
85	s2130_85_2_std85_eng	eb85_EB85.2_Volume_A_xls.zip	https://webgate.ec.europa.eu/eurobarometer/api/public/odp/download?key=E7795D544EC9141DE2B3F718F1C69E80
86	s2137_86_2_std86_eng	eb86_ST86_Volume_A_xls.zip	https://webgate.ec.europa.eu/eurobarometer/api/public/odp/download?key=0DFE05B6D69DACACAF2CBDAC57B4ED37
87	s2142_87_3_std87_eng	eb87_Volume_A_xls.zip	https://webgate.ec.europa.eu/eurobarometer/api/public/odp/download?key=F01145E536AAFD4E292E91165A232CA5
88	s2143_88_3_std88_eng	eb88_Volume_A_xlsl.zip	https://webgate.ec.europa.eu/eurobarometer/api/public/odp/download?key=E84CDFA31A2687A1864EC02078F99BEA
89	s2180_89_1_std89_eng	eb89_vol_A_xls.zip	https://webgate.ec.europa.eu/eurobarometer/api/public/odp/download?key=ED91B120BAF1F44FFD008C77E9BA34FE
90	s2215_90_3_std90_eng	eb90_volume_A_xls.zip	https://webgate.ec.europa.eu/eurobarometer/api/public/odp/download?key=AA59CDC91B966A9A7064F4F700A916C3
91	s2253_91_5_std91_eng	eb91_volume_A_xls.zip	https://webgate.ec.europa.eu/eurobarometer/api/public/odp/download?key=BD32911BEED333EC8E89A49A4EE084F5
92	s2255_92_3_std92_eng	eb92_volume_A.zip	https://webgate.ec.europa.eu/eurobarometer/api/public/odp/download?key=3A3D4A80BC3648F6E1BF5F2157F58A1E
93	s2262_93_1_93_1_eng	eb93_volume_A.zip	https://webgate.ec.europa.eu/ebsm/api/public/odp/download?key=30C0FB6220D55B50B2DBE9A093B20A11
94	s2355_94_1_std94_eng	eb94_volume_A.xlsx	https://webgate.ec.europa.eu/eurobarometer/api/public/odp/download?key=ACD89D783A5E2C6E54B6B9BA1C5751EF
95	s2532_95_3_95_eng	eb95_vol_A.xlsx	https://webgate.ec.europa.eu/ebsm/api/public/odp/download?key=3DBFC38A29B12076FF0CF68FE03E83CF
96	s2553_96_3_std96_eng	eb96_Volume_A.xlsx	https://webgate.ec.europa.eu/ebsm/api/public/odp/download?key=380C6267C97A89EEC7117E72F4948595
97	s2693_97_5_std97_eng	eb97_Volume_A.xlsx	https://webgate.ec.europa.eu/ebsm/api/public/odp/download?key=BFB9C0CCD4CDFA7AE9B5828600C2BCC7
98	s2872_98_2_std98_eng	eb98_volume_A.xlsx	https://webgate.ec.europa.eu/ebsm/api/public/odp/download?key=8B1605BC8964BBFFEC49C32E79FF9A63
99	s3052_99_4_std99_eng	eb99_volume_A.xlsx	https://webgate.ec.europa.eu/ebsm/api/public/odp/download?key=0CCD7D6A9EDFF30E68F80A00649E91B6
100	s3053_100_2_std100_eng	eb100_Volume_A.xlsx	https://webgate.ec.europa.eu/ebsm/api/public/odp/download?key=63ACA1A088544CA14E217226F8E32A1F
101	s3216_101_3_std101_eng	eb101_VOL_A.xlsx	https://webgate.ec.europa.eu/ebsm/api/public/odp/download?key=202289A0E60356141402B692C20F5193
102	s3215_102_2_std102_eng	eb102_volume_A.xlsx	https://webgate.ec.europa.eu/ebsm/api/public/odp/download?key=A2534D981B6C0CF8878DEB8447FB08F4
103	s3372_103_3_std103_eng	eb103_volume_A.xlsx	https://webgate.ec.europa.eu/ebsm/api/public/odp/download?key=AF452C0A6DD70E5D13273C5DF2086956
EOF

( cd "$D" && sha256sum -b $(ls | grep -v -e '^SHA256SUMS.txt$' -e '^fetch_log.txt$' | sort) > SHA256SUMS.txt )
echo "fetched; digests in $D/SHA256SUMS.txt"
