#!/bin/bash
# K269 (a) (2026-10-29): THE PLAYER'S IDLE BOOK OUT OF THE ENGINE - a loop instrument, never the bar (hold.sh's standing). frf_play.sh's driving idiom (seed,
# the player's own start, PolicyDecision.None for every country, a turn a year) with one country the player with no party seated (the shared century's
# idle book), printing the player's book every year from the opening (year 0): the debt ratio, revenue, the reaction's multiplier, the income tax's yield,
# total spending, the spending lines (all, driverless, caseload, the pension line, the age-cost lines), interest and the primary balance as per cent of
# GDP, unemployment, what the runaway guards absorbed and the crisis phase; at years 0, 5, 10, 30, 50 and 100 also a LINE row per spending line (its
# category, driver, share of GDP and the driver's level) and a MACRO row (the real wage index, the price level, real and potential GDP, the population) -
# filter on the first field. Built once; each player and seed runs in its own process (no static state carried from one world to the next).
#   bash Tools/headless_hold/player_book.sh <srcRoot> <play|nofrf|unchanged> <players, comma> <years> <seeds, comma>
# play: the book as the source root carries it; nofrf: the sweep hook's pair set to (0, 1, 1) - the reaction retired in every book; unchanged: K249's
# no-policy-change book (SimulationManager.NoPolicyChangeBook) - the hold's premise, for comparison. Its stdout is a CSV.
# ⚠ .NET's runtime, not Mono's (hold.sh's caveat): a prediction to the printed decimal at best.
set -eu
ROOT=$(cd "$1" && pwd)   # ⚠ absolute (hold.sh's trap)
MODE=$2; PLAYERS=$3; YEARS=$4; SEEDS=$5
SRC=$(cygpath -w "$ROOT")
LIB='G:\UNITY\Projects\PoliSim'
HERE=$(cd "$(dirname "$0")" && pwd)
CORE='G:\UNITY\Unity Hub\6000.5.6f1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll'
OUT=$(mktemp -d)
trap '[ -n "${KEEP:-}" ] || rm -rf "$OUT"' EXIT
mkdir -p "$OUT/rt" "$OUT/patch" "$OUT/play"
perl "$HERE/../cgate/make.pl" "$SRC" "$(cygpath -w "$OUT/rt")" "$LIB" >/dev/null
(cd "$OUT/rt" && dotnet build Runtime.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "PLAYER BOOK: the runtime did not build" >&2; exit 2; }
cp "$HERE/ctorpatch/CtorPatch.cs" "$OUT/patch/"
CECIL=$(ls -d "$(cygpath -u "$LIB")"/Library/PackageCache/com.unity.nuget.mono-cecil@*/Mono.Cecil.dll | head -1)
printf '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Reference Include="Mono.Cecil"><HintPath>%s</HintPath></Reference></ItemGroup></Project>
' "$(cygpath -w "$CECIL")" > "$OUT/patch/ctorpatch.csproj"
(cd "$OUT/patch" && dotnet build ctorpatch.csproj -nologo -v q -clp:ErrorsOnly >/dev/null && dotnet bin/Debug/net8.0/ctorpatch.dll "$CORE" "$(cygpath -w "$OUT/patch/UnityEngine.CoreModule.dll")" >/dev/null) || { echo "PLAYER BOOK: the engine-module patch failed" >&2; exit 2; }
perl "$HERE/mkproj.pl" "$OUT/rt/Runtime.csproj" "$(cygpath -w "$OUT/rt/bin/Assembly-CSharp.dll")" "$(cygpath -w "$OUT/patch/UnityEngine.CoreModule.dll")" > "$OUT/play/play.csproj"
cp "$HERE/PlayerBook.cs" "$OUT/play/"
(cd "$OUT/play" && dotnet build play.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "PLAYER BOOK: the runner did not build" >&2; exit 2; }
FIRST=1
for SEED in ${SEEDS//,/ }; do
  for P in ${PLAYERS//,/ }; do
    if [ $FIRST = 1 ]; then dotnet "$OUT/play/bin/Debug/net8.0/play.dll" "$MODE" "$P" "$YEARS" "$SEED"; FIRST=0
    else dotnet "$OUT/play/bin/Debug/net8.0/play.dll" "$MODE" "$P" "$YEARS" "$SEED" | tail -n +2; fi
  done
done
