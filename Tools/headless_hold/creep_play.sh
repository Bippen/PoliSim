#!/bin/bash
# K275 (2026-10-29): THE INCOME TAX'S CREEP CAP OUT OF THE ENGINE - a loop instrument, never the bar (hold.sh's standing). frf_play.sh's driving idiom (seed,
# the start, PolicyDecision.None for every country, a turn a year), with no player (the dump's all-AI world) or one country the player with no party seated
# (the shared century's idle book). Per responding country it prints the statute's creep, its bound, the ratio form and the capped ratio the revenue reads,
# the take on the anchored base, the flat-equivalent rate capped and uncapped, the participation term and the income tax as per cent of GDP - the player's every year, every country every tenth year with no player - then, at
# years 1, 40 and 100, the player's own moves (the lever one point up and down, the top band at 0) capped against the ratio form, and last the AI books'
# largest creep over its bound and how many country-years the cap bound there. IncomeTaxCreepCapDiagnostic asserts the same statements in the engine.
#   bash Tools/headless_hold/creep_play.sh <srcRoot> <none|Poland|Germany|...> <years> [seed]
# ⚠ .NET's runtime, not Mono's (hold.sh's caveat): a prediction to the printed decimal at best.
set -eu
ROOT=$(cd "$1" && pwd)   # ⚠ absolute (hold.sh's trap)
PLAYER=$2; YEARS=$3; SEED=${4:-777}
SRC=$(cygpath -w "$ROOT")
LIB='G:\UNITY\Projects\PoliSim'
HERE=$(cd "$(dirname "$0")" && pwd)
CORE='G:\UNITY\Unity Hub\6000.5.6f1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll'
OUT=$(mktemp -d)
trap '[ -n "${KEEP:-}" ] || rm -rf "$OUT"' EXIT
mkdir -p "$OUT/rt" "$OUT/patch" "$OUT/play"
perl "$HERE/../cgate/make.pl" "$SRC" "$(cygpath -w "$OUT/rt")" "$LIB" >/dev/null
(cd "$OUT/rt" && dotnet build Runtime.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "CREEP PLAY: the runtime did not build" >&2; exit 2; }
cp "$HERE/ctorpatch/CtorPatch.cs" "$OUT/patch/"
CECIL=$(ls -d "$(cygpath -u "$LIB")"/Library/PackageCache/com.unity.nuget.mono-cecil@*/Mono.Cecil.dll | head -1)
printf '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Reference Include="Mono.Cecil"><HintPath>%s</HintPath></Reference></ItemGroup></Project>
' "$(cygpath -w "$CECIL")" > "$OUT/patch/ctorpatch.csproj"
(cd "$OUT/patch" && dotnet build ctorpatch.csproj -nologo -v q -clp:ErrorsOnly >/dev/null && dotnet bin/Debug/net8.0/ctorpatch.dll "$CORE" "$(cygpath -w "$OUT/patch/UnityEngine.CoreModule.dll")" >/dev/null) || { echo "CREEP PLAY: the engine-module patch failed" >&2; exit 2; }
perl "$HERE/mkproj.pl" "$OUT/rt/Runtime.csproj" "$(cygpath -w "$OUT/rt/bin/Assembly-CSharp.dll")" "$(cygpath -w "$OUT/patch/UnityEngine.CoreModule.dll")" > "$OUT/play/play.csproj"
cp "$HERE/CreepPlay.cs" "$OUT/play/"
(cd "$OUT/play" && dotnet build play.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "CREEP PLAY: the runner did not build" >&2; exit 2; }
dotnet "$OUT/play/bin/Debug/net8.0/play.dll" "$PLAYER" "$YEARS" "$SEED"
