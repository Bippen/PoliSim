#!/bin/bash
# run.sh <srcRoot> <mode> [args]: K302's out-of-Unity measurement (Program.cs) against <srcRoot>'s runtime, built as the compile gate builds it
# (Tools/cgate/make.pl). ~15 s. Never Unity. Modes: Program.cs's header.
set -eu
[ -d "${1:-}/Assets/Scripts" ] || { echo "no project at '${1:-}'"; exit 2; }
WT=$(cd "$1" && pwd); shift
HERE=$(cd "$(dirname "$0")" && pwd)
OUT=$(mktemp -d); trap 'rm -rf "$OUT"' EXIT
perl "$HERE/../cgate/make.pl" "$(cygpath -w "$WT")" "$(cygpath -w "$OUT")" 'G:\UNITY\Projects\PoliSim' >/dev/null
(cd "$OUT" && dotnet build Runtime.csproj -nologo -v q -clp:ErrorsOnly >/dev/null) || { echo "runtime build failed"; exit 1; }
mkdir "$OUT/h" && cp "$HERE/Art155Replay.csproj" "$HERE/Program.cs" "$OUT/h/"
(cd "$OUT/h" && dotnet build -nologo -v q -clp:ErrorsOnly -p:RuntimeDll="$(cygpath -w "$OUT/bin/Assembly-CSharp.dll")" >"$OUT/hbuild.txt") || { cat "$OUT/hbuild.txt"; exit 1; }
dotnet "$OUT/h/bin/Debug/net10.0/Art155Replay.dll" "$@"
