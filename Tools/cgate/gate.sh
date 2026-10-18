#!/bin/bash
# gate.sh <srcRoot>: compile the whole project at <srcRoot> (a worktree or the main tree) outside Unity, against the main project's Library.
# Prints "GATE OK" or the errors. ~10 s. Each call builds in its own temp dir, so agents can run it in parallel.
set -u
SRC=$(cygpath -w "$1")
LIB='G:\UNITY\Projects\PoliSim'
OUT=$(mktemp -d)
trap 'rm -rf "$OUT"' EXIT
perl "$(dirname "$0")/make.pl" "$SRC" "$(cygpath -w "$OUT")" "$LIB" >/dev/null || { echo "GATE BROKEN: make.pl"; exit 2; }
res=$(cd "$OUT" && dotnet build Editor.csproj -nologo -v q -clp:ErrorsOnly 2>&1)
if echo "$res" | grep -q "Build succeeded"; then echo "GATE OK"; exit 0; fi
echo "$res" | grep -E "error|Error" | sed "s#$(cygpath -w "$OUT" | sed 's/\\/\\\\/g')##g" | sort -u | head -40
echo "GATE FAILED"; exit 1
