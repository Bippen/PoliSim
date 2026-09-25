using System;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-3a (§628): WHO GOVERNS, ASSERTED. At Sweden's start the government of record is Kristersson's M+KD+L with SD's support, INSTALLED,
    /// led by M (the record's own head); by party the four roles read S OPPOSITION, M PRIME MINISTER, KD and L JUNIOR PARTNER, SD SUPPORT; the
    /// one test `PlayerGoverns` is true for M alone (a junior partner's portfolios are PS-3's next part, stated); the AI finance ministry's
    /// gate follows it (a Sweden the player's S does not lead is AI-governed); the record survives a save round trip; Germany's start reads SPD PRIME MINISTER;
    /// Poland's and Italy's stand-ins are printed. PS-3b (§629): every start stores a record - the USA's president and party, France's cabinet under its president -
    /// and a country with none FAILS LOUDLY at AtStart and at PlayerGoverns (proved red on §628's build first: Reviews/evidence/2026-09-25_s629_probe_red.txt).
    /// </summary>
    public static class PlayerRoleDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== PlayerRoleDiagnostic (PS-3a, §628): who governs is the player's role ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using System.IDisposable epoch = SimulationManager.EpochScope();
            var go = new GameObject("PlayerRoleDiagnostic");
            try
            {
                WorldClock.ApplyStart(CountryId.Sweden);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country sweden = world.GetCountry(CountryId.Sweden);
                DateTime start = WorldClock.StartDate(CountryId.Sweden);

                Check(sweden.Government != null, "the factory stored Sweden's government of record at creation (§629)");
                Check(sim.PlayerGoverns(sweden), "no party seated: the player country is the instrument's hand - the Editor tools' case, stated (§629)");
                sweden.Government = GovernmentRecord.AtStart(sweden, start, world);
                GovernmentRecord g = sweden.Government;
                Check(!g.Provisional && g.PmParty == "M" && string.Join("+", g.Cabinet) == "M+KD+L" && string.Join("+", g.Support) == "SD",
                    F("Sweden's start: {0} led by {1}, support {2}, {3}", string.Join("+", g.Cabinet), g.PmParty, string.Join("+", g.Support), g.Provisional ? "PROVISIONAL" : "of record"));
                foreach ((string party, PlayerRole role, bool governs) in new[] { ("S", PlayerRole.Opposition, false), ("M", PlayerRole.PrimeMinister, true), ("KD", PlayerRole.JuniorPartner, false), ("L", PlayerRole.JuniorPartner, false), ("SD", PlayerRole.Support, false), ("V", PlayerRole.Opposition, false) })
                {
                    sweden.PlayerPartyAbbrev = party;
                    Check(g.RoleOf(party) == role && sim.PlayerGoverns(sweden) == governs, F("{0}: {1}, governs {2}", party, g.RoleOf(party), sim.PlayerGoverns(sweden)));
                }
                Check(g.RoleOf(null) == PlayerRole.None, "no party: no role");

                // The AI ministry's gate follows the role: with S seated, Sweden is AI-governed and the ministry writes on its turn.
                sweden.PlayerPartyAbbrev = "S";
                Check(!sim.PlayerGoverns(sweden), "with S seated the player does not govern Sweden - the AI ministry's gate opens for it");
                Country germany = world.GetCountry(CountryId.Germany);
                Check(!sim.PlayerGoverns(germany), "another country is never the player's to govern");
                // PS-3c (§630): THE ROLE GATE ON THE LEVERS - with S seated (opposition) every bill is refused with the reason; with M seated (the prime minister's party) it is introduced.
                bool refused = !sim.PlayerMayIntroduce(CountryId.Sweden, out string lockedBecause) && !sim.IntroduceLawBill(CountryId.Sweden, new LawBill { LawId = "cash_bail_reform_act", IsRepeal = false });
                Check(refused && lockedBecause == "IN OPPOSITION · THE GOVERNMENT INTRODUCES BILLS · YOUR PARTY VOTES ON THEM", F("S in opposition: a law bill is refused - {0}", lockedBecause ?? "(no reason)"));
                Check(sim.PlayerMayIntroduce(CountryId.Germany, out _), "another country's bills are its own government's - never refused here");
                sweden.PlayerPartyAbbrev = "M";
                Check(sim.PlayerMayIntroduce(CountryId.Sweden, out _) && sim.IntroduceLawBill(CountryId.Sweden, new LawBill { LawId = "cash_bail_reform_act", IsRepeal = false }), "M as the prime minister's party: the same bill is introduced");
                sweden.PlayerPartyAbbrev = "S";

                // The record survives the save round trip (format 28).
                SaveGame save = SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null);
                string json = SaveGameService.Serialize(save);
                SaveGame back = SaveGameService.Deserialize(json);
                Country loaded = back.World.GetCountry(CountryId.Sweden);
                Check(loaded.Government != null && loaded.Government.PmParty == "M" && string.Join("+", loaded.Government.Cabinet) == "M+KD+L" && loaded.Government.RoleOf("S") == PlayerRole.Opposition,
                    "the government rides the save (format 28) and reads the same role after the round trip");

                // The formation's record after an election: on the seated 2022 chamber with 2026's declarations the formation forms SD+M+KD+L (§607) - the record names it and its head.
                GovernmentRecord formed = GovernmentRecord.FromView(sweden, GovernmentFormation.ViewOf(sweden, ElectionVintage.Sweden2026), new DateTime(2026, 9, 13), world: world);
                Check(formed.Cabinet.Count > 0 && formed.PmParty == "M" && formed.RoleOf("S") == PlayerRole.Opposition && !formed.Provisional,
                    F("the formation's record: {0} led by {1} ({2}); S is {3}", string.Join("+", formed.Cabinet), formed.PmParty, formed.Outcome, formed.RoleOf("S")));

                // Germany's start: the government of record led by the chancellor's party.
                Country de = world.GetCountry(CountryId.Germany);
                GovernmentRecord gde = de.Government = GovernmentRecord.AtStart(de, WorldClock.StartDate(CountryId.Germany), world);
                Check(gde.PmParty == "SPD" && gde.Cabinet.Contains("SPD"), F("Germany's start: {0} led by {1}{2}", string.Join("+", gde.Cabinet), gde.PmParty, gde.Provisional ? " (provisional)" : string.Empty));
                Check(gde.Cabinet.Contains("FDP"), "6 November 2024 is the day BEFORE the FDP ministers were dismissed - the record's SPD+Grüne+FDP cabinet still stands (§618)");
                // The other starts, printed: the USA's president and France's cabinet under its president (§629), Poland's and Italy's the formation's stand-in.
                foreach (CountryId other in new[] { CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
                {
                    Country c = world.GetCountry(other);
                    c.Government = GovernmentRecord.AtStart(c, WorldClock.StartDate(other), world);
                    sb.Append("    ").Append(GovernmentRecord.Describe(other, WorldClock.StartDate(other), c)).Append(Environment.NewLine);
                }
                Check(world.GetCountry(CountryId.Poland).Government != null && world.GetCountry(CountryId.Poland).Government.Provisional && world.GetCountry(CountryId.Italy).Government != null && world.GetCountry(CountryId.Italy).Government.Provisional, "Poland and Italy: the formation's stand-in, marked provisional (§605)");

                // PS-3b (§629): THE EXECUTIVES, AND NO SILENT DEFAULT. Every playable start stores a government of record - the USA's is its president
                // and their party, France's its cabinet under its president - and a country with none FAILS LOUDLY: `AtStart` throws, and `PlayerGoverns`
                // throws on a country whose record was never stored, rather than reading the player as governing. Proved both ways: this block was
                // run against §628's build first (RED: the USA and France stored null and S/DEM/ENS governed by default), then against this one (GREEN).
                foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
                {
                    Country c = world.GetCountry(id);
                    Check(c.Government != null && !string.IsNullOrEmpty(c.Government.PmParty), F("{0}: a government of record is stored at its start, led by {1}", id, c.Government?.PmParty ?? "NONE"));
                }
                GovernmentRecord usa = world.GetCountry(CountryId.USA).Government;
                Check(usa != null && usa.PmParty == "DEM" && !usa.Provisional && usa.Kind == WorldClock.ExecutiveKind.Presidency && usa.Executive != null && usa.Executive.Contains("Biden"),
                    F("the USA's start ({0:yyyy-MM-dd}): the president and their party of record - {1}, {2}", WorldClock.StartDate(CountryId.USA), usa?.Executive ?? "NONE", usa?.PmParty ?? "-"));
                GovernmentRecord fr = world.GetCountry(CountryId.France).Government;
                Check(fr != null && fr.Provisional && fr.Kind == WorldClock.ExecutiveKind.Cabinet && fr.Executive != null && fr.Executive.Contains("Macron"),
                    F("France's start ({0:yyyy-MM-dd}): Attal's caretaker cabinet under {3} - its parties are GAP G4, so the formation's stand-in {1} led by {2} is PROVISIONAL", WorldClock.StartDate(CountryId.France), fr == null ? "NONE" : string.Join("+", fr.Cabinet), fr?.PmParty ?? "-", fr?.Executive ?? "no president"));
                bool threw = false;
                try { GovernmentRecord.AtStart(sweden, new DateTime(1900, 1, 1)); } catch (InvalidOperationException) { threw = true; }
                Check(threw, "a date with no government of record: AtStart THROWS rather than returning null");
                threw = false;
                sweden.Government = null;
                try { sim.PlayerGoverns(sweden); } catch (InvalidOperationException) { threw = true; }
                Check(threw, "a country whose government was never stored: PlayerGoverns THROWS rather than defaulting the player to governing");
                sweden.Government = g;

                // PS-3d (§631, ruled): France's governing mode is a WHAT-IF - the player's party governs on the 2024 Assembly of record; as an AI country France keeps the provisional stand-in.
                Country france = world.GetCountry(CountryId.France);
                Check(france.Government.Provisional && france.Government.Outcome != "what-if", "France as an AI country: the provisional stand-in stands");
                GovernmentRecord whatIf = GovernmentRecord.WhatIfGoverning(france, "UG", WorldClock.StartDate(CountryId.France), world);
                Check(whatIf.PmParty == "UG" && whatIf.RoleOf("UG") == PlayerRole.PrimeMinister && whatIf.RoleOf("RN") == PlayerRole.Opposition && !whatIf.Provisional && whatIf.Outcome == "what-if" && whatIf.Executive != null && whatIf.Executive.Contains("Macron"),
                    F("France's what-if: {0} governs under {1}; RN is {2}", whatIf.PmParty, whatIf.Executive, whatIf.RoleOf("RN")));
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { UnityEngine.Object.DestroyImmediate(go); EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"PLAYER ROLE: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
