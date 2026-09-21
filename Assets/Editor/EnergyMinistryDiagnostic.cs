using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Data.Generated;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **P6-F2d (2026-09-21, `COMPLETED.md` §544): the AI energy ministry's failure modes, MEASURED** - S11's own condition (*"its failure modes measured,
    /// not authored"*), met before the ministry is let onto the baseline. The ministry is HELD (`AiEnergyMinistry.Live` is false and the turn does not call
    /// it), so this check drives it itself: the baseline dump's world (seed 777, no player, no decision), and before every turn each of the six countries'
    /// ministries decides once (`AiEnergyMinistry.Decide`) - exactly the call the turn would make with the flag on. It prints, per country, what was ordered
    /// and why, what was deferred, where the fleet stands against its statute at the statute's own years, and what the orders do to the clearing - the blocks
    /// at the curtailment price, the curtailed MW, the wholesale price over the price level.
    ///
    /// <para><b>What it ASSERTS</b> is the rule, not the outcome: the hold stands; the two countries with nothing to answer (the USA: no statute; Poland:
    /// BILLED) place nothing; no fleet label goes below zero; every order carries its sentence; a capacity-path country reaches its statute's figure in the
    /// statute's year to within one order's rounding, or the run says which year it could first have (the lead time is the only thing that may stand in
    /// the way); a fossil-free or coal-exit country stands at zero by the year after the statute's, or carries a DEFERRED line naming the peak block. The
    /// outcomes it only MEASURES are the ones the flip will be ruled on.</para>
    /// </summary>
    public static class EnergyMinistryDiagnostic
    {
        /// <summary>CONVENTION - the statutes' horizon: the last point any of them names is 2045, and the run ends past it.</summary>
        private const int Years = 40;
        private static readonly int[] Checkpoints = { 2027, 2030, 2035, 2040, 2045, 2060 };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var failures = new List<string>();
            var sb = new StringBuilder("=== EnergyMinistryDiagnostic (P6-F2d): the AI energy ministry driven for forty years on the baseline's world - HELD in the game, measured here ===\n");
            if (AiEnergyMinistry.Live) { failures.Add("the hold is off - the ministry's family must be dumped and ruled before the turn calls it"); }

            FleetIsTheCountrys(sb, failures);
            QueueHoldsWhatIsPublished(sb, failures);
            MinistryOrdersWhatFits(sb, failures);

            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            int startYear = world.Countries[0].CalendarYear;
            var go = new GameObject("ENERGYMINISTRY");
            var placed = new Dictionary<CountryId, List<EnergyFleet.Order>>();
            var deferred = new Dictionary<CountryId, List<string>>();
            var firstOrders = new Dictionary<CountryId, string>();
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); placed[c.Id] = new List<EnergyFleet.Order>(); deferred[c.Id] = new List<string>(); }

                int next = 0;
                for (int turn = 1; turn <= Years; turn++)
                {
                    foreach (Country c in world.Countries)
                    {
                        AiEnergyMinistry.Decision d = AiEnergyMinistry.Decide(c, startYear + sim.CurrentTurn, sim.CurrentTurn);   // the year being played and its turn - the clock an order waits on (the calendar's year drifts across the 365-day boundary)
                        placed[c.Id].AddRange(d.Placed);
                        deferred[c.Id].AddRange(d.Deferred);
                        foreach (EnergyFleet.Order o in d.Placed)
                        {
                            if (string.IsNullOrEmpty(o.Reason)) { failures.Add(F("{0} {1}: an order without its sentence", c.Id, startYear + sim.CurrentTurn)); }
                            if (!firstOrders.ContainsKey(c.Id)) { firstOrders[c.Id] = F("{0} {1:+0;-0} MW placed {2}, lands {3} - {4}", EnergyLayerData.Labels[o.Technology].ToUpperInvariant(), o.Mw, o.OrderedYear, o.OnlineYear, o.Reason); }
                        }
                    }
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);

                    int calendar = startYear + sim.CurrentTurn;   // the year the fleet now serves - the boundary just passed landed what was due
                    while (next < Checkpoints.Length && Checkpoints[next] < calendar) { next++; }
                    if (next < Checkpoints.Length && Checkpoints[next] == calendar) { Snapshot(sb, world, calendar); next++; }
                    AssertMandates(world, calendar, deferred, failures);
                }

                // ---- the rule, asserted ---------------------------------------------------------------------------------------------
                sb.Append("\n--- per country: what was ordered, what was deferred, and the statute's years -----------------------------------------\n");
                foreach (Country c in world.Countries)
                {
                    AiEnergyMinistry.Mandate m = AiEnergyMinistry.MandateOf(c.Id);
                    double built = 0, retired = 0; foreach (EnergyFleet.Order o in placed[c.Id]) { if (o.Mw > 0) { built += o.Mw; } else { retired -= o.Mw; } }
                    sb.Append(F("{0,-8} {1} · {2}\n         {3} order(s): +{4:0} MW built, -{5:0} MW retired; {6} deferral(s){7}\n", c.Id, m != null ? m.Form.ToString() : "-", m != null ? m.Headline : "-",
                        placed[c.Id].Count, built, retired, deferred[c.Id].Count, firstOrders.TryGetValue(c.Id, out string first) ? "\n         first: " + first : ""));
                    for (int i = 0; i < Math.Min(3, deferred[c.Id].Count); i++) { sb.Append("         deferred: ").Append(deferred[c.Id][i]).Append('\n'); }
                    if (m != null && (m.Form == AiEnergyMinistry.MandateForm.None || m.Form == AiEnergyMinistry.MandateForm.Billed) && placed[c.Id].Count > 0) { failures.Add(F("{0} has nothing to answer ({1}) and placed {2} order(s)", c.Id, m.Form, placed[c.Id].Count)); }
                    for (int l = 0; l < EnergyLayerData.Labels.Length; l++) { double mw = EnergyLayer.RecordCapacityMw(c.Id, l) + EnergyFleet.LandedMw(c, l); if (mw < -1e-6) { failures.Add(F("{0}'s {1} fleet stands below zero: {2:0} MW", c.Id, EnergyLayerData.Labels[l], mw)); } }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                EnergyMarket.ResetTurnState();
            }

            foreach (string f in failures) { sb.Append("    ⚠ ").Append(f).Append('\n'); }
            sb.Append(failures.Count == 0 ? "    CLEAN - the hold stands, every order carries its sentence, the two with nothing to answer placed nothing, no fleet below zero." : F("    {0} FAILURE(S).", failures.Count));
            if (failures.Count == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures.Count == 0 ? 0 : 1);
        }

        /// <summary>
        /// §544's review, its first finding, as a standing assertion: THE FLEET IS THE COUNTRY'S. Two worlds at once - as the game holds the played world beside the
        /// shadow baseline's and the ledger's forks - an order landed in one, and every reader asked in both, before and after the other world's own boundary step:
        /// §539's static table let the second world rebuild the first's row to zero. And the queue's refusals: a fossil label whose plants the record does not hold
        /// (France's and Italy's coal), a nuclear fleet the law has closed (Germany's), and that a coal fleet ordered to zero takes its must-run with it.
        /// </summary>
        private static void FleetIsTheCountrys(StringBuilder sb, List<string> failures)
        {
            World a = WorldFactory.CreateDefault(), b = WorldFactory.CreateDefault();
            Country deA = a.GetCountry(CountryId.Germany), deB = b.GetCountry(CountryId.Germany);
            double record = EnergyLayer.RecordCapacityMw(CountryId.Germany, 4);
            EnergyFleet.Order wind = EnergyFleet.Place(deA, 4, 10000.0, 2026, 0);
            if (wind == null || wind.OnlineTurn != 3 || wind.OnlineYear != 2029) { failures.Add("a wind order placed in turn 0 of 2026 should land at the third boundary and serve from 2029"); }
            EnergyFleet.Advance(deA, 2, 2028);
            if (wind != null && wind.Landed) { failures.Add("the wind order landed a boundary early"); }
            EnergyFleet.Advance(deA, 3, 2029);
            if (wind != null && !wind.Landed) { failures.Add("the wind order did not land at its boundary"); }
            double mustRunA = EnergyMarket.Clear(deA).Zones[0][0].MustRunMw, mustRunB = EnergyMarket.Clear(deB).Zones[0][0].MustRunMw;
            EnergyFleet.Advance(deB, 3, 2029);   // the other world's own boundary - the step that zeroed the played world's row
            EnergyMarket.Clear(deB);
            double afterA = EnergyMarket.Clear(deA).Zones[0][0].MustRunMw;
            sb.Append(F("    the fleet is the country's: world A's Germany +10 000 MW of wind landed - fleet {0:0} MW (the record {1:0}), base-block must-run {2:0} MW against world B's {3:0}; after B's own boundary, A's {4:0}\n",
                EnergyFleet.CapacityMw(deA, 4), record, mustRunA, mustRunB, afterA));
            if (Math.Abs(EnergyFleet.CapacityMw(deA, 4) - (record + 10000.0)) > 1e-6) { failures.Add("world A's fleet does not carry its landed order"); }
            if (Math.Abs(EnergyFleet.CapacityMw(deB, 4) - record) > 1e-6) { failures.Add("world B's fleet carries world A's order"); }
            if (!(mustRunA > mustRunB)) { failures.Add("world A's clearing does not see its landed wind"); }
            if (Math.Abs(afterA - mustRunA) > 1e-6) { failures.Add("world B's boundary changed world A's clearing - the fleet is shared between worlds"); }
            if (Math.Abs(EnergyLayer.CapacityMw(CountryId.Germany, 4) - record) > 1e-6) { failures.Add("an id-keyed read outside any scope does not see the record"); }

            // the queue's refusals
            foreach ((CountryId id, int label, bool expected, string why) in new[] {
                (CountryId.France, 0, false, "France's coal sits in the record's multi-fuel groups"), (CountryId.Italy, 0, false, "Italy's coal sits in the record's multi-fuel groups"),
                (CountryId.Germany, 2, false, "Germany's nuclear is closed by law"), (CountryId.Sweden, 0, false, "Sweden's zones clear without a fossil fleet"), (CountryId.Sweden, 1, false, "Sweden's zones clear without a fossil fleet"), (CountryId.Germany, 0, true, "Germany's coal is the record's own"), (CountryId.Poland, 0, true, "Poland's coal is the record's own") })
            {
                bool can = EnergyFleet.CanOrder(id, label);
                sb.Append(F("    {0} {1}: {2}{3}\n", id, EnergyLayerData.Labels[label], can ? "orderable" : "refused - " + EnergyFleet.CannotOrderWhy(id, label), can == expected ? "" : "   <-- expected " + (expected ? "orderable" : "refused") + ": " + why));
                if (can != expected) { failures.Add(F("{0} {1}: {2}", id, EnergyLayerData.Labels[label], why)); }
            }

            // a coal fleet ordered to zero takes its must-run and its capacity with it
            EnergyFleet.Place(deA, 0, -1e9, 2026, 0);
            EnergyFleet.Advance(deA, 1, 2027);
            EnergyMarket.Result gone = EnergyMarket.Clear(deA);
            double coalMw = 0; foreach (EnergyMarket.BlockResult block in gone.Zones[0]) { coalMw += block.FossilMw[EnergyMarket.Coal]; }
            sb.Append(F("    Germany's coal ordered to zero: fleet {0:0} MW, coal dispatched over the three blocks {1:0} MW\n", EnergyFleet.CapacityMw(deA, 0), coalMw));
            if (EnergyFleet.CapacityMw(deA, 0) > 1e-6 || coalMw > 1e-6) { failures.Add("a coal fleet ordered to zero still runs"); }
        }

        /// <summary>
        /// P6-F2e (§551), asserted: the connection queue holds what the country's operator publishes and no more. For every line of every published queue - a build of the line's
        /// whole figure stands, a megawatt more is refused with the queue's own sentence, a retirement is never refused and takes no room, a landed order frees its megawatts, and the
        /// line's technologies share one room. Where the capacity is BILLED (Germany; a technology the published queue has no line for) an order of any size stands. And the
        /// figures are the COUNTRY's: a second world's queue is empty whatever the first one holds (§544's class).
        /// </summary>
        private static void QueueHoldsWhatIsPublished(StringBuilder sb, List<string> failures)
        {
            sb.Append("    the connection queue's capacity (P6-F2e):\n");
            foreach (CountryId id in new[] { CountryId.USA, CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland })
            {
                World a = WorldFactory.CreateDefault(), b = WorldFactory.CreateDefault();
                Country c = a.GetCountry(id), other = b.GetCountry(id);
                EnergyConnectionQueue.Published p = EnergyConnectionQueue.Of(id);
                if (p == null) { failures.Add(F("{0}: the catalog holds no entry - a covered country is either published or billed", id)); continue; }
                sb.Append(F("    {0,-8} {1}\n             {2}\n", id, EnergyConnectionQueue.CapacityText(c), EnergyConnectionQueue.SourceText(c)));
                if (p.BilledWhy != null)
                {
                    if (p.Lines.Length != 0) { failures.Add(F("{0}: billed and yet carries lines", id)); }
                    if (EnergyFleet.CanOrder(id, 4) && EnergyFleet.Place(c, 4, 1e6, 2026, 0) == null) { failures.Add(F("{0}: the capacity is billed, so a terawatt of wind must stand - and it was refused", id)); }
                    continue;
                }
                foreach (EnergyConnectionQueue.Line line in p.Lines)
                {
                    if (!(line.CapMw > 0) || string.IsNullOrEmpty(line.Made)) { failures.Add(F("{0} {1}: a line without its figure or without the publisher's cells it is made of", id, line.Name)); }
                    int tech = -1; foreach (int t in line.Technologies) { if (EnergyFleet.CanOrder(id, t)) { tech = t; break; } }
                    if (tech < 0) { sb.Append(F("             {0}: no technology of the line can be ordered here - nothing to fill\n", line.Name)); continue; }
                    EnergyFleet.Order whole = EnergyFleet.Place(c, tech, line.CapMw, 2026, 0);
                    if (whole == null) { failures.Add(F("{0} {1}: the line's whole figure ({2:0} MW) was refused in an empty queue", id, line.Name, line.CapMw)); continue; }
                    string why = EnergyFleet.CannotPlaceWhy(c, tech, 1.0);
                    if (EnergyFleet.Place(c, tech, 1.0, 2026, 0) != null || why == null || !why.StartsWith("THE QUEUE IS FULL", StringComparison.Ordinal)) { failures.Add(F("{0} {1}: a megawatt past the line's figure stood, or was refused without the queue's sentence ({2})", id, line.Name, why ?? "no sentence")); }
                    foreach (int t in line.Technologies) { if (t != tech && EnergyFleet.CanOrder(id, t) && EnergyFleet.Place(c, t, 1.0, 2026, 0) != null) { failures.Add(F("{0} {1}: {2} shares the line and took a megawatt from a full queue", id, line.Name, EnergyLayerData.Labels[t])); } }
                    if (EnergyFleet.CannotPlaceWhy(c, tech, -1.0) != null) { failures.Add(F("{0} {1}: a retirement was refused by a full queue - a retirement is not a connection", id, line.Name)); }
                    EnergyFleet.Order leaves = EnergyFleet.Place(c, tech, -100.0, 2026, 0);   // really placed: a retirement stands in a full line, takes no room and GIVES none
                    if (leaves == null) { failures.Add(F("{0} {1}: a retirement of 100 MW could not be placed in a full line", id, line.Name)); }
                    if (Math.Abs(EnergyConnectionQueue.StandingMw(c, line) - line.CapMw) > 1e-6 || EnergyConnectionQueue.RoomMw(c, tech) > 1e-6) { failures.Add(F("{0} {1}: a retirement in the queue changed what stands in the line ({2:0} of {3:0} MW) - it gave room, or took it", id, line.Name, EnergyConnectionQueue.StandingMw(c, line), line.CapMw)); }
                    if (leaves != null) { EnergyFleet.Withdraw(c, leaves); }
                    if (Math.Abs(EnergyConnectionQueue.StandingMw(other, line)) > 1e-9 || EnergyFleet.CannotPlaceWhy(other, tech, 1.0) != null) { failures.Add(F("{0} {1}: the second world's queue is not empty - the queue is shared between worlds", id, line.Name)); }
                    if (EnergyConnectionQueue.FullText(c, tech) == null || EnergyConnectionQueue.StepUpMw(c, tech, EnergyFleet.StepMw(id)) >= 1.0) { failures.Add(F("{0} {1}: the line is full and the page would still offer a step, or would not say the queue is full", id, line.Name)); }
                    EnergyFleet.Advance(c, whole.OnlineTurn, whole.OnlineYear);
                    double offered = EnergyConnectionQueue.StepUpMw(c, tech, EnergyFleet.StepMw(id));
                    if (offered > line.CapMw + 1e-6 || offered < 1.0 || EnergyFleet.CannotPlaceWhy(c, tech, offered) != null) { failures.Add(F("{0} {1}: in an empty line the page offers a step of {2:0} MW that the queue would refuse (the line holds {3:0})", id, line.Name, offered, line.CapMw)); }
                    if (!whole.Landed || EnergyFleet.CannotPlaceWhy(c, tech, line.CapMw) != null) { failures.Add(F("{0} {1}: the order landed and its megawatts were not freed", id, line.Name)); }
                    // THE MID-STATE: half a step short of the figure, the page's step is exactly that half, it can be placed, and then the line is full; and an order past the room that
                    // is left says ROOM, not FULL (the review: the sentence said FULL of an empty queue)
                    double stepMw = EnergyFleet.StepMw(id), half = Math.Floor(Math.Min(stepMw, line.CapMw) / 2.0);
                    EnergyFleet.Order most = half >= 1.0 ? EnergyFleet.Place(c, tech, line.CapMw - half, 2026, 0) : null;
                    if (most != null)
                    {
                        double upMw = EnergyConnectionQueue.StepUpMw(c, tech, stepMw);
                        string past = EnergyFleet.CannotPlaceWhy(c, tech, half + 1.0);
                        if (Math.Abs(upMw - half) > 1e-6 || EnergyConnectionQueue.FullText(c, tech) != null) { failures.Add(F("{0} {1}: {2:0} MW short of the figure the page offers a step of {3:0} MW, or already says FULL", id, line.Name, half, upMw)); }
                        if (past == null || !past.StartsWith("THE ORDER IS PAST THE QUEUE'S ROOM", StringComparison.Ordinal)) { failures.Add(F("{0} {1}: an order past the room that is left must say ROOM, and said: {2}", id, line.Name, past ?? "nothing")); }
                        if (EnergyFleet.Place(c, tech, upMw, 2026, 0) == null || EnergyConnectionQueue.FullText(c, tech) == null) { failures.Add(F("{0} {1}: the last step of {2:0} MW was refused, or did not fill the line", id, line.Name, upMw)); }
                    }
                    else { failures.Add(F("{0} {1}: the mid-state could not be staged", id, line.Name)); }
                    sb.Append(F("             {0}: {1:0} MW stood whole, a megawatt more was refused - {2}; landed, the room is the line's again; {3:0} MW short, the step is {3:0} and fills it\n", line.Name, line.CapMw, why, half));
                }
                for (int t = 0; t < EnergyLayerData.Labels.Length; t++)
                {
                    int inLines = 0; foreach (EnergyConnectionQueue.Line line in p.Lines) { if (Array.IndexOf(line.Technologies, t) >= 0) { inLines++; } }
                    if (inLines > 1) { failures.Add(F("{0}: {1} stands in {2} lines - LineOf would take the first and StandingMw would count an order in each", id, EnergyLayerData.Labels[t], inLines)); }
                }
                foreach (int t in new[] { 0, 1, 2 })
                {
                    if (EnergyConnectionQueue.LineOf(id, t) == null && EnergyFleet.CanOrder(id, t))
                    {
                        if (EnergyFleet.Place(c, t, 1e6, 2026, 0) == null || EnergyConnectionQueue.NoLineText(id, t) == null) { failures.Add(F("{0} {1}: the published queue has no line for it, so its capacity is billed and any size must stand - with the page's note", id, EnergyLayerData.Labels[t])); }
                        sb.Append(F("             {0}: no line in the published queue - {1}\n", EnergyLayerData.Labels[t].ToUpperInvariant(), EnergyConnectionQueue.NoLineText(id, t)));
                    }
                }
            }
        }

        /// <summary>
        /// The ministry against a queue with little room. The review's finding was that no published queue bound the ministry on the measured forty years, so the clip and its
        /// deferral had never run; with France's wind line on the cells its publisher calls queued (16 934 MW) the forty years DO clip France, in 2027 and 2028 - but France's is a
        /// share mandate, which AssertMandates does not assert, so the dated excuse there still runs for no country. This stages the clip directly: France's wind line is filled to a thousand megawatts short of its
        /// figure and the ministry asked to decide: it must place exactly the thousand that fits, say what it deferred in the queue's own words, and leave the line full.
        /// </summary>
        /// <summary>How a build's deferral opens (AiEnergyMinistry.Place): the country, the year the ministry decided in, the label, a plus. ONE format for the excuse in
        /// <see cref="AssertMandates"/> - which no run reaches today: only capacity paths are asserted there, Germany's queue is billed and Italy's lines stand far above its statute's
        /// figures - and for <see cref="MinistryOrdersWhatFits"/>, which does run it.</summary>
        private static string DeferralPrefix(CountryId id, int decidedInYear, int label) => F("{0} {1}: {2} +", id, decidedInYear, EnergyLayerData.Labels[label].ToUpperInvariant());

        private static void MinistryOrdersWhatFits(StringBuilder sb, List<string> failures)
        {
            World w = WorldFactory.CreateDefault();
            Country fr = w.GetCountry(CountryId.France);
            EnergyConnectionQueue.Line wind = EnergyConnectionQueue.LineOf(CountryId.France, 4);
            if (wind == null) { failures.Add("France's wind has no line - the ministry's clip cannot be exercised"); return; }
            if (EnergyFleet.Place(fr, 4, wind.CapMw - 1000.0, 2026, 0) == null) { failures.Add("France's wind line could not be filled to a thousand megawatts short"); return; }
            AiEnergyMinistry.Decision d = AiEnergyMinistry.Decide(fr, 2026, 0);
            EnergyFleet.Order placedWind = d.Placed.Find(o => o.Technology == 4);
            string deferral = d.Deferred.Find(x => x.StartsWith(DeferralPrefix(CountryId.France, 2026, 4), StringComparison.Ordinal));   // the excuse's own prefix - the one format, exercised here
            if (deferral == null && (placedWind == null || placedWind.Mw < 1000.0 - 1e-6)) { failures.Add("the staged room exceeds the ministry's first-year wind ask - the clip was NOT exercised (the staging's fault, not the rule's)"); return; }
            sb.Append(F("    the ministry against a queue with 1 000 MW of room: placed {0}; deferred: {1}\n", placedWind != null ? F("WIND +{0:0} MW", placedWind.Mw) : "no wind", deferral ?? "nothing"));
            if (placedWind == null || Math.Abs(placedWind.Mw - 1000.0) > 1e-6) { failures.Add("the ministry did not place exactly the thousand megawatts of wind the queue had room for"); }
            if (deferral == null || !deferral.Contains("THE CONNECTION QUEUE HAS ROOM FOR 1000 MW") || !deferral.Contains("THE ORDER IS PAST THE QUEUE'S ROOM")) { failures.Add("the ministry's deferral does not say what the queue had room for, in the queue's own words"); }
            if (Math.Abs(EnergyConnectionQueue.StandingMw(fr, wind) - wind.CapMw) > 1e-6) { failures.Add(F("after the ministry's order France's wind line holds {0:0} of {1:0} MW - not full", EnergyConnectionQueue.StandingMw(fr, wind), wind.CapMw)); }
            AiEnergyMinistry.Decision again = AiEnergyMinistry.Decide(fr, 2026, 0);
            if (again.Placed.Exists(o => o.Technology == 4) || !again.Deferred.Exists(x => x.StartsWith(DeferralPrefix(CountryId.France, 2026, 4), StringComparison.Ordinal) && x.Contains("THE QUEUE IS FULL"))) { failures.Add("asked again with the line full, the ministry placed wind, or did not say the queue is full"); }
        }

        /// <summary>The rule's own years: a capacity path's figure stands in the statute's year to within one per cent (from the first year the lead time allows), and a
        /// coal exit or a fossil-free goal stands at zero the year after the statute's - or the country carries a DEFERRED line, which is the veto saying why.</summary>
        private static void AssertMandates(World world, int calendar, Dictionary<CountryId, List<string>> deferred, List<string> failures)
        {
            foreach (Country c in world.Countries)
            {
                AiEnergyMinistry.Mandate m = AiEnergyMinistry.MandateOf(c.Id);
                if (m == null) { continue; }
                if (m.Form == AiEnergyMinistry.MandateForm.CapacityPath)
                {
                    foreach ((int label, (int Year, double Gw)[] path) in new[] { (4, m.WindGw), (5, m.SolarGw) })
                    {
                        if (path == null) { continue; }
                        foreach ((int year, double gw) in path)
                        {
                            if (year != calendar) { continue; }
                            double fleet = EnergyFleet.CapacityMw(c, label) / 1000.0;
                            // P6-F2e: the queue's capacity is the ministry's excuse, in the queue's own words - and DATED: only a deferral placed the lead time before this statute year
                            // speaks for it (the review: an undated excuse from 2026 would have waved through 2045)
                            string excuse = DeferralPrefix(c.Id, year - EnergyFleet.LeadTimeYears[label], label);
                            if (deferred[c.Id].Exists(d => d.StartsWith(excuse, StringComparison.Ordinal) && d.Contains("THE CONNECTION QUEUE HAS ROOM"))) { continue; }
                            if (Math.Abs(fleet - gw) > gw * 0.01 + AiEnergyMinistry.MinOrderMw / 1000.0) { failures.Add(F("{0}: the statute names {1:0.0} GW of {2} in {3}; the fleet stands at {4:0.0}", c.Id, gw, EnergyLayerData.Labels[label], year, fleet)); }
                        }
                    }
                    if (m.CoalFraction != null && calendar == m.CoalFraction[m.CoalFraction.Length - 1].Year + 1 && EnergyFleet.CapacityMw(c, 0) > AiEnergyMinistry.MinOrderMw && !deferred[c.Id].Exists(d => d.Contains(": COAL -")))
                    { failures.Add(F("{0}: coal stands at {1:0} MW the year after its statute's end and no deferral names coal", c.Id, EnergyFleet.CapacityMw(c, 0))); }
                }
                else if (m.Form == AiEnergyMinistry.MandateForm.FossilFree && calendar == m.FossilFreeYear + 1)
                {
                    foreach ((int label, string name) in new[] { (0, "COAL"), (1, "GAS") })
                    {
                        if (!EnergyFleet.CanOrder(c.Id, label)) { continue; }   // a label the queue refuses is one the goal cannot reach - the refusal is asserted with the others above
                        if (EnergyFleet.CapacityMw(c, label) > 1e-6 && !deferred[c.Id].Exists(d => d.Contains(": " + name + " -")))
                        { failures.Add(F("{0}: {1} stands at {2:0} MW the year after the goal's and no veto deferral names it", c.Id, name.ToLowerInvariant(), EnergyFleet.CapacityMw(c, label))); }
                    }
                }
            }
        }

        private static void Snapshot(StringBuilder sb, World world, int calendar)
        {
            sb.Append(F("\n--- {0} -----------------------------------------------------------------------------------------------\n", calendar));
            EnergyMarket.BeginTurn(world);
            try
            {
                foreach (Country c in world.Countries)
                {
                    if (!EnergyLayer.Has(c.Id)) { continue; }
                    AiEnergyMinistry.Mandate m = AiEnergyMinistry.MandateOf(c.Id);
                    double p = Math.Max(0.0001f, c.State.PriceLevel);
                    EnergyMarket.Result r = EnergyMarket.Clear(c);
                    int blocks = 0, atFloor = 0; double curtailed = 0, unserved = 0, priceSum = 0;
                    if (r.Zones != null) { foreach (EnergyMarket.BlockResult[] zone in r.Zones) { foreach (EnergyMarket.BlockResult b in zone) { if (b == null) { continue; } blocks++; priceSum += b.Price / p; curtailed += b.CurtailedMw; unserved += b.UnservedMw; if (b.CurtailedMw > 0) { atFloor++; } } } }
                    string target = "";
                    if (m != null && m.Form == AiEnergyMinistry.MandateForm.CapacityPath)
                    {
                        target = F(" · statute: wind {0:0.0} solar {1:0.0} GW{2}", AiEnergyMinistry.PathAt(m.WindGw, calendar, EnergyLayer.RecordCapacityMw(c.Id, 4) / 1000.0), AiEnergyMinistry.PathAt(m.SolarGw, calendar, EnergyLayer.RecordCapacityMw(c.Id, 5) / 1000.0),
                            m.CoalFraction != null ? F(", coal {0:0.0} GW", AiEnergyMinistry.PathAt(m.CoalFraction, calendar, 1.0) * EnergyLayer.RecordCapacityMw(c.Id, 0) / 1000.0) : "");
                    }
                    else if (m != null && m.Form == AiEnergyMinistry.MandateForm.RenewableShare) { target = F(" · statute: {0:0.#} % in {1}; the fleet implies {2:0.0} %", m.RenewableSharePercent, m.ShareYear, AiEnergyMinistry.RenewableSharePercent(c)); }
                    else if (m != null && m.Form == AiEnergyMinistry.MandateForm.FossilFree) { target = F(" · statute: coal and gas at zero by {0}", m.FossilFreeYear); }
                    sb.Append(F("{0,-8} fleet GW: coal {1:0.0} gas {2:0.0} nuclear {3:0.0} wind {4:0.0} solar {5:0.0}{6}\n         clearing: mean block price/P {7:0.0} · {8} of {9} block(s) curtailing, {10:0} MW curtailed · {11:0} MW unserved · household real {12:G4}\n",
                        c.Id, EnergyFleet.CapacityMw(c, 0) / 1000.0, EnergyFleet.CapacityMw(c, 1) / 1000.0, EnergyFleet.CapacityMw(c, 2) / 1000.0, EnergyFleet.CapacityMw(c, 4) / 1000.0, EnergyFleet.CapacityMw(c, 5) / 1000.0, target,
                        blocks > 0 ? priceSum / blocks : 0.0, atFloor, blocks, curtailed, unserved, c.State.EnergyHouseholdPriceReal));
                }
            }
            finally { EnergyMarket.EndTurn(); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
