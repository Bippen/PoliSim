#!/bin/bash
# run.sh [--main] <srcRoot> [args]: K273's sweep (Program.cs, Variants.cs) against <srcRoot>'s runtime, built outside Unity as the compile gate builds it.
# --main: the runtime is built from <srcRoot>'s `main` branch instead (git archive), and only Variants.cs is compiled - its `validate` mode then proves
# the generic formation equals main's own CoalitionFormation.Form. ~15 s. Never Unity.
set -eu
FROM_MAIN=0; if [ "${1:-}" = "--main" ]; then FROM_MAIN=1; shift; fi
[ -d "${1:-}/Assets/Scripts" ] || { echo "no project at '${1:-}'"; exit 2; }
WT=$(cd "$1" && pwd); shift
HERE=$(cd "$(dirname "$0")" && pwd)
OUT=$(mktemp -d); trap 'rm -rf "$OUT"' EXIT
if [ $FROM_MAIN = 1 ]; then mkdir "$OUT/src" && git -C "$WT" archive main Assets/Scripts | tar -x -C "$OUT/src"; SRC=$(cygpath -w "$OUT/src"); else SRC=$(cygpath -w "$WT"); fi
perl "$HERE/../cgate/make.pl" "$SRC" "$(cygpath -w "$OUT")" 'G:\UNITY\Projects\PoliSim' >/dev/null
(cd "$OUT" && dotnet build Runtime.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "runtime build failed"; exit 1; }
mkdir "$OUT/h" && cp "$HERE/K273SupportSweep.csproj" "$HERE/Variants.cs" "$HERE/DumpCheck.cs" "$OUT/h/"
DEFINE=""; if [ $FROM_MAIN = 1 ]; then DEFINE="-p:DefineConstants=VARIANTS_ONLY"; else cp "$HERE/Program.cs" "$OUT/h/"; fi
(cd "$OUT/h" && dotnet build -nologo -v q -clp:ErrorsOnly $DEFINE -p:RuntimeDll="$(cygpath -w "$OUT/bin/Assembly-CSharp.dll")" >"$OUT/hbuild.txt") || { cat "$OUT/hbuild.txt"; exit 1; }
dotnet "$OUT/h/bin/Debug/net10.0/K273SupportSweep.dll" "$@"
