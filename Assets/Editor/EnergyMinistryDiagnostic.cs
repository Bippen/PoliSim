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
        /// P6-F2e (§551; the third kind of source §575), asserted: a country's connection capacity holds what its SOURCE allows and no more, in the terms of the kind that source
        /// is. For every line of every sourced country - a build of the line's whole figure for the year stands, a megawatt more is refused in the source's own words, a retirement
        /// is never refused and takes no room, the line's technologies share one room, and the figures are the COUNTRY's (a second world's is empty whatever the first holds,
        /// §544's class). Then the two kinds part, and that parting is the point of this assertion: on an OPERATOR QUEUE a landed order FREES its megawatts, because the stock is
        /// what is in process; on a STATUTE a landed order frees NOTHING in its own year, because the volume was taken when it was awarded - and the next year's volume is whole,
        /// at its own figure. A statute's schedule is asserted too: the years ascend, CapMw is the last volume named, and the convention past the last year and before the first
        /// stands where the class note says it does. Where the capacity is billed (a technology its source has no line for) an order of any size stands.
        /// </summary>
        private static void QueueHoldsWhatIsPublished(StringBuilder sb, List<string> failures)
        {
            sb.Append("    the connection capacity (P6-F2e; the statute kind, §575):\n");
            foreach (CountryId id in new[] { CountryId.USA, CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland })
            {
                World a = WorldFactory.CreateDefault(), b = WorldFactory.CreateDefault();
                Country c = a.GetCountry(id), other = b.GetCountry(id);
                EnergyConnectionQueue.Published p = EnergyConnectionQueue.Of(id);
                if (p == null) { failures.Add(F("{0}: the catalog holds no entry - a covered country is either sourced or billed", id)); continue; }
                int year = c.CalendarYear;   // §575: the year an order placed here will carry - the one a statute's volume is counted in
                bool statute = p.Kind == EnergyConnectionQueue.SourceKind.Statute;
                sb.Append(F("    {0,-8} [{1}] {2}\n             {3}\n", id, p.Kind, EnergyConnectionQueue.CapacityText(c, year), EnergyConnectionQueue.SourceText(c)));
                if (p.BilledWhy != null)
                {
                    if (p.Lines.Length != 0) { failures.Add(F("{0}: billed and yet carries lines", id)); }
                    if (p.Kind != EnergyConnectionQueue.SourceKind.Billed) { failures.Add(F("{0}: billed and yet not of the billed kind - the page would name the wrong one", id)); }
                    if (EnergyFleet.CanOrder(id, 4) && EnergyFleet.Place(c, 4, 1e6, year, 0) == null) { failures.Add(F("{0}: the capacity is billed, so a terawatt of wind must stand - and it was refused", id)); }
                    continue;
                }
                string fullSaid = statute ? "THE YEAR'S TENDER VOLUME IS TAKEN" : "THE QUEUE IS FULL";
                string pastSaid = statute ? "THE ORDER IS PAST THE YEAR'S TENDER VOLUME" : "THE ORDER IS PAST THE QUEUE'S ROOM";
                foreach (EnergyConnectionQueue.Line line in p.Lines)
                {
                    double cap = line.CapMwIn(year);
                    if (!(cap > 0) || string.IsNullOrEmpty(line.Made)) { failures.Add(F("{0} {1}: a line without its figure or without the source's own cells it is made of", id, line.Name)); }
                    // §575: the schedule and the standing figure are ONE figure - CapMw is the last volume the statute names, and every reader goes through CapMwIn
                    if (statute)
                    {
                        if (line.Schedule == null || line.Schedule.Length == 0) { failures.Add(F("{0} {1}: a statute's line without a schedule of years", id, line.Name)); continue; }
                        for (int s = 0; s < line.Schedule.Length; s++)
                        {
                            EnergyConnectionQueue.Line.Volume v = line.Schedule[s];
                            if (v.Through < v.Year) { failures.Add(F("{0} {1}: a volume named from {2} through {3} - the statute cannot stop speaking before it starts", id, line.Name, v.Year, v.Through)); }
                            if (s > 0 && v.Year != line.Schedule[s - 1].Through + 1) { failures.Add(F("{0} {1}: the schedule gaps or overlaps at {2} - the one before it runs through {3}, and CapMwIn would read a year the statute did name as one it did not", id, line.Name, v.Year, line.Schedule[s - 1].Through)); }
                        }
                        int last = line.Schedule[line.Schedule.Length - 1].Through;   // the last year the STATUTE names, not the last the figure changes in
                        if (line.NamedThrough != last) { failures.Add(F("{0} {1}: NamedThrough reads {2} and the schedule's last year is {3}", id, line.Name, line.NamedThrough, last)); }
                        // §575: the convention is disclosed ON THIS LINE from the year after the statute stops naming it, and never in a year it names - the
                        // row-wide clause of the first cut stayed silent through 2029 while wind (named to 2028) already ran on it, and a 2029 film frame showed it
                        string mark = "(" + last + "'S VOLUME, BY CONVENTION)";
                        string inNamed = LineSegment(EnergyConnectionQueue.CapacityText(c, last), line.Name), pastNamed = LineSegment(EnergyConnectionQueue.CapacityText(c, last + 1), line.Name);
                        if (inNamed == null || inNamed.Contains("CONVENTION")) { failures.Add(F("{0} {1}: in {2}, a year the statute names, the page's line reads '{3}'", id, line.Name, last, inNamed ?? "nothing")); }
                        if (pastNamed == null || !pastNamed.EndsWith(mark, StringComparison.Ordinal)) { failures.Add(F("{0} {1}: in {2} the line runs on the convention and the page's line reads '{3}' - it must end {4}", id, line.Name, last + 1, pastNamed ?? "nothing", mark)); }
                        sb.Append(F("             {0}: in {1} the page reads '{2}'; in {3} '{4}'\n", line.Name, last, inNamed, last + 1, pastNamed));
                        if (Math.Abs(line.CapMwIn(last) - line.CapMw) > 1e-6) { failures.Add(F("{0} {1}: CapMw reads {2:0} and the last volume the statute names is {3:0} ({4}) - two figures for one line", id, line.Name, line.CapMw, line.CapMwIn(last), last)); }
                        if (Math.Abs(line.CapMwIn(last + 50) - line.CapMw) > 1e-6) { failures.Add(F("{0} {1}: past the last year the statute names, its last volume does not stand on", id, line.Name)); }
                        if (Math.Abs(line.CapMwIn(line.Schedule[0].Year - 1) - line.Schedule[0].Mw) > 1e-6) { failures.Add(F("{0} {1}: before the first year the statute names, its first volume does not stand", id, line.Name)); }
                        sb.Append(F("             {0}: the statute's schedule {1}, CapMwIn({2}) = {3:0} MW\n", line.Name, ScheduleText(line), year, cap));
                    }
                    else if (line.Schedule != null) { failures.Add(F("{0} {1}: an operator's line carries a schedule of years - one publisher, one date, one figure", id, line.Name)); }
                    int tech = -1; foreach (int t in line.Technologies) { if (EnergyFleet.CanOrder(id, t)) { tech = t; break; } }
                    if (tech < 0) { sb.Append(F("             {0}: no technology of the line can be ordered here - nothing to fill\n", line.Name)); continue; }
                    EnergyFleet.Order whole = EnergyFleet.Place(c, tech, cap, year, 0);
                    if (whole == null) { failures.Add(F("{0} {1}: the line's whole figure ({2:0} MW) was refused in an empty line", id, line.Name, cap)); continue; }
                    string why = EnergyFleet.CannotPlaceWhy(c, tech, 1.0, year);
                    if (EnergyFleet.Place(c, tech, 1.0, year, 0) != null || why == null || !why.StartsWith(fullSaid, StringComparison.Ordinal)) { failures.Add(F("{0} {1}: a megawatt past the figure stood, or was refused without the source's own sentence ({2})", id, line.Name, why ?? "no sentence")); }
                    foreach (int t in line.Technologies) { if (t != tech && EnergyFleet.CanOrder(id, t) && EnergyFleet.Place(c, t, 1.0, year, 0) != null) { failures.Add(F("{0} {1}: {2} shares the line and took a megawatt from a full one", id, line.Name, EnergyLayerData.Labels[t])); } }
                    if (EnergyFleet.CannotPlaceWhy(c, tech, -1.0, year) != null) { failures.Add(F("{0} {1}: a retirement was refused by a full line - a retirement is not a connection", id, line.Name)); }
                    EnergyFleet.Order leaves = EnergyFleet.Place(c, tech, -100.0, year, 0);   // really placed: a retirement stands in a full line, takes no room and GIVES none
                    if (leaves == null) { failures.Add(F("{0} {1}: a retirement of 100 MW could not be placed in a full line", id, line.Name)); }
                    if (Math.Abs(EnergyConnectionQueue.TakenMw(c, line, year) - cap) > 1e-6 || EnergyConnectionQueue.RoomMw(c, tech, year) > 1e-6) { failures.Add(F("{0} {1}: a retirement changed what stands against the line ({2:0} of {3:0} MW) - it gave room, or took it", id, line.Name, EnergyConnectionQueue.TakenMw(c, line, year), cap)); }
                    if (leaves != null) { EnergyFleet.Withdraw(c, leaves); }
                    if (Math.Abs(EnergyConnectionQueue.TakenMw(other, line, year)) > 1e-9 || EnergyFleet.CannotPlaceWhy(other, tech, 1.0, year) != null) { failures.Add(F("{0} {1}: the second world's line is not empty - the capacity is shared between worlds", id, line.Name)); }
                    if (EnergyConnectionQueue.FullText(c, tech, year) == null || EnergyConnectionQueue.StepUpMw(c, tech, EnergyFleet.StepMw(id), year) >= 1.0) { failures.Add(F("{0} {1}: the line is full and the page would still offer a step, or would not say so", id, line.Name)); }
                    // ⚠ HERE THE TWO KINDS PART (§575). An operator's queue is a stock: the landed order leaves it and its megawatts are the line's again, in the SAME year. A
                    // statute's volume is a flow: it was taken when the connection was awarded, so landing frees nothing in that year - and it is the NEXT year that is whole.
                    EnergyFleet.Advance(c, whole.OnlineTurn, whole.OnlineYear);
                    if (!whole.Landed) { failures.Add(F("{0} {1}: the order did not land at its own turn", id, line.Name)); }
                    int freeYear = statute ? year + 1 : year;
                    double freeCap = line.CapMwIn(freeYear);
                    if (statute)
                    {
                        if (EnergyFleet.CannotPlaceWhy(c, tech, 1.0, year) == null) { failures.Add(F("{0} {1}: the order landed and {2}'s volume was freed - a tender volume is taken when it is awarded, not when the plant connects", id, line.Name, year)); }
                        if (EnergyConnectionQueue.RefusalFor(c, tech, freeCap + 1.0, freeYear) == null) { failures.Add(F("{0} {1}: {2} took a megawatt more than the statute names for it", id, line.Name, freeYear)); }
                    }
                    if (EnergyFleet.CannotPlaceWhy(c, tech, freeCap, freeYear) != null) { failures.Add(F("{0} {1}: {2} is not whole ({3:0} MW refused)", id, line.Name, statute ? freeYear + "'s volume" : "the landed order's room", freeCap)); }
                    double offered = EnergyConnectionQueue.StepUpMw(c, tech, EnergyFleet.StepMw(id), freeYear);
                    if (offered > freeCap + 1e-6 || offered < 1.0 || EnergyFleet.CannotPlaceWhy(c, tech, offered, freeYear) != null) { failures.Add(F("{0} {1}: in a whole line the page offers a step of {2:0} MW that would be refused (the line holds {3:0})", id, line.Name, offered, freeCap)); }
                    // THE MID-STATE: half a step short of the figure, the page's step is exactly that half, it can be placed, and then the line is full; and an order past the room
                    // that is left says ROOM (or, of a statute, PAST THE YEAR'S VOLUME), not FULL (the F2e review: the sentence said FULL of an empty queue)
                    double stepMw = EnergyFleet.StepMw(id), half = Math.Floor(Math.Min(stepMw, freeCap) / 2.0);
                    EnergyFleet.Order most = half >= 1.0 ? EnergyFleet.Place(c, tech, freeCap - half, freeYear, 0) : null;
                    if (most != null)
                    {
                        double upMw = EnergyConnectionQueue.StepUpMw(c, tech, stepMw, freeYear);
                        string past = EnergyFleet.CannotPlaceWhy(c, tech, half + 1.0, freeYear);
                        if (Math.Abs(upMw - half) > 1e-6 || EnergyConnectionQueue.FullText(c, tech, freeYear) != null) { failures.Add(F("{0} {1}: {2:0} MW short of the figure the page offers a step of {3:0} MW, or already says the line is full", id, line.Name, half, upMw)); }
                        if (past == null || !past.StartsWith(pastSaid, StringComparison.Ordinal)) { failures.Add(F("{0} {1}: an order past the room that is left must say \"{2}\", and said: {3}", id, line.Name, pastSaid, past ?? "nothing")); }
                        if (EnergyFleet.Place(c, tech, upMw, freeYear, 0) == null || EnergyConnectionQueue.FullText(c, tech, freeYear) == null) { failures.Add(F("{0} {1}: the last step of {2:0} MW was refused, or did not fill the line", id, line.Name, upMw)); }
                    }
                    else { failures.Add(F("{0} {1}: the mid-state could not be staged", id, line.Name)); }
                    sb.Append(F("             {0}: {1:0} MW stood whole in {2}, a megawatt more refused - {3}; {4}; {5:0} MW short of {6}'s figure the step is {5:0} and fills it\n",
                        line.Name, cap, year, why, statute ? F("landed, {0}'s volume is still taken and {1} is whole", year, freeYear) : "landed, the room is the line's again", half, freeYear));
                }
                for (int t = 0; t < EnergyLayerData.Labels.Length; t++)
                {
                    int inLines = 0; foreach (EnergyConnectionQueue.Line line in p.Lines) { if (Array.IndexOf(line.Technologies, t) >= 0) { inLines++; } }
                    if (inLines > 1) { failures.Add(F("{0}: {1} stands in {2} lines - LineOf would take the first and TakenMw would count an order in each", id, EnergyLayerData.Labels[t], inLines)); }
                }
                foreach (int t in new[] { 0, 1, 2 })
                {
                    if (EnergyConnectionQueue.LineOf(id, t) == null && EnergyFleet.CanOrder(id, t))
                    {
                        if (EnergyFleet.Place(c, t, 1e6, year, 0) == null || EnergyConnectionQueue.NoLineText(id, t) == null) { failures.Add(F("{0} {1}: the source has no line for it, so its capacity is billed and any size must stand - with the page's note", id, EnergyLayerData.Labels[t])); }
                        sb.Append(F("             {0}: {1}\n", EnergyLayerData.Labels[t].ToUpperInvariant(), EnergyConnectionQueue.NoLineText(id, t)));
                    }
                }
            }
        }

        /// <summary>The one segment of a capacity line that belongs to the named line (the text between its separators) - null where the line is not on it (§575).</summary>
        private static string LineSegment(string capacityText, string lineName)
        {
            foreach (string part in (capacityText ?? string.Empty).Split(new[] { " · " }, StringSplitOptions.None)) { if (part.StartsWith(lineName + " ", StringComparison.Ordinal)) { return part; } }
            return null;
        }

        /// <summary>A statute line's schedule in the statute's own years, for the print: each year it names and the volume it names for it (§575).</summary>
        private static string ScheduleText(EnergyConnectionQueue.Line line)
        {
            var parts = new List<string>();
            foreach (EnergyConnectionQueue.Line.Volume v in line.Schedule) { parts.Add(F(v.Through > v.Year ? "{0}-{1} {2:0} MW" : "{0} {2:0} MW", v.Year, v.Through, v.Mw)); }
            return string.Join(", ", parts);
        }

        /// <summary>
        /// The ministry against a queue with little room. The review's finding was that no published queue bound the ministry on the measured forty years, so the clip and its
        /// deferral had never run; with France's wind line on the cells its publisher calls queued (16 934 MW) the forty years DO clip France, in 2027 and 2028 - but France's is a
        /// share mandate, which AssertMandates does not assert, so the dated excuse there still runs for no country. This stages the clip directly: France's wind line is filled to a thousand megawatts short of its
        /// figure and the ministry asked to decide: it must place exactly the thousand that fits, say what it deferred in the queue's own words, and leave the line full.
        /// </summary>
        /// <summary>How a build's deferral opens (AiEnergyMinistry.Place): the country, the year the ministry decided in, the label, a plus. ONE format for the excuse in
        /// <see cref="AssertMandates"/> - which no run reaches today: only capacity paths are asserted there, Germany's statutory volumes bind a YEAR (not a stock) and Italy's lines stand far above its statute's
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
            if (Math.Abs(EnergyConnectionQueue.TakenMw(fr, wind, 2026) - wind.CapMw) > 1e-6) { failures.Add(F("after the ministry's order France's wind line holds {0:0} of {1:0} MW - not full", EnergyConnectionQueue.TakenMw(fr, wind, 2026), wind.CapMw)); }
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
