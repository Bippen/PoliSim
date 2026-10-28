#!/bin/bash
# gate.sh <srcRoot>: compile the whole project at <srcRoot> (a worktree or the main tree) outside Unity, against the main project's Library.
# Prints "GATE OK" or the errors. ~10 s. Each call builds in its own temp dir, so agents can run it in parallel.
set -u
[ -d "${1:-}/Assets/Scripts" ] || { echo "GATE BROKEN: no project at '${1:-}' (Assets/Scripts not found)"; exit 2; }   # s940 (K218 review M1): a missing root built nothing and printed GATE OK
SRC=$(cygpath -w "$(cd "$1" && pwd)")   # K218: absolute - make.pl writes the root into the globs, and a relative one (".") resolved against the temp folder: an empty build that printed GATE OK
LIB='G:\UNITY\Projects\PoliSim'
OUT=$(mktemp -d)
trap 'rm -rf "$OUT"' EXIT
perl "$(dirname "$0")/make.pl" "$SRC" "$(cygpath -w "$OUT")" "$LIB" >/dev/null || { echo "GATE BROKEN: make.pl"; exit 2; }
res=$(cd "$OUT" && dotnet build Editor.csproj -nologo -v q -clp:ErrorsOnly 2>&1)
if echo "$res" | grep -q "CS2008"; then echo "GATE BROKEN: no source files compiled (CS2008)"; exit 2; fi   # s940: an empty build is never OK
if echo "$res" | grep -q "Build succeeded"; then echo "GATE OK"; exit 0; fi
echo "$res" | grep -E "error|Error" | sed "s#$(cygpath -w "$OUT" | sed 's/\\/\\\\/g')##g" | sort -u | head -40
echo "GATE FAILED"; exit 1
