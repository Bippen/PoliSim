using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// AF-30b (J5, ruled 2026-10-19): "the cabinet's seats start filled with fictional ministers at the average (Rule 9)" - so an idle governing player's
    /// health does not worsen from empty chairs; the symmetric factor stays as built (§862). On every start (the six countries' own, WorldClock.ApplyStart),
    /// the player's party the head's (France's the governing mode's what-if, as the game seats it), <see cref="CabinetSystem.SeatAverageCabinet"/> - what a
    /// new game calls - seats the average minister in every chair: the role's name (Rule 9), neutral on every term - efficiency x1 exactly (the pool's mean), loyalty, knowledge and
    /// popularity as written, the competence bias 0 (the review's M2), each checked against the unmodelled cabinet's reading. The health family's targets with those chairs equal an unmodelled cabinet's (the neutral point - the path
    /// the chairs do not move), and an empty chair's are never better (Sweden's strictly worse, as §862 measured). A second call seats none; a party in
    /// opposition none; a junior partner its own portfolios only, Finance excepted where the gate locks it (§755). No run: an election inside a run would
    /// re-form the government and change whose chairs they are (EffectivenessDiagnostic's AF-30 section reads the targets the same way).
    /// </summary>
    public static class AverageCabinetDiagnostic
    {
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== AverageCabinetDiagnostic (AF-30b, J5): every start's player cabinet seated at the average ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            try
            {
                CabinetPortfolio[] all = (CabinetPortfolio[])Enum.GetValues(typeof(CabinetPortfolio));
                const CabinetPortfolio health = CabinetPortfolio.HealthSocialAffairs;
                foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
                {
                    WorldClock.ApplyStart(id);
                    SimulationRandom.Seed(777);
                    EnergyMarket.ResetCalibration();
                    World world = WorldFactory.CreateDefault();
                    Country c = world.GetCountry(id);
                    string party = c.Government?.PmParty;
                    if (WorldClock.GoverningModeOnly(id) && party != null) { c.Government = GovernmentRecord.WhatIfGoverning(c, party, WorldClock.StartDate(id), world); }
                    if (string.IsNullOrEmpty(party)) { Check(false, F("{0}: the start's government names no head's party - nothing to seat", id)); continue; }
                    c.PlayerPartyAbbrev = party;
                    c.CabinetMinisters.Clear();
                    int seated = CabinetSystem.SeatAverageCabinet(c);

                    // the review's M2: NEUTRAL on every term a reader turns into an effect - efficiency x1 exactly (the pool's mean over itself), loyalty,
                    // knowledge and popularity as written (no event rolled, every estimate disclosed, approval as written), and the competence bias the three
                    // macro readers read (poverty, youth unemployment, crime - and Finance's collection) exactly the unmodelled cabinet's 0
                    int average = 0, unit = 0, neutral = 0, competence = 0;
                    foreach (CabinetPortfolio p in all)
                    {
                        if (!c.CabinetMinisters.TryGetValue(p, out CabinetMinister m) || m == null) { continue; }
                        if (m.Name == CabinetSystem.AverageMinisterName && m.Portfolio == p) { average++; }
                        if (CabinetSystem.EfficiencyScale(c, p) == 1f) { unit++; }
                        if (m.Loyalty == CabinetSystem.NeutralAttribute && m.Knowledge == CabinetSystem.NeutralAttribute && m.Popularity == CabinetSystem.NeutralAttribute
                            && m.Efficiency == CabinetSystem.PoolMeanEfficiency && CabinetSystem.PopularityFactor(c, p) == 1f && CabinetSystem.CanEstimateShocks(c, p)) { neutral++; }
                        float seatedBias = CabinetSystem.GetCompetenceBias(c, p);
                        c.CabinetMinisters.Remove(p);
                        float unmodelledBias = CabinetSystem.GetCompetenceBias(c, p);
                        c.CabinetMinisters[p] = m;
                        if (seatedBias == unmodelledBias && seatedBias == 0f) { competence++; }
                    }
                    Check(seated == all.Length && average == all.Length && unit == all.Length && neutral == all.Length && competence == all.Length,
                        F("{0} ({1:yyyy-MM-dd}), {2} the head's: {3} of {4} chairs seated - {5} the average minister by the role's name, {6} at efficiency x1 exactly, {7} neutral on loyalty, knowledge and popularity, {8} with the competence bias the readers read equal to the unmodelled cabinet's (0)",
                            id, WorldClock.StartDate(id), party, seated, all.Length, average, unit, neutral, competence));
                    Check(CabinetSystem.SeatAverageCabinet(c) == 0, F("{0}: a second call seats none - every chair is filled", id));

                    // the health path the chairs do not move: the targets with the average cabinet are an unmodelled cabinet's
                    float spending = HealthFamily.HealthSpendingReal(c);
                    HealthFamily.Targets withAverage = HealthFamily.TargetsFor(c, spending);
                    CabinetMinister kept = c.CabinetMinisters[health];
                    c.CabinetMinisters.Remove(health);
                    HealthFamily.Targets empty = HealthFamily.TargetsFor(c, spending);   // the player's chair, vacant: below the pool's floor (AF-30)
                    c.PlayerPartyAbbrev = null;
                    HealthFamily.Targets unmodelled = HealthFamily.TargetsFor(c, spending);   // no party, no minister: the cabinet not modelled, x1
                    c.PlayerPartyAbbrev = party;
                    c.CabinetMinisters[health] = kept;
                    bool same = Math.Abs(withAverage.WaitFactor - unmodelled.WaitFactor) < 1e-5f && Math.Abs(withAverage.TreatableMortality - unmodelled.TreatableMortality) < 1e-3f
                        && Math.Abs(withAverage.Coverage - unmodelled.Coverage) < 1e-4f;
                    bool notBetter = empty.WaitFactor >= withAverage.WaitFactor - 1e-6f && empty.TreatableMortality >= withAverage.TreatableMortality - 1e-6f;
                    bool strict = id != CountryId.Sweden || (empty.WaitFactor > withAverage.WaitFactor && empty.TreatableMortality > withAverage.TreatableMortality);
                    Check(same && notBetter && strict,
                        F("{0}: the health targets with the average cabinet - waits x{1:F4}, treatable mortality {2:F3} - are an unmodelled cabinet's (x{3:F4}, {4:F3}); an empty Health chair's x{5:F4}, {6:F3} are {7}",
                            id, withAverage.WaitFactor, withAverage.TreatableMortality, unmodelled.WaitFactor, unmodelled.TreatableMortality, empty.WaitFactor, empty.TreatableMortality,
                            id == CountryId.Sweden ? "worse (§862's measurement)" : "no better"));

                    if (id != CountryId.Sweden) { continue; }
                    // the review's M1: a portfolio with no authored pool (the economy portfolio) can always be refilled - its shortlist offers the average minister
                    List<CabinetMinister> economyShortlist = CabinetSystem.GenerateCandidates(CabinetPortfolio.Economy);
                    Check(!CabinetSystem.HasAuthoredPool(CabinetPortfolio.Economy) && economyShortlist.Count == 1 && economyShortlist[0].Name == CabinetSystem.AverageMinisterName && economyShortlist[0].Portfolio == CabinetPortfolio.Economy,
                        F("the economy portfolio has no named candidates: its shortlist offers {0} candidate(s), {1}", economyShortlist.Count, economyShortlist.Count > 0 ? economyShortlist[0].Name : "none"));
                    // the roles that seat fewer: the opposition none; a junior partner its own portfolios, Finance excepted where the gate locks it (§755)
                    c.CabinetMinisters.Clear();
                    string opposition = null;
                    foreach (KeyValuePair<string, int> seat in c.ParliamentSeats) { if (seat.Value > 0 && c.Government.RoleOf(seat.Key) == PlayerRole.Opposition) { opposition = seat.Key; break; } }
                    c.PlayerPartyAbbrev = opposition;
                    Check(opposition != null && CabinetSystem.SeatAverageCabinet(c) == 0 && c.CabinetMinisters.Count == 0, F("Sweden: {0} in opposition - no chair is its to fill, none seated", opposition ?? "no party"));
                    string partner = null;
                    foreach (string member in c.Government.Cabinet) { if (member != party) { partner = member; break; } }
                    if (partner == null) { Check(false, "Sweden's start cabinet has no junior partner to read"); continue; }
                    c.PlayerPartyAbbrev = partner;
                    int expected = 0;
                    foreach (CabinetPortfolio p in all) { if (CabinetSystem.ChairIsThePlayers(c, p)) { expected++; } }
                    int partnerSeated = CabinetSystem.SeatAverageCabinet(c);
                    Check(partnerSeated == expected && partnerSeated == c.CabinetMinisters.Count && expected > 0,
                        F("Sweden: {0} a junior partner holding {1} - {2} chair(s) its own, {3} seated", partner, c.Government.PortfoliosOf(partner), expected, partnerSeated));
                }
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { EnergyMarket.ResetTurnState(); }
            sb.Append(failures == 0 ? "=== AverageCabinetDiagnostic: every start seats the average cabinet ===" : F("=== AverageCabinetDiagnostic: {0} FAILED ===", failures));
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
