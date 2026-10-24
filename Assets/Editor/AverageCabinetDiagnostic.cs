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
    /// new game calls - seats the average minister in every chair: the role's name (Rule 9), at the pool's means (K58, ruled 2026-10-23) - efficiency x1 exactly, loyalty, knowledge and
    /// popularity the means of the drawn candidates (not 100, as before K58), the competence bias 0 (the review's M2), each checked against the unmodelled cabinet's reading. K59: an opposition party entering government later has the chairs it gains seated, and every install site in the manager seats them. The health family's targets with those chairs equal an unmodelled cabinet's (the neutral point - the path
    /// the chairs do not move), and an empty chair's are never better (Sweden's strictly worse, as §862 measured). A second call seats none; a party in
    /// opposition none; a junior partner its own portfolios only, Finance excepted where the gate locks it (§755). No run: an election inside a run would
    /// re-form the government and change whose chairs they are (EffectivenessDiagnostic's AF-30 section reads the targets the same way).
    /// </summary>
    public static class AverageCabinetDiagnostic
    {
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        /// <summary>Equal to the summation order's rounding (the runtime sums the table in its own order, this check the drawn candidates in theirs).</summary>
        private static bool Same(float a, float b) => Math.Abs(a - b) < 1e-3f;

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
                // K58: the pool's means read off the candidates the shortlists draw - every authored portfolio's whole pool, each name once - against the
                // runtime's derived means, and against the ruling's own rounded words (77, 79, 60)
                var names = new HashSet<string>();
                float loyaltySum = 0f, knowledgeSum = 0f, popularitySum = 0f;
                foreach (CabinetPortfolio p in all)
                {
                    if (!CabinetSystem.HasAuthoredPool(p)) { continue; }
                    foreach (CabinetMinister candidate in CabinetSystem.GenerateCandidates(p))
                    {
                        if (names.Add(candidate.Name)) { loyaltySum += candidate.Loyalty; knowledgeSum += candidate.Knowledge; popularitySum += candidate.Popularity; }
                    }
                }
                int pool = Math.Max(1, names.Count);
                (float Loyalty, float Knowledge, float Popularity) drawn = (loyaltySum / pool, knowledgeSum / pool, popularitySum / pool);
                Check(names.Count > 0 && Same(drawn.Loyalty, CabinetSystem.PoolMeanLoyalty) && Same(drawn.Knowledge, CabinetSystem.PoolMeanKnowledge) && Same(drawn.Popularity, CabinetSystem.PoolMeanPopularity)
                      && Math.Round(drawn.Loyalty) == 77 && Math.Round(drawn.Knowledge) == 79 && Math.Round(drawn.Popularity) == 60,
                    F("K58: the pool of {0} candidates averages loyalty {1:F2}, knowledge {2:F2}, popularity {3:F2} (the ruling's 77, 79, 60); the runtime's means {4:F2}, {5:F2}, {6:F2}",
                        names.Count, drawn.Loyalty, drawn.Knowledge, drawn.Popularity, CabinetSystem.PoolMeanLoyalty, CabinetSystem.PoolMeanKnowledge, CabinetSystem.PoolMeanPopularity));
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

                    // K58 (ruled 2026-10-23): AT THE POOL'S MEANS - efficiency x1 exactly (the pool's mean over itself), loyalty, knowledge and popularity the
                    // means of the candidates the shortlists draw (read off the drawn candidates here, never typed, so a mean the runtime derives wrongly fails),
                    // popularity's factor the mean over 100 and knowledge disclosing as the mean against the floor reads; and (the review's M2) the competence
                    // bias the three macro readers read (poverty, youth unemployment, crime - and Finance's collection) exactly the unmodelled cabinet's 0
                    int average = 0, unit = 0, neutral = 0, competence = 0;
                    foreach (CabinetPortfolio p in all)
                    {
                        if (!c.CabinetMinisters.TryGetValue(p, out CabinetMinister m) || m == null) { continue; }
                        if (m.Name == CabinetSystem.AverageMinisterName && m.Portfolio == p) { average++; }
                        if (CabinetSystem.EfficiencyScale(c, p) == 1f) { unit++; }
                        if (Same(m.Loyalty, drawn.Loyalty) && Same(m.Knowledge, drawn.Knowledge) && Same(m.Popularity, drawn.Popularity) && m.Loyalty < 100f && m.Popularity < 100f
                            && m.Efficiency == CabinetSystem.PoolMeanEfficiency && Same(CabinetSystem.PopularityFactor(c, p), drawn.Popularity / 100f)
                            && CabinetSystem.CanEstimateShocks(c, p) == (drawn.Knowledge >= CabinetSystem.KnowledgeDisclosureFloor)) { neutral++; }
                        float seatedBias = CabinetSystem.GetCompetenceBias(c, p);
                        c.CabinetMinisters.Remove(p);
                        float unmodelledBias = CabinetSystem.GetCompetenceBias(c, p);
                        c.CabinetMinisters[p] = m;
                        if (seatedBias == unmodelledBias && seatedBias == 0f) { competence++; }
                    }
                    Check(seated == all.Length && average == all.Length && unit == all.Length && neutral == all.Length && competence == all.Length,
                        F("{0} ({1:yyyy-MM-dd}), {2} the head's: {3} of {4} chairs seated - {5} the average minister by the role's name, {6} at efficiency x1 exactly, {7} at the pool's mean loyalty, knowledge and popularity (K58), {8} with the competence bias the readers read equal to the unmodelled cabinet's (0)",
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

                    // K59 (ruled 2026-10-23): "the average cabinet is also seated when the player enters government later". The opposition party, no chair
                    // its own, comes to lead a government (planted - the what-if record a governing mode seats): every chair it gains is seated at the average;
                    // a chair it already held and left vacant (a resignation) stays vacant through a change that does not hand it the chair anew
                    c.CabinetMinisters.Clear();
                    c.PlayerPartyAbbrev = opposition;
                    HashSet<CabinetPortfolio> before = CabinetSystem.PlayersChairs(c);
                    GovernmentRecord standing = c.Government;
                    c.Government = GovernmentRecord.WhatIfGoverning(c, opposition, WorldClock.StartDate(id), world);
                    int gained = 0;
                    foreach (CabinetPortfolio p in all) { if (!before.Contains(p) && CabinetSystem.ChairIsThePlayers(c, p)) { gained++; } }
                    int entered = CabinetSystem.SeatOnEntry(c, before);
                    int enteredAverage = 0;
                    foreach (CabinetPortfolio p in all) { if (c.CabinetMinisters.TryGetValue(p, out CabinetMinister m) && m != null && m.Name == CabinetSystem.AverageMinisterName && Same(m.Loyalty, CabinetSystem.PoolMeanLoyalty)) { enteredAverage++; } }
                    Check(before.Count == 0 && gained == all.Length && entered == gained && enteredAverage == gained,
                        F("K59: Sweden's {0}, in opposition with {1} chair(s), enters government ({2} leading): {3} chair(s) gained, {4} seated at the average", opposition, before.Count, c.Government.PmParty, gained, entered));
                    c.CabinetMinisters.Remove(health);   // a resignation's vacancy in a chair the player holds
                    HashSet<CabinetPortfolio> held = CabinetSystem.PlayersChairs(c);
                    c.Government = GovernmentRecord.WhatIfGoverning(c, opposition, WorldClock.StartDate(id).AddDays(1), world);   // a change that keeps every chair the player's
                    int reseated = CabinetSystem.SeatOnEntry(c, held);
                    Check(reseated == 0 && !c.CabinetMinisters.ContainsKey(health) && CabinetSystem.IsVacantChair(c, health),
                        F("K59: a government change that keeps the player's chairs seats none ({0}); the vacated Health chair stays vacant - the player's to fill", reseated));

                    // K87 (Elias, 2026-10-23): "an empty chair lands a decision card's approval at x0.6 like the average minister" - the vacant Health chair's
                    // factor is the average minister's (the pool's mean popularity over 100), and a planted option of +1.00 approval chosen there lands that
                    // much, through the path a chosen card takes (CabinetSystem.ApplyDecisionOption); the dismissal cost's factor keeps the vacancy at 1
                    float vacantFactor = CabinetSystem.DecisionApprovalFactor(c, health);
                    float averageFactor = CabinetSystem.DecisionApprovalFactor(c, CabinetPortfolio.Education);   // a chair the average minister holds
                    c.State.ApprovalRating = 50f;
                    float approvalBefore = c.State.ApprovalRating;
                    CabinetSystem.ApplyDecisionOption(c, health, new CabinetDecisionOption("K87 planted option", approvalEffect: 1f));
                    float landed = c.State.ApprovalRating - approvalBefore;
                    Check(Same(vacantFactor, CabinetSystem.PoolMeanPopularity / 100f) && Same(vacantFactor, averageFactor) && vacantFactor < 1f && Math.Abs(landed - vacantFactor) < 0.01f
                          && CabinetSystem.PopularityFactor(c, health) == 1f,
                        F("K87: the vacant Health chair's card factor x{0:F3} is the average minister's (x{1:F3}, the pool's mean popularity {2:F2} over 100); a planted +1.00 option chosen there lands {3:+0.000;-0.000}; the dismissal factor of a vacancy stays x{4:F0}",
                            vacantFactor, averageFactor, CabinetSystem.PoolMeanPopularity, landed, CabinetSystem.PopularityFactor(c, health)));
                    c.Government = standing;
                }

                // K59: every government the manager installs is bracketed by the seating - the chairs read before the write and seated after it - on every
                // `<x>.Government = ` the manager's partials hold (a new install site that does not seat the chairs it hands the player fails here, by line), and
                // every write of the player-presidency's office (the review's L2: its end hands the party's chairs back with no Government write)
                var unseated = new List<string>();
                int installs = 0;
                var write = new System.Text.RegularExpressions.Regex(@"\b\w+\.(?:Government|PlayerHoldsPresidency)\s*=(?!=)");
                foreach (string path in System.IO.Directory.GetFiles("Assets/Scripts/Simulation", "SimulationManager*.cs"))
                {
                    string[] lines = SourceText.ReadWithoutComments(path).Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (!write.IsMatch(lines[i])) { continue; }
                        installs++;
                        bool readBefore = false, seatedAfter = false;
                        for (int k = Math.Max(0, i - 3); k < i; k++) { readBefore |= lines[k].Contains("CabinetSystem.PlayersChairs("); }
                        for (int k = i + 1; k < Math.Min(lines.Length, i + 4); k++) { seatedAfter |= lines[k].Contains("CabinetSystem.SeatOnEntry("); }
                        if (!(readBefore && seatedAfter)) { unseated.Add(System.IO.Path.GetFileName(path) + ":" + (i + 1)); }
                    }
                }
                Check(installs > 0 && unseated.Count == 0, F("K59: {0} government install(s) and office change(s) in the manager's partials, each reading the player's chairs before and seating the gained ones after{1}",
                    installs, unseated.Count == 0 ? string.Empty : " - NOT at " + string.Join(", ", unseated)));
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
