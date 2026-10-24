using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>One attributed line of the impact ledger: what a family of dials contributed to a stat's
    /// divergence from the no-policy baseline.</summary>
    public struct ImpactLine
    {
        public string Family;
        public float Contribution;
    }

    /// <summary>
    /// C-C10 (P-G2) — **the impact ledger: the live-vs-shadow divergence, attributed to the dials that
    /// caused it, with the part that belongs to no single dial shown as its own line.**
    ///
    /// <para><b>The method is LEAVE-ONE-OUT</b>, the strongest attribution available for a model whose
    /// dials interact. Beside the real game runs C-C9's no-policy shadow, and — for each FAMILY of dials
    /// the player has actually touched — a world running everything the player did except that family.
    /// Then</para>
    ///
    /// <list type="bullet">
    /// <item><description><c>divergence = real − none</c>, the whole gap to explain;</description></item>
    /// <item><description><c>contribution(f) = real − except(f)</c>, what removing that family would
    /// have changed with every other dial still in play;</description></item>
    /// <item><description><c>interaction = divergence − Σ contribution(f)</c>.</description></item>
    /// </list>
    ///
    /// <para>⚠ <b>THE INTERACTION LINE IS NOT AN ERROR TERM, AND IT IS NOT OPTIONAL.</b> Measured before
    /// this class was written (`ImpactLedgerFeasibilityDiagnostic`, `COMPLETED.md` §106): over 12 turns of
    /// four dials on the USA it reaches <b>17.4 % of the divergence on Government Debt</b> and 12.6 % on
    /// the Budget. A tax rise and a spending rise meet in the same GDP; no decomposition into per-dial
    /// lines can be exact unless the model is linear in its dials, and it is not. So the ledger prints
    /// the interaction as a named line rather than folding it into the largest contributor or scaling the
    /// lines to close the sum — Elias's ruling for this item: <b>an honest residual beats a false
    /// identity.</b> With the interaction line present the ledger sums EXACTLY, by construction, and
    /// <see cref="PolicyImpactLedgerDiagnostic"/> asserts that identity rather than trusting it.</para>
    ///
    /// <para><b>Cost.</b> A family the player has never touched has an except-world identical to the real
    /// game, so it costs nothing to not have one; the fork happens on FIRST TOUCH and is exact, not an
    /// approximation (see <see cref="ShadowBaseline"/>'s fork constructor). A game where the player only
    /// ever moves taxes therefore pays for two counterfactual worlds, not eleven.</para>
    /// </summary>
    public sealed class PolicyImpactLedger
    {
        /// <summary>
        /// ⚠ **The families partition `PolicyDecision`'s fields, and the partition is CHECKED.**
        /// A hand-written list of field names is exactly the shape R4-1's clone-escape class warns about
        /// — a new field added to `PolicyDecision` would silently belong to no family and vanish from
        /// every attribution. So <see cref="AssertPartitionCovers"/> compares this table against the
        /// type's real fields at construction and throws naming the offender. The list is a hand-list
        /// because grouping dials into families is a JUDGEMENT and cannot be derived; that it is
        /// COMPLETE is not a judgement, and is not left to one.
        /// </summary>
        private static readonly Dictionary<string, string[]> Families = new Dictionary<string, string[]>
        {
            { "Taxes", new[] { "TaxRateOverrides" } },
            { "Welfare", new[] { "WelfareGenerosityOverrides" } },
            {
                "Spending", new[]
                {
                    "SpendingLineChanges", "SpendingNominalTargets", "SpendingPinChanges",   // P5-B2's figure and pin - the same spending family
                    "HealthcareSpendingChange", "DefenseSpendingChange",
                    "InfrastructureSpendingChange", "EducationSpendingChange", "JusticeSpendingChange",
                    "HomelandSecuritySpendingChange", "EnergySpendingChange", "HousingSpendingChange"
                }
            },
            { "Trade", new[] { "TariffRateChange", "PartnerTariffOverrides" } },
            // ⚠ S-18: since C-C7 every country has a central-bank head, so ApplyInterestRateChanges never
            // reads this field and its contribution is exactly zero. It stays a family of its own so the
            // ledger reports that zero rather than hiding a dead lever inside another family's line.
            // AF-11: the head's appointment is this family's lever (LeverFamilies).
            { "Monetary", new[] { "InterestRateChange" } },
            // AF-11: three families no PolicyDecision field belongs to - each is touched by its levers alone
            // (LeverFamilies), and an untouched one costs nothing, as any family does. The fiscal stance moves
            // every line and the two household rates at once (FinancePartner), so it is a family of its own.
            { "Energy fleet", new string[0] },
            { "Cabinet", new string[0] },
            { "Fiscal stance", new string[0] },
            {
                "Labour", new[]
                {
                    "MinimumWageOverride", "PaidFamilyLeaveWeeksOverride", "OvertimeRegulationOverride",
                    "RetrainingProgramOverride"
                }
            },
            {
                "Crime and justice", new[]
                {
                    "PoliceFundingOverride", "SentencingSeverityOverride", "BailReformOverride",
                    "DrugPolicyOverride", "JudicialFundingOverride", "BorderEnforcementOverride"
                }
            },
            {
                "Sectors", new[]
                {
                    "SectorSubsidyOverrides", "SectorRegulationOverrides", "SectorTaxCreditOverrides",
                    "SectorResearchGrantsOverrides", "SectorDeregulationNationalizationOverrides"
                }
            },
            {
                "Sovereign wealth", new[]
                {
                    "SwfContributionRateOverride", "SwfDomesticAllocationOverride", "SwfEquitiesWeightOverride",
                    "SwfBondsWeightOverride", "SwfInfrastructureWeightOverride", "SwfRealEstateWeightOverride"
                }
            },
            { "Demographics", new[] { "FamilyPolicyOverride", "ImmigrationPolicyOverride" } },
            // K51 (Elias's ruling of 2026-10-23: "each kind of bill is its own ledger family"): no PolicyDecision field belongs to these - each is touched by the
            // player's bills of its kind alone (BillFamilies), as the lever families are by their levers
            { "Budget bill", new string[0] },
            { "Tax programme bill", new string[0] },
            { "Welfare programme bill", new string[0] },
            { "Labour bill", new string[0] },
            { "Crime and justice bill", new string[0] },
            { "Law bill", new string[0] },
            { "Sector bill", new string[0] },
            { "Trade bill", new string[0] },
            { "Fund drawdown bill", new string[0] }
        };

        /// <summary>
        /// K51 (Elias's ruling of 2026-10-23, `docs/RULINGS_2026-10-23.md`; FORK-2's ask, §890): <b>each kind of bill is its own ledger family</b> - the family of
        /// every bill kind the game votes (`Congress.Kind`). Since FORK-2 a bill was an answer of no family, replayed in every world, so its effect landed on the
        /// interaction line; and because a player's dials reach the game through bills, no dial family was ever forked in a real game. A bill is now read as a
        /// lever is (`BeforeAnswer`, `AfterAnswer`): its family's world never lays it, every other world lays it on the same day. An alternative budget is the
        /// budget's kind. Completeness is checked (`AssertPartitionCovers`): a kind with no family throws.
        /// </summary>
        private static readonly Dictionary<Congress.Kind, string> BillFamilies = new Dictionary<Congress.Kind, string>
        {
            { Congress.Kind.Budget, "Budget bill" },
            { Congress.Kind.TaxProgram, "Tax programme bill" },
            { Congress.Kind.WelfareProgram, "Welfare programme bill" },
            { Congress.Kind.Labor, "Labour bill" },
            { Congress.Kind.CrimeJustice, "Crime and justice bill" },
            { Congress.Kind.Law, "Law bill" },
            { Congress.Kind.Sector, "Sector bill" },
            { Congress.Kind.Trade, "Trade bill" },
            { Congress.Kind.SwfDrawdown, "Fund drawdown bill" },
        };

        /// <summary>K51: the family an answer belongs to - its bill kind's (`BillFamilies`), or null for an answer that lays no bill (it belongs to no family).</summary>
        private static string FamilyOf(PlayerAnswer answer) => answer?.Bill != null && BillFamilies.TryGetValue(answer.Bill.Value, out string family) ? family : null;

        /// <summary>
        /// AF-11 (Elias's ruling q11 of 2026-10-18): **the family each lever belongs to** - the levers a player pulls outside the turn's decision,
        /// through their <see cref="SimulationManager"/> verbs (<see cref="PlayerLeverKind"/>). A lever is read as a dial is: its family's
        /// except-world never does it, and every other one does (<see cref="BeforeLever"/>, <see cref="AfterLever"/>). Like the table above, the
        /// grouping is a judgement and its completeness is checked (<see cref="AssertPartitionCovers"/>): a lever kind with no family throws.
        /// </summary>
        private static readonly Dictionary<PlayerLeverKind, string> LeverFamilies = new Dictionary<PlayerLeverKind, string>
        {
            { PlayerLeverKind.PartnerTariffOverride, "Trade" },
            { PlayerLeverKind.CentralBankHead, "Monetary" },
            { PlayerLeverKind.FleetOrder, "Energy fleet" },
            { PlayerLeverKind.MinisterAppointment, "Cabinet" },
            { PlayerLeverKind.MinisterReshuffle, "Cabinet" },
            { PlayerLeverKind.FinanceStance, "Fiscal stance" },
        };

        /// ⚠ Not bound at construction. `GameController.Start` runs BEFORE the player picks a country, so
        /// an id captured then is the default one and every later attribution would be read off the wrong
        /// country - which is exactly what the first film of this panel showed. The id travels with the
        /// call instead.
        private CountryId _playerCountryId;
        private readonly ShadowBaseline _none;
        private readonly Dictionary<string, ShadowBaseline> _except = new Dictionary<string, ShadowBaseline>();

        /// <summary>The no-policy counterfactual — C-C9's shadow, which this ledger measures against.</summary>
        public ShadowBaseline NoPolicy => _none;

        public PolicyImpactLedger(ShadowBaseline noPolicy)
        {
            AssertPartitionCovers();

            _none = noPolicy;
            // FORK-2 (the review's H2; DECLARED, put to Elias): the world this ledger measures against is handed no answer, so it answers every wait on the
            // player by the AI's rule for the player's party - it never stalls on the Speaker's question or an offer nobody there will answer
            if (_none != null) { _none.AnswersWaitsAsTheAi = true; }
        }

        /// <summary>FORK-2 (the review's M3): the families whose world failed - a fork that threw, a replay that threw - each with the reason, logged once. Its
        /// world is dropped, so its line is unavailable from that day (its share of the gap falls to the interaction line), and the family is not forked again
        /// in this ledger: a fresh fork would count from the failure, not from the family's first touch. A counterfactual never breaks the played game.</summary>
        public IReadOnlyDictionary<string, string> Unavailable => _unavailable;
        private readonly Dictionary<string, string> _unavailable = new Dictionary<string, string>();

        /// <summary>FORK-2 (the review's M3): the family's world set aside where it has failed (<see cref="ShadowBaseline.Faulted"/>, or <paramref name="why"/>).</summary>
        private void RetireIfFailed(string family, string why = null)
        {
            _except.TryGetValue(family, out ShadowBaseline world);
            why = why ?? world?.Faulted;
            if (why == null) { return; }
            if (world != null) { _except.Remove(family); world.Dispose(); }
            if (_unavailable.ContainsKey(family)) { return; }
            _unavailable[family] = why;
            Debug.LogError($"LEDGER: the {family} world failed and is set aside - {why}; the {family} line is unavailable from now on, the played game untouched");
        }

        /// <summary>FORK-2 (the review's M3): every family's world checked after a call that reached them all.</summary>
        private void RetireTheFailed()
        {
            foreach (string family in new List<string>(_except.Keys)) { RetireIfFailed(family); }
        }

        /// <summary>FORK-2 (the review's M3): a family's world forked, or the reason it could not be - never an exception into the played game.</summary>
        private void Fork(string family, System.Func<ShadowBaseline> fork)
        {
            if (_unavailable.ContainsKey(family) || _except.ContainsKey(family)) { return; }
            try { ShadowBaseline world = fork(); world.AnswersWaitsAsTheAi = true; _except[family] = world; }   // the fix review's H1: every ledger world answers an unanswered wait by the AI's rule
            catch (Exception e) { RetireIfFailed(family, "the fork threw " + e.GetType().Name + ": " + e.Message); }
        }

        /// <summary>
        /// ⚠ **Call this BEFORE the real game crosses the same turn's boundary.** A family first touched this
        /// turn forks from the state the real game holds at the call - in the game, after the turn's days and
        /// before its boundary (AF-11: the fork then runs no day and crosses the boundary, `ShadowBaseline`'s
        /// fork reads the days already run; until AF-11 it ran a whole extra turn); in a harness that calls it
        /// at the turn's start, before the days. Either way it is precisely the state its except-world would
        /// have been in had it existed from the beginning — that is what makes the lazy fork exact.
        /// </summary>
        public void AdvanceTurn(SimulationManager realSim, World realWorld, CountryId playerCountryId, Dictionary<CountryId, PolicyDecision> decisions)
        {
            _playerCountryId = playerCountryId;
            decisions.TryGetValue(_playerCountryId, out PolicyDecision playerDecision);

            foreach (KeyValuePair<string, string[]> family in Families)
            {
                bool touched = playerDecision != null && Touches(playerDecision, family.Value);
                if (touched) { Fork(family.Key, () => new ShadowBaseline(realSim, realWorld, _playerCountryId, dayTick: true, seatAsPlayed: true)); }   // FORK-1 (J8): its days step as the played world's; LEDGER-1: it seats what the played world seats

                if (!_except.TryGetValue(family.Key, out ShadowBaseline world)) { continue; }

                if (touched) { _withheld.Add(family.Key); }   // K46: the family's own dials stripped from this world's turn
                world.AdvanceTurn(Strip(decisions, family.Value));   // FORK-2 (M3): a world that throws marks itself failed and returns
                RetireIfFailed(family.Key);
            }
        }

        /// <summary>
        /// AF-11: **a lever is about to act** (<see cref="SimulationManager"/>'s verbs call this before the act). A family first touched by it forks
        /// its except-world NOW, from the state the lever finds - the state that world would hold had it existed all along, since every earlier lever
        /// of another family is in it and none of its own came before (the first touch's reasoning in <see cref="AdvanceTurn"/>, at the lever's moment).
        /// A fork made inside a turn has run that turn's days so far, and runs only the rest at the boundary (<see cref="ShadowBaseline"/>'s fork).
        /// </summary>
        public void BeforeLever(SimulationManager realSim, PlayerLever lever)
        {
            string family = LeverFamilies[lever.Kind];
            Fork(family, () => new ShadowBaseline(realSim, realSim.World, realSim.PlayerCountryId ?? lever.CountryId, insideTurn: true, dayTick: true, seatAsPlayed: true));   // the save's player, as the boundary's forks; FORK-1 (J8): its days step as the played world's; LEDGER-1: it seats what the played world seats
        }

        /// <summary>
        /// FORK-2 (D-45's remainder; the s871 review's M2): **the player has answered** in the real game - a meeting, a bill, the docket, a withhold, a motion, a
        /// formation, the campaign (<see cref="PlayerAnswer"/>). Every except-world gives the same answer on the same day of the turn
        /// (<see cref="ShadowBaseline.Schedule(int, PlayerAnswer)"/>): an answer is no dial and no lever, so it belongs to no family and no except-world leaves it
        /// out - and a fork with no policy difference stays the played world through it. The no-policy world is handed no answer; it answers its own waits by
        /// the AI's rule (<see cref="ShadowBaseline.AnswersWaitsAsTheAi"/>).
        /// ⚠ Before FORK-2 no except-world received an answer, so its effect was booked to every family forked before it (§865's M4 shape).
        /// </summary>
        public void AfterAnswer(SimulationManager realSim, PlayerAnswer answer)
        {
            int day = realSim.DaysIntoTurn;
            string own = FamilyOf(answer);   // K51: a bill's family's world never lays it
            foreach (KeyValuePair<string, ShadowBaseline> pair in _except)
            {
                if (pair.Key == own) { _withheld.Add(own); continue; }   // K46: the bill left unlaid in its own kind's world
                pair.Value.Schedule(day, answer);
            }
            RetireTheFailed();   // FORK-2 (M3): a replay that threw sets its world aside; the played world has acted and stays as it is
        }

        /// <summary>
        /// K51 (Elias's ruling of 2026-10-23: "each kind of bill is its own ledger family"): <b>the player is about to lay a bill</b> (`SimulationManager`'s
        /// answer path calls this before the act). A kind first laid forks its family's world NOW, from the state the bill finds - the lazy fork's reasoning at a
        /// lever's moment (`BeforeLever`): every earlier act of another family is in that state, and no bill of its kind came before. True where this call forked
        /// the world (the caller drops it again where the bill is refused, `ForgetUnlaid`); false for an answer that lays no bill, or a kind forked already.
        /// </summary>
        internal bool BeforeAnswer(SimulationManager realSim, PlayerAnswer answer)
        {
            string family = FamilyOf(answer);
            if (family == null || _except.ContainsKey(family) || _unavailable.ContainsKey(family)) { return false; }
            CountryId? seat = realSim.PlayerCountryId ?? answer.CountryId;
            if (!seat.HasValue) { return false; }
            Fork(family, () => new ShadowBaseline(realSim, realSim.World, seat.Value, insideTurn: true, dayTick: true, seatAsPlayed: true));   // as a lever's fork (BeforeLever)
            return _except.ContainsKey(family);
        }

        /// <summary>K51: a bill whose kind's world `BeforeAnswer` forked was refused (no slot, the gate) - the kind was never laid, so its world is dropped
        /// and the family stays untouched, as a lever that acts on nothing forks nothing it keeps.</summary>
        internal void ForgetUnlaid(PlayerAnswer answer)
        {
            string family = FamilyOf(answer);
            if (family == null || !_except.TryGetValue(family, out ShadowBaseline world)) { return; }
            _except.Remove(family);
            world.Dispose();
        }

        /// <summary>
        /// FORK-2 (the s871 review's M1): **the per-family worlds as a save carries them** - each brought to the real game's day (<see cref="ShadowBaseline.CatchUp"/>)
        /// and carried as the no-policy world is (<see cref="ShadowBaseline.Carry"/>), keyed by its family; null where there is none to carry (no family
        /// touched, or a world with no player seat). So after a load each family's line keeps its span from the family's first touch, as the divergence keeps
        /// its span from the start - before FORK-2 the ledger restarted empty at a load and every line counted from it.
        /// </summary>
        public Dictionary<string, string> Carry(SimulationManager realSim) => Carry(realSim, out _);

        /// <summary>
        /// K46 (Elias's ruling of 2026-10-23: "a save carries worlds only for families whose ledger line is non-zero; autosaves carry none and rebuild on load"):
        /// the worlds a MANUAL save carries - only the families whose line is non-zero, or may become so; each family whose world is still the played world is
        /// named in <paramref name="rebuilt"/> instead, and the load forks it again from the loaded game (`Rebuild`). Exact: such a world is the very world a fork
        /// of the loaded game is, so its line keeps its span (zero so far, and every later act still counted).
        /// <para>DECLARED reading of "non-zero": a family's world is still the played world where the ledger has withheld nothing from it since its fork - no
        /// dial of its family stripped from a turn's decision, no lever or bill of its family left undone in it (`_withheld`), and the world has answered
        /// nothing the played world did not (`ShadowBaseline.AnsweredAlone`: a replay that missed, a wait answered by the AI's rule) - AND, brought to the played
        /// world's day, every country's economic state reads alike to the bit (so all its lines are zero). A world whose line reads zero but which has had an act
        /// withheld (a bill of its kind still before the chamber) is carried: its line can move without another touch. The text of the two worlds is not
        /// compared: a snapshot numbers its objects in the order it writes them, and a world restored from one (every fork) can list a table's entries in
        /// another order than the world it was taken from.</para>
        /// </summary>
        public Dictionary<string, string> Carry(SimulationManager realSim, out List<string> rebuilt)
        {
            rebuilt = null;
            if (_except.Count == 0) { return null; }
            var carried = new Dictionary<string, string>();
            foreach (KeyValuePair<string, ShadowBaseline> pair in new List<KeyValuePair<string, ShadowBaseline>>(_except))
            {
                if (pair.Value.Faulted != null) { RetireIfFailed(pair.Key); continue; }   // FORK-2 (M3): a failed world is not carried
                if (!_withheld.Contains(pair.Key) && !pair.Value.AnsweredAlone)
                {
                    pair.Value.CatchUp(realSim.DaysIntoTurn);   // K46: read on the played world's day - the boundary's days run sooner, exactly (FORK-2)
                    if (pair.Value.Faulted != null) { RetireIfFailed(pair.Key); continue; }
                    if (!pair.Value.AnsweredAlone && StatesAlike(realSim.World, pair.Value.World)) { (rebuilt = rebuilt ?? new List<string>()).Add(pair.Key); continue; }   // K46: still the played world - rebuilt at the load
                }
                string world = pair.Value.Carry(realSim.DaysIntoTurn);
                if (pair.Value.Faulted != null) { RetireIfFailed(pair.Key); continue; }   // a catch-up that threw
                if (world == null) { rebuilt = null; return null; }
                carried[pair.Key] = world;
            }
            return carried;
        }

        /// <summary>K46: the families whose world the ledger has withheld an act from since its fork - a dial of the family stripped from a turn's decision
        /// (`AdvanceTurn`), a lever of it left undone (`AfterLever`), a bill of its kind left unlaid (`AfterAnswer`); a world carried by a save counts as one
        /// (it was carried because it had). A family not here holds a world that is still the played world.</summary>
        private readonly HashSet<string> _withheld = new HashSet<string>();

        /// <summary>K46: the families an AUTOSAVE names for the load to rebuild - every family whose world stands, none carried (null where there is none). Its
        /// load forks each from the loaded game, so each line counts from that load (DECLARED: an autosave carries no world, so a line the family had opened before
        /// it is not kept).</summary>
        public List<string> FamiliesToRebuild() => _except.Count == 0 ? null : new List<string>(_except.Keys);

        /// <summary>K46: the families the last `Rebuild` forked at a load, in the order named - read by the load's log and the checks.</summary>
        public IReadOnlyList<string> Rebuilt => _rebuilt;
        private readonly List<string> _rebuilt = new List<string>();

        /// <summary>
        /// K46: <b>the load rebuilds the worlds a save named and did not carry</b> (`SaveGame.ImpactLedgerRebuild`) - each forked from the loaded game as it stands
        /// (C-C10's fork: a save round trip of the loaded world, on the played world's day, seating what it seats), deterministic: the fork reads the loaded
        /// state and draws nothing from the played world's streams (`ShadowBaseline`'s swap). A family the build does not have, one carried, or one set aside as
        /// unavailable is not forked. Triggered by the game's own load (`GameController.RestoreFromSave`), after the played world and the no-policy world stand.
        /// </summary>
        public void Rebuild(SimulationManager realSim, IEnumerable<string> families)
        {
            _rebuilt.Clear();
            if (realSim == null || families == null || !realSim.PlayerCountryId.HasValue) { return; }
            CountryId seat = realSim.PlayerCountryId.Value;
            foreach (string family in families)
            {
                if (family == null || !Families.ContainsKey(family) || _except.ContainsKey(family) || _unavailable.ContainsKey(family)) { continue; }
                Fork(family, () => new ShadowBaseline(realSim, realSim.World, seat, insideTurn: true, dayTick: true, seatAsPlayed: true));   // the load's fork (insideTurn: an inactive host)
                if (_except.ContainsKey(family)) { _rebuilt.Add(family); }
            }
        }

        /// <summary>K46: every country's economic state alike in two worlds (each public float compared exactly) - `Carry`'s last test, after the bookkeeping
        /// (nothing withheld, nothing answered alone) that the world is still the played world.</summary>
        private static bool StatesAlike(World a, World b)
        {
            if (a == null || b == null) { return false; }
            foreach (Country c in a.Countries)
            {
                Country d = b.GetCountry(c.Id);
                if (d == null) { return false; }
                foreach (FieldInfo f in StateFloats)
                {
                    if (((float)f.GetValue(c.State)).CompareTo((float)f.GetValue(d.State)) != 0) { return false; }
                }
            }
            return true;
        }

        private static readonly List<FieldInfo> StateFloats = FloatsOf(typeof(EconomyState));

        private static List<FieldInfo> FloatsOf(Type type)
        {
            var floats = new List<FieldInfo>();
            foreach (FieldInfo f in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) { if (f.FieldType == typeof(float)) { floats.Add(f); } }
            return floats;
        }

        /// <summary>
        /// FORK-2 (the s871 review's M1): the ledger a save carried (<see cref="Carry"/>), around the no-policy world the same save carried - every family's world
        /// restored as it was saved. Null, with the reason, where they cannot stand together: no worlds carried, a family the table does not name, a world that
        /// will not restore, or one seating another player country or standing on another day than the no-policy world's turn - the caller then starts the
        /// ledger empty, as every load did before FORK-2.
        /// </summary>
        public static PolicyImpactLedger FromCarried(ShadowBaseline noPolicy, Dictionary<string, string> carried, System.DateTime savedOn, out string refusedBecause) =>
            FromCarried(noPolicy, carried, null, savedOn, out refusedBecause);

        /// <summary>FORK-2 (the fix review's M2): <see cref="FromCarried(ShadowBaseline, Dictionary{string, string}, System.DateTime, out string)"/> with the
        /// families the save carried as unavailable (<see cref="CarryUnavailable"/>) - so a load neither forks one afresh nor drops its notice.</summary>
        public static PolicyImpactLedger FromCarried(ShadowBaseline noPolicy, Dictionary<string, string> carried, Dictionary<string, string> unavailable, System.DateTime savedOn, out string refusedBecause)
        {
            refusedBecause = null;
            if ((carried == null || carried.Count == 0) && (unavailable == null || unavailable.Count == 0)) { refusedBecause = "the save carries no family's world"; return null; }
            var ledger = new PolicyImpactLedger(noPolicy);
            if (unavailable != null) { foreach (KeyValuePair<string, string> pair in unavailable) { if (Families.ContainsKey(pair.Key)) { ledger._unavailable[pair.Key] = pair.Value; } } }
            carried = carried ?? new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> pair in carried)
            {
                if (!Families.ContainsKey(pair.Key)) { refusedBecause = $"the save carries a world for '{pair.Key}', a family this build does not have"; ledger.Dispose(); return null; }
                ShadowBaseline world;
                try { world = ShadowBaseline.FromCarried(pair.Value); }
                catch (Exception e) { refusedBecause = $"the {pair.Key} world did not restore - {e.Message}"; ledger.Dispose(); return null; }
                world.AnswersWaitsAsTheAi = true;   // the fix review's H1 (and M2: the mode needs no carrying - every ledger world has it)
                ledger._except[pair.Key] = world;
                ledger._withheld.Add(pair.Key);   // K46: carried because it had an act withheld - never read as the played world again
                if (world.PlayerCountryId != noPolicy?.PlayerCountryId || world.CurrentDate > savedOn || world.CurrentTurn != noPolicy?.CurrentTurn)
                {
                    refusedBecause = $"the {pair.Key} world stands on {world.CurrentDate:yyyy-MM-dd} seating {world.PlayerCountryId?.ToString() ?? "no country"} - not the saved game's turn and seat";
                    ledger.Dispose();
                    return null;
                }
            }
            return ledger;
        }

        /// <summary>
        /// AF-11: **a lever has acted** in the real game: every except-world but its own family's does the same act on its copy of the country, on
        /// the same day of the turn (<see cref="ShadowBaseline.Schedule"/> - an except-world runs its turn's days at the boundary, so it lags the real
        /// game inside a turn), as every except-world but a dial's family is handed that dial (<see cref="Strip"/>). The no-policy world never is.
        /// ⚠ Without this a world forked before the lever never received it, and its effect was booked to every family forked earlier (§544 (b), the fleet).
        /// </summary>
        public void AfterLever(SimulationManager realSim, PlayerLever lever)
        {
            string family = LeverFamilies[lever.Kind];
            int day = realSim.DaysIntoTurn;
            foreach (KeyValuePair<string, ShadowBaseline> pair in _except)
            {
                if (pair.Key == family) { _withheld.Add(family); continue; }   // K46: the lever left undone in its own family's world
                pair.Value.Schedule(day, lever);
            }
            RetireTheFailed();
        }

        /// <summary>
        /// The ledger for one stat: every touched family's contribution, then the interaction, in an
        /// order that reads — largest absolute contribution first, the interaction always last because it
        /// is the qualification on everything above it rather than another cause.
        /// </summary>
        public List<ImpactLine> LinesFor(Country realCountry, string statField, out float divergence)
        {
            float real = Read(realCountry, statField);
            CountryId id = realCountry.Id;
            float none = Read(_none.CountryFor(id), statField);
            divergence = real - none;

            var lines = new List<ImpactLine>();
            float attributed = 0f;
            foreach (KeyValuePair<string, ShadowBaseline> pair in _except)
            {
                float contribution = real - Read(pair.Value.CountryFor(id), statField);
                attributed += contribution;
                lines.Add(new ImpactLine { Family = pair.Key, Contribution = contribution });
            }

            lines.Sort((a, b) => Mathf.Abs(b.Contribution).CompareTo(Mathf.Abs(a.Contribution)));
            lines.Add(new ImpactLine { Family = "interaction", Contribution = divergence - attributed });
            return lines;
        }

        /// <summary>True once at least one family has been touched — before that the ledger has nothing
        /// to explain and the screen says so rather than printing an empty table.</summary>
        public bool HasAnything => _except.Count > 0 || _unavailable.Count > 0;   // FORK-2 (the fix review's M4): a family whose world failed is still a family the player touched

        /// <summary>FORK-2 (the fix review's M2): the families set aside as unavailable, with their reasons, as a save carries them (`SaveGame.ImpactLedgerUnavailable`)
        /// - null where there is none.</summary>
        public Dictionary<string, string> CarryUnavailable() => _unavailable.Count == 0 ? null : new Dictionary<string, string>(_unavailable);

        private static float Read(Country country, string statField)
        {
            if (country == null) { return 0f; }

            FieldInfo field = typeof(EconomyState).GetField(statField);
            return field == null ? 0f : (float)field.GetValue(country.State);
        }

        /// <summary>A copy of every country's decision with one family's fields returned to their
        /// untouched defaults. ⚠ The copy is by REFLECTION over every field, so a field added to
        /// `PolicyDecision` travels automatically; only the family membership is a hand-list, and that
        /// list is checked to be complete.</summary>
        private Dictionary<CountryId, PolicyDecision> Strip(Dictionary<CountryId, PolicyDecision> decisions, string[] familyFields)
        {
            var stripped = new Dictionary<CountryId, PolicyDecision>();
            var defaults = new PolicyDecision();
            var family = new HashSet<string>(familyFields);

            foreach (KeyValuePair<CountryId, PolicyDecision> pair in decisions)
            {
                if (pair.Key != _playerCountryId || pair.Value == null)
                {
                    stripped[pair.Key] = pair.Value ?? PolicyDecision.None();
                    continue;
                }

                var copy = new PolicyDecision();
                foreach (FieldInfo f in typeof(PolicyDecision).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    object source = family.Contains(f.Name) ? f.GetValue(defaults) : f.GetValue(pair.Value);
                    f.SetValue(copy, CopyValue(f.FieldType, source));
                }

                stripped[pair.Key] = copy;
            }

            return stripped;
        }

        /// <summary>Dictionaries are copied rather than shared, so a stripped decision can never be a
        /// second reference to the live one.</summary>
        private static object CopyValue(Type type, object value)
        {
            if (value is IDictionary source)
            {
                var copy = (IDictionary)Activator.CreateInstance(type);
                foreach (DictionaryEntry entry in source) { copy[entry.Key] = entry.Value; }
                return copy;
            }

            return value;
        }

        private static bool Touches(PolicyDecision decision, string[] familyFields)
        {
            var defaults = new PolicyDecision();
            foreach (string name in familyFields)
            {
                FieldInfo f = typeof(PolicyDecision).GetField(name);
                if (f == null) { continue; }

                object value = f.GetValue(decision);
                if (value is IDictionary dictionary) { if (dictionary.Count > 0) { return true; } continue; }
                if (!Equals(value, f.GetValue(defaults))) { return true; }
            }

            return false;
        }

        /// <summary>⚠ The completeness check. A `PolicyDecision` field in no family would be a dial the
        /// ledger silently never attributes; a name in the table that is not a field would be a family
        /// that quietly attributes nothing. Both throw, naming the offender.</summary>
        private static void AssertPartitionCovers()   // PolicyImpactLedgerCheck reaches it by reflection on the cheap bar (P5-B5) - a harness reaching private state is the project's idiom
        {
            var claimed = new HashSet<string>();
            foreach (KeyValuePair<string, string[]> family in Families)
            {
                foreach (string name in family.Value)
                {
                    if (typeof(PolicyDecision).GetField(name) == null)
                    {
                        throw new InvalidOperationException(
                            $"PolicyImpactLedger: family '{family.Key}' names '{name}', which is not a field of PolicyDecision.");
                    }

                    if (!claimed.Add(name))
                    {
                        throw new InvalidOperationException(
                            $"PolicyImpactLedger: '{name}' is claimed by more than one family, so leave-one-out would double-count it.");
                    }
                }
            }

            foreach (FieldInfo f in typeof(PolicyDecision).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!claimed.Contains(f.Name))
                {
                    throw new InvalidOperationException(
                        $"PolicyImpactLedger: PolicyDecision.{f.Name} belongs to no family, so the impact ledger would never attribute it. "
                        + "Add it to the family it belongs to - a dial with no family is a dial the player cannot be told about.");
                }
            }

            // AF-11: every lever kind in a family the table above names - a lever with none would act in the real game and in no except-world's absence
            foreach (PlayerLeverKind kind in Enum.GetValues(typeof(PlayerLeverKind)))
            {
                if (!LeverFamilies.TryGetValue(kind, out string family))
                {
                    throw new InvalidOperationException(
                        $"PolicyImpactLedger: the lever {kind} belongs to no family, so the impact ledger would never attribute it. Add it to LeverFamilies.");
                }

                if (!Families.ContainsKey(family))
                {
                    throw new InvalidOperationException(
                        $"PolicyImpactLedger: the lever {kind} names the family '{family}', which the family table does not hold.");
                }
            }

            // K51: every bill kind the game votes in a family of its own the table holds - a kind with none would land on the interaction line again
            var billFamilies = new HashSet<string>();
            foreach (Congress.Kind kind in Enum.GetValues(typeof(Congress.Kind)))
            {
                if (!BillFamilies.TryGetValue(kind, out string family) || !Families.ContainsKey(family))
                {
                    throw new InvalidOperationException(
                        $"PolicyImpactLedger: the bill kind {kind} has no family the family table holds, so its bills would land on the interaction line. Add it to BillFamilies.");
                }

                if (!billFamilies.Add(family) || Families[family].Length > 0 || new List<string>(LeverFamilies.Values).Contains(family))
                {
                    throw new InvalidOperationException(
                        $"PolicyImpactLedger: the bill kind {kind}'s family '{family}' is not its own - another kind, a dial or a lever shares it.");
                }
            }
        }

        public void Dispose()
        {
            foreach (KeyValuePair<string, ShadowBaseline> pair in _except) { pair.Value.Dispose(); }
            _except.Clear();
            _withheld.Clear();
        }
    }
}
