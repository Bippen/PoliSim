#!/bin/bash
# K218 (2026-10-28): DebtPathHoldDiagnostic OUT OF THE ENGINE - a loop instrument, never the bar (Tools/textcheck's standing). The hold's Unity run is the
# evidence; this answers "what will it print" in about two minutes, so a prereg's predictions are measured rather than estimated.
#   bash Tools/headless_hold/hold.sh <srcRoot>        (a worktree or the main tree; compiled against the main project's Library, as Tools/cgate does)
# What it does: builds the runtime assembly from <srcRoot> (Tools/cgate/make.pl), patches a COPY of Unity's engine module so MonoBehaviour's constructor
# needs no engine (ctorpatch/), and compiles NoPolicyCentury, DebtPathHoldDiagnostic and the generated anchors from <srcRoot> with three textual changes -
# the century's GameObject host becomes a plain `new SimulationManager()`, the diagnostic's log lines go to the console, and CheckExit is not armed.
# ⚠ Out of the engine is .NET's runtime, not Mono's: float intermediates may round differently, so a figure is a prediction to the printed tenth at
# best - K218 measured it against the integrator's ps8 sim bar log (France, Poland and the USA's five REGISTERED lines) and it matched each to the tenth.
set -eu
ROOT=$(cd "$1" && pwd)   # ⚠ absolute: make.pl writes <srcRoot> into the project's globs, and a relative root resolves against the temp folder - an empty build
SRC=$(cygpath -w "$ROOT")
LIB='G:\UNITY\Projects\PoliSim'
HERE=$(cd "$(dirname "$0")" && pwd)
CORE='G:\UNITY\Unity Hub\6000.5.6f1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll'
OUT=$(mktemp -d)
trap '[ -n "${KEEP:-}" ] || rm -rf "$OUT"' EXIT
mkdir -p "$OUT/rt" "$OUT/patch" "$OUT/hold"
perl "$HERE/../cgate/make.pl" "$SRC" "$(cygpath -w "$OUT/rt")" "$LIB" >/dev/null
(cd "$OUT/rt" && dotnet build Runtime.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "HEADLESS HOLD: the runtime did not build"; exit 2; }
cp "$HERE/ctorpatch/CtorPatch.cs" "$OUT/patch/"
# the patcher's project is written here (the repo ignores *.csproj): Mono.Cecil from the main project's package cache
CECIL=$(ls -d "$(cygpath -u "$LIB")"/Library/PackageCache/com.unity.nuget.mono-cecil@*/Mono.Cecil.dll | head -1)
printf '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Reference Include="Mono.Cecil"><HintPath>%s</HintPath></Reference></ItemGroup></Project>
' "$(cygpath -w "$CECIL")" > "$OUT/patch/ctorpatch.csproj"
(cd "$OUT/patch" && dotnet build ctorpatch.csproj -nologo -v q -clp:ErrorsOnly >/dev/null && dotnet bin/Debug/net8.0/ctorpatch.dll "$CORE" "$(cygpath -w "$OUT/patch/UnityEngine.CoreModule.dll")" >/dev/null) || { echo "HEADLESS HOLD: the engine-module patch failed"; exit 2; }
perl "$HERE/mkproj.pl" "$OUT/rt/Runtime.csproj" "$(cygpath -w "$OUT/rt/bin/Assembly-CSharp.dll")" "$(cygpath -w "$OUT/patch/UnityEngine.CoreModule.dll")" > "$OUT/hold/hold.csproj"
cp "$HERE/Main.cs" "$OUT/hold/"
cp "$ROOT/Assets/Editor/Generated/DebtPathAnchors.cs" "$OUT/hold/"
perl -pe 's/var go = new GameObject\("NOPOLICYCENTURY"\);/object go = null;/; s/SimulationManager sim = go\.AddComponent<SimulationManager>\(\);/SimulationManager sim = new SimulationManager();/; s/finally \{ Object\.DestroyImmediate\(go\); \}/finally { }/' "$ROOT/Assets/Editor/NoPolicyCentury.cs" > "$OUT/hold/NoPolicyCentury.cs"
perl -pe 's/CheckExit\.ArmLogFold\(\);//; s/CheckExit\.Finish\([^;]*\);//; s/Debug\.Log(Error)?\(/System.Console.WriteLine(/g' "$ROOT/Assets/Editor/DebtPathHoldDiagnostic.cs" > "$OUT/hold/DebtPathHoldDiagnostic.cs"
grep -q "new SimulationManager()" "$OUT/hold/NoPolicyCentury.cs" || { echo "HEADLESS HOLD: NoPolicyCentury's host line moved - the rewrite matched nothing"; exit 2; }
(cd "$OUT/hold" && dotnet build hold.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "HEADLESS HOLD: the hold did not build"; (cd "$OUT/hold" && dotnet build hold.csproj -nologo -v q -clp:ErrorsOnly | grep -E " error " | head -20); exit 2; }
dotnet "$OUT/hold/bin/Debug/net8.0/hold.dll"
