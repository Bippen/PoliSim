using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The protocol's invariants, checked on every audit run (`docs/audit/AUDIT_PROTOCOL.md`, PHASE 2: "Invariants on every run"). Each
    /// reads the model where the Phase 0 inventory found it (`docs/audit/AUDIT_LEDGER.md`'s GA-0 rows name the inventory); none writes it.
    /// A breach is recorded once per distinct key with its day and its figures, counted every time, and printed in the run's summary - it
    /// is a lead for a ledger row, which the protocol's evidence rules then reproduce, never a row by itself.
    /// </summary>
    internal sealed class AuditInvariants
    {
        private readonly Action<string> _log;
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);
        private readonly FieldInfo[] _floatFields;
        private readonly FieldInfo[] _doubleFields;
        private int _electionsAtStart = -1;
        private DateTime _lastExtraElectionDate = DateTime.MinValue;
        private readonly List<DateTime> _pollingDays = new List<DateTime>();

        /// <summary>The checks that ran at least once, so the summary can say which held and which never had a chance to fire.</summary>
        private readonly HashSet<string> _exercised = new HashSet<string>(StringComparer.Ordinal);

        internal AuditInvariants(Action<string> log)
        {
            _log = log;
            FieldInfo[] all = TrajectoryBaselineDump.StateFields();
            _floatFields = all.Where(f => f.FieldType == typeof(float)).ToArray();
            _doubleFields = all.Where(f => f.FieldType == typeof(double)).ToArray();
        }

        internal int Breaches => _counts.Values.Sum();

        internal string Summary()
        {
            var sb = new StringBuilder();
            sb.Append("invariants exercised: ").Append(string.Join(" ", _exercised.OrderBy(s => s, StringComparer.Ordinal)));
            if (_counts.Count == 0) { sb.Append("; no breach"); }
            else { sb.Append("; BREACHES: ").Append(string.Join(", ", _counts.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + " x" + p.Value.ToString(CultureInfo.InvariantCulture)))); }
            return sb.ToString();
        }

        private void Breach(string invariant, string key, int day, DateTime date, string detail)
        {
            _counts[invariant] = (_counts.TryGetValue(invariant, out int n) ? n : 0) + 1;
            if (_seen.Add(invariant + "|" + key))
            {
                _log($"INVARIANT {invariant} day {day} ({date:yyyy-MM-dd}): {detail}");
            }
        }

        /// <summary>No NaN or Inf in any public float or double of any country's state, nor in the country-level figures the trajectory dump reads.</summary>
        internal void Finite(World world, int day, DateTime date)
        {
            _exercised.Add("FINITE");
            foreach (Country c in world.Countries)
            {
                foreach (FieldInfo f in _floatFields)
                {
                    float v = (float)f.GetValue(c.State);
                    if (float.IsNaN(v) || float.IsInfinity(v)) { Breach("FINITE", c.Id + "." + f.Name, day, date, $"{c.Id} State.{f.Name} = {v.ToString(CultureInfo.InvariantCulture)}"); }
                }

                foreach (FieldInfo f in _doubleFields)
                {
                    double v = (double)f.GetValue(c.State);
                    if (double.IsNaN(v) || double.IsInfinity(v)) { Breach("FINITE", c.Id + "." + f.Name, day, date, $"{c.Id} State.{f.Name} = {v.ToString(CultureInfo.InvariantCulture)}"); }
                }

                float[] extra = { c.PotentialGrowthRate, c.PartyApprovalRating, c.CurrencyZone != null ? c.CurrencyZone.InterestRate : 0f };
                string[] names = { "PotentialGrowthRate", "PartyApprovalRating", "Zone.InterestRate" };
                for (int i = 0; i < extra.Length; i++)
                {
                    if (float.IsNaN(extra[i]) || float.IsInfinity(extra[i])) { Breach("FINITE", c.Id + "." + names[i], day, date, $"{c.Id} {names[i]} = {extra[i].ToString(CultureInfo.InvariantCulture)}"); }
                }
            }
        }

        /// <summary>The clamps the model states for itself (the energy/macro inventory's bounds table): a value outside one means a write went round its clamp.</summary>
        internal void Bounds(World world, int day, DateTime date)
        {
            _exercised.Add("BOUNDS");
            foreach (Country c in world.Countries)
            {
                EconomyState s = c.State;
                Check(c, "Unemployment", s.Unemployment, 0f, 30f, day, date);
                Check(c, "Inflation", s.Inflation, 0f, 30f, day, date);
                Check(c, "GDP", s.GDP, 1f, float.MaxValue, day, date);
                Check(c, "PriceLevel", s.PriceLevel, 0.0001f, float.MaxValue, day, date);
                Check(c, "ApprovalRating", s.ApprovalRating, 0f, 100f, day, date);
                Check(c, "PartyApprovalRating", c.PartyApprovalRating, 0f, 100f, day, date);
                if (c.CurrencyZone != null) { Check(c, "Zone.InterestRate", c.CurrencyZone.InterestRate, 0f, 15f, day, date); }
            }
        }

        private void Check(Country c, string name, float v, float lo, float hi, int day, DateTime date)
        {
            if (float.IsNaN(v) || v < lo - 1e-4f || v > hi + 1e-4f)
            {
                Breach("BOUNDS", c.Id + "." + name, day, date, $"{c.Id} {name} = {v.ToString("R", CultureInfo.InvariantCulture)} outside [{lo.ToString(CultureInfo.InvariantCulture)}, {hi.ToString(CultureInfo.InvariantCulture)}]");
            }
        }

        /// <summary>Seat totals equal the chamber: before the run's first count the chamber of record's own size (Germany's 2021 chamber holds 736), after it the chamber's statutory size.</summary>
        internal void Seats(Country c, int day, DateTime date)
        {
            if (_electionsAtStart < 0) { _electionsAtStart = c.ElectionHistory.Count; }
            if (c.ParliamentSeats == null || c.ParliamentSeats.Count == 0) { return; }
            _exercised.Add("SEATS");
            int sum = c.ParliamentSeats.Values.Sum();
            bool counted = c.ElectionHistory.Count > _electionsAtStart;
            int expected = counted ? PartySystems.ChamberSeats(c.Id) : PartySystems.ChamberSizeAt(c.Id, WorldClock.SeatedVintage(c.Id, date));
            if (sum != expected)
            {
                Breach("SEATS", c.Id + "|" + sum + "|" + expected, day, date,
                    $"{c.Id} ParliamentSeats sum to {sum}, the chamber {(counted ? "after the run's count" : "of record")} holds {expected} (statutory {PartySystems.ChamberSeats(c.Id)})");
            }

            foreach (KeyValuePair<string, int> p in c.ParliamentSeats)
            {
                if (p.Value < 0) { Breach("SEATS", c.Id + "|negative|" + p.Key, day, date, $"{c.Id} {p.Key} holds {p.Value} seats"); }
            }
        }

        /// <summary>Called each day with the manager: remembers the extra-election day the manager has set, which an extra election's record must match.</summary>
        internal void NoteDay(SimulationManager sm)
        {
            if (sm.ExtraElectionDate != DateTime.MinValue) { _lastExtraElectionDate = sm.ExtraElectionDate; }
        }

        /// <summary>A new election record: its shares sum to one (fractions), its seats to the chamber, and its date is legal by the country's own rule.</summary>
        internal void Election(Country c, ElectionRecord r, int day, DateTime date)
        {
            _exercised.Add("ELECTION");
            if (r.Shares != null && r.Shares.Count > 0)
            {
                double total = r.Shares.Values.Sum();
                if (Math.Abs(total - 1.0) > 1e-6) { Breach("SHARES", c.Id + "|" + r.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), day, date, $"{c.Id} the election of {r.Date:yyyy-MM-dd}: shares sum to {total.ToString("R", CultureInfo.InvariantCulture)}, not 1"); }
                foreach (KeyValuePair<string, double> p in r.Shares)
                {
                    if (double.IsNaN(p.Value) || p.Value < 0.0 || p.Value > 1.0) { Breach("SHARES", c.Id + "|" + p.Key + "|" + r.Date.Ticks, day, date, $"{c.Id} {p.Key}'s share {p.Value.ToString("R", CultureInfo.InvariantCulture)} on {r.Date:yyyy-MM-dd}"); }
                }
            }
            else
            {
                Breach("SHARES", c.Id + "|none|" + r.Date.Ticks, day, date, $"{c.Id} the election of {r.Date:yyyy-MM-dd} carries no shares ({r.Method}{(string.IsNullOrEmpty(r.NotHeldReason) ? string.Empty : ": " + r.NotHeldReason)})");
            }

            int seats = r.Seats != null ? r.Seats.Values.Sum() : 0;
            if (seats != PartySystems.ChamberSeats(c.Id))
            {
                Breach("SEATS", c.Id + "|record|" + r.Date.Ticks, day, date, $"{c.Id} the election of {r.Date:yyyy-MM-dd}: its seats sum to {seats}, the chamber holds {PartySystems.ChamberSeats(c.Id)}");
            }

            string why = LegalDate(c.Id, r.Date);
            if (why != null) { Breach("DATES", c.Id + "|" + r.Date.Ticks, day, date, $"{c.Id} the election of {r.Date:yyyy-MM-dd}: {why}"); }
            _pollingDays.Add(r.Date);
        }

        /// <summary>The date rules, read from the statutes as ruled (the elections inventory's table), independently of the code that computes them:
        /// Sweden's ordinary election on the second Sunday of September every fourth year from 2026, or the extra election the manager set;
        /// Germany's snap election of 23 February 2025; Poland's Sejm on a Sunday inside the thirty days before the fourth anniversary of the first
        /// sitting (polling day plus thirty, ruling E2). Null when the date is legal.</summary>
        private string LegalDate(CountryId id, DateTime d)
        {
            switch (id)
            {
                case CountryId.Sweden:
                    if (d.DayOfWeek != DayOfWeek.Sunday) { return "not a Sunday"; }
                    if (d.Date == _lastExtraElectionDate.Date) { return null; }
                    if (d.Month == 9 && d.Day >= 8 && d.Day <= 14 && (d.Year - 2026) % 4 == 0 && d.Year >= 2026) { return null; }
                    return "neither the second Sunday of September of an election year (2026 + 4k) nor the extra election's day the manager set (" + (_lastExtraElectionDate == DateTime.MinValue ? "none set" : _lastExtraElectionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) + ")";
                case CountryId.Germany:
                    return d.Date == new DateTime(2025, 2, 23) ? null : "Germany holds only the snap election of 23 February 2025 in the game";
                case CountryId.Poland:
                    if (d.DayOfWeek != DayOfWeek.Sunday) { return "not a Sunday"; }
                    if (d.Date == new DateTime(2023, 10, 15)) { return null; }
                    if (_pollingDays.Count == 0) { return "a Sejm election with no previous polling day in the run to date it from"; }
                    DateTime previous = _pollingDays[_pollingDays.Count - 1];
                    DateTime anniversary = previous.AddDays(30).AddYears(4);
                    return d > anniversary.AddDays(-30) && d <= anniversary ? null
                        : $"outside the thirty days before the fourth anniversary of the first sitting ({anniversary.AddDays(-30):yyyy-MM-dd} .. {anniversary:yyyy-MM-dd}, from polling day {previous:yyyy-MM-dd} + 30)";
                default:
                    return "an election held in a country whose elections are not modelled";
            }
        }

        /// <summary>A government holds a majority or is a declared minority - recomputed from the seats, never trusted from its label (the elections
        /// inventory's D-14: the label is not recomputed when a partner leaves). The declared exceptions are named, not failed.</summary>
        internal void Government(Country c, SimulationManager sm, int day, DateTime date)
        {
            GovernmentRecord g = c.Government;
            if (g == null) { Breach("GOVERNMENT", c.Id + "|null", day, date, $"{c.Id} has no government record"); return; }
            if (g.Kind == WorldClock.ExecutiveKind.Presidency) { return; }   // a presidency's government is not a chamber's majority
            if (c.ParliamentSeats == null || c.ParliamentSeats.Count == 0) { return; }
            _exercised.Add("GOVERNMENT");
            int total = c.ParliamentSeats.Values.Sum();
            int majority = total / 2 + 1;
            int cabinet = g.Cabinet.Sum(p => c.ParliamentSeats.TryGetValue(p, out int s) ? s : 0);
            int support = g.Support.Sum(p => c.ParliamentSeats.TryGetValue(p, out int s) ? s : 0);
            SpeakerRound round = sm.RoundOf(c.Id);
            string state = $"{g.PmParty} [{string.Join("+", g.Cabinet)}{(g.Support.Count > 0 ? " / support " + string.Join("+", g.Support) : string.Empty)}] '{g.Outcome}', cabinet {cabinet}, with support {cabinet + support}, majority {majority} of {total}";
            string key = c.Id + "|" + g.PmParty + "|" + string.Join("+", g.Cabinet) + "|" + string.Join("+", g.Support) + "|" + g.Outcome + "|" + total + "|" + g.Caretaker;

            bool majorityHeld = cabinet >= majority;
            bool supported = g.Support.Count > 0 && cabinet + support >= majority;
            if (g.Outcome == "MajorityCoalition" && !majorityHeld)
            {
                Breach("GOVERNMENT", key + "|label", day, date, $"{c.Id} labelled MajorityCoalition below a majority: {state}");
            }

            if (majorityHeld || supported) { return; }
            if (g.Outcome == "MinorityGovernment") { return; }   // declared
            if (g.Caretaker || g.Outcome == "what-if" || (round != null && round.Open)) { return; }   // declared exceptions: a caretaker, France's what-if, a round in progress
            Breach("GOVERNMENT", key, day, date, $"{c.Id} neither a majority, a supported minority nor a declared minority: {state}");
        }

        private readonly Dictionary<CountryId, double> _twoBooks = new Dictionary<CountryId, double>();

        /// <summary>A load or a scenario's start re-writes the debt: the two books' sum is taken afresh from the next day.</summary>
        internal void RestartDebt() => _twoBooks.Clear();

        /// <summary>Debt change = deficit, daily, in the money inventory's form (1e): the debt falls by the day's balance and the running budget rises by
        /// it, so `GovernmentDebt + Budget` stays where it was - except on a day the debt's clamp binds (at -10x or +3x nominal GDP), which is named,
        /// not failed. The yearly ledger audit is the game's own (`ATTRIB:` in the log, counted as a logged error). And the fund: a negative standing
        /// contribution drawn from an empty fund books money the fund never had (the inventory's lead D-02).</summary>
        internal void Debt(World world, int day, DateTime date)
        {
            _exercised.Add("DEBT");
            foreach (Country c in world.Countries)
            {
                EconomyState s = c.State;
                double sum = (double)s.GovernmentDebt + s.Budget;
                double nominal = Math.Max(1e-9, (double)s.NominalGdp);
                bool clamped = Math.Abs(s.GovernmentDebt - 3.0 * nominal) <= 1e-4 * nominal || Math.Abs(s.GovernmentDebt + 10.0 * nominal) <= 1e-4 * nominal;
                if (_twoBooks.TryGetValue(c.Id, out double previous) && !clamped)
                {
                    double tolerance = 1e-3 + 1e-6 * (Math.Abs(s.GovernmentDebt) + Math.Abs(s.Budget));
                    if (Math.Abs(sum - previous) > tolerance)
                    {
                        Breach("DEBT", c.Id + "|" + day, day, date, $"{c.Id} GovernmentDebt + Budget moved {(sum - previous).ToString("R", CultureInfo.InvariantCulture)} in a day (debt {s.GovernmentDebt.ToString("R", CultureInfo.InvariantCulture)}, budget {s.Budget.ToString("R", CultureInfo.InvariantCulture)}) with the clamp not binding");
                    }
                }

                _twoBooks[c.Id] = sum;
                SovereignWealthFund fund = c.SovereignWealthFund;
                if (fund != null && fund.TotalAssets <= 0f && fund.ContributionRatePercent < 0f)
                {
                    Breach("FUND", c.Id + "|empty-draw", day, date, $"{c.Id} the fund is empty (assets {fund.TotalAssets.ToString("R", CultureInfo.InvariantCulture)}) while its standing contribution is {fund.ContributionRatePercent.ToString("R", CultureInfo.InvariantCulture)} % of GDP - the budget books a draw the fund cannot pay");
                }
            }
        }

        /// <summary>Energy supply = demand (+ losses, which the layer folds into its demand): per block, from a fresh clearing of the player's country at a
        /// turn boundary (the energy inventory's identity: (MustRun - floors) + fossil - curtailed + unserved = demand, to a relative 1e-9). Sweden's
        /// chain balances to its printed unbalance where no link binds, which stays small but not zero (the unmodelled losses).</summary>
        internal void Energy(Country c, int day, DateTime date)
        {
            EnergyMarket.Result r;
            try { r = EnergyMarket.Clear(c); }
            catch (Exception e) { Breach("ENERGY", c.Id + "|throws", day, date, $"{c.Id} EnergyMarket.Clear threw {e.GetType().Name}: {e.Message}"); return; }
            if (r == null || r.Zones == null) { return; }
            _exercised.Add("ENERGY");
            if (c.Id == CountryId.Sweden)
            {
                if (r.ChainUnbalanceMw == null) { return; }
                for (int b = 0; b < r.ChainUnbalanceMw.Length; b++)
                {
                    bool binding = r.Links != null && r.Links.Any(l => l.Binding[b]);
                    if (binding) { continue; }
                    double surplus = r.Zones.Sum(z => z[b].MustRunMw - z[b].DemandMw);
                    if (Math.Abs(surplus - r.ChainUnbalanceMw[b]) > 1e-6 * Math.Max(1.0, Math.Abs(surplus)))
                    {
                        Breach("ENERGY", c.Id + "|chain|" + b, day, date, $"Sweden block {b}: the zones' surplus {surplus:F3} MW against the printed chain unbalance {r.ChainUnbalanceMw[b]:F3} MW");
                    }

                    if (Math.Abs(r.ChainUnbalanceMw[b]) > 500.0)
                    {
                        Breach("ENERGY", c.Id + "|unbalance|" + b, day, date, $"Sweden block {b}: the chain's unbalance {r.ChainUnbalanceMw[b]:F1} MW with no link binding - the losses it stands for are tens of MW");
                    }
                }

                return;
            }

            using (EnergyFleet.For(c))
            {
                double floors = 0.0;
                for (int k = 0; k < EnergyMarket.FossilCount; k++) { floors += EnergyMarket.MustRunFossilMw(c.Id, k); }
                foreach (EnergyMarket.BlockResult[] zone in r.Zones)
                {
                    foreach (EnergyMarket.BlockResult block in zone)
                    {
                        double supply = block.MustRunMw - floors + block.FossilMw.Sum() - block.CurtailedMw + block.UnservedMw;
                        if (Math.Abs(supply - block.DemandMw) > 1e-9 * Math.Max(1.0, block.DemandMw))
                        {
                            Breach("ENERGY", c.Id + "|" + block.Zone + "|" + block.Block, day, date,
                                $"{c.Id} {block.Zone} block {block.Block}: supply {supply:F6} MW (must-run {block.MustRunMw:F3} - floors {floors:F3} + fossil {block.FossilMw.Sum():F3} - curtailed {block.CurtailedMw:F3} + unserved {block.UnservedMw:F3}) against demand {block.DemandMw:F6} MW");
                        }
                    }
                }
            }
        }
    }
}
