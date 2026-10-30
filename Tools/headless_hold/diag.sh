#!/bin/bash
# fisc2 (2026-10-29): A NAMED DIAGNOSTIC OUT OF THE ENGINE - hold.sh's idiom for any Editor diagnostic that needs no engine but a SimulationManager host and the
# log: a loop instrument, never the bar (hold.sh's standing). The diagnostic and the Editor files it names are compiled from <srcRoot> with hold.sh's textual
# changes made general - a `new GameObject(...)` host becomes a plain `new SimulationManager()`, DestroyImmediate does nothing, every Debug.Log / LogWarning /
# LogError line goes to the console (an error line prefixed ERROR:), CheckExit is not armed - and its Run() is called. NoPolicyCentury.cs and the generated
# Editor anchors ride along. A diagnostic that needs anything else of the engine does not build here, and says so.
#   bash Tools/headless_hold/diag.sh <srcRoot> <DiagnosticClass> [more Assets/Editor files the diagnostic calls, by file name]
# ⚠ .NET's runtime, not Mono's (hold.sh's caveat): a prediction to the printed decimal at best.
set -eu
ROOT=$(cd "$1" && pwd)   # ⚠ absolute (hold.sh's trap)
NAME=$2; shift 2
SRC=$(cygpath -w "$ROOT")
LIB='G:\UNITY\Projects\PoliSim'
HERE=$(cd "$(dirname "$0")" && pwd)
CORE='G:\UNITY\Unity Hub\6000.5.6f1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll'
OUT=$(mktemp -d)
trap '[ -n "${KEEP:-}" ] || rm -rf "$OUT"' EXIT
mkdir -p "$OUT/rt" "$OUT/patch" "$OUT/diag"
perl "$HERE/../cgate/make.pl" "$SRC" "$(cygpath -w "$OUT/rt")" "$LIB" >/dev/null
(cd "$OUT/rt" && dotnet build Runtime.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "DIAG: the runtime did not build"; exit 2; }
cp "$HERE/ctorpatch/CtorPatch.cs" "$OUT/patch/"
CECIL=$(ls -d "$(cygpath -u "$LIB")"/Library/PackageCache/com.unity.nuget.mono-cecil@*/Mono.Cecil.dll | head -1)
printf '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Reference Include="Mono.Cecil"><HintPath>%s</HintPath></Reference></ItemGroup></Project>
' "$(cygpath -w "$CECIL")" > "$OUT/patch/ctorpatch.csproj"
(cd "$OUT/patch" && dotnet build ctorpatch.csproj -nologo -v q -clp:ErrorsOnly >/dev/null && dotnet bin/Debug/net8.0/ctorpatch.dll "$CORE" "$(cygpath -w "$OUT/patch/UnityEngine.CoreModule.dll")" >/dev/null) || { echo "DIAG: the engine-module patch failed"; exit 2; }
perl "$HERE/mkproj.pl" "$OUT/rt/Runtime.csproj" "$(cygpath -w "$OUT/rt/bin/Assembly-CSharp.dll")" "$(cygpath -w "$OUT/patch/UnityEngine.CoreModule.dll")" > "$OUT/diag/diag.csproj"
printf 'public static class HeadlessDiag { public static void Main() { System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; UnityEngine.Debug.unityLogger.logEnabled = false; PoliSim.EditorTools.%s.Run(); } }\n' "$NAME" > "$OUT/diag/Main.cs"
cp "$ROOT"/Assets/Editor/Generated/*.cs "$OUT/diag/" 2>/dev/null || true
for F in NoPolicyCentury.cs "$NAME.cs" "$@"; do
  perl -pe 's/var go = new GameObject\([^)]*\);/object go = null;/; s/var (\w+) = new GameObject\([^)]*\);/object $1 = null;/g; s/\b(\w+)\.AddComponent<SimulationManager>\(\)/new SimulationManager()/g; s/(UnityEngine\.)?Object\.DestroyImmediate\([^)]*\);//g; s/CheckExit\.ArmLogFold\(\);//; s/CheckExit\.Finish\([^;]*\);//; s/Debug\.LogError\(/System.Console.WriteLine("ERROR: " + /g; s/Debug\.Log(Warning)?\(/System.Console.WriteLine(/g' "$ROOT/Assets/Editor/$F" > "$OUT/diag/$F"
done
(cd "$OUT/diag" && dotnet build diag.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "DIAG: $NAME did not build out of the engine"; (cd "$OUT/diag" && dotnet build diag.csproj -nologo -v q -clp:ErrorsOnly | grep -E " error " | sed 's/.*error/error/' | sort -u | head -15); exit 2; }
dotnet "$OUT/diag/bin/Debug/net8.0/diag.dll"
