# Review — §675 SP-3 part two, the campaign cast from the roster (2026-09-29)

Reviewed: the diff to `Assets/Scripts/Elections/LiveCampaignSetup.cs` and `Assets/Scripts/Simulation/SimulationManager.cs`
(`PlayerPartyIndexForCampaign`), with `Assets/Scripts/Data/CreatedParty.cs`, `PartySystem.cs` (the Swedish roster, `TryHistory`/`TryRealHistory`,
`For`/`RealRoster`), `SimulationManager.cs` 3150-3470 (the run-up and campaign day path, `RecordCampaignLedger`, `RestoreCampaign`),
`NationalElection.TryCompatibility`/`SharesFromCampaign`, `CampaignRun.cs` (day-0 offices and staff), `LiveCampaignSnapshot.PlayerPartyIndex`,
`VoteAttribution.Value`, the rail's CAMPAIGN cell, `SaveGameService`'s restore order, `CreatedPartyCampaignDiagnostic` and `Tools/bar_tier.ps1`'s
money definition; every remaining use of `SwedenParties`/`SwedenPersonalities`/`PlayerPartyIndexForCampaign` found by grep. One independent
reading, read-only. **Verdict READY.**

## Defects

None that fail. Latent only:

1. **`SimulationManager.cs:3313`** - the fallback `LiveCampaignSetup.SwedenParties[me]` still indexed the static 8-long array; taken only when
   `run.Setup.Parties == null`, never on any path read, but with a created player party (`me == 8`) it would throw. **Fixed**: the fallback reads
   `Keys(PlayerCountryId.Value)[me]`.
2. **`SimulationManager.cs:3321`** - `Keys()` allocates a new `string[]` per call, and `PlayerPartyIndexForCampaign` is called from the rail's
   CAMPAIGN cell every OnGUI pass in the run-up: GC churn, no correctness effect. Not taken (small; stated).

Caveat: the Regional sort matches `CreatedParty.Region` against the valkrets names; nothing sets it yet - SP-4 must store a valkrets name, or the
party falls back to the largest regions, silently.

## Confirmed sound, stated

- **Byte-identical with nothing registered.** `For(Sweden)` returns the same real array; its abbreviations are `SwedenParties` in order;
  `PersonalityOf` returns `SwedenPersonalities[p]`; `TryCreatedDayZero` returns false, so every day-0 value is the old operand in the old order;
  the polling houses are 8 long; the note prints "8"; no new code draws from any stream; the override maps by key over the same eight.
- **Lengths agree with a created party registered.** `Keys`, `TryHistory` (padded from the same list, same order) and `TryCompatibility` build
  from one roster - parties, prior, loyalty, compatibility, the override (8+N; the `Length ==` guard passes) and both houses are 8+N; a created
  party with no position gets override 0.0; `SharesFromCampaign` maps by key.
- **The player's index** agrees everywhere: `PlayerPartyIndexForCampaign` and `LiveCampaignSnapshot.PlayerPartyIndex` resolve over the same cast;
  a load registers created parties before `RestoreCampaign`, so a created player party resolves on replay.
- **Other countries**: `PlayerPartyIndexForCampaign` now returns an index ≥ 0 where it returned −1; `TryFor` still refuses every country but
  Sweden, so nothing runs there.
- **Day-0 logic**: the comparator is a valid total preorder (the Regional party's region first, then audience descending); `GetRange` cannot
  overrun; the pips clamp 1-5; zero offices are handled; a Splinter with a missing parent falls back to Grassroots.

**Money path: yes by the project's definition** (`SimulationManager.cs`), so the row is owed; in substance no - only campaign war chests inside
`CampaignRun`'s own pools move, `WarChest` is read nowhere outside `LiveCampaignSetup`, the fiscal book is untouched, and with nothing registered
every amount is unchanged.
