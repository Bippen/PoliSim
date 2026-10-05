# The generated documents — what writes each, and the one rule

⚠ **NEVER HAND-EDITED.** Each file here is emitted by a tool and is re-derivable by one command; a hand edit is
overwritten the next time the tool runs, and until then it is a claim nothing checks. This is the claim
convention's GENERATED destination (`CLAUDE.md`): a derived figure lives in a marked block a tool writes.

| file | written by | command |
|---|---|---|
| `docs/generated/LEVER_MAP.md` | `PoliSim.EditorTools.LeverMapDump` | `-executeMethod PoliSim.EditorTools.LeverMapDump.Run [-levermapout=<path>]` |
| `docs/generated/PEOPLE_AT_REST_REMOVED.tsv` | `Tools/text_baseline.pl removed` (§666) | `perl Tools/text_baseline.pl removed <before labels.tsv> <after labels.tsv> 04_demographics People EMPLOYMENT` - the before and after dry films the file's header names |
| `docs/generated/STATS_AT_REST_REMOVED.tsv` | `Tools/text_baseline.pl removed-set` (§728) | `perl Tools/text_baseline.pl removed-set <before labels.tsv> <after labels.tsv> Statistics 02a_statistics_domestic 02a_statistics_domestic_deep 02a_statistics_domestic_rows 02b_statistics_international 02b_statistics_international_deep 02b_statistics_international_rows` - before = drys698 (pre-D-ST), after = the latest Statistics pass's dry film, as the file's header names |
| `docs/generated/PEOPLE_V35_AT_REST_REMOVED.tsv` | `Tools/text_baseline.pl removed-set` (§732) | `perl Tools/text_baseline.pl removed-set <before labels.tsv> <after labels.tsv> People 04_demographics 04a_demographics_pie 04b_people_health_plate 04c_people_education_plate 04d_people_infrastructure_plate 04e_people_environment_plate 04f_people_migration_plate 04a_people_dependency_electorate 04b_people_health 04c_people_education 04d_people_infrastructure 04e_people_environment 04f_people_migration_employment` - the at-rest frames under their old names and their new; before = drys729 (the plates), after = the People pass's dry film, as the file's header names. Read with the §666 list by PeopleSlipReachabilityCheck |
| `docs/generated/BUDGET_PREMISE.md` | `PoliSim.EditorTools.BudgetPremiseDump` | `-executeMethod PoliSim.EditorTools.BudgetPremiseDump.Run [-premiseout=<path>]` |
| `docs/generated/ENERGY_LAYER_PREMISE.md` | `PoliSim.EditorTools.EnergyLayerDump` | `-executeMethod PoliSim.EditorTools.EnergyLayerDump.Run [-energyout=<path>]` |
| `docs/generated/POTENTIAL_PREMISE.md` | `PoliSim.EditorTools.PotentialPremiseDump` | `-executeMethod PoliSim.EditorTools.PotentialPremiseDump.Run` |
| `docs/generated/UI_V33_BASELINE.tsv` | `Tools/text_baseline.pl` (from a dry film's label table, §648) | `perl Tools/text_baseline.pl rank ../PoliSim-captures/labels/drys648_sweden_1280_labels.tsv > docs/generated/UI_V33_BASELINE.tsv` |
| `docs/reference/PRESIDENTIAL_VOTE.md` - the marked block under *The readings* (§769), not a file of its own: the model card carries its figures | `PoliSim.EditorTools.PresidentialVoteBacktest.WriteReadings` | `-executeMethod PoliSim.EditorTools.PresidentialVoteBacktest.WriteReadings` - and `PresidentialVoteBacktest.Run` (the cheap bar) fails while the block differs from what it measures |
| `docs/generated/VETO_B1_BACKTEST.md` | `Tools/veto_b1_backtest.pl` (§775, Elias's ruling E4; F3) | `perl Tools/veto_b1_backtest.pl` - the veto on the 10th Sejm's record: F3's widened base per president and the rates the game draws at (the same run writes the runtime table `Assets/Scripts/Elections/Generated/PolishVetoRates.cs`, which `PresidentialVetoDiagnostic` recomputes); B1 as first ruled - the confusion matrix, the hit rate, the precision, every veto it misses and every act it names that was not vetoed |

Each runs `Unity.exe -batchmode -nographics -projectPath <path> -executeMethod <above> -logFile <log>`.
All four moved here from the repository root on 2026-09-22 (`COMPLETED.md` §579) and their tools' default
output paths moved with them, so a regeneration writes here and not to the root.
