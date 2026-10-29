#!/bin/bash
# K276 (2026-10-29): LongRunAnchorsDiagnostic's no-policy band ((d1)/(d2)) and ParticipationCohortDiagnostic OUT OF THE ENGINE - a loop instrument,
# never the bar (hold.sh's standing, K218). The integrator's Unity run is the evidence; this answers "what will it print" in a few minutes.
#   bash Tools/headless_hold/anchors.sh <srcRoot>        (a worktree or the main tree; compiled against the main project's Library, as Tools/cgate does)
# What it does: hold.sh's build (the runtime from <srcRoot>, the engine module's MonoBehaviour constructor patched in a COPY), then NoPolicyCentury,
# LongRunAnchorsDiagnostic and ParticipationCohortDiagnostic from <srcRoot> with hold.sh's textual changes - the century's GameObject host becomes a
# plain `new SimulationManager()`, the diagnostics' log lines go to the console, CheckExit is not armed. LongRunAnchorsDiagnostic's no-policy band is
# invoked by name (its other parts host a GameObject and are not run here); ParticipationCohortDiagnostic runs whole.
# ⚠ Out of the engine is .NET's runtime, not Mono's: float intermediates may round differently - a figure is a prediction to the printed tenth at best.
set -eu
ROOT=$(cd "$1" && pwd)
SRC=$(cygpath -w "$ROOT")
LIB='G:\UNITY\Projects\PoliSim'
HERE=$(cd "$(dirname "$0")" && pwd)
CORE='G:\UNITY\Unity Hub\6000.5.6f1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll'
OUT=$(mktemp -d)
trap '[ -n "${KEEP:-}" ] || rm -rf "$OUT"' EXIT
mkdir -p "$OUT/rt" "$OUT/patch" "$OUT/hold"
perl "$HERE/../cgate/make.pl" "$SRC" "$(cygpath -w "$OUT/rt")" "$LIB" >/dev/null
(cd "$OUT/rt" && dotnet build Runtime.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "HEADLESS ANCHORS: the runtime did not build"; exit 2; }
cp "$HERE/ctorpatch/CtorPatch.cs" "$OUT/patch/"
CECIL=$(ls -d "$(cygpath -u "$LIB")"/Library/PackageCache/com.unity.nuget.mono-cecil@*/Mono.Cecil.dll | head -1)
printf '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Reference Include="Mono.Cecil"><HintPath>%s</HintPath></Reference></ItemGroup></Project>
' "$(cygpath -w "$CECIL")" > "$OUT/patch/ctorpatch.csproj"
(cd "$OUT/patch" && dotnet build ctorpatch.csproj -nologo -v q -clp:ErrorsOnly >/dev/null && dotnet bin/Debug/net8.0/ctorpatch.dll "$CORE" "$(cygpath -w "$OUT/patch/UnityEngine.CoreModule.dll")" >/dev/null) || { echo "HEADLESS ANCHORS: the engine-module patch failed"; exit 2; }
perl "$HERE/mkproj.pl" "$OUT/rt/Runtime.csproj" "$(cygpath -w "$OUT/rt/bin/Assembly-CSharp.dll")" "$(cygpath -w "$OUT/patch/UnityEngine.CoreModule.dll")" > "$OUT/hold/hold.csproj"
cp "$HERE/AnchorsMain.cs" "$OUT/hold/"
perl -pe 's/var go = new GameObject\("NOPOLICYCENTURY"\);/object go = null;/; s/SimulationManager sim = go\.AddComponent<SimulationManager>\(\);/SimulationManager sim = new SimulationManager();/; s/finally \{ Object\.DestroyImmediate\(go\); \}/finally { }/' "$ROOT/Assets/Editor/NoPolicyCentury.cs" > "$OUT/hold/NoPolicyCentury.cs"
for d in LongRunAnchorsDiagnostic ParticipationCohortDiagnostic; do
  perl -pe 's/CheckExit\.ArmLogFold\(\);//; s/CheckExit\.Finish\([^;]*\);//; s/Debug\.Log(Error)?\(/System.Console.WriteLine(/g' "$ROOT/Assets/Editor/$d.cs" > "$OUT/hold/$d.cs"
done
grep -q "new SimulationManager()" "$OUT/hold/NoPolicyCentury.cs" || { echo "HEADLESS ANCHORS: NoPolicyCentury's host line moved - the rewrite matched nothing"; exit 2; }
(cd "$OUT/hold" && dotnet build hold.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "HEADLESS ANCHORS: the hold did not build"; (cd "$OUT/hold" && dotnet build hold.csproj -nologo -v q -clp:ErrorsOnly | grep -E " error " | head -20); exit 2; }
dotnet "$OUT/hold/bin/Debug/net8.0/hold.dll"
