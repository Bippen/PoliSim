#!/bin/bash
# K251 (2026-10-29): THE PLAYED BOOKS OUT OF THE ENGINE - who the fiscal reaction acts on and what it moves, a loop instrument, never the bar (hold.sh's
# standing). The trajectory dump's driving idiom (seed, Sweden's start, PolicyDecision.None for every country, a turn a year), with no player (the dump's
# all-AI world) or one country the player with no party seated (the shared century's idle book), printing every country every year: the debt ratio,
# revenue, the flow balance and the primary balance as per cent of GDP, the multiplier as GetFiscalReactionMultiplier reads it, the rating, the crisis.
#   bash Tools/headless_hold/frf_play.sh <srcRoot> <on|off> <none|Sweden|USA|...> <years> [seed]
# on: the reaction as the source root carries it; off: the sweep hook's pair set to (0, 1, 1) - the reaction retired in every book, the player's too.
# Run it against two source roots (main before K251, the branch after) for the before and after; its stdout is a CSV.
# ⚠ .NET's runtime, not Mono's (hold.sh's caveat): a prediction to the printed decimal at best.
set -eu
ROOT=$(cd "$1" && pwd)   # ⚠ absolute (hold.sh's trap)
MODE=$2; PLAYER=$3; YEARS=$4; SEED=${5:-777}
SRC=$(cygpath -w "$ROOT")
LIB='G:\UNITY\Projects\PoliSim'
HERE=$(cd "$(dirname "$0")" && pwd)
CORE='G:\UNITY\Unity Hub\6000.5.6f1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll'
OUT=$(mktemp -d)
trap '[ -n "${KEEP:-}" ] || rm -rf "$OUT"' EXIT
mkdir -p "$OUT/rt" "$OUT/patch" "$OUT/play"
perl "$HERE/../cgate/make.pl" "$SRC" "$(cygpath -w "$OUT/rt")" "$LIB" >/dev/null
(cd "$OUT/rt" && dotnet build Runtime.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "FRF PLAY: the runtime did not build" >&2; exit 2; }
cp "$HERE/ctorpatch/CtorPatch.cs" "$OUT/patch/"
CECIL=$(ls -d "$(cygpath -u "$LIB")"/Library/PackageCache/com.unity.nuget.mono-cecil@*/Mono.Cecil.dll | head -1)
printf '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Reference Include="Mono.Cecil"><HintPath>%s</HintPath></Reference></ItemGroup></Project>
' "$(cygpath -w "$CECIL")" > "$OUT/patch/ctorpatch.csproj"
(cd "$OUT/patch" && dotnet build ctorpatch.csproj -nologo -v q -clp:ErrorsOnly >/dev/null && dotnet bin/Debug/net8.0/ctorpatch.dll "$CORE" "$(cygpath -w "$OUT/patch/UnityEngine.CoreModule.dll")" >/dev/null) || { echo "FRF PLAY: the engine-module patch failed" >&2; exit 2; }
perl "$HERE/mkproj.pl" "$OUT/rt/Runtime.csproj" "$(cygpath -w "$OUT/rt/bin/Assembly-CSharp.dll")" "$(cygpath -w "$OUT/patch/UnityEngine.CoreModule.dll")" > "$OUT/play/play.csproj"
cp "$HERE/FrfPlay.cs" "$OUT/play/"
(cd "$OUT/play" && dotnet build play.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "FRF PLAY: the runner did not build" >&2; exit 2; }
dotnet "$OUT/play/bin/Debug/net8.0/play.dll" "$MODE" "$PLAYER" "$YEARS" "$SEED"
