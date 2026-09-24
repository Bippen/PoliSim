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
    /// gate follows it (a Sweden the player's S does not lead is AI-governed); the record survives a save round trip; a country with no stored
    /// government reads as the player's; Germany's start reads SPD PRIME MINISTER; France and the USA, whose record names no cabinet, store no record (the player governs); Poland's and Italy's stand-ins are printed.
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

                Check(sim.PlayerGoverns(sweden) && sweden.Government == null, "no stored government: the player governs their own country, as before");
                sweden.Government = GovernmentRecord.AtStart(sweden, start);
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

                // The record survives the save round trip (format 27).
                SaveGame save = SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null);
                string json = SaveGameService.Serialize(save);
                SaveGame back = SaveGameService.Deserialize(json);
                Country loaded = back.World.GetCountry(CountryId.Sweden);
                Check(loaded.Government != null && loaded.Government.PmParty == "M" && string.Join("+", loaded.Government.Cabinet) == "M+KD+L" && loaded.Government.RoleOf("S") == PlayerRole.Opposition,
                    "the government rides the save (format 27) and reads the same role after the round trip");

                // The formation's record after an election: on the seated 2022 chamber with 2026's declarations the formation forms SD+M+KD+L (§607) - the record names it and its head.
                GovernmentRecord formed = GovernmentRecord.FromView(sweden, GovernmentFormation.ViewOf(sweden, ElectionVintage.Sweden2026), new DateTime(2026, 9, 13));
                Check(formed.Cabinet.Count > 0 && formed.PmParty == "M" && formed.RoleOf("S") == PlayerRole.Opposition && !formed.Provisional,
                    F("the formation's record: {0} led by {1} ({2}); S is {3}", string.Join("+", formed.Cabinet), formed.PmParty, formed.Outcome, formed.RoleOf("S")));

                // Germany's start: the government of record led by the chancellor's party.
                Country de = world.GetCountry(CountryId.Germany);
                GovernmentRecord gde = GovernmentRecord.AtStart(de, WorldClock.StartDate(CountryId.Germany));
                Check(gde.PmParty == "SPD" && gde.Cabinet.Contains("SPD"), F("Germany's start: {0} led by {1}{2}", string.Join("+", gde.Cabinet), gde.PmParty, gde.Provisional ? " (provisional)" : string.Empty));
                Check(gde.Cabinet.Contains("FDP"), "6 November 2024 is the day BEFORE the FDP ministers were dismissed - the record's SPD+Grüne+FDP cabinet still stands (§618)");
                // The other starts, printed so the stand-in is visible: France and the USA store NO record (their record names no cabinet), Poland and Italy the formation's stand-in.
                foreach (CountryId other in new[] { CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
                {
                    Country c = world.GetCountry(other);
                    c.Government = GovernmentRecord.AtStart(c, WorldClock.StartDate(other));
                    sb.Append("    ").Append(GovernmentRecord.Describe(other, WorldClock.StartDate(other), c)).Append(Environment.NewLine);
                }
                Check(world.GetCountry(CountryId.France).Government == null && world.GetCountry(CountryId.USA).Government == null, "France and the USA: no cabinet of record, no stored government - the player governs their own country");
                Check(world.GetCountry(CountryId.Poland).Government != null && world.GetCountry(CountryId.Poland).Government.Provisional && world.GetCountry(CountryId.Italy).Government != null && world.GetCountry(CountryId.Italy).Government.Provisional, "Poland and Italy: the formation's stand-in, marked provisional (§605)");
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
