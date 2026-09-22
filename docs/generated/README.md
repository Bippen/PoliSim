# The generated documents — what writes each, and the one rule

⚠ **NEVER HAND-EDITED.** Each file here is emitted by a tool and is re-derivable by one command; a hand edit is
overwritten the next time the tool runs, and until then it is a claim nothing checks. This is the claim
convention's GENERATED destination (`CLAUDE.md`): a derived figure lives in a marked block a tool writes.

| file | written by | command |
|---|---|---|
| `docs/generated/LEVER_MAP.md` | `PoliSim.EditorTools.LeverMapDump` | `-executeMethod PoliSim.EditorTools.LeverMapDump.Run [-levermapout=<path>]` |
| `docs/generated/BUDGET_PREMISE.md` | `PoliSim.EditorTools.BudgetPremiseDump` | `-executeMethod PoliSim.EditorTools.BudgetPremiseDump.Run [-premiseout=<path>]` |
| `docs/generated/ENERGY_LAYER_PREMISE.md` | `PoliSim.EditorTools.EnergyLayerDump` | `-executeMethod PoliSim.EditorTools.EnergyLayerDump.Run [-energyout=<path>]` |
| `docs/generated/POTENTIAL_PREMISE.md` | `PoliSim.EditorTools.PotentialPremiseDump` | `-executeMethod PoliSim.EditorTools.PotentialPremiseDump.Run` |

Each runs `Unity.exe -batchmode -nographics -projectPath <path> -executeMethod <above> -logFile <log>`.
All four moved here from the repository root on 2026-09-22 (`COMPLETED.md` §579) and their tools' default
output paths moved with them, so a regeneration writes here and not to the root.
