using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-16 (`docs/specs/USA_STAGE_PLAN.md`, "The House and a Senate class elected on the same day" - checkpoint 2): THE CONGRESS-ELECTION DIAGNOSTIC.
    /// The plan's done-when: "a fresh world on the eve of 5 Nov 2024 seats a 119th whose misses against the record are written into the card; 435 and 100 sum
    /// after every election; a save across the count loads into the game".
    /// <list type="bullet">
    /// <item>(a) the rulings it runs on, asserted as R-US18's and R-US19's blocks ask ("US-16's Congress-election diagnostic asserts R-US18 RULED, with an option
    /// that can be built" - and R-US19 the same, "a current digest and a V basis equal to R-US15's ruling. While not, it fails"): each instrument's ruling record
    /// RULED, its option one its block can build - (b), and (c) over (a) - each block's digest the last of its instrument's list; V the own-line House vote (R-US15
    /// (a), G2);</item>
    /// <item>(b) the House count is the rule R-US18 was measured on: at the record's own 2024 swing from the record's 2022 it gives the national REP seats US-11's
    /// instrument prints for C4's (b) row on the card (read from the card's generated block, never typed);</item>
    /// <item>(c) the Senate count at 2024's true national vote reproduces 2024's class by construction (R-US19 (c)'s factors, "in-sample");</item>
    /// <item>(d) the eve world - epoch 4 Nov 2024, seed 777, the USA the player's country (REP), stepped in the controller's order - holds its own count on 5 Nov
    /// 2024 once: the House by state and the Senate class, seated on 3 Jan 2025 and not before, the House 435 and the Senate 100; the record seats no House from
    /// then in this world (the country's gate unchanged); the Twelfth Amendment's delegations on 6 Jan read the House the game elected; "divided" read from the
    /// game's own chambers; the misses against the record written into the card;</item>
    /// <item>(e) a save cut the day after the count and one cut after the seating, each loaded INTO THE GAME (the controller's own `RestoreFromSave`, §557) and
    /// stepped a day: the contests ride whole, nothing is counted twice, the seated chambers kept; a save of the format before is refused;</item>
    /// <item>(f) every later election - 2028, 2032 and 2036, on planted even polls, each on the game's own previous count - seats 435 and 100;</item>
    /// <item>(g) US-8c (Elias's ruling J4): the tie draw is a fair coin keyed on the seed, the year and the place - each way over seeds, the same for the same seed.</item>
    /// </list>
    /// Pinned in the cheap bar and the documents bar through the US card's US-16 block (`docs/reference/US_ELECTIONS.md`), written by <see cref="WriteReadings"/>;
    /// a stale block fails.
    /// </summary>
    public static class CongressElectionDiagnostic
    {
        private static readonly DateTime Eve = new DateTime(2024, 11, 4);
        private static readonly DateTime Polled = new DateTime(2024, 11, 5);
        private static readonly DateTime HouseDay = new DateTime(2025, 1, 3);
        private static readonly DateTime CountedOn = new DateTime(2025, 1, 6);
        private static readonly DateTime Settled = new DateTime(2025, 1, 21);   // every 2024 winner sworn (West Virginia's on 14 Jan, the Class I page's date) - the truth's holders
        private const int Seed = 777;
        private const string PlayerParty = "REP";

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CongressElectionDiagnostic (PS-6 US-16): the House and a Senate class elected on the same day - checkpoint 2 ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string expected = CardBlock(card, digest);
                string onDisk = CardBlockOf(File.ReadAllText(PathOf(CardRelative), Encoding.UTF8));
                Check(onDisk == expected, onDisk == null ? F("the model card ({0}) carries no US-16 block", CardRelative)
                    : onDisk == expected ? F("the model card's US-16 block ({0}) says what this measures - {1} lines", CardRelative, card.Count)
                    : F("the model card's US-16 block ({0}) is STALE - regenerate: -executeMethod PoliSim.EditorTools.CongressElectionDiagnostic.WriteReadings", CardRelative));
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== CongressElectionDiagnostic: the game elects its Congress ===" : F("=== CongressElectionDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Writes the card's US-16 block from this reading - the stamp and its END marker placed once by hand; refuses while any check fails.</summary>
        public static void WriteReadings()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CongressElectionDiagnostic.WriteReadings (PS-6 US-16): the US model card's US-16 block ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string path = PathOf(CardRelative);
                string text = File.ReadAllText(path, Encoding.UTF8);
                string old = CardBlockOf(text);
                if (failures > 0) { Check(false, "a check above failed - the card is not written; fix what failed, then write"); }
                else if (old == null) { Check(false, F("the model card ({0}) carries no US-16 block to write into", CardRelative)); }
                else
                {
                    string written = text.Replace("\r\n", "\n").Replace(old, CardBlock(card, digest));
                    if (text.Contains("\r\n")) { written = written.Replace("\n", "\r\n"); }
                    File.WriteAllText(path, written, new UTF8Encoding(false));
                    Check(true, F("the model card's US-16 block written ({0}) - {1} lines", CardRelative, card.Count));
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== WriteReadings: written ===" : F("=== WriteReadings: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>The reading <see cref="Run"/> checks and <see cref="WriteReadings"/> writes - the random streams, the energy state and the regional derivation
        /// put back after, the epoch by its scope, every host destroyed.</summary>
        private static string Measure(StringBuilder sb, Action<bool, string> Check, List<string> card)
        {
            var hosts = new List<GameObject>();
            string dir = Path.Combine(Application.temporaryCachePath, "us_congress_" + Guid.NewGuid().ToString("N"));
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            double[][] regionalShares = NationalElection.LastRegionalShares;
            string[] regionalNames = NationalElection.LastRegionalNames;
            double[] regionalWeights = NationalElection.LastRegionalWeights;
            double regionalWorst = NationalElection.LastRegionalWorstAbsError;
            IReadOnlyList<string> regionalKeys = NationalElection.LastRegionalKeys;
            try
            {
                Directory.CreateDirectory(dir);
                using (SimulationManager.EpochScope()) { return MeasureAll(sb, Check, card, hosts, dir); }
            }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                EnergyMarket.ResetTurnState();
                SimulationRandom.RestoreState(seedWas, drawsWas);
                void Set(string property, object value) =>
                    typeof(NationalElection).GetProperty(property, BindingFlags.Public | BindingFlags.Static).GetSetMethod(true).Invoke(null, new[] { value });
                Set(nameof(NationalElection.LastRegionalShares), regionalShares);
                Set(nameof(NationalElection.LastRegionalNames), regionalNames);
                Set(nameof(NationalElection.LastRegionalWeights), regionalWeights);
                Set(nameof(NationalElection.LastRegionalWorstAbsError), regionalWorst);
                Set(nameof(NationalElection.LastRegionalKeys), regionalKeys);
                try { if (Directory.Exists(dir)) { Directory.Delete(dir, true); } } catch (Exception e) { sb.Append("    note      the temporary folder stays: ").Append(e.Message).Append('\n'); }
            }
        }

        private static string MeasureAll(StringBuilder sb, Action<bool, string> Check, List<string> card, List<GameObject> hosts, string dir)
        {
            string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string Long(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
            string plan = File.ReadAllText(PathOf("docs/specs/USA_STAGE_PLAN.md"), Encoding.UTF8).Replace("\r\n", "\n");
            string cardText = File.ReadAllText(PathOf(CardRelative), Encoding.UTF8).Replace("\r\n", "\n");

            // ---- (a) the rulings: RULED, an option each block can build, each block's digest the last of its list; V the own-line House vote
            string houseBlock = Block(plan, "R-US18"), senateBlock = Block(plan, "R-US19");
            Check(UsHouseCountCheck.RulingState == "RULED" && UsHouseCountCheck.RulingOption == "(b)" && Array.IndexOf(UsHouseCountCheck.BuildableOptions, UsHouseCountCheck.RulingOption) >= 0
                  && houseBlock != null && Digest(houseBlock) == UsHouseCountCheck.RuleDigests[UsHouseCountCheck.RuleDigests.Length - 1].Digest,
                F("(a) R-US18 RULED, option {0} - one its block can build; the block's digest the last of the instrument's list ({1})", UsHouseCountCheck.RulingOption, houseBlock == null ? "the block NOT FOUND" : Digest(houseBlock).Substring(0, 8)));
            Check(UsSenateRaceCheck.RulingState == "RULED" && UsSenateRaceCheck.RulingOption == "(c) over (a)" && Array.IndexOf(UsSenateRaceCheck.BuildableOptions, UsSenateRaceCheck.RulingOption) >= 0
                  && senateBlock != null && Digest(senateBlock) == UsSenateRaceCheck.RuleDigests[UsSenateRaceCheck.RuleDigests.Length - 1].Digest,
                F("(a) R-US19 RULED, option {0} - one its block can build; the block's digest the last of the instrument's list ({1})", UsSenateRaceCheck.RulingOption, senateBlock == null ? "the block NOT FOUND" : Digest(senateBlock).Substring(0, 8)));
            long ownR = UsPresidentialReturns.House.Where(h => h.Year == 2022).Sum(h => h.VotesR), ownD = UsPresidentialReturns.House.Where(h => h.Year == 2022).Sum(h => h.VotesD);
            Check(plan.Contains("R-US15 - RULED (a) (G2, §797): V in play is the own-line House vote") && UsCongressElection.RecordV(2022) == (double)ownR / (ownR + ownD),
                F("(a) V's basis is R-US15's ruling - the own-line House vote (house_by_state.csv): V(2022) {0:0.000000}", UsCongressElection.RecordV(2022)));

            // ---- (b) the House count is R-US18's (b): at the record's own 2024 swing from 2022 it gives US-11's C4 (b) row on this card
            List<UsCongressElection.StateHouse> atRecord = UsCongressElection.CountHouse(Polled, UsCongressElection.RecordV(2024), null, out int base2022, out double v2022, out bool _, out string whyRecord);
            int c4 = CardC4B(cardText);
            int atRecordRep = atRecord?.Sum(s => s.Rep) ?? -1;
            Check(atRecord != null && base2022 == 2022 && c4 > 0 && atRecordRep == c4 && atRecord.Sum(s => s.Seats) == UsCongressElection.HouseMembers,
                F("(b) at the record's own swing (V 2024 {0:0.0000} against 2022's {1:0.0000}) the count gives REP {2} of {3} - US-11's C4 (b) row on this card reads REP {4}{5}",
                    UsCongressElection.RecordV(2024), v2022, atRecordRep, atRecord?.Sum(s => s.Seats) ?? 0, c4, atRecord == null ? " - the count failed: " + whyRecord : string.Empty));

            // ---- (c) the Senate count at the true 2024 national vote reproduces 2024's class (R-US19 (c)'s factors, in-sample by construction)
            double[] national = National2024();
            ElectoralCollege.FromNationalShares(2020, 2024, national, out ElectoralCollege.StateCall[] trueNight, out double _);
            List<UsCongressElection.SenateSeat> atTrue = UsCongressElection.CountSenate(null, Polled, trueNight, 0, out string whySenate);
            Dictionary<(string, int), string> truth = SenateTruth();
            List<string> trueMisses = atTrue == null ? new List<string> { "the count failed: " + whySenate } : atTrue.Where(s => !truth.TryGetValue((s.State, s.Class), out string side) || side != s.Side).Select(s => s.State + "-" + s.Class).ToList();
            List<double> factors = atTrue?.Where(s => s.Factor != 0.0).Select(s => Math.Abs(s.Factor)).ToList() ?? new List<double>();
            Check(atTrue != null && atTrue.Count == truth.Count && trueMisses.Count == 0 && UsCongressElection.ClassUp(2024) == 1,
                F("(c) at 2024's true national vote the Senate count reproduces the class by construction: {0} seats (class {1} and the specials of the day), {2} called wrong; |c_j| over {3} races mean {4:0.000}, largest {5:0.000}",
                    atTrue?.Count ?? 0, UsCongressElection.ClassUp(2024), trueMisses.Count == 0 ? "none" : string.Join(", ", trueMisses), factors.Count, factors.Count > 0 ? factors.Average() : 0.0, factors.Count > 0 ? factors.Max() : 0.0));

            // ---- (d) the eve world: its own count, seated on 3 January
            string saveCount = Path.Combine(dir, "after_count.json"), saveSeated = Path.Combine(dir, "after_seating.json");
            var logged = new List<string>();
            void Listen(string condition, string stack, LogType type) { if (condition != null && condition.StartsWith("CONGRESS: USA", StringComparison.Ordinal)) { logged.Add(condition); } }
            UsCongressElection.Contest held = null;
            UsPresidentialElection.Contest president = null;
            Dictionary<string, int> houseOnDay = null, senateOnDay = null;
            string government = "not read";
            {
                (SimulationManager sim, World w, Country usa) = Open(hosts, Eve, PlayerParty);
                Dictionary<string, int> house118 = new Dictionary<string, int>(usa.ParliamentSeats);
                object seatsRef = usa.ParliamentSeats;
                int seatChanges = 0;
                DateTime seatOn = DateTime.MinValue;
                bool notBefore = true, recordYields = false, delegationsRead = false, cutCount = false, cutSeated = false;
                Dictionary<string, double> pollRead = null;
                string delegationsWhat = "not reached";
                Application.logMessageReceived += Listen;
                try
                {
                    for (int d = 0; d < 120 && sim.CurrentDate < CountedOn; d++)
                    {
                        if (sim.CurrentDate == Polled.AddDays(-1))
                        {
                            // the polling day in the day loop's own order: the count is held inside AdvanceDay, so the poll read right after it is the poll it read
                            bool boundary = sim.AdvanceDay();
                            NationalElection.TryPredictShares(CountryId.USA, out pollRead, EconomicVote.RecordOverTerm(usa, sim.CurrentDate, out _), on: sim.CurrentDate);
                            sim.AdvanceCountryDayTick(CountryId.USA);
                            if (boundary)
                            {
                                var none = new Dictionary<CountryId, PolicyDecision>();
                                foreach (Country c in w.Countries) { none[c.Id] = PolicyDecision.None(); }
                                sim.AdvanceTurn(none);
                            }
                        }
                        else { Step(sim, w); }
                        DateTime today = sim.CurrentDate;
                        if (!ReferenceEquals(seatsRef, usa.ParliamentSeats)) { seatChanges++; seatOn = today; seatsRef = usa.ParliamentSeats; }
                        if (today < HouseDay && !Same(usa.ParliamentSeats, house118)) { notBefore = false; }
                        if (today == Polled)
                        {
                            held = usa.UsCongressElections.Count == 1 ? usa.UsCongressElections[0] : null;
                            president = usa.UsPresidentialElections.Count == 1 ? usa.UsPresidentialElections[0] : null;
                        }
                        if (today == Polled.AddDays(1)) { SaveGameService.SaveToFile(saveCount, SaveGameService.CreateSaveGame(sim, w, CountryId.USA, null)); cutCount = true; }
                        if (today == HouseDay)
                        {
                            houseOnDay = new Dictionary<string, int>(usa.ParliamentSeats);
                            senateOnDay = usa.SecondChamberSeats == null ? null : new Dictionary<string, int>(usa.SecondChamberSeats);
                            recordYields = !WorldClock.RecordSeatsChamber(usa, today) && WorldClock.RecordSeatsChamber(CountryId.USA) && !WorldClock.RecordSeatsSecondChamber(usa, today);
                            Dictionary<string, double> magnitudes = EconomicVote.Magnitudes(usa, today);
                            string pm = usa.Government?.PmParty;
                            string largest = usa.ParliamentSeats.OrderByDescending(kv => kv.Value).First().Key;
                            string senateMajority = UsSenate.MajorityOf(usa.SecondChamberSeats, UsSenate.VicePresidentParty(usa, today));
                            bool unified = largest == pm && senateMajority == pm;
                            government = pm != null && magnitudes.TryGetValue(pm, out double mag) && mag == (unified ? EconomicVote.UsPresidentUnified : EconomicVote.UsPresidentDivided)
                                ? F("{0} - the president's party {1}, the House's largest {2}, the Senate's majority {3}", unified ? "unified" : "divided", pm, largest, senateMajority ?? "none")
                                : F("NOT READ FROM THE GAME'S CHAMBERS - magnitudes {0}", string.Join(", ", magnitudes.Select(kv => kv.Key + " " + kv.Value.ToString(CultureInfo.InvariantCulture))));
                        }
                        if (today == HouseDay.AddDays(1)) { SaveGameService.SaveToFile(saveSeated, SaveGameService.CreateSaveGame(sim, w, CountryId.USA, null)); cutSeated = true; }
                        if (today == CountedOn && held != null)
                        {
                            List<(string State, int Republicans, int Democrats)> read = UsPresidentialElection.DelegationsOn(usa, today, out string houseName);
                            delegationsRead = read.SequenceEqual(held.Delegations()) && read.Count == 50 && houseName.StartsWith("the House the game elected", StringComparison.Ordinal);
                            delegationsWhat = F("{0}: {1} states, REP {2}, DEM {3}, tied {4}", houseName, read.Count, read.Count(x => x.Republicans > x.Democrats), read.Count(x => x.Democrats > x.Republicans), read.Count(x => x.Republicans == x.Democrats));
                        }
                    }
                }
                finally { Application.logMessageReceived -= Listen; }

                Check(president != null && president.PollingDay == Polled, F("(d) the eve world ({0}, seed {1}, the USA the player's country, {2}) holds its presidential count on {3} - {4}", Day(Eve), Seed, PlayerParty, Day(Polled), president?.Line ?? "NONE"));
                Check(held != null && held.PollingDay == Polled && held.Convenes == HouseDay && usa.UsCongressElections.Count == 1 && logged.Count(l => l.StartsWith("CONGRESS: USA - the House of ", StringComparison.Ordinal)) == 1,
                    F("(d) and its Congress count on the same day, once, seated from {0} - \"{1}\"", Day(HouseDay), held?.Line ?? "NO COUNT"));
                Check(held != null && held.House.Sum(s => s.Seats) == UsCongressElection.HouseMembers && held.HouseRep + held.HouseDem == UsCongressElection.HouseMembers && held.House.Count == 50
                      && held.BaseOfRecord && held.BaseYear == 2022 && pollRead != null && held.V == UsCongressElection.TwoPartyV(UsPresidentialElection.CountedPoll(usa, Polled, pollRead)),
                    F("(d) the House by R-US18 (b) from the record's 2022 at V {0:0.0000} - the poll the day's counts read, REP / (REP + DEM), recomputed here {1:0.0000}: 50 states, {2} seats, REP {3} DEM {4}",
                        held?.V ?? double.NaN, pollRead == null ? double.NaN : UsCongressElection.TwoPartyV(pollRead), held?.House.Sum(s => s.Seats) ?? 0, held?.HouseRep ?? 0, held?.HouseDem ?? 0));
                Check(held != null && held.SenateClass == 1 && held.Senate.Count == truth.Count && held.Senate.All(s => truth.ContainsKey((s.State, s.Class))),
                    F("(d) the Senate's class {0} and the specials of the day, {1} seats - the 2024 races of record, seat for seat", held?.SenateClass ?? 0, held?.Senate.Count ?? 0));
                // a count whose seats equal the sitting House's (at a swing near zero R-US18 (b) returns the base's own seats) seats nothing new: the seating
                // compares contents, so the House object changes once, on the 3rd of January, or not at all where the two are equal
                bool sameAsSitting = held != null && Same(held.HouseSeats(CountryId.USA), house118);
                Check(notBefore && (seatChanges == 1 && seatOn == HouseDay || seatChanges == 0 && sameAsSitting),
                    F("(d) the House changed {0} time(s){1}, not before the 3rd of January{2}", seatChanges, seatChanges > 0 ? ", on " + Day(seatOn) : string.Empty, sameAsSitting ? " - the count's seats equal the sitting House's, so nothing new was seated" : string.Empty));
                int houseSum = houseOnDay?.Values.Sum() ?? 0, senateSum = senateOnDay?.Values.Sum() ?? 0;
                Check(held != null && Same(houseOnDay, held.HouseSeats(CountryId.USA)) && houseSum == UsCongressElection.HouseMembers && senateSum == UsSenate.Members
                      && Same(senateOnDay, UsCongressElection.SenateOn(usa, HouseDay)),
                    F("(d) on {0} the game's 119th is seated: the House {1} = {2}, the Senate {3} = {4} - the seats the game did not elect of record by date", Day(HouseDay), Seats(houseOnDay), houseSum, Seats(senateOnDay), senateSum));
                Check(recordYields, "(d) from the 3rd of January the record seats neither the House nor the whole Senate in this world (the country's gate unchanged)");
                Check(delegationsRead, F("(d) the Twelfth Amendment's delegations on {0} read the House the game elected - {1}", Day(CountedOn), delegationsWhat));
                Check(!government.StartsWith("NOT", StringComparison.Ordinal), F("(d) \"divided\" is read from the game's own chambers on {0}: {1}", Day(HouseDay), government));

                // ---- (e) the saves, each loaded INTO THE GAME and stepped a day
                foreach ((string path, string label, bool cut, bool seated) in new[] { (saveCount, "the day after the count", cutCount, false), (saveSeated, "the day after the seating", cutSeated, true) })
                {
                    if (!cut || held == null) { Check(false, F("(e) {0}: no save was cut, or no count was held", label)); continue; }
                    string json = File.ReadAllText(path);
                    SaveGame save = SaveGameService.Deserialize(json);
                    Country saved = save.World.GetCountry(CountryId.USA);
                    bool carried = saved.UsCongressElections.Count == 1 && SameCongress(saved.UsCongressElections[0], held)
                                   && saved.UsPresidentialElections.Count == 1 && saved.UsPresidentialElections[0].TieSeed == president.TieSeed;
                    object seatsSaved = saved.ParliamentSeats;
                    (SimulationManager adopted, PoliSim.UI.GameController loaded) = PresidencyDiagnostic.Adopt(save, hosts, out string error);
                    if (adopted == null) { Check(false, F("(e) the save cut {0}: {1}", label, error)); continue; }
                    try
                    {
                        Country u = adopted.World.GetCountry(CountryId.USA);
                        bool asLoaded = seated ? Same(u.ParliamentSeats, held.HouseSeats(CountryId.USA)) && ReferenceEquals(u.ParliamentSeats, seatsSaved) : Same(u.ParliamentSeats, house118);
                        DateTime from = adopted.CurrentDate;
                        Step(adopted, adopted.World);
                        bool after = u.UsCongressElections.Count == 1 && u.UsPresidentialElections.Count == 1 && (seated ? ReferenceEquals(u.ParliamentSeats, seatsSaved) : Same(u.ParliamentSeats, house118));
                        Check(carried && asLoaded && after,
                            F("(e) the save cut {0} ({1}, format {2}) loads into the game - the Congress contest whole, the tie seed kept, the House as cut ({3}); stepped to {4}: nothing counted twice, the House the same",
                                label, Day(from), save.SaveVersion, Seats(u.ParliamentSeats), Day(adopted.CurrentDate)));
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                    if (seated)
                    {
                        string older = json.Replace("\"SaveVersion\": " + SaveGameService.CurrentSaveVersion.ToString(CultureInfo.InvariantCulture), "\"SaveVersion\": " + (SaveGameService.CurrentSaveVersion - 1).ToString(CultureInfo.InvariantCulture));
                        bool refused = false; string why = null;
                        try { SaveGameService.Deserialize(older); } catch (SaveLoadException e) { refused = true; why = e.Message; }
                        Check(older != json && refused && why.Contains("format version " + (SaveGameService.CurrentSaveVersion - 1).ToString(CultureInfo.InvariantCulture)),
                            F("(e) a save of the format before this build's ({0}) is refused with the plain message: \"{1}\"", SaveGameService.CurrentSaveVersion, why));
                    }
                }

                // ---- (f) every later election seats 435 and 100 - planted even polls, each on the game's own previous count, removed after
                var later = new List<string>();
                bool sums = held != null;
                var planted = new List<UsCongressElection.Contest>();
                try
                {
                    foreach (int year in new[] { 2028, 2032, 2036 })
                    {
                        if (held == null) { break; }
                        DateTime day = UsPresidentialElection.PresidentialDay(year);
                        var poll = new Dictionary<string, double> { { "REP", 0.5 }, { "DEM", 0.5 } };
                        UsPresidentialElection.Contest night = UsPresidentialElection.Count(CountryId.USA, day, poll, null, UsPresidentialElection.TieSeedOf(Seed, year), out ElectoralCollege.StateCall[] _, out string whyNight);
                        UsCongressElection.Contest c = night == null ? null : UsCongressElection.Count(usa, day, poll, night, out string whyCongress);
                        if (c == null) { sums = false; later.Add(F("{0}: no count - {1}", year, whyNight ?? "the Congress count failed")); continue; }
                        usa.UsCongressElections.Add(c);
                        planted.Add(c);
                        Dictionary<string, int> senate = UsCongressElection.SenateOn(usa, c.Convenes);
                        int s = senate.Values.Sum(), h = c.HouseRep + c.HouseDem;
                        bool onGameBase = !c.BaseOfRecord && c.BaseYear == year - 4;
                        sums &= h == UsCongressElection.HouseMembers && s == UsSenate.Members && onGameBase;
                        later.Add(F("{0} (class {1}, from the game's {2}): the House REP {3} DEM {4} = {5}, the Senate seated {6} = {7}", year, c.SenateClass, c.BaseYear, c.HouseRep, c.HouseDem, h, Long(c.Convenes), s));
                    }
                }
                finally { foreach (UsCongressElection.Contest c in planted) { usa.UsCongressElections.Remove(c); } }
                Check(sums && later.Count == 3, F("(f) every later election seats 435 and 100 on its 3 January, each on the game's own previous count: {0}", string.Join("; ", later)));
                EnergyMarket.ResetTurnState();

                // ---- the card: the 119th against the record
                if (held != null)
                {
                    var record = UsPresidentialReturns.HouseDistricts.Where(x => x.Year == 2024).GroupBy(x => x.State).ToDictionary(g => g.Key, g => (R: g.Count(x => x.Winner == "R"), D: g.Count(x => x.Winner == "D")), StringComparer.Ordinal);
                    int recR = record.Values.Sum(x => x.R), recD = record.Values.Sum(x => x.D);
                    string Class(int r, int d) => r > d ? "R" : d > r ? "D" : "T";
                    var misplaced = held.House.Where(s => s.Rep != record[s.State].R).Select(s => F("{0} REP {1} (record {2})", s.State, s.Rep, record[s.State].R)).ToList();
                    int g = held.House.Sum(s => Math.Abs(s.Rep - record[s.State].R));
                    int k = held.House.Count(s => Class(s.Rep, s.Dem) == Class(record[s.State].R, record[s.State].D));
                    int dr = held.House.Count(s => s.Rep > s.Dem), dd = held.House.Count(s => s.Dem > s.Rep);
                    int rr = record.Values.Count(x => x.R > x.D), rd = record.Values.Count(x => x.D > x.R);
                    var senateMisses = held.Senate.Where(s => truth[(s.State, s.Class)] != s.Side).Select(s => F("{0}-{1} {2} (record {3})", s.State, s.Class, s.Side, truth[(s.State, s.Class)])).ToList();
                    int truthR = truth.Values.Count(x => x == "REP"), truthD = truth.Values.Count(x => x == "DEM");
                    UsSenate.Seating open119 = UsSenate.On(HouseDay);
                    card.Add(F("**The Congress election of {0}** - a fresh world on its eve (the epoch {1}, seed {2}, the USA the player's country, {3}), stepped in the controller's order: its own count holds the House and the Senate class on the presidential polling day, on the poll the presidential count reads, and seats both on {4}.",
                        Long(Polled), Long(Eve), Seed, PlayerParty, Long(HouseDay)));
                    card.Add("");
                    card.Add(F("**The House** (R-US18 (b), RULED - I1): REP **{0}**, DEM **{1}** of 435, against the record's **{2} / {3}** - N {4:+0;-0;0}. V(T) {5:0.0000} (the poll on the day, REP / (REP + DEM)) against 2022's own-line {6:0.0000}, a swing of {7:+0.0000;-0.0000}. Delegations REP {8}, DEM {9}, tied {10}, against the record's {11} / {12} / {13}; K {14}/50. Seats misplaced, G {15}: {16}.",
                        held.HouseRep, held.HouseDem, recR, recD, held.HouseRep - recR, held.V, held.VBase, held.V - held.VBase, dr, dd, 50 - dr - dd, rr, rd, 50 - rr - rd, k, g, misplaced.Count == 0 ? "none" : string.Join(", ", misplaced)));
                    card.Add("");
                    card.Add(F("At the record's own swing (2024's own-line V {0:0.0000}) the same count gives REP {1} - US-11's C4 (b) row above reads REP {2}: the game counts the rule R-US18 was measured on.", UsCongressElection.RecordV(2024), atRecordRep, c4));
                    card.Add("");
                    card.Add(F("**The Senate** (R-US19 (c) over (a), RULED - I2): class {0}'s seats and the specials of the day, {1} seats - REP **{2}**, DEM **{3}**, against the record's **{4} / {5}**; called wrong: {6}. Each seat takes its state's share from the presidential count that night, moved by its race's factor c_j, fitted on 2024's class only ({7} seats seated with a factor; |c_j| mean {8:0.000}, largest {9:0.000}); at 2024's true national vote the factors reproduce the class by construction ({10} called wrong).",
                        held.SenateClass, held.Senate.Count, held.SenateOf("REP"), held.SenateOf("DEM"), truthR, truthD, senateMisses.Count == 0 ? "none" : string.Join(", ", senateMisses),
                        factors.Count, factors.Count > 0 ? factors.Average() : 0.0, factors.Count > 0 ? factors.Max() : 0.0, trueMisses.Count == 0 ? "none" : string.Join(", ", trueMisses)));
                    card.Add("");
                    card.Add(F("**The 119th the world seats** from {0}: the House {1} ({2}); the Senate {3} ({4}) - the seats the game did not elect stay the record's, by date. The record's own 119th Senate on that day: {5}, {6} vacant. The economic vote reads the government as {7}.",
                        Long(HouseDay), Seats(houseOnDay), houseSum, Seats(senateOnDay), senateSum, Seats(open119.BySide), open119.Vacant, government));
                    card.Add("");
                    card.Add(F("**Every later election** (planted even polls, each on the game's own previous count), the House and the Senate as each sits on its 3 January: {0}.", string.Join("; ", later)));
                    card.Add("");
                }

                // ---- (g) US-8c: the tie draw - a fair coin on the seed, the year and the place
                int repState = 0, repDistrict = 0, apart = 0;
                for (int seed = 0; seed < 1000; seed++)
                {
                    if (ElectoralCollege.TieBreak(seed, 2024, "PA") == ElectoralCollege.Republican) { repState++; }
                    if (ElectoralCollege.TieBreak(seed, 2024, "NE-2") == ElectoralCollege.Republican) { repDistrict++; }
                    if (ElectoralCollege.TieBreak(seed, 2024, "PA") != ElectoralCollege.TieBreak(seed, 2028, "PA")) { apart++; }
                }
                Check(repState > 400 && repState < 600 && repDistrict > 400 && repDistrict < 600 && apart > 400 && apart < 600,
                    F("(g) US-8c (J4): an exact tie falls each way under the draw - over 1,000 seeds Pennsylvania's tie goes REP {0} times, Nebraska's 2nd district's {1}; the same state in 2028 draws apart from 2024 on {2} seeds", repState, repDistrict, apart));
                card.Add(F("**Ties** (US-8c, Elias's ruling J4): an exact state tie in the presidential count - and, by Code's reading, a district's - is broken by a seeded draw, never in one party's favour: over 1,000 seeds Pennsylvania's tie goes REP {0} times and Nebraska's 2nd district's {1}. The House count's own halves are R-US18's block's (the base winner, the half rule), and a Senate seat at exactly a half goes by R-US19's cascade.", repState, repDistrict));
                card.Add("");
                card.Add("**Declared**: the Senate seats the game did not elect keep the record's resignations and appointments, those that follow from the record's own presidential result included - Vance's Ohio seat vacant from 10 Jan 2025 and Rubio's Florida seat on 20 Jan, both filled on 21 Jan, whoever the game elected - so \"435 and 100\" is held on each 3 January, the day each Congress sits. Under a president acting after an undecided contest (R-US22) no Vice President is in office, and the acting president's party decides an equal Senate. An acting president of the player's party is the player's.");
                card.Add("");
                card.Add("**Not modelled, stated**: third-party winners; runoffs, ranked-choice transfers and Georgia's majority rule; specials the record does not hold; party switches; a delayed oath (every winner sits from 3 January); the midterms (US-27).");
                card.Add("");
                card.Add("The rulings it runs on: R-US18 (b) (I1) and R-US19 (c) over (a) (I2), each RULED; R-US15 (a) (G2); R-US22's order (I6) where the House cannot choose.");
            }
            return Digest(string.Join("\n", card) + UsPresidentialReturns.HouseDistrictSourceDigest + UsPresidentialReturns.HouseMapSourceDigest + UsPresidentialReturns.HouseSourceDigest
                + UsPresidentialReturns.SenateRaceSourceDigest + UsPresidentialReturns.SenateSeatSourceDigest + UsPresidentialReturns.StateSourceDigest);
        }


        /// <summary>The 2024 class's truth: each seat's side as its winner sits once every 2024 winner was sworn (<see cref="Settled"/>) - the record's holder,
        /// each independent with his conference (R-US10 (a)); the class up and the specials of the day.</summary>
        private static Dictionary<(string, int), string> SenateTruth()
        {
            var truth = new Dictionary<(string, int), string>();
            UsSenate.Seating settled = UsSenate.On(Settled);
            foreach ((string state, int cls, string _) in UsCongressElection.SeatsUp(Polled))
            {
                foreach (UsSenate.Seat s in settled.Seats) { if (s.State == state && s.Class == cls) { truth[(state, cls)] = s.Side; } }
            }
            return truth;
        }

        /// <summary>The card's US-11 block, C4's (b) row: the national REP seats R-US18's instrument prints for 2022 -> 2024. 0 where not found.</summary>
        private static int CardC4B(string card)
        {
            int at = card.IndexOf("C4 2022->2024", StringComparison.Ordinal);
            if (at < 0) { return 0; }
            int row = card.IndexOf("\n  (b)  ", at, StringComparison.Ordinal);
            if (row < 0) { return 0; }
            int rep = card.IndexOf("REP ", row, StringComparison.Ordinal);
            int end = rep < 0 ? -1 : card.IndexOf("  ", rep + 4, StringComparison.Ordinal);
            return rep < 0 || end < 0 ? 0 : int.TryParse(card.Substring(rep + 4, end - rep - 4).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;
        }

        private static double[] National2024()
        {
            double d = 0, r = 0, o = 0, t = 0;
            foreach (var s in UsPresidentialReturns.States) { if (s.Year == 2024) { d += s.VotesD; r += s.VotesR; o += s.VotesOther; t += s.VotesTotal; } }
            return new[] { d / t, r / t, o / t };
        }

        /// <summary>A Congress contest as the save carries it, field by field.</summary>
        internal static bool SameCongress(UsCongressElection.Contest a, UsCongressElection.Contest b)
        {
            bool House(UsCongressElection.StateHouse x, UsCongressElection.StateHouse y) => x.State == y.State && x.Seats == y.Seats && x.Rep == y.Rep && x.Dem == y.Dem && x.Share.Equals(y.Share) && x.UnopposedRep == y.UnopposedRep && x.UnopposedDem == y.UnopposedDem;
            bool Senate(UsCongressElection.SenateSeat x, UsCongressElection.SenateSeat y) => x.State == y.State && x.Class == y.Class && x.Term == y.Term && x.Parent.Equals(y.Parent) && x.Factor.Equals(y.Factor) && x.Share.Equals(y.Share) && x.Side == y.Side && x.Cascade == y.Cascade;
            return a.PollingDay == b.PollingDay && a.Convenes == b.Convenes && a.V.Equals(b.V) && a.VBase.Equals(b.VBase) && a.BaseYear == b.BaseYear && a.BaseOfRecord == b.BaseOfRecord
                   && a.HouseRep == b.HouseRep && a.HouseDem == b.HouseDem && a.SenateClass == b.SenateClass && a.Line == b.Line
                   && a.House.Count == b.House.Count && a.House.Zip(b.House, House).All(s => s) && a.Senate.Count == b.Senate.Count && a.Senate.Zip(b.Senate, Senate).All(s => s);
        }

        /// <summary>A world on <paramref name="epoch"/>, as a new game builds one: the USA the player's country and <paramref name="party"/> its party; the Senate of
        /// record seated as the game's selection seats it before any day is stepped (PresidencyDiagnostic's way).</summary>
        private static (SimulationManager Sim, World World, Country Usa) Open(List<GameObject> hosts, DateTime epoch, string party)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            SimulationManager.SetEpoch(epoch);
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("CongressElectionDiagnostic"); hosts.Add(go);
            var s = go.AddComponent<SimulationManager>();
            s.SetWorld(w);
            s.PlayerCountryId = CountryId.USA;
            w.GetCountry(CountryId.USA).PlayerPartyAbbrev = party;
            typeof(SimulationManager).GetMethod("SeatTheSecondChamberOfRecord", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(s, null);
            return (s, w, w.GetCountry(CountryId.USA));
        }

        /// <summary>One day in the controller's order (GameController: AdvanceDay, the player's day tick, the boundary's turn).</summary>
        private static void Step(SimulationManager s, World w)
        {
            bool boundary = s.AdvanceDay();
            if (s.PlayerCountryId.HasValue) { s.AdvanceCountryDayTick(s.PlayerCountryId.Value); }
            if (boundary)
            {
                var none = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in w.Countries) { none[c.Id] = PolicyDecision.None(); }
                s.AdvanceTurn(none);
            }
        }

        private static bool Same(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b) => a != null && b != null && a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out int n) && n == kv.Value);

        private static string Seats(IReadOnlyDictionary<string, int> seats) =>
            seats == null ? "none" : string.Join(", ", seats.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + " " + kv.Value.ToString(CultureInfo.InvariantCulture)));

        /// <summary>A ruling block's text as its instrument digests it: the lines strictly between its two markers, LF.</summary>
        private static string Block(string plan, string name)
        {
            string begin = "<!-- " + name + " RULE BEGIN -->\n", end = "<!-- " + name + " RULE END -->";
            int b0 = plan.IndexOf(begin, StringComparison.Ordinal), b1 = plan.IndexOf(end, StringComparison.Ordinal);
            return b0 >= 0 && b1 > b0 ? plan.Substring(b0 + begin.Length, b1 - b0 - begin.Length) : null;
        }

        // ---- the US model card's US-16 block (docs/reference/US_ELECTIONS.md) - written by WriteReadings

        private const string CardRelative = "docs/reference/US_ELECTIONS.md";
        private const string CardStamp = "<!-- GENERATED by PoliSim.EditorTools.CongressElectionDiagnostic.WriteReadings. DO NOT EDIT BY HAND. source-digest: ";
        private const string CardEnd = "<!-- END GENERATED -->";

        private static string PathOf(string relative) => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), relative);

        private static string Digest(string text)
        {
            using (var sha = SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLowerInvariant(); }
        }

        private static string CardBlock(List<string> lines, string digest)
        {
            var block = new StringBuilder(CardStamp).Append(digest).Append(" -->\n");
            foreach (string line in lines) { block.Append(line).Append('\n'); }
            return block.Append(CardEnd).ToString();
        }

        private static string CardBlockOf(string card)
        {
            string text = card.Replace("\r\n", "\n");
            int start = text.IndexOf(CardStamp, StringComparison.Ordinal);
            int end = start < 0 ? -1 : text.IndexOf(CardEnd, start, StringComparison.Ordinal);
            return end < 0 ? null : text.Substring(start, end + CardEnd.Length - start);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
